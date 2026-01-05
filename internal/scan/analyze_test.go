package scan

import (
	"strings"
	"testing"
)

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

func TestInsertSelectSourceExtraction(t *testing.T) {
	cand := SqlCandidate{
		RawSql:    "INSERT INTO dbo.TableA (col1) SELECT col1 FROM dbo.TableB",
		LineStart: 30,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "INSERT" {
		t.Fatalf("unexpected usage kind: got %s want INSERT", cand.UsageKind)
	}
	if len(cand.Objects) != 2 {
		t.Fatalf("expected 2 objects, got %d", len(cand.Objects))
	}

	find := func(base string) (ObjectToken, bool) {
		for _, obj := range cand.Objects {
			if strings.EqualFold(obj.BaseName, base) {
				return obj, true
			}
		}
		return ObjectToken{}, false
	}

	target, ok := find("TableA")
	if !ok {
		t.Fatalf("expected to find TableA target")
	}
	if target.Role != "target" || target.DmlKind != "INSERT" || !target.IsWrite {
		t.Fatalf("unexpected target classification: role=%s dml=%s write=%v", target.Role, target.DmlKind, target.IsWrite)
	}

	source, ok := find("TableB")
	if !ok {
		t.Fatalf("expected to find TableB source")
	}
	if source.Role != "source" || source.DmlKind != "SELECT" || source.IsWrite {
		t.Fatalf("unexpected source classification: role=%s dml=%s write=%v", source.Role, source.DmlKind, source.IsWrite)
	}
}

func TestUpdateFromSourceExtraction(t *testing.T) {
	cand := SqlCandidate{
		RawSql:    "UPDATE dbo.TableA SET col1 = src.col1 FROM dbo.TableB src",
		LineStart: 40,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "UPDATE" {
		t.Fatalf("unexpected usage kind: got %s want UPDATE", cand.UsageKind)
	}
	if len(cand.Objects) != 2 {
		t.Fatalf("expected 2 objects, got %d", len(cand.Objects))
	}

	find := func(base string) (ObjectToken, bool) {
		for _, obj := range cand.Objects {
			if strings.EqualFold(obj.BaseName, base) {
				return obj, true
			}
		}
		return ObjectToken{}, false
	}

	target, ok := find("TableA")
	if !ok {
		t.Fatalf("expected to find TableA target")
	}
	if target.Role != "target" || target.DmlKind != "UPDATE" || !target.IsWrite {
		t.Fatalf("unexpected target classification: role=%s dml=%s write=%v", target.Role, target.DmlKind, target.IsWrite)
	}

	source, ok := find("TableB")
	if !ok {
		t.Fatalf("expected to find TableB source")
	}
	if source.Role != "source" || source.DmlKind != "SELECT" || source.IsWrite {
		t.Fatalf("unexpected source classification: role=%s dml=%s write=%v", source.Role, source.DmlKind, source.IsWrite)
	}
}

func TestMergeUsingSourceExtraction(t *testing.T) {
	cand := SqlCandidate{
		RawSql:    "MERGE INTO dbo.TableA AS tgt USING dbo.TableB AS src ON tgt.Id = src.Id WHEN MATCHED THEN UPDATE SET tgt.Col = src.Col;",
		LineStart: 50,
	}

	analyzeCandidate(&cand)

	if cand.UsageKind != "MERGE" {
		t.Fatalf("unexpected usage kind: got %s want MERGE", cand.UsageKind)
	}
	if len(cand.Objects) != 2 {
		t.Fatalf("expected 2 objects, got %d", len(cand.Objects))
	}

	find := func(base string) (ObjectToken, bool) {
		for _, obj := range cand.Objects {
			if strings.EqualFold(obj.BaseName, base) {
				return obj, true
			}
		}
		return ObjectToken{}, false
	}

	target, ok := find("TableA")
	if !ok {
		t.Fatalf("expected to find TableA target")
	}
	if target.Role != "target" || target.DmlKind != "MERGE" || !target.IsWrite {
		t.Fatalf("unexpected target classification: role=%s dml=%s write=%v", target.Role, target.DmlKind, target.IsWrite)
	}

	source, ok := find("TableB")
	if !ok {
		t.Fatalf("expected to find TableB source")
	}
	if source.Role != "source" || source.DmlKind != "SELECT" || source.IsWrite {
		t.Fatalf("unexpected source classification: role=%s dml=%s write=%v", source.Role, source.DmlKind, source.IsWrite)
	}
}
