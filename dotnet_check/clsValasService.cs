using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NISPDataSourceNetCore.database;
using NISPDataSourceNetCore.webservice.model;
using Treasury.Scheduler.ONFX.Models;
using Treasury.Scheduler.ONFX.Services.Connection;
using Treasury.Scheduler.ONFX.Utilities;
//20231218, yudha.n, ANT-361, begin
using Oracle.ManagedDataAccess.Client;
using Treasury.Scheduler.ONFX.Models.Valas;
//20231218, yudha.n, ANT-361, end

namespace Treasury.Scheduler.ONFX.Services
{
    public class clsValasService : IValasService
    {
        private IConfiguration _configuration;
        private bool _ignoreSSL;
        private string _strWsOmniObli;
        private string _strConnStringSFX;
        private string _strConnStringSmartfx;
        private string _strEncConnStringObl;
        private string _strEncConnStringSibs;
        private string _strUrlAPIDealKurs;
        private clsAPIHelper _apihelper;
        private clsMsSQLHelper _msHelper;
        private bool _bInsert2SIBS;
        private bool _bInsert2OBL;
        private bool _bInsert2ONFX;
        private ConnectionProperties _connSFX;
        private ConnectionProperties _connSmartFX;
        private ConnectionProperties _connObli;
        private ConnectionProperties _connSibs;
        private readonly IConnectionDB _connDB;
        private GlobalVariableList _globalVariable;
        //20231227, darul.wahid, HTR-214, begin
        private bool _isDaily;

        public bool IsDaily { get => this._isDaily; set => this._isDaily = value; }

        //20231227, darul.wahid, HTR-214, end
        //20231218, yudha.n, ANT-361, begin
        private readonly ConnectionProperties _connMurex;
        //20231218, yudha.n, ANT-361, end

        public clsValasService(IConfiguration iConfig,GlobalVariableList globalVariable, IConnectionDB dB)
        {
            this._configuration = iConfig;
            this._ignoreSSL = globalVariable.ignoreSSL;
            this._strConnStringSFX = globalVariable.ConnectionStringSFx;
            this._strConnStringSmartfx = globalVariable.ConnectionStringSmartfx;
            this._strUrlAPIDealKurs = globalVariable.UrlAPIDealKurs;
            this._apihelper = new clsAPIHelper();
            this._msHelper = new clsMsSQLHelper(globalVariable.UrlWsOmniObli);
            this._bInsert2SIBS = globalVariable.InsertIntoSibs;
            this._bInsert2OBL = globalVariable.InsertIntoObl;
            this._bInsert2ONFX = globalVariable.InsertIntoONFX;
            this._strWsOmniObli = globalVariable.UrlWsOmniObli;
            this._strEncConnStringSibs = globalVariable.EncConnectionStringSibs;
            this._connSFX = globalVariable.ConnectionSFX;
            this._connSmartFX = globalVariable.ConnectionSmartFX;
            this._connObli = globalVariable.ConnectionObli;
            this._connSibs = globalVariable.ConnectionSibs;
            this._connDB = dB;
            this._globalVariable = globalVariable;
            //20231218, yudha.n, ANT-361, begin
            this._connMurex = globalVariable.ConnectionMurex;
            //20231218, yudha.n, ANT-361, end
        }

        public async Task<ApiMessage<List<TDeal>>> InquiryTDeal(ApiMessage<List<ZFXMAST>> param)
        {
            ApiMessage<List<TDeal>> inqTDealRs = new ApiMessage<List<TDeal>>();
            inqTDealRs.copyHeaderForReply(param);

            try
            {                
                DataSet dsData = new DataSet("Data");
                DataTable dtData = new DataTable();
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                List<Task<DataSet>> listInq = new List<Task<DataSet>>();
                List<TDeal> listTran = new List<TDeal>();

                #region Query
                string query = @"
                    IF OBJECT_ID('tempdb..#tmpZFXMASTDeal') IS NOT NULL
	                    DROP TABLE #tmpZFXMASTDeal

                    CREATE TABLE #tmpZFXMASTDeal                     
                    (    
	                    [FXDEALN]	BIGINT                		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @xmlInput

                    INSERT INTO #tmpZFXMASTDeal
                    (
	                    [FXDEALN]									
                    )
                    SELECT
	                    DEALNO             			
                    FROM openxml(@nDocHandle, N'/Data/ZFXMAST',2)           
                    WITH (
	                    DEALNO                    BIGINT
                    )  

                    SELECT
	                    FId
	                    , FPairId
	                    , FCustAcc
	                    , FAmount
	                    , FIORate
	                    , FBranchRate
	                    , FCustRate
	                    , FStatus
	                    , FUSDAmount
	                    , FIdentifier
                        , FAgentId
                        , FDocumentId
                        , FDocumentName
                        , FLastDocumentId
                        , FLastDocumentName
                        --20251016, darul.wahid, ANT-525, begin
                        , FCustCIF
                        --20251016, darul.wahid, ANT-525, end
                    FROM dbo.TDeal AS t
                    JOIN #tmpZFXMASTDeal AS tmp
                    ON t.FId = tmp.[FXDEALN]                ";
                #endregion

                int maxIndex = this._connSmartFX.isDBNISP ? int.Parse(this._configuration["maxIndexRowDBNISPParam"].ToString()) : param.Data.Count();

                List<List<ZFXMAST>> partition = clsUtils.SplitList(param.Data, maxIndex);

                var A = new List<string> { "DEALNO" };

                foreach (List<ZFXMAST> data in partition)
                {
                    dtData = new DataTable();
                    dtData = this._msHelper.MapListToTable<ZFXMAST>(data);

                    var toRemove = dtData.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Except(A).ToList();

                    foreach (var col in toRemove) dtData.Columns.Remove(col);

                    dsData = new DataSet("Data");
                    dsData.Tables.Add(dtData);
                    dsData.Tables[0].TableName = "ZFXMAST";

                    string xmlDeal = dsData.GetXml();

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@xmlInput ", xmlDeal));

                    listInq.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSmartFX, query, sqlPar)));
                }

                await Task.WhenAll(listInq);

                foreach (Task<DataSet> inq in listInq)
                {
                    if (inq.IsFaulted)
                        throw new Exception(inq.Exception.Message);

                    List<TDeal> tmpRes = JsonConvert.DeserializeObject<List<TDeal>>(JsonConvert.SerializeObject(inq.Result.Tables[0]));
                    listTran.AddRange(tmpRes);
                }

                inqTDealRs.IsSuccess = true;
                inqTDealRs.Data = listTran;
                
            }
            catch(Exception ex)
            {
                inqTDealRs.IsSuccess = false;
                inqTDealRs.ErrorDescription = "Error InquiryTDeal(): " + ex.Message;
            }
            finally
            {
                inqTDealRs.MessageDateTime = DateTime.Now;
            }

