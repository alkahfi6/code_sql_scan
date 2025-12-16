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
	expected := make(map[string]*functionAgg)
	for _, q := range queries {
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
	}

	for _, v := range expected {
		v.writeCount = v.insertCount + v.updateCount + v.deleteCount + v.truncateCount + v.execCount
	}

	summaryMap := make(map[string]FunctionSummaryRow)
	for _, s := range summaries {
		key := strings.Join([]string{s.AppName, s.RelPath, s.Func}, "|")
		summaryMap[key] = s
	}

	var mismatches []string
	for key, raw := range expected {
		summary, ok := summaryMap[key]
		rel, fn := splitKey(key)
		if !ok {
			mismatches = append(mismatches, fmt.Sprintf("function %s/%s missing in summary (raw select=%d insert=%d update=%d delete=%d truncate=%d exec=%d write=%d)", rel, fn, raw.selectCount, raw.insertCount, raw.updateCount, raw.deleteCount, raw.truncateCount, raw.execCount, raw.writeCount))
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
		mismatches = append(mismatches, fmt.Sprintf("function %s/%s present in summary only (summary select=%d insert=%d update=%d delete=%d truncate=%d exec=%d write=%d)", rel, fn, summary.TotalSelect, summary.TotalInsert, summary.TotalUpdate, summary.TotalDelete, summary.TotalTruncate, summary.TotalExec, summary.TotalWrite))
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

	return strings.Join(diffs, "; ")
}

func compareObjectSummary(queries []QueryRow, objects []ObjectRow, summaries []ObjectSummaryRow) []string {
	type agg struct {
		reads   int
		writes  int
		execs   int
		funcSet map[string]struct{}
	}

	queryByKey := make(map[string]QueryRow)
	for _, q := range queries {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	expected := make(map[string]*agg)
	for _, o := range objects {
		base := strings.TrimSpace(o.BaseName)
		key := strings.Join([]string{o.AppName, base}, "|")
		entry := expected[key]
		if entry == nil {
			entry = &agg{funcSet: make(map[string]struct{})}
			expected[key] = entry
		}
		upperDml := strings.ToUpper(strings.TrimSpace(o.DmlKind))
		isWrite := o.IsWrite || isWriteDml(upperDml)
		isRead := (!isWrite && upperDml == "SELECT") || strings.EqualFold(o.Role, "source")
		isExec := strings.EqualFold(o.Role, "exec") || upperDml == "EXEC"
		if isRead {
			entry.reads++
		}
		if isWrite && (upperDml == "INSERT" || upperDml == "UPDATE" || upperDml == "DELETE" || upperDml == "TRUNCATE") {
			entry.writes++
		}
		if isExec {
			entry.execs++
		}
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		if q, ok := queryByKey[qKey]; ok {
			fn := strings.TrimSpace(q.Func)
			if fn != "" {
				entry.funcSet[fn] = struct{}{}
			}
		}
	}

	summaryMap := make(map[string]ObjectSummaryRow)
	for _, s := range summaries {
		key := strings.Join([]string{s.AppName, strings.TrimSpace(s.BaseName)}, "|")
		summaryMap[key] = s
	}

	var mismatches []string
	for key, raw := range expected {
		summary, ok := summaryMap[key]
		base := strings.SplitN(key, "|", 2)
		baseName := ""
		if len(base) == 2 {
			baseName = base[1]
		}
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
		if col, ok := idx["TopObjects"]; ok {
			row.TopObjects = rec[col]
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
	required := []string{"AppName", "BaseName", "TotalReads", "TotalWrites", "TotalExec", "TotalFuncs"}
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
			AppName:     rec[idx["AppName"]],
			BaseName:    rec[idx["BaseName"]],
			TotalReads:  parseInt(rec[idx["TotalReads"]]),
			TotalWrites: parseInt(rec[idx["TotalWrites"]]),
			TotalExec:   parseInt(rec[idx["TotalExec"]]),
			TotalFuncs:  parseInt(rec[idx["TotalFuncs"]]),
		}
		if col, ok := idx["FullObjectName"]; ok {
			row.FullObjectName = rec[col]
		}
		if col, ok := idx["Roles"]; ok {
			row.Roles = rec[col]
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
