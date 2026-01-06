package summary

import (
	"encoding/csv"
	"fmt"
	"go/ast"
	"go/parser"
	"go/token"
	"io"
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strconv"
	"strings"
	"sync"
	"unicode"
)

var (
	csMethodWithBrace      = regexp.MustCompile(`(?i)^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|async|sealed|override|virtual|partial|extern|unsafe|new)?(?:\s+(?:public|private|protected|internal|static|async|sealed|override|virtual|partial|extern|unsafe|new))*\s+[A-Za-z_][A-Za-z0-9_<>,\[\]\s\.\?]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\([^;]*\)\s*(?:where\s+[^\{]+)?\{`)
	csMethodNoModWithBrace = regexp.MustCompile(`(?i)^\s*(?:\[[^\]]+\]\s*)*(?:ref\s+|out\s+|in\s+|params\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\s\.\?]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\([^;]*\)\s*(?:where\s+[^\{]+)?\{`)
	atLinePattern          = regexp.MustCompile(`(?i)@l(\d+)$`)
	fileScopeLabel         = regexp.MustCompile(`(?i)<file-scope>`)
)

var (
	sourceRoot   string
	sourceRootMu sync.RWMutex
)

// QueryRow represents a row from QueryUsage.csv used for summaries.
type QueryRow struct {
	AppName       string
	RelPath       string
	File          string
	SourceCat     string
	SourceKind    string
	CallSite      string
	DynamicSig    string
	DynamicReason string
	Func          string
	RawSql        string
	SqlClean      string
	QueryHash     string
	UsageKind     string
	IsWrite       bool
	IsDynamic     bool
	HasCrossDb    bool
	DbList        []string
	ConnDb        string
	LineStart     int
	LineEnd       int
}

// ObjectRow represents a row from ObjectUsage.csv used for summaries.
type ObjectRow struct {
	AppName         string
	RelPath         string
	File            string
	QueryHash       string
	Func            string
	ObjectName      string
	DbName          string
	SchemaName      string
	BaseName        string
	Role            string
	DmlKind         string
	IsWrite         bool
	IsCrossDb       bool
	IsObjectNameDyn bool
	IsPseudoObject  bool
	PseudoKind      string
	Line            int
}

// FunctionSummaryRow represents aggregated information per function.
type FunctionSummaryRow struct {
	AppName                  string
	RelPath                  string
	Func                     string
	TotalQueries             int
	TotalExec                int
	TotalSelect              int
	TotalInsert              int
	TotalUpdate              int
	TotalDelete              int
	TotalTruncate            int
	TotalDynamic             int
	DynamicRawCount          int
	TotalDynamicSql          int
	TotalDynamicObject       int
	DynamicSqlCount          int
	DynamicObjectCount       int
	DynamicCount             int
	DynamicSig               string
	TotalWrite               int
	TotalObjects             int
	TotalObjectsRead         int
	TotalObjectsWrite        int
	TotalObjectsExec         int
	TopObjectsRead           string
	TopObjectsWrite          string
	TopObjectsExec           string
	ObjectsUsed              string
	DynamicPseudoKinds       string
	DynamicExampleSignatures string
	DynamicReason            string
	HasCrossDb               bool
	DbList                   string
	LineStart                int
	LineEnd                  int
}

// ObjectSummaryRow represents aggregated information per database object.
type ObjectSummaryRow struct {
	AppName            string
	RelPath            string
	BaseName           string
	FullObjectName     string
	Roles              string
	RolesSummary       string
	DmlKinds           string
	TotalReads         int
	TotalWrites        int
	TotalDynamicSql    int
	TotalDynamicObject int
	IsPseudoObject     bool
	PseudoKind         string
	TotalExec          int
	TotalFuncs         int
	ExampleFuncs       string
	HasCrossDb         bool
	DbList             string
}

// FormSummaryRow represents aggregated information per file/form.
type FormSummaryRow struct {
	AppName              string
	RelPath              string
	File                 string
	TotalFunctionsWithDB int
	TotalQueries         int
	TotalExec            int
	TotalWrite           int
	TotalDynamic         int
	TotalDynamicSql      int
	TotalDynamicObject   int
	HasCrossDb           bool
	HasDbAccess          bool
	TotalObjects         int
	DistinctObjectsUsed  int
	TopObjectsRead       string
	TopObjectsWrite      string
	TopObjectsExec       string
	DbList               string
}

type objectRoleUsage struct {
	read        int
	write       int
	exec        int
	isPseudo    bool
	pseudoKinds map[string]int
}

type methodRange struct {
	Name       string
	Start, End int
}

type fileMethodIndex struct {
	lines        []string
	methodAtLine []*methodRange
	loadErr      error
}

type fileScopeResolver struct {
	root  string
	cache map[string]*fileMethodIndex
	mu    sync.Mutex
}

// NewFuncResolver constructs a resolver that can translate placeholder
// function names to concrete method identifiers using the provided source
// root. If root is empty, the currently configured source root is used.
func NewFuncResolver(root string) *FuncResolver {
	trimmed := strings.TrimSpace(root)
	if trimmed != "" {
		SetSourceRoot(trimmed)
	}
	return &FuncResolver{fileScope: newFileScopeResolver(getSourceRoot())}
}

// Resolve normalizes and resolves a raw function name using file-scope
// heuristics. When a meaningful method name cannot be determined, the
// placeholder input is returned.
func (r *FuncResolver) Resolve(rawFunc, relPath, file string, lineStart int) string {
	if r == nil {
		return resolveFuncName(normalizeFuncName(rawFunc), relPath, lineStart)
	}
	if r.fileScope == nil {
		r.fileScope = newFileScopeResolver(getSourceRoot())
	}
	normalized := normalizeFuncName(rawFunc)
	resolved := resolveFuncName(normalized, relPath, lineStart)
	return r.fileScope.resolve(resolved, rawFunc, QueryRow{RelPath: relPath, File: file, LineStart: lineStart, Func: rawFunc})
}

// FuncResolver resolves function names using file-scope heuristics.
// It maps placeholder values (e.g., <unknown-func>, path@L123) to the closest
// method name found in source files under the configured root. When a
// reasonable method name cannot be determined, the current placeholder is
// returned unchanged.
type FuncResolver struct {
	fileScope *fileScopeResolver
}

// SetSourceRoot configures the source root so that file-scope placeholders can be resolved.
func SetSourceRoot(root string) {
	sourceRootMu.Lock()
	defer sourceRootMu.Unlock()
	sourceRoot = strings.TrimSpace(root)
}

func getSourceRoot() string {
	sourceRootMu.RLock()
	defer sourceRootMu.RUnlock()
	return sourceRoot
}

func newFileScopeResolver(root string) *fileScopeResolver {
	return &fileScopeResolver{root: strings.TrimSpace(root), cache: make(map[string]*fileMethodIndex)}
}

func (r *fileScopeResolver) resolve(current, raw string, q QueryRow) string {
	if !isFileScopePlaceholder(current, raw) {
		return current
	}
	line := q.LineStart
	if line == 0 {
		line = extractLineNumberFromFunc(raw)
	}
	if line == 0 {
		line = extractLineNumberFromFunc(current)
	}
	if line <= 0 {
		return current
	}
	if method := r.lookupMethodName(q.RelPath, q.File, line); method != "" {
		if normalizeFuncName(method) != "<unknown-func>" {
			return method
		}
	}
	if strings.TrimSpace(current) != "" {
		return current
	}
	return r.fallbackLabel(q, line)
}

func (r *fileScopeResolver) fallbackLabel(q QueryRow, line int) string {
	label := strings.TrimSpace(q.RelPath)
	if label == "" {
		label = strings.TrimSpace(q.File)
	}
	if label == "" {
		label = "<file-scope>"
	}
	if line <= 0 {
		return label
	}
	return fmt.Sprintf("%s@L%d", label, line)
}

func (r *fileScopeResolver) lookupMethodName(relPath, file string, line int) string {
	for _, candidate := range r.candidatePaths(relPath, file) {
		if candidate == "" {
			continue
		}
		if method := r.methodAtLine(candidate, line); method != "" {
			return method
		}
	}
	return ""
}

func (r *fileScopeResolver) candidatePaths(relPath, file string) []string {
	seen := make(map[string]struct{})
	var paths []string
	add := func(p string) {
		p = strings.TrimSpace(p)
		if p == "" {
			return
		}
		if r.root != "" {
			p = filepath.Join(r.root, p)
		}
		p = filepath.Clean(p)
		if _, ok := seen[p]; ok {
			return
		}
		seen[p] = struct{}{}
		paths = append(paths, p)
	}

	add(relPath)
	if file != relPath {
		add(file)
	}
	if base := filepath.Base(relPath); base != "" && base != relPath {
		add(base)
	}
	return paths
}

func (r *fileScopeResolver) methodAtLine(path string, line int) string {
	info := r.loadFileIndex(path)
	if info == nil || info.methodAtLine == nil {
		return ""
	}
	idx := line - 1
	if idx >= 0 && idx < len(info.methodAtLine) {
		if mr := info.methodAtLine[idx]; mr != nil {
			return mr.Name
		}
	}
	if info.lines != nil {
		if mr := scanBackwardForMethod(info.lines, line); mr != nil {
			return mr.Name
		}
	}
	return ""
}

func (r *fileScopeResolver) loadFileIndex(path string) *fileMethodIndex {
	r.mu.Lock()
	defer r.mu.Unlock()
	if info, ok := r.cache[path]; ok {
		return info
	}
	data, err := os.ReadFile(path)
	if err != nil {
		info := &fileMethodIndex{loadErr: err}
		r.cache[path] = info
		return info
	}
	lines := strings.Split(string(data), "\n")
	index := buildMethodIndex(path, lines, data)
	info := &fileMethodIndex{lines: lines, methodAtLine: index}
	r.cache[path] = info
	return info
}

func LoadQueryUsage(path string) ([]QueryRow, error) {
	f, err := os.Open(path)
	if err != nil {
		return nil, err
	}
	defer f.Close()

	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		return nil, err
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"AppName", "RelPath", "File", "Func", "QueryHash", "UsageKind", "IsWrite", "IsDynamic"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			return nil, fmt.Errorf("missing column %s in query usage", req)
		}
	}

	var rows []QueryRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			return nil, err
		}
		row := QueryRow{
			AppName:       rec[idx["AppName"]],
			RelPath:       rec[idx["RelPath"]],
			File:          rec[idx["File"]],
			SourceCat:     pick(rec, idx, "SourceCategory"),
			SourceKind:    pick(rec, idx, "SourceKind"),
			CallSite:      pick(rec, idx, "CallSiteKind"),
			DynamicSig:    pick(rec, idx, "DynamicSignature"),
			DynamicReason: pick(rec, idx, "DynamicReason"),
			RawSql:        pick(rec, idx, "RawSql"),
			SqlClean:      pick(rec, idx, "SqlClean"),
			Func:          rec[idx["Func"]],
			QueryHash:     rec[idx["QueryHash"]],
			UsageKind:     rec[idx["UsageKind"]],
			IsWrite:       parseBool(rec[idx["IsWrite"]]),
			IsDynamic:     parseBool(rec[idx["IsDynamic"]]),
		}
		if col, ok := idx["HasCrossDb"]; ok {
			row.HasCrossDb = parseBool(rec[col])
		}
		if col, ok := idx["DbList"]; ok {
			row.DbList = parseList(rec[col])
		}
		if col, ok := idx["ConnDb"]; ok {
			row.ConnDb = strings.TrimSpace(rec[col])
		}
		if col, ok := idx["LineStart"]; ok {
			row.LineStart = parseInt(rec[col])
		}
		if col, ok := idx["LineEnd"]; ok {
			row.LineEnd = parseInt(rec[col])
		}
		if row.IsDynamic {
			row.CallSite = saltDynamicCallSite(row.CallSite, row.QueryHash, row.LineStart, len(rows))
		}
		rows = append(rows, row)
	}
	return rows, nil
}

func LoadObjectUsage(path string) ([]ObjectRow, error) {
	f, err := os.Open(path)
	if err != nil {
		return nil, err
	}
	defer f.Close()

	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		return nil, err
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"AppName", "RelPath", "File", "Func", "QueryHash", "FullObjectName", "DbName", "SchemaName", "BaseName", "Role", "DmlKind", "IsWrite", "IsCrossDb", "IsPseudoObject", "PseudoKind", "IsObjectNameDynamic"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			return nil, fmt.Errorf("missing column %s in object usage", req)
		}
	}

	var rows []ObjectRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			return nil, err
		}
		row := ObjectRow{
			AppName:         rec[idx["AppName"]],
			RelPath:         rec[idx["RelPath"]],
			File:            rec[idx["File"]],
			Func:            rec[idx["Func"]],
			QueryHash:       rec[idx["QueryHash"]],
			ObjectName:      rec[idx["FullObjectName"]],
			DbName:          rec[idx["DbName"]],
			SchemaName:      rec[idx["SchemaName"]],
			BaseName:        rec[idx["BaseName"]],
			Role:            rec[idx["Role"]],
			DmlKind:         rec[idx["DmlKind"]],
			IsWrite:         parseBool(rec[idx["IsWrite"]]),
			IsCrossDb:       parseBool(rec[idx["IsCrossDb"]]),
			IsObjectNameDyn: parseBool(rec[idx["IsObjectNameDynamic"]]),
			IsPseudoObject:  parseBool(rec[idx["IsPseudoObject"]]),
			PseudoKind:      rec[idx["PseudoKind"]],
		}
		if col, ok := idx["Line"]; ok {
			row.Line = parseInt(rec[col])
		}
		rows = append(rows, row)
	}
	return rows, nil
}

func dedupeDynamicQueries(queries []QueryRow) ([]QueryRow, map[string]int) {
	seen := make(map[string]struct{})
	counts := make(map[string]int)
	var out []QueryRow

	for _, q := range queries {
		key := ""
		if isDynamicQuery(q) {
			key = dynamicSummarySignature(q)
			counts[key]++
			if _, ok := seen[key]; ok {
				continue
			}
			seen[key] = struct{}{}
		}
		out = append(out, q)
	}

	return out, counts
}

