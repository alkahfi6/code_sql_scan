package summary

import (
	"encoding/csv"
	"fmt"
	"io"
	"os"
	"sort"
	"strings"
)

// ConsistencyReport captures mismatches between raw usage and summaries.
type ConsistencyReport struct {
	FunctionMismatches []string
	ObjectMismatches   []string
}

type functionAgg struct {
	totalQueries  int
	selectCount   int
	insertCount   int
	updateCount   int
	deleteCount   int
	truncateCount int
	execCount     int
	writeCount    int
	dynamicCount  int
}

// TotalMismatches returns the total number of mismatches found.
func (r *ConsistencyReport) TotalMismatches() int {
	if r == nil {
		return 0
	}
	return len(r.FunctionMismatches) + len(r.ObjectMismatches)
}

// Examples returns up to n mismatch messages.
func (r *ConsistencyReport) Examples(n int) []string {
	if r == nil || n <= 0 {
		return nil
	}
	combined := append([]string{}, r.FunctionMismatches...)
	combined = append(combined, r.ObjectMismatches...)
	if len(combined) > n {
		combined = combined[:n]
	}
	return combined
}

// VerifyConsistency checks whether summary files align with raw usage files.
func VerifyConsistency(queryPath, objectPath, funcSummaryPath, objSummaryPath string) (*ConsistencyReport, error) {
	if funcSummaryPath == "" || objSummaryPath == "" {
		return nil, nil
	}

	queries, err := LoadQueryUsage(queryPath)
	if err != nil {
		return nil, fmt.Errorf("load query usage: %w", err)
	}
	objects, err := LoadObjectUsage(objectPath)
	if err != nil {
		return nil, fmt.Errorf("load object usage: %w", err)
	}
	funcSummaries, err := LoadFunctionSummary(funcSummaryPath)
	if err != nil {
		return nil, fmt.Errorf("load function summary: %w", err)
	}
	objSummaries, err := LoadObjectSummary(objSummaryPath)
	if err != nil {
		return nil, fmt.Errorf("load object summary: %w", err)
	}

	funcMismatches := compareFunctionSummary(queries, funcSummaries)
	objMismatches := compareObjectSummary(queries, objects, objSummaries)

	return &ConsistencyReport{FunctionMismatches: funcMismatches, ObjectMismatches: objMismatches}, nil
}

func compareFunctionSummary(queries []QueryRow, summaries []FunctionSummaryRow) []string {
	normQueries := normalizeQueryFuncs(queries)
	expected := make(map[string]*functionAgg)
	for _, q := range normQueries {
		key := strings.Join([]string{q.AppName, q.RelPath, q.Func}, "|")
		entry := expected[key]
		if entry == nil {
			entry = &functionAgg{}
			expected[key] = entry
		}
		entry.totalQueries++
		switch strings.ToUpper(strings.TrimSpace(q.UsageKind)) {
		case "SELECT":
			entry.selectCount++
		case "INSERT":
			entry.insertCount++
		case "UPDATE":
			entry.updateCount++
		case "DELETE":
			entry.deleteCount++
		case "TRUNCATE":
			entry.truncateCount++
		case "EXEC":
			entry.execCount++
		}
		if isDynamicQuery(q) {
			entry.dynamicCount++
		}
	}

	for _, v := range expected {
		v.writeCount = v.insertCount + v.updateCount + v.deleteCount + v.truncateCount + v.execCount
	}

	summaryMap := make(map[string]FunctionSummaryRow)
	for _, s := range summaries {
		fn := resolveFuncName(s.Func, s.RelPath, s.LineStart)
		key := strings.Join([]string{s.AppName, s.RelPath, fn}, "|")
		summaryMap[key] = s
	}

	var mismatches []string
	for key, raw := range expected {
		summary, ok := summaryMap[key]
		rel, fn := splitKey(key)
		if !ok {
			mismatches = append(mismatches, fmt.Sprintf("function %s/%s missing in summary (raw select=%d insert=%d update=%d delete=%d truncate=%d exec=%d write=%d dynamic=%d)", rel, fn, raw.selectCount, raw.insertCount, raw.updateCount, raw.deleteCount, raw.truncateCount, raw.execCount, raw.writeCount, raw.dynamicCount))
			continue
		}

		diff := compareFunctionCounts(raw, summary)
		if diff != "" {
			mismatches = append(mismatches, fmt.Sprintf("function %s/%s mismatch: %s", rel, fn, diff))
		}
	}

	for key, summary := range summaryMap {
		if _, ok := expected[key]; ok {
			continue
		}
		rel, fn := splitKey(key)
		mismatches = append(mismatches, fmt.Sprintf("function %s/%s present in summary only (summary select=%d insert=%d update=%d delete=%d truncate=%d exec=%d write=%d dynamic=%d)", rel, fn, summary.TotalSelect, summary.TotalInsert, summary.TotalUpdate, summary.TotalDelete, summary.TotalTruncate, summary.TotalExec, summary.TotalWrite, summary.TotalDynamic))
	}

	sort.Strings(mismatches)
	return mismatches
}

