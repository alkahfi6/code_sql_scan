package summary

import (
	"encoding/csv"
	"fmt"
	"io"
	"os"
	"sort"
	"strconv"
	"strings"
	"unicode"
)

// QueryRow represents a row from QueryUsage.csv used for summaries.
type QueryRow struct {
	AppName    string
	RelPath    string
	File       string
	SourceCat  string
	SourceKind string
	Func       string
	RawSql     string
	SqlClean   string
	QueryHash  string
	UsageKind  string
	IsWrite    bool
	IsDynamic  bool
	HasCrossDb bool
	DbList     []string
	LineStart  int
	LineEnd    int
}

// ObjectRow represents a row from ObjectUsage.csv used for summaries.
type ObjectRow struct {
	AppName         string
	RelPath         string
	File            string
	QueryHash       string
	ObjectName      string
	DbName          string
	SchemaName      string
	BaseName        string
	Role            string
	DmlKind         string
	IsWrite         bool
	IsCrossDb       bool
	IsObjectNameDyn bool
	IsPseudoObject  bool
	PseudoKind      string
}

// FunctionSummaryRow represents aggregated information per function.
type FunctionSummaryRow struct {
	AppName       string
	RelPath       string
	Func          string
	TotalQueries  int
	TotalExec     int
	TotalSelect   int
	TotalInsert   int
	TotalUpdate   int
	TotalDelete   int
	TotalTruncate int
	TotalDynamic  int
	DynamicSig    string
	TotalWrite    int
	TotalObjects  int
	ObjectsUsed   string
	HasCrossDb    bool
	DbList        string
	LineStart     int
	LineEnd       int
}

// ObjectSummaryRow represents aggregated information per database object.
type ObjectSummaryRow struct {
	AppName        string
	BaseName       string
	FullObjectName string
	Roles          string
	TotalReads     int
	TotalWrites    int
	IsPseudoObject bool
	PseudoKind     string
	TotalExec      int
	TotalFuncs     int
	ExampleFuncs   string
	HasCrossDb     bool
	DbList         string
}

// FormSummaryRow represents aggregated information per file/form.
type FormSummaryRow struct {
	AppName      string
	RelPath      string
	TotalQueries int
	TotalExec    int
	TotalWrite   int
	TotalDynamic int
	HasCrossDb   bool
	TotalObjects int
	TopObjects   string
	DbList       string
}

func LoadQueryUsage(path string) ([]QueryRow, error) {
	f, err := os.Open(path)
	if err != nil {
		return nil, err
	}
	defer f.Close()

	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		return nil, err
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"AppName", "RelPath", "File", "Func", "QueryHash", "UsageKind", "IsWrite", "IsDynamic"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			return nil, fmt.Errorf("missing column %s in query usage", req)
		}
	}

	var rows []QueryRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			return nil, err
		}
		row := QueryRow{
			AppName:    rec[idx["AppName"]],
			RelPath:    rec[idx["RelPath"]],
			File:       rec[idx["File"]],
			SourceCat:  pick(rec, idx, "SourceCategory"),
			SourceKind: pick(rec, idx, "SourceKind"),
			RawSql:     pick(rec, idx, "RawSql"),
			SqlClean:   pick(rec, idx, "SqlClean"),
			Func:       rec[idx["Func"]],
			QueryHash:  rec[idx["QueryHash"]],
			UsageKind:  rec[idx["UsageKind"]],
			IsWrite:    parseBool(rec[idx["IsWrite"]]),
			IsDynamic:  parseBool(rec[idx["IsDynamic"]]),
		}
		if col, ok := idx["HasCrossDb"]; ok {
			row.HasCrossDb = parseBool(rec[col])
		}
		if col, ok := idx["DbList"]; ok {
			row.DbList = parseList(rec[col])
		}
		if col, ok := idx["LineStart"]; ok {
			row.LineStart = parseInt(rec[col])
		}
		if col, ok := idx["LineEnd"]; ok {
			row.LineEnd = parseInt(rec[col])
		}
		rows = append(rows, row)
	}
	return rows, nil
}

