package scan

import "sync"

// connRegistry keeps connection name -> database mappings with concurrency safety.
type connRegistry struct {
	mu   sync.RWMutex
	data map[string]string
}

func newConnRegistry() *connRegistry {
	return &connRegistry{data: make(map[string]string)}
}

func (c *connRegistry) set(name, db string) {
	c.mu.Lock()
	c.data[name] = db
	c.mu.Unlock()
}

func (c *connRegistry) get(name string) (string, bool) {
	c.mu.RLock()
	defer c.mu.RUnlock()
	db, ok := c.data[name]
	return db, ok
}

func (c *connRegistry) snapshot() map[string]string {
	c.mu.RLock()
	defer c.mu.RUnlock()
	copy := make(map[string]string, len(c.data))
	for k, v := range c.data {
		copy[k] = v
	}
	return copy
}

// goSymtabCache caches package-wide Go symbol tables per directory to avoid redundant parsing.
type goSymtabCache struct {
	mu   sync.RWMutex
	data map[string]map[string]SqlSymbol
}

func newGoSymtabCache() *goSymtabCache {
	return &goSymtabCache{data: make(map[string]map[string]SqlSymbol)}
}

func (c *goSymtabCache) load(dir string) (map[string]SqlSymbol, bool) {
	c.mu.RLock()
	defer c.mu.RUnlock()
	sym, ok := c.data[dir]
	return sym, ok
}

func (c *goSymtabCache) store(dir string, symtab map[string]SqlSymbol) {
	c.mu.Lock()
	c.data[dir] = symtab
	c.mu.Unlock()
}

var goSymtabStore = newGoSymtabCache()

type goPkgSymtabCache struct {
	mu   sync.RWMutex
	data map[string]map[string]SqlSymbol
}

func newGoPkgSymtabCache() *goPkgSymtabCache {
	return &goPkgSymtabCache{data: make(map[string]map[string]SqlSymbol)}
}

func (c *goPkgSymtabCache) load(key string) (map[string]SqlSymbol, bool) {
	c.mu.RLock()
	defer c.mu.RUnlock()
	sym, ok := c.data[key]
	return sym, ok
}

func (c *goPkgSymtabCache) store(key string, symtab map[string]SqlSymbol) {
	c.mu.Lock()
	c.data[key] = symtab
	c.mu.Unlock()
}

var goPkgSymtabStore = newGoPkgSymtabCache()
var connStore = newConnRegistry()

func resetGlobalStores() {
	goSymtabStore = newGoSymtabCache()
	goPkgSymtabStore = newGoPkgSymtabCache()
	connStore = newConnRegistry()
}
