//go:build samples
// +build samples

package main

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

type objectExpectation struct {
	db, schema, base, role, dml string
	cross, linked, dyn          bool
}

func assertCandidate(t *testing.T, cand SqlCandidate, usage string, isWrite, isDyn bool, objs []objectExpectation) {
	t.Helper()
	if cand.UsageKind != usage {
		t.Fatalf("unexpected usage kind: got %s want %s", cand.UsageKind, usage)
	}
	if cand.IsWrite != isWrite {
		t.Fatalf("unexpected write flag: got %v want %v", cand.IsWrite, isWrite)
	}
	if cand.IsDynamic != isDyn {
		t.Fatalf("unexpected dynamic flag: got %v want %v", cand.IsDynamic, isDyn)
	}
	if len(cand.Objects) != len(objs) {
		t.Fatalf("unexpected object count for %s: got %d want %d (%+v)", cand.RawSql, len(cand.Objects), len(objs), cand.Objects)
	}
	for i, obj := range cand.Objects {
		exp := objs[i]
		if obj.DbName != exp.db || obj.SchemaName != exp.schema || obj.BaseName != exp.base {
			t.Fatalf("object %d name mismatch: got %s.%s.%s", i, obj.DbName, obj.SchemaName, obj.BaseName)
		}
		if obj.Role != exp.role || obj.DmlKind != exp.dml {
			t.Fatalf("object %d role/dml mismatch: got %s/%s want %s/%s", i, obj.Role, obj.DmlKind, exp.role, exp.dml)
		}
		if obj.IsCrossDb != exp.cross || obj.IsLinkedServer != exp.linked || obj.IsObjectNameDyn != exp.dyn {
			t.Fatalf("object %d flags mismatch: cross=%v linked=%v dyn=%v", i, obj.IsCrossDb, obj.IsLinkedServer, obj.IsObjectNameDyn)
		}
	}
}

func findCandidateByRaw(t *testing.T, cands []SqlCandidate, contains string) *SqlCandidate {
	t.Helper()
	for i := range cands {
		if strings.Contains(cands[i].RawSql, contains) {
			return &cands[i]
		}
	}
	var raws []string
	for _, c := range cands {
		raws = append(raws, c.RawSql)
	}
	t.Fatalf("candidate containing %q not found; saw %v", contains, raws)
	return nil
}

func TestCSharpExecProcGateway(t *testing.T) {
	cases := []struct {
		name     string
		content  string
		matchRaw string
		want     objectExpectation
	}{
		{
			name:     "literal proc call stays exec stub",
			content:  `using System; class R { void Run(){ var c = new Db(); c.ExecProc("dbo.RunMe"); } class Db { public void ExecProc(string s){} } }`,
			matchRaw: "dbo.RunMe",
			want:     objectExpectation{schema: "dbo", base: "RunMe", role: "exec", dml: "EXEC", dyn: false},
		},
		{
			name:     "identifier resolved to literal",
			content:  `class R { void Run(){ var conn = new Db(); var procName = "Sales.uspSync"; conn.ExecProc(procName); } class Db { public void ExecProc(string s){} } }`,
			matchRaw: "Sales.uspSync",
			want:     objectExpectation{schema: "Sales", base: "uspSync", role: "exec", dml: "EXEC", dyn: false},
		},
		{
			name: "SqlCommand uses identifier first arg",
			content: `using System.Data;
class R {
    void Run(){
        var procName = "dbo.SyncOrders";
        var cmd = new SqlCommand(procName, new Conn());
        cmd.CommandType = CommandType.StoredProcedure;
    }
}
class Conn { }`,
			matchRaw: "dbo.SyncOrders",
			want:     objectExpectation{schema: "dbo", base: "SyncOrders", role: "exec", dml: "EXEC", dyn: false},
		},
		{
			name: "CommandText assigned from identifier",
			content: `using System.Data;
class R {
    void Run(){
        var proc = "Inventory.AdjustStock";
        var cmd = new Cmd();
        cmd.CommandText = proc;
        cmd.CommandType = CommandType.StoredProcedure;
    }
}
class Cmd { public string CommandText { get; set; } public CommandType CommandType { get; set; } }`,
			matchRaw: "Inventory.AdjustStock",
			want:     objectExpectation{schema: "Inventory", base: "AdjustStock", role: "exec", dml: "EXEC", dyn: false},
		},
	}

	for _, tt := range cases {
		t.Run(tt.name, func(t *testing.T) {
			tmp := t.TempDir()
			path := filepath.Join(tmp, "repo.cs")
			if err := os.WriteFile(path, []byte(tt.content), 0o644); err != nil {
				t.Fatalf("write file: %v", err)
			}

			cfg := &Config{Root: tmp, AppName: "app"}
			cands, err := scanCsFile(cfg, path, "repo.cs")
			if err != nil {
				t.Fatalf("scanCsFile: %v", err)
			}

			for i := range cands {
				analyzeCandidate(&cands[i])
			}
			cand := findCandidateByRaw(t, cands, tt.matchRaw)
			assertCandidate(t, *cand, "EXEC", true, false, []objectExpectation{tt.want})
		})
	}
}
