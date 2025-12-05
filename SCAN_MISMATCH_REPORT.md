# Scan Validation Report

## Run Summary
- Commands executed:
  - `go run main.go -root "golang" -app "golang-sample" -lang "go" -out-query "out_query_golang.csv" -out-object "out_object_golang.csv"`
  - `go run main.go -root "dotnet_check" -app "dotnet-sample" -lang "dotnet" -out-query "out_query_dotnet.csv" -out-object "out_object_dotnet.csv"`
- Line counts (including header):
  - Go: `out_query_golang.csv` = 2,294 rows; `out_object_golang.csv` = 149 rows.
  - .NET: `out_query_dotnet.csv` = 8,643 rows; `out_object_dotnet.csv` = 425 rows.

## Detected Mismatches
- No outstanding contract violations found after normalizing repository-relative paths/hashes and stripping commented C# SQL blocks.
  - Stored procedure `ExecBASelectRefundBilling` now reports `RelPath`/`DefinedInRelPath` as `go3/refundBillingQueries_manual.sql.go` with a hash derived solely from `SqlClean` (`exec BA_SelectRefundBilling @p1`).【F:out_query_golang.csv†L2289-L2294】
  - The commented-out multi-statement insert/select in `clsValasService.InsertValasResultHistory` is absent from the latest .NET scan output (confirmed via search for `InsertValasResultHistory`).【a89b0c†L1-L1】
