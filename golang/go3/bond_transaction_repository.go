//go:build samples
// +build samples

package repository

import (
	"context"
	"database/sql"
	"fmt"
	"obligasitransaksi-v2-be/baselib/exception"
	"obligasitransaksi-v2-be/bootstrap"
	"obligasitransaksi-v2-be/internal/model"
)

type BondTransactionRepository interface {
	GetSecondaryTransactionStatus(c context.Context) (*[]model.SecondaryTransactionStatus, error)
	GetSecondaryTransactionBlotter(c context.Context, trxStatus int, dateFrom string, dateTo string, trxType string, benefitType string) (*[]model.SecondaryTransactionBlotter, error)
}

type BondTransactionRepositoryImpl struct {
	db_trs *sql.DB
	cfg    *bootstrap.Container
}

func NewBondTransactionRepository(db_trs *sql.DB, cfg *bootstrap.Container) BondTransactionRepository {
	return &BondTransactionRepositoryImpl{
		db_trs: db_trs,
		cfg:    cfg}
}

func (r *BondTransactionRepositoryImpl) GetSecondaryTransactionStatus(c context.Context) (*[]model.SecondaryTransactionStatus, error) {
	var err error

	// Check if database is alive.
	err = r.db_trs.PingContext(c)
	if err != nil {
		return nil, err
	}

	query := `SELECT Id,Description FROM dbo.SecurityTransactionStatus_TM ORDER BY Id`
	rows, err := r.db_trs.Query(query)

	if err != nil {
		return nil, exception.ValidationException("", fmt.Sprintf("Gagal ambil data SecurityTransactionStatus_TM %s", err))
	}

	defer rows.Close()

	var secondaryTrxStatusList []model.SecondaryTransactionStatus
	for rows.Next() {
		var secondaryTrxStatus model.SecondaryTransactionStatus
		rows.Scan(
			&secondaryTrxStatus.Id,
			&secondaryTrxStatus.Description,
		)
		secondaryTrxStatusList = append(secondaryTrxStatusList, secondaryTrxStatus)
	}
	return &secondaryTrxStatusList, err
}

