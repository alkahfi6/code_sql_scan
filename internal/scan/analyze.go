package scan

import (
	"crypto/sha1"
	"fmt"
	"regexp"
	"sort"
	"strings"
	"unicode"
)

var (
	insertTargetRe = regexp.MustCompile(`(?is)insert\s+(?:into\s+)?([A-Za-z0-9_\[\]\.\"]+)`)
	updateTargetRe = regexp.MustCompile(`(?is)update\s+([A-Za-z0-9_\[\]\.\"]+)\s+set\s+`)
	deleteTargetRe = regexp.MustCompile(`(?is)delete\s+from\s+([A-Za-z0-9_\[\]\.\"]+)`)
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

func normalizeSqlWhitespace(sql string) string {
	if sql == "" {
		return ""
	}
	sql = strings.ReplaceAll(sql, "\r", "\n")
	parts := strings.Fields(sql)
	return strings.Join(parts, " ")
}

func analyzeCandidate(c *SqlCandidate) {
	if !c.IsDynamic {
		raw := c.RawSql
		if strings.Contains(raw, "[[") || strings.Contains(raw, "]]") || strings.Contains(raw, "${") {
			c.IsDynamic = true
		}
	}
	sqlClean := StripSqlComments(c.RawSql)
	sqlClean = normalizeSqlWhitespace(sqlClean)
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

	dmlTokens := detectAllDmlTargets(sqlClean, c.LineStart)
	tokens := append([]ObjectToken{}, dmlTokens...)
	tokens = append(tokens, findObjectTokens(sqlClean)...)
	tokens = append(tokens, detectDynamicObjectPlaceholders(sqlClean, usage, c.LineStart)...)
	tokens = append(tokens, inferDynamicObjectFallbacks(sqlClean, c.RawSql, usage, c.LineStart)...)
	if strings.EqualFold(strings.TrimSpace(c.RawSql), "<dynamic-sql>") {
		tokens = ensureDynamicSqlPseudo(tokens, c, "UNKNOWN")
	}
	insertTargetKeys := make(map[string]struct{})
	if usage == "INSERT" {
		insertTargets := detectDmlTargetsFromSql(sqlClean, usage, c.LineStart)
		insertTargets = append(insertTargets, collectInsertTargets(sqlClean, c.ConnDb, c.LineStart)...)
		if tok, ok := parseLeadingInsertTarget(sqlClean, c.ConnDb, c.LineStart); ok {
			insertTargets = append([]ObjectToken{tok}, insertTargets...)
		}
		for _, t := range insertTargets {
			key := strings.ToLower(buildFullName(t.DbName, t.SchemaName, t.BaseName))
			if key == "" {
				key = strings.ToLower(strings.TrimSpace(t.FullName))
			}
			insertTargetKeys[key] = struct{}{}
		}
		tokens = append(insertTargets, tokens...)
	} else {
		for _, t := range dmlTokens {
			if strings.EqualFold(strings.TrimSpace(t.DmlKind), "INSERT") {
				key := strings.ToLower(buildFullName(t.DbName, t.SchemaName, t.BaseName))
				if key == "" {
					key = strings.ToLower(strings.TrimSpace(t.FullName))
				}
				insertTargetKeys[key] = struct{}{}
			}
		}
	}
	classifyObjects(c, usage, tokens)

	if usage == "INSERT" && len(insertTargetKeys) > 0 {
		for i := range c.Objects {
			key := strings.ToLower(buildFullName(c.Objects[i].DbName, c.Objects[i].SchemaName, c.Objects[i].BaseName))
			if key == "" {
				key = strings.ToLower(strings.TrimSpace(c.Objects[i].FullName))
			}
			if _, ok := insertTargetKeys[key]; ok {
				c.Objects[i].Role = "target"
				c.Objects[i].DmlKind = "INSERT"
				c.Objects[i].IsWrite = true
			}
		}
	}

	// If Exec stub and no tokens, interpret RawSql as proc spec
	if c.IsExecStub && len(c.Objects) == 0 && strings.TrimSpace(c.RawSql) != "" {
		tok := parseProcNameSpec(c.RawSql)
		tok.IsCrossDb = tok.DbName != ""
		tok.Role = "exec"
		tok.DmlKind = "EXEC"
		tok.IsWrite = true
		tok.RepresentativeLine = c.LineStart
		if tok.SchemaName == "" && tok.BaseName != "" && tok.DbName != "" {
			tok.SchemaName = "dbo"
		}
		c.Objects = []ObjectToken{tok}
	}

	if strings.TrimSpace(c.CallSiteKind) == "" {
		c.CallSiteKind = canonicalCallSiteKind(c.SourceKind)
	} else {
		c.CallSiteKind = canonicalCallSiteKind(c.CallSiteKind)
	}

	updateCrossDbMetadata(c)

	hashInput := c.SqlClean
	if hashInput == "" {
		hashInput = c.RawSql
	}
	h := sha1.Sum([]byte(hashInput))
	c.QueryHash = fmt.Sprintf("%x", h[:])

	c.RiskLevel = classifyRisk(c)
}

func updateCrossDbMetadata(c *SqlCandidate) {
	dbSet := make(map[string]struct{})
	hasCross := false

	for i := range c.Objects {
		obj := &c.Objects[i]
		obj.DbName = strings.TrimSpace(obj.DbName)
		if obj.DbName != "" && obj.SchemaName == "" {
			obj.SchemaName = "dbo"
		}
		if obj.DbName != "" {
			dbSet[obj.DbName] = struct{}{}
			if !obj.IsCrossDb {
				obj.IsCrossDb = true
			}
			hasCross = true
			continue
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

	if hasCross {
		c.DbList = dbList
	} else {
		c.DbList = nil
	}
	c.HasCrossDb = hasCross
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
	var found []string
	foundSet := make(map[string]struct{})

	flushToken := func() string {
		tok := strings.Trim(tokenBuf.String(), "[]")
		tokenBuf.Reset()
		return tok
	}

	processToken := func(tok string) {
		if tok == "" {
			return
		}
		if _, skip := skipTokens[tok]; skip {
			return
		}
		if val, ok := targets[tok]; ok {
			found = append(found, val)
			foundSet[val] = struct{}{}
		}
	}

	for _, r := range trimmed {
		if unicode.IsSpace(r) || strings.ContainsRune("();,", r) {
			tok := strings.ToLower(flushToken())
			processToken(tok)
			continue
		}
		tokenBuf.WriteRune(unicode.ToLower(r))
	}

	processToken(strings.ToLower(flushToken()))

	if len(foundSet) == 0 {
		return "UNKNOWN"
	}

	order := []string{"INSERT", "UPDATE", "DELETE", "TRUNCATE", "SELECT", "EXEC"}
	for _, kind := range order {
		if _, ok := foundSet[kind]; ok {
			return kind
		}
	}
	if len(found) > 0 {
		return found[0]
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
			key := strings.TrimSpace(objText)
			if key == "" {
				start = end
				continue
			}
			dbName, schemaName, baseName, isLinked := splitObjectNameParts(objText)
			if baseName != "" && schemaName == "" {
				schemaName = "dbo"
			}
			fullName := buildFullName(dbName, schemaName, baseName)
			tok := ObjectToken{
				DbName:          dbName,
				SchemaName:      schemaName,
				BaseName:        baseName,
				FullName:        fullName,
				FoundAt:         p,
				IsLinkedServer:  isLinked,
				IsObjectNameDyn: hasDynamicPlaceholder(objText),
			}
			tok = normalizeObjectToken(tok)
			tokens = append(tokens, tok)
			start = end
		}
	}

	return tokens
}

func detectDmlTargetsFromSql(sql string, usage string, line int) []ObjectToken {
	usage = strings.ToUpper(strings.TrimSpace(usage))
	var re *regexp.Regexp
	switch usage {
	case "INSERT":
		re = insertTargetRe
	case "UPDATE":
		re = updateTargetRe
	case "DELETE":
		re = deleteTargetRe
	default:
		return nil
	}

	cleaned := StripSqlComments(sql)
	matches := re.FindAllStringSubmatchIndex(cleaned, -1)
	var tokens []ObjectToken

	for _, m := range matches {
		if len(m) < 4 {
			continue
		}
		rawName := strings.TrimSpace(cleaned[m[2]:m[3]])
		if rawName == "" {
			continue
		}

		dbName, schemaName, baseName, isLinked := splitObjectNameParts(rawName)
		if baseName == "" {
			continue
		}
		if schemaName == "" {
			schemaName = "dbo"
		}

		startPos := m[2]
		if startPos < 0 {
			startPos = m[0]
		}

		tok := ObjectToken{
			DbName:             dbName,
			SchemaName:         schemaName,
			BaseName:           baseName,
			FullName:           buildFullName(dbName, schemaName, baseName),
			Role:               "target",
			DmlKind:            usage,
			IsWrite:            true,
			FoundAt:            startPos,
			RepresentativeLine: line,
			IsObjectNameDyn:    hasDynamicPlaceholder(rawName),
			IsLinkedServer:     isLinked,
		}

		tok = normalizeObjectToken(tok)
		tokens = append(tokens, tok)
	}

	return tokens
}

func detectAllDmlTargets(sql string, line int) []ObjectToken {
	var tokens []ObjectToken
	seen := make(map[string]struct{})
	for _, kind := range []string{"INSERT", "UPDATE", "DELETE"} {
		for _, tok := range detectDmlTargetsFromSql(sql, kind, line) {
			key := strings.ToLower(fmt.Sprintf("%s|%s|%s|%s", kind, tok.DbName, tok.SchemaName, tok.BaseName))
			if key == "" {
				key = strings.ToLower(strings.TrimSpace(tok.FullName + "|" + tok.Role + "|" + tok.DmlKind))
			}
			if _, ok := seen[key]; ok {
				continue
			}
			seen[key] = struct{}{}
			tokens = append(tokens, tok)
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
		b == '_' || b == '.' || b == '$' || b == '-' || b == '[' || b == ']'
}

func splitObjectNameParts(full string) (db, schema, base string, isLinked bool) {
	full = strings.TrimSpace(full)
	if full == "" {
		return "", "", "", false
	}
	unbracket := func(s string) string {
		s = strings.TrimSpace(s)
		if strings.HasPrefix(s, "[[") && strings.HasSuffix(s, "]]") {
			return s
		}
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

func normalizeObjectToken(tok ObjectToken) ObjectToken {
	if tok.SchemaName == "" && tok.BaseName != "" {
		tok.SchemaName = "dbo"
	}
	if tok.FullName == "" {
		tok.FullName = buildFullName(tok.DbName, tok.SchemaName, tok.BaseName)
	}
	if tok.DbName != "" {
		tok.IsCrossDb = true
	}
	return tok
}

func hasDynamicToken(tokens []ObjectToken) bool {
	for _, tok := range tokens {
		if tok.IsPseudoObject || tok.IsObjectNameDyn {
			return true
		}
		if strings.EqualFold(strings.TrimSpace(tok.BaseName), "<dynamic-sql>") {
			return true
		}
	}
	return false
}

func ensureDynamicSqlPseudo(tokens []ObjectToken, c *SqlCandidate, dml string) []ObjectToken {
	for _, tok := range tokens {
		if strings.EqualFold(strings.TrimSpace(tok.BaseName), "<dynamic-sql>") {
			return tokens
		}
	}
	tokens = append(tokens, ObjectToken{
		BaseName:           "<dynamic-sql>",
		FullName:           "<dynamic-sql>",
		Role:               "mixed",
		DmlKind:            dml,
		IsWrite:            c.IsWrite,
		IsObjectNameDyn:    false,
		IsPseudoObject:     true,
		PseudoKind:         "dynamic-sql",
		RepresentativeLine: c.LineStart,
	})
	return tokens
}

func classifyObjects(c *SqlCandidate, usageKind string, tokens []ObjectToken) {
	if c.IsDynamic && !hasDynamicToken(tokens) {
		dml := usageKind
		if strings.EqualFold(strings.TrimSpace(c.RawSql), "<dynamic-sql>") {
			dml = "UNKNOWN"
		}
		tokens = ensureDynamicSqlPseudo(tokens, c, dml)
	}
	applyDynamicRewrite := strings.EqualFold(strings.TrimSpace(c.SourceKind), "csharp")
	for i := range tokens {
		if strings.TrimSpace(tokens[i].BaseName) == "" {
			tokens[i].BaseName = "<dynamic-object>"
			tokens[i].IsObjectNameDyn = true
			tokens[i].IsPseudoObject = true
			if tokens[i].PseudoKind == "" {
				tokens[i].PseudoKind = "dynamic-object"
			}
			tokens[i].FullName = buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName)
		}
		if applyDynamicRewrite && (hasDynamicPlaceholder(tokens[i].DbName) || hasDynamicPlaceholder(tokens[i].SchemaName)) {
			tokens[i].IsObjectNameDyn = true
		}
		if tokens[i].IsObjectNameDyn {
			if applyDynamicRewrite {
				if hasDynamicPlaceholder(tokens[i].DbName) {
					tokens[i].DbName = ""
				}
				if hasDynamicPlaceholder(tokens[i].SchemaName) {
					tokens[i].SchemaName = ""
				}
			}
			tokens[i].BaseName = "<dynamic-object>"
			tokens[i].FullName = buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName)
		}
		if tokens[i].IsObjectNameDyn && !tokens[i].IsPseudoObject {
			tokens[i].IsPseudoObject = true
		}
		if tokens[i].IsPseudoObject {
			if tokens[i].PseudoKind == "" {
				if tokens[i].IsObjectNameDyn {
					tokens[i].PseudoKind = "dynamic-object"
				} else {
					tokens[i].PseudoKind = "unknown"
				}
			}
		}
	}
	preserveRole := make([]bool, len(tokens))
	// Determine cross-DB for each token first
	for i := range tokens {
		tokens[i].IsCrossDb = tokens[i].DbName != ""
		if tokens[i].RepresentativeLine == 0 {
			tokens[i].RepresentativeLine = c.LineStart
		}
		if strings.TrimSpace(tokens[i].Role) != "" || strings.TrimSpace(tokens[i].DmlKind) != "" || tokens[i].IsWrite {
			preserveRole[i] = true
			continue
		}
		if tokens[i].IsPseudoObject && strings.TrimSpace(tokens[i].Role) != "" {
			preserveRole[i] = true
		}
	}
	// Normalize SQL to lower case for position lookup
	sqlLower := strings.ToLower(c.SqlClean)
	// Compute first occurrence index for each token in the SQL
	positions := make([]int, len(tokens))
	for i := range tokens {
		if tokens[i].FoundAt > 0 {
			positions[i] = tokens[i].FoundAt
			continue
		}
		// search using the token's full name in lower case
		nameLower := strings.ToLower(strings.TrimSpace(tokens[i].FullName))
		idx := strings.Index(sqlLower, nameLower)
		if idx < 0 {
			idx = len(sqlLower) + i // if not found, push to end
		}
		positions[i] = idx
	}
	// Initialize defaults: assume all tokens are sources with UNKNOWN unless we already have a preserved role
	for i := range tokens {
		if preserveRole[i] {
			if tokens[i].RepresentativeLine == 0 {
				tokens[i].RepresentativeLine = c.LineStart
			}
			continue
		}
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
			if preserveRole[i] {
				continue
			}
			tokens[i].Role = "source"
			tokens[i].DmlKind = "SELECT"
			tokens[i].IsWrite = false
		}
	case "INSERT":
		for i := range tokens {
			if preserveRole[i] {
				continue
			}
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
			if preserveRole[i] {
				continue
			}
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
			if preserveRole[i] {
				continue
			}
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
			if preserveRole[i] {
				continue
			}
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
			if preserveRole[i] {
				continue
			}
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
	if usageKind == "INSERT" {
		targetKeys := make(map[string]struct{})
		for i := range tokens {
			if strings.EqualFold(tokens[i].Role, "target") {
				key := strings.ToLower(strings.TrimSpace(tokens[i].FullName))
				if key == "" {
					key = strings.ToLower(buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName))
				}
				targetKeys[key] = struct{}{}
			}
		}
		if len(targetKeys) > 0 {
			for i := range tokens {
				key := strings.ToLower(strings.TrimSpace(tokens[i].FullName))
				if key == "" {
					key = strings.ToLower(buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName))
				}
				if _, ok := targetKeys[key]; ok {
					tokens[i].Role = "target"
					tokens[i].DmlKind = "INSERT"
					tokens[i].IsWrite = true
				}
			}
		}
	}
	if usageKind == "DELETE" {
		targetKeys := make(map[string]struct{})
		for i := range tokens {
			if strings.EqualFold(tokens[i].Role, "target") {
				key := strings.ToLower(strings.TrimSpace(tokens[i].FullName))
				if key == "" {
					key = strings.ToLower(buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName))
				}
				targetKeys[key] = struct{}{}
			}
		}
		if len(targetKeys) > 0 {
			for i := range tokens {
				key := strings.ToLower(strings.TrimSpace(tokens[i].FullName))
				if key == "" {
					key = strings.ToLower(buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName))
				}
				if _, ok := targetKeys[key]; ok {
					tokens[i].Role = "target"
					tokens[i].DmlKind = "DELETE"
					tokens[i].IsWrite = true
				}
			}
		}
	}
	// Mark dynamic object names
	for i := range tokens {
		full := tokens[i].FullName
		tokens[i].IsObjectNameDyn = tokens[i].IsObjectNameDyn || hasDynamicPlaceholder(full)
		tokens[i].RepresentativeLine = c.LineStart
	}
	c.Objects = mergeObjectRoles(tokens)
}

func mergeObjectRoles(tokens []ObjectToken) []ObjectToken {
	if len(tokens) <= 1 {
		for i := range tokens {
			if tokens[i].SchemaName == "" && tokens[i].BaseName != "" && !tokens[i].IsPseudoObject {
				tokens[i].SchemaName = "dbo"
				tokens[i].FullName = buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName)
			}
		}
		return tokens
	}

	type agg struct {
		first     ObjectToken
		hasSource bool
		hasTarget bool
		hasExec   bool
		hasWrite  bool
		dmlSet    map[string]struct{}
		minLine   int
	}

	groups := make(map[string]*agg)
	order := []string{}
	for _, tok := range tokens {
		key := strings.ToLower(buildFullName(tok.DbName, tok.SchemaName, tok.BaseName))
		if key == "" {
			key = strings.ToLower(strings.TrimSpace(tok.FullName))
		}
		if _, ok := groups[key]; !ok {
			copyTok := tok
			if copyTok.SchemaName == "" && copyTok.BaseName != "" && !copyTok.IsPseudoObject {
				copyTok.SchemaName = "dbo"
				copyTok.FullName = buildFullName(copyTok.DbName, copyTok.SchemaName, copyTok.BaseName)
			}
			groups[key] = &agg{first: copyTok, dmlSet: make(map[string]struct{}), minLine: tok.RepresentativeLine}
			order = append(order, key)
		}
		g := groups[key]
		roleLower := strings.ToLower(strings.TrimSpace(tok.Role))
		switch roleLower {
		case "target":
			g.hasTarget = true
		case "exec":
			g.hasExec = true
		default:
			g.hasSource = true
		}
		if tok.IsWrite {
			g.hasWrite = true
		}
		if tok.DmlKind != "" {
			g.dmlSet[strings.ToUpper(strings.TrimSpace(tok.DmlKind))] = struct{}{}
		}
		if tok.RepresentativeLine > 0 && (g.minLine == 0 || tok.RepresentativeLine < g.minLine) {
			g.minLine = tok.RepresentativeLine
		}
		if tok.IsPseudoObject {
			g.first.IsPseudoObject = true
			g.first.PseudoKind = choosePseudoKindLocal(g.first.PseudoKind, defaultPseudoKind(tok.PseudoKind))
		}
		g.first.IsObjectNameDyn = g.first.IsObjectNameDyn || tok.IsObjectNameDyn
	}

	var merged []ObjectToken
	for _, key := range order {
		g := groups[key]
		tok := g.first
		tok.RepresentativeLine = g.minLine
		role := "source"
		if g.hasExec {
			if g.hasSource || g.hasTarget {
				role = "mixed"
			} else {
				role = "exec"
			}
		} else if g.hasTarget && g.hasSource {
			role = "mixed"
		} else if g.hasTarget {
			role = "target"
		}
		tok.Role = role
		tok.IsWrite = g.hasWrite
		if len(g.dmlSet) == 1 {
			for k := range g.dmlSet {
				tok.DmlKind = k
			}
		} else if len(g.dmlSet) > 1 {
			var kinds []string
			for k := range g.dmlSet {
				kinds = append(kinds, k)
			}
			sort.Strings(kinds)
			tok.DmlKind = strings.Join(kinds, ";")
		} else {
			tok.DmlKind = "UNKNOWN"
		}
		if tok.IsPseudoObject && tok.PseudoKind == "" {
			tok.PseudoKind = "unknown"
		}
		merged = append(merged, tok)
	}

	return merged
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
	name = strings.TrimSpace(name)
	if name == "" {
		return false
	}
	if strings.Contains(name, "[[") || strings.Contains(name, "]]") {
		return true
	}
	if strings.Contains(name, "${") || strings.Contains(name, "$\"") || strings.Contains(name, "<expr>") {
		return true
	}
	if strings.Contains(name, "{") && strings.Contains(name, "}") && strings.Contains(name, "$") {
		return true
	}
	if strings.Contains(name, "+") {
		return true
	}
	return regexes.dynamicPlaceholder.MatchString(name)
}

