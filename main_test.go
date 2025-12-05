package main

import "testing"

func TestParseProcNameSpecRemovesParamPlaceholders(t *testing.T) {
	tests := []struct {
		name   string
		spec   string
		full   string
		base   string
		schema string
		db     string
		isDyn  bool
	}{
		{
			name:   "Bracketed schema with question marks",
			spec:   "[dbo].[MyProc] ?, ?",
			full:   "[dbo].[MyProc]",
			base:   "MyProc",
			schema: "dbo",
			db:     "",
			isDyn:  true,
		},
		{
			name:   "Bracketed db and schema with named params",
			spec:   "[db].[schema].[ProcName] @p1, @p2",
			full:   "[db].[schema].[ProcName]",
			base:   "ProcName",
			schema: "schema",
			db:     "db",
			isDyn:  true,
		},
		{
			name:   "Colon parameters",
			spec:   "[dbo].[ProcWithParams] :1, :2",
			full:   "[dbo].[ProcWithParams]",
			base:   "ProcWithParams",
			schema: "dbo",
			db:     "",
			isDyn:  true,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			tok := parseProcNameSpec(tt.spec)
			if tok.FullName != tt.full {
				t.Fatalf("FullName = %q, want %q", tok.FullName, tt.full)
			}
			if tok.BaseName != tt.base {
				t.Fatalf("BaseName = %q, want %q", tok.BaseName, tt.base)
			}
			if tok.SchemaName != tt.schema {
				t.Fatalf("SchemaName = %q, want %q", tok.SchemaName, tt.schema)
			}
			if tok.DbName != tt.db {
				t.Fatalf("DbName = %q, want %q", tok.DbName, tt.db)
			}
			if tok.IsObjectNameDyn != tt.isDyn {
				t.Fatalf("IsObjectNameDyn = %v, want %v", tok.IsObjectNameDyn, tt.isDyn)
			}
		})
	}
}
