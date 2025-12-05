# Scan Validation Report

## Run Summary
- Commands executed:
  - `go run main.go -root "golang" -app "golang-sample" -lang "go" -out-query "out_query_golang.csv" -out-object "out_object_golang.csv"`
  - `go run main.go -root "dotnet_check" -app "dotnet-sample" -lang "dotnet" -out-query "out_query_dotnet.csv" -out-object "out_object_dotnet.csv"`
- Line counts (including header):
  - Go: `out_query_golang.csv` = 2,294 rows; `out_object_golang.csv` = 158 rows.
  - .NET: `out_query_dotnet.csv` = 8,643 rows; `out_object_dotnet.csv` = 433 rows.

## Detected Mismatches

### Go
1. **Branch-selected DELETE treated as multiple static statements instead of one dynamic query**
   - Function `DeleteDuplicateConfo` picks between two literals (`QueryDeleteDuplicateDataConfoMK005`/`MK006`) but they share one execution site. The scanner now emits a concrete DELETE row and multiple object entries (e.g., `confomk005_a`, `confomk006_a`) instead of the single `<dynamic-sql>` row in the reference output. This inflates counts and invents `Role=source` entries for a pure DELETE.
   - Evidence: branching at `golang/go1/destination.go` lines 32–41; corresponding row 4 in `out_query_golang.csv` shows `UsageKind=DELETE` with `IsDynamic=false` and hash `ad30b57d…` rather than the expected dynamic placeholder.

2. **Stored procedure ExecBASelectRefundBilling hash drift and odd `DefinedInRelPath`**
   - The procedure literal is detected, but the hash differs from ground truth (`0c2fcc8d…` vs `09e3f5e7…`) and `DefinedInRelPath` contains a relative `.././../` prefix. The SQL text matches the source, so the change likely stems from path normalization and is producing a divergent hash and duplicate object rows compared to the curated output.
   - Evidence: SQL literal at `golang/go3/refundBillingQueries_manual.sql.go` lines 12–14; `out_query_golang.csv` rows 2289–2292 show the warped path and altered hash.

3. **Extra EXEC rows for grouped stored-procedure constants**
   - Functions like `ExecuteSPMurexTConfo`, `ExecSPMK`, and `GetFlatFileData` switch on an enum to choose between several stored-procedure names. The reference output collapses these into single dynamic entries, but the scanner emits multiple EXEC rows (and cross-db objects) for each branch, over-reporting usage.
   - Evidence: switch blocks at `golang/go1/destination.go` lines 48–79 and 93–121 produce EXEC rows (e.g., hashes `e124b8d…`, `7de3f7…`) that are absent from `new_golang_query.csv`.

### .NET
- Hash set matches `new_dotnet_*` outputs; spot checks on EXEC/TRUNCATE classification align with source samples. No mismatches found in this run.

## Suggested Fixes
- Treat branch-selected literals that share a single execution site (e.g., `DeleteDuplicateConfo`) as dynamic when the chosen SQL depends solely on runtime parameters, so only one row is emitted and object roles stay accurate.
- Normalize `DefinedInRelPath` to repository-relative paths before hashing to keep stable hashes for generated sqlc files like `refundBillingQueries_manual.sql.go`.
- When multiple literal procedure names are selected in a switch/if chain but executed at one call site, emit a single dynamic EXEC row instead of per-branch expansions to avoid artificial inflation.
