//go:build tools
// +build tools

package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"strings"

	summary "code_sql_scan/summary"
)

type args struct {
	queryPath   string
	objectPath  string
	funcSummary string
	objSummary  string
	examples    int
}

func main() {
	a := parseArgs()

	report, err := summary.VerifyConsistency(a.queryPath, a.objectPath, a.funcSummary, a.objSummary)
	if err != nil {
		log.Fatalf("verify consistency: %v", err)
	}
	if report == nil {
		fmt.Println("No summary paths provided; nothing to check.")
		return
	}

	total := report.TotalMismatches()
	if total == 0 {
		fmt.Println("SUMMARY CONSISTENCY PASS")
		return
	}

	fmt.Printf("SUMMARY CONSISTENCY FAIL (%d mismatches)\n", total)
	if len(report.FunctionMismatches) > 0 {
		fmt.Printf(" - Function mismatches: %d\n", len(report.FunctionMismatches))
	}
	if len(report.ObjectMismatches) > 0 {
		fmt.Printf(" - Object mismatches: %d\n", len(report.ObjectMismatches))
	}
	for i, ex := range report.Examples(limitExamples(a.examples)) {
		fmt.Printf("   #%d %s\n", i+1, ex)
	}
	os.Exit(1)
}

func parseArgs() args {
	var a args
	flag.StringVar(&a.queryPath, "query", "", "path to QueryUsage CSV (e.g., ./out_regress/app-query.csv)")
	flag.StringVar(&a.objectPath, "object", "", "path to ObjectUsage CSV (e.g., ./out_regress/app-object.csv)")
	flag.StringVar(&a.funcSummary, "summary-func", "", "path to function summary CSV (e.g., ./out_regress/app-summary-function.csv)")
	flag.StringVar(&a.objSummary, "summary-object", "", "path to object summary CSV (e.g., ./out_regress/app-summary-object.csv)")
	flag.IntVar(&a.examples, "examples", 3, "number of mismatch examples to print")

	flag.Parse()

	if strings.TrimSpace(a.queryPath) == "" ||
		strings.TrimSpace(a.objectPath) == "" ||
		strings.TrimSpace(a.funcSummary) == "" ||
		strings.TrimSpace(a.objSummary) == "" {
		flag.Usage()
		os.Exit(1)
	}

	return a
}

func limitExamples(n int) int {
	if n < 0 {
		return 0
	}
	if n == 0 {
		return 0
	}
	return n
}
