package scan

import (
	"fmt"
	"log"
	"os"
	"path/filepath"
	"runtime/debug"
	"sort"
	"strings"
	"sync"
	"time"
)

// Run orchestrates the end-to-end scan and summary generation.
func Run(cfg *Config) ([]string, error) {
	initRegexes()

	start := time.Now()
	log.Printf("[INFO] starting scan root=%s app=%s lang=%s workers=%d maxSize=%d", cfg.Root, cfg.AppName, cfg.Lang, cfg.Workers, cfg.MaxFileSize)

	pathCh, countCh := streamFiles(cfg)
	cands := runWorkers(cfg, pathCh)
	fileCount := <-countCh
	log.Printf("[INFO] total files to scan: %d", fileCount)

	var analyzed []SqlCandidate
	for i := range cands {
		parts := expandMultiStatementCandidate(cands[i])
		for j := range parts {
			analyzeCandidate(&parts[j])
			dedupeObjectTokens(&parts[j])
			if len(parts[j].Objects) > 1 {
				sort.Slice(parts[j].Objects, func(a, b int) bool {
					oa, ob := parts[j].Objects[a], parts[j].Objects[b]
					if oa.DbName != ob.DbName {
						return oa.DbName < ob.DbName
					}
					if oa.SchemaName != ob.SchemaName {
						return oa.SchemaName < ob.SchemaName
					}
					if oa.BaseName != ob.BaseName {
						return oa.BaseName < ob.BaseName
					}
					if oa.Role != ob.Role {
						return oa.Role < ob.Role
					}
					return oa.DmlKind < ob.DmlKind
				})
			}
			analyzed = append(analyzed, parts[j])
		}
	}

	cands = dedupeCandidates(analyzed)

	sort.Slice(cands, func(i, j int) bool {
		a, b := cands[i], cands[j]
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.LineStart != b.LineStart {
			return a.LineStart < b.LineStart
		}
		if a.QueryHash != b.QueryHash {
			return a.QueryHash < b.QueryHash
		}
		return a.SourceKind < b.SourceKind
	})

	if err := writeCSVs(cfg, cands); err != nil {
		return nil, fmt.Errorf("write CSV failed: %w", err)
	}
	if err := generateSummaries(cfg); err != nil {
		return nil, fmt.Errorf("write summary failed: %w", err)
	}

	log.Printf("[INFO] done in %s", time.Since(start))
	return CollectOutputPaths(cfg), nil
}

func CollectOutputPaths(cfg *Config) []string {
	var paths []string
	if cfg.OutQuery != "" {
		paths = append(paths, cfg.OutQuery)
	}
	if cfg.OutObject != "" {
		paths = append(paths, cfg.OutObject)
	}
	if cfg.OutSummaryFunc != "" {
		paths = append(paths, cfg.OutSummaryFunc)
	}
	if cfg.OutSummaryObject != "" {
		paths = append(paths, cfg.OutSummaryObject)
	}
	if cfg.OutSummaryForm != "" {
		paths = append(paths, cfg.OutSummaryForm)
	}
	return paths
}

var skipDirs = []string{
	"bin", "obj", "dist", "out", "target", "node_modules", "packages", "vendor",
}

func extractRegexpPattern(msg string) string {
	start := strings.Index(msg, "regexp: Compile(")
	if start == -1 {
		return ""
	}
	start += len("regexp: Compile(")
	end := strings.Index(msg[start:], ")")
	if end == -1 {
		return ""
	}
	return msg[start : start+end]
}

func stackSingleLine() string {
	return strings.ReplaceAll(strings.TrimSpace(string(debug.Stack())), "\n", "|")
}

func workerRecover(id int, cfg *Config, stage *string, currentPath *string) {
	if r := recover(); r != nil {
		msg := fmt.Sprint(r)
		pattern := extractRegexpPattern(msg)
		log.Printf("[ERROR] stage=%s lang=%s worker=%d root=%q file=%q err=%q pattern=%q stack=%q", *stage, cfg.Lang, id, cfg.Root, *currentPath, msg, pattern, stackSingleLine())
	}
}

func streamFiles(cfg *Config) (<-chan string, <-chan int) {
	paths := make(chan string, cfg.Workers*2)
	count := make(chan int, 1)
	go func() {
		defer close(paths)
		defer close(count)
		total := 0
		err := filepath.WalkDir(cfg.Root, func(path string, d os.DirEntry, err error) error {
			if err != nil {
				log.Printf("[WARN] walk error on %s: %v", path, err)
				return nil
			}
			if d.IsDir() {
				name := strings.ToLower(d.Name())
				for _, s := range skipDirs {
					if name == s {
						return filepath.SkipDir
					}
				}
				return nil
			}
			ext := strings.ToLower(filepath.Ext(path))
			if !isAllowedExt(cfg, ext) {
				return nil
			}
			total++
			paths <- path
			return nil
		})
		if err != nil {
			log.Printf("[WARN] walkdir error: %v", err)
		}
		count <- total
	}()
	return paths, count
}

func isAllowedExt(cfg *Config, ext string) bool {
	if ext == "" {
		return false
	}
	switch cfg.Lang {
	case "go":
		switch ext {
		case ".go", ".xml", ".config", ".json", ".yaml", ".yml", ".sql":
			return true
		}
	case "dotnet":
		switch ext {
		case ".cs", ".xml", ".config", ".json", ".yaml", ".yml", ".sql":
			return true
		}
	}
	if _, ok := cfg.IncludeExt[ext]; ok {
		return true
	}
	return false
}

func runWorkers(cfg *Config, paths <-chan string) []SqlCandidate {
	workers := cfg.Workers
	if workers <= 0 {
		workers = 4
	}

	jobBuf := workers * 2
	if jobBuf < workers {
		jobBuf = workers
	}
	resultBuf := workers * 4

	jobs := make(chan string, jobBuf)
	results := make(chan []SqlCandidate, resultBuf)

	var wg sync.WaitGroup

	for i := 0; i < workers; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			stage := "init"
			currentPath := ""
			defer workerRecover(id, cfg, &stage, &currentPath)

			for path := range jobs {
				currentPath = path
				stage = fmt.Sprintf("scan-%s-file", cfg.Lang)
				cs, err := scanFile(cfg, path)
				if err != nil {
					log.Printf("[WARN] stage=%s lang=%s worker=%d root=%q file=%q err=%v", stage, cfg.Lang, id, cfg.Root, path, err)
					continue
				}
				if len(cs) > 0 {
					results <- cs
				}
			}
		}(i + 1)
	}

	go func() {
		for p := range paths {
			jobs <- p
		}
		close(jobs)
		wg.Wait()
		close(results)
	}()

	var all []SqlCandidate
	for batch := range results {
		all = append(all, batch...)
	}
	return all
}
