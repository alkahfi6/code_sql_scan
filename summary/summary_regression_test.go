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
	Func         string
	RelPath      string
	TotalExec    int
	TotalWrite   int
	TotalDynamic int
	ObjectsUsed  string
}

type objSummaryRow struct {
	BaseName     string
	ExampleFuncs string
	Roles        string
	IsPseudo     bool
	PseudoKind   string
}

func TestDotnetSummaryQuality(t *testing.T) {
	funcPath, objPath := runSampleScan(t)
	funcRows := loadFunctionSummary(t, funcPath)
	objRows := loadObjectSummary(t, objPath)

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

	dyn := findDynamicObject(objRows)
	if dyn == nil {
		t.Fatalf("expected dynamic-sql pseudo object to exist")
	}
	if !dyn.IsPseudo || dyn.PseudoKind == "" {
		t.Fatalf("dynamic pseudo object flags not set properly: %+v", dyn)
	}
	if !strings.Contains(strings.ToLower(dyn.ExampleFuncs), "subpopulategridmain") {
		t.Fatalf("dynamic pseudo object should reference subPopulateGridMain, got %s", dyn.ExampleFuncs)
	}
}

func runSampleScan(t *testing.T) (string, string) {
	t.Helper()
	tdir := t.TempDir()
	funcPath := filepath.Join(tdir, "summary_function_dotnet.csv")
	objPath := filepath.Join(tdir, "summary_object_dotnet.csv")
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
	return funcPath, objPath
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
	required := []string{"Func", "RelPath", "TotalExec", "TotalWrite", "TotalDynamic", "ObjectsUsed"}
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
			Func:         rec[idx["Func"]],
			RelPath:      rec[idx["RelPath"]],
			TotalExec:    atoi(rec[idx["TotalExec"]]),
			TotalWrite:   atoi(rec[idx["TotalWrite"]]),
			TotalDynamic: atoi(rec[idx["TotalDynamic"]]),
			ObjectsUsed:  rec[idx["ObjectsUsed"]],
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
	required := []string{"BaseName", "ExampleFuncs", "Roles", "IsPseudoObject", "PseudoKind"}
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
			BaseName:     rec[idx["BaseName"]],
			ExampleFuncs: rec[idx["ExampleFuncs"]],
			Roles:        rec[idx["Roles"]],
			IsPseudo:     strings.EqualFold(rec[idx["IsPseudoObject"]], "true"),
			PseudoKind:   rec[idx["PseudoKind"]],
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

func findDynamicObject(rows []objSummaryRow) *objSummaryRow {
	for i := range rows {
		if strings.EqualFold(rows[i].BaseName, "<dynamic-sql>") {
			return &rows[i]
		}
	}
	return nil
}

func atoi(s string) int {
	v, _ := strconv.Atoi(strings.TrimSpace(s))
	return v
}
