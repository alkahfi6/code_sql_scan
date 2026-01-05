import argparse
import csv
import os
import sys
from collections import defaultdict


def parse_bool(val: str) -> bool:
    return str(val).strip().lower() == "true"


def parse_int(val: str) -> int:
    try:
        return int(val.strip())
    except Exception:
        return 0


def parse_list(val: str):
    items = [item.strip() for item in val.split(";") if item.strip()]
    return sorted(set(items))


def normalize_role(val: str) -> str:
    return val.strip().lower()


def split_dml(val: str):
    return [item.strip().upper() for item in val.split(";") if item.strip()]


def has_read_dml(val: str) -> bool:
    return any(item in {"SELECT", "READ"} for item in split_dml(val))


def has_exec_dml(val: str) -> bool:
    return any(item == "EXEC" for item in split_dml(val))


def object_read_count(row: dict) -> int:
    role = normalize_role(row.get("Role", ""))
    if role == "source":
        return 1
    if role == "mixed" and has_read_dml(row.get("DmlKind", "")):
        return 1
    return 0


def object_write_count(row: dict) -> int:
    return 1 if parse_bool(row.get("IsWrite", "false")) else 0


def object_exec_count(row: dict) -> int:
    role = normalize_role(row.get("Role", ""))
    if role == "exec" or has_exec_dml(row.get("DmlKind", "")):
        return 1
    return 0


def default_pseudo_kind(kind: str) -> str:
    k = (kind or "").strip().lower()
    return k if k else "unknown"


def pseudo_object_info(base: str, pseudo_kind_hint: str):
    trimmed = (base or "").strip()
    if not trimmed:
        return False, ""
    lower = trimmed.lower()
    if lower == "<dynamic-sql>":
        return True, "dynamic-sql"
    if lower.startswith("<dynamic-object"):
        return True, "dynamic-object"
    if lower.startswith("<") and lower.endswith(">"):
        kind = lower[1:-1].strip() or "unknown"
        return True, kind
    hint = default_pseudo_kind(pseudo_kind_hint)
    if hint.startswith("dynamic-") and hint != "unknown":
        return True, hint
    return False, ""


def split_full_object_name(full_name: str):
    parts = [p for p in (full_name or "").split(".") if p]
    db_name = schema_name = ""
    base = full_name or ""
    if len(parts) == 3:
        db_name, schema_name, base = parts
    elif len(parts) == 2:
        schema_name, base = parts
    elif len(parts) == 1:
        base = parts[0]
    return db_name, schema_name, base


def make_object_key(row: dict):
    full_name = row.get("FullObjectName", "") or ""
    db_name, schema_name, base_from_full = split_full_object_name(full_name)
    base = (row.get("BaseName", "") or base_from_full).strip()
    pseudo_kind_hint = row.get("PseudoKind", "") or ""
    is_pseudo = parse_bool(row.get("IsPseudoObject", "false"))
    detected, detected_kind = pseudo_object_info(base, pseudo_kind_hint)
    if detected:
        is_pseudo = True
        pseudo_kind = default_pseudo_kind(detected_kind)
    elif is_pseudo:
        pseudo_kind = default_pseudo_kind(pseudo_kind_hint)
    else:
        pseudo_kind = ""
    db_name = row.get("DbName", "") or db_name
    schema_name = row.get("SchemaName", "") or schema_name
    key = (
        row.get("AppName", ""),
        row.get("RelPath", ""),
        full_name,
        db_name,
        schema_name,
        base,
        "true" if is_pseudo else "false",
        pseudo_kind,
    )
    return key, pseudo_kind, is_pseudo


def summarize_pseudo_kinds_flat(counts: dict) -> str:
    if not counts:
        return ""
    ordered = sorted(counts.items(), key=lambda kv: (-kv[1], kv[0]))
    return ";".join([name for name, _ in ordered])


def is_dynamic_base_name(base: str) -> bool:
    trimmed = (base or "").strip().lower()
    return trimmed == "<dynamic-sql>" or trimmed.startswith("<dynamic-object")


def should_skip_object(row: dict) -> bool:
    base = (row.get("BaseName", "") or "").strip()
    if is_dynamic_base_name(base):
        return False
    if not base:
        return True
    lower = base.lower()
    if lower in {"eq", "dbo", "dbo."}:
        return True
    if base.endswith("."):
        return True
    return False


