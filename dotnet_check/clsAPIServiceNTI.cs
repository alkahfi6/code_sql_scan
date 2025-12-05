using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NISPDataSourceNetCore.database;
using NISPDataSourceNetCore.helper;
using NISPDataSourceNetCore.logger;
using NISPDataSourceNetCore.webservice;
using NISPDataSourceNetCore.webservice.model;
using Treasury.Model;
using Treasury.Customer.API.Utilities;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;

namespace Treasury.Customer.API.Services
{
    public class clsAPIServiceNTI: IServiceNTI
    {
        private readonly IConfiguration _iconfiguration;
        private readonly bool _ignoreSSL;
        private readonly int _nTimeOut = 0;
        private readonly string _strConnStringOBL = "";
        private readonly string _strUrlWsObli = "";
        private readonly string _strUrlWsProTeller = "";
        private readonly string _strUrlWsWEBI = "";
        private readonly EPV.EPVEnvironmentType _envType;
        private readonly int _sqlTellerId = 0;
        private readonly string _sqlMailDb = "";
        private readonly string _oneNotifSingleCall = "";
        private readonly string _oneNotifBulkCall = "";
        private readonly int _eventId = 0;
        private readonly string _clientKey = "";
        private readonly string _clientSecret = "";
        private readonly string _urlAPIProCIF = "";

        private readonly string _moduleName = "";
        private readonly string _apiGuid = "";
        private readonly IApiLogger _logger = null;
        //20221216, yudha.n, BONDRETAIL-1154, begin
        private readonly string _strUrlWsOmniObli = "";
        private readonly string _strEncConnString_OBL = "";
        //20221216, yudha.n, BONDRETAIL-1154, end
        public clsAPIServiceNTI(IConfiguration iConfiguration, GlobalVariableList globalVar)
        {
            this._iconfiguration = iConfiguration;
            this._ignoreSSL = globalVar.IgnoreSSL;
            this._nTimeOut = globalVar.TimeOut;
            this._strConnStringOBL = globalVar.ConnectionStringDBOBL;
            this._envType = globalVar.EnvironmentType;
            this._moduleName = globalVar.ModuleName;
            this._apiGuid = globalVar.APIGuid;
            this._logger = globalVar.Logger;
            this._strUrlWsObli = globalVar.URLWsObli;
            this._strUrlWsProTeller = globalVar.UrlWsProTeller;
            this._strUrlWsWEBI = globalVar.UrlWsWEBI;
            this._sqlTellerId = int.Parse(this._iconfiguration["ServerID_SQL_TELLER"].ToString());
            this._sqlMailDb = this._iconfiguration["DBName_SQL_MAIL"];
            this._oneNotifSingleCall = globalVar.UrlOneNotifSingleCall;
            this._oneNotifBulkCall = globalVar.UrlOneNotifBulkCall;
            this._eventId = globalVar.EventId;
            this._clientKey = globalVar.ClientKey;
            this._clientSecret = globalVar.ClientSecret;
            this._urlAPIProCIF = globalVar.urlAPIProCif;
            //20221216, yudha.n, BONDRETAIL-1154, begin
            _strUrlWsOmniObli = globalVar.URLWsOmniObli;
            _strEncConnString_OBL = globalVar.EncConnectionStringDBOBL;

            clsCallSPWs._strUrlWsOmniObli = _strUrlWsOmniObli;
            clsCallSPWs._strEncConnectionStringObl = _strEncConnString_OBL;
            //20221216, yudha.n, BONDRETAIL-1154, end
        }
        public ApiMessage<InquiryTreasuryCustomerRs> InquiryTreasuryCustomer(ApiMessage<InquiryTreasuryCustomerRq> paramIn)
        {
            ApiMessage<InquiryTreasuryCustomerRs> msgResponse = new ApiMessage<InquiryTreasuryCustomerRs>();
            msgResponse.copyHeaderForReply(paramIn);
            InquiryTreasuryCustomerRs inquiryResponse = new InquiryTreasuryCustomerRs();

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                long CIF = 0;
                if (!long.TryParse(paramIn.Data.CIF, out CIF))
                    throw new Exception("CIFNo should be numerical");

                string strCIF = CIF.ToString("D19");

                #region Query Inquiry Trs Customer
                var queryString = @"
                    DECLARE @cErrMsg			VARCHAR(MAX)
                            , @nCIFId			BIGINT
                            , @bIsExist			BIT
		                    , @bIsPendingSID	BIT

                    SET @bIsExist = CAST(0 AS BIT)
                    SET @bIsPendingSID = CAST(0 AS BIT)
                    BEGIN TRY
                        IF EXISTS(SELECT TOP 1 1
			                    FROM dbo.TreasuryCustomer_TM
			                    WHERE CIFNo = @pnCIFNo)    
                        BEGIN
		                    SET @bIsExist = CAST(1 AS BIT)

		                    SELECT @bIsPendingSID = CASE WHEN ISNULL(SID, '') = '' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
		                    FROM dbo.TreasuryCustomer_TM
		                    WHERE CIFNo = @pnCIFNo
                        END

                        SELECT @pnCIFNo AS CIF, @bIsExist AS isExist, @bIsPendingSID AS isPendingSID
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                        BEGIN
		                    ROLLBACK TRANSACTION
                        END
            
                        DECLARE @ErrorMessage nvarchar(max)
                            , @ErrorSeverity int
                            , @ErrorState int
            
                        SELECT
                        @ErrorMessage = ERROR_MESSAGE()
                        , @ErrorSeverity = ERROR_SEVERITY()
                        , @ErrorState = ERROR_STATE()
            
                        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
                    END CATCH";
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pnCIFNo", strCIF);

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, queryString, ref sqlParam, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                inquiryResponse = JsonConvert.DeserializeObject<List<InquiryTreasuryCustomerRs>>(JsonConvert.SerializeObject(dsOut.Tables[0])).FirstOrDefault();

                msgResponse.IsSuccess = true;
                msgResponse.Data = inquiryResponse;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.Data = null;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }
            return msgResponse;
        }

