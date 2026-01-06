package scan

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strings"
	"unicode"
)

// ------------------------------------------------------------
// C# extractor (regex-based)
// ------------------------------------------------------------

func scanCsFile(cfg *Config, path, relPath string) ([]SqlCandidate, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("stage=read-cs lang=%s root=%q file=%q err=%w", cfg.Lang, cfg.Root, path, err)
	}
	src := string(data)
	clean := stripCSComments(src)

	lines := strings.Split(src, "\n")
	methodRanges := detectCsMethods(lines)
	methodAtLine := indexCsMethodLines(methodRanges, len(lines))
	sequentialMethodAtLine := buildSequentialMethodIndex(lines)
	fallbackMethod := func(line int) *methodRange {
		if line < 1 {
			line = 1
		}

		const (
			maxSearch        = 300
			maxBraceLookback = 100
		)
		startIdx := line - 1
		if startIdx >= len(lines) {
			startIdx = len(lines) - 1
		}

		tryExtract := func(idx int) string {
			trimmed := strings.TrimSpace(lines[idx])
			if trimmed == "" {
				return ""
			}
			candidates := []string{trimmed}
			if idx > 0 {
				candidates = append(candidates, strings.TrimSpace(lines[idx-1]+" "+trimmed))
			}
			if idx > 1 {
				candidates = append(candidates, strings.TrimSpace(lines[idx-2]+" "+lines[idx-1]+" "+trimmed))
			}
			for _, cand := range candidates {
				if cand == "" {
					continue
				}
				if name := extractCsMethodName(cand); name != "" {
					return name
				}
			}
			return ""
		}

		for lookback := 0; lookback <= maxBraceLookback && startIdx-lookback >= 0; lookback++ {
			idx := startIdx - lookback
			if name := tryExtract(idx); name != "" {
				start := idx + 1
				end := line
				if end < start {
					end = start
				}
				return &methodRange{Name: name, Start: start, End: end}
			}
		}

		for i := startIdx; i >= 0 && startIdx-i <= maxSearch; i-- {
			trimmed := strings.TrimSpace(lines[i])
			if !strings.Contains(trimmed, "{") {
				continue
			}
			if name := tryExtract(i); name != "" {
				start := i + 1
				end := line
				if end < start {
					end = start
				}
				return &methodRange{Name: name, Start: start, End: end}
			}
		}

		braceIdx := -1
		for i := startIdx; i >= 0 && startIdx-i <= maxSearch; i-- {
			if strings.Contains(lines[i], "{") {
				braceIdx = i
				break
			}
		}
		if braceIdx >= 0 {
			for i := braceIdx; i >= 0 && braceIdx-i <= maxBraceLookback; i-- {
				if name := tryExtract(i); name != "" {
					start := i + 1
					end := line
					if end < start {
						end = start
					}
					return &methodRange{Name: name, Start: start, End: end}
				}
			}
		}

		var candidate *methodRange
		bestDist := maxSearch + 1
		for i := range methodRanges {
			mr := &methodRanges[i]
			if line >= mr.Start && line <= mr.End {
				return mr
			}
			dist := 0
			if line < mr.Start {
				dist = mr.Start - line
			} else {
				dist = line - mr.End
			}
			if dist <= maxSearch && dist < bestDist {
				candidate = mr
				bestDist = dist
			}
		}
		return candidate
	}

	// Track simple string assignments per method (e.g., var cmd = "dbo.MyProc";)
	literalInMethod := make(map[string]map[string]SqlSymbol)
	globalLiterals := make(map[string]SqlSymbol)
	recordLiteral := func(method, name, val string, line int, isDyn bool) {
		if name == "" {
			return
		}
		target := globalLiterals
		if method != "" {
			if _, ok := literalInMethod[method]; !ok {
				literalInMethod[method] = make(map[string]SqlSymbol)
			}
			target = literalInMethod[method]
		}
		target[name] = SqlSymbol{
			Name:       name,
			Value:      val,
			RelPath:    relPath,
			Line:       line,
			IsComplete: true,
			IsDynamic:  isDyn,
		}
	}

	lookupLiteral := func(method, name string) (SqlSymbol, bool) {
		if method != "" {
			if vals, ok := literalInMethod[method]; ok {
				if val, ok := vals[name]; ok {
					return val, true
				}
			}
		}
		if val, ok := globalLiterals[name]; ok {
			return val, true
		}
		return SqlSymbol{}, false
	}

	lookupLiteralAnyMethod := func(name string) (SqlSymbol, bool) {
		if val, ok := globalLiterals[name]; ok {
			return val, true
		}
		methods := make([]string, 0, len(literalInMethod))
		for m := range literalInMethod {
			methods = append(methods, m)
		}
		sort.Strings(methods)
		for _, method := range methods {
			if val, ok := literalInMethod[method][name]; ok {
				return val, true
			}
		}
		return SqlSymbol{}, false
	}
	for _, m := range regexes.verbatimAssign.FindAllStringSubmatchIndex(clean, -1) {
		line := countLinesUpTo(clean, m[0])
		funcName := ""
		if line-1 >= 0 && line-1 < len(methodAtLine) {
			if methodAtLine[line-1] != nil {
				funcName = methodAtLine[line-1].Name
			}
		}
		if funcName == "" && line-1 >= 0 && line-1 < len(sequentialMethodAtLine) {
			if sequentialMethodAtLine[line-1] != nil {
				funcName = sequentialMethodAtLine[line-1].Name
			}
		}
		if funcName == "" {
			if mr := fallbackMethod(line); mr != nil {
				funcName = mr.Name
			}
		}
		name := cleanedGroup(clean, m, 1)
		val := decodeCSharpLiteralContent(cleanedGroup(clean, m, 2), true)
		recordLiteral(funcName, name, val, line, false)
	}
	for i, line := range lines {
		method := ""
		if i < len(methodAtLine) && methodAtLine[i] != nil {
			method = methodAtLine[i].Name
		}
		if method == "" && i < len(sequentialMethodAtLine) && sequentialMethodAtLine[i] != nil {
			method = sequentialMethodAtLine[i].Name
		}
		if method == "" {
			if mr := fallbackMethod(i + 1); mr != nil {
				method = mr.Name
			}
		}
		if m := regexes.simpleDeclAssign.FindStringSubmatch(line); len(m) == 3 {
			recordLiteral(method, m[1], decodeCSharpLiteralContent(m[2], false), i+1, false)
			continue
		}
		if strings.Contains(line, "==") || strings.Contains(line, "!=") || strings.Contains(line, "+=") || strings.Contains(line, "-=") || strings.Contains(line, "*=") || strings.Contains(line, "/=") {
			continue
		}
		if m := regexes.bareAssign.FindStringSubmatch(line); len(m) == 3 {
			recordLiteral(method, m[1], decodeCSharpLiteralContent(m[2], false), i+1, false)
			continue
		}
		if name, expr, ok := parseCsBinaryAssignment(line); ok {
			if val, dyn, merged := mergeCSharpBinaryConcat(expr, func(id string) (SqlSymbol, bool) {
				if sym, ok := lookupLiteral(method, id); ok {
					return sym, true
				}
				if sym, ok := lookupLiteralAnyMethod(id); ok {
					return sym, true
				}
				return SqlSymbol{}, false
			}); merged {
				recordLiteral(method, name, val, i+1, dyn)
			}
		}
	}

	var cands []SqlCandidate

	type pat struct {
		re           *regexp.Regexp
		execStub     bool
		dynamic      bool
		sqlArgIndex  int
		callSiteKind string
	}
	patterns := []pat{
		{regexes.execProcLit, true, false, 0, "ExecProc"},
		{regexes.execProcDyn, true, true, 0, "ExecProc"},
		{regexes.newCmd, false, false, 0, "SqlCommand"},
		{regexes.newCmdIdent, false, false, 0, "SqlCommand"},
		{regexes.efExecRawIdent, false, false, 0, "CommandText"},
		{regexes.dapperQuery, false, false, 0, "CommandText"},
		{regexes.dapperExec, false, false, 0, "CommandText"},
		{regexes.efFromSql, false, false, 0, "CommandText"},
		{regexes.efExecRaw, false, false, 0, "CommandText"},
		{regexes.execQuery, false, false, 1, "CommandText"},       // ExecuteQuery(conn, "SQL")
		{regexes.execQueryIdent, false, false, 1, "CommandText"},  // ExecuteQuery(conn, variable)
		{regexes.byQueryCall, false, false, 1, "CommandText"},     // InsertXByQuery(data, sql)
		{regexes.callQueryWsLit, false, false, 2, "CommandText"},  // CallQueryFromWs with literal SQL
		{regexes.callQueryWsDyn, false, true, 2, "CommandText"},   // CallQueryFromWs with dynamic SQL expression
		{regexes.commandTextLit, false, false, -1, "CommandText"}, // CommandText = "ProcName"
		{regexes.commandTextIdent, false, false, -1, "CommandText"},
	}

	unquoteIfQuoted := func(s string) (string, bool) {
		trimmed := strings.TrimSpace(s)
		if len(trimmed) >= 2 && trimmed[0] == '"' && trimmed[len(trimmed)-1] == '"' {
			if unq, err := strconvUnquoteSafe(trimmed); err == nil {
				return unq, true
			}
		}
		return s, false
	}

	resolveIdentLiteral := func(name string, funcName string, methodText string, fallbackLine int) (string, string, int, bool, bool) {
		name = strings.TrimSpace(name)
		if name == "" {
			return "", "", 0, false, false
		}
		if lit, ok := lookupLiteral(funcName, name); ok {
			return lit.Value, lit.RelPath, lit.Line, true, lit.IsDynamic
		}
		if lit, ok := lookupLiteralAnyMethod(name); ok {
			return lit.Value, lit.RelPath, lit.Line, true, lit.IsDynamic
		}
		if rebuilt, dyn := rebuildCSharpVariableSql(methodText, name); rebuilt != "" {
			return rebuilt, relPath, fallbackLine, true, dyn
		}
		return "", "", 0, false, false
	}

	for _, p := range patterns {
		matches := p.re.FindAllStringSubmatchIndex(clean, -1)
		for _, m := range matches {
			start := m[0]
			line := countLinesUpTo(clean, start)
			if line <= 0 {
				line = 1
			}
			var (
				connName string
				raw      string
				rawExpr  string
			)

			var (
				funcName  string
				funcRange *methodRange
				lineStart = line
				lineEnd   = line
			)
			if line-1 >= 0 && line-1 < len(methodAtLine) {
				funcRange = methodAtLine[line-1]
			}
			if funcRange == nil && line-1 >= 0 && line-1 < len(sequentialMethodAtLine) {
				funcRange = sequentialMethodAtLine[line-1]
			}
			if funcRange == nil {
				funcRange = fallbackMethod(line)
			}
			if funcRange == nil {
				funcRange = scanBackwardForMethod(lines, line)
			}
			if funcRange != nil {
				funcName = funcRange.Name
			} else {
				funcName = fmt.Sprintf("<file-scope>@L%d", line)
			}

			if endPos := m[1]; endPos > 0 {
				if endLine := countLinesUpTo(clean, endPos); endLine > 0 {
					lineEnd = endLine
				}
			}

			methodText := ""
			if funcRange != nil {
				start := funcRange.Start - 1
				if start < 0 {
					start = 0
				}
				end := funcRange.End
				if end > len(lines) {
					end = len(lines)
				}
				methodText = strings.Join(lines[start:end], "\n")
			} else {
				ctxStart := line - 50
				if ctxStart < 0 {
					ctxStart = 0
				}
				ctxEnd := line + 50
				if ctxEnd > len(lines) {
					ctxEnd = len(lines)
				}
				methodText = strings.Join(lines[ctxStart:ctxEnd], "\n")
			}

			isDyn := p.dynamic
			isExecStub := p.execStub
			rawLiteral := false
			defPath := relPath
			defLine := line
			dynReason := ""
			rawFromVariable := false

			switch p.re {
			case regexes.newCmd:
				// group1 = verbatim flag, group2 = SQL, group3 = conn
				raw = decodeCSharpLiteralContent(cleanedGroup(clean, m, 2), cleanedGroup(clean, m, 1) == "@")
				rawLiteral = true
				connName = strings.TrimSpace(cleanedGroup(clean, m, 3))
			case regexes.newCmdIdent:
				raw = strings.TrimSpace(cleanedGroup(clean, m, 1))
				connName = strings.TrimSpace(cleanedGroup(clean, m, 2))
				if regexes.identRe.MatchString(raw) {
					if lit, litPath, litLine, okLiteral, dyn := resolveIdentLiteral(raw, funcName, methodText, line); okLiteral {
						raw = lit
						defPath = litPath
						defLine = litLine
						rawLiteral = true
						isDyn = dyn
					} else {
						isDyn = true
					}
				}
			case regexes.execProcLit:
				// group1 = conn, group2 = verbatim flag, group3 = arg (SP literal or expr)
				connName = strings.TrimSpace(cleanedGroup(clean, m, 1))
				rawArg := strings.TrimSpace(cleanedGroup(clean, m, 3))
				raw = decodeCSharpLiteralContent(rawArg, cleanedGroup(clean, m, 2) == "@")
				rawLiteral = strings.HasPrefix(rawArg, "\"") || strings.HasPrefix(rawArg, "@\"")
				if regexes.identRe.MatchString(rawArg) && !rawLiteral {
					rawFromVariable = true
					isDyn = true
				}
			case regexes.execProcDyn:
				connName = strings.TrimSpace(cleanedGroup(clean, m, 1))
				rawArg := strings.TrimSpace(cleanedGroup(clean, m, 2))
				raw = rawArg
				rawFromVariable = true
				if regexes.identRe.MatchString(rawArg) {
					if lit, litPath, litLine, okLiteral, dyn := resolveIdentLiteral(rawArg, funcName, methodText, line); okLiteral {
						raw = lit
						isDyn = dyn
						isExecStub = true
						rawLiteral = true
						defPath = litPath
						defLine = litLine
					}
				}
			case regexes.efExecRawIdent:
				raw = strings.TrimSpace(cleanedGroup(clean, m, 1))
				rawLiteral = false
				if regexes.identRe.MatchString(raw) {
					if lit, litPath, litLine, okLiteral, dyn := resolveIdentLiteral(raw, funcName, methodText, line); okLiteral {
						raw = lit
						rawLiteral = true
						isDyn = dyn
						defPath = litPath
						defLine = litLine
					} else {
						isDyn = true
					}
				}
			case regexes.execQueryIdent:
				connName = strings.TrimSpace(cleanedGroup(clean, m, 1))
				expr := strings.TrimSpace(cleanedGroup(clean, m, 2))
				raw = expr
				rawLiteral = false
				if regexes.identRe.MatchString(expr) {
					if lit, litPath, litLine, okLiteral, dyn := resolveIdentLiteral(expr, funcName, methodText, line); okLiteral {
						raw = lit
						defPath = litPath
						defLine = litLine
						rawLiteral = true
						isDyn = dyn
					} else {
						assignRe := regexp.MustCompile("(?is)" + regexp.QuoteMeta(expr) + `\s*=\s*@?"([^"]+)"`)
						if prefix := clean[:start]; prefix != "" {
							if matches := assignRe.FindAllStringSubmatch(prefix, -1); len(matches) > 0 {
								last := matches[len(matches)-1]
								if len(last) >= 2 {
									raw = last[1]
									rawLiteral = true
									isDyn = false
								}
							}
						}
						if !rawLiteral {
							isDyn = true
						}
					}
				}
			case regexes.byQueryCall:
				raw = strings.TrimSpace(cleanedGroup(clean, m, 1))
				rawLiteral = false
			case regexes.callQueryWsLit, regexes.callQueryWsDyn:
				// group1 = SQL or expression
				if p.re == regexes.callQueryWsLit {
					raw = decodeCSharpLiteralContent(cleanedGroup(clean, m, 2), cleanedGroup(clean, m, 1) == "@")
				} else {
					raw = cleanedGroup(clean, m, 1)
				}
				if p.re == regexes.callQueryWsLit {
					rawLiteral = true
				}
			default:
				// Dapper / EF / ExecuteQuery / CommandText: group1 = SQL
				raw = decodeCSharpLiteralContent(cleanedGroup(clean, m, 2), cleanedGroup(clean, m, 1) == "@")
				rawLiteral = true
				if p.re == regexes.commandTextIdent {
					expr := strings.TrimSpace(raw)
					rawLiteral = false
					raw = expr
					if regexes.identRe.MatchString(expr) {
						if lit, litPath, litLine, okLiteral, dyn := resolveIdentLiteral(expr, funcName, methodText, line); okLiteral {
							raw = lit
							defPath = litPath
							defLine = litLine
							rawLiteral = true
							isDyn = dyn
						} else {
							isDyn = true
						}
					}
				}
			}

			rawExpr = raw
			argExpr := ""
			if p.sqlArgIndex >= 0 {
				if args := extractCSharpArgs(clean, start); len(args) > p.sqlArgIndex {
					argExpr = strings.TrimSpace(args[p.sqlArgIndex])
				}
			}

			if norm, dyn, ok := normalizeCSharpSqlExpression(argExpr); ok {
				raw = norm
				rawLiteral = true
				isDyn = dyn
				if argExpr != "" && regexes.identRe.MatchString(argExpr) {
					rawFromVariable = true
				}
				if dyn && dynReason == "" {
					dynReason = "concat"
				}
			} else if argExpr != "" && regexes.identRe.MatchString(argExpr) {
				if rebuilt, dyn := rebuildCSharpVariableSql(methodText, argExpr); rebuilt != "" {
					raw = rebuilt
					rawLiteral = true
					isDyn = dyn
					rawFromVariable = true
					if dyn && dynReason == "" {
						dynReason = "concat"
					}
				}
				if rawFromVariable && dynReason == "" {
					dynReason = "runtime variable"
				}
			}

			if !rawLiteral {
				if unq, ok := unquoteIfQuoted(raw); ok {
					raw = unq
					rawLiteral = true
				}
			}

			rawTrim := strings.TrimSpace(raw)
			if !rawLiteral && (p.re == regexes.execProcDyn || p.re == regexes.callQueryWsDyn || regexes.identRe.MatchString(rawTrim)) {
				isDyn = true
				raw = "<dynamic-sql>"
			}

			if !rawLiteral {
				ctxText := ""
				ctxStart := line - 5
				if ctxStart < 0 {
					ctxStart = 0
				}
				ctxEnd := line + 5
				if ctxEnd > len(lines) {
					ctxEnd = len(lines)
				}
				ctxText = strings.Join(lines[ctxStart:ctxEnd], "\n")
				ctxText = stripCSComments(ctxText)
				methodTextClean := stripCSComments(methodText)
				cleanRawExpr := stripCSComments(rawExpr)

				skel, dyn, reason := BuildSqlSkeletonFromCSharpExpr(cleanRawExpr)
				if skel == "" && ctxText != "" {
					skel, dyn, reason = BuildSqlSkeletonFromCSharpExpr(ctxText)
				}
				if skel == "" && methodTextClean != "" {
					skel, dyn, reason = BuildSqlSkeletonFromCSharpExpr(methodTextClean)
				}
				if skel != "" && detectUsageKind(false, skel) != "UNKNOWN" {
					raw = skel
					rawLiteral = true
					if dyn {
						isDyn = true
						rawFromVariable = true
						if dynReason == "" {
							dynReason = reason
						}
					}
				}
			}

			if raw == "" {
				if isDyn {
					raw = "<dynamic-sql>"
				} else {
					continue
				}
			}

			hasKeyword := containsSqlKeyword(raw)
			if raw != "<dynamic-sql>" && !hasKeyword {
				if strings.Contains(raw, "(") && strings.Contains(raw, ".") && !strings.ContainsAny(raw, "@?:") {
					continue
				}
				if isProcNameSpec(raw) {
					isExecStub = true
				} else {
					continue
				}
			}
			usageCheck := detectUsageKind(isExecStub, normalizeSqlWhitespace(StripSqlComments(raw)))
			if raw != "<dynamic-sql>" && usageCheck == "UNKNOWN" && !hasKeyword && !isExecStub {
				continue
			}
			if !isDyn && usageCheck == "UNKNOWN" && !isExecStub {
				continue
			}
			// mark dynamic if raw contains interpolations or variables
			if !p.dynamic {
				if strings.Contains(raw, "$") || (strings.Contains(raw, "{") && strings.Contains(raw, "}")) {
					isDyn = true
					if dynReason == "" {
						dynReason = "interpolation"
					}
				}
			}
			// Determine exec stub: if pattern flagged or the raw string looks like a proc name spec
			if !isDyn && !isExecStub {
				if isProcNameSpec(raw) {
					isExecStub = true
				}
			}
			if isDyn && raw == "" {
				raw = "<dynamic-sql>"
			}

			if raw != "" && raw != "<dynamic-sql>" {
				raw = StripSqlComments(raw)
			}

			if rawFromVariable && isDyn && dynReason == "" {
				dynReason = "runtime variable"
			}

			cand := SqlCandidate{
				AppName:       cfg.AppName,
				RelPath:       relPath,
				File:          filepath.Base(path),
				SourceCat:     "code",
				SourceKind:    "csharp",
				CallSiteKind:  canonicalCallSiteKind(p.callSiteKind),
				LineStart:     lineStart,
				LineEnd:       lineEnd,
				Func:          funcName,
				RawSql:        raw,
				IsDynamic:     isDyn,
				IsExecStub:    isExecStub,
				DynamicReason: normalizeDynamicReasonLabel(dynReason),
				ConnName:      connName,
				ConnDb:        "",
				DefinedPath:   defPath,
				DefinedLine:   defLine,
			}
			cands = append(cands, cand)
		}
	}

	return cands, nil
}

