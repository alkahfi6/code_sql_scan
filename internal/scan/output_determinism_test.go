package scan

import (
	"crypto/sha256"
	"encoding/hex"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"testing"
)

func TestDeterministicCsvOutputs(t *testing.T) {
	testCases := []struct {
		name string
		root string
		app  string
		lang string
	}{
		{name: "golang-sample", root: "./golang", app: "golang-sample", lang: "go"},
		{name: "dotnet-sample", root: "./dotnet_check", app: "dotnet-sample", lang: "dotnet"},
	}

	for _, tc := range testCases {
		t.Run(tc.name, func(t *testing.T) {
			dir := t.TempDir()
			firstOut := filepath.Join(dir, "run1")
			secondOut := filepath.Join(dir, "run2")
			if err := os.MkdirAll(firstOut, 0o755); err != nil {
				t.Fatalf("create first out dir: %v", err)
			}
			if err := os.MkdirAll(secondOut, 0o755); err != nil {
				t.Fatalf("create second out dir: %v", err)
			}

			runScan := func(outDir string) error {
				cmd := exec.Command("go", "run", "./", "-root", tc.root, "-app", tc.app, "-lang", tc.lang, "-out-dir", outDir)
				cmd.Env = append(os.Environ(), "GO111MODULE=on")
				cmd.Dir = filepath.Clean(filepath.Join("..", ".."))
				cmd.Stdout = os.Stdout
				cmd.Stderr = os.Stderr
				return cmd.Run()
			}

			if err := runScan(firstOut); err != nil {
				t.Fatalf("first scan run failed: %v", err)
			}
			if err := runScan(secondOut); err != nil {
				t.Fatalf("second scan run failed: %v", err)
			}

			firstHashes, err := hashCsvOutputs(firstOut)
			if err != nil {
				t.Fatalf("hash first outputs: %v", err)
			}
			secondHashes, err := hashCsvOutputs(secondOut)
			if err != nil {
				t.Fatalf("hash second outputs: %v", err)
			}

			if len(firstHashes) != len(secondHashes) {
				t.Fatalf("output file count mismatch: %d vs %d", len(firstHashes), len(secondHashes))
			}
			for name, hash := range firstHashes {
				other, ok := secondHashes[name]
				if !ok {
					t.Fatalf("missing output %s in second run", name)
				}
				if hash != other {
					t.Fatalf("hash mismatch for %s: %s vs %s", name, hash, other)
				}
			}
		})
	}
}

func hashCsvOutputs(dir string) (map[string]string, error) {
	files, err := filepath.Glob(filepath.Join(dir, "*.csv"))
	if err != nil {
		return nil, err
	}
	sort.Strings(files)
	out := make(map[string]string, len(files))
	for _, f := range files {
		hash, err := hashFile(f)
		if err != nil {
			return nil, err
		}
		out[filepath.Base(f)] = hash
	}
	return out, nil
}

func hashFile(path string) (string, error) {
	f, err := os.Open(path)
	if err != nil {
		return "", err
	}
	defer f.Close()
	h := sha256.New()
	if _, err := io.Copy(h, f); err != nil {
		return "", err
	}
	return hex.EncodeToString(h.Sum(nil)), nil
}
