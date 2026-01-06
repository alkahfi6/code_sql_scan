package scan

import "testing"

func TestPseudoCardinalityOffendersSorted(t *testing.T) {
	cands := []SqlCandidate{
		{
			RelPath:   "r1",
			Func:      "f1",
			QueryHash: "q1",
			Objects: []ObjectToken{
				{BaseName: "<dynamic-object>", FullName: "<dynamic-object>", PseudoKind: "dynamic-object", IsPseudoObject: true, RepresentativeLine: 1},
				{BaseName: "<dynamic-object>", FullName: "<dynamic-object>", PseudoKind: "dynamic-object", IsPseudoObject: true, RepresentativeLine: 2},
			},
		},
		{
			RelPath:   "r2",
			Func:      "f2",
			QueryHash: "q2",
			Objects: []ObjectToken{
				{BaseName: "<dynamic-object>", FullName: "<dynamic-object>", PseudoKind: "dynamic-object", IsPseudoObject: true, RepresentativeLine: 1},
				{BaseName: "<dynamic-object>", FullName: "<dynamic-object>", PseudoKind: "dynamic-object", IsPseudoObject: true, RepresentativeLine: 2},
				{BaseName: "<dynamic-object>", FullName: "<dynamic-object>", PseudoKind: "dynamic-object", IsPseudoObject: true, RepresentativeLine: 3},
			},
		},
	}

	offenders := logPseudoCardinalityWarningsWithThreshold(cands, 1, true)
	if len(offenders) != 2 {
		t.Fatalf("expected 2 offenders, got %d", len(offenders))
	}
	if offenders[0].funcKey != "r2|f2" || offenders[0].count != 3 {
		t.Fatalf("unexpected first offender: %+v", offenders[0])
	}
	if offenders[1].funcKey != "r1|f1" || offenders[1].count != 2 {
		t.Fatalf("unexpected second offender: %+v", offenders[1])
	}
}