// groupNumber di sini adalah nomor group capture (1-based), bukan index byte.
func cleanedGroup(s string, idxs []int, groupNumber int) string {
	i := groupNumber * 2
	if i+1 >= len(idxs) {
		return ""
	}
	start, end := idxs[i], idxs[i+1]
	if start < 0 || end < 0 || start >= len(s) || end > len(s) || start >= end {
		return ""
	}
	return s[start:end]
}

func decodeCSharpLiteralContent(val string, verbatim bool) string {
	if val == "" {
		return ""
	}
	if verbatim {
		return strings.ReplaceAll(val, `""`, `"`)
	}
	wrapped := `"` + val + `"`
	if decoded, _, ok := parseCSharpStringLiteral(wrapped, 0); ok {
		return decoded
	}
	return val
}

type methodRange struct {
	Name       string
	Start, End int
}

func extractCsMethodName(trimmed string) string {
	if strings.TrimSpace(trimmed) == "" {
		return ""
	}
	if isCsControlKeyword(leadingToken(trimmed)) {
		return ""
	}
	if m := regexes.methodRe.FindStringSubmatch(trimmed); len(m) >= 3 {
		return m[2]
	}
	if m := regexes.methodReNoMod.FindStringSubmatch(trimmed); len(m) >= 2 {
		candidate := m[1]
		if isCsControlKeyword(strings.ToLower(candidate)) {
			return ""
		}
		return candidate
	}
	return ""
}

