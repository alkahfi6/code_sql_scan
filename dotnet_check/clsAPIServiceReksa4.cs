using System;
using System.Data;
using System.Text;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NISPDataSourceNetCore.webservice;
using NISPDataSourceNetCore.database;
using System.Globalization;
using RestSharp;
using static NISPDataSourceNetCore.database.EPV;
using static NISPDataSourceNetCore.database.SQLSPParameter;
using reksa_job.Service;
using reksa_job.Support;
using reksa_job.Model;
using System.Security.Cryptography;

namespace reksa_job.Services
{
    public class clsAPIService : IService
    {
        private IConfiguration _configuration;
        private ICommonService _common;
        private string _strConnSOA;
        //private string _strUrlWsPwd;
        private string _strApiUrlCoba;
        private bool _ignoreSSL;
        private EPVEnvironmentType _envType;
        private string _strConnReksa;
        private string _strUrlWsReksa;
        private string _apiGuid;
        private string _localDataDurationDays;
        private string _userNIK;

      

        public clsAPIService(IConfiguration iconfiguration, GlobalVariabel globalVariabel, ICommonService commonService)
        {
            this._configuration = iconfiguration;
            this._strConnReksa = globalVariabel.ConnectionStringDBReksa;
            this._common = commonService;
            this._strConnSOA = globalVariabel.ConnectionStringDBSOA;
            this._envType = globalVariabel.EnvironmentType;
            this._ignoreSSL = globalVariabel.IgnoreSSL;
            this._strApiUrlCoba = globalVariabel.URLApiCoba;
            //this._strUrlWsPwd = globalVariabel.URLWsPwd;
            this._strUrlWsReksa = globalVariabel.URLWsReksa;
            //this._strUrlWsBancaBatch = globalVariabel.URLWsBancaBatch;
            this._localDataDurationDays = globalVariabel.LocalDataDurationDays;
            this._apiGuid = globalVariabel.ApiGuid;
            this._userNIK = _configuration["userNIK"];
        }
        public string GetMethodName()
        {
            return new StackTrace(1).GetFrame(0).GetMethod().ReflectedType.Name + "." + new StackTrace(1).GetFrame(0).GetMethod().Name;
        }

        #region Push Notif Email NTI

