package scan

import (
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
)

func scanFile(cfg *Config, path string) ([]SqlCandidate, error) {
	info, err := os.Stat(path)
	if err != nil {
		return nil, fmt.Errorf("stage=stat file=%q err=%w", path, err)
	}
	if info.Size() > cfg.MaxFileSize {
		logInfof("[INFO] stage=skip-too-large lang=%s root=%q file=%q size=%d", cfg.Lang, cfg.Root, path, info.Size())
		return nil, nil
	}
	isBin, binErr := isBinaryFile(path)
	if binErr != nil {
		logWarnf("[WARN] stage=read-bytes lang=%s root=%q file=%q err=%v", cfg.Lang, cfg.Root, path, binErr)
		return nil, nil
	}
	if isBin {
		logInfof("[INFO] stage=skip-binary lang=%s root=%q file=%q", cfg.Lang, cfg.Root, path)
		return nil, nil
	}

	ext := strings.ToLower(filepath.Ext(path))
	relPath := ensureRelPath(cfg.Root, path)

	isSqlGo := strings.HasSuffix(strings.ToLower(path), ".sql.go")

	switch ext {
	case ".go":
		if cfg.Lang != "go" {
			return nil, nil
		}
		return scanGoFile(cfg, path, relPath)
	case ".cs":
		if cfg.Lang != "dotnet" {
			return nil, nil
		}
		return scanCsFile(cfg, path, relPath)
	case ".xml", ".config", ".json", ".yaml", ".yml", ".toml", ".ini", ".conf":
		return scanConfigFile(cfg, path, relPath)
	case ".sql":
		return scanSqlFile(cfg, path, relPath)
	default:
		if cfg.Lang == "go" && isSqlGo {
			return scanSqlFile(cfg, path, relPath)
		}
		return nil, nil
	}
}

func isBinaryFile(path string) (bool, error) {
	f, err := os.Open(path)
	if err != nil {
		return false, err
	}
	defer f.Close()
	buf := make([]byte, 2048)
	n, err := f.Read(buf)
	if err != nil && err != io.EOF {
		return false, err
	}
	ctrl := 0
	for i := 0; i < n; i++ {
		b := buf[i]
		if b == 0 {
			return true, nil
		}
		if b < 0x09 {
			ctrl++
			if ctrl > 5 {
				return true, nil
			}
		}
	}
	return false, nil
}