func countBracesAndStringState(line string, inString bool, verbatim bool, escaped bool) (int, int, bool, bool, bool) {
	open := 0
	close := 0
	for i := 0; i < len(line); i++ {
		c := line[i]
		if inString {
			if verbatim {
				if c == '"' {
					if i+1 < len(line) && line[i+1] == '"' {
						i++
						continue
					}
					inString = false
					verbatim = false
				}
				continue
			}
			if escaped {
				escaped = false
				continue
			}
			if c == '\\' {
				escaped = true
				continue
			}
			if c == '"' {
				inString = false
			}
			continue
		}
		if c == '"' {
			inString = true
			verbatim = i > 0 && line[i-1] == '@'
			escaped = false
			continue
		}
		if c == '{' {
			open++
			continue
		}
		if c == '}' {
			close++
		}
	}
	return open, close, inString, verbatim, escaped
}

func leadingToken(line string) string {
	line = strings.TrimSpace(line)
	if line == "" {
		return ""
	}
	fields := strings.Fields(line)
	if len(fields) == 0 {
		return ""
	}
	token := fields[0]
	token = strings.TrimLeft(token, "([{\t")
	token = strings.TrimRight(token, "({")
	return strings.ToLower(token)
}

func isCsControlKeyword(tok string) bool {
	switch strings.ToLower(strings.TrimSpace(tok)) {
	case "", "if", "for", "foreach", "while", "switch", "catch", "using", "lock", "else", "try", "do", "case", "default":
		return tok != ""
	default:
		return false
	}
}

