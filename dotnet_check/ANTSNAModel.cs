using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Excel = Microsoft.Office.Interop.Excel;

namespace BankNISP.Obligasi01
{
    public enum EnumAntsnaActionStatus
    {
        Open = 0,
        WaitingApproval = 1,
        Approved = 2,
        Printed = 3,
        Rejected = 4,
        Revised = 5
    };

    public class ANTSNAModel
    {
        private ObligasiQuery _cQuery = null;
        private int _userNIK;
        private string _userBranch;

        public ANTSNAModel(ObligasiQuery cQuery, int userNIK, string userBranch)
        {
            this._cQuery = cQuery;
            this._userNIK = userNIK;
            this._userBranch = userBranch;
        }

        #region ANTSNA Compose

        public bool PopulateComboParam(out DataSet dsOut)
        {
            dsOut = new DataSet();
            return this._cQuery.ExecProc("dbo.ANTSNAGetMasterParam", out dsOut);
        }

        public bool PopulateColumnParam(string fileType, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);

            return this._cQuery.ExecProc("dbo.ANTSNAGetColumnParam", ref dbPar, out dsOut);
        }

        //20200305, dennis, TR12020-2-LHBU-58, begin
        //public bool PopulateANTSNAData(string fileType, string status, DateTime? date, out DataSet dsOut)
        public bool PopulateANTSNAData(string fileType, string status, DateTime? date, int topRow, string product, out DataSet dsOut)
        //20200305, dennis, TR12020-2-LHBU-58, end
        {
            dsOut = new DataSet();

            //20200305, dennis, TR12020-2-LHBU-58, begin
            //OleDbParameter[] dbPar = new OleDbParameter[3];
            OleDbParameter[] dbPar = new OleDbParameter[5];
            //20200305, dennis, TR12020-2-LHBU-58, end
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);
            dbPar[1] = new OleDbParameter("@pnStatus", status);
            dbPar[2] = new OleDbParameter("@pdDate", date);
            //20200305, dennis, TR12020-2-LHBU-58, begin
            dbPar[3] = new OleDbParameter("@pnTopRow", topRow);
            dbPar[4] = new OleDbParameter("@pcProduct", product);
            //20200305, dennis, TR12020-2-LHBU-58, end
            if (date == null)  
                dbPar[2].Value = DBNull.Value;  
            else
                dbPar[2].Value = date;