func LoadObjectUsage(path string) ([]ObjectRow, error) {
	f, err := os.Open(path)
	if err != nil {
		return nil, err
	}
	defer f.Close()

	r := csv.NewReader(f)
	header, err := r.Read()
	if err != nil {
		return nil, err
	}
	idx := make(map[string]int)
	for i, h := range header {
		idx[h] = i
	}
	required := []string{"AppName", "RelPath", "File", "QueryHash", "DbName", "SchemaName", "BaseName", "Role", "DmlKind", "IsWrite", "IsCrossDb"}
	for _, req := range required {
		if _, ok := idx[req]; !ok {
			return nil, fmt.Errorf("missing column %s in object usage", req)
		}
	}

	var rows []ObjectRow
	for {
		rec, err := r.Read()
		if err != nil {
			if err == io.EOF {
				break
			}
			return nil, err
		}
		row := ObjectRow{
			AppName:    rec[idx["AppName"]],
			RelPath:    rec[idx["RelPath"]],
			File:       rec[idx["File"]],
			QueryHash:  rec[idx["QueryHash"]],
			DbName:     rec[idx["DbName"]],
			SchemaName: rec[idx["SchemaName"]],
			BaseName:   rec[idx["BaseName"]],
			Role:       rec[idx["Role"]],
			DmlKind:    rec[idx["DmlKind"]],
			IsWrite:    parseBool(rec[idx["IsWrite"]]),
			IsCrossDb:  parseBool(rec[idx["IsCrossDb"]]),
		}
		if col, ok := idx["ObjectName"]; ok {
			row.ObjectName = rec[col]
		}
		if col, ok := idx["IsObjectNameDynamic"]; ok {
			row.IsObjectNameDyn = parseBool(rec[col])
		}
		if col, ok := idx["IsPseudoObject"]; ok {
			row.IsPseudoObject = parseBool(rec[col])
		}
		if col, ok := idx["PseudoKind"]; ok {
			row.PseudoKind = rec[col]
		}
		rows = append(rows, row)
	}
	return rows, nil
}

