package summary

import (
	"encoding/csv"
	"fmt"
	"io"
	"os"
	"sort"
	"strconv"
	"strings"
	"unicode"
)

// QueryRow represents a row from QueryUsage.csv used for summaries.
type QueryRow struct {
	AppName    string
	RelPath    string
	File       string
	SourceCat  string
	SourceKind string
	CallSite   string
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
	AppName       string
	RelPath       string
	Func          string
	TotalQueries  int
	TotalExec     int
	TotalSelect   int
	TotalInsert   int
	TotalUpdate   int
	TotalDelete   int
	TotalTruncate int
	TotalDynamic  int
	DynamicSig    string
	TotalWrite    int
	TotalObjects  int
	ObjectsUsed   string
	HasCrossDb    bool
	DbList        string
	LineStart     int
	LineEnd       int
}

// ObjectSummaryRow represents aggregated information per database object.
type ObjectSummaryRow struct {
	AppName        string
	RelPath        string
	BaseName       string
	FullObjectName string
	Roles          string
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
	DbList               string
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
	required := []string{"AppName", "RelPath", "File", "QueryHash", "DbName", "SchemaName", "BaseName", "Role", "DmlKind", "IsWrite", "IsCrossDb"}
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
			AppName:    rec[idx["AppName"]],
			RelPath:    rec[idx["RelPath"]],
			File:       rec[idx["File"]],
			QueryHash:  rec[idx["QueryHash"]],
			DbName:     rec[idx["DbName"]],
			SchemaName: rec[idx["SchemaName"]],
			BaseName:   rec[idx["BaseName"]],
			Role:       rec[idx["Role"]],
			DmlKind:    rec[idx["DmlKind"]],
			IsWrite:    parseBool(rec[idx["IsWrite"]]),
			IsCrossDb:  parseBool(rec[idx["IsCrossDb"]]),
		}
		if col, ok := idx["ObjectName"]; ok {
			row.ObjectName = rec[col]
		}
		if col, ok := idx["IsObjectNameDynamic"]; ok {
			row.IsObjectNameDyn = parseBool(rec[col])
		}
		if col, ok := idx["IsPseudoObject"]; ok {
			row.IsPseudoObject = parseBool(rec[col])
		}
		if col, ok := idx["PseudoKind"]; ok {
			row.PseudoKind = rec[col]
		}
		if col, ok := idx["Func"]; ok {
			row.Func = rec[col]
		}
		rows = append(rows, row)
	}
	return rows, nil
}