            return this._cQuery.ExecProc("dbo.ANTSNAPopulateData", ref dbPar, out dsOut);
        }

        public bool PopulateParamReference(string strDimensi, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcDimensi", strDimensi);

            return this._cQuery.ExecProc("dbo.ANTSNAPopulateParamReference", ref dbPar, out dsOut);
        }

        public bool ValidateANTSNA(string fileType, string xmlData, out DataSet dsResult)
        {
            dsResult = null;

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);
            dbPar[1] = new OleDbParameter("@pcXmlData", xmlData);
            dbPar[2] = new OleDbParameter("@pnNIK", this._userNIK);
            dbPar[3] = new OleDbParameter("@pcBranch", this._userBranch);

            bool resQuery = this._cQuery.ExecProc("dbo.ANTSNAValidateField", ref dbPar, out dsResult);

            if (!resQuery || dsResult == null)
                return false;

            return true;
        }

        public bool SubmitANTSNA(string fileType, string xmlData, string action)
        {
            if (string.IsNullOrEmpty(action))
                action = "I";

            OleDbParameter[] dbPar = new OleDbParameter[5];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);
            dbPar[1] = new OleDbParameter("@pcXmlData", xmlData);
            dbPar[2] = new OleDbParameter("@pnNIK", this._userNIK);
            dbPar[3] = new OleDbParameter("@pcBranch", this._userBranch);
            dbPar[4] = new OleDbParameter("@pcAction", action);   

            return this._cQuery.ExecProc("dbo.ANTSNASubmitForm", ref dbPar);
        }

        public string GetSpecificGeneralParam(string fileType, string paramType)
        {
            string cRes = "";
            DataSet ds = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);
            dbPar[1] = new OleDbParameter("@pcParamType", paramType);

            if (this._cQuery.ExecProc("dbo.ANTSNAGetGeneralParam", ref dbPar, out ds))
                cRes = ds.Tables[0].Rows[0]["ValueParam"].ToString();

            return cRes;
        }

        //20200305, dion, TR12020-2-LHBU-58, begin
        public bool PopulateListProduct(string fileType, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);

            return this._cQuery.ExecProc("dbo.ANTSNAPopulateProduct", ref dbPar, out dsOut);
        }
        //20200305, dion, TR12020-2-LHBU-58, end

        #endregion ANTSNA Compose

        #region ANTSNA Approval and Generate

        public bool UpdateStatusDataANTSNA(string fileType, string idDataList, string newStatus)
        {
            OleDbParameter[] dbPar = new OleDbParameter[5];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);
            dbPar[1] = new OleDbParameter("@pcIdData", idDataList);
            dbPar[2] = new OleDbParameter("@pcNewStatus", newStatus);
            dbPar[3] = new OleDbParameter("@pnNIK", this._userNIK);
            dbPar[4] = new OleDbParameter("@pcBranch", this._userBranch);

            return this._cQuery.ExecProc("dbo.ANTSNAUpdateStatusData", ref dbPar);
        }

        public bool GenerateDataANTSNA(string fileType, string idDataList, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);
            dbPar[1] = new OleDbParameter("@pcIdData", idDataList);
            dbPar[2] = new OleDbParameter("@pnNIK", this._userNIK);
            dbPar[3] = new OleDbParameter("@pcBranch", this._userBranch);

            return this._cQuery.ExecProc("dbo.ANTSNAGenerateFile", ref dbPar, out dsOut);
        }

        public bool GenerateFile(string data, string path, out string errMsg)
        {
            errMsg = "";
            try
            {
                using (StreamWriter sw = new StreamWriter(path, false))
                {
                    sw.Write(data);
                    sw.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        public bool WriteXlsFile(DataSet dsData, out string errMsg)
        {
            errMsg = "";

            if (dsData == null || dsData.Tables.Count == 0)
            {
                errMsg = "There are no data to proceed !";
                return false;
            }

            try
            {
                Excel.Application xlApp = new Excel.ApplicationClass();
                Microsoft.Office.Interop.Excel.Workbook xlWorkbook = xlApp.Workbooks.Add(Type.Missing);

                int NoSheet = 0;
                for (int z = dsData.Tables.Count - 1; z >= 0; z--)
                {
                    NoSheet = (dsData.Tables.Count - z);
                    Excel.Worksheet xlWorksheet = (Excel.Worksheet)xlWorkbook.Worksheets[NoSheet];
                    if (NoSheet > 3)
                    {
                        xlWorksheet = (Excel.Worksheet)xlWorkbook.Sheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                        xlWorksheet.Name = "Sheet" + NoSheet.ToString();
                    }

                    for (int i = 0; i < dsData.Tables[z].Columns.Count; i++)
                    {
                        xlWorksheet.Cells[1, i + 1] = dsData.Tables[z].Columns[i].ColumnName.ToString();
                    }

                    List<Excel.Range> listHeader = new List<Microsoft.Office.Interop.Excel.Range>();
                    for (int u = 0; u < dsData.Tables[z].Columns.Count; u++)
                        listHeader.Add((Excel.Range)xlWorksheet.Cells[1, u + 1]);

                    //style
                    foreach (Excel.Range range in listHeader)
                    {
                        range.Font.Bold = true;
                        range.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                    }

                    //Content
                    for (int k = 0; k < dsData.Tables[z].Rows.Count; k++)
                    {
                        for (int j = 0; j < dsData.Tables[z].Columns.Count; j++)
                            xlWorksheet.Cells[2 + k, j + 1] = dsData.Tables[z].Rows[k][j].ToString();
                    }

                    xlWorksheet.Columns.AutoFit();
                    xlWorksheet.Rows.AutoFit();

                }

                xlApp.WindowState = Microsoft.Office.Interop.Excel.XlWindowState.xlNormal;
                xlApp.Visible = true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
            finally
            {
                // Cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return true;
        }

        #endregion ANTSNA Approval and Generate
        #region Update LCS
        //20220118, samy, LHBU-167, begin
        public bool UpdateLCS(string xmlData, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlData);

            return clsGlobal.QueryACC.ExecProc("dbo.AntasenaUpdateDataLCS", ref dbPar, out dsOut);
        }
        //20220118, samy, LHBU-167, end
        #endregion
    }
}