def is_dynamic_query(row: dict) -> bool:
    raw = row.get("RawSql", "").lower()
    clean = row.get("SqlClean", "").lower()
    if parse_bool(row.get("IsDynamic", "false")):
        return True
    if "<expr>" in clean:
        return True
    if "<dynamic" in clean or "<dynamic" in raw:
        return True
    return False


def load_csv(path: str):
    with open(path, newline="") as f:
        reader = csv.DictReader(f)
        return list(reader)


def check_function_summary(app: str, out_dir: str):
    query_rows = load_csv(os.path.join(out_dir, f"{app}-query.csv"))
    summary_rows = load_csv(os.path.join(out_dir, f"{app}-summary-function.csv"))

    grouped = defaultdict(list)
    for row in query_rows:
        grouped[(row.get("RelPath", ""), row.get("Func", ""))].append(row)

    expected = {}
    for key, rows in grouped.items():
        totals = defaultdict(int)
        db_list = set()
        has_cross = False
        line_start = []
        line_end = []
        for r in rows:
            kind = r.get("UsageKind", "").upper()
            totals[kind] += 1
            if kind in {"INSERT", "UPDATE", "DELETE", "TRUNCATE", "EXEC"}:
                totals["WRITE"] += 1
            if is_dynamic_query(r):
                totals["DYNAMIC"] += 1
            if parse_bool(r.get("HasCrossDb", "false")):
                has_cross = True
            db_list.update(parse_list(r.get("DbList", "")))
            ls = parse_int(r.get("LineStart", "0"))
            le = parse_int(r.get("LineEnd", "0"))
            if ls:
                line_start.append(ls)
            if le:
                line_end.append(le)
        expected[key] = {
            "TotalQueries": len(rows),
            "TotalSelect": totals.get("SELECT", 0),
            "TotalInsert": totals.get("INSERT", 0),
            "TotalUpdate": totals.get("UPDATE", 0),
            "TotalDelete": totals.get("DELETE", 0),
            "TotalTruncate": totals.get("TRUNCATE", 0),
            "TotalExec": totals.get("EXEC", 0),
            "TotalWrite": totals.get("WRITE", 0),
            "TotalDynamic": totals.get("DYNAMIC", 0),
            "HasCrossDb": has_cross,
            "DbList": ";".join(sorted(db_list)),
            "LineStart": min(line_start) if line_start else 0,
            "LineEnd": max(line_end) if line_end else 0,
        }

    found = {(row.get("RelPath", ""), row.get("Func", "")): row for row in summary_rows}

    mismatches = []
    for key, exp in expected.items():
        row = found.get(key)
        if not row:
            mismatches.append((key, "missing in summary"))
            continue
        for col in [
            "TotalQueries",
            "TotalSelect",
            "TotalInsert",
            "TotalUpdate",
            "TotalDelete",
            "TotalTruncate",
            "TotalExec",
            "TotalWrite",
            "TotalDynamic",
            "DbList",
        ]:
            val = row.get(col, "")
            if col == "DbList":
                val_norm = ";".join(parse_list(val))
                if val_norm != exp[col]:
                    mismatches.append((key, f"{col} expected {exp[col]} found {val_norm}"))
            else:
                if parse_int(val) != exp[col]:
                    mismatches.append((key, f"{col} expected {exp[col]} found {val}"))
            if len(mismatches) >= 20:
                break
        if len(mismatches) >= 20:
            break

    return mismatches


