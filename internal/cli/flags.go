package cli

import (
	"flag"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"unicode"

	"code_sql_scan/internal/scan"
)

func ParseFlags() *scan.Config {
	root := flag.String("root", "", "root path project")
	app := flag.String("app", "", "application name")
	lang := flag.String("lang", "", "language mode: go | dotnet")
	outDir := flag.String("out-dir", "", "output directory for generated CSVs")
	outQ := flag.String("out-query", "", "output CSV for QueryUsage")
	outO := flag.String("out-object", "", "output CSV for ObjectUsage")
	outSummaryFunc := flag.String("out-summary-func", "", "output CSV for function-level summary")
	outSummaryObject := flag.String("out-summary-object", "", "output CSV for object-level summary")
	outSummaryForm := flag.String("out-summary-form", "", "output CSV for form/file-level summary")
	maxSize := flag.Int64("max-size", 2*1024*1024, "max file size in bytes")
	workers := flag.Int("workers", 4, "number of workers")
	includeExt := flag.String("include-ext", "", "additional extensions, comma-separated, e.g. .cshtml,.razor")
	logLevel := flag.String("log-level", "info", "log level: debug | info | warn | error | fatal | none")
	logSql := flag.Bool("log-sql", false, "log SQL text (RawSql/SqlClean) in debug/error paths")
	pseudoThreshold := flag.Int("pseudo-threshold", 100, "threshold for unique pseudo-objects before warning/fail")
	failOnPseudo := flag.Bool("fail-on-pseudo-explosion", false, "exit non-zero when pseudo-object cardinality exceeds threshold")

	flag.Parse()

	if *root == "" || *app == "" || *lang == "" {
		flag.Usage()
		os.Exit(1)
	}

	if absRoot, err := filepath.Abs(*root); err == nil {
		*root = filepath.Clean(absRoot)
	} else {
		*root = filepath.Clean(*root)
	}

	l := strings.ToLower(*lang)
	if l == "cs" || l == "csharp" {
		l = "dotnet"
	}
	if l != "go" && l != "dotnet" {
		log.Fatalf("invalid -lang value: %s (must be go or dotnet)", *lang)
	}

	inc := make(map[string]struct{})
	if *includeExt != "" {
		for _, e := range strings.Split(*includeExt, ",") {
			e = strings.TrimSpace(e)
			if e == "" {
				continue
			}
			if !strings.HasPrefix(e, ".") {
				e = "." + e
			}
			inc[strings.ToLower(e)] = struct{}{}
		}
	}

	// clamp workers to a reasonable range (1–32). if zero or negative, default to NumCPU.
	w := *workers
	if w <= 0 {
		if n := runtime.NumCPU(); n > 0 {
			w = n
		} else {
			w = 4
		}
	}
	if w < 1 {
		w = 1
	}
	if w > 32 {
		w = 32
	}

	sanitizedApp := sanitizeAppName(*app)
	resolvedOut := resolveOutputs(sanitizedApp, l, *outDir, *outQ, *outO, *outSummaryFunc, *outSummaryObject, *outSummaryForm)

	if resolvedOut.query == "" || resolvedOut.object == "" {
		flag.Usage()
		os.Exit(1)
	}

	if resolvedOut.baseDir != "" {
		if err := os.MkdirAll(resolvedOut.baseDir, 0o755); err != nil {
			log.Fatalf("failed to create out dir %s: %v", resolvedOut.baseDir, err)
		}
	}

	if *maxSize <= 0 {
		*maxSize = 2 * 1024 * 1024
	}
	if *pseudoThreshold <= 0 {
		*pseudoThreshold = 100
	}

	return &scan.Config{
		Root:             *root,
		AppName:          *app,
		Lang:             l,
		OutDir:           resolvedOut.baseDir,
		OutQuery:         resolvedOut.query,
		OutObject:        resolvedOut.object,
		OutSummaryFunc:   resolvedOut.summaryFunc,
		OutSummaryObject: resolvedOut.summaryObject,
		OutSummaryForm:   resolvedOut.summaryForm,
		MaxFileSize:      *maxSize,
		Workers:          w,
		IncludeExt:       inc,
		LogLevel:         strings.ToLower(*logLevel),
		LogSql:           *logSql,
		PseudoThreshold:  *pseudoThreshold,
		FailOnPseudo:     *failOnPseudo,
	}
}

func sanitizeAppName(app string) string {
	trimmed := strings.TrimSpace(app)
	if trimmed == "" {
		return "app"
	}
	var b strings.Builder
	for _, r := range trimmed {
		if unicode.IsLetter(r) || unicode.IsDigit(r) || r == '-' || r == '_' {
			b.WriteRune(unicode.ToLower(r))
			continue
		}
		if unicode.IsSpace(r) {
			b.WriteRune('-')
			continue
		}
		b.WriteRune('-')
	}
	cleaned := strings.Trim(b.String(), "-")
	if cleaned == "" {
		return "app"
	}
	return cleaned
}

type outputs struct {
	baseDir       string
	query         string
	object        string
	summaryFunc   string
	summaryObject string
	summaryForm   string
}

func resolveOutputs(app, lang, outDir, outQ, outO, outSummaryFunc, outSummaryObject, outSummaryForm string) outputs {
	baseDir := strings.TrimSpace(outDir)
	if baseDir != "" {
		baseDir = filepath.Clean(baseDir)
	}

	defaultQuery := ""
	defaultObject := ""
	defaultSummaryFunc := ""
	defaultSummaryObject := ""
	defaultSummaryForm := ""
	if baseDir != "" {
		defaultQuery = filepath.Join(baseDir, fmt.Sprintf("%s-query.csv", app))
		defaultObject = filepath.Join(baseDir, fmt.Sprintf("%s-object.csv", app))
		defaultSummaryFunc = filepath.Join(baseDir, fmt.Sprintf("%s-summary-function.csv", app))
		defaultSummaryObject = filepath.Join(baseDir, fmt.Sprintf("%s-summary-object.csv", app))
		if lang == "dotnet" {
			defaultSummaryForm = filepath.Join(baseDir, fmt.Sprintf("%s-summary-form.csv", app))
		}
	}

	resolved := outputs{baseDir: baseDir,
		query:         outQ,
		object:        outO,
		summaryFunc:   outSummaryFunc,
		summaryObject: outSummaryObject,
		summaryForm:   outSummaryForm,
	}

	if resolved.query == "" {
		resolved.query = defaultQuery
	}
	if resolved.object == "" {
		resolved.object = defaultObject
	}
	if resolved.summaryFunc == "" {
		resolved.summaryFunc = defaultSummaryFunc
	}
	if resolved.summaryObject == "" {
		resolved.summaryObject = defaultSummaryObject
	}
	if resolved.summaryForm == "" {
		resolved.summaryForm = defaultSummaryForm
	}

	return resolved
}

func OutputPaths(cfg *scan.Config) []string {
	return scan.CollectOutputPaths(cfg)
}
