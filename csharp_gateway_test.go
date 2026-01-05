//go:build samples
// +build samples

package main

import (
	"os"
	"path/filepath"
	"testing"
)

func TestCSharpQueryGateways(t *testing.T) {
	cases := []struct {
		name         string
		content      string
		matchRaw     string
		usage        string
		isWrite      bool
		isDynamic    bool
		expectations []objectExpectation
	}{
		{
			name:     "CallQueryFromWs multi-statement with temp table",
			content:  `class R { void Run(){ var c = new Db(); c.CallQueryFromWs("http://x", true, "CREATE TABLE #tmp(id int); INSERT INTO dbo.Logs SELECT * FROM dbo.Items; DROP TABLE #tmp;"); } class Db { public void CallQueryFromWs(string u, bool b, string sql){} } }`,
			matchRaw: "INSERT INTO dbo.Logs",
			usage:    "INSERT",
			isWrite:  true,
			expectations: []objectExpectation{
				{schema: "dbo", base: "Items", role: "source", dml: "SELECT"},
				{schema: "dbo", base: "Logs", role: "target", dml: "INSERT"},
			},
		},
		{
			name:         "ExecuteQuery multi-statement with placeholders",
			content:      `class R { void Run(){ var c = new Db(); c.ExecuteQuery("conn", "SELECT * FROM [[schema]].[[paramTableName]] WHERE id IN ([[param]])"); } class Db { public void ExecuteQuery(string conn, string sql){} } }`,
			matchRaw:     "[[paramTableName]]",
			usage:        "SELECT",
			isWrite:      false,
			isDynamic:    true,
			expectations: []objectExpectation{{base: "[schema", role: "source", dml: "SELECT", dyn: true}},
		},
		{
			name:         "comment-only payload stays unknown",
			content:      `class R { void Run(){ var c = new Db(); c.CallQueryFromWs("http://x", true, "-- just a comment\n/* another */"); } class Db { public void CallQueryFromWs(string u, bool b, string sql){} } }`,
			matchRaw:     "just a comment",
			usage:        "UNKNOWN",
			isWrite:      false,
			expectations: []objectExpectation{},
		},
		{
			name:         "dynamic CallQueryFromWs expression cannot be parsed",
			content:      `class R { void Run(){ var c = new Db(); var sqlStmt = BuildSql(); c.CallQueryFromWs("http://x", true, sqlStmt); } string BuildSql(){ return ""; } class Db { public void CallQueryFromWs(string u, bool b, string sql){} } }`,
			matchRaw:     "<dynamic-sql>",
			usage:        "UNKNOWN",
			isWrite:      false,
			isDynamic:    true,
			expectations: []objectExpectation{},
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
			t.Logf("raw=%s objects=%+v", cand.RawSql, cand.Objects)
			assertCandidate(t, *cand, tt.usage, tt.isWrite, tt.isDynamic, tt.expectations)
		})
	}
}
