// code_sql_scan.go
//
// Static SQL scanner for Go / .NET code + config.
//
// - Membaca folder project (-root) secara rekursif.
// - Mengabaikan komentar (Go/C#/SQL/JSON/YAML/XML).
// - Menemukan SQL / SP usage di Go, C#, XML/JSON/YAML, .sql.
// - Menganalisa DML utama, write/read, objek (DB/schema/table/proc).
// - Menghasilkan 2 CSV: QueryUsage & ObjectUsage.
//
// Engine ini murni static analysis, tidak pernah connect ke database.

package main

import (
	"bufio"
	"bytes"
	"crypto/sha1"
	"encoding/csv"
	"encoding/json"
	"flag"
	"fmt"
	"go/ast"
	"go/parser"
	"go/token"
	"io"
	"log"
	"os"
	"path/filepath"
	"regexp"
	"runtime"
	"sort"
	"strconv"
	"strings"
	"sync"
	"time"
)

// connRegistry keeps connection name -> database mappings with concurrency safety.
type connRegistry struct {
	mu   sync.RWMutex
	data map[string]string
}

func newConnRegistry() *connRegistry {
	return &connRegistry{data: make(map[string]string)}
}

func (c *connRegistry) set(name, db string) {
	c.mu.Lock()
	c.data[name] = db
	c.mu.Unlock()
}

func (c *connRegistry) get(name string) (string, bool) {
	c.mu.RLock()
	defer c.mu.RUnlock()
	db, ok := c.data[name]
	return db, ok
}

func (c *connRegistry) snapshot() map[string]string {
	c.mu.RLock()
	defer c.mu.RUnlock()
	copy := make(map[string]string, len(c.data))
	for k, v := range c.data {
		copy[k] = v
	}
	return copy
}

// goSymtabCache caches package-wide Go symbol tables per directory to avoid redundant parsing.
type goSymtabCache struct {
	mu   sync.RWMutex
	data map[string]map[string]SqlSymbol
}

func newGoSymtabCache() *goSymtabCache {
	return &goSymtabCache{data: make(map[string]map[string]SqlSymbol)}
}

func (c *goSymtabCache) load(dir string) (map[string]SqlSymbol, bool) {
	c.mu.RLock()
	defer c.mu.RUnlock()
	val, ok := c.data[dir]
	return val, ok
}

func (c *goSymtabCache) store(dir string, symtab map[string]SqlSymbol) {
	c.mu.Lock()
	c.data[dir] = symtab
	c.mu.Unlock()
}

var (
	connStore     = newConnRegistry()
	goSymtabStore = newGoSymtabCache()
)

// ------------------------------------------------------------
// Konfigurasi & tipe data utama
// ------------------------------------------------------------

type Config struct {
	Root        string
	AppName     string
	Lang        string
	OutQuery    string
	OutObject   string
	MaxFileSize int64
	Workers     int
	IncludeExt  map[string]struct{}
}

type SqlSymbol struct {
	Name       string
	Value      string
	RelPath    string
	Line       int
	IsComplete bool
	IsProcSpec bool
}

type SqlCandidate struct {
	AppName     string
	RelPath     string
	File        string
	SourceCat   string // code / config / script
	SourceKind  string // go / csharp / xml / yaml / json / sql
	LineStart   int
	LineEnd     int
	Func        string
	RawSql      string
	SqlClean    string
	UsageKind   string // SELECT/INSERT/UPDATE/DELETE/TRUNCATE/EXEC/UNKNOWN
	IsWrite     bool
	IsDynamic   bool
	IsExecStub  bool
	ConnName    string
	ConnDb      string
	DefinedPath string
	DefinedLine int
	// Analisis objek
	HasCrossDb bool
	DbList     []string
	Objects    []ObjectToken
	// Flags
	QueryHash string
	RiskLevel string
}

type ObjectToken struct {
	DbName             string
	SchemaName         string
	BaseName           string
	FullName           string
	Role               string // target/source/exec
	DmlKind            string // SELECT/INSERT/...
	IsWrite            bool
	IsCrossDb          bool
	IsLinkedServer     bool
	IsObjectNameDyn    bool
	RepresentativeLine int
}

// CSV row untuk QueryUsage
type QueryUsageRow struct {
	AppName     string
	RelPath     string
	File        string
	SourceCat   string
	SourceKind  string
	LineStart   int
	LineEnd     int
	Func        string
	RawSql      string
	SqlClean    string
	UsageKind   string
	IsWrite     bool
	HasCrossDb  bool
	DbList      string
	ObjectCount int
	IsDynamic   bool
	ConnName    string
	ConnDb      string
	QueryHash   string
	RiskLevel   string
	DefinedPath string
	DefinedLine int
}

