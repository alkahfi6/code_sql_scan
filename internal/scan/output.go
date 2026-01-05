package scan

import (
	"encoding/csv"
	"fmt"
	"log"
	"os"
	"sort"
	"strings"

	summary "code_sql_scan/summary"
)

// ------------------------------------------------------------
// CSV output
// ------------------------------------------------------------

func writeCSVs(cfg *Config, cands []SqlCandidate) error {
	type objectRow struct {
		row       []string
		app       string
		rel       string
		funcName  string
		line      int
		queryHash string
		base      string
		role      string
		dml       string
		fullName  string
	}

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
	resolver := summary.NewFuncResolver(cfg.Root)

	qHeader := []string{
		"AppName", "RelPath", "File", "Func", "LineStart", "LineEnd", "RawSql", "SqlClean", "UsageKind", "IsWrite",
		"IsDynamic", "HasCrossDb", "DbList", "ObjectCount", "QueryHash", "SourceCategory", "SourceKind", "CallSiteKind",
		"DynamicSignature", "DynamicReason", "ConnName", "ConnDb", "RiskLevel", "DefinedInRelPath", "DefinedInLine",
	}
	if err := qw.Write(qHeader); err != nil {
		return err
	}

	oHeader := []string{
		"AppName", "RelPath", "File", "Func", "QueryHash", "FullObjectName", "DbName", "SchemaName", "BaseName", "Role",
		"DmlKind", "IsWrite", "IsCrossDb", "IsPseudoObject", "PseudoKind", "SourceCategory", "SourceKind", "Line",
		"IsLinkedServer", "IsObjectNameDynamic",
	}
	if err := ow.Write(oHeader); err != nil {
		return err
	}

	sort.Slice(cands, func(i, j int) bool {
		a, b := cands[i], cands[j]
		if a.AppName != b.AppName {
			return a.AppName < b.AppName
		}
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.Func != b.Func {
			return a.Func < b.Func
		}
		if a.LineStart != b.LineStart {
			return a.LineStart < b.LineStart
		}
		if a.LineEnd != b.LineEnd {
			return a.LineEnd < b.LineEnd
		}
		if a.UsageKind != b.UsageKind {
			return a.UsageKind < b.UsageKind
		}
		return a.QueryHash < b.QueryHash
	})

	pseudoWritten := make(map[string]struct{})
	var objectRows []objectRow

	for _, c := range cands {
		funcName := resolver.Resolve(c.Func, c.RelPath, c.File, c.LineStart)
		dbList := strings.Join(c.DbList, ";")
		dynSig := strings.TrimSpace(c.DynamicSignature)
		if dynSig == "" && c.IsDynamic {
			dynSig = fmt.Sprintf("%s@%d", c.RelPath, c.LineStart)
		}

		qRow := []string{
			c.AppName,
			c.RelPath,
			c.File,
			funcName,
			fmt.Sprintf("%d", c.LineStart),
			fmt.Sprintf("%d", c.LineEnd),
			c.RawSql,
			c.SqlClean,
			c.UsageKind,
			boolToStr(c.IsWrite),
			boolToStr(c.IsDynamic),
			boolToStr(c.HasCrossDb),
			dbList,
			fmt.Sprintf("%d", len(c.Objects)),
			c.QueryHash,
			c.SourceCat,
			c.SourceKind,
			c.CallSiteKind,
			dynSig,
			c.DynamicReason,
			c.ConnName,
			c.ConnDb,
			c.RiskLevel,
			c.DefinedPath,
			fmt.Sprintf("%d", c.DefinedLine),
		}
		if err := qw.Write(qRow); err != nil {
			return err
		}

		for _, o := range c.Objects {
			isPseudo := o.IsPseudoObject || strings.TrimSpace(o.PseudoKind) != ""
			if !isPseudo {
				if strings.TrimSpace(o.PseudoKind) != "" || isDynamicBaseName(o.BaseName) {
					isPseudo = true
				}
			}
			if o.IsPseudoObject && (o.PseudoKind == "dynamic-sql" || o.PseudoKind == "dynamic-object") {
				sig := dynSig
				if sig == "" {
					sig = fmt.Sprintf("%s@%d", c.RelPath, c.LineStart)
				}
				key := strings.Join([]string{c.AppName, c.RelPath, funcName, sig, o.PseudoKind, strings.TrimSpace(o.DmlKind)}, "|")
				if _, ok := pseudoWritten[key]; ok {
					continue
				}
				pseudoWritten[key] = struct{}{}
			}

			full := o.FullName
			if full == "" {
				full = buildFullName(o.DbName, o.SchemaName, o.BaseName)
			}
			pseudoKind := o.PseudoKind
			if isPseudo && strings.TrimSpace(pseudoKind) == "" {
				pseudoKind = "unknown"
			}
			role := o.Role
			if o.PseudoKind == "dynamic-sql" && strings.TrimSpace(role) == "" {
				role = "mixed"
			}
			fullName := full
			oRow := []string{
				c.AppName,
				c.RelPath,
				c.File,
				funcName,
				c.QueryHash,
				full,
				o.DbName,
				o.SchemaName,
				o.BaseName,
				role,
				o.DmlKind,
				boolToStr(o.IsWrite),
				boolToStr(o.IsCrossDb),
				boolToStr(isPseudo),
				pseudoKind,
				c.SourceCat,
				c.SourceKind,
				fmt.Sprintf("%d", o.RepresentativeLine),
				boolToStr(o.IsLinkedServer),
				boolToStr(o.IsObjectNameDyn),
			}
			objectRows = append(objectRows, objectRow{
				row:       oRow,
				app:       c.AppName,
				rel:       c.RelPath,
				funcName:  funcName,
				line:      o.RepresentativeLine,
				queryHash: c.QueryHash,
				base:      o.BaseName,
				role:      role,
				dml:       o.DmlKind,
				fullName:  fullName,
			})
		}
	}

	sort.Slice(objectRows, func(i, j int) bool {
		a, b := objectRows[i], objectRows[j]
		if a.app != b.app {
			return a.app < b.app
		}
		if a.rel != b.rel {
			return a.rel < b.rel
		}
		if a.funcName != b.funcName {
			return a.funcName < b.funcName
		}
		if a.line != b.line {
			return a.line < b.line
		}
		if a.queryHash != b.queryHash {
			return a.queryHash < b.queryHash
		}
		if a.base != b.base {
			return a.base < b.base
		}
		if a.role != b.role {
			return a.role < b.role
		}
		if a.dml != b.dml {
			return a.dml < b.dml
		}
		return a.fullName < b.fullName
	})

	for _, row := range objectRows {
		if err := ow.Write(row.row); err != nil {
			return err
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

func canonicalCallSite(kind string) string {
	trimmed := strings.TrimSpace(kind)
	if trimmed == "" {
		return ""
	}
	switch strings.ToLower(trimmed) {
	case "code", "inline", "raw-sql", "raw", "sql":
		return "code"
	case "config", "xml", "json", "yaml":
		return "config"
	case "script", "sql-file":
		return "script"
	case "go", "csharp":
		return strings.ToLower(trimmed)
	default:
		return strings.ToLower(trimmed)
	}
}

func canonicalCallSiteKindLocal(kind string) string {
	trimmed := strings.TrimSpace(kind)
	if trimmed == "" {
		return ""
	}
	switch strings.ToLower(trimmed) {
	case "execproc", "exec-proc", "exec":
		return "ExecProc"
	case "commandtext", "command-text":
		return "CommandText"
	case "sqlcommand", "sql-command":
		return "SqlCommand"
	default:
		return trimmed
	}
}

func dynamicSummarySignatureLocal(q summary.QueryRow) string {
	call := canonicalCallSiteKindLocal(q.CallSite)
	if call == "" {
		call = canonicalCallSiteKindLocal(q.SourceKind)
	}
	if call == "" {
		call = canonicalCallSiteKindLocal(q.SourceCat)
	}
	if call == "" {
		call = canonicalCallSiteKindLocal(q.UsageKind)
	}
	if call == "" {
		call = "unknown"
	}
	return fmt.Sprintf("%s|%s|%s", strings.TrimSpace(q.RelPath), strings.TrimSpace(q.Func), call)
}

func validateSummary(cfg *Config, queries []summary.QueryRow, objects []summary.ObjectRow, funcSummary []summary.FunctionSummaryRow, objectSummary []summary.ObjectSummaryRow) error {
	validateFuncs := len(funcSummary) > 0
	validateObjects := len(objectSummary) > 0
	if !validateFuncs && !validateObjects {
		return nil
	}

	resolver := summary.NewFuncResolver(cfg.Root)

	var dedupQueries []summary.QueryRow
	dynSeen := make(map[string]struct{})
	for _, q := range queries {
		if q.IsDynamic {
			sig := dynamicSummarySignatureLocal(q)
			if _, ok := dynSeen[sig]; ok {
				continue
			}
			dynSeen[sig] = struct{}{}
		}
		dedupQueries = append(dedupQueries, q)
	}

	type funcCounts struct {
		total    int
		selectQ  int
		insert   int
		update   int
		deleteQ  int
		truncate int
		exec     int
		write    int
		dynamic  int
		dynRaw   int
	}

	var errors []string
	if validateFuncs {
		rawDynByFunc := make(map[string]int)
		for _, q := range queries {
			fn := resolver.Resolve(q.Func, q.RelPath, q.File, q.LineStart)
			key := strings.Join([]string{q.AppName, q.RelPath, fn}, "|")
			if q.IsDynamic {
				rawDynByFunc[key]++
			}
		}

		queryCounts := make(map[string]funcCounts)
		for _, q := range dedupQueries {
			fn := resolver.Resolve(q.Func, q.RelPath, q.File, q.LineStart)
			key := strings.Join([]string{q.AppName, q.RelPath, fn}, "|")
			counts := queryCounts[key]

			switch strings.ToUpper(strings.TrimSpace(q.UsageKind)) {
			case "SELECT":
				counts.selectQ++
			case "INSERT":
				counts.insert++
			case "UPDATE":
				counts.update++
			case "DELETE":
				counts.deleteQ++
			case "TRUNCATE":
				counts.truncate++
			case "EXEC":
				counts.exec++
			}

			if q.IsWrite {
				counts.write++
			}
			if q.IsDynamic {
				counts.dynamic++
				counts.dynRaw = rawDynByFunc[key]
			}
			counts.total++
			queryCounts[key] = counts
		}

		funcSummaryMap := make(map[string]summary.FunctionSummaryRow)
		for _, row := range funcSummary {
			key := strings.Join([]string{row.AppName, row.RelPath, row.Func}, "|")
			funcSummaryMap[key] = row
		}

		for key, expected := range queryCounts {
			sum, ok := funcSummaryMap[key]
			if !ok {
				parts := strings.SplitN(key, "|", 3)
				errors = append(errors, fmt.Sprintf("function %s/%s missing in summary (expected total=%d write=%d dynamic=%d)", parts[1], parts[2], expected.total, expected.write, expected.dynamic))
				continue
			}
			rawDyn := sum.DynamicRawCount
			if rawDyn == 0 {
				rawDyn = sum.DynamicCount
			}
			if sum.TotalQueries != expected.total || sum.TotalSelect != expected.selectQ || sum.TotalInsert != expected.insert || sum.TotalUpdate != expected.update || sum.TotalDelete != expected.deleteQ || sum.TotalTruncate != expected.truncate || sum.TotalExec != expected.exec || sum.TotalWrite != expected.write || sum.TotalDynamic != expected.dynamic || (rawDyn != 0 && rawDyn != expected.dynRaw) {
				errors = append(errors, fmt.Sprintf(
					"function %s/%s count mismatch (expected total=%d select=%d insert=%d update=%d delete=%d truncate=%d exec=%d write=%d dynamic=%d rawDynamic=%d, summary total=%d select=%d insert=%d update=%d delete=%d truncate=%d exec=%d write=%d dynamic=%d rawDynamic=%d)",
					sum.RelPath,
					sum.Func,
					expected.total, expected.selectQ, expected.insert, expected.update, expected.deleteQ, expected.truncate, expected.exec, expected.write, expected.dynamic, expected.dynRaw,
					sum.TotalQueries, sum.TotalSelect, sum.TotalInsert, sum.TotalUpdate, sum.TotalDelete, sum.TotalTruncate, sum.TotalExec, sum.TotalWrite, sum.TotalDynamic, rawDyn,
				))
			}
		}

		for key, sum := range funcSummaryMap {
			if _, ok := queryCounts[key]; ok {
				continue
			}
			errors = append(errors, fmt.Sprintf("function %s/%s appears only in summary (total=%d)", sum.RelPath, sum.Func, sum.TotalQueries))
		}
	}

	if len(objectSummary) > 0 {
		objectCounts := aggregateObjectCounts(queries, objects)
		objectSummaryMap := make(map[string]summary.ObjectSummaryRow)
		for _, row := range objectSummary {
			key := summary.ObjectSummaryRowKey(row)
			objectSummaryMap[key] = row
		}

		for key, expected := range objectCounts {
			sum, ok := objectSummaryMap[key]
			parts := strings.Split(key, "|")
			rel := ""
			full := ""
			if len(parts) >= 2 {
				rel = parts[1]
			}
			if len(parts) >= 3 {
				full = parts[2]
			}
			if !ok {
				errors = append(errors, fmt.Sprintf("object %s/%s missing in summary (expected reads=%d writes=%d exec=%d)", rel, full, expected.Reads, expected.Writes, expected.Execs))
				continue
			}
			if sum.TotalReads != expected.Reads || sum.TotalWrites != expected.Writes || sum.TotalExec != expected.Execs {
				errors = append(errors, fmt.Sprintf(
					"object %s/%s count mismatch (expected reads=%d writes=%d exec=%d, summary reads=%d writes=%d exec=%d)",
					sum.RelPath,
					sum.FullObjectName,
					expected.Reads, expected.Writes, expected.Execs,
					sum.TotalReads, sum.TotalWrites, sum.TotalExec,
				))
			}
		}

		for key, sum := range objectSummaryMap {
			if _, ok := objectCounts[key]; ok {
				continue
			}
			errors = append(errors, fmt.Sprintf("object %s/%s appears only in summary (reads=%d writes=%d exec=%d)", sum.RelPath, sum.FullObjectName, sum.TotalReads, sum.TotalWrites, sum.TotalExec))
		}
	}

	if len(errors) == 0 {
		return nil
	}

	for _, msg := range errors {
		fmt.Fprintf(os.Stderr, "[ERROR] %s\n", msg)
	}

	if len(errors) > 5 {
		errors = errors[:5]
	}
	return fmt.Errorf("summary validation failed: %s", strings.Join(errors, "; "))
}

type objectCount struct {
	Reads  int
	Writes int
	Execs  int
}

func aggregateObjectCounts(queries []summary.QueryRow, objects []summary.ObjectRow) map[string]objectCount {
	queryByKey := make(map[string]summary.QueryRow)
	for _, q := range queries {
		key := strings.Join([]string{q.AppName, q.RelPath, q.File, q.QueryHash}, "|")
		queryByKey[key] = q
	}

	counts := make(map[string]objectCount)
	for _, o := range summary.NormalizeObjectRows(objects) {
		if shouldSkipObjectForValidation(o) {
			continue
		}
		key := summary.ObjectSummaryGroupKey(o)
		agg := counts[key]
		qRow, hasQuery := queryByKey[strings.Join([]string{o.AppName, o.RelPath, o.File, o.QueryHash}, "|")]
		r, w, e := summary.ObjectRoleCounts(o, qRow, hasQuery)
		agg.Reads += r
		agg.Writes += w
		agg.Execs += e
		counts[key] = agg
	}

	return counts
}

type roleFlags struct {
	read  bool
	write bool
	exec  bool
}

func classifyObjectRole(o summary.ObjectRow, q summary.QueryRow, hasQuery bool) roleFlags {
	if !isDynamicBaseName(strings.TrimSpace(o.BaseName)) {
		return classifyRoles(o)
	}

	usage := strings.ToUpper(strings.TrimSpace(q.UsageKind))
	switch usage {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE":
		return roleFlags{write: true}
	case "EXEC":
		return roleFlags{exec: true}
	case "SELECT":
		return roleFlags{read: true}
	default:
		if q.IsWrite || (!hasQuery && o.IsWrite) {
			return roleFlags{write: true}
		}
		return roleFlags{read: true}
	}
}

func normalizeRoleFlags(flags roleFlags) roleFlags {
	if flags.exec {
		return roleFlags{exec: true}
	}
	if flags.write {
		return roleFlags{write: true}
	}
	if flags.read {
		return roleFlags{read: true}
	}
	return roleFlags{read: true}
}

func classifyRoles(o summary.ObjectRow) roleFlags {
	role := strings.ToLower(strings.TrimSpace(o.Role))
	upperDml := strings.ToUpper(strings.TrimSpace(o.DmlKind))
	parts := strings.Split(o.DmlKind, ";")
	hasExec := role == "exec"
	hasWrite := o.IsWrite || role == "target"
	for _, p := range parts {
		upper := strings.ToUpper(strings.TrimSpace(p))
		if upper == "EXEC" {
			hasExec = true
		}
		if isWriteDml(upper) {
			hasWrite = true
		}
	}

	isExec := hasExec || upperDml == "EXEC"
	isWrite := (!isExec && hasWrite)
	isRead := role == "source" || (!isExec && !isWrite && upperDml == "SELECT")

	return roleFlags{
		read:  isRead,
		write: isWrite,
		exec:  isExec,
	}
}

func isWriteDml(dml string) bool {
	switch strings.ToUpper(strings.TrimSpace(dml)) {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE":
		return true
	default:
		return false
	}
}

func isDynamicBaseName(base string) bool {
	trimmed := strings.ToLower(strings.TrimSpace(base))
	if trimmed == "<dynamic-sql>" {
		return true
	}
	return strings.HasPrefix(trimmed, "<dynamic-object")
}

func shouldSkipObjectForValidation(o summary.ObjectRow) bool {
	base := strings.TrimSpace(o.BaseName)
	if isDynamicBaseName(base) {
		return false
	}
	if base == "" {
		return true
	}
	lower := strings.ToLower(base)
	if lower == "eq" || lower == "dbo" || lower == "dbo." {
		return true
	}
	if strings.HasSuffix(base, ".") {
		return true
	}
	return false
}

func generateSummaries(cfg *Config) error {
	if cfg.OutSummaryFunc == "" && cfg.OutSummaryObject == "" && cfg.OutSummaryForm == "" {
		return nil
	}

	summary.SetSourceRoot(cfg.Root)

	queries, err := summary.LoadQueryUsage(cfg.OutQuery)
	if err != nil {
		return fmt.Errorf("load query usage: %w", err)
	}
	objects, err := summary.LoadObjectUsage(cfg.OutObject)
	if err != nil {
		return fmt.Errorf("load object usage: %w", err)
	}

	var funcSummaryRows []summary.FunctionSummaryRow
	if cfg.OutSummaryFunc != "" {
		funcSummaryRows, err = summary.BuildFunctionSummary(queries, objects)
		if err != nil {
			return fmt.Errorf("build function summary: %w", err)
		}
		if err := summary.ValidateFunctionSummaryCounts(queries, funcSummaryRows); err != nil {
			return fmt.Errorf("function summary validation: %w", err)
		}
	}

	var objectSummaryRows []summary.ObjectSummaryRow
	if cfg.OutSummaryObject != "" {
		objectSummaryRows, err = summary.BuildObjectSummary(queries, objects)
		if err != nil {
			return fmt.Errorf("build object summary: %w", err)
		}
	}

	if err := validateSummary(cfg, queries, objects, funcSummaryRows, objectSummaryRows); err != nil {
		return err
	}

	if cfg.OutSummaryFunc != "" {
		if err := summary.WriteFunctionSummary(cfg.OutSummaryFunc, funcSummaryRows); err != nil {
			return fmt.Errorf("write function summary: %w", err)
		}
	}

	if cfg.OutSummaryObject != "" {
		if err := summary.WriteObjectSummary(cfg.OutSummaryObject, objectSummaryRows); err != nil {
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
			log.Printf("[ERROR] summary consistency mismatches=%d", report.TotalMismatches())
			for i, ex := range examples {
				log.Printf("[ERROR] mismatch #%d: %s", i+1, ex)
			}
			return fmt.Errorf("SUMMARY CONSISTENCY FAIL (%d mismatches). Examples: %s", report.TotalMismatches(), strings.Join(examples, "; "))
		}
		log.Printf("[INFO] summary consistency check passed")
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
