using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NISPDataSourceNetCore.webservice.model;
using Treasury.Scheduler.ONFX.Models;
using Treasury.Scheduler.ONFX.Utilities;

namespace Treasury.Scheduler.ONFX.Services
{
    public class clsAntasenaService : IAntasenaService
    {
        private IConfiguration _configuration;
        private string _strWsOmniObli;
        private bool _ignoreSSL;
        private string _strEncConnStringSibs;
        private string _strEncConnStringObl;
        private string _strConnStringSFX;
        private string _strUrlAPIDealKurs;
        private clsAPIHelper _apihelper;
        private clsMsSQLHelper _msHelper;
        private ConnectionProperties _connObl;
        private GlobalVariableList _globalVar;

        public clsAntasenaService(IConfiguration iConfig, GlobalVariableList globalVariable)
        {
            this._configuration = iConfig;
            this._strWsOmniObli = globalVariable.UrlWsOmniObli;
            this._ignoreSSL = globalVariable.ignoreSSL;
            this._strEncConnStringObl = globalVariable.EncConnectionStringObl;
            this._strEncConnStringSibs = globalVariable.EncConnectionStringSibs;
            this._strConnStringSFX = globalVariable.ConnectionStringSFx;
            this._strUrlAPIDealKurs = globalVariable.UrlAPIDealKurs;
            this._apihelper = new clsAPIHelper();
            this._msHelper = new clsMsSQLHelper(globalVariable.UrlWsOmniObli);
            this._connObl = globalVariable.ConnectionObli;
            this._globalVar = globalVariable;
        }

        public async Task<ApiMessage> InsertForm42ByTran(ApiMessage<List<Form042Underlying>> trans)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(trans);

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsRate = new DataSet();
                DataTable dtRate = new DataTable();

                dtRate = this._msHelper.MapListToTable<Form042Underlying>(trans.Data);

                dsRate.Tables.Add(dtRate);
                dsRate.DataSetName = "Data";
                dsRate.Tables[0].TableName = "Trans";

                string strXml = dsRate.GetXml();

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpTrans') IS NOT NULL
	                    DROP TABLE #tmpTrans

