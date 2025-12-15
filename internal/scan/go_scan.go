package scan

import (
	"fmt"
	"go/ast"
	"go/parser"
	"go/token"
	"path/filepath"
	"strconv"
	"strings"
)

// ------------------------------------------------------------
// Go extractor (AST-based, package-wide symtab & local vars)
// ------------------------------------------------------------

// goDbCallArgIndex maps DB gateway names to the zero-based index of the SQL argument.
// Functions that receive a context and connection first have index=2, etc.
var goDbCallArgIndex = map[string]int{
	// Standard *sql.DB methods without context
	"Query":    0,
	"Exec":     0,
	"QueryRow": 0,
	// Standard methods with context as first argument
	"QueryContext":    1,
	"ExecContext":     1,
	"QueryRowContext": 1,
	// Custom DB util functions: (ctx, conn, query [, args...])
	"QuerySingleRow":                2,
	"QueryMultipleRows":             2,
	"QueryMultipleRowsInArg":        2,
	"ExecStoredProcedure":           2,
	"ExecStoredProcedureWithReturn": 2,
	"ExecNonQuery":                  2,
	// Oracle stored procedure support: (ctx, conn, procName, args...)
	"ExecStoredProcedureOracle": 2,
}

// goDbCalls contains the set of names considered DB gateway functions.
var goDbCalls map[string]bool

func init() {
	goDbCalls = make(map[string]bool)
	for k := range goDbCallArgIndex {
		goDbCalls[k] = true
	}
}

// buildGoSymtabForDir parses all Go files in the given directory to construct
// a package-wide symbol table of constant string definitions.
// Only simple literal concatenations are resolved; dynamic expressions are skipped.
func buildGoSymtabForDir(dir, root string) map[string]SqlSymbol {
	if cached, ok := goSymtabStore.load(dir); ok {
		return cached
	}

	symtab := make(map[string]SqlSymbol)
	fset := token.NewFileSet()
	pkgs, err := parser.ParseDir(fset, dir, nil, parser.ParseComments)
	if err != nil {
		goSymtabStore.store(dir, symtab)
		return symtab
	}
	for _, pkg := range pkgs {
		for fileName, f := range pkg.Files {
			absPath := fileName
			if !filepath.IsAbs(fileName) {
				absPath = filepath.Join(dir, fileName)
			}
			relPath := ensureRelPath(root, absPath)
			ast.Inspect(f, func(n ast.Node) bool {
				switch v := n.(type) {
				case *ast.ValueSpec:
					for i, name := range v.Names {
						if name.Name == "_" {
							continue
						}
						if len(v.Values) <= i {
							continue
						}
						valExpr := v.Values[i]
						val, dyn := evalStringExpr(valExpr, nil)
						if dyn || val == "" {
							continue
						}
						pos := fset.Position(name.Pos())
						symtab[name.Name] = SqlSymbol{
							Name:       name.Name,
							Value:      val,
							RelPath:    ensureRelPath(root, relPath),
							Line:       pos.Line,
							IsComplete: true,
							IsProcSpec: isProcNameSpec(val),
						}
					}
				}
				return true
			})
		}
	}
	goSymtabStore.store(dir, symtab)
	return symtab
}

