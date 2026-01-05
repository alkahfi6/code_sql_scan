# code_sql_scan

## Validation

- Run `scripts/regression.sh` to compare current outputs against the baseline artifacts and to ensure regression checks are green.
- Any change to the scanning engine must pass the summary consistency validator: `go run ./tools/check_summary_consistency.go --out ./out_regress --app golang-sample --app dotnet-sample`.
- TRUNCATE and MERGE statements are supported end-to-end; add more test variants under `internal/scan/` if you encounter additional patterns.
