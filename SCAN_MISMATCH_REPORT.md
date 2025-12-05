# Scan Validation Report

## Run Summary
- Commands executed:
  - `go run main.go -root "/workspace/code_sql_scan/golang" -app "golang-sample" -lang "go" -out-query "out_query_golang.csv" -out-object "out_object_golang.csv"` (Go sample)
  - `go run main.go -root "/workspace/code_sql_scan/dotnet_check" -app "dotnet-sample" -lang "dotnet" -out-query "out_query_dotnet.csv" -out-object "out_object_dotnet.csv"`
- Line counts:
  - Go: `out_query_golang.csv` = 2,260 rows; `out_object_golang.csv` = 135 rows.
  - .NET: `out_query_dotnet.csv` = 8,643 rows; `out_object_dotnet.csv` = 433 rows.

## Detected Mismatches

### Go
1. **UsageKind misclassified as UNKNOWN in dynamic dispatch**
   - Function `DeleteDuplicateConfo` chooses between two static DELETE statements (`QueryDeleteDuplicateDataConfoMK005/MK006`). The emitted row shows `UsageKind=UNKNOWN` and `IsDynamic=true` even though both branches are DELETE operations. This under-reports write activity and hides the target tables.
   - Evidence: function logic at `golang/go1/destination.go` lines 32–45 defines the delete queries before calling `ExecNonQuery`; CSV row 4 shows `UsageKind=UNKNOWN` and `ObjectCount=0` for that function.

2. **Stored-procedure definitions rewritten with implicit `EXEC` prefix and absolute `DefinedInRelPath`**
   - Constants such as `SPUpdateControlTableResikoPasar` in `golang/go1/sp.go` hold only the procedure name, but `SqlClean` in `out_query_golang.csv` prepends `EXEC`, changing `QueryHash` from the reference and duplicating the object entry for `ControlTableResikoPasar`. The `DefinedInRelPath` column also embeds the absolute workspace path (`go1/workspace/code_sql_scan/...`) instead of a repository-relative path.
   - Evidence: procedure constant at `golang/go1/sp.go` line 3; CSV row 2 shows `SqlClean="[dbo].[UpdateControlTableResikoPasar] ?,?"` but `DefinedInRelPath=go1/workspace/code_sql_scan/golang/go1/sp.go`.

3. **Extra INSERT/DELETE hashes for multi-line constants inflate row counts**
   - `golang/go1/query.go` contains multi-line INSERT blocks for `SQL_REPLICATE.dbo.*`. The scanner emits these as additional entries not present in the curated `new_golang_*` outputs, driving Go row counts far above the ground truth and suggesting inconsistent normalization.
   - Evidence: constants at `golang/go1/query.go` lines 33–75 and 77–79; corresponding new `QueryHash` values (`7713fe8d…`, `a7bca9c…`, `51bb2133…`) appear only in `out_query_golang.csv`.

### .NET
- No content mismatches found when comparing hashes to `new_dotnet_*`; counts align after de-duplication. Spot checks on EXEC/TRUNCATE classifications in `dotnet_check` match expectations.

## Suggested Fixes
- Treat branch-selected constants as static SQL when all alternatives are literal strings so `UsageKind` and objects can be derived (applies to `DeleteDuplicateConfo`).
- Avoid mutating procedure strings by auto-prepending `EXEC`; use the literal text to keep hashes stable and object names unduplicated. Normalize `DefinedInRelPath` to repository-relative paths.
- Normalize multi-line SQL constants (e.g., collapse whitespace) consistently with reference outputs to prevent extra hashes.
