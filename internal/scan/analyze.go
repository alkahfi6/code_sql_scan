package scan

import (
	"crypto/sha1"
	"fmt"
	"sort"
	"strings"
	"unicode"
)

// ------------------------------------------------------------
// SQL usage analysis (DML, objek, cross-DB)
// ------------------------------------------------------------

func isProcNameSpec(s string) bool {
	trimmed := strings.TrimSpace(s)
	if trimmed == "" {
		return false
	}

	firstTok := strings.ToLower(leadingAlphaNumToken(trimmed))
	switch firstTok {
	case "select", "insert", "update", "delete", "truncate", "exec", "execute", "with":
		return false
	}

	// Allow trailing parameter markers (e.g., "?,?" or "(@p1, @p2)") after the proc name.
	base := trimmed
	if idx := strings.IndexAny(trimmed, " \t\r\n("); idx >= 0 {
		base = strings.TrimSpace(trimmed[:idx])
		tail := strings.TrimSpace(trimmed[idx:])
		if tail != "" && !paramsOnly(tail) {
			return false
		}
	}

	if base == "" || strings.ContainsAny(base, " \t\r\n") {
		return false
	}
	if strings.Contains(base, "[[") || strings.Contains(base, "]]") || strings.ContainsAny(base, "?:") {
		return true
	}
	return true
}

func leadingAlphaNumToken(s string) string {
	var b strings.Builder
	started := false
	for _, r := range s {
		if unicode.IsLetter(r) || unicode.IsDigit(r) || r == '_' {
			b.WriteRune(unicode.ToLower(r))
			started = true
			continue
		}
		if started {
			break
		}
	}
	return b.String()
}

func paramsOnly(s string) bool {
	if s == "" {
		return true
	}
	for _, r := range s {
		switch r {
		case '?', '@', ':', ',', '(', ')', '[', ']', '.', '-', '+', '\'', '"':
		case ' ', '\t', '\r', '\n':
		default:
			if unicode.IsLetter(r) || unicode.IsDigit(r) || r == '_' {
				continue
			}
			return false
		}
	}
	return true
}

func analyzeCandidate(c *SqlCandidate) {
	if !c.IsDynamic {
		raw := c.RawSql
		if strings.Contains(raw, "[[") || strings.Contains(raw, "]]") || strings.Contains(raw, "${") {
			c.IsDynamic = true
		}
	}
	sqlClean := StripSqlComments(c.RawSql)
	sqlClean = strings.TrimSpace(sqlClean)
	c.SqlClean = sqlClean

	// Attempt to map ConnName to default database via shared connection registry.
	if c.ConnDb == "" && c.ConnName != "" {
		if db, ok := connStore.get(c.ConnName); ok {
			c.ConnDb = db
		}
	}

	usage := detectUsageKind(c.IsExecStub, sqlClean)
	c.UsageKind = usage
	c.IsWrite = isWriteKind(usage)

	tokens := findObjectTokens(sqlClean)
	classifyObjects(c, usage, tokens)

	// If Exec stub and no tokens, interpret RawSql as proc spec
	if c.IsExecStub && len(c.Objects) == 0 && strings.TrimSpace(c.RawSql) != "" {
		tok := parseProcNameSpec(c.RawSql)
		if c.ConnDb != "" {
			tok.IsCrossDb = tok.DbName != "" && !strings.EqualFold(tok.DbName, c.ConnDb)
		} else {
			tok.IsCrossDb = tok.DbName != ""
		}
		tok.Role = "exec"
		tok.DmlKind = "EXEC"
		tok.IsWrite = true
		tok.RepresentativeLine = c.LineStart
		if tok.SchemaName == "" && tok.BaseName != "" && tok.DbName != "" {
			tok.SchemaName = "dbo"
		}
		c.Objects = []ObjectToken{tok}
	}

	dbSet := make(map[string]struct{})
	hasCross := false
	for i := range c.Objects {
		obj := &c.Objects[i]
		if obj.DbName != "" {
			dbSet[obj.DbName] = struct{}{}
		}
		if obj.IsCrossDb {
			hasCross = true
		}
	}
	var dbList []string
	for db := range dbSet {
		dbList = append(dbList, db)
	}
	sort.Strings(dbList)
	c.DbList = dbList
	c.HasCrossDb = hasCross

	hashInput := c.SqlClean
	if hashInput == "" {
		hashInput = c.RawSql
	}
	h := sha1.Sum([]byte(hashInput))
	c.QueryHash = fmt.Sprintf("%x", h[:])

	c.RiskLevel = classifyRisk(c)
}