            return inqTDealRs;
        }

        public async Task<ApiMessage<List<ZFXMAST>>> InquiryZFXMAST(ApiMessage<InqZFXMASTRq> param)
        {
            ApiMessage<List<ZFXMAST>> inqRes = new ApiMessage<List<ZFXMAST>>();
            inqRes.copyHeaderForReply(param);

            DataSet result = new DataSet();
            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                #region Query
                string query = @"
                    SELECT 
                        [FXDEALN]   
                        , CAST([FXBRAN]  AS INT) AS [FXBRAN]
                        , CAST([FXORD7]  AS INT) AS [FXORD7]
                        , CAST(ISNULL([FXDRAC_], 0) AS BIGINT) AS [FXDRAC_]
                        , [FXCUCD]   
                        , [FXTXAM] 
                        , ISNULL([FXREMK], '') AS [FXREMK]
                        , [FXTYPE]
                        , CAST([FXDRCIF] AS BIGINT) AS [FXDRCIF]
                        , [FXACCT]
                        , [FXBSCD]
                        , [FXECFLG]
                        , [FXSTAT]
                        , [FXCRTM]
                        , [FXCIFN]
                        , [FXFIL3]
                        , SUBSTRING(FXDEALN, 6 , 1) AS [trxSource]
                        --20250701, darul.wahid, ONFX-267, begin
                        , ISNULL([FXFLGLCS], '') AS [FXFLGLCS]
                        --20250701, darul.wahid, ONFX-267, end
                    FROM dbo.ZFXMAST 
                    --20250701, darul.wahid, ONFX-267, begin
                    --WHERE [FXBSCD] = @cBuySellCode  
                    WHERE 1 = 1
                    --20250701, darul.wahid, ONFX-267, end
                        AND [FXORD7] >= @jStartDate 
                        AND [FXORD7] <= @jEndDate                                                                               
                        ";

                if (param.Data.StatusNotIn != null)
                { if (param.Data.StatusNotIn.Count > 0)
                    {
                        query += " AND [FXSTAT] NOT IN ('";
                        query += param.Data.StatusNotIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }
                if(!string.IsNullOrEmpty(param.Data.OrderType))
                    query += " AND [FXTYPE] = '"+ param.Data.OrderType + "'";

                if (!string.IsNullOrEmpty(param.Data.RemarkNotLike))
                    query += " AND ISNULL([FXREMK], '')  NOT LIKE '%" + param.Data.RemarkNotLike + "%'";

                if (!string.IsNullOrEmpty(param.Data.ErrFlagNot))
                    query += " AND ISNULL([FXECFLG], '')  != '" + param.Data.ErrFlagNot + "'";
                    
                //20241204,pratama,ANT-456,begin
                if (param.Data.paramExcludeTrancode.Count > 0)
                    query += $" AND ISNULL(FXAUXT, '') NOT IN ('{param.Data.paramExcludeTrancode.Aggregate((i, j) => i + "', '" + j)}') ";
                //20241204,pratama,ANT-456,end

                //20250701, darul.wahid, ONFX-267, begin
                if (!string.IsNullOrEmpty(param.Data.BuySellCode))
                    query += " AND [FXBSCD] = '" + param.Data.BuySellCode + "'";

                if (param.Data.BuySellCodeIn != null)
                {
                    if (param.Data.BuySellCodeIn.Count > 0)
                    {
                        query += " AND [FXBSCD] IN ('";
                        query += param.Data.BuySellCodeIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }
                //20250701, darul.wahid, ONFX-267, end
                #endregion

                //20250701, darul.wahid, ONFX-267, begin
                //sqlPar.Add(new SqlParameter("@cBuySellCode ", param.Data.BuySellCode));
                //20250701, darul.wahid, ONFX-267, end
                sqlPar.Add(new SqlParameter("@jStartDate ", clsUtils.DatetimeToJulian(param.Data.DateStart)));
                sqlPar.Add(new SqlParameter("@jEndDate ", clsUtils.DatetimeToJulian(param.Data.DateEnd)));

                result = await this._msHelper.ExecuteQuery(this._connSFX, query, sqlPar);
                
                List<ZFXMAST> dataTran = JsonConvert.DeserializeObject<List<ZFXMAST>>(JsonConvert.SerializeObject(result.Tables[0]));

                dataTran
                    .ForEach(i => i.FXDEALN = i.FXDEALN.Trim());
                    
                inqRes.IsSuccess = true;
                inqRes.Data = dataTran;
            }
            catch(Exception ex)
            {
                inqRes.IsSuccess = false;
                inqRes.ErrorDescription = "Error InquiryZFXMAST() " + ex.Message;
            }
            finally
            {
                inqRes.MessageDateTime = DateTime.Now;
            }

            return inqRes;
        }

        // 20250928, Filian, ONFX-276, begin
        public async Task<ApiMessage<List<VLSTransaction>>> InquiryVLSTransactionONFX276(string type)
        {
            ApiMessage<List<VLSTransaction>> inqRes = new ApiMessage<List<VLSTransaction>>();

            DataSet result = new DataSet();
            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                #region Query
                string query = @"
                    SELECT 
                        src,
                        dealno,
                        customer_id,
                        acc_id,
                        trx_branch,
                        trx_date,
                        trx_datetime,
                        currency_code,
                        amount,
                        rate,
                        InUSD,
                        SourceKey,
                        JISDORRate,
                        InUSDJISDOR,
                        m_pl_key,
                        NIKAgent,
                        GuidProcess,
                        m_pl_key1
                    FROM dbo.VLSTransactions_TR_BackupONFX276 
                ";
                #endregion

                if (type.ToLower() == "option")
                {
                    query += @"
                        WHERE m_pl_key in ('OPTVLS', 'OPTIDR')
                    ";
                } else
                {
                    query += @"
                        WHERE m_pl_key not in ('OPTVLS', 'OPTIDR')
                    ";
                }
                
                result = await this._msHelper.ExecuteQuery(this._connSFX, query, sqlPar);

                List<VLSTransaction> dataTran = JsonConvert.DeserializeObject<List<VLSTransaction>>(JsonConvert.SerializeObject(result.Tables[0]));

                inqRes.IsSuccess = true;
                inqRes.Data = dataTran;
            }
            catch (Exception ex)
            {
                inqRes.IsSuccess = false;
                inqRes.ErrorDescription = "Error InquiryVLSTransactionONFX276() " + ex.Message;
            }
            finally
            {
                inqRes.MessageDateTime = DateTime.Now;
            }

            return inqRes;
        }

        public async Task<ApiMessage<List<TRN_HDR_FX>>> inquiryTRN_HDR_FX(ApiMessage<MxTranRq> param)
        {
            ApiMessage<List<TRN_HDR_FX>> inqRes = new ApiMessage<List<TRN_HDR_FX>>();
            inqRes.copyHeaderForReply(param);

            DataSet result = new DataSet();
            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                #region Query
                string query = @"
                select M_NB
                    , M_IDSIBSCIF1
                    , M_TRN_DATE
                    , TIME
                    , M_BRW_NOMU1
                    , M_BRW_NOM1
                    , M_BRW_NOMU2
                    , M_BRW_NOM2
                    , M_COMMENT_BS
                    , M_SOURCE_ID
                    , M_RSNCODE
                    , M_PL_KEY1
                    , M_TRN_TYPO
                    --20241209, darul.wahid, MRX-2754, begin
                    , TRIM(M_TRN_FMLY) AS M_TRN_FMLY
                    , TRIM(M_TRN_GRP) AS M_TRN_GRP
                    , TRIM(M_TRN_TYPE) AS M_TRN_TYPE
                    --20241209, darul.wahid, MRX-2754, end
                from dbo.TRN_HDR_FX
                where M_TRN_DATE >= @dStartDate and M_TRN_DATE <= @dEndDate
                    and 
	                    ( M_BRW_NOMU2 = @cCur  OR M_BRW_NOMU1 = @cCur)                    
                    and isnumeric(M_IDSIBSCIF1) = 1   
                    and M_IDSIBSCIF1 <> '8888888888888888888'   
                    and (M_SOURCE_ID <> 'BDS' OR M_SOURCE_ID IS NULL)
                    and isnull(M_RSNCODE,'') = ''                       
                    --and M_TRN_TYPO = @cTypology                                                                              
                        ";
                        
                //20240819,pratama,DCR-158,begin
                query += " AND M_TRN_TYPO NOT IN ('A286_FX_COMM_SPOT') ";
                //20240819,pratama,DCR-158,end

                if (param.Data.CommentBsIn != null)
                {
                    if (param.Data.CommentBsIn.Count > 0)
                    {
                        query += " AND [M_COMMENT_BS] IN ('";
                        query += param.Data.CommentBsIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.KeyIn != null)
                {
                    if (param.Data.KeyIn.Count > 0)
                    {
                        query += " AND [M_PL_KEY1] IN ('";
                        query += param.Data.KeyIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.TypoIn != null)
                {
                    if (param.Data.TypoIn.Count > 0)
                    {
                        query += " AND [M_TRN_TYPO] IN ('";
                        query += param.Data.TypoIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }
                #endregion

                sqlPar.Add(new SqlParameter("@dStartDate ", param.Data.StartDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new SqlParameter("@dEndDate ", param.Data.EndDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new SqlParameter("@cCur", param.Data.M_BRW_NOMU));

                result = await this._msHelper.ExecuteQuery(this._connSFX, query, sqlPar);

                List<TRN_HDR_FX> dataTran = JsonConvert.DeserializeObject<List<TRN_HDR_FX>>(JsonConvert.SerializeObject(result.Tables[0]));

                inqRes.IsSuccess = true;
                inqRes.Data = dataTran;
            }
            catch (Exception ex)
            {
                inqRes.IsSuccess = false;
                inqRes.ErrorDescription = "Error inquiryTRN_HDR_FX(): " + ex.Message;
            }
            finally
            {
                inqRes.MessageDateTime = DateTime.Now;
            }

            return inqRes;
        }

        public async Task<ApiMessage<List<TRN_HDR_FX>>> inquiry_VW_TRN_HDR_FX(ApiMessage<MxTranRq> param)
        {
            ApiMessage<List<TRN_HDR_FX>> inqRes = new ApiMessage<List<TRN_HDR_FX>>();
            inqRes.copyHeaderForReply(param);

            DataSet result = new DataSet();
            try
            {
                List<OracleParameter> sqlPar = new();
                #region Query
                string query = @"
                SELECT M_NB
                     , M_IDSIBSCIF1
                     , M_TRN_DATE
                     , TIME
                     , M_BRW_NOMU1
                     , M_BRW_NOM1
                     , M_BRW_NOMU2
                     , M_BRW_NOM2
                     , M_COMMENT_BS
                     , M_SOURCE_ID
                     , M_RSNCODE
                     , M_PL_KEY1
                     , M_TRN_TYPO
                    --20241209, darul.wahid, MRX-2754, begin
                    , TRIM(M_TRN_FMLY) AS M_TRN_FMLY
                    , TRIM(M_TRN_GRP) AS M_TRN_GRP
                    , TRIM(M_TRN_TYPE) AS M_TRN_TYPE
                    --20241209, darul.wahid, MRX-2754, end
                    --20251017, darul.wahid, ONFX-280, begin
                    , TRIM(BrokerName) AS BrokerName
                    , TRIM(Purpose) AS Purpose
                    , TRIM(CurrencyPair) AS CurrencyPair
                    , TRIM(M_LABEL) AS M_LABEL
                    , TRIM(Underlying) AS Underlying
                    , TRIM(Keterangan) AS Keterangan
                    , M_SINTERNAL
                    , M_BINTERNAL
                    , FlagLCS
                    --20251017, darul.wahid, ONFX-280, end
                FROM [[MurexSchema]].VW_TRN_HDR_FX
                WHERE M_TRN_DATE >= :dStartDate and M_TRN_DATE <= :dEndDate
                    and 
	                    ( M_BRW_NOMU2 = :cCur  OR M_BRW_NOMU1 = :cCur)                    
                     AND translate( trim(M_IDSIBSCIF1), ' 1234567890', 'X' ) is null 
                    AND M_IDSIBSCIF1 <> '8888888888888888888'   
                    AND (M_SOURCE_ID <> 'BDS' OR M_SOURCE_ID IS NULL)
                    AND NVL(M_RSNCODE,' ') = ' '
                    AND M_TRN_TYPO NOT IN ('A286_FX_COMM_SPOT')
                        ";

                if (param.Data.CommentBsIn != null)
                {
                    if (param.Data.CommentBsIn.Count > 0)
                    {
                        query += " AND M_COMMENT_BS IN ('";
                        query += param.Data.CommentBsIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.KeyIn != null)
                {
                    if (param.Data.KeyIn.Count > 0)
                    {
                        query += " AND M_PL_KEY1 IN ('";
                        query += param.Data.KeyIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.TypoIn != null)
                {
                    if (param.Data.TypoIn.Count > 0)
                    {
                        query += " AND M_TRN_TYPO IN ('";
                        query += param.Data.TypoIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }
                #endregion
                query = query.Replace("[[MurexSchema]]", this._configuration["MurexSchema"].ToString());

                sqlPar.Add(new OracleParameter(":dStartDate", param.Data.StartDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new OracleParameter(":dEndDate", param.Data.EndDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new OracleParameter(":cCur", param.Data.M_BRW_NOMU));

                result = await this._msHelper.ExecuteOracleQuery(this._connMurex, query, sqlPar);

                List<TRN_HDR_FX> dataTran = JsonConvert.DeserializeObject<List<TRN_HDR_FX>>(JsonConvert.SerializeObject(result.Tables[0]));

                inqRes.IsSuccess = true;
                inqRes.Data = dataTran;
            }
            catch (Exception ex)
            {
                inqRes.IsSuccess = false;
                inqRes.ErrorDescription = "Error inquiryTRN_HDR_FX(): " + ex.Message;
            }
            finally
            {
                inqRes.MessageDateTime = DateTime.Now;
            }

            return inqRes;
        }
        
        public async Task<ApiMessage<List<TRN_HDR_FX>>> inquiry_VW_TRN_HDR_FX_Today(ApiMessage<MxTranRq> param)
        {
            ApiMessage<List<TRN_HDR_FX>> inqRes = new ApiMessage<List<TRN_HDR_FX>>();
            inqRes.copyHeaderForReply(param);

            DataSet result = new DataSet();
            try
            {
                List<OracleParameter> sqlPar = new();
                #region Query
                string query = @"
                SELECT M_NB
                     , M_IDSIBSCIF1
                     , M_TRN_DATE
                     , TIME
                     , M_BRW_NOMU1
                     , CASE WHEN NVL(M_RSNCODE,' ') != ' ' THEN -1*M_BRW_NOM1
                            ELSE M_BRW_NOM1 END AS M_BRW_NOM1
                     , M_BRW_NOMU2
                     , CASE WHEN NVL(M_RSNCODE,' ') != ' ' THEN -1*M_BRW_NOM2
                            ELSE M_BRW_NOM2 END AS M_BRW_NOM2
                     , M_COMMENT_BS
                     , M_SOURCE_ID
                     , M_RSNCODE
                     , M_PL_KEY1
                     , M_TRN_TYPO
                    --20241209, darul.wahid, MRX-2754, begin
                    , TRIM(M_TRN_FMLY) AS M_TRN_FMLY
                    , TRIM(M_TRN_GRP) AS M_TRN_GRP
                    , TRIM(M_TRN_TYPE) AS M_TRN_TYPE
                    --20241209, darul.wahid, MRX-2754, end
                    --20251017, darul.wahid, ONFX-280, begin
                    , TRIM(BrokerName) AS BrokerName
                    , TRIM(Purpose) AS Purpose
                    , TRIM(CurrencyPair) AS CurrencyPair
                    , TRIM(M_LABEL) AS M_LABEL
                    , TRIM(Underlying) AS Underlying
                    , TRIM(Keterangan) AS Keterangan
                    , M_SINTERNAL
                    , M_BINTERNAL
                    , FlagLCS
                    --20251017, darul.wahid, ONFX-280, end
                FROM [[MurexSchema]].VW_TRN_HDR_FX_Temp
                WHERE M_TRN_DATE >= :dStartDate and M_TRN_DATE <= :dEndDate
                    and ( M_BRW_NOMU2 = :cCur  OR M_BRW_NOMU1 = :cCur)                    
                    AND translate( trim(M_IDSIBSCIF1), ' 1234567890', 'X' ) is null 
                    AND M_IDSIBSCIF1 <> '8888888888888888888'   
                    AND (M_SOURCE_ID <> 'BDS' OR M_SOURCE_ID IS NULL)
                    AND (
                            NVL(M_RSNCODE,' ') = ' '
                            OR ( NVL(M_RSNCODE,' ') != ' ' AND M_TRN_DATE < :dEndDate )
                        )
                    AND M_TRN_TYPO NOT IN ('A286_FX_COMM_SPOT')
                        ";

                if (param.Data.CommentBsIn != null)
                {
                    if (param.Data.CommentBsIn.Count > 0)
                    {
                        query += " AND M_COMMENT_BS IN ('";
                        query += param.Data.CommentBsIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.KeyIn != null)
                {
                    if (param.Data.KeyIn.Count > 0)
                    {
                        query += " AND M_PL_KEY1 IN ('";
                        query += param.Data.KeyIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.TypoIn != null)
                {
                    if (param.Data.TypoIn.Count > 0)
                    {
                        query += " AND M_TRN_TYPO IN ('";
                        query += param.Data.TypoIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }
                #endregion
                query = query.Replace("[[MurexSchema]]", this._configuration["MurexSchema"].ToString());

                sqlPar.Add(new OracleParameter(":dStartDate", param.Data.StartDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new OracleParameter(":dEndDate", param.Data.EndDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new OracleParameter(":cCur", param.Data.M_BRW_NOMU));

                result = await this._msHelper.ExecuteOracleQuery(this._connMurex, query, sqlPar);

                List<TRN_HDR_FX> dataTran = JsonConvert.DeserializeObject<List<TRN_HDR_FX>>(JsonConvert.SerializeObject(result.Tables[0]));

                inqRes.IsSuccess = true;
                inqRes.Data = dataTran;
            }
            catch (Exception ex)
            {
                inqRes.IsSuccess = false;
                inqRes.ErrorDescription = "Error inquiryTRN_HDR_FX(): " + ex.Message;
            }
            finally
            {
                inqRes.MessageDateTime = DateTime.Now;
            }

            return inqRes;
        }

        public async Task<ApiMessage<List<TDealDocument>>> inquiryTDealDoc(ApiMessage<List<TDeal>> param)
        {
            ApiMessage<List<TDealDocument>> inquiryRs = new ApiMessage<List<TDealDocument>>();
            inquiryRs.copyHeaderForReply(param);

            try
            {
                DataSet dsData = new DataSet("Data");
                DataTable dtData = new DataTable();
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                List<Task<DataSet>> listInq = new List<Task<DataSet>>();
                List<TDealDocument> listTran = new List<TDealDocument>();

                #region Query
                string query = @"
                    IF OBJECT_ID('tempdb..#tmpDeal') IS NOT NULL
	                    DROP TABLE #tmpDeal

                    CREATE TABLE #tmpDeal                     
                    (    
	                    FDocumentId	INT                		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @xmlInput

                    INSERT INTO #tmpDeal
                    (
	                    FDocumentId									
                    )
                    SELECT
	                    FDocumentId
                    FROM openxml(@nDocHandle, N'/Data/Deal',2)           
                    WITH (
	                    FDocumentId         INT       			
                    )  

                    SELECT DISTINCT
	                    FId
                        , FLabel
                        , FExtLink
                        , FExpired
                    FROM dbo.TDealDocument AS t
                    JOIN #tmpDeal AS tmp
                    ON t.FId = tmp.FDocumentId
                    ";
                #endregion

                int maxIndex = this._connSmartFX.isDBNISP ? int.Parse(this._configuration["maxIndexRowDBNISPParam"].ToString()) : param.Data.Count();

                List<List<TDeal>> partition = clsUtils.SplitList(param.Data, maxIndex);

                var A = new List<string> { "FDocumentId" };

                foreach (List<TDeal> data in partition)
                {
                    dtData = new DataTable();
                    dtData = this._msHelper.MapListToTable<TDeal>(data);

                    var toRemove = dtData.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Except(A).ToList();

                    foreach (var col in toRemove) dtData.Columns.Remove(col);

                    dsData = new DataSet("Data");
                    dsData.Tables.Add(dtData);
                    dsData.DataSetName = "Data";
                    dsData.Tables[0].TableName = "Deal";
                    dsData.Tables[0].Columns[0].ColumnName = "FDocumentId";

                    string xmlDeal = dsData.GetXml();

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@xmlInput ", xmlDeal));

                    listInq.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSmartFX, query, sqlPar)));
                }

                await Task.WhenAll(listInq);

                foreach (Task<DataSet> inq in listInq)
                {
                    if (inq.IsFaulted)
                        throw new Exception(inq.Exception.Message);

                    List<TDealDocument> tmpRes = JsonConvert.DeserializeObject<List<TDealDocument>>(JsonConvert.SerializeObject(inq.Result.Tables[0]));
                    listTran.AddRange(tmpRes);
                }
                
                inquiryRs.IsSuccess = true;
                inquiryRs.Data = listTran;
            }
            catch (Exception e)
            {
                inquiryRs.IsSuccess = false;
                inquiryRs.ErrorDescription = "Error inquiryTDealDoc(): " + e.Message;
            }
            finally
            {
                inquiryRs.MessageDateTime = DateTime.Now;
            }

            return inquiryRs;
        }

        public async Task<ApiMessage<List<TAgent>>> inquiryTAgent(ApiMessage<List<TDeal>> param)
        {
            ApiMessage<List<TAgent>> inquiryRs = new ApiMessage<List<TAgent>>();
            inquiryRs.copyHeaderForReply(param);

            try
            {
                DataSet dsData = new DataSet("Data");
                DataTable dtData = new DataTable();
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                List<Task<DataSet>> listInq = new List<Task<DataSet>>();
                List<TAgent> listTran = new List<TAgent>();

                #region Query
                string query = @"
                    IF OBJECT_ID('tempdb..#tmpDeal') IS NOT NULL
	                    DROP TABLE #tmpDeal

                    CREATE TABLE #tmpDeal                     
                    (    
	                    FAgentId	INT                		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @xmlInput

                    INSERT INTO #tmpDeal
                    (
	                    FAgentId									
                    )
                    SELECT
	                    FAgentId
                    FROM openxml(@nDocHandle, N'/Data/Deal',2)           
                    WITH (
	                    FAgentId         INT       			
                    )  

                    SELECT DISTINCT
	                    FId
                        , FBranchId
                        , FLabel
                        , FExtLink
                        , FExtId
                        , FTypeId
                    FROM dbo.TAgent AS t
                    JOIN #tmpDeal AS tmp
                    ON t.FId = tmp.FAgentId
                    ";
                #endregion

                int maxIndex = this._connSmartFX.isDBNISP ? int.Parse(this._configuration["maxIndexRowDBNISPParam"].ToString()) : param.Data.Count();

                List<List<TDeal>> partition = clsUtils.SplitList(param.Data, maxIndex);

                var A = new List<string> { "FAgentId" };

                foreach (List<TDeal> data in partition)
                {
                    dtData = new DataTable();
                    dtData = this._msHelper.MapListToTable<TDeal>(data);

                    var toRemove = dtData.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Except(A).ToList();

                    foreach (var col in toRemove) dtData.Columns.Remove(col);

                    dsData = new DataSet("Data");
                    dsData.Tables.Add(dtData);
                    dsData.DataSetName = "Data";
                    dsData.Tables[0].TableName = "Deal";
                    dsData.Tables[0].Columns[0].ColumnName = "FAgentId";

                    string xmlDeal = dsData.GetXml();

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@xmlInput ", xmlDeal));

                    listInq.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSmartFX, query, sqlPar)));
                }

                await Task.WhenAll(listInq);

                foreach (Task<DataSet> inq in listInq)
                {
                    if (inq.IsFaulted)
                        throw new Exception(inq.Exception.Message);

                    List<TAgent> tmpRes = JsonConvert.DeserializeObject<List<TAgent>>(JsonConvert.SerializeObject(inq.Result.Tables[0]));
                    listTran.AddRange(tmpRes);
                }
                
                inquiryRs.IsSuccess = true;
                inquiryRs.Data = listTran;
            }
            catch (Exception e)
            {
                inquiryRs.IsSuccess = false;
                inquiryRs.ErrorDescription = "Error inquiryTAgent(): " + e.Message;
            }
            finally
            {
                inquiryRs.MessageDateTime = DateTime.Now;
            }

            return inquiryRs;
        }

        private async Task<ApiMessage> InsertValasTransactionByTablename(ApiMessage<List<VLSTransaction>> trans, string TableName)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(trans);

            try
            {             
                List<SqlParameter> sqlPar = new List<SqlParameter>();                

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSTransaction>(trans.Data);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Trans";

                string strXml = dsTran.GetXml();

                #region Query Populate Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpTrans') IS NOT NULL
	                    DROP TABLE #tmpTrans

                    CREATE TABLE #tmpTrans                     
                    (    
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , trx_date			DATETIME
	                    , trx_datetime		DATETIME
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpTrans
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR							
                    )
                    SELECT
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , strTrx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR		        			
                    FROM openxml(@nDocHandle, N'/Data/Trans',2)           
                    WITH (
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , strTrx_date		VARCHAR(20)
                        , strTrx_datetime	VARCHAR(50)
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY             			
                    )  

--20240508, darul.wahid, HTR - 214, begin
                    --DELETE FROM dbo.[[paramTableName]]
                    TRUNCATE TABLE dbo.[[paramTableName]]
--20240508, darul.wahid, HTR - 214, end

                    INSERT INTO dbo.[[paramTableName]] 
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR	
                    )
                    SELECT 
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR	
                    FROM #tmpTrans
                    ";

                strQuery = strQuery.Replace("[[paramTableName]]", TableName);
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                Task<DataSet> insertSFXRs;
                Task<DataSet> insertOblRs;
                Task<DataSet> insertSibsRs;

                if (this._bInsert2ONFX)
                {
                    insertSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listInsert.Add(insertSFXRs);
                }

                if (this._bInsert2OBL)
                {
                    insertOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listInsert.Add(insertOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    insertSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listInsert.Add(insertSibsRs);
                }

                await Task.WhenAll(listInsert);

                foreach(Task<DataSet> insert in listInsert)
                {
                    if(insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Transaction: " + ex.Message;
            }

            return insertRs;
        }

        private async Task<ApiMessage> InsertValasSummaryByTablename(ApiMessage<List<VLSSummary>> sumaries, string TableName)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(sumaries);

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();                

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSSummary>(sumaries.Data);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Summaries";

                string strXml = dsTran.GetXml();

                #region Query Populate Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
	                    , InUSD			MONEY
	                    , branch		VARCHAR(5)
	                    , [name]		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , ProcessTime	DATETIME
	                    , InUSDJISDOR	MONEY    		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR							
                    )
                    SELECT
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , strProcessTime
	                    , InUSDJISDOR	        			
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
	                    , InUSD			    MONEY
	                    , branch		    VARCHAR(5)
	                    , [name]		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    , ProcessTime	    DATETIME
                        , strProcessTime	VARCHAR(50)	                    
                        , InUSDJISDOR	    MONEY    	          			
                    )  

--20240508, darul.wahid, HTR - 214, begin
                    --DELETE FROM dbo.[[paramTableName]]
                    TRUNCATE TABLE dbo.[[paramTableName]]
--20240508, darul.wahid, HTR - 214, end

                    INSERT INTO dbo.[[paramTableName]] 
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    )
                    SELECT 
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    FROM #tmpSummary
                    ";

                strQuery = strQuery.Replace("[[paramTableName]]", TableName);
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                Task<DataSet> insertSFXRs;
                Task<DataSet> insertOblRs;
                Task<DataSet> insertSibsRs;

                if (this._bInsert2ONFX)
                {
                    insertSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listInsert.Add(insertSFXRs);
                }

                if (this._bInsert2OBL)
                {
                    insertOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listInsert.Add(insertOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    insertSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listInsert.Add(insertSibsRs);
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Summary: " + ex.Message;
            }

            return insertRs;
        }

        private async Task<ApiMessage> InsertValasResultByTablename(ApiMessage<List<VLSResultFinal>> results, string TableName)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(results);

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();                
              
                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSResultFinal>(results.Data);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Results";

                string strXml = dsTran.GetXml();

                #region Query Populate Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpResult') IS NOT NULL
	                    DROP TABLE #tmpResult

                    CREATE TABLE #tmpResult                     
                    (    
	                    branch			VARCHAR(5)
	                    , customer_id	VARCHAR(20)
	                    , [name]		VARCHAR(20)
	                    , acc_id		VARCHAR(20)
	                    , trx_branch	VARCHAR(5)
	                    , trx_date		DATETIME
	                    , currency_code	VARCHAR(5)
	                    , amount		MONEY
	                    , rate			FLOAT
	                    , InUSD			MONEY
	                    , dealno		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , isHit			BIT
	                    , ProcessTime	DATETIME
	                    , underlying	VARCHAR(15)
	                    , JISDORRate	FLOAT
	                    , InUSDJISDOR	MONEY
	                    , isHitJISDOR	BIT   		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpResult
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 						
                    )
                    SELECT
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR        			
                    FROM openxml(@nDocHandle, N'/Data/Results',2)           
                    WITH (
	                    branch			VARCHAR(5)
	                    , customer_id	VARCHAR(20)
	                    , [name]		VARCHAR(20)
	                    , acc_id		VARCHAR(20)
	                    , trx_branch	VARCHAR(5)
	                    , trx_date		DATETIME
	                    , currency_code	VARCHAR(5)
	                    , amount		MONEY
	                    , rate			FLOAT
	                    , InUSD			MONEY
	                    , dealno		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , isHit			BIT
	                    , ProcessTime	DATETIME
	                    , underlying	VARCHAR(15)
	                    , JISDORRate	FLOAT
	                    , InUSDJISDOR	MONEY
	                    , isHitJISDOR	BIT   		          			
                    )  

--20240508, darul.wahid, HTR - 214, begin
                    --DELETE FROM dbo.[[paramTableName]]
                    TRUNCATE TABLE dbo.[[paramTableName]]
--20240508, darul.wahid, HTR - 214, end

                    INSERT INTO dbo.[[paramTableName]] 
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                    )
                    SELECT 
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                    FROM #tmpResult
                    ";

                strQuery = strQuery.Replace("[[paramTableName]]", TableName);
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                Task<DataSet> insertSFXRs;
                Task<DataSet> insertOblRs;
                Task<DataSet> insertSibsRs;

                if (this._bInsert2ONFX)
                {
                    insertSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listInsert.Add(insertSFXRs);
                }

                if (this._bInsert2OBL)
                {
                    insertOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listInsert.Add(insertOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    insertSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listInsert.Add(insertSibsRs);
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

               
                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Result: " + ex.Message;
            }

            return insertRs;
        }

        private async Task<ApiMessage> InsertValasTransactionByQuery(ApiMessage<List<VLSTransaction>> trans, string strQuery)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(trans);                

            try
            {
                if (string.IsNullOrEmpty(strQuery))
                    throw new Exception("string query cannot be empty");

                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSTransaction>(trans.Data);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Trans";

                string strXml = dsTran.GetXml();

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                Task<DataSet> insertSFXRs;
                Task<DataSet> insertOblRs;
                Task<DataSet> insertSibsRs;

                if (this._bInsert2ONFX)
                {
                    insertSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listInsert.Add(insertSFXRs);
                }

                if (this._bInsert2OBL)
                {
                    insertOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listInsert.Add(insertOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    insertSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listInsert.Add(insertSibsRs);
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Transaction: " + ex.Message;
            }

            return insertRs;
        }

        private async Task<ApiMessage> InsertValasSummaryByQuery(ApiMessage<List<VLSSummary>> sumaries, string strQuery)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(sumaries);

            try
            {
                if (string.IsNullOrEmpty(strQuery))
                    throw new Exception("string query cannot be empty");

                List<SqlParameter> sqlPar = new List<SqlParameter>();
               
                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSSummary>(sumaries.Data);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Summaries";

                string strXml = dsTran.GetXml();

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                Task<DataSet> insertSFXRs;
                Task<DataSet> insertOblRs;
                Task<DataSet> insertSibsRs;

                if (this._bInsert2ONFX)
                {
                    insertSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listInsert.Add(insertSFXRs);
                }

                if (this._bInsert2OBL)
                {
                    insertOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listInsert.Add(insertOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    insertSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listInsert.Add(insertSibsRs);
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Summary: " + ex.Message;
            }

            return insertRs;
        }

        private async Task<ApiMessage> InsertValasResultByQuery(ApiMessage<List<VLSResultFinal>> results, string strQuery)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(results);

            try
            {
                if (string.IsNullOrEmpty(strQuery))
                    throw new Exception("string query cannot be empty");

                List<SqlParameter> sqlPar = new List<SqlParameter>();              

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSResultFinal>(results.Data);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Results";

                string strXml = dsTran.GetXml();
                
                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                Task<DataSet> insertSFXRs;
                Task<DataSet> insertOblRs;
                Task<DataSet> insertSibsRs;

                if (this._bInsert2ONFX)
                {
                    insertSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listInsert.Add(insertSFXRs);
                }

                if (this._bInsert2OBL)
                {
                    insertOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listInsert.Add(insertOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    insertSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listInsert.Add(insertSibsRs);
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Result: " + ex.Message;
            }

            return insertRs;
        }

        public async Task<ApiMessage> InsertValasTransaction(ApiMessage<List<VLSTransaction>> trans, bool isDaily)
        {

            #region Query Populate Data
            string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpTrans') IS NOT NULL
	                    DROP TABLE #tmpTrans

                    CREATE TABLE #tmpTrans                     
                    (    
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , trx_date			DATETIME
	                    , trx_datetime		DATETIME
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpTrans
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR	
                        , m_pl_key
                        , nik_agent
                    )
                    SELECT
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , strTrx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR		
                        , m_pl_key
                        , nik_agent         
                    FROM openxml(@nDocHandle, N'/Data/Trans',2)           
                    WITH (
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , strTrx_date		VARCHAR(20)
                        , strTrx_datetime	VARCHAR(50)
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                    )  ";

            if(isDaily)
            {
                strQuery = strQuery + @"
--20240508, darul.wahid, HTR-214, begin
                    --DELETE FROM dbo.VLSTransactions_TR
                    TRUNCATE TABLE dbo.VLSTransactions_TR
--20240508, darul.wahid, HTR-214, end

                    INSERT INTO dbo.VLSTransactions_TR
                    (
                        src
                        , dealno
                        , customer_id
                        , acc_id
                        , trx_branch
                        , trx_date
                        , trx_datetime
                        , currency_code
                        , amount
                        , rate
                        , InUSD
                        , SourceKey
                        , JISDORRate
                        , InUSDJISDOR
                        , m_pl_key
                        , NIKAgent         
                    )
                    SELECT

                        src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR
                        , m_pl_key
                        , nik_agent         
                    FROM #tmpTrans";
            }
            else
            {
                strQuery = strQuery + @"                    
                    INSERT INTO dbo.VLSTransactionsToday_TR
                    (
                        src
                        , dealno
                        , customer_id
                        , acc_id
                        , trx_branch
                        , trx_date
                        , trx_datetime
                        , currency_code
                        , amount
                        , rate
                        , InUSD
                        , SourceKey
                        , JISDORRate
                        , InUSDJISDOR
                        , m_pl_key
                        , NIKAgent         
                    )
                    SELECT

                        tmp.src
	                    , tmp.dealno
	                    , tmp.customer_id
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.trx_datetime
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.SourceKey
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
                        , tmp.m_pl_key
                        , tmp.nik_agent         
                    FROM #tmpTrans AS tmp
                    LEFT JOIN dbo.VLSTransactionsToday_TR AS tr
                    ON tmp.dealno = tr.dealno
                    WHERE tr.dealno is null";
            }                    
            #endregion

            return await this.InsertValasTransactionByQuery(trans, strQuery);
        }

        public async Task<ApiMessage> InsertValasSummary(ApiMessage<List<VLSSummary>> sumaries, bool isDaily)
        {
            #region Query Populate Data
            string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
	                    , InUSD			MONEY
	                    , branch		VARCHAR(5)
	                    , [name]		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , ProcessTime	DATETIME
	                    , InUSDJISDOR	MONEY    		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR							
                    )
                    SELECT
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , strProcessTime
	                    , InUSDJISDOR	        			
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
	                    , InUSD			    MONEY
	                    , branch		    VARCHAR(5)
	                    , [name]		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    --, ProcessTime	    DATETIME
                        , strProcessTime	VARCHAR(50)	                    
                        , InUSDJISDOR	    MONEY    	          			
                    )  

                    ";

            if (isDaily)
            {
                strQuery = strQuery + @"
--20240508, darul.wahid, HTR-214, begin
                    --DELETE FROM dbo.VLSSummary_TM
                    TRUNCATE TABLE dbo.VLSSummary_TM
--20240508, darul.wahid, HTR-214, end

                    INSERT INTO dbo.VLSSummary_TM
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    )
                    SELECT 
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    FROM #tmpSummary";
            }
            else
            {
                strQuery = strQuery + @"                    
                    INSERT INTO dbo.VLSSummaryToday_TM
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    )
                    SELECT 
	                    tmp.customer_id
	                    , tmp.InUSD
	                    , tmp.branch
	                    , tmp.[name]
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.ProcessTime
	                    , tmp.InUSDJISDOR
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.VLSSummaryToday_TM AS tm
                    ON tmp.customer_id = tm.customer_id
                    WHERE tm.customer_id is null

                    UPDATE tm
                    SET InUSD = new.InUSD
                        , InUSDJISDOR = new.InUSDJISDOR
                    FROM dbo.VLSSummaryToday_TM AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id
                    
                    ";
            }
            #endregion

            return await this.InsertValasSummaryByQuery(sumaries, strQuery);
        }

        public async Task<ApiMessage> InsertValasResult(ApiMessage<List<VLSResultFinal>> results, bool isDaily)
        {
            #region Query Populate Data
            string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpResult') IS NOT NULL
	                    DROP TABLE #tmpResult

                    CREATE TABLE #tmpResult                     
                    (    
	                    branch			    VARCHAR(5)
	                    , customer_id	    VARCHAR(20)
	                    , [name]		    VARCHAR(20)
	                    , acc_id		    VARCHAR(20)
	                    , trx_branch	    VARCHAR(5)
	                    , trx_date		    DATETIME
	                    , currency_code	    VARCHAR(5)
	                    , amount		    MONEY
	                    , rate			    FLOAT
	                    , InUSD			    MONEY
	                    , dealno		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    , isHit			    BIT
	                    , ProcessTime	    DATETIME
	                    , underlying	    VARCHAR(15)
                        , KetUnderlying	    VARCHAR(150)
                        , Purpose           VARCHAR(50)
	                    , JISDORRate	    FLOAT
	                    , InUSDJISDOR	    MONEY
	                    , isHitJISDOR	    BIT   		
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpResult
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , KetUnderlying
                        , Purpose          
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 	
                        , m_pl_key
                        , nik_agent         
                    )
                    SELECT
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , strProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose           
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR    
                        , m_pl_key
                        , nik_agent         
                    FROM openxml(@nDocHandle, N'/Data/Results',2)           
                    WITH (
	                    branch			    VARCHAR(5)
	                    , customer_id	    VARCHAR(20)
	                    , [name]		    VARCHAR(20)
	                    , acc_id		    VARCHAR(20)
	                    , trx_branch	    VARCHAR(5)
	                    --, trx_date		    DATETIME
	                    , currency_code	    VARCHAR(5)
	                    , amount		    MONEY
	                    , rate			    FLOAT
	                    , InUSD			    MONEY
	                    , dealno		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    , isHit			    BIT
	                    --, ProcessTime	    DATETIME
	                    , underlying	    VARCHAR(15)
                        , ketUnderlying     VARCHAR(150)
                        , Purpose           VARCHAR(50)
	                    , JISDORRate	    FLOAT
	                    , InUSDJISDOR	    MONEY
	                    , isHitJISDOR	    BIT   	
                        , strTrx_date		VARCHAR(20)
                        , strProcessTime	VARCHAR(50)
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT    
                    )  
                    ";

            if (isDaily)
            {
                strQuery = strQuery + @"
--20240508, darul.wahid, HTR-214, begin
                    --DELETE FROM dbo.VLSResultFinal_TT
                    TRUNCATE TABLE dbo.VLSResultFinal_TT
--20240508, darul.wahid, HTR-214, end

                    INSERT INTO dbo.VLSResultFinal_TT
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent        
                    )
                    SELECT 
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , KetUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , nik_agent         
                    FROM #tmpResult";
            }
            else
            {
                strQuery = strQuery + @"                    
                    INSERT INTO dbo.VLSResultFinalToday_TT 
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent         
                    )
                    SELECT 
	                    tmp.branch
	                    , tmp.customer_id
	                    , tmp.[name]
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.dealno
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.isHit
	                    , tmp.ProcessTime
	                    , tmp.underlying
                        , tmp.KetUnderlying
                        , tmp.Purpose
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
	                    , tmp.isHitJISDOR 
                        , tmp.m_pl_key
                        , tmp.nik_agent         
                    FROM #tmpResult AS tmp
                    LEFT JOIN dbo.VLSResultFinalToday_TT AS tt
                    ON tmp.dealno = tt.dealno
                    WHERE tt.dealno is null";
            }
            #endregion
            
            return await this.InsertValasResultByQuery(results, strQuery);
        }

        public async Task<ApiMessage> EmptyDataValasToday()
        {
            ApiMessage truncateRs = new ApiMessage();

            try
            {
                #region Query
                string strQuery = @"
					--20240508, darul.wahid, HTR-214, begin
                    --DELETE FROM dbo.VLSTransactionsToday_TR
                    
                    --DELETE FROM dbo.VLSSummaryToday_TM

                    --DELETE FROM dbo.VLSResultFinalToday_TT

					--20241106, darul.wahid, ONFX-243, begin
                    INSERT INTO dbo.VLSTransactionsToday_TL
                    (
                        src			
                        , dealno		
                        , customer_id	
                        , acc_id		
                        , trx_branch	
                        , trx_date		
                        , trx_datetime	
                        , currency_code	
                        , amount		
                        , rate			
                        , InUSD			
                        , SourceKey		
                        , JISDORRate	
                        , InUSDJISDOR	
                        , m_pl_key		
                        , NIKAgent		
                        , GuidProcess	
                        , m_pl_key1		
                        , InsertedDate	
                    )
                    SELECT
                        src			
                        , dealno		
                        , customer_id	
                        , acc_id		
                        , trx_branch	
                        , trx_date		
                        , trx_datetime	
                        , currency_code	
                        , amount		
                        , rate			
                        , InUSD			
                        , SourceKey		
                        , JISDORRate	
                        , InUSDJISDOR	
                        , m_pl_key		
                        , NIKAgent		
                        , GuidProcess	
                        , m_pl_key1		
                        , WaktuMasukDeal	
                    FROM dbo.VLSTransactionsToday_TR
                    GO
                    --20241106, darul.wahid, ONFX-243, end

					TRUNCATE TABLE dbo.VLSTransactionsToday_TR
                    GO
                    TRUNCATE TABLE dbo.VLSSummaryToday_TM
					GO
                    TRUNCATE TABLE dbo.VLSResultFinalToday_TT
					GO
					--20240508, darul.wahid, HTR-214, begin                
					";

                #endregion              

                List<SqlParameter> sqlPar = new List<SqlParameter>();

                List<Task<DataSet>> listTruncate = new List<Task<DataSet>>();

                Task<DataSet> truncateSFXRs;
                Task<DataSet> truncateOblRs;
                Task<DataSet> truncateSibsRs;

                if (this._bInsert2ONFX)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    //truncateSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    string strExec = "";
                    
                    var mappingTableTrx = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableTrx = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.Transaction) && !x.IsDaily).FirstOrDefault();
                    mappingTableTrx = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX) 
                        && x.DataType.Equals(DataType.Transaction) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableTrx.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableTrx.TableName;
                        strExec += @"
                            --20241106, darul.wahid, ONFX-243, begin
                            INSERT INTO dbo.VLSTransactionsToday_TL
                            (
                                src			
                                , dealno		
                                , customer_id	
                                , acc_id		
                                , trx_branch	
                                , trx_date		
                                , trx_datetime	
                                , currency_code	
                                , amount		
                                , rate			
                                , InUSD			
                                , SourceKey		
                                , JISDORRate	
                                , InUSDJISDOR	
                                , m_pl_key		
                                , NIKAgent		
                                , GuidProcess	
                                , m_pl_key1		
                                , InsertedDate	
                            )
                            SELECT
                                src			
                                , dealno		
                                , customer_id	
                                , acc_id		
                                , trx_branch	
                                , trx_date		
                                , trx_datetime	
                                , currency_code	
                                , amount		
                                , rate			
                                , InUSD			
                                , SourceKey		
                                , JISDORRate	
                                , InUSDJISDOR	
                                , m_pl_key		
                                , NIKAgent		
                                , GuidProcess	
                                , m_pl_key1		
                                , WaktuMasukDeal	
                            FROM dbo." + mappingTableTrx.TableName + @"                            
                            --20241106, darul.wahid, ONFX-243, end

                            TRUNCATE TABLE dbo." + mappingTableTrx.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    var mappingTableSummary = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableSummary = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.Summary) && !x.IsDaily).FirstOrDefault();
                    mappingTableSummary = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX) 
                        && x.DataType.Equals(DataType.Summary) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableSummary.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableSummary.TableName;
                        strExec += @"
                            TRUNCATE TABLE dbo." + mappingTableSummary.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    var mappingTableResult = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableResult = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.ResultFinal) && !x.IsDaily).FirstOrDefault();
                    mappingTableResult = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableResult.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //      DELETE FROM dbo." + mappingTableResult.TableName;
                        strExec += @"
                            TRUNCATE TABLE dbo." + mappingTableResult.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    if(!string.IsNullOrEmpty(strExec))
                    {                        
                        truncateSFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExec, sqlPar));
                        //20231227, darul.wahid, HTR-214, end
                        listTruncate.Add(truncateSFXRs);
                        //20231227, darul.wahid, HTR-214, begin
                    }
                    //20231227, darul.wahid, HTR-214, end
                }

                if (this._bInsert2OBL)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    //truncateOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    string strExec = "";

                    var mappingTableTrx = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableTrx = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.Transaction) && !x.IsDaily).FirstOrDefault();
                    mappingTableTrx = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.Transaction) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableTrx.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableTrx.TableName;
                        strExec += @"
                            --20241106, darul.wahid, ONFX-243, begin
                            INSERT INTO dbo.VLSTransactionsToday_TL
                            (
                                src			
                                , dealno		
                                , customer_id	
                                , acc_id		
                                , trx_branch	
                                , trx_date		
                                , trx_datetime	
                                , currency_code	
                                , amount		
                                , rate			
                                , InUSD			
                                , SourceKey		
                                , JISDORRate	
                                , InUSDJISDOR	
                                , m_pl_key		
                                , NIKAgent		
                                , GuidProcess	
                                , m_pl_key1		
                                , InsertedDate	
                            )
                            SELECT
                                src			
                                , dealno		
                                , customer_id	
                                , acc_id		
                                , trx_branch	
                                , trx_date		
                                , trx_datetime	
                                , currency_code	
                                , amount		
                                , rate			
                                , InUSD			
                                , SourceKey		
                                , JISDORRate	
                                , InUSDJISDOR	
                                , m_pl_key		
                                , NIKAgent		
                                , GuidProcess	
                                , m_pl_key1		
                                , WaktuMasukDeal	
                            FROM dbo." + mappingTableTrx.TableName + @"                            
                            --20241106, darul.wahid, ONFX-243, end

                            TRUNCATE TABLE dbo." + mappingTableTrx.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    var mappingTableSummary = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableSummary = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.Summary) && !x.IsDaily).FirstOrDefault();
                    mappingTableSummary = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.Summary) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableSummary.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableSummary.TableName;
                        strExec += @"
                                TRUNCATE TABLE dbo." + mappingTableSummary.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    var mappingTableResult = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableResult = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.ResultFinal) && !x.IsDaily).FirstOrDefault();
                    mappingTableResult = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableResult.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableResult.TableName;
                        strExec += @"
                                TRUNCATE TABLE dbo." + mappingTableResult.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    if (!string.IsNullOrEmpty(strExec))
                    {
                        truncateOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExec, sqlPar));
                        //20231227, darul.wahid, HTR-214, end
                        listTruncate.Add(truncateOblRs);
                        //20231227, darul.wahid, HTR-214, begin
                    }
                    //20231227, darul.wahid, HTR-214, end
                }

                if (this._bInsert2SIBS)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    //truncateSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    string strExec = "";

                    var mappingTableTrx = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableTrx = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.Transaction) && !x.IsDaily).FirstOrDefault();
                    mappingTableTrx = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.Transaction) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableTrx.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableTrx.TableName;
                        strExec += @"
                            --20241106, darul.wahid, ONFX-243, begin
                            INSERT INTO dbo.VLSTransactionsToday_TL
                            (
                                src			
                                , dealno		
                                , customer_id	
                                , acc_id		
                                , trx_branch	
                                , trx_date		
                                , trx_datetime	
                                , currency_code	
                                , amount		
                                , rate			
                                , InUSD			
                                , SourceKey		
                                , JISDORRate	
                                , InUSDJISDOR	
                                , m_pl_key		
                                , NIKAgent		
                                , GuidProcess	
                                , m_pl_key1		
                                , InsertedDate	
                            )
                            SELECT
                                src			
                                , dealno		
                                , customer_id	
                                , acc_id		
                                , trx_branch	
                                , trx_date		
                                , trx_datetime	
                                , currency_code	
                                , amount		
                                , rate			
                                , InUSD			
                                , SourceKey		
                                , JISDORRate	
                                , InUSDJISDOR	
                                , m_pl_key		
                                , NIKAgent		
                                , GuidProcess	
                                , m_pl_key1		
                                , WaktuMasukDeal	
                            FROM dbo." + mappingTableTrx.TableName + @"                            
                            --20241106, darul.wahid, ONFX-243, end

                            TRUNCATE TABLE dbo." + mappingTableTrx.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    var mappingTableSummary = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableSummary = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.Summary) && !x.IsDaily).FirstOrDefault();
                    mappingTableSummary = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.Summary) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableSummary.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableSummary.TableName;
                        strExec += @"
                            TRUNCATE TABLE dbo." + mappingTableSummary.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    var mappingTableResult = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingTableResult = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.ResultFinal) && !x.IsDaily).FirstOrDefault();
                    mappingTableResult = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingTableResult.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += @"
                        //    DELETE FROM dbo." + mappingTableResult.TableName;
                        strExec += @"
                                TRUNCATE TABLE dbo." + mappingTableResult.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    if (!string.IsNullOrEmpty(strExec))
                    {
                        truncateSibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExec, sqlPar));
                        //20231227, darul.wahid, HTR-214, end
                        listTruncate.Add(truncateSibsRs);
                        //20231227, darul.wahid, HTR-214, begin
                    }
                    //20231227, darul.wahid, HTR-214, end
                }

                await Task.WhenAll(listTruncate);

                foreach (Task<DataSet> truncate in listTruncate)
                {
                    if (truncate.IsFaulted)
                        throw new Exception("Faulted: " + truncate.Exception.Message);

                    if (truncate.IsCanceled)
                        throw new Exception("Canceled: " + truncate.Exception.Message);
                }

                truncateRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                truncateRs.IsSuccess = false;
                truncateRs.ErrorDescription = "Gagal Truncate Data Transaction Today: " + ex.Message;
            }

            return truncateRs;
        }

        public async Task<ApiMessage> InsertValasTransactionHistory()
        {
            ApiMessage InsertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                DataSet dsOut = new DataSet();
                List<Task<DataSet>> listHistory = new List<Task<DataSet>>();

                Task<DataSet> historySFXRs;
                Task<DataSet> historyOblRs;
                Task<DataSet> historySibsRs;

                #region Query Populate Data
                string strQuery = @"
                    INSERT INTO dbo.VLSTransactions_TH
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , m_pl_key
	                    , NIKAgent
                    )
                    SELECT
	                    tr.src
	                    , tr.dealno
	                    , tr.customer_id
	                    , tr.acc_id
	                    , tr.trx_branch
	                    , tr.trx_date
	                    , tr.trx_datetime
	                    , tr.currency_code
	                    , tr.amount
	                    , tr.rate
	                    , tr.InUSD
	                    , tr.SourceKey
	                    , tr.JISDORRate
	                    , tr.InUSDJISDOR
	                    , tr.m_pl_key
	                    , tr.NIKAgent
                    FROM dbo.VLSTransactions_TR AS tr
                    LEFT JOIN dbo.VLSTransactions_TH AS th
                    ON tr.dealno = th.dealno
                    --20241106, darul.wahid, ONFX-243, begin
                        AND tr.trx_date = th.trx_date
                    --20241106, darul.wahid, ONFX-243, end
                    WHERE ISNULL(th.dealno, '') = ''

                    --Transaksi Today (yang kemarin sebelum dihapus)
                    INSERT INTO dbo.VLSTransactions_TH
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , m_pl_key
	                    , NIKAgent
                    )
                    SELECT
	                    tr.src
	                    , tr.dealno
	                    , tr.customer_id
	                    , tr.acc_id
	                    , tr.trx_branch
	                    , tr.trx_date
	                    , tr.trx_datetime
	                    , tr.currency_code
	                    , tr.amount
	                    , tr.rate
	                    , tr.InUSD
	                    , tr.SourceKey
	                    , tr.JISDORRate
	                    , tr.InUSDJISDOR
	                    , tr.m_pl_key
	                    , tr.NIKAgent
                    FROM dbo.VLSTransactionsToday_TR AS tr
                    LEFT JOIN dbo.VLSTransactions_TH AS th
                    ON tr.dealno = th.dealno
                    --20241106, darul.wahid, ONFX-243, begin
                        AND tr.trx_date = th.trx_date
                    --20241106, darul.wahid, ONFX-243, end                    
                    WHERE ISNULL(th.dealno, '') = ''

