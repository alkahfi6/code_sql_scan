package main

import "testing"

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
