package scan

import (
	"log"
	"regexp"
)

type regexRegistry struct {
	// method patterns
	simpleDeclAssign *regexp.Regexp
	bareAssign       *regexp.Regexp
	verbatimAssign   *regexp.Regexp
	execProcLit      *regexp.Regexp
	execProcDyn      *regexp.Regexp
	newCmd           *regexp.Regexp
	newCmdIdent      *regexp.Regexp
	dapperQuery      *regexp.Regexp
	dapperExec       *regexp.Regexp
	efFromSql        *regexp.Regexp
	efExecRaw        *regexp.Regexp
	execQuery        *regexp.Regexp
	callQueryWsLit   *regexp.Regexp
	callQueryWsDyn   *regexp.Regexp
	execQueryIdent   *regexp.Regexp
	commandTextLit   *regexp.Regexp
	commandTextIdent *regexp.Regexp
	byQueryCall      *regexp.Regexp
	identRe          *regexp.Regexp
	methodRe         *regexp.Regexp
	methodReNoMod    *regexp.Regexp

	// config / markup patterns
	xmlAttr        *regexp.Regexp
	xmlElem        *regexp.Regexp
	pipeField      *regexp.Regexp
	connStringAttr *regexp.Regexp

	// helpers
	dynamicPlaceholder   *regexp.Regexp
	procParamPlaceholder *regexp.Regexp
}

var regexes regexRegistry

func mustCompileRegex(name, pattern string) *regexp.Regexp {
	re, err := regexp.Compile(pattern)
	if err != nil {
		log.Fatalf("[FATAL] compile regex name=%s pattern=%q err=%v", name, pattern, err)
	}
	return re
}

func initRegexes() {
	regexes = regexRegistry{
		simpleDeclAssign:     mustCompileRegex("simpleDeclAssign", `(?i)\b(?:var|const|string)\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*@?"([^"]+)"`),
		bareAssign:           mustCompileRegex("bareAssign", `(?i)\b([A-Za-z_][A-Za-z0-9_]*)\s*=\s*@?"([^"]+)"`),
		verbatimAssign:       mustCompileRegex("verbatimAssign", `(?is)(?:var|const|string)\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*@"([^"]+)"`),
		execProcLit:          mustCompileRegex("execProcLit", `(?is)(\w+)\s*\.\s*ExecProc\s*\(\s*(@?)"([^"]+)"`),
		execProcDyn:          mustCompileRegex("execProcDyn", `(?i)(\w+)\s*\.\s*ExecProc\s*\(\s*([^),]+)`),
		newCmd:               mustCompileRegex("newCmd", `(?is)new\s+SqlCommand\s*\(\s*(@?)"([^"]+)"\s*,\s*([^)]+?)\)`),
		newCmdIdent:          mustCompileRegex("newCmdIdent", `(?is)new\s+SqlCommand\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*,\s*([^)]+?)\)`),
		dapperQuery:          mustCompileRegex("dapperQuery", `(?is)\.\s*Query(?:Async)?(?:<[^>]*>)?\s*\(\s*(@?)"([^"]+)"`),
		dapperExec:           mustCompileRegex("dapperExec", `(?is)\.\s*Execute(?:Async)?\s*\(\s*(@?)"([^"]+)"`),
		efFromSql:            mustCompileRegex("efFromSql", `(?is)\.\s*FromSqlRaw\s*\(\s*(@?)"([^"]+)"`),
		efExecRaw:            mustCompileRegex("efExecRaw", `(?is)\.\s*ExecuteSqlRaw\s*\(\s*(@?)"([^"]+)"`),
		execQuery:            mustCompileRegex("execQuery", `(?is)\.\s*ExecuteQuery\s*\(\s*[^,]+,\s*(@?)"([^"]+)"`),
		callQueryWsLit:       mustCompileRegex("callQueryWsLit", `(?is)\.\s*CallQueryFromWs\s*\(\s*[^,]+,\s*[^,]+,\s*(@?)"([^"]+)"`),
		callQueryWsDyn:       mustCompileRegex("callQueryWsDyn", `(?is)\.\s*CallQueryFromWs\s*\(\s*[^,]+,\s*[^,]+,\s*([^),]+)`),
		execQueryIdent:       mustCompileRegex("execQueryIdent", `(?i)\.\s*ExecuteQuery\s*\(\s*([^,]+),\s*([A-Za-z_][A-Za-z0-9_]*)`),
		commandTextLit:       mustCompileRegex("commandTextLit", `(?is)\.\s*CommandText\s*=\s*(@?)"([^"]+)"`),
		commandTextIdent:     mustCompileRegex("commandTextIdent", `(?i)\.\s*CommandText\s*=\s*([A-Za-z_][A-Za-z0-9_]*)\b`),
		byQueryCall:          mustCompileRegex("byQueryCall", `(?is)Insert[A-Za-z0-9_]*ByQuery\s*\(\s*[^,]+,\s*([^,]+)`),
		identRe:              mustCompileRegex("ident", `^[A-Za-z_][A-Za-z0-9_]*$`),
		methodRe:             mustCompileRegex("methodWithMods", `(?i)\b(?:public|private|protected|internal|static|async|sealed|override|virtual|partial)(?:\s+(?:public|private|protected|internal|static|async|sealed|override|virtual|partial))*\b[^{]*\b([A-Za-z_][A-Za-z0-9_]*)\s*\(`),
		methodReNoMod:        mustCompileRegex("methodNoMods", `(?i)^\s*(?:partial\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\s]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\([^;]*\)\s*{?`),
		xmlAttr:              mustCompileRegex("xmlAttr", `(?i)(sql|query|command|commandtext|storedprocedure)\s*=\s*"([^"]+)"`),
		xmlElem:              mustCompileRegex("xmlElem", `(?i)<\s*(sql|query|command|commandtext|storedprocedure)[^>]*>(.*?)<\s*/\s*(?:sql|query|command|commandtext|storedprocedure)\s*>`),
		pipeField:            mustCompileRegex("pipeField", `(?i)\s*(sql|query|command|commandtext|storedprocedure)[^>]*:\s*(.*?)(\s*[|]\s*|$)`),
		connStringAttr:       mustCompileRegex("connStringAttr", `(?i)<\s*add\s+[^>]*name\s*=\s*"([^"]+)"[^>]*connectionString\s*=\s*"([^"]+)"[^>]*>`),
		dynamicPlaceholder:   mustCompileRegex("dynamicPlaceholder", `@\w+|\$\{[^}]+\}|\<expr\>`),
		procParamPlaceholder: mustCompileRegex("procParamPlaceholder", `\s+[?@:][^\s,]*(\s*,\s*[?@:][^\s,]*)*\s*$`),
	}
}
