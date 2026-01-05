package summary

import (
	"encoding/csv"
	"fmt"
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
	AppName    string
	RelPath    string
	File       string
	SourceCat  string
	SourceKind string
	CallSite   string
	DynamicSig string
	Func       string
	RawSql     string
	SqlClean   string
	QueryHash  string
	UsageKind  string
	IsWrite    bool
	IsDynamic  bool
	HasCrossDb bool
	DbList     []string
	ConnDb     string
	LineStart  int
	LineEnd    int
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
	TotalDynamicSql          int
	TotalDynamicObject       int
	DynamicSig               string
	TotalWrite               int
	TotalObjects             int
	TopObjects               string
	ObjectsUsed              string
	TopObjectsShort          string
	DynamicPseudoKinds       string
	DynamicExampleSignatures string
	HasCrossDb               bool
	DbList                   string
	LineStart                int
	LineEnd                  int
}

// ObjectSummaryRow represents aggregated information per database object.
type ObjectSummaryRow struct {
	AppName        string
	RelPath        string
	BaseName       string
	FullObjectName string
	Roles          string
	RolesSummary   string
	DmlKinds       string
	TotalReads     int
	TotalWrites    int
	IsPseudoObject bool
	PseudoKind     string
	TotalExec      int
	TotalFuncs     int
	ExampleFuncs   string
	HasCrossDb     bool
	DbList         string
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
	HasCrossDb           bool
	HasDbAccess          bool
	TotalObjects         int
	DistinctObjectsUsed  int
	TopObjects           string
	TopObjectsByExec     string
	DbList               string
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
	return fmt.Sprintf("%s::%d", label, line)
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
	info := &fileMethodIndex{
		lines:        lines,
		methodAtLine: buildSequentialMethodIndex(lines),
	}
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
			AppName:    rec[idx["AppName"]],
			RelPath:    rec[idx["RelPath"]],
			File:       rec[idx["File"]],
			SourceCat:  pick(rec, idx, "SourceCategory"),
			SourceKind: pick(rec, idx, "SourceKind"),
			CallSite:   pick(rec, idx, "CallSiteKind"),
			DynamicSig: pick(rec, idx, "DynamicSignature"),
			RawSql:     pick(rec, idx, "RawSql"),
			SqlClean:   pick(rec, idx, "SqlClean"),
			Func:       rec[idx["Func"]],
			QueryHash:  rec[idx["QueryHash"]],
			UsageKind:  rec[idx["UsageKind"]],
			IsWrite:    parseBool(rec[idx["IsWrite"]]),
			IsDynamic:  parseBool(rec[idx["IsDynamic"]]),
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
		rows = append(rows, row)
	}
	return rows, nil
}

