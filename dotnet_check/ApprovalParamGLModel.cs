using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace BankNISP.Obligasi01
{
    public class ApprovalParamGLModel
    {
        private ObligasiQuery _cQuery;
        private int _userNIK;
        private string _userBranch;

        public ApprovalParamGLModel(ObligasiQuery cQuery, int userNIK, string userBranch)
        {
            this._cQuery = cQuery;
            this._userNIK = userNIK;
            this._userBranch = userBranch;
        }

        public bool PopulateApprovalParamGL( out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                blnResult = this._cQuery.ExecProc("TRSPopulateApprovalParameterGL", out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveApprovalParamGL(string xmlData, int prosesNIK, string statusApproval)
        {
            bool blnResult = false;

            try
            {

                OleDbParameter[] oParam = new OleDbParameter[3];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnProcessNIK", prosesNIK);
                oParam[2] = new OleDbParameter("@pcStatusApproval", statusApproval);

                blnResult = this._cQuery.ExecProc("TRSApprovalParamGL", ref oParam);



            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
    }
}