func containsSqlKeyword(raw string) bool {
	lower := strings.ToLower(raw)
	keywords := []string{"select", "insert", "update", "delete", "truncate", "exec", "execute"}
	for _, kw := range keywords {
		if strings.Contains(lower, kw) {
			return true
		}
	}
	return false
}

func detectCsMethods(lines []string) []methodRange {
	var methods []methodRange
	var current *methodRange
	braceDepth := 0
	methodStarted := false
	inString := false
	verbatim := false
	escaped := false

	for i, line := range lines {
		trimmed := strings.TrimSpace(line)
		name := ""
		if !inString {
			name = extractCsMethodName(trimmed)
			if name == "" && strings.Contains(trimmed, "{") && i > 0 {
				combined := strings.TrimSpace(lines[i-1] + " " + trimmed)
				name = extractCsMethodName(combined)
			}
			if current != nil && name != "" {
				if current.End < i {
					current.End = i
				}
				methods = append(methods, *current)
				current = nil
				braceDepth = 0
				methodStarted = false
			}
			if current == nil && name != "" {
				current = &methodRange{Name: name, Start: i + 1, End: i + 1}
				braceDepth = 0
				methodStarted = false
			}
		}

		open, close, nextInString, nextVerbatim, nextEscaped := countBracesAndStringState(line, inString, verbatim, escaped)
		inString, verbatim, escaped = nextInString, nextVerbatim, nextEscaped

		if current != nil {
			if open > 0 {
				methodStarted = true
			}
			braceDepth += open
			if methodStarted && i+1 > current.End {
				current.End = i + 1
			}
			braceDepth -= close
			if methodStarted && braceDepth <= 0 {
				if current.End < current.Start {
					current.End = current.Start
				}
				methods = append(methods, *current)
				current = nil
				braceDepth = 0
				methodStarted = false
			}
		}
	}

	if current != nil {
		if !methodStarted {
			current.End = current.Start
		}
		methods = append(methods, *current)
	}

	return methods
}

