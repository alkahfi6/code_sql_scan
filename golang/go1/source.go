//go:build samples
// +build samples

package dbobox

import (
	"context"
	"errors"
	"strings"

	dbutil "gitlab.ocbcnisp.com/app/internal/database"
	"gitlab.ocbcnisp.com/app/internal/models/dbmodel"
	"gitlab.ocbcnisp.com/app/internal/replicate"
	"gitlab.ocbcnisp.com/app/internal/utility"
)

func (dbobox *DBObox) ExecSPMurexMK(ctx context.Context, mk, getPrevWorkDate, schema string) error {
	var err error
	db := dbobox.DBMurex.ConnWrite
	var spMurex string
	switch mk {
	case "MK005":
		spMurex = strings.ReplaceAll(SPMurexMK005, "[[schema]]", schema)
	case "MK006":
		spMurex = strings.ReplaceAll(SPMurexMK006, "[[schema]]", schema)
	}
	err = dbutil.ExecStoredProcedureOracle(ctx, db, spMurex, getPrevWorkDate)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) ExecuteSPMurexConfoMK(ctx context.Context, mk, schema string) error {
	var err error
	db := dbobox.DBMurex.ConnWrite
	var spMurexConfo string
	switch mk {
	case "MK005":
		spMurexConfo = strings.ReplaceAll(SPMurexConfoMK005, "[[schema]]", schema)
	case "MK006":
		spMurexConfo = strings.ReplaceAll(SPMurexConfoMK006, "[[schema]]", schema)
	}
	err = dbutil.ExecStoredProcedureOracle(ctx, db, spMurexConfo)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) GetMK006AConfo(ctx context.Context, schema string) ([]*dbmodel.MurexConfoMK, error) {
	db := dbobox.DBMurex.ConnRead
	query := strings.ReplaceAll(QueryMUREXGetMK006AConfo, "[[schema]]", schema)
	listConfoMk006A, oerr := dbutil.QueryMultipleRows[*dbmodel.MurexConfoMK](ctx, db, query)
	if oerr != nil {
		return nil, oerr
	}
	return listConfoMk006A, nil
}

func (dbobox *DBObox) GetMK006A(ctx context.Context, schema string) ([]*dbmodel.MUREXMK006A, error) {
	db := dbobox.DBMurex.ConnRead
	query := strings.ReplaceAll(QueryMurexGetMK006A, "[[schema]]", schema)
	listMk006A, oerr := dbutil.QueryMultipleRows[*dbmodel.MUREXMK006A](ctx, db, query)
	if oerr != nil {
		return nil, oerr
	}
	return listMk006A, nil
}
func (dbobox *DBObox) GetMurexAvgPrice(ctx context.Context, schema, paramGetNameSSB string) ([]*AvgPriceMK006Actuate, error) {
	db := dbobox.DBMurexActuate.ConnRead
	query := strings.ReplaceAll(QueryMurexGetAvgPrice, "[[schema]]", schema)
	query = strings.ReplaceAll(query, "[[param]]", paramGetNameSSB)
	listData, oerr := dbutil.QueryMultipleRows[*AvgPriceMK006Actuate](ctx, db, query)
	if oerr != nil {
		return nil, oerr
	}

	return listData, nil
}

func (dbobox *DBObox) GetMK005AConfo(ctx context.Context, schema string) ([]*dbmodel.MurexConfoMK, error) {
	db := dbobox.DBMurex.ConnRead
	query := strings.ReplaceAll(QueryMurexGetMK005AConfo, "[[schema]]", schema)
	listConfoMk005A, oerr := dbutil.QueryMultipleRows[*dbmodel.MurexConfoMK](ctx, db, query)
	if oerr != nil {
		replicate.Log(ctx, utility.GetFuncName(), oerr, "error")
		return nil, oerr
	}
	return listConfoMk005A, nil
}

func (dbobox *DBObox) GetMK005A(ctx context.Context, schema string) ([]*dbmodel.MUREXMK005A, error) {
	db := dbobox.DBMurex.ConnRead
	query := strings.ReplaceAll(QueryMurexGetMK005A, "[[schema]]", schema)
	listMk005A, oerr := dbutil.QueryMultipleRows[*dbmodel.MUREXMK005A](ctx, db, query)
	if oerr != nil {
		replicate.Log(ctx, utility.GetFuncName(), oerr, "error")
		return nil, oerr
	}
	return listMk005A, nil
}

func (dbobox *DBObox) ValidateMKData(ctx context.Context, mk string) error {
	db := dbobox.DBTRSRetail.ConnRead
	var param string
	sp := SPTRSRetailValidateMK
	if mk == MK002 {
		param = "mk002_a"
	} else if mk == MK005 {
		param = "mk005_a"
	} else if mk == MK006 {
		param = "mk006_a"
	}
	res, err := dbutil.ExecStoredProcedureWithReturn[string](ctx, db, sp, param)
	if err != nil || res[0] == "1" {
		println("JOB IS SKIPPED Data Is %s Generated", mk)
		msg := "data " + mk + " is already generated"
		return errors.New(msg)
	}
	return nil
}