// CSV row untuk ObjectUsage
type ObjectUsageRow struct {
	AppName         string
	RelPath         string
	File            string
	SourceCat       string
	SourceKind      string
	Line            int
	Func            string
	QueryHash       string
	ObjectName      string
	DbName          string
	SchemaName      string
	BaseName        string
	IsCrossDb       bool
	IsLinkedServer  bool
	Role            string
	DmlKind         string
	IsWrite         bool
	IsObjectNameDyn bool
}

// ------------------------------------------------------------
// Main & arg parsing
// ------------------------------------------------------------

func main() {
	log.SetFlags(log.LstdFlags | log.Lmicroseconds)

	cfg := parseFlags()
	start := time.Now()
	log.Printf("[INFO] starting scan root=%s app=%s lang=%s workers=%d maxSize=%d",
		cfg.Root, cfg.AppName, cfg.Lang, cfg.Workers, cfg.MaxFileSize)

	pathCh, countCh := streamFiles(cfg)
	cands := runWorkers(cfg, pathCh)
	fileCount := <-countCh
	log.Printf("[INFO] total files to scan: %d", fileCount)

	for i := range cands {
		analyzeCandidate(&cands[i])
		// sort objects inside candidate for deterministic output
		if len(cands[i].Objects) > 1 {
			sort.Slice(cands[i].Objects, func(a, b int) bool {
				oa, ob := cands[i].Objects[a], cands[i].Objects[b]
				if oa.DbName != ob.DbName {
					return oa.DbName < ob.DbName
				}
				if oa.SchemaName != ob.SchemaName {
					return oa.SchemaName < ob.SchemaName
				}
				if oa.BaseName != ob.BaseName {
					return oa.BaseName < ob.BaseName
				}
				if oa.Role != ob.Role {
					return oa.Role < ob.Role
				}
				return oa.DmlKind < ob.DmlKind
			})
		}
	}

	// sort candidates deterministically by path, line and hash
	sort.Slice(cands, func(i, j int) bool {
		a, b := cands[i], cands[j]
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.LineStart != b.LineStart {
			return a.LineStart < b.LineStart
		}
		if a.QueryHash != b.QueryHash {
			return a.QueryHash < b.QueryHash
		}
		return a.SourceKind < b.SourceKind
	})

	if err := writeCSVs(cfg, cands); err != nil {
		log.Fatalf("[FATAL] write CSV failed: %v", err)
	}

	log.Printf("[INFO] done in %s", time.Since(start))
}

func parseFlags() *Config {
	root := flag.String("root", "", "root path project")
	app := flag.String("app", "", "application name")
	lang := flag.String("lang", "", "language mode: go | dotnet")
	outQ := flag.String("out-query", "", "output CSV for QueryUsage")
	outO := flag.String("out-object", "", "output CSV for ObjectUsage")
	maxSize := flag.Int64("max-size", 10*1024*1024, "max file size in bytes")
	workers := flag.Int("workers", 4, "number of workers")
	includeExt := flag.String("include-ext", "", "additional extensions, comma-separated, e.g. .cshtml,.razor")

	flag.Parse()

	if *root == "" || *app == "" || *lang == "" || *outQ == "" || *outO == "" {
		flag.Usage()
		os.Exit(1)
	}

	l := strings.ToLower(*lang)
	if l == "cs" || l == "csharp" {
		l = "dotnet"
	}
	if l != "go" && l != "dotnet" {
		log.Fatalf("invalid -lang value: %s (must be go or dotnet)", *lang)
	}

	inc := make(map[string]struct{})
	if *includeExt != "" {
		for _, e := range strings.Split(*includeExt, ",") {
			e = strings.TrimSpace(e)
			if e == "" {
				continue
			}
			if !strings.HasPrefix(e, ".") {
				e = "." + e
			}
			inc[strings.ToLower(e)] = struct{}{}
		}
	}

	// clamp workers to a reasonable range (1–32). if zero or negative, default to NumCPU.
	w := *workers
	if w <= 0 {
		if n := runtime.NumCPU(); n > 0 {
			w = n
		} else {
			w = 4
		}
	}
	if w < 1 {
		w = 1
	}
	if w > 32 {
		w = 32
	}
	return &Config{
		Root:        *root,
		AppName:     *app,
		Lang:        l,
		OutQuery:    *outQ,
		OutObject:   *outO,
		MaxFileSize: *maxSize,
		Workers:     w,
		IncludeExt:  inc,
	}
}

// ------------------------------------------------------------
// File discovery & filtering
// ------------------------------------------------------------

var skipDirs = []string{
	"bin", "obj", "dist", "out", "target", "node_modules", "packages", "vendor",
}

