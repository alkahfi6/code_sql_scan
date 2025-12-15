package scan

import (
	"os"
	"strings"
	"testing"
)

func TestMain(m *testing.M) {
	initRegexes()
	os.Exit(m.Run())
}

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

func TestAnalyzeCandidateExecStubAndDynamicCases(t *testing.T) {
	tests := []struct {
		name          string
		cand          SqlCandidate
		wantUsage     string
		wantObjCount  int
		wantBaseNames []string
		wantDynFlags  []bool
		wantCrossDb   []bool
	}{
		{
			name: "exec stub literal dbo schema",
			cand: SqlCandidate{
				RawSql:     "dbo.SBNSchedGetGeneralParam",
				IsExecStub: true,
				ConnDb:     "SQL_APP",
				RelPath:    "clsDataAccess1.cs",
				Func:       "GetGeneralParam",
				LineStart:  12,
			},
			wantUsage:     "EXEC",
			wantObjCount:  1,
			wantBaseNames: []string{"SBNSchedGetGeneralParam"},
			wantDynFlags:  []bool{false},
			wantCrossDb:   []bool{false},
		},
		{
			name: "exec stub cross-db spec",
			cand: SqlCandidate{
				RawSql:     "SQL_DBA.dbo.OMValidateCustomer",
				IsExecStub: true,
				ConnDb:     "SQL_DBB",
				RelPath:    "clsDataAccess2.cs",
				Func:       "ValidateCustomer",
				LineStart:  20,
			},
			wantUsage:     "EXEC",
			wantObjCount:  1,
			wantBaseNames: []string{"OMValidateCustomer"},
			wantDynFlags:  []bool{false},
			wantCrossDb:   []bool{true},
		},
		{
			name: "dynamic schema placeholder query",
			cand: SqlCandidate{
				RawSql:    "SELECT * FROM [[schema]].MK005_A WHERE TRIM(SECID) IN ([[param]])",
				IsDynamic: true,
				ConnDb:    "SQL_APP",
				RelPath:   "query.go",
				Func:      "GetMK006AConfo",
				LineStart: 33,
			},
			wantUsage:    "SELECT",
			wantObjCount: 1,
			wantDynFlags: []bool{true},
			wantCrossDb:  []bool{false},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			analyzeCandidate(&tt.cand)
			if tt.cand.UsageKind != tt.wantUsage {
				t.Fatalf("unexpected usage kind: got %s want %s", tt.cand.UsageKind, tt.wantUsage)
			}
			if len(tt.cand.Objects) != tt.wantObjCount {
				t.Fatalf("expected %d objects, got %d", tt.wantObjCount, len(tt.cand.Objects))
			}
			for i, obj := range tt.cand.Objects {
				if i < len(tt.wantBaseNames) && obj.BaseName != tt.wantBaseNames[i] {
					t.Fatalf("object %d base name mismatch: got %s want %s", i, obj.BaseName, tt.wantBaseNames[i])
				}
				if i < len(tt.wantDynFlags) && obj.IsObjectNameDyn != tt.wantDynFlags[i] {
					t.Fatalf("object %d dynamic flag mismatch: got %v want %v", i, obj.IsObjectNameDyn, tt.wantDynFlags[i])
				}
				if i < len(tt.wantCrossDb) && obj.IsCrossDb != tt.wantCrossDb[i] {
					t.Fatalf("object %d cross-db flag mismatch: got %v want %v", i, obj.IsCrossDb, tt.wantCrossDb[i])
				}
			}
			if tt.wantUsage == "EXEC" && !tt.cand.IsWrite {
				t.Fatalf("exec stub should be treated as write")
			}
		})
	}
}

func TestIsProcNameSpecAllowsParamsSuffix(t *testing.T) {
	cases := []struct {
		name     string
		input    string
		expected bool
	}{
		{
			name:     "proc name with positional params",
			input:    "[dbo].[UpdateControlTableResikoPasar] ?,?",
			expected: true,
		},
		{
			name:     "proc name containing keyword but not leading",
			input:    "dbo.ProcessUpdateData",
			expected: true,
		},
		{
			name:     "proc name with named params",
			input:    "dbo.ProcessData @p1, @p2",
			expected: true,
		},
		{
			name:     "non-proc due to select keyword",
			input:    "select * from dbo.TableA",
			expected: false,
		},
		{
			name:     "non-proc due to exec keyword",
			input:    "EXEC dbo.SomeProc @p1",
			expected: false,
		},
		{
			name:     "non-proc due to cte",
			input:    "WITH cte AS (SELECT 1) SELECT * FROM cte",
			expected: false,
		},
	}

	for _, tt := range cases {
		t.Run(tt.name, func(t *testing.T) {
			if got := isProcNameSpec(tt.input); got != tt.expected {
				t.Fatalf("expected %v, got %v", tt.expected, got)
			}
		})
	}
}

func TestAnalyzeCandidateMultiStatementAndCommentsOnly(t *testing.T) {
	t.Run("multi-statement with cross-db join", func(t *testing.T) {
		sql := `-- name: inquiry
DECLARE @x INT; SET @x = 1;
IF @x = 1 BEGIN
    SELECT c.Id, a.AccountNo FROM dbo.Customers c JOIN SQL_DBA..Accounts a ON a.Id = c.AccountId
END`
		cand := &SqlCandidate{RawSql: sql, ConnDb: "SQL_APP", RelPath: "clsAPIServiceNTI.cs", Func: "Inquiry", LineStart: 101}
		analyzeCandidate(cand)
		if cand.UsageKind != "SELECT" {
			t.Fatalf("expected SELECT usage, got %s", cand.UsageKind)
		}
		if !cand.HasCrossDb {
			t.Fatalf("expected cross-db detection from SQL_DBA..Accounts")
		}
		if len(cand.Objects) != 2 {
			t.Fatalf("expected two objects (customers + accounts), got %d", len(cand.Objects))
		}
		var seenCustomers, seenAccounts bool
		for _, obj := range cand.Objects {
			switch obj.BaseName {
			case "Customers":
				seenCustomers = true
				if obj.IsCrossDb {
					t.Fatalf("dbo.Customers should not be cross-db")
				}
			case "Accounts":
				seenAccounts = true
				if !obj.IsCrossDb {
					t.Fatalf("SQL_DBA..Accounts should be cross-db")
				}
			}
			if obj.DmlKind != "SELECT" || obj.Role != "source" {
				t.Fatalf("expected source SELECT for %s, got %+v", obj.BaseName, obj)
			}
		}
		if !seenCustomers || !seenAccounts {
			t.Fatalf("expected both Customers and Accounts tokens to be present")
		}
	})

	t.Run("comments only are ignored", func(t *testing.T) {
		cand := &SqlCandidate{RawSql: "-- just a comment\n/* nothing */", RelPath: "query.sql", Func: "noop", LineStart: 5}
		analyzeCandidate(cand)
		if cand.SqlClean != "" {
			t.Fatalf("expected cleaned SQL to be empty, got %q", cand.SqlClean)
		}
		if cand.UsageKind != "UNKNOWN" {
			t.Fatalf("expected UNKNOWN usage for comment-only SQL, got %s", cand.UsageKind)
		}
		if len(cand.Objects) != 0 {
			t.Fatalf("expected no objects for comment-only SQL, got %d", len(cand.Objects))
		}
		if cand.QueryHash == "" {
			t.Fatalf("expected query hash to still be populated")
		}
	})
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
