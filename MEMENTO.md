# Quick Memento

- Scope: offline static scan for Go and .NET sample apps in `./golang` and `./dotnet_check`.
- Outputs per app (raw + summary CSVs) under `./out_regress`: `*-query.csv`, `*-object.csv`, `*-summary-function.csv`, `*-summary-object.csv`, and `*-summary-form.csv` (for .NET).
- Schema essentials:
  - QueryUsage: app, path, function, line span, raw/clean SQL, usage kind, write flag, dynamic/cross-DB flags, object count, `QueryHash = hash(SqlClean)`.
  - ObjectUsage: per `QueryHash`, includes object name breakdown, role (source/target/exec/mixed), DML kind, write/cross-DB flags, pseudo-object markers.
- Key rules: derive UsageKind (INSERT/UPDATE/DELETE/TRUNCATE/EXEC) from call-site, do not inject `EXEC` into literals, default schema `dbo`, cross-DB only when DB prefix is present, distinguish `dynamic-sql` vs `dynamic-object` placeholders.
- Regression evidence checklist: regenerate outputs with `go run main.go ...`, then record `ls`, `wc -l`, `sha256sum`, `head -n 5`, and targeted `grep` proofs for expected objects or invariants.