func BuildFunctionSummary(queries []QueryRow, objects []ObjectRow) ([]FunctionSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
	grouped := make(map[string][]QueryRow)
	hashByFunc := make(map[string]map[string]struct{})
	queryByKey := make(map[string]QueryRow)

	for _, q := range normQueries {
		key := functionKey(q.AppName, q.RelPath, q.File, q.Func)
		grouped[key] = append(grouped[key], q)
		if _, ok := hashByFunc[key]; !ok {
			hashByFunc[key] = make(map[string]struct{})
		}
		hashByFunc[key][q.QueryHash] = struct{}{}
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	objectsByQuery := map[string][]ObjectRow{}
	for _, o := range objects {
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		objectsByQuery[qKey] = append(objectsByQuery[qKey], o)
	}

	objectsByFunc := map[string][]ObjectRow{}
	for _, o := range objects {
		qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
		if q, ok := queryByKey[qKey]; ok {
			funcKey := functionKey(o.AppName, o.RelPath, o.File, q.Func)
			objectsByFunc[funcKey] = append(objectsByFunc[funcKey], o)
		}
	}

	var result []FunctionSummaryRow
	for key, qRows := range grouped {
		app, rel, file, fn := splitFunctionKey(key)
		var totalExec, totalSelect, totalInsert, totalUpdate, totalDelete, totalTruncate, totalDynamic, totalWrite int
		hasCross := false
		minLine := 0
		maxLine := 0
		dbListSet := make(map[string]struct{})
		objectCounter := make(map[string]*objectRoleCounter)
		dynamicSigCounts := make(map[string]dynamicSignatureInfo)
		for _, q := range qRows {
			switch strings.ToUpper(q.UsageKind) {
			case "EXEC":
				totalExec++
			case "SELECT":
				totalSelect++
			case "INSERT":
				totalInsert++
			case "UPDATE":
				totalUpdate++
			case "DELETE":
				totalDelete++
			case "TRUNCATE":
				totalTruncate++
			}
			if isDynamicQuery(q) {
				totalDynamic++
				sig := dynamicSignature(q)
				if sig != "" {
					entry := dynamicSigCounts[sig]
					entry.count++
					if entry.exampleHash == "" {
						entry.exampleHash = q.QueryHash
					}
					dynamicSigCounts[sig] = entry
				}
			}
			if q.HasCrossDb {
				hasCross = true
			}
			for _, db := range q.DbList {
				if db != "" {
					dbListSet[db] = struct{}{}
				}
			}
			if q.LineStart > 0 {
				if minLine == 0 || q.LineStart < minLine {
					minLine = q.LineStart
				}
			}
			if q.LineEnd > 0 {
				if maxLine == 0 || q.LineEnd > maxLine {
					maxLine = q.LineEnd
				}
			}
		}

		totalWrite = totalInsert + totalUpdate + totalDelete + totalTruncate + totalExec

		objSet := make(map[string]struct{})
		consumeObj := func(o ObjectRow) {
			name := strings.TrimSpace(o.BaseName)
			if name == "" {
				return
			}
			objSet[name] = struct{}{}
			if o.IsCrossDb {
				hasCross = true
			}
			if o.DbName != "" {
				dbListSet[o.DbName] = struct{}{}
			}
			counter := objectCounter[name]
			if counter == nil {
				counter = &objectRoleCounter{}
				objectCounter[name] = counter
			}
			counter.Register(o)
		}
		if hashes, ok := hashByFunc[key]; ok {
			for h := range hashes {
				qKey := queryObjectKey(app, rel, file, h)
				for _, o := range objectsByQuery[qKey] {
					consumeObj(o)
				}
			}
		}
		for _, o := range objectsByFunc[key] {
			consumeObj(o)
		}

		objectsUsed := buildTopObjectSummary(objectCounter)
		dbList := setToSortedSlice(dbListSet)
		dynamicSig := summarizeDynamicSignatures(dynamicSigCounts)

		result = append(result, FunctionSummaryRow{
			AppName:       app,
			RelPath:       rel,
			Func:          fn,
			TotalQueries:  len(qRows),
			TotalExec:     totalExec,
			TotalSelect:   totalSelect,
			TotalInsert:   totalInsert,
			TotalUpdate:   totalUpdate,
			TotalDelete:   totalDelete,
			TotalTruncate: totalTruncate,
			TotalDynamic:  totalDynamic,
			DynamicSig:    dynamicSig,
			TotalWrite:    totalWrite,
			TotalObjects:  len(objSet),
			ObjectsUsed:   objectsUsed,
			HasCrossDb:    hasCross,
			DbList:        strings.Join(dbList, ";"),
			LineStart:     minLine,
			LineEnd:       maxLine,
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.Func != b.Func {
			return a.Func < b.Func
		}
		return a.LineStart < b.LineStart
	})

	return result, nil
}

func BuildObjectSummary(queries []QueryRow, objects []ObjectRow) ([]ObjectSummaryRow, error) {
	normQueries := normalizeQueryFuncs(queries)
	queryByKey := make(map[string]QueryRow)
	for _, q := range normQueries {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	grouped := make(map[string][]ObjectRow)
	for _, o := range objects {
		key := strings.TrimSpace(o.BaseName)
		grouped[key] = append(grouped[key], o)
	}

	var result []ObjectSummaryRow
	for base, objs := range grouped {
		baseName := strings.TrimSpace(base)
		funcSet := make(map[string]struct{})
		totalReads := 0
		totalWrites := 0
		totalExec := 0
		hasCross := false
		dbSet := make(map[string]struct{})
		isPseudo, pseudoKind := pseudoObjectInfo(baseName)
		if isPseudo && pseudoKind == "" {
			pseudoKind = "unknown"
		}
		fullNames := make(map[string]struct{})

		for _, o := range objs {
			upperDml := strings.ToUpper(o.DmlKind)
			isWrite := o.IsWrite || isWriteDml(upperDml)
			isRead := (!isWrite && upperDml == "SELECT") || strings.EqualFold(o.Role, "source")
			isExec := strings.EqualFold(o.Role, "exec") || upperDml == "EXEC"
			if isRead {
				totalReads++
			}
			if isWrite && (upperDml == "INSERT" || upperDml == "UPDATE" || upperDml == "DELETE" || upperDml == "TRUNCATE") {
				totalWrites++
			}
			if isExec {
				totalExec++
			}
			if o.IsCrossDb {
				hasCross = true
			}
			if o.DbName != "" {
				dbSet[o.DbName] = struct{}{}
			}
			fullNames[buildFullName(o.DbName, o.SchemaName, o.BaseName)] = struct{}{}
			if o.IsPseudoObject {
				isPseudo = true
				pseudoKind = choosePseudoKind(pseudoKind, defaultPseudoKind(o.PseudoKind))
			}
			qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
			if q, ok := queryByKey[qKey]; ok {
				fn := strings.TrimSpace(q.Func)
				if fn != "" {
					funcSet[fn] = struct{}{}
				}
			}
		}
		if isPseudo && pseudoKind == "" {
			pseudoKind = "unknown"
		}

		usedIn := setToSortedSlice(funcSet)
		fullNameList := setToSortedSlice(fullNames)
		dbList := setToSortedSlice(dbSet)
		exampleFuncs := usedIn
		if len(exampleFuncs) > 5 {
			exampleFuncs = exampleFuncs[:5]
		}

		roleSummary := summarizeRoleCounts(totalReads, totalWrites, totalExec)

		result = append(result, ObjectSummaryRow{
			AppName:        objs[0].AppName,
			BaseName:       baseName,
			FullObjectName: strings.Join(fullNameList, ";"),
			Roles:          roleSummary,
			TotalReads:     totalReads,
			TotalWrites:    totalWrites,
			TotalExec:      totalExec,
			TotalFuncs:     len(usedIn),
			ExampleFuncs:   strings.Join(exampleFuncs, ";"),
			IsPseudoObject: isPseudo,
			PseudoKind:     pseudoKind,
			HasCrossDb:     hasCross,
			DbList:         strings.Join(dbList, ";"),
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.BaseName != b.BaseName {
			return a.BaseName < b.BaseName
		}
		return a.FullObjectName < b.FullObjectName
	})

	return result, nil
}

func BuildFormSummary(queries []QueryRow, objects []ObjectRow) ([]FormSummaryRow, error) {
	groupQueries := make(map[string][]QueryRow)
	objectSetByForm := make(map[string]map[string]struct{})
	hasCrossByForm := make(map[string]bool)
	topObjectStats := make(map[string]map[string]*objectRoleCounter)
	dbListByForm := make(map[string]map[string]struct{})

	for _, q := range queries {
		key := formKey(q.AppName, q.RelPath, q.File)
		groupQueries[key] = append(groupQueries[key], q)
		if _, ok := dbListByForm[key]; !ok {
			dbListByForm[key] = make(map[string]struct{})
		}
		for _, db := range q.DbList {
			if db != "" {
				dbListByForm[key][db] = struct{}{}
			}
		}
		if q.HasCrossDb {
			hasCrossByForm[key] = true
		}
	}

	for _, o := range objects {
		key := formKey(o.AppName, o.RelPath, o.File)
		if _, ok := objectSetByForm[key]; !ok {
			objectSetByForm[key] = make(map[string]struct{})
		}
		if strings.TrimSpace(o.BaseName) != "" {
			objectSetByForm[key][strings.TrimSpace(o.BaseName)] = struct{}{}
		}
		if o.DbName != "" {
			if _, ok := dbListByForm[key]; !ok {
				dbListByForm[key] = make(map[string]struct{})
			}
			dbListByForm[key][o.DbName] = struct{}{}
		}
		if o.IsCrossDb {
			hasCrossByForm[key] = true
		}

		baseName := strings.TrimSpace(o.BaseName)
		isPseudo, _ := pseudoObjectInfo(baseName)
		if isPseudo || baseName == "" {
			continue
		}
		if _, ok := topObjectStats[key]; !ok {
			topObjectStats[key] = make(map[string]*objectRoleCounter)
		}
		counter := topObjectStats[key][baseName]
		if counter == nil {
			counter = &objectRoleCounter{}
			topObjectStats[key][baseName] = counter
		}
		counter.Register(o)
	}

	var result []FormSummaryRow
	for key, qRows := range groupQueries {
		app, rel, _ := splitFormKey(key)
		totalExec := 0
		totalWrite := 0
		totalDynamic := 0
		totalInsert := 0
		totalUpdate := 0
		totalDelete := 0
		totalTruncate := 0
		dbListSet := make(map[string]struct{})
		for _, q := range qRows {
			switch strings.ToUpper(q.UsageKind) {
			case "EXEC":
				totalExec++
			case "INSERT":
				totalInsert++
			case "UPDATE":
				totalUpdate++
			case "DELETE":
				totalDelete++
			case "TRUNCATE":
				totalTruncate++
			}
			if isDynamicQuery(q) {
				totalDynamic++
			}
			for _, db := range q.DbList {
				if db != "" {
					dbListSet[db] = struct{}{}
				}
			}
		}
		totalWrite = totalInsert + totalUpdate + totalDelete + totalTruncate + totalExec
		objSet := objectSetByForm[key]
		distinctObjects := len(objSet)
		hasCross := hasCrossByForm[key]
		for db := range dbListByForm[key] {
			dbListSet[db] = struct{}{}
		}

		topObjects := buildTopObjects(topObjectStats[key])
		if distinctObjects == 0 {
			topObjects = ""
		}

		result = append(result, FormSummaryRow{
			AppName:      app,
			RelPath:      rel,
			TotalQueries: len(qRows),
			TotalWrite:   totalWrite,
			TotalDynamic: totalDynamic,
			TotalExec:    totalExec,
			TotalObjects: distinctObjects,
			TopObjects:   topObjects,
			HasCrossDb:   hasCross,
			DbList:       strings.Join(setToSortedSlice(dbListSet), ";"),
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		return a.RelPath < b.RelPath
	})
	return result, nil
}

type objectRoleCounter struct {
	ReadCount  int
	WriteCount int
	ExecCount  int
	HasRead    bool
	HasWrite   bool
	HasExec    bool
}

func (c *objectRoleCounter) Register(o ObjectRow) {
	role := strings.ToLower(strings.TrimSpace(o.Role))
	isExec := role == "exec" || strings.EqualFold(o.DmlKind, "exec")
	isWrite := o.IsWrite || isWriteDml(o.DmlKind) || role == "target"
	isRead := role == "source" || (!isExec && !isWrite)

	if isExec {
		c.HasExec = true
		c.ExecCount++
	}
	if isWrite {
		c.HasWrite = true
		c.WriteCount++
	}
	if isRead {
		c.HasRead = true
		c.ReadCount++
	}
}

func dynamicSignature(q QueryRow) string {
	if q.LineStart == 0 {
		return ""
	}
	callKind := strings.TrimSpace(q.SourceKind)
	if callKind == "" {
		callKind = strings.TrimSpace(q.SourceCat)
	}
	if callKind == "" {
		callKind = strings.TrimSpace(q.UsageKind)
	}
	if callKind == "" {
		callKind = "unknown"
	}
	relPath := strings.TrimSpace(q.RelPath)
	funcName := strings.TrimSpace(q.Func)
	parts := []string{}
	if relPath != "" {
		parts = append(parts, relPath)
	}
	if funcName != "" {
		parts = append(parts, funcName)
	}
	parts = append(parts, callKind)
	return fmt.Sprintf("%s@%d", strings.Join(parts, "|"), q.LineStart)
}

type dynamicSignatureInfo struct {
	count       int
	exampleHash string
}

func summarizeDynamicSignatures(counts map[string]dynamicSignatureInfo) string {
	if len(counts) == 0 {
		return ""
	}
	type sigCount struct {
		key     string
		count   int
		example string
	}
	list := make([]sigCount, 0, len(counts))
	for key, info := range counts {
		list = append(list, sigCount{key: key, count: info.count, example: info.exampleHash})
	}
	sort.Slice(list, func(i, j int) bool {
		if list[i].count != list[j].count {
			return list[i].count > list[j].count
		}
		return list[i].key < list[j].key
	})
	if len(list) > 3 {
		list = list[:3]
	}
	parts := make([]string, 0, len(list))
	for _, item := range list {
		example := strings.TrimSpace(item.example)
		if example != "" {
			parts = append(parts, fmt.Sprintf("%s x%d (example=%s)", item.key, item.count, example))
			continue
		}
		parts = append(parts, fmt.Sprintf("%s x%d", item.key, item.count))
	}
	return strings.Join(parts, ";")
}

func buildTopObjectSummary(stats map[string]*objectRoleCounter) string {
	if len(stats) == 0 {
		return ""
	}
	type top struct {
		name  string
		count int
		role  string
	}
	tops := make([]top, 0, len(stats))
	for name, counter := range stats {
		role := dominantRole(counter)
		count := roleCount(counter, role)
		if count == 0 {
			count = counter.ReadCount + counter.WriteCount + counter.ExecCount
		}
		tops = append(tops, top{name: name, count: count, role: role})
	}
	sort.Slice(tops, func(i, j int) bool {
		if tops[i].count != tops[j].count {
			return tops[i].count > tops[j].count
		}
		return tops[i].name < tops[j].name
	})
	if len(tops) > 10 {
		tops = tops[:10]
	}
	parts := make([]string, 0, len(tops))
	for _, t := range tops {
		parts = append(parts, fmt.Sprintf("%s(%s)", t.name, t.role))
	}
	return strings.Join(parts, ";")
}

func summarizeRoleCounts(reads, writes, execs int) string {
	return fmt.Sprintf("read=%d; write=%d; exec=%d", reads, writes, execs)
}

func buildTopObjects(stats map[string]*objectRoleCounter) string {
	if len(stats) == 0 {
		return ""
	}
	type entry struct {
		name  string
		count int
	}
	buckets := map[string][]entry{
		"exec":  {},
		"write": {},
		"read":  {},
	}

	for name, counter := range stats {
		if counter.ExecCount > 0 || counter.HasExec {
			buckets["exec"] = append(buckets["exec"], entry{name: name, count: counter.ExecCount})
		}
		if counter.WriteCount > 0 || counter.HasWrite {
			buckets["write"] = append(buckets["write"], entry{name: name, count: counter.WriteCount})
		}
		if counter.ReadCount > 0 || counter.HasRead {
			buckets["read"] = append(buckets["read"], entry{name: name, count: counter.ReadCount})
		}
	}

	order := []string{"exec", "write", "read"}
	sections := make([]string, 0, len(order))
	for _, role := range order {
		items := buckets[role]
		if len(items) == 0 {
			continue
		}
		sort.Slice(items, func(i, j int) bool {
			if items[i].count != items[j].count {
				return items[i].count > items[j].count
			}
			return items[i].name < items[j].name
		})
		if len(items) > 5 {
			items = items[:5]
		}
		names := make([]string, 0, len(items))
		for _, item := range items {
			names = append(names, item.name)
		}
		sections = append(sections, fmt.Sprintf("%s: %s", role, strings.Join(names, ", ")))
	}

	return strings.Join(sections, "; ")
}

func classifyObjectUsage(counter *objectRoleCounter) string {
	if counter == nil {
		return "(mixed)"
	}
	if counter.HasWrite {
		if counter.HasExec || counter.HasRead {
			return "(mixed)"
		}
		return "(write)"
	}
	if counter.HasExec {
		if counter.HasRead {
			return "(mixed)"
		}
		return "(exec)"
	}
	if counter.HasRead {
		return "(read)"
	}
	return "(mixed)"
}

func summarizeRole(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	if counter.HasWrite && !counter.HasRead && !counter.HasExec {
		return "write"
	}
	if counter.HasExec && !counter.HasRead && !counter.HasWrite {
		return "exec"
	}
	if counter.HasRead && !counter.HasWrite && !counter.HasExec {
		return "read"
	}
	return "mixed"
}

func describeRoles(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	roles := []string{}
	if counter.HasWrite {
		roles = append(roles, "write")
	}
	if counter.HasExec {
		roles = append(roles, "exec")
	}
	if counter.HasRead {
		roles = append(roles, "read")
	}
	if len(roles) == 0 {
		return "mixed"
	}
	return strings.Join(roles, "+")
}

func dominantRole(counter *objectRoleCounter) string {
	if counter == nil {
		return "mixed"
	}
	if counter.HasExec {
		return "exec"
	}
	if counter.HasWrite {
		return "write"
	}
	if counter.HasRead {
		return "read"
	}
	return "mixed"
}

func roleCount(counter *objectRoleCounter, role string) int {
	if counter == nil {
		return 0
	}
	switch strings.ToLower(role) {
	case "exec":
		return counter.ExecCount
	case "write":
		return counter.WriteCount
	case "read":
		return counter.ReadCount
	default:
		return counter.ReadCount + counter.WriteCount + counter.ExecCount
	}
}

func WriteFunctionSummary(path string, rows []FunctionSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "RelPath", "Func", "LineStart", "LineEnd", "TotalQueries", "TotalSelect", "TotalInsert", "TotalUpdate", "TotalDelete", "TotalTruncate", "TotalExec", "TotalWrite", "TotalDynamic", "DynamicSignatures", "TotalObjects", "ObjectsUsed", "HasCrossDb", "DbList"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			r.Func,
			fmt.Sprintf("%d", r.LineStart),
			fmt.Sprintf("%d", r.LineEnd),
			fmt.Sprintf("%d", r.TotalQueries),
			fmt.Sprintf("%d", r.TotalSelect),
			fmt.Sprintf("%d", r.TotalInsert),
			fmt.Sprintf("%d", r.TotalUpdate),
			fmt.Sprintf("%d", r.TotalDelete),
			fmt.Sprintf("%d", r.TotalTruncate),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalWrite),
			fmt.Sprintf("%d", r.TotalDynamic),
			r.DynamicSig,
			fmt.Sprintf("%d", r.TotalObjects),
			r.ObjectsUsed,
			boolToStr(r.HasCrossDb),
			r.DbList,
		}
		if err := w.Write(rec); err != nil {
			return err
		}
	}
	w.Flush()
	return w.Error()
}

func WriteObjectSummary(path string, rows []ObjectSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "FullObjectName", "BaseName", "Roles", "TotalReads", "TotalWrites", "TotalExec", "TotalFuncs", "ExampleFuncs", "IsPseudoObject", "PseudoKind", "HasCrossDb", "DbList"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.FullObjectName,
			r.BaseName,
			r.Roles,
			fmt.Sprintf("%d", r.TotalReads),
			fmt.Sprintf("%d", r.TotalWrites),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalFuncs),
			r.ExampleFuncs,
			boolToStr(r.IsPseudoObject),
			r.PseudoKind,
			boolToStr(r.HasCrossDb),
			r.DbList,
		}
		if err := w.Write(rec); err != nil {
			return err
		}
	}
	w.Flush()
	return w.Error()
}

