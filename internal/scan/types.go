package scan

// Config holds user-supplied scanning options.
type Config struct {
	Root             string
	AppName          string
	Lang             string
	OutDir           string
	OutQuery         string
	OutObject        string
	OutSummaryFunc   string
	OutSummaryObject string
	OutSummaryForm   string
	MaxFileSize      int64
	Workers          int
	IncludeExt       map[string]struct{}
}

type SqlSymbol struct {
	Name       string
	Value      string
	RelPath    string
	Line       int
	IsComplete bool
	IsProcSpec bool
}

type staticSet struct {
	Values []SqlSymbol
}

type SqlCandidate struct {
	AppName          string
	RelPath          string
	File             string
	SourceCat        string // code / config / script
	SourceKind       string // go / csharp / xml / yaml / json / sql
	CallSiteKind     string
	DynamicSignature string
	LineStart        int
	LineEnd          int
	Func             string
	RawSql           string
	SqlClean         string
	UsageKind        string // SELECT/INSERT/UPDATE/DELETE/TRUNCATE/EXEC/UNKNOWN
	IsWrite          bool
	IsDynamic        bool
	IsExecStub       bool
	ConnName         string
	ConnDb           string
	DefinedPath      string
	DefinedLine      int
	// Analisis objek
	HasCrossDb bool
	DbList     []string
	Objects    []ObjectToken
	// Flags
	QueryHash string
	RiskLevel string
}

type ObjectToken struct {
	DbName             string
	SchemaName         string
	BaseName           string
	FullName           string
	FoundAt            int
	Role               string // target/source/exec
	DmlKind            string // SELECT/INSERT/...
	IsWrite            bool
	IsCrossDb          bool
	IsLinkedServer     bool
	IsObjectNameDyn    bool
	IsPseudoObject     bool
	PseudoKind         string
	RepresentativeLine int
}

// QueryUsageRow represents one CSV row for query usage output.
type QueryUsageRow struct {
	AppName          string
	RelPath          string
	File             string
	SourceCat        string
	SourceKind       string
	CallSiteKind     string
	DynamicSignature string
	LineStart        int
	LineEnd          int
	Func             string
	RawSql           string
	SqlClean         string
	UsageKind        string
	IsWrite          bool
	HasCrossDb       bool
	DbList           string
	ObjectCount      int
	IsDynamic        bool
	ConnName         string
	ConnDb           string
	QueryHash        string
	RiskLevel        string
	DefinedPath      string
	DefinedLine      int
}

// ObjectUsageRow represents one CSV row for object usage output.
type ObjectUsageRow struct {
	AppName         string
	RelPath         string
	File            string
	SourceCat       string
	SourceKind      string
	Line            int
	Func            string
	QueryHash       string
	ObjectName      string
	DbName          string
	SchemaName      string
	BaseName        string
	IsCrossDb       bool
	IsLinkedServer  bool
	Role            string
	DmlKind         string
	IsWrite         bool
	IsObjectNameDyn bool
	IsPseudoObject  bool
	PseudoKind      string
}