func streamFiles(cfg *Config) (<-chan string, <-chan int) {
	paths := make(chan string, cfg.Workers*2)
	count := make(chan int, 1)
	go func() {
		defer close(paths)
		defer close(count)
		total := 0
		err := filepath.WalkDir(cfg.Root, func(path string, d os.DirEntry, err error) error {
			if err != nil {
				log.Printf("[WARN] walk error on %s: %v", path, err)
				return nil
			}
			if d.IsDir() {
				name := strings.ToLower(d.Name())
				for _, s := range skipDirs {
					if name == s {
						return filepath.SkipDir
					}
				}
				return nil
			}
			ext := strings.ToLower(filepath.Ext(path))
			if !isAllowedExt(cfg, ext) {
				return nil
			}
			total++
			paths <- path
			return nil
		})
		if err != nil {
			log.Printf("[WARN] walkdir error: %v", err)
		}
		count <- total
	}()
	return paths, count
}

func isAllowedExt(cfg *Config, ext string) bool {
	if ext == "" {
		return false
	}
	switch cfg.Lang {
	case "go":
		switch ext {
		case ".go", ".xml", ".config", ".json", ".yaml", ".yml", ".sql":
			return true
		}
	case "dotnet":
		switch ext {
		case ".cs", ".xml", ".config", ".json", ".yaml", ".yml", ".sql":
			return true
		}
	}
	if _, ok := cfg.IncludeExt[ext]; ok {
		return true
	}
	return false
}

// ------------------------------------------------------------
// Worker pool
// ------------------------------------------------------------

func runWorkers(cfg *Config, paths <-chan string) []SqlCandidate {
	jobs := make(chan string, cfg.Workers*2)
	results := make(chan []SqlCandidate, cfg.Workers*2)

	var wg sync.WaitGroup
	workers := cfg.Workers
	if workers <= 0 {
		workers = 4
	}

	for i := 0; i < workers; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			defer func() {
				if r := recover(); r != nil {
					log.Printf("[ERROR] worker %d panic: %v", id, r)
				}
			}()
			for path := range jobs {
				cs, err := scanFile(cfg, path)
				if err != nil {
					log.Printf("[WARN] scan file %s: %v", path, err)
					continue
				}
				if len(cs) > 0 {
					results <- cs
				}
			}
		}(i + 1)
	}

	go func() {
		for p := range paths {
			jobs <- p
		}
		close(jobs)
		wg.Wait()
		close(results)
	}()

	var all []SqlCandidate
	for batch := range results {
		all = append(all, batch...)
	}
	return all
}

// ------------------------------------------------------------
// File-level scanner dispatcher
// ------------------------------------------------------------

func scanFile(cfg *Config, path string) ([]SqlCandidate, error) {
	info, err := os.Stat(path)
	if err != nil {
		return nil, err
	}
	if info.Size() > cfg.MaxFileSize {
		log.Printf("[INFO] skip too large file: %s (%d bytes)", path, info.Size())
		return nil, nil
	}
	if isBinaryFile(path) {
		log.Printf("[INFO] skip binary file: %s", path)
		return nil, nil
	}

	ext := strings.ToLower(filepath.Ext(path))
	relPath, _ := filepath.Rel(cfg.Root, path)

	switch ext {
	case ".go":
		if cfg.Lang != "go" {
			return nil, nil
		}
		return scanGoFile(cfg, path, relPath)
	case ".cs":
		if cfg.Lang != "dotnet" {
			return nil, nil
		}
		return scanCsFile(cfg, path, relPath)
	case ".xml", ".config", ".json", ".yaml", ".yml":
		return scanConfigFile(cfg, path, relPath)
	case ".sql":
		return scanSqlFile(cfg, path, relPath)
	default:
		return nil, nil
	}
}

// ------------------------------------------------------------
// Binary sniff
// ------------------------------------------------------------

func isBinaryFile(path string) bool {
	f, err := os.Open(path)
	if err != nil {
		return true
	}
	defer f.Close()
	buf := make([]byte, 2048)
	n, err := f.Read(buf)
	if err != nil && err != io.EOF {
		return true
	}
	ctrl := 0
	for i := 0; i < n; i++ {
		b := buf[i]
		if b == 0 {
			return true
		}
		if b < 0x09 {
			ctrl++
			if ctrl > 5 {
				return true
			}
		}
	}
	return false
}

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
			absPath := filepath.Join(dir, fileName)
			relPath, errRel := filepath.Rel(root, absPath)
			if errRel != nil {
				relPath = absPath
			}
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
							RelPath:    relPath,
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
	return symtab
}

