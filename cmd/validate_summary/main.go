package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"

	"code_sql_scan/summary"
)

func main() {
	var (
		outDir = flag.String("out", "./out_regress", "output directory containing CSV files")
		app    = flag.String("app", "", "application prefix (e.g., golang-sample)")
		strict = flag.Bool("strict", false, "exit non-zero when mismatches are found")
	)
	flag.Parse()

	if strings.TrimSpace(*app) == "" {
		log.Fatalf("missing required -app")
	}

	resolve := func(name string) string {
		return filepath.Join(*outDir, fmt.Sprintf("%s-%s.csv", *app, name))
	}

	queryPath := resolve("query")
	objectPath := resolve("object")
	funcSummaryPath := resolve("summary-function")
	objSummaryPath := resolve("summary-object")

	for _, path := range []string{queryPath, objectPath, funcSummaryPath, objSummaryPath} {
		if _, err := os.Stat(path); err != nil {
			log.Fatalf("required file %s not found: %v", path, err)
		}
	}

	report, err := summary.VerifyConsistency(queryPath, objectPath, funcSummaryPath, objSummaryPath)
	if err != nil {
		log.Fatalf("validate summary: %v", err)
	}
	if report == nil {
		fmt.Println("no summary files provided; nothing to validate")
		return
	}

	if report.TotalMismatches() == 0 {
		fmt.Printf("summary consistency OK for app=%s\n", *app)
		return
	}

	fmt.Printf("summary consistency FAILED for app=%s (mismatches=%d)\n", *app, report.TotalMismatches())
	for _, msg := range report.FunctionMismatches {
		fmt.Printf("FUNCTION: %s\n", msg)
	}
	for _, msg := range report.ObjectMismatches {
		fmt.Printf("OBJECT: %s\n", msg)
	}
	if *strict {
		os.Exit(1)
	}
}