func WriteFormSummary(path string, rows []FormSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "RelPath", "TotalQueries", "TotalWrite", "TotalExec", "TotalDynamic", "TotalObjects", "TopObjects", "HasCrossDb", "DbList"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			fmt.Sprintf("%d", r.TotalQueries),
			fmt.Sprintf("%d", r.TotalWrite),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalDynamic),
			fmt.Sprintf("%d", r.TotalObjects),
			r.TopObjects,
			boolToStr(r.HasCrossDb),
			r.DbList,
		}
		if err := w.Write(rec); err != nil {
			return err
		}
	}
	w.Flush()
	return w.Error()
}

func parseBool(s string) bool {
	return strings.EqualFold(strings.TrimSpace(s), "true")
}

func parseInt(s string) int {
	v, _ := strconv.Atoi(strings.TrimSpace(s))
	return v
}

func parseList(raw string) []string {
	parts := strings.Split(raw, ";")
	var res []string
	seen := make(map[string]struct{})
	for _, p := range parts {
		item := strings.TrimSpace(p)
		if item == "" {
			continue
		}
		if _, ok := seen[item]; ok {
			continue
		}
		seen[item] = struct{}{}
		res = append(res, item)
	}
	sort.Strings(res)
	return res
}

func pick(rec []string, idx map[string]int, key string) string {
	if col, ok := idx[key]; ok && col < len(rec) {
		return rec[col]
	}
	return ""
}