func (r *BondTransactionRepositoryImpl) GetSecondaryTransactionBlotter(c context.Context, trxStatus int, dateFrom string, dateTo string, trxType string, benefitType string) (*[]model.SecondaryTransactionBlotter, error) {
	var err error

	// Check if database is alive.
	err = r.db_trs.PingContext(c)
	if err != nil {
		return nil, err
	}

	query := `
	DECLARE
		@pdFrom DATETIME  
		,@pdTo 	DATETIME  

	SET @pdFrom = DATEADD(dd,DATEDIFF(dd,0,@pdTrxFrom),0)    
  	SET @pdTo = DATEADD(DAY,1,@pdTrxTo)            

	  CREATE TABLE #tmp_blotter (  
		[DealId]				BIGINT  
		,[SecurityAcctNo]		VARCHAR(20)  
		,[TransactionType]		VARCHAR(50)  
		,[ProductName]			VARCHAR(100)  
		,[TipeBenefit]			VARCHAR(50)  
		,[Name]					VARCHAR(100)  
		,[BranchTransaksi]		VARCHAR(150)  
		,[Currency]				VARCHAR(10)  
		,[FaceValue]			VARCHAR(30)  
		,[nFaceValue]			DECIMAL(20,5)  
		,[DealPrice]			DECIMAL(20,5)  
		,[AccruedInterest]		VARCHAR(30)  
		,[TotalProceed]			VARCHAR(30)  
		,SumberDana				VARCHAR(500)  
		,[TanggalTransaksi]		DATETIME  
		,[SettlementDate]		DATETIME  
		,[DealNoMurex]			VARCHAR(100)  
		,[Status]				VARCHAR(100)  
		,[Instruksi]			VARCHAR(100)    
		,[Switching]			VARCHAR(100)    
		,IdLelang				VARCHAR(50)  
		,MurexNoKemenkeu		VARCHAR(20)  
		,MurexNoTrader			VARCHAR(20)  
		,[ReplaceIdLelang]		VARCHAR(1000)  
		,HargaModal				VARCHAR(40)  
		,nHargaModal			DECIMAL(20,5)  
		,NIKInputter			VARCHAR(150)  
		,NIKNamaInputter		VARCHAR(150)  
		,NIKSeller				VARCHAR(150)  
		,NIKNamaSeller			VARCHAR(150)  
		,NIKMarketing			VARCHAR(150)  
		,NIKNamaMarketing		VARCHAR(150)  
        ,PBEACustomerId			VARCHAR(13)       
        ,FlagOther				BIT
        ,NoRekInestor			VARCHAR(40)
		,RevenueInIDR			DECIMAL(20,5)
		,TrxType				INT
		,SecId					BIGINT
        ,Kurs					DECIMAL(13,7)     
        ,BondType				VARCHAR(10)       
		,TrxStatus				VARCHAR(10)
	)  

	CREATE TABLE #tmp_SOF (  
		DealIdProduct			BIGINT
		,Product				VARCHAR(50)
		,SourceData				VARCHAR(50)  
		,DealIdSource			BIGINT
		,Account				VARCHAR(50)  
	)
   
	/*** TRX SEKUNDER ***/  
	INSERT #tmp_blotter (  
		[DealId]     
		,[SecurityAcctNo]   
		,[TransactionType]   
		,[ProductName]  
		,[TipeBenefit]
		,[Name]      
		,[BranchTransaksi]   
		,[Currency]
		,[FaceValue]    
		,[nFaceValue]
		,[DealPrice]    
		,[AccruedInterest]   
		,[TotalProceed]  
		,[TanggalTransaksi]  
		,[SettlementDate]      
		,[DealNoMurex]   
		,[Status]     
		,[Instruksi]    
		,[Switching]  
		,HargaModal  
		,nHargaModal  
		,NIKInputter
		,NIKSeller
		,PBEACustomerId
		,FlagOther
		,NoRekInestor
		,TrxType
		,SecId
		,TrxStatus
	)  
	SELECT   
		sttt.DealId   
		,tctm.SecAccNo  
		,sctr.TrxDesc  
		,sm.SecDescr  
		,par.Description
		,tctm.Nama  
		,sttt.TrxBranch + '-' + mie.office_name
		,sm.SecCcy
		,CONVERT(VARCHAR(30),sttt.FaceValue,1)  
		,sttt.FaceValue
		,sttt.DealPrice  
		,CONVERT(VARCHAR(30),sttt.AccruedInterest,1)  
		,CONVERT(VARCHAR(30),sttt.TotalProceed,1)  
		,sttt.TrxDate  
		,sttt.SettlementDate  
		,ISNULL(sttt.DealIdExternal,'-')  
		,ISNULL(sts.[Description],'-')  
		,CASE WHEN sttt.Instruction = 'OM'   
		THEN 'OneMobile'    
		WHEN sttt.Instruction = 'VELO' THEN 'VELOCITY' 
		ELSE CASE WHEN sttt.PhoneOrderBit = 1   
			THEN 'Phone Order'  
			ELSE 'Cabang'  
			END  
		END  
		,CASE sttt.FlagSwitching   
			WHEN 1 THEN sttt.DealIdSwitching  
			ELSE '' 
		END  AS Switching  
		,CONVERT(VARCHAR(30),sttt.HargaModal, 1) AS HargaModal    
		,sttt.HargaModal
		,sttt.NIK_Dealer
		,sttt.NIK_CS
        ,RIGHT(tctm.CIFNo, 13) AS PBEACustomerId       
		,sttt.FlagOther
		,sttt.NoRekInvestor
		,sttt.TrxType
		,sttt.SecId
		,sttt.TrxStatus
	FROM dbo.SecurityTransaction_TT sttt   WITH(NOLOCK)
	INNER JOIN dbo.TreasuryCustomer_TM tctm WITH(NOLOCK)   
		ON sttt.CIFId = tctm.CIFId      
	LEFT JOIN dbo.SecurityTransactionStatus_TM sts WITH(NOLOCK) 
		ON sttt.TrxStatus = sts.Id      
	LEFT JOIN dbo.MISOfficeInformationEODFull_v mie WITH(NOLOCK) 
		ON sttt.TrxBranch = mie.office_id      
	LEFT JOIN dbo.SecurityMaster_TM sm WITH(NOLOCK)    
		ON sttt.SecId = sm.SecId    
	LEFT JOIN dbo.SecurityTransaction_TR sctr WITH(NOLOCK)  
		ON sttt.TrxType = sctr.TrxTypeID
	LEFT JOIN dbo.TRSParameter_TR par
		ON sm.ImbalHasil = par.Value
			AND par.Code = 'ImbalHasil'
	WHERE (@pnStatus = -1 or sttt.TrxStatus = @pnStatus)  
		AND (CASE WHEN sttt.TrxType = 5 THEN sttt.SettlementDate ELSE sttt.TrxDate END) >= @pdFrom  
		AND (CASE WHEN sttt.TrxType = 5 THEN sttt.SettlementDate ELSE sttt.TrxDate END) < @pdTo
		AND TrxType not in (11)    
		AND ISNULL([IsBuyBack], 0) = 0  
		AND ISNULL([IsLelang], 0) = 0  
		AND @pcTrxType <> 'Lelang'

	UPDATE tmp  
	SET tmp.[Instruksi] = 'RM Mobile'  
	FROM #tmp_blotter tmp  
	JOIN dbo.RMMSecurityTransaction_TT rmm  
		ON tmp.[DealId] = rmm.DealId  
	WHERE tmp.[Instruksi] = 'Cabang'  
  
	/*** TRX LElANG ***/  
	INSERT #tmp_blotter (  
		IdLelang  
		,[SecurityAcctNo]   
		,[TransactionType]   
		,[ProductName]    
		,[TipeBenefit]
		,[Name]      
		,[BranchTransaksi]   
		,[Currency]
		,[FaceValue]    
		,[nFaceValue]    
		,[DealPrice]    
		,[AccruedInterest]   
		,[TotalProceed]  
		,[TanggalTransaksi]  
		,[SettlementDate]      
		,[DealNoMurex]   
		,[Status]     
		,[Instruksi]      
		,HargaModal  
		,nHargaModal  
		,NIKInputter
		,NIKSeller
		,PBEACustomerId
		,FlagOther
		,NoRekInestor
		,TrxType
		,SecId
		,TrxStatus
	)  
	SELECT   
		CONVERT(VARCHAR(50), stl.IdLelang)
		,tctm.SecAccNo  
		,'Sell'  
		,sm.SecDescr  
		,par.Description
		,tctm.Nama  
		,stl.TrxBranch + '-' + mie.office_name  
		,sm.SecCcy
		,CONVERT(VARCHAR(30),ISNULL(sttt.FaceValue,stl.FaceValue),1)  
		,ISNULL(sttt.FaceValue,stl.FaceValue)  
		,ISNULL(sttt.DealPrice,stl.DealPrice)  
		,CONVERT(VARCHAR(30),ISNULL(sttt.AccruedInterest,stl.AccruedInterest),1)  
		,CONVERT(VARCHAR(30),ISNULL(sttt.TotalProceed,stl.TotalProceed),1)  
		,ISNULL(sttt.TrxDate,stl.TrxDate)  
		,stl.SettlementDate  
		,ISNULL(sttt.DealIdExternal,'-')  
		,ISNULL(sts.ParamDesc,'-')  
		,'Cabang'  
		,CONVERT(VARCHAR(30),stl.HargaModal, 1) AS HargaModal  
		,stl.HargaModal
		,stl.NIK_Dealer
		,stl.NIK_CS
        ,RIGHT(tctm.CIFNo, 13) AS PBEACustomerId       
		,stl.FlagOther
		,stl.NoRekInvestor
		,4
		,stl.SecId
		,stl.[Status]
	FROM dbo.SecurityTrxLelang_TT stl  WITH(NOLOCK)    
	INNER JOIN dbo.TreasuryCustomer_TM tctm WITH(NOLOCK) 
		ON stl.CIFId = tctm.CIFId      
	LEFT JOIN dbo.SecurityParamLelang_TR sts WITH(NOLOCK) 
		ON stl.[Status] = sts.ParamValue      
	LEFT JOIN dbo.MISOfficeInformationEODFull_v mie WITH(NOLOCK) 
		ON stl.TrxBranch = mie.office_id      
	LEFT JOIN dbo.SecurityMaster_TM sm WITH(NOLOCK) 
		ON stl.SecId = sm.SecId  
	LEFT JOIN dbo.SecurityTransaction_TT sttt WITH(NOLOCK) 
		ON stl.IdLelang = sttt.IdLelang  
			AND sttt.TrxDate >= @pdFrom 
			AND sttt.TrxDate < @pdTo  
	LEFT JOIN dbo.TRSParameter_TR par
		ON sm.ImbalHasil = par.Value
			AND par.Code = 'ImbalHasil'
	WHERE ((sttt.TrxDate IS NULL AND stl.TrxDate >= @pdFrom) OR sttt.TrxDate >= @pdFrom)  
		AND ((sttt.TrxDate IS NULL AND stl.TrxDate < @pdTo) OR sttt.TrxDate < @pdTo)  
		AND @pcTrxType <> 'Sekunder'
    
	IF @pcTrxType = 'Lelang' AND @pnStatus >= 0
	BEGIN  
		DELETE bl  
		FROM #tmp_blotter bl WITH(NOLOCK)  
		LEFT JOIN dbo.SecurityTransaction_TT stt WITH(NOLOCK)  
			ON bl.IdLelang = stt.IdLelang  
				AND stt.TrxDate >= @pdFrom  
				AND stt.TrxDate < @pdTo  
				AND stt.TrxStatus = @pnStatus  
		WHERE bl.[TransactionType] = 'Sell'  
			AND stt.IdLelang IS NULL  
	END  
   
	UPDATE bl  
	SET [DealId] = tt.DealId  
		,[Status] = sts.[Description]  
	FROM #tmp_blotter bl  
	JOIN dbo.SecurityTransaction_TT tt   
		ON bl.IdLelang = tt.IdLelang  
	JOIN dbo.SecurityTransactionStatus_TM sts  
		ON tt.TrxStatus = sts.Id      
	WHERE bl.[TransactionType] = 'Sell'  
		AND tt.IsLelang = 1  
		AND tt.TrxDate >= DATEADD(day,-7,@pdFrom)  
		AND tt.TrxDate < DATEADD(day,7,@pdTo)  
      
	UPDATE bl  
	SET MurexNoKemenkeu = tr.MurexNoKemenkeu  
		,MurexNoTrader = tr.MurexNoTrader  
	FROM #tmp_blotter bl  
	JOIN dbo.[SecurityTrxResultLelang_TT] tt  
		ON bl.IdLelang = tt.IdLelang  
	JOIN dbo.SecurityTrxLelang_TT trx  
		ON trx.IdLelang = tt.IdLelang  
	JOIN dbo.SecurityBatchResult_TR tr  
		ON tt.ResultId = tr.ResultId  
			AND tt.Instruction = tr.BatchType  
	WHERE trx.[Status] = 'EXC'  
    
	UPDATE bl  
	SET MurexNoKemenkeu = tr.MurexNoKemenkeu  
		,MurexNoTrader = tr.MurexNoTrader  
	FROM #tmp_blotter bl  
	JOIN SecurityReplaceTrxLelang_TT rep  
		ON bl.IdLelang = rep.IdLelang  
	JOIN dbo.[SecurityTrxResultLelang_TT] tt  
		ON rep.IdLelang_Source = tt.IdLelang  
	JOIN dbo.SecurityBatchResult_TR tr  
		ON tt.ResultId = tr.ResultId  
			AND tt.Instruction = tr.BatchType  
	WHERE tt.StatusData = 'EXC'  
    
	IF EXISTS(SELECT TOP 1 1 FROM #tmp_blotter a  
		JOIN dbo.SecurityReplaceTrxLelang_TT b WITH(NOLOCK)  
			ON b.IdLelang_Source = a.IdLelang  
		WHERE b.IdLelang IS NOT NULL AND @pcTrxType <> 'Sekunder'  
	)  
	BEGIN  
		UPDATE a  
		SET [ReplaceIdLelang] = STUFF(          
			(  
				SELECT DISTINCT ',' + CAST(IdLelang AS VARCHAR(19))  
				FROM dbo.SecurityReplaceTrxLelang_TT  WITH(NOLOCK)        
				WHERE IdLelang_Source = a.IdLelang  
			FOR XML PATH ('')  
			), 1, 1, ''  
			)  
		FROM #tmp_blotter a  
		WHERE a.IdLelang IS NOT NULL  
     
		UPDATE a  
		SET [ReplaceIdLelang] = [ReplaceIdLelang] + ' - ' +CONVERT(VARCHAR(15),c.InsertedDate,106)  
		FROM #tmp_blotter a  
		JOIN dbo.SecurityReplaceTrxLelang_TT b WITH(NOLOCK)  
			ON b.IdLelang_Source = a.IdLelang  
		JOIN dbo.SecurityTrxLelang_TT c WITH(NOLOCK)  
			ON b.IdLelang = c.IdLelang  
		WHERE a.IdLelang IS NOT NULL  
	END 

    --- NamaInputter ---       
    UPDATE tmp       
        SET NIKNamaInputter = RTRIM(tmp.NIKInputter) + '-' + RTRIM(u.fullname)
    FROM #tmp_blotter tmp       
    INNER JOIN dbo.user_nisp_v u       
        ON tmp.NIKInputter = u.nik 

	--- NamaSeller ---       
	UPDATE tmp       
		SET NIKNamaSeller = RTRIM(tmp.NIKSeller) + '-' + RTRIM(u.fullname)
	FROM #tmp_blotter tmp       
	INNER JOIN dbo.user_nisp_v u       
		ON tmp.NIKSeller = u.nik   

    --- EA Customer NIKMarketingFlagging ---       
    UPDATE tmp       
        SET NIKMarketing = ea.MOId       
    FROM #tmp_blotter tmp       
    INNER JOIN dbo.EACustomerFlagging_TM_v ea       
        ON tmp.PBEACustomerId = ea.CustomerId       
          
    --- NIKMarketingFlagging & IsPremierBanking ---          
    UPDATE tmp       
        SET NIKMarketing = pb.UserId       
    FROM #tmp_blotter tmp       
    INNER JOIN dbo.PBCustomerFlagging_TM_v pb       
        ON tmp.PBEACustomerId = pb.CustomerId 

    --- NameMarketingFlagging ---        
	UPDATE tmp       
        SET NIKNamaMarketing = RTRIM(tmp.NIKMarketing) + '-' + RTRIM(u.fullname)
    FROM #tmp_blotter tmp       
    INNER JOIN dbo.user_nisp_v u       
        ON tmp.NIKMarketing = u.nik 
	
	--- Sumber Dana ---        
	INSERT #tmp_SOF(  
		DealIdProduct
		,Product
		,SourceData
		,DealIdSource
		,Account
	)
	SELECT 
		DealIdProduct
		,Product
		,SourceData
		,DealIdSource
		,Account
	FROM #tmp_blotter tmp
	JOIN dbo.TRSListTransactionSourceOfFund_TT tt
		ON RTRIM(tt.Product) IN ('Obligasi', 'Lelang')
			AND tmp.DealId = tt.DealIdProduct
	WHERE TrxStatus NOT IN ('11')

	INSERT #tmp_SOF(  
		DealIdProduct
		,Product
		,SourceData
		,DealIdSource
		,Account
	)
	SELECT 
		DealIdProduct
		,Product
		,SourceData
		,DealIdSource
		,Account
	FROM #tmp_blotter tmp
	JOIN dbo.TRSListTransactionSourceOfFund_TH tt
		ON RTRIM(tt.Product) IN ('Obligasi', 'Lelang')
			AND tmp.DealId = tt.DealIdProduct
	WHERE TrxStatus IN ('11')
		AND [Description] = 'DELETE DATA'

	INSERT #tmp_SOF(  
		DealIdProduct
		,Product
		,SourceData
		,DealIdSource
		,Account
	)
	SELECT 
		DealIdParent
		,'Obligasi'
		,'Obligasi'
		,DealIdChild
		,0
	FROM #tmp_blotter tmp
	JOIN dbo.SecurityTransactionLink_TR tr
		ON tmp.DealId = tr.DealIdParent
	WHERE TrxStatus NOT IN ('11')

	INSERT #tmp_SOF(  
		DealIdProduct
		,Product
		,SourceData
		,DealIdSource
		,Account
	)
	SELECT 
		DealIdParent
		,'Obligasi'
		,'Obligasi'
		,DealIdChild
		,0
	FROM #tmp_blotter tmp
	JOIN dbo.SecurityTranscationLink_TL tr
		ON tmp.DealId = tr.DealIdParent
	WHERE TrxStatus IN ('11')

	SELECT DISTINCT sof2.DealIdProduct, 
		SUBSTRING(
			(
				SELECT 
					CASE sof1.SourceData 
					WHEN 'Rekening' THEN ', ' + 'Rekening-[' + CONVERT(VARCHAR(100), sof1.Account) + ']'
					ELSE ',' + sof1.SourceData + '-[' + CONVERT(VARCHAR(100), sof1.DealIdSource) + ']' END AS [text()]
				FROM #tmp_SOF sof1
				WHERE sof1.DealIdProduct = sof2.DealIdProduct
				ORDER BY sof1.DealIdProduct
				FOR XML PATH (''), TYPE
			).value('text()[1]	','NVARCHAR(max)'), 2, 1000) AS SumberDana
	INTO #tmpSumberDana
	FROM #tmp_SOF sof2

	UPDATE blt
		SET SumberDana = sd.SumberDana
	FROM #tmp_blotter blt
	JOIN #tmpSumberDana sd
		ON blt.DealId = sd.DealIdProduct

	UPDATE blt
		SET SumberDana = 'Rekening-[' + CONVERT(VARCHAR(100), NoRekInestor) + ']' 
	FROM #tmp_blotter blt
	WHERE ISNULL(SumberDana, '') = ''
	AND TransactionType != 'Buy'
	
	DELETE tmp FROM #tmp_blotter tmp
	WHERE @pcBenefitType != 'ALL' AND (ISNULL(TipeBenefit, '') = '' OR TipeBenefit != @pcBenefitType)

	DELETE tmp FROM #tmp_blotter tmp WHERE RTRIM(TransactionType) IN ('Bond Due', 'Coupon')

	UPDATE tmp     
	SET Kurs = sct.BIRate     
	FROM #tmp_blotter tmp     
	INNER JOIN dbo.SIBSCurrencyTable_TM_v sct     
		ON tmp.Currency = sct.CurrencyCode     
	WHERE sct.PBOId = '500'    
		AND CONVERT(VARCHAR, tmp.TanggalTransaksi, 112) >= CONVERT(VARCHAR, GETDATE(), 112)    
		AND CONVERT(VARCHAR, tmp.TanggalTransaksi, 112) < DATEADD(DAY, 1, DATEADD(DAY, DATEDIFF(DAY,0,GETDATE()), 0))    
     
	UPDATE tmp    
	SET tmp.Kurs =  ISNULL(sc.BIRate, 1)    
	FROM #tmp_blotter AS tmp    
	JOIN SQL_SIBS.dbo.SIBSCurrencyTable_EOD AS sc    
		ON CONVERT(VARCHAR, tmp.TanggalTransaksi, 112) = sc.Period    
			AND sc.CurrencyCode = tmp.Currency    
	WHERE tmp.Currency <> 'IDR' and sc.PBOId = 500    
		AND tmp.TanggalTransaksi < CONVERT(VARCHAR, GETDATE(), 112)    
		AND tmp.Kurs IS NULL    
  
	UPDATE #tmp_blotter    
		SET Kurs = 1    
	WHERE Currency = 'IDR'     
	
	UPDATE tmp
		SET RevenueInIDR = hr.ProfitLCY
    FROM #tmp_blotter tmp
	JOIN dbo.TRSHasilRevenue_TM AS hr
		ON tmp.DealId = hr.DealId

    UPDATE tmp       
		SET RevenueInIDR = 	CASE 
			WHEN Currency  = 'IDR' THEN convert( DECIMAL(20,5), CASE
				WHEN tmp.TrxType = 4 THEN ( ISNULL(tmp.DealPrice,0)  - ISNULL(tmp.nHargaModal,0)) * tmp.nFaceValue
				WHEN tmp.TrxType = 3 THEN ( ISNULL(tmp.nHargaModal,0) - ISNULL(tmp.DealPrice,0)) * tmp.nFaceValue
				ELSE 0
				END) / 100.00 
			ELSE (convert( DECIMAL(20,5), CASE 
				WHEN tmp.TrxType = 4 THEN ( ISNULL(tmp.DealPrice,0)  - ISNULL(tmp.nHargaModal,0)) * tmp.nFaceValue     
				WHEN tmp.TrxType = 3 THEN ( ISNULL(tmp.nHargaModal,0) - ISNULL(tmp.DealPrice,0)) * tmp.nFaceValue     
				ELSE 0     
				END) / 100.00 ) * Kurs     
			END   
    FROM #tmp_blotter tmp     
    WHERE RevenueInIDR IS NULL

	UPDATE tmp
		SET BondType = CASE
				WHEN sm.IsCorporateBond = 0 AND sm.SecType != 3 THEN 'GOV'
				WHEN sm.IsCorporateBond = 0 AND sm.SecType = 3 THEN 'SRBI'
				ELSE 'CORP'
			END
	FROM #tmp_blotter tmp
	JOIN dbo.SecurityMaster_TM sm WITH(NOLOCK) 
		ON tmp.SecId = sm.SecId  


	/*** RESULT ***/  
	SELECT   
		[DealId]     
		,IdLelang  
		,[SecurityAcctNo]   
		,[TransactionType]   
		,[ProductName]    
		,[TipeBenefit]
		,[Name]      
		,[BranchTransaksi]   
		,[Currency]
		,[FaceValue]    
		,HargaModal  
		,[DealPrice]    
		,[AccruedInterest]   
		,[TotalProceed]  
		,SumberDana
		,[TanggalTransaksi]  
		,[SettlementDate]      
		,[DealNoMurex]   
		,[Status]     
		,[Instruksi]      
		,[ReplaceIdLelang]
		,MurexNoKemenkeu   
		,MurexNoTrader   
		,NIKNamaInputter
		,NIKNamaSeller
		,NIKNamaMarketing
		,CONVERT(VARCHAR(30),CONVERT(MONEY,RevenueInIDR),1) AS RevenueInIDR
		,BondType
	FROM #tmp_blotter 
  
	IF OBJECT_ID('tempdb..#tmp_blotter') IS NOT NULL  
		DROP TABLE #tmp_blotter     
	
	IF OBJECT_ID('tempdb..#tmp_SOF') IS NOT NULL  
		DROP TABLE #tmp_SOF

	IF OBJECT_ID('tempdb..#tmpSumberDana') IS NOT NULL  
		DROP TABLE #tmpSumberDana
	`

	sqlArgs := []any{
		sql.Named("pnStatus", trxStatus),
		sql.Named("pdTrxFrom", dateFrom),
		sql.Named("pdTrxTo", dateTo),
		sql.Named("pcTrxType", trxType),
		sql.Named("pcBenefitType", benefitType),
	}
	rows, err := r.db_trs.Query(query, sqlArgs...)

	if err != nil {
		return nil, exception.ValidationException("", fmt.Sprintf("Gagal ambil data SecurityTransaction_TT %s", err))
	}

	defer rows.Close()

	var secondaryTrxBlotterList []model.SecondaryTransactionBlotter
	for rows.Next() {
		var secondaryTrxBlotter model.SecondaryTransactionBlotter
		rows.Scan(
			&secondaryTrxBlotter.DealId,
			&secondaryTrxBlotter.IdLelang,
			&secondaryTrxBlotter.SecurityAcctNo,
			&secondaryTrxBlotter.TransactionType,
			&secondaryTrxBlotter.ProductName,
			&secondaryTrxBlotter.TipeBenefit,
			&secondaryTrxBlotter.Name,
			&secondaryTrxBlotter.BranchTransaksi,
			&secondaryTrxBlotter.Currency,
			&secondaryTrxBlotter.FaceValue,
			&secondaryTrxBlotter.HargaModal,
			&secondaryTrxBlotter.DealPrice,
			&secondaryTrxBlotter.AccruedInterest,
			&secondaryTrxBlotter.TotalProceed,
			&secondaryTrxBlotter.SumberDana,
			&secondaryTrxBlotter.TanggalTransaksi,
			&secondaryTrxBlotter.SettlementDate,
			&secondaryTrxBlotter.DealNoMurex,
			&secondaryTrxBlotter.Status,
			&secondaryTrxBlotter.Instruksi,
			&secondaryTrxBlotter.ReplaceIdLelang,
			&secondaryTrxBlotter.MurexNoKemenkeu,
			&secondaryTrxBlotter.MurexNoTrader,
			&secondaryTrxBlotter.NIKNamaInputter,
			&secondaryTrxBlotter.NIKNamaSeller,
			&secondaryTrxBlotter.NIKNamaMarketing,
			&secondaryTrxBlotter.RevenueInIDR,
			&secondaryTrxBlotter.BondType,
		)
		secondaryTrxBlotterList = append(secondaryTrxBlotterList, secondaryTrxBlotter)
	}
	return &secondaryTrxBlotterList, err
}
