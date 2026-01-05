package scan

import "testing"

func TestCSharpConcatDynamicObjectInsert(t *testing.T) {
	expr := `"INSERT INTO dbo.[[" + tableName + "]] (ColA) SELECT ColA"`

	sql, dyn, _ := BuildSqlSkeletonFromCSharpExpr(expr)
	if sql == "" {
		t.Fatalf("expected skeleton, got empty")
	}
	if !dyn {
		t.Fatalf("expected dynamic flag for concatenated table name")
	}

	cand := SqlCandidate{
		RawSql:    sql,
		IsDynamic: dyn,
		LineStart: 10,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "INSERT" {
		t.Fatalf("expected INSERT usage, got %s", cand.UsageKind)
	}
	if len(cand.Objects) != 1 {
		t.Fatalf("expected 1 object, got %d", len(cand.Objects))
	}
	obj := cand.Objects[0]
	if obj.PseudoKind != "dynamic-object" {
		t.Fatalf("expected dynamic-object pseudo, got %s", obj.PseudoKind)
	}
	if obj.Role != "target" || !obj.IsWrite || obj.DmlKind != "INSERT" {
		t.Fatalf("expected insert target classification, got role=%s write=%v dml=%s", obj.Role, obj.IsWrite, obj.DmlKind)
	}
}

func TestCSharpInterpolatedTruncateDynamicObject(t *testing.T) {
	expr := `$"TRUNCATE TABLE dbo.{tableName}"`

	sql, dyn, _ := BuildSqlSkeletonFromCSharpExpr(expr)
	if sql == "" {
		t.Fatalf("expected skeleton, got empty")
	}
	if !dyn {
		t.Fatalf("expected dynamic flag for interpolated table name")
	}

	cand := SqlCandidate{
		RawSql:    sql,
		IsDynamic: dyn,
		LineStart: 20,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "TRUNCATE" {
		t.Fatalf("expected TRUNCATE usage, got %s", cand.UsageKind)
	}
	if len(cand.Objects) != 1 {
		t.Fatalf("expected 1 object, got %d", len(cand.Objects))
	}
	obj := cand.Objects[0]
	if obj.PseudoKind != "dynamic-object" {
		t.Fatalf("expected dynamic-object pseudo, got %s", obj.PseudoKind)
	}
	if obj.Role != "target" || !obj.IsWrite || obj.DmlKind != "TRUNCATE" {
		t.Fatalf("expected truncate target classification, got role=%s write=%v dml=%s", obj.Role, obj.IsWrite, obj.DmlKind)
	}
}

func TestCSharpStringFormatInsertDynamicObject(t *testing.T) {
	expr := `string.Format("INSERT INTO dbo.{0} (ColA) VALUES ({1})", tableName, valueExpr)`

	sql, dyn, _ := BuildSqlSkeletonFromCSharpExpr(expr)
	if sql == "" {
		t.Fatalf("expected skeleton, got empty")
	}
	if !dyn {
		t.Fatalf("expected dynamic flag for formatted table name")
	}

	cand := SqlCandidate{
		RawSql:    sql,
		IsDynamic: dyn,
		LineStart: 30,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "INSERT" {
		t.Fatalf("expected INSERT usage, got %s", cand.UsageKind)
	}
	if len(cand.Objects) != 1 {
		t.Fatalf("expected 1 object, got %d", len(cand.Objects))
	}
	obj := cand.Objects[0]
	if obj.PseudoKind != "dynamic-object" {
		t.Fatalf("expected dynamic-object pseudo, got %s", obj.PseudoKind)
	}
	if obj.Role != "target" || !obj.IsWrite || obj.DmlKind != "INSERT" {
		t.Fatalf("expected insert target classification, got role=%s write=%v dml=%s", obj.Role, obj.IsWrite, obj.DmlKind)
	}
}
