package main

import (
	"fmt"
	"log"
	"strings"

	"code_sql_scan/internal/cli"
	"code_sql_scan/internal/scan"
)

func main() {
	log.SetFlags(log.LstdFlags | log.Lmicroseconds)

	cfg := cli.ParseFlags()
	paths, err := scan.Run(cfg)
	if err != nil {
		log.Fatalf("[FATAL] %v", err)
	}

	if len(paths) > 0 {
		fmt.Printf("Wrote: %s\n", strings.Join(paths, " "))
	}
}