func dynamicRawCountIndex(queries []QueryRow) map[string]map[string]int {
	index := make(map[string]map[string]int)
	for _, q := range queries {
		if !isDynamicQuery(q) {
			continue
		}
		funcKey := functionKey(q.AppName, q.RelPath, q.Func)
		sig := dynamicSummarySignature(q)
		if index[funcKey] == nil {
			index[funcKey] = make(map[string]int)
		}
		index[funcKey][sig]++
	}
	return index
}

func dynamicDedupSignature(q QueryRow) string {
	callKind := canonicalCallSiteKind(q.CallSite)
	if callKind == "" {
		callKind = "unknown"
	}
	relPath := strings.TrimSpace(q.RelPath)
	funcName := strings.TrimSpace(q.Func)
	return fmt.Sprintf("%s|%s|%s@%d", relPath, funcName, callKind, q.LineStart)
}

func dynamicSummarySignature(q QueryRow) string {
	callKind := callSiteKind(q)
	if callKind == "" {
		callKind = "unknown"
	}
	relPath := strings.TrimSpace(q.RelPath)
	funcName := strings.TrimSpace(q.Func)
	return fmt.Sprintf("%s|%s|%s", relPath, funcName, callKind)
}

func BuildFunctionSummary(queries []QueryRow, objects []ObjectRow) ([]FunctionSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
	dynamicRawIndex := dynamicRawCountIndex(normQueries)
	dedupQueries, _ := dedupeDynamicQueries(normQueries)
	objects = NormalizeObjectRows(objects)
	groupedRaw := make(map[string][]QueryRow)
	groupedDedup := make(map[string][]QueryRow)
	for _, q := range normQueries {
		key := functionKey(q.AppName, q.RelPath, q.Func)
		groupedRaw[key] = append(groupedRaw[key], q)
	}
	for _, q := range dedupQueries {
		key := functionKey(q.AppName, q.RelPath, q.Func)
		groupedDedup[key] = append(groupedDedup[key], q)
	}

	objectsByQuery := map[string][]ObjectRow{}
	for _, o := range objects {
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		objectsByQuery[qKey] = append(objectsByQuery[qKey], o)
	}

	var result []FunctionSummaryRow
	for key, rawRows := range groupedRaw {
		qRows := groupedDedup[key]
		if len(qRows) == 0 {
			qRows = rawRows
		}
		app, rel, fn := splitFunctionKey(key)
		if strings.EqualFold(strings.TrimSpace(fn), "<unknown-func>") {
			if recovered := recoverBestFuncName(rawRows); recovered != "" {
				fn = recovered
			} else {
				continue
			}
		}
		var totalExec, totalSelect, totalInsert, totalUpdate, totalDelete, totalTruncate, totalDynamic, totalDynamicSql, totalDynamicObject, totalWrite int
		rawDynamicCount := 0
		hasCross := false
		minLine := 0
		maxLine := 0
		dbListSet := make(map[string]struct{})
		roleUsage := make(map[string]*objectRoleUsage)
		dynamicSigCounts := make(map[string]dynamicSignatureInfo)
		dynamicPseudoKinds := make(map[string]int)
		dynamicExampleSigCounts := make(map[string]int)
		dynamicReasonCounts := make(map[string]int)
		pseudoKindsPerQuery := make(map[string]map[string]struct{})
		for _, q := range rawRows {
			qKey := queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)
			baseKinds := make(map[string]struct{})
			upperUsage := strings.ToUpper(strings.TrimSpace(q.UsageKind))
			if upperUsage != "" && upperUsage != "UNKNOWN" {
				baseKinds[upperUsage] = struct{}{}
			}

			objectsForQuery := objectsByQuery[qKey]
			dynKind := dynamicKindForQuery(q, objectsForQuery)

			if len(baseKinds) == 0 {
				for _, o := range objectsByQuery[qKey] {
					for _, kind := range strings.Split(o.DmlKind, ";") {
						kind = strings.ToUpper(strings.TrimSpace(kind))
						if kind == "" || kind == "UNKNOWN" {
							continue
						}
						baseKinds[kind] = struct{}{}
					}
				}
			}

			kindsForQuery := make(map[string]struct{}, len(baseKinds))
			for k := range baseKinds {
				kindsForQuery[k] = struct{}{}
			}

			for kind := range kindsForQuery {
				switch kind {
				case "EXEC":
					totalExec++
				case "SELECT":
					totalSelect++
				case "INSERT":
					totalInsert++
				case "UPDATE":
					totalUpdate++
				case "DELETE":
					totalDelete++
				case "TRUNCATE":
					totalTruncate++
				}
			}

			writeKinds := 0
			for k := range baseKinds {
				switch k {
				case "INSERT", "UPDATE", "DELETE", "TRUNCATE", "EXEC":
					writeKinds++
				}
			}
			totalWrite += writeKinds
			if q.IsDynamic {
				rawDynamicCount++
				switch dynKind {
				case "dynamic-object":
					totalDynamicObject++
				case "dynamic-sql":
					totalDynamicSql++
				}
				if sig := strings.TrimSpace(q.DynamicSig); sig != "" {
					dynamicExampleSigCounts[sig]++
				}
			}
			for _, o := range objectsForQuery {
				if !o.IsPseudoObject {
					continue
				}
				kind := normalizePseudoKindLabel(o.BaseName, o.PseudoKind)
				if _, ok := pseudoKindsPerQuery[qKey]; !ok {
					pseudoKindsPerQuery[qKey] = make(map[string]struct{})
				}
				if _, seen := pseudoKindsPerQuery[qKey][kind]; seen {
					continue
				}
				pseudoKindsPerQuery[qKey][kind] = struct{}{}
				dynamicPseudoKinds[kind]++
			}
			if q.HasCrossDb {
				hasCross = true
			}
			for _, db := range q.DbList {
				if db != "" {
					dbListSet[db] = struct{}{}
				}
			}
			if db := strings.TrimSpace(q.ConnDb); db != "" {
				dbListSet[db] = struct{}{}
			}
			if q.LineStart > 0 {
				if minLine == 0 || q.LineStart < minLine {
					minLine = q.LineStart
				}
			}
			if q.LineEnd > 0 {
				if maxLine == 0 || q.LineEnd > maxLine {
					maxLine = q.LineEnd
				}
			}
		}

		for _, q := range qRows {
			rawDynForQuery := dynamicRawIndex[key][dynamicSummarySignature(q)]
			if rawDynForQuery <= 0 {
				rawDynForQuery = 1
			}
			qKey := queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)
			sig := dynamicSignature(q)
			if sig == "" {
				sig = q.QueryHash
			}
			if sig == "" && q.LineStart > 0 {
				sig = fmt.Sprintf("%s@%d", strings.TrimSpace(q.RelPath), q.LineStart)
			}
			if sig != "" {
				entry := dynamicSigCounts[sig]
				entry.count += rawDynForQuery
				if entry.exampleHash == "" {
					entry.exampleHash = q.QueryHash
				}
				dynamicSigCounts[sig] = entry
			}
			for _, reason := range collectDynamicReasonsForSummary(q, objectsByQuery[qKey]) {
				dynamicReasonCounts[reason] += rawDynForQuery
			}
		}

		if totalWrite == 0 {
			totalWrite = totalInsert + totalUpdate + totalDelete + totalTruncate + totalExec
		}

		objSet := make(map[string]struct{})
		consumeObj := func(o ObjectRow) {
			if shouldSkipObject(o) {
				return
			}
			name := strings.TrimSpace(o.BaseName)
			if name == "" {
				return
			}
			objSet[name] = struct{}{}
			if o.IsCrossDb {
				hasCross = true
			}
			if o.DbName != "" {
				dbListSet[o.DbName] = struct{}{}
			}
			registerObjectRoleUsage(roleUsage, o)
		}
		for _, q := range qRows {
			qKey := queryObjectKey(app, rel, q.File, q.QueryHash)
			for _, o := range objectsByQuery[qKey] {
				consumeObj(o)
			}
		}
		totalObjectsRead, totalObjectsWrite, totalObjectsExec := countObjectsByRoleUsage(roleUsage)
		topObjectsRead := buildTopObjectsByRoleUsage(roleUsage, "read", 5)
		topObjectsWrite := buildTopObjectsByRoleUsage(roleUsage, "write", 5)
		topObjectsExec := buildTopObjectsByRoleUsage(roleUsage, "exec", 5)
		objectsUsed := buildObjectsUsedByRoleUsage(roleUsage, 10)
		dbList := setToSortedSlice(dbListSet)
		totalDynamic = rawDynamicCount
		dynamicSig := summarizeDynamicSignatures(dynamicSigCounts)
		dynamicPseudoSummary := summarizePseudoKindCounts(dynamicPseudoKinds)
		dynamicExampleSummary := summarizeDynamicExamples(dynamicExampleSigCounts, 3)
		dynamicReasonSummary := summarizeReasons(dynamicReasonCounts, 5)
		if maxLine == 0 && minLine > 0 {
			maxLine = minLine
		}
		if maxLine > 0 && (minLine == 0 || minLine > maxLine) {
			minLine = maxLine
		}

		result = append(result, FunctionSummaryRow{
			AppName:                  app,
			RelPath:                  rel,
			Func:                     fn,
			TotalQueries:             len(rawRows),
			TotalExec:                totalExec,
			TotalSelect:              totalSelect,
			TotalInsert:              totalInsert,
			TotalUpdate:              totalUpdate,
			TotalDelete:              totalDelete,
			TotalTruncate:            totalTruncate,
			TotalDynamic:             totalDynamic,
			DynamicRawCount:          rawDynamicCount,
			TotalDynamicSql:          totalDynamicSql,
			TotalDynamicObject:       totalDynamicObject,
			DynamicSqlCount:          totalDynamicSql,
			DynamicObjectCount:       totalDynamicObject,
			DynamicCount:             rawDynamicCount,
			DynamicSig:               dynamicSig,
			DynamicReason:            dynamicReasonSummary,
			DynamicPseudoKinds:       dynamicPseudoSummary,
			DynamicExampleSignatures: dynamicExampleSummary,
			TotalWrite:               totalWrite,
			TotalObjects:             len(objSet),
			TotalObjectsRead:         totalObjectsRead,
			TotalObjectsWrite:        totalObjectsWrite,
			TotalObjectsExec:         totalObjectsExec,
			TopObjectsRead:           topObjectsRead,
			TopObjectsWrite:          topObjectsWrite,
			TopObjectsExec:           topObjectsExec,
			ObjectsUsed:              objectsUsed,
			HasCrossDb:               hasCross,
			DbList:                   strings.Join(dbList, ";"),
			LineStart:                minLine,
			LineEnd:                  maxLine,
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.AppName != b.AppName {
			return a.AppName < b.AppName
		}
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.Func != b.Func {
			return a.Func < b.Func
		}
		if a.LineStart != b.LineStart {
			return a.LineStart < b.LineStart
		}
		return a.LineEnd < b.LineEnd
	})

	return result, nil
}

// ValidateFunctionSummaryCounts ensures that the function summary aligns with the raw query rows.
// It groups queries by (AppName, RelPath, Func) and checks TotalQueries and TotalDynamic against the summary rows.
func ValidateFunctionSummaryCounts(queries []QueryRow, summaries []FunctionSummaryRow) error {
	normQueries := normalizeQueryFuncs(queries)
	expectedTotals := make(map[string]int)
	expectedDynamic := make(map[string]int)
	expectedRawDynamic := make(map[string]int)
	for _, q := range normQueries {
		key := strings.Join([]string{q.AppName, q.RelPath, q.Func}, "|")
		expectedTotals[key]++
		if q.IsDynamic {
			expectedDynamic[key]++
			expectedRawDynamic[key]++
		}
	}

	summaryTotals := make(map[string]FunctionSummaryRow)
	for _, s := range summaries {
		fn := resolveFuncName(s.Func, s.RelPath, s.LineStart)
		key := strings.Join([]string{s.AppName, s.RelPath, fn}, "|")
		summaryTotals[key] = s
	}

	var mismatches []string
	for key, total := range expectedTotals {
		sum, ok := summaryTotals[key]
		rel, fn := splitKey(key)
		if !ok {
			mismatches = append(mismatches, fmt.Sprintf("%s/%s missing in summary (expected total=%d dyn=%d)", rel, fn, total, expectedDynamic[key]))
			continue
		}
		expectedDyn := expectedDynamic[key]
		rawDyn := expectedRawDynamic[key]
		if sum.TotalQueries != total || sum.TotalDynamic != expectedDyn || (sum.DynamicRawCount != 0 && sum.DynamicRawCount != rawDyn) {
			mismatches = append(mismatches, fmt.Sprintf("%s/%s mismatch (expected total=%d dyn=%d rawDyn=%d, summary total=%d dyn=%d rawDyn=%d)", rel, fn, total, expectedDyn, rawDyn, sum.TotalQueries, sum.TotalDynamic, sum.DynamicRawCount))
		}
	}
	for key, sum := range summaryTotals {
		if _, ok := expectedTotals[key]; ok {
			continue
		}
		rel, fn := splitKey(key)
		mismatches = append(mismatches, fmt.Sprintf("%s/%s appears only in summary (total=%d dyn=%d)", rel, fn, sum.TotalQueries, sum.TotalDynamic))
	}

	if len(mismatches) == 0 {
		return nil
	}
	if len(mismatches) > 5 {
		mismatches = mismatches[:5]
	}
	return fmt.Errorf("function summary validation failed: %s", strings.Join(mismatches, "; "))
}