                    CREATE TABLE #tmpTrans                     
                    (    
	                    IdData					BIGINT
	                    , src					VARCHAR(10)
	                    , dealno				VARCHAR(20)
	                    , customer_id			VARCHAR(20)
	                    , acc_id				VARCHAR(20)
	                    , trx_branch			VARCHAR(5)
	                    , trx_date				DATETIME
	                    , currency_code			VARCHAR(5)
	                    , amount				MONEY
	                    , rate					FLOAT
	                    , InUSD					MONEY
	                    , SourceKey				VARCHAR(20)
	                    , trx_datetime			DATETIME
	                    , isHit					BIT
	                    , purpose				VARCHAR(15)
	                    , underlying			VARCHAR(15)
	                    , KeteranganUnderlying	VARCHAR(150)
	                    , CreatedDate			DATETIME
	                    , JISDORRate			FLOAT
	                    , InUSDJISDOR			MONEY
	                    , isHitJISDOR			BIT   
						--darul
						, lcs_flag				BIT
						, buy_sell_code			CHAR(1)
						, product				VARCHAR(50)
						--darul
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpTrans
                    (
	                    IdData					
	                    , src					
	                    , dealno				
	                    , customer_id			
	                    , acc_id				
	                    , trx_branch			
	                    , trx_date				
	                    , currency_code			
	                    , amount				
	                    , rate					
	                    , InUSD					
	                    , SourceKey				
	                    , trx_datetime			
	                    , isHit					
	                    , purpose				
	                    , underlying			
	                    , KeteranganUnderlying	
	                    , CreatedDate			
	                    , JISDORRate			
	                    , InUSDJISDOR			
	                    , isHitJISDOR
						--darul	
						, lcs_flag				
						, buy_sell_code			
						, product		
						--darul
                    )
                    SELECT
	                    IdData					
	                    , src					
	                    , dealno				
	                    , customer_id			
	                    , acc_id				
	                    , trx_branch			
	                    , strTrx_date				
	                    , currency_code			
	                    , amount				
	                    , rate					
	                    , InUSD					
	                    , SourceKey				
	                    , strTrx_datetime			
	                    , isHit					
	                    , purpose				
	                    , underlying			
	                    , KeteranganUnderlying	
	                    , strCreatedDate			
	                    , JISDORRate			
	                    , InUSDJISDOR			
	                    , isHitJISDOR	
						--darul
						, FlagLCS				
						, BuySellCode			
						, Product			
						--darul
                    FROM openxml(@nDocHandle, N'/Data/Trans',2)           
                    WITH (
	                    IdData					BIGINT
	                    , src					VARCHAR(10)
	                    , dealno				VARCHAR(20)
	                    , customer_id			VARCHAR(20)
	                    , acc_id				VARCHAR(20)
	                    , trx_branch			VARCHAR(5)
	                    , strTrx_date			VARCHAR(25)
	                    , currency_code			VARCHAR(5)
	                    , amount				MONEY
	                    , rate					FLOAT
	                    , InUSD					MONEY
	                    , SourceKey				VARCHAR(20)
	                    , strTrx_datetime		VARCHAR(25)
	                    , isHit					BIT
	                    , purpose				VARCHAR(15)
	                    , underlying			VARCHAR(15)
	                    , KeteranganUnderlying	VARCHAR(150)
	                    , strCreatedDate		VARCHAR(15)
	                    , JISDORRate			FLOAT
	                    , InUSDJISDOR			MONEY
	                    , isHitJISDOR			BIT     
						--darul
						, FlagLCS				BIT
						, BuySellCode			CHAR(1)
						, Product				VARCHAR(50)
						--darul
                    )  
					
					DELETE #tmpTrans
                    WHERE amount < 0

                    DELETE eq
                    FROM dbo.[[paramTableName]] eq
                    JOIN #tmpTrans tr
	                    ON tr.dealno = eq.dealno
		                    AND tr.customer_id = eq.customer_id
		                    AND tr.trx_date = eq.trx_date
                    WHERE eq.isHit != tr.isHit
		                    OR eq.purpose != tr.purpose
		                    OR eq.purpose != tr.purpose
		                    OR eq.underlying != tr.underlying
		                    OR ISNULL(eq.InUSD,0) != ISNULL(tr.InUSD,0)
		                    OR eq.isHitJISDOR != tr.isHitJISDOR
		                    OR ISNULL(eq.InUSDJISDOR,0) != ISNULL(tr.InUSDJISDOR,0)
							--darul
							OR eq.lcs_flag != tr.lcs_flag
							--darul
                    
                    INSERT INTO dbo.[[paramTableName]]
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , trx_datetime
	                    , isHit
	                    , purpose
	                    , underlying
	                    , KeteranganUnderlying
	                    , InUSDJISDOR
	                    , isHitJISDOR
	                    , JISDORRate
						--darul
					    , lcs_flag				
						, buy_sell_code			
						, product	
						--darul
                    )
                    SELECT 
	                    tr.src
	                    , tr.dealno
	                    , tr.customer_id
	                    , tr.acc_id
	                    , tr.trx_branch
	                    , tr.trx_date
	                    , tr.currency_code
	                    , tr.amount
	                    , tr.rate
	                    , tr.InUSD
	                    , tr.SourceKey
	                    , tr.trx_datetime
	                    , tr.isHit
	                    , tr.purpose	
	                    , tr.underlying
	                    , tr.KeteranganUnderlying
	                    , tr.InUSDJISDOR
	                    , tr.isHitJISDOR
	                    , tr.JISDORRate
						--darul
						, tr.lcs_flag				
						, tr.buy_sell_code			
						, tr.product	
						--darul
                    FROM #tmpTrans tr
                    LEFT JOIN dbo.[[paramTableName]] eq
	                    ON tr.dealno = eq.dealno
		                    AND tr.customer_id = eq.customer_id
		                    AND tr.trx_date = eq.trx_date
                    WHERE eq.dealno IS NULL
                    GROUP BY 
	                    tr.src
	                    , tr.dealno
	                    , tr.customer_id
	                    , tr.acc_id
	                    , tr.trx_branch
	                    , tr.trx_date
	                    , tr.currency_code
	                    , tr.amount
	                    , tr.rate
	                    , tr.InUSD
	                    , tr.SourceKey
	                    , tr.trx_datetime
	                    , tr.isHit
	                    , tr.purpose	
	                    , tr.underlying
	                    , tr.KeteranganUnderlying
	                    , tr.InUSDJISDOR
	                    , tr.isHitJISDOR
	                    , tr.JISDORRate
						--darul
						, tr.lcs_flag				
						, tr.buy_sell_code			
						, tr.product	
						--darul
                    