func BuildFunctionSummary(queries []QueryRow, objects []ObjectRow) ([]FunctionSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
	objects = NormalizeObjectRows(objects)
	grouped := make(map[string][]QueryRow)
	queryByKey := make(map[string]QueryRow)

	for _, q := range normQueries {
		key := functionKey(q.AppName, q.RelPath, q.Func)
		grouped[key] = append(grouped[key], q)
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	objectsByQuery := map[string][]ObjectRow{}
	for _, o := range objects {
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		objectsByQuery[qKey] = append(objectsByQuery[qKey], o)
	}

	var result []FunctionSummaryRow
	for key, qRows := range grouped {
		app, rel, fn := splitFunctionKey(key)
		if strings.EqualFold(strings.TrimSpace(fn), "<unknown-func>") {
			if recovered := recoverBestFuncName(qRows); recovered != "" {
				fn = recovered
			} else {
				continue
			}
		}
		var totalExec, totalSelect, totalInsert, totalUpdate, totalDelete, totalTruncate, totalDynamic, totalDynamicSql, totalDynamicObject, totalWrite int
		dynamicCount := 0
		hasCross := false
		minLine := 0
		maxLine := 0
		dbListSet := make(map[string]struct{})
		objectCounter := make(map[string]*objectRoleCounter)
		dynamicGroups := make(map[string]dynamicGroup)
		dynamicSigCounts := make(map[string]dynamicSignatureInfo)
		dynamicPseudoKinds := make(map[string]int)
		dynamicExampleCounts := make(map[string]int)
		pseudoKindsPerQuery := make(map[string]map[string]struct{})
		for _, q := range qRows {
			qKey := queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)
			baseKinds := make(map[string]struct{})
			upperUsage := strings.ToUpper(strings.TrimSpace(q.UsageKind))
			if upperUsage != "" && upperUsage != "UNKNOWN" {
				baseKinds[upperUsage] = struct{}{}
			}

			dynSqlPseudo := false
			dynObjPseudo := false
			for _, o := range objectsByQuery[qKey] {
				if !o.IsPseudoObject {
					continue
				}
				kind := normalizePseudoKindLabel(o.BaseName, o.PseudoKind)
				if kind == "dynamic-sql" {
					dynSqlPseudo = true
				} else if strings.TrimSpace(kind) != "" {
					dynObjPseudo = true
				}
			}

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
				dynamicCount++
				if dynSqlPseudo || (!dynSqlPseudo && !dynObjPseudo) {
					totalDynamicSql++
				}
				if dynObjPseudo {
					totalDynamicObject++
				}
				groupKey := buildDynamicGroupKey(q)
				grp := dynamicGroups[groupKey]
				grp.count++
				grp.usageKind = usageKindWithFallback(q.UsageKind)
				grp.callSite = callSiteKind(q)
				grp.pseudoKind = pickDynamicPseudoKind(grp.pseudoKind, dynSqlPseudo, dynObjPseudo)
				if grp.minLine == 0 || q.LineStart < grp.minLine {
					grp.minLine = q.LineStart
				}
				if grp.maxLine == 0 || q.LineEnd > grp.maxLine {
					grp.maxLine = q.LineEnd
				}
				dynamicGroups[groupKey] = grp
				sig := dynamicSignature(q)
				if sig == "" {
					sig = q.QueryHash
				}
				if sig == "" && q.LineStart > 0 {
					sig = fmt.Sprintf("%s@%d", strings.TrimSpace(q.RelPath), q.LineStart)
				}
				if sig != "" {
					entry := dynamicSigCounts[sig]
					entry.count++
					if entry.exampleHash == "" {
						entry.exampleHash = q.QueryHash
					}
					dynamicSigCounts[sig] = entry
				}
				exampleSig := dynamicExampleSignature(q, objectsByQuery[qKey])
				if exampleSig != "" {
					dynamicExampleCounts[exampleSig]++
				}
			}
			for _, o := range objectsByQuery[qKey] {
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
			counter := objectCounter[name]
			if counter == nil {
				counter = &objectRoleCounter{}
				objectCounter[name] = counter
			}
			qRow, hasQuery := queryByKey[queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)]
			counter.Register(o, qRow, hasQuery)
		}
		for _, q := range qRows {
			qKey := queryObjectKey(app, rel, q.File, q.QueryHash)
			for _, o := range objectsByQuery[qKey] {
				consumeObj(o)
			}
		}
		displayCounter := filterDynamicPseudoObjects(objectCounter)
		topObjects := buildTopObjectSummary(displayCounter, 10)
		topObjectsShort := buildTopObjectSummary(displayCounter, 5)
		objectsUsed := buildObjectsUsedDetailed(displayCounter)
		dynamicDisplay := summarizeDynamicGroups(dynamicGroups, 3)
		if dynamicDisplay != "" {
			topObjects = appendSummary(topObjects, dynamicDisplay)
			topObjectsShort = appendSummary(topObjectsShort, dynamicDisplay)
			objectsUsed = appendSummary(objectsUsed, dynamicDisplay)
		}
		dbList := setToSortedSlice(dbListSet)
		totalDynamic = dynamicCount
		dynamicSig := summarizeDynamicSignatures(dynamicSigCounts)
		dynamicPseudoSummary := summarizePseudoKindCounts(dynamicPseudoKinds)
		dynamicExampleSummary := summarizeDynamicExamples(dynamicExampleCounts, 3)
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
			TotalQueries:             len(qRows),
			TotalExec:                totalExec,
			TotalSelect:              totalSelect,
			TotalInsert:              totalInsert,
			TotalUpdate:              totalUpdate,
			TotalDelete:              totalDelete,
			TotalTruncate:            totalTruncate,
			TotalDynamic:             totalDynamic,
			TotalDynamicSql:          totalDynamicSql,
			TotalDynamicObject:       totalDynamicObject,
			DynamicSig:               dynamicSig,
			DynamicPseudoKinds:       dynamicPseudoSummary,
			DynamicExampleSignatures: dynamicExampleSummary,
			TotalWrite:               totalWrite,
			TotalObjects:             len(objSet),
			TopObjects:               topObjects,
			TopObjectsShort:          topObjectsShort,
			ObjectsUsed:              objectsUsed,
			HasCrossDb:               hasCross,
			DbList:                   strings.Join(dbList, ";"),
			LineStart:                minLine,
			LineEnd:                  maxLine,
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		return a.Func < b.Func
	})

	return result, nil
}

