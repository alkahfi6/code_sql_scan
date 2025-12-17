package scan

import (
	"encoding/csv"
	"fmt"
	"os"
	"strings"

	summary "code_sql_scan/summary"
)

// ------------------------------------------------------------
// CSV output
// ------------------------------------------------------------

func writeCSVs(cfg *Config, cands []SqlCandidate) error {
	qf, err := os.Create(cfg.OutQuery)
	if err != nil {
		return err
	}
	defer qf.Close()
	of, err := os.Create(cfg.OutObject)
	if err != nil {
		return err
	}
	defer of.Close()

	qw := csv.NewWriter(qf)
	ow := csv.NewWriter(of)

	qHeader := []string{
		"AppName", "RelPath", "File", "SourceCategory", "SourceKind", "CallSiteKind",
		"LineStart", "LineEnd", "Func", "RawSql", "SqlClean",
		"UsageKind", "IsWrite", "HasCrossDb", "DbList", "ObjectCount",
		"IsDynamic", "ConnName", "ConnDb", "QueryHash", "RiskLevel",
		"DefinedInRelPath", "DefinedInLine",
	}
	if err := qw.Write(qHeader); err != nil {
		return err
	}

	oHeader := []string{
		"AppName", "RelPath", "File", "SourceCategory", "SourceKind",
		"Line", "Func", "QueryHash", "ObjectName",
		"DbName", "SchemaName", "BaseName",
		"IsCrossDb", "IsLinkedServer", "Role", "DmlKind",
		"IsWrite", "IsObjectNameDynamic", "IsPseudoObject", "PseudoKind",
	}
	if err := ow.Write(oHeader); err != nil {
		return err
	}

	for _, c := range cands {
		dbList := strings.Join(c.DbList, ";")

		qRow := []string{
			c.AppName,
			c.RelPath,
			c.File,
			c.SourceCat,
			c.SourceKind,
			c.CallSiteKind,
			fmt.Sprintf("%d", c.LineStart),
			fmt.Sprintf("%d", c.LineEnd),
			c.Func,
			c.RawSql,
			c.SqlClean,
			c.UsageKind,
			boolToStr(c.IsWrite),
			boolToStr(c.HasCrossDb),
			dbList,
			fmt.Sprintf("%d", len(c.Objects)),
			boolToStr(c.IsDynamic),
			c.ConnName,
			c.ConnDb,
			c.QueryHash,
			c.RiskLevel,
			c.DefinedPath,
			fmt.Sprintf("%d", c.DefinedLine),
		}
		if err := qw.Write(qRow); err != nil {
			return err
		}

		for _, o := range c.Objects {
			full := o.FullName
			if full == "" {
				full = buildFullName(o.DbName, o.SchemaName, o.BaseName)
			}
			pseudoKind := o.PseudoKind
			if o.IsPseudoObject && strings.TrimSpace(pseudoKind) == "" {
				pseudoKind = "unknown"
			}
			oRow := []string{
				c.AppName,
				c.RelPath,
				c.File,
				c.SourceCat,
				c.SourceKind,
				fmt.Sprintf("%d", o.RepresentativeLine),
				c.Func,
				c.QueryHash,
				full,
				o.DbName,
				o.SchemaName,
				o.BaseName,
				boolToStr(o.IsCrossDb),
				boolToStr(o.IsLinkedServer),
				o.Role,
				o.DmlKind,
				boolToStr(o.IsWrite),
				boolToStr(o.IsObjectNameDyn),
				boolToStr(o.IsPseudoObject),
				pseudoKind,
			}
			if err := ow.Write(oRow); err != nil {
				return err
			}
		}
	}

	qw.Flush()
	ow.Flush()
	if err := qw.Error(); err != nil {
		return err
	}
	if err := ow.Error(); err != nil {
		return err
	}

	return nil
}

func buildFullName(db, schema, base string) string {
	var parts []string
	if db != "" {
		parts = append(parts, db)
	}
	if schema != "" {
		parts = append(parts, schema)
	}
	if base != "" {
		parts = append(parts, base)
	}
	return strings.Join(parts, ".")
}

func boolToStr(b bool) string {
	if b {
		return "true"
	}
	return "false"
}

func generateSummaries(cfg *Config) error {
	if cfg.OutSummaryFunc == "" && cfg.OutSummaryObject == "" && cfg.OutSummaryForm == "" {
		return nil
	}

	queries, err := summary.LoadQueryUsage(cfg.OutQuery)
	if err != nil {
		return fmt.Errorf("load query usage: %w", err)
	}
	objects, err := summary.LoadObjectUsage(cfg.OutObject)
	if err != nil {
		return fmt.Errorf("load object usage: %w", err)
	}

	if cfg.OutSummaryFunc != "" {
		rows, err := summary.BuildFunctionSummary(queries, objects)
		if err != nil {
			return fmt.Errorf("build function summary: %w", err)
		}
		if err := summary.WriteFunctionSummary(cfg.OutSummaryFunc, rows); err != nil {
			return fmt.Errorf("write function summary: %w", err)
		}
	}

	if cfg.OutSummaryObject != "" {
		rows, err := summary.BuildObjectSummary(queries, objects)
		if err != nil {
			return fmt.Errorf("build object summary: %w", err)
		}
		if err := summary.WriteObjectSummary(cfg.OutSummaryObject, rows); err != nil {
			return fmt.Errorf("write object summary: %w", err)
		}
	}

	if cfg.OutSummaryForm != "" {
		rows, err := summary.BuildFormSummary(queries, objects)
		if err != nil {
			return fmt.Errorf("build form summary: %w", err)
		}
		if err := summary.WriteFormSummary(cfg.OutSummaryForm, rows); err != nil {
			return fmt.Errorf("write form summary: %w", err)
		}
	}

	if cfg.OutSummaryFunc != "" && cfg.OutSummaryObject != "" {
		report, err := summary.VerifyConsistency(cfg.OutQuery, cfg.OutObject, cfg.OutSummaryFunc, cfg.OutSummaryObject)
		if err != nil {
			return fmt.Errorf("summary consistency: %w", err)
		}
		if report != nil && report.TotalMismatches() > 0 {
			examples := report.Examples(3)
			return fmt.Errorf("SUMMARY CONSISTENCY FAIL (%d mismatches). Examples: %s", report.TotalMismatches(), strings.Join(examples, "; "))
		}
	}

	return nil
}

// looksLikeSQL heuristically checks if a string resembles an SQL statement.
// It searches for common DML keywords like select, insert, update, delete, truncate, or exec.
// A simple lower-case search is performed and only returns true if at least one keyword is found.
func looksLikeSQL(s string) bool {
	norm := strings.ToLower(StripSqlComments(strings.TrimSpace(s)))
	norm = strings.Join(strings.Fields(norm), " ")
	if norm == "" {
		return false
	}
	keywords := []string{"select", "insert", "update", "delete", "truncate", "exec", "execute"}
	for _, kw := range keywords {
		if strings.HasPrefix(norm, kw) || strings.Contains(norm, kw+" ") {
			return true
		}
	}
	return false
}