        public ApiMessage<CreateTreasuryCustomerRs> CreateTreasuryCustomer(ApiMessage<CreateTreasuryCustomerRq> paramIn)
        {
            ApiMessage<CreateTreasuryCustomerRs> msgResponse = new ApiMessage<CreateTreasuryCustomerRs>();
            msgResponse.copyHeaderForReply(paramIn);

            try
            {
                string xml = clsUtils.SerializeObject(paramIn.Data.CustomerData);
                string xmlDoc = paramIn.Data.CustomerDocs == null ? "" : clsUtils.SerializeObject(paramIn.Data.CustomerDocs);
                string strErrMsg = "";
                var query = "";
                DataSet dsOut = new DataSet();

                #region Query insert Trs Cust (based on sp: trs_InsertTreasuryCustomer_TM)
                query = @"
                    DECLARE @error						INT                          
                            , @message					VARCHAR(MAX)                          
                            , @inErrNo					INT          
                            , @nErrNo					INT          
                            , @CheckXml					INT          
                            , @cSecAccNo				VARCHAR(20)          
                            , @cCIFNo					VARCHAR(19)          
                            , @nRunNo					INT          
                            , @nOK						INT                          
                            , @nCIFId					BIGINT                          
                            , @dcurrent_working_date	DATETIME                          
                            , @nCounter1				INT                  
                            , @nCounter2				INT                          
                            , @cAuthorizationBranch		VARCHAR(5)                          
                            , @cDuplicateNoRek			VARCHAR(20)                          
                            , @cSecAccUsingRek			VARCHAR(20)                          
                            , @cInsertSecAcc			VARCHAR(20)        
                            , @nNextCIFId				BIGINT    
                            , @nValidYear				INT    
                            , @cNonIDRAccount			VARCHAR(20)    
                            , @cFuncGroupNonIDR			VARCHAR(40)    
                            , @cSubFuncGroupNonIDR		VARCHAR(40)    
                            , @cIDNeedReview			CHAR(1)    
		                    , @cNIKSystem				VARCHAR(10)    
                            , @isDebug					BIT 
		                    , @nPos						INT          
		                    , @XMLDocs					XML          
		                    , @nStatus					INT  
		                    , @nCIFNo					BIGINT
		                    , @dLastUpdateDate			DATETIME
		                    , @dExpiredDate				DATETIME  
		                    , @cRegViaOneMobile			CHAR(1) 
		
                    SET @isDebug = 0  
                    SET @cIDNeedReview = 'N'    

                    SET @error = @@ERROR  
	                        
                    IF @isDebug = 1  
                    BEGIN   
	                    INSERT INTO dbo.xDbg_trs_InsertTreasuryCustomer_TM   
		                    (
		                    xmlInput
		                    , pxXMLDocs
		                    , cRegViaOneMobile
		                    , pcSecAccNo
		                    , pbIsOnlineAcc
		                    , inputDate
		                    )  
	                    SELECT  @xmlInput
		                    , @pxXMLDocs
		                    , @cRegViaOneMobile
		                    , @pcSecAccNo
		                    , @pbIsOnlineAcc
		                    , GETDATE()   
                    END  

                    SELECT @dcurrent_working_date = dbo.fnGetDateTime(current_working_date) 
                    FROM dbo.control_table

                    SET @cRegViaOneMobile = CASE WHEN @pcChannelReg = 'OM' THEN 'Y' ELSE 'N' END

                    IF NOT EXISTS(SELECT TOP 1 1 
			                    FROM dbo.TRSListChannelBonds_TR
			                    WHERE ChannelCode = @pcChannelReg)
                    BEGIN
	                    SET @message='Channel Registrasi tidak diketahui'                          
	                    GOTO ERR_HANDLER    
                    END

                    IF OBJECT_ID('tempdb..#tempTreasuryCustomer_TM') IS NOT NULL 
	                    DROP TABLE #tempTreasuryCustomer_TM

                    IF OBJECT_ID('tempdb..#tempCustAccNumber') IS NOT NULL 
	                    DROP TABLE #tempCustAccNumber

                    IF OBJECT_ID('tempdb..#tempRekTaxAmnesty') IS NOT NULL 
	                    DROP TABLE #tempRekTaxAmnesty

                    IF OBJECT_ID('tempdb..#tmpDocsOAO') IS NOT NULL 
	                    DROP TABLE #tmpDocsOAO

                    IF OBJECT_ID('tempdb..#tmpREPOFunded') IS NOT NULL 
	                    DROP TABLE #tmpREPOFunded
	            
                    CREATE TABLE #tempTreasuryCustomer_TM(                          
                        SecAccNo                        VARCHAR(20)         
                        ,CIFNo                          VARCHAR(19)                           
                        ,Nama                           VARCHAR(95)       
                        ,BranchCode                     VARCHAR(5)                            
                        ,JenisIdentitas                 CHAR(1)                  
                        ,NoIdentitas                    VARCHAR(40)                            
                        ,TempatLahir                    VARCHAR(50)                            
                        ,TanggalLahir                   VARCHAR(40) 
	                    ,strTanggalLahir                DATETIME                                 
                        ,JenisKelamin                   CHAR(1)                                 
                        ,JenisPekerjaan                 CHAR(2)                               
                        ,AlamatIdentitas                VARCHAR(120)                            
                        ,KodeKota                       VARCHAR(4)                            
                        ,KodePropinsi                   VARCHAR(4)                            
                        ,NoTelp                         VARCHAR(40)                            
                        ,NoHP                           VARCHAR(40)                            
                        ,NoFax                          VARCHAR(40)                            
                        ,Email                          VARCHAR(50)                            
                        ,NoRekInvestor                  VARCHAR(20)                            
                        ,AlamatSurat                    VARCHAR(120)                            
                        ,NIK_CS                         INT                             
                        ,NPWP                           VARCHAR(40)                            
                        ,[Status]                       INT                               
                        ,InsertedBy                     VARCHAR(20)                          
                        ,InsertedDate                   VARCHAR(40)   
	                    ,strInsertedDate                DATETIME                          
                        ,LastUpdateBy                   VARCHAR(20)                          
                        ,LastUpdateDate                 VARCHAR(40)
	                    ,strLastUpdateDate              DATETIME                          
                        ,MetodeKontak                   INT                          
                        ,RiskProfile                    VARCHAR(40)                          
                        ,LastRiskProfileUpdateDate      VARCHAR(40)  
	                    ,strLastRiskProfileUpdateDate   DATETIME                          
                        ,BankCustodyCode1               VARCHAR(2)                          
                        ,BankCustodySecurityNo1         VARCHAR(30)                          
                        ,BankCustodyCode2               VARCHAR(2)                          
                        ,BankCustodySecurityNo2         VARCHAR(30)         
                        ,BankCustodyCode3               VARCHAR(2)                          
                        ,BankCustodySecurityNo3         VARCHAR(30)                                  
                        ,SyaratKetentuanBit             BIT                          
                        ,TanggalPengisian               VARCHAR(40)
	                    ,strTanggalPengisian            DATETIME                          
                        ,AuthorizeBranch                VARCHAR(5)                          
                        ,Citizenship                    VARCHAR(3)                      
                        ,RiskProfileCode                CHAR(1)                  
                        ,FlagKaryawan                   BIT                      
                        ,IsSuratUseRumah                BIT                      
                        ,AlamatSuratSequence            INT                      
                        ,KodeAlamatSurat                CHAR(1) 
                        ,KodeCabangAlamatSurat          VARCHAR(5)                      
                        ,FuncGroupIDR                   VARCHAR(50)                      
                        ,SubFuncGroupIDR				VARCHAR(50)                      
                        ,FuncGroupNonIDR                VARCHAR(50)                      
                        ,SubFuncGroupNonIDR             VARCHAR(50)                      
                        ,FlagPremier                    BIT                      
                        ,NikReferentor                  INT                      
                        ,RiskProfileExpiredDate         VARCHAR(40) 
	                    ,strRiskProfileExpiredDate      DATETIME                
                        ,IdentitasExpiredDate           VARCHAR(40)  
	                    ,strIdentitasExpiredDate        DATETIME                    
                        ,FuncGroupIDRTA                 VARCHAR(50)                      
                        ,SubFuncGroupIDRTA              VARCHAR(50)                  
                        ,FuncGroupNonIDRTA              VARCHAR(50)                      
                        ,SubFuncGroupNonIDRTA           VARCHAR(50)                         
                        ,[Signature]                    IMAGE      
	                    --,[Signature]                  VARBINARY(MAX)                  
                        ,isOnlineAcc                    BIT                  
	                    ,Summary                        IMAGE  
	                    --,Summary                      VARBINARY(MAX)                  
                        ,Spouse                         VARCHAR(20)          
                        ,ApprovalNNH                    BIT    
                        ,TanggalApprovalNNH             VARCHAR(40) 
	                    ,strTanggalApprovalNNH          DATETIME    
	                    ,[KITASNo]						VARCHAR(40)
	                    ,[KITASExpDate]					VARCHAR(10)
	                    ,[KITASLastUpdateDate]			VARCHAR(10)
	                    ,ChannelReg						VARCHAR(15)
                    )
	                          
                    CREATE TABLE #tempCustAccNumber                          
                    (                          
	                    ACCTNO                          VARCHAR(20   )      NOT NULL  ,                          
	                    STATUS_JOIN                     VARCHAR(1    )      NOT NULL  ,                          
	                    CIF_PRIMARY_OWNER_JOIN          VARCHAR(19   )      NULL      ,                          
	                    NAMA_PRIMARY_OWNER_JOIN         VARCHAR(95   )      NULL      ,                          
	                    CCY                             VARCHAR(4    )      NOT NULL  ,                          
	                    RELASI                          VARCHAR(2    )      NULL      ,                          
	                    PRODUCT_CODE                    VARCHAR(10   )      NOT NULL                            
                    )
                           
                    CREATE TABLE #tempRekTaxAmnesty                        
                    (                          
	                    ACCTNO                          VARCHAR(20   )      NULL  ,                          
	                    AccountName                     VARCHAR(95   )      NULL  ,                          
	                    CurrencyCode     VARCHAR(5    )      NULL                                
                    )
                
                    CREATE TABLE #tmpDocsOAO                
                    (                
	                    MenuName VARCHAR(100)                
	                    ,ParamId VARCHAR(500)                
	                    ,NamaFile VARCHAR(350)                
	                    ,JenisFile VARCHAR(350)                
	                    ,ResultId VARCHAR(250)                
                    )

                    CREATE TABLE #tmpREPOFunded
                    (                
	                    CIFId				BIGINT,
	                    SecAccNo			VARCHAR(10),
	                    CIFNo				VARCHAR(19),
	                    GMRA				BIT,
	                    AgreementNo			VARCHAR(100),
	                    TglMulaiAgreement	DATETIME,
	
	                    LastUpdateNIK		VARCHAR(7),
	                    LastUpdateBranch	VARCHAR(7),
	                    LastUpdateDate		DATETIME DEFAULT GETDATE()
                    )
                 
                    EXEC @nOK = sp_xml_preparedocument @CheckXml OUTPUT, @xmlInput                          
                    IF @nOK!=0 OR @@error!=0                          
                    BEGIN                          
	                    SET @message='Gagal di sp_xml_preparedocument'                          
	                    GOTO ERR_HANDLER                          
                    END
	                          
                    INSERT INTO #tempTreasuryCustomer_TM                          
	                    (          
	                    SecAccNo          
	                    ,CIFNo          
	                    ,Nama          
	                    ,BranchCode          
	                    ,JenisIdentitas          
	                    ,NoIdentitas          
	                    ,TempatLahir
	                    ,TanggalLahir          
	                    ,strTanggalLahir          
	                    ,JenisKelamin          
	                    ,JenisPekerjaan          
	                    ,AlamatIdentitas          
	                    ,KodeKota          
	                    ,KodePropinsi          
	                    ,NoTelp          
	                    ,NoHP          
	                    ,NoFax          
	                    ,Email          
	                    ,NoRekInvestor          
	                    ,AlamatSurat          
	                    ,NIK_CS          
	                    ,NPWP          
	                    ,[Status]          
	                    ,InsertedBy
	                    ,InsertedDate          
	                    ,strInsertedDate          
	                    ,LastUpdateBy 
	                    ,LastUpdateDate         
	                    ,strLastUpdateDate                            
	                    ,MetodeKontak                                             
	                    ,RiskProfile   
	                    ,LastRiskProfileUpdateDate                                           
	                    ,strLastRiskProfileUpdateDate                                
	                    ,BankCustodyCode1                                         
	                    ,BankCustodySecurityNo1                                   
	                    ,BankCustodyCode2                                         
	                    ,BankCustodySecurityNo2                                   
	                    ,BankCustodyCode3                                         
	                    ,BankCustodySecurityNo3                                                
	                    ,SyaratKetentuanBit  
	                    ,TanggalPengisian                        
	                    ,strTanggalPengisian                                      
	                    ,Citizenship                      
	                    ,RiskProfileCode                     
	                    ,FlagKaryawan                           
	                    ,IsSuratUseRumah                          
	                    ,AlamatSuratSequence                         
	                    ,KodeAlamatSurat                          
	                    ,KodeCabangAlamatSurat                         
	                    ,FuncGroupIDR                           
	                    ,SubFuncGroupIDR                          
	                    ,FuncGroupNonIDR                          
	                    ,SubFuncGroupNonIDR                          
	                    ,FlagPremier                           
	                    ,NikReferentor 
	                    ,RiskProfileExpiredDate                       
	                    ,strRiskProfileExpiredDate  
	                    ,IdentitasExpiredDate   
	                    ,strIdentitasExpiredDate                    
	                    ,FuncGroupIDRTA                           
	                    ,SubFuncGroupIDRTA                          
	                    ,FuncGroupNonIDRTA                          
	                    ,SubFuncGroupNonIDRTA                    
	                    ,[Signature]                   
	                    ,isOnlineAcc                   
	                    ,Summary                  
	                    ,Spouse    
	                    ,ApprovalNNH                      
	                    ,strTanggalApprovalNNH    
	                    ,[KITASNo]
	                    ,[KITASExpDate]
	                    ,[KITASLastUpdateDate]
	                    ,ChannelReg
	                    )                          
                    SELECT           
	                    SecAccNo    
	                    ,CIFNo          
	                    ,Nama          
	                    ,BranchCode          
	                    ,JenisIdentitas          
	                    ,NoIdentitas          
	                    ,TempatLahir 
	                    ,TanggalLahir         
	                    ,strTanggalLahir          
	                    ,JenisKelamin          
	                    ,JenisPekerjaan          
	                    ,AlamatIdentitas          
	                    ,KodeKota  
	                    ,KodePropinsi 
	                    ,NoTelp          
	                    ,NoHP          
	                    ,NoFax          
	                    ,Email          
	                    ,NoRekInvestor          
	                    ,AlamatSurat          
	                    ,NIK_CS
	                    ,NPWP          
	                    ,[Status]
	                    ,InsertedBy
	                    ,InsertedDate
	                    ,strInsertedDate          
	                    ,LastUpdateBy 
	                    ,LastUpdateDate         
	                    ,strLastUpdateDate                       
	                    ,MetodeKontak                                     
	                    ,RiskProfile   
	                    ,LastRiskProfileUpdateDate                                           
	                    ,strLastRiskProfileUpdateDate                                
	                    ,BankCustodyCode1                                         
	                    ,BankCustodySecurityNo1                                   
	                    ,BankCustodyCode2                                         
	                    ,BankCustodySecurityNo2                               
	                    ,BankCustodyCode3                                         
	                    ,BankCustodySecurityNo3                                    
	                    ,SyaratKetentuanBit
	                    ,TanggalPengisian                          
	                    ,strTanggalPengisian                          
	                    ,Citizenship                      
	                    ,RiskProfileCode                  
	                    ,FlagKaryawan                           
	                    ,IsSuratUseRumah                          
	                    ,AlamatSuratSequence                         
	                    ,KodeAlamatSurat                          
	                    ,KodeCabangAlamatSurat                         
	                    ,FuncGroupIDR                           
	                    ,SubFuncGroupIDR                          
	                    ,FuncGroupNonIDR                          
	                    ,SubFuncGroupNonIDR                          
	                    ,FlagPremier                           
	                    ,NikReferentor 
	                    ,RiskProfileExpiredDate                       
	                    ,strRiskProfileExpiredDate 
	                    ,IdentitasExpiredDate                      
	                    ,strIdentitasExpiredDate                    
	                    ,FuncGroupIDRTA                           
	                    ,SubFuncGroupIDRTA                        
	                    ,FuncGroupNonIDRTA                          
	                    ,SubFuncGroupNonIDRTA                           
	                    --,@piSignature
                        ,null
	                    ,@pbIsOnlineAcc                
	                    --,@piSummary
	                    ,null
                        ,Spouse     
	                    ,CAST(ApprovalNNH AS BIT)    
	                    --,CASE WHEN ISNULL(TanggalApprovalNNH, '') != '' AND TanggalApprovalNNH != 0 THEN CONVERT(VARCHAR, CAST(TanggalApprovalNNH AS DATETIME), 112) ELSE TanggalApprovalNNH END
	                    ,strTanggalApprovalNNH
	                    ,[KITASNo]
	                    ,CASE WHEN [KITASExpDate] = '0' THEN NULL ELSE [KITASExpDate] END
	                    ,CASE WHEN [KITASLastUpdateDate] = '0' THEN NULL ELSE [KITASLastUpdateDate] END
	                    ,@pcChannelReg
                    FROM OPENXML(@CheckXml, '/MainCustomerData', 2)                          
                    WITH #tempTreasuryCustomer_TM
             
                    SELECT @cIDNeedReview = NeedReview    
                    FROM OPENXML(@CheckXml, '/MainCustomerData', 2)    
                    WITH (    
	                    NeedReview          CHAR(1)    
                    )    

                    INSERT INTO #tmpREPOFunded
	                    (
	                    SecAccNo
	                    ,CIFNo
	                    ,GMRA
	                    ,AgreementNo
	                    ,TglMulaiAgreement
	                    ,LastUpdateNIK
	                    ,LastUpdateBranch
	                    )
                    SELECT SecAccNo
	                    ,RIGHT(REPLICATE('0',19)+ CAST(CIFNo AS VARCHAR),19)
	                    ,GMRA
	                    ,AgreementNo
	                    ,TglMulaiAgreement	
	                    ,LastUpdateNIK
	                    ,LastUpdateBranch
                    FROM OPENXML(@CheckXml, '/MainCustomerData/RepoFunded', 2)                          
                    WITH #tmpREPOFunded
	
                    --IF EXISTS (
	                --    SELECT TOP 1 1 
	                --    FROM dbo.TRSCustomerRepoFunded_TR tr
	                --    JOIN #tmpREPOFunded rf
	                --    ON tr.CIFNo <> rf.CIFNo
		            --        AND tr.AgreementNo = rf.AgreementNo
	                --    )
                    --BEGIN
	                --    SET @message='AgreementNo sudah terpakai nasabah lain'                          
	                --    GOTO ERR_HANDLER     
                    --END
    
                    INSERT INTO #tempCustAccNumber                          
	                    (
	                    ACCTNO
	                    , STATUS_JOIN
	                    , CIF_PRIMARY_OWNER_JOIN
	                    , NAMA_PRIMARY_OWNER_JOIN
	                    , CCY
	                    , RELASI
	                    , PRODUCT_CODE
	                    )                         
                    SELECT ACCTNO
	                    , STATUS_JOIN
	                    , CIF_PRIMARY_OWNER_JOIN
	                    , NAMA_PRIMARY_OWNER_JOIN
	                    , CCY
	                    , RELASI
	                    , PRODUCT_CODE                          
                    FROM OPENXML(@CheckXml, '/MainCustomerData/CustomerAccountNumber/CustomerAccNumber', 2)                          
                    WITH #tempCustAccNumber
                           
                    INSERT INTO #tempRekTaxAmnesty                          
	                    (
	                    ACCTNO
	                    , AccountName
	                    , CurrencyCode
	                    )                          
                    SELECT ACCTNO
                    , AccountName
                    , CurrencyCode          
                    FROM OPENXML(@CheckXml, '/MainCustomerData/RekTaxAmnesty', 2)                          
                    WITH #tempRekTaxAmnesty 
                    WHERE CurrencyCode = 'IDR'                         
    
                    INSERT INTO #tempRekTaxAmnesty                      
	                    (
	                    ACCTNO
	                    , AccountName
	                    , CurrencyCode
	                    )                          
                    SELECT ACCTNO, AccountName, CurrencyCode                          
                    FROM OPENXML(@CheckXml, '/MainCustomerData/RekTaxAmnesty', 2)                          
                    WITH #tempRekTaxAmnesty 
                    WHERE CurrencyCode <> 'IDR'                          
 
                    IF(@pxXMLDocs IS NOT NULL AND ISNULL(@pxXMLDocs, '') <> '')          
                    BEGIN          
	                    SET @nPos = CHARINDEX('?>', @pxXMLDocs)                 
	                    SET @XMLDocs = CONVERT(XML, SUBSTRING(@pxXMLDocs, @nPos + 2, LEN(@pxXMLDocs) - @nPos - 1))                                    
	
	                    INSERT INTO #tmpDocsOAO                
		                    (                 
		                    MenuName
		                    , ParamId
		                    , NamaFile
		                    , JenisFile
		                    , ResultId                
		                    )                                
	                    SELECT 'MasterNasabah'                
		                    ,nref.value('(CIFNo/text())[1]', 'VARCHAR(19)') AS 'ParamId'                                    
		                    ,nref.value('(NamaFile/text())[1]', 'VARCHAR(500)') AS 'NamaFile'                       
		                    ,nref.value('(JenisFile/text())[1]', 'VARCHAR(500)') AS 'JenisFile'                       
		                    ,nref.value('(ResultId/text())[1]', 'VARCHAR(250)') AS 'ResultId'                                        
	                    FROM @XMLDocs.nodes('/CustomerDocument') AS R(nref) 
	                
	                    DELETE 
	                    FROM #tmpDocsOAO 
	                    WHERE ResultId is NULL            
                    END   
       
                    EXEC @nOK = sp_xml_removedocument @CheckXml
	                          
                    IF @nOK!=0 OR @@error!=0                          
                    BEGIN                          
	                    SET @message='Gagal di sp_xml_removedocument'                          
	                    GOTO ERR_HANDLER                          
                    END  
                        
                    SELECT @cSecAccNo=SecAccNo
	                    ,@cCIFNo=CIFNo 
                    FROM #tempTreasuryCustomer_TM                          

                    SELECT @cCIFNo = RIGHT(REPLICATE('0',19)+ CAST(@cCIFNo AS VARCHAR),19)                          

                    -- pengecekan CIFNo di tabel TreasuryCustomer                          
                    IF EXISTS(SELECT TOP 1 1 FROM dbo.TreasuryCustomer_TM WHERE CIFNo = @cCIFNo)                          
                    BEGIN                          
	                    SET @message='Nasabah sudah terdaftar'                          
	                    GOTO ERR_HANDLER                          
                    END    
                      
                    -- pengecekan Rek Valas di tabel TRSCustAccNumber_TR                          
                    IF EXISTS (SELECT TOP 1 1 
		                    FROM #tempCustAccNumber t 
		                    INNER JOIN dbo.TRSCustAccNumber_TR tca 
		                    ON t.ACCTNO = tca.NoRekInvestor 
			                    AND t.CCY = tca.CcyCode)                          
                    BEGIN                          
	                    SELECT TOP 1 @cDuplicateNoRek = tca.NoRekInvestor
		                    , @cSecAccUsingRek = tctm.SecAccNo                          
	                    FROM #tempCustAccNumber t 
	                    INNER JOIN dbo.TRSCustAccNumber_TR tca                          
	                    ON t.ACCTNO = tca.NoRekInvestor AND t.CCY = tca.CcyCode                          
	                    LEFT JOIN dbo.TreasuryCustomer_TM tctm                           
	                    ON tca.CIFId = tctm.CIFId                          
	
	                    SET @message='Rekening Relasi valas ' + ISNULL(@cDuplicateNoRek, '') + ' sudah digunakan oleh sekuritas ' + ISNULL(@cSecAccUsingRek, 'lain')                          
	                    GOTO ERR_HANDLER                          
                    END      

                    -- 20220914, Update Status Join untuk Rekening Non IDR, begin
					IF EXISTS(SELECT TOP 1 1
							FROM dbo.TRCFACCT_v AS A
							JOIN dbo.TRYUKON_DDMAST_v B WITH(NOLOCK) 
								ON B.ACCTNO = A.CFACC# 
							JOIN dbo.TRCFALTN_v C WITH(NOLOCK) 
								ON C.CFAACT = A.CFACC# 
							JOIN #tempCustAccNumber AS t
								ON t.ACCTNO = B.ACCTNO
							WHERE A.CFRELA <> 'P' 
								AND B.DDCTYP <> 'IDR')
					BEGIN
						UPDATE t
						SET	STATUS_JOIN = 'Y'
							, CIF_PRIMARY_OWNER_JOIN = RIGHT(REPLICATE('0', 19) + CAST(C.CFCIF# AS VARCHAR), 19)
							, NAMA_PRIMARY_OWNER_JOIN = C.CFAAL1
						FROM dbo.TRCFACCT_v AS A
						JOIN dbo.TRYUKON_DDMAST_v B  
							ON B.ACCTNO = A.CFACC# 
						JOIN dbo.TRCFALTN_v C 
							ON C.CFAACT = A.CFACC# 
						JOIN #tempCustAccNumber AS t
							ON t.ACCTNO = B.ACCTNO
						WHERE A.CFRELA <> 'P' 
							AND B.DDCTYP <> 'IDR'
					END
					-- 20220914, Update Status Join untuk Rekening Non IDR, end
      
                    IF (EXISTS(SELECT SecAccNo FROM TreasuryCustomer_TM WHERE SecAccNo = @cSecAccNo) OR                          
	                    EXISTS(SELECT SecAccNo FROM TreasuryCustomer_TH WHERE SecAccNo = @cSecAccNo))                          
                    BEGIN                                  
	                    SELECT @nCounter1 = MAX(CIFId)                           
	                    FROM TreasuryCustomer_TM   
	                       
	                    SELECT @nCounter2 = MAX(CIFId)                           
	                    FROM TreasuryCustomer_TH                          
	
	                    SET @nCounter1 = ISNULL(@nCounter1, 0)                          
	                    SET @nCounter2 = ISNULL(@nCounter2, 0)                          
	
	                    IF @nCounter2 > @nCounter1                          
	                    BEGIN                          
		                    SET @nCounter2 = @nCounter2 + 1                          
		                    SELECT @cSecAccNo = 'N' + RIGHT(REPLICATE('0',7) + CAST(@nCounter2 AS VARCHAR ), 7)                           
	                    END                          
	                    ELSE                          
	                    BEGIN 
		                    SET @nCounter1 = @nCounter1 + 1                          
		                    SELECT @cSecAccNo = 'N' + RIGHT(REPLICATE('0',7) + CAST(@nCounter1 AS VARCHAR ), 7)                          
	                    END                          
                    END           
	               
                    UPDATE ttc                          
                    SET ttc.AuthorizeBranch = un.office_id_sibs                          
                    FROM #tempTreasuryCustomer_TM ttc                          
                    JOIN dbo.user_nisp_v un                          
                        ON un.nik = ttc.InsertedBy 
		                         
                    SET @cInsertSecAcc = @cSecAccNo                           
	  
                    /*** UPDATE functional group / sub functional group yg berasal dari registrasi one mobile ***/    
                    SET @nValidYear = LEFT( CONVERT(VARCHAR(8), GETDATE(), 112), 4 )    

                    UPDATE #tempTreasuryCustomer_TM    
                    SET FuncGroupIDR            = ISNULL(FuncGroupIDR, '')   
	                    , SubFuncGroupIDR       = ISNULL(SubFuncGroupIDR, '') 
                        , FuncGroupNonIDR       = ISNULL(FuncGroupNonIDR, '')
                        , SubFuncGroupNonIDR    = ISNULL(SubFuncGroupNonIDR, '')

                    UPDATE tmp    
                    SET tmp.FuncGroupIDR        = ISNULL(acs.FunctionalGroup, '')   
	                    , tmp.SubFuncGroupIDR    = ISNULL(acs.SubFunctionalGroup, '')   
                    FROM #tempTreasuryCustomer_TM tmp    
                    JOIN dbo.AccountSegment_v acs    
	                    ON tmp.NoRekInvestor    = acs.AccountId    
                    WHERE acs.ValidYear    = @nValidYear 
	                    AND tmp.ChannelReg IN ('OM', 'RMM', 'WOB')   
    
                    SELECT @cNonIDRAccount = ACCTNO    
                    FROM #tempCustAccNumber    
        
                    SELECT @cFuncGroupNonIDR    = ISNULL(FunctionalGroup, '')    
	                    , @cSubFuncGroupNonIDR   = ISNULL(SubFunctionalGroup, '')    
                    FROM dbo.AccountSegment_v    
                    WHERE AccountId             = @cNonIDRAccount    
	                    AND ValidYear           = @nValidYear    
        
                    UPDATE #tempTreasuryCustomer_TM    
                    SET FuncGroupNonIDR         = @cFuncGroupNonIDR
	                    , SubFuncGroupNonIDR     = @cSubFuncGroupNonIDR
                    WHERE ChannelReg IN ('OM', 'RMM', 'WOB')  
  
                    IF EXISTS (SELECT TOP 1 1 FROM #tempTreasuryCustomer_TM t 
				                    INNER JOIN SQL_REPLICATE..PSEmployeeAccount_TM ps 
				                    ON t.CIFNo = ps.ACCOUNT_EC_ID 
					                    AND ps.ACCOUNT_TYPE_PYE = 'C'
					                    AND t.ChannelReg IN ('OM', 'RMM', 'WOB') )
                    BEGIN
	                    UPDATE #tempTreasuryCustomer_TM    
	                    SET FlagKaryawan = 1
                    END

                    BEGIN TRAN 
                    BEGIN TRY    
	                    INSERT INTO dbo.TreasuryCustomer_TM
		                    (
		                    SecAccNo          
		                    , CIFNo          
		                    , Nama          
		                    , BranchCode                              
		                    , JenisIdentitas          
		                    , NoIdentitas          
		                    , TempatLahir          
		                    , TanggalLahir          
		                    , JenisKelamin          
		                    , JenisPekerjaan          
		                    , AlamatIdentitas          
		                    , KodeKota          
		                    , KodePropinsi          
		                    , NoTelp          
		                    , NoHP          
		                    , NoFax          
		                    , Email          
		                    , NoRekInvestor          
		                    , AlamatSurat          
		                    , NIK_CS       
		                    , NPWP          
		                    , [Status]          
		                    , InsertedBy          
		                    , InsertedDate          
		                    , LastUpdateBy          
		                    , LastUpdateDate                           
		                    , MetodeKontak                                     
		                    , RiskProfile                                              
		                    , LastRiskProfileUpdateDate                                
		                    , BankCustodyCode1                                         
		                    , BankCustodySecurityNo1                                   
		                    , BankCustodyCode2                                         
		                    , BankCustodySecurityNo2              
		                    , BankCustodyCode3                                         
		                    , BankCustodySecurityNo3                           
		                    , SyaratKetentuanBit                          
		                    , TanggalPengisian                          
		                    , AuthorizeBranch                          
		                    , Citizenship                      
		                    , RiskProfileCode 
		                    , FlagKaryawan                           
		                    , IsSuratUseRumah                          
		                    , AlamatSuratSequence                      
		                    , KodeAlamatSurat                          
		                    , KodeCabangAlamatSurat                         
		                    , FuncGroupIDR                           
		                    , SubFuncGroupIDR                          
		                    , FuncGroupNonIDR                          
		                    , SubFuncGroupNonIDR                          
		                    , FlagPremier                           
		                    , NikReferentor                       
		                    , [Guid]                        
		                    , RiskProfileExpiredDate                        
		                    , IdentitasExpiredDate                       
		                    , FuncGroupIDRTA                           
		                    , SubFuncGroupIDRTA                          
		                    , FuncGroupNonIDRTA                          
		                    , SubFuncGroupNonIDRTA                          
		                    , [Signature]                   
		                    , isOnlineAcc                   
		                    , Summary                  
		                    , Spouse      
		                    , ApprovalNNH                    
		                    , TanggalApprovalNNH    
		                    , RegViaONEMobile    
		                    , IDNeedReview    
		                    , IDReviewBit    
		                    , [KITASNo]
		                    , [KITASExpDate]
		                    , [KITASLastUpdateDate]
		                    , ChannelReg
		                    )       
	                    SELECT @cSecAccNo AS SecAccNo          
		                    , @cCIFNo AS CIFNo          
		                    , Nama          
		                    , BranchCode                              
		                    , JenisIdentitas          
		                    , NoIdentitas          
		                    , TempatLahir                              
		                    , strTanggalLahir AS TanggalLahir          
		                    , JenisKelamin                              
		                    , JenisPekerjaan          
		                    , AlamatIdentitas          
		                    , KodeKota          
		                    , KodePropinsi          
		                    , NoTelp          
		                    , NoHP          
		                    , NoFax          
		                    , Email          
		                    , NoRekInvestor          
		                    , AlamatSurat          
		                    , NIK_CS          
		                    , NPWP          
		                    , [Status]          
		                    , InsertedBy          
		                    , @dcurrent_working_date AS InsertedDate          
		                    , LastUpdateBy          
		                    , strLastUpdateDate  AS LastUpdateDate                           
		                    , MetodeKontak                                     
		                    , RiskProfile                                              
		                    , strLastRiskProfileUpdateDate  AS LastRiskProfileUpdateDate                               
		                    , BankCustodyCode1                                         
		                    , BankCustodySecurityNo1                                   
		                    , BankCustodyCode2                                         
		                    , BankCustodySecurityNo2                                   
		                    , BankCustodyCode3                                         
		                    , BankCustodySecurityNo3                           
		                    , SyaratKetentuanBit                          
		                    , CONVERT(VARCHAR, strTanggalPengisian, 112)   AS TanggalPengisian                        
		                    , AuthorizeBranch                          
		                    , Citizenship                      
		                    , RiskProfileCode                  
		                    , FlagKaryawan                           
		                    , IsSuratUseRumah                
		                    , AlamatSuratSequence                         
		                    , KodeAlamatSurat                          
		                    , KodeCabangAlamatSurat                         
		                    , FuncGroupIDR                           
		                    , SubFuncGroupIDR                          
		                    , FuncGroupNonIDR                          
		                    , SubFuncGroupNonIDR         
		                    , FlagPremier                           
		                    , NikReferentor         
		                    , newid()                       
		                    , strRiskProfileExpiredDate AS RiskProfileExpiredDate                       
		                    , strIdentitasExpiredDate  AS IdentitasExpiredDate                  
		                    , FuncGroupIDRTA                           
		                    , SubFuncGroupIDRTA                          
		                    , FuncGroupNonIDRTA                          
		                    , SubFuncGroupNonIDRTA                    
		                    , [Signature]
		                    , isOnlineAcc                   
		                    , Summary  
		                    , Spouse            
		                    , ApprovalNNH    
		                    , CASE WHEN ISNULL(strTanggalApprovalNNH, '') = '' THEN NULL ELSE strTanggalApprovalNNH END AS TanggalApprovalNNH    
		                    , @cRegViaOneMobile    
		                    , @cIDNeedReview    
		                    , 0    
		                    , [KITASNo]
		                    , [KITASExpDate]
		                    , [KITASLastUpdateDate]
		                    , ChannelReg
	                    FROM #tempTreasuryCustomer_TM    
		
	                    IF @@error!=0                          
	                    BEGIN                          
		                    SET @message='Gagal INSERT Registrasi Nasabah ke TABLE TreasuryCustomer_TM'                          
		                    GOTO ERR_HANDLER                          
	                    END                          
	                    ELSE                          
	                    BEGIN 

		                    SELECT @nCIFId = @@identity                          
		                    SELECT @cSecAccNo = 'N' + RIGHT(REPLICATE('0',7) + CAST(@nCIFId AS VARCHAR ), 7)                          
		                    SELECT @cSecAccNo AS SecAccNo                          
		
		                    IF NOT EXISTS(SELECT TOP 1 1 
				                    FROM dbo.TreasuryCustomerChange_TH                          
				                    WHERE [CIFId] = @nCIFId)                          
		                    BEGIN                          
			                    INSERT dbo.TreasuryCustomerChange_TH 
					                    (                          
					                    [CIFId]
					                    , [ChangeSyaratKetentuan]
					                    , [ChangeProfileResiko]
					                    , [ChangeRekRelasi]                          
					                    )                          
			                    SELECT @nCIFId AS CIFId
					                    , 1
					                    , 1
					                    , 0                          
		                    END  
	
		                    --Insert Data Rekening Valas Nasabah
		                    IF (SELECT COUNT(1) FROM #tempCustAccNumber) > 0              
		                    BEGIN                          
			                    INSERT dbo.TRSCustAccNumber_TR                          
				                    (
				                    CIFId
				                    , CcyCode
				                    , NoRekInvestor
				                    , NamaPemilik
				                    , IsJoin
				                    , Relasi
				                    ,ProductCode
				                    )                          
			                    SELECT @nCIFId AS CIFId
				                    , CCY
				                    , ACCTNO
				                    , NAMA_PRIMARY_OWNER_JOIN
				                    , STATUS_JOIN
				                    , RELASI
				                    , PRODUCT_CODE                          
			                    FROM #tempCustAccNumber
		                          
			                    IF @@error!=0                          
			                    BEGIN                          
				                    SET @message='Gagal INSERT data rekening nasabah ke TABLE TRSCustAccNumber_TR'                          
				                    GOTO ERR_HANDLER                          
			                    END                          
		                    END  
	
		                    --Insert Data Rekening Tax Amnesty Nasabah			                        
		                    IF(SELECT COUNT(1) FROM #tempRekTaxAmnesty) > 0                        
		                    BEGIN                       
			                    INSERT dbo.TRSRekTaxAmnesty_TR                          
				                    (
				                    CIFId
				                    , CcyCode
				                    , NoRekInvestor
				                    , NamaPemilik
				                    )                    
			                    SELECT @nCIFId AS CIFId
				                    , CurrencyCode
				                    , ACCTNO
				                    , AccountName                          
			                    FROM #tempRekTaxAmnesty  
		                     
			                    IF @@error!=0                          
			                    BEGIN                          
				                    SET @message='Gagal INSERT data rekening nasabah ke TABLE TRSRekTaxAmnesty_TR'                          
				                    GOTO ERR_HANDLER                 
			                    END              
		                    END   
    
		                    --Insert Data Dokument pendukung (sudah tidak dipakai)
		                    IF(SELECT COUNT(1) FROM #tmpDocsOAO) > 0                              
		                    BEGIN                                      
			                    UPDATE tm                
			                    SET tm.StatusDocs = 'O'                
			                    FROM TRSOAODocumentFiles_TM tm JOIN #tmpDocsOAO oa                
			                    ON tm.MenuName = oa.MenuName 
				                    AND tm.ParamId = oa.ParamId 
		               
			                    INSERT INTO TRSOAODocumentFiles_TM                
				                    (                 
				                    MenuName
				                    , ParamId
				                    , NamaFile
				                    , JenisFile
				                    , ResultId
				                    , StatusDocs                
				                    )                                
			                    SELECT MenuName
				                    , ParamId
				                    , NamaFile
				                    , JenisFile
				                    , ResultId
				                    , 'I'                   
			                    FROM #tmpDocsOAO  
		        
			                    INSERT INTO SQL_ACC.dbo.ProACCASODocRepository_TM 
				                    (          
				                    CIFNo
				                    , FNDocTitle
				                    , FNDocID
				                    , ObjStore
				                    , DocType
				                    , Keterangan
				                    , CompletionDate
				                    , Dus
				                    , Batch
				                    , Location
				                    , Rak
				                    , Baris
				                    , NoBantex            
				                    , CIFName 
				                    , AccountNo
				                    , CompletionNIK
				                    , [Source]            
				                    )            
			                    SELECT ParamId
				                    , NamaFile
				                    , ResultId
				                    , 'ASO'
				                    , 'Dokumen Pendukung'
				                    , 'Dokumen Pendukung master Nasabah'
				                    , GETDATE()
				                    , ''
				                    , ''
				                    , ''
				                    , ''
				                    , ''
				                    , ''
				                    , ''
				                    , ''
				                    , ''        
				                    ,'Online Account Opening Obligasi'            
			                    FROM #tmpDocsOAO            
			                    IF @@error!=0                                
			                    BEGIN                                
				                    SET @message='Gagal INSERT data document ke TABLE TRSOAODocumentFiles_TM'                                
				                    GOTO ERR_HANDLER                                
			                    END                             
		                    END

		                    --Insert Data Repo
		                    --INSERT INTO dbo.TRSCustomerRepoFunded_TR
			                --    (
			                --    CIFId
			                --    , SecAccNo
			                --    , CIFNo
			                --    , GMRA
			                --    , AgreementNo
			                --    , TglMulaiAgreement
			                --    )
		                    --SELECT  @nCIFId AS CIFId
			                --    , @cSecAccNo
			                --    , CIFNo			
			                --    , GMRA			
			                --    , AgreementNo		
			                --    , TglMulaiAgreement
		                    --FROM #tmpREPOFunded
		                    --WHERE GMRA = 1
	
		                    --INSERT INTO dbo.TRSCustomerRepoFunded_TH
			                --    (
			                --    IdRF
			                --    , CIFId
			                --    , SecAccNo
			                --    , CIFNo
			                --    , GMRA
			                --    , AgreementNo
			                --    , TglMulaiAgreement
			                --    , LastUpdateNIK	
			                --    , LastUpdateBranch
			                --    , LastUpdateDate	
			                --    , uGuid			
			                --    , [Description]
			                --    )
		                    --SELECT tr.IdRF
			                --    , tr.CIFId
			                --    , tr.SecAccNo
			                --    , tr.CIFNo
			                --    , tr.GMRA
			                --    , tr.AgreementNo
			                --    , tr.TglMulaiAgreement
			                --    , tmp.LastUpdateNIK	
			                --    , tmp.LastUpdateBranch
			                --    , tmp.LastUpdateDate	
			                --    , newid()
			                --    , 'Insert'
		                    --FROM #tmpREPOFunded tmp
		                    --JOIN dbo.TRSCustomerRepoFunded_TR  tr
		                    --ON tmp.CIFId = tr.CIFId
			                --    AND tmp.CIFNo = tr.CIFNo
 
		                    SELECT @nCIFNo =  CAST(@cCIFNo AS BIGINT)
			                    , @dLastUpdateDate = strLastRiskProfileUpdateDate
			                    , @dExpiredDate = dateadd(yy,3,strLastRiskProfileUpdateDate)
		                    FROM #tempTreasuryCustomer_TM    

		                    IF NOT EXISTS (SELECT TOP 1 1 
				                    FROM SQL_CIF.dbo.ProCIFRiskProfile_TM 
				                    WHERE CIFNumber = @nCIFNo)    
		                    BEGIN    
			                    INSERT INTO SQL_CIF.dbo.ProCIFRiskProfile_TM 
				                    (
				                    CIFNumber
				                    , LastUpdated
				                    , ExpiredDate
                                    , SourceData
				                    )      
			                    SELECT @nCIFNo AS CIFNo
				                    , @dLastUpdateDate
				                    , @dExpiredDate   
                                    , 'NTI Obligasi' AS SourceData
		                    END    

		                    INSERT INTO dbo.TreasuryCustomerLog_TH                           
			                    (                           
			                    CIFId                           
			                    , SecAccNo                           
			                    , CIFNo                           
			                    , Nama                           
			                    , BranchCode                           
			                    , JenisIdentitas                          
			                    , NoIdentitas                           
			                    , TempatLahir                           
			                    , TanggalLahir                           
			                    , JenisKelamin                           
			                    , JenisPekerjaan                           
			                    , AlamatIdentitas                           
			                    , KodeKota                           
			                    , KodePropinsi                           
			                    , NoTelp                           
			                    , NoHP                      
			                    , NoFax                           
			                    , Email                           
			                    , NoRekInvestor                           
			                    , AlamatSurat                           
			                    , NIK_CS                           
			                    , NPWP                           
			                    , [Status]          
			                    , InsertedBy                           
			                    , InsertedDate                           
			                    , LastUpdateBy                           
			                    , LastUpdateDate                           
			                    , RiskProfile                           
			                    , LastRiskProfileUpdateDate   
			                    , BankCustodyCode1                           
			                    , BankCustodyCode2                           
			                    , BankCustodyCode3                           
			                    , BankCustodySecurityNo1                           
			                    , BankCustodySecurityNo2                           
			                    , BankCustodySecurityNo3                           
			                    , MetodeKontak                           
			                    , ValasCcyCode                           
			                    , ValasNoRekInvestor                           
			                    , ValasNamaPemilik                           
			                    , ValasIsJoin                           
			                    , ValasRelasi                           
			                    , ValasProductCode                           
			                    , SyaratKetentuanBit                           
			                    , TanggalPengisian                                     
			                    , NoRekIDRTaxAmnesty                        
			                    , NoRekNonIDRTaxAmnesty                                        
			                    , Citizenship                      
			                    , RiskProfileCode                  
			                    , FlagKaryawan                           
			                    , IsSuratUseRumah                          
			                    , AlamatSuratSequence                         
			                    , KodeAlamatSurat                          
			                    , KodeCabangAlamatSurat                         
			                    , FuncGroupIDR                           
			                    , SubFuncGroupIDR                          
			                    , FuncGroupNonIDR                          
			                    , SubFuncGroupNonIDR                          
			                    , FlagPremier                           
			                    , NikReferentor                       
			                    , RiskProfileExpiredDate                       
			                    , IdentitasExpiredDate                    
			                    , FuncGroupIDRTA                           
			                    , SubFuncGroupIDRTA                          
			                    , FuncGroupNonIDRTA                         
			                    , SubFuncGroupNonIDRTA                     
			                    , [Signature]                   
			                    , isOnlineAcc                   
			                    , Summary                  
			                    , Spouse       
			                    , ApprovalNNH    
			                    , TanggalApprovalNNH    
			                    , RegViaONEMobile    
			                    , IDNeedReview    
			                    , IDReviewBit    
			                    , [KITASNo]
			                    , [KITASExpDate]
			                    , [KITASLastUpdateDate]
                                , SourceData		                        
                                , ChannelReg
			                    )                           
		                    SELECT                           
			                    @nCIFId AS CIFId                           
			                    , @cInsertSecAcc AS SecAccNo                           
			                    , @cCIFNo AS CIFNo                           
			                    , ttc.Nama                           
			                    , ttc.BranchCode                           
			                    , ttc.JenisIdentitas                           
			                    , ttc.NoIdentitas                           
			                    , ttc.TempatLahir                           
			                    , ttc.strTanggalLahir AS TanggalLahir               
			                    , ttc.JenisKelamin                           
			                    , ttc.JenisPekerjaan          
			                    , ttc.AlamatIdentitas                           
			                    , ttc.KodeKota                      
			                    , ttc.KodePropinsi                           
			                    , ttc.NoTelp                           
			                    , ttc.NoHP                           
			                    , ttc.NoFax                           
			                    , ttc.Email                           
			                    , ttc.NoRekInvestor                           
			                    , ttc.AlamatSurat                           
			                    , ttc.NIK_CS                           
			                    , ttc.NPWP                       
			                    , ttc.[Status]          
			                    , ttc.InsertedBy                           
			                    , @dcurrent_working_date AS InsertedDate                          
			                    , ttc.LastUpdateBy                           
			                    , ttc.strLastUpdateDate AS LastUpdateDate                           
			                    , ttc.RiskProfile                           
			                    , ttc.strLastRiskProfileUpdateDate AS LastRiskProfileUpdateDate                           
			                    , ttc.BankCustodyCode1                           
			                    , ttc.BankCustodyCode2                           
			                    , ttc.BankCustodyCode3                           
			                    , ttc.BankCustodySecurityNo1                           
			                    , ttc.BankCustodySecurityNo2                           
			                    , ttc.BankCustodySecurityNo3                           
			                    , ttc.MetodeKontak                           
			                    , tcan.CCY AS ValasCcyCode                           
			                    , tcan.ACCTNO AS ValasNoRekInvestor                           
			                    , tcan.NAMA_PRIMARY_OWNER_JOIN AS ValasNamaPemilik                           
			                    , tcan.STATUS_JOIN AS ValasIsJoin                           
			                    , tcan.RELASI AS ValasRelasi                           
			                    , tcan.PRODUCT_CODE AS ValasProductCode                           
			                    , ttc.SyaratKetentuanBit                           
			                    , CONVERT(VARCHAR, ttc.strTanggalPengisian, 112) AS TanggalPengisian                        
			                    , trtaIDR.ACCTNO                        
			                    , trtaNonIDR.ACCTNO                                        
			                    , ttc.Citizenship                      
			                    , ttc.RiskProfileCode                  
			                    , ttc.FlagKaryawan                           
			                    , ttc.IsSuratUseRumah                          
			                    , ttc.AlamatSuratSequence                         
			                    , ttc.KodeAlamatSurat                          
			                    , ttc.KodeCabangAlamatSurat                         
			                    , ttc.FuncGroupIDR                           
			                    , ttc.SubFuncGroupIDR                          
			                    , ttc.FuncGroupNonIDR                          
			                    , ttc.SubFuncGroupNonIDR                          
			                    , ttc.FlagPremier                           
			                    , ttc.NikReferentor                       
			                    , ttc.strRiskProfileExpiredDate AS RiskProfileExpiredDate                        
			                    , ttc.strIdentitasExpiredDate  AS IdentitasExpiredDate                   
			                    , ttc.FuncGroupIDRTA                           
			                    , ttc.SubFuncGroupIDRTA                          
			                    , ttc.FuncGroupNonIDRTA                          
			                    , ttc.SubFuncGroupNonIDRTA                    
			                    , [Signature]
			                    , isOnlineAcc                   
			                    , Summary  
			                    , ttc.Spouse          
			                    , ttc.ApprovalNNH    
			                    , ttc.strTanggalApprovalNNH AS TanggalApprovalNNH    
			                    , @cRegViaOneMobile    
			                    , @cIDNeedReview    
			                    , 0    
			                    , ttc.[KITASNo]
			                    , ttc.[KITASExpDate]
			                    , ttc.[KITASLastUpdateDate]
                                , 'NTI Obligasi' AS SourceData
			                    , ttc.ChannelReg
		                    FROM #tempTreasuryCustomer_TM ttc                           
		                    LEFT JOIN #tempCustAccNumber tcan                                           
		                    ON 1 = 1                           
		                    LEFT JOIN #tempRekTaxAmnesty trtaIDR                        
		                    ON 1 = 1 AND trtaIDR.CurrencyCode = 'IDR'   
		                    LEFT JOIN #tempRekTaxAmnesty trtaNonIDR                        
		                    ON 1 = 1 AND trtaNonIDR.CurrencyCode != 'IDR' 
	                    END

	                    COMMIT TRAN 
                    END TRY 

                    BEGIN CATCH
	                    SET @message=ERROR_MESSAGE()                              
	                    GOTO ERR_HANDLER    
                    END CATCH

                    SET @pcSecAccNo = LEFT(@cSecAccNo, 10)    
                    --    ------------------------------------------------------------------------------------------    
	      
                    ERR_HANDLER: 
	                    --IF @@Trancount > 0 
		                --    ROLLBACK TRAN 
		                         
	                    --IF @message is NULL                           
	                    --BEGIN                          
		                --    SET @message = 'Unkown Error'                          
	                    --END 
	                         
	                    --IF @nOK <> 0 OR @@error <> 0 
		                --    RAISERROR (@message, 16, 1)
                        
                        IF @message is NOT NULL 
						BEGIN
							IF @@Trancount > 0 
			                    ROLLBACK TRAN 

		                    RAISERROR (@message, 16, 1)
						END
                            ";
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[7];
                sqlParam[0] = new SqlParameter("@xmlInput", xml);
                sqlParam[1] = new SqlParameter("@piSignature", SqlDbType.Image);
                if (paramIn.Data.Signature == null)
                    sqlParam[1].Value = DBNull.Value;
                else
                    sqlParam[1].Value = paramIn.Data.Signature;
                sqlParam[2] = new SqlParameter("@pbIsOnlineAcc", SqlDbType.Bit);
                if (paramIn.Data.IsOnlineAcc == null)
                    sqlParam[2].Value = DBNull.Value;
                else
                    sqlParam[2].Value = paramIn.Data.IsOnlineAcc;
                sqlParam[3] = new SqlParameter("@piSummary", SqlDbType.Image);
                if (paramIn.Data.Summary == null)
                    sqlParam[3].Value = DBNull.Value;
                else
                    sqlParam[3].Value = paramIn.Data.Summary;
                sqlParam[4] = new SqlParameter("@pxXMLDocs", xmlDoc);
                sqlParam[5] = new SqlParameter("@pcChannelReg", paramIn.Data.ChannelRegistration);
                sqlParam[6] = new SqlParameter("@pcSecAccNo", SqlDbType.VarChar, 10);
                sqlParam[6].Direction = ParameterDirection.Output;

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, query, ref sqlParam, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                string tmpSecAccNo = sqlParam[6].Value.ToString();

                msgResponse.IsSuccess = true;
                msgResponse.Data = new CreateTreasuryCustomerRs
                {
                    CIFNo = paramIn.Data.CustomerData.CIFNo,
                    SecurityAccountNo = tmpSecAccNo
                };
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.Data = null;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }

        public ApiMessage<string> SetupSecAccNo(ApiMessage paramIn)
        {
            ApiMessage<string> setupAccNoRs = new ApiMessage<string>();
            setupAccNoRs.copyHeaderForReply(paramIn);

            string newSecAccno = "";
            try
            {
                string strErrMsg = "";
                DataSet dsOut = new DataSet();

                #region Query Get Max identity Table TreasuryCustomer_TM
                var queryString = @"
                    DECLARE @cErrMsg		VARCHAR(MAX)
                            , @nNextCIFId   BIGINT    
                            , @cSecAccNo    VARCHAR(20)  
                    
                    BEGIN TRY
                        SET @nNextCIFId = 1 + isnull( IDENT_CURRENT('TreasuryCustomer_TM'), 0 )          
                        SET @cSecAccNo = 'N' + right( replicate('0',7) + cast(@nNextCIFId as varchar(8)), 7 ) 
                    
                        SELECT @cSecAccNo AS SecAccNo
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                        BEGIN
		                    ROLLBACK TRANSACTION
                        END
            
                        DECLARE @ErrorMessage nvarchar(max)
                            , @ErrorSeverity int
                            , @ErrorState int
            
                        SELECT
                        @ErrorMessage = ERROR_MESSAGE()
                        , @ErrorSeverity = ERROR_SEVERITY()
                        , @ErrorState = ERROR_STATE()
            
                        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
                    END CATCH";
                #endregion

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, queryString, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                if (dsOut.Tables.Count < 1)
                    throw new Exception("Cannot Find Latest SecAccNo!");

                newSecAccno = dsOut.Tables[0].Rows[0][0].ToString();

                setupAccNoRs.IsSuccess = true;
                setupAccNoRs.Data = newSecAccno;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                setupAccNoRs.IsSuccess = false;
                setupAccNoRs.ErrorCode = "500";
                setupAccNoRs.ErrorDescription = ex.Message;
            }
            finally
            {
                setupAccNoRs.MessageDateTime = DateTime.Now;
            }

            return setupAccNoRs;
        }

        public ApiMessage<UpdateTreasuryCustomerRs> UpdateTreasuryCustomer(ApiMessage<UpdateTreasuryCustomerRq> paramIn)
        {
            throw new NotImplementedException();
        }

        public ApiMessage<ValidateTreasuryCustomerRs> ValidateTreasuryCustomer(ApiMessage<ValidateTreasuryCustomerRq> paramIn)
        {
            throw new NotImplementedException();
        }
        public ApiMessage<List<OnlineGeneralParam>> GetOnlineGeneralParam(ApiMessage<OnlineGeneralParam> paramIn)
        {
            ApiMessage<List<OnlineGeneralParam>> OnlineParamRes = new ApiMessage<List<OnlineGeneralParam>>();
            OnlineParamRes.copyHeaderForReply(paramIn);

            List<OnlineGeneralParam> param = new List<OnlineGeneralParam>();
            string strErrMsg = "";
            try
            {
                DataSet dsOut = new DataSet();
                #region Query Inquiry ORIOnlineGeneralParam
                var queryString = @"
                    SELECT GroupId, GroupDesc, ValueId, ValueDesc
                    FROM dbo.ORIOnlineGeneralParam_TR 
                    WHERE (ISNULL(@pnGroupId, 0) = 0 OR GroupId = @pnGroupId)
	                    AND (ISNULL(@pcGroupDesc, '') = '' OR GroupDesc = @pcGroupDesc)
	                    AND (ISNULL(@pcValueId, '') = '' OR ValueId = @pcValueId)
	                    AND (ISNULL(@pcValueDesc, '') = '' OR ValueDesc = @pcValueDesc)";
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[4];
                sqlParam[0] = new SqlParameter("@pnGroupId", paramIn.Data.GroupId);
                sqlParam[1] = new SqlParameter("@pcGroupDesc", paramIn.Data.GroupDesc);
                sqlParam[2] = new SqlParameter("@pcValueId", paramIn.Data.ValueId);
                sqlParam[3] = new SqlParameter("@pcValueDesc", paramIn.Data.ValueDesc);

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, queryString, ref sqlParam, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                param = JsonConvert.DeserializeObject<List<OnlineGeneralParam>>(JsonConvert.SerializeObject(dsOut.Tables[0]));

                OnlineParamRes.IsSuccess = true;
                OnlineParamRes.Data = param;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                OnlineParamRes.IsSuccess = false;
                OnlineParamRes.Data = null;
                OnlineParamRes.ErrorCode = "500";
                OnlineParamRes.ErrorDescription = ex.Message;
            }
            finally
            {
                OnlineParamRes.MessageDateTime = DateTime.Now;
            }

            return OnlineParamRes;
        }
        public ApiMessage<GetDataCoreRs> GetDataCore(ApiMessage<GetDataCoreRq> paramIn)
        {
            ApiMessage<GetDataCoreRs> msgResponse = new ApiMessage<GetDataCoreRs>();
            msgResponse.copyHeaderForReply(paramIn);

            try
            {
                DataSet dsOut = new DataSet();
                string strErrMsg = "";
                #region Query Based On TRSRetailGetDataCore
                var queryString = @"
                    DECLARE     
                        @cErrMsg					VARCHAR(200)     
                        ,@nErrNo					INT     
                        ,@nOK						INT    
                        ,@nJDate					DECIMAL     
                        ,@cCIFNo					VARCHAR(19)    
                        ,@nCIFId					BIGINT     
                        ,@nNextCIFId				BIGINT     
                        ,@nJenis					TINYINT   
                        ,@nSessionIdent				BIGINT     
                        ,@cCFASEQ					CHAR(3)     
                        ,@cCFINVC					CHAR(1)     
                        ,@cCF2DT2					CHAR(40)    
                        ,@cCFUIC8					CHAR(1)    
                        ,@cCFSSCD					CHAR(4)    
                        ,@cCFIEX7					CHAR(7)     
                        ,@cYearExpiredRisk			VARCHAR(20)    
                        ,@nYearExpiredRisk			INT    
                        ,@cRegViaONEMobile			CHAR(1)     
						,@cCurKITASNo				VARCHAR(40)
						,@cCurKITASExpDate			VARCHAR(10)
						,@cCurKITASLastUpdDate		VARCHAR(10)
						,@dLastUpdateRiskProfile	DATETIME
						,@dExpiredRiskProfile		DATETIME
						,@dExpiredID				DATETIME
    
    
                    IF OBJECT_ID('tempdb..#xtmpProCIFChange') IS NOT NULL 
	                    DROP TABLE #xtmpProCIFChange

                    IF OBJECT_ID('tempdb..#tmpCustAccNumber') IS NOT NULL 
	                    DROP TABLE #tmpCustAccNumber

                    CREATE TABLE #xtmpProCIFChange     
                    (     
                        ChangeId  INT,     
                        UpdateDate  DATETIME     
                    )     
                    CREATE INDEX ix_#xtmpProCIFChange ON #xtmpProCIFChange ( ChangeId ) INCLUDE ( UpdateDate )     
    
                    SET @nJDate = dbo.fnDatetimeToJulian( GETDATE() )     
    
                    SET @cCIFNo = RIGHT( REPLICATE('0', 19) + CAST(@nCIFNo AS VARCHAR), 19)     
    
                    SELECT @cYearExpiredRisk = Value     
                    FROM TRSParameter_TR     
                    WHERE Code = 'ParameterRiskProfile'     
    
                    SET @nYearExpiredRisk =     
                        CASE WHEN ISNUMERIC(@cYearExpiredRisk) = 1     
                        THEN CAST(@cYearExpiredRisk AS INT)     
                        ELSE 3     
                        END     
    
                    BEGIN TRY
	                    SELECT @cCIFNo = CFCIF,     
		                    @cNama = CFNA1,     
		                    @cBranchCode = CFBRNN,     
		                    @cTempatLahir = CFYBIP,     
		                    @dTanggalLahir = CONVERT(VARCHAR(8), SQL_SIBS.dbo.fnJulian2Date(CFBIRD),112),     
		                    @cJenisKelamin =     
		                    CASE CFSEX     
		                    WHEN 'M' THEN '1'     
		                    WHEN 'F' THEN '2'     
		                    ELSE ''     
		                    END,     
		                    @nJenis =     
		                    CASE CFCLAS     
		                    WHEN 'A' then 1  --perorangan     
		                    ELSE 2    --non perorangan     
		                    END,     
		                    @cNoIdentitas =     
		                    CASE WHEN CFSSCD like 'KTP%' OR CFSSCD like 'PP%' OR CFSSCD like 'KITS%'     
		                    THEN CFSSNO     
		                    ELSE ''     
		                    END,     
		                    @pcCitizenship = CFCITZ,     
		                    @cCFUIC8 = CFUIC8,     
		                    @cCFSSCD = CFSSCD,     
		                    @cCFIEX7 = RTRIM(CFIEX7)     
	                    FROM SQL_SIBS.dbo.CFMAST     
	                    WHERE CFCIF = @cCIFNo     
    
	                    -- Jenis Risk Profile     
	                    SELECT @pcJenisRiskProfile = CP3DSC,     
		                    @pcRiskProfileCode = CP3UCD     
	                    FROM SQL_CIF.dbo.CFPAR3_v     
	                    WHERE CP3UCD = @cCFUIC8     
		                    AND CP3UIC = 8     
    
	                    IF ISNULL(@cCIFNo,'') = ''     
	                    BEGIN     
		                    SET @cErrMsg = 'Nasabah belum terdaftar di core-bank'     
		                    GOTO ErrorHandler     
	                    END     
    
	                    IF ISNULL(@cNama, '') = ''     
	                    BEGIN     
		                    SET @cErrMsg = 'Nama Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
	                    END     
    
	                    IF ISNULL(@cBranchCode, '') = ''     
	                    BEGIN     
		                    SET @cErrMsg = 'Branch Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
	                    END     
    
	                    IF ISNULL(@cTempatLahir, '') = ''     
	                    BEGIN     
		                    SET @cErrMsg = 'Tempat lahir Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
	                    END     
    
	                    IF ISNULL(CAST(@dTanggalLahir AS VARCHAR), '') = ''     
	                    BEGIN     
		                    SET @cErrMsg = 'Tanggal lahir Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
	                    END     
    
	                    IF ISNULL(@cJenisKelamin, '') = ''     
	                    BEGIN     
		                    IF @nJenis = 1     
		                    BEGIN  
		                    SET @cErrMsg = 'Jenis kelamin Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
		                    END     
		                    ELSE IF @nJenis = 2     
		                    BEGIN     
		                    SET @cJenisKelamin = '3'     
		                    END     
	                    END     
    
	                    IF @nJenis = 1     
	                    BEGIN     
		                    IF @cCFSSCD = 'PP'     
		                    BEGIN     
		                    SET @cJenisIdentitas = '4'     
		                    END
		                    ELSE IF @cCFSSCD = 'KITS'    
		                    BEGIN
		                    SET @cJenisIdentitas = '5'	
		                    END
		                    ELSE     
		                    BEGIN     
		                    SET @cJenisIdentitas = '1'     
		                    END     
	                    END     
	                    ELSE     
	                    BEGIN     
		                    SET @cJenisIdentitas = '3'     
	                    END     
    
	                    IF ISNULL(@cNoIdentitas, '') = '' AND @nJenis = 1  AND @pcCitizenship = '000'
	                    BEGIN     
		                    SELECT @cJenisIdentitas = '1',     
		                    @cNoIdentitas = CFSSNO,     
		                    @cCFIEX7 = RTRIM(CFIEX7)     
		                    FROM SQL_SIBS.dbo.CFAIDN     
		                    WHERE CFCIF = @cCIFNo     
		                    AND CFSSCD like 'KTP%'     
     
		                    IF ISNULL(@cNoIdentitas, '') = ''     
		                    BEGIN     
		                    SET @cErrMsg = 'Nomor KTP Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
		                    END     
	                    END     
	                    ELSE IF ISNULL(@cNoIdentitas, '') = '' and @nJenis = 2     
	                    BEGIN     
		                    IF EXISTS(SELECT TOP 1 1 FROM SQL_SIBS.dbo.CFAIDN WHERE CFCIF = @cCIFNo)     
		                    BEGIN     
		                    SELECT @cJenisIdentitas = '3',     
		                    @cNoIdentitas = CFSSNO,     
		                    @cCFIEX7 = RTRIM(CFIEX7)     
		                    FROM SQL_SIBS.dbo.CFAIDN     
		                    WHERE CFCIF = @cCIFNo     
		                    END     
		                    ELSE     
		                    BEGIN     
		                    SET @cErrMsg = 'Nomor Identitas Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
		                    END     
	                    END     
    
	                    SELECT @cJenisPekerjaan = b.TRSJob_ID     
	                    FROM SQL_SIBS.dbo.CFZEMP a     
		                    INNER JOIN TRSMapPekerjaan_TR b     
		                    ON a.CFOCCD = b.OCCP_ID     
	                    WHERE a.CFCIF = @cCIFNo     
    
	                    IF @cJenisPekerjaan IS NULL     
	                    BEGIN     
		                    SELECT @cJenisPekerjaan = TRSJob_ID     
		                    FROM TRSMapPekerjaan_TR     
		                    WHERE OCCP_ID = 'LAIN'     
	                    END     
    
	                    IF EXISTS ( SELECT TOP 1 1 FROM SQL_SIBS.dbo.CFADDR WHERE CFCIF = @cCIFNo AND CFUSE = 'Y' )     
	                    BEGIN     
		                    SELECT TOP 1 @cCFINVC = CFINVC,     
		                    @cCFASEQ = CFASEQ,     
		                    @cAlamatIdentitas1 = CFNA2,     
		                    @cAlamatIdentitas2 = CFNA3,     
		                    @cAlamatIdentitas3 = CFNA5,     
		                    @cCF2DT2 = CF2DT2     
		                    FROM SQL_SIBS.dbo.CFADDR     
		                    WHERE CFCIF = @cCIFNo     
		                    AND CFUSE = 'Y'     
		                    ORDER BY     
		                    CASE WHEN CFINVC = 'S' THEN 1     
		                    WHEN CFINVC = 'G' THEN 2     
		                    WHEN CFINVC = 'I' THEN 3     
		                    WHEN CFINVC = 'J' THEN 4     
		                    ELSE 5     
		                    END,     
		                    CAST(CFASEQ AS SMALLINT)     
	                    END     
	                    ELSE     
	                    BEGIN     
		                    --jika alamat sesuai KTP (S) kosong, maka ambil dari alamat sesuai ID (G)     
		                    --jika kosong ambil dari alamat KIMS/KITAS (I)     
		                    --jika kosong ambil dari alamat saat ini (J)     
     
		                    SET @cCFINVC = 'S'  -- alamat sesuai KTP     
     
		                    SELECT TOP 1 @cCFASEQ = CFASEQ,     
		                    @cAlamatIdentitas1 = CFNA2,     
		                    @cAlamatIdentitas2 = CFNA3,     
		                    @cAlamatIdentitas3 = CFNA5,     
		                    @cCF2DT2 = CF2DT2      
		                    FROM SQL_SIBS.dbo.CFADDR     
		                    WHERE CFCIF = @cCIFNo     
		                    AND CFINVC = @cCFINVC     
		                    ORDER BY CAST(CFASEQ AS SMALLINT)     
     
		                    IF @cCFASEQ IS NULL OR ( RTRIM(@cAlamatIdentitas1) = '' AND RTRIM(@cAlamatIdentitas2) = '' AND RTRIM(@cAlamatIdentitas3) = '' )     
		                    BEGIN     
			                    SET @cCFINVC = 'G'  -- alamat sesuai ID     
      
			                    SELECT TOP 1 @cCFASEQ = CFASEQ,     
			                    @cAlamatIdentitas1 = CFNA2,     
			                    @cAlamatIdentitas2 = CFNA3,     
			                    @cAlamatIdentitas3 = CFNA5,     
			                    @cCF2DT2 = CF2DT2     
			                    FROM SQL_SIBS.dbo.CFADDR     
			                    WHERE CFCIF = @cCIFNo     
			                    AND CFINVC = @cCFINVC     
			                    --ORDER BY CFASEQ     
			                    ORDER BY CAST(CFASEQ AS SMALLINT)     
      
			                    IF @cCFASEQ IS NULL OR ( RTRIM(@cAlamatIdentitas1) = '' AND RTRIM(@cAlamatIdentitas2) = '' AND RTRIM(@cAlamatIdentitas3) = '' )     
			                    BEGIN     
				                    SET @cCFINVC = 'I'  -- KIMS/KITAS   
       
				                    SELECT TOP 1 @cCFASEQ = CFASEQ,     
				                    @cAlamatIdentitas1 = CFNA2,     
				                    @cAlamatIdentitas2 = CFNA3,     
				                    @cAlamatIdentitas3 = CFNA5,     
				                    @cCF2DT2 = CF2DT2     
				                    FROM SQL_SIBS.dbo.CFADDR     
				                    WHERE CFCIF = @cCIFNo     
				                    AND CFINVC = @cCFINVC     
				                    ORDER BY CAST(CFASEQ AS SMALLINT)     
       
				                    IF @cCFASEQ IS NULL OR ( RTRIM(@cAlamatIdentitas1) = '' AND RTRIM(@cAlamatIdentitas2) = '' AND RTRIM(@cAlamatIdentitas3) = '' )     
				                    BEGIN     
					                    SET @cCFINVC = 'J'  -- alamat saat ini     
        
					                    SELECT TOP 1 @cCFASEQ = CFASEQ,     
						                    @cAlamatIdentitas1 = CFNA2,     
						                    @cAlamatIdentitas2 = CFNA3,     
						                    @cAlamatIdentitas3 = CFNA5,     
						                    @cCF2DT2 = CF2DT2     
					                    FROM SQL_SIBS.dbo.CFADDR     
					                    WHERE CFCIF = @cCIFNo     
				                    AND CFINVC = @cCFINVC     
					                    ORDER BY CAST(CFASEQ AS SMALLINT)     
				                    END     
			                    END      
		                    END     
	                    END     
    
	                    IF (ISNULL(@cAlamatIdentitas1, '') = '' OR ISNULL(@cAlamatIdentitas3, '') = '') AND @nJenis = 1     
	                    BEGIN     
		                    SET @cErrMsg = 'Alamat sesuai identitas Nasabah masih kosong di core-bank'     
		                    GOTO ErrorHandler     
	                    END     

	                    -- Kode Kota & Provinsi -     
	                    SELECT @cKodeKota = k.KodeKota,     
		                    @cKodePropinsi = p.KodePropinsi     
	                    FROM KodeKota_TR k     
		                    INNER JOIN KodePropinsi_TR p     
		                    ON k.KodePropinsi = p.KodePropinsi     
	                    WHERE k.Nama = @cCF2DT2     
    
	                    IF ISNULL(@cKodeKota,'') = ''     
	                    BEGIN     
		                    SELECT @cKodeKota = k.KodeKota,     
		                    @cKodePropinsi = p.KodePropinsi     
		                    FROM CFADDRKodeKotaMapping_TR akmap     
		                    INNER JOIN KodeKota_TR k     
		                    ON akmap.Nama = k.Nama     
		                    INNER JOIN KodePropinsi_TR p     
		                    ON k.KodePropinsi = p.KodePropinsi     
		                    WHERE akmap.CF2DT2 = @cCF2DT2     
	                    END     
                        
                        SET @cNoTelp = ''
	                    --IF EXISTS(SELECT TOP 1 1 FROM SQL_SIBS.dbo.CFCONN WHERE CFCIF = @cCIFNo)     
	                    --BEGIN     
		                --    SELECT TOP 1 @cNoTelp = CFEADD     
		                --    FROM SQL_SIBS.dbo.CFCONN     
		                --    WHERE CFCIF = @cCIFNo     
		                --    AND CFEADC in ('TR', 'TL', 'TK', 'TB', 'TS', 'TP', 'TI', 'HP')     
		                --    ORDER BY     
		                --    CASE WHEN CFEADC = 'TR' THEN 1     
		                --    WHEN CFEADC = 'TL' THEN 2     
		                --    WHEN CFEADC = 'TK' THEN 3     
		                --    WHEN CFEADC = 'TB' THEN 4     
		                --    WHEN CFEADC = 'TI' THEN 5     
		                --    WHEN CFEADC = 'TS' THEN 6     
		                --    WHEN CFEADC = 'TP' THEN 7     
		                --    WHEN CFEADC = 'HP' THEN 8    
		                --    ELSE 9    
		                --    END,     
		                --    CAST(CFZSEQ AS SMALLINT)     
	                    --END     
	                    --ELSE     
	                    --BEGIN     
		                --    SET @cErrMsg = 'Nasabah belum memiliki No. Telepon Rumah'     
		                --    GOTO ErrorHandler     
	                    --END         
    
	                    IF ISNULL(@cSecAccNo,'') = ''     
	                    BEGIN     
		                    -- New     
		                    SET @nCIFId = 0     
     
		                    SET @nNextCIFId = 1 + ISNULL( IDENT_CURRENT('TreasuryCustomer_TM'), 0 )     
     
		                    SET @cSecAccNo = 'N' + RIGHT( REPLICATE('0',7) + CAST(@nNextCIFId AS VARCHAR), 7 )     
	                    END     
	                    ELSE     
	                    BEGIN     
		                    SELECT @nCIFId = CIFId,     
		                    @pcHandphone = NoHP,     
		                    @cRegViaONEMobile = RegViaONEMobile     
		                    FROM TreasuryCustomer_TM     
		                    WHERE SecAccNo = @cSecAccNo     
     
		                    SET @nCIFId = ISNULL(@nCIFId,0)     
	                    END     
    
	                    EXEC @nOK = dbo.GetAccountInfo @cCIFNo, @nSessionIdent OUTPUT     
    
	                    IF @nOK <> 0 OR @@ERROR <> 0     
	                    BEGIN     
		                    SET @cErrMsg = 'gagal ambil data rekening dari core'     
		                    GOTO ErrorHandler     
	                    END     
    
	                    IF @nSessionIdent IS NOT NULL     
	                    BEGIN     
		                    SELECT RowId
				                    ,CIFId
				                    ,CcyCode
				                    ,CASE WHEN LEN(NoRekInvestor) >= 13
							                    THEN RIGHT( REPLICATE('0',14) + CONVERT(VARCHAR(14),NoRekInvestor), 14 ) 
						                    ELSE RIGHT( REPLICATE('0',12) + CONVERT(VARCHAR(13),NoRekInvestor), 12 ) 
					                    END AS [NoRekInvestor]
				                    ,NamaPemilik
				                    ,IsJoin
				                    ,Relasi
				                    ,ProductCode
		                    INTO #tmpCustAccNumber
		                    FROM dbo.TRSCustAccNumber_TR
		                    WHERE CIFId = @nCIFId 
		                    SELECT a.ACCTNO, a.STATUS_JOIN,     
		                    a.CIF_PRIMARY_OWNER_JOIN, a.NAMA_PRIMARY_OWNER_JOIN,     
		                    a.CCY, a.RELASI, a.PRODUCT_CODE,     
		                    CASE WHEN c.NoRekInvestor IS NULL     
		                    THEN 'NEW'     
		                    ELSE 'EXIST'     
		                    END AS isExist     
		                    FROM dbo.trs_getaccount_tmp a     
		                    INNER JOIN TRSSecurityCurrency_TR b     
		                    ON a.CCY = b.CcyCode     
		                    AND b.CcyIsUse = 1     
		                    LEFT JOIN #tmpCustAccNumber c     
		                    ON a.CCY = c.CcyCode     
		                    AND a.ACCTNO = c.NoRekInvestor     
		                    AND c.CIFId = @nCIFId     
		                    LEFT JOIN TAMAST_v d     
		                    ON a.ACCTNO = d.CFACC#     
		                    LEFT JOIN TCLOGT_v e     
		                    ON d.CFCIF# = e.TCLCIF     
		                    AND e.TCFLAG = 1     
		                    AND e.TCEXP7 > @nJDate     
		                    WHERE a.SESSION_IDENT = @nSessionIdent     
		                    AND (     
		                    ( d.TAFLAG = 0 OR d.TAFLAG IS NULL ) OR     
		                    ( d.TAFLAG = 1 AND e.TCLCIF IS NULL )     
		                    )     
     
		                    DELETE dbo.trs_getaccount_tmp     
		                    WHERE SESSION_IDENT = @nSessionIdent     
     
		                    DELETE dbo.trs_getaccount_session     
		                    WHERE SESSION_IDENT = @nSessionIdent     
	                    END     
    
	                    -- Fax     
	                    SELECT TOP 1 @pcFax = CFCONN.CFEADD     
	                    FROM SQL_SIBS.dbo.CFCONN AS CFCONN     
	                    WHERE CFCONN.CFCIF = @cCIFNo     
		                    AND CFEADC IN ('FR', 'FL', 'FX')     
	                    ORDER BY     
		                    CASE     
		                    WHEN CFEADC  = 'FR' THEN 1     
		                    WHEN CFEADC  = 'FL' THEN 2     
		                    WHEN CFEADC  = 'FX' THEN 3     
		                    END,     
		                    CAST(CFZSEQ AS SMALLINT)     
    
	                    -- NPWP     
	                    SELECT @pcNPWP = CFSSNO     
	                    FROM SQL_SIBS.dbo.CFAIDN     
	                    WHERE CFCIF = @cCIFNo     
		                    AND CFSSCD = 'NPWP'     
    
	                    -- EMAIL
	                    SELECT TOP 1 @pcEmail = CFCONN.CFEADD
	                    FROM SQL_SIBS.dbo.CFCONN AS CFCONN     
	                    WHERE CFCONN.CFCIF = @cCIFNo     
		                    AND CFEADC IN ( 'EB', 'EI', 'EK', 'EM', 'EP', 'ES', 'EZ' ) 
	                    ORDER BY
		                    CASE WHEN CFEADC = 'EM' THEN 2 
				                    WHEN CFEADC = 'EK' THEN 3 
				                    WHEN CFEADC = 'EB' THEN 4 
				                    WHEN CFEADC = 'EI' THEN 5 
				                    WHEN CFEADC = 'ES' THEN 6 
				                    WHEN CFEADC = 'EP' THEN 7 
				                    WHEN CFEADC = 'EZ' AND @cRegViaONEMobile = 'Y' 
				                    THEN 1 
				                    WHEN CFEADC = 'EZ' AND ISNULL(@cRegViaONEMobile,'N') <> 'Y' 
		 		                    THEN 8 
				                    ELSE 9 
		                    END, 
		                    CAST(CFZSEQ AS SMALLINT) 
     
		                    -- Handphone     
		                    SELECT TOP 1 @pcHandphone = CFEADD     
		                    FROM SQL_SIBS.dbo.CFCONN     
		                    WHERE CFCIF = @cCIFNo     
		                    AND CFEADC IN ('HP')     
		                    ORDER BY CAST(CFZSEQ AS SMALLINT)     
     
	                    -- Last Update Risk Profile     
	                    INSERT #xtmpProCIFChange     
	                    ( ChangeId, UpdateDate )     
	                    SELECT ChangeId, DATEADD(DAY, DATEDIFF(DAY,0,ApprovedDate), 0)     
	                    FROM SQL_CIF.dbo.ProCIFChangeMaster_TH WITH (NOLOCK)     
	                    WHERE CIFNumber = @nCIFNo     
		                    AND TableName = 'CFMAST'     
		                    AND Status = 2     
    
	                    SELECT @dLastUpdateRiskProfile = MAX(t.UpdateDate)     
	                    FROM #xtmpProCIFChange t     
		                    INNER JOIN SQL_CIF.dbo.ProCIFChangeDetail_TH d WITH (NOLOCK)     
		                    ON t.ChangeId = d.ChangeId     
		                    AND d.ColumnName = 'CFUIC8'     
    
	                    SET @dLastUpdateRiskProfile = ISNULL(@dLastUpdateRiskProfile, GETDATE())
	                    
                        SET @dExpiredRiskProfile = DATEADD ( YEAR, @nYearExpiredRisk, @dLastUpdateRiskProfile )     

	                    SELECT @dLastUpdateRiskProfile = LastUpdated, @dExpiredRiskProfile = ExpiredDate    
	                    FROM SQL_CIF.dbo.ProCIFRiskProfile_TM    
	                    WHERE CIFNumber = @nCIFNo    
                        
                        SET @pdLastUpdateRiskProfile = CONVERT(VARCHAR(8), @dLastUpdateRiskProfile, 112) 
	                    SET @pdExpiredRiskProfile =  CONVERT(VARCHAR(8), @dExpiredRiskProfile, 112) 

                        -- Spouse     
	                    SELECT @pcSpouse = NamaPasangan     
	                    FROM dbo.ProCIFCustPersonalInfo_TM_v     
	                    WHERE CustomerId = @nCIFNo     
    
	                    -- Expired Identitas     
	                    SET @dExpiredID = NULL     
    
	                    IF LEN(@cCFIEX7) = 7 AND ISNUMERIC(@cCFIEX7) = 1     
	                    BEGIN     
		                    IF ISDATE( LEFT(@cCFIEX7,4) + '0101' ) = 1     
		                    BEGIN     
		                    SET @dExpiredID = DATEADD ( DAY, CAST(RIGHT(@cCFIEX7,3) AS INT)-1, LEFT(@cCFIEX7,4) + '0101' )     
		                    END     
	                    END     
    
	                    IF @cJenisIdentitas = 1 AND @dExpiredID IS NULL     
	                    BEGIN     
		                    SET @dExpiredID = DATEADD(YEAR, 100, CONVERT(VARCHAR, GETDATE(), 112))     
	                    END     
    
                        SET @pdExpiredID = CONVERT(VARCHAR(8), @dExpiredID, 112)
						
	                    /*********
	                    * KITAS *
	                    *********/
	                    SELECT @cCurKITASNo				= LTRIM(RTRIM([KITASNo]))
		                    ,@cCurKITASExpDate			= convert(varchar(8), [KITASExpDate], 112)		
		                    ,@pcKITASLastUpdDate		= convert(varchar(8), [KITASLastUpdateDate], 112)
	                    FROM dbo.TreasuryCustomer_TM
	                    WHERE [CIFNo]					= @cCIFNo

	                    --- KITAS AS MAIN ID ---
	                    SELECT 
		                    @pcKITASNo					= LTRIM(RTRIM([CFSSNO]))
		                    ,@pcKITASExpDate			= [CFIEX6]		
	                    FROM dbo.CFMAST_v 
	                    WHERE [CFCIF] = @cCIFNo AND [CFCITZ] <> '000' AND [CFSSCD] = 'KITS'

	                    --- KITAS AS ADDITIONAL ID ---
	                    IF(isnull(@pcKITASNo, '') = '' OR isnull(@pcKITASExpDate, '') = '')
	                    BEGIN
		                    SELECT @pcKITASNo				= LTRIM(RTRIM([CFSSNO]))
			                    ,@pcKITASExpDate			= [CFIEX6]	
		                    FROM dbo.CFAIDN_v
		                    WHERE [CFCIF] = @cCIFNo AND [CFSSCD] = 'KITS'
	                    END

	                    IF (isnull(@cCurKITASNo, '') <> isnull(@pcKITASNo, '')
		                    OR isnull(@cCurKITASExpDate, '') <> isnull(@pcKITASExpDate, ''))
	                    BEGIN
		                    SET @pcKITASLastUpdDate = convert(varchar(8), getdate(), 112)
	                    END

	                    SET @pcKITASNo = isnull(@pcKITASNo, '')
	                    SET @pcKITASExpDate = isnull(@pcKITASExpDate, '0')
	                    SET @pcKITASLastUpdDate = isnull(@pcKITASLastUpdDate, '0')
                    END TRY
                    BEGIN CATCH    
	                    SET @cErrMsg = 'GetDataCore - ' + error_message()     
	                    GOTO ErrorHandler    
                    END CATCH
   
                    ErrorHandler:         
                        IF @nSessionIdent IS NOT NULL     
                        BEGIN     
                                DELETE dbo.trs_getaccount_tmp     
                                WHERE SESSION_IDENT = @nSessionIdent
 
                                DELETE dbo.trs_getaccount_session
                                WHERE SESSION_IDENT = @nSessionIdent                          
                        END
	                    IF @cErrMsg <> ''
		                    RAISERROR(@cErrMsg, 16, 1)  ";
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[30];
                sqlParam[0] = new SqlParameter("@nCIFNo", SqlDbType.BigInt);
                sqlParam[0].Value = Convert.ToInt64(paramIn.Data.CIFNo);

                sqlParam[1] = new SqlParameter("@cSecAccNo", SqlDbType.VarChar, 8);
                sqlParam[1].Direction = ParameterDirection.Output;

                sqlParam[2] = new SqlParameter("@cNama", SqlDbType.VarChar, 95);
                sqlParam[2].Direction = ParameterDirection.Output;

                sqlParam[3] = new SqlParameter("@cBranchCode", SqlDbType.VarChar, 5);
                sqlParam[3].Direction = ParameterDirection.Output;

                sqlParam[4] = new SqlParameter("@cJenisIdentitas", SqlDbType.Char, 1);
                sqlParam[4].Direction = ParameterDirection.Output;

                sqlParam[5] = new SqlParameter("@cNoIdentitas", SqlDbType.VarChar, 40);
                sqlParam[5].Direction = ParameterDirection.Output;

                sqlParam[6] = new SqlParameter("@cTempatLahir", SqlDbType.VarChar, 50);
                sqlParam[6].Direction = ParameterDirection.Output;

                sqlParam[7] = new SqlParameter("@dTanggalLahir", SqlDbType.VarChar, 10);
                sqlParam[7].Direction = ParameterDirection.Output;

                sqlParam[8] = new SqlParameter("@cJenisKelamin", SqlDbType.Char, 1);
                sqlParam[8].Direction = ParameterDirection.Output;

                sqlParam[9] = new SqlParameter("@cJenisPekerjaan", SqlDbType.Char, 2);
                sqlParam[9].Direction = ParameterDirection.Output;

                sqlParam[10] = new SqlParameter("@cAlamatIdentitas1", SqlDbType.VarChar, 40);
                sqlParam[10].Direction = ParameterDirection.Output;

                sqlParam[11] = new SqlParameter("@cAlamatIdentitas2", SqlDbType.VarChar, 40);
                sqlParam[11].Direction = ParameterDirection.Output;

                sqlParam[12] = new SqlParameter("@cAlamatIdentitas3", SqlDbType.VarChar, 40);
                sqlParam[12].Direction = ParameterDirection.Output;

                sqlParam[13] = new SqlParameter("@cKodeKota", SqlDbType.VarChar, 4);
                sqlParam[13].Direction = ParameterDirection.Output;

                sqlParam[14] = new SqlParameter("@cKodePropinsi", SqlDbType.VarChar, 4);
                sqlParam[14].Direction = ParameterDirection.Output;

                sqlParam[15] = new SqlParameter("@cNoTelp", SqlDbType.VarChar, 40);
                sqlParam[15].Direction = ParameterDirection.Output;

                sqlParam[16] = new SqlParameter("@pdExpiredRiskProfile", SqlDbType.VarChar, 10);
                sqlParam[16].Direction = ParameterDirection.Output;

                sqlParam[17] = new SqlParameter("@pcFax", SqlDbType.VarChar, 40);
                sqlParam[17].Direction = ParameterDirection.Output;

                sqlParam[18] = new SqlParameter("@pcEmail", SqlDbType.VarChar, 40);
                sqlParam[18].Direction = ParameterDirection.Output;

                sqlParam[19] = new SqlParameter("@pcJenisRiskProfile", SqlDbType.VarChar, 40);
                sqlParam[19].Direction = ParameterDirection.Output;

                sqlParam[20] = new SqlParameter("@pdLastUpdateRiskProfile", SqlDbType.VarChar, 10);
                sqlParam[20].Direction = ParameterDirection.Output;

                sqlParam[21] = new SqlParameter("@pcHandphone", SqlDbType.VarChar, 40);
                sqlParam[21].Direction = ParameterDirection.Output;

                sqlParam[22] = new SqlParameter("@pcNPWP", SqlDbType.VarChar, 40);
                sqlParam[22].Direction = ParameterDirection.Output;

                sqlParam[23] = new SqlParameter("@pcCitizenship", SqlDbType.VarChar, 3);
                sqlParam[23].Direction = ParameterDirection.Output;

                sqlParam[24] = new SqlParameter("@pcRiskProfileCode", SqlDbType.Char, 1);
                sqlParam[24].Direction = ParameterDirection.Output;

                sqlParam[25] = new SqlParameter("@pcSpouse", SqlDbType.VarChar, 20);
                sqlParam[25].Direction = ParameterDirection.Output;

                sqlParam[26] = new SqlParameter("@pdExpiredID", SqlDbType.VarChar, 10);
                sqlParam[26].Direction = ParameterDirection.Output;

                sqlParam[27] = new SqlParameter("@pcKITASNo", SqlDbType.VarChar, 40);
                sqlParam[27].Direction = ParameterDirection.Output;

                sqlParam[28] = new SqlParameter("@pcKITASExpDate", SqlDbType.VarChar, 10);
                sqlParam[28].Direction = ParameterDirection.Output;

                sqlParam[29] = new SqlParameter("@pcKITASLastUpdDate", SqlDbType.VarChar, 10);
                sqlParam[29].Direction = ParameterDirection.Output;

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, queryString, ref sqlParam, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                GetDataCoreRs dataCoreRs = new GetDataCoreRs
                {
                    SecAccNo = sqlParam[1].Value.ToString(),
                    Nama = sqlParam[2].Value.ToString(),
                    BranchCode = sqlParam[3].Value.ToString(),
                    JenisIdentitas = sqlParam[4].Value.ToString(),
                    NoIdentitas = sqlParam[5].Value.ToString(),
                    TempatLahir = sqlParam[6].Value.ToString(),
                    TanggalLahir = sqlParam[7].Value.ToString() == "" ? DateTime.MinValue : DateTime.ParseExact(sqlParam[7].Value.ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                    JenisKelamin = sqlParam[8].Value.ToString(),
                    JenisPekerjaan = sqlParam[9].Value.ToString(),
                    AlamatIdentitas1 = sqlParam[10].Value.ToString(),
                    AlamatIdentitas2 = sqlParam[11].Value.ToString(),
                    AlamatIdentitas3 = sqlParam[12].Value.ToString(),
                    KodeKota = sqlParam[13].Value.ToString(),
                    KodePropinsi = sqlParam[14].Value.ToString(),
                    NoTelp = sqlParam[15].Value.ToString(),
                    RiskProfileExpiredDate = sqlParam[16].Value.ToString() == "" ? DateTime.MinValue : DateTime.ParseExact(sqlParam[16].Value.ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                    NoFax = sqlParam[17].Value.ToString(),
                    Email = sqlParam[18].Value.ToString(),
                    JenisRiskProfile = sqlParam[19].Value.ToString(),
                    LastUpdateRiskProfile = sqlParam[20].Value.ToString() == "" ? DateTime.MinValue : DateTime.ParseExact(sqlParam[20].Value.ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                    Handphone = sqlParam[21].Value.ToString(),
                    NPWP = sqlParam[22].Value.ToString(),
                    Citizenship = sqlParam[23].Value.ToString(),
                    RiskProfileCode = sqlParam[24].Value.ToString(),
                    Spouse = sqlParam[25].Value.ToString(),
                    IdentitasExpiredDate = sqlParam[26].Value.ToString() == "" ? DateTime.MinValue : DateTime.ParseExact(sqlParam[26].Value.ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                    KITASNo = sqlParam[27].Value.ToString(),
                    KITASExpDate = sqlParam[28].Value.ToString(),
                    KITASLastUpdateDate = sqlParam[29].Value.ToString()
                };

                msgResponse.IsSuccess = true;
                msgResponse.Data = dataCoreRs;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.Data = null;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }
        public ApiMessage<List<ChannelBondsRs>> GetChannelBonds(ApiMessage<ChannelBondsRq> paramIn)
        {
            ApiMessage<List<ChannelBondsRs>> msgResponse = new ApiMessage<List<ChannelBondsRs>>();
            msgResponse.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                #region Query Inquiry Channel Bonds
                var queryString = @"
                    SELECT 
		                ChannelCode
		                ,ChannelDesc
		                ,isUseECPrice
		                ,isECSettingProduct
		                ,isAutoApprovedNasabah
		                ,isAutoApprovedTransaksi
	                FROM dbo.TRSListChannelBonds_TR";

                if (!String.IsNullOrEmpty(paramIn.Data.ChannelCode))
                {
                    queryString += @" WHERE ChannelCode = '" + paramIn.Data.ChannelCode + "'";
                }
                #endregion

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, queryString, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                List<ChannelBondsRs> inqRes = new List<ChannelBondsRs>();
                inqRes = JsonConvert.DeserializeObject<List<ChannelBondsRs>>(JsonConvert.SerializeObject(dsOut.Tables[0]));

                msgResponse.IsSuccess = true;
                msgResponse.Data = inqRes;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.Data = null;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }

        public ApiMessage<string> ApprovalTreasuryCustomer(ApiMessage<string> paramIn)
        {
            throw new NotImplementedException();
        }

        public ApiMessage<InquiryTreasuryCustomerMxRs> InquiryTreasuryCustomerMx(ApiMessage<string> paramIn)
        {
            ApiMessage<InquiryTreasuryCustomerMxRs> inqTrsCustMxRs = new ApiMessage<InquiryTreasuryCustomerMxRs>();
            inqTrsCustMxRs.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                #region Based on sp TRSPopulateMasterNasabahForMurex
                string sqCmd = @"
                    DECLARE 
                        @newID          UNIQUEIDENTIFIER, 
                        @bIsNew         BIT, 
                        @cCIFNo         VARCHAR(19), 
                        @nCIFId         BIGINT, 
                        @nStatus        INT, 
                        @cMxLabel       VARCHAR(16), 
                        @cCleanNama     VARCHAR(50), 
                        @cCFRESD        CHAR(1), 
                        @cBIPEMI        CHAR(3),
	                    @cCFCLAS		varchar(1)

                    SET @newID = NEWID() 

                    SELECT @nCIFId = CIFId, 
                        @cCIFNo = CIFNo, 
                        @nStatus = Status, 
                        @cMxLabel = MxLabel, 
                        @cCleanNama = LTRIM( REPLACE( REPLACE( REPLACE( REPLACE( REPLACE( REPLACE( REPLACE( REPLACE(Nama,' ',''), '''', ''), '""', ''), '" + "`" + @"', ''), ' / ', ''), '.', ''), ',', ''), ' & ', '') ) 
                    FROM TreasuryCustomer_TM
                    WHERE SecAccNo = @pcSecAccNo

                    SELECT @cCFRESD = CFRESD,
                            @cCFCLAS = CFCLAS
                    FROM dbo.CFMAST_v
                    WHERE CFCIF = @cCIFNo

                    SELECT @cBIPEMI = BIPEMI
                    FROM dbo.CFBICD_v
                    WHERE CFCIF = @cCIFNo

                    IF ISNULL(@cMxLabel,'') = ''
                    BEGIN
                        SELECT  @cMxLabel = MxLabel
                        FROM TRSDetailNasabahStructuredProduct_TM
                        WHERE CIFNo = @cCIFNo


                        IF ISNULL(@cMxLabel,'') = ''
                        BEGIN
                            SELECT @cMxLabel = MxLabel
                            FROM dbo.TRSMasterNasabahFLD_TM
                            WHERE CIFNo = @cCIFNo


                            IF ISNULL(@cMxLabel,'') = ''
                            BEGIN
                                SELECT @cMxLabel = m_label
                                FROM table#data#counterp_dbf_v 
                                WHERE cifno = @cCIFNo
                                    AND m_status = '0'


                                IF ISNULL(@cMxLabel,'') = ''
                                BEGIN
                                    SET @cMxLabel =
                                        CASE WHEN LEN(@cCleanNama) >= 10 THEN LEFT(@cCleanNama,10) +RIGHT(@cCIFNo, 5) ELSE @cCleanNama +REPLICATE('0', 10 - ISNULL(LEN(@cCleanNama), 0)) + RIGHT(@cCIFNo, 5) END
                                    END
                            END
                        END
                    END

                    IF @nStatus = 3 AND
                        EXISTS(SELECT TOP 1 1
                                    FROM TreasuryCustomerPending_TM tcptm
                                    WHERE tcptm.CIFId = @nCIFId)
                    BEGIN
                        SET @bIsNew = 0


                        SELECT
                            @newID AS[Guid], 
                            @cMxLabel AS mxLabel, 
                            CAST(tcp.CIFNo AS DECIMAL(19, 0)) AS CUS1,
                           REPLACE(tcp.Nama, '&', '&amp;') AS CUN,
                           @cBIPEMI AS CTP1,
                          tcp.AlamatIdentitas, 
                            k.Nama AS NA5, 
                            ISNULL(tcp.NoTelp, tcp.NoHP) AS PHN,
                            tcp.NoFax AS FAX, 
                            tcp.NPWP, 
                            'ID' AS COUNTRY,
                            tcp.Email, 
                            '0' AS RELT,
		                    (case when isnull(@cCFCLAS, '') = 'B' then '1' else '0' end) as CORP,
                            'Customer' AS CTP,
                            CASE WHEN @cCFRESD = 'Y'
                                    THEN 'RS'
                                ELSE 'NRS'
                            END AS RES, 
                            '' AS RMCOD,
                            '' AS SLBHU,
                            ISNULL(tcp.Citizenship, '') AS CITIZEN
                            , tcp.CIFId
                         FROM dbo.TreasuryCustomerPending_TM tcp
                            LEFT JOIN KodeKota_TR k
                                ON tcp.KodeKota = k.KodeKota
                        WHERE tcp.CIFId = @nCIFId
                    END
                    ELSE
                    BEGIN
                        SET @bIsNew = 1


                        SELECT
                            @newID AS[Guid], 
                            @cMxLabel AS mxLabel, 
                            CAST(tctm.CIFNo AS BIGINT) AS CUS1,
                           REPLACE(tctm.Nama, '&', '&amp;') AS CUN,
                           @cBIPEMI AS CTP1,
                           tctm.AlamatIdentitas, 
                            k.Nama AS NA5, 
                            ISNULL(tctm.NoTelp, tctm.NoHP) AS PHN,
                            tctm.NoFax AS FAX, 
                            tctm.NPWP, 
                            'ID' AS COUNTRY,
                            tctm.Email, 
                            '0' AS RELT,
                            '0' AS CORP,
                            'Customer' AS CTP,
                            CASE WHEN @cCFRESD = 'Y'
                                    THEN 'RS'
                                ELSE 'NRS'
                            END AS RES, 
                            '' AS RMCOD,
                            '' AS SLBHU,
                            ISNULL(tctm.Citizenship, '') AS CITIZEN
                            , tctm.CIFId
                         FROM dbo.TreasuryCustomer_TM tctm
                            LEFT JOIN KodeKota_TR k
                                ON tctm.KodeKota = k.KodeKota
                        WHERE tctm.CIFId = @nCIFId
                    END

                    BEGIN TRY
                        BEGIN TRAN
                            UPDATE TreasuryCustomer_TM
                            SET[Guid] = @newID
                            WHERE CIFId = @nCIFId


                            IF @bIsNew = 1
                            BEGIN
                                UPDATE TreasuryCustomerPending_TM
                                set[Guid] = @newID
                                WHERE CIFId = @nCIFId
                            END
                        COMMIT TRAN
                    END TRY
                    BEGIN CATCH
                        DECLARE
                            @cErrorMessage VARCHAR(2048), 
                            @nErrorNumber INT

                        IF(@@TRANCOUNT > 0)

                            ROLLBACK TRAN

                        SET @cErrorMessage = ISNULL(ERROR_MESSAGE(), 'TRSPopulateMasterNasabahForMurex - Unknown Error')

                        RAISERROR(@cErrorMessage, 16, 1)
                    END CATCH";
                #endregion

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcSecAccNo", paramIn.Data));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqCmd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                if (dsOut.Tables.Count < 1)
                    throw new Exception("Customer Mx not found");

                if (dsOut.Tables[0].Rows.Count == 0)
                    throw new Exception("Customer Mx not found");

                List<InquiryTreasuryCustomerMxRs> inqCust = JsonConvert.DeserializeObject<List<InquiryTreasuryCustomerMxRs>>(JsonConvert.SerializeObject(dsOut.Tables[0]));

                inqTrsCustMxRs.IsSuccess = true;
                inqTrsCustMxRs.Data = inqCust.FirstOrDefault();
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                inqTrsCustMxRs.IsSuccess = false;
                inqTrsCustMxRs.Data = null;
                inqTrsCustMxRs.ErrorCode = "500";
                inqTrsCustMxRs.ErrorDescription = ex.Message;
            }
            finally
            {
                inqTrsCustMxRs.MessageDateTime = DateTime.Now;
            }

            return inqTrsCustMxRs;
        }

        public ApiMessage<List<TreasuryParameter>> GetTreasuryParameter(ApiMessage<TreasuryParameter> paramIn)
        {
            ApiMessage<List<TreasuryParameter>> TrsParamRes = new ApiMessage<List<TreasuryParameter>>();
            TrsParamRes.copyHeaderForReply(paramIn);

            List<TreasuryParameter> param = new List<TreasuryParameter>();
            string strErrMsg = "";
            try
            {
                DataSet dsOut = new DataSet();
                #region Query Inquiry Trs Customer
                var queryString = @"
                    SELECT Code, Value, [Description]
                    FROM dbo.TRSParameter_TR 
                    WHERE (ISNULL(@pcCode, '') = '' OR Code = @pcCode)
	                    AND (ISNULL(@pcValue, '') = '' OR Value = @pcValue)
	                    AND (ISNULL(@pcDescription, '') = '' OR [Description] = @pcDescription)";
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[3];
                sqlParam[0] = new SqlParameter("@pcCode", paramIn.Data.Code);
                sqlParam[1] = new SqlParameter("@pcValue", paramIn.Data.Value);
                sqlParam[2] = new SqlParameter("@pcDescription", paramIn.Data.Description);

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, queryString, ref sqlParam, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                param = JsonConvert.DeserializeObject<List<TreasuryParameter>>(JsonConvert.SerializeObject(dsOut.Tables[0]));

                TrsParamRes.IsSuccess = true;
                TrsParamRes.Data = param;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                TrsParamRes.IsSuccess = false;
                TrsParamRes.Data = null;
                TrsParamRes.ErrorCode = "500";
                TrsParamRes.ErrorDescription = ex.Message;
            }
            finally
            {
                TrsParamRes.MessageDateTime = DateTime.Now;
            }

            return TrsParamRes;
        }

        public ApiMessage SendCustomerToMurex(ApiMessage<InterfaceCustomerToMurexRq> paramIn)
        {
            ApiMessage sendToMxRs = new ApiMessage();
            sendToMxRs.copyHeaderForReply(paramIn);

            StringBuilder xmlSend = new StringBuilder();
            string strErrMsg = "";

            SOAPWSClient soapWSClient;
            HttpResponseMessage httpResponseMessage;
            String strServiceName;
            Dictionary<String, String> dctHttpHeader;
            StringBuilder sbXMLBodyRq = new StringBuilder();
            String strXMLRs;
            XDocument xml;
            bool isSuccess = false;

            DataTable dtTempInternal = new DataTable();
            dtTempInternal.Columns.Add("guid");
            dtTempInternal.Columns.Add("ACTION");
            dtTempInternal.Columns.Add("MxLabel");
            dtTempInternal.Columns.Add("CUS1");
            dtTempInternal.Columns.Add("CUN");
            dtTempInternal.Columns.Add("CTP1");
            dtTempInternal.Columns.Add("NA2");
            dtTempInternal.Columns.Add("NA3");
            dtTempInternal.Columns.Add("NA4");
            dtTempInternal.Columns.Add("NA5");
            dtTempInternal.Columns.Add("PHN");
            dtTempInternal.Columns.Add("FAX");
            dtTempInternal.Columns.Add("NPWP");
            dtTempInternal.Columns.Add("COUNTRY");
            dtTempInternal.Columns.Add("EMAIL");
            dtTempInternal.Columns.Add("RELT");
            dtTempInternal.Columns.Add("CORP");
            dtTempInternal.Columns.Add("CTP");
            dtTempInternal.Columns.Add("RES");
            dtTempInternal.Columns.Add("RMCOD");
            dtTempInternal.Columns.Add("SLBHU");
            dtTempInternal.Columns.Add("CITIZEN");
            dtTempInternal.Columns.Add("Type");
            List<string>  alamat = new List<string>();
            string N2, N3, N4;
            DataRow drInternal;

            try
            {
                if (paramIn.Data.DataCustomer.AlamatIdentitas.Contains('&'))
                    alamat = paramIn.Data.DataCustomer.AlamatIdentitas.Split('&').ToList();
                else if (paramIn.Data.DataCustomer.AlamatIdentitas.Contains(','))
                    alamat = paramIn.Data.DataCustomer.AlamatIdentitas.Split(',').ToList();
                else
                    alamat.Add(paramIn.Data.DataCustomer.AlamatIdentitas);

                N2 = alamat.Count > 0 ? alamat[0] : "";
                N3 = alamat.Count > 1 ? alamat[1] : "";
                N4 = alamat.Count > 2 ? alamat[2] : "";

                if (alamat.Count > 3)
                    N4 = N4 + ", " + string.Join(", ", alamat.Skip(3).Take(alamat.Count));

                drInternal = dtTempInternal.NewRow();
                drInternal[0] = paramIn.Data.DataCustomer.Guid;
                drInternal[1] = paramIn.Data.ActionType;
                drInternal[2] = paramIn.Data.DataCustomer.mxLabel;
                drInternal[3] = paramIn.Data.DataCustomer.CUS1;
                drInternal[4] = paramIn.Data.DataCustomer.CUN;
                drInternal[5] = paramIn.Data.DataCustomer.CTP1;
                drInternal[6] = N2;
                drInternal[7] = N3;
                drInternal[8] = N4;
                drInternal[9] = paramIn.Data.DataCustomer.NA5;
                drInternal[10] = paramIn.Data.DataCustomer.PHN;
                drInternal[11] = paramIn.Data.DataCustomer.FAX;
                drInternal[12] = paramIn.Data.DataCustomer.NPWP;
                drInternal[13] = paramIn.Data.DataCustomer.COUNTRY;
                drInternal[14] = paramIn.Data.DataCustomer.Email;
                drInternal[15] = paramIn.Data.DataCustomer.RELT;
                drInternal[16] = paramIn.Data.DataCustomer.CORP;
                drInternal[17] = paramIn.Data.DataCustomer.CTP;
                drInternal[18] = paramIn.Data.DataCustomer.RES;
                drInternal[19] = paramIn.Data.DataCustomer.RMCOD;
                drInternal[20] = paramIn.Data.DataCustomer.SLBHU;
                drInternal[21] = paramIn.Data.DataCustomer.CITIZEN;
                drInternal[22] = paramIn.Data.ProductType;

                dtTempInternal.Rows.Add(drInternal);

                string xmlInternal = clsUtils.GetXMLCTP(dtTempInternal);
                DataSet dsOut = new DataSet();

                clsUtils.ServiceHeaderMBASE3(ref xmlSend, paramIn.Data.UserNik, paramIn.Data.Guid);
                xmlSend.Replace("[SERVICENAME]", "OBL_Add_FLDCTP");
                xmlSend.Replace("[OPERATIONCODE]", "000");
                xmlSend.Replace("[REQUESTDETAILFIELD]", "<ns1:CIF>" + xmlInternal.Replace("<", "&lt;") + "</ns1:CIF>");
                xmlSend.Replace("[MOREINDICATOR]", "N");
                strServiceName = "http://tempuri.org/OBLAddFLDCTP";

                dctHttpHeader = null;

                sbXMLBodyRq.Append("<tem:OBLAddFLDCTP  xmlns:tem=\"http://tempuri.org/\">");  //xmlns:tem=\"http://tempuri.org/\" ambil dari header
                sbXMLBodyRq.Append("<tem:GUID>" + paramIn.Data.Guid + "</tem:GUID>");
                sbXMLBodyRq.Append("<tem:Data>" + clsUtils.EncodeXML(xmlSend.ToString()) + "</tem:Data>");
                sbXMLBodyRq.Append("<tem:TellerId>" + paramIn.Data.UserNik + "</tem:TellerId>");
                sbXMLBodyRq.Append("</tem:OBLAddFLDCTP>");

                soapWSClient = new SOAPWSClient(this._ignoreSSL);
                httpResponseMessage = soapWSClient.callService(SOAPWSClient.RequestBodyType.DEFAULT_SOAP_BODY, this._strUrlWsObli, strServiceName, null, dctHttpHeader, sbXMLBodyRq);

                strXMLRs = httpResponseMessage.Content.ReadAsStringAsync().Result;

                if (String.IsNullOrEmpty(strXMLRs))
                    throw new Exception("Failed interface data customer to murex");

                xml = XDocument.Parse(strXMLRs);
                bool.TryParse(XMLHelper.getXMLValue(xml, "OBLAddFLDCTPResult", 0), out isSuccess);
                strErrMsg = XMLHelper.getXMLValue(xml, "RejectDesc", 0);

                sendToMxRs.IsSuccess = isSuccess;
                sendToMxRs.ErrorDescription = strErrMsg;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                sendToMxRs.IsSuccess = false;
                sendToMxRs.ErrorCode = "500";
                sendToMxRs.ErrorDescription = ex.Message;
            }
            finally
            {
                sendToMxRs.MessageDateTime = DateTime.Now;
            }

            return sendToMxRs;
        }

        public ApiMessage<GetParamEmailRs> GetParamEmail(ApiMessage<GetParamEmailRq> paramIn)
        {
            ApiMessage<GetParamEmailRs> paramEmailRs = new ApiMessage<GetParamEmailRs>();
            paramEmailRs.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                string sqlCmd = @"
                Declare @nEmailId int    
  
                select @nEmailId = EmailId from dbo.TRSParameterEmail_TM  
                where EmailName = @pcEmailName  
  
                select * from dbo.TRSParameterEmail_TM where EmailId = @nEmailId  
  
                select * from dbo.TRSParameterEmailReceipt_TM where EmailId = @nEmailId";

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcEmailName", paramIn.Data.EmailParamName));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count < 1)
                    throw new Exception("Param Email Empty");

                EmailParameter paramEmail = JsonConvert.DeserializeObject<List<EmailParameter>>(JsonConvert.SerializeObject(dsOut.Tables[0])).FirstOrDefault();

                if (dsOut.Tables.Count < 2 || dsOut.Tables[1].Rows.Count < 1)
                    throw new Exception("Cannot Found Param Email Recipients");

                List<EmailNotifrecipients> ListEmailRecipients = JsonConvert.DeserializeObject<List<EmailNotifrecipients>>(JsonConvert.SerializeObject(dsOut.Tables[1]));

                GetParamEmailRs emailParamData = new GetParamEmailRs
                {
                    EmailParameter = paramEmail,
                    EmailReceipt = ListEmailRecipients
                };

                paramEmailRs.IsSuccess = true;
                paramEmailRs.Data = emailParamData;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                paramEmailRs.IsSuccess = false;
                paramEmailRs.ErrorCode = "500";
                paramEmailRs.ErrorDescription = ex.Message;
            }
            finally
            {
                paramEmailRs.MessageDateTime = DateTime.Now;
            }

            return paramEmailRs;
        }

        public ApiMessage SendEmail(ApiMessage<SendEmailRq> paramIn)
        {
            ApiMessage sendEmailRs = new ApiMessage();
            sendEmailRs.copyHeaderForReply(paramIn);

            StringBuilder xmlSend = new StringBuilder();
            SOAPWSClient soapWSClient;
            HttpResponseMessage httpResponseMessage;
            String strServiceName;
            Dictionary<String, String> dctHttpHeader;
            StringBuilder sbXMLBodyRq = new StringBuilder();
            String strXMLRs;
            XDocument xml;
            OneNotifTriggerCall triggerOneNotif = new OneNotifTriggerCall(this._clientKey, this._clientSecret, this._nTimeOut * 1000);
            bool isSuccess = false;

            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            try
            {
                #region Using WsWEBI
                /*
                strServiceName = "http://tempuri.org/SendEmailWithoutAttachment";

                dctHttpHeader = null;

                paramIn.Data.Body = paramIn.Data.Body.Replace("<br>", System.Environment.NewLine);

                sbXMLBodyRq.Append("<tem:SendEmailWithoutAttachment  xmlns:tem=\"http://tempuri.org/\">");  //xmlns:tem=\"http://tempuri.org/\" ambil dari header
                sbXMLBodyRq.Append("<tem:GUID>" + paramIn.Data.Guid + "</tem:GUID>");
                sbXMLBodyRq.Append("<tem:TellerId>" + paramIn.Data.TellerId + "</tem:TellerId>");
                sbXMLBodyRq.Append("<tem:MailSubject>" + paramIn.Data.Subject + "</tem:MailSubject>");
                sbXMLBodyRq.Append("<tem:MailBody>" + paramIn.Data.Body + "</tem:MailBody>");
                sbXMLBodyRq.Append("<tem:SenderAddress>" + paramIn.Data.SenderAddress + "</tem:SenderAddress>");
                sbXMLBodyRq.Append("<tem:DummyEmail>" + paramIn.Data.dummy + "</tem:DummyEmail>");
                sbXMLBodyRq.Append("<tem:ToAddress>" + paramIn.Data.Recipient + "</tem:ToAddress>");
                sbXMLBodyRq.Append("</tem:SendEmailWithoutAttachment>");

                soapWSClient = new SOAPWSClient(this._ignoreSSL);
                httpResponseMessage = soapWSClient.callService(SOAPWSClient.RequestBodyType.DEFAULT_SOAP_BODY, this._strUrlWsWEBI, strServiceName, null, dctHttpHeader, sbXMLBodyRq);

                strXMLRs = httpResponseMessage.Content.ReadAsStringAsync().Result;

                if (String.IsNullOrEmpty(strXMLRs))
                    throw new Exception("Failed interface data customer to murex");

                xml = XDocument.Parse(strXMLRs);
                bool.TryParse(XMLHelper.getXMLValue(xml, "OBLAddFLDCTPResult", 0), out isSuccess);
                strErrMsg = XMLHelper.getXMLValue(xml, "RejectDesc", 0);
                */
                #endregion

                #region Using SP
                /*
                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                if (!string.IsNullOrEmpty(paramIn.Data.Recipient))
                {
                    string strSPName = "SendEmailWithoutAttachment";
                    dbPar.Add(new SQLSPParameter("@pcMailSubject", paramIn.Data.Subject));
                    dbPar.Add(new SQLSPParameter("@pcMailBody", paramIn.Data.Body));
                    dbPar.Add(new SQLSPParameter("@pcSenderAddress", paramIn.Data.SenderAddress));
                    dbPar.Add(new SQLSPParameter("@pcDummyEmail", paramIn.Data.dummy));
                    dbPar.Add(new SQLSPParameter("@pcToAddress", paramIn.Data.Recipient));
                    dbPar.Add(new SQLSPParameter("@pcBCC", paramIn.Data.SenderAddress));
                    dbPar.Add(new SQLSPParameter("@pcType", "NTI Obligasi"));
                    isSuccess = clsCallSPWs.CallSPFromWs(
                        this._strUrlWsProTeller,
                        this._sqlTellerId,
                        this._sqlMailDb,
                        this._ignoreSSL,
                        strSPName,
                        ref dbPar,
                        out dsOut,
                        out strErrMsg);
                    if (!isSuccess)
                        throw new Exception(strErrMsg);
                }
                */
                #endregion

                #region Using One Notif
                Model.OneNotif.BulkCallRq triggerOneNotifRq = new Model.OneNotif.BulkCallRq();
                triggerOneNotifRq.event_id = this._eventId;

                EmailTropsOneNotifRq oneNotifParams = JsonConvert.DeserializeObject<EmailTropsOneNotifRq>(JsonConvert.SerializeObject(paramIn.Data.AdditionalParams));

                var objParams = new
                {
                    subject = paramIn.Data.Subject,
                    CIF = oneNotifParams.CIF,
                    NAMA = oneNotifParams.NAMA,
                    SECACCNO = oneNotifParams.SECACCNO
                };

                var objRecipient_Params = new List<object>();

                foreach (string str in paramIn.Data.ListRecipients)
                {
                    var tags = new { to = str };
                    objRecipient_Params.Add(tags);
                }

                triggerOneNotifRq.@params = objParams;
                triggerOneNotifRq.recipient_params = objRecipient_Params;

                string bodytoOneNotif = JsonConvert.SerializeObject(triggerOneNotifRq);

                string strDt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
                string signature = SignatureGenerator.SHA512_ComputeHash(this._clientKey, strDt, bodytoOneNotif, this._clientSecret);

                Dictionary<string, string> dictHeader = new Dictionary<string, string>();
                dictHeader["Signature"] = signature;
                dictHeader["Client-Id"] = this._clientKey;
                dictHeader["Created-Time"] = strDt;
                //dictHeader["Content-Type"] = "application/json";

                RestWSClient<OneNotifRs> restAPI = new RestWSClient<OneNotifRs>(this._ignoreSSL);
                OneNotifRs apiRes = restAPI.invokeRESTServicePost(this._oneNotifBulkCall, null, dictHeader, triggerOneNotifRq);

                //ApiMessage triggerRs = triggerOneNotif.TriggerOneNotif(this._oneNotifBulkCall, bodytoOneNotif);

                //if (!triggerRs.IsSuccess)
                //    throw new Exception(triggerRs.ErrorDescription);

                //isSuccess = triggerRs.IsSuccess;

                if (!apiRes.status.Equals("ok"))
                    throw new Exception(apiRes.error);

                isSuccess = apiRes.status.Equals("ok");
                #endregion

                sendEmailRs.IsSuccess = isSuccess;
                sendEmailRs.ErrorDescription = strErrMsg;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                sendEmailRs.IsSuccess = false;
                sendEmailRs.ErrorCode = "500";
                sendEmailRs.ErrorDescription = ex.Message;
            }
            finally
            {
                sendEmailRs.MessageDateTime = DateTime.Now;
            }

            return sendEmailRs;
        }

        public ApiMessage UpdateStatusTreasuryCustomer(ApiMessage<UpdateStatusTreasuryCustRq> paramIn)
        {
            DatabaseConnectorMsSQL conn = new DatabaseConnectorMsSQL(this._strConnStringOBL);
            ApiMessage updateStatusTrsCustRs = new ApiMessage();
            updateStatusTrsCustRs.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                #region Based on SP OMUpdateStatusTreasuryCust
                string sqlCmnd = @"
                    declare    
	                    @cErrMsg    varchar(1000)    
	                    ,@nOK       int    
	                    ,@nErrNo    int    
  
                    begin try    
	                    if(@pcStatus = 'F')    
	                    begin  
		                    insert dbo.TreasuryCustomerLog_TH    
			                    (    
			                    CIFId, SecAccNo, CIFNo, Nama, BranchCode, JenisIdentitas, NoIdentitas, TempatLahir, TanggalLahir    
			                    , JenisKelamin, JenisPekerjaan, AlamatIdentitas, KodeKota, KodePropinsi, NoTelp, NoHP, NoFax, Email, NoRekInvestor    
			                    , AlamatSurat, NIK_CS, NPWP, [Status], InsertedBy, InsertedDate, LastUpdateBy, LastUpdateDate, RiskProfile, LastRiskProfileUpdateDate    
			                    , BankCustodyCode1, BankCustodyCode2, BankCustodyCode3, BankCustodySecurityNo1, BankCustodySecurityNo2, BankCustodySecurityNo3, MetodeKontak        
			                    , OperationCode, ProcessDate, SyaratKetentuanBit, TanggalPengisian, FlagKaryawan, IsSuratUseRumah, AlamatSuratSequence, KodeAlamatSurat    
			                    , KodeCabangAlamatSurat, FuncGroupIDR, SubFuncGroupIDR, FuncGroupNonIDR, SubFuncGroupNonIDR, FlagPremier, NikReferentor, Keterangan    
			                    , RiskProfileExpiredDate, IdentitasExpiredDate, ProcessBy, FuncGroupIDRTA, SubFuncGroupIDRTA    
			                    , FuncGroupNonIDRTA, SubFuncGroupNonIDRTA, Citizenship, RiskProfileCode, [Signature], isOnlineAcc, Summary  
			                    , Spouse, ApprovalNNH, TanggalApprovalNNH, RegViaONEMobile    
			                    )    
		                    select    
			                    CIFId, SecAccNo, CIFNo, Nama, BranchCode, JenisIdentitas, NoIdentitas, TempatLahir, TanggalLahir    
			                    , JenisKelamin, JenisPekerjaan, AlamatIdentitas, KodeKota, KodePropinsi, NoTelp, NoHP, NoFax, Email, NoRekInvestor    
			                    , AlamatSurat, NIK_CS, NPWP, [Status], InsertedBy, InsertedDate, LastUpdateBy, LastUpdateDate, RiskProfile, LastRiskProfileUpdateDate    
			                    , BankCustodyCode1, BankCustodyCode2, BankCustodyCode3, BankCustodySecurityNo1, BankCustodySecurityNo2, BankCustodySecurityNo3, MetodeKontak    
			                    , 'F', getdate(), SyaratKetentuanBit, TanggalPengisian, FlagKaryawan, IsSuratUseRumah, AlamatSuratSequence, KodeAlamatSurat    
			                    , KodeCabangAlamatSurat, FuncGroupIDR, SubFuncGroupIDR, FuncGroupNonIDR, SubFuncGroupNonIDR, FlagPremier, NikReferentor, Keterangan    
			                    , RiskProfileExpiredDate, IdentitasExpiredDate, 77777, FuncGroupIDRTA, SubFuncGroupIDRTA    
			                    , FuncGroupNonIDRTA, SubFuncGroupNonIDRTA, Citizenship, RiskProfileCode, [Signature], isOnlineAcc, Summary  
			                    , Spouse, ApprovalNNH, TanggalApprovalNNH, RegViaONEMobile    
		                    from dbo.TreasuryCustomer_TM    
		                    where CIFId = @pnCIFId  
		
		                    IF EXISTS (SELECT TOP 1 1 FROM dbo.TreasuryCustomer_TM    
					                    WHERE CIFId = @pnCIFId AND Status = 3)
		                    BEGIN
			                    DELETE FROM dbo.TreasuryCustomerPending_TM    
			                    WHERE CIFId = @pnCIFId
		                    END
		                    ELSE
		                    BEGIN		
			                    DELETE FROM dbo.TreasuryCustomer_TM    
			                    WHERE CIFId = @pnCIFId     
		                    END
	                    end  
                    end try    
                    begin catch    
	                    set @cErrMsg = '[OMUpdateStatusTreasuryCust] - ' + error_message()     
	                    goto ERROR    
                    end catch    
                                     
                    ------------------------------------------------------------------------------------------        
                        
                    ERROR:                        
	                    if @@trancount > 0     
		                    rollback tran                        
                            
	                    if @cErrMsg is null  
	                    begin                        
		                    set @cErrMsg = '[OMUpdateStatusTreasuryCust] - unknown error occurred'                        
	                    end  
                            
	                    --exec @nOK = set_raiserror @@ProcId, @nErrNo output  
	                    if @nOK <> 0 or @@error <> 0     
	                    raiserror (@cErrMsg, 16, 1)";
                #endregion  

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcStatus", paramIn.Data.Status));
                dbPar.Add(new SQLSPParameter("@pnCIFId", paramIn.Data.CIFId));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmnd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                updateStatusTrsCustRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                updateStatusTrsCustRs.IsSuccess = false;
                updateStatusTrsCustRs.ErrorCode = "500";
                updateStatusTrsCustRs.ErrorDescription = ex.Message;
            }
            finally
            {
                updateStatusTrsCustRs.MessageDateTime = DateTime.Now;
            }

            return updateStatusTrsCustRs;
        }
        public ApiMessage AddLogInterfaceMx(ApiMessage<InterfaceCustomerToMurexRq> paramIn)
        {
            ApiMessage logInterfaceRs = new ApiMessage();
            logInterfaceRs.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                string xml = clsUtils.SerializeObject(paramIn.Data.DataCustomer);
                #region based on sp MXINTFCPYInLog
                string sqlCmd = @"
                    declare @dNow				datetime

                    set @dNow = getdate()

                    insert dbo.MXInterfaceCPYIn_TM (LogTimeStamp, LogGuid, LogData)
                    select @dNow, @pcGuid, @pcData";
                #endregion

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcGuid", paramIn.Data.Guid));
                dbPar.Add(new SQLSPParameter("@pcData", xml));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                logInterfaceRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                logInterfaceRs.IsSuccess = false;
                logInterfaceRs.ErrorCode = "500";
                logInterfaceRs.ErrorDescription = ex.Message;
            }
            finally
            {
                logInterfaceRs.MessageDateTime = DateTime.Now;
            }

            return logInterfaceRs;
        }

        public ApiMessage<MainCustomerData> GetKodeKotaProv(ApiMessage<string> paramIn)
        {
            ApiMessage<MainCustomerData> getKodeKotaRs = new ApiMessage<MainCustomerData>();
            getKodeKotaRs.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                #region Query Get Kode Kota dan Provinsi
                string sqlCmd = @"
                    DECLARE @cKodeKota    VARCHAR(4)     
                        , @cKodePropinsi   VARCHAR(4)
                    
                    SELECT @cKodeKota = k.KodeKota
                        , @cKodePropinsi = p.KodePropinsi  
                    FROM SQL_TRSRETAIL.dbo.KodeKota_TR k     
                    JOIN SQL_TRSRETAIL.dbo.KodePropinsi_TR p     
                        ON k.KodePropinsi = p.KodePropinsi 
                    WHERE k.Nama = @pcNamaKota

                    IF ISNULL(@cKodeKota,'') = ''     
                    BEGIN         
                        SELECT @cKodeKota = k.KodeKota
                            , @cKodePropinsi = p.KodePropinsi     
                        FROM CFADDRKodeKotaMapping_TR akmap     
                        INNER JOIN KodeKota_TR k     
                            ON akmap.Nama = k.Nama     
                        INNER JOIN KodePropinsi_TR p     
                            ON k.KodePropinsi = p.KodePropinsi     
                        WHERE akmap.CF2DT2 = @pcNamaKota          
                    END 

                    SELECT  @cKodeKota AS KodeKota, @cKodePropinsi AS KodePropinsi";
                #endregion

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcNamaKota", paramIn.Data.Trim().ToUpper()));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                if (dsOut.Tables.Count < 1)
                    throw new Exception("Cannot get Kode Kota and Provinsi");

                MainCustomerData tmpDataRs = new MainCustomerData();
                if (dsOut.Tables[0].Rows.Count > 0)
                {
                    tmpDataRs.KodeKota = dsOut.Tables[0].Rows[0][0].ToString();
                    tmpDataRs.KodePropinsi = dsOut.Tables[0].Rows[0][1].ToString();
                }
                else
                {
                    tmpDataRs.KodeKota = "0";
                    tmpDataRs.KodePropinsi = "0";
                }

                getKodeKotaRs.IsSuccess = true;
                getKodeKotaRs.Data = tmpDataRs;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                getKodeKotaRs.IsSuccess = false;
                getKodeKotaRs.ErrorCode = "500";
                getKodeKotaRs.ErrorDescription = ex.Message;
            }
            finally
            {
                getKodeKotaRs.MessageDateTime = DateTime.Now;
            }

            return getKodeKotaRs;
        }

        public ApiMessage NewToInvestmentLogging(ApiMessage<CreateTreasuryCustomerRq> paramIn)
        {
            ApiMessage NTILoggingRs = new ApiMessage();
            NTILoggingRs.copyHeaderForReply(paramIn);

            bool bResult = false;
            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                string bodyJson = JsonConvert.SerializeObject(paramIn.Data);
                #region Query NTI Logging
                string sqlCmd = @"
                    DECLARE @dNow				DATETIME
	                    , @dtHousekeep			DATETIME

                    SET @dNow = getdate()
                    SET @dtHousekeep = DATEADD(MONTH, -3, DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE())))

                    IF EXISTS(
	                    SELECT TOP 1 1
	                    FROM dbo.[TRSNTITreasuryCust_TL] WITH(NOLOCK)
	                    WHERE TimeInput < @dtHousekeep
	                    )
                    BEGIN
	                    DELETE
	                    FROM dbo.[TRSNTITreasuryCust_TL]
	                    WHERE TimeInput < @dtHousekeep
                    END

                    INSERT dbo.[TRSNTITreasuryCust_TL] ([Action], CIFNo, TimeInput, Channel, TransactionGuid, BodyInput)
                    SELECT @pcAction, @pcCIF, @dNow, @pcChannel, @pcGuid, @pcData";
                #endregion

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcAction", "Create"));
                dbPar.Add(new SQLSPParameter("@pcCIF", paramIn.Data.CustomerData.CIFNo));
                dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.ChannelRegistration));
                dbPar.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));
                dbPar.Add(new SQLSPParameter("@pcData", bodyJson));

                bResult = clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out strErrMsg);
                if (!bResult)
                    throw new Exception(strErrMsg);

                NTILoggingRs.IsSuccess = bResult;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                NTILoggingRs.IsSuccess = false;
                NTILoggingRs.ErrorCode = "500";
                NTILoggingRs.ErrorDescription = ex.Message;
            }
            finally
            {
                NTILoggingRs.MessageDateTime = DateTime.Now;
            }

            return NTILoggingRs;
        }

        public DataPribadiInquiryDetailV2Rs InquiryDataPribadi(DataPribadiInquiryDetailV2Rq paramIn)
        {
            DataPribadiInquiryDetailV2Rs msgResponse = new DataPribadiInquiryDetailV2Rs();
            
            string methodApiUrl = this._urlAPIProCIF + "/api/v2/cif/datapribadi/inquirydetailv2";
            Console.WriteLine(System.DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + "-" + methodApiUrl);

            try
            {
                RestWSClient<DataPribadiInquiryDetailV2Rs> restAPI = new RestWSClient<DataPribadiInquiryDetailV2Rs>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, paramIn);

                if (!msgResponse.isSuccess)
                    throw new Exception(msgResponse.description);

            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), ex.Message, paramIn.GUID);
                msgResponse.isSuccess = false;
                msgResponse.description = ex.Message;
            }

            return msgResponse;
        }

        public AlamatInquiryByCodeRs InquiryAlamat(AlamatInquiryByCodeRq paramIn)
        {
            AlamatInquiryByCodeRs msgResponse = new AlamatInquiryByCodeRs();

            string methodApiUrl = this._urlAPIProCIF + "/api/v2/cif/alamat/inquiryalamatbycode";
            Console.WriteLine(System.DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + "-" + methodApiUrl);

            try
            {
                RestWSClient<AlamatInquiryByCodeRs> restAPI = new RestWSClient<AlamatInquiryByCodeRs>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, paramIn);

                if (!msgResponse.isSuccess)
                    throw new Exception(msgResponse.description);

            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), ex.Message, paramIn.GUID);
                msgResponse.isSuccess = false;
                msgResponse.description = ex.Message;
            }

            return msgResponse;
        }

        public PekerjaanInquiryV2Rs InquiryPekerjaan(PekerjaanInquiryV2Rq paramIn)
        {
            PekerjaanInquiryV2Rs msgResponse = new PekerjaanInquiryV2Rs();

            string methodApiUrl = this._urlAPIProCIF + "/api/v2/cif/pekerjaan/inquiryv2";
            Console.WriteLine(System.DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + "-" + methodApiUrl);

            try
            {
                RestWSClient<PekerjaanInquiryV2Rs> restAPI = new RestWSClient<PekerjaanInquiryV2Rs>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, paramIn);

                if (!msgResponse.isSuccess)
                    throw new Exception(msgResponse.description);

            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), ex.Message, paramIn.GUID);
                msgResponse.isSuccess = false;
                msgResponse.description = ex.Message;
            }

            return msgResponse;
        }
        public ApiMessage<string> GetMappingPekerjaan(ApiMessage<string> paramIn)
        {
            ApiMessage<string> getMappingJobRs = new ApiMessage<string>();
            getMappingJobRs.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                #region Query Get Mapping Pekerjaan
                string sqlCmd = @"
                        DECLARE @cJenisPekerjaan    VARCHAR(5)     
                                            
                        SELECT @cJenisPekerjaan = TRSJob_ID     
                        FROM dbo.TRSMapPekerjaan_TR     
                        WHERE OCCP_ID = @pcJobCode    
    
                        IF @cJenisPekerjaan IS NULL     
                        BEGIN     
                            SELECT @cJenisPekerjaan = TRSJob_ID     
                            FROM TRSMapPekerjaan_TR     
                            WHERE OCCP_ID = 'LAIN'     
                        END     

                        SELECT @cJenisPekerjaan AS JenisPekerjaan";
                #endregion

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcJobCode", paramIn.Data.Trim().ToUpper()));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                if (dsOut.Tables.Count < 1)
                    throw new Exception("Cannot get Mappingan Pekerjaan");

                getMappingJobRs.IsSuccess = true;
                getMappingJobRs.Data = dsOut.Tables[0].Rows[0][0].ToString();
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                getMappingJobRs.IsSuccess = false;
                getMappingJobRs.ErrorCode = "500";
                getMappingJobRs.ErrorDescription = ex.Message;
            }
            finally
            {
                getMappingJobRs.MessageDateTime = DateTime.Now;
            }

            return getMappingJobRs;
        }
        public ApiMessage<GetDataCoreRs> GetParamExpireDate(ApiMessage<string> paramIn)
        {
            ApiMessage<GetDataCoreRs> getParamExpireDate = new ApiMessage<GetDataCoreRs>();
            getParamExpireDate.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                #region Query Get Mapping Pekerjaan
                string sqlCmd = @"
                        DECLARE
	                        @pdExpiredRiskProfile		DATETIME         
	                        ,@pdLastUpdateRiskProfile	DATETIME     
	                        ,@pdExpiredID				DATETIME     
	                        ,@cCIFNo					VARCHAR(19)     
	                        ,@nCIFId					BIGINT     
	                        ,@cYearExpiredRisk			VARCHAR(20)     
	                        ,@nYearExpiredRisk			INT     
    
                        CREATE TABLE #xtmpProCIFChange     
	                        (     
	                        ChangeId  INT,     
	                        UpdateDate  DATETIME     
	                        )     
                        CREATE INDEX ix_#xtmpProCIFChange ON #xtmpProCIFChange ( ChangeId ) INCLUDE ( UpdateDate )     
    
                        SET @cCIFNo = RIGHT( REPLICATE('0', 19) + CAST(@nCIFNo AS VARCHAR), 19)     
     
                        SELECT @cYearExpiredRisk = Value     
                        FROM TRSParameter_TR     
                        WHERE Code = 'ParameterRiskProfile'     
    
                        SET @nYearExpiredRisk =     
	                        CASE WHEN ISNUMERIC(@cYearExpiredRisk) = 1     
	                        THEN CAST(@cYearExpiredRisk AS INT)     
	                        ELSE 3     
	                        END     
    
                        -- Last Update Risk Profile     
                        INSERT #xtmpProCIFChange(
	                        ChangeId
	                        , UpdateDate 
	                        )     
                        SELECT 
	                        ChangeId
	                        , DATEADD(DAY, DATEDIFF(DAY,0,ApprovedDate), 0)     
                        FROM SQL_CIF.dbo.ProCIFChangeMaster_TH WITH (NOLOCK)     
                        WHERE CIFNumber = @nCIFNo     
	                        AND TableName = 'CFMAST'     
	                        AND Status = 2     
    
                        SELECT @pdLastUpdateRiskProfile = MAX(t.UpdateDate)     
                        FROM #xtmpProCIFChange t     
                        INNER JOIN SQL_CIF.dbo.ProCIFChangeDetail_TH d WITH (NOLOCK)     
	                        ON t.ChangeId = d.ChangeId     
	                        AND d.ColumnName = 'CFUIC8'     
    
                        SET @pdLastUpdateRiskProfile = ISNULL(@pdLastUpdateRiskProfile, GETDATE())
  
                        SET @pdExpiredRiskProfile = DATEADD ( YEAR, @nYearExpiredRisk, ISNULL(@pdLastUpdateRiskProfile, GETDATE()) )     

                        SELECT @pdLastUpdateRiskProfile = LastUpdated, @pdExpiredRiskProfile = ExpiredDate    
                        FROM SQL_CIF.dbo.ProCIFRiskProfile_TM    
                        WHERE CIFNumber = @nCIFNo  
    
                        DROP TABLE #xtmpProCIFChange

                        SELECT @pdExpiredRiskProfile AS ExpiredRiskProfile, @pdLastUpdateRiskProfile AS LastUpdateRiskProfile";
                #endregion

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@nCIFNo", paramIn.Data.Trim().ToUpper()));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                if (dsOut.Tables.Count < 1)
                    throw new Exception("Cannot get Param Expire Date");

                GetDataCoreRs dataExpireDate = new GetDataCoreRs();
                dataExpireDate.RiskProfileExpiredDate = DateTime.Parse(dsOut.Tables[0].Rows[0]["ExpiredRiskProfile"].ToString());
                dataExpireDate.LastUpdateRiskProfile = DateTime.Parse(dsOut.Tables[0].Rows[0]["LastUpdateRiskProfile"].ToString());

                getParamExpireDate.IsSuccess = true;
                getParamExpireDate.Data = dataExpireDate;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                getParamExpireDate.IsSuccess = false;
                getParamExpireDate.ErrorCode = "500";
                getParamExpireDate.ErrorDescription = ex.Message;
            }
            finally
            {
                getParamExpireDate.MessageDateTime = DateTime.Now;
            }

            return getParamExpireDate;
        }

        public ApiMessage<bool> ValidasiRekeningNonIDR(ApiMessage<List<CustomerAccNumber>> paramIn)
        {
            ApiMessage<bool> validasiRs = new ApiMessage<bool>();
            validasiRs.copyHeaderForReply(paramIn);
            validasiRs.Data = false;
            
            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            try
            {
                string xml = clsUtils.SerializeObject(paramIn.Data);
                #region Query Validasi Rekening Non IDR
                string sqlCmd = @"
                    DECLARE @message					VARCHAR(MAX)                          
	                    , @CheckXml					INT          
	                    , @nOK						INT                          

                    SET @pbIsValid = CAST(1 AS BIT)
                    SET @pcValidationMsg = ''

                    IF OBJECT_ID('tempdb..#tempRekening') IS NOT NULL 
	                    DROP TABLE #tempRekening

                    CREATE TABLE #tempRekening                          
	                    (                          
		                    ACCTNO                          VARCHAR(20   )      NOT NULL  ,                          
		                    STATUS_JOIN                     VARCHAR(1    )      NOT NULL  ,                          
		                    CIF_PRIMARY_OWNER_JOIN          VARCHAR(19   )      NULL      ,                          
		                    NAMA_PRIMARY_OWNER_JOIN         VARCHAR(95   )      NULL      ,                          
		                    CCY                             VARCHAR(4    )      NOT NULL  ,                          
		                    RELASI                          VARCHAR(2    )      NULL      ,                          
		                    PRODUCT_CODE                    VARCHAR(10   )      NOT NULL                            
	                    )

                    EXEC @nOK = sp_xml_preparedocument @CheckXml OUTPUT, @pcXmlInput                          
                    IF @nOK!=0 OR @@error!=0                          
                    BEGIN                          
	                    SET @message='Gagal di sp_xml_preparedocument'                          
	                    GOTO ERR_HANDLER                          
                    END

                    INSERT INTO #tempRekening                          
	                    (
		                    ACCTNO
		                    , STATUS_JOIN
		                    , CIF_PRIMARY_OWNER_JOIN
		                    , NAMA_PRIMARY_OWNER_JOIN
		                    , CCY
		                    , RELASI
		                    , PRODUCT_CODE
	                    )                         
                    SELECT ACCTNO
	                    , STATUS_JOIN
	                    , CIF_PRIMARY_OWNER_JOIN
	                    , NAMA_PRIMARY_OWNER_JOIN
	                    , CCY
	                    , RELASI
	                    , PRODUCT_CODE                          
                    FROM OPENXML(@CheckXml, '/ArrayOfCustomerAccNumber/CustomerAccNumber', 2)                          
                    WITH #tempRekening

                    --- Cek apakah produk taka?
                    IF EXISTS(
		                    SELECT TOP 1 1
		                    FROM #tempRekening ta  
		                    INNER JOIN DDPAR2_v dp 
			                    ON ta.PRODUCT_CODE = dp.SCCODE 
				                    AND ta.CCY = dp.DP2CUR 
		                    WHERE dp.DY2INS = 'Y'	
		                    )
                    BEGIN
	                    SET @pbIsValid = CAST(0 AS BIT)
	                    SET @pcValidationMsg = 'Rekening TAKA tidak dapat digunakan sebagai sumber dana'
                    END

                    --- Cek apakah rekening Tax Amnesty?
                    IF EXISTS(
		                    SELECT TOP 1 1
		                    FROM #tempRekening ta     
		                    INNER JOIN dbo.TAMAST_v tms 
			                    ON ta.ACCTNO = tms.[CFACC#] 
		                    WHERE tms.TAFLAG = 7	
		                    )
                    BEGIN
	                    SET @pbIsValid = CAST(0 AS BIT)
	                    SET @pcValidationMsg = 'Rekening Tax Amnesty tidak dapat digunakan sebagai sumber dana'
                    END

                    ERR_HANDLER: 
	                    IF @message is NOT NULL 
	                    BEGIN
		                    IF @@Trancount > 0 
			                    ROLLBACK TRAN 

		                    RAISERROR (@message, 16, 1)
	                    END
                       ";
                #endregion

                List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcXmlInput", xml));
                dbPar.Add(new SQLSPParameter("@pbIsValid", null, SQLSPParameter.ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcValidationMsg", null, SQLSPParameter.ParamDirection.OUTPUT));

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                validasiRs.Data = dbPar[1].ParameterValue.ToString().Equals("1");

                if (!validasiRs.Data)
                    validasiRs.ErrorDescription = dbPar[2].ParameterValue.ToString();
            }
            catch(Exception ex)
            {
                this._logger.logError(this, new StackTrace(), "Request => " + JsonConvert.SerializeObject(paramIn) + "; Error => ", ex, paramIn.TransactionMessageGUID);
                validasiRs.IsSuccess = false;
                validasiRs.ErrorCode = "500";
                validasiRs.ErrorDescription = ex.Message;
            }
            finally
            {
                validasiRs.MessageDateTime = DateTime.Now;
            }

            return validasiRs;
        }
    }
}