func isDynamicQuery(q QueryRow) bool {
	if q.IsDynamic {
		return true
	}
	lowerClean := strings.ToLower(q.SqlClean)
	lowerRaw := strings.ToLower(q.RawSql)
	if strings.Contains(lowerClean, "<expr>") || strings.Contains(lowerClean, "<dynamic") {
		return true
	}
	if strings.Contains(lowerRaw, "<dynamic") {
		return true
	}
	return false
}

func setToSortedSlice(m map[string]struct{}) []string {
	if len(m) == 0 {
		return nil
	}
	res := make([]string, 0, len(m))
	for v := range m {
		res = append(res, v)
	}
	sort.Strings(res)
	return res
}

func normalizeQueryFuncs(queries []QueryRow) []QueryRow {
	if len(queries) == 0 {
		return nil
	}
	res := make([]QueryRow, len(queries))
	for i, q := range queries {
		q.Func = resolveFuncName(q.Func, q.RelPath, q.LineStart)
		res[i] = q
	}
	return res
}

func resolveFuncName(raw, rel string, line int) string {
	name := strings.TrimSpace(raw)
	if name != "" && !strings.EqualFold(name, "<unknown-func>") {
		return name
	}
	anchor := strings.TrimSpace(rel)
	if anchor == "" {
		anchor = "<file-scope>"
	}
	if line <= 0 {
		return fmt.Sprintf("%s@L?", anchor)
	}
	return fmt.Sprintf("%s@L%d", anchor, line)
}

