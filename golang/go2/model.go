//go:build samples
// +build samples

package mssql

import (
	"database/sql"

	"github.com/shopspring/decimal"
)

type BIRateDB struct {
	CurrencyCode sql.NullString      `db:"CurrencyCode"`
	Value        decimal.NullDecimal `db:"BIRate"`
}

type ValasMappingParameterDB struct {
	Currency                 sql.NullString      `db:"currency"`
	Buy_sell_code            sql.NullString      `db:"buy_sell_code"`
	Product                  sql.NullString      `db:"product"`
	Product_category         sql.NullString      `db:"product_category"`
	Transaction_limit_flag   sql.NullBool        `db:"transaction_limit_flag"`
	Transaction_limit_amount decimal.NullDecimal `db:"transaction_limit_amount"`
	Monthly_limit_flag       sql.NullBool        `db:"monthly_limit_flag"`
	Monthly_limit_amount     sql.NullInt64       `db:"monthly_limit_amount"`
	Lcs_flag                 sql.NullBool        `db:"lcs_flag"`
	Rate_currency            sql.NullString      `db:"rate_currency"`
	Negara_mitra             sql.NullString      `db:"negara_mitra"`
}

type VLSRateDB struct {
	Period           sql.NullTime        `db:"Period"`
	JPeriod          sql.NullInt64       `db:"JPeriod"`
	CurrencyCode     sql.NullString      `db:"CurrencyCode"`
	BiRateExchange   decimal.NullDecimal `db:"BiRateExchange"`
	USDRateExchange  decimal.NullDecimal `db:"USDRateExchange"`
	JISDORRate       decimal.NullDecimal `db:"JISDORRate"`
	USDRateExcJisdor decimal.NullDecimal `db:"USDRateExcJisdor"`
}

type VLSTransctionsDB struct {
	Src           sql.NullString      `db:"src"`
	Dealno        sql.NullString      `db:"dealno"`
	Customer_id   sql.NullString      `db:"customer_id"`
	Acc_id        sql.NullString      `db:"acc_id"`
	Trx_branch    sql.NullString      `db:"trx_branch"`
	Trx_date      sql.NullTime        `db:"trx_date"`
	Trx_datetime  sql.NullTime        `db:"trx_datetime"`
	Currency_code sql.NullString      `db:"currency_code"`
	Amount        decimal.NullDecimal `db:"amount"`
	Rate          decimal.NullDecimal `db:"rate"`
	InUSD         decimal.NullDecimal `db:"InUSD"`
	SourceKey     sql.NullString      `db:"SourceKey"`
	JISDORRate    decimal.NullDecimal `db:"JISDORRate"`
	InUSDJISDOR   decimal.NullDecimal `db:"InUSDJISDOR"`
	M_pl_key      sql.NullString      `db:"m_pl_key"`
	NIKAgent      sql.NullInt64       `db:"NIKAgent"`
	GuidProcess   sql.NullString      `db:"GuidProcess"`
	M_pl_key1     sql.NullString      `db:"m_pl_key1"`
	Lcs_flag      sql.NullBool        `db:"lcs_flag"`
	Buy_sell_code sql.NullString      `db:"buy_sell_code"`
	Product       sql.NullString      `db:"product"`
	In_usd_before decimal.NullDecimal `db:"in_usd_before"`
	In_usd_total  decimal.NullDecimal `db:"in_usd_total"`
}
