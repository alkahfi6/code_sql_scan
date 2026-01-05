using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NispQuery;
using System.Configuration;
using System.Data.OleDb;
using System.Data;

namespace WSReksa
{
    //20190306, Lita, DIGIT18207, begin

    //20190306, Lita, DIGIT18207, end

    public class clsDataAccessReksa
    {
        private ClsQuery _query;

        public clsDataAccessReksa()
        {
            string server, usname, pwd, dbname;
            AppSettingsReader setting = new AppSettingsReader();
            server = setting.GetValue("Server_REKSA", typeof(string)).ToString();
            usname = setting.GetValue("User_REKSA", typeof(string)).ToString();
            string tempPwd = setting.GetValue("Password", typeof(string)).ToString();
            string tempKey = setting.GetValue("Key", typeof(string)).ToString();
            pwd = nispTextEncriptor.clsTextEncryptor.Decryption(tempPwd, tempKey);
            dbname = setting.GetValue("DB_REKSA", typeof(string)).ToString();

            this._query = new ClsQuery(server, usname, pwd, dbname);
        }

        public bool ReksaEBWInquiryRDBInsurance(string sCIFNo, int iInsProdId, out decimal dAmountRDBIns, out bool bEligibleInsBit
                                                , out string sErrMsg, out string sErrCode)
        {
            bool bOK = false;
            sErrCode = "";
            sErrMsg = "";
            dAmountRDBIns = 0;
            bEligibleInsBit = false;

            OleDbParameter[] dbPar = new OleDbParameter[6];
            dbPar[0] = new OleDbParameter("@pnCIFKey", OleDbType.Decimal);
            dbPar[0].Value = sCIFNo;

            dbPar[1] = new OleDbParameter("@pnInsuranceProdId", OleDbType.Integer);
            dbPar[1].Value = iInsProdId;

            dbPar[2] = new OleDbParameter("@pdAmountRDBInsurance", OleDbType.Decimal);
            dbPar[2].Direction = ParameterDirection.Output;

            dbPar[3] = new OleDbParameter("@pbEligibleInsBit", OleDbType.Boolean);
            dbPar[3].Direction = ParameterDirection.Output;

            dbPar[4] = new OleDbParameter("@pcErrMessage", OleDbType.VarChar,100);
            dbPar[4].Direction = ParameterDirection.Output;

            dbPar[5] = new OleDbParameter("@pcProviderErrCode", OleDbType.VarChar, 5);
            dbPar[5].Direction = ParameterDirection.Output;

            bOK = this._query.ExecProc("dbo.ReksaEBWInquiryRDBInsurance", ref dbPar);

            if (bOK)
            {
                decimal.TryParse(dbPar[2].Value.ToString(), out dAmountRDBIns);  
                bEligibleInsBit = Convert.ToBoolean(dbPar[3].Value.ToString());
                sErrMsg = dbPar[4].Value.ToString();
                sErrCode = dbPar[5].Value.ToString();
            }

            bOK = bOK && sErrMsg.Equals("");

            return bOK;
        }

        public static bool GetAPIUrlNotification(
           NispQuery.ClsQuery cQuery
           , string strNotifKey
           , out string strAPIUrl
           , out string strAPIMethod
           , out string strAPIHeader
           , out string strErrMsg
           )
        {
            strAPIUrl = ""; strAPIMethod = ""; strAPIHeader = ""; strErrMsg = "";
            bool isOK = false;
            DataSet dsOut = new DataSet();

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[1];
                dbPar[0] = new OleDbParameter("@pcNotifType", strNotifKey);
                isOK = cQuery.ExecProc("dbo.ReksaSchedGetNotifAPIParam", ref dbPar, out dsOut);
                if (isOK)
                {
                    if (dsOut.Tables.Count > 0 && dsOut.Tables[0].Rows.Count > 0)
                    {
                        strAPIUrl = dsOut.Tables[0].Rows[0]["APIUrl"].ToString();
                        strAPIMethod = dsOut.Tables[0].Rows[0]["APIMethod"].ToString();
                        strAPIHeader = dsOut.Tables[0].Rows[0]["APIHeader"].ToString();
                    }
                    else
                    {
                        throw new Exception("Parameter not set!");
                    }
                }
                else
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
                            strErrMsg = "Failed exec sp TRSObliGetNotifAPIParam";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "GetAPIUrlNoification : " + ex.Message;
                isOK = false;
            }
            return isOK;
        }

