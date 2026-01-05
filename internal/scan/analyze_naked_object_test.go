package scan

import "testing"

func TestNakedObjectExtraction(t *testing.T) {
	tests := []struct {
		name      string
		sql       string
		wantRoles map[string]string
		wantDml   map[string]string
	}{
		{
			name: "select from joins naked tables",
			sql:  "select * from TableX join TableY on TableX.id = TableY.id",
			wantRoles: map[string]string{
				"TableX": "source",
				"TableY": "source",
			},
			wantDml: map[string]string{
				"TableX": "SELECT",
				"TableY": "SELECT",
			},
		},
		{
			name: "update with from join sources and target",
			sql:  "update TableZ set a=1 from TableZ join TableW on TableZ.id = TableW.id",
			wantRoles: map[string]string{
				"TableZ": "target",
				"TableW": "source",
			},
			wantDml: map[string]string{
				"TableZ": "UPDATE",
				"TableW": "SELECT",
			},
		},
		{
			name: "exec naked proc",
			sql:  "exec ProcB @p1",
			wantRoles: map[string]string{
				"ProcB": "exec",
			},
			wantDml: map[string]string{
				"ProcB": "EXEC",
			},
		},
		{
			name: "truncate naked table",
			sql:  "truncate table TableT",
			wantRoles: map[string]string{
				"TableT": "target",
			},
			wantDml: map[string]string{
				"TableT": "TRUNCATE",
			},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			cand := SqlCandidate{
				RawSql:     tt.sql,
				RelPath:    "file.sql",
				File:       "file.sql",
				AppName:    "app",
				SourceCat:  "code",
				SourceKind: "go",
				LineStart:  10,
				LineEnd:    10,
			}
			analyzeCandidate(&cand)
			if len(tt.wantRoles) != len(tt.wantDml) {
				t.Fatalf("invalid test setup: roles and dml expectation lengths differ")
			}

			for base, role := range tt.wantRoles {
				found := false
				for _, obj := range cand.Objects {
					if obj.BaseName == base {
						found = true
						if obj.SchemaName != "dbo" {
							t.Fatalf("expected schema dbo for %s, got %s", base, obj.SchemaName)
						}
						if obj.Role != role {
							t.Fatalf("object %s role mismatch: got %s want %s", base, obj.Role, role)
						}
						if got := tt.wantDml[base]; obj.DmlKind != got {
							t.Fatalf("object %s DML mismatch: got %s want %s", base, obj.DmlKind, got)
						}
						if role == "source" && obj.IsWrite {
							t.Fatalf("object %s should not be marked write for source role", base)
						}
						if role != "source" && !obj.IsWrite {
							t.Fatalf("object %s should be marked write for role %s", base, role)
						}
					}
				}
				if !found {
					t.Fatalf("expected object %s to be detected", base)
				}
			}
		})
	}
}
