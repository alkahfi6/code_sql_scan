using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
//20250514, dion.wijna, BONDRETAIL-1696, begin
using BankNISP.Obligasi01.APIService;
using BankNISP.Obligasi01.Model;
//20250514, dion.wijna, BONDRETAIL-1696, end

namespace BankNISP.Obligasi01
{
    public partial class frmObligasiUpdateStatusTRX : BankNISP.Template.StandardForm
    {
        internal ObligasiQuery cQuery = new ObligasiQuery();
        public Int32 intNIK;
        public string strGuid;
        public string strLocalMenu;
        private DataView _dvAkses;
        private DataView _dvAksesNode;
        private string[] _strDefToolBar;
        public string strModule;
        public string strMenuName;

        System.Data.DataSet dsTree = new System.Data.DataSet();
        System.Data.DataSet dsGridMain = new System.Data.DataSet();
        System.Data.DataTable dtGridDetail = new System.Data.DataTable();

        System.Data.DataTable dtSelectedTree = new DataTable();

        private string _strNodeKey;
        //20120802, hermanto_salim, BAALN12003, begin  
        private clsCallWebService clsCallWebService;
        //20120802, hermanto_salim, BAALN12003, end  
        //20171128, samypasha, COPOD17323, begin
        public string _WFIFilenetWSAddress;
        public string _WFIDatacapUser;
        public string _WFIDatacapPassword;
        public string _WFIDatacapAddressEverest;
        public string _WFIFilenetAddress;
        public string _WFIDatacapKey;
        public string _WFIDatacapPasswordEncyrypted;
        //20171128, samypasha, COPOD17323, end
        // 20160310, fauzil, TRBST16240, begin
        private string ViewType = "";
        private string TrxType = "";
        private string SpText = "";
        // 20160310, fauzil, TRBST16240, end

        public string[] DefToolBar
        {
            get { return _strDefToolBar; }
            set { _strDefToolBar = value; }
        }