// scanGoFile analyses a single Go source file for SQL usage.
// It builds a package-wide symtab for constants and then processes each function
// individually, tracking local variables for inline query definitions.
func scanGoFile(cfg *Config, path, relPath string) ([]SqlCandidate, error) {
	fset := token.NewFileSet()
	fileAst, err := parser.ParseFile(fset, path, nil, parser.ParseComments)
	if err != nil {
		return nil, err
	}
	dir := filepath.Dir(path)
	pkgSymtab := buildGoSymtabForDir(dir, cfg.Root)

	var cands []SqlCandidate

	// Helper to evaluate an expression as a potential SQL string using local and global symtabs.
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
				left, ld, _, _ := evalArg(v.X, localSymtab, localDyn)
				right, rd, _, _ := evalArg(v.Y, localSymtab, localDyn)
				if !ld && !rd {
					return left + right, false, relPath, 0
				}
			}
		case *ast.Ident:
			name := v.Name
			if localSymtab != nil {
				if sym, ok := localSymtab[name]; ok {
					return sym.Value, false, sym.RelPath, sym.Line
				}
				if _, ok := localDyn[name]; ok {
					return "", true, relPath, 0
				}
			}
			if sym, ok := pkgSymtab[name]; ok && sym.IsComplete {
				return sym.Value, false, sym.RelPath, sym.Line
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

	// Process each function separately
	for _, decl := range fileAst.Decls {
		fd, ok := decl.(*ast.FuncDecl)
		if !ok || fd.Body == nil {
			continue
		}
		currentFunc := fd.Name.Name
		localSymtab := make(map[string]SqlSymbol)
		localDyn := make(map[string]bool)

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
						rawVal, dyn, _, _ := evalArg(v.Rhs[0], localSymtab, localDyn)
						if dyn || rawVal == "" {
							delete(localSymtab, name)
							localDyn[name] = true
						} else {
							if _, exists := localSymtab[name]; exists {
								delete(localSymtab, name)
								localDyn[name] = true
							} else {
								pos := fset.Position(lhsIdent.Pos())
								localSymtab[name] = SqlSymbol{
									Name:       name,
									Value:      rawVal,
									RelPath:    relPath,
									Line:       pos.Line,
									IsComplete: true,
									IsProcSpec: isProcNameSpec(rawVal),
								}
							}
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
								localDyn[name.Name] = true
								delete(localSymtab, name.Name)
								continue
							}
							valExpr := vs.Values[i]
							// evaluate using local and global symtab
							rawVal, dyn, _, _ := evalArg(valExpr, localSymtab, localDyn)
							if dyn || rawVal == "" {
								delete(localSymtab, name.Name)
								localDyn[name.Name] = true
							} else {
								if _, exists := localSymtab[name.Name]; exists {
									delete(localSymtab, name.Name)
									localDyn[name.Name] = true
								} else {
									pos := fset.Position(name.Pos())
									localSymtab[name.Name] = SqlSymbol{
										Name:       name.Name,
										Value:      rawVal,
										RelPath:    relPath,
										Line:       pos.Line,
										IsComplete: true,
										IsProcSpec: isProcNameSpec(rawVal),
									}
								}
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
				// Determine connection name: for custom calls, use argument before SQL parameter if available
				connName := initialConn
				if idx > 0 && len(v.Args) > idx-1 {
					if cn := getReceiverName(v.Args[idx-1]); cn != "" {
						connName = cn
					}
				}
				expr := v.Args[idx]
				raw, dynamic, defPath, defLine := evalArg(expr, localSymtab, localDyn)
				if raw == "" && dynamic {
					raw = "<dynamic-sql>"
				}
				isExecStub := false
				if !dynamic && isProcNameSpec(raw) {
					isExecStub = true
				}
				pos := fset.Position(v.Pos())
				endPos := fset.Position(v.End())
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
	switch fun := call.Fun.(type) {
	case *ast.SelectorExpr:
		funcName = fun.Sel.Name
		connName = getReceiverName(fun.X)
		return
	case *ast.Ident:
		funcName = fun.Name
		return
	}
	return "", ""
}

func getReceiverName(expr ast.Expr) string {
	switch v := expr.(type) {
	case *ast.Ident:
		return v.Name
	case *ast.SelectorExpr:
		return v.Sel.Name
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

func strconvUnquoteSafe(s string) (string, error) {
	return strconv.Unquote(s)
}

// ------------------------------------------------------------
// C# extractor (regex-based)
// ------------------------------------------------------------

func scanCsFile(cfg *Config, path, relPath string) ([]SqlCandidate, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}
	src := string(data)
	clean := StripCodeCommentsCStyle(src, true)

	lines := strings.Split(src, "\n")
	methodAtLine := detectCsMethods(lines)

	var cands []SqlCandidate

	execProcLit := regexp.MustCompile(`(?i)(\w+)\s*\.\s*ExecProc\s*\(\s*"([^"]+)"`)
	execProcDyn := regexp.MustCompile(`(?i)(\w+)\s*\.\s*ExecProc\s*\(\s*([^),]+)`)
	newCmd := regexp.MustCompile(`(?i)new\s+SqlCommand\s*\(\s*"([^"]+)"\s*,\s*([^)]+?)\)`) // group1=sql, group2=conn
	// Dapper-style Query<T>("SQL", ...) and Execute("SQL", ...)
	dapperQuery := regexp.MustCompile(`(?i)\.\s*Query(?:Async)?(?:<[^>]*>)?\s*\(\s*"([^"]+)"`)
	dapperExec := regexp.MustCompile(`(?i)\.\s*Execute(?:Async)?\s*\(\s*"([^"]+)"`)
	// EF Core raw SQL
	efFromSql := regexp.MustCompile(`(?i)\.\s*FromSqlRaw\s*\(\s*"([^"]+)"`)
	efExecRaw := regexp.MustCompile(`(?i)\.\s*ExecuteSqlRaw\s*\(\s*"([^"]+)"`)
	// Additional helpers: ExecuteQuery(conn, sql, ...)
	execQuery := regexp.MustCompile(`(?i)\.\s*ExecuteQuery\s*\(\s*[^,]+,\s*"([^"]+)"`)
	// CallQueryFromWs(url, ignoreSSL, sql, ...)
	callQueryWsLit := regexp.MustCompile(`(?i)\.\s*CallQueryFromWs\s*\(\s*[^,]+,\s*[^,]+,\s*"([^"]+)"`)
	callQueryWsDyn := regexp.MustCompile(`(?i)\.\s*CallQueryFromWs\s*\(\s*[^,]+,\s*[^,]+,\s*([^),]+)`)

	// CommandText assignment: cmd.CommandText = "ProcName"
	commandTextLit := regexp.MustCompile(`(?i)\.\s*CommandText\s*=\s*"([^"]+)"`)

	type pat struct {
		re       *regexp.Regexp
		execStub bool
		dynamic  bool
	}
	patterns := []pat{
		{execProcLit, true, false},
		{execProcDyn, true, true},
		{newCmd, false, false},
		{dapperQuery, false, false},
		{dapperExec, false, false},
		{efFromSql, false, false},
		{efExecRaw, false, false},
		{execQuery, false, false},      // ExecuteQuery(conn, "SQL")
		{callQueryWsLit, false, false}, // CallQueryFromWs with literal SQL
		{callQueryWsDyn, false, true},  // CallQueryFromWs with dynamic SQL expression
		{commandTextLit, false, false}, // CommandText = "ProcName"
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

			switch p.re {
			case newCmd:
				// group1 = SQL, group2 = conn
				raw = cleanedGroup(clean, m, 1)
				connName = strings.TrimSpace(cleanedGroup(clean, m, 2))
			case execProcLit, execProcDyn:
				// group1 = conn, group2 = arg (SP literal or expr)
				connName = strings.TrimSpace(cleanedGroup(clean, m, 1))
				raw = cleanedGroup(clean, m, 2)
				if raw == "" && p.dynamic {
					raw = "<dynamic-proc>"
				}
			case callQueryWsLit, callQueryWsDyn:
				// group1 = SQL or expression
				raw = cleanedGroup(clean, m, 1)
			default:
				// Dapper / EF / ExecuteQuery / CommandText: group1 = SQL
				raw = cleanedGroup(clean, m, 1)
			}

			if raw == "" {
				continue
			}
			funcName := ""
			if line-1 >= 0 && line-1 < len(methodAtLine) {
				funcName = methodAtLine[line-1]
			}
			isDyn := p.dynamic
			// mark dynamic if raw contains interpolations or variables
			if !p.dynamic {
				if strings.Contains(raw, "$") || (strings.Contains(raw, "{") && strings.Contains(raw, "}")) {
					isDyn = true
				}
			}
			// Determine exec stub: if pattern flagged or the raw string looks like a proc name spec
			isExecStub := p.execStub
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
				LineStart:   line,
				LineEnd:     line,
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

func detectCsMethods(lines []string) []string {
	methodAtLine := make([]string, len(lines))
	current := ""
	re := regexp.MustCompile(`(?i)\b(public|private|protected|internal|static|async|sealed|override|virtual|partial)\b[^{]*\b([A-Za-z_][A-Za-z0-9_]*)\s*\(`)
	for i, line := range lines {
		trimmed := strings.TrimSpace(line)
		if trimmed == "" {
			methodAtLine[i] = current
			continue
		}
		if m := re.FindStringSubmatch(trimmed); len(m) >= 3 {
			current = m[2]
		}
		methodAtLine[i] = current
	}
	return methodAtLine
}

// ------------------------------------------------------------
// Config / JSON / YAML / XML / .sql
// ------------------------------------------------------------

func scanConfigFile(cfg *Config, path, relPath string) ([]SqlCandidate, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}
	ext := strings.ToLower(filepath.Ext(path))
	fileName := filepath.Base(path)

	var cands []SqlCandidate
	switch ext {
	case ".json":
		clean := StripJsonLineComments(string(data))
		var obj interface{}
		if err := json.Unmarshal([]byte(clean), &obj); err != nil {
			log.Printf("[WARN] json parse failed on %s: %v", path, err)
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
	reAttr := regexp.MustCompile(`(?i)(sql|query|command|commandtext|storedprocedure)\s*=\s*"([^"]+)"`)
	for _, m := range reAttr.FindAllStringSubmatch(content, -1) {
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
	reElem := regexp.MustCompile(`(?i)<\s*(sql|query|command|commandtext|storedprocedure)[^>]*>(.*?)<\s*/\s*\1\s*>`)
	matches := reElem.FindAllStringSubmatch(content, -1)
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
	}
}

// extractConnectionStrings scans XML/config content for <connectionStrings> entries and updates
// the global connNameToDb map with ConnName->Database mapping. This function is intended to be
// invoked during scanning of .config/.xml files. It uses regex to find <add name="..."
func extractConnectionStrings(content string) {
	// regex to find <add name="ConnName" connectionString="...">
	re := regexp.MustCompile(`(?i)<\s*add\s+[^>]*name\s*=\s*"([^"]+)"[^>]*connectionString\s*=\s*"([^"]+)"[^>]*>`)
	matches := re.FindAllStringSubmatch(content, -1)
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
		return nil, err
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
			if c == '*' && next == '/' {
				state = stateNormal
				i++
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
			if c == '\n' {
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

// ------------------------------------------------------------
// SQL usage analysis (DML, objek, cross-DB)
// ------------------------------------------------------------

func isProcNameSpec(s string) bool {
	l := strings.ToLower(strings.TrimSpace(s))
	if l == "" {
		return false
	}
	kw := []string{"select", "insert", "update", "delete", "truncate", "exec"}
	for _, k := range kw {
		if strings.Contains(l, k) {
			return false
		}
	}
	if strings.ContainsAny(l, " \t\r\n") {
		return false
	}
	if strings.Contains(s, "[[") || strings.Contains(s, "]]") || strings.ContainsAny(s, "?:") {
		return true
	}
	return true
}

func analyzeCandidate(c *SqlCandidate) {
	sqlClean := StripSqlComments(c.RawSql)
	sqlClean = strings.TrimSpace(sqlClean)
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

	tokens := findObjectTokens(sqlClean)
	classifyObjects(c, usage, tokens)

	// If Exec stub and no tokens, interpret RawSql as proc spec
	if c.IsExecStub && len(c.Objects) == 0 && strings.TrimSpace(c.RawSql) != "" {
		tok := parseProcNameSpec(c.RawSql)
		if c.ConnDb != "" {
			tok.IsCrossDb = tok.DbName != "" && !strings.EqualFold(tok.DbName, c.ConnDb)
		} else {
			tok.IsCrossDb = tok.DbName != ""
		}
		tok.Role = "exec"
		tok.DmlKind = "EXEC"
		tok.IsWrite = true
		tok.RepresentativeLine = c.LineStart
		if tok.SchemaName == "" && tok.BaseName != "" && tok.DbName != "" {
			tok.SchemaName = "dbo"
		}
		c.Objects = []ObjectToken{tok}
	}

	dbSet := make(map[string]struct{})
	hasCross := false
	for i := range c.Objects {
		obj := &c.Objects[i]
		if obj.DbName != "" {
			dbSet[obj.DbName] = struct{}{}
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
	c.DbList = dbList
	c.HasCrossDb = hasCross

	hashInput := c.SqlClean
	if hashInput == "" {
		hashInput = c.RawSql
	}
	if hashInput == "" {
		hashInput = fmt.Sprintf("%s:%d:%d:%s", c.RelPath, c.LineStart, c.LineEnd, c.Func)
	}
	h := sha1.Sum([]byte(hashInput))
	c.QueryHash = fmt.Sprintf("%x", h[:])

	c.RiskLevel = classifyRisk(c)
}

func detectUsageKind(isExecStub bool, sql string) string {
	if isExecStub {
		return "EXEC"
	}
	if sql == "" {
		return "UNKNOWN"
	}
	l := strings.ToLower(strings.TrimSpace(sql))
	r := bufio.NewReader(strings.NewReader(l))
	word, _ := r.ReadString(' ')
	word = strings.TrimSpace(word)

	switch {
	case strings.HasPrefix(word, "select"):
		return "SELECT"
	case strings.HasPrefix(word, "insert"):
		return "INSERT"
	case strings.HasPrefix(word, "update"):
		return "UPDATE"
	case strings.HasPrefix(word, "delete"):
		return "DELETE"
	case strings.HasPrefix(word, "truncate"):
		return "TRUNCATE"
	case strings.HasPrefix(word, "exec"), strings.HasPrefix(word, "execute"):
		return "EXEC"
	default:
		return "UNKNOWN"
	}
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
			if objText == "" {
				start = end
				continue
			}
			dbName, schemaName, baseName, isLinked := splitObjectNameParts(objText)
			tokens = append(tokens, ObjectToken{
				DbName:         dbName,
				SchemaName:     schemaName,
				BaseName:       baseName,
				FullName:       objText,
				IsLinkedServer: isLinked,
			})
			start = end
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
		b == '_' || b == '.' || b == '$'
}

func splitObjectNameParts(full string) (db, schema, base string, isLinked bool) {
	full = strings.TrimSpace(full)
	if full == "" {
		return "", "", "", false
	}
	unbracket := func(s string) string {
		s = strings.TrimSpace(s)
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

func classifyObjects(c *SqlCandidate, usageKind string, tokens []ObjectToken) {
	// Determine cross-DB for each token first
	for i := range tokens {
		tokens[i].IsCrossDb = tokens[i].DbName != "" && c.ConnDb != "" && !strings.EqualFold(tokens[i].DbName, c.ConnDb)
		if c.ConnDb == "" && tokens[i].DbName != "" {
			tokens[i].IsCrossDb = true
		}
	}
	// Normalize SQL to lower case for position lookup
	sqlLower := strings.ToLower(c.SqlClean)
	// Compute first occurrence index for each token in the SQL
	positions := make([]int, len(tokens))
	for i := range tokens {
		// search using the token's full name in lower case
		nameLower := strings.ToLower(strings.TrimSpace(tokens[i].FullName))
		idx := strings.Index(sqlLower, nameLower)
		if idx < 0 {
			idx = len(sqlLower) + i // if not found, push to end
		}
		positions[i] = idx
	}
	// Initialize defaults: assume all tokens are sources with UNKNOWN
	for i := range tokens {
		tokens[i].Role = "source"
		tokens[i].DmlKind = "UNKNOWN"
		tokens[i].IsWrite = false
		tokens[i].RepresentativeLine = c.LineStart
	}
	// Determine target based on DML keyword position
	// We'll search for the relevant keyword and then assign the first token whose position is >= keyword position
	var targetIdx int = -1
	switch usageKind {
	case "INSERT":
		// find position of "insert" and "into"
		kw := "insert"
		posInsert := strings.Index(sqlLower, kw)
		if posInsert >= 0 {
			// choose token after position of "insert"
			minPos := len(sqlLower) + 1
			for i, p := range positions {
				if p >= posInsert && p < minPos {
					targetIdx = i
					minPos = p
				}
			}
		}
	case "UPDATE":
		kw := "update"
		pos := strings.Index(sqlLower, kw)
		if pos >= 0 {
			minPos := len(sqlLower) + 1
			for i, p := range positions {
				if p >= pos && p < minPos {
					targetIdx = i
					minPos = p
				}
			}
		}
	case "DELETE":
		// support "delete from" or "delete"
		pos := strings.Index(sqlLower, "delete")
		if pos >= 0 {
			minPos := len(sqlLower) + 1
			for i, p := range positions {
				if p >= pos && p < minPos {
					targetIdx = i
					minPos = p
				}
			}
		}
	case "TRUNCATE":
		pos := strings.Index(sqlLower, "truncate")
		if pos >= 0 {
			minPos := len(sqlLower) + 1
			for i, p := range positions {
				if p >= pos && p < minPos {
					targetIdx = i
					minPos = p
				}
			}
		}
	}
	// Assign role and DmlKind based on usageKind
	switch usageKind {
	case "SELECT":
		for i := range tokens {
			tokens[i].Role = "source"
			tokens[i].DmlKind = "SELECT"
			tokens[i].IsWrite = false
		}
	case "INSERT":
		for i := range tokens {
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
			if i == targetIdx {
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
	// Mark dynamic object names
	for i := range tokens {
		full := tokens[i].FullName
		// detect patterns indicating dynamic names: [[placeholder]], ?, :, or @ variable
		if strings.Contains(full, "[[") || strings.Contains(full, "]]") ||
			strings.Contains(full, "?") || strings.Contains(full, ":") ||
			strings.Contains(full, "@") {
			tokens[i].IsObjectNameDyn = true
		}
		tokens[i].RepresentativeLine = c.LineStart
	}
	c.Objects = tokens
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
	db, schema, base, isLinked := splitObjectNameParts(trimmed)
	dyn := false
	if strings.Contains(trimmed, "[[") || strings.Contains(trimmed, "]]") || strings.ContainsAny(trimmed, "?:") {
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

// ------------------------------------------------------------
// CSV output
// ------------------------------------------------------------

func writeCSVs(cfg *Config, cands []SqlCandidate) error {
	qf, err := os.Create(cfg.OutQuery)
	if err != nil {
		return err
	}
	defer qf.Close()
	of, err := os.Create(cfg.OutObject)
	if err != nil {
		return err
	}
	defer of.Close()

	qw := csv.NewWriter(qf)
	ow := csv.NewWriter(of)

	qHeader := []string{
		"AppName", "RelPath", "File", "SourceCategory", "SourceKind",
		"LineStart", "LineEnd", "Func", "RawSql", "SqlClean",
		"UsageKind", "IsWrite", "HasCrossDb", "DbList", "ObjectCount",
		"IsDynamic", "ConnName", "ConnDb", "QueryHash", "RiskLevel",
		"DefinedInRelPath", "DefinedInLine",
	}
	if err := qw.Write(qHeader); err != nil {
		return err
	}

	oHeader := []string{
		"AppName", "RelPath", "File", "SourceCategory", "SourceKind",
		"Line", "Func", "QueryHash", "ObjectName",
		"DbName", "SchemaName", "BaseName",
		"IsCrossDb", "IsLinkedServer", "Role", "DmlKind",
		"IsWrite", "IsObjectNameDynamic",
	}
	if err := ow.Write(oHeader); err != nil {
		return err
	}

	for _, c := range cands {
		dbList := strings.Join(c.DbList, ",")

		qRow := []string{
			c.AppName,
			c.RelPath,
			c.File,
			c.SourceCat,
			c.SourceKind,
			fmt.Sprintf("%d", c.LineStart),
			fmt.Sprintf("%d", c.LineEnd),
			c.Func,
			c.RawSql,
			c.SqlClean,
			c.UsageKind,
			boolToStr(c.IsWrite),
			boolToStr(c.HasCrossDb),
			dbList,
			fmt.Sprintf("%d", len(c.Objects)),
			boolToStr(c.IsDynamic),
			c.ConnName,
			c.ConnDb,
			c.QueryHash,
			c.RiskLevel,
			c.DefinedPath,
			fmt.Sprintf("%d", c.DefinedLine),
		}
		if err := qw.Write(qRow); err != nil {
			return err
		}

		for _, o := range c.Objects {
			full := o.FullName
			if full == "" {
				full = buildFullName(o.DbName, o.SchemaName, o.BaseName)
			}
			oRow := []string{
				c.AppName,
				c.RelPath,
				c.File,
				c.SourceCat,
				c.SourceKind,
				fmt.Sprintf("%d", o.RepresentativeLine),
				c.Func,
				c.QueryHash,
				full,
				o.DbName,
				o.SchemaName,
				o.BaseName,
				boolToStr(o.IsCrossDb),
				boolToStr(o.IsLinkedServer),
				o.Role,
				o.DmlKind,
				boolToStr(o.IsWrite),
				boolToStr(o.IsObjectNameDyn),
			}
			if err := ow.Write(oRow); err != nil {
				return err
			}
		}
	}

	qw.Flush()
	ow.Flush()
	if err := qw.Error(); err != nil {
		return err
	}
	if err := ow.Error(); err != nil {
		return err
	}

	return nil
}

func buildFullName(db, schema, base string) string {
	var parts []string
	if db != "" {
		parts = append(parts, db)
	}
	if schema != "" {
		parts = append(parts, schema)
	}
	if base != "" {
		parts = append(parts, base)
	}
	return strings.Join(parts, ".")
}

func boolToStr(b bool) string {
	if b {
		return "true"
	}
	return "false"
}

// looksLikeSQL heuristically checks if a string resembles an SQL statement.
// It searches for common DML keywords like select, insert, update, delete, truncate, or exec.
// A simple lower-case search is performed and only returns true if at least one keyword is found.
func looksLikeSQL(s string) bool {
	norm := strings.ToLower(StripSqlComments(strings.TrimSpace(s)))
	norm = strings.Join(strings.Fields(norm), " ")
	if norm == "" {
		return false
	}
	keywords := []string{"select", "insert", "update", "delete", "truncate", "exec", "execute"}
	for _, kw := range keywords {
		if strings.HasPrefix(norm, kw) || strings.Contains(norm, kw+" ") {
			return true
		}
	}
	return false
}