func splitKey(key string) (string, string) {
	parts := strings.SplitN(key, "|", 3)
	for len(parts) < 3 {
		parts = append(parts, "")
	}
	return parts[1], parts[2]
}

func compareFunctionCounts(raw *functionAgg, summary FunctionSummaryRow) string {
	diffs := []string{}
	check := func(label string, rawVal, sumVal int) {
		if rawVal != sumVal {
			diffs = append(diffs, fmt.Sprintf("%s raw=%d summary=%d", label, rawVal, sumVal))
		}
	}

	check("total", raw.totalQueries, summary.TotalQueries)
	check("select", raw.selectCount, summary.TotalSelect)
	check("insert", raw.insertCount, summary.TotalInsert)
	check("update", raw.updateCount, summary.TotalUpdate)
	check("delete", raw.deleteCount, summary.TotalDelete)
	check("truncate", raw.truncateCount, summary.TotalTruncate)
	check("exec", raw.execCount, summary.TotalExec)
	check("write", raw.writeCount, summary.TotalWrite)
	check("dynamic", raw.dynamicCount, summary.TotalDynamic)

	return strings.Join(diffs, "; ")
}

func compareObjectSummary(queries []QueryRow, objects []ObjectRow, summaries []ObjectSummaryRow) []string {
	type agg struct {
		reads      int
		writes     int
		execs      int
		funcSet    map[string]struct{}
		isPseudo   bool
		pseudoKind string
		hasCross   bool
		dbSet      map[string]struct{}
		dmlSet     map[string]struct{}
		baseName   string
		fullName   string
	}

	queryByKey := make(map[string]QueryRow)
	for _, q := range queries {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	expected := make(map[string]*agg)
	for _, o := range objects {
		if shouldSkipObject(o) {
			continue
		}
		fullName := chooseFullObjectName(o)
		if strings.TrimSpace(fullName) == "" {
			continue
		}
		dbParsed, schemaParsed, parsedBase := splitFullObjectName(fullName)
		base := strings.TrimSpace(o.BaseName)
		if base == "" {
			base = parsedBase
		}
		pseudoKind := defaultPseudoKind(o.PseudoKind)
		isPseudoObj := o.IsPseudoObject
		if detected, kind := pseudoObjectInfo(base, pseudoKind); detected {
			isPseudoObj = true
			pseudoKind = defaultPseudoKind(choosePseudoKind(pseudoKind, defaultPseudoKind(kind)))
		}
		key := objectSummaryKeyDetailed(
			o.AppName,
			o.RelPath,
			fullName,
			base,
			"",
			"",
			isPseudoObj,
			pseudoKind,
			dbParsed,
			schemaParsed,
		)
		entry := expected[key]
		if entry == nil {
			entry = &agg{
				funcSet:  make(map[string]struct{}),
				dbSet:    make(map[string]struct{}),
				dmlSet:   make(map[string]struct{}),
				baseName: base,
				fullName: fullName,
			}
			expected[key] = entry
		}
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		qRow, hasQuery := queryByKey[qKey]
		flags := roleFlagsForObject(o, qRow, hasQuery)
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
		if db := strings.TrimSpace(o.DbName); db != "" {
			entry.dbSet[db] = struct{}{}
		}
		if upperDml := strings.ToUpper(strings.TrimSpace(o.DmlKind)); upperDml != "" {
			entry.dmlSet[upperDml] = struct{}{}
		}

		if isPseudoObj {
			entry.isPseudo = true
			entry.pseudoKind = choosePseudoKind(entry.pseudoKind, pseudoKind)
		}
		if fn := strings.TrimSpace(o.Func); fn != "" {
			entry.funcSet[fn] = struct{}{}
		}
	}

	summaryMap := make(map[string]ObjectSummaryRow)
	for _, s := range summaries {
		dbParsed, schemaParsed, parsedBase := splitFullObjectName(strings.TrimSpace(s.FullObjectName))
		baseName := strings.TrimSpace(s.BaseName)
		if baseName == "" {
			baseName = parsedBase
		}
		key := objectSummaryKeyDetailed(
			s.AppName,
			s.RelPath,
			strings.TrimSpace(s.FullObjectName),
			baseName,
			"",
			"",
			s.IsPseudoObject,
			defaultPseudoKind(s.PseudoKind),
			dbParsed,
			schemaParsed,
		)
		summaryMap[key] = s
	}

	var mismatches []string
	for key, raw := range expected {
		summary, ok := summaryMap[key]
		baseName := raw.baseName
		if !ok {
			mismatches = append(mismatches, fmt.Sprintf("object %s missing in summary (raw read=%d write=%d exec=%d funcs=%d)", baseName, raw.reads, raw.writes, raw.execs, len(raw.funcSet)))
			continue
		}
		rawFuncCount := len(raw.funcSet)
		diffs := []string{}
		if raw.reads != summary.TotalReads {
			diffs = append(diffs, fmt.Sprintf("read raw=%d summary=%d", raw.reads, summary.TotalReads))
		}
		if raw.writes != summary.TotalWrites {
			diffs = append(diffs, fmt.Sprintf("write raw=%d summary=%d", raw.writes, summary.TotalWrites))
		}
		if raw.execs != summary.TotalExec {
			diffs = append(diffs, fmt.Sprintf("exec raw=%d summary=%d", raw.execs, summary.TotalExec))
		}
		if rawFuncCount != summary.TotalFuncs {
			diffs = append(diffs, fmt.Sprintf("funcs raw=%d summary=%d", rawFuncCount, summary.TotalFuncs))
		}
		expectedRoles := summarizeRoleCounts(raw.reads, raw.writes, raw.execs)
		if strings.TrimSpace(summary.Roles) != expectedRoles {
			diffs = append(diffs, fmt.Sprintf("roles raw=%s summary=%s", expectedRoles, summary.Roles))
		}
		expectedDml := strings.Join(setToSortedSlice(raw.dmlSet), ";")
		if strings.TrimSpace(summary.DmlKinds) != expectedDml {
			diffs = append(diffs, fmt.Sprintf("dml raw=%s summary=%s", expectedDml, summary.DmlKinds))
		}
		expectedPseudoKind := strings.TrimSpace(defaultPseudoKind(raw.pseudoKind))
		if raw.isPseudo != summary.IsPseudoObject {
			diffs = append(diffs, fmt.Sprintf("pseudo raw=%t summary=%t", raw.isPseudo, summary.IsPseudoObject))
		} else if raw.isPseudo {
			if strings.TrimSpace(summary.PseudoKind) == "" {
				summary.PseudoKind = "unknown"
			}
			if expectedPseudoKind == "" {
				expectedPseudoKind = "unknown"
			}
			if !strings.EqualFold(expectedPseudoKind, strings.TrimSpace(summary.PseudoKind)) {
				diffs = append(diffs, fmt.Sprintf("pseudoKind raw=%s summary=%s", expectedPseudoKind, summary.PseudoKind))
			}
		}
		if raw.hasCross != summary.HasCrossDb {
			diffs = append(diffs, fmt.Sprintf("crossdb raw=%t summary=%t", raw.hasCross, summary.HasCrossDb))
		}
		expectedDbList := strings.Join(setToSortedSlice(raw.dbSet), ";")
		if strings.TrimSpace(summary.DbList) != expectedDbList {
			diffs = append(diffs, fmt.Sprintf("dbList raw=%s summary=%s", expectedDbList, summary.DbList))
		}
		if len(diffs) > 0 {
			mismatches = append(mismatches, fmt.Sprintf("object %s mismatch: %s", baseName, strings.Join(diffs, "; ")))
		}
	}

	for key, summary := range summaryMap {
		if _, ok := expected[key]; ok {
			continue
		}
		base := strings.TrimSpace(summary.BaseName)
		mismatches = append(mismatches, fmt.Sprintf("object %s present in summary only (summary read=%d write=%d exec=%d funcs=%d)", base, summary.TotalReads, summary.TotalWrites, summary.TotalExec, summary.TotalFuncs))
	}

	sort.Strings(mismatches)
	return mismatches
}

// LoadFunctionSummary reads a function summary CSV.
func LoadFunctionSummary(path string) ([]FunctionSummaryRow, error) {
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
	required := []string{"AppName", "RelPath", "Func", "TotalQueries", "TotalSelect", "TotalInsert", "TotalUpdate", "TotalDelete", "TotalTruncate", "TotalExec", "TotalWrite"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			return nil, fmt.Errorf("missing column %s in function summary", req)
		}
	}

	var rows []FunctionSummaryRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			return nil, err
		}
		row := FunctionSummaryRow{
			AppName:       rec[idx["AppName"]],
			RelPath:       rec[idx["RelPath"]],
			Func:          rec[idx["Func"]],
			TotalQueries:  parseInt(rec[idx["TotalQueries"]]),
			TotalSelect:   parseInt(rec[idx["TotalSelect"]]),
			TotalInsert:   parseInt(rec[idx["TotalInsert"]]),
			TotalUpdate:   parseInt(rec[idx["TotalUpdate"]]),
			TotalDelete:   parseInt(rec[idx["TotalDelete"]]),
			TotalTruncate: parseInt(rec[idx["TotalTruncate"]]),
			TotalExec:     parseInt(rec[idx["TotalExec"]]),
			TotalWrite:    parseInt(rec[idx["TotalWrite"]]),
		}
		if col, ok := idx["LineStart"]; ok {
			row.LineStart = parseInt(rec[col])
		}
		if col, ok := idx["LineEnd"]; ok {
			row.LineEnd = parseInt(rec[col])
		}
		if col, ok := idx["TotalDynamic"]; ok {
			row.TotalDynamic = parseInt(rec[col])
		}
		if col, ok := idx["DynamicSignatures"]; ok {
			row.DynamicSig = rec[col]
		}
		if col, ok := idx["TotalObjects"]; ok {
			row.TotalObjects = parseInt(rec[col])
		}
		if col, ok := idx["ObjectsUsed"]; ok {
			row.ObjectsUsed = rec[col]
		} else if col, ok := idx["TopObjects"]; ok {
			row.ObjectsUsed = rec[col]
		}
		if col, ok := idx["HasCrossDb"]; ok {
			row.HasCrossDb = parseBool(rec[col])
		}
		if col, ok := idx["DbList"]; ok {
			row.DbList = rec[col]
		}
		rows = append(rows, row)
	}
	return rows, nil
}

