using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace BankNISP.Obligasi01
{
    class UploadTreasuryProductResultModel
    {
        private ObligasiQuery _cQuery;
        private int _nUserNik;
        private IUploadTreasuryProductRslt _viewUploadTrsProductRslt;
        
        public UploadTreasuryProductResultModel(IUploadTreasuryProductRslt IUploadTRSProductRslt, ObligasiQuery cQuery, int userNIK)
        {
            this._cQuery = cQuery;
            this._nUserNik = userNIK;
            this._viewUploadTrsProductRslt = IUploadTRSProductRslt;
        }
        
        public bool PopulateProductType(out DataSet dsData)
        {
            dsData = new DataSet();
            OleDbParameter[] oParam = new OleDbParameter[1];
            oParam[0] = new OleDbParameter("@pcFilterExpression", "ProductTreasury != 'FLD'");

            return this._cQuery.ExecProc("dbo.TRSPopulateProductList", ref oParam, out dsData);
        }

        private bool ProcessData(out DataSet dsStructure) 
        {
            if (_viewUploadTrsProductRslt.idsUpload == null)
            {
                MessageBox.Show("Tidak ada data untuk di upload !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dsStructure = null;
                return false;
            }

            DataSet dsLog = new DataSet();

            DataSet dsData = new DataSet();
            dsData.Tables.Add(_viewUploadTrsProductRslt.idsUpload.Copy());

            dsData.DataSetName = "Data";
            dsData.Tables[0].TableName = "Parameter";
            StringBuilder dataSave = new StringBuilder();

            dsData.Tables[0].WriteXml(System.Xml.XmlWriter.Create(dataSave));

            dsStructure = new DataSet();
            OleDbParameter[] oParam = new OleDbParameter[3];
            oParam[0] = new OleDbParameter("@pxmlData", dataSave.ToString());
            oParam[1] = new OleDbParameter("@pcJenisProduk", _viewUploadTrsProductRslt.SelectedJenisProduct);
            oParam[2] = new OleDbParameter("@pnUserNik", _nUserNik);

            return this._cQuery.ExecProc("TRSSaveTreasuryProductResult", ref oParam, out dsStructure);
        }

        public void UploadData() 
        {
            if (MessageBox.Show("Apakah anda ingin mengupload data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DataSet dsStructure = null;
                if (ProcessData(out dsStructure))
                {
                    if (dsStructure.Tables[0].Rows.Count != this._viewUploadTrsProductRslt.idsUpload.Rows.Count)
                        MessageBox.Show("Data berhasil diupload !", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    if (dsStructure.Tables[0].Rows.Count > 0)
                    {
                        MessageBox.Show("Terdapat data yang gagal diupload !", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _viewUploadTrsProductRslt.idsLogError = dsStructure.Tables[0];
                    }
                    _viewUploadTrsProductRslt.idsUpload = null;
                }
                else
                {
                    MessageBox.Show("Terdapat data yang gagal diupload !", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
            }
        }

        public bool PopulateTreasuryResultProduct(string JenisProduk, out DataSet dsStructure)
        {
            dsStructure = new DataSet();
            OleDbParameter[] oParam = new OleDbParameter[1];
            oParam[0] = new OleDbParameter("@pcJenisProduk", JenisProduk);

            return this._cQuery.ExecProc("dbo.TRSPopulateTreasuryProductResult", ref oParam, out dsStructure);
        }

        private void GetTemplate()
        {
            DataSet dsStructure = new DataSet();
            DataTable newTable = new DataTable();

            //Declare dataset structure
            if (PopulateTreasuryResultProduct(_viewUploadTrsProductRslt.SelectedJenisProduct, out dsStructure))
            {
                _viewUploadTrsProductRslt.idsUpload = dsStructure.Tables[0].Clone();
                _viewUploadTrsProductRslt.idsUpload.Columns[0].DataType = typeof(String);

                if (dsStructure.Tables.Count.Equals(0))
                    return;
            }
        }

        public void OpenFile()
        {
            GetTemplate();

            OpenFileDialog openDialog = new OpenFileDialog();

            openDialog.Filter = "Excel 97-2003 Workbook(*.xls)|*.xls|Excel Workbook files (*.xlsx)|*.xlsx";
            openDialog.FilterIndex = 1;
            openDialog.Multiselect = false;
            openDialog.RestoreDirectory = true;

            openDialog.ShowDialog();

            if (!openDialog.FileName.Equals(""))
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(openDialog.FileName);
                _viewUploadTrsProductRslt.UploadText = fileInfo.Name;
                ReadExcelFile(openDialog.FileName);
            }
        }
        
        private void ReadExcelFile(string FilePath)
        {
            System.Data.OleDb.OleDbConnection conn;

            //conn = new System.Data.OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + FilePath + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1\"");
            conn = new System.Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + FilePath + ";Extended Properties=\"Excel 8.0;HDR=YES; IMEX =1\"");

            Excel.Application xlAPP = new Excel.Application();
            xlAPP.Visible = false;
            Microsoft.Office.Interop.Excel.Workbook xlWbk = xlAPP.Workbooks.Open(FilePath, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing);

            string worksheetName = "";

            foreach (Excel.Worksheet worksheet in xlWbk.Worksheets)
            {
                worksheetName = worksheet.Name.ToString();
                break;
            }

            releaseObject(xlWbk);
            releaseObject(xlAPP);

            System.Data.OleDb.OleDbCommand oconn = new System.Data.OleDb.OleDbCommand("Select * From [" + worksheetName + "$]", conn);
            if (conn.DataSource.Length > 0)
            {
                try
                {
                    conn.Open();
                    System.Data.OleDb.OleDbDataAdapter sda = new System.Data.OleDb.OleDbDataAdapter(oconn);
                    System.Data.DataSet data = new System.Data.DataSet();
                    sda.Fill(data, "ExcelInfo");

                    if (data.Tables[0].Columns.Count != this._viewUploadTrsProductRslt.idsUpload.Columns.Count)
                    {
                        MessageBox.Show("Data tidak sesuai !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (this._viewUploadTrsProductRslt.SelectedJenisProduct == "PPD")
                    {
                        for (int i = 0; i < data.Tables[0].Rows.Count; i++)
                        {
                            string[] temp = data.Tables[0].Rows[i]["Barrier"].ToString().Split('-');
                            if (temp.Length == 1)
                            {
                                if (data.Tables[0].Rows[i]["Barrier"].ToString() != "")
                                    data.Tables[0].Rows[i]["Barrier"] = Convert.ToDecimal(data.Tables[0].Rows[i]["Barrier"]);
                            }
                        }
                    }
                    else if (this._viewUploadTrsProductRslt.SelectedJenisProduct == "DCR")
                    {
                        for (int i = 0; i < data.Tables[0].Rows.Count; i++)
                        {
                            if (data.Tables[0].Rows[i]["Pay to Customer Amount"].ToString() != "")
                                data.Tables[0].Rows[i]["Pay to Customer Amount"] = Convert.ToDecimal(data.Tables[0].Rows[i]["Pay to Customer Amount"]);
                        }
                    }

                    this._viewUploadTrsProductRslt.idsUpload = data.Tables[0];
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message.ToString());
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
                MessageBox.Show("Unable to release the Object " + ex.ToString());
            }
            finally
            {
                GC.Collect();
            }
        }
        
        public void PopulateDCR() 
        { 
            DataSet dsData = null;
            if (PopulateDCRResultData(_viewUploadTrsProductRslt.DCRStartDate, _viewUploadTrsProductRslt.DCREndDate, out dsData)) 
            {
                _viewUploadTrsProductRslt.idsDCRData = dsData.Tables[0];
            }
        }

        public void PopulateDCRKIKO() 
        {
            DataSet dsData = null;
            if (PopulateDCRKIKOResultData(_viewUploadTrsProductRslt.DCRKIKOStartDate, _viewUploadTrsProductRslt.DCRKIKOEndDate, out dsData))
            {
                _viewUploadTrsProductRslt.idsDCRKIKOData = dsData.Tables[0];
            }
        }

        public void PopulatePPD() 
        {
            DataSet dsData = null;
            if (PopulatePPDResultData(_viewUploadTrsProductRslt.PPDStartDate, _viewUploadTrsProductRslt.PPDEndDate, out dsData))
            {
                _viewUploadTrsProductRslt.idsPPDData = dsData.Tables[0];
            }
        }

        private bool PopulateDCRResultData(string StartDate, string EndDate, out DataSet dsData)
        {
            dsData = new DataSet();

            OleDbParameter[] oParam = new OleDbParameter[2];
            oParam[0] = new OleDbParameter("@pdtStartDate", StartDate);
            oParam[1] = new OleDbParameter("@pdtEndDate", EndDate);

            return this._cQuery.ExecProc("TRSPopulateProductTreasuryDCRResult", ref oParam, out dsData);
        }

        private bool PopulateDCRKIKOResultData(string StartDate, string EndDate, out DataSet dsData) 
        {
            dsData = new DataSet();
            
            OleDbParameter[] oParam = new OleDbParameter[2];
            oParam[0] = new OleDbParameter("@pdtStartDate", StartDate);
            oParam[1] = new OleDbParameter("@pdtEndDate", EndDate);
            
            return this._cQuery.ExecProc("TRSPopulateProductTreasuryDCRKIKOResult", ref oParam, out dsData);
        }

        private bool PopulatePPDResultData(string StartDate, string EndDate, out DataSet dsData)
        {
            dsData = new DataSet();

            OleDbParameter[] oParam = new OleDbParameter[2];
            oParam[0] = new OleDbParameter("@pdtStartDate", StartDate);
            oParam[1] = new OleDbParameter("@pdtEndDate", EndDate);

            return this._cQuery.ExecProc("TRSPopulateProductTreasuryPPDResult", ref oParam, out dsData);
        }

        //20180925, hanssen.k, BOSIT18209, begin
        public void PopulateDCRBONUS()
        {
            DataSet dsData = null;
            if (PopulateDCRBONUSResultData(_viewUploadTrsProductRslt.DCRBONUSStartDate, _viewUploadTrsProductRslt.DCRBONUSEndDate, out dsData))
            {
                _viewUploadTrsProductRslt.idsDCRBONUSData = dsData.Tables[0];
            }
        }

        private bool PopulateDCRBONUSResultData(string StartDate, string EndDate, out DataSet dsData)
        {
            dsData = new DataSet();

            OleDbParameter[] oParam = new OleDbParameter[2];
            oParam[0] = new OleDbParameter("@pdtStartDate", StartDate);
            oParam[1] = new OleDbParameter("@pdtEndDate", EndDate);

            return this._cQuery.ExecProc("TRSPopulateProductTreasuryDCRBONUSResult", ref oParam, out dsData);
        }

        public void PopulateDCREKI()
        {
            DataSet dsData = null;
            if (PopulateDCREKIResultData(_viewUploadTrsProductRslt.DCREKIStartDate, _viewUploadTrsProductRslt.DCREKIEndDate, out dsData))
            {
                _viewUploadTrsProductRslt.idsDCREKIData = dsData.Tables[0];
            }
        }

        private bool PopulateDCREKIResultData(string StartDate, string EndDate, out DataSet dsData)
        {
            dsData = new DataSet();

            OleDbParameter[] oParam = new OleDbParameter[2];
            oParam[0] = new OleDbParameter("@pdtStartDate", StartDate);
            oParam[1] = new OleDbParameter("@pdtEndDate", EndDate);

            return this._cQuery.ExecProc("TRSPopulateProductTreasuryDCREKIResult", ref oParam, out dsData);
        }
        //20180925, hanssen.k, BOSIT18209, end
    }
}
