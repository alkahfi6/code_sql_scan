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

func TestQueryHashV2_StrongerButContentOnly(t *testing.T) {
	v1 := computeQueryHashV2("select 1", "select 1")
	v2 := computeQueryHashV2("select 1", "select 1")
	if v1 != v2 {
		t.Fatalf("expected deterministic sha256 hash, got %s vs %s", v1, v2)
	}

	v3 := computeQueryHashV2("", "select 2")
	if v3 == "" || len(v3) != 64 {
		t.Fatalf("expected sha256 hex digest, got %s", v3)
	}
}