func detectUsageKind(isExecStub bool, sql string) string {
	if isExecStub {
		return "EXEC"
	}
	if sql == "" {
		return "UNKNOWN"
	}

	trimmed := strings.ToLower(strings.TrimSpace(sql))
	if trimmed == "" {
		return "UNKNOWN"
	}

	targets := map[string]string{
		"select":   "SELECT",
		"insert":   "INSERT",
		"update":   "UPDATE",
		"delete":   "DELETE",
		"truncate": "TRUNCATE",
		"exec":     "EXEC",
		"execute":  "EXEC",
	}

	skipTokens := map[string]struct{}{
		"declare": {},
		"set":     {},
		"if":      {},
		"begin":   {},
		"end":     {},
		"drop":    {},
		"create":  {},
		"alter":   {},
		"use":     {},
		"go":      {},
	}

	var tokenBuf strings.Builder
	flushToken := func() string {
		tok := strings.Trim(tokenBuf.String(), "[]")
		tokenBuf.Reset()
		return tok
	}

	processToken := func(tok string) (string, bool) {
		if tok == "" {
			return "", false
		}
		if _, skip := skipTokens[tok]; skip {
			return "", false
		}
		if val, ok := targets[tok]; ok {
			return val, true
		}
		return "", false
	}

	for _, r := range trimmed {
		if unicode.IsSpace(r) || strings.ContainsRune("();,", r) {
			tok := flushToken()
			if kind, ok := processToken(tok); ok {
				return kind
			}
			continue
		}
		tokenBuf.WriteRune(r)
	}

	if kind, ok := processToken(flushToken()); ok {
		return kind
	}

	return "UNKNOWN"
}

// normalizeProcSpecForHash removes optional EXEC/EXECUTE prefixes and trailing semicolons
// to produce a stable hash for stored procedure calls regardless of textual prefixes.
func normalizeProcSpecForHash(s string) string {
	trimmed := strings.TrimSpace(s)
	if trimmed == "" {
		return ""
	}

	lower := strings.ToLower(trimmed)
	if strings.HasPrefix(lower, "exec") {
		fields := strings.Fields(trimmed)
		if len(fields) >= 2 {
			trimmed = strings.Join(fields[1:], " ")
		} else {
			trimmed = ""
		}
	} else if strings.HasPrefix(lower, "execute") {
		fields := strings.Fields(trimmed)
		if len(fields) >= 2 {
			trimmed = strings.Join(fields[1:], " ")
		} else {
			trimmed = ""
		}
	}

	trimmed = strings.TrimSpace(trimmed)
	trimmed = strings.TrimSuffix(trimmed, ";")
	trimmed = strings.TrimSpace(trimmed)
	return trimmed
}

func isWriteKind(kind string) bool {
	switch kind {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE", "EXEC":
		return true
	default:
		return false
	}
}

func findObjectTokens(sql string) []ObjectToken {
	lower := strings.ToLower(sql)
	var tokens []ObjectToken
	seen := make(map[string]bool)

	keywords := []string{
		"from", "join", "update", "into",
		"truncate",
		"delete from", "delete",
		"exec", "execute",
	}

	for _, kw := range keywords {
		k := kw
		start := 0
		for {
			idx := strings.Index(lower[start:], k)
			if idx < 0 {
				break
			}
			pos := start + idx
			end := pos + len(k)

			// Ensure the keyword is not in the middle of an identifier (e.g., object name containing "update").
			if pos > 0 && isIdentChar(lower[pos-1]) {
				start = end
				continue
			}
			if end < len(lower) && isIdentChar(lower[end]) {
				start = end
				continue
			}

			if k == "delete" && strings.HasPrefix(lower[end:], " from") {
				start = end
				continue
			}

			p := skipWS(lower, end)

			if k == "truncate" {
				if strings.HasPrefix(lower[p:], "table") {
					p = skipWS(lower, p+len("table"))
				}
			}

			if p >= len(lower) {
				break
			}
			objText, _ := scanObjectName(sql, lower, p)
			key := strings.ToLower(strings.TrimSpace(objText))
			if objText == "" || seen[key] {
				start = end
				continue
			}
			seen[key] = true
			dbName, schemaName, baseName, isLinked := splitObjectNameParts(objText)
			tokens = append(tokens, ObjectToken{
				DbName:          dbName,
				SchemaName:      schemaName,
				BaseName:        baseName,
				FullName:        objText,
				IsLinkedServer:  isLinked,
				IsObjectNameDyn: hasDynamicPlaceholder(objText),
			})
			start = end
		}
	}

	return tokens
}