--20240508, darul.wahid, HTR - 214, begin
                    --DELETE FROM dbo.VLSTransactions_TR 
                    TRUNCATE TABLE dbo.VLSTransactions_TR 
--20240508, darul.wahid, HTR - 214, end ";
                #endregion

                if (this._bInsert2ONFX)
                {
                    historySFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listHistory.Add(historySFXRs);
                }

                if (this._bInsert2OBL)
                {
                    historyOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listHistory.Add(historyOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    historySibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listHistory.Add(historySibsRs);
                }

                await Task.WhenAll(listHistory);

                foreach (Task<DataSet> history in listHistory)
                {
                    if (history.IsFaulted)
                        throw new Exception("Faulted: " + history.Exception.Message);

                    if (history.IsCanceled)
                        throw new Exception("Canceled: " + history.Exception.Message);
                }

                InsertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                InsertRs.IsSuccess = false;
                InsertRs.ErrorDescription = "Error InsertValasTransactionHistory(): " + ex.Message;
            }
            finally
            {
                InsertRs.MessageDateTime = DateTime.Now;
            }

            return InsertRs;
        }

        public async Task<ApiMessage> InsertValasSummaryHistory()
        {
            ApiMessage InsertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                DataSet dsOut = new DataSet();
                List<Task<DataSet>> listHistory = new List<Task<DataSet>>();

                Task<DataSet> historySFXRs;
                Task<DataSet> historyOblRs;
                Task<DataSet> historySibsRs;

                #region Query Populate Data
                string strQuery = @"
                    INSERT INTO dbo.VLSSummary_TH
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , name
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    )
                    SELECT
	                    tm.customer_id
	                    , tm.InUSD
	                    , tm.branch
	                    , tm.name
	                    , tm.office_name
	                    , tm.npwp
	                    , tm.identity_1
	                    , tm.ProcessTime
	                    , tm.InUSDJISDOR
                    FROM dbo.VLSSummary_TM AS tm
                    LEFT JOIN dbo.VLSSummary_TH AS th
                    ON tm.customer_id = th.customer_id
	                    AND th.ProcessTime >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE())-1, 0)
                    WHERE ISNULL(th.customer_id, '') = ''

                    --Summary Data Today (hanya yang kemarin)
                    INSERT INTO dbo.VLSSummary_TH
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , name
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    )
                    SELECT
	                    tm.customer_id
	                    , tm.InUSD
	                    , tm.branch
	                    , tm.name
	                    , tm.office_name
	                    , tm.npwp
	                    , tm.identity_1
	                    , tm.ProcessTime
	                    , tm.InUSDJISDOR
                    FROM dbo.VLSSummaryToday_TM AS tm
                    LEFT JOIN dbo.VLSSummary_TH AS th
                    ON tm.customer_id = th.customer_id
	                    AND th.ProcessTime >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE())-1, 0)
                    WHERE ISNULL(th.customer_id, '') = ''

--20240508, darul.wahid, HTR - 214, begin
                    --DELETE FROM dbo.VLSSummary_TM 
                    TRUNCATE TABLE dbo.VLSSummary_TM 
--20240508, darul.wahid, HTR - 214, end 
                    
                    --20231218, yudha.n, ANT-361, begin
                    INSERT INTO dbo.VLSSummaryFXOption_TH
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , name
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    )
                    SELECT
	                    tm.customer_id
	                    , tm.InUSD
	                    , tm.branch
	                    , tm.name
	                    , tm.office_name
	                    , tm.npwp
	                    , tm.identity_1
	                    , tm.ProcessTime
	                    , tm.InUSDJISDOR
                    FROM dbo.VLSSummaryFXOption_TM AS tm
                    LEFT JOIN dbo.VLSSummaryFXOption_TH AS th
                    ON tm.customer_id = th.customer_id
	                    AND th.ProcessTime >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE())-1, 0)
                    WHERE ISNULL(th.customer_id, '') = ''

                    --Summary Data Today FXOption (hanya yang kemarin)
                    INSERT INTO dbo.VLSSummaryFXOption_TH
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , name
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                    )
                    SELECT
	                    tm.customer_id
	                    , tm.InUSD
	                    , tm.branch
	                    , tm.name
	                    , tm.office_name
	                    , tm.npwp
	                    , tm.identity_1
	                    , tm.ProcessTime
	                    , tm.InUSDJISDOR
                    FROM dbo.VLSSummaryTodayFXOption_TM AS tm
                    LEFT JOIN dbo.VLSSummaryFXOption_TH AS th
                    ON tm.customer_id = th.customer_id
	                    AND th.ProcessTime >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE())-1, 0)
                    WHERE ISNULL(th.customer_id, '') = ''

--20240508, darul.wahid, HTR - 214, begin
                    --DELETE FROM dbo.VLSSummaryFXOption_TM 
                    TRUNCATE TABLE dbo.VLSSummaryFXOption_TM 
