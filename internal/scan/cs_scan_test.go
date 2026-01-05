package scan

import (
	"path/filepath"
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

func containsString(haystack, needle string) bool {
	return strings.Contains(haystack, needle)
}
