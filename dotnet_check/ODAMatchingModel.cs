using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace BankNISP.Obligasi01.Model
{
    class ODAMatchingModel
    {
        private ObligasiQuery _cQuery;
        private int intNik;
        private string userBranch;
        //20230801, yudha.n, BONDRETAIL-1394, begin
        private wsOmniObli.clsService clsOmniService;
        //20230801, yudha.n, BONDRETAIL-1394, end

        public ODAMatchingModel(ObligasiQuery cQuery, int nik, string branch)
        {
            this._cQuery = cQuery;
            this.intNik = nik;
            this.userBranch = branch;
            //20230801, yudha.n, BONDRETAIL-1394, begin
            clsOmniService = new BankNISP.Obligasi01.wsOmniObli.clsService();
            //20230801, yudha.n, BONDRETAIL-1394, end
        }

        public bool PopulateParam(string strParamType, string Status, string strFilter, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[5];

            dbParams[0] = new OleDbParameter("@pcParamType", strParamType);
            dbParams[1] = new OleDbParameter("@pnStatus", Status);
            dbParams[2] = new OleDbParameter("@pcFilter", strFilter);
            dbParams[3] = new OleDbParameter("@pnNIK", this.intNik);
            dbParams[4] = new OleDbParameter("@pcBranch", this.userBranch);

            try
            {
                return this._cQuery.ExecProc("dbo.ODAPopulateParam", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }

        public bool PopulateMatching(string strOrderEffective, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[3];

            dbParams[0] = new OleDbParameter("@pcEffective", strOrderEffective);
            dbParams[1] = new OleDbParameter("@pnNIK", this.intNik);
            dbParams[2] = new OleDbParameter("@pcBranch", this.userBranch);

            try
            {
                return this._cQuery.ExecProc("dbo.ODAPopulateDataMatching", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }

        public bool PopulateResult(string strOrderEffective, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[3];

            dbParams[0] = new OleDbParameter("@pcEffective", strOrderEffective);
            dbParams[1] = new OleDbParameter("@pnNIK", this.intNik);
            dbParams[2] = new OleDbParameter("@pcBranch", this.userBranch);

            try
            {
                return this._cQuery.ExecProc("dbo.ODAPopulateResult", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }

        public bool SubmitResult(int DealNo,bool Execute)
        {
            OleDbParameter[] dbParams = new OleDbParameter[4];

            dbParams[0] = new OleDbParameter("@pnDealNo", DealNo);
            dbParams[1] = new OleDbParameter("@pbExecute", Execute);
            dbParams[2] = new OleDbParameter("@pnNIK", this.intNik);
            dbParams[3] = new OleDbParameter("@pcBranch", this.userBranch);

            try
            {
                return this._cQuery.ExecProc("dbo.ODADataProcesResult", ref dbParams);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }

        public bool GetXMLToONFX(string DealNo, out DataSet dsOut)
        {
            //20230801, yudha.n, BONDRETAIL-1394, begin
            string xmlDataTPair = "";
            GetTPair(out xmlDataTPair);
            //20230801, yudha.n, BONDRETAIL-1394, end

            dsOut = new DataSet();

            //20230801, yudha.n, BONDRETAIL-1394, begin
            //OleDbParameter[] dbParams = new OleDbParameter[3];
            OleDbParameter[] dbParams = new OleDbParameter[4];
            //20230801, yudha.n, BONDRETAIL-1394, end

            dbParams[0] = new OleDbParameter("@pnDealNo", DealNo);
            dbParams[1] = new OleDbParameter("@pnNIK", this.intNik);
            dbParams[2] = new OleDbParameter("@pcBranch", this.userBranch);
            //20230801, yudha.n, BONDRETAIL-1394, begin
            dbParams[3] = new OleDbParameter("@pcXMLTPair", xmlDataTPair);
            //20230801, yudha.n, BONDRETAIL-1394, end

            try
            {
                return this._cQuery.ExecProc("dbo.ODAGetXMLToONFX", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }

        //20230801, yudha.n, BONDRETAIL-1394, begin
        public bool GetTPair(out string xmlDataTPair)
        {
            bool isSuccess = false;
            DataSet dsOut = new DataSet();
            xmlDataTPair = "";
            string query = "SELECT FId, FExtLink, FExpired FROM dbo.TPair";

            try
            {
                string paramOut = "", message = "";
                isSuccess = clsOmniService.APIExecQuery(clsGlobal.strEncConnStringSMARTFX, query, "", out paramOut, out dsOut, out message);

                if (dsOut.Tables.Count > 0)
                {
                    dsOut.DataSetName = "DocumentElement";
                    DataTable dtToXMLCalculate = dsOut.Tables[0];
                    dtToXMLCalculate.TableName = "TPair";
                    StringBuilder stringBuilderInner = new StringBuilder();
                    dtToXMLCalculate.WriteXml(System.Xml.XmlWriter.Create(stringBuilderInner));
                    xmlDataTPair = stringBuilderInner.ToString();
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
            }
            return isSuccess;
        }
        //20230801, yudha.n, BONDRETAIL-1394, end

        public bool PopulateUserDummy(out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                blnResult = this._cQuery.ExecProc("dbo.TRSPopulateNIKFLD", out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool ValidateData(string strTypeValidasi, string Param1, string Param2, out string strResult, out bool bStop)
        {

            strResult = "";
            bStop = false;

            OleDbParameter[] dbParams = new OleDbParameter[7];
            dbParams[0] = new OleDbParameter("@pcTypeValidasi", strTypeValidasi);
            dbParams[1] = new OleDbParameter("@pcParam1", Param1);
            dbParams[2] = new OleDbParameter("@pcParam2", Param2);
            dbParams[3] = new OleDbParameter("@pcParam3", "");
            dbParams[4] = new OleDbParameter("@pcParam4", "");
            dbParams[5] = new OleDbParameter("@pcResult", OleDbType.VarChar, 100);
            dbParams[5].Direction = ParameterDirection.Output;
            dbParams[6] = new OleDbParameter("@pcStop", OleDbType.Boolean);
            dbParams[6].Direction = ParameterDirection.Output;


            if (this._cQuery.ExecProc("dbo.ODA_ValidateField", ref dbParams))
            {

                strResult = dbParams[5].Value.ToString();
                bStop = bool.Parse(dbParams[6].Value.ToString());

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ValidateData(string strTypeValidasi, string Param1, string Param2, string Param3, string Param4, out string strResult, out bool bStop)
        {
            strResult = "";
            bStop = false;
            bool bResult = false;
            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[7];
                dbParams[0] = new OleDbParameter("@pcTypeValidasi", strTypeValidasi);
                dbParams[1] = new OleDbParameter("@pcParam1", Param1);
                dbParams[2] = new OleDbParameter("@pcParam2", Param2);
                dbParams[3] = new OleDbParameter("@pcParam3", Param3);
                dbParams[4] = new OleDbParameter("@pcParam4", Param4);
                dbParams[5] = new OleDbParameter("@pcResult", OleDbType.VarChar, 255);
                dbParams[5].Direction = ParameterDirection.Output;
                dbParams[6] = new OleDbParameter("@pcStop", OleDbType.Boolean);
                dbParams[6].Direction = ParameterDirection.Output;


                if (this._cQuery.ExecProc("dbo.ODA_ValidateField", ref dbParams))
                {

                    strResult = dbParams[5].Value.ToString();
                    bStop = bool.Parse(dbParams[6].Value.ToString());

                    bResult = true;
                }
                else
                {
                    bResult = false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return bResult;
        }

        public bool UpdateStatusBlokir(string Sequence, string account, string status, int BlockSeq, string BlockType)
        {

            OleDbParameter[] dbParams = new OleDbParameter[7];
            try
            {
                dbParams[0] = new OleDbParameter("@pcSequence", Sequence);
                dbParams[1] = new OleDbParameter("@pcAccount", account);
                dbParams[2] = new OleDbParameter("@pcStatus", status);
                dbParams[3] = new OleDbParameter("@pnBlockSeq", BlockSeq);
                dbParams[4] = new OleDbParameter("@pcBlockTyp", BlockType);
                dbParams[5] = new OleDbParameter("@pnNIK", this.intNik);
                dbParams[6] = new OleDbParameter("@pcBranch", this.userBranch);

                return this._cQuery.ExecProc("dbo.ODAUpdateStatusBlokir", ref dbParams);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message.ToString());
                return false;
            }
        }
        //20170808, rezakahfi, LOGEN00444, begin
        public bool PopulateResultMurex(string strOrderEffective, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[3];

            dbParams[0] = new OleDbParameter("@pcEffective", strOrderEffective);
            dbParams[1] = new OleDbParameter("@pnNIK", this.intNik);
            dbParams[2] = new OleDbParameter("@pcBranch", this.userBranch);

            try
            {
                return this._cQuery.ExecProc("dbo.ODAPopulateResultToMX", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }

        public bool ProcessDataToMurex(string strXML,string strOrderEffective)
        {
            OleDbParameter[] dbParams = new OleDbParameter[3];

            dbParams[0] = new OleDbParameter("@pcEffective", strOrderEffective);
            dbParams[1] = new OleDbParameter("@pcXmlData", strXML);
            dbParams[2] = new OleDbParameter("@pnNIK", this.intNik);

            try
            {
                return this._cQuery.ExecProc("dbo.ODAProcessTrxToMurex", ref dbParams);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }
        //20170808, rezakahfi, LOGEN00444, end
        //20171110, rezakahfi, LOGEN00521, begin
        public bool PopulateCheckList(string strExecutedDate, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[3];

            dbParams[0] = new OleDbParameter("@pcExecutedDate", strExecutedDate);
            dbParams[1] = new OleDbParameter("@pnNIK", this.intNik);
            dbParams[2] = new OleDbParameter("@pcBranch", this.userBranch);

            try
            {
                return this._cQuery.ExecProc("dbo.ODAPopulateCheckListTrx", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }

        public bool SubmitDataChecklistTrx(string strExecutedDate, string strXML)
        {
            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[4];

                dbParams[0] = new OleDbParameter("@pcExecutedDate", strExecutedDate);
                dbParams[1] = new OleDbParameter("@pcXmlData", strXML);
                dbParams[2] = new OleDbParameter("@pnNIK", this.intNik);
                dbParams[3] = new OleDbParameter("@pcBranch", this.userBranch);

            
                return this._cQuery.ExecProc("dbo.ODASubmitChecklistTrxNonRekening", ref dbParams);
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
                return false;
            }
        }
        //20171110, rezakahfi, LOGEN00521, end
    }
}
