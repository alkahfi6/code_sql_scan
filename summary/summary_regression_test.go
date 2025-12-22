package summary

import (
	"encoding/csv"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"testing"
)

type funcSummaryRow struct {
	Func             string
	RelPath          string
	TotalExec        int
	TotalWrite       int
	TotalDynamic     int
	ObjectsUsed      string
	DynamicSignature string
}

type objSummaryRow struct {
	BaseName       string
	RelPath        string
	FullObjectName string
	ExampleFuncs   string
	Roles          string
	RolesSummary   string
	IsPseudo       bool
	PseudoKind     string
	TotalReads     int
	TotalWrites    int
	TotalExec      int
}

type samplePaths struct {
	funcSummary string
	objSummary  string
	queryPath   string
	objectPath  string
	formSummary string
}

func TestDotnetSummaryQuality(t *testing.T) {
	paths := runSampleScan(t)
	funcRows := loadFunctionSummary(t, paths.funcSummary)
	objRows := loadObjectSummary(t, paths.objSummary)

	banned := map[string]struct{}{
		"exception": {}, "convert": {}, "cast": {}, "in": {}, "isnull": {}, "len": {}, "openxml": {}, "varchar": {},
	}
	for _, r := range funcRows {
		if _, ok := banned[strings.ToLower(r.Func)]; ok {
			t.Fatalf("found banned func name in summary: %s", r.Func)
		}
	}

	for _, fn := range []string{"subAcceptReject", "subPopulateGridMain"} {
		if !funcExists(funcRows, fn) {
			t.Fatalf("expected function %s to appear in summary", fn)
		}
	}

	for _, r := range funcRows {
		if (r.TotalExec > 0 || r.TotalWrite > 0) && r.ObjectsUsed == "" {
			t.Fatalf("function %s expected to have objects used for write/exec", r.Func)
		}
	}

	if row := findFunc(funcRows, "subAcceptReject"); row != nil {
		if !strings.Contains(strings.ToLower(row.ObjectsUsed), "trs_updatestatussecuritytransactionaftermurex") {
			t.Fatalf("subAcceptReject missing expected target object, got %s", row.ObjectsUsed)
		}
	}

	dyn := findDynamicObjectWithExample(objRows, "subpopulategridmain")
	if dyn == nil {
		t.Fatalf("expected dynamic-sql pseudo object to exist with subPopulateGridMain example")
	}
	if !dyn.IsPseudo || dyn.PseudoKind == "" {
		t.Fatalf("dynamic pseudo object flags not set properly: %+v", dyn)
	}
}

