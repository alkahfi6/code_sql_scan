package scan

import (
	"regexp"
	"strings"
	"unicode"
)

var (
	sbDeclRe     = regexp.MustCompile(`(?i)\bStringBuilder\s+([A-Za-z_][A-Za-z0-9_]*)`)
	sbAppendRe   = regexp.MustCompile(`([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*Append(Line)?\s*\(([^)]*)\)`)
	sbToStringRe = regexp.MustCompile(`([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*ToString\s*\(`)
)

// BuildSqlSkeletonFromCSharpExpr attempts to extract a deterministic SQL skeleton
// from simple C# string concatenations or StringBuilder append chains.
// It only supports limited patterns as described in the micro-mission requirements.
func BuildSqlSkeletonFromCSharpExpr(expr string) (string, bool, string) {
	trimmed := strings.TrimSpace(expr)
	if trimmed == "" {
		return "", false, "empty"
	}

	if sql, dyn, ok := buildStringBuilderSkeleton(trimmed); ok {
		sql = trimToSqlVerb(sql)
		if sql == "" {
			return "", dyn, "stringbuilder"
		}
		return sql, dyn, "stringbuilder"
	}

	if sql, dyn := buildConcatSkeleton(trimmed); sql != "" {
		sql = trimToSqlVerb(sql)
		if sql == "" {
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
			if isQuoteStart(expr, i) {
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
	if !isQuoteStart(trimmed, 0) {
		return "", false
	}
	lit, next, ok := parseCSharpStringLiteral(trimmed, 0)
	if !ok {
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
