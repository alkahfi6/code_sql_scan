# Scanner Output Review

This document summarizes a manual spot-check of the latest scanner outputs for the Go and .NET sample projects.

## Method
- Parsed the generated CSVs to count usage kinds and cross-database flags.
- Spot-checked representative source files to confirm whether the scanner classifications matched the code.

## Go (`golang`)
- Usage kinds counted from `new_golang_query.csv`: 25 `SELECT`, 18 `UNKNOWN`, 3 `INSERT`, 2 `DELETE`, 2 `UPDATE`, 1 `EXEC`.
- Cross-database flag counts: 49 `false`, 2 `true`.
- Observations:
  - The stored procedure call `ExecStoredProcedure(ctx, db, SPUpdateControlTableResikoPasar, ...)` at `destination.go:12-19` is reported with `UsageKind=UNKNOWN` in `new_golang_query.csv` even though the SQL text is the stored procedure name (`[dbo].[UpdateControlTableResikoPasar] ?,?`).
  - Several dynamic query construction paths (e.g., `DeleteDuplicateConfo`, `ExecuteSPMurexTConfo`, `UpdateTableMK`) appear as `<dynamic-sql>` with `UsageKind=UNKNOWN`; this matches the code paths where the query string is selected at runtime.

## .NET (`dotnet_check`)
- Usage kinds counted from `new_dotnet_query.csv`: 430 `EXEC`, 36 `UNKNOWN`, 30 `TRUNCATE`, 8 `INSERT`, 3 `SELECT`, 1 `DELETE`.
- Cross-database flag counts: all 508 rows are `false`.
- Observations:
  - The `UNKNOWN` rows correspond to `<dynamic-sql>` entries such as `clsAPIServiceInvestmentObligasi.cs:321` and `clsAPIServiceNTI.cs:142`, which aligns with dynamic concatenation in the source.

Overall, the CSVs are internally consistent and reflect the dynamic/static SQL usage observed in the code, with the main potential mislabel being the stored procedure call noted above.