func isDynamicObjectName(name string) bool {
	name = strings.TrimSpace(name)
	if name == "" {
		return false
	}
	if strings.Contains(name, "[[") || strings.Contains(name, "]]") {
		return true
	}
	if strings.Contains(name, "+") {
		return true
	}
	if strings.Contains(name, "${") || strings.Contains(name, "<expr>") || strings.Contains(name, "$\"") {
		return true
	}
	if regexes.dynamicPlaceholder.MatchString(name) {
		return true
	}
	return false
}

func detectDynamicObjectPlaceholders(sql string, usage string, line int) []ObjectToken {
	cleaned := StripSqlComments(sql)
	var tokens []ObjectToken
	re := regexp.MustCompile(`(?is)(insert\s+into|update|delete\s+from|truncate\s+table|from|join)\s+([^\s;]+)`)

	matches := re.FindAllStringSubmatch(cleaned, -1)
	for _, m := range matches {
		if len(m) < 3 {
			continue
		}
		keyword := strings.ToLower(strings.TrimSpace(m[1]))
		objText := strings.TrimSpace(m[2])
		if objText == "" {
			continue
		}
		if !isDynamicObjectName(objText) {
			continue
		}

		dbName, schemaName, _, isLinked := splitObjectNameParts(objText)
		baseName := "<dynamic-object>"
		full := buildFullName(dbName, schemaName, baseName)

		tok := ObjectToken{
			DbName:             dbName,
			SchemaName:         schemaName,
			BaseName:           baseName,
			FullName:           full,
			IsObjectNameDyn:    true,
			IsPseudoObject:     true,
			PseudoKind:         "dynamic-object",
			RepresentativeLine: line,
			IsLinkedServer:     isLinked,
		}

		tok = normalizeObjectToken(tok)

		switch keyword {
		case "insert into":
			tok.Role = "target"
			tok.DmlKind = "INSERT"
			tok.IsWrite = true
		case "update":
			tok.Role = "target"
			tok.DmlKind = "UPDATE"
			tok.IsWrite = true
		case "delete from":
			tok.Role = "target"
			tok.DmlKind = "DELETE"
			tok.IsWrite = true
		case "truncate table":
			tok.Role = "target"
			tok.DmlKind = "TRUNCATE"
			tok.IsWrite = true
		default:
			tok.Role = "source"
			tok.DmlKind = "SELECT"
			tok.IsWrite = false
		}

		if tok.DbName != "" {
			tok.IsCrossDb = true
		}

		tokens = append(tokens, tok)
	}

	return tokens
}