// ValidateObjectSummaryCounts ensures that the object summary aligns with the raw object rows.
// It groups objects by their summary grouping key and compares role counts against the summary rows.
func ValidateObjectSummaryCounts(queries []QueryRow, objects []ObjectRow, summaries []ObjectSummaryRow) error {
	objects = NormalizeObjectRows(objects)
	grouped := aggregateObjectsForSummary(objects, buildDynamicKindIndex(queries, groupObjectsByQuery(objects)))

	summaryMap := make(map[string]ObjectSummaryRow)
	for _, s := range summaries {
		key := ObjectSummaryRowKey(s)
		summaryMap[key] = s
	}

	var mismatches []string
	for key, agg := range grouped {
		sum, ok := summaryMap[key]
		base := strings.TrimSpace(agg.baseName)
		if base == "" {
			base = strings.TrimSpace(agg.fullName)
		}
		if !ok {
			mismatches = append(mismatches, fmt.Sprintf("object %s missing in summary (reads=%d writes=%d exec=%d)", base, agg.reads, agg.writes, agg.execs))
			continue
		}
		diff := []string{}
		if agg.reads != sum.TotalReads {
			diff = append(diff, fmt.Sprintf("reads raw=%d summary=%d", agg.reads, sum.TotalReads))
		}
		if agg.writes != sum.TotalWrites {
			diff = append(diff, fmt.Sprintf("writes raw=%d summary=%d", agg.writes, sum.TotalWrites))
		}
		if agg.execs != sum.TotalExec {
			diff = append(diff, fmt.Sprintf("exec raw=%d summary=%d", agg.execs, sum.TotalExec))
		}
		if agg.dynamicSql != sum.TotalDynamicSql {
			diff = append(diff, fmt.Sprintf("dynamic-sql raw=%d summary=%d", agg.dynamicSql, sum.TotalDynamicSql))
		}
		if agg.dynamicObject != sum.TotalDynamicObject {
			diff = append(diff, fmt.Sprintf("dynamic-object raw=%d summary=%d", agg.dynamicObject, sum.TotalDynamicObject))
		}
		if len(diff) > 0 {
			mismatches = append(mismatches, fmt.Sprintf("object %s mismatch (%s)", base, strings.Join(diff, "; ")))
		}
	}
	for key, sum := range summaryMap {
		if _, ok := grouped[key]; ok {
			continue
		}
		base := strings.TrimSpace(sum.BaseName)
		if base == "" {
			base = strings.TrimSpace(sum.FullObjectName)
		}
		mismatches = append(mismatches, fmt.Sprintf("object %s present in summary only (reads=%d writes=%d exec=%d)", base, sum.TotalReads, sum.TotalWrites, sum.TotalExec))
	}

	if len(mismatches) == 0 {
		return nil
	}
	if len(mismatches) > 5 {
		mismatches = mismatches[:5]
	}
	return fmt.Errorf("object summary validation failed: %s", strings.Join(mismatches, "; "))
}

func BuildObjectSummary(queries []QueryRow, objects []ObjectRow) ([]ObjectSummaryRow, error) {
	objects = NormalizeObjectRows(objects)
	objectsByQuery := groupObjectsByQuery(objects)
	dynamicKinds := buildDynamicKindIndex(queries, objectsByQuery)

	grouped := aggregateObjectsForSummary(objects, dynamicKinds)

	var result []ObjectSummaryRow
	const exampleLimit = 5
	for _, entry := range grouped {
		funcs := sortedFuncNames(entry.funcSet)
		exampleFuncs := topFuncExamples(entry.funcCounts, exampleLimit)
		if len(exampleFuncs) == 0 {
			exampleFuncs = funcs
		}
		if entry.firstFunc != "" {
			exampleFuncs = prependIfMissing(exampleFuncs, entry.firstFunc)
		}
		if maxLineFunc := maxLineFunction(entry.funcMaxLine); maxLineFunc != "" {
			exampleFuncs = prependIfMissing(exampleFuncs, maxLineFunc)
		}
		exampleFuncs = dedupeCaseInsensitive(exampleFuncs)
		if len(exampleFuncs) > exampleLimit {
			exampleFuncs = exampleFuncs[:exampleLimit]
		}

		pseudoKind := summarizePseudoKindsFlat(entry.pseudoKinds)
		if entry.isPseudo && pseudoKind == "" {
			pseudoKind = "unknown"
		}

		roleCounts := summarizeRoleCounts(entry.reads, entry.writes, entry.execs)
		dmlKinds := setToSortedSlice(entry.dmlSet)
		roleDisplay := formatRoleDisplay(entry.roleSet, entry.roleCounts, entry.reads, entry.writes, entry.execs)

		result = append(result, ObjectSummaryRow{
			AppName:            entry.appName,
			RelPath:            entry.relPath,
			BaseName:           entry.baseName,
			FullObjectName:     entry.fullName,
			Roles:              roleDisplay,
			RolesSummary:       roleCounts,
			DmlKinds:           strings.Join(dmlKinds, ";"),
			TotalReads:         entry.reads,
			TotalWrites:        entry.writes,
			TotalDynamicSql:    entry.dynamicSql,
			TotalDynamicObject: entry.dynamicObject,
			TotalExec:          entry.execs,
			TotalFuncs:         len(funcs),
			ExampleFuncs:       strings.Join(exampleFuncs, ";"),
			IsPseudoObject:     entry.isPseudo,
			PseudoKind:         pseudoKind,
			HasCrossDb:         entry.hasCross,
			DbList:             strings.Join(setToSortedSlice(entry.dbSet), ";"),
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.IsPseudoObject != b.IsPseudoObject {
			return !a.IsPseudoObject && b.IsPseudoObject
		}
		if a.BaseName != b.BaseName {
			return a.BaseName < b.BaseName
		}
		if a.FullObjectName != b.FullObjectName {
			return a.FullObjectName < b.FullObjectName
		}
		dbA, _, _ := splitFullObjectName(a.FullObjectName)
		dbB, _, _ := splitFullObjectName(b.FullObjectName)
		if dbA != dbB {
			return dbA < dbB
		}
		return a.RelPath < b.RelPath
	})

	return result, nil
}

type objectSummaryAgg struct {
	appName       string
	relPath       string
	fullName      string
	baseName      string
	funcSet       map[string]struct{}
	funcCounts    map[string]int
	funcMaxLine   map[string]int
	firstFunc     string
	reads         int
	writes        int
	execs         int
	dmlSet        map[string]struct{}
	roleSet       map[string]struct{}
	roleCounts    map[string]int
	dbSet         map[string]struct{}
	hasCross      bool
	isPseudo      bool
	pseudoKinds   map[string]int
	dynamicSql    int
	dynamicObject int
	dynamicSeen   map[string]map[string]struct{}
}

func aggregateObjectsForSummary(objects []ObjectRow, dynamicKinds map[string]string) map[string]*objectSummaryAgg {
	objects = NormalizeObjectRows(objects)
	grouped := make(map[string]*objectSummaryAgg)

	for _, o := range objects {
		if shouldSkipObject(o) {
			continue
		}
		fullName := chooseFullObjectName(o)
		if strings.TrimSpace(fullName) == "" {
			continue
		}

		key := ObjectSummaryGroupKey(o)
		entry := grouped[key]
		if entry == nil {
			base := strings.TrimSpace(o.BaseName)
			if base == "" {
				_, _, parsedBase := splitFullObjectName(fullName)
				base = parsedBase
			}
			entry = &objectSummaryAgg{
				appName:     o.AppName,
				relPath:     o.RelPath,
				fullName:    fullName,
				baseName:    base,
				funcSet:     make(map[string]struct{}),
				funcCounts:  make(map[string]int),
				funcMaxLine: make(map[string]int),
				dmlSet:      make(map[string]struct{}),
				roleSet:     make(map[string]struct{}),
				roleCounts:  make(map[string]int),
				dbSet:       make(map[string]struct{}),
				pseudoKinds: make(map[string]int),
				dynamicSeen: make(map[string]map[string]struct{}),
			}
			grouped[key] = entry
		}

		entry.reads += objectReadCount(o)
		entry.writes += objectWriteCount(o)
		entry.execs += objectExecCount(o)

		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		if kind := dynamicKinds[qKey]; kind != "" {
			if _, ok := entry.dynamicSeen[kind]; !ok {
				entry.dynamicSeen[kind] = make(map[string]struct{})
			}
			if _, ok := entry.dynamicSeen[kind][qKey]; !ok {
				entry.dynamicSeen[kind][qKey] = struct{}{}
				switch kind {
				case "dynamic-sql":
					entry.dynamicSql++
				case "dynamic-object":
					entry.dynamicObject++
				}
			}
		}

		if o.IsCrossDb {
			entry.hasCross = true
		}
		if db := strings.TrimSpace(o.DbName); db != "" {
			entry.dbSet[db] = struct{}{}
		}
		for _, part := range splitDmlKinds(o.DmlKind) {
			entry.dmlSet[part] = struct{}{}
		}
		if role := normalizeRoleValue(o.Role); role != "" {
			entry.roleSet[role] = struct{}{}
			entry.roleCounts[role]++
		}
		if o.IsPseudoObject {
			entry.isPseudo = true
			entry.pseudoKinds[defaultPseudoKind(o.PseudoKind)]++
		}

		if fn := strings.TrimSpace(o.Func); fn != "" {
			entry.funcSet[fn] = struct{}{}
			entry.funcCounts[fn]++
			if entry.firstFunc == "" {
				entry.firstFunc = fn
			}
			if o.Line > entry.funcMaxLine[fn] {
				entry.funcMaxLine[fn] = o.Line
			}
		}
	}

	return grouped
}

func groupObjectsByQuery(objects []ObjectRow) map[string][]ObjectRow {
	result := make(map[string][]ObjectRow)
	for _, o := range objects {
		key := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		result[key] = append(result[key], o)
	}
	return result
}

func buildDynamicKindIndex(queries []QueryRow, objectsByQuery map[string][]ObjectRow) map[string]string {
	result := make(map[string]string)
	for _, q := range normalizeQueryFuncs(queries) {
		key := queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)
		if kind := dynamicKindForQuery(q, objectsByQuery[key]); kind != "" {
			result[key] = kind
		}
	}
	return result
}

func BuildFormSummary(queries []QueryRow, objects []ObjectRow) ([]FormSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
	objects = NormalizeObjectRows(objects)
	groupQueries := make(map[string][]QueryRow)
	objectSetByForm := make(map[string]map[string]struct{})
	objectCountByForm := make(map[string]int)
	hasCrossByForm := make(map[string]bool)
	roleUsageByForm := make(map[string]map[string]*objectRoleUsage)
	dbListByForm := make(map[string]map[string]struct{})
	funcSetByForm := make(map[string]map[string]struct{})
	fileByForm := make(map[string]string)
	objectsByQuery := make(map[string][]ObjectRow)

	for _, q := range normQueries {
		key := formKey(q.AppName, q.RelPath, q.File)
		groupQueries[key] = append(groupQueries[key], q)
		if _, ok := funcSetByForm[key]; !ok {
			funcSetByForm[key] = make(map[string]struct{})
		}
		if fn := strings.TrimSpace(q.Func); fn != "" {
			funcSetByForm[key][fn] = struct{}{}
		}
		if _, ok := dbListByForm[key]; !ok {
			dbListByForm[key] = make(map[string]struct{})
		}
		for _, db := range q.DbList {
			if db != "" {
				dbListByForm[key][db] = struct{}{}
			}
		}
		if q.ConnDb != "" {
			dbListByForm[key][q.ConnDb] = struct{}{}
		}
		if q.HasCrossDb {
			hasCrossByForm[key] = true
		}
		if _, ok := fileByForm[key]; !ok {
			fileByForm[key] = q.File
		}
	}

	for _, o := range objects {
		if shouldSkipObject(o) {
			continue
		}
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		objectsByQuery[qKey] = append(objectsByQuery[qKey], o)
		key := formKey(o.AppName, o.RelPath, o.File)
		if _, ok := objectSetByForm[key]; !ok {
			objectSetByForm[key] = make(map[string]struct{})
		}
		base := strings.TrimSpace(o.BaseName)
		if base != "" {
			objectSetByForm[key][base] = struct{}{}
		}
		objectCountByForm[key]++
		if o.DbName != "" {
			if _, ok := dbListByForm[key]; !ok {
				dbListByForm[key] = make(map[string]struct{})
			}
			dbListByForm[key][o.DbName] = struct{}{}
		}
		if o.IsCrossDb {
			hasCrossByForm[key] = true
		}

		if _, ok := roleUsageByForm[key]; !ok {
			roleUsageByForm[key] = make(map[string]*objectRoleUsage)
		}
		registerObjectRoleUsage(roleUsageByForm[key], o)
	}

	allKeys := make(map[string]struct{})
	for key := range groupQueries {
		allKeys[key] = struct{}{}
	}
	for key := range objectSetByForm {
		allKeys[key] = struct{}{}
	}

	var result []FormSummaryRow
	for key := range allKeys {
		qRows := groupQueries[key]
		app, rel, file := splitFormKey(key)
		if file == "" {
			file = fileByForm[key]
		}
		totalExec := 0
		totalWrite := 0
		totalDynamic := 0
		totalDynamicSql := 0
		totalDynamicObject := 0
		totalInsert := 0
		totalUpdate := 0
		totalDelete := 0
		totalTruncate := 0
		dbListSet := make(map[string]struct{})
		for _, q := range qRows {
			switch strings.ToUpper(q.UsageKind) {
			case "EXEC":
				totalExec++
			case "INSERT":
				totalInsert++
			case "UPDATE":
				totalUpdate++
			case "DELETE":
				totalDelete++
			case "TRUNCATE":
				totalTruncate++
			}
			if q.IsDynamic {
				kind := dynamicKindForQuery(q, objectsByQuery[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)])
				switch kind {
				case "dynamic-object":
					totalDynamicObject++
				default:
					totalDynamicSql++
				}
			}
			for _, db := range q.DbList {
				if db != "" {
					dbListSet[db] = struct{}{}
				}
			}
			if q.ConnDb != "" {
				dbListSet[q.ConnDb] = struct{}{}
			}
		}
		totalDynamic = totalDynamicSql + totalDynamicObject
		totalWrite = totalInsert + totalUpdate + totalDelete + totalTruncate + totalExec
		distinctObjects := len(objectSetByForm[key])
		hasCross := hasCrossByForm[key]
		for db := range dbListByForm[key] {
			dbListSet[db] = struct{}{}
		}

		topObjectsRead := buildTopObjectsByRoleUsage(roleUsageByForm[key], "read", 5)
		topObjectsWrite := buildTopObjectsByRoleUsage(roleUsageByForm[key], "write", 5)
		topObjectsExec := buildTopObjectsByRoleUsage(roleUsageByForm[key], "exec", 5)
		if distinctObjects == 0 {
			topObjectsRead = ""
			topObjectsWrite = ""
			topObjectsExec = ""
		}

		funcCount := len(funcSetByForm[key])
		totalQueries := len(qRows)
		totalObjects := objectCountByForm[key]
		hasDbAccess := totalQueries > 0 || totalObjects > 0

		result = append(result, FormSummaryRow{
			AppName:              app,
			RelPath:              rel,
			File:                 file,
			TotalFunctionsWithDB: funcCount,
			TotalQueries:         totalQueries,
			TotalWrite:           totalWrite,
			TotalDynamic:         totalDynamic,
			TotalDynamicSql:      totalDynamicSql,
			TotalDynamicObject:   totalDynamicObject,
			TotalExec:            totalExec,
			TotalObjects:         totalObjects,
			DistinctObjectsUsed:  distinctObjects,
			TopObjectsRead:       topObjectsRead,
			TopObjectsWrite:      topObjectsWrite,
			TopObjectsExec:       topObjectsExec,
			HasCrossDb:           hasCross,
			HasDbAccess:          hasDbAccess,
			DbList:               strings.Join(setToSortedSlice(dbListSet), ";"),
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.AppName != b.AppName {
			return a.AppName < b.AppName
		}
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		return a.File < b.File
	})
	return result, nil
}