                    ";

                strQuery = strQuery.Replace("[[paramTableName]]", this._globalVar.Form42ULTableName);
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                DataSet insertOblRs = await this._msHelper.ExecuteQuery(this._connObl, strQuery, sqlPar);                

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Form042: " + ex.Message;
            }

            return insertRs;
        }

        public async Task<ApiMessage> InsertForm42ByTran(ApiMessage paramIn)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(paramIn);

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                #region Query Insert Data
                string strQuery = @"
                    DELETE eq
                    FROM dbo.[[paramTableName]] eq
                    JOIN dbo.VLSResultFinalToday_TT tr
	                    ON tr.dealno = eq.dealno
		                    AND tr.customer_id = eq.customer_id
		                    AND tr.trx_date = eq.trx_date
                    WHERE eq.isHit != tr.isHit
		                    OR eq.purpose != tr.Purpose
		                    OR eq.purpose != tr.Purpose
		                    OR eq.underlying != tr.underlying
		                    OR ISNULL(eq.InUSD,0) != ISNULL(tr.InUSD,0)
		                    OR eq.isHitJISDOR != tr.isHitJISDOR
		                    OR ISNULL(eq.InUSDJISDOR,0) != ISNULL(tr.InUSDJISDOR,0)
                    
                    INSERT INTO dbo.[[paramTableName]]
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , trx_datetime
	                    , isHit
	                    , purpose
	                    , underlying
	                    , KeteranganUnderlying
	                    , InUSDJISDOR
	                    , isHitJISDOR
	                    , JISDORRate
                    )
                    SELECT 
	                    tt.src
	                    , tr.dealno
	                    , tr.customer_id
	                    , tr.acc_id
	                    , tr.trx_branch
	                    , tr.trx_date
	                    , tr.currency_code
	                    , tr.amount
	                    , tr.rate
	                    , tr.InUSD
	                    , tt.SourceKey
	                    , tt.trx_datetime
	                    , tr.isHit
	                    , tr.Purpose	
	                    , tr.underlying
	                    , tr.ketUnderlying
	                    , tr.InUSDJISDOR
	                    , tr.isHitJISDOR
	                    , tr.JISDORRate
                    FROM dbo.VLSResultFinalToday_TT tr
                    JOIN dbo.VLSTransactionsToday_TR AS tt
                    ON tr.dealno = tt.dealno
	                    AND tr.customer_id = tt.customer_id
                    LEFT JOIN dbo.[[paramTableName]] eq
	                    ON tr.dealno = eq.dealno
		                    AND tr.customer_id = eq.customer_id
		                    AND tr.trx_date = eq.trx_date
                    LEFT JOIN dbo.VLSSummaryToday_TM su
	                    ON tr.customer_id = su.customer_id
                    WHERE 
	                    eq.dealno IS NULL
	                    AND
	                    (
		                    su.customer_id IS NOT NULL
			                    OR ISNULL(tr.underlying,'') != ''
	                    )
                    GROUP BY 
	                    tt.src
	                    , tr.dealno
	                    , tr.customer_id
	                    , tr.acc_id
	                    , tr.trx_branch
	                    , tr.trx_date
	                    , tr.currency_code
	                    , tr.amount
	                    , tr.rate
	                    , tr.InUSD
	                    , tt.SourceKey
	                    , tt.trx_datetime
	                    , tr.isHit
	                    , tr.Purpose	
	                    , tr.underlying
	                    , tr.ketUnderlying
	                    , tr.InUSDJISDOR
	                    , tr.isHitJISDOR
	                    , tr.JISDORRate

                    

                    ";

                strQuery = strQuery.Replace("[[paramTableName]]", this._globalVar.Form42ULTableName);
                #endregion

                DataSet insertOblRs = await this._msHelper.ExecuteQuery(this._connObl, strQuery, sqlPar);

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Form042: " + ex.Message;
            }

            return insertRs;
        }

        public async Task<ApiMessage> InsertIDPLBySummary(ApiMessage<List<IDPLEquivalentUSD>> summaries)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(summaries);

            try
            {               
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsRate = new DataSet();
                DataTable dtRate = new DataTable();

                dtRate = this._msHelper.MapListToTable<IDPLEquivalentUSD>(summaries.Data);

                dsRate.Tables.Add(dtRate);
                dsRate.DataSetName = "Data";
                dsRate.Tables[0].TableName = "Summary";

                string strXml = dsRate.GetXml();

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummaries') IS NOT NULL
	                    DROP TABLE #tmpSummaries

                    CREATE TABLE #tmpSummaries                     
                    (    
	                    PeriodeData		DATETIME
	                    , CIFN			VARCHAR(20)
	                    , customer_id	VARCHAR(20)
	                    , InUSD			MONEY
	                    , branch		VARCHAR(5)
	                    , [name]		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , createdDate	DATETIME
	                    , InUSDJISDOR	MONEY        		
                    )   

                    DECLARE 
	                    @nDocHandle	INT
                        , @dPeriodStart     DATETIME

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummaries
                    (
	                    PeriodeData		
	                    , CIFN			
	                    , customer_id	
	                    , InUSD			
	                    , branch		
	                    , [name]		
	                    , office_name	
	                    , npwp			
	                    , identity_1	
	                    , InUSDJISDOR	 			
                    )
                    SELECT
	                    CAST(strPeriodeData AS DATETIME)		
	                    , CAST(CIFN AS BIGINT)
	                    , customer_id	
	                    , InUSD			
	                    , branch		
	                    , [name]		
	                    , office_name	
	                    , npwp			
	                    , identity_1	
	                    , InUSDJISDOR	 			
                    FROM openxml(@nDocHandle, N'/Data/Summary',2)           
                    WITH (
	                    strPeriodeData	VARCHAR(15)
	                    , CIFN			VARCHAR(20)
	                    , customer_id	VARCHAR(20)
	                    , InUSD			MONEY
	                    , branch		VARCHAR(5)
	                    , [name]		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , InUSDJISDOR	MONEY        		            			
                    )  

                    SELECT TOP 1 @dPeriodStart = PeriodeData
                    FROM #tmpSummaries

                    DELETE tr
                    FROM dbo.[[paramTableName]] tr
                    --JOIN #tmpSummaries ry
	                --    ON tr.customer_id = ry.customer_id
		            --        AND tr.PeriodeData = ry.PeriodeData
                    WHERE tr.PeriodeData = @dPeriodStart                     

                    INSERT INTO dbo.[[paramTableName]]
                    (
	                    PeriodeData
	                    , CIFN
	                    , customer_id
	                    , InUSD
	                    , branch
	                    , name
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , InUSDJISDOR
                    )
                    SELECT 
	                    PeriodeData
	                    , CIFN
	                    , customer_id
	                    , MAX(ISNULL(InUSD,0))
	                    , branch
	                    , name
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , MAX(ISNULL(InUSDJISDOR,0))
                    FROM #tmpSummaries
                    GROUP BY PeriodeData, CIFN, customer_id,branch,name,office_name,npwp,identity_1
                    ";

                strQuery = strQuery.Replace("[[paramTableName]]", this._globalVar.IDPLTableName);
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                DataSet insertOblRs = await this._msHelper.ExecuteQuery(this._connObl, strQuery, sqlPar);

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert IDPL Summary: " + ex.Message;
            }

            return insertRs;
        }
    }
}
