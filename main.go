package main

import (
	"fmt"
	"log"
	"os"
	"strings"

	"code_sql_scan/internal/cli"
	"code_sql_scan/internal/scan"
)

func main() {
	log.SetOutput(os.Stdout)
	log.SetFlags(log.LstdFlags | log.Lmicroseconds)

	cfg := cli.ParseFlags()
	scan.ConfigureLogging(cfg.LogLevel, cfg.LogSql)
	paths, err := scan.Run(cfg)
	if err != nil {
		log.Fatalf("[FATAL] %v", err)
	}

	if len(paths) > 0 {
		fmt.Printf("Wrote: %s\n", strings.Join(paths, " "))
	}
}
