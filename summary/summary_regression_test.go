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
	BaseName        string
	UsedInFuncs     string
	Roles           string
	IsDynamicObject bool
	DynamicKind     string
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
	if !dyn.IsDynamicObject || dyn.DynamicKind == "" {
		t.Fatalf("dynamic pseudo object flags not set properly: %+v", dyn)
	}
	if !strings.Contains(strings.ToLower(dyn.UsedInFuncs), "subpopulategridmain") {
		t.Fatalf("dynamic pseudo object should reference subPopulateGridMain, got %s", dyn.UsedInFuncs)
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
	if _, err := r.Read(); err != nil {
		t.Fatalf("read header: %v", err)
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
			Func:         rec[3],
			RelPath:      rec[1],
			TotalExec:    atoi(rec[5]),
			TotalWrite:   atoi(rec[12]),
			TotalDynamic: atoi(rec[11]),
			ObjectsUsed:  rec[14],
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
	if _, err := r.Read(); err != nil {
		t.Fatalf("read header: %v", err)
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
			BaseName:        rec[6],
			UsedInFuncs:     rec[7],
			Roles:           rec[9],
			IsDynamicObject: strings.EqualFold(rec[13], "true"),
			DynamicKind:     rec[14],
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