type objectRoleCounter struct {
	ReadCount  int
	WriteCount int
	ExecCount  int
	HasRead    bool
	HasWrite   bool
	HasExec    bool
	IsPseudo   bool
	PseudoKind map[string]int
}

type roleFlags struct {
	read  bool
	write bool
	exec  bool
}

func (c *objectRoleCounter) Register(o ObjectRow, q QueryRow, hasQuery bool) {
	flags := roleFlagsForObject(o, q, hasQuery)

	if o.IsPseudoObject {
		c.IsPseudo = true
		kind := normalizePseudoKindLabel(o.BaseName, o.PseudoKind)
		if strings.TrimSpace(kind) != "" {
			if c.PseudoKind == nil {
				c.PseudoKind = make(map[string]int)
			}
			c.PseudoKind[kind]++
		}
	}

	if flags.exec {
		c.HasExec = true
		c.ExecCount++
	}
	if flags.write {
		c.HasWrite = true
		c.WriteCount++
	}
	if flags.read {
		c.HasRead = true
		c.ReadCount++
	}
}

func classifyRoles(o ObjectRow) roleFlags {
	role := strings.ToLower(strings.TrimSpace(o.Role))
	upperDml := strings.ToUpper(strings.TrimSpace(o.DmlKind))
	parts := strings.FieldsFunc(upperDml, func(r rune) bool { return r == ';' })
	hasExec := role == "exec"
	hasWrite := role == "target"
	hasRead := role == "source"
	for _, p := range parts {
		part := strings.TrimSpace(p)
		if part == "" {
			continue
		}
		switch part {
		case "EXEC":
			hasExec = true
		case "SELECT":
			hasRead = true
		default:
			if isWriteDml(part) {
				hasWrite = true
			}
		}
	}
	if !hasExec {
		hasExec = strings.TrimSpace(role) == "exec"
	}
	if !hasWrite {
		hasWrite = o.IsWrite
	}
	if !hasRead && !hasExec && !hasWrite && strings.TrimSpace(upperDml) == "SELECT" {
		hasRead = true
	}
	isExec := hasExec
	isWrite := hasWrite
	if isExec && !isWrite {
		isWrite = true
	}
	isRead := hasRead
	if !isRead && !isWrite && !isExec {
		isRead = true
	}

	return roleFlags{
		read:  isRead,
		write: isWrite,
		exec:  isExec,
	}
}

func dynamicPseudoRoleFlags(o ObjectRow, q QueryRow, hasQuery bool) roleFlags {
	if !isDynamicBaseName(o.BaseName) {
		return classifyRoles(o)
	}
	usage := strings.ToUpper(strings.TrimSpace(q.UsageKind))
	switch usage {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE":
		return roleFlags{write: true}
	case "EXEC":
		return roleFlags{exec: true, write: true}
	case "SELECT":
		return roleFlags{read: true}
	default:
		if q.IsWrite || (!hasQuery && o.IsWrite) {
			return roleFlags{write: true}
		}
		return roleFlags{read: true}
	}
}

func roleFlagsForObject(o ObjectRow, q QueryRow, hasQuery bool) roleFlags {
	_ = q
	_ = hasQuery
	read, write, exec := objectReadCount(o), objectWriteCount(o), objectExecCount(o)
	return roleFlags{
		read:  read > 0,
		write: write > 0,
		exec:  exec > 0,
	}
}

func normalizeRoleFlags(flags roleFlags) roleFlags {
	normalized := roleFlags{
		read:  flags.read,
		write: flags.write,
		exec:  flags.exec,
	}
	if normalized.exec && !normalized.write {
		normalized.write = true
	}
	if !normalized.read && !normalized.write && !normalized.exec {
		normalized.read = true
	}
	return normalized
}

func recordObjectRoles(counter *objectRoleCounter, o ObjectRow, q QueryRow, hasQuery bool, name, queryKey string, seen map[string]map[string]map[string]struct{}) {
	if counter == nil {
		return
	}
	flags := normalizeRoleFlags(roleFlagsForObject(o, q, hasQuery))

	if o.IsPseudoObject {
		counter.IsPseudo = counter.IsPseudo || o.IsPseudoObject
		kind := normalizePseudoKindLabel(o.BaseName, o.PseudoKind)
		if strings.TrimSpace(kind) != "" {
			if counter.PseudoKind == nil {
				counter.PseudoKind = make(map[string]int)
			}
			counter.PseudoKind[kind]++
		}
	}

	ensureRole := func(role string, apply func()) {
		if _, ok := seen[name]; !ok {
			seen[name] = make(map[string]map[string]struct{})
		}
		if _, ok := seen[name][role]; !ok {
			seen[name][role] = make(map[string]struct{})
		}
		if _, ok := seen[name][role][queryKey]; ok {
			return
		}
		seen[name][role][queryKey] = struct{}{}
		apply()
	}

	if flags.exec {
		ensureRole("exec", func() {
			counter.HasExec = true
			counter.ExecCount++
		})
	}
	if flags.write {
		ensureRole("write", func() {
			counter.HasWrite = true
			counter.WriteCount++
		})
	}
	if flags.read {
		ensureRole("read", func() {
			counter.HasRead = true
			counter.ReadCount++
		})
	}
}

func dynamicSignature(q QueryRow) string {
	if trimmed := strings.TrimSpace(q.DynamicSig); trimmed != "" {
		return trimmed
	}
	if q.LineStart == 0 {
		return ""
	}
	callKind := callSiteKind(q)
	if callKind == "" {
		callKind = "unknown"
	}
	relPath := strings.TrimSpace(q.RelPath)
	funcName := strings.TrimSpace(q.Func)
	parts := []string{}
	if relPath != "" {
		parts = append(parts, relPath)
	}
	if funcName != "" {
		parts = append(parts, funcName)
	}
	parts = append(parts, callKind)
	return fmt.Sprintf("%s@%d", strings.Join(parts, "|"), q.LineStart)
}

type dynamicSignatureInfo struct {
	count       int
	exampleHash string
}

func callSiteKind(q QueryRow) string {
	candidates := []string{q.CallSite, q.SourceKind, q.SourceCat, q.UsageKind}
	for _, cand := range candidates {
		normalized := canonicalCallSiteKind(cand)
		if normalized != "" {
			return normalized
		}
	}
	return ""
}

func canonicalCallSiteKind(kind string) string {
	trimmed := strings.TrimSpace(kind)
	if trimmed == "" {
		return ""
	}
	parts := strings.SplitN(trimmed, "|", 2)
	trimmed = strings.TrimSpace(parts[0])
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

func saltDynamicCallSite(callSite, queryHash string, line, idx int) string {
	trimmed := strings.TrimSpace(callSite)
	hash := strings.TrimSpace(queryHash)
	if hash == "" && line > 0 {
		hash = fmt.Sprintf("L%d", line)
	}
	if hash == "" {
		return trimmed
	}
	tag := "dyn#" + hash
	if idx >= 0 {
		tag = fmt.Sprintf("%s#%d", tag, idx)
	}
	if strings.Contains(trimmed, tag) {
		return trimmed
	}
	if trimmed == "" {
		return tag
	}
	return fmt.Sprintf("%s|%s", trimmed, tag)
}

func summarizeDynamicSignatures(counts map[string]dynamicSignatureInfo) string {
	if len(counts) == 0 {
		return ""
	}
	type sigCount struct {
		key     string
		count   int
		example string
	}
	list := make([]sigCount, 0, len(counts))
	for key, info := range counts {
		list = append(list, sigCount{key: key, count: info.count, example: info.exampleHash})
	}
	sort.Slice(list, func(i, j int) bool {
		if list[i].count != list[j].count {
			return list[i].count > list[j].count
		}
		return list[i].key < list[j].key
	})
	display := list
	overflow := 0
	limit := 5
	if len(display) > limit {
		overflow = len(display) - limit
		display = display[:limit]
	}
	parts := make([]string, 0, len(display)+1)
	for _, item := range display {
		example := strings.TrimSpace(item.example)
		if example != "" {
			parts = append(parts, fmt.Sprintf("%s x%d (example=%s)", item.key, item.count, example))
			continue
		}
		parts = append(parts, fmt.Sprintf("%s x%d", item.key, item.count))
	}
	if overflow > 0 {
		total := 0
		for _, item := range list {
			total += item.count
		}
		parts = append(parts, fmt.Sprintf("... (%d total)", total))
	}
	return strings.Join(parts, ";")
}

func summarizeDynamicExamples(counts map[string]int, limit int) string {
	if len(counts) == 0 || limit <= 0 {
		return ""
	}
	type sigCount struct {
		sig   string
		count int
	}
	list := make([]sigCount, 0, len(counts))
	for sig, c := range counts {
		list = append(list, sigCount{sig: sig, count: c})
	}
	sort.Slice(list, func(i, j int) bool {
		if list[i].count != list[j].count {
			return list[i].count > list[j].count
		}
		return list[i].sig < list[j].sig
	})
	if len(list) > limit {
		list = list[:limit]
	}
	parts := make([]string, 0, len(list))
	for _, item := range list {
		if item.count > 1 {
			parts = append(parts, fmt.Sprintf("%s x%d", item.sig, item.count))
			continue
		}
		parts = append(parts, item.sig)
	}
	return strings.Join(parts, "; ")
}

func summarizeReasons(counts map[string]int, limit int) string {
	if len(counts) == 0 || limit <= 0 {
		return ""
	}
	type item struct {
		name  string
		count int
	}
	list := make([]item, 0, len(counts))
	for name, c := range counts {
		list = append(list, item{name: name, count: c})
	}
	sort.Slice(list, func(i, j int) bool {
		if list[i].count != list[j].count {
			return list[i].count > list[j].count
		}
		return list[i].name < list[j].name
	})
	if len(list) > limit {
		list = list[:limit]
	}
	parts := make([]string, 0, len(list))
	for _, item := range list {
		if item.count > 1 {
			parts = append(parts, fmt.Sprintf("%s x%d", item.name, item.count))
			continue
		}
		parts = append(parts, item.name)
	}
	return strings.Join(parts, "; ")
}

func dynamicExampleSignature(q QueryRow, objects []ObjectRow) string {
	if !q.IsDynamic {
		return ""
	}
	lineLabel := "Line?"
	if q.LineStart > 0 {
		lineLabel = fmt.Sprintf("Line%d", q.LineStart)
	}
	usage := primaryUsageKind(q, objects)
	if usage == "" {
		usage = "EXEC"
	}
	pseudoKinds := collectPseudoKinds(objects)
	pseudoLabel := "<dynamic>"
	if len(pseudoKinds) > 0 {
		pseudoLabel = fmt.Sprintf("<%s>", pseudoKinds[0])
	}
	return fmt.Sprintf("%s:%s %s", lineLabel, strings.ToUpper(usage), pseudoLabel)
}

func primaryUsageKind(q QueryRow, objects []ObjectRow) string {
	baseKinds := make(map[string]struct{})
	upperUsage := strings.ToUpper(strings.TrimSpace(q.UsageKind))
	if upperUsage != "" && upperUsage != "UNKNOWN" {
		baseKinds[upperUsage] = struct{}{}
	}
	if len(baseKinds) == 0 {
		for _, o := range objects {
			for _, kind := range strings.Split(o.DmlKind, ";") {
				kind = strings.ToUpper(strings.TrimSpace(kind))
				if kind == "" || kind == "UNKNOWN" {
					continue
				}
				baseKinds[kind] = struct{}{}
			}
		}
	}
	order := []string{"EXEC", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "SELECT"}
	for _, kind := range order {
		if _, ok := baseKinds[kind]; ok {
			return kind
		}
	}
	for kind := range baseKinds {
		return kind
	}
	return ""
}

func collectPseudoKinds(objects []ObjectRow) []string {
	kindCount := make(map[string]int)
	for _, o := range objects {
		if !o.IsPseudoObject {
			continue
		}
		kind := normalizePseudoKindLabel(o.BaseName, o.PseudoKind)
		kindCount[kind]++
	}
	if len(kindCount) == 0 {
		return nil
	}
	return sortedKindCounts(kindCount)
}

func collectDynamicReasonsForSummary(q QueryRow, objects []ObjectRow) []string {
	reasonSet := make(map[string]struct{})
	addReason := func(reason string) {
		if trimmed := strings.TrimSpace(reason); trimmed != "" {
			reasonSet[trimmed] = struct{}{}
		}
	}

	for _, part := range strings.Split(q.DynamicReason, ";") {
		addReason(part)
	}

	for _, o := range objects {
		if !o.IsPseudoObject {
			continue
		}
		switch normalizePseudoKindLabel(o.BaseName, o.PseudoKind) {
		case "schema-placeholder":
			addReason("[[schema]] placeholder")
		case "table-placeholder":
			addReason("[[table]] placeholder")
		case "dynamic-object":
			addReason("dynamic object name")
		case "dynamic-sql":
			addReason("dynamic sql")
		}
	}

	if len(reasonSet) == 0 && isDynamicQuery(q) {
		addReason("dynamic construction")
	}

	return setToSortedSlice(reasonSet)
}

func sortedKindCounts(kindCount map[string]int) []string {
	type item struct {
		name  string
		count int
	}
	list := make([]item, 0, len(kindCount))
	for k, v := range kindCount {
		list = append(list, item{name: k, count: v})
	}
	sort.Slice(list, func(i, j int) bool {
		if list[i].count != list[j].count {
			return list[i].count > list[j].count
		}
		return list[i].name < list[j].name
	})
	res := make([]string, 0, len(list))
	for _, item := range list {
		res = append(res, item.name)
	}
	return res
}

func normalizePseudoKindLabel(baseName, pseudoKind string) string {
	if detected, kind := pseudoObjectInfo(baseName, pseudoKind); detected {
		return defaultPseudoKind(kind)
	}
	return defaultPseudoKind(pseudoKind)
}

func summarizePseudoKindCounts(counts map[string]int) string {
	if len(counts) == 0 {
		return ""
	}
	kinds := sortedKindCounts(counts)
	parts := make([]string, 0, len(kinds))
	for _, kind := range kinds {
		parts = append(parts, fmt.Sprintf("%s(%d)", kind, counts[kind]))
	}
	return strings.Join(parts, "; ")
}

func summarizePseudoKindsFlat(counts map[string]int) string {
	if len(counts) == 0 {
		return ""
	}
	return strings.Join(sortedKindCounts(counts), "; ")
}

func recoverBestFuncName(qRows []QueryRow) string {
	best := ""
	for _, q := range qRows {
		candidate := resolveFuncName(q.Func, q.RelPath, q.LineStart)
		if strings.EqualFold(strings.TrimSpace(candidate), "<unknown-func>") || strings.TrimSpace(candidate) == "" {
			continue
		}
		if best == "" || funcNameQuality(candidate) < funcNameQuality(best) || (funcNameQuality(candidate) == funcNameQuality(best) && strings.ToLower(candidate) < strings.ToLower(best)) {
			best = candidate
		}
	}
	return best
}

func buildTopObjectSummary(stats map[string]*objectRoleCounter, limit int) string {
	if len(stats) == 0 || limit <= 0 {
		return ""
	}
	type top struct {
		name  string
		total int
		roles *objectRoleCounter
	}
	tops := make([]top, 0, len(stats))
	for name, counter := range stats {
		total := totalUsageCount(counter)
		if total == 0 {
			continue
		}
		tops = append(tops, top{name: name, total: total, roles: counter})
	}
	sort.Slice(tops, func(i, j int) bool {
		if tops[i].total != tops[j].total {
			return tops[i].total > tops[j].total
		}
		return strings.ToLower(tops[i].name) < strings.ToLower(tops[j].name)
	})
	if len(tops) == 0 {
		return ""
	}
	display := tops
	overflow := 0
	if len(display) > limit {
		overflow = len(display) - limit
		display = display[:limit]
	}
	parts := make([]string, 0, len(display)+1)
	for _, t := range display {
		name := formatObjectName(t.name, t.roles)
		roleLabel := primaryRoleLabel(t.roles)
		roleCount := roleCountWithFallback(t.roles, roleLabel)
		suffix := roleLabel
		if roleCount > 1 {
			suffix = fmt.Sprintf("%s x%d", roleLabel, roleCount)
		}
		parts = append(parts, fmt.Sprintf("%s(%s)", name, suffix))
	}
	if overflow > 0 {
		parts = append(parts, fmt.Sprintf("+%d others", overflow))
	}
	return strings.Join(parts, "; ")
}

func buildObjectsUsedDetailed(stats map[string]*objectRoleCounter) string {
	if len(stats) == 0 {
		return ""
	}
	type entry struct {
		name  string
		total int
		role  *objectRoleCounter
	}
	entries := make([]entry, 0, len(stats))
	for name, counter := range stats {
		total := totalUsageCount(counter)
		if total == 0 {
			continue
		}
		entries = append(entries, entry{name: name, total: total, role: counter})
	}
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].total != entries[j].total {
			return entries[i].total > entries[j].total
		}
		return strings.ToLower(entries[i].name) < strings.ToLower(entries[j].name)
	})
	limit := 10
	if len(entries) < limit {
		limit = len(entries)
	}
	parts := make([]string, 0, len(entries))
	for i := 0; i < limit; i++ {
		e := entries[i]
		roleDetails := formatRoleBreakdown(e.role)
		parts = append(parts, fmt.Sprintf("%s(%s)", formatObjectName(e.name, e.role), roleDetails))
	}
	if extra := len(entries) - limit; extra > 0 {
		parts = append(parts, fmt.Sprintf("+%d others", extra))
	}
	return strings.Join(parts, "; ")
}