func functionKey(app, rel, file, fn string) string {
	return strings.Join([]string{app, rel, file, fn}, "|")
}

func splitFunctionKey(key string) (string, string, string, string) {
	parts := strings.SplitN(key, "|", 4)
	for len(parts) < 4 {
		parts = append(parts, "")
	}
	return parts[0], parts[1], parts[2], parts[3]
}

func queryObjectKey(app, rel, file, hash string) string {
	return strings.Join([]string{app, rel, file, hash}, "|")
}

func objectKey(app, rel, file, db, schema, base string) string {
	return strings.Join([]string{app, rel, file, db, schema, base}, "|")
}

func splitObjectKey(key string) (string, string, string, string, string, string) {
	parts := strings.SplitN(key, "|", 6)
	for len(parts) < 6 {
		parts = append(parts, "")
	}
	return parts[0], parts[1], parts[2], parts[3], parts[4], parts[5]
}

func formKey(app, rel, file string) string {
	return strings.Join([]string{app, rel, file}, "|")
}

func splitFormKey(key string) (string, string, string) {
	parts := strings.SplitN(key, "|", 3)
	for len(parts) < 3 {
		parts = append(parts, "")
	}
	return parts[0], parts[1], parts[2]
}

func boolToStr(b bool) string {
	if b {
		return "true"
	}
	return "false"
}

