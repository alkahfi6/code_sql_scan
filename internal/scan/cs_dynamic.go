package scan

import (
	"regexp"
	"strings"
	"unicode"
)

var (
	sbDeclRe        = regexp.MustCompile(`(?i)\bStringBuilder\s+([A-Za-z_][A-Za-z0-9_]*)`)
	sbAppendRe      = regexp.MustCompile(`([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*Append(Line)?\s*\(([^)]*)\)`)
	sbToStringRe    = regexp.MustCompile(`([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*ToString\s*\(`)
	formatCallRe    = regexp.MustCompile(`(?is)^\s*string\s*\.\s*Format\s*\((.*)\)\s*$`)
	dynamicTargetRe = regexp.MustCompile(`(?i)^(insert\s+into|truncate\s+table|update|delete\s+from)\s+([^\s;]+)`)
)

// BuildSqlSkeletonFromCSharpExpr attempts to extract a deterministic SQL skeleton
// from simple C# string concatenations or StringBuilder append chains.
// It only supports limited patterns as described in the micro-mission requirements.
func BuildSqlSkeletonFromCSharpExpr(expr string) (string, bool, string) {
	trimmed := strings.TrimSpace(expr)
	if trimmed == "" {
		return "", false, "empty"
	}

	if sql, dyn, ok := buildTernarySkeleton(trimmed); ok {
		sql = trimToSqlVerb(sql)
		if sql == "" || !allowDynamicSkeleton(sql, dyn) {
			return "", dyn, "ternary"
		}
		return sql, dyn, "ternary"
	}

	if sql, dyn, ok := buildStringFormatSkeleton(trimmed); ok {
		sql = trimToSqlVerb(sql)
		if sql == "" || !allowDynamicSkeleton(sql, dyn) {
			return "", dyn, "format"
		}
		return sql, dyn, "format"
	}

	if sql, dyn, ok := buildStringBuilderSkeleton(trimmed); ok {
		sql = trimToSqlVerb(sql)
		if sql == "" || !allowDynamicSkeleton(sql, dyn) {
			return "", dyn, "stringbuilder"
		}
		return sql, dyn, "stringbuilder"
	}

	if sql, dyn := buildConcatSkeleton(trimmed); sql != "" {
		sql = trimToSqlVerb(sql)
		if sql == "" || !allowDynamicSkeleton(sql, dyn) {
			return "", dyn, "concat"
		}
		return sql, dyn, "concat"
	}

	return "", false, "no-match"
}

func buildConcatSkeleton(expr string) (string, bool) {
	var fragments []string
	dynamic := false

	i := 0
	for i < len(expr) {
		c := expr[i]
		if isInterpolatedStart(expr, i) {
			lit, next, dynLit, ok := parseInterpolatedString(expr, i)
			if !ok {
				i++
				continue
			}
			fragments = append(fragments, lit)
			dynamic = dynamic || dynLit
			i = next
			continue
		}
		if isQuoteStart(expr, i) {
			lit, next, ok := parseCSharpStringLiteral(expr, i)
			if !ok {
				i++
				continue
			}
			fragments = append(fragments, lit)
			i = next
			continue
		}
		if unicode.IsSpace(rune(c)) || c == '+' {
			i++
			continue
		}

		dynamic = true
		fragments = append(fragments, "<expr>")
		for i < len(expr) && expr[i] != '+' {
			if isQuoteStart(expr, i) || isInterpolatedStart(expr, i) {
				break
			}
			i++
		}
	}

	if len(fragments) == 0 {
		return "", dynamic
	}

	skeleton := normalizeSqlSkeleton(strings.Join(fragments, ""))
	return skeleton, dynamic
}

