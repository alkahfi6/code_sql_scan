using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Data.OleDb;
//using ComUtil;
using Utility;
//20120802, hermanto_salim, BAALN12003, begin   
using System.IO;    
//20120802, hermanto_salim, BAALN12003, end   

namespace BankNISP.Obligasi01
{
    public partial class frmObligasiTransaksiSuratBerharga : BankNISP.Template.StandardForm
    {
        //20120802, hermanto_salim, BAALN12003, begin   
        private const int SELLER_NO_SERTIFIKAT_COLUMN_INDEX = 2;
        private const int SELLER_EXPIRE_COLUMN_INDEX = 3;
        //20120802, hermanto_salim, BAALN12003, end   
        internal ObligasiQuery cQuery;
        public string strFlag;
        public int iSecId;
        public int CIFid;
        public int iInterestTypeID;
        public decimal iSafeKeepingFeeAfterTax;
        public decimal iTransactionFee;
        public decimal iTotalProceed;
        public decimal dMinSafeKeepingFee;
        public Int32 intNIK;
        public string strGuid;
        public string strLocalMenu;
        private DataView _dvAkses;
        private string[] _strDefToolBar;
        public string strModule;
        public string strMenuName;
        public string strWorkDate;
        public bool strTrxValidate;
        private DateUtility FormatDate = new DateUtility();
        //20090910, David, SYARIAH001, begin
        //20190116, samypasha, BOSOD18432, begin
        //static string strBranchCode;
        public string strBranchCode;
        //20190116, samypasha, BOSOD18432, end
        //20090910, David, SYARIAH001, end
        //20120802, hermanto_salim, BAALN12003, begin   
        private clsCallWebService clsCallWebService;
        private DataTable dgvTransactionLinkColumnProperty;
        private DataTable dtAmendTransaksi;
        private string lastSecAccNo;
        private string lastSecurityNo;
        private decimal? effectiveBalance;
        //20130211, victor, BAALN12003, begin
        private int? AccountStatus;
        //20130211, victor, BAALN12003, end
        //20120802, hermanto_salim, BAALN12003, end   
        //20130222, victor, BAALN12003, begin
        private string ProductCode;
        private string AccountType;
        //20130222, victor, BAALN12003, end
        //20130122, uzia, BAALN12003, begin
        private string CIFNo;
        //20130122, uzia, BAALN12003, end
        //20130717, uzia, BAFEM12012, begin
        private DataSet _dsAccrued = null;
        //20130717, uzia, BAFEM12012, end
        //20160818, fauzil, LOGEN196, begin
        private string NoRekInvestorTaxAmnesty = "";
        private string NamaRekInvestorTaxAmnesty = "";
        private bool iTaxAmnesty = false;
        TransaksiBankBeli transaksiBankBeliTA = new TransaksiBankBeli();
        private decimal FaceValueTA;
        private string NoRekInvestorTA;
        TransaksiBankBeli transaksiBankBeliNonTA = new TransaksiBankBeli();
        private decimal FaceValueNonTA;
        private string NoRekInvestorNonTA;
        //20160818, fauzil, LOGEN196, end
        //20161206, agireza, TRBST16249, begin
        private string _strSID = "";
        //20161206, agireza, TRBST16249, end
        //20160225, fauzil, TRBST16240, begin
        private string trxType = "ALL";
        private bool needTanggal = false;
        //20160225, fauzil, TRBST16240, end
        //20160229, fauzil, TRBST16240, begin
        private string Currency = "";
        private DateTime? dLastUpdateJenisRiskProfile = null;
        private bool NeedUpdateDataNasabah = false;
        private bool flagClear = false;
        private string updateNorekSecurity = "";
        //20160229, fauzil, TRBST16240, end
        //20161019, fauzil, TRBST16240, begin
        private decimal HargaModalAwal;
        //20161019, fauzil, TRBST15176, end
        //20160407, agireza, TRSBT16240, begin
        private bool _bEarlyRedemption;
        //20160407, agireza, TRSBT16240, end
        //20170830, imelda, COPOD17271 , begin
        private char _RiskProfileProduct;
        private char _RiskProfileCust;
		public string strClassificationId;
        //20170830, imelda, COPOD17271 , end
        //20171006, agireza, COPOD17271, begin
        private bool isNeedRecalculate;
        //20171006, agireza, COPOD17271, end
        //20171108, agireza, TRBST16240, begin
        private bool _bIsCorporateBond;
        private string _strSpouseName;
        //20171108, agireza, TRBST16240, end
        //20180213, uzia, LOGEN00584, begin
        private string _strMaritalStatus;
        //20180213, uzia, LOGEN00584, end
        //20180730, samypasha, LOGEN00665, begin
        private bool _isPVB;
        //20180730, samypasha, LOGEN00665, end
        //20190116, samypasha, BOSOD18432, begin
        dlgFMCTSourceFund myFormSourceFund;
        //20190116, samypasha, BOSOD18432, end
        //20190715, darul.wahid, BOSIT18196, begin
        private DataSet dsPort;
        //20190715, darul.wahid, BOSIT18196, end
        //20200417, uzia, BONDRETAIL-257, begin
        private ParamTAModel _paramTAModel = null;
        //20200417, uzia, BONDRETAIL-257, end
        //20220208, darul.wahid, BONDRETAIL-895, begin
        private decimal dCapitalGain;
        //20220208, darul.wahid, BONDRETAIL-895, end
        //20220708, darul.wahid, BONDRETAIL-977, begin
        private decimal dCapitalGainNonIDR;
        private decimal dTotalTax;
        private decimal dIncome;
        //20220708, darul.wahid, BONDRETAIL-977, end
        ////20221020, Tobias Renal, HFUNDING-181, Begin
        private DataTable dtSimanis;
        ////20221020, Tobias Renal, HFUNDING-181, End
        //20240422, alfian.andhika, BONDRETAIL-1581, begin
        private decimal dYieldHargaModal;
        //20240422, alfian.andhika, BONDRETAIL-1581, end
        //20220708, yudha.n, BONDRETAIL-1052, begin
        private bool _bCalculate = false;
        private bool isMeteraiAbsorbed;
        private decimal dMateraiCost;
        private decimal dUpdateMateraiCost;
        private decimal? effectiveBalanceMaterai;
        private string AccountTypeMaterai;
        public string strUserBranch;
        private string updateNorekMeterai;
        private string updateMateraiAccountBlockACTYPE;
        private int updateMateraiAccountBlockSequence;
        private clsBlokirSaldo _clsBlokirSaldo;
        //20220708, yudha.n, BONDRETAIL-1052, end
        //20231227, rezakahfi, BONDRETAIL-1513, begin
        private clsInqTransactionSB InqData;
        private clsBlokirSaldo _clsSaldo;
        public bool isTA = false;
        //20231227, rezakahfi, BONDRETAIL-1513, end
        //20231205,pratama, BONDRETAIL-1392, begin
        private decimal weightedSpread;
        private decimal weightedPrice;
        private decimal indikasiTotalSpread;
        private decimal weightedHoldingPeriod;
        private decimal untungRugiNasabah;
        private int keterangan;
        //20231205,pratama, BONDRETAIL-1392, end

        public frmObligasiTransaksiSuratBerharga()
        {
            InitializeComponent();
        }

        public string[] DefToolBar
        {
            get { return _strDefToolBar; }
            set { _strDefToolBar = value; }
        }

        public void InitializeForm()
        {
            //20120802, hermanto_salim, BAALN12003, begin   
            clsCallWebService = new clsCallWebService();
            clsCallWebService.clsWebServiceLoad(intNIK.ToString(), Guid.NewGuid().ToString(), strModule, cQuery);
            //20120802, hermanto_salim, BAALN12003, end
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            TransaksiSuratBerharga SB = new TransaksiSuratBerharga();
            DataSet ds = new DataSet();
            DataSet dsWorkDate = new DataSet();

            dsWorkDate = SB.CurrentWorkingDate();
            strWorkDate = dsWorkDate.Tables[0].Rows[0]["working_date"].ToString();


            ds = SuratBerharga.ListTypeTransaction();
            cmbJenisTransaksi.DataSource = ds.Tables[0];
            cmbJenisTransaksi.DisplayMember = "TrxDesc";
            cmbJenisTransaksi.ValueMember = "TrxTypeID";
            cmpsrNomorSekuriti.Criteria = "1";
            //201600622, fauzil, TRBST16240, begin
            gbSumberDana.Visible = false;
            //201600622, fauzil, TRBST16240, end
            //20180116, samy, BOSOD18243, begin

            this.cmbSourceFund.DataSource = getSourceFund();
            this.cmbSourceFund.ValueMember = "Value";
            this.cmbSourceFund.DisplayMember = "Description";

            this.cmbSourceFund.SelectedIndex = 0;
            createTableSource();
            //20180116, samy, BOSOD18243, end
            //20200417, uzia, BONDRETAIL-257, begin
            this._paramTAModel = new ParamTAModel(this.cQuery, intNIK, strBranchCode);
            //20200417, uzia, BONDRETAIL-257, end
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            InqData = new clsInqTransactionSB(cQuery, intNIK, strBranchCode);
            _clsSaldo = new clsBlokirSaldo(cQuery, intNIK, strBranchCode, Module);
            //20231227, rezakahfi, BONDRETAIL-1513, end
        }

        private void subResetToolBar()
        {
            //20180210, uzia, TRBST16240, begin
            _strDefToolBar = new string[1];
            _strDefToolBar[0] = "1";
            //20180210, uzia, TRBST16240, end

//20231227, rezakahfi, BONDRETAIL-1513, begin
            if (!isTA)
            {
//20231227, rezakahfi, BONDRETAIL-1513, begin
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
                if (strLocalMenu == "mnuTransaksiObligasi")
                {
                    this.NISPToolbarButton("0").Visible = false;
                    //20090910, David, SYARIAH001, begin
                    this.NISPToolbarButton("4").Visible = false;
                    //20090910, David, SYARIAH001, end
                    //20161227, agireza, TRBST16249, begin
                    if (this._strSID == "" || this._bEarlyRedemption)
                    {
                        this.NISPToolbarButton("6").Visible = false;
                    }
                    //20161227, agireza, TRBST16249, end
                }
//20231227, rezakahfi, BONDRETAIL-1513, begin
            }
            else
            {
                string[] visibleToolbar = new string[5];
                visibleToolbar[0] = "1";
                visibleToolbar[1] = "2";
                visibleToolbar[2] = "3";
                visibleToolbar[3] = "6";
                visibleToolbar[4] = "7";
                this.NISPToolbarButtonSetVisible(true, visibleToolbar);
            }
//20231227, rezakahfi, BONDRETAIL-1513, end

            //string[] strToolbarId = new string[4];
            //strToolbarId[0] = "1";
            //strToolbarId[1] = "7";
            //strToolbarId[2] = "6";
            //strToolbarId[3] = "2";

            //this.NISPToolbarButtonSetVisible(true, strToolbarId);
        }

        private void frmObligasiTransaksiSuratBerharga_Load(object sender, EventArgs e)
        {
            try
            {
                InitializeForm();
//20231227, rezakahfi, BONDRETAIL-1513, begin
                if (!isTA)
                {
                    //20231227, rezakahfi, BONDRETAIL-1513, end
                    DataSet dsTreeview;
                    OleDbParameter[] dbParam = new OleDbParameter[3];

                    (dbParam[0] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                    (dbParam[1] = new OleDbParameter("@pcModule", OleDbType.VarChar, 30)).Value = strModule;
                    (dbParam[2] = new OleDbParameter("@pcMenuName", OleDbType.VarChar, 50)).Value = strMenuName;

                    bool blnResult = cQuery.ExecProc("UserGetTreeView", ref dbParam, out dsTreeview);

                    //20160426, samy, TRBST16240, begin
                    //Debug Dev/UAT
                    //string[] visibleToolbar = new string[4];
                    //visibleToolbar[0] = "1";
                    //visibleToolbar[1] = "7";
                    //visibleToolbar[2] = "6";
                    //visibleToolbar[3] = "2";
                    //this.NISPToolbarButtonSetVisible(true, visibleToolbar);
                    //20160426, samy, TRBST16240, end

                    //20160519, fauzil, TRBST16240, begin
                    if (blnResult == true)
                    {
                        _dvAkses = new DataView(dsTreeview.Tables[1]);
                        subResetToolBar();
                    }
                    else
                    {
                        MessageBox.Show("Error Load ToolBar");
                    }
                    //20160519, fauzil, TRBST16240, end
//20231227, rezakahfi, BONDRETAIL-1513, begin
                }
                else
                {
                    string[] visibleToolbar = new string[5];
                    visibleToolbar[0] = "1";
                    visibleToolbar[1] = "2";
                    visibleToolbar[2] = "3";
                    visibleToolbar[3] = "6";
                    visibleToolbar[4] = "7";
                    this.NISPToolbarButtonSetVisible(true, visibleToolbar);

                    //lblHargaModal.Visible = true;
                    //ndHargaModal.Visible = true;
                }
//20231227, rezakahfi, BONDRETAIL-1513, end

                controlsEnabled(false);
                controlsClear(true);
                DataSet dsBranch = new DataSet();
                RegistrasiNasabah Nasabah = new RegistrasiNasabah();
                dsBranch = Nasabah.getInfoUser(intNIK);
                cmpsrSearch1.Text1 = dsBranch.Tables[0].Rows[0]["office_id_sibs"].ToString();
                cmpsrSearch1.ValidateField();
                cmpsrNomorSekuriti.Focus();
                strFlag = "";
                //20090910, David, SYARIAH001, begin
                //20240611, uzia, HTR-249, begin
                //strBranchCode = cmpsrSearch1.Text1.Trim();
                strBranchCode = (isTA ? strUserBranch : cmpsrSearch1.Text1.Trim());
                //20240611, uzia, HTR-249, end
                lblDealNumber.Visible = false;
                txtDealNumber.Visible = false;
                btnCari.Visible = false;
                //20090910, David, SYARIAH001, end
                //20120802, hermanto_salim, BAALN12003, begin
                DataSet dsResult = new DataSet();
                if (clsDatabase.subTRSGetMenuSettings(cQuery, new object[] { "mnuTransaksiObligasi" }, out dsResult))
                {
                    dgvTransactionLinkColumnProperty = dsResult.Tables[0];
                }
                //20120802, hermanto_salim, BAALN12003, end
                //20130305, uzia, BAALN12003, begin
                chkFlagPhoneOrder.Enabled = false;
                //20130305, uzia, BAALN12003, end
                //20160524, fauzil, TRBST16240, begin
                //20231227, rezakahfi, BONDRETAIL-1513, begin
                if (!isTA)
                {
                    ndHargaModal.Visible = false;
                }
                //20231227, rezakahfi, BONDRETAIL-1513, end
                //20160524, fauzil, TRBST15176, end
				//20171006, agireza, COPOD17271, begin
                isNeedRecalculate = true;
                //20171006, agireza, COPOD17271, end
                //20200417, uzia, BONDRETAIL-257, begin
                ValidateTA();
                //20200417, uzia, BONDRETAIL-257, end
                //20220708, yudha.n, BONDRETAIL-1052, begin
                _clsBlokirSaldo = new clsBlokirSaldo(cQuery, intNIK, strUserBranch, strModule);
                //20220708, yudha.n, BONDRETAIL-1052, end
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void frmObligasiTransaksiSuratBerharga_OnNISPToolbarClick(ref ToolStripButton NISPToolbarButton)
        {
            switch (NISPToolbarButton.Name)
            {
                case "1":
                    this.Close();
                    break;
                case "7":
                    btnRefresh();
                    break;
                case "6":
                    //20200417, uzia, BONDRETAIL-257, begin
                    if (!ValidateTA())
                        return;
                    //20200417, uzia, BONDRETAIL-257, end
                    btnSave();
                    break;
                //20090910, David, SYARIAH001, begin
                case "2":
                    btnSearch();
                    break;
                //20130107, victor, BAALN12003, begin
                //case "4":
                //    btnUpdate();
                //    break;
                //20130107, victor, BAALN12003, end
                //20090910, David, SYARIAH001, end
            }
        }

        //20090910, David, SYARIAH001, begin
        public void btnSearch()
        {
            //20121127, hermanto_salim, BAALN12003, begin
            strFlag = "";
            //20121127, hermanto_salim, BAALN12003, end
            controlsEnabled(false);
            controlsClear(true);
            strFlag = "Search";

            //enabled object utk mencari deal number
            lblDealNumber.Visible = true;
            //20160524, fauzil, TRBST16240, begin
            //txtDealNumber.Visible = true;
            //btnCari.Visible = true;
            cmpsrGetPushBack.Visible = true;
            //20160524, fauzil, TRBST16240, end
            //disabled semua object mandatory
            cmpsrNomorSekuriti.Enabled = false;
            cmpsrSearch1.Text1 = strBranchCode;
            cmpsrSearch1.ValidateField();
            cmpsrSearch1.Enabled = false;
            cmbJenisTransaksi.Enabled = false;

            txtTaxOnCapitalGainLoss.Visible = true;
            lblTaxOnCapitalGainLoss.Visible = true;
            cmbSafeKeepingFeeAfterTax.Visible = true;
            lblSafeKeepingFee.Visible = true;
            txtPajakBungaBerjalan.Visible = true;
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            lblTaxOnAccrued.Visible = true;
            //20231227, rezakahfi, BONDRETAIL-1513, end

            //20130107, victor, BAALN12003, begin
            //this.NISPToolbarButton("4").Visible = true;
            //20130107, victor, BAALN12003, end
            this.NISPToolbarButton("6").Visible = false;
            this.NISPToolbarButton("7").Visible = true;
        }

        public void btnUpdate()
        {
            if (txtNamaNasabah.Text != "")
            {
                controlsEnabled(true);
                strFlag = "Update";

                //disabled semua object mandatory
                cmpsrNomorSekuriti.Enabled = false;
                cmpsrSearch1.Text1 = strBranchCode;
                cmpsrSearch1.ValidateField();
                cmpsrSearch1.Enabled = false;
                cmbJenisTransaksi.Enabled = false;

                this.NISPToolbarButton("2").Visible = false;
                //20130107, victor, BAALN12003, begin
                //this.NISPToolbarButton("4").Visible = false;
                //20130107, victor, BAALN12003, end
                this.NISPToolbarButton("6").Visible = true;
                this.NISPToolbarButton("7").Visible = true;
            }
            else
            {
                MessageBox.Show("Data masih kosong", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //20160524, fauzil, TRBST16240, begin
                //txtDealNumber.Focus();
                cmpsrGetPushBack.Focus();
                //20160524, fauzil, TRBST16240, begin
            }
        }
        //20090910, David, SYARIAH001, end

        public void btnRefresh()
        {
            //20121023, hermanto_salim, BAALN12003, begin
            strFlag = "";
            //20121023, hermanto_salim, BAALN12003, end            
            controlsEnabled(false);
            controlsClear(true);
            //20121023, hermanto_salim, BAALN12003, begin
            //strFlag = "";
            //20121023, hermanto_salim, BAALN12003, end            
            txtTaxOnCapitalGainLoss.Visible = true;
            lblTaxOnCapitalGainLoss.Visible = true;
            cmbSafeKeepingFeeAfterTax.Visible = true;
            lblSafeKeepingFee.Visible = true;
            txtPajakBungaBerjalan.Visible = true;
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            lblTaxOnAccrued.Visible = true;
            //20231227, rezakahfi, BONDRETAIL-1513, end
            //20090910, David, SYARIAH001, begin
            //enabled object utk mencari deal number
            lblDealNumber.Visible = false;
            //20160524, fauzil, TRBST16240, begin
            //txtDealNumber.Visible = false;
            //btnCari.Visible = false;
            cmpsrGetPushBack.Visible = false;
            cmpsrGetPushBack.Text1 = "";
            cmpsrGetPushBack.Text2 = "";
            //20160524, fauzil, TRBST16240, end
            this.NISPToolbarButton("2").Visible = true;
            //20130107, victor, BAALN12003, begin
            //this.NISPToolbarButton("4").Visible = false;
            //20130107, victor, BAALN12003, end
            this.NISPToolbarButton("6").Visible = true;
            this.NISPToolbarButton("7").Visible = true;
            //20090910, David, SYARIAH001, end
            //20160822, samy,LOGEN196, begin
            iTaxAmnesty = false;
            //20160822, samy,LOGEN196, end
            //20160622, fauzil, TRBST16240, begin
            gbSumberDana.Visible = false;
            //20160622, fauzil, TRBST16240, end
            //20180116, samy, BOSOD18243, begin
            this.cmbSourceFund.SelectedIndex = 0;
            this.createTableSource();
            //20180116, samy, BOSOD18243, end
        }

        public void btnSave()
        {
            //20181102, vanny_w, BOSIT18196, begin
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            string strInput = cmpsrNoRekSecurity.Text1;
            //20230215, samypasha, BONDRETAIL-1241, begin
            //string Input = "where SecurityNo='" + cmpsrNomorSekuriti.Text1 + "'";
            //string SecurityNo = cmpsrNomorSekuriti.Text1;
            string Input = "where SecurityNo='" + cmpsrNomorSekuriti._Text1.Text.ToString() + "'";
            string SecurityNo = cmpsrNomorSekuriti._Text1.Text.ToString();
            //20230215, samypasha, BONDRETAIL-1241, end
            DataSet ds = SuratBerharga.findSecAccNo(strInput, SecurityNo);
            //if (ds.Tables[0].Rows[0]["Email"] == System.DBNull.Value || ds.Tables[0].Rows[0]["Email"].ToString().Trim() == "")
            //{
            //    MessageBox.Show("Mohon untuk melakukan pengkinian data alamat email di Pro CIF dan di tab Nasabah2 Pro Obligasi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}
            //20181102, vanny_w, BOSIT18196, end
			//20171006, agireza, COPOD17271, begin
            if (isNeedRecalculate)
            {
                MessageBox.Show("Mohon lakukan kalkulasi ulang", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            //20171006, agireza, COPOD17271, end
            //20210309, rezakahfi, BONDRETAIL-703, begin
            if (chkOther.Checked)
            {
                if (txtTotalProceed.Value > decimal.Parse(this.txtTotalAmountSource.Text))
                {
                    MessageBox.Show("Mohon lakukan penyesuaian sumber dana"
                        +"\nTotal Proceed lebih besar dibandingkan total sumber dana"
                        , "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            //20210309, rezakahfi, BONDRETAIL-703, end
            //20180828, samypasha, LOGEN00665, begin
            decimal nProfit = 0;
            if (cmbJenisTransaksi.Text == "Buy")
            {
                //Profit Bank beli= (Harga Modal – Deal Price) x Nominal /100
                nProfit = (ndHargaModal.Value - moneyDealPrice.Value) * moneyFaceValue.Value / 100;
                if (nProfit < 0)
                {
                    MessageBox.Show("Transaksi ini tidak dapat disave dikarenakan profit transaksi minus.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (cmbJenisTransaksi.Text == "Sell")
            {
                //Profit Bank Jual= (Deal Price – Harga Modal) x Nominal /100
                nProfit = (moneyDealPrice.Value - ndHargaModal.Value) * moneyFaceValue.Value / 100;
                if (nProfit < 0)
                {
                    MessageBox.Show("Transaksi ini tidak dapat disave dikarenakan profit transaksi minus.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            if (nispSettlementDate.Value < nispDealDate.Value)
            {
                MessageBox.Show("Settlement date tidak bisa backdate !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int currentDate = int.Parse(System.DateTime.Now.ToString("yyyyMMdd"));
            if (nispDealDate.Value < currentDate)
            {
                if (MessageBox.Show("Tanggal transaksi lebih kecil daripada tanggal hari ini (backdate) ... Lanjutkan ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;
            }


            if (nispSettlementDate.Value == nispDealDate.Value)
            {
                if (MessageBox.Show("Settlement date sama dengan transaction date ... Lanjutkan ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;
            }
            //20231227, rezakahfi, BONDRETAIL-1513, end
            //20180828, samypasha, LOGEN00665, end
            if (strFlag == "Insert")
            {
                //20160720, fauzil, TRBST16240, begin
                //Cursor.Current = Cursors.WaitCursor;
                //insertMethod();
                //Cursor.Current = Cursors.Default;
                //20210813, rezakahfi, BONDRETAIL-799, begin
//20231227, rezakahfi, BONDRETAIL-1513, begin
                if (!isTA)
                {
//20231227, rezakahfi, BONDRETAIL-1513, end
                    if (!CekHargaModal())
                        return;
                }
                else
                {
                    if (!ValidateProduct())
                        return;
//20231227, rezakahfi, BONDRETAIL-1513, begin
                }
//20231227, rezakahfi, BONDRETAIL-1513, end
                //20210813, rezakahfi, BONDRETAIL-799, end
                if (MessageBox.Show("Transaksi tidak dapat dibatalkan. Apakah detail transaksi sudah benar?", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    insertMethod();
                    Cursor.Current = Cursors.Default;
                }
                //20160720, fauzil, TRBST16240, end

            }
            //20090910, David, SYARIAH001, begin
            else if (strFlag == "Update")
            {
                if (MessageBox.Show("Transaksi tidak dapat dibatalkan. Apakah detail transaksi sudah benar?", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    updateMethod();
                    Cursor.Current = Cursors.Default;
                }
            }
            //20090910, David, SYARIAH001, end
            else if (strFlag == "")
            {
                MessageBox.Show("Anda Belum Melakukan Transaksi", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        //20090910, David, SYARIAH001, begin
        private void btnCari_Click(object sender, EventArgs e)
        {
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            DataSet dsTrx;
            bool getTrx = SuratBerharga.getTrxForAmend(txtDealNumber.Text.Trim(), 1, out dsTrx);
            if (getTrx == true)
            {
                //20120802, hermanto_salim, BAALN12003, begin
                dtAmendTransaksi = dsTrx.Tables[0];
                //20120802, hermanto_salim, BAALN12003, end
                cmpsrNomorSekuriti.Text1 = dsTrx.Tables[0].Rows[0]["SecurityNo"].ToString().Trim();
                cmpsrNomorSekuriti.ValidateField();

                cmbJenisTransaksi.SelectedValue = dsTrx.Tables[0].Rows[0]["TrxType"].ToString().Trim();

                cmpsrNoRekSecurity.Text1 = dsTrx.Tables[0].Rows[0]["SecAccNo"].ToString().Trim();
                cmpsrNoRekSecurity.ValidateField();
                txtNamaNasabah.Text = dsTrx.Tables[0].Rows[0]["Nama"].ToString().Trim();
                txtCouponRate.Text = dsTrx.Tables[0].Rows[0]["CouponRate"].ToString();
                nispDealDate.Value = int.Parse(dsTrx.Tables[0].Rows[0]["TrxDate"].ToString().Trim());
                nispSettlementDate.Value = int.Parse(dsTrx.Tables[0].Rows[0]["SettlementDate"].ToString().Trim());
                txtAccruedDays.Text = dsTrx.Tables[0].Rows[0]["AccruedDays"].ToString().Trim();
                moneyFaceValue.Text = dsTrx.Tables[0].Rows[0]["FaceValue"].ToString().Trim();
                moneyDealPrice.Text = dsTrx.Tables[0].Rows[0]["DealPrice"].ToString().Trim();
                txtTaxTarif.Text = dsTrx.Tables[0].Rows[0]["Tax"].ToString().Trim();
                txtAccruedInterest.Text = dsTrx.Tables[0].Rows[0]["AccruedInterest"].ToString().Trim();
                txtProceed.Text = dsTrx.Tables[0].Rows[0]["Proceed"].ToString().Trim();
                txtPajakBungaBerjalan.Text = dsTrx.Tables[0].Rows[0]["TaxOnAccrued"].ToString().Trim();
                txtTaxOnCapitalGainLoss.Text = dsTrx.Tables[0].Rows[0]["TaxOnCapitalGainLoss"].ToString().Trim();
                
                //20220208, darul.wahid, BONDRETAIL-895, begin
                this.dCapitalGain = dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim());
                //20220208, darul.wahid, BONDRETAIL-895, end
                //20220708, darul.wahid, BONDRETAIL-977, begin
                this.dCapitalGainNonIDR = dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim());
                this.dTotalTax = dsTrx.Tables[0].Rows[0]["TotalTax"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim());
                this.dIncome = dsTrx.Tables[0].Rows[0]["Income"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim());
                //20220708, darul.wahid, BONDRETAIL-977, end
                //20220920, yudha.n, BONDRETAIL-1052, begin
                this.dMateraiCost = dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString().Trim());
                //20220920, yudha.n, BONDRETAIL-1052, end
                //20240422, alfian.andhika, BONDRETAIL-1581, begin
                this.dYieldHargaModal = dsTrx.Tables[0].Rows[0]["YieldHargaModal"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["YieldHargaModal"].ToString().Trim());
                //20240422, alfian.andhika, BONDRETAIL-1581, end

                cmbSafeKeepingFeeAfterTax.Items.Clear();
                cmbSafeKeepingFeeAfterTax.Items.Add(0);
                cmbSafeKeepingFeeAfterTax.Items.Add(dsTrx.Tables[0].Rows[0]["SafeKeepingFeeTaxTarif"].ToString().Trim());
                cmbSafeKeepingFeeAfterTax.Text = dsTrx.Tables[0].Rows[0]["SafeKeepingFeeTaxTarif"].ToString().Trim();

                //20121127, hermanto_salim, BAALN12003, begin
                decimal transactionFee = iTransactionFee = dsTrx.Tables[0].Rows[0]["TransactionFeeAmount"].ToString() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["TransactionFeeAmount"].ToString());
                //20121127, hermanto_salim, BAALN12003, end
                cmbTrxFee.Items.Clear();
                cmbTrxFee.Items.Add(0);
                //20121127, hermanto_salim, BAALN12003, begin
                cmbTrxFee.Items.Add(transactionFee);
                chkTransactionFee.Checked = transactionFee != 0;
                subChangeTransactionFee();
                //20121127, hermanto_salim, BAALN12003, end
                //20120802, hermanto_salim, BAALN12003, begin
                //cmbTrxFee.Items.Add(dsTrx.Tables[0].Rows[0]["TransactionFee"].ToString().Trim());
                //cmbTrxFee.Text = dsTrx.Tables[0].Rows[0]["TransactionFee"].ToString().Trim();
                //20120802, hermanto_salim, BAALN12003, end

                txtTotalProceed.Text = dsTrx.Tables[0].Rows[0]["TotalProceed"].ToString().Trim();
                //20220920, yudha.n, BONDRETAIL-1052, begin
                txtMateraiCost.Value = dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString());
                //20220920, yudha.n, BONDRETAIL-1052, begin
                //20120802, hermanto_salim, BAALN12003, begin
                //cmpsrNIK.Text1 = dsTrx.Tables[0].Rows[0]["NIK_CS"].ToString().Trim();
                //cmpsrNIK.ValidateField();
                cmpsrSeller.Text1 = dsTrx.Tables[0].Rows[0]["NIK_CS"].ToString().Trim();
                cmpsrSeller.ValidateField();
                //20120802, hermanto_salim, BAALN12003, end
                cmpsrDealer.Text1 = dsTrx.Tables[0].Rows[0]["NIK_Dealer"].ToString().Trim();
                cmpsrDealer.ValidateField();
                //20160323, Junius, LOGEN00100, begin
                cmpsrNIKRef.Text1 = dsTrx.Tables[0].Rows[0]["NIK_Ref"].ToString().Trim();
                cmpsrNIKRef.ValidateField();
                //20160323, Junius, LOGEN00100, end

                //20121127, hermanto_salim, BAALN12003, begin
                strFlag = "Search";
                //20121127, hermanto_salim, BAALN12003, end
                controlsEnabled(false);
                //20160307, fauzil, TRBST16240, begin
                ////20121127, hermanto_salim, BAALN12003, begin
                //strFlag = "Insert";
                ////20121127, hermanto_salim, BAALN12003, end                
                //20160307, fauzil, TRBST16240, end
                cmpsrNomorSekuriti.Enabled = false;
                cmpsrSearch1.Enabled = false;
                cmbJenisTransaksi.Enabled = false;
                //20130307, uzia, BAFEM12016, begin
                chkFlagPhoneOrder.Checked = bool.Parse(dsTrx.Tables[0].Rows[0]["PhoneOrderBit"].ToString());
                //20190715, darul.wahid, BOSIT18196, begin
                //chkFlagPhoneOrder.Enabled = false;                
                DataSet dss;
                bool isPremier = false;
                bool isHavingPhoneOrder = false;
                string strInput = cmpsrNoRekSecurity.Text1;
                //20230215, samypasha, BONDRETAIL-1241, begin
                //string SecurityNo = cmpsrNomorSekuriti.Text1;
                string SecurityNo = cmpsrNomorSekuriti._Text1.Text.ToString();
                //20230215, samypasha, BONDRETAIL-1241, end

                dss = SuratBerharga.findSecAccNo(strInput, SecurityNo);
                CIFNo = dss.Tables[0].Rows[0]["CIFNo"].ToString();
                ValidatePhoneOrderFlag(out isPremier, out isHavingPhoneOrder);

                if (isHavingPhoneOrder || isPremier)
                    chkFlagPhoneOrder.Enabled = true;
                else
                    chkFlagPhoneOrder.Enabled = false;

                if (!chkFlagPhoneOrder.Enabled)
                    chkFlagPhoneOrder.Checked = false;
                //20190715, darul.wahid, BOSIT18196, end
                //20130307, uzia, BAFEM12016, end
                //20160211, samy, TRBST16240, begin
                ndHargaPublish.Text = dsTrx.Tables[0].Rows[0]["HargaORI"].ToString().Trim();
                ndHargaModal.Text = dsTrx.Tables[0].Rows[0]["HargaModal"].ToString().Trim();
                //20160211, samy, TRBST16240, end
                //20160307, fauzil, TRBST16240, begin
                //20200917, rezakahfi, BONDRETAIL-550, begin
                //GetDataDealIdForSwitching();
                //if (dsTrx.Tables[0].Rows[0]["FlagSwitching"].GetType() != typeof(DBNull))
                //    chkSwitching.Checked = bool.Parse(dsTrx.Tables[0].Rows[0]["FlagSwitching"].ToString());
                //if (dsTrx.Tables[0].Rows[0]["DealIdSwitching"].GetType() != typeof(DBNull))
                //    cmbDealIdSwitching.SelectedValue = dsTrx.Tables[0].Rows[0]["DealIdSwitching"].ToString().Trim();
                //20200917, rezakahfi, BONDRETAIL-550, end
                if (dsTrx.Tables[0].Rows[0]["FlagOther"].GetType() != typeof(DBNull))
                    chkOther.Checked = bool.Parse(dsTrx.Tables[0].Rows[0]["FlagOther"].ToString());

                ndHargaPublish.Enabled = false;
                ndHargaModal.Enabled = false;
                //20200917, rezakahfi, BONDRETAIL-550, begin
                //chkSwitching.Enabled = false;
                //cmbDealIdSwitching.Enabled = false;
                //20200917, rezakahfi, BONDRETAIL-550, end
                chkOther.Enabled = false;
                cmpsrNomorSekuriti.Enabled = false;
                cmpsrSearch1.Enabled = false;
                cmbJenisTransaksi.Enabled = false;

                if (chkOther.Checked)
                {
                    flagClear = true;
                    gbSumberDana.Visible = true;
                    //20210309, rezakahfi, BONDRETAIL-703, begin
                    //gbSDCntrl.Enabled = false;
                    gbSDCntrl.Enabled = true;
                    //20210309, rezakahfi, BONDRETAIL-703, end
                    //20190116, samypasha, BOSOD18243, begin
                    //gbSDData.Enabled = true;
                    //btnRemove.Enabled = false;
                    //20190116, samypasha, BOSOD18243, end
                    DataSet dsResult = new DataSet();
                    dsResult = SuratBerharga.GetSecurityTransactionSumberDana(long.Parse(txtDealNumber.Text.Trim()));
                    //20190116, samypasha, BOSOD18243, begin
                    //if (dsResult.Tables.Count > 0)
                    //{
                    //    if (dgvSumberDana.Columns.Count > 0)
                    //    {
                    //        dgvSumberDana.Columns.Remove("SumberDana");
                    //        dgvSumberDana.Columns.Remove("Tanggal");
                    //    }
                    //    dgvSumberDana.DataSource = dsResult.Tables[0];
                    //}
                    if (dsResult.Tables.Count > 0)
                    {
                        dgvSourceFund.DataSource = dsResult.Tables[0];
                    }
                    //20190116, samypasha, BOSOD18243, end
                }
                else
                    gbSumberDana.Visible = false;

                if (int.Parse(cmbJenisTransaksi.SelectedValue.ToString()) == 3) //bank beli
                {
                    //tampilkan data link agar bisa diupdate nominal jualnya
                    if (cmbJenisTransaksi.Text == "Buy")
                    {
                        if (dgvTransactionLink.DataSource == null)
                        {
                            DataSet dsResult = new DataSet();
                            lastSecAccNo = cmpsrNoRekSecurity.Text1;
                            //20230215, samypasha, BONDRETAIL-1241, begin
                            //lastSecurityNo = cmpsrNomorSekuriti.Text1;
                            lastSecurityNo = cmpsrNomorSekuriti._Text1.Text.ToString();
                            //20230215, samypasha, BONDRETAIL-1241, end
                            if (clsDatabase.subTRSPopulateTransactionBalanceForUpdate(cQuery, new object[] { txtDealNumber.Text.Trim() }, out dsResult))
                            {
                                dgvTransactionLink.DataSource = dsResult.Tables[0];

                                for (int i = 0; i < dgvTransactionLink.ColumnCount; i++)
                                    dgvTransactionLink.Columns[i].Visible = false;

                                for (int i = 0; i < dgvTransactionLinkColumnProperty.Rows.Count; i++)
                                {
                                    string columnName = (string)dgvTransactionLinkColumnProperty.Rows[i]["ColumnName"];
                                    dgvTransactionLink.Columns[columnName].ReadOnly = (bool)dgvTransactionLinkColumnProperty.Rows[i]["IsReadOnly"];
                                    dgvTransactionLink.Columns[columnName].Frozen = (bool)dgvTransactionLinkColumnProperty.Rows[i]["IsFrozen"];
                                    if (dgvTransactionLink.Columns[columnName].Name == "SelectBit")
                                        dgvTransactionLink.Columns[columnName].Visible = false;
                                    //20221223, yazri, VSYARIAH-340, begin
                                    else if (dgvTransactionLink.Columns[columnName].Name == "NoRekening")
                                        dgvTransactionLink.Columns[columnName].Visible = false;
                                    //20221223, yazri, VSYARIAH-340, end
                                    else
                                        dgvTransactionLink.Columns[columnName].Visible = true;
                                    dgvTransactionLink.Columns[columnName].HeaderText = (string)dgvTransactionLinkColumnProperty.Rows[i]["ColumnAlias"];
                                }
                            }
                        }
                    }
                    else
                    {
                        dgvTransactionLink.DataSource = null;
                    }
                }
                else
                { //bank jual 
                    //20200917, rezakahfi, BONDRETAIL-550, begin
                    //if (chkSwitching.Checked)
                    //{
                    //    lblCmbSwitching.Visible = true;
                    //    cmbDealIdSwitching.Visible = true;
                    //}
                    //else
                    //{
                    //    lblCmbSwitching.Visible = false;
                    //    cmbDealIdSwitching.Visible = false;
                    //}
                    //20200917, rezakahfi, BONDRETAIL-550, end

                    chkOther.Visible = true;
                }

                btnCalculate.Enabled = true;
                moneyDealPrice.Enabled = true;
                cmpsrSeller.Enabled = true;
                if (dsTrx.Tables[0].Rows[0]["Functional"].GetType() != typeof(DBNull))
                    txtFuncGrp.Text = dsTrx.Tables[0].Rows[0]["Functional"].ToString();
                this.NISPToolbarButton("6").Visible = true;
                ClearDataForCalculate();
                strFlag = "Update";
                //20160307, fauzil, TRBST16240, end
            }
            else
            {
                MessageBox.Show("Gagal mengambil data transaksi", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDealNumber.Focus();
            }
        }
        //20090910, David, SYARIAH001, end
        public void insertMethod()
        {
            //20230725, yudha.n, BONDRETAIL-1398, begin
            string PODealId = "";
            //20230725, yudha.n, BONDRETAIL-1398, end
            try
            {
                TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
                //20231227, rezakahfi, BONDRETAIL-1513, begin
                TransaksiSuratBerhargaTA SuratBerhargaTA = new TransaksiSuratBerhargaTA();
                //20231227, rezakahfi, BONDRETAIL-1513, end
                DataSet dsSecId = new DataSet();
                //20160301, fauzil, TRBST16240, begin
                RegistrasiNasabah Nasabah = new RegistrasiNasabah();
                bool isBookBld = false;
                bool isNeedWMApp = false;
                //20160301, fauzil, TRBST15176, end
                //20200917, rezakahfi, BONDRETAIL-550, begin
                decimal dcAmountBlock = txtTotalProceed.Value;
                //20200917, rezakahfi, BONDRETAIL-550, end
                //20220920, yudha.n, BONDRETAIL-1052, begin
                string DealId = "0";
                int BlockSequenceBond = 0;
                int BlockSequenceMaterai = 0;
                decimal dcMateraiAmountBlock = txtMateraiCost.Value;
                //20220920, yudha.n, BONDRETAIL-1052, end

                //20171109, agireza, TRBST16240, begin
                //20180213, uzia, LOGEN00584, begin                
                //if (_bIsCorporateBond && _strSpouseName == "") 
                //{
                //    MessageBox.Show("Khusus produk coorporate bond, transaksi hanya bisa dilakukan apabila memiliki pasangan", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                if (cmbJenisTransaksi.Text == "Sell")
                {
                    if (this._bIsCorporateBond && this._strSpouseName.Equals("") && this._strMaritalStatus.Equals("A"))
                    {
                        //20180219, uzia, LOGEN00584, begin
                        //MessageBox.Show("Khusus produk coorporate bond, transaksi sell hanya bisa dilakukan apabila memiliki pasangan", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        MessageBox.Show("Khusus untuk transaksi nasabah beli obligasi korporasi (corporate bond), untuk nasabah yang telah menikah harus mengisi data nama pasangan", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //20180219, uzia, LOGEN00584, end
                        return;
                    }
                }
                //20180213, uzia, LOGEN00584, end
                //20171109, agireza, TRBST16240, end

                //20220920, yudha.n, BONDRETAIL-1052, begin
                if (!isMeteraiAbsorbed && dMateraiCost != 0 && cbRekeningMaterai.SelectedIndex < 1)
                {
                    MessageBox.Show("Rekening Biaya Meterai Masih kosong !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20220920, yudha.n, BONDRETAIL-1052, end

                //20160815, fauzil, LOGEN196, begin
                if (cmbJenisTransaksi.Text == "Sell")
                //20160301, fauzil, TRBST16240, begin
                {
                    //20160301, fauzil, TRBST16240, end                
                        if (cbRekeningRelasi.SelectedIndex < 1)
                        {
                            MessageBox.Show("Rekening Relasi Masih kosong !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    //20160301, fauzil, TRBST16240, begin
                        if (moneyDealPrice.Value < ndHargaModal.Value)
                        {
                            if (MessageBox.Show("Apakah transaksi ini sudah mendapatkan harga spesial dari Treasury?", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                return;
                        }
                }
                else
                {
                    if (moneyDealPrice.Value > ndHargaModal.Value)
                    {
                        if (MessageBox.Show("Apakah transaksi ini sudah mendapatkan harga spesial dari Treasury?", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            return;
                    }
                //20230106, yazri, VSYARIAH-340, begin 
                    if (cbRekeningRelasi.SelectedIndex < 1)
                    {
                        MessageBox.Show("Rekening Relasi Masih kosong !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                //20230106, yazri, VSYARIAH-340, begin
                }
                //20160301, fauzil, TRBST16240, end
                //20160815, fauzil, LOGEN196, end
                //20130305, uzia, BAFEM12016, begin
                if (!SavePhoneOrderValidation())
                    return;
                //20130305, uzia, BAFEM12016, end
                if (cmpsrNomorSekuriti.Text1.Length != 0 && cmpsrSearch1.Text1.Length != 0 && cmpsrNoRekSecurity.Text1.Length != 0
                    && nispSettlementDate.Text.Length != 0 && nispDealDate.Text.Length != 0 && txtAccruedDays.Text.Length != 0
                    && moneyFaceValue.Value != 0 && txtAccruedInterest.Text.Length != 0 && txtProceed.Value != 0
                    && moneyDealPrice.Text.Length != 0 && //MoneyTransactionFee.Value != 0 && 
                    //20120802, hermanto_salim, BAALN12003, begin
                    //txtTotalProceed.Value != 0 && cmpsrNIK.Text1 != "" && cmpsrDealer.Text1 != "")
                    txtTotalProceed.Value != 0 && cmpsrSeller.Text1 != "" && cmpsrDealer.Text1 != "")
                //20120802, hermanto_salim, BAALN12003, end
                {
                    //bool bCalculate = false;
                    //Calculate(out bCalculate);
                    //if(bCalculate == true)
                    //{
                    //20161021, fauzil, TRBST16240, begin
                    // cek harga modal
                    string errMsg = "";
                    decimal HargaOri = 0;
                    decimal HargaModal = 0;
                    ValidateHargaModal(out errMsg, out HargaOri, out HargaModal);
                    if (errMsg.Length > 0)
                    {
                        MessageBox.Show(errMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        PopulateHarga();

                        //clean value after calculate   
                        txtAccruedInterest.Value = 0;
                        txtProceed.Value = 0;
                        txtPajakBungaBerjalan.Value = 0;
                        cmbSafeKeepingFeeAfterTax.Items.Clear();
                        cmbSafeKeepingFeeAfterTax.Items.Add(0);
                        cmbTrxFee.Items.Clear();
                        cmbTrxFee.Items.Add(0);
                        txtTotalProceed.Value = 0;
                        txtTaxOnCapitalGainLoss.Value = 0;
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        this.dCapitalGain = 0;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        //20220708, darul.wahid, BONDRETAIL-977, begin
                        this.dCapitalGainNonIDR = 0;
                        this.dTotalTax = 0;
                        this.dIncome = 0;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        this.dMateraiCost = 0;
                        txtMateraiCost.Value = 0;
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        dYieldHargaModal = 0;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                        return;
                    }
                    //Validate Settlement Date
                    string dSettleTrx = nispSettlementDate.Text;
                    string dSettleNew = "";
                    errMsg = "";
                    //20220331, darul.wahid, ONEMBL-1279, begin
                    //ValidateSettlementDate(cmpsrNomorSekuriti.Text1, dSettleTrx, out errMsg, out dSettleNew);
                    int JenisTrx = int.Parse(cmbJenisTransaksi.SelectedValue.ToString());
                    //20230215, samypasha, BONDRETAIL-1241, begin
                    //ValidateSettlementDate(cmpsrNomorSekuriti.Text1, dSettleTrx, JenisTrx, "CB", out errMsg, out dSettleNew);
                    //20231227, rezakahfi, BONDRETAIL-1513, begin
                    if (!isTA)
                    {
                        ValidateSettlementDate(cmpsrNomorSekuriti._Text1.Text.ToString(), dSettleTrx, JenisTrx, "CB", out errMsg, out dSettleNew);
                    }
                    //20231227, rezakahfi, BONDRETAIL-1513, end
                    //20230215, samypasha, BONDRETAIL-1241, end
                    //20220331, darul.wahid, ONEMBL-1279, end
                    if (errMsg.Length > 0)
                    {
                        MessageBox.Show(errMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nispSettlementDate.Text = dSettleNew;
                        //clean value after calculate   
                        txtAccruedInterest.Value = 0;
                        txtProceed.Value = 0;
                        txtPajakBungaBerjalan.Value = 0;
                        cmbSafeKeepingFeeAfterTax.Items.Clear();
                        cmbSafeKeepingFeeAfterTax.Items.Add(0);
                        cmbTrxFee.Items.Clear();
                        cmbTrxFee.Items.Add(0);
                        txtTotalProceed.Value = 0;
                        txtTaxOnCapitalGainLoss.Value = 0;
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        this.dCapitalGain = 0;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        //20220708, darul.wahid, BONDRETAIL-977, begin
                        this.dCapitalGainNonIDR = 0;
                        this.dTotalTax = 0;
                        this.dIncome = 0;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        dYieldHargaModal = 0;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                        return;
                    }
                    //20161021, fauzil, TRBST16240, end
                    //20120802, hermanto_salim, BAALN12003, begin
                    if (!subAdditionalValidate())
                        return;

                    string xmlTransactionLink = null;
                    string key = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                    if (cmbJenisTransaksi.Text == "Buy")
                    {
                        DataTable dtToXML = (dgvTransactionLink.DataSource as DataTable).Clone();
                        dtToXML.TableName = "SecurityTransactionLink_TR";
                        //20161208, fauzil, TRBST16240, begin
                        //for (int i = 0; i < dgvTransactionLink.RowCount; i++)
                        //{
                        //    if ((bool)dgvTransactionLink.Rows[i].Cells["SelectBit"].Value)
                        //    {
                        //        DataRow selectedRow = (dgvTransactionLink.Rows[i].DataBoundItem as DataRowView).Row;
                        //        selectedRow.AcceptChanges();
                        //        dtToXML.ImportRow(selectedRow);
                        //    }
                        //}
                        dtToXML.Columns.Add("TransactionFeeAmount");
                        dtToXML.Columns.Add("Proceed");
                        dtToXML.Columns.Add("AccruedDays");
                        dtToXML.Columns.Add("AccruedInterest");
                        dtToXML.Columns.Add("TaxOnAccrued");
                        dtToXML.Columns.Add("TaxOnCapitalGainLoss");
                        dtToXML.Columns.Add("SafeKeepingFeeAfterTax");
                        dtToXML.Columns.Add("TotalProceed");

                        decimal? trxFee = chkTransactionFee.Checked ? 0 : (decimal?)null;

                        foreach (DataGridViewRow row in dgvTransactionLink.Rows)
                        {
                            if ((bool)row.Cells["SelectBit"].Value)
                            {
                                DataTable dtToXMLCalculate = (dgvTransactionLink.DataSource as DataTable).Clone();
                                dtToXMLCalculate.TableName = "SecurityTransactionLink_TR";
                                DataRow dRow = dtToXML.NewRow();
                                DataRow dCRow = dtToXMLCalculate.NewRow();
                                DataSet dsData = new DataSet();
                                string xmlTransactionLinkInner = null;
                                foreach (DataGridViewCell cell in row.Cells)
                                {
                                    dRow[cell.ColumnIndex] = cell.Value;
                                    dCRow[cell.ColumnIndex] = cell.Value;
                                }
                                dtToXMLCalculate.Rows.Add(dCRow);
                                StringBuilder stringBuilderInner = new StringBuilder();
                                dtToXMLCalculate.WriteXml(System.Xml.XmlWriter.Create(stringBuilderInner));
                                xmlTransactionLinkInner = stringBuilderInner.ToString();
                                decimal FaceValue = decimal.Parse(dRow["NominalJual"].ToString());
                                //20220920, yudha.n, BONDRETAIL-1052, begin
                                //dsData = SuratBerharga.calculateORI2(this.cQuery, Convert.ToInt32(cmbJenisTransaksi.SelectedValue), CIFid, iSecId, nispSettlementDate.Value, FaceValue, decimal.Parse(moneyDealPrice.Text.ToString()), decimal.Parse(txtTaxTarif.Text.ToString()), trxFee, xmlTransactionLinkInner);
                                //20240715,pratama,BONDRETAIL-1392, begin
                                long pushBackDealId = 0;
                                Int64.TryParse(cmpsrGetPushBack.Text1.Trim(), out pushBackDealId);
                                //dsData = SuratBerharga.calculateORI2(this.cQuery, Convert.ToInt32(cmbJenisTransaksi.SelectedValue), CIFid, iSecId, nispSettlementDate.Value, FaceValue, decimal.Parse(moneyDealPrice.Text.ToString()), decimal.Parse(txtTaxTarif.Text.ToString()), trxFee, xmlTransactionLinkInner, "BOND");
                                dsData = SuratBerharga.calculateORI2(this.cQuery, Convert.ToInt32(cmbJenisTransaksi.SelectedValue), CIFid, iSecId, nispSettlementDate.Value, FaceValue, decimal.Parse(moneyDealPrice.Text.ToString()), decimal.Parse(txtTaxTarif.Text.ToString()), trxFee, xmlTransactionLinkInner, "BOND", pushBackDealId);
                                //20240715,pratama,BONDRETAIL-1392, end
                                //20220920, yudha.n, BONDRETAIL-1052, end
                                dRow["TransactionFeeAmount"] = dsData.Tables[0].Rows[0]["TransactionFee"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TransactionFee"].ToString());
                                dRow["Proceed"] = dsData.Tables[0].Rows[0]["Proceed"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["Proceed"].ToString());
                                dRow["AccruedDays"] = dsData.Tables[0].Rows[0]["AccruedDays"].ToString() == "" ? Convert.ToDecimal(0) : decimal.Parse(dsData.Tables[0].Rows[0]["AccruedDays"].ToString());
                                dRow["AccruedInterest"] = dsData.Tables[0].Rows[0]["Interest"].ToString() == "" ? Convert.ToDecimal(0) : decimal.Parse(dsData.Tables[0].Rows[0]["Interest"].ToString());
                                dRow["TaxOnAccrued"] = dsData.Tables[0].Rows[0]["TaxOnAccrued"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TaxOnAccrued"].ToString());
                                dRow["TaxOnCapitalGainLoss"] = dsData.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString());
                                dRow["SafeKeepingFeeAfterTax"] = dsData.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString());
                                dRow["TotalProceed"] = dsData.Tables[0].Rows[0]["TotalProceed"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TotalProceed"].ToString());
                                dtToXML.Rows.Add(dRow);
                            }
                        }
                        //20161208, fauzil, TRBST16240, end

                        StringBuilder stringBuilder = new StringBuilder();
                        dtToXML.WriteXml(System.Xml.XmlWriter.Create(stringBuilder));
                        xmlTransactionLink = stringBuilder.ToString();
                        //20160622, fauzil, TRBST16240, begin
                        chkOther.Checked = false;
                        //20200917, rezakahfi, BONDRETAIL-550, begin
                        //chkSwitching.Checked = false;
                        //20200917, rezakahfi, BONDRETAIL-550, end
                        //20160622, fauzil, TRBST16240, end
                    }
                    //20160622, fauzil, TRBST16240, begin
                    string xmlSumberDana = null;
                    if (cmbJenisTransaksi.Text == "Sell")
                    {
                        if (chkOther.Checked)
                        {
                            //20190116, samypasha, BOSOD18243, begin
                            //if (dgvSumberDana.Rows.Count > 0)
                            //{
                            //    DataTable dtToXML = new DataTable();
                            //    dtToXML.TableName = "SecurityTransaction_SumberDana";
                            //    foreach (DataGridViewColumn col in dgvSumberDana.Columns)
                            //    {
                            //        dtToXML.Columns.Add(col.Name);
                            //    }
                            //    foreach (DataGridViewRow row in dgvSumberDana.Rows)
                            //    {
                            //        DataRow dRow = dtToXML.NewRow();
                            //        foreach (DataGridViewCell cell in row.Cells)
                            //        {
                            //            dRow[cell.ColumnIndex] = cell.Value;
                            //        }
                            //        dtToXML.Rows.Add(dRow);
                            //    }
                            //    StringBuilder stringBuilder = new StringBuilder();
                            //    dtToXML.WriteXml(System.Xml.XmlWriter.Create(stringBuilder));
                            //    xmlSumberDana = stringBuilder.ToString();
                            //}
                            //20190528, samypasha, BOSOD18243, begin
                            if (txtTotalProceed.Value < decimal.Parse(txtTotalAmountSource.Text))
                            {
                                MessageBox.Show("Total Proceed lebih kecil dari jumlah sumber dana.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            if (decimal.Parse(txtTotalAmountSource.Text) < txtTotalProceed.Value)
                            {
                                MessageBox.Show("Dana dari sumber dana masih kurang, silakan tambah sumber dana.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            //20190528, samypasha, BOSOD18243, end
                            if (dgvSourceFund.Rows.Count > 0)
                            {
                                DataTable dtToXML = new DataTable();
                                dtToXML.TableName = "Source";
                                foreach (DataGridViewColumn col in dgvSourceFund.Columns)
                                {
                                    dtToXML.Columns.Add(col.Name);
                                }
                                foreach (DataGridViewRow row in dgvSourceFund.Rows)
                                {
                                    DataRow dRow = dtToXML.NewRow();
                                    foreach (DataGridViewCell cell in row.Cells)
                                    {
                                        dRow[cell.ColumnIndex] = cell.Value;
                                    }
                                    dtToXML.Rows.Add(dRow);
                                }

                                DataSet dstSumberDana = new DataSet();
                                dstSumberDana.Tables.Add(dtToXML.Copy());

                                dstSumberDana.DataSetName = "Data";
                                dstSumberDana.Tables[0].TableName = "Source";
                                StringBuilder dataSave = new StringBuilder();

                                dstSumberDana.Tables[0].WriteXml(System.Xml.XmlWriter.Create(dataSave));
                                xmlSumberDana = dataSave.ToString();
                            }
                            //20190116, samypasha, BOSOD18243, end
                            else
                            {
                                MessageBox.Show("Sumber dana harus dipilih, jika Other Check Box tercentang", "Warning", MessageBoxButtons.OK);
                                return;
                            }
                        }
                    }
                    //20160622, fauzil, TRBST16240, end

                    //20120802, hermanto_salim, BAALN12003, end
                    DataSet ds = new DataSet();
                    DataSet dsTransaksi = new DataSet("Root");
                    DataTable dtTransaksi = new DataTable("RS");
                    ds = SuratBerharga.columnsTransaksi();
                    int iRowColum = ds.Tables[0].Rows.Count;

                    /* Add Column To Data Table*/
                    dsTransaksi.Tables.Add(dtTransaksi);
                    for (int i = 0; i < iRowColum; i++)
                    {
                        dtTransaksi.Columns.Add(ds.Tables[0].Rows[i][0].ToString());
                    }
                    dtTransaksi.Columns.Add("SecurityNo");
                    //20220920, yudha.n, BONDRETAIL-1052, begin
                    dtTransaksi.Columns.Add("NoRekMaterai");
                    dtTransaksi.Columns.Add("MateraiCost");
                    dtTransaksi.Columns.Add("IsAbsorbedByBank");

                    //20220920, yudha.n, BONDRETAIL-1052, end
                    //20231025,yazri, BONDRETAIL-1392, begin
                    dtTransaksi.Columns.Add("WeightedSpread");
                    dtTransaksi.Columns.Add("WeightedPrice");
                    dtTransaksi.Columns.Add("TotalSpread");
                    dtTransaksi.Columns.Add("UntungRugiNasabah");
                    dtTransaksi.Columns.Add("WeightedHoldingPeriod");
                    dtTransaksi.Columns.Add("Keterangan");
                    //20231025,yazri, BONDRETAIL-1392, end

                    /* inisialisasi */
                    int iType = Convert.ToInt32(cmbJenisTransaksi.SelectedValue);

                    //20230215, samypasha, BONDRETAIL-1241, begin
                    //string Input = "where SecurityNo='" + cmpsrNomorSekuriti.Text1 + "'";
                    string Input = "where SecurityNo='" + cmpsrNomorSekuriti._Text1.Text.ToString() + "'";
                    //20230215, samypasha, BONDRETAIL-1241, end
                    dsSecId = SuratBerharga.findSecId(Input);
                    //int iSecId = Int32.Parse(dsSecId.Tables[0].Rows[0]["SecId"].ToString());                   
                    //20170830, imelda, COPOD17271 , begin
                    _RiskProfileProduct = Convert.ToChar(dsSecId.Tables[0].Rows[0]["MinimumRiskProfile"]);
                    //20170830, imelda, COPOD17271 , end
                    //20160307, fauzil, TRBST16240, begin
                    // cek wewenang

                    bool NeedTreasuryApp = false;
                    string MessagePublishError = "";
                    string MessageModalError = "";

                    //20170830, imelda, COPOD17271 , begin
                    if (cmbJenisTransaksi.Text == "Sell")
                    {
                        if (_RiskProfileCust < _RiskProfileProduct)
                        {
                            if (MessageBox.Show("Product yang dipilih diatas ketentuan profile nasabah; Lanjutkan transaksi?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            {
                                controlsClear(true);
                                return;
                            }
                            else
                                MessageBox.Show("Pastikan kolom Profile Resiko nasabah di form pembelian/penjualan obligasi sudah ditandatangani nasabah", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    //20170830, imelda, COPOD17271 , end
                    //20231227, rezakahfi, BONDRETAIL-1513, begin
                    if (!isTA)
                    {
                        //20240216, pratama, BONDRETAIL-1392, begin
                        //if (SuratBerharga.checkWewenangDeviasi(iSecId, iType, dsSecId.Tables[0].Rows[0]["SecCcy"].ToString(), ndHargaModal.Value, ndHargaPublish.Value, moneyDealPrice.Value, moneyFaceValue.Value, chKaryawan.Checked, out MessagePublishError, out MessageModalError, out NeedTreasuryApp, out isNeedWMApp))
                        //{
                        //if (!string.IsNullOrEmpty(MessagePublishError))
                        //    MessageBox.Show(MessagePublishError, "WARNING!!", MessageBoxButtons.OK);
                        //if (!string.IsNullOrEmpty(MessageModalError))
                        //    MessageBox.Show(MessageModalError, "WARNING!!", MessageBoxButtons.OK);
                        //20180731, samypasha, LOGEN00665, begin
                        //if (_isPVB)
                        //{
                        //    isNeedWMApp = false;
                        //}
                        //20180731, samypasha, LOGEN00665, end
                        //if (isNeedWMApp)
                        //    MessageBox.Show("Transaksi ini tidak dapat di otorisasi oleh Supervisor cabang karena spread yang digunakan memerlukan approval dari Wealth Management", "WARNING!!", MessageBoxButtons.OK);
                        //}
                        //20240216, pratama, BONDRETAIL-1392, end
                    }
                    //20231227, rezakahfi, BONDRETAIL-1513, end
                    
                    // cari data trader
                    string SourceTrader = "";
                    string DestTrader = "";

                    SuratBerharga.GetDataTraderTRX(out SourceTrader, out DestTrader);
                    if (string.IsNullOrEmpty(SourceTrader))
                    {
                        MessageBox.Show("NIK Trader RM Kosong, data belum dapat disimpan", "WARNING!!", MessageBoxButtons.OK);
                        return;
                    }
                    if (string.IsNullOrEmpty(DestTrader))
                    {
                        MessageBox.Show("NIK Trader Destination RM Kosong, data belum dapat disimpan", "WARNING!!", MessageBoxButtons.OK);
                        return;
                    }
                    //20230215, samypasha, BONDRETAIL-1241, begin
                    //bool isValidDate = SuratBerharga.checkRecordingDate(cmpsrNomorSekuriti.Text1.Trim().ToUpper(), nispSettlementDate.Value);
                    bool isValidDate = SuratBerharga.checkRecordingDate(cmpsrNomorSekuriti._Text1.Text.ToString().Trim().ToUpper(), nispSettlementDate.Value);
                    //20230215, samypasha, BONDRETAIL-1241, end
                    if (isValidDate != true)
                    {
                        MessageBox.Show("Settlement Date tidak valid", "Peringatan");
                        nispSettlementDate.Text = "";
                        nispSettlementDate.Focus();
                        return;
                    }
                    //20160307, fauzil, TRBST16240, end
                    //20161018, fauzil, TRBST16240, begin
                    if (chkSwitching.Checked)
                    {
                        //if (cmbDealIdSwitching.SelectedIndex == -1)
                        //{
                        //    MessageBox.Show("Data Deal Id untuk Switching tidak boleh kosong", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //    return;
                        //}
                        string NoRekening = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                        //long NoSwitching = long.Parse(cmbDealIdSwitching.SelectedValue.ToString());
                        //string errMessage = "";
                        //SuratBerharga.CheckDiffTAandNonTA(NoRekening, NoSwitching, out errMessage);
                        //if (!string.IsNullOrEmpty(errMessage))
                        //{
                        //    MessageBox.Show("Error Data Switching " + errMessage, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //    return;
                        //}
                    }
                    //20161018, fauzil, TRBST16240, end

                    /* Format Inputan XML*/
                    System.Data.DataRow DetailRow = dtTransaksi.NewRow();

                    DetailRow["CIFId"] = CIFid;
                    DetailRow["SecId"] = iSecId;
                    DetailRow["TrxType"] = iType;
                    DetailRow["TrxDate"] = nispDealDate.Value;

                    DetailRow["SettlementDate"] = nispSettlementDate.Value;
                    DetailRow["Tenor"] = 0; //SBI
                    DetailRow["FaceValue"] = moneyFaceValue.Value;
                    DetailRow["Yield"] = 0; // SBI
                    DetailRow["DealYield"] = 0; // SBI
                    DetailRow["SpreadYield"] = 0; // SBI
                    DetailRow["TaxAmount"] = 0; // SBI
                    DetailRow["SafeKeepingFeeAmount"] = 0; // SBI
                    DetailRow["TransactionFeeAmount"] = cmbTrxFee.Text;
                    DetailRow["DealPrice"] = moneyDealPrice.Value;
                    DetailRow["Proceed"] = txtProceed.Value;
                    DetailRow["AccruedDays"] = Convert.ToInt16(txtAccruedDays.Value);
                    DetailRow["AccruedInterest"] = txtAccruedInterest.Value;
                    DetailRow["TaxOnAccrued"] = txtPajakBungaBerjalan.Value;
                    DetailRow["TaxOnCapitalGainLoss"] = txtTaxOnCapitalGainLoss.Value;
                    DetailRow["SafeKeepingFeeAfterTax"] = cmbSafeKeepingFeeAfterTax.Text;
                    DetailRow["TotalProceed"] = txtTotalProceed.Value;
                    DetailRow["TrxStatus"] = "0";
                    //20120802, hermanto_salim, BAALN12003, begin
                    //DetailRow["NIK_CS"] = cmpsrNIK.Text1; // nik cs
                    DetailRow["NIK_CS"] = cmpsrSeller.Text1; // nik cs
                    //20120802, hermanto_salim, BAALN12003, end
                    //tambah field NIK_Dealer
                    DetailRow["NIK_Dealer"] = cmpsrDealer.Text1; // NIK_Dealer
                    //end
                    DetailRow["TrxBranch"] = cmpsrSearch1.Text1;
                    DetailRow["InsertedBy"] = intNIK; // nik user login

                    DetailRow["InsertedDate"] = FormatDate.ConvertStandardDate(StandardDate.yyyymmdd, strWorkDate, Sparate.blank);
                    DetailRow["LastUpdateBy"] = null;
                    DetailRow["LastUpdateDate"] = null;
                    DetailRow["SecurityNo"] = dsSecId.Tables[0].Rows[0]["SecurityNo"].ToString();
                    //20130307, uzia, BAFEM12016, begin
                    DetailRow["PhoneOrderBit"] = chkFlagPhoneOrder.Checked;
                    //20130307, uzia, BAFEM12016, end
                    //20160323, Junius, LOGEN00100, begin
                    DetailRow["NIK_Ref"] = cmpsrNIKRef.Text1;
                    //20160323, Junius, LOGEN00100, end
                    //20160815, fauzil, LOGEN196, begin
                    if (cmbJenisTransaksi.Text == "Sell")
                    {
                        DetailRow["NoRekInvestor"] = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                        DetailRow["isTaxAmnesty"] = chkTaxAmnesty.Checked;
                    }
                    else
                    {
                        if (FaceValueTA > 0 && FaceValueNonTA > 0)
                        {
                            //20221223, yazri, VSYARIAH-340, begin
                            //DetailRow["NoRekInvestor"] = ""
                            DetailRow["NoRekInvestor"] = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                            //20221223, yazri, VSYARIAH-340, end
                            DetailRow["isTaxAmnesty"] = false;
                        }
                        else
                        {
                            if (FaceValueTA > 0)
                            {
                                DetailRow["NoRekInvestor"] = NoRekInvestorTA;
                                DetailRow["isTaxAmnesty"] = true;
                            }
                            else if (FaceValueNonTA > 0)
                            {
                                //20221223, yazri, VSYARIAH-340, begin
                                //DetailRow["NoRekInvestor"] = NoRekInvestorNonTA;
                                DetailRow["NoRekInvestor"] = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                                //20221223, yazri, VSYARIAH-340, end
                                DetailRow["isTaxAmnesty"] = false;
                            }
                        }
                    }
                    //20160815, fauzil, LOGEN196, end
                    //20160211, samy, TRBST16240, begin
                    DetailRow["HargaORI"] = ndHargaPublish.Value;
                    DetailRow["HargaModal"] = ndHargaModal.Value;
                    //20160211, samy, TRBST16240, end
                    //20160303, fauzil, TRBST16240, begin
                    //20200917, rezakahfi, BONDRETAIL-550, begin
                    //DetailRow["FlagSwitching"] = chkSwitching.Checked;
                    //DetailRow["DealIdSwitching"] = chkSwitching.Checked ? cmbDealIdSwitching.SelectedValue : "";
                    DataTable dtTable = (DataTable)dgvSourceFund.DataSource;
                    DataRow[] drRowObligasi = dtTable.Select("SourceData = 'Obligasi'");
                    string strDealIdSwitching = "";
                    if (drRowObligasi.Length > 0)
                    {
                        for (int i = 0; i < drRowObligasi.Length; i++)
                        {
                            if (i == 0)
                                strDealIdSwitching = drRowObligasi[0]["DealIdSource"].ToString();
                            else
                                strDealIdSwitching = strDealIdSwitching + ", " + drRowObligasi[i]["DealIdSource"].ToString();
                        }
                        DetailRow["DealIdSwitching"] = strDealIdSwitching;
                        DetailRow["FlagSwitching"] = true;
                    }
                    else
                    {
                        DetailRow["DealIdSwitching"] = "";
                        DetailRow["FlagSwitching"] = false;
                    }
                    
                    //20200917, rezakahfi, BONDRETAIL-550, end
                    DetailRow["FlagOther"] = chkOther.Checked;
                    DetailRow["BranchProfit"] = 0;
                    DetailRow["NeedTreasuryApp"] = NeedTreasuryApp;
                    DetailRow["NIKSourceTrader"] = SourceTrader;
                    DetailRow["NIKTraderRM"] = cmpsrDealer.Text1 + cmpsrDealer.Text2;
                    DetailRow["NikDestTraderRM"] = DestTrader;
                    if (!string.IsNullOrEmpty(MessagePublishError))
                        DetailRow["TreasuryAppMessage"] = MessagePublishError;
                    else if (!string.IsNullOrEmpty(MessageModalError))
                        DetailRow["TreasuryAppMessage"] = MessageModalError;
                    else
                        DetailRow["TreasuryAppMessage"] = "";
                    //20240219, pratama, BONDRETAIL-1392,begin
                    //keterangan == 1 ? "Transaksi dapat langsung diproses tanpa approval WM" : "Transaksi ini memerlukan approval wm karena spread melebihi max spread yang diperbolehkan"; 
                    if (keterangan == 0)
                    {
                        isNeedWMApp = true;
                    }
                    // if (_isPVB) //mau nasabah pvb atau bukan kalau kena keterangan approval wm tetep pas otornya ke wm
                    // {
                    //     isNeedWMApp = false;
                    // }
                    //20240219, pratama, BONDRETAIL-1392,end
                    DetailRow["isNeedWMApp"] = isNeedWMApp;
                    string jenisTransaksi = (string)(cmbJenisTransaksi.SelectedItem as DataRowView).Row["TrxDesc"];
                    string messageError = "";
                    if (jenisTransaksi.Equals("Sell", StringComparison.OrdinalIgnoreCase))
                    {
                        //20230215, samypasha, BONDRETAIL-1241, begin
                        //SuratBerharga.checkBookBuilding(cmpsrNomorSekuriti.Text1, out messageError);
                        SuratBerharga.checkBookBuilding(cmpsrNomorSekuriti._Text1.Text.ToString(), out messageError);
                        //20230215, samypasha, BONDRETAIL-1241, end
                        if (messageError.Length > 0)
                        {
                            isBookBld = true;
                            DetailRow["isBookBld"] = isBookBld;
                        }
                        else
                        {
                            DetailRow["isBookBld"] = isBookBld;
                        }
                    }
                    else
                    {
                        DetailRow["isBookBld"] = isBookBld;
                    }
                    DetailRow["HargaModalAwal"] = ndHargaModal.Value;
                    //20160303, fauzil, TRBST16240, end
                    //20190715, darul.wahid, BOSIT18196, begin
                    dsPort = new DataSet();
                    int liSecId2 = Int32.Parse(dsSecId.Tables[0].Rows[0]["SecId"].ToString());
                    string where = " WHERE SecId = '" + liSecId2 + "'";
                    dsPort = SuratBerharga.findSecId(where);
                    string portfolioTA = dsPort.Tables[0].Rows[0]["PortfolioTA"].ToString();
                    string portfolioInternal = dsPort.Tables[0].Rows[0]["PortfolioInternal"].ToString();
                    DetailRow["PortfolioTA"] = portfolioTA;
                    DetailRow["PortfolioInternal"] = portfolioInternal;
                    //20190715, darul.wahid, BOSIT18196, end

                    //20220208, darul.wahid, BONDRETAIL-895, begin
                    //20220708, darul.wahid, BONDRETAIL-977, begin
                    //DetailRow["CapitalGain"] = this.dCapitalGain;
                    //20220208, darul.wahid, BONDRETAIL-895, end
                    DetailRow["CapitalGain"] = dsSecId.Tables[0].Rows[0]["SecCcy"].ToString() == "IDR" ? this.dCapitalGain : this.dCapitalGainNonIDR;
                    DetailRow["TotalTax"] = this.dTotalTax;
                    DetailRow["Income"] = this.dIncome;
                    //20220708, darul.wahid, BONDRETAIL-977, end
					//20220920, yudha.n, BONDRETAIL-1052, begin
                    DetailRow["NoRekMaterai"] = cbRekeningMaterai.SelectedIndex != -1 ? ((KeyValuePair<string, string>)cbRekeningMaterai.SelectedItem).Key : "";
                    DetailRow["MateraiCost"] = this.dMateraiCost;
                    DetailRow["IsAbsorbedByBank"] = this.isMeteraiAbsorbed;
                    //20220920, yudha.n, BONDRETAIL-1052, end
                    //20230110, tobias, BONDRETAIL-1162, begin
                    DataSet dsOut;
                    //20230725, yudha.n, BONDRETAIL-1398, begin
                    //string PODealId = "";
                    PODealId = "";
                    //20230725, yudha.n, BONDRETAIL-1398, end
                    DetailRow["Instruction"] = "CB";
                    DetailRow["PODealId"] = PODealId;
                    if (PopulateDataNasabah("NumberOfData", "BONDS", "", "", "PODealId", false, out dsOut))
                    {
                        PODealId = dsOut.Tables[0].Rows[0][0].ToString();
                        DetailRow["PODealId"] = PODealId;
                    }
                    else
                    {
                        throw new Exception("gagal tarik id");
                    }
                    //20230110, tobias, BONDRETAIL-1162, end
                    //20240422, alfian.andhika, BONDRETAIL-1581, begin
                    DetailRow["YieldHargaModal"] = dYieldHargaModal;
                    //20240422, alfian.andhika, BONDRETAIL-1581, end
                    //20231025,yazri, BONDRETAIL-1392, begin
                    DetailRow["WeightedSpread"] = weightedSpread;
                    DetailRow["WeightedPrice"] = weightedPrice;
                    DetailRow["TotalSpread"] = indikasiTotalSpread;
                    DetailRow["UntungRugiNasabah"] = untungRugiNasabah;
                    DetailRow["WeightedHoldingPeriod"] = weightedHoldingPeriod;
                    DetailRow["Keterangan"] = keterangan;
                    //20231025,yazri, BONDRETAIL-1392, end

                    dtTransaksi.Rows.Add(DetailRow);

                    string xmlFormat = dsTransaksi.GetXml().ToString();
                    System.Data.DataSet dsSave;
                    //bool bSaveXML = true; // setting true untuk test
                    //20160831, fauzil, LOGEN196, begin
                    string xmlFormatTA = "";
                    string xmlFormatNonTA = "";
                    if (FaceValueTA > 0 && FaceValueNonTA > 0)
                    {
                        //Tax Amenesty
                        DataSet dsTA = new DataSet();
                        DataSet dsTATransaksi = new DataSet("Root");
                        DataTable dtTATransaksi = new DataTable("RS");
                        dsTA = SuratBerharga.columnsTransaksi();
                        int iRowColumTA = dsTA.Tables[0].Rows.Count;

                        /* Add Column To Data Table*/
                        dsTATransaksi.Tables.Add(dtTATransaksi);
                        for (int i = 0; i < iRowColumTA; i++)
                        {
                            dtTATransaksi.Columns.Add(dsTA.Tables[0].Rows[i][0].ToString());
                        }
                        dtTATransaksi.Columns.Add("SecurityNo");

                        /* Format Inputan XML*/
                        System.Data.DataRow DetailRowTA = dtTATransaksi.NewRow();

                        DetailRowTA["CIFId"] = CIFid;
                        DetailRowTA["SecId"] = iSecId;
                        DetailRowTA["TrxType"] = iType;
                        DetailRowTA["TrxDate"] = nispDealDate.Value;
                        DetailRowTA["SettlementDate"] = nispSettlementDate.Value;
                        DetailRowTA["Tenor"] = 0;
                        DetailRowTA["FaceValue"] = transaksiBankBeliTA.FaceValue;
                        DetailRowTA["Yield"] = 0;
                        DetailRowTA["DealYield"] = 0;
                        DetailRowTA["SpreadYield"] = 0;
                        DetailRowTA["TaxAmount"] = 0;
                        DetailRowTA["SafeKeepingFeeAmount"] = 0;
                        DetailRowTA["TransactionFeeAmount"] = transaksiBankBeliTA.TransactionFee;
                        DetailRowTA["DealPrice"] = moneyDealPrice.Value;
                        DetailRowTA["Proceed"] = transaksiBankBeliTA.Proceed;
                        DetailRowTA["AccruedDays"] = Convert.ToInt16(transaksiBankBeliTA.AccruedDays);
                        DetailRowTA["AccruedInterest"] = transaksiBankBeliTA.Interest;
                        DetailRowTA["TaxOnAccrued"] = transaksiBankBeliTA.TaxOnAccrued;
                        DetailRowTA["TaxOnCapitalGainLoss"] = transaksiBankBeliTA.TaxOnCapitalGL;
                        DetailRowTA["SafeKeepingFeeAfterTax"] = transaksiBankBeliTA.SafeKeepingFeeAmount;
                        DetailRowTA["TotalProceed"] = transaksiBankBeliTA.TotalProceed;
                        DetailRowTA["TrxStatus"] = "0";
                        DetailRowTA["NIK_CS"] = cmpsrSeller.Text1;
                        DetailRowTA["NIK_Dealer"] = cmpsrDealer.Text1;
                        DetailRowTA["TrxBranch"] = cmpsrSearch1.Text1;
                        DetailRowTA["InsertedBy"] = intNIK;
                        DetailRowTA["InsertedDate"] = FormatDate.ConvertStandardDate(StandardDate.yyyymmdd, strWorkDate, Sparate.blank);
                        DetailRowTA["LastUpdateBy"] = null;
                        DetailRowTA["LastUpdateDate"] = null;
                        DetailRow["SecurityNo"] = dsSecId.Tables[0].Rows[0]["SecurityNo"].ToString();
                        DetailRowTA["PhoneOrderBit"] = chkFlagPhoneOrder.Checked;
                        DetailRowTA["NIK_Ref"] = cmpsrNIKRef.Text1;
                        DetailRowTA["NoRekInvestor"] = transaksiBankBeliTA.NoRekInvestor;
                        DetailRowTA["isTaxAmnesty"] = transaksiBankBeliTA.IsTaxAmnesty;
                        //20190715, darul.wahid, BOSIT18196, begin
                        DetailRowTA["PortfolioTA"] = portfolioTA;
                        DetailRowTA["PortfolioInternal"] = portfolioInternal;
                        //20190715, darul.wahid, BOSIT18196, end
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        //20220708, darul.wahid, BONDRETAIL-977, begin
                        //DetailRowTA["CapitalGain"] = transaksiBankBeliTA.CapitalGain;                        
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        DetailRowTA["CapitalGain"] = dsSecId.Tables[0].Rows[0]["SecCcy"].ToString() == "IDR" ? transaksiBankBeliTA.CapitalGain : transaksiBankBeliTA.CapitalGainNonIdr;
                        DetailRowTA["TotalTax"] = transaksiBankBeliTA.TotalTax;
                        DetailRowTA["Income"] = transaksiBankBeliTA.Income;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        DetailRowTA["NoRekMaterai"] = cbRekeningMaterai.SelectedIndex != -1 ? ((KeyValuePair<string, string>)cbRekeningMaterai.SelectedItem).Key : "";
                        DetailRowTA["MateraiCost"] = this.dMateraiCost;
                        DetailRowTA["IsAbsorbedByBank"] = this.isMeteraiAbsorbed;
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        DetailRowTA["YieldHargaModal"] = transaksiBankBeliTA.YieldHargaModal;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                        //20231025,yazri, BONDRETAIL-1392, begin
                        DetailRowTA["WeightedSpread"] = weightedSpread;
                        DetailRowTA["WeightedPrice"] = weightedPrice;
                        DetailRowTA["TotalSpread"] = indikasiTotalSpread;
                        DetailRowTA["UntungRugiNasabah"] = untungRugiNasabah;
                        DetailRowTA["WeightedHoldingPeriod"] = weightedHoldingPeriod;
                        DetailRowTA["Keterangan"] = keterangan;
                        //20231025,yazri, BONDRETAIL-1392, end


                        dtTATransaksi.Rows.Add(DetailRowTA);
                        xmlFormatTA = dsTATransaksi.GetXml().ToString();

                        //Tax Amenesty
                        DataSet dsNonTA = new DataSet();
                        DataSet dsNonTATransaksi = new DataSet("Root");
                        DataTable dtNonTATransaksi = new DataTable("RS");
                        dsNonTA = SuratBerharga.columnsTransaksi();
                        int iRowColumNonTA = dsNonTA.Tables[0].Rows.Count;

                        /* Add Column To Data Table*/
                        dsNonTATransaksi.Tables.Add(dtNonTATransaksi);
                        for (int i = 0; i < iRowColumNonTA; i++)
                        {
                            dtNonTATransaksi.Columns.Add(dsNonTA.Tables[0].Rows[i][0].ToString());
                        }
                        dtNonTATransaksi.Columns.Add("SecurityNo");

                        /* Format Inputan XML*/
                        System.Data.DataRow DetailRowNonTA = dtNonTATransaksi.NewRow();

                        DetailRowNonTA["CIFId"] = CIFid;
                        DetailRowNonTA["SecId"] = iSecId;
                        DetailRowNonTA["TrxType"] = iType;
                        DetailRowNonTA["TrxDate"] = nispDealDate.Value;
                        DetailRowNonTA["SettlementDate"] = nispSettlementDate.Value;
                        DetailRowNonTA["Tenor"] = 0;
                        DetailRowNonTA["FaceValue"] = transaksiBankBeliNonTA.FaceValue;
                        DetailRowNonTA["Yield"] = 0;
                        DetailRowNonTA["DealYield"] = 0;
                        DetailRowNonTA["SpreadYield"] = 0;
                        DetailRowNonTA["TaxAmount"] = 0;
                        DetailRowNonTA["SafeKeepingFeeAmount"] = 0;
                        DetailRowNonTA["TransactionFeeAmount"] = transaksiBankBeliNonTA.TransactionFee;
                        DetailRowNonTA["DealPrice"] = moneyDealPrice.Value;
                        DetailRowNonTA["Proceed"] = transaksiBankBeliNonTA.Proceed;
                        DetailRowNonTA["AccruedDays"] = Convert.ToInt16(transaksiBankBeliNonTA.AccruedDays);
                        DetailRowNonTA["AccruedInterest"] = transaksiBankBeliNonTA.Interest;
                        DetailRowNonTA["TaxOnAccrued"] = transaksiBankBeliNonTA.TaxOnAccrued;
                        DetailRowNonTA["TaxOnCapitalGainLoss"] = transaksiBankBeliNonTA.TaxOnCapitalGL;
                        DetailRowNonTA["SafeKeepingFeeAfterTax"] = transaksiBankBeliNonTA.SafeKeepingFeeAmount;
                        DetailRowNonTA["TotalProceed"] = transaksiBankBeliNonTA.TotalProceed;
                        DetailRowNonTA["TrxStatus"] = "0";
                        DetailRowNonTA["NIK_CS"] = cmpsrSeller.Text1;
                        DetailRowNonTA["NIK_Dealer"] = cmpsrDealer.Text1;
                        DetailRowNonTA["TrxBranch"] = cmpsrSearch1.Text1;
                        DetailRowNonTA["InsertedBy"] = intNIK;
                        DetailRowNonTA["InsertedDate"] = FormatDate.ConvertStandardDate(StandardDate.yyyymmdd, strWorkDate, Sparate.blank);
                        DetailRowNonTA["LastUpdateBy"] = null;
                        DetailRowNonTA["LastUpdateDate"] = null;
                        DetailRowNonTA["SecurityNo"] = dsSecId.Tables[0].Rows[0]["SecurityNo"].ToString();
                        DetailRowNonTA["PhoneOrderBit"] = chkFlagPhoneOrder.Checked;
                        DetailRowNonTA["NIK_Ref"] = cmpsrNIKRef.Text1;
                        DetailRowNonTA["NoRekInvestor"] = transaksiBankBeliNonTA.NoRekInvestor;
                        DetailRowNonTA["isTaxAmnesty"] = transaksiBankBeliNonTA.IsTaxAmnesty;
                        //20190715, darul.wahid, BOSIT18196, begin
                        DetailRowNonTA["PortfolioTA"] = portfolioTA;
                        DetailRowNonTA["PortfolioInternal"] = portfolioInternal;
                        //20190715, darul.wahid, BOSIT18196, end

                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        //20220708, darul.wahid, BONDRETAIL-977, begin
                        //DetailRowNonTA["CapitalGain"] = transaksiBankBeliNonTA.CapitalGain;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        DetailRowNonTA["CapitalGain"] = dsSecId.Tables[0].Rows[0]["SecCcy"].ToString() == "IDR" ? transaksiBankBeliNonTA.CapitalGain : transaksiBankBeliNonTA.CapitalGainNonIdr;
                        DetailRowNonTA["TotalTax"] = transaksiBankBeliNonTA.TotalTax;
                        DetailRowNonTA["Income"] = transaksiBankBeliNonTA.Income;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        DetailRowNonTA["NoRekMaterai"] = cbRekeningMaterai.SelectedIndex != -1 ? ((KeyValuePair<string, string>)cbRekeningMaterai.SelectedItem).Key : "";
                        DetailRowNonTA["MateraiCost"] = this.dMateraiCost;
                        DetailRowNonTA["IsAbsorbedByBank"] = this.isMeteraiAbsorbed;
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        DetailRowNonTA["YieldHargaModal"] = transaksiBankBeliNonTA.YieldHargaModal;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                        //20231025,yazri, BONDRETAIL-1392, begin
                        DetailRowNonTA["WeightedSpread"] = weightedSpread;
                        DetailRowNonTA["WeightedPrice"] = weightedPrice;
                        DetailRowNonTA["TotalSpread"] = indikasiTotalSpread;
                        DetailRowNonTA["UntungRugiNasabah"] = untungRugiNasabah;
                        DetailRowNonTA["WeightedHoldingPeriod"] = weightedHoldingPeriod;
                        DetailRowNonTA["Keterangan"] = keterangan;
                        //20231025,yazri, BONDRETAIL-1392, end

                        dtNonTATransaksi.Rows.Add(DetailRowNonTA);
                        xmlFormatNonTA = dsNonTATransaksi.GetXml().ToString();
                    }
                    else if (FaceValueTA > 0)
                    {
                        xmlFormatTA = dsTransaksi.GetXml().ToString();
                        xmlFormatNonTA = null;
                    }
                    else if (FaceValueNonTA > 0)
                    {
                        xmlFormatNonTA = dsTransaksi.GetXml().ToString();
                        xmlFormatTA = null;
                    }
                    //20160831, fauzil, LOGEN196, end
                    //20120802, hermanto_salim, BAALN12003, begin
                    //bool bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, out dsSave);
                    bool bSaveXML = false;
                    dsSave = null;
                    //20160726, fauzil, TRBST16240, begin
                    //string jenisTransaksi = (string)(cmbJenisTransaksi.SelectedItem as DataRowView).Row["TrxDesc"];
                    //20160726, fauzil, TRBST16240, end
                    
                    switch (jenisTransaksi)
                    {
                        case "Buy":
                            {
								//20220920, yudha.n, BONDRETAIL-1052, begin
								if (!MateraiValidation(dcMateraiAmountBlock, 0))
									return;
								//20220920, yudha.n, BONDRETAIL-1052, end
                                //20200117, dion, TR12020-1, BONDRETAIL-102, begin
                                string strInvalidAcc = "";
                                foreach (DataGridViewRow row in dgvTransactionLink.Rows)
                                {
                                    if ((bool)row.Cells["SelectBit"].Value)
                                    {
                                        //20221223, yazri, VSYARIAH-340, begin
                                        //string strNoRek = (string)row.Cells["NoRekInvestor"].Value;
                                        string strNoRek = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                                        //20221223, yazri, VSYARIAH-340, end
                                        string result = "";
                                        string rejectDesc = "";
                                        clsCallWebService.CallAccountInquiry(strGuid, intNIK.ToString(), strNoRek, out result, out rejectDesc);

                                        if (string.IsNullOrEmpty(rejectDesc) && !string.IsNullOrEmpty(result))
                                        {
                                            DataSet dsData = new DataSet();
                                            dsData.ReadXml(new StringReader(result));
                                            AccountStatus = Convert.ToInt16(dsData.Tables[0].Rows[0]["AccountStatus"]);

                                            if ((AccountStatus.HasValue) && (AccountStatus != 1) && (AccountStatus != 4))
                                            {
                                                if (!string.IsNullOrEmpty(strInvalidAcc))
                                                    strInvalidAcc += ", ";

                                                strInvalidAcc += string.Format("[{0}]", strNoRek.Trim());
                                            }
                                        }
                                    }
                                }
                                if (!string.IsNullOrEmpty(strInvalidAcc))
                                {
                                    //20221220, yazri, VSYARIAH-310, begin
                                    //MessageBox.Show(string.Format("Rekening {0} berstatus close/dormant. silakan ganti di master nasabah terlebih dahulu", strInvalidAcc), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    MessageBox.Show(string.Format("Rekening {0} berstatus close/dormant. Silakan menggunakan rekening lain.", cbRekeningRelasi.Text.ToString().Trim()), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //20221220, yazri, VSYARIAH-310, end
                                    return;
                                }
                                //20231120, rezakahfi, BONDRETAIL-1484, begin
                                /*
                                    DetailRow["CIFId"] = CIFid;
                                    DetailRow["SecId"] = iSecId;
                                    DetailRow["TrxType"] = iType;
                                    DetailRow["FaceValue"] = moneyFaceValue.Value;
                                 */
                                bool isValid = false;
                                string strErrMsg = "";
                                string strErrCode = "";

                                if (!SuratBerharga.ValidateBalance(iType, iSecId, CIFid, moneyFaceValue.Value, PODealId, out isValid, out strErrMsg, out strErrCode))
                                    return;
                                else
                                {
                                    if (!isValid)
                                    {
                                        MessageBox.Show(strErrCode + " - " + strErrMsg);
                                        return;
                                    }
                                }
                                //20231120, rezakahfi, BONDRETAIL-1484, end
                                //20200117, dion, TR12020-1, BONDRETAIL-102, end
                                //20160301, fauzil, TRBST16240, begin
                                //bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave);
                                //20220920, yudha.n, BONDRETAIL-1052, begin
                                //bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave, xmlFormatTA, xmlFormatNonTA, xmlSumberDana);
                                //20231227, rezakahfi, BONDRETAIL-1513, begin
                                if (!isTA)
                                {
                                    bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave, xmlFormatTA, xmlFormatNonTA, xmlSumberDana, out DealId);
                                }
                                else
                                {
                                    bSaveXML = SuratBerhargaTA.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave, xmlFormatTA, xmlFormatNonTA, xmlSumberDana, intNIK, out DealId);
                                }
                                //20231227, rezakahfi, BONDRETAIL-1513, end
                                //20220920, yudha.n, BONDRETAIL-1052, end
                                //20160301, fauzil, TRBST16240, end
                                //20130717, uzia, BAFEM12012, begin
                                // Process saving accrued interest
                                if (bSaveXML)
                                {
                                    if (this._dsAccrued != null && this._dsAccrued.Tables.Count > 0)
                                    {
                                        string strDealNo = "";
                                        if (dsSave.Tables[0].Rows.Count > 0)
                                        //20210113, rezakahfi, BONDRETAIL-544, begin
                                            //strDealNo = dsSave.Tables[0].Rows[0][0].ToString();
                                            strDealNo = dsSave.Tables[0].Rows[0]["DealNo"].ToString();
                                        //20210113, rezakahfi, BONDRETAIL-544, end

                                        this._dsAccrued.DataSetName = "Data";
                                        this._dsAccrued.Tables[0].TableName = "Accrued";
                                        string strXmlAccrued = this._dsAccrued.GetXml();

                                        SuratBerharga.UpdateAccruedInterest(this.cQuery, strXmlAccrued, strDealNo);
                                    }
                                    ////20221020, Tobias Renal, HFUNDING-181, Begin
                                    //saveSimanis();
                                    ////20221020, Tobias Renal, HFUNDING-181, End
                                }
                                //20130717, uzia, BAFEM12012, end
                                break;
                            }
                        case "Sell":
                            {
                                bool applyBlockAccount = clsGlobal.insertSecurityTransactionApplyBlockAccount;
                                //20160301, fauzil, TRBST16240, begin                                             
                                if (isBookBld)
                                    MessageBox.Show(messageError, "Warning!!!", MessageBoxButtons.OK);

                                //if ((chkSwitching.Checked) & (cmbDealIdSwitching.SelectedIndex == -1))
                                //{
                                //    MessageBox.Show("Deal Number untuk switching belum terpilih ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                //    return;
                                //}

                                if (chkSwitching.Checked)
                                    applyBlockAccount = false;
                                else if (chkOther.Checked)
//20200917, rezakahfi, BONDRETAIL-550, begin
                                //applyBlockAccount = false;
                                {
                                    //20220221, darul.wahid, BONDRETAIL-892, begin
                                    //20220406, darul.wahid, BONDRETAIL-927, begin
                                    if (gbSumberDana.Visible == true)
                                    {
                                        if (!ValidateSumberDana(SuratBerharga))
                                            return;
                                    }
                                    //20220406, darul.wahid, BONDRETAIL-927, end
                                    //20220221, darul.wahid, BONDRETAIL-892, end
                                    DataTable dtTableRek = (DataTable)dgvSourceFund.DataSource;
                                    DataRow[] drRowRekening = dtTableRek.Select("SourceData = 'Rekening'");
                                    if (drRowRekening.Length > 0)
                                    {
                                        applyBlockAccount = true;
                                        dcAmountBlock = decimal.Parse(drRowRekening[0]["Amount"].ToString());
                                    }
                                    else
                                    {
                                        applyBlockAccount = false;
                                        //20220920, yudha.n, BONDRETAIL-1052, begin
                                        dcAmountBlock = 0;
                                        //20220920, yudha.n, BONDRETAIL-1052, end
                                    }
                                }
//20200917, rezakahfi, BONDRETAIL-550, end
                                //20160301, fauzil, TRBST16240, end
								//20220920, yudha.n, BONDRETAIL-1052, begin
								if (!MateraiValidation(dcMateraiAmountBlock, dcAmountBlock))
									return;
								//20220920, yudha.n, BONDRETAIL-1052, end
                                //20200117, dion, TR12020-1, BONDRETAIL-102, begin
                                if ((AccountStatus.HasValue) && (AccountStatus != 1) && (AccountStatus != 4))
                                {
                                    //20221220, yazri, VSYARIAH-310, begin
                                    //MessageBox.Show(string.Format("Rekening {0} berstatus close/dormant. silakan ganti di master nasabah terlebih dahulu", cmpsrNoRekSecurity.Text1.Trim()), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    MessageBox.Show(string.Format("Rekening {0} berstatus close/dormant. Silakan menggunakan rekening lain.", cbRekeningRelasi.Text.ToString().Trim()), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //20221220, yazri, VSYARIAH-310, end
                                    return;
                                }

                                if (applyBlockAccount)
                                {
                                    //20130211, victor, BAALN12003, begin
                                    //if ((AccountStatus.HasValue) && (AccountStatus != 1) && (AccountStatus != 4))
                                    //{
                                    //    MessageBox.Show("Status Rekening Relasi tidak Aktif", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //    return;
                                    //}
                                    //20130211, victor, BAALN12003, end
                                    //20200117, dion, TR12020-1, BONDRETAIL-102, end
                                    //20231227, rezakahfi, BONDRETAIL-1513, begin
                                    #region check amount

                                    string NoRek = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                                    decimal dcSaldo = _clsSaldo.GetSaldoRekening(NoRek);
                                    effectiveBalance = dcSaldo;

                                    #endregion
                                    //20231227, rezakahfi, BONDRETAIL-1513, end
                                    if (effectiveBalance.HasValue)
                                    {
                                        //20200917, rezakahfi, BONDRETAIL-550, begin
                                        //if (effectiveBalance.Value < txtTotalProceed.Value)
                                        if (effectiveBalance.Value < txtTotalProceed.Value && !chkOther.Checked)
                                        //20200917, rezakahfi, BONDRETAIL-550, end
                                        {
                                            // versi lama, munculkan dialog option proses tanpa block bila saldo tidak cukup
                                            //DialogResult dialogResult = MessageBox.Show("Saldo efektif nasabah lebih kecil dari nominal yang diperlukan, lanjutkan transaksi tanpa lakukan block?\n(Pilih 'Yes' untuk melanjutkan tanpa block, pilih 'No' untuk melanjutkan tetap dengan block, atau pilih 'Cancel' untuk membatalkan transaksi)", "Konfirmasi", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                                            //if (dialogResult == DialogResult.Cancel)
                                            //    return;
                                            //else
                                            //    applyBlockAccount = dialogResult == DialogResult.No;
                                            // versi baru, langsung tolak bila saldo tidak cukup
                                            //20180503, uzia, begin 
                                            //MessageBox.Show("Saldo efektif nasabah lebih kecil dari nominal yang diperlukan, transaksi tidak dapat dilanjutkan.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            MessageBox.Show("Saldo efektif nasabah lebih kecil dari nominal yang diperlukan, transaksi tidak dapat dilanjutkan. [ " + effectiveBalance.Value.ToString("N2") + " | " + txtTotalProceed.Value.ToString("N2") + " ] ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            //20180503, uzia, end 
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        // versi lama, munculkan dialog option proses tanpa block bila saldo tidak cukup
                                        //DialogResult dialogResult = MessageBox.Show("Tidak dapat menentukan saldo efektif nasabah, lanjutkan transaksi tanpa lakukan block?\n(Pilih 'Yes' untuk melanjutkan tanpa block, pilih 'No' untuk melanjutkan tetap dengan block, atau pilih 'Cancel' untuk membatalkan transaksi)", "Konfirmasi", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                                        //if (dialogResult == DialogResult.Cancel)
                                        //    return;
                                        //else
                                        //    applyBlockAccount = dialogResult == DialogResult.No;
                                        // versi baru, langsung tolak bila saldo tidak cukup
                                        MessageBox.Show("Tidak dapat menentukan saldo efektif nasabah, transaksi tidak dapat dilanjutkan.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return;
                                    }
                                }
								//20210813, rezakahfi, BONDRETAIL-799, begin
//20231227, rezakahfi, BONDRETAIL-1513, begin
                                if (!isTA)
                                {
//20231227, rezakahfi, BONDRETAIL-1513, end
                                    if (!CekHargaModal())
                                        return;
                                }
                                else
                                {
                                    if (!ValidateProduct())
                                        return;
//20231227, rezakahfi, BONDRETAIL-1513, begin
                                }
//20231227, rezakahfi, BONDRETAIL-1513, end
                                //20210813, rezakahfi, BONDRETAIL-799, end
                                //20231120, rezakahfi, BONDRETAIL-1484, begin
                                /*
                                    DetailRow["CIFId"] = CIFid;
                                    DetailRow["SecId"] = iSecId;
                                    DetailRow["TrxType"] = iType;
                                    DetailRow["FaceValue"] = moneyFaceValue.Value;
                                 */
                                bool isValid = false;
                                string strErrMsg = "";
                                string strErrCode = "";

                                if (!SuratBerharga.ValidateBalance(iType, iSecId, CIFid, moneyFaceValue.Value, PODealId, out isValid, out strErrMsg, out strErrCode))
                                    return;
                                else
                                {
                                    if (!isValid)
                                    {
                                        MessageBox.Show(strErrCode + " - " + strErrMsg);
                                        return;
                                    }
                                }
                                //20231120, rezakahfi, BONDRETAIL-1484, end
                                if (applyBlockAccount)
                                {
                                    string currency = dsSecId.Tables[0].Rows[0]["SecCcy"].ToString();
                                    //20130222, victor, BAALN12003, begin
                                    //bSaveXML = SuratBerharga.saveSuratBerhargaBankJualWithBlock(xmlFormat, xmlTransactionLink, cmpsrNoRekSecurity.Text1, txtTotalProceed.Value, currency, clsCallWebService, out dsSave);
                                    //20160815, fauzil, LOGEN196, begin
                                    //bSaveXML = SuratBerharga.saveSuratBerhargaBankJualWithBlock(xmlFormat, xmlTransactionLink, cmpsrNoRekSecurity.Text1, txtTotalProceed.Value, currency, clsCallWebService, out dsSave, ProductCode, AccountType);
                                    //20200917, rezakahfi, BONDRETAIL-550, begin
                                    //bSaveXML = SuratBerharga.saveSuratBerhargaBankJualWithBlock(xmlFormat, xmlTransactionLink, cmpsrNoRekSecurity.Text1, txtTotalProceed.Value, currency, clsCallWebService, out dsSave, ProductCode, AccountType, ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key, txtRekeningRelasiName.Text);
                                    //20220920, yudha.n, BONDRETAIL-1052, begin
                                    //bSaveXML = SuratBerharga.saveSuratBerhargaBankJualWithBlock(xmlFormat, xmlTransactionLink, cmpsrNoRekSecurity.Text1, dcAmountBlock, currency, clsCallWebService, out dsSave, ProductCode, AccountType, ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key, txtRekeningRelasiName.Text, xmlSumberDana);
                                    //20231227, rezakahfi, BONDRETAIL-1513, begin
                                    if (!isTA)
                                    {
                                        bSaveXML = SuratBerharga.saveSuratBerhargaBankJualWithBlock(xmlFormat, xmlTransactionLink, cmpsrNoRekSecurity.Text1, dcAmountBlock, currency, clsCallWebService, out dsSave, ProductCode, AccountType, ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key, txtRekeningRelasiName.Text, xmlSumberDana, out DealId, out BlockSequenceBond);
                                    }
                                    else
                                    {
                                        int inputter = 0;
                                        int.TryParse(cmpsrDealer.Text1, out inputter);
                                        bSaveXML = SuratBerhargaTA.saveSuratBerhargaBankJualWithBlock(xmlFormat, xmlTransactionLink, cmpsrNoRekSecurity.Text1, dcAmountBlock, currency, clsCallWebService, out dsSave, ProductCode, AccountType, ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key, txtRekeningRelasiName.Text, cmpsrSearch1.Text1, inputter, intNIK, xmlSumberDana, out DealId, out BlockSequenceBond);
                                    }
                                    //20231227, rezakahfi, BONDRETAIL-1513, end
                                    //20220920, yudha.n, BONDRETAIL-1052, end
                                    //20200917, rezakahfi, BONDRETAIL-550, end
                                    //20160815, fauzil, LOGEN196, end
                                    //20130222, victor, BAALN12003, end
                                }
                                else
                                {
                                    //20160301, fauzil, TRBST16240, begin
                                    //bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave);
                                    //bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave, null, null, null);
                                    //20220920, yudha.n, BONDRETAIL-1052, begin
                                    //bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave, xmlFormatTA, xmlFormatNonTA, xmlSumberDana);
                                    //20231227, rezakahfi, BONDRETAIL-1513, begin
                                    if (!isTA)
                                    {
                                        bSaveXML = SuratBerharga.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave, xmlFormatTA, xmlFormatNonTA, xmlSumberDana, out DealId);
                                    }
                                    else
                                    {
                                        bSaveXML = SuratBerhargaTA.saveSuratBerharga(xmlFormat, xmlTransactionLink, out dsSave, xmlFormatTA, xmlFormatNonTA, xmlSumberDana, intNIK, out DealId);
                                    }
                                    //20231227, rezakahfi, BONDRETAIL-1513, end
                                    //20220920, yudha.n, BONDRETAIL-1052, end
                                    //20160301, fauzil, TRBST15176, end
                                } 
                                break;
                            }
                    }
                    //20220920, yudha.n, BONDRETAIL-1052, begin
                    if (bSaveXML && !isMeteraiAbsorbed && dcMateraiAmountBlock > 0)
                    {
                        DateTime expDate = DateTime.ParseExact(nispDealDate.Value.ToString(), "yyyyMMdd", null).AddDays(1);
                        DealId = dsSave.Tables[0].Rows[0]["DealId"].ToString();
                        bSaveXML = SuratBerharga.BlokirRekeningBiayaMaterai(_clsBlokirSaldo, dcMateraiAmountBlock, AccountTypeMaterai, DetailRow["NoRekMaterai"].ToString(), DealId, jenisTransaksi, out BlockSequenceMaterai, expDate.ToString("ddMMyy"));
                    }
                    //20220920, yudha.n, BONDRETAIL-1052, end
                    //20120802, hermanto_salim, BAALN12003, end
                    if (bSaveXML == true)
                    {

                        //20160301, fauzil, TRBST16240, begin
                        // risk profile dan flag NeedUpdateDataNasabah true maka update data nasabah treasury
                        if (NeedUpdateDataNasabah)
                            Nasabah.updateTreasuryCustomerCauseRiskProfile(CIFNo, this.nispDealDate.Value, dLastUpdateJenisRiskProfile);
                        //20160301, fauzil, TRBST16240, end

                        //20160622, fauzil, TRBST16240, begin
                        //20210113, rezakahfi, BONDRETAIL-544, begin
                        //string DealId = dsSave.Tables[0].Rows[0][0].ToString();
                        //DealId = DealId.Substring(3);
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        //string DealId = dsSave.Tables[0].Rows[0]["DealId"].ToString();
                        DealId = dsSave.Tables[0].Rows[0]["DealId"].ToString();
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        //20210113, rezakahfi, BONDRETAIL-544, end
                        DealId = long.Parse(DealId).ToString();
                        MessageBox.Show("Data Success Tersimpan\n Deal Id : " + DealId + "\n ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //20160622, fauzil, TRBST16240, end
                        //20121127, hermanto_salim, BAALN12003, begin
                        strFlag = "";
                        //20121127, hermanto_salim, BAALN12003, end
                        //20221020, Tobias Renal, HFUNDING-181, Begin
                        saveSimanis(DealId);
                        //20221020, Tobias Renal, HFUNDING-181, End
                        controlsEnabled(false);
                        controlsClear(true);
                        //20121127, hermanto_salim, BAALN12003, begin
                        //strFlag = "";
                        //20121127, hermanto_salim, BAALN12003, end
                        //20130307, uzia, BAFEM12016, begin
                        chkFlagPhoneOrder.Enabled = false;
                        //20130307, uzia, BAFEM12016, end
                    }
                    else
                    {
                        MessageBox.Show("Data Gagal Tersimpan", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						//20230109, tobias, BONDRETAIL-1162, begin
                        DeleteTransactionSekunder(cQuery, "", PODealId, "CB");
                        //20230109, tobias, BONDRETAIL-1162, end
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        int inputter = 0;
                        int.TryParse(cmpsrDealer.Text1, out inputter);
                        clsDatabase.DeleteTransactionMateraiFee(cQuery, DealId);
                        if (BlockSequenceBond != 0)
                        {
                            _clsBlokirSaldo.RelaseBlockRekening(DetailRow["NoRekInvestor"].ToString(), AccountType, "Relasi", BlockSequenceBond, inputter);
                        }
                        if (BlockSequenceMaterai != 0)
                        {
                            _clsBlokirSaldo.RelaseBlockRekening(DetailRow["NoRekMaterai"].ToString(), AccountTypeMaterai, "Meterai", BlockSequenceMaterai, null);
                        }
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        nispDealDate.Focus();
                    }

                    //}
                    //else
                    //{
                    //    MessageBox.Show("Data Gagal Tersimpan", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //    nispDealDate.Focus();
                    //}
                }
                else
                {
                    //20160513, fauzil, TRBST16240, begin
                    //MessageBox.Show("Data Ada Yang Kosong", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    string errorMessage = "";
                    if (cmpsrNomorSekuriti.Text1.Length == 0)
                        errorMessage = errorMessage + "Nomor Sekurity,";
                    if (cmpsrSearch1.Text1.Length == 0)
                        errorMessage = errorMessage + "Kode Cabang,";
                    if (nispSettlementDate.Text.Length == 0)
                        errorMessage = errorMessage + "Settlement Date,";
                    if (nispDealDate.Text.Length == 0)
                        errorMessage = errorMessage + "Deal Date,";
                    if (txtAccruedDays.Text.Length == 0)
                        errorMessage = errorMessage + "Accrued Days,";
                    if (moneyFaceValue.Value == 0)
                        errorMessage = errorMessage + "Face value,";
                    if (txtAccruedInterest.Text.Length == 0)
                        errorMessage = errorMessage + "Accrued Interest,";
                    if (txtProceed.Value == 0)
                        errorMessage = errorMessage + "Proceed,";
                    if (moneyDealPrice.Text.Length == 0)
                        errorMessage = errorMessage + "Deal Price,";
                    if (txtTotalProceed.Value == 0)
                        errorMessage = errorMessage + "Total Proceed,";
                    if (string.IsNullOrEmpty(cmpsrSeller.Text1))
                        errorMessage = errorMessage + "Seller,";
                    if (string.IsNullOrEmpty(cmpsrDealer.Text1))
                        errorMessage = errorMessage + "Dealer,";
                    errorMessage = errorMessage.Substring(0, errorMessage.Length - 1);
                    MessageBox.Show(errorMessage + " Tidak Boleh Kosong", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //20160513, fauzil, TRBST16240, end
                    //20221220, yazri, VSYARIAH-340, begin
                    //cmpsrNoRekSecurity.Focus();
                    nispDealDate.Focus();
                    //20221220, yazri, VSYARIAH-340, end
                }
            }
            catch (NullReferenceException ex)
            {
                //20230725, yudha.n, BONDRETAIL-1398, begin
                DeleteTransactionSekunder(cQuery, "", PODealId, "CB");
                //20230725, yudha.n, BONDRETAIL-1398, end
                MessageBox.Show(ex.Message);
            }
            //20160301, fauzil, TRBST16240, begin
            //catch            
            //{
            //    MessageBox.Show("Error Insert Method : Tidak Teridentifikasi");
            //}
            catch (Exception exp)
            {
                //20230725, yudha.n, BONDRETAIL-1398, begin
                DeleteTransactionSekunder(cQuery, "", PODealId, "CB");
                //20230725, yudha.n, BONDRETAIL-1398, end
                //20230303, tobias, BONDRETAIL-1162, begin
                //MessageBox.Show("Error Insert Method : Tidak Teridentifikasi(" + exp.Message + ")");
                MessageBox.Show("Proses Save Transaksi Tidak Berhasil\n Error :(" + exp.Message + ")\n");
                //20230303, tobias, BONDRETAIL-1162, end
            }
            //20160301, fauzil, TRBST16240, end
        }

        //20220920, yudha.n, BONDRETAIL-1052, begin
        private bool MateraiValidation(decimal dcMateraiAmountBlock, decimal dcTrxAmountBlock)
        {
            string noRekInvestor = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
            string noRekMaterai = cbRekeningMaterai.SelectedIndex != -1 ? ((KeyValuePair<string, string>)cbRekeningMaterai.SelectedItem).Key : "";

            if (effectiveBalanceMaterai.HasValue)
            {
                if (noRekInvestor == noRekMaterai)
                {
                    if (effectiveBalanceMaterai.Value < dcMateraiAmountBlock + dcTrxAmountBlock)
                    {
                        MessageBox.Show("Saldo efektif nasabah [ " + effectiveBalanceMaterai.Value.ToString("N2") + " ] lebih kecil dari nominal yang diperlukan [ " + (dcMateraiAmountBlock + dcTrxAmountBlock).ToString("N2") + " ], transaksi tidak dapat dilanjutkan.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                else {
                    if (effectiveBalanceMaterai.Value < dcMateraiAmountBlock)
                    {
                        MessageBox.Show("Saldo efektif nasabah lebih kecil dari nominal yang diperlukan untuk pembayaran meterai, transaksi tidak dapat dilanjutkan. [ " + effectiveBalanceMaterai.Value.ToString("N2") + " | " + txtMateraiCost.Value.ToString("N2") + " ] ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }
            return true;
        }
        //20220920, yudha.n, BONDRETAIL-1052, end

        //20090910, David, SYARIAH001, begin
        public void     updateMethod()
        {
            try
            {
                TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
                DataSet dsSecId = new DataSet();
                //20160310, fauzil, TRBST16240, begin
                RegistrasiNasabah Nasabah = new RegistrasiNasabah();
                //20160310, fauzil, TRBST16240, end
                //20200917, rezakahfi, BONDRETAIL-550, begin
                decimal dcAmountBlock = txtTotalProceed.Value;
                //20200917, rezakahfi, BONDRETAIL-550, end
                //20220920, yudha.n, BONDRETAIL-1052, begin
                string DealId = "0";
                int BlockSequenceMaterai = 0;
                decimal dcMateraiAmountBlock = txtMateraiCost.Value;
                if (cbRekeningMaterai.Enabled && cbRekeningMaterai.SelectedIndex < 1 && !isMeteraiAbsorbed && dMateraiCost != 0)
                {
                    MessageBox.Show("Rekening Biaya Meterai Masih kosong !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20220920, yudha.n, BONDRETAIL-1052, end
                //20130305, uzia, BAFEM12016, begin
                /* Checking transaksi pertama */
                //20160301, fauzil, TRBST16240, begin
                if (cmbJenisTransaksi.Text == "Sell")                
                {                    
                    if (moneyDealPrice.Value < ndHargaModal.Value)
                    {
                        if (MessageBox.Show("Apakah transaksi ini sudah mendapatkan harga spesial dari Treasury?", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            return;
                    }
                }
                else
                {
                    if (moneyDealPrice.Value > ndHargaModal.Value)
                    {
                        if (MessageBox.Show("Apakah transaksi ini sudah mendapatkan harga spesial dari Treasury?", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            return;
                    }
                }
                //20160301, fauzil, TRBST16240, end
                if (!SavePhoneOrderValidation())
                    return;
                //20130305, uzia, BAFEM12016, end
                if (cmpsrNomorSekuriti.Text1.Length != 0 && cmpsrSearch1.Text1.Length != 0 && cmpsrNoRekSecurity.Text1.Length != 0
                    && nispSettlementDate.Text.Length != 0 && nispDealDate.Text.Length != 0 && txtAccruedDays.Text.Length != 0
                    && moneyFaceValue.Value != 0 && txtAccruedInterest.Text.Length != 0 && txtProceed.Value != 0
                    && moneyDealPrice.Text.Length != 0 && //MoneyTransactionFee.Value != 0 && 
                    //20120802, hermanto_salim, BAALN12003, begin
                    //txtTotalProceed.Value != 0 && cmpsrNIK.Text1 != "" && cmpsrDealer.Text1 != "")
                    txtTotalProceed.Value != 0 && cmpsrSeller.Text1 != "" && cmpsrDealer.Text1 != "")
                //20120802, hermanto_salim, BAALN12003, end
                {
                    //20120802, hermanto_salim, BAALN12003, begin
                    if (!subAdditionalValidate())
                        return;
                    //20120802, hermanto_salim, BAALN12003, end

                    //20160310, fauzil, TRBST16240, begin
                    string xmlTransactionLink = null;
                    if (cmbJenisTransaksi.Text == "Buy")
                    {
                        DataTable dtToXML = (dgvTransactionLink.DataSource as DataTable).Clone();
                        dtToXML.TableName = "SecurityTransactionLink_TR";
                        //20161213, fauzil, TRBST16240, begin
                        //for (int i = 0; i < dgvTransactionLink.RowCount; i++)
                        //{
                        //    if ((bool)dgvTransactionLink.Rows[i].Cells["SelectBit"].Value)
                        //    {
                        //        DataRow selectedRow = (dgvTransactionLink.Rows[i].DataBoundItem as DataRowView).Row;
                        //        selectedRow.AcceptChanges();
                        //        dtToXML.ImportRow(selectedRow);
                        //    }
                        //}

                        dtToXML.Columns.Add("TransactionFeeAmount");
                        dtToXML.Columns.Add("Proceed");
                        dtToXML.Columns.Add("AccruedDays");
                        dtToXML.Columns.Add("AccruedInterest");
                        dtToXML.Columns.Add("TaxOnAccrued");
                        dtToXML.Columns.Add("TaxOnCapitalGainLoss");
                        dtToXML.Columns.Add("SafeKeepingFeeAfterTax");
                        dtToXML.Columns.Add("TotalProceed");

                        decimal? trxFee = chkTransactionFee.Checked ? 0 : (decimal?)null;

                        foreach (DataGridViewRow row in dgvTransactionLink.Rows)
                        {
                            if ((bool)row.Cells["SelectBit"].Value)
                            {
                                DataTable dtToXMLCalculate = (dgvTransactionLink.DataSource as DataTable).Clone();
                                dtToXMLCalculate.TableName = "SecurityTransactionLink_TR";
                                DataRow dRow = dtToXML.NewRow();
                                DataRow dCRow = dtToXMLCalculate.NewRow();
                                DataSet dsData = new DataSet();
                                string xmlTransactionLinkInner = null;
                                foreach (DataGridViewCell cell in row.Cells)
                                {
                                    dRow[cell.ColumnIndex] = cell.Value;
                                    dCRow[cell.ColumnIndex] = cell.Value;
                                }
                                dtToXMLCalculate.Rows.Add(dCRow);
                                StringBuilder stringBuilderInner = new StringBuilder();
                                dtToXMLCalculate.WriteXml(System.Xml.XmlWriter.Create(stringBuilderInner));
                                xmlTransactionLinkInner = stringBuilderInner.ToString();
                                decimal FaceValue = decimal.Parse(dRow["NominalJual"].ToString());
                                //20220920, yudha.n, BONDRETAIL-1052, begin
                                //dsData = SuratBerharga.calculateORI2(this.cQuery, Convert.ToInt32(cmbJenisTransaksi.SelectedValue), CIFid, iSecId, nispSettlementDate.Value, FaceValue, decimal.Parse(moneyDealPrice.Text.ToString()), decimal.Parse(txtTaxTarif.Text.ToString()), trxFee, xmlTransactionLinkInner);
                                //20240715,pratama,BONDRETAIL-1392, begin
                                long pushBackDealId = 0;
                                Int64.TryParse(cmpsrGetPushBack.Text1.Trim(), out pushBackDealId);
                                //dsData = SuratBerharga.calculateORI2(this.cQuery, Convert.ToInt32(cmbJenisTransaksi.SelectedValue), CIFid, iSecId, nispSettlementDate.Value, FaceValue, decimal.Parse(moneyDealPrice.Text.ToString()), decimal.Parse(txtTaxTarif.Text.ToString()), trxFee, xmlTransactionLinkInner, "BOND");
                                dsData = SuratBerharga.calculateORI2(this.cQuery, Convert.ToInt32(cmbJenisTransaksi.SelectedValue), CIFid, iSecId, nispSettlementDate.Value, FaceValue, decimal.Parse(moneyDealPrice.Text.ToString()), decimal.Parse(txtTaxTarif.Text.ToString()), trxFee, xmlTransactionLinkInner, "BOND", pushBackDealId);
                                //20240715,pratama,BONDRETAIL-1392, end
                                //20220920, yudha.n, BONDRETAIL-1052, end
                                dRow["TransactionFeeAmount"] = dsData.Tables[0].Rows[0]["TransactionFee"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TransactionFee"].ToString());
                                dRow["Proceed"] = dsData.Tables[0].Rows[0]["Proceed"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["Proceed"].ToString());
                                dRow["AccruedDays"] = dsData.Tables[0].Rows[0]["AccruedDays"].ToString() == "" ? Convert.ToDecimal(0) : decimal.Parse(dsData.Tables[0].Rows[0]["AccruedDays"].ToString());
                                dRow["AccruedInterest"] = dsData.Tables[0].Rows[0]["Interest"].ToString() == "" ? Convert.ToDecimal(0) : decimal.Parse(dsData.Tables[0].Rows[0]["Interest"].ToString());
                                dRow["TaxOnAccrued"] = dsData.Tables[0].Rows[0]["TaxOnAccrued"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TaxOnAccrued"].ToString());
                                dRow["TaxOnCapitalGainLoss"] = dsData.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString());
                                dRow["SafeKeepingFeeAfterTax"] = dsData.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString());
                                dRow["TotalProceed"] = dsData.Tables[0].Rows[0]["TotalProceed"].ToString() == "" ? 0 : decimal.Parse(dsData.Tables[0].Rows[0]["TotalProceed"].ToString());
                                dtToXML.Rows.Add(dRow);
                            }
                        }
                        //20161213, fauzil, TRBST16240, end
                        StringBuilder stringBuilder = new StringBuilder();
                        dtToXML.WriteXml(System.Xml.XmlWriter.Create(stringBuilder));
                        xmlTransactionLink = stringBuilder.ToString();
                    }
                    //20160310, fauzil, TRBST16240, end

                    DataSet ds = new DataSet();
                    DataSet dsTransaksi = new DataSet("Root");
                    DataTable dtTransaksi = new DataTable("RS");

                    bool bCalculate;
                    Calculate(out bCalculate);

                    ds = SuratBerharga.columnsTransaksi();
                    int iRowColum = ds.Tables[0].Rows.Count;

                    /* Add Column To Data Table*/

                    dsTransaksi.Tables.Add(dtTransaksi);
                    for (int i = 0; i < iRowColum; i++)
                    {
                        dtTransaksi.Columns.Add(ds.Tables[0].Rows[i][0].ToString());
                    }
                    dtTransaksi.Columns.Add("SecurityNo");
                    //20120802, hermanto_salim, BAALN12003, begin
                    //20160721, fauzil, TRBST16240, begin
                    //dtTransaksi.Columns.Add("NoRekInvestor");
                    dtTransaksi.Columns.Add("NoRekInvestorBlock");
                    //20160721, fauzil, TRBST16240, end
                    dtTransaksi.Columns.Add("DoReleaseBlock");
                    dtTransaksi.Columns.Add("SNAME");
                    dtTransaksi.Columns.Add("ACTYPE");
                    dtTransaksi.Columns.Add("SecAccNo");
                    //20120802, hermanto_salim, BAALN12003, end
                    //20220920, yudha.n, BONDRETAIL-1052, begin
                    dtTransaksi.Columns.Add("NoRekMaterai");
                    dtTransaksi.Columns.Add("MateraiCost");
                    dtTransaksi.Columns.Add("IsAbsorbedByBank");
                    //20220920, yudha.n, BONDRETAIL-1052, end
                    //20231025,yazri, BONDRETAIL-1392, begin
                    dtTransaksi.Columns.Add("WeightedSpread");
                    dtTransaksi.Columns.Add("WeightedPrice");
                    dtTransaksi.Columns.Add("TotalSpread");
                    dtTransaksi.Columns.Add("UntungRugiNasabah");
                    dtTransaksi.Columns.Add("WeightedHoldingPeriod");
                    dtTransaksi.Columns.Add("Keterangan");
                    //20231025,yazri, BONDRETAIL-1392, end


                    /* inisialisasi */
                    int iType = Convert.ToInt32(cmbJenisTransaksi.SelectedValue);
                    //20230215, samypasha, BONDRETAIL-1241, begin
                    //string Input = "where SecurityNo='" + cmpsrNomorSekuriti.Text1 + "'";
                    string Input = "where SecurityNo='" + cmpsrNomorSekuriti._Text1.Text.ToString() + "'";
                    //20230215, samypasha, BONDRETAIL-1241, end
                    dsSecId = SuratBerharga.findSecId(Input);
                    int liSecId = Int32.Parse(dsSecId.Tables[0].Rows[0]["SecId"].ToString());

                    //20160310, fauzil, TRBST16240, begin
                    // cek wewenang
                    bool isNeedWMApp = false;
                    bool NeedTreasuryApp = false;
                    string MessagePublishError = "";
                    string MessageModalError = "";

                    //20231227, rezakahfi, BONDRETAIL-1513, begin
                    if (!isTA)
                    {
                        //20240219, pratama, BONDRETAIL-1392, begin
                        //if (SuratBerharga.checkWewenangDeviasi(liSecId, iType, dsSecId.Tables[0].Rows[0]["SecCcy"].ToString(), ndHargaModal.Value, ndHargaPublish.Value, moneyDealPrice.Value, moneyFaceValue.Value, chKaryawan.Checked, out MessagePublishError, out MessageModalError, out NeedTreasuryApp, out isNeedWMApp))
                        //{
                        //if (!string.IsNullOrEmpty(MessagePublishError))
                        //    MessageBox.Show(MessagePublishError, "WARNING!!", MessageBoxButtons.OK);
                        //if (!string.IsNullOrEmpty(MessageModalError))
                        //    MessageBox.Show(MessageModalError, "WARNING!!", MessageBoxButtons.OK);
                        //20180731, samypasha, LOGEN00665, begin
                        //if (_isPVB)
                        //{
                        //    isNeedWMApp = false;
                        //}
                        //20180731, samypasha, LOGEN00665, end
                        //    if (isNeedWMApp)
                        //        MessageBox.Show("Transaksi ini tidak dapat di otorisasi oleh Supervisor cabang karena spread yang digunakan memerlukan approval dari Wealth Management", "WARNING!!", MessageBoxButtons.OK);
                        //}
                        //20240219, pratama, BONDRETAIL-1392, end
                    }
                    //20231227, rezakahfi, BONDRETAIL-1513, end
                    string SourceTrader = "";
                    string DestTrader = "";

                    SuratBerharga.GetDataTraderTRX(out SourceTrader, out DestTrader);
                    if (string.IsNullOrEmpty(SourceTrader))
                    {
                        MessageBox.Show("NIK Trader RM Kosong, data belum dapat disimpan", "WARNING!!", MessageBoxButtons.OK);
                        return;
                    }
                    if (string.IsNullOrEmpty(DestTrader))
                    {
                        MessageBox.Show("NIK Trader Destination RM Kosong, data belum dapat disimpan", "WARNING!!", MessageBoxButtons.OK);
                        return;
                    }
                    //20230215, samypasha, BONDRETAIL-1241, begin
                    //bool isValidDate = SuratBerharga.checkRecordingDate(cmpsrNomorSekuriti.Text1.Trim().ToUpper(), nispSettlementDate.Value);
                    bool isValidDate = SuratBerharga.checkRecordingDate(cmpsrNomorSekuriti._Text1.Text.ToString().Trim().ToUpper(), nispSettlementDate.Value);
                    //20230215, samypasha, BONDRETAIL-1241, end
                    if (isValidDate != true)
                    {
                        MessageBox.Show("Settlement Date tidak valid", "Peringatan");
                        nispSettlementDate.Text = "";
                        nispSettlementDate.Focus();
                        return;
                    }
                    //20160310, fauzil, TRBST16240, end


                    /* Format Inputan XML*/
                    System.Data.DataRow DetailRow = dtTransaksi.NewRow();
                    //20160721, fauzil, TRBST16240, begin
                    //DetailRow["DealNo"] = txtDealNumber.Text.Trim();
                    DetailRow["DealNo"] = cmpsrGetPushBack.Text1.Trim();
                    //20160721, fauzil, TRBST16240, end
                    DetailRow["CIFId"] = CIFid;
                    DetailRow["SecId"] = liSecId;
                    DetailRow["TrxType"] = iType;
                    DetailRow["TrxDate"] = nispDealDate.Value;

                    DetailRow["SettlementDate"] = nispSettlementDate.Value;
                    DetailRow["Tenor"] = 0; //SBI
                    DetailRow["FaceValue"] = moneyFaceValue.Value;
                    DetailRow["Yield"] = 0; // SBI
                    DetailRow["DealYield"] = 0; // SBI
                    DetailRow["SpreadYield"] = 0; // SBI
                    DetailRow["TaxAmount"] = 0; // SBI
                    DetailRow["SafeKeepingFeeAmount"] = 0; // SBI
                    DetailRow["TransactionFeeAmount"] = cmbTrxFee.Text;
                    DetailRow["DealPrice"] = moneyDealPrice.Value;
                    DetailRow["Proceed"] = txtProceed.Value;
                    DetailRow["AccruedDays"] = Convert.ToInt16(txtAccruedDays.Value);
                    DetailRow["AccruedInterest"] = txtAccruedInterest.Value;
                    DetailRow["TaxOnAccrued"] = txtPajakBungaBerjalan.Value;
                    DetailRow["TaxOnCapitalGainLoss"] = txtTaxOnCapitalGainLoss.Value;
                    DetailRow["SafeKeepingFeeAfterTax"] = cmbSafeKeepingFeeAfterTax.Text;
                    DetailRow["TotalProceed"] = txtTotalProceed.Value;
                    DetailRow["TrxStatus"] = "0";
                    //20120802, hermanto_salim, BAALN12003, begin
                    //DetailRow["NIK_CS"] = cmpsrNIK.Text1; // nik cs
                    DetailRow["NIK_CS"] = cmpsrSeller.Text1; // nik cs
                    DetailRow["AccountBlockSequence"] = dtAmendTransaksi.Rows[0]["AccountBlockSequence"];
                    DetailRow["AccountBlockACTYPE"] = dtAmendTransaksi.Rows[0]["AccountBlockACTYPE"];
                    //2016, fauzil, TRBST16240, begin
                    //DetailRow["NoRekInvestor"] = dtAmendTransaksi.Rows[0]["NoRekInvestor"];
                    DetailRow["NoRekInvestorBlock"] = dtAmendTransaksi.Rows[0]["NoRekInvestor"];
                    //2016, fauzil, TRBST16240, end
                    DetailRow["DoReleaseBlock"] = dtAmendTransaksi.Rows[0]["DoReleaseBlock"];
                    //20160303, fauzil, TRBST16240, begin
                    //DetailRow["SNAME"] = dtAmendTransaksi.Rows[0]["SNAME"];
                    //DetailRow["ACTYPE"] = dtAmendTransaksi.Rows[0]["ACTYPE"];
                    if (dtAmendTransaksi.Rows[0]["SNAME"].GetType() != typeof(DBNull))
                        DetailRow["SNAME"] = dtAmendTransaksi.Rows[0]["SNAME"];
                    else
                        DetailRow["SNAME"] = "";
                    if (dtAmendTransaksi.Rows[0]["ACTYPE"].GetType() != typeof(DBNull))
                        DetailRow["ACTYPE"] = dtAmendTransaksi.Rows[0]["ACTYPE"];
                    else
                        DetailRow["ACTYPE"] = "";
                    //20160303, fauzil, TRBST16240, end
                    DetailRow["SecAccNo"] = dtAmendTransaksi.Rows[0]["SecAccNo"];
                    //20120802, hermanto_salim, BAALN12003, end
                    //tambah field NIK_Dealer
                    DetailRow["NIK_Dealer"] = cmpsrDealer.Text1; // NIK_Dealer
                    //end
                    DetailRow["TrxBranch"] = cmpsrSearch1.Text1;
                    DetailRow["InsertedBy"] = null; // nik user login
                    DetailRow["InsertedDate"] = null;

                    DetailRow["LastUpdateBy"] = intNIK;
                    DetailRow["LastUpdateDate"] = FormatDate.ConvertStandardDate(StandardDate.yyyymmdd, strWorkDate, Sparate.blank);
                    DetailRow["SecurityNo"] = dsSecId.Tables[0].Rows[0]["SecurityNo"].ToString();
                    //20160323, Junius, LOGEN00100, begin
                    DetailRow["NIK_Ref"] = cmpsrNIKRef.Text1;
                    //20160323, Junius, LOGEN00100, end
                    //2016, fauzil, TRBST16240, begin
                    DetailRow["HargaORI"] = ndHargaPublish.Value;
                    DetailRow["HargaModal"] = ndHargaModal.Value;
                    //20200917, rezakahfi, BONDRETAIL-550, begin
                    //DetailRow["FlagSwitching"] = chkSwitching.Checked;
                    //DetailRow["DealIdSwitching"] = chkSwitching.Checked ? cmbDealIdSwitching.SelectedValue : "";
                    DataTable dtTable = (DataTable)dgvSourceFund.DataSource;
                    DataRow[] drRowObligasi = dtTable.Select("SourceData = 'Obligasi'");
                    string strDealIdSwitching = "";
                    if (drRowObligasi.Length > 0)
                    {
                        for (int i = 0; i < drRowObligasi.Length; i++)
                        {
                            if (i == 0)
                                strDealIdSwitching = drRowObligasi[0]["DealIdSource"].ToString();
                            else
                                strDealIdSwitching = strDealIdSwitching + ", " + drRowObligasi[i]["DealIdSource"].ToString();
                        }
                        DetailRow["DealIdSwitching"] = strDealIdSwitching;
                        DetailRow["FlagSwitching"] = true;
                    }
                    else
                    {
                        DetailRow["DealIdSwitching"] = "";
                        DetailRow["FlagSwitching"] = false;
                    }
                    if (chkOther.Checked)
                    {
                        DataTable dtTableRek = (DataTable)dgvSourceFund.DataSource;
                        DataRow[] drRowRekening = dtTableRek.Select("SourceData = 'Rekening'");
                        if (drRowRekening.Length > 0)
                        {
                            dcAmountBlock = decimal.Parse(drRowRekening[0]["Amount"].ToString());
                        }
                    }
                    //20200917, rezakahfi, BONDRETAIL-550, end
                    DetailRow["FlagOther"] = chkOther.Checked;
                    DetailRow["BranchProfit"] = 0;
                    DetailRow["NeedTreasuryApp"] = NeedTreasuryApp;
                    DetailRow["NIKSourceTrader"] = SourceTrader;
                    DetailRow["NIKTraderRM"] = cmpsrDealer.Text1 + cmpsrDealer.Text2;
                    DetailRow["NikDestTraderRM"] = DestTrader;
                    if (!string.IsNullOrEmpty(MessagePublishError))
                        DetailRow["TreasuryAppMessage"] = MessagePublishError;
                    else if (!string.IsNullOrEmpty(MessageModalError))
                        DetailRow["TreasuryAppMessage"] = MessageModalError;
                    else
                        DetailRow["TreasuryAppMessage"] = "";
                   //20240219, pratama, BONDRETAIL-1392,begin
                    //keterangan == 1 ? "Transaksi dapat langsung diproses tanpa approval WM" : "Transaksi ini memerlukan approval wm karena spread melebihi max spread yang diperbolehkan"; 
                    if (keterangan == 0)
                    {
                        isNeedWMApp = true;
                    }
                    //nasabah pvb atau bukan kalau kena keterangan approval wm tetep pas otornya ke wm
                    //if (_isPVB)
                    //{
                    //    isNeedWMApp = false;
                    //}
                    //20240219, pratama, BONDRETAIL-1392,end
                    DetailRow["isNeedWMApp"] = isNeedWMApp;
                    if (cmbJenisTransaksi.Text == "Sell")
                    {
                        DetailRow["NoRekInvestor"] = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                        DetailRow["isTaxAmnesty"] = chkTaxAmnesty.Checked;
                    }
                    else
                    {
                        if (FaceValueTA > 0 && FaceValueNonTA > 0)
                        {
                            DetailRow["NoRekInvestor"] = "";
                            DetailRow["isTaxAmnesty"] = false;
                        }
                        else
                        {
                            if (FaceValueTA > 0)
                            {
                                DetailRow["NoRekInvestor"] = NoRekInvestorTA;
                                DetailRow["isTaxAmnesty"] = true;
                            }
                            else if (FaceValueNonTA > 0)
                            {
                                DetailRow["NoRekInvestor"] = NoRekInvestorNonTA;
                                DetailRow["isTaxAmnesty"] = false;
                            }
                        }
                    }
                    DetailRow["HargaModalAwal"] = HargaModalAwal;
                    //20160303, fauzil, TRBST16240, end
                    //20190715, darul.wahid, BOSIT18196, begin
                    DetailRow["PhoneOrderBit"] = chkFlagPhoneOrder.Checked; 
                    if (cmpsrGetPushBack.Visible == true)
                    {
                        DetailRow["PortfolioTA"] = dtAmendTransaksi.Rows[0]["PortfolioTA"];
                        DetailRow["PortfolioInternal"] = dtAmendTransaksi.Rows[0]["PortfolioInternal"];
                    }
                    else
                    {
                        dsPort = new DataSet();
                        string where = " WHERE SecId = '" + liSecId + "'";
                        dsPort = SuratBerharga.findSecId(where);
                        DetailRow["PortfolioTA"] = dsPort.Tables[0].Rows[0]["PortfolioTA"];   
                        DetailRow["PortfolioInternal"] = dsPort.Tables[0].Rows[0]["PortfolioInternal"];
                    }
                    //20190715, darul.wahid, BOSIT18196, end
                    //20220208, darul.wahid, BONDRETAIL-895, begin
                    //20220708, darul.wahid, BONDRETAIL-977, begin
                    //DetailRow["CapitalGain"] = cmbJenisTransaksi.Text == "Buy" ? this.dCapitalGain: 0;
                    //20220208, darul.wahid, BONDRETAIL-895, end
                    if (cmbJenisTransaksi.Text == "Buy")
                        DetailRow["CapitalGain"] = dsSecId.Tables[0].Rows[0]["SecCcy"].ToString() == "IDR" ? this.dCapitalGain : this.dCapitalGainNonIDR;
                    else
                        DetailRow["CapitalGain"] = 0;
                    DetailRow["TotalTax"] = this.dTotalTax;
                    DetailRow["Income"] = this.dIncome;
                    //20220708, darul.wahid, BONDRETAIL-977, end
                    //20220920, yudha.n, BONDRETAIL-1052, begin
                    DetailRow["NoRekMaterai"] = cbRekeningMaterai.SelectedIndex != -1 ? ((KeyValuePair<string, string>)cbRekeningMaterai.SelectedItem).Key : "";
                    DetailRow["MateraiCost"] = this.dMateraiCost;
                    DetailRow["IsAbsorbedByBank"] = this.isMeteraiAbsorbed;
                    //20220920, yudha.n, BONDRETAIL-1052, end
                    //20240422, alfian.andhika, BONDRETAIL-1581, begin
                    DetailRow["YieldHargaModal"] = dYieldHargaModal;
                    //20240422, alfian.andhika, BONDRETAIL-1581, end
                    //20231025,yazri, BONDRETAIL-1392, begin
                    DetailRow["WeightedSpread"] = weightedSpread;
                    DetailRow["WeightedPrice"] = weightedPrice;
                    DetailRow["TotalSpread"] = indikasiTotalSpread;
                    DetailRow["UntungRugiNasabah"] = untungRugiNasabah;
                    DetailRow["WeightedHoldingPeriod"] = weightedHoldingPeriod;
                    DetailRow["Keterangan"] = keterangan;
                    //20231025,yazri, BONDRETAIL-1392, end

                    dtTransaksi.Rows.Add(DetailRow);

                    string xmlFormat = dsTransaksi.GetXml().ToString();
                    DataSet dsSave;
                    //20160303, fauzil, TRBST16240, begin
                    string xmlFormatTA = "";
                    string xmlFormatNonTA = "";
                    if (FaceValueTA > 0 && FaceValueNonTA > 0)
                    {
                        //Tax Amenesty
                        DataSet dsTA = new DataSet();
                        DataSet dsTATransaksi = new DataSet("Root");
                        DataTable dtTATransaksi = new DataTable("RS");
                        dsTA = SuratBerharga.columnsTransaksi();
                        int iRowColumTA = dsTA.Tables[0].Rows.Count;

                        /* Add Column To Data Table*/
                        dsTATransaksi.Tables.Add(dtTATransaksi);
                        for (int i = 0; i < iRowColumTA; i++)
                        {
                            dtTATransaksi.Columns.Add(dsTA.Tables[0].Rows[i][0].ToString());
                        }
                        dtTATransaksi.Columns.Add("SecurityNo");

                        /* Format Inputan XML*/
                        System.Data.DataRow DetailRowTA = dtTATransaksi.NewRow();

                        DetailRowTA["CIFId"] = CIFid;
                        DetailRowTA["SecId"] = iSecId;
                        DetailRowTA["TrxType"] = iType;
                        DetailRowTA["TrxDate"] = nispDealDate.Value;
                        DetailRowTA["SettlementDate"] = nispSettlementDate.Value;
                        DetailRowTA["Tenor"] = 0;
                        DetailRowTA["FaceValue"] = transaksiBankBeliTA.FaceValue;
                        DetailRowTA["Yield"] = 0;
                        DetailRowTA["DealYield"] = 0;
                        DetailRowTA["SpreadYield"] = 0;
                        DetailRowTA["TaxAmount"] = 0;
                        DetailRowTA["SafeKeepingFeeAmount"] = 0;
                        DetailRowTA["TransactionFeeAmount"] = transaksiBankBeliTA.TransactionFee;
                        DetailRowTA["DealPrice"] = moneyDealPrice.Value;
                        DetailRowTA["Proceed"] = transaksiBankBeliTA.Proceed;
                        DetailRowTA["AccruedDays"] = Convert.ToInt16(transaksiBankBeliTA.AccruedDays);
                        DetailRowTA["AccruedInterest"] = transaksiBankBeliTA.Interest;
                        DetailRowTA["TaxOnAccrued"] = transaksiBankBeliTA.TaxOnAccrued;
                        DetailRowTA["TaxOnCapitalGainLoss"] = transaksiBankBeliTA.TaxOnCapitalGL;
                        DetailRowTA["SafeKeepingFeeAfterTax"] = transaksiBankBeliTA.SafeKeepingFeeAmount;
                        DetailRowTA["TotalProceed"] = transaksiBankBeliTA.TotalProceed;
                        DetailRowTA["TrxStatus"] = "0";
                        DetailRowTA["NIK_CS"] = cmpsrSeller.Text1;
                        DetailRowTA["NIK_Dealer"] = cmpsrDealer.Text1;
                        DetailRowTA["TrxBranch"] = cmpsrSearch1.Text1;
                        DetailRowTA["InsertedBy"] = intNIK;
                        DetailRowTA["InsertedDate"] = FormatDate.ConvertStandardDate(StandardDate.yyyymmdd, strWorkDate, Sparate.blank);
                        DetailRowTA["LastUpdateBy"] = null;
                        DetailRowTA["LastUpdateDate"] = null;
                        DetailRow["SecurityNo"] = dsSecId.Tables[0].Rows[0]["SecurityNo"].ToString();
                        DetailRowTA["PhoneOrderBit"] = chkFlagPhoneOrder.Checked;
                        DetailRowTA["NIK_Ref"] = cmpsrNIKRef.Text1;
                        DetailRowTA["NoRekInvestor"] = transaksiBankBeliTA.NoRekInvestor;
                        DetailRowTA["isTaxAmnesty"] = transaksiBankBeliTA.IsTaxAmnesty;
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        //20220708, daurl.wahid, BONDRETAIL-977, begin
                        //DetailRowTA["CapitalGain"] = transaksiBankBeliTA.CapitalGain;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        DetailRowTA["CapitalGain"] = dsSecId.Tables[0].Rows[0]["SecCcy"].ToString() == "IDR" ? transaksiBankBeliTA.CapitalGain : transaksiBankBeliTA.CapitalGainNonIdr;
                        DetailRowTA["TotalTax"] = transaksiBankBeliTA.TotalTax;
                        DetailRowTA["Income"] = transaksiBankBeliTA.Income;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        DetailRowTA["YieldHargaModal"] = transaksiBankBeliTA.YieldHargaModal;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                        //20231025,yazri, BONDRETAIL-1392, begin
                        DetailRowTA["WeightedSpread"] = weightedSpread;
                        DetailRowTA["WeightedPrice"] = weightedPrice;
                        DetailRowTA["TotalSpread"] = indikasiTotalSpread;
                        DetailRowTA["UntungRugiNasabah"] = untungRugiNasabah;
                        DetailRowTA["WeightedHoldingPeriod"] = weightedHoldingPeriod;
                        DetailRowTA["Keterangan"] = keterangan;
                        //20231025,yazri, BONDRETAIL-1392, end

                        dtTATransaksi.Rows.Add(DetailRowTA);
                        xmlFormatTA = dsTATransaksi.GetXml().ToString();

                        //Tax Amenesty
                        DataSet dsNonTA = new DataSet();
                        DataSet dsNonTATransaksi = new DataSet("Root");
                        DataTable dtNonTATransaksi = new DataTable("RS");
                        dsNonTA = SuratBerharga.columnsTransaksi();
                        int iRowColumNonTA = dsNonTA.Tables[0].Rows.Count;

                        /* Add Column To Data Table*/
                        dsNonTATransaksi.Tables.Add(dtNonTATransaksi);
                        for (int i = 0; i < iRowColumNonTA; i++)
                        {
                            dtNonTATransaksi.Columns.Add(dsNonTA.Tables[0].Rows[i][0].ToString());
                        }
                        dtNonTATransaksi.Columns.Add("SecurityNo");

                        /* Format Inputan XML*/
                        System.Data.DataRow DetailRowNonTA = dtNonTATransaksi.NewRow();

                        DetailRowNonTA["CIFId"] = CIFid;
                        DetailRowNonTA["SecId"] = iSecId;
                        DetailRowNonTA["TrxType"] = iType;
                        DetailRowNonTA["TrxDate"] = nispDealDate.Value;
                        DetailRowNonTA["SettlementDate"] = nispSettlementDate.Value;
                        DetailRowNonTA["Tenor"] = 0;
                        DetailRowNonTA["FaceValue"] = transaksiBankBeliNonTA.FaceValue;
                        DetailRowNonTA["Yield"] = 0;
                        DetailRowNonTA["DealYield"] = 0;
                        DetailRowNonTA["SpreadYield"] = 0;
                        DetailRowNonTA["TaxAmount"] = 0;
                        DetailRowNonTA["SafeKeepingFeeAmount"] = 0;
                        DetailRowNonTA["TransactionFeeAmount"] = transaksiBankBeliNonTA.TransactionFee;
                        DetailRowNonTA["DealPrice"] = moneyDealPrice.Value;
                        DetailRowNonTA["Proceed"] = transaksiBankBeliNonTA.Proceed;
                        DetailRowNonTA["AccruedDays"] = Convert.ToInt16(transaksiBankBeliNonTA.AccruedDays);
                        DetailRowNonTA["AccruedInterest"] = transaksiBankBeliNonTA.Interest;
                        DetailRowNonTA["TaxOnAccrued"] = transaksiBankBeliNonTA.TaxOnAccrued;
                        DetailRowNonTA["TaxOnCapitalGainLoss"] = transaksiBankBeliNonTA.TaxOnCapitalGL;
                        DetailRowNonTA["SafeKeepingFeeAfterTax"] = transaksiBankBeliNonTA.SafeKeepingFeeAmount;
                        DetailRowNonTA["TotalProceed"] = transaksiBankBeliNonTA.TotalProceed;
                        DetailRowNonTA["TrxStatus"] = "0";
                        DetailRowNonTA["NIK_CS"] = cmpsrSeller.Text1;
                        DetailRowNonTA["NIK_Dealer"] = cmpsrDealer.Text1;
                        DetailRowNonTA["TrxBranch"] = cmpsrSearch1.Text1;
                        DetailRowNonTA["InsertedBy"] = intNIK;
                        DetailRowNonTA["InsertedDate"] = FormatDate.ConvertStandardDate(StandardDate.yyyymmdd, strWorkDate, Sparate.blank);
                        DetailRowNonTA["LastUpdateBy"] = null;
                        DetailRowNonTA["LastUpdateDate"] = null;
                        DetailRowNonTA["SecurityNo"] = dsSecId.Tables[0].Rows[0]["SecurityNo"].ToString();
                        DetailRowNonTA["PhoneOrderBit"] = chkFlagPhoneOrder.Checked;
                        DetailRowNonTA["NIK_Ref"] = cmpsrNIKRef.Text1;
                        DetailRowNonTA["NoRekInvestor"] = transaksiBankBeliNonTA.NoRekInvestor;
                        DetailRowNonTA["isTaxAmnesty"] = transaksiBankBeliNonTA.IsTaxAmnesty;
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //DetailRowNonTA["CapitalGain"] = transaksiBankBeliNonTA.CapitalGain;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        DetailRowNonTA["CapitalGain"] = dsSecId.Tables[0].Rows[0]["SecCcy"].ToString() == "IDR" ? transaksiBankBeliNonTA.CapitalGain : transaksiBankBeliNonTA.CapitalGainNonIdr;
                        DetailRowNonTA["TotalTax"] = transaksiBankBeliNonTA.TotalTax;
                        DetailRowNonTA["Income"] = transaksiBankBeliNonTA.Income;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        DetailRowNonTA["YieldHargaModal"] = transaksiBankBeliNonTA.YieldHargaModal;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                        //20231025,yazri, BONDRETAIL-1392, begin
                        DetailRowNonTA["WeightedSpread"] = weightedSpread;
                        DetailRowNonTA["WeightedPrice"] = weightedPrice;
                        DetailRowNonTA["TotalSpread"] = indikasiTotalSpread;
                        DetailRowNonTA["UntungRugiNasabah"] = untungRugiNasabah;
                        DetailRowNonTA["WeightedHoldingPeriod"] = weightedHoldingPeriod;
                        DetailRowNonTA["Keterangan"] = keterangan;
                        //20231025,yazri, BONDRETAIL-1392, end
                        dtNonTATransaksi.Rows.Add(DetailRowNonTA);
                        xmlFormatNonTA = dsNonTATransaksi.GetXml().ToString();
                    }
                    else if (FaceValueTA > 0)
                    {
                        xmlFormatTA = dsTransaksi.GetXml().ToString();
                        xmlFormatNonTA = null;
                    }
                    else if (FaceValueNonTA > 0)
                    {
                        xmlFormatNonTA = dsTransaksi.GetXml().ToString();
                        xmlFormatTA = null;
                    }
                    //20210315, rezakahfi, BONDRETAIL-703, begin
                    string xmlSumberDana = getXMLSumberDana();
                    if (xmlSumberDana == "" && chkOther.Checked)
                        return;
                    //20210315, rezakahfi, BONDRETAIL-703, end

                    //20220221, darul.wahid, BONDRETAIL-892, begin
                    //20220406, darul.wahid, BONDRETAIL-927, begin
                    if (gbSumberDana.Visible == true)
                    {
                        if (!ValidateSumberDana(SuratBerharga))
                            return;
                    }
                    //20220406, darul.wahid, BONDRETAIL-927, end
                    //20220221, darul.wahid, BONDRETAIL-892, end

                    //20160303, fauzil, TRBST16240, end
                    //20120802, hermanto_salim, BAALN12003, begin
                    //bool bSaveXML = SuratBerharga.amendSuratBerharga(xmlFormat, out dsSave);
                    //20160310, fauzil, TRBST16240, begin
                    //bool bSaveXML = SuratBerharga.amendSuratBerhargaWithBlock(xmlFormat, clsCallWebService, out dsSave);
                    //20200917, rezakahfi, BONDRETAIL-550, begin
                    //bool bSaveXML = SuratBerharga.amendSuratBerhargaWithBlock(xmlFormat, clsCallWebService, out dsSave, xmlTransactionLink, AccountType, xmlFormatTA, xmlFormatNonTA);
                    bool bSaveXML = SuratBerharga.amendSuratBerhargaWithBlock(xmlFormat, clsCallWebService, out dsSave, xmlTransactionLink, AccountType, xmlFormatTA, xmlFormatNonTA, dcAmountBlock, xmlSumberDana);
                    //20200917, rezakahfi, BONDRETAIL-550, end
                    //20160310, fauzil, TRBST16240, end
                    //20120802, hermanto_salim, BAALN12003, end
                    //20220920, yudha.n, BONDRETAIL-1052, begin
                    DealId = dsSave.Tables[0].Rows[0][0].ToString();
                    if (updateMateraiAccountBlockSequence != 0 && !isMeteraiAbsorbed)
                    {
                        if (!_clsBlokirSaldo.RelaseBlockRekening(updateNorekMeterai, AccountTypeMaterai, "Meterai", updateMateraiAccountBlockSequence, null))
                        {
                            throw new Exception("Data Gagal Tersimpan");
                        }
                    }
                    //20240301, pratama, BONDRETAIL-1392, begin
                    //if (dcMateraiAmountBlock > 0 && updateMateraiAccountBlockSequence == 0 && !isMeteraiAbsorbed)
                    if (dcMateraiAmountBlock > 0 && !isMeteraiAbsorbed)
                    //20240301, pratama, BONDRETAIL-1392, end
                    {
                        DateTime expDate = DateTime.ParseExact(nispDealDate.Value.ToString(), "yyyyMMdd", null).AddDays(1);
                        string jenisTransaksi = (string)(cmbJenisTransaksi.SelectedItem as DataRowView).Row["TrxDesc"];
                        bSaveXML = SuratBerharga.BlokirRekeningBiayaMaterai(_clsBlokirSaldo, dcMateraiAmountBlock, AccountTypeMaterai, DetailRow["NoRekMaterai"].ToString(), DealId, jenisTransaksi, out BlockSequenceMaterai, expDate.ToString("ddMMyy"));

                    }
                    //20220920, yudha.n, BONDRETAIL-1052, end
                    if (bSaveXML == true)
                    {
                        MessageBox.Show("Data Success Tersimpan\n Deal Number : " + dsSave.Tables[0].Rows[0][0].ToString() + "\n ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        //20121127, hermanto_salim, BAALN12003, begin
                        strFlag = "";
                        //20121127, hermanto_salim, BAALN12003, end
                        //20221020, Tobias Renal, HFUNDING-181, Begin
                        updateSimanis();
                        //20221020, Tobias Renal, HFUNDING-181, End
                        controlsEnabled(false);
                        controlsClear(true);
                        //20121127, hermanto_salim, BAALN12003, begin
                        //strFlag = "";
                        //20121127, hermanto_salim, BAALN12003, end

                        //20090910, David, SYARIAH001, begin
                        //disabled object utk mencari deal number
                        lblDealNumber.Visible = false;
                        //20160721, fauzil, TRBST16240, begin
                        //txtDealNumber.Visible = false;
                        //btnCari.Visible = false;
                        cmpsrGetPushBack.Visible = false;
                        cmpsrGetPushBack.Text1 = "";
                        cmpsrGetPushBack.Text2 = "";
                        //20160721, fauzil, TRBST16240, end

                        this.NISPToolbarButton("2").Visible = true;
                        //20130107, victor, BAALN12003, begin
                        //this.NISPToolbarButton("4").Visible = false;
                        //20130107, victor, BAALN12003, end
                        this.NISPToolbarButton("6").Visible = true;
                        this.NISPToolbarButton("7").Visible = true;
                        //20090910, David, SYARIAH001, end
                        //20130307, uzia, BAFEM12016, begin
                        chkFlagPhoneOrder.Enabled = false;
                        //20130307, uzia, BAFEM12016, end
                    }
                    else
                    {
                        MessageBox.Show("Data Gagal Tersimpan", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nispDealDate.Focus();
                    }

                }
                else
                {
                    //20160516, fauzil, TRBST16240, Begin
                    //MessageBox.Show("Data Ada Yang Kosong", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    string errorMessage = "";
                    if (cmpsrNoRekSecurity.Text1.Length == 0)
                        errorMessage = errorMessage + "No Rekening Security,";
                    if (cmpsrSearch1.Text1.Length == 0)
                        errorMessage = errorMessage + "Kode Cabang,";
                    if (cmpsrNomorSekuriti.Text1.Length == 0)
                        errorMessage = errorMessage + "Nomor Security,";
                    if (nispDealDate.Text.Length == 0)
                        errorMessage = errorMessage + "Dael Date,";
                    if (moneyFaceValue.Value == 0)
                        errorMessage = errorMessage + "Face Value,";
                    if (txtAccruedInterest.Text.Length == 0)
                        errorMessage = errorMessage + "Accrued Intereset,";
                    if (txtProceed.Value == 0)
                        errorMessage = errorMessage + "Proceed,";
                    if (txtTotalProceed.Value == 0)
                        errorMessage = errorMessage + "Total Proceed,";
                    if (cmpsrSeller.Text1 == "")
                        errorMessage = errorMessage + "Seller,";
                    if (cmpsrDealer.Text1 == "")
                        errorMessage = errorMessage + "Dealer,";
                    errorMessage = errorMessage.Substring(0, errorMessage.Length - 1);
                    MessageBox.Show(errorMessage + " Tidak Boleh Kosong", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //20160516, fauzil, TRBST16240, end                                              
                    nispDealDate.Focus();
                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                MessageBox.Show("Error Update Method : Tidak Teridentifikasi");
            }
        }
        //20090910, David, SYARIAH001, end

        public void controlsEnabled(bool State)
        {
            btnCalculate.Enabled = State;
            cmpsrNomorSekuriti.Enabled = true;
            cmpsrNomorSekuriti._Text1.Enabled = true;
            cmpsrNomorSekuriti._Text1.BackColor = Color.LightYellow;
            cmpsrNomorSekuriti._Text2.BackColor = Color.LightYellow;
            cmpsrSearch1.Enabled = true;
            cmpsrSearch1._Text1.Enabled = true;
            cmpsrSearch1._Text1.BackColor = Color.LightYellow;
            cmpsrSearch1._Text2.BackColor = Color.LightYellow;
            cmbJenisTransaksi.Enabled = true;
            cmpsrNoRekSecurity.Enabled = State;
            txtNamaNasabah.Enabled = false;
            txtNamaNasabah.BackColor = Color.LightYellow;
            txtCouponRate.Enabled = false;
            txtCouponRate.BackColor = Color.LightYellow;
            //20160720, fauzil, TRBST16240, begin
            //nispDealDate.Enabled = State;
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            //nispDealDate.Enabled = false;
            nispDealDate.Enabled = (isTA ? State : false);
            //20231227, rezakahfi, BONDRETAIL-1513, end
            //20160720, fauzil, TRBST16240, end
            //20160310, fauzil, TRBST16240, begin
            //nispSettlementDate.Enabled = state;
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            //nispSettlementDate.Enabled = false;
            nispSettlementDate.Enabled = (isTA ? State : false);
            //20231227, rezakahfi, BONDRETAIL-1513, end
            //20160310, fauzil, TRBST16240, end
            txtAccruedDays.Enabled = false;
            txtAccruedDays.BackColor = Color.LightYellow;
            //20120802, hermanto_salim, BAALN12003, begin  
            //moneyFaceValue.Enabled = State;
            bool isBuy = cmbJenisTransaksi.Text == "Buy";
            //20220113, rezakahfi, BONDRETAIL-877, begin
            //moneyFaceValue.Enabled = !isBuy && State;
            moneyFaceValue.Enabled = State;
            //20220113, rezakahfi, BONDRETAIL-877, end
            //20120802, hermanto_salim, BAALN12003, end  
            txtTaxOnCapitalGainLoss.Enabled = false;
            txtTaxOnCapitalGainLoss.BackColor = Color.LightYellow;
            txtAccruedInterest.Enabled = false;
            txtAccruedInterest.BackColor = Color.LightYellow;
            //20180321, uzia, LOGAMxxx, begin
            //txtTaxTarif.Enabled = State;
            txtTaxTarif.Enabled = false;
            //20180321, uzia, LOGAMxxx, end
            moneyDealPrice.Enabled = State;
            txtProceed.Enabled = false;
            txtProceed.BackColor = Color.LightYellow;
            txtPajakBungaBerjalan.Enabled = false;
            txtPajakBungaBerjalan.BackColor = Color.LightYellow;
            //20171219, agireza, TRBST16240, begin
            //cmbSafeKeepingFeeAfterTax.Enabled = State;
            //20171219, agireza, TRBST16240, end
            // txtSafeKeepingFeeAfterTax.BackColor = Color.LightYellow;
            //20121023, hermanto_salim, BAALN12003, begin
            //cmbTrxFee.Enabled = State;
            cmbTrxFee.Enabled = false;
            //20121023, hermanto_salim, BAALN12003, end
            txtTotalProceed.Enabled = false;
            txtTotalProceed.BackColor = Color.LightYellow;
            //20120802, hermanto_salim, BAALN12003, begin  
            //cmpsrNIK.Enabled = State;
            cmpsrSeller.Enabled = State;
            //20121023, hermanto_salim, BAALN12003, begin
            //20171219, agireza, TRBST16240, begin
            //chkTransactionFee.Enabled = State;
            //20171219, agireza, TRBST16240, end
            //20121127, hermanto_salim, BAALN12003, begin
            if (strFlag != "Search")
                //20121127, hermanto_salim, BAALN12003, end
                chkTransactionFee.Checked = State;
            //20121023, hermanto_salim, BAALN12003, end
            //20120802, hermanto_salim, BAALN12003, end
            //20211012, irene, BONDRETAIL-829, begin
            //cmpsrDealer.Enabled = State;
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            //cmpsrDealer.Enabled = false;
            cmpsrDealer.Enabled = (isTA ? State : false);
            //20231227, rezakahfi, BONDRETAIL-1513, end
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            if (!isTA)
            {
                cmpsrDealer.Text1 = intNIK.ToString();
                cmpsrDealer.ValidateField();
            }
            //20231227, rezakahfi, BONDRETAIL-1513, end
            //20211012, irene, BONDRETAIL-829, end
            //20160225, fauzil, TRBST16240, begin
            cmpsrNIKRef.Enabled = true;
            chkOther.Enabled = State;
            ndHargaModal.Enabled = State;
            ndHargaPublish.Enabled = false;
            //20220920, yudha.n, BONDRETAIL-1052, begin
			cbRekeningMaterai.Enabled = true;
            txtMateraiCost.Enabled = false;
            txtMateraiCost.BackColor = Color.LightYellow;
            //20220920, yudha.n, BONDRETAIL-1052, end
            //20200917, rezakahfi, BONDRETAIL-550, begin
            //chkSwitching.Enabled = State;
            //cmbDealIdSwitching.Enabled = State;
            //20200917, rezakahfi, BONDRETAIL-550, end
            //20160225, fauzil, TRBST16240, end
        }
        //20161018, fauzil, TRBST16240, begin
        //public void controlsClear()
        public void controlsClear(bool erase)
        //20161018, fauzil, TRBST16240, end
        {
            txtNamaNasabah.Text = "";
            txtNamaNasabah.Text = "";
            //20161018, fauzil, TRBST16240, begin
            if (erase)
            {
                //20161018, fauzil, TRBST16240, end
                cmpsrNomorSekuriti.Text1 = "";
                cmpsrNomorSekuriti.Text2 = "";
                //20161018, fauzil, TRBST16240, begin
            }
            //20161018, fauzil, TRBST16240, end
            //cmpsrSearch1.Text1 = "";
            //cmpsrSearch1.Text2 = "";
            cmpsrNoRekSecurity.Text1 = "";
            cmpsrNoRekSecurity.Text2 = "";
            nispDealDate.Text = "";
            nispSettlementDate.Text = "";
            txtCouponRate.Value = 0;
            cmbJenisTransaksi.SelectedIndex = -1;
            txtAccruedDays.Value = 0;
            moneyFaceValue.Value = 0;
            txtAccruedInterest.Value = 0;
            txtTaxTarif.Value = 0;
            moneyDealPrice.Value = 0;
            txtProceed.Value = 0;
            txtPajakBungaBerjalan.Value = 0;
            cmbSafeKeepingFeeAfterTax.Items.Clear();
            cmbSafeKeepingFeeAfterTax.Items.Add(0);
            cmbTrxFee.Items.Clear();
            cmbTrxFee.Items.Add(0);
            txtTotalProceed.Value = 0;
            txtTaxOnCapitalGainLoss.Value = 0;
            //20120802, hermanto_salim, BAALN12003, begin  
            //cmpsrNIK.Text1 = "";
            //cmpsrNIK.Text2 = "";
            //20120802, hermanto_salim, BAALN12003, end  
            cmpsrDealer.Text1 = "";
            cmpsrDealer.Text2 = "";
            //20090910, David, SYARIAH001, begin
            txtDealNumber.Text = "";
            //20090910, David, SYARIAH001, end
            //20120802, hermanto_salim, BAALN12003, begin  
            cmpsrSeller.Text1 = "";
            cmpsrSeller.Text2 = "";
            txtNoSertifikasiSeller.Text = "";
            txtTglExpiredSertifikasi.Text = "";
            txtRekeningRelasi.Text = "";
            txtRekeningRelasiName.Text = "";
            dgvTransactionLink.DataSource = null;
            //20120802, hermanto_salim, BAALN12003, end  
            //20121127, hermanto_salim, BAALN12003, begin
            iTransactionFee = 0;
            //20121127, hermanto_salim, BAALN12003, end  
            //20130307, uzia, BAFEM12016, begin
            //20190919, darul.wahid, BOSIT18196, begin
            //chkFlagPhoneOrder.Checked = false;
            //20190919, darul.wahid, BOSIT18196, end
            //20130307, uzia, BAFEM12016, end
            //20160818, fauzil, LOGEN196, begin
            cbRekeningRelasi.DataSource = null;
            chkTaxAmnesty.Checked = false;
            //20160818, fauzil, LOGEN196, end
            //20160211, samy, TRBST16240, begin
            ndHargaModal.Value = 0;
            ndHargaPublish.Value = 0;
            //20160211, samy, TRBST16240, end
            //20160225, fauzil, TRBST16240, begin
            cmpsrNIKRef.Enabled = false;
            cmpsrNIKRef.Text1 = "";
            cmpsrNIKRef.Text2 = "";
            //20200917, rezakahfi, BONDRETAIL-550, begin
            //chkSwitching.Checked = false;
            //20200917, rezakahfi, BONDRETAIL-550, end
            chKaryawan.Checked = false;
            //20160225, fauzil, TRBST16240, end
            //20160301, fauzil, TRBST16240, begin
            chkOther.Checked = false;
            //20190116, samypasha, BOSOD18243, begin
            //cmbSumberDana.DataSource = null;
            //ndMaturSumberDana.Value = 0;
            //if (!flagClear)
            //{
            //    if (dgvSumberDana.Rows.Count > 0)
            //        dgvSumberDana.Rows.Clear();
            //}
            //else
            //    dgvSumberDana.DataSource = null;
            //20200604, uzia, BONDRETAIL-438, begin
            txtSaldoRekening.Value = 0;
            //20200604, uzia, BONDRETAIL-438, end
            txtSaldoRekening.Text = "";            
            if (!flagClear)
            {
                if (dgvSourceFund.Rows.Count > 0)
//201190723, rezakahfi, LOGAM10236, begin
                    //dgvSourceFund.Rows.Clear();
                {
                    createTableSource();
                }
//201190723, rezakahfi, LOGAM10236, end
            }
            else
                dgvSourceFund.DataSource = null;
            //20190116, samypasha, BOSOD18243, end
            flagClear = false;
            txtFuncGrp.Text = "";
            //20160301, fauzil, TRBST16240, end
            //20180118, uzia, TRBST16240, begin
            txtYTM.Text = "";
            //20180118, uzia, TRBST16240, end
            //20180123, uzia, TRBST16240, begin
            //if (cmbDealIdSwitching.DataSource != null)
            //    cmbDealIdSwitching.DataSource = null;
            //cmbDealIdSwitching.Text = "";
            //20180123, uzia, TRBST16240, end
			//20220920, yudha.n, BONDRETAIL-1052, begin
            dMateraiCost = 0;
            dUpdateMateraiCost = 0;
            txtMateraiCost.Value = 0;
            cbRekeningMaterai.DataSource = null;
            txtSaldoRekeningMaterai.Value = 0;
            txtSaldoRekeningMaterai.Text = "";
            isMeteraiAbsorbed = false;
            updateNorekMeterai = "";
            _bCalculate = false;
            //20220920, yudha.n, BONDRETAIL-1052, end
            //20221020, Tobias Renal, HFUNDING-181, Begin
            txtKodeSales.Text = "";
            txtKeteranganSimanis.Text = "";
            //20221020, Tobias Renal, HFUNDING-181, End
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            txtTenor.Value = 0;
            txtBondType.Clear();
            nispMaturityDate.Text = "";
            txtTotalAmountSource.Text = "0";
            //20231227, rezakahfi, BONDRETAIL-1513, end
        }

        public bool numberValidator(string strInput)
        {
            return Regex.IsMatch(strInput, "^([0-9])*$");
        }

        public bool stringValidator(string strInput)
        {
            return Regex.IsMatch(strInput, "^([a-z][A-Z])*$");
        }

        public bool dateValidator(string strInput)
        {
            return Regex.IsMatch(strInput, "^\\d{1,2}\\/\\d{1,2}\\/\\d{2,4}$");
        }

        public bool NoRekSecurityValidator(string strInput)
        {
            return Regex.IsMatch(strInput, "^[N]{1,1}\\d{9,9}$");
        }

        public void StateTransaction()
        {
            controlsEnabled(true);
            //20160818, fauzil, LOGEN196, begin
            cmpsrNoRekSecurity.Focus();
            //20160818, fauzil, LOGEN196, end
            strFlag = "Insert";

            //MoneyTransactionFee.Value = 20000;
            int iTypeTrx = Convert.ToInt32(cmbJenisTransaksi.SelectedValue);
            if (iTypeTrx == 4)
            {
                /* Sell */
                // txtTax.Visible = true;

                txtTaxOnCapitalGainLoss.Visible = false;
                lblTaxOnCapitalGainLoss.Visible = false;
                cmbSafeKeepingFeeAfterTax.Visible = false;
                lblSafeKeepingFee.Visible = false;
                txtPajakBungaBerjalan.Visible = false;
                lblTaxOnAccrued.Visible = false;
                //txtTaxTarif.Value = 0;
                txtTaxTarif.Visible = false;
                lblTaxTarif.Visible = false;
                labelPersenTaxTarif.Visible = false;
                //20160818, fauzil, LOGEN196, begin
                if (cbRekeningRelasi.Items.Count > 1)
                {
                    if (cbRekeningRelasi.Items.Count > 2)
                    {
                        cbRekeningRelasi.Enabled = true;
                        cbRekeningRelasi.SelectedIndex = 0;
                    }
                    else
                    {
                        cbRekeningRelasi.Enabled = false;
                        cbRekeningRelasi.SelectedIndex = 1;
                    }
                }
                //20160818, fauzil, LOGEN196, end
                //20160210, samy, TRBST16240, begin
                //20200917, rezakahfi, BONDRETAIL-550, begin
                //labelSwitching.Visible = true;
                //chkSwitching.Visible = true;
                //20200917, rezakahfi, BONDRETAIL-550, end
                //20160210, samy, TRBST16240, end
                //20160301, fauzil, TRBST16240, begin
                gbSumberDana.Visible = true;
                //20160301, fauzil, TRBST16240, end
            }
            else
            {
                /* Buy */
                // txtTax.Visible = false;
                txtTaxOnCapitalGainLoss.Visible = true;
                lblTaxOnCapitalGainLoss.Visible = true;
                cmbSafeKeepingFeeAfterTax.Visible = true;
                lblSafeKeepingFee.Visible = true;
                txtPajakBungaBerjalan.Visible = true;
                lblTaxOnAccrued.Visible = true;
                txtTaxTarif.Visible = true;
                lblTaxTarif.Visible = true;
                //txtTaxTarif.Value = 20;
                labelPersenTaxTarif.Visible = true;
                //20160818, fauzil, LOGEN196, begin
                if (cbRekeningRelasi.Items.Count > 1)
                {
                    if (cbRekeningRelasi.Items.Count > 2)
                    {
                        cbRekeningRelasi.Enabled = false;
                        cbRekeningRelasi.SelectedIndex = 0;
                    }
                    else
                    {
                        cbRekeningRelasi.Enabled = false;
                        cbRekeningRelasi.SelectedIndex = 1;
                    }
                }
                //20160818, fauzil, LOGEN196, end
                //20160210, samy, TRBST16240, begin
                //20200917, rezakahfi, BONDRETAIL-550, begin
                //labelSwitching.Visible = false;
                //chkSwitching.Visible = false;
                //chkSwitching.Checked = false;
                //20200917, rezakahfi, BONDRETAIL-550, end
                //20160210, samy, TRBST16240, end
                //20160301, fauzil, TRBST16240, begin
                gbSumberDana.Visible = false;
                //20160301, fauzil, TRBST16240, end

            }
        }

        private void cmbJenisTransaksi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmpsrNomorSekuriti.Text1.Length != 0 && cmpsrSearch1.Text1.Length != 0)
            {
                StateTransaction();
            }

        }

        private void cmbJenisTransaksi_Validating(object sender, CancelEventArgs e)
        {

            if (cmbJenisTransaksi.SelectedIndex == -1)
            {
                // MessageBox.Show("Pilih Jenis Transaksi Terlebih Dahulu!", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbJenisTransaksi.Focus();
            }
            else if (cmbJenisTransaksi.SelectedIndex != -1 && cmpsrNomorSekuriti.Text1.Length != 0 && cmpsrSearch1.Text1.Length != 0)
            {
                //20160225, fauzil, TRBST16240, begin
                if (trxType.Equals("SELL", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.Parse(cmbJenisTransaksi.SelectedValue.ToString()) == 3)
                    {
                        MessageBox.Show("Hanya dapat melakukan transaksi Sell", "Information", MessageBoxButtons.OK);
                        cmbJenisTransaksi.SelectedValue = 4;
                    }
                }
                else if (trxType.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.Parse(cmbJenisTransaksi.SelectedValue.ToString()) == 4)
                    {
                        MessageBox.Show("Hanya dapat melakukan transaksi Buy", "Information", MessageBoxButtons.OK);
                        cmbJenisTransaksi.SelectedValue = 3;
                    }
                }

                //20160225, fauzil, TRBST16240, end

                controlsEnabled(true);

                TransaksiSuratBerharga SB = new TransaksiSuratBerharga();
                System.Data.DataSet dsHari = new System.Data.DataSet();
                System.Data.DataSet dsLusa = new System.Data.DataSet();
                dsHari = SB.GetWorkingDate(0, 0);
                //20160225, fauzil, TRBST16240, begin
                //dsLusa = SB.GetWorkingDate(1, 2);
                dsLusa = SB.GetWorkingDate(2, 2);
                //20160225, fauzil, TRBST16240, end

                DataSet DefaultSettlementDate = new DataSet();
                //20220331, darul.wahid, ONEMBL-1279, begin
                //DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti.Text1);
                int JenisTrx = int.Parse(cmbJenisTransaksi.SelectedValue.ToString());
                //20230215, samypasha, BONDRETAIL-1241, begin
                //DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti.Text1, JenisTrx, "CB");
                DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti._Text1.Text.ToString(), JenisTrx, "CB");
                //20230215, samypasha, BONDRETAIL-1241, end
                //20220331, darul.wahid, ONEMBL-1279, end

                if (DefaultSettlementDate.Tables[0].Rows[0][0].GetType() != typeof(DBNull))
                    nispSettlementDate.Text = DefaultSettlementDate.Tables[0].Rows[0][0].ToString();
                else
                    nispSettlementDate.Text = dsLusa.Tables[0].Rows[0][0].ToString();

                if (DefaultSettlementDate.Tables[0].Rows[0][1].GetType() != typeof(DBNull))
                    nispDealDate.Text = DefaultSettlementDate.Tables[0].Rows[0][1].ToString();
                else
                    nispDealDate.Text = dsHari.Tables[0].Rows[0][0].ToString();
                //20231227, rezakahfi, BONDRETAIL-1513, begin
                if (string.IsNullOrEmpty(nispMaturityDate.Text) && cmpsrNomorSekuriti._Text1.Text.ToString() != "")
                {
                    DataRow drSecMaster = InqData.InquiryDataSecurity(cmpsrNomorSekuriti._Text1.Text.ToString());
                    if (drSecMaster.ItemArray.Length > 0)
                    {
                        nispMaturityDate.Value = int.Parse(DateTime.Parse(drSecMaster["MaturityDate"].ToString()).ToString("yyyyMMdd"));
                        txtBondType.Text = drSecMaster["SecTypeDesc"].ToString().Trim();
                        if (drSecMaster["ImbalHasil"].ToString().Contains("1"))
                        {
                            if (!tcImbalHasil.TabPages.Contains(pgKupon))
                                tcImbalHasil.TabPages.Add(pgKupon);
                            if (tcImbalHasil.TabPages.Contains(pgDiskonto))
                                tcImbalHasil.TabPages.Remove(pgDiskonto);
                        }
                        else
                        {
                            if (!tcImbalHasil.TabPages.Contains(pgDiskonto))
                                tcImbalHasil.TabPages.Add(pgDiskonto);
                            if (tcImbalHasil.TabPages.Contains(pgKupon))
                                tcImbalHasil.TabPages.Remove(pgKupon);
                        }
                    }
                }
                DateTime SettDate = DateTime.ParseExact(nispSettlementDate.Text, "dd-MM-yyyy", null);
                DateTime MattDate = DateTime.ParseExact(nispMaturityDate.Text, "dd-MM-yyyy", null);
                txtTenor.Value = decimal.Parse((MattDate - SettDate).TotalDays.ToString());

                //20231227, rezakahfi, BONDRETAIL-1513, end
                //20171124, agireza, TRBST16240, begin
                txtAccruedDays.Text = DefaultSettlementDate.Tables[0].Rows[0]["AccruedDays"].ToString();
                //20171124, agireza, TRBST16240, end

                //string Hari = dsHari.Tables[0].Rows[0][0].ToString();
                //string lusa = dsLusa.Tables[0].Rows[0][0].ToString();

                //nispDealDate.Text = Hari;
                //nispSettlementDate.Text = lusa;
                //20160225, fauzil, TRBST16240, end

                strFlag = "Insert";
                //20160114, samy, TRBST16240, begin
                PopulateHarga();
                //20160114, samy, TRBST16240, end

                //20160302, fauzil, TRBST16240, begin
                //if (nispSettlementDate.Value > 0 & cmpsrNoRekSecurity.Text1 != "" & Currency.Length > 0)
                //    GetDataDealIdForSwitching();
                //20160302, fauzil, TRBST16240, end
            }
        }

        //20221020, Tobias Renal, HFUNDING-181, Begin
        public bool saveSimanis(string DealId)
        {
            DataSet ds;
            OleDbParameter[] dbParam = new OleDbParameter[4];

            (dbParam[0] = new OleDbParameter("@pcKodeSales", OleDbType.VarChar, 50)).Value = txtKodeSales.Text;
            (dbParam[1] = new OleDbParameter("@pcKeterangan", OleDbType.VarChar, 50)).Value = txtKeteranganSimanis.Text;
            (dbParam[2] = new OleDbParameter("@pcNIKInputter", OleDbType.VarChar, 50)).Value = intNIK.ToString();
            (dbParam[3] = new OleDbParameter("@pcDealId", OleDbType.VarChar, 50)).Value = DealId;

            bool blnResult = cQuery.ExecProc("TRSInsertSimanis", ref dbParam, out ds);
            return blnResult;
        }
        //20221020, Tobias Renal, HFUNDING-181, End

        //20221020, Tobias Renal, HFUNDING-181, Begin
        public bool updateSimanis()
        {
            DataSet ds;
            OleDbParameter[] dbParam = new OleDbParameter[4];

            (dbParam[0] = new OleDbParameter("@pcDealId", OleDbType.VarChar, 50)).Value = cmpsrGetPushBack.Text1;
            (dbParam[1] = new OleDbParameter("@pcKodeSales", OleDbType.VarChar, 50)).Value = txtKodeSales.Text;
            (dbParam[2] = new OleDbParameter("@pcKeterangan", OleDbType.VarChar, 50)).Value = txtKeteranganSimanis.Text;
            (dbParam[3] = new OleDbParameter("@pcNIKInputter", OleDbType.VarChar, 50)).Value = intNIK.ToString();

            bool blnResult = cQuery.ExecProc("TRSUpdateSimanis", ref dbParam, out ds);
            return blnResult;
        }

        public bool getSimanisData(string DealNo, out System.Data.DataSet ds)
        {
            try
            {
                OleDbParameter[] dbParam = new OleDbParameter[1];

                (dbParam[0] = new OleDbParameter("@pcDealId", OleDbType.VarChar, 50)).Value = DealNo;

                bool blnResult = cQuery.ExecProc("dbo.TRSPopulateSimanis", ref dbParam, out ds);
                return blnResult;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
                ds = null;
                return false;
            }
        }
        //20221020, Tobias Renal, HFUNDING-181, End
        //20160114, samy, TRBST16240, begin
        public void PopulateHarga()
        {
            //TRSPopulateDataTransaksiSuratBerharga
            DataSet dshargaORI;
            OleDbParameter[] dbParam = new OleDbParameter[3];
            //20230215, samypasha, BONDRETAIL-1241, begin
            //(dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti.Text1;
            (dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti._Text1.Text.ToString();
            //20230215, samypasha, BONDRETAIL-1241, end
            (dbParam[1] = new OleDbParameter("@pcJenisTransaksi", OleDbType.VarChar, 30)).Value = cmbJenisTransaksi.Text;
            (dbParam[2] = new OleDbParameter("@pcParameter", OleDbType.VarChar, 50)).Value = "HargaOri";

            bool blnResult = cQuery.ExecProc("TRSPopulateDataTransaksiSuratBerharga", ref dbParam, out dshargaORI);

            //20211005, rezakahfi, BONDRETAIL-730, begin
            if (blnResult)
            {
            //20211005, rezakahfi, BONDRETAIL-730, end
                if (dshargaORI.Tables[0].Rows.Count > 0)
                {
                    ////20231227, rezakahfi, BONDRETAIL-1513, begin
                    //ndHargaPublish.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());
                    //moneyDealPrice.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());

                    if (tcImbalHasil.TabPages.Contains(pgDiskonto))
                    {
                        decimal dcPrice = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());

                        if (cmbJenisTransaksi.Text.Contains("Sell"))
                        {
                            dcPrice = Math.Ceiling(dcPrice * 100) / 100;
                        }
                        else
                        {
                            dcPrice = Math.Floor(dcPrice * 100) / 100;
                        }

                        ndHargaPublish.Value = dcPrice;
                        moneyDealPrice.Value = dcPrice;
                    }
                    else
                    {
                        ndHargaPublish.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());
                        moneyDealPrice.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());
                    }
                    //20231227, rezakahfi, BONDRETAIL-1513, end
                }

                if (dshargaORI.Tables[1].Rows.Count > 0)
                {
                    ndHargaModal.Value = decimal.Parse(dshargaORI.Tables[1].Rows[0][0].ToString());
                }

                if (dshargaORI.Tables.Count > 2)
                {
                    if (dshargaORI.Tables[2].Rows.Count > 0)
                    {
                        //20211012, rezakahfi, BONDRETAIL-814, begin
                        //MessageBox.Show("Terdapat Perubahan Harga Modal yang belum di-approved", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //ndHargaPublish.Value = 0;
                        //moneyDealPrice.Value = 0;
                        //ndHargaModal.Value = 0;
                        //controlsClear(true);
                        bool bStop = false;
                        for (int i = 0; i < dshargaORI.Tables[2].Rows.Count; i++)
                        {
                            if (bool.Parse(dshargaORI.Tables[2].Rows[i]["Stoper"].ToString())
                                && ( dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "All"
                                    || dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "CB"
                                    )
                                )
                            {
                                MessageBox.Show(dshargaORI.Tables[2].Rows[i]["Description"].ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                bStop = true;
                            }
                            else if (!bool.Parse(dshargaORI.Tables[2].Rows[i]["Stoper"].ToString())
                                && ( dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "All"
                                    || dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "CB"
                                    ))
                            {
                                if (MessageBox.Show(dshargaORI.Tables[2].Rows[i]["Description"].ToString(), "Warnings", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                    bStop = true;
                            }
                        }

                        if (bStop)
                        {
                            ndHargaPublish.Value = 0;
                            moneyDealPrice.Value = 0;
                            ndHargaModal.Value = 0;

                            controlsClear(true);
                        }
                        //20211012, rezakahfi, BONDRETAIL-814, end
                    }
                }
            //20211005, rezakahfi, BONDRETAIL-730, begin
            }
            else
            {
                ndHargaPublish.Value = 0;
                moneyDealPrice.Value = 0;
                ndHargaModal.Value = 0;
                controlsClear(true);
            }
            //20211005, rezakahfi, BONDRETAIL-730, end
        }
        //20160114, samy, TRBST16240, end
        private void txtNilaiPokok_Validating(object sender, CancelEventArgs e)
        {
            bool bValid = numberValidator(moneyDealPrice.Text);
            if (bValid == false)
            {
                MessageBox.Show("Masukan Angka", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                moneyDealPrice.Focus();
            }
        }

        private void txtJangkaWaktu_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (txtAccruedDays.Text.Length > 0)
            {
                if (e.KeyValue == 13 || e.KeyValue == 9)
                {
                    bool bValid = numberValidator(txtAccruedDays.Text);
                    if (bValid == false)
                    {
                        MessageBox.Show("Masukan Angka", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtAccruedDays.Focus();
                    }
                    else if (bValid == true)
                    {
                        if (txtAccruedDays.Text.Length > 3)
                        {
                            MessageBox.Show("Jangka Waktu  Tidak Lebih dari 3 Digit", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtAccruedDays.Focus();
                        }
                    }
                }
            }
            else if (txtAccruedDays.Text.Length == 0)
            {
                if (e.KeyValue == 13 || e.KeyValue == 9)
                {
                    MessageBox.Show("Tidak Boleh Kosong!", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAccruedDays.Focus();
                }
            }
        }

        public void ListDataAwal()
        {
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            DataSet ds = new DataSet();
            DataSet dsJthTempo = new DataSet();
            //20160226, fauzil, TRBST16240, begin
            DataSet dsRefar = new DataSet();
            //20160226, fauzil, TRBST16240, end
            //20160301, fauzil, TRBST16240, begin
            DataSet dsSwitching = new DataSet();
            //20160301, fauzil, TRBST16240, end
            //20221122, yazri, VSYARIAH-310, begin
            DataSet dsRekening = new DataSet();
            //20221122, yazri, VSYARIAH-310,end
            //20220920, yudha.n, BONDRETAIL-1052, begin
            DataSet dsRekMaterai = new DataSet();
            //20220920, yudha.n, BONDRETAIL-1052, end

            try
            {
                //20160818, fauzil, LOGEN196, begin
                //20230215, samypasha, BONDRETAIL-1241, begin
                //if (!SuratBerharga.getRekeningTaxAmnesty(cQuery, cmpsrNoRekSecurity.Text1.Trim(), cmpsrNomorSekuriti.Text1.Trim(), out NoRekInvestorTaxAmnesty, out NamaRekInvestorTaxAmnesty))
                if (!SuratBerharga.getRekeningTaxAmnesty(cQuery, cmpsrNoRekSecurity.Text1.Trim(), cmpsrNomorSekuriti._Text1.Text.ToString().Trim(), out NoRekInvestorTaxAmnesty, out NamaRekInvestorTaxAmnesty))
                //20230215, samypasha, BONDRETAIL-1241, end
                    return;
                //20160818, fauzil, LOGEN196, end
                string strInput = cmpsrNoRekSecurity.Text1;
                //20230215, samypasha, BONDRETAIL-1241, begin
                //string Input = "where SecurityNo='" + cmpsrNomorSekuriti.Text1 + "'";
                string Input = "where SecurityNo='" + cmpsrNomorSekuriti._Text1.Text.ToString() + "'";
                //20230215, samypasha, BONDRETAIL-1241, end
                //20130122, victor, BAALN12003, begin
                //20230215, samypasha, BONDRETAIL-1241, begin
                //string SecurityNo = cmpsrNomorSekuriti.Text1;
                string SecurityNo = cmpsrNomorSekuriti._Text1.Text.ToString();
                //20230215, samypasha, BONDRETAIL-1241, end
                //20130122, victor, BAALN12003, end

                //20130122, victor, BAALN12003, begin
                //ds = SuratBerharga.findSecAccNo(strInput);
                ds = SuratBerharga.findSecAccNo(strInput, SecurityNo);
                //20130122, victor, BAALN12003, end
                dsJthTempo = SuratBerharga.findSecId(Input);

                //20221122, yazri, VSYARIAH-310, begin
                dsRekening = SuratBerharga.searchRekening(strInput, SecurityNo);
                //20221122, yazri, VSYARIAH-310, end
                //20220920, yudha.n, BONDRETAIL-1052, begin
                dsRekMaterai = SuratBerharga.searchRekeningMaterai(strInput, SecurityNo);
                //20220920, yudha.n, BONDRETAIL-1052, end

                //20130305, uzia, BAALN12003, begin
                bool isPremier = false;
                bool isHavingPhoneOrder = false;

                //20160226, fauzil, TRBST16240, begin
                dsRefar = SuratBerharga.findNikNameReferentorAndStatusKaryawan(strInput);
                if (dsRefar.Tables[0].Rows.Count > 0)
                {
                    if (dsRefar.Tables[0].Rows[0]["FlagKaryawan"].GetType() != typeof(DBNull))
                        chKaryawan.Checked = bool.Parse(dsRefar.Tables[0].Rows[0]["FlagKaryawan"].ToString());
                    else
                        chKaryawan.Checked = false;
                }
                //20160226, fauzil, TRBST16240, end

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //20181210, vanny_w, BOSIT18196, begin
                    if (ds.Tables[0].Rows[0]["Email"].ToString() == "" && (!bool.Parse(ds.Tables[0].Rows[0]["ApprovalNNH"].ToString()) ||
                        ds.Tables[0].Rows[0]["TanggalApprovalNNH"].ToString() == "" || ds.Tables[0].Rows[0]["TanggalApprovalNNH"].ToString() == "0"))
                    {
                        MessageBox.Show("Tidak dapat melakukan transaksi karena Email atau Approval NNH kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20181210, vanny_w, BOSIT18196, end
                    //20170830, imelda, COPOD17271 , begin
                    if (ds.Tables[0].Rows[0]["RiskProfileCode"] != System.DBNull.Value)
                    {
                        _RiskProfileCust = Convert.ToChar(ds.Tables[0].Rows[0]["RiskProfileCode"]);
                    }
                    else
                    {
                        MessageBox.Show("Harap Update Data Nasabah Terlebih Dahulu");
                        controlsClear(true);
                        return;
                    }
                    //20170830, imelda, COPOD17271 , end
                    CIFNo = ds.Tables[0].Rows[0]["CIFNo"].ToString();
                    
                    //20171109, agireza, TRBST16240, begin
                    _strSpouseName = ds.Tables[0].Rows[0]["Spouse"].ToString();
                    //20171109, agireza, TRBST16240, end
                    //20180213, uzia, LOGEN00584, begin
                    this._strMaritalStatus = ds.Tables[0].Rows[0]["MaritalStatus"].ToString();
                    //20180213, uzia, LOGEN00584, end
                    
                    //20170829, agireza, COPOD17271, begin
                    //20180731, samypasha, LOGEN00665, begin
                    if ((strClassificationId == "372" || strClassificationId == "373" || strClassificationId == "375"))
                    {
                        //do nothing
                    }
                    else
                    {
                    //20180731, samypasha, LOGEN00665, end
                        string ErrMsg = "";
                        DataSet dsOut = new DataSet();
                        if (!clsCallWebService.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(CIFNo, strBranchCode, this.strClassificationId, out ErrMsg, out dsOut))//dapet akses private banking
                        {
                            if (ErrMsg != "")
                            {
                                MessageBox.Show(ErrMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("Nasabah Private Banking, Silahkan menghubungi CBCO ONT atau RM Private Banking", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                controlsClear(true);
                                return;
                            }
                        }
                    //20180731, samypasha, LOGEN00665, begin
                    }
                    //20180731, samypasha, LOGEN00665, end
                    //20170829, agireza, COPOD17271, end
                    //20180731, samypasha, LOGEN00665, begin
                     string cErrMsg = "";
                    string strResultPVB = "";
                    DataSet dsOut2 = new DataSet();
                    if (clsCallWebService.CallCIFInquiryInquiryParameterCustomerFlagging13150(CIFNo, out strResultPVB, out cErrMsg))
                    {
                        System.IO.StringReader srResultTest = new System.IO.StringReader(strResultPVB);
                        System.Data.DataSet dsResult = new System.Data.DataSet();
                        dsResult.ReadXml(srResultTest);

                        dsResult.Tables[0].TableName = "PVBTable";

                        if (dsResult.Tables[0].Rows[0]["FlagType"].ToString() == "PV")
                        {
                            _isPVB = true;
                        }
                        else
                        {
                            _isPVB = false;
                        }
                    }
                    else
                    {
                        _isPVB = false;
                    }
                    if ((strClassificationId == "372" || strClassificationId == "373" || strClassificationId == "375") && !_isPVB)
                    {
                        MessageBox.Show("Nasabah bukan Private Banking", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        controlsClear(true);
                        return;
                    }
                    //20180731, samypasha, LOGEN00665, end
                    ValidatePhoneOrderFlag(out isPremier, out isHavingPhoneOrder);
                    //20130607, uzia, LOGAM05465, begin
                    //if(!txtDealNumber.Visible)
                    //    chkFlagPhoneOrder.Enabled = (!isPremier || !isHavingPhoneOrder ? false : true);
                    //20190919, darul.wahid, BOSIT18196, begin
                    //if (this.NISPToolbarButton("6").Visible == true)
                    //    chkFlagPhoneOrder.Enabled = (!isPremier || !isHavingPhoneOrder ? false : true);
                    //else
                    //    chkFlagPhoneOrder.Enabled = false;
                    //20190919, darul.wahid, BOSIT18196, end
                    //20130607, uzia, LOGAM05465, end
                    //20190715, darul.wahid, BOSIT18196, begin
                    if (isHavingPhoneOrder || isPremier)
                        chkFlagPhoneOrder.Enabled = true;
                    else
                        chkFlagPhoneOrder.Enabled = false;                                        
                    //20190715, darul.wahid, BOSIT18196, end
                    if (!chkFlagPhoneOrder.Enabled)
                        chkFlagPhoneOrder.Checked = false;
                    //20160226, fauzil, TRBST16240, begin
                    //if (ds.Tables[0].Rows[0]["NikReferentor"].GetType() != typeof(DBNull))
                    //{
                    //    cmpsrNIKRef.Text1 = ds.Tables[0].Rows[0]["NikReferentor"].ToString();
                    //    cmpsrNIKRef.ValidateField();
                    //}                    
                    //20160226, fauzil, TRBST16240, end
                    if (ds.Tables[0].Rows[0]["Functional"].GetType() != typeof(DBNull))
                        txtFuncGrp.Text = ds.Tables[0].Rows[0]["Functional"].ToString();
                }
                //20130305, uzia, BAALN12003, end

                if (dsJthTempo.Tables[0].Rows.Count > 0)
                {
                    //nispDateJatuhTempo.Text = dsJthTempo.Tables[0].Rows[0]["MaturityDate"].ToString().Replace("/", "-");
                    //DateTime strLastCoupoDate = dsJthTempo.Tables[0].Rows[0]["LastCouponDate"] ;
                    //string strAccuredDays = strLastCoupoDate - nispSettlementDate.Value ;
                    iSecId = Int32.Parse(dsJthTempo.Tables[0].Rows[0]["SecId"].ToString());
                    txtCouponRate.Text = dsJthTempo.Tables[0].Rows[0]["CouponRate"].ToString();
                    iInterestTypeID = Int32.Parse(dsJthTempo.Tables[0].Rows[0]["InterestTypeID"].ToString());
                    
                    //20171109, agireza, TRBST16240, begin
                    _bIsCorporateBond = Convert.ToBoolean(dsJthTempo.Tables[0].Rows[0]["IsCorporateBond"]);
                    //20171109, agireza, TRBST16240, end

                    dMinSafeKeepingFee = Convert.ToDecimal(dsJthTempo.Tables[0].Rows[0]["MinSafeKeepingFee"].ToString());
                    decimal trxFee = Convert.ToDecimal(dsJthTempo.Tables[0].Rows[0]["TransactionFee"].ToString());
                    if (strFlag != "Update")
                    {
                        cmbTrxFee.Items.Clear();
                        cmbTrxFee.Items.Add(0);
                    }
                    //20120802, hermanto_salim, BAALN12003, begin
                    //cmbTrxFee.Items.Add(trxFee);
                    //cmbTrxFee.Text = trxFee.ToString().Trim();
                    //20120802, hermanto_salim, BAALN12003, end
                    decimal safeKeepFee = Convert.ToDecimal(dsJthTempo.Tables[0].Rows[0]["SafeKeepingFeeTaxTarif"].ToString());
                    cmbSafeKeepingFeeAfterTax.Items.Clear();
                    cmbSafeKeepingFeeAfterTax.Items.Add(0);
                    cmbSafeKeepingFeeAfterTax.Items.Add(safeKeepFee);
                    cmbSafeKeepingFeeAfterTax.Text = safeKeepFee.ToString().Trim();                    
                    //txtSafeKeepingFeePercetage.Text = dsJthTempo.Tables[0].Rows[0]["SafeKeepingFeeTarif"].ToString();
                    //20160229, fauzil, TRBST16240, begin
                    Currency = dsJthTempo.Tables[0].Rows[0]["SecCcy"].ToString();
                    //20160229, fauzil, TRBST15176, end
                    //20170508, agireza, TRBST16240, begin
                    if (dsJthTempo.Tables[0].Rows[0]["UsingEarlyRedemption"].ToString() != "")
                        this._bEarlyRedemption = Convert.ToBoolean(dsJthTempo.Tables[0].Rows[0]["UsingEarlyRedemption"].ToString());
                    else
                        this._bEarlyRedemption = true;
                    //20170508, agireza, TRBST16240, end
                    //20181210, vanny_w, BOSIT18196, begin
                    if(_bIsCorporateBond && Currency == "IDR" && ds.Tables[0].Rows[0]["Email"].ToString() == "")
                    {
                        MessageBox.Show("Tidak dapat melakukan transaksi Corporate BOND IDR karena Email kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20181210, vanny_w, BOSIT18196, end

                }

                //20170125, agireza, TRBST16249, begin
                if (ds.Tables[0].Rows.Count > 0 && dsJthTempo.Tables[0].Rows.Count > 0)
                {
                    if (ds.Tables[0].Rows[0]["Citizenship"].ToString() == "000"
                        || ds.Tables[0].Rows[0]["Citizenship"].ToString() == ""
                        //20200121, dion, TR12020-1, BONDRETAIL-82, begin
                        //20170524, agireza, LOGAM8788, begin
                        //|| (ds.Tables[0].Rows[0]["Citizenship"].ToString() != "000" && ds.Tables[0].Rows[0]["NPWP"].ToString() != "")
                        //20170524, agireza, LOGAM8788, end
                        || (ds.Tables[0].Rows[0]["Citizenship"].ToString() != "000"
                            && ds.Tables[0].Rows[0]["NPWP"].ToString() != ""
                            && ds.Tables[0].Rows[0]["KITASNo"].ToString() != ""
                            && DateTime.ParseExact(ds.Tables[0].Rows[0]["KITASExpDate"].ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture).Date > DateTime.Now.Date
                            )
                        //20200121, dion, TR12020-1, BONDRETAIL-82, end
                        )
                        txtTaxTarif.Value = Convert.ToDecimal(dsJthTempo.Tables[0].Rows[0]["Tax"].ToString());
                    else
                        txtTaxTarif.Value = Convert.ToDecimal(dsJthTempo.Tables[0].Rows[0]["TaxWNAWithoutNPWP"].ToString());
                }
                //20170125, agireza, TRBST16249, end
                //20120802, hermanto_salim, BAALN12003, begin    
                effectiveBalance = null;
                //20130211, victor, BAALN12003, begin
                AccountStatus = null;
                //20130211, victor, BAALN12003, end
                //20130222, victor, BAALN12003, begin
                ProductCode = "";
                AccountType = "";
                //20130222, victor, BAALN12003, end

                if (cmbJenisTransaksi.Text == "Buy")
                {
                    //20230215, samypasha, BONDRETAIL-1241, begin
                    //if (dgvTransactionLink.DataSource == null || lastSecAccNo != cmpsrNoRekSecurity.Text1 || lastSecurityNo != cmpsrNomorSekuriti.Text1)
                    if (dgvTransactionLink.DataSource == null || lastSecAccNo != cmpsrNoRekSecurity.Text1 || lastSecurityNo != cmpsrNomorSekuriti._Text1.Text.ToString())
                    //20230215, samypasha, BONDRETAIL-1241, end
                    {
                        DataSet dsResult = new DataSet();
                        lastSecAccNo = cmpsrNoRekSecurity.Text1;
                        //20230215, samypasha, BONDRETAIL-1241, begin
                        //lastSecurityNo = cmpsrNomorSekuriti.Text1;
                        //if (clsDatabase.subTRSPopulateTransactionBalance(cQuery, new object[] { cmpsrNoRekSecurity.Text1, cmpsrNomorSekuriti.Text1 }, out dsResult))
                        lastSecurityNo = cmpsrNomorSekuriti._Text1.Text.ToString();
                        if (clsDatabase.subTRSPopulateTransactionBalance(cQuery, new object[] { cmpsrNoRekSecurity.Text1, cmpsrNomorSekuriti._Text1.Text.ToString() }, out dsResult))
                        //20230215, samypasha, BONDRETAIL-1241, end
                        {
                            dgvTransactionLink.DataSource = dsResult.Tables[0];

                            for (int i = 0; i < dgvTransactionLink.ColumnCount; i++)
                            {
                                dgvTransactionLink.Columns[i].Visible = false;
                                //20220113, rezakahfi, BONDRETAIL-877, begin
                                dgvTransactionLink.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                                //20220113, rezakahfi, BONDRETAIL-877, end
                            }

                            for (int i = 0; i < dgvTransactionLinkColumnProperty.Rows.Count; i++)
                            {
                                string columnName = (string)dgvTransactionLinkColumnProperty.Rows[i]["ColumnName"];
                                dgvTransactionLink.Columns[columnName].ReadOnly = (bool)dgvTransactionLinkColumnProperty.Rows[i]["IsReadOnly"];
                                dgvTransactionLink.Columns[columnName].Frozen = (bool)dgvTransactionLinkColumnProperty.Rows[i]["IsFrozen"];
                                dgvTransactionLink.Columns[columnName].Visible = true;
                                //20221220, yazri, VSYARIAH-340, begin
                                if (dgvTransactionLink.Columns[columnName].Name == "NoRekInvestor")
                                    dgvTransactionLink.Columns[columnName].Visible = false;
                                //20221220, yazri, VSYARIAH-340, end
                                dgvTransactionLink.Columns[columnName].HeaderText = (string)dgvTransactionLinkColumnProperty.Rows[i]["ColumnAlias"];
                            }
                        }
                    }
                }
                else
                {
                    dgvTransactionLink.DataSource = null;
                }
                //20120802, hermanto_salim, BAALN12003, end
                if (cmpsrNoRekSecurity.Text1.Length != 0)
                {
                    if (ds != null)
                    {
                        if (ds.Tables.Count > 0)
                        {
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                //20161206, agireza, TRBST16249, begin
                                if (ds.Tables[0].Rows[0]["SID"].ToString() == "")
                                {
                                    _strSID = "";
                                    MessageBox.Show("Nasabah tidak memiliki SID. Harap menghubungi Treasury Operation (email ke opd_tso@ocbcnisp.com) dengan menyertakan scan KTP / Paspor dan Nomor Rekening Security Nasabah", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                                else
                                {
                                    _strSID = ds.Tables[0].Rows[0]["SID"].ToString();
                                }
                                subResetToolBar();
                                //20161206, agireza, TRBST16249, end
                                txtNamaNasabah.Text = ds.Tables[0].Rows[0]["Nama"].ToString();
                                CIFid = Int32.Parse(ds.Tables[0].Rows[0]["CIFId"].ToString());
                                //20120802, hermanto_salim, BAALN12003, begin 
                                //20160818, fauzil, LOGEN196, begin
                                //string rekeningRelasi = ds.Tables[0].Rows[0]["NoRekInvestor"].GetType() == typeof(DBNull) ? null : (string)ds.Tables[0].Rows[0]["NoRekInvestor"];
                                //20221122, yazri, VSYARIAH-310, begin
								//string rekeningRelasi = ds.Tables[0].Rows[0]["NoRekInvestor"].GetType() == typeof(DBNull) ? "" : (string)ds.Tables[0].Rows[0]["NoRekInvestor"];
                                string rekeningRelasi = ds.Tables[0].Rows[0]["NoRekInvestor"].ToString().GetType() == typeof(DBNull) ? "" : (string)ds.Tables[0].Rows[0]["NoRekInvestor"].ToString();
                                //20221122, yazri, VSYARIAH-310, end
                                //txtRekeningRelasi.Text = rekeningRelasi == null ? "" : rekeningRelasi;
                                //txtRekeningRelasiName.Text = ds.Tables[0].Rows[0]["SNAME"].GetType() == typeof(DBNull) ? "" : (string)ds.Tables[0].Rows[0]["SNAME"];
                                //if (rekeningRelasi != null)
                                //{
                                //    cbRekeningRelasi.SelectedIndex = cbRekeningRelasi.FindString(rekeningRelasi);
                                //    cbRekeningRelasi.Enabled = false;

                                //    string result = "";
                                //    string rejectDesc = "";
                                //    clsCallWebService.CallAccountInquiry(strGuid, intNIK.ToString(), rekeningRelasi, out result, out rejectDesc);

                                //    if (string.IsNullOrEmpty(rejectDesc) && !string.IsNullOrEmpty(result))
                                //    {
                                //        DataSet dsData = new DataSet();
                                //        dsData.ReadXml(new StringReader(result));
                                //        effectiveBalance = Convert.ToDecimal(dsData.Tables[0].Rows[0]["ATMEffectiveBalance"], System.Globalization.CultureInfo.InvariantCulture);

                                //        //20130211, victor, BAALN12003, begin
                                //        AccountStatus = Convert.ToInt16(dsData.Tables[0].Rows[0]["AccountStatus"]);
                                //        //20130211, victor, BAALN12003, end
                                //        //20130222, victor, BAALN12003, begin
                                //        ProductCode = dsData.Tables[0].Rows[0]["ProductCode"].ToString();
                                //        AccountType = dsData.Tables[0].Rows[0]["AccountType"].ToString();
                                //        //20130222, victor, BAALN12003, end
                                //    }
                                //} 
                                //20220920, yudha.n, BONDRETAIL-1052, begin
                                string rekeningMaterai = "";
                                string rekeningMateraiName = "";
                                Dictionary<string, string> itemRekMaterai = new Dictionary<string, string>();
                                itemRekMaterai.Clear();
                                itemRekMaterai.Add("-pilih-", "");
                                if (NoRekInvestorTaxAmnesty.Trim().Length > 0)
                                    itemRekMaterai.Add(NoRekInvestorTaxAmnesty, NamaRekInvestorTaxAmnesty);

                                if (dsRekMaterai.Tables.Count > 0)
                                {
                                    isMeteraiAbsorbed = strFlag.Equals("Update") ? isMeteraiAbsorbed : dsRekMaterai.Tables[0].Rows.Count == 0;
                                    if (isMeteraiAbsorbed)
                                    {
                                        effectiveBalanceMaterai = null;
                                    }

                                    for (int i = 0; i < dsRekMaterai.Tables[0].Rows.Count; i++)
                                    {
                                        rekeningMaterai = dsRekMaterai.Tables[0].Rows[i]["NoRekInvestor"].ToString().GetType() == typeof(DBNull) ? "" : (string)dsRekMaterai.Tables[0].Rows[i]["NoRekInvestor"].ToString();
                                        rekeningMateraiName = dsRekMaterai.Tables[0].Rows[i]["SNAME"].GetType() == typeof(DBNull) ? "" : (string)dsRekMaterai.Tables[0].Rows[i]["SNAME"];
                                        itemRekMaterai.Add(rekeningMaterai.Trim(), rekeningMateraiName.Trim());
                                    }
                                }
                                cbRekeningMaterai.DisplayMember = "Key";
                                cbRekeningMaterai.ValueMember = "Value";
                                cbRekeningMaterai.DataSource = new BindingSource(itemRekMaterai, null);
                                if (cmbJenisTransaksi.Text == "Sell")
                                {
                                    if (isMeteraiAbsorbed)
                                    {
                                        cbRekeningMaterai.SelectedIndex = -1;
                                    }
                                    else if (cbRekeningMaterai.Items.Count > 1)
                                    {
                                        if (cbRekeningMaterai.Items.Count > 2)
                                        {
                                            cbRekeningMaterai.Enabled = true;
                                            cbRekeningMaterai.SelectedIndex = 0;
                                        }
                                        else
                                        {
                                            cbRekeningMaterai.Enabled = false;
                                            cbRekeningMaterai.SelectedIndex = 1;
                                        }
                                    }
                                    if (strFlag.Equals("Update"))
                                    {
                                        cbRekeningMaterai.Text = updateNorekMeterai;
                                        cbRekeningMaterai.Enabled = false;
                                    }
                                }
                                else
                                {
                                    if (isMeteraiAbsorbed)
                                    {
                                        cbRekeningMaterai.SelectedIndex = -1;
                                    }
                                    else if (cbRekeningMaterai.Items.Count > 1)
                                    {
                                        if (cbRekeningMaterai.Items.Count > 2)
                                        {
                                            cbRekeningMaterai.Enabled = true;
                                            cbRekeningMaterai.SelectedIndex = 0;
                                        }
                                        else
                                        {
                                            cbRekeningMaterai.Enabled = true;
                                            cbRekeningMaterai.SelectedIndex = 1;
                                        }
                                    }

                                    if (strFlag.Equals("Update"))
                                    {
                                        if (updateNorekMeterai.Length > 0)
                                        {
                                            cbRekeningMaterai.Text = updateNorekMeterai;
                                        }
                                        cbRekeningMaterai.Enabled = false;
                                    }
                                }
                                //20220920, yudha.n, BONDRETAIL-1052, end
                                string rekeneingRelasiName = ds.Tables[0].Rows[0]["SNAME"].GetType() == typeof(DBNull) ? "" : (string)ds.Tables[0].Rows[0]["SNAME"];
                                Dictionary<string, string> item = new Dictionary<string, string>();
                                item.Clear();
                                item.Add("-pilih-", "");
                                //20221017, yazri, VSYARIAH-278, begin
                                bool bSyariah = false;
                                bool.TryParse(dsJthTempo.Tables[0].Rows[0]["FlagSyariah"].ToString(), out bSyariah);
                                
                                for (int i = 0; i < dsRekening.Tables[0].Rows.Count; i++)
                                {
                                    rekeningRelasi = dsRekening.Tables[0].Rows[i]["NoRekInvestor"].ToString().GetType() == typeof(DBNull) ? "" : (string)dsRekening.Tables[0].Rows[i]["NoRekInvestor"].ToString();
                                    rekeneingRelasiName = dsRekening.Tables[0].Rows[i]["SNAME"].GetType() == typeof(DBNull) ? "" : (string)dsRekening.Tables[0].Rows[i]["SNAME"];
                                    item.Add(rekeningRelasi.Trim(), rekeneingRelasiName.Trim());
                                }
								//20221017, yazri, VSYARIAH-278, end
								
								//20221122, yazri, VSYARIAH-310, begin
                                //if (rekeningRelasi.Trim().Length > 0)
								//20221122, yazri, VSYARIAH-310, end
                                    //20160909, Junius, LOGAM08236, begin
                                    //item.Add(rekeningRelasi.Trim(), txtRekeningRelasiName.Text.Trim());
									//20221122, yazri, VSYARIAH-310, begin
                                    //item.Add(rekeningRelasi.Trim(), rekeneingRelasiName.Trim());
									//20221122, yazri, VSYARIAH-310, end
                                //20160909, Junius, LOGAM08236, end
								

                                if (NoRekInvestorTaxAmnesty.Trim().Length > 0)
                                    item.Add(NoRekInvestorTaxAmnesty, NamaRekInvestorTaxAmnesty);

                                cbRekeningRelasi.DataSource = null;
                                cbRekeningRelasi.DataSource = new BindingSource(item, null);
                                cbRekeningRelasi.DisplayMember = "Key";
                                cbRekeningRelasi.ValueMember = "Value";
                                if (cmbJenisTransaksi.Text == "Sell")
                                {
                                    //20221017, yazri, VSYARIAH-278, begin
									//if (cbRekeningRelasi.Items.Count > 1)
                                    if (bSyariah)
									//20221017, yazri, VSYARIAH-278, end
                                    {
										//20221017, yazri, VSYARIAH-310, begin
                                        //if (cbRekeningRelasi.Items.Count > 2)
                                        //{
										//20221017, yazri, VSYARIAH-310, end
                                        cbRekeningRelasi.Enabled = true;
                                        cbRekeningRelasi.SelectedIndex = 0; 
                                    }
                                    else
                                    {
										//20221017, yazri, VSYARIAH-310, begin
                                        //cbRekeningRelasi.Enabled = false;
                                        //cbRekeningRelasi.SelectedIndex = 1;
										cbRekeningRelasi.Enabled = true;
                                        cbRekeningRelasi.SelectedIndex = 0;
										//20221017, yazri, VSYARIAH-310, end 
                                    }
                                    if (strFlag.Equals("Update"))
                                    {
                                        cbRekeningRelasi.Text = updateNorekSecurity;
                                        cbRekeningRelasi.Enabled = false;
                                    }
                                }
                                else
                                {
                                    if (cbRekeningRelasi.Items.Count > 1)
                                    {
                                        if (cbRekeningRelasi.Items.Count > 2)
                                        {
											//20221017, yazri, VSYARIAH-310, begin
											//cbRekeningRelasi.Enabled = false;
                                            cbRekeningRelasi.Enabled = true;
											//20221017, yazri, VSYARIAH-310, end
                                            cbRekeningRelasi.SelectedIndex = 0;
                                        }
                                        else
                                        {
											//20221017, yazri, VSYARIAH-310, begin
                                            //cbRekeningRelasi.Enabled = false;
                                            //cbRekeningRelasi.SelectedIndex = 1;
											cbRekeningRelasi.Enabled = true;
                                            cbRekeningRelasi.SelectedIndex = 0;
											//20221017, yazri, VSYARIAH-310, end
                                        }
                                    }

                                    if (strFlag.Equals("Update"))
                                    {
                                        if (updateNorekSecurity.Length > 0)
                                        {
                                            cbRekeningRelasi.Text = updateNorekSecurity;
                                            cbRekeningRelasi.Enabled = false;
                                        }
                                    }
                                }

                                //20160818, fauzil, LOGEN196, end
                                //20120802, hermanto_salim, BAALN12003, end    
                            }
                        }
                    }
                }
                //20160302, fauzil, TRBST16240, begin
                //if (nispSettlementDate.Value > 0 & cmpsrNoRekSecurity.Text1 != "" & Currency.Length > 0)
                //    GetDataDealIdForSwitching();
                GetDataSumberDana();
                //initiate datagridview
                //20190321, samy, BOSOD18243, begin
                //if (!dgvSumberDana.Columns.Contains("SumberDana"))
                //    dgvSumberDana.Columns.Add("SumberDana", "Sumber Dana");
                //if (!dgvSumberDana.Columns.Contains("Tanggal"))
                //    dgvSumberDana.Columns.Add("Tanggal", "Tanggal");
                //20190321, samy, BOSOD18243, end
                //20160302, fauzil, TRBST16240, end

                //20200121, dion, TR12020-1, BONDRETAIL-82, begin
                //20210804, irene, BONDRETAIL-796, begin
                //string errMsg = "";
                //this.NISPToolbarButton("6").Enabled = true;

                //ValidateKitas(ds, dsJthTempo, cmbJenisTransaksi.Text, out errMsg);

                //if (!string.IsNullOrEmpty(errMsg))
                //{
                //    if (MessageBox.Show(errMsg, "Confirmation",
                //                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                //        this.NISPToolbarButton("6").Enabled = false;
                //}
                //20210804, irene, BONDRETAIL-796, end
                //20200121, dion, TR12020-1, BONDRETAIL-82, end
            }
            catch (DataException dEx)
            {
                MessageBox.Show(dEx.Message);
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Calculate(out bool bCalculate)
        {
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            DataSet ds = new DataSet();

            int iType = Convert.ToInt32(cmbJenisTransaksi.SelectedValue);


            if (//MoneyTransactionFee.Value != 0 && 
                moneyFaceValue.Value != 0
                && nispSettlementDate.Text.Length != 0 && moneyDealPrice.Text.Length != 0 && cmpsrNoRekSecurity.Text1 != "")
            {


                if (moneyFaceValue.Value.ToString().Length <= 15 && moneyDealPrice.Value.ToString().Length <= 10)
                {


                    int iSettlementDate = nispSettlementDate.Value;

                    decimal dDealPrice = decimal.Parse(moneyDealPrice.Text.ToString());
                    decimal dTaxTarif;
                    if (txtTaxTarif.Visible == true)
                    {
                        dTaxTarif = decimal.Parse(txtTaxTarif.Text.ToString());
                    }
                    else
                    {
                        dTaxTarif = 0;
                    }
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {

                        //20120802, hermanto_salim, BAALN12003, begin
                        //ds = SuratBerharga.calculateORI(iType, CIFid, iSecId, iSettlementDate, moneyFaceValue.Value, dDealPrice, dTaxTarif, Convert.ToDecimal(cmbTrxFee.Text.Trim()));
                        string transactionLinkXML = null;
                        //20160831, fauzil,LOGEN196, begin
                        string transactionLinkXMlTA = null;
                        string transactionLinkXMLNonTA = null;
                        //20160831, fauzil,LOGEN196, end
                        if (dgvTransactionLink.DataSource != null)
                        {
                            DataTable dtToXML = (dgvTransactionLink.DataSource as DataTable).Clone();
                            dtToXML.TableName = "SecurityTransactionLink_TR";

                            for (int i = 0; i < dgvTransactionLink.RowCount; i++)
                            {
                                //20130822, uzia, BAFEM12012, begin
                                //if ((bool)dgvTransactionLink.Rows[i].Cells["SelectBit"].Value)
                                //{
                                //20130822, uzia, BAFEM12012, end
                                DataRow selectedRow = (dgvTransactionLink.Rows[i].DataBoundItem as DataRowView).Row;
                                selectedRow.AcceptChanges();
                                dtToXML.ImportRow(selectedRow);
                                //20130822, uzia, BAFEM12012, begin    
                                //}
                                //20130822, uzia, BAFEM12012, end
                            }
                            StringBuilder stringBuilder = new StringBuilder();
                            dtToXML.WriteXml(System.Xml.XmlWriter.Create(stringBuilder));
                            transactionLinkXML = stringBuilder.ToString();
                            //20160831, fauzil,LOGEN196, begin
                            if (FaceValueTA > 0 & FaceValueNonTA > 0)
                            {
                                dtToXML.Clear();

                                for (int i = 0; i < dgvTransactionLink.RowCount; i++)
                                {
                                    DataRow selectedRow = (dgvTransactionLink.Rows[i].DataBoundItem as DataRowView).Row;
                                    selectedRow.AcceptChanges();
                                    if (selectedRow["isTaxAmnesty"].ToString().Equals("Tax Amnesty", StringComparison.OrdinalIgnoreCase))
                                        dtToXML.ImportRow(selectedRow);
                                }
                                StringBuilder stringBuilderTA = new StringBuilder();
                                dtToXML.WriteXml(System.Xml.XmlWriter.Create(stringBuilderTA));
                                if (stringBuilderTA.ToString().Length > 0)
                                    transactionLinkXMlTA = stringBuilderTA.ToString();

                                dtToXML.Clear();

                                for (int i = 0; i < dgvTransactionLink.RowCount; i++)
                                {
                                    DataRow selectedRow = (dgvTransactionLink.Rows[i].DataBoundItem as DataRowView).Row;
                                    selectedRow.AcceptChanges();
                                    if (selectedRow["isTaxAmnesty"].ToString().Equals("Non Tax Amnesty", StringComparison.OrdinalIgnoreCase))
                                        dtToXML.ImportRow(selectedRow);
                                }
                                StringBuilder stringBuilderNonTA = new StringBuilder();
                                dtToXML.WriteXml(System.Xml.XmlWriter.Create(stringBuilderNonTA));
                                if (stringBuilderNonTA.ToString().Length > 0)
                                    transactionLinkXMLNonTA = stringBuilderNonTA.ToString();

                            }
                            //20160831, fauzil,LOGEN196, end
                        }
                        //20121023, hermanto_salim, BAALN12003, begin
                        decimal? trxFee = chkTransactionFee.Checked ? 0 : (decimal?)null;
                        //20121023, hermanto_salim, BAALN12003, end
                        //20130717, uzia, BAFEM12012, begin
                        //ds = SuratBerharga.calculateORI(iType, CIFid, iSecId, iSettlementDate, moneyFaceValue.Value, dDealPrice, dTaxTarif, trxFee, transactionLinkXML);
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        //ds = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, moneyFaceValue.Value, dDealPrice, dTaxTarif, trxFee, transactionLinkXML);
                        //20240715,pratama,BONDRETAIL-1392, begin
                        long pushBackDealId = 0;
                        Int64.TryParse(cmpsrGetPushBack.Text1.Trim(), out pushBackDealId);
                        //ds = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, moneyFaceValue.Value, dDealPrice, dTaxTarif, trxFee, transactionLinkXML, "BOND");
                        ds = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, moneyFaceValue.Value, dDealPrice, dTaxTarif, trxFee, transactionLinkXML, "BOND", pushBackDealId);
                        //20240715,pratama,BONDRETAIL-1392, end
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        //20130717, uzia, BAFEM12012, end
                        //20120802, hermanto_salim, BAALN12003, end
                        if (ds != null)
                        {
                            if (ds.Tables.Count > 0)
                            {
                                if (ds.Tables[0].Rows.Count > 0)
                                {
                                    txtAccruedDays.Value = ds.Tables[0].Rows[0]["AccruedDays"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["AccruedDays"].ToString());
                                    if (txtAccruedDays.Value == 0)
                                    {
                                        txtAccruedDays.Text = "0.0";
                                    }
                                    txtAccruedInterest.Value = ds.Tables[0].Rows[0]["Interest"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["Interest"].ToString());
                                    if (txtAccruedInterest.Value == 0)
                                    {
                                        txtAccruedInterest.Text = "0.0";
                                    }
                                    txtProceed.Value = ds.Tables[0].Rows[0]["Proceed"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["Proceed"].ToString());
                                    txtPajakBungaBerjalan.Value = ds.Tables[0].Rows[0]["TaxOnAccrued"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["TaxOnAccrued"].ToString());
                                    txtTaxOnCapitalGainLoss.Value = ds.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString());
                                    //20171009, agireza, TRBST16240, begin
                                    txtYTM.Text = ds.Tables[0].Rows[0]["YTM"].ToString() == "" ? "0" : ds.Tables[0].Rows[0]["YTM"].ToString();
                                    //20171009, agireza, TRBST16240, end

                                    //20220208, darul.wahid, BONDRETAIL-895, begin
                                    this.dCapitalGain = ds.Tables[0].Rows[0]["CapitalGain"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["CapitalGain"].ToString());
                                    this.dCapitalGain = cmbJenisTransaksi.Text == "Buy" ? this.dCapitalGain : 0;
                                    //20220208, darul.wahid, BONDRETAIL-895, end
                                    //20220708, darul.wahid, BONDRETAIL-977, begin
                                    this.dCapitalGainNonIDR = ds.Tables[0].Rows[0]["CapitalGainNonIDR"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["CapitalGainNonIDR"].ToString());
                                    this.dCapitalGainNonIDR = cmbJenisTransaksi.Text == "Buy" ? this.dCapitalGainNonIDR : 0;
                                    this.dTotalTax = ds.Tables[0].Rows[0]["TotalTax"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["TotalTax"].ToString());
                                    this.dIncome = ds.Tables[0].Rows[0]["Income"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["Income"].ToString());
                                    //20220708, darul.wahid, BONDRETAIL-977, end
                                    //20220920, yudha.n, BONDRETAIL-1052, begin
                                    this.dMateraiCost = ds.Tables[0].Rows[0]["NominalMaterai"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["NominalMaterai"].ToString());
                                    txtMateraiCost.Value = ds.Tables[0].Rows[0]["NominalMaterai"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["NominalMaterai"].ToString());
                                    //20220920, yudha.n, BONDRETAIL-1052, end

                                    decimal safeKeepFee = ds.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString());
                                    cmbSafeKeepingFeeAfterTax.Items.Clear();
                                    cmbSafeKeepingFeeAfterTax.Items.Add(0);
                                    cmbSafeKeepingFeeAfterTax.Items.Add(safeKeepFee);
                                    cmbSafeKeepingFeeAfterTax.Text = safeKeepFee.ToString().Trim();
                                    txtTotalProceed.Value = ds.Tables[0].Rows[0]["TotalProceed"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["TotalProceed"].ToString());
                                    //20130315, uzia, BAALN12003, begin
                                    //iSafeKeepingFeeAfterTax = ds.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString());
                                    iSafeKeepingFeeAfterTax = ds.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString());
                                    //20130315, uzia, BAALN12003, end
                                    iTotalProceed = ds.Tables[0].Rows[0]["TotalProceed"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["TotalProceed"].ToString());
                                    //tangkap fee
                                    //20120802, hermanto_salim, BAALN12003, begin
                                    //iTransactionFee = Convert.ToDecimal(cmbTrxFee.Text.Trim());
                                    decimal calculatedTransactionFee = iTransactionFee = ds.Tables[0].Rows[0]["TransactionFee"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["TransactionFee"].ToString());
                                    if (chkTransactionFee.Checked)
                                        iTransactionFee = calculatedTransactionFee;
                                    else
                                        iTransactionFee = 0;
                                    //20240422, alfian.andhika, BONDRETAIL-1581, begin
                                    dYieldHargaModal = ds.Tables[0].Rows[0]["YieldHargaModal"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["YieldHargaModal"].ToString());
                                    //20240422, alfian.andhika, BONDRETAIL-1581, end
                                    //20231025,yazri, BONDRETAIL-1392, begin
                                    weightedSpread = ds.Tables[0].Rows[0]["WeightedSpread"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["WeightedSpread"].ToString());
                                    weightedPrice = ds.Tables[0].Rows[0]["WeightedPrice"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["WeightedPrice"].ToString());
                                    indikasiTotalSpread = ds.Tables[0].Rows[0]["IndikasiTotalSpread"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["IndikasiTotalSpread"].ToString());
                                    weightedHoldingPeriod = ds.Tables[0].Rows[0]["WeightedHoldingPeriod"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["WeightedHoldingPeriod"].ToString());
                                    untungRugiNasabah = ds.Tables[0].Rows[0]["UntungRugiNasabah"].ToString() == "" ? 0 : decimal.Parse(ds.Tables[0].Rows[0]["UntungRugiNasabah"].ToString());
                                    keterangan = ds.Tables[0].Rows[0]["Keterangan"] == "" ? 0 : int.Parse(ds.Tables[0].Rows[0]["Keterangan"].ToString());
                                    //20231025,yazri, BONDRETAIL-1392, end
                                    cmbTrxFee.Items.Clear();
                                    cmbTrxFee.Items.Add(0);
                                    cmbTrxFee.Items.Add(calculatedTransactionFee);
                                    subChangeTransactionFee();
                                    //20120802, hermanto_salim, BAALN12003, end
                                    bCalculate = true;
                                    //20190514, uzia, DIGIT18207, begin
                                    SetTrxLinkCapitalGain(ds.Tables[1]);
                                    //20190514, uzia, DIGIT18207, end
                                    //20160831, fauzil,LOGEN196, begin
                                    if (FaceValueTA > 0 & FaceValueNonTA > 0)
                                    {
                                        DataSet dsTA = new DataSet();
                                        DataSet dsNonTA = new DataSet();

                                        //Tax Amnesty
                                        //20220920, yudha.n, BONDRETAIL-1052, begin
                                        //dsTA = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, FaceValueTA, dDealPrice, dTaxTarif, trxFee, transactionLinkXMlTA);
                                        //20240715,pratama,BONDRETAIL-1392, begin
                                        //dsTA = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, FaceValueTA, dDealPrice, dTaxTarif, trxFee, transactionLinkXMlTA, "BOND");
                                        dsTA = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, FaceValueTA, dDealPrice, dTaxTarif, trxFee, transactionLinkXMlTA, "BOND", pushBackDealId);
                                        //20240715,pratama,BONDRETAIL-1392, end
                                        //20220920, yudha.n, BONDRETAIL-1052, end
                                        if (dsTA != null)
                                        {
                                            if (dsTA.Tables.Count > 0)
                                            {
                                                if (dsTA.Tables[0].Rows.Count > 0)
                                                {
                                                    transaksiBankBeliTA.AccruedDays = dsTA.Tables[0].Rows[0]["AccruedDays"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["AccruedDays"].ToString());
                                                    if (transaksiBankBeliTA.AccruedDays == 0)
                                                    {
                                                        transaksiBankBeliTA.AccruedDays = Convert.ToDecimal(0);
                                                    }
                                                    transaksiBankBeliTA.Interest = dsTA.Tables[0].Rows[0]["Interest"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["Interest"].ToString());
                                                    if (transaksiBankBeliTA.Interest == 0)
                                                    {
                                                        transaksiBankBeliTA.Interest = Convert.ToDecimal(0);
                                                    }
                                                    transaksiBankBeliTA.Proceed = dsTA.Tables[0].Rows[0]["Proceed"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["Proceed"].ToString());
                                                    transaksiBankBeliTA.TaxOnAccrued = dsTA.Tables[0].Rows[0]["TaxOnAccrued"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["TaxOnAccrued"].ToString());
                                                    transaksiBankBeliTA.TaxOnCapitalGL = dsTA.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString());
                                                    transaksiBankBeliTA.SafeKeepingFeeAmount = dsTA.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString());
                                                    transaksiBankBeliTA.TotalProceed = dsTA.Tables[0].Rows[0]["TotalProceed"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["TotalProceed"].ToString());
                                                    transaksiBankBeliTA.TransactionFee = dsTA.Tables[0].Rows[0]["TransactionFee"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["TransactionFee"].ToString());
                                                    transaksiBankBeliTA.FaceValue = FaceValueTA;
                                                    transaksiBankBeliTA.IsTaxAmnesty = true;
                                                    transaksiBankBeliTA.NoRekInvestor = NoRekInvestorTA;
                                                    //20220208, darul.wahid, BONDRETAIL-895, begin
                                                    transaksiBankBeliTA.CapitalGain = dsTA.Tables[0].Rows[0]["CapitalGain"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["CapitalGain"].ToString());
                                                    transaksiBankBeliTA.CapitalGain = cmbJenisTransaksi.Text == "Buy" ? transaksiBankBeliTA.CapitalGain : 0;
                                                    //20220208, darul.wahid, BONDRETAIL-895, end
                                                    //20220708, darul.wahid, BONDRETAIL-977, begin
                                                    transaksiBankBeliTA.CapitalGainNonIdr = dsTA.Tables[0].Rows[0]["CapitalGainNonIDR"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["CapitalGainNonIDR"].ToString());
                                                    transaksiBankBeliTA.CapitalGainNonIdr = cmbJenisTransaksi.Text == "Buy" ? transaksiBankBeliNonTA.CapitalGainNonIdr : 0;
                                                    transaksiBankBeliTA.TotalTax = dsTA.Tables[0].Rows[0]["TotalTax"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["TotalTax"].ToString());
                                                    transaksiBankBeliTA.Income = dsTA.Tables[0].Rows[0]["Income"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["Income"].ToString());
                                                    //20220708, darul.wahid, BONDRETAIL-977, end
                                                    //20240422, alfian.andhika, BONDRETAIL-1581, begin
                                                    transaksiBankBeliTA.YieldHargaModal = dsTA.Tables[0].Rows[0]["YieldHargaModal"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["YieldHargaModal"].ToString());
                                                    //20240422, alfian.andhika, BONDRETAIL-1581, end
                                                }
                                            }
                                        }

                                        //Non Tax Amnesty
                                        //20220920, yudha.n, BONDRETAIL-1052, begin
                                        //dsNonTA = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, FaceValueNonTA, dDealPrice, dTaxTarif, trxFee, transactionLinkXMLNonTA);
                                        //20240715,pratama,BONDRETAIL-1392, begin
                                        //dsNonTA = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, FaceValueNonTA, dDealPrice, dTaxTarif, trxFee, transactionLinkXMLNonTA, "BOND");
                                        dsNonTA = SuratBerharga.calculateORI2(this.cQuery, iType, CIFid, iSecId, iSettlementDate, FaceValueNonTA, dDealPrice, dTaxTarif, trxFee, transactionLinkXMLNonTA, "BOND", pushBackDealId);
                                        //20240715,pratama,BONDRETAIL-1392, end
                                        //20220920, yudha.n, BONDRETAIL-1052, end
                                        if (dsNonTA != null)
                                        {
                                            if (dsNonTA.Tables.Count > 0)
                                            {
                                                if (dsNonTA.Tables[0].Rows.Count > 0)
                                                {
                                                    transaksiBankBeliNonTA.AccruedDays = dsNonTA.Tables[0].Rows[0]["AccruedDays"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["AccruedDays"].ToString());
                                                    if (transaksiBankBeliNonTA.AccruedDays == 0)
                                                    {
                                                        transaksiBankBeliNonTA.AccruedDays = Convert.ToDecimal(0);
                                                    }
                                                    transaksiBankBeliNonTA.Interest = dsNonTA.Tables[0].Rows[0]["Interest"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["Interest"].ToString());
                                                    if (transaksiBankBeliNonTA.Interest == 0)
                                                    {
                                                        transaksiBankBeliNonTA.Interest = Convert.ToDecimal(0);
                                                    }
                                                    transaksiBankBeliNonTA.Proceed = dsNonTA.Tables[0].Rows[0]["Proceed"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["Proceed"].ToString());
                                                    transaksiBankBeliNonTA.TaxOnAccrued = dsNonTA.Tables[0].Rows[0]["TaxOnAccrued"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["TaxOnAccrued"].ToString());
                                                    transaksiBankBeliNonTA.TaxOnCapitalGL = dsNonTA.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["TaxOnCapitalGL"].ToString());
                                                    transaksiBankBeliNonTA.SafeKeepingFeeAmount = dsNonTA.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["SafeKeepingFeeAmount"].ToString());
                                                    transaksiBankBeliNonTA.TotalProceed = dsNonTA.Tables[0].Rows[0]["TotalProceed"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["TotalProceed"].ToString());
                                                    transaksiBankBeliNonTA.TransactionFee = dsNonTA.Tables[0].Rows[0]["TransactionFee"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["TransactionFee"].ToString());
                                                    transaksiBankBeliNonTA.FaceValue = FaceValueNonTA;
                                                    transaksiBankBeliNonTA.IsTaxAmnesty = false;
                                                    transaksiBankBeliNonTA.NoRekInvestor = NoRekInvestorNonTA;
                                                    //20220208, darul.wahid, BONDRETAIL-895, begin
                                                    transaksiBankBeliNonTA.CapitalGain = dsNonTA.Tables[0].Rows[0]["CapitalGain"].ToString() == "" ? 0 : decimal.Parse(dsTA.Tables[0].Rows[0]["CapitalGain"].ToString());
                                                    transaksiBankBeliNonTA.CapitalGain = cmbJenisTransaksi.Text == "Buy" ? transaksiBankBeliNonTA.CapitalGain : 0;
                                                    //20220208, darul.wahid, BONDRETAIL-895, end
                                                    //20220708, darul.wahid, BONDRETAIL-977, begin
                                                    transaksiBankBeliNonTA.CapitalGainNonIdr = dsNonTA.Tables[0].Rows[0]["CapitalGainNonIDR"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["CapitalGainNonIDR"].ToString());
                                                    transaksiBankBeliNonTA.CapitalGainNonIdr = cmbJenisTransaksi.Text == "Buy" ? transaksiBankBeliNonTA.CapitalGainNonIdr : 0;
                                                    transaksiBankBeliNonTA.TotalTax = dsNonTA.Tables[0].Rows[0]["TotalTax"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["TotalTax"].ToString());
                                                    transaksiBankBeliNonTA.Income = dsNonTA.Tables[0].Rows[0]["Income"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["Income"].ToString());
                                                    //20220708, darul.wahid, BONDRETAIL-977, end
                                                    //20240422, alfian.andhika, BONDRETAIL-1581, begin
                                                    transaksiBankBeliNonTA.YieldHargaModal = dsNonTA.Tables[0].Rows[0]["YieldHargaModal"].ToString() == "" ? 0 : decimal.Parse(dsNonTA.Tables[0].Rows[0]["YieldHargaModal"].ToString());
                                                    //20240422, alfian.andhika, BONDRETAIL-1581, end
                                                }
                                            }
                                        }

                                    }
                                    //20160831, fauzil,LOGEN196, end
                                    //20221213, darul.wahid, BONDRETAIL-1117, begin
                                    //Validasi YTM, kalau minus tidak bisa lanjut
                                    decimal dcYTM = 0;
                                    decimal.TryParse(ds.Tables[0].Rows[0]["YTM"].ToString(), out dcYTM);
                                    
                                    if (dcYTM < 0)
                                    {
                                        MessageBox.Show("YTM negatif, tidak dapat melanjutkan transaksi", "Warnings!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        //throw new Exception();
                                        moneyDealPrice.Value = 0;
                                        bCalculate = false;
                                        return;
                                    }
                                    //20240222,pratama,BONDRETAIL-1392,begin
                                    if (keterangan == 10)
                                    {
                                        MessageBox.Show("Transaksi belum dapat diproses karena parameter total spread untuk produk ini belum diatur. Mohon hubungi WM untuk proses lebih lanjut.", "Warnings!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        bCalculate = false;
                                        return;
                                    }
                                    var ket = keterangan == 1 ? "Transaksi dapat langsung diproses tanpa approval WM" : "Transaksi ini memerlukan approval WM karena melebihi max spread yang diperbolehkan. Jika ingin tetap ingin melanjutkan transaksi, mohon kirim email permohonan ke wmtp@ocbc.id";
                                    if (iType == 4 && keterangan != 1) // Bank Jual Nasabah Beli
                                    {
                                        MessageBox.Show("Keterangan: Transaksi ini memerlukan approval WM karena melebihi max spread yang diperbolehkan. Jika ingin tetap ingin melanjutkan transaksi, mohon kirim email permohonan ke wmtp@ocbc.id", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else if (iType == 3)
                                    {// Bank Beli Nasabah Jual
                                        //tanpa approval WM / keterangan 1 && tidak rugi
                                        if (keterangan == 1 && untungRugiNasabah >= 0)//(ndHargaModal.Value - moneyDealPrice.Value) == 0 && untungRugiNasabah > 0)
                                        {
                                            MessageBox.Show("Weighted Spread Nasabah Beli: " + weightedSpread + "\nWeighted Price Nasabah Beli: " + weightedPrice + "\nIndikasi Total Spread untuk bank: " + indikasiTotalSpread + "\nIndikasi Untung atau Rugi Nasabah: " + untungRugiNasabah + "\nWeighted Holding Period: " + weightedHoldingPeriod + " Years \n", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                        //nasabah jual rugi dan spread = 0
                                        else if (keterangan == 1 && untungRugiNasabah < 0)//(ndHargaModal.Value - moneyDealPrice.Value) == 0 && untungRugiNasabah < 0) //jika nasabah rugi atau
                                        {
                                            MessageBox.Show("Weighted Spread Nasabah Beli: " + weightedSpread + "\nWeighted Price Nasabah Beli: " + weightedPrice + "\nIndikasi Total Spread untuk bank: " + indikasiTotalSpread + "\nIndikasi Untung atau Rugi Nasabah: " + untungRugiNasabah + "\nWeighted Holding Period: " + weightedHoldingPeriod + " Years \n\nKeterangan: " + ket + "", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            if (MessageBox.Show("Nasabah akan menjual dengan posisi rugi, apakah transaksi akan di lanjutkan?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                            {
                                                controlsClear(true);
                                                return;
                                            }
                                        }
                                        if (keterangan == 0)
                                        {
                                            MessageBox.Show("Weighted Spread Nasabah Beli: " + weightedSpread + "\nWeighted Price Nasabah Beli: " + weightedPrice + "\nIndikasi Total Spread untuk bank: " + indikasiTotalSpread + "\nIndikasi Untung atau Rugi Nasabah: " + untungRugiNasabah + "\nWeighted Holding Period: " + weightedHoldingPeriod + " Years", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            if (MessageBox.Show("Transaksi ini memerlukan approval WM karena melebihi max spread yang diperbolehkan. Jika ingin tetap ingin melanjutkan transaksi, mohon kirim email permohonan ke wmtp@ocbc.id", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                            {
                                                controlsClear(true);
                                                return;
                                            }
                                        }
                                        //20231208, pratama,BONDRETAIL-1392, end
                                    }
                                    //20221213, darul.wahid, BONDRETAIL-1117, end                                    
                                }
                                //20130717, uzia, BAFEM12012, begin                                
                                this._dsAccrued = new DataSet();
                                this._dsAccrued.Tables.Add(ds.Tables[1].Copy());
                                //20130717, uzia, BAFEM12012, end
                            }
                        }
                        else if (ds == null)
                        {

                            txtAccruedInterest.Value = 0;
                            txtProceed.Value = 0;
                            txtPajakBungaBerjalan.Value = 0;
                            txtTaxOnCapitalGainLoss.Value = 0;
                            cmbSafeKeepingFeeAfterTax.Items.Clear();
                            cmbSafeKeepingFeeAfterTax.Items.Add(0);
                            cmbSafeKeepingFeeAfterTax.Text = "0";
                            txtTotalProceed.Value = 0;

                            bCalculate = false;
                            //20220208, darul.wahid, BONDRETAIL-895, begin
                            this.dCapitalGain = 0;
                            //20220208, darul.wahid, BONDRETAIL-895, end
                            //20220708, darul.wahid, BONDRETAIL-977, begin
                            this.dCapitalGainNonIDR = 0;
                            this.dTotalTax = 0;
                            this.dIncome = 0;
                            //20220708, darul.wahid, BONDRETAIL-977, end
                            //20220920, yudha.n, BONDRETAIL-1052, begin
                            this.dMateraiCost = 0;
                            txtMateraiCost.Value = 0;
                            //20220920, yudha.n, BONDRETAIL-1052, end
                            //20240422, alfian.andhika, BONDRETAIL-1581, begin
                            dYieldHargaModal = 0;
                            //20240422, alfian.andhika, BONDRETAIL-1581, end
                        }
                        Cursor.Current = Cursors.Default;
                        nispSettlementDate.Focus();
                        bCalculate = true;
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        this._bCalculate = bCalculate;
                        if (dMateraiCost == 0)
                        {
                            isMeteraiAbsorbed = false;
                            cbRekeningMaterai.Enabled = false;
                            cbRekeningMaterai.SelectedIndex = -1;
                        }
                        else
                        {
                            if (strFlag == "Update")
                            {
                                if (updateNorekMeterai.Length > 0)
                                {
                                    isMeteraiAbsorbed = false;
                                }
                                else
                                {
                                    cbRekeningMaterai.Enabled = !isMeteraiAbsorbed;
                                }
                            }
                            else
                            {
                                cbRekeningMaterai.Enabled = !isMeteraiAbsorbed;
                                cbRekeningMaterai.SelectedIndex = cbRekeningMaterai.SelectedIndex == -1 && !isMeteraiAbsorbed ? 0 : cbRekeningMaterai.SelectedIndex;
                            }
                        }
                        //20220920, yudha.n, BONDRETAIL-1052, end
                    }
                    catch (NullReferenceException)
                    {
                        txtAccruedInterest.Value = 0;
                        txtProceed.Value = 0;
                        txtPajakBungaBerjalan.Value = 0;
                        txtTaxOnCapitalGainLoss.Value = 0;
                        cmbSafeKeepingFeeAfterTax.Items.Clear();
                        cmbSafeKeepingFeeAfterTax.Items.Add(0);
                        cmbSafeKeepingFeeAfterTax.Text = "0";
                        txtTotalProceed.Value = 0;
                        nispSettlementDate.Focus();
                        bCalculate = false;
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        this.dCapitalGain = 0;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        //20220708, darul.wahid, BONDRETAIL-977, begin
                        this.dCapitalGainNonIDR = 0;
                        this.dTotalTax = 0;
                        this.dIncome = 0;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        this.dMateraiCost = 0;
                        txtMateraiCost.Value = 0;
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        dYieldHargaModal = 0;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                    }
                    catch (OverflowException)
                    {
                        // MessageBox.Show("Calculate Gagal!");
                        txtAccruedDays.Value = 0;
                        moneyFaceValue.Value = 0;
                        moneyDealPrice.Value = 0;
                        txtTaxTarif.Value = 0;
                        txtAccruedInterest.Value = 0;
                        txtProceed.Value = 0;
                        txtPajakBungaBerjalan.Value = 0;
                        txtTaxOnCapitalGainLoss.Value = 0;
                        cmbSafeKeepingFeeAfterTax.Items.Clear();
                        cmbSafeKeepingFeeAfterTax.Items.Add(0);
                        cmbSafeKeepingFeeAfterTax.Text = "0";
                        txtTotalProceed.Value = 0;
                        nispSettlementDate.Focus();
                        bCalculate = false;
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        this.dCapitalGain = 0;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        //20220708, darul.wahid, BONDRETAIL-977, begin
                        this.dCapitalGainNonIDR = 0;
                        this.dTotalTax = 0;
                        this.dIncome = 0;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        this.dMateraiCost = 0;
                        txtMateraiCost.Value = 0;
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        dYieldHargaModal = 0;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                    }
                    catch
                    {

                        //  MessageBox.Show("Calculate Gagal!");
                        txtAccruedDays.Value = 0;
                        moneyFaceValue.Value = 0;
                        moneyDealPrice.Value = 0;
                        txtTaxTarif.Value = 0;
                        txtAccruedInterest.Value = 0;
                        txtProceed.Value = 0;
                        txtPajakBungaBerjalan.Value = 0;
                        txtTaxOnCapitalGainLoss.Value = 0;
                        cmbSafeKeepingFeeAfterTax.Items.Clear();
                        cmbSafeKeepingFeeAfterTax.Items.Add(0);
                        cmbSafeKeepingFeeAfterTax.Text = "0";
                        txtTotalProceed.Value = 0;
                        nispSettlementDate.Focus();
                        bCalculate = false;
                        //20220208, darul.wahid, BONDRETAIL-895, begin
                        this.dCapitalGain = 0;
                        //20220208, darul.wahid, BONDRETAIL-895, end
                        //20220708, darul.wahid, BONDRETAIL-977, begin
                        this.dCapitalGainNonIDR = 0;
                        this.dTotalTax = 0;
                        this.dIncome = 0;
                        //20220708, darul.wahid, BONDRETAIL-977, end
                        //20220920, yudha.n, BONDRETAIL-1052, begin
                        this.dMateraiCost = 0;
                        txtMateraiCost.Value = 0;
                        //20220920, yudha.n, BONDRETAIL-1052, end
                        //20240422, alfian.andhika, BONDRETAIL-1581, begin
                        dYieldHargaModal = 0;
                        //20240422, alfian.andhika, BONDRETAIL-1581, end
                    }
                }
                else
                {
                    MessageBox.Show("Face Value Atau Deal Price Salah", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    moneyFaceValue.Focus();
                    bCalculate = false;
                }
            }
            else
            {

                MessageBox.Show("Gagal calculate! \nada data yang kosong", "Warnings!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nispSettlementDate.Focus();
                bCalculate = false;
            }


        }

        private void cmpsrSearch2_AfterShowSearchForm(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            ListDataAwal();
            Cursor.Current = Cursors.Default;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool bCalculate;
            //20210813, rezakahfi, BONDRETAIL-799, begin
            if (strFlag == "Insert")
            {
//20231227, rezakahfi, BONDRETAIL-1513, begin
                if (!isTA)
                {
//20231227, rezakahfi, BONDRETAIL-1513, end
                    if (!CekHargaModal())
                        return;
                }
                else
                {
                    if (!ValidateProduct())
                        return;
//20231227, rezakahfi, BONDRETAIL-1513, begin
                }
//20231227, rezakahfi, BONDRETAIL-1513, end
            }
            //20210813, rezakahfi, BONDRETAIL-799, end
            //20220113, rezakahfi, BONDRETAIL-877, begin
            if (cmbJenisTransaksi.Text == "Buy")
            {
                if (!SettingCherryPick())
                    return;
            }
            //20220113, rezakahfi, BONDRETAIL-877, end
            Calculate(out bCalculate);
            if (bCalculate == false)
            {
                ClearDataForCalculate();
                //20221213, darul.wahid, BONDRETAIL-1117, begin
                if (!string.IsNullOrEmpty(moneyFaceValue.ToString()))
                    moneyDealPrice.Focus();
                else
                //20221213, darul.wahid, BONDRETAIL-1117, end
                    moneyFaceValue.Focus();
            }
            //20171006, agireza, COPOD17271, begin
            else
            {
                isNeedRecalculate = false;
            }
            //20171006, agireza, COPOD17271, end
        }

        //20220113, rezakahfi, BONDRETAIL-877, begin
        private bool SettingCherryPick() 
        {
            decimal dcFaceValue = moneyFaceValue.Value;
            bool bProcess = true;

            for (int i = 0; i < dgvTransactionLink.RowCount; i++)
            {
                decimal dcValueLeft = (decimal)dgvTransactionLink.Rows[i].Cells["ValueLeft"].Value;

                if (dcFaceValue == 0)
                {
                    dgvTransactionLink["SelectBit", i].Value = false;
                    dgvTransactionLink["NominalJual", i].Value = 0;
                }
                else if (dcValueLeft >= dcFaceValue)
                {
                    dgvTransactionLink["SelectBit", i].Value = true;
                    dgvTransactionLink["NominalJual", i].Value = dcFaceValue;
                    dcFaceValue = 0;
                }
                else if (dcValueLeft < dcFaceValue)
                {
                    dgvTransactionLink["SelectBit", i].Value = true;
                    dgvTransactionLink["NominalJual", i].Value = dcValueLeft;
                    dcFaceValue = dcFaceValue - dcValueLeft;
                }
            }

            if (dcFaceValue > 0)
            {
                MessageBox.Show("Face Value lebih besar dari outstanding nasabah", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                bProcess = false;
            }

            return bProcess;
        }
        //20220113, rezakahfi, BONDRETAIL-877, end

        private void nispDealDate_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                nispSettlementDate.Focus();
            }
        }

        private void nispSettlementDate_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                txtAccruedDays.Focus();
            }
        }

        private void txtAccruedDays_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                moneyFaceValue.Focus();
            }
        }

        private void moneyFaceValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                moneyDealPrice.Focus();
            }
        }

        private void moneyDealPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                txtTaxTarif.Focus();
            }
        }

        private void txtTaxTarif_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                btnCalculate.Focus();
            }
        }

        private void cmpsrNoRekSecurity_onNispText1Changed(object sender, EventArgs e)
        {
            if (cmpsrNoRekSecurity.Text1.Length == 0)
            {
                txtNamaNasabah.Text = "";
                //20150815, fauzil, LOGEN196, begin
                cbRekeningRelasi.DataSource = null;
                chkTaxAmnesty.Checked = false;
                txtRekeningRelasiName.Text = "";
                //20150815, fauzil, LOGEN196, end
            }
            //20150815, samy, LOGEN191, begin            
            SaveTaxAmnestyValidation();
            //20150815, samy, LOGEN191, end

        }

        private void cmpsrNoRekSecurity_Leave(object sender, EventArgs e)
        {
            if (cmpsrNoRekSecurity.Text1 != "")
            {
                cmpsrNoRekSecurity.ValidateField();
                //20150815, samy, LOGEN191, begin                
                SaveTaxAmnestyValidation();
                //20150815, samy, LOGEN191, end
                //20160915, pengki, LOGEN00204, Begin
                cbRekeningRelasi.DataSource = null;
                chkTaxAmnesty.Checked = false;
                txtRekeningRelasiName.Text = "";
                //20160915, pengki, LOGEN00204, end
            }

        }

        private void moneyFaceValue_Validating(object sender, CancelEventArgs e)
        {
            if (moneyFaceValue.Value.ToString().Length > 15)
            {
                MessageBox.Show("Face Value \nMelebihi Batas Maksimal 15 Digit", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
            if (moneyFaceValue.Value < 0)
            {
                MessageBox.Show("Face Value \nTidak Boleh Minus", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void cmpsrSearch1_AfterShowSearchForm(object sender, EventArgs e)
        {
            if (cmpsrNomorSekuriti.Text1 != "" && cmbJenisTransaksi.SelectedIndex != -1)
            {
                StateTransaction();
            }
        }

        private void cmpsrNomorSekuriti_AfterShowSearchForm(object sender, EventArgs e)
        {
            //20160721, fauzil, TRBST16240, begin
            bool CanProcess = false;
            string ErrorMessage = "";
            //20230215, samypasha, BONDRETAIL-1241, begin
            //clsDatabase.subTRSValidateCutOffTimeTransaction(cQuery, "1", cmpsrNomorSekuriti.Text1, out CanProcess, out ErrorMessage);
            //20231227, rezakahfi, BONDRETAIL-1513, begin
            if (!isTA)
            {
                clsDatabase.subTRSValidateCutOffTimeTransaction(cQuery, "1", cmpsrNomorSekuriti._Text1.Text.ToString(), out CanProcess, out ErrorMessage);
            }else
                CanProcess = true;
            //20231227, rezakahfi, BONDRETAIL-1513, end
            //20230215, samypasha, BONDRETAIL-1241, end
            if (!CanProcess)
            {
                MessageBox.Show(ErrorMessage, "Warning!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnRefresh();
                return;
            }

            //20160721, fauzil, TRBST16240, end
            if (cmpsrSearch1.Text1 != "" && cmbJenisTransaksi.SelectedIndex != -1)
            {
                StateTransaction();
                //20160118, fauzil, TRBST16240, begin
                controlsClear(false);
                //20160118, fauzil, TRBST16240, end
            }
        }

        private void moneyDealPrice_Validating(object sender, CancelEventArgs e)
        {
            if (moneyDealPrice.Value.ToString().Length > 15)
            {
                MessageBox.Show("Deal Price \nMelebihi Batas maksimal 15 Digit", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
            if (moneyDealPrice.Value < 0)
            {
                MessageBox.Show("Deal Price \nTidak Boleh Minus", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
            ClearDataForCalculate();
        }

        private void txtTaxTarif_Validating(object sender, CancelEventArgs e)
        {
            if (txtTaxTarif.Value.ToString().Length > 15)
            {
                MessageBox.Show("Tax Tarif \nMelebihi Batas maksimal 15 Digit", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
            if (txtTaxTarif.Value < 0)
            {
                MessageBox.Show("Tax Tarif \nTidak Boleh Minus", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        //private void MoneyTransactionFee_Validating(object sender, CancelEventArgs e)
        //{

        //        if (MoneyTransactionFee.Value == 0)
        //        {
        //            MoneyTransactionFee.Text = "0.00";
        //        }
        //    if (MoneyTransactionFee.Text.Length != 0)
        //    {
        //        if (MoneyTransactionFee.Value != iTransactionFee)
        //        {
        //            MessageBox.Show("Transaction Fee Tidak Sesuai\nMohon Calculate Ulang", "Peringatan");
        //            return;
        //        }

        //        if (MoneyTransactionFee.Value.ToString().Length > 15)
        //        {
        //            MessageBox.Show("Transaction Fee \nMelebihi Batas maksimal 15 Digit", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            e.Cancel = true;
        //        }
        //        if (MoneyTransactionFee.Value < 0)
        //        {
        //            MessageBox.Show("Transaction Fee \nTidak Boleh Minus", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            e.Cancel = true;
        //        }
        //    }

        //}

        private void txtCouponRate_Validating(object sender, CancelEventArgs e)
        {
            if (txtCouponRate.Value.ToString().Length > 15)
            {
                MessageBox.Show("CouponRate \nMelebihi Batas maksimal 15 Digit", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
            if (txtCouponRate.Value < 0)
            {
                MessageBox.Show("CouponRate \nTidak Boleh Minus", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void cmpsrNomorSekuriti_Validating(object sender, CancelEventArgs e)
        {
            //20160225, fauzil, TRBST16240, begin
            if (cmpsrNomorSekuriti.Text1.Length != 0)
            {
                decimal ModalJual = 0;
                decimal ModalBeli = 0;
                bool nOK = false;
                string ErrorMessage = "";
                DataSet ds = new DataSet();
                TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
                cmbJenisTransaksi.DataSource = null;

                ds = SuratBerharga.ListTypeTransaction();
                cmbJenisTransaksi.DataSource = ds.Tables[0];
                cmbJenisTransaksi.DisplayMember = "TrxDesc";
                cmbJenisTransaksi.ValueMember = "TrxTypeID";
                cmbJenisTransaksi.SelectedIndex = -1;
                cmpsrNomorSekuriti.Criteria = "1";

                //20230215, samypasha, BONDRETAIL-1241, begin
                //if (TransaksiSuratBerharga.ValidateDataTransaskiHargaModal(cmpsrNomorSekuriti.Text1, out ModalJual, out ModalBeli, out nOK, out ErrorMessage))
                if (TransaksiSuratBerharga.ValidateDataTransaskiHargaModal(cmpsrNomorSekuriti._Text1.Text.ToString(), out ModalJual, out ModalBeli, out nOK, out ErrorMessage))
                //20230215, samypasha, BONDRETAIL-1241, end
                {
                    if (!nOK)
                    {
                        MessageBox.Show(ErrorMessage, "Error", MessageBoxButtons.OK);
                        cmbJenisTransaksi.DataSource = null;
                        controlsClear(true);
                        cmpsrNomorSekuriti.Focus();
                        return;
                    }
                    else
                    {
                        if ((ModalJual != 0) & (ModalBeli == 0))
                            trxType = "SELL";
                        else if ((ModalJual == 0) & (ModalBeli != 0))
                            trxType = "BUY";

                        //20171218, uzia, TRBST16240, begin
                        Cursor.Current = Cursors.WaitCursor;
                        //ListDataAwal();
                        Cursor.Current = Cursors.Default;
                        //20171218, uzia, TRBST16240, end
                    }
                }
                else
                {
                    MessageBox.Show("Gagal dalam pengecekan harga modal jual/beli ", "Error", MessageBoxButtons.OK);
                    cmbJenisTransaksi.DataSource = null;
                    controlsClear(true);
                    cmpsrNomorSekuriti.Focus();
                    return;
                }
                //20231227, rezakahfi, BONDRETAIL-1513, begin
                if (cmpsrNomorSekuriti._Text1.Text.ToString() != "")
                {
                    DataRow drSecMaster = InqData.InquiryDataSecurity(cmpsrNomorSekuriti._Text1.Text.ToString());
                    if (drSecMaster.ItemArray.Length > 0)
                    {
                        nispMaturityDate.Value = int.Parse(DateTime.Parse(drSecMaster["MaturityDate"].ToString()).ToString("yyyyMMdd"));
                        txtBondType.Text = drSecMaster["SecTypeDesc"].ToString().Trim();
                        if (drSecMaster["ImbalHasil"].ToString().Contains("1"))
                        {
                            if (!tcImbalHasil.TabPages.Contains(pgKupon))
                                tcImbalHasil.TabPages.Add(pgKupon);
                            if (tcImbalHasil.TabPages.Contains(pgDiskonto))
                                tcImbalHasil.TabPages.Remove(pgDiskonto);
                        }
                        else
                        {
                            if (!tcImbalHasil.TabPages.Contains(pgDiskonto))
                                tcImbalHasil.TabPages.Add(pgDiskonto);
                            if (tcImbalHasil.TabPages.Contains(pgKupon))
                                tcImbalHasil.TabPages.Remove(pgKupon);
                        }
                    }
                }
                //20231227, rezakahfi, BONDRETAIL-1513, end
            }

            //20160225, fauzil, TRBST16240, end
            if (cmbJenisTransaksi.SelectedIndex != -1 && cmpsrNomorSekuriti.Text1.Length != 0 && cmpsrSearch1.Text1.Length != 0)
            {
                controlsEnabled(true);
                //david
                if (nispDealDate.Text == "")
                {
                    TransaksiSuratBerharga SB = new TransaksiSuratBerharga();
                    System.Data.DataSet dsHari = new System.Data.DataSet();
                    System.Data.DataSet dsLusa = new System.Data.DataSet();
                    dsHari = SB.GetWorkingDate(0, 0);
                    //20160225, fauzil, TRBST16240, begin
                    //dsLusa = SB.GetWorkingDate(1, 2);
                    dsLusa = SB.GetWorkingDate(2, 2);
                    //20160225, fauzil, TRBST16240, end
                    //20160225, fauzil, TRBST16240, begin
                    DataSet DefaultSettlementDate = new DataSet();
                    //20220331, darul.wahid, ONEMBL-1279, begin
                    //DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti.Text1);
                    int JenisTrx = int.Parse(cmbJenisTransaksi.SelectedValue.ToString());
                    //20230215, samypasha, BONDRETAIL-1241, begin
                    //DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti.Text1, JenisTrx, "CB");
                    DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti._Text1.Text.ToString(), JenisTrx, "CB");
                    //20230215, samypasha, BONDRETAIL-1241, end
                    //20220331, darul.wahid, ONEMBL-1279, end                    

                    if (DefaultSettlementDate.Tables[0].Rows[0][0].GetType() != typeof(DBNull))
                        nispSettlementDate.Text = DefaultSettlementDate.Tables[0].Rows[0][0].ToString();
                    else
                        nispSettlementDate.Text = dsLusa.Tables[0].Rows[0][0].ToString();
                    if (DefaultSettlementDate.Tables[0].Rows[0][1].GetType() != typeof(DBNull))
                        nispDealDate.Text = DefaultSettlementDate.Tables[0].Rows[0][1].ToString();
                    else
                        nispDealDate.Text = dsHari.Tables[0].Rows[0][0].ToString();
                    //20231227, rezakahfi, BONDRETAIL-1513, begin

                    DateTime SettDate = DateTime.ParseExact(nispSettlementDate.Text, "dd-MM-yyyy", null);
                    DateTime MattDate = DateTime.ParseExact(nispMaturityDate.Text, "dd-MM-yyyy", null);
                    txtTenor.Value = decimal.Parse((MattDate - SettDate).TotalDays.ToString());

                    //20231227, rezakahfi, BONDRETAIL-1513, end
                    //20171121, agireza, TRBST16240, begin
                    txtAccruedDays.Text = DefaultSettlementDate.Tables[0].Rows[0]["AccruedDays"].ToString();
                    //20171121, agireza, TRBST16240, end
                    //string Hari = dsHari.Tables[0].Rows[0][0].ToString();
                    //string lusa = dsLusa.Tables[0].Rows[0][0].ToString();

                    //nispDealDate.Text = Hari;
                    //nispSettlementDate.Text = lusa;
                    //20160225, fauzil, TRBST16240, end

                    //20160302, fauzil, TRBST16240, begin
                    //if (nispSettlementDate.Value > 0 & cmpsrNoRekSecurity.Text1 != "" & Currency.Length > 0)
                    //    GetDataDealIdForSwitching();
                    //20160302, fauzil, TRBST16240, end
                }

                strFlag = "Insert";

            }
        }

        private void cmpsrSearch1_Validating(object sender, CancelEventArgs e)
        {
            if (cmbJenisTransaksi.SelectedIndex != -1 && cmpsrNomorSekuriti.Text1.Length != 0 && cmpsrSearch1.Text1.Length != 0)
            {
                controlsEnabled(true);

                TransaksiSuratBerharga SB = new TransaksiSuratBerharga();
                System.Data.DataSet dsHari = new System.Data.DataSet();
                System.Data.DataSet dsLusa = new System.Data.DataSet();
                dsHari = SB.GetWorkingDate(0, 0);
                //20160225, fauzil, TRBST16240, begin
                //dsLusa = SB.GetWorkingDate(1, 2);
                dsLusa = SB.GetWorkingDate(2, 2);
                //20160225, fauzil, TRBST16240, end


                //20160225, fauzil, TRBST16240, begin
                DataSet DefaultSettlementDate = new DataSet();
                //20220331, darul.wahid, ONEMBL-1279, begin
                //DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti.Text1);
                int JenisTrx = int.Parse(cmbJenisTransaksi.SelectedValue.ToString());
                //20230215, samypasha, BONDRETAIL-1241, begin
                //DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti.Text1, JenisTrx, "CB");
                DefaultSettlementDate = SB.GetDefaultSettlementDate(cmpsrNomorSekuriti._Text1.Text.ToString(), JenisTrx, "CB");
                //20230215, samypasha, BONDRETAIL-1241, end
                //20220331, darul.wahid, ONEMBL-1279, end   
                

                if (DefaultSettlementDate.Tables[0].Rows[0][0].GetType() != typeof(DBNull))
                    nispSettlementDate.Text = DefaultSettlementDate.Tables[0].Rows[0][0].ToString();
                else
                    nispSettlementDate.Text = dsLusa.Tables[0].Rows[0][0].ToString();

                if (DefaultSettlementDate.Tables[0].Rows[0][1].GetType() != typeof(DBNull))
                    nispDealDate.Text = DefaultSettlementDate.Tables[0].Rows[0][1].ToString();
                else
                    nispDealDate.Text = dsHari.Tables[0].Rows[0][0].ToString();
                //20231227, rezakahfi, BONDRETAIL-1513, begin

                DateTime SettDate = DateTime.ParseExact(nispSettlementDate.Text, "dd-MM-yyyy", null);
                DateTime MattDate = DateTime.ParseExact(nispMaturityDate.Text, "dd-MM-yyyy", null);
                txtTenor.Value = decimal.Parse((MattDate - SettDate).TotalDays.ToString());

                //20231227, rezakahfi, BONDRETAIL-1513, end
                //20171124, agireza, TRBST16240, begin
                txtAccruedDays.Text = DefaultSettlementDate.Tables[0].Rows[0]["AccruedDays"].ToString();
                //20171124, agireza, TRBST16240, end

                //string Hari = dsHari.Tables[0].Rows[0][0].ToString();
                //string lusa = dsLusa.Tables[0].Rows[0][0].ToString();

                //nispDealDate.Text = Hari;
                //nispSettlementDate.Text = lusa;
                //20160225, fauzil, TRBST16240, end

                strFlag = "Insert";

            }
        }

        //private void txtSafeKeepingFeeAfterTax_Validating(object sender, CancelEventArgs e)
        //{
        //    if (txtSafeKeepingFeeAfterTax.Value == 0)
        //    {
        //        txtSafeKeepingFeeAfterTax.Text = "0.0";
        //    }
        //    //if (txtSafeKeepingFeeAfterTax.Value != null)
        //    //{
        //        if (iSafeKeepingFeeAfterTax != 0)
        //        {

        //            if (txtSafeKeepingFeeAfterTax.Value != 0 && txtSafeKeepingFeeAfterTax.Value != iSafeKeepingFeeAfterTax)  
        //            {
        //                MessageBox.Show("Safe Keeping Fee After Tax Tidak Sesuai\nMohon Calculate Ulang","Peringatan");
        //                //txtSafeKeepingFeeAfterTax.Value = iSafeKeepingFeeAfterTax;
        //                txtSafeKeepingFeeAfterTax.Text = "0.0";
        //                txtTotalProceed.Value = txtTotalProceed.Value + iSafeKeepingFeeAfterTax;
        //                return;
        //            }
        //            else if (txtSafeKeepingFeeAfterTax.Value == 0)
        //            {
        //                txtTotalProceed.Value = txtTotalProceed.Value + iSafeKeepingFeeAfterTax;
        //            }
        //            else 
        //            {
        //                //if (txtTotalProceed.Value != iTotalProceed) //textbox totalproceed pernah berubah krn safekeepingfee jd 0
        //                //{
        //                    txtTotalProceed.Value = iTotalProceed;
        //                //}

        //            }
        //        }
        //    //}
        //}

        private void cmpsrNoRekSecurity_Validating(object sender, CancelEventArgs e)
        {
            if (cmpsrNoRekSecurity.Text1.Length != 0)
            {                
                Cursor.Current = Cursors.WaitCursor;
                ListDataAwal();
                Cursor.Current = Cursors.Default;
                
            }
        }

        private void cmbTrxFee_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cmbTrxFee.Text != "")
            {
                if (Convert.ToDecimal(cmbTrxFee.Text) != iTransactionFee)
                {
                    //20121127, hermanto_salim, BAALN12003, begin    
                    if (!strFlag.Equals(""))
                        //20121127, hermanto_salim, BAALN12003, end
                        MessageBox.Show("Transaction Fee Tidak Sesuai\nMohon Calculate Ulang", "Peringatan");

                    return;
                }
            }
        }

        private void cmbSafeKeepingFeeAfterTax_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (iSafeKeepingFeeAfterTax != 0)
            {
                if ((Convert.ToDecimal(cmbSafeKeepingFeeAfterTax.Text) != 0) && (Convert.ToDecimal(cmbSafeKeepingFeeAfterTax.Text) != iSafeKeepingFeeAfterTax))
                {
                    MessageBox.Show("Safe Keeping Fee After Tax Tidak Sesuai\nMohon Calculate Ulang", "Peringatan");
                    cmbSafeKeepingFeeAfterTax.SelectedIndex = 0;
                    txtTotalProceed.Value = txtTotalProceed.Value + iSafeKeepingFeeAfterTax;

                    return;
                }
                else if (Convert.ToDecimal(cmbSafeKeepingFeeAfterTax.Text) == 0)
                {
                    txtTotalProceed.Value = txtTotalProceed.Value + iSafeKeepingFeeAfterTax;
                }
                else
                {
                    txtTotalProceed.Value = iTotalProceed;
                }
            }
        }
        //20090911, David, SYARIAH001, begin
        private void nispSettlementDate_onLeave(object sender, EventArgs e)
        {
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            if (nispSettlementDate.Value > 0 && cmpsrNomorSekuriti.Text1 != "")
            {
                //20230215, samypasha, BONDRETAIL-1241, begin
                //bool isValidDate = SuratBerharga.checkRecordingDate(cmpsrNomorSekuriti.Text1.Trim().ToUpper(), nispSettlementDate.Value);
                bool isValidDate = SuratBerharga.checkRecordingDate(cmpsrNomorSekuriti._Text1.Text.ToString().Trim().ToUpper(), nispSettlementDate.Value);
                //20230215, samypasha, BONDRETAIL-1241, end
                if (isValidDate != true)
                {
                    MessageBox.Show("Settlement Date tidak valid", "Peringatan");
                    nispSettlementDate.Text = "";
                    nispSettlementDate.Focus();
                    return;
                }

                //20180530, samypasha, LOGEN00633, begin
                //20230215, samypasha, BONDRETAIL-1241, begin
                //bool isValDate = SuratBerharga.checkSettlementDateTrx(cmpsrNomorSekuriti.Text1.Trim().ToUpper(), nispSettlementDate.Value);
                //20231227, rezakahfi, BONDRETAIL-1513, begin
                if (!isTA)
                {
                    bool isValDate = SuratBerharga.checkSettlementDateTrx(cmpsrNomorSekuriti._Text1.Text.ToString().Trim().ToUpper(), nispSettlementDate.Value);
                    //20230215, samypasha, BONDRETAIL-1241, end
                    if (isValDate != true)
                    {
                        MessageBox.Show("Settlement Date tidak valid", "Peringatan");
                        nispSettlementDate.Text = "";
                        nispSettlementDate.Focus();
                        return;
                    }
                }
                //20231227, rezakahfi, BONDRETAIL-1513, end
                //20180530, samypasha, LOGEN00633, end
            }

            //20160302, fauzil, TRBST16240, begin
            //if (nispSettlementDate.Value > 0 & cmpsrNoRekSecurity.Text1 != "" & Currency.Length > 0)
            //    GetDataDealIdForSwitching();


            //20160302, fauzil, TRBST16240, end

        }
        //20090911, David, SYARIAH001, end

        //20120802, hermanto_salim, BAALN12003, begin    
        private bool subAdditionalValidate()
        {
            bool isValid = true;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Data transaksi tidak valid:\n");

            if (!cmpsrSeller.ValidateField())
            {
                stringBuilder.Append("\nSeller tidak valid.");
                isValid = false;
            }
            else
            {
                DateTime tglExpire = DateTime.ParseExact(txtTglExpiredSertifikasi.Text, "dd-MM-yyyy", null);
                if (DateTime.Today >= tglExpire)
                {
                    stringBuilder.Append("\nSeller tidak valid karena sudah mencapai tanggal expired.");
                    isValid = false;
                }
            }
            //20121127, hermanto_salim, BAALN12003, begin
            //if (cmbJenisTransaksi.Text == "Buy")
            if (cmbJenisTransaksi.Text == "Buy" && strFlag == "Insert")
            //20121127, hermanto_salim, BAALN12003, end
            {
                for (int i = 0; i < dgvTransactionLink.RowCount; i++)
                {
                    if ((bool)dgvTransactionLink.Rows[i].Cells["SelectBit"].Value)
                    {
                        if ((decimal)dgvTransactionLink.Rows[i].Cells["NominalJual"].Value > (decimal)dgvTransactionLink.Rows[i].Cells["ValueLeft"].Value)
                        {
                            stringBuilder.Append("\nNominal Jual pada transaksi baris ke-" + (i + 1) + " lebih besar dari value tersisa.");
                            isValid = false;
                        }
                        //20160413, LOGEN00112, begin 
                        else if ((decimal)dgvTransactionLink.Rows[i].Cells["NominalJual"].Value <= 0)
                        {
                            stringBuilder.Append("\nNominal Jual pada transaksi baris ke-" + (i + 1) + " bernilai 0 atau minus.");
                            isValid = false;
                        }
                        //20160413, LOGEN00112, end 
                    }
                }
            }

            if (!isValid)
                MessageBox.Show(stringBuilder.ToString(), "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return isValid;
        }

        private void cmpsrSeller_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrSeller.Text2 != "")
            {
                object noSertifikat = cmpsrSeller[SELLER_NO_SERTIFIKAT_COLUMN_INDEX];
                txtNoSertifikasiSeller.Text = noSertifikat == null ? "" : (string)noSertifikat;

                object tglExpire = cmpsrSeller[SELLER_EXPIRE_COLUMN_INDEX];
                txtTglExpiredSertifikasi.Text = tglExpire == null ? "" : DateTime.ParseExact(((int)tglExpire).ToString(), "yyyyMMdd", null).ToString("dd-MM-yyyy");
            }
            else
            {
                txtNoSertifikasiSeller.Text = "";
                txtTglExpiredSertifikasi.Text = "";
            }
        }

        private void dgvTransactionLink_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            //20190514, uzia, DIGIT18207, begin
            decimal dummy = 0;
            //20190514, uzia, DIGIT18207, end
            if (e.ColumnIndex == dgvTransactionLink.Columns["SelectBit"].Index)
            {
                Color color = (bool)e.Value ? Color.LightGreen : Color.LightGray;
                dgvTransactionLink.Rows[e.RowIndex].DefaultCellStyle.BackColor = color;
            }
            else if (e.ColumnIndex == dgvTransactionLink.Columns["TrxType"].Index)
            {
                short trxType = (short)e.Value;
                switch (trxType)
                {
                    case 3: e.Value = "Bank Beli"; break;
                    case 4: e.Value = "Bank Jual"; break;
                    case 6: e.Value = "Allotment"; break;
                    //20161031, samy, CSODD16311, begin
                    case 9: e.Value = "Free Of Payment"; break;
                    case 10: e.Value = "Transfer Asset"; break;
                    //20161031, samy, CSODD16311, end
                }
            }
            else if (e.ColumnIndex == dgvTransactionLink.Columns["TrxDate"].Index)
            {
                e.Value = ((DateTime)e.Value).ToString("dd-MM-yyyy HH:mm:ss");
            }
            else if (e.ColumnIndex == dgvTransactionLink.Columns["SettlementDate"].Index)
            {
                e.Value = ((DateTime)e.Value).ToString("dd-MM-yyyy HH:mm:ss");
            }
            else if (e.ColumnIndex == dgvTransactionLink.Columns["FaceValue"].Index)
            {
                e.Value = ((decimal)e.Value).ToString("N", clsGlobal.defaultNumberFormat);
            }
            else if (e.ColumnIndex == dgvTransactionLink.Columns["DealPrice"].Index)
            {
                e.Value = ((decimal)e.Value / 100).ToString("P", clsGlobal.defaultNumberFormat);
            }
            else if (e.ColumnIndex == dgvTransactionLink.Columns["ValueLeft"].Index)
            {
                e.Value = ((decimal)e.Value).ToString("N", clsGlobal.defaultNumberFormat);
            }
            else if (e.ColumnIndex == dgvTransactionLink.Columns["NominalJual"].Index)
            {
                e.Value = ((decimal)e.Value).ToString("N", clsGlobal.defaultNumberFormat);
            }
            //20190514, uzia, DIGIT18207, begin
            //else if (e.ColumnIndex == dgvTransactionLink.Columns["CapitalGainPercent"].Index)
            //{
            //    if(decimal.TryParse(e.Value.ToString(), out dummy))
            //        e.Value = ((decimal)e.Value).ToString("N2", clsGlobal.defaultNumberFormat);
            //}
            //else if (e.ColumnIndex == dgvTransactionLink.Columns["CapitalGainAmt"].Index)
            //{
            //    if (decimal.TryParse(e.Value.ToString(), out dummy))
            //        e.Value = ((decimal)e.Value).ToString("N2", clsGlobal.defaultNumberFormat);
            //}
            //20190514, uzia, DIGIT18207, end
        }

        private void dgvTransactionLink_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvTransactionLink.Columns["SelectBit"].Index || e.ColumnIndex == dgvTransactionLink.Columns["NominalJual"].Index)
            {
                subCalculateTotalFaceValue();
                ClearDataForCalculate();
            }
        }

        private void dgvTransactionLink_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.ColumnIndex == dgvTransactionLink.Columns["NominalJual"].Index)
            {
                MessageBox.Show("Format nominal tidak sesuai, harap masukkan angka tanpa memberikan number separator (separator ribuan).", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void subCalculateTotalFaceValue()
        {
            decimal totalFaceValue = 0;
            //20160831, fauzil, LOGEN196, begin
            FaceValueNonTA = 0;
            FaceValueTA = 0;
            //20160831, fauzil, LOGEN196, end
            //20160519, fauzil, TRBST16240, begin
            int total = 0;
            //20160519, fauzil, TRBST16240, end
            for (int i = 0; i < dgvTransactionLink.RowCount; i++)
            {
                if ((bool)dgvTransactionLink.Rows[i].Cells["SelectBit"].Value)
                {
                    totalFaceValue += (decimal)dgvTransactionLink.Rows[i].Cells["NominalJual"].Value;
                    total = total + 1;
                    //20160831, fauzil, LOGEN196, begin
                    string TaxAmnesty = dgvTransactionLink.Rows[i].Cells["isTaxAmnesty"].Value.ToString();
                    if (TaxAmnesty.Equals("Tax Amnesty", StringComparison.OrdinalIgnoreCase))
                    {
                        FaceValueTA += (decimal)dgvTransactionLink.Rows[i].Cells["NominalJual"].Value;
                        NoRekInvestorTA = dgvTransactionLink.Rows[i].Cells["NoRekInvestor"].Value.ToString();
                    }
                    else
                    {
                        FaceValueNonTA += (decimal)dgvTransactionLink.Rows[i].Cells["NominalJual"].Value;
                        //20221220, yazri, VSYARIAH-340, begin
                        NoRekInvestorNonTA = dgvTransactionLink.Rows[i].Cells["NoRekInvestor"].Value.ToString();
                        //NoRekInvestorNonTA = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                        //20221220, yazri, VSYARIAH-340, end
                    }
                    //20160831, fauzil, LOGEN196, end
                }


            }
            //20160519, fauzil, TRBST16240, begin
            //moneyFaceValue.Value = totalFaceValue;
            //20171204, agireza, TRBST16240, begin
            //if (total > 0)
            moneyFaceValue.Value = totalFaceValue;
            //20171204, agireza, TRBST16240, end
            //20171204, agireza, TRBST16240, end
            //20160519, fauzil, TRBST15176, end

        }


        private void chkTransactionFee_CheckedChanged(object sender, EventArgs e)
        {
            subChangeTransactionFee();
        }

        private void subChangeTransactionFee()
        {
            if (cmbTrxFee.Items.Count > 0)
            {
                if (chkTransactionFee.Checked)
                {
                    int nonZeroIndex = -1;
                    for (int i = 0; i < cmbTrxFee.Items.Count; i++)
                        if (cmbTrxFee.Items[i].ToString() != "0")
                            nonZeroIndex = i;
                    if (nonZeroIndex != -1)
                        cmbTrxFee.SelectedIndex = nonZeroIndex;
                }
                else
                {
                    int zeroIndex = -1;
                    for (int i = 0; i < cmbTrxFee.Items.Count; i++)
                        if (cmbTrxFee.Items[i].ToString() == "0")
                            zeroIndex = i;
                    if (zeroIndex != -1)
                        cmbTrxFee.SelectedIndex = zeroIndex;
                }
            }
            cmbTrxFee_SelectionChangeCommitted(cmbTrxFee, EventArgs.Empty);
        }
        //20120802, hermanto_salim, BAALN12003, end
        //20130305, uzia, BAALN12003, begin
        private void ValidatePhoneOrderFlag(out bool isPremier, out bool isHavingPhoneOrder)
        {
            isPremier = false;
            isHavingPhoneOrder = false;

            TransaksiSuratBerharga clsTrxSuratBerharga = new TransaksiSuratBerharga();
            clsTrxSuratBerharga.CheckPhoneOrder(clsGlobal.QueryCIF, CIFNo, out isPremier, out isHavingPhoneOrder);
        }

        private bool SavePhoneOrderValidation()
        {
            bool isValid = true;
            bool bPremier = false;
            bool bPhoneOrder = false;
            //20160310, fauzil, TRBST16240, begin
            bool cannotSave = false;
            bool newCheck = false;
            //20160310, fauzil, TRBST16240, end
            string strErrMsg = "";
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();

            if (chkFlagPhoneOrder.Checked)
            {
                /* Re-check fasilitas phone order */
                ValidatePhoneOrderFlag(out bPremier, out bPhoneOrder);
                if (!bPhoneOrder)
                {
                    MessageBox.Show("CIF ini tidak memiliki fasilitas phone order !", "Warning"
                        , MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                /* Checking transaksi pertama */
                //if (SuratBerharga.CheckFirstOrder(ObligasiQuery.cQuery, CIFid, out isValid))
                //    if (!isValid)
                //    {
                //        MessageBox.Show("CIF ini baru pertama melakukan transaksi surat order perdana atau jual beli pasar sekunder !", "Warning"
                //            , MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //        return false;
                //    }
            }

            /* Checking no rekening security */
            cmpsrNoRekSecurity.ValidateField();
            if (!cmpsrNoRekSecurity.Text1.Trim().Equals(""))
            {
                // 201603003, fauzil, TRBST16240, begin
                //if (SuratBerharga.CheckNoRekSecurity(ObligasiQuery.cQuery, cmpsrNoRekSecurity.Text1, out isValid, out strErrMsg))
                //    if (!isValid)
                //    {
                //        //20130506, uzia, BAFEM12016, begin
                //        //MessageBox.Show(strErrMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //        //return false;

                //        if(MessageBox.Show(strErrMsg + System.Environment.NewLine +  "Apakah Anda akan tetap menyimpan data ?", "Confirmation"
                //            , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                //            return false;
                //        //20130506, uzia, BAFEM12016, end
                //    }
                System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("en-Us");
                if (SuratBerharga.CheckNoRekSecurity(ObligasiQuery.cQuery, cmpsrNoRekSecurity.Text1, DateTime.ParseExact(nispSettlementDate.Value.ToString(), "yyyyMMdd", cultureInfo), out isValid, out strErrMsg, out this.dLastUpdateJenisRiskProfile, out cannotSave, out newCheck))
                    if (!isValid)
                    {
                        if (cannotSave)
                        {
                            MessageBox.Show(strErrMsg, "WARNING!!!", MessageBoxButtons.OK);
                            return false;
                        }
                        else
                        {
                            if (newCheck)
                            {
                                if (MessageBox.Show(strErrMsg, "Confirmation",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                    NeedUpdateDataNasabah = true;
                                else
                                {
                                    MessageBox.Show("Lakukan update risk profile nasabah di Pro CIF!!", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    NeedUpdateDataNasabah = false;
                                }
                            }
                            else
                            {
                                MessageBox.Show(strErrMsg, "WARNING!!!", MessageBoxButtons.OK);
                                return false;
                            }
                        }
                    }


                // 201603003, fauzil, TRBST16240, end
            }

            return true;
        }
        //20130305, uzia, BAALN12003, end
        //20160815, samy, LOGEN191, begin
        private void ValidateTaxAmnesty(out bool isTaxAmnesty)
        {
            isTaxAmnesty = false;

            TransaksiSuratBerharga clsTrxSuratBerharga = new TransaksiSuratBerharga();
            clsTrxSuratBerharga.CheckTaxAmnesty(this.cQuery, cmpsrNoRekSecurity.Text1, out isTaxAmnesty);
        }

        private void SaveTaxAmnestyValidation()
        {
            bool isTaxAmnesty = false;
            string strErrMsg = "";

            ValidateTaxAmnesty(out isTaxAmnesty);
            if (isTaxAmnesty)
            {
                strErrMsg = "No CIF teridentifikasi sebagai nasabah tax amnesty,  Apabila transaksi adalah untuk Tax Amnesty, pastikan rekening relasi yang digunakan adalah rekening tax amnesty khusus";
                //20160822, samy,LOGEN196, begin
                iTaxAmnesty = true;
                //20160822, samy,LOGEN196, end
                MessageBox.Show(strErrMsg, "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        //20160815, samy,LOGEN191, end
        //20160822, samy,LOGEN196, begin
        private void cbRekeningRelasi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbRekeningRelasi.SelectedIndex != -1 & cbRekeningRelasi.SelectedIndex != 0)
            {
                if (cmbJenisTransaksi.Text == "Sell")
                {
                    string value = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Value;
                    string key = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                    txtRekeningRelasiName.Text = value;
                    if (key.Equals(NoRekInvestorTaxAmnesty))
                        chkTaxAmnesty.Checked = true;
                    else
                        chkTaxAmnesty.Checked = false;
                    effectiveBalance = null;
                    AccountStatus = null;
                    ProductCode = "";
                    AccountType = "";
                    string result = "";
                    string rejectDesc = "";
                    clsCallWebService.CallAccountInquiry(strGuid, intNIK.ToString(), key, out result, out rejectDesc);

                   if (string.IsNullOrEmpty(rejectDesc) && !string.IsNullOrEmpty(result))
                    {
                        DataSet dsData = new DataSet();
                        dsData.ReadXml(new StringReader(result));
                        effectiveBalance = Convert.ToDecimal(dsData.Tables[0].Rows[0]["ATMEffectiveBalance"], System.Globalization.CultureInfo.InvariantCulture);
                        AccountStatus = Convert.ToInt16(dsData.Tables[0].Rows[0]["AccountStatus"]);
                        ProductCode = dsData.Tables[0].Rows[0]["ProductCode"].ToString();
                        AccountType = dsData.Tables[0].Rows[0]["AccountType"].ToString();
                        //20190116, samy, BOSOD18243, begin
                        //20200604, uzia, BONDRETAIL-438, begin
                        //txtSaldoRekening.Text = effectiveBalance.ToString();
                        /* logic agar bisa .00 */
                        decimal dcEffectiveBalance = 0;
                        decimal.TryParse(effectiveBalance.ToString(), out dcEffectiveBalance);
                        string strEffectiveBalance = dcEffectiveBalance.ToString("N2");

                        dcEffectiveBalance = 0;
                        decimal.TryParse(strEffectiveBalance, out dcEffectiveBalance);
                        dcEffectiveBalance = decimal.Round(dcEffectiveBalance, 2);
                        txtSaldoRekening.Value = dcEffectiveBalance;
                        //20200604, uzia, BONDRETAIL-438, end
                        //20190116, samy, BOSOD18243, end
                    }
                }
                //20160915, pengki, LOGEN00204, Begin
                else
                {
                    //20221220, yazri, VSYARIAH-340, begin
                    txtSaldoRekening.Value = 0;
                    //20221220, yazri, VSYARIAH-340, end
                    string value = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Value;
                    string key = ((KeyValuePair<string, string>)cbRekeningRelasi.SelectedItem).Key;
                    txtRekeningRelasiName.Text = value;
                    if (key.Equals(NoRekInvestorTaxAmnesty))
                        chkTaxAmnesty.Checked = true;
                    else
                        chkTaxAmnesty.Checked = false;
                    //20221220, yazri, VSYARIAH-340, begin
                    effectiveBalance = null;
                    AccountStatus = null;
                    ProductCode = "";
                    AccountType = "";
                    string result = "";
                    string rejectDesc = "";
                    clsCallWebService.CallAccountInquiry(strGuid, intNIK.ToString(), key, out result, out rejectDesc);

                    if (string.IsNullOrEmpty(rejectDesc) && !string.IsNullOrEmpty(result))
                    {
                        DataSet dsData = new DataSet();
                        dsData.ReadXml(new StringReader(result));
                        effectiveBalance = Convert.ToDecimal(dsData.Tables[0].Rows[0]["ATMEffectiveBalance"], System.Globalization.CultureInfo.InvariantCulture);
                        AccountStatus = Convert.ToInt16(dsData.Tables[0].Rows[0]["AccountStatus"]);
                        ProductCode = dsData.Tables[0].Rows[0]["ProductCode"].ToString();
                        AccountType = dsData.Tables[0].Rows[0]["AccountType"].ToString();
                        //20190116, samy, BOSOD18243, begin
                        //20200604, uzia, BONDRETAIL-438, begin
                        //txtSaldoRekening.Text = effectiveBalance.ToString();
                        /* logic agar bisa .00 */
                        decimal dcEffectiveBalance = 0;
                        decimal.TryParse(effectiveBalance.ToString(), out dcEffectiveBalance);
                        string strEffectiveBalance = dcEffectiveBalance.ToString("N2");

                        dcEffectiveBalance = 0;
                        decimal.TryParse(strEffectiveBalance, out dcEffectiveBalance);
                        dcEffectiveBalance = decimal.Round(dcEffectiveBalance, 2);
                        txtSaldoRekening.Value = dcEffectiveBalance;
                        //20200604, uzia, BONDRETAIL-438, end
                        //20190116, samy, BOSOD18243, end
                    }
                    //20221220, yazri, VSYARIAH-340, end
                }
                //20160915, pengki, LOGEN00204, end
            }
            else
            {
                txtRekeningRelasiName.Text = "";
                //20221220, yazri, VSYARIAH-340, begin
                txtSaldoRekening.Value = 0;
                //20221220, yazri, VSYARIAH-340, end
                chkTaxAmnesty.Checked = false;
            }
        }

        //20160822, samy,LOGEN196, end
		//20220920, yudha.n, BONDRETAIL-1052, begin
        private void cbRekeningMaterai_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbRekeningMaterai.Enabled = !isMeteraiAbsorbed && (!_bCalculate || (_bCalculate && dMateraiCost != 0));
            if (!isMeteraiAbsorbed && cbRekeningMaterai.SelectedIndex != -1)
            {
                if (cbRekeningMaterai.SelectedIndex != 0)
                {
                    string value = ((KeyValuePair<string, string>)cbRekeningMaterai.SelectedItem).Value;
                    string key = ((KeyValuePair<string, string>)cbRekeningMaterai.SelectedItem).Key;

                    effectiveBalanceMaterai = null;
                    AccountTypeMaterai = "";
                    string result = "";
                    string rejectDesc = "";
                    clsCallWebService.CallAccountInquiry(strGuid, intNIK.ToString(), key, out result, out rejectDesc);

                    if (string.IsNullOrEmpty(rejectDesc) && !string.IsNullOrEmpty(result))
                    {
                        DataSet dsData = new DataSet();
                        dsData.ReadXml(new StringReader(result));
                        effectiveBalanceMaterai = Convert.ToDecimal(dsData.Tables[0].Rows[0]["ATMEffectiveBalance"], System.Globalization.CultureInfo.InvariantCulture);
                        AccountTypeMaterai = dsData.Tables[0].Rows[0]["AccountType"].ToString();
                        /* logic agar bisa .00 */
                        decimal dcEffectiveBalanceMaterai = 0;
                        decimal.TryParse(effectiveBalanceMaterai.ToString(), out dcEffectiveBalanceMaterai);
                        string strEffectiveBalance = dcEffectiveBalanceMaterai.ToString("N2");

                        dcEffectiveBalanceMaterai = 0;
                        decimal.TryParse(strEffectiveBalance, out dcEffectiveBalanceMaterai);
                        dcEffectiveBalanceMaterai = decimal.Round(dcEffectiveBalanceMaterai, 2);
                        txtSaldoRekeningMaterai.Value = dcEffectiveBalanceMaterai;
                    }
                    else
                    {
                        MessageBox.Show("Gagal mendapatkan informasi saldo rekening meterai! " + rejectDesc, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        effectiveBalanceMaterai = 0;
                        txtSaldoRekeningMaterai.Value = 0;
                    }
                }
            }
            else if (cbRekeningMaterai.SelectedIndex == -1)
            {
                effectiveBalanceMaterai = null;
                txtSaldoRekeningMaterai.Value = 0;
                txtSaldoRekeningMaterai.Text = "";
            }
        }
        //20220920, yudha.n, BONDRETAIL-1052, end
        //20160915, pengki, LOGEN00204, Begin
        private void cmpsrNomorSekuriti_Leave(object sender, EventArgs e)
        {
            if (cmpsrSearch1.Text1 != "" && cmbJenisTransaksi.SelectedIndex != -1)
            {
                StateTransaction();
                //20160118, fauzil, TRBST16240, begin
                controlsClear(false);
                //20160118, fauzil, TRBST16240, end
            }
        }
        //20160915, pengki, LOGEN00204, end

        //20160225, fauzil, TRBST16240, begin
        //private void chkSwitching_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (chkSwitching.Checked)
        //    {
        //        lblCmbSwitching.Visible = true;
        //        cmbDealIdSwitching.Visible = true;
        //    }
        //    else
        //    {
        //        lblCmbSwitching.Visible = false;
        //        cmbDealIdSwitching.Visible = false;
        //    }
        //}
        //20160225, fauzil, TRBST16240, End
        //20160301, fauzil, TRBST16240, begin
        //private void GetDataDealIdForSwitching()
        //{
        //    if (cmbDealIdSwitching.DataSource != null)
        //        cmbDealIdSwitching.DataSource = null;

        //    TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
        //    DataSet dsSwitching = new DataSet();

        //    //20171122, agireza, TRBST16240, begin
        //    //dsSwitching = SuratBerharga.GetTransactionBankSellForSwitching(cmpsrNoRekSecurity.Text1, nispSettlementDate.Value, Currency);
        //    dsSwitching = SuratBerharga.GetTransactionBankSellForSwitching(cmpsrNoRekSecurity.Text1, nispSettlementDate.Value, Currency, nispDealDate.Value);
        //    //20171122, agireza, TRBST16240, end
        //    if (dsSwitching.Tables[0].Rows.Count > 0)
        //    {
        //        cmbDealIdSwitching.DataSource = dsSwitching.Tables[0];
        //        cmbDealIdSwitching.DisplayMember = "DealId";
        //        cmbDealIdSwitching.ValueMember = "DealId";
        //        cmbDealIdSwitching.SelectedIndex = -1;
        //    }
        //}

        private void chkOther_CheckedChanged(object sender, EventArgs e)
        {
            if (chkOther.Checked)
            {
                gbSDCntrl.Enabled = true;
                //20190116, samypasha, BOSOD18243, begin
                //gbSDData.Enabled = true;
                //20190116, samypasha, BOSOD18243, end
                //20220308, darul.wahid, BONDRETAIL-892, begin
                this.cmbSourceFund.DataSource = getSourceFund();
                this.cmbSourceFund.ValueMember = "Value";
                this.cmbSourceFund.DisplayMember = "Description";
                this.cmbSourceFund.SelectedIndex = 0;
                //20220308, darul.wahid, BONDRETAIL-892, end
                //20221220, yazri, VSYARIAH-340, begin
                cbRekeningRelasi.Enabled = false;
                //20221220, yazri, VSYARIAH-340, end
            }
            else
            {
                gbSDCntrl.Enabled = false;
                //20190116, samypasha, BOSOD18243, begin
                //gbSDData.Enabled = false;
                //20190116, samypasha, BOSOD18243, end
            }
        }

        private void GetDataSumberDana()
        {
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            DataSet dsSumberDana = new DataSet();
            //20190116, samypasha, BOSOD18243, begin
            //dsSumberDana = SuratBerharga.GetSumberDana();
            //if (dsSumberDana.Tables[0].Rows.Count > 0)
            //{
            //    cmbSumberDana.DataSource = dsSumberDana.Tables[0];                
            //    cmbSumberDana.DisplayMember = "SumberDana";
            //    cmbSumberDana.ValueMember = "SumberDana";                
            //}
            //20190116, samypasha, BOSOD18243, end
        }

        //20190116, samypasha, BOSOD18243, begin
        //private void cmbSumberDana_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (cmbSumberDana.DataSource != null)
        //        if (cmbSumberDana.Items.Count > 0)
        //        {
        //            string value = cmbSumberDana.SelectedValue.ToString();
        //            if (value.ToLower().Contains("matur"))
        //            {
        //                lbltglMatur.Visible = true;
        //                ndMaturSumberDana.Visible = true;
        //                needTanggal = true;
        //            }
        //            else
        //            {
        //                lbltglMatur.Visible = false;
        //                ndMaturSumberDana.Visible = false;
        //                needTanggal = false;
        //            }
        //        }
        //}

        //private void btnAdd_Click(object sender, EventArgs e)
        //{
        //    string SumberDana;
        //    string Tanggal;

        //    if (cmbSumberDana.SelectedIndex == -1)
        //    {
        //        MessageBox.Show("Sumber Dana tidak boleh kosong", "Warning!!", MessageBoxButtons.OK);
        //        return;
        //    }

        //    SumberDana = cmbSumberDana.SelectedValue.ToString();
        //    if (string.IsNullOrEmpty(SumberDana))
        //    {
        //        MessageBox.Show("Sumber Dana tidak boleh kosong", "Warning!!", MessageBoxButtons.OK);
        //        return;
        //    }
        //    if (needTanggal)
        //    {
        //        if (ndMaturSumberDana.Value == 0)
        //        {
        //            MessageBox.Show("Tanggal Maturity harus disi", "Warning!!", MessageBoxButtons.OK);
        //            return;
        //        }
        //        if (ndMaturSumberDana.Value < nispDealDate.Value)
        //        {
        //            MessageBox.Show("Tanggal Maturity tidak boleh kurang dari tanggal transaksi", "Warning!!", MessageBoxButtons.OK);
        //            return;
        //        }
        //        if (ndMaturSumberDana.Value > nispSettlementDate.Value)
        //        {
        //            MessageBox.Show("Tanggal Maturity tidak boleh melebihi tanggal settlement", "Warning!!", MessageBoxButtons.OK);
        //            return;
        //        }
        //        Tanggal = ndMaturSumberDana.Value.ToString();
        //    }
        //    else
        //        Tanggal = "";

        //    //foreach(DataGridViewRow row in dgvSumberDana.Rows)
        //    //{
        //    //    if (string.Equals(SumberDana, row.Cells["SumberDana"].Value.ToString(), StringComparison.OrdinalIgnoreCase))
        //    //    {
        //    //        MessageBox.Show(SumberDana + " sudah dipilih sebelumnya", "Warning!!", MessageBoxButtons.OK);
        //    //        return;
        //    //    }
        //    //}
        //    this.dgvSumberDana.Rows.Add(SumberDana, Tanggal);
        //}

        //private void btnRemove_Click(object sender, EventArgs e)
        //{
        //    if (dgvSumberDana.SelectedRows.Count == 0)
        //        return;

        //    foreach (DataGridViewRow row in dgvSumberDana.SelectedRows)
        //    {
        //        if (!row.IsNewRow)
        //            dgvSumberDana.Rows.Remove(row);
        //    }
        //}

        //private void dgvSumberDana_CellClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0)
        //        return;

        //    int index = e.RowIndex;
        //    dgvSumberDana.Rows[index].Selected = true;
        //}
        public DataTable getSourceFund()
        {
            DataSet dsOut = new DataSet();
            DataTable dtTable = new DataTable();
            try
            {
                //201190723, rezakahfi, LOGAM10236, begin
                //PopulateDataMature("SourceFund", 0, "", out dsOut);
                string strFilter = "OBL";
                PopulateDataMature("SourceFund", 0, strFilter, out dsOut);
                //201190723, rezakahfi, LOGAM10236, end

                return dsOut.Tables[0].Copy();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("getSourceFund : " + ex.Message.ToString());
                return dtTable;
            }

        }
        //20190116, samypasha, BOSOD18243, end
        private void cmpsrGetPushBack_AfterShowSearchForm(object sender, EventArgs e)
        {
            getAmmendData();
            //20221020, Tobias Renal, HFUNDING-181, Begin
            getSimanisData();
            //20221020, Tobias Renal, HFUNDING-181, End
        }
        //20221020, Tobias Renal, HFUNDING-181, Begin
        private void getSimanisData()
        {
            DataSet dsSimanis;
            bool getSimanis = getSimanisData(cmpsrGetPushBack.Text1, out dsSimanis);
            if (dsSimanis.Tables.Count != 0)
            {
                dtSimanis = dsSimanis.Tables[0];
                txtKodeSales.Text = dsSimanis.Tables[0].Rows[0]["KodeSales"].ToString().Trim();
                txtKeteranganSimanis.Text = dsSimanis.Tables[0].Rows[0]["Keterangan"].ToString().Trim();
            }
            else
            {
                txtKodeSales.Text = "";
                txtKeteranganSimanis.Text = "";
            }
        }
        //20221020, Tobias Renal, HFUNDING-181, End
        private void getAmmendData()
        {
            controlsClear(true);
            cmpsrGetPushBack.Focus();            
            if (cmpsrGetPushBack.Text1.Trim().Length == 0)
                return;
            TransaksiSuratBerharga SuratBerharga = new TransaksiSuratBerharga();
            DataSet dsTrx = new DataSet();
            //20240109, rezakahfi, BONDRETAIL-1513, begin
            //bool getTrx = SuratBerharga.getTrxForAmend(cmpsrGetPushBack.Text1.Trim(), 1, out dsTrx);
            DataRow drSecTrx = InqData.InquiryDataTransaction(cmpsrGetPushBack._Text1.Text.ToString());
            Int16 strSecType = 1;
            bool getTrx;
            if (drSecTrx.ItemArray.Length > 0)
            {
                strSecType = Int16.Parse(drSecTrx["SecType"].ToString());
                getTrx = SuratBerharga.getTrxForAmend(cmpsrGetPushBack.Text1.Trim(), strSecType, out dsTrx);
            }
            else
                getTrx = false;
            //20240109, rezakahfi, BONDRETAIL-1513, end
            if (getTrx == true)
            {

                dtAmendTransaksi = dsTrx.Tables[0];

                cmpsrNomorSekuriti.Text1 = dsTrx.Tables[0].Rows[0]["SecurityNo"].ToString().Trim();
                cmpsrNomorSekuriti.ValidateField();

                cmbJenisTransaksi.SelectedValue = dsTrx.Tables[0].Rows[0]["TrxType"].ToString().Trim();

                cmpsrNoRekSecurity.Text1 = dsTrx.Tables[0].Rows[0]["SecAccNo"].ToString().Trim();
                cmpsrNoRekSecurity.ValidateField();
                txtNamaNasabah.Text = dsTrx.Tables[0].Rows[0]["Nama"].ToString().Trim();
                txtCouponRate.Text = dsTrx.Tables[0].Rows[0]["CouponRate"].ToString();
                nispDealDate.Value = int.Parse(dsTrx.Tables[0].Rows[0]["TrxDate"].ToString().Trim());
                nispSettlementDate.Value = int.Parse(dsTrx.Tables[0].Rows[0]["SettlementDate"].ToString().Trim());
                txtAccruedDays.Text = dsTrx.Tables[0].Rows[0]["AccruedDays"].ToString().Trim();
                moneyFaceValue.Text = dsTrx.Tables[0].Rows[0]["FaceValue"].ToString().Trim();
                moneyDealPrice.Text = dsTrx.Tables[0].Rows[0]["DealPrice"].ToString().Trim();
                txtTaxTarif.Text = dsTrx.Tables[0].Rows[0]["Tax"].ToString().Trim();
                txtAccruedInterest.Text = dsTrx.Tables[0].Rows[0]["AccruedInterest"].ToString().Trim();
                txtProceed.Text = dsTrx.Tables[0].Rows[0]["Proceed"].ToString().Trim();
                txtPajakBungaBerjalan.Text = dsTrx.Tables[0].Rows[0]["TaxOnAccrued"].ToString().Trim();
                txtTaxOnCapitalGainLoss.Text = dsTrx.Tables[0].Rows[0]["TaxOnCapitalGainLoss"].ToString().Trim();

                cmbSafeKeepingFeeAfterTax.Items.Clear();
                cmbSafeKeepingFeeAfterTax.Items.Add(0);
                cmbSafeKeepingFeeAfterTax.Items.Add(dsTrx.Tables[0].Rows[0]["SafeKeepingFeeTaxTarif"].ToString().Trim());
                cmbSafeKeepingFeeAfterTax.Text = dsTrx.Tables[0].Rows[0]["SafeKeepingFeeTaxTarif"].ToString().Trim();


                decimal transactionFee = iTransactionFee = dsTrx.Tables[0].Rows[0]["TransactionFeeAmount"].ToString() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["TransactionFeeAmount"].ToString());

                cmbTrxFee.Items.Clear();
                cmbTrxFee.Items.Add(0);
                cmbTrxFee.Items.Add(transactionFee);
                chkTransactionFee.Checked = transactionFee != 0;
                subChangeTransactionFee();

                txtTotalProceed.Text = dsTrx.Tables[0].Rows[0]["TotalProceed"].ToString().Trim();
                //20220920, yudha.n, BONDRETAIL-1052, begin
                dUpdateMateraiCost = dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString());
                txtMateraiCost.Value = dUpdateMateraiCost;
                updateNorekMeterai = dsTrx.Tables[0].Rows[0]["NoRekMaterai"].ToString().Trim();
                isMeteraiAbsorbed = bool.Parse(dsTrx.Tables[0].Rows[0]["IsAbsorbedByBank"].ToString());
                updateMateraiAccountBlockACTYPE = dsTrx.Tables[0].Rows[0]["MateraiAccountBlockACTYPE"].ToString();
                updateMateraiAccountBlockSequence = int.Parse(dsTrx.Tables[0].Rows[0]["MateraiAccountBlockSequence"].ToString());
                //20220920, yudha.n, BONDRETAIL-1052, begin
                cmpsrSeller.Text1 = dsTrx.Tables[0].Rows[0]["NIK_CS"].ToString().Trim();
                cmpsrSeller.ValidateField();
                cmpsrDealer.Text1 = dsTrx.Tables[0].Rows[0]["NIK_Dealer"].ToString().Trim();
                cmpsrDealer.ValidateField();
                cmpsrNIKRef.Text1 = dsTrx.Tables[0].Rows[0]["NIK_Ref"].ToString().Trim();
                cmpsrNIKRef.ValidateField();
                strFlag = "Search";
                controlsEnabled(false);
                cmpsrNomorSekuriti.Enabled = false;
                cmpsrSearch1.Enabled = false;
                cmbJenisTransaksi.Enabled = false;
                chkFlagPhoneOrder.Checked = bool.Parse(dsTrx.Tables[0].Rows[0]["PhoneOrderBit"].ToString());
                //20190715, darul.wahid, BOSIT18196, begin
                //chkFlagPhoneOrder.Enabled = false;      
                //20190919, darul.wahid, BOSIT18196, begin
                //if (cmpsrGetPushBack.Visible)
                  //  chkFlagPhoneOrder.Enabled = true;
                //else
                  //  chkFlagPhoneOrder.Enabled = false;
                //20190919, darul.wahid, BOSIT18196, end
                //20190715, darul.wahid, BOSIT18196, end
                ndHargaPublish.Text = dsTrx.Tables[0].Rows[0]["HargaORI"].ToString().Trim();
                ndHargaModal.Text = dsTrx.Tables[0].Rows[0]["HargaModal"].ToString().Trim();
                //20161019, fauzil, TRBST16240, begin
                if (dsTrx.Tables[0].Rows[0]["HargaModalAwal"].GetType() != typeof(DBNull))
                    HargaModalAwal = decimal.Parse(dsTrx.Tables[0].Rows[0]["HargaModalAwal"].ToString().Trim());
                else
                    HargaModalAwal = decimal.Parse(dsTrx.Tables[0].Rows[0]["HargaModal"].ToString().Trim());
                //20161019, fauzil, TRBST16240, end
                updateNorekSecurity = dsTrx.Tables[0].Rows[0]["NoRekInvestor"].ToString().Trim();
                //20200917, rezakahfi, BONDRETAIL-550, begin
                //GetDataDealIdForSwitching();
                //if (dsTrx.Tables[0].Rows[0]["FlagSwitching"].GetType() != typeof(DBNull))
                //    chkSwitching.Checked = bool.Parse(dsTrx.Tables[0].Rows[0]["FlagSwitching"].ToString());
                //if (dsTrx.Tables[0].Rows[0]["DealIdSwitching"].GetType() != typeof(DBNull))
                //    cmbDealIdSwitching.SelectedValue = dsTrx.Tables[0].Rows[0]["DealIdSwitching"].ToString().Trim();
                //20200917, rezakahfi, BONDRETAIL-550, end
                if (dsTrx.Tables[0].Rows[0]["FlagOther"].GetType() != typeof(DBNull))
                    chkOther.Checked = bool.Parse(dsTrx.Tables[0].Rows[0]["FlagOther"].ToString());
                
                //20220208, darul.wahid, BONDRETAIL-895, begin
                this.dCapitalGain = dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim());
                //20220208, darul.wahid, BONDRETAIL-895, end
                //20220708, darul.wahid, BONDRETAIL-977, begin
                this.dCapitalGainNonIDR = dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["CapitalGain"].ToString().Trim());
                this.dTotalTax = dsTrx.Tables[0].Rows[0]["TotalTax"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["TotalTax"].ToString().Trim());
                this.dIncome = dsTrx.Tables[0].Rows[0]["Income"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["Income"].ToString().Trim());
                //20220708, darul.wahid, BONDRETAIL-977, end
                //20220920, yudha.n, BONDRETAIL-1052, begin
                this.dMateraiCost = dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["NominalMaterai"].ToString().Trim());
                //20220920, yudha.n, BONDRETAIL-1052, end
                //20240422, alfian.andhika, BONDRETAIL-1581, begin
                this.dYieldHargaModal = dsTrx.Tables[0].Rows[0]["YieldHargaModal"].ToString().Trim() == "" ? 0 : decimal.Parse(dsTrx.Tables[0].Rows[0]["YieldHargaModal"].ToString().Trim());
                //20240422, alfian.andhika, BONDRETAIL-1581, end

                ndHargaPublish.Enabled = false;
                ndHargaModal.Enabled = false;
                //20200917, rezakahfi, BONDRETAIL-550, begin
                //chkSwitching.Enabled = false;
                //cmbDealIdSwitching.Enabled = false;
                //20200917, rezakahfi, BONDRETAIL-550, end
                chkOther.Enabled = false;
                cmpsrNomorSekuriti.Enabled = false;
                cmpsrSearch1.Enabled = false;
                cmbJenisTransaksi.Enabled = false;

                if (chkOther.Checked)
                {
                    flagClear = true;
                    gbSumberDana.Visible = true;
                    //20210309, rezakahfi, BONDRETAIL-703, begin
                    //gbSDCntrl.Enabled = false;
                    gbSDCntrl.Enabled = true;
                    //20210309, rezakahfi, BONDRETAIL-703, end
                    //gbSDData.Enabled = true;
                    //btnRemove.Enabled = false;
                    DataSet dsResult = new DataSet();
                    dsResult = SuratBerharga.GetSecurityTransactionSumberDana(long.Parse(cmpsrGetPushBack.Text1.ToString().Trim()));
                    if (dsResult.Tables.Count > 0)
                    {
                        //20190116, samypasha, BOSOD18243, begin
                        //if (dgvSumberDana.Columns.Count > 0)
                        //{
                        //    dgvSumberDana.Columns.Remove("SumberDana");
                        //    dgvSumberDana.Columns.Remove("Tanggal");
                        //}
                        //dgvSumberDana.DataSource = dsResult.Tables[0];
                        //20200917, rezakahfi, BONDRETAIL-550, begin
                        //dgvSourceFund.DataSource = dsResult.Tables[0];
                        this.dtSourceOfFund = dsResult.Tables[0];
                        //20200917, rezakahfi, BONDRETAIL-550, end
                        //20190116, samypasha, BOSOD18243, end
                    }
                }
                else
                    gbSumberDana.Visible = false;

                if (int.Parse(cmbJenisTransaksi.SelectedValue.ToString()) == 3) //bank beli
                {
                    //tampilkan data link agar bisa diupdate nominal jualnya
                    if (cmbJenisTransaksi.Text == "Buy")
                    {
                        if (dgvTransactionLink.DataSource == null)
                        {
                            DataSet dsResult = new DataSet();
                            lastSecAccNo = cmpsrNoRekSecurity.Text1;
                            //20230215, samypasha, BONDRETAIL-1241, begin
                            //lastSecurityNo = cmpsrNomorSekuriti.Text1;
                            lastSecurityNo = cmpsrNomorSekuriti._Text1.Text.ToString();
                            //20230215, samypasha, BONDRETAIL-1241, end
                            if (clsDatabase.subTRSPopulateTransactionBalanceForUpdate(cQuery, new object[] { cmpsrGetPushBack.Text1.ToString().Trim() }, out dsResult))
                            {
                                dgvTransactionLink.DataSource = dsResult.Tables[0];
                                //20200519, uzia, BONDRETAIL-369, begin
                                //for (int i = 0; i < dgvTransactionLink.ColumnCount; i++)
                                //    dgvTransactionLink.Columns[i].Visible = false;
                                for (int i = 0; i < dgvTransactionLink.ColumnCount; i++)
                                {
                                    dgvTransactionLink.Columns[i].Visible = false;
                                    dgvTransactionLink.Columns[i].ReadOnly = true;
                                }
                                //20200519, uzia, BONDRETAIL-369, end
                                for (int i = 0; i < dgvTransactionLinkColumnProperty.Rows.Count; i++)
                                {
                                    string columnName = (string)dgvTransactionLinkColumnProperty.Rows[i]["ColumnName"];
                                    //20200519, uzia, BONDRETAIL-369, begin
                                    //dgvTransactionLink.Columns[columnName].ReadOnly = (bool)dgvTransactionLinkColumnProperty.Rows[i]["IsReadOnly"];
                                    dgvTransactionLink.Columns[columnName].ReadOnly = true;
                                    //20200519, uzia, BONDRETAIL-369, end
                                    dgvTransactionLink.Columns[columnName].Frozen = (bool)dgvTransactionLinkColumnProperty.Rows[i]["IsFrozen"];
                                    if (dgvTransactionLink.Columns[columnName].Name == "SelectBit")
                                        dgvTransactionLink.Columns[columnName].Visible = false;
                                    else
                                        dgvTransactionLink.Columns[columnName].Visible = true;
                                    dgvTransactionLink.Columns[columnName].HeaderText = (string)dgvTransactionLinkColumnProperty.Rows[i]["ColumnAlias"];
                                }
                            }
                        }
                    }
                    else
                    {
                        dgvTransactionLink.DataSource = null;
                    }
                }
                else
                { //bank jual 
                    //20200917, rezakahfi, BONDRETAIL-550, begin
                    //if (chkSwitching.Checked)
                    //{
                    //    lblCmbSwitching.Visible = true;
                    //    cmbDealIdSwitching.Visible = true;
                    //}
                    //else
                    //{
                    //    lblCmbSwitching.Visible = false;
                    //    cmbDealIdSwitching.Visible = false;
                    //}
                    //20200917, rezakahfi, BONDRETAIL-550, end

                    chkOther.Visible = true;
                }

                btnCalculate.Enabled = true;
                moneyDealPrice.Enabled = true;
                cmpsrSeller.Enabled = true;
                if (dsTrx.Tables[0].Rows[0]["Functional"].GetType() != typeof(DBNull))
                    txtFuncGrp.Text = dsTrx.Tables[0].Rows[0]["Functional"].ToString();
                this.NISPToolbarButton("6").Visible = true;
                ClearDataForCalculate();
                strFlag = "Update";
                ListDataAwal();

                //20240109, rezakahfi, BONDRETAIL-1513, begin
                if (cmpsrNomorSekuriti._Text1.Text.ToString() != "")
                {
                    DataRow drSecMaster = InqData.InquiryDataSecurity(cmpsrNomorSekuriti._Text1.Text.ToString());
                    if (drSecMaster.ItemArray.Length > 0)
                    {
                        nispMaturityDate.Value = int.Parse(DateTime.Parse(drSecMaster["MaturityDate"].ToString()).ToString("yyyyMMdd"));
                        txtBondType.Text = drSecMaster["SecTypeDesc"].ToString().Trim();
                        if (drSecMaster["ImbalHasil"].ToString().Contains("1"))
                        {
                            if (!tcImbalHasil.TabPages.Contains(pgKupon))
                                tcImbalHasil.TabPages.Add(pgKupon);
                            if (tcImbalHasil.TabPages.Contains(pgDiskonto))
                                tcImbalHasil.TabPages.Remove(pgDiskonto);
                        }
                        else
                        {
                            if (!tcImbalHasil.TabPages.Contains(pgDiskonto))
                                tcImbalHasil.TabPages.Add(pgDiskonto);
                            if (tcImbalHasil.TabPages.Contains(pgKupon))
                                tcImbalHasil.TabPages.Remove(pgKupon);
                        }
                    }
                    DateTime SettDate = DateTime.ParseExact(nispSettlementDate.Text, "dd-MM-yyyy", null);
                    DateTime MattDate = DateTime.ParseExact(nispMaturityDate.Text, "dd-MM-yyyy", null);
                    txtTenor.Value = decimal.Parse((MattDate - SettDate).TotalDays.ToString());
                }
                //20240109, rezakahfi, BONDRETAIL-1513, end
            }
            else
            {
                MessageBox.Show("Gagal mengambil data transaksi", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmpsrGetPushBack.Focus();
            }
        }

        private void ValidateHargaModal(out string errMsg, out decimal HargaOri, out decimal HargaModal)
        {
            errMsg = "";
            HargaOri = 0;
            HargaModal = 0;
            OleDbParameter[] dbParam = new OleDbParameter[7];
            //20230215, samypasha, BONDRETAIL-1241, begin
            //(dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti.Text1;
            (dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti._Text1.Text.ToString();
            //20230215, samypasha, BONDRETAIL-1241, end
            (dbParam[1] = new OleDbParameter("@pcJenisTransaksi", OleDbType.VarChar, 30)).Value = cmbJenisTransaksi.Text;
            (dbParam[2] = new OleDbParameter("@pcParameter", OleDbType.VarChar, 50)).Value = "HargaOri";
            (dbParam[3] = new OleDbParameter("@pcHargaModal", OleDbType.Decimal)).Value = decimal.Parse(ndHargaModal.Text);
            (dbParam[4] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 200)).Direction = ParameterDirection.Output;
            (dbParam[5] = new OleDbParameter("@pcHargaModalNew", OleDbType.Decimal)).Direction = ParameterDirection.Output;
            (dbParam[6] = new OleDbParameter("@pcHargaOriNew", OleDbType.Decimal)).Direction = ParameterDirection.Output;

            bool blnResult = cQuery.ExecProc("TRSValidateHargaModal", ref dbParam);

            if (blnResult)
            {
                errMsg = dbParam[4].Value.ToString();
                //20171206, agireza, TRBST16240, begin
                //HargaModal = decimal.Parse(dbParam[5].Value.ToString());
                //HargaOri = decimal.Parse(dbParam[6].Value.ToString());

                if (dbParam[5].Value.ToString() != "")
                {
                    HargaModal = decimal.Parse(dbParam[5].Value.ToString());
                }
                else 
                {
                    errMsg = "Harga Modal belum disetting";
                }

                if (dbParam[6].Value.ToString() != "")
                {
                    HargaOri = decimal.Parse(dbParam[6].Value.ToString());
                }
                else 
                {
                    errMsg = "Harga ORI belum disetting pada menu Parameter Harga Modal";
                }
                
                
                //20171206, agireza, TRBST16240, end
            }
        }
        //20231227, rezakahfi, BONDRETAIL-1513, begin
        public bool ValidateProduct()
        {
            bool bResult = true;

            DataSet dshargaORI;
            OleDbParameter[] dbParam = new OleDbParameter[4];
            //20230215, samypasha, BONDRETAIL-1241, begin
            //(dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti.Text1;
            (dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti._Text1.Text.ToString();
            //20230215, samypasha, BONDRETAIL-1241, end
            (dbParam[1] = new OleDbParameter("@pcJenisTransaksi", OleDbType.VarChar, 30)).Value = cmbJenisTransaksi.Text;
            (dbParam[2] = new OleDbParameter("@pcParameter", OleDbType.VarChar, 50)).Value = "HargaOri";
            //20211012, rezakahfi, BONDRETAIL-814, begin
            (dbParam[3] = new OleDbParameter("@pnFaceValue", OleDbType.Decimal)).Value = moneyFaceValue.Value;
            //20211012, rezakahfi, BONDRETAIL-814, end

            bResult = cQuery.ExecProc("TRSPopulateDataTransaksiSuratBerharga", ref dbParam, out dshargaORI);

            if (bResult)
            {
                if (dshargaORI.Tables.Count > 2)
                {
                    if (dshargaORI.Tables[2].Rows.Count > 0)
                    {
                        bool bStop = false;
                        for (int i = 0; i < dshargaORI.Tables[2].Rows.Count; i++)
                        {
                            if (bool.Parse(dshargaORI.Tables[2].Rows[i]["Stoper"].ToString())
                                && (dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "All"
                                    || dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "TA"
                                    )
                                )
                            {
                                MessageBox.Show(dshargaORI.Tables[2].Rows[i]["Description"].ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                bStop = true;
                            }
                            else if (!bool.Parse(dshargaORI.Tables[2].Rows[i]["Stoper"].ToString())
                                && (dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "All"
                                    || dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "TA"
                                    ))
                            {
                                if (MessageBox.Show(dshargaORI.Tables[2].Rows[i]["Description"].ToString(), "Warnings", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                    bStop = true;
                            }
                        }

                        if (bStop)
                        {
                            ndHargaPublish.Value = 0;
                            moneyDealPrice.Value = 0;
                            ndHargaModal.Value = 0;

                            controlsClear(true);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Error saat melakukan pengecekan harga modal", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return bResult;
        }
        //20231227, rezakahfi, BONDRETAIL-1513, end
        //20220331, darul.wahid, ONEMBL-1279, begin
        //private void ValidateSettlementDate(string SecurityNo, string SettleDateTrx, out string errMsg, out string SettleDateNew)
        private void ValidateSettlementDate(string SecurityNo, string SettleDateTrx, int TrxType, string Channel, out string errMsg, out string SettleDateNew)
        //20220331, darul.wahid, ONEMBL-1279, end
        {
            errMsg = "";
            SettleDateNew = "";
            //20220331, darul.wahid, ONEMBL-1279, begin
            //OleDbParameter[] dbParam = new OleDbParameter[4];
            OleDbParameter[] dbParam = new OleDbParameter[6];
            //20220331, darul.wahid, ONEMBL-1279, end
            (dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20)).Value = SecurityNo;
            (dbParam[1] = new OleDbParameter("@pcSettleDateTrx", OleDbType.VarChar, 12)).Value = SettleDateTrx;
            (dbParam[2] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 200)).Direction = ParameterDirection.Output;
            (dbParam[3] = new OleDbParameter("@pcSettleDateNew", OleDbType.VarChar, 12)).Direction = ParameterDirection.Output;
            //20220331, darul.wahid, ONEMBL-1279, begin
            (dbParam[4] = new OleDbParameter("@pcTrxType", OleDbType.Integer)).Value = TrxType;
            (dbParam[5] = new OleDbParameter("@pcChannelTrx", OleDbType.VarChar, 15)).Value = Channel;
            //20220331, darul.wahid, ONEMBL-1279, end

            bool blnResult = cQuery.ExecProc("TRSValidateSettlementDate", ref dbParam);

            if (blnResult)
            {
                errMsg = dbParam[2].Value.ToString();
                SettleDateNew = dbParam[3].Value.ToString();
            }
        }

        private void nispDealDate_onNispDateChanged(object sender, EventArgs e)
        {
            ClearDataForCalculate();
        }

        private void nispSettlementDate_onNispDateChanged(object sender, EventArgs e)
        {
            ClearDataForCalculate();
        }
        //20160301, fauzil, TRBST15176, end

        //20200121, dion, TR12020-1, BONDRETAIL-82, begin
        //20210804, irene, BONDRETAIL-796, begin
        //private void ValidateKitas(DataSet dsCustomer, DataSet dsSecurities, string trxType, out string errMsg)
        //{
        //    errMsg = "";

        //    if (dsCustomer != null && dsSecurities != null)
        //    {
        //        if (dsCustomer.Tables[0].Rows.Count > 0 && dsSecurities.Tables[0].Rows.Count > 0)
        //        {
        //            string citizenship = dsCustomer.Tables[0].Rows[0]["Citizenship"].ToString().Trim();
        //            string npwp = dsCustomer.Tables[0].Rows[0]["NPWP"].ToString().Trim();
        //            string kitasNo = dsCustomer.Tables[0].Rows[0]["KITASNo"].ToString().Trim();
        //            string strKitasExpDate = "";

        //            if (dsCustomer.Tables[0].Rows[0]["KITASExpDate"].GetType() != typeof(DBNull))
        //                strKitasExpDate = dsCustomer.Tables[0].Rows[0]["KITASExpDate"].ToString().Trim();

        //            decimal tax = Convert.ToDecimal(dsSecurities.Tables[0].Rows[0]["TaxWNAWithoutNPWP"].ToString());
                    //if (citizenship != "000" && !string.IsNullOrEmpty(citizenship))
                    //{
                        // WNA
                        //if (string.IsNullOrEmpty(npwp) || string.IsNullOrEmpty(kitasNo) || string.IsNullOrEmpty(strKitasExpDate))
                        //{
                        //    errMsg = "Karena nasabah tidak memiliki NPWP dan KITAS, maka saat nasabah jual obligasi akan dikenakan PPh lebih besar. Lakukan pengkinian data di Pro CIF. \nApakah ingin lanjut?";
                    //    }
                    //    else
                    //    {
                    //        if (DateTime.ParseExact(strKitasExpDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture).Date < DateTime.Now.Date)
                    //        {
                    //            string strTaxPct = tax.ToString("N") + "%";
                    //            if (trxType.ToLower() == "buy")
                    //            {
                    //                errMsg = string.Format("Kitas telah expired. Informasikan ke Nasabah bahwa Pajak atas transaksi ini adalah {0}. \nApakah ingin lanjut?", strTaxPct);
                    //            }
                    //            else
                    //            {
                    //                errMsg = "Kitas telah expired. Informasikan hal ini ke Nasabah sebelum Nasabah Jual Obligasi ini. \nApakah ingin lanjut?";
                    //            }
                    //        }
                    //        else if ((DateTime.ParseExact(strKitasExpDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) - DateTime.Now).TotalDays <= 30)
                    //        {
                    //            strKitasExpDate = (DateTime.ParseExact(strKitasExpDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)).ToString("dd-MMM-yyyy");
                    //            errMsg = string.Format("Kitas akan expired tanggal [{0}]. Informasikan hal ini kepada Nasabah untuk segera update Kitas. \nApakah ingin lanjut?", strKitasExpDate);
                    //        }
                    //    }
                    //}
        //        }
        //    }
        //}
        //20210804, irene, BONDRETAIL-796, end
        //20200121, dion, TR12020-1, BONDRETAIL-82, end

        //20171006, agireza, COPOD17271, begin
        public void ClearDataForCalculate()
        {
            isNeedRecalculate = true;
        }
        
        private void moneyFaceValue_onNispMoneyValueChanged(object sender, EventArgs e)
        {
            //20171219, agireza, TRBST16240, begin
            ClearDataForCalculate();
            //20171219, agireza, TRBST16240, end
        }

        private void moneyDealPrice_onNispMoneyValueChanged(object sender, EventArgs e)
        {
            ClearDataForCalculate();
        }

        private void txtTaxTarif_onNispMoneyValueChanged(object sender, EventArgs e)
        {
            ClearDataForCalculate();
        }
        //20190514, uzia, DIGIT18207, begin
        private void SetTrxLinkCapitalGain(DataTable dtCalc)
        {
            if (dtCalc == null || dtCalc.Rows.Count == 0)
                return;

            for (int i = 0; i < dgvTransactionLink.RowCount; i++)
            {
                for (int j = 0; j < dtCalc.Rows.Count; j++)
                {
                    if (dgvTransactionLink["DealId", i].Value.ToString().Equals(dtCalc.Rows[j]["DealId"].ToString()))
                    {
                        dgvTransactionLink["CapitalGainPercent", i].Value = dtCalc.Rows[j]["CapitalGainPercent"];
                        dgvTransactionLink["CapitalGainAmt", i].Value = dtCalc.Rows[j]["CapitalGainAmt"];
                    }
                }
            }
        }
        //20190514, uzia, DIGIT18207, end
        //20171006, agireza, COPOD17271, begin  
        //20190116, samypasha, BOSOD18243, begin
        public void createTableSource()
        {
            DataTable dtSource = new DataTable();
            dtSource.Columns.Add("SourceData");
            dtSource.Columns.Add("DealIdSource");
            dtSource.Columns.Add("Account");
            dtSource.Columns.Add("CCY");
            dtSource.Columns.Add("Amount");
            dtSource.Columns.Add("Value_Date");
            //--------------------------------------
            dtSource.Columns.Add("ValueDate");
            dtSource.Columns.Add("isManual");

            dtSourceOfFund = dtSource.Copy();
        }

        public void LockSourceOfFund(bool bLock)
        {
            //cmpsrCIF.Enabled = bLock;
            //cmbCIF2.Enabled = bLock;
            //----------------------------------------
            //txtre.Enabled = bLock;
            //cmbCcyPair.Enabled = bLock;
        }

        private void VisiblePanelSF(bool bVisible)
        {
            lblTotalAmountSF.Visible = bVisible;
            txtTotalAmountSource.Visible = bVisible;
            dgvSourceFund.Visible = bVisible;

            btnAddSourceOfFund.Visible = bVisible;
            btnResetSourceFund.Visible = bVisible;
            cmbSourceFund.Visible = bVisible;
        }

        public void AddSourceFund(string SourceData, string DealId, string Account, string CCY, decimal Amount
                                    , string Value_Date, string ValueDate, bool isManual)
        {
            dtSourceOfFund.Rows.Add(SourceData, DealId, Account, CCY, Amount, Value_Date, ValueDate, isManual);
            dtSourceOfFund.AcceptChanges();
        }

        private void btnResetSourceFund_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to Reset Source of Fund ?\n"
                              + "(Transaction Data Will be Enabled)"
                              , "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question
                      ) == DialogResult.Yes)
            {
                this.createTableSource();
                LockSourceOfFund(true);
                cbRekeningRelasi.Enabled = true;
            }
        }

        private void pnlSourceOfFund_ControlRemoved(object sender, ControlEventArgs e)
        {
            if (e.Control.Name == "dlgFMCTSourceFund")
            {
                VisiblePanelSF(true);


                if (myFormSourceFund.strEndProcess == "Confirm")
                {
                    if (myFormSourceFund.strProcess == "Add")
                    {
                        //20210309, rezakahfi, BONDRETAIL-703, begin
                        string strCommand = "";
                        if (myFormSourceFund.strSourceDataReturn.ToUpper() == "REKENING")
                        {
                            strCommand = "SourceData = '" + myFormSourceFund.strSourceDataReturn + "'";
                        }
                        else
                        {
                            strCommand = "SourceData = '" + myFormSourceFund.strSourceDataReturn + "' and DealIdSource = '" + myFormSourceFund.strDealNoReturn + "'";
                        }
                        //20210309, rezakahfi, BONDRETAIL-703, end
                        if (
                              (this.dtSourceOfFund.Select(strCommand).Length > 0)
                            )
                        {
                            MessageBox.Show("sumber dana sudah ditambahkan sebelumnya " + myFormSourceFund.strSourceDataReturn + "(" + myFormSourceFund.strDealNoReturn + ")"
                                            , "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning
                                    );
                            return;
                        }

                        AddSourceFund(myFormSourceFund.strSourceDataReturn, myFormSourceFund.strDealNoReturn, myFormSourceFund.strAccountReturn, myFormSourceFund.strCurrencyReturn
                                        , myFormSourceFund.dcAmountReturn, myFormSourceFund.strValue_DateReturn
                                        , myFormSourceFund.strValueDateReturn, myFormSourceFund.bIsManualReturn
                                        );
                    }
                    else if (myFormSourceFund.strProcess == "Update")
                    {
                        for (int i = 0; i < dgvSourceFund.Rows.Count; i++)
                        {
                            if (dgvSourceFund["SourceData", i].Value.ToString() == myFormSourceFund.strSourceDataReturn
                                    && dgvSourceFund["DealIdSource", i].Value.ToString() == myFormSourceFund.strDealNoReturn
                                )
                            {
                                dgvSourceFund["SourceData", i].Value = myFormSourceFund.strSourceDataReturn;
                                dgvSourceFund["CCY", i].Value = myFormSourceFund.strCurrencyReturn;
                                dgvSourceFund["DealIdSource", i].Value = myFormSourceFund.strDealNoReturn;
                                dgvSourceFund["Account", i].Value = myFormSourceFund.strAccountReturn;
                                dgvSourceFund["ValueDate", i].Value = myFormSourceFund.strValueDateReturn;
                                dgvSourceFund["Value_Date", i].Value = myFormSourceFund.strValue_DateReturn;
                                dgvSourceFund["Amount", i].Value = myFormSourceFund.dcAmountReturn;
                            }
                        }
                    }
                }
                else if (myFormSourceFund.strEndProcess == "Delete")
                {
                    for (int i = 0; i < dgvSourceFund.Rows.Count; i++)
                    {
                        if (dgvSourceFund["SourceData", i].Value.ToString() == myFormSourceFund.strSourceDataReturn
                                && dgvSourceFund["DealIdSource", i].Value.ToString() == myFormSourceFund.strDealNoReturn
                            )
                        {
                            dgvSourceFund.Rows.RemoveAt(i);
                        }
                    }
                }

                dtSourceOfFund.AcceptChanges();

                if (myFormSourceFund.strEndProcess != "Cancel")
                {
                    //string strAmount = dtSourceOfFund.Compute("SUM(Amount)", string.Empty).ToString();
                    decimal dcAmountTotal = 0;

                    foreach (DataRow drRow in dtSourceOfFund.Rows)
                        dcAmountTotal += decimal.Parse(drRow["Amount"].ToString());

                    Double dblText = 0;
                    Double.TryParse(dcAmountTotal.ToString(), out dblText);

                    txtTotalAmountSource.Text = dblText.ToString("N");
                }

                myFormSourceFund.strSourceDataReturn = "";
                myFormSourceFund.strCurrencyReturn = "";
                myFormSourceFund.strDealNoReturn = "";
                myFormSourceFund.strAccountReturn = "";
                myFormSourceFund.strValueDateReturn = "";
                myFormSourceFund.strValue_DateReturn = "";
                myFormSourceFund.dcValueDateMax = 0;
                myFormSourceFund.dcAmountMaximal = 0;
                myFormSourceFund.dcAmountReturn = 0;

            }
            LockSourceOfFund(true);
        }

        private void dgvSourceFund_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            myFormSourceFund = new dlgFMCTSourceFund();

            if (btnAddSourceOfFund.Visible)
                myFormSourceFund.strProcess = "Update";
            else
                myFormSourceFund.strProcess = "view";

            myFormSourceFund.strProduct = "Obligasi";
            //20210315, rezakahfi, BONDRETAIL-703, begin
            if (dgvSourceFund.Columns.Contains("SourceData"))
                SourceFund = dgvSourceFund["SourceData", e.RowIndex].Value.ToString();
            //20210315, rezakahfi, BONDRETAIL-703, end

            if (SourceFund == "SavingAccount" || SourceFund.ToUpper() == "REKENING")
            {
                myFormSourceFund.strSourceData = dgvSourceFund["SourceData", e.RowIndex].Value.ToString();
                myFormSourceFund.strCurrency = dgvSourceFund["CCY", e.RowIndex].Value.ToString();
                myFormSourceFund.strDealNo = dgvSourceFund["DealIdSource", e.RowIndex].Value.ToString();
                myFormSourceFund.strAccount = dgvSourceFund["Account", e.RowIndex].Value.ToString();
                myFormSourceFund.strValueDateReturn = dgvSourceFund["ValueDate", e.RowIndex].Value.ToString();
                myFormSourceFund.strValue_Date = dgvSourceFund["Value_Date", e.RowIndex].Value.ToString();
                myFormSourceFund.dcAmount = decimal.Parse(dgvSourceFund["Amount", e.RowIndex].Value.ToString());
                //20200604, uzia, BONDRETAIL-438, begin
                //myFormSourceFund.dcAmountMaximal = decimal.Parse(txtSaldoRekening.Text);
                //myFormSourceFund.dcBalanceSource = decimal.Parse(txtSaldoRekening.Text);
                myFormSourceFund.dcAmountMaximal = txtSaldoRekening.Value;
                myFormSourceFund.dcBalanceSource = txtSaldoRekening.Value;
                //20200604, uzia, BONDRETAIL-438, end
            }
            else
            {
                DataTable dtSource = new DataTable();
                this.SourceFund = dgvSourceFund["SourceData", e.RowIndex].Value.ToString();

                if (!isManual(out dtSource))
                {
                    //20210315, rezakahfi, BONDRETAIL-703, begin
                    //myFormSourceFund.dcAmountMaximal = getAmountSourceOfFund(dgvSourceFund["DealIdSource", e.RowIndex].Value.ToString());
                    //20230213, rezakahfi, BONDRETAIL-1193, begin
                    //decimal dcAmountMaximalTemp = getAmountSourceOfFund(dgvSourceFund["DealIdSource", e.RowIndex].Value.ToString())
                    //                            + decimal.Parse(dgvSourceFund["Amount", e.RowIndex].Value.ToString());
                    //myFormSourceFund.dcAmountMaximal = dcAmountMaximalTemp;
                    DataRow[] DrSource = dtSource.Select("DealId = " + dgvSourceFund["DealIdSource", e.RowIndex].Value.ToString());
                    if (DrSource.Length > 0)
                    {
                        myFormSourceFund.dcAmountMaximal = decimal.Parse(DrSource[0]["Balance"].ToString());
                        myFormSourceFund.dcBalanceSource = myFormSourceFund.dcAmountMaximal;
                    }
                    //20230213, rezakahfi, BONDRETAIL-1193, end
                    //20210315, rezakahfi, BONDRETAIL-703, end
                    myFormSourceFund.dcBalanceSource = myFormSourceFund.dcAmountMaximal;
                }
                else
                    myFormSourceFund.dtAccount = getAccountSourceFund();

                myFormSourceFund.strSourceData = dgvSourceFund["SourceData", e.RowIndex].Value.ToString();
                myFormSourceFund.strCurrency = dgvSourceFund["CCY", e.RowIndex].Value.ToString();
                myFormSourceFund.strDealNo = dgvSourceFund["DealIdSource", e.RowIndex].Value.ToString();
                myFormSourceFund.strAccount = dgvSourceFund["Account", e.RowIndex].Value.ToString();
                myFormSourceFund.strValueDateReturn = dgvSourceFund["ValueDate", e.RowIndex].Value.ToString();
                myFormSourceFund.strValue_DateReturn = dgvSourceFund["Value_Date", e.RowIndex].Value.ToString();
                myFormSourceFund.dcAmount = decimal.Parse(dgvSourceFund["Amount", e.RowIndex].Value.ToString());
            }

            if (myFormSourceFund.strSourceData != "")
            {

                myFormSourceFund.dcNeededAmount = getAmountNeededFromSource();
                myFormSourceFund.dcValueDateMax = nispSettlementDate.Value;
                VisiblePanelSF(false);
                myFormSourceFund.TopLevel = false;
                myFormSourceFund.AutoScroll = true;
                myFormSourceFund.Dock = DockStyle.Fill;
                myFormSourceFund.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                pnlSourceOfFund.Controls.Add(myFormSourceFund);
                myFormSourceFund.Show();
            }
        }

        private void dgvSourceFund_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            decimal dcAmountTotal = 0;

            foreach (DataRow drRow in dtSourceOfFund.Rows)
            {
                if (drRow.RowState.ToString() != "Deleted")
                {
                    dcAmountTotal += decimal.Parse(drRow["Amount"].ToString());
                }
            }

            Double dblText = 0;
            Double.TryParse(dcAmountTotal.ToString(), out dblText);

            txtTotalAmountSource.Text = dblText.ToString("N");
        }

        public bool isCompleteToSourceOfFund()
        {

            if (txtTotalProceed.Text == "0" || txtTotalProceed.Text == "" || cbRekeningRelasi.Text == "" || nispSettlementDate.Text == ""
                    || Currency == ""
                    || CIFNo == "")
            {
                string strListKolom = "";

                if (txtTotalProceed.Text == "0" || txtTotalProceed.Text == "")
                    strListKolom = strListKolom + "\n ; " + "Total Proceed";
                if (Currency == "")
                    strListKolom = strListKolom + "\n ; " + "Currency";
                if (nispSettlementDate.Text == "")
                    strListKolom = strListKolom + "\n ; " + "Settlement Date";
                if (cbRekeningRelasi.Text == "")
                    strListKolom = strListKolom + "\n ; " + "Rekening Relasi";
                if (CIFNo == "")
                    strListKolom = strListKolom + "\n ; " + "No CIF";

                MessageBox.Show("Kolom  : " + strListKolom + "\nbelum terisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return false;
            }
            else
            {
                return true;
            }
        }

        private void btnAddSourceOfFund_Click(object sender, EventArgs e)
        {
            if (SourceFund == "")
                return;

            if (
                    (this.dtSourceOfFund.Select("SourceData = 'SavingAccount' or SourceData = 'Rekening'").Length > 0)
                        && (SourceFund.ToUpper() == "SAVINGACCOUNT" || SourceFund.ToUpper() == "REKENING")
                )
            {
                MessageBox.Show("Hanya boleh menggunakan satu sumber dana\nyang berasal dari rekening"
                                , "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning
                        );
                return;
            }

            if (decimal.Parse(txtTotalAmountSource.Text) <= 0)
            {
                if (!isCompleteToSourceOfFund())
                    return;

                if (
                MessageBox.Show("Sumber Dana yang akan digunakan terkait Transaksi Obligasi dengan Rincian :\n"
                                + "\n"
                                + "Nama Nasabah : " + txtNamaNasabah.Text + "\n"
                    //20230215, samypasha, BONDRETAIL-1241, begin
                                //+ "Produk :" + cmpsrNomorSekuriti.Text2 + "\n"
                                + "Produk :" + cmpsrNomorSekuriti._Text2.Text.ToString() + "\n"
                    //20230215, samypasha, BONDRETAIL-1241, end
                                + "Currency : " + Currency + "\n"
                                + "Debit Account : " + cbRekeningRelasi.Text + "\n"
                                + "Settlement Date : " + nispSettlementDate.Text + "\n"    
                                + "\n"
                                + "Apakah sumber dana akan ditambahkan untuk transaksi tersebut ?"
                                , "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question
                        ) == DialogResult.Yes
                )
                {
                    LockSourceOfFund(false);
                    //20221220, yazri, VSYARIAH-340, begin
                    cbRekeningRelasi.Enabled = false;
                    //20221220, yazri, VSYARIAH-340, end
                }
                else
                    return;
            }

            decimal nSisaPenempatan = txtTotalProceed.Value;
            myFormSourceFund = new dlgFMCTSourceFund();
            myFormSourceFund.strProcess = "Add";
            myFormSourceFund.strProduct = "Obligasi";

            if (SourceFund.ToUpper() == "SAVINGACCOUNT" || SourceFund.ToUpper() == "REKENING")
            {
                myFormSourceFund.strSourceData = this.SourceFund;
                myFormSourceFund.strCurrency = Currency;
                myFormSourceFund.strDealNo = "-";
                myFormSourceFund.strAccount = cbRekeningRelasi.Text;
                myFormSourceFund.strValue_Date = "-";
                myFormSourceFund.strValueDateReturn = "-";
                //20230213, rezakahfi, BONDRETAIL-1193, begin
                //myFormSourceFund.dcBalanceSource = this.dcSaldoRekening;
                //nSisaPenempatan = txtTotalProceed.Value - this.TotalAmountSource;
                //myFormSourceFund.dcAmount = (nSisaPenempatan < this.dcSaldoRekening ? nSisaPenempatan : this.dcSaldoRekening);
                nSisaPenempatan = txtTotalProceed.Value - this.TotalAmountSource;
                decimal dcTotalBalance = this.dcSaldoRekening;
                if (this.strFlag == "Update")
                {
                    DataSet dtSource = new DataSet();
                    this.PopulateSourceOfFundByDeal(cmpsrGetPushBack._Text1.Text.Trim(), out dtSource);
                    if (dtSource.Tables.Count > 0)
                    {
                        if (dtSource.Tables[0].Rows.Count > 0)
                        {
                            DataRow[] drCekRekening = dtSource.Tables[0].Select("SourceData = 'Rekening'");
                            
                            if (drCekRekening.Length > 0)
                            {
                                dcTotalBalance = dcTotalBalance + decimal.Parse(drCekRekening[0]["Amount"].ToString());
                            }
                        }
                    }
                }
                myFormSourceFund.dcAmount = (nSisaPenempatan < dcTotalBalance ? nSisaPenempatan : dcTotalBalance);
                myFormSourceFund.dcBalanceSource = dcTotalBalance;
                //20230213, rezakahfi, BONDRETAIL-1193, end
                myFormSourceFund.dcAmountMaximal = this.dcSaldoRekening;
                cbRekeningRelasi.Enabled = false;
            }
            else
            {
                DataTable dtSource = new DataTable();

                if (!isManual(out dtSource))
                {
                    dlgTRSSourceOfFund dlgSource = new dlgTRSSourceOfFund(dtSource);
                    dlgSource.cQuery = this.cQuery;
                    dlgSource.userNIK = this.intNIK;
                    dlgSource.Branch = this.strBranchCode;

                    dlgSource.ShowDialog();

                    if (dlgSource.isSetSource)
                    {
                        if (
                                (this.dtSourceOfFund.Select("SourceData = '" + this.SourceFund + "' and DealIdSource = " + dlgSource.strDealNoReturn).Length > 0)
                            )
                        {
                            MessageBox.Show("sumber dana sudah ditambahkan sebelumnya " + this.SourceFund + "(" + dlgSource.strDealNoReturn + ")"
                                            , "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning
                                    );
                            return;
                        }

                        myFormSourceFund.strSourceData = this.SourceFund;
                        myFormSourceFund.strCurrency = dlgSource.strCurrencyReturn;
                        myFormSourceFund.strDealNo = dlgSource.strDealNoReturn;
                        myFormSourceFund.strAccount = dlgSource.strAccountReturn;
                        myFormSourceFund.strValue_Date = dlgSource.strValueDateReturn;
                        myFormSourceFund.strValue_Date = dlgSource.strValue_DateReturn;
                        myFormSourceFund.dcBalanceSource = dlgSource.dcAmountReturn;
                        nSisaPenempatan = txtTotalProceed.Value - this.TotalAmountSource;
                        myFormSourceFund.dcAmount = (nSisaPenempatan < dlgSource.dcAmountReturn ? nSisaPenempatan : dlgSource.dcAmountReturn);
                        myFormSourceFund.dcAmountMaximal = dlgSource.dcAmountReturn;
                    }
                }
                else
                {
                    myFormSourceFund.strSourceData = this.SourceFund;
                    myFormSourceFund.strCurrency = Currency;
                    myFormSourceFund.dtAccount = getAccountSourceFund();
                    myFormSourceFund.dcAmountMaximal = txtTotalProceed.Value;
                }
            }

            if (myFormSourceFund.strSourceData != "")
            {

                myFormSourceFund.dcValueDateMax = nispSettlementDate.Value;
                myFormSourceFund.dcNeededAmount = getAmountNeededFromSource();
                VisiblePanelSF(false);
                myFormSourceFund.TopLevel = false;
                myFormSourceFund.AutoScroll = true;
                myFormSourceFund.Dock = DockStyle.Fill;
                myFormSourceFund.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                pnlSourceOfFund.Controls.Add(myFormSourceFund);
                myFormSourceFund.Show();
            }
        }
        //20210315, rezakahfi, BONDRETAIL-703, begin
        public string getXMLSumberDana()
        {
            string xmlSumberDana = null;
            if (chkOther.Checked)
            {

                if (txtTotalProceed.Value < decimal.Parse(txtTotalAmountSource.Text))
                {
                    MessageBox.Show("Total Proceed lebih kecil dari jumlah sumber dana.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return "";
                }

                if (decimal.Parse(txtTotalAmountSource.Text) < txtTotalProceed.Value)
                {
                    MessageBox.Show("Dana dari sumber dana masih kurang, silakan tambah sumber dana.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return "";
                }

                if (dgvSourceFund.Rows.Count > 0)
                {
                    DataTable dtToXML = new DataTable();
                    dtToXML.TableName = "Source";
                    foreach (DataGridViewColumn col in dgvSourceFund.Columns)
                    {
                        dtToXML.Columns.Add(col.Name);
                    }
                    foreach (DataGridViewRow row in dgvSourceFund.Rows)
                    {
                        DataRow dRow = dtToXML.NewRow();
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            dRow[cell.ColumnIndex] = cell.Value;
                        }
                        dtToXML.Rows.Add(dRow);
                    }

                    DataSet dstSumberDana = new DataSet();
                    dstSumberDana.Tables.Add(dtToXML.Copy());

                    dstSumberDana.DataSetName = "Data";
                    dstSumberDana.Tables[0].TableName = "Source";
                    StringBuilder dataSave = new StringBuilder();

                    dstSumberDana.Tables[0].WriteXml(System.Xml.XmlWriter.Create(dataSave));
                    xmlSumberDana = dataSave.ToString();
                }
                else
                {
                    MessageBox.Show("Sumber dana harus dipilih, jika Other Check Box tercentang", "Warning", MessageBoxButtons.OK);
                    return "";
                }
            }

            return xmlSumberDana;
        }
        //20210315, rezakahfi, BONDRETAIL-703, end

        public bool isManual(out DataTable dtSource)
        {
            //20230213, rezakahfi, BONDRETAIL-1193, begin
            //dtSource = getDataSourceFund(cmbSourceFund.SelectedValue.ToString().Trim(), CIFNo, nispSettlementDate.Value.ToString(), Currency, cbRekeningRelasi.Text);
            long DealId = 0;
            if (cmpsrGetPushBack._Text1.Text.Trim() != "" && strFlag == "Update")
                DealId = long.Parse(cmpsrGetPushBack._Text1.Text.Trim());
            
            dtSource = getDataSourceFund(cmbSourceFund.SelectedValue.ToString().Trim()
                            , CIFNo, nispSettlementDate.Value.ToString()
                            , Currency, cbRekeningRelasi.Text
                
                            , DealId
                
                            );
            //20230213, rezakahfi, BONDRETAIL-1193, end

            return dtSource.Columns.Contains("MANUAL");
        }

        public decimal getAmountSourceOfFund(string srDealId)
        {
            //20210315, rezakahfi, BONDRETAIL-703, begin
            //DataTable dtSource = getDataSourceFund("amountsource", CIFNo, nispSettlementDate.Value.ToString(), Currency, srDealId);
            //return decimal.Parse(dtSource.Rows[0]["Amount"].ToString());
            DataTable dtSource = getDataSourceFund("balancesource", CIFNo, nispSettlementDate.Value.ToString(), Currency, srDealId,SourceFund);
            //20210315, rezakahfi, BONDRETAIL-703, end
            return decimal.Parse(dtSource.Rows[0]["Balance"].ToString());
        }

        public DataTable getAccountSourceFund()
        {
            DataTable dtReturn = new DataTable();
            DataSet dsOut = new DataSet();

            try
            {
                if (PopulateDataNasabah("AccountTrxProductObli", "Obligasi", CIFNo, "", Currency, chkTaxAmnesty.Checked, out dsOut))
                {
                    dtReturn = dsOut.Tables[0].Copy();
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Gagal Populate Account Source of Fund");
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("getAccountSourceFund : " + ex.Message.ToString());
            }

            return dtReturn;
        }

        public DataTable getDataSourceFund(string Source, string cif, string valueDate, string currency, string Filter)
        {
            DataSet dsOut = new DataSet();
            DataTable dtTable = new DataTable();

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[5];
                dbParams[0] = new OleDbParameter("@pcSource", Source);
                dbParams[1] = new OleDbParameter("@pcCIF", cif);
                dbParams[2] = new OleDbParameter("@pcValueDate", valueDate);
                dbParams[3] = new OleDbParameter("@pcCcySource", currency);
                dbParams[4] = new OleDbParameter("@pcFilter", Filter);

                if (this.cQuery.ExecProc("dbo.TRSPopulateSourceOfFund", ref dbParams, out dsOut))
                {
                    if (dsOut.Tables.Count > 0)
                    {
                        dtTable = dsOut.Tables[0].Copy();
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return dtTable;
        }
        //20210315, rezakahfi, BONDRETAIL-703, begin
        public DataTable getDataSourceFund(string Source, string cif, string valueDate, string currency, string Filter, string Filter2)
        {
            DataSet dsOut = new DataSet();
            DataTable dtTable = new DataTable();

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[6];
                dbParams[0] = new OleDbParameter("@pcSource", Source);
                dbParams[1] = new OleDbParameter("@pcCIF", cif);
                dbParams[2] = new OleDbParameter("@pcValueDate", valueDate);
                dbParams[3] = new OleDbParameter("@pcCcySource", currency);
                dbParams[4] = new OleDbParameter("@pcFilter", Filter);
                dbParams[5] = new OleDbParameter("@pcFilter2", Filter2);

                if (this.cQuery.ExecProc("dbo.TRSPopulateSourceOfFund", ref dbParams, out dsOut))
                {
                    if (dsOut.Tables.Count > 0)
                    {
                        dtTable = dsOut.Tables[0].Copy();
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return dtTable;
        }
        //20210315, rezakahfi, BONDRETAIL-703, end
        //20210813, rezakahfi, BONDRETAIL-799, begin
        public bool CekHargaModal() 
        {
            bool bResult = true;

            DataSet dshargaORI;
            OleDbParameter[] dbParam = new OleDbParameter[4];

            //20230215, samypasha, BONDRETAIL-1241, begin
            //(dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti.Text1;
            (dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 50)).Value = cmpsrNomorSekuriti._Text1.Text.ToString();
            //20230215, samypasha, BONDRETAIL-1241, end
            (dbParam[1] = new OleDbParameter("@pcJenisTransaksi", OleDbType.VarChar, 30)).Value = cmbJenisTransaksi.Text;
            (dbParam[2] = new OleDbParameter("@pcParameter", OleDbType.VarChar, 50)).Value = "HargaOri";
            //20211012, rezakahfi, BONDRETAIL-814, begin
            (dbParam[3] = new OleDbParameter("@pnFaceValue", OleDbType.Decimal)).Value = moneyFaceValue.Value;
            //20211012, rezakahfi, BONDRETAIL-814, end

            bResult = cQuery.ExecProc("TRSPopulateDataTransaksiSuratBerharga", ref dbParam, out dshargaORI);

            if (bResult)
            {
                if (dshargaORI.Tables[1].Rows.Count > 0)
                {
                    if (ndHargaModal.Value != decimal.Parse(dshargaORI.Tables[1].Rows[0][0].ToString()))
                    {
                        MessageBox.Show("Terdapat Perbedaan Harga Modal\nMohon lakukan pengecekan DealPrice dan lakukan kalkulasi ulang"
                            , "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        isNeedRecalculate = true;

                        ndHargaModal.Value = decimal.Parse(dshargaORI.Tables[1].Rows[0][0].ToString());

                        if (dshargaORI.Tables[0].Rows.Count > 0)
                        {
                            //20231227, rezakahfi, BONDRETAIL-1513, begin
                            //ndHargaPublish.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());
                            //moneyDealPrice.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());

                            if (tcImbalHasil.TabPages.Contains(pgDiskonto))
                            {
                                decimal dcPrice = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());

                                if (cmbJenisTransaksi.Text.Contains("Sell"))
                                {
                                    dcPrice = Math.Ceiling(dcPrice * 100) / 100;
                                }
                                else
                                {
                                    dcPrice = Math.Floor(dcPrice * 100) / 100;
                                }

                                ndHargaPublish.Value = dcPrice;
                                moneyDealPrice.Value = dcPrice;
                            }
                            else
                            {
                                ndHargaPublish.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());
                                moneyDealPrice.Value = decimal.Parse(dshargaORI.Tables[0].Rows[0][0].ToString());
                            }
                            //20231227, rezakahfi, BONDRETAIL-1513, end
                        }

                        bResult = false;
                    }
                }

                if (dshargaORI.Tables.Count > 2)
                {
                    if (dshargaORI.Tables[2].Rows.Count > 0)
                    {
                        //20211012, rezakahfi, BONDRETAIL-814, begin
                        //MessageBox.Show("Terdapat Perubahan Harga Modal yang belum di-approved", "Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //ndHargaPublish.Value = 0;
                        //moneyDealPrice.Value = 0;
                        //ndHargaModal.Value = 0;
                        //controlsClear(true);
                        bool bStop = false;
                        for (int i = 0; i < dshargaORI.Tables[2].Rows.Count; i++)
                        {
                            if (bool.Parse(dshargaORI.Tables[2].Rows[i]["Stoper"].ToString())
                                && (dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "All"
                                    || dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "CB"
                                    )
                                )
                            {
                                MessageBox.Show(dshargaORI.Tables[2].Rows[i]["Description"].ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                bStop = true;
                            }
                            else if (!bool.Parse(dshargaORI.Tables[2].Rows[i]["Stoper"].ToString())
                                && (dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "All"
                                    || dshargaORI.Tables[2].Rows[i]["Type"].ToString() == "CB"
                                    ))
                            {
                                if (MessageBox.Show(dshargaORI.Tables[2].Rows[i]["Description"].ToString(), "Warnings", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                    bStop = true;
                            }
                        }

                        if (bStop)
                        {
                            ndHargaPublish.Value = 0;
                            moneyDealPrice.Value = 0;
                            ndHargaModal.Value = 0;

                            controlsClear(true);
                        }
                        //20211012, rezakahfi, BONDRETAIL-814, end
                    }
                }
            }
            else
            {
                MessageBox.Show("Error saat melakukan pengecekan harga modal","Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }

            return bResult;
        }
        //20210813, rezakahfi, BONDRETAIL-799, end
        //20230213, rezakahfi, BONDRETAIL-1193, begin
        public bool PopulateSourceOfFundByDeal(string DealId, out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;
            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[2];
                dbPar[0] = new OleDbParameter("@pcProduct", "Obligasi");
                dbPar[1] = new OleDbParameter("@pnDealId", DealId);

                bResult = this.cQuery.ExecProc("dbo.TRSPopulateSourceOfFundDeal", ref dbPar, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return bResult;
        }

        public DataTable getDataSourceFund(string Source, string cif, string valueDate, string currency, string Filter, long DealId)
        {
            DataSet dsOut = new DataSet();
            DataTable dtTable = new DataTable();

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[8];
                dbParams[0] = new OleDbParameter("@pcSource", Source);
                dbParams[1] = new OleDbParameter("@pcCIF", cif);
                dbParams[2] = new OleDbParameter("@pcValueDate", valueDate);
                dbParams[3] = new OleDbParameter("@pcCcySource", currency);
                dbParams[4] = new OleDbParameter("@pcFilter", Filter);
                dbParams[5] = new OleDbParameter("@pcFilter2", "");
                dbParams[6] = new OleDbParameter("@pnProduct", "Obligasi");
                dbParams[7] = new OleDbParameter("@pnDealIdProduct", DealId);

                if (this.cQuery.ExecProc("dbo.TRSPopulateSourceOfFund", ref dbParams, out dsOut))
                {
                    if (dsOut.Tables.Count > 0)
                    {
                        dtTable = dsOut.Tables[0].Copy();
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return dtTable;
        }
        //20230213, rezakahfi, BONDRETAIL-1193, end
        public bool PopulateSumberDanaSP(string cCIF, string cParam, string cDescription, string cCcy, string cTrxDate
            , string cValueDate, long nDealId, out DataSet dsOut)
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[7];
                oParam[0] = new OleDbParameter("@pcCIFNo", cCIF);
                oParam[1] = new OleDbParameter("@pcParam", cParam);
                oParam[2] = new OleDbParameter("@pcDesc", cDescription);
                oParam[3] = new OleDbParameter("@pcCcy", cCcy);
                oParam[4] = new OleDbParameter("@pcTrxDate", cTrxDate);
                oParam[5] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[6] = new OleDbParameter("@pnDealId", nDealId);

                blnResult = this.cQuery.ExecProc("DCRPopulateSumberDanaStructuredProduct", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool PopulateDataMature(string strParamType, int JenisData, string strFilter, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[3];
            dbParams[0] = new OleDbParameter("@pcParamType", strParamType);
            dbParams[1] = new OleDbParameter("@pnJnsData", JenisData);
            dbParams[2] = new OleDbParameter("@pcFilter", strFilter);

            return this.cQuery.ExecProc("dbo.FMCTPopulateDataMature", ref dbParams, out dsOut);
        }

        public DataTable getDataSourceFund(string Source, string cif, string valueDate, string currency)
        {
            DataSet dsOut = new DataSet();
            DataTable dtTable = new DataTable();

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[4];
                dbParams[0] = new OleDbParameter("@pcSource", Source);
                dbParams[1] = new OleDbParameter("@pcCIF", cif);
                dbParams[2] = new OleDbParameter("@pcValueDate", valueDate);
                dbParams[3] = new OleDbParameter("@pcCcySource", currency);

                if (this.cQuery.ExecProc("dbo.TRSPopulateSourceOfFund", ref dbParams, out dsOut))
                {
                    if (dsOut.Tables.Count > 0)
                    {
                        dtTable = dsOut.Tables[0].Copy();
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return dtTable;
        }

        public bool PopulateDataNasabah(string strParamType, string strProduct, string strCIF, string strCIF2, string strFilter, bool bStatus, out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[6];
                dbParams[0] = new OleDbParameter("@pcTypePopulate", strParamType);
                dbParams[1] = new OleDbParameter("@pcProduct", strProduct);
                dbParams[2] = new OleDbParameter("@pcCIF", strCIF);
                dbParams[3] = new OleDbParameter("@pcCIF2", strCIF2);
                dbParams[4] = new OleDbParameter("@pcFilter", strFilter);
                dbParams[5] = new OleDbParameter("@pbStatus", bStatus);

                bResult = this.cQuery.ExecProc("dbo.TRSPopulateDataNasabahProduct", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return bResult;
            }

            return bResult;

        }
		
		//20230109, tobias, BONDRETAIL-1162, begin
        public bool DeleteTransactionSekunder(ObligasiQuery cQuery
            , string DealId, string PoDealId, string Instruction
            )
        {
            bool isOK = false;
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[3];
            odpParam[0] = new System.Data.OleDb.OleDbParameter("@pnDealId", DealId);
            odpParam[1] = new System.Data.OleDb.OleDbParameter("@pnPODealId", PoDealId);
            odpParam[2] = new System.Data.OleDb.OleDbParameter("@pcInstruction", Instruction);

            isOK = (cQuery.ExecProc("dbo.OMDeleteProcessInputSecurityMaster", ref odpParam));

            return isOK;
        }
        //20230109, tobias, BONDRETAIL-1162, end

        public decimal getAmountNeededFromSource()
        {
            decimal dcSelisih = (txtTotalProceed.Value - decimal.Parse(txtTotalAmountSource.Text));

            return dcSelisih < 0 ? 0 : dcSelisih;
        }


        public decimal dcSaldoRekening
        {
            get
            {
                //20200604, uzia, BONDRETAIL-438, begin
                //return decimal.Parse(txtSaldoRekening.Text.Trim());
                return txtSaldoRekening.Value;
                //20200604, uzia, BONDRETAIL-438, end
            }
            set
            {   
                //20200604, uzia, BONDRETAIL-438, begin
                //txtSaldoRekening.Text = value.ToString();

                //Double dblText = 0;
                //Double.TryParse(txtSaldoRekening.Text, out dblText);
                //txtSaldoRekening.Text = dblText.ToString("N");
                decimal dcEffectiveBalance = 0;
                string strEffectiveBalance = value.ToString("N2");                
                decimal.TryParse(strEffectiveBalance, out dcEffectiveBalance);
                dcEffectiveBalance = decimal.Round(dcEffectiveBalance, 2);

                txtSaldoRekening.Value = dcEffectiveBalance;
                //20200604, uzia, BONDRETAIL-438, end
            }
        }

        public DataTable dtSourceOfFund
        {
            get
            {
                return (DataTable)dgvSourceFund.DataSource;
            }

            set
            {
                dgvSourceFund.DataSource = value;

                if (value != null)
                {
                    if (value.Columns.Contains("ValueDate"))
                        dgvSourceFund.Columns["ValueDate"].Visible = false;
                    if (value.Columns.Contains("isManual"))
                        dgvSourceFund.Columns["isManual"].Visible = false;
                    if (value.Columns.Contains("DealIdSource"))
                        dgvSourceFund.Columns["DealIdSource"].HeaderText = "DealId";
                    if (value.Columns.Contains("ValueDate"))
                        dgvSourceFund.Columns["ValueDate"].HeaderText = "MaturityDate";
                }
            }
        }

        public string SourceFund
        {
            get
            {
                try
                {
                    return cmbSourceFund.SelectedValue.ToString().Trim();
                }
                catch
                {
                    return "";
                }
            }

            set
            {
                cmbSourceFund.SelectedValue = value;
            }

        }

        public decimal TotalAmountSource
        {
            get
            {
                try
                {
                    return decimal.Parse(txtTotalAmountSource.Text.Trim());
                }
                catch
                {
                    return 0;
                }

            }
            set
            {
                Double dblText = 0;
                Double.TryParse(value.ToString(), out dblText);
                txtTotalAmountSource.Text = dblText.ToString("N");
            }
        }

        private void cmpsrSearch1_Load(object sender, EventArgs e)
        {

        }
        //20190116, samypasha, BOSOD18243, end
        
        //20200417, uzia, BONDRETAIL-257, begin
        private bool ValidateTA()
        {
            bool isValid = false;
            string strErrMsg = "";

            if (!this._paramTAModel.ValidateTAParam("BOND", out isValid, out strErrMsg))
            {
                MessageBox.Show("Fail to validate TA !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!isValid)
                MessageBox.Show(strErrMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return isValid;
        }
        //20200417, uzia, BONDRETAIL-257, end
        //20220221, darul.wahid, BONDRETAIL-892, begin
        private bool ValidateSumberDana(TransaksiSuratBerharga trxSuratBerharga)
        {
            bool bValid = false;

            if (cmbJenisTransaksi.Text == "Buy")
                bValid = true;
            else
            {
                string strSumberDana = "",
                    strErrMsg = "";

                strSumberDana = getXMLSumberDana();
                if (!trxSuratBerharga.ValidateSumberDana(strSumberDana, out bValid, out strErrMsg))
                {
                    MessageBox.Show("Fail to validate Sumber Dana !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (!bValid)
                    MessageBox.Show(strErrMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);                
            }
            return bValid;
        }

        private void cmbSourceFund_DropDown(object sender, EventArgs e)
        {
            this.cmbSourceFund.DataSource = getSourceFund();
            this.cmbSourceFund.ValueMember = "Value";
            this.cmbSourceFund.DisplayMember = "Description";
            this.cmbSourceFund.SelectedIndex = 0;
        }
        //20220221, darul.wahid, BONDRETAIL-892, end
    }
    //20160831, fauzil,LOGEN196, begin
    public class TransaksiBankBeli
    {
        private decimal nFaceValue;
        private decimal nAccruedDays;
        private decimal nInterest;
        private decimal nProceed;
        private decimal nTaxOnAccrued;
        private decimal nTaxOnCapitalGL;
        private decimal nSafeKeepingFeeAmount;
        private decimal nTotalProceed;
        private decimal nTransactionFee;
        private string cNoRekInvestor;
        private bool bIsTaxAmnesty;

        //20220208, darul.wahid, BONDRETAIL-895, begin
        private decimal _dCapitalGain;

        public decimal CapitalGain
        {
            get
            {
                return _dCapitalGain;
            }
            set
            {
                _dCapitalGain = value;
            }
        }
        //20220208, darul.wahid, BONDRETAIL-895, end
        //20220708, darul.wahid, BONDRETAIL-977, begin
        private decimal _dCapitalGainNonIDR;
        private decimal _dTotalTax;
        private decimal _dIncome;

        public decimal CapitalGainNonIdr
        {
            get
            {
                return _dCapitalGainNonIDR;
            }
            set
            {
                _dCapitalGainNonIDR = value;
            }
        }

        public decimal TotalTax
        {
            get
            {
                return _dTotalTax;
            }
            set
            {
                _dTotalTax = value;
            }
        }

        public decimal Income
        {
            get
            {
                return _dIncome;
            }
            set
            {
                _dIncome = value;
            }
        }
        //20220708, darul.wahid, BONDRETAIL-977, end
        //20220708, yudha.n, BONDRETAIL-1052, begin
        private decimal _dMateraiCost;

        public decimal MateraiCost
        {
            get
            {
                return _dMateraiCost;
            }
            set
            {
                _dMateraiCost = value;
            }
        }
        //20220708, yudha.n, BONDRETAIL-1052, end
        
        //20240422, alfian.andhika, BONDRETAIL-1581, begin
        private decimal _dYieldHargaModal;

        public decimal YieldHargaModal
        {
            get { return _dYieldHargaModal; }
            set { _dYieldHargaModal = value; }
        }
        //20240422, alfian.andhika, BONDRETAIL-1581, end

        public decimal FaceValue
        {
            get { return nFaceValue; }
            set { nFaceValue = value; }
        }

        public decimal AccruedDays
        {
            get { return nAccruedDays; }
            set { nAccruedDays = value; }
        }

        public decimal Interest
        {
            get { return nInterest; }
            set { nInterest = value; }
        }

        public decimal Proceed
        {
            get { return nProceed; }
            set { nProceed = value; }
        }

        public decimal TaxOnAccrued
        {
            get { return nTaxOnAccrued; }
            set { nTaxOnAccrued = value; }
        }

        public decimal TaxOnCapitalGL
        {
            get { return nTaxOnCapitalGL; }
            set { nTaxOnCapitalGL = value; }
        }

        public decimal SafeKeepingFeeAmount
        {
            get { return nSafeKeepingFeeAmount; }
            set { nSafeKeepingFeeAmount = value; }
        }

        public decimal TotalProceed
        {
            get { return nTotalProceed; }
            set { nTotalProceed = value; }
        }
        public decimal TransactionFee
        {
            get { return nTransactionFee; }
            set { nTransactionFee = value; }
        }
        public string NoRekInvestor
        {
            get { return cNoRekInvestor; }
            set { cNoRekInvestor = value; }
        }
        public bool IsTaxAmnesty
        {
            get { return bIsTaxAmnesty; }
            set { bIsTaxAmnesty = value; }
        }

    }
    //20160831, fauzil,LOGEN196, end

   
}