func inferDynamicObjectFallbacks(sqlClean, rawSql, usage string, line int) []ObjectToken {
	usage = strings.ToUpper(strings.TrimSpace(usage))
	if usage == "" || usage == "UNKNOWN" {
		return nil
	}

	hasHint := func(s string) bool {
		s = strings.TrimSpace(s)
		if s == "" {
			return false
		}
		if strings.Contains(s, "[[") || strings.Contains(s, "]]") || strings.Contains(s, "${") {
			return true
		}
		if strings.Contains(s, "<expr>") {
			return true
		}
		if strings.Contains(s, "$") && strings.Contains(s, "{") && strings.Contains(s, "}") {
			return true
		}
		if strings.Contains(s, "\" +") || strings.Contains(s, "+ \"") {
			return true
		}
		return false
	}

	if !hasHint(sqlClean) && !hasHint(rawSql) {
		return nil
	}

	tok := ObjectToken{
		BaseName:           "<dynamic-object>",
		IsObjectNameDyn:    true,
		IsPseudoObject:     true,
		PseudoKind:         "dynamic-object",
		RepresentativeLine: line,
	}

	tok.FullName = buildFullName(tok.DbName, tok.SchemaName, tok.BaseName)

	switch usage {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE":
		tok.Role = "target"
		tok.DmlKind = usage
		tok.IsWrite = true
	case "EXEC":
		tok.Role = "exec"
		tok.DmlKind = usage
		tok.IsWrite = true
	default:
		tok.Role = "source"
		tok.DmlKind = "SELECT"
		tok.IsWrite = false
	}

	return []ObjectToken{tok}
}

