package scan

import (
	"strings"
	"testing"
)

func TestDetectTruncateAndMergeTargets(t *testing.T) {
	initRegexes()

	t.Run("truncate target only", func(t *testing.T) {
		cand := &SqlCandidate{
			RawSql:    "TRUNCATE TABLE dbo.TempValas",
			LineStart: 12,
		}

		analyzeCandidate(cand)

		if cand.UsageKind != "TRUNCATE" {
			t.Fatalf("unexpected usage kind: %s", cand.UsageKind)
		}
		if len(cand.Objects) != 1 {
			t.Fatalf("expected 1 object, got %d", len(cand.Objects))
		}

		obj := cand.Objects[0]
		if obj.BaseName != "TempValas" || obj.SchemaName != "dbo" {
			t.Fatalf("unexpected truncate object parts: db=%s schema=%s base=%s", obj.DbName, obj.SchemaName, obj.BaseName)
		}
		if obj.Role != "target" || obj.DmlKind != "TRUNCATE" || !obj.IsWrite {
			t.Fatalf("truncate should be write target, got %+v", obj)
		}
		if obj.DbName != "" {
			t.Fatalf("truncate should not infer cross-db for dbo scoped table, got db=%s", obj.DbName)
		}
	})

	t.Run("truncate cross-db two-part", func(t *testing.T) {
		cand := &SqlCandidate{
			RawSql:    "TRUNCATE TABLE dbA..TempValas",
			LineStart: 20,
		}

		analyzeCandidate(cand)

		if cand.UsageKind != "TRUNCATE" {
			t.Fatalf("unexpected usage kind: %s", cand.UsageKind)
		}
		if len(cand.Objects) != 1 {
			t.Fatalf("expected 1 object, got %d", len(cand.Objects))
		}
		obj := cand.Objects[0]
		if obj.DbName != "dbA" || obj.SchemaName != "dbo" || obj.BaseName != "TempValas" {
			t.Fatalf("unexpected cross-db truncate parts: db=%s schema=%s base=%s", obj.DbName, obj.SchemaName, obj.BaseName)
		}
		if obj.Role != "target" || !obj.IsWrite {
			t.Fatalf("cross-db truncate should be write target, got %+v", obj)
		}
		if !obj.IsCrossDb || !cand.HasCrossDb {
			t.Fatalf("cross-db flags should be set for dbA..TempValas")
		}
	})

	t.Run("merge target and source", func(t *testing.T) {
		cand := &SqlCandidate{
			RawSql: `MERGE INTO dbo.Customer AS c USING dbo.TempCustomer AS t ON c.id = t.id
WHEN MATCHED THEN UPDATE SET name = t.name
WHEN NOT MATCHED THEN INSERT (id,name) VALUES (t.id,t.name);`,
			LineStart: 30,
		}

		analyzeCandidate(cand)

		if cand.UsageKind != "MERGE" {
			t.Fatalf("unexpected usage kind: %s", cand.UsageKind)
		}
		if len(cand.Objects) != 2 {
			t.Fatalf("expected 2 objects, got %d: %+v", len(cand.Objects), cand.Objects)
		}

		var target, source *ObjectToken
		for i := range cand.Objects {
			o := &cand.Objects[i]
			switch o.FullName {
			case "dbo.Customer":
				target = o
			case "dbo.TempCustomer":
				source = o
			}
		}

		if target == nil {
			t.Fatalf("expected target dbo.Customer present")
		}
		if target.Role != "target" || target.DmlKind == "" || !target.IsWrite {
			t.Fatalf("merge target should be write, got %+v", *target)
		}
		if !strings.HasPrefix(target.DmlKind, "MERGE") && !strings.Contains(target.DmlKind, "MERGE") {
			t.Fatalf("merge target should retain MERGE dml kind, got %s", target.DmlKind)
		}

		if source == nil {
			t.Fatalf("expected source dbo.TempCustomer present")
		}
		if source.Role != "source" || source.IsWrite {
			t.Fatalf("merge source should be read-only, got %+v", *source)
		}
	})
}
