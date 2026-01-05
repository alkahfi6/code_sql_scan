//go:build samples
// +build samples

package dbobox

import (
	"context"
	"strings"

	dbutil "gitlab.ocbcnisp.com/app/internal/database"
	"gitlab.ocbcnisp.com/app/internal/replicate"
	"gitlab.ocbcnisp.com/app/internal/utility"
)

func (dbobox *DBObox) UpdateControlTableResikoPasar(ctx context.Context, param1, param2 string) error {
	var err error
	db := dbobox.DBTRSRetail.ConnWrite
	err = dbutil.ExecStoredProcedure(ctx, db, SPUpdateControlTableResikoPasar, param1, param2)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) GetOboxPreviousWorkingDate(ctx context.Context) (string, error) {
	var err error
	db := dbobox.DBTRSRetail.ConnRead
	res, err := dbutil.QuerySingleRow[OboxGetPrevDate](ctx, db, QueryGetPreviousWorkingDate)
	if err != nil {
		return "", err
	}
	return res.WorkingDate.String, err
}

func (dbobox *DBObox) DeleteDuplicateConfo(ctx context.Context, mkT string) error {
	var err error
	db := dbobox.DBTRSRetail.ConnWrite
	var queryDelete string
	if mkT == MK005 {
		queryDelete = QueryDeleteDuplicateDataConfoMK005
	} else if mkT == MK006 {
		queryDelete = QueryDeleteDuplicateDataConfoMK006
	}
	_, err = dbutil.ExecNonQuery(ctx, db, queryDelete)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) ExecuteSPMurexTConfo(ctx context.Context, mkt string) error {
	var err error
	db := dbobox.DBTRSRetail.ConnWrite
	var queryConfoMK string
	switch mkt {
	case MK005:
		queryConfoMK = SPTRSRetailTConfoMK005A
	case MK006:
		queryConfoMK = SPTRSRetailTConfoMK006A
	}
	_, err = dbutil.ExecNonQuery(ctx, db, queryConfoMK)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) GetNamaSSB(ctx context.Context) (string, error) {
	var err error
	db := dbobox.DBReplicate.ConnRead
	res, err := dbutil.QuerySingleRow[string](ctx, db, QueryGetNameSSB)
	if err != nil {
		return "", err
	}
	return res, err
}

func (dbobox *DBObox) DeleteAvgPrice(ctx context.Context) error {
	var err error
	db := dbobox.DBReplicate.ConnWrite
	_, err = dbutil.ExecNonQuery(ctx, db, QueryDeleteAvgPrice)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) UpdateTableMK(ctx context.Context, mk string) error {
	var err error
	db := dbobox.DBTRSRetail.ConnWrite
	var queryUpdate string
	if mk == MK006 {
		queryUpdate = SPTrsRetailMK006UpdateValue
	} else if mk == MK005 {
		queryUpdate = SPTRSRetailMK005AUpdateJumlah
	}
	err = dbutil.ExecStoredProcedure(ctx, db, queryUpdate)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) ExecSPMK(ctx context.Context, mk string) error {
	var err error
	db := dbobox.DBTRSRetail.ConnWrite
	var sp string
	if mk == MK002 {
		sp = SPTRSRetailMK002A
	} else if mk == MK002Detail {
		sp = SPTRSRetailMK002ADetail
	} else if mk == MK005 {
		sp = SPTRSRetailMK005A
	} else if mk == MK006 {
		sp = SPTRSRetailMK006A
	}

	err = dbutil.ExecStoredProcedure(ctx, db, sp)
	if err != nil {
		return err
	}
	return nil
}

func (dbobox *DBObox) GetFlatFileData(ctx context.Context, mk string) (*strings.Reader, error) {
	db := dbobox.DBTRSRetail.ConnRead
	var spFlatFile string
	var input *strings.Reader
	switch mk {
	case MK006:
		spFlatFile = SPFlatFileMk006
		res, err := dbutil.ExecStoredProcedureWithReturn[string](ctx, db, spFlatFile)
		if err != nil {
			replicate.Log(ctx, utility.GetFuncName(), err, "error")
			return input, err
		}
		input = strings.NewReader(strings.Join(res, "\n") + "\n")
	case MK005:
		spFlatFile = SPFlatFileMk005
		results, err := dbutil.ExecStoredProcedureWithReturn[MKFlatFileResult](ctx, db, spFlatFile)
		if err != nil {
			replicate.Log(ctx, utility.GetFuncName(), err, "error")
			return input, err
		}
		var res []string
		for _, result := range results {
			if result.Content.Valid {
				res = append(res, result.Content.String)
			}
		}
		input = strings.NewReader(strings.Join(res, "\n") + "\n")
	case MK002:
		spFlatFile = SPFlatFileMK002
		results, err := dbutil.ExecStoredProcedureWithReturn[MKFlatFileResult](ctx, db, spFlatFile)
		if err != nil {
			replicate.Log(ctx, utility.GetFuncName(), err, "error")
			return input, err
		}
		var res []string
		for _, result := range results {
			if result.Content.Valid {
				res = append(res, result.Content.String)
			}
		}
		input = strings.NewReader(strings.Join(res, "\n") + "\n")
	}
	return input, nil
}

func (dbobox *DBObox) GetFlatFileUnstructuredData(ctx context.Context, mk string) ([]string, error) {
	db := dbobox.DBTRSRetail.ConnRead // rep.Cfg.SQLTrsRetail.ConnRead
	var spFlatFile string
	switch mk {
	case MK006:
		spFlatFile = SPFlatFileMK006UN
	case MK005:
		spFlatFile = SPFlatFileMK005UN
	case MK002:
		spFlatFile = SPFlatFileMK002UN
	}

	resMkUN, err := dbutil.ExecStoredProcedureWithReturn[string](ctx, db, spFlatFile)
	if err != nil {
		replicate.Log(ctx, utility.GetFuncName(), err, "error")
		return nil, err
	}
	return resMkUN, nil
}

func (dbobox *DBObox) GetTableRetry(ctx context.Context) (*[]string, error) {
	var err error
	db := dbobox.DBTRSRetail.ConnRead
	query := "select process_type from dbo.control_table_resiko_pasar where success_bit = 0 "
	listRetry, err := dbutil.QueryMultipleRows[string](ctx, db, query)
	if err != nil {
		return nil, err
	}
	return &listRetry, nil
}

func (dbobox DBObox) DeleteDuplicateDataMK(ctx context.Context, mk string) error {
	var err error
	db := dbobox.DBTRSRetail.ConnWrite
	var queryDelDupMK string
	switch mk {
	case MK005:
		queryDelDupMK = QueryDeleteDuplicateDataMK005A
	case MK006:
		queryDelDupMK = QueryDeleteDuplicateDataMK006A
	}
	_, err = dbutil.ExecNonQuery(ctx, db, queryDelDupMK)
	if err != nil {
		replicate.Log(ctx, utility.GetFuncName(), err, "error")
		return err
	}
	return nil
}