func skipWS(s string, i int) int {
	for i < len(s) && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r') {
		i++
	}
	return i
}

func scanObjectName(sql, lower string, i int) (string, int) {
	start := i
	if i >= len(sql) {
		return "", i
	}
	if sql[i] == '[' {
		end := i + 1
		for end < len(sql) {
			if sql[end] == ']' {
				end++
				break
			}
			end++
		}
		for end < len(sql) && sql[end] == '.' {
			end++
			if end < len(sql) && sql[end] == '[' {
				end2 := end + 1
				for end2 < len(sql) {
					if sql[end2] == ']' {
						end2++
						break
					}
					end2++
				}
				end = end2
			} else {
				for end < len(sql) && isIdentChar(sql[end]) {
					end++
				}
			}
		}
		return strings.TrimSpace(sql[start:end]), end
	}
	if sql[i] == '"' {
		end := i + 1
		for end < len(sql) {
			if sql[end] == '"' {
				if end+1 < len(sql) && sql[end+1] == '"' {
					end += 2
					continue
				}
				end++
				break
			}
			end++
		}
		for end < len(sql) && sql[end] == '.' {
			end++
			if end < len(sql) && sql[end] == '"' {
				end2 := end + 1
				for end2 < len(sql) {
					if sql[end2] == '"' {
						if end2+1 < len(sql) && sql[end2+1] == '"' {
							end2 += 2
							continue
						}
						end2++
						break
					}
					end2++
				}
				end = end2
			} else {
				for end < len(sql) && isIdentChar(sql[end]) {
					end++
				}
			}
		}
		return strings.TrimSpace(sql[start:end]), end
	}
	end := i
	for end < len(sql) && isIdentChar(sql[end]) {
		end++
	}
	return strings.TrimSpace(sql[start:end]), end
}

func isIdentChar(b byte) bool {
	return (b >= '0' && b <= '9') ||
		(b >= 'a' && b <= 'z') ||
		(b >= 'A' && b <= 'Z') ||
		b == '_' || b == '.' || b == '$' || b == '-'
}

func splitObjectNameParts(full string) (db, schema, base string, isLinked bool) {
	full = strings.TrimSpace(full)
	if full == "" {
		return "", "", "", false
	}
	unbracket := func(s string) string {
		s = strings.TrimSpace(s)
		if len(s) >= 2 && s[0] == '[' && s[len(s)-1] == ']' {
			return s[1 : len(s)-1]
		}
		if len(s) >= 2 && s[0] == '"' && s[len(s)-1] == '"' {
			return s[1 : len(s)-1]
		}
		return s
	}

	parts := strings.Split(full, ".")
	for i := range parts {
		parts[i] = unbracket(parts[i])
	}

	if len(parts) == 4 {
		isLinked = true
		db = parts[1]
		schema = parts[2]
		base = parts[3]
		return
	}
	if len(parts) == 3 {
		db = parts[0]
		if parts[1] == "" {
			schema = "dbo"
		} else {
			schema = parts[1]
		}
		base = parts[2]
		return
	}
	if len(parts) == 2 {
		schema = parts[0]
		base = parts[1]
		return
	}
	base = parts[0]
	return
}

