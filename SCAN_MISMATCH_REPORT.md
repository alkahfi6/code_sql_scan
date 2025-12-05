# Scan Validation Report

## Run Summary
- Commands executed:
  - `go run main.go -root "golang" -app "golang-sample" -lang "go" -out-query "out_query_golang.csv" -out-object "out_object_golang.csv"`
  - `go run main.go -root "dotnet_check" -app "dotnet-sample" -lang "dotnet" -out-query "out_query_dotnet.csv" -out-object "out_object_dotnet.csv"`
- Line counts (including header):
  - Go: `out_query_golang.csv` = 2,294 rows; `out_object_golang.csv` = 149 rows.
  - .NET: `out_query_dotnet.csv` = 8,643 rows; `out_object_dotnet.csv` = 425 rows.

## Detected Mismatches (relative to sample sources and `new_*` reference CSVs)

### Go
- **Query missing**: `ExecBASelectRefundBilling` from `go3/refundBillingQueries_manual.sql.go` (`exec BA_SelectRefundBilling @p1`) exists in code but its hash (`09e3f5e7...`) is absent from the fresh `out_query_golang.csv` even though it is present in `new_golang_query.csv`. The scanner skipped this stored-procedure call entirely.【F:new_golang_query.csv†L2288-L2292】【F:out_query_golang.csv†L2289-L2294】
- **Over-scanned extras** (24 hashes not in the reference): mostly the inline SQL helpers in `go1/destination.go` and the multi-statement block in `go1/query.go` are emitted as separate rows (DELETE/INSERT/SELECT variants, plus Actuate staging insert). These queries were not intended to be expanded individually in the curated output and inflate both QueryUsage and ObjectUsage counts.【F:out_query_golang.csv†L20-L69】
- **SP rewriting side effect**: Stored procedures defined without the `EXEC` prefix (e.g., `UpdateControlTableResikoPasar` in `go1/sp.go`) are rewritten with `EXEC` in `SqlClean`, changing hashes and creating duplicate object rows compared to the reference expectations.【F:out_query_golang.csv†L3-L8】

### .NET
- No missing hashes or classification differences versus `new_dotnet_query.csv`/`new_dotnet_object.csv`. Query hashes are fully aligned (set comparison shows zero deltas), and spot checks on EXEC/TRUNCATE rows match the code samples.【F:out_query_dotnet.csv†L1-L5】

## Notes
- File discovery matched all code/config files under the provided roots (12 Go files; 10 .NET files).
- The Go discrepancies above warrant parser/heuristic adjustments; .NET results appear stable for the current samples.