        public bool ReksaPopulateNotif(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            strErrMsg = "";
            bool isSuccess = false;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                if(!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaSchedPopulateNotifNTI", ref sqlPar, out dsOut, out strErrMsg))
                {
                    throw new Exception(strErrMsg);
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        
        #endregion

        #region Sync Data NTI

        //public bool ReksaSchedGetFlowNTI(string ProcessGuid, string ProdCode, int IMode, string spName, out DataSet dsOut, out string strErrMsg) //RMM
        //{
        //    bool isSuccess = false;

        //    strErrMsg = "";
        //    dsOut = new DataSet();
        //    List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
        //    try
        //    {
        //        sqlPar = new List<SQLSPParameter>();
        //        sqlPar.Add(new SQLSPParameter("@pcProcessGuid", ProcessGuid));
        //        sqlPar.Add(new SQLSPParameter("@pcProductCode", ProdCode));
        //        sqlPar.Add(new SQLSPParameter("@pnMode", IMode));
        //        sqlPar.Add(new SQLSPParameter("@pcSPName", spName));
        //        sqlPar.Add(new SQLSPParameter("@pcErrMsg", strErrMsg));
        //        isSuccess = true;
        //        if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaGWSchedulerGetFlowAPI", ref sqlPar, out dsOut, out strErrMsg))
        //        {
        //            isSuccess = false;
        //            throw new Exception(strErrMsg);
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        isSuccess = false;
        //        throw new Exception(ex.Message);
        //    }
        //    return isSuccess;
        //}

        public bool ReksaSchedGetFlow(string ProcessGuid, string ProdCode, int IMode, string spName, out DataSet dsOut, out string strErrMsg) //OM
        {
            bool isSuccess = false;

            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcProcessGuid", ProcessGuid));
                sqlPar.Add(new SQLSPParameter("@pcProductCode", ProdCode));
                sqlPar.Add(new SQLSPParameter("@pnMode", IMode));
                sqlPar.Add(new SQLSPParameter("@pcSPName", spName));
                sqlPar.Add(new SQLSPParameter("@pcErrMsg", strErrMsg));
                isSuccess = true;
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaGWSchedulerGetFlow", ref sqlPar, out dsOut, out strErrMsg))
                {
                    isSuccess = false;
                    throw new Exception(strErrMsg);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaPopulateNotifOM(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            strErrMsg = "";
            bool isSuccess = false;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaSchedPopulateNotifNew", ref sqlPar, out dsOut, out strErrMsg))
                {
                    throw new Exception(strErrMsg);
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaNotificationUpdateStatus(string notifType, string guid, string status, string responseMsg, string apiReq, string apiRes, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            DataSet dsOut = new DataSet();

            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcNotifType", notifType));
                sqlPar.Add(new SQLSPParameter("@puGuidProcess", guid));
                sqlPar.Add(new SQLSPParameter("@pcStatus", status));
                sqlPar.Add(new SQLSPParameter("@pcResponseMsg", responseMsg));
                sqlPar.Add(new SQLSPParameter("@pcAPIRequest", apiReq));
                sqlPar.Add(new SQLSPParameter("@pcAPIResponse", apiRes));

                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaSchedUpdateNotifStatus", ref sqlPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }

        public bool ReksaSignatureGenerator(string clientKey, string clientSecret, string dataEmail, string timestamp, out string signature)
        {
            bool isSuccess = false;
            try
            {
                signature = SHA512_ComputeHash(clientKey, timestamp, dataEmail, clientSecret);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        private static string SHA512_ComputeHash(string strClientKey, string dtTimeStamp, string strData, string strSecretKey)
        {
            var hash = new StringBuilder();
            string inputText = strClientKey + dtTimeStamp + strData;
            byte[] secretkeyBytes = Encoding.UTF8.GetBytes(strSecretKey);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputText);
            using (var hmac = new HMACSHA512(secretkeyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
        #endregion

        //20220905, Lita, RDN-849, begin
        #region PDF
        public bool ReksaPopulateGenerateRDBCert(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            strErrMsg = "";
            bool isSuccess = false;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaPopulateGenerateRDBCert", ref sqlPar, out dsOut, out strErrMsg))
                {
                    throw new Exception(strErrMsg);
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaSchedGetFlowUpdate(string strGuid, string ProdCode, string sSPUpdate, string sBase64, string sMailTo, string sMailSubject, string sMailBody, string sStatus, string sStatusDesc
            , out string strErrMsg)
        {
            bool isSuccess = false;

            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcGuidData", strGuid));
                sqlPar.Add(new SQLSPParameter("@pcFlowCode", ProdCode));
                sqlPar.Add(new SQLSPParameter("@pvBase64", sBase64));
                sqlPar.Add(new SQLSPParameter("@pcMailTo", sMailTo));
                sqlPar.Add(new SQLSPParameter("@pcMailSubject", sMailSubject));
                sqlPar.Add(new SQLSPParameter("@pcMailBody", sMailBody));
                sqlPar.Add(new SQLSPParameter("@pcStatus", sStatus));
                sqlPar.Add(new SQLSPParameter("@pcStatusProcessDesc", sStatusDesc));
                sqlPar.Add(new SQLSPParameter("@pcErrMsg", strErrMsg));

                isSuccess = true;
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, sSPUpdate, ref sqlPar,  out strErrMsg))
                {
                    isSuccess = false;
                    throw new Exception(strErrMsg);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                strErrMsg = ex.Message;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaGenerateTrxMailNotif(string sRefID, string sTranCode, string sAuthMode, out DataSet dsOut, out string strErrMsg)
        {
            bool isSuccess = false;

            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcRefId", sRefID));
                sqlPar.Add(new SQLSPParameter("@pcTranCode", sTranCode));
                sqlPar.Add(new SQLSPParameter("@pcAuth", sAuthMode));
                sqlPar.Add(new SQLSPParameter("@pnMode", 1));
                isSuccess = true;
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaGenerateTrxMailNotif", ref sqlPar, out dsOut, out strErrMsg))
                {
                    isSuccess = false;
                    throw new Exception(strErrMsg);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                strErrMsg = ex.Message;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaPopulateMailAttachment(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            strErrMsg = "";
            bool isSuccess = false;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaPopulateMailAttachment", ref sqlPar, out dsOut, out strErrMsg))
                {
                    throw new Exception(strErrMsg);
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaMailAttachmentUpdateStatus(string guid, string status, string responseMsg, string apiReq, string apiRes, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            DataSet dsOut = new DataSet();

            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@puGuidProcess", guid));
                sqlPar.Add(new SQLSPParameter("@pcStatus", status));
                sqlPar.Add(new SQLSPParameter("@pcResponseMsg", responseMsg));
                sqlPar.Add(new SQLSPParameter("@pcAPIRequest", apiReq));
                sqlPar.Add(new SQLSPParameter("@pcAPIResponse", apiRes));

                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaMailAttachmentUpdateStatus", ref sqlPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }


        #endregion PDF
        //20220905, Lita, RDN-849, end


        //20221011, Gio, RDN-863, begin 
        #region SMSNotif
        public bool ReksaPopulateSMSRDBGagalDebet(out DataSet dsOut, out string strErrMsg)
        {
            bool isSuccess = false;
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaPopulateSMSRDBGagalDebet", out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaPopulateSMSRDBJatuhTempo(out DataSet dsOut, out string strErrMsg)
        {
            bool isSuccess = false;
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaPopulateSMSRDBJatuhTempo", out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaUpdateSMSNotifStatus(string notifType, string guid, string status, string responseMsg, string apiReq, string apiRes, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            DataSet dsOut = new DataSet();

            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcNotifType", notifType));
                sqlPar.Add(new SQLSPParameter("@puGuidProcess", guid));
                sqlPar.Add(new SQLSPParameter("@pcStatus", status));
                sqlPar.Add(new SQLSPParameter("@pcResponseMsg", responseMsg));
                sqlPar.Add(new SQLSPParameter("@pcAPIRequest", apiReq));
                sqlPar.Add(new SQLSPParameter("@pcAPIResponse", apiRes));

                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaUpdateSMSNotifStatus", ref sqlPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }

        public bool ReksaSchedGetFlowUpdateSMS(string smsGuid, string sSPUpdate, string sSMSBody, string sStatus, string sStatusDesc
            , out string strErrMsg)
        {
            bool isSuccess = false;

            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcGuidData", smsGuid));
                sqlPar.Add(new SQLSPParameter("@pcSMSBody", sSMSBody));
                sqlPar.Add(new SQLSPParameter("@pcStatus", sStatus));
                sqlPar.Add(new SQLSPParameter("@pcStatusProcessDesc", sStatusDesc));
                sqlPar.Add(new SQLSPParameter("@pcErrMsg", strErrMsg));

                isSuccess = true;
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, sSPUpdate, ref sqlPar, out strErrMsg))
                {
                    isSuccess = false;
                    throw new Exception(strErrMsg);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                strErrMsg = ex.Message;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }
        #endregion SMSNotif
        //20221011, Gio, RDN-863, begin

        

        public bool ReksaPopulateNotifGlobal(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            strErrMsg = "";
            bool isSuccess = false;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaPopulatePushNotificationData", ref sqlPar, out dsOut, out strErrMsg))
                {
                    throw new Exception(strErrMsg);
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }
        
        public bool ReksaSchedUpdateNotifKafka(string guid, string status, string responseMsg, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            DataSet dsOut = new DataSet();

            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@puGuidProcess", guid));
                sqlPar.Add(new SQLSPParameter("@pcStatus", status));
                sqlPar.Add(new SQLSPParameter("@pcResponseMsg", responseMsg));

                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaSchedUpdateNotifKafkaStatus", ref sqlPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }

        public bool ReksaSchedGetFlowUpdateKafka(string guid, string status, string processDesc, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            DataSet dsOut = new DataSet();

            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@puGuidProcess", guid));
                sqlPar.Add(new SQLSPParameter("@pcStatus", status));
                sqlPar.Add(new SQLSPParameter("@pcStatusProcessDesc", processDesc));
                sqlPar.Add(new SQLSPParameter("@pcErrMessage", strErrMsg));

                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaPopulateGenerateNotifBiayaMeteraiUpdate", ref sqlPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }
        //20230223, Gio, RDN-13, end

        //20240111, gio, RDN-1115, begin
        #region Push Notif Email Reminder
        public bool ReksaPopulateMailReminder(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            strErrMsg = "";
            bool isSuccess = false;
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            try
            {
                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaPopulateMailReminder", ref sqlPar, out dsOut, out strErrMsg))
                {
                    throw new Exception(strErrMsg);
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaMailReminderUpdateStatus(string notifType, string guid, string status, string responseMsg, string apiReq, string apiRes, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            DataSet dsOut = new DataSet();

            try
            {
                sqlPar = new List<SQLSPParameter>();
                sqlPar.Add(new SQLSPParameter("@pcNotifType", notifType));
                sqlPar.Add(new SQLSPParameter("@puGuidProcess", guid));
                sqlPar.Add(new SQLSPParameter("@pcStatus", status));
                sqlPar.Add(new SQLSPParameter("@pcResponseMsg", responseMsg));
                sqlPar.Add(new SQLSPParameter("@pcAPIRequest", apiReq));
                sqlPar.Add(new SQLSPParameter("@pcAPIResponse", apiRes));

                if (!clsCallWS.CallSPFromWs(this._strUrlWsReksa, this._ignoreSSL, "ReksaMailReminderUpdateStatus", ref sqlPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }
        #endregion
        //20240111, gio, RDN-1115, end

        //20250909, gio, RDN-1270, begin
        public bool ReksaAutoCancelTrxPending(out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            string query = @"
                declare @dCurrDate datetime, @cErrMsg varchar(200)

                select @dCurrDate = current_working_date
                from control_table
                --set @dCurrDate = '20251015'
                begin try
                if (not exists(select top 1 1 from ReksaTransaction_TT where Status = 0  
                and TranDate <= DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate), 0)) )
                and TranDate > DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate-1), 0)) )) 
                
                and not exists(select top 1 1 from ReksaSwitchingTransaction_TM where Status = 0  
                and TranDate <= DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate), 0)) )
                and TranDate > DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate-1), 0)) ))

				and not exists (select top 1 1 from ReksaRegulerSubscriptionClient_TM where Status = 0 
                and JoinDate <= DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate), 0)) )
                and JoinDate > DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate-1), 0)) ))
                )
				begin
					raiserror('Data tidak ada', 16, 1)
				end
                update ReksaTransaction_TT 
                set Status = 5 , CheckerSuid = 777
                where Status = 0 
                --and TranDate + 1< DATEADD(dd, 0, DATEDIFF(dd, 0, @dCurrDate)) 
                and TranDate <= DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate), 0)) )
                and TranDate > DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate-1), 0)) )
               

                update ReksaSwitchingTransaction_TM
                set Status = 5 , CheckerSuid = 777
                where Status = 0 
                --and TranDate + 1< DATEADD(dd, 0, DATEDIFF(dd, 0, @dCurrDate)) 
                and TranDate <= DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate), 0)) )
                and TranDate > DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate-1), 0)) )

                update ReksaRegulerSubscriptionClient_TM
                set Status = 5 , CheckerSuid = 777
                where Status = 0 
                and JoinDate <= DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate), 0)) )
                and JoinDate > DATEADD(minute,0,DATEADD(hh, 13, DATEADD(dd, DATEDIFF(dd, 0, @dCurrDate-1), 0)) )
                end try
                begin catch
				
					set @cErrMsg = ERROR_MESSAGE()  
					select @cErrMsg 'cErrMsg'
                end catch
                ";
            DataSet dsOut = new DataSet();

            try
            {
                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, query, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                if (dsOut.Tables.Count>0)
                    if (dsOut.Tables[0].Rows.Count > 0)
                    {
                        isSuccess = false;
                        throw new Exception(dsOut.Tables[0].Rows[0]["cErrMsg"].ToString());
                    }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }
            return isSuccess;
        }

        public bool ReksaPopulateMail(out DataSet dsOut, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            string query = @"
                DECLARE    
                  @cFunctionIdMail  varchar(10)    
                  ,@cFunctionIdNotif  varchar(10)    
                  ,@cErrMsg    varchar(200)     
                  ,@cProcessGuid   uniqueidentifier    
                  , @cType    varchar(20)    
                  , @cNotifType   varchar(20)    
                  , @cClassId    varchar(20)    
                  , @cEventInfo   varchar(20)    
                  , @cNotifChannelId  varchar(20)   
    
                 select @cProcessGuid = newid()    
   
    
                begin try    
                 create table #TempGuidMail (GuidMail uniqueidentifier, GuidProcess uniqueidentifier)    
    
    
                 -- Generate Mail data  ================================================================================================================     
                insert into #TempGuidMail (GuidMail, GuidProcess)    
                 select GuidMail, GuidProcess  
                 from dbo.ReksaGWMailTransactionCancel_TT    
                 where ([Status] = 'P'     
                    or ([Status] = 'F' AND [RetryCount] < [MaxRetry]))    
                    --and Channel in ('Cabang-RMD')    
    
                 update rgm     
                 set rgm.[Status] = 'I', DateProcess = getdate()    
                 from #TempGuidMail temp join dbo.ReksaGWMailTransactionCancel_TT rgm    
                 on temp.GuidMail = rgm.GuidMail    
                 --where Channel in ('Cabang-RMD')    
    
                 -- dataset Mail     
                 select 232 as 'FunctionID', temp.GuidMail 'GuidProcess', [Language]    
                   , MailSubject, MailBody, MailTo, MailSource   
                   , rgm.Status 'Status'  , temp.GuidProcess 'GuidProcessMail'  
                 from #TempGuidMail temp join dbo.ReksaGWMailTransactionCancel_TT rgm    
                 on temp.GuidMail = rgm.GuidMail    
                 where --Channel in ('Cabang-RMD')   and
                 rgm.Status = 'I'  
    
    
                 drop table #TempGuidMail    
    
                end try    
                begin catch    
                 if @@trancount > 0     
                  rollback tran    
    
                    set @cErrMsg = ERROR_MESSAGE()    
                 exec dbo.ReksaGWSchedulerLog @cProcessGuid, '', 'EMAILNOTIF ', 'ReksaPopulateMail', @cErrMsg    
                end catch    
            ";

            try
            {
                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, query, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }

        public bool ReksaMailUpdateStatus(string notifType, string guid, string status, string responseMsg, string apiReq, string apiRes, out string strErrMsg)
        {
            bool isSuccess = false;
            strErrMsg = "";
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            DataSet dsOut = new DataSet();
            string query = @"
            Declare
            @pcNotifType varchar(40)    
             ,@puGuidProcess uniqueidentifier    
             ,@pcStatus  char(1)      
             ,@pcResponseMsg varchar(max)    
             ,@pcAPIRequest varchar(max)    
             ,@pcAPIResponse varchar(max) 
     
             set @pcNotifType = '" + notifType + @"'
             set @puGuidProcess = '" + guid + @"'
             set @pcStatus = '" + status + @"'
             set @pcResponseMsg = '" + responseMsg + @"'
             set @pcAPIRequest = '" + apiReq + @"'
             set @pcAPIResponse = '" + apiRes + @"'
             set @puGuidProcess = upper(@puGuidProcess)    
    
             UPDATE dbo.ReksaGWMailTransactionCancel_TT    
             SET Status   = @pcStatus    
              ,DateProcess  = GETDATE()    
              ,[ResponseMessage] = @pcResponseMsg    
              ,[APIRequest]  = @pcAPIRequest    
              ,[APIResponse]  = @pcAPIResponse    
              ,[RetryCount]  = [RetryCount] + 1    
    
             WHERE GuidMail = @puGuidProcess    
            ";

            try
            {

                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, query, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                throw new Exception(ex.Message);
            }

            return isSuccess;
        }
        //20250909, gio, RDN-1270, end
    }
}