func TestObjectSummaryCountsMatchRaw(t *testing.T) {
	paths := runSampleScan(t)
	objSummary := loadObjectSummary(t, paths.objSummary)
	queries := loadRawQueries(t, paths.queryPath)
	objects := loadRawObjects(t, paths.objectPath)

	queryByKey := make(map[string]QueryRow)
	for _, q := range queries {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	for _, sum := range objSummary {
		reads, writes, execs := 0, 0, 0
		for _, obj := range objects {
			if !strings.EqualFold(strings.TrimSpace(obj.RelPath), strings.TrimSpace(sum.RelPath)) {
				continue
			}
			if !strings.EqualFold(chooseFullObjectName(obj), sum.FullObjectName) {
				continue
			}
			key := queryObjectKey(obj.AppName, obj.RelPath, obj.File, obj.QueryHash)
			qRow, hasQuery := queryByKey[key]
			flags := roleFlagsForObject(obj, qRow, hasQuery)
			switch {
			case flags.exec:
				execs++
			case flags.write:
				writes++
			default:
				reads++
			}
		}

		if reads != sum.TotalReads || writes != sum.TotalWrites || execs != sum.TotalExec {
			t.Fatalf("summary totals mismatch for %s: got r/w/e=%d/%d/%d want %d/%d/%d", sum.FullObjectName, sum.TotalReads, sum.TotalWrites, sum.TotalExec, reads, writes, execs)
		}
		if sum.IsPseudo && strings.TrimSpace(sum.PseudoKind) == "" {
			t.Fatalf("pseudo object missing kind for %s", sum.FullObjectName)
		}
	}
}

func runSampleScan(t *testing.T) samplePaths {
	t.Helper()
	tdir := t.TempDir()
	funcPath := filepath.Join(tdir, "summary_function_dotnet.csv")
	objPath := filepath.Join(tdir, "summary_object_dotnet.csv")
	queryPath := filepath.Join(tdir, "out_query_dotnet.csv")
	objectPath := filepath.Join(tdir, "out_object_dotnet.csv")
	cmd := exec.Command("go", "run", "./", "-root", "./dotnet_check", "-app", "dotnet-sample", "-lang", "dotnet", "-out-query", filepath.Join(tdir, "out_query_dotnet.csv"), "-out-object", filepath.Join(tdir, "out_object_dotnet.csv"), "-out-summary-func", funcPath, "-out-summary-object", objPath, "-out-summary-form", filepath.Join(tdir, "summary_form_dotnet.csv"))
	cmd.Env = append(os.Environ(), "GO111MODULE=on")
	if cwd, err := os.Getwd(); err == nil {
		if strings.HasSuffix(cwd, string(filepath.Separator)+"summary") {
			cmd.Dir = filepath.Dir(cwd)
		} else {
			cmd.Dir = cwd
		}
	}
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	if err := cmd.Run(); err != nil {
		t.Fatalf("failed to run dotnet scan: %v", err)
	}
	return samplePaths{
		funcSummary: funcPath,
		objSummary:  objPath,
		queryPath:   queryPath,
		objectPath:  objectPath,
		formSummary: filepath.Join(tdir, "summary_form_dotnet.csv"),
	}
}

func loadFunctionSummary(t *testing.T, path string) []funcSummaryRow {
	t.Helper()
	f, err := os.Open(path)
	if err != nil {
		t.Fatalf("open function summary: %v", err)
	}
	defer f.Close()
	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		t.Fatalf("read header: %v", err)
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"Func", "RelPath", "TotalExec", "TotalWrite", "TotalDynamic", "ObjectsUsed", "DynamicSignatures"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			t.Fatalf("function summary missing column %s", req)
		}
	}
	var rows []funcSummaryRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			t.Fatalf("read record: %v", err)
		}
		rows = append(rows, funcSummaryRow{
			Func:             rec[idx["Func"]],
			RelPath:          rec[idx["RelPath"]],
			TotalExec:        atoi(rec[idx["TotalExec"]]),
			TotalWrite:       atoi(rec[idx["TotalWrite"]]),
			TotalDynamic:     atoi(rec[idx["TotalDynamic"]]),
			ObjectsUsed:      rec[idx["ObjectsUsed"]],
			DynamicSignature: rec[idx["DynamicSignatures"]],
		})
	}
	return rows
}

func loadObjectSummary(t *testing.T, path string) []objSummaryRow {
	t.Helper()
	f, err := os.Open(path)
	if err != nil {
		t.Fatalf("open object summary: %v", err)
	}
	defer f.Close()
	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		t.Fatalf("read header: %v", err)
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"BaseName", "RelPath", "ExampleFuncs", "Roles", "RolesSummary", "IsPseudoObject", "PseudoKind", "TotalReads", "TotalWrites", "TotalExec", "FullObjectName"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			t.Fatalf("object summary missing column %s", req)
		}
	}
	var rows []objSummaryRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			t.Fatalf("read record: %v", err)
		}
		rows = append(rows, objSummaryRow{
			BaseName:       rec[idx["BaseName"]],
			RelPath:        rec[idx["RelPath"]],
			FullObjectName: rec[idx["FullObjectName"]],
			ExampleFuncs:   rec[idx["ExampleFuncs"]],
			Roles:          rec[idx["Roles"]],
			RolesSummary:   rec[idx["RolesSummary"]],
			IsPseudo:       strings.EqualFold(rec[idx["IsPseudoObject"]], "true"),
			PseudoKind:     rec[idx["PseudoKind"]],
			TotalReads:     atoi(rec[idx["TotalReads"]]),
			TotalWrites:    atoi(rec[idx["TotalWrites"]]),
			TotalExec:      atoi(rec[idx["TotalExec"]]),
		})
	}
	return rows
}

func funcExists(rows []funcSummaryRow, name string) bool {
	for _, r := range rows {
		if r.Func == name {
			return true
		}
	}
	return false
}

