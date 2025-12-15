#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

baseline_go_query="/tmp/baseline_go_query.csv"
baseline_go_object="/tmp/baseline_go_object.csv"
baseline_go_summary_function="/tmp/baseline_go_summary_function.csv"
baseline_go_summary_object="/tmp/baseline_go_summary_object.csv"

baseline_dotnet_query="/tmp/baseline_dotnet_query.csv"
baseline_dotnet_object="/tmp/baseline_dotnet_object.csv"
baseline_dotnet_summary_function="/tmp/baseline_dotnet_summary_function.csv"
baseline_dotnet_summary_object="/tmp/baseline_dotnet_summary_object.csv"
baseline_dotnet_summary_form="/tmp/baseline_dotnet_summary_form.csv"

out_dir_new="/tmp/out_new"
compat_dir="/tmp/compat_legacy"

run_baseline() {
        echo "[baseline] legacy flag run (go)"
        go run main.go -root "./golang" -app "golang-sample" -lang "go" \
                -out-query "$baseline_go_query" \
                -out-object "$baseline_go_object" \
                -out-summary-func "$baseline_go_summary_function" \
                -out-summary-object "$baseline_go_summary_object"

        echo "[baseline] legacy flag run (dotnet)"
        go run main.go -root "./dotnet_check" -app "dotnet-sample" -lang "dotnet" \
                -out-query "$baseline_dotnet_query" \
                -out-object "$baseline_dotnet_object" \
                -out-summary-func "$baseline_dotnet_summary_function" \
                -out-summary-object "$baseline_dotnet_summary_object" \
                -out-summary-form "$baseline_dotnet_summary_form"

        echo "[baseline] checksums"
        shasum -a 256 /tmp/baseline_*.csv > /tmp/baseline_sha.txt
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
                -out-query "$compat_dir/go_query.csv" \
                -out-object "$compat_dir/go_object.csv" \
                -out-summary-func "$compat_dir/go_summary_function.csv" \
                -out-summary-object "$compat_dir/go_summary_object.csv"

        go run main.go -root "./dotnet_check" -app "dotnet-sample" -lang "dotnet" \
                -out-query "$compat_dir/dotnet_query.csv" \
                -out-object "$compat_dir/dotnet_object.csv" \
                -out-summary-func "$compat_dir/dotnet_summary_function.csv" \
                -out-summary-object "$compat_dir/dotnet_summary_object.csv" \
                -out-summary-form "$compat_dir/dotnet_summary_form.csv"

        diff -u "$baseline_go_query" "$compat_dir/go_query.csv"
        diff -u "$baseline_go_object" "$compat_dir/go_object.csv"
        diff -u "$baseline_go_summary_function" "$compat_dir/go_summary_function.csv"
        diff -u "$baseline_go_summary_object" "$compat_dir/go_summary_object.csv"

        diff -u "$baseline_dotnet_query" "$compat_dir/dotnet_query.csv"
        diff -u "$baseline_dotnet_object" "$compat_dir/dotnet_object.csv"
        diff -u "$baseline_dotnet_summary_function" "$compat_dir/dotnet_summary_function.csv"
        diff -u "$baseline_dotnet_summary_object" "$compat_dir/dotnet_summary_object.csv"
        diff -u "$baseline_dotnet_summary_form" "$compat_dir/dotnet_summary_form.csv"
}

run_baseline
run_new_outdir
compare_pairs
compat_legacy