func buildStringBuilderSkeleton(expr string) (string, bool, bool) {
	decls := sbDeclRe.FindAllStringSubmatch(expr, -1)
	builderNames := make(map[string]struct{})
	for _, d := range decls {
		if len(d) > 1 {
			builderNames[d[1]] = struct{}{}
		}
	}

	appendMatches := sbAppendRe.FindAllStringSubmatchIndex(expr, -1)
	if len(appendMatches) == 0 {
		return "", false, false
	}

	stopPos := len(expr)
	if loc := sbToStringRe.FindStringIndex(expr); loc != nil {
		stopPos = loc[0]
	}

	var fragments []string
	dynamic := false
	for _, m := range appendMatches {
		if len(m) < 8 {
			continue
		}
		start := m[0]
		if start >= stopPos {
			break
		}
		name := strings.TrimSpace(expr[m[2]:m[3]])
		if len(builderNames) > 0 {
			if _, ok := builderNames[name]; !ok {
				continue
			}
		}
		isLine := m[4] >= 0 && m[5] >= 0 && strings.TrimSpace(expr[m[4]:m[5]]) != ""
		arg := strings.TrimSpace(expr[m[6]:m[7]])

		frag, ok := extractPureStringLiteral(arg)
		if !ok {
			dynamic = true
			fragments = append(fragments, "<expr>")
			continue
		}
		if isLine {
			frag += "\n"
		}
		fragments = append(fragments, frag)
	}

	if len(fragments) == 0 {
		return "", dynamic, false
	}

	skeleton := normalizeSqlSkeleton(strings.Join(fragments, ""))
	return skeleton, dynamic, true
}

func extractPureStringLiteral(arg string) (string, bool) {
	trimmed := strings.TrimSpace(arg)
	if trimmed == "" {
		return "", false
	}
	if !(isQuoteStart(trimmed, 0) || isInterpolatedStart(trimmed, 0)) {
		return "", false
	}
	lit, next, dyn, ok := parseAnyStringLiteral(trimmed, 0)
	if !ok || dyn {
		return "", false
	}
	if rest := strings.TrimSpace(trimmed[next:]); rest != "" {
		return "", false
	}
	return lit, true
}

func isQuoteStart(s string, idx int) bool {
	if idx < 0 || idx >= len(s) {
		return false
	}
	if s[idx] == '"' {
		return true
	}
	if s[idx] == '@' && idx+1 < len(s) && s[idx+1] == '"' {
		return true
	}
	return false
}

func parseCSharpStringLiteral(src string, start int) (string, int, bool) {
	if start < 0 || start >= len(src) {
		return "", start, false
	}

	if src[start] == '@' && start+1 < len(src) && src[start+1] == '"' {
		return parseVerbatimString(src, start+2)
	}
	if src[start] != '"' {
		return "", start, false
	}

	var b strings.Builder
	escaped := false
	for i := start + 1; i < len(src); i++ {
		c := src[i]
		if escaped {
			switch c {
			case 'n':
				b.WriteByte('\n')
			case 'r':
				b.WriteByte('\r')
			case 't':
				b.WriteByte('\t')
			case '\\', '\'', '"':
				b.WriteByte(c)
			default:
				b.WriteByte(c)
			}
			escaped = false
			continue
		}
		if c == '\\' {
			escaped = true
			continue
		}
		if c == '"' {
			return b.String(), i + 1, true
		}
		b.WriteByte(c)
	}
	return "", start, false
}

func parseVerbatimString(src string, start int) (string, int, bool) {
	var b strings.Builder
	for i := start; i < len(src); i++ {
		c := src[i]
		if c == '"' {
			if i+1 < len(src) && src[i+1] == '"' {
				b.WriteByte('"')
				i++
				continue
			}
			return b.String(), i + 1, true
		}
		b.WriteByte(c)
	}
	return "", start, false
}

func normalizeSqlSkeleton(s string) string {
	cleaned := strings.ReplaceAll(s, "\r", "\n")
	lines := strings.Split(cleaned, "\n")
	for i, line := range lines {
		lines[i] = strings.Join(strings.Fields(line), " ")
	}
	return strings.TrimSpace(strings.Join(lines, "\n"))
}

