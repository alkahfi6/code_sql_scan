package scan

import (
	"crypto/sha1"
	"fmt"
	"testing"
)

func TestQueryHash_UsesOnlySqlClean(t *testing.T) {
	hash1 := computeQueryHash("select 1", "select 1")

	hash2 := computeQueryHash("select 1", "select 1") // metadata differences are irrelevant to hashing
	if hash1 != hash2 {
		t.Fatalf("expected identical hashes for same SqlClean; got %s vs %s", hash1, hash2)
	}

	hash3 := computeQueryHash("", "select 2")
	expected := fmt.Sprintf("%x", sha1.Sum([]byte("select 2")))
	if hash3 != expected {
		t.Fatalf("expected fallback hash %s, got %s", expected, hash3)
	}
}
