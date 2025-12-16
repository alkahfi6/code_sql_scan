package scan

import (
	"bytes"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"
)

// ------------------------------------------------------------
// Config / JSON / YAML / XML / .sql
// ------------------------------------------------------------

func scanConfigFile(cfg *Config, path, relPath string) ([]SqlCandidate, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("stage=read-config lang=%s root=%q file=%q err=%w", cfg.Lang, cfg.Root, path, err)
	}
	ext := strings.ToLower(filepath.Ext(path))
	fileName := filepath.Base(path)

	var cands []SqlCandidate
	switch ext {
	case ".json":
		clean := StripJsonLineComments(string(data))
		var obj interface{}
		if err := json.Unmarshal([]byte(clean), &obj); err != nil {
			log.Printf("[WARN] stage=parse-json lang=%s root=%q file=%q err=%v", cfg.Lang, cfg.Root, path, err)
			return nil, nil
		}
		walkJSONForSQL(cfg, obj, "", relPath, fileName, &cands)
	case ".yaml", ".yml":
		scanYamlForSQL(cfg, string(data), relPath, fileName, &cands)
	case ".xml", ".config":
		content := StripXmlComments(string(data))
		scanXmlForSQL(cfg, content, relPath, fileName, &cands)
		// Additionally, attempt to extract connectionStrings mapping from .config/.xml
		extractConnectionStrings(content)
	}
	return cands, nil
}

func walkJSONForSQL(cfg *Config, node interface{}, parentKey, relPath, fileName string, cands *[]SqlCandidate) {
	switch v := node.(type) {
	case map[string]interface{}:
		for k, val := range v {
			kl := strings.ToLower(k)
			matchedKey := false
			if strings.Contains(kl, "sql") ||
				strings.Contains(kl, "query") ||
				strings.Contains(kl, "command") ||
				strings.Contains(kl, "storedprocedure") {
				matchedKey = true
			}
			if s, ok := val.(string); ok {
				// create candidate if key suggests SQL or if the string heuristically looks like SQL
				if matchedKey || looksLikeSQL(s) {
					cand := SqlCandidate{
						AppName:     cfg.AppName,
						RelPath:     relPath,
						File:        fileName,
						SourceCat:   "config",
						SourceKind:  "json",
						LineStart:   0,
						LineEnd:     0,
						Func:        "",
						RawSql:      s,
						IsDynamic:   false,
						IsExecStub:  isProcNameSpec(s),
						ConnName:    "",
						ConnDb:      "",
						DefinedPath: relPath,
						DefinedLine: 0,
					}
					*cands = append(*cands, cand)
				}
			}
			walkJSONForSQL(cfg, val, k, relPath, fileName, cands)
		}
	case []interface{}:
		for _, it := range v {
			walkJSONForSQL(cfg, it, parentKey, relPath, fileName, cands)
		}
	}
}

func scanYamlForSQL(cfg *Config, content, relPath, fileName string, cands *[]SqlCandidate) {
	lines := strings.Split(content, "\n")
	for i := 0; i < len(lines); i++ {
		rawLine := lines[i]
		line := stripYamlLineComment(rawLine)
		trimmed := strings.TrimSpace(line)
		if trimmed == "" {
			continue
		}
		if idx := strings.Index(trimmed, ":"); idx >= 0 {
			key := strings.TrimSpace(trimmed[:idx])
			val := strings.TrimSpace(trimmed[idx+1:])
			kl := strings.ToLower(key)
			matchedKey := false
			if strings.Contains(kl, "sql") ||
				strings.Contains(kl, "query") ||
				strings.Contains(kl, "command") ||
				strings.Contains(kl, "storedprocedure") {
				matchedKey = true
			}
			if matchedKey {
				if strings.HasPrefix(val, "|") || strings.HasPrefix(val, ">") {
					indent := indentation(rawLine)
					var buf bytes.Buffer
					for j := i + 1; j < len(lines); j++ {
						if indentation(lines[j]) > indent {
							buf.WriteString(strings.TrimRight(lines[j], "\r"))
							buf.WriteString("\n")
						} else {
							break
						}
					}
					sql := strings.TrimSpace(buf.String())
					if sql == "" {
						continue
					}
					if looksLikeSQL(sql) || matchedKey {
						cand := SqlCandidate{
							AppName:     cfg.AppName,
							RelPath:     relPath,
							File:        fileName,
							SourceCat:   "config",
							SourceKind:  "yaml",
							LineStart:   i + 1,
							LineEnd:     i + 1,
							Func:        "",
							RawSql:      sql,
							IsDynamic:   false,
							IsExecStub:  isProcNameSpec(sql),
							ConnName:    "",
							ConnDb:      "",
							DefinedPath: relPath,
							DefinedLine: i + 1,
						}
						*cands = append(*cands, cand)
					}
				} else if strings.HasPrefix(val, "\"") || strings.HasPrefix(val, "'") {
					un, err := strconvUnquoteSafe(val)
					if err == nil && strings.TrimSpace(un) != "" {
						if looksLikeSQL(un) || matchedKey {
							cand := SqlCandidate{
								AppName:     cfg.AppName,
								RelPath:     relPath,
								File:        fileName,
								SourceCat:   "config",
								SourceKind:  "yaml",
								LineStart:   i + 1,
								LineEnd:     i + 1,
								Func:        "",
								RawSql:      un,
								IsDynamic:   false,
								IsExecStub:  isProcNameSpec(un),
								ConnName:    "",
								ConnDb:      "",
								DefinedPath: relPath,
								DefinedLine: i + 1,
							}
							*cands = append(*cands, cand)
						}
					}
				}
			}
		}
	}
}

