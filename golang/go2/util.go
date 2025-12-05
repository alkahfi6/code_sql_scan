package mssql

import (
	"gitlab.ocbcnisp.com/app/internal/repository/sibs"
	"gitlab.ocbcnisp.com/app/pkg/mapper"
)

func ToDomainBIRate(src *BIRateDB, trg *sibs.BIRate) {
	trg.CurrencyCode = mapper.ToString(src.CurrencyCode)
	trg.Value = mapper.ToDecimal(src.Value)
}

func ToDomainValasMappingParameter(db *ValasMappingParameterDB) *sibs.ValasMappingParameter {
	return &sibs.ValasMappingParameter{
		Currency:                 mapper.ToString(db.Currency),
		Buy_sell_code:            mapper.ToString(db.Buy_sell_code),
		Product:                  mapper.ToString(db.Product),
		Product_category:         mapper.ToString(db.Product_category),
		Transaction_limit_flag:   mapper.ToBool(db.Transaction_limit_flag),
		Transaction_limit_amount: mapper.ToDecimal(db.Transaction_limit_amount),
		Monthly_limit_flag:       mapper.ToBool(db.Monthly_limit_flag),
		Monthly_limit_amount:     mapper.ToInt64(db.Monthly_limit_amount),
		Lcs_flag:                 mapper.ToBool(db.Lcs_flag),
		Rate_currency:            mapper.ToString(db.Rate_currency),
		Negara_mitra:             mapper.ToString(db.Negara_mitra),
	}
}

func ToDomainRate(db *VLSRateDB) *sibs.VLSRate {
	return &sibs.VLSRate{
		Period:           mapper.ToTime(db.Period),
		JPeriod:          mapper.ToInt64(db.JPeriod),
		CurrencyCode:     mapper.ToString(db.CurrencyCode),
		BiRateExchange:   mapper.ToDecimal(db.BiRateExchange),
		USDRateExchange:  mapper.ToDecimal(db.USDRateExchange),
		JISDORRate:       mapper.ToDecimal(db.JISDORRate),
		USDRateExcJisdor: mapper.ToDecimal(db.USDRateExcJisdor),
	}
}

func ToDomainVlsTran(db *VLSTransctionsDB) *sibs.VLSTransctions {
	return &sibs.VLSTransctions{
		Src:           mapper.ToString(db.Src),
		Dealno:        mapper.ToString(db.Dealno),
		Customer_id:   mapper.ToString(db.Customer_id),
		Acc_id:        mapper.ToString(db.Acc_id),
		Trx_branch:    mapper.ToString(db.Trx_branch),
		Trx_date:      mapper.ToTime(db.Trx_date),
		Trx_datetime:  mapper.ToTime(db.Trx_datetime),
		Currency_code: mapper.ToString(db.Currency_code),
		Amount:        mapper.ToDecimal(db.Amount),
		Rate:          mapper.ToDecimal(db.Rate),
		InUSD:         mapper.ToDecimal(db.InUSD),
		SourceKey:     mapper.ToString(db.SourceKey),
		JISDORRate:    mapper.ToDecimal(db.JISDORRate),
		InUSDJISDOR:   mapper.ToDecimal(db.InUSDJISDOR),
		M_pl_key:      mapper.ToString(db.M_pl_key),
		NIKAgent:      mapper.ToInt64(db.NIKAgent),
		GuidProcess:   mapper.ToString(db.GuidProcess),
		M_pl_key1:     mapper.ToString(db.M_pl_key1),
		Lcs_flag:      mapper.ToBool(db.Lcs_flag),
		Buy_sell_code: mapper.ToString(db.Buy_sell_code),
		Product:       mapper.ToString(db.Product),
		In_usd_before: mapper.ToDecimal(db.In_usd_before),
		In_usd_total:  mapper.ToDecimal(db.In_usd_total),
	}
}
