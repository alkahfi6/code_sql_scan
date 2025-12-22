package scan

import (
	"path/filepath"
	"strings"
)

func ensureRelPath(root, p string) string {
	if p == "" {
		return ""
	}
	clean := filepath.Clean(p)
	rootClean := filepath.Clean(root)
	if rootClean == "" {
		return filepath.ToSlash(clean)
	}

	rootAbs, err := filepath.Abs(rootClean)
	if err != nil {
		rootAbs = rootClean
	}

	var absPath string
	if filepath.IsAbs(clean) {
		absPath = clean
	} else {
		cleanSlash := filepath.ToSlash(clean)
		rootSlash := filepath.ToSlash(rootClean)
		if strings.HasPrefix(cleanSlash, rootSlash+"/") {
			trimmed := strings.TrimPrefix(cleanSlash[len(rootSlash):], "/")
			clean = filepath.FromSlash(trimmed)
		} else if cleanSlash == rootSlash {
			clean = ""
		}
		absPath = filepath.Join(rootAbs, clean)
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
