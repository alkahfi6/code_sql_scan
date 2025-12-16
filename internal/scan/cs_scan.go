package scan

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strings"
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

	// Track simple string assignments per method (e.g., var cmd = "dbo.MyProc";)
	literalInMethod := make(map[string]map[string]string)
	recordLiteral := func(method, name, val string) {
		if method == "" || name == "" {
			return
		}
		if _, ok := literalInMethod[method]; !ok {
			literalInMethod[method] = make(map[string]string)
		}
		literalInMethod[method][name] = val
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
		recordLiteral(funcName, name, val)
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
			recordLiteral(method, m[1], m[2])
			continue
		}
		if strings.Contains(line, "==") || strings.Contains(line, "!=") || strings.Contains(line, "+=") || strings.Contains(line, "-=") || strings.Contains(line, "*=") || strings.Contains(line, "/=") {
			continue
		}
		if m := regexes.bareAssign.FindStringSubmatch(line); len(m) == 3 {
			recordLiteral(method, m[1], m[2])
		}
	}

	var cands []SqlCandidate

	type pat struct {
		re       *regexp.Regexp
		execStub bool
		dynamic  bool
	}
	patterns := []pat{
		{regexes.execProcLit, true, false},
		{regexes.execProcDyn, true, true},
		{regexes.newCmd, false, false},
		{regexes.newCmdIdent, false, false},
		{regexes.dapperQuery, false, false},
		{regexes.dapperExec, false, false},
		{regexes.efFromSql, false, false},
		{regexes.efExecRaw, false, false},
		{regexes.execQuery, false, false},      // ExecuteQuery(conn, "SQL")
		{regexes.execQueryIdent, false, false}, // ExecuteQuery(conn, variable)
		{regexes.callQueryWsLit, false, false}, // CallQueryFromWs with literal SQL
		{regexes.callQueryWsDyn, false, true},  // CallQueryFromWs with dynamic SQL expression
		{regexes.commandTextLit, false, false}, // CommandText = "ProcName"
		{regexes.commandTextIdent, false, false},
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
			if funcRange != nil {
				funcName = funcRange.Name
				lineStart = funcRange.Start
				lineEnd = funcRange.End
			} else {
				funcName = fmt.Sprintf("<file-scope>@L%d", line)
			}

			isDyn := p.dynamic
			isExecStub := p.execStub
			rawLiteral := false

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
						raw = lit
						rawLiteral = true
					} else {
						isDyn = true
						raw = "<dynamic-sql>"
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
						raw = lit
						isDyn = false
						isExecStub = true
						rawLiteral = true
					}
				}
			case regexes.execQueryIdent:
				connName = strings.TrimSpace(cleanedGroup(clean, m, 1))
				expr := strings.TrimSpace(cleanedGroup(clean, m, 2))
				raw = expr
				rawLiteral = false
				if funcName != "" && regexes.identRe.MatchString(expr) {
					if lit, ok := literalInMethod[funcName][expr]; ok {
						raw = lit
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
							raw = "<dynamic-sql>"
						}
					}
				}
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
							raw = lit
							rawLiteral = true
						} else {
							isDyn = true
							raw = "<dynamic-sql>"
						}
					}
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
				AppName:     cfg.AppName,
				RelPath:     relPath,
				File:        filepath.Base(path),
				SourceCat:   "code",
				SourceKind:  "csharp",
				LineStart:   lineStart,
				LineEnd:     lineEnd,
				Func:        funcName,
				RawSql:      raw,
				IsDynamic:   isDyn,
				IsExecStub:  isExecStub,
				ConnName:    connName,
				ConnDb:      "",
				DefinedPath: relPath,
				DefinedLine: line,
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
