package summary

import "testing"

func TestBuildFunctionSummary_RawDynamicCounts(t *testing.T) {
	queries := []QueryRow{
		{
			AppName:    "sample",
			RelPath:    "sample.cs",
			File:       "sample.cs",
			Func:       "TestFunc",
			UsageKind:  "EXEC",
			IsWrite:    true,
			IsDynamic:  true,
			DynamicSig: "dyn-sig",
			QueryHash:  "hash-1",
			LineStart:  10,
			LineEnd:    12,
		},
		{
			AppName:    "sample",
			RelPath:    "sample.cs",
			File:       "sample.cs",
			Func:       "TestFunc",
			UsageKind:  "EXEC",
			IsWrite:    true,
			IsDynamic:  true,
			DynamicSig: "dyn-sig",
			QueryHash:  "hash-2",
			LineStart:  20,
			LineEnd:    22,
		},
	}

	summaryRows, err := BuildFunctionSummary(queries, nil)
	if err != nil {
		t.Fatalf("build summary: %v", err)
	}
	if len(summaryRows) != 1 {
		t.Fatalf("expected 1 summary row, got %d", len(summaryRows))
	}
	row := summaryRows[0]
	if row.TotalQueries != 2 {
		t.Fatalf("TotalQueries mismatch: got %d want %d", row.TotalQueries, 2)
	}
	if row.TotalExec != 2 {
		t.Fatalf("TotalExec mismatch: got %d want %d", row.TotalExec, 2)
	}
	if row.TotalWrite != 2 {
		t.Fatalf("TotalWrite mismatch: got %d want %d", row.TotalWrite, 2)
	}
	if row.TotalDynamic != 2 {
		t.Fatalf("TotalDynamic mismatch: got %d want %d", row.TotalDynamic, 2)
	}
	if row.TotalDynamicSql != 2 {
		t.Fatalf("TotalDynamicSql mismatch: got %d want %d", row.TotalDynamicSql, 2)
	}
	if row.DynamicCount != 2 {
		t.Fatalf("DynamicCount mismatch: got %d want %d", row.DynamicCount, 2)
	}

	mismatches := compareFunctionSummary(queries, nil, summaryRows)
	if len(mismatches) > 0 {
		t.Fatalf("expected consistency to pass, got mismatches: %v", mismatches)
	}
}