func indexCsMethodLines(methods []methodRange, totalLines int) []*methodRange {
	methodAtLine := make([]*methodRange, totalLines)
	for i := range methods {
		m := &methods[i]
		start := m.Start
		if start < 1 {
			start = 1
		}
		end := m.End
		if end > totalLines {
			end = totalLines
		}
		for line := start; line <= end; line++ {
			methodAtLine[line-1] = m
		}
	}
	return methodAtLine
}

func buildSequentialMethodIndex(lines []string) []*methodRange {
	methodAtLine := make([]*methodRange, len(lines))
	var current *methodRange
	inString := false
	verbatim := false
	escaped := false
	for i, line := range lines {
		if !inString {
			if name := extractCsMethodName(strings.TrimSpace(line)); name != "" {
				current = &methodRange{Name: name, Start: i + 1, End: i + 1}
			}
		}
		_, _, inString, verbatim, escaped = countBracesAndStringState(line, inString, verbatim, escaped)
		if current != nil {
			current.End = i + 1
			methodAtLine[i] = current
		}
	}
	return methodAtLine
}

func scanBackwardForMethod(lines []string, line int) *methodRange {
	if line < 1 {
		line = 1
	}

	const maxSearch = 300
	limit := line - maxSearch
	if limit < 0 {
		limit = 0
	}

	inString := false
	verbatim := false
	escaped := false

	for i := line - 1; i >= limit && i < len(lines); i-- {
		open, _, nextInString, nextVerbatim, nextEscaped := countBracesAndStringState(lines[i], inString, verbatim, escaped)
		inString, verbatim, escaped = nextInString, nextVerbatim, nextEscaped

		trimmed := strings.TrimSpace(lines[i])
		if trimmed == "" || strings.HasPrefix(trimmed, "//") {
			continue
		}
		if strings.HasPrefix(trimmed, "[") && strings.HasSuffix(trimmed, "]") {
			continue
		}

		if open == 0 || !strings.Contains(trimmed, "{") {
			continue
		}

		name := extractCsMethodName(trimmed)
		if name == "" && i > 0 {
			combined := strings.TrimSpace(lines[i-1] + " " + trimmed)
			name = extractCsMethodName(combined)
		}
		if name != "" {
			start := i + 1
			end := line
			if end < start {
				end = start
			}
			return &methodRange{Name: name, Start: start, End: end}
		}
	}
	return nil
}

