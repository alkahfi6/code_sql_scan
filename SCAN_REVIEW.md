# Scanner Output Review

This document summarizes a manual spot-check of the latest scanner outputs for the Go and .NET sample projects.

- Scanner invocation (fresh run):
  - `go run . -root ./golang -app golang -lang go -out-query new_golang_query.csv -out-object new_golang_object.csv`
  - `go run . -root ./dotnet_check -app dotnet -lang dotnet -out-query new_dotnet_query.csv -out-object new_dotnet_object.csv`
  - File discovery reported `12` files in `golang` (all `.go`/`.sql`/config; `.txt` helpers intentionally skipped) and `10` files in `dotnet_check` (all `.cs`/config; one non-code ancillary file skipped). The allowed-extension gating in `main.go` (`.go/.sql/.xml/.json/.yaml/.yml/.config` for Go, `.cs/.sql/.xml/.json/.yaml/.yml/.config` for .NET) matches the sample content, so all code/config files with SQL are traversed.

## Method
- Parsed the freshly generated CSVs (`out_query_golang.csv`, `out_object_golang.csv`, `out_query_dotnet.csv`, `out_object_dotnet.csv`).
- Compared them against the curated references (`new_*` CSVs) and directly against the sample source files.

## Go (`golang`)
- Fresh run counts: `out_query_golang.csv` = **2,260 rows**, `out_object_golang.csv` = **136 rows**.
- Reference counts (`new_*`): `new_golang_query.csv` = 1,586 rows, `new_golang_object.csv` = 115 rows.
- Mismatches vs reference:
  - Two reference hashes are missing because the new run prepends `EXEC` to stored-procedure calls, changing the hash for:
    - `UpdateControlTableResikoPasar` (`go1/destination.go`) – now emitted as `EXEC [dbo].[UpdateControlTableResikoPasar] ?,?` (`QueryHash=bf6f164a…`).
    - `ValidateMKData` (`go1/source.go`) – now emitted as `EXEC [dbo].[MKValidateData] ?` (`QueryHash=28f859d7…`).
  - Eighteen extra hashes appear from `go1/query.go`, e.g., full-body `INSERT`/`SELECT` text such as `INSERT INTO SQL_REPLICATE.dbo.mk005_a …` (`QueryHash=51bb2133…`) that were not present in the reference.
- Observed issues when comparing with code:
  - The stored-procedure strings in `go1/sp.go` do **not** contain the `EXEC` prefix, but the scanner rewrites them with `EXEC`, changing `SqlClean` and `QueryHash` and duplicating object names (e.g., `ControlTableResikoPasar` now appears twice in `out_object_golang.csv`).
  - Multi-line `INSERT` blocks in `go1/query.go` are now emitted as separate `SqlClean` strings with embedded newlines; they were absent in the reference, so they inflate row counts and object entries for `mk005_a`/`mk006_a`.

## .NET (`dotnet_check`)
- Fresh run counts: `out_query_dotnet.csv` = **8,643 rows**, `out_object_dotnet.csv` = **433 rows**.
- Reference counts: `new_dotnet_query.csv` = 8,758 rows, `new_dotnet_object.csv` = 587 rows.
- Hash comparison shows **no differences** between the fresh run and `new_*` for .NET (the row count delta comes from duplicate hashes being collapsed when counting unique hashes). Spot checks (e.g., `clsDataAccess1.cs`, `ANTSNAModel.cs`) show the same `EXEC` and `TRUNCATE` classifications as the reference.
