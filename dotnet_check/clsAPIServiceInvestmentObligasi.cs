using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NISPDataSourceNetCore.database;
using NISPDataSourceNetCore.helper;
using NISPDataSourceNetCore.webservice;
using NISPDataSourceNetCore.webservice.model;
using NISPDataSourceNetCore.converter;
using NISPDataSourceNetCore.logger;
using Treasury.Model;

namespace Treasury.Customer.API.Services
{
    public class clsAPIServiceInvestmentObligasi : IServiceInvestmentObligasi
    {
        private readonly IConfiguration _iconfiguration;
        private readonly bool _ignoreSSL;
        private readonly int _nTimeOut = 0;
        private readonly string _strConnString_CIF = "";
        private readonly string _strConnString_OBL = "";
        private readonly string _strConnString_REGSIBS = "";
        private readonly string _strConnString_REG = "";
        private readonly string _strConnString_SOA = "";
        private readonly string _strUrlWsPwd = "";
        private readonly string _strUrlWsObli = "";
        private readonly string _strUrlApiParamObligasi = "";
        private readonly EPV.EPVEnvironmentType _envType;
        //private readonly int _logDaysKeep = 0;
        private readonly string _apiGuid = "";
        private readonly IApiLogger _logger = null;
        //20221216, yudha.n, BONDRETAIL-1154, begin
        private readonly string _strUrlWsOmniObli = "";
        private readonly string _strEncConnString_OBL = "";
        //20221216, yudha.n, BONDRETAIL-1154, end

        public clsAPIServiceInvestmentObligasi(IConfiguration iconfiguration, GlobalVariableList globalVariabelList)
        {
            this._iconfiguration = iconfiguration;
            this._ignoreSSL = globalVariabelList.IgnoreSSL;
            this._nTimeOut = globalVariabelList.TimeOut;
            this._strUrlWsPwd = globalVariabelList.URLWsPwd;
            this._strUrlWsObli = globalVariabelList.URLWsObli;
            this._envType = globalVariabelList.EnvironmentType;
            this._strConnString_CIF = globalVariabelList.ConnectionStringDBCIF;
            this._strConnString_OBL = globalVariabelList.ConnectionStringDBOBL;
            this._strConnString_REGSIBS = globalVariabelList.ConnectionStringDBREGSIBS;
            this._strConnString_REG = globalVariabelList.ConnectionStringDBREG;
            this._strConnString_SOA = globalVariabelList.ConnectionStringDBSOA;
            this._strUrlApiParamObligasi = globalVariabelList.URLApiParamObli;
            this._apiGuid = globalVariabelList.APIGuid;
            //this._logDaysKeep = globalVariabelList.LogDaysKeep;
            this._logger = globalVariabelList.Logger;
            //20221216, yudha.n, BONDRETAIL-1154, begin
            _strUrlWsOmniObli = globalVariabelList.URLWsOmniObli;
            _strEncConnString_OBL = globalVariabelList.EncConnectionStringDBOBL;

            clsCallSPWs._strUrlWsOmniObli = _strUrlWsOmniObli;
            clsCallSPWs._strEncConnectionStringObl = _strEncConnString_OBL;
            //20221216, yudha.n, BONDRETAIL-1154, end
        }