func findFunc(rows []funcSummaryRow, name string) *funcSummaryRow {
	for i := range rows {
		if rows[i].Func == name {
			return &rows[i]
		}
	}
	return nil
}

func findDynamicObjectWithExample(rows []objSummaryRow, needle string) *objSummaryRow {
	lowerNeedle := strings.ToLower(strings.TrimSpace(needle))
	for i := range rows {
		if !strings.EqualFold(rows[i].BaseName, "<dynamic-sql>") {
			continue
		}
		if strings.Contains(strings.ToLower(rows[i].ExampleFuncs), lowerNeedle) {
			return &rows[i]
		}
	}
	return nil
}

func loadRawQueries(t *testing.T, path string) []QueryRow {
	t.Helper()
	f, err := os.Open(path)
	if err != nil {
		t.Fatalf("open query csv: %v", err)
	}
	defer f.Close()
	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		t.Fatalf("read query header: %v", err)
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"AppName", "RelPath", "File", "Func", "UsageKind", "IsWrite", "IsDynamic", "HasCrossDb", "DbList", "ConnDb", "QueryHash", "LineStart", "LineEnd", "CallSiteKind", "DynamicSignature"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			t.Fatalf("query csv missing column %s", req)
		}
	}
	var rows []QueryRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			t.Fatalf("read query csv: %v", err)
		}
		rows = append(rows, QueryRow{
			AppName:    rec[idx["AppName"]],
			RelPath:    rec[idx["RelPath"]],
			File:       rec[idx["File"]],
			Func:       rec[idx["Func"]],
			UsageKind:  rec[idx["UsageKind"]],
			IsWrite:    strings.EqualFold(rec[idx["IsWrite"]], "true"),
			IsDynamic:  strings.EqualFold(rec[idx["IsDynamic"]], "true"),
			HasCrossDb: strings.EqualFold(rec[idx["HasCrossDb"]], "true"),
			DbList:     strings.Split(strings.TrimSpace(rec[idx["DbList"]]), ";"),
			ConnDb:     rec[idx["ConnDb"]],
			QueryHash:  rec[idx["QueryHash"]],
			LineStart:  atoi(rec[idx["LineStart"]]),
			LineEnd:    atoi(rec[idx["LineEnd"]]),
			CallSite:   rec[idx["CallSiteKind"]],
			DynamicSig: rec[idx["DynamicSignature"]],
		})
	}
	return rows
}

func loadRawObjects(t *testing.T, path string) []ObjectRow {
	t.Helper()
	f, err := os.Open(path)
	if err != nil {
		t.Fatalf("open object csv: %v", err)
	}
	defer f.Close()
	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		t.Fatalf("read object header: %v", err)
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"AppName", "RelPath", "File", "Func", "QueryHash", "FullObjectName", "DbName", "SchemaName", "BaseName", "IsCrossDb", "Role", "DmlKind", "IsWrite", "IsObjectNameDynamic", "IsPseudoObject", "PseudoKind"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			t.Fatalf("object csv missing column %s", req)
		}
	}
	var rows []ObjectRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			t.Fatalf("read object csv: %v", err)
		}
		rows = append(rows, ObjectRow{
			AppName:         rec[idx["AppName"]],
			RelPath:         rec[idx["RelPath"]],
			File:            rec[idx["File"]],
			Func:            rec[idx["Func"]],
			QueryHash:       rec[idx["QueryHash"]],
			ObjectName:      rec[idx["FullObjectName"]],
			DbName:          rec[idx["DbName"]],
			SchemaName:      rec[idx["SchemaName"]],
			BaseName:        rec[idx["BaseName"]],
			IsCrossDb:       strings.EqualFold(rec[idx["IsCrossDb"]], "true"),
			Role:            rec[idx["Role"]],
			DmlKind:         rec[idx["DmlKind"]],
			IsWrite:         strings.EqualFold(rec[idx["IsWrite"]], "true"),
			IsObjectNameDyn: strings.EqualFold(rec[idx["IsObjectNameDynamic"]], "true"),
			IsPseudoObject:  strings.EqualFold(rec[idx["IsPseudoObject"]], "true"),
			PseudoKind:      rec[idx["PseudoKind"]],
		})
	}
	return rows
}

func atoi(s string) int {
	v, _ := strconv.Atoi(strings.TrimSpace(s))
	return v
}