func replaceFormatHoles(format string) (string, bool) {
	var b strings.Builder
	dynamic := false
	for i := 0; i < len(format); i++ {
		c := format[i]
		if c == '{' {
			if i+1 < len(format) && format[i+1] == '{' {
				b.WriteByte('{')
				i++
				continue
			}
			end := i + 1
			for end < len(format) && format[end] != '}' {
				end++
			}
			if end >= len(format) {
				b.WriteByte(c)
				continue
			}
			b.WriteString("<expr>")
			dynamic = true
			i = end
			continue
		}
		if c == '}' {
			if i+1 < len(format) && format[i+1] == '}' {
				b.WriteByte('}')
				i++
				continue
			}
		}
		b.WriteByte(c)
	}
	return b.String(), dynamic
}

func buildStringFormatSkeleton(expr string) (string, bool, bool) {
	m := formatCallRe.FindStringSubmatch(expr)
	if len(m) < 2 {
		return "", false, false
	}
	args := strings.TrimSpace(m[1])
	if args == "" {
		return "", false, false
	}

	firstLit, next, dyn, ok := parseAnyStringLiteral(args, 0)
	if !ok {
		return "", false, false
	}
	if dyn {
		return "", true, true
	}

	rest := strings.TrimSpace(args[next:])
	if rest != "" && !strings.HasPrefix(rest, ",") {
		return "", true, true
	}

	skel, hadHoles := replaceFormatHoles(firstLit)
	dynamic := hadHoles
	if rest != "" {
		dynamic = true
	}
	return normalizeSqlSkeleton(skel), dynamic, true
}

func buildTernarySkeleton(expr string) (string, bool, bool) {
	_, left, right, ok := splitTernary(expr)
	if !ok {
		return "", false, false
	}

	leftSkel, leftDyn, _ := BuildSqlSkeletonFromCSharpExpr(strings.TrimSpace(left))
	rightSkel, rightDyn, _ := BuildSqlSkeletonFromCSharpExpr(strings.TrimSpace(right))

	if leftSkel == "" && rightSkel == "" {
		return "", leftDyn || rightDyn, true
	}
	if leftSkel != "" && rightSkel != "" {
		if normalizeSqlSkeleton(strings.ToLower(leftSkel)) == normalizeSqlSkeleton(strings.ToLower(rightSkel)) {
			return leftSkel, true, true
		}
		return leftSkel, true, true
	}
	if leftSkel != "" {
		return leftSkel, true, true
	}
	return rightSkel, true, true
}

func splitTernary(expr string) (string, string, string, bool) {
	depth := 0
	for i := 0; i < len(expr); {
		if isInterpolatedStart(expr, i) {
			_, next, _, ok := parseInterpolatedString(expr, i)
			if !ok {
				i++
				continue
			}
			i = next
			continue
		}
		if isQuoteStart(expr, i) {
			_, next, ok := parseCSharpStringLiteral(expr, i)
			if !ok {
				i++
				continue
			}
			i = next
			continue
		}
		switch expr[i] {
		case '(':
			depth++
		case ')':
			if depth > 0 {
				depth--
			}
		case '?':
			cond := strings.TrimSpace(expr[:i])
			j := i + 1
			ternDepth := 0
			for j < len(expr) {
				if isInterpolatedStart(expr, j) {
					_, next, _, ok := parseInterpolatedString(expr, j)
					if !ok {
						j++
						continue
					}
					j = next
					continue
				}
				if isQuoteStart(expr, j) {
					_, next, ok := parseCSharpStringLiteral(expr, j)
					if !ok {
						j++
						continue
					}
					j = next
					continue
				}
				switch expr[j] {
				case '(':
					depth++
				case ')':
					if depth > 0 {
						depth--
					}
				case '?':
					ternDepth++
				case ':':
					if depth == 0 && ternDepth == 0 {
						left := strings.TrimSpace(expr[i+1 : j])
						right := strings.TrimSpace(expr[j+1:])
						if left == "" || right == "" || cond == "" {
							return "", "", "", false
						}
						return cond, left, right, true
					}
					if ternDepth > 0 {
						ternDepth--
					}
				}
				j++
			}
		}
		i++
	}
	return "", "", "", false
}

func parseAnyStringLiteral(src string, start int) (string, int, bool, bool) {
	if isInterpolatedStart(src, start) {
		return parseInterpolatedString(src, start)
	}
	lit, next, ok := parseCSharpStringLiteral(src, start)
	return lit, next, false, ok
}

