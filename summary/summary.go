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
	Func       string
	QueryHash  string
	UsageKind  string
	IsWrite    bool
	IsDynamic  bool
	HasCrossDb bool
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
	File          string
	Func          string
	TotalQueries  int
	TotalExec     int
	TotalSelect   int
	TotalInsert   int
	TotalUpdate   int
	TotalDelete   int
	TotalTruncate int
	TotalDynamic  int
	TotalWrite    int
	HasCrossDb    bool
	ObjectsUsed   string
	LineStart     int
	LineEnd       int
}

// ObjectSummaryRow represents aggregated information per database object.
type ObjectSummaryRow struct {
	AppName         string
	RelPath         string
	File            string
	DbName          string
	SchemaName      string
	BaseName        string
	FullObjectName  string
	UsedInFuncs     string
	DmlKinds        string
	Roles           string
	TotalReads      int
	TotalWrites     int
	IsCrossDb       bool
	IsDynamicObject bool
	DynamicKind     string
	IsPseudoObject  bool
	PseudoKind      string
}

// FormSummaryRow represents aggregated information per file/form.
type FormSummaryRow struct {
	AppName              string
	RelPath              string
	File                 string
	TotalFunctionsWithDB int
	TotalQueries         int
	TotalObjects         int
	TotalExec            int
	TotalWrite           int
	TotalDynamic         int
	DistinctObjectsUsed  int
	HasDbAccess          bool
	HasCrossDb           bool
	TopObjects           string
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
			AppName:   rec[idx["AppName"]],
			RelPath:   rec[idx["RelPath"]],
			File:      rec[idx["File"]],
			Func:      rec[idx["Func"]],
			QueryHash: rec[idx["QueryHash"]],
			UsageKind: rec[idx["UsageKind"]],
			IsWrite:   parseBool(rec[idx["IsWrite"]]),
			IsDynamic: parseBool(rec[idx["IsDynamic"]]),
		}
		if col, ok := idx["HasCrossDb"]; ok {
			row.HasCrossDb = parseBool(rec[col])
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
	grouped := make(map[string][]QueryRow)
	hashByFunc := make(map[string]map[string]struct{})
	queryByKey := make(map[string]QueryRow)

	for _, q := range queries {
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
		var hasRealObject bool
		var hasDynamicObject bool
		minLine := 0
		maxLine := 0
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
			if q.IsDynamic {
				totalDynamic++
			}
			if q.IsWrite {
				totalWrite++
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

		objSet := make(map[string]struct{})
		consumeObj := func(o ObjectRow) {
			name := strings.TrimSpace(o.BaseName)
			if name == "" {
				return
			}
			objSet[name] = struct{}{}
			isPseudo, _ := pseudoObjectInfo(name)
			if isPseudo {
				hasDynamicObject = true
			} else {
				hasRealObject = true
			}
			if o.IsCrossDb {
				hasCross = true
			}
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
		if !hasCross {
			for _, q := range qRows {
				if q.HasCrossDb {
					hasCross = true
					break
				}
			}
		}
		objNames := setToSortedSlice(objSet)
		objectsUsed := strings.Join(objNames, ";")
		dynamicOnly := (hasDynamicObject && !hasRealObject) || (len(objNames) == 0 && totalDynamic > 0)
		if (totalWrite > 0 || totalExec > 0) && objectsUsed == "" {
			if dynamicOnly {
				objectsUsed = "<dynamic-sql>"
			} else {
				objectsUsed = "<dynamic-sql>"
			}
		}

		result = append(result, FunctionSummaryRow{
			AppName:       app,
			RelPath:       rel,
			File:          file,
			Func:          fn,
			TotalQueries:  len(qRows),
			TotalExec:     totalExec,
			TotalSelect:   totalSelect,
			TotalInsert:   totalInsert,
			TotalUpdate:   totalUpdate,
			TotalDelete:   totalDelete,
			TotalTruncate: totalTruncate,
			TotalDynamic:  totalDynamic,
			TotalWrite:    totalWrite,
			HasCrossDb:    hasCross,
			ObjectsUsed:   objectsUsed,
			LineStart:     minLine,
			LineEnd:       maxLine,
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.AppName != b.AppName {
			return a.AppName < b.AppName
		}
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.File != b.File {
			return a.File < b.File
		}
		return a.Func < b.Func
	})

	return result, nil
}

func BuildObjectSummary(queries []QueryRow, objects []ObjectRow) ([]ObjectSummaryRow, error) {
	grouped := make(map[string][]ObjectRow)
	for _, o := range objects {
		key := objectKey(o.AppName, o.RelPath, o.File, o.DbName, o.SchemaName, o.BaseName)
		grouped[key] = append(grouped[key], o)
	}

	queryByKey := make(map[string]QueryRow)
	for _, q := range queries {
		queryByKey[queryObjectKey(q.AppName, q.RelPath, q.File, q.QueryHash)] = q
	}

	var result []ObjectSummaryRow
	for key, objs := range grouped {
		app, rel, file, db, schema, base := splitObjectKey(key)
		funcSet := make(map[string]struct{})
		dmlSet := make(map[string]struct{})
		roleSet := make(map[string]struct{})
		totalReads := 0
		totalWrites := 0
		hasCross := false
		isDynamicObj := isDynamicBaseName(base)
		dynamicKind := ""
		if isDynamicObj {
			dynamicKind = "unknown-target"
		}
		isPseudo, pseudoKind := pseudoObjectInfo(base)
		if isPseudo && pseudoKind == "" {
			pseudoKind = "unknown"
		}

		for _, o := range objs {
			dmlSet[strings.ToUpper(o.DmlKind)] = struct{}{}
			roleSet[o.Role] = struct{}{}
			if o.IsWrite {
				totalWrites++
			} else {
				totalReads++
			}
			if o.IsCrossDb {
				hasCross = true
			}
			if o.IsObjectNameDyn {
				isDynamicObj = true
				if o.PseudoKind != "" {
					dynamicKind = o.PseudoKind
				}
				if dynamicKind == "" {
					dynamicKind = "unknown-target"
				}
			}
			if o.IsPseudoObject {
				isPseudo = true
				pseudoKind = choosePseudoKind(pseudoKind, defaultPseudoKind(o.PseudoKind))
			}
			qKey := queryObjectKey(o.AppName, o.RelPath, o.File, o.QueryHash)
			if q, ok := queryByKey[qKey]; ok {
				funcSet[q.Func] = struct{}{}
			}
		}

		if isPseudo && pseudoKind == "" {
			pseudoKind = "unknown"
		}

		result = append(result, ObjectSummaryRow{
			AppName:         app,
			RelPath:         rel,
			File:            file,
			DbName:          db,
			SchemaName:      schema,
			BaseName:        base,
			FullObjectName:  buildFullName(db, schema, base),
			UsedInFuncs:     strings.Join(setToSortedSlice(funcSet), ";"),
			DmlKinds:        strings.Join(setToSortedSlice(dmlSet), ";"),
			Roles:           strings.Join(setToSortedSlice(roleSet), ";"),
			TotalReads:      totalReads,
			TotalWrites:     totalWrites,
			IsCrossDb:       hasCross,
			IsDynamicObject: isDynamicObj,
			DynamicKind:     dynamicKind,
			IsPseudoObject:  isPseudo,
			PseudoKind:      pseudoKind,
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.AppName != b.AppName {
			return a.AppName < b.AppName
		}
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		if a.File != b.File {
			return a.File < b.File
		}
		if a.DbName != b.DbName {
			return a.DbName < b.DbName
		}
		if a.SchemaName != b.SchemaName {
			return a.SchemaName < b.SchemaName
		}
		return a.BaseName < b.BaseName
	})

	return result, nil
}

func BuildFormSummary(queries []QueryRow, objects []ObjectRow) ([]FormSummaryRow, error) {
	groupQueries := make(map[string][]QueryRow)
	for _, q := range queries {
		key := formKey(q.AppName, q.RelPath, q.File)
		groupQueries[key] = append(groupQueries[key], q)
	}

	objectSetByForm := make(map[string]map[string]struct{})
	hasCrossByForm := make(map[string]bool)
	topObjectStats := make(map[string]map[string]*objectRoleCounter)
	for _, o := range objects {
		key := formKey(o.AppName, o.RelPath, o.File)
		if _, ok := objectSetByForm[key]; !ok {
			objectSetByForm[key] = make(map[string]struct{})
		}
		objectSetByForm[key][o.BaseName] = struct{}{}
		if o.IsCrossDb {
			hasCrossByForm[key] = true
		}

		baseName := strings.TrimSpace(o.BaseName)
		isPseudo, _ := pseudoObjectInfo(baseName)
		if isPseudo {
			continue
		}
		if baseName == "" {
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
		app, rel, file := splitFormKey(key)
		funcSet := make(map[string]struct{})
		totalExec := 0
		totalWrite := 0
		totalDynamic := 0
		for _, q := range qRows {
			funcSet[normalizeFuncName(q.Func)] = struct{}{}
			if strings.ToUpper(q.UsageKind) == "EXEC" {
				totalExec++
			}
			if q.IsWrite {
				totalWrite++
			}
			if q.IsDynamic {
				totalDynamic++
			}
		}
		objSet := objectSetByForm[key]
		distinctObjects := len(objSet)
		hasCross := hasCrossByForm[key]
		totalObjects := 0
		for _, o := range objects {
			if o.AppName == app && o.RelPath == rel && o.File == file {
				totalObjects++
			}
		}

		topObjects := buildTopObjects(topObjectStats[key])
		hasDbAccess := distinctObjects > 0
		if !hasDbAccess {
			topObjects = ""
		}

		result = append(result, FormSummaryRow{
			AppName:              app,
			RelPath:              rel,
			File:                 file,
			TotalFunctionsWithDB: len(funcSet),
			TotalQueries:         len(qRows),
			TotalObjects:         totalObjects,
			TotalExec:            totalExec,
			TotalWrite:           totalWrite,
			TotalDynamic:         totalDynamic,
			DistinctObjectsUsed:  distinctObjects,
			HasDbAccess:          hasDbAccess,
			HasCrossDb:           hasCross,
			TopObjects:           topObjects,
		})
	}

	sort.Slice(result, func(i, j int) bool {
		a, b := result[i], result[j]
		if a.AppName != b.AppName {
			return a.AppName < b.AppName
		}
		if a.RelPath != b.RelPath {
			return a.RelPath < b.RelPath
		}
		return a.File < b.File
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
	if o.IsWrite || isWriteDml(o.DmlKind) {
		c.HasWrite = true
		c.WriteCount++
	}
	if strings.EqualFold(o.Role, "exec") || strings.EqualFold(o.DmlKind, "exec") {
		c.HasExec = true
		c.ExecCount++
	}
	if !o.IsWrite && !strings.EqualFold(o.Role, "exec") {
		c.HasRead = true
		c.ReadCount++
	}
}

func buildTopObjects(stats map[string]*objectRoleCounter) string {
	if len(stats) == 0 {
		return ""
	}
	type top struct {
		name       string
		writeScore int
		readScore  int
		label      string
	}
	var tops []top
	for name, counter := range stats {
		tops = append(tops, top{
			name:       name,
			writeScore: counter.WriteCount + counter.ExecCount,
			readScore:  counter.ReadCount,
			label:      classifyObjectUsage(counter),
		})
	}
	sort.Slice(tops, func(i, j int) bool {
		if tops[i].writeScore != tops[j].writeScore {
			return tops[i].writeScore > tops[j].writeScore
		}
		if tops[i].readScore != tops[j].readScore {
			return tops[i].readScore > tops[j].readScore
		}
		return tops[i].name < tops[j].name
	})
	if len(tops) > 3 {
		tops = tops[:3]
	}
	parts := make([]string, 0, len(tops))
	for _, t := range tops {
		parts = append(parts, fmt.Sprintf("%s %s", t.name, t.label))
	}
	return strings.Join(parts, ", ")
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

func WriteFunctionSummary(path string, rows []FunctionSummaryRow) error {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	w := csv.NewWriter(f)
	header := []string{"AppName", "RelPath", "File", "Func", "TotalQueries", "TotalExec", "TotalSelect", "TotalInsert", "TotalUpdate", "TotalDelete", "TotalTruncate", "TotalDynamic", "TotalWrite", "HasCrossDb", "ObjectsUsed", "LineStart", "LineEnd"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			r.File,
			r.Func,
			fmt.Sprintf("%d", r.TotalQueries),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalSelect),
			fmt.Sprintf("%d", r.TotalInsert),
			fmt.Sprintf("%d", r.TotalUpdate),
			fmt.Sprintf("%d", r.TotalDelete),
			fmt.Sprintf("%d", r.TotalTruncate),
			fmt.Sprintf("%d", r.TotalDynamic),
			fmt.Sprintf("%d", r.TotalWrite),
			boolToStr(r.HasCrossDb),
			r.ObjectsUsed,
			fmt.Sprintf("%d", r.LineStart),
			fmt.Sprintf("%d", r.LineEnd),
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
	header := []string{"AppName", "RelPath", "File", "DbName", "SchemaName", "BaseName", "FullObjectName", "UsedInFuncs", "DmlKinds", "Roles", "TotalReads", "TotalWrites", "IsCrossDb", "IsDynamicObject", "DynamicKind", "IsPseudoObject", "PseudoKind"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			r.File,
			r.DbName,
			r.SchemaName,
			r.BaseName,
			r.FullObjectName,
			r.UsedInFuncs,
			r.DmlKinds,
			r.Roles,
			fmt.Sprintf("%d", r.TotalReads),
			fmt.Sprintf("%d", r.TotalWrites),
			boolToStr(r.IsCrossDb),
			boolToStr(r.IsDynamicObject),
			r.DynamicKind,
			boolToStr(r.IsPseudoObject),
			r.PseudoKind,
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
	header := []string{"AppName", "RelPath", "File", "TotalFunctionsWithDB", "TotalQueries", "TotalObjects", "TotalExec", "TotalWrite", "TotalDynamic", "DistinctObjectsUsed", "HasDbAccess", "HasCrossDb", "TopObjects"}
	if err := w.Write(header); err != nil {
		return err
	}
	for _, r := range rows {
		rec := []string{
			r.AppName,
			r.RelPath,
			r.File,
			fmt.Sprintf("%d", r.TotalFunctionsWithDB),
			fmt.Sprintf("%d", r.TotalQueries),
			fmt.Sprintf("%d", r.TotalObjects),
			fmt.Sprintf("%d", r.TotalExec),
			fmt.Sprintf("%d", r.TotalWrite),
			fmt.Sprintf("%d", r.TotalDynamic),
			fmt.Sprintf("%d", r.DistinctObjectsUsed),
			boolToStr(r.HasDbAccess),
			boolToStr(r.HasCrossDb),
			r.TopObjects,
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
