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
	clean := StripCodeCommentsCStyle(src, true)

	lines := strings.Split(src, "\n")
	methodRanges := detectCsMethods(lines)
	methodAtLine := indexCsMethodLines(methodRanges, len(lines))
	fallbackMethod := func(line int) *methodRange {
		if len(methodRanges) == 0 {
			return nil
		}
		if line < 1 {
			line = 1
		}
		var best *methodRange
		for i := range methodRanges {
			mr := &methodRanges[i]
			if mr.Start > line {
				break
			}
			best = mr
			if line <= mr.End {
				return mr
			}
		}
		if best != nil {
			const maxGap = 200
			if line-best.End <= maxGap {
				return best
			}
		}
		return nil
	}

	// Track simple string assignments per method (e.g., var cmd = "dbo.MyProc";)
	literalInMethod := make(map[string]map[string]SqlSymbol)
	recordLiteral := func(method, name, val string, line int) {
		if method == "" || name == "" {
			return
		}
		if _, ok := literalInMethod[method]; !ok {
			literalInMethod[method] = make(map[string]SqlSymbol)
		}
		literalInMethod[method][name] = SqlSymbol{
			Name:       name,
			Value:      val,
			RelPath:    relPath,
			Line:       line,
			IsComplete: true,
		}
	}

	lookupLiteralAnyMethod := func(name string) (SqlSymbol, bool) {
		for _, m := range literalInMethod {
			if val, ok := m[name]; ok {
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
		if funcName == "" {
			continue
		}
		name := cleanedGroup(clean, m, 1)
		val := cleanedGroup(clean, m, 2)
		recordLiteral(funcName, name, val, line)
	}
	for i, line := range lines {
		method := ""
		if i < len(methodAtLine) && methodAtLine[i] != nil {
			method = methodAtLine[i].Name
		}
		if method == "" {
			continue
		}
		if m := regexes.simpleDeclAssign.FindStringSubmatch(line); len(m) == 3 {
			recordLiteral(method, m[1], m[2], i+1)
			continue
		}
		if strings.Contains(line, "==") || strings.Contains(line, "!=") || strings.Contains(line, "+=") || strings.Contains(line, "-=") || strings.Contains(line, "*=") || strings.Contains(line, "/=") {
			continue
		}
		if m := regexes.bareAssign.FindStringSubmatch(line); len(m) == 3 {
			recordLiteral(method, m[1], m[2], i+1)
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

			switch p.re {
			case regexes.newCmd:
				// group1 = SQL, group2 = conn
				raw = cleanedGroup(clean, m, 1)
				rawLiteral = true
				connName = strings.TrimSpace(cleanedGroup(clean, m, 2))
			case regexes.newCmdIdent:
				raw = strings.TrimSpace(cleanedGroup(clean, m, 1))
				connName = strings.TrimSpace(cleanedGroup(clean, m, 2))
				if funcName != "" && regexes.identRe.MatchString(raw) {
					if lit, ok := literalInMethod[funcName][raw]; ok {
						raw = lit.Value
						defPath = lit.RelPath
						defLine = lit.Line
						rawLiteral = true
					} else {
						isDyn = true
					}
				}
			case regexes.execProcLit, regexes.execProcDyn:
				// group1 = conn, group2 = arg (SP literal or expr)
				connName = strings.TrimSpace(cleanedGroup(clean, m, 1))
				rawArg := strings.TrimSpace(cleanedGroup(clean, m, 2))
				raw = rawArg
				if p.re == regexes.execProcLit {
					rawLiteral = true
				}
				if p.re == regexes.execProcDyn && funcName != "" && regexes.identRe.MatchString(rawArg) {
					if lit, ok := literalInMethod[funcName][rawArg]; ok {
						raw = lit.Value
						isDyn = false
						isExecStub = true
						rawLiteral = true
						defPath = lit.RelPath
						defLine = lit.Line
					}
				}
			case regexes.execQueryIdent:
				connName = strings.TrimSpace(cleanedGroup(clean, m, 1))
				expr := strings.TrimSpace(cleanedGroup(clean, m, 2))
				raw = expr
				rawLiteral = false
				if funcName != "" && regexes.identRe.MatchString(expr) {
					if lit, ok := literalInMethod[funcName][expr]; ok {
						raw = lit.Value
						defPath = lit.RelPath
						defLine = lit.Line
						rawLiteral = true
					} else {
						if lit, ok := lookupLiteralAnyMethod(expr); ok {
							raw = lit.Value
							defPath = lit.RelPath
							defLine = lit.Line
							rawLiteral = true
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
				}
			case regexes.byQueryCall:
				raw = strings.TrimSpace(cleanedGroup(clean, m, 1))
				rawLiteral = false
			case regexes.callQueryWsLit, regexes.callQueryWsDyn:
				// group1 = SQL or expression
				raw = cleanedGroup(clean, m, 1)
				if p.re == regexes.callQueryWsLit {
					rawLiteral = true
				}
			default:
				// Dapper / EF / ExecuteQuery / CommandText: group1 = SQL
				raw = cleanedGroup(clean, m, 1)
				rawLiteral = true
				if p.re == regexes.commandTextIdent {
					expr := strings.TrimSpace(raw)
					rawLiteral = false
					raw = expr
					if funcName != "" && regexes.identRe.MatchString(expr) {
						if lit, ok := literalInMethod[funcName][expr]; ok {
							raw = lit.Value
							defPath = lit.RelPath
							defLine = lit.Line
							rawLiteral = true
						} else {
							if lit, ok := lookupLiteralAnyMethod(expr); ok {
								raw = lit.Value
								defPath = lit.RelPath
								defLine = lit.Line
								rawLiteral = true
							} else {
								isDyn = true
							}
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
			} else if argExpr != "" && regexes.identRe.MatchString(argExpr) {
				if rebuilt, dyn := rebuildCSharpVariableSql(methodText, argExpr); rebuilt != "" {
					raw = rebuilt
					rawLiteral = true
					isDyn = dyn
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

				skel, dyn, _ := BuildSqlSkeletonFromCSharpExpr(rawExpr)
				if skel == "" && ctxText != "" {
					skel, dyn, _ = BuildSqlSkeletonFromCSharpExpr(ctxText)
				}
				if skel == "" && methodText != "" {
					skel, dyn, _ = BuildSqlSkeletonFromCSharpExpr(methodText)
				}
				if skel != "" && detectUsageKind(false, skel) != "UNKNOWN" {
					raw = skel
					rawLiteral = true
					if dyn {
						isDyn = true
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
			// mark dynamic if raw contains interpolations or variables
			if !p.dynamic {
				if strings.Contains(raw, "$") || (strings.Contains(raw, "{") && strings.Contains(raw, "}")) {
					isDyn = true
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

			cand := SqlCandidate{
				AppName:      cfg.AppName,
				RelPath:      relPath,
				File:         filepath.Base(path),
				SourceCat:    "code",
				SourceKind:   "csharp",
				CallSiteKind: canonicalCallSiteKind(p.callSiteKind),
				LineStart:    lineStart,
				LineEnd:      lineEnd,
				Func:         funcName,
				RawSql:       raw,
				IsDynamic:    isDyn,
				IsExecStub:   isExecStub,
				ConnName:     connName,
				ConnDb:       "",
				DefinedPath:  defPath,
				DefinedLine:  defLine,
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

type methodRange struct {
	Name       string
	Start, End int
}

func detectCsMethods(lines []string) []methodRange {
	var methods []methodRange
	var current *methodRange
	braceDepth := 0
	methodStarted := false

	for i, line := range lines {
		trimmed := strings.TrimSpace(line)
		if current == nil && trimmed != "" {
			name := ""
			if m := regexes.methodRe.FindStringSubmatch(trimmed); len(m) >= 3 {
				name = m[2]
			} else if m := regexes.methodReNoMod.FindStringSubmatch(trimmed); len(m) >= 2 {
				candidate := m[1]
				kw := strings.ToLower(candidate)
				if kw != "if" && kw != "for" && kw != "foreach" && kw != "while" && kw != "switch" && kw != "catch" && kw != "using" && kw != "lock" {
					name = candidate
				}
			}
			if name != "" {
				current = &methodRange{Name: name, Start: i + 1, End: i + 1}
				braceDepth = 0
				methodStarted = false
			}
		}

		if current != nil {
			open := strings.Count(line, "{")
			close := strings.Count(line, "}")
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

func scanBackwardForMethod(lines []string, line int) *methodRange {
	if line < 1 {
		line = 1
	}
	limit := line - 200
	if limit < 0 {
		limit = 0
	}
	for i := line - 1; i >= limit && i < len(lines); i-- {
		trimmed := strings.TrimSpace(lines[i])
		if trimmed == "" || strings.HasPrefix(trimmed, "//") {
			continue
		}
		if strings.HasPrefix(trimmed, "[") && strings.HasSuffix(trimmed, "]") {
			continue
		}
		name := ""
		if m := regexes.methodRe.FindStringSubmatch(trimmed); len(m) >= 3 {
			name = m[2]
		} else if m := regexes.methodReNoMod.FindStringSubmatch(trimmed); len(m) >= 2 {
			candidate := m[1]
			kw := strings.ToLower(candidate)
			if kw != "if" && kw != "for" && kw != "foreach" && kw != "while" && kw != "switch" && kw != "catch" && kw != "using" && kw != "lock" {
				name = candidate
			}
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

	cleaned = StripCodeCommentsCStyle(cleaned, true)
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

	cleaned := StripCodeCommentsCStyle(methodText, true)
	assignFinder := regexp.MustCompile(`(?is)\b` + regexp.QuoteMeta(varName) + `\s*(\+=|=)`)

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
