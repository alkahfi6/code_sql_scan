package scan

import (
	"path/filepath"
	"testing"

	"code_sql_scan/summary"
)

func TestObjectUsageKeepsRowsPerQueryHash(t *testing.T) {
	dir := t.TempDir()
	cfg := &Config{
		AppName:   "app",
		Root:      dir,
		OutQuery:  filepath.Join(dir, "app-query.csv"),
		OutObject: filepath.Join(dir, "app-object.csv"),
	}

	cands := []SqlCandidate{
		{
			AppName:   "app",
			RelPath:   "file.cs",
			File:      "file.cs",
			Func:      "Func",
			RawSql:    "exec dbo.One",
			SqlClean:  "exec dbo.One",
			UsageKind: "EXEC",
			QueryHash: "hash-one",
			Objects: []ObjectToken{{
				DbName:             "",
				SchemaName:         "dbo",
				BaseName:           "One",
				FullName:           "dbo.One",
				Role:               "exec",
				DmlKind:            "EXEC",
				IsWrite:            true,
				RepresentativeLine: 10,
			}},
		},
		{
			AppName:   "app",
			RelPath:   "file.cs",
			File:      "file.cs",
			Func:      "Func",
			RawSql:    "exec dbo.OneOther",
			SqlClean:  "exec dbo.OneOther",
			UsageKind: "EXEC",
			QueryHash: "hash-two",
			Objects: []ObjectToken{{
				DbName:             "",
				SchemaName:         "dbo",
				BaseName:           "One",
				FullName:           "dbo.One",
				Role:               "exec",
				DmlKind:            "EXEC",
				IsWrite:            true,
				RepresentativeLine: 10,
			}},
		},
	}

	if err := writeCSVs(cfg, cands); err != nil {
		t.Fatalf("writeCSVs failed: %v", err)
	}

	objs, err := summary.LoadObjectUsage(cfg.OutObject)
	if err != nil {
		t.Fatalf("load object usage: %v", err)
	}
	if len(objs) != 2 {
		t.Fatalf("expected 2 object rows, got %d: %#v", len(objs), objs)
	}
	hashes := map[string]struct{}{objs[0].QueryHash: {}, objs[1].QueryHash: {}}
	if len(hashes) != 2 {
		t.Fatalf("expected distinct query hashes in output, got %v", hashes)
	}
}
