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
	objNamePattern   = `[@A-Za-z0-9_\[\]\"#]+(?:\s*\.\s*[@A-Za-z0-9_\[\]\"#]*)*`
	insertTargetRe   = regexp.MustCompile(`(?is)insert\s+(?:into\s+)?(` + objNamePattern + `)`)
	updateTargetRe   = regexp.MustCompile(`(?is)update\s+(` + objNamePattern + `)\s+set\s+`)
	deleteTargetRe   = regexp.MustCompile(`(?is)delete\s+(?:from\s+)?(` + objNamePattern + `)`)
	truncateTargetRe = regexp.MustCompile(`(?is)truncate\s+(?:table\s+)?(` + objNamePattern + `)`)
	mergeTargetRe    = regexp.MustCompile(`(?is)merge\s+(?:into\s+)?(` + objNamePattern + `)`)
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
	if c.RawSql != "" && c.RawSql != sqlClean {
		tokens = append(tokens, detectDynamicObjectPlaceholders(c.RawSql, usage, c.LineStart)...)
	}
	if !hasDynamicToken(tokens) {
		tokens = append(tokens, inferDynamicObjectFallbacks(sqlClean, c.RawSql, usage, c.LineStart)...)
	}
	if strings.EqualFold(strings.TrimSpace(c.RawSql), "<dynamic-sql>") {
		tokens = ensureDynamicSqlPseudo(tokens, c, "UNKNOWN")
	}

	insertTargetKeys := make(map[string]struct{})
	addInsertKey := func(tok ObjectToken) bool {
		key := strings.ToLower(buildFullName(tok.DbName, tok.SchemaName, tok.BaseName))
		if key == "" {
			key = strings.ToLower(strings.TrimSpace(tok.FullName))
		}
		if key == "" {
			return false
		}
		if _, ok := insertTargetKeys[key]; ok {
			return false
		}
		insertTargetKeys[key] = struct{}{}
		return true
	}
	tokenKey := func(tok ObjectToken) string {
		name := buildFullName(tok.DbName, tok.SchemaName, tok.BaseName)
		if strings.TrimSpace(name) == "" {
			name = strings.TrimSpace(tok.FullName)
		}
		dml := strings.ToUpper(strings.TrimSpace(tok.DmlKind))
		return strings.ToLower(strings.TrimSpace(dml + "|" + name))
	}

	for _, tok := range tokens {
		if strings.EqualFold(strings.TrimSpace(tok.DmlKind), "INSERT") {
			addInsertKey(tok)
		}
	}

	var heuristicTokens []ObjectToken
	if c.IsDynamic {
		seen := make(map[string]struct{})
		for _, tok := range tokens {
			if key := tokenKey(tok); key != "" {
				seen[key] = struct{}{}
			}
		}

		variantSet := make(map[string]struct{})
		var variants []string
		addVariant := func(v string) {
			v = strings.TrimSpace(v)
			if v == "" {
				return
			}
			if _, ok := variantSet[v]; ok {
				return
			}
			variantSet[v] = struct{}{}
			variants = append(variants, v)
		}
		addVariant(sqlClean)
		if rawTrim := normalizeSqlWhitespace(StripSqlComments(c.RawSql)); rawTrim != "" {
			addVariant(rawTrim)
		}
		if rawLiteral := strings.TrimSpace(c.RawSql); rawLiteral != "" {
			addVariant(rawLiteral)
		}

		for _, fragment := range variants {
			frag := strings.TrimSpace(fragment)
			if frag == "" || strings.EqualFold(frag, "<dynamic-sql>") {
				continue
			}
			lowerFrag := strings.ToLower(frag)
			var kinds []string
			if strings.Contains(lowerFrag, "insert into") || insertTargetRe.MatchString(frag) {
				kinds = append(kinds, "INSERT")
			}
			if strings.Contains(lowerFrag, "update") || updateTargetRe.MatchString(frag) {
				kinds = append(kinds, "UPDATE")
			}
			if strings.Contains(lowerFrag, "delete from") || deleteTargetRe.MatchString(frag) {
				kinds = append(kinds, "DELETE")
			}
			if strings.Contains(lowerFrag, "truncate") || truncateTargetRe.MatchString(frag) {
				kinds = append(kinds, "TRUNCATE")
			}
			if strings.Contains(lowerFrag, "merge into") || mergeTargetRe.MatchString(frag) {
				kinds = append(kinds, "MERGE")
			}
			if len(kinds) == 0 {
				continue
			}

			for _, kind := range kinds {
				matched := detectDmlTargetsFromSql(fragment, kind, c.LineStart)
				if len(matched) == 0 {
					for _, tok := range detectDynamicObjectPlaceholders(fragment, kind, c.LineStart) {
						if kind != "" && !strings.EqualFold(tok.DmlKind, kind) {
							continue
						}
						matched = append(matched, tok)
					}
				}
				if len(matched) == 0 {
					matched = append(matched, ObjectToken{
						BaseName:           "<dynamic-object>",
						FullName:           "<dynamic-object>",
						Role:               "target",
						DmlKind:            strings.ToUpper(kind),
						IsWrite:            true,
						IsObjectNameDyn:    true,
						IsPseudoObject:     true,
						PseudoKind:         "dynamic-object",
						RepresentativeLine: c.LineStart,
					})
				}
				for _, tok := range matched {
					if strings.EqualFold(tok.DmlKind, "INSERT") && !addInsertKey(tok) {
						if key := tokenKey(tok); key != "" {
							if _, ok := seen[key]; ok {
								continue
							}
						}
					}
					if key := tokenKey(tok); key != "" {
						if _, ok := seen[key]; ok {
							continue
						}
						seen[key] = struct{}{}
					}
					heuristicTokens = append(heuristicTokens, tok)
				}
			}
		}

		if len(heuristicTokens) > 0 {
			tokens = append(heuristicTokens, tokens...)
			dmlTokens = append(heuristicTokens, dmlTokens...)
		}
	}

	if usage == "INSERT" {
		insertTargets := detectDmlTargetsFromSql(sqlClean, usage, c.LineStart)
		insertTargets = append(insertTargets, collectInsertTargets(sqlClean, c.ConnDb, c.LineStart)...)
		if tok, ok := parseLeadingInsertTarget(sqlClean, c.ConnDb, c.LineStart); ok {
			insertTargets = append([]ObjectToken{tok}, insertTargets...)
		}

		var newInsertTargets []ObjectToken
		for _, t := range insertTargets {
			if addInsertKey(t) {
				newInsertTargets = append(newInsertTargets, t)
			}
		}
		tokens = append(newInsertTargets, tokens...)
	} else {
		for _, t := range dmlTokens {
			if strings.EqualFold(strings.TrimSpace(t.DmlKind), "INSERT") {
				addInsertKey(t)
			}
		}
	}

	classifyObjects(c, usage, tokens)

	c.DynamicSignature = buildDynamicSignature(c)

	if usage == "INSERT" && len(insertTargetKeys) > 0 {
		for i := range c.Objects {
			key := strings.ToLower(buildFullName(c.Objects[i].DbName, c.Objects[i].SchemaName, c.Objects[i].BaseName))
			if key == "" {
				key = strings.ToLower(strings.TrimSpace(c.Objects[i].FullName))
			}
			if _, ok := insertTargetKeys[key]; ok {
				if strings.EqualFold(strings.TrimSpace(c.Objects[i].DmlKind), "DELETE") || strings.EqualFold(strings.TrimSpace(c.Objects[i].DmlKind), "UPDATE") {
					continue
				}
				c.Objects[i].Role = "target"
				c.Objects[i].DmlKind = "INSERT"
				c.Objects[i].IsWrite = true
			}
		}
	}

	// If Exec stub and no tokens, interpret RawSql as proc spec
	if c.IsExecStub && !hasRealObjects(c.Objects) && strings.TrimSpace(c.RawSql) != "" {
		tok := parseProcNameSpec(c.RawSql)
		tok.IsCrossDb = tok.DbName != ""
		tok.Role = "exec"
		tok.DmlKind = "EXEC"
		tok.IsWrite = true
		tok.RepresentativeLine = c.LineStart
		if tok.SchemaName == "" && tok.BaseName != "" && tok.DbName != "" {
			tok.SchemaName = "dbo"
		}
		c.Objects = append(c.Objects, tok)
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

func expandMultiStatementCandidate(c SqlCandidate) []SqlCandidate {
	sqlClean := StripSqlComments(c.RawSql)
	sqlClean = normalizeSqlWhitespace(sqlClean)
	parts := splitCandidateStatements(sqlClean)
	if len(parts) <= 1 {
		return []SqlCandidate{c}
	}

	var out []SqlCandidate
	for _, part := range parts {
		clone := c
		clone.RawSql = part
		clone.SqlClean = ""
		out = append(out, clone)
	}
	return out
}

func splitCandidateStatements(sql string) []string {
	sql = strings.TrimSpace(sql)
	if sql == "" {
		return nil
	}

	baseParts := splitSqlStatements(sql)
	var expanded []string
	for _, part := range baseParts {
		part = strings.TrimSpace(part)
		if part == "" {
			continue
		}
		fragments := splitMultiDmlPart(part)
		expanded = append(expanded, fragments...)
	}

	if len(expanded) == 0 {
		return []string{sql}
	}
	return expanded
}

func splitSqlStatements(sql string) []string {
	sql = strings.TrimSpace(sql)
	if sql == "" {
		return nil
	}

	var parts []string
	var buf strings.Builder
	inSingle, inDouble := false, false
	parenDepth := 0

	flush := func() {
		part := strings.TrimSpace(buf.String())
		if part != "" {
			parts = append(parts, part)
		}
		buf.Reset()
	}

	for i := 0; i < len(sql); {
		ch := sql[i]

		if ch == '\'' && !inDouble {
			if inSingle && i+1 < len(sql) && sql[i+1] == '\'' {
				buf.WriteByte(ch)
				buf.WriteByte(sql[i+1])
				i += 2
				continue
			}
			inSingle = !inSingle
			buf.WriteByte(ch)
			i++
			continue
		}
		if ch == '"' && !inSingle {
			if inDouble && i+1 < len(sql) && sql[i+1] == '"' {
				buf.WriteByte(ch)
				buf.WriteByte(sql[i+1])
				i += 2
				continue
			}
			inDouble = !inDouble
			buf.WriteByte(ch)
			i++
			continue
		}

		if !inSingle && !inDouble {
			if ch == '(' {
				parenDepth++
			}
			if ch == ')' {
				if parenDepth > 0 {
					parenDepth--
				}
			}

			if ch == ';' && parenDepth == 0 {
				flush()
				i++
				continue
			}
		}

		buf.WriteByte(ch)
		i++
	}

	flush()
	return parts
}

func splitMultiDmlPart(sql string) []string {
	trimmed := strings.TrimSpace(sql)
	if trimmed == "" {
		return nil
	}

	dmlRe := regexp.MustCompile(`(?is)(insert\s+into|update\s+|delete\s+from|truncate\s+table|merge\s+into)`) //nolint:lll
	matches := dmlRe.FindAllStringSubmatchIndex(trimmed, -1)
	if len(matches) <= 1 {
		return []string{trimmed}
	}

	var parts []string
	start := 0
	for i, m := range matches {
		if i == 0 {
			continue
		}
		seg := strings.TrimSpace(trimmed[start:m[0]])
		if seg != "" {
			parts = append(parts, seg)
		}
		start = m[0]
	}
	if start < len(trimmed) {
		last := strings.TrimSpace(trimmed[start:])
		if last != "" {
			parts = append(parts, last)
		}
	}

	if len(parts) == 0 {
		return []string{trimmed}
	}

	return parts
}

func updateCrossDbMetadata(c *SqlCandidate) {
	dbSet := make(map[string]struct{})
	hasCross := false
	sqlLower := strings.ToLower(c.SqlClean)
	crossAnchor := findCrossDbAnchor(sqlLower, c.UsageKind)
	trimIdent := func(s string) string {
		return cleanIdentifier(s)
	}
	isLikelyVarName := func(name string) bool {
		trimmed := strings.TrimSpace(name)
		if trimmed == "" {
			return false
		}
		if regexes.identRe != nil && regexes.identRe.MatchString(trimmed) {
			r := rune(trimmed[0])
			if (unicode.IsLower(r) && trimmed == strings.ToLower(trimmed)) || strings.EqualFold(trimmed, "this") {
				return true
			}
		}
		return false
	}

	for i := range c.Objects {
		obj := &c.Objects[i]
		placeholderKind := strings.ToLower(strings.TrimSpace(obj.PseudoKind))
		if strings.TrimSpace(obj.PseudoKind) != "" {
			obj.IsPseudoObject = true
		}
		if placeholderKind == "schema-placeholder" || placeholderKind == "table-placeholder" {
			obj.DbName = strings.TrimSpace(obj.DbName)
			obj.SchemaName = strings.TrimSpace(obj.SchemaName)
			obj.BaseName = trimIdent(obj.BaseName)
		} else {
			obj.DbName = trimIdent(obj.DbName)
			obj.SchemaName = trimIdent(obj.SchemaName)
			obj.BaseName = trimIdent(obj.BaseName)
		}

		if obj.DbName == "" {
			full := strings.TrimSpace(obj.FullName)
			if full == "" && obj.BaseName != "" {
				full = buildFullName(obj.DbName, obj.SchemaName, obj.BaseName)
			}
			full = normalizeFullObjectName(full)
			parts := strings.Split(full, ".")
			if len(parts) == 3 {
				dbCandidate := trimIdent(parts[0])
				schemaCandidate := trimIdent(parts[1])
				baseCandidate := trimIdent(parts[2])
				if dbCandidate != "" && !strings.EqualFold(dbCandidate, "dbo") && schemaCandidate != "" && baseCandidate != "" {
					obj.DbName = dbCandidate
					obj.SchemaName = schemaCandidate
					obj.BaseName = baseCandidate
					obj.FullName = buildFullName(obj.DbName, obj.SchemaName, obj.BaseName)
					obj.IsCrossDb = true
				}
			}
		}

		if placeholderKind != "schema-placeholder" {
			if obj.DbName != "" && obj.SchemaName == "" {
				obj.SchemaName = "dbo"
			}
			if obj.DbName == "" && strings.TrimSpace(obj.SchemaName) == "" {
				obj.SchemaName = "dbo"
			}
		}
		obj.FullName = normalizeFullObjectName(buildFullName(obj.DbName, obj.SchemaName, obj.BaseName))

		if obj.DbName != "" {
			if (placeholderKind == "schema-placeholder" || placeholderKind == "table-placeholder") && isDynamicObjectName(obj.DbName) {
				obj.IsCrossDb = true
				dbSet[obj.DbName] = struct{}{}
				hasCross = true
				continue
			}
			if isLikelyVarName(obj.DbName) || (crossAnchor < 0 && !strings.EqualFold(c.UsageKind, "EXEC")) || (obj.FoundAt > 0 && obj.FoundAt < crossAnchor) {
				obj.DbName = ""
				obj.IsCrossDb = false
			} else {
				obj.IsCrossDb = true
				dbSet[obj.DbName] = struct{}{}
				hasCross = true
				continue
			}
		}
		if obj.IsCrossDb {
			hasCross = true
		}
	}

	if !hasCross && crossAnchor >= 0 {
		crossDbRe := regexp.MustCompile(`(?i)([A-Za-z0-9_]+)\.[A-Za-z0-9_]+\.[A-Za-z0-9_]+`)
		sources := []string{c.SqlClean, c.RawSql}
		for _, src := range sources {
			matches := crossDbRe.FindAllStringSubmatchIndex(src, -1)
			for _, m := range matches {
				if len(m) < 4 {
					continue
				}
				db := strings.TrimSpace(cleanIdentifier(src[m[2]:m[3]]))
				if db == "" || isLikelyVarName(db) {
					continue
				}
				matchStart := m[0]
				if matchStart >= 0 && crossAnchor >= 0 && matchStart < crossAnchor {
					continue
				}
				hasCross = true
				dbSet[db] = struct{}{}
			}
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
		"merge":    "MERGE",
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

	order := []string{"MERGE", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "SELECT", "EXEC"}
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
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "EXEC":
		return true
	default:
		return false
	}
}

func findObjectTokens(sql string) []ObjectToken {
	lower := strings.ToLower(sql)
	var tokens []ObjectToken
	trimIdent := func(s string) string {
		s = strings.TrimSpace(s)
		s = strings.Trim(s, "[]")
		s = strings.Trim(s, `"`)
		return strings.ToLower(s)
	}

	keywords := []string{
		"from", "join", "using", "update", "merge", "into",
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
			trimmedBase := strings.TrimSpace(baseName)
			if baseName != "" && schemaName == "" && !strings.HasPrefix(trimmedBase, "#") && !strings.HasPrefix(trimmedBase, "@") {
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

			baseKey := trimIdent(tok.BaseName)
			if baseKey == "into" {
				start = end
				continue
			}
			if baseKey == "set" {
				start = end
				continue
			}
			if baseKey == "openxml" || baseKey == "sp_xml_preparedocument" {
				tok.Role = "exec"
				tok.DmlKind = "EXEC"
				tok.IsWrite = true
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
	case "TRUNCATE":
		re = truncateTargetRe
	case "MERGE":
		re = mergeTargetRe
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
		trimmedBase := strings.TrimSpace(baseName)
		if schemaName == "" && !strings.HasPrefix(trimmedBase, "#") && !strings.HasPrefix(trimmedBase, "@") {
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
	for _, kind := range []string{"INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE"} {
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
		b == '_' || b == '.' || b == '$' || b == '-' || b == '[' || b == ']' || b == '#' || b == '@'
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
	trimmedBase := strings.TrimSpace(tok.BaseName)
	isTemp := strings.HasPrefix(trimmedBase, "#")
	isTableVar := strings.HasPrefix(trimmedBase, "@")
	if tok.SchemaName == "" && tok.BaseName != "" && !isTemp && !isTableVar {
		tok.SchemaName = "dbo"
	}
	if tok.FullName == "" {
		tok.FullName = buildFullName(tok.DbName, tok.SchemaName, tok.BaseName)
	}
	if tok.DbName != "" {
		tok.IsCrossDb = true
	}
	if isTemp || isTableVar {
		tok.IsPseudoObject = true
		if strings.TrimSpace(tok.PseudoKind) == "" {
			if isTableVar {
				tok.PseudoKind = "table-variable"
			} else {
				tok.PseudoKind = "temp-table"
			}
		}
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

func hasRealObjects(tokens []ObjectToken) bool {
	for _, tok := range tokens {
		if tok.IsPseudoObject {
			continue
		}
		if isDynamicBaseName(tok.BaseName) {
			continue
		}
		return true
	}
	return false
}

func normalizeDynamicReasonLabel(reason string) string {
	switch strings.ToLower(strings.TrimSpace(reason)) {
	case "":
		return ""
	case "concat":
		return "concat runtime"
	case "stringbuilder":
		return "stringbuilder"
	case "interpolation":
		return "string interpolation"
	case "placeholder":
		return "template placeholder"
	default:
		return strings.TrimSpace(reason)
	}
}

func inferDynamicReason(c *SqlCandidate, tokens []ObjectToken) string {
	reasonSet := make(map[string]struct{})
	add := func(r string) {
		if normalized := normalizeDynamicReasonLabel(r); normalized != "" {
			reasonSet[normalized] = struct{}{}
		}
	}

	add(c.DynamicReason)

	lowerRaw := strings.ToLower(c.RawSql)
	lowerClean := strings.ToLower(c.SqlClean)
	if strings.Contains(lowerRaw, "<expr>") || strings.Contains(lowerClean, "<expr>") {
		add("concat runtime")
	}
	if strings.Contains(lowerRaw, "[[") || strings.Contains(lowerRaw, "${") || strings.Contains(lowerRaw, "]]") {
		add("template placeholder")
	}
	for _, tok := range tokens {
		switch strings.ToLower(strings.TrimSpace(tok.PseudoKind)) {
		case "schema-placeholder":
			add("[[schema]] placeholder")
		case "table-placeholder":
			add("[[table]] placeholder")
		case "dynamic-object":
			add("dynamic object name")
		case "dynamic-sql":
			add("dynamic sql")
		}
	}
	if strings.Contains(lowerRaw, "<dynamic-sql>") {
		add("dynamic sql")
	}
	if len(reasonSet) == 0 && c.IsDynamic {
		add("dynamic construction")
	}

	var reasons []string
	for reason := range reasonSet {
		reasons = append(reasons, reason)
	}
	sort.Strings(reasons)
	return strings.Join(reasons, "; ")
}

func buildDynamicSignature(c *SqlCandidate) string {
	if !c.IsDynamic {
		return ""
	}

	raw := strings.TrimSpace(c.RawSql)
	sqlClean := strings.TrimSpace(c.SqlClean)
	isDynamicPlaceholder := strings.EqualFold(raw, "<dynamic-sql>") || strings.EqualFold(sqlClean, "<dynamic-sql>")
	hasPseudo := isDynamicPlaceholder
	if !hasPseudo {
		for _, obj := range c.Objects {
			if !obj.IsPseudoObject {
				continue
			}
			if obj.PseudoKind == "dynamic-sql" || obj.PseudoKind == "dynamic-object" {
				hasPseudo = true
				break
			}
		}
	}

	callKind := canonicalCallSiteKind(c.CallSiteKind)
	if callKind == "" {
		callKind = canonicalCallSiteKind(c.SourceKind)
	}
	if callKind == "" {
		callKind = "unknown"
	}

	relPath := strings.TrimSpace(c.RelPath)
	funcName := strings.TrimSpace(c.Func)
	return fmt.Sprintf("%s|%s|%s@%d", relPath, funcName, callKind, c.LineStart)
}

func ensureDynamicSqlPseudo(tokens []ObjectToken, c *SqlCandidate, dml string) []ObjectToken {
	for _, tok := range tokens {
		if strings.EqualFold(strings.TrimSpace(tok.BaseName), "<dynamic-sql>") {
			return tokens
		}
	}
	if !strings.EqualFold(strings.TrimSpace(c.RawSql), "<dynamic-sql>") {
		return tokens
	}
	if strings.TrimSpace(dml) == "" {
		dml = "UNKNOWN"
	}
	role := "mixed"
	isWrite := c.IsWrite
	if strings.EqualFold(strings.TrimSpace(c.UsageKind), "EXEC") {
		role = "exec"
		dml = "EXEC"
		isWrite = true
	} else if strings.EqualFold(strings.TrimSpace(c.RawSql), "<dynamic-sql>") && strings.EqualFold(strings.TrimSpace(c.UsageKind), "EXEC") {
		role = "exec"
		dml = "EXEC"
		isWrite = true
	}
	tokens = append(tokens, ObjectToken{
		BaseName:           "<dynamic-sql>",
		FullName:           "<dynamic-sql>",
		DbName:             "",
		SchemaName:         "",
		Role:               role,
		DmlKind:            dml,
		IsWrite:            isWrite,
		IsObjectNameDyn:    false,
		IsPseudoObject:     true,
		PseudoKind:         "dynamic-sql",
		RepresentativeLine: c.LineStart,
	})
	return tokens
}

func dynamicObjectSpecificityScore(tok ObjectToken) int {
	score := 0
	if strings.TrimSpace(tok.DbName) != "" {
		score += 4
	}
	schema := strings.TrimSpace(tok.SchemaName)
	if schema != "" && !strings.EqualFold(schema, "dbo") {
		score += 2
	}
	if strings.TrimSpace(tok.Role) != "" {
		score++
	}
	if strings.TrimSpace(tok.DmlKind) != "" {
		score++
	}
	switch strings.ToLower(strings.TrimSpace(tok.PseudoKind)) {
	case "schema-placeholder", "table-placeholder":
		score += 6
	case "dynamic-object":
		score++
	}
	return score
}

func isDynamicSqlToken(tok ObjectToken) bool {
	if strings.EqualFold(strings.TrimSpace(tok.BaseName), "<dynamic-sql>") {
		return true
	}
	return strings.EqualFold(strings.TrimSpace(tok.PseudoKind), "dynamic-sql")
}

func isDynamicObjectPlaceholder(tok ObjectToken) bool {
	kind := strings.ToLower(strings.TrimSpace(tok.PseudoKind))
	if kind == "dynamic-sql" {
		return false
	}
	if kind == "dynamic-object" || kind == "schema-placeholder" || kind == "table-placeholder" {
		return true
	}
	base := strings.TrimSpace(tok.BaseName)
	if strings.EqualFold(base, "<dynamic-object>") {
		return true
	}
	if tok.IsObjectNameDyn && kind == "" {
		return true
	}
	return false
}

func buildDynamicObjectPseudo(usageKind string, line int) ObjectToken {
	tok := ObjectToken{
		BaseName:           "<dynamic-object>",
		FullName:           "<dynamic-object>",
		Role:               "source",
		DmlKind:            "SELECT",
		IsObjectNameDyn:    true,
		IsPseudoObject:     true,
		PseudoKind:         "dynamic-object",
		RepresentativeLine: line,
	}
	switch strings.ToUpper(strings.TrimSpace(usageKind)) {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE":
		tok.Role = "target"
		tok.DmlKind = strings.ToUpper(strings.TrimSpace(usageKind))
		tok.IsWrite = true
	case "EXEC":
		tok.Role = "exec"
		tok.DmlKind = "EXEC"
		tok.IsWrite = true
	}
	return tok
}

func condenseDynamicPseudoTokens(tokens []ObjectToken, allowDynamicSql bool) []ObjectToken {
	var dynObjs []ObjectToken
	var dynSqls []ObjectToken
	var out []ObjectToken

	for _, tok := range tokens {
		if isDynamicSqlToken(tok) {
			if allowDynamicSql {
				dynSqls = append(dynSqls, tok)
			}
			continue
		}
		if isDynamicObjectPlaceholder(tok) {
			dynObjs = append(dynObjs, tok)
			continue
		}
		out = append(out, tok)
	}

	if allowDynamicSql && len(dynSqls) > 0 {
		best := dynSqls[0]
		for _, tok := range dynSqls[1:] {
			if strings.TrimSpace(tok.DmlKind) != "" && strings.TrimSpace(best.DmlKind) == "" {
				best = tok
				continue
			}
			if strings.TrimSpace(tok.Role) != "" && strings.TrimSpace(best.Role) == "" {
				best = tok
			}
		}
		out = append(out, best)
	}

	if len(dynObjs) > 0 {
		best := dynObjs[0]
		bestScore := dynamicObjectSpecificityScore(best)
		for _, tok := range dynObjs[1:] {
			if score := dynamicObjectSpecificityScore(tok); score > bestScore {
				best = tok
				bestScore = score
			}
		}
		out = append(out, best)
	}

	return out
}

func hasDynamicPlaceholderToken(tokens []ObjectToken) bool {
	for _, tok := range tokens {
		if isDynamicSqlToken(tok) || isDynamicObjectPlaceholder(tok) {
			return true
		}
	}
	return false
}

func classifyObjects(c *SqlCandidate, usageKind string, tokens []ObjectToken) {
	allowDynamicSql := strings.EqualFold(strings.TrimSpace(c.RawSql), "<dynamic-sql>")
	if allowDynamicSql {
		tokens = ensureDynamicSqlPseudo(tokens, c, usageKind)
	}
	origTokens := tokens
	if c.IsDynamic && !hasDynamicPlaceholderToken(tokens) {
		fallback := inferDynamicObjectFallbacks(c.SqlClean, c.RawSql, usageKind, c.LineStart)
		if len(fallback) == 0 {
			fallback = []ObjectToken{buildDynamicObjectPseudo(usageKind, c.LineStart)}
		}
		tokens = append(tokens, fallback...)
	}
	tokens = condenseDynamicPseudoTokens(tokens, allowDynamicSql)
	if len(tokens) <= len(origTokens) {
		copy(origTokens, tokens)
		tokens = origTokens[:len(tokens)]
	}

	if c.IsDynamic && strings.TrimSpace(c.DynamicReason) == "" {
		c.DynamicReason = inferDynamicReason(c, tokens)
	}

	applyDynamicRewrite := strings.EqualFold(strings.TrimSpace(c.SourceKind), "csharp")
	sqlLower := strings.ToLower(c.SqlClean)
	for i := range tokens {
		if strings.TrimSpace(tokens[i].PseudoKind) != "" {
			tokens[i].IsPseudoObject = true
		}
		originalFull := tokens[i].FullName
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
		if hasDynamicPlaceholder(tokens[i].SchemaName) {
			tokens[i].IsPseudoObject = true
			if strings.TrimSpace(tokens[i].PseudoKind) == "" || strings.EqualFold(strings.TrimSpace(tokens[i].PseudoKind), "dynamic-object") {
				tokens[i].PseudoKind = "schema-placeholder"
			}
			tokens[i].SchemaName = ""
		}
		if hasDynamicPlaceholder(tokens[i].BaseName) {
			tokens[i].IsPseudoObject = true
			if strings.TrimSpace(tokens[i].PseudoKind) == "" || strings.EqualFold(strings.TrimSpace(tokens[i].PseudoKind), "dynamic-object") {
				tokens[i].PseudoKind = "table-placeholder"
			}
		}
		if strings.HasPrefix(strings.TrimSpace(tokens[i].BaseName), "#") {
			tokens[i].DbName = ""
			tokens[i].SchemaName = ""
			tokens[i].FullName = strings.TrimSpace(tokens[i].BaseName)
			tokens[i].IsPseudoObject = true
			tokens[i].IsObjectNameDyn = true
			if strings.TrimSpace(tokens[i].PseudoKind) == "" {
				tokens[i].PseudoKind = "temp-table"
			}
		}
		if strings.HasPrefix(strings.TrimSpace(tokens[i].BaseName), "@") {
			tokens[i].IsPseudoObject = true
			if strings.TrimSpace(tokens[i].PseudoKind) == "" {
				tokens[i].PseudoKind = "table-variable"
			}
		}
		schemaPlaceholder := strings.EqualFold(strings.TrimSpace(tokens[i].PseudoKind), "schema-placeholder")
		if tokens[i].IsObjectNameDyn {
			if applyDynamicRewrite {
				if hasDynamicPlaceholder(tokens[i].DbName) && !schemaPlaceholder {
					tokens[i].DbName = ""
				}
				if hasDynamicPlaceholder(tokens[i].SchemaName) {
					tokens[i].SchemaName = ""
				}
			}
			if !schemaPlaceholder {
				tokens[i].BaseName = "<dynamic-object>"
			}
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
		} else {
			tokens[i].PseudoKind = ""
		}

		rawFull := strings.TrimSpace(originalFull)
		if rawFull == "" {
			rawFull = buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName)
		}
		if strings.TrimSpace(tokens[i].PseudoKind) == "dynamic-object" {
			tokens[i].IsObjectNameDyn = true
		}
		if strings.TrimSpace(tokens[i].PseudoKind) != "" {
			tokens[i].IsPseudoObject = true
		} else {
			tokens[i].IsPseudoObject = false
		}
		tokens[i].IsObjectNameDyn = tokens[i].IsObjectNameDyn || (tokens[i].IsPseudoObject && tokens[i].PseudoKind == "dynamic-object")

		rawFull = strings.TrimSpace(rawFull)
		if rawFull == "" {
			rawFull = buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName)
		}

		if tokens[i].IsObjectNameDyn || hasDynamicPlaceholder(rawFull) {
			tokens[i].IsPseudoObject = true
			if tokens[i].PseudoKind == "" {
				tokens[i].PseudoKind = "dynamic-object"
			}
			tokens[i].IsObjectNameDyn = true
		}

		placeholderKind := strings.ToLower(strings.TrimSpace(tokens[i].PseudoKind))
		if placeholderKind == "schema-placeholder" || placeholderKind == "table-placeholder" {
			tokens[i].DbName = strings.TrimSpace(tokens[i].DbName)
			tokens[i].SchemaName = strings.TrimSpace(tokens[i].SchemaName)
			tokens[i].BaseName = cleanIdentifier(tokens[i].BaseName)
		} else {
			tokens[i].DbName = cleanIdentifier(tokens[i].DbName)
			tokens[i].SchemaName = cleanIdentifier(tokens[i].SchemaName)
			tokens[i].BaseName = cleanIdentifier(tokens[i].BaseName)
		}
		if tokens[i].IsPseudoObject {
			switch placeholderKind {
			case "schema-placeholder":
				tokens[i].SchemaName = ""
				tokens[i].FullName = normalizeFullObjectName(buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName))
			case "table-placeholder":
				tokens[i].FullName = normalizeFullObjectName(buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName))
			default:
				tokens[i].DbName = ""
				tokens[i].SchemaName = ""
				tokens[i].FullName = strings.TrimSpace(tokens[i].BaseName)
			}
		} else {
			if strings.TrimSpace(tokens[i].DbName) == "" && strings.TrimSpace(tokens[i].SchemaName) == "" {
				tokens[i].SchemaName = "dbo"
			}
			tokens[i].FullName = normalizeFullObjectName(buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName))
		}
	}
	preserveRole := make([]bool, len(tokens))
	// Determine cross-DB for each token first
	for i := range tokens {
		tokens[i].IsCrossDb = tokens[i].DbName != ""
		if tokens[i].RepresentativeLine == 0 {
			tokens[i].RepresentativeLine = c.LineStart
		}
		if tokens[i].IsPseudoObject && strings.EqualFold(strings.TrimSpace(tokens[i].BaseName), "<dynamic-sql>") {
			if strings.EqualFold(strings.TrimSpace(usageKind), "EXEC") {
				preserveRole[i] = true
				tokens[i].Role = "exec"
				tokens[i].DmlKind = "EXEC"
				tokens[i].IsWrite = true
				tokens[i].DbName = ""
				tokens[i].SchemaName = ""
				tokens[i].FullName = strings.TrimSpace(tokens[i].BaseName)
			} else {
				preserveRole[i] = false
				tokens[i].IsWrite = false
				tokens[i].Role = strings.TrimSpace(tokens[i].Role)
			}
			continue
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
	case "MERGE":
		for i := range tokens {
			if preserveRole[i] {
				continue
			}
			if i == targetIdx {
				tokens[i].Role = "target"
				tokens[i].DmlKind = "MERGE"
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
					dmlUpper := strings.ToUpper(strings.TrimSpace(tokens[i].DmlKind))
					if dmlUpper != "" && dmlUpper != "UNKNOWN" && dmlUpper != "INSERT" {
						continue
					}
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
	if usageKind == "MERGE" {
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
					tokens[i].DmlKind = "MERGE"
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

		// TRUNCATE statements should stay write-target only even if a generic scan
		// token also marked the same object as a source.
		if _, ok := g.dmlSet["TRUNCATE"]; ok {
			delete(g.dmlSet, "SELECT")
			g.hasSource = false
		}

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
	case "MERGE":
		if pos := strings.Index(sqlLower, "merge into"); pos >= 0 {
			return pos + len("merge into")
		}
		return strings.Index(sqlLower, "merge")
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

func findCrossDbAnchor(sqlLower, usageKind string) int {
	if pos := findKeywordPosition(sqlLower, usageKind); pos >= 0 {
		return pos
	}
	keywords := []string{"from", "join", "into", "update", "delete", "exec", "execute"}
	best := -1
	for _, kw := range keywords {
		if pos := strings.Index(sqlLower, kw); pos >= 0 {
			if best == -1 || pos < best {
				best = pos
			}
		}
	}
	return best
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

func cleanIdentifier(raw string) string {
	return strings.Trim(strings.TrimSpace(raw), "[]\"")
}

func normalizeFullObjectName(name string) string {
	trimmed := strings.TrimSpace(name)
	if trimmed == "" {
		return ""
	}
	parts := strings.Split(trimmed, ".")
	cleaned := make([]string, 0, len(parts))
	for _, p := range parts {
		segment := cleanIdentifier(p)
		if segment == "" {
			continue
		}
		cleaned = append(cleaned, segment)
	}
	return strings.Join(cleaned, ".")
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

		dbName, schemaName, baseName, isLinked := splitObjectNameParts(objText)
		normalized := normalizeFullObjectName(objText)
		parts := strings.Split(normalized, ".")
		if dbName == "" && len(parts) >= 2 && len(parts) <= 3 {
			dbName = strings.TrimSpace(parts[0])
			if len(parts) > 1 {
				schemaName = strings.TrimSpace(parts[1])
			}
			if schemaName == "" {
				schemaName = "dbo"
			}
		}
		baseName = strings.TrimSpace(baseName)
		if baseName == "" {
			baseName = "<dynamic-object>"
		}
		prefixPlaceholder := hasDynamicPlaceholder(dbName) || hasDynamicPlaceholder(schemaName)
		basePlaceholder := hasDynamicPlaceholder(baseName)
		pseudoKind := "dynamic-object"
		switch {
		case prefixPlaceholder && !basePlaceholder:
			pseudoKind = "schema-placeholder"
		case basePlaceholder && !prefixPlaceholder:
			pseudoKind = "table-placeholder"
		}
		if pseudoKind != "schema-placeholder" {
			baseName = "<dynamic-object>"
		}
		if pseudoKind == "schema-placeholder" {
			schemaName = ""
		}
		full := buildFullName(dbName, schemaName, baseName)

		tok := ObjectToken{
			DbName:             dbName,
			SchemaName:         schemaName,
			BaseName:           baseName,
			FullName:           full,
			IsObjectNameDyn:    true,
			IsPseudoObject:     true,
			PseudoKind:         pseudoKind,
			RepresentativeLine: line,
			IsLinkedServer:     isLinked,
		}

		if tok.PseudoKind == "schema-placeholder" {
			tok.SchemaName = ""
		}

		tok = normalizeObjectToken(tok)
		if tok.PseudoKind == "schema-placeholder" {
			tok.SchemaName = ""
			tok.FullName = buildFullName(tok.DbName, tok.SchemaName, tok.BaseName)
		}

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

		if tok.DbName == "" && strings.Contains(objText, ".") && isDynamicObjectName(objText) {
			prefix := objText[:strings.Index(objText, ".")]
			prefix = strings.TrimSpace(prefix)
			prefix = strings.Trim(prefix, "[]")
			prefix = strings.Trim(prefix, `"`)
			if prefix != "" {
				tok.DbName = prefix
				if strings.EqualFold(strings.TrimSpace(tok.SchemaName), strings.TrimSpace(prefix)) {
					tok.SchemaName = ""
				}
				tok.FullName = buildFullName(tok.DbName, tok.SchemaName, tok.BaseName)
				tok.IsCrossDb = true
			}
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

	if strings.Contains(sqlClean, "[[") || strings.Contains(sqlClean, "]]") {
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
		case "schema-placeholder", "table-placeholder":
			return 2
		case "dynamic-object":
			return 1
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

	sort.SliceStable(cands, func(i, j int) bool {
		a, b := cands[i], cands[j]
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.LineStart != b.LineStart {
			return a.LineStart < b.LineStart
		}
		if a.LineEnd != b.LineEnd {
			return a.LineEnd < b.LineEnd
		}
		if a.Func != b.Func {
			return a.Func < b.Func
		}
		if a.UsageKind != b.UsageKind {
			return a.UsageKind < b.UsageKind
		}
		if a.SqlClean != b.SqlClean {
			return a.SqlClean < b.SqlClean
		}
		if a.RawSql != b.RawSql {
			return a.RawSql < b.RawSql
		}
		return a.SourceKind < b.SourceKind
	})

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