func classifyObjects(c *SqlCandidate, usageKind string, tokens []ObjectToken) {
	// Determine cross-DB for each token first
	for i := range tokens {
		tokens[i].IsCrossDb = tokens[i].DbName != "" && c.ConnDb != "" && !strings.EqualFold(tokens[i].DbName, c.ConnDb)
		if c.ConnDb == "" && tokens[i].DbName != "" {
			tokens[i].IsCrossDb = true
		}
	}
	// Normalize SQL to lower case for position lookup
	sqlLower := strings.ToLower(c.SqlClean)
	// Compute first occurrence index for each token in the SQL
	positions := make([]int, len(tokens))
	for i := range tokens {
		// search using the token's full name in lower case
		nameLower := strings.ToLower(strings.TrimSpace(tokens[i].FullName))
		idx := strings.Index(sqlLower, nameLower)
		if idx < 0 {
			idx = len(sqlLower) + i // if not found, push to end
		}
		positions[i] = idx
	}
	// Initialize defaults: assume all tokens are sources with UNKNOWN
	for i := range tokens {
		tokens[i].Role = "source"
		tokens[i].DmlKind = "UNKNOWN"
		tokens[i].IsWrite = false
		tokens[i].RepresentativeLine = c.LineStart
	}
	// Determine target based on DML keyword position
	// Choose the first token appearing after the keyword; if none, fall back to the earliest token
	var targetIdx int = -1
	keywordPos := findKeywordPosition(sqlLower, usageKind)
	if keywordPos >= 0 {
		minPos := len(sqlLower) + len(sqlLower)
		for i, p := range positions {
			if p >= keywordPos && p < minPos && p < len(sqlLower) {
				targetIdx = i
				minPos = p
			}
		}
	}
	if targetIdx == -1 && len(tokens) > 0 {
		minPos := len(sqlLower) + len(sqlLower)
		for i, p := range positions {
			if p < minPos && p < len(sqlLower) {
				targetIdx = i
				minPos = p
			}
		}
		if targetIdx == -1 {
			targetIdx = 0
		}
	}
	multiDeleteTargets := usageKind == "DELETE" && strings.Count(sqlLower, "delete") >= len(tokens) && len(tokens) > 1

	// Assign role and DmlKind based on usageKind
	switch usageKind {
	case "SELECT":
		for i := range tokens {
			tokens[i].Role = "source"
			tokens[i].DmlKind = "SELECT"
			tokens[i].IsWrite = false
		}
	case "INSERT":
		for i := range tokens {
			if i == targetIdx {
				tokens[i].Role = "target"
				tokens[i].DmlKind = "INSERT"
				tokens[i].IsWrite = true
			} else {
				tokens[i].Role = "source"
				tokens[i].DmlKind = "SELECT"
				tokens[i].IsWrite = false
			}
		}
	case "UPDATE":
		for i := range tokens {
			if i == targetIdx {
				tokens[i].Role = "target"
				tokens[i].DmlKind = "UPDATE"
				tokens[i].IsWrite = true
			} else {
				tokens[i].Role = "source"
				tokens[i].DmlKind = "SELECT"
				tokens[i].IsWrite = false
			}
		}
	case "DELETE":
		for i := range tokens {
			if multiDeleteTargets || i == targetIdx {
				tokens[i].Role = "target"
				tokens[i].DmlKind = "DELETE"
				tokens[i].IsWrite = true
			} else {
				tokens[i].Role = "source"
				tokens[i].DmlKind = "SELECT"
				tokens[i].IsWrite = false
			}
		}
	case "TRUNCATE":
		for i := range tokens {
			if i == targetIdx || targetIdx == -1 {
				// in truncate, typically one token; if not found choose all
				tokens[i].Role = "target"
				tokens[i].DmlKind = "TRUNCATE"
				tokens[i].IsWrite = true
			} else {
				tokens[i].Role = "source"
				tokens[i].DmlKind = "SELECT"
				tokens[i].IsWrite = false
			}
		}
	case "EXEC":
		for i := range tokens {
			tokens[i].Role = "exec"
			tokens[i].DmlKind = "EXEC"
			tokens[i].IsWrite = true
		}
	default:
		// unknown, treat as select sources
		for i := range tokens {
			tokens[i].Role = "source"
			tokens[i].DmlKind = "UNKNOWN"
			tokens[i].IsWrite = false
		}
	}
	// Mark dynamic object names
	for i := range tokens {
		full := tokens[i].FullName
		tokens[i].IsObjectNameDyn = tokens[i].IsObjectNameDyn || hasDynamicPlaceholder(full)
		tokens[i].RepresentativeLine = c.LineStart
	}
	c.Objects = tokens
}

