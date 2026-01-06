package scan

import (
	"path/filepath"
	"strings"
	"testing"

	"code_sql_scan/summary"
)

func TestDotnetExecProcFixtureOutputs(t *testing.T) {
	outDir := t.TempDir()
	root := filepath.Clean(filepath.Join("..", "..", "dotnet_check"))

	cfg := &Config{
		Root:             root,
		AppName:          "dotnet-sample",
		Lang:             "dotnet",
		OutDir:           outDir,
		OutQuery:         filepath.Join(outDir, "dotnet-sample-query.csv"),
		OutObject:        filepath.Join(outDir, "dotnet-sample-object.csv"),
		OutSummaryFunc:   filepath.Join(outDir, "dotnet-sample-summary-function.csv"),
		OutSummaryObject: filepath.Join(outDir, "dotnet-sample-summary-object.csv"),
		OutSummaryForm:   filepath.Join(outDir, "dotnet-sample-summary-form.csv"),
		MaxFileSize:      2 * 1024 * 1024,
		Workers:          4,
	}

	if _, err := Run(cfg); err != nil {
		t.Fatalf("run scan: %v", err)
	}

	queries, err := summary.LoadQueryUsage(cfg.OutQuery)
	if err != nil {
		t.Fatalf("load queries: %v", err)
	}
	objects, err := summary.LoadObjectUsage(cfg.OutObject)
	if err != nil {
		t.Fatalf("load objects: %v", err)
	}

	var fixtureQueries []summary.QueryRow
	for _, q := range queries {
		if strings.HasSuffix(q.RelPath, "ExecProcConst.cs") {
			fixtureQueries = append(fixtureQueries, q)
			if q.IsDynamic {
				t.Fatalf("expected ExecProc fixture queries to be static, got dynamic row: %+v", q)
			}
		}
	}
	if len(fixtureQueries) < 5 {
		t.Fatalf("expected fixture queries to be captured, got %d", len(fixtureQueries))
	}

	expectedProcs := map[string]struct{}{
		"ConstDirect":        {},
		"ConstTernA":         {},
		"ConstTernB":         {},
		"ConstSwitchX":       {},
		"ConstSwitchY":       {},
		"ConstSwitchDefault": {},
		"ConstInterp":        {},
		"ConstFormat":        {},
	}

	found := make(map[string]struct{})
	for _, o := range objects {
		if !strings.HasSuffix(o.RelPath, "ExecProcConst.cs") {
			continue
		}
		if strings.ToLower(o.Role) != "exec" {
			continue
		}
		if o.IsPseudoObject || o.IsObjectNameDyn {
			t.Fatalf("unexpected dynamic exec object: %+v", o)
		}
		found[o.BaseName] = struct{}{}
	}

	for name := range expectedProcs {
		if _, ok := found[name]; !ok {
			t.Fatalf("missing exec proc %s in object output (found %v)", name, found)
		}
	}
}
