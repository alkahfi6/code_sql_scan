#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

baseline_dir="/tmp/baseline_out"
baseline_go_query="$baseline_dir/golang-sample-query.csv"
baseline_go_object="$baseline_dir/golang-sample-object.csv"
baseline_go_summary_function="$baseline_dir/golang-sample-summary-function.csv"
baseline_go_summary_object="$baseline_dir/golang-sample-summary-object.csv"
baseline_go_summary_form="$baseline_dir/golang-sample-summary-form.csv"

baseline_dotnet_query="$baseline_dir/dotnet-sample-query.csv"
baseline_dotnet_object="$baseline_dir/dotnet-sample-object.csv"
baseline_dotnet_summary_function="$baseline_dir/dotnet-sample-summary-function.csv"
baseline_dotnet_summary_object="$baseline_dir/dotnet-sample-summary-object.csv"
baseline_dotnet_summary_form="$baseline_dir/dotnet-sample-summary-form.csv"

out_dir_new="/tmp/out_new"
compat_dir="/tmp/compat_legacy"

run_baseline() {
        echo "[baseline] legacy flag run (go)"
        rm -rf "$baseline_dir"
        mkdir -p "$baseline_dir"

        go run main.go -root "./golang" -app "golang-sample" -lang "go" \
                -out-query "$baseline_go_query" \
                -out-object "$baseline_go_object" \
                -out-summary-func "$baseline_go_summary_function" \
                -out-summary-object "$baseline_go_summary_object" \
                -out-summary-form "$baseline_go_summary_form"

        echo "[baseline] legacy flag run (dotnet)"
        go run main.go -root "./dotnet_check" -app "dotnet-sample" -lang "dotnet" \
                -out-query "$baseline_dotnet_query" \
                -out-object "$baseline_dotnet_object" \
                -out-summary-func "$baseline_dotnet_summary_function" \
                -out-summary-object "$baseline_dotnet_summary_object" \
                -out-summary-form "$baseline_dotnet_summary_form"

        echo "[baseline] checksums"
        shasum -a 256 "$baseline_dir"/*.csv > /tmp/baseline_sha.txt
        cat /tmp/baseline_sha.txt
}

run_new_outdir() {
        echo "[refactor] new out-dir run (go)"
        rm -rf "$out_dir_new"
        go run main.go -root "./golang" -app "golang-sample" -lang "go" -out-dir "$out_dir_new"

        echo "[refactor] new out-dir run (dotnet)"
        go run main.go -root "./dotnet_check" -app "dotnet-sample" -lang "dotnet" -out-dir "$out_dir_new"
}

compare_pairs() {
        echo "[compare] out-dir outputs vs baseline"
        diff -u "$baseline_go_query" "$out_dir_new/golang-sample-query.csv"
        diff -u "$baseline_go_object" "$out_dir_new/golang-sample-object.csv"
        diff -u "$baseline_go_summary_function" "$out_dir_new/golang-sample-summary-function.csv"
        diff -u "$baseline_go_summary_object" "$out_dir_new/golang-sample-summary-object.csv"

        diff -u "$baseline_dotnet_query" "$out_dir_new/dotnet-sample-query.csv"
        diff -u "$baseline_dotnet_object" "$out_dir_new/dotnet-sample-object.csv"
        diff -u "$baseline_dotnet_summary_function" "$out_dir_new/dotnet-sample-summary-function.csv"
        diff -u "$baseline_dotnet_summary_object" "$out_dir_new/dotnet-sample-summary-object.csv"
        diff -u "$baseline_dotnet_summary_form" "$out_dir_new/dotnet-sample-summary-form.csv"
}

compat_legacy() {
        echo "[compat] legacy flags after refactor"
        rm -rf "$compat_dir"
        mkdir -p "$compat_dir"

        go run main.go -root "./golang" -app "golang-sample" -lang "go" \
                -out-query "$compat_dir/golang-sample-query.csv" \
                -out-object "$compat_dir/golang-sample-object.csv" \
                -out-summary-func "$compat_dir/golang-sample-summary-function.csv" \
                -out-summary-object "$compat_dir/golang-sample-summary-object.csv" \
                -out-summary-form "$compat_dir/golang-sample-summary-form.csv"

        go run main.go -root "./dotnet_check" -app "dotnet-sample" -lang "dotnet" \
                -out-query "$compat_dir/dotnet-sample-query.csv" \
                -out-object "$compat_dir/dotnet-sample-object.csv" \
                -out-summary-func "$compat_dir/dotnet-sample-summary-function.csv" \
                -out-summary-object "$compat_dir/dotnet-sample-summary-object.csv" \
                -out-summary-form "$compat_dir/dotnet-sample-summary-form.csv"

        diff -u "$baseline_go_query" "$compat_dir/golang-sample-query.csv"
        diff -u "$baseline_go_object" "$compat_dir/golang-sample-object.csv"
        diff -u "$baseline_go_summary_function" "$compat_dir/golang-sample-summary-function.csv"
        diff -u "$baseline_go_summary_object" "$compat_dir/golang-sample-summary-object.csv"
        diff -u "$baseline_go_summary_form" "$compat_dir/golang-sample-summary-form.csv"

        diff -u "$baseline_dotnet_query" "$compat_dir/dotnet-sample-query.csv"
        diff -u "$baseline_dotnet_object" "$compat_dir/dotnet-sample-object.csv"
        diff -u "$baseline_dotnet_summary_function" "$compat_dir/dotnet-sample-summary-function.csv"
        diff -u "$baseline_dotnet_summary_object" "$compat_dir/dotnet-sample-summary-object.csv"
        diff -u "$baseline_dotnet_summary_form" "$compat_dir/dotnet-sample-summary-form.csv"
}

validate_outputs() {
        echo "[validate] baseline outputs"
        go run ./tools/check_summary_consistency.go --out "$baseline_dir" --app golang-sample --app dotnet-sample

        echo "[validate] out-dir outputs"
        go run ./tools/check_summary_consistency.go --out "$out_dir_new" --app golang-sample --app dotnet-sample

        echo "[validate] compat outputs"
        go run ./tools/check_summary_consistency.go --out "$compat_dir" --app golang-sample --app dotnet-sample
}

run_baseline
run_new_outdir
compare_pairs
compat_legacy
validate_outputs