        public ApiMessage<List<BONDInvestmentInquiryResponse>> ObliInquiryInvestment(ApiMessage<BONDInvestmentInquiryRequest> paramIn)
        {
            ApiMessage<List<BONDInvestmentInquiryResponse>> msgResponse = new ApiMessage<List<BONDInvestmentInquiryResponse>>();
            msgResponse.copyHeaderForReply(paramIn);
            msgResponse.MessageDateTime = DateTime.Now;

            string errMsg = "", sqlCmd = "", trxGuid = paramIn.MessageGUID;

            string dCurrentDate = ""
                    , cNama = ""
                    , cSecAccNo = ""
                    , cNoRekInvestor = ""
                    , cCIFNumber = ""
                    , cFlagType = "";

            int nCIFId = 0;
            bool bFlagKarywan;

            SqlParameter[] dbPar;
            DataSet dsOut;
            //msgResponse.Data = new BONDInvestmentInquiryResponse();

            DataTable dtNasabah = new DataTable();
            cCIFNumber = paramIn.Data.CIFNumber;
            cFlagType = paramIn.Data.BondType;
            cCIFNumber = cCIFNumber.Trim();

            if (cCIFNumber.Length < 19)
            {
                cCIFNumber = cCIFNumber.Trim();
                string tmpCIFNumber = "0000000000000000000" + cCIFNumber;
                cCIFNumber = tmpCIFNumber.Substring(tmpCIFNumber.Length - 19, 19);
            }

            try
            {
                #region Mapping Parameter

                dtNasabah = this.InquiryCustomerDetail(cCIFNumber);
                dCurrentDate = this.InquiryCurrentWkDate();

                cNama = dtNasabah.Rows[0]["Nama"].ToString();
                cSecAccNo = dtNasabah.Rows[0]["SecAccNo"].ToString();
                cNoRekInvestor = dtNasabah.Rows[0]["NoRekInvestor"].ToString();
                nCIFId = int.Parse(dtNasabah.Rows[0]["CIFId"].ToString());
                bFlagKarywan = bool.Parse(dtNasabah.Rows[0]["FlagKaryawan"].ToString());

                if (dtNasabah.Rows[0]["Status"].ToString() != "3")
                {
                    throw new Exception("Status Nasabah menunggu approval TROPS");
                }
                #endregion

                #region Query
                sqlCmd = @"
                     DECLARE @tmp_result table (  "
                  + "        CIFNumber              varchar(19)  "
                  + "        ,ClientCode            varchar(10)  "
                  + "        ,ProductName           varchar(80)  "
                  + "        ,ProductCode           varchar(20)  "
                  + "        ,UnitBalance           money        "
                  + "        ,ProductCcy            varchar(3)   "
                  + "        ,NAV			        decimal(20,3) "
                  + "        ,UnitNominal           decimal(22,3) "
                  + "        ,NAVDate		        varchar(12)   "
                  + "        ,SecId		            int   "
                  + "        ,SecCategory	        varchar(120)   "
                  + "        ,SecCategoryID		    varchar(120)   "
                  + "        ,SecCategoryEN		    varchar(120)   "
                  + "        ,MarketSecurityName	varchar(120)   "
                  + "  ) "
                  + " "
                  + "  SELECT @pnCIFId			AS CIFId   "
                  + "          , @pcCIFNumber	AS CIFNo   "
                  + "          , @pcSecAccNo	AS SecAccNo    "
                  + "          , @pcNama		AS Nama,       "
                  + "          CASE WHEN ISNULL(tcan.NoRekInvestor, '') = '' THEN @pcNoRekInvestor else tcan.NoRekInvestor end as NoRekInvestor "
                  + "          , sm.SecId        "
                  + "          , sm.SecDescr     "
                  + "          , sm.SecurityNo   "
                  + "          , sm.SecCcy,      "
                  + "          SUM(CASE st.TrxType   "
                  + "             WHEN 11 THEN ISNULL(st.FaceValue,0)    "
                  + "             WHEN 10 THEN ISNULL(st.FaceValue,0)    "
                  + "             WHEN 9 THEN ISNULL(st.FaceValue,0)     "
                  + "             WHEN 8 THEN -ISNULL(st.FaceValue,0)    "
                  + "             WHEN 7 THEN -ISNULL(st.FaceValue,0)    "
                  + "             WHEN 6 THEN ISNULL(st.FaceValue,0)     "
                  + "             WHEN 4 THEN ISNULL(st.FaceValue,0)     "
                  + "             WHEN 3 THEN -ISNULL(st.FaceValue,0)    "
                  + "             ELSE ISNULL(st.FaceValue,0) END) AS Outstanding  "
                  + "       ,SUM(CASE st.TrxType WHEN 11 THEN ISNULL(st.FaceValue,0) END) AS 'OutstandingBlokir' "
                  + "       ,sm.MarketSecurityName, sm.IsCorporateBond    "
                  + "  INTO #tmpCustomerSaldo    "
                  + "  FROM dbo.SecurityTransaction_TT st "
                  + "  JOIN dbo.SecurityMaster_TM sm     "
                  + "      on st.SecId = sm.SecId        "
                  + "  LEFT JOIN dbo.TRSCustAccNumber_TR tcan "
                  + "      ON tcan.CIFId = @pnCIFId           "
                  + "          AND sm.SecCcy = tcan.CcyCode   "
                  + "  WHERE st.TrxType in (3,4,6,7,8,9,10,11)"
                  + "          AND st.TrxStatus in (3,6,8)    "
                  + "          AND st.CIFId = @pnCIFId        "
                  + "          AND (                          "
                  + "                  (sm.FlagPerdana = 1    "
                    + "                     AND sm.OrderPerdanaFromDate <= @pdCurrentDate"
                    + "                     AND sm.OrderPerdanaToDate >= @pdCurrentDate  "
                    + "                     AND @pcFlagType = 'PR'                       "
                  + "                  )                                                 "
                  + "                  OR                                                "
                  + "                  ( @pcFlagType = 'SC'                              "
                  + "                      AND  (                                        "
                  + "                      sm.OrderPerdanaFromDate > @pdCurrentDate      "
                  + "                          OR sm.OrderPerdanaToDate < @pdCurrentDate "
                  + "                          OR sm.OrderPerdanaFromDate IS NULL        "
                  + "                      )                                             "
                  + "                  )                                                 "
                  + "              )                                                     "
                  + "  GROUP BY tcan.NoRekInvestor, sm.SecId, sm.SecDescr, sm.SecurityNo, sm.SecCcy , sm.MarketSecurityName, sm.IsCorporateBond   "
                  + " "
                  + "  IF @pcFlagType = 'PR' "
                  + "  BEGIN                 "
                  + "      INSERT INTO #tmpCustomerSaldo"
                  + "                  (                "
                    + "                     CIFId,CIFNo,SecAccNo,Nama,NoRekInvestor,SecId,SecDescr,SecurityNo,SecCcy,Outstanding,OutstandingBlokir"
                    + "                     ,MarketSecurityName,IsCorporateBond   "
                  + "                  ) "
                  + "      SELECT @pnCIFId				AS CIFId    "
                  + "              , @pcCIFNumber		AS CIFNo    "
                  + "              , @pcSecAccNo		AS SecAccNo  "
                  + "              , @pcNama			AS Nama      "
                  + "              , tt.NoRekInvestor                "
                  + "              , sm.SecId                        "
                  + "              , sm.SecDescr                     "
                  + "              , sm.SecurityNo                   "
                  + "              , sm.SecCcy                       "
                  + "              , SUM(OrderNominal),0             "
                  + "              , sm.MarketSecurityName,sm.IsCorporateBond"
                  + "      FROM dbo.ORI_Order_TT tt                  "
                  + "      JOIN dbo.ORI_Online_TR tr                 "
                  + "          ON tt.OrderId = tr.OrderId            "
                  + "      JOIN dbo.SecurityMaster_TM sm             "
                  + "          on tt.SecId = sm.SecId                "
                  //20230324, rezakahfi, FIX-xxx, begin
                  //+ "      WHERE tr.ProcessStatus = 0                "
                  //+ "              AND tr.KemenkeuStatusId = 4       "
                  + "      WHERE tr.KemenkeuStatusId = 4               "
                  //20230324, rezakahfi, FIX-xxx, end
                  + "              AND tt.OrderDate > @pdCurrentDate "
                  + "              AND tt.CIFId = @pnCIFId           "
                  + "              AND tr.CIFId = @pnCIFId           "
                  + "      GROUP BY tt.NoRekInvestor                 "
                  + "              , sm.SecId                        "
                  + "              , sm.SecDescr                     "
                  + "              , sm.SecurityNo                   "
                  + "              , sm.SecCcy                       "
                  + "              , sm.MarketSecurityName          "
                  + "              , sm.IsCorporateBond             "
                  + "  END                                          "
                  + "                                               "
                  + "  INSERT @tmp_result (                         "
                  + "      CIFNumber,ClientCode,ProductName,ProductCode,UnitBalance,ProductCcy "
                  + "      ,SecId,SecCategory,SecCategoryID,SecCategoryEN,MarketSecurityName "
                  + "  )                                                                               "
                  + "  SELECT CIFNo					AS CIFNumber                                       "
                  + "          ,SecAccNo				AS ClientCode                                  "
                  + "          ,SecDescr	            AS ProductName                                 "
                  + "          ,SecurityNo				AS ProductCode                                 "
                  + "          ,SUM(Outstanding)		AS UnitBalance                                 "
                  + "          ,SecCcy					AS ProductCcy                                  "
                  + "          ,SecId   "
                  + "          ,CASE WHEN IsCorporateBond = 1 then 'Obligasi Korporasi' ELSE 'Obligasi Pemerintah' END  "
                  + "          ,CASE WHEN IsCorporateBond = 1 then 'Obligasi Korporasi ' ELSE 'Obligasi Pemerintah ' END + SecCcy"
                  + "          ,CASE WHEN IsCorporateBond = 1 then 'Corporate Bond ' ELSE 'Government Bond ' END + SecCcy"
                  + "          ,MarketSecurityName          "
                  + "  FROM #tmpCustomerSaldo                                                          "
                  + "  GROUP BY CIFNo,SecAccNo,SecDescr,Nama,NoRekInvestor,SecId,SecurityNo,SecCcy     "
                  + "           ,SecId,MarketSecurityName,IsCorporateBond                              "
                  + "   "
                  + "  DELETE @tmp_result                                                              "
                  + "  WHERE UnitBalance <= 0                                                          "
                  + "   ";
                //+ "  UPDATE tcs                                                                      "
                //+ "  SET NAV				= ISNULL (tbp.Bidprice, 0.0)                               "
                //+ "      ,UnitNominal	= (tcs.UnitBalance * isnull (tbp.Bidprice, 0.0)) / 100.0       "
                //+ "      ,NAVDate		= convert(varchar(8), isnull(isnull(tbp.LastUpdateDate, tbp.InsertedDate),'1900-12-31'), 112)"
                //+ "  FROM @tmp_result tcs  "
                //+ "  JOIN dbo.TRSECBondsPrice_TM tbp   "
                //+ "      ON tbp.SecurityNo = tcs.ProductCode   "
                if (bFlagKarywan)
                {
                    sqlCmd = sqlCmd + "   "
                              + "  UPDATE tcs           "
                              + "  SET NAV			    = ISNULL(HargaModalBeli,100)+ISNULL(BidSpreadKaryawan,0)                                            "
                              + "      ,UnitNominal	    = (tcs.UnitBalance * isnull ((ISNULL(HargaModalBeli,100)+ISNULL(BidSpreadKaryawan,0)), 0.0)) / 100.0"
                              + "      ,NAVDate		    = CONVERT(VARCHAR(8), hm.LastUpdateDate, 112)           "
                              + "  FROM @tmp_result tcs  "
                              + "  LEFT JOIN dbo.TRSParamHargaModal_TR hm   "
                              + "      ON hm.SecId = tcs.Sec    Id              "
                              + "  LEFT JOIN dbo.TRSECParamSpread_TR spr    "
                              + "      ON spr.SecId = tcs.SecId         "
                              + "      AND spr.NominalFrom <= tcs.UnitBalance   "
                              + "      AND spr.NominalTo > tcs.UnitBalance      "
                              + "      AND spr.BuySell = 'B'";
                }
                else
                {
                    //sqlCmd = sqlCmd + "   "
                    //          + "  UPDATE tcs           "
                    //          + "  SET NAV			    = ISNULL(HargaModalBeli,100)+ISNULL(BidSpread,0)                                                "
                    //          + "      ,UnitNominal	    = (tcs.UnitBalance * isnull ((ISNULL(HargaModalBeli,100)+ISNULL(BidSpread,0)), 0.0)) / 100.0    "
                    //          + "      ,NAVDate		    = CONVERT(VARCHAR(8), hm.LastUpdateDate, 112)           "
                    //          + "  FROM @tmp_result tcs  "
                    //          + "  LEFT JOIN dbo.TRSParamHargaModal_TR hm   "
                    //          + "      ON hm.SecId = tcs.SecId              "
                    //          + "  LEFT JOIN dbo.TRSECParamSpread_TR spr    "
                    //          + "      ON spr.SecId = tcs.SecId             "
                    //          + "      AND spr.NominalFrom <= tcs.UnitBalance   "
                    //          + "      AND spr.NominalTo > tcs.UnitBalance      "
                    //          + "      AND spr.BuySell = 'B'";

                    sqlCmd = sqlCmd + "   "
                     + "  UPDATE tcs                                                                      "
                     + "  SET NAV				= ISNULL (tbp.Bidprice, 0.0)                               "
                     + "      ,UnitNominal	= (tcs.UnitBalance * isnull (tbp.Bidprice, 0.0)) / 100.0       "
                     + "      ,NAVDate		= convert(varchar(8), isnull(isnull(tbp.LastUpdateDate, tbp.InsertedDate),'1900-12-31'), 112)"
                     + "  FROM @tmp_result tcs  "
                     + "  JOIN dbo.TRSECBondsPrice_TM tbp   "
                     + "      ON tbp.SecurityNo = tcs.ProductCode   ";
                }

                sqlCmd = sqlCmd + "   "
                + " "
                + "  DELETE @tmp_result    "
                + "  WHERE NAV < 0 OR UnitNominal < 0  "
                + " "
                + "  drop table #tmpCustomerSaldo  "
                + " "
                + "  SELECT CIFNumber,ClientCode,ProductName,ProductCode,UnitBalance,ProductCcy,NAV,UnitNominal,NAVDate  "
                + "       ,SecId, SecCategory, SecCategoryID, SecCategoryEN, MarketSecurityName           "
                + "  FROM @tmp_result";

                #endregion

                dbPar = new SqlParameter[7];
                dbPar[0] = new SqlParameter("@pdCurrentDate", dCurrentDate);
                dbPar[1] = new SqlParameter("@pnCIFId", nCIFId);
                dbPar[2] = new SqlParameter("@pcNama", cNama);
                dbPar[3] = new SqlParameter("@pcSecAccNo", cSecAccNo);
                dbPar[4] = new SqlParameter("@pcNoRekInvestor", cNoRekInvestor);
                dbPar[5] = new SqlParameter("@pcCIFNumber", cCIFNumber);
                dbPar[6] = new SqlParameter("@pcFlagType", cFlagType);

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsOut, out errMsg))
                    throw new Exception(errMsg);

                if (dsOut == null || dsOut.Tables.Count == 0)
                    throw new Exception("Data not found !");

                //List<BONDInvestmentInquiryResponse> myList = new List<BONDInvestmentInquiryResponse>();
                //msgResponse.Data = myList;
                if (dsOut.Tables[0].Rows.Count > 0)
                    msgResponse.Data = JsonConvert.DeserializeObject<List<BONDInvestmentInquiryResponse>>(JsonConvert.SerializeObject(dsOut.Tables[0]));

                msgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._logger.logError(this, new StackTrace(), ex.Message, paramIn.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;

            }

            return msgResponse;
        }

