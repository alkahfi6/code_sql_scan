package scan

import "testing"

func TestTablePlaceholderTruncate(t *testing.T) {
	cand := SqlCandidate{
		RawSql:    "TRUNCATE TABLE dbo.[[paramTableName]]",
		IsDynamic: true,
		LineStart: 10,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "TRUNCATE" {
		t.Fatalf("unexpected usage kind: got %s want TRUNCATE", cand.UsageKind)
	}
	if len(cand.Objects) != 1 {
		t.Fatalf("expected 1 object, got %d", len(cand.Objects))
	}

	obj := cand.Objects[0]
	if obj.PseudoKind != "table-placeholder" {
		t.Fatalf("expected pseudo kind table-placeholder, got %s", obj.PseudoKind)
	}
	if obj.Role != "target" {
		t.Fatalf("expected target role, got %s", obj.Role)
	}
	if !obj.IsWrite || obj.DmlKind != "TRUNCATE" {
		t.Fatalf("expected truncate write classification, got write=%v dml=%s", obj.IsWrite, obj.DmlKind)
	}
}

func TestSchemaPlaceholderCrossDbInsert(t *testing.T) {
	cand := SqlCandidate{
		RawSql:    "INSERT INTO [[dbName]].dbo.MyTable VALUES (1)",
		IsDynamic: true,
		LineStart: 20,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "INSERT" {
		t.Fatalf("unexpected usage kind: got %s want INSERT", cand.UsageKind)
	}
	if len(cand.Objects) != 1 {
		t.Fatalf("expected 1 object, got %d", len(cand.Objects))
	}

	obj := cand.Objects[0]
	if obj.PseudoKind != "schema-placeholder" {
		t.Fatalf("expected pseudo kind schema-placeholder, got %s", obj.PseudoKind)
	}
	if obj.BaseName != "MyTable" {
		t.Fatalf("expected base name MyTable, got %s", obj.BaseName)
	}
	if obj.Role != "target" {
		t.Fatalf("expected target role, got %s", obj.Role)
	}
	if !obj.IsCrossDb || !cand.HasCrossDb {
		t.Fatalf("expected cross-db flags set, got obj=%v cand=%v", obj.IsCrossDb, cand.HasCrossDb)
	}
	if !obj.IsWrite || obj.DmlKind != "INSERT" {
		t.Fatalf("expected insert write classification, got write=%v dml=%s", obj.IsWrite, obj.DmlKind)
	}
}