        private void subResetToolBar()
        {
            try
            {
                if (_dvAkses.Count > 0)
                {
                    string[] strVisibleToolbars = new string[_dvAkses.Count];
                    for (int i = 0; i < _dvAkses.Count; i++)
                    {
                        strVisibleToolbars[i] = _dvAkses[i]["IconId"].ToString();
                    }
                    this.NISPToolbarButtonSetVisible(true, strVisibleToolbars);
                }
                else
                {
                    this.NISPToolbarButtonSetVisible(true, _strDefToolBar);
                }
                if (strLocalMenu == "mnuUpdateStatusTRX")
                {
                    this.NISPToolbarButton("0").Visible = false;
                    this.NISPToolbarButton("2").Visible = false;
                    this.NISPToolbarButton("5").Visible = false;
                    this.NISPToolbarButton("7").Visible = false;
                    this.NISPToolbarButton("42").Visible = false;
                    this.NISPToolbarButton("43").Visible = false;
                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void subResetToolBarTreeNode()
        {
            if (_dvAksesNode.Count > 0)
            {
                string[] strVisibleToolbars = new string[_dvAksesNode.Count];
                for (int i = 0; i < _dvAksesNode.Count; i++)
                {
                    strVisibleToolbars[i] = _dvAksesNode[i]["IconId"].ToString();
                }
                if ((_strNodeKey.Equals("TRS11")) || (_strNodeKey.Equals("TRS12")) || (_strNodeKey.Equals("TRS13"))
                   || (_strNodeKey.Equals("TRS15")) || (_strNodeKey.Equals("TRS16")) || (_strNodeKey.Equals("TRS17")))
                {
                    this.NISPToolbarButton("43").Text = "Push Back";
                }
                else
                    this.NISPToolbarButton("43").Text = "Reject";
                // 20160128, fauzil, TRBST15176, Begin
                this.NISPToolbarButton("2").Visible = false;
                // 20160128, fauzil, TRBST15176, end
                this.NISPToolbarButtonSetVisible(true, strVisibleToolbars);
            }
            else
            {
                this.NISPToolbarButton("2").Visible = false;
                this.NISPToolbarButtonSetVisible(true, _strDefToolBar);
            }



        }

        public frmObligasiUpdateStatusTRX()
        {
            InitializeComponent();

        }

        public void InitializeForm(string strStatus)
        {

            switch (strStatus)
            {
                case "Awal":
                    string[] strMain = new string[3];
                    strMain[0] = "0";
                    strMain[1] = "1";
                    strMain[2] = "18"; //AUTHORISATION
                    this.NISPToolbarButtonSetVisible(true, strMain);
                    break;
                case "Check":
                    string[] strMain1 = new string[4];
                    strMain1[0] = "0";
                    strMain1[1] = "1";
                    strMain1[2] = "18";
                    strMain1[3] = "5"; //DELETE
                    this.NISPToolbarButtonSetVisible(true, strMain1);
                    break;
                case "Confirm":
                    string[] strMain2 = new string[4];
                    strMain2[0] = "0";
                    strMain2[1] = "1";
                    strMain2[2] = "18";
                    strMain2[3] = "43"; //REJECT
                    this.NISPToolbarButtonSetVisible(true, strMain2);
                    break;
                case "Approve":
                    string[] strMain3 = new string[4];
                    strMain3[0] = "0";
                    strMain3[1] = "1";
                    strMain3[2] = "18";
                    strMain3[3] = "43";
                    this.NISPToolbarButtonSetVisible(true, strMain3);
                    break;
            }
        }

        private void frmObligasiUpdateStatusTRX_OnNISPToolbarClick(ref ToolStripButton NISPToolbarButton)
        {


            switch (NISPToolbarButton.Name)
            {
                case ("1"): //keluar
                    {
                        this.Close();
                        break;
                    }
                case ("5"):
                    {
                        subDeleteTransaction();
                        break;
                    }
                case ("7"):
                    {
                        subCancelOrder();
                        break;
                    }
                case ("42"): // process
                    {
                        subAcceptReject(true);
                        break;
                    }
                case ("43"): // reject
                    {
                        subAcceptReject(false);
                        break;
                    }
            }
        }

        private void subAcceptReject(bool isAccept)
        {
            Boolean blnReturn = false;
            string strPopulate = isAccept ? dtSelectedTree.Rows[0]["button1_query"].ToString() : dtSelectedTree.Rows[0]["button2_query"].ToString();
            string strAcceptReject = isAccept ? "Proses" : "Reject";
            System.Data.OleDb.OleDbParameter[] dbParam;
            System.Data.DataSet dsDummy;

            string strCommand;

            dgvMain.EndEdit();
            // 20160314, fauzil, TRBST16240, begin
            TransaksiSuratBerharga tsb = new TransaksiSuratBerharga();
            // 20160314, fauzil, TRBST16240, end

            //20170726, agireza, TRBST16240, begin
            Dictionary<string, decimal> dict = new Dictionary<string, decimal>();
            //20170726, agireza, TRBST16240, end

            //20120802, hermanto_salim, BAALN12003, begin  
            List<int> securityIDs = new List<int>();
            //20120802, hermanto_salim, BAALN12003, end  

            for (int i = 0; i < dsGridMain.Tables[0].Rows.Count; i++)
            {
                // if (System.Convert.ToBoolean(dsGridMain.Tables[0].Rows[i]["checked"].ToString()))
                //{
                if (dsGridMain.Tables[0].Rows[i]["checked"].ToString() == "True")
                {
                    string cOut = "";
                    string[] strArCmd = strPopulate.Split('&');
                    strCommand = fnCreateCommand1(strPopulate, out dbParam, i);

                    for (int j = 0; j < dsGridMain.Tables[0].Columns.Count; j++)
                    {
                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "DealNo")
                        {
                            cOut = strAcceptReject + " Deal No : " + dsGridMain.Tables[0].Rows[i]["DealNo"].ToString();
                            break;
                        }
                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "OrderId")
                        {
                            cOut = strAcceptReject + " Order Id :" + dsGridMain.Tables[0].Rows[i]["OrderId"].ToString();
                            break;
                        }
                        else
                        {
                            if (dsGridMain.Tables[0].Columns["CIFNo"] != null)
                                cOut = strAcceptReject + " No CIF : " + dsGridMain.Tables[0].Rows[i]["CIFNo"].ToString();
                            else
                                cOut = strAcceptReject + " No Security : " + dsGridMain.Tables[0].Rows[i]["SecAccNoDebet"].ToString();
                        }

                    }

                    System.IO.TextReader txtReader = new System.IO.StringReader(dbParam[0].Value.ToString());

                    DataSet dsXmlReader = new DataSet();
                    dsXmlReader.ReadXml(txtReader);

                    if (isAccept && (_strNodeKey == "TRS3" || _strNodeKey == "TRS4") && dsXmlReader.Tables[0].Rows[0]["NoRekInvestor"].ToString().Trim() == "504800000011")
                    {
                        if (MessageBox.Show("No Rekening Relasi Milik GNC TROPS\nMau Melanjutkan Proses Verify?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            continue;
                        }
                    }

                    //20120802, hermanto_salim, BAALN12003, begin  
                    //blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                    switch (strCommand)
                    {
                        //20170428, agireza, TRBST16240, begin
                        case "trs_CancelTopUpOriOrder_Temp":
                            {
                                strCommand = "trs_CancelTopUpOriOrder_Temp";

                                #region Delete Block Account
                                int iSecId = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["SecId"].ToString());
                                int CIFId = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["CIFId"].ToString());

                                int nSequence = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["AccountBlockSequence"].ToString());
                                string strAccountNo = dsGridMain.Tables[0].Rows[i]["NoRekInvestor"].ToString();
                                string strIssueNIK = dsGridMain.Tables[0].Rows[i]["InsertedBy"].ToString();
                                string AccountType = dsGridMain.Tables[0].Rows[i]["AccountBlockACTYPE"].ToString();
                                string strError = "";

                                clsCallWebService.CallOBL_WSDeleteBlockAccount(strAccountNo, AccountType, nSequence, strIssueNIK, out strError);

                                if (!string.IsNullOrEmpty(strError))
                                {
                                    MessageBox.Show("Lepas blokir gagal (" + strError + "), Data tidak dapat di Execute.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                else
                                {
                                    MessageBox.Show("Rekening berhasil dilepas", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                #endregion

                                blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                break;
                            }
                        case "trs_updateTopUpORI_Order_Temp":
                            {
                                if (dsGridMain.Tables[0].Rows.Count > 0)
                                {
                                    #region Delete Block Account
                                    int iSecId = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["SecId"].ToString());
                                    int CIFId = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["CIFId"].ToString());

                                    //int nSequence = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["AccountBlockSequence"].ToString());
                                    int nSequence = 0;
                                    string strAccountNo = dsGridMain.Tables[0].Rows[i]["NoRekInvestor"].ToString();
                                    string strIssueNIK = dsGridMain.Tables[0].Rows[i]["InsertedBy"].ToString();
                                    string AccountType = dsGridMain.Tables[0].Rows[i]["AccountBlockACTYPE"].ToString();
                                    string strError = "";

                                    clsCallWebService.CallOBL_WSDeleteBlockAccount(strAccountNo, AccountType, nSequence, strIssueNIK, out strError);

                                    if (!string.IsNullOrEmpty(strError))
                                    {
                                        MessageBox.Show("Lepas blokir gagal (" + strError + "), Data tidak dapat di Execute.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    else
                                    {
                                        int nPriority = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["Priority"]);
                                        if (nPriority == 0)
                                        {
                                            #region Delete Block Account Main Order
                                            AccountType = dsGridMain.Tables[0].Rows[i]["MainAccountBlockACTYPE"].ToString();
                                            strIssueNIK = dsGridMain.Tables[0].Rows[i]["MainInsertedBy"].ToString();
                                            //nSequence = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["MainAccountBlockSequence"].ToString());
                                            nSequence = 0;
                                            strError = "";

                                            clsCallWebService.CallOBL_WSDeleteBlockAccount(strAccountNo, AccountType, nSequence, strIssueNIK, out strError);

                                            if (!string.IsNullOrEmpty(strError))
                                            {
                                                MessageBox.Show("Lepas blokir gagal (" + strError + "), Data tidak dapat di Execute.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                            else
                                            {
                                                #region Block Account
                                                decimal BlockAmount = 0;
                                                if (dict.TryGetValue(strAccountNo, out BlockAmount))
                                                {
                                                    dict[strAccountNo] = BlockAmount + Convert.ToDecimal(dsGridMain.Tables[0].Rows[i]["OrderNominal"].ToString());
                                                }
                                                else
                                                {
                                                    BlockAmount = Convert.ToDecimal(dsGridMain.Tables[0].Rows[i]["MainOrderNominal"].ToString()) + Convert.ToDecimal(dsGridMain.Tables[0].Rows[i]["OrderNominal"].ToString());
                                                    dict.Add(strAccountNo, BlockAmount);
                                                }

                                                string strLogDesc = "";
                                                nSequence = 0;
                                                string strRecordID = "";
                                                string strTypeOfEntry = "HG";
                                                int nLowCheckNo = 0;
                                                int nHighCheckNo = 0;
                                                int nStopCharge = 0;
                                                string strPayeeName = "";
                                                string strHoldRemarks = "ProObligasi Order " + dsGridMain.Tables[0].Rows[i]["SecurityNo"].ToString();
                                                int nCheckRTNumber = 0;
                                                string strExpirationDate = ""; // harus diisi
                                                int nCheckDate = 0;
                                                int nDateMaintenance = 0;
                                                string strDatePlaced = ""; // harus diisi
                                                int nHoldBranch = int.Parse((string)BankNISP.Obligasi01.clsGlobal.dsUserProfile.Tables[0].Rows[0]["office_id_sibs"]);
                                                string strWorkStationID = "ProObligasi"; // harus diisi
                                                int nTimeChangeMade = 0;
                                                string strReasonCode = ""; // harus diisi
                                                int nNik = Convert.ToInt32(strIssueNIK);
                                                string strPrefixCheckNo = "";
                                                System.Data.DataSet dsResponse = new System.Data.DataSet();

                                                BlockAmount = dict[strAccountNo];

                                                clsCallWebService.CallOBL_WSAddBlockAccount(strLogDesc, strAccountNo, AccountType, nSequence, strRecordID,
                                                        strTypeOfEntry, BlockAmount, nLowCheckNo, nHighCheckNo
                                                        , nStopCharge, strPayeeName, strHoldRemarks, nCheckRTNumber
                                                        , strExpirationDate, nCheckDate, nDateMaintenance, strDatePlaced, nHoldBranch
                                                        , nNik, strWorkStationID, nTimeChangeMade, strReasonCode
                                                        , strPrefixCheckNo, out dsResponse, out strError);

                                                if (!string.IsNullOrEmpty(strError))
                                                {
                                                    MessageBox.Show("Gagal ketika melakukan block rekening,\nerror message: " + strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                }
                                                else
                                                {
                                                    MessageBox.Show("Rekening di block:\nNomor Rekening: " + strAccountNo + ",\nNominal: " + String.Format("{0:n}", BlockAmount) + ".", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                                    //nSequence = Convert.ToInt32(dsResponse.Tables[0].Rows[0]["Sequence"].ToString());
                                                    nSequence = 0;
                                                    //AccountType = dsResponse.Tables[0].Rows[0]["ACTYPE"].ToString();
                                                    AccountType = "";

                                                    #region update data dengan block seq baru
                                                    int OrderId = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["OrderIdReal"].ToString());
                                                    string xmlParam = dbParam[0].Value.ToString();
                                                    xmlParam = xmlParam.Replace("</RS>", "<Sequence>" + nSequence.ToString() + "</Sequence><AccountType>" + AccountType + "</AccountType></RS>");
                                                    dbParam[0].Value = xmlParam;
                                                    #endregion

                                                    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                                }
                                                #endregion
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Block Account
                                            decimal BlockAmount = Convert.ToDecimal(dsGridMain.Tables[0].Rows[i]["OrderNominal"].ToString());
                                            string strLogDesc = "";
                                            nSequence = 0;
                                            string strRecordID = "";
                                            string strTypeOfEntry = "HG";
                                            int nLowCheckNo = 0;
                                            int nHighCheckNo = 0;
                                            int nStopCharge = 0;
                                            string strPayeeName = "";
                                            string strHoldRemarks = "ProObligasi Order " + dsGridMain.Tables[0].Rows[i]["SecurityNo"].ToString();
                                            int nCheckRTNumber = 0;
                                            string strExpirationDate = ""; // harus diisi
                                            int nCheckDate = 0;
                                            int nDateMaintenance = 0;
                                            string strDatePlaced = ""; // harus diisi
                                            int nHoldBranch = int.Parse((string)BankNISP.Obligasi01.clsGlobal.dsUserProfile.Tables[0].Rows[0]["office_id_sibs"]);
                                            string strWorkStationID = "ProObligasi"; // harus diisi
                                            int nTimeChangeMade = 0;
                                            string strReasonCode = ""; // harus diisi
                                            int nNik = Convert.ToInt32(strIssueNIK);
                                            string strPrefixCheckNo = "";
                                            System.Data.DataSet dsResponse = new System.Data.DataSet();

                                            BlockAmount = dict[strAccountNo];

                                            clsCallWebService.CallOBL_WSAddBlockAccount(strLogDesc, strAccountNo, AccountType, nSequence, strRecordID,
                                                    strTypeOfEntry, BlockAmount, nLowCheckNo, nHighCheckNo
                                                    , nStopCharge, strPayeeName, strHoldRemarks, nCheckRTNumber
                                                    , strExpirationDate, nCheckDate, nDateMaintenance, strDatePlaced, nHoldBranch
                                                    , nNik, strWorkStationID, nTimeChangeMade, strReasonCode
                                                    , strPrefixCheckNo, out dsResponse, out strError);

                                            if (!string.IsNullOrEmpty(strError))
                                            {
                                                MessageBox.Show("Gagal ketika melakukan block rekening,\nerror message: " + strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                            else
                                            {
                                                MessageBox.Show("Rekening di block:\nNomor Rekening: " + strAccountNo + ",\nNominal: " + String.Format("{0:n}", BlockAmount) + ".", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                                //nSequence = Convert.ToInt32(dsResponse.Tables[0].Rows[0]["Sequence"].ToString());
                                                nSequence = 0;
                                                //AccountType = dsResponse.Tables[0].Rows[0]["ACTYPE"].ToString();
                                                AccountType = "";

                                                #region update data dengan block seq baru
                                                int OrderId = Convert.ToInt32(dsGridMain.Tables[0].Rows[i]["OrderIdReal"].ToString());
                                                string xmlParam = dbParam[0].Value.ToString();
                                                xmlParam = xmlParam.Replace("</RS>", "<Sequence>" + nSequence.ToString() + "</Sequence><AccountType>" + AccountType + "</AccountType></RS>");
                                                dbParam[0].Value = xmlParam;
                                                #endregion

                                                blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    blnReturn = true;
                                }
                                break;
                            }
                        //20170428, agireza, TRBST16240, end
                        case "trs_CancelOriOrder_TT":
                            {
                                int secId = (int)dsGridMain.Tables[0].Rows[i]["SecId"];
                                //20160815, fauzil, LOGEN191, begin
                                string NoRekInvestor = dsGridMain.Tables[0].Rows[i]["NoRekInvestor"].ToString().Trim();
                                //20160815, fauzil, LOGEN191, end
                                if (!securityIDs.Contains(secId))
                                    securityIDs.Add(secId);
                                //20160617, fauzil, TRBST16240, begin
                                //bool applyReleaseAccount = clsGlobal.cancelORIOrderApplyReleaseAccount;
                                string value = "";
                                bool applyReleaseAccount = false;
                                if (clsDatabase.getParamaterTR(cQuery, "cclORIOrdAppRelAcc", out value))
                                {
                                    applyReleaseAccount = Convert.ToBoolean(value);
                                }
                                //20160617, fauzil, TRBST16240, end
                                if (applyReleaseAccount)
                                {
                                    OleDbParameter[] oleDbParameters = new OleDbParameter[4];
                                    oleDbParameters[0] = dbParam[0];
                                    oleDbParameters[1] = dbParam[1];
                                    oleDbParameters[2] = new OleDbParameter("@pbCommitImmediately", OleDbType.Boolean);
                                    oleDbParameters[2].Value = false;
                                    oleDbParameters[3] = new OleDbParameter("@pcORI_Order_TT_XML", OleDbType.VarChar, int.MaxValue);
                                    oleDbParameters[3].Direction = ParameterDirection.Output;

                                    blnReturn = cQuery.ExecProc(strCommand, ref oleDbParameters, out dsDummy);

                                    if (!blnReturn)
                                        break;

                                    // unblock rekening
                                    if (dsGridMain.Tables[0].Rows[i]["AccountBlockSequence"].GetType() != typeof(DBNull))
                                    {
                                        System.Data.DataSet dsResultCustomer = new System.Data.DataSet();
                                        string currency = dsGridMain.Tables[0].Rows[i]["SecCcy"].ToString();
                                        if (!clsDatabase.subtrs_ListTreasuryCustomer_TM_Original(cQuery, new object[] { (string)dsGridMain.Tables[0].Rows[i]["SecAccNo"], currency }, out dsResultCustomer))
                                        {
                                            blnReturn = false;
                                            break;
                                        }
                                        //20160815, fauzil, LOGEN191, begin
                                        //string strAccountNo = (string)dsResultCustomer.Tables[0].Rows[0]["NoRekInvestor"];
                                        string strAccountNo = NoRekInvestor;
                                        //20160815, fauzil, LOGEN191, end
                                        string strAccountType = (string)dsResultCustomer.Tables[0].Rows[0]["ACTYPE"];
                                        int nSequence = (int)dsGridMain.Tables[0].Rows[i]["AccountBlockSequence"];
                                        //20130423, victor, BAALN12003, begin
                                        //int nNik = (int)clsGlobal.dsUserProfile.Tables[0].Rows[0]["nik"];
                                        int nNik = (int)dsGridMain.Tables[0].Rows[i]["InsertedBy"];
                                        //20130423, victor, BAALN12003, end
                                        System.Data.DataSet dsResponse = new System.Data.DataSet();
                                        string strError = "";
                                        clsCallWebService.CallOBL_WSDeleteBlockAccount(strAccountNo, strAccountType, nSequence, nNik.ToString(), out  strError);

                                        if (!string.IsNullOrEmpty(strError))
                                        {
                                            //20140109, Samy, LOGAM05910, begin   
                                            //MessageBox.Show("Gagal ketika melakukan unblock rekening,\nerror message: " + strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            //blnReturn = false;
                                            //break;
                                            MessageBox.Show("Lepas blokir gagal, mohon lakukan lepas blokir secara manual", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            //20140109, Samy, LOGAM05910, end
                                        }
                                    }

                                    // commit sql data
                                    bool blnReturn3 = clsDatabase.subTRSProcessCancelORIOrderCommit(cQuery, new object[] { (string)oleDbParameters[3].Value });

                                    if (!blnReturn3)
                                    {
                                        blnReturn = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    OleDbParameter[] oleDbParameters = new OleDbParameter[4];
                                    oleDbParameters[0] = dbParam[0];
                                    oleDbParameters[1] = dbParam[1];
                                    oleDbParameters[2] = new OleDbParameter("@pbCommitImmediately", OleDbType.Boolean);
                                    oleDbParameters[2].Value = true;
                                    oleDbParameters[3] = new OleDbParameter("@pcORI_Order_TT_XML", OleDbType.VarChar, int.MaxValue);
                                    oleDbParameters[3].Direction = ParameterDirection.Output;

                                    blnReturn = cQuery.ExecProc(strCommand, ref oleDbParameters, out dsDummy);
                                }
                                break;
                            }
                        case "trs_DeleteStatusSecurityTransaction_TT":
                            {
                                // 20160721, fauzil, TRBST16240, begin
                                //bool applyReleaseAccount = clsGlobal.deleteSecurityTransactionApplyReleaseAccount;
                                ////20160815, fauzil, LOGEN191, begin
                                //string NoRekInvestor = dsGridMain.Tables[0].Rows[i]["NoRekInvestor"].ToString().Trim();
                                ////20160815, fauzil, LOGEN191, end
                                //if (applyReleaseAccount)
                                //{
                                //    OleDbParameter[] oleDbParameters = new OleDbParameter[4];
                                //    oleDbParameters[0] = dbParam[0];
                                //    oleDbParameters[1] = dbParam[1];
                                //    oleDbParameters[2] = new OleDbParameter("@pbCommitImmediately", OleDbType.Boolean);
                                //    oleDbParameters[2].Value = false;
                                //    oleDbParameters[3] = new OleDbParameter("@pcSecurityTransaction_TT_XML", OleDbType.VarChar, int.MaxValue);
                                //    oleDbParameters[3].Direction = ParameterDirection.Output;

                                //    blnReturn = cQuery.ExecProc(strCommand, ref oleDbParameters, out dsDummy);

                                //    if (!blnReturn)
                                //        break;

                                //    // unblock rekening
                                //    if (dsGridMain.Tables[0].Rows[i]["AccountBlockSequence"].GetType() != typeof(DBNull))
                                //    {
                                //        System.Data.DataSet dsResultCustomer = new System.Data.DataSet();
                                //        string currency = dsGridMain.Tables[0].Rows[i]["SecCcy"].ToString();
                                //        if (!clsDatabase.subtrs_ListTreasuryCustomer_TM_Original(cQuery, new object[] { (string)dsGridMain.Tables[0].Rows[i]["SecAccNo"], currency }, out dsResultCustomer))
                                //        {
                                //            blnReturn = false;
                                //            break;
                                //        }
                                //        //20160815, fauzil, LOGEN191, begin
                                //        //string strAccountNo = (string)dsResultCustomer.Tables[0].Rows[0]["NoRekInvestor"];
                                //        string strAccountNo;
                                //        if (NoRekInvestor.Length > 0)
                                //            strAccountNo = NoRekInvestor;
                                //        else strAccountNo = (string)dsResultCustomer.Tables[0].Rows[0]["NoRekInvestor"];
                                //        //20160815, fauzil, LOGEN191, end
                                //        string strAccountType = (string)dsResultCustomer.Tables[0].Rows[0]["ACTYPE"];
                                //        int nSequence = (int)dsGridMain.Tables[0].Rows[i]["AccountBlockSequence"];
                                //        //20130423, victor, BAALN12003, begin
                                //        //int nNik = (int)clsGlobal.dsUserProfile.Tables[0].Rows[0]["nik"];
                                //        int nNik = (int)dsGridMain.Tables[0].Rows[i]["InsertedBy"];
                                //        //20130423, victor, BAALN12003, end
                                //        System.Data.DataSet dsResponse = new System.Data.DataSet();
                                //        string strError = "";
                                //        clsCallWebService.CallOBL_WSDeleteBlockAccount(strAccountNo, strAccountType, nSequence, nNik.ToString(), out  strError);

                                //        if (!string.IsNullOrEmpty(strError))
                                //        {
                                //            //20140109, Samy, LOGAM05910, begin   
                                //            //MessageBox.Show("Gagal ketika melakukan unblock rekening,\nerror message: " + strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                //            //blnReturn = false;
                                //            //break;
                                //            MessageBox.Show("Lepas blokir gagal, mohon lakukan lepas blokir secara manual", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                //            //20140109, Samy, LOGAM05910, end
                                //        }
                                //    }

                                //    // commit sql data
                                //    bool blnReturn3 = clsDatabase.subTRSProcessDeleteSecurityTransactionCommit(cQuery, new object[] { (string)oleDbParameters[3].Value });

                                //    if (!blnReturn3)
                                //    {
                                //        blnReturn = false;
                                //        break;
                                //    }
                                //}
                                //else
                                //{
                                //    OleDbParameter[] oleDbParameters = new OleDbParameter[4];
                                //    oleDbParameters[0] = dbParam[0];
                                //    oleDbParameters[1] = dbParam[1];
                                //    oleDbParameters[2] = new OleDbParameter("@pbCommitImmediately", OleDbType.Boolean);
                                //    oleDbParameters[2].Value = true;
                                //    oleDbParameters[3] = new OleDbParameter("@pcORI_Order_TT_XML", OleDbType.VarChar, int.MaxValue);
                                //    oleDbParameters[3].Direction = ParameterDirection.Output;

                                //    blnReturn = cQuery.ExecProc(strCommand, ref oleDbParameters, out dsDummy);
                                //}
                                // perubahan tidak ada reject melainkan push back mengembalikan data ke inputer untuk di koreksi
                                bool CanProcess = false;
                                string ErrorMessage = "";
                                clsDatabase.subTRSValidateCutOffTimeTransaction(cQuery, "2", dsGridMain.Tables[0].Rows[i]["SecurityNo"].ToString(), out CanProcess, out ErrorMessage);
                                if (!CanProcess)
                                {
                                    MessageBox.Show(ErrorMessage, "Warning!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                                else
                                {
                                    cOut = cOut.Replace("Reject", "Push Back");
                                    string CIFNo = dsGridMain.Tables[0].Rows[i]["CIFNo"].ToString().Trim();
                                    string Nama = dsGridMain.Tables[0].Rows[i]["Nama"].ToString().Trim();
                                    string DealId = dsGridMain.Tables[0].Rows[i]["DealId"].ToString().Trim();
                                    string NIKSeller = dsGridMain.Tables[0].Rows[i]["NIKSeller"].ToString().Trim();
                                    string NamaSeller = dsGridMain.Tables[0].Rows[i]["NamaSeller"].ToString().Trim();
                                    string NIKInputer = dsGridMain.Tables[0].Rows[i]["NIKInputer"].ToString().Trim();
                                    string NameInputer = dsGridMain.Tables[0].Rows[i]["NameInputer"].ToString().Trim();
                                    //20180124, samypasha, TRBST16240, begin
                                    string SecAccNo = dsGridMain.Tables[0].Rows[i]["SecAccNo"].ToString().Trim();
                                    //20180124, samypasha, TRBST16240, end

                                    //sent email
                                    //20180124, samypasha, TRBST16240, begin
                                    //SentEmail(CIFNo, Nama, DealId, NIKSeller, NamaSeller, NIKInputer, NameInputer);
                                    SentEmail(CIFNo, Nama, DealId, NIKSeller, NamaSeller, NIKInputer, NameInputer, SecAccNo);
                                    //20180124, samypasha, TRBST16240, end

                                    OleDbParameter[] oleDbParameters = new OleDbParameter[2];
                                    oleDbParameters[0] = dbParam[0];
                                    oleDbParameters[1] = dbParam[1];
                                    blnReturn = cQuery.ExecProc("trs_PushBackStatusSecurityTransaction_TT", ref oleDbParameters, out dsDummy);

                                }

                                break;
                                // 20160721, fauzil, TRBST16240, end
                            }
                        // 20160129, fauzil, TRBST16240, begin
                        // add unblock rekening kemudian block dengang total yang baru jika tipenya
                        case "trs_updateStatusORI_Order_Temp":
                            {
                                bool OK = false;
                                long OrderIdTemp = long.Parse(dsGridMain.Tables[0].Rows[i]["OrderId"].ToString());

                                DataSet dsNikInputer = new System.Data.DataSet();
                                dsNikInputer = getAccBlockSequnceAndSeqAcBaseOrderIDTemp(OrderIdTemp, "1");

                                cOut = strAcceptReject + " Order Id :" + dsNikInputer.Tables[0].Rows[0]["OrderIdReal"].ToString();
                                if (dsNikInputer.Tables[0].Rows.Count > 0)
                                {
                                    // cek apakah nik inputer sama dengan nik approval
                                    if (dsNikInputer.Tables[0].Rows[0]["InsertedBy"].GetType() == typeof(DBNull))
                                    {
                                        cOut = cOut + "\nData nik yang melakukan perubahan/koreksi kosong.";
                                        blnReturn = false;
                                        break;
                                    }
                                    int nikKoreksi = (int)dsNikInputer.Tables[0].Rows[0]["InsertedBy"];
                                    if (nikKoreksi == intNIK)
                                    {
                                        cOut = cOut + "\nNik yang melakukan perubahan dengan Otorisasi tidak boleh sama.";
                                        blnReturn = false;
                                        break;
                                    }

                                    // cek apakah yang berubah order nominal
                                    clsDatabase.CheckIsThereAnyOrderCorrection(this.cQuery, OrderIdTemp, out OK);
                                    if (!OK)
                                    {
                                        // jika order berubah unblock
                                        int secId = (int)dsGridMain.Tables[0].Rows[i]["SecId"];
                                        if (!securityIDs.Contains(secId))
                                            securityIDs.Add(secId);
                                        bool applyReleaseAccount = clsGlobal.ApprovePerubahanOrderSuratBerhargaOnApplyReleaseAccount;
                                        //20190314, rezakahfi, BOSIT18140, begin
                                        if (decimal.Parse((dsGridMain.Tables[0].Rows[i]["OrderNominal"].ToString())) == decimal.Parse((dsGridMain.Tables[0].Rows[i]["MainOrderNominal"].ToString())))
                                        {
                                            applyReleaseAccount = false;
                                            blnReturn = true;


                                            dbParam[dbParam.Length - 2] = new System.Data.OleDb.OleDbParameter("@AccountBlockSequence", System.Data.OleDb.OleDbType.Integer);
                                            dbParam[dbParam.Length - 2].Direction = ParameterDirection.Input;
                                            dbParam[dbParam.Length - 2].Value = (int)dsNikInputer.Tables[0].Rows[0]["AccountBlockSequence"];
                                            dbParam[dbParam.Length - 1] = new System.Data.OleDb.OleDbParameter("@BlokingAmount", System.Data.OleDb.OleDbType.Decimal);
                                            dbParam[dbParam.Length - 1].Direction = ParameterDirection.Input;
                                            dbParam[dbParam.Length - 1].Value = decimal.Parse((dsGridMain.Tables[0].Rows[i]["OrderNominal"].ToString()));
                                            blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                        }
                                        //20190314, rezakahfi, BOSIT18140, end
                                        if (applyReleaseAccount)
                                        {
                                            if (dsNikInputer.Tables[0].Rows.Count > 0)
                                            {
                                                string strAccountNo = "";
                                                string strAccountType = "";
                                                int nNik = 0;
                                                // unblock rekening
                                                if (dsNikInputer.Tables[0].Rows[0]["AccountBlockSequence"].GetType() != typeof(DBNull))
                                                {
                                                    System.Data.DataSet dsResultCustomer = new System.Data.DataSet();
                                                    string currency = dsNikInputer.Tables[0].Rows[0]["SecCcy"].ToString();
                                                    if (!clsDatabase.subtrs_ListTreasuryCustomer_TM_Original(cQuery, new object[] { (string)dsGridMain.Tables[0].Rows[i]["SecAccNo"], currency }, out dsResultCustomer))
                                                    {
                                                        blnReturn = false;
                                                        break;
                                                    }

                                                    strAccountNo = (string)dsNikInputer.Tables[0].Rows[0]["NoRekInvestor"];
                                                    strAccountType = (string)dsNikInputer.Tables[0].Rows[0]["AccountBlockACTYPE"];
                                                    int nSequence = (int)dsNikInputer.Tables[0].Rows[0]["AccountBlockSequence"];
                                                    nNik = (int)dsNikInputer.Tables[0].Rows[0]["InsertedBy"];
                                                    System.Data.DataSet dsResponse = new System.Data.DataSet();
                                                    string strError = "";
                                                    clsCallWebService.CallOBL_WSDeleteBlockAccount(strAccountNo, strAccountType, nSequence, nNik.ToString(), out  strError);

                                                    if (!string.IsNullOrEmpty(strError))
                                                    {
                                                        //20160617, fauzil, TRBST16240, begin
                                                        //cOut = cOut + "\nLepas blokir gagal, Data tidak dapat di Execute.";
                                                        cOut = cOut + "\nLepas blokir gagal (" + strError + "), Data tidak dapat di Execute.";
                                                        //20160617, fauzil, TRBST16240, end
                                                        blnReturn = false;
                                                    }
                                                    else
                                                    {
                                                        // jika unblock success, block data dengan data baru
                                                        cOut = cOut + "\nLepas blokir berhasil";
                                                        // buat flagging klo lepas blokir berhasil..  (next)

                                                        //block rekening
                                                        string strLogDesc = "";
                                                        string strAccountName = (string)dsResultCustomer.Tables[0].Rows[0]["SNAME"];
                                                        int nSequenceBlock = 0;
                                                        string strRecordID = "";
                                                        string strTypeOfEntry = "HG";
                                                        int nLowCheckNo = 0;
                                                        int nHighCheckNo = 0;
                                                        int nStopCharge = 0;
                                                        string strPayeeName = "";
                                                        string strHoldRemarks = "ProObligasi Order " + dsNikInputer.Tables[0].Rows[0]["OrderIdReal"].ToString(); // harus diisi
                                                        int nCheckRTNumber = 0;
                                                        string strExpirationDate = ""; // harus diisi
                                                        int nCheckDate = 0;
                                                        int nDateMaintenance = 0;
                                                        string strDatePlaced = ""; // harus diisi
                                                        int nHoldBranch = int.Parse((string)BankNISP.Obligasi01.clsGlobal.dsUserProfile.Tables[0].Rows[0]["office_id_sibs"]);
                                                        string strWorkStationID = "ProObligasi"; // harus diisi
                                                        int nTimeChangeMade = 0;
                                                        string strReasonCode = ""; // harus diisi
                                                        //20160623, fauzil, TRBST16240, begin
                                                        //nNik = (int)BankNISP.Obligasi01.clsGlobal.dsUserProfile.Tables[0].Rows[0]["nik"];
                                                        //20160623, fauzil, TRBST16240, end
                                                        string strPrefixCheckNo = "";
                                                        System.Data.DataSet dsResponseBlock = new System.Data.DataSet();
                                                        //20140904, samypasha, LOGAM06620, begin
                                                        string CIFId = dsResultCustomer.Tables[0].Rows[0]["CIFId"].ToString();
                                                        //20140904, samypasha, LOGAM06620, end

                                                        //blokir amount harus ditambah minimum balance atau closing fee, mana yang lebih besar
                                                        decimal BlokirAmount = decimal.Parse((dsGridMain.Tables[0].Rows[i]["OrderNominal"].ToString()));
                                                        clsCallWebService.CallOBL_WSAddBlockAccount(strLogDesc, strAccountNo, strAccountType, nSequenceBlock, strRecordID,
                                                                    strTypeOfEntry, BlokirAmount, nLowCheckNo, nHighCheckNo
                                                                    , nStopCharge, strPayeeName, strHoldRemarks, nCheckRTNumber
                                                                    , strExpirationDate, nCheckDate, nDateMaintenance, strDatePlaced, nHoldBranch
                                                                    , nNik, strWorkStationID, nTimeChangeMade, strReasonCode
                                                                    , strPrefixCheckNo, out dsResponse, out strError);

                                                        if (!string.IsNullOrEmpty(strError))
                                                        {
                                                            cOut = cOut + "\nGagal ketika melakukan block rekening (" + strError + ")";
                                                            blnReturn = false;
                                                        }
                                                        else
                                                        {

                                                            MessageBox.Show("Rekening di block:\nNomor Rekening: " + strAccountNo + ",\nNama Rekening: " + strAccountName + ",\nNominal: " + String.Format("{0:n}", BlokirAmount) + ".", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                            int blockSequence = int.Parse((string)dsResponse.Tables[0].Rows[0]["Sequence"]);

                                                            dbParam[dbParam.Length - 2] = new System.Data.OleDb.OleDbParameter("@AccountBlockSequence", System.Data.OleDb.OleDbType.Integer);
                                                            dbParam[dbParam.Length - 2].Direction = ParameterDirection.Input;
                                                            dbParam[dbParam.Length - 2].Value = blockSequence;
                                                            dbParam[dbParam.Length - 1] = new System.Data.OleDb.OleDbParameter("@BlokingAmount", System.Data.OleDb.OleDbType.Decimal);
                                                            dbParam[dbParam.Length - 1].Direction = ParameterDirection.Input;
                                                            dbParam[dbParam.Length - 1].Value = BlokirAmount;
                                                            blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                                            if (!blnReturn)
                                                                cOut = cOut + "\nUnexpected error, gagal mengupdate database, namun blockir rekening telah dimasukkan ke core banking. Laporkan kasus ini pada service desk!";
                                                        }

                                                    }
                                                }
                                                else
                                                {
                                                    cOut = cOut + "\nLepas blokir gagal, Data tidak dapat di Execute.";
                                                    blnReturn = false;
                                                }
                                            }
                                            else
                                            {
                                                cOut = cOut + "\nLepas blokir gagal, Data tidak dapat di Execute.";
                                                blnReturn = false;
                                            }

                                        }
                                        else
                                        {
                                            // do nothing karena pasti harus relese acoount...
                                        }
                                    }
                                    else
                                    {
                                        cOut = cOut + "\nTidak Ada Perubahan Pada Data Order Nominal.";
                                        blnReturn = false;
                                    }
                                }
                                else
                                {
                                    cOut = cOut + "\nData nik yang melakukan perubahan/koreksi tidak ditemukan.";
                                    blnReturn = false;
                                }
                                break;
                            }

                        // 20160129, fauzil, TRBST16240, end

                        // 20160311, fauzil, TRBST16240, begin
                        case "trs_UpdateStatusSecurityTransaction_TT":
                            {
                                bool CanProcess = false;
                                string ErrorMessage = "";

                                clsDatabase.subTRSValidateCutOffTimeTransaction(cQuery, "2", dsGridMain.Tables[0].Rows[i]["SecurityNo"].ToString(), out CanProcess, out ErrorMessage);
                                if (!CanProcess)
                                {
                                    MessageBox.Show(ErrorMessage, "Warning!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                                else
                                {
                                    string ValueApprove = "";
                                    string errorMessage = "";
                                    ValueApprove = strArCmd[2].Split('|')[4]; // 0 = approve, 2 = Confirm, 1= verify
                                    decimal HargaModal = decimal.Parse(dsGridMain.Tables[0].Rows[i]["HargaModal"].ToString());
                                    decimal DealPrice = decimal.Parse(dsGridMain.Tables[0].Rows[i]["DealPrice"].ToString());
                                    bool IsAppWM = bool.Parse(dsGridMain.Tables[0].Rows[i]["isNeedWMApp"].ToString());
                                    //20180116, uzia, TRBST16240, begin
                                    bool IsCorporateBond = false;
                                    if (dsGridMain.Tables[0].Columns.Contains("IsCorporateBond"))
                                        bool.TryParse(dsGridMain.Tables[0].Rows[i]["IsCorporateBond"].ToString(), out IsCorporateBond);
                                    //20180828, samypasha, LOGEN00665, begin
                                    decimal ProfitCabang = 0;
                                    decimal FaceValue = 0;
                                    if (dsGridMain.Tables[0].Columns.Contains("FaceValue"))
                                    {
                                        FaceValue = decimal.Parse(dsGridMain.Tables[0].Rows[i]["FaceValue"].ToString());
                                    }
                                    //20180828, samypasha, LOGEN00665, end
                                    //20180116, uzia, TRBST16240, end
                                    //20230509, darul.wahid, BONDRETAIL-1310, begin
                                    bool IsBondOffshore = false;
                                    if (dsGridMain.Tables[0].Columns.Contains("IsBondOffshore"))
                                        bool.TryParse(dsGridMain.Tables[0].Rows[i]["IsBondOffshore"].ToString(), out IsBondOffshore);
                                    //20230509, darul.wahid, BONDRETAIL-1310, end
                                    if (ValueApprove == "1")
                                    {
                                        // validasi expired risk profile                                    
                                        tsb.CheckValidateRiskProfile(ObligasiQuery.cQuery, dsGridMain.Tables[0].Rows[i]["SecAccNo"].ToString(), DateTime.Parse(dsGridMain.Tables[0].Rows[i]["SettlementDate"].ToString()), out errorMessage);
                                        if (string.IsNullOrEmpty(errorMessage))
                                        {
                                            blnReturn = true;
                                            // berikan warning deviasi jika ada
                                            if (dsGridMain.Tables[0].Rows[i]["TreasuryAppMessage"].ToString().Length > 0)
                                            {
                                                if (MessageBox.Show(dsGridMain.Tables[0].Rows[i]["TreasuryAppMessage"].ToString(), "WARNING!!!", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                                    blnReturn = true;
                                                else
                                                    blnReturn = false;
                                            }

                                            //20180828, samypasha, LOGEN00665, begin
                                            if (TrxType == "4") // bank sell
                                            {
                                                //(Deal Price – Harga Modal) x Nominal /100
                                                ProfitCabang = (DealPrice - HargaModal) * FaceValue / 100;
                                                if (ProfitCabang < 0)
                                                {
                                                    MessageBox.Show("Transaksi ini tidak dapat di approve dikarenakan profit transaksi minus. ", "Warning!!", MessageBoxButtons.OK);
                                                    blnReturn = false;
                                                }
                                            }
                                            else if (TrxType == "3") // bank buy
                                            {
                                                //(Harga Modal - Deal Price) x Nominal /100
                                                ProfitCabang = (HargaModal - DealPrice) * FaceValue / 100;
                                                if (ProfitCabang < 0)
                                                {
                                                    MessageBox.Show("Transaksi ini tidak dapat di approve dikarenakan profit transaksi minus. ", "Warning!!", MessageBoxButtons.OK);
                                                    blnReturn = false;
                                                }
                                            }
                                            //20180828, samypasha, LOGEN00665, end
                                            //20201111, rezakahfi, BONDRETAIL-634, begin
                                            #region cek wewenang Deviasi
                                            //20201111, rezakahfi, BONDRETAIL-634, begin
                                            // cek wewenang
                                            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();

                                            bool isNeedWMApp = false;
                                            bool NeedTreasuryApp = false;
                                            string MessagePublishError = "";
                                            string MessageModalError = "";

                                            string strResultPVB = "";
                                            string cErrMsg = "";
                                            string CIFNo = dsGridMain.Tables[0].Rows[i]["CIFNo"].ToString().Trim();

                                            bool isPVB = false;

                                            int liSecId = Int32.Parse(dsGridMain.Tables[0].Rows[i]["SecId"].ToString());

                                            //decimal FaceValue = decimal.Parse(dsGridMain.Tables[0].Rows[i]["FaceValue"].ToString().Replace(",", "."));
                                            decimal HargaModalCurrent = 0;
                                            decimal HargaPublishCurrent = 0;

                                            //get current harga
                                            SuratBerharga.PopulateHarga(dsGridMain.Tables[0].Rows[i]["SecurityNo"].ToString(), TrxType, out HargaPublishCurrent, out HargaModalCurrent);

                                            //get flag PVB
                                            #region PVB
                                            if (clsCallWebService.CallCIFInquiryInquiryParameterCustomerFlagging13150(CIFNo, out strResultPVB, out cErrMsg))
                                            {
                                                System.IO.StringReader srResultTest = new System.IO.StringReader(strResultPVB);
                                                System.Data.DataSet dsResult = new System.Data.DataSet();
                                                dsResult.ReadXml(srResultTest);

                                                if (dsResult.Tables[0].Rows[0]["FlagType"].ToString() == "PV")
                                                {
                                                    isPVB = true;
                                                }
                                                else
                                                {
                                                    isPVB = false;
                                                }
                                            }
                                            else
                                            {
                                                isPVB = false;
                                            }
                                            #endregion

                                            bool chKaryawan = bool.Parse(dsGridMain.Tables[0].Rows[i]["FlagKaryawan"].ToString());

                                            //20240227, pratama, BONDRETAIL-1392, begin
                                            //if (SuratBerharga.checkWewenangDeviasi(
                                            //                                dsGridMain.Tables[0].Rows[i]["DealId"].ToString()
                                            //                                , liSecId, int.Parse(TrxType)
                                            //                                , dsGridMain.Tables[0].Rows[i]["SecCcy"].ToString()
                                            //                                , decimal.Parse(dsGridMain.Tables[0].Rows[i]["HargaModal"].ToString())
                                            //                                , HargaPublishCurrent
                                            //                                , DealPrice
                                            //                                , FaceValue
                                            //                                , chKaryawan
                                            //                                , out MessagePublishError, out MessageModalError, out NeedTreasuryApp, out isNeedWMApp))
                                            //{
                                            //    if (!string.IsNullOrEmpty(MessagePublishError))
                                            //        MessageBox.Show(MessagePublishError, "WARNING!!", MessageBoxButtons.OK);
                                            //    if (!string.IsNullOrEmpty(MessageModalError))
                                            //        MessageBox.Show(MessageModalError, "WARNING!!", MessageBoxButtons.OK);

                                            //    if (isPVB)
                                            //        isNeedWMApp = false;

                                                //if (isNeedWMApp)
                                                //{
                                                //    blnReturn = false;
                                                //SuratBerharga.UpdateStatusNeedApproval(dsGridMain.Tables[0].Rows[i]["DealId"].ToString(), isNeedWMApp, NeedTreasuryApp);
                                                //    MessageBox.Show("Transaksi ini tidak dapat di otorisasi oleh Supervisor cabang karena spread yang digunakan memerlukan approval dari Wealth Management", "WARNING!!", MessageBoxButtons.OK);
                                                //}
                                            //}
                                        //20240227, pratama, BONDRETAIL-1392, end
                                            #endregion
                                            //20201111, rezakahfi, BONDRETAIL-634, end
                                            if (blnReturn)
                                            {
                                                if (TrxType == "4") // bank sell
                                                {
                                                    // cek book building
                                                    if (dsGridMain.Tables[0].Rows[i]["BookBuilding"].ToString() == "N")  // tidak book building
                                                    {
                                                        blnReturn = true;
                                                    }
                                                    else if (dsGridMain.Tables[0].Rows[i]["Release"].ToString() == "Y") // book building tapi sudah direlease
                                                    {
                                                        blnReturn = true;
                                                    }
                                                    else // book building
                                                    {
                                                        MessageBox.Show("Book building tidak terpenuhi. Transaksi tidak dapat dijalankan", "Warning!!", MessageBoxButtons.OK);
                                                        blnReturn = false;
                                                    }
                                                    //20180116, uzia, TRBST16240, begin
                                                    //if (IsCorporateBond)
                                                    //20230509, darul.wahid, BONDRETAIL-1310, begin
                                                    if (IsBondOffshore)
                                                    {
                                                        if (MessageBox.Show("Apakah Operation Cabang sudah melakukan call back telepon konfirmasi ke Nasabah untuk memastikan Nasabah memahami fitur dan risiko atas obligasi Bond Offshore ini?", "Confirmation"
                                                            , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                                            blnReturn = true;
                                                        else
                                                            blnReturn = false;
                                                    }
                                                    else if (IsCorporateBond)
                                                    //20230509, darul.wahid, BONDRETAIL-1310, end
                                                    {
                                                        if (MessageBox.Show("Apakah Operation Cabang sudah melakukan call back telepon konfirmasi ke Nasabah untuk memastikan Nasabah memahami fitur dan risiko atas obligasi korporasi ini?", "Confirmation"
                                                            , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                                            blnReturn = true;
                                                        else
                                                            blnReturn = false;
                                                    }
                                                    //20180116, uzia, TRBST16240, end
                                                    if (blnReturn)
                                                    {
                                                        //if (DealPrice < HargaModal)
                                                        //{
                                                        //    dbParam[dbParam.Length - 1] = new System.Data.OleDb.OleDbParameter("@isWM", System.Data.OleDb.OleDbType.Boolean);
                                                        //    dbParam[dbParam.Length - 1].Direction = ParameterDirection.Input;
                                                        //    dbParam[dbParam.Length - 1].Value = false;
                                                        //    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                                        //}
                                                        //else if (IsAppWM)
                                                        //{
                                                        //    dbParam[dbParam.Length - 1] = new System.Data.OleDb.OleDbParameter("@isWM", System.Data.OleDb.OleDbType.Boolean);
                                                        //    dbParam[dbParam.Length - 1].Direction = ParameterDirection.Input;
                                                        //    dbParam[dbParam.Length - 1].Value = true;
                                                        //    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                                        //}
                                                        //else
                                                        //{
                                                        Guid GuidInternal = Guid.NewGuid();
                                                        Guid GuidExternal = Guid.NewGuid();

                                                        //SP untuk data gantung dengan status 19 bukan lagi 3
                                                        strCommand = "trs_UpdateStatusSecurityTransactionAfterMurex";
                                                        System.Data.OleDb.OleDbParameter[] dbParameter = new OleDbParameter[5];
                                                        dbParameter[0] = dbParam[0];
                                                        dbParameter[1] = dbParam[1];
                                                        dbParameter[1].Value = 19;
                                                        dbParameter[2] = dbParam[2];
                                                        dbParameter[3] = new OleDbParameter("@pcGuidInternal", OleDbType.Guid);
                                                        dbParameter[3].Value = GuidInternal;
                                                        dbParameter[4] = new OleDbParameter("@pcGuidExternal", OleDbType.Guid);
                                                        dbParameter[4].Value = GuidExternal;
                                                        blnReturn = cQuery.ExecProc(strCommand, ref dbParameter, out dsDummy);
                                                        if (blnReturn)
                                                        {
                                                            if (DataToXML(i, GuidInternal, GuidExternal))
                                                            {
                                                                // do nothing
                                                                //20210914, rezkahfi, BONDRETAIL-805, begin
                                                                string strXMLOut = "";
                                                                string strErrMsg = "";
                                                                if (dsGridMain.Tables[0].Columns.Contains("IsRMMobileTransaction"))
                                                                {
                                                                    if (bool.Parse(dsGridMain.Tables[0].Rows[i]["IsRMMobileTransaction"].ToString()))
                                                                    {
                                                                        wsProObligasi.clsService clsService = new BankNISP.Obligasi01.wsProObligasi.clsService();
                                                                        if (!clsService.CallBackRMMobile(dsGridMain.Tables[0].Rows[i]["DealId"].ToString()
                                                                            , true, out strXMLOut, out strErrMsg))
                                                                            MessageBox.Show("Callback to RMMobile Failed\nDealId : " + dsGridMain.Tables[0].Rows[i]["DealId"].ToString()
                                                                                                , "Warning!!", MessageBoxButtons.OK);
                                                                    }
                                                                }
                                                                //20210914, rezkahfi, BONDRETAIL-805, end
                                                            }
                                                        }
                                                        //}
                                                    }

                                                }
                                                else
                                                {
                                                    //if (DealPrice > HargaModal)
                                                    //{
                                                    //    dbParam[dbParam.Length - 1] = new System.Data.OleDb.OleDbParameter("@isWM", System.Data.OleDb.OleDbType.Boolean);
                                                    //    dbParam[dbParam.Length - 1].Direction = ParameterDirection.Input;
                                                    //    dbParam[dbParam.Length - 1].Value = false;
                                                    //    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                                    //}
                                                    //else if (IsAppWM)
                                                    //{
                                                    //    dbParam[dbParam.Length - 1] = new System.Data.OleDb.OleDbParameter("@isWM", System.Data.OleDb.OleDbType.Boolean);
                                                    //    dbParam[dbParam.Length - 1].Direction = ParameterDirection.Input;
                                                    //    dbParam[dbParam.Length - 1].Value = true;
                                                    //    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                                    //}
                                                    //else
                                                    //{
                                                    Guid GuidInternal = Guid.NewGuid();
                                                    Guid GuidExternal = Guid.NewGuid();

                                                    //SP untuk data gantung dengan status 19 bukan lagi 3
                                                    strCommand = "trs_UpdateStatusSecurityTransactionAfterMurex";
                                                    System.Data.OleDb.OleDbParameter[] dbParameter = new OleDbParameter[5];
                                                    dbParameter[0] = dbParam[0];
                                                    dbParameter[1] = dbParam[1];
                                                    dbParameter[1].Value = 19;
                                                    dbParameter[2] = dbParam[2];
                                                    dbParameter[3] = new OleDbParameter("@pcGuidInternal", OleDbType.Guid);
                                                    dbParameter[3].Value = GuidInternal;
                                                    dbParameter[4] = new OleDbParameter("@pcGuidExternal", OleDbType.Guid);
                                                    dbParameter[4].Value = GuidExternal;
                                                    blnReturn = cQuery.ExecProc(strCommand, ref dbParameter, out dsDummy);
                                                    if (blnReturn)
                                                    {
                                                        if (DataToXML(i, GuidInternal, GuidExternal))
                                                        {
                                                            // do nothing
                                                            //20210914, rezkahfi, BONDRETAIL-805, begin
                                                            string strXMLOut = "";
                                                            string strErrMsg = "";
                                                            if (dsGridMain.Tables[0].Columns.Contains("IsRMMobileTransaction"))
                                                            {
                                                                if (bool.Parse(dsGridMain.Tables[0].Rows[i]["IsRMMobileTransaction"].ToString()))
                                                                {
                                                                    wsProObligasi.clsService clsService = new BankNISP.Obligasi01.wsProObligasi.clsService();
                                                                    if (!clsService.CallBackRMMobile(dsGridMain.Tables[0].Rows[i]["DealId"].ToString()
                                                                        , true, out strXMLOut, out strErrMsg))
                                                                        MessageBox.Show("Callback to RMMobile Failed\nDealId : " + dsGridMain.Tables[0].Rows[i]["DealId"].ToString()
                                                                                            , "Warning!!", MessageBoxButtons.OK);
                                                                }
                                                            }
                                                            //20210914, rezkahfi, BONDRETAIL-805, end
                                                        }
                                                    }
                                                    //}
                                                }
                                            }
                                        }
                                        else
                                        {
                                            cOut = cOut + "Risk profile nasabah sudah expired. Lakukan update risk profile nasabah di Pro CIF";
                                            blnReturn = false;
                                        }
                                    }
                                    //else if (ValueApprove == "2")
                                    //{
                                    //    Guid GuidInternal = Guid.NewGuid();
                                    //    Guid GuidExternal = Guid.NewGuid();
                                    //    //SP untuk data gantung dengan status 9 bukan lagi 3
                                    //    strCommand = "trs_UpdateStatusSecurityTransactionAfterMurex";
                                    //    System.Data.OleDb.OleDbParameter[] dbParameter = new OleDbParameter[5];
                                    //    dbParameter[0] = dbParam[0];
                                    //    dbParameter[1] = dbParam[1];
                                    //    dbParameter[1].Value = 9;
                                    //    dbParameter[2] = dbParam[2];
                                    //    dbParameter[3] = new OleDbParameter("@pcGuidInternal", OleDbType.Guid);
                                    //    dbParameter[3].Value = GuidInternal;
                                    //    dbParameter[4] = new OleDbParameter("@pcGuidExternal", OleDbType.Guid);
                                    //    dbParameter[4].Value = GuidExternal;
                                    //    blnReturn = cQuery.ExecProc(strCommand, ref dbParameter, out dsDummy);
                                    //    if (blnReturn)
                                    //    {
                                    //        if (DataToXML(i, GuidInternal, GuidExternal))
                                    //        {
                                    //            // do nothing
                                    //        }
                                    //    }      
                                    //}
                                    else if (ValueApprove == "3")
                                    {
                                        //Guid GuidInternal = Guid.NewGuid();
                                        //Guid GuidExternal = Guid.NewGuid();
                                        //if (DataToXML(i, GuidInternal, GuidExternal))
                                        //{
                                        //    //SP untuk data gantung dengan status 20 bukan lagi 3
                                        //    strCommand = "trs_UpdateStatusSecurityTransactionAfterMurex";
                                        //    System.Data.OleDb.OleDbParameter[] dbParameter = new OleDbParameter[5];
                                        //    dbParameter[0] = dbParam[0];
                                        //    dbParameter[1] = dbParam[1];
                                        //    dbParameter[1].Value = 20;
                                        //    dbParameter[2] = dbParam[2];
                                        //    dbParameter[3] = new OleDbParameter("@pcGuidInternal", OleDbType.Guid);
                                        //    dbParameter[3].Value = GuidInternal;
                                        //    dbParameter[4] = new OleDbParameter("@pcGuidExternal", OleDbType.Guid);
                                        //    dbParameter[4].Value = GuidExternal;
                                        //    blnReturn = cQuery.ExecProc(strCommand, ref dbParameter, out dsDummy);
                                        //}
                                        //20180116, uzia, TRBST16240, begin
                                        //dbParam[dbParam.Length - 1] = new System.Data.OleDb.OleDbParameter("@isWM", System.Data.OleDb.OleDbType.Boolean);
                                        //dbParam[dbParam.Length - 1].Direction = ParameterDirection.Input;
                                        //dbParam[dbParam.Length - 1].Value = true;
                                        //20180116, uzia, TRBST16240, end
                                        blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                    }
                                }
                                break;
                            }
                        case "trs_updateStatusTreasuryCustomer_TM":
                            {
                                string ValueApprove = "";
                                ValueApprove = strArCmd[2].Split('|')[4]; // -1 = approval BS ; 3 = Approval TROOPS


                                if (ValueApprove != "3")
                                {
                                    string CIF = dsGridMain.Tables[0].Rows[i]["CIFNo"].ToString().Trim();
                                    string Nama = dsGridMain.Tables[0].Rows[i]["Nama"].ToString().Trim();
                                    string SecAccNo = dsGridMain.Tables[0].Rows[i]["SecAccNo"].ToString().Trim();
                                    MessageBox.Show("Master Nasabah baru ini memerlukan otorisasi dari Treasury Operation agar dapat digunakan. Segera hubungi Treasury Operation", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    // sending email
                                    SentEmail(CIF, Nama, SecAccNo);
                                    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                    if (blnReturn)
                                    {
                                        DataToXML(SecAccNo);
                                    }
                                }
                                else
                                {
                                    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                }

                                break;
                            }
                        // 20160311, fauzil, TRBST16240, end    
                        default:
                            {
                                blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);
                                break;
                            }
                    }
                    //20120802, hermanto_salim, BAALN12003, end                      

                    if (!blnReturn)
                    {
                        MessageBox.Show(cOut + "\nGagal!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(cOut + "\nBerhasil!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
            }
            //20120802, hermanto_salim, BAALN12003, begin
            strCommand = strPopulate.Split('&')[0];
            if (strCommand == "trs_CancelOriOrder_TT")
            {
                for (int i = 0; i < securityIDs.Count; i++)
                {
                    clsDatabase.subTRSUpdateOrderPriority(cQuery, new object[] { securityIDs[i] });
                }
            }
            //20120802, hermanto_salim, BAALN12003, end  
            // 20160201, fauzil, TRBST16240, begin
            strCommand = strPopulate.Split('&')[0];
            if (strCommand == "trs_updateStatusORI_Order_Temp")
            {
                for (int i = 0; i < securityIDs.Count; i++)
                {
                    clsDatabase.subTRSUpdateOrderPriority(cQuery, new object[] { securityIDs[i] });
                }
            }
            // 20160201, fauzil, TRBST16240, end
            subPopulateGridMain();
        }

        private void subDeleteTransaction()
        {
            Boolean blnReturn = false;
            string strPopulate = dtSelectedTree.Rows[0]["button3_query"].ToString();
            System.Data.OleDb.OleDbParameter[] dbParam;
            System.Data.DataSet dsDummy;

            string strCommand;

            dgvMain.EndEdit();

            for (int i = 0; i < dsGridMain.Tables[0].Rows.Count; i++)
            {
                // if (System.Convert.ToBoolean(dsGridMain.Tables[0].Rows[i]["checked"].ToString()))
                //{
                if (dsGridMain.Tables[0].Rows[i]["checked"].ToString() == "True")
                {
                    strCommand = fnCreateCommand1(strPopulate, out dbParam, i);

                    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);

                    if (!blnReturn)
                    {
                        MessageBox.Show("PROSES GAGAL", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Proses Berhasil", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
            }
            subPopulateGridMain();
        }

        private void subCancelOrder()
        {
            Boolean blnReturn = false;
            string strPopulate = dtSelectedTree.Rows[0]["button3_query"].ToString();
            System.Data.OleDb.OleDbParameter[] dbParam;
            System.Data.DataSet dsDummy;

            string strCommand;

            dgvMain.EndEdit();

            for (int i = 0; i < dsGridMain.Tables[0].Rows.Count; i++)
            {
                // if (System.Convert.ToBoolean(dsGridMain.Tables[0].Rows[i]["checked"].ToString()))
                //{
                if (dsGridMain.Tables[0].Rows[i]["checked"].ToString() == "True")
                {
                    string cOut = "";
                    strCommand = fnCreateCommand1(strPopulate, out dbParam, i);


                    strCommand = fnCreateCommand1(strPopulate, out dbParam, i);
                    for (int j = 0; j < dsGridMain.Tables[0].Columns.Count; j++)
                    {
                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "OrderId")
                        {
                            cOut = "Cancel Order Id :" + dsGridMain.Tables[0].Rows[i]["OrderId"].ToString();
                            break;
                        }
                        else
                        {
                            cOut = "Cancel No CIF : " + dsGridMain.Tables[0].Rows[i]["CIFNo"].ToString();
                        }

                    }
                    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);

                    if (!blnReturn)
                    {
                        MessageBox.Show(cOut + "\nGagal!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(cOut + "\nBerhasil!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
            }
            subPopulateGridMain();
        }

        private void subRejectTreasuryCustomer()
        {
            Boolean blnReturn = false;
            string strPopulate = dtSelectedTree.Rows[0]["button3_query"].ToString();
            System.Data.OleDb.OleDbParameter[] dbParam;
            System.Data.DataSet dsDummy;

            string strCommand;

            dgvMain.EndEdit();

            for (int i = 0; i < dsGridMain.Tables[0].Rows.Count; i++)
            {
                // if (System.Convert.ToBoolean(dsGridMain.Tables[0].Rows[i]["checked"].ToString()))
                //{
                if (dsGridMain.Tables[0].Rows[i]["checked"].ToString() == "True")
                {
                    string cOut = "";
                    strCommand = fnCreateCommand1(strPopulate, out dbParam, i);
                    for (int j = 0; j < dsGridMain.Tables[0].Columns.Count; j++)
                    {
                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "DealNo")
                        {
                            cOut = "Deal No : " + dsGridMain.Tables[0].Rows[i]["DealNo"].ToString();
                        }

                    }
                    blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsDummy);

                    if (!blnReturn)
                    {
                        MessageBox.Show("PROSES GAGAL" + cOut, "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Proses Berhasil" + cOut, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
            }
            subPopulateGridMain();
        }

        private void frmObligasiUpdateStatusTRX_Load(object sender, EventArgs e)
        {
            //InitializeForm("Awal");
            //20120802, hermanto_salim, BAALN12003, begin  
            clsCallWebService = new clsCallWebService();
            //20120802, hermanto_salim, BAALN12003, end
            //20160714, fauzil, TRBST16240, begin
            clsCallWebService.clsWebServiceLoad(intNIK.ToString(), Guid.NewGuid().ToString(), strModule, cQuery);
            //20160714, fauzil, TRBST16240, end
            subInitForm();

            DataSet dsTreeview;

            OleDbParameter[] dbParam = new OleDbParameter[3];

            (dbParam[0] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
            (dbParam[1] = new OleDbParameter("@pcModule", OleDbType.VarChar, 30)).Value = strModule;
            (dbParam[2] = new OleDbParameter("@pcMenuName", OleDbType.VarChar, 50)).Value = strMenuName;

            bool blnResult = cQuery.ExecProc("UserGetTreeView", ref dbParam, out dsTreeview);
            if (blnResult == true)
            {
                _dvAkses = new DataView(dsTreeview.Tables[1]);
                subResetToolBar();
            }
            else
            {
                MessageBox.Show("Error Get Treeview Nodes");
            }

        }


        private void subInitForm()
        {
            Boolean blnReturn = false;

            System.Data.OleDb.OleDbParameter[] Params = new System.Data.OleDb.OleDbParameter[3];

            Params[0] = new System.Data.OleDb.OleDbParameter("@cMenu", System.Data.OleDb.OleDbType.VarChar, 40);
            Params[0].Direction = ParameterDirection.Input;
            Params[0].Value = strLocalMenu;

            Params[1] = new System.Data.OleDb.OleDbParameter("@nNik", System.Data.OleDb.OleDbType.Integer);
            Params[1].Direction = ParameterDirection.Input;
            Params[1].Value = intNIK;

            (Params[2] = new OleDbParameter("@pcModule", OleDbType.VarChar, 25)).Value = strModule;

            blnReturn = cQuery.ExecProc("common2_populate_tree", ref Params, out dsTree);
            if (!blnReturn)
            {
                return;
            }

            dtSelectedTree = dsTree.Tables[0].Clone();

            dtGridDetail.Columns.Add("Item", System.Type.GetType("System.String"));
            dtGridDetail.Columns.Add("Value", System.Type.GetType("System.String"));


            TreeNode rootNode;
            Int32 intI;

            rootNode = new TreeNode(dsTree.Tables[0].Rows[0]["tree_name"].ToString());
            rootNode.Name = dsTree.Tables[0].Rows[0]["tree_id"].ToString();

            TreeNode ParentNode = rootNode;

            for (intI = 0; intI < dsTree.Tables[0].Rows.Count; intI++)
            {
                if (dsTree.Tables[0].Rows[intI]["parent_tree"].ToString() != "")
                {
                    try
                    {
                        ParentNode = rootNode.Nodes.Find(dsTree.Tables[0].Rows[intI]["parent_tree"].ToString(), true)[0];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        ParentNode = rootNode;
                    }
                    finally
                    {
                        ParentNode.Nodes.Add(dsTree.Tables[0].Rows[intI]["tree_id"].ToString(), dsTree.Tables[0].Rows[intI]["tree_name"].ToString());
                    }
                }
            }

            trvOtorisasi.Nodes.Add(rootNode);
        }

        /// <summary>
        /// Node Click
        /// </summary>
        ///
        private void trvOtorisasi_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //08 april
            _strNodeKey = e.Node.Name.ToString();
            Console.Write(_strNodeKey);

            Int32 intRow = 0;

            for (intRow = 0; intRow < dsTree.Tables[0].Rows.Count; intRow++)
            {
                if (dsTree.Tables[0].Rows[intRow]["tree_id"].ToString() == _strNodeKey)
                {
                    if (System.Convert.ToBoolean(dsTree.Tables[0].Rows[intRow]["end_level_bit"]))
                    {
                        dtSelectedTree.Rows.Clear();
                        dtSelectedTree.ImportRow(dsTree.Tables[0].Rows[intRow]);

                        DataSet dsTreeviewNode;

                        OleDbParameter[] dbParamNode = new OleDbParameter[4];
                        Console.WriteLine(intNIK.ToString() + "<br/>");
                        Console.WriteLine(_strNodeKey.ToString() + "<br/>");
                        (dbParamNode[0] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                        (dbParamNode[1] = new OleDbParameter("@pcModule", OleDbType.VarChar, 30)).Value = strModule;
                        (dbParamNode[2] = new OleDbParameter("@pcMenuName", OleDbType.VarChar, 50)).Value = strMenuName;
                        (dbParamNode[3] = new OleDbParameter("@pcInterfaceIdName", OleDbType.VarChar, 50)).Value = _strNodeKey;
                        if (_strNodeKey != "")
                        {
                            bool blnResult = cQuery.ExecProc("UserGetToolbarTree", ref dbParamNode, out dsTreeviewNode);
                            if (blnResult == true)
                            {
                                if (dsTreeviewNode.Tables[0].Rows.Count > 0)
                                {
                                    _dvAksesNode = new DataView(dsTreeviewNode.Tables[0]);
                                    Cursor.Current = Cursors.WaitCursor;
                                    subResetToolBarTreeNode();
                                    Cursor.Current = Cursors.Default;
                                }
                                //20170704, agireza, TRBST16240, begin
                                else
                                {
                                    string[] strVisibleToolbars = null;
                                    if (!Object.ReferenceEquals(_dvAksesNode, null))
                                    {
                                        strVisibleToolbars = new string[_dvAksesNode.Count];
                                        for (int i = 0; i < _dvAksesNode.Count; i++)
                                        {
                                            if (_dvAksesNode[i]["IconId"].ToString() != "1")
                                                this.NISPToolbarButton(_dvAksesNode[i]["IconId"].ToString()).Visible = false;
                                        }
                                    }
                                    else
                                    {
                                        strVisibleToolbars = new string[_dvAkses.Count];
                                        for (int i = 0; i < _dvAkses.Count; i++)
                                        {
                                            if (_dvAkses[i]["IconId"].ToString() != "1")
                                                this.NISPToolbarButton(_dvAkses[i]["IconId"].ToString()).Visible = false;
                                        }
                                    }
                                }
                                //20170704, agireza, TRBST16240, end
                            }
                            else
                            {
                                MessageBox.Show("Error Get Treeview Nodes");
                            }

                        }

                        subPopulateGridMain();
                        break;
                    }
                    else
                    {
                        clearGridMainDetal(true);
                        this.NISPToolbarButton("0").Visible = false;
                        this.NISPToolbarButton("2").Visible = false;
                        this.NISPToolbarButton("5").Visible = false;
                        this.NISPToolbarButton("7").Visible = false;
                        this.NISPToolbarButton("42").Visible = false;
                        this.NISPToolbarButton("43").Visible = false;
                    }
                }
            }



        }


        private void subPopulateGridMain()
        {
            Boolean blnReturn = false;
            string strPopulate = dtSelectedTree.Rows[0]["populate_query"].ToString();
            System.Data.OleDb.OleDbParameter[] dbParam;

            string strCommand = fnCreateCommand1(strPopulate, out dbParam, 0);

            // 20160121, fauzil, TRBST16240, begin        
            string[] strArCmd = strPopulate.Split('&');
            string namaSP = strArCmd[0];
            SpText = namaSP;
            if (namaSP.Equals("trs_StatusSecurityTransaction_TT", StringComparison.OrdinalIgnoreCase))
            {
                TrxType = strArCmd[1].Split('|')[4];    // 3 = Buy, 4 = Sell
                ViewType = strArCmd[2].Split('|')[4];   // 0 = BS, 1 = Treasur, 2 = Troops
            }
            // 20160121, fauzil, TRBST16240, end

            /*Development*/
            blnReturn = cQuery.ExecProc(strCommand, ref dbParam, out dsGridMain);
            if (blnReturn)
            {
                if (dsGridMain != null)
                {
                    if (dsGridMain.Tables.Count > 0)
                    {
                        if (dsGridMain.Tables[0].Rows.Count > 0)
                        {
                            clearGridMainDetal(true);
                            dgvMain.DataSource = dsGridMain.Tables[0];
                            dgvMain.Columns[0].Frozen = true;
                            dgvMain.Columns[0].HeaderText = "";
                            dgvMain.Columns[0].Width = 40;

                            for (int j = 0; j < dsGridMain.Tables[0].Columns.Count; j++)
                            {
                                // 20160121, fauzil, TRBST16240, begin
                                // hilang beberapa kolom untuk authorization pemesanan ORI
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "BranchCode")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "AccountBlockSequence")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "InsertedBy")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "GrandTotalKuota")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SisaKuotaConfirmed")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SisaKuotaAll")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "KuotaBuyBack")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SisaKuotaBuyBackConfrimed")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SisaKuotaBuyBackAll")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "KuotaCashBack")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SisaKuotaCashBackCofrimed")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SisaKuotaCashBackAll")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "Kuota Transaksi Beli")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "Sisa Kuota Transaksi Beli Confirmed")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "Sisa Kuota Transaksi Beli All")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SisaKuotaCashBackConfirmed")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                // 20160121, fauzil, TRBST16240, end
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "CIFId")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "CIFNo")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "TrxType")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "OrderStatus")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "DealId")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }

                                //FOP & Transfer Asset
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "SecId")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "DealId")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "CIFIdDebet")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "CIFIdKredit")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "TrxType")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "TrxDetailType")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                // 20160419, fauzil, TRBST16240, begin 
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "FOPUniq")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                // 20160419, fauzil, TRBST16240, End

                                //Blokir Transaksi Nasabah
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "CollateralId")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "CollateralStatus")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }
                                if (dsGridMain.Tables[0].Columns[j].ColumnName == "DealId")
                                {
                                    dgvMain.Columns[j].Visible = false;
                                }

                                // 20160121, fauzil, TRBST15176, begin
                                //Transaksi Jual Beli
                                if (namaSP.Equals("trs_StatusSecurityTransaction_TT", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "DealId")
                                    {
                                        dgvMain.Columns[j].Visible = true;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "BranchCode")
                                    {
                                        dgvMain.Columns[j].Visible = true;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "TrxType")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "SecDescr")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "DealNo")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "Tenor")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "AccountBlockSequence")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "TreasuryAppMessage")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "NIKSourceTrader")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "NIKTraderRM")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "NikDestTraderRM")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "BranchProfit")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "isBookBld")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "isNeedWMApp")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "ISINCode")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }

