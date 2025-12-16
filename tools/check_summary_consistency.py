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
    query_rows = load_csv(os.path.join(out_dir, f"{app}-query.csv"))
    query_by_hash = {
        (row.get("RelPath", ""), row.get("QueryHash", "")): row for row in query_rows
    }
    obj_rows = load_csv(os.path.join(out_dir, f"{app}-object.csv"))
    summary_rows = load_csv(os.path.join(out_dir, f"{app}-summary-object.csv"))

    grouped = defaultdict(list)
    for row in obj_rows:
        grouped[row.get("BaseName", "")].append(row)

    expected = {}
    for base, rows in grouped.items():
        roles = set()
        total_reads = total_writes = total_exec = 0
        func_set = set()
        dbs = set()
        has_cross = False
        pseudo = False
        pseudo_kind = ""
        full_names = set()
        for r in rows:
            role = r.get("Role", "").strip()
            if role:
                roles.add(role)
            dml = r.get("DmlKind", "").upper()
            is_write = parse_bool(r.get("IsWrite", "false"))
            if ((not is_write and dml == "SELECT") or role.lower() == "source"):
                total_reads += 1
            if role.lower() in {"target", "mixed"} and dml in {"INSERT", "UPDATE", "DELETE", "TRUNCATE"}:
                total_writes += 1
            if role.lower() == "exec":
                total_exec += 1
            if parse_bool(r.get("IsCrossDb", "false")):
                has_cross = True
            db_name = r.get("DbName", "").strip()
            if db_name:
                dbs.add(db_name)
            full_name = r.get("FullObjectName") or ""
            if full_name:
                full_names.add(full_name)
            if parse_bool(r.get("IsPseudoObject", "false")):
                pseudo = True
                pseudo_kind = r.get("PseudoKind", "") or pseudo_kind or "unknown"
            qkey = (r.get("RelPath", ""), r.get("QueryHash", ""))
            qrow = query_by_hash.get(qkey)
            if qrow:
                fn = (qrow.get("Func", "") or "").strip()
                if fn:
                    func_set.add(fn)
        example_funcs = sorted(func_set)
        if len(example_funcs) > 5:
            example_funcs = example_funcs[:5]
        if pseudo and not pseudo_kind:
            pseudo_kind = "unknown"

        expected[base] = {
            "Roles": ";".join(sorted(roles)) if roles else "",
            "TotalReads": total_reads,
            "TotalWrites": total_writes,
            "TotalExec": total_exec,
            "TotalFuncs": len(func_set),
            "ExampleFuncs": ";".join(example_funcs),
            "IsPseudoObject": "true" if pseudo else "false",
            "PseudoKind": pseudo_kind if pseudo else "",
            "HasCrossDb": has_cross,
            "DbList": ";".join(sorted(dbs)),
            "FullObjectName": ";".join(sorted(full_names)),
        }

    summary_map = {row.get("BaseName", ""): row for row in summary_rows}
    mismatches = []
    for base, exp in expected.items():
        row = summary_map.get(base)
        if not row:
            mismatches.append((base, "missing in summary"))
            if len(mismatches) >= 20:
                break
            continue
        for col in [
            "Roles",
            "TotalReads",
            "TotalWrites",
            "TotalExec",
            "TotalFuncs",
            "ExampleFuncs",
            "IsPseudoObject",
            "PseudoKind",
            "HasCrossDb",
            "DbList",
        ]:
            val = row.get(col, "")
            if col in {"Roles", "ExampleFuncs", "DbList"}:
                if ";".join(parse_list(val)) != ";".join(parse_list(exp[col])):
                    mismatches.append((base, f"{col} expected {exp[col]} found {val}"))
            elif col in {"IsPseudoObject", "HasCrossDb"}:
                if (val or "").strip().lower() not in {"true", "false"}:
                    mismatches.append((base, f"{col} invalid value {val}"))
                elif (val or "").strip().lower() != ("true" if exp[col] in [True, "true"] else "false"):
                    mismatches.append((base, f"{col} expected {exp[col]} found {val}"))
            elif col in {"TotalReads", "TotalWrites", "TotalExec", "TotalFuncs"}:
                if parse_int(val) != exp[col]:
                    mismatches.append((base, f"{col} expected {exp[col]} found {val}"))
            else:
                if (val or "") != (exp[col] or ""):
                    mismatches.append((base, f"{col} expected {exp[col]} found {val}"))
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
