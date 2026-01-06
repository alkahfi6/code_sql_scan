package scan

import (
	"crypto/sha1"
	"crypto/sha256"
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
	nakedTokens := detectNakedObjectTokens(sqlClean, usage, c.LineStart, tokens)
	if len(nakedTokens) > 0 {
		tokens = append(tokens, nakedTokens...)
		for _, tok := range nakedTokens {
			if tok.Role == "target" || strings.EqualFold(tok.DmlKind, "EXEC") || tok.IsWrite {
				dmlTokens = append(dmlTokens, tok)
			}
		}
	}
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
	tokens = append(tokens, detectClauseRoleTokens(sqlClean, usage, c.LineStart)...)

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

	if c.IsWrite && !hasTargetOrExecObject(c.Objects) {
		c.Objects = append(c.Objects, ObjectToken{
			BaseName:           "<missing-target>",
			FullName:           "<missing-target>",
			Role:               "target",
			DmlKind:            strings.ToUpper(strings.TrimSpace(c.UsageKind)),
			IsWrite:            true,
			IsPseudoObject:     true,
			PseudoKind:         "missing-target",
			RepresentativeLine: c.LineStart,
		})
	}

	updateCrossDbMetadata(c)

	c.QueryHash = computeQueryHash(c.SqlClean, c.RawSql)
	c.QueryHashStrong = computeQueryHashV2(c.SqlClean, c.RawSql)

	c.RiskLevel = classifyRisk(c)
}

func computeQueryHash(sqlClean, rawSql string) string {
	// Hash must remain content-derived only (no metadata such as file path or function name).
	hashInput := sqlClean
	if hashInput == "" {
		hashInput = rawSql
	}
	h := sha1.Sum([]byte(hashInput))
	return fmt.Sprintf("%x", h[:])
}

