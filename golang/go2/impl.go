//go:build samples
// +build samples

package mssql

import (
	"context"
	"sync"
	"time"

	"gitlab.ocbcnisp.com/app/internal/models/dbmodel"
	"gitlab.ocbcnisp.com/app/internal/repository/dbutil"
	"gitlab.ocbcnisp.com/app/internal/repository/sibs"
	"gitlab.ocbcnisp.com/app/pkg/mapper"
	"gitlab.ocbcnisp.com/app/pkg/utility"
)

type SibsDB struct {
	db dbmodel.SQLType
}

func NewAntSibsDB(dbParam dbmodel.SQLType) *SibsDB {
	return &SibsDB{
		db: dbParam,
	}
}

func (s *SibsDB) GetCurrWorkingDate(ctx context.Context) (time.Time, error) {
	return dbutil.QuerySingleRow[time.Time](ctx, s.db.ConnRead, QueryGetCurrWorkingDate)
}

func (s *SibsDB) GetCFCOUN(ctx context.Context, cif string) (string, error) {
	return dbutil.QuerySingleRow[string](ctx, s.db.ConnRead, QueryGetCFCOUN, cif)
}

func (s *SibsDB) GetBIRates(ctx context.Context) ([]sibs.BIRate, error) {
	res, err := dbutil.QueryMultipleRows[BIRateDB](ctx, s.db.ConnRead, QueryGetBIRate)
	if err != nil {
		return nil, err
	}

	return mapper.MapSlice2(res, ToDomainBIRate), nil
}

func (c *SibsDB) GetValasMappingParameter(ctx context.Context) ([]sibs.ValasMappingParameter, error) {
	query := `
		SELECT currency, buy_sell_code, product,
		product_category, transaction_limit_flag, transaction_limit_amount,
		monthly_limit_flag, monthly_limit_amount, lcs_flag, rate_currency,
		negara_mitra
		FROM dbo.valas_mapping_parameter
		WHERE  lcs_flag = 1
		AND transaction_limit_flag = 1
	`
	res, err := dbutil.QueryMultipleRows[ValasMappingParameterDB](ctx, c.db.ConnRead, query)
	if err != nil {
		return nil, err
	}

	return *mapper.MapSlice(&res, ToDomainValasMappingParameter), nil
}

func (c *SibsDB) GetVlsRate(ctx context.Context, period time.Time) ([]sibs.VLSRate, error) {
	query := `
		select Period, JPeriod, CurrencyCode, BiRateExchange
		USDRateExchange, JISDORRate, USDRateExcJisdor
		from dbo.VLSRate_TM
		where Period = ?
			`
	args := []any{utility.GetFormatYyyyMmDd(period)}

	res, err := dbutil.QueryMultipleRows[VLSRateDB](ctx, c.db.ConnRead, query, args...)
	if err != nil {
		return nil, err
	}
	return *mapper.MapSlice(&res, ToDomainRate), nil
}

func (c *SibsDB) GetVlsTransaction(ctx context.Context, dealno []string) (result []sibs.VLSTransctions, err error) {

	var wg sync.WaitGroup
	var vlstran []VLSTransctionsDB
	var vlstrantoday []VLSTransctionsDB
	errCh := make(chan error, 4)

	wg.Add(1)
	go func() {
		defer wg.Done()
		var erru error
		vlstran, erru = dbutil.QueryMultipleRowsInArg[VLSTransctionsDB](ctx, c.db.ConnRead,
			QueryVlsTran, dealno)
		if erru != nil {
			errCh <- erru
		}
	}()
	wg.Add(1)
	go func() {
		defer wg.Done()
		var errt error
		vlstran, errt = dbutil.QueryMultipleRowsInArg[VLSTransctionsDB](ctx, c.db.ConnRead,
			queryVlsTranToday, dealno)
		if errt != nil {
			errCh <- errt
		}
	}()
	wg.Wait()
	close(errCh)
	if len(errCh) > 0 {
		err = <-errCh
		return nil, err
	}

	allvlstran := append(vlstran, vlstrantoday...)

	return *mapper.MapSlice(&allvlstran, ToDomainVlsTran), nil
}