func stripYamlLineComment(s string) string {
	var out bytes.Buffer
	inSingle, inDouble := false, false
	for i := 0; i < len(s); i++ {
		c := s[i]
		if c == '\'' && !inDouble {
			inSingle = !inSingle
			out.WriteByte(c)
			continue
		}
		if c == '"' && !inSingle {
			inDouble = !inDouble
			out.WriteByte(c)
			continue
		}
		if c == '#' && !inSingle && !inDouble {
			break
		}
		out.WriteByte(c)
	}
	return out.String()
}

func indentation(s string) int {
	count := 0
	for _, r := range s {
		if r == ' ' || r == '\t' {
			count++
		} else {
			break
		}
	}
	return count
}

func scanXmlForSQL(cfg *Config, content, relPath, fileName string, cands *[]SqlCandidate) {
	for _, m := range regexes.xmlAttr.FindAllStringSubmatch(content, -1) {
		if len(m) >= 3 {
			raw := strings.TrimSpace(m[2])
			if raw == "" {
				continue
			}
			cand := SqlCandidate{
				AppName:     cfg.AppName,
				RelPath:     relPath,
				File:        fileName,
				SourceCat:   "config",
				SourceKind:  "xml",
				LineStart:   0,
				LineEnd:     0,
				Func:        "",
				RawSql:      raw,
				IsDynamic:   false,
				IsExecStub:  isProcNameSpec(raw),
				ConnName:    "",
				ConnDb:      "",
				DefinedPath: relPath,
				DefinedLine: 0,
			}
			*cands = append(*cands, cand)
		}
	}
	matches := regexes.xmlElem.FindAllStringSubmatch(content, -1)
	for _, m := range matches {
		if len(m) >= 3 {
			raw := strings.TrimSpace(m[2])
			if raw == "" {
				continue
			}
			cand := SqlCandidate{
				AppName:     cfg.AppName,
				RelPath:     relPath,
				File:        fileName,
				SourceCat:   "config",
				SourceKind:  "xml",
				LineStart:   0,
				LineEnd:     0,
				Func:        "",
				RawSql:      raw,
				IsDynamic:   false,
				IsExecStub:  isProcNameSpec(raw),
				ConnName:    "",
				ConnDb:      "",
				DefinedPath: relPath,
				DefinedLine: 0,
			}
			*cands = append(*cands, cand)
		}
		// handle pipe-delimited config entries like "sql: SELECT ... | conn"
		for _, m := range regexes.pipeField.FindAllStringSubmatch(content, -1) {
			if len(m) < 3 {
				continue
			}
			raw := strings.TrimSpace(m[2])
			if raw == "" {
				continue
			}
			cand := SqlCandidate{
				AppName:     cfg.AppName,
				RelPath:     relPath,
				File:        fileName,
				SourceCat:   "config",
				SourceKind:  "xml",
				LineStart:   0,
				LineEnd:     0,
				Func:        "",
				RawSql:      raw,
				IsDynamic:   false,
				IsExecStub:  isProcNameSpec(raw),
				ConnName:    "",
				ConnDb:      "",
				DefinedPath: relPath,
				DefinedLine: 0,
			}
			*cands = append(*cands, cand)
		}
	}
}