// ValidateFunctionSummaryCounts ensures that the function summary aligns with the raw query rows.
// It groups queries by (AppName, RelPath, Func) and checks TotalQueries and TotalDynamic against the summary rows.
func ValidateFunctionSummaryCounts(queries []QueryRow, summaries []FunctionSummaryRow) error {
	normQueries := normalizeQueryFuncs(queries)
	expectedTotals := make(map[string]int)
	expectedDynamic := make(map[string]int)
	for _, q := range normQueries {
		key := strings.Join([]string{q.AppName, q.RelPath, q.Func}, "|")
		expectedTotals[key]++
		if q.IsDynamic {
			expectedDynamic[key]++
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
		if sum.TotalQueries != total || sum.TotalDynamic != expectedDyn {
			mismatches = append(mismatches, fmt.Sprintf("%s/%s mismatch (expected total=%d dyn=%d, summary total=%d dyn=%d)", rel, fn, total, expectedDyn, sum.TotalQueries, sum.TotalDynamic))
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

func BuildObjectSummary(queries []QueryRow, objects []ObjectRow) ([]ObjectSummaryRow, error) {
	queryByKey := make(map[string]QueryRow)
	for _, q := range normalizeQueryFuncs(queries) {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	type agg struct {
		appName     string
		relPath     string
		fullName    string
		baseName    string
		dbName      string
		schemaName  string
		funcSet     map[string]struct{}
		funcCounts  map[string]int
		firstFunc   string
		reads       int
		writes      int
		execs       int
		dmlSet      map[string]struct{}
		dbSet       map[string]struct{}
		hasCross    bool
		isPseudo    bool
		pseudoKinds map[string]int
	}

	objects = NormalizeObjectRows(objects)

	grouped := make(map[string]*agg)
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
			entry = &agg{
				appName:     o.AppName,
				relPath:     o.RelPath,
				fullName:    fullName,
				baseName:    strings.TrimSpace(o.BaseName),
				dbName:      strings.TrimSpace(o.DbName),
				schemaName:  strings.TrimSpace(o.SchemaName),
				funcSet:     make(map[string]struct{}),
				funcCounts:  make(map[string]int),
				dmlSet:      make(map[string]struct{}),
				dbSet:       make(map[string]struct{}),
				pseudoKinds: make(map[string]int),
			}
			grouped[key] = entry
		}

		qRow, hasQuery := queryByKey[queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)]
		read, write, exec := ObjectRoleCounts(o, qRow, hasQuery)
		entry.reads += read
		entry.writes += write
		entry.execs += exec

		if o.IsCrossDb {
			entry.hasCross = true
		}
		if db := strings.TrimSpace(o.DbName); db != "" {
			entry.dbSet[db] = struct{}{}
		}
		upperDml := strings.ToUpper(strings.TrimSpace(o.DmlKind))
		if upperDml != "" {
			for _, part := range strings.Split(upperDml, ";") {
				trimmed := strings.TrimSpace(part)
				if trimmed == "" {
					continue
				}
				entry.dmlSet[trimmed] = struct{}{}
			}
		}

		if o.IsPseudoObject {
			entry.isPseudo = true
			entry.pseudoKinds[defaultPseudoKind(o.PseudoKind)]++
		}

		fn := strings.TrimSpace(o.Func)
		if fn != "" {
			entry.funcSet[fn] = struct{}{}
			entry.funcCounts[fn]++
			if entry.firstFunc == "" {
				entry.firstFunc = fn
			}
		}
	}

	var result []ObjectSummaryRow
	for _, entry := range grouped {
		funcs := sortedFuncNames(entry.funcSet)
		exampleFuncs := topFuncExamples(entry.funcCounts, 5)
		if len(exampleFuncs) == 0 {
			exampleFuncs = funcs
			if len(exampleFuncs) > 5 {
				exampleFuncs = exampleFuncs[:5]
			}
		}
		if len(exampleFuncs) == 0 && entry.firstFunc != "" {
			exampleFuncs = append([]string{entry.firstFunc}, exampleFuncs...)
			if len(exampleFuncs) > 5 {
				exampleFuncs = exampleFuncs[:5]
			}
		}

		pseudoKind := summarizePseudoKindsFlat(entry.pseudoKinds)
		if entry.isPseudo && pseudoKind == "" {
			pseudoKind = "unknown"
		}

		roleLabel := summarizeRoleLabel(entry.reads, entry.writes, entry.execs)
		roleCounts := summarizeRoleCounts(entry.reads, entry.writes, entry.execs)
		dmlKinds := setToSortedSlice(entry.dmlSet)

		result = append(result, ObjectSummaryRow{
			AppName:        entry.appName,
			RelPath:        entry.relPath,
			BaseName:       entry.baseName,
			FullObjectName: entry.fullName,
			Roles:          roleLabel,
			RolesSummary:   roleCounts,
			DmlKinds:       strings.Join(dmlKinds, ";"),
			TotalReads:     entry.reads,
			TotalWrites:    entry.writes,
			TotalExec:      entry.execs,
			TotalFuncs:     len(funcs),
			ExampleFuncs:   strings.Join(exampleFuncs, ";"),
			IsPseudoObject: entry.isPseudo,
			PseudoKind:     pseudoKind,
			HasCrossDb:     entry.hasCross,
			DbList:         strings.Join(setToSortedSlice(entry.dbSet), ";"),
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
		if a.Roles != b.Roles {
			return a.Roles < b.Roles
		}
		return a.RelPath < b.RelPath
	})

	return result, nil
}

func BuildFormSummary(queries []QueryRow, objects []ObjectRow) ([]FormSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
	objects = NormalizeObjectRows(objects)
	groupQueries := make(map[string][]QueryRow)
	objectSetByForm := make(map[string]map[string]struct{})
	objectCountByForm := make(map[string]int)
	hasCrossByForm := make(map[string]bool)
	topObjectStats := make(map[string]map[string]*objectRoleCounter)
	dbListByForm := make(map[string]map[string]struct{})
	funcSetByForm := make(map[string]map[string]struct{})
	fileByForm := make(map[string]string)
	queryByKey := make(map[string]QueryRow)

	for _, q := range normQueries {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
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

		baseName := strings.TrimSpace(o.BaseName)
		if _, ok := topObjectStats[key]; !ok {
			topObjectStats[key] = make(map[string]*objectRoleCounter)
		}
		counter := topObjectStats[key][baseName]
		if counter == nil {
			counter = &objectRoleCounter{}
			topObjectStats[key][baseName] = counter
		}
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		qRow, hasQuery := queryByKey[qKey]
		counter.Register(o, qRow, hasQuery)
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
			if isDynamicQuery(q) {
				totalDynamic++
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
		totalWrite = totalInsert + totalUpdate + totalDelete + totalTruncate + totalExec
		distinctObjects := len(objectSetByForm[key])
		hasCross := hasCrossByForm[key]
		for db := range dbListByForm[key] {
			dbListSet[db] = struct{}{}
		}

		topObjects := buildTopObjects(topObjectStats[key])
		topObjectsByExec := buildTopObjectsByRole(topObjectStats[key], "exec", 5)
		if distinctObjects == 0 {
			topObjects = ""
			topObjectsByExec = ""
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
			TotalExec:            totalExec,
			TotalObjects:         totalObjects,
			DistinctObjectsUsed:  distinctObjects,
			TopObjects:           topObjects,
			TopObjectsByExec:     topObjectsByExec,
			HasCrossDb:           hasCross,
			HasDbAccess:          hasDbAccess,
			DbList:               strings.Join(setToSortedSlice(dbListSet), ";"),
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
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
	flags := dynamicPseudoRoleFlags(o, q, hasQuery)
	return normalizeRoleFlags(flags)
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
		total := counter.ExecCount + counter.WriteCount + counter.ReadCount
		if total == 0 && !(counter.HasExec || counter.HasWrite || counter.HasRead) {
			continue
		}
		if total == 0 {
			total = 1
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
	formatName := func(name string, counter *objectRoleCounter) string {
		if counter == nil || !counter.IsPseudo {
			return name
		}
		return "[P] " + name
	}
	display := tops
	if len(display) > limit {
		display = display[:limit]
	}
	parts := make([]string, 0, len(display)+1)
	for _, t := range display {
		name := formatName(t.name, t.roles)
		roleLabel := describeRolesOrdered(t.roles)
		roleCount := 0
		switch roleLabel {
		case "exec":
			roleCount = t.roles.ExecCount
		case "write":
			roleCount = t.roles.WriteCount
		case "read":
			roleCount = t.roles.ReadCount
		default:
			roleLabel = "mixed"
			roleCount = t.roles.ExecCount + t.roles.WriteCount + t.roles.ReadCount
		}
		parts = append(parts, fmt.Sprintf("%s(%s=%d)", name, roleLabel, roleCount))
	}
	if len(tops) > len(display) {
		parts = append(parts, "...")
	}
	return strings.Join(parts, "; ")
}

func buildObjectsUsedDetailed(stats map[string]*objectRoleCounter) string {
	if len(stats) == 0 {
		return ""
	}
	type entry struct {
		name  string
		score int
		role  *objectRoleCounter
	}
	entries := make([]entry, 0, len(stats))
	for name, counter := range stats {
		score := objectDisplayScore(counter)
		if score == 0 && !(counter.HasExec || counter.HasWrite || counter.HasRead) {
			continue
		}
		entries = append(entries, entry{name: name, score: score, role: counter})
	}
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].score != entries[j].score {
			return entries[i].score > entries[j].score
		}
		return strings.ToLower(entries[i].name) < strings.ToLower(entries[j].name)
	})
	parts := make([]string, 0, len(entries))
	for _, e := range entries {
		roleDetails := describeRoleCountsDetailed(e.role)
		parts = append(parts, fmt.Sprintf("%s(%s)", e.name, roleDetails))
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
		count := roleCount(counter, role)
		if count == 0 {
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
	if len(entries) > limit {
		entries = entries[:limit]
	}
	parts := make([]string, 0, len(entries))
	for _, e := range entries {
		parts = append(parts, fmt.Sprintf("%s(%s=%d)", e.name, role, e.count))
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

func ObjectRoleBuckets(o ObjectRow) (int, int, int) {
	return ObjectRoleCounts(o, QueryRow{}, false)
}

func ObjectRoleCounts(o ObjectRow, q QueryRow, hasQuery bool) (int, int, int) {
	flags := roleFlagsForObject(o, q, hasQuery)
	read := 0
	write := 0
	exec := 0
	if flags.read {
		read = 1
	}
	if flags.write {
		write = 1
	}
	if flags.exec {
		exec = 1
	}
	return read, write, exec
}

func buildTopObjects(stats map[string]*objectRoleCounter) string {
	if len(stats) == 0 {
		return ""
	}
	type entry struct {
		name  string
		score int
		role  *objectRoleCounter
	}
	var entries []entry
	for name, counter := range stats {
		score := objectDisplayScore(counter)
		if score == 0 && !(counter.HasExec || counter.HasWrite || counter.HasRead) {
			continue
		}
		entries = append(entries, entry{name: name, score: score, role: counter})
	}
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].score != entries[j].score {
			return entries[i].score > entries[j].score
		}
		return strings.ToLower(entries[i].name) < strings.ToLower(entries[j].name)
	})
	display := entries
	overflow := 0
	limit := 8
	if len(display) > limit {
		overflow = len(display) - limit
		display = display[:limit]
	}
	parts := make([]string, 0, len(display)+1)
	for _, e := range display {
		roleLabel := describeRolesOrdered(e.role)
		roleCount := 0
		switch roleLabel {
		case "exec":
			roleCount = e.role.ExecCount
		case "write":
			roleCount = e.role.WriteCount
		case "read":
			roleCount = e.role.ReadCount
		default:
			roleCount = e.role.ExecCount + e.role.WriteCount + e.role.ReadCount
			roleLabel = "mixed"
		}
		parts = append(parts, fmt.Sprintf("%s(%s=%d)", e.name, roleLabel, roleCount))
	}
	if overflow > 0 {
		parts = append(parts, "...")
	}
	return strings.Join(parts, "; ")
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
	header := []string{"AppName", "RelPath", "Func", "LineStart", "LineEnd", "TotalQueries", "TotalSelect", "TotalInsert", "TotalUpdate", "TotalDelete", "TotalTruncate", "TotalExec", "TotalWrite", "TotalDynamic", "TotalDynamicSql", "TotalDynamicObject", "DynamicSignatures", "TotalObjects", "TopObjects", "ObjectsUsed", "HasCrossDb", "DbList", "TopObjectsShort", "DynamicPseudoKinds", "DynamicExampleSignatures"}
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
			fmt.Sprintf("%d", r.TotalDynamicSql),
			fmt.Sprintf("%d", r.TotalDynamicObject),
			r.DynamicSig,
			fmt.Sprintf("%d", r.TotalObjects),
			r.TopObjects,
			r.ObjectsUsed,
			boolToStr(r.HasCrossDb),
			r.DbList,
			r.TopObjectsShort,
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
	header := []string{"AppName", "RelPath", "FullObjectName", "BaseName", "Roles", "RolesSummary", "DmlKinds", "TotalReads", "TotalWrites", "TotalExec", "TotalFuncs", "ExampleFuncs", "IsPseudoObject", "PseudoKind", "HasCrossDb", "DbList"}
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
	header := []string{"AppName", "RelPath", "File", "TotalFunctionsWithDB", "TotalQueries", "TotalObjects", "TotalExec", "TotalWrite", "TotalDynamic", "DistinctObjectsUsed", "HasDbAccess", "HasCrossDb", "DbList", "TopObjects", "TopObjectsByExec"}
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
			fmt.Sprintf("%d", r.DistinctObjectsUsed),
			boolToStr(r.HasDbAccess),
			boolToStr(r.HasCrossDb),
			r.DbList,
			r.TopObjects,
			r.TopObjectsByExec,
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
