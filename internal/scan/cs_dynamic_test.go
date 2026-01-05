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
	t.Logf("concat insert objects: %+v", cand.Objects)

	if cand.UsageKind != "INSERT" {
		t.Fatalf("expected INSERT usage, got %s", cand.UsageKind)
	}
	if len(cand.Objects) != 1 {
		t.Fatalf("expected 1 object, got %d", len(cand.Objects))
	}
	obj := cand.Objects[0]
	if obj.PseudoKind != "schema-placeholder" {
		t.Fatalf("expected schema-placeholder pseudo, got %s", obj.PseudoKind)
	}
	if obj.Role != "target" || !obj.IsWrite || obj.DmlKind != "INSERT" || !obj.IsObjectNameDyn {
		t.Fatalf("expected insert target classification, got role=%s write=%v dml=%s dyn=%v", obj.Role, obj.IsWrite, obj.DmlKind, obj.IsObjectNameDyn)
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
	t.Logf("interpolated truncate objects: %+v", cand.Objects)

	if cand.UsageKind != "TRUNCATE" {
		t.Fatalf("expected TRUNCATE usage, got %s", cand.UsageKind)
	}
	if len(cand.Objects) != 2 {
		t.Fatalf("expected 2 objects, got %d", len(cand.Objects))
	}
	hasDyn, hasPlaceholder := false, false
	for _, obj := range cand.Objects {
		switch obj.PseudoKind {
		case "dynamic-object":
			if obj.Role != "target" || !obj.IsWrite || obj.DmlKind != "TRUNCATE" || !obj.IsObjectNameDyn {
				t.Fatalf("expected dynamic truncate target, got role=%s write=%v dml=%s dyn=%v", obj.Role, obj.IsWrite, obj.DmlKind, obj.IsObjectNameDyn)
			}
			hasDyn = true
		case "schema-placeholder":
			hasPlaceholder = true
		}
	}
	if !hasDyn || !hasPlaceholder {
		t.Fatalf("missing dynamic-object (%v) or schema-placeholder (%v)", hasDyn, hasPlaceholder)
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
	t.Logf("string format insert objects: %+v", cand.Objects)

	if cand.UsageKind != "INSERT" {
		t.Fatalf("expected INSERT usage, got %s", cand.UsageKind)
	}
	if len(cand.Objects) != 2 {
		t.Fatalf("expected 2 objects, got %d", len(cand.Objects))
	}
	hasDyn, hasPlaceholder := false, false
	for _, obj := range cand.Objects {
		switch obj.PseudoKind {
		case "dynamic-object":
			if obj.Role != "target" || !obj.IsWrite || obj.DmlKind != "INSERT" || !obj.IsObjectNameDyn {
				t.Fatalf("expected dynamic insert target, got role=%s write=%v dml=%s dyn=%v", obj.Role, obj.IsWrite, obj.DmlKind, obj.IsObjectNameDyn)
			}
			hasDyn = true
		case "schema-placeholder":
			hasPlaceholder = true
		}
	}
	if !hasDyn || !hasPlaceholder {
		t.Fatalf("missing dynamic-object (%v) or schema-placeholder (%v)", hasDyn, hasPlaceholder)
	}
}