func buildFullName(db, schema, base string) string {
	var parts []string
	if db != "" {
		parts = append(parts, db)
	}
	if schema != "" {
		parts = append(parts, schema)
	}
	if base != "" {
		parts = append(parts, base)
	}
	return strings.Join(parts, ".")
}

func normalizeFuncName(raw string) string {
	name := strings.TrimSpace(raw)
	if name == "" {
		return "<unknown-func>"
	}
	lower := strings.ToLower(name)
	if _, banned := forbiddenFuncNames()[lower]; banned {
		return "<unknown-func>"
	}
	if isAllUpperShort(name) {
		return "<unknown-func>"
	}
	if isLikelyFuncName(name) {
		return name
	}
	return "<unknown-func>"
}

func forbiddenFuncNames() map[string]struct{} {
	return map[string]struct{}{
		"exception": {},
		"convert":   {},
		"cast":      {},
		"in":        {},
		"isnull":    {},
		"nvarchar":  {},
		"varchar":   {},
		"bigint":    {},
		"len":       {},
		"openxml":   {},
		"count":     {},
		"select":    {},
		"from":      {},
		"where":     {},
		"group":     {},
		"order":     {},
		"join":      {},
		"left":      {},
		"right":     {},
		"inner":     {},
		"outer":     {},
		"insert":    {},
		"update":    {},
		"delete":    {},
		"truncate":  {},
		"exec":      {},
		"coalesce":  {},
		"substring": {},
		"case":      {},
		"when":      {},
		"then":      {},
		"else":      {},
		"end":       {},
	}
}