func BuildFunctionSummary(queries []QueryRow, objects []ObjectRow) ([]FunctionSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
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
		var totalExec, totalSelect, totalInsert, totalUpdate, totalDelete, totalTruncate, totalDynamic, totalWrite int
		hasCross := false
		minLine := 0
		maxLine := 0
		dbListSet := make(map[string]struct{})
		objectCounter := make(map[string]*objectRoleCounter)
		dynamicSigCounts := make(map[string]dynamicSignatureInfo)
		for _, q := range qRows {
			switch strings.ToUpper(q.UsageKind) {
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
			if isDynamicQuery(q) {
				totalDynamic++
				sig := dynamicSignature(q)
				if sig != "" {
					entry := dynamicSigCounts[sig]
					entry.count++
					if entry.exampleHash == "" {
						entry.exampleHash = q.QueryHash
					}
					dynamicSigCounts[sig] = entry
				}
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

		totalWrite = totalInsert + totalUpdate + totalDelete + totalTruncate + totalExec

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
		objectsUsed := buildTopObjectSummary(objectCounter, 10)
		dbList := setToSortedSlice(dbListSet)
		dynamicSig := summarizeDynamicSignatures(dynamicSigCounts)
		if maxLine == 0 && minLine > 0 {
			maxLine = minLine
		}
		if maxLine > 0 && (minLine == 0 || minLine > maxLine) {
			minLine = maxLine
		}

		result = append(result, FunctionSummaryRow{
			AppName:       app,
			RelPath:       rel,
			Func:          fn,
			TotalQueries:  len(qRows),
			TotalExec:     totalExec,
			TotalSelect:   totalSelect,
			TotalInsert:   totalInsert,
			TotalUpdate:   totalUpdate,
			TotalDelete:   totalDelete,
			TotalTruncate: totalTruncate,
			TotalDynamic:  totalDynamic,
			DynamicSig:    dynamicSig,
			TotalWrite:    totalWrite,
			TotalObjects:  len(objSet),
			ObjectsUsed:   objectsUsed,
			HasCrossDb:    hasCross,
			DbList:        strings.Join(dbList, ";"),
			LineStart:     minLine,
			LineEnd:       maxLine,
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.Func != b.Func {
			return a.Func < b.Func
		}
		return a.LineStart < b.LineStart
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
		if isDynamicQuery(q) {
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
		if sum.TotalQueries != total || sum.TotalDynamic != expectedDynamic[key] {
			mismatches = append(mismatches, fmt.Sprintf("%s/%s mismatch (expected total=%d dyn=%d, summary total=%d dyn=%d)", rel, fn, total, expectedDynamic[key], sum.TotalQueries, sum.TotalDynamic))
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
	type agg struct {
		appName        string
		relPath        string
		fullName       string
		baseName       string
		funcSet        map[string]struct{}
		funcCounts     map[string]int
		reads          int
		writes         int
		execs          int
		dmlSet         map[string]struct{}
		dbSet          map[string]struct{}
		hasCross       bool
		isPseudo       bool
		pseudoKind     string
		hasPseudoLines bool
	}

	grouped := make(map[string]*agg)
	queryByKey := make(map[string]QueryRow)
	for _, q := range queries {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}
	for _, o := range objects {
		if shouldSkipObject(o) {
			continue
		}
		fullName := chooseFullObjectName(o)
		if strings.TrimSpace(fullName) == "" {
			continue
		}
		dbParsed, schemaParsed, parsedBase := splitFullObjectName(fullName)
		baseName := strings.TrimSpace(o.BaseName)
		if baseName == "" {
			baseName = parsedBase
		}
		pseudoKind := defaultPseudoKind(o.PseudoKind)
		isPseudoObj := o.IsPseudoObject
		if detected, kind := pseudoObjectInfo(baseName, pseudoKind); detected {
			isPseudoObj = true
			pseudoKind = defaultPseudoKind(choosePseudoKind(pseudoKind, defaultPseudoKind(kind)))
		}
		roleKey := "" // summary CSV does not carry raw role; keep empty for stable keying.
		key := objectSummaryKeyDetailed(
			o.AppName,
			o.RelPath,
			fullName,
			baseName,
			roleKey,
			"",
			isPseudoObj,
			pseudoKind,
			dbParsed,
			schemaParsed,
		)
		entry := grouped[key]
		if entry == nil {
			entry = &agg{
				appName:    o.AppName,
				relPath:    o.RelPath,
				fullName:   fullName,
				baseName:   baseName,
				funcSet:    make(map[string]struct{}),
				funcCounts: make(map[string]int),
				dmlSet:     make(map[string]struct{}),
				dbSet:      make(map[string]struct{}),
			}
			grouped[key] = entry
		}

		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		qRow, hasQuery := queryByKey[qKey]
		flags := roleFlagsForObject(o, qRow, hasQuery)
		upperDml := strings.ToUpper(strings.TrimSpace(o.DmlKind))
		switch {
		case flags.exec:
			entry.execs++
		case flags.write:
			entry.writes++
		default:
			entry.reads++
		}

		if o.IsCrossDb {
			entry.hasCross = true
		}
		if db := strings.TrimSpace(dbParsed); db != "" {
			entry.dbSet[db] = struct{}{}
		}
		if upperDml != "" {
			entry.dmlSet[upperDml] = struct{}{}
		}

		if isPseudoObj {
			entry.isPseudo = true
			entry.pseudoKind = choosePseudoKind(entry.pseudoKind, pseudoKind)
			entry.hasPseudoLines = true
		}

		fn := strings.TrimSpace(o.Func)
		if fn != "" {
			entry.funcSet[fn] = struct{}{}
			entry.funcCounts[fn]++
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

		pseudoKind := entry.pseudoKind
		if entry.isPseudo && pseudoKind == "" {
			pseudoKind = "unknown"
		}

		roleSummary := summarizeRoleCounts(entry.reads, entry.writes, entry.execs)
		dmlKinds := setToSortedSlice(entry.dmlSet)

		result = append(result, ObjectSummaryRow{
			AppName:        entry.appName,
			RelPath:        entry.relPath,
			BaseName:       entry.baseName,
			FullObjectName: entry.fullName,
			Roles:          roleSummary,
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
		if a.FullObjectName != b.FullObjectName {
			return a.FullObjectName < b.FullObjectName
		}
		scoreA := objectSummaryScore(a)
		scoreB := objectSummaryScore(b)
		if scoreA != scoreB {
			return scoreA > scoreB
		}
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		return a.BaseName < b.BaseName
	})

	return result, nil
}

func BuildFormSummary(queries []QueryRow, objects []ObjectRow) ([]FormSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
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
		if distinctObjects == 0 {
			topObjects = ""
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
}

type roleFlags struct {
	read  bool
	write bool
	exec  bool
}

func (c *objectRoleCounter) Register(o ObjectRow, q QueryRow, hasQuery bool) {
	flags := roleFlagsForObject(o, q, hasQuery)

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
	isExec := role == "exec" || upperDml == "EXEC"
	isWrite := (!isExec && (o.IsWrite || isWriteDml(upperDml) || role == "target"))
	isRead := role == "source" || (!isExec && !isWrite && upperDml == "SELECT")

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
		return roleFlags{exec: true}
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
	if flags.exec {
		return roleFlags{exec: true}
	}
	if flags.write {
		return roleFlags{write: true}
	}
	if flags.read {
		return roleFlags{read: true}
	}
	return roleFlags{read: true}
}

func dynamicSignature(q QueryRow) string {
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
	if len(list) > 3 {
		list = list[:3]
	}
	parts := make([]string, 0, len(list))
	for _, item := range list {
		example := strings.TrimSpace(item.example)
		if example != "" {
			parts = append(parts, fmt.Sprintf("%s x%d (example=%s)", item.key, item.count, example))
			continue
		}
		parts = append(parts, fmt.Sprintf("%s x%d", item.key, item.count))
	}
	return strings.Join(parts, ";")
}

func buildTopObjectSummary(stats map[string]*objectRoleCounter, limit int) string {
	if len(stats) == 0 || limit <= 0 {
		return ""
	}
	type top struct {
		name   string
		score  int
		roles  *objectRoleCounter
		hasAny bool
	}
	tops := make([]top, 0, len(stats))
	for name, counter := range stats {
		score := objectDisplayScore(counter)
		if score == 0 && !(counter.HasExec || counter.HasWrite || counter.HasRead) {
			continue
		}
		tops = append(tops, top{name: name, score: score, roles: counter, hasAny: counter.HasExec || counter.HasWrite || counter.HasRead})
	}
	sort.Slice(tops, func(i, j int) bool {
		if tops[i].score != tops[j].score {
			return tops[i].score > tops[j].score
		}
		return strings.ToLower(tops[i].name) < strings.ToLower(tops[j].name)
	})
	if len(tops) > limit {
		tops = tops[:limit]
	}
	parts := make([]string, 0, len(tops))
	for _, t := range tops {
		roleLabel := describeRolesOrdered(t.roles)
		parts = append(parts, fmt.Sprintf("%s (%s)", t.name, roleLabel))
	}
	return strings.Join(parts, ", ")
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

func summarizeRoleCounts(reads, writes, execs int) string {
	return fmt.Sprintf("read=%d; write=%d; exec=%d", reads, writes, execs)
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
	if len(entries) > 5 {
		entries = entries[:5]
	}
	parts := make([]string, 0, len(entries))
	for _, e := range entries {
		parts = append(parts, fmt.Sprintf("%s (%s)", e.name, describeRolesOrdered(e.role)))
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
	header := []string{"AppName", "RelPath", "Func", "LineStart", "LineEnd", "TotalQueries", "TotalSelect", "TotalInsert", "TotalUpdate", "TotalDelete", "TotalTruncate", "TotalExec", "TotalWrite", "TotalDynamic", "DynamicSignatures", "TotalObjects", "ObjectsUsed", "HasCrossDb", "DbList"}
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
			r.DynamicSig,
			fmt.Sprintf("%d", r.TotalObjects),
			r.ObjectsUsed,
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

func WriteObjectSummary(path string, rows []ObjectSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "RelPath", "FullObjectName", "BaseName", "Roles", "DmlKinds", "TotalReads", "TotalWrites", "TotalExec", "TotalFuncs", "ExampleFuncs", "IsPseudoObject", "PseudoKind", "HasCrossDb", "DbList"}
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
	header := []string{"AppName", "RelPath", "File", "TotalFunctionsWithDB", "TotalQueries", "TotalObjects", "TotalExec", "TotalWrite", "TotalDynamic", "DistinctObjectsUsed", "HasDbAccess", "HasCrossDb", "DbList", "TopObjects"}
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
	res := make([]QueryRow, len(queries))
	for i, q := range queries {
		normalized := normalizeFuncName(q.Func)
		q.Func = resolveFuncName(normalized, q.RelPath, q.LineStart)
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