func findKeywordPosition(sqlLower, usageKind string) int {
	switch usageKind {
	case "INSERT":
		if pos := strings.Index(sqlLower, "insert into"); pos >= 0 {
			return pos + len("insert into")
		}
		if pos := strings.Index(sqlLower, "insert"); pos >= 0 {
			return pos + len("insert")
		}
		return strings.Index(sqlLower, "into")
	case "UPDATE":
		return strings.Index(sqlLower, "update")
	case "DELETE":
		if pos := strings.Index(sqlLower, "delete from"); pos >= 0 {
			return pos + len("delete from")
		}
		return strings.Index(sqlLower, "delete")
	case "TRUNCATE":
		return strings.Index(sqlLower, "truncate")
	case "EXEC":
		if pos := strings.Index(sqlLower, "exec"); pos >= 0 {
			return pos + len("exec")
		}
		if pos := strings.Index(sqlLower, "execute"); pos >= 0 {
			return pos + len("execute")
		}
		return -1
	default:
		return -1
	}
}

func hasDynamicPlaceholder(name string) bool {
	if strings.Contains(name, "[[") || strings.Contains(name, "]]") {
		return true
	}
	return regexes.dynamicPlaceholder.MatchString(name)
}

// parseProcNameSpec interprets a raw stored procedure specification and returns an ObjectToken.

func parseProcNameSpec(s string) ObjectToken {
	trimmed := strings.TrimSpace(s)
	if strings.HasSuffix(trimmed, ";") {
		trimmed = strings.TrimSuffix(trimmed, ";")
		trimmed = strings.TrimSpace(trimmed)
	}
	if idx := strings.Index(trimmed, "("); idx >= 0 {
		trimmed = strings.TrimSpace(trimmed[:idx])
	}

	origTrimmed := trimmed
	trimmed = regexes.procParamPlaceholder.ReplaceAllString(trimmed, "")
	trimmed = strings.TrimSpace(trimmed)

	db, schema, base, isLinked := splitObjectNameParts(trimmed)
	dyn := false
	if strings.Contains(origTrimmed, "[[") || strings.Contains(origTrimmed, "]]") || strings.ContainsAny(origTrimmed, "?:@") {
		dyn = true
	}
	return ObjectToken{
		DbName:          db,
		SchemaName:      schema,
		BaseName:        base,
		FullName:        trimmed,
		IsLinkedServer:  isLinked,
		IsObjectNameDyn: dyn,
	}
}

func classifyRisk(c *SqlCandidate) string {
	if !c.IsWrite {
		return "LOW"
	}
	if c.HasCrossDb && c.IsDynamic {
		return "CRITICAL"
	}
	if c.HasCrossDb || c.IsDynamic {
		return "HIGH"
	}
	return "MEDIUM"
}

func dedupeObjectTokens(c *SqlCandidate) {
	if len(c.Objects) <= 1 {
		return
	}

	seen := make(map[string]bool)
	var uniq []ObjectToken
	for _, o := range c.Objects {
		full := o.FullName
		if full == "" {
			full = buildFullName(o.DbName, o.SchemaName, o.BaseName)
		}
		key := fmt.Sprintf("%s|%s|%d|%s|%s|%s", c.QueryHash, c.RelPath, o.RepresentativeLine, strings.ToLower(full), o.Role, o.DmlKind)
		if seen[key] {
			continue
		}
		seen[key] = true
		uniq = append(uniq, o)
	}
	c.Objects = uniq
}

func dedupeCandidates(cands []SqlCandidate) []SqlCandidate {
	if len(cands) <= 1 {
		return cands
	}

	seen := make(map[string]bool)
	var uniq []SqlCandidate
	for _, c := range cands {
		key := fmt.Sprintf("%s|%s|%d|%d|%s|%s|%s", c.AppName, c.RelPath, c.LineStart, c.LineEnd, c.Func, c.SqlClean, c.UsageKind)
		if seen[key] {
			continue
		}
		seen[key] = true
		uniq = append(uniq, c)
	}
	return uniq
}