type dynamicGroup struct {
	count      int
	minLine    int
	maxLine    int
	usageKind  string
	callSite   string
	pseudoKind string
}

func classifyDynamicPseudoKinds(objects []ObjectRow) (bool, bool) {
	dynSql := false
	dynObj := false
	for _, o := range objects {
		if !o.IsPseudoObject {
			continue
		}
		kind := normalizePseudoKindLabel(o.BaseName, o.PseudoKind)
		if kind == "dynamic-sql" {
			dynSql = true
		} else if strings.TrimSpace(kind) != "" {
			dynObj = true
		}
	}
	return dynSql, dynObj
}

func dynamicKindForQuery(q QueryRow, objects []ObjectRow) string {
	if !q.IsDynamic {
		return ""
	}
	dynSqlPseudo, dynObjPseudo := classifyDynamicPseudoKinds(objects)
	if dynObjPseudo {
		return "dynamic-object"
	}
	if dynSqlPseudo {
		return "dynamic-sql"
	}
	return "dynamic-sql"
}

func buildDynamicGroupKey(q QueryRow) string {
	usage := usageKindWithFallback(q.UsageKind)
	base := dynamicSignature(q)
	if base == "" {
		base = fmt.Sprintf("%s|%s@%d", strings.TrimSpace(q.RelPath), strings.TrimSpace(q.Func), q.LineStart)
	}
	return fmt.Sprintf("%s|%s", usage, base)
}

func usageKindWithFallback(kind string) string {
	upper := strings.ToUpper(strings.TrimSpace(kind))
	if upper == "" {
		return "UNKNOWN"
	}
	return upper
}

func pickDynamicPseudoKind(current string, hasDynSQL, hasDynObj bool) string {
	if strings.TrimSpace(current) != "" {
		return current
	}
	switch {
	case hasDynObj:
		return "dynamic-object"
	default:
		return "dynamic-sql"
	}
}

func filterDynamicPseudoObjects(stats map[string]*objectRoleCounter) map[string]*objectRoleCounter {
	if len(stats) == 0 {
		return stats
	}
	filtered := make(map[string]*objectRoleCounter, len(stats))
	dynAggregate := &objectRoleCounter{}
	hasDyn := false
	for name, counter := range stats {
		if counter != nil && counter.IsPseudo {
			if counter.PseudoKind != nil {
				if _, ok := counter.PseudoKind["dynamic-sql"]; ok {
					mergeRoleCounters(dynAggregate, counter)
					hasDyn = true
					continue
				}
				if _, ok := counter.PseudoKind["dynamic-object"]; ok {
					mergeRoleCounters(dynAggregate, counter)
					hasDyn = true
					continue
				}
			}
		}
		filtered[name] = counter
	}
	if hasDyn {
		dynAggregate.IsPseudo = true
		if dynAggregate.PseudoKind == nil {
			dynAggregate.PseudoKind = make(map[string]int)
		}
		dynAggregate.PseudoKind["dynamic-sql"] += dynAggregate.ExecCount + dynAggregate.WriteCount + dynAggregate.ReadCount
		filtered["<dynamic-sql>"] = dynAggregate
	}
	return filtered
}

func mergeRoleCounters(dst, src *objectRoleCounter) {
	if dst == nil || src == nil {
		return
	}
	dst.ReadCount += src.ReadCount
	dst.WriteCount += src.WriteCount
	dst.ExecCount += src.ExecCount
	dst.HasRead = dst.HasRead || src.HasRead || src.ReadCount > 0
	dst.HasWrite = dst.HasWrite || src.HasWrite || src.WriteCount > 0
	dst.HasExec = dst.HasExec || src.HasExec || src.ExecCount > 0
	if src.IsPseudo {
		dst.IsPseudo = true
	}
	if src.PseudoKind != nil {
		if dst.PseudoKind == nil {
			dst.PseudoKind = make(map[string]int)
		}
		for k, v := range src.PseudoKind {
			dst.PseudoKind[k] += v
		}
	}
}

func summarizeDynamicGroups(groups map[string]dynamicGroup, limit int) string {
	if len(groups) == 0 || limit <= 0 {
		return ""
	}
	type entry struct {
		key   string
		group dynamicGroup
	}
	list := make([]entry, 0, len(groups))
	for k, g := range groups {
		list = append(list, entry{key: k, group: g})
	}
	sort.Slice(list, func(i, j int) bool {
		if list[i].group.count != list[j].group.count {
			return list[i].group.count > list[j].group.count
		}
		if list[i].group.minLine != list[j].group.minLine {
			return list[i].group.minLine < list[j].group.minLine
		}
		return list[i].key < list[j].key
	})
	if len(list) > limit {
		list = list[:limit]
	}
	parts := make([]string, 0, len(list))
	for _, item := range list {
		g := item.group
		label := fmt.Sprintf("<%s>", strings.TrimSpace(g.pseudoKind))
		if strings.TrimSpace(g.pseudoKind) == "" {
			label = "<dynamic-sql>"
		}
		lineLabel := ""
		switch {
		case g.minLine > 0 && g.maxLine > 0 && g.minLine != g.maxLine:
			lineLabel = fmt.Sprintf("lines=%d..%d", g.minLine, g.maxLine)
		case g.minLine > 0:
			lineLabel = fmt.Sprintf("line=%d", g.minLine)
		}
		usage := strings.TrimSpace(g.usageKind)
		if usage != "" {
			lineLabel = strings.TrimSpace(strings.Join(filterNonEmpty([]string{lineLabel, fmt.Sprintf("usage=%s", usage)}), ", "))
		}
		if lineLabel != "" {
			parts = append(parts, fmt.Sprintf("%s(n=%d, %s)", label, g.count, lineLabel))
			continue
		}
		parts = append(parts, fmt.Sprintf("%s(n=%d)", label, g.count))
	}
	return strings.Join(parts, "; ")
}

func filterNonEmpty(parts []string) []string {
	var res []string
	for _, p := range parts {
		if strings.TrimSpace(p) == "" {
			continue
		}
		res = append(res, p)
	}
	return res
}

func appendSummary(base, extra string) string {
	base = strings.TrimSpace(base)
	extra = strings.TrimSpace(extra)
	switch {
	case base == "" && extra == "":
		return ""
	case base == "":
		return extra
	case extra == "":
		return base
	default:
		return base + "; " + extra
	}
}

func registerObjectRoleUsage(stats map[string]*objectRoleUsage, o ObjectRow) {
	if stats == nil {
		return
	}
	name := strings.TrimSpace(o.BaseName)
	if name == "" {
		return
	}
	entry := stats[name]
	if entry == nil {
		entry = &objectRoleUsage{}
		stats[name] = entry
	}
	entry.read += objectReadCount(o)
	entry.write += objectWriteCount(o)
	entry.exec += objectExecCount(o)
	if o.IsPseudoObject || isDynamicBaseName(o.BaseName) {
		entry.isPseudo = true
		if entry.pseudoKinds == nil {
			entry.pseudoKinds = make(map[string]int)
		}
		if kind := normalizePseudoKindLabel(o.BaseName, o.PseudoKind); kind != "" {
			entry.pseudoKinds[kind]++
		}
	}
}

func countObjectsByRoleUsage(stats map[string]*objectRoleUsage) (int, int, int) {
	read := 0
	write := 0
	exec := 0
	for _, usage := range stats {
		if usage == nil {
			continue
		}
		if usage.read > 0 {
			read++
		}
		if usage.write > 0 {
			write++
		}
		if usage.exec > 0 {
			exec++
		}
	}
	return read, write, exec
}

func objectUsageTotal(usage *objectRoleUsage) int {
	if usage == nil {
		return 0
	}
	return usage.read + usage.write + usage.exec
}

func objectUsageName(name string, usage *objectRoleUsage) string {
	if usage == nil {
		return name
	}
	if usage.isPseudo {
		return "[P] " + name
	}
	return name
}

func roleUsageCount(usage *objectRoleUsage, role string) int {
	if usage == nil {
		return 0
	}
	switch strings.ToLower(role) {
	case "read":
		return usage.read
	case "write":
		return usage.write
	case "exec":
		return usage.exec
	default:
		return objectUsageTotal(usage)
	}
}

func buildTopObjectsByRoleUsage(stats map[string]*objectRoleUsage, role string, limit int) string {
	if len(stats) == 0 || limit <= 0 {
		return ""
	}
	type entry struct {
		name  string
		count int
		usage *objectRoleUsage
	}
	entries := make([]entry, 0, len(stats))
	for name, usage := range stats {
		count := roleUsageCount(usage, role)
		if count <= 0 {
			continue
		}
		entries = append(entries, entry{name: name, count: count, usage: usage})
	}
	if len(entries) == 0 {
		return ""
	}
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].count != entries[j].count {
			return entries[i].count > entries[j].count
		}
		return strings.ToLower(entries[i].name) < strings.ToLower(entries[j].name)
	})
	totalEntries := len(entries)
	if len(entries) > limit {
		entries = entries[:limit]
	}
	parts := make([]string, 0, len(entries))
	for _, e := range entries {
		parts = append(parts, fmt.Sprintf("%s(%d)", objectUsageName(e.name, e.usage), e.count))
	}
	if overflow := totalEntries - len(entries); overflow > 0 {
		parts = append(parts, fmt.Sprintf("+%d others", overflow))
	}
	return strings.Join(parts, "; ")
}

