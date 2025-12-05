# Scanner Output Review

This document summarizes a manual spot-check of the latest scanner outputs for the Go and .NET sample projects.

- Scanner invocation (fresh run):
  - `go run . -root ./golang -app golang -lang go -out-query new_golang_query.csv -out-object new_golang_object.csv`
  - `go run . -root ./dotnet_check -app dotnet -lang dotnet -out-query new_dotnet_query.csv -out-object new_dotnet_object.csv`
  - File discovery reported `12` files in `golang` (all `.go`/`.sql`/config; `.txt` helpers intentionally skipped) and `10` files in `dotnet_check` (all `.cs`/config; one non-code ancillary file skipped). The allowed-extension gating in `main.go` (`.go/.sql/.xml/.json/.yaml/.yml/.config` for Go, `.cs/.sql/.xml/.json/.yaml/.yml/.config` for .NET) matches the sample content, so all code/config files with SQL are traversed.

## Method
- Parsed the generated CSVs to count usage kinds and cross-database flags.
- Spot-checked representative source files to confirm whether the scanner classifications matched the code.

## Go (`golang`)
- Usage kinds counted from the fresh `new_golang_query.csv`: 24 `SELECT`, 16 `UNKNOWN`, 5 `EXEC`, 2 `DELETE`, 2 `UPDATE`, 2 `INSERT`.
- Cross-database flag counts: 49 `false`, 2 `true` (SQL_Employee, SQL_SIBS references).
- Observations:
  - The stored procedure call `ExecStoredProcedure(ctx, db, SPUpdateControlTableResikoPasar, ...)` in `go1/destination.go` is now typed as `EXEC` with the procedure text captured.
  - Dynamic query construction paths (e.g., `DeleteDuplicateConfo`, `ExecuteSPMurexTConfo`, `UpdateTableMK`) still appear as `<dynamic-sql>` with `UsageKind=UNKNOWN`, which is expected because the concrete SQL is chosen at runtime.

## .NET (`dotnet_check`)
- Usage kinds counted from the fresh `new_dotnet_query.csv`: 430 `EXEC`, 36 `UNKNOWN`, 30 `TRUNCATE`, 8 `INSERT`, 3 `SELECT`, 1 `DELETE`.
- Cross-database flag counts: all 508 rows are `false`.
- Observations:
  - The `UNKNOWN` rows correspond to `<dynamic-sql>` entries such as `clsAPIServiceInvestmentObligasi.cs:321` and `clsAPISERVICE_NTI.cs:142`, which aligns with dynamic concatenation in the source.

Overall, the CSVs are internally consistent and reflect the dynamic/static SQL usage observed in the code. The `main.go` extension filters ensure every SQL-bearing sample file is scanned; remaining `UNKNOWN` usage kinds come from dynamic SQL that cannot be resolved statically, which is expected for a static-only engine.