// computeQueryHashV2 provides a stronger SHA-256 hash for internal use cases that
// still require a stable, content-only fingerprint. The primary QueryHash exposed
// in CSV outputs intentionally remains SHA-1 for compatibility.
func computeQueryHashV2(sqlClean, rawSql string) string {
	hashInput := sqlClean
	if hashInput == "" {
		hashInput = rawSql
	}
	h := sha256.Sum256([]byte(hashInput))
	return fmt.Sprintf("%x", h[:])
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

func detectNakedObjectTokens(sql string, usageKind string, line int, existing []ObjectToken) []ObjectToken {
	lower := strings.ToLower(sql)
	usageUpper := strings.ToUpper(strings.TrimSpace(usageKind))
	existingKeys := make(map[string]struct{})
	for _, tok := range existing {
		if key := objectIdentityKey(tok.DbName, tok.SchemaName, tok.BaseName); key != "" {
			existingKeys[key] = struct{}{}
		}
		if key := objectIdentityKey("", "", tok.FullName); key != "" {
			existingKeys[key] = struct{}{}
		}
	}

	type rule struct {
		keyword      string
		role         string
		dml          string
		requireUsage []string
	}

	matchesUsage := func(req []string) bool {
		if len(req) == 0 {
			return true
		}
		for _, r := range req {
			if strings.EqualFold(strings.TrimSpace(r), usageUpper) {
				return true
			}
		}
		return false
	}

	rules := []rule{
		{keyword: "update", role: "target", dml: "UPDATE", requireUsage: []string{"UPDATE"}},
		{keyword: "delete from", role: "target", dml: "DELETE", requireUsage: []string{"DELETE"}},
		{keyword: "insert into", role: "target", dml: "INSERT", requireUsage: []string{"INSERT"}},
		{keyword: "truncate table", role: "target", dml: "TRUNCATE", requireUsage: []string{"TRUNCATE"}},
		{keyword: "from", role: "source"},
		{keyword: "join", role: "source"},
		{keyword: "exec", role: "exec", dml: "EXEC"},
		{keyword: "execute", role: "exec", dml: "EXEC"},
	}

	var tokens []ObjectToken

	for _, rule := range rules {
		if !matchesUsage(rule.requireUsage) {
			continue
		}
		start := 0
		kw := strings.ToLower(rule.keyword)
		for {
			idx := strings.Index(lower[start:], kw)
			if idx < 0 {
				break
			}
			pos := start + idx
			end := pos + len(kw)
			if pos > 0 && isIdentChar(lower[pos-1]) {
				start = end
				continue
			}
			if end < len(lower) && isIdentChar(lower[end]) {
				start = end
				continue
			}

			p := skipWS(lower, end)
			if p >= len(lower) {
				break
			}

			objText, next := scanObjectName(sql, lower, p)
			if hasDynamicPlaceholder(objText) {
				start = next
				continue
			}
			name := cleanNakedIdentifier(objText)
			if name == "" {
				start = next
				continue
			}
			dbName, schemaName, baseName, isLinked := splitObjectNameParts(name)
			if baseName == "" || isSqlKeyword(baseName) {
				start = next
				continue
			}
			if schemaName == "" && !strings.HasPrefix(strings.TrimSpace(baseName), "#") && !strings.HasPrefix(strings.TrimSpace(baseName), "@") {
				schemaName = "dbo"
			}

			dml := resolveNakedDmlKind(rule.dml, usageUpper)
			isWrite := false
			if rule.role == "target" || rule.role == "exec" {
				isWrite = true
			} else if rule.role == "" {
				isWrite = isWriteKind(dml)
			}
			tok := ObjectToken{
				DbName:             dbName,
				SchemaName:         schemaName,
				BaseName:           baseName,
				FullName:           buildFullName(dbName, schemaName, baseName),
				Role:               rule.role,
				DmlKind:            dml,
				IsWrite:            isWrite,
				RepresentativeLine: line,
				FoundAt:            pos,
				IsLinkedServer:     isLinked,
			}

			tok = normalizeObjectToken(tok)
			key := objectIdentityKey(tok.DbName, tok.SchemaName, tok.BaseName)
			if key == "" {
				key = objectIdentityKey("", "", tok.FullName)
			}
			if key == "" {
				start = next
				continue
			}
			if _, ok := existingKeys[key]; ok {
				start = next
				continue
			}
			existingKeys[key] = struct{}{}
			tokens = append(tokens, tok)
			start = next
		}
	}

	return tokens
}

func cleanNakedIdentifier(raw string) string {
	raw = strings.TrimSpace(raw)
	if raw == "" {
		return ""
	}
	raw = strings.TrimLeft(raw, "(")
	raw = strings.TrimRight(raw, ",;)")
	if idx := strings.IndexAny(raw, " \t\r\n"); idx >= 0 {
		raw = raw[:idx]
	}
	raw = strings.Trim(raw, "[]\"")
	raw = strings.TrimSpace(raw)
	return raw
}

func objectIdentityKey(db, schema, base string) string {
	full := strings.TrimSpace(buildFullName(db, schema, base))
	if full == "" {
		full = strings.TrimSpace(base)
	}
	full = strings.Trim(full, ".")
	full = strings.TrimSpace(full)
	if full == "" {
		return ""
	}
	return strings.ToLower(full)
}

func resolveNakedDmlKind(ruleKind, usageKind string) string {
	kind := strings.ToUpper(strings.TrimSpace(ruleKind))
	if kind != "" {
		return kind
	}
	if usageKind != "" && usageKind != "UNKNOWN" {
		return usageKind
	}
	return "SELECT"
}

func isSqlKeyword(tok string) bool {
	switch strings.ToLower(strings.TrimSpace(tok)) {
	case "", "from", "join", "on", "where", "group", "order", "by", "inner", "left", "right", "full", "cross", "and", "or", "as", "set", "values", "into", "delete", "update", "insert", "exec", "execute", "top", "distinct", "truncate", "table", "using", "merge", "with":
		return true
	default:
		return false
	}
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
		if hasDynamicPlaceholder(rawName) {
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

func findKeywordWithBoundary(sqlLower, keyword string, start int) int {
	if start < 0 {
		start = 0
	}
	for {
		idx := strings.Index(sqlLower[start:], keyword)
		if idx < 0 {
			return -1
		}
		pos := start + idx
		end := pos + len(keyword)
		if pos > 0 && isIdentChar(sqlLower[pos-1]) {
			start = end
			continue
		}
		if end < len(sqlLower) && isIdentChar(sqlLower[end]) {
			start = end
			continue
		}
		return pos
	}
}

func buildClauseObjectToken(rawName, role, dml string, isWrite bool, line, foundAt int) (ObjectToken, bool) {
	rawName = strings.TrimSpace(rawName)
	if rawName == "" {
		return ObjectToken{}, false
	}
	dbName, schemaName, baseName, isLinked := splitObjectNameParts(rawName)
	if baseName == "" {
		return ObjectToken{}, false
	}
	trimmedBase := strings.TrimSpace(baseName)
	if schemaName == "" && !strings.HasPrefix(trimmedBase, "#") && !strings.HasPrefix(trimmedBase, "@") {
		schemaName = "dbo"
	}
	tok := ObjectToken{
		DbName:             dbName,
		SchemaName:         schemaName,
		BaseName:           baseName,
		FullName:           buildFullName(dbName, schemaName, baseName),
		Role:               role,
		DmlKind:            strings.ToUpper(strings.TrimSpace(dml)),
		IsWrite:            isWrite,
		FoundAt:            foundAt,
		RepresentativeLine: line,
		IsObjectNameDyn:    hasDynamicPlaceholder(rawName),
		IsLinkedServer:     isLinked,
	}

	tok = normalizeObjectToken(tok)
	return tok, true
}

func collectClauseObjects(sql, sqlLower string, start int, keywords []string, role, dml string, isWrite bool, line int) []ObjectToken {
	var tokens []ObjectToken
	for _, kw := range keywords {
		searchPos := start
		for searchPos >= 0 && searchPos < len(sqlLower) {
			idx := strings.Index(sqlLower[searchPos:], kw)
			if idx < 0 {
				break
			}
			pos := searchPos + idx
			end := pos + len(kw)
			if pos > 0 && isIdentChar(sqlLower[pos-1]) {
				searchPos = end
				continue
			}
			if end < len(sqlLower) && isIdentChar(sqlLower[end]) {
				searchPos = end
				continue
			}
			objStart := skipWS(sqlLower, end)
			if objStart >= len(sqlLower) {
				break
			}
			rawObj, next := scanObjectName(sql, sqlLower, objStart)
			if tok, ok := buildClauseObjectToken(rawObj, role, dml, isWrite, line, pos); ok {
				tokens = append(tokens, tok)
			}
			if next <= searchPos {
				next = end
			}
			searchPos = next
		}
	}
	return tokens
}

func detectOutputTargets(sql, sqlLower, usage string, line int) []ObjectToken {
	keywords := []string{"output", "returning"}
	var tokens []ObjectToken
	for _, kw := range keywords {
		searchPos := 0
		for searchPos < len(sqlLower) {
			idx := strings.Index(sqlLower[searchPos:], kw)
			if idx < 0 {
				break
			}
			pos := searchPos + idx
			end := pos + len(kw)
			if pos > 0 && isIdentChar(sqlLower[pos-1]) {
				searchPos = end
				continue
			}
			if end < len(sqlLower) && isIdentChar(sqlLower[end]) {
				searchPos = end
				continue
			}
			intoPos := findKeywordWithBoundary(sqlLower, "into", end)
			if intoPos < 0 {
				searchPos = end
				continue
			}
			objStart := skipWS(sqlLower, intoPos+len("into"))
			if objStart >= len(sqlLower) {
				break
			}
			rawObj, next := scanObjectName(sql, sqlLower, objStart)
			if tok, ok := buildClauseObjectToken(rawObj, "target", usage, true, line, intoPos); ok {
				tokens = append(tokens, tok)
			}
			if next <= searchPos {
				next = end
			}
			searchPos = next
		}
	}
	return tokens
}

func detectClauseRoleTokens(sql, usage string, line int) []ObjectToken {
	sqlLower := strings.ToLower(sql)
	usageUpper := strings.ToUpper(strings.TrimSpace(usage))
	var tokens []ObjectToken
	switch usageUpper {
	case "INSERT":
		tokens = append(tokens, detectDmlTargetsFromSql(sql, usageUpper, line)...)
		selectPos := findKeywordWithBoundary(sqlLower, "select", 0)
		if selectPos >= 0 {
			tokens = append(tokens, collectClauseObjects(sql, sqlLower, selectPos, []string{"from", "join", "using"}, "source", "SELECT", false, line)...)
		}
	case "UPDATE":
		tokens = append(tokens, detectDmlTargetsFromSql(sql, usageUpper, line)...)
		fromPos := findKeywordWithBoundary(sqlLower, "from", findKeywordWithBoundary(sqlLower, "set", 0))
		if fromPos >= 0 {
			tokens = append(tokens, collectClauseObjects(sql, sqlLower, fromPos, []string{"from", "join", "using"}, "source", "SELECT", false, line)...)
		}
	case "MERGE":
		tokens = append(tokens, detectDmlTargetsFromSql(sql, usageUpper, line)...)
		usingPos := findKeywordWithBoundary(sqlLower, "using", 0)
		if usingPos >= 0 {
			tokens = append(tokens, collectClauseObjects(sql, sqlLower, usingPos, []string{"using", "join"}, "source", "SELECT", false, line)...)
		}
	}

	if usageUpper == "INSERT" || usageUpper == "UPDATE" || usageUpper == "DELETE" || usageUpper == "MERGE" {
		tokens = append(tokens, detectOutputTargets(sql, sqlLower, usageUpper, line)...)
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
		tok.BaseName = "<dynamic-sql>"
		tok.FullName = "<dynamic-sql>"
		tok.PseudoKind = "dynamic-sql"
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

func hasDynamicSqlToken(tokens []ObjectToken) bool {
	for _, tok := range tokens {
		if isDynamicSqlToken(tok) {
			return true
		}
	}
	return false
}

func classifyObjects(c *SqlCandidate, usageKind string, tokens []ObjectToken) {
	allowDynamicSql := strings.EqualFold(strings.TrimSpace(c.RawSql), "<dynamic-sql>")
	if c.IsDynamic {
		allowDynamicSql = true
	}
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
	if c.IsDynamic && strings.EqualFold(strings.TrimSpace(usageKind), "EXEC") && !hasDynamicSqlToken(tokens) {
		tokens = append(tokens, ObjectToken{
			BaseName:           "<dynamic-sql>",
			FullName:           "<dynamic-sql>",
			Role:               "exec",
			DmlKind:            "EXEC",
			IsWrite:            true,
			IsPseudoObject:     true,
			PseudoKind:         "dynamic-sql",
			RepresentativeLine: c.LineStart,
		})
	}
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
	c.Objects = mergeObjectRoles(tokens, usageKind)
}

func mergeObjectRoles(tokens []ObjectToken, usageKind string) []ObjectToken {
	if len(tokens) <= 1 {
		for i := range tokens {
			if tokens[i].SchemaName == "" && tokens[i].BaseName != "" && !tokens[i].IsPseudoObject {
				tokens[i].SchemaName = "dbo"
				tokens[i].FullName = buildFullName(tokens[i].DbName, tokens[i].SchemaName, tokens[i].BaseName)
			}
		}
		return tokens
	}
	usageUpper := strings.ToUpper(strings.TrimSpace(usageKind))

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
			role = "exec"
		} else if g.hasTarget {
			role = "target"
		}
		if g.hasTarget && !g.hasExec {
			role = "target"
		}
		tok.Role = role
		tok.IsWrite = g.hasWrite
		if g.hasTarget {
			delete(g.dmlSet, "SELECT")
		}
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
		if tok.Role == "source" && usageUpper != "" && tok.DmlKind != usageUpper {
			if _, ok := g.dmlSet[usageUpper]; ok {
				tok.DmlKind = usageUpper
			}
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
	re := regexp.MustCompile(`(?is)(insert(?:\s+into)?|update|delete\s+from|truncate\s+table|merge\s+into|from|join)\s+([^\s;]+)`)
	placeholderObjRe := regexp.MustCompile(`(?i)(?:[A-Za-z0-9_\[\]"#]+\s*\.\s*)*\[\[[^\]]+\]\](?:\s*\.\s*[A-Za-z0-9_\[\]"#]+)*`)
	usageUpper := strings.ToUpper(strings.TrimSpace(usage))

	addToken := func(objText string, keyword string) {
		objText = strings.TrimSpace(objText)
		if objText == "" {
			return
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
		if prefixPlaceholder && basePlaceholder {
			if hasDynamicPlaceholder(dbName) {
				dbName = ""
			}
			if dbName != "" {
				schemaName = "dbo"
			} else if hasDynamicPlaceholder(schemaName) {
				schemaName = ""
			}
			baseName = "<dynamic-object>"
		} else {
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
		switch keyword {
		case "insert", "insert into":
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
		case "merge into":
			tok.Role = "target"
			tok.DmlKind = "MERGE"
			tok.IsWrite = true
		default:
			if usageUpper == "INSERT" || usageUpper == "UPDATE" || usageUpper == "DELETE" || usageUpper == "TRUNCATE" || usageUpper == "MERGE" {
				tok.Role = "target"
				tok.DmlKind = usageUpper
				tok.IsWrite = true
			} else {
				tok.Role = "source"
				tok.DmlKind = "SELECT"
			}
		}

		if tok.DbName != "" {
			tok.IsCrossDb = true
		}
		tok = normalizeObjectToken(tok)
		tokens = append(tokens, tok)
	}

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
		addToken(objText, keyword)
	}

	for _, m := range placeholderObjRe.FindAllString(cleaned, -1) {
		addToken(m, usageUpper)
	}

	if len(tokens) == 0 && strings.Contains(cleaned, "<expr>") {
		tokens = append(tokens, buildDynamicObjectPseudo(usage, line))
	}

	return condenseDynamicPseudoTokens(tokens, true)
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
		if hasDynamicPlaceholder(objText) {
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

	isPseudoObj := func(o ObjectToken) bool {
		if o.IsPseudoObject || strings.TrimSpace(o.PseudoKind) != "" {
			return true
		}
		return isDynamicBaseName(o.BaseName)
	}

	seen := make(map[string]bool)
	var uniq []ObjectToken
	for _, o := range c.Objects {
		full := o.FullName
		if full == "" {
			full = buildFullName(o.DbName, o.SchemaName, o.BaseName)
		}
		pseudoKind := strings.ToLower(strings.TrimSpace(o.PseudoKind))
		if pseudoKind == "" && isDynamicBaseName(o.BaseName) {
			pseudoKind = "dynamic-sql"
		}
		baseKey := fmt.Sprintf("%s|%s|%d|%s|%s|%s", c.QueryHash, c.RelPath, o.RepresentativeLine, strings.ToLower(full), o.Role, o.DmlKind)
		if isPseudoObj(o) {
			baseKey = fmt.Sprintf("%s|%s|%s", c.QueryHash, pseudoKind, baseKey)
		}
		key := baseKey
		if seen[key] {
			continue
		}
		seen[key] = true
		uniq = append(uniq, o)
	}
	c.Objects = uniq
}

func hasTargetOrExecObject(objs []ObjectToken) bool {
	for _, o := range objs {
		role := strings.ToLower(strings.TrimSpace(o.Role))
		if role == "target" || role == "exec" || strings.EqualFold(o.DmlKind, "EXEC") {
			return true
		}
	}
	return false
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

const pseudoCardinalityWarningThreshold = 100

type pseudoOffender struct {
	funcKey   string
	count     int
	kindCount map[string]int
}

func pseudoObjectKey(c SqlCandidate, o ObjectToken) (string, string, string, bool) {
	isPseudo := o.IsPseudoObject || strings.TrimSpace(o.PseudoKind) != "" || isDynamicBaseName(o.BaseName)
	if !isPseudo {
		return "", "", "", false
	}
	kind := strings.ToLower(strings.TrimSpace(o.PseudoKind))
	if kind == "" && isDynamicBaseName(o.BaseName) {
		kind = "dynamic-sql"
	}
	funcKey := fmt.Sprintf("%s|%s", strings.TrimSpace(c.RelPath), strings.TrimSpace(c.Func))
	full := o.FullName
	if full == "" {
		full = buildFullName(o.DbName, o.SchemaName, o.BaseName)
	}
	key := strings.Join([]string{
		strings.TrimSpace(c.QueryHash),
		kind,
		strings.ToLower(full),
		strings.ToLower(o.Role),
		strings.ToLower(o.DmlKind),
		fmt.Sprintf("%d", o.RepresentativeLine),
	}, "|")
	return funcKey, kind, key, key != "" && isPseudo
}

func logPseudoCardinalityWarnings(cands []SqlCandidate) {
	logPseudoCardinalityWarningsWithThreshold(cands, pseudoCardinalityWarningThreshold, false)
}

func logPseudoCardinalityWarningsWithThreshold(cands []SqlCandidate, threshold int, fail bool) []pseudoOffender {
	if len(cands) == 0 {
		return nil
	}
	seen := make(map[string]map[string]struct{})
	kindCounts := make(map[string]map[string]int)
	for _, c := range cands {
		for _, o := range c.Objects {
			funcKey, kind, pseudoKey, ok := pseudoObjectKey(c, o)
			if !ok {
				continue
			}
			if _, ok := seen[funcKey]; !ok {
				seen[funcKey] = make(map[string]struct{})
			}
			if _, ok := seen[funcKey][pseudoKey]; ok {
				continue
			}
			seen[funcKey][pseudoKey] = struct{}{}
			if _, ok := kindCounts[funcKey]; !ok {
				kindCounts[funcKey] = make(map[string]int)
			}
			kindCounts[funcKey][kind]++
		}
	}

	if len(seen) == 0 {
		return nil
	}
	funcKeys := make([]string, 0, len(seen))
	for k := range seen {
		funcKeys = append(funcKeys, k)
	}
	sort.Strings(funcKeys)
	var offenders []pseudoOffender
	for _, funcKey := range funcKeys {
		count := len(seen[funcKey])
		if count <= threshold {
			continue
		}
		parts := strings.SplitN(funcKey, "|", 2)
		rel := ""
		fn := ""
		if len(parts) >= 1 {
			rel = parts[0]
		}
		if len(parts) >= 2 {
			fn = parts[1]
		}
		kinds := kindCounts[funcKey]
		kindList := make([]string, 0, len(kinds))
		for kind, v := range kinds {
			kindList = append(kindList, fmt.Sprintf("%s=%d", kind, v))
		}
		sort.Strings(kindList)
		logWarnf("[WARN] high pseudo-object cardinality rel=%s func=%s unique=%d pseudoKinds=%s", rel, fn, count, strings.Join(kindList, "; "))
		offenders = append(offenders, pseudoOffender{
			funcKey:   funcKey,
			count:     count,
			kindCount: kinds,
		})
	}

	sort.Slice(offenders, func(i, j int) bool {
		if offenders[i].count != offenders[j].count {
			return offenders[i].count > offenders[j].count
		}
		return offenders[i].funcKey < offenders[j].funcKey
	})

	if fail && len(offenders) > 10 {
		offenders = offenders[:10]
	}
	return offenders
}
