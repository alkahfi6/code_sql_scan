using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using static NISPDataSourceNetCore.database.EPV;
using static NISPDataSourceNetCore.database.SQLSPParameter;
using NISPDataSourceNetCore.webservice.model;
using Wealth.ReksaNasabah.API.Models;
using Wealth.ReksaNasabah.API.Support;
using System.Data;
using NISPDataSourceNetCore.converter;
using NISPDataSourceNetCore.database;
using NISPDataSourceNetCore.webservice;
using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Net.Http;
//using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
//20250707, gio, RDN-1254, begin
using System.Data.SqlClient;
//20250707, gio, RDN-1254, end

namespace Wealth.ReksaNasabah.API.Services
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
       20230313    Andhika J           RDN-903             Migrasi SP ReksaNTICreateNasabah to service API
       20230428    gio                 RDN-957             Penambahan service SaveRiskProfile, ReksaPopulateBigDataRiskProfile
       ==================================================================================================
    */
        // test build jenkins 6
        private IConfiguration _configuration;
        private string _strUrlWsPwd;
        private bool _ignoreSSL;
        private EPVEnvironmentType _envType;
        private string _strConnReksa;
        private string _strUrlWsReksa;
        private string _apiGuid;
        private string _localDataDurationDays;
        //20230313, Andhika J, RDN-903, begin
        private readonly string _url_apiWealthTransactionBE;
        //20230313, Andhika J, RDN-903, end
        //20230428 , Gio, RDN-957, begin
        private readonly string _url_apiBigDataPopulateRiskProfile;
        //private GlobalVariabel _global;
        private Thread ActionThread { get; set; }
        private Thread TimeoutThread { get; set; }
        private AutoResetEvent ThreadSynchronizer { get; set; }
        private bool _success;
        private bool _timeout;
        //20230428 , Gio, RDN-957, end

        public clsAPIService(IConfiguration iconfiguration, GlobalVariabelList globalVariabelList)
        {
            this._configuration = iconfiguration;
            this._strConnReksa = globalVariabelList.ConnectionStringDBReksa;

            this._envType = globalVariabelList.EnvironmentType;
            this._ignoreSSL = globalVariabelList.IgnoreSSL;
            //this._strUrlWsPwd = globalVariabelList.URLWsPwd;
            this._strUrlWsReksa = globalVariabelList.URLWsReksa;
            this._localDataDurationDays = globalVariabelList.LocalDataDurationDays;
            this._apiGuid = globalVariabelList.ApiGuid;
            //20230313, Andhika J, RDN-903, begin
            this._url_apiWealthTransactionBE = iconfiguration["url_apiWealthTransactionBE"].ToString();
            //20230313, Andhika J, RDN-903, end
            //20230428, Gio, RDN-957, begin
            //this._global = globalVariabel;
            this._url_apiBigDataPopulateRiskProfile = iconfiguration["url_apiBigDataPopulateRiskProfile"].ToString();
            //20230428, Gio, RDN-957, end
        }

        #region Create Customer
        public ApiMessage<ReksaEBWCreateCustomerRs> ReksaEBWCreateCustomer(ApiMessage<ReksaEBWCreateCustomerRq> paramIn)
        {
            ApiMessage<ReksaEBWCreateCustomerRs> msgResponse = new ApiMessage<ReksaEBWCreateCustomerRs>();
            List<ReksaEBWCreateCustomerRs> clsResponse = new List<ReksaEBWCreateCustomerRs>(); 
            //20230313, Andhika J, RDN-903, begin
            ApiMessage<ReksaNTICreateNasabahRq> ReqAPIMessage = new ApiMessage<ReksaNTICreateNasabahRq>();
            ReqAPIMessage.Data = new ReksaNTICreateNasabahRq();
            ApiMessage<ReksaNTICreateNasabahRs> ResAPIMessage = new ApiMessage<ReksaNTICreateNasabahRs>();
            ResAPIMessage.Data = new ReksaNTICreateNasabahRs();
            //20230313, Andhika J, RDN-903, end
            string errMsg = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            string spName = "ReksaNTICreateNasabah";
            string paramErrMsg = "";
            string providerErrCode = "";
            Int64 nasabahId = 0;
            string sid = "";


            try
            {
                //20230313, Andhika J, RDN-903, begin
                #region RemarkExisting
                //sqlPar = new List<SQLSPParameter>();
                //if (paramIn.Data.Channel == "WOB")
                //{
                //    sqlPar.Add(new SQLSPParameter("@pcLoginId", ""));
                //}
                //else
                //{
                //    sqlPar.Add(new SQLSPParameter("@pcLoginId", paramIn.Data.LoginId));
                //}
                //sqlPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel));
                //sqlPar.Add(new SQLSPParameter("@pcXMLDataNasabah", convertDataNasabahToXML(paramIn.Data.DataNasabah))); //convert data object ke xml
                //sqlPar.Add(new SQLSPParameter("@pcXMLRiskAnswer", convertDataAnswerToXML(paramIn.Data.RiskAnswer))); //convert data object ke xml
                //sqlPar.Add(new SQLSPParameter("@pnNasabahId", nasabahId = 0, ParamDirection.OUTPUT));
                //sqlPar.Add(new SQLSPParameter("@pcShareholderId", sid = "", ParamDirection.OUTPUT));
                //sqlPar.Add(new SQLSPParameter("@pcErrMessage", paramErrMsg = "", ParamDirection.OUTPUT));
                //sqlPar.Add(new SQLSPParameter("@pcProviderErrCode", providerErrCode = "", ParamDirection.OUTPUT));

                //if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsDataOut, out errMsg))
                //{
                //    throw new Exception(errMsg);
                //}

                //if (!errMsg.EndsWith(""))
                //    throw new Exception(errMsg);
                #endregion 
                #region HitAPI
                string cLoginID = "";
                if (paramIn.Data.Channel == "WOB")
                {
                    cLoginID = "";
                }
                else
                {
                    cLoginID = paramIn.Data.LoginId;
                }
                ReqAPIMessage.Data.cLoginId = cLoginID;
                ReqAPIMessage.Data.cChannel = paramIn.Data.Channel;
                ReqAPIMessage.Data.DataNasabah = paramIn.Data.DataNasabah;
                ReqAPIMessage.Data.RiskAnswer = paramIn.Data.RiskAnswer;
                //debug
                ReqAPIMessage.Data.cProviderErrCode = "";
                ReqAPIMessage.Data.cErrMessage = "";
                ReqAPIMessage.Data.cErrMsgProCIF = "";

                msgResponse.Data = new ReksaEBWCreateCustomerRs();
                //debug end
                ResAPIMessage = ReksaNTICreateNasabah(paramIn);
                if (!ResAPIMessage.IsSuccess)
                {
                    msgResponse.Data.NasabahId = 0;
                    msgResponse.Data.ShareHolderId = "";
                    msgResponse.Data.ProviderErrCode = "01000";
                    msgResponse.Data.ErrMessage = ResAPIMessage.ErrorDescription;
                    msgResponse.IsSuccess = true;
                    msgResponse.ModuleName = paramIn.ModuleName;
                    msgResponse.UserBranch = paramIn.UserBranch;
                    throw new Exception(ResAPIMessage.ErrorDescription);
                }
                else
                {
                    msgResponse.Data.NasabahId = ResAPIMessage.Data.NasabahId;
                    msgResponse.Data.ShareHolderId = ResAPIMessage.Data.ShareholderId;
                    msgResponse.Data.ProviderErrCode = ResAPIMessage.Data.ProviderErrCode;
                    msgResponse.Data.ErrMessage = ResAPIMessage.Data.ErrMessage;
                    msgResponse.IsSuccess = true;
                    msgResponse.ModuleName = paramIn.ModuleName;
                    msgResponse.UserBranch = paramIn.UserBranch;

                }
                #endregion HitAPI

                //20230313, Andhika J, RDN-903, end

            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
            }
            return msgResponse;
        }
        #endregion Create Customer

        #region Inquiry Customer Status
        public ApiMessage<ReksaEBWInquiryCustomerStatusRs> ReksaEBWInquiryCustomerStatus(ApiMessage<ReksaEBWInquiryCustomerStatusRq> paramIn)
        {
            ApiMessage<ReksaEBWInquiryCustomerStatusRs> msgResponse = new ApiMessage<ReksaEBWInquiryCustomerStatusRs>();
            string errMsg = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            string spName = "ReksaNTIInquiryCustomerStatus";
            string paramErrMsg = "";
            string providerErrCode = "";
            string newNasabah = "";
            string isPendingSII = "";
            //20250707, gio, RDN-1254, begin
            DataSet dsDataUSCitizen = new DataSet();
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
            //20250707, gio, RDN-1254, end

            try
            {
                if (paramIn.Data.CIFNo.ToString() != null)
                {
                    sqlPar = new List<SQLSPParameter>();
                    sqlPar.Add(new SQLSPParameter("@pnCIFKey", paramIn.Data.CIFNo));
                    sqlPar.Add(new SQLSPParameter("@pbIsNewNasabah", newNasabah, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pbIsPendingSII", isPendingSII, ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pcErrMessage", paramErrMsg = "", ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pcProviderErrCode", providerErrCode = "", ParamDirection.OUTPUT));
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new ReksaEBWInquiryCustomerStatusRs();
                    msgResponse.Data.ProviderErrCode = sqlPar[4].ParameterValue.ToString();
                    msgResponse.Data.ErrorMessage = (sqlPar[3].ParameterValue.ToString());
                    bool isNewNasabah = true;
                    bool isPending = false;
                    if (sqlPar[1].ParameterValue.ToString() == "0")
                        isNewNasabah = false;
                    if (sqlPar[2].ParameterValue.ToString() == "1")
                        isPending = true;
                    msgResponse.Data.IsNewNasabah = isNewNasabah;
                    msgResponse.Data.IsPendingSID = isPending;
                    if (dsDataOut.Tables.Count > 0)
                    {
                        if (dsDataOut.Tables[0].Rows.Count > 0)
                        {
                            DataCustomerStatusRs data = new DataCustomerStatusRs();
                            data.SpouseName = dsDataOut.Tables[0].Rows[0]["SpouseName"].ToString();
                            data.SourceOfFund = dsDataOut.Tables[0].Rows[0]["SourceOfFund"].ToString();
                            data.InvestmentPurpose = dsDataOut.Tables[0].Rows[0]["InvestmentPurpose"].ToString();
                            msgResponse.Data.DataFromReksa = data;
                        }
                    }
                    //20250707, gio, RDN-1254, begin
                    SqlParameter[] sqlParam = new SqlParameter[1];
                    sqlParam[0] = new SqlParameter("@pcCIFNo", paramIn.Data.CIFNo);
                    if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCheckUSCitizen, ref sqlParam, out dsDataUSCitizen, out errMsg))
                    {
                        if (dsDataUSCitizen.Tables[0].Rows.Count > 0)
                        {
                            msgResponse.Data.USCitizen = Convert.ToBoolean(dsDataUSCitizen.Tables[0].Rows[0]["bUSCitizen"].ToString());
                        }
                    }
                    //20250707, gio, RDN-1254, end
                    //msgResponse.Data.XMLDataFromReksa = sqlPar[5].ParameterValue.ToString();
                    msgResponse.IsSuccess = true;
                    msgResponse.ModuleName = paramIn.ModuleName;
                    msgResponse.UserBranch = paramIn.UserBranch;
                }
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
            }
            return msgResponse;
        }
        #endregion Inquiry Customer Status

        #region Inquiry Question Param
        public ApiMessage<ReksaEBWInquiryQuestionParamRs> ReksaEBWInquiryQuestionParam(ApiMessage<ReksaEBWInquiryQuestionParamRq> paramIn)
        {
            ApiMessage<ReksaEBWInquiryQuestionParamRs> msgResponse = new ApiMessage<ReksaEBWInquiryQuestionParamRs>();
            //20230428, gio, RDN-957, begin
            ApiMessage<BigDataRiskProfileRs> msgResponse2 = new ApiMessage<BigDataRiskProfileRs>();
            //20230428, gio, RDN-957, end
            string errMsg = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            //string spName = "ReksaNTIInquiryQuestionParam";
            // string spName = "ReksaNTIInquiryQuestionParam_AutoPopulateQuestion";
            string spName2 = "ReksaNTIInquiryQuestionParamAPI";
            var paramErrMsg = "";
            var providerErrCode = "";
            try
            {
                if(paramIn.Data.PageSeq == 0)
                {
                    sqlPar = new List<SQLSPParameter>();
                    sqlPar.Add(new SQLSPParameter("@pcQuestionType", paramIn.Data.QuestionType));
                    sqlPar.Add(new SQLSPParameter("@pcQuestionLang", paramIn.Data.QuestionLang));
                    sqlPar.Add(new SQLSPParameter("@pnPageSequence", paramIn.Data.PageSeq));
                    sqlPar.Add(new SQLSPParameter("@pbQuestionResultBit", paramIn.Data.QuestionResultBit));
                    sqlPar.Add(new SQLSPParameter("@pcAnswerResult", paramIn.Data.AnswerResult));
                    sqlPar.Add(new SQLSPParameter("@pbAnswerResultByCode", paramIn.Data.AnswerResultByCode));
                    sqlPar.Add(new SQLSPParameter("@pnCIFNo", paramIn.Data.CIFNo));
                    sqlPar.Add(new SQLSPParameter("@pcErrMessage", paramErrMsg = "", ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pcProviderErrCode", providerErrCode = "", ParamDirection.OUTPUT));
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName2, ref sqlPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new ReksaEBWInquiryQuestionParamRs();
                    msgResponse.Data.ProviderErrCode = sqlPar[7].ParameterValue.ToString();
                    msgResponse.Data.ErrorMessage = (sqlPar[6].ParameterValue.ToString());
                    //20230428, Gio, RDN-957, begin
                    //msgResponse2 = ReksaPopulateBigDataRiskProfile(paramIn);
                    HttpClient cekTimeout = new HttpClient();
                    cekTimeout.Timeout = TimeSpan.FromSeconds(5);
                    var task = Task.Run(() => ReksaPopulateBigDataRiskProfile(paramIn));
                    //20230428, Gio, RDN-957, end
                    if (dsDataOut.Tables.Count > 0)
                    {
                        if (dsDataOut.Tables[0].Rows.Count > 0) // pengecekan question list
                        {
                            bool multAnswer = false;
                            bool isUpdate = false;
                            List<DataQuestionParamRs> listDataQ = new List<DataQuestionParamRs>();
                            for (int i = 0; i < dsDataOut.Tables[0].Rows.Count; i++)
                            {
                                DataQuestionParamRs dataQ = new DataQuestionParamRs();
                                dataQ.QuestionCode = dsDataOut.Tables[0].Rows[i]["QuestionCode"].ToString();
                                dataQ.PageSeq = Int32.Parse(dsDataOut.Tables[0].Rows[i]["PageSeq"].ToString());
                                dataQ.QuestionTitle = dsDataOut.Tables[0].Rows[i]["QuestionTitle"].ToString();
                                dataQ.QuestionDesc = dsDataOut.Tables[0].Rows[i]["QuestionDesc"].ToString();
                                if (dsDataOut.Tables[0].Rows[i]["IsMultipleAnswer"].ToString() == "True")
                                    multAnswer = true;
                                if (dsDataOut.Tables[0].Rows[i]["IsUpdated"].ToString() == "True")
                                    isUpdate = true;
                                dataQ.IsMultipleAnswer = multAnswer;
                                dataQ.IsUpdated = isUpdate;
                                listDataQ.Add(dataQ);
                                multAnswer = false;
                                isUpdate = false;
                            }
                            msgResponse.Data.QuestionList = listDataQ;
                        }
                        if (dsDataOut.Tables[1].Rows.Count > 0) // pengecekan answer list
                        {
                            List<DataAnswerParamRs> listDataA = new List<DataAnswerParamRs>();
                            
                            for (int i = 0; i < dsDataOut.Tables[1].Rows.Count; i++)
                            {
                                DataAnswerParamRs dataA = new DataAnswerParamRs();
                                dataA.QuestionCode = dsDataOut.Tables[1].Rows[i]["QuestionCode"].ToString();
                                dataA.AnswerCode = dsDataOut.Tables[1].Rows[i]["AnswerCode"].ToString();
                                dataA.AnswerDesc = dsDataOut.Tables[1].Rows[i]["AnswerDesc"].ToString();
                                dataA.AnswerValue = (dsDataOut.Tables[1].Rows[i]["AnswerValue"].ToString());
                                dataA.AnswerScore = Int32.Parse(dsDataOut.Tables[1].Rows[i]["AnswerScore"].ToString());
                                dataA.ActionPage = Int32.Parse(dsDataOut.Tables[1].Rows[i]["ActionPage"].ToString());
                                //20230428, Gio, RDN-957, begin
                                //string isFilled = dsDataOut.Tables[1].Rows[i]["IsFilled"].ToString();
                                if (dsDataOut.Tables[1].Rows[i]["IsFilled"].ToString() == "True")
                                    dataA.IsFilled = true;
                                else 
                                    dataA.IsFilled = false;
                                //20230428, Gio, RDN-957, end
                                listDataA.Add(dataA);
                            }                            
                            msgResponse.Data.AnswerList = listDataA;
                        }
                    }
                }
                else
                {
                    sqlPar = new List<SQLSPParameter>();
                    sqlPar.Add(new SQLSPParameter("@pcQuestionType", paramIn.Data.QuestionType));
                    sqlPar.Add(new SQLSPParameter("@pcQuestionLang", paramIn.Data.QuestionLang));
                    sqlPar.Add(new SQLSPParameter("@pnPageSequence", paramIn.Data.PageSeq));
                    sqlPar.Add(new SQLSPParameter("@pbQuestionResultBit", paramIn.Data.QuestionResultBit));
                    sqlPar.Add(new SQLSPParameter("@pcAnswerResult", paramIn.Data.AnswerResult));
                    sqlPar.Add(new SQLSPParameter("@pbAnswerResultByCode", paramIn.Data.AnswerResultByCode));
                    sqlPar.Add(new SQLSPParameter("@pnCIFNo", paramIn.Data.CIFNo));
                    sqlPar.Add(new SQLSPParameter("@pcErrMessage", paramErrMsg = "", ParamDirection.OUTPUT));
                    sqlPar.Add(new SQLSPParameter("@pcProviderErrCode", providerErrCode = "", ParamDirection.OUTPUT));
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName2, ref sqlPar, out dsDataOut, out errMsg))
                    {
                        throw new Exception(errMsg);
                    }

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new ReksaEBWInquiryQuestionParamRs();
                    msgResponse.Data.ProviderErrCode = sqlPar[7].ParameterValue.ToString();
                    msgResponse.Data.ErrorMessage = (sqlPar[6].ParameterValue.ToString());
                    //20230428, Gio, RDN-957, begin
                    //msgResponse2 = ReksaPopulateBigDataRiskProfile(paramIn);
                    HttpClient cekTimeout = new HttpClient();
                    cekTimeout.Timeout = TimeSpan.FromSeconds(5);
                    var task = Task.Run(() => ReksaPopulateBigDataRiskProfile(paramIn));
                    //20230428, Gio, RDN-957, end
                    if (dsDataOut.Tables.Count > 0)
                    {
                        string dateMax = "";
                        if (dsDataOut.Tables[0].Rows.Count > 0) // pengecekan question list
                        {
                            bool multAnswer = false;
                            bool isUpdate = false;
                            List<DataQuestionParamRs> listDataQ = new List<DataQuestionParamRs>();
                            for (int i = 0; i < dsDataOut.Tables[0].Rows.Count; i++)
                            {
                                DataQuestionParamRs dataQ = new DataQuestionParamRs();
                                dataQ.QuestionCode = dsDataOut.Tables[0].Rows[i]["QuestionCode"].ToString();
                                dataQ.PageSeq = Int32.Parse(dsDataOut.Tables[0].Rows[i]["PageSeq"].ToString());
                                dataQ.QuestionTitle = dsDataOut.Tables[0].Rows[i]["QuestionTitle"].ToString();
                                dataQ.QuestionDesc = dsDataOut.Tables[0].Rows[i]["QuestionDesc"].ToString();
                                dateMax = dsDataOut.Tables[0].Rows[i]["MaxDate"].ToString();
                                if (dsDataOut.Tables[0].Rows[i]["IsMultipleAnswer"].ToString() == "True")
                                    multAnswer = true;
                                    
                                if (dsDataOut.Tables[0].Rows[i]["IsUpdated"].ToString() == "True")
                                    isUpdate = true;
                                dataQ.IsMultipleAnswer = multAnswer;
                                dataQ.IsUpdated = isUpdate;
                                listDataQ.Add(dataQ);
                                multAnswer = false;
                                isUpdate = false;
                            }
                            msgResponse.Data.QuestionList = listDataQ;
                        }
                        if (dsDataOut.Tables[1].Rows.Count > 0) // pengecekan answer list
                        {
                            List<DataAnswerParamRs> listDataA = new List<DataAnswerParamRs>();

                            if (task.Wait(TimeSpan.FromSeconds(20)))
                            {
                                msgResponse2 = task.Result;
                                for (int i = 0; i < dsDataOut.Tables[1].Rows.Count; i++)
                                {
                                    DataAnswerParamRs dataA = new DataAnswerParamRs();
                                    dataA.QuestionCode = dsDataOut.Tables[1].Rows[i]["QuestionCode"].ToString();
                                    dataA.AnswerCode = dsDataOut.Tables[1].Rows[i]["AnswerCode"].ToString();
                                    dataA.AnswerDesc = dsDataOut.Tables[1].Rows[i]["AnswerDesc"].ToString();
                                    dataA.AnswerValue = (dsDataOut.Tables[1].Rows[i]["AnswerValue"].ToString());
                                    dataA.AnswerScore = Int32.Parse(dsDataOut.Tables[1].Rows[i]["AnswerScore"].ToString());
                                    dataA.ActionPage = Int32.Parse(dsDataOut.Tables[1].Rows[i]["ActionPage"].ToString());
                                    //20230428, Gio, RDN-957, begin
                                    if (dsDataOut.Tables[1].Rows[i]["IsFilled"].ToString() == "True")
                                        dataA.IsFilled = true;
                                    else 
                                        dataA.IsFilled = false;

                                    if(dateMax != "" && dateMax != null && dateMax != "null" && dateMax != "NULL")
                                    {
                                        string date = DateTime.Now.ToString("yyyy-MM-dd");
                                        
                                        if(date == dateMax)
                                        {

                                        }
                                        else
                                        {
                                            if (dataA.AnswerCode == "RISKANSWER15001")
                                            {
                                                if (msgResponse2.Data.PASAR_UANG == "Y")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            else if (dataA.AnswerCode == "RISKANSWER15002")
                                            {
                                                if (msgResponse2.Data.OBLIGASI == "Y")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            else if (dataA.AnswerCode == "RISKANSWER15003")
                                            {
                                                if (msgResponse2.Data.SAHAM == "Y")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            if (dataA.AnswerCode == "RISKANSWER16001")
                                            {
                                                if (msgResponse2.Data.AVGJANGKAWAKTUINVESTASI != null)
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            if (dataA.AnswerCode == "RISKANSWER17001")
                                            {
                                                if (msgResponse2.Data.AVGJANGKAWAKTUINVESTASI == "AT1")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            else if (dataA.AnswerCode == "RISKANSWER17002")
                                            {
                                                if (msgResponse2.Data.PASAR_UANG == "AT2")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            if (dataA.AnswerCode == "RISKANSWER18001")
                                            {
                                                if (msgResponse2.Data.AVGPENEMPATANDANA == "AB1")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            else if (dataA.AnswerCode == "RISKANSWER18002")
                                            {
                                                if (msgResponse2.Data.AVGPENEMPATANDANA == "AB2")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            if (dataA.AnswerCode == "RISKANSWER19001")
                                            {
                                                if (msgResponse2.Data.FREQPENEMPATANDANA == "FB1")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                            else if (dataA.AnswerCode == "RISKANSWER19002")
                                            {
                                                if (msgResponse2.Data.FREQPENEMPATANDANA == "FB2")
                                                {
                                                    dataA.IsFilled = true;
                                                }
                                            }
                                        }
                                    }

                                   
                                    //20230428, Gio, RDN-957, end
                                    listDataA.Add(dataA);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < dsDataOut.Tables[1].Rows.Count; i++)
                                {
                                    DataAnswerParamRs dataA = new DataAnswerParamRs();
                                    dataA.QuestionCode = dsDataOut.Tables[1].Rows[i]["QuestionCode"].ToString();
                                    dataA.AnswerCode = dsDataOut.Tables[1].Rows[i]["AnswerCode"].ToString();
                                    dataA.AnswerDesc = dsDataOut.Tables[1].Rows[i]["AnswerDesc"].ToString();
                                    dataA.AnswerValue = (dsDataOut.Tables[1].Rows[i]["AnswerValue"].ToString());
                                    dataA.AnswerScore = Int32.Parse(dsDataOut.Tables[1].Rows[i]["AnswerScore"].ToString());
                                    dataA.ActionPage = Int32.Parse(dsDataOut.Tables[1].Rows[i]["ActionPage"].ToString());
                                    //20230428, Gio, RDN-957, begin
                                    string isFilled = dsDataOut.Tables[1].Rows[i]["IsFilled"].ToString();
                                    //string isUpdated = dsDataOut.Tables[1].Rows[i]["IsUpdated"].ToString();

                                    if (isFilled == "0")
                                    {
                                        dataA.IsFilled = false;
                                    }
                                    else
                                    {
                                        dataA.IsFilled = true;
                                    }
                                    //if (isUpdated == "0")
                                    //{
                                    //    dataA.IsUpdated = false;
                                    //}
                                    //else
                                    //{
                                    //    dataA.IsUpdated = true;
                                    //}
                                    //20230428, Gio, RDN-957, end
                                    listDataA.Add(dataA);
                                }
                            }
                            msgResponse.Data.AnswerList = listDataA;
                        }
                    }
                }                
                //msgResponse.ModuleName = paramIn.ModuleName.ToString();
                //msgResponse.UserBranch = paramIn.UserBranch;
                msgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
            }
            return msgResponse;
        }
        #endregion Inquiry Question Param

        #region convert data to XML
        public String convertDataNasabahToXML(ReksaCreateNasabahRq rq)
        {
            string data = "<ROOT><RS>" +
               $"<Channel>{rq.Channel}</Channel>" +
               $"<CIFNo>{rq.CIFNo}</CIFNo>" +
               $"<CIFName>{rq.CIFName.ToUpper()}</CIFName>" +
               $"<SpouseName>{rq.SpouseName.ToUpper()}</SpouseName>" +
               $"<IncomePerAnum>{rq.IncomePerAnum}</IncomePerAnum>" +
               $"<SourceOfFund>{rq.SourceOfFund}</SourceOfFund>" +
               $"<InvestmentPurpose>{rq.InvestmentPurpose}</InvestmentPurpose>" +
               $"<OfficeId>{rq.OfficeId}</OfficeId>" +
               $"<BirthPlace>{rq.BirthPlace}</BirthPlace>" +
               $"<BirthDate>{rq.BirthDate}</BirthDate>" +
               $"<Email>{rq.Email}</Email>" +
               $"<NoHP>{rq.NoHP}</NoHP>" +
               $"<AccNo>{rq.AccNo}</AccNo>" +
               $"<AccName>{rq.AccName.ToUpper()}</AccName>" +
               $"<AccCurr>{rq.AccCurr}</AccCurr>" +
               $"<NPWPNo>{rq.NPWPNo}</NPWPNo>" +
               $"<NPWPName>{rq.NPWPName.ToUpper()}</NPWPName>" +
               $"<NPWPRegDate>{rq.NPWPRegDate}</NPWPRegDate>" +
               $"<AddressSeq>{rq.AddressSeq}</AddressSeq>" +
               $"<AddressLine1>{rq.AddressLine1}</AddressLine1>" +
               $"<AddressLine2>{rq.AddressLine2}</AddressLine2>" +
               $"<AddressLine3>{rq.AddressLine3}</AddressLine3>" +
               $"<AddressLine4>{rq.AddressLine4}</AddressLine4>" +
               $"<PostalCode>{rq.PostalCode}</PostalCode>" +
               $"<AddressForeign>{rq.AddressForeign}</AddressForeign>" +
               $"<MainAddress>{rq.MainAddress}</MainAddress>" +
               $"<AddressCode>{rq.AddressCode}</AddressCode>" +
               $"<AddressSID>{rq.AddressSID}</AddressSID>" +
               $"<AddressType>{rq.AddressType}</AddressType>" +
               $"<StaySince>{rq.StaySince}</StaySince>" +
               $"<Kelurahan>{rq.Kelurahan}</Kelurahan>" +
               $"<Kecamatan>{rq.Kecamatan}</Kecamatan>" +
               $"<Kota>{rq.Kota}</Kota>" +
               $"<Provinsi>{rq.Provinsi}</Provinsi>" +
               $"<RiskProfileCode>{rq.RiskProfileCode}</RiskProfileCode>" +
               $"<RiskProfileScore>{rq.RiskProfileScore}</RiskProfileScore>" +
               $"<NeedReviewKTP>{rq.NeedReviewKTP}</NeedReviewKTP>" +
               $"<NeedReviewNPWP>{rq.NeedReviewNPWP}</NeedReviewNPWP>" +
               $"<LanguageCode>{rq.LanguageCode}</LanguageCode>" +
               $"<Education>{rq.Education}</Education>" +
            "</RS> </ROOT>";
            return data;
        }

        public String convertDataAnswerToXML(List<ReksaRiskAnswerRq> rq)
        {
            string data = "<ROOT><RS><RiskProfileAnswerList><QuestionList>";
            for (int i = 0; i < rq.Count; i++)
            {
                data += "<QuestionData>";
                data += $"<PageSec>{rq[i].pageSec}</PageSec>";
                data += $"<QuestionCode>{rq[i].QuestionCode}</QuestionCode>";
                data += $"<QuestionDesc>{rq[i].QuestionDesc}</QuestionDesc>";
                for (int j = 0; j < rq[i].AnsList.Count; j++)
                {
                    if(rq[i].AnsList[j].AnswerDes.Contains("<"))
                        data += $"<AnswerDes>{Regex.Replace(rq[i].AnsList[j].AnswerDes,"<", "&lt;")}</AnswerDes>";
                    else if(rq[i].AnsList[j].AnswerDes.Contains(">"))
                        data += $"<AnswerDes>{Regex.Replace(rq[i].AnsList[j].AnswerDes, ">", "&gt;")}</AnswerDes>";
                    else
                        data += $"<AnswerDes>{rq[i].AnsList[j].AnswerDes}</AnswerDes>";

                    data += $"<AnswerScore>{rq[i].AnsList[j].AnswerScore}</AnswerScore>";
                }
                data += "</QuestionData>";
            }
            data += "</QuestionList></RiskProfileAnswerList></RS></ROOT>";
            return data;
        }
        #endregion convert data to XML
        //20230313, Andhika J, RDN-903, begin
        //20230612, Gio, RDN-978, begin
        //public ApiMessage<ReksaNTICreateNasabahRs> ReksaNTICreateNasabah(ApiMessage<ReksaNTICreateNasabahRq> inModel)
        public ApiMessage<ReksaNTICreateNasabahRs> ReksaNTICreateNasabah(ApiMessage<ReksaEBWCreateCustomerRq> inModel)
        //20230612, Gio, RDN-978, end
        {
            ApiMessage<ReksaNTICreateNasabahRs> msgResponse = new ApiMessage<ReksaNTICreateNasabahRs>();
            inModel.copyHeaderForReply(inModel);

            string methodApiUrl = this._url_apiWealthTransactionBE;
            methodApiUrl = methodApiUrl + "/ReksaNTICreateNasabah";
            msgResponse.copyHeaderForReply(inModel);

            try
            {

                RestWSClient<ApiMessage<ReksaNTICreateNasabahRs>> restAPI = new RestWSClient<ApiMessage<ReksaNTICreateNasabahRs>>(this._ignoreSSL);
                msgResponse = restAPI.invokeRESTServicePost(methodApiUrl, inModel);
                if (!msgResponse.IsSuccess)
                    throw new Exception(msgResponse.ErrorDescription);

            }
            catch (Exception ex)
            {
                //this._iApiLogger.logError(this, new StackTrace(), "Request => " + inModel.getJSONString() + "; Error = > " + ex.Message, inModel.TransactionMessageGUID);
                msgResponse.IsSuccess = false;
                msgResponse.ErrorCode = "500";
                msgResponse.ErrorDescription = "ReksaNTICreateNasabah : " + ex.Message;
            }
            finally
            {
                msgResponse.MessageDateTime = DateTime.Now;
            }

            return msgResponse;
        }

        //20230313, Andhika J, RDN-903, end

        //20230428, gio, RDN-957, begin
        public ApiMessage<ReksaSaveAnswerRiskProfileRs> ReksaSaveAnswerRiskProfile(ApiMessage<ReksaSaveAnswerRiskProfileRq> paramIn)
        {
            ApiMessage<ReksaSaveAnswerRiskProfileRs> msgResponse = new ApiMessage<ReksaSaveAnswerRiskProfileRs>();
            string errMsg = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            string spName = "ReksaSaveAnswerRiskProfile";
            string dateNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            int batchId = 0;
            Int64 cifNo = Int64.Parse(paramIn.Data.CIFNo.ToString());
            try
            {
                string query = @"
                    declare @pnCIFNo bigint 

                    set @pnCIFNo = " + cifNo + @"

                    if exists (select top 1 1 from ReksaRiskProfileQALog_TT where CIFNo = @pnCIFNo )  
                    begin  
                    insert into ReksaRiskProfileQALog_TH (CIFNo, InsertDate, PageSeq, QuestionCode, QuestionDesc, AnswerCode  
                     , AnswerDesc, AnswerScore, Channel, OfficeId, InputterNIK, BatchId)
                    select CIFNo, getdate(), PageSeq, QuestionCode, QuestionDesc, AnswerCode  
                     , AnswerDesc, AnswerScore, Channel, OfficeId, InputterNIK, BatchId
                     from ReksaRiskProfileQALog_TT where CIFNo = @pnCIFNo 

                    delete from ReksaRiskProfileQALog_TT where CIFNo = @pnCIFNo 

                    end  

                    declare @cParamValue int
                    select @cParamValue = isnull(ParamValue,0) + 1 from ReksaParam_TR where ParamCode = 'BATCHIDRP'
                    update ReksaParam_TR set ParamValue = @cParamValue where ParamCode = 'BATCHIDRP'
                    select @cParamValue BatchId
                ";
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, query, out dsParamOut, out errMsg))
                {
                    if(dsParamOut.Tables[0].Rows.Count > 0)
                        batchId = Convert.ToInt32(dsParamOut.Tables[0].Rows[0]["BatchId"].ToString());
                }
                else
                {
                    throw new Exception(errMsg);
                }

                
                string channel = paramIn.Data.Channel.ToString();
                for (int i = 0; i<paramIn.Data.RiskQuestion.Count; i++)
                {
                    for(int j = 0; j<paramIn.Data.RiskQuestion[i].AnsList.Count; j++)
                    {
                        sqlPar = new List<SQLSPParameter>();
                        sqlPar.Add(new SQLSPParameter("@pnCIFNo", cifNo));  
                        sqlPar.Add(new SQLSPParameter("@pnPageSeq", Int32.Parse(paramIn.Data.RiskQuestion[i].PageSeq.ToString())));
                        sqlPar.Add(new SQLSPParameter("@pcInsertDate", dateNow));
                        sqlPar.Add(new SQLSPParameter("@pcQuestionCode", paramIn.Data.RiskQuestion[i].QuestionCode.ToString()));
                        sqlPar.Add(new SQLSPParameter("@pcQuestionDesc", paramIn.Data.RiskQuestion[i].QuestionDesc.ToString()));
                        sqlPar.Add(new SQLSPParameter("@pnAnswerCode", paramIn.Data.RiskQuestion[i].AnsList[j].AnswerCode.ToString()));
                        sqlPar.Add(new SQLSPParameter("@pnAnswerDesc", paramIn.Data.RiskQuestion[i].AnsList[j].AnswerDesc.ToString()));
                        sqlPar.Add(new SQLSPParameter("@pnAnswerScore", Int32.Parse(paramIn.Data.RiskQuestion[i].AnsList[j].AnswerScore.ToString())));
                        sqlPar.Add(new SQLSPParameter("@pcChannel", channel));
                        sqlPar.Add(new SQLSPParameter("@pcNik", paramIn.UserNIK.ToString()));
                        sqlPar.Add(new SQLSPParameter("@pcOfficeId", paramIn.UserBranch.ToString()));
                        sqlPar.Add(new SQLSPParameter("@pnBatchId", batchId));
                        if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, spName, ref sqlPar, out dsDataOut, out errMsg))
                        {
                            throw new Exception(errMsg);
                        }

                    }
                }
                
                msgResponse.Data = new ReksaSaveAnswerRiskProfileRs();
                msgResponse.Data.ErrMsg = "";
                msgResponse.Data.IsSuccess = true;
                msgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
            }
            return msgResponse;
        }

        public ApiMessage<BigDataRiskProfileRs> ReksaPopulateBigDataRiskProfile(ApiMessage<ReksaEBWInquiryQuestionParamRq> paramIn)
        {
            ApiMessage<BigDataRiskProfileRs> msgResponse = new ApiMessage<BigDataRiskProfileRs>();
            string errMsg = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            ApiMessage<BigDataRiskProfileRq> req = new ApiMessage<BigDataRiskProfileRq>();
            req.Data = new BigDataRiskProfileRq();
            req.TransactionMessageGUID = paramIn.TransactionMessageGUID.ToString();
            req.Data.CustomerId = paramIn.Data.CIFNo.ToString();
            string methodAPIUrl = _url_apiBigDataPopulateRiskProfile;
            //methodAPIUrl = methodAPIUrl + "?id=";
            msgResponse.copyHeaderForReply(req);
            try
            {
                var client = new RestClient(methodAPIUrl);
                client.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                var request = new RestRequest(Method.POST);
                //request.AddHeader("id", "d46c712f90aea2178d0f6e9eeb0e25abd81b1c7227f6b509bae675c97464a953");
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("undefined", "{\"CUSTOMERID\":\""+ paramIn.Data.CIFNo + "\"}", ParameterType.RequestBody);
                 
                var response = client.Execute(request);
                //var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<BigDataResponse>(response.Content);
                response.Content = response.Content.Replace("\n", "");
                //Dictionary<string, string> sData = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                var responseData = JsonConvert.DeserializeObject<BigDataResponse>(response.Content);
                
                var waitSuccess = false;
                if (response.IsSuccessful)
                {
                    waitSuccess = true;
                }
                //RestWSClient<ApiMessage<BigDataRiskProfileRs>> restAPI = new RestWSClient<ApiMessage<BigDataRiskProfileRs>>(this._ignoreSSL);
                //msgResponse = restAPI.invokeRESTServicePost(methodAPIUrl, req.Data);
                if (!msgResponse.IsSuccess)
                    throw new Exception(msgResponse.ErrorDescription);


                //msgResponse.Data = new BigDataRiskProfileRs();
                msgResponse.Data = responseData.Data[0];
                //msgResponse.ModuleName = paramIn.ModuleName.ToString();
                //msgResponse.UserBranch = paramIn.UserBranch;
                msgResponse.MessageDateTime = DateTime.Now;
                msgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
            }
            return msgResponse;
        }

        //20230428, gio, RDN-957, end
    }
}
