package scan

import (
	"os"
	"path/filepath"
	"sort"
	"strings"
	"testing"

	"code_sql_scan/summary"
)

func TestStripCSComments_RemovesBlockAndLine(t *testing.T) {
	src := "" +
		"var a = 1;\n" +
		"// SELECT should go away\n" +
		"var b = 2; /* DELETE FROM X */ var c = 3;\n" +
		"var d = \"/* not a comment */\";\n" +
		"var e = @\"// keep verbatim\";\n"

	cleaned := stripCSComments(src)
	if contains := "SELECT"; contains != "" && containsString(cleaned, contains) {
		t.Fatalf("expected line comment SQL to be stripped, got %q", cleaned)
	}
	if contains := "DELETE FROM X"; containsString(cleaned, contains) {
		t.Fatalf("expected block comment SQL to be stripped, got %q", cleaned)
	}
	if !containsString(cleaned, "/* not a comment */") {
		t.Fatalf("string literal content should remain, got %q", cleaned)
	}
	if !containsString(cleaned, "// keep verbatim") {
		t.Fatalf("verbatim string content should remain, got %q", cleaned)
	}
}

func TestStripCSComments_PreservesNewlines(t *testing.T) {
	src := "line1\n/* block\ncomment */\nline4\n"
	cleaned := stripCSComments(src)
	lines := strings.Count(cleaned, "\n")
	if lines != strings.Count(src, "\n") {
		t.Fatalf("expected newline count preserved, got %d", lines)
	}
}

func TestStripCSComments_StripsInlineConcatComment(t *testing.T) {
	src := "var sql = \"SELECT 1\"; // + \"FROM dual\"\nreturn sql;"
	cleaned := stripCSComments(src)
	if strings.Contains(cleaned, "FROM dual") {
		t.Fatalf("inline line comment content should be stripped, got %q", cleaned)
	}
	if !strings.Contains(cleaned, "SELECT 1") {
		t.Fatalf("string literal should remain intact, got %q", cleaned)
	}
}

func TestStripCSComments_IgnoresHttpInString(t *testing.T) {
	src := "var url = \"http://example.com\"; // comment\nreturn url;"
	cleaned := stripCSComments(src)
	if !strings.Contains(cleaned, "http://example.com") {
		t.Fatalf("URL literal should be preserved, got %q", cleaned)
	}
	if strings.Contains(cleaned, "comment") {
		t.Fatalf("line comment should be stripped, got %q", cleaned)
	}
}

func TestStripCSComments_StripsBlockWithSql(t *testing.T) {
	src := "/* DELETE FROM Foo */\nvar keep = \"value\";"
	cleaned := stripCSComments(src)
	if strings.Contains(cleaned, "DELETE FROM Foo") {
		t.Fatalf("block comment SQL should be stripped, got %q", cleaned)
	}
	if !strings.Contains(cleaned, "value") {
		t.Fatalf("non-comment content should remain, got %q", cleaned)
	}
}

func TestStripCSComments_StripsDocAndNestedBlocks(t *testing.T) {
	src := "" +
		"/// SELECT * FROM Doc\n" +
		"var keep = \"/* still string */\"; /* outer /* inner SELECT 1 */ end */\n" +
		"var tail = \"value\"; /* trailing */\n"
	cleaned := stripCSComments(src)
	if strings.Contains(cleaned, "Doc") || strings.Contains(cleaned, "inner SELECT 1") {
		t.Fatalf("doc or nested block comment should be stripped, got %q", cleaned)
	}
	if !strings.Contains(cleaned, "/* still string */") {
		t.Fatalf("string literal with comment content should remain, got %q", cleaned)
	}
	if strings.Contains(cleaned, "trailing") {
		t.Fatalf("block comment content should be stripped, got %q", cleaned)
	}
}

func TestStripCSComments_SkipsSqlInsideCommentConcats(t *testing.T) {
	src := "" +
		"var sql = \"SELECT 1\"; // + \"FROM table\"\n" +
		"/* SELECT * FROM Users */ var noop = 1;\n"
	cleaned := stripCSComments(src)
	if strings.Contains(cleaned, "FROM table") || strings.Contains(cleaned, "SELECT * FROM Users") {
		t.Fatalf("SQL inside comments should be removed, got %q", cleaned)
	}
	if !strings.Contains(cleaned, "SELECT 1") {
		t.Fatalf("non-comment SQL should remain, got %q", cleaned)
	}
}

