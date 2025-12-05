using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace OBLI_SBN_Scheduler
{
    public class clsDataAccess
    {
        private NispQuery.ClsQuery _cQuery;

        public clsDataAccess(NispQuery.ClsQuery cQuery)
        {
            this._cQuery = cQuery;  
        }

        #region "General Parameter"
        public bool GetGeneralParam(out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool isOK = false;

            isOK = this._cQuery.ExecProc("dbo.SBNSchedGetGeneralParam", out dsOut);

            if (isOK)
            {
                if (dsOut.Tables.Count == 0 || dsOut.Tables[0].Rows.Count == 0)
                    return false;   
            }

            return isOK;
            
        }
        #endregion

        #region "Logging"
        public bool InsertLiveLog(int nTotalSuccess, int nTotalFailed, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[2];
                dbPar[0] = new OleDbParameter("@pnSuccessProcess", nTotalSuccess);
                dbPar[1] = new OleDbParameter("@pnFailedProcess", nTotalFailed);            

                isOK = this._cQuery.ExecProc("dbo.SBNSchedCreateLiveLog", ref dbPar);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(InsertLiveLog) - " + ex.Message;
                isOK = false;
            }
            return isOK;
        }

        #endregion

        #region "Investor"
        public bool InvestorUpdateResponse(string strGuid, string strWorkflowID, int nStatus, string strMessage, string strXmlResponse, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[5];
                dbPar[0] = new OleDbParameter("@puProcessGuid", strGuid);
                dbPar[1] = new OleDbParameter("@pcWorkflowID", strWorkflowID);
                dbPar[2] = new OleDbParameter("@pnStatus", nStatus);
                dbPar[3] = new OleDbParameter("@pcErrMsg", strMessage);
                dbPar[4] = new OleDbParameter("@pcXmlResponse", strXmlResponse);
              
                isOK = this._cQuery.ExecProc("dbo.SBNSchedUpdateResponseInvestor", ref dbPar);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(InvestorUpdateResponse) - " + ex.Message;
            }

            return isOK;
        }

        public bool InvestorPopulateRegData(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            bool isOK = false;
            strErrMsg = "";

            try
            {
                isOK = this._cQuery.ExecProc("dbo.SBNSchedPopulateRegInvestor", out dsOut);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch(Exception ex)
            {
                strErrMsg = "(InvestorPopulateRegData) - " + ex.Message;
                isOK = false;
            }
            return isOK;
        }

        //20221128, rezakahfi, BONDRETAIL-1125, begin
		public bool InvestorPopulateRegData(string ProcessGuid,string WorkflowID,string SID,int IdSeri
                                                , out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            bool isOK = false;
            strErrMsg = "";

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[4];
                dbPar[0] = new OleDbParameter("@puProcessGuid", ProcessGuid);
                dbPar[1] = new OleDbParameter("@pcWorkflowID", WorkflowID);
                dbPar[2] = new OleDbParameter("@pcSID", SID);
                dbPar[3] = new OleDbParameter("@pnIdSeri", IdSeri);

                isOK = this._cQuery.ExecProc("dbo.SBNSchedPopulateRegInvestorForOrder", ref dbPar, out dsOut);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch(Exception ex)
            {
                strErrMsg = "(InvestorPopulateRegData) - " + ex.Message;
                isOK = false;
            }
            return isOK;
        }
		//20221128, rezakahfi, BONDRETAIL-1125, end

        #endregion

        #region "Status Pemesanan ORI"
        public bool StatusORIOrderPopulate(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            bool isOK = false;
            strErrMsg = "";

            try
            {
                isOK = this._cQuery.ExecProc("dbo.SBNSchedGetDataStatusPemesanan", out dsOut);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(StatusORIOrderPopulate) - " + ex.Message;
                isOK = false;
            }
            return isOK;
        }

        public bool StatusORIOrderProcess(
            string strProcessGuid
            , long nOrderId
            , string strIdSeri
            , string strTrxId
            , string strIdStatus
            , string strStatusDesc
            , int nLogStatus
//20190422, uzia, BOSIT18140, begin
            , string strNTPN
//20190422, uzia, BOSIT18140, end
            , string strLogMsg
            , string strLogResponseMsg
            , out string strErrMsg)
        {            
            bool isOK = false;
            strErrMsg = "";            

            try
            {
                //20190422, uzia, BOSIT18140, begin
                //OleDbParameter[] dbPar = new OleDbParameter[9];                
                //dbPar[0] = new OleDbParameter("@puProcessGuid", strProcessGuid);                    			
                //dbPar[1] = new OleDbParameter("@pnOrderId", nOrderId);
                //dbPar[2] = new OleDbParameter("@pcIdSeri", strIdSeri);
                //dbPar[3] = new OleDbParameter("@pcTrxId", strTrxId);
                //dbPar[4] = new OleDbParameter("@pcIdStatus", strIdStatus);
                //dbPar[5] = new OleDbParameter("@pcStatusDesc", strStatusDesc);
                //dbPar[6] = new OleDbParameter("@pnLogStatus", nLogStatus);
                //dbPar[7] = new OleDbParameter("@pcLogMsg", strLogMsg);
                //dbPar[8] = new OleDbParameter("@pcLogResponseMsg", strLogResponseMsg);

                OleDbParameter[] dbPar = new OleDbParameter[10];
                dbPar[0] = new OleDbParameter("@puProcessGuid", strProcessGuid);
                dbPar[1] = new OleDbParameter("@pnOrderId", nOrderId);
                dbPar[2] = new OleDbParameter("@pcIdSeri", strIdSeri);
                dbPar[3] = new OleDbParameter("@pcTrxId", strTrxId);
                dbPar[4] = new OleDbParameter("@pcIdStatus", strIdStatus);
                dbPar[5] = new OleDbParameter("@pcStatusDesc", strStatusDesc);
                dbPar[6] = new OleDbParameter("@pcNTPN", strNTPN);               
                dbPar[7] = new OleDbParameter("@pnLogStatus", nLogStatus);
                dbPar[8] = new OleDbParameter("@pcLogMsg", strLogMsg);
                dbPar[9] = new OleDbParameter("@pcLogResponseMsg", strLogResponseMsg);
                //20190422, uzia, BOSIT18140, end

                isOK = this._cQuery.ExecProc("dbo.SBNSchedUpdateStatusPemesananORI", ref dbPar);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(StatusORIOrderProcess) - " + ex.Message;
                isOK = false;
            }
            return isOK;
        }

        #endregion

        #region "Order ORI Main"
        public bool ORIOrderPopulateMainSched(out DataSet dsOut, out string strErrMsg)
        {
            dsOut = new DataSet();
            bool isOK = false;
            strErrMsg = "";

            try
            {
                isOK = this._cQuery.ExecProc("dbo.SBNSchedPopulateORIOrder", out dsOut);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(ORIOrderPopulateMainSched) - " + ex.Message;
                isOK = false;
            }
            return isOK;
        }

        public bool ORIOrderUpdateResponse(string strGuid, string strWorkflowID, int nStatus, string strMessage, string strXmlResponse, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[5];
                dbPar[0] = new OleDbParameter("@puProcessGuid", strGuid);
                dbPar[1] = new OleDbParameter("@pcWorkflowID", strWorkflowID);
                dbPar[2] = new OleDbParameter("@pnStatus", nStatus);
                dbPar[3] = new OleDbParameter("@pcErrMsg", strMessage);
                dbPar[4] = new OleDbParameter("@pcXmlResponse", strXmlResponse);

                isOK = this._cQuery.ExecProc("dbo.SBNSchedUpdateResponseOrderORI", ref dbPar);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(ORIOrderUpdateResponse) - " + ex.Message;
            }

            return isOK;
        }

       

        #endregion

        #region "Notification"
        public bool NotifUpdateStatus(string strNotifType, string strGuid, string strStatus, string strResponseMsg, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;

            try
            {

                OleDbParameter[] dbPar = new OleDbParameter[4];
                dbPar[0] = new OleDbParameter("@pcNotifType", strNotifType);
                dbPar[1] = new OleDbParameter("@puGuidProcess", strGuid);
                dbPar[2] = new OleDbParameter("@pcStatus", strStatus);
                dbPar[3] = new OleDbParameter("@pcResponseMsg", strResponseMsg);

                isOK = this._cQuery.ExecProc("dbo.SBNSchedUpdateNotifStatus", ref dbPar);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(SBNSchedUpdateNotifStatus) - " + ex.Message;
            }

            return isOK;
        }


        public bool NotifPopulateData(out DataSet dsOut, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;
            dsOut = new DataSet();

            try
            {
                isOK = this._cQuery.ExecProc("dbo.SBNSchedPopulateNotif", out dsOut);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(SBNSchedPopulateNotif) - " + ex.Message;
            }

            return isOK;
        }
        #endregion

        //20231014, rezakahif, BONDRETAIL-1455, begin
        public bool ExecQuery(string strCommand, out DataSet dsOut, out string strErrMsg)
        {
            strErrMsg = "";
            dsOut = new DataSet();
            OleDbParameter[] dbParams = new OleDbParameter[1];
            bool bResult = true;

            try
            {
                dbParams[0] = new OleDbParameter("@pcQuery", strCommand);

                bResult = this._cQuery.ExecProc("dbo.TRSExecQuery", ref dbParams, out dsOut);

                if (!bResult)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec ";
                    }
                }

                return bResult;
            }
            catch (Exception ex)
            {
                strErrMsg = "ExecQuery : " + ex.Message.ToString();
                return false;
            }
        }

        public bool UpdatePushKafka(string WorkflowStatus,string GuidProcess, out string strErrMsg)
        {
            DataSet dsOut = new DataSet();
            
            bool bResult = true;

            try
            {
                string strCommand = @"
                    UPDATE dbo.ORISchedWorkflow_TR
                    SET WorkflowStatus = '" + WorkflowStatus + "'";
	            
                strCommand += @" ,RetryCount = RetryCount+1
                        WHERE WorkflowID = 'PUSH_KAFKA' ";

                strCommand += " and GuidProcess = '" + GuidProcess + "'";

                if (!this.ExecQuery(strCommand, out dsOut, out strErrMsg))
                {
                    bResult = false;
                }

                return bResult;
            }
            catch (Exception ex)
            {
                strErrMsg = "(UpdatePushKafka) - " + ex.Message;

                bResult = false;
            }

            return bResult;
        }
        //20231014, rezakahif, BONDRETAIL-1455, end

        //20240226, uzia, HTR-234, begin
        #region Logger
        public bool LogPaymentDirect(string guidProcess, string paymentGuid, string flag, string payload, string status, string execMessage, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;

            try
            {

                OleDbParameter[] dbPar = new OleDbParameter[6];
                dbPar[0] = new OleDbParameter("@puGuidProcess", guidProcess);
                dbPar[1] = new OleDbParameter("@puPaymentGuid", paymentGuid);
                dbPar[2] = new OleDbParameter("@pcFlag", flag);
                dbPar[3] = new OleDbParameter("@pcPayload", payload);
                dbPar[4] = new OleDbParameter("@pcStatus", status);
                dbPar[5] = new OleDbParameter("@pcErrDesc", execMessage);

                isOK = this._cQuery.ExecProc("dbo.SBNSchedLogORIPaymentDirect", ref dbPar);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(LogPaymentDirect) - " + ex.Message;
            }

            return isOK;
        }
        //20250821, darul.wahid, BONDRETAIL-1721, begin
        public bool LogUnblokirAPI(string guidProcess, string payloadType, string payloadBody, string status, string execMessage, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;

            try
            {

                OleDbParameter[] dbPar = new OleDbParameter[5];
                dbPar[0] = new OleDbParameter("@puGuidProcess", guidProcess);
                dbPar[1] = new OleDbParameter("@pcPayloadType", payloadType);
                dbPar[2] = new OleDbParameter("@pcPayloadBody", payloadBody);
                dbPar[3] = new OleDbParameter("@pcStatus", status);
                dbPar[4] = new OleDbParameter("@pcMessage", execMessage);

                isOK = this._cQuery.ExecProc("dbo.sp_sbn_log_unblokir_api", ref dbPar);
                if (!isOK)
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec stored procedure";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "(LogUnblokirAPI) - " + ex.Message;
            }

            return isOK;
        }
        //20250821, darul.wahid, BONDRETAIL-1721, end
        #endregion
        //20240226, uzia, HTR-234, end
    }
}