func parseCsBinaryAssignment(line string) (string, string, bool) {
	clean := stripCSComments(line)
	if strings.Contains(clean, "=>") {
		return "", "", false
	}
	idx := strings.Index(clean, "=")
	if idx <= 0 {
		return "", "", false
	}
	left := strings.TrimSpace(clean[:idx])
	right := strings.TrimSpace(clean[idx+1:])
	if strings.HasSuffix(left, "!") || strings.HasSuffix(left, ">") || strings.HasSuffix(left, "<") {
		return "", "", false
	}
	if strings.HasPrefix(right, "=") {
		return "", "", false
	}
	right = strings.TrimSuffix(right, ";")
	if right == "" || !strings.Contains(right, "+") {
		return "", "", false
	}
	leftFields := strings.Fields(left)
	if len(leftFields) == 0 {
		return "", "", false
	}
	name := leftFields[len(leftFields)-1]
	if !regexes.identRe.MatchString(name) {
		return "", "", false
	}
	return name, right, true
}

func mergeCSharpBinaryConcat(expr string, resolver func(string) (SqlSymbol, bool)) (string, bool, bool) {
	trimmed := strings.TrimSpace(expr)
	trimmed = strings.TrimSuffix(trimmed, ";")
	if trimmed == "" {
		return "", false, false
	}
	parts := splitTopLevelConcat(trimmed)
	if len(parts) == 0 {
		return "", false, false
	}

	var fragments []string
	dyn := false
	for _, part := range parts {
		part = strings.TrimSpace(part)
		if part == "" {
			continue
		}
		if lit, ok := extractPureStringLiteral(part); ok {
			fragments = append(fragments, lit)
			continue
		}
		if sym, ok := resolver(part); ok {
			fragments = append(fragments, sym.Value)
			dyn = dyn || sym.IsDynamic
			continue
		}
		if looksNumericLiteral(part) {
			return "", false, false
		}
		dyn = true
		fragments = append(fragments, "<expr>")
	}
	if len(fragments) == 0 {
		return "", dyn, false
	}
	return normalizeSqlSkeleton(strings.Join(fragments, "")), dyn, true
}

func extractCSharpArgs(src string, matchStart int) []string {
	if matchStart < 0 || matchStart >= len(src) {
		return nil
	}
	openRel := strings.Index(src[matchStart:], "(")
	if openRel < 0 {
		return nil
	}
	i := matchStart + openRel + 1
	depth := 1
	start := i
	inString := false
	verbatim := false
	escaped := false
	var args []string

	for i < len(src) {
		c := src[i]
		if inString {
			if verbatim {
				if c == '"' {
					if i+1 < len(src) && src[i+1] == '"' {
						i++
					} else {
						inString = false
						verbatim = false
					}
				}
				i++
				continue
			}
			if escaped {
				escaped = false
				i++
				continue
			}
			if c == '\\' {
				escaped = true
				i++
				continue
			}
			if c == '"' {
				inString = false
			}
			i++
			continue
		}

		switch c {
		case '"':
			inString = true
			verbatim = i > 0 && src[i-1] == '@'
			escaped = false
		case '(':
			depth++
		case ')':
			depth--
			if depth == 0 {
				arg := strings.TrimSpace(src[start:i])
				if arg != "" {
					args = append(args, arg)
				}
				return args
			}
		case ',':
			if depth == 1 {
				arg := strings.TrimSpace(src[start:i])
				args = append(args, arg)
				start = i + 1
			}
		}

		i++
	}

	return args
}