        //20210914, Korvi, RDN-664, begin
        public static bool GetAPIRMMUrlNotification(
            NispQuery.ClsQuery cQuery
            , string strType
            , string strTranId
            , string strTranCode
            , string strRefID
            , string strAuthStatus
            , out string strAPIUrl
            , out string strAPIMethod
            , out string strAPIHeader
            , out string strAPIBody
            , out string strErrMsg
           )
        {
            strAPIUrl = ""; strAPIMethod = ""; strAPIHeader = ""; strAPIBody = ""; strErrMsg = "";
            bool isOK = false;
            DataSet dsOut = new DataSet();

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[5];
                dbPar[0] = new OleDbParameter("@pcType", strType);
                dbPar[1] = new OleDbParameter("@pcTranId", strTranId);
                dbPar[2] = new OleDbParameter("@pcTranCode", strTranCode);
                dbPar[3] = new OleDbParameter("@pcRefID", strRefID);
                dbPar[4] = new OleDbParameter("@pcAuthStatus", strAuthStatus);

                isOK = cQuery.ExecProc("dbo.ReksaRMMGetNotifAPIParam", ref dbPar, out dsOut);
                if (isOK)
                {
                    if (dsOut.Tables.Count > 0 && dsOut.Tables[0].Rows.Count > 0)
                    {
                        strAPIUrl = dsOut.Tables[0].Rows[0]["APIUrl"].ToString();
                        strAPIMethod = dsOut.Tables[0].Rows[0]["APIMethod"].ToString();
                        strAPIHeader = dsOut.Tables[0].Rows[0]["APIHeader"].ToString();
                        strAPIBody = dsOut.Tables[0].Rows[0]["APIBody"].ToString();
                    }
                    else
                    {
                        throw new Exception("Parameter not set!");
                    }
                }
                else
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
                            strErrMsg = "Failed exec sp ReksaRMMGetNotifAPIParam";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "GetAPIUrlNotification : " + ex.Message;
                isOK = false;
            }
            return isOK;
        }

        public static bool ReksaSaveNotifLog(
            NispQuery.ClsQuery cQuery
            , string strNotifChannel
            , string strModule
            , string strRefID
            , string strTranId
            , string strTranCode
            , string strRqBody
            , string strRsStatus
            , string strRsStatusDesc
            , string strRsBody
            , string strReceiptNo
            , out string strErrMsg
           )
        {
            strErrMsg = ""; 
            bool isOK = false;
            DataSet dsOut = new DataSet();

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[10];
                dbPar[0] = new OleDbParameter("@pcNotifChannel", strNotifChannel);
                dbPar[1] = new OleDbParameter("@pcModule", strModule);
                dbPar[2] = new OleDbParameter("@pcRefID", strRefID);
                dbPar[3] = new OleDbParameter("@pnTranId", strTranId);
                dbPar[4] = new OleDbParameter("@pcTranCode", strTranCode);
                
                dbPar[5] = new OleDbParameter("@pcRqBody", strRqBody);
                dbPar[6] = new OleDbParameter("@pcRsStatus", strRsStatus);
                dbPar[7] = new OleDbParameter("@pcRsStatusDesc", strRsStatusDesc);
                dbPar[8] = new OleDbParameter("@pcRsBody", strRsBody);
                dbPar[9] = new OleDbParameter("@pcReceiptNo", strReceiptNo);

                isOK = cQuery.ExecProc("dbo.ReksaSaveNotifLog", ref dbPar);
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
                            strErrMsg = "Failed exec sp ReksaSaveNotifLog";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "ReksaSaveNotifLog : " + ex.Message;
                isOK = false;
            }
            return isOK;
        }
        //20210914, Korvi, RDN-664, end
    }
}