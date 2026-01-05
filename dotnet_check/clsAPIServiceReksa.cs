using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NISPDataSourceNetCore.converter;
using NISPDataSourceNetCore.database;
using NISPDataSourceNetCore.logger;
using NISPDataSourceNetCore.webservice.model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Wealth.ReksaAccountTransaction.API.Model;
using static NISPDataSourceNetCore.database.EPV;
using static NISPDataSourceNetCore.database.SQLSPParameter;
using static Wealth.ReksaAccountTransaction.API.Model.ReksaRefreshNasabahRes;
using static Wealth.ReksaAccountTransaction.API.Model.ReksaMaintainAllTransaksiNewRes;
using NISPDataSourceNetCore.webservice;
using NISPDataSourceNetCore.ede;
using Newtonsoft.Json.Linq;
//20220614, Andhika J, VELOWEB-1961, begin
using System.Text;
using NISPDataSourceNetCore.helper;
using System.Net.Http;
using System.Xml.Linq;
//20220614, Andhika J, VELOWEB-1961, end
using static Wealth.ReksaAccountTransaction.API.Model.OATransaksiReksaRes;
using System.Globalization;
//20240108, Andhika J, RDN-1119, begin
using NISPDataSourceNetCore.ede.model;
//20240108, Andhika J, RDN-1119, end

namespace Wealth.ReksaAccountTransaction.API.Service
{
    public class clsAPIService : IService
    {/*    
       ==================================================================================================
       Created By      : Andhika J
       Created Date    : 20230206
       Description     : Migrasi MW lama to API 
       Edited          :
       ==================================================================================================
       Date        Editor              Project ID          Description
       ==================================================================================================
       20230206    Andhika J           RDN-903             Migrasi SP ReksaAuthorizeSwitching_BS to service API
       20230228    Andhika J           RDN-903             Migrasi SP ReksaAuthorizeTransaction_BS to service API
       20230306    Andhika J           RDN-903             Migrasi SP ReksaMaintainAllTransaksiNew to service API
       ==================================================================================================
    */
        private IConfiguration _configuration;
        private string _strConnSOA;
        private string _strUrlWsPwd;
        private string _strApiUrlCoba;
        private string _strApiReksaParameter;
        private bool _ignoreSSL;
        private IApiLogger _apiLogger;
        private EPVEnvironmentType _envType;
        private string _strConnReksa;
        private string _strUrlWsReksa;
        private string _strServerIdReksa;
        private string _strDbReksa;
        private string _apiGuid;
        private string _localDataDurationDays;
        private string _apiOAReksaInquiryData;
        private string _apiOAReksaInquiryDataSwitching;
        //20220614, Andhika J, VELOWEB-1961, begin
        private readonly string _url_apiACCInquiryAccountDetail;
        //20220614, Andhika J, VELOWEB-1961, end
        //20230214, Andhika J, RDN-903, begin
        private readonly string _url_apiWealthTransactionBE;
        //20230214, Andhika J, RDN-903, end
        //20240108, Andhika J, RDN-1119, begin
        private readonly string _urlGwEDE;
        private readonly string _urlCoreEDE;
        private readonly string _strUrlMBASE;
        private readonly IApiLogger _iApiLogger;
        private decimal dExpiredDate;
        //20240108, Andhika J, RDN-1119, end

        public clsAPIService(IConfiguration iconfiguration, GlobalVariabelList globalVariabelList)
        {
            this._configuration = iconfiguration;
            this._strConnReksa = globalVariabelList.ConnectionStringDBReksa;
            this._strConnSOA = globalVariabelList.ConnectionStringDBSOA;
            this._envType = globalVariabelList.EnvironmentType;
            this._ignoreSSL = globalVariabelList.IgnoreSSL;
            this._strApiUrlCoba = globalVariabelList.URLApiCoba;
            this._strApiReksaParameter = globalVariabelList.URLAPIReksaParameter;
            //this._strUrlWsPwd = globalVariabelList.URLWsPwd;
            this._strUrlWsReksa = globalVariabelList.URLWsReksa;
            this._localDataDurationDays = globalVariabelList.LocalDataDurationDays;
            this._apiGuid = globalVariabelList.ApiGuid;
            this._apiLogger = globalVariabelList.Logger;
            this._apiOAReksaInquiryData = iconfiguration["apiOAReksaInquiryData"];
            this._apiOAReksaInquiryDataSwitching = iconfiguration["apiOAReksaInquiryDataSwitching"];
            this._strServerIdReksa = iconfiguration["DB_ServerID"];
            this._strDbReksa = iconfiguration["DB_DBName"];
            //20220614, Andhika J, VELOWEB-1961, begin
            this._url_apiACCInquiryAccountDetail = iconfiguration["url_apiACCInquiryAccountDetail"].ToString();
            //20220614, Andhika J, VELOWEB-1961, end
            //20230214, Andhika J, RDN-903, begin
            this._url_apiWealthTransactionBE = iconfiguration["url_apiWealthTransactionBE"].ToString();
            //20230214, Andhika J, RDN-903, end
            //20240108, Andhika J, RDN-1119, begin
            _iApiLogger = globalVariabelList.Logger;
            _urlGwEDE = iconfiguration["url_edeGW"].ToString();
            _urlCoreEDE = iconfiguration["url_abcsCore"].ToString();
            _strUrlMBASE = iconfiguration["url_mbaseCore"].ToString();
            //20240108, Andhika J, RDN-1119, begin

        }

        #region Logger
        /*
        private IApiLogger buildApiLogger()
        {
            int intEPVServerID;
            int intPort;
            int intDBTimeOut;
            IApiLogger apiLogger;
            IDatabaseConnector databaseConnector;
            EPV.EPVEnvironmentType epvEnvironmentType;
            bool boolIgnoreSSLCertificate;

            //* 
            logger butuh konfigurasi logger di JSON appsetting.json
            Environment : UAT / DEV / PROD
            EPVUrl : DB Setting tabel log Kafka
            APIGuid : GUID tiap project beda2, ini untuk pengenal sekaligus key enkripsi password

            Logger_LogDebugBit : 0 / 1
            Logger_LogAPIMessageBit : 0 / 1
            Logger_LogInformationBit : 0 / 1
            Logger_LogWarningBit : 0 / 1
            Logger_LogErrorBit : 0 / 1
            Logger_UseEPVBit : DB Setting tabel log Kafka
            Logger_ServerID : DB Setting tabel log Kafka
            Logger_TimeOut : DB Setting tabel log Kafka
            Logger_IPAddress : DB Setting tabel log Kafka
            Logger_Port : DB Setting tabel log Kafka
            Logger_InstanceName : DB Setting tabel log Kafka
            Logger_DBName : DB Setting tabel log Kafka
            Logger_UserName : DB Setting tabel log Kafka
            Logger_Password : DB Setting tabel log Kafka
            /*-/

            databaseConnector = null;
            apiLogger = null;

            epvEnvironmentType = this._envType;

            boolIgnoreSSLCertificate = false;
            if (_configuration["Logger_EPVIgnoreSSLCertificate"].ToString() == "1")
            {
                boolIgnoreSSLCertificate = true;
            }

            Int32.TryParse(_configuration["Logger_ServerID"], out intEPVServerID);
            Int32.TryParse(_configuration["Logger_Port"], out intPort);
            Int32.TryParse(_configuration["Logger_TimeOut"], out intDBTimeOut);

            databaseConnector = new DatabaseConnectorMsSQL(_configuration["APIGuid"],
                                                            _configuration["Logger_UseEPVBit"].Equals("1"),
                                                            epvEnvironmentType,
                                                            _configuration["EPVUrl"],
                                                            intEPVServerID,
                                                            _configuration["Logger_IPAddress"],
                                                            intPort,
                                                            _configuration["Logger_InstanceName"],
                                                            _configuration["Logger_DBName"],
                                                            _configuration["Logger_UserName"],
                                                            _configuration["Logger_Password"],
                                                            boolIgnoreSSLCertificate);
            databaseConnector.setDBTimeOut(intDBTimeOut);

            apiLogger = new ApiLoggerKafka(databaseConnector,
                                            _configuration["APIGuid"],
                                            _configuration["Logger_LogAPIMessageBit"] == "1",
                                            _configuration["Logger_LogInformationBit"] == "1",
                                            _configuration["Logger_LogWarningBit"] == "1",
                                            _configuration["Logger_LogErrorBit"] == "1",
                                            _configuration["Logger_LogDebugBit"] == "1");


            return apiLogger;
        }
        */
        #endregion

        #region PreSaveValidation

        #region SUB REDEMP

        #region ReksaCheckCIF
        public ApiMessage ReksaCheckCIF (ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {           
            ApiMessage apiMessageResponse = new ApiMessage();
            apiMessageResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            string spName = "dbo.ReksaCheckCIF", errMsg = "";

            string nCIF = paramIn.Data.CIFNo.PadLeft(13, '0');

            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pcCIFNo", nCIF, 20));
            //sqlPar.Add(new SQLSPParameter("@pcErrMsgOut", "", ParamDirection.OUTPUT));
            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }

            return apiMessageResponse;
        }
        #endregion ReksaCheckCIF

        #region ReksaRefreshNasabah
        public ApiMessage<ReksaRefreshNasabahRes> ReksaRefreshNasabah(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<ReksaRefreshNasabahRes> apiMsgResponse = new ApiMessage<ReksaRefreshNasabahRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            DataSet dsResult;

            ReksaRefreshNasabahRes responseClass = new ReksaRefreshNasabahRes();
            List<Response1> listResponse = new List<Response1>();
            List<Response2> listResponse2 = new List<Response2>();
            
            string errMsg = "";
            string strSPName = "dbo.ReksaRefreshNasabah";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
                param.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                param.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                #region Data1
                //Data 1
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    listResponse = JsonConvert.DeserializeObject<List<Response1>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    responseClass.Data1 = listResponse[0];
                }
                else
                    throw new Exception("Data ReksaRefreshNasabah Detail not found !");
                #endregion

                #region Data 2
                //Data2
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[1].Rows.Count > 0)
                {
                    listResponse2 = JsonConvert.DeserializeObject<List<Response2>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[1],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    responseClass.Data2 = listResponse2[0];
                }
                else
                    throw new Exception("Data ReksaRefreshNasabah Risk Profile not found !");
                #endregion Data 2

                //insert ke apimsgresponse
                apiMsgResponse.Data = responseClass;
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }
        #endregion ReksaRefreshNasabah

        #region GetDetailProduct
        public ApiMessage<ValidasiGetDetailProductRes> GetDetailProduct(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<ValidasiGetDetailProductRes> apiMsgResponse = new ApiMessage<ValidasiGetDetailProductRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<ValidasiGetDetailProductRes> response = new List<ValidasiGetDetailProductRes>();
            List<SQLSPParameter> param;
            DataSet dsResult;
            string errMsg = "";

            string Guid = "";
            string NIK = "";
            string Office = "";
            int SearchId = 0;
            string Col1 = "";
            string Col2 = "";
            bool Validate = false;
            string Criteria = "";
            string SearchDesc = "";

            if (paramIn.Data.Subscription != null )
            {
                Guid = "";
                NIK = "";
                Office = "";
                SearchId = 0;
                Col1 = paramIn.Data.Subscription[0].KodeProduk;
                Col2 = "";
                Validate = false;
                Criteria = "SUBS#";
                SearchDesc = "REKSA_TRXPRODUCT";
            }
                
            if (paramIn.Data.Redemption != null)
            {
                Guid = "";
                NIK = "";
                Office = "";
                SearchId = 1;
                Col1 = paramIn.Data.Redemption[0].KodeProduk;
                Col2 = "";
                Validate = false;
                Criteria = "REDEMP#"+paramIn.Data.CIFNo;
                SearchDesc = "REKSA_TRXPRODUCT";
            }
            
            if (paramIn.Data.SubsRDB != null)
            {
                Guid = "";
                NIK = "";
                Office = "";
                SearchId = 1;
                Col1 = paramIn.Data.SubsRDB[0].KodeProduk;
                Col2 = "";
                Validate = false;
                Criteria = "SUBSRDB#";
                SearchDesc = "REKSA_TRXPRODUCT";
            }
            
            //exec SearchExecute '','','','0','ADOUN','',0,'SUBS#','REKSA_TRXPRODUCT'
            //exec SearchExecute '','','','1','','',0,'SUBSRDB#','REKSA_TRXPRODUCT'
            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cGuid", Guid));
                param.Add(new SQLSPParameter("@iNik", NIK));
                param.Add(new SQLSPParameter("@cOffice", Office));
                param.Add(new SQLSPParameter("@iSearchId", SearchId));
                param.Add(new SQLSPParameter("@cCol1", Col1));
                param.Add(new SQLSPParameter("@cCol2", Col2));
                param.Add(new SQLSPParameter("@bValidate", Validate));
                param.Add(new SQLSPParameter("@cCriteria", Criteria));
                param.Add(new SQLSPParameter("@cSearchDesc", SearchDesc));


                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "SearchExecute", ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    response = JsonConvert.DeserializeObject<List<ValidasiGetDetailProductRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    apiMsgResponse.Data = response[0];
                    apiMsgResponse.IsSuccess = true;
                }
                else
                    throw new Exception("Data GetDetailProduct Detail not found !");

            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }

        #endregion GetDetailProduct

        #region ReksaCheckSubsType
        public ApiMessage<ValidasiReksaCheckSubsTypeRes> ReksaCheckSubsType(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<ValidasiReksaCheckSubsTypeRes> apiMessageResponse = new ApiMessage<ValidasiReksaCheckSubsTypeRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            string spName = "ReksaCheckSubsType", errMsg = "";

            ValidasiReksaCheckSubsTypeRes response = new ValidasiReksaCheckSubsTypeRes();

            int ProdId = GetDetailProduct(paramIn).Data.ProdId;

            bool bTrxTaxAmnesty = false;
            if(paramIn.Data.Subscription != null)
            {
                bTrxTaxAmnesty = Convert.ToBoolean(paramIn.Data.Subscription[0].TrxTaxAmnesty);
            }
            if (paramIn.Data.Redemption != null)
            {
                bTrxTaxAmnesty = paramIn.Data.Redemption[0].TrxTaxAmnesty;
            }
            if (paramIn.Data.SubsRDB != null)
            {
                bTrxTaxAmnesty = paramIn.Data.SubsRDB[0].TrxTaxAmnesty;
            }

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 20));
            sqlPar.Add(new SQLSPParameter("@pnProductId", ProdId));
            sqlPar.Add(new SQLSPParameter("@pbIsRDB", false));
            sqlPar.Add(new SQLSPParameter("@pbIsSubsNew", false, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pcClientCode", "", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pbIsTrxTA", Convert.ToInt16(bTrxTaxAmnesty)));
            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (sqlPar[4].ParameterValue.ToString() != null || sqlPar[3].ParameterValue.ToString() != null)
                {
                    response.isSubsNew = sqlPar[3].ParameterValue.ToString() == "0" ? false : true;
                    response.ClientCode = sqlPar[4].ParameterValue.ToString();

                    apiMessageResponse.Data = response;
                    apiMessageResponse.IsSuccess = true;
                }

                else
                    throw new Exception("Gagal exec sp ReksaCheckSubsType");
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }

            return apiMessageResponse;
        }
        #endregion

        #region getClientId
        public string getClientId(string cif, string kodeProduct, bool trxTaxAmnesty, string ClientCode)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            string result = "", errMsg = "";
            DataSet dsOut;
            bool boolExecQuery;
            // set default out parameter 
            //cek tax amnesty
            int taxAmensty = 0;
            if (trxTaxAmnesty == true)
            {
                taxAmensty = 1;
            }

            string Guid = "";
            string NIK = "";
            string Office = "";
            int SearchId = 0;
            string Col1 = ClientCode != "" ? ClientCode : "";
            string Col2 = "";
            bool Validate = false;
            string Criteria = cif.ToString() + "#" + kodeProduct.ToString() + "#" + "REDEMP#" + taxAmensty.ToString();
            string SearchDesc = "REKSA_TRXCLIENTNEW";


            //exec SearchExecute '','','','0','','',0,'0000000111710#RSDPP#REDEMP#0','REKSA_TRXCLIENTNEW' --> 0 trx non tax amnesty
            //exec SearchExecute '','','','0','','',0,'0000000111710#RSDPP#REDEMP#1','REKSA_TRXCLIENTNEW' --> 1 trx tax amensty

            //parameter
            List<SQLSPParameter> lstSQLParam = new List<SQLSPParameter>();
            lstSQLParam.Add(new SQLSPParameter("@cGuid", Guid));
            lstSQLParam.Add(new SQLSPParameter("@iNik", NIK));
            lstSQLParam.Add(new SQLSPParameter("@cOffice", Office));
            lstSQLParam.Add(new SQLSPParameter("@iSearchId", SearchId));
            lstSQLParam.Add(new SQLSPParameter("@cCol1", Col1));
            lstSQLParam.Add(new SQLSPParameter("@cCol2", Col2));
            lstSQLParam.Add(new SQLSPParameter("@bValidate", Validate));
            lstSQLParam.Add(new SQLSPParameter("@cCriteria", Criteria));
            lstSQLParam.Add(new SQLSPParameter("@cSearchDesc", SearchDesc));
            try
            {
                //databaseConnector.execSP(TransactionType.HANDLED_IN_STORED_PROCEDURE, "SearchExecute", lstSQLParam, out dsOut);
                boolExecQuery = clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "SearchExecute", ref lstSQLParam, out dsOut, out errMsg);
                databaseConnector.Dispose();
                databaseConnector = null;
                if (boolExecQuery)
                {
                    result = dsOut.Tables[0].Rows[0]["ClientId"].ToString();
                }
            }
            catch (Exception)
            {
                //_apiLogger.logError(this, new StackTrace(false), "Error Exception exec sp ReksaGenerateTranCodeClientCode" + ex);
                result = "-1";
            }
            //close connection
            if (databaseConnector != null)
            {
                databaseConnector.Dispose();
            }
            databaseConnector = null;
            return result;
        }
        #endregion getClientId

        #region LastBalace
        public decimal getLastBalace(string ClientId, string NIK, string Guid)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            decimal result = 0;
            bool boolExecQuery;
            string errMsg = "";

            // set default out parameter 
            float UnitBalance = 0;

            //@pnClientId                         int,
            //@pnNIK                              int,
            //@pcGuid                             varchar(50),
            //@pmUnitBalance                      decimal(25, 13)          output

            //parameter
            List<SQLSPParameter> lstSQLParam = new List<SQLSPParameter>();
            lstSQLParam.Add(new SQLSPParameter("@pnClientId", ClientId));
            lstSQLParam.Add(new SQLSPParameter("@pnNIK", NIK));
            lstSQLParam.Add(new SQLSPParameter("@pcGuid", Guid));
            lstSQLParam.Add(new SQLSPParameter("@pmUnitBalance", UnitBalance, 20, ParamDirection.INPUT_OUTPUT));
            try
            {
                //databaseConnector.execSP(TransactionType.HANDLED_IN_STORED_PROCEDURE, "ReksaGetLatestBalance", lstSQLParam);
                boolExecQuery = clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaGetLatestBalance", ref lstSQLParam, out errMsg);
                databaseConnector.Dispose();
                databaseConnector = null;
                if (boolExecQuery)
                {
                    result = Convert.ToDecimal(lstSQLParam[3].ParameterValue.ToString());
                }
            }
            catch (Exception)
            {
                //_apiLogger.logError(this, new StackTrace(false), "Error Exception exec sp ReksaGetLatestBalance" + ex);
                result = -1;
            }
            //close connection
            if (databaseConnector != null)
            {
                databaseConnector.Dispose();
            }
            databaseConnector = null;
            return result;
        }
        #endregion LastBalace

        #region ReksaCalcFee 
        public ApiMessage<ValidasiReksaCalcFee> ReksaCalcFee(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<ValidasiReksaCalcFee> msgResponse = new ApiMessage<ValidasiReksaCalcFee>();
            msgResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            string errMsg = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            string spName = "ReksaCalcFee";

            List<ValidasiReksaCalcFee> clsResponse = new List<ValidasiReksaCalcFee>();

            int ntrxTaxAmnesty = 0;
            bool btrxTaxAmnesty = false;
            string cKodeProduct = "";
            string strClientCode = "";

            int nTranType = 1;

            if (paramIn.Data.Subscription != null)
            {
                cKodeProduct = paramIn.Data.Subscription[0].KodeProduk;
                ntrxTaxAmnesty = paramIn.Data.Subscription[0].TrxTaxAmnesty;

                bool bIsNew = ReksaCheckSubsType(paramIn).Data.isSubsNew;
               
                if (bIsNew == false)
                    nTranType = 2;
            }

            if (paramIn.Data.Redemption != null)
            {
                cKodeProduct = paramIn.Data.Redemption[0].KodeProduk;
                ntrxTaxAmnesty = Convert.ToInt16(paramIn.Data.Redemption[0].TrxTaxAmnesty);
                nTranType = 3;
                if (paramIn.Data.Redemption[0].IsRedempAll)
                    nTranType = 4;

                strClientCode = paramIn.Data.Redemption[0].ClientCode;
            }

            if (paramIn.Data.SubsRDB != null)
            {
                cKodeProduct = paramIn.Data.SubsRDB[0].KodeProduk;
                ntrxTaxAmnesty = Convert.ToInt16(paramIn.Data.SubsRDB[0].TrxTaxAmnesty);
            }
                
            //ambil data
            int nProdId = GetDetailProduct(paramIn).Data.ProdId;
            
            if (ntrxTaxAmnesty == 1)
                btrxTaxAmnesty = true;

            string cClientId = getClientId(paramIn.Data.CIFNo, cKodeProduct, btrxTaxAmnesty, strClientCode);
            if (cClientId == "-1")
                cClientId = "0";

            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            int skipResult = 0;

            try
            {
                if(paramIn.Data.Subscription != null)
                {
                    sqlPar = new List<SQLSPParameter>();
                    sqlPar.Add(new SQLSPParameter("@pnProdId", nProdId));
                    sqlPar.Add(new SQLSPParameter("@pnClientId", cClientId));
                    sqlPar.Add(new SQLSPParameter("@pnTranType", nTranType));
                    sqlPar.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.Subscription[0].Nominal));
                    sqlPar.Add(new SQLSPParameter("@pmUnit", 0));
                    sqlPar.Add(new SQLSPParameter("@pcFeeCCY", paramIn.Data.Subscription[0].FeeCurr = "", ParamDirection.OUTPUT)); //5
                    sqlPar.Add(new SQLSPParameter("@pnFee", paramIn.Data.Subscription[0].NominalFee != 0 ? paramIn.Data.Subscription[0].NominalFee : 0 , ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                    sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID));
                    sqlPar.Add(new SQLSPParameter("@pmNAV", 0));
                    sqlPar.Add(new SQLSPParameter("@pbFullAmount", paramIn.Data.Subscription[0].FullAmount));
                    sqlPar.Add(new SQLSPParameter("@pbIsByPercent", true));
                    sqlPar.Add(new SQLSPParameter("@pbIsFeeEdit", paramIn.Data.Subscription[0].EditFee));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageFeeInput", paramIn.Data.Subscription[0].PctFee));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageFeeOutput", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pbProcess", false));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased", 0, ParamDirection.OUTPUT));//16
                    sqlPar.Add(new SQLSPParameter("@pmRedempUnit", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmRedempDev", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pbByUnit", 0));
                    sqlPar.Add(new SQLSPParameter("@pbDebug", false));
                    sqlPar.Add(new SQLSPParameter("@pmProcessTranId", 0));
                    sqlPar.Add(new SQLSPParameter("@pmErrMsg", "", 200, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnOutType", 0));
                    sqlPar.Add(new SQLSPParameter("@pdValueDate", DBNull.Value)); //24
                    sqlPar.Add(new SQLSPParameter("@pmTaxFeeBased", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased3", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased4", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased5", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnPeriod", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnIsRDB", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 20));//31
                }
                else if (paramIn.Data.Redemption != null)
                {
                    string strQuery = "";
                    DataSet dsResult = null;

                    bool forBool = false;
                    int forNumber = 0;

                    strQuery = @"
                            declare @pcFeeCCY varchar(8000)
                            set @pcFeeCCY=NULL
                            declare @pnFee varchar(8000)
                            set @pnFee=NULL
                            declare @pdPercentageFeeOutput varchar(8000)
                            set @pdPercentageFeeOutput=NULL
                            declare @pmFeeBased varchar(8000)
                            set @pmFeeBased=NULL
                            declare @pmRedempUnit varchar(8000)
                            set @pmRedempUnit=NULL
                            declare @pmRedempDev varchar(8000)
                            set @pmRedempDev=NULL
                            declare @pmErrMsg varchar(8000)
                            set @pmErrMsg=NULL
                            declare @pmTaxFeeBased varchar(8000)
                            set @pmTaxFeeBased=NULL
                            declare @pmFeeBased3 varchar(8000)
                            set @pmFeeBased3=NULL
                            declare @pmFeeBased4 varchar(8000)
                            set @pmFeeBased4=NULL
                            declare @pmFeeBased5 varchar(8000)
                            set @pmFeeBased5=NULL
                            declare @pnPeriod varchar(8000)
                            set @pnPeriod=NULL
                            declare @pnIsRDB varchar(8000)
                            set @pnIsRDB=NULL
                            exec dbo.ReksaCalcFee @pnProdId, @pnClientId, @pnTranType, @pmTranAmt, @pmUnit, @pcFeeCCY output, @pnFee output,
                            @pnNIK, @pcGuid, @pmNAV, @pbFullAmount, @pbIsByPercent, @pbIsFeeEdit, @pdPercentageFeeInput,
                            @pdPercentageFeeOutput output, @pbProcess, @pmFeeBased output, @pmRedempUnit output, @pmRedempDev output, @pbByUnit, @pbDebug, @pmProcessTranId,    
                            @pmErrMsg output, @pnOutType, null, @pmTaxFeeBased output, @pmFeeBased3 output,
                            @pmFeeBased4 output, @pmFeeBased5 output, @pnPeriod output, @pnIsRDB output, @pcCIFNo
                            select @pcFeeCCY as FeeCCY, @pnFee as Fee, @pdPercentageFeeOutput as PercentageFeeOutput, @pmFeeBased as FeeBased, @pmRedempUnit as RedempUnit, @pmRedempDev as RedempDev, @pmErrMsg as ErrMsg, @pmTaxFeeBased as TaxFeeBased,  @pmFeeBased3 as FeeBased3, @pmFeeBased4 as FeeBased4, @pmFeeBased5 as FeeBased5, 
                            @pnPeriod as Period, @pnIsRDB as IsRDB
                        ";

                    SqlParameter[] sqlParam = new SqlParameter[18];
                    sqlParam[0] = new SqlParameter("@pnProdId", nProdId);
                    sqlParam[1] = new SqlParameter("@pnClientId", cClientId);
                    sqlParam[2] = new SqlParameter("@pnTranType", nTranType);
                    sqlParam[3] = new SqlParameter("@pmTranAmt", forNumber = 0);
                    sqlParam[4] = new SqlParameter("@pmUnit", paramIn.Data.Redemption[0].RedempUnit);
                    sqlParam[5] = new SqlParameter("@pnNIK", paramIn.UserNIK);
                    sqlParam[6] = new SqlParameter("@pcGuid", paramIn.MessageGUID);
                    sqlParam[7] = new SqlParameter("@pmNAV", forNumber = 0);
                    sqlParam[8] = new SqlParameter("@pbFullAmount", forBool = true);
                    sqlParam[9] = new SqlParameter("@pbIsByPercent", forBool = true);
                    sqlParam[10] = new SqlParameter("@pbIsFeeEdit", paramIn.Data.Redemption[0].EditFee);
                    sqlParam[11] = new SqlParameter("@pdPercentageFeeInput", paramIn.Data.Redemption[0].PctFee);
                    sqlParam[12] = new SqlParameter("@pbProcess", forBool = false);
                    sqlParam[13] = new SqlParameter("@pbByUnit", forBool = true);
                    sqlParam[14] = new SqlParameter("@pbDebug", forBool = false);
                    sqlParam[15] = new SqlParameter("@pmProcessTranId", forNumber = 0);
                    sqlParam[16] = new SqlParameter("@pnOutType", forNumber = 0);
                    sqlParam[17] = new SqlParameter("@pcCIFNo", paramIn.Data.CIFNo);//31


                    if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, strQuery, ref sqlParam, out dsResult, out errMsg))
                    {
                        if (errMsg != "")
                            throw new Exception(errMsg);

                        msgResponse.Data = new ValidasiReksaCalcFee();
                        msgResponse.Data.FeeCCY = dsResult.Tables[0].Rows[0]["FeeCCY"].ToString();
                        msgResponse.Data.Fee = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["Fee"].ToString());
                        msgResponse.Data.PercentageFeeOutput = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["PercentageFeeOutput"].ToString());
                        msgResponse.Data.FeeBased = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased"].ToString());
                        msgResponse.Data.RedempUnit = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["RedempUnit"].ToString());
                        msgResponse.Data.RedempDev = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["RedempDev"].ToString());
                        msgResponse.Data.TaxFeeBased = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["TaxFeeBased"].ToString());
                        msgResponse.Data.FeeBased3 = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased3"].ToString());
                        msgResponse.Data.FeeBased4 = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased4"].ToString());
                        msgResponse.Data.FeeBased5 = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased5"].ToString());
                        msgResponse.Data.Period = Convert.ToInt16(dsResult.Tables[0].Rows[0]["Period"].ToString());
                        msgResponse.Data.IsRDB = Convert.ToInt16(dsResult.Tables[0].Rows[0]["IsRDB"].ToString());
                        msgResponse.IsSuccess = true;

                        skipResult = 1;

                    }
                }

                else if (paramIn.Data.SubsRDB != null)
                {
                    sqlPar = new List<SQLSPParameter>();
                    sqlPar.Add(new SQLSPParameter("@pnProdId", nProdId));
                    sqlPar.Add(new SQLSPParameter("@pnClientId", cClientId));
                    sqlPar.Add(new SQLSPParameter("@pnTranType", 5));
                    sqlPar.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.SubsRDB[0].Nominal));
                    sqlPar.Add(new SQLSPParameter("@pmUnit", 0));
                    sqlPar.Add(new SQLSPParameter("@pcFeeCCY", paramIn.Data.SubsRDB[0].FeeCurr = "", ParamDirection.OUTPUT)); //5
                    //20230728, Filian, RDN-1030, begin
                    //sqlPar.Add(new SQLSPParameter("@pnFee", paramIn.Data.SubsRDB[0].NominalFee = 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnFee", paramIn.Data.SubsRDB[0].NominalFee != 0 ? paramIn.Data.SubsRDB[0].NominalFee : 0, ParamDirection.OUTPUT));
                    //20230728, Filian, RDN-1030, end

                    sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                    sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID));
                    sqlPar.Add(new SQLSPParameter("@pmNAV", 0));
                    sqlPar.Add(new SQLSPParameter("@pbFullAmount", true));
                    sqlPar.Add(new SQLSPParameter("@pbIsByPercent", false));
                    sqlPar.Add(new SQLSPParameter("@pbIsFeeEdit", paramIn.Data.SubsRDB[0].EditFee));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageFeeInput", paramIn.Data.SubsRDB[0].PctFee));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageFeeOutput", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pbProcess", false));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased", 0, ParamDirection.OUTPUT));//16
                    sqlPar.Add(new SQLSPParameter("@pmRedempUnit", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmRedempDev", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pbByUnit", 0));
                    sqlPar.Add(new SQLSPParameter("@pbDebug", false));
                    sqlPar.Add(new SQLSPParameter("@pmProcessTranId", 0));
                    sqlPar.Add(new SQLSPParameter("@pmErrMsg", "", 200, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnOutType", 0));
                    sqlPar.Add(new SQLSPParameter("@pdValueDate", DBNull.Value)); //24
                    sqlPar.Add(new SQLSPParameter("@pmTaxFeeBased", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased3", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased4", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pmFeeBased5", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnPeriod", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pnIsRDB", 0, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 20));//31
                }

                // jika hitungan selain redempt
                if (skipResult == 0)
                {
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsDataOut, out errMsg))
                        throw new Exception(errMsg);

                    errMsg = sqlPar[22].ParameterValue.ToString();

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);


                    msgResponse.Data = new ValidasiReksaCalcFee();
                    msgResponse.Data.FeeCCY = sqlPar[5].ParameterValue != DBNull.Value ? sqlPar[5].ParameterValue.ToString() : "";
                    msgResponse.Data.Fee = sqlPar[6].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[6].ParameterValue.ToString()) : 0;
                    msgResponse.Data.PercentageFeeOutput = sqlPar[14].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[14].ParameterValue.ToString()) : 0;
                    msgResponse.Data.FeeBased = sqlPar[16].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[16].ParameterValue.ToString()) : 0;
                    msgResponse.Data.RedempUnit = sqlPar[17].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[17].ParameterValue.ToString()) : 0;
                    msgResponse.Data.RedempDev = sqlPar[18].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[18].ParameterValue.ToString()) : 0;
                    msgResponse.Data.TaxFeeBased = sqlPar[25].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[25].ParameterValue.ToString()) : 0;
                    msgResponse.Data.FeeBased3 = sqlPar[26].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[26].ParameterValue.ToString()) : 0;
                    msgResponse.Data.FeeBased4 = sqlPar[27].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[27].ParameterValue.ToString()) : 0;
                    msgResponse.Data.FeeBased5 = sqlPar[28].ParameterValue != DBNull.Value ? Convert.ToDecimal(sqlPar[28].ParameterValue.ToString()) : 0;
                    if (sqlPar[29].ParameterValue != DBNull.Value)
                    {
                        if (sqlPar[29].ParameterValue.ToString() != "")
                        {
                            msgResponse.Data.Period = Convert.ToInt16(sqlPar[30].ParameterValue.ToString());
                        }
                        else
                        {
                            msgResponse.Data.Period = 0;
                        }
                    }
                    else
                    {
                        msgResponse.Data.Period = 0;
                    }
                    //msgResponse.Data.Period = sqlPar[29].ParameterValue != DBNull.Value ?  Convert.ToInt16(sqlPar[29].ParameterValue.ToString()) : 0;
                    msgResponse.Data.IsRDB = sqlPar[30].ParameterValue != DBNull.Value ? Convert.ToInt16(sqlPar[30].ParameterValue.ToString()) : 0;
                    msgResponse.IsSuccess = true;
                }

                
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }
        #endregion ReksaCalcFee

        #region ReksaGenerateTranCodeClientCode
        public ApiMessage<GenerateTranCodeClientCodeRes> ReksaGenerateTranCodeClientCode(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<GenerateTranCodeClientCodeRes> apiMessageResponse = new ApiMessage<GenerateTranCodeClientCodeRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            DataSet dsParamOut = new DataSet();
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            //20220425, Rendy, M32022-4, begin
            //string spName = "API_ReksaGenerateTranCodeClientCode", errMsg = "";
            string spName = "ReksaGenerateTranCodeClientCode", errMsg = "";
            //20220425, Rendy, M32022-4, end

            GenerateTranCodeClientCodeRes result = new GenerateTranCodeClientCodeRes();

            //initial
            string nTranCode = "";
            string nNewClientCode = "";
            string nstrWarnMsg = "";
            string nstrWarnMsg2 = "";
            //20220425, Rendy, M32022-4, begin
            //string nErrMsgOut = "";
            //20220425, Rendy, M32022-4, end

            // set default out parameter
            string nJenisTrx = "";
            string nKodeProduct = "";
            bool bIsFeeEdit = false;
            decimal dPercentageFee = 0;
            decimal nNominalFee = 0;
            decimal nNominal = 0;
            decimal nRedempUnit = 0;
            bool bIsRedempAll = false;
            int nFrekPendebetan = 0;
            int nJangkaWaktu = 0;

            //20221111, Lita, RDN-876, begin
            string sFrekDebetMethod = "";
            string sRDBIns = "";
            string sTglDebetRDB = "";
            //20221111, Lita, RDN-876, end
            //20250917, Andhika J, RDN-1264, begin
            bool bIsSubsNew = false;
            string nClientCode = "";
            int nType = 0;
            //20250917, Andhika J, RDN-1264, end

            if (paramIn.Data.Subscription != null)
            {
                nJenisTrx = "SUBS";
                nKodeProduct = paramIn.Data.Subscription[0].KodeProduk;
                bIsFeeEdit = paramIn.Data.Subscription[0].EditFee;
                dPercentageFee = paramIn.Data.Subscription[0].PctFee;
                nNominalFee = paramIn.Data.Subscription[0].NominalFee;
                nNominal = paramIn.Data.Subscription[0].Nominal;
                //20250917, Andhika J, RDN-1264, begin
                nClientCode = paramIn.Data.Subscription[0].ClientCode;
                //20250917, Andhika J, RDN-1264, end
            }

            if (paramIn.Data.Redemption != null)
            {
                nJenisTrx = "REDEMP";
                nKodeProduct = paramIn.Data.Redemption[0].KodeProduk;
                bIsFeeEdit = paramIn.Data.Redemption[0].EditFee;
                dPercentageFee = paramIn.Data.Redemption[0].PctFee;
                nNominalFee = paramIn.Data.Redemption[0].NominalFee;
                nNominal = 0;
                nRedempUnit = paramIn.Data.Redemption[0].RedempUnit;
                bIsRedempAll = paramIn.Data.Redemption[0].IsRedempAll;
                //20250917, Andhika J, RDN-1264, begin
                nClientCode = paramIn.Data.Redemption[0].ClientCode;
                //20250917, Andhika J, RDN-1264, end
            }

            if (paramIn.Data.SubsRDB != null)
            {
                nJenisTrx = "SUBSRDB";
                nKodeProduct = paramIn.Data.SubsRDB[0].KodeProduk;
                bIsFeeEdit = paramIn.Data.SubsRDB[0].EditFee;
                dPercentageFee = paramIn.Data.SubsRDB[0].PctFee;
                nNominalFee = paramIn.Data.SubsRDB[0].NominalFee;
                nNominal = paramIn.Data.SubsRDB[0].Nominal;
                nFrekPendebetan = paramIn.Data.SubsRDB[0].FrekPendebetan;
                nJangkaWaktu = paramIn.Data.SubsRDB[0].JangkaWaktu;
                //20221111, Lita, RDN-876, begin
                sFrekDebetMethod = paramIn.Data.SubsRDB[0].FrekDebetMethodValue;
                sRDBIns = paramIn.Data.SubsRDB[0].Asuransi;
                sTglDebetRDB = paramIn.Data.SubsRDB[0].TanggalDebet;

                //20221111, Lita, RDN-876, end
                //20250917, Andhika J, RDN-1264, begin
                nClientCode = paramIn.Data.SubsRDB[0].ClientCode;
                //20250917, Andhika J, RDN-1264, end
            }
            //20250917, Andhika J, RDN-1264, begin
            //bool bIsSubsNew = ReksaCheckSubsType(paramIn).Data.isSubsNew;
            //string nClientCode = ReksaCheckSubsType(paramIn).Data.ClientCode;
            //int nType = 0;
            //if (bIsSubsNew == false)
            //    nType = 1;
            if (paramIn.Data.Subscription != null || paramIn.Data.SubsRDB != null)
            {
                bIsSubsNew = ReksaCheckSubsType(paramIn).Data.isSubsNew;
                nClientCode = ReksaCheckSubsType(paramIn).Data.ClientCode;
                nType = 0;
                if (bIsSubsNew == false)
                    nType = 1;
            }
            //20250917, Andhika J, RDN-1264, end

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pcJenisTrx", nJenisTrx, 20));
            sqlPar.Add(new SQLSPParameter("@pbIsSubsNew", bIsSubsNew));
            sqlPar.Add(new SQLSPParameter("@pcProdCode", nKodeProduct, 10));
            sqlPar.Add(new SQLSPParameter("@pcClientCode", nClientCode, 20));
            sqlPar.Add(new SQLSPParameter("@pcTranCode", nTranCode, 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pcNewClientCode", nNewClientCode, 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 20));
            sqlPar.Add(new SQLSPParameter("@pbIsFeeEdit", bIsFeeEdit));
            sqlPar.Add(new SQLSPParameter("@pdPercentageFee", dPercentageFee));
            sqlPar.Add(new SQLSPParameter("@pnPeriod", 0));
            sqlPar.Add(new SQLSPParameter("@pbFullAmount", true));
            sqlPar.Add(new SQLSPParameter("@pmFee", nNominalFee));
            sqlPar.Add(new SQLSPParameter("@pmTranAmt", nNominal));
            sqlPar.Add(new SQLSPParameter("@pmTranUnit", nRedempUnit));
            sqlPar.Add(new SQLSPParameter("@pbIsRedempAll", bIsRedempAll));
            sqlPar.Add(new SQLSPParameter("@pnFrekuensiPendebetan", nFrekPendebetan));
            sqlPar.Add(new SQLSPParameter("@pnJangkaWaktu", nJangkaWaktu));
            sqlPar.Add(new SQLSPParameter("@pcType", nType));
            sqlPar.Add(new SQLSPParameter("@pcWarnMsg", nstrWarnMsg, 8000, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pcWarnMsg2", nstrWarnMsg2, 8000, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
            sqlPar.Add(new SQLSPParameter("@piTrxTA", 0));
            //20220425, Rendy, M32022-4, begin
            //sqlPar.Add(new SQLSPParameter("@cErrMsgOut", nErrMsgOut, 8000, ParamDirection.OUTPUT));
            //20220425, Rendy, M32022-4, end
            //20221111, Lita, RDN-876, begin
            sqlPar.Add(new SQLSPParameter("@pcRDBDebetMethod", sFrekDebetMethod));
            sqlPar.Add(new SQLSPParameter("@pcRDBIns", sRDBIns));
            sqlPar.Add(new SQLSPParameter("@pdTglDebetRDB", sTglDebetRDB));

            //20221111, Lita, RDN-876, end

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsParamOut, out errMsg))
                    throw new Exception(errMsg);

                //20220425, Rendy, M32022-4, begin
                //if (sqlPar[21].ParameterValue.ToString() != "")
                //    throw new Exception(sqlPar[21].ParameterValue.ToString());
                //20220425, Rendy, M32022-4, end

                else
                {
                    result.TranCode = sqlPar[4].ParameterValue.ToString();
                    result.NewClientCode = sqlPar[5].ParameterValue.ToString();
                    result.strWarnMsg = sqlPar[18].ParameterValue.ToString();
                    result.strWarnMsg2 = sqlPar[19].ParameterValue.ToString();
                    //result.ErrMsgOut = sqlPar[21].ParameterValue.ToString();
                    apiMessageResponse.Data = result;
                    apiMessageResponse.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }

            return apiMessageResponse;
        }
        #endregion ReksaGenerateTranCodeClientCode

        #region ValidasiOfficeId
        public ApiMessage ValidasiOfficeId(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage apiResponseMessage = new ApiMessage();
            apiResponseMessage.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            DataSet dsResult;
            string errMsg = "";
            string strSPName = "ReksaValidateOfficeId";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcKodeKantor", paramIn.Data.OfficeId, 5));
                param.Add(new SQLSPParameter("@pbIsAllowed", "", 1, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (Convert.ToInt16(param[1].ParameterValue) != 1)
                    throw new Exception("Kode Kantor Belum Terdaftar");

                apiResponseMessage.IsSuccess = true;

            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiResponseMessage.IsSuccess = false;
                apiResponseMessage.ErrorCode = "500";
                apiResponseMessage.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiResponseMessage.MessageDateTime = DateTime.Now;
            }

            return apiResponseMessage;
        }
        #endregion ValidasiOfficeId

        #region ValidasiWaperd
        public ApiMessage<ValidasiWaperd> ValidasiWaperd (ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<ValidasiWaperd> apiResponseMessage = new ApiMessage<ValidasiWaperd>();

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            DataSet dsResult;

            ValidasiWaperd response = new ValidasiWaperd();
            
            string errMsg = "";
            string strSPName = "SearchExecute";

            string Guid = "";
            string NIK = "";
            string Office = "";
            int SearchId = 0;
            string Col1 = paramIn.Data.Seller.ToString();
            string Col2 = "";
            bool Validate = false;
            string Criteria = "";
            string SearchDesc = "REKSA_WAPERD";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cGuid", Guid));
                param.Add(new SQLSPParameter("@iNik", NIK));
                param.Add(new SQLSPParameter("@cOffice", Office));
                param.Add(new SQLSPParameter("@iSearchId", SearchId));
                param.Add(new SQLSPParameter("@cCol1", Col1));
                param.Add(new SQLSPParameter("@cCol2", Col2));
                param.Add(new SQLSPParameter("@bValidate", Validate));
                param.Add(new SQLSPParameter("@cCriteria", Criteria));
                param.Add(new SQLSPParameter("@cSearchDesc", SearchDesc));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    if (dsResult.Tables[0].Rows[0]["WaperdNo"].ToString() != "")
                    {
                        response.WaperdNo = dsResult.Tables[0].Rows[0]["WaperdNo"].ToString();
                        response.DateExpire = Convert.ToDateTime(dsResult.Tables[0].Rows[0]["DateExpire"]);

                        apiResponseMessage.Data = response;
                        apiResponseMessage.IsSuccess = true;
                    }
                    else
                        throw new Exception("Data Waperd tidak ditemukan");
                }
                else
                    throw new Exception("Data Waperd tidak ditemukan");
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiResponseMessage.IsSuccess = false;
                apiResponseMessage.ErrorCode = "500";
                apiResponseMessage.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiResponseMessage.MessageDateTime = DateTime.Now;
            }

            return apiResponseMessage;
        }
        #endregion ValidasiWaperd

        #region MandatoryField
        public ApiMessage<List<ValidasiMandatoryFieldRes>> ValidasiMandatoryField(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<List<ValidasiMandatoryFieldRes>> apiResponseMessage = new ApiMessage<List<ValidasiMandatoryFieldRes>>();
            apiResponseMessage.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            DataSet dsResult;

            List<ValidasiMandatoryFieldRes> response = new List<ValidasiMandatoryFieldRes>();
            
            string errMsg = "";

            string strSPName = "ReksaGetMandatoryFieldStatus";
            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cCIF", paramIn.Data.CIFNo));
                param.Add(new SQLSPParameter("@cErrMsg", "", (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {

                    response = JsonConvert.DeserializeObject<List<ValidasiMandatoryFieldRes>>(
                                    JsonConvert.SerializeObject(dsResult.Tables[0],
                                            Newtonsoft.Json.Formatting.None,
                                            new JsonSerializerSettings
                                            {
                                                NullValueHandling = NullValueHandling.Ignore
                                            }));


                    apiResponseMessage.Data = response;
                    apiResponseMessage.IsSuccess = true;

                }
                else
                    apiResponseMessage.IsSuccess = false;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiResponseMessage.IsSuccess = false;
                apiResponseMessage.ErrorCode = "500";
                apiResponseMessage.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiResponseMessage.MessageDateTime = DateTime.Now;
            }

            return apiResponseMessage;
        }
        #endregion MandatoryField

        #region ClientId
        public ApiMessage<ValidasiClientIdRes> ClientId(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<ValidasiClientIdRes> apiMessageResponse = new ApiMessage<ValidasiClientIdRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            DataSet dsOut;

            string errMsg = "";
            
            bool boolExecQuery;
            // set default out parameter 

            string Guid = "";
            string NIK = "";
            string Office = "";
            int SearchId = 0;
            string Col1 = "";
            string Col2 = "";
            bool Validate = false;
            string Criteria = "";
            string SearchDesc = "";

            if (paramIn.Data.Subscription != null)
            {
                Guid = "";
                NIK = "";
                Office = "";
                SearchId = 0;
                Col1 = ReksaCheckSubsType(paramIn).Data.ClientCode;
                Col2 = "";
                Validate = false;
                Criteria = paramIn.Data.CIFNo + "#" + paramIn.Data.Subscription[0].KodeProduk.ToString() + "#" + "SUBS#" + paramIn.Data.Subscription[0].TrxTaxAmnesty;
                SearchDesc = "REKSA_TRXCLIENTNEW";
            }

            if (paramIn.Data.Redemption != null)
            {
                Guid = "";
                NIK = "";
                Office = "";
                SearchId = 0;
                Col1 = paramIn.Data.Redemption[0].ClientCode == null ? "" : paramIn.Data.Redemption[0].ClientCode;
                Col2 = "";
                Validate = false;
                Criteria = paramIn.Data.CIFNo + "#" + paramIn.Data.Redemption[0].KodeProduk.ToString() + "#" + "REDEMP#" + paramIn.Data.Redemption[0].TrxTaxAmnesty;
                SearchDesc = "REKSA_TRXCLIENTNEW";
            }

            if (paramIn.Data.SubsRDB != null)
            {
                Guid = "";
                NIK = "";
                Office = "";
                SearchId = 1;
                Col1 = paramIn.Data.SubsRDB[0].KodeProduk;
                Col2 = paramIn.Data.SubsRDB[0].NamaProduk;
                Validate = false;
                Criteria = paramIn.Data.CIFNo + "#" + paramIn.Data.SubsRDB[0].KodeProduk.ToString() + "#" + "SUBSRDB#" + paramIn.Data.SubsRDB[0].TrxTaxAmnesty; ;
                SearchDesc = "REKSA_TRXCLIENTNEW";
            }

            //exec SearchExecute '','','','0','','',0,'0000000111710#RSDPP#REDEMP#0','REKSA_TRXCLIENTNEW' --> 0 trx non tax amnesty
            //exec SearchExecute '','','','0','','',0,'0000000111710#RSDPP#REDEMP#1','REKSA_TRXCLIENTNEW' --> 1 trx tax amensty

            //parameter
            List<SQLSPParameter> lstSQLParam = new List<SQLSPParameter>();
            lstSQLParam.Add(new SQLSPParameter("@cGuid", Guid));
            lstSQLParam.Add(new SQLSPParameter("@iNik", NIK));
            lstSQLParam.Add(new SQLSPParameter("@cOffice", Office));
            lstSQLParam.Add(new SQLSPParameter("@iSearchId", SearchId));
            lstSQLParam.Add(new SQLSPParameter("@cCol1", Col1));
            lstSQLParam.Add(new SQLSPParameter("@cCol2", Col2));
            lstSQLParam.Add(new SQLSPParameter("@bValidate", Validate));
            lstSQLParam.Add(new SQLSPParameter("@cCriteria", Criteria));
            lstSQLParam.Add(new SQLSPParameter("@cSearchDesc", SearchDesc));
            try
            {
                //databaseConnector.execSP(TransactionType.HANDLED_IN_STORED_PROCEDURE, "SearchExecute", lstSQLParam, out dsOut);
                boolExecQuery = clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "SearchExecute", ref lstSQLParam, out dsOut, out errMsg);
                //databaseConnector.Dispose();
                //databaseConnector = null;
                if (boolExecQuery)
                {
                    if (dsOut != null && dsOut.Tables.Count > 0 && dsOut.Tables[0].Rows.Count > 0)
                    {
                        ValidasiClientIdRes response = new ValidasiClientIdRes();
                        response.ClientId = dsOut.Tables[0].Rows[0]["ClientId"] != DBNull.Value ? dsOut.Tables[0].Rows[0]["ClientId"].ToString() : "0";
                        response.ClientCode = dsOut.Tables[0].Rows[0]["ClientCode"] != DBNull.Value ? dsOut.Tables[0].Rows[0]["ClientCode"].ToString() : "0";
                        response.IsRDB = dsOut.Tables[0].Rows[0]["IsRDB"] != DBNull.Value ? dsOut.Tables[0].Rows[0]["IsRDB"].ToString() : "0";
                        apiMessageResponse.Data = response;
                        apiMessageResponse.IsSuccess = true;
                    }
                    else
                    {

                        ValidasiClientIdRes response = new ValidasiClientIdRes();
                        response.ClientId = "0";
                        response.ClientCode = "0";
                        response.IsRDB = "0";
                        apiMessageResponse.Data = response;
                        apiMessageResponse.IsSuccess = true;
                    }
                }
                else
                    throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }

            return apiMessageResponse;
        }
        #endregion ClientId

        #region ReksaGetListClientRDB
        public ApiMessage<ValidasiListDetailRDBRes> ReksaGetListClientRDB(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            ApiMessage<ValidasiListDetailRDBRes> apiMessageResponse = new ApiMessage<ValidasiListDetailRDBRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            DataSet dsResult;

            ValidasiListDetailRDBRes response = new ValidasiListDetailRDBRes();

            string errMsg = "";
            string strSPName = "ReksaGetListClientRDB";

            string nClientCode = ClientId(paramIn).Data.ClientCode;
            //parameter
            List<SQLSPParameter> param = new List<SQLSPParameter>();
            param.Add(new SQLSPParameter("@cGuid", nClientCode));
            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);
            
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    //20230728, Lita, RDN-1017, begin
                    //response.JangkaWaktu = Convert.ToInt64(dsResult.Tables[0].Rows[0]["JangkaWaktu"].ToString());
                    response.JangkaWaktu = Convert.ToInt32(dsResult.Tables[0].Rows[0]["JangkaWaktu"].ToString());
                    //20230728, Lita, RDN-1017, end
                    response.JatuhTempo = Convert.ToDateTime(dsResult.Tables[0].Rows[0]["JatuhTempo"].ToString());
                    response.AutoRedemption = dsResult.Tables[0].Rows[0]["AutoRedemption"].ToString();
                    response.Asuransi = dsResult.Tables[0].Rows[0]["Asuransi"].ToString();
                    //20230728, Lita, RDN-1017, begin
                    //response.FrekPendebetan = Convert.ToInt64(dsResult.Tables[0].Rows[0]["FrekPendebetan"].ToString());
                    //response.SisaJangkaWaktu = Convert.ToInt64(dsResult.Tables[0].Rows[0]["SisaJangkaWaktu"].ToString());
                    response.FrekPendebetan = Convert.ToInt32(dsResult.Tables[0].Rows[0]["FrekPendebetan"].ToString());
                    response.SisaJangkaWaktu = Convert.ToInt32(dsResult.Tables[0].Rows[0]["SisaJangkaWaktu"].ToString());
                    //20230728, Lita, RDN-1017, end
                    response.IsDoneDebet = dsResult.Tables[0].Rows[0]["IsDoneDebet"].ToString();
                    response.StartDebetDate = Convert.ToDateTime(dsResult.Tables[0].Rows[0]["StartDebetDate"].ToString());
                    response.FreqDebetUnit = dsResult.Tables[0].Rows[0]["FreqDebetUnit"].ToString();
                }
                else
                    throw new Exception("Data ReksaGetListClientRDB Detail not found !");


                //insert ke apimsgresponse
                apiMessageResponse.Data = response;
                apiMessageResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }

            return apiMessageResponse;
        }

        //20230728, Lita, RDN-1017, begin
        public ApiMessage<ValidasiListDetailRDBRes> ReksaGetListClientRDBByCode(string sClientCode)
        {
            ApiMessage<ValidasiListDetailRDBRes> apiMessageResponse = new ApiMessage<ValidasiListDetailRDBRes>();
            //apiMessageResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            DataSet dsResult;

            ValidasiListDetailRDBRes response = new ValidasiListDetailRDBRes();
            try
            {
                string errMsg = "";
                string strSPName = "ReksaGetListClientRDB";
               

                //parameter
                List<SQLSPParameter> param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcClientCode", sClientCode));
            
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    //20230728, Lita, RDN-1017, begin
                    //response.JangkaWaktu = Convert.ToInt64(dsResult.Tables[0].Rows[0]["JangkaWaktu"].ToString());
                    response.JangkaWaktu = Convert.ToInt32(dsResult.Tables[0].Rows[0]["JangkaWaktu"].ToString());
                    //20230728, Lita, RDN-1017, end
                    response.JatuhTempo = Convert.ToDateTime(dsResult.Tables[0].Rows[0]["JatuhTempo"].ToString());
                    response.AutoRedemption = dsResult.Tables[0].Rows[0]["AutoRedemption"].ToString();
                    response.Asuransi = dsResult.Tables[0].Rows[0]["Asuransi"].ToString();
                    //20230728, Lita, RDN-1017, begin
                    //response.FrekPendebetan = Convert.ToInt64(dsResult.Tables[0].Rows[0]["FrekPendebetan"].ToString());
                    //response.SisaJangkaWaktu = Convert.ToInt64(dsResult.Tables[0].Rows[0]["SisaJangkaWaktu"].ToString());
                    response.FrekPendebetan = Convert.ToInt32(dsResult.Tables[0].Rows[0]["FrekPendebetan"].ToString());
                    response.SisaJangkaWaktu = Convert.ToInt32(dsResult.Tables[0].Rows[0]["SisaJangkaWaktu"].ToString());
                    //20230728, Lita, RDN-1017, end
                    response.IsDoneDebet = dsResult.Tables[0].Rows[0]["IsDoneDebet"].ToString();
                    response.StartDebetDate = Convert.ToDateTime(dsResult.Tables[0].Rows[0]["StartDebetDate"].ToString());
                    response.FreqDebetUnit = dsResult.Tables[0].Rows[0]["FreqDebetUnit"].ToString();
                    //20230728, Lita, RDN-1017, begin
                    response.IsMature = dsResult.Tables[0].Rows[0]["IsMature"].ToString();
                    //20230728, Lita, RDN-1017, end
                }
                else
                    throw new Exception("Data ReksaGetListClientRDB Detail not found !");


                //insert ke apimsgresponse
                apiMessageResponse.Data = response;
                apiMessageResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                //this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }

            return apiMessageResponse;
        }
    

        public bool ClientCode(int iClientId, out string sClientCode, out bool bIsRDB, out string sErrMessage)
        {
            
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            DataSet dsResult;
            string errMsg = "";

            sClientCode = "";
            bIsRDB = false;
            sErrMessage = "";
            //parameter
            try
            {
                string strQueryProd = @"
                        declare 
                            @pnClientId int

                        set @pnClientId = " + iClientId + @"

                        select ClientCode, IsRDB = case when isnull(b.ClientId, 0) <> 0 then 1 else 0 end from dbo.ReksaCIFData_TM a with(nolock) left join dbo.ReksaRegulerSubscriptionClient_TM b with(nolock) on a.ClientId = b.ClientId 
                        where a.ClientId = @pnClientId
                        
                    ";
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, strQueryProd, out dsResult, out errMsg))
                {
                    if (dsResult.Tables[0].Rows.Count > 0)
                    {
                        sClientCode = dsResult.Tables[0].Rows[0]["ClientCode"].ToString();
                        bIsRDB = dsResult.Tables[0].Rows[0]["IsRDB"].ToString() == "1" ? true : false;
                    }
                    else
                        throw new Exception("Client Code tidak ditemukan");
                }
                else
                    throw new Exception("GetClientCode error !"+ errMsg);

                return true;
            }
            catch (Exception ex)
            {
                sErrMessage = ex.Message;
                return false;
            }

            
        }
        //20230728, Lita, RDN-1017, end
        #endregion ReksaGetListClientRDB

        #region ValidasiOffice
        public string ValidasiOffice(string OfficeId)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            DataSet dsResult;
            string ExecQueryResult = "-1";
            string errMsg = "";

            string strSPName = "ReksaValidateOfficeId";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcKodeKantor", OfficeId, 5));
                param.Add(new SQLSPParameter("@pbIsAllowed", "", 1, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                ExecQueryResult = param[1].ParameterValue.ToString();

                //if (databaseConnector.execSP(TransactionType.HANDLED_IN_STORED_PROCEDURE, strSPName, param, out dsResult))
                //{
                //    ExecQueryResult = param[1].ParameterValue.ToString();
                //}
            }
            catch (Exception ex)
            {
                ExecQueryResult = ex.Message;
            }
            finally
            {
                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }
            }

            return ExecQueryResult;
        }
        #endregion ValidasiOffice

        #region ValidasiOfficeCBOId
        public ApiMessage<ValidasiOfficeIdReq> ValidasiOfficeCBOId(ApiMessage<ValidasiOfficeIdReq> paramIn)
        {
            ApiMessage<ValidasiOfficeIdReq> apiMsgResponse = new ApiMessage<ValidasiOfficeIdReq>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            DataSet dsResult;

            ValidasiOfficeIdReq response = new ValidasiOfficeIdReq();
           
            string errMsg = "";
            string errCode = "00";

            string strSPName = "ReksaValidateCBOOfficeId";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcOfficeId", paramIn.Data.OfficeId, 5));
                param.Add(new SQLSPParameter("@pcIsEnable", "0", 1, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
                param.Add(new SQLSPParameter("@pcErrorMessage", "", 500, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
                param.Add(new SQLSPParameter("@pcErrorCode", "00", 2, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (param[2].ParameterValue.ToString() != "")
                {
                    errCode = param[3].ParameterValue.ToString();
                    throw new Exception(param[2].ParameterValue.ToString());
                }

                else
                {
                    if (param[1].ParameterValue.ToString() == "1")
                        throw new Exception("Office Id is disablee");
                    else
                    {

                        response.OfficeId = paramIn.Data.OfficeId;
                        response.IsEnable = param[1].ParameterValue.ToString();
                    }
                }

                apiMsgResponse.Data = response;
                apiMsgResponse.IsSuccess = true;


            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }
        #endregion ValidasiOfficeCBOId

        #region ClientIdRDB
        public string getClientIdRedempRDB(string clientCode)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            string result = "", errMsg = "";
            DataSet dsOut;
            bool boolExecQuery;
            // set default out parameter 

            //parameter
            List<SQLSPParameter> lstSQLParam = new List<SQLSPParameter>();
            lstSQLParam.Add(new SQLSPParameter("@pcCariApa", "CLIENTID"));
            lstSQLParam.Add(new SQLSPParameter("@pcInput", clientCode));
            lstSQLParam.Add(new SQLSPParameter("@cValue", result, 100, ParamDirection.OUTPUT));

            try
            {
                //databaseConnector.execSP(TransactionType.HANDLED_IN_STORED_PROCEDURE, "SearchExecute", lstSQLParam, out dsOut);
                boolExecQuery = clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaGetImportantData", ref lstSQLParam, out dsOut, out errMsg);
                databaseConnector.Dispose();
                databaseConnector = null;
                if (boolExecQuery)
                {
                    result = lstSQLParam[2].ParameterValue.ToString();
                }
            }
            catch (Exception)
            {
                //_apiLogger.logError(this, new StackTrace(false), "Error Exception exec sp ReksaGenerateTranCodeClientCode" + ex);
                result = "-1";
            }
            //close connection
            if (databaseConnector != null)
            {
                databaseConnector.Dispose();
            }
            databaseConnector = null;
            return result;
        }
        #endregion ClientIdRDB

        #endregion SUB REDEMP

        //20201201, Dennis, RDN2-23, begin
        #region Swithcing Non RDB

        #region ValidasiOfficeId
        public ApiMessage ValidasiOfficeIdSwitching(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage apiResponseMessage = new ApiMessage();
            apiResponseMessage.copyHeaderForReply(paramIn);
            List<SQLSPParameter> param;
            DataSet dsResult;
            string errMsg = "";

            string strSPName = "ReksaValidateOfficeId";
            string officeId = paramIn.Data.OfficeId.Substring(paramIn.Data.OfficeId.Length - 5, 5);

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcKodeKantor", officeId, 5));
                param.Add(new SQLSPParameter("@pbIsAllowed", "", 1, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (Convert.ToInt16(param[1].ParameterValue) != 1)
                    throw new Exception("Kode Kantor Belum Terdaftar");

                apiResponseMessage.IsSuccess = true;

            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiResponseMessage.IsSuccess = false;
                apiResponseMessage.ErrorCode = "500";
                apiResponseMessage.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiResponseMessage.MessageDateTime = DateTime.Now;
            }

            return apiResponseMessage;
        }
        #endregion ValidasiOfficeId

        #region ReksaRefreshNasabahSwitching
        public ApiMessage<ReksaRefreshNasabahRes> ReksaRefreshNasabahSwitching(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<ReksaRefreshNasabahRes> apiMessageResponse = new ApiMessage<ReksaRefreshNasabahRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);
            ReksaRefreshNasabahRes responseClass = new ReksaRefreshNasabahRes();
            List<Response1> listResponse1 = new List<Response1>();
            List<Response2> listResponse2 = new List<Response2>();
            List<SQLSPParameter> param;
            DataSet dsResult;
            string errMsg = "";

            #region validasi
            //Validasi 
            //CIFNo
            if (paramIn.Data.CIFNo == "" || paramIn.Data.CIFNo == null)
            {
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorDescription = "CIFNo harus di isi";
                return apiMessageResponse;
            }
            #endregion validasi

            string strSPName = "ReksaRefreshNasabah";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
                param.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                param.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                #region Data1
                //Data 1
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    listResponse1 = JsonConvert.DeserializeObject<List<Response1>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    responseClass.Data1 = listResponse1[0];
                }
                else
                    throw new Exception("Data ReksaRefreshNasabah Detail not found !");
                #endregion

                #region Data 2
                //Data2
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[1].Rows.Count > 0)
                {
                    listResponse2 = JsonConvert.DeserializeObject<List<Response2>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[1],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    responseClass.Data2 = listResponse2[0];
                }
                else
                    throw new Exception("Data ReksaRefreshNasabah Risk Profile not found !");
                #endregion Data 2

                //insert ke apimsgresponse
                apiMessageResponse.Data = responseClass;
                apiMessageResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }

            return apiMessageResponse;
        }
        #endregion ReksaRefreshNasabah

        #region ReksaHitungUmur
        public ApiMessage<ReksaHitungUmurRes> ReksaHitungUmur(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaHitungUmurRes response = new ReksaHitungUmurRes();
            ApiMessage<ReksaHitungUmurRes> apiMessageResponse = new ApiMessage<ReksaHitungUmurRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaHitungUmur", errMsg = "";

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
            sqlPar.Add(new SQLSPParameter("@pnUmur", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (sqlPar[1].ParameterValue.ToString() != null)
                {
                    response.Umur = sqlPar[1].ParameterValue.ToString();

                    apiMessageResponse.Data = response;
                    apiMessageResponse.IsSuccess = true;
                }

                else
                    throw new Exception("Gagal exec sp ReksaHitungUmur");
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
            return apiMessageResponse;
        }
        #endregion

        #region ReksaCheckingTaxAmnesty
        public ApiMessage<ReksaCheckingTaxAmnestyRes> ReksaCheckingTaxAmnesty(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaCheckingTaxAmnestyRes response = new ReksaCheckingTaxAmnestyRes();
            ApiMessage<ReksaCheckingTaxAmnestyRes> apiMessageResponse = new ApiMessage<ReksaCheckingTaxAmnestyRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "dbo.ReksaCheckingTaxAmnesty", errMsg = "";

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
            sqlPar.Add(new SQLSPParameter("@pcIsAllow", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pcErrorMessage", 50, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (sqlPar[1].ParameterValue.ToString() != null)
                {
                    response.IsAllow = sqlPar[1].ParameterValue.ToString();
                    response.ErrorMessage = sqlPar[2].ParameterValue.ToString();

                    apiMessageResponse.Data = response;
                    apiMessageResponse.IsSuccess = true;
                }

                else
                    throw new Exception("Data ReksaCheckingTaxAmnesty Not Found!");

            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
            return apiMessageResponse;
        }
        #endregion

        #region ReksaCheckCIFTaxAmnesty
        public ApiMessage<ReksaCheckCIFTaxAmnestyRes> ReksaCheckCIFTaxAmnesty(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ReksaCheckCIFTaxAmnestyRes response = new ReksaCheckCIFTaxAmnestyRes();
            List<ReksaCheckCIFTaxAmnesty> listResponse = new List<ReksaCheckCIFTaxAmnesty>();
            ApiMessage<ReksaCheckCIFTaxAmnestyRes> apiMsgResponse = new ApiMessage<ReksaCheckCIFTaxAmnestyRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            string strSPName = "dbo.ReksaCheckCIFTaxAmnesty", errMsg = "";
            DataSet dsResult = new DataSet();
            DataSet dsParamOut = new DataSet();

            paramIn.Data.ErrMsg = "";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
                param.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                param.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                    throw new Exception("Data not found !");

                listResponse = JsonConvert.DeserializeObject<List<ReksaCheckCIFTaxAmnesty>>(JsonConvert.SerializeObject(dsResult.Tables[0]));

                var count = listResponse.Count();

                #region MainData

                response.ListReksaCheckCIFTaxAmnesty = listResponse;

                #endregion

                apiMsgResponse.Data = response;
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        #endregion

        #region GetDetailReksaProduct
        public ApiMessage<ReksaProductRes> GetDetailReksaProduct(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaProductRes response = new ReksaProductRes();
            List<ReksaProduct> listResponse = new List<ReksaProduct>();
            ApiMessage<ReksaProductRes> apiMsgResponse = new ApiMessage<ReksaProductRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            string strErrMsg = "";
            DataSet dsResult;

            try
            {
                string sqlCommand = @"
                    declare @ProdIdSwcOut varchar(3)
                    declare @ProdIdSwcIn varchar(3)
                    set @ProdIdSwcOut = '" + paramIn.Data.ProdIdSwcOut + @"'
                    set @ProdIdSwcIn = '" + paramIn.Data.ProdIdSwcIn + @"'

                    select distinct rs.ProdSwitchOut, rp.ProdName, rp.ProdId, rp.ProdCCY, rp.Status
                       from dbo.ReksaProdSwitchingParam_TR rs  with (nolock)      
                       join dbo.ReksaProduct_TM rp     
                       on rs.ProdSwitchOut = rp.ProdCode
                       where rp.ProdId  = @ProdIdSwcOut
                    UNION ALL
--20230728, Lita, RDN-1017, begin
                    --select distinct rs.ProdSwitchOut, rp.ProdName, rp.ProdId, rp.ProdCCY, rp.Status
                    select distinct rs.ProdSwitchIn, rp.ProdName, rp.ProdId, rp.ProdCCY, rp.Status
--20230728, Lita, RDN - 1017, end
                       from dbo.ReksaProdSwitchingParam_TR rs  with (nolock)      
                       join dbo.ReksaProduct_TM rp     
--20230728, Lita, RDN-1017, begin
                       on rs.ProdSwitchIn = rp.ProdCode
--20230728, Lita, RDN - 1017, end
                       where rp.ProdId  = @ProdIdSwcIn";



                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsResult, out strErrMsg))
                {
                    if (dsResult.Tables.Count < 1 || dsResult.Tables[0].Rows.Count.Equals(0))
                    {
                        apiMsgResponse.IsSuccess = false;
                        apiMsgResponse.ErrorCode = "3000";
                        apiMsgResponse.ErrorDescription = "Data GetDetailReksaProduct not found";
                        return apiMsgResponse;
                    }

                    //listResponse = JsonConvert.DeserializeObject<List<ReksaProductRes>>(JsonConvert.SerializeObject(dsResult.Tables[0]));

                    #region mapping sesuai tipe data agar tidak merubah 
                    listResponse = (from DataRow dr in dsResult.Tables[0].Rows
                                    select new ReksaProduct()
                                    {
                                        ProdSwitchOut = dr["ProdSwitchOut"] != DBNull.Value ? dr["ProdSwitchOut"].ToString() : "",
                                        ProdId = dr["ProdId"] != DBNull.Value ? Convert.ToInt32(dr["ProdId"]) : 0,
                                        ProdName = dr["ProdName"] != DBNull.Value ? dr["ProdName"].ToString() : "",
                                        ProdCCY = dr["ProdCCY"] != DBNull.Value ? dr["ProdCCY"].ToString() : "",
                                    }).ToList();
                    #endregion mapping sesuai tipe data agar tidak merubah

                    response.ListReksaProduct = listResponse;
                    apiMsgResponse.Data = response;
                    apiMsgResponse.IsSuccess = true;

                }
                else
                {
                    apiMsgResponse.IsSuccess = false;
                    apiMsgResponse.ErrorCode = "4002";
                    apiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return apiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        #endregion

        #region ReksaSrcTrxProduct
        public ApiMessage<ReksaSrcTrxProductRes> ReksaSrcTrxProduct(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ReksaSrcTrxProductRes response = new ReksaSrcTrxProductRes();
            List<ReksaSrcTrxProduct> listResponse = new List<ReksaSrcTrxProduct>();
            ApiMessage<ReksaSrcTrxProductRes> apiMsgResponse = new ApiMessage<ReksaSrcTrxProductRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "dbo.API_ReksaSrcTrxProduct", errMsg = "";
            string strSPName = "dbo.ReksaSrcTrxProduct", errMsg = "";
            //20220425, Rendy, M32022-4, end

            string strCol1 = "", strCol2 = "", strJenisTrx = "SWCNONRDB#" + paramIn.Data.CIFNo;
            DataSet dsResult = new DataSet();
            DataSet dsParamOut = new DataSet();

            paramIn.Data.ErrMsg = "";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cCol1", strCol1));
                param.Add(new SQLSPParameter("@cCol2", strCol2));
                param.Add(new SQLSPParameter("@bValidate", 0));
                param.Add(new SQLSPParameter("@cJenisTrx", strJenisTrx));
                //20220425, Rendy, M32022-4, begin
                //param.Add(new SQLSPParameter("@pcErrMsg", paramIn.Data.ErrMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//10
                //20220425, Rendy, M32022-4, end

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                    throw new Exception("Data Product Switch Out not found !");

                listResponse = JsonConvert.DeserializeObject<List<ReksaSrcTrxProduct>>(JsonConvert.SerializeObject(dsResult.Tables[0]));

                var count = listResponse.Count();

                #region MainData

                response.ListReksaSrcTrxProduct = listResponse;

                #endregion

                #region Cek Error Di SP

                //errMsg = (param[4] == null ? "" : param[4].ParameterValue.ToString());

                //if (!errMsg.Equals(""))
                //    throw new Exception(errMsg);

                #endregion

                apiMsgResponse.Data = response;
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        #endregion

        #region ReksaSrcTrxClientNew
        public ApiMessage<ReksaSrcTrxClientNewRes> ReksaSrcTrxClientNew(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<ReksaSrcTrxClientNewRes> apiMsgResponse = new ApiMessage<ReksaSrcTrxClientNewRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);
            ReksaSrcTrxClientNewRes responseClass = new ReksaSrcTrxClientNewRes();
            List<MainData> listResponse = new List<MainData>();
            List<SQLSPParameter> param;
            DataSet dsResult;

            string strProductName = "";
            strProductName = GetDetailReksaProduct(paramIn).Data.ListReksaProduct[0].ProdSwitchOut;

            //20221111, Lita, RDN-876, begin
            //string strCol1 = "", strCol2 = "", strCriteria = paramIn.Data.CIFNo + "#" + strProductName + "#SWCNONRDB", errMsg = "";
            //20230728, Lita, RDN-1017, begin

            //string strCol1 = "", strCol2 = "", strCriteria = paramIn.Data.CIFNo + "#" + strProductName + "#SWCNONRDB" + "#" + paramIn.Data.TrxTaxAmnesty;
            string strCol1 = "", strCol2="", strCriteria="";
            if (paramIn.Data.IsRDB == true)
                strCriteria = paramIn.Data.CIFNo + "#" + strProductName + "#SWCRDB" + "#" + paramIn.Data.TrxTaxAmnesty;
            else
                strCriteria = paramIn.Data.CIFNo + "#" + strProductName + "#SWCNONRDB" + "#" + paramIn.Data.TrxTaxAmnesty;
            //20230728, Lita, RDN-1017, end

            string errMsg = "";
            //20221111, Lita, RDN-876, end

            bool bValidate = false;
            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaSrcTrxClientNew";
            string strSPName = "ReksaSrcTrxClientNew";
            //20220425, Rendy, M32022-4, end

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cCol1", strCol1));
                param.Add(new SQLSPParameter("@cCol2", strCol2));
                param.Add(new SQLSPParameter("@bValidate", bValidate));
                param.Add(new SQLSPParameter("@cCriteria", strCriteria));
                //20220425, Rendy, M32022-4, begin
                //param.Add(new SQLSPParameter("@pcErrMsg", paramIn.Data.ErrMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //20220425, Rendy, M32022-4, end

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                //listResponse = JsonConvert.DeserializeObject<List<ReksaSrcTrxClientNewRes>>(JsonConvert.SerializeObject(dsResult.Tables[0]));

                #region Cek Table Result
                //Data
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    listResponse = JsonConvert.DeserializeObject<List<MainData>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    responseClass.MainData = listResponse;
                }
                else
                    throw new Exception("Data ClientCode Switch Outs not found !");
                #endregion


                #region Cek Error Di SP

                //errMsg = (param[4] == null ? "" : param[4].ParameterValue.ToString());

                //if (!errMsg.Equals(""))
                //    throw new Exception(errMsg);

                #endregion

                //insert ke apimsgresponse
                apiMsgResponse.Data = responseClass;
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }
        #endregion

        #region ReksaSrcTransSwitchIn
        public ApiMessage<ReksaSrcTransSwitchInRes> ReksaSrcTransSwitchIn(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ReksaSrcTransSwitchInRes response = new ReksaSrcTransSwitchInRes();
            List<ReksaSrcTransSwitchIn> listResponse = new List<ReksaSrcTransSwitchIn>();
            ApiMessage<ReksaSrcTransSwitchInRes> apiMsgResponse = new ApiMessage<ReksaSrcTransSwitchInRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "dbo.API_ReksaSrcTransSwitchIn", errMsg = "";
            string strSPName = "dbo.ReksaSrcTransSwitchIn", errMsg = "";
            //20220425, Rendy, M32022-4, end

            string strCol1 = "", strCol2 = "", strProdCode = "";

            strProdCode = GetDetailReksaProduct(paramIn).Data.ListReksaProduct[1].ProdSwitchOut;

            bool bValidate = false;
            DataSet dsResult = new DataSet();
            DataSet dsParamOut = new DataSet();

            paramIn.Data.ErrMsg = "";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cCol1", strCol1));
                param.Add(new SQLSPParameter("@cCol2", strCol2));
                param.Add(new SQLSPParameter("@bValidate", bValidate));
                param.Add(new SQLSPParameter("@cProdCode", strProdCode));
                //20220425, Rendy, M32022-4, begin
                //param.Add(new SQLSPParameter("@pcErrMsg", paramIn.Data.ErrMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//10
                //20220425, Rendy, M32022-4, end

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                    throw new Exception("Data Product Switch In not found !");

                listResponse = JsonConvert.DeserializeObject<List<ReksaSrcTransSwitchIn>>(JsonConvert.SerializeObject(dsResult.Tables[0]));

                var count = listResponse.Count();

                #region MainData

                response.ListReksaSrcTransSwitchIn = listResponse;

                #endregion

                #region Cek Error Di SP

                //errMsg = (param[4] == null ? "" : param[4].ParameterValue.ToString());

                //if (!errMsg.Equals(""))
                //    throw new Exception(errMsg);

                #endregion

                apiMsgResponse.Data = response;
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            
            return apiMsgResponse;
        }
        #endregion

        #region ReksaSrcClientSwitchIn
        public ApiMessage<ReksaSrcClientSwitchInRes> ReksaSrcClientSwitchIn(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<ReksaSrcClientSwitchInRes> apiMsgResponse = new ApiMessage<ReksaSrcClientSwitchInRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);
            ReksaSrcClientSwitchInRes responseClass = new ReksaSrcClientSwitchInRes();
            List<ReksaSrcClientSwitchIn> listResponse = new List<ReksaSrcClientSwitchIn>();
            List<SQLSPParameter> param;
            DataSet dsResult;

            //20230728, Lita, RDN-1017, begin
            //string strCol1 = "", strCol2 = "", strCriteria = paramIn.Data.ProdIdSwcOut + "#" + paramIn.Data.CIFNo, errMsg = "";
            string strCol1 = "", strCol2 = "", strCriteria = paramIn.Data.ProdIdSwcIn + "#" + paramIn.Data.CIFNo, errMsg = "";
            //20230728, Lita, RDN-1017, end

            bool bValidate = false;
            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaSrcClientSwitchIn";
            string strSPName = "ReksaSrcClientSwitchIn";
            //20220425, Rendy, M32022-4, end

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cCol1", strCol1));
                param.Add(new SQLSPParameter("@cCol2", strCol2));
                param.Add(new SQLSPParameter("@bValidate", bValidate));
                param.Add(new SQLSPParameter("@cCriteria", strCriteria));
                //20220425, Rendy, M32022-4, begin
                //param.Add(new SQLSPParameter("@pcErrMsg", paramIn.Data.ErrMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //20220425, Rendy, M32022-4, end

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                #region Cek Table Result
                //Data
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    listResponse = JsonConvert.DeserializeObject<List<ReksaSrcClientSwitchIn>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    responseClass.MainData = listResponse;
                }
                else
                    throw new Exception("Data ClientCode Switch In not found !");
                #endregion


                #region Cek Error Di SP

                //errMsg = (param[4] == null ? "" : param[4].ParameterValue.ToString());

                //if (!errMsg.Equals(""))
                //    throw new Exception(errMsg);

                #endregion

                //insert ke apimsgresponse
                apiMsgResponse.Data = responseClass;
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }
        #endregion

        #region ReksaGetLatestNAV
        public ApiMessage<ReksaGetLatestNAVRes> ReksaGetLatestNAV(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaGetLatestNAVRes response = new ReksaGetLatestNAVRes();
            ApiMessage<ReksaGetLatestNAVRes> apiMessageResponse = new ApiMessage<ReksaGetLatestNAVRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaGetLatestNAV", errMsg = "";

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            List<SQLSPParameter> sqlPar2 = new List<SQLSPParameter>();

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pnProdId", paramIn.Data.ProdIdSwcOut));
            sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
            sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));
            sqlPar.Add(new SQLSPParameter("@@pmNAV", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

            sqlPar2 = new List<SQLSPParameter>();
            sqlPar2.Add(new SQLSPParameter("@pnProdId", paramIn.Data.ProdIdSwcIn));
            sqlPar2.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
            sqlPar2.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));
            sqlPar2.Add(new SQLSPParameter("@@pmNAV", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar2, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (sqlPar[3].ParameterValue.ToString() != null || sqlPar2[3].ParameterValue.ToString() != null)
                {
                    response.NAVSwcOut = decimal.Parse(sqlPar[3].ParameterValue.ToString());
                    response.NAVSwcIn = decimal.Parse(sqlPar2[3].ParameterValue.ToString());

                    apiMessageResponse.Data = response;
                    apiMessageResponse.IsSuccess = true;
                }

                else
                    throw new Exception("Gagal exec sp ReksaGetLatestBalance");
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
            return apiMessageResponse;
        }
        #endregion

        #region ReksaGetLatestBalance
        public ApiMessage<ReksaGetLatestBalanceRes> ReksaGetLatestBalance(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaGetLatestBalanceRes response = new ReksaGetLatestBalanceRes();
            ApiMessage<ReksaGetLatestBalanceRes> apiMessageResponse = new ApiMessage<ReksaGetLatestBalanceRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaGetLatestBalance", errMsg = "";
            //int nClientId = 0;
            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            List<SQLSPParameter> sqlPar2 = new List<SQLSPParameter>();

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientIdSwcOut));
            sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
            sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));
            sqlPar.Add(new SQLSPParameter("@pmUnitBalance", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

            //parameter
            sqlPar2 = new List<SQLSPParameter>();
            sqlPar2.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientIdSwcIn));
            sqlPar2.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
            sqlPar2.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));
            sqlPar2.Add(new SQLSPParameter("@pmUnitBalance", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar2, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (sqlPar[3].ParameterValue.ToString() != null || sqlPar2[3].ParameterValue.ToString() != null)
                {
                    response.UnitBalanceSwcOut = decimal.Parse(sqlPar[3].ParameterValue.ToString());
                    response.UnitBalanceSwcIn = decimal.Parse(sqlPar2[3].ParameterValue.ToString());

                    apiMessageResponse.Data = response;
                    apiMessageResponse.IsSuccess = true;
                }

                else
                    throw new Exception("Data Latest balance Not Found");
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
            
            return apiMessageResponse;
        }
        #endregion

        #region ReksaCheckSubsType
        public ApiMessage<ValidasiReksaCheckSubsTypeRes> ReksaCheckSubsTypeSwitching(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ValidasiReksaCheckSubsTypeRes response = new ValidasiReksaCheckSubsTypeRes();
            ApiMessage<ValidasiReksaCheckSubsTypeRes> apiMessageResponse = new ApiMessage<ValidasiReksaCheckSubsTypeRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaCheckSubsType", errMsg = "";
            bool IsSubsNew = false, IsRDB = false;
            int nTrxTaxAmnesty;

            if (paramIn.Data.TrxTaxAmnesty == true)
            {
                nTrxTaxAmnesty = 1;
            }
            else
            {
                nTrxTaxAmnesty = 0;
            }

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 20));
            sqlPar.Add(new SQLSPParameter("@pnProductId", paramIn.Data.ProdIdSwcIn, 3));
            sqlPar.Add(new SQLSPParameter("@pbIsRDB", IsSubsNew));
            sqlPar.Add(new SQLSPParameter("@pbIsSubsNew", IsRDB, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pcClientCode", "", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
            sqlPar.Add(new SQLSPParameter("@pbIsTrxTA", nTrxTaxAmnesty));
            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (sqlPar[4].ParameterValue.ToString() != null || sqlPar[3].ParameterValue.ToString() != null)
                {
                    response.isSubsNew = sqlPar[3].ParameterValue.ToString() == "0" ? false : true;
                    response.ClientCode = sqlPar[4].ParameterValue.ToString();

                    apiMessageResponse.Data = response;
                    apiMessageResponse.IsSuccess = true;
                }

                else
                    throw new Exception("Data Subs Type Not Found!");
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
            
            return apiMessageResponse;
        }
        #endregion

        #region ReksaCalcSwitchingFee
        public ApiMessage<ReksaCalcSwitchingFeeRes> ReksaCalcSwitchingFee(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaCalcSwitchingFeeRes response = new ReksaCalcSwitchingFeeRes();
            ApiMessage<ReksaCalcSwitchingFeeRes> apiMessageResponse = new ApiMessage<ReksaCalcSwitchingFeeRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaCalcSwitchingFee", strProdNameSwcOut = "", strProdNameSwcIn = "", errMsg = "";
            string isEmployee;

            //get data
            strProdNameSwcOut = GetDetailReksaProduct(paramIn).Data.ListReksaProduct[0].ProdSwitchOut;
            strProdNameSwcIn = GetDetailReksaProduct(paramIn).Data.ListReksaProduct[1].ProdSwitchOut;
            //isEmployee = Convert.ToBoolean(ReksaRefreshNasabahSwitching(paramIn).Data.Data1.IsEmployee.ToString());
            isEmployee = ReksaRefreshNasabahSwitching(paramIn).Data.Data1.IsEmployee;

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            bool isJenis = false;
            if (!paramIn.Data.ByUnit)
            {
                isJenis = true;
            }

            //20230728, Andi, RDN-1017, begin
            int nCondition = 0;
            //20230728, Andi, RDN-1017, end

            ////20230728, Andi, RDN-1017, begin
            ////parameter
            //sqlPar = new List<SQLSPParameter>();
            //sqlPar.Add(new SQLSPParameter("@pcProdSwitchOut", strProdNameSwcOut));
            //sqlPar.Add(new SQLSPParameter("@pcProdSwitchIn", strProdNameSwcIn));
            //sqlPar.Add(new SQLSPParameter("@pbJenis", true));
            //sqlPar.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.TranAmt));
            //sqlPar.Add(new SQLSPParameter("@pmUnit", paramIn.Data.TranUnit));
            //sqlPar.Add(new SQLSPParameter("@pcFeeCCY", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//5
            //sqlPar.Add(new SQLSPParameter("@pnFee", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//6
            //sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
            //sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));
            //sqlPar.Add(new SQLSPParameter("@pmNAV", 0));
            //sqlPar.Add(new SQLSPParameter("@pcIsEdit", paramIn.Data.IsFeeEdit));
            //sqlPar.Add(new SQLSPParameter("@pdPercentageInput", paramIn.Data.PercentageFee));
            //sqlPar.Add(new SQLSPParameter("@pdPercentageOutput", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//12
            //sqlPar.Add(new SQLSPParameter("@bIsEmployee", isEmployee));
            //sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));

            //try
            //{
            //    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
            //        throw new Exception(errMsg);

            //    if (sqlPar[3].ParameterValue.ToString() != null)
            //    {
            //        response.FeeCCY = sqlPar[5].ParameterValue.ToString();
            //        response.Fee = decimal.Parse(sqlPar[6].ParameterValue.ToString());
            //        response.PercentageOutput = decimal.Parse(sqlPar[12].ParameterValue.ToString());

            //        apiMessageResponse.Data = response;
            //        apiMessageResponse.IsSuccess = true;
            //    }

            //    else
            //        throw new Exception("Data Switching Fee Not Found!");
            //}
            //catch (Exception ex)
            //{
            //    this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
            //    apiMessageResponse.IsSuccess = false;
            //    apiMessageResponse.ErrorCode = "500";
            //    apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            //}
            //finally
            //{
            //    apiMessageResponse.MessageDateTime = DateTime.Now;
            //}

            //20230728, Andi, RDN-1017, begin
                    
            if (paramIn.Data.IsRDB)
            {
                spName = "dbo.ReksaCalcSwitchingRDBFee";
                nCondition = 2;
                //msgResponse.Data = new RateRes();
                //msgResponse.Data.IsRDB = Convert.ToInt32(dsDataOut.Tables[0].Rows[0]["IsRDB"].ToString());
            }
            else 
            {
                spName = "dbo.ReksaCalcSwitchingFee";
                nCondition = 1;
            }


            if (nCondition == 1) //Swc Non RDB
            {
                //parameter
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcProdSwitchOut", strProdNameSwcOut));
                sqlPar.Add(new SQLSPParameter("@pcProdSwitchIn", strProdNameSwcIn));
                sqlPar.Add(new SQLSPParameter("@pbJenis", true));
                sqlPar.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.TranAmt));
                sqlPar.Add(new SQLSPParameter("@pmUnit", paramIn.Data.TranUnit));
                sqlPar.Add(new SQLSPParameter("@pcFeeCCY", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//5
                sqlPar.Add(new SQLSPParameter("@pnFee", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//6
                sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID));
                sqlPar.Add(new SQLSPParameter("@pmNAV", 0));
                sqlPar.Add(new SQLSPParameter("@pcIsEdit", paramIn.Data.IsFeeEdit));
                sqlPar.Add(new SQLSPParameter("@pdPercentageInput", paramIn.Data.PercentageFee));
                sqlPar.Add(new SQLSPParameter("@pdPercentageOutput", 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//12
                sqlPar.Add(new SQLSPParameter("@bIsEmployee", isEmployee));
                sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (sqlPar[3].ParameterValue.ToString() != null)
                {
                    response.FeeCCY = sqlPar[5].ParameterValue.ToString();
                    response.Fee = decimal.Parse(sqlPar[6].ParameterValue.ToString());
                    response.PercentageOutput = decimal.Parse(sqlPar[12].ParameterValue.ToString());

                    apiMessageResponse.Data = response;
                    apiMessageResponse.IsSuccess = true;
                }

                    else
                        throw new Exception("Data Switching Fee Not Found!");
                }
                catch (Exception ex)
                {
                    this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                    apiMessageResponse.IsSuccess = false;
                    apiMessageResponse.ErrorCode = "500";
                    apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
                }
                finally
                {
                    apiMessageResponse.MessageDateTime = DateTime.Now;
                }
            }
            else if (nCondition == 2) //Swc  RDB
            {
                //variable untuk menerima kode produk switching
                string KodeProdSwitchOut = "";
                string KodeProdSwitchIn = "";
                //ambil data dari ReksaProduct_TM, kemudian dimasukkan ke variable baru diatas
                string strQueryProd = @"
                        declare 
                            @pcProdSwitchOut varchar(20),
                            @pcProdSwitchIn varchar(20),
                            @nProdSwitchOut int,
                            @nProdSwitchIn int

                        set @pcProdSwitchOut = '" + strProdNameSwcOut + @"'
                        set @pcProdSwitchIn = '" + strProdNameSwcIn + @"'

                        --ini untuk prodSwitchOut
                        set @nProdSwitchOut = (select ProdId from ReksaProduct_TM where ProdCode = @pcProdSwitchOut)

                        --ini untuk prodSwitchIn
                        set @nProdSwitchIn = (select ProdId from ReksaProduct_TM where ProdCode = @pcProdSwitchIn)

                        
                        select @nProdSwitchOut as ProdSwitchOut, @nProdSwitchIn as ProdSwitchIn
                        
                    ";
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, strQueryProd, out dsResult, out errMsg))
                {
                    if (dsResult.Tables[0].Rows.Count > 0)
                    {
                        KodeProdSwitchOut = dsResult.Tables[0].Rows[0]["ProdSwitchOut"].ToString();
                        KodeProdSwitchIn = dsResult.Tables[0].Rows[0]["ProdSwitchIn"].ToString();
                    }
                }

                //parameter
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pnProdSwitchOut", KodeProdSwitchOut));
                sqlPar.Add(new SQLSPParameter("@pnClientSwitchOut", paramIn.Data.ClientIdSwcOut));
                sqlPar.Add(new SQLSPParameter("@pmUnit", paramIn.Data.TranUnit));
                sqlPar.Add(new SQLSPParameter("@pcFeeCCY", "", (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT)); //3
                sqlPar.Add(new SQLSPParameter("@pnFee", 0, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//4
                sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID));
                sqlPar.Add(new SQLSPParameter("@pcIsEdit", paramIn.Data.IsFeeEdit));
                sqlPar.Add(new SQLSPParameter("@pdPercentageInput", paramIn.Data.PercentageFee));
                sqlPar.Add(new SQLSPParameter("@pdPercentageOutput", 0, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));//9
                sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
                sqlPar.Add(new SQLSPParameter("@pnProdSwitchIn", KodeProdSwitchIn));
                sqlPar.Add(new SQLSPParameter("@pcRefID", ""));

                try
                {
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                        throw new Exception(errMsg);

                    if (sqlPar[3].ParameterValue.ToString() != null)
                    {
                        response.FeeCCY = sqlPar[3].ParameterValue.ToString();
                        response.Fee = decimal.Parse(sqlPar[4].ParameterValue.ToString());
                        response.PercentageOutput = decimal.Parse(sqlPar[9].ParameterValue.ToString());

                        apiMessageResponse.Data = response;
                        apiMessageResponse.IsSuccess = true;
                    }

                    else
                        throw new Exception("Data Switching Fee Not Found!");
                }
                catch (Exception ex)
                {
                    this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                    apiMessageResponse.IsSuccess = false;
                    apiMessageResponse.ErrorCode = "500";
                    apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
                }
                finally
                {
                    apiMessageResponse.MessageDateTime = DateTime.Now;
                }
            }
            //20230728, Andi, RDN-1017, end
            return apiMessageResponse;
        }
        #endregion

        #region ReksaSrcReferentor
        public ApiMessage<ReksaSrcReferentorRes> ReksaSrcReferentor(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaSrcReferentorRes response = new ReksaSrcReferentorRes();
            List<ReksaSrcReferentorRes> listResponse = new List<ReksaSrcReferentorRes>();
            ApiMessage<ReksaSrcReferentorRes> apiMessageResponse = new ApiMessage<ReksaSrcReferentorRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaSrcReferentor", errMsg = "", strNAME = "";
            bool isValidate = true;

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@cCol1", paramIn.Data.Referentor));
            sqlPar.Add(new SQLSPParameter("@cCol2", strNAME));
            sqlPar.Add(new SQLSPParameter("@bValidate", isValidate));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                #region Cek Table Result
                //Data
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    listResponse = JsonConvert.DeserializeObject<List<ReksaSrcReferentorRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    apiMessageResponse.Data = listResponse[0];
                    apiMessageResponse.IsSuccess = true;
                }
                else
                    throw new Exception("Data Referentor not found !");
                #endregion
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
            
            return apiMessageResponse;
        }
        #endregion

        #region ReksaSrcWaperd
        public ApiMessage<ReksaSrcWaperdRes> ReksaSrcWaperd(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaSrcWaperdRes response = new ReksaSrcWaperdRes();
            List<ReksaSrcWaperdRes> listResponse = new List<ReksaSrcWaperdRes>();
            ApiMessage<ReksaSrcWaperdRes> apiMessageResponse = new ApiMessage<ReksaSrcWaperdRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaSrcWaperd", errMsg = "", strWaperdNo = "";
            bool isValidate = false;

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@cCol1", paramIn.Data.Waperd));
            sqlPar.Add(new SQLSPParameter("@cCol2", strWaperdNo));
            sqlPar.Add(new SQLSPParameter("@bValidate", isValidate));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                #region Cek Table Result
                //Data
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    listResponse = JsonConvert.DeserializeObject<List<ReksaSrcWaperdRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    if (listResponse[0].DateExpire.Date < DateTime.Now.Date)
                    {
                        throw new Exception("Waperd Expired !");
                    }
                    apiMessageResponse.Data = listResponse[0];
                    apiMessageResponse.IsSuccess = true;
                }
                else
                    throw new Exception("Data Waperd not found !");
                #endregion
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
           
            return apiMessageResponse;
        }
        #endregion

        #region ReksaGetRiskProfile
        public ApiMessage<ReksaGetRiskProfileRes> ReksaGetRiskProfile(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ReksaGetRiskProfileRes response = new ReksaGetRiskProfileRes();
            List<ReksaGetRiskProfileRes> listResponse = new List<ReksaGetRiskProfileRes>();
            ApiMessage<ReksaGetRiskProfileRes> apiMessageResponse = new ApiMessage<ReksaGetRiskProfileRes>();
            apiMessageResponse.copyHeaderForReply(paramIn);

            string spName = "ReksaGetRiskProfile", errMsg = "";

            DataSet dsResult;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();

            //parameter
            sqlPar = new List<SQLSPParameter>();
            sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.ProdIdSwcOut));

            try
            {
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                #region Cek Table Result
                //Data
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    listResponse = JsonConvert.DeserializeObject<List<ReksaGetRiskProfileRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    apiMessageResponse.Data = listResponse[0];
                    apiMessageResponse.IsSuccess = true;
                }
                else
                    throw new Exception("Data ReksaGetRiskProfileRes not found !");
                #endregion
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMessageResponse.IsSuccess = false;
                apiMessageResponse.ErrorCode = "500";
                apiMessageResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMessageResponse.MessageDateTime = DateTime.Now;
            }
            
            return apiMessageResponse;
        }
        #endregion

        #endregion
        //20201201, Dennis, RDN2-23, begin

        #endregion PreSaveValidation

        #region Transaction

        #region OA
        public ApiMessage<OATransaksiReksaRes> InquiryTransactionData(ApiMessage<OATransaksiReksaReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<OATransaksiReksaRes> apiMsgResponse = new ApiMessage<OATransaksiReksaRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);
            OATransaksiReksaRes response = new OATransaksiReksaRes();
            response.Detail_News = new Detail_New();

            string errMsg = "";
            string strQuery = "";
            DataSet dsResult = null;

            apiMsgResponse.copyHeaderForReply(paramIn);

            List<int> nTranId = new List<int>();
            List<string> cTranCode = new List<string>();

            try
            {
                #region old
                /*
                #region Query
                if (paramIn.Data.RefId.Contains("SUBS") || paramIn.Data.RefId.Contains("RDB") || paramIn.Data.RefId.Contains("RDMP"))
                {
                    strQuery = @"
                    if exists (select top 1 1 from ReksaTransaction_TT where RefID = @pcRefID)
                    begin
                        select rt.RefID as 'NoReferensi'
                        , TranType as 'JenisTransaksi'
                        , rt.TranCode as 'KodeTransaksi'
                        , TranDate as 'TglTransaksi'
                        , NAVValueDate as 'TglValuta'
                        , ProdCode as 'KodeProduk'
                        , ClientCode as 'ClientCode'
                        , rt.TranCCY as 'MataUang'
                        , TranAmt as'NominalTransaksi'
                        , TranUnit as 'UnitTransaksi'
                        , rt.FullAmount 
                        , rrsc.JangkaWaktu 
                        , rrsc.JatuhTempo 
                        , rrsc.FreqDebet as 'FrekPendebetan'
                        , rrsc.FreqDebetMethod as 'MetodePendebetan'
                        , isnull(rt.Asuransi, 0)
                        , rt.AutoRedemption
                        , rt.IsFeeEdit
                        , case when TranType = 1 then rt.SubcFee else RedempFee end as 'FeeNominal'
                        , rt.PercentageFee as 'PctFee'
                        , rt.Channel
                        , TranType
                        , rt.TranId
                        from ReksaTransaction_TT rt with(nolock) 
                        join ReksaCIFData_TM rcd with(nolock) on rt.ClientId = rcd.ClientId
                        join ReksaProduct_TM rp with(nolock) on rt.ProdId = rp.ProdId
                        left join ReksaRegulerSubscriptionClient_TM rrsc with(nolock) on rt.ClientId = rrsc.ClientId and rt.RefID = rrsc.RefID
                        where rt.RefID = @pcRefID

                        select WarnMsg, WarnMsg2, WarnMsg3, WarnMsg4
                        from ReksaRMM_TT
                        where RefID = @pcRefID

                        select FnDocID, ObjectStore, DocName
                        from ReksaRMM_TT
                        where RefID = @pcRefID
                end
                else
                begin
                select RefID as 'NoReferensi'
                                        , 'FutureRDB' as 'JenisTransaksi'
                                        , TranCode as 'KodeTransaksi'
                                        , JoinDate as 'TglTransaksi'
                                        , '1900-01-01' as 'TglValuta'
                                        , rp.ProdCode as 'KodeProduk'
                                        , ClientCodeIns as 'ClientCode'
                                        , TranCCY as 'MataUang'
                                        , TranAmount as'NominalTransaksi'
                                        , 0 as 'UnitTransaksi'
                                        , FullAmount 
                                        , JangkaWaktu 
                                        , JatuhTempo 
                                        , FreqDebet as 'FrekPendebetan'
                                        , FreqDebetMethod as 'MetodePendebetan'
                                        , Asuransi
                                        , AutoRedemption
                                        , IsFeeEdit
                                        , SubcFee as 'FeeNominal'
                                        , PercentageFee as 'PctFee'
                                        , Channel
                                        , 8 as 'TranType'
                                        , TranId
	                from ReksaRegulerSubscriptionClient_TM  rrsc
	                join ReksaProduct_TM rp with(nolock) on rrsc.ProdId = rp.ProdId
	                where RefID = @pcRefID

	                select WarnMsg, WarnMsg2, WarnMsg3, WarnMsg4
                                        from ReksaRMM_TT
                                        where RefID = @pcRefID

                                        select FnDocID, ObjectStore, DocName
                                        from ReksaRMM_TT
                                        where RefID = @pcRefID
                end
                        ";
                }

                else if (paramIn.Data.RefId.Contains("SWC"))
                {
                    strQuery = @"
                        select rt.RefID as 'NoReferensi'
                        , TranType as 'JenisTransaksi'
                        , rt.TranCode as 'KodeTransaksi'
                        , TranDate as 'TglTransaksi'
                        , NAVValueDate as 'TglValuta'
                        --, ProdCode as 'KodeProduk'
                        , rp1.ProdCode as 'KodeProdSwitchOut'
                        , rp2.ProdCode as 'KodeProdSwitchIn'
                        , ClientCode as 'ClientCode'
                        , ClientIdSwcOut as 'ClientCodeSwitchOut'
                        , ClientIdSwcOut as 'ClientCodeSwitchIn'
                        , rt.TranCCY as 'MataUang'
                        , TranAmt as'NominalTransaksi'
                        , TranUnit as 'UnitTransaksi'
                        , 0 as 'FullAmount'
                        , rrsc.JangkaWaktu
                        , rrsc.JatuhTempo
                        , rrsc.FreqDebet as 'FrekPendebetan'
                        , rrsc.FreqDebetMethod as 'MetodePendebetan'
                        , rt.Asuransi
                        , rt.AutoRedemption
                        , rt.IsFeeEdit
                        , rt.SwitchingFee 'FeeNominal'
                        , rt.PercentageFee as 'PctFee'
                        , rt.Channel
                        , TranType
                        , rt.TranId
                        --, TrxTaxAmnesty
                        from ReksaSwitchingTransaction_TM rt with(nolock)
                        left join ReksaCIFData_TM rcd with(nolock) on rt.ClientIdSwcOut = rcd.ClientId
                        left join ReksaProduct_TM rp1 with(nolock) on rt.ProdSwitchOut = rp1.ProdId
                        left join ReksaProduct_TM rp2 with(nolock) on rt.ProdSwitchIn = rp1.ProdId
                        left join ReksaRegulerSubscriptionClient_TM rrsc with(nolock) on rt.ClientIdSwcOut = rrsc.ClientId and rt.RefID = rrsc.RefID
                        where rt.RefID = @pcRefID
                        
                        select top 1 WarnMsg, WarnMsg2, WarnMsg3, WarnMsg4
                        from ReksaRMM_TT
                        where RefID = @pcRefID

                        select top 1FnDocID, ObjectStore, DocName
                        from ReksaRMM_TT
                        where RefID = @pcRefID
                        ";
                        

                }
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pcRefID", paramIn.Data.RefId);

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa,  this._ignoreSSL, strQuery, ref sqlParam, out dsResult, out errMsg))
                {
                    if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                        throw new Exception("Data transaction for " + paramIn.Data.RefId + " not found !");

                    if (dsResult.Tables[2].Rows[0]["DocName"].ToString() == "")
                        throw new Exception("Transaction "  + paramIn.Data.RefId + " not for OneApproval !");

                    response.Detail_News = JsonConvert.DeserializeObject<List<Detail_New>>(
                                         JsonConvert.SerializeObject(dsResult.Tables[0],
                                                 Newtonsoft.Json.Formatting.None,
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 }));
                    response.TabTables = new TabTable();
                    response.TabTables.RiskProfile = dsResult.Tables[1].Rows[0]["WarnMsg2"].ToString();
                    response.TabTables.UmurNasabah = dsResult.Tables[1].Rows[0]["WarnMsg3"].ToString();
                    response.TabTables.TieringNotification = "";

                    response.Docs = new Document();
                    response.Docs.DocName = dsResult.Tables[2].Rows[0]["DocName"].ToString();
                    response.Docs.FnDocID = dsResult.Tables[2].Rows[0]["FnDocID"].ToString();
                    response.Docs.ObjectStore = dsResult.Tables[2].Rows[0]["ObjectStore"].ToString();

                    if(response.Detail_News[0].tglValuta.Value.ToString("yyyy-MM-dd") == "1900-01-01")
                    {
                        response.Detail_News[0].tglValuta = null;
                    }
                    
                    apiMsgResponse.Data = response;
                    apiMsgResponse.IsSuccess = true;
                } */
                #endregion old

                if (paramIn.Data.TranId == 0 && paramIn.Data.TranCode == null)
                {
                    nTranId = this.getTranIdAuthorize(paramIn.Data.RefId);
                    cTranCode = this.getTranCodeAuthorize(paramIn.Data.RefId);
                }

                else
                {
                    nTranId.Add(paramIn.Data.TranId);
                    cTranCode.Add(paramIn.Data.TranCode);
                }

                if(paramIn.Data.RefId.Contains("RDB"))
                {
                    nTranId.Clear();
                    cTranCode.Clear();
                    //20220919, Rendy, M32022, begin
                    //nTranId.Add(0);
                    nTranId.Add(paramIn.Data.TranId);
                    //20220919, Rendy, M32022, end
                    cTranCode.Add(paramIn.Data.TranCode);
                }

                if (paramIn.Data.RefId.Contains("SWC"))
                {
                    //nTranId = this.getTranIdAuthorizeSwitching(paramIn.Data.RefId);
                    //cTranCode = this.getTranCodeAuthorize(paramIn.Data.RefId);
                    nTranId.Add(paramIn.Data.TranId);
                    cTranCode.Add(paramIn.Data.TranCode);
                }

                ApiMessage<JObject> msgResponse = new ApiMessage<JObject>();

                //20230728, Lita, RDN-1017, begin
                //if (paramIn.Data.RefId.Contains("SWC"))
                if (paramIn.Data.RefId.Contains("SWC") || paramIn.Data.RefId.Contains("SRDB"))
                //20230728, Lita, RDN-1017, end
                {
                    JObject msgRequest = new JObject();
                    msgRequest.Add("Id", nTranId[0]);
                    msgRequest.Add("NIK", paramIn.UserNIK);
                    msgRequest.Add("Guid", paramIn.TransactionMessageGUID);

                    string strMethodApiUrl = _apiOAReksaInquiryDataSwitching;
                    RestWSClient<ApiMessage<JObject>> restAPI = new RestWSClient<ApiMessage<JObject>>(_ignoreSSL);
                    msgResponse = restAPI.invokeRESTServicePost(strMethodApiUrl, msgRequest);

                    if (!msgResponse.IsSuccess)
                        throw new Exception("Inquiry AuthTransaksi Detail Switching " + msgResponse.ErrorDescription);
                } 
                else
                {
                    JObject msgRequest = new JObject();
                    msgRequest.Add("id", nTranId[0] + "|||" + cTranCode[0]);
                    msgRequest.Add("NIK", paramIn.UserNIK);
                    msgRequest.Add("Guid", paramIn.TransactionMessageGUID);

                    string strMethodApiUrl = _apiOAReksaInquiryData;
                    RestWSClient<ApiMessage<JObject>> restAPI = new RestWSClient<ApiMessage<JObject>>(_ignoreSSL);
                    msgResponse = restAPI.invokeRESTServicePost(strMethodApiUrl, msgRequest);

                    if (!msgResponse.IsSuccess)
                        throw new Exception("Inquiry AuthTransaksi DetailTransaction gagal " + msgResponse.ErrorDescription);
                }

                

                strQuery = @"
                        select top 1 FnDocID, ObjectStore, DocName
                        from ReksaRMM_TT
                        where RefID = @pcRefID";

                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pcRefID", paramIn.Data.RefId);

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, strQuery, ref sqlParam, out dsResult, out errMsg))
                {
                    if (dsResult.Tables[0].Rows[0]["DocName"].ToString() == "")
                        throw new Exception("Transaction " + paramIn.Data.RefId + " not for OneApproval !");

                    response.Docs = new Document();
                    response.Docs.DocName = dsResult.Tables[0].Rows[0]["DocName"].ToString();
                    response.Docs.FnDocID = dsResult.Tables[0].Rows[0]["FnDocID"].ToString();
                    response.Docs.ObjectStore = dsResult.Tables[0].Rows[0]["ObjectStore"].ToString();
                }

                response.TabTables = JsonConvert.DeserializeObject<OATransaksiReksaRes.TabTable>(msgResponse.Data["TabTable"].ToString());
				 //20250821, Andhika J, RDN-1264 , begin
                response.TabTables.RemarkFromRMM = paramIn.Data.remark;
                //20250821, Andhika J, RDN-1264 , end
                response.Detail_News = JsonConvert.DeserializeObject<OATransaksiReksaRes.Detail_New>(msgResponse.Data["Detail_NEW"].ToString());

                apiMsgResponse.Data = response;

            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }
        public ApiMessage<ONEApprovalResponse> PushToONEApproval(ApiMessage<ONEApprovalRequest> inModel)
        {
            ApiMessage<ONEApprovalResponse> msgResponse = new ApiMessage<ONEApprovalResponse>();
            msgResponse.copyHeaderForReply(inModel);
            string strUrlOAApprove = this._configuration["apiOAReksaProcessApprovalURL"];
            string strUrlOAReject = this._configuration["apiOAReksaProcessApprovalURL"];
            string strUrlOAApproveSwitching = this._configuration["apiOAReksaProcessApprovalSwitchingURL"];
            string strUrlOARejectSwitching = this._configuration["apiOAReksaProcessApprovalSwitchingURL"];
            
            string strUrlOAPush = this._configuration["apiOAPushApprovalURL"] + @"KafkaToSQL/PushApproval";
            string strTrxType = ""; //, strTranId = "";
            SqlParameter[] dbPar = new SqlParameter[3];
            // string sqlCmd = "";
            //long nDealId = 0; 
            // string strErrMsg = "";
            long nOAApprovalId = 0;
            
            try
            {
                #region Push to API ONEApproval
                string strJSONData = EDEHelper.GetValue(inModel, "$.Data.JSONData");
                strTrxType = EDEHelper.GetValue(JObject.Parse(strJSONData), "$.DETAIL_NEW.transactionDetails[0].TranType");
                //strTranId = EDEHelper.GetValue(JObject.Parse(strJSONData), "$.DetailTransaksi.TranId");

                //if (string.IsNullOrEmpty(strTrxType))
                //    throw new Exception("[PushToOA] - Transaction Description harus  !");
                //if (string.IsNullOrEmpty(strTranId))
                //    throw new Exception("[PushToOA] - TranId tidak boleh kosong !");
                //if (!long.TryParse(strTranId, out nDealId))
                //    throw new Exception("[PushToOA] - TranId tidak numerik !");

                //20230728, Lita, RDN-1017, begin
                //if (strTrxType == "5" || strTrxType == "6")
                if (strTrxType == "5" || strTrxType == "6" || strTrxType == "9")
                //20230728, Lita, RDN-1017, end
                {
                    inModel.Data.ApproveAPIURL = strUrlOAApproveSwitching;
                    inModel.Data.RejectAPIURL = strUrlOARejectSwitching;
                   
                }
                else
                {
                    inModel.Data.ApproveAPIURL = strUrlOAApprove;
                    inModel.Data.RejectAPIURL = strUrlOAReject;
                }

                Dictionary<string, string> dictionaryHeader = new Dictionary<string, string>();
                dictionaryHeader.Add("NIK", inModel.UserNIK);
                dictionaryHeader.Add("Subsystem", "Wealth.ReksaAccountTransaction.API");
                dictionaryHeader.Add("GUID", inModel.TransactionMessageGUID);
                dictionaryHeader.Add("XGENId", inModel.Data.XGENId);

                RestWSClient<ONEApprovalResponse> restAPI = new RestWSClient<ONEApprovalResponse>(this._ignoreSSL);
                ONEApprovalResponse responseAPI = new ONEApprovalResponse();
                responseAPI = restAPI.invokeRESTServicePost(strUrlOAPush, "", dictionaryHeader, inModel.Data);

                if (!responseAPI.IsSuccess)
                    throw new Exception(responseAPI.Description);

                if (!long.TryParse(responseAPI.ONEApprovalId, out nOAApprovalId))
                    throw new Exception("[PushToOA] - Approval Id from ONEApproval bukan numerik !");
                #endregion
                
                msgResponse.IsSuccess = true;
                msgResponse.Data = new ONEApprovalResponse();
                msgResponse.Data.ONEApprovalId = responseAPI.ONEApprovalId;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + inModel.getJSONString() + "; Error = > " + ex.Message, inModel.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }
        #endregion

        #endregion Transaction

        #region Sub

        #region Lump Sum
        public ApiMessage<ReksaMaintainAllTransaksiNewRes> saveTransactionLumpSum(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ApiMessage<ReksaMaintainAllTransaksiNewRes> apiMsgResponse = new ApiMessage<ReksaMaintainAllTransaksiNewRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);
            ReksaMaintainAllTransaksiNewRes responsesClass = new ReksaMaintainAllTransaksiNewRes();
            ReksaMaintainAllTransaksiNew response1 = new ReksaMaintainAllTransaksiNew();
            List<MandatoryField> response2 = new List<MandatoryField>();
            List<DetailTransaction> response3 = new List<DetailTransaction>();
            //20230306, Andhika J, RDN-903, begin
            ApiMessage<ReksaMaintainAllTransaksiNewRq> ReqAPIMessage = new ApiMessage<ReksaMaintainAllTransaksiNewRq>();
            ReqAPIMessage.Data = new ReksaMaintainAllTransaksiNewRq();
            //20230306, Andhika J, RDN-903, end
            DateTime datenow = DateTime.Now;

            apiMsgResponse.copyHeaderForReply(paramIn);
            apiMsgResponse.MessageDateTime = DateTime.Now;
            apiMsgResponse.MessageGUID = paramIn.MessageGUID;
            apiMsgResponse.UserNIK = paramIn.UserNIK.ToString();
            apiMsgResponse.ModuleName = "Wealth.ReksaAccountTransaction SUB Lump Sum";

            //membuat xml
            XmlDocument xmlDataSubs = new XmlDocument();
            XmlDocument xmlDataRedemp = new XmlDocument();
            XmlDocument xmlDataRDB = new XmlDocument();

            string jsonDataSubs = "";
            string resultSubs = "";

            if (paramIn.Data.TransactionType == "SUBS")
            {
                for(int i = 0; i < paramIn.Data.Subscription.Count; i++)
                {
                    SubscriptionReq temp = paramIn.Data.Subscription[i];
                    jsonDataSubs = JsonConvert.SerializeObject(temp);
                    xmlDataSubs = JsonConvert.DeserializeXmlNode(jsonDataSubs, "Subscription");
                    resultSubs += resultSubs + xmlDataSubs.InnerXml.ToString();
                }

            }

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaMaintainAllTransaksiNew", errMsg = "";
            //20230306, Andhika J, RDN-903, begin
            //string strSPName = "ReksaMaintainAllTransaksiNew", errMsg = "";
            string strSPName = "", errMsg = "";
            //20230306, Andhika J, RDN-903, end
            //20220425, Rendy, M32022-4, end

            DataSet dsResult = new DataSet();

            paramIn.Data.WarnMsg = "";
            paramIn.Data.WarnMsg2 = "";
            paramIn.Data.WarnMsg3 = "";
            paramIn.Data.WarnMsg4 = "";
            paramIn.Data.RaiseErrorMessage = "";

            try
            {
                //20230306, Andhika J, RDN-903, begin
                #region RemarkExisting
                //param = new List<SQLSPParameter>();
                //param.Add(new SQLSPParameter("@pnType", paramIn.Data.Type));
                //param.Add(new SQLSPParameter("@pcTranType", paramIn.Data.TransactionType, 20));
                //param.Add(new SQLSPParameter("@pcRefID", paramIn.Data.ReferenceID, 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
                //param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 13));
                //param.Add(new SQLSPParameter("@pcOfficeId", paramIn.Data.OfficeId, 5));
                //param.Add(new SQLSPParameter("@pcNoRekening", "", 20));
                //param.Add(new SQLSPParameter("@pvcXMLTrxSubscription", "<DocumentElement>" + resultSubs + "</DocumentElement>"));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRedemption", xmlDataRedemp.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRDB", xmlDataRDB.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pcInputter", paramIn.Data.Inputter, 40));
                //param.Add(new SQLSPParameter("@pnSeller", paramIn.Data.Seller));
                //param.Add(new SQLSPParameter("@pnWaperd", paramIn.Data.Waperd));
                //param.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                //param.Add(new SQLSPParameter("@pnReferentor", paramIn.Data.Referentor));
                //param.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID, 50));
                //param.Add(new SQLSPParameter("@pcWarnMsg", paramIn.Data.WarnMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg2", paramIn.Data.WarnMsg2, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg3", paramIn.Data.WarnMsg3, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbDocFCSubscriptionForm", paramIn.Data.DocFCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocFCDevidentAuthLetter", paramIn.Data.DocFCDevidentAuthLetter));
                //param.Add(new SQLSPParameter("@pbDocFCJoinAcctStatementLetter", paramIn.Data.DocFCJoinAcctStatementLetter));
                //param.Add(new SQLSPParameter("@pbDocFCIDCopy", paramIn.Data.DocFCIDCopy));
                //param.Add(new SQLSPParameter("@pbDocFCOthers", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pbDocTCSubscriptionForm", paramIn.Data.DocTCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocTCTermCondition", paramIn.Data.DocTCTermCondition));
                //param.Add(new SQLSPParameter("@pbDocTCProspectus", paramIn.Data.DocTCProspectus));
                //param.Add(new SQLSPParameter("@pbDocTCFundFactSheet", paramIn.Data.DocTCFundFactSheet));
                //param.Add(new SQLSPParameter("@pbDocTCOthers", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcDocFCOthersList", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pcDocTCOthersList", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcWarnMsg4", paramIn.Data.WarnMsg4, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbIsAOANonSII", 0));
                //param.Add(new SQLSPParameter("@pcNoRek", paramIn.Data.NoRekening, 20));
                //param.Add(new SQLSPParameter("@pcNoRekCcy", paramIn.Data.Subscription[0].CCY, 4));
                ////20220425, Rendy, M32022-4, begin
                ////param.Add(new SQLSPParameter("@pcRaiseErrorMessage", paramIn.Data.RaiseErrorMessage, 1000, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                ////20220425, Rendy, M32022-4, end

                //if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                //    throw new Exception(errMsg);
                ////throw new Exception(param[34].ParameterValue.ToString());

                ////if (param[2].ParameterValue.ToString() == "" || param[34].ParameterValue.ToString() != "")
                //if (param[2].ParameterValue.ToString() == "")
                //{
                //    //Data 1
                //    if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                //    {
                //        response2 = JsonConvert.DeserializeObject<List<MandatoryField>>(
                //                            JsonConvert.SerializeObject(dsResult.Tables[0],
                //                                    Newtonsoft.Json.Formatting.None,
                //                                    new JsonSerializerSettings
                //                                    {
                //                                        NullValueHandling = NullValueHandling.Ignore
                //                                    }));
                //        responsesClass.MandatoryField2 = response2;
                //    }

                //    //Data 2
                //    //if (param[34].ParameterValue.ToString() != "")
                //    //    throw new Exception(param[34].ParameterValue.ToString());
                    
                //    response1.RefID = param[2].ParameterValue.ToString();
                //    response1.WarnMsg = param[15].ParameterValue.ToString();
                //    response1.WarnMsg2 = param[16].ParameterValue.ToString();
                //    response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //    response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //    response1.NIK = paramIn.UserNIK;
                //    response1.MsgGuid = paramIn.MessageGUID;
                //    response1.TrxGuid = paramIn.TransactionMessageGUID;

                //    responsesClass.ReksaMaintainAllTransaksi1 = response1;

                //    apiMsgResponse.Data = responsesClass;
                //    apiMsgResponse.ErrorDescription = param[34].ParameterValue.ToString();
                //    apiMsgResponse.IsSuccess = false;
                //}
                //if(param[2].ParameterValue.ToString() != "")
                //{
                //    //test
                //    //Data 1
                //    if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                //    {
                //        response2 = JsonConvert.DeserializeObject<List<MandatoryField>>(
                //                            JsonConvert.SerializeObject(dsResult.Tables[0],
                //                                    Newtonsoft.Json.Formatting.None,
                //                                    new JsonSerializerSettings
                //                                    {
                //                                        NullValueHandling = NullValueHandling.Ignore
                //                                    }));
                //        responsesClass.MandatoryField2 = response2;
                //    }

                //    //Data 2
                //    //if (param[34].ParameterValue.ToString() != "")
                //    //    throw new Exception(param[34].ParameterValue.ToString());

                //    response1.RefID = param[2].ParameterValue.ToString();
                //    response1.WarnMsg = param[15].ParameterValue.ToString();
                //    response1.WarnMsg2 = param[16].ParameterValue.ToString();
                //    response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //    response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //    response1.NIK = paramIn.UserNIK;
                //    response1.MsgGuid = paramIn.MessageGUID;
                //    response1.TrxGuid = paramIn.TransactionMessageGUID;

                //    responsesClass.ReksaMaintainAllTransaksi1 = response1;
                //    //end test
                //    response1.RefID = param[2].ParameterValue.ToString();
                //    responsesClass.ReksaMaintainAllTransaksi1 = response1;
                //}
                #endregion RemarkExisting
                ReqAPIMessage.Data.nType = paramIn.Data.Type;
                ReqAPIMessage.Data.cTranType = paramIn.Data.TransactionType;
                ReqAPIMessage.Data.cRefID = paramIn.Data.ReferenceID;
                ReqAPIMessage.Data.cCIFNo = paramIn.Data.CIFNo;
                ReqAPIMessage.Data.cNoRekening = paramIn.Data.NoRekening;
                ReqAPIMessage.Data.cOfficeId = paramIn.Data.OfficeId;
                ReqAPIMessage.Data.vcXMLTrxSubscription = "<DocumentElement>" + resultSubs + "</DocumentElement>";
                ReqAPIMessage.Data.vcXMLTrxRedemption = xmlDataRedemp.OuterXml.ToString();
                ReqAPIMessage.Data.vcXMLTrxRDB = xmlDataRDB.OuterXml.ToString();
                ReqAPIMessage.Data.cInputter = paramIn.Data.Inputter;
                ReqAPIMessage.Data.nSeller = paramIn.Data.Seller;
                ReqAPIMessage.Data.nWaperd = paramIn.Data.Waperd;
                ReqAPIMessage.Data.nNIK = Convert.ToInt32(paramIn.UserNIK);
                ReqAPIMessage.Data.nReferentor = paramIn.Data.Referentor;
                ReqAPIMessage.Data.cGuid = paramIn.TransactionMessageGUID;
                ReqAPIMessage.Data.bDocFCSubscriptionForm = paramIn.Data.DocFCSubscriptionForm;
                ReqAPIMessage.Data.bDocFCDevidentAuthLetter = paramIn.Data.DocFCDevidentAuthLetter;
                ReqAPIMessage.Data.bDocFCJoinAcctStatementLetter = paramIn.Data.DocFCJoinAcctStatementLetter;
                ReqAPIMessage.Data.bDocFCIDCopy = paramIn.Data.DocFCIDCopy;
                ReqAPIMessage.Data.bDocFCOthers = paramIn.Data.DocFCOthers;
                ReqAPIMessage.Data.bDocTCSubscriptionForm = paramIn.Data.DocTCSubscriptionForm;
                ReqAPIMessage.Data.bDocTCTermCondition = paramIn.Data.DocTCTermCondition;
                ReqAPIMessage.Data.bDocTCProspectus = paramIn.Data.DocTCProspectus;
                ReqAPIMessage.Data.bDocTCFundFactSheet = paramIn.Data.DocTCFundFactSheet;
                ReqAPIMessage.Data.bDocTCOthers = paramIn.Data.DocTCOthers;
                ReqAPIMessage.Data.cDocFCOthersList = paramIn.Data.DocFCOthers.ToString();
                ReqAPIMessage.Data.bIsAOANonSII = false;
                ReqAPIMessage.Data.cNoRek = paramIn.Data.NoRekening;
                //20240925, Andhika J, RFR-56218, begin
                ReqAPIMessage.Data.cNoRekCcy = paramIn.Data.Subscription[0].CCY.ToString();
                //20240925, Andhika J, RFR-56218, end
                ReqAPIMessage.MessageDateTime = DateTime.Now;
                ReqAPIMessage.MessageGUID = paramIn.MessageGUID;
                ReqAPIMessage.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ReqAPIMessage.UserNIK = paramIn.UserNIK.ToString();
                #region HitAPI
                apiMsgResponse = ReksaMaintainAllTransaksiNew(ReqAPIMessage);
                if (apiMsgResponse.IsSuccess == true)
                {
                    if (apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID == "")
                    {
                        //apiMsgResponse.ErrorDescription = ReqAPIMessage.ErrorDescription;
                        apiMsgResponse.IsSuccess = false;
                    }
                    else
                    {
                        apiMsgResponse.IsSuccess = true;
                    }
                    
                }
                else
                {

                    throw new Exception(apiMsgResponse.ErrorDescription);
                }
                #endregion HitAPI

                //20230306, Andhika J, RDN-903, end
                if (!string.IsNullOrEmpty(apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID))
                {
                    #region Query
                    string sqlCommand = @"
                        SELECT rt.TranId, rt.TranCode, rt.ProdId, rp.ProdName
                        FROM ReksaTransaction_TT rt with(nolock)
                        join ReksaProduct_TM rp with(nolock) on rp.ProdId = rt.ProdId
                        where rt.RefID = @pcRefId 

                        INSERT ReksaRMM_TT 
                        values (@pcRefId, @cWarnMsg, @cWarnMsg2, @cWarnMsg3, @cWarnMsg4, @cFnDocID, @cObjectStore, @cDocName)

                        UPDATE ReksaTransaction_TT
                        set Channel = 'RMM'
                        where RefID = @pcRefId
                    ";
                    #endregion

                    //20230306, Andhika J, RDN-903, begin
                    SqlParameter[] sqlParam = new SqlParameter[8];
                    //sqlParam[0] = new SqlParameter("@pcRefId", response1.RefID);
                    //sqlParam[1] = new SqlParameter("@cWarnMsg", response1.WarnMsg == null ? "" : response1.WarnMsg);
                    //sqlParam[2] = new SqlParameter("@cWarnMsg2", response1.WarnMsg2 == null ? "" : response1.WarnMsg2);
                    //sqlParam[3] = new SqlParameter("@cWarnMsg3", response1.WarnMsg3 == null ? "" : response1.WarnMsg3);
                    //sqlParam[4] = new SqlParameter("@cWarnMsg4", response1.WarnMsg4 == null ? "" : response1.WarnMsg4);
                    //sqlParam[5] = new SqlParameter("@cFnDocID", paramIn.Data.FnDocID == null ? "" : paramIn.Data.FnDocID);
                    //sqlParam[6] = new SqlParameter("@cObjectStore", paramIn.Data.ObjectStore == null ? "" : paramIn.Data.ObjectStore);
                    //sqlParam[7] = new SqlParameter("@cDocName", paramIn.Data.DocName == null ? "" : paramIn.Data.DocName);
                    sqlParam[0] = new SqlParameter("@pcRefId", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID);
                    sqlParam[1] = new SqlParameter("@cWarnMsg", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg);
                    sqlParam[2] = new SqlParameter("@cWarnMsg2", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg2 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg2);
                    sqlParam[3] = new SqlParameter("@cWarnMsg3", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg3 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg3);
                    sqlParam[4] = new SqlParameter("@cWarnMsg4", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg4 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg4);
                    sqlParam[5] = new SqlParameter("@cFnDocID", paramIn.Data.FnDocID == null ? "" : paramIn.Data.FnDocID);
                    sqlParam[6] = new SqlParameter("@cObjectStore", paramIn.Data.ObjectStore == null ? "" : paramIn.Data.ObjectStore);
                    sqlParam[7] = new SqlParameter("@cDocName", paramIn.Data.DocName == null ? "" : paramIn.Data.DocName);
                    //20230306, Andhika J, RDN-903, end

                    if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                        throw new Exception(errMsg);
                    if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                        throw new Exception("TranId : " + apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID + " not found !");

                    response3 = JsonConvert.DeserializeObject<List<DetailTransaction>>(
                                            JsonConvert.SerializeObject(dsResult.Tables[0],
                                                    Newtonsoft.Json.Formatting.None,
                                                    new JsonSerializerSettings
                                                    {
                                                        NullValueHandling = NullValueHandling.Ignore
                                                    }));

                    //20230306, Andhika J, RDN-903, begin
                    apiMsgResponse.Data.DetailTransactions = response3;
                    //20230306, Andhika J, RDN-903, end

                }
                //20230306, Andhika J, RDN-903, begin
                //apiMsgResponse.Data = responsesClass;
                //20230306, Andhika J, RDN-903, end
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            
            return apiMsgResponse;
        }
        #endregion Lump Sum
        
        #region RDB
        public ApiMessage<ReksaMaintainAllTransaksiNewRes> saveTransactionRDB(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ApiMessage<ReksaMaintainAllTransaksiNewRes> apiMsgResponse = new ApiMessage<ReksaMaintainAllTransaksiNewRes>();
            ReksaMaintainAllTransaksiNewRes responsesClass = new ReksaMaintainAllTransaksiNewRes();
            ReksaMaintainAllTransaksiNew response1 = new ReksaMaintainAllTransaksiNew();
            List<MandatoryField> response2 = new List<MandatoryField>();
            List<DetailTransaction> response3 = new List<DetailTransaction>();
            //20230306, Andhika J, RDN-903, begin
            ApiMessage<ReksaMaintainAllTransaksiNewRq> ReqAPIMessage = new ApiMessage<ReksaMaintainAllTransaksiNewRq>();
            ReqAPIMessage.Data = new ReksaMaintainAllTransaksiNewRq();
            //20230306, Andhika J, RDN-903, end
            DateTime datenow = DateTime.Now;

            apiMsgResponse.copyHeaderForReply(paramIn);
            apiMsgResponse.MessageDateTime = DateTime.Now;
            apiMsgResponse.MessageGUID = paramIn.MessageGUID;
            apiMsgResponse.UserNIK = paramIn.UserNIK.ToString();
            apiMsgResponse.ModuleName = "Wealth.ReksaAccountTransaction";
            
            //membuat xml
            XmlDocument xmlDataSubs = new XmlDocument();
            XmlDocument xmlDataRedemp = new XmlDocument();
            XmlDocument xmlDataRDB = new XmlDocument();
            
            string jsonDataRDB = "";
            string resultRDB = "";

            if (paramIn.Data.TransactionType == "SUBSRDB")
            {
                int count = paramIn.Data.SubsRDB.Count;
                for (int i = 0; i < count; i++)
                {
                    SubsRDBReq temp = paramIn.Data.SubsRDB[i];
                    jsonDataRDB = JsonConvert.SerializeObject(temp);
                    xmlDataRDB = JsonConvert.DeserializeXmlNode(jsonDataRDB, "SubsRDB");
                    resultRDB += resultRDB + xmlDataRDB.InnerXml.ToString();
                }
            }

            paramIn.Data.WarnMsg = "";
            paramIn.Data.WarnMsg2 = "";
            paramIn.Data.WarnMsg3 = "";
            paramIn.Data.WarnMsg4 = "";

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaMaintainAllTransaksiNew", errMsg = "";  
            //20230306, Andhika J, RDN-903, begin
            //string strSPName = "ReksaMaintainAllTransaksiNew", errMsg = "";
            string strSPName = "", errMsg = "";
            //20230306, Andhika J, RDN-903, end
            //20220425, Rendy, M32022-4, end

            DataSet dsResult = null;

            try
            {
                //20230306, Andhika J, RDN-903, begin
                #region RemarkExisting
                //param = new List<SQLSPParameter>();
                //param.Add(new SQLSPParameter("@pnType", paramIn.Data.Type));
                //param.Add(new SQLSPParameter("@pcTranType", paramIn.Data.TransactionType, 20));
                //param.Add(new SQLSPParameter("@pcRefID", paramIn.Data.ReferenceID, 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
                //param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 13));
                //param.Add(new SQLSPParameter("@pcOfficeId", paramIn.Data.OfficeId, 5));
                //param.Add(new SQLSPParameter("@pcNoRekening", paramIn.Data.NoRekening, 20));
                //param.Add(new SQLSPParameter("@pvcXMLTrxSubscription", xmlDataSubs.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRedemption", xmlDataRedemp.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRDB", "<DocumentElement>" + resultRDB + "</DocumentElement>"));
                //param.Add(new SQLSPParameter("@pcInputter", paramIn.Data.Inputter, 40));
                //param.Add(new SQLSPParameter("@pnSeller", paramIn.Data.Seller));
                //param.Add(new SQLSPParameter("@pnWaperd", paramIn.Data.Waperd));
                //param.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                //param.Add(new SQLSPParameter("@pnReferentor", paramIn.Data.Referentor));
                //param.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID, 50));
                //param.Add(new SQLSPParameter("@pcWarnMsg", paramIn.Data.WarnMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg2", paramIn.Data.WarnMsg2, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg3", paramIn.Data.WarnMsg3, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbDocFCSubscriptionForm", paramIn.Data.DocFCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocFCDevidentAuthLetter", paramIn.Data.DocFCDevidentAuthLetter));
                //param.Add(new SQLSPParameter("@pbDocFCJoinAcctStatementLetter", paramIn.Data.DocFCJoinAcctStatementLetter));
                //param.Add(new SQLSPParameter("@pbDocFCIDCopy", paramIn.Data.DocFCIDCopy));
                //param.Add(new SQLSPParameter("@pbDocFCOthers", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pbDocTCSubscriptionForm", paramIn.Data.DocTCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocTCTermCondition", paramIn.Data.DocTCTermCondition));
                //param.Add(new SQLSPParameter("@pbDocTCProspectus", paramIn.Data.DocTCProspectus));
                //param.Add(new SQLSPParameter("@pbDocTCFundFactSheet", paramIn.Data.DocTCFundFactSheet));
                //param.Add(new SQLSPParameter("@pbDocTCOthers", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcDocFCOthersList", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pcDocTCOthersList", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcWarnMsg4", paramIn.Data.WarnMsg4, 200, ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbIsAOANonSII", 0));
                //param.Add(new SQLSPParameter("@pcNoRek", paramIn.Data.NoRekening, 20));
                //param.Add(new SQLSPParameter("@pcNoRekCcy", paramIn.Data.SubsRDB[0].CCY, 4));
                //20220425, Rendy, M32022-4, begin
                //param.Add(new SQLSPParameter("@pcRaiseErrorMessage", paramIn.Data.RaiseErrorMessage, 1000, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //20220425, Rendy, M32022-4, end

                //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                //{
                //    if (param[2].ParameterValue.ToString() == "")
                //    {
                //        //Data 1
                //        if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                //        {
                //            response2 = JsonConvert.DeserializeObject<List<MandatoryField>>(
                //                                JsonConvert.SerializeObject(dsResult.Tables[0],
                //                                        Newtonsoft.Json.Formatting.None,
                //                                        new JsonSerializerSettings
                //                                        {
                //                                            NullValueHandling = NullValueHandling.Ignore
                //                                        }));

                //            responsesClass.MandatoryField2 = response2;
                //        }

                //        //Data 2
                //        //if (param[34].ParameterValue.ToString() != "")
                //        //    throw new Exception(param[34].ParameterValue.ToString());

                //        response1.RefID = param[2].ParameterValue.ToString();
                //        response1.WarnMsg = param[15].ParameterValue.ToString();
                //        response1.WarnMsg2 = param[16].PadetailTransactionsrameterValue.ToString();
                //        response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //        response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //        response1.NIK = paramIn.UserNIK;
                //        response1.MsgGuid = paramIn.MessageGUID;
                //        response1.TrxGuid = paramIn.TransactionMessageGUID;

                //        responsesClass.ReksaMaintainAllTransaksi1 = response1;

                //        apiMsgResponse.Data = responsesClass;
                //        apiMsgResponse.IsSuccess = false;
                //    }
                //    else
                //    {
                //        //test
                //        //Data 1
                //        if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                //        {
                //            response2 = JsonConvert.DeserializeObject<List<MandatoryField>>(
                //                                JsonConvert.SerializeObject(dsResult.Tables[0],
                //                                        Newtonsoft.Json.Formatting.None,
                //                                        new JsonSerializerSettings
                //                                        {
                //                                            NullValueHandling = NullValueHandling.Ignore
                //                                        }));

                //            responsesClass.MandatoryField2 = response2;
                //        }

                //        //Data 2
                //        //if (param[34].ParameterValue.ToString() != "")
                //        //    throw new Exception(param[34].ParameterValue.ToString());

                //        response1.RefID = param[2].ParameterValue.ToString();
                //        response1.WarnMsg = param[15].ParameterValue.ToString();
                //        response1.WarnMsg2 = param[16].ParameterValue.ToString();
                //        response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //        response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //        response1.NIK = paramIn.UserNIK;
                //        response1.MsgGuid = paramIn.MessageGUID;
                //        response1.TrxGuid = paramIn.TransactionMessageGUID;

                //        responsesClass.ReksaMaintainAllTransaksi1 = response1;
                //        //end test
                //        response1.RefID = param[2].ParameterValue.ToString();
                //        responsesClass.ReksaMaintainAllTransaksi1 = response1;
                //    }
                #endregion RemarkExisting
                ReqAPIMessage.Data.nType = paramIn.Data.Type;
                ReqAPIMessage.Data.cTranType = paramIn.Data.TransactionType;
                ReqAPIMessage.Data.cRefID = paramIn.Data.ReferenceID;
                ReqAPIMessage.Data.cCIFNo = paramIn.Data.CIFNo;
                ReqAPIMessage.Data.cNoRekening = paramIn.Data.NoRekening;
                ReqAPIMessage.Data.cOfficeId = paramIn.Data.OfficeId;
                ReqAPIMessage.Data.vcXMLTrxSubscription = xmlDataSubs.OuterXml.ToString();
                ReqAPIMessage.Data.vcXMLTrxRedemption = xmlDataRedemp.OuterXml.ToString();
                ReqAPIMessage.Data.vcXMLTrxRDB = "<DocumentElement>" + resultRDB + "</DocumentElement>";
                ReqAPIMessage.Data.cInputter = paramIn.Data.Inputter;
                ReqAPIMessage.Data.nSeller = paramIn.Data.Seller;
                ReqAPIMessage.Data.nWaperd = paramIn.Data.Waperd;
                ReqAPIMessage.Data.nNIK = Convert.ToInt32(paramIn.UserNIK);
                ReqAPIMessage.Data.nReferentor = paramIn.Data.Referentor;
                ReqAPIMessage.Data.cGuid = paramIn.TransactionMessageGUID;
                ReqAPIMessage.Data.bDocFCSubscriptionForm = paramIn.Data.DocFCSubscriptionForm;
                ReqAPIMessage.Data.bDocFCDevidentAuthLetter = paramIn.Data.DocFCDevidentAuthLetter;
                ReqAPIMessage.Data.bDocFCJoinAcctStatementLetter = paramIn.Data.DocFCJoinAcctStatementLetter;
                ReqAPIMessage.Data.bDocFCIDCopy = paramIn.Data.DocFCIDCopy;
                ReqAPIMessage.Data.bDocFCOthers = paramIn.Data.DocFCOthers;
                ReqAPIMessage.Data.bDocTCSubscriptionForm = paramIn.Data.DocTCSubscriptionForm;
                ReqAPIMessage.Data.bDocTCTermCondition = paramIn.Data.DocTCTermCondition;
                ReqAPIMessage.Data.bDocTCProspectus = paramIn.Data.DocTCProspectus;
                ReqAPIMessage.Data.bDocTCFundFactSheet = paramIn.Data.DocTCFundFactSheet;
                ReqAPIMessage.Data.bDocTCOthers = paramIn.Data.DocTCOthers;
                ReqAPIMessage.Data.cDocFCOthersList = paramIn.Data.DocFCOthers.ToString();
                ReqAPIMessage.Data.bIsAOANonSII = false;
                ReqAPIMessage.Data.cNoRek = paramIn.Data.NoRekening;
                //20240924, Andhika J, RFR-56218, begin
                ReqAPIMessage.Data.cNoRekCcy = paramIn.Data.SubsRDB[0].CCY.ToString();
                //20240924, Andhika J, RFR-56218, end
                ReqAPIMessage.MessageDateTime = DateTime.Now;
                ReqAPIMessage.MessageGUID = paramIn.MessageGUID;
                ReqAPIMessage.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ReqAPIMessage.UserNIK = paramIn.UserNIK.ToString();
                #region HitAPI
                apiMsgResponse = ReksaMaintainAllTransaksiNew(ReqAPIMessage);
                if (apiMsgResponse.IsSuccess == true)
                {
                    if (apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID == "")
                    {
                        apiMsgResponse.IsSuccess = false;
                    }
                    else
                    {
                        apiMsgResponse.IsSuccess = true;
                    }
                }
                #endregion HitAPI

                //if (!string.IsNullOrEmpty(response1.RefID))
                if (apiMsgResponse.IsSuccess == true)
                {

                    if (!string.IsNullOrEmpty(apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID))
                    //20230306, Andhika J, RDN-903, end
                    {
                        #region Query
                        int result = DateTime.Compare(DateTime.Today, DateTime.Parse(paramIn.Data.SubsRDB[0].TanggalDebet));
                        string sqlCommand = "";
                        if (result == 0)
                        {
                            sqlCommand = @"
                        SELECT rt.TranId, rt.TranCode, rt.ProdId, rp.ProdName
                        FROM ReksaTransaction_TT rt with(nolock)
                        join ReksaProduct_TM rp with(nolock) on rp.ProdId = rt.ProdId
                        where rt.RefID = @pcRefId 

                        INSERT ReksaRMM_TT 
                        values (@pcRefId, @cWarnMsg, @cWarnMsg2, @cWarnMsg3, @cWarnMsg4, @cFnDocID, @cObjectStore, @cDocName)

                        UPDATE ReksaTransaction_TT
                        set Channel = 'RMM'
                        where RefID = @pcRefId

                        --20221021, Lita, RDN-865, begin
                        UPDATE dbo.ReksaRegulerSubscriptionClient_TM
                        set Channel = 'RMM'
                        where RefID = @pcRefId
                        --20221021, Lita, RDN-865, end
                        ";
                        }

                        if (result < 0)
                        {
                            sqlCommand = @"
                        SELECT rt.TranId, rt.TranCode, rt.ProdId, rp.ProdName
                        FROM ReksaRegulerSubscriptionClient_TM rt with(nolock)
                        join ReksaProduct_TM rp with(nolock) on rp.ProdId = rt.ProdId
                        where rt.RefID = @pcRefId 

                        INSERT ReksaRMM_TT 
                        values (@pcRefId, @cWarnMsg, @cWarnMsg2, @cWarnMsg3, @cWarnMsg4, @cFnDocID, @cObjectStore, @cDocName)

                        UPDATE ReksaRegulerSubscriptionClient_TM
                        set Channel = 'RMM'
                        where RefID = @pcRefId";
                        }


                        #endregion

                        SqlParameter[] sqlParam = new SqlParameter[8];
                        //20230306, Andhika J, RDN-903, begin
                        //sqlParam[0] = new SqlParameter("@pcRefId", response1.RefID);
                        //sqlParam[1] = new SqlParameter("@cWarnMsg", response1.WarnMsg == null ? "" : response1.WarnMsg);
                        //sqlParam[2] = new SqlParameter("@cWarnMsg2", response1.WarnMsg2 == null ? "" : response1.WarnMsg2);
                        //sqlParam[3] = new SqlParameter("@cWarnMsg3", response1.WarnMsg3 == null ? "" : response1.WarnMsg3);
                        //sqlParam[4] = new SqlParameter("@cWarnMsg4", response1.WarnMsg4 == null ? "" : response1.WarnMsg4);
                        sqlParam[0] = new SqlParameter("@pcRefId", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID);
                        sqlParam[1] = new SqlParameter("@cWarnMsg", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg);
                        sqlParam[2] = new SqlParameter("@cWarnMsg2", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg2 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg2);
                        sqlParam[3] = new SqlParameter("@cWarnMsg3", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg3 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg3);
                        sqlParam[4] = new SqlParameter("@cWarnMsg4", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg4 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg4);
                        //20230306, Andhika J, RDN-903, end
                        sqlParam[5] = new SqlParameter("@cFnDocID", paramIn.Data.FnDocID == null ? "" : paramIn.Data.FnDocID);
                        sqlParam[6] = new SqlParameter("@cObjectStore", paramIn.Data.ObjectStore == null ? "" : paramIn.Data.ObjectStore);
                        sqlParam[7] = new SqlParameter("@cDocName", paramIn.Data.DocName == null ? "" : paramIn.Data.DocName);

                        if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                            throw new Exception(errMsg);

                        if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                            throw new Exception("TranId : " + apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID + " not found !");

                        response3 = JsonConvert.DeserializeObject<List<DetailTransaction>>(
                                                JsonConvert.SerializeObject(dsResult.Tables[0],
                                                        Newtonsoft.Json.Formatting.None,
                                                        new JsonSerializerSettings
                                                        {
                                                            NullValueHandling = NullValueHandling.Ignore
                                                        }));

                        //20230306, Andhika J, RDN-903, begin
                        //responsesClass.DetailTransactions = response3;
                        apiMsgResponse.Data.DetailTransactions = response3;
                        //20230306, Andhika J, RDN-903, end
                    }

                    apiMsgResponse.IsSuccess = true;
                }
                    //20230306, Andhika J, RDN-903, begin
                    //apiMsgResponse.Data = responsesClass;
                    //20230306, Andhika J, RDN-903, end
                
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        #endregion RDB

        #endregion Sub

        #region Redemp
        
        public ApiMessage<ReksaMaintainAllTransaksiNewRes> saveTransactionRedemp(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ApiMessage<ReksaMaintainAllTransaksiNewRes> apiMsgResponse = new ApiMessage<ReksaMaintainAllTransaksiNewRes>();
            ReksaMaintainAllTransaksiNewRes responsesClass = new ReksaMaintainAllTransaksiNewRes();
            ReksaMaintainAllTransaksiNew response1 = new ReksaMaintainAllTransaksiNew();
            List<MandatoryField> response2 = new List<MandatoryField>();
            List<DetailTransaction> response3 = new List<DetailTransaction>();
            //20230306, Andhika J, RDN-903, begin
            ApiMessage<ReksaMaintainAllTransaksiNewRq> ReqAPIMessage = new ApiMessage<ReksaMaintainAllTransaksiNewRq>();
            ReqAPIMessage.Data = new ReksaMaintainAllTransaksiNewRq();
            //20230306, Andhika J, RDN-903, end
            DateTime datenow = DateTime.Now;
            apiMsgResponse.copyHeaderForReply(paramIn);
            
            apiMsgResponse.copyHeaderForReply(paramIn);
            apiMsgResponse.MessageDateTime = DateTime.Now;
            apiMsgResponse.MessageGUID = paramIn.MessageGUID;
            apiMsgResponse.UserNIK = paramIn.UserNIK.ToString();
            apiMsgResponse.ModuleName = "Wealth.ReksaAccountTransaction.Redemp Lump Sum";
            
            //membuat xml
            XmlDocument xmlDataSubs = new XmlDocument();
            XmlDocument xmlDataRedemp = new XmlDocument();
            XmlDocument xmlDataRDB = new XmlDocument();
            
            string jsonDataRedeemp = "";
            string resultRedem = "";

            if (paramIn.Data.TransactionType == "REDEMP")
            {
                int count = paramIn.Data.Redemption.Count;
                for (int i = 0; i < count; i++)
                {
                    RedemptionReq temp = paramIn.Data.Redemption[i];
                    jsonDataRedeemp = JsonConvert.SerializeObject(temp);
                    xmlDataRedemp = JsonConvert.DeserializeXmlNode(jsonDataRedeemp, "Redemption");
                    resultRedem += resultRedem + xmlDataRedemp.InnerXml.ToString();
                }
            }

            paramIn.Data.WarnMsg = "";
            paramIn.Data.WarnMsg2 = "";
            paramIn.Data.WarnMsg3 = "";
            paramIn.Data.WarnMsg4 = "";

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaMaintainAllTransaksiNew";
            //20230306, Andhika J, RDN-903, begin
            //string strSPName = "ReksaMaintainAllTransaksiNew", errMsg = "";
            string strSPName = "", errMsg = "";
            //20230306, Andhika J, RDN-903, end
            //20220425, Rendy, M32022-4, end

            DataSet dsResult = null;
            //20230306, Andhika J, RDN-903, begin
            //string errMsg = "";
            //20230306, Andhika J, RDN-903, end

            try
            {
                //20230306, Andhika J, RDN-903, begin
                #region RemarkExisting
                //param = new List<SQLSPParameter>();
                //param.Add(new SQLSPParameter("@pnType", paramIn.Data.Type));
                //param.Add(new SQLSPParameter("@pcTranType", paramIn.Data.TransactionType, 20));
                //param.Add(new SQLSPParameter("@pcRefID", paramIn.Data.ReferenceID, 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
                //param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 13));
                //param.Add(new SQLSPParameter("@pcOfficeId", paramIn.Data.OfficeId, 5));
                //param.Add(new SQLSPParameter("@pcNoRekening", paramIn.Data.NoRekening, 20));
                //param.Add(new SQLSPParameter("@pvcXMLTrxSubscription", xmlDataSubs.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRedemption", "<DocumentElement>" + resultRedem + "</DocumentElement>"));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRDB", xmlDataRDB.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pcInputter", paramIn.Data.Inputter, 40));
                //param.Add(new SQLSPParameter("@pnSeller", paramIn.Data.Seller));
                //param.Add(new SQLSPParameter("@pnWaperd", paramIn.Data.Waperd));
                //param.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                //param.Add(new SQLSPParameter("@pnReferentor", paramIn.Data.Referentor));
                //param.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID, 50));
                //param.Add(new SQLSPParameter("@pcWarnMsg", paramIn.Data.WarnMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg2", paramIn.Data.WarnMsg2, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg3", paramIn.Data.WarnMsg3, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbDocFCSubscriptionForm", paramIn.Data.DocFCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocFCDevidentAuthLetter", paramIn.Data.DocFCDevidentAuthLetter));
                //param.Add(new SQLSPParameter("@pbDocFCJoinAcctStatementLetter", paramIn.Data.DocFCJoinAcctStatementLetter));
                //param.Add(new SQLSPParameter("@pbDocFCIDCopy", paramIn.Data.DocFCIDCopy));
                //param.Add(new SQLSPParameter("@pbDocFCOthers", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pbDocTCSubscriptionForm", paramIn.Data.DocTCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocTCTermCondition", paramIn.Data.DocTCTermCondition));
                //param.Add(new SQLSPParameter("@pbDocTCProspectus", paramIn.Data.DocTCProspectus));
                //param.Add(new SQLSPParameter("@pbDocTCFundFactSheet", paramIn.Data.DocTCFundFactSheet));
                //param.Add(new SQLSPParameter("@pbDocTCOthers", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcDocFCOthersList", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pcDocTCOthersList", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcWarnMsg4", paramIn.Data.WarnMsg4, 200, ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbIsAOANonSII", 0));
                //param.Add(new SQLSPParameter("@pcNoRek", paramIn.Data.NoRekening == null ? "": paramIn.Data.NoRekening, 20));
                //param.Add(new SQLSPParameter("@pcNoRekCcy", paramIn.Data.Redemption[0].CCY, 4));
                ////20220425, Rendy, M32022-4, begin
                ////param.Add(new SQLSPParameter("@pcRaiseErrorMessage", paramIn.Data.RaiseErrorMessage, 1000, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                ////20220425, Rendy, M32022-4, end

                //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                //{
                //    //if (param[2].ParameterValue.ToString() == "" || param[34].ParameterValue.ToString() != "")
                //    if (param[2].ParameterValue.ToString() == "")
                //    {
                //        //Data 1
                //        if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                //        {
                //            response2 = JsonConvert.DeserializeObject<List<MandatoryField>>(
                //                                JsonConvert.SerializeObject(dsResult.Tables[0],
                //                                        Newtonsoft.Json.Formatting.None,
                //                                        new JsonSerializerSettings
                //                                        {
                //                                            NullValueHandling = NullValueHandling.Ignore
                //                                        }));

                //            responsesClass.MandatoryField2 = response2;
                //        }

                //        //Data 2
                //        //if (param[34].ParameterValue.ToString() != "")
                //        //    throw new Exception(param[34].ParameterValue.ToString());

                //        response1.RefID = param[2].ParameterValue.ToString();
                //        response1.WarnMsg = param[15].ParameterValue.ToString();
                //        response1.WarnMsg2 = param[16].ParameterValue.ToString();
                //        response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //        response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //        response1.NIK = paramIn.UserNIK;
                //        response1.MsgGuid = paramIn.MessageGUID;
                //        response1.TrxGuid = paramIn.TransactionMessageGUID;

                //        responsesClass.ReksaMaintainAllTransaksi1 = response1;

                //        apiMsgResponse.Data = responsesClass;
                //        apiMsgResponse.IsSuccess = false;
                //    }
                //    else
                //    {
                //        response1.RefID = param[2].ParameterValue.ToString();
                //        response1.WarnMsg = param[15].ParameterValue.ToString();
                //        response1.WarnMsg2 = param[16].ParameterValue.ToString();
                //        response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //        response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //        response1.NIK = paramIn.UserNIK;
                //        response1.MsgGuid = paramIn.MessageGUID;
                //        response1.TrxGuid = paramIn.TransactionMessageGUID;
                //        responsesClass.ReksaMaintainAllTransaksi1 = response1;
                //    }
                #endregion RemarkExisting
                ReqAPIMessage.Data.nType = paramIn.Data.Type;
                ReqAPIMessage.Data.cTranType = paramIn.Data.TransactionType;
                ReqAPIMessage.Data.cRefID = paramIn.Data.ReferenceID;
                ReqAPIMessage.Data.cCIFNo = paramIn.Data.CIFNo;
                ReqAPIMessage.Data.cNoRekening = paramIn.Data.NoRekening;
                ReqAPIMessage.Data.cOfficeId = paramIn.Data.OfficeId;
                ReqAPIMessage.Data.vcXMLTrxSubscription = xmlDataSubs.OuterXml.ToString();
                ReqAPIMessage.Data.vcXMLTrxRedemption = "<DocumentElement>" + resultRedem + "</DocumentElement>";
                ReqAPIMessage.Data.vcXMLTrxRDB = xmlDataRDB.OuterXml.ToString();
                ReqAPIMessage.Data.cInputter = paramIn.Data.Inputter;
                ReqAPIMessage.Data.nSeller = paramIn.Data.Seller;
                ReqAPIMessage.Data.nWaperd = paramIn.Data.Waperd;
                ReqAPIMessage.Data.nNIK = Convert.ToInt32(paramIn.UserNIK);
                ReqAPIMessage.Data.nReferentor = paramIn.Data.Referentor;
                ReqAPIMessage.Data.cGuid = paramIn.TransactionMessageGUID;
                ReqAPIMessage.Data.bDocFCSubscriptionForm = paramIn.Data.DocFCSubscriptionForm;
                ReqAPIMessage.Data.bDocFCDevidentAuthLetter = paramIn.Data.DocFCDevidentAuthLetter;
                ReqAPIMessage.Data.bDocFCJoinAcctStatementLetter = paramIn.Data.DocFCJoinAcctStatementLetter;
                ReqAPIMessage.Data.bDocFCIDCopy = paramIn.Data.DocFCIDCopy;
                ReqAPIMessage.Data.bDocFCOthers = paramIn.Data.DocFCOthers;
                ReqAPIMessage.Data.bDocTCSubscriptionForm = paramIn.Data.DocTCSubscriptionForm;
                ReqAPIMessage.Data.bDocTCTermCondition = paramIn.Data.DocTCTermCondition;
                ReqAPIMessage.Data.bDocTCProspectus = paramIn.Data.DocTCProspectus;
                ReqAPIMessage.Data.bDocTCFundFactSheet = paramIn.Data.DocTCFundFactSheet;
                ReqAPIMessage.Data.bDocTCOthers = paramIn.Data.DocTCOthers;
                ReqAPIMessage.Data.cDocFCOthersList = paramIn.Data.DocFCOthers.ToString();
                ReqAPIMessage.Data.bIsAOANonSII = false;
                ReqAPIMessage.Data.cNoRek = paramIn.Data.NoRekening;
                //20240924, Andhika J, RFR-56218, begin
                ReqAPIMessage.Data.cNoRekCcy = paramIn.Data.Redemption[0].CCY.ToString();
                //20240924, Andhika J, RFR-56218, end
                ReqAPIMessage.MessageDateTime = DateTime.Now;
                ReqAPIMessage.MessageGUID = paramIn.MessageGUID;
                ReqAPIMessage.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ReqAPIMessage.UserNIK = paramIn.UserNIK.ToString();
                #region HitAPI
                apiMsgResponse = ReksaMaintainAllTransaksiNew(ReqAPIMessage);
                if (apiMsgResponse.IsSuccess == true)
                {
                    if (apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID == "")
                    {
                        //apiMsgResponse.ErrorDescription = ReqAPIMessage.ErrorDescription;
                        apiMsgResponse.IsSuccess = false;
                    }
                    else
                    {
                        apiMsgResponse.IsSuccess = true;
                    }
                }
                else
                {

                    throw new Exception(apiMsgResponse.ErrorDescription);
                }
                #endregion HitAPI

                if (!string.IsNullOrEmpty(apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID))

                //20230306, Andhika J, RDN-903, end
                {
                    #region Query
                    string sqlCommand = @"
                        SELECT rt.TranId, rt.TranCode, rt.ProdId, rp.ProdName
                        FROM ReksaTransaction_TT rt with(nolock)
                        join ReksaProduct_TM rp with(nolock) on rp.ProdId = rt.ProdId
                        where rt.RefID = @pcRefId 

                        INSERT ReksaRMM_TT 
                        values (@pcRefId, @cWarnMsg, @cWarnMsg2, @cWarnMsg3, @cWarnMsg4, @cFnDocID, @cObjectStore, @cDocName)

                        UPDATE ReksaTransaction_TT
                        set Channel = 'RMM'
                        where RefID = @pcRefId
                        ";
                        #endregion

                    SqlParameter[] sqlParam = new SqlParameter[8];
                    //20230306, Andhika J, RDN-903, begin
                    //sqlParam[0] = new SqlParameter("@pcRefId", response1.RefID);
                    //sqlParam[1] = new SqlParameter("@cWarnMsg", response1.WarnMsg == null ? "" : response1.WarnMsg);
                    //sqlParam[2] = new SqlParameter("@cWarnMsg2", response1.WarnMsg2 == null ? "" : response1.WarnMsg2);
                    //sqlParam[3] = new SqlParameter("@cWarnMsg3", response1.WarnMsg3 == null ? "" : response1.WarnMsg3);
                    //sqlParam[4] = new SqlParameter("@cWarnMsg4", response1.WarnMsg4 == null ? "" : response1.WarnMsg4);
                    sqlParam[0] = new SqlParameter("@pcRefId", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID);
                    sqlParam[1] = new SqlParameter("@cWarnMsg", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg);
                    sqlParam[2] = new SqlParameter("@cWarnMsg2", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg2 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg2);
                    sqlParam[3] = new SqlParameter("@cWarnMsg3", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg3 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg3);
                    sqlParam[4] = new SqlParameter("@cWarnMsg4", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg4 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg4);
                    //20230306, Andhika J, RDN-903, end
                    sqlParam[5] = new SqlParameter("@cFnDocID", paramIn.Data.FnDocID == null ? "" : paramIn.Data.FnDocID);
                    sqlParam[6] = new SqlParameter("@cObjectStore", paramIn.Data.ObjectStore == null ? "" : paramIn.Data.ObjectStore);
                    sqlParam[7] = new SqlParameter("@cDocName", paramIn.Data.DocName == null ? "" : paramIn.Data.DocName);

                    if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                        throw new Exception(errMsg);

                    if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                        throw new Exception("TranId : " + apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID + " not found !");

                    response3 = JsonConvert.DeserializeObject<List<DetailTransaction>>(
                                            JsonConvert.SerializeObject(dsResult.Tables[0],
                                                    Newtonsoft.Json.Formatting.None,
                                                    new JsonSerializerSettings
                                                        {
                                                            NullValueHandling = NullValueHandling.Ignore
                                                        }));


                    //20230306, Andhika J, RDN-903, begin
                    //responsesClass.DetailTransactions = response3;
                    apiMsgResponse.Data.DetailTransactions = response3;
                    //20230306, Andhika J, RDN-903, end
                }
                //20230306, Andhika J, RDN-903, begin
                //apiMsgResponse.Data = responsesClass;
                //20230306, Andhika J, RDN-903, end
                apiMsgResponse.IsSuccess = true;
               
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }

        public ApiMessage<ReksaMaintainAllTransaksiNewRes> saveTransactionRedempRDB(ApiMessage<ReksaMaintainAllTransaksiNewReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ApiMessage<ReksaMaintainAllTransaksiNewRes> apiMsgResponse = new ApiMessage<ReksaMaintainAllTransaksiNewRes>();
            ReksaMaintainAllTransaksiNewRes responsesClass = new ReksaMaintainAllTransaksiNewRes();
            ReksaMaintainAllTransaksiNew response1 = new ReksaMaintainAllTransaksiNew();
            List<MandatoryField> response2 = new List<MandatoryField>();
            List<DetailTransaction> response3 = new List<DetailTransaction>();
            //20230306, Andhika J, RDN-903, begin
            ApiMessage<ReksaMaintainAllTransaksiNewRq> ReqAPIMessage = new ApiMessage<ReksaMaintainAllTransaksiNewRq>();
            ReqAPIMessage.Data = new ReksaMaintainAllTransaksiNewRq();
            //20230306, Andhika J, RDN-903, end
            DateTime datenow = DateTime.Now;
            apiMsgResponse.copyHeaderForReply(paramIn);

            string clientId = (getClientIdRedempRDB(paramIn.Data.Redemption[0].ClientCode));

            if (clientId == "" || clientId == "-1")
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "1001";
                apiMsgResponse.ErrorDescription = "ClientId tidak ada / tidak berhasil di ambil";
                return apiMsgResponse;
            }

            decimal lastBalance = getLastBalace(clientId, paramIn.UserNIK, paramIn.TransactionMessageGUID);

            if (lastBalance == -1)
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "1001";
                apiMsgResponse.ErrorDescription = "Gagal mengambil last balance";
                return apiMsgResponse;
            }

            apiMsgResponse.copyHeaderForReply(paramIn);
            apiMsgResponse.MessageDateTime = DateTime.Now;
            apiMsgResponse.MessageGUID = paramIn.MessageGUID;
            apiMsgResponse.UserNIK = paramIn.UserNIK.ToString();
            apiMsgResponse.ModuleName = "Wealth.ReksaAccountTransaction";

            paramIn.Data.TransactionType = "REDEMP";
            paramIn.Data.Type = 1;
            paramIn.Data.DocFCSubscriptionForm = true;
            paramIn.Data.DocFCDevidentAuthLetter = true;
            paramIn.Data.DocFCJoinAcctStatementLetter = true;
            paramIn.Data.DocFCIDCopy = true;
            paramIn.Data.DocFCOthers = false;
            paramIn.Data.DocTCSubscriptionForm = true;
            paramIn.Data.DocTCTermCondition = true;
            paramIn.Data.DocTCProspectus = true;
            paramIn.Data.DocTCFundFactSheet = true;
            paramIn.Data.DocTCOthers = false;
            
            //membuat xml
            XmlDocument xmlDataSubs = new XmlDocument();
            XmlDocument xmlDataRedemp = new XmlDocument();
            XmlDocument xmlDataRDB = new XmlDocument();
            
            string jsonDataRedeemp = "";
            string resultRedem = "";

            if (paramIn.Data.TransactionType == "REDEMP")
            {
                int count = paramIn.Data.Redemption.Count;
                for (int i = 0; i < count; i++)
                {
                    RedemptionReq temp = paramIn.Data.Redemption[i];
                    jsonDataRedeemp = JsonConvert.SerializeObject(temp);
                    xmlDataRedemp = JsonConvert.DeserializeXmlNode(jsonDataRedeemp, "Redemption");
                    resultRedem += resultRedem + xmlDataRedemp.InnerXml.ToString();
                }
            }

            paramIn.Data.WarnMsg = "";
            paramIn.Data.WarnMsg2 = "";
            paramIn.Data.WarnMsg3 = "";
            paramIn.Data.WarnMsg4 = "";

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaMaintainAllTransaksiNew";
            //20230306, Andhika J, RDN-903, begin
            //string strSPName = "ReksaMaintainAllTransaksiNew", errMsg = "";
            string strSPName = "", errMsg = "";
            //20230306, Andhika J, RDN-903, end
            //20220425, Rendy, M32022-4, end
            DataSet dsResult = null;
            //20230306, Andhika J, RDN-903, begin
            //string errMsg = "";
            //20230306, Andhika J, RDN-903, begin

            try
            {
                //20230306, Andhika J, RDN-903, begin
                #region RemarkExisting
                //param = new List<SQLSPParameter>();
                //param.Add(new SQLSPParameter("@pnType", paramIn.Data.Type));
                //param.Add(new SQLSPParameter("@pcTranType", paramIn.Data.TransactionType, 20));
                //param.Add(new SQLSPParameter("@pcRefID", paramIn.Data.ReferenceID, 20, (SQLSPParameter.ParamDirection)ParamDirection.INPUT_OUTPUT));
                //param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 13));
                //param.Add(new SQLSPParameter("@pcOfficeId", paramIn.Data.OfficeId, 5));
                //param.Add(new SQLSPParameter("@pcNoRekening", paramIn.Data.NoRekening, 20));
                //param.Add(new SQLSPParameter("@pvcXMLTrxSubscription", xmlDataSubs.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRedemption", "<DocumentElement>" + resultRedem + "</DocumentElement>"));
                //param.Add(new SQLSPParameter("@pvcXMLTrxRDB", xmlDataRDB.OuterXml.ToString()));
                //param.Add(new SQLSPParameter("@pcInputter", paramIn.Data.Inputter, 40));
                //param.Add(new SQLSPParameter("@pnSeller", paramIn.Data.Seller));
                //param.Add(new SQLSPParameter("@pnWaperd", paramIn.Data.Waperd));
                //param.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                //param.Add(new SQLSPParameter("@pnReferentor", paramIn.Data.Referentor));
                //param.Add(new SQLSPParameter("@pcGuid", paramIn.TransactionMessageGUID, 50));
                //param.Add(new SQLSPParameter("@pcWarnMsg", paramIn.Data.WarnMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg2", paramIn.Data.WarnMsg2, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcWarnMsg3", paramIn.Data.WarnMsg3, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbDocFCSubscriptionForm", paramIn.Data.DocFCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocFCDevidentAuthLetter", paramIn.Data.DocFCDevidentAuthLetter));
                //param.Add(new SQLSPParameter("@pbDocFCJoinAcctStatementLetter", paramIn.Data.DocFCJoinAcctStatementLetter));
                //param.Add(new SQLSPParameter("@pbDocFCIDCopy", paramIn.Data.DocFCIDCopy));
                //param.Add(new SQLSPParameter("@pbDocFCOthers", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pbDocTCSubscriptionForm", paramIn.Data.DocTCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocTCTermCondition", paramIn.Data.DocTCTermCondition));
                //param.Add(new SQLSPParameter("@pbDocTCProspectus", paramIn.Data.DocTCProspectus));
                //param.Add(new SQLSPParameter("@pbDocTCFundFactSheet", paramIn.Data.DocTCFundFactSheet));
                //param.Add(new SQLSPParameter("@pbDocTCOthers", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcDocFCOthersList", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pcDocTCOthersList", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcWarnMsg4", paramIn.Data.WarnMsg4, 200, ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pbIsAOANonSII", 0));
                //param.Add(new SQLSPParameter("@pcNoRek", paramIn.Data.NoRekening, 20));
                //param.Add(new SQLSPParameter("@pcNoRekCcy", paramIn.Data.Redemption[0].CCY, 4));
                ////20220425, Rendy, M32022-4, begin
                ////param.Add(new SQLSPParameter("@pcRaiseErrorMessage", paramIn.Data.RaiseErrorMessage, 1000, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                ////20220425, Rendy, M32022-4, end

                //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                //{
                //    //if (param[2].ParameterValue.ToString() == "" || param[34].ParameterValue.ToString() != "")
                //    if (param[2].ParameterValue.ToString() == "")
                //    {
                //        //Data 1
                //        if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                //        {
                //            response2 = JsonConvert.DeserializeObject<List<MandatoryField>>(
                //                                JsonConvert.SerializeObject(dsResult.Tables[0],
                //                                        Newtonsoft.Json.Formatting.None,
                //                                        new JsonSerializerSettings
                //                                        {
                //                                            NullValueHandling = NullValueHandling.Ignore
                //                                        }));

                //            responsesClass.MandatoryField2 = response2;
                //        }

                //        //Data 2
                //        //if (param[34].ParameterValue.ToString() != "")
                //        //    throw new Exception(param[34].ParameterValue.ToString());

                //        response1.RefID = param[2].ParameterValue.ToString();
                //        response1.WarnMsg = param[15].ParameterValue.ToString();
                //        response1.WarnMsg2 = param[16].ParameterValue.ToString();
                //        response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //        response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //        response1.NIK = paramIn.UserNIK;
                //        response1.MsgGuid = paramIn.MessageGUID;
                //        response1.TrxGuid = paramIn.TransactionMessageGUID;

                //        responsesClass.ReksaMaintainAllTransaksi1 = response1;

                //        apiMsgResponse.Data = responsesClass;
                //        apiMsgResponse.IsSuccess = false;
                //    }
                //    else
                //    {
                //        response1.RefID = param[2].ParameterValue.ToString();
                //        response1.WarnMsg = param[15].ParameterValue.ToString();
                //        response1.WarnMsg2 = param[16].ParameterValue.ToString();
                //        response1.WarnMsg3 = param[17].ParameterValue.ToString();
                //        response1.WarnMsg4 = param[30].ParameterValue.ToString();
                //        response1.NIK = paramIn.UserNIK;
                //        response1.MsgGuid = paramIn.MessageGUID;
                //        response1.TrxGuid = paramIn.TransactionMessageGUID;
                //        responsesClass.ReksaMaintainAllTransaksi1 = response1;
                //    }
                #endregion RemarkExisting
                ReqAPIMessage.Data.nType = paramIn.Data.Type;
                ReqAPIMessage.Data.cTranType = paramIn.Data.TransactionType;
                ReqAPIMessage.Data.cRefID = paramIn.Data.ReferenceID;
                ReqAPIMessage.Data.cCIFNo = paramIn.Data.CIFNo;
                ReqAPIMessage.Data.cNoRekening = paramIn.Data.NoRekening;
                ReqAPIMessage.Data.cOfficeId = paramIn.Data.OfficeId;
                ReqAPIMessage.Data.vcXMLTrxSubscription =xmlDataSubs.OuterXml.ToString();
                ReqAPIMessage.Data.vcXMLTrxRedemption = "<DocumentElement>" + resultRedem + "</DocumentElement>";
                ReqAPIMessage.Data.vcXMLTrxRDB = xmlDataRDB.OuterXml.ToString();
                ReqAPIMessage.Data.cInputter = paramIn.Data.Inputter;
                ReqAPIMessage.Data.nSeller = paramIn.Data.Seller;
                ReqAPIMessage.Data.nWaperd = paramIn.Data.Waperd;
                ReqAPIMessage.Data.nNIK = Convert.ToInt32(paramIn.UserNIK);
                ReqAPIMessage.Data.nReferentor = paramIn.Data.Referentor;
                ReqAPIMessage.Data.cGuid = paramIn.TransactionMessageGUID;
                ReqAPIMessage.Data.bDocFCSubscriptionForm = paramIn.Data.DocFCSubscriptionForm;
                ReqAPIMessage.Data.bDocFCDevidentAuthLetter = paramIn.Data.DocFCDevidentAuthLetter;
                ReqAPIMessage.Data.bDocFCJoinAcctStatementLetter = paramIn.Data.DocFCJoinAcctStatementLetter;
                ReqAPIMessage.Data.bDocFCIDCopy = paramIn.Data.DocFCIDCopy;
                ReqAPIMessage.Data.bDocFCOthers = paramIn.Data.DocFCOthers;
                ReqAPIMessage.Data.bDocTCSubscriptionForm = paramIn.Data.DocTCSubscriptionForm;
                ReqAPIMessage.Data.bDocTCTermCondition = paramIn.Data.DocTCTermCondition;
                ReqAPIMessage.Data.bDocTCProspectus = paramIn.Data.DocTCProspectus;
                ReqAPIMessage.Data.bDocTCFundFactSheet = paramIn.Data.DocTCFundFactSheet;
                ReqAPIMessage.Data.bDocTCOthers = paramIn.Data.DocTCOthers;
                ReqAPIMessage.Data.cDocFCOthersList = paramIn.Data.DocFCOthers.ToString();
                ReqAPIMessage.Data.bIsAOANonSII = false;
                ReqAPIMessage.Data.cNoRek = paramIn.Data.NoRekening;
                ReqAPIMessage.MessageDateTime = DateTime.Now;
                ReqAPIMessage.MessageGUID = paramIn.MessageGUID;
                ReqAPIMessage.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ReqAPIMessage.UserNIK = paramIn.UserNIK.ToString();
                #region HitAPI
                apiMsgResponse = ReksaMaintainAllTransaksiNew(ReqAPIMessage);
                if (apiMsgResponse.IsSuccess == true)
                {
                    if (apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID == "")
                    {
                        //apiMsgResponse.ErrorDescription = ReqAPIMessage.ErrorDescription;
                        apiMsgResponse.IsSuccess = false;
                    }
                    else
                    {
                        apiMsgResponse.IsSuccess = true;
                    }

                }
                else
                {

                    throw new Exception(apiMsgResponse.ErrorDescription);
                }
                #endregion HitAPI

                if (!string.IsNullOrEmpty(apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID))
                //20230306, Andhika J, RDN-903, end
                {
                    #region Query
                    string sqlCommand = @"
                        SELECT rt.TranId, rt.TranCode, rt.ProdId, rp.ProdName
                        FROM ReksaTransaction_TT rt with(nolock)
                        join ReksaProduct_TM rp with(nolock) on rp.ProdId = rt.ProdId
                        where rt.RefID = @pcRefId 

                        INSERT ReksaRMM_TT 
                        values (@pcRefId, @cWarnMsg, @cWarnMsg2, @cWarnMsg3, @cWarnMsg4, @cFnDocID, @cObjectStore, @cDocName)

                        UPDATE ReksaTransaction_TT
                        set Channel = 'RMM'
                        where RefID = @pcRefId
                        ";
                        #endregion

                        SqlParameter[] sqlParam = new SqlParameter[8];
                    //20230306, Andhika J, RDN-903, begin
                    //sqlParam[0] = new SqlParameter("@pcRefId", response1.RefID);
                    //sqlParam[1] = new SqlParameter("@cWarnMsg", response1.WarnMsg == null ? "": response1.WarnMsg);
                    //sqlParam[2] = new SqlParameter("@cWarnMsg2", response1.WarnMsg2 == null ? "" : response1.WarnMsg);
                    //sqlParam[3] = new SqlParameter("@cWarnMsg3", response1.WarnMsg3 == null ? "" : response1.WarnMsg);
                    //sqlParam[4] = new SqlParameter("@cWarnMsg4", response1.WarnMsg4 == null ? "" : response1.WarnMsg);
                    sqlParam[0] = new SqlParameter("@pcRefId", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.RefID);
                    sqlParam[1] = new SqlParameter("@cWarnMsg", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg);
                    sqlParam[2] = new SqlParameter("@cWarnMsg2", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg2 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg);
                    sqlParam[3] = new SqlParameter("@cWarnMsg3", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg3 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg);
                    sqlParam[4] = new SqlParameter("@cWarnMsg4", apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg4 == null ? "" : apiMsgResponse.Data.ReksaMaintainAllTransaksi1.WarnMsg);
                    //20230306, Andhika J, RDN-903, end
                    sqlParam[5] = new SqlParameter("@cFnDocID", paramIn.Data.FnDocID == null ? "" : paramIn.Data.FnDocID);
                        sqlParam[6] = new SqlParameter("@cObjectStore", paramIn.Data.ObjectStore == null ? "" : paramIn.Data.ObjectStore);
                        sqlParam[7] = new SqlParameter("@cDocName", paramIn.Data.DocName == null ? "" : paramIn.Data.DocName);

                        if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                            throw new Exception(errMsg);

                        if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                            throw new Exception("TranId : " + response1.RefID + " not found !");

                        response3 = JsonConvert.DeserializeObject<List<DetailTransaction>>(
                                                JsonConvert.SerializeObject(dsResult.Tables[0],
                                                        Newtonsoft.Json.Formatting.None,
                                                        new JsonSerializerSettings
                                                        {
                                                            NullValueHandling = NullValueHandling.Ignore
                                                        }));
                    
                    //20230306, Andhika J, RDN-903, begin
                    //responsesClass.DetailTransactions = response3;
                    apiMsgResponse.Data.DetailTransactions = response3;
                    //20230306, Andhika J, RDN-903, end
                }
                //20230306, Andhika J, RDN-903, begin
                //apiMsgResponse.Data = responsesClass;
                //20230306, Andhika J, RDN-903, end
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }

        #endregion Redemp

        #region Switch

        #region Lump Sum
        //20201019, Dennis, RDN2-5, begin
        public ApiMessage<ReksaMaintainSwitchingRes> SaveTransactionSwitching(ApiMessage<ReksaMaintainSwitchingReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ReksaMaintainSwitchingRes response = new ReksaMaintainSwitchingRes();
            ApiMessage<ReksaMaintainSwitchingRes> apiMsgResponse = new ApiMessage<ReksaMaintainSwitchingRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);
            //20230306, Andhika J, RDN-903, begin
            ApiMessage<ReksaMaintainSwitchingRq> ReqAPIMessage = new ApiMessage<ReksaMaintainSwitchingRq>();
            ReqAPIMessage.Data = new ReksaMaintainSwitchingRq();
            ApiMessage<ReksaMaintainSwitchingRs> ResAPIMessage = new ApiMessage<ReksaMaintainSwitchingRs>();
            ResAPIMessage.Data = new ReksaMaintainSwitchingRs();
            apiMsgResponse.Data = new ReksaMaintainSwitchingRes();
            //20230306, Andhika J, RDN-903, end
            DateTime datenow = DateTime.Now;
            //20230729, ahmad.fansyuri, RDN-1017, begin
            int IsRDB;
            bool bIsRDB;
            string sClientCode = "";
            //20230729, ahmad.fansyuri, RDN-1017, end

            #region validasi
            //Validasi 
            //NIK
            if (paramIn.UserNIK.ToString() == "")
            {
                apiMsgResponse.IsSuccess = false;

            }
            //GUID
            if (paramIn.MessageGUID == "")
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "Guid tidak boleh kosong";
                return apiMsgResponse;
            }

            //OfficeId
            if (paramIn.Data.OfficeId == "")
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "Office Id tidak boleh kosong";
                return apiMsgResponse;
            }

            else
            {
                if (ValidasiOffice(paramIn.Data.OfficeId) != "1")
                {
                    apiMsgResponse.IsSuccess = false;
                    apiMsgResponse.ErrorDescription = "Office Id tidak terdaftar";
                    return apiMsgResponse;
                }
            }

            //ProdIdSwcIn
            if (paramIn.Data.ProdIdSwcIn.ToString() == "0" || paramIn.Data.ProdIdSwcIn.ToString() == null)
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "ProdIdSwcIn tidak boleh kosong";
                return apiMsgResponse;
            }
            //

            //ProdIdSwcOut
            if (paramIn.Data.ProdIdSwcOut.ToString() == "0" || paramIn.Data.ProdIdSwcOut.ToString() == null)
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "ProdIdSwcOut tidak boleh kosong";
                return apiMsgResponse;
            }
            //

            //TranUnit
            if (paramIn.Data.TranUnit.ToString() == "0" || paramIn.Data.TranUnit.ToString() == null)
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "TranUnit tidak boleh kosong";
                return apiMsgResponse;
            }
            //

            #endregion validasi

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaMaintainSwitching", errMsg = "";
            //20230306, Andhika J, RDN-903, begin
            //string strSPName = "ReksaMaintainSwitching", errMsg = "";
            string strSPName = "", errMsg = "";
            //20230306, Andhika J, RDN-903, end
            //20220425, Rendy, M32022-4, end
            DataSet dsResult = new DataSet();
            DataSet dsParamOut = new DataSet();

            paramIn.Data.DocFCSubscriptionForm = true;
            paramIn.Data.DocFCDevidentAuthLetter = true;
            paramIn.Data.DocFCJoinAcctStatementLetter = true;
            paramIn.Data.DocFCIDCopy = true;
            paramIn.Data.DocFCOthers = false;
            paramIn.Data.DocTCSubscriptionForm = true;
            paramIn.Data.DocTCTermCondition = true;
            paramIn.Data.DocTCProspectus = true;
            paramIn.Data.DocTCFundFactSheet = true;
            paramIn.Data.DocTCOthers = false;
            paramIn.Data.DocFCOthersList = "";
            paramIn.Data.DocTCOthersList = "";

            paramIn.Data.WarnMsg = "";
            paramIn.Data.WarnMsg2 = "";
            paramIn.Data.WarnMsg3 = "";
            paramIn.Data.WarnMsg4 = "";
            paramIn.Data.WarnMsg5 = "";
            // paramIn.Data.IsFeeEdit = false;
            paramIn.Data.RefID = "";
            paramIn.Data.TranCode = "        ";
            //20250312, gio, RDN-1229, begin
            //paramIn.Data.TranDate = datenow;
            //20250312, gio, RDN-1229, end
            //20250925, Andhika J, RDN-1264, begin
            if (!paramIn.Data.IsNew)
            {
                paramIn.Data.IsNew = ReksaCheckSubsTypeSwitching(paramIn).Data.isSubsNew;
            }
            //20250925, Andhika J, RDN-1264, end

            try
            {

                //20230306, Andhika J, RDN-903, begin
                #region RemarkExisting
                //param = new List<SQLSPParameter>();
                //param.Add(new SQLSPParameter("@pnType", paramIn.Data.Type));
                //param.Add(new SQLSPParameter("@pnTranType", paramIn.Data.TranType));
                //param.Add(new SQLSPParameter("@pcTranCode", paramIn.Data.TranCode));
                //param.Add(new SQLSPParameter("@pnTranId", paramIn.Data.TranId));
                //param.Add(new SQLSPParameter("@pdTranDate", paramIn.Data.TranDate.ToString("yyyy-MM-dd HH:mm:ss")));
                //param.Add(new SQLSPParameter("@pnProdIdSwcOut", paramIn.Data.ProdIdSwcOut));
                //param.Add(new SQLSPParameter("@pnProdIdSwcIn", paramIn.Data.ProdIdSwcIn));
                //param.Add(new SQLSPParameter("@pnClientIdSwcOut", paramIn.Data.ClientIdSwcOut));
                //param.Add(new SQLSPParameter("@pnClientIdSwcIn", paramIn.Data.ClientIdSwcIn));
                //param.Add(new SQLSPParameter("@pnFundIdSwcOut", paramIn.Data.FundIdSwcOut));
                //param.Add(new SQLSPParameter("@pnFundIdSwcIn", paramIn.Data.FundIdSwcIn));
                //param.Add(new SQLSPParameter("@pcSelectedAccNo", paramIn.Data.SelectedAccNo));
                //param.Add(new SQLSPParameter("@pnAgentIdSwcOut", paramIn.Data.AgentIdSwcOut));
                //param.Add(new SQLSPParameter("@pnAgentIdSwcIn", paramIn.Data.AgentIdSwcIn));
                //param.Add(new SQLSPParameter("@pcTranCCY", paramIn.Data.TranCCY));
                //param.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.TranAmt));
                //param.Add(new SQLSPParameter("@pmTranUnit", paramIn.Data.TranUnit));
                //param.Add(new SQLSPParameter("@pmSwitchingFee", paramIn.Data.SwitchingFee));
                //param.Add(new SQLSPParameter("@pmNAVSwcOut", paramIn.Data.NAVSwcOut));
                //param.Add(new SQLSPParameter("@pmNAVSwcIn", paramIn.Data.NAVSwcIn));
                //param.Add(new SQLSPParameter("@pdNAVValueDate", paramIn.Data.NAVValueDate.ToString("yyyy-MM-dd")));
                //param.Add(new SQLSPParameter("@pmUnitBalanceSwcOut", paramIn.Data.UnitBalanceSwcOut));
                //param.Add(new SQLSPParameter("@pmUnitBalanceNomSwcOut", paramIn.Data.UnitBalanceNomSwcOut));
                //param.Add(new SQLSPParameter("@pmUnitBalanceSwcIn", paramIn.Data.UnitBalanceSwcIn));
                //param.Add(new SQLSPParameter("@pmUnitBalanceNomSwcIn", paramIn.Data.UnitBalanceNomSwcIn));
                //param.Add(new SQLSPParameter("@pnUserSuid", paramIn.Data.UserSuid));
                //param.Add(new SQLSPParameter("@pbByUnit", paramIn.Data.ByUnit));
                //param.Add(new SQLSPParameter("@pnSalesId", paramIn.Data.SalesId));
                //param.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID));
                //param.Add(new SQLSPParameter("@pcWarnMsg", paramIn.Data.WarnMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));
                //param.Add(new SQLSPParameter("@pcInputter", paramIn.Data.Inputter));
                //param.Add(new SQLSPParameter("@pnSeller", paramIn.Data.Seller));
                //param.Add(new SQLSPParameter("@pnWaperd", paramIn.Data.Waperd));
                //param.Add(new SQLSPParameter("@pbIsFeeEdit", paramIn.Data.IsFeeEdit));
                //param.Add(new SQLSPParameter("@pbDocFCSubscriptionForm", paramIn.Data.DocFCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocFCDevidentAuthLetter", paramIn.Data.DocFCDevidentAuthLetter));
                //param.Add(new SQLSPParameter("@pbDocFCJoinAcctStatementLetter", paramIn.Data.DocFCJoinAcctStatementLetter));
                //param.Add(new SQLSPParameter("@pbDocFCIDCopy", paramIn.Data.DocFCIDCopy));
                //param.Add(new SQLSPParameter("@pbDocFCOthers", paramIn.Data.DocFCOthers));
                //param.Add(new SQLSPParameter("@pbDocTCSubscriptionForm", paramIn.Data.DocTCSubscriptionForm));
                //param.Add(new SQLSPParameter("@pbDocTCTermCondition", paramIn.Data.DocTCTermCondition));
                //param.Add(new SQLSPParameter("@pbDocTCProspectus", paramIn.Data.DocTCProspectus));
                //param.Add(new SQLSPParameter("@pbDocTCFundFactSheet", paramIn.Data.DocTCFundFactSheet));
                //param.Add(new SQLSPParameter("@pbDocTCOthers", paramIn.Data.DocTCOthers));
                //param.Add(new SQLSPParameter("@pcDocFCOthersList", paramIn.Data.DocFCOthersList));
                //param.Add(new SQLSPParameter("@pcDocTCOthersList", paramIn.Data.DocTCOthersList));
                //param.Add(new SQLSPParameter("@pcWarnMsg2", paramIn.Data.WarnMsg2, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//46
                //param.Add(new SQLSPParameter("@pdPercentageFee", paramIn.Data.PercentageFee));
                //param.Add(new SQLSPParameter("@pbByPhoneOrder", paramIn.Data.ByPhoneOrder));
                //param.Add(new SQLSPParameter("@pcWarnMsg3", paramIn.Data.WarnMsg3, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//49
                //param.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
                //param.Add(new SQLSPParameter("@pcOfficeId", paramIn.Data.OfficeId));
                //param.Add(new SQLSPParameter("@pcRefID", paramIn.Data.RefID, ParamDirection.INPUT_OUTPUT));//52
                //param.Add(new SQLSPParameter("@pbIsNew", paramIn.Data.IsNew));
                //param.Add(new SQLSPParameter("@pcClientCodeSwitchInNew", paramIn.Data.ClientCodeSwitchInNew));
                //param.Add(new SQLSPParameter("@pnReferentor", paramIn.Data.Referentor));
                //param.Add(new SQLSPParameter("@pcWarnMsg4", paramIn.Data.WarnMsg4, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//56
                //param.Add(new SQLSPParameter("@pcWarnMsg5", paramIn.Data.WarnMsg5, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//57
                //param.Add(new SQLSPParameter("@pbTrxTaxAmnesty", paramIn.Data.TrxTaxAmnesty));
                //param.Add(new SQLSPParameter("@pcNoRek", paramIn.Data.SelectedAccNo));
                //param.Add(new SQLSPParameter("@pcNoRekCcy", paramIn.Data.TranCCY));
                ////20220425, Rendy, M32022-4, begin
                ////param.Add(new SQLSPParameter("@pnErrorSP", "", 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//59
                ////20220425, Rendy, M32022-4, end

                //if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                //    throw new Exception(errMsg);

                //if (param[61].ParameterValue.ToString() != "")
                //    throw new Exception(param[61].ParameterValue.ToString());

                //response.RefID = param[52].ParameterValue.ToString();
                //response.WarnMsg = param[29].ParameterValue.ToString();
                //response.WarnMsg2 = param[46].ParameterValue.ToString();
                //response.WarnMsg3 = param[49].ParameterValue.ToString();
                //response.WarnMsg4 = param[56].ParameterValue.ToString();
                //response.WarnMsg4 = param[57].ParameterValue.ToString();
                #endregion

                ReqAPIMessage.UserNIK = paramIn.UserNIK;
                ReqAPIMessage.MessageGUID = paramIn.MessageGUID;
                ReqAPIMessage.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ReqAPIMessage.Data.pnType = paramIn.Data.Type;
                ReqAPIMessage.Data.pnTranType = paramIn.Data.TranType;
                ReqAPIMessage.Data.pcTranCode = paramIn.Data.TranCode;
                //paramIn.Data.TranId; // output
                ReqAPIMessage.Data.pdTranDate = paramIn.Data.TranDate.ToString("yyyy-MM-dd HH:mm:ss");
                ReqAPIMessage.Data.pnProdIdSwcOut = paramIn.Data.ProdIdSwcOut;
                ReqAPIMessage.Data.pnProdIdSwcIn = paramIn.Data.ProdIdSwcIn;
                ReqAPIMessage.Data.pnClientIdSwcOut = paramIn.Data.ClientIdSwcOut;
                ReqAPIMessage.Data.pnClientIdSwcIn = paramIn.Data.ClientIdSwcIn;

                ReqAPIMessage.Data.pnFundIdSwcOut = paramIn.Data.FundIdSwcOut;
                ReqAPIMessage.Data.pnFundIdSwcIn = paramIn.Data.FundIdSwcIn;
                ReqAPIMessage.Data.pcSelectedAccNo = paramIn.Data.SelectedAccNo;
                ReqAPIMessage.Data.pnAgentIdSwcOut = paramIn.Data.AgentIdSwcOut;
                ReqAPIMessage.Data.pnAgentIdSwcIn = paramIn.Data.AgentIdSwcIn;
                ReqAPIMessage.Data.pcTranCCY = paramIn.Data.TranCCY;
                ReqAPIMessage.Data.pmTranAmt = paramIn.Data.TranAmt;
                ReqAPIMessage.Data.pmTranUnit = paramIn.Data.TranUnit;
                ReqAPIMessage.Data.pmSwitchingFee = paramIn.Data.SwitchingFee;
                ReqAPIMessage.Data.pmNAVSwcOut = paramIn.Data.NAVSwcOut;
                ReqAPIMessage.Data.pmNAVSwcIn = paramIn.Data.NAVSwcIn;
                ReqAPIMessage.Data.pdNAVValueDate  = paramIn.Data.NAVValueDate.ToString("yyyy-MM-dd");
                ReqAPIMessage.Data.pmUnitBalanceSwcOut = paramIn.Data.UnitBalanceSwcOut;
                ReqAPIMessage.Data.pmUnitBalanceNomSwcOut = paramIn.Data.UnitBalanceNomSwcOut;
                ReqAPIMessage.Data.pmUnitBalanceSwcIn = paramIn.Data.UnitBalanceSwcIn;
                ReqAPIMessage.Data.pmUnitBalanceNomSwcIn = paramIn.Data.UnitBalanceNomSwcIn;
                ReqAPIMessage.Data.pnUserSuid = paramIn.Data.UserSuid;
                ReqAPIMessage.Data.pbByUnit = Convert.ToInt32(paramIn.Data.ByUnit);
                ReqAPIMessage.Data.pnSalesId = paramIn.Data.SalesId;
                ReqAPIMessage.Data.pcGuid  = paramIn.MessageGUID;
                //ReqAPIMessage.Data.  = paramIn.Data.WarnMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT;
                ReqAPIMessage.Data.pcInputter = paramIn.Data.Inputter;
                ReqAPIMessage.Data.pnSeller = paramIn.Data.Seller;
                ReqAPIMessage.Data.pnWaperd = paramIn.Data.Waperd;
                ReqAPIMessage.Data.pbIsFeeEdit = Convert.ToInt32(paramIn.Data.IsFeeEdit);
                ReqAPIMessage.Data.pbDocFCSubscriptionForm = Convert.ToInt32(paramIn.Data.DocFCSubscriptionForm);
                ReqAPIMessage.Data.pbDocFCDevidentAuthLetter = Convert.ToInt32(paramIn.Data.DocFCDevidentAuthLetter);
                ReqAPIMessage.Data.pbDocFCJoinAcctStatementLetter = Convert.ToInt32(paramIn.Data.DocFCJoinAcctStatementLetter);
                ReqAPIMessage.Data.pbDocFCIDCopy = Convert.ToInt32(paramIn.Data.DocFCIDCopy);
                ReqAPIMessage.Data.pbDocFCOthers = Convert.ToInt32(paramIn.Data.DocFCOthers);
                ReqAPIMessage.Data.pbDocTCSubscriptionForm = Convert.ToInt32(paramIn.Data.DocTCSubscriptionForm);
                ReqAPIMessage.Data.pbDocTCTermCondition = Convert.ToInt32(paramIn.Data.DocTCTermCondition);
                ReqAPIMessage.Data.pbDocTCProspectus = Convert.ToInt32(paramIn.Data.DocTCProspectus);
                ReqAPIMessage.Data.pbDocTCFundFactSheet = Convert.ToInt32(paramIn.Data.DocTCFundFactSheet);
                ReqAPIMessage.Data.pbDocTCOthers = Convert.ToInt32(paramIn.Data.DocTCOthers);
                ReqAPIMessage.Data.pcDocFCOthersList = paramIn.Data.DocFCOthersList;
                ReqAPIMessage.Data.pcDocTCOthersList = paramIn.Data.DocTCOthersList;
                //ReqAPIMessage.Data.  = paramIn.Data.WarnMsg2, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT;//46
                ReqAPIMessage.Data.pdPercentageFee  = paramIn.Data.PercentageFee;
                ReqAPIMessage.Data.pbByPhoneOrder = Convert.ToInt32(paramIn.Data.ByPhoneOrder);
                //ReqAPIMessage.Data.  = paramIn.Data.WarnMsg3, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT;//49
                ReqAPIMessage.Data.pcCIFNo = paramIn.Data.CIFNo;
                ReqAPIMessage.Data.pcOfficeId = paramIn.Data.OfficeId;
                //ReqAPIMessage.Data.  = paramIn.Data.RefID, ParamDirection.INPUT_OUTPUT;//52
                ReqAPIMessage.Data.pbIsNew  = Convert.ToInt32(paramIn.Data.IsNew);
                ReqAPIMessage.Data.pcClientCodeSwitchInNew = paramIn.Data.ClientCodeSwitchInNew;
                ReqAPIMessage.Data.pnReferentor  = paramIn.Data.Referentor;
                //ReqAPIMessage.Data.  = paramIn.Data.WarnMsg4, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT;//56
                //ReqAPIMessage.Data.  = paramIn.Data.WarnMsg5, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT;//57
                ReqAPIMessage.Data.pbTrxTaxAmnesty = Convert.ToInt32(paramIn.Data.TrxTaxAmnesty);
                ReqAPIMessage.Data.pcNoRek = paramIn.Data.SelectedAccNo;
                ReqAPIMessage.Data.pcNoRekCcy = paramIn.Data.TranCCY;

                //20230801, ahmad.fansyuri, RDN-1017, begin

                ReqAPIMessage.Data.pnTranId = 0;
                ReqAPIMessage.Data.pcRefID = "";
                ReqAPIMessage.Data.pnJangkaWaktu = 0;
                ReqAPIMessage.Data.pdJatuhTempo = "";
                ReqAPIMessage.Data.pnAutoRedemption = 0;
                ReqAPIMessage.Data.pnFrekuensiPendebetan = 0;
                ReqAPIMessage.Data.pnAsuransi = 0;

                ReqAPIMessage.Data.pcWarnMsg = paramIn.Data.WarnMsg;
                ReqAPIMessage.Data.pcWarnMsg2 = paramIn.Data.WarnMsg2;
                ReqAPIMessage.Data.pcWarnMsg3 = paramIn.Data.WarnMsg3;
                ReqAPIMessage.Data.pcWarnMsg4 = paramIn.Data.WarnMsg4;
                ReqAPIMessage.Data.pcWarnMsg5 = paramIn.Data.WarnMsg5;

                if (!ClientCode(paramIn.Data.ClientIdSwcOut, out sClientCode, out bIsRDB, out errMsg))
                    throw new Exception(errMsg);

               
                #region ReksaMaintainSwitchingRDB
                if (bIsRDB)
                {
                    ApiMessage<ValidasiListDetailRDBRes> ValidasiListDetailRDB = ReksaGetListClientRDBByCode(sClientCode);
                    if (!ValidasiListDetailRDB.IsSuccess)
                        throw new Exception(ValidasiListDetailRDB.ErrorDescription);

                    //ReqAPIMessage.Data.pnTranId = Convert.ToInt32(dsResult.Tables[0].Rows[0]["TranId"].ToString());
                    ReqAPIMessage.Data.pnTranType = 9;
                            ReqAPIMessage.Data.pcRefID = "";
                            ReqAPIMessage.Data.pnJangkaWaktu = ValidasiListDetailRDB.Data.SisaJangkaWaktu;
                            ReqAPIMessage.Data.pdJatuhTempo = ValidasiListDetailRDB.Data.JatuhTempo.ToString("yyyyMMdd");
                            ReqAPIMessage.Data.pnAutoRedemption = Convert.ToInt32(ValidasiListDetailRDB.Data.AutoRedemption);
                            ReqAPIMessage.Data.pnFrekuensiPendebetan = Convert.ToInt32(ValidasiListDetailRDB.Data.FrekPendebetan);
                            ReqAPIMessage.Data.pnAsuransi = Convert.ToInt32(ValidasiListDetailRDB.Data.Asuransi);


                    		ResAPIMessage = ReksaMaintainSwitchingRDB(ReqAPIMessage);
                            if (ResAPIMessage.IsSuccess == true)
                            {
                                if (ResAPIMessage.Data.pcRefID == "")
                                {
                                    apiMsgResponse.IsSuccess = false;
                                }
                                else
                                {
                                    apiMsgResponse.IsSuccess = true;
                                }
                                apiMsgResponse.Data.RefID = ResAPIMessage.Data.pcRefID;
                                apiMsgResponse.Data.WarnMsg = ResAPIMessage.Data.pcWarnMsg;
                                apiMsgResponse.Data.WarnMsg2 = ResAPIMessage.Data.pcWarnMsg2;
                                apiMsgResponse.Data.WarnMsg3 = ResAPIMessage.Data.pcWarnMsg3;
                                apiMsgResponse.Data.WarnMsg4 = ResAPIMessage.Data.pcWarnMsg4;
                            }
                            else
                            {
                                throw new Exception(ResAPIMessage.ErrorDescription);
                            }

                            if (!string.IsNullOrEmpty(apiMsgResponse.Data.RefID))
                            {
                                #region Query
                                string sqlCommand = @"
                                    SELECT TranId, TranCode, rp.ProdName as 'ProdSwitchOutName', rp2.ProdName as 'ProdSwitchInName'
                                    FROM ReksaSwitchingTransaction_TM rst with(nolock) 
                                    join ReksaProduct_TM rp with(nolock) on rp.ProdId = rst.ProdSwitchOut 
                                    join ReksaProduct_TM rp2 with(nolock) on rp2.ProdId = rst.ProdSwitchIn
                                    where RefID = @pcRefId

                                INSERT ReksaRMM_TT 
                                    values (@pcRefId, @cWarnMsg, @cWarnMsg2, @cWarnMsg3, @cWarnMsg4, @cFnDocID, @cObjectStore, @cDocName)

                                UPDATE ReksaSwitchingTransaction_TM
                                SET Channel = 'RMM'
                                WHERE RefID = @pcRefId
                                AND Status = 0
                                ";
                                #endregion

                                SqlParameter[] sqlParam = new SqlParameter[8];
                                sqlParam[0] = new SqlParameter("@pcRefId", apiMsgResponse.Data.RefID);
                                sqlParam[1] = new SqlParameter("@cWarnMsg", apiMsgResponse.Data.WarnMsg == null ? "" : apiMsgResponse.Data.WarnMsg);
                                sqlParam[2] = new SqlParameter("@cWarnMsg2", apiMsgResponse.Data.WarnMsg2 == null ? "" : apiMsgResponse.Data.WarnMsg2);
                                sqlParam[3] = new SqlParameter("@cWarnMsg3", apiMsgResponse.Data.WarnMsg3 == null ? "" : apiMsgResponse.Data.WarnMsg3);
                                sqlParam[4] = new SqlParameter("@cWarnMsg4", apiMsgResponse.Data.WarnMsg4 == null ? "" : apiMsgResponse.Data.WarnMsg4);
                                sqlParam[5] = new SqlParameter("@cFnDocID", paramIn.Data.FnDocID == null ? "" : paramIn.Data.FnDocID);
                                sqlParam[6] = new SqlParameter("@cObjectStore", paramIn.Data.ObjectStore == null ? "" : paramIn.Data.ObjectStore);
                                sqlParam[7] = new SqlParameter("@cDocName", paramIn.Data.DocName == null ? "" : paramIn.Data.DocName);

                                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                                    throw new Exception(errMsg);

                                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                                    throw new Exception("TranId : " + apiMsgResponse.Data.RefID + " not found !");

                                apiMsgResponse.Data.detailTransactions = JsonConvert.DeserializeObject<List<DetailTransactionSwitching>>(
                                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                                Newtonsoft.Json.Formatting.None,
                                                                new JsonSerializerSettings
                                                                {
                                                                    NullValueHandling = NullValueHandling.Ignore
                                                                }));
                            }

                            apiMsgResponse.IsSuccess = true;
                        }
                        #endregion ReksaMaintainSwitchingRDB

                        #region ReksaMaintainSwitching
                        else
                        {
                            ResAPIMessage = ReksaMaintainSwitching(ReqAPIMessage);
                            if (ResAPIMessage.IsSuccess == true)
                            {
                                if (ResAPIMessage.Data.pcRefID == "")
                                {
                                    apiMsgResponse.IsSuccess = false;
                                }
                                else
                                {
                                    apiMsgResponse.IsSuccess = true;

                                }
                                apiMsgResponse.Data.RefID = ResAPIMessage.Data.pcRefID;
                                apiMsgResponse.Data.WarnMsg = ResAPIMessage.Data.pcWarnMsg;
                                apiMsgResponse.Data.WarnMsg2 = ResAPIMessage.Data.pcWarnMsg2;
                                apiMsgResponse.Data.WarnMsg3 = ResAPIMessage.Data.pcWarnMsg3;
                                apiMsgResponse.Data.WarnMsg4 = ResAPIMessage.Data.pcWarnMsg4;

                }
                else
                {

                    throw new Exception(ResAPIMessage.ErrorDescription);
                }

                //20230306, Andhika J, RDN-903, end
                if (!string.IsNullOrEmpty(apiMsgResponse.Data.RefID))
                {
                    #region Query
                    string sqlCommand = @"
                        SELECT TranId, TranCode, rp.ProdName as 'ProdSwitchOutName', rp2.ProdName as 'ProdSwitchInName'
                        FROM ReksaSwitchingTransaction_TM rst with(nolock) 
                        join ReksaProduct_TM rp with(nolock) on rp.ProdId = rst.ProdSwitchOut 
                        join ReksaProduct_TM rp2 with(nolock) on rp2.ProdId = rst.ProdSwitchIn
                        where RefID = @pcRefId

                    INSERT ReksaRMM_TT 
                        values (@pcRefId, @cWarnMsg, @cWarnMsg2, @cWarnMsg3, @cWarnMsg4, @cFnDocID, @cObjectStore, @cDocName)

                    UPDATE ReksaSwitchingTransaction_TM
                    SET Channel = 'RMM'
                    WHERE RefID = @pcRefId
                    AND Status = 0
                    ";
                    #endregion

                    SqlParameter[] sqlParam = new SqlParameter[8];
                    //20230306, Andhika J, RDN-903, begin
                    //sqlParam[0] = new SqlParameter("@pcRefId", response.RefID);
                    //sqlParam[1] = new SqlParameter("@cWarnMsg", response.WarnMsg == null ? "" : response.WarnMsg);
                    //sqlParam[2] = new SqlParameter("@cWarnMsg2", response.WarnMsg2 == null ? "" : response.WarnMsg2);
                    //sqlParam[3] = new SqlParameter("@cWarnMsg3", response.WarnMsg3 == null ? "" : response.WarnMsg3);
                    //sqlParam[4] = new SqlParameter("@cWarnMsg4", response.WarnMsg4 == null ? "" : response.WarnMsg4);
                    sqlParam[0] = new SqlParameter("@pcRefId", apiMsgResponse.Data.RefID);
                    sqlParam[1] = new SqlParameter("@cWarnMsg", apiMsgResponse.Data.WarnMsg == null ? "" : apiMsgResponse.Data.WarnMsg);
                    sqlParam[2] = new SqlParameter("@cWarnMsg2", apiMsgResponse.Data.WarnMsg2 == null ? "" : apiMsgResponse.Data.WarnMsg2);
                    sqlParam[3] = new SqlParameter("@cWarnMsg3", apiMsgResponse.Data.WarnMsg3 == null ? "" : apiMsgResponse.Data.WarnMsg3);
                    sqlParam[4] = new SqlParameter("@cWarnMsg4", apiMsgResponse.Data.WarnMsg4 == null ? "" : apiMsgResponse.Data.WarnMsg4);
                    //20230306, Andhika J, RDN-903, end
                    sqlParam[5] = new SqlParameter("@cFnDocID", paramIn.Data.FnDocID == null ? "" : paramIn.Data.FnDocID);
                    sqlParam[6] = new SqlParameter("@cObjectStore", paramIn.Data.ObjectStore == null ? "" : paramIn.Data.ObjectStore);
                    sqlParam[7] = new SqlParameter("@cDocName", paramIn.Data.DocName == null ? "" : paramIn.Data.DocName);

                    if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                        throw new Exception(errMsg);

                    if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                        throw new Exception("TranId : " + apiMsgResponse.Data.RefID + " not found !");

                    //20230306, Andhika J, RDN-903, begin
                    //response.detailTransactions = JsonConvert.DeserializeObject<List<DetailTransactionSwitching>>(
                    //                        JsonConvert.SerializeObject(dsResult.Tables[0],
                    //                                Newtonsoft.Json.Formatting.None,
                    //                                new JsonSerializerSettings
                    //                                {
                    //                                    NullValueHandling = NullValueHandling.Ignore
                    //                                }));
                    apiMsgResponse.Data.detailTransactions = JsonConvert.DeserializeObject<List<DetailTransactionSwitching>>(
                                            JsonConvert.SerializeObject(dsResult.Tables[0],
                                                    Newtonsoft.Json.Formatting.None,
                                                    new JsonSerializerSettings
                                                    {
                                                        NullValueHandling = NullValueHandling.Ignore
                                                    }));
                    //20230306, Andhika J, RDN-903, end
                }

                            //20230306, Andhika J, RDN-903, begin
                            //apiMsgResponse.Data = response;
                            //20230306, Andhika J, RDN-903, end
                            apiMsgResponse.IsSuccess = true;
                        }

                        #endregion ReksaMaintainSwitching
            }

            //20230727, ahmad.fansyuri, RDN-1017, end

            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        //20201019, Dennis, RDN2-5, end
        #endregion Lump Sum

        #endregion Switch


        #region Authorization
        public ApiMessage<ReksaAuthorizeTransactionRes> AuthorizeTransaction(ApiMessage<ReksaAuthorizeTransactionReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ApiMessage<ReksaAuthorizeTransactionRes> apiMsgResponse = new ApiMessage<ReksaAuthorizeTransactionRes>();
            ReksaAuthorizeTransactionRes apiMsgResponse2 = new ReksaAuthorizeTransactionRes();
            //20230228, Andhika J, RDN-903, begin
            ApiMessage<ReksaAuthorizeTransaction_BSRq> ReqReksaAuthorizeTransaction_BS = new ApiMessage<ReksaAuthorizeTransaction_BSRq>();
            ReqReksaAuthorizeTransaction_BS.Data = new ReksaAuthorizeTransaction_BSRq();
            ApiMessage<ReksaAuthorizeTransaction_BSRs> RespReksaAuthorizeTransaction_BS = new ApiMessage<ReksaAuthorizeTransaction_BSRs>();
            RespReksaAuthorizeTransaction_BS.Data = new ReksaAuthorizeTransaction_BSRs();
            //20230228, Andhika J, RDN-903, end
            string errMsg = "";
            
            apiMsgResponse.copyHeaderForReply(paramIn);
            apiMsgResponse.MessageDateTime = DateTime.Now;
            apiMsgResponse.MessageGUID = paramIn.MessageGUID;
            apiMsgResponse.UserNIK = paramIn.UserNIK.ToString();
            apiMsgResponse.ModuleName = "Wealth.ReksaAccountTransaction";

            //20220425, Rendy, M32022-4, begin
            //string strSPName = "API_ReksaAuthorizeTransaction_BS";
            //20230228, Andhika J, RDN-903, begin
            //string strSPName = "ReksaAuthorizeTransaction_BS";
            ReqReksaAuthorizeTransaction_BS.copyHeaderForReply(paramIn);
            ReqReksaAuthorizeTransaction_BS.Data.nTranId = paramIn.Data.TranId;
            ReqReksaAuthorizeTransaction_BS.Data.bAccepted = paramIn.Data.Accepted;
            ReqReksaAuthorizeTransaction_BS.Data.cNik = Convert.ToInt32(paramIn.UserNIK);
            ReqReksaAuthorizeTransaction_BS.Data.pcTranCode = paramIn.Data.TranCode;
            //20230228, Andhika J, RDN-903, end
            //20220425, Rendy, M32022-4, end

            try
            {
                // approve / reject dengan RefId
                if (!string.IsNullOrEmpty(paramIn.Data.RefID) && paramIn.Data.TranId == 0)
                {

                    List<int> nTranId = this.getTranIdAuthorize(paramIn.Data.RefID);
                    List<string> cTranCode = this.getTranCodeAuthorize(paramIn.Data.RefID);

                    for (int i = 0; i < nTranId.Count; i++)
                    {
                        //20230228, Andhika J, RDN-903, begin
                        #region RemarkExisting
                        //param = new List<SQLSPParameter>();
                        //param.Add(new SQLSPParameter("@nTranId", Convert.ToInt64(nTranId[i])));
                        //param.Add(new SQLSPParameter("@bAccepted", Convert.ToByte(paramIn.Data.Accepted)));
                        //param.Add(new SQLSPParameter("@cNik", Convert.ToInt64(paramIn.UserNIK)));
                        //param.Add(new SQLSPParameter("@pcTranCode", cTranCode[i], 8));
                        ////20220425, Rendy, M32022-4, begin
                        ////param.Add(new SQLSPParameter("@pcErrMsgOut", "", ParamDirection.OUTPUT));
                        ////20220425, Rendy, M32022-4, end

                        //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out errMsg))
                        //{
                        //    //if (param[4].ParameterValue.ToString() == "")
                        //    //{
                        //    //    apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + nTranId[i].ToString() + Environment.NewLine;
                        //    //    apiMsgResponse.Data = apiMsgResponse2;
                        //    //    apiMsgResponse.IsSuccess = true;
                        //    //}
                        //    //else
                        //    //{
                        //    //    apiMsgResponse.IsSuccess = false;
                        //    //    apiMsgResponse.ErrorDescription = "Gagal approve untuk RefID " + paramIn.Data.RefID + " TranId " + nTranId[i] + Environment.NewLine;
                        //    //    apiMsgResponse.ErrorDescription = apiMsgResponse.ErrorDescription + param[4].ParameterValue.ToString();
                        //    //}

                        //    apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + nTranId[i].ToString() + Environment.NewLine;
                        //    apiMsgResponse.Data = apiMsgResponse2;
                        //    apiMsgResponse.IsSuccess = true;
                        //}
                        //else
                        //{
                        //    throw new Exception(errMsg);
                        //}
                        #endregion
                        #region HitAPI
                        RespReksaAuthorizeTransaction_BS = ReksaAuthorizeTransaction_BS(ReqReksaAuthorizeTransaction_BS);
                        if (RespReksaAuthorizeTransaction_BS.IsSuccess == true)
                        {
                            apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + nTranId[i].ToString() + Environment.NewLine;
                            apiMsgResponse.Data = apiMsgResponse2;
                            apiMsgResponse.IsSuccess = true;
                        }
                        else
                        {

                            throw new Exception(RespReksaAuthorizeTransaction_BS.ErrorDescription);
                        }
                        #endregion HitAPI
                        //20230228, Andhika J, RDN-903, end
                    }
                }
                // Approve / Reject dengan TranId
                else if (string.IsNullOrEmpty(paramIn.Data.RefID) && paramIn.Data.TranId != 0 && !string.IsNullOrEmpty(paramIn.Data.TranCode))
                {

                    //20230228, Andhika J, RDN-903, begin
                    #region RemarkExisting
                    //param = new List<SQLSPParameter>();
                    //param.Add(new SQLSPParameter("@nTranId", Convert.ToInt64(paramIn.Data.TranId)));
                    //param.Add(new SQLSPParameter("@bAccepted", Convert.ToByte(paramIn.Data.Accepted)));
                    //param.Add(new SQLSPParameter("@cNik", Convert.ToInt64(paramIn.UserNIK)));
                    //param.Add(new SQLSPParameter("@pcTranCode", paramIn.Data.TranCode, 8));
                    ////param.Add(new SQLSPParameter("@pcErrMsgOut", "", ParamDirection.OUTPUT));

                    //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out errMsg))
                    //{
                    //    //if(param[4].ParameterValue.ToString() == "")
                    //    //{
                    //    //    apiMsgResponse2.Message += "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Tran ID " + paramIn.Data.TranId.ToString() + Environment.NewLine;
                    //    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    //    apiMsgResponse.IsSuccess = true;
                    //    //}
                    //    //else
                    //    //{
                    //    //    apiMsgResponse.IsSuccess = false;
                    //    //    apiMsgResponse.ErrorDescription = "Gagal approve untuk RefID " + paramIn.Data.RefID + "TranId " + paramIn.Data.TranId + Environment.NewLine;
                    //    //    apiMsgResponse.ErrorDescription = apiMsgResponse.ErrorDescription + param[4].ParameterValue.ToString();

                    //    //}
                    //    apiMsgResponse2.Message += "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Tran ID " + paramIn.Data.TranId.ToString() + Environment.NewLine;
                    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    apiMsgResponse.IsSuccess = true;
                    //}
                    //else
                    //{
                    //    throw new Exception(errMsg);
                    //}
                    #endregion
                    #region HitAPI
                    RespReksaAuthorizeTransaction_BS = ReksaAuthorizeTransaction_BS(ReqReksaAuthorizeTransaction_BS);
                    if (RespReksaAuthorizeTransaction_BS.IsSuccess == true)
                    {
                        apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + ReqReksaAuthorizeTransaction_BS.Data.nTranId.ToString() + Environment.NewLine;
                        apiMsgResponse.Data = apiMsgResponse2;
                        apiMsgResponse.IsSuccess = true;
                    }
                    else
                    {

                        throw new Exception(RespReksaAuthorizeTransaction_BS.ErrorDescription);
                    }
                    #endregion HitAPI
                }
                // untuk future RDB
                else if (string.IsNullOrEmpty(paramIn.Data.RefID) && paramIn.Data.TranId == -1 && !string.IsNullOrEmpty(paramIn.Data.TranCode))
                {
                    //20230228, Andhika J, RDN-903, begin
                    #region RemarkExisting
                    //param = new List<SQLSPParameter>();
                    //param.Add(new SQLSPParameter("@nTranId", 0));
                    //param.Add(new SQLSPParameter("@bAccepted", Convert.ToByte(paramIn.Data.Accepted)));
                    //param.Add(new SQLSPParameter("@cNik", Convert.ToInt64(paramIn.UserNIK)));
                    //param.Add(new SQLSPParameter("@pcTranCode", paramIn.Data.TranCode, 8));
                    ////param.Add(new SQLSPParameter("@pcErrMsgOut", "", ParamDirection.OUTPUT));

                    //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out errMsg))
                    //{
                    //    //if(param[4].ParameterValue.ToString() == "")
                    //    //{
                    //    //    apiMsgResponse2.Message += "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Future RDB " + paramIn.Data.TranCode.ToString() + Environment.NewLine;
                    //    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    //    apiMsgResponse.IsSuccess = true;
                    //    //}
                    //    //else
                    //    //{
                    //    //    apiMsgResponse.IsSuccess = false;
                    //    //    apiMsgResponse.ErrorDescription = "Gagal approve untuk Future RDB TranCode " + paramIn.Data.TranCode + Environment.NewLine;
                    //    //    apiMsgResponse.ErrorDescription = apiMsgResponse.ErrorDescription + param[4].ParameterValue.ToString();
                    //    //}
                    //    apiMsgResponse2.Message += "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Future RDB " + paramIn.Data.TranCode.ToString() + Environment.NewLine;
                    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    apiMsgResponse.IsSuccess = true;
                    //}
                    //else
                    //{
                    //    throw new Exception(errMsg);
                    //}
                    #endregion
                    #region HitAPI

                    ReqReksaAuthorizeTransaction_BS.Data.nTranId = 0;

                    RespReksaAuthorizeTransaction_BS = ReksaAuthorizeTransaction_BS(ReqReksaAuthorizeTransaction_BS);
                    if (RespReksaAuthorizeTransaction_BS.IsSuccess == true)
                    {
                        apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + ReqReksaAuthorizeTransaction_BS.Data.nTranId.ToString() + Environment.NewLine;
                        apiMsgResponse.Data = apiMsgResponse2;
                        apiMsgResponse.IsSuccess = true;
                    }
                    else
                    {

                        throw new Exception(RespReksaAuthorizeTransaction_BS.ErrorDescription);
                    }
                    #endregion HitAPI
                    //20230228, Andhika J, RDN-903, end
                }
                else if (!string.IsNullOrEmpty(paramIn.Data.RefID) && paramIn.Data.TranId != 0 && !string.IsNullOrEmpty(paramIn.Data.TranCode))
                {
                    //20230228, Andhika J, RDN-903, begin
                    #region RemarkExisting
                    //param = new List<SQLSPParameter>();
                    //param.Add(new SQLSPParameter("@nTranId", Convert.ToInt64(paramIn.Data.TranId)));
                    //param.Add(new SQLSPParameter("@bAccepted", Convert.ToByte(paramIn.Data.Accepted)));
                    //param.Add(new SQLSPParameter("@cNik", Convert.ToInt64(paramIn.UserNIK)));
                    //param.Add(new SQLSPParameter("@pcTranCode", paramIn.Data.TranCode, 8));
                    ////param.Add(new SQLSPParameter("@pcErrMsgOut", "", ParamDirection.OUTPUT));

                    //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out errMsg))
                    //{
                    //    //if (param[4].ParameterValue.ToString() == "")
                    //    //{
                    //    //    apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + paramIn.Data.TranId + Environment.NewLine;
                    //    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    //    apiMsgResponse.IsSuccess = true;
                    //    //}
                    //    //else
                    //    //{
                    //    //    apiMsgResponse.IsSuccess = false;
                    //    //    apiMsgResponse.ErrorDescription = "Gagal approve untuk RefID " + paramIn.Data.RefID + " TranId " + paramIn.Data.TranId + Environment.NewLine;
                    //    //    apiMsgResponse.ErrorDescription = apiMsgResponse.ErrorDescription + param[4].ParameterValue.ToString();
                    //    //}
                    //    apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + paramIn.Data.TranId + Environment.NewLine;
                    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    apiMsgResponse.IsSuccess = true;
                    //}
                    //else
                    //{
                    //    throw new Exception(errMsg);
                    //}
                    #endregion
                    #region HitAPI
                    RespReksaAuthorizeTransaction_BS = ReksaAuthorizeTransaction_BS(ReqReksaAuthorizeTransaction_BS);
                    if (RespReksaAuthorizeTransaction_BS.IsSuccess == true)
                    {
                        apiMsgResponse2.Message += "Berhasil" + (paramIn.Data.Accepted == false ? " Reject " : " Approve ") + " Untuk Tran ID " + ReqReksaAuthorizeTransaction_BS.Data.nTranId.ToString() + Environment.NewLine;
                        apiMsgResponse.Data = apiMsgResponse2;
                        apiMsgResponse.IsSuccess = true;
                    }
                    else
                    {

                        throw new Exception(RespReksaAuthorizeTransaction_BS.ErrorDescription);
                    }
                    #endregion HitAPI
                    //20230228, Andhika J, RDN-903, end
                }
                else
                {
                    throw new Exception("Ref Id atau TranId dan Trancode tidak boleh kosong");
                }
               
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }

        #endregion Authorization

        #region AuthorizationSwitching

        public ApiMessage<ReksaAuthorizeSwitchingRes> AuthorizeSwtiching(ApiMessage<ReksaAuthorizeSwitchingReq> paramIn)
        {
            //20230214, Andhika J, RDN-903, begin
            ApiMessage<ReksaAuthorizeSwitching_BSRq> ReqReksaAuthorizeSwitching_BS = new ApiMessage<ReksaAuthorizeSwitching_BSRq>();
            ReqReksaAuthorizeSwitching_BS.Data = new ReksaAuthorizeSwitching_BSRq();
            ApiMessage<ReksaAuthorizeSwitching_BSRs> RespReksaAuthorizeSwitching_BS = new ApiMessage<ReksaAuthorizeSwitching_BSRs>();
            RespReksaAuthorizeSwitching_BS.Data = new ReksaAuthorizeSwitching_BSRs();
            //20230214, Andhika J, RDN-903, end
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            ApiMessage<ReksaAuthorizeSwitchingRes> apiMsgResponse = new ApiMessage<ReksaAuthorizeSwitchingRes>();
            ReksaAuthorizeSwitchingRes apiMsgResponse2 = new ReksaAuthorizeSwitchingRes();
            apiMsgResponse.copyHeaderForReply(paramIn);

            string errMsg = "";

            #region validasi
            //Validasi 
            //NIK
            if (paramIn.UserNIK.ToString() == "")
            {
                apiMsgResponse.ErrorDescription = "User NIK tidak boleh kosong";
                apiMsgResponse.IsSuccess = false;
                return apiMsgResponse;

            }
            //GUID
            if (paramIn.TransactionMessageGUID == "")
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "Guid tidak boleh kosong";
                return apiMsgResponse;
            }
            #endregion validasi

            apiMsgResponse.copyHeaderForReply(paramIn);
            apiMsgResponse.MessageDateTime = DateTime.Now;
            apiMsgResponse.MessageGUID = paramIn.MessageGUID;
            apiMsgResponse.UserNIK = paramIn.UserNIK.ToString();
            apiMsgResponse.ModuleName = "Wealth.ReksaAccountTransaction";

            //20230214, Andhika J, RDN-903, begin
            //string strSPName = "ReksaAuthorizeSwitching_BS";
            ReqReksaAuthorizeSwitching_BS.copyHeaderForReply(paramIn);
            ReqReksaAuthorizeSwitching_BS.Data.TranId = paramIn.Data.TranId;
            ReqReksaAuthorizeSwitching_BS.Data.bAccepted = paramIn.Data.Accepted;
            ReqReksaAuthorizeSwitching_BS.Data.NIK = Convert.ToInt32(paramIn.UserNIK);
            //20230214, Andhika J, RDN-903, end
            try
            {
                if (string.IsNullOrEmpty(paramIn.Data.RefID) && paramIn.Data.TranId != 0)
                {
                    //@nTranId      int,        
                    //@bAccepted    bit,
                    //@cNik         int,
                    //@pcTranCode   char (8) = null

                    //20230214, Andhika J, RDN-903, begin
                    #region RemarkExisting
                    //param = new List<SQLSPParameter>();
                    //param.Add(new SQLSPParameter("@nTranId", paramIn.Data.TranId));
                    //param.Add(new SQLSPParameter("@bAccepted", paramIn.Data.Accepted));
                    //param.Add(new SQLSPParameter("@cNik", paramIn.UserNIK));

                    //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out errMsg))
                    //{
                    //    apiMsgResponse2.Message = "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Ref ID " + paramIn.Data.RefID + " memiliki Tran Id " + paramIn.Data.TranId;
                    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    apiMsgResponse.IsSuccess = true;
                    //}
                    //else
                    //{
                    //    apiMsgResponse.IsSuccess = false;
                    //    apiMsgResponse.ErrorDescription = "RefID " + paramIn.Data.RefID + " Tidak Ditemukan ";
                    //}
                    #endregion RemarkExisting
                    #region HitAPI
                    ReksaAuthorizeSwitching_BS(ReqReksaAuthorizeSwitching_BS, out RespReksaAuthorizeSwitching_BS);
                    if (RespReksaAuthorizeSwitching_BS.IsSuccess == true)
                    {
                        apiMsgResponse2.Message = "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Ref ID " + paramIn.Data.RefID + " memiliki Tran Id " + paramIn.Data.TranId;
                        apiMsgResponse.Data = apiMsgResponse2;
                        apiMsgResponse.IsSuccess = true;
                    }
                    else
                    {
                        if (RespReksaAuthorizeSwitching_BS.ErrorDescription != "")
                        {
                            apiMsgResponse.IsSuccess = false;
                            apiMsgResponse.ErrorDescription = RespReksaAuthorizeSwitching_BS.ErrorDescription;
                        }
                        else
                        {
                            apiMsgResponse.IsSuccess = false;
                            apiMsgResponse.ErrorDescription = "RefID " + paramIn.Data.RefID + " Tidak Ditemukan ";
                        }
                    }
                    #endregion HitAPI
                    //20230214, Andhika J, RDN-903, end
                }
                else if (!string.IsNullOrEmpty(paramIn.Data.RefID) && paramIn.Data.TranId == 0)
                {
                    List<int> nTranId = this.getTranIdAuthorizeSwitching(paramIn.Data.RefID);

                    if (nTranId.Contains(-1))
                    {
                        apiMsgResponse.IsSuccess = false;
                        apiMsgResponse.ErrorDescription = "Ref ID tidak memiliki TranId";
                    }

                    //@nTranId      int,        
                    //@bAccepted    bit,
                    //@cNik         int,
                    //@pcTranCode   char (8) = null

                    for(int i=0; i<nTranId.Count(); i++)
                    {
                        //20230214, Andhika J, RDN-903, begin
                        #region RemarkExisting 
                        //param = new List<SQLSPParameter>();
                        //param.Add(new SQLSPParameter("@nTranId", nTranId[i]));
                        //param.Add(new SQLSPParameter("@bAccepted", paramIn.Data.Accepted));
                        //param.Add(new SQLSPParameter("@cNik", paramIn.UserNIK));

                        //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out errMsg))
                        //{
                        //    apiMsgResponse2.Message = "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Tran Id " + nTranId[i]; 
                        //    apiMsgResponse.Data = apiMsgResponse2;
                        //    apiMsgResponse.IsSuccess = true;
                        //}
                        //else
                        //{
                        //    apiMsgResponse.IsSuccess = false;
                        //    apiMsgResponse.ErrorDescription = "RefID " + paramIn.Data.RefID + " Tidak Ditemukan ";
                        //}
                        #endregion RemarkExisting
                        #region HitAPI
                        ReksaAuthorizeSwitching_BS(ReqReksaAuthorizeSwitching_BS, out RespReksaAuthorizeSwitching_BS);
                        if (RespReksaAuthorizeSwitching_BS.IsSuccess == true)
                        {
                            apiMsgResponse2.Message = "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Ref ID " + paramIn.Data.RefID + " memiliki Tran Id " + paramIn.Data.TranId;
                            apiMsgResponse.Data = apiMsgResponse2;
                            apiMsgResponse.IsSuccess = true;
                        }
                        else
                        {
                            if (RespReksaAuthorizeSwitching_BS.ErrorDescription != "")
                            {
                                apiMsgResponse.IsSuccess = false;
                                apiMsgResponse.ErrorDescription = RespReksaAuthorizeSwitching_BS.ErrorDescription;
                            }
                            else
                            {
                                apiMsgResponse.IsSuccess = false;
                                apiMsgResponse.ErrorDescription = "RefID " + paramIn.Data.RefID + " Tidak Ditemukan ";
                            }
                        }
                        #endregion HitAPI
                        //20230214, Andhika J, RDN-903, end
                    }
                }
                else if(!string.IsNullOrEmpty(paramIn.Data.RefID) && paramIn.Data.TranId != 0)
                {
                    //20230214, Andhika J, RDN-903, begin
                    #region RemarkExisting
                    //param = new List<SQLSPParameter>();
                    //param.Add(new SQLSPParameter("@nTranId", paramIn.Data.TranId));
                    //param.Add(new SQLSPParameter("@bAccepted", paramIn.Data.Accepted));
                    //param.Add(new SQLSPParameter("@cNik", paramIn.UserNIK));

                    //if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out errMsg))
                    //{
                    //    apiMsgResponse2.Message = "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Ref ID " + paramIn.Data.RefID + " memiliki Tran Id " + paramIn.Data.TranId;
                    //    apiMsgResponse.Data = apiMsgResponse2;
                    //    apiMsgResponse.IsSuccess = true;
                    //}
                    //else
                    //{
                    //    apiMsgResponse.IsSuccess = false;
                    //    apiMsgResponse.ErrorDescription = "RefID " + paramIn.Data.RefID + " Tidak Ditemukan ";
                    //}
                    #endregion RemarkExisting
                    #region HitAPI
                    ReksaAuthorizeSwitching_BS(ReqReksaAuthorizeSwitching_BS, out RespReksaAuthorizeSwitching_BS);
                    if (RespReksaAuthorizeSwitching_BS.IsSuccess == true)
                    {
                        apiMsgResponse2.Message = "Berhasil " + (paramIn.Data.Accepted == false ? "Reject " : "Approve ") + "Untuk Ref ID " + paramIn.Data.RefID + " memiliki Tran Id " + paramIn.Data.TranId;
                        apiMsgResponse.Data = apiMsgResponse2;
                        apiMsgResponse.IsSuccess = true;
                    }
                    else
                    {
                        if (RespReksaAuthorizeSwitching_BS.ErrorDescription != "")
                        {
                            apiMsgResponse.IsSuccess = false;
                            apiMsgResponse.ErrorDescription = RespReksaAuthorizeSwitching_BS.ErrorDescription;
                        }
                        else
                        {
                            apiMsgResponse.IsSuccess = false;
                            apiMsgResponse.ErrorDescription = "RefID " + paramIn.Data.RefID + " Tidak Ditemukan ";
                        }
                    }
                    #endregion HitAPI
                    //20230214, Andhika J, RDN-903, end
                }
                else
                {
                    apiMsgResponse.IsSuccess = false;
                    apiMsgResponse.ErrorDescription = "RefID / TranId tidak boleh kosong";
                }

            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }

        #endregion AuthorizationSwitching

        #region Cek Settlement
        public ApiMessage<List<ReksaCekSettlementRes>> CekSettlement(ApiMessage<ReksaCekSettlementReq> paramIn)
        {
            ApiMessage<List<ReksaCekSettlementRes>> ApiMsgResponse = new ApiMessage<List<ReksaCekSettlementRes>>();
            List<ReksaCekSettlementRes> listResponse = new List<ReksaCekSettlementRes>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            ApiMsgResponse.copyHeaderForReply(paramIn);
            ApiMsgResponse.MessageDateTime = DateTime.Now;
            ApiMsgResponse.MessageGUID = paramIn.MessageGUID;
            ApiMsgResponse.UserNIK = paramIn.UserNIK;
            ApiMsgResponse.ModuleName = "Wealth.AccountTransaction.API Cek Settlement";


            try
            {
                string sqlCommand = @"
                declare @vcRefID varchar(20)
                set @vcRefID = '" + paramIn.Data.RefId + @"'

                 --create temp table for result
                declare @temp_CekSettle table(
                    ClientId        int
                  , ClientIdTarget  int
                  , BillId          int
                  , TranDate        datetime
                  , TranId          int
                  , TranCode        char(8)
                  , TranType        int
                  , ProdId          int
                  , ProductName     varchar(100)
                  , ProdIdTarget    int
                  , ProductNameTarget varchar(100) 
                  , Status          tinyint
                  , NAV             decimal(25,13)
                  , NAVTarget       decimal(25,13)
                  , NAVValueDate    datetime
                  , FeeBruto        decimal(25,13)
                  , TranUnit        decimal(25,13)
                  , TranUnitTarget  decimal(25,13)
                  , TranCCY         varchar(4)
                  , FullAmount      tinyint
                  , TranAmt         decimal(25,13)
                  , GrandTotal      decimal(25,13)
                  , RefID           varchar(20)
                    )


                if exists (
                    select top 1 1
                    from ReksaSwitchingTransaction_TM
                        where RefID = @vcRefID
                        )
                begin
                    insert @temp_CekSettle
                    select ClientIdSwcOut
                            , ClientIdSwcIn
                            , BillId
                            , TranDate
                            , TranId
                            , TranCode
                            , TranType
                            , ProdSwitchOut
                            , rp1.ProdName as 'ProductName'
                            , ProdSwitchIn
			                , rp2.ProdName as 'ProductNameTarget'
                            , rs.Status
                            , NAVSwcOut
                            , NAVSwcIn
                            , NAVValueDate
                            , ActualSwitchingFee
                            , TranUnit
                            , 0
                            , TranCCY
                            , 1
                            , TranAmt
                            , TranAmt
                            , RefID
                        from ReksaSwitchingTransaction_TM rs with(Nolock)
		                left join ReksaProduct_TM rp1 on rp1.ProdId = rs.ProdSwitchOut
		                left join ReksaProduct_TM rp2 on rp2.ProdId = rs.ProdSwitchIn
                        where RefID = @vcRefID


                    update tcs
                    set   tcs.TranUnitTarget = tt.TranUnit
                        , tcs.TranAmt        = tt.TranAmt
                        , tcs.GrandTotal     = tt.TranAmt
                        , tcs.NAVTarget      = tt.NAV
                    from @temp_CekSettle tcs
                        join ReksaTransaction_TT tt
                            on tcs.RefID = tt.RefID
                            and tcs.TranCode = tt.TranCode
                            and tcs.NAVValueDate = tt.NAVValueDate
                    where tcs.RefID = @vcRefID
                        and tt.TranType in (1,2,8)
                        and isnull(tt.ExtStatus,0) in (10,20) 

                    update tcs
                    set   tcs.TranUnitTarget = th.TranUnit
                        , tcs.TranAmt        = th.TranAmt
                        , tcs.GrandTotal     = th.TranAmt
                        , tcs.NAVTarget      = th.NAV
                    from @temp_CekSettle tcs
                        join ReksaTransaction_TH th
                            on tcs.RefID = th.RefID
                            and tcs.TranCode = th.TranCode
                            and tcs.NAVValueDate = th.NAVValueDate
                    where tcs.RefID = @vcRefID
                        and th.TranType in (1,2,8)
                        and isnull(th.ExtStatus,0) in (10,20) 


                    update tcs
                    set   tcs.TranUnit   = tt.TranUnit
                        , tcs.NAV       = tt.NAV
                    from @temp_CekSettle tcs
                        join ReksaTransaction_TT tt
                            on tcs.RefID = tt.RefID
                            and tcs.TranCode = tt.TranCode
                            and tcs.NAVValueDate = tt.NAVValueDate
                    where tcs.RefID = @vcRefID
                        and tt.TranType in (3,4)
                        and isnull(tt.ExtStatus,0) in (10,20) 

                    update tcs
                    set   tcs.TranUnit   = th.TranUnit
                        , tcs.NAV      = th.NAV
                    from @temp_CekSettle tcs
                        join ReksaTransaction_TH th
                            on tcs.RefID = th.RefID
                            and tcs.TranCode = th.TranCode
                            and tcs.NAVValueDate = th.NAVValueDate
                    where tcs.RefID = @vcRefID
                        and th.TranType in (3,4)
                        and isnull(th.ExtStatus,0) in (10,20) 
                    goto HYPERJUMP 
                end
                else
                --isi data untuk Subs dan Redemp
                begin
                    insert @temp_CekSettle
                    select ClientId
                            , 0
                            , BillId
                            , TranDate
                            , TranId
                            , TranCode
                            , TranType
                            , rt.ProdId
                            , rp.ProdName as 'ProductName'
                            , 0
                            , ''
                            , rt.Status
                            , rt.NAV
                            , 0
                            , NAVValueDate
                            , case when TranType in (1,2,8) then SubcFee
                                    when TranType in (3,4) then RedempFee
                                    else 0 end as 'FeeBruto'
                            , TranUnit
                            , 0
                            , TranCCY
                            , isnull(FullAmount,0) as FullAmount
                            , TranAmt
                            , case when TranType in (1,2,8) and FullAmount = 1 then TranAmt + SubcFee
                                    when TranType in (3,4)                       then TranAmt - RedempFee
                                    else TranAmt end
                                as 'GrandTotal'
                            , RefID
                        from ReksaTransaction_TH rt with(Nolock)
		                left join ReksaProduct_TM rp with (Nolock) on rp.ProdId = rt.ProdId 
                        where RefID = @vcRefID
                    union all
                        select ClientId
                            , 0
                            , BillId
                            , TranDate
                            , TranId
                            , TranCode
                            , TranType
                            , rt.ProdId
                            , rp.ProdName as 'ProductName'
                            , 0
                            , ''
                            , rt.Status
                            , rt.NAV
                            , 0
                            , NAVValueDate
                            , case when TranType in (1,2,8) then SubcFee
                                    when TranType in (3,4) then RedempFee
                                    else 0 end as 'FeeBruto'
                            , TranUnit
                            , 0
                            , TranCCY
                            , isnull(FullAmount,0) as FullAmount
                            , TranAmt
                            , case when TranType in (1,2,8) and FullAmount = 1 then TranAmt + SubcFee
                                    when TranType in (3,4)                       then TranAmt - RedempFee
                                    else TranAmt end
                                as 'GrandTotal'
                            , RefID
                        from ReksaTransaction_TT rt with(Nolock)
                        left join ReksaProduct_TM rp with (Nolock) on rp.ProdId = rt.ProdId 
                        where RefID = @vcRefID
                        goto HYPERJUMP
                end

                HYPERJUMP:
                select *
                from @temp_CekSettle
                return

                ";

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    #region mapping sesuai tipe data agar tidak merubah 
                    listResponse = JsonConvert.DeserializeObject<List<ReksaCekSettlementRes>>(JsonConvert.SerializeObject(dsOut.Tables[0]));
                    #endregion mapping sesuai tipe data agar tidak merubah

                    ApiMsgResponse.Data = listResponse;
                    ApiMsgResponse.IsSuccess = true;
                }

                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return ApiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorCode = "500";
                ApiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return ApiMsgResponse;
        }
        #endregion Cek Settlement

        #region PortofolioNasabah
        public ApiMessage<List<PortofolioNasabahRes>> getPortofolioNasabah(ApiMessage<PortofolioNasabahReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<PortofolioNasabahRes> listResponse = new List<PortofolioNasabahRes>();
            ApiMessage<List<PortofolioNasabahRes>> apiMsgResponse = new ApiMessage<List<PortofolioNasabahRes>>();
            decimal LastBalance = 0;

            //20230308, sandi, RDN-899, begin
            decimal OutstandingAmount = 0;
            //20230308, sandi, RDN-899, end

            DataSet dsResult = null;
            string strErrMsg = "";

            apiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                string sqlCommand = @"
                    declare @dNAVValueDate          datetime
                            , @dCurrWorkingDate		datetime

                    select top 1 @dNAVValueDate = ValueDate
                    from ReksaNAVParam_TH
                    order by ValueDate desc

					select @dCurrWorkingDate = current_working_date
					from control_table

                    select rp.ProdId as 'ProductId'
                           , rp.ProdCode as 'ProductCode'
	                       , rp.ProdName as 'ProductName'
	                       , rp.ProdCCY
						   , rdrp.RiskProfile 
	                       , rdrp.RiskProfileDesc as 'ProductRiskProfile'
						   , rt.TypeCode
						   , rt.TypeName -- bahasa indonesia
						   , rt.TypeNameEnglish -- english
	                       , ClientId
	                       , ClientCode
	                       , rcd.UnitBalance --ReksaCIFData_TM
                           , rcd.UnitBalance * rnp.NAV as 'TotalInvestasi'--ReksaCIFData_TM * ReksaNAVParam_TH
                           , rnp.ValueDate as 'NAVValueDate' -- ReksaNAVParam_TH
	                       , rnp.NAV 
	                       , 0 as 'isRDB'
	                       , 0 as 'isTA'
	                       , CAST(NULL as int) as 'JangkaWaktu'
	                       , CAST(NULL as datetime) as 'JatuhTempo'
	                       , CAST(NULL as varchar) as 'FreqDebetMethod'
	                       , CAST(NULL as datetime) as 'StartDebetDate'
						   --tambahan omdonz, begin
						   , CAST(NULL as datetime) as 'NAVValueDateSubsNew'
						   , CAST(NULL as tinyint) as 'IsBerhenti'
						   , CAST(NULL as tinyint) as 'IsAsuransi'
						   --tambahan omdonz, end
                       --20230725, ahmad.fansyuri, RDN-1017, begin
                           , CAST(NULL as tinyint) as 'IsRDBDoneDebet'
                            , CAST(1 as tinyint) as 'IsMature'
                       --20230725, ahmad.fansyuri, RDN-1017, end
                    into #tempDataPorto
                    from ReksaProduct_TM rp
                    left join ReksaProductRiskProfile_TM rprp 
	                    on rp.ProdCode = rprp.ProductCode
                    left join ReksaDescRiskProfile_TR rdrp 
	                    on rdrp.RiskProfile = rprp.RiskProfile
                    left join ReksaCIFData_TM rcd
	                    on rp.ProdId = rcd.ProdId
                    left join ReksaNAVParam_TH rnp
	                    on rp.ProdId = rnp.ProdId
						and rnp.ValueDate = @dNAVValueDate
					left join ReksaType_TR rt
                    on rp.TypeId = rt.TypeId
                    where rcd.CIFNo = @pcCIFNo
	                    and rnp.ValueDate = @dNAVValueDate --nav terbaru
	                    and rp.Status = 1 --product aktif
	                    and CIFStatus = 'A'

                    --update jika RDB
                    update t
                    set t.isRDB = 1
	                    , t.JangkaWaktu = rrs.JangkaWaktu
	                    , t.JatuhTempo = rrs.JatuhTempo
	                    , t.FreqDebetMethod = rrs.FreqDebetMethod
	                    , t.StartDebetDate = rrs.StartDebetDate
					--tambahan omdonz, begin
						, t.IsAsuransi = rrs.Asuransi
					--tambahan omdonz, end
--20230725, ahmad.fansyuri, RDN-1017, begin   
                        , t.IsMature = case when datediff(d, rrs.JatuhTempo, getdate()) >= 0 then 1 else 0 end
--20230725, ahmad.fansyuri, RDN-1017, end
                    from #tempDataPorto t
                    join dbo.ReksaRegulerSubscriptionClient_TM rrs
                        on t.ClientId = rrs.ClientId

                    --update jika TA
                    update t
                    set isTA = 1
                    from #tempDataPorto t
                    join dbo.ReksaClientCodeTAMapping_TM rc
	                    on t.ClientId = rc.ClientIdTax
                    where rc.IsTaxAmnesty = 1

					--update tunggakan
					update t
					set IsBerhenti = 1
					from #tempDataPorto t
					join dbo.ReksaRegulerSubscriptionClient_TM rrs
						on t.ClientId= rrs.ClientId
					join ReksaRegulerSubscriptionSchedule_TT a
						on rrs.TranId = a.TranId
							and a.StatusId = 5

					update t
					set t.NAVValueDateSubsNew = rt.NAVValueDate
					from #tempDataPorto t
					join ReksaTransaction_TH rt
						on t.ClientId = rt.ClientId
							and rt.TranType = 1
							and rt.Status = 1

					update t
					set t.NAVValueDateSubsNew = rt.NAVValueDate
					from #tempDataPorto t
					join ReksaTransaction_TT rt
						on t.ClientId = rt.ClientId
							and rt.TranType = 1
							and rt.Status = 1

                    --20230725, ahmad.fansyuri, RDN-1017, begin                
                    update t 
                    set IsRDBDoneDebet = 1
                    from #tempDataPorto t
                    
                    update t
                    set IsRDBDoneDebet = 0
 					from #tempDataPorto t join ReksaRegulerSubscriptionSchedule_TT a
					    on t.ClientId = a.ClientId
                    where a.StatusId in (0) and a.Type = 0 and t.FreqDebetMethod in ('D')       
                    
                    update t
                    set IsRDBDoneDebet = 0
                    from #tempDataPorto t join ReksaRegulerSubscriptionSchedule_TT a
                        on t.ClientId = a.ClientId
                    where a.StatusId in (0, 3, 5, 6) and a.Type = 0 and t.FreqDebetMethod not in ('D')
                    
                    --update #tempDataPorto set IsRDBDoneDebet = 0 where IsMature = 0 --kondisi harus sudah jatuh tempo dan beres schedule
                    --20230725, ahmad.fansyuri, RDN-1017, end

                    --tampilkan data 
                    select ProductId
                            , ProductCode
	                        , ProductName
	                        , ProdCCY
							, RiskProfile 
	                        , ProductRiskProfile
							, TypeCode
						    , TypeName -- bahasa indonesia
						    , TypeNameEnglish -- english
	                        , ClientId
	                        , ClientCode
	                        , UnitBalance --ReksaCIFData_TM
	                        , TotalInvestasi--ReksaCIFData_TM * ReksaNAVParam_TH
	                        , NAVValueDate -- ReksaNAVParam_TH
	                        , NAV 
	                        , case when isRDB = 1 then 'RDB' else 'Non RDB' end as 'isRDB'
	                        , case when isTA = 1 then 'TA' else 'Non TA' end as 'isTA'
	                        , isnull(JangkaWaktu, 0) as 'JangkaWaktu'
	                        , case when isnull(JatuhTempo, 0) = 0 then null else JatuhTempo end as 'JatuhTempo'
	                        , isnull(FreqDebetMethod, '') as 'FreqDebetMethod'
		                    , case when isRDB =1 
								   then case when isnull(StartDebetDate, 0) = 0 
											 then null else StartDebetDate end
								   else NAVValueDateSubsNew end
							  as 'StartDebetDate'
		                    , case when isRDB = 1 
								   then datediff(month, 
										case when isnull(StartDebetDate, 0) = 0 
											 then getdate() else StartDebetDate end, getdate())
								   else datediff(mm, NAVValueDateSubsNew, @dCurrWorkingDate) end 
							  as 'Period'
							, IsBerhenti
							, IsAsuransi
                    --20230725, ahmad.fansyuri, RDN-1017, begin
                            , IsRDBDoneDebet
                    --20230725, ahmad.fansyuri, RDN-1017, end
                    from #tempDataPorto

                    drop table #tempDataPorto";
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pcCIFNo", paramIn.Data.CIFNo);
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out strErrMsg))
                {
                    if (dsResult.Tables.Count < 1 || dsResult.Tables[0].Rows.Count.Equals(0))
                    {
                        apiMsgResponse.IsSuccess = false;
                        apiMsgResponse.ErrorCode = "3000";
                        apiMsgResponse.ErrorDescription = "Data not found";
                        return apiMsgResponse;
                    }
                     
                    listResponse = JsonConvert.DeserializeObject<List<PortofolioNasabahRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                    int count = listResponse.Count;
                    for (int i=0; i<count; i++)
                    {
                        if (listResponse[i].JatuhTempo.ToString() == "01/01/1900 00:00:00")
                            listResponse[i].JatuhTempo = null;
                        
                        if (listResponse[i].StartDebetDate.ToString() == "01/01/1900 00:00:00")
                            listResponse[i].StartDebetDate = null;   
                    }

                    apiMsgResponse.Data = listResponse;
                    apiMsgResponse.IsSuccess = true;


                    // add last balance 
                    for(int i=0; i<apiMsgResponse.Data.Count; i++)
                    {
                        if(apiMsgResponse.Data[i].UnitBalance == 0)
                        {
                            apiMsgResponse.Data[i].OutstandingUnit = LastBalance;
                            //20230308, sandi, RDN-899, begin
                            apiMsgResponse.Data[i].OutstandingAmount = OutstandingAmount;
                            //20230308, sandi, RDN-899, end
                        }
                        else
                        {
                            LastBalance = getLastBalace(apiMsgResponse.Data[i].ClientId.ToString().Trim(), paramIn.UserNIK, paramIn.TransactionMessageGUID);
                            apiMsgResponse.Data[i].OutstandingUnit = LastBalance;
                            //20230308, sandi, RDN-899, begin
                            apiMsgResponse.Data[i].OutstandingAmount = LastBalance * apiMsgResponse.Data[i].NAV;
                            //20230308, sandi, RDN-899, end
                        }
                        LastBalance = 0;
                        //20230308, sandi, RDN-899, begin
                        OutstandingAmount = 0;
                        //20230308, sandi, RDN-899, end
                    }

                }
                else
                {
                    apiMsgResponse.IsSuccess = false;
                    apiMsgResponse.ErrorCode = "4002";
                    apiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return apiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        #endregion PortofolioNasabah

        #region InquiryCustomerProfile
        public ApiMessage<InquiryCustomerProfileRes> InquiryCustomerProfile(ApiMessage<InquiryCustomerProfileReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<InquiryCustomerProfileRes> apiMsgResponse = new ApiMessage<InquiryCustomerProfileRes>();
            List<InquiryCustomerProfileRes> response = new List<InquiryCustomerProfileRes>();

            apiMsgResponse.copyHeaderForReply(paramIn);

            #region Query
            string sqlCommand = @"
                    select * 
                    from ReksaMasterNasabah_TM
                        where CIFNo = @pcCIFNo
                ";
            #endregion
            //20250707, gio, RDN-1254, begin
            #region Query Pengecekan US Citizen
            string sqlCheckUSCitizen = @"
                    declare @cCIFNoChar19					char(19)  , @bUSCitizen bit

                    set @cCIFNoChar19 = right('0000000000000000000' + ltrim(rtrim(@pcCIFNo)),19)    
                    set  @bUSCitizen = 0            
                    if exists (select top 1 1 from CFMAST_v with (nolock)              
                    where CFCIF = @cCIFNoChar19 and CFCITZ = '411' )  
                    begin                  
                        set  @bUSCitizen = 1
                    end                  
                    select @bUSCitizen 'bUSCitizen'
                ";
            #endregion
            DataSet dsDataUSCitizen = new DataSet();
            //20250707, gio, RDN-1254, end

            string errMsg = "";
            DataSet dsResult = new DataSet();

            try
            {
                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pcCIFNo", paramIn.Data.CIFNo);

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                    throw new Exception("Data Account for mutual fund CIF : " + paramIn.Data.CIFNo + " not found !");

                response = JsonConvert.DeserializeObject<List<InquiryCustomerProfileRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                //20250707, gio, RDN-1254, begin
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCheckUSCitizen, ref sqlParam, out dsDataUSCitizen, out errMsg))
                {
                    if (dsDataUSCitizen.Tables[0].Rows.Count > 0)
                    {
                        response[0].USCitizen = Convert.ToBoolean(dsDataUSCitizen.Tables[0].Rows[0]["bUSCitizen"].ToString());
                    }
                }
                //20250707, gio, RDN-1254, end

                apiMsgResponse.Data = response[0];
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }
        #endregion InquiryCustomerProfile

        #region TotalProceed
        public ApiMessage<InquiryRateRes> InquiryRate(ApiMessage<InquiryRateReq> paramIn)
        {
            ApiMessage<InquiryRateRes> apiMsgResponse = new ApiMessage<InquiryRateRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            string strApiName = "InquiryRate";
            string methodApiUrl = this._strApiReksaParameter + strApiName;
            Console.WriteLine(System.DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + "-" + methodApiUrl);

            //string errMsg = "";

            try
            {
                RestWSClient<ApiMessage<InquiryRateRes>> restAPI = new RestWSClient<ApiMessage<InquiryRateRes>>(this._ignoreSSL);
                apiMsgResponse = restAPI.invokeRESTServicePost(methodApiUrl, paramIn);

                if (!apiMsgResponse.IsSuccess)
                    throw new Exception(apiMsgResponse.ErrorDescription);
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }

        public ApiMessage<InquiryProductRes> InquiryProduct(ApiMessage<InquiryProductReq> paramIn)
        {
            ApiMessage<List<InquiryProductRes>> tempResult = new ApiMessage<List<InquiryProductRes>>();
            ApiMessage<InquiryProductRes> apiMsgResponse = new ApiMessage<InquiryProductRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);

            string strApiName = "InquiryProduct";
            string methodApiUrl = this._strApiReksaParameter + strApiName;
            Console.WriteLine(System.DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + "-" + methodApiUrl);

            //string errMsg = "";

            try
            {
                RestWSClient<ApiMessage<List<InquiryProductRes>>> restAPI = new RestWSClient<ApiMessage<List<InquiryProductRes>>>(this._ignoreSSL);
                tempResult = restAPI.invokeRESTServicePost(methodApiUrl, paramIn);

                if (!tempResult.IsSuccess)
                    throw new Exception(tempResult.ErrorDescription);

                apiMsgResponse.Data = tempResult.Data[0];
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }

        public ApiMessage<CalculateMutualFundTotalProceedRes> TotalProceed(ApiMessage<CalculateMutualFundTotalProceedReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<CalculateMutualFundTotalProceedRes> apiMsgResponse = new ApiMessage<CalculateMutualFundTotalProceedRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);
            List<CalculateMutualFundTotalProceedRes> response = new List<CalculateMutualFundTotalProceedRes>();

            apiMsgResponse.copyHeaderForReply(paramIn);

            int fullAmount = 0;
            if (paramIn.Data.FullAmount == true)
                fullAmount = 1;

            #region Query
            string sqlCommand = @"
                    declare @nTotalFee    decimal(25,13)
                    , @nNAVValue      decimal(25, 13)
                    , @nNAVValueTarget  decimal(25, 13)
		            , @dNAVValueDate datetime
                    , @dNAVValueDateTarget  datetime

            set @nTotalFee = REPLACE(@pnTotalFee, ',', '.')

            --create temp table for result
            declare @temp_TotalProceed table(
              EstimasiJumlahUnit decimal(25, 13)-- subs dari nominal ke unit
            , EstimasiNilaiReksadana decimal(25, 13)-- redemp / swc dari unit ke nominal
            , NominalInvestasi decimal(25, 13)
            , NominalFee decimal(25, 13)
            , TotalProceed decimal(25, 13)
            )

            select top 1 @nNAVValue = NAV, @dNAVValueDate = ValueDate
            from ReksaNAVParam_TH with(Nolock)
            where ProdId = @pnProdId
            order by ValueDate desc

            select top 1 @nNAVValueTarget = NAV, @dNAVValueDateTarget = ValueDate
            from ReksaNAVParam_TH with(Nolock)
            where ProdId = @pnProdIdTarget
            order by ValueDate desc

            if (@pcTipeTrx in ('SUBS', 'SUBSRDB'))
                    begin
                    --full amount
                if (" + fullAmount + @" = 1)
                begin
                    --ya
                    insert @temp_TotalProceed(EstimasiJumlahUnit, NominalInvestasi, NominalFee, TotalProceed)
                    select dbo.fnReksaSetRounding(@pnProdId, 2, cast((@pnNominal / @nNAVValue) as decimal(25, 13)))
                            , @pnNominal
                            , @nTotalFee
                            , @pnNominal + @nTotalFee
                end
                else
                begin
                    --tidak
                    insert @temp_TotalProceed(EstimasiJumlahUnit, NominalInvestasi, NominalFee, TotalProceed)
                    select dbo.fnReksaSetRounding(@pnProdId, 2, cast(((@pnNominal - @nTotalFee) / @nNAVValue) as decimal(25, 13)))
                            , @pnNominal
                            , @nTotalFee
                            , @pnNominal
                end
                goto HYPERJUMP
            end

            if (@pcTipeTrx = 'REDEMP')
                begin
                    insert @temp_TotalProceed(EstimasiNilaiReksadana, NominalFee, TotalProceed)
                select dbo.fnReksaSetRounding(@pnProdId, 2, cast((@pnUnitTrx * @nNAVValue) as decimal(25, 13)))
                        , @nTotalFee as NominalFee
                        , dbo.fnReksaSetRounding(@pnProdId, 2, cast((@pnUnitTrx * @nNAVValue) as decimal(25, 13))) - @nTotalFee
                goto HYPERJUMP
            end

            if (@pcTipeTrx in ('SWCNONRDB', 'SWCRDB'))
                begin
                    insert @temp_TotalProceed(EstimasiNilaiReksadana, NominalFee, EstimasiJumlahUnit, TotalProceed)
                select dbo.fnReksaSetRounding(@pnProdId, 3, cast((@pnUnitTrx * @nNAVValue) as decimal(25, 13)))
						, @nTotalFee
						, dbo.fnReksaSetRounding(@pnProdId, 2, cast(((@pnUnitTrx * @nNAVValue) / (@nNAVValueTarget)) as decimal(25, 13)))
                        , dbo.fnReksaSetRounding(@pnProdId, 3, cast((@pnUnitTrx * @nNAVValue) as decimal(25, 13))) - @nTotalFee
                goto HYPERJUMP
            end

            HYPERJUMP:
            select*
            from @temp_TotalProceed
                ";
            #endregion

            string errMsg = "";
            DataSet dsResult = new DataSet();

            try
            {
                SqlParameter[] sqlParam = new SqlParameter[8];
                sqlParam[0] = new SqlParameter("@pcCIFNo", paramIn.Data.CIFNo);
                sqlParam[1] = new SqlParameter("@pcTipeTrx", paramIn.Data.TipeTrx);
                sqlParam[2] = new SqlParameter("@pnProdId", paramIn.Data.ProdId);
                sqlParam[3] = new SqlParameter("@pnProdIdTarget", paramIn.Data.ProdIdTarget);
                sqlParam[4] = new SqlParameter("@pnNominal", paramIn.Data.Nominal);
                sqlParam[5] = new SqlParameter("@pbFullAmout", paramIn.Data.FullAmount);
                sqlParam[6] = new SqlParameter("@pnUnitTrx", paramIn.Data.UnitTrx);
                sqlParam[7] = new SqlParameter("@pnTotalFee", paramIn.Data.Fee);


                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                    throw new Exception("Data Product Fee not found !");

                response = JsonConvert.DeserializeObject<List<CalculateMutualFundTotalProceedRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                apiMsgResponse.Data = response[0];
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }
        #endregion TotalProceed
        
        #region EBWInvestmentInquiry
        public ApiMessage<EBWInvestmentInquiryRes> getEBWInvestmentInquiry(ApiMessage<EBWInvestmentInquiryReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            EBWInvestmentInquiryRes responseClass = new EBWInvestmentInquiryRes();
            List<EBWInvestmentMainData> listMainData = new List<EBWInvestmentMainData>();
            ApiMessage<EBWInvestmentInquiryRes> apiMsgResponse = new ApiMessage<EBWInvestmentInquiryRes>();
            apiMsgResponse.copyHeaderForReply(paramIn);
            List<SQLSPParameter> param;
            int recordCount = 0;
            string providerErrCode = "", errResponse = "";
            string strSPName = "EBWInvestmentInquiry#RDN", errMsg = "";
            DataSet dsResult = new DataSet();

            apiMsgResponse.copyHeaderForReply(paramIn);
            
            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@pcCIFNumber", paramIn.Data.CIFNumber, 19));
                param.Add(new SQLSPParameter("@pnRecCount", paramIn.Data.RecCount, 100, ParamDirection.OUTPUT));
                param.Add(new SQLSPParameter("@pcErrMessage", paramIn.Data.ErrMessage, 100, ParamDirection.OUTPUT));
                param.Add(new SQLSPParameter("@pcErrResponse", paramIn.Data.ErrResponse, ParamDirection.OUTPUT));
                param.Add(new SQLSPParameter("@pcProviderErrCode", paramIn.Data.ProviderErrCodes, 5, ParamDirection.OUTPUT));

                if (clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                {
                    if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                        throw new Exception("Data not found !");

                    listMainData = JsonConvert.DeserializeObject<List<EBWInvestmentMainData>>(JsonConvert.SerializeObject(dsResult.Tables[0]));

                    var count = listMainData.Count();

                    for (int i = 0; i < count; i++)
                    {
                        listMainData[i].XMLDataExt = listMainData[i].XMLDataExt.Replace("\n\t\t\t\t\t\t\t", "").Replace("\n\t\t\t\t\t\t", "");
                    }

                    /*** MAIN DATA ***/
                    responseClass.MainData = listMainData;

                    /*** ADDITIONAL DATA ***/
                    responseClass.AdditionalData = new EBWInvestmentAdditionalData();

                    errMsg = (param[2] == null ? "" : param[2].ParameterValue.ToString());
                    errResponse = (param[3] == null ? "" : param[3].ParameterValue.ToString());
                    providerErrCode = (param[4] == null ? "" : param[4].ParameterValue.ToString());

                    if (!errMsg.Equals(""))
                        throw new Exception(errMsg);

                    if (param[1] != null && int.TryParse(param[1].ParameterValue.ToString(), out recordCount))
                        responseClass.AdditionalData.RecordCount = recordCount;

                    responseClass.AdditionalData.ErrMessage = errMsg;
                    responseClass.AdditionalData.ErrResponse = errResponse;
                    responseClass.AdditionalData.ProviderErrCode = providerErrCode;

                    apiMsgResponse.Data = responseClass;
                    apiMsgResponse.IsSuccess = true;
                }
                else
                    throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = providerErrCode;
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return apiMsgResponse;
        }
        #endregion EBWInvestmentInquiry

        #region get TranId from RefId
        public int getTranId(string RefId)
        {
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            try
            {
                string sqlCommand = @"
                    declare @refId varchar(20)
                    set @refId = '" + RefId + @"'

                    select a.TranId
                    from ReksaCurrentTransaction_TM a
                    join ReksaTransaction_TT b
	                    on b.TranId = a.TranId
                    where b.RefID = @refId
                    UNION 
                    select a.TranId
                    from ReksaCurrentTransaction_TH a
                    join ReksaTransaction_TH b
	                    on b.TranId = a.TranId
                    where RefID = @refId

                ";

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                    {
                        return -1;
                    }

                    return Convert.ToInt32(dsOut.Tables[0].Rows[0]["TranId"]);
                }

                else
                {
                    return -1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public List<int> getTranIdAuthorize(string RefId)
        {
            List<int> tranIdValue = new List<int>();
            tranIdValue.Add(-1);
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            try
            {
                string sqlCommand = @"
                    declare @refId varchar(20)
                    set @refId = '" + RefId + @"'
                    
            --20230324, Lita, RDN-949, fix auth RDB Future, begin
                    if not exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM WHERE RefID = @refId and IsFutureRDB = 1)
                    begin
            --20230324, Lita, RDN-949, fix auth RDB Future, end
                    select TranId
                    from ReksaTransaction_TT
                    where RefID = @refId
                    UNION 
                    select TranId
                    from ReksaTransaction_TH
                    where RefID = @refId
            --20230324, Lita, RDN-949, fix auth RDB Future, begin
                    end
                    else
                    begin
                        select 0 'TranId'
                    end
            --20230324, Lita, RDN-949, fix auth RDB Future, end
                ";

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                    {
                        return tranIdValue;
                    }

                    tranIdValue.Clear();
                    for(int i = 0; i< dsOut.Tables[0].Rows.Count; i++)
                    {
                        tranIdValue.Add(int.Parse(dsOut.Tables[0].Rows[i]["TranId"].ToString()));
                    }
                    return tranIdValue;
                }

                else
                {
                    return tranIdValue;
                }
            }
            catch (Exception)
            {
                return tranIdValue;
            }
        }

        public List<string> getTranCodeAuthorize(string RefId)
        {
            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            List<string> listTranCode = new List<string>();
            listTranCode.Add("-1");

            try
            {
                string sqlCommand = @"
                    declare @refId varchar(20)
                    set @refId = '" + RefId + @"'
                    
                    --20230324, Lita, RDN-949, fix auth RDB Future, begin
                    if not exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM WHERE RefID = @refId)
                    begin
                    --20230324, Lita, RDN-949, fix auth RDB Future, end
                        select TranCode
                        from ReksaTransaction_TT
                        where RefID = @refId
                        UNION 
                        select TranCode
                        from ReksaTransaction_TH
                        where RefID = @refId
--20230324, Lita, RDN-949, fix auth RDB Future, begin
                    end
                    else
                    begin
                        select TranCode from dbo.ReksaRegulerSubscriptionClient_TM WHERE RefID = @refId
                    end
--20230324, Lita, RDN-949, fix auth RDB Future, end
                    
                ";

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                    {
                        return listTranCode;
                    }

                    listTranCode.Clear();
                    for(int i=0; i<dsOut.Tables[0].Rows.Count; i++)
                    {
                        listTranCode.Add(dsOut.Tables[0].Rows[i]["TranCode"].ToString());
                    }
                    return listTranCode;
                }

                else
                {
                    return listTranCode;
                }
            }
            catch (Exception)
            {
                return listTranCode;
            }
        }

        public List<int> getTranIdAuthorizeSwitching(string RefId)
        {
            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            List<int> response = new List<int>();
            response.Add(-1);

            try
            {
                string sqlCommand = @"
                    
                    select TranId 
                    from [dbo].[ReksaSwitchingTransaction_TM]
                    where RefID ='" + RefId + @"'

                ";

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                    {
                        return response;
                    }

                    response.Clear();
                    for(int i=0; i<dsOut.Tables[0].Rows.Count; i++)
                    {
                        response.Add(int.Parse(dsOut.Tables[0].Rows[i]["TranId"].ToString()));
                    }
                    return response;
                }

                else
                {
                    return response;
                }
            }
            catch (Exception)
            {
                return response;
            }
        }
        #endregion get TranId from RefId

        #region SearchExecute
        public ApiMessage<SearchExecuteRes> SearchExecute(ApiMessage<SearchExecuteReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> param;
            SearchExecuteRes response = new SearchExecuteRes();
            List<ValidasiSearchExecute> listMainData = new List<ValidasiSearchExecute>();
            ApiMessage<SearchExecuteRes> apiMsgResponse = new ApiMessage<SearchExecuteRes>();

            DateTime datenow = DateTime.Now;

            #region validasi
            //Validasi 
            //NIK
            if (paramIn.UserNIK.ToString() == "")
            {
                apiMsgResponse.IsSuccess = false;

            }
            //GUID
            if (paramIn.MessageGUID == "")
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "Guid tidak boleh kosong";
                return apiMsgResponse;
            }

            //OfficeId
            if (paramIn.Data.Office == "")
            {
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorDescription = "Office Id tidak boleh kosong";
                return apiMsgResponse;
            }

            else
            {
                if (ValidasiOffice(paramIn.Data.Office) != "1")
                {
                    apiMsgResponse.IsSuccess = false;
                    apiMsgResponse.ErrorDescription = "Office Id tidak terdaftar";
                    return apiMsgResponse;
                }
            }

            //Col1
            if (paramIn.Data.Col1 == null)
            {
                paramIn.Data.Col1 = "";
            }
            //Col2
            if (paramIn.Data.Col2 == null)
            {
                paramIn.Data.Col2 = "";
            }
            #endregion validasi

            string strSPName = "SearchExecute", errMsg = ""; //, Criteria = "";
            DataSet dsResult = new DataSet();
            DataSet dsParamOut = new DataSet();

            paramIn.Data.ErrMsg = "";

            try
            {
                param = new List<SQLSPParameter>();
                param.Add(new SQLSPParameter("@cGuid", paramIn.MessageGUID));
                param.Add(new SQLSPParameter("@iNik", paramIn.UserNIK));
                param.Add(new SQLSPParameter("@cOffice", paramIn.Data.Office));
                param.Add(new SQLSPParameter("@iSearchId", paramIn.Data.SearchId));
                param.Add(new SQLSPParameter("@cCol1", paramIn.Data.Col1));
                param.Add(new SQLSPParameter("@cCol2", paramIn.Data.Col2));
                param.Add(new SQLSPParameter("@bValidate", paramIn.Data.Validate));
                param.Add(new SQLSPParameter("@cCriteria", paramIn.Data.Criteria));
                param.Add(new SQLSPParameter("@cSearchDesc", paramIn.Data.SearchDesc));
                param.Add(new SQLSPParameter("@pcErrMsg", paramIn.Data.ErrMsg, 200, (SQLSPParameter.ParamDirection)ParamDirection.OUTPUT));//10

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, strSPName, ref param, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                    throw new Exception("Data not found !");

                listMainData = JsonConvert.DeserializeObject<List<ValidasiSearchExecute>>(JsonConvert.SerializeObject(dsResult.Tables[0]));

                var count = listMainData.Count();

                #region MainData

                response.MainData = listMainData;

                #endregion

                #region AdditionalData

                response.AdditionalData = new ValidasiSearchExecuteAdditionalData();

                #endregion

                #region Cek Error Di SP

                errMsg = (param[9] == null ? "" : param[9].ParameterValue.ToString());

                if (!errMsg.Equals(""))
                    throw new Exception(errMsg);

                #endregion

                response.AdditionalData.ErrMessage = errMsg;

                apiMsgResponse.Data = response;
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        #endregion

        #region AccountMaintenance
        public ApiMessage AccountMaintenance(ApiMessage<AccountMaintenanceRequest> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage apiMsgResponse = new ApiMessage();

            DataSet dsResult = null;
            string strErrMsg = "";

            try
            {
                #region Query
                string sqlCommand = @"

                    Declare @pcRelationAccountNameNew	varchar(20)
                     		, @pcRelationAccountName	varchar(20)

                    set @pcRelationAccountNameNew = ''
                    set @pcRelationAccountName = '' 

                    --cek rekening ada atau tidak
                    select @pcRelationAccountName = SNAME   
                    from SQL_SIBS.dbo.DDMAST  
                    where ACCTNO = @pcRelationAccount 

                    if exists (select top 1 1 from SQL_SIBS.dbo.DDMAST where ACCTNO = @pcRelationAccount AND SCCODE like '%MC%')
                    begin
	                    set @bIsMC = 1
                    end

                    --untuk ambil nama rekening baru 
                    set @pcRelationAccountNameNew = ''
                    select @pcRelationAccountNameNew = CFAAL1 from CFALTN where CFAACT = @pcRelationAccount

                    if isnull(@pcRelationAccountNameNew, '') = ''
                    begin
	                    select @pcRelationAccountNameNew = CFAAL1 
	                    from CFALTNNew_v
	                    where CFAACT = @pcRelationAccount
                    end

                    if isnull(@pcRelationAccountNameNew, '') = ''
	                    set @pcRelationAccountNameNew = @pcRelationAccountName

                    -- jika subs
                    if(@nTrantype IN (1, 8))
                    begin
                        if @pcProductCurrency = 'IDR' and @bIsMC = 0
                        begin   
                            update dbo.ReksaMasterNasabah_TM
                            set NISPAccountId = @pcRelationAccount,
			                    NISPAccountName = @pcRelationAccountNameNew
                            where CIFNo = @pcCIFKey 
                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdMC,'') != '')
                            begin
			                    if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
			                    where CIFNo = @pcCIFKey and b.ProdCCY <> 'IDR' and a.CIFStatus= 'A')
				                    begin
					                    if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM
					                    where CIFNo = @pcCIFKey and isnull(NISPAccountIdUSD,'') = '')
						                    begin
						                    update dbo.ReksaMasterNasabah_TM
						                    set NISPAccountIdUSD = NISPAccountIdMC ,
						                    NISPAccountNameUSD = NISPAccountNameMC
						                    where CIFNo = @pcCIFKey 
						                    end
				                    end
                                update dbo.ReksaMasterNasabah_TM
                                set NISPAccountIdMC = null,
                                    NISPAccountNameMC = null
                                where CIFNo = @pcCIFKey 
                            end                  
                        end
                        else if @pcProductCurrency = 'USD' and @bIsMC = 0
                        begin
                            update dbo.ReksaMasterNasabah_TM
                            set NISPAccountIdUSD = @pcRelationAccount,
			                    NISPAccountNameUSD = @pcRelationAccountNameNew
                            where CIFNo = @pcCIFKey

		                    if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
		                    where CIFNo = @pcCIFKey and b.ProdCCY = 'IDR' and a.CIFStatus= 'A')
			                    begin
				                    if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM
				                    where CIFNo = @pcCIFKey and isnull(NISPAccountId,'') = '')
					                    begin
					                    update dbo.ReksaMasterNasabah_TM
					                    set NISPAccountId = NISPAccountIdMC ,
					                    NISPAccountName = NISPAccountNameMC
					                    where CIFNo = @pcCIFKey 
					                    end
			                    end

                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdMC,'') != '')
                            begin
                                update dbo.ReksaMasterNasabah_TM
			                    set NISPAccountIdMC = null,
                                    NISPAccountNameMC = null
                                where CIFNo = @pcCIFKey 
                            end      
                        end
                        else if @bIsMC = 1
                        begin
                            update dbo.ReksaMasterNasabah_TM
                            set NISPAccountIdMC = @pcRelationAccount,
			                    NISPAccountNameMC = @pcRelationAccountNameNew
                            where CIFNo = @pcCIFKey 
                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
                            where CIFNo = @pcCIFKey and isnull(NISPAccountId,'') != '')
                            begin
                                update dbo.ReksaMasterNasabah_TM
                                set NISPAccountId = null,
                                    NISPAccountName = null
                             where CIFNo = @pcCIFKey 
                            end
                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdUSD,'') != '')
                            begin
                                update dbo.ReksaMasterNasabah_TM
                                set NISPAccountIdUSD = null,
                                    NISPAccountNameUSD = null
                                where CIFNo = @pcCIFKey 
                            end     
                        end
                    end
                ";
                #endregion

                SqlParameter[] sqlParam = new SqlParameter[5];
                sqlParam[0] = new SqlParameter("@pcProductCurrency", paramIn.Data.ProductCurrency);
                sqlParam[1] = new SqlParameter("@bIsMC", "0");
                sqlParam[2] = new SqlParameter("@pcRelationAccount", paramIn.Data.RelationAccount);
                sqlParam[3] = new SqlParameter("@pcCIFKey", paramIn.Data.CIFKey);
                sqlParam[4] = new SqlParameter("@nTrantype", paramIn.Data.Trantype);


                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out strErrMsg))
                {
                    apiMsgResponse.IsSuccess = true;

                }
                else
                {
                    apiMsgResponse.IsSuccess = false;
                    apiMsgResponse.ErrorCode = "4002";
                    apiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return apiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                this._apiLogger.logError(this, new StackTrace(), "Request => " + paramIn.getJSONString() + "; Error = > " + ex.Message, paramIn.TransactionMessageGUID);
                apiMsgResponse.IsSuccess = false;
                apiMsgResponse.ErrorCode = "500";
                apiMsgResponse.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                apiMsgResponse.MessageDateTime = DateTime.Now;
            }


            return apiMsgResponse;
        }
        #endregion AccountMaintenance

        public ApiMessage<object> CobaSelect()
        {
            ApiMessage<object> response = new ApiMessage<object>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            DataSet dsParamOut = new DataSet();
            SqlParameter[] dbPar = new SqlParameter[0];

            try
            {
                string sqlCommand = @"
                    SELECT TOP 10 * FROM dbo.ReksaProduct_TM
                ";

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                    response.Data = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dsOut));
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorDescription = ex.Message;
            }

            return response;
        }

        //20220614, Andhika J, VELOWEB-1961, begin
        #region logException
        public void LogErrorException(Exception exception, string MessageError, out bool IsSuccess, out string ErrorMsg)
        {
            _apiLogger.logError(this, new StackTrace(false), MessageError + " " + exception.Message);
            IsSuccess = false;
            ErrorMsg = exception.Message;
        }
        #endregion logException
        #region inquiryaccount 
        public ApiMessage<RsInquiryAccount> GetCustomerDetailInfoByAcctNo(ApiMessage<RqInquiryAccount> inModel)
        {
            RqInquiryAccount requestMsg = new RqInquiryAccount();
            ApiMessage<RsInquiryAccount> responseMsg = new ApiMessage<RsInquiryAccount>();
            responseMsg.copyHeaderForReply(inModel);

            string methodApiUrl = this._url_apiACCInquiryAccountDetail;
            responseMsg.copyHeaderForReply(inModel);

            try
            {
                requestMsg.GUID = inModel.TransactionMessageGUID;
                requestMsg.Branch = inModel.UserBranch;
                requestMsg.AccountNo = inModel.Data.AccountNo;
                requestMsg.Module = inModel.ModuleName;
                requestMsg.MoreIndicator = "N";
                requestMsg.NIK = inModel.UserNIK;

                RestWSClient<ApiMessage<RsInquiryAccount>> restAPI = new RestWSClient<ApiMessage<RsInquiryAccount>>(this._ignoreSSL);
                responseMsg = restAPI.invokeRESTServicePost(methodApiUrl, requestMsg);
                if (!responseMsg.IsSuccess)
                    throw new Exception(responseMsg.ErrorDescription);

            }
            catch (Exception ex)
            {
                //this._iApiLogger.logError(this, new StackTrace(), "Request => " + inModel.getJSONString() + "; Error = > " + ex.Message, inModel.TransactionMessageGUID);
                responseMsg.IsSuccess = false;
                responseMsg.ErrorCode = "500";
                responseMsg.ErrorDescription = "Inquiry Account : " + ex.Message;
            }
            finally
            {
                responseMsg.MessageDateTime = DateTime.Now;
            }

            return responseMsg;

        }
        #endregion inquiryaccount
        #region checksubs
        public ApiMessage<ReksaEBWUTCheckingSubsRes> ReksaEBWUTCheckingSubs(ApiMessage<ReksaEBWUTCheckingSubsReq> paramIn)
        {
            ApiMessage<ReksaEBWUTCheckingSubsRes> msgResponse = new ApiMessage<ReksaEBWUTCheckingSubsRes>();
            string errMsg = "", XMLDataExt ="";
            string ErrCode = "", ErrDesc = "", strChannel = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            string spName = "ReksaEBWUTCheckingSubs";
            //20230324, Andhika J, VELOWEB-2313, begin
            if (paramIn.ModuleName == "VELO")
            {
                strChannel = "VL";
            }
            //20230324, Andhika J, VELOWEB-2313, end
            msgResponse.copyHeaderForReply(paramIn);
            if (paramIn.Data.XMLDataExt.ToString() != "")
            {
                string Sxml = "<ROOT><RS>";
                string Exml = "</RS></ROOT>";

                XMLDataExt = Sxml
                    + " <RDBFreqDebetMethod>" + paramIn.Data.XMLDataExt.RDBFreqDebetMethod + "</RDBFreqDebetMethod>"
                    + " <RDBFreqDebet>" + paramIn.Data.XMLDataExt.RDBFreqDebet + "</RDBFreqDebet>"
                    + " <RDBDebetDate>" + paramIn.Data.XMLDataExt.RDBDebetDate + "</RDBDebetDate>"
                    //20221021, Lita, RDN-865, begin
                    + " <LangCode>" + paramIn.Data.LangCode + "</LangCode>"
                    + " <Channel>" + paramIn.ModuleName + "</Channel>"
                    //20221021, Lita, RDN-865, end
                    + Exml;
            }
            //20221021, Lita, RDN-865, begin
            else
            {
                string Sxml = "<ROOT><RS>";
                string Exml = "</RS></ROOT>";

                XMLDataExt = Sxml
                    + " <LangCode>" + paramIn.Data.LangCode + "</LangCode>"
                    + Exml;
            }
            //20221021, Lita, RDN-865, end

            try
            {
                if (paramIn.Data.CIFKey.ToString() != null)
                {
                    dbPar = new List<SQLSPParameter>();
                    dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo" , paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                    dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                    dbPar.Add(new SQLSPParameter("@pcClientCode", paramIn.Data.ClientCode));
                    dbPar.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientId));
                    dbPar.Add(new SQLSPParameter("@pnProductId", paramIn.Data.ProductId));
                    dbPar.Add(new SQLSPParameter("@pcProductCode", paramIn.Data.ProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccount", paramIn.Data.RelationAccount));
                    dbPar.Add(new SQLSPParameter("@pcProductCurrency", paramIn.Data.ProductCurrency));
                    dbPar.Add(new SQLSPParameter("@pnUnitBalance", paramIn.Data.UnitBalance));
                    dbPar.Add(new SQLSPParameter("@pnSubsAmount", paramIn.Data.SubsAmount));
                    dbPar.Add(new SQLSPParameter("@pnFeeType", paramIn.Data.FeeType));
                    dbPar.Add(new SQLSPParameter("@pnSubsFee", paramIn.Data.SubsFee));
                    dbPar.Add(new SQLSPParameter("@pnMinSubs", paramIn.Data.MinSubs));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountProductCode", paramIn.Data.RelationAccountProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountCurr", paramIn.Data.RelationAccountCurr));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountStatus", paramIn.Data.RelationAccountStatus));
                    dbPar.Add(new SQLSPParameter("@pnRelationAccountBalance", paramIn.Data.RelationAccountBalance));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountMC", paramIn.Data.RelationAccountMC));
                    dbPar.Add(new SQLSPParameter("@pcProductRiskProfile", paramIn.Data.ProductRiskProfile));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountName", paramIn.Data.RelationAccountName));
                    dbPar.Add(new SQLSPParameter("@pcCheckingType", paramIn.Data.CheckingType));
                    dbPar.Add(new SQLSPParameter("@pbIsRDB", paramIn.Data.IsRDB));
                    dbPar.Add(new SQLSPParameter("@pcRDBAutoSubsUntil", paramIn.Data.RDBAutoSubsUntil));
                    dbPar.Add(new SQLSPParameter("@pnRDBTenor", paramIn.Data.RDBTenor));
                    dbPar.Add(new SQLSPParameter("@pcRDBMaturityDate", paramIn.Data.RDBMaturityDate));
                    dbPar.Add(new SQLSPParameter("@pdRDBMonthlySubs", paramIn.Data.RDBMonthlySubs));
                    dbPar.Add(new SQLSPParameter("@pbRDBInsuranceBit", paramIn.Data.RDBInsuranceBit));
                    dbPar.Add(new SQLSPParameter("@pbRDBAutoRedeemBit", paramIn.Data.RDBAutoRedeemBit));
                    dbPar.Add(new SQLSPParameter("@pnRDBInsuranceProductId", paramIn.Data.RDBInsuranceProductId));
                    dbPar.Add(new SQLSPParameter("@pcRDBInsuranceVendor", paramIn.Data.RDBInsuranceVendor));
                    dbPar.Add(new SQLSPParameter("@pcRDBInsuranceVendorName", paramIn.Data.RDBInsuranceVendorName));
                    dbPar.Add(new SQLSPParameter("@pcXMLDataExt", XMLDataExt));
                    dbPar.Add(new SQLSPParameter("@pcErrMessage", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref dbPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                    {
                        ErrDesc = dbPar[33].ParameterValue.ToString();
                        ErrCode = dbPar[34].ParameterValue.ToString();
                        if (ErrCode == "01000")
                        {
                            msgResponse.IsSuccess = true;
                            msgResponse.ErrorCode = ErrCode;
                            msgResponse.ErrorDescription = ErrDesc;
                        }
                        else
                        {
                            throw new Exception(ErrDesc);
                        }
                    }
                    else
                    {
                        
                        msgResponse.Data = new ReksaEBWUTCheckingSubsRes();
                        msgResponse.ErrorDescription = dbPar[33].ParameterValue.ToString();
                        msgResponse.ErrorCode = dbPar[34].ParameterValue.ToString();
                        msgResponse.Data.SubsFeeAmount = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["SubsFeeAmount"]);
                        msgResponse.Data.SubsFeeCurr = Convert.ToString(dsDataOut.Tables[0].Rows[0]["SubsFeeCurr"]);
                        msgResponse.Data.SubsAmount = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["SubsAmount"]);
                        msgResponse.Data.isRDB = Convert.ToBoolean(dsDataOut.Tables[0].Rows[0]["isRDB"]);
                        msgResponse.Data.RDBTenor = Convert.ToInt32(dsDataOut.Tables[0].Rows[0]["RDBTenor"]);
                        msgResponse.Data.RDBAutoSubsUntil = dsDataOut.Tables[0].Rows[0]["RDBAutoSubsUntil"].ToString();
                        msgResponse.Data.RDBMaturityDate = dsDataOut.Tables[0].Rows[0]["RDBMaturityDate"].ToString();
                        msgResponse.Data.RDBMonthlySubs = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["RDBMonthlySubs"]);
                        msgResponse.Data.RDBInsuranceBit = Convert.ToBoolean(dsDataOut.Tables[0].Rows[0]["RDBInsuranceBit"]);
                        msgResponse.Data.RDBAutoRedeemBit = Convert.ToBoolean(dsDataOut.Tables[0].Rows[0]["RDBAutoRedeemBit"]);
                        msgResponse.Data.RDBInsuranceProductId = Convert.ToInt32(dsDataOut.Tables[0].Rows[0]["RDBInsuranceProductId"]);
                        msgResponse.Data.RDBInsuranceVendor = dsDataOut.Tables[0].Rows[0]["RDBInsuranceVendor"].ToString();
                        msgResponse.Data.RDBInsuranceVendorName = dsDataOut.Tables[0].Rows[0]["RDBInsuranceVendorName"].ToString();
                        //20230324, Andhika J, VELOWEB-2313, begin
                        if (paramIn.ModuleName == "VL")
                        {
                            msgResponse.Data.BlockedAmount = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["BlockedAmount"]);
                            msgResponse.Data.TranAmount = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["TranAmount"]);
                            msgResponse.Data.NAVDate = Convert.ToDateTime(dsDataOut.Tables[0].Rows[0]["NAVDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                            //20230904, Andhika J, VELOWEB-2313, begin
                            msgResponse.Data.SID = dsDataOut.Tables[0].Rows[0]["SID"].ToString();
                            msgResponse.Data.PercentageFee = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["PercentageFee"]);
                            //20230904, Andhika J, VELOWEB-2313, end
                        }
                        //20230324, Andhika J, VELOWEB-2313, end
                        msgResponse.Data.XMLDataExt = new ReksaXMLDataExtRes();
                        msgResponse.Data.XMLDataExt.RDBFreqDebetMethod = paramIn.Data.XMLDataExt.RDBFreqDebetMethod; 
                        msgResponse.Data.XMLDataExt.RDBFreqDebet = paramIn.Data.XMLDataExt.RDBFreqDebet;
                        msgResponse.Data.XMLDataExt.RDBDebetDate = paramIn.Data.XMLDataExt.RDBDebetDate;

                        //20221021, Lita, RDN-865, begin
                        msgResponse.Data.RDBInsCustData = new ReksaRDBInsCustData();
                        if (paramIn.Data.IsRDB == true && paramIn.Data.RDBInsuranceBit == true)
                        {
                            string sXMLExt = dsDataOut.Tables[0].Rows[0]["XMLDataExt"].ToString();

                            int iTenor=0, iUsia = 0;
                            XmlDocument xmlDoc = new XmlDocument();
                            XmlNodeList xmlNodes;

                            xmlDoc.XmlResolver = null;
                            xmlDoc.LoadXml(sXMLExt);
                            xmlNodes = xmlDoc.DocumentElement.SelectNodes("/ROOT/RS");

                            XmlNode xmlNode = xmlNodes[0];

                            msgResponse.Data.RDBInsCustData.NamaPeserta = xmlNode["NamaPeserta"].InnerText.ToString();
                            msgResponse.Data.RDBInsCustData.TanggalLahir = xmlNode["TanggalLahir"].InnerText;
                            int.TryParse(xmlNode["UsiaMasuk"].InnerText.ToString(), out iUsia);
                            msgResponse.Data.RDBInsCustData.UsiaMasuk = iUsia;
                            msgResponse.Data.RDBInsCustData.JenisKelamin = xmlNode["JenisKelamin"].InnerText;
                            int.TryParse(xmlNode["Tenor"].InnerText.ToString(), out iTenor);
                            msgResponse.Data.RDBInsCustData.Tenor = iTenor;
                            msgResponse.Data.RDBInsCustData.PremiDesc = xmlNode["PremiDesc"].InnerText;
                            msgResponse.Data.RDBInsCustData.TargetDana = xmlNode["TargetDana"].InnerText;
                            
                        }
                        else
                            msgResponse.Data.RDBInsCustData = null;
                        //20221021, Lita, RDN-865, end

                        //public ReksaXMLDataExtReq XMLDataExt { get; set; }
                        //msgResponse.Data. = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dsDataOut.Tables[0]));
                    }

                    //msgResponse.Data = new ReksaEBWUTCheckingSubsRes();
                    msgResponse.IsSuccess = true;
                   // msgResponseXML.Data.XMLDataExt 
                }
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                //20221021, Lita, RDN-865, begin
                //msgResponse.ErrorDescription = "ReksaEBWUTCheckingSubs : " + ex.Message;
                msgResponse.ErrorDescription = ex.Message;
                //20221021, Lita, RDN-865, end
                msgResponse.ErrorCode = ErrCode;
                //msgResponse.ErrorDescription = ErrDesc;
            }
            return msgResponse;
        }
        #endregion checksubs
        #region createsubs
        public ApiMessage<ReksaEBWUTProcessSubscriptionRes> ReksaEBWUTProcessSubscription(ApiMessage<ReksaEBWUTProcessSubscriptionReq> paramIn, out string XMLDataExt)
        {
            ApiMessage<ReksaEBWUTProcessSubscriptionRes> msgResponse = new ApiMessage<ReksaEBWUTProcessSubscriptionRes>();
            msgResponse.copyHeaderForReply(paramIn);
            string errMsg = "", _XMLDataExt ="";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            string spName = "ReksaEBWUTProcessSubscription";

            if (paramIn.Data.XMLDataExt != null)
            {
                string Sxml = "<ROOT><RS>";
                string Exml = "</RS></ROOT>";

                _XMLDataExt = Sxml
                    + " <RDBFreqDebetMethod>" + paramIn.Data.XMLDataExt.RDBFreqDebetMethod + "</RDBFreqDebetMethod>"
                    + " <RDBFreqDebet>" + paramIn.Data.XMLDataExt.RDBFreqDebet + "</RDBFreqDebet>"
                    + " <RDBDebetDate>" + paramIn.Data.XMLDataExt.RDBDebetDate + "</RDBDebetDate>"
                    + Exml;
            }
            XMLDataExt = _XMLDataExt;
            try
            {
                if (paramIn.Data.CIFKey.ToString() != null)
                {
                    dbPar = new List<SQLSPParameter>();
                    dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                    dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                    dbPar.Add(new SQLSPParameter("@pcClientCode", paramIn.Data.ClientCode));
                    dbPar.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientId));
                    dbPar.Add(new SQLSPParameter("@pnProductId", paramIn.Data.ProductId));
                    dbPar.Add(new SQLSPParameter("@pcProductCode", paramIn.Data.ProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccount", paramIn.Data.RelationAccount));
                    dbPar.Add(new SQLSPParameter("@pcProductCurrency", paramIn.Data.ProductCurrency));
                    dbPar.Add(new SQLSPParameter("@pnUnitBalance", paramIn.Data.UnitBalance));
                    dbPar.Add(new SQLSPParameter("@pnSubsAmount", paramIn.Data.SubsAmount));
                    dbPar.Add(new SQLSPParameter("@pnFeeType", paramIn.Data.FeeType));
                    dbPar.Add(new SQLSPParameter("@pnSubsFee", paramIn.Data.SubsFee));
                    dbPar.Add(new SQLSPParameter("@pnSubsFeeAmount", paramIn.Data.SubsFeeAmount));
                    dbPar.Add(new SQLSPParameter("@pcSubsFeeCurr", paramIn.Data.SubsFeeCurr));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountCurr", paramIn.Data.RelationAccountCurr));
                    dbPar.Add(new SQLSPParameter("@pnNAV", paramIn.Data.NAV));
                    dbPar.Add(new SQLSPParameter("@pdNAVDate", paramIn.Data.NAVDate));
                    dbPar.Add(new SQLSPParameter("@pcLanguageCode", paramIn.Data.LanguageCode));
                    dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel));
                    dbPar.Add(new SQLSPParameter("@pcReferenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountProductCode", paramIn.Data.RelationAccountProductCode));
                    dbPar.Add(new SQLSPParameter("@pcProductRiskProfile", paramIn.Data.ProductRiskProfile));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountName", paramIn.Data.RelationAccountName));
                    dbPar.Add(new SQLSPParameter("@pbIsRDB", paramIn.Data.IsRDB));
                    dbPar.Add(new SQLSPParameter("@pcRDBAutoSubsUntil", paramIn.Data.RDBAutoSubsUntil));
                    dbPar.Add(new SQLSPParameter("@pnRDBTenor", paramIn.Data.RDBTenor));
                    dbPar.Add(new SQLSPParameter("@pcRDBMaturityDate", paramIn.Data.RDBMaturityDate));
                    dbPar.Add(new SQLSPParameter("@pdRDBMonthlySubs", paramIn.Data.RDBMonthlySubs));
                    dbPar.Add(new SQLSPParameter("@pbRDBAutoRedeemBit", paramIn.Data.RDBAutoRedeemBit));
                    dbPar.Add(new SQLSPParameter("@pbRDBInsuranceBit", paramIn.Data.RDBInsuranceBit));
                    dbPar.Add(new SQLSPParameter("@pnRDBInsuranceProductId", paramIn.Data.RDBInsuranceProductId));
                    dbPar.Add(new SQLSPParameter("@pcRDBInsuranceVendor", paramIn.Data.RDBInsuranceVendor));
                    dbPar.Add(new SQLSPParameter("@pcRDBInsuranceVendorName", paramIn.Data.RDBInsuranceVendorName));
                    dbPar.Add(new SQLSPParameter("@pxRDBInsuranceQAList", paramIn.Data.RDBInsuranceQAList));
                    dbPar.Add(new SQLSPParameter("@pcXMLDataExt", _XMLDataExt));

                    dbPar.Add(new SQLSPParameter("@pcErrMessage", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pnTranId", 0, ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcBlockReason", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcBlokirAmount", 0, ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pbIsLewatCutOff", 0, ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcHoldType", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcTranCode", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pbIsFutureTran", 0, ParamDirection.OUTPUT));
                    //20230809, ahmad.fansyuri, RDN-1021, begin
                    dbPar.Add(new SQLSPParameter("@pcPromoCode", paramIn.Data.PromoCode == null? "":paramIn.Data.PromoCode));
                    //20230809, ahmad.fansyuri, RDN-1021, end

                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref dbPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new ReksaEBWUTProcessSubscriptionRes();
                    if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                    {
                        ErrDesc = dbPar[36].ParameterValue.ToString();
                        ErrCode = dbPar[37].ParameterValue.ToString();
                        if (ErrCode == "01000" && ErrDesc == "")
                        {
                            msgResponse.IsSuccess = true;
                            msgResponse.Data.TranId = Convert.ToInt64(dbPar[38].ParameterValue.ToString());
                            msgResponse.Data.BlockReason = dbPar[39].ParameterValue.ToString();
                            msgResponse.Data.BlokirAmount = Convert.ToDecimal(dbPar[40].ParameterValue.ToString());
                            msgResponse.Data.IsLewatCutOff = Convert.ToBoolean(Convert.ToInt32(dbPar[41].ParameterValue.ToString()));
                            msgResponse.Data.HoldType = dbPar[42].ParameterValue.ToString();
                            msgResponse.Data.TranCode = dbPar[43].ParameterValue.ToString();
                            msgResponse.Data.IsFutureTran = Convert.ToBoolean(Convert.ToInt32(dbPar[44].ParameterValue.ToString()));
                            msgResponse.ErrorCode = ErrCode;
                            msgResponse.ErrorDescription = ErrDesc;
                        }
                        else
                        {
                            throw new Exception(ErrDesc);
                        }
                    }
                    else
                    {
                        msgResponse.ErrorCode = dbPar[36].ParameterValue.ToString();
                        msgResponse.ErrorDescription = dbPar[37].ParameterValue.ToString();
                        msgResponse.Data.TranId = Convert.ToInt64(dbPar[38].ParameterValue.ToString());
                        msgResponse.Data.BlockReason = dbPar[39].ParameterValue.ToString();
                        msgResponse.Data.BlokirAmount = Convert.ToDecimal(dbPar[40].ParameterValue.ToString());
                        msgResponse.Data.IsLewatCutOff = Convert.ToBoolean(Convert.ToInt32(dbPar[41].ParameterValue.ToString()));
                        msgResponse.Data.HoldType = dbPar[42].ParameterValue.ToString();
                        msgResponse.Data.TranCode = dbPar[43].ParameterValue.ToString();
                        msgResponse.Data.IsFutureTran = Convert.ToBoolean(Convert.ToInt32(dbPar[44].ParameterValue.ToString()));
                        //20230324, Andhika J, VELOWEB-2313, begin
                        if (paramIn.Data.Channel == "VL")
                        {
                            msgResponse.Data.TranAmount = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["TranAmount"]);
                            msgResponse.Data.NAVDate = Convert.ToDateTime(dsDataOut.Tables[0].Rows[0]["NAVDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                            msgResponse.Data.RefID = dsDataOut.Tables[0].Rows[0]["RefID"].ToString();
                        }
                        //20230324, Andhika J, VELOWEB-2313, end
                        msgResponse.IsSuccess = true;
                    }

                    //msgResponse.Data = new ReksaEBWUTCheckingSubsRes();
                    msgResponse.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = ErrCode;
                msgResponse.ErrorDescription = "ReksaEBWUTProcessSubscription : " + ex.Message;
            }
            return msgResponse;
        }
        #endregion createsubs
        #region checkredeem
        public ApiMessage<ReksaEBWUTCheckingRedempRes> ReksaEBWUTCheckingRedemp(ApiMessage<ReksaEBWUTCheckingRedempReq> paramIn)
        {
            ApiMessage<ReksaEBWUTCheckingRedempRes> msgResponse = new ApiMessage<ReksaEBWUTCheckingRedempRes>();
            string errMsg = "";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            string spName = "ReksaEBWUTCheckingRedempNEW";
            msgResponse.Data = new ReksaEBWUTCheckingRedempRes();
            msgResponse.copyHeaderForReply(paramIn);
            try
            {
                if (paramIn.Data.CIFKey.ToString() != null)
                {
                    dbPar = new List<SQLSPParameter>();
                    dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                    dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                    dbPar.Add(new SQLSPParameter("@pcClientCode", paramIn.Data.ClientCode));
                    dbPar.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientId));
                    dbPar.Add(new SQLSPParameter("@pnProductId", paramIn.Data.ProductId));
                    dbPar.Add(new SQLSPParameter("@pcProductCode", paramIn.Data.ProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccount", paramIn.Data.RelationAccount));
                    dbPar.Add(new SQLSPParameter("@pcProductCurr", paramIn.Data.ProductCurr));
                    dbPar.Add(new SQLSPParameter("@pnUnitBalance", paramIn.Data.UnitBalance));
                    dbPar.Add(new SQLSPParameter("@pnRedemptionUnit", paramIn.Data.RedemptionUnit));
                    dbPar.Add(new SQLSPParameter("@pnPctRedemptionFee", paramIn.Data.PctRedemptionFee));
                    dbPar.Add(new SQLSPParameter("@pnMinRedemptionUnit", paramIn.Data.MinRedemptionUnit));
                    dbPar.Add(new SQLSPParameter("@pnMinRedemptionNom", paramIn.Data.MinRedemptionNom));
                    dbPar.Add(new SQLSPParameter("@pnMinUnitRemaining", paramIn.Data.MinUnitRemaining));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountProductCode", paramIn.Data.RelationAccountProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountCurr", paramIn.Data.RelationAccountCurr));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountStatus", paramIn.Data.RelationAccountStatus));
                    dbPar.Add(new SQLSPParameter("@pnRelationAccountBalance", paramIn.Data.RelationAccountBalance));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountMC", paramIn.Data.RelationAccountMC));
                    dbPar.Add(new SQLSPParameter("@pcProductRiskProfile", paramIn.Data.ProductRiskProfile));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountName", paramIn.Data.RelationAccountName));
                    dbPar.Add(new SQLSPParameter("@pcCheckingType", paramIn.Data.CheckingType));
                    dbPar.Add(new SQLSPParameter("@cErrMsg", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref dbPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                    {
                        ErrDesc = dbPar[23].ParameterValue.ToString();
                        ErrCode = dbPar[24].ParameterValue.ToString();
                        if (ErrCode == "01000")
                        {
                            //msgResponse.IsSuccess = true;
                            msgResponse.ErrorCode = ErrCode;
                            msgResponse.ErrorDescription = ErrDesc;
                        }
                        else
                        {
                            throw new Exception(ErrDesc);
                        }
                    }
                    else
                    {
                         msgResponse.IsSuccess = true;
                         msgResponse.ErrorCode = dbPar[24].ParameterValue.ToString(); ;
                         msgResponse.ErrorDescription = dbPar[23].ParameterValue.ToString();
                         msgResponse.Data.EstRedempFeeAmount = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["EstRedempFeeAmount"]);
                         msgResponse.Data.RedempFeeCurr = Convert.ToString(dsDataOut.Tables[0].Rows[0]["RedempFeeCurr"]);
                         //20230904, Andhika J, VELOWEB-2313, begin
                         msgResponse.Data.SID = dsDataOut.Tables[0].Rows[0]["SID"].ToString();
                         msgResponse.Data.PercentageFee = Convert.ToDecimal(dsDataOut.Tables[0].Rows[0]["PercentageFee"]);
                         //20230904, Andhika J, VELOWEB-2313, end
                    }

                    //msgResponse.Data = new ReksaEBWUTCheckingSubsRes();
                    msgResponse.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = "ReksaEBWUTCheckingRedemp : " + ex.Message;
                msgResponse.ErrorCode = ErrCode;
                //msgResponse.ErrorDescription = ErrDesc;
            }
            return msgResponse;
        }
        #endregion checkredeem
        #region createredeem
        public ApiMessage<ReksaEBWUTProcessRedemptionRes> ReksaEBWUTProcessRedemption(ApiMessage<ReksaEBWUTProcessRedemptionReq> paramIn)
        {
            ApiMessage<ReksaEBWUTProcessRedemptionRes> msgResponse = new ApiMessage<ReksaEBWUTProcessRedemptionRes>();
            msgResponse.copyHeaderForReply(paramIn);
            string errMsg = "";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();

            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            string spName = "ReksaEBWUTProcessRedemption";

            try
            {
                if (paramIn.Data.CIFKey.ToString() != null)
                {
                    dbPar = new List<SQLSPParameter>();
                    dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                    dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                    dbPar.Add(new SQLSPParameter("@pcClientCode", paramIn.Data.ClientCode));
                    dbPar.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientId));
                    dbPar.Add(new SQLSPParameter("@pnProductId", paramIn.Data.ProductId));
                    dbPar.Add(new SQLSPParameter("@pcProductCode", paramIn.Data.ProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccount", paramIn.Data.RelationAccount));
                    dbPar.Add(new SQLSPParameter("@pcProductCurrency", paramIn.Data.ProductCurrency));
                    dbPar.Add(new SQLSPParameter("@pnUnitBalance", paramIn.Data.UnitBalance));
                    dbPar.Add(new SQLSPParameter("@pnRedemptionUnit", paramIn.Data.RedemptionUnit));
                    dbPar.Add(new SQLSPParameter("@pnMinRedemptionNom", paramIn.Data.MinRedemptionNom));
                    dbPar.Add(new SQLSPParameter("@pnMinRedemptionUnit", paramIn.Data.MinRedemptionUnit));
                    dbPar.Add(new SQLSPParameter("@pnMinUnitRemaining", paramIn.Data.MinUnitRemaining));
                    dbPar.Add(new SQLSPParameter("@pnPctRedemptionFee", paramIn.Data.PctRedemptionFee));
                    dbPar.Add(new SQLSPParameter("@pnRedempFeeAmount", paramIn.Data.RedempFeeAmount));
                    dbPar.Add(new SQLSPParameter("@pcRedempFeeCurr", paramIn.Data.RedempFeeCurr));
                    dbPar.Add(new SQLSPParameter("@pnNAV", paramIn.Data.NAV));
                    dbPar.Add(new SQLSPParameter("@pdNAVDate", paramIn.Data.NAVDate));
                    dbPar.Add(new SQLSPParameter("@pcLanguageCode", paramIn.Data.LanguageCode));
                    dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel));
                    dbPar.Add(new SQLSPParameter("@pcReferenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcProductRiskProfile", paramIn.Data.ProductRiskProfile));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountName", paramIn.Data.RelationAccountName));
                    dbPar.Add(new SQLSPParameter("@cErrMsg", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pdProcessDate", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pnHostSessionId", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcRefID", "", ParamDirection.OUTPUT));

                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref dbPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new ReksaEBWUTProcessRedemptionRes();
                    if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                    {
                        ErrDesc = dbPar[24].ParameterValue.ToString();
                        ErrCode = dbPar[25].ParameterValue.ToString();
                        if (ErrCode == "01000" && ErrDesc == "")
                        {
                            msgResponse.IsSuccess = true; 
                            msgResponse.Data.ProcessDate = Convert.ToDateTime(dbPar[26].ParameterValue.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                            msgResponse.Data.HostSessionId = dbPar[27].ParameterValue.ToString();
                            msgResponse.Data.RefID = dbPar[28].ParameterValue.ToString();
                            msgResponse.Data.XMLScreen = dsDataOut.Tables[0].Rows[0]["XMLScreen"].ToString();
                            msgResponse.ErrorCode = ErrCode;
                            msgResponse.ErrorDescription = ErrDesc;
                        }
                        else
                        {
                            throw new Exception(ErrDesc);
                        }
                    }
                    else
                    {

                        msgResponse.ErrorDescription = dbPar[24].ParameterValue.ToString();
                        msgResponse.ErrorCode = dbPar[25].ParameterValue.ToString();
                        msgResponse.Data.ProcessDate = Convert.ToDateTime(dbPar[26].ParameterValue.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                        msgResponse.Data.HostSessionId = dbPar[27].ParameterValue.ToString();
                        //20230324, Andhika J, VELOWEB-2313, begin
                        msgResponse.Data.RefID = dbPar[28].ParameterValue.ToString();
                        msgResponse.Data.XMLScreen = dsDataOut.Tables[0].Rows[0]["XMLScreen"].ToString();
                        //20230324, Andhika J, VELOWEB-2313, end
                        msgResponse.IsSuccess = true;
                    }

                    //msgResponse.Data = new ReksaEBWUTCheckingSubsRes();
                    msgResponse.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = ErrCode;
                msgResponse.ErrorDescription = "ReksaEBWUTProcessRedemption : " + ex.Message;
            }
            return msgResponse;
        }
        #endregion createredeem
        #region checkswitch
        public ApiMessage<object> ReksaEBWUTCheckingSwitching(ApiMessage<ReksaEBWUTCheckingSwitchingReq> paramIn)
        {
            ApiMessage<object> msgResponse = new ApiMessage<object>();
            string errMsg = "";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            //20250515, lita, RDN-1244, begin
            //string spName = "ReksaEBWUTCheckingSwitching";
            string spName = "ReksaEBWUTCheckingSwitchingAPI";
            //20250515, lita, RDN-1244, end
            msgResponse.copyHeaderForReply(paramIn);
            try
            {
                if (paramIn.Data.CIFKey.ToString() != null)
                {
                    dbPar = new List<SQLSPParameter>();
                    dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                    dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                    dbPar.Add(new SQLSPParameter("@pmSwitchingUnit", paramIn.Data.SwitchingUnit));
                    dbPar.Add(new SQLSPParameter("@pmPctSwitchingFee", paramIn.Data.PctSwitchingFee));
                    dbPar.Add(new SQLSPParameter("@pmMinSwitchingUnit", paramIn.Data.MinSwitchingUnit));
                    dbPar.Add(new SQLSPParameter("@pnMinSwitchingNom", paramIn.Data.MinSwitchingNom));
                    dbPar.Add(new SQLSPParameter("@pmMinUnitRemaining", paramIn.Data.MinUnitRemaining));
                    dbPar.Add(new SQLSPParameter("@pcClientCodeSwcOut", paramIn.Data.ClientCodeSwcOut));
                    dbPar.Add(new SQLSPParameter("@pnClientIdSwcOut", paramIn.Data.ClientIdSwcOut));
                    dbPar.Add(new SQLSPParameter("@pnProductIdSwcOut", paramIn.Data.ProductIdSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcProductCodeSwcOut", paramIn.Data.ProductCodeSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcProductRiskProfileSwcOut", paramIn.Data.ProductRiskProfileSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountSwcOut", paramIn.Data.RelationAccountSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountNameSwcOut", paramIn.Data.RelationAccountNameSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcProductCurrencySwcOut", paramIn.Data.ProductCurrencySwcOut));
                    dbPar.Add(new SQLSPParameter("@pmUnitBalanceSwcOut", paramIn.Data.UnitBalanceSwcOut));
                    dbPar.Add(new SQLSPParameter("@pmNAVSwcOut", paramIn.Data.NAVSwcOut));
                    dbPar.Add(new SQLSPParameter("@pdNAVDateSwcOut", DateTime.Parse(paramIn.Data.NAVDateSwcOut)));
                    dbPar.Add(new SQLSPParameter("@pcClientCodeSwcIn", paramIn.Data.ClientCodeSwcIn));
                    dbPar.Add(new SQLSPParameter("@pnClientIdSwcIn", paramIn.Data.ClientIdSwcIn));
                    dbPar.Add(new SQLSPParameter("@pnProductIdSwcIn", paramIn.Data.ProductIdSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcProductCodeSwcIn", paramIn.Data.ProductCodeSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcProductRiskProfileSwcIn", paramIn.Data.ProductRiskProfileSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountSwcIn", paramIn.Data.RelationAccountSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountNameSwcIn", paramIn.Data.RelationAccountNameSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcProductCurrencySwcIn", paramIn.Data.ProductCurrencySwcIn));
                    dbPar.Add(new SQLSPParameter("@pmUnitBalanceSwcIn", paramIn.Data.UnitBalanceSwcIn));
                    dbPar.Add(new SQLSPParameter("@pmNAVSwcIn", paramIn.Data.NAVSwcIn));
                    dbPar.Add(new SQLSPParameter("@pdNAVDateSwcIn", DateTime.Parse(paramIn.Data.NAVDateSwcIn)));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountProductCode", paramIn.Data.RelationAccountProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountCurr", paramIn.Data.RelationAccountCurr));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountStatus", paramIn.Data.RelationAccountStatus));
                    dbPar.Add(new SQLSPParameter("@pnRelationAccountBalance", paramIn.Data.RelationAccountBalance));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountMC", paramIn.Data.RelationAccountMC));
                    dbPar.Add(new SQLSPParameter("@pcCheckingType", paramIn.Data.CheckingType));
                    dbPar.Add(new SQLSPParameter("@pcLanguageCode", paramIn.Data.LanguageCode));
                    dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel));
                    dbPar.Add(new SQLSPParameter("@cErrMsg", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref dbPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                    {
                        ErrDesc = dbPar[38].ParameterValue.ToString();
                        ErrCode = dbPar[39].ParameterValue.ToString();
                        if (ErrCode == "01000")
                        {
                            msgResponse.IsSuccess = true;
                            msgResponse.ErrorCode = ErrCode;
                            msgResponse.ErrorDescription = ErrDesc;
                        }
                        else
                        {
                            throw new Exception(ErrDesc);
                        }
                    }
                    else
                    {

                        msgResponse.ErrorDescription = dbPar[38].ParameterValue.ToString();
                        msgResponse.ErrorCode = dbPar[39].ParameterValue.ToString();
                        msgResponse.Data = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dsDataOut.Tables[0]));
                    }

                    //msgResponse.Data = new ReksaEBWUTCheckingSubsRes();
                    msgResponse.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = "ReksaEBWUTCheckingSwitching : " + ex.Message;
                msgResponse.ErrorCode = ErrCode;
                //msgResponse.ErrorDescription = ErrDesc;
            }
            return msgResponse;
        }
        #endregion checkswitch
        #region createswitch
        public ApiMessage<ReksaEBWUTProcessSwitchingRes> ReksaEBWUTProcessSwitching(ApiMessage<ReksaEBWUTProcessSwitchingReq> paramIn)
        {
            ApiMessage<ReksaEBWUTProcessSwitchingRes> msgResponse = new ApiMessage<ReksaEBWUTProcessSwitchingRes>();
            msgResponse.copyHeaderForReply(paramIn);
            string errMsg = "";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();

            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            //20250515, lita, RDN-1244, begin
            //string spName = "ReksaEBWUTProcessSwitching";
            string spName = "ReksaEBWUTProcessSwitchingAPI";
            //20250515, lita, RDN-1244, end

            try
            {
                if (paramIn.Data.CIFKey.ToString() != null)
                {
                    dbPar = new List<SQLSPParameter>();
                    dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                    dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                    dbPar.Add(new SQLSPParameter("@pmSwitchingUnit", paramIn.Data.SwitchingUnit));
                    dbPar.Add(new SQLSPParameter("@pmPctSwitchingFee", paramIn.Data.PctSwitchingFee));
                    dbPar.Add(new SQLSPParameter("@pmMinSwitchingUnit", paramIn.Data.MinSwitchingUnit));
                    dbPar.Add(new SQLSPParameter("@pnMinSwitchingNom", paramIn.Data.MinSwitchingNom));
                    dbPar.Add(new SQLSPParameter("@pmMinUnitRemaining", paramIn.Data.MinUnitRemaining));
                    dbPar.Add(new SQLSPParameter("@pmEstSwitchingAmt", paramIn.Data.EstSwitchingAmt));
                    dbPar.Add(new SQLSPParameter("@pmEstSwitchingFeeAmt", paramIn.Data.EstSwitchingFeeAmt));
                    dbPar.Add(new SQLSPParameter("@pcClientCodeSwcOut", paramIn.Data.ClientCodeSwcOut));
                    dbPar.Add(new SQLSPParameter("@pnClientIdSwcOut", paramIn.Data.ClientIdSwcOut));
                    dbPar.Add(new SQLSPParameter("@pnProductIdSwcOut", paramIn.Data.ProductIdSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcProductCodeSwcOut", paramIn.Data.ProductCodeSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountSwcOut", paramIn.Data.RelationAccountSwcOut));
                    dbPar.Add(new SQLSPParameter("@pcProductCurrencySwcOut", paramIn.Data.ProductCurrencySwcOut));
                    dbPar.Add(new SQLSPParameter("@pmUnitBalanceSwcOut", paramIn.Data.UnitBalanceSwcOut));
                    dbPar.Add(new SQLSPParameter("@pmNAVSwcOut", paramIn.Data.NAVSwcOut));
                    dbPar.Add(new SQLSPParameter("@pdNAVDateSwcOut", DateTime.Parse(paramIn.Data.NAVDateSwcOut)));
                    dbPar.Add(new SQLSPParameter("@pcClientCodeSwcIn", paramIn.Data.ClientCodeSwcIn));
                    dbPar.Add(new SQLSPParameter("@pnClientIdSwcIn", paramIn.Data.ClientIdSwcIn));
                    dbPar.Add(new SQLSPParameter("@pnProductIdSwcIn", paramIn.Data.ProductIdSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcProductCodeSwcIn", paramIn.Data.ProductCodeSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountSwcIn", paramIn.Data.RelationAccountSwcIn));
                    dbPar.Add(new SQLSPParameter("@pcProductCurrencySwcIn", paramIn.Data.ProductCurrencySwcIn));
                    dbPar.Add(new SQLSPParameter("@pmUnitBalanceSwcIn", paramIn.Data.UnitBalanceSwcIn));
                    dbPar.Add(new SQLSPParameter("@pmNAVSwcIn", paramIn.Data.NAVSwcIn));
                    dbPar.Add(new SQLSPParameter("@pdNAVDateSwcIn", DateTime.Parse(paramIn.Data.NAVDateSwcIn)));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountProductCode", paramIn.Data.RelationAccountProductCode));
                    dbPar.Add(new SQLSPParameter("@pcRelationAccountCurr", paramIn.Data.RelationAccountCurr));
                    dbPar.Add(new SQLSPParameter("@cErrMsg", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcLanguageCode", paramIn.Data.LanguageCode));
                    dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel));
                    dbPar.Add(new SQLSPParameter("@pcReferenceNo", paramIn.Data.TransactionSequenceNo));
                    dbPar.Add(new SQLSPParameter("@pbIsLewatCutOff", 0, ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcHoldType", "",ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcBlockReason", "", ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pcBlokirAmount", 0, ParamDirection.OUTPUT));
                    dbPar.Add(new SQLSPParameter("@pnTranId", 0, ParamDirection.OUTPUT));

                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref dbPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new ReksaEBWUTProcessSwitchingRes();
                    if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                    {
                        ErrDesc = dbPar[30].ParameterValue.ToString();
                        ErrCode = dbPar[31].ParameterValue.ToString();
                        if (ErrCode == "01000" && ErrDesc=="")
                        {
                            msgResponse.IsSuccess = true;
                            msgResponse.Data.TranId = Convert.ToInt64(dbPar[39].ParameterValue.ToString());
                            msgResponse.Data.BlockReason = dbPar[37].ParameterValue.ToString();
                            msgResponse.Data.BlokirAmount = Convert.ToDecimal(dbPar[38].ParameterValue.ToString());
                            msgResponse.Data.IsLewatCutOff = Convert.ToBoolean(Convert.ToInt32(dbPar[35].ParameterValue.ToString()));
                            msgResponse.Data.HoldType = dbPar[36].ParameterValue.ToString();
                            msgResponse.ErrorCode = ErrCode;
                            msgResponse.ErrorDescription = ErrDesc;
                        }
                        else
                        {
                            throw new Exception(ErrDesc);
                        }
                    }
                    else
                    {
                        msgResponse.ErrorCode = dbPar[31].ParameterValue.ToString();
                        msgResponse.ErrorDescription = dbPar[30].ParameterValue.ToString();
                        msgResponse.Data.TranId = Convert.ToInt64(dbPar[39].ParameterValue.ToString());
                        msgResponse.Data.BlockReason = dbPar[37].ParameterValue.ToString();
                        msgResponse.Data.BlokirAmount = Convert.ToDecimal(dbPar[38].ParameterValue.ToString());
                        msgResponse.Data.IsLewatCutOff = Convert.ToBoolean(Convert.ToInt32(dbPar[35].ParameterValue.ToString()));
                        msgResponse.Data.HoldType = dbPar[36].ParameterValue.ToString();
                        //20230324, Andhika J, VELOWEB-2313, begin
                        msgResponse.Data.RefID = dsDataOut.Tables[0].Rows[0]["RefID"].ToString();
                        //20230324, Andhika J, VELOWEB-2313, end
                        //20230911, Andhika J, RDN-1049, begin
                        int IsRDB = Convert.ToInt16(dsDataOut.Tables[0].Rows[0]["IsRDB"]);

                        if (IsRDB == 1)
                        {
                             msgResponse.Data.JangkaWaktu = Convert.ToInt32(dsDataOut.Tables[0].Rows[0]["JangkaWaktu"]);
                             msgResponse.Data.TanggalJatuhTempo = Convert.ToDateTime(dsDataOut.Tables[0].Rows[0]["JatuhTempo"].ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                             msgResponse.Data.Asuransi = dsDataOut.Tables[0].Rows[0]["Asuransi"].ToString();
                             msgResponse.Data.FrekuensiPendebetan = dsDataOut.Tables[0].Rows[0]["FrekuensiPendebetan"].ToString();
                             msgResponse.Data.NamaAsuransi = dsDataOut.Tables[0].Rows[0]["InsuranceName"].ToString();
                        }
                        //20230911, Andhika J, RDN-1049, end
                        //20250515, lita, RDN-1244, begin
                        msgResponse.Data.AccountNoWithMCPrefix = dsDataOut.Tables[0].Rows[0]["AccountNoWithMCPrefix"].ToString();
                        //20250515, lita, RDN-1244, end

                        msgResponse.IsSuccess = true;
                    }

                    //msgResponse.Data = new ReksaEBWUTCheckingSubsRes();
                    msgResponse.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = ErrCode;
                msgResponse.ErrorDescription = "ReksaEBWUTProcessSwitching : " + ex.Message;
            }
            return msgResponse;
        }
        #endregion createswitch
        #region TrxSwcPost
        public ApiMessage<ReksaEBWUTProcessSwitchingPostRes> ReksaEBWUTProcessSwitchingPost(ApiMessage<ReksaEBWUTProcessSwitchingPostReq> paramIn, string SpName)
        {
            ApiMessage<ReksaEBWUTProcessSwitchingPostRes> msgResponse = new ApiMessage<ReksaEBWUTProcessSwitchingPostRes>();
            string errMsg = "";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            msgResponse.Data = new ReksaEBWUTProcessSwitchingPostRes();
            msgResponse.copyHeaderForReply(paramIn);
            try
            {
                dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                dbPar.Add(new SQLSPParameter("@pmSwitchingUnit", paramIn.Data.SwitchingUnit));
                dbPar.Add(new SQLSPParameter("@pmPctSwitchingFee", paramIn.Data.PctSwitchingFee));
                dbPar.Add(new SQLSPParameter("@pmMinSwitchingUnit", paramIn.Data.MinSwitchingUnit));
                dbPar.Add(new SQLSPParameter("@pnMinSwitchingNom", paramIn.Data.MinSwitchingNom));
                dbPar.Add(new SQLSPParameter("@pmMinUnitRemaining", paramIn.Data.MinUnitRemaining));
                dbPar.Add(new SQLSPParameter("@pmEstSwitchingAmt", paramIn.Data.EstSwitchingAmt));
                dbPar.Add(new SQLSPParameter("@pmEstSwitchingFeeAmt", paramIn.Data.EstSwitchingFeeAmt));
                dbPar.Add(new SQLSPParameter("@pcClientCodeSwcOut", paramIn.Data.ClientCodeSwcOut));
                dbPar.Add(new SQLSPParameter("@pnClientIdSwcOut", paramIn.Data.ClientIdSwcOut));
                dbPar.Add(new SQLSPParameter("@pnProductIdSwcOut", paramIn.Data.ProductIdSwcOut));
                dbPar.Add(new SQLSPParameter("@pcProductCodeSwcOut", paramIn.Data.ProductCodeSwcOut));
                dbPar.Add(new SQLSPParameter("@pcRelationAccountSwcOut", paramIn.Data.RelationAccountSwcOut));
                dbPar.Add(new SQLSPParameter("@pcProductCurrencySwcOut", paramIn.Data.ProductCurrencySwcOut));
                dbPar.Add(new SQLSPParameter("@pmUnitBalanceSwcOut", paramIn.Data.UnitBalanceSwcOut));
                dbPar.Add(new SQLSPParameter("@pmNAVSwcOut", paramIn.Data.NAVSwcOut));
                dbPar.Add(new SQLSPParameter("@pdNAVDateSwcOut", DateTime.Parse(paramIn.Data.NAVDateSwcOut)));
                dbPar.Add(new SQLSPParameter("@pcClientCodeSwcIn", paramIn.Data.ClientCodeSwcIn));
                dbPar.Add(new SQLSPParameter("@pnClientIdSwcIn", paramIn.Data.ClientIdSwcIn));
                dbPar.Add(new SQLSPParameter("@pnProductIdSwcIn", paramIn.Data.ProductIdSwcIn));
                dbPar.Add(new SQLSPParameter("@pcProductCodeSwcIn", paramIn.Data.ProductCodeSwcIn));
                dbPar.Add(new SQLSPParameter("@pcRelationAccountSwcIn", paramIn.Data.RelationAccountSwcIn));
                dbPar.Add(new SQLSPParameter("@pcProductCurrencySwcIn", paramIn.Data.ProductCurrencySwcIn));
                dbPar.Add(new SQLSPParameter("@pmUnitBalanceSwcIn", paramIn.Data.UnitBalanceSwcIn));
                dbPar.Add(new SQLSPParameter("@pmNAVSwcIn", paramIn.Data.NAVSwcIn));
                dbPar.Add(new SQLSPParameter("@pdNAVDateSwcIn", DateTime.Parse(paramIn.Data.NAVDateSwcIn)));
                dbPar.Add(new SQLSPParameter("@cErrMsg", "", ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcLanguageCode", paramIn.Data.LanguageCode));
                dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel));
                dbPar.Add(new SQLSPParameter("@pbIsLewatCutOff", paramIn.Data.IsLewatCutOff));
                dbPar.Add(new SQLSPParameter("@pnTranId", paramIn.Data.TranId));
                dbPar.Add(new SQLSPParameter("@pcBlockBranch", paramIn.Data.BlockBranch));
                dbPar.Add(new SQLSPParameter("@pcBlockSequence", paramIn.Data.BlockSequence));
                dbPar.Add(new SQLSPParameter("@pnStatus", paramIn.Data.Status));
                dbPar.Add(new SQLSPParameter("@pdProcessDate", "", ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pnHostSessionId", "", ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcClientCodeOut", "", ParamDirection.OUTPUT));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, SpName, ref dbPar, out dsDataOut, out errMsg))
                {
                    throw new Exception(errMsg);
                }

                if (!errMsg.EndsWith(""))
                    throw new Exception(errMsg);

                if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                {
                    ErrDesc = dbPar[28].ParameterValue.ToString();
                    ErrCode = dbPar[29].ParameterValue.ToString();
                    if (ErrCode == "01000")
                    {
                        msgResponse.Data.XMLScreen = dsDataOut.Tables[0].Rows[0]["XMLScreen"].ToString();
                        msgResponse.Data.ClientCodeOut = dbPar[39].ParameterValue.ToString();
                        msgResponse.Data.ProcessDate = Convert.ToDateTime(dbPar[37].ParameterValue.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                        msgResponse.Data.HostSessionId = dbPar[38].ParameterValue.ToString();
                        msgResponse.IsSuccess = true;
                        msgResponse.ErrorCode = ErrCode;
                        msgResponse.ErrorDescription = ErrDesc;
                    }
                    else
                    {
                        throw new Exception(ErrDesc);
                    }
                }
                else
                {
                    msgResponse.Data.XMLScreen = dsDataOut.Tables[0].Rows[0]["XMLScreen"].ToString();
                    msgResponse.Data.ClientCodeOut = dbPar[39].ParameterValue.ToString();
                    msgResponse.Data.ProcessDate = Convert.ToDateTime(dbPar[37].ParameterValue.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                    msgResponse.Data.HostSessionId = dbPar[38].ParameterValue.ToString();
                    msgResponse.IsSuccess = true;
                    msgResponse.ErrorDescription = dbPar[28].ParameterValue.ToString();
                    msgResponse.ErrorCode = dbPar[29].ParameterValue.ToString();
                }

                msgResponse.IsSuccess = true;

            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
                msgResponse.ErrorCode = ErrCode;
                msgResponse.ErrorDescription = "ReksaEBWUTProcessSwitchingPost : " + ErrDesc;
            }
            return msgResponse;
        }
        #endregion TrxSwcPost
        #region TrxSubsPost
        public ApiMessage<ReksaEBWUTProcessSubscriptionPostRes> ReksaEBWUTProcessSubscriptionPost(ApiMessage<ReksaEBWUTProcessSubscriptionPostReq> paramIn, string SpName)
        {
            ApiMessage<ReksaEBWUTProcessSubscriptionPostRes> msgResponse = new ApiMessage<ReksaEBWUTProcessSubscriptionPostRes>();
            string errMsg = "";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            msgResponse.Data = new ReksaEBWUTProcessSubscriptionPostRes();
            msgResponse.copyHeaderForReply(paramIn);
            try
            {

                if (string.IsNullOrEmpty(paramIn.Data.TranCode))
                    paramIn.Data.TranCode = "";
                dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey));
                dbPar.Add(new SQLSPParameter("@pnTranId", paramIn.Data.TranId));
                dbPar.Add(new SQLSPParameter("@pbIsLewatCutOff", paramIn.Data.IsLewatCutOff));
                dbPar.Add(new SQLSPParameter("@pcClientCode", paramIn.Data.ClientCode));
                dbPar.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientId));
                dbPar.Add(new SQLSPParameter("@pnProductId", paramIn.Data.ProductId));
                dbPar.Add(new SQLSPParameter("@pcProductCode", paramIn.Data.ProductCode));
                dbPar.Add(new SQLSPParameter("@pcRelationAccount", paramIn.Data.RelationAccount));
                dbPar.Add(new SQLSPParameter("@pcProductCurrency", paramIn.Data.ProductCurrency));
                dbPar.Add(new SQLSPParameter("@pnUnitBalance", paramIn.Data.UnitBalance));
                dbPar.Add(new SQLSPParameter("@pnSubsAmount", paramIn.Data.SubsAmount));
                dbPar.Add(new SQLSPParameter("@pnFeeType", paramIn.Data.FeeType));
                dbPar.Add(new SQLSPParameter("@pnSubsFee", paramIn.Data.SubsFee));
                dbPar.Add(new SQLSPParameter("@pnSubsFeeAmount", paramIn.Data.SubsFeeAmount));
                dbPar.Add(new SQLSPParameter("@pcSubsFeeCurr", paramIn.Data.SubsFeeCurr));
                dbPar.Add(new SQLSPParameter("@pcRelationAccountCurr", paramIn.Data.RelationAccountCurr));
                dbPar.Add(new SQLSPParameter("@pnNAV", paramIn.Data.NAV));
                dbPar.Add(new SQLSPParameter("@pdNAVDate", paramIn.Data.NAVDate));
                dbPar.Add(new SQLSPParameter("@pcLanguageCode", paramIn.Data.LanguageCode));
                dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel));
                dbPar.Add(new SQLSPParameter("@pcReferenceNo", paramIn.Data.TransactionSequenceNo));
                dbPar.Add(new SQLSPParameter("@pcBlockBranch", paramIn.Data.BlockBranch));
                dbPar.Add(new SQLSPParameter("@pcBlockSequence", paramIn.Data.BlockSequence));
                dbPar.Add(new SQLSPParameter("@pcProductRiskProfile", paramIn.Data.ProductRiskProfile));
                dbPar.Add(new SQLSPParameter("@pcRelationAccountName", paramIn.Data.RelationAccountName));
                dbPar.Add(new SQLSPParameter("@pcClientCodeOut", paramIn.Data.ClientCodeOut, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pnStatus", paramIn.Data.Status));
                dbPar.Add(new SQLSPParameter("@cErrMsg", paramIn.Data.ErrMsg, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcProviderErrCode", paramIn.Data.ProviderErrCode, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pdProcessDate", paramIn.Data.ProcessDate, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pnHostSessionId", paramIn.Data.HostSessionId, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pbIsRDB", paramIn.Data.IsRDB));
                dbPar.Add(new SQLSPParameter("@pcRDBAutoSubsUntil", paramIn.Data.RDBAutoSubsUntil));
                dbPar.Add(new SQLSPParameter("@pnRDBTenor", paramIn.Data.RDBTenor));
                dbPar.Add(new SQLSPParameter("@pcRDBMaturityDate", paramIn.Data.RDBMaturityDate));
                dbPar.Add(new SQLSPParameter("@pdRDBMonthlySubs", paramIn.Data.RDBMonthlySubs));
                dbPar.Add(new SQLSPParameter("@pbRDBInsuranceBit", paramIn.Data.RDBInsuranceBit));
                dbPar.Add(new SQLSPParameter("@pbRDBAutoRedeemBit", paramIn.Data.RDBAutoRedeemBit));
                dbPar.Add(new SQLSPParameter("@pcXMLDataExt", paramIn.Data.XMLDataExt));
                dbPar.Add(new SQLSPParameter("@pcTranCode", paramIn.Data.TranCode));
                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, SpName, ref dbPar, out dsDataOut, out errMsg))
                {
                    throw new Exception(errMsg);
                }

                if (!errMsg.EndsWith(""))
                    throw new Exception(errMsg);

                if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                {
                    ErrDesc = dbPar[29].ParameterValue.ToString();
                    ErrCode = dbPar[30].ParameterValue.ToString();
                    if (ErrCode == "01000")
                    {

                        msgResponse.Data.ClientCodeOut = dbPar[27].ParameterValue.ToString();
                        msgResponse.Data.ProcessDate = Convert.ToDateTime(dbPar[31].ParameterValue.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                        msgResponse.Data.HostSessionId = dbPar[32].ParameterValue.ToString();
                        msgResponse.IsSuccess = true;
                        msgResponse.ErrorCode = ErrCode;
                        msgResponse.ErrorDescription = ErrDesc;
                    }
                    else
                    {
                        throw new Exception(ErrDesc);
                    }
                }
                else
                {
                    msgResponse.Data.XMLScreen = dsDataOut.Tables[0].Rows[0]["XMLScreen"].ToString();
                    msgResponse.Data.ClientCodeOut = dbPar[27].ParameterValue.ToString();
                    msgResponse.Data.ProcessDate = msgResponse.Data.ProcessDate = Convert.ToDateTime(dbPar[31].ParameterValue.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                    msgResponse.Data.HostSessionId = dbPar[32].ParameterValue.ToString();
                    msgResponse.IsSuccess = true;
                    msgResponse.ErrorDescription = dbPar[29].ParameterValue.ToString();
                    msgResponse.ErrorCode = dbPar[30].ParameterValue.ToString();
                }

                msgResponse.IsSuccess = true;

            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
                msgResponse.ErrorCode = ErrCode;
                msgResponse.ErrorDescription = "ReksaEBWUTProcessSubscriptionPost : " + ErrDesc;
            }
            return msgResponse;
        }
        #endregion TrxSubsPost
        #region TrxReversal
        public ApiMessage<ReksaEBWUTProcessReversalRes> ReksaEBWUTProcessReversal(ApiMessage<ReksaEBWUTProcessReversalReq> paramIn, string SpName)
        {
            ApiMessage<ReksaEBWUTProcessReversalRes> msgResponse = new ApiMessage<ReksaEBWUTProcessReversalRes>();
            string errMsg = "";
            string ErrCode = "", ErrDesc = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            msgResponse.Data = new ReksaEBWUTProcessReversalRes();
            msgResponse.copyHeaderForReply(paramIn);
            try
            {
                dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcTransactionSequenceNo", paramIn.Data.TransactionSequenceNo));
                dbPar.Add(new SQLSPParameter("@pcLoginID", paramIn.Data.LoginID));
                dbPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFKey)); 
                dbPar.Add(new SQLSPParameter("@pcTranType", paramIn.Data.TranType)); 
                dbPar.Add(new SQLSPParameter("@pnTranId", paramIn.Data.TranId));
                dbPar.Add(new SQLSPParameter("@pcRelationAccount", paramIn.Data.RelationAccount, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcBlockSequence", paramIn.Data.BlockSequence, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@cErrMsg", "", ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcProviderErrCode", "", ParamDirection.OUTPUT));

                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, SpName, ref dbPar, out dsDataOut, out errMsg))
                {
                    throw new Exception(errMsg);
                }

                if (!errMsg.EndsWith(""))
                    throw new Exception(errMsg);
                if (string.IsNullOrEmpty(dbPar[6].ParameterValue.ToString()))
                    dbPar[6].ParameterValue = "0";
               

                if (dsDataOut == null || dsDataOut.Tables.Count < 1 || dsDataOut.Tables[0].Rows.Count < 1)
                {
                    ErrDesc = dbPar[7].ParameterValue.ToString();
                    ErrCode = dbPar[8].ParameterValue.ToString();
                    if (ErrCode == "01000")
                    {
                        msgResponse.Data.RelationAccount = dbPar[5].ParameterValue.ToString();
                        msgResponse.Data.BlockSequence = Convert.ToInt64(dbPar[6].ParameterValue.ToString());
                        msgResponse.IsSuccess = true;
                        msgResponse.ErrorCode = ErrCode;
                        msgResponse.ErrorDescription = ErrDesc;
                    }
                    else
                    {
                        throw new Exception(ErrDesc);
                    }
                }
                else
                {
                    msgResponse.Data.RelationAccount = dbPar[5].ParameterValue.ToString();
                    msgResponse.Data.BlockSequence = Convert.ToInt64(dbPar[6].ParameterValue.ToString());
                    msgResponse.IsSuccess = true;
                }

                msgResponse.IsSuccess = true;

            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "ReksaEBWUTProcessReversal : " + ErrDesc;
            }
            return msgResponse;
        }
        #endregion TrxReversal
        #region hitTIBCO 

        //20250610, Lita, RDN-1244, migrate tibco to ede, begin
        //#region releaseblockaccount
        //public ApiMessage<ReleaseBlockirAccountRes> CallACTReleaseBlockirAccount(ApiMessage<ReleaseBlockirAccountReq> request, out bool IsSuccess, out string strErrorDescription)
        //{
        //    TibcoClient tibcoClient;
        //    StringBuilder sbRqDetail;
        //    String strXMLResult;
        //    TibcoResultData tibcoResultData;
        //    String strTibcoServiceName;
        //    IsSuccess = false;
        //    strErrorDescription = "";
        //    ApiMessage<ReleaseBlockirAccountRes> respModel = new ApiMessage<ReleaseBlockirAccountRes>();
        //    try
        //    {
        //        strTibcoServiceName = "ACT_DEL_BlockAccount";
        //        Guid guid = Guid.NewGuid();
        //        // Request
        //        string strNIK = "7"; //must included , 7  = auto process
        //        string strOperationCode = "100";
        //        string strTrxGuid = guid.ToString();
        //        if (string.IsNullOrEmpty(request.Data.RecordID))
        //            request.Data.RecordID = "0";
        //        if (string.IsNullOrEmpty(request.Data.PayeeName))
        //            request.Data.PayeeName = "0";
        //        if (string.IsNullOrEmpty(request.Data.PREFIXCHECKNO))
        //            request.Data.PREFIXCHECKNO = "0";
        //        if (string.IsNullOrEmpty(request.Data.CheckDate))
        //            request.Data.CheckDate = "0";
        //        if (string.IsNullOrEmpty(request.Data.DateLastMaintenance))
        //            request.Data.DateLastMaintenance = "0";
        //        if (string.IsNullOrEmpty(request.Data.DatePlaced))
        //            request.Data.DatePlaced = "0";
        //        if (string.IsNullOrEmpty(request.Data.TimeChangeMode))
        //            request.Data.TimeChangeMode = "0";

        //        sbRqDetail = new StringBuilder();
        //        sbRqDetail.AppendLine("<ns1:AccountNumber>" + XMLHelper.convertStringToXML(request.Data.AccountNumber) + "</ns1:AccountNumber>");
        //        sbRqDetail.AppendLine("<ns1:AccountType>" + XMLHelper.convertStringToXML(request.Data.AccountType) + "</ns1:AccountType>");
        //        sbRqDetail.AppendLine("<ns1:Sequence>" + XMLHelper.convertStringToXML(request.Data.Sequence.ToString()) + "</ns1:Sequence>");
        //        sbRqDetail.AppendLine("<ns1:RecordID>" + XMLHelper.convertStringToXML(request.Data.RecordID.ToString()) + "</ns1:RecordID>");
        //        sbRqDetail.AppendLine("<ns1:TypeOfEntry>" + XMLHelper.convertStringToXML(request.Data.TypeOfEntry.ToString()) + "</ns1:TypeOfEntry>");
        //        sbRqDetail.AppendLine("<ns1:CheckAmount>" + XMLHelper.convertStringToXML(request.Data.CheckAmount.ToString()) + "</ns1:CheckAmount>");
        //        sbRqDetail.AppendLine("<ns1:LowCheckNumber>" + XMLHelper.convertStringToXML(request.Data.LowCheckNumber.ToString()) + "</ns1:LowCheckNumber>");
        //        sbRqDetail.AppendLine("<ns1:HighCheckNumber>" + XMLHelper.convertStringToXML(request.Data.HighCheckNumber.ToString()) + "</ns1:HighCheckNumber>");
        //        sbRqDetail.AppendLine("<ns1:StopCharge>" + XMLHelper.convertStringToXML(request.Data.StopCharge.ToString()) + "</ns1:StopCharge>");
        //        sbRqDetail.AppendLine("<ns1:PayeeName>" + XMLHelper.convertStringToXML(request.Data.PayeeName.ToString()) + "</ns1:PayeeName>");
        //        sbRqDetail.AppendLine("<ns1:StopHoldRemarks>" + XMLHelper.convertStringToXML(request.Data.StopHoldRemarks.ToString()) + "</ns1:StopHoldRemarks>");
        //        sbRqDetail.AppendLine("<ns1:CheckRTNumber>" + XMLHelper.convertStringToXML(request.Data.CheckRTNumber.ToString()) + "</ns1:CheckRTNumber>");
        //        sbRqDetail.AppendLine("<ns1:ExpirationDate>" + XMLHelper.convertStringToXML("0") + "</ns1:ExpirationDate>");
        //        sbRqDetail.AppendLine("<ns1:CheckDate>" + XMLHelper.convertStringToXML(request.Data.CheckDate.ToString()) + "</ns1:CheckDate>");
        //        sbRqDetail.AppendLine("<ns1:DateLastMaintenance>" + XMLHelper.convertStringToXML(request.Data.DateLastMaintenance.ToString()) + "</ns1:DateLastMaintenance>");
        //        sbRqDetail.AppendLine("<ns1:DatePlaced>" + XMLHelper.convertStringToXML(request.Data.DatePlaced.ToString()) + "</ns1:DatePlaced>");
        //        sbRqDetail.AppendLine("<ns1:HoldByBranch>" + XMLHelper.convertStringToXML(request.Data.HoldByBranch) + "</ns1:HoldByBranch>");
        //        sbRqDetail.AppendLine("<ns1:UserID>" + XMLHelper.convertStringToXML(request.Data.UserID.ToString()) + "</ns1:UserID>");
        //        sbRqDetail.AppendLine("<ns1:WorkstationID>" + XMLHelper.convertStringToXML("REKSA") + " </ns1:WorkstationID>");
        //        sbRqDetail.AppendLine("<ns1:TimeChangeMade>" + XMLHelper.convertStringToXML(request.Data.TimeChangeMode.ToString()) + "</ns1:TimeChangeMade>");
        //        sbRqDetail.AppendLine("<ns1:REASONCODE>" + XMLHelper.convertStringToXML("16") + "</ns1:REASONCODE>");
        //        sbRqDetail.AppendLine("<ns1:PREFIXCHECKNO>" + XMLHelper.convertStringToXML(request.Data.PREFIXCHECKNO.ToString()) + "</ns1:PREFIXCHECKNO>");

        //        tibcoClient = new TibcoClient(this._strConnSOA);
        //        strXMLResult = tibcoClient.callTibcoService(TibcoClient.TibcoHeaderType.MBASE_HEADER,
        //            strNIK,
        //            strTrxGuid,
        //            strTibcoServiceName,
        //            strOperationCode,
        //            "VELO",
        //            false,
        //            sbRqDetail.ToString());

        //        tibcoResultData = tibcoClient.convertXMLToObject(strXMLResult, typeof(ReleaseBlockirAccountRes));
        //        IsSuccess = true;
        //        strErrorDescription = "";

        //        // jika ada error di body response nya harus di handle
        //        if (tibcoResultData.ServiceEnvelope.ServiceBody.Error != null)
        //        {
        //            if (tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ProviderError != null)
        //            {
        //                // strErrorCode = tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ProviderError.ProviderErrorCode;
        //                strErrorDescription = tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ProviderError.ProviderErrorDetail;
        //            }
        //            else
        //            {
        //                strErrorDescription = tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ErrorDescription;
        //            }

        //            // throw new Exception ("[" + strErrorCode + "] - " + strErrorDescription);
        //            throw new Exception(strErrorDescription);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //this.LogErrorException(ex, "Error [ACT_DEL_BlockAccount]", out IsSuccess, out strErrorDescription);
        //        return respModel;
        //    }

        //    respModel.Data = (ReleaseBlockirAccountRes)tibcoResultData.ServiceEnvelope.ServiceBody.RsDetail;
        //    respModel.ErrorCode = "500";
        //    respModel.ErrorDescription = "ReleaseBlockirAccount : " + strErrorDescription;
        //    return respModel;
        //}

        //#endregion releaseblockaccount

        #region ede
        #region releaseblockaccount
        public ApiMessage<ReleaseBlockirAccountRes> CallACTReleaseBlockirAccount(ApiMessage<ReleaseBlockirAccountReq> request, out bool IsSuccess, out string strErrorDescription)
        {
            ApiMessage<ReleaseBlockirAccountRes> responseMsg = new ApiMessage<ReleaseBlockirAccountRes>();
            responseMsg.copyHeaderForReply(request);

            string strErrMsg = "", strErrorCode = "";
            string strUrlEDE = this._strUrlMBASE + @"/DD_DELETE_24210_DDVelocityStopEarmarkingDelete";
            Object edeResponse = null;
            IsSuccess = false;
            strErrorDescription = "";

            #region Setup Mbase
            string strMoreIndicator = request.Data.MoreIndicator;
            EDEProcessType processType = EDEProcessType.Transactional;
            string strCustomHeader = "";


            #endregion
            try
            {
                #region Validation
                if (request.Data.AccountNumber.Equals(0))
                    throw new Exception("AccountNumber wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.AccountType))
                    throw new Exception("AccountType wajib diisi ! ");
                if (request.Data.Sequence.Equals(0))
                    throw new Exception("Sequence wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.MoreIndicator))
                    throw new Exception("MoreIndicator wajib diisi ! ");


                #endregion

                #region Push MBASE
                StringBuilder stbHeader = new StringBuilder();
                StringBuilder stbBody = new StringBuilder();
                stbBody.AppendLine("\"iaccountnumber\" : " + request.Data.AccountNumber + ",");
                stbBody.AppendLine("\"iaccounttype\" : " + "\"" + request.Data.AccountType + "\", ");
                stbBody.AppendLine("\"isequence\" : " + request.Data.Sequence + ",");
                stbBody.AppendLine("\"hdmrec\" : " + "\"" + request.Data.MoreIndicator + "\" ");

                strCustomHeader = stbHeader.ToString();

                EDEClient edeClient = new EDEClient(this._urlGwEDE, this._urlCoreEDE, this._ignoreSSL);
                if (!edeClient.PushMBASEMessage(
                    strUrlEDE
                    , request.TransactionMessageGUID
                    , request.UserNIK
                    , request.UserBranch
                    , strMoreIndicator
                    , "wealth-ReksaAccountTransaction.ReleaseAccount"
                    , true
                    , processType
                    , strCustomHeader
                    , stbBody.ToString()
                    , out strErrMsg
                    , out strErrorCode
                    , out edeResponse))
                {
                    throw new Exception(strErrMsg);
                }

                if (edeResponse == null)
                    throw new Exception("Response dari core bank kosong !");

                #endregion

                #region Mapping Output
                /*** Get Header and Body ***/
                JObject jObj = JObject.Parse(edeResponse.ToString());
                JObject jHeader = JObject.Parse(jObj["data"][0]["header"].ToString());
                JObject jBody = JObject.Parse(jObj["data"][0]["body"].ToString());

                strMoreIndicator = jHeader["hdmrec"].ToString();
                List<JObject> listData = JsonConvert.DeserializeObject<List<JObject>>(jObj["data"][0]["body"]["data"].ToString());

                responseMsg.Data = new ReleaseBlockirAccountRes();
                responseMsg.Data.MoreIndicator = strMoreIndicator;

                List<Data24210> respData = new List<Data24210>();
                Data24210 singleData = new Data24210();
                foreach (JObject row in listData)
                {
                    singleData = new Data24210();
                    singleData.AccountNumber = decimal.Parse(row["raccountnumber"].ToString());
                    singleData.AccountType = (row["raccounttype"] == null ? "" : row["raccounttype"].ToString());
                    singleData.Sequence = decimal.Parse(row["rsequence"].ToString());
                    respData.Add(singleData);
                }
                responseMsg.Data.ListData = respData;

                #endregion
                responseMsg.IsSuccess = true;
                IsSuccess = true;
                strErrorDescription = "";

            }
            catch (Exception ex)
            {
                this._iApiLogger.logError(this, new StackTrace(), "Request => " + request.getJSONString() + "; Error = > " + ex.Message, request.TransactionMessageGUID);
                responseMsg.IsSuccess = false;
                responseMsg.ErrorCode = "500";
                responseMsg.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;

                IsSuccess = false;
                strErrorDescription = responseMsg.ErrorDescription;
            }
            finally
            {
                responseMsg.MessageDateTime = DateTime.Now;
            }

            return responseMsg;
        }
        #endregion releaseblockaccount
        #endregion ede
        //20250610, Lita, RDN-1244, migrate tibco to ede, end

        //20240108, Andhika J, RDN-1119, begin
        #region RemarkblockirAccountTIBCO 
        //public ApiMessage<AddBlockirAccountRes> CallACTBlockirAccount(ApiMessage<AddBlockirAccountReq> request, out bool IsSuccess, out string strErrorDescription)
        //{
        //    TibcoClient tibcoClient;
        //    StringBuilder sbRqDetail;
        //    String strXMLResult;
        //    TibcoResultData tibcoResultData;
        //    String strTibcoServiceName;
        //    IsSuccess = false;
        //    strErrorDescription = "";
        //    ApiMessage<AddBlockirAccountRes> respModel = new ApiMessage<AddBlockirAccountRes>();
        //    try
        //    {
        //        strTibcoServiceName = "ACT_ADD_BlockAccount";
        //        Guid guid = Guid.NewGuid();
        //        // Request
        //        string strNIK = "7"; //must included , 7  = auto process
        //        string strOperationCode = "100";
        //        string strTrxGuid = guid.ToString();

        //        if (string.IsNullOrEmpty(request.Data.RecordID))
        //            request.Data.RecordID = "0";
        //        if (string.IsNullOrEmpty(request.Data.PayeeName))
        //            request.Data.PayeeName = "0";
        //        if (string.IsNullOrEmpty(request.Data.PREFIXCHECKNO))
        //            request.Data.PREFIXCHECKNO = "0";
        //        if (string.IsNullOrEmpty(request.Data.CheckDate))
        //            request.Data.CheckDate = "0";
        //        if (string.IsNullOrEmpty(request.Data.DateLastMaintenance))
        //            request.Data.DateLastMaintenance = "0";
        //        if (string.IsNullOrEmpty(request.Data.DatePlaced))
        //            request.Data.DatePlaced = "0";
        //        if (string.IsNullOrEmpty(request.Data.TimeChangeMode))
        //            request.Data.TimeChangeMode = "0";

        //        sbRqDetail = new StringBuilder();
        //        sbRqDetail.AppendLine("<ns1:AccountNumber>" + XMLHelper.convertStringToXML(request.Data.AccountNumber) + "</ns1:AccountNumber>");
        //        sbRqDetail.AppendLine("<ns1:AccountType>" + XMLHelper.convertStringToXML(request.Data.AccountType) + "</ns1:AccountType>");
        //        sbRqDetail.AppendLine("<ns1:Sequence>" + XMLHelper.convertStringToXML(request.Data.Sequence.ToString()) + "</ns1:Sequence>");
        //        sbRqDetail.AppendLine("<ns1:RecordID>" + XMLHelper.convertStringToXML(request.Data.RecordID.ToString()) + "</ns1:RecordID>");
        //        sbRqDetail.AppendLine("<ns1:TypeOfEntry>" + XMLHelper.convertStringToXML(request.Data.TypeOfEntry.ToString()) + "</ns1:TypeOfEntry>");
        //        sbRqDetail.AppendLine("<ns1:CheckAmount>" + XMLHelper.convertStringToXML(request.Data.CheckAmount.ToString()) + "</ns1:CheckAmount>");
        //        sbRqDetail.AppendLine("<ns1:LowCheckNumber>" + XMLHelper.convertStringToXML(request.Data.LowCheckNumber.ToString()) + "</ns1:LowCheckNumber>");
        //        sbRqDetail.AppendLine("<ns1:HighCheckNumber>" + XMLHelper.convertStringToXML(request.Data.HighCheckNumber.ToString()) + "</ns1:HighCheckNumber>");
        //        sbRqDetail.AppendLine("<ns1:StopCharge>" + XMLHelper.convertStringToXML(request.Data.StopCharge.ToString()) + "</ns1:StopCharge>");
        //        sbRqDetail.AppendLine("<ns1:PayeeName>" + XMLHelper.convertStringToXML(request.Data.PayeeName.ToString()) + "</ns1:PayeeName>");
        //        sbRqDetail.AppendLine("<ns1:StopHoldRemarks>" + XMLHelper.convertStringToXML(request.Data.StopHoldRemarks.ToString()) + "</ns1:StopHoldRemarks>");
        //        sbRqDetail.AppendLine("<ns1:CheckRTNumber>" + XMLHelper.convertStringToXML(request.Data.CheckRTNumber.ToString()) + "</ns1:CheckRTNumber>");
        //        sbRqDetail.AppendLine("<ns1:ExpirationDate>" + XMLHelper.convertStringToXML("0") + "</ns1:ExpirationDate>");
        //        sbRqDetail.AppendLine("<ns1:CheckDate>" + XMLHelper.convertStringToXML(request.Data.CheckDate.ToString()) + "</ns1:CheckDate>");
        //        sbRqDetail.AppendLine("<ns1:DateLastMaintenance>" + XMLHelper.convertStringToXML(request.Data.DateLastMaintenance.ToString()) + "</ns1:DateLastMaintenance>");
        //        sbRqDetail.AppendLine("<ns1:DatePlaced>" + XMLHelper.convertStringToXML(request.Data.DatePlaced.ToString()) + "</ns1:DatePlaced>");
        //        sbRqDetail.AppendLine("<ns1:HoldByBranch>" + XMLHelper.convertStringToXML(request.Data.HoldByBranch) + "</ns1:HoldByBranch>");
        //        sbRqDetail.AppendLine("<ns1:UserID>" + XMLHelper.convertStringToXML(request.Data.UserID.ToString()) + "</ns1:UserID>");
        //        sbRqDetail.AppendLine("<ns1:WorkstationID>" + XMLHelper.convertStringToXML("REKSA") + " </ns1:WorkstationID>");
        //        sbRqDetail.AppendLine("<ns1:TimeChangeMade>" + XMLHelper.convertStringToXML(request.Data.TimeChangeMode.ToString()) + "</ns1:TimeChangeMade>");
        //        sbRqDetail.AppendLine("<ns1:REASONCODE>" + XMLHelper.convertStringToXML("16") + "</ns1:REASONCODE>");
        //        sbRqDetail.AppendLine("<ns1:PREFIXCHECKNO>" + XMLHelper.convertStringToXML(request.Data.PREFIXCHECKNO.ToString()) + "</ns1:PREFIXCHECKNO>");

        //        tibcoClient = new TibcoClient(this._strConnSOA);
        //        strXMLResult = tibcoClient.callTibcoService(TibcoClient.TibcoHeaderType.MBASE_HEADER,
        //            strNIK,
        //            strTrxGuid,
        //            strTibcoServiceName,
        //            strOperationCode,
        //            "VELO",
        //            false,
        //            sbRqDetail.ToString());

        //        tibcoResultData = tibcoClient.convertXMLToObject(strXMLResult, typeof(AddBlockirAccountRes));
        //        IsSuccess = true;
        //        strErrorDescription = "";

        //        // jika ada error di body response nya harus di handle
        //        if (tibcoResultData.ServiceEnvelope.ServiceBody.Error != null)
        //        {
        //            if (tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ProviderError != null)
        //            {
        //                // strErrorCode = tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ProviderError.ProviderErrorCode;
        //                strErrorDescription = tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ProviderError.ProviderErrorDetail;
        //            }
        //            else
        //            {
        //                //strErrorCode = tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ErrorCode;
        //                strErrorDescription = tibcoResultData.ServiceEnvelope.ServiceBody.Error.ErrorDetail.ErrorDescription;
        //            }

        //            // throw new Exception ("[" + strErrorCode + "] - " + strErrorDescription);
        //            throw new Exception(strErrorDescription);
        //        }
        //        respModel.IsSuccess = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        //this.LogErrorException(ex, "Error [ACT_ADD_BlockAccount]", out IsSuccess, out strErrorDescription);
        //        respModel.ErrorCode = "500";
        //        respModel.ErrorDescription = "AddBlockirAccount : " + strErrorDescription;
        //        return respModel;
        //    }

        //    respModel.Data = (AddBlockirAccountRes)tibcoResultData.ServiceEnvelope.ServiceBody.RsDetail;
        //    return respModel;
        //}
        #endregion blockirAccount
        private void GetRangeBlokirAccount(int nTranId, string strTranType, string sChannel)
        {
            string strError = "";
            DataSet dsData = new DataSet();
            string strQuery = @"
                declare @nTranId    int 
                , @cTranType        varchar(10)
                , @cChannel         varchar(10)
                , @dNAVValueDate    datetime
				, @nRangePeriod		int

                set @nTranId =  '" + nTranId + @"'
                set @cTranType =  '" + strTranType + @"'
                set @cChannel = '" + sChannel + @"'

                select @nRangePeriod = ParamValue 
                from ReksaParam_TR
                where ParamDesc = @cChannel
                
                if(@nTranId != 0)
                begin
                    if(@cTranType != 'SWC')
                    begin
                        if exists (select top 1 1 from ReksaTransaction_TT where TranId = @nTranId)
                        begin
                            select @dNAVValueDate = NAVValueDate from ReksaTransaction_TT where TranId = @nTranId
                        end
                        else
                        begin
                            select @dNAVValueDate = NAVValueDate from ReksaTransaction_TH where TranId = @nTranId
                        end
                    end
                    else 
                    begin
                        if exists (select top 1 1 from ReksaSwitchingTransaction_TM where TranId = @nTranId)
                        begin
                            select @dNAVValueDate = NAVValueDate from ReksaSwitchingTransaction_TM where TranId = @nTranId
                        end
                    end
               end
               else
               begin
                    set @dNAVValueDate = getdate()
               end
               select dbo.fnReksaGetEffectiveDate(@dNAVValueDate, @nRangePeriod) ExpiredDate
            ";
            if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, strQuery, out dsData, out strError))
            {
                if (dsData.Tables[0].Rows.Count > 0)
                {
                    dExpiredDate = Convert.ToDecimal(Convert.ToDateTime(dsData.Tables[0].Rows[0]["ExpiredDate"]).ToString("ddMMyy"));
                }
            }
        }
        #region blockirAccount 
        public ApiMessage<AddBlockirAccountRes> CallACTBlockirAccount(ApiMessage<AddBlockirAccountReq> request)
        {
            ApiMessage<AddBlockirAccountRes> responseMsg = new ApiMessage<AddBlockirAccountRes>();
            responseMsg.copyHeaderForReply(request);

            string strErrMsg = "", strErrorCode = "";
            string strUrlEDE = this._strUrlMBASE + @"/DD_ADD_22210_DDVelocityStopEarmarkingAdd";
            Object edeResponse = null;
            

            #region Setup Mbase
            string strMoreIndicator = request.Data.MoreIndicator;
            EDEProcessType processType = EDEProcessType.Transactional;
            string strCustomHeader = "";


            #endregion
            try
            {
                
                #region Validation
                if (request.Data.AccountNumber.Equals(0))
                    throw new Exception("AccountNumber wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.AccountType))
                    throw new Exception("AccountType wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.Typeofentry))
                    throw new Exception("Typeofentry wajib diisi ! ");
                if (request.Data.Checkamount.Equals(0))
                    throw new Exception("Checkamount wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.Stopholdremarks))
                    throw new Exception("Stopholdremarks wajib diisi ! ");
                //untuk pemisah dg channel lain
                if (request.Data.WorkstationId == "REKSA")
                {

                    //20240108, Andhika J, RDN-1119, begin
                    GetRangeBlokirAccount(request.Data.TranId, request.Data.TranType, request.Data.Channel);
                    //20240108, Andhika J, RDN-1119, end
                    if (request.Data.Expirationdate == 0)
                    {
                        //20231228, Lita, RDN-1109, perpanjang exp blokir jika hari libur, begin
                        //request.Data.Expirationdate = Convert.ToDecimal(DateTime.Now.AddDays(3).ToString("ddMMyy"));
                        //20240108, Andhika J, RDN-1119, begin
                        //request.Data.Expirationdate = Convert.ToDecimal(DateTime.Now.AddDays(this._iBlokirExpDays).ToString("ddMMyy"));
                        request.Data.Expirationdate = dExpiredDate;
                        //20240108, Andhika J, RDN-1119, end
                        //20231228, Lita, RDN-1109, perpanjang exp blokir jika hari libur, end
                    }

                    if (request.Data.Expirationdate.Equals(0))
                    {
                        throw new Exception("Expirationdate wajib diisi ! ");
                    }
                }
                if (request.Data.Holdbybranch.Equals(0))
                    throw new Exception("Holdbybranch wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.UserId))
                    throw new Exception("UserId wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.WorkstationId))
                    throw new Exception("WorkstationId wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.ReasonCode))
                    throw new Exception("ReasonCode wajib diisi ! ");
                if (string.IsNullOrEmpty(request.Data.MoreIndicator))
                    throw new Exception("MoreIndicator wajib diisi ! ");




                #endregion
                if (request.Data.Dateplaced == 0)
                {
                    request.Data.Dateplaced = Convert.ToDecimal(DateTime.Now.ToString("ddMMyy"));
                }



                #region Push MBASE
                StringBuilder stbHeader = new StringBuilder();
                StringBuilder stbBody = new StringBuilder();
                stbBody.AppendLine("\"iaccountnumber\" : " + request.Data.AccountNumber + ",");
                stbBody.AppendLine("\"iaccounttype\" : " + "\"" + request.Data.AccountType + "\", ");
                stbBody.AppendLine("\"isequence\" : " + request.Data.Sequence + ",");
                stbBody.AppendLine("\"irecordid\" : " + "\"" + request.Data.RecordId + "\", ");
                stbBody.AppendLine("\"itypeofentry\" : " + "\"" + request.Data.Typeofentry + "\", ");
                stbBody.AppendLine("\"icheckamount\" : " + request.Data.Checkamount + ",");
                stbBody.AppendLine("\"ilowchecknumber\" : " + request.Data.LowcheckNumber + ",");
                stbBody.AppendLine("\"ihighchecknumber\" : " + request.Data.HighcheckNumber + ",");
                stbBody.AppendLine("\"istopcharge\" : " + request.Data.Stopcharge + ",");
                stbBody.AppendLine("\"ipayeename\" : " + "\"" + request.Data.PayeeName + "\", ");
                stbBody.AppendLine("\"istopholdremarks\" : " + "\"" + request.Data.Stopholdremarks + "\", ");
                stbBody.AppendLine("\"icheckrtnumber\" : " + request.Data.CheckrtNumber + ",");
                stbBody.AppendLine("\"iexpirationdate\" : " + request.Data.Expirationdate + ",");
                stbBody.AppendLine("\"icheckdate\" : " + request.Data.Checkdate + ",");
                stbBody.AppendLine("\"idatelastmaintenance\" : " + request.Data.Datelastmaintenance + ",");
                stbBody.AppendLine("\"idateplaced\" : " + request.Data.Dateplaced + ",");
                stbBody.AppendLine("\"iholdbybranch\" : " + request.Data.Holdbybranch + ",");
                stbBody.AppendLine("\"iuserid\" : " + "\"" + request.Data.UserId + "\", ");
                stbBody.AppendLine("\"iworkstationid\" : " + "\"" + request.Data.WorkstationId + "\", ");
                stbBody.AppendLine("\"itimechangemade\" : " + request.Data.Timechangemade + ",");
                stbBody.AppendLine("\"ireasoncode\" : " + "\"" + request.Data.ReasonCode + "\", ");
                stbBody.AppendLine("\"iprefixcheckno\" : " + "\"" + request.Data.Prefixcheckno + "\", ");
                stbBody.AppendLine("\"hdmrec\" : " + "\"" + request.Data.MoreIndicator + "\" ");

                strCustomHeader = stbHeader.ToString();

                EDEClient edeClient = new EDEClient(this._urlGwEDE, this._urlCoreEDE, this._ignoreSSL);
                if (!edeClient.PushMBASEMessage(
                    strUrlEDE
                    , request.TransactionMessageGUID
                    , request.UserNIK
                    , request.UserBranch
                    , strMoreIndicator
                    , "VELO"
                    , true
                    , processType
                    , strCustomHeader
                    , stbBody.ToString()
                    , out strErrMsg
                    , out strErrorCode
                    , out edeResponse))
                {
                    throw new Exception(strErrMsg);
                }

                if (edeResponse == null)
                    throw new Exception("Response dari core bank kosong !");

                #endregion

                JObject jObj = JObject.Parse(edeResponse.ToString());
                JObject jHeader = JObject.Parse(jObj["data"][0]["header"].ToString());
                JObject jBody = JObject.Parse(jObj["data"][0]["body"].ToString());
                responseMsg.IsSuccess = true;
                List<JObject> listData = JsonConvert.DeserializeObject<List<JObject>>(jObj["data"][0]["body"]["data"].ToString());

                responseMsg.Data = new AddBlockirAccountRes();
                foreach (JObject row in listData)
                {
                    responseMsg.Data.AccountNumber = row["raccountnumber"].ToString();
                    responseMsg.Data.AccountType = (row["raccounttype"] == null ? "" : row["raccounttype"].ToString());
                    responseMsg.Data.Sequence = Convert.ToInt32(row["rsequence"].ToString());
                    responseMsg.Data.RecordID = row["rrecordid"].ToString();
                    responseMsg.Data.TypeOfEntry = row["rtypeofentry"].ToString();
                    responseMsg.Data.CheckAmount = Convert.ToDecimal(row["rcheckamount"]);
                    responseMsg.Data.LowCheckNumber = Convert.ToInt32(row["rlowchecknumber"]);
                    responseMsg.Data.StopCharge = Convert.ToInt32(row["rstopcharge"]);
                    responseMsg.Data.StopHoldRemarks = row["rstopholdremarks"].ToString();
                    responseMsg.Data.ExpirationDate = row["rexpirationdate"].ToString();
                    responseMsg.Data.DateLastMaintenance = row["rdatelastmaintenance"].ToString();
                    responseMsg.Data.DatePlaced = row["rdateplaced"].ToString();
                    responseMsg.Data.HoldByBranch = row["rholdbybranch"].ToString();
                    responseMsg.Data.UserID = row["ruserid"].ToString();
                    responseMsg.Data.WorkStationID = row["rworkstationid"].ToString();
                    responseMsg.Data.TimeChangeMode = row["rtimechangemade"].ToString();
                    responseMsg.Data.REASONCODE = Convert.ToInt32(row["rreasoncode"]);
                    responseMsg.Data.PREFIXCHECKNO = row["rprefixcheckno"].ToString();
                }

            }
            catch (Exception ex)
            {
                this._iApiLogger.logError(this, new StackTrace(), "Request => " + request.getJSONString() + "; Error = > " + ex.Message, request.TransactionMessageGUID);
                responseMsg.IsSuccess = false;
                responseMsg.ErrorCode = "500";
                responseMsg.ErrorDescription = "[" + new StackTrace().GetFrame(0).GetMethod().Name + "] - " + ex.Message;
            }
            finally
            {
                responseMsg.MessageDateTime = DateTime.Now;
            }

            return responseMsg;
        }
        #endregion blockirAccount
        #endregion hitTIBCO 
        //20220614, Andhika J, VELOWEB-1961, end
        //20230214, Andhika J, RDN-903, begin
        public void ReksaAuthorizeSwitching_BS(ApiMessage<ReksaAuthorizeSwitching_BSRq> inModel, out ApiMessage<ReksaAuthorizeSwitching_BSRs> RespReksaAuthorizeSwitching_BS)
        {
            //inModel.Data = new ReksaAuthorizeSwitching_BSRq();
            RespReksaAuthorizeSwitching_BS = new ApiMessage<ReksaAuthorizeSwitching_BSRs>();
            inModel.copyHeaderForReply(inModel);

            string methodApiUrl = this._url_apiWealthTransactionBE;
            methodApiUrl = methodApiUrl + "/ReksaAuthorizeSwitching_BS";
            RespReksaAuthorizeSwitching_BS.copyHeaderForReply(inModel);


            try
            {

                RestWSClient<ApiMessage<ReksaAuthorizeSwitching_BSRs>> restAPI = new RestWSClient<ApiMessage<ReksaAuthorizeSwitching_BSRs>>(this._ignoreSSL);
                RespReksaAuthorizeSwitching_BS = restAPI.invokeRESTServicePost(methodApiUrl, inModel);
                if (!RespReksaAuthorizeSwitching_BS.IsSuccess)
                    throw new Exception(RespReksaAuthorizeSwitching_BS.ErrorDescription);

            }
            catch (Exception ex)
            {
                //this._iApiLogger.logError(this, new StackTrace(), "Request => " + inModel.getJSONString() + "; Error = > " + ex.Message, inModel.TransactionMessageGUID);
                RespReksaAuthorizeSwitching_BS.IsSuccess = false;
                RespReksaAuthorizeSwitching_BS.ErrorCode = "500";
                RespReksaAuthorizeSwitching_BS.ErrorDescription = "ReksaAuthorizeSwitching_BS : " + ex.Message;
            }
            finally
            {
                RespReksaAuthorizeSwitching_BS.MessageDateTime = DateTime.Now;
            }
            

        }
        //20230214, Andhika J, RDN-903, end
        //20230228, Andhika J, RDN-903, begin
        public ApiMessage<ReksaAuthorizeTransaction_BSRs> ReksaAuthorizeTransaction_BS(ApiMessage<ReksaAuthorizeTransaction_BSRq> inModel)
        {
            ApiMessage<ReksaAuthorizeTransaction_BSRs> msgResponse = new ApiMessage<ReksaAuthorizeTransaction_BSRs>();
            inModel.copyHeaderForReply(inModel);

            string methodApiUrl = this._url_apiWealthTransactionBE;
            methodApiUrl = methodApiUrl + "/ReksaAuthorizeTransaction_BS";
            msgResponse.copyHeaderForReply(inModel);


            try
            {

                RestWSClient<ApiMessage<ReksaAuthorizeTransaction_BSRs>> restAPI = new RestWSClient<ApiMessage<ReksaAuthorizeTransaction_BSRs>>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, inModel);
                if (!msgResponse.IsSuccess)
                    throw new Exception(msgResponse.ErrorDescription);

            }
            catch (Exception ex)
            {
                //this._iApiLogger.logError(this, new StackTrace(), "Request => " + inModel.getJSONString() + "; Error = > " + ex.Message, inModel.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "ReksaAuthorizeTransaction_BS : " + ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }
        //20230214, Andhika J, RDN-903, end
        //20230306, Andhika J, RDN-903, begin
        public ApiMessage<ReksaMaintainAllTransaksiNewRes> ReksaMaintainAllTransaksiNew(ApiMessage<ReksaMaintainAllTransaksiNewRq> inModel)
        {
            ApiMessage<ReksaMaintainAllTransaksiNewRes> msgResponse = new ApiMessage<ReksaMaintainAllTransaksiNewRes>();
            inModel.copyHeaderForReply(inModel);

            string methodApiUrl = this._url_apiWealthTransactionBE;
            methodApiUrl = methodApiUrl + "/ReksaMaintainAllTransaksiNew";
            msgResponse.copyHeaderForReply(inModel);

            try
            {

                RestWSClient<ApiMessage<ReksaMaintainAllTransaksiNewRes>> restAPI = new RestWSClient<ApiMessage<ReksaMaintainAllTransaksiNewRes>>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, inModel);
                if (!msgResponse.IsSuccess)
                    throw new Exception(msgResponse.ErrorDescription);

            }
            catch (Exception ex)
            {
                //this._iApiLogger.logError(this, new StackTrace(), "Request => " + inModel.getJSONString() + "; Error = > " + ex.Message, inModel.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "ReksaMaintainAllTransaksiNew : " + ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }
        //20230306, Andhika J, RDN-903, end
        //20230306, Andhika J, RDN-903, begin
        public ApiMessage<ReksaMaintainSwitchingRs> ReksaMaintainSwitching(ApiMessage<ReksaMaintainSwitchingRq> inModel)
        {
            ApiMessage<ReksaMaintainSwitchingRs> msgResponse = new ApiMessage<ReksaMaintainSwitchingRs>();
            inModel.copyHeaderForReply(inModel);

            string methodApiUrl = this._url_apiWealthTransactionBE;
            methodApiUrl = methodApiUrl + "/ReksaMaintainSwitching";
            msgResponse.copyHeaderForReply(inModel);

            try
            {

                RestWSClient<ApiMessage<ReksaMaintainSwitchingRs>> restAPI = new RestWSClient<ApiMessage<ReksaMaintainSwitchingRs>>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, inModel);
                if (!msgResponse.IsSuccess)
                    throw new Exception(msgResponse.ErrorDescription);

            }
            catch (Exception ex)
            {
                //this._iApiLogger.logError(this, new StackTrace(), "Request => " + inModel.getJSONString() + "; Error = > " + ex.Message, inModel.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "ReksaMaintainSwitching : " + ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }
        //20230306, Andhika J, RDN-903, end

        //20230728, ahmad.fansyuri, RDN-1017, begin
        public ApiMessage<ReksaMaintainSwitchingRs> ReksaMaintainSwitchingRDB(ApiMessage<ReksaMaintainSwitchingRq> inModel)
        {
            ApiMessage<ReksaMaintainSwitchingRs> msgResponse = new ApiMessage<ReksaMaintainSwitchingRs>();
            inModel.copyHeaderForReply(inModel);

            string methodApiUrl = this._url_apiWealthTransactionBE;
            methodApiUrl = methodApiUrl + "/ReksaMaintainSwitchingRDB";
            msgResponse.copyHeaderForReply(inModel);

            try
            {
                RestWSClient<ApiMessage<ReksaMaintainSwitchingRs>> restAPI = new RestWSClient<ApiMessage<ReksaMaintainSwitchingRs>>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, inModel);
                if (!msgResponse.IsSuccess)
                    throw new Exception(msgResponse.ErrorDescription);
            }

            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "ReksaMaintainSwitchingRDB : " + ex.Message;
            }

            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }
            return msgResponse;
        }
        //20230728, ahmad.fansyuri, RDN-1017, end

    }
}
