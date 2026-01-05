package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"

	summary "code_sql_scan/summary"
)

type appList []string

func (a *appList) String() string {
	if a == nil {
		return ""
	}
	return strings.Join(*a, ",")
}

func (a *appList) Set(value string) error {
	for _, part := range strings.FieldsFunc(value, func(r rune) bool { return r == ',' || r == ';' }) {
		trimmed := strings.TrimSpace(part)
		if trimmed == "" {
			continue
		}
		*a = append(*a, trimmed)
	}
	return nil
}

func main() {
	var outDir string
	apps := appList{}
	defaultApps := []string{"golang-sample", "dotnet-sample"}

	flag.StringVar(&outDir, "out", "./out_regress", "output directory containing summary CSVs")
	flag.Var(&apps, "app", "app name to validate (can be provided multiple times)")
	flag.Parse()

	if len(apps) == 0 {
		apps = append(apps, defaultApps...)
	}
	if len(apps) == 0 {
		log.Fatalf("at least one -app value is required")
	}
	trimmedOut := strings.TrimSpace(outDir)
	if trimmedOut == "" {
		log.Fatalf("output directory cannot be empty")
	}

	ok := true
	for _, app := range apps {
		if err := validateApp(trimmedOut, app); err != nil {
			ok = false
			fmt.Printf("[FAIL] %s: %v\n", app, err)
			continue
		}
		fmt.Printf("[PASS] %s summaries match raw usage\n", app)
	}

	if !ok {
		os.Exit(1)
	}
}

func validateApp(outDir, app string) error {
	queryPath := filepath.Join(outDir, fmt.Sprintf("%s-query.csv", app))
	objectPath := filepath.Join(outDir, fmt.Sprintf("%s-object.csv", app))
	funcSummaryPath := filepath.Join(outDir, fmt.Sprintf("%s-summary-function.csv", app))
	objSummaryPath := filepath.Join(outDir, fmt.Sprintf("%s-summary-object.csv", app))

	queries, err := summary.LoadQueryUsage(queryPath)
	if err != nil {
		return fmt.Errorf("load query usage: %w", err)
	}
	funcSummary, err := summary.LoadFunctionSummary(funcSummaryPath)
	if err != nil {
		return fmt.Errorf("load function summary: %w", err)
	}
	if err := summary.ValidateFunctionSummaryCounts(queries, funcSummary); err != nil {
		return fmt.Errorf("function summary validation: %w", err)
	}

	objects, err := summary.LoadObjectUsage(objectPath)
	if err != nil {
		return fmt.Errorf("load object usage: %w", err)
	}
	objSummary, err := summary.LoadObjectSummary(objSummaryPath)
	if err != nil {
		return fmt.Errorf("load object summary: %w", err)
	}
	if err := summary.ValidateObjectSummaryCounts(objects, objSummary); err != nil {
		return fmt.Errorf("object summary validation: %w", err)
	}

	return nil
}
