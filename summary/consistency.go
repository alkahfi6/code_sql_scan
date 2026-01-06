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
	dynamicRaw    int
	dynamicSql    int
	dynamicObject int
	objectReads   int
	objectWrites  int
	objectExecs   int
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

	funcMismatches := compareFunctionSummary(queries, objects, funcSummaries)
	objMismatches := compareObjectSummary(queries, objects, objSummaries)

	return &ConsistencyReport{FunctionMismatches: funcMismatches, ObjectMismatches: objMismatches}, nil
}

func compareFunctionSummary(queries []QueryRow, objects []ObjectRow, summaries []FunctionSummaryRow) []string {
	normQueries := normalizeQueryFuncs(queries)
	dedupQueries, _ := dedupeDynamicQueries(normQueries)
	objects = NormalizeObjectRows(objects)
	objectsByQuery := map[string][]ObjectRow{}
	for _, o := range objects {
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		objectsByQuery[qKey] = append(objectsByQuery[qKey], o)
	}

	expected := make(map[string]*functionAgg)
	objectCounters := make(map[string]map[string]*objectRoleUsage)
	for _, q := range normQueries {
		key := strings.Join([]string{q.AppName, q.RelPath, q.Func}, "|")
		entry := expected[key]
		if entry == nil {
			entry = &functionAgg{}
			expected[key] = entry
		}
		entry.totalQueries++

		baseKinds := make(map[string]struct{})
		if usage := strings.ToUpper(strings.TrimSpace(q.UsageKind)); usage != "" && usage != "UNKNOWN" {
			baseKinds[usage] = struct{}{}
		}
		if len(baseKinds) == 0 {
			qKey := queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)
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
		}

		writeKinds := 0
		for k := range baseKinds {
			switch k {
			case "INSERT", "UPDATE", "DELETE", "TRUNCATE", "EXEC":
				writeKinds++
			}
		}
		entry.writeCount += writeKinds

		if isDynamicQuery(q) {
			entry.dynamicCount++
			entry.dynamicRaw++

			qKey := queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)
			switch dynamicKindForQuery(q, objectsByQuery[qKey]) {
			case "dynamic-object":
				entry.dynamicObject++
			case "dynamic-sql":
				entry.dynamicSql++
			}
		}

	}

	for _, q := range dedupQueries {
		key := strings.Join([]string{q.AppName, q.RelPath, q.Func}, "|")
		funcObjects := objectCounters[key]
		if funcObjects == nil {
			funcObjects = make(map[string]*objectRoleUsage)
			objectCounters[key] = funcObjects
		}
		qKey := queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)
		for _, o := range objectsByQuery[qKey] {
			if shouldSkipObject(o) {
				continue
			}
			registerObjectRoleUsage(funcObjects, o)
		}
	}

	for key, funcObjects := range objectCounters {
		read, write, exec := countObjectsByRoleUsage(funcObjects)
		if entry := expected[key]; entry != nil {
			entry.objectReads = read
			entry.objectWrites = write
			entry.objectExecs = exec
		}
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
	rawCount := summary.DynamicRawCount
	if rawCount == 0 {
		rawCount = summary.DynamicCount
	}
	if rawCount != 0 {
		check("dynamicRaw", raw.dynamicRaw, rawCount)
	}
	if summary.TotalDynamicSql != 0 || raw.dynamicSql != 0 {
		check("dynamicSql", raw.dynamicSql, summary.TotalDynamicSql)
	}
	if summary.TotalDynamicObject != 0 || raw.dynamicObject != 0 {
		check("dynamicObject", raw.dynamicObject, summary.TotalDynamicObject)
	}
	if summary.DynamicSqlCount != 0 || raw.dynamicSql != 0 {
		check("dynamicSqlCount", raw.dynamicSql, summary.DynamicSqlCount)
	}
	if summary.DynamicObjectCount != 0 || raw.dynamicObject != 0 {
		check("dynamicObjectCount", raw.dynamicObject, summary.DynamicObjectCount)
	}
	if summary.TotalObjectsRead != 0 || raw.objectReads != 0 {
		check("objectsRead", raw.objectReads, summary.TotalObjectsRead)
	}
	if summary.TotalObjectsWrite != 0 || raw.objectWrites != 0 {
		check("objectsWrite", raw.objectWrites, summary.TotalObjectsWrite)
	}
	if summary.TotalObjectsExec != 0 || raw.objectExecs != 0 {
		check("objectsExec", raw.objectExecs, summary.TotalObjectsExec)
	}

	return strings.Join(diffs, "; ")
}