// extractConnectionStrings scans XML/config content for <connectionStrings> entries and updates
// the global connNameToDb map with ConnName->Database mapping. This function is intended to be
// invoked during scanning of .config/.xml files. It uses regex to find <add name="..."
func extractConnectionStrings(content string) {
	// regex to find <add name="ConnName" connectionString="...">
	matches := regexes.connStringAttr.FindAllStringSubmatch(content, -1)
	for _, m := range matches {
		if len(m) < 3 {
			continue
		}
		name := strings.TrimSpace(m[1])
		connStr := m[2]
		if name == "" || connStr == "" {
			continue
		}
		// parse connection string for Database or Initial Catalog
		dbName := ""
		parts := strings.Split(connStr, ";")
		for _, part := range parts {
			p := strings.TrimSpace(part)
			lower := strings.ToLower(p)
			// allow optional spaces around '='
			if strings.HasPrefix(lower, "database") {
				// find position of '='
				if idx := strings.Index(lower, "="); idx >= 0 {
					dbName = strings.TrimSpace(p[idx+1:])
				}
			} else if strings.HasPrefix(lower, "initial catalog") {
				if idx := strings.Index(lower, "="); idx >= 0 {
					dbName = strings.TrimSpace(p[idx+1:])
				}
			}
			if dbName != "" {
				break
			}
		}
		if dbName == "" {
			continue
		}
		connStore.set(name, dbName)
	}
}

func scanSqlFile(cfg *Config, path, relPath string) ([]SqlCandidate, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("stage=read-sql lang=%s root=%q file=%q err=%w", cfg.Lang, cfg.Root, path, err)
	}
	lines := strings.Split(string(data), "\n")
	var cands []SqlCandidate
	var buf bytes.Buffer
	startLine := 1

	flush := func(endLine int) {
		raw := strings.TrimSpace(buf.String())
		if raw == "" {
			return
		}
		cand := SqlCandidate{
			AppName:     cfg.AppName,
			RelPath:     relPath,
			File:        filepath.Base(path),
			SourceCat:   "script",
			SourceKind:  "sql",
			LineStart:   startLine,
			LineEnd:     endLine,
			Func:        "",
			RawSql:      raw,
			IsDynamic:   false,
			IsExecStub:  false,
			ConnName:    "",
			ConnDb:      "",
			DefinedPath: relPath,
			DefinedLine: startLine,
		}
		cands = append(cands, cand)
	}

	for i, line := range lines {
		if strings.EqualFold(strings.TrimSpace(line), "GO") {
			flush(i)
			buf.Reset()
			startLine = i + 2
		} else {
			buf.WriteString(line)
			buf.WriteString("\n")
		}
	}
	flush(len(lines))
	return cands, nil
}

// ------------------------------------------------------------
// Comment strippers
// ------------------------------------------------------------

func StripCodeCommentsCStyle(src string, isCSharp bool) string {
	var out bytes.Buffer
	const (
		stateNormal = iota
		stateLineComment
		stateBlockComment
		stateStringDouble
		stateStringBacktick
		stateStringVerbatim
	)
	state := stateNormal
	blockDepth := 0
	for i := 0; i < len(src); i++ {
		c := src[i]
		var next byte
		if i+1 < len(src) {
			next = src[i+1]
		}
		switch state {
		case stateNormal:
			if c == '/' && next == '/' {
				state = stateLineComment
				i++
				continue
			}
			if c == '/' && next == '*' {
				state = stateBlockComment
				blockDepth = 1
				i++
				continue
			}
			if c == '"' {
				if isCSharp && i > 0 && src[i-1] == '@' {
					state = stateStringVerbatim
				} else {
					state = stateStringDouble
				}
				out.WriteByte(c)
				continue
			}
			if !isCSharp && c == '`' {
				state = stateStringBacktick
				out.WriteByte(c)
				continue
			}
			out.WriteByte(c)
		case stateLineComment:
			if c == '\n' {
				state = stateNormal
				out.WriteByte(c)
			}
		case stateBlockComment:
			if c == '\n' {
				out.WriteByte(c)
			}
			if c == '/' && next == '*' {
				blockDepth++
				i++
				continue
			}
			if c == '*' && next == '/' {
				if blockDepth > 0 {
					blockDepth--
				}
				i++
				if blockDepth == 0 {
					state = stateNormal
				}
				continue
			}
		case stateStringDouble:
			out.WriteByte(c)
			if c == '\\' && i+1 < len(src) {
				out.WriteByte(src[i+1])
				i++
			} else if c == '"' {
				state = stateNormal
			}
		case stateStringBacktick:
			out.WriteByte(c)
			if c == '`' {
				state = stateNormal
			}
		case stateStringVerbatim:
			out.WriteByte(c)
			if c == '"' {
				if i+1 < len(src) && src[i+1] == '"' {
					out.WriteByte(src[i+1])
					i++
				} else {
					state = stateNormal
				}
			}
		}
	}
	return out.String()
}