// scanGoFile analyses a single Go source file for SQL usage.
// It builds a package-wide symtab for constants and then processes each function
// individually, tracking local variables for inline query definitions.
func scanGoFile(cfg *Config, path, relPath string) ([]SqlCandidate, error) {
	fset := token.NewFileSet()
	fileAst, err := parser.ParseFile(fset, path, nil, parser.ParseComments)
	if err != nil {
		return nil, fmt.Errorf("stage=parse-go lang=%s root=%q file=%q err=%w", cfg.Lang, cfg.Root, path, err)
	}
	dir := filepath.Dir(path)
	pkgSymtab := buildGoSymtabForDir(dir, cfg.Root)

	var cands []SqlCandidate

	var isPureStringLiteral func(expr ast.Expr) bool
	isPureStringLiteral = func(expr ast.Expr) bool {
		switch v := expr.(type) {
		case *ast.BasicLit:
			return v.Kind == token.STRING
		case *ast.BinaryExpr:
			if v.Op == token.ADD {
				return isPureStringLiteral(v.X) && isPureStringLiteral(v.Y)
			}
		case *ast.ParenExpr:
			return isPureStringLiteral(v.X)
		}
		return false
	}

	var evalLiteralConcat func(expr ast.Expr) (string, bool)
	evalLiteralConcat = func(expr ast.Expr) (string, bool) {
		switch v := expr.(type) {
		case *ast.BasicLit:
			if v.Kind == token.STRING {
				s, err := strconvUnquoteSafe(v.Value)
				if err != nil {
					return v.Value, true
				}
				return s, true
			}
		case *ast.BinaryExpr:
			if v.Op == token.ADD {
				left, lok := evalLiteralConcat(v.X)
				right, rok := evalLiteralConcat(v.Y)
				if lok && rok {
					return left + right, true
				}
			}
		case *ast.ParenExpr:
			return evalLiteralConcat(v.X)
		}
		return "", false
	}

	// Helper to evaluate an expression as a potential SQL string using local and global symtabs.
	normalizeDef := func(p string) string {
		return ensureRelPath(cfg.Root, p)
	}

	var evalArg func(expr ast.Expr, localSymtab map[string]SqlSymbol, localDyn map[string]bool) (raw string, dynamic bool, defPath string, defLine int)
	evalArg = func(expr ast.Expr, localSymtab map[string]SqlSymbol, localDyn map[string]bool) (raw string, dynamic bool, defPath string, defLine int) {
		if expr == nil {
			return "", true, relPath, 0
		}
		switch v := expr.(type) {
		case *ast.BasicLit:
			if v.Kind == token.STRING {
				s, err := strconvUnquoteSafe(v.Value)
				if err != nil {
					return v.Value, true, relPath, 0
				}
				return s, false, relPath, 0
			}
		case *ast.BinaryExpr:
			if v.Op == token.ADD {
				if !isPureStringLiteral(v) {
					return "", true, relPath, 0
				}
				if combined, ok := evalLiteralConcat(v); ok {
					return combined, false, relPath, 0
				}
			}
		case *ast.Ident:
			name := v.Name
			if localSymtab != nil {
				if sym, ok := localSymtab[name]; ok {
					return sym.Value, false, normalizeDef(sym.RelPath), sym.Line
				}
				if _, ok := localDyn[name]; ok {
					return "", true, relPath, 0
				}
			}
			if sym, ok := pkgSymtab[name]; ok && sym.IsComplete {
				return sym.Value, false, normalizeDef(sym.RelPath), sym.Line
			}
			return "", true, relPath, 0
		case *ast.CallExpr:
			// handle string replacement functions like strings.ReplaceAll and strings.Replace
			// If the call is of the form strings.ReplaceAll(base, old, new) or strings.Replace(base, old, new, n)
			// then preserve the base SQL template but mark the result as dynamic because it depends on runtime
			if sel, ok := v.Fun.(*ast.SelectorExpr); ok {
				if pkgIdent, ok2 := sel.X.(*ast.Ident); ok2 && pkgIdent.Name == "strings" {
					if sel.Sel.Name == "ReplaceAll" || sel.Sel.Name == "Replace" {
						if len(v.Args) > 0 {
							// evaluate first argument to get the base constant; use local symtab for local vars
							baseRaw, baseDyn, baseDefPath, baseDefLine := evalArg(v.Args[0], localSymtab, localDyn)
							if baseRaw != "" && !baseDyn {
								// treat as dynamic but keep baseRaw to analyze objects later
								return baseRaw, true, baseDefPath, baseDefLine
							}
						}
					}
				}
				// handle fmt.Sprintf: return base format if literal, mark dynamic
				if pkgIdent, ok2 := sel.X.(*ast.Ident); ok2 && pkgIdent.Name == "fmt" && sel.Sel.Name == "Sprintf" {
					if len(v.Args) > 0 {
						baseRaw, baseDyn, baseDefPath, baseDefLine := evalArg(v.Args[0], localSymtab, localDyn)
						if baseRaw != "" && !baseDyn {
							return baseRaw, true, baseDefPath, baseDefLine
						}
					}
				}
			}
			// fallback: treat any other call expression as dynamic
			return "", true, relPath, 0
		default:
			val, dyn := evalStringExpr(expr, pkgSymtab)
			if dyn || val == "" {
				return "", true, relPath, 0
			}
			return val, false, relPath, 0
		}
		return "", true, relPath, 0
	}

	// Promote package-level SQL constants to candidates
	for _, decl := range fileAst.Decls {
		gen, ok := decl.(*ast.GenDecl)
		if !ok || gen.Tok != token.CONST {
			continue
		}
		for _, spec := range gen.Specs {
			vs, ok := spec.(*ast.ValueSpec)
			if !ok {
				continue
			}
			for i, name := range vs.Names {
				if name == nil || name.Name == "_" {
					continue
				}
				if len(vs.Values) <= i {
					continue
				}
				valExpr := vs.Values[i]
				val, dyn := evalStringExpr(valExpr, pkgSymtab)
				if dyn || val == "" {
					continue
				}
				// Heuristic: consider package-level const that looks like SQL or whose name starts with "Query"/"SQL".
				// Also include sqlc-generated *.sql.go constants that often begin with a "-- name:" comment block.
				sqlLike := looksLikeSQL(val)
				if !sqlLike {
					lowerVal := strings.ToLower(val)
					if strings.Contains(lowerVal, "-- name:") || strings.HasSuffix(strings.ToLower(path), ".sql.go") {
						sqlLike = true
					}
				}
				if !sqlLike && !(strings.HasPrefix(name.Name, "Query") || strings.HasPrefix(name.Name, "SQL") || strings.HasPrefix(name.Name, "Sql")) {
					continue
				}
				pos := fset.Position(name.Pos())
				cand := SqlCandidate{
					AppName:     cfg.AppName,
					RelPath:     relPath,
					File:        filepath.Base(path),
					SourceCat:   "code",
					SourceKind:  "go",
					LineStart:   pos.Line,
					LineEnd:     pos.Line,
					Func:        "",
					RawSql:      val,
					IsDynamic:   false,
					IsExecStub:  isProcNameSpec(val),
					ConnName:    "",
					ConnDb:      "",
					DefinedPath: relPath,
					DefinedLine: pos.Line,
				}
				cands = append(cands, cand)
			}
		}
	}

	// Process each function separately
	for _, decl := range fileAst.Decls {
		fd, ok := decl.(*ast.FuncDecl)
		if !ok || fd.Body == nil {
			continue
		}
		currentFunc := fd.Name.Name
		localSymtab := make(map[string]SqlSymbol)
		localDyn := make(map[string]bool)
		staticAssignments := make(map[string]*staticSet)

		addStaticValue := func(name string, sym SqlSymbol) {
			set := staticAssignments[name]
			if set == nil {
				set = &staticSet{}
				staticAssignments[name] = set
			}
			for _, v := range set.Values {
				if v.Value == sym.Value && v.RelPath == sym.RelPath && v.Line == sym.Line {
					return
				}
			}
			set.Values = append(set.Values, sym)
			combined := combineStaticValues(set.Values)
			base := set.Values[0]
			localSymtab[name] = SqlSymbol{
				Name:       name,
				Value:      combined,
				RelPath:    base.RelPath,
				Line:       base.Line,
				IsComplete: true,
				IsProcSpec: isProcNameSpec(combined),
			}
			delete(localDyn, name)
		}

		markDynamic := func(name string) {
			localDyn[name] = true
			delete(localSymtab, name)
			delete(staticAssignments, name)
		}

		ast.Inspect(fd.Body, func(n ast.Node) bool {
			switch v := n.(type) {
			case *ast.AssignStmt:
				// handle simple assignments (var := expr or var = expr)
				if len(v.Lhs) == 1 && len(v.Rhs) == 1 {
					if lhsIdent, ok := v.Lhs[0].(*ast.Ident); ok && lhsIdent.Name != "_" {
						name := lhsIdent.Name
						if _, dyn := localDyn[name]; dyn {
							return true
						}
						// evaluate using local and global symtab
						rawVal, dyn, defPath, defLine := evalArg(v.Rhs[0], localSymtab, localDyn)
						defPath = ensureRelPath(cfg.Root, defPath)
						if dyn || rawVal == "" {
							markDynamic(name)
						} else {
							pos := fset.Position(lhsIdent.Pos())
							line := defLine
							if line == 0 {
								line = pos.Line
							}
							if defPath == "" {
								defPath = relPath
							}
							sym := SqlSymbol{
								Name:       name,
								Value:      rawVal,
								RelPath:    defPath,
								Line:       line,
								IsComplete: true,
								IsProcSpec: isProcNameSpec(rawVal),
							}
							addStaticValue(name, sym)
						}
					}
				}
			case *ast.DeclStmt:
				// handle var declarations inside function
				if gen, ok := v.Decl.(*ast.GenDecl); ok && gen.Tok == token.VAR {
					for _, spec := range gen.Specs {
						vs, ok := spec.(*ast.ValueSpec)
						if !ok {
							continue
						}
						for i, name := range vs.Names {
							if name.Name == "_" {
								continue
							}
							if _, dyn := localDyn[name.Name]; dyn {
								continue
							}
							if len(vs.Values) <= i {
								delete(localSymtab, name.Name)
								delete(staticAssignments, name.Name)
								continue
							}
							valExpr := vs.Values[i]
							// evaluate using local and global symtab
							rawVal, dyn, defPath, defLine := evalArg(valExpr, localSymtab, localDyn)
							defPath = ensureRelPath(cfg.Root, defPath)
							if dyn || rawVal == "" {
								markDynamic(name.Name)
							} else {
								pos := fset.Position(name.Pos())
								line := defLine
								if line == 0 {
									line = pos.Line
								}
								if defPath == "" {
									defPath = relPath
								}
								sym := SqlSymbol{
									Name:       name.Name,
									Value:      rawVal,
									RelPath:    defPath,
									Line:       line,
									IsComplete: true,
									IsProcSpec: isProcNameSpec(rawVal),
								}
								addStaticValue(name.Name, sym)
							}
						}
					}
				}
			case *ast.CallExpr:
				fname, initialConn := getFuncAndReceiver(v)
				if fname == "" || !goDbCalls[fname] {
					return true
				}
				idx, ok := goDbCallArgIndex[fname]
				if !ok || len(v.Args) <= idx {
					return true
				}
				// Special handling for stored procedure helpers to ensure EXEC classification
				if fname == "ExecStoredProcedure" || fname == "ExecStoredProcedureWithReturn" {
					// Determine connection name: for custom calls, use argument before SQL parameter if available
					connName := initialConn
					if idx > 0 && len(v.Args) > idx-1 {
						if cn := getReceiverName(v.Args[idx-1]); cn != "" {
							connName = cn
						}
					}

					expr := v.Args[idx]
					rawProc, dynamicProc, defPath, defLine := evalArg(expr, localSymtab, localDyn)
					defPath = ensureRelPath(cfg.Root, defPath)
					rawSql := rawProc
					isDynamic := dynamicProc
					if rawSql == "" {
						rawSql = "[[dynamic-proc]]"
						isDynamic = true
					}

					pos := fset.Position(v.Pos())
					endPos := fset.Position(v.End())

					if !isDynamic {
						if split := splitProcSpecs(rawSql); len(split) > 1 {
							for _, proc := range split {
								cand := SqlCandidate{
									AppName:     cfg.AppName,
									RelPath:     relPath,
									File:        filepath.Base(path),
									SourceCat:   "code",
									SourceKind:  "go",
									LineStart:   pos.Line,
									LineEnd:     endPos.Line,
									Func:        currentFunc,
									RawSql:      proc,
									IsDynamic:   false,
									IsExecStub:  true,
									ConnName:    connName,
									ConnDb:      "",
									DefinedPath: defPath,
									DefinedLine: defLine,
								}
								cands = append(cands, cand)
							}
							return true
						}
					}

					cand := SqlCandidate{
						AppName:     cfg.AppName,
						RelPath:     relPath,
						File:        filepath.Base(path),
						SourceCat:   "code",
						SourceKind:  "go",
						LineStart:   pos.Line,
						LineEnd:     endPos.Line,
						Func:        currentFunc,
						RawSql:      rawSql,
						IsDynamic:   isDynamic,
						IsExecStub:  true,
						ConnName:    connName,
						ConnDb:      "",
						DefinedPath: defPath,
						DefinedLine: defLine,
					}
					cands = append(cands, cand)
					return true
				}

				// Determine connection name: for custom calls, use argument before SQL parameter if available
				connName := initialConn
				if idx > 0 && len(v.Args) > idx-1 {
					if cn := getReceiverName(v.Args[idx-1]); cn != "" {
						connName = cn
					}
				}
				expr := v.Args[idx]
				raw, dynamic, defPath, defLine := evalArg(expr, localSymtab, localDyn)
				defPath = ensureRelPath(cfg.Root, defPath)
				if raw == "" && dynamic {
					raw = "<dynamic-sql>"
				}
				isExecStub := false
				if !dynamic && isProcNameSpec(raw) {
					isExecStub = true
				}
				pos := fset.Position(v.Pos())
				endPos := fset.Position(v.End())

				if isExecStub && !dynamic {
					if split := splitProcSpecs(raw); len(split) > 1 {
						for _, proc := range split {
							cand := SqlCandidate{
								AppName:     cfg.AppName,
								RelPath:     relPath,
								File:        filepath.Base(path),
								SourceCat:   "code",
								SourceKind:  "go",
								LineStart:   pos.Line,
								LineEnd:     endPos.Line,
								Func:        currentFunc,
								RawSql:      proc,
								IsDynamic:   false,
								IsExecStub:  true,
								ConnName:    connName,
								ConnDb:      "",
								DefinedPath: defPath,
								DefinedLine: defLine,
							}
							cands = append(cands, cand)
						}
						return true
					}
				}

				cand := SqlCandidate{
					AppName:     cfg.AppName,
					RelPath:     relPath,
					File:        filepath.Base(path),
					SourceCat:   "code",
					SourceKind:  "go",
					LineStart:   pos.Line,
					LineEnd:     endPos.Line,
					Func:        currentFunc,
					RawSql:      raw,
					IsDynamic:   dynamic,
					IsExecStub:  isExecStub,
					ConnName:    connName,
					ConnDb:      "",
					DefinedPath: defPath,
					DefinedLine: defLine,
				}
				cands = append(cands, cand)
			}
			return true
		})
	}
	return cands, nil
}