func parseLeadingInsertTarget(sql string, connDb string, line int) (ObjectToken, bool) {
	trimmed := strings.TrimSpace(StripSqlComments(sql))
	lower := strings.ToLower(trimmed)
	lower = strings.TrimSpace(lower)
	if !strings.HasPrefix(lower, "insert") {
		return ObjectToken{}, false
	}

	p := len("insert")
	p = skipWS(lower, p)
	if strings.HasPrefix(lower[p:], "into") {
		p = skipWS(lower, p+len("into"))
	}
	if p >= len(lower) {
		return ObjectToken{}, false
	}

	objText, _ := scanObjectName(trimmed, lower, p)
	objText = strings.TrimSpace(objText)
	if objText == "" {
		return ObjectToken{}, false
	}

	dbName, schemaName, baseName, isLinked := splitObjectNameParts(objText)
	if baseName == "" {
		return ObjectToken{}, false
	}
	if schemaName == "" {
		schemaName = "dbo"
	}
	tok := ObjectToken{
		DbName:             dbName,
		SchemaName:         schemaName,
		BaseName:           baseName,
		FullName:           buildFullName(dbName, schemaName, baseName),
		FoundAt:            p,
		IsLinkedServer:     isLinked,
		IsObjectNameDyn:    hasDynamicPlaceholder(objText),
		RepresentativeLine: line,
	}
	tok = normalizeObjectToken(tok)
	tok.Role = "target"
	tok.DmlKind = "INSERT"
	tok.IsWrite = true

	return tok, true
}