--20240508, darul.wahid, HTR - 214, end
                    --20231218, yudha.n, ANT-361, end

                    --20250701, darul.wahid, ONFX-267, begin
                    INSERT INTO dbo.[summary_valas_hist]
                    (
                        [customer_id]		
                        , [product]			
                        , [buy_sell]			
                        , [total_in_usd]		
                        , [total_in_usd_jisdor]
                        , [branch]			
                        , [name]				
                        , [office_name]		
                        , [no_npwp]			
                        , [no_identity]		
                        , [process_time]		
                        , [guid_process]		
                    )
                    SELECT
                        tm.[customer_id]		
                        , tm.[product]			
                        , tm.[buy_sell]			
                        , tm.[total_in_usd]		
                        , tm.[total_in_usd_jisdor]
                        , tm.[branch]			
                        , tm.[name]				
                        , tm.[office_name]		
                        , tm.[no_npwp]			
                        , tm.[no_identity]		
                        , tm.[process_time]		
                        , tm.[guid_process]		
                    FROM dbo.[summary_valas] AS tm
                    LEFT JOIN dbo.[summary_valas_hist] AS th
                    ON tm.customer_id = th.customer_id
                        AND tm.[product] = th.[product]
                        AND tm.[buy_sell] = th.[buy_sell]
	                    AND th.process_time >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE())-1, 0)
                    WHERE ISNULL(th.customer_id, '') = ''

                    --Summary Data Today (hanya yang kemarin)
                    INSERT INTO dbo.summary_valas_hist
                    (
                        [customer_id]		
                        , [product]			
                        , [buy_sell]			
                        , [total_in_usd]		
                        , [total_in_usd_jisdor]
                        , [branch]			
                        , [name]				
                        , [office_name]		
                        , [no_npwp]			
                        , [no_identity]		
                        , [process_time]		
                        , [guid_process]		
                    )
                    SELECT
                        tm.[customer_id]		
                        , tm.[product]			
                        , tm.[buy_sell]			
                        , tm.[total_in_usd]		
                        , tm.[total_in_usd_jisdor]
                        , tm.[branch]			
                        , tm.[name]				
                        , tm.[office_name]		
                        , tm.[no_npwp]			
                        , tm.[no_identity]		
                        , tm.[process_time]		
                        , tm.[guid_process]		
                    FROM dbo.[summary_valas_today] AS tm
                    LEFT JOIN dbo.[summary_valas_hist] AS th
                    ON tm.customer_id = th.customer_id
                        AND tm.[product] = th.[product]
                        AND tm.[buy_sell] = th.[buy_sell]
	                    AND th.process_time >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE())-1, 0)
                    WHERE ISNULL(th.customer_id, '') = ''

                    TRUNCATE TABLE dbo.[summary_valas] 
                    --20250701, darul.wahid, ONFX-267, end
                    ";
                #endregion

                if (this._bInsert2ONFX)
                {
                    historySFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));
                    listHistory.Add(historySFXRs);
                }

                if (this._bInsert2OBL)
                {
                    historyOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar));
                    listHistory.Add(historyOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    historySibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));
                    listHistory.Add(historySibsRs);
                }

                await Task.WhenAll(listHistory);

                foreach (Task<DataSet> history in listHistory)
                {
                    if (history.IsFaulted)
                        throw new Exception("Faulted: " + history.Exception.Message);

                    if (history.IsCanceled)
                        throw new Exception("Canceled: " + history.Exception.Message);
                }

                InsertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                InsertRs.IsSuccess = false;
                InsertRs.ErrorDescription = "Error InsertValasSummaryHistory(): " + ex.Message;
            }
            finally
            {
                InsertRs.MessageDateTime = DateTime.Now;
            }

            return InsertRs;
        }

        public async Task<ApiMessage> InsertValasResultHistory()
        {
            ApiMessage InsertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();
                DataSet dsOut = new DataSet();
                List<Task<DataSet>> listHistory = new List<Task<DataSet>>();

                Task<DataSet> historySFXRs;
                Task<DataSet> historyOblRs;
                Task<DataSet> historySibsRs;

                #region Query Populate Data
                //20231227, darul.wahid, HTR-214, begin
                /*
                string strQuery = @"
                INSERT INTO dbo.VLSResultFinal_TH
                (
                 branch
                 , customer_id
                 , name
                 , acc_id
                 , trx_branch
                 , trx_date
                 , currency_code
                 , amount
                 , rate
                 , InUSD
                 , dealno
                 , office_name
                 , npwp
                 , identity_1
                 , isHit
                 , ProcessTime
                 , underlying
                 , JISDORRate
                 , InUSDJISDOR
                 , isHitJISDOR
                 , m_pl_key
                 , NIKAgent
                )
                SELECT
                 tt.branch
                 , tt.customer_id
                 , tt.name
                 , tt.acc_id
                 , tt.trx_branch
                 , tt.trx_date
                 , tt.currency_code
                 , tt.amount
                 , tt.rate
                 , tt.InUSD
                 , tt.dealno
                 , tt.office_name
                 , tt.npwp
                 , tt.identity_1
                 , tt.isHit
                 , tt.ProcessTime
                 , tt.underlying
                 , tt.JISDORRate
                 , tt.InUSDJISDOR
                 , tt.isHitJISDOR
                 , tt.m_pl_key
                 , tt.NIKAgent	
                FROM dbo.VLSResultFinal_TT AS tt
                LEFT JOIN dbo.VLSResultFinal_TH AS th
                ON tt.dealno = th.dealno
                WHERE ISNULL(th.dealno, '') = ''

                --Data Final Today (hanya yang kemarin saja)
                INSERT INTO dbo.VLSResultFinal_TH
                (
                 branch
                 , customer_id
                 , name
                 , acc_id
                 , trx_branch
                 , trx_date
                 , currency_code
                 , amount
                 , rate
                 , InUSD
                 , dealno
                 , office_name
                 , npwp
                 , identity_1
                 , isHit
                 , ProcessTime
                 , underlying
                 , JISDORRate
                 , InUSDJISDOR
                 , isHitJISDOR
                 , m_pl_key
                 , NIKAgent
                )
                SELECT
                 tt.branch
                 , tt.customer_id
                 , tt.name
                 , tt.acc_id
                 , tt.trx_branch
                 , tt.trx_date
                 , tt.currency_code
                 , tt.amount
                 , tt.rate
                 , tt.InUSD
                 , tt.dealno
                 , tt.office_name
                 , tt.npwp
                 , tt.identity_1
                 , tt.isHit
                 , tt.ProcessTime
                 , tt.underlying
                 , tt.JISDORRate
                 , tt.InUSDJISDOR
                 , tt.isHitJISDOR
                 , tt.m_pl_key
                 , tt.NIKAgent	
                FROM dbo.VLSResultFinalToday_TT AS tt
                LEFT JOIN dbo.VLSResultFinal_TH AS th
                ON tt.dealno = th.dealno
                WHERE ISNULL(th.dealno, '') = ''

                DELETE FROM dbo.VLSResultFinal_TT ";
                */
                string strQuery = @"
                    INSERT INTO dbo.VLSResultFinal_TH
                    (
	                    branch
	                    , customer_id
	                    , name
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR
	                    , m_pl_key
	                    , NIKAgent
                    )
                    SELECT
	                    tt.branch
	                    , tt.customer_id
	                    , tt.name
	                    , tt.acc_id
	                    , tt.trx_branch
	                    , tt.trx_date
	                    , tt.currency_code
	                    , tt.amount
	                    , tt.rate
	                    , tt.InUSD
	                    , tt.dealno
	                    , tt.office_name
	                    , tt.npwp
	                    , tt.identity_1
	                    , tt.isHit
	                    , tt.ProcessTime
	                    , tt.underlying
	                    , tt.JISDORRate
	                    , tt.InUSDJISDOR
	                    , tt.isHitJISDOR
	                    , tt.m_pl_key
	                    , tt.NIKAgent	
                    FROM dbo.[[tabelDaily]] AS tt
                    LEFT JOIN dbo.VLSResultFinal_TH AS th
                    ON tt.dealno = th.dealno
                    --20241106, darul.wahid, ONFX-243, begin
                        AND tt.trx_date = th.trx_date
                    --20241106, darul.wahid, ONFX-243, end                    
                    WHERE ISNULL(th.dealno, '') = ''

                    --Data Final Today (hanya yang kemarin saja)
                    INSERT INTO dbo.VLSResultFinal_TH
                    (
	                    branch
	                    , customer_id
	                    , name
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR
	                    , m_pl_key
	                    , NIKAgent
                    )
                    SELECT
	                    tt.branch
	                    , tt.customer_id
	                    , tt.name
	                    , tt.acc_id
	                    , tt.trx_branch
	                    , tt.trx_date
	                    , tt.currency_code
	                    , tt.amount
	                    , tt.rate
	                    , tt.InUSD
	                    , tt.dealno
	                    , tt.office_name
	                    , tt.npwp
	                    , tt.identity_1
	                    , tt.isHit
	                    , tt.ProcessTime
	                    , tt.underlying
	                    , tt.JISDORRate
	                    , tt.InUSDJISDOR
	                    , tt.isHitJISDOR
	                    , tt.m_pl_key
	                    , tt.NIKAgent	
                    FROM dbo.[[tabelToday]] AS tt
                    LEFT JOIN dbo.VLSResultFinal_TH AS th
                    ON tt.dealno = th.dealno
                    --20241106, darul.wahid, ONFX-243, begin
                        AND tt.trx_date = th.trx_date
                    --20241106, darul.wahid, ONFX-243, end
                    WHERE ISNULL(th.dealno, '') = ''

                    ";
                //20231227, darul.wahid, HTR-214, end
                #endregion

                if (this._bInsert2ONFX)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    //historySFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar));

                    string strExec = strQuery;
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingInsertTable.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += "DELETE FROM dbo." + mappingInsertTable.TableName;
                        strExec += "TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    //ganti nama tabel daily
                    MappingTableName tabelDaily = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //tabelDaily = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily).FirstOrDefault();
                    tabelDaily = this._globalVariable.MappingTables.Where(x =>
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX)
                        && x.DataType.Equals(DataType.ResultFinal)
                        && x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    strExec = strExec.Replace("[[tabelDaily]]", tabelDaily.TableName);

                    //ganti nama tabel today
                    MappingTableName tabelToday = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //tabelToday = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.ResultFinal) && !x.IsDaily).FirstOrDefault();
                    tabelToday = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    strExec = strExec.Replace("[[tabelToday]]", tabelToday.TableName);

                    historySFXRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExec, sqlPar));
                    //20231227, darul.wahid, HTR-214, end
                    listHistory.Add(historySFXRs);
                }

                if (this._bInsert2OBL)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    //historyOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)); 

                    string strExec = strQuery; 
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingInsertTable.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += "DELETE FROM dbo." + mappingInsertTable.TableName;
                        strExec += "TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    //ganti nama tabel daily
                    MappingTableName tabelDaily = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //tabelDaily = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily).FirstOrDefault();
                    tabelDaily = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    strExec = strExec.Replace("[[tabelDaily]]", tabelDaily.TableName);

                    //ganti nama tabel today
                    MappingTableName tabelToday = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //tabelToday = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.ResultFinal) && !x.IsDaily).FirstOrDefault();
                    tabelToday = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    strExec = strExec.Replace("[[tabelToday]]", tabelToday.TableName);

                    historyOblRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExec, sqlPar));
                    //20231227, darul.wahid, HTR-214, end
                    listHistory.Add(historyOblRs);
                }

                if (this._bInsert2SIBS)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    //historySibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar));

                    string strExec = strQuery; 
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    if (mappingInsertTable.ExecuteTable)
                        //20240508, darul.wahid, HTR - 214, begin
                        //strExec += "DELETE FROM dbo." + mappingInsertTable.TableName;
                        strExec += "TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                        //20240508, darul.wahid, HTR - 214, end

                    //ganti nama tabel daily
                    MappingTableName tabelDaily = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //tabelDaily = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily).FirstOrDefault();
                    tabelDaily = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    strExec = strExec.Replace("[[tabelDaily]]", tabelDaily.TableName);

                    //ganti nama tabel today
                    MappingTableName tabelToday = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //tabelToday = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.ResultFinal) && !x.IsDaily).FirstOrDefault();
                    tabelToday = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && !x.IsDaily).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end

                    strExec = strExec.Replace("[[tabelToday]]", tabelToday.TableName);

                    historySibsRs = Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExec, sqlPar));
                    //20231227, darul.wahid, HTR-214, end
                    listHistory.Add(historySibsRs);
                }

                await Task.WhenAll(listHistory);

                foreach (Task<DataSet> history in listHistory)
                {
                    if (history.IsFaulted)
                        throw new Exception("Faulted: " + history.Exception.Message);

                    if (history.IsCanceled)
                        throw new Exception("Canceled: " + history.Exception.Message);
                }

                InsertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                InsertRs.IsSuccess = false;
                InsertRs.ErrorDescription = "Error InsertValasResultHistory(): " + ex.Message;
            }
            finally
            {
                InsertRs.MessageDateTime = DateTime.Now;
            }

            return InsertRs;
        }

        public async Task<ApiMessage<List<VLSSummary>>> inquiryValasSummary(ApiMessage<List<VLSSummary>> trans)
        {
            ApiMessage<List<VLSSummary>> inqRs = new ApiMessage<List<VLSSummary>>();
            inqRs.copyHeaderForReply(trans);

            try
            {
                List<VLSSummary> dataInq = new List<VLSSummary>();
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSSummary>(trans.Data);

                var KeepColumn = new List<string> { "customer_id" };

                var toRemove = dtTran.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Except(KeepColumn).ToList();

                foreach (var col in toRemove) dtTran.Columns.Remove(col);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Summaries";

                string strXml = dsTran.GetXml();

                #region Query Populate Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
                    )
                    SELECT
	                    customer_id
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
                    )  

                    SELECT
	                    tm.customer_id
	                    , tm.InUSD
	                    , tm.branch
	                    , tm.[name]
	                    , tm.office_name
	                    , tm.npwp
	                    , tm.identity_1
	                    , tm.ProcessTime
	                    , tm.InUSDJISDOR	
                    FROM dbo.VLSSummary_TM AS tm
                    JOIN #tmpSummary AS tmp
                    ON tm.customer_id = tmp.customer_id
                    ";
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                //DataSet InqVLS = await this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar);
                DataSet InqVLS = await this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar);

                if (InqVLS.Tables.Count > 0)
                    dataInq = JsonConvert.DeserializeObject<List<VLSSummary>>(JsonConvert.SerializeObject(InqVLS.Tables[0]));


                inqRs.IsSuccess = true;
                inqRs.Data = dataInq;
            }
            catch (Exception ex)
            {
                inqRs.IsSuccess = false;
                inqRs.ErrorDescription = "Gagal Inquiry Valas Summary: " + ex.Message;
            }

            return inqRs;
        }

        public async Task<ApiMessage<List<VLSTransaction>>> UpdateInUSDRounding(ApiMessage<List<VLSTransaction>> trans)
        {
            ApiMessage<List<VLSTransaction>> updateRS = new ApiMessage<List<VLSTransaction>>();
            updateRS.Data = new List<VLSTransaction>();
            updateRS.copyHeaderForReply(trans);

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<VLSTransaction>(trans.Data);

                var KeepColumn = new List<string> { "dealno", "customer_id", "InUSDPrecise", "InUSDJISDORPrecise" };

                var toRemove = dtTran.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Except(KeepColumn).ToList();

                foreach (var col in toRemove) dtTran.Columns.Remove(col);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Trans";

                string strXml = dsTran.GetXml();

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                #region Query Update Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpTrans') IS NOT NULL
	                    DROP TABLE #tmpTrans

                    CREATE TABLE #tmpTrans                     
                    (    
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , trx_date			DATETIME
	                    , trx_datetime		DATETIME
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpTrans
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR	
                        , m_pl_key
                        , nik_agent
                    )
                    SELECT
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , strTrx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR		
                        , m_pl_key
                        , nik_agent         
                    FROM openxml(@nDocHandle, N'/Data/Trans',2)           
                    WITH (
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , strTrx_date		VARCHAR(20)
                        , strTrx_datetime	VARCHAR(50)
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                    )  ";

                
                #endregion

                var updateDbRs = await this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar);

                if (updateDbRs == null)
                    throw new Exception("Data Not Found");

                if (updateDbRs.Tables.Count > 0)
                    updateRS.Data = JsonConvert.DeserializeObject<List<VLSTransaction>>(JsonConvert.SerializeObject(updateDbRs.Tables[0]));

                updateRS.IsSuccess = true;
            }
            catch (Exception ex)
            {
                updateRS.IsSuccess = false;
                updateRS.ErrorDescription = "Gagal Update Transaction: " + ex.Message;
            }

            return updateRS;
        }
        
        private async Task<ApiMessage> DeleteDataValasTransaction()
        {
            ApiMessage insertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                //20240508, darul.wahid, HTR - 214, begin
                //string strQuery = @"DELETE FROM dbo.VLSTransactions_TR  ";
                string strQuery = @"TRUNCATE TABLE dbo.VLSTransactions_TR  ";
                //20240508, darul.wahid, HTR - 214, end

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                if (this._bInsert2ONFX)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));

                if (this._bInsert2OBL)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));

                if (this._bInsert2SIBS)
                     listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Transaction: " + ex.Message;
            }

            return insertRs;
        }

        private async Task<ApiMessage> DeleteDataValasSummary()
        {
            ApiMessage insertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();
                
                //20240508, darul.wahid, HTR - 214, begin
                //string strQuery = @" DELETE FROM dbo.VLSSummary_TM  ";
                string strQuery = @" TRUNCATE TABLE dbo.VLSSummary_TM  ";
                //20240508, darul.wahid, HTR - 214, end

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                if (this._bInsert2ONFX)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));

                if (this._bInsert2OBL)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));

                if (this._bInsert2SIBS)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Summary Transaction: " + ex.Message;
            }

            return insertRs;
        }

        //20231218, yudha.n, ANT-361, begin
        private async Task<ApiMessage> DeleteDataValasSummaryFXOption()
        {
            ApiMessage insertRs = new();

            try
            {
                List<SqlParameter> sqlPar = new();

                DataSet dsTran = new();
                DataTable dtTran = new();

                //20240508, darul.wahid, HTR - 214, begin
                //string strQuery = @"DELETE FROM dbo.VLSSummaryFXOption_TM ";
                string strQuery = @"TRUNCATE TABLE dbo.VLSSummaryFXOption_TM ";
                //20240508, darul.wahid, HTR - 214, end

                List<Task<DataSet>> listInsert = new();

                if (this._bInsert2ONFX)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));

                if (this._bInsert2OBL)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));

                if (this._bInsert2SIBS)
                    listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Summary Transaction: " + ex.Message;
            }

            return insertRs;
        }
        //20231218, yudha.n, ANT-361, end

        private async Task<ApiMessage> DeleteDataValasResult()
        {
            ApiMessage insertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strQuery = @"DELETE FROM dbo.VLSResultFinal_TT  ";

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                if (this._bInsert2ONFX)
                //20231227, darul.wahid, HTR-214, begin
                //listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end
                    //20240508, darul.wahid, HTR - 214, begin
                    //string strExec = @"DELETE FROM dbo." + mappingInsertTable.TableName;
                    string strExec = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                    //20240508, darul.wahid, HTR - 214, end
                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExec, sqlPar)));
                }
                //20231227, darul.wahid, HTR-214, end

                if (this._bInsert2OBL)
                //20231227, darul.wahid, HTR-214, begin
                //listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end
                    //20240508, darul.wahid, HTR - 214, begin
                    //string strExec = @"DELETE FROM dbo." + mappingInsertTable.TableName;
                    string strExec = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                    //20240508, darul.wahid, HTR - 214, end
                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExec, sqlPar)));
                }
                //20231227, darul.wahid, HTR-214, end

                if (this._bInsert2SIBS)
                //20231227, darul.wahid, HTR-214, begin
                //listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end
                    //20240508, darul.wahid, HTR - 214, begin
                    //string strExec = @"DELETE FROM dbo." + mappingInsertTable.TableName;
                    string strExec = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                    //20240508, darul.wahid, HTR - 214, end
                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExec, sqlPar)));
                }
                //20231227, darul.wahid, HTR-214, end

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Final Result: " + ex.Message;
            }

            return insertRs;
        }
        
        public async Task<ApiMessage> InsertValasTransactionV2(ApiMessage<List<VLSTransaction>> trans, bool isDaily, string guidProcess)
        {            
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(trans);

            try
            {
                if (isDaily)
                {
                    var deleteTran = await this.DeleteDataValasTransaction();

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "",
                    transactionTodayTableName = "VLSTransactionsToday_TR";
                
                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpTrans') IS NOT NULL
	                    DROP TABLE #tmpTrans

                    CREATE TABLE #tmpTrans                     
                    (    
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , trx_date			DATETIME
	                    , trx_datetime		DATETIME
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                        , m_pl_key1         VARCHAR(38)
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpTrans
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR	
                        , m_pl_key
                        , nik_agent
                        , m_pl_key1         
                    )
                    SELECT
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , strTrx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , ISNULL(SourceKey, '')
	                    , JISDORRate
	                    , InUSDJISDOR		
                        , m_pl_key
                        , nik_agent     
                        , ISNULL(m_pl_key1, '')         
                    FROM openxml(@nDocHandle, N'/Data/Trans',2)           
                    WITH (
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , strTrx_date		VARCHAR(20)
                        , strTrx_datetime	VARCHAR(50)
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                        , m_pl_key1         VARCHAR(38)
                    )  ";

                if (isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.VLSTransactions_TR
                    (
                        src
                        , dealno
                        , customer_id
                        , acc_id
                        , trx_branch
                        , trx_date
                        , trx_datetime
                        , currency_code
                        , amount
                        , rate
                        , InUSD
                        , SourceKey
                        , JISDORRate
                        , InUSDJISDOR
                        , m_pl_key
                        , NIKAgent       
                        , GuidProcess
                        , m_pl_key1         
                    )
                    SELECT
                        tmp.src
	                    , tmp.dealno
	                    , tmp.customer_id
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.trx_datetime
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.SourceKey
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
                        , tmp.m_pl_key
                        , tmp.nik_agent  
                        , @pcGuidProcess
                        , tmp.m_pl_key1         
                    FROM #tmpTrans AS tmp
                    LEFT JOIN dbo.VLSTransactions_TR AS tr
                    ON tmp.dealno = tr.dealno
                        AND tmp.customer_id = tr.customer_id
                    WHERE tr.dealno is null ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tr  
                        SET  amount = tmp.amount
                            , rate = tmp.rate
                            , InUSD = tmp.InUSD
                            , JISDORRate = tmp.JISDORRate
                            , InUSDJISDOR = tmp.InUSDJISDOR
                            , m_pl_key = tmp.m_pl_key
                            , m_pl_key1 = tmp.m_pl_key1
                    FROM dbo.VLSTransactionsToday_TR AS tr
                    JOIN #tmpTrans AS tmp
                    ON tmp.dealno = tr.dealno
                        AND tr.customer_id = tmp.customer_id
                    WHERE 1 = 1
                        AND
                        (
                            tr.amount != tmp.amount
                            OR tr.rate != tmp.rate
                            OR tr.InUSD != tmp.InUSD
                            OR tr.JISDORRate != tmp.JISDORRate
                            OR tr.InUSDJISDOR != tmp.InUSDJISDOR
                            OR tr.m_pl_key != tmp.m_pl_key
                            OR ISNULL(tr.m_pl_key1, '') != tmp.m_pl_key1
                        )                     

                    INSERT INTO dbo.VLSTransactionsToday_TR
                    (
                        src
                        , dealno
                        , customer_id
                        , acc_id
                        , trx_branch
                        , trx_date
                        , trx_datetime
                        , currency_code
                        , amount
                        , rate
                        , InUSD
                        , SourceKey
                        , JISDORRate
                        , InUSDJISDOR
                        , m_pl_key
                        , NIKAgent  
                        , GuidProcess
                        , m_pl_key1         
                    )
                    SELECT
                        tmp.src
	                    , tmp.dealno
	                    , tmp.customer_id
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.trx_datetime
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.SourceKey
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
                        , tmp.m_pl_key
                        , tmp.nik_agent  
                        , @pcGuidProcess
                        , tmp.m_pl_key1         
                    FROM #tmpTrans AS tmp
                    LEFT JOIN dbo.VLSTransactionsToday_TR AS tr
                    ON tmp.dealno = tr.dealno
                    WHERE tr.dealno is null

                    UPDATE tr  
                        SET  GuidProcess = @pcGuidProcess
                    FROM dbo.VLSTransactionsToday_TR AS tr
                    JOIN #tmpTrans AS tmp
                    ON tmp.dealno = tr.dealno
                        AND tr.customer_id = tmp.customer_id
                    WHERE 1 = 1
                        AND ISNULL(tr.GuidProcess, 0x0) != @pcGuidProcess ";
                }
                #endregion

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();
                List<Task<ApiMessage>> listDeleteToday = new List<Task<ApiMessage>>();

                List<List<VLSTransaction>> dataToInsert = new List<List<VLSTransaction>>();
                List<List<VLSTransaction>> unParitioned = new List<List<VLSTransaction>>();
                List<List<VLSTransaction>> partitioned = new List<List<VLSTransaction>>();

                int maxIndex = trans.Data.Count();

                unParitioned = clsUtils.SplitList(trans.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(trans.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSTransaction>>();
                    dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSTransaction> data in dataToInsert)
                    {
                        if (this._connSFX.isDBNISP)
                        { 
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSTransaction>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Trans";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(transactionTodayTableName, guidProcess, this._connSFX)));
                }

                if (this._bInsert2OBL)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSTransaction>>();
                    dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSTransaction> data in dataToInsert)
                    {
                        if (this._connObli.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSTransaction>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Trans";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(transactionTodayTableName, guidProcess, this._connObli)));
                }

                if (this._bInsert2SIBS)
                {
                    
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSTransaction>>();
                    dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSTransaction> data in dataToInsert)
                    {
                        if (this._connSibs.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();
                        
                        dtTran = this._msHelper.MapListToTable<VLSTransaction>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Trans";

                        strXml = dsTran.GetXml();
                        
                        sqlPar[0].Value = strXml;
                        
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));

                    }

                    if(listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }                        
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(transactionTodayTableName, guidProcess, this._connSibs)));
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if (!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Transaction: " + ex.Message;
            }

            return insertRs;
        }

        public async Task<ApiMessage> InsertValasSummaryV2(ApiMessage<List<VLSSummary>> sumaries, bool isDaily, string guidProcess)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(sumaries);

            try
            {
                if (isDaily)
                {
                    var deleteTran = await this.DeleteDataValasSummary();

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "",
                    summaryTodayTableName = "VLSSummaryToday_TM";

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
	                    , InUSD			MONEY
	                    , branch		VARCHAR(5)
	                    , [name]		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , ProcessTime	DATETIME
	                    , InUSDJISDOR	MONEY    		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR							
                    )
                    SELECT
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , strProcessTime
	                    , InUSDJISDOR	        			
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
	                    , InUSD			    MONEY
	                    , branch		    VARCHAR(5)
	                    , [name]		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    --, ProcessTime	    DATETIME
                        , strProcessTime	VARCHAR(50)	                    
                        , InUSDJISDOR	    MONEY    	          			
                    )  

                    ";

                if (isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.VLSSummary_TM
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.customer_id
	                    , tmp.InUSD
	                    , tmp.branch
	                    , tmp.[name]
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.ProcessTime
	                    , tmp.InUSDJISDOR
                        , @pcGuidProcess
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.VLSSummary_TM AS tm
                    ON tmp.customer_id = tm.customer_id
                    WHERE ISNULL(tm.customer_id, '') = ''
                    ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tm
                    SET InUSD = new.InUSD
                        , InUSDJISDOR = new.InUSDJISDOR                        
                    FROM dbo.VLSSummaryToday_TM AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id  
                    WHERE 1 = 1
                        AND
                        (
                            tm.InUSD != new.InUSD
                            OR tm.InUSDJISDOR != new.InUSDJISDOR
                        )

                    INSERT INTO dbo.VLSSummaryToday_TM
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.customer_id
	                    , tmp.InUSD
	                    , tmp.branch
	                    , tmp.[name]
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.ProcessTime
	                    , tmp.InUSDJISDOR
                        , @pcGuidProcess
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.VLSSummaryToday_TM AS tm
                    ON tmp.customer_id = tm.customer_id
                    WHERE tm.customer_id is null 

                    UPDATE tm
                    SET GuidProcess = @pcGuidProcess
                    FROM dbo.VLSSummaryToday_TM AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id 
                    WHERE ISNULL(tm.GuidProcess, 0x0) != @pcGuidProcess ";
                }
                #endregion

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();
                List<Task<ApiMessage>> listDeleteToday = new List<Task<ApiMessage>>();

                List<List<VLSSummary>> dataToInsert = new List<List<VLSSummary>>();
                List<List<VLSSummary>> unParitioned = new List<List<VLSSummary>>();
                List<List<VLSSummary>> partitioned = new List<List<VLSSummary>>();

                int maxIndex = sumaries.Data.Count();

                unParitioned = clsUtils.SplitList(sumaries.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(sumaries.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSSummary>>();
                    dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSSummary> data in dataToInsert)
                    {
                        if (this._connSFX.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Summaries";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(summaryTodayTableName, guidProcess, this._connSFX)));
                }

                if (this._bInsert2OBL)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSSummary>>();
                    dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSSummary> data in dataToInsert)
                    {
                        if (this._connObli.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Summaries";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(summaryTodayTableName, guidProcess, this._connObli)));
                }

                if (this._bInsert2SIBS)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSSummary>>();
                    dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSSummary> data in dataToInsert)
                    {
                        if (this._connSibs.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Summaries";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(summaryTodayTableName, guidProcess, this._connSibs)));
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if (!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Summary: " + ex.Message;
            }

            return insertRs;
        }

        public async Task<ApiMessage> InsertValasResultV2(ApiMessage<List<VLSResultFinal>> results, bool isDaily, string guidProcess)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(results);

            try
            {
                if (isDaily)
                {
                    var deleteTran = await this.DeleteDataValasResult();

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "",
                    resultTodayTableName = "VLSResultFinalToday_TT";

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpResult') IS NOT NULL
	                    DROP TABLE #tmpResult

                    CREATE TABLE #tmpResult                     
                    (    
	                    branch			    VARCHAR(5)
	                    , customer_id	    VARCHAR(20)
	                    , [name]		    VARCHAR(20)
	                    , acc_id		    VARCHAR(20)
	                    , trx_branch	    VARCHAR(5)
	                    , trx_date		    DATETIME
	                    , currency_code	    VARCHAR(5)
	                    , amount		    MONEY
	                    , rate			    FLOAT
	                    , InUSD			    MONEY
	                    , dealno		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    , isHit			    BIT
	                    , ProcessTime	    DATETIME
	                    , underlying	    VARCHAR(15)
                        , KetUnderlying	    VARCHAR(150)
                        , Purpose           VARCHAR(50)
	                    , JISDORRate	    FLOAT
	                    , InUSDJISDOR	    MONEY
	                    , isHitJISDOR	    BIT   		
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpResult
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , KetUnderlying
                        , Purpose          
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 	
                        , m_pl_key
                        , nik_agent         
                    )
                    SELECT
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , ISNULL(npwp, '')
	                    , identity_1
	                    , isHit
	                    , strProcessTime
	                    , ISNULL(underlying, '')
                        , ISNULL(ketUnderlying, '')
                        , ISNULL(Purpose, '')           
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR    
                        , m_pl_key
                        , nik_agent         
                    FROM openxml(@nDocHandle, N'/Data/Results',2)           
                    WITH (
	                    branch			    VARCHAR(5)
	                    , customer_id	    VARCHAR(20)
	                    , [name]		    VARCHAR(20)
	                    , acc_id		    VARCHAR(20)
	                    , trx_branch	    VARCHAR(5)
	                    --, trx_date		    DATETIME
	                    , currency_code	    VARCHAR(5)
	                    , amount		    MONEY
	                    , rate			    FLOAT
	                    , InUSD			    MONEY
	                    , dealno		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    , isHit			    BIT
	                    --, ProcessTime	    DATETIME
	                    , underlying	    VARCHAR(15)
                        , ketUnderlying     VARCHAR(150)
                        , Purpose           VARCHAR(50)
	                    , JISDORRate	    FLOAT
	                    , InUSDJISDOR	    MONEY
	                    , isHitJISDOR	    BIT   	
                        , strTrx_date		VARCHAR(20)
                        , strProcessTime	VARCHAR(50)
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT    
                    )  
                    ";

                //20231227, darul.wahid, HTR-214, begin
                /*
                if (isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.VLSResultFinal_TT
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent        
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.branch
	                    , tmp.customer_id
	                    , tmp.[name]
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.dealno
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.isHit
	                    , tmp.ProcessTime
	                    , tmp.underlying
                        , tmp.KetUnderlying
                        , tmp.Purpose
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
	                    , tmp.isHitJISDOR 
                        , tmp.m_pl_key
                        , tmp.nik_agent      
                        , @pcGuidProcess
                    FROM #tmpResult AS tmp
                    LEFT JOIN dbo.VLSResultFinal_TT AS tt
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE ISNULL(tt.dealno, '') = ''
                    ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tt
                        SET  amount = tmp.amount
                            , rate = tmp.rate
                            , InUSD = tmp.InUSD
                            , isHit = tmp.isHit
                            , JISDORRate = tmp.JISDORRate
                            , InUSDJISDOR = tmp.InUSDJISDOR
                            , isHitJISDOR = tmp.isHitJISDOR
                    FROM dbo.VLSResultFinalToday_TT AS tt
                    JOIN #tmpResult AS tmp
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE 1 = 1
                        AND
                        (
                            tmp.amount != tt.amount
                            OR tmp.rate != tt.rate
                            OR tmp.InUSD != tt.InUSD
                            OR tmp.isHit != tt.isHit
                            OR tmp.JISDORRate != tt.JISDORRate
                            OR tmp.InUSDJISDOR != tt.InUSDJISDOR
                            OR tmp.isHitJISDOR != tt.isHitJISDOR
                        )

                    INSERT INTO dbo.VLSResultFinalToday_TT 
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent         
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.branch
	                    , tmp.customer_id
	                    , tmp.[name]
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.dealno
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.isHit
	                    , tmp.ProcessTime
	                    , tmp.underlying
                        , tmp.KetUnderlying
                        , tmp.Purpose
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
	                    , tmp.isHitJISDOR 
                        , tmp.m_pl_key
                        , tmp.nik_agent  
                        , @pcGuidProcess
                    FROM #tmpResult AS tmp
                    LEFT JOIN dbo.VLSResultFinalToday_TT AS tt
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE tt.dealno is null 

                    UPDATE tt
                        SET GuidProcess = @pcGuidProcess                    
                    FROM dbo.VLSResultFinalToday_TT AS tt
                    JOIN #tmpResult AS tmp
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE ISNULL(tt.GuidProcess, 0x0) != @pcGuidProcess ";
                
                }
                */
                if (isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.[[TableName]]
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent        
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.branch
	                    , tmp.customer_id
	                    , tmp.[name]
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.dealno
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.isHit
	                    , tmp.ProcessTime
	                    , tmp.underlying
                        , tmp.KetUnderlying
                        , tmp.Purpose
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
	                    , tmp.isHitJISDOR 
                        , tmp.m_pl_key
                        , tmp.nik_agent      
                        , @pcGuidProcess
                    FROM #tmpResult AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tt
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE ISNULL(tt.dealno, '') = ''
                    ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tt
                        SET  amount = tmp.amount
                            , rate = tmp.rate
                            , InUSD = tmp.InUSD
                            , isHit = tmp.isHit
                            , JISDORRate = tmp.JISDORRate
                            , InUSDJISDOR = tmp.InUSDJISDOR
                            , isHitJISDOR = tmp.isHitJISDOR
                    FROM dbo.[[TableName]] AS tt
                    JOIN #tmpResult AS tmp
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE 1 = 1
                        AND
                        (
                            tmp.amount != tt.amount
                            OR tmp.rate != tt.rate
                            OR tmp.InUSD != tt.InUSD
                            OR tmp.isHit != tt.isHit
                            OR tmp.JISDORRate != tt.JISDORRate
                            OR tmp.InUSDJISDOR != tt.InUSDJISDOR
                            OR tmp.isHitJISDOR != tt.isHitJISDOR
                        )

                    INSERT INTO dbo.[[TableName]] 
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent         
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.branch
	                    , tmp.customer_id
	                    , tmp.[name]
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.dealno
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.isHit
	                    , tmp.ProcessTime
	                    , tmp.underlying
                        , tmp.KetUnderlying
                        , tmp.Purpose
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
	                    , tmp.isHitJISDOR 
                        , tmp.m_pl_key
                        , tmp.nik_agent  
                        , @pcGuidProcess
                    FROM #tmpResult AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tt
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE tt.dealno is null 

                    UPDATE tt
                        SET GuidProcess = @pcGuidProcess                    
                    FROM dbo.[[TableName]] AS tt
                    JOIN #tmpResult AS tmp
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE ISNULL(tt.GuidProcess, 0x0) != @pcGuidProcess ";

                }
                //20231227, darul.wahid, HTR-214, begin
                #endregion

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();
                List<Task<ApiMessage>> listDeleteToday = new List<Task<ApiMessage>>();

                List<List<VLSResultFinal>> dataToInsert = new List<List<VLSResultFinal>>();
                List<List<VLSResultFinal>> unParitioned = new List<List<VLSResultFinal>>();
                List<List<VLSResultFinal>> partitioned = new List<List<VLSResultFinal>>();

                int maxIndex = results.Data.Count();

                unParitioned = clsUtils.SplitList(results.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(results.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_OnlineFX") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end
                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExec = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strResultTodayTableName = mappingInsertTable.TableName;
                        //20231227, darul.wahid, HTR-214, end
                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSResultFinal>>();
                        dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSResultFinal> data in dataToInsert)
                        {
                            if (this._connSFX.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSResultFinal>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Results";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            //20231227, darul.wahid, HTR-214, begin
                            //listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));
                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExec, sqlPar)));
                            //20231227, darul.wahid, HTR-214, end
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if(!isDaily)
                            //20231227, darul.wahid, HTR-214, begin
                            //listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(resultTodayTableName, guidProcess, this._connSFX)));
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strResultTodayTableName, guidProcess, this._connSFX)));

                    }
                    //20231227, darul.wahid, HTR-214, end
                }

                if (this._bInsert2OBL)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_TRSRETAIL") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end
                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExec = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strResultTodayTableName = mappingInsertTable.TableName;
                        //20231227, darul.wahid, HTR-214, end
                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSResultFinal>>();
                        dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSResultFinal> data in dataToInsert)
                        {
                            if (this._connObli.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSResultFinal>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Results";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            //20231227, darul.wahid, HTR-214, begin
                            //listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));
                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExec, sqlPar)));
                            //20231227, darul.wahid, HTR-214, end
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!isDaily)
                            //20231227, darul.wahid, HTR-214, begin
                            //listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(resultTodayTableName, guidProcess, this._connObli)));
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strResultTodayTableName, guidProcess, this._connObli)));

                    }
                    //20231227, darul.wahid, HTR-214, end
                }

                if (this._bInsert2SIBS)
                {
                    //20231227, darul.wahid, HTR-214, begin
                    MappingTableName mappingInsertTable = new MappingTableName();
                    //20240826, darul.wahid, RFR-54578, begin
                    //mappingInsertTable = this._globalVariable.MappingTables.Where(x => x.DataBaseName.Equals("SQL_SIBS") && x.DataType.Equals(DataType.ResultFinal) && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        x.DataBaseName.Equals(DatabaseName.SQL_SIBS) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(IsDaily)).FirstOrDefault();
                    //20240826, darul.wahid, RFR-54578, end
                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExec = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strResultTodayTableName = mappingInsertTable.TableName;
                        //20231227, darul.wahid, HTR-214, end
                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSResultFinal>>();
                        dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSResultFinal> data in dataToInsert)
                        {
                            if (this._connSibs.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSResultFinal>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Results";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            //20231227, darul.wahid, HTR-214, begin
                            //listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));
                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExec, sqlPar)));
                            //20231227, darul.wahid, HTR-214, end
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!isDaily)
                            //20231227, darul.wahid, HTR-214, begin
                            //listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(resultTodayTableName, guidProcess, this._connSibs)));
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strResultTodayTableName, guidProcess, this._connSibs)));

                    }
                    //20231227, darul.wahid, HTR-214, end
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if(!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Result: " + ex.Message;
            }

            return insertRs;
        }

        public async Task<ApiMessage> InsertValasTransactionEachRow(ApiMessage<List<VLSTransaction>> trans, bool isDaily)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(trans);

            try
            {
                if (isDaily)
                {
                    var deleteTran = await this.DeleteDataValasTransaction();

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "";

                string strTableName = isDaily ? "VLSTransactions_TR" : "VLSTransactionsToday_TR";

                #region Query Insert Data
                string strQuery = @"
                    IF NOT EXISTS (
		                    SELECT TOP 1 1 
		                    FROM dbo." + strTableName;
                    strQuery += @" WHERE dealno = @dealno AND customer_id = @customer_id
		                    )
                    BEGIN
	                    INSERT INTO dbo." + strTableName;
                                    strQuery += @" 
	                        (
		                        src
                                , dealno
                                , customer_id
                                , acc_id
                                , trx_branch
                                , trx_date
                                , trx_datetime
                                , currency_code
                                , amount
                                , rate
                                , InUSD
                                , SourceKey
                                , JISDORRate
                                , InUSDJISDOR
                                , m_pl_key
                                , NIKAgent
	                        )
                        SELECT @src
                            , @dealno
                            , @customer_id
                            , @acc_id
                            , @trx_branch
                            , @trx_date
                            , @trx_datetime
                            , @currency_code
                            , @amount
                            , @rate
                            , @InUSD
                            , @SourceKey
                            , @JISDORRate
                            , @InUSDJISDOR
                            , @m_pl_key
                            , @nik_agent
                    END ";                
                #endregion

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                List<List<VLSTransaction>> dataToInsert = new List<List<VLSTransaction>>();
                List<List<VLSTransaction>> unParitioned = new List<List<VLSTransaction>>();
                List<List<VLSTransaction>> partitioned = new List<List<VLSTransaction>>();

                int maxIndex = trans.Data.Count();

                unParitioned = clsUtils.SplitList(trans.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(trans.Data, maxIndex);
                }

                List<Task<ApiMessage>> listInsertt = new List<Task<ApiMessage>>();
                if (this._bInsert2ONFX)
                {
                    listInsertt = new List<Task<ApiMessage>>();
                    foreach (var item in trans.Data)
                    {
                        object Param = new
                        {
                            item.src,
                            item.dealno,
                            item.customer_id,
                            item.acc_id,
                            item.trx_branch,
                            item.trx_date,
                            item.trx_datetime,
                            item.currency_code,
                            item.amount,
                            item.rate,
                            item.InUSD,
                            item.SourceKey,
                            item.JISDORRate,
                            item.InUSDJISDOR,
                            item.m_pl_key,
                            item.nik_agent
                        };

                        listInsertt.Add(Task.Run(() => this._connDB.ExecuteApiMsg(this._connSFX, strQuery, Param)));

                    }
                    
                    await Task.WhenAll(listInsertt);

                    foreach (var insert in listInsertt)
                    {
                        if (insert.IsFaulted)
                            throw new Exception("Faulted: " + insert.Exception.Message);

                        if (insert.IsCanceled)
                            throw new Exception("Canceled: " + insert.Exception.Message);
                        
                    }
                }

                if (this._bInsert2OBL)
                {
                    listInsertt = new List<Task<ApiMessage>>();
                    foreach (var item in trans.Data)
                    {
                        object Param = new
                        {
                            item.src,
                            item.dealno,
                            item.customer_id,
                            item.acc_id,
                            item.trx_branch,
                            item.trx_date,
                            item.trx_datetime,
                            item.currency_code,
                            item.amount,
                            item.rate,
                            item.InUSD,
                            item.SourceKey,
                            item.JISDORRate,
                            item.InUSDJISDOR,
                            item.m_pl_key,
                            item.nik_agent
                        };

                        listInsertt.Add(Task.Run(() => this._connDB.ExecuteApiMsg(this._connObli, strQuery, Param)));

                    }

                    await Task.WhenAll(listInsertt);

                    foreach (var insert in listInsertt)
                    {
                        if (insert.IsFaulted)
                            throw new Exception("Faulted: " + insert.Exception.Message);

                        if (insert.IsCanceled)
                            throw new Exception("Canceled: " + insert.Exception.Message);
                    }

                }

                if (this._bInsert2SIBS)
                {
                    listInsertt = new List<Task<ApiMessage>>();
                    foreach (var item in trans.Data)
                    {
                        object Param = new
                        {
                            item.src,
                            item.dealno,
                            item.customer_id,
                            item.acc_id,
                            item.trx_branch,
                            item.trx_date,
                            item.trx_datetime,
                            item.currency_code,
                            item.amount,
                            item.rate,
                            item.InUSD,
                            item.SourceKey,
                            item.JISDORRate,
                            item.InUSDJISDOR,
                            item.m_pl_key,
                            item.nik_agent
                        };

                        listInsertt.Add(Task.Run(() => this._connDB.ExecuteApiMsg(this._connSibs, strQuery, Param)));
                    }

                    await Task.WhenAll(listInsertt);

                    foreach (var insert in listInsertt)
                    {
                        if (insert.IsFaulted)
                            throw new Exception("Faulted: " + insert.Exception.Message);

                        if (insert.IsCanceled)
                            throw new Exception("Canceled: " + insert.Exception.Message);

                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Transaction: " + ex.Message;
            }

            return insertRs;
        }

        //20250701, darul.wahid, ONFX-267, begin
        private async Task<ApiMessage> DeleteDataNotInProcess(string tableName, string keyColumnName, string strGuid, ConnectionProperties connProps)
        {
            ApiMessage deleteRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "";

                #region Query Insert Data
                //20241106, darul.wahid, ONFX - 243, begin
                //string strQuery = @"       
                //    DELETE 
                //    FROM dbo." + tableName;
                string strQuery = @"";
                if (tableName.Equals("VLSTransactionsToday_TR"))
                {
                    strQuery += @"
                        INSERT INTO dbo.VLSTransactionsToday_TL
                        (
                            src			
                            , dealno		
                            , customer_id	
                            , acc_id		
                            , trx_branch	
                            , trx_date		
                            , trx_datetime	
                            , currency_code	
                            , amount		
                            , rate			
                            , InUSD			
                            , SourceKey		
                            , JISDORRate	
                            , InUSDJISDOR	
                            , m_pl_key		
                            , NIKAgent		
                            , GuidProcess	
                            , m_pl_key1		
                            , InsertedDate	
                        )
                        SELECT
                            src			
                            , dealno		
                            , customer_id	
                            , acc_id		
                            , trx_branch	
                            , trx_date		
                            , trx_datetime	
                            , currency_code	
                            , amount		
                            , rate			
                            , InUSD			
                            , SourceKey		
                            , JISDORRate	
                            , InUSDJISDOR	
                            , m_pl_key		
                            , NIKAgent		
                            , GuidProcess	
                            , m_pl_key1		
                            , WaktuMasukDeal	
                        FROM dbo.VLSTransactionsToday_TR
                        WHERE ISNULL(GuidProcess, 0x0) != '" + strGuid + @"'";
                }
                strQuery += @"       
                    DELETE 
                    FROM dbo." + tableName;
                //20241106, darul.wahid, ONFX - 243, end

                strQuery += @" WHERE ISNULL("+ keyColumnName + ", 0x0) != '" + strGuid + "'";
                #endregion

                var deleteProcess = await this._msHelper.ExecuteQuery(connProps, strQuery, sqlPar);

                deleteRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                deleteRs.IsSuccess = false;
                deleteRs.ErrorDescription = "Gagal Delete on Table " + tableName + " Guid " + strGuid + ": " + ex.Message;
            }

            return deleteRs;
        }
        //20250701, darul.wahid, ONFX-267, end

        private async Task<ApiMessage> DeleteDataNotInProcess(string tableName, string strGuid, ConnectionProperties connProps)
        {
            ApiMessage deleteRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "";

                #region Query Insert Data
                //20241106, darul.wahid, ONFX - 243, begin
                //string strQuery = @"       
                //    DELETE 
                //    FROM dbo." + tableName;
                string strQuery = @"";
                if(tableName.Equals("VLSTransactionsToday_TR"))
                {
                    strQuery += @"
                        INSERT INTO dbo.VLSTransactionsToday_TL
                        (
                            src			
                            , dealno		
                            , customer_id	
                            , acc_id		
                            , trx_branch	
                            , trx_date		
                            , trx_datetime	
                            , currency_code	
                            , amount		
                            , rate			
                            , InUSD			
                            , SourceKey		
                            , JISDORRate	
                            , InUSDJISDOR	
                            , m_pl_key		
                            , NIKAgent		
                            , GuidProcess	
                            , m_pl_key1		
                            , InsertedDate	
                        )
                        SELECT
                            src			
                            , dealno		
                            , customer_id	
                            , acc_id		
                            , trx_branch	
                            , trx_date		
                            , trx_datetime	
                            , currency_code	
                            , amount		
                            , rate			
                            , InUSD			
                            , SourceKey		
                            , JISDORRate	
                            , InUSDJISDOR	
                            , m_pl_key		
                            , NIKAgent		
                            , GuidProcess	
                            , m_pl_key1		
                            , WaktuMasukDeal	
                        FROM dbo.VLSTransactionsToday_TR
                        WHERE ISNULL(GuidProcess, 0x0) != '" + strGuid + @"'";
                }
                strQuery += @"       
                    DELETE 
                    FROM dbo." + tableName;
                //20241106, darul.wahid, ONFX - 243, end

                strQuery += @" WHERE ISNULL(GuidProcess, 0x0) != '" + strGuid + "'";
                #endregion

                var deleteProcess = await this._msHelper.ExecuteQuery(connProps, strQuery, sqlPar);

                deleteRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                deleteRs.IsSuccess = false;
                deleteRs.ErrorDescription = "Gagal Delete on Table "+ tableName + " Guid "+ strGuid + ": " + ex.Message;
            }

            return deleteRs;
        }

        //20231218, yudha.n, ANT-361, begin
        public async Task<ApiMessage<List<TRN_HDR_FX>>> inquiryTRN_HDR_FX_Option(ApiMessage<MxTranRq> param)
        {
            ApiMessage<List<TRN_HDR_FX>> inqRes = new();
            inqRes.copyHeaderForReply(param);

            DataSet result = new();
            try
            {
                List<OracleParameter> sqlPar = new();
                #region Query
                //20240527, samy, MX3, begin
                /*
                string query = @"
                    SELECT a.M_NB,
                        case
                            when translate( trim(c.M_IDSIBSCIF1), ' 1234567890', 'X' ) is null then lpad(trim(c.M_IDSIBSCIF1),19,'0')
                            else rtrim(c.M_IDSIBSCIF1) end     as M_IDSIBSCIF1,
                        to_char(a.M_TRN_DATE,'YYYY-MM-DD')     as M_TRN_DATE,
                        (M_TRN_TIME - mod(M_TRN_TIME,3600))/3600||':'||LPAD((MOD(M_TRN_TIME,3600)-MOD(M_TRN_TIME,60))/60,2,'0')||':'||LPAD(MOD(M_TRN_TIME,60),2,'0') as TIME,
                        a.M_BRW_NOMU1     as M_BRW_NOMU1,
                        a.M_BRW_NOM1     as M_BRW_NOM1,
                        a.M_BRW_NOMU2     as M_BRW_NOMU2,
                        a.M_BRW_NOM2     as M_BRW_NOM2,
                        a.M_COMMENT_BS     as M_COMMENT_BS,
                        rtrim(tdc.M_SOURCE_ID)     as M_SOURCE_ID,
                        rtrim(mktop.M_RSNCODE)     as M_RSNCODE,
                        CASE a.M_BRW_ODFC0 
                            WHEN 'IDR' THEN 'OPTIDR'
                            ELSE 'OPTVLS' END    as M_PL_KEY1,
                        a.M_TRN_TYPO    as M_TRN_TYPO,
                        a.M_BRW_ODFC0
                    FROM MUREXPROD.TABLE#DATA#COUNTERP_DBF c
                    INNER JOIN MUREXPROD.TRN_HDR_DBF a
                        ON c.M_LABEL =  CASE WHEN a.M_COMMENT_BS='S' THEN a.M_BPFOLIO ELSE a.M_SPFOLIO END
                    INNER JOIN MUREXPROD.TABLE#DATA#DEALCURR_DBF tdc
                        ON a.M_NB = tdc.M_NB
                    LEFT JOIN MUREXPROD.TABLE#DATA#MARKETOP_DBF mktop
                        ON a.M_OPT_MOPNB=mktop.M_NB
                    LEFT JOIN MUREXPROD.TRN_BROKER_DBF brok
                        ON a.M_NB=brok.M_NB and brok.M_LINE=0
                    LEFT JOIN MUREXPROD.FD121200_DBF fd
                        ON a.M_NB=fd.M_NB      
                    WHERE a.M_TRN_FMLY = 'CURR' 
                        AND M_TRN_TYPO LIKE '%D169_DCR_%'
                        AND M_TRN_DATE >= to_date(:dStartDate, 'yyyy-MM-dd')
                        AND M_TRN_DATE <= to_date(:dEndDate, 'yyyy-MM-dd')
                        and (a.M_BRW_NOMU2 = :cCur  OR a.M_BRW_NOMU1 = :cCur)                    
                        AND c.M_IDSIBSCIF1 <> '8888888888888888888'   
                        AND (rtrim(tdc.M_SOURCE_ID) <> 'BDS' OR rtrim(tdc.M_SOURCE_ID) IS NULL)
                        AND (mktop.M_RSNCODE is null OR rtrim(mktop.M_RSNCODE) = '') 
                        ";
                */
                string query = @"
                    SELECT a.M_CONTRACT as M_NB,
                        case
                            when translate( trim(c.M_IDSIBSCIF1), ' 1234567890', 'X' ) is null then lpad(trim(c.M_IDSIBSCIF1),19,'0')
                            else rtrim(c.M_IDSIBSCIF1) end     as M_IDSIBSCIF1,
                        to_char(a.M_TRN_DATE,'YYYY-MM-DD')     as M_TRN_DATE,
                        (M_TRN_TIME - mod(M_TRN_TIME,3600))/3600||':'||LPAD((MOD(M_TRN_TIME,3600)-MOD(M_TRN_TIME,60))/60,2,'0')||':'||LPAD(MOD(M_TRN_TIME,60),2,'0') as TIME,
                        a.M_BRW_NOMU1     as M_BRW_NOMU1,
                        a.M_BRW_NOM1     as M_BRW_NOM1,
                        a.M_BRW_NOMU2     as M_BRW_NOMU2,
                        a.M_BRW_NOM2     as M_BRW_NOM2,
                        a.M_COMMENT_BS     as M_COMMENT_BS,
                        rtrim(tdc.M_SOURCE_ID)     as M_SOURCE_ID,
                        rtrim(mktop.M_RSNCODE)     as M_RSNCODE,
--20241129, darul.wahid, MRX-2754, begin
                        --CASE a.M_BRW_ODFC0 
                        --   WHEN 'IDR' THEN 'OPTIDR'
                        --   ELSE 'OPTVLS' END    as M_PL_KEY1,
                        CASE WHEN a.m_trn_type = 'RBT'
                            THEN CASE 
                                WHEN a.M_COMMENT_BS = 'S' AND a.M_BRW_NOMU1 != 'IDR' THEN 'OPTIDR'
                                WHEN a.M_COMMENT_BS = 'B' AND a.M_BRW_NOMU1 = 'IDR' THEN 'OPTVLS' 
                                ELSE a.m_pl_key1
                            END
                        ELSE
                        CASE a.M_BRW_ODFC0 
                            WHEN 'IDR' THEN 'OPTIDR'
                            ELSE 'OPTVLS' END    
                        END as M_PL_KEY1,
--20241129, darul.wahid, MRX-2754, end
                        a.M_TRN_TYPO    as M_TRN_TYPO,
                        a.M_BRW_ODFC0
                        ,a.M_NB as  M_XNB
                       ,a.M_CONTRACT as M_COTRACT
                    --20241129, darul.wahid, MRX-2754, begin
                    --FROM MUREX310.TABLE#DATA#COUNTERP_DBF c
                    --INNER JOIN MUREX310.TRN_HDR_DBF a
                    --    ON c.M_LABEL =  CASE WHEN a.M_COMMENT_BS='S' THEN a.M_BPFOLIO ELSE a.M_SPFOLIO END
                    --join MUREX310.TRN_EXT_DBF U  
                    --on a.M_LEXTREF =U.M_REFERENCE
                    --INNER JOIN MUREX310.TABLE#DATA#DEALCURR_DBF tdc
                    --    on U.M_UDF_REF = tdc.M_NB
                    --LEFT JOIN MUREX310.table#data#event_dbf mktop
                    --    ON U.M_EVT_REF=mktop.M_REFERENCE
                    --LEFT JOIN MUREX310.TRN_BROKER_DBF brok
                    --    ON a.M_NB=brok.M_NB and brok.M_LINE=0
                    --LEFT JOIN MUREX310.FD121200_DBF fd
                    --    ON a.M_NB=fd.M_NB      
                        ,TRIM(a.M_TRN_TYPE) AS M_TRN_TYPE
                    FROM [[MurexSchema]].TABLE#DATA#COUNTERP_DBF c
                    INNER JOIN [[MurexSchema]].TRN_HDR_DBF a
                        ON c.M_LABEL =  CASE WHEN a.M_COMMENT_BS='S' THEN a.M_BPFOLIO ELSE a.M_SPFOLIO END
                    join [[MurexSchema]].TRN_EXT_DBF U  
                    on a.M_LEXTREF =U.M_REFERENCE
                    INNER JOIN [[MurexSchema]].TABLE#DATA#DEALCURR_DBF tdc
                        on U.M_UDF_REF = tdc.M_NB
                    LEFT JOIN [[MurexSchema]].table#data#event_dbf mktop
                        ON U.M_EVT_REF=mktop.M_REFERENCE
                    LEFT JOIN [[MurexSchema]].TRN_BROKER_DBF brok
                        ON a.M_NB=brok.M_NB and brok.M_LINE=0
                    LEFT JOIN [[MurexSchema]].FD121200_DBF fd
                        ON a.M_NB=fd.M_NB                    
                    --20241129, darul.wahid, MRX-2754, end
                    WHERE a.M_TRN_FMLY = 'CURR' 
                        --AND M_TRN_TYPO LIKE '%D169_DCR_%'              
                        and a.M_trn_grp='OPT'
                        --20241129, darul.wahid, MRX-2754, begin
                        --and a.m_trn_type = 'SMP'
                        and a.m_trn_type IN ('SMP', 'RBT')
                        --20241129, darul.wahid, MRX-2754, end
                        AND a.M_TRN_DATE >= to_date(:dStartDate, 'yyyy-MM-dd')
                        AND a.M_TRN_DATE <= to_date(:dEndDate, 'yyyy-MM-dd')
                        AND (a.M_BRW_NOMU2 = :cCur  OR a.M_BRW_NOMU1 = :cCur)  
                        AND c.M_IDSIBSCIF1 <> '8888888888888888888'   
                        AND (rtrim(tdc.M_SOURCE_ID) <> 'BDS' OR rtrim(tdc.M_SOURCE_ID) IS NULL)
                        AND (mktop.M_RSNCODE is null OR rtrim(mktop.M_RSNCODE) = '')
                        ";
                //20240527, samy, MX3, end
                if (param.Data.CommentBsIn != null)
                {
                    if (param.Data.CommentBsIn.Count > 0)
                    {
                        query += " AND a.M_COMMENT_BS IN ('";
                        query += param.Data.CommentBsIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.KeyIn != null)
                {
                    if (param.Data.KeyIn.Count > 0)
                    {
                        query += " AND M_PL_KEY1 IN ('";
                        query += param.Data.KeyIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }

                if (param.Data.TypoIn != null)
                {
                    if (param.Data.TypoIn.Count > 0)
                    {
                        query += " AND a.M_TRN_TYPO IN ('";
                        query += param.Data.TypoIn.Aggregate((i, j) => i + "', '" + j);
                        query += "') ";
                    }
                }
                #endregion
                sqlPar.Add(new OracleParameter(":dStartDate", param.Data.StartDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new OracleParameter(":dEndDate", param.Data.EndDate.ToString("yyyy-MM-dd")));
                sqlPar.Add(new OracleParameter(":cCur", param.Data.M_BRW_NOMU));

                //20241129, darul.wahid, MRX - 2754, begin
                query = query.Replace("[[MurexSchema]]", this._configuration["MurexSchema"].ToString());
                //20241129, darul.wahid, MRX - 2754, end

                result = await this._msHelper.ExecuteOracleQuery(this._connMurex, query, sqlPar);

                List<TRN_HDR_FX> dataTran = JsonConvert.DeserializeObject<List<TRN_HDR_FX>>(JsonConvert.SerializeObject(result.Tables[0]));

                inqRes.IsSuccess = true;
                inqRes.Data = dataTran;
            }
            catch (Exception ex)
            {
                inqRes.IsSuccess = false;
                inqRes.ErrorDescription = "Error inquiryTRN_HDR_FX_Murex(): " + ex.Message;
            }
            finally
            {
                inqRes.MessageDateTime = DateTime.Now;
            }

            return inqRes;
        }

        public async Task<ApiMessage<List<VLSSummary>>> inquiryValasSummaryFXOption(ApiMessage<List<VLSSummary>> trans)
        {
            ApiMessage<List<VLSSummary>> inqRs = new();
            inqRs.copyHeaderForReply(trans);

            try
            {
                List<VLSSummary> dataInq = new();
                List<SqlParameter> sqlPar = new();

                DataSet dsTran = new();
                DataTable dtTran = new();

                dtTran = this._msHelper.MapListToTable<VLSSummary>(trans.Data);

                List<string> KeepColumn = new() { "customer_id" };

                List<string> toRemove = dtTran.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Except(KeepColumn).ToList();

                foreach (string col in toRemove) dtTran.Columns.Remove(col);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Summaries";

                string strXml = dsTran.GetXml();

                #region Query Populate Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
                    )
                    SELECT
	                    customer_id
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
                    )  

                    SELECT
	                    tm.customer_id
	                    , tm.InUSD
	                    , tm.branch
	                    , tm.[name]
	                    , tm.office_name
	                    , tm.npwp
	                    , tm.identity_1
	                    , tm.ProcessTime
	                    , tm.InUSDJISDOR	
                    FROM dbo.VLSSummaryFXOption_TM AS tm
                    JOIN #tmpSummary AS tmp
                    ON tm.customer_id = tmp.customer_id
                    ";
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                //DataSet InqVLS = await this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar);
                DataSet InqVLS = await this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar);

                if (InqVLS.Tables.Count > 0)
                    dataInq = JsonConvert.DeserializeObject<List<VLSSummary>>(JsonConvert.SerializeObject(InqVLS.Tables[0]));


                inqRs.IsSuccess = true;
                inqRs.Data = dataInq;
            }
            catch (Exception ex)
            {
                inqRs.IsSuccess = false;
                inqRs.ErrorDescription = "Gagal Inquiry Valas Summary: " + ex.Message;
            }

            return inqRs;
        }

        public async Task<ApiMessage> InsertValasSummaryFXOptionV2(ApiMessage<List<VLSSummary>> sumaries, bool isDaily, string guidProcess)
        {
            ApiMessage insertRs = new();
            insertRs.copyHeaderForReply(sumaries);

            try
            {
                if (isDaily)
                {
                    ApiMessage deleteTran = await this.DeleteDataValasSummaryFXOption();

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }
                List<SqlParameter> sqlPar = new();

                DataSet dsTran = new();
                DataTable dtTran = new();

                string strXml = "",
                    summaryTodayTableName = "VLSSummaryTodayFXOption_TM";

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
	                    , InUSD			MONEY
	                    , branch		VARCHAR(5)
	                    , [name]		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , ProcessTime	DATETIME
	                    , InUSDJISDOR	MONEY    		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR							
                    )
                    SELECT
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , strProcessTime
	                    , InUSDJISDOR	        			
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
	                    , InUSD			    MONEY
	                    , branch		    VARCHAR(5)
	                    , [name]		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    --, ProcessTime	    DATETIME
                        , strProcessTime	VARCHAR(50)	                    
                        , InUSDJISDOR	    MONEY    	          			
                    )  

                    ";

                if (isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.VLSSummaryFXOption_TM
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.customer_id
	                    , tmp.InUSD
	                    , tmp.branch
	                    , tmp.[name]
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.ProcessTime
	                    , tmp.InUSDJISDOR
                        , @pcGuidProcess
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.VLSSummaryFXOption_TM AS tm
                    ON tmp.customer_id = tm.customer_id
                    WHERE ISNULL(tm.customer_id, '') = ''
                    ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tm
                    SET InUSD = new.InUSD
                        , InUSDJISDOR = new.InUSDJISDOR                        
                    FROM dbo.VLSSummaryTodayFXOption_TM AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id  
                    WHERE 1 = 1
                        AND
                        (
                            tm.InUSD != new.InUSD
                            OR tm.InUSDJISDOR != new.InUSDJISDOR
                        )

                    INSERT INTO dbo.VLSSummaryTodayFXOption_TM
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.customer_id
	                    , tmp.InUSD
	                    , tmp.branch
	                    , tmp.[name]
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.ProcessTime
	                    , tmp.InUSDJISDOR
                        , @pcGuidProcess
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.VLSSummaryTodayFXOption_TM AS tm
                    ON tmp.customer_id = tm.customer_id
                    WHERE tm.customer_id is null 

                    UPDATE tm
                    SET GuidProcess = @pcGuidProcess
                    FROM dbo.VLSSummaryTodayFXOption_TM AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id 
                    WHERE ISNULL(tm.GuidProcess, 0x0) != @pcGuidProcess ";
                }
                #endregion

                List<Task<DataSet>> listInsert = new();
                List<Task<ApiMessage>> listDeleteToday = new();

                List<List<VLSSummary>> dataToInsert = new();
                List<List<VLSSummary>> unParitioned = new();
                List<List<VLSSummary>> partitioned = new();

                int maxIndex = sumaries.Data.Count();

                unParitioned = clsUtils.SplitList(sumaries.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(sumaries.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSSummary>>();
                    dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSSummary> data in dataToInsert)
                    {
                        if (this._connSFX.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Summaries";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(summaryTodayTableName, guidProcess, this._connSFX)));
                }

                if (this._bInsert2OBL)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSSummary>>();
                    dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSSummary> data in dataToInsert)
                    {
                        if (this._connObli.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Summaries";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(summaryTodayTableName, guidProcess, this._connObli)));
                }

                if (this._bInsert2SIBS)
                {
                    listInsert = new List<Task<DataSet>>();

                    dataToInsert = new List<List<VLSSummary>>();
                    dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                    sqlPar = new List<SqlParameter>();
                    sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                    sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                    foreach (List<VLSSummary> data in dataToInsert)
                    {
                        if (this._connSibs.isDBNISP)
                        {
                            if (listInsert.Count == this._globalVariable.MaxInsertTask)
                            {
                                await Task.WhenAll(listInsert);

                                foreach (Task<DataSet> insert in listInsert)
                                {
                                    if (insert.IsFaulted)
                                        throw new Exception("Faulted: " + insert.Exception.Message);

                                    if (insert.IsCanceled)
                                        throw new Exception("Canceled: " + insert.Exception.Message);
                                }

                                listInsert = new List<Task<DataSet>>();
                            }
                        }

                        dsTran = new DataSet();
                        dtTran = new DataTable();

                        dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                        dsTran.Tables.Add(dtTran);
                        dsTran.DataSetName = "Data";
                        dsTran.Tables[0].TableName = "Summaries";

                        strXml = dsTran.GetXml();

                        sqlPar[0].Value = strXml;

                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strQuery, sqlPar)));
                    }

                    if (listInsert.Count > 0)
                    {
                        await Task.WhenAll(listInsert);

                        foreach (Task<DataSet> insert in listInsert)
                        {
                            if (insert.IsFaulted)
                                throw new Exception("Faulted: " + insert.Exception.Message);

                            if (insert.IsCanceled)
                                throw new Exception("Canceled: " + insert.Exception.Message);
                        }
                    }

                    if (!isDaily)
                        listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(summaryTodayTableName, guidProcess, this._connSibs)));
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if (!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Transaction: " + ex.Message;
            }

            return insertRs;
        }

        //20231218, yudha.n, ANT-361, end
        //20240826, darul.wahid, RFR-54578, begin
        public async Task<ApiMessage> InsertValasTransactionV3(ApiMessage<List<VLSTransaction>> trans, CalculateLimitPeriod period, string guidProcess)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(trans);

            try
            {
                MappingTableName mappingInsertTable = null;

                if (period.isDaily)
                {
                    var deleteTran = await this.DeleteDataValasTransactionV2(period);

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }
                
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "";

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpTrans') IS NOT NULL
	                    DROP TABLE #tmpTrans

                    CREATE TABLE #tmpTrans                     
                    (    
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , trx_date			DATETIME
	                    , trx_datetime		DATETIME
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                        , m_pl_key1         VARCHAR(38)
                        --20250701, darul.wahid, ONFX-267, begin
                        , lcs_flag          BIT
                        , buy_sell_code     CHAR(1)
                        , product           VARCHAR(50)
                        , in_usd_before     MONEY
                        , in_usd_total      MONEY
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , [call]            VARCHAR(5)
                        --20251120, darul.wahid, end
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpTrans
                    (
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , trx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , SourceKey
	                    , JISDORRate
	                    , InUSDJISDOR	
                        , m_pl_key
                        , nik_agent
                        , m_pl_key1         
                        --20250701, darul.wahid, ONFX-267, begin
                        , lcs_flag          
                        , buy_sell_code     
                        , product           
                        , in_usd_before     
                        , in_usd_total      
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , [call]            
                        --20251120, darul.wahid, end
                    )
                    SELECT
	                    src
	                    , dealno
	                    , customer_id
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , strTrx_datetime
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , ISNULL(SourceKey, '')
	                    , JISDORRate
	                    , InUSDJISDOR		
                        , m_pl_key
                        , nik_agent     
                        , ISNULL(m_pl_key1, '')   
                        --20250701, darul.wahid, ONFX-267, begin
                        , FlagLCS          
                        , BuySellCode     
                        , Product           
                        , inUSDBefore     
                        , inUSDTotal      
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , [call]            
                        --20251120, darul.wahid, end
                    FROM openxml(@nDocHandle, N'/Data/Trans',2)           
                    WITH (
	                    src					VARCHAR(20)
	                    , dealno			VARCHAR(20)
	                    , customer_id		VARCHAR(20)
	                    , acc_id			VARCHAR(20)
	                    , trx_branch		VARCHAR(20)
	                    , strTrx_date		VARCHAR(20)
                        , strTrx_datetime	VARCHAR(50)
	                    , currency_code		VARCHAR(20)
	                    , amount			MONEY
	                    , rate				FLOAT
	                    , InUSD				MONEY
	                    , SourceKey			VARCHAR(20)
	                    , JISDORRate		FLOAT
	                    , InUSDJISDOR		MONEY      
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                        , m_pl_key1         VARCHAR(38)
                        --20250701, darul.wahid, ONFX-267, begin
                        , FlagLCS          BIT
                        , BuySellCode      CHAR(1)
                        , Product          VARCHAR(50)
                        , inUSDBefore      MONEY
                        , inUSDTotal       MONEY
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , [call]            VARCHAR(5)
                        --20251120, darul.wahid, end
                    )  
                    
                    DELETE #tmpTrans
                    WHERE amount < 0
                    ";

                if (period.isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.[[TableName]]
                    (
                        src
                        , dealno
                        , customer_id
                        , acc_id
                        , trx_branch
                        , trx_date
                        , trx_datetime
                        , currency_code
                        , amount
                        , rate
                        , InUSD
                        , SourceKey
                        , JISDORRate
                        , InUSDJISDOR
                        , m_pl_key
                        , NIKAgent       
                        , GuidProcess
                        , m_pl_key1         
                        --20250701, darul.wahid, ONFX-267, begin
                        , lcs_flag          
                        , buy_sell_code     
                        , product           
                        , in_usd_before     
                        , in_usd_total      
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , [call]            
                        --20251120, darul.wahid, end
                    )
                    SELECT
                        tmp.src
	                    , tmp.dealno
	                    , tmp.customer_id
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.trx_datetime
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.SourceKey
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
                        , tmp.m_pl_key
                        , tmp.nik_agent  
                        , @pcGuidProcess
                        , tmp.m_pl_key1         
                        --20250701, darul.wahid, ONFX-267, begin
                        , tmp.lcs_flag          
                        , tmp.buy_sell_code     
                        , tmp.product           
                        , tmp.in_usd_before     
                        , tmp.in_usd_total      
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , tmp.[call]            
                        --20251120, darul.wahid, end
                    FROM #tmpTrans AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tr
                    ON tmp.dealno = tr.dealno
                        AND tmp.customer_id = tr.customer_id
                    WHERE tr.dealno is null ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tr  
                        SET  amount = tmp.amount
                            , rate = tmp.rate
                            , InUSD = tmp.InUSD
                            , JISDORRate = tmp.JISDORRate
                            , InUSDJISDOR = tmp.InUSDJISDOR
                            , m_pl_key = tmp.m_pl_key
                            , m_pl_key1 = tmp.m_pl_key1
                            --20250701, darul.wahid, ONFX-267, begin
                            , lcs_flag = tmp.lcs_flag          
                            , buy_sell_code = tmp.buy_sell_code     
                            , product = tmp.product
                            , in_usd_before = tmp.in_usd_before
                            , in_usd_total = tmp.in_usd_total
                            --20250701, darul.wahid, ONFX-267, end
                            --20251120, darul.wahid, begin
                            , [call] = tmp.[call]
                            --20251120, darul.wahid, end
                    FROM dbo.[[TableName]] AS tr
                    JOIN #tmpTrans AS tmp
                    ON tmp.dealno = tr.dealno
                        AND tr.customer_id = tmp.customer_id
                    WHERE 1 = 1
                        AND
                        (
                            tr.amount != tmp.amount
                            OR tr.rate != tmp.rate
                            OR tr.InUSD != tmp.InUSD
                            OR tr.JISDORRate != tmp.JISDORRate
                            OR tr.InUSDJISDOR != tmp.InUSDJISDOR
                            OR tr.m_pl_key != tmp.m_pl_key
                            OR ISNULL(tr.m_pl_key1, '') != tmp.m_pl_key1
                            --20250701, darul.wahid, ONFX-267, begin
                            OR tr.lcs_flag != tmp.lcs_flag          
                            OR tr.buy_sell_code != tmp.buy_sell_code     
                            OR tr.product != tmp.product
                            OR tr.in_usd_before != tmp.in_usd_before
                            OR tr.in_usd_total != tmp.in_usd_total
                            --20250701, darul.wahid, ONFX-267, end
                            --20251120, darul.wahid, begin
                            OR tr.[call] != tmp.[call]
                            --20251120, darul.wahid, end
                        )                     

                    INSERT INTO dbo.[[TableName]]
                    (
                        src
                        , dealno
                        , customer_id
                        , acc_id
                        , trx_branch
                        , trx_date
                        , trx_datetime
                        , currency_code
                        , amount
                        , rate
                        , InUSD
                        , SourceKey
                        , JISDORRate
                        , InUSDJISDOR
                        , m_pl_key
                        , NIKAgent  
                        , GuidProcess
                        , m_pl_key1         
                        --20250701, darul.wahid, ONFX-267, begin
                        , lcs_flag          
                        , buy_sell_code     
                        , product           
                        , in_usd_before     
                        , in_usd_total      
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , [call]            
                        --20251120, darul.wahid, end
                    )
                    SELECT
                        tmp.src
	                    , tmp.dealno
	                    , tmp.customer_id
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.trx_datetime
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.SourceKey
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
                        , tmp.m_pl_key
                        , tmp.nik_agent  
                        , @pcGuidProcess
                        , tmp.m_pl_key1         
                        --20250701, darul.wahid, ONFX-267, begin
                        , tmp.lcs_flag          
                        , tmp.buy_sell_code     
                        , tmp.product           
                        , tmp.in_usd_before     
                        , tmp.in_usd_total      
                        --20250701, darul.wahid, ONFX-267, end
                        --20251120, darul.wahid, begin
                        , tmp.[call]            
                        --20251120, darul.wahid, end
                    FROM #tmpTrans AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tr
                    ON tmp.dealno = tr.dealno
                    WHERE tr.dealno is null

                    UPDATE tr  
                        SET  GuidProcess = @pcGuidProcess
                    FROM dbo.[[TableName]] AS tr
                    JOIN #tmpTrans AS tmp
                    ON tmp.dealno = tr.dealno
                        AND tr.customer_id = tmp.customer_id
                    WHERE 1 = 1
                        AND ISNULL(tr.GuidProcess, 0x0) != @pcGuidProcess ";
                }
                #endregion
                
                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();
                List<Task<ApiMessage>> listDeleteToday = new List<Task<ApiMessage>>();

                List<List<VLSTransaction>> dataToInsert = new List<List<VLSTransaction>>();
                List<List<VLSTransaction>> unParitioned = new List<List<VLSTransaction>>();
                List<List<VLSTransaction>> partitioned = new List<List<VLSTransaction>>();

                int maxIndex = trans.Data.Count();

                unParitioned = clsUtils.SplitList(trans.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(trans.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Transaction)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if(mappingInsertTable.ExecuteTable)
                    {
                        string strExecONFX = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strTransactionTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSTransaction>>();
                        dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSTransaction> data in dataToInsert)
                        {
                            if (this._connSFX.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSTransaction>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Trans";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strTransactionTodayTableName, guidProcess, this._connSFX)));
                    }
                }

                if (this._bInsert2OBL)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Transaction)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if(mappingInsertTable.ExecuteTable)
                    {
                        string strExecOBL = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strTransactionTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSTransaction>>();
                        dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSTransaction> data in dataToInsert)
                        {
                            if (this._connObli.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSTransaction>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Trans";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strTransactionTodayTableName, guidProcess, this._connObli)));
                    }
                }

                if (this._bInsert2SIBS)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Transaction)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if(mappingInsertTable.ExecuteTable)
                    {
                        string strExecSIBS = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strTransactionTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSTransaction>>();
                        dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSTransaction> data in dataToInsert)
                        {
                            if (this._connSibs.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSTransaction>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Trans";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));

                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strTransactionTodayTableName, guidProcess, this._connSibs)));
                    }
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if (!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Transaction: " + ex.Message;
            }

            return insertRs;
        }
        public async Task<ApiMessage> InsertValasSummaryV3(ApiMessage<List<VLSSummary>> sumaries, CalculateLimitPeriod period, string guidProcess)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(sumaries);

            try
            {
                MappingTableName mappingInsertTable = null;

                if (period.isDaily)
                {
                    var deleteTran = await this.DeleteDataValasSummaryV2(period);

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }

                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "";

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
	                    , InUSD			MONEY
	                    , branch		VARCHAR(5)
	                    , [name]		VARCHAR(20)
	                    , office_name	VARCHAR(50)
	                    , npwp			VARCHAR(30)
	                    , identity_1	VARCHAR(50)
	                    , ProcessTime	DATETIME
	                    , InUSDJISDOR	MONEY    		
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR							
                    )
                    SELECT
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , strProcessTime
	                    , InUSDJISDOR	        			
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
	                    , InUSD			    MONEY
	                    , branch		    VARCHAR(5)
	                    , [name]		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    --, ProcessTime	    DATETIME
                        , strProcessTime	VARCHAR(50)	                    
                        , InUSDJISDOR	    MONEY    	          			
                    )  

                    ";

                if (period.isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.[[TableName]]
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.customer_id
	                    , tmp.InUSD
	                    , tmp.branch
	                    , tmp.[name]
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.ProcessTime
	                    , tmp.InUSDJISDOR
                        , @pcGuidProcess
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tm
                    ON tmp.customer_id = tm.customer_id
                    WHERE ISNULL(tm.customer_id, '') = ''
                    ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tm
                    SET InUSD = new.InUSD
                        , InUSDJISDOR = new.InUSDJISDOR                        
                    FROM dbo.[[TableName]] AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id  
                    WHERE 1 = 1
                        AND
                        (
                            tm.InUSD != new.InUSD
                            OR tm.InUSDJISDOR != new.InUSDJISDOR
                        )

                    INSERT INTO dbo.[[TableName]]
                    (
	                    customer_id
	                    , InUSD
	                    , branch
	                    , [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , ProcessTime
	                    , InUSDJISDOR
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.customer_id
	                    , tmp.InUSD
	                    , tmp.branch
	                    , tmp.[name]
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.ProcessTime
	                    , tmp.InUSDJISDOR
                        , @pcGuidProcess
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tm
                    ON tmp.customer_id = tm.customer_id
                    WHERE tm.customer_id is null 

                    UPDATE tm
                    SET GuidProcess = @pcGuidProcess
                    FROM dbo.[[TableName]] AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id 
                    WHERE ISNULL(tm.GuidProcess, 0x0) != @pcGuidProcess ";
                }
                #endregion
                
                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();
                List<Task<ApiMessage>> listDeleteToday = new List<Task<ApiMessage>>();

                List<List<VLSSummary>> dataToInsert = new List<List<VLSSummary>>();
                List<List<VLSSummary>> unParitioned = new List<List<VLSSummary>>();
                List<List<VLSSummary>> partitioned = new List<List<VLSSummary>>();

                int maxIndex = sumaries.Data.Count();

                unParitioned = clsUtils.SplitList(sumaries.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(sumaries.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Summary)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if(mappingInsertTable.ExecuteTable)
                    {
                        string strExecONFX = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strSummaryTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSSummary>>();
                        dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSSummary> data in dataToInsert)
                        {
                            if (this._connSFX.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Summaries";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strSummaryTodayTableName, guidProcess, this._connSFX)));
                    }
                }

                if (this._bInsert2OBL)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Summary)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if(mappingInsertTable.ExecuteTable)
                    {
                        string strExecOBL = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strSummaryTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSSummary>>();
                        dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSSummary> data in dataToInsert)
                        {
                            if (this._connObli.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Summaries";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strSummaryTodayTableName, guidProcess, this._connObli)));
                    }
                }

                if (this._bInsert2SIBS)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Summary)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if(mappingInsertTable.ExecuteTable)
                    {
                        string strExecSIBS = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strSummaryTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSSummary>>();
                        dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSSummary> data in dataToInsert)
                        {
                            if (this._connSibs.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSSummary>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Summaries";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strSummaryTodayTableName, guidProcess, this._connSibs)));
                    }
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if (!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Summary: " + ex.Message;
            }

            return insertRs;
        }
        public async Task<ApiMessage> InsertValasResultV3(ApiMessage<List<VLSResultFinal>> results, CalculateLimitPeriod period, string guidProcess)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(results);

            try
            {
                MappingTableName mappingInsertTable = null;

                if (period.isDaily)
                {                    
                    var deleteTran = await this.DeleteDataValasResultV2(period);

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "";

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpResult') IS NOT NULL
	                    DROP TABLE #tmpResult

                    CREATE TABLE #tmpResult                     
                    (    
	                    branch			    VARCHAR(5)
	                    , customer_id	    VARCHAR(20)
	                    , [name]		    VARCHAR(20)
	                    , acc_id		    VARCHAR(20)
	                    , trx_branch	    VARCHAR(5)
	                    , trx_date		    DATETIME
	                    , currency_code	    VARCHAR(5)
	                    , amount		    MONEY
	                    , rate			    FLOAT
	                    , InUSD			    MONEY
	                    , dealno		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    , isHit			    BIT
	                    , ProcessTime	    DATETIME
	                    , underlying	    VARCHAR(15)
                        , KetUnderlying	    VARCHAR(150)
                        , Purpose           VARCHAR(50)
	                    , JISDORRate	    FLOAT
	                    , InUSDJISDOR	    MONEY
	                    , isHitJISDOR	    BIT   		
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpResult
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , KetUnderlying
                        , Purpose          
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 	
                        , m_pl_key
                        , nik_agent         
                    )
                    SELECT
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , strTrx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , ISNULL(npwp, '')
	                    , identity_1
	                    , isHit
	                    , strProcessTime
	                    , ISNULL(underlying, '')
                        , ISNULL(ketUnderlying, '')
                        , ISNULL(Purpose, '')           
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR    
                        , m_pl_key
                        , nik_agent         
                    FROM openxml(@nDocHandle, N'/Data/Results',2)           
                    WITH (
	                    branch			    VARCHAR(5)
	                    , customer_id	    VARCHAR(20)
	                    , [name]		    VARCHAR(20)
	                    , acc_id		    VARCHAR(20)
	                    , trx_branch	    VARCHAR(5)
	                    --, trx_date		    DATETIME
	                    , currency_code	    VARCHAR(5)
	                    , amount		    MONEY
	                    , rate			    FLOAT
	                    , InUSD			    MONEY
	                    , dealno		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    , isHit			    BIT
	                    --, ProcessTime	    DATETIME
	                    , underlying	    VARCHAR(15)
                        , ketUnderlying     VARCHAR(150)
                        , Purpose           VARCHAR(50)
	                    , JISDORRate	    FLOAT
	                    , InUSDJISDOR	    MONEY
	                    , isHitJISDOR	    BIT   	
                        , strTrx_date		VARCHAR(20)
                        , strProcessTime	VARCHAR(50)
                        , m_pl_key			NVARCHAR(50)
                        , nik_agent         BIGINT    
                    )  
                    ";

                if (period.isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.[[TableName]]
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent        
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.branch
	                    , tmp.customer_id
	                    , tmp.[name]
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.dealno
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.isHit
	                    , tmp.ProcessTime
	                    , tmp.underlying
                        , tmp.KetUnderlying
                        , tmp.Purpose
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
	                    , tmp.isHitJISDOR 
                        , tmp.m_pl_key
                        , tmp.nik_agent      
                        , @pcGuidProcess
                    FROM #tmpResult AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tt
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE ISNULL(tt.dealno, '') = ''
                    ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tt
                        SET  amount = tmp.amount
                            , rate = tmp.rate
                            , InUSD = tmp.InUSD
                            , isHit = tmp.isHit
                            , JISDORRate = tmp.JISDORRate
                            , InUSDJISDOR = tmp.InUSDJISDOR
                            , isHitJISDOR = tmp.isHitJISDOR
                    FROM dbo.[[TableName]] AS tt
                    JOIN #tmpResult AS tmp
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE 1 = 1
                        AND
                        (
                            tmp.amount != tt.amount
                            OR tmp.rate != tt.rate
                            OR tmp.InUSD != tt.InUSD
                            OR tmp.isHit != tt.isHit
                            OR tmp.JISDORRate != tt.JISDORRate
                            OR tmp.InUSDJISDOR != tt.InUSDJISDOR
                            OR tmp.isHitJISDOR != tt.isHitJISDOR
                        )

                    INSERT INTO dbo.[[TableName]] 
                    (
	                    branch
	                    , customer_id
	                    , [name]
	                    , acc_id
	                    , trx_branch
	                    , trx_date
	                    , currency_code
	                    , amount
	                    , rate
	                    , InUSD
	                    , dealno
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , isHit
	                    , ProcessTime
	                    , underlying
                        , ketUnderlying
                        , Purpose
	                    , JISDORRate
	                    , InUSDJISDOR
	                    , isHitJISDOR 
                        , m_pl_key
                        , NIKAgent         
                        , GuidProcess
                    )
                    SELECT 
	                    tmp.branch
	                    , tmp.customer_id
	                    , tmp.[name]
	                    , tmp.acc_id
	                    , tmp.trx_branch
	                    , tmp.trx_date
	                    , tmp.currency_code
	                    , tmp.amount
	                    , tmp.rate
	                    , tmp.InUSD
	                    , tmp.dealno
	                    , tmp.office_name
	                    , tmp.npwp
	                    , tmp.identity_1
	                    , tmp.isHit
	                    , tmp.ProcessTime
	                    , tmp.underlying
                        , tmp.KetUnderlying
                        , tmp.Purpose
	                    , tmp.JISDORRate
	                    , tmp.InUSDJISDOR
	                    , tmp.isHitJISDOR 
                        , tmp.m_pl_key
                        , tmp.nik_agent  
                        , @pcGuidProcess
                    FROM #tmpResult AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tt
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE tt.dealno is null 

                    UPDATE tt
                        SET GuidProcess = @pcGuidProcess                    
                    FROM dbo.[[TableName]] AS tt
                    JOIN #tmpResult AS tmp
                    ON tmp.dealno = tt.dealno
                        AND tmp.customer_id = tt.customer_id
                    WHERE ISNULL(tt.GuidProcess, 0x0) != @pcGuidProcess ";

                }
                #endregion

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();
                List<Task<ApiMessage>> listDeleteToday = new List<Task<ApiMessage>>();

                List<List<VLSResultFinal>> dataToInsert = new List<List<VLSResultFinal>>();
                List<List<VLSResultFinal>> unParitioned = new List<List<VLSResultFinal>>();
                List<List<VLSResultFinal>> partitioned = new List<List<VLSResultFinal>>();

                int maxIndex = results.Data.Count();

                unParitioned = clsUtils.SplitList(results.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(results.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>                         
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();
                    
                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExecONFX = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strResultTodayTableName = mappingInsertTable.TableName;
                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSResultFinal>>();
                        dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSResultFinal> data in dataToInsert)
                        {
                            if (this._connSFX.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSResultFinal>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Results";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strResultTodayTableName, guidProcess, this._connSFX)));

                    }
                }

                if (this._bInsert2OBL)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();
                    
                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExecOBL = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strResultTodayTableName = mappingInsertTable.TableName;
                        
                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSResultFinal>>();
                        dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSResultFinal> data in dataToInsert)
                        {
                            if (this._connObli.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSResultFinal>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Results";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strResultTodayTableName, guidProcess, this._connObli)));

                    }
                }

                if (this._bInsert2SIBS)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();
                    
                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExecSIBS = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strResultTodayTableName = mappingInsertTable.TableName;
                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<VLSResultFinal>>();
                        dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", guidProcess));

                        foreach (List<VLSResultFinal> data in dataToInsert)
                        {
                            if (this._connSibs.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<VLSResultFinal>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Results";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strResultTodayTableName, guidProcess, this._connSibs)));

                    }
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if (!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Result: " + ex.Message;
            }

            return insertRs;
        }
        private async Task<ApiMessage> DeleteDataValasTransactionV2(CalculateLimitPeriod period)
        {
            ApiMessage insertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                if (this._bInsert2ONFX)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Transaction)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecONFX = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                }

                if (this._bInsert2OBL)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Transaction)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecOBL = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                }

                if (this._bInsert2SIBS)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Transaction)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecSIBS = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Transaction: " + ex.Message;
            }

            return insertRs;
        }
        private async Task<ApiMessage> DeleteDataValasSummaryV2(CalculateLimitPeriod period)
        {
            ApiMessage insertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                if (this._bInsert2ONFX)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Summary)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecONFX = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                }

                if (this._bInsert2OBL)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Summary)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecOBL = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                }

                if (this._bInsert2SIBS)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.Summary)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecSIBS = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Summary Transaction: " + ex.Message;
            }

            return insertRs;
        }        
        private async Task<ApiMessage> DeleteDataValasResultV2(CalculateLimitPeriod period)
        {
            ApiMessage insertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                if (this._bInsert2ONFX)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX)) 
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        ) 
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();
                    
                    string strExecONFX = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                    
                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                }

                if (this._bInsert2OBL)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();
                    
                    string strExecOBL = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                    
                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                }

                if (this._bInsert2SIBS)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x => 
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.ResultFinal) 
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();
                    
                    string strExecSIBS = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;
                    
                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Final Result: " + ex.Message;
            }

            return insertRs;
        }
        //20240826, darul.wahid, RFR-54578, end
        //20241106, darul.wahid, ONFX-243, begin
        public async Task<(bool isSuccess, string errMsg)> GenerateDataTransaksiNasabah(CalculateLimitPeriod period)
        {
            bool isSuccess = false;
            string errMsg = "";
            try
            {
                string strQuery = "EXEC dbo.VLSGenerateDataTrxNasabah @pdCurrentDate = @pdInputDate";

                object Param = new
                {
                    pdInputDate = period.PeriodEnd.ToString("yyyyMMdd")
                };
                
                var execSPRs = await this._connDB.Execute(this._connSibs, strQuery, Param);

                if (!execSPRs.isSuccess)
                    throw new Exception(execSPRs.strErrMsg);

                isSuccess = true;
            }
            catch(Exception ex)
            {
                isSuccess = false;
                errMsg = ex.Message;
            }

            return await Task.FromResult((isSuccess, errMsg));
        }
        //20241106, darul.wahid, ONFX-243, end
        //20250701, darul.wahid, ONFX-267, begin
        public async Task<ApiMessage> InsertSummaryValas(ApiMessage<List<summary_valas>> sumaries, CalculateLimitPeriod period)
        {
            ApiMessage insertRs = new ApiMessage();
            insertRs.copyHeaderForReply(sumaries);

            try
            {
                MappingTableName mappingInsertTable = null;

                if (period.isDaily)
                {
                    var deleteTran = await this.DeleteDataSummaryValas(period);

                    if (!deleteTran.IsSuccess)
                        throw new Exception(deleteTran.ErrorDescription);
                }

                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                string strXml = "";

                #region Query Insert Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
                        [customer_id]			    [varchar](20)		
	                    , [product]			        [varchar](25)		
	                    , [buy_sell]			    [varchar](1)		
	                    , [currency]			    [varchar](3)	
                        , [lcs_flag]                [bit]
	                    , [total_in_usd]		    [money]				
	                    , [total_in_usd_jisdor]     [money]				
	                    , [branch]			        [varchar](5)		
	                    , [name]				    [varchar](20)		
	                    , [office_name]		        [varchar](50)		
	                    , [no_npwp]			        [varchar](30)		
	                    , [no_identity]		        [varchar](50)		
	                    , [process_time]		    [datetime]			
	                    , [guid_process]		    [uniqueidentifier]	
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
                        [customer_id]			
	                    , [product]				
	                    , [buy_sell]				
	                    , [currency]	
                        , [lcs_flag]
	                    , [total_in_usd]			
	                    , [total_in_usd_jisdor]	
	                    , [branch]				
	                    , [name]					
	                    , [office_name]			
	                    , [no_npwp]				
	                    , [no_identity]			
	                    , [process_time]			
	                    , [guid_process]			
                    )
                    SELECT
	                    customer_id
                        , Product
                        , BuySellCode
                        , Currency
                        , FlagLCS
	                    , InUSD
                        , InUSDJISDOR		                    
                        , ISNULL(branch, '') AS branch
	                    , ISNULL([name], '') AS [name]
	                    , office_name
	                    , npwp
	                    , identity_1
	                    , strProcessTime
                        , @pcGuidProcess
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
                        , Product           VARCHAR(25)
                        , BuySellCode       VARCHAR(1)
                        , Currency          VARCHAR(3)
                        , FlagLCS           BIT
	                    , InUSD			    MONEY
	                    , branch		    VARCHAR(5)
	                    , [name]		    VARCHAR(20)
	                    , office_name	    VARCHAR(50)
	                    , npwp			    VARCHAR(30)
	                    , identity_1	    VARCHAR(50)
	                    --, ProcessTime	    DATETIME
                        , strProcessTime	VARCHAR(50)	                    
                        , InUSDJISDOR	    MONEY   
                    )  

                    ";

                if (period.isDaily)
                {
                    strQuery = strQuery + @"
                    INSERT INTO dbo.[[TableName]]
                    (
                        [customer_id]			
	                    , [product]				
	                    , [buy_sell]				
                        , [lcs_flag]
	                    , [total_in_usd]			
	                    , [total_in_usd_jisdor]	
	                    , [branch]				
	                    , [name]					
	                    , [office_name]			
	                    , [no_npwp]				
	                    , [no_identity]			
	                    , [process_time]			
	                    , [guid_process]			
                    )
                    SELECT 
                        tmp.[customer_id]			
	                    , tmp.[product]				
	                    , tmp.[buy_sell]				
                        , tmp.[lcs_flag]
	                    , tmp.[total_in_usd]			
	                    , tmp.[total_in_usd_jisdor]	
	                    , tmp.[branch]				
	                    , tmp.[name]					
	                    , tmp.[office_name]			
	                    , tmp.[no_npwp]				
	                    , tmp.[no_identity]			
	                    , tmp.[process_time]			
	                    , tmp.[guid_process]			
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tm
                    ON tmp.customer_id = tm.customer_id
                        AND tmp.[product] = tm.[product]
                        AND tmp.[buy_sell] = tm.[buy_sell]
                        AND tmp.[lcs_flag] = tm.[lcs_flag]
                    WHERE ISNULL(tm.customer_id, '') = ''
                    ";
                }
                else
                {
                    strQuery = strQuery + @"                    
                    UPDATE tm
                    SET [total_in_usd] = new.[total_in_usd]
                        , [total_in_usd_jisdor] = new.[total_in_usd_jisdor]                        
                    FROM dbo.[[TableName]] AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id 
                        AND tm.[product] = new.[product]
                        AND tm.[buy_sell] = new.[buy_sell]
                        AND tm.[lcs_flag] = new.[lcs_flag]
                    WHERE 1 = 1
                        AND
                        (
                            tm.[total_in_usd] != new.[total_in_usd]
                            OR tm.[total_in_usd_jisdor] != new.[total_in_usd_jisdor]
                        )

                    INSERT INTO dbo.[[TableName]]
                    (
                        [customer_id]			
	                    , [product]				
	                    , [buy_sell]				
                        , [lcs_flag]
	                    , [total_in_usd]			
	                    , [total_in_usd_jisdor]	
	                    , [branch]				
	                    , [name]					
	                    , [office_name]			
	                    , [no_npwp]				
	                    , [no_identity]			
	                    , [process_time]			
	                    , [guid_process]			
                    )
                    SELECT 
                        tmp.[customer_id]			
	                    , tmp.[product]				
	                    , tmp.[buy_sell]				
                        , tmp.[lcs_flag]
	                    , tmp.[total_in_usd]			
	                    , tmp.[total_in_usd_jisdor]	
	                    , tmp.[branch]				
	                    , tmp.[name]					
	                    , tmp.[office_name]			
	                    , tmp.[no_npwp]				
	                    , tmp.[no_identity]			
	                    , tmp.[process_time]			
	                    , tmp.[guid_process]			                        
                    FROM #tmpSummary AS tmp
                    LEFT JOIN dbo.[[TableName]] AS tm
                    ON tmp.customer_id = tm.customer_id
                        AND tmp.[product] = tm.[product]
                        AND tmp.[buy_sell] = tm.[buy_sell]
                        AND tmp.[lcs_flag] = tm.[lcs_flag]
                    WHERE tm.customer_id is null 

                    UPDATE tm
                    SET [guid_process] = @pcGuidProcess
                    FROM dbo.[[TableName]] AS tm
                    JOIN #tmpSummary AS new
                    ON tm.customer_id = new.customer_id 
                    WHERE ISNULL(tm.[guid_process], 0x0) != @pcGuidProcess ";
                }
                #endregion

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();
                List<Task<ApiMessage>> listDeleteToday = new List<Task<ApiMessage>>();

                List<List<summary_valas>> dataToInsert = new List<List<summary_valas>>();
                List<List<summary_valas>> unParitioned = new List<List<summary_valas>>();
                List<List<summary_valas>> partitioned = new List<List<summary_valas>>();

                int maxIndex = sumaries.Data.Count();

                unParitioned = clsUtils.SplitList(sumaries.Data, maxIndex);

                if (this._connSibs.isDBNISP
                    || this._connObli.isDBNISP)
                {
                    maxIndex = int.Parse(this._configuration["maxIndexRowDBNISPTrx"].ToString());
                    partitioned = clsUtils.SplitList(sumaries.Data, maxIndex);
                }

                if (this._bInsert2ONFX)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.summary_valas)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExecONFX = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strSummaryTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<summary_valas>>();
                        dataToInsert = this._connSFX.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", period.GuidProcess));

                        foreach (List<summary_valas> data in dataToInsert)
                        {
                            if (this._connSFX.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<summary_valas>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Summaries";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strSummaryTodayTableName, "guid_process", period.GuidProcess, this._connSFX)));
                    }
                }

                if (this._bInsert2OBL)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.summary_valas)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExecOBL = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strSummaryTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<summary_valas>>();
                        dataToInsert = this._connObli.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", period.GuidProcess));

                        foreach (List<summary_valas> data in dataToInsert)
                        {
                            if (this._connObli.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<summary_valas>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Summaries";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strSummaryTodayTableName, "guid_process", period.GuidProcess, this._connObli)));
                    }
                }

                if (this._bInsert2SIBS)
                {
                    mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.summary_valas)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    if (mappingInsertTable.ExecuteTable)
                    {
                        string strExecSIBS = strQuery.Replace("[[TableName]]", mappingInsertTable.TableName);
                        string strSummaryTodayTableName = mappingInsertTable.TableName;

                        listInsert = new List<Task<DataSet>>();

                        dataToInsert = new List<List<summary_valas>>();
                        dataToInsert = this._connSibs.isDBNISP ? partitioned : unParitioned;

                        sqlPar = new List<SqlParameter>();
                        sqlPar.Add(new SqlParameter("@pcXmlInput", ""));
                        sqlPar.Add(new SqlParameter("@pcGuidProcess", period.GuidProcess));

                        foreach (List<summary_valas> data in dataToInsert)
                        {
                            if (this._connSibs.isDBNISP)
                            {
                                if (listInsert.Count == this._globalVariable.MaxInsertTask)
                                {
                                    await Task.WhenAll(listInsert);

                                    foreach (Task<DataSet> insert in listInsert)
                                    {
                                        if (insert.IsFaulted)
                                            throw new Exception("Faulted: " + insert.Exception.Message);

                                        if (insert.IsCanceled)
                                            throw new Exception("Canceled: " + insert.Exception.Message);
                                    }

                                    listInsert = new List<Task<DataSet>>();
                                }
                            }

                            dsTran = new DataSet();
                            dtTran = new DataTable();

                            dtTran = this._msHelper.MapListToTable<summary_valas>(data);

                            dsTran.Tables.Add(dtTran);
                            dsTran.DataSetName = "Data";
                            dsTran.Tables[0].TableName = "Summaries";

                            strXml = dsTran.GetXml();

                            sqlPar[0].Value = strXml;

                            listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));
                        }

                        if (listInsert.Count > 0)
                        {
                            await Task.WhenAll(listInsert);

                            foreach (Task<DataSet> insert in listInsert)
                            {
                                if (insert.IsFaulted)
                                    throw new Exception("Faulted: " + insert.Exception.Message);

                                if (insert.IsCanceled)
                                    throw new Exception("Canceled: " + insert.Exception.Message);
                            }
                        }

                        if (!period.isDaily)
                            listDeleteToday.Add(Task.Run(() => this.DeleteDataNotInProcess(strSummaryTodayTableName, "guid_process", period.GuidProcess, this._connSibs)));
                    }
                }

                if (listDeleteToday.Count > 0)
                {
                    await Task.WhenAll(listDeleteToday);

                    foreach (Task<ApiMessage> delete in listDeleteToday)
                    {
                        if (delete.IsFaulted)
                            throw new Exception("Faulted: " + delete.Exception.Message);

                        if (delete.IsCanceled)
                            throw new Exception("Canceled: " + delete.Exception.Message);

                        if (!delete.Result.IsSuccess)
                            throw new Exception("Not Success: " + delete.Result.ErrorDescription);
                    }
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Insert Summary: " + ex.Message;
            }

            return insertRs;
        }
        private async Task<ApiMessage> DeleteDataSummaryValas(CalculateLimitPeriod period)
        {
            ApiMessage insertRs = new ApiMessage();

            try
            {
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                List<Task<DataSet>> listInsert = new List<Task<DataSet>>();

                if (this._bInsert2ONFX)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_OnlineFX))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.summary_valas)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecONFX = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSFX, strExecONFX, sqlPar)));
                }

                if (this._bInsert2OBL)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_TRSRETAIL))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.summary_valas)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecOBL = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connObli, strExecOBL, sqlPar)));
                }

                if (this._bInsert2SIBS)
                {
                    MappingTableName mappingInsertTable = new MappingTableName();
                    mappingInsertTable = this._globalVariable.MappingTables.Where(x =>
                        (
                            !period.isAdHoc && (x.DataBaseName.Equals(DatabaseName.SQL_SIBS))
                            || (period.isAdHoc && x.DataBaseName.Equals(DatabaseName.Adhoc))
                        )
                        && x.DataType.Equals(DataType.summary_valas)
                        && x.IsDaily.Equals(period.isDaily)).FirstOrDefault();

                    string strExecSIBS = @"TRUNCATE TABLE dbo." + mappingInsertTable.TableName;

                    if (mappingInsertTable.ExecuteTable)
                        listInsert.Add(Task.Run(() => this._msHelper.ExecuteQuery(this._connSibs, strExecSIBS, sqlPar)));
                }

                await Task.WhenAll(listInsert);

                foreach (Task<DataSet> insert in listInsert)
                {
                    if (insert.IsFaulted)
                        throw new Exception("Faulted: " + insert.Exception.Message);

                    if (insert.IsCanceled)
                        throw new Exception("Canceled: " + insert.Exception.Message);
                }

                insertRs.IsSuccess = true;
            }
            catch (Exception ex)
            {
                insertRs.IsSuccess = false;
                insertRs.ErrorDescription = "Gagal Delete Summary All product: " + ex.Message;
            }

            return insertRs;
        }
        public async Task<ApiMessage<List<summary_valas>>> InquirySummaryValas(ApiMessage<List<summary_valas>> trans)
        {
            ApiMessage<List<summary_valas>> inqRs = new ApiMessage<List<summary_valas>>();
            inqRs.copyHeaderForReply(trans);

            try
            {
                List<summary_valas> dataInq = new List<summary_valas>();
                List<SqlParameter> sqlPar = new List<SqlParameter>();

                DataSet dsTran = new DataSet();
                DataTable dtTran = new DataTable();

                dtTran = this._msHelper.MapListToTable<summary_valas>(trans.Data);

                var KeepColumn = new List<string> { "customer_id", "Product", "BuySellCode", "FlagLCS" };

                var toRemove = dtTran.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Except(KeepColumn).ToList();

                foreach (var col in toRemove) dtTran.Columns.Remove(col);

                dsTran.Tables.Add(dtTran);
                dsTran.DataSetName = "Data";
                dsTran.Tables[0].TableName = "Summaries";

                string strXml = dsTran.GetXml();

                #region Query Populate Data
                string strQuery = @"
                    IF OBJECT_ID('tempdb..#tmpSummary') IS NOT NULL
	                    DROP TABLE #tmpSummary

                    CREATE TABLE #tmpSummary                     
                    (    
	                    customer_id		VARCHAR(20)
                        , product       VARCHAR(25)
                        , buy_sell      CHAR(1)
                        , lcs_flag      BIT
                    )   

                    DECLARE 
	                    @nDocHandle	INT

                    EXEC sp_xml_preparedocument @nDocHandle output, @pcXmlInput

                    INSERT INTO #tmpSummary
                    (
	                    customer_id
                        , product       
                        , buy_sell      
                        , lcs_flag      
                    )
                    SELECT
	                    customer_id
                        , Product
                        , BuySellCode
                        , FlagLCS
                    FROM openxml(@nDocHandle, N'/Data/Summaries',2)           
                    WITH (
	                    customer_id		    VARCHAR(20)
                        , Product           VARCHAR(25)
                        , BuySellCode       VARCHAR(1)
                        , FlagLCS           VARCHAR(25)
                    )  

                    SELECT	                   
                        tm.[customer_id]                AS 	[customer_id]
	                    , tm.[product]				    AS  [Product]
	                    , tm.[buy_sell]				    AS  [BuySellCode]
	                    , tm.[lcs_flag]				    AS  [FlagLCS]
	                    , tm.[total_in_usd]			    AS  [InUSD]
	                    , tm.[total_in_usd_jisdor]	    AS  [InUSDJISDOR]
	                    , tm.[branch]				    AS  [branch]
	                    , tm.[name]					    AS  [name]
	                    , tm.[office_name]			    AS  [office_name]
	                    , tm.[no_npwp]				    AS  [npwp]
	                    , tm.[no_identity]			    AS  [identity_1]
	                    , tm.[process_time]			    AS  [ProcessTime]
	                    , tm.[guid_process]			    AS  [guid_process]
                    FROM dbo.[summary_valas] AS tm
                    JOIN #tmpSummary AS tmp
                    ON tm.customer_id = tmp.customer_id
                        AND tm.product  = tmp.product     
                        AND tm.buy_sell = tmp.buy_sell     
                        AND tm.lcs_flag = tmp.lcs_flag   
                    ";
                #endregion

                sqlPar.Add(new SqlParameter("@pcXmlInput", strXml));

                //DataSet InqVLS = await this._msHelper.ExecuteQuery(this._connSFX, strQuery, sqlPar);
                DataSet InqVLS = await this._msHelper.ExecuteQuery(this._connObli, strQuery, sqlPar);

                if (InqVLS.Tables.Count > 0)
                    dataInq = JsonConvert.DeserializeObject<List<summary_valas>>(JsonConvert.SerializeObject(InqVLS.Tables[0]));


                inqRs.IsSuccess = true;
                inqRs.Data = dataInq;
            }
            catch (Exception ex)
            {
                inqRs.IsSuccess = false;
                inqRs.ErrorDescription = "Gagal Inquiry summary_valas: " + ex.Message;
            }

            return inqRs;
        }
        //20250701, darul.wahid, ONFX-267, begin
    }
}