func buildObjectsUsedByRoleUsage(stats map[string]*objectRoleUsage, limit int) string {
	if len(stats) == 0 {
		return ""
	}
	type entry struct {
		name  string
		total int
		usage *objectRoleUsage
	}
	entries := make([]entry, 0, len(stats))
	for name, usage := range stats {
		total := objectUsageTotal(usage)
		if total == 0 {
			continue
		}
		entries = append(entries, entry{name: name, total: total, usage: usage})
	}
	if len(entries) == 0 {
		return ""
	}
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].total != entries[j].total {
			return entries[i].total > entries[j].total
		}
		return strings.ToLower(entries[i].name) < strings.ToLower(entries[j].name)
	})
	totalEntries := len(entries)
	if limit > 0 && len(entries) > limit {
		entries = entries[:limit]
	}
	parts := make([]string, 0, len(entries))
	for _, e := range entries {
		parts = append(parts, fmt.Sprintf("%s(%s)", objectUsageName(e.name, e.usage), roleUsageBreakdown(e.usage)))
	}
	if overflow := totalEntries - len(entries); overflow > 0 {
		parts = append(parts, fmt.Sprintf("+%d others", overflow))
	}
	return strings.Join(parts, "; ")
}

func roleUsageBreakdown(usage *objectRoleUsage) string {
	if usage == nil {
		return "read=0/write=0/exec=0"
	}
	parts := []string{}
	if usage.read > 0 {
		parts = append(parts, fmt.Sprintf("read=%d", usage.read))
	}
	if usage.write > 0 {
		parts = append(parts, fmt.Sprintf("write=%d", usage.write))
	}
	if usage.exec > 0 {
		parts = append(parts, fmt.Sprintf("exec=%d", usage.exec))
	}
	if len(parts) == 0 {
		return "read=0/write=0/exec=0"
	}
	return strings.Join(parts, "/")
}

func buildTopObjectsByRole(stats map[string]*objectRoleCounter, role string, limit int) string {
	if len(stats) == 0 || limit <= 0 {
		return ""
	}
	type entry struct {
		name  string
		count int
		role  *objectRoleCounter
	}
	entries := make([]entry, 0, len(stats))
	for name, counter := range stats {
		count := roleCountWithFallback(counter, role)
		hasRole := false
		switch strings.ToLower(role) {
		case "exec":
			hasRole = counter != nil && (counter.HasExec || counter.ExecCount > 0)
		case "write":
			hasRole = counter != nil && (counter.HasWrite || counter.WriteCount > 0)
		case "read":
			hasRole = counter != nil && (counter.HasRead || counter.ReadCount > 0)
		default:
			hasRole = counter != nil
		}
		if count == 0 || !hasRole {
			continue
		}
		entries = append(entries, entry{name: name, count: count, role: counter})
	}
	if len(entries) == 0 {
		return ""
	}
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].count != entries[j].count {
			return entries[i].count > entries[j].count
		}
		return strings.ToLower(entries[i].name) < strings.ToLower(entries[j].name)
	})
	overflow := 0
	display := entries
	if len(display) > limit {
		overflow = len(display) - limit
		display = display[:limit]
	}
	parts := make([]string, 0, len(display)+1)
	for _, e := range display {
		parts = append(parts, fmt.Sprintf("%s(%s x%d)", formatObjectName(e.name, e.role), role, e.count))
	}
	if overflow > 0 {
		parts = append(parts, fmt.Sprintf("+%d others", overflow))
	}
	return strings.Join(parts, "; ")
}

func objectDisplayScore(counter *objectRoleCounter) int {
	if counter == nil {
		return 0
	}
	exec := counter.ExecCount
	write := counter.WriteCount
	read := counter.ReadCount
	if exec == 0 && counter.HasExec {
		exec = 1
	}
	if write == 0 && counter.HasWrite {
		write = 1
	}
	if read == 0 && counter.HasRead {
		read = 1
	}
	return exec*100000 + write*1000 + read*10
}

func totalUsageCount(counter *objectRoleCounter) int {
	if counter == nil {
		return 0
	}
	total := counter.ExecCount + counter.WriteCount + counter.ReadCount
	if total == 0 && (counter.HasExec || counter.HasWrite || counter.HasRead) {
		return 1
	}
	return total
}

func objectSummaryScore(r ObjectSummaryRow) int {
	return r.TotalExec*100000 + r.TotalWrites*1000 + r.TotalReads*10
}

func describeRolesOrdered(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	roles := []string{}
	if counter.HasExec {
		roles = append(roles, "exec")
	}
	if counter.HasWrite {
		roles = append(roles, "write")
	}
	if counter.HasRead {
		roles = append(roles, "read")
	}
	if len(roles) == 0 {
		return "mixed"
	}
	return strings.Join(roles, "+")
}

func describeRoleCountsDetailed(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	parts := []string{}
	if counter.ReadCount > 0 {
		parts = append(parts, fmt.Sprintf("read:%d", counter.ReadCount))
	}
	if counter.WriteCount > 0 {
		parts = append(parts, fmt.Sprintf("write:%d", counter.WriteCount))
	}
	if counter.ExecCount > 0 {
		parts = append(parts, fmt.Sprintf("exec:%d", counter.ExecCount))
	}
	if len(parts) == 0 {
		return "mixed"
	}
	return strings.Join(parts, ", ")
}

func summarizeRoleCounts(reads, writes, execs int) string {
	return fmt.Sprintf("read=%d; write=%d; exec=%d", reads, writes, execs)
}

func summarizeRoleCountsCompact(reads, writes, execs int) string {
	return fmt.Sprintf("reads:%d,writes:%d,execs:%d", reads, writes, execs)
}

func buildRoleDisplay(reads, writes, execs int) string {
	parts := []string{}
	if execs > 0 {
		parts = append(parts, fmt.Sprintf("exec (%d)", execs))
	}
	if writes > 0 {
		parts = append(parts, fmt.Sprintf("write (%d)", writes))
	}
	if reads > 0 {
		parts = append(parts, fmt.Sprintf("read (%d)", reads))
	}
	return strings.Join(parts, "; ")
}

func formatRoleDisplay(roleSet map[string]struct{}, roleCounts map[string]int, reads, writes, execs int) string {
	roles := setToSortedSlice(roleSet)
	if len(roles) == 0 {
		return buildRoleDisplay(reads, writes, execs)
	}
	parts := make([]string, 0, len(roles))
	for _, role := range roles {
		count := roleCounts[role]
		if count == 0 {
			count = 1
		}
		parts = append(parts, fmt.Sprintf("%s (%d)", role, count))
	}
	return strings.Join(parts, "; ")
}

func summarizeRoleLabel(reads, writes, execs int) string {
	switch {
	case execs > 0 && reads == 0 && (writes == 0 || writes == execs):
		return "exec"
	case reads > 0 && writes == 0 && execs == 0:
		return "source"
	case writes > 0 && reads == 0 && execs == 0:
		return "target"
	default:
		return "mixed"
	}
}

func normalizeRoleValue(role string) string {
	return strings.ToLower(strings.TrimSpace(role))
}

func splitDmlKinds(dml string) []string {
	upper := strings.ToUpper(strings.TrimSpace(dml))
	if upper == "" {
		return nil
	}
	parts := strings.FieldsFunc(upper, func(r rune) bool { return r == ';' })
	out := make([]string, 0, len(parts))
	for _, part := range parts {
		if trimmed := strings.TrimSpace(part); trimmed != "" {
			out = append(out, trimmed)
		}
	}
	return out
}

func hasReadDml(dml string) bool {
	for _, part := range splitDmlKinds(dml) {
		if part == "SELECT" || part == "READ" {
			return true
		}
	}
	return false
}

func hasExecDml(dml string) bool {
	for _, part := range splitDmlKinds(dml) {
		if part == "EXEC" {
			return true
		}
	}
	return false
}

func objectReadCount(o ObjectRow) int {
	role := normalizeRoleValue(o.Role)
	if role == "source" {
		return 1
	}
	if role == "mixed" && hasReadDml(o.DmlKind) {
		return 1
	}
	return 0
}

func objectWriteCount(o ObjectRow) int {
	if o.IsWrite {
		return 1
	}
	return 0
}

func objectExecCount(o ObjectRow) int {
	role := normalizeRoleValue(o.Role)
	if role == "exec" || hasExecDml(o.DmlKind) {
		return 1
	}
	return 0
}

func ObjectRoleBuckets(o ObjectRow) (int, int, int) {
	return ObjectRoleCounts(o, QueryRow{}, false)
}

func ObjectRoleCounts(o ObjectRow, q QueryRow, hasQuery bool) (int, int, int) {
	_ = q
	_ = hasQuery
	return objectReadCount(o), objectWriteCount(o), objectExecCount(o)
}

func buildTopObjects(stats map[string]*objectRoleCounter) string {
	if len(stats) == 0 {
		return ""
	}
	type entry struct {
		name  string
		total int
		role  *objectRoleCounter
	}
	var entries []entry
	for name, counter := range stats {
		total := totalUsageCount(counter)
		if total == 0 {
			continue
		}
		entries = append(entries, entry{name: name, total: total, role: counter})
	}
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].total != entries[j].total {
			return entries[i].total > entries[j].total
		}
		return strings.ToLower(entries[i].name) < strings.ToLower(entries[j].name)
	})
	display := entries
	overflow := 0
	limit := 10
	if len(display) > limit {
		overflow = len(display) - limit
		display = display[:limit]
	}
	parts := make([]string, 0, len(display)+1)
	for _, e := range display {
		roleLabel := primaryRoleLabel(e.role)
		count := roleCountWithFallback(e.role, roleLabel)
		suffix := roleLabel
		if count > 1 {
			suffix = fmt.Sprintf("%s x%d", roleLabel, count)
		}
		parts = append(parts, fmt.Sprintf("%s(%s)", formatObjectName(e.name, e.role), suffix))
	}
	if overflow > 0 {
		parts = append(parts, fmt.Sprintf("+%d others", overflow))
	}
	return strings.Join(parts, "; ")
}

func primaryRoleLabel(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	switch {
	case counter.HasExec:
		return "exec"
	case counter.HasWrite:
		return "write"
	case counter.HasRead:
		return "read"
	default:
		return "mixed"
	}
}

func roleCountWithFallback(counter *objectRoleCounter, role string) int {
	if counter == nil {
		return 0
	}
	switch strings.ToLower(role) {
	case "exec":
		if counter.ExecCount > 0 {
			return counter.ExecCount
		}
		if counter.HasExec {
			return 1
		}
	case "write":
		if counter.WriteCount > 0 {
			return counter.WriteCount
		}
		if counter.HasWrite {
			return 1
		}
	case "read":
		if counter.ReadCount > 0 {
			return counter.ReadCount
		}
		if counter.HasRead {
			return 1
		}
	}
	return totalUsageCount(counter)
}

func countObjectsByRole(stats map[string]*objectRoleCounter) (int, int, int) {
	read := 0
	write := 0
	exec := 0
	for _, counter := range stats {
		if counter == nil {
			continue
		}
		if counter.HasRead {
			read++
		}
		if counter.HasWrite {
			write++
		}
		if counter.HasExec {
			exec++
		}
	}
	return read, write, exec
}

func formatObjectName(name string, counter *objectRoleCounter) string {
	if counter == nil || !counter.IsPseudo {
		return name
	}
	return "[P] " + name
}

func formatRoleBreakdown(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	parts := []string{}
	add := func(role string, count int) {
		if count <= 0 {
			return
		}
		if count == 1 {
			parts = append(parts, role)
			return
		}
		parts = append(parts, fmt.Sprintf("%s x%d", role, count))
	}
	add("exec", roleCountWithFallback(counter, "exec"))
	add("write", roleCountWithFallback(counter, "write"))
	add("read", roleCountWithFallback(counter, "read"))
	if len(parts) == 0 {
		return "mixed"
	}
	return strings.Join(parts, "/")
}

func classifyObjectUsage(counter *objectRoleCounter) string {
	if counter == nil {
		return "(mixed)"
	}
	if counter.HasWrite {
		if counter.HasExec || counter.HasRead {
			return "(mixed)"
		}
		return "(write)"
	}
	if counter.HasExec {
		if counter.HasRead {
			return "(mixed)"
		}
		return "(exec)"
	}
	if counter.HasRead {
		return "(read)"
	}
	return "(mixed)"
}

func summarizeRole(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	if counter.HasWrite && !counter.HasRead && !counter.HasExec {
		return "write"
	}
	if counter.HasExec && !counter.HasRead && !counter.HasWrite {
		return "exec"
	}
	if counter.HasRead && !counter.HasWrite && !counter.HasExec {
		return "read"
	}
	return "mixed"
}

func describeRoles(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	roles := []string{}
	if counter.HasWrite {
		roles = append(roles, "write")
	}
	if counter.HasExec {
		roles = append(roles, "exec")
	}
	if counter.HasRead {
		roles = append(roles, "read")
	}
	if len(roles) == 0 {
		return "mixed"
	}
	return strings.Join(roles, "+")
}

func dominantRole(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	if counter.HasExec {
		return "exec"
	}
	if counter.HasWrite {
		return "write"
	}
	if counter.HasRead {
		return "read"
	}
	return "mixed"
}

func collectRoles(counter *objectRoleCounter) []string {
	if counter == nil {
		return nil
	}
	ordered := []struct {
		name string
		has  bool
	}{
		{"exec", counter.HasExec},
		{"write", counter.HasWrite},
		{"read", counter.HasRead},
	}
	roles := []string{}
	for _, entry := range ordered {
		if entry.has {
			roles = append(roles, entry.name)
		}
	}
	return roles
}