func collectInsertTargets(sql string, connDb string, line int) []ObjectToken {
	cleaned := StripSqlComments(sql)
	lower := strings.ToLower(cleaned)
	idx := 0
	var tokens []ObjectToken

	for idx < len(lower) {
		pos := strings.Index(lower[idx:], "insert")
		if pos < 0 {
			break
		}
		start := idx + pos + len("insert")
		p := skipWS(lower, start)
		if strings.HasPrefix(lower[p:], "into") {
			p = skipWS(lower, p+len("into"))
		}
		objText, _ := scanObjectName(cleaned, lower, p)
		objText = strings.TrimSpace(objText)
		if objText == "" {
			idx = p
			continue
		}
		dbName, schemaName, baseName, isLinked := splitObjectNameParts(objText)
		if baseName == "" {
			idx = p
			continue
		}
		if schemaName == "" {
			schemaName = "dbo"
		}
		tok := ObjectToken{
			DbName:             dbName,
			SchemaName:         schemaName,
			BaseName:           baseName,
			FullName:           buildFullName(dbName, schemaName, baseName),
			FoundAt:            p,
			IsLinkedServer:     isLinked,
			IsObjectNameDyn:    hasDynamicPlaceholder(objText),
			RepresentativeLine: line,
			Role:               "target",
			DmlKind:            "INSERT",
			IsWrite:            true,
		}
		tok = normalizeObjectToken(tok)
		tokens = append(tokens, tok)
		idx = p
	}

	return tokens
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

func choosePseudoKindLocal(current, candidate string) string {
	pri := func(kind string) int {
		switch strings.ToLower(strings.TrimSpace(kind)) {
		case "dynamic-sql":
			return 3
		case "dynamic-object":
			return 2
		case "unknown":
			return 1
		default:
			return 0
		}
	}

	if pri(candidate) > pri(current) {
		return strings.ToLower(strings.TrimSpace(candidate))
	}
	return strings.ToLower(strings.TrimSpace(current))
}

func defaultPseudoKind(kind string) string {
	k := strings.ToLower(strings.TrimSpace(kind))
	if k == "" {
		return "unknown"
	}
	return k
}

func canonicalCallSiteKind(kind string) string {
	trimmed := strings.TrimSpace(kind)
	if trimmed == "" {
		return ""
	}
	switch strings.ToLower(trimmed) {
	case "execproc", "exec-proc", "exec":
		return "ExecProc"
	case "commandtext", "command-text":
		return "CommandText"
	case "sqlcommand", "sql-command":
		return "SqlCommand"
	default:
		return trimmed
	}
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
