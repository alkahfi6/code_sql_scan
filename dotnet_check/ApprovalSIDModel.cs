using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.Data;

namespace BankNISP.Obligasi01
{
    class ApprovalSIDModel
    {
        private ObligasiQuery _cQuery;
        private int _nUserNik;

        public ApprovalSIDModel(ObligasiQuery cQuery, int UserNik) 
        {
            _cQuery = cQuery;
            _nUserNik = UserNik;
        }

        public bool PopulateApprovalSID(out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pnUserNik", this._nUserNik);

                blnResult = this._cQuery.ExecProc("TRSPopulateApprovalSID", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20220608, darul.wahid, RMM-1374, begin
        //public bool ApproveSID(string SelectedId, string statusApproval)
        public bool ApproveSID(string SelectedId, string statusApproval, out DataSet dsOut)
        //20220608, darul.wahid, RMM-1374, end
        {
            bool blnResult = false;
            //20220608, darul.wahid, RMM-1374, begin
            dsOut = new DataSet();
            //20220608, darul.wahid, RMM-1374, end

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[3];
                oParam[0] = new OleDbParameter("@pcSelectedId", SelectedId);
                oParam[1] = new OleDbParameter("@pnUserNik", this._nUserNik);
                oParam[2] = new OleDbParameter("@pcStatusApproval", statusApproval);

                //20220608, darul.wahid, RMM-1374, begin
                //blnResult = this._cQuery.ExecProc("TRSApproveSID", ref oParam);
                blnResult = this._cQuery.ExecProc("TRSApproveSID", ref oParam, out dsOut);
                //20220608, darul.wahid, RMM-1374, end
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20220608, darul.wahid, RMM-1374, begin
        public void UpdateStatusNotifNTI(int IdNotif, string StatusNotif, int NoOfTry, string NotifDesc, bool isBulk, string strXML)
        {
            bool blnResult = false;
            try
            {
                OleDbParameter[] oParam = new OleDbParameter[6];
                oParam[0] = new OleDbParameter("@nIdLog", IdNotif);
                oParam[1] = new OleDbParameter("@cStatus", StatusNotif);
                oParam[2] = new OleDbParameter("@nNumOfTry", NoOfTry);
                oParam[3] = new OleDbParameter("@cDescription", NotifDesc);
                oParam[4] = new OleDbParameter("@bIsBulk", isBulk);
                oParam[5] = new OleDbParameter("@cXMLInput", strXML);

                blnResult = this._cQuery.ExecProc("TRSNTIUpdateStatusNotif", ref oParam);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //20220608, darul.wahid, RMM-1374, end
        //20220929, darul.wahid, BONDRETAIL-1070, begin
        public void InsertLogOneNotif(string url, string strJsonData)
        {
            bool blnResult = false;
            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcUrl", url);
                oParam[1] = new OleDbParameter("@pcJsonBody", strJsonData);

                blnResult = this._cQuery.ExecProc("TRSInsertLogOneNotifNTI", ref oParam);
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message);
            }
        }
        //20220929, darul.wahid, BONDRETAIL-1070, end
    }
}
