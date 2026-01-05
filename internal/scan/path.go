package scan

import (
	"path/filepath"
	"strings"
)

func ensureRelPath(root, p string) string {
	if p == "" {
		return ""
	}
	clean := filepath.ToSlash(filepath.Clean(p))
	rootClean := filepath.ToSlash(filepath.Clean(root))
	if rootClean == "" {
		return filepath.ToSlash(clean)
	}

	rootAbs, err := filepath.Abs(rootClean)
	if err != nil {
		rootAbs = rootClean
	}

	rootAbs = filepath.ToSlash(rootAbs)

	var absPath string
	if filepath.IsAbs(clean) {
		absPath = clean
	} else {
		cleanSlash := filepath.ToSlash(clean)
		if strings.HasPrefix(cleanSlash, rootClean+"/") {
			trimmed := strings.TrimPrefix(cleanSlash[len(rootClean):], "/")
			clean = filepath.FromSlash(trimmed)
		} else if cleanSlash == rootClean {
			clean = ""
		}
		absPath = filepath.ToSlash(filepath.Join(rootAbs, clean))
	}

	if !isWithinRoot(rootAbs, absPath) {
		return ""
	}

	if rel, err := filepath.Rel(rootAbs, absPath); err == nil && rel != "." && !strings.HasPrefix(rel, "..") {
		return filepath.ToSlash(filepath.Clean(rel))
	}
	return ""
}

func isWithinRoot(root, target string) bool {
	if root == "" || target == "" {
		return false
	}
	rootAbs, err := filepath.Abs(root)
	if err != nil {
		rootAbs = filepath.Clean(root)
	}
	tgtAbs, err := filepath.Abs(target)
	if err != nil {
		tgtAbs = filepath.Clean(target)
	}
	rel, err := filepath.Rel(rootAbs, tgtAbs)
	if err != nil {
		return false
	}
	return rel == "." || (!strings.HasPrefix(rel, "..") && rel != "")
}
