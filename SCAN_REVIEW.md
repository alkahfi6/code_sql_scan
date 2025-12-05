# Scanner Output Review

This document summarizes a manual check of the latest scanner outputs for the Go and .NET sample projects.

- Scanner invocation (fresh run):
  - `go run main.go -root "golang" -app "golang-sample" -lang "go" -out-query "out_query_golang.csv" -out-object "out_object_golang.csv"`
  - `go run main.go -root "dotnet_check" -app "dotnet-sample" -lang "dotnet" -out-query "out_query_dotnet.csv" -out-object "out_object_dotnet.csv"`
- File discovery reported `12` files in `golang` and `10` files in `dotnet_check` (all SQL-bearing code/config covered by the allowed extensions).

## Go (`golang`)
- Fresh run counts: `out_query_golang.csv` = **2,294 rows**, `out_object_golang.csv` = **149 rows**.
- Reference counts (`new_*`): `new_golang_query.csv` = 1,586 rows, `new_golang_object.csv` = 115 rows.
- Diff against reference hashes:
  - **Missing**: 1 hash corresponding to `ExecBASelectRefundBilling` (`go3/refundBillingQueries_manual.sql.go`) is present in the reference but absent from the fresh run.
  - **Extra**: 24 hashes emitted from inline SQL helpers in `go1/destination.go` and the long multi-statement block in `go1/query.go` (DELETE/INSERT/SELECT variants and Actuate staging insert) that were not meant to be split out in the curated output.
- Behavioral observations:
  - Stored procedures without an explicit `EXEC` prefix (e.g., `UpdateControlTableResikoPasar` in `go1/sp.go`) are rewritten with `EXEC` in `SqlClean`, altering hashes and duplicating object rows versus the reference.

## .NET (`dotnet_check`)
- Fresh run counts: `out_query_dotnet.csv` = **8,643 rows**, `out_object_dotnet.csv` = **425 rows**.
- Reference counts: `new_dotnet_query.csv` = 8,758 rows, `new_dotnet_object.csv` = 587 rows.
- Hash comparison: no differences between the fresh run and the `new_*` references (set delta = 0); row-count gaps reflect duplicate hashes rather than missed queries.

## Takeaways
- The Go path still has precision issues: one stored-proc missed entirely, and aggressive splitting/`EXEC` rewriting introduces extra hashes and objects.
- .NET output aligns with the curated reference for the current samples.