func isInterpolatedStart(s string, idx int) bool {
	if idx < 0 || idx >= len(s) {
		return false
	}
	if s[idx] != '$' {
		return false
	}
	next := idx + 1
	if next < len(s) && s[next] == '@' {
		next++
	}
	return next < len(s) && s[next] == '"'
}

func parseInterpolatedString(src string, start int) (string, int, bool, bool) {
	if start < 0 || start >= len(src) || src[start] != '$' {
		return "", start, false, false
	}
	idx := start + 1
	verbatim := false
	if idx < len(src) && src[idx] == '@' {
		verbatim = true
		idx++
	}
	if idx >= len(src) || src[idx] != '"' {
		return "", start, false, false
	}
	idx++

	var b strings.Builder
	dynamic := false
	escaped := false
	for idx < len(src) {
		c := src[idx]
		if verbatim {
			if c == '"' {
				if idx+1 < len(src) && src[idx+1] == '"' {
					b.WriteByte('"')
					idx += 2
					continue
				}
				return b.String(), idx + 1, dynamic, true
			}
			if c == '{' {
				if idx+1 < len(src) && src[idx+1] == '{' {
					b.WriteByte('{')
					idx += 2
					continue
				}
				dynamic = true
				idx = skipInterpolation(src, idx+1, verbatim)
				b.WriteString("<expr>")
				continue
			}
			if c == '}' {
				if idx+1 < len(src) && src[idx+1] == '}' {
					b.WriteByte('}')
					idx += 2
					continue
				}
			}
			b.WriteByte(c)
			idx++
			continue
		}

		if escaped {
			switch c {
			case 'n':
				b.WriteByte('\n')
			case 'r':
				b.WriteByte('\r')
			case 't':
				b.WriteByte('\t')
			case '\\', '\'', '"':
				b.WriteByte(c)
			default:
				b.WriteByte(c)
			}
			escaped = false
			idx++
			continue
		}
		if c == '\\' {
			escaped = true
			idx++
			continue
		}
		if c == '"' {
			return b.String(), idx + 1, dynamic, true
		}
		if c == '{' {
			if idx+1 < len(src) && src[idx+1] == '{' {
				b.WriteByte('{')
				idx += 2
				continue
			}
			dynamic = true
			idx = skipInterpolation(src, idx+1, verbatim)
			b.WriteString("<expr>")
			continue
		}
		if c == '}' && idx+1 < len(src) && src[idx+1] == '}' {
			b.WriteByte('}')
			idx += 2
			continue
		}
		b.WriteByte(c)
		idx++
	}
	return "", start, dynamic, false
}

func skipInterpolation(src string, idx int, verbatim bool) int {
	depth := 1
	for idx < len(src) && depth > 0 {
		c := src[idx]
		if !verbatim && c == '\\' {
			idx += 2
			continue
		}
		if c == '{' {
			depth++
		} else if c == '}' {
			depth--
		}
		idx++
	}
	return idx
}

func allowDynamicSkeleton(sql string, dynamic bool) bool {
	if !dynamic {
		return true
	}
	lower := strings.ToLower(sql)
	m := dynamicTargetRe.FindStringSubmatch(lower)
	if len(m) >= 3 {
		obj := m[2]
		if strings.Contains(obj, "<expr>") || strings.Contains(obj, "[[") || strings.Contains(obj, "]]") {
			return true
		}
	}
	if strings.HasPrefix(lower, "select") {
		return false
	}
	return false
}

func trimToSqlVerb(s string) string {
	if s == "" {
		return ""
	}

	verbs := map[string]struct{}{
		"select":   {},
		"insert":   {},
		"update":   {},
		"delete":   {},
		"truncate": {},
		"exec":     {},
		"execute":  {},
		"with":     {},
	}

	tokens := strings.Fields(s)
	for i, tok := range tokens {
		base := strings.Trim(tok, "[]();,")
		base = strings.ToLower(base)
		if _, ok := verbs[base]; ok {
			return strings.Join(tokens[i:], " ")
		}
	}
	return ""
}
