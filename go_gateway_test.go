//go:build samples
// +build samples

package main

import (
	"os"
	"path/filepath"
	"testing"
)

func TestGoSqlcConstantsAndLocalQueries(t *testing.T) {
	connStore = newConnRegistry()
	connStore.set("db", "MainDb")

	src := `package sample

import "fmt"

const (
    selectAll = "SELECT * FROM dbo.Users"
    deleteUser = "DELETE FROM dbo.Users WHERE id=@id"
    execProc = "Reporting.dbo.SyncProc"
)

func run(ctx any, db *DB) {
    db.Query(selectAll)
    db.Exec(deleteUser)
    db.ExecContext(ctx, execProc)
}

func local(db *DB) {
    base := "SELECT * FROM Orders"
    temp := base + " WHERE Region = 'X'"
    db.Query(temp)
    stmt := "CREATE TABLE #tmp(id int); INSERT INTO dbo.Audit SELECT * FROM dbo.Orders"
    db.Exec(stmt)
    db.Exec("TRUNCATE TABLE [LinkSrv].[DbX].[dbo].[Archive]")
    dyn := fmt.Sprintf("SELECT * FROM %s", "[[schema]].[[table]]")
    db.Query(dyn)
}

type DB struct{}

func (d *DB) Query(query string, args ...any) {}
func (d *DB) Exec(query string, args ...any)  {}
func (d *DB) ExecContext(ctx any, query string, args ...any) {}
`

	tmp := t.TempDir()
	path := filepath.Join(tmp, "repo.go")
	if err := os.WriteFile(path, []byte(src), 0o644); err != nil {
		t.Fatalf("write file: %v", err)
	}

	cfg := &Config{Root: tmp, AppName: "app"}
	cands, err := scanGoFile(cfg, path, "repo.go")
	if err != nil {
		t.Fatalf("scanGoFile: %v", err)
	}
	if len(cands) != 7 {
		t.Fatalf("expected 7 candidates, got %d", len(cands))
	}

	expectations := map[string]struct {
		usage   string
		isWrite bool
		isDyn   bool
		objects []objectExpectation
	}{
		"SELECT * FROM dbo.Users": {
			usage:   "SELECT",
			isWrite: false,
			objects: []objectExpectation{{schema: "dbo", base: "Users", role: "source", dml: "SELECT"}},
		},
		"DELETE FROM dbo.Users WHERE id=@id": {
			usage:   "DELETE",
			isWrite: true,
			objects: []objectExpectation{
				{schema: "dbo", base: "Users", role: "target", dml: "DELETE"},
				{schema: "dbo", base: "Users", role: "source", dml: "SELECT"},
			},
		},
		"Reporting.dbo.SyncProc": {
			usage:   "EXEC",
			isWrite: true,
			objects: []objectExpectation{{db: "Reporting", schema: "dbo", base: "SyncProc", role: "exec", dml: "EXEC", cross: true}},
		},
		"SELECT * FROM Orders WHERE Region = 'X'": {
			usage:   "SELECT",
			isWrite: false,
			objects: []objectExpectation{{base: "Orders", role: "source", dml: "SELECT"}},
		},
		"CREATE TABLE #tmp(id int); INSERT INTO dbo.Audit SELECT * FROM dbo.Orders": {
			usage:   "INSERT",
			isWrite: true,
			objects: []objectExpectation{
				{schema: "dbo", base: "Orders", role: "source", dml: "SELECT"},
				{schema: "dbo", base: "Audit", role: "target", dml: "INSERT"},
			},
		},
		"TRUNCATE TABLE [LinkSrv].[DbX].[dbo].[Archive]": {
			usage:   "TRUNCATE",
			isWrite: true,
			objects: []objectExpectation{{db: "DbX", schema: "dbo", base: "Archive", role: "target", dml: "TRUNCATE", linked: true, cross: true}},
		},
		"<dynamic-sql>": {
			usage:   "UNKNOWN",
			isWrite: false,
			isDyn:   true,
			objects: nil,
		},
	}

	for i := range cands {
		analyzeCandidate(&cands[i])
	}

	for _, cand := range cands {
		t.Logf("func=%s raw=%s usage=%s dyn=%v objs=%d", cand.Func, cand.RawSql, cand.UsageKind, cand.IsDynamic, len(cand.Objects))
		exp, ok := expectations[cand.RawSql]
		if !ok {
			t.Fatalf("unexpected candidate with raw SQL: %s", cand.RawSql)
		}
		assertCandidate(t, cand, exp.usage, exp.isWrite, exp.isDyn, exp.objects)
	}
}