                                    if (TrxType == "3") // 3 = Buy, 4 = Sell
                                    {
                                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "BookBuilding")
                                        {
                                            dgvMain.Columns[j].Visible = false;
                                        }
                                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "Release")
                                        {
                                            dgvMain.Columns[j].Visible = false;
                                        }
                                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "Switching")
                                        {
                                            dgvMain.Columns[j].Visible = true;
                                        }
                                    }
                                    else
                                    {
                                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "BoolBuilding")
                                        {
                                            dgvMain.Columns[j].Visible = true;
                                        }
                                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "Release")
                                        {
                                            dgvMain.Columns[j].Visible = true;
                                        }
                                        if (dsGridMain.Tables[0].Columns[j].ColumnName == "Switching")
                                        {
                                            dgvMain.Columns[j].Visible = false;
                                        }
                                    }
                                    //20250819, uzia, BONDRETAIL-xxx, begin
                                    //if (ViewType != "1")  // 0 = BS, 1 = Treasury, 2 = Troops
                                    //{
                                    //    if (dsGridMain.Tables[0].Columns[j].ColumnName == "HargaModal")
                                    //    {
                                    //        dgvMain.Columns[j].Visible = false;
                                    //    }
                                    //    if (dsGridMain.Tables[0].Columns[j].ColumnName == "HargaModalAkhir")
                                    //    {
                                    //        dgvMain.Columns[j].Visible = false;
                                    //    }
                                    //}
                                    //else if (ViewType == "1")
                                    //{
                                    //    if (dsGridMain.Tables[0].Columns[j].ColumnName == "HargaModal")
                                    //    {
                                    //        dgvMain.Columns[j].Visible = true;
                                    //    }
                                    //    if (dsGridMain.Tables[0].Columns[j].ColumnName == "HargaModalAkhir")
                                    //    {
                                    //        dgvMain.Columns[j].Visible = true;
                                    //    }
                                    //}
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "HargaModal")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    if (dsGridMain.Tables[0].Columns[j].ColumnName == "HargaModalAkhir")
                                    {
                                        dgvMain.Columns[j].Visible = false;
                                    }
                                    //20250819, uzia, BONDRETAIL-xxx, end

                                }

                                // 20160121, fauzil, TRBST15176, end

                            }

                            for (int i = 0; i < dsGridMain.Tables[0].Rows.Count; i++)
                            {
                                if ((_strNodeKey == "TRS3" || _strNodeKey == "TRS4") && dsGridMain.Tables[0].Rows[i]["NoRekInvestor"].ToString().Trim() == "504800000011")
                                {
                                    dgvMain.Rows[i].DefaultCellStyle.ForeColor = Color.Red;
                                    dgvMain.Rows[i].DefaultCellStyle.SelectionBackColor = Color.Red;
                                }
                                else
                                {
                                    dgvMain.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                                    dgvMain.Rows[i].DefaultCellStyle.SelectionBackColor = Color.Empty;
                                }
                            }

                            for (int i = 2; i < dgvMain.ColumnCount; i++)
                            {
                                dgvMain.Columns[i].ReadOnly = true;
                            }
                        }
                        else
                        {
                            clearGridMainDetal(true);
                        }
                    }
                    else
                    {
                        clearGridMainDetal(true);
                    }
                }
                else
                {
                    clearGridMainDetal(true);
                }
            }
        }

        private string fnCreateCommand1(string strCmd, out System.Data.OleDb.OleDbParameter[] dbpar1, Int32 intDSMainRow)
        {
            string[] strArCmd = strCmd.Split('&');
            string[] strParams;
            // 20160128, fauzil, TRBST16240, Begin
            //dbpar1 = new System.Data.OleDb.OleDbParameter[strArCmd.Length - 1];
            if (strArCmd[0].Equals("trs_updateStatusAccORI_Order_TT", StringComparison.OrdinalIgnoreCase))
                dbpar1 = new System.Data.OleDb.OleDbParameter[strArCmd.Length];
            else if (strArCmd[0].Equals("trs_updateStatusORI_Order_Temp", StringComparison.OrdinalIgnoreCase))
                dbpar1 = new System.Data.OleDb.OleDbParameter[strArCmd.Length + 1];
            else if (strArCmd[0].Equals("trs_UpdateStatusSecurityTransaction_TT", StringComparison.OrdinalIgnoreCase))
                dbpar1 = new System.Data.OleDb.OleDbParameter[strArCmd.Length - 1];
            else
                dbpar1 = new System.Data.OleDb.OleDbParameter[strArCmd.Length - 1];
            // 20160128, fauzil, TRBST16240, end
            Int32 intCounter = 0;

            for (intCounter = 1; intCounter < strArCmd.Length; intCounter++)
            {
                strParams = strArCmd[intCounter].Split('|');

                if (strParams[0] == "I")
                    dbpar1[intCounter - 1] = new System.Data.OleDb.OleDbParameter(strParams[3], System.Data.OleDb.OleDbType.Integer);
                else if (strParams[0] == "V")
                    dbpar1[intCounter - 1] = new System.Data.OleDb.OleDbParameter(strParams[3], System.Data.OleDb.OleDbType.VarChar, System.Convert.ToInt32(strParams[1]));
                else if (strParams[0] == "M")
                    dbpar1[intCounter - 1] = new System.Data.OleDb.OleDbParameter(strParams[3], System.Data.OleDb.OleDbType.Currency);
                else if (strParams[0] == "B")
                    dbpar1[intCounter - 1] = new System.Data.OleDb.OleDbParameter(strParams[3], System.Data.OleDb.OleDbType.TinyInt);

                if (strParams[2] == "I")
                    dbpar1[intCounter - 1].Direction = ParameterDirection.Input;
                else if (strParams[2] == "O")
                    dbpar1[intCounter - 1].Direction = ParameterDirection.Output;
                else if (strParams[2] == "IO")
                    dbpar1[intCounter - 1].Direction = ParameterDirection.InputOutput;

                if ((strParams[3] == "@cNik") || (strParams[3] == "@pnNIK"))
                {
                    dbpar1[intCounter - 1].Value = intNIK;
                }
                else if (strParams[3] == "@pcGuid")
                {
                    dbpar1[intCounter - 1].Value = strGuid;
                }
                else if (strParams[3] == "@pnXMLInput")
                {
                    // 20160128, fauzil, TRBST16240, Begin
                    switch (strArCmd[0])
                    {
                        case "trs_updateStatusAccORI_Order_TT":
                            {
                                DataSet dts1 = new DataSet();

                                object[] obj;
                                string priority = dsGridMain.Tables[0].Rows[intDSMainRow]["Priority"].ToString();
                                string bitPriority = (priority.Equals("Masuk Kuota", StringComparison.OrdinalIgnoreCase) ? "0" : "1");
                                dsGridMain.Tables[0].Rows[intDSMainRow]["Priority"] = bitPriority;

                                DateTime? ExpiredRiskProfile = null;
                                if (dsGridMain.Tables[0].Rows[intDSMainRow]["Expire_Risk_Profile"].GetType() != typeof(DBNull))
                                    ExpiredRiskProfile = DateTime.Parse(dsGridMain.Tables[0].Rows[intDSMainRow]["Expire_Risk_Profile"].ToString());

                                dbpar1[strArCmd.Length - 1] = new System.Data.OleDb.OleDbParameter("@dExpire_Risk_Profile", System.Data.OleDb.OleDbType.Date);
                                dbpar1[strArCmd.Length - 1].Direction = ParameterDirection.Input;
                                dbpar1[strArCmd.Length - 1].Value = ExpiredRiskProfile;



                                dsGridMain.Tables[0].AcceptChanges();
                                obj = dsGridMain.Tables[0].Rows[intDSMainRow].ItemArray;

                                dts1 = dsGridMain.Clone();
                                dts1.Tables[0].Rows.Add(obj);

                                dbpar1[intCounter - 1].Value = dts1.GetXml().ToString().Replace("NewDataSet", "Root").Replace("Table", "RS");

                                dts1.Dispose();
                                dts1 = null;

                                break;
                            }
                        case "trs_CancelOriOrder_TT":
                            {
                                DataSet dts1 = new DataSet();

                                object[] obj;
                                string priority = dsGridMain.Tables[0].Rows[intDSMainRow]["Priority"].ToString();
                                string bitPriority = (priority.Equals("Masuk Kuota", StringComparison.OrdinalIgnoreCase) ? "0" : "1");
                                dsGridMain.Tables[0].Rows[intDSMainRow]["Priority"] = bitPriority;


                                dsGridMain.Tables[0].AcceptChanges();
                                obj = dsGridMain.Tables[0].Rows[intDSMainRow].ItemArray;

                                dts1 = dsGridMain.Clone();
                                dts1.Tables[0].Rows.Add(obj);

                                dbpar1[intCounter - 1].Value = dts1.GetXml().ToString().Replace("NewDataSet", "Root").Replace("Table", "RS");

                                dts1.Dispose();
                                dts1 = null;

                                break;
                            }
                        default:
                            {
                                DataSet dts1 = new DataSet();

                                object[] obj;
                                obj = dsGridMain.Tables[0].Rows[intDSMainRow].ItemArray;

                                dts1 = dsGridMain.Clone();
                                dts1.Tables[0].Rows.Add(obj);

                                dbpar1[intCounter - 1].Value = dts1.GetXml().ToString().Replace("NewDataSet", "Root").Replace("Table", "RS");

                                dts1.Dispose();
                                dts1 = null;
                                break;
                            }
                    }
                    //DataSet dts1 = new DataSet();

                    //object[] obj;
                    //obj = dsGridMain.Tables[0].Rows[intDSMainRow].ItemArray;

                    //dts1 = dsGridMain.Clone();
                    //dts1.Tables[0].Rows.Add(obj);

                    //dbpar1[intCounter - 1].Value = dts1.GetXml().ToString().Replace("NewDataSet", "Root").Replace("Table", "RS");

                    //dts1.Dispose();
                    //dts1 = null;

                    // 20160128, fauzil, TRBST16240, End

                    //dbpar1[intCounter - 1].Value = GetXML(dgvMain);
                }
                else if (strParams[4].Substring(0, 1) == "!")
                {
                    string strColName = strParams[4].Replace("!", "");
                    dbpar1[intCounter - 1].Value = dsGridMain.Tables[0].Rows[intDSMainRow][strColName].ToString();
                }
                else if (strParams[4].Substring(0, 1) != "")
                    dbpar1[intCounter - 1].Value = strParams[4];

            }

            strCmd = strArCmd[0];
            return strCmd;

        }

        //20090729, David, BONDRTL004, begin
        //private void subPopulateDetailGrid()
        private void subPopulateDetailGrid(int ColumnIndex, int RowIndex)
        //20090729, David, BONDRTL004, end
        {
            bool isGNCTROPS = false;
            Int32 intCount = 0;
            //20090729, David, BONDRTL004, begin
            //Int32 indexdgv;
            Int32 indexdgv = -1;
            dgvMain.EndEdit();
            if (RowIndex >= 0)
            {
                //20090729, David, BONDRTL004, end
                if (dgvMain.CurrentRow != null)
                {
                    //20090729, David, BONDRTL004, begin
                    //indexdgv = dgvMain.CurrentRow.Index;
                    indexdgv = RowIndex;
                    //20090729, David, BONDRTL004, end
                }
                else
                {
                    indexdgv = 0;
                }

                String[] objVal = new string[2];
                dtGridDetail.Rows.Clear();
                //20161019, fauzil, TRBST16240, begin
                if (_strNodeKey == "TRS11" || _strNodeKey == "TRS12" || _strNodeKey == "TRS13" || _strNodeKey == "TRS15" || _strNodeKey == "TRS16" || _strNodeKey == "TRS17")
                {
                    for (intCount = 3; intCount < System.Convert.ToInt32(dsGridMain.Tables[0].Columns.Count); intCount++)
                    {
                        string tmp = dgvMain.Columns[intCount].HeaderText.ToString();
                        if ((tmp != "SecDesc") && (tmp != "DealNo") && (tmp != "Tenor") && (tmp != "AccountBlockSequence") && (tmp != "TreasuryAppMessage") && (tmp != "NIKSourceTrader") && (tmp != "NIKTraderRM")
                            && (tmp != "NikDestTraderRM") && (tmp != "BranchProfit") && (tmp != "isBookBld") && (tmp != "isNeedWMApp") && (tmp != "SecDescr") && (tmp != "TrxType")
                            //20250819, uzia, BONDRETAIL-xxx, begin
                            && (tmp != "HargaModal") && (tmp != "HargaModalAkhir")
                            //20250819, uzia, BONDRETAIL-xxx, end
                            )
                        {
                            objVal[0] = dgvMain.Columns[intCount].HeaderText.ToString();
                            objVal[1] = dgvMain[intCount, indexdgv].Value.ToString();
                            dtGridDetail.Rows.Add(objVal);
                        }
                    }
                }
                else
                {
                    //20161019, fauzil, TRBST16240, end               
                    for (intCount = 5; intCount < System.Convert.ToInt32(dsGridMain.Tables[0].Columns.Count); intCount++)
                    {
                        //20160614, fauzil, TRBST15176, begin
                        if (_strNodeKey == "TRS7" || _strNodeKey == "TRS8")
                        {
                            string tmp = dgvMain.Columns[intCount].HeaderText.ToString();
                            if ((tmp != "BranchCode") && (tmp != "AccountBlockSequence") && (tmp != "InsertedBy") && (tmp != "GrandTotalKuota") && (tmp != "SisaKuotaConfirmed") && (tmp != "SisaKuotaAll") && (tmp != "KuotaBuyBack")
                                && (tmp != "SisaKuotaBuyBackConfrimed") && (tmp != "SisaKuotaBuyBackAll") && (tmp != "KuotaCashBack") && (tmp != "SisaKuotaCashBackCofrimed") && (tmp != "SisaKuotaCashBackAll")
                                && (tmp != "Kuota Transaksi Beli") && (tmp != "Sisa Kuota Transaksi Beli Confirmed") && (tmp != "Sisa Kuota Transaksi Beli All") && (tmp != "SisaKuotaCashBackConfirmed"))
                            {
                                objVal[0] = dgvMain.Columns[intCount].HeaderText.ToString();
                                objVal[1] = dgvMain[intCount, indexdgv].Value.ToString();
                                dtGridDetail.Rows.Add(objVal);
                            }
                        }
                        else
                        {
                            //20160614, fauzil, TRBST15176, end
                            //tambahan kondisi khusus untuk FOP dan Transfer Asset
                            string tmp = dgvMain.Columns[intCount].HeaderText.ToString();
                            if ((tmp != "SecId") && (tmp != "DealId") && (tmp != "CIFId") && (tmp != "CIFNo") && (tmp != "CIFIdDebet") && (tmp != "CIFIdKredit") && (tmp != "TrxType") && (tmp != "TrxDetailType"))
                            {
                                objVal[0] = dgvMain.Columns[intCount].HeaderText.ToString(); ;// dgMain[intCount, 0].ToString();  
                                //20090729, David, BONDRTL004, begin
                                //objVal[1] = dsGridMain.Tables[0].Rows[indexdgv][intCount].ToString();
                                objVal[1] = dgvMain[intCount, indexdgv].Value.ToString();
                                //20090729, David, BONDRTL004, end
                                dtGridDetail.Rows.Add(objVal);
                            }
                            //20160614, fauzil, TRBST15176, begin
                        }
                        //20160614, fauzil, TRBST15176, end

                        if ((_strNodeKey == "TRS3" || _strNodeKey == "TRS4") && ((objVal[0].Trim() == "NoRekInvestor") && (objVal[1].Trim() == "504800000011")))
                        {
                            isGNCTROPS = true;
                        }
                    }
                }
                dgvDetail.DataSource = dtGridDetail;
                dgvDetail.Columns[1].Width = 600;

                if (isGNCTROPS)
                {
                    dgvDetail.DefaultCellStyle.ForeColor = Color.Red;
                }
                else
                {
                    dgvDetail.DefaultCellStyle.ForeColor = Color.Black;
                }
                //20090729, David, BONDRTL004, begin
            }
            else
            {
                dgvDetail.DataSource = null;
            }
            //20090729, David, BONDRTL004, end
            return;
        }

        public void checkedBit(bool State, DataGridViewCellMouseEventArgs e)
        {
            //20090729, David, BONDRTL004, begin
            if (e.RowIndex >= 0)
            {
                //20090729, David, BONDRTL004, end
                if (e.ColumnIndex == 0)
                {
                    dgvMain.EndEdit();
                    Int32 indexdgv;
                    if (dgvMain.CurrentRow != null)
                    {
                        indexdgv = dgvMain.CurrentRow.Index;

                    }
                    else
                    {
                        indexdgv = 0;
                    }

                    for (int i = 0; i < dgvMain.RowCount; i++)
                    {
                        for (int j = 0; j < dgvMain.ColumnCount; j++)
                        {
                            if (dgvMain.Columns[j].Name == "checked")
                            {
                                dgvMain.Rows[i].Cells["checked"].Value = State;
                            }
                            dgvMain.Rows[indexdgv].Cells["checked"].Value = State;

                        }

                    }

                }
                //20090729, David, BONDRTL004, begin
            }
            else
            {
                dgvDetail.DataSource = null;
            }
            //20090729, David, BONDRTL004, end
        }

        /// <summary>
        /// Kumpulan Method / Function
        /// </summary>
        /// 
        /* method untuk tidak menampilkan datagrid main 
         * dan datagrid detail 
         */
        public void clearGridMainDetal(bool State)
        {

            if (State == true)
            {
                dgvMain.DataSource = null;
                dgvDetail.DataSource = null;

            }

        }

        /* method untuk menampilkan data di datagrid main 
         * jika ada dan tidak di tampilkan jika tidak ada 
         */
        public void defaultGridMain(DataSet dsInput)
        {
            DataSet dsMain = new DataSet();
            dsMain = dsInput;
            if (dsMain != null)
            {
                if (dsMain.Tables.Count > 0)
                {
                    if (dsMain.Tables[0].Rows.Count > 0)
                    {
                        dgvMain.DataSource = dsMain.Tables[0];
                    }
                    else
                    {
                        clearGridMainDetal(true);
                    }
                }
                else
                {
                    clearGridMainDetal(true);
                }

            }
            else
            {
                clearGridMainDetal(true);
            }

        }

        /* method untuk menampilkan data di datagrid detail 
        * jika ada dan tidak di tampilkan jika tidak ada 
        */
        public void defaultGridDetail(DataSet dsInput)
        {
            DataSet dsDetail = new DataSet();
            DataTable dtGridDetail = new DataTable();
            dsDetail = dsInput;
            if (dsDetail.Tables.Count > 0)
            {
                if (dsDetail.Tables[0].Rows.Count > 0)
                {
                    Int32 intCount = 0;
                    Int32 iColCount = System.Convert.ToInt32(dsDetail.Tables[0].Columns.Count);

                    object[] objVal = new object[2];
                    dtGridDetail.Columns.Add("Item", System.Type.GetType("System.String"));
                    dtGridDetail.Columns.Add("Value", System.Type.GetType("System.String"));
                    dtGridDetail.Rows.Clear();

                    int iHead = 1;
                    for (intCount = 0; intCount < iColCount; intCount++)
                    {
                        objVal[0] = dgvMain.Columns[iHead].HeaderText.ToString(); // dgMain[intCount, 0].ToString();  
                        objVal[1] = dsDetail.Tables[0].Rows[0][intCount].ToString();
                        dtGridDetail.Rows.Add(objVal);
                        iHead++;

                    }
                    dgvDetail.DataSource = dtGridDetail;
                }
            }

        }

        private void dgvMain_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvMain.Rows.Count > 0)
            {
                //20090729, David, BONDRTL004, begin
                //subPopulateDetailGrid();
                subPopulateDetailGrid(e.ColumnIndex, e.RowIndex);
                //20090729, David, BONDRTL004, end
            }
        }

        private void dgvMain_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvMain.Rows.Count > 0)
            {
                //20090729, David, BONDRTL004, begin
                //subPopulateDetailGrid();
                subPopulateDetailGrid(e.ColumnIndex, e.RowIndex);
                //20090729, David, BONDRTL004, end
            }

            if ((_strNodeKey == "TRS3" || _strNodeKey == "TRS4") && dsGridMain.Tables[0].Rows[e.RowIndex]["NoRekInvestor"].ToString().Trim() == "504800000011")
            {
                dgvDetail.DefaultCellStyle.ForeColor = Color.Red;

                MessageBox.Show("No Rekening Relasi Milik GNC TROPS", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string GetXML(DataGridView dgv)
        {
            string result = "";

            result += "<ROOT>";
            for (int i = 0; i < dgv.RowCount; i++)
            {
                if (dgv[0, i].Value != null)
                {
                    result += "<RS ";

                    for (int j = 0; j < dgv.ColumnCount; j++)
                    {
                        result += dgv.Columns[j].Name + "=\"" + dgv[j, i].Value + "\" ";
                    }

                    result += "></RS>";
                }
            }
            result += "</ROOT>";

            return result;
        }

        private void dgvMain_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            checkedBit(true, e);
        }

        private void dgvMain_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            checkedBit(false, e);
        }
        //20161101, fauzil, CSODD16311, begin
        private void dgvMain_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_strNodeKey == "TRS28" || _strNodeKey == "TRS30")
            {
                if (e.RowIndex != -1)
                {
                    int index = e.RowIndex;
                    bool Status = bool.Parse(dgvMain.Rows[index].Cells["checked"].Value.ToString());
                    string NoJaminan = dgvMain.Rows[index].Cells["CollateralNo"].Value.ToString();
                    for (int i = 0; i <= dgvMain.Rows.Count - 1; i++)
                    {
                        if (index != i)
                        {
                            string NoJaminanOther = dgvMain.Rows[i].Cells["CollateralNo"].Value.ToString();
                            if (NoJaminan.Equals(NoJaminanOther, StringComparison.OrdinalIgnoreCase))
                            {
                                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)dgvMain.Rows[i].Cells["checked"];
                                chk.Value = Status;
                            }
                        }
                    }
                }
            }
            //20161114, fauzil, TRBST16240, begin
            if (SpText.Equals("trs_StatusSecurityTransaction2nd_TT", StringComparison.OrdinalIgnoreCase))
            {
                bool isChecked = bool.Parse(dgvMain.Rows[e.RowIndex].Cells["checked"].Value.ToString());
                string FOPUniqString = dgvMain.Rows[e.RowIndex].Cells["FOPUniq"].Value.ToString();
                if (!string.IsNullOrEmpty(FOPUniqString))
                {
                    for (int i = 0; i <= dgvMain.Rows.Count - 1; i++)
                    {
                        if (i != e.RowIndex)
                        {
                            if (FOPUniqString.Equals(dgvMain.Rows[i].Cells["FOPUniq"].Value.ToString(), StringComparison.OrdinalIgnoreCase))
                                dgvMain.Rows[i].Cells["checked"].Value = isChecked;
                        }
                    }
                    dgvMain.EndEdit();
                }
            }
            //20161114, fauzil, TRBST16240, end
        }
        //20161101, fauzil, CSODD16311, end
        //20171025, samy, COPOD17323, begin
        private void dgvMain_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            this._WFIDatacapPasswordEncyrypted = nispTextEncriptor.clsTextEncryptor.Decryption(this._WFIDatacapPassword, this._WFIDatacapKey); ;
            if (_strNodeKey == "TRS3" || _strNodeKey == "TRS4")
            {
                if (e.RowIndex < 0)
                    return;
                string cIsOAO = "";

                cIsOAO = dgvMain["isOnlineAcc", e.RowIndex].Value.ToString();

                if (cIsOAO == "Double Click for Detail")
                {
                    string CIFNo = dgvMain["CIFNo", e.RowIndex].Value.ToString();

                    dlgApprovalMasternasabahOAO dlg = new dlgApprovalMasternasabahOAO(CIFNo, this.cQuery, "Insert", this._WFIFilenetWSAddress, this._WFIDatacapUser, this._WFIDatacapPasswordEncyrypted, this._WFIDatacapAddressEverest, this._WFIFilenetAddress);
                    dlg.ShowDialog();
                }
            }
            else if (_strNodeKey == "TRS11" || _strNodeKey == "TRS12" || _strNodeKey == "TRS13"
                || _strNodeKey == "TRS15" || _strNodeKey == "TRS16" || _strNodeKey == "TRS17")
            {
                if (e.RowIndex < 0)
                    return;
                string cIsOAO = "";

                cIsOAO = dgvMain["isOnlineAcc", e.RowIndex].Value.ToString();

                if (cIsOAO == "Double Click for Detail")
                {
                    string DealId = dgvMain["DealId", e.RowIndex].Value.ToString();

                    dlgOAOApprovalTransaksi dlg = new dlgOAOApprovalTransaksi(DealId, this.cQuery, "Insert", this._WFIFilenetWSAddress, this._WFIDatacapUser, this._WFIDatacapPasswordEncyrypted, this._WFIDatacapAddressEverest, this._WFIFilenetAddress);
                    dlg.ShowDialog();
                }
            }
        }
        //20171025, samy, COPOD17323, end

        // 20160201, fauzil, TRBST16240, begin
        public System.Data.DataSet getAccBlockSequnceAndSeqAcBaseOrderIDTemp(long OrderIDTEmp, string type)
        {
            ObligasiQuery cQuery = new ObligasiQuery();
            System.Data.DataSet dsResult = new System.Data.DataSet();

            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[2];
            try
            {
                odpParam[0] = new System.Data.OleDb.OleDbParameter();
                odpParam[0].OleDbType = System.Data.OleDb.OleDbType.BigInt;
                odpParam[0].Value = OrderIDTEmp;
                odpParam[1] = new System.Data.OleDb.OleDbParameter();
                odpParam[1].OleDbType = System.Data.OleDb.OleDbType.Char;
                odpParam[1].Value = type;
                cQuery.ExecProc("dbo.trs_getAccBlokSeqOrder", ref odpParam, out dsResult);

                return dsResult;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
                return dsResult;
            }
        }
        // 20160201, fauzil, TRBST16240, end
        // 20160324, fauzil, TRBST16240, begin
        private bool DataToXML(int i, Guid GuidInternal, Guid GuidExternal)
        {
            DataTable dtTempInternal = new DataTable();
            DataTable dtTempExternal = new DataTable();

            dtTempInternal.Columns.Add("SourceReference");
            dtTempInternal.Columns.Add("guid");
            dtTempInternal.Columns.Add("Internal");
            dtTempInternal.Columns.Add("Instrument");
            dtTempInternal.Columns.Add("TradingDate");
            dtTempInternal.Columns.Add("Currency");
            dtTempInternal.Columns.Add("BuySell");
            dtTempInternal.Columns.Add("PayDate");
            dtTempInternal.Columns.Add("Nominal");
            dtTempInternal.Columns.Add("CleanPrice");
            dtTempInternal.Columns.Add("TraderCleanPrice");
            dtTempInternal.Columns.Add("SourceTrader");
            dtTempInternal.Columns.Add("TraderRM");
            dtTempInternal.Columns.Add("InternalDestTrader");
            dtTempInternal.Columns.Add("Profit");
            dtTempInternal.Columns.Add("CIF");
            dtTempInternal.Columns.Add("Action");
            dtTempInternal.Columns.Add("ReasonCode");
            dtTempInternal.Columns.Add("ReasonDesc");
            dtTempInternal.Columns.Add("ISINCode");
            //20171102, agireza, TRBST16240, begin
            dtTempInternal.Columns.Add("PortfolioTA");
            dtTempInternal.Columns.Add("PortfolioInternal");
            dtTempInternal.Columns.Add("Product");
            dtTempInternal.Columns.Add("TrxBranch");
            //20171102, agireza, TRBST16240, end
            //20200917, rezakahfi, BONDRETAIL-550, begin
            dtTempInternal.Columns.Add("SwitchingDealIdBankBeli");
            //20200917, rezakahfi, BONDRETAIL-550, end

            dtTempExternal.Columns.Add("SourceReference");
            dtTempExternal.Columns.Add("guid");
            dtTempExternal.Columns.Add("Internal");
            dtTempExternal.Columns.Add("Instrument");
            dtTempExternal.Columns.Add("TradingDate");
            dtTempExternal.Columns.Add("Currency");
            dtTempExternal.Columns.Add("BuySell");
            dtTempExternal.Columns.Add("PayDate");
            dtTempExternal.Columns.Add("Nominal");
            dtTempExternal.Columns.Add("CleanPrice");
            dtTempExternal.Columns.Add("TraderCleanPrice");
            dtTempExternal.Columns.Add("SourceTrader");
            dtTempExternal.Columns.Add("TraderRM");
            dtTempExternal.Columns.Add("InternalDestTrader");
            dtTempExternal.Columns.Add("Profit");
            dtTempExternal.Columns.Add("CIF");
            dtTempExternal.Columns.Add("Action");
            dtTempExternal.Columns.Add("ReasonCode");
            dtTempExternal.Columns.Add("ReasonDesc");
            dtTempExternal.Columns.Add("ISINCode");
            //20171102, agireza, TRBST16240, begin
            dtTempExternal.Columns.Add("PortfolioTA");
            dtTempExternal.Columns.Add("PortfolioInternal");
            dtTempExternal.Columns.Add("Product");
            dtTempExternal.Columns.Add("TrxBranch");
            //20171102, agireza, TRBST16240, end
            //20200917, rezakahfi, BONDRETAIL-550, begin
            dtTempExternal.Columns.Add("SwitchingDealIdBankBeli");
            //20200917, rezakahfi, BONDRETAIL-550, end

            DataTable data = (DataTable)dsGridMain.Tables[0];
            DataRow drInternal;
            DataRow drExternal;

            string BuySell = "";
            BuySell = data.Rows[i]["TrxType"].ToString();

            //20180129, uzia, TRBST16240, begin
            //if (BuySell == "3")
            //    BuySell = "B";
            //else BuySell = "S";
            //20180129, uzia, TRBST16240, end
            drInternal = dtTempInternal.NewRow();
            drInternal[0] = data.Rows[i]["DealId"];               //DealID
            drInternal[1] = GuidInternal;                         //GUID
            drInternal[2] = "Y";                                  //Y if internal N if External
            drInternal[3] = data.Rows[i]["SecurityNo"];           //SecNo
            drInternal[4] = DateTime.Parse(data.Rows[i]["TrxDate"].ToString()).ToString("yyyyMMdd");               //TrxDate
            drInternal[5] = data.Rows[i]["SecCcy"];               //SecCcy
            if (BuySell == "3")
                drInternal[6] = "S";
            else drInternal[6] = "B";
            drInternal[7] = DateTime.Parse(data.Rows[i]["SettlementDate"].ToString()).ToString("yyyyMMdd");       //SettlementDate
            string FaceValue = data.Rows[i]["FaceValue"].ToString();
            int index = FaceValue.IndexOf('.');
            FaceValue = FaceValue.Substring(0, index);
            drInternal[8] = FaceValue.Replace(",", "");            //Face Value
            drInternal[9] = data.Rows[i]["DealPrice"];            // ?? Clean Price HargaModal
            drInternal[10] = data.Rows[i]["HargaModal"];          // ?? Trader Clean Price
            drInternal[11] = data.Rows[i]["NIKSourceTrader"];     // TRSTAParameter_TM -> TA_BOND = 1 -- NIK NAMA
            drInternal[12] = data.Rows[i]["NIKTraderRM"];         // ?? TradeRM
            drInternal[13] = data.Rows[i]["NikDestTraderRM"];     // Today Trader_MX    
            drInternal[14] = data.Rows[i]["BranchProfit"];        // ?? profit
            drInternal[15] = data.Rows[i]["CIFNo"];               // CIFNo
            drInternal[16] = "INSERT";                            // INSERT
            drInternal[17] = "";                                  // Kosongkan
            drInternal[18] = "";                                  // Kosongkan
            drInternal[19] = data.Rows[i]["ISINCode"];            // ISINCode
            //20171102, agireza, TRBST16240, begin
            drInternal[20] = data.Rows[i]["PortfolioTA"];
            drInternal[21] = data.Rows[i]["PortfolioInternal"];
            drInternal["Product"] = "BOND";
            drInternal["TrxBranch"] = data.Rows[i]["TrxBranch"];
            //20171102, agireza, TRBST16240, end
            //20200917, rezakahfi, BONDRETAIL-550, begin
            drInternal["SwitchingDealIdBankBeli"] = data.Rows[i]["Switching"];
            //20200917, rezakahfi, BONDRETAIL-550, end
            dtTempInternal.Rows.Add(drInternal);

            drExternal = dtTempExternal.NewRow();
            drExternal[0] = data.Rows[i]["DealId"];               //DealID
            drExternal[1] = GuidExternal;                         //GUID
            drExternal[2] = "N";                                  //Y if internal N if External
            drExternal[3] = data.Rows[i]["SecurityNo"];           //SecNo
            drExternal[4] = DateTime.Parse(data.Rows[i]["TrxDate"].ToString()).ToString("yyyyMMdd");              //TrxDate
            drExternal[5] = data.Rows[i]["SecCcy"];               //SecCcy
            if (BuySell == "3")
                drExternal[6] = "B";
            else drExternal[6] = "S";
            drExternal[7] = DateTime.Parse(data.Rows[i]["SettlementDate"].ToString()).ToString("yyyyMMdd");       //SettlementDate
            string FaceValue2 = data.Rows[i]["FaceValue"].ToString();
            int index2 = FaceValue2.IndexOf('.');
            FaceValue2 = FaceValue2.Substring(0, index2);
            drExternal[8] = FaceValue2.Replace(",", "");            //Face Value
            drExternal[9] = data.Rows[i]["DealPrice"];            // ?? Clean Price
            drExternal[10] = data.Rows[i]["HargaModal"];          // ?? Trader Clean Price
            drExternal[11] = data.Rows[i]["NIKSourceTrader"];     // TRSTAParameter_TM -> TA_BOND = 1 -- NIK NAMA
            drExternal[12] = data.Rows[i]["NIKTraderRM"];         // ?? TradeRM
            drExternal[13] = data.Rows[i]["NikDestTraderRM"];     // Today Trader_MX    
            drExternal[14] = data.Rows[i]["BranchProfit"];        // ?? profit
            drExternal[15] = data.Rows[i]["CIFNo"];               // CIFNo
            drExternal[16] = "INSERT";                            // INSERT
            drExternal[17] = "";                                  // Kosongkan
            drExternal[18] = "";                                  // Kosongkan
            drExternal[19] = data.Rows[i]["ISINCode"];            // ISINCode
            //20171102, agireza, TRBST16240, begin
            drExternal[20] = data.Rows[i]["PortfolioTA"];
            drExternal[21] = data.Rows[i]["PortfolioInternal"];
            drExternal["Product"] = "BOND";
            drExternal["TrxBranch"] = data.Rows[i]["TrxBranch"];
            //20171102, agireza, TRBST16240, end
            //20200917, rezakahfi, BONDRETAIL-550, begin
            drExternal["SwitchingDealIdBankBeli"] = data.Rows[i]["Switching"];
            //20200917, rezakahfi, BONDRETAIL-550, end
            dtTempExternal.Rows.Add(drExternal);


            string xmlInternal = Model.clsXML.GetXMLFLD(dtTempInternal);
            string xmlExternal = Model.clsXML.GetXMLFLD(dtTempExternal);
            DataSet dsOutWS = new DataSet();
            string strError = "";

            //20250514, dion.wijna, BONDRETAIL-1696, begin
            //clsCallWebService.CallOBL_WSAddFLDCTP(xmlInternal, this.intNIK, out dsOutWS, out strError);
            GatewaySftpModelRq req;
            clsAPISyncGatewayService clsSyncGw = new clsAPISyncGatewayService(this.intNIK, null, this.strModule);
            
            req = new GatewaySftpModelRq
            {
                referenceCode = dtTempInternal.Rows[0]["guid"].ToString(),
                fileName = dtTempInternal.Rows[0]["guid"].ToString() + ".xml",
                content = xmlInternal
            };
            clsSyncGw.CallPutMurexDealRequest(req, out strError);

            //clsCallWebService.CallOBL_WSAddFLDTransaction(xmlExternal, this.intNIK, out dsOutWS, out strError);
            req = new GatewaySftpModelRq
            {
                referenceCode = dtTempExternal.Rows[0]["guid"].ToString(),
                fileName = dtTempExternal.Rows[0]["guid"].ToString() + ".xml",
                content = xmlExternal
            };
            clsSyncGw.CallPutMurexDealRequest(req, out strError);
            //20250514, dion.wijna, BONDRETAIL-1696, end

            if (strError.Trim() != "")
            {
                MessageBox.Show(strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool DataToXML(string SecAccno)
        {
            DataTable dtTempInternal = new DataTable();
            RegistrasiNasabah nasabah = new RegistrasiNasabah();
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
            dtTempInternal.Columns.Add("Type");


            DataSet ds = nasabah.getPopulateMasterNasabahForMurex(SecAccno);
            DataRow drInternal;

            string[] alamat = ds.Tables[0].Rows[0]["AlamatIdentitas"].ToString().Split('&');
            string N2 = alamat[0];
            string N3 = alamat[1];
            string N4 = alamat[2];
            drInternal = dtTempInternal.NewRow();
            drInternal[0] = ds.Tables[0].Rows[0]["Guid"].ToString();
            drInternal[1] = "INSERT";
            drInternal[2] = ds.Tables[0].Rows[0]["mxLabel"].ToString();
            drInternal[3] = ds.Tables[0].Rows[0]["CUS1"].ToString();
            drInternal[4] = ds.Tables[0].Rows[0]["CUN"].ToString();
            drInternal[5] = ds.Tables[0].Rows[0]["CTP1"].ToString();
            drInternal[6] = N2;
            drInternal[7] = N3;
            drInternal[8] = N4;
            drInternal[9] = ds.Tables[0].Rows[0]["NA5"].ToString();
            drInternal[10] = ds.Tables[0].Rows[0]["PHN"].ToString();
            drInternal[11] = ds.Tables[0].Rows[0]["FAX"].ToString();
            drInternal[12] = ds.Tables[0].Rows[0]["NPWP"].ToString();
            drInternal[13] = ds.Tables[0].Rows[0]["COUNTRY"].ToString();
            drInternal[14] = ds.Tables[0].Rows[0]["EMAIL"].ToString();
            drInternal[15] = ds.Tables[0].Rows[0]["RELT"].ToString();
            drInternal[16] = ds.Tables[0].Rows[0]["CORP"].ToString();
            drInternal[17] = ds.Tables[0].Rows[0]["CTP"].ToString();
            drInternal[18] = ds.Tables[0].Rows[0]["RES"].ToString();
            drInternal[19] = ds.Tables[0].Rows[0]["RMCOD"].ToString();
            drInternal[20] = ds.Tables[0].Rows[0]["SLBHU"].ToString();
            drInternal[21] = ds.Tables[0].Rows[0]["CITIZEN"].ToString();
            drInternal[22] = "BOND";

            dtTempInternal.Rows.Add(drInternal);


            string xmlInternal = Model.clsXML.GetXMLCTP(dtTempInternal);
            DataSet dsOutWS = new DataSet();
            string strError = "";

            //20250514, dion.wijna, BONDRETAIL-1696, begin
            //clsCallWebService.CallOBL_WSAddFLDCTP(xmlInternal, this.intNIK, out dsOutWS, out strError);
            clsAPISyncGatewayService clsSyncGw = new clsAPISyncGatewayService(this.intNIK, null, this.strModule);
            GatewaySftpModelRq req = new GatewaySftpModelRq
            {
                referenceCode = dtTempInternal.Rows[0]["guid"].ToString(),
                fileName = dtTempInternal.Rows[0]["guid"].ToString() + ".xml",
                content = xmlInternal
            };
            clsSyncGw.CallPutMurexCounterpartyRequest(req, out strError);
            //20250514, dion.wijna, BONDRETAIL-1696, end

            if (strError.Trim() != "")
            {
                MessageBox.Show(strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void SentEmail(string CIFNo, string Nama, string SecAccNo)
        {
            try
            {
                string subject = "";
                string body = "";
                string emailReceipt = "";
                string emailSender = "";
                string emailName = "AppNewAccountCustomer";
                string strXML = "", errMessage = "";
                int intErrorNumber = 0;
                DataSet dsResultEmail = new DataSet();
                OleDbParameter[] dbParam = new OleDbParameter[1];

                (dbParam[0] = new OleDbParameter("@pcEmailName", OleDbType.VarChar, 50)).Value = emailName;

                bool blnResult = cQuery.ExecProc("TRSGetEmailLookUp", ref dbParam, out dsResultEmail);

                if (blnResult)
                {
                    if (dsResultEmail.Tables[0].Rows.Count > 0)
                    {
                        emailSender = dsResultEmail.Tables[0].Rows[0]["SenderEmail"].ToString();
                        subject = dsResultEmail.Tables[0].Rows[0]["SubjectEmail"].ToString();
                        body = dsResultEmail.Tables[0].Rows[0]["BodyEmail"].ToString();

                        subject = subject.Replace("#CIF#", CIFNo);
                        subject = subject.Replace("#NAMA#", Nama);

                        body = body.Replace("#CIF#", CIFNo);
                        body = body.Replace("#NAMA#", Nama);
                        body = body.Replace("#SECACCNO#", SecAccNo);

                    }

                    if (dsResultEmail.Tables[1].Rows.Count > 0)
                    {
                        AlertService.AlertService alert = new AlertService.AlertService();
                        string strGuid = Guid.NewGuid().ToString().Trim();
                        for (int i = 0; i < dsResultEmail.Tables[1].Rows.Count; i++)
                        {
                            emailReceipt = dsResultEmail.Tables[1].Rows[i]["EmailReceipt"].ToString();

                            alert.SendEmailWithoutAttachment(ref strGuid, this.intNIK.ToString(), subject, body,
                                emailSender, "", emailReceipt, out strXML, out intErrorNumber, out errMessage);

                            emailReceipt = "";
                        }

                    }
                }

                if (errMessage != "")
                {
                    throw new Exception(errMessage);
                }

            }

            catch (Exception ex)
            {
                MessageBox.Show("[Send Email Error]:" + ex.Message, "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //20180124, samy, TRBST16240, begin
        //private void SentEmail(string CIFNo, string Nama, string DealId, string NIKSeller, string NamaSeller, string NIKInputer, string NameInputer)
        private void SentEmail(string CIFNo, string Nama, string DealId, string NIKSeller, string NamaSeller, string NIKInputer, string NameInputer, string SecAccNo)
        //20180124, samy, TRBST16240, end
        {
            try
            {
                string subject = "";
                string body = "";
                string emailSender = "";
                string emailName = "PushBackTransaksi";
                string strXML = "", errMessage = "";
                int intErrorNumber = 0;
                DataSet dsResultEmail = new DataSet();
                OleDbParameter[] dbParam = new OleDbParameter[1];

                (dbParam[0] = new OleDbParameter("@pcEmailName", OleDbType.VarChar, 50)).Value = emailName;

                bool blnResult = cQuery.ExecProc("TRSGetEmailLookUp", ref dbParam, out dsResultEmail);

                if (blnResult)
                {
                    if (dsResultEmail.Tables[0].Rows.Count > 0)
                    {
                        emailSender = dsResultEmail.Tables[0].Rows[0]["SenderEmail"].ToString();
                        subject = dsResultEmail.Tables[0].Rows[0]["SubjectEmail"].ToString();
                        body = dsResultEmail.Tables[0].Rows[0]["BodyEmail"].ToString();

                        subject = subject.Replace("#DEALID#", DealId);
                        subject = subject.Replace("#CIF#", CIFNo);

                        body = body.Replace("#DEALID#", DealId);
                        body = body.Replace("#CIF#", CIFNo);
                        body = body.Replace("#NAMA#", Nama);
                        //20180124, samy, TRBST16240, begin
                        body = body.Replace("#SECACCNO#", SecAccNo);
                        //20180124, samy, TRBST16240, end

                    }

                    AlertService.AlertService alert = new AlertService.AlertService();
                    string strGuid = Guid.NewGuid().ToString().Trim();
                    string messageBody = "";
                    string emailReceipt = "";
                    string nameSPV = "";
                    string emailReceiptSPV = "";

                    // inputert
                    OleDbParameter[] dbParamInputer = new OleDbParameter[4];
                    (dbParamInputer[0] = new OleDbParameter("@pnNik", OleDbType.Integer)).Value = int.Parse(NIKInputer);
                    dbParamInputer[1] = new OleDbParameter("@pvEmail", OleDbType.VarChar, 70);
                    dbParamInputer[1].Direction = ParameterDirection.Output;
                    dbParamInputer[2] = new OleDbParameter("@pvNameSPV", OleDbType.VarChar, 50);
                    dbParamInputer[2].Direction = ParameterDirection.Output;
                    dbParamInputer[3] = new OleDbParameter("@pvEmailSPV", OleDbType.VarChar, 70);
                    dbParamInputer[3].Direction = ParameterDirection.Output;
                    bool blnResultInputer = cQuery.ExecProc("TRSGetEmailForPushBack", ref dbParamInputer);

                    if (blnResultInputer)
                    {
                        emailReceipt = dbParamInputer[1].Value.ToString().Trim();
                        nameSPV = dbParamInputer[2].Value.ToString().Trim();
                        emailReceiptSPV = dbParamInputer[3].Value.ToString().Trim();

                        if (emailReceipt.Trim().Length > 0) // inputer
                        {
                            messageBody = body.Replace("#NAMATUJUAN#", NameInputer);
                            alert.SendEmailWithoutAttachment(ref strGuid, this.intNIK.ToString(), subject, messageBody,
                               emailSender, "", emailReceipt, out strXML, out intErrorNumber, out errMessage);
                            if (errMessage != "")
                            {
                                throw new Exception(errMessage);
                            }
                            messageBody = "";
                        }

                        if (emailReceiptSPV.Trim().Length > 0) // inputer SPV
                        {
                            messageBody = body.Replace("#NAMATUJUAN#", nameSPV);
                            alert.SendEmailWithoutAttachment(ref strGuid, this.intNIK.ToString(), subject, messageBody,
                               emailSender, "", emailReceiptSPV, out strXML, out intErrorNumber, out errMessage);
                            if (errMessage != "")
                            {
                                throw new Exception(errMessage);
                            }
                            messageBody = "";
                        }

                    }

                    emailReceipt = "";
                    nameSPV = "";
                    emailReceiptSPV = "";

                    //seller
                    OleDbParameter[] dbParamSeller = new OleDbParameter[4];
                    (dbParamSeller[0] = new OleDbParameter("@pnNik", OleDbType.Integer)).Value = int.Parse(NIKSeller);
                    dbParamSeller[1] = new OleDbParameter("@pvEmail", OleDbType.VarChar, 70);
                    dbParamSeller[1].Direction = ParameterDirection.Output;
                    dbParamSeller[2] = new OleDbParameter("@pvNameSPV", OleDbType.VarChar, 50);
                    dbParamSeller[2].Direction = ParameterDirection.Output;
                    dbParamSeller[3] = new OleDbParameter("@pvEmailSPV", OleDbType.VarChar, 70);
                    dbParamSeller[3].Direction = ParameterDirection.Output;
                    bool blnResultSeller = cQuery.ExecProc("TRSGetEmailForPushBack", ref dbParamSeller);

                    if (blnResultSeller)
                    {
                        emailReceipt = dbParamSeller[1].Value.ToString().Trim();
                        nameSPV = dbParamSeller[2].Value.ToString().Trim();
                        emailReceiptSPV = dbParamSeller[3].Value.ToString().Trim();

                        if (emailReceipt.Trim().Length > 0) // seller
                        {
                            messageBody = body.Replace("#NAMATUJUAN#", NamaSeller);
                            alert.SendEmailWithoutAttachment(ref strGuid, this.intNIK.ToString(), subject, messageBody,
                               emailSender, "", emailReceipt, out strXML, out intErrorNumber, out errMessage);
                            if (errMessage != "")
                            {
                                throw new Exception(errMessage);
                            }
                            messageBody = "";
                        }

                        if (emailReceiptSPV.Trim().Length > 0) // inputer SPV
                        {
                            messageBody = body.Replace("#NAMATUJUAN#", nameSPV);
                            alert.SendEmailWithoutAttachment(ref strGuid, this.intNIK.ToString(), subject, messageBody,
                               emailSender, "", emailReceiptSPV, out strXML, out intErrorNumber, out errMessage);
                            if (errMessage != "")
                            {
                                throw new Exception(errMessage);
                            }
                            messageBody = "";
                        }

                    }

                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("[Send Email Error]:" + ex.Message, "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        // 20160324, fauzil, TRBST16240, end
    }
}