func normalizeCSharpSqlExpression(expr string) (string, bool, bool) {
	cleaned := strings.TrimSpace(expr)
	if cleaned == "" {
		return "", false, false
	}

	cleaned = stripCSComments(cleaned)
	cleaned = strings.TrimSpace(cleaned)
	if cleaned == "" {
		return "", false, false
	}

	sql, dyn, _ := BuildSqlSkeletonFromCSharpExpr(cleaned)
	if sql == "" {
		return "", false, false
	}
	return sql, dyn, true
}

func rebuildCSharpVariableSql(methodText, varName string) (string, bool) {
	varName = strings.TrimSpace(varName)
	if varName == "" {
		return "", false
	}

	cleaned := stripCSComments(methodText)
	assignFinder := regexp.MustCompile(`(?is)\b` + regexp.QuoteMeta(varName) + `\s*(\+=|=)`)
	appendFinder := regexp.MustCompile(`(?is)\b` + regexp.QuoteMeta(varName) + `\s*\.\s*Append(?:Line|Format)?\s*\(`)

	type sqlBuildMatch struct {
		pos  int
		expr string
		kind string
	}

	matches := make([]sqlBuildMatch, 0)
	for _, m := range assignFinder.FindAllStringSubmatchIndex(cleaned, -1) {
		if len(m) < 4 {
			continue
		}
		op := strings.TrimSpace(cleaned[m[2]:m[3]])
		expr, _ := extractCSharpStatementExpr(cleaned, m[1])
		kind := "assign"
		if op == "+=" {
			kind = "append"
		}
		lower := strings.ToLower(strings.TrimSpace(expr))
		base := strings.ToLower(varName)
		if kind == "assign" && strings.HasPrefix(lower, base+"+") {
			kind = "append"
		}
		matches = append(matches, sqlBuildMatch{pos: m[0], expr: expr, kind: kind})
	}
	for _, m := range appendFinder.FindAllStringIndex(cleaned, -1) {
		args := extractCSharpArgs(cleaned, m[0])
		expr := ""
		if len(args) > 0 {
			expr = args[0]
		}
		matches = append(matches, sqlBuildMatch{pos: m[0], expr: expr, kind: "append"})
	}

	sort.Slice(matches, func(i, j int) bool {
		return matches[i].pos < matches[j].pos
	})

	var fragments []string
	dynamic := false

	for _, m := range matches {
		expr := strings.TrimSpace(m.expr)
		lower := strings.ToLower(expr)
		baseName := strings.ToLower(varName)
		if m.kind == "assign" {
			if strings.HasPrefix(lower, baseName+"+") {
				m.kind = "append"
			}
		}

		frag, dyn := extractSqlFragmentFromCSharpExpr(expr)
		if frag == "" {
			if base, fromLit, toLit, hasFrom, hasTo, ok := parseReplaceExpression(expr); ok && !strings.EqualFold(base, varName) {
				if sql, innerDyn := rebuildCSharpVariableSql(methodText, base); sql != "" {
					frag = sql
					dyn = dyn || innerDyn
					if hasFrom {
						if hasTo {
							frag = strings.ReplaceAll(frag, fromLit, toLit)
						} else {
							dyn = true
						}
					}
				}
			}
		}
		trimmedExpr := strings.TrimSpace(expr)
		if frag == "" && trimmedExpr != "" && !strings.EqualFold(trimmedExpr, varName) {
			if regexes.identRe.MatchString(trimmedExpr) {
				if sql, innerDyn := rebuildCSharpVariableSql(methodText, trimmedExpr); sql != "" {
					frag = sql
					dyn = dyn || innerDyn
				}
			}
			lowerExpr := strings.ToLower(trimmedExpr)
			if frag == "" && strings.HasSuffix(lowerExpr, ".tostring()") {
				base := strings.TrimSpace(trimmedExpr[:len(trimmedExpr)-len(".tostring()")])
				if regexes.identRe.MatchString(base) {
					if sql, innerDyn := rebuildCSharpVariableSql(methodText, base); sql != "" {
						frag = sql
						dyn = dyn || innerDyn
					}
				}
			}
		}
		dynamic = dynamic || dyn

		if m.kind == "assign" {
			if frag != "" {
				fragments = []string{frag}
				dynamic = dynamic || dyn
				continue
			}

			trimmedExpr := strings.TrimSpace(expr)
			if strings.Contains(trimmedExpr, varName) {
				// Preserve prior fragments when the assignment rewrites the same variable (e.g., Replace). If
				// we fail to parse the new expression, fallback to previously collected SQL parts.
				dynamic = dynamic || dyn
				continue
			}

			fragments = fragments[:0]
			continue
		}

		if frag != "" {
			fragments = append(fragments, frag)
		}
	}

	if len(fragments) == 0 {
		return "", dynamic
	}

	combined := normalizeSqlSkeleton(strings.Join(fragments, "\n"))
	return combined, dynamic
}