func StripJsonLineComments(src string) string {
	var out bytes.Buffer
	inString := false
	escaped := false
	for i := 0; i < len(src); i++ {
		c := src[i]
		if c == '"' && !escaped {
			inString = !inString
		}
		if !inString && c == '/' && i+1 < len(src) && src[i+1] == '/' {
			i += 2
			for i < len(src) && src[i] != '\n' {
				i++
			}
			if i < len(src) {
				out.WriteByte('\n')
			}
			continue
		}
		out.WriteByte(c)
		if c == '\\' && !escaped {
			escaped = true
		} else {
			escaped = false
		}
	}
	return out.String()
}

func StripSqlComments(sql string) string {
	sql = injectLineBreakAfterDashComments(sql)
	var out bytes.Buffer
	inLine := false
	inBlock := false
	inString := false
	inBracket := false

	for i := 0; i < len(sql); i++ {
		c := sql[i]
		var next byte
		if i+1 < len(sql) {
			next = sql[i+1]
		}
		if inLine {
			if c == '\n' || c == '\r' {
				inLine = false
				out.WriteByte(c)
			}
			continue
		}
		if inBlock {
			if c == '*' && next == '/' {
				inBlock = false
				i++
			}
			continue
		}
		if !inString && !inBracket {
			if c == '-' && next == '-' {
				inLine = true
				i++
				continue
			}
			if c == '/' && next == '*' {
				inBlock = true
				i++
				continue
			}
			if c == '[' {
				inBracket = true
				out.WriteByte(c)
				continue
			}
			if c == '\'' {
				inString = true
				out.WriteByte(c)
				continue
			}
		} else if inBracket {
			out.WriteByte(c)
			if c == ']' {
				inBracket = false
			}
			continue
		} else if inString {
			out.WriteByte(c)
			if c == '\'' {
				if i+1 < len(sql) && sql[i+1] == '\'' {
					out.WriteByte(sql[i+1])
					i++
				} else {
					inString = false
				}
			}
			continue
		}
		out.WriteByte(c)
	}
	return out.String()
}

func injectLineBreakAfterDashComments(sql string) string {
	var out strings.Builder
	for i := 0; i < len(sql); i++ {
		c := sql[i]
		if c == '-' && i+1 < len(sql) && sql[i+1] == '-' {
			out.WriteString("--")
			i++
			if i+1 < len(sql) && sql[i+1] != '\n' && sql[i+1] != '\r' {
				out.WriteByte('\n')
			}
			continue
		}
		out.WriteByte(c)
	}
	return out.String()
}

// StripXmlComments menghapus <!-- ... --> tanpa mengganggu konten lain.
func StripXmlComments(src string) string {
	var out bytes.Buffer
	i := 0
	for i < len(src) {
		if i+3 < len(src) && src[i] == '<' && src[i+1] == '!' && src[i+2] == '-' && src[i+3] == '-' {
			i += 4
			for i+2 < len(src) {
				if src[i] == '-' && src[i+1] == '-' && src[i+2] == '>' {
					i += 3
					break
				}
				i++
			}
			continue
		}
		out.WriteByte(src[i])
		i++
	}
	return out.String()
}

func countLinesUpTo(s string, pos int) int {
	if pos <= 0 {
		return 1
	}
	line := 1
	for i := 0; i < pos && i < len(s); i++ {
		if s[i] == '\n' {
			line++
		}
	}
	return line
}