func compareObjectSummary(queries []QueryRow, objects []ObjectRow, summaries []ObjectSummaryRow) []string {
	objects = NormalizeObjectRows(objects)
	objectsByQuery := groupObjectsByQuery(objects)
	expected := aggregateObjectsForSummary(objects, buildDynamicKindIndex(queries, objectsByQuery))

	summaryMap := make(map[string]ObjectSummaryRow)
	for _, s := range summaries {
		key := ObjectSummaryRowKey(s)
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
		if raw.dynamicSql != summary.TotalDynamicSql {
			diffs = append(diffs, fmt.Sprintf("dynamic-sql raw=%d summary=%d", raw.dynamicSql, summary.TotalDynamicSql))
		}
		if raw.dynamicObject != summary.TotalDynamicObject {
			diffs = append(diffs, fmt.Sprintf("dynamic-object raw=%d summary=%d", raw.dynamicObject, summary.TotalDynamicObject))
		}
		if rawFuncCount != summary.TotalFuncs {
			diffs = append(diffs, fmt.Sprintf("funcs raw=%d summary=%d", rawFuncCount, summary.TotalFuncs))
		}
		expectedRoles := strings.Join(setToSortedSlice(raw.roleSet), ";")
		if normalizeRoles(summary.Roles) != expectedRoles {
			diffs = append(diffs, fmt.Sprintf("roles raw=%s summary=%s", expectedRoles, summary.Roles))
		}
		expectedDml := strings.Join(setToSortedSlice(raw.dmlSet), ";")
		if normalizeDmlKinds(summary.DmlKinds) != expectedDml {
			diffs = append(diffs, fmt.Sprintf("dml raw=%s summary=%s", expectedDml, summary.DmlKinds))
		}
		expectedPseudoKind := summarizePseudoKindSummary(raw.isPseudo, raw.pseudoKinds)
		if raw.isPseudo != summary.IsPseudoObject {
			diffs = append(diffs, fmt.Sprintf("pseudo raw=%t summary=%t", raw.isPseudo, summary.IsPseudoObject))
		} else if raw.isPseudo {
			if normalizePseudoKind(summary.PseudoKind) != expectedPseudoKind {
				diffs = append(diffs, fmt.Sprintf("pseudoKind raw=%s summary=%s", expectedPseudoKind, summary.PseudoKind))
			}
		}
		if raw.hasCross != summary.HasCrossDb {
			diffs = append(diffs, fmt.Sprintf("crossdb raw=%t summary=%t", raw.hasCross, summary.HasCrossDb))
		}
		expectedDbList := strings.Join(setToSortedSlice(raw.dbSet), ";")
		if normalizeDbList(summary.DbList) != expectedDbList {
			diffs = append(diffs, fmt.Sprintf("dbList raw=%s summary=%s", expectedDbList, summary.DbList))
		}
		expectedRoleSummary := summarizeRoleCounts(raw.reads, raw.writes, raw.execs)
		if strings.TrimSpace(summary.RolesSummary) != "" && strings.TrimSpace(summary.RolesSummary) != expectedRoleSummary {
			diffs = append(diffs, fmt.Sprintf("roleSummary raw=%s summary=%s", expectedRoleSummary, summary.RolesSummary))
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

func normalizeRoles(val string) string {
	set := make(map[string]struct{})
	for _, role := range strings.Split(val, ";") {
		trimmed := strings.TrimSpace(role)
		if idx := strings.IndexAny(trimmed, " ("); idx >= 0 {
			trimmed = strings.TrimSpace(trimmed[:idx])
		}
		if normalized := normalizeRoleValue(trimmed); normalized != "" {
			set[normalized] = struct{}{}
		}
	}
	return strings.Join(setToSortedSlice(set), ";")
}

func normalizeDmlKinds(val string) string {
	set := make(map[string]struct{})
	for _, part := range splitDmlKinds(val) {
		set[part] = struct{}{}
	}
	return strings.Join(setToSortedSlice(set), ";")
}

func normalizeDbList(val string) string {
	set := make(map[string]struct{})
	for _, part := range strings.Split(val, ";") {
		if trimmed := strings.TrimSpace(part); trimmed != "" {
			set[trimmed] = struct{}{}
		}
	}
	return strings.Join(setToSortedSlice(set), ";")
}

func summarizePseudoKindSummary(isPseudo bool, counts map[string]int) string {
	if !isPseudo {
		return ""
	}
	kind := summarizePseudoKindsFlat(counts)
	if strings.TrimSpace(kind) == "" {
		return "unknown"
	}
	return kind
}

func normalizePseudoKind(val string) string {
	if strings.TrimSpace(val) == "" {
		return "unknown"
	}
	return strings.ToLower(strings.TrimSpace(val))
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
		if col, ok := idx["TotalDynamicSql"]; ok {
			row.TotalDynamicSql = parseInt(rec[col])
		}
		if col, ok := idx["TotalDynamicObject"]; ok {
			row.TotalDynamicObject = parseInt(rec[col])
		}
		if col, ok := idx["DynamicSqlCount"]; ok {
			row.DynamicSqlCount = parseInt(rec[col])
		}
		if col, ok := idx["DynamicObjectCount"]; ok {
			row.DynamicObjectCount = parseInt(rec[col])
		}
		if col, ok := idx["DynamicRawCount"]; ok {
			row.DynamicRawCount = parseInt(rec[col])
		}
		if col, ok := idx["DynamicCount"]; ok {
			row.DynamicCount = parseInt(rec[col])
		}
		if col, ok := idx["DynamicSignatures"]; ok {
			row.DynamicSig = rec[col]
		}
		if col, ok := idx["DynamicReason"]; ok {
			row.DynamicReason = rec[col]
		}
		if col, ok := idx["TotalObjects"]; ok {
			row.TotalObjects = parseInt(rec[col])
		}
		if col, ok := idx["TotalObjectsRead"]; ok {
			row.TotalObjectsRead = parseInt(rec[col])
		}
		if col, ok := idx["TotalObjectsWrite"]; ok {
			row.TotalObjectsWrite = parseInt(rec[col])
		}
		if col, ok := idx["TotalObjectsExec"]; ok {
			row.TotalObjectsExec = parseInt(rec[col])
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
		if col, ok := idx["TopObjectsRead"]; ok {
			row.TopObjectsRead = rec[col]
		}
		if col, ok := idx["TopObjectsWrite"]; ok {
			row.TopObjectsWrite = rec[col]
		}
		if col, ok := idx["TopObjectsExec"]; ok {
			row.TopObjectsExec = rec[col]
		}
		if col, ok := idx["DynamicPseudoKinds"]; ok {
			row.DynamicPseudoKinds = rec[col]
		}
		if col, ok := idx["DynamicExampleSignatures"]; ok {
			row.DynamicExampleSignatures = rec[col]
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
	required := []string{"AppName", "RelPath", "FullObjectName", "BaseName", "TotalReads", "TotalWrites", "TotalDynamicSql", "TotalDynamicObject", "TotalExec", "TotalFuncs"}
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
			AppName:            rec[idx["AppName"]],
			RelPath:            rec[idx["RelPath"]],
			FullObjectName:     rec[idx["FullObjectName"]],
			BaseName:           rec[idx["BaseName"]],
			TotalReads:         parseInt(rec[idx["TotalReads"]]),
			TotalWrites:        parseInt(rec[idx["TotalWrites"]]),
			TotalDynamicSql:    parseInt(rec[idx["TotalDynamicSql"]]),
			TotalDynamicObject: parseInt(rec[idx["TotalDynamicObject"]]),
			TotalExec:          parseInt(rec[idx["TotalExec"]]),
			TotalFuncs:         parseInt(rec[idx["TotalFuncs"]]),
		}
		if col, ok := idx["Roles"]; ok {
			row.Roles = rec[col]
		}
		if col, ok := idx["RolesSummary"]; ok {
			row.RolesSummary = rec[col]
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