// getFuncAndReceiver extracts the function name and the receiver/selector name from a call expression.
func getFuncAndReceiver(call *ast.CallExpr) (funcName, connName string) {
	var unwrap func(ast.Expr) (string, string)
	unwrap = func(expr ast.Expr) (string, string) {
		switch v := expr.(type) {
		case *ast.IndexExpr:
			return unwrap(v.X)
		case *ast.IndexListExpr:
			return unwrap(v.X)
		case *ast.SelectorExpr:
			return v.Sel.Name, getReceiverName(v.X)
		case *ast.Ident:
			return v.Name, ""
		}
		return "", ""
	}
	return unwrap(call.Fun)
}

func getReceiverName(expr ast.Expr) string {
	switch v := expr.(type) {
	case *ast.Ident:
		return v.Name
	case *ast.SelectorExpr:
		prefix := getReceiverName(v.X)
		if prefix == "" {
			return v.Sel.Name
		}
		return prefix + "." + v.Sel.Name
	case *ast.IndexExpr:
		return getReceiverName(v.X)
	case *ast.StarExpr:
		return getReceiverName(v.X)
	case *ast.CallExpr:
		return getReceiverName(v.Fun)
	}
	return ""
}

// evalStringExpr evaluates simple string expressions (literal or concatenation) for constants.
func evalStringExpr(expr ast.Expr, symtab map[string]SqlSymbol) (string, bool) {
	if expr == nil {
		return "", true
	}
	switch v := expr.(type) {
	case *ast.BasicLit:
		if v.Kind == token.STRING {
			s, err := strconvUnquoteSafe(v.Value)
			if err != nil {
				return v.Value, true
			}
			return s, false
		}
	case *ast.BinaryExpr:
		if v.Op == token.ADD {
			left, ld := evalStringExpr(v.X, symtab)
			right, rd := evalStringExpr(v.Y, symtab)
			if !ld && !rd {
				return left + right, false
			}
		}
	case *ast.Ident:
		if symtab != nil {
			if sym, ok := symtab[v.Name]; ok && sym.IsComplete {
				return sym.Value, false
			}
		}
		return "", true
	case *ast.CallExpr:
		// dynamic call like fmt.Sprintf
		return "", true
	}
	return "", true
}

func combineStaticValues(vals []SqlSymbol) string {
	if len(vals) == 0 {
		return ""
	}
	var parts []string
	for _, v := range vals {
		parts = append(parts, v.Value)
	}
	return strings.Join(parts, ";\n")
}

// splitProcSpecs separates a combined stored procedure literal (often joined by
// semicolons/newlines when multiple static assignments exist) into individual
// proc specs. Only entries that look like a proc spec are returned.
func splitProcSpecs(raw string) []string {
	if raw == "" {
		return nil
	}
	parts := strings.FieldsFunc(raw, func(r rune) bool {
		return r == ';' || r == '\n'
	})
	var out []string
	for _, p := range parts {
		val := strings.TrimSpace(p)
		if val == "" || !isProcNameSpec(val) {
			continue
		}
		out = append(out, val)
	}
	return out
}

func strconvUnquoteSafe(s string) (string, error) {
	return strconv.Unquote(s)
}
