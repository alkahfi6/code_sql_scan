using System;
using System.Data;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
//20190115, uzia, DIGIT18207, begin
using System.Text;
//20190115, uzia, DIGIT18207, end

namespace wsProObligasi
{
    public class clsDataAccess
    {
        public static bool ExecStoredProcedure(
            String ConnectionString,
            String StoredProcedureName,
            ref OleDbParameter[] Param,
            int Timeout,
            out DataSet myDataSet,
            out String StrError)
        {
            bool bReturn = true;
            OleDbConnection myConn = new OleDbConnection();
            OleDbCommand myCommand;
            OleDbDataAdapter myAdapter;

            StrError = "";
            myDataSet = new DataSet();

            try
            {
                myConn.ConnectionString = ConnectionString;
                myConn.Open();
                myCommand = new OleDbCommand();
                myCommand.Connection = myConn;
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandTimeout = Timeout;
                myCommand.CommandText = StoredProcedureName;
                myCommand.Parameters.AddRange(Param);
                myAdapter = new OleDbDataAdapter(myCommand);
                myAdapter.Fill(myDataSet);
                myConn.Close();
            }
            catch (Exception ex)
            {
                StrError = ex.Message;
                if (myConn != null && myConn.State == ConnectionState.Connecting) myConn.Close();
                bReturn = false;
            }
            return bReturn;
        }

        public static bool ExecStoredProcedureODBC(
            String ConnectionString,
            String StoredProcedureName,
            ref OdbcParameter[] Param,
            int Timeout,
            out DataSet myDataSet,
            out String StrError)
        {
            bool bReturn = true;
            OdbcConnection myConn = new OdbcConnection();
            OdbcCommand myCommand;
            OdbcDataAdapter myAdapter;

            StrError = "";
            myDataSet = new DataSet();

            try
            {
                myConn.ConnectionString = ConnectionString;
                myConn.Open();
                myCommand = new OdbcCommand();
                myCommand.Connection = myConn;
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandTimeout = Timeout;
                myCommand.CommandText = StoredProcedureName;
                myCommand.Parameters.AddRange(Param);
                myAdapter = new OdbcDataAdapter(myCommand);
                myAdapter.Fill(myDataSet);
                myConn.Close();
            }
            catch (Exception ex)
            {
                StrError = ex.Message;
                if (myConn != null && myConn.State == ConnectionState.Connecting) myConn.Close();
                bReturn = false;
            }
            return bReturn;
        }

        //20190115, uzia, DIGIT18207, begin
        #region "ONE Mobile"

        #region "NTI"
        //20190225, uzia, DIGIT18207, begin
        public static bool NTIUpdateReviewedCustomerID(NispQuery.ClsQuery cQuery, string strCIFNo, out string strErrMsg)
        {
            strErrMsg = "";

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[1];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", strCIFNo);

            bool isOK = cQuery.ExecProc("dbo.OMUpdateReviewedCustomerID", ref dbPar);

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
                        strErrMsg = "Failed exec sp OMUpdateReviewedCustomerID";
                }
            }

