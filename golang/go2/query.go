//go:build samples
// +build samples

package mssql

const QueryGetCurrWorkingDate = `
	SELECT TOP 1 current_working_date
	FROM dbo.control_table
`

const QueryGetCFCOUN = `
	SELECT DISTINCT TRIM(CFCOUN)
	FROM dbo.CFMAST
	WHERE (CFCIF = ?)
`

const QueryGetBIRate = `
	SELECT CurrencyCode, BIRate
	FROM dbo.SIBSCurrencyTable_TM
	WHERE (PBOId = 500)
`

const QueryVlsTran = `
	SELECT src,dealno
		,customer_id,acc_id
		,trx_branch,trx_date
		,trx_datetime,currency_code
		,amount,rate
		,InUSD,SourceKey
		,JISDORRate,InUSDJISDOR
		,m_pl_key,NIKAgent
		,GuidProcess,m_pl_key1
		,lcs_flag,buy_sell_code
		,product,in_usd_before
		,in_usd_total
	FROM dbo.VLSTransactions_TR
	WHERE dealno IN (?)
`
const queryVlsTranToday = `
	SELECT src,dealno
		,customer_id,acc_id
		,trx_branch,trx_date
		,trx_datetime,currency_code
		,amount,rate
		,InUSD,SourceKey
		,JISDORRate,InUSDJISDOR
		,m_pl_key,NIKAgent
		,GuidProcess,m_pl_key1
		,lcs_flag,buy_sell_code
		,product,in_usd_before
		,in_usd_total
	FROM dbo.VLSTransactionsToday_TR
	WHERE dealno IN (?)
`