func stripCSComments(src string) string {
	const (
		stateNormal = iota
		stateLineComment
		stateBlockComment
		stateString
		stateChar
		stateVerbatimString
	)

	var b strings.Builder
	state := stateNormal
	escaped := false
	blockDepth := 0

	for i := 0; i < len(src); {
		c := src[i]
		next := byte(0)
		if i+1 < len(src) {
			next = src[i+1]
		}

		switch state {
		case stateNormal:
			if c == '/' && next == '/' {
				state = stateLineComment
				i += 2
				continue
			}
			if c == '/' && next == '*' {
				state = stateBlockComment
				blockDepth = 1
				i += 2
				continue
			}
			if c == '@' && next == '"' {
				state = stateVerbatimString
				b.WriteByte(c)
				b.WriteByte(next)
				i += 2
				continue
			}
			if c == '"' {
				state = stateString
				b.WriteByte(c)
				i++
				continue
			}
			if c == '\'' {
				state = stateChar
				b.WriteByte(c)
				i++
				continue
			}
			b.WriteByte(c)
			i++
		case stateLineComment:
			if c == '\n' {
				b.WriteByte('\n')
				state = stateNormal
			}
			i++
		case stateBlockComment:
			if c == '\n' {
				b.WriteByte('\n')
				i++
				continue
			}
			if c == '/' && next == '*' {
				blockDepth++
				i += 2
				continue
			}
			if c == '*' && next == '/' {
				blockDepth--
				i += 2
				if blockDepth == 0 {
					state = stateNormal
				}
				continue
			}
			i++
		case stateString:
			b.WriteByte(c)
			if escaped {
				escaped = false
				i++
				continue
			}
			if c == '\\' {
				escaped = true
				i++
				continue
			}
			if c == '"' {
				state = stateNormal
			}
			i++
		case stateChar:
			b.WriteByte(c)
			if escaped {
				escaped = false
				i++
				continue
			}
			if c == '\\' {
				escaped = true
				i++
				continue
			}
			if c == '\'' {
				state = stateNormal
			}
			i++
		case stateVerbatimString:
			b.WriteByte(c)
			if c == '"' {
				if next == '"' {
					b.WriteByte(next)
					i += 2
					continue
				}
				state = stateNormal
			}
			i++
		}
	}

	return b.String()
}

func extractCSharpStatementExpr(src string, start int) (string, int) {
	i := start
	for i < len(src) {
		if !unicode.IsSpace(rune(src[i])) {
			break
		}
		i++
	}
	exprStart := i
	inString := false
	verbatim := false
	escaped := false

	for i < len(src) {
		c := src[i]
		if inString {
			if verbatim {
				if c == '"' {
					if i+1 < len(src) && src[i+1] == '"' {
						i++
					} else {
						inString = false
						verbatim = false
					}
				}
				i++
				continue
			}
			if escaped {
				escaped = false
				i++
				continue
			}
			if c == '\\' {
				escaped = true
				i++
				continue
			}
			if c == '"' {
				inString = false
			}
			i++
			continue
		}

		switch c {
		case '"':
			inString = true
			verbatim = i > 0 && src[i-1] == '@'
		case ';':
			expr := strings.TrimSpace(src[exprStart:i])
			return expr, i
		}
		i++
	}

	return strings.TrimSpace(src[exprStart:]), len(src)
}

func splitTopLevelConcat(expr string) []string {
	var parts []string
	start := 0
	for i := 0; i < len(expr); {
		if isQuoteStart(expr, i) || isInterpolatedStart(expr, i) {
			_, next, _, ok := parseAnyStringLiteral(expr, i)
			if !ok {
				i++
				continue
			}
			i = next
			continue
		}
		if expr[i] == '+' {
			segment := strings.TrimSpace(expr[start:i])
			if segment != "" {
				parts = append(parts, segment)
			}
			i++
			start = i
			continue
		}
		i++
	}
	if tail := strings.TrimSpace(expr[start:]); tail != "" {
		parts = append(parts, tail)
	}
	return parts
}

func looksNumericLiteral(part string) bool {
	part = strings.TrimSpace(part)
	if part == "" {
		return false
	}
	if strings.HasPrefix(part, "\"") || strings.HasPrefix(part, "@\"") || strings.HasPrefix(part, "$\"") {
		return false
	}
	for _, r := range part {
		if unicode.IsDigit(r) {
			continue
		}
		if r == '.' || r == '_' {
			continue
		}
		return false
	}
	return true
}

func extractSqlFragmentFromCSharpExpr(expr string) (string, bool) {
	trimmed := strings.TrimSpace(expr)
	trimmed = strings.TrimSuffix(trimmed, ";")
	trimmed = strings.TrimSpace(trimmed)
	if trimmed == "" {
		return "", false
	}

	sql, dyn, _ := BuildSqlSkeletonFromCSharpExpr(trimmed)
	return sql, dyn
}

func parseReplaceExpression(expr string) (base string, fromLit string, toLit string, hasFrom bool, hasTo bool, ok bool) {
	trimmed := strings.TrimSpace(expr)
	if trimmed == "" {
		return
	}
	lower := strings.ToLower(trimmed)
	idx := strings.Index(lower, ".replace")
	if idx <= 0 {
		return
	}

	base = strings.TrimSpace(trimmed[:idx])
	base = strings.Trim(base, "()")
	if base == "" {
		return
	}

	args := extractCSharpArgs(trimmed, idx)
	if len(args) < 2 {
		return
	}

	from, fromOk := extractPureStringLiteral(args[0])
	to, toOk := extractPureStringLiteral(args[1])

	return base, from, to, fromOk, toOk, true
}