func TestCsScan_ResolvesFunctionsInApiServices(t *testing.T) {
	cfg := &Config{
		Root:    filepath.Clean(filepath.Join("..", "..", "dotnet_check")),
		AppName: "dotnet-sample",
		Lang:    "dotnet",
	}
	resolver := summary.NewFuncResolver(cfg.Root)
	files := []string{
		"clsAPIServiceReksa2.cs",
		"clsAPIServiceNTI.cs",
	}
	for _, file := range files {
		path := filepath.Join(cfg.Root, file)
		cands, err := scanCsFile(cfg, path, file)
		if err != nil {
			t.Fatalf("scanCsFile(%s) returned error: %v", file, err)
		}
		if len(cands) == 0 {
			t.Fatalf("expected candidates for %s, got none", file)
		}
		for _, cand := range cands {
			resolved := resolver.Resolve(cand.Func, cand.RelPath, cand.File, cand.LineStart)
			if strings.EqualFold(strings.TrimSpace(resolved), "<unknown-func>") {
				t.Fatalf("unexpected <unknown-func> for %s line %d raw func %q resolved to %q", file, cand.LineStart, cand.Func, resolved)
			}
		}
	}
}

func TestCsScan_DoesNotDetectSqlInComments(t *testing.T) {
	dir := t.TempDir()
	src := "" +
		"using System;\n" +
		"class Demo {\n" +
		"  void Run() {\n" +
		"    var txt = \"harmless\"; // + \"SELECT * FROM Commented\"\n" +
		"    /* SELECT ignored FROM Block */\n" +
		"  }\n" +
		"}\n"
	path := filepath.Join(dir, "Demo.cs")
	if err := os.WriteFile(path, []byte(src), 0o644); err != nil {
		t.Fatalf("failed to write temp file: %v", err)
	}

	cfg := &Config{
		Root:    dir,
		AppName: "temp",
		Lang:    "dotnet",
	}
	cands, err := scanCsFile(cfg, path, "Demo.cs")
	if err != nil {
		t.Fatalf("scanCsFile returned error: %v", err)
	}
	for _, cand := range cands {
		if strings.Contains(strings.ToUpper(cand.SqlClean), "SELECT") {
			t.Fatalf("expected no SQL detected from comments, got %q", cand.SqlClean)
		}
	}
}

func TestExecProcConstantPropagation(t *testing.T) {
	src := `
using System;
class Demo {
  void Run(bool flag, string input) {
    var conn = new Db();
    var fixedName = "dbo.FixedProc";
    conn.ExecProc(fixedName);
    var ternary = flag ? "dbo.TernA" : "dbo.TernB";
    conn.ExecProc(ternary);
    string sw;
    switch (input) {
      case "x": sw = "dbo.SwitchX"; break;
      case "y": sw = "dbo.SwitchY"; break;
      default: sw = "dbo.SwitchDef"; break;
    }
    conn.ExecProc(sw);
    var interp = "dbo.Interp";
    conn.ExecProc($"exec {interp}");
    var fmt = "dbo.Format";
    conn.ExecProc(string.Format("exec {0}", fmt));
  }
}
class Db { public void ExecProc(string s) {} }
`

	dir := t.TempDir()
	path := filepath.Join(dir, "Demo.cs")
	if err := os.WriteFile(path, []byte(src), 0o644); err != nil {
		t.Fatalf("write file: %v", err)
	}

	cfg := &Config{
		Root:    dir,
		AppName: "app",
		Lang:    "dotnet",
	}
	cands, err := scanCsFile(cfg, path, "Demo.cs")
	if err != nil {
		t.Fatalf("scanCsFile: %v", err)
	}
	var procs []string
	for i := range cands {
		analyzeCandidate(&cands[i])
		if !strings.EqualFold(cands[i].UsageKind, "EXEC") {
			continue
		}
		if cands[i].IsDynamic {
			t.Fatalf("expected resolved exec proc, got dynamic cand: %+v", cands[i])
		}
		for _, o := range cands[i].Objects {
			if strings.EqualFold(o.Role, "exec") && strings.TrimSpace(o.BaseName) != "" {
				procs = append(procs, o.BaseName)
			}
		}
	}
	sort.Strings(procs)
	expected := []string{"FixedProc", "Format", "Interp", "SwitchDef", "SwitchX", "SwitchY", "TernA", "TernB"}
	if len(procs) != len(expected) {
		t.Fatalf("expected %d proc calls, got %d (%v)", len(expected), len(procs), procs)
	}
	for i := range expected {
		if procs[i] != expected[i] {
			t.Fatalf("proc mismatch at %d: got %s want %s (all=%v)", i, procs[i], expected[i], procs)
		}
	}
}

func containsString(haystack, needle string) bool {
	return strings.Contains(haystack, needle)
}