        #region internal method
        private DataTable InquiryCustomerDetail(string CIFNumber)
        {
            DataSet dsResult = new DataSet();
            DataTable dtResult = new DataTable();

            string errMsg = "", sqlCmd = "";
            SqlParameter[] dbPar;

            if (CIFNumber.Length < 19)
            {
                CIFNumber = CIFNumber.Trim();
                string tmpCIFNumber = "0000000000000000000" + CIFNumber;
                CIFNumber = tmpCIFNumber.Substring(tmpCIFNumber.Length - 19, 19);
            }

            try
            {
                sqlCmd = @"
                        SELECT CIFId
		                    ,CIFNo
		                    ,Nama				
		                    ,SecAccNo			
		                    ,NoRekInvestor
		                    ,JenisIdentitas
		                    ,NoIdentitas
		                    ,TempatLahir
		                    ,TanggalLahir
		                    ,JenisKelamin
		                    ,NPWP
		                    ,[Status]
		                    ,RiskProfile
		                    ,FlagKaryawan
		                    ,FlagPremier
		                    ,RiskProfileExpiredDate
		                    ,IdentitasExpiredDate
		                    ,[SID]
		                    ,Citizenship
		                    ,RiskProfileCode
		                    ,MxLabel
		                    ,IDNeedReview
		                    ,RegViaONEMobile
		                    ,KITASNo
		                    ,KITASExpDate
		                    ,KITASLastUpdateDate
	                    FROM dbo.TreasuryCustomer_TM
	                    WHERE CIFNo = @pcCIFNumber
                ";

                dbPar = new SqlParameter[1];
                dbPar[0] = new SqlParameter("@pcCIFNumber", CIFNumber);

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, ref dbPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count == 0 || dsResult.Tables[0].Rows.Count == 0)
                    throw new Exception("Nasabah not found !");

                dtResult = dsResult.Tables[0].Copy();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return dtResult.Copy();
        }

        private string InquiryCurrentWkDate()
        {
            DataSet dsOut = new DataSet();

            string errMsg = "", sqlCmd = "";
            string strWkDateResult = "";

            try
            {
                sqlCmd = @"
                        SELECT CONVERT(VARCHAR(8),current_working_date,112) AS current_working_date
                        FROM dbo.control_table 
                ";

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsObli, this._ignoreSSL, sqlCmd, out dsOut, out errMsg))
                    throw new Exception(errMsg);

                if (dsOut == null || dsOut.Tables.Count == 0 || dsOut.Tables[0].Rows.Count == 0)
                    throw new Exception("Data not found !");

                strWkDateResult = dsOut.Tables[0].Rows[0]["current_working_date"].ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return strWkDateResult;
        }
        #endregion
    }
}