package main

import (
	"strings"
	"testing"
)

func TestDetectUsageKindSkipsLeadingStatements(t *testing.T) {
	tests := []struct {
		name     string
		sql      string
		expected string
	}{
		{
			name:     "select after declare and if",
			sql:      "DECLARE @foo INT; IF @foo = 1 BEGIN SELECT * FROM table1; END",
			expected: "SELECT",
		},
		{
			name:     "insert after declare and set",
			sql:      "DECLARE @foo INT; SET @foo = 1; IF @foo = 1 BEGIN INSERT INTO table1(id) VALUES (1); END",
			expected: "INSERT",
		},
		{
			name:     "delete after temp table management",
			sql:      "IF OBJECT_ID('tempdb..#temp') IS NOT NULL DROP TABLE #temp; CREATE TABLE #temp(id INT); DELETE FROM table1 WHERE id = 1;",
			expected: "DELETE",
		},
		{
			name:     "truncate after declare and if",
			sql:      "DECLARE @exists BIT; IF @exists = 1 BEGIN TRUNCATE TABLE #temp; END",
			expected: "TRUNCATE",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			if got := detectUsageKind(false, tt.sql); got != tt.expected {
				t.Fatalf("expected %s, got %s", tt.expected, got)
			}
		})
	}
}

func TestParseProcNameSpecDropsParamsAndFlagsDynamics(t *testing.T) {
	cases := []struct {
		name       string
		spec       string
		wantSchema string
		wantBase   string
		dyn        bool
	}{
		{
			name:       "strip placeholder tail",
			spec:       "[dbo].[UpdateControlTableResikoPasar] ?,?",
			wantSchema: "dbo",
			wantBase:   "UpdateControlTableResikoPasar",
			dyn:        true,
		},
		{
			name:       "dynamic schema placeholder",
			spec:       "[[schema]].sp_mk006_a(:1);",
			wantSchema: "[schema]",
			wantBase:   "sp_mk006_a",
			dyn:        true,
		},
	}

	for _, tt := range cases {
		t.Run(tt.name, func(t *testing.T) {
			tok := parseProcNameSpec(tt.spec)
			if tok.SchemaName != tt.wantSchema || tok.BaseName != tt.wantBase {
				t.Fatalf("unexpected parse result: got schema=%s base=%s", tok.SchemaName, tok.BaseName)
			}
			if tok.IsObjectNameDyn != tt.dyn {
				t.Fatalf("expected dynamic=%v, got %v", tt.dyn, tok.IsObjectNameDyn)
			}
			if strings.Contains(tok.FullName, "?") || strings.Contains(tok.FullName, ":") {
				t.Fatalf("parameter markers should be stripped from FullName, got %s", tok.FullName)
			}
		})
	}
}

func TestSplitObjectNamePartsCrossDbAndLinked(t *testing.T) {
	db, schema, base, linked := splitObjectNameParts("SQL_DBA..TempData")
	if db != "SQL_DBA" || schema != "dbo" || base != "TempData" {
		t.Fatalf("unexpected cross-db parse: %s %s %s", db, schema, base)
	}
	if linked {
		t.Fatalf("cross-db two-part should not be linked server")
	}

	db, schema, base, linked = splitObjectNameParts("SERVER1.SQL_DBA.dbo.TableZ")
	if !linked {
		t.Fatalf("expected linked server flag")
	}
	if db != "SQL_DBA" || schema != "dbo" || base != "TableZ" {
		t.Fatalf("unexpected linked parse: %s %s %s", db, schema, base)
	}
}

func TestFindAndClassifyObjectsHandlesTruncateAndCrossDb(t *testing.T) {
	sql := "TRUNCATE TABLE SQL_DBA..TempData; SELECT * FROM dbo.Users;"
	tokens := findObjectTokens(sql)
	if len(tokens) != 2 {
		t.Fatalf("expected 2 tokens, got %d", len(tokens))
	}

	cand := &SqlCandidate{SqlClean: strings.ToLower(sql), ConnDb: "SQL_DBB", LineStart: 42}
	classifyObjects(cand, "TRUNCATE", tokens)

	var truncateTok *ObjectToken
	var selectTok *ObjectToken
	for i := range tokens {
		tok := &tokens[i]
		switch tok.BaseName {
		case "TempData":
			truncateTok = tok
		case "Users":
			selectTok = tok
		}
	}

	if truncateTok == nil {
		t.Fatalf("expected truncate target token present")
	}
	if truncateTok.Role != "target" || truncateTok.DmlKind != "TRUNCATE" || !truncateTok.IsWrite {
		t.Fatalf("truncate token should be target, got %+v", *truncateTok)
	}
	if !truncateTok.IsCrossDb {
		t.Fatalf("expected cross-db target due to different ConnDb")
	}

	if selectTok == nil {
		t.Fatalf("expected select source token present")
	}
	if selectTok.Role != "source" || selectTok.DmlKind != "SELECT" || selectTok.IsWrite {
		t.Fatalf("select token should remain read-only source, got %+v", *selectTok)
	}
}

func TestClassifyObjectsFlagsDynamicPlaceholders(t *testing.T) {
	sql := "SELECT * FROM [[schema]].MK005_A WHERE id IN (?)"
	tokens := findObjectTokens(sql)
	cand := &SqlCandidate{SqlClean: strings.ToLower(sql), LineStart: 7}
	classifyObjects(cand, "SELECT", tokens)

	if len(tokens) != 1 {
		t.Fatalf("expected single token, got %d", len(tokens))
	}
	tok := tokens[0]
	if !tok.IsObjectNameDyn {
		t.Fatalf("expected dynamic flag for placeholder name")
	}
	if tok.Role != "source" || tok.DmlKind != "SELECT" || tok.IsWrite {
		t.Fatalf("expected select source, got %+v", tok)
	}
}

func TestStripSqlCommentsRemovesLeadingAndAllComments(t *testing.T) {
	withComments := "-- name: Query\nSELECT * FROM dbo.TableA -- trailing comment"
	cleaned := StripSqlComments(withComments)
	if strings.Contains(cleaned, "name: Query") || strings.Contains(cleaned, "trailing comment") {
		t.Fatalf("expected SQL comments stripped, got %q", cleaned)
	}
	if !strings.Contains(strings.ToLower(cleaned), "select") {
		t.Fatalf("expected select to remain after stripping, got %q", cleaned)
	}

	onlyComments := "-- just a comment\n/* block */"
	cleanedOnly := strings.TrimSpace(StripSqlComments(onlyComments))
	if cleanedOnly != "" {
		t.Fatalf("expected empty string when only comments remain, got %q", cleanedOnly)
	}
}