func roleCount(counter *objectRoleCounter, role string) int {
	if counter == nil {
		return 0
	}
	switch strings.ToLower(role) {
	case "exec":
		return counter.ExecCount
	case "write":
		return counter.WriteCount
	case "read":
		return counter.ReadCount
	default:
		return counter.ReadCount + counter.WriteCount + counter.ExecCount
	}
}

func WriteFunctionSummary(path string, rows []FunctionSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "RelPath", "Func", "LineStart", "LineEnd", "TotalQueries", "TotalSelect", "TotalInsert", "TotalUpdate", "TotalDelete", "TotalTruncate", "TotalExec", "TotalWrite", "TotalDynamic", "DynamicRawCount", "TotalDynamicSql", "TotalDynamicObject", "DynamicSqlCount", "DynamicObjectCount", "DynamicCount", "DynamicSignatures", "DynamicReason", "TotalObjects", "TotalObjectsRead", "TotalObjectsWrite", "TotalObjectsExec", "TopObjectsRead", "TopObjectsWrite", "TopObjectsExec", "ObjectsUsed", "HasCrossDb", "DbList", "DynamicPseudoKinds", "DynamicExampleSignatures"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			r.Func,
			fmt.Sprintf("%d", r.LineStart),
			fmt.Sprintf("%d", r.LineEnd),
			fmt.Sprintf("%d", r.TotalQueries),
			fmt.Sprintf("%d", r.TotalSelect),
			fmt.Sprintf("%d", r.TotalInsert),
			fmt.Sprintf("%d", r.TotalUpdate),
			fmt.Sprintf("%d", r.TotalDelete),
			fmt.Sprintf("%d", r.TotalTruncate),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalWrite),
			fmt.Sprintf("%d", r.TotalDynamic),
			fmt.Sprintf("%d", r.DynamicRawCount),
			fmt.Sprintf("%d", r.TotalDynamicSql),
			fmt.Sprintf("%d", r.TotalDynamicObject),
			fmt.Sprintf("%d", r.DynamicSqlCount),
			fmt.Sprintf("%d", r.DynamicObjectCount),
			fmt.Sprintf("%d", r.DynamicCount),
			r.DynamicSig,
			r.DynamicReason,
			fmt.Sprintf("%d", r.TotalObjects),
			fmt.Sprintf("%d", r.TotalObjectsRead),
			fmt.Sprintf("%d", r.TotalObjectsWrite),
			fmt.Sprintf("%d", r.TotalObjectsExec),
			r.TopObjectsRead,
			r.TopObjectsWrite,
			r.TopObjectsExec,
			r.ObjectsUsed,
			boolToStr(r.HasCrossDb),
			r.DbList,
			r.DynamicPseudoKinds,
			r.DynamicExampleSignatures,
		}
		if err := w.Write(rec); err != nil {
			return err
		}
	}
	w.Flush()
	return w.Error()
}

func WriteObjectSummary(path string, rows []ObjectSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "RelPath", "FullObjectName", "BaseName", "Roles", "RolesSummary", "DmlKinds", "TotalReads", "TotalWrites", "TotalDynamicSql", "TotalDynamicObject", "TotalExec", "TotalFuncs", "ExampleFuncs", "IsPseudoObject", "PseudoKind", "HasCrossDb", "DbList"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			r.FullObjectName,
			r.BaseName,
			r.Roles,
			r.RolesSummary,
			r.DmlKinds,
			fmt.Sprintf("%d", r.TotalReads),
			fmt.Sprintf("%d", r.TotalWrites),
			fmt.Sprintf("%d", r.TotalDynamicSql),
			fmt.Sprintf("%d", r.TotalDynamicObject),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalFuncs),
			r.ExampleFuncs,
			boolToStr(r.IsPseudoObject),
			r.PseudoKind,
			boolToStr(r.HasCrossDb),
			r.DbList,
		}
		if err := w.Write(rec); err != nil {
			return err
		}
	}
	w.Flush()
	return w.Error()
}

func WriteFormSummary(path string, rows []FormSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "RelPath", "File", "TotalFunctionsWithDB", "TotalQueries", "TotalObjects", "TotalExec", "TotalWrite", "TotalDynamic", "TotalDynamicSql", "TotalDynamicObject", "DistinctObjectsUsed", "HasDbAccess", "HasCrossDb", "DbList", "TopObjectsRead", "TopObjectsWrite", "TopObjectsExec"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			r.File,
			fmt.Sprintf("%d", r.TotalFunctionsWithDB),
			fmt.Sprintf("%d", r.TotalQueries),
			fmt.Sprintf("%d", r.TotalObjects),
			fmt.Sprintf("%d", r.TotalWrite),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalDynamic),
			fmt.Sprintf("%d", r.TotalDynamicSql),
			fmt.Sprintf("%d", r.TotalDynamicObject),
			fmt.Sprintf("%d", r.DistinctObjectsUsed),
			boolToStr(r.HasDbAccess),
			boolToStr(r.HasCrossDb),
			r.DbList,
			r.TopObjectsRead,
			r.TopObjectsWrite,
			r.TopObjectsExec,
		}
		if err := w.Write(rec); err != nil {
			return err
		}
	}
	w.Flush()
	return w.Error()
}

func parseBool(s string) bool {
	return strings.EqualFold(strings.TrimSpace(s), "true")
}

func parseInt(s string) int {
	v, _ := strconv.Atoi(strings.TrimSpace(s))
	return v
}

func parseList(raw string) []string {
	parts := strings.Split(raw, ";")
	var res []string
	seen := make(map[string]struct{})
	for _, p := range parts {
		item := strings.TrimSpace(p)
		if item == "" {
			continue
		}
		if _, ok := seen[item]; ok {
			continue
		}
		seen[item] = struct{}{}
		res = append(res, item)
	}
	sort.Strings(res)
	return res
}

func pick(rec []string, idx map[string]int, key string) string {
	if col, ok := idx[key]; ok && col < len(rec) {
		return rec[col]
	}
	return ""
}

func isDynamicQuery(q QueryRow) bool {
	return q.IsDynamic
}

func setToSortedSlice(m map[string]struct{}) []string {
	if len(m) == 0 {
		return nil
	}
	res := make([]string, 0, len(m))
	for v := range m {
		res = append(res, v)
	}
	sort.Strings(res)
	return res
}

func isFileScopePlaceholder(current, raw string) bool {
	cur := strings.ToLower(strings.TrimSpace(current))
	rawLower := strings.ToLower(strings.TrimSpace(raw))
	if fileScopeLabel.MatchString(rawLower) || fileScopeLabel.MatchString(cur) {
		return true
	}
	if !strings.Contains(cur, "@l") && !strings.Contains(rawLower, "@l") {
		return false
	}
	if atLinePattern.MatchString(cur) || atLinePattern.MatchString(rawLower) {
		return true
	}
	if strings.Contains(cur, ".cs@l") || strings.Contains(cur, ".vb@l") || strings.Contains(cur, ".fs@l") {
		return true
	}
	return false
}

func extractLineNumberFromFunc(name string) int {
	if name == "" {
		return 0
	}
	m := atLinePattern.FindStringSubmatch(strings.ToLower(strings.TrimSpace(name)))
	if len(m) != 2 {
		return 0
	}
	return parseInt(m[1])
}

func buildMethodIndex(path string, lines []string, data []byte) []*methodRange {
	ext := strings.ToLower(filepath.Ext(path))
	switch ext {
	case ".go":
		if idx := buildGoMethodIndex(path, len(lines), data); len(idx) > 0 {
			return idx
		}
	case ".cs":
		if idx := buildCsMethodIndex(lines); len(idx) > 0 {
			return idx
		}
	}
	return buildSequentialMethodIndex(lines)
}

func buildGoMethodIndex(path string, totalLines int, data []byte) []*methodRange {
	if totalLines <= 0 {
		return nil
	}
	methodAtLine := make([]*methodRange, totalLines)
	fset := token.NewFileSet()
	fileAst, err := parser.ParseFile(fset, path, data, parser.ParseComments)
	if err != nil {
		return methodAtLine
	}
	fill := func(start, end int, name string) {
		if name == "" {
			return
		}
		if start < 1 {
			start = 1
		}
		if end < start {
			end = start
		}
		if end > totalLines {
			end = totalLines
		}
		mr := &methodRange{Name: name, Start: start, End: end}
		for i := start - 1; i < end && i < len(methodAtLine); i++ {
			methodAtLine[i] = mr
		}
	}
	for _, decl := range fileAst.Decls {
		fd, ok := decl.(*ast.FuncDecl)
		if !ok {
			continue
		}
		pos := fset.Position(fd.Pos())
		end := fset.Position(fd.End())
		startLine := pos.Line
		endLine := end.Line
		if endLine < startLine {
			endLine = startLine
		}
		fill(startLine, endLine, fd.Name.Name)
	}
	return methodAtLine
}

func buildCsMethodIndex(lines []string) []*methodRange {
	if len(lines) == 0 {
		return nil
	}
	methodAtLine := make([]*methodRange, len(lines))
	i := 0
	inString := false
	verbatim := false
	escaped := false

	for i < len(lines) {
		name, braceLine := detectCsMethodSignature(lines, i)
		if name == "" {
			_, _, inString, verbatim, escaped = countBracesAndStringState(lines[i], inString, verbatim, escaped)
			i++
			continue
		}

		startLine := i + 1
		endLine := startLine
		depth := 0
		for idx := i; idx < len(lines); idx++ {
			open, close, nextInString, nextVerbatim, nextEscaped := countBracesAndStringState(lines[idx], inString, verbatim, escaped)
			inString, verbatim, escaped = nextInString, nextVerbatim, nextEscaped
			depth += open
			depth -= close
			if idx == braceLine && depth == 0 {
				depth = 1
			}
			if depth == 0 && idx >= braceLine {
				endLine = idx + 1
				break
			}
			if idx == len(lines)-1 {
				endLine = len(lines)
			}
		}

		mr := &methodRange{Name: name, Start: startLine, End: endLine}
		for line := startLine - 1; line < endLine && line < len(methodAtLine); line++ {
			methodAtLine[line] = mr
		}
		if endLine <= startLine {
			endLine = startLine
		}
		i = endLine
	}

	return methodAtLine
}

