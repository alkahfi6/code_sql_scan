package scan

import (
	"fmt"
	"log"
	"strings"
)

type logLevel int

const (
	levelDebug logLevel = iota
	levelInfo
	levelWarn
	levelError
	levelFatal
	levelNone
)

var (
	currentLogLevel logLevel = levelInfo
	allowLogSql     bool
)

// ConfigureLogging sets the desired log level and whether SQL text is allowed in logs.
func ConfigureLogging(level string, logSql bool) {
	currentLogLevel = parseLogLevel(level)
	allowLogSql = logSql
}

func parseLogLevel(level string) logLevel {
	switch strings.ToLower(strings.TrimSpace(level)) {
	case "debug":
		return levelDebug
	case "info", "":
		return levelInfo
	case "warn", "warning":
		return levelWarn
	case "error":
		return levelError
	case "fatal":
		return levelFatal
	case "none", "silent", "off":
		return levelNone
	default:
		return levelInfo
	}
}

func shouldLog(level logLevel) bool {
	if currentLogLevel == levelNone {
		return false
	}
	return level >= currentLogLevel
}

func containsSqlFields(msg string) bool {
	lower := strings.ToLower(msg)
	return strings.Contains(lower, "rawsql") || strings.Contains(lower, "sqlclean")
}

func redactSql(msg string) string {
	if allowLogSql {
		return msg
	}
	lower := strings.ToLower(msg)
	keywords := []string{"select", "insert", "update", "delete", "truncate", "merge", "exec"}
	for _, kw := range keywords {
		if strings.Contains(lower, kw) {
			return "<redacted-sql>"
		}
	}
	return msg
}

func logDebugf(format string, args ...interface{}) {
	logf(levelDebug, format, args...)
}

func logInfof(format string, args ...interface{}) {
	logf(levelInfo, format, args...)
}

func logWarnf(format string, args ...interface{}) {
	logf(levelWarn, format, args...)
}

func logErrorf(format string, args ...interface{}) {
	logf(levelError, format, args...)
}

func logf(level logLevel, format string, args ...interface{}) {
	if !shouldLog(level) {
		return
	}
	msg := fmt.Sprintf(format, args...)
	if !allowLogSql && containsSqlFields(msg) {
		return
	}
	log.Print(msg)
}
