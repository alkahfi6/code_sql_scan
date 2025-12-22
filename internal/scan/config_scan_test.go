package scan

import (
	"strings"
	"testing"
)

func TestStripCodeCommentsCStyle_RemovesSqlInComments(t *testing.T) {
	src := `var sql = "SELECT 1";
/* SELECT from Comments should be stripped */
var joined = "SELECT" // + " FROM ShouldNotAppear"
var block = @"Line1"
/* nested start /* SELECT * FROM dbo.TableB */ end */
`
	cleaned := StripCodeCommentsCStyle(src, true)
	if strings.Contains(strings.ToUpper(cleaned), "FROM COMMENTS") {
		t.Fatalf("expected block comment SQL to be removed, got: %s", cleaned)
	}
	if strings.Contains(strings.ToUpper(cleaned), "FROM SHOULDNOTAPPEAR") {
		t.Fatalf("expected line comment concatenation to be stripped, got: %s", cleaned)
	}
	if strings.Count(cleaned, "SELECT") < 1 {
		t.Fatalf("expected original literal to remain, cleaned: %s", cleaned)
	}
}