func buildSequentialMethodIndex(lines []string) []*methodRange {
	methodAtLine := make([]*methodRange, len(lines))
	i := 0
	inString := false
	verbatim := false
	escaped := false

	for i < len(lines) {
		name, braceLine := detectCsMethodSignature(lines, i)
		if name == "" {
			_, _, inString, verbatim, escaped = countBracesAndStringState(lines[i], inString, verbatim, escaped)
			i++
			continue
		}

		startLine := i + 1
		endLine := startLine
		depth := 0
		for idx := i; idx < len(lines); idx++ {
			open, close, nextInString, nextVerbatim, nextEscaped := countBracesAndStringState(lines[idx], inString, verbatim, escaped)
			inString, verbatim, escaped = nextInString, nextVerbatim, nextEscaped
			depth += open
			depth -= close
			if idx == braceLine && depth == 0 {
				depth = 1
			}
			if depth == 0 && idx >= braceLine {
				endLine = idx + 1
				break
			}
			if idx == len(lines)-1 {
				endLine = len(lines)
			}
		}

		mr := &methodRange{Name: name, Start: startLine, End: endLine}
		for line := startLine - 1; line < endLine && line < len(methodAtLine); line++ {
			methodAtLine[line] = mr
		}
		if endLine <= startLine {
			endLine = startLine
		}
		i = endLine
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
		name, _ := detectCsMethodSignature([]string{trimmed + "{"}, 0)
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

func detectCsMethodSignature(lines []string, start int) (string, int) {
	var buffer strings.Builder
	braceLine := -1
	for i := start; i < len(lines); i++ {
		trimmed := strings.TrimSpace(lines[i])
		if trimmed == "" || strings.HasPrefix(trimmed, "//") {
			continue
		}
		buffer.WriteString(trimmed)
		buffer.WriteString(" ")
		candidate := buffer.String()
		if strings.Contains(candidate, "{") {
			braceLine = i
		}

		if name := extractCsMethodName(candidate); name != "" && braceLine >= 0 {
			return name, braceLine
		}

		if braceLine >= 0 {
			break
		}

		if i-start > 5 {
			break
		}
	}
	return "", -1
}

func extractCsMethodName(candidate string) string {
	trimmed := strings.TrimSpace(candidate)
	if trimmed == "" {
		return ""
	}
	if isCsControlKeyword(leadingToken(trimmed)) {
		return ""
	}
	if m := csMethodWithBrace.FindStringSubmatch(trimmed); len(m) >= 2 {
		return m[1]
	}
	if m := csMethodNoModWithBrace.FindStringSubmatch(trimmed); len(m) >= 2 {
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

func sortedFuncNames(set map[string]struct{}) []string {
	names := setToSortedSlice(set)
	sort.Slice(names, func(i, j int) bool {
		qi := funcNameQuality(names[i])
		qj := funcNameQuality(names[j])
		if qi != qj {
			return qi < qj
		}
		return strings.ToLower(names[i]) < strings.ToLower(names[j])
	})
	return names
}

func topFuncExamples(counts map[string]int, limit int) []string {
	if len(counts) == 0 || limit <= 0 {
		return nil
	}
	type item struct {
		name    string
		count   int
		quality int
	}
	list := make([]item, 0, len(counts))
	for name, count := range counts {
		list = append(list, item{name: name, count: count, quality: funcNameQuality(name)})
	}
	sort.Slice(list, func(i, j int) bool {
		if list[i].count != list[j].count {
			return list[i].count > list[j].count
		}
		if list[i].quality != list[j].quality {
			return list[i].quality < list[j].quality
		}
		if len(list[i].name) != len(list[j].name) {
			return len(list[i].name) > len(list[j].name)
		}
		return strings.ToLower(list[i].name) < strings.ToLower(list[j].name)
	})
	if len(list) > limit {
		list = list[:limit]
	}
	names := make([]string, 0, len(list))
	for _, item := range list {
		names = append(names, item.name)
	}
	return names
}

func maxLineFunction(lines map[string]int) string {
	if len(lines) == 0 {
		return ""
	}
	maxLine := 0
	best := ""
	for fn, line := range lines {
		if line > maxLine || (line == maxLine && strings.ToLower(fn) < strings.ToLower(best)) {
			maxLine = line
			best = fn
		}
	}
	return best
}

func containsFuncName(list []string, name string) bool {
	target := strings.ToLower(strings.TrimSpace(name))
	if target == "" {
		return false
	}
	for _, item := range list {
		if strings.ToLower(strings.TrimSpace(item)) == target {
			return true
		}
	}
	return false
}

func prependIfMissing(list []string, value string) []string {
	if strings.TrimSpace(value) == "" || containsFuncName(list, value) {
		return list
	}
	return append([]string{value}, list...)
}

func dedupeCaseInsensitive(list []string) []string {
	seen := make(map[string]struct{}, len(list))
	result := make([]string, 0, len(list))
	for _, item := range list {
		key := strings.ToLower(strings.TrimSpace(item))
		if key == "" {
			continue
		}
		if _, ok := seen[key]; ok {
			continue
		}
		seen[key] = struct{}{}
		result = append(result, item)
	}
	return result
}

func funcNameQuality(name string) int {
	trimmed := strings.TrimSpace(name)
	if trimmed == "" {
		return 4
	}
	lower := strings.ToLower(trimmed)
	if strings.Contains(lower, "@l") {
		return 3
	}
	if strings.HasPrefix(trimmed, "<") && strings.HasSuffix(trimmed, ">") {
		return 3
	}
	if _, banned := forbiddenFuncNames()[lower]; banned {
		return 2
	}
	if strings.ToUpper(trimmed) == trimmed {
		return 2
	}
	return 0
}

func normalizeQueryFuncs(queries []QueryRow) []QueryRow {
	if len(queries) == 0 {
		return nil
	}
	resolver := NewFuncResolver(getSourceRoot())
	res := make([]QueryRow, len(queries))
	for i, q := range queries {
		q.Func = resolver.Resolve(q.Func, q.RelPath, q.File, q.LineStart)
		if q.IsDynamic {
			q.CallSite = saltDynamicCallSite(q.CallSite, q.QueryHash, q.LineStart, i)
		}
		res[i] = q
	}
	return res
}

// NormalizeFuncName exposes function-name normalization for other packages.
func NormalizeFuncName(raw string) string {
	return normalizeFuncName(raw)
}

// ResolveFuncName exposes resolution helper for other packages.
func ResolveFuncName(raw, rel string, line int) string {
	return resolveFuncName(raw, rel, line)
}

func resolveFuncName(raw, rel string, line int) string {
	name := strings.TrimSpace(raw)
	if name != "" && !strings.EqualFold(name, "<unknown-func>") {
		return name
	}
	anchor := strings.TrimSpace(rel)
	if anchor == "" {
		anchor = "<file-scope>"
	}
	if line <= 0 {
		return fmt.Sprintf("%s@L?", anchor)
	}
	return fmt.Sprintf("%s@L%d", anchor, line)
}

func functionKey(app, rel, fn string) string {
	return strings.Join([]string{app, rel, fn}, "|")
}

func splitFunctionKey(key string) (string, string, string) {
	parts := strings.SplitN(key, "|", 3)
	for len(parts) < 3 {
		parts = append(parts, "")
	}
	return parts[0], parts[1], parts[2]
}

func queryObjectKey(app, rel, file, hash string) string {
	return strings.Join([]string{app, rel, file, hash}, "|")
}

func objectKey(app, rel, file, db, schema, base string) string {
	return strings.Join([]string{app, rel, file, db, schema, base}, "|")
}

func splitObjectKey(key string) (string, string, string, string, string, string) {
	parts := strings.SplitN(key, "|", 6)
	for len(parts) < 6 {
		parts = append(parts, "")
	}
	return parts[0], parts[1], parts[2], parts[3], parts[4], parts[5]
}

func formKey(app, rel, file string) string {
	return strings.Join([]string{app, rel, file}, "|")
}

func splitFormKey(key string) (string, string, string) {
	parts := strings.SplitN(key, "|", 3)
	for len(parts) < 3 {
		parts = append(parts, "")
	}
	return parts[0], parts[1], parts[2]
}

func objectSummaryKey(app, rel, full string) string {
	return strings.Join([]string{app, rel, full}, "|")
}

func objectSummaryKeyDetailed(app, rel, full, base, role, dml string, isPseudo bool, pseudoKind, db, schema string) string {
	parts := []string{
		app,
		rel,
		full,
		strings.TrimSpace(base),
		strings.ToLower(strings.TrimSpace(role)),
		strings.ToUpper(strings.TrimSpace(dml)),
		boolToStr(isPseudo),
		defaultPseudoKind(strings.TrimSpace(pseudoKind)),
		strings.TrimSpace(db),
		strings.TrimSpace(schema),
	}
	return strings.Join(parts, "|")
}

func ObjectSummaryGroupKey(o ObjectRow) string {
	full := chooseFullObjectName(o)
	base := strings.TrimSpace(o.BaseName)
	if base == "" {
		_, _, parsedBase := splitFullObjectName(full)
		base = parsedBase
	}

	isPseudo := o.IsPseudoObject
	pseudoKind := strings.TrimSpace(o.PseudoKind)
	if detected, kind := pseudoObjectInfo(base, pseudoKind); detected {
		isPseudo = true
		pseudoKind = defaultPseudoKind(kind)
	} else if isPseudo {
		pseudoKind = defaultPseudoKind(pseudoKind)
	} else {
		pseudoKind = ""
	}

	return strings.Join([]string{
		o.AppName,
		o.RelPath,
		full,
		strings.TrimSpace(o.DbName),
		strings.TrimSpace(o.SchemaName),
		base,
		boolToStr(isPseudo),
		pseudoKind,
	}, "|")
}

func ObjectSummaryRowKey(row ObjectSummaryRow) string {
	db, schema, parsedBase := splitFullObjectName(row.FullObjectName)
	base := strings.TrimSpace(row.BaseName)
	if base == "" {
		base = parsedBase
	}

	o := normalizeObjectRow(ObjectRow{
		AppName:        row.AppName,
		RelPath:        row.RelPath,
		ObjectName:     row.FullObjectName,
		DbName:         db,
		SchemaName:     schema,
		BaseName:       base,
		IsPseudoObject: row.IsPseudoObject,
		PseudoKind:     row.PseudoKind,
	})

	return ObjectSummaryGroupKey(o)
}

func boolToStr(b bool) string {
	if b {
		return "true"
	}
	return "false"
}

func buildFullName(db, schema, base string) string {
	var parts []string
	if db != "" {
		parts = append(parts, filepath.ToSlash(db))
	}
	if schema != "" {
		parts = append(parts, filepath.ToSlash(schema))
	}
	if base != "" {
		parts = append(parts, filepath.ToSlash(base))
	}
	return strings.Join(parts, ".")
}

func NormalizeObjectRows(objects []ObjectRow) []ObjectRow {
	if len(objects) == 0 {
		return objects
	}
	norm := make([]ObjectRow, 0, len(objects))
	for _, o := range objects {
		norm = append(norm, normalizeObjectRow(o))
	}
	return norm
}

func normalizeObjectRow(o ObjectRow) ObjectRow {
	o.ObjectName = normalizeFullObjectName(o.ObjectName)
	o.DbName = cleanIdentifier(o.DbName)
	o.SchemaName = cleanIdentifier(o.SchemaName)
	o.BaseName = cleanIdentifier(o.BaseName)

	if o.ObjectName != "" {
		parts := strings.Split(o.ObjectName, ".")
		if len(parts) > 0 && strings.TrimSpace(o.BaseName) == "" {
			o.BaseName = cleanIdentifier(parts[len(parts)-1])
		}
		if len(parts) >= 2 && strings.TrimSpace(o.SchemaName) == "" {
			o.SchemaName = cleanIdentifier(parts[len(parts)-2])
		}
		if len(parts) >= 3 && strings.TrimSpace(o.DbName) == "" {
			o.DbName = cleanIdentifier(parts[0])
		}
	}

	if strings.TrimSpace(o.DbName) == "" && strings.TrimSpace(o.SchemaName) == "" {
		o.SchemaName = "dbo"
	}

	if detected, kind := pseudoObjectInfo(o.BaseName, o.PseudoKind); detected {
		o.IsPseudoObject = true
		o.PseudoKind = defaultPseudoKind(kind)
	} else {
		o.IsPseudoObject = false
		o.PseudoKind = ""
	}

	// Rebuild object name with normalized parts to ensure consistency.
	o.ObjectName = normalizeFullObjectName(buildFullName(o.DbName, o.SchemaName, o.BaseName))
	return o
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

// ChooseFullObjectName exposes the full object name selection logic for
// external packages while preserving the existing internal helper.
func ChooseFullObjectName(o ObjectRow) string {
	return chooseFullObjectName(o)
}

func cleanIdentifier(raw string) string {
	return strings.Trim(strings.TrimSpace(raw), "[]\"")
}

func chooseFullObjectName(o ObjectRow) string {
	if name := strings.TrimSpace(o.ObjectName); name != "" {
		return name
	}
	full := buildFullName(o.DbName, o.SchemaName, o.BaseName)
	if strings.TrimSpace(full) != "" {
		return full
	}
	return strings.TrimSpace(o.BaseName)
}

func splitFullObjectName(full string) (db, schema, base string) {
	parts := strings.Split(full, ".")
	switch len(parts) {
	case 0:
		return "", "", ""
	case 1:
		return "", "", strings.TrimSpace(parts[0])
	case 2:
		return "", strings.TrimSpace(parts[0]), strings.TrimSpace(parts[1])
	default:
		return strings.TrimSpace(parts[0]), strings.TrimSpace(parts[1]), strings.TrimSpace(parts[len(parts)-1])
	}
}

func normalizeFuncName(raw string) string {
	name := strings.TrimSpace(raw)
	if name == "" {
		return "<unknown-func>"
	}
	lower := strings.ToLower(name)
	if _, banned := forbiddenFuncNames()[lower]; banned {
		return "<unknown-func>"
	}
	if isAllUpperShort(name) {
		return "<unknown-func>"
	}
	if isLikelyFuncName(name) {
		return name
	}
	return "<unknown-func>"
}

func forbiddenFuncNames() map[string]struct{} {
	return map[string]struct{}{
		"exception": {},
		"convert":   {},
		"cast":      {},
		"in":        {},
		"isnull":    {},
		"nvarchar":  {},
		"varchar":   {},
		"bigint":    {},
		"len":       {},
		"openxml":   {},
		"count":     {},
		"select":    {},
		"from":      {},
		"where":     {},
		"group":     {},
		"order":     {},
		"join":      {},
		"left":      {},
		"right":     {},
		"inner":     {},
		"outer":     {},
		"insert":    {},
		"update":    {},
		"delete":    {},
		"truncate":  {},
		"exec":      {},
		"coalesce":  {},
		"substring": {},
		"case":      {},
		"when":      {},
		"then":      {},
		"else":      {},
		"end":       {},
	}
}

func isLikelyFuncName(name string) bool {
	if strings.HasPrefix(name, "<") && strings.HasSuffix(name, ">") {
		return false
	}
	forbidden := forbiddenFuncNames()
	if _, ok := forbidden[strings.ToLower(name)]; ok {
		return false
	}
	for _, r := range name {
		if !(r == '_' || r == '.' || r == '@' || ('0' <= r && r <= '9') || ('A' <= r && r <= 'Z') || ('a' <= r && r <= 'z')) {
			return false
		}
	}
	first := name[0]
	if (first >= '0' && first <= '9') || first == '.' {
		return false
	}
	return true
}

func isAllUpperShort(name string) bool {
	if len(name) == 0 || len(name) > 3 {
		return false
	}
	hasLetter := false
	for _, r := range name {
		if unicode.IsLetter(r) {
			hasLetter = true
			if !unicode.IsUpper(r) {
				return false
			}
		}
	}
	return hasLetter
}

func isDynamicBaseName(base string) bool {
	trimmed := strings.ToLower(strings.TrimSpace(base))
	if trimmed == "<dynamic-sql>" {
		return true
	}
	return strings.HasPrefix(trimmed, "<dynamic-object")
}

func shouldSkipObject(o ObjectRow) bool {
	base := strings.TrimSpace(o.BaseName)
	if isDynamicBaseName(base) {
		return false
	}
	if base == "" {
		return true
	}
	lower := strings.ToLower(base)
	if lower == "eq" || lower == "dbo" || lower == "dbo." {
		return true
	}
	if strings.HasSuffix(base, ".") {
		return true
	}
	return false
}

func pseudoObjectInfo(base string, pseudoKindHint string) (bool, string) {
	trimmed := strings.TrimSpace(base)
	if trimmed == "" {
		return false, ""
	}
	lower := strings.ToLower(trimmed)
	switch {
	case lower == "<dynamic-sql>":
		return true, "dynamic-sql"
	case strings.HasPrefix(lower, "<dynamic-object"):
		return true, "dynamic-object"
	case strings.HasPrefix(lower, "<") && strings.HasSuffix(lower, ">"):
		kind := strings.TrimSpace(strings.TrimSuffix(strings.TrimPrefix(lower, "<"), ">"))
		if kind == "" {
			kind = "unknown"
		}
		return true, kind
	}
	hint := defaultPseudoKind(pseudoKindHint)
	if strings.HasPrefix(hint, "dynamic-") && hint != "unknown" {
		return true, hint
	}
	return false, ""
}

func choosePseudoKind(current, candidate string) string {
	pri := func(kind string) int {
		switch strings.ToLower(strings.TrimSpace(kind)) {
		case "dynamic-sql":
			return 3
		case "dynamic-object":
			return 2
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

func isWriteDml(dml string) bool {
	switch strings.ToUpper(strings.TrimSpace(dml)) {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE":
		return true
	default:
		return false
	}
}