            return isOK;
        }
        //20190225, uzia, DIGIT18207, end
        public static bool ValidateCustomer(NispQuery.ClsQuery cQuery, string strCIFNo, out bool isCustValid, out string strXmlOut, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;
            isCustValid = false;
            strXmlOut = "";

            DataSet dsOut = new DataSet();

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[2];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", strCIFNo);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pbExists", System.Data.OleDb.OleDbType.Boolean);
            dbPar[1].Direction = ParameterDirection.Output;

            isOK = cQuery.ExecProc("dbo.OMValidateCustomer", ref dbPar, out dsOut);
            if (isOK)
            {
                isCustValid = (bool)dbPar[1].Value;

                dsOut.DataSetName = "Data";
                dsOut.Tables[0].TableName = "NTI";
                dsOut.Tables[1].TableName = "Address";

                strXmlOut = dsOut.GetXml();

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
                        strErrMsg = "Failed exec sp OMValidateCustomer";
                }
            }

            return isOK;
        }

        public static bool NTIInsertNew(NispQuery.ClsQuery cQuery, int nSystemNIK, string XmlInput, out long nCIFId, out string strXMLOut, out string strErrMsg)
        {
            bool isOK = false;
            DataSet dsOut = new DataSet();
            string strSecAccNo = "";
            string xmlInternal = "";
            string strGUID = "";
            strXMLOut = "";
            strErrMsg = "";
            nCIFId = 0;

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
            //20190227, uzia, DIGIT18207, begin
            dtTempInternal.Columns.Add("Type");
            //20190227, uzia, DIGIT18207, end

            try
            {
                System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[7];
                dbPar[0] = new System.Data.OleDb.OleDbParameter("@xmlInput", XmlInput);
                dbPar[1] = new System.Data.OleDb.OleDbParameter("@piSignature", null);
                dbPar[2] = new System.Data.OleDb.OleDbParameter("@pbIsOnlineAcc", false);
                dbPar[3] = new System.Data.OleDb.OleDbParameter("@piSummary", null);
                dbPar[4] = new System.Data.OleDb.OleDbParameter("@pxXMLDocs", null);
                dbPar[5] = new System.Data.OleDb.OleDbParameter("@cRegViaOneMobile", "Y");
                dbPar[6] = new System.Data.OleDb.OleDbParameter("@pcSecAccNo", System.Data.OleDb.OleDbType.VarChar, 10);
                dbPar[6].Direction = ParameterDirection.Output;

                //20191018, samy, LOGEN00939, begin
                //isOK = cQuery.ExecProc("dbo.trs_InsertTreasuryCustomer_TM", ref dbPar, out dsOut);
                isOK = cQuery.ExecProc("dbo.OMBranchingNTI", ref dbPar, out dsOut);
                //20191018, samy, LOGEN00939, end

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
                            strErrMsg = "Failed exec sp trs_InsertTreasuryCustomer_TM";
                    }
                }

                if (!strErrMsg.Equals(""))
                    throw new Exception(strErrMsg);

                /*** get data for murex ***/
                strSecAccNo = dbPar[6].Value.ToString();

                dbPar = new System.Data.OleDb.OleDbParameter[1];
                dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcSecAccNo", strSecAccNo);

                if (!cQuery.ExecProc("dbo.TRSPopulateMasterNasabahForMurex", ref dbPar, out dsOut))
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
                            strErrMsg = "Failed exec sp TRSPopulateMasterNasabahForMurex";
                    }
                }

                if (!strErrMsg.Equals(""))
                    throw new Exception(strErrMsg);

                if (dsOut.Tables[0].Rows.Count == 0)
                    throw new Exception("No data for murex interface");

                long.TryParse(dsOut.Tables[0].Rows[0]["CIFId"].ToString(), out nCIFId);

                DataRow drInternal;
                string[] alamat = dsOut.Tables[0].Rows[0]["AlamatIdentitas"].ToString().Split('&');
                string N2 = alamat[0];
                string N3 = alamat[1];
                string N4 = alamat[2];
                drInternal = dtTempInternal.NewRow();
                drInternal[0] = dsOut.Tables[0].Rows[0]["Guid"].ToString();
                drInternal[1] = "INSERT";
                drInternal[2] = dsOut.Tables[0].Rows[0]["mxLabel"].ToString();
                drInternal[3] = dsOut.Tables[0].Rows[0]["CUS1"].ToString();
                drInternal[4] = dsOut.Tables[0].Rows[0]["CUN"].ToString();
                drInternal[5] = dsOut.Tables[0].Rows[0]["CTP1"].ToString();
                drInternal[6] = N2;
                drInternal[7] = N3;
                drInternal[8] = N4;
                drInternal[9] = dsOut.Tables[0].Rows[0]["NA5"].ToString();
                drInternal[10] = dsOut.Tables[0].Rows[0]["PHN"].ToString();
                drInternal[11] = dsOut.Tables[0].Rows[0]["FAX"].ToString();
                drInternal[12] = dsOut.Tables[0].Rows[0]["NPWP"].ToString();
                drInternal[13] = dsOut.Tables[0].Rows[0]["COUNTRY"].ToString();
                drInternal[14] = dsOut.Tables[0].Rows[0]["EMAIL"].ToString();
                drInternal[15] = dsOut.Tables[0].Rows[0]["RELT"].ToString();
                drInternal[16] = dsOut.Tables[0].Rows[0]["CORP"].ToString();
                drInternal[17] = dsOut.Tables[0].Rows[0]["CTP"].ToString();
                drInternal[18] = dsOut.Tables[0].Rows[0]["RES"].ToString();
                drInternal[19] = dsOut.Tables[0].Rows[0]["RMCOD"].ToString();
                drInternal[20] = dsOut.Tables[0].Rows[0]["SLBHU"].ToString();
                drInternal[21] = dsOut.Tables[0].Rows[0]["CITIZEN"].ToString();
                //20190227, uzia, DIGIT18207, begin
                drInternal[22] = "BOND";
                //20190227, uzia, DIGIT18207, end

                dtTempInternal.Rows.Add(drInternal);
                xmlInternal = clsXML.GetXMLCTP(dtTempInternal);
                //20190227, uzia, DIGIT18207, begin
                //strGUID = System.Guid.NewGuid().ToString();

                //StringBuilder strXMLSend = new StringBuilder();

                //clsMessagingTIBCO.ServiceHeaderMBASE5("77777", strGUID, ref strXMLSend, "ProObligasi");
                //strXMLSend.Replace("[SERVICENAME]", "OBL_Add_BONDCTP");
                //strXMLSend.Replace("[OPERATIONCODE]", "000");
                //strXMLSend.Replace("[REQUESTDETAILFIELD]", "<ns1:CIF>" + xmlInternal.Replace("<", "&lt;") + "</ns1:CIF>");
                //strXMLSend.Replace("[MOREINDICATOR]", "N");

                //strXMLOut = strXMLSend.ToString();

                StringBuilder strXMLSend = new StringBuilder();
                strGUID = System.Guid.NewGuid().ToString();
                clsMessagingTIBCO.ServiceHeaderMBASE3(nSystemNIK.ToString(), ref strXMLSend, strGUID, "ProObligasi");
                strXMLSend.Replace("[SERVICENAME]", "OBL_Add_FLDCTP");
                strXMLSend.Replace("[OPERATIONCODE]", "000");
                strXMLSend.Replace("[REQUESTDETAILFIELD]", "<ns1:CIF>" + xmlInternal.Replace("<", "&lt;") + "</ns1:CIF>");
                strXMLSend.Replace("[MOREINDICATOR]", "N");

                strXMLOut = strXMLSend.ToString();
                //20190227, uzia, DIGIT18207, end
            }
            catch (Exception ex)
            {
                strErrMsg = ex.Message;
                isOK = false;
            }

            return isOK;
        }

        public static bool UpdateStatusTreasuryCust(NispQuery.ClsQuery cQuery, string strStatus, long nCIFId)
        {
            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[2];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcStatus", strStatus);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pnCIFId", nCIFId);

            return cQuery.ExecProc("dbo.OMUpdateStatusTreasuryCust", ref dbPar);
        }
        #endregion

        #region Support
        public static bool GetSystemNIK(NispQuery.ClsQuery cQuery, out int nNIK)
        {
            nNIK = 0;
            bool isOK = false;

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[1];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pnNIK", OleDbType.Integer);
            dbPar[0].Direction = ParameterDirection.Output;

            isOK = cQuery.ExecProc("dbo.OMGetSystemNIK", ref dbPar);

            if (isOK)
            {
                nNIK = int.Parse(dbPar[0].Value.ToString());
            }

            return isOK;
        }
        //20220629, darul.wahid, BONDRETAIL-979, begin
        public static bool GetAPIUrlNotificationV2(
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
                OleDbParameter[] dbPar = new OleDbParameter[2];
                dbPar[0] = new OleDbParameter("@pcNotifType", strNotifKey);
                dbPar[1] = new OleDbParameter("@pcParamValue", (strNotifKey == "MAIL" || strNotifKey == "NOTIF") ? "OneNotif" : "");                
                isOK = cQuery.ExecProc("dbo.TRSObliGetNotifAPIParam", ref dbPar, out dsOut);
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
        //insert log, begin
        public static bool InsertObliNotifLog(
            NispQuery.ClsQuery cQuery
            , string strNotifType
            , string strBody
            , out string strErrMsg
            )
        {
            strErrMsg = "";
            bool isOK = false;
            DataSet dsOut = new DataSet();

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[2];
                dbPar[0] = new OleDbParameter("@pcNotifType", strNotifType);
                dbPar[1] = new OleDbParameter("@pcBody", strBody);
                isOK = cQuery.ExecProc("dbo.TRSCallNotifAPILog", ref dbPar, out dsOut);
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
                            strErrMsg = "Failed exec sp TRSCallNotifAPILog";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "InsertObliNotifLog : " + ex.Message;
                isOK = false;
            }
            return isOK;
        }
        //insert log, end
        //20220629, darul.wahid, BONDRETAIL-979, end
        //20190227, uzia, BOSIT18140, begin
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
                isOK = cQuery.ExecProc("dbo.TRSObliGetNotifAPIParam", ref dbPar, out dsOut);
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
        public static bool GetAPIUrl(NispQuery.ClsQuery cQuery
            , string strAPICode
            , out string strAPIUrl
            , out string strBaseAPIUrl
            , out string strAPIMethod
            , out string strAPIHeader
            , out bool isPath
            , out string strErrMsg)
        {
            strErrMsg = "";
            strAPIUrl = "";
            strAPIMethod = "";
            strAPIHeader = "";
            strBaseAPIUrl = "";
            DataSet dsOut = new DataSet();
            bool isOK = false;
            isPath = false;
            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[1];
                dbPar[0] = new OleDbParameter("@pcAPICode", strAPICode);
                isOK = cQuery.ExecProc("dbo.TRSObliGetAPI", ref dbPar, out dsOut);
                if (isOK)
                {
                    if (dsOut.Tables.Count > 0 && dsOut.Tables[0].Rows.Count > 0)
                    {
                        strAPIUrl = dsOut.Tables[0].Rows[0]["APIUrl"].ToString();
                        strAPIMethod = dsOut.Tables[0].Rows[0]["APIMethod"].ToString();
                        strAPIHeader = dsOut.Tables[0].Rows[0]["APIHeader"].ToString();
                        isPath = bool.Parse(dsOut.Tables[0].Rows[0]["PathBit"].ToString());
                        strBaseAPIUrl = dsOut.Tables[0].Rows[0]["APIBaseUrl"].ToString();
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
                            strErrMsg = "Failed exec sp TRSObliGetAPI";
                    }
                }
            }
            catch (Exception ex)
            {
                strErrMsg = "GetAPIUrl : " + ex.Message;
                return false;
            }
            return isOK;
        }
        public static bool APICreateLog(
            NispQuery.ClsQuery cQuery
            , string strAPICode
            , string strXMLInput
            , string strXMLOutput
            , string strAPIResponse
            , string strTIBCOGuid
            , int nProcessStatus
            , string strErrMsg
            )
        {
            OleDbParameter[] dbPar = new OleDbParameter[7];
            dbPar[0] = new OleDbParameter("@pcAPICode", strAPICode);
            dbPar[1] = new OleDbParameter("@pcXMLInput", strXMLInput);
            dbPar[2] = new OleDbParameter("@pcXMLOutput", strXMLOutput);
            dbPar[3] = new OleDbParameter("@pcAPIResponse", strAPIResponse);
            dbPar[4] = new OleDbParameter("@pcTIBCOGuid", strTIBCOGuid);
            dbPar[5] = new OleDbParameter("@pnProcessStatus", nProcessStatus);
            dbPar[6] = new OleDbParameter("@pcErrMsg", strErrMsg);
            return cQuery.ExecProc("dbo.TRSObliAPICreateLog", ref dbPar);
        }
        public static bool APIGetErrorMapping(NispQuery.ClsQuery cQuery, out DataSet dsOut)
        {
            dsOut = new DataSet();
            return cQuery.ExecProc("dbo.TRSORIGetAPIErrorMapping", out dsOut);
        }
        public static bool ConvertTIBCOResponse(NispQuery.ClsQuery cQuery, string strResponse, out DataSet dsOut, out string strErrMsg)
        {
            bool isOK = false;
            strErrMsg = "";
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnNIK", "0");
            dbPar[1] = new OleDbParameter("@pvGuid", "");
            dbPar[2] = new OleDbParameter("@pxData", strResponse);
            isOK = cQuery.ExecProc("dbo.ProCIFP2Parse", ref dbPar, out dsOut);
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
                        strErrMsg = "Failed exec sp ProCIFP2Parse";
                }
            }
            return isOK;
        }
        #endregion

        #region "ORI Perdana"
        public static bool ORI_Online_PopulateHistory(
            NispQuery.ClsQuery cQuery
            , string strHistType
            , string strCIFNo
            , string strSecurityNo
            , out DataSet dsOut
            , out string strErrMsg
            , out string strErrCode
            )
        {
            strErrMsg = "";
            strErrCode = "";
            dsOut = new DataSet();
            bool isOK = false;
            try
            {
                //20190409, uzia, DIGIT18207, begin
                //OleDbParameter[] dbPar = new OleDbParameter[4];
                ///dbPar[0] = new OleDbParameter("@pcHistoryType", strHistType);
                //dbPar[1] = new OleDbParameter("@pcCIFNo", strCIFNo);
                //dbPar[2] = new OleDbParameter("@pcSecurityNo", strSecurityNo);
                //dbPar[3] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 5);
                //dbPar[3].Direction = ParameterDirection.Output;
                //isOK = cQuery.ExecProc("dbo.TRSORIOnlinePopulateHist", ref dbPar, out dsOut);
                //if (!isOK)
                //{					
                //    strErrCode = dbPar[4].Value.ToString();
                //    while (NispQuery.ClsQuery.queError.Count > 0)
                //   {
                //        string[] errList = NispQuery.ClsQuery.queError.Dequeue();
                //        if (errList.Length >= 2)
                //        {
                //            for (int i = 1; i < errList.Length; i++)
                //                strErrMsg += errList[i] + System.Environment.NewLine;
                //        }
                //        else
                //            strErrMsg = "Failed exec sp TRSORIOnlineGetBlockAccountData";
                //    }
                //    throw new Exception(strErrMsg);
                //}
                OleDbParameter[] dbPar = new OleDbParameter[5];
                dbPar[0] = new OleDbParameter("@pcHistoryType", strHistType);
                dbPar[1] = new OleDbParameter("@pcCIFNo", strCIFNo);
                dbPar[2] = new OleDbParameter("@pcSecurityNo", strSecurityNo);
                dbPar[3] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 5);
                dbPar[3].Direction = ParameterDirection.Output;
                dbPar[4] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 1000);
                dbPar[4].Direction = ParameterDirection.Output;
                isOK = cQuery.ExecProc("dbo.TRSORIOnlinePopulateHist", ref dbPar, out dsOut);
                if (!isOK)
                {
                    strErrCode = "01000";
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();
                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                strErrMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            strErrMsg = "Failed exec sp TRSORIOnlineGetBlockAccountData";
                    }
                    throw new Exception(strErrMsg);
                }
                else
                {
                    strErrCode = dbPar[3].Value.ToString();
                    strErrMsg = dbPar[4].Value.ToString();

                    if (!strErrCode.Equals("") || !strErrMsg.Equals(""))
                        throw new Exception(strErrMsg);
                }

                //20190409, uzia, DIGIT18207, end
                return true;
            }
            catch (Exception ex)
            {
                strErrMsg = ex.Message;
                return false;
            }
        }
        public static bool ORI_Online_GetBlockAccountData(
            NispQuery.ClsQuery cQuery
            , string strCIFNo
            , out DataSet dsOut
            , out string strErrMsg
            )
        {
            bool isOK = false;
            strErrMsg = "";
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcCIFNo", strCIFNo);
            isOK = cQuery.ExecProc("dbo.TRSORIOnlineGetBlockAccountData", ref dbPar, out dsOut);
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
                        strErrMsg = "Failed exec sp TRSORIOnlineGetBlockAccountData";
                }
            }
            return isOK;
        }
        public static bool ORI_Online_NewTransaction(
            NispQuery.ClsQuery cQuery
            , string strXmlData
            , out string strErrMsg
            , out string strErrCode
            , out string strXmlOut
			//20230814, tobias, BONDRETAIL-1414, begin
            , string strGuid
            //20230814, tobias, BONDRETAIL-1414, end
            )
        {
            bool isOK = false;
            strErrMsg = "";
            strErrCode = "";
            strXmlOut = "";
            DataSet dsOut = new DataSet();
            //20230814, tobias, BONDRETAIL-1414, begin
            //OleDbParameter[] dbPar = new OleDbParameter[3];
            OleDbParameter[] dbPar = new OleDbParameter[4];
            //20230814, tobias, BONDRETAIL-1414, end
            dbPar[0] = new OleDbParameter("@pcXmlData", strXmlData);
            dbPar[1] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 1000);
            dbPar[1].Direction = ParameterDirection.Output;
            dbPar[2] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 5);
            dbPar[2].Direction = ParameterDirection.Output;
			//20230814, tobias, BONDRETAIL-1414, begin
            dbPar[3] = new OleDbParameter("@pcGuid", strGuid);
            //20230814, tobias, BONDRETAIL-1414, end
            isOK = cQuery.ExecProc("dbo.TRSORIOnlineNewTransaction", ref dbPar, out dsOut);
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
                        strErrMsg = "Failed exec sp TRSORIOnlineNewTransaction";
                }
                //20190426, uzia, BOSIT18140, begin
                //if (dbPar[2] != null)
                //    strErrCode = dbPar[2].Value.ToString();
                strErrCode = "01000";
                //20190426, uzia, BOSIT18140, end
            }
            else
            {
                //20190426, uzia, BOSIT18140, begin
                if (dbPar[1].Value != null && dbPar[2].Value != null)
                {
                    if (!dbPar[1].Value.Equals(""))
                    {
                        strErrMsg = dbPar[1].Value.ToString();
                        strErrCode = dbPar[2].Value.ToString();
                        return false;
                    }
                }
                //20190426, uzia, BOSIT18140, end

                dsOut.DataSetName = "data";
                dsOut.Tables[0].TableName = "Order";
                strXmlOut = dsOut.GetXml();
            }
            return isOK;
        }
        public static bool ORI_Online_CheckProductEligibility(
            NispQuery.ClsQuery cQuery
            , int nIdSeri
            , string strCIFNo
            , out string strSID
            , out bool isEligible
            , out string strErrCode
            , out string strErrMsg
            )
        {
            bool isOK = false;
            strErrMsg = "";
            strErrCode = "";
            strSID = "";
            isEligible = false;
            OleDbParameter[] dbPar = new OleDbParameter[6];
            dbPar[0] = new OleDbParameter("@pnIdSeri", nIdSeri);
            dbPar[1] = new OleDbParameter("@pcCIFNo", strCIFNo);
            dbPar[2] = new OleDbParameter("@pcSID", OleDbType.VarChar, 100);
            dbPar[2].Direction = ParameterDirection.Output;
            dbPar[3] = new OleDbParameter("@pbIsEligible", OleDbType.Boolean);
            dbPar[3].Direction = ParameterDirection.Output;
            dbPar[4] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 10);
            dbPar[4].Direction = ParameterDirection.Output;
            dbPar[5] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 1000);
            dbPar[5].Direction = ParameterDirection.Output;
            isOK = cQuery.ExecProc("dbo.TRSORICheckProductEligibility", ref dbPar);
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
                        strErrMsg = "Failed exec sp TRSORICheckProductEligibility";
                }
            }
            else
            {
                strErrMsg = dbPar[5].Value.ToString();
                strErrCode = dbPar[4].Value.ToString();
                isEligible = (bool)dbPar[3].Value;
                strSID = dbPar[2].Value.ToString();
            }
            return isOK;
        }
        public static bool ORI_Online_UpdatePaymentStatus(
            NispQuery.ClsQuery cQuery
            , string strKodePemesanan
            , string strKodeBilling
            , string strNTPN
            , string strNTB
            , string strTglPembayaran
            , string strBankPersepsi
            , string strChannelPembayaran
            , out string strErrMsg
        )
        {
            bool isOK = false;
            strErrMsg = "";
            OleDbParameter[] dbPar = new OleDbParameter[7];
            dbPar[0] = new OleDbParameter("@pcKodePemesanan", strKodePemesanan);
            dbPar[1] = new OleDbParameter("@pcKodeBilling", strKodeBilling);
            dbPar[2] = new OleDbParameter("@pcNTPN", strNTPN);
            dbPar[3] = new OleDbParameter("@pcNTB", strNTB);
            dbPar[4] = new OleDbParameter("@pcTglPembayaran", strTglPembayaran);
            dbPar[5] = new OleDbParameter("@pcBankPersepsi", strBankPersepsi);
            dbPar[6] = new OleDbParameter("@pcChannelPembayaran", strChannelPembayaran);
            isOK = cQuery.ExecProc("dbo.TRSORIUpdateStatusPembayaranOnline", ref dbPar);
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
                        strErrMsg = "Failed exec sp TRSORIUpdateStatusPembayaranOnline";
                }
            }
            return isOK;
        }
        public static bool ORI_Online_GetProductDetail(NispQuery.ClsQuery cQuery, int nIdSeri, string strCIFNo, out DataSet dsOut, out string strErrCode, out string strErrMsg)
        {
            bool isOK = false;
            strErrMsg = "";
            strErrCode = "";
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcIdSeri", nIdSeri);
            dbPar[1] = new OleDbParameter("@pcCIFNo", strCIFNo);
            dbPar[2] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 10);
            dbPar[2].Direction = ParameterDirection.Output;
            dbPar[3] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 1000);
            dbPar[3].Direction = ParameterDirection.Output;
            isOK = cQuery.ExecProc("dbo.TRSORIGetProductDetail", ref dbPar, out dsOut);
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
                        strErrMsg = "Failed exec sp TRSORIGetProductDetail";
                }
            }
            else
            {
                strErrCode = dbPar[2].Value.ToString();
                strErrMsg = dbPar[3].Value.ToString();
            }
            return isOK;
        }
        //20190227, uzia, BOSIT18140, end
        //20190227, rezakahfi, BOSIT18140, begin
        public static bool ORI_Online_GetRegisterInvestor(NispQuery.ClsQuery cQuery
            , string NoCIF
            , string IdSeri
            , out string strXmlOut
            , out string strErrCode
            , out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;
            strXmlOut = "";
            strErrCode = "";
            strErrMsg = "";
            DataSet dsOut = new DataSet();
            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[2];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcNoCIF", NoCIF);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pnIdSeri", IdSeri);
            isOK = cQuery.ExecProc("dbo.ORISBNIPopulateRegisterInvestor", ref dbPar, out dsOut);
            if (isOK)
            {
                dsOut.DataSetName = "data";
                dsOut.Tables[0].TableName = "row";
                strErrCode = dsOut.Tables[1].Rows[0]["ErrCode"].ToString();
                strErrMsg = dsOut.Tables[1].Rows[0]["ErrMsg"].ToString();
                dsOut.Tables.RemoveAt(1);
                strXmlOut = dsOut.GetXml();
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
                        strErrMsg = "Failed exec sp ORISBNIPopulateRegisterInvestor";
                }
            }
            return isOK;
        }
        public static bool ORI_Online_SubmitRegisterInvestorPending(NispQuery.ClsQuery cQuery
            , string strXML
            , out string strErrCode
            , out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;
            strErrCode = "";
            strErrMsg = "";
            DataSet dsOut = new DataSet();
            strXML = clsXML.DecodeXML(strXML);
            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[1];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcXML", strXML);
            isOK = cQuery.ExecProc("dbo.ORISBNISubmitInvestorPending", ref dbPar, out dsOut);
            if (isOK)
            {
                strErrCode = dsOut.Tables[0].Rows[0]["ErrCode"].ToString();
                strErrMsg = dsOut.Tables[0].Rows[0]["ErrMsg"].ToString();
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
                        strErrMsg = "Failed exec sp ORISBNISubmitInvestorPending";
                }
            }
            return isOK;
        }
        public static bool ORI_Online_GetEarlyRedemptionData(NispQuery.ClsQuery cQuery
            , string TypeData
            , string NoCIF
            , int SecId
            , out string strXmlOut
            , out string strErrCode
            , out string strErrMsg
            )
        {
            strErrMsg = "";
            bool isOK = false;
            strXmlOut = "";
            strErrCode = "";
            strErrMsg = "";
            DataSet dsOut = new DataSet();
            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[3];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcTypeData", TypeData);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", NoCIF);
            dbPar[2] = new System.Data.OleDb.OleDbParameter("@pnSeriId", SecId);
            isOK = cQuery.ExecProc("dbo.ORISBNIPopulateEarlyRedemption", ref dbPar, out dsOut);
            if (isOK)
            {
                if (dsOut.Tables.Count > 1)
                {
                    dsOut.DataSetName = "data";
                    dsOut.Tables[0].TableName = "row";
                    strErrCode = dsOut.Tables[1].Rows[0]["ErrCode"].ToString();
                    strErrMsg = dsOut.Tables[1].Rows[0]["ErrMsg"].ToString();
                    dsOut.Tables.RemoveAt(1);
                    strXmlOut = dsOut.GetXml();
                }
                else
                {
                    strErrCode = dsOut.Tables[0].Rows[0]["ErrCode"].ToString();
                    strErrMsg = dsOut.Tables[0].Rows[0]["ErrMsg"].ToString();
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
                        strErrMsg = "Failed exec sp ORISBNIPopulateEarlyRedemption";
                }
            }
            return isOK;
        }
        public static bool ORISBNISubmitEarlyRedemption(NispQuery.ClsQuery cQuery
            , string strXML
            , string strGuid
            , bool isAfterAPI
            , bool isSuccess
            //, out string EarlyId
            //, out string strXMLRespon
            , out DataSet dsEarlyRedemp
            , out string strErrCode
            , out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;
            strErrCode = "";
            strErrMsg = "";
            //strXMLRespon = "";
            //EarlyId = "";
            dsEarlyRedemp = new DataSet();

            DataSet dsOut = new DataSet();
            //strGuid = System.Guid.NewGuid().ToString();
            strXML = clsXML.DecodeXML(strXML);
            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[4];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcXML", strXML);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@puGuid", strGuid);
            dbPar[2] = new System.Data.OleDb.OleDbParameter("@pbIsAfterAPI", isAfterAPI);
            dbPar[3] = new System.Data.OleDb.OleDbParameter("@pbIsSuccess", isSuccess);

            isOK = cQuery.ExecProc("dbo.ORISBNISubmitEarlyRedemption", ref dbPar, out dsOut);

            if (isOK)
            {
                if (dsOut.Tables.Count > 1)
                {
                    //set name tag xml
                    dsOut.DataSetName = "data";
                    dsOut.Tables[0].TableName = "row";

                    //set err msg
                    int intPosError = dsOut.Tables.Count - 1;
                    strErrCode = dsOut.Tables[intPosError].Rows[0]["ErrCode"].ToString();
                    strErrMsg = dsOut.Tables[intPosError].Rows[0]["ErrMsg"].ToString();

                    //EarlyId = dsOut.Tables[0].Rows[0]["EarlyId"].ToString();

                    //drop table error
                    dsOut.Tables.RemoveAt(intPosError);

                    dsOut.Tables[0].AcceptChanges();
                    //dsOut.Tables[0].Columns.Remove("EarlyId");
                    dsEarlyRedemp = dsOut;
                    dsOut.Tables[0].AcceptChanges();

                    //create xml
                    //strXMLRespon = dsOut.GetXml();
                }
                else
                {
                    //set err msg
                    strErrCode = dsOut.Tables[0].Rows[0]["ErrCode"].ToString();
                    strErrMsg = dsOut.Tables[0].Rows[0]["ErrMsg"].ToString();
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
                        strErrMsg = "Failed exec sp ORISBNISubmitEarlyRedemption";
                }
            }

            return isOK;
        }
        #endregion
        //20190401, uzia, DIGIT18207, begin
        #region "Transaksi Sekunder"
        public static bool TrxSecGetListProduct(NispQuery.ClsQuery cQuery
            , string cifNo
            , int criteria
            , string custBuySell
            , int secId
            , out string xmlOut
            , out string errMsg
            , out string errCode)
        {
            errMsg = ""; errCode = ""; xmlOut = "";
            DataSet dsOut = new DataSet();

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[6];
                dbPar[0] = new OleDbParameter("@pcCIFNo", cifNo);
                dbPar[1] = new OleDbParameter("@pnCriteria", criteria);
                dbPar[2] = new OleDbParameter("@pcCustBuySell", custBuySell);
                dbPar[3] = new OleDbParameter("@pnSecId", secId);
                dbPar[4] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 1000);
                dbPar[4].Direction = ParameterDirection.Output;
                dbPar[5] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 10);
                dbPar[5].Direction = ParameterDirection.Output;

                if (cQuery.ExecProc("dbo.OMGetListProductSekunder", ref dbPar, out dsOut))
                {
                    errCode = dbPar[5].Value.ToString();
                    errMsg = dbPar[4].Value.ToString();
                    if (!errCode.Equals("") || !errMsg.Equals(""))
                        throw new Exception(errMsg);

                    if (dsOut.Tables.Count == 0)
                        throw new Exception("Data not found");

                    dsOut.DataSetName = "Data";
                    dsOut.Tables[0].TableName = "ProdList";
                    dsOut.Tables[1].TableName = "DocLink";

                    xmlOut = dsOut.GetXml();
                }
                else
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();
                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                errMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            errMsg = "Failed exec sp OMGetListProductSekunder";
                    }

                    throw new Exception(errMsg);
                }

                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        public static bool TrxSecGetListTransaction(NispQuery.ClsQuery cQuery
            , string secAccNo
            , string secNo
            , out string xmlOut
            , out string errMsg
            , out string errCode
           )
        {
            DataSet dsOut = new DataSet();
            xmlOut = "";
            errMsg = "";
            errCode = "";

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[2];
                dbPar[0] = new OleDbParameter("@pcSecAccNo", secAccNo);
                dbPar[1] = new OleDbParameter("@pcSecurityNo", secNo);

                if (cQuery.ExecProc("dbo.TRSPopulateTransactionBalance", ref dbPar, out dsOut))
                {
                    if (dsOut.Tables.Count == 0)
                        throw new Exception("Data not found");

                    if (dsOut.Tables[0].Rows.Count == 0)
                    {
                        errCode = "70005";
                        errMsg = "Data transaksi tidak ditemukan !";
                        throw new Exception(errMsg);
                    }

                    dsOut.DataSetName = "Data";
                    dsOut.Tables[0].TableName = "Trx";

                    xmlOut = dsOut.GetXml();
                }
                else
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();
                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                errMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            errMsg = "Failed exec sp OMGetListProductSekunder";
                    }

                    throw new Exception(errMsg);
                }

                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }


        public static bool TrxSecCalculateTransaction(
            NispQuery.ClsQuery cQuery
            , string custBuySell
            , long cifId
            , int secId
            , string settlementDate
            , decimal faceValue
            , decimal dealPrice
            , decimal tax
            , decimal transactionFee
            , string xmlTrxLink
            , out string xmlOut
            , out string errMsg
            , out string errCode
            )
        {
            int calcType = (custBuySell.Equals("B") ? 4 : 3);

            xmlOut = "";
            errMsg = "";
            errCode = "";
            int accruedDays = 0;
            decimal proceed = 0;
            decimal interest = 0;
            decimal taxOnAccured = 0;
            decimal capitalGain = 0;
            decimal taxOnCapitalGainGL = 0;
            decimal skfAmount = 0;
            decimal totalProceed = 0;
            decimal calcTrxFee = 0;
            decimal ytm = 0;

            try
            {
                /*** create data structures ***/
                DataSet dsOut = new DataSet();
                DataSet dsTemp = new DataSet();

                DataTable dtMain = new DataTable();
                dtMain.Columns.Add("AccruedDays");
                dtMain.Columns.Add("Proceed");
                dtMain.Columns.Add("Interest");
                dtMain.Columns.Add("TaxOnAccrued");
                dtMain.Columns.Add("CapitalGain");
                dtMain.Columns.Add("TaxOnCapitalGL");
                dtMain.Columns.Add("SafeKeepingFeeAmount");
                dtMain.Columns.Add("TotalProceed");
                dtMain.Columns.Add("TransactionFee");
                dtMain.Columns.Add("YTM");
                dsOut.Tables.Add(dtMain);

                /*** execute stored procedure for calculation ***/
                OleDbParameter[] dbPar = new OleDbParameter[20];
                dbPar[0] = new OleDbParameter("@nCalcType", calcType);
                dbPar[1] = new OleDbParameter("@nCIFId", cifId);
                dbPar[2] = new OleDbParameter("@nSecId", secId);
                dbPar[3] = new OleDbParameter("@dSettlementDate", settlementDate);
                dbPar[4] = new OleDbParameter("@nFaceValue", faceValue);
                dbPar[5] = new OleDbParameter("@nDealPrice", dealPrice);
                dbPar[6] = new OleDbParameter("@nTax", tax);
                dbPar[7] = new OleDbParameter("@nTransactionFee", transactionFee);
                dbPar[8] = new OleDbParameter("@nAccruedDays", OleDbType.Integer);
                dbPar[8].Direction = ParameterDirection.Output;
                dbPar[9] = new OleDbParameter("@nProceed", OleDbType.Currency);
                dbPar[9].Direction = ParameterDirection.Output;
                dbPar[10] = new OleDbParameter("@nInterest", OleDbType.Currency);
                dbPar[10].Direction = ParameterDirection.Output;
                dbPar[11] = new OleDbParameter("@nTaxOnAccrued", OleDbType.Currency);
                dbPar[11].Direction = ParameterDirection.Output;
                dbPar[12] = new OleDbParameter("@nCapitalGain", OleDbType.Currency);
                dbPar[12].Direction = ParameterDirection.Output;
                dbPar[13] = new OleDbParameter("@nTaxOnCapitalGL", OleDbType.Currency);
                dbPar[13].Direction = ParameterDirection.Output;
                dbPar[14] = new OleDbParameter("@nSafeKeepingFeeAmount", OleDbType.Currency);
                dbPar[14].Direction = ParameterDirection.Output;
                dbPar[15] = new OleDbParameter("@nTotalProceed", OleDbType.Currency);
                dbPar[15].Direction = ParameterDirection.Output;
                dbPar[16] = new OleDbParameter("@nCalculatedTransactionFee", OleDbType.Currency);
                dbPar[16].Direction = ParameterDirection.Output;
                dbPar[17] = new OleDbParameter("@nYTM", OleDbType.Currency);
                dbPar[17].Direction = ParameterDirection.Output;
                dbPar[18] = new OleDbParameter("@pcXMLTransactionLink", xmlTrxLink);
                dbPar[19] = new OleDbParameter("@pbIsUpload", false);

                if (cQuery.ExecProc("dbo.TRSRetailCalculateFee", ref dbPar, out dsTemp))
                {
                    if (dsTemp.Tables.Count == 0)
                        throw new Exception("Data not found");

                    int.TryParse(dbPar[8].Value.ToString(), out accruedDays);
                    decimal.TryParse(dbPar[9].Value.ToString(), out proceed);
                    decimal.TryParse(dbPar[10].Value.ToString(), out interest);
                    decimal.TryParse(dbPar[11].Value.ToString(), out taxOnAccured);
                    decimal.TryParse(dbPar[12].Value.ToString(), out capitalGain);
                    decimal.TryParse(dbPar[13].Value.ToString(), out taxOnCapitalGainGL);
                    decimal.TryParse(dbPar[14].Value.ToString(), out skfAmount);
                    decimal.TryParse(dbPar[15].Value.ToString(), out totalProceed);
                    decimal.TryParse(dbPar[16].Value.ToString(), out calcTrxFee);
                    decimal.TryParse(dbPar[17].Value.ToString(), out ytm);

                    dtMain.Rows.Add(new object[] {
                        accruedDays,
                        proceed,
                        interest,
                        taxOnAccured,
                        capitalGain,
                        taxOnCapitalGainGL,
                        skfAmount,
                        totalProceed,
                        calcTrxFee,
                        ytm
                    }
                    );

                    dsOut.Tables.Add(dsTemp.Tables[0].Copy());

                    dsOut.DataSetName = "Data";
                    dsOut.Tables[0].TableName = "Main";
                    dsOut.Tables[1].TableName = "List";

                    xmlOut = dsOut.GetXml();
                }
                else
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();
                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                errMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            errMsg = "Failed exec sp TRSRetailCalculateFee";
                    }

                    throw new Exception(errMsg);
                }

                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }

        }

        public static bool TrxSecCalculateTransaction(
            NispQuery.ClsQuery cQuery
            , string xmlInput
            , out string xmlOut
            , out string errMsg
            , out string errCode
            )
        {
            xmlOut = "";
            errMsg = "";
            errCode = "";
            DataSet dsOut = new DataSet();

            xmlInput = clsXML.DecodeXMLAll(xmlInput);
            xmlInput = clsXML.NormalizeSpecialTag(xmlInput, "<TransactionLink>", "</TransactionLink>");

            try
            {
                /*** execute stored procedure for calculation ***/
                OleDbParameter[] dbPar = new OleDbParameter[3];
                dbPar[0] = new OleDbParameter("@pcXmlData", xmlInput);
                dbPar[1] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 1000);
                dbPar[1].Direction = ParameterDirection.Output;
                dbPar[2] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 5);
                dbPar[2].Direction = ParameterDirection.Output;

                if (cQuery.ExecProc("dbo.OMTrxSecCalculate", ref dbPar, out dsOut))
                {
                    if (dsOut.Tables.Count == 0)
                        throw new Exception("Data not found !");

                    errMsg = dbPar[1].Value.ToString();
                    errCode = dbPar[2].Value.ToString();

                    if (!errMsg.Equals("") || !errCode.Equals(""))
                        throw new Exception(errMsg);

                    dsOut.DataSetName = "Data";
                    dsOut.Tables[0].TableName = "List";
                    dsOut.Tables[1].TableName = "Main";

                    xmlOut = dsOut.GetXml();
                }
                else
                {
                    while (NispQuery.ClsQuery.queError.Count > 0)
                    {
                        string[] errList = NispQuery.ClsQuery.queError.Dequeue();
                        if (errList.Length >= 2)
                        {
                            for (int i = 1; i < errList.Length; i++)
                                errMsg += errList[i] + System.Environment.NewLine;
                        }
                        else
                            errMsg = "Failed exec sp OMGetListProductSekunder";
                    }

                    throw new Exception(errMsg);
                }

                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }

        }

        #endregion
        //20190401, uzia, DIGIT18207, end
        #endregion
        //20190115, rezakahfi, DIGIT18207, end
        //20190123, rezakahfi, DIGIT17257, begin
        #region Velocity

        public static bool ONFXDataTrxInquiry(NispQuery.ClsQuery cQuery, string DealNo, out string strXmlOut, out string intErrCode, out string strErrMsg)
        {
            strErrMsg = "";
            bool isOK = false;
            strXmlOut = "";
            intErrCode = "";
            strErrMsg = "";

            DataSet dsOut = new DataSet();

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[1];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcDealNo_ONFX", DealNo);
            isOK = cQuery.ExecProc("dbo.VELPopulateONFXTrxByDeal", ref dbPar, out dsOut);
            if (isOK)
            {
                dsOut.DataSetName = "Data";
                dsOut.Tables[0].TableName = "FX";

                intErrCode = dsOut.Tables[1].Rows[0]["ErrCode"].ToString();
                strErrMsg = dsOut.Tables[1].Rows[0]["ErrMsg"].ToString();

                dsOut.Tables.RemoveAt(1);

                strXmlOut = dsOut.GetXml();
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
                        strErrMsg = "Failed exec sp VELPopulateONFXTrxByDeal";
                }
            }

            return isOK;
        }

        #endregion
        //20190123, rezakahfi, DIGIT17257, end
        //20190424, rezakahfi, DIGIT18207, begin
        public static bool SecurityTranasctionSubmit(NispQuery.ClsQuery cQuery
            , string xmlInput
            , bool bCommitImmediately

            , out string SecurityTransaction_TT_XML
            , out string SecurityTransactionLink_TR_XML
            , out bool ClearUnpaidSafeKeepingFee

            , out string Respon
            , out string ErrCode
            , out string ErrMsg

            , out DataSet dsOut
            )
        {
            bool isOK = false;
            SecurityTransaction_TT_XML = "";
            SecurityTransactionLink_TR_XML = "";
            ClearUnpaidSafeKeepingFee = false;

            Respon = "";
            ErrCode = "";
            ErrMsg = "";

            dsOut = new DataSet();
            xmlInput = clsXML.DecodeXML(xmlInput);
            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[8];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcXmlInput", xmlInput);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pbCommitImmediately", bCommitImmediately);

            dbPar[2] = new System.Data.OleDb.OleDbParameter("@pcSecurityTransaction_TT_XML", System.Data.OleDb.OleDbType.VarChar, int.MaxValue);
            dbPar[2].Direction = ParameterDirection.Output;
            dbPar[3] = new System.Data.OleDb.OleDbParameter("@pcSecurityTransactionLink_TR_XML", System.Data.OleDb.OleDbType.VarChar, int.MaxValue);
            dbPar[3].Direction = ParameterDirection.Output;
            dbPar[4] = new System.Data.OleDb.OleDbParameter("@pbClearUnpaidSafeKeepingFee", System.Data.OleDb.OleDbType.Boolean);
            dbPar[4].Direction = ParameterDirection.Output;

            dbPar[5] = new System.Data.OleDb.OleDbParameter("@pcRespon", System.Data.OleDb.OleDbType.VarChar, 4000);
            dbPar[5].Direction = ParameterDirection.Output;
            dbPar[6] = new System.Data.OleDb.OleDbParameter("@pcErrCode", System.Data.OleDb.OleDbType.VarChar, 4000);
            dbPar[6].Direction = ParameterDirection.Output;
            dbPar[7] = new System.Data.OleDb.OleDbParameter("@pcErrMsg", System.Data.OleDb.OleDbType.VarChar, 4000);
            dbPar[7].Direction = ParameterDirection.Output;

            isOK = cQuery.ExecProc("dbo.OMSubmitTransactionSekunder", ref dbPar, out dsOut);

            if (isOK)
            {
                SecurityTransaction_TT_XML = dbPar[2].Value.ToString();
                SecurityTransactionLink_TR_XML = dbPar[3].Value.ToString();

                if (dbPar[4].Value.ToString().Trim() != "")
                    ClearUnpaidSafeKeepingFee = bool.Parse(dbPar[4].Value.ToString());

                Respon = dbPar[5].Value.ToString();
                ErrCode = dbPar[6].Value.ToString();
                ErrMsg = dbPar[7].Value.ToString();
            }
            else
            {
                while (NispQuery.ClsQuery.queError.Count > 0)
                {
                    string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                    if (errList.Length >= 2)
                    {
                        for (int i = 1; i < errList.Length; i++)
                            ErrMsg += errList[i] + System.Environment.NewLine;
                    }
                    else
                        ErrMsg = "Failed exec sp OMSubmitTransactionSekunder";
                }
            }

            return isOK;
        }

        public static bool UpdateAccruedInterest(NispQuery.ClsQuery cQuery, string strXml, string strDealNo, int nik, out string ErrMsg)
        {
            ErrMsg = "";
            bool isOK = false;

            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[3];
            odpParam[0] = new System.Data.OleDb.OleDbParameter("@pcXmlData", strXml);
            odpParam[1] = new System.Data.OleDb.OleDbParameter("@pcDealNo", strDealNo);
            odpParam[2] = new System.Data.OleDb.OleDbParameter("@pnNIK", nik);

            isOK = (cQuery.ExecProc("dbo.TRSRetailUpdateAccruedInterest", ref odpParam));

            if (!isOK)
            {
                while (NispQuery.ClsQuery.queError.Count > 0)
                {
                    string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                    if (errList.Length >= 2)
                    {
                        for (int i = 1; i < errList.Length; i++)
                            ErrMsg += errList[i] + System.Environment.NewLine;
                    }
                    else
                        ErrMsg = "Failed exec sp TRSRetailUpdateAccruedInterest";
                }
            }

            return isOK;
        }

        public static bool ProcessInsertSecurityTransactionCommit(NispQuery.ClsQuery obligasiQuery
            , object[] objParams
            , out DataSet dsResult
            , out string ErrMsg
            )
        {
            string strCommand = "TRSProcessInsertSecurityTransactionCommit";

            bool isOK = false;
            ErrMsg = "";

            int paramCount = 5;
            dsResult = new DataSet();

            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pcXMLDataTransactionLink", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pcAccountBlockACTYPE", OleDbType.Char, 1);
            dbParam[4] = new OleDbParameter("@pbClearUnpaidSafeKeepingFee", OleDbType.Boolean);

            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];

            isOK = obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult);

            if (!isOK)
            {
                while (NispQuery.ClsQuery.queError.Count > 0)
                {
                    string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                    if (errList.Length >= 2)
                    {
                        for (int i = 1; i < errList.Length; i++)
                            ErrMsg += errList[i] + System.Environment.NewLine;
                    }
                    else
                        ErrMsg = "Failed exec sp TRSProcessInsertSecurityTransactionCommit";
                }
            }

            return isOK;
        }

        public static System.Data.DataSet calculateORI2(NispQuery.ClsQuery cQuery, int Type, int CIFId, int SecId, int SettlementDate, decimal FaceValue, decimal DealPrice, decimal Tax, decimal? TransactionFee, string transactionLinkXML)
        {

            System.Data.DataSet dsResult = new System.Data.DataSet();
            System.Data.DataSet dsOutput = new System.Data.DataSet();
            bool bOK = false;

            //20171009, agireza, TRBST16240, begin
            //System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[18];
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[19];
            //20171009, agireza, TRBST16240, end
            try
            {
                /* Type */
                odpParam[0] = new System.Data.OleDb.OleDbParameter();
                odpParam[0].OleDbType = System.Data.OleDb.OleDbType.SmallInt;
                odpParam[0].Value = Type;

                odpParam[1] = new System.Data.OleDb.OleDbParameter();
                odpParam[1].OleDbType = System.Data.OleDb.OleDbType.BigInt;
                odpParam[1].Value = CIFId;

                odpParam[2] = new System.Data.OleDb.OleDbParameter();
                odpParam[2].OleDbType = System.Data.OleDb.OleDbType.Integer;
                odpParam[2].Value = SecId;

                odpParam[3] = new System.Data.OleDb.OleDbParameter();
                odpParam[3].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[3].Value = SettlementDate;

                /* FaceValue */
                odpParam[4] = new System.Data.OleDb.OleDbParameter();
                odpParam[4].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[4].Value = FaceValue;
                /* DealPrice */
                odpParam[5] = new System.Data.OleDb.OleDbParameter();
                odpParam[5].OleDbType = System.Data.OleDb.OleDbType.Decimal;
                odpParam[5].Value = DealPrice;
                /* Tax */
                odpParam[6] = new System.Data.OleDb.OleDbParameter();
                odpParam[6].OleDbType = System.Data.OleDb.OleDbType.Decimal;
                odpParam[6].Value = Tax;
                /*  Tranasction Fee */
                odpParam[7] = new System.Data.OleDb.OleDbParameter();
                odpParam[7].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[7].Value = TransactionFee;
                /*AccruedDays*/
                odpParam[8] = new System.Data.OleDb.OleDbParameter();
                odpParam[8].OleDbType = System.Data.OleDb.OleDbType.Integer;
                odpParam[8].Direction = System.Data.ParameterDirection.Output;
                /*Proceed*/
                odpParam[9] = new System.Data.OleDb.OleDbParameter();
                odpParam[9].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[9].Direction = System.Data.ParameterDirection.Output;
                /*Interest*/
                odpParam[10] = new System.Data.OleDb.OleDbParameter();
                odpParam[10].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[10].Direction = System.Data.ParameterDirection.Output;
                /*TaxOnAccrued*/
                odpParam[11] = new System.Data.OleDb.OleDbParameter();
                odpParam[11].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[11].Direction = System.Data.ParameterDirection.Output;
                /*CapitalGain*/
                odpParam[12] = new System.Data.OleDb.OleDbParameter();
                odpParam[12].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[12].Direction = System.Data.ParameterDirection.Output;
                /*TaxOnCapitalGL*/
                odpParam[13] = new System.Data.OleDb.OleDbParameter();
                odpParam[13].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[13].Direction = System.Data.ParameterDirection.Output;
                /*SafeKeepingFeeAmount*/
                odpParam[14] = new System.Data.OleDb.OleDbParameter();
                odpParam[14].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[14].Direction = System.Data.ParameterDirection.Output;
                /*TotalProceed*/
                odpParam[15] = new System.Data.OleDb.OleDbParameter();
                odpParam[15].OleDbType = System.Data.OleDb.OleDbType.Currency;
                odpParam[15].Direction = System.Data.ParameterDirection.Output;

                odpParam[16] = new System.Data.OleDb.OleDbParameter("@nCalculatedTransactionFee", System.Data.OleDb.OleDbType.Currency);
                odpParam[16].Direction = System.Data.ParameterDirection.Output;

                odpParam[17] = new System.Data.OleDb.OleDbParameter("@nYTM", System.Data.OleDb.OleDbType.Currency);
                odpParam[17].Direction = System.Data.ParameterDirection.Output;

                //20171009, agireza, TRBST16240, begin
                //odpParam[17] = new System.Data.OleDb.OleDbParameter("@pcXMLTransactionLink", System.Data.OleDb.OleDbType.VarChar);
                //odpParam[17].Value = transactionLinkXML;
                odpParam[18] = new System.Data.OleDb.OleDbParameter("@pcXMLTransactionLink", System.Data.OleDb.OleDbType.VarChar);
                odpParam[18].Value = transactionLinkXML;
                //20171009, agireza, TRBST16240, end

                bOK = cQuery.ExecProc("dbo.TRSRetailCalculateFee", ref odpParam, out dsOutput);

                System.Data.DataSet ds = new System.Data.DataSet("Root");
                System.Data.DataTable dt = new System.Data.DataTable("RS");
                ds.Tables.Add(dt);
                if (bOK)
                    ds.Tables.Add(dsOutput.Tables[0].Copy());

                dt.Columns.Add("AccruedDays");
                dt.Columns.Add("Proceed");
                dt.Columns.Add("Interest");
                dt.Columns.Add("TaxOnAccrued");
                dt.Columns.Add("CapitalGain");
                dt.Columns.Add("TaxOnCapitalGL");
                dt.Columns.Add("SafeKeepingFeeAmount");
                dt.Columns.Add("TotalProceed");
                dt.Columns.Add("TransactionFee");
                //20171009, agireza, TRBST16240, begin
                dt.Columns.Add("YTM");
                //20171009, agireza, TRBST16240, end

                if (bOK)
                {
                    System.Data.DataRow DetailRow = dt.NewRow();
                    DetailRow["AccruedDays"] = odpParam[8].Value.ToString();
                    DetailRow["Proceed"] = odpParam[9].Value.ToString();
                    DetailRow["Interest"] = odpParam[10].Value.ToString();
                    DetailRow["TaxOnAccrued"] = odpParam[11].Value.ToString();
                    DetailRow["CapitalGain"] = odpParam[12].Value.ToString();
                    DetailRow["TaxOnCapitalGL"] = odpParam[13].Value.ToString();
                    DetailRow["SafeKeepingFeeAmount"] = odpParam[14].Value.ToString();
                    DetailRow["TotalProceed"] = odpParam[15].Value.ToString();
                    DetailRow["TransactionFee"] = odpParam[16].Value.ToString();
                    DetailRow["YTM"] = odpParam[17].Value.ToString();
                    dt.Rows.Add(DetailRow);
                }
                dsResult = ds;

                return dsResult;
            }
            catch (NullReferenceException ex)
            {
                return dsResult;
            }
        }

        public static bool InputTransactionSekunderLog(NispQuery.ClsQuery cQuery
            , Guid uGuid
            , string Description
            , string ActionProcess
            , string xmlInput1
            , string xmlInput2
            , string xmlInput3
            , string xmlInputProcess

            )
        {
            //20210113, rezakahfi, BONDRETAIL-544, begin
            //bool isOK = false;
            bool isOK = true;
            try
            {
                //20210113, rezakahfi, BONDRETAIL-544, end
                System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[7];
                odpParam[0] = new System.Data.OleDb.OleDbParameter("@puGuidProcess", uGuid);
                odpParam[1] = new System.Data.OleDb.OleDbParameter("@pcDescription", Description);
                odpParam[2] = new System.Data.OleDb.OleDbParameter("@pcInputAction", ActionProcess);
                odpParam[3] = new System.Data.OleDb.OleDbParameter("@pcInputXML_1", xmlInput1);
                odpParam[4] = new System.Data.OleDb.OleDbParameter("@pcInputXML_2", xmlInput2);
                odpParam[5] = new System.Data.OleDb.OleDbParameter("@pcInputXML_3", xmlInput3);
                odpParam[6] = new System.Data.OleDb.OleDbParameter("@pcInputProcess", xmlInputProcess);

                isOK = (cQuery.ExecProc("dbo.OMInputLogProcess", ref odpParam));
                //20210113, rezakahfi, BONDRETAIL-544, begin
            }
            catch (NullReferenceException ex)
            {

            }
            //20210113, rezakahfi, BONDRETAIL-544, end
            return isOK;
        }

        public static bool DeleteTransactionSekunder(NispQuery.ClsQuery cQuery
            , string DealId
            )
        {
            bool isOK = false;

            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[1];
            odpParam[0] = new System.Data.OleDb.OleDbParameter("@pnDealId", int.Parse(DealId));

            isOK = (cQuery.ExecProc("dbo.OMDeleteProcessInputSecurityMaster", ref odpParam));

            return isOK;
        }

        public static bool UpdateStatusSecurityTransactionMurex(NispQuery.ClsQuery cQuery, string stbData
            , int nNik
            , Guid GuidInternal
            , Guid GuidExternal
            , out string ErrMsg
            )
        {
            bool isOK = false;

            ErrMsg = "";

            DataSet dsOut = new DataSet();

            string strCommand = "trs_UpdateStatusSecurityTransactionAfterMurex";
            System.Data.OleDb.OleDbParameter[] dbParameter = new OleDbParameter[5];
            dbParameter[0] = new OleDbParameter("@xmlInput", stbData);
            dbParameter[1] = new OleDbParameter("@inpStatus", OleDbType.Integer);
            dbParameter[1].Value = 19;
            dbParameter[2] = new OleDbParameter("@nNik", nNik);
            dbParameter[3] = new OleDbParameter("@pcGuidInternal", OleDbType.Guid);
            dbParameter[3].Value = GuidInternal;
            dbParameter[4] = new OleDbParameter("@pcGuidExternal", OleDbType.Guid);
            dbParameter[4].Value = GuidExternal;
            isOK = cQuery.ExecProc(strCommand, ref dbParameter, out dsOut);

            if (!isOK)
            {
                while (NispQuery.ClsQuery.queError.Count > 0)
                {
                    string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                    if (errList.Length >= 2)
                    {
                        for (int i = 1; i < errList.Length; i++)
                            ErrMsg += errList[i] + System.Environment.NewLine;
                    }
                    else
                        ErrMsg = "Failed exec sp trs_UpdateStatusSecurityTransactionAfterMurex";
                }
            }

            return isOK;
        }

        public static bool PreBookSubmit(NispQuery.ClsQuery cQuery, string xmlInput
            , out string Respon
            , out string ErrCode
            , out string ErrMsg
            )
        {
            bool isOK = false;
            Respon = "";
            ErrCode = "";
            ErrMsg = "";

            DataSet dsOut = new DataSet();

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[4];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcXmlInput", xmlInput);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pcRespon", System.Data.OleDb.OleDbType.VarChar, 4000);
            dbPar[1].Direction = ParameterDirection.Output;
            dbPar[2] = new System.Data.OleDb.OleDbParameter("@pcErrCode", System.Data.OleDb.OleDbType.VarChar, 4000);
            dbPar[2].Direction = ParameterDirection.Output;
            dbPar[3] = new System.Data.OleDb.OleDbParameter("@pcErrMsg", System.Data.OleDb.OleDbType.VarChar, 4000);
            dbPar[3].Direction = ParameterDirection.Output;

            isOK = cQuery.ExecProc("dbo.OMTrxSecPreBook", ref dbPar, out dsOut);
            if (isOK)
            {
                Respon = dbPar[1].Value.ToString();
                ErrCode = dbPar[2].Value.ToString();
                ErrMsg = dbPar[3].Value.ToString();
            }
            else
            {
                while (NispQuery.ClsQuery.queError.Count > 0)
                {
                    string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                    if (errList.Length >= 2)
                    {
                        for (int i = 1; i < errList.Length; i++)
                            ErrMsg += errList[i] + System.Environment.NewLine;
                    }
                    else
                        ErrMsg = "Failed exec sp OMTrxSecPreBook";
                }
            }

            return isOK;
        }
        //20190424, rezakahfi, DIGIT18207, end
        //20200417, rezakahfi, BONDRETAIL-261, begin
        public static bool SecurityCheckAccount(NispQuery.ClsQuery cQuery
            , string SecId
            , string NoRekInvestor
            , string Ccy
            , out DataSet dsResult
            , out string ErrMsg
            )
        {
            bool isOK = false;

            ErrMsg = "";

            dsResult = new DataSet();

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[4];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@nSecId", SecId);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@nNoRekInvestor", NoRekInvestor);
            dbPar[2] = new System.Data.OleDb.OleDbParameter("@cCcy", Ccy);
            dbPar[3] = new System.Data.OleDb.OleDbParameter("@pcErrorMsg", System.Data.OleDb.OleDbType.VarChar, 4000);
            dbPar[3].Direction = ParameterDirection.Output;

            isOK = cQuery.ExecProc("dbo.SecurityCheckAccount", ref dbPar, out dsResult);
            if (isOK)
            {
                ErrMsg = dbPar[3].Value.ToString();
            }
            else
            {
                while (NispQuery.ClsQuery.queError.Count > 0)
                {
                    string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                    if (errList.Length >= 2)
                    {
                        for (int i = 1; i < errList.Length; i++)
                            ErrMsg += errList[i] + System.Environment.NewLine;
                    }
                    else
                        ErrMsg = "Failed exec sp SecurityCheckAccount";
                }
            }

            return isOK;
        }
        //20200417, rezakahfi, BONDRETAIL-261, end
        //20210113, rezakahfi, BONDRETAIL-544, begin
        public static bool subUpdateSequenceBlock(NispQuery.ClsQuery obligasiQuery, long DealId, int BlockSequence, string ACType, out string ErrMsg)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            ErrMsg = "";

            dbPar[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            dbPar[0] = new OleDbParameter("@pnDealId", DealId);

            dbPar[1] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbPar[1] = new OleDbParameter("@pnAccountBlockSequence", BlockSequence);

            dbPar[2] = new OleDbParameter("@pcAccountACType", OleDbType.VarChar);
            dbPar[2] = new OleDbParameter("@pcAccountACType", ACType);

            string strCommand = "TRSUpdateBlokirSwitchingTransaction";

            bool isOK = obligasiQuery.ExecProc(strCommand, ref dbPar);

            if (!isOK)
            {
                while (NispQuery.ClsQuery.queError.Count > 0)
                {
                    string[] errList = NispQuery.ClsQuery.queError.Dequeue();

                    if (errList.Length >= 2)
                    {
                        for (int i = 1; i < errList.Length; i++)
                            ErrMsg += errList[i] + System.Environment.NewLine;
                    }
                    else
                        ErrMsg = "Failed exec sp subTRSUpdateBlokirSwitchingTransaction";
                }
            }

            return isOK;
        }
        //20210113, rezakahfi, BONDRETAIL-544, end
        //20210730, victor, MRX-189, begin
        public static bool LogFLDCPY(NispQuery.ClsQuery cQuery, string GUID, string Data, out string ErrMsg)
        {
            bool isOK = false;

            ErrMsg = "";

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[2];

            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 100);
            dbPar[0].Value = GUID;
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pcData", System.Data.OleDb.OleDbType.VarChar, 1000);
            dbPar[1].Value = Data;

            isOK = cQuery.ExecProc("dbo.MXINTFCPYInLog", ref dbPar);

            return isOK;
        }

        public static bool LogFLDDEAL(NispQuery.ClsQuery cQuery, string GUID, string Product, string SourceReff, string Data, out string ErrMsg)
        {
            bool isOK = false;

            ErrMsg = "";

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[4];

            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 100);
            dbPar[0].Value = GUID;
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pcData", System.Data.OleDb.OleDbType.VarChar, 1000);
            dbPar[1].Value = Data;
            dbPar[2] = new System.Data.OleDb.OleDbParameter("@pcProduct", System.Data.OleDb.OleDbType.VarChar, 1000);
            dbPar[2].Value = Product;
            dbPar[3] = new System.Data.OleDb.OleDbParameter("@pcSourceReff", System.Data.OleDb.OleDbType.VarChar, 1000);
            dbPar[3].Value = SourceReff;

            isOK = cQuery.ExecProc("dbo.MXINTFDEALInLog", ref dbPar);

            return isOK;
        }

        public static bool isUsingMQ(NispQuery.ClsQuery cQuery,string Type)
        {
            DataSet dsOut = new DataSet();

            bool isOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcParamType", Type);

            isOK = cQuery.ExecProc("dbo.TRSLelangGetParam", ref dbPar, out dsOut);

            if (isOK)
            {
                if (dsOut.Tables.Count > 0)
                {
                    if (dsOut.Tables[0].Rows.Count >0)
                    {
                        if (dsOut.Tables[0].Columns.Contains("ParamValue"))
                            isOK = bool.Parse(dsOut.Tables[0].Rows[0]["ParamValue"].ToString());
                    }
                }
            }

            return isOK;
        }
        //20210730, victor, MRX-189, end
        //20220815, rezakahfi, BONDRETAIL-1022, begin
        //20230626, rezakahfi, FMCT-25, begin
        //public static bool AddInfoInterface(NispQuery.ClsQuery cQuery, string xmlFLD, out string newXmlFLD)
        public static bool AddInfoInterface(NispQuery.ClsQuery cQuery
            , string xmlFLD
            , string Product
            , out string newXmlFLD
            )
        //20230626, rezakahfi, FMCT-25, end
        {
            DataSet dsOut = new DataSet();

            bool isOK = false;
            newXmlFLD = "";

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlFLD);
            //20230626, rezakahfi, FMCT-25, begin
            dbPar[1] = new OleDbParameter("@pcProduct", Product);
            //20230626, rezakahfi, FMCT-25, end

            if (cQuery.ExecProc("dbo.TRSAddInfoXMLInterfaceToMX", ref dbPar, out dsOut))
            {
                if (dsOut.Tables.Count > 0)
                {
                    if (dsOut.Tables[0].Rows.Count > 0)
                    {
                        if (dsOut.Tables[0].Columns.Contains("ResultXML"))
                        {
                            newXmlFLD = dsOut.Tables[0].Rows[0]["ResultXML"].ToString();
                            isOK = true;
                        }
                    }
                }
            }

            return isOK;
        }
        //20220815, rezakahfi, BONDRETAIL-1022, end
		//20230814, tobias, BONDRETAIL-1414, begin
        public static bool ORI_Online_DeleteTransaction(
            NispQuery.ClsQuery cQuery
            , out string strErrMsg
            , out string strErrCode
            , string strGuid
            )
        {
            bool isOK = false;
            strErrMsg = "";
            strErrCode = "";
            DataSet dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcGuid", strGuid);
            dbPar[1] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 1000);
            dbPar[1].Direction = ParameterDirection.Output;
            dbPar[2] = new OleDbParameter("@pcErrCode", OleDbType.VarChar, 5);
            dbPar[2].Direction = ParameterDirection.Output;
            isOK = cQuery.ExecProc("dbo.ORISBNIDeleteTransactionData", ref dbPar, out dsOut);
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
                        strErrMsg = "Failed exec sp ORISBNIDeleteTransactionData";
                }
                strErrCode = "01000";
            }
            else
            {
                if (dbPar[1].Value != null && dbPar[2].Value != null)
                {
                    if (!dbPar[1].Value.Equals(""))
                    {
                        strErrMsg = dbPar[1].Value.ToString();
                        strErrCode = dbPar[2].Value.ToString();
                        return false;
                    }
                }
            }
            return isOK;
        }
        //20230814, tobias, BONDRETAIL-1414, end
        //20230907, darul.wahid, BONDRETAIL-1394, begin
        public static bool GetAPIUrlParam(
            NispQuery.ClsQuery cQuery
            , string strAPICode
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
                OleDbParameter[] dbPar = new OleDbParameter[2];
                dbPar[0] = new OleDbParameter("@pcNotifType", strAPICode);
                dbPar[1] = new OleDbParameter("@pcParamValue", "Other");
                isOK = cQuery.ExecProc("dbo.TRSObliGetNotifAPIParam", ref dbPar, out dsOut);
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
        //20230907, darul.wahid, BONDRETAIL-1394, end
    }
}