def check_object_summary(app: str, out_dir: str):
    obj_rows = load_csv(os.path.join(out_dir, f"{app}-object.csv"))
    summary_rows = load_csv(os.path.join(out_dir, f"{app}-summary-object.csv"))

    grouped = defaultdict(list)
    for row in obj_rows:
        if should_skip_object(row):
            continue
        key, pseudo_kind, is_pseudo = make_object_key(row)
        grouped[key].append((row, pseudo_kind, is_pseudo))

    expected = {}
    for key, grouped_rows in grouped.items():
        roles = set()
        dml_set = set()
        total_reads = total_writes = total_exec = 0
        func_counts = defaultdict(int)
        dbs = set()
        has_cross = False
        pseudo = False
        pseudo_kinds = defaultdict(int)
        for r, pseudo_kind_value, is_pseudo in grouped_rows:
            role = normalize_role(r.get("Role", ""))
            if role:
                roles.add(role)
            for part in split_dml(r.get("DmlKind", "")):
                dml_set.add(part)
            total_reads += object_read_count(r)
            total_writes += object_write_count(r)
            total_exec += object_exec_count(r)
            if parse_bool(r.get("IsCrossDb", "false")):
                has_cross = True
            db_name = r.get("DbName", "").strip()
            if db_name:
                dbs.add(db_name)
            if is_pseudo:
                pseudo = True
                pseudo_kinds[default_pseudo_kind(pseudo_kind_value)] += 1
            fn = (r.get("Func", "") or "").strip()
            if fn:
                func_counts[fn] += 1
        example_funcs = sorted(func_counts.items(), key=lambda kv: (-kv[1], kv[0].lower()))
        example_funcs = [name for name, _ in example_funcs[:5]]
        if pseudo and not pseudo_kinds:
            pseudo_kinds["unknown"] += 1

        expected[key] = {
            "Roles": ";".join(sorted(roles)) if roles else "",
            "DmlKinds": ";".join(sorted(dml_set)) if dml_set else "",
            "TotalReads": total_reads,
            "TotalWrites": total_writes,
            "TotalExec": total_exec,
            "TotalFuncs": len(func_counts),
            "ExampleFuncs": ";".join(example_funcs),
            "IsPseudoObject": "true" if pseudo else "false",
            "PseudoKind": summarize_pseudo_kinds_flat(pseudo_kinds) if pseudo else "",
            "HasCrossDb": has_cross,
            "DbList": ";".join(sorted(dbs)),
            "RolesSummary": f"read={total_reads}; write={total_writes}; exec={total_exec}",
        }

    summary_map = {}
    for row in summary_rows:
        key, _, _ = make_object_key(row)
        summary_map[key] = row
    mismatches = []
    for key, exp in expected.items():
        row = summary_map.get(key)
        if not row:
            mismatches.append((key, "missing in summary"))
            if len(mismatches) >= 20:
                break
            continue
        for col in [
            "Roles",
            "DmlKinds",
            "TotalReads",
            "TotalWrites",
            "TotalExec",
            "TotalFuncs",
            "ExampleFuncs",
            "IsPseudoObject",
            "PseudoKind",
            "HasCrossDb",
            "DbList",
            "RolesSummary",
        ]:
            val = row.get(col, "")
            if col in {"Roles", "ExampleFuncs", "DbList", "DmlKinds"}:
                if ";".join(parse_list(val)) != ";".join(parse_list(exp[col])):
                    mismatches.append((key, f"{col} expected {exp[col]} found {val}"))
            elif col in {"IsPseudoObject", "HasCrossDb"}:
                if (val or "").strip().lower() not in {"true", "false"}:
                    mismatches.append((key, f"{col} invalid value {val}"))
                elif (val or "").strip().lower() != ("true" if exp[col] in [True, "true"] else "false"):
                    mismatches.append((key, f"{col} expected {exp[col]} found {val}"))
            elif col in {"TotalReads", "TotalWrites", "TotalExec", "TotalFuncs"}:
                if parse_int(val) != exp[col]:
                    mismatches.append((key, f"{col} expected {exp[col]} found {val}"))
            else:
                if (val or "") != (exp[col] or ""):
                    mismatches.append((key, f"{col} expected {exp[col]} found {val}"))
            if len(mismatches) >= 20:
                break
        if len(mismatches) >= 20:
            break
    return mismatches


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="./out_regress", help="output directory")
    parser.add_argument("--apps", nargs="*", default=["golang-sample", "dotnet-sample"], help="apps to check")
    args = parser.parse_args()

    overall_ok = True
    for app in args.apps:
        func_mismatch = check_function_summary(app, args.out)
        obj_mismatch = check_object_summary(app, args.out)
        if func_mismatch:
            overall_ok = False
            print(f"[FAIL] {app} function summary mismatches ({len(func_mismatch)} shown):")
            for m in func_mismatch[:20]:
                print("  ", m)
        else:
            print(f"[PASS] {app} function summary matches raw")
        if obj_mismatch:
            overall_ok = False
            print(f"[FAIL] {app} object summary mismatches ({len(obj_mismatch)} shown):")
            for m in obj_mismatch[:20]:
                print("  ", m)
        else:
            print(f"[PASS] {app} object summary matches raw")

    if overall_ok:
        print("Overall: PASS")
        return 0
    print("Overall: FAIL")
    return 1


if __name__ == "__main__":
    sys.exit(main())