func isLikelyFuncName(name string) bool {
	if strings.HasPrefix(name, "<") && strings.HasSuffix(name, ">") {
		return false
	}
	forbidden := forbiddenFuncNames()
	if _, ok := forbidden[strings.ToLower(name)]; ok {
		return false
	}
	for _, r := range name {
		if !(r == '_' || r == '.' || r == '@' || ('0' <= r && r <= '9') || ('A' <= r && r <= 'Z') || ('a' <= r && r <= 'z')) {
			return false
		}
	}
	first := name[0]
	if (first >= '0' && first <= '9') || first == '.' {
		return false
	}
	return true
}

func isAllUpperShort(name string) bool {
	if len(name) == 0 || len(name) > 3 {
		return false
	}
	hasLetter := false
	for _, r := range name {
		if unicode.IsLetter(r) {
			hasLetter = true
			if !unicode.IsUpper(r) {
				return false
			}
		}
	}
	return hasLetter
}

func isDynamicBaseName(base string) bool {
	trimmed := strings.ToLower(strings.TrimSpace(base))
	if trimmed == "<dynamic-sql>" {
		return true
	}
	return strings.HasPrefix(trimmed, "<dynamic-object")
}

func pseudoObjectInfo(base string) (bool, string) {
	trimmed := strings.TrimSpace(base)
	lower := strings.ToLower(trimmed)
	if isDynamicBaseName(trimmed) {
		return true, "dynamic-sql"
	}
	if strings.HasPrefix(lower, "<") && strings.HasSuffix(lower, ">") {
		kind := strings.TrimSuffix(strings.TrimPrefix(lower, "<"), ">")
		kind = strings.TrimSpace(kind)
		if kind == "" {
			kind = "unknown"
		}
		return true, kind
	}
	return false, ""
}

func choosePseudoKind(current, candidate string) string {
	pri := func(kind string) int {
		switch strings.ToLower(strings.TrimSpace(kind)) {
		case "dynamic-sql":
			return 3
		case "dynamic-object":
			return 2
		case "unknown":
			return 1
		default:
			return 0
		}
	}

	if pri(candidate) > pri(current) {
		return strings.ToLower(strings.TrimSpace(candidate))
	}
	return strings.ToLower(strings.TrimSpace(current))
}

func defaultPseudoKind(kind string) string {
	k := strings.ToLower(strings.TrimSpace(kind))
	if k == "" {
		return "unknown"
	}
	return k
}

func isWriteDml(dml string) bool {
	switch strings.ToUpper(strings.TrimSpace(dml)) {
	case "INSERT", "UPDATE", "DELETE", "TRUNCATE", "EXEC":
		return true
	default:
		return false
	}
}
