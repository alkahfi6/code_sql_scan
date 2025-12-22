package scan

import (
	"strings"
	"testing"
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
	lines := 0
	for _, c := range cleaned {
		if c == '\n' {
			lines++
		}
	}
	if lines != 3 {
		t.Fatalf("expected newline count preserved, got %d", lines)
	}
}

func containsString(haystack, needle string) bool {
	return strings.Contains(haystack, needle)
}