// LoadObjectSummary reads an object summary CSV.
func LoadObjectSummary(path string) ([]ObjectSummaryRow, error) {
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
	required := []string{"AppName", "RelPath", "FullObjectName", "BaseName", "TotalReads", "TotalWrites", "TotalExec", "TotalFuncs"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			return nil, fmt.Errorf("missing column %s in object summary", req)
		}
	}

	var rows []ObjectSummaryRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			return nil, err
		}
		row := ObjectSummaryRow{
			AppName:        rec[idx["AppName"]],
			RelPath:        rec[idx["RelPath"]],
			FullObjectName: rec[idx["FullObjectName"]],
			BaseName:       rec[idx["BaseName"]],
			TotalReads:     parseInt(rec[idx["TotalReads"]]),
			TotalWrites:    parseInt(rec[idx["TotalWrites"]]),
			TotalExec:      parseInt(rec[idx["TotalExec"]]),
			TotalFuncs:     parseInt(rec[idx["TotalFuncs"]]),
		}
		if col, ok := idx["Roles"]; ok {
			row.Roles = rec[col]
		}
		if col, ok := idx["DmlKinds"]; ok {
			row.DmlKinds = rec[col]
		}
		if col, ok := idx["ExampleFuncs"]; ok {
			row.ExampleFuncs = rec[col]
		}
		if col, ok := idx["IsPseudoObject"]; ok {
			row.IsPseudoObject = parseBool(rec[col])
		}
		if col, ok := idx["PseudoKind"]; ok {
			row.PseudoKind = rec[col]
		}
		if col, ok := idx["HasCrossDb"]; ok {
			row.HasCrossDb = parseBool(rec[col])
		}
		if col, ok := idx["DbList"]; ok {
			row.DbList = rec[col]
		}
		rows = append(rows, row)
	}
	return rows, nil
}
