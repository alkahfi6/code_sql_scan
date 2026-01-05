using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//20200408, Lita, RDN-88, begin
using System.Collections;
//20200408, Lita, RDN-88, end
//20230222, Filian, RDN-903, begin
using BankNISP.FrontEnd;
using ProReksa2.RepositoryAPI.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
//20230222, Filian, RDN-903, end

namespace BankNISP.FrontEnd
{
    public partial class frmTransactionNew : BankNISP.Template.StandardForm
    {/*    
       ==================================================================================================
       Created By      : Andhika J
       Created Date    : 20230206
       Description     : Migrasi MW lama to API 
       Edited          :
       ==================================================================================================
       Date        Editor              Project ID          Description
       ==================================================================================================
       20230206    Andhika J           RDN-903             Migrasi SP ReksaRejectBooking to service API
       20230315    Andhika J           RDN-903             Migrasi SP ReksaMaintainBooking to service API
       20230222    Filian              RDN-903             Migrasi SP ReksaMaintainNewBooking to service API
       20231121    Ahmad.fansyuri      HTR-189             Validasi Checkbox 'FeeEdit' submenu Subscription -> hanya percentage
	   20231227    gio				   RDN-1108			   Penambahan Message Jam Instruksi Transaksi	
       ==================================================================================================
    */
        internal NispLogin.ClsUser clUserInside;
        internal NispQuery.ClsQuery ClQ;
        private string[] _strDefToolBar;
        private string _strTabName;
        private string _strLastTabName;
        private DataView _dvAkses;

        public string strModule;
        public int intNIK;
        public string strGuid;
        public string strMenuName;
        public string strBranch;
        public int intClassificationId;

        int _intType;
        private bool IsSubsNew;
        private bool IsRedempAll;
        private bool IsSwitchingAll;
        private int intPeriod;
        private System.Double Fee = 0;
        private System.Double PercentFee = 0;
        private bool ByPercent;
        string strFeeCurr;
        private clsTransaksi cTransaksi;

        private DataView dvSubscription;
        private DataView dvRedemption;
        private DataView dvSubsRDB;
        private DateTime globalJatuhTempoRDB;
        private DateTime globalJatuhTempoSwcRDB;

        private bool IsClickByDatagrid;
        private decimal _NAVSwcInNonRDB;
        private decimal _NAVSwcOutNonRDB;
        private decimal OutstandingUnitSwcIn;
        private frmDocument objFormDocument;
        //20150629, liliana, LIBST13020, begin
        private string _StatusTransaksiSubs;
        private string _StatusTransaksiRDB;
        private string _StatusTransaksiRedemp;
        //20150629, liliana, LIBST13020, end
        //20150820, liliana, LIBST13020, begin
        private bool IsSwitchingRDBSebagian;
        //20150820, liliana, LIBST13020, end
        //20160816, Elva, LOGEN00191, begin
        private bool _isCheckingTASubs = true, _isCheckingTARedemp = true, _isCheckingTARDB = true, _isCheckingTASwcRDB = true, _isCheckingTASwcNonRDB = true, _isCheckingTABook = true;
        //20160816, Elva, LOGEN00191, end
        //20170825, liliana, COPOD17271, begin
        private clsCoreBankMessaging _clsCoreBankMessaging = null;
        private DataSet dsOut;
        private string ErrMsg;
        //20170825, liliana, COPOD17271, end

        //20200408, Lita, RDN-88, begin
        private DataTable _dtDataFreqDebetRDB = null;
        private string _strFreqDebetMethod;
        //20200408, Lita, RDN-88, end

        //20230222, Filian, RDN-903, begin
        private clsProc _cProc = new clsProc();
        private IServicesAPI _iServiceAPI;
        private ReksaMaintainNewBookingRq _ReksaMaintainNewBookingRq;
        private ReksaMaintainAllTransaksiNewRq _ReksaMaintainAllTransaksiNewRq;
        private ReksaMaintainSwitchingRq _ReksaMaintainSwitchingRq;
        private ReksaMaintainSwitchingRDBRq _ReksaMaintainSwitchingRDBRq;
        //20230222, Filian, RDN-903, end
        //20240911, Lely, RDN-1182, begin
        private int _iMode = 0;
        //20240911, Lely, RDN-1182, end

        public frmTransactionNew()
        {
            InitializeComponent();
            //20230223, Filian, RDN-903, begin
            this._iServiceAPI = clsStaticClass.APIService;
            //20230223, Filian, RDN-903, end
        }

        private void PopulateComboBox()
        {
            DataSet _ds;

            bool blnResult = ClQ.ExecProc("dbo.ReksaPopulateCombo", out _ds);
            if (blnResult)
            {
                if (_ds.Tables[0] != null)
                {
                    if (_ds.Tables[0].Rows.Count > 0)
                    {
                        //20200408, Lita, RDN-88, begin
                        //cmbFrekPendebetanRDB.DataSource = _ds.Tables[0];
                        //cmbFrekPendebetanRDB.DisplayMember = "FrekuensiPendebetan";
                        //20200408, Lita, RDN-88, end

                        cmbFrekPendebetanSwcRDB.DataSource = _ds.Tables[0];
                        cmbFrekPendebetanSwcRDB.DisplayMember = "FrekuensiPendebetan";
                    }
                }
            }

            //20200408, Lita, RDN-88, begin



            //combobox freq debet
            _ds = new DataSet();
            this._dtDataFreqDebetRDB = null;

            //20220322, Gio, RDN-757, begin
            //if (PopulateParamComboRDB("FREQDEBETMETHOD", "", out _ds))
            if (PopulateParamComboRDB("FREQDEBET", "", "", out _ds))
            //20220322, Gio, RDN-757, end
            {
                if (_ds.Tables[0] != null)
                {
                    if (_ds.Tables[0].Rows.Count > 0)
                    {
                        this._dtDataFreqDebetRDB = new DataTable();
                        this._dtDataFreqDebetRDB = _ds.Tables[0];
                    }

                }
            }

            _ds = new DataSet();
            //20220322, Gio, RDN-757, begin
            if (PopulateParamComboRDB("FREQDEBETMETHOD", "", "", out _ds))
            //if (PopulateParamComboRDB("FREQDEBETMETHOD", "", out _ds))
            //20220322, Gio, RDN-757, end
            {
                if (_ds.Tables[0] != null)
                {
                    if (_ds.Tables[0].Rows.Count > 0)
                    {
                        cmbFrekDebetMethodRDB.DataSource = _ds.Tables[0];
                        cmbFrekDebetMethodRDB.DisplayMember = "ComboText";
                        cmbFrekDebetMethodRDB.ValueMember = "ComboValue";
                        cmbFrekDebetMethodRDB.SelectedValue = "M";
                    }

                }
            }


            //20200408, Lita, RDN-88, end

        }

        //20200408, Lita, RDN-88, begin
        //20220322, Gio, RDN-757, begin
        //private bool PopulateParamComboRDB(string sParamType, string sParamCriteria, out DataSet dsDataCombo)
        private bool PopulateParamComboRDB(string sParamType, string sProdCode, string sParamCriteria, out DataSet dsDataCombo)
        //20220322, Gio, RDN-757, end
        {
            dsDataCombo = new DataSet();
            bool blnResult = true;
            //20220322, Gio, RDN-757, begin
            //OleDbParameter[] dbComboFreqDebet = new OleDbParameter[1];
            OleDbParameter[] dbComboFreqDebet = new OleDbParameter[2];
            //20220322, Gio, RDN-757, end

            (dbComboFreqDebet[0] = new OleDbParameter("@pcParamType", OleDbType.VarChar)).Value = sParamType;
            //20220322, Gio, RDN-757, begin
            (dbComboFreqDebet[1] = new OleDbParameter("@pcProdCode", OleDbType.VarChar)).Value = sProdCode;
            //20220322, Gio, RDN-757, end

            blnResult = ClQ.ExecProc("dbo.ReksaPopulateParamRDB", ref dbComboFreqDebet, out dsDataCombo);

            return blnResult;
            //if (blnResult)
            //{
            //    if (dsDataCombo.Tables[0] != null)
            //    {
            //        if (dsDataCombo.Tables[0].Rows.Count > 0)
            //        {
            //            cmbFrekPendebetanRDB.DataSource = dsDataCombo.Tables[0];
            //            cmbFrekPendebetanRDB.DisplayMember = "FrekuensiPendebetan";

            //            cmbFrekPendebetanSwcRDB.DataSource = dsDataCombo.Tables[0];
            //            cmbFrekPendebetanSwcRDB.DisplayMember = "FrekuensiPendebetan";
            //        }
            //    }
            //}
            //20200408, Lita, RDN-88, end

        }
        //20200408, Lita, RDN-88, end

        //20220329, gio, RDN-757, begin
        private bool PopulateParamComboRDBFrekDebet(string sProdCode, string sFrekMethod, out DataSet dsDataCombo)
        {
            dsDataCombo = new DataSet();
            bool blnResult = true;
            OleDbParameter[] dbComboFreqDebet = new OleDbParameter[2];


            (dbComboFreqDebet[0] = new OleDbParameter("@pcProdCode", OleDbType.VarChar)).Value = sProdCode;
            (dbComboFreqDebet[1] = new OleDbParameter("@pcFreqDebetMethod", OleDbType.VarChar)).Value = sFrekMethod;

            blnResult = ClQ.ExecProc("dbo.ReksaPopulateParamRDB_FreqDebet", ref dbComboFreqDebet, out dsDataCombo);

            return blnResult;

        }
        //20220329, gio, RDN-757, end

        private void subSetVisibleGrid(string _strTabName)
        {
            if (_strTabName == "SUBS")
            {
                //20150617, liliana, LIBST13020, begin
                dataGridViewSubs.Columns["TglTrx"].Visible = false;
                //20150617, liliana, LIBST13020, end
                dataGridViewSubs.Columns["FeeCurr"].Visible = false;
                dataGridViewSubs.Columns["FeeKet"].Visible = false;
                dataGridViewSubs.Columns["CCY"].Visible = false;
                //20150827, liliana, LIBST13020, begin
                //dataGridViewSubs.Columns["PctFee"].Visible = false;
                //20150827, liliana, LIBST13020, end
                dataGridViewSubs.Columns["EditFee"].Visible = false;
                dataGridViewSubs.Columns["IsNew"].Visible = false;
                dataGridViewSubs.Columns["OutstandingUnit"].Visible = false;
                dataGridViewSubs.Columns["JenisFee"].Visible = false;
                dataGridViewSubs.Columns["ApaDiUpdate"].Visible = false;
            }
            else if (_strTabName == "REDEMP")
            {
                //20150617, liliana, LIBST13020, begin
                dataGridViewRedemp.Columns["TglTrx"].Visible = false;
                //20150617, liliana, LIBST13020, end
                dataGridViewRedemp.Columns["FeeCurr"].Visible = false;
                dataGridViewRedemp.Columns["FeeKet"].Visible = false;
                //20150827, liliana, LIBST13020, begin
                //dataGridViewRedemp.Columns["PctFee"].Visible = false;
                //20150827, liliana, LIBST13020, end
                dataGridViewRedemp.Columns["EditFee"].Visible = false;
                //20150617, liliana, LIBST13020, begin
                //dataGridViewRedemp.Columns["IsRedempAll"].Visible = false;
                //20150617, liliana, LIBST13020, end
                dataGridViewRedemp.Columns["Period"].Visible = false;
                dataGridViewRedemp.Columns["JenisFee"].Visible = false;
                dataGridViewRedemp.Columns["ApaDiUpdate"].Visible = false;

            }
            else if (_strTabName == "SUBSRDB")
            {
                //20150617, liliana, LIBST13020, begin
                dataGridViewRDB.Columns["TglTrx"].Visible = false;
                //20150617, liliana, LIBST13020, end
                dataGridViewRDB.Columns["CCY"].Visible = false;
                dataGridViewRDB.Columns["FeeCurr"].Visible = false;
                dataGridViewRDB.Columns["FeeKet"].Visible = false;
                //20150827, liliana, LIBST13020, begin
                //dataGridViewRDB.Columns["PctFee"].Visible = false;
                //20150827, liliana, LIBST13020, end
                dataGridViewRDB.Columns["EditFee"].Visible = false;
                dataGridViewRDB.Columns["JenisFee"].Visible = false;
                dataGridViewRDB.Columns["ApaDiUpdate"].Visible = false;

                //20200408, Lita, RDN-88, begin
                dataGridViewRDB.Columns["FrekDebetMethodValue"].Visible = false;
                //20200408, Lita, RDN-88, end
            }
        }

        public string[] DefToolBar
        {
            get { return _strDefToolBar; }
            set { _strDefToolBar = value; }
        }

        private void subResetToolBar()
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

            //20151005, liliana, LIBST13020, begin
            if (_strTabName == "SWCRDB")
            {
                switch (_intType)
                {
                    case 0:
                        {
                            this.NISPToolbarButton("2").Enabled = true;
                            this.NISPToolbarButton("3").Enabled = true;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = false;
                            this.NISPToolbarButton("7").Enabled = false;
                            break;
                        }
                    case 1:
                        {
                            this.NISPToolbarButton("2").Enabled = false;
                            this.NISPToolbarButton("3").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = true;
                            this.NISPToolbarButton("7").Enabled = true;
                            break;
                        }
                    case 2:
                        {
                            this.NISPToolbarButton("2").Enabled = false;
                            this.NISPToolbarButton("3").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = true;
                            this.NISPToolbarButton("7").Enabled = true;
                            break;
                        }
                }
            }
            //20151107, liliana, LIBST13020, begin
            else if (_strTabName == "BOOK")
            {
                switch (_intType)
                {
                    case 0:
                        {
                            this.NISPToolbarButton("2").Enabled = true;
                            this.NISPToolbarButton("3").Enabled = true;
                            //20221208, Andi, HFUNDING-178, Begin
                            //this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = true;
                            //20221208, Andi, HFUNDING-178, End
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = false;
                            this.NISPToolbarButton("7").Enabled = false;
                            break;
                        }
                    case 1:
                        {
                            this.NISPToolbarButton("2").Enabled = false;
                            this.NISPToolbarButton("3").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = true;
                            this.NISPToolbarButton("7").Enabled = true;
                            break;
                        }
                    case 2:
                        {
                            this.NISPToolbarButton("2").Enabled = false;
                            this.NISPToolbarButton("3").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = true;
                            this.NISPToolbarButton("7").Enabled = true;
                            break;
                        }
                }
            }
            //20151107, liliana, LIBST13020, end
            else
            {
                //20151005, liliana, LIBST13020, end
                switch (_intType)
                {
                    case 0:
                        {
                            //20150706, liliana, LIBST13020, begin
                            //this.NISPToolbarButton("2").Visible = true;
                            //this.NISPToolbarButton("3").Visible = true;
                            //this.NISPToolbarButton("4").Visible = true;
                            //this.NISPToolbarButton("5").Visible = false;
                            //this.NISPToolbarButton("6").Visible = false;
                            //this.NISPToolbarButton("7").Visible = false;
                            this.NISPToolbarButton("2").Enabled = true;
                            this.NISPToolbarButton("3").Enabled = true;
                            this.NISPToolbarButton("4").Enabled = true;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = false;
                            this.NISPToolbarButton("7").Enabled = false;
                            //20150706, liliana, LIBST13020, end
                            break;
                        }
                    case 1:
                        {
                            //20150706, liliana, LIBST13020, begin
                            //this.NISPToolbarButton("2").Visible = false;
                            //this.NISPToolbarButton("3").Visible = false;
                            //this.NISPToolbarButton("4").Visible = false;
                            //this.NISPToolbarButton("5").Visible = false;
                            //this.NISPToolbarButton("6").Visible = true;
                            //this.NISPToolbarButton("7").Visible = true;
                            this.NISPToolbarButton("2").Enabled = false;
                            this.NISPToolbarButton("3").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = true;
                            this.NISPToolbarButton("7").Enabled = true;
                            //20150706, liliana, LIBST13020, begin
                            break;
                        }
                    case 2:
                        {
                            //20150706, liliana, LIBST13020, begin
                            //this.NISPToolbarButton("2").Visible = false;
                            //this.NISPToolbarButton("3").Visible = false;
                            //this.NISPToolbarButton("4").Visible = false;
                            //this.NISPToolbarButton("5").Visible = false;
                            //this.NISPToolbarButton("6").Visible = true;
                            //this.NISPToolbarButton("7").Visible = true;
                            this.NISPToolbarButton("2").Enabled = false;
                            this.NISPToolbarButton("3").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Visible = false;
                            this.NISPToolbarButton("6").Enabled = true;
                            this.NISPToolbarButton("7").Enabled = true;
                            //20150706, liliana, LIBST13020, end
                            break;
                        }
                }
                //20151005, liliana, LIBST13020, begin
            }
            //20151005, liliana, LIBST13020, end
        }

        private void frmTransactionNew_Load(object sender, EventArgs e)
        {
            //20170825, liliana, COPOD17271, begin
            this._clsCoreBankMessaging = new clsCoreBankMessaging(intNIK, strGuid, strModule, GlobalFunctionCIF.QueryCIF);
            //20170825, liliana, COPOD17271, end
            objFormDocument = new frmDocument();
            BankNISP.Template.StandardForm.NISPFormAuthorization CurrentAuthorization = new BankNISP.Template.StandardForm.NISPFormAuthorization();
            objFormDocument.NISPSetAuthorization(CurrentAuthorization);

            DataSet dsTreeview;
            OleDbParameter[] dbParam = new OleDbParameter[3];
            (dbParam[0] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
            (dbParam[1] = new OleDbParameter("@pcModule", OleDbType.VarChar, 30)).Value = strModule;
            (dbParam[2] = new OleDbParameter("@pcMenuName", OleDbType.VarChar, 50)).Value = strMenuName;
            bool blnResult = ClQ.ExecProc("UserGetTreeView", ref dbParam, out dsTreeview);
            if (blnResult == true)
            {
                _dvAkses = new DataView(dsTreeview.Tables[1]);
                subResetToolBar();
            }
            else
            {
                MessageBox.Show("Error Get Treeview Nodes");
            }

            _strTabName = "SUBS";
            cTransaksi = new clsTransaksi(_strTabName, intNIK, strGuid, ClQ);

            dvSubscription = new DataView(cTransaksi.dttSubscription);
            dvRedemption = new DataView(cTransaksi.dttRedemption);
            dvSubsRDB = new DataView(cTransaksi.dttSubsRDB);

            dataGridViewSubs.DataSource = dvSubscription;
            dataGridViewRedemp.DataSource = dvRedemption;
            dataGridViewRDB.DataSource = dvSubsRDB;

            subSetVisibleGrid(_strTabName);

            _intType = 0;
            ResetForm();
            DisableAllForm(false);
            DisableFormTrxSubs(false);
            DisableFormTrxRedemp(false);
            DisableFormTrxRDB(false);

            GetComponentSearch();
            GetKodeKantor();
            PopulateComboBox();
            IsClickByDatagrid = false;

            objFormDocument.ClQ = ClQ;
            objFormDocument._menuName = strMenuName;
            //20161004, liliana, CSODD16311, begin
            tmrTimer.Tick += new EventHandler(OnTimerTick);
            tmrTimer.Interval = 1000; // 1 second interval
            tmrTimer.Start();
            //20161004, liliana, CSODD16311, end

            //20231227, gio, RDN-1108, begin
            MessageBox.Show("Pastikan jam yang diinput sesuai dengan intrusksi Nasabah dan tercantum pada formulir transaksi. \nInstruksi transaksi yang diterima setelah jam 13.00 WIB akan menggunakan NAB hari bursa berikutnya. ", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //20231227, gio, RDN-1108, end
        }

        private void SetDefaultValue()
        {
            dateTglTransaksiSubs.Value = DateTime.Today;
            dateTglTransaksiRedemp.Value = DateTime.Today;
            dateTglTransaksiRDB.Value = DateTime.Today;
            dateTglTransaksiSwc.Value = DateTime.Today;
            dateTglTransaksiSwcRDB.Value = DateTime.Today;
            dateTglTransaksiBooking.Value = DateTime.Today;
            //20200408, Lita, RDN-88, begin
            dtTglDebetRDB.Value = DateTime.Today;
            //20200408, Lita, RDN-88, end

            txtbInputterSubs.Text = ProReksa2.Global.strCurrentUserID.ToString();
            txtbInputterRedemp.Text = ProReksa2.Global.strCurrentUserID.ToString();
            txtbInputterRDB.Text = ProReksa2.Global.strCurrentUserID.ToString();
            txtbInputterSwc.Text = ProReksa2.Global.strCurrentUserID.ToString();
            txtbInputterSwcRDB.Text = ProReksa2.Global.strCurrentUserID.ToString();
            txtbInputterBooking.Text = ProReksa2.Global.strCurrentUserID.ToString();


            _ComboJenisSubs.SelectedIndex = 1;
            _ComboJenisRedemp.SelectedIndex = 1;
            _ComboJenisRDB.SelectedIndex = 0;
            _ComboJenisBooking.SelectedIndex = 0;
        }

        private void GetComponentSearch()
        {
            cmpsrNoRefSubs.SearchDesc = "REKSA_TRXREFID";
            cmpsrNoRefRedemp.SearchDesc = "REKSA_TRXREFID";
            cmpsrNoRefRDB.SearchDesc = "REKSA_TRXREFID";
            cmpsrNoRefSwc.SearchDesc = "SWITCHING";
            cmpsrNoRefSwcRDB.SearchDesc = "SWITCHINGRDB";
            cmpsrNoRefBooking.SearchDesc = "BOOKING_CODE";

            cmpsrNoRefSubs.Criteria = _strTabName;
            cmpsrNoRefRedemp.Criteria = _strTabName;
            cmpsrNoRefRDB.Criteria = _strTabName;

            cmpsrProductSubs.SearchDesc = "REKSA_TRXPRODUCT";

            cmpsrKodeKantorSubs.SearchDesc = "SIBS_OFFICE";
            cmpsrKodeKantorRedemp.SearchDesc = "SIBS_OFFICE";
            cmpsrKodeKantorRDB.SearchDesc = "SIBS_OFFICE";
            cmpsrKodeKantorSwc.SearchDesc = "SIBS_OFFICE";
            cmpsrKodeKantorSwcRDB.SearchDesc = "SIBS_OFFICE";
            cmpsrKodeKantorBooking.SearchDesc = "SIBS_OFFICE";

            //20150723, liliana, LIBST13020, begin
            //cmpsrCIFSubs.SearchDesc = "REKSA_CIF";
            //cmpsrCIFRedemp.SearchDesc = "REKSA_CIF";
            //cmpsrCIFRDB.SearchDesc = "REKSA_CIF";
            //cmpsrCIFSwc.SearchDesc = "REKSA_CIF";
            //cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF";
            //cmpsrCIFBooking.SearchDesc = "REKSA_CIF";
            cmpsrCIFSubs.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFRedemp.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFRDB.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFSwc.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFBooking.SearchDesc = "REKSA_CIF_ALL";
            //20150723, liliana, LIBST13020, end

            cmpsrClientSubs.SearchDesc = "REKSA_TRXCLIENTNEW";
            cmpsrClientRedemp.SearchDesc = "REKSA_TRXCLIENTNEW";
            cmpsrClientRDB.SearchDesc = "REKSA_TRXCLIENTNEW";

            cmpsrClientSwcIn.SearchDesc = "REKSA_CLIENTIN";
            cmpsrClientSwcOut.SearchDesc = "REKSA_TRXCLIENTNEW";

            cmpsrClientSwcRDBOut.SearchDesc = "REKSA_TRXCLIENTNEW";
            cmpsrClientSwcRDBIn.SearchDesc = "REKSA_TRXCLIENTNEW";

            cmpsrCurrSubs.SearchDesc = "CURRENCY_CODE";
            cmpsrCurrRDB.SearchDesc = "CURRENCY_CODE";
            cmpsrCurrBooking.SearchDesc = "CURRENCY_CODE";

            cmpsrSellerSubs.SearchDesc = "REFERENTOR";
            cmpsrSellerRedemp.SearchDesc = "REFERENTOR";
            cmpsrSellerRDB.SearchDesc = "REFERENTOR";
            cmpsrSellerSwc.SearchDesc = "REFERENTOR";
            cmpsrSellerSwcRDB.SearchDesc = "REFERENTOR";
            cmpsrSellerBooking.SearchDesc = "REFERENTOR";

            cmpsrWaperdSubs.SearchDesc = "REKSA_WAPERD";
            cmpsrWaperdRedemp.SearchDesc = "REKSA_WAPERD";
            cmpsrWaperdRDB.SearchDesc = "REKSA_WAPERD";
            cmpsrWaperdSwc.SearchDesc = "REKSA_WAPERD";
            cmpsrWaperdSwcRDB.SearchDesc = "REKSA_WAPERD";
            cmpsrWaperdBooking.SearchDesc = "REKSA_WAPERD";

            cmpsrReferentorSubs.SearchDesc = "REFERENTOR";
            cmpsrReferentorRedemp.SearchDesc = "REFERENTOR";
            cmpsrReferentorRDB.SearchDesc = "REFERENTOR";
            cmpsrReferentorSwc.SearchDesc = "REFERENTOR";
            cmpsrReferentorSwcRDB.SearchDesc = "REFERENTOR";
            cmpsrReferentorBooking.SearchDesc = "REFERENTOR";

            //20210913, korvi, RDN-674, begin
            cmpsrNoRekSubs.SearchDesc = "REKSA_CIF_ACCTNO";
            cmpsrNoRekRedemp.SearchDesc = "REKSA_CIF_ACCTNO";
            cmpsrNoRekRDB.SearchDesc = "REKSA_CIF_ACCTNO";
            cmpsrNoRekSwc.SearchDesc = "REKSA_CIF_ACCTNO";
            cmpsrNoRekSwcRDB.SearchDesc = "REKSA_CIF_ACCTNO";
            cmpsrNoRekBooking.SearchDesc = "REKSA_CIF_ACCTNO";
            //20210913, korvi, RDN-674, end

        }

        private void GetKodeKantor()
        {
            //20160509, Elva, CSODD16117, begin
            if (ValidasiKodeKantor(strBranch))
            {
                //20160509, Elva, CSODD16117, end
                cmpsrKodeKantorSubs.Text1 = strBranch;
                cmpsrKodeKantorSubs.ValidateField();
                cmpsrKodeKantorRedemp.Text1 = strBranch;
                cmpsrKodeKantorRedemp.ValidateField();
                cmpsrKodeKantorRDB.Text1 = strBranch;
                cmpsrKodeKantorRDB.ValidateField();
                cmpsrKodeKantorSwc.Text1 = strBranch;
                cmpsrKodeKantorSwc.ValidateField();
                cmpsrKodeKantorSwcRDB.Text1 = strBranch;
                cmpsrKodeKantorSwcRDB.ValidateField();
                cmpsrKodeKantorBooking.Text1 = strBranch;
                cmpsrKodeKantorBooking.ValidateField();
                //20160509, Elva, CSODD16117, begin
            }
            else
                subCancel();
            //ValidasiKodeKantor(strBranch);
            //20160509, Elva, CSODD16117, begin
        }

        //20160509, Elva, CSODD16117, begin
        //private void ValidasiKodeKantor(string strKodeKantor)
        private bool ValidasiKodeKantor(string strKodeKantor)
        //20160509, Elva, CSODD16117, begin
        {
            string strIsAllowed;
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[2];

            try
            {
                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcKodeKantor", System.Data.OleDb.OleDbType.VarChar, 5);
                dbParam[0].Value = strKodeKantor;
                dbParam[0].Direction = System.Data.ParameterDirection.Input;

                dbParam[1] = new System.Data.OleDb.OleDbParameter("@pbIsAllowed", System.Data.OleDb.OleDbType.VarChar, 1);
                dbParam[1].Value = "";
                dbParam[1].Direction = System.Data.ParameterDirection.InputOutput;

                if (ClQ.ExecProc("dbo.ReksaValidateOfficeId", ref dbParam))
                {
                    strIsAllowed = dbParam[1].Value.ToString();

                    if (strIsAllowed == "0")
                    {
                        _tabJenisTransaksi.Enabled = false;
                        MessageBox.Show("Kode Kantor tidak terdaftar, transaksi reksadana tidak dapat dilakukan!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        this.NISPToolbarButton("2").Visible = false;
                        this.NISPToolbarButton("3").Visible = false;
                        this.NISPToolbarButton("4").Visible = false;
                        this.NISPToolbarButton("5").Visible = false;
                        this.NISPToolbarButton("6").Visible = false;
                        this.NISPToolbarButton("7").Visible = false;
                        this.NISPToolbarButton("8").Visible = false;
                        //20160509, Elva, CSODD16117, begin
                        //return;
                        return false;
                        //20160509, Elva, CSODD16117, end
                    }
                    else
                    {
                        _tabJenisTransaksi.Enabled = true;
                        //20221104, ahmad.fansyuri, RDN-873, Begin
                        label63.Visible = false;
                        cmbAutoRedempRDB.Visible = false;
                        //20221104, ahmad.fansyuri, RDN-873, End
                        //20160509, Elva, CSODD16117, begin
                        return true;
                        //20160509, Elva, CSODD16117, end
                    }
                }
                //20160509, Elva, CSODD16117, begin
                else
                    return false;
                //20160509, Elva, CSODD16117, end
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
                //20160509, Elva, CSODD16117, begin
                return false;
                //20160509, Elva, CSODD16117, end
            }
        }

        private void ResetForm()
        {
            ResetFormSubs();
            ResetFormRedemp();
            ResetFormRDB();
            ResetFormSwc();
            ResetFormSwcRDB();
            ResetFormBooking();

            //20150723, liliana, LIBST13020, begin
            //cmpsrCIFSubs.SearchDesc = "REKSA_CIF";
            //cmpsrCIFRedemp.SearchDesc = "REKSA_CIF";
            //cmpsrCIFRDB.SearchDesc = "REKSA_CIF";
            //cmpsrCIFSwc.SearchDesc = "REKSA_CIF";
            //cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF";
            //cmpsrCIFBooking.SearchDesc = "REKSA_CIF";
            cmpsrCIFSubs.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFRedemp.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFRDB.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFSwc.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF_ALL";
            cmpsrCIFBooking.SearchDesc = "REKSA_CIF_ALL";
            //20150723, liliana, LIBST13020, end
            //20161004, liliana, CSODD16311, begin
            lblTaxAmnestySubs.Visible = false;
            lblTaxAmnestyRedemp.Visible = false;
            lblTaxAmnestySwc.Visible = false;
            lblTaxAmnestySwcRDB.Visible = false;
            lblTaxAmnestyRDB.Visible = false;
            lblTaxAmnestyBooking.Visible = false;
            //20161004, liliana, CSODD16311, end

            if (objFormDocument != null) objFormDocument.ResetForm();
        }

        private void ResetFormSubs()
        {
            cmpsrNoRefSubs.Text1 = "";
            cmpsrNoRefSubs.Text2 = "";
            cmpsrCIFSubs.Text1 = "";
            cmpsrCIFSubs.Text2 = "";
            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSubs.Text1 = "";
            cmpsrNoRekSubs.Text2 = "";
            //20210922, korvi, RDN-674, end

            textSIDSubs.Text = "";
            txtUmurSubs.Text = "";
            textShareHolderIdSubs.Text = "";
            textRekeningSubs.Text = "";
            maskedRekeningSubs.Text = "";
            maskedRekeningSubsUSD.Text = "";
            //20150728, liliana, LIBST13020, begin
            maskedRekeningSubsMC.Text = "";
            //20150728, liliana, LIBST13020, end
            //20150702, liliana, LIBST13020, begin
            txtbRiskProfileSubs.Text = "";
            dtpRiskProfileSubs.Value = DateTime.Today;
            //20150702, liliana, LIBST13020, end

            dateTglTransaksiSubs.Value = DateTime.Today;
            ResetFormTrxSubs();
            dataGridViewSubs.DataSource = null;

            txtbInputterSubs.Text = "";
            cmpsrSellerSubs.Text1 = "";
            cmpsrSellerSubs.Text2 = "";
            cmpsrWaperdSubs.Text1 = "";
            cmpsrWaperdSubs.Text2 = "";
            textExpireWaperdSubs.Text = "";
            cmpsrReferentorSubs.Text1 = "";
            cmpsrReferentorSubs.Text2 = "";
            //20161114, liliana, LOGEN08391, begin
            cmbTASubs.SelectedIndex = -1;
            //20161114, liliana, LOGEN08391, end
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSubs.Text = "";
            richTextBoxKeteranganSubs.Text = "";
            //20221017, Andi, HFUNDING-178, end
        }

        private void ResetFormTrxSubs()
        {
            //20150629, liliana, LIBST13020, begin
            _StatusTransaksiSubs = "";
            //20150629, liliana, LIBST13020, end
            textNoTransaksiSubs.Text = "";
            cmpsrProductSubs.Text1 = "";
            cmpsrProductSubs.Text2 = "";
            cmpsrClientSubs.Text1 = "";
            cmpsrClientSubs.Text2 = "";
            cmpsrCurrSubs.Text1 = "";
            cmpsrCurrSubs.Text2 = "";
            nispMoneyNomSubs.Value = 0;
            checkPhoneOrderSubs.Checked = false;
            checkFullAmtSubs.Checked = true;
            checkFeeEditSubs.Checked = false;
            //20150424, liliana, LIBST13020, begin
            //_ComboJenisSubs.SelectedIndex = 0;
            _ComboJenisSubs.SelectedIndex = 1;
            //20150424, liliana, LIBST13020, end
            nispMoneyFeeSubs.Value = 0;
            labelFeeCurrencySubs.Text = "";
            nispPercentageFeeSubs.Value = 0;
            //20150505, liliana, LIBST13020, begin
            buttonAddSubs.Text = "&Add";
            buttonEditSubs.Text = "&Edit";
            //20150505, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            //20161114, liliana, LOGEN08391, begin
            //cmbTASubs.SelectedIndex = -1;
            //20161114, liliana, LOGEN08391, end
            //20160829, liliana, LOGEN00196, end

        }

        private void ResetFormRedemp()
        {
            cmpsrNoRefRedemp.Text1 = "";
            cmpsrNoRefRedemp.Text2 = "";
            cmpsrCIFRedemp.Text1 = "";
            cmpsrCIFRedemp.Text2 = "";
            //20210922, korvi, RDN-674, begin
            cmpsrNoRekRedemp.Text1 = "";
            cmpsrNoRekRedemp.Text2 = "";
            //20210922, korvi, RDN-674, end

            textSIDRedemp.Text = "";
            txtUmurRedemp.Text = "";
            textShareHolderIdRedemp.Text = "";
            textRekeningRedemp.Text = "";
            //20150505, liliana, LIBST13020, begin
            maskedRekeningRedemp.Text = "";
            //20150505, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningRedempUSD.Text = "";
            maskedRekeningRedempMC.Text = "";
            //20150728, liliana, LIBST13020, end
            //20150702, liliana, LIBST13020, begin
            txtbRiskProfileRedemp.Text = "";
            dtpRiskProfileRedemp.Value = DateTime.Today;
            //20150702, liliana, LIBST13020, end

            //20150427, liliana, LIBST13020, begin
            //dateTglTransaksiRedemp.Value = DateTime.Parse("1900-01-01");
            dateTglTransaksiRedemp.Value = DateTime.Today;
            //20150427, liliana, LIBST13020, end
            ResetFormTrxRedemp();
            dataGridViewRedemp.DataSource = null;

            txtbInputterRedemp.Text = "";
            cmpsrSellerRedemp.Text1 = "";
            cmpsrSellerRedemp.Text2 = "";
            cmpsrWaperdRedemp.Text1 = "";
            cmpsrWaperdRedemp.Text2 = "";
            textExpireWaperdRedemp.Text = "";
            cmpsrReferentorRedemp.Text1 = "";
            cmpsrReferentorRedemp.Text2 = "";
            //20161114, liliana, LOGEN08391, begin
            cmbTARedemp.SelectedIndex = -1;
            //20161114, liliana, LOGEN08391, end
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesRedemp.Text = "";
            richTextBoxKeteranganRedemp.Text = "";
            //20221017, Andi, HFUNDING-178, end
        }

        private void ResetFormTrxRedemp()
        {
            //20150629, liliana, LIBST13020, begin
            _StatusTransaksiRedemp = "";
            //20150629, liliana, LIBST13020, end
            textNoTransaksiRedemp.Text = "";
            cmpsrProductRedemp.Text1 = "";
            cmpsrProductRedemp.Text2 = "";
            cmpsrClientRedemp.Text1 = "";
            cmpsrClientRedemp.Text2 = "";

            nispOutstandingUnitRedemp.Value = 0;
            nispRedempUnit.Value = 0;

            checkPhoneOrderRedemp.Checked = false;
            checkFeeEditRedemp.Checked = false;
            //20150424, liliana, LIBST13020, begin
            //_ComboJenisRedemp.SelectedIndex = 0;
            _ComboJenisRedemp.SelectedIndex = 1;
            //20150424, liliana, LIBST13020, end
            nispMoneyFeeRedemp.Value = 0;
            labelFeeCurrencyRedemp.Text = "";
            nispPercentageFeeRedemp.Value = 0;
            //20150505, liliana, LIBST13020, begin
            buttonAddRedemp.Text = "&Add";
            buttonEditRedemp.Text = "&Edit";
            //20150505, liliana, LIBST13020, end
            //20150622, liliana, LIBST13020, begin
            checkAll.Checked = false;
            //20150622, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            //20161114, liliana, LOGEN08391, begin
            //cmbTARedemp.SelectedIndex = -1;
            //20161114, liliana, LOGEN08391, end
            //20160829, liliana, LOGEN00196, end
        }

        private void ResetFormRDB()
        {
            cmpsrNoRefRDB.Text1 = "";
            cmpsrNoRefRDB.Text2 = "";
            cmpsrCIFRDB.Text1 = "";
            cmpsrCIFRDB.Text2 = "";

            textSIDRDB.Text = "";
            txtUmurRDB.Text = "";
            textShareHolderIdRDB.Text = "";
            textRekeningRDB.Text = "";
            //20150505, liliana, LIBST13020, begin
            maskedRekeningRDB.Text = "";
            //20150505, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningRDBUSD.Text = "";
            maskedRekeningRDBMC.Text = "";
            //20150728, liliana, LIBST13020, end
            //20150702, liliana, LIBST13020, begin
            txtbRiskProfileRDB.Text = "";
            dtpRiskProfileRDB.Value = DateTime.Today;
            //20150702, liliana, LIBST13020, end

            //20150427, liliana, LIBST13020, begin
            //dateTglTransaksiRDB.Value = DateTime.Parse("1900-01-01");
            dateTglTransaksiRDB.Value = DateTime.Today;
            //20150427, liliana, LIBST13020, end
            //20200408, Lita, RDN-88, begin
            dtTglDebetRDB.Value = DateTime.Today;
            //20200408, Lita, RDN-88, end

            ResetFormTrxSubsRDB();
            dataGridViewRDB.DataSource = null;

            txtbInputterRDB.Text = "";
            cmpsrSellerRDB.Text1 = "";
            cmpsrSellerRDB.Text2 = "";
            cmpsrWaperdRDB.Text1 = "";
            cmpsrWaperdRDB.Text2 = "";
            textExpireWaperdRDB.Text = "";
            cmpsrReferentorRDB.Text1 = "";
            cmpsrReferentorRDB.Text2 = "";
            //20161114, liliana, LOGEN08391, begin
            cmbTARDB.SelectedIndex = -1;
            //20161114, liliana, LOGEN08391, end

            //20220520, Lita, RDN-781, begin
            cmpsrNoRekRDB.Text1 = "";
            cmpsrNoRekRDB.Text2 = "";

            //20220520, Lita, RDN-781, end
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSubsRdb.Text = "";
            richTextBoxKeteranganSubsRdb.Text = "";
            //20221017, Andi, HFUNDING-178, end
        }

        private void ResetFormTrxSubsRDB()
        {
            //20150629, liliana, LIBST13020, begin
            _StatusTransaksiRDB = "";
            //20150629, liliana, LIBST13020, end
            //20150413, liliana, LIBST13020, begin
            textNoTransaksiRDB.Text = "";
            //20150413, liliana, LIBST13020, end
            cmpsrProductRDB.Text1 = "";
            cmpsrProductRDB.Text2 = "";
            cmpsrClientRDB.Text1 = "";
            cmpsrClientRDB.Text2 = "";
            cmpsrCurrRDB.Text1 = "";
            cmpsrCurrRDB.Text2 = "";
            nispMoneyNomRDB.Value = 0;
            checkPhoneOrderRDB.Checked = false;

            nispJangkaWktRDB.Value = 0;
            dtJatuhTempoRDB.Value = 0;
            dtJatuhTempoRDB.Text = "";
            //20220322, gio, RDN-757, begin
            //cmbFrekPendebetanRDB.SelectedIndex = -1;
            //20220322, gio, RDN-757, end
            //20221107, ahmad.fansyuri, RDN-873, BEGIN
            cmbAutoRedempRDB.SelectedIndex = 1;
            //20221107, ahmad.fansyuri, RDN-873, END
            //20220322, gio, RDN-757, begin
            //cmbAsuransiRDB.SelectedIndex = 0;
            cmbAsuransiRDB.SelectedIndex = -1;
            //20220322, gio, RDN-757, end

            checkFeeEditRDB.Checked = false;
            _ComboJenisRDB.SelectedIndex = 0;
            nispMoneyFeeRDB.Value = 0;
            labelFeeCurrencyRDB.Text = "";
            nispPercentageFeeRDB.Value = 0;
            //20150505, liliana, LIBST13020, begin
            buttonAddRDB.Text = "&Add";
            buttonEditRDB.Text = "&Edit";
            //20150505, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            //20161114, liliana, LOGEN08391, begin
            //cmbTARDB.SelectedIndex = -1;
            //20161114, liliana, LOGEN08391, end
            //20160829, liliana, LOGEN00196, end
        }

        private void ResetFormSwc()
        {
            //20150420, liliana, LIBST13020, begin
            labelStatusSwc.Text = "";
            //20150420, liliana, LIBST13020, end
            cmpsrNoRefSwc.Text1 = "";
            cmpsrNoRefSwc.Text2 = "";
            cmpsrCIFSwc.Text1 = "";
            cmpsrCIFSwc.Text2 = "";
            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSwc.Text1 = "";
            cmpsrNoRekSwc.Text2 = "";
            //20210922, korvi, RDN-674, end

            textSIDSwc.Text = "";
            txtUmurSwc.Text = "";
            textShareHolderIdSwc.Text = "";
            textRekeningSwc.Text = "";
            //20150505, liliana, LIBST13020, begin
            maskedRekeningSwc.Text = "";
            //20150505, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningSwcUSD.Text = "";
            maskedRekeningSwcMC.Text = "";
            //20150728, liliana, LIBST13020, end
            //20150702, liliana, LIBST13020, begin
            txtbRiskProfileSwc.Text = "";
            dtpRiskProfileSwc.Value = DateTime.Today;
            //20150702, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTASwc.SelectedIndex = -1;
            //20160829, liliana, LOGEN00196, end

            textNoTransaksiSwc.Text = "";
            //20150427, liliana, LIBST13020, begin
            //dateTglTransaksiSwc.Value = DateTime.Parse("1900-01-01");
            dateTglTransaksiSwc.Value = DateTime.Today;
            //20150427, liliana, LIBST13020, end

            cmpsrProductSwcOut.Text1 = "";
            cmpsrProductSwcOut.Text2 = "";

            cmpsrClientSwcOut.Text1 = "";
            cmpsrClientSwcOut.Text2 = "";

            nispOutstandingUnitSwc.Value = 0;
            nispRedempSwc.Value = 0;

            cmpsrProductSwcIn.Text1 = "";
            cmpsrProductSwcIn.Text2 = "";

            cmpsrClientSwcIn.Text1 = "";
            cmpsrClientSwcIn.Text2 = "";

            checkPhoneOrderSwc.Checked = false;
            checkFeeEditSwc.Checked = false;
            //20150622, liliana, LIBST13020, begin
            checkSwcAll.Checked = false;
            //20150622, liliana, LIBST13020, end
            nispMoneyFeeSwc.Value = 0;
            labelFeeCurrencySwc.Text = "";
            nispPercentageFeeSwc.Value = 0;


            txtbInputterSwc.Text = "";
            cmpsrSellerSwc.Text1 = "";
            cmpsrSellerSwc.Text2 = "";
            cmpsrWaperdSwc.Text1 = "";
            cmpsrWaperdSwc.Text2 = "";
            textExpireWaperdSwc.Text = "";
            cmpsrReferentorSwc.Text1 = "";
            cmpsrReferentorSwc.Text2 = "";
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSwcNonRdb.Text = "";
            richTextBoxKeteranganSwcNonRdb.Text = "";
            //20221017, Andi, HFUNDING-178, end
        }

        private void ResetFormSwcRDB()
        {
            //20150420, liliana, LIBST13020, begin
            labelStatusSwcRDB.Text = "";
            //20150420, liliana, LIBST13020, end
            cmpsrNoRefSwcRDB.Text1 = "";
            cmpsrNoRefSwcRDB.Text2 = "";
            cmpsrCIFSwcRDB.Text1 = "";
            cmpsrCIFSwcRDB.Text2 = "";
            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSwcRDB.Text1 = "";
            cmpsrNoRekSwcRDB.Text2 = "";
            //20210922, korvi, RDN-674, end

            textSIDSwcRDB.Text = "";
            txtUmurSwcRDB.Text = "";
            textShareHolderIdSwcRDB.Text = "";
            textRekeningSwcRDB.Text = "";
            //20150505, liliana, LIBST13020, begin
            maskedRekeningSwcRDB.Text = "";
            //20150505, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningSwcRDBUSD.Text = "";
            maskedRekeningSwcRDBMC.Text = "";
            //20150728, liliana, LIBST13020, end
            //20150702, liliana, LIBST13020, begin
            txtbRiskProfileSwcRDB.Text = "";
            dtpRiskProfileSwcRDB.Value = DateTime.Today;
            //20150702, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTASwcRDB.SelectedIndex = -1;
            //20160829, liliana, LOGEN00196, end

            textNoTransaksiSwcRDB.Text = "";
            //20150427, liliana, LIBST13020, begin
            //dateTglTransaksiSwcRDB.Value = DateTime.Parse("1900-01-01");
            dateTglTransaksiSwcRDB.Value = DateTime.Today;
            //20150427, liliana, LIBST13020, end


            cmpsrProductSwcRDBOut.Text1 = "";
            cmpsrProductSwcRDBOut.Text2 = "";

            cmpsrClientSwcRDBOut.Text1 = "";
            cmpsrClientSwcRDBOut.Text2 = "";

            nispOutstandingUnitSwcRDB.Value = 0;
            nispRedempSwcRDB.Value = 0;

            //20220805, antoniusfilian, RDN-835, begin
            checkSwcRDBAll.Checked = false;
            //20220805, antoniusfilian, RDN-835, end
            nispJangkaWktSwcRDB.Value = 0;
            dtJatuhTempoSwcRDB.Value = 0;
            cmbFrekPendebetanSwcRDB.SelectedIndex = -1;
            cmbAutoRedempSwcRDB.SelectedIndex = -1;
            cmbAsuransiSwcRDB.SelectedIndex = -1;

            cmpsrProductSwcRDBIn.Text1 = "";
            cmpsrProductSwcRDBIn.Text2 = "";

            cmpsrClientSwcRDBIn.Text1 = "";
            cmpsrClientSwcRDBIn.Text2 = "";

            checkPhoneOrderSwcRDB.Checked = false;
            checkFeeEditSwcRDB.Checked = false;
            nispMoneyFeeSwcRDB.Value = 0;
            labelFeeCurrencySwcRDB.Text = "";
            nispPercentageFeeSwcRDB.Value = 0;


            txtbInputterSwcRDB.Text = "";
            cmpsrSellerSwcRDB.Text1 = "";
            cmpsrSellerSwcRDB.Text2 = "";
            cmpsrWaperdSwcRDB.Text1 = "";
            cmpsrWaperdSwcRDB.Text2 = "";
            textExpireWaperdSwcRDB.Text = "";
            cmpsrReferentorSwcRDB.Text1 = "";
            cmpsrReferentorSwcRDB.Text2 = "";

            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSwcRdb.Text = "";
            richTextBoxKeteranganSwcRdb.Text = "";
            //20221017, Andi, HFUNDING-178, end
        }

        private void ResetFormBooking()
        {
            //20150420, liliana, LIBST13020, begin
            labelStatusBook.Text = "";
            //20150420, liliana, LIBST13020, end
            cmpsrNoRefBooking.Text1 = "";
            cmpsrNoRefBooking.Text2 = "";
            cmpsrCIFBooking.Text1 = "";
            cmpsrCIFBooking.Text2 = "";
            //20210922, korvi, RDN-674, begin
            cmpsrNoRekBooking.Text1 = "";
            cmpsrNoRekBooking.Text2 = "";
            //20210922, korvi, RDN-674, end

            textSIDBooking.Text = "";
            txtUmurBooking.Text = "";
            textShareHolderIdBooking.Text = "";
            textRekeningBooking.Text = "";
            //20150505, liliana, LIBST13020, begin
            maskedRekeningBooking.Text = "";
            //20150505, liliana, LIBST13020, end
            //201507028, liliana, LIBST13020, begin
            maskedRekeningBookingUSD.Text = "";
            maskedRekeningBookingMC.Text = "";
            //201507028, liliana, LIBST13020, end
            //20150702, liliana, LIBST13020, begin
            txtbRiskProfileBooking.Text = "";
            dtpRiskProfileBooking.Value = DateTime.Today;
            //20150702, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTABook.SelectedIndex = -1;
            //20160829, liliana, LOGEN00196, end

            textNoTransaksiBooking.Text = "";
            //20150427, liliana, LIBST13020, begin
            //dateTglTransaksiBooking.Value = DateTime.Parse("1900-01-01");
            dateTglTransaksiBooking.Value = DateTime.Today;
            //20150427, liliana, LIBST13020, end
            cmpsrProductBooking.Text1 = "";
            cmpsrProductBooking.Text2 = "";
            cmpsrClientBooking.Text1 = "";
            cmpsrClientBooking.Text2 = "";
            cmpsrCurrBooking.Text1 = "";
            cmpsrCurrBooking.Text2 = "";

            nispMoneyNomBooking.Value = 0;
            checkPhoneOrderBooking.Checked = false;
            _sisaunit.Text = "";

            checkFeeEditBooking.Checked = false;
            _ComboJenisBooking.SelectedIndex = -1;
            nispMoneyFeeBooking.Value = 0;
            labelFeeCurrencyBooking.Text = "";
            nispPercentageFeeBooking.Value = 0;

            txtbInputterBooking.Text = "";
            cmpsrSellerBooking.Text1 = "";
            cmpsrSellerBooking.Text2 = "";
            cmpsrWaperdBooking.Text1 = "";
            cmpsrWaperdBooking.Text2 = "";
            textExpireWaperdBooking.Text = "";
            cmpsrReferentorBooking.Text1 = "";
            cmpsrReferentorBooking.Text2 = "";
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesBook.Text = "";
            richTextBoxKeteranganBook.Text = "";
            //20221017, Andi, HFUNDING-178, end
        }

        private void DisableAllForm(bool isEnabled)
        {
            DisableFormSubs(isEnabled);
            DisableFormRedemp(isEnabled);
            DisableFormRDB(isEnabled);
            DisableFormSwc(isEnabled);
            DisableFormSwcRDB(isEnabled);
            DisableFormBooking(isEnabled);
            objFormDocument.SetReadOnly(!isEnabled);
        }

        private void DisableFormSubs(bool isEnabled)
        {
            cmpsrKodeKantorSubs.Enabled = false;
            cmpsrNoRefSubs.Enabled = !isEnabled;
            //20150505, liliana, LIBST13020, begin
            //cmpsrCIFSubs.Enabled = isEnabled;
            cmpsrCIFSubs.Enabled = !isEnabled;
            btnDocumentSubs.Enabled = isEnabled;
            //20150505, liliana, LIBST13020, end

            textSIDSubs.Enabled = false;
            txtUmurSubs.Enabled = false;
            textShareHolderIdSubs.Enabled = false;
            textRekeningSubs.Enabled = false;
            //20150505, liliana, LIBST13020, begin
            maskedRekeningSubs.Enabled = false;
            //20150518, liliana, LIBST13020, begin
            maskedRekeningSubsUSD.Enabled = false;
            //20150518, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningSubsMC.Enabled = false;
            //20150728, liliana, LIBST13020, end
            //20150505, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTASubs.Enabled = isEnabled;
            //20160829, liliana, LOGEN00196, end

            //20150505, liliana, LIBST13020, begin
            //textNoTransaksiSubs.Enabled = false;
            //dateTglTransaksiSubs.Enabled = false;
            //cmpsrProductSubs.Enabled = isEnabled;
            //cmpsrClientSubs.Enabled = isEnabled;
            //cmpsrCurrSubs.Enabled = false;
            //20150505, liliana, LIBST13020, end

            //20150505, liliana, LIBST13020, begin
            //if (_intType == 2)
            //{
            //    nispMoneyNomSubs.Enabled = !isEnabled;
            //    checkPhoneOrderSubs.Enabled = !isEnabled;
            //    checkFullAmtSubs.Enabled = !isEnabled;
            //    checkFeeEditSubs.Enabled = !isEnabled;

            //    cmpsrNoRefSubs.Enabled = false;
            //    //20150505, liliana, LIBST13020, begin
            //    cmpsrCIFSubs.Enabled = false;
            //    //20150505, liliana, LIBST13020, end

            //    buttonAddSubs.Enabled = false;
            //    buttonEditSubs.Enabled = isEnabled;
            //}
            //else
            //{
            //    nispMoneyNomSubs.Enabled = isEnabled;
            //    checkPhoneOrderSubs.Enabled = isEnabled;
            //    checkFullAmtSubs.Enabled = isEnabled;
            //    checkFeeEditSubs.Enabled = isEnabled;

            //    buttonAddSubs.Enabled = isEnabled;
            //    buttonEditSubs.Enabled = isEnabled;
            //}


            //_ComboJenisSubs.Enabled = false;
            //nispMoneyFeeSubs.Enabled = false;
            //nispPercentageFeeSubs.Enabled = false;
            //20150505, liliana, LIBST13020, end

            txtbInputterSubs.Enabled = false;
            cmpsrSellerSubs.Enabled = isEnabled;
            cmpsrWaperdSubs.Enabled = false;
            textExpireWaperdSubs.Enabled = false;
            cmpsrReferentorSubs.Enabled = isEnabled;

            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSubs.Enabled = isEnabled;
            richTextBoxKeteranganSubs.Enabled = isEnabled;
            //20221017, Andi, HFUNDING-178, end
        }
        //20150505, liliana, LIBST13020, begin

        private void DisableFormTrxSubs(bool isEnabled)
        {
            textNoTransaksiSubs.Enabled = false;
            //20240115, gio, RDN-1115, begin
            //dateTglTransaksiSubs.Enabled = false;
            dateTglTransaksiSubs.Enabled = isEnabled;
            //20240115, gio, RDN-1115, end

            cmpsrProductSubs.Enabled = isEnabled;
            cmpsrClientSubs.Enabled = isEnabled;
            cmpsrCurrSubs.Enabled = false;

            nispMoneyNomSubs.Enabled = isEnabled;
            //20150812, liliana, LIBST13020, begin
            //checkPhoneOrderSubs.Enabled = isEnabled;
            checkPhoneOrderSubs.Enabled = false;

            if ((isEnabled == true) && (cmpsrCIFSubs.Text1 != ""))
            {
                if (GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSubs.Text1))
                {
                    checkPhoneOrderBooking.Enabled = true;
                }
            }
            //20150812, liliana, LIBST13020, end
            checkFullAmtSubs.Enabled = isEnabled;
            checkFeeEditSubs.Enabled = isEnabled;

            buttonAddSubs.Enabled = isEnabled;
            buttonEditSubs.Enabled = isEnabled;


            _ComboJenisSubs.Enabled = false;
            nispMoneyFeeSubs.Enabled = false;
            nispPercentageFeeSubs.Enabled = false;

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSubs.Enabled = isEnabled;
            //20210922, korvi, RDN-674, end
        }
        //20150505, liliana, LIBST13020, end

        private void DisableFormRedemp(bool isEnabled)
        {
            cmpsrKodeKantorRedemp.Enabled = false;
            cmpsrNoRefRedemp.Enabled = !isEnabled;
            //20150505, liliana, LIBST13020, begin
            //cmpsrCIFRedemp.Enabled = isEnabled;
            cmpsrCIFRedemp.Enabled = !isEnabled;
            btnDokumenRedemp.Enabled = false;
            //20150505, liliana, LIBST13020, end

            textSIDRedemp.Enabled = false;
            txtUmurRedemp.Enabled = false;
            textShareHolderIdRedemp.Enabled = false;
            textRekeningRedemp.Enabled = false;
            //20150505, liliana, LIBST13020, begin
            maskedRekeningRedemp.Enabled = false;
            //20150505, liliana, LIBST13020, end
            //20150619, liliana, LIBST13020, begin
            maskedRekeningRedempUSD.Enabled = false;
            //20150619, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningRedempMC.Enabled = false;
            //20150728, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTARedemp.Enabled = isEnabled;
            //20160829, liliana, LOGEN00196, end

            //20150505, liliana, LIBST13020, begin
            //textNoTransaksiRedemp.Enabled = false;
            //dateTglTransaksiRedemp.Enabled = false;
            //cmpsrProductRedemp.Enabled = isEnabled;
            //cmpsrClientRedemp.Enabled = isEnabled;

            //nispOutstandingUnitRedemp.Enabled = false;

            //if (_intType == 2)
            //{
            //    nispRedempUnit.Enabled = !isEnabled;
            //    checkPhoneOrderRedemp.Enabled = !isEnabled;
            //    checkFeeEditRedemp.Enabled = !isEnabled;
            //    checkAll.Enabled = !isEnabled;

            //    cmpsrNoRefRedemp.Enabled = false;
            //    //20150505, liliana, LIBST13020, begin
            //    cmpsrCIFRedemp.Enabled = false;
            //    //20150505, liliana, LIBST13020, end

            //    buttonAddRedemp.Enabled = false;
            //    buttonEditRedemp.Enabled = isEnabled;
            //}
            //else
            //{
            //    nispRedempUnit.Enabled = isEnabled;
            //    checkPhoneOrderRedemp.Enabled = isEnabled;
            //    checkFeeEditRedemp.Enabled = isEnabled;
            //    checkAll.Enabled = isEnabled;

            //    buttonAddRedemp.Enabled = isEnabled;
            //    buttonEditRedemp.Enabled = isEnabled;
            //}


            //_ComboJenisRedemp.Enabled = false;
            //nispMoneyFeeRedemp.Enabled = false;
            //nispPercentageFeeRedemp.Enabled = false;
            //20150505, liliana, LIBST13020, end

            txtbInputterRedemp.Enabled = false;
            cmpsrSellerRedemp.Enabled = isEnabled;
            cmpsrWaperdRedemp.Enabled = false;
            textExpireWaperdRedemp.Enabled = false;
            cmpsrReferentorRedemp.Enabled = isEnabled;

            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesRedemp.Enabled = isEnabled;
            richTextBoxKeteranganRedemp.Enabled = isEnabled;
            //20221017, Andi, HFUNDING-178, end
        }
        //20150505, liliana, LIBST13020, begin

        private void DisableFormTrxRedemp(bool isEnabled)
        {
            textNoTransaksiRedemp.Enabled = false;
            //20240115, gio, RDN-1115, begin
            //dateTglTransaksiRedemp.Enabled = false;
            dateTglTransaksiRedemp.Enabled = isEnabled;
            //20240115, gio, RDN-1115, end
            cmpsrProductRedemp.Enabled = isEnabled;
            cmpsrClientRedemp.Enabled = isEnabled;

            nispOutstandingUnitRedemp.Enabled = false;

            nispRedempUnit.Enabled = isEnabled;
            //20150812, liliana, LIBST13020, begin
            //checkPhoneOrderRedemp.Enabled = isEnabled;
            checkPhoneOrderRedemp.Enabled = false;

            if ((isEnabled == true) && (cmpsrCIFRedemp.Text1 != ""))
            {
                if (GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRedemp.Text1))
                {
                    checkPhoneOrderRedemp.Enabled = true;
                }
            }
            //20150812, liliana, LIBST13020, end
            checkFeeEditRedemp.Enabled = isEnabled;
            checkAll.Enabled = isEnabled;

            buttonAddRedemp.Enabled = isEnabled;
            buttonEditRedemp.Enabled = isEnabled;

            _ComboJenisRedemp.Enabled = false;
            nispMoneyFeeRedemp.Enabled = false;
            nispPercentageFeeRedemp.Enabled = false;

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekRedemp.Enabled = isEnabled;
            //20210922, korvi, RDN-674, end
        }
        //20150505, liliana, LIBST13020, end

        private void DisableFormRDB(bool isEnabled)
        {
            cmpsrKodeKantorRDB.Enabled = false;
            cmpsrNoRefRDB.Enabled = !isEnabled;
            //20150505, liliana, LIBST13020, begin
            //cmpsrCIFRDB.Enabled = isEnabled;
            cmpsrCIFRDB.Enabled = !isEnabled;
            btnDokumenRDB.Enabled = isEnabled;
            //20150505, liliana, LIBST13020, end

            textSIDRDB.Enabled = false;
            txtUmurRDB.Enabled = false;
            textShareHolderIdRDB.Enabled = false;
            textRekeningRDB.Enabled = false;
            //20150505, liliana, LIBST13020, begin
            maskedRekeningRDB.Enabled = false;
            //20150505, liliana, LIBST13020, end
            //20150619, liliana, LIBST13020, begin
            maskedRekeningRDBUSD.Enabled = false;
            //20150619, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningRDBMC.Enabled = false;
            //20150728, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTARDB.Enabled = isEnabled;
            //20160829, liliana, LOGEN00196, end

            //20150505, liliana, LIBST13020, begin
            //textNoTransaksiRDB.Enabled = isEnabled;
            //dateTglTransaksiRDB.Enabled = false;
            //cmpsrProductRDB.Enabled = isEnabled;
            //cmpsrClientRDB.Enabled = isEnabled;
            //cmpsrCurrRDB.Enabled = false;

            //if (_intType == 2)
            //{
            //    nispMoneyNomRDB.Enabled = !isEnabled;
            //    checkPhoneOrderRDB.Enabled = !isEnabled;
            //    cmbAutoRedempRDB.Enabled = !isEnabled;
            //    cmbAsuransiRDB.Enabled = !isEnabled;

            //    cmpsrNoRefRDB.Enabled = false;
            //    //20150505, liliana, LIBST13020, begin
            //    cmpsrCIFRDB.Enabled = false;
            //    //20150505, liliana, LIBST13020, end

            //    buttonAddRDB.Enabled = false;
            //    buttonEditRDB.Enabled = isEnabled;
            //}
            //else
            //{
            //    nispMoneyNomRDB.Enabled = isEnabled;
            //    checkPhoneOrderRDB.Enabled = isEnabled;
            //    cmbAutoRedempRDB.Enabled = isEnabled;
            //    cmbAsuransiRDB.Enabled = isEnabled;

            //    buttonAddRDB.Enabled = isEnabled;
            //    buttonEditRDB.Enabled = isEnabled;

            //}


            //nispJangkaWktRDB.Enabled = isEnabled;
            //dtJatuhTempoRDB.Enabled = false;
            //cmbFrekPendebetanRDB.Enabled = isEnabled;


            //checkFeeEditRDB.Enabled = false;
            //_ComboJenisRDB.Enabled = false;
            //nispMoneyFeeRDB.Enabled = false;
            //nispPercentageFeeRDB.Enabled = false;
            //20150505, liliana, LIBST13020, end

            txtbInputterRDB.Enabled = false;
            cmpsrSellerRDB.Enabled = isEnabled;
            cmpsrWaperdRDB.Enabled = false;
            textExpireWaperdRDB.Enabled = false;
            cmpsrReferentorRDB.Enabled = isEnabled;

            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSubsRdb.Enabled = isEnabled;
            richTextBoxKeteranganSubsRdb.Enabled = isEnabled;
            //20221017, Andi, HFUNDING-178, end
        }
        //20150505, liliana, LIBST13020, begin

        private void DisableFormTrxRDB(bool isEnabled)
        {
            textNoTransaksiRDB.Enabled = false;
            //20240115, gio, RDN-1115, begin
            //dateTglTransaksiRDB.Enabled = false;
            dateTglTransaksiRDB.Enabled = isEnabled;
            //20240115, gio, RDN-1115, end
            cmpsrProductRDB.Enabled = isEnabled;
            cmpsrClientRDB.Enabled = false;
            cmpsrCurrRDB.Enabled = false;

            nispMoneyNomRDB.Enabled = isEnabled;
            //20150812, liliana, LIBST13020, begin
            //checkPhoneOrderRDB.Enabled = isEnabled;
            checkPhoneOrderRDB.Enabled = false;

            if ((isEnabled == true) && (cmpsrCIFRDB.Text1 != ""))
            {
                if (GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRDB.Text1))
                {
                    checkPhoneOrderRDB.Enabled = true;
                }
            }
            //20150812, liliana, LIBST13020, end
            cmbAutoRedempRDB.Enabled = isEnabled;
            cmbAsuransiRDB.Enabled = isEnabled;

            buttonAddRDB.Enabled = isEnabled;
            buttonEditRDB.Enabled = isEnabled;

            nispJangkaWktRDB.Enabled = isEnabled;
            dtJatuhTempoRDB.Enabled = false;
            cmbFrekPendebetanRDB.Enabled = isEnabled;
            //20200408, Lita, RDN-88, begin
            cmbFrekDebetMethodRDB.Enabled = isEnabled;
            dtTglDebetRDB.Enabled = isEnabled;
            //20200408, Lita, RDN-88, begin

            checkFeeEditRDB.Enabled = false;
            _ComboJenisRDB.Enabled = false;
            nispMoneyFeeRDB.Enabled = false;
            nispPercentageFeeRDB.Enabled = false;

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekRDB.Enabled = isEnabled;
            //20210922, korvi, RDN-674, end

        }
        //20150505, liliana, LIBST13020, end

        private void DisableFormSwc(bool isEnabled)
        {
            cmpsrKodeKantorSwc.Enabled = false;
            cmpsrNoRefSwc.Enabled = !isEnabled;
            //20150505, liliana, LIBST13020, begin
            //cmpsrCIFSwc.Enabled = isEnabled;
            cmpsrCIFSwc.Enabled = !isEnabled;
            checkSwcAll.Enabled = isEnabled;
            //20150710, liliana, LIBST13020, begin
            checkPhoneOrderSwc.Enabled = isEnabled;
            checkFeeEditSwc.Enabled = isEnabled;
            nispRedempSwc.Enabled = isEnabled;
            //20150710, liliana, LIBST13020, end
            //20151013, liliana, LIBST13020, begin
            nispPercentageFeeSwc.Enabled = false;
            //20151013, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTASwc.Enabled = isEnabled;
            //20160829, liliana, LOGEN00196, end

            if (_intType == 2)
            {
                cmpsrNoRefSwc.Enabled = false;
                cmpsrCIFSwc.Enabled = false;
                //20150710, liliana, LIBST13020, begin
                checkPhoneOrderSwc.Enabled = true;
                checkFeeEditSwc.Enabled = true;
                //20150825,liliana, LIBST13020, begin
                //nispRedempSwc.Enabled = true;
                //checkSwcAll.Enabled = true;
                nispRedempSwc.Enabled = false;
                checkSwcAll.Enabled = false;
                //20150825,liliana, LIBST13020, end
                //20150710, liliana, LIBST13020, end
                //20151013, liliana, LIBST13020, begin
                if (checkFeeEditSwc.Checked)
                {
                    nispPercentageFeeSwc.Enabled = true;
                }
                else
                {
                    nispPercentageFeeSwc.Enabled = false;
                }
                //20151013, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                cmbTASwc.Enabled = false;
                //20160829, liliana, LOGEN00196, end
            }

            maskedRekeningSwc.Enabled = false;
            //20150619, liliana, LIBST13020, begin
            maskedRekeningSwcUSD.Enabled = false;
            //20150619, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningSwcMC.Enabled = false;
            //20150728, liliana, LIBST13020, END
            btnDokumenSwc.Enabled = isEnabled;
            //20150505, liliana, LIBST13020, end

            textSIDSwc.Enabled = false;
            txtUmurSwc.Enabled = false;
            textShareHolderIdSwc.Enabled = false;
            textRekeningSwc.Enabled = false;

            textNoTransaksiSwc.Enabled = isEnabled;
            //20240115, gio, RDN-1115, begin
            //dateTglTransaksiSwc.Enabled = false;
            dateTglTransaksiSwc.Enabled = isEnabled;
            //20240115, gio, RDN-1115, end

            cmpsrProductSwcOut.Enabled = isEnabled;
            cmpsrClientSwcOut.Enabled = isEnabled;

            nispOutstandingUnitSwc.Enabled = false;
            //20150710, liliana, LIBST13020, begin
            //nispRedempSwc.Enabled = isEnabled;
            //20150710, liliana, LIBST13020, end

            cmpsrProductSwcIn.Enabled = isEnabled;

            cmpsrClientSwcIn.Enabled = false;

            //20150710, liliana, LIBST13020, begin
            //checkPhoneOrderSwc.Enabled = isEnabled;
            //checkFeeEditSwc.Enabled = isEnabled;
            //20150710, liliana, LIBST13020, end
            nispMoneyFeeSwc.Enabled = false;
            //20151013, liliana, LIBST13020, begin
            //nispPercentageFeeSwc.Enabled = false;
            //20151013, liliana, LIBST13020, end

            txtbInputterSwc.Enabled = false;
            cmpsrSellerSwc.Enabled = isEnabled;
            cmpsrWaperdSwc.Enabled = false;
            textExpireWaperdSwc.Enabled = false;
            cmpsrReferentorSwc.Enabled = isEnabled;

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSwc.Enabled = isEnabled;
            //20210922, korvi, RDN-674, end
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSwcNonRdb.Enabled = isEnabled;
            richTextBoxKeteranganSwcNonRdb.Enabled = isEnabled;
            //20221017, Andi, HFUNDING-178, end
        }

        private void DisableFormSwcRDB(bool isEnabled)
        {
            cmpsrKodeKantorSwcRDB.Enabled = false;
            cmpsrNoRefSwcRDB.Enabled = !isEnabled;
            //20150505, liliana, LIBST13020, begin
            //cmpsrCIFSwcRDB.Enabled = isEnabled;
            cmpsrCIFSwcRDB.Enabled = !isEnabled;
            //20150710, liliana, LIBST13020, begin
            checkPhoneOrderSwcRDB.Enabled = isEnabled;
            //20150825,liliana, LIBST13020, begin
            //checkFeeEditSwcRDB.Enabled = isEnabled;
            checkFeeEditSwcRDB.Enabled = false;
            //20150825,liliana, LIBST13020, end
            nispRedempSwcRDB.Enabled = isEnabled;
            //20150710, liliana, LIBST13020, end
            //20220805, antoniusfilian, RDN-835, begin
            checkSwcRDBAll.Enabled = isEnabled;
            //20220805, antoniusfilian, RDN-835, end
            //20160829, liliana, LOGEN00196, begin
            cmbTASwcRDB.Enabled = isEnabled;
            //20160829, liliana, LOGEN00196, end

            if (_intType == 2)
            {
                cmpsrNoRefSwcRDB.Enabled = false;
                cmpsrCIFSwcRDB.Enabled = false;
                //20150710, liliana, LIBST13020, begin
                checkPhoneOrderSwcRDB.Enabled = true;
                //20150825,liliana, LIBST13020, begin
                //checkFeeEditSwcRDB.Enabled = true;
                //nispRedempSwcRDB.Enabled = true;
                checkFeeEditSwcRDB.Enabled = false;
                nispRedempSwcRDB.Enabled = false;
                //20150825,liliana, LIBST13020, end
                //20150710, liliana, LIBST13020, end
                //20220805, antoniusfilian, RDN-835, begin
                checkSwcRDBAll.Enabled = false;
                //20220805, antoniusfilian, RDN-835, end
                //20160829, liliana, LOGEN00196, begin
                cmbTASwcRDB.Enabled = false;
                //20160829, liliana, LOGEN00196, end
            }

            maskedRekeningSwcRDB.Enabled = false;
            btnDokumenSwcRDB.Enabled = isEnabled;
            //20150505, liliana, LIBST13020, end

            textSIDSwcRDB.Enabled = false;
            txtUmurSwcRDB.Enabled = false;
            textShareHolderIdSwcRDB.Enabled = false;
            textRekeningSwcRDB.Enabled = false;
            //20150619, liliana, LIBST13020, begin
            maskedRekeningSwcRDBUSD.Enabled = false;
            //20150619, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            maskedRekeningSwcRDBMC.Enabled = false;
            //20150728, liliana, LIBST13020, END

            textNoTransaksiSwcRDB.Enabled = isEnabled;
            //20240115, gio, RDN-1115, begin
            //dateTglTransaksiSwcRDB.Enabled = false;
            dateTglTransaksiSwcRDB.Enabled = isEnabled;
            //20240115, gio, RDN-1115, end

            cmpsrProductSwcRDBOut.Enabled = isEnabled;
            cmpsrClientSwcRDBOut.Enabled = isEnabled;

            nispOutstandingUnitSwcRDB.Enabled = false;
            //20150710, liliana, LIBST13020, begin
            //nispRedempSwcRDB.Enabled = false;
            //20150710, liliana, LIBST13020, end

            nispJangkaWktSwcRDB.Enabled = false;
            dtJatuhTempoSwcRDB.Enabled = false;
            cmbFrekPendebetanSwcRDB.Enabled = false;
            cmbAutoRedempSwcRDB.Enabled = false;
            cmbAsuransiSwcRDB.Enabled = false;

            cmpsrProductSwcRDBIn.Enabled = isEnabled;
            cmpsrClientSwcRDBIn.Enabled = false;

            //20150710, liliana, LIBST13020, begin
            //checkPhoneOrderSwcRDB.Enabled = isEnabled;
            //checkFeeEditSwcRDB.Enabled = false;
            //20150710, liliana, LIBST13020, end
            nispMoneyFeeSwcRDB.Enabled = false;
            nispPercentageFeeSwcRDB.Enabled = false;


            txtbInputterSwcRDB.Enabled = false;
            cmpsrSellerSwcRDB.Enabled = isEnabled;
            cmpsrWaperdSwcRDB.Enabled = false;
            textExpireWaperdSwcRDB.Enabled = false;
            cmpsrReferentorSwcRDB.Enabled = isEnabled;

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSwcRDB.Enabled = isEnabled;
            //20210922, korvi, RDN-674, end
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesSwcRdb.Enabled = isEnabled;
            richTextBoxKeteranganSwcRdb.Enabled = isEnabled;
            //20221017, Andi, HFUNDING-178, end
        }

        private void DisableFormBooking(bool isEnabled)
        {
            cmpsrKodeKantorBooking.Enabled = false;
            cmpsrNoRefBooking.Enabled = !isEnabled;
            //20150505, liliana, LIBST13020, begin
            //cmpsrCIFBooking.Enabled = isEnabled;
            cmpsrCIFBooking.Enabled = !isEnabled;
            btnDokumenBooking.Enabled = isEnabled;
            //20150709, liliana, LIBST13020, begin
            //20150811, liliana, LIBST13020, begin
            //checkFeeEditBooking.Enabled = isEnabled;
            checkFeeEditBooking.Enabled = false;
            //20150811, liliana, LIBST13020, end
            nispMoneyNomBooking.Enabled = isEnabled;
            //20150709, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            cmbTABook.Enabled = isEnabled;
            //20160829, liliana, LOGEN00196, end

            if (_intType == 2)
            {
                cmpsrNoRefBooking.Enabled = false;
                cmpsrCIFBooking.Enabled = false;

                nispMoneyNomBooking.Enabled = true;
                //20150709, liliana, LIBST13020, begin
                //cmpsrProductBooking.Enabled = true;
                //20150811, liliana, LIBST13020, begin
                //checkFeeEditBooking.Enabled = true;
                checkFeeEditBooking.Enabled = false;
                //20150811, liliana, LIBST13020, end
                //20150709, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                cmbTABook.Enabled = false;
                //20160829, liliana, LOGEN00196, end
            }
            //20150505, liliana, LIBST13020, end

            textSIDBooking.Enabled = false;
            txtUmurBooking.Enabled = false;
            textShareHolderIdBooking.Enabled = false;
            textRekeningBooking.Enabled = false;
            //20150505, liliana, LIBST13020, begin
            maskedRekeningBooking.Enabled = false;
            //20150505, liliana, LIBST13020, end
            //20150619, liliana, LIBST13020, begin
            maskedRekeningBookingUSD.Enabled = false;
            //20150619, liliana, LIBST13020, end
            //201507028, liliana, LIBST13020, begin
            maskedRekeningBookingMC.Enabled = false;
            //201507028, liliana, LIBST13020, END

            textNoTransaksiBooking.Enabled = isEnabled;
            //20240115, gio, RDN-1115, begin
            //dateTglTransaksiBooking.Enabled = false;
            dateTglTransaksiBooking.Enabled = isEnabled;
            //20240115, gio, RDN-1115, end
            cmpsrProductBooking.Enabled = isEnabled;
            cmpsrClientBooking.Enabled = false;
            cmpsrCurrBooking.Enabled = false;

            //20150709, liliana, LIBST13020, begin
            //nispMoneyNomBooking.Enabled = isEnabled;
            //20150709, liliana, LIBST13020, end
            checkPhoneOrderBooking.Enabled = false;
            _sisaunit.Enabled = false;

            //20150709, liliana, LIBST13020, begin
            //checkFeeEditBooking.Enabled = false;
            //20150709, liliana, LIBST13020, end
            _ComboJenisBooking.Enabled = false;
            nispMoneyFeeBooking.Enabled = false;
            nispPercentageFeeBooking.Enabled = false;

            txtbInputterBooking.Enabled = false;
            cmpsrSellerBooking.Enabled = isEnabled;
            cmpsrWaperdBooking.Enabled = false;
            textExpireWaperdBooking.Enabled = false;
            cmpsrReferentorBooking.Enabled = isEnabled;

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekBooking.Enabled = isEnabled;
            //20210922, korvi, RDN-674, end
            //20221017, Andi, HFUNDING-178, begin
            textBoxKodeSalesBook.Enabled = isEnabled;
            richTextBoxKeteranganBook.Enabled = isEnabled;
            //20221017, Andi, HFUNDING-178, end
        }

        private void frmTransactionNew_OnNISPToolbarClick(ref ToolStripButton NISPToolbarButton)
        {
            switch (NISPToolbarButton.Name)
            {
                case ("1"): //keluar
                    {
                        this.Close();
                        break;
                    }
                case ("2"): //refresh
                    {
                        subRefresh();
                        break;
                    }
                case ("3"): // new
                    {
                        subNew();
                        break;
                    }
                case ("4"): // update
                    {
                        //20150326, liliana, LIBST13020, begin
                        //subUpdate();
                        if (strMenuName == "mnuMaintainTrxPO")
                        {
                            subUpdateAsuransi();
                        }
                        else
                        {
                            subUpdate();
                        }
                        //20150326, liliana, LIBST13020, end
                        break;
                    }
                case ("6"): //save
                    {
                        if (strMenuName == "mnuMaintainTrxPO")
                        {
                            subSaveAsuransi();
                        }
                        else
                        {
                            subSave();
                        }
                        break;
                    }
                case ("7"): //cancel
                    {
                        subCancel();
                        break;
                    }
                //20141211, Ferry, LIBST13020, begin
                case ("8"): //print
                    {
                        //20150505, liliana, LIBST13020, begin
                        if (_strTabName == "SUBS")
                        {
                            if (cmpsrCIFSubs.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No CIF Subscription Harus Diisi!");
                                return;
                            }


                            if (cmpsrNoRefSubs.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No Referensi Subscription Harus Diisi!");
                                return;
                            }
                        }
                        else if (_strTabName == "REDEMP")
                        {
                            if (cmpsrCIFRedemp.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No CIF Redemption Harus Diisi!");
                                return;
                            }


                            if (cmpsrNoRefRedemp.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No Referensi Redemption Harus Diisi!");
                                return;
                            }
                        }
                        else if (_strTabName == "SUBSRDB")
                        {
                            if (cmpsrCIFRDB.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No CIF RDB Harus Diisi!");
                                return;
                            }

                            if (cmpsrNoRefRDB.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No Referensi RDB Harus Diisi!");
                                return;
                            }
                        }
                        else if (_strTabName == "BOOK")
                        {
                            if (cmpsrCIFBooking.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No CIF Booking Harus Diisi!");
                                return;
                            }

                            if (cmpsrNoRefBooking.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No Referensi Booking Harus Diisi!");
                                return;
                            }
                        }
                        else if (_strTabName == "SWCNONRDB")
                        {
                            if (cmpsrCIFSwc.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No CIF Switching Harus Diisi!");
                                return;
                            }

                            if (cmpsrNoRefSwc.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No Referensi Switching Harus Diisi!");
                                return;
                            }
                        }
                        else if (_strTabName == "SWCNONRDB")
                        {
                            if (cmpsrCIFSwcRDB.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No CIF Switching RDB Harus Diisi!");
                                return;
                            }

                            if (cmpsrNoRefSwcRDB.Text1.Trim().Length == 0)
                            {
                                MessageBox.Show("No Referensi Switching RDB Harus Diisi!");
                                return;
                            }
                        }
                        //20150505, liliana, LIBST13020, end
                        string strTranType = "";
                        if (_strTabName == "SUBS") //Subscription
                            strTranType = cmpsrNoRefSubs._Text1.Text;
                        else if (_strTabName == "REDEMP") //Redemption
                            strTranType = cmpsrNoRefRedemp._Text1.Text;
                        else if (_strTabName == "SUBSRDB") //RDB
                            strTranType = cmpsrNoRefRDB._Text1.Text;
                        else if (_strTabName == "SWCNONRDB") //Switching
                            strTranType = cmpsrNoRefSwc._Text1.Text;
                        else if (_strTabName == "SWCRDB") //Switching RDB
                            strTranType = cmpsrNoRefSwcRDB._Text1.Text;
                        else if (_strTabName == "BOOK") //Nooking
                            //20150505, liliana, LIBST13020, begin
                            //strTranType = cmpsrNoRefBooking._Text1.Text.Remove(3, 1);
                            strTranType = cmpsrNoRefBooking._Text1.Text;
                        //20150505, liliana, LIBST13020, end
                        ProReksa2.frmKonfirmasi FK = new ProReksa2.frmKonfirmasi(ClQ, strTranType, _strTabName);
                        FK.Show();
                        break;
                    }
                //20141211, Ferry, LIBST13020, end
            }
        }

        private void subRefresh()
        {
            //20240912, Lely, RDN-1182, begin
            _iMode = 0;
            //20240912, Lely, RDN-1182, end

            if (_strTabName == "SUBS")
            {
                //20150505, liliana, LIBST13020, begin
                //20150715, liliana, LIBST13020, begin
                //if (cmpsrCIFSubs.Text1.Trim().Length == 0)
                //{
                //    MessageBox.Show("No CIF Subscription Harus Diisi!");
                //    return;
                //}
                //20150715, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, end
                if (cmpsrNoRefSubs.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("Silahkan Pilih kode Nomor referensi terlebih dahulu!");
                    return;
                }

                cTransaksi.ClearData();

                if (cTransaksi.GetDataTransaksi(cmpsrNoRefSubs.Text1.Trim(), intNIK, strGuid, _strTabName))
                {
                    //20170828, liliana, COPOD17271, begin
                    if (this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cTransaksi.CIFNo, strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        //20170828, liliana, COPOD17271, end
                        dvSubscription = new DataView(cTransaksi.dttSubscription);
                        dataGridViewSubs.DataSource = dvSubscription;
                        dataGridViewSubs.AutoResizeColumns();
                        subSetVisibleGrid(_strTabName);

                        for (int i = 0; i < dataGridViewSubs.Columns.Count; i++)
                        {
                            if (dataGridViewSubs.Columns[i].ValueType.ToString() == "System.Decimal")
                            {
                                //20220802, Lita, RDN-825, begin
                                //dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N2";
                                if (dataGridViewSubs.Columns[i].Name == "PctFee")
                                    dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N4";
                                else
                                    dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N2";
                                //20220802, Lita, RDN-825, end
                            }
                        }


                        cmpsrKodeKantorSubs.Text1 = cTransaksi.OfficeId;
                        cmpsrKodeKantorSubs.ValidateField();
                        //20160816, Elva, LOGEN00191, begin
                        _isCheckingTASubs = false;
                        //20160816, Elva, LOGEN00191, end
                        cmpsrCIFSubs.Text1 = cTransaksi.CIFNo;
                        cmpsrCIFSubs.Text2 = cTransaksi.CIFName;

                        //20160816, Elva, LOGEN00191, begin
                        _isCheckingTASubs = true;
                        //20160816, Elva, LOGEN00191, end

                        string strShareholderID, strNoRek, strNamaRek, strSID;
                        //20150518, liliana, LIBST13020, begin
                        //GetDataCIF(cmpsrCIFSubs.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                        string strNoRekUSD, strNamaRekUSD;
                        //20150702, liliana, LIBST13020, begin
                        //20150728, liliana, LIBST13020, begin
                        string strNoRekMC, strNamaRekMC;
                        //20150728, liliana, LIBST13020, end
                        string strRiskProfile;
                        DateTime dtLastUpdateRiskProfile;
                        //20150702, liliana, LIBST13020, end
                        GetDataCIF(cmpsrCIFSubs.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID,
                            out strNoRekUSD, out strNamaRekUSD
                            //20150728, liliana, LIBST13020, begin
                            , out strNoRekMC, out strNamaRekMC
                            //20150728, liliana, LIBST13020, end
                            //20150702, liliana, LIBST13020, begin
                            , out strRiskProfile, out dtLastUpdateRiskProfile
                            //20150702, liliana, LIBST13020, end
                            );
                        //20150518, liliana, LIBST13020, end
                        //20150702, liliana, LIBST13020, begin
                        txtbRiskProfileSubs.Text = strRiskProfile;
                        dtpRiskProfileSubs.Value = dtLastUpdateRiskProfile;
                        //20150702, liliana, LIBST13020, end
                        textSIDSubs.Text = strSID;
                        textShareHolderIdSubs.Text = strShareholderID;
                        textRekeningSubs.Text = strNoRek;
                        //20150505, liliana, LIBST13020, begin
                        maskedRekeningSubs.Text = strNoRek;
                        //20150505, liliana, LIBST13020, end
                        //20150518, liliana, LIBST13020, begin
                        maskedRekeningSubsUSD.Text = strNoRekUSD;
                        textNamaRekeningSubsUSD.Text = strNamaRekUSD;
                        //20150518, liliana, LIBST13020, end
                        //20150728, liliana, LIBST13020, begin
                        maskedRekeningSubsMC.Text = strNoRekMC;
                        textNamaRekeningSubsMC.Text = strNamaRekMC;
                        //20150728, liliana, LIBST13020, end
                        textNamaRekeningSubs.Text = strNamaRek;
                        int intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFSubs.Text1);
                        txtUmurSubs.Text = intUmur.ToString();

                        //20220112, sandi, RDN-727, begin
                        //string strCriteria = _strTabName + "#" + cmpsrCIFSubs.Text1;
                        string strCriteria = _strTabName + "#" + cmpsrCIFSubs.Text1 + "#" + cmpsrNoRekSubs.Text2.Trim();
                        //20220112, sandi, RDN-727, end
                        cmpsrProductSubs.SearchDesc = "REKSA_TRXPRODUCT";
                        cmpsrProductSubs.Criteria = strCriteria;

                        cmpsrClientSubs.SearchDesc = "REKSA_TRXCLIENTNEW";
                        //20160829, liliana, LOGEN00196, begin
                        //cmpsrClientSubs.Criteria = cmpsrCIFSubs.Text1.Trim() + "#" + cmpsrProductSubs.Text1.Trim() + "#" + _strTabName;
                        cmpsrClientSubs.Criteria = cmpsrCIFSubs.Text1.Trim() + "#" + cmpsrProductSubs.Text1.Trim() + "#" + _strTabName
                                                    + "#" + cmbTASubs.SelectedIndex.ToString();
                        //20160829, liliana, LOGEN00196, end

                        txtbInputterSubs.Text = cTransaksi.Inputter;
                        cmpsrSellerSubs.Text1 = cTransaksi.Seller;
                        cmpsrSellerSubs.ValidateField();
                        cmpsrWaperdSubs.Text1 = cTransaksi.Waperd;
                        cmpsrWaperdSubs.ValidateField();
                        cmpsrReferentorSubs.Text1 = cTransaksi.Referentor;
                        cmpsrReferentorSubs.ValidateField();

                        //20210922, korvi, RDN-674, begin
                        cmpsrNoRekSubs.SearchDesc = "REKSA_CIF_ACCTNO";
                        cmpsrNoRekSubs.Criteria = cmpsrCIFSubs.Text1.Trim() + "#" + cmbTASubs.SelectedIndex.ToString();
                        cmpsrNoRekSubs.Text1 = cTransaksi.SelectedAccNo;
                        cmpsrNoRekSubs.Text2 = cTransaksi.TranCCY;
                        //20220119, sandi, RDN-727, begin
                        //cmpsrNoRekSubs.ValidateField();
                        //20220119, sandi, RDN-727, end
                        //20210922, korvi, RDN-674, end

                        objFormDocument.LoadData(false, 0, false, false, cmpsrNoRefSubs.Text1.Trim());

                        //20221019, Andi, HFUNDING-178, begin
                        string kodeSales, keterangan;
                        GetDataSalesEksekutif(cmpsrNoRefSubs.Text1.Trim(), out kodeSales, out keterangan);
                        textBoxKodeSalesSubs.Text = kodeSales;
                        richTextBoxKeteranganSubs.Text = keterangan;
                        //20221019, Andi, HFUNDING-178, end
                        //20240115, gio, RDN-1115, begin
                        dateTglTransaksiSubs.Value = DateTime.Parse(cTransaksi.dttSubscription.Rows[0]["TglTrx"].ToString());
                        //20240115, gio, RDN-1115, end
                        //20170828, liliana, COPOD17271, begin
                    }
                    else
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    //20170828, liliana, COPOD17271, end
                }
                else
                {
                    MessageBox.Show("No Referensi tidak ditemukan untuk transaksi subscription!");
                    return;
                }

            }
            else if (_strTabName == "REDEMP")
            {
                //20150505, liliana, LIBST13020, begin
                //20150715, liliana, LIBST13020, begin
                //if (cmpsrCIFRedemp.Text1.Trim().Length == 0)
                //{
                //    MessageBox.Show("No CIF Redemption Harus Diisi!");
                //    return;
                //}
                //20150715, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, end
                if (cmpsrNoRefRedemp.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("Silahkan Pilih kode Nomor referensi terlebih dahulu!");
                    return;
                }

                cTransaksi.ClearData();

                if (cTransaksi.GetDataTransaksi(cmpsrNoRefRedemp.Text1.Trim(), intNIK, strGuid, _strTabName))
                {
                    //20170828, liliana, COPOD17271, begin
                    if (this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cTransaksi.CIFNo, strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        //20170828, liliana, COPOD17271, end
                        dvRedemption = new DataView(cTransaksi.dttRedemption);
                        dataGridViewRedemp.DataSource = dvRedemption;
                        dataGridViewRedemp.AutoResizeColumns();
                        subSetVisibleGrid(_strTabName);

                        for (int i = 0; i < dataGridViewRedemp.Columns.Count; i++)
                        {
                            if (dataGridViewRedemp.Columns[i].ValueType.ToString() == "System.Decimal")
                            {
                                //20220520, Lita, RDN-825, begin
                                //dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N2";
                                if (dataGridViewRedemp.Columns[i].Name == "PctFee")
                                    dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N4";
                                else
                                    dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N2";
                                //20220802, Lita, RDN-825, end
                            }
                        }

                        cmpsrKodeKantorRedemp.Text1 = cTransaksi.OfficeId;
                        cmpsrKodeKantorRedemp.ValidateField();
                        //20160816, Elva, LOGEN00191, begin
                        _isCheckingTARedemp = false;
                        //20160816, Elva, LOGEN00191, end
                        cmpsrCIFRedemp.Text1 = cTransaksi.CIFNo;
                        cmpsrCIFRedemp.Text2 = cTransaksi.CIFName;
                        //20160816, Elva, LOGEN00191, begin
                        _isCheckingTARedemp = true;
                        //20160816, Elva, LOGEN00191, end

                        string strShareholderID, strNoRek, strNamaRek, strSID;
                        //20150518, liliana, LIBST13020, begin
                        //20150702, liliana, LIBST13020, begin
                        string strRiskProfile;
                        DateTime dtLastUpdateRiskProfile;
                        //20150702, liliana, LIBST13020, end
                        //GetDataCIF(cmpsrCIFRedemp.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                        string strNoRekUSD, strNameRekUSD;
                        //20150728, liliana, LIBST13020, begin
                        string strNoRekMC, strNameRekMC;
                        //20150728, liliana, LIBST13020, end
                        GetDataCIF(cmpsrCIFRedemp.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                            , out strNoRekUSD, out strNameRekUSD
                            //20150702, liliana, LIBST13020, begin
                            //20150728, liliana, LIBST13020, begin
                            , out strNoRekMC, out strNameRekMC
                            //20150728, liliana, LIBST13020, end
                            , out strRiskProfile, out dtLastUpdateRiskProfile
                            //20150702, liliana, LIBST13020, end
                            );
                        //20150518, liliana, LIBST13020, end
                        //20150702, liliana, LIBST13020, begin
                        txtbRiskProfileRedemp.Text = strRiskProfile;
                        dtpRiskProfileRedemp.Value = dtLastUpdateRiskProfile;
                        //20150702, liliana, LIBST13020, end
                        textSIDRedemp.Text = strSID;
                        textShareHolderIdRedemp.Text = strShareholderID;
                        textRekeningRedemp.Text = strNoRek;
                        //20150505, liliana, LIBST13020, begin
                        maskedRekeningRedemp.Text = strNoRek;
                        //20150505, liliana, LIBST13020, end
                        //20150518, liliana, LIBST13020, begin
                        maskedRekeningRedempUSD.Text = strNoRekUSD;
                        textNamaRekeningRedempUSD.Text = strNameRekUSD;
                        //20150518, liliana, LIBST13020, end
                        //20150728, liliana, LIBST13020, begin
                        maskedRekeningRedempMC.Text = strNoRekMC;
                        textNamaRekeningRedempMC.Text = strNameRekMC;
                        //20150728, liliana, LIBST13020, end
                        textNamaRekeningRedemp.Text = strNamaRek;
                        int intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFRedemp.Text1);
                        txtUmurRedemp.Text = intUmur.ToString();

                        //20220112, sandi, RDN-727, begin
                        //string strCriteria = _strTabName + "#" + cmpsrCIFRedemp.Text1;
                        string strCriteria = _strTabName + "#" + cmpsrCIFRedemp.Text1 + "#" + cmpsrNoRekRedemp.Text2.Trim();
                        //20220112, sandi, RDN-727, end
                        cmpsrProductRedemp.SearchDesc = "REKSA_TRXPRODUCT";
                        cmpsrProductRedemp.Criteria = strCriteria;
                        cmpsrClientRedemp.SearchDesc = "REKSA_TRXCLIENTNEW";
                        //20160829, liliana, LOGEN00196, begin
                        //cmpsrClientRedemp.Criteria = cmpsrCIFRedemp.Text1.Trim() + "#" + cmpsrProductRedemp.Text1.Trim() + "#" + _strTabName;
                        cmpsrClientRedemp.Criteria = cmpsrCIFRedemp.Text1.Trim() + "#" + cmpsrProductRedemp.Text1.Trim() + "#" + _strTabName
                                                    + "#" + cmbTARedemp.SelectedIndex.ToString();
                        //20160829, liliana, LOGEN00196, end

                        txtbInputterRedemp.Text = cTransaksi.Inputter;
                        cmpsrSellerRedemp.Text1 = cTransaksi.Seller;
                        cmpsrSellerRedemp.ValidateField();
                        cmpsrWaperdRedemp.Text1 = cTransaksi.Waperd;
                        cmpsrWaperdRedemp.ValidateField();
                        cmpsrReferentorRedemp.Text1 = cTransaksi.Referentor;
                        cmpsrReferentorRedemp.ValidateField();

                        //20210922, korvi, RDN-674, begin
                        cmpsrNoRekRedemp.SearchDesc = "REKSA_CIF_ACCTNO";
                        cmpsrNoRekRedemp.Criteria = cmpsrCIFRedemp.Text1.Trim() + "#" + cmbTARedemp.SelectedIndex.ToString();
                        cmpsrNoRekRedemp.Text1 = cTransaksi.SelectedAccNo;
                        cmpsrNoRekRedemp.Text2 = cTransaksi.TranCCY;

                        //20220119, sandi, RDN-727, begin
                        //cmpsrNoRekRedemp.ValidateField();
                        //20220119, sandi, RDN-727, end
                        //20210922, korvi, RDN-674, end

                        //20221019, Andi, HFUNDING-178, begin
                        string kodeSales, keterangan;
                        GetDataSalesEksekutif(cmpsrNoRefRedemp.Text1.Trim(), out kodeSales, out keterangan);
                        textBoxKodeSalesRedemp.Text = kodeSales;
                        richTextBoxKeteranganRedemp.Text = keterangan;
                        //20221019, Andi, HFUNDING-178, end

                        //20240115, gio, RDN-1115, begin
                        dateTglTransaksiRedemp.Value = DateTime.Parse(cTransaksi.dttRedemption.Rows[0]["TglTrx"].ToString());
                        //20240115, gio, RDN-1115, end
                        //20170828, liliana, COPOD17271, begin
                    }
                    else
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    //20170828, liliana, COPOD17271, end

                }
                else
                {
                    MessageBox.Show("No Referensi tidak ditemukan untuk transaksi redemption!");
                    return;
                }
            }
            else if (_strTabName == "SUBSRDB")
            {
                //20150505, liliana, LIBST13020, begin
                //20150715, liliana, LIBST13020, begin
                //if (cmpsrCIFRDB.Text1.Trim().Length == 0)
                //{
                //    MessageBox.Show("No CIF RDB Harus Diisi!");
                //    return;
                //}
                //20150715, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, end
                if (cmpsrNoRefRDB.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("Silahkan Pilih kode Nomor referensi terlebih dahulu!");
                    return;
                }

                cTransaksi.ClearData();

                if (cTransaksi.GetDataTransaksi(cmpsrNoRefRDB.Text1.Trim(), intNIK, strGuid, _strTabName))
                {
                    //20170828, liliana, COPOD17271, begin
                    if (this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cTransaksi.CIFNo, strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        //20170828, liliana, COPOD17271, end
                        dvSubsRDB = new DataView(cTransaksi.dttSubsRDB);
                        dataGridViewRDB.DataSource = dvSubsRDB;
                        dataGridViewRDB.AutoResizeColumns();
                        subSetVisibleGrid(_strTabName);

                        for (int i = 0; i < dataGridViewRDB.Columns.Count; i++)
                        {
                            if (dataGridViewRDB.Columns[i].ValueType.ToString() == "System.Decimal")
                            {
                                //20220802, Lita, RDN-825, begin
                                //dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N2";
                                if (dataGridViewRDB.Columns[i].Name == "PctFee")
                                    dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N4";
                                else
                                    dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N2";
                                //20220802, Lita, RDN-825, end
                            }
                        }

                        cmpsrKodeKantorRDB.Text1 = cTransaksi.OfficeId;
                        cmpsrKodeKantorRDB.ValidateField();
                        //20160816, Elva, LOGEN00191, begin
                        _isCheckingTARDB = false;
                        //20160816, Elva, LOGEN00191, end
                        cmpsrCIFRDB.Text1 = cTransaksi.CIFNo;
                        cmpsrCIFRDB.Text2 = cTransaksi.CIFName;
                        //20160816, Elva, LOGEN00191, begin
                        _isCheckingTARDB = true;
                        //20160816, Elva, LOGEN00191, end

                        string strShareholderID, strNoRek, strNamaRek, strSID;
                        //20150518, liliana, LIBST13020, begin
                        //20150702, liliana, LIBST13020, begin
                        string strRiskProfile;
                        DateTime dtLastUpdateRiskProfile;
                        //20150702, liliana, LIBST13020, end
                        //GetDataCIF(cmpsrCIFRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                        string strNoRekUSD, strNameRekUSD;
                        //20150728, liliana, LIBST13020, begin
                        string strNoRekMC, strNameRekMC;
                        //20150728, liliana, LIBST13020, END
                        GetDataCIF(cmpsrCIFRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                            , out strNoRekUSD, out strNameRekUSD
                            //20150702, liliana, LIBST13020, begin
                            //20150728, liliana, LIBST13020, begin
                            , out strNoRekMC, out strNameRekMC
                            //20150728, liliana, LIBST13020, END
                            , out strRiskProfile, out dtLastUpdateRiskProfile
                            //20150702, liliana, LIBST13020, end

                            );
                        //20150518, liliana, LIBST13020, end
                        //20150702, liliana, LIBST13020, begin
                        txtbRiskProfileRDB.Text = strRiskProfile;
                        dtpRiskProfileRDB.Value = dtLastUpdateRiskProfile;
                        //20150702, liliana, LIBST13020, end
                        textSIDRDB.Text = strSID;
                        textShareHolderIdRDB.Text = strShareholderID;
                        textRekeningRDB.Text = strNoRek;
                        //20150505, liliana, LIBST13020, begin
                        maskedRekeningRDB.Text = strNoRek;
                        //20150505, liliana, LIBST13020, end
                        //20150518, liliana, LIBST13020, begin
                        maskedRekeningRDBUSD.Text = strNoRekUSD;
                        textNamaRekeningRDBUSD.Text = strNameRekUSD;
                        //20150518, liliana, LIBST13020, end
                        //20150728, liliana, LIBST13020, begin
                        maskedRekeningRDBMC.Text = strNoRekMC;
                        textNamaRekeningRDBMC.Text = strNameRekMC;
                        //20150728, liliana, LIBST13020, end
                        textNamaRekeningRDB.Text = strNamaRek;
                        int intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFRDB.Text1);
                        txtUmurRDB.Text = intUmur.ToString();

                        //20220112, sandi, RDN-727, begin
                        //string strCriteria = _strTabName + "#" + cmpsrCIFRDB.Text1;
                        string strCriteria = _strTabName + "#" + cmpsrCIFRDB.Text1 + "#" + cmpsrNoRekRDB.Text2.Trim();
                        //20220112, sandi, RDN-727, end
                        cmpsrProductRDB.SearchDesc = "REKSA_TRXPRODUCT";
                        cmpsrProductRDB.Criteria = strCriteria;

                        cmpsrClientRDB.SearchDesc = "REKSA_TRXCLIENTNEW";
                        //20160829, liliana, LOGEN00196, begin
                        //cmpsrClientRDB.Criteria = cmpsrCIFRDB.Text1.Trim() + "#" + cmpsrProductRDB.Text1.Trim() + "#" + _strTabName;
                        cmpsrClientRDB.Criteria = cmpsrCIFRDB.Text1.Trim() + "#" + cmpsrProductRDB.Text1.Trim() + "#" + _strTabName
                                                  + "#" + cmbTARDB.SelectedIndex.ToString();
                        //20160829, liliana, LOGEN00196, end



                        txtbInputterRDB.Text = cTransaksi.Inputter;
                        cmpsrSellerRDB.Text1 = cTransaksi.Seller;
                        cmpsrSellerRDB.ValidateField();
                        cmpsrWaperdRDB.Text1 = cTransaksi.Waperd;
                        cmpsrWaperdRDB.ValidateField();
                        cmpsrReferentorRDB.Text1 = cTransaksi.Referentor;
                        cmpsrReferentorRDB.ValidateField();

                        //20150618, liliana, LIBST13020, begin
                        //objFormDocument.LoadData(false, 0, false, false, cmpsrCIFRDB.Text1.Trim());
                        objFormDocument.LoadData(false, 0, false, false, cmpsrNoRefRDB.Text1.Trim());
                        //20150618, liliana, LIBST13020, end

                        //20210922, korvi, RDN-674, begin

                        cmpsrNoRekRDB.SearchDesc = "REKSA_CIF_ACCTNO";
                        cmpsrNoRekRDB.Criteria = cmpsrCIFRDB.Text1.Trim() + "#" + cmbTARDB.SelectedIndex.ToString();
                        cmpsrNoRekRDB.Text1 = cTransaksi.SelectedAccNo;
                        cmpsrNoRekRDB.Text2 = cTransaksi.TranCCY;
                        //20220119, sandi, RDN-727, begin
                        //cmpsrNoRekRDB.ValidateField();
                        //20220119, sandi, RDN-727, end
                        //20210922, korvi, RDN-674, end

                        //20221019, Andi, HFUNDING-178, begin
                        string kodeSales, keterangan;
                        GetDataSalesEksekutif(cmpsrNoRefRDB.Text1.Trim(), out kodeSales, out keterangan);
                        textBoxKodeSalesSubsRdb.Text = kodeSales;
                        richTextBoxKeteranganSubsRdb.Text = keterangan;
                        //20221019, Andi, HFUNDING-178, end

                        //20240115, gio, RDN-1115, begin
                        dateTglTransaksiRDB.Value = DateTime.Parse(cTransaksi.dttSubsRDB.Rows[0]["TglTrx"].ToString());
                        //20240115, gio, RDN-1115, end
                        //20170828, liliana, COPOD17271, begin
                    }
                    else
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    //20170828, liliana, COPOD17271, end      
                }
                else
                {
                    MessageBox.Show("No Referensi tidak ditemukan untuk transaksi RDB!");
                    return;
                }

                //20221017, Andhika J, RDN-861, begin
                string IsAsuransi = "";
                ReksaValidateInsuranceRDB(cmpsrCIFRDB.Text1.Trim(), IsAsuransi);
                //20221017, Andhika J, RDN-861, end
            }
            else if (_strTabName == "SWCNONRDB")
            {
                //20150505, liliana, LIBST13020, begin
                //20150715, liliana, LIBST13020, begin
                //if (cmpsrCIFSwc.Text1.Trim().Length == 0)
                //{
                //    MessageBox.Show("No CIF Switching Harus Diisi!");
                //    return;
                //}
                //20150715, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, end
                if (cmpsrNoRefSwc.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("Silahkan Pilih kode Nomor referensi terlebih dahulu!");
                    return;
                }

                DataSet ds = new DataSet();
                OleDbParameter[] odp = new OleDbParameter[3];

                (odp[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar, 20)).Value = cmpsrNoRefSwc.Text1.Trim();
                (odp[1] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                (odp[2] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;

                bool blnResult = ClQ.ExecProc("dbo.ReksaRefreshSwitching", ref odp, out ds);

                if (blnResult)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        //20170828, liliana, COPOD17271, begin
                        if (this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(ds.Tables[0].Rows[0]["CIFNo"].ToString(), strBranch, intClassificationId.ToString(),
                         out ErrMsg, out dsOut))//dapet akses private banking
                        {
                            //20170828, liliana, COPOD17271, end
                            cmpsrKodeKantorSwc.Text1 = ds.Tables[0].Rows[0]["OfficeId"].ToString();
                            cmpsrKodeKantorSwc.ValidateField();
                            //20160816, Elva, LOGEN00191, begin
                            _isCheckingTASwcNonRDB = false;
                            //20160816, Elva, LOGEN00191, end
                            cmpsrCIFSwc.Text1 = ds.Tables[0].Rows[0]["CIFNo"].ToString();
                            cmpsrCIFSwc.Text2 = ds.Tables[0].Rows[0]["CIFName"].ToString();
                            //20160816, Elva, LOGEN00191, begin
                            _isCheckingTASwcNonRDB = true;
                            //20160816, Elva, LOGEN00191, end
                            //20160901, liliana, LOGEN00196, begin
                            //cmbTASwc.SelectedIndex = (int)ds.Tables[0].Rows[0]["TrxTaxAmnesty"];
                            //20160901, liliana, LOGEN00196, end
                            //20240115, gio, RDN-1115, begin
                            dateTglTransaksiSwc.Value = DateTime.Parse(ds.Tables[0].Rows[0]["TranDate"].ToString());
                            //20240115, gio, RDN-1115, end

                            string strShareholderID, strNoRek, strNamaRek, strSID;
                            //20150518, liliana, LIBST13020, begin
                            //20150702, liliana, LIBST13020, begin
                            string strRiskProfile;
                            DateTime dtLastUpdateRiskProfile;
                            //20150702, liliana, LIBST13020, end
                            //GetDataCIF(cmpsrCIFSwc.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                            string strNoRekUSD, strNamaRekUSD;
                            //20150728, liliana, LIBST13020, begin
                            string strNoRekMC, strNamaRekMC;
                            //20150728, liliana, LIBST13020, end
                            GetDataCIF(cmpsrCIFSwc.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                                , out strNoRekUSD, out strNamaRekUSD
                                //20150702, liliana, LIBST13020, begin
                                //20150728, liliana, LIBST13020, begin
                                , out strNoRekMC, out strNamaRekMC
                                //20150728, liliana, LIBST13020, END
                                , out strRiskProfile, out dtLastUpdateRiskProfile
                                //20150702, liliana, LIBST13020, end
                                );
                            //20150518, liliana, LIBST13020, end
                            //20150702, liliana, LIBST13020, begin
                            txtbRiskProfileSwc.Text = strRiskProfile;
                            dtpRiskProfileSwc.Value = dtLastUpdateRiskProfile;
                            //20150702, liliana, LIBST13020, end
                            textSIDSwc.Text = strSID;
                            textShareHolderIdSwc.Text = strShareholderID;
                            textRekeningSwc.Text = strNoRek;
                            //20150505, liliana, LIBST13020, begin
                            maskedRekeningSwc.Text = strNoRek;
                            //20150505, liliana, LIBST13020, end
                            //20150518, liliana, LIBST13020, begin
                            maskedRekeningSwcUSD.Text = strNoRekUSD;
                            textNamaRekeningSwcUSD.Text = strNamaRekUSD;
                            //20150518, liliana, LIBST13020, end
                            //20150728, liliana, LIBST13020, begin
                            maskedRekeningSwcMC.Text = strNoRekMC;
                            textNamaRekeningSwcMC.Text = strNamaRekMC;
                            //20150728, liliana, LIBST13020, END
                            textNamaRekeningSwc.Text = strNamaRek;
                            int intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFSwc.Text1);
                            txtUmurSwc.Text = intUmur.ToString();
                            //20170508, liliana, COPOD17019, begin
                            cmbTASwc.SelectedIndex = (int)ds.Tables[0].Rows[0]["TrxTaxAmnesty"];
                            //20170508, liliana, COPOD17019, end

                            textNoTransaksiSwc.Text = ds.Tables[0].Rows[0]["TranCode"].ToString();
                            dateTglTransaksiSwc.Value = DateTime.Parse(ds.Tables[0].Rows[0]["TranDate"].ToString());

                            //20220112, sandi, RDN-727, begin
                            //string strCriteria = _strTabName + "#" + cmpsrCIFSwc.Text1;
                            string strCriteria = _strTabName + "#" + cmpsrCIFSwc.Text1 + "#" + cmpsrNoRekSwc.Text2.Trim();
                            //20220112, sandi, RDN-727, end

                            cmpsrProductSwcOut.SearchDesc = "REKSA_TRXPRODUCT";
                            cmpsrProductSwcOut.Criteria = strCriteria;
                            cmpsrProductSwcOut.Criteria = _strTabName + "#" + cmpsrCIFSwc.Text1;

                            cmpsrProductSwcOut.Text1 = ds.Tables[0].Rows[0]["ProdCodeSwcOut"].ToString();
                            cmpsrProductSwcOut.ValidateField();

                            //20160829, liliana, LOGEN00196, begin
                            //cmpsrClientSwcOut.Criteria = cmpsrCIFSwc.Text1 + "#" + cmpsrProductSwcOut.Text1 + "#" + _strTabName;
                            cmpsrClientSwcOut.Criteria = cmpsrCIFSwc.Text1 + "#" + cmpsrProductSwcOut.Text1 + "#" + _strTabName
                                                        + "#" + cmbTASwc.SelectedIndex.ToString();
                            //20160829, liliana, LOGEN00196, end

                            cmpsrClientSwcOut.Text1 = ds.Tables[0].Rows[0]["ClientCodeSwcOut"].ToString();
                            cmpsrClientSwcOut.ValidateField();

                            cmpsrProductSwcIn.SearchDesc = "TRXSWITCHIN";
                            cmpsrProductSwcIn.Criteria = cmpsrProductSwcOut.Text1.Trim();

                            cmpsrProductSwcIn.Text1 = ds.Tables[0].Rows[0]["ProdCodeSwcIn"].ToString();
                            cmpsrProductSwcIn.ValidateField();

                            //20160829, liliana, LOGEN00196, begin
                            //cmpsrClientSwcIn.Criteria = cmpsrProductSwcIn[2].ToString() + "#" + cmpsrCIFSwc.Text1.Trim();
                            cmpsrClientSwcIn.Criteria = cmpsrProductSwcIn[2].ToString() + "#" + cmpsrCIFSwc.Text1.Trim()
                                                        + "#" + cmbTASwc.SelectedIndex.ToString();
                            //20160829, liliana, LOGEN00196, end

                            cmpsrClientSwcIn.Text1 = ds.Tables[0].Rows[0]["ClientCodeSwcIn"].ToString();
                            cmpsrClientSwcIn.ValidateField();

                            txtbInputterSwc.Text = ds.Tables[0].Rows[0]["Inputter"].ToString();
                            cmpsrSellerSwc.Text1 = ds.Tables[0].Rows[0]["Seller"].ToString();
                            cmpsrSellerSwc.ValidateField();
                            cmpsrWaperdSwc.Text1 = ds.Tables[0].Rows[0]["Waperd"].ToString();
                            cmpsrWaperdSwc.ValidateField();
                            cmpsrReferentorSwc.Text1 = ds.Tables[0].Rows[0]["Referentor"].ToString();
                            cmpsrReferentorSwc.ValidateField();


                            nispRedempSwc.Value = (decimal)ds.Tables[0].Rows[0]["TranUnit"];
                            checkPhoneOrderSwc.Checked = Convert.ToBoolean(ds.Tables[0].Rows[0]["PhoneOrder"]);
                            checkFeeEditSwc.Checked = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFeeEdit"]);
                            //20220705, sandi, RDN-802, begin
                            //nispMoneyFeeSwc.Value = (decimal)ds.Tables[0].Rows[0]["SwitchingFee"];
                            //20220705, sandi, RDN-802, end
                            nispPercentageFeeSwc.Value = (decimal)ds.Tables[0].Rows[0]["Percentage"];
                            labelFeeCurrencySwc.Text = ds.Tables[0].Rows[0]["TranCCY"].ToString();

                            //20220705, sandi, RDN-802, begin
                            nispMoneyFeeSwc.Value = (decimal)ds.Tables[0].Rows[0]["SwitchingFee"];
                            //20220705, sandi, RDN-802, end

                            objFormDocument.LoadData(false, 0, true, false, cmpsrNoRefSwc.Text1.Trim());

                            //20210922, korvi, RDN-674, begin

                            cmpsrNoRekSwc.SearchDesc = "REKSA_CIF_ACCTNO";
                            cmpsrNoRekSwc.Criteria = cmpsrCIFSwc.Text1.Trim() + "#" + cmbTASwc.SelectedIndex.ToString();
                            cmpsrNoRekSwc.Text1 = ds.Tables[0].Rows[0]["SelectedAccNo"].ToString();
                            cmpsrNoRekSwc.Text2 = ds.Tables[0].Rows[0]["TranCCY"].ToString();
                            //20220119, sandi, RDN-727, begin
                            //cmpsrNoRekSwc.ValidateField();
                            //20220119, sandi, RDN-727, end
                            //20210922, korvi, RDN-674, end

                            //20150325, liliana, LIBST13020, begin
                            //nispJangkaWktSwcRDB.Value = (int)ds.Tables[0].Rows[0]["JangkaWaktu"];
                            //dtJatuhTempoSwcRDB.Value = Convert.ToInt32(Convert.ToDateTime(ds.Tables[0].Rows[0]["JatuhTempo"]).ToString("yyyyMMdd"));
                            //cmbFrekPendebetanSwcRDB.Text = ds.Tables[0].Rows[0]["FrekPendebetan"].ToString();

                            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AutoRedemption"]) == true)
                            //{
                            //    cmbAutoRedempSwcRDB.Text = "YA";
                            //}
                            //else
                            //{
                            //    cmbAutoRedempSwcRDB.Text = "TIDAK";
                            //}

                            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["Asuransi"]) == true)
                            //{
                            //    cmbAsuransiSwcRDB.Text = "YA";
                            //}
                            //else
                            //{
                            //    cmbAsuransiSwcRDB.Text = "TIDAK";
                            //}
                            //20150325, liliana, LIBST13020, end


                            switch (ds.Tables[0].Rows[0]["Status"].ToString().Trim())
                            {
                                case "0":
                                    {
                                        //20150410, liliana, LIBST13020, begin
                                        //labelStatusSwcRDB.Text = "Status : Pending";
                                        labelStatusSwc.Text = "Status : Pending";
                                        //20150410, liliana, LIBST13020, end
                                        break;
                                    }
                                case "1":
                                    {
                                        //20150410, liliana, LIBST13020, begin
                                        //labelStatusSwcRDB.Text = "Status : Authorized";
                                        labelStatusSwc.Text = "Status : Authorized";
                                        //20150410, liliana, LIBST13020, end
                                        break;
                                    }
                                case "2":
                                    {
                                        //20150410, liliana, LIBST13020, begin
                                        //labelStatusSwcRDB.Text = "Status : Rejected";
                                        labelStatusSwc.Text = "Status : Rejected";
                                        //20150410, liliana, LIBST13020, end
                                        break;
                                    }
                                case "3":
                                    {
                                        //20150410, liliana, LIBST13020, begin
                                        //labelStatusSwcRDB.Text = "Status : Reversed";
                                        labelStatusSwc.Text = "Status : Reversed";
                                        //20150410, liliana, LIBST13020, end
                                        break;
                                    }
                            }

                            //20221019, Andi, HFUNDING-178, begin
                            string kodeSales, keterangan;
                            GetDataSalesEksekutif(cmpsrNoRefSwc.Text1.Trim(), out kodeSales, out keterangan);
                            textBoxKodeSalesSwcNonRdb.Text = kodeSales;
                            richTextBoxKeteranganSwcNonRdb.Text = keterangan;
                            //20221019, Andi, HFUNDING-178, end

                            //20170828, liliana, COPOD17271, begin
                        }
                        else
                        {
                            MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        //20170828, liliana, COPOD17271, end  
                    }
                }
            }
            else if (_strTabName == "SWCRDB")
            {
                //20150505, liliana, LIBST13020, begin
                //20150715, liliana, LIBST13020, begin
                //if (cmpsrCIFSwcRDB.Text1.Trim().Length == 0)
                //{
                //    MessageBox.Show("No CIF Switching RDB Harus Diisi!");
                //    return;
                //}
                //20150715, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, end
                if (cmpsrNoRefSwcRDB.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("Silahkan Pilih kode Nomor referensi terlebih dahulu!");
                    return;
                }

                DataSet ds = new DataSet();
                OleDbParameter[] odp = new OleDbParameter[3];

                (odp[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar, 20)).Value = cmpsrNoRefSwcRDB.Text1.Trim();
                (odp[1] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                (odp[2] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;

                bool blnResult = ClQ.ExecProc("ReksaRefreshSwitchingRDB", ref odp, out ds);

                if (blnResult)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        //20170828, liliana, COPOD17271, begin
                        if (this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(ds.Tables[0].Rows[0]["CIFNo"].ToString(), strBranch, intClassificationId.ToString(),
                         out ErrMsg, out dsOut))//dapet akses private banking
                        {
                            //20170828, liliana, COPOD17271, end
                            cmpsrKodeKantorSwcRDB.Text1 = ds.Tables[0].Rows[0]["OfficeId"].ToString();
                            cmpsrKodeKantorSwcRDB.ValidateField();
                            //20160816, Elva, LOGEN00191, begin
                            _isCheckingTASwcRDB = false;
                            //20160816, Elva, LOGEN00191, end
                            cmpsrCIFSwcRDB.Text1 = ds.Tables[0].Rows[0]["CIFNo"].ToString();
                            cmpsrCIFSwcRDB.Text2 = ds.Tables[0].Rows[0]["CIFName"].ToString();
                            //20160816, Elva, LOGEN00191, begin
                            _isCheckingTASwcRDB = true;
                            //20160816, Elva, LOGEN00191, end
                            //20160901, liliana, LOGEN00196, begin
                            //cmbTASwcRDB.SelectedIndex = (int)ds.Tables[0].Rows[0]["TrxTaxAmnesty"];
                            //20160901, liliana, LOGEN00196, end
                            //20240115, gio, RDN-1115, begin
                            dateTglTransaksiSwcRDB.Value = DateTime.Parse(ds.Tables[0].Rows[0]["TranDate"].ToString());
                            //20240115, gio, RDN-1115, end

                            string strShareholderID, strNoRek, strNamaRek, strSID;
                            //20150518, liliana, LIBST13020, begin
                            //20150702, liliana, LIBST13020, begin
                            string strRiskProfile;
                            DateTime dtLastUpdateRiskProfile;
                            //20150702, liliana, LIBST13020, end
                            //GetDataCIF(cmpsrCIFSwcRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                            string strNoRekUSD, strNamaRekUSD;
                            //20150728, liliana, LIBST13020, begin
                            string strNoRekMC, strNamaRekMC;
                            //20150728, liliana, LIBST13020, end
                            GetDataCIF(cmpsrCIFSwcRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                                , out strNoRekUSD, out strNamaRekUSD
                                //20150702, liliana, LIBST13020, begin
                                //20150728, liliana, LIBST13020, begin
                                 , out strNoRekMC, out strNamaRekMC
                                //20150728, liliana, LIBST13020, end
                                , out strRiskProfile, out dtLastUpdateRiskProfile
                                //20150702, liliana, LIBST13020, end
                                );
                            //20150518, liliana, LIBST13020, end
                            //20150702, liliana, LIBST13020, begin
                            txtbRiskProfileSwcRDB.Text = strRiskProfile;
                            dtpRiskProfileSwcRDB.Value = dtLastUpdateRiskProfile;
                            //20150702, liliana, LIBST13020, end
                            textSIDSwcRDB.Text = strSID;
                            textShareHolderIdSwcRDB.Text = strShareholderID;
                            textRekeningSwcRDB.Text = strNoRek;
                            //20150505, liliana, LIBST13020, begin
                            maskedRekeningSwcRDB.Text = strNoRek;
                            //20150505, liliana, LIBST13020, end
                            textNamaRekeningSwcRDB.Text = strNamaRek;
                            //20150518, liliana, LIBST13020, begin
                            maskedRekeningSwcRDBUSD.Text = strNoRekUSD;
                            textNamaRekeningSwcRDBUSD.Text = strNamaRekUSD;
                            //20150518, liliana, LIBST13020, end
                            //20150728, liliana, LIBST13020, begin
                            maskedRekeningSwcRDBMC.Text = strNoRekMC;
                            textNamaRekeningSwcRDBMC.Text = strNamaRekMC;
                            //20150728, liliana, LIBST13020, end
                            int intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFSwcRDB.Text1);
                            txtUmurSwcRDB.Text = intUmur.ToString();
                            //20170508, liliana, COPOD17019, begin
                            cmbTASwcRDB.SelectedIndex = (int)ds.Tables[0].Rows[0]["TrxTaxAmnesty"];
                            //20170508, liliana, COPOD17019, end

                            //20150728, liliana, LIBST13020, begin
                            //textRekeningSwcRDB.Text = ds.Tables[0].Rows[0]["SelectedAccNo"].ToString();
                            ////20150505, liliana, LIBST13020, begin
                            //maskedRekeningSwcRDB.Text = ds.Tables[0].Rows[0]["SelectedAccNo"].ToString();
                            ////20150505, liliana, LIBST13020, end
                            //20150728, liliana, LIBST13020, END
                            textNoTransaksiSwcRDB.Text = ds.Tables[0].Rows[0]["TranCode"].ToString();
                            dateTglTransaksiSwcRDB.Value = DateTime.Parse(ds.Tables[0].Rows[0]["TranDate"].ToString());

                            //20220112, sandi, RDN-727, begin
                            //string strCriteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1;
                            string strCriteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1 + "#" + cmpsrNoRekSwcRDB.Text2.Trim();
                            //20220112, sandi, RDN-727, end

                            cmpsrProductSwcRDBOut.SearchDesc = "REKSA_TRXPRODUCT";
                            cmpsrProductSwcRDBOut.Criteria = strCriteria;


                            cmpsrProductSwcRDBOut.Text1 = ds.Tables[0].Rows[0]["ProdCodeSwcOut"].ToString();
                            cmpsrProductSwcRDBOut.ValidateField();

                            //20160829, liliana, LOGEN00196, begin
                            //cmpsrClientSwcRDBOut.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBOut.Text1 + "#" + _strTabName;
                            cmpsrClientSwcRDBOut.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBOut.Text1 + "#" + _strTabName
                                                             + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                            //20160829, liliana, LOGEN00196, end

                            cmpsrClientSwcRDBOut.Text1 = ds.Tables[0].Rows[0]["ClientCodeSwcOut"].ToString();
                            //20220705, sandi, RDN-802, begin
                            //cmpsrClientSwcRDBOut.ValidateField();
                            //20220705, sandi, RDN-802, end

                            cmpsrProductSwcRDBIn.SearchDesc = "TRXSWITCHIN";
                            cmpsrProductSwcRDBIn.Criteria = cmpsrProductSwcRDBOut.Text1.Trim();

                            cmpsrProductSwcRDBIn.Text1 = ds.Tables[0].Rows[0]["ProdCodeSwcIn"].ToString();
                            cmpsrProductSwcRDBIn.ValidateField();
                            //20150820, liliana, LIBST13020, begin
                            //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName;
                            //20160830, liliana, LOGEN00196, begin
                            //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName + "IN";
                            cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName + "IN"
                                                            + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                            //20160830, liliana, LOGEN00196, end
                            //20150820, liliana, LIBST13020, end

                            cmpsrClientSwcRDBIn.Text1 = ds.Tables[0].Rows[0]["ClientCodeSwcIn"].ToString();
                            cmpsrClientSwcRDBIn.ValidateField();

                            //20220714, sandi, RDN-802, begin
                            //tarik ulang data jika status client RDB sudah 2 (hanya untuk refresh)
                            cmpsrClientSwcRDBOut.Text1 = ds.Tables[0].Rows[0]["ClientCodeSwcOut"].ToString();
                            cmpsrClientSwcRDBOut.Text2 = ds.Tables[0].Rows[0]["CIFName"].ToString();

                            cmpsrClientSwcRDBIn.Text1 = ds.Tables[0].Rows[0]["ClientCodeSwcIn"].ToString();
                            cmpsrClientSwcRDBIn.Text2 = ds.Tables[0].Rows[0]["CIFName"].ToString();

                            decimal OSBalance;
                            int intClientIdSwcOut;

                            intClientIdSwcOut = (int)ds.Tables[0].Rows[0]["ClientIdSwcOut"];
                            OSBalance = GetLatestBalance(intClientIdSwcOut);

                            nispOutstandingUnitSwcRDB.Value = OSBalance;
                            //20220714, sandi, RDN-802, end

                            txtbInputterSwcRDB.Text = ds.Tables[0].Rows[0]["Inputter"].ToString();
                            cmpsrSellerSwcRDB.Text1 = ds.Tables[0].Rows[0]["Seller"].ToString();
                            cmpsrSellerSwcRDB.ValidateField();
                            cmpsrWaperdSwcRDB.Text1 = ds.Tables[0].Rows[0]["Waperd"].ToString();
                            cmpsrWaperdSwcRDB.ValidateField();
                            cmpsrReferentorSwcRDB.Text1 = ds.Tables[0].Rows[0]["Referentor"].ToString();
                            cmpsrReferentorSwcRDB.ValidateField();

                            nispRedempSwcRDB.Value = (decimal)ds.Tables[0].Rows[0]["TranUnit"];
                            //20220805, antoniusfilian, RDN-835, begin
                            if (nispRedempSwcRDB.Value == nispOutstandingUnitSwcRDB.Value)
                            {
                                checkSwcRDBAll.Checked = true;
                            }
                            //20220805, antoniusfilian, RDN-835, end
                            checkPhoneOrderSwcRDB.Checked = Convert.ToBoolean(ds.Tables[0].Rows[0]["PhoneOrder"]);
                            checkFeeEditSwcRDB.Checked = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFeeEdit"]);
                            nispMoneyFeeSwcRDB.Value = (decimal)ds.Tables[0].Rows[0]["SwitchingFee"];
                            nispPercentageFeeSwcRDB.Value = (decimal)ds.Tables[0].Rows[0]["Percentage"];
                            labelFeeCurrencySwcRDB.Text = ds.Tables[0].Rows[0]["TranCCY"].ToString();

                            //20150325, liliana, LIBST13020, begin
                            nispJangkaWktSwcRDB.Value = (int)ds.Tables[0].Rows[0]["JangkaWaktu"];
                            dtJatuhTempoSwcRDB.Value = Convert.ToInt32(Convert.ToDateTime(ds.Tables[0].Rows[0]["JatuhTempo"]).ToString("yyyyMMdd"));
                            cmbFrekPendebetanSwcRDB.Text = ds.Tables[0].Rows[0]["FrekPendebetan"].ToString();

                            if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AutoRedemption"]) == true)
                            {
                                cmbAutoRedempSwcRDB.Text = "YA";
                            }
                            else
                            {
                                cmbAutoRedempSwcRDB.Text = "TIDAK";
                            }

                            if (Convert.ToBoolean(ds.Tables[0].Rows[0]["Asuransi"]) == true)
                            {
                                cmbAsuransiSwcRDB.Text = "YA";
                            }
                            else
                            {
                                cmbAsuransiSwcRDB.Text = "TIDAK";
                            }


                            objFormDocument.LoadData(false, 0, true, false, cmpsrNoRefSwcRDB.Text1.Trim());
                            //20150325, liliana, LIBST13020, end

                            //20210922, korvi, RDN-674, begin

                            cmpsrNoRekSwcRDB.SearchDesc = "REKSA_CIF_ACCTNO";
                            cmpsrNoRekSwcRDB.Criteria = cmpsrCIFSwcRDB.Text1.Trim() + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                            cmpsrNoRekSwcRDB.Text1 = ds.Tables[0].Rows[0]["SelectedAccNo"].ToString(); ;
                            cmpsrNoRekSwcRDB.Text2 = ds.Tables[0].Rows[0]["TranCCY"].ToString();

                            //20220119, sandi, RDN-727, begin
                            //cmpsrNoRekSwcRDB.ValidateField();
                            //20220119, sandi, RDN-727, end
                            //20210922, korvi, RDN-674, end

                            switch (ds.Tables[0].Rows[0]["Status"].ToString().Trim())
                            {
                                case "0":
                                    {
                                        labelStatusSwcRDB.Text = "Status : Pending";
                                        break;
                                    }
                                case "1":
                                    {
                                        labelStatusSwcRDB.Text = "Status : Authorized";
                                        break;
                                    }
                                case "2":
                                    {
                                        labelStatusSwcRDB.Text = "Status : Rejected";
                                        break;
                                    }
                                case "3":
                                    {
                                        labelStatusSwcRDB.Text = "Status : Reversed";
                                        break;
                                    }
                            }
                            //20221019, Andi, HFUNDING-178, begin
                            string kodeSales, keterangan;
                            GetDataSalesEksekutif(cmpsrNoRefSwcRDB.Text1.Trim(), out kodeSales, out keterangan);
                            textBoxKodeSalesSwcRdb.Text = kodeSales;
                            richTextBoxKeteranganSwcRdb.Text = keterangan;
                            //20221019, Andi, HFUNDING-178, end
                            //20170828, liliana, COPOD17271, begin
                        }
                        else
                        {
                            MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        //20170828, liliana, COPOD17271, end  
                    }
                }

            }
            else if (_strTabName == "BOOK")
            {
                //20150505, liliana, LIBST13020, begin
                //20150715, liliana, LIBST13020, begin
                //if (cmpsrCIFBooking.Text1.Trim().Length == 0)
                //{
                //    MessageBox.Show("No CIF Booking Harus Diisi!");
                //    return;
                //}
                //20150715, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, end
                if (cmpsrNoRefBooking.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("Silahkan Pilih kode Nomor referensi terlebih dahulu!");
                    return;
                }

                DataSet dsRefresh;

                OleDbParameter[] dbParam = new OleDbParameter[3];

                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcRefID", System.Data.OleDb.OleDbType.VarChar, 20);
                dbParam[0].Value = cmpsrNoRefBooking.Text1.Trim();
                dbParam[0].Direction = System.Data.ParameterDirection.Input;

                dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
                dbParam[1].Value = intNIK;
                dbParam[1].Direction = System.Data.ParameterDirection.Input;

                dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
                dbParam[2].Value = strGuid;
                dbParam[2].Direction = System.Data.ParameterDirection.Input;

                bool blnResult = ClQ.ExecProc("ReksaRefreshBookingNew", ref dbParam, out dsRefresh);

                if (blnResult == true)
                {
                    if (dsRefresh.Tables[0].Rows.Count > 0)
                    {
                        //20170828, liliana, COPOD17271, begin
                        if (this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(dsRefresh.Tables[0].Rows[0]["CIFNo"].ToString(), strBranch, intClassificationId.ToString(),
                         out ErrMsg, out dsOut))//dapet akses private banking
                        {
                            //20170828, liliana, COPOD17271, end
                            cmpsrKodeKantorBooking.Text1 = dsRefresh.Tables[0].Rows[0]["OfficeId"].ToString();
                            cmpsrKodeKantorBooking.ValidateField();
                            //20160816, Elva, LOGEN00191, begin
                            _isCheckingTABook = false;
                            //20160816, Elva, LOGEN00191, end
                            cmpsrCIFBooking.Text1 = dsRefresh.Tables[0].Rows[0]["CIFNo"].ToString();
                            cmpsrCIFBooking.Text2 = dsRefresh.Tables[0].Rows[0]["CIFName"].ToString();
                            //20160816, Elva, LOGEN00191, begin
                            _isCheckingTABook = true;
                            //20160816, Elva, LOGEN00191, end
                            //20160901, liliana, LOGEN00196, begin
                            //cmbTABook.SelectedIndex = (int)dsRefresh.Tables[0].Rows[0]["TrxTaxAmnesty"];
                            //20160901, liliana, LOGEN00196, end
                            //20240115, gio, RDN-1115, begin
                            dateTglTransaksiBooking.Value = DateTime.Parse(dsRefresh.Tables[0].Rows[0]["BookingDate"].ToString());
                            //20240115, gio, RDN-1115, end

                            string strShareholderID, strNoRek, strNamaRek, strSID;
                            //20150518, liliana, LIBST13020, begin
                            //20150702, liliana, LIBST13020, begin
                            string strRiskProfile;
                            DateTime dtLastUpdateRiskProfile;
                            //20150702, liliana, LIBST13020, end
                            //GetDataCIF(cmpsrCIFBooking.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                            string strNoRekUSD, strNamaRekUSD;
                            //201507028, liliana, LIBST13020, begin
                            string strNoRekMC, strNamaRekMC;
                            //201507028, liliana, LIBST13020, end
                            GetDataCIF(cmpsrCIFBooking.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                                , out strNoRekUSD, out strNamaRekUSD
                                //20150702, liliana, LIBST13020, begin
                                , out strNoRekMC, out strNamaRekMC
                                //201507028, liliana, LIBST13020, end
                                //201507028, liliana, LIBST13020, begin
                                , out strRiskProfile, out dtLastUpdateRiskProfile
                                //20150702, liliana, LIBST13020, end
                                );
                            //20150518, liliana, LIBST13020, end
                            //20150702, liliana, LIBST13020, begin
                            txtbRiskProfileBooking.Text = strRiskProfile;
                            dtpRiskProfileBooking.Value = dtLastUpdateRiskProfile;
                            //20150702, liliana, LIBST13020, end
                            textSIDBooking.Text = strSID;
                            textShareHolderIdBooking.Text = strShareholderID;
                            textRekeningBooking.Text = strNoRek;
                            //20150505, liliana, LIBST13020, begin
                            maskedRekeningBooking.Text = strNoRek;
                            //20150505, liliana, LIBST13020, end
                            //20150518, liliana, LIBST13020, begin
                            maskedRekeningBookingUSD.Text = strNoRekUSD;
                            textNamaRekeningBookingUSD.Text = strNamaRekUSD;
                            //20150518, liliana, LIBST13020, end
                            //201507028, liliana, LIBST13020, begin
                            maskedRekeningBookingMC.Text = strNoRekMC;
                            textNamaRekeningBookingMC.Text = strNamaRekMC;
                            //201507028, liliana, LIBST13020, end
                            textNamaRekeningBooking.Text = strNamaRek;
                            int intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFBooking.Text1);
                            txtUmurBooking.Text = intUmur.ToString();
                            //20170508, liliana, COPOD17019, begin
                            cmbTABook.SelectedIndex = (int)dsRefresh.Tables[0].Rows[0]["TrxTaxAmnesty"];
                            //20170508, liliana, COPOD17019, end

                            //20220112, sandi, RDN-727, begin
                            //string strCriteria = _strTabName + "#" + cmpsrCIFBooking.Text1;
                            string strCriteria = _strTabName + "#" + cmpsrCIFBooking.Text1 + "#" + cmpsrNoRekBooking.Text2.Trim();
                            //20220112, sandi, RDN-727, end

                            cmpsrProductBooking.SearchDesc = "REKSA_TRXPRODUCT";
                            cmpsrProductBooking.Criteria = strCriteria;

                            textNoTransaksiBooking.Text = dsRefresh.Tables[0].Rows[0]["BookingCode"].ToString();
                            cmpsrProductBooking.Text1 = dsRefresh.Tables[0].Rows[0]["ProdCode"].ToString();
                            cmpsrProductBooking.ValidateField();
                            cmpsrClientBooking.Text1 = dsRefresh.Tables[0].Rows[0]["ClientCode"].ToString();
                            cmpsrClientBooking.ValidateField();
                            cmpsrCurrBooking.Text1 = dsRefresh.Tables[0].Rows[0]["BookingCCY"].ToString();
                            cmpsrCurrBooking.ValidateField();
                            txtbInputterBooking.Text = dsRefresh.Tables[0].Rows[0]["Inputter"].ToString();
                            cmpsrSellerBooking.Text1 = dsRefresh.Tables[0].Rows[0]["Seller"].ToString();
                            cmpsrSellerBooking.ValidateField();
                            cmpsrWaperdBooking.Text1 = dsRefresh.Tables[0].Rows[0]["Waperd"].ToString();
                            cmpsrWaperdBooking.ValidateField();
                            cmpsrReferentorBooking.Text1 = dsRefresh.Tables[0].Rows[0]["Referentor"].ToString();
                            cmpsrReferentorBooking.ValidateField();

                            dateTglTransaksiBooking.Value = DateTime.Parse(dsRefresh.Tables[0].Rows[0]["BookingDate"].ToString());
                            nispMoneyNomBooking.Value = (decimal)dsRefresh.Tables[0].Rows[0]["BookingAmt"];
                            checkPhoneOrderBooking.Checked = Convert.ToBoolean(dsRefresh.Tables[0].Rows[0]["PhoneOrder"]);
                            checkFeeEditBooking.Checked = Convert.ToBoolean(dsRefresh.Tables[0].Rows[0]["IsFeeEdit"]);
                            _ComboJenisBooking.SelectedIndex = (int.Parse(dsRefresh.Tables[0].Rows[0]["JenisPerhitunganFee"].ToString()));
                            nispMoneyFeeBooking.Value = (decimal)dsRefresh.Tables[0].Rows[0]["SubcFee"];
                            nispPercentageFeeBooking.Value = (decimal)dsRefresh.Tables[0].Rows[0]["PercentageFee"];
                            labelFeeCurrencyBooking.Text = dsRefresh.Tables[0].Rows[0]["BookingCCY"].ToString();

                            objFormDocument.LoadData(false, 0, false, true, cmpsrNoRefBooking.Text1.Trim());

                            //20210922, korvi, RDN-674, begin
                            cmpsrNoRekBooking.SearchDesc = "REKSA_CIF_ACCTNO";
                            cmpsrNoRekBooking.Criteria = cmpsrCIFBooking.Text1.Trim() + "#" + cmbTABook.SelectedIndex.ToString();
                            cmpsrNoRekBooking.Text1 = dsRefresh.Tables[0].Rows[0]["SelectedAccNo"].ToString();
                            cmpsrNoRekBooking.Text2 = dsRefresh.Tables[0].Rows[0]["BookingCCY"].ToString();
                            //20220119, sandi, RDN-727, begin
                            //cmpsrNoRekBooking.ValidateField();
                            //20220119, sandi, RDN-727, end
                            //20210922, korvi, RDN-674, end

                            switch (dsRefresh.Tables[0].Rows[0]["Status"].ToString().Trim())
                            {
                                case "0":
                                    {
                                        labelStatusBook.Text = "Status : Pending";
                                        break;
                                    }
                                case "1":
                                    {
                                        labelStatusBook.Text = "Status : Authorized";
                                        break;
                                    }
                                case "2":
                                    {
                                        labelStatusBook.Text = "Status : Rejected";
                                        break;
                                    }
                                case "3":
                                    {
                                        labelStatusBook.Text = "Status : Reversed";
                                        break;
                                    }
                            }
                            //20221019, Andi, HFUNDING-178, begin
                            string kodeSales, keterangan;
                            GetDataSalesEksekutif(cmpsrNoRefBooking.Text1.Trim(), out kodeSales, out keterangan);
                            textBoxKodeSalesBook.Text = kodeSales;
                            richTextBoxKeteranganBook.Text = keterangan;
                            //20221019, Andi, HFUNDING-178, end
                            //20170828, liliana, COPOD17271, begin
                        }
                        else
                        {
                            MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        //20170828, liliana, COPOD17271, end  
                    }
                }
            }
            //20150724, liliana, LIBST13020, begin
            _intType = 0;
            DisableAllForm(false);
            subResetToolBar();
            //20150724, liliana, LIBST13020, end
        }

        private void subNew()
        {
            //20240912, Lely, RDN-1182, begin
            _iMode = 1;
            //20240912, Lely, RDN-1182, end
            _intType = 1;
            ResetForm();
            DisableAllForm(true);
            subResetToolBar();
            SetDefaultValue();

            checkFullAmtSubs.Checked = true;
            cmpsrCIFSubs.Enabled = true;
            cmpsrCIFRedemp.Enabled = true;
            cmpsrCIFRDB.Enabled = true;
            cmpsrCIFSwc.Enabled = true;
            cmpsrCIFSwcRDB.Enabled = true;
            cmpsrCIFBooking.Enabled = true;
            string strCriteria;
            strCriteria = "";
            //20150410, liliana, LIBST13020, begin
            GetKodeKantor();
            cTransaksi.ClearData();
            //20150410, liliana, LIBST13020, end
            //20160509, Elva, CSODD16117, begin
            SetEnableOfficeId(strBranch);
            //20160509, Elva, CSODD16117, end

            if (_strTabName == "SUBS")
            {
                //20150505, liliana, LIBST13020, begin
                DisableFormTrxSubs(true);
                //20150505, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, begin
                //cmpsrCIFSubs.SearchDesc = "CUSTOMER_ID";
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFSubs.SearchDesc = "REKSA_CIF";
                cmpsrCIFSubs.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFSubs.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFSubs.Text1 + "#" + cmpsrNoRekSubs.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrClientSubs.Enabled = false;
                IsSubsNew = true;
                cmpsrProductSubs.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductSubs.Criteria = strCriteria;

                buttonAddSubs.Enabled = true;
                //20150818, liliana, LIBST13020, begin
                //buttonEditSubs.Enabled = true;
                buttonEditSubs.Enabled = false;
                //20150818, liliana, LIBST13020, end
                //20150414, liliana, LIBST13020, begin
                dateTglTransaksiSubs.Value = DateTime.Today;
                //20150414, liliana, LIBST13020, end

                //20220802, Lita, RDN-825, begin
                _ComboJenisSubs.SelectedIndex = 0;
                //20220802, Lita, RDN-825, end

            }
            else if (_strTabName == "REDEMP")
            {
                //20150505, liliana, LIBST13020, begin
                DisableFormTrxRedemp(true);
                //20150505, liliana, LIBST13020, end
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFRedemp.SearchDesc = "REKSA_CIF";
                cmpsrCIFRedemp.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                IsRedempAll = false;
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFRedemp.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFRedemp.Text1 + "#" + cmpsrNoRekRedemp.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrProductRedemp.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductRedemp.Criteria = strCriteria;

                buttonAddRedemp.Enabled = true;
                //20150818, liliana, LIBST13020, begin
                //buttonEditRedemp.Enabled = true;
                buttonEditRedemp.Enabled = false;
                //20150818, liliana, LIBST13020, end
                //20150414, liliana, LIBST13020, begin
                dateTglTransaksiRedemp.Value = DateTime.Today;
                //20150414, liliana, LIBST13020, end

                //20220802, Lita, RDN-825, begin
                _ComboJenisRedemp.SelectedIndex = 0;
                //20220802, Lita, RDN-825, end

            }
            else if (_strTabName == "SUBSRDB")
            {
                //20150505, liliana, LIBST13020, begin
                DisableFormTrxRDB(true);
                //20150505, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, begin
                //cmpsrCIFRDB.SearchDesc = "CUSTOMER_ID";
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFRDB.SearchDesc = "REKSA_CIF";
                cmpsrCIFRDB.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFRDB.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFRDB.Text1 + "#" + cmpsrNoRekRDB.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrProductRDB.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductRDB.Criteria = strCriteria;
                cmpsrClientRDB.Enabled = false;

                buttonAddRDB.Enabled = true;
                //20150818, liliana, LIBST13020, begin
                //buttonEditRDB.Enabled = true;
                buttonEditRDB.Enabled = false;
                //20150818, liliana, LIBST13020, end
                //20150414, liliana, LIBST13020, begin
                dateTglTransaksiRDB.Value = DateTime.Today;
                //20150414, liliana, LIBST13020, end

                //20200408, Lita, RDN-88, begin
                dtTglDebetRDB.Value = DateTime.Today;
                cmbFrekDebetMethodRDB.SelectedValue = "M";
                //20200408, Lita, RDN-88, end

                //20220802, Lita, RDN-825, begin
                _ComboJenisRDB.SelectedIndex = 0;
                //20220802, Lita, RDN-825, end
                //20221017, Andhika J, RDN-861, begin
                string IsAsuransi = "";
                ReksaValidateInsuranceRDB(cmpsrCIFRDB.Text1.Trim(), IsAsuransi);
                //20221017, Andhika J, RDN-861, end
            }
            else if (_strTabName == "BOOK")
            {
                //20150706, liliana, LIBST13020, begin
                //cmpsrCIFBooking.SearchDesc = "CUSTOMER_ID";
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFBooking.SearchDesc = "REKSA_CIF";
                cmpsrCIFBooking.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFBooking.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFBooking.Text1 + "#" + cmpsrNoRekBooking.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrProductBooking.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductBooking.Criteria = strCriteria;
                //20150414, liliana, LIBST13020, begin
                dateTglTransaksiBooking.Value = DateTime.Today;
                //20150414, liliana, LIBST13020, end

                //20220802, Lita, RDN-825, begin
                _ComboJenisBooking.SelectedIndex = 0;
                //20220802, Lita, RDN-825, end
            }
            else if (_strTabName == "SWCNONRDB")
            {
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFSwc.SearchDesc = "REKSA_CIF";
                cmpsrCIFSwc.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20220111, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFSwc.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFSwc.Text1 + "#" + cmpsrNoRekSwc.Text2.Trim();
                //20220111, sandi, RDN-727, end

                cmpsrProductSwcOut.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductSwcOut.Criteria = strCriteria;

                cmpsrProductSwcIn.SearchDesc = "TRXSWITCHIN";
                cmpsrProductSwcIn.Criteria = cmpsrProductSwcOut.Text1.Trim();
                //20150414, liliana, LIBST13020, begin
                dateTglTransaksiSwc.Value = DateTime.Today;
                //20150414, liliana, LIBST13020, end
            }
            else if (_strTabName == "SWCRDB")
            {
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF";
                cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1 + "#" + cmpsrNoRekSwcRDB.Text2.Trim();
                //20220112, sandi, RDN-727, end

                cmpsrProductSwcRDBOut.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductSwcRDBOut.Criteria = strCriteria;

                cmpsrProductSwcRDBIn.SearchDesc = "TRXSWITCHIN";
                cmpsrProductSwcRDBIn.Criteria = cmpsrProductSwcRDBOut.Text1.Trim();
                //20150414, liliana, LIBST13020, begin
                dateTglTransaksiSwcRDB.Value = DateTime.Today;
                //20150414, liliana, LIBST13020, end
            }


        }

        private void subUpdate()
        {
            //20240912, Lely, RDN-1182, begin
            _iMode = 2;
            //20240912, Lely, RDN-1182, end
            //20150505, liliana, LIBST13020, begin
            if (_strTabName == "SUBS")
            {
                if (cmpsrCIFSubs.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No CIF Subscription Harus Diisi!");
                    return;
                }

                if (cmpsrNoRefSubs.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No Referensi Subscription Harus Diisi!");
                    return;
                }
                //20150723, liliana, LIBST13020, begin
                if (!CheckCIF(cmpsrCIFSubs.Text1.Trim()))
                {
                    return;
                }
                //20150723, liliana, LIBST13020, end
            }
            else if (_strTabName == "REDEMP")
            {
                if (cmpsrCIFRedemp.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No CIF Redemption Harus Diisi!");
                    return;
                }

                if (cmpsrNoRefRedemp.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No Referensi Redemption Harus Diisi!");
                    return;
                }
                //20150723, liliana, LIBST13020, begin
                if (!CheckCIF(cmpsrCIFRedemp.Text1.Trim()))
                {
                    return;
                }
                //20150723, liliana, LIBST13020, end
            }
            else if (_strTabName == "SUBSRDB")
            {
                if (cmpsrCIFRDB.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No CIF RDB Harus Diisi!");
                    return;
                }

                if (cmpsrNoRefRDB.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No Referensi RDB Harus Diisi!");
                    return;
                }
                //20150723, liliana, LIBST13020, begin
                if (!CheckCIF(cmpsrCIFRDB.Text1.Trim()))
                {
                    return;
                }
                //20150723, liliana, LIBST13020, end
            }
            else if (_strTabName == "BOOK")
            {
                if (cmpsrCIFBooking.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No CIF Booking Harus Diisi!");
                    return;
                }

                if (cmpsrNoRefBooking.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No Referensi Booking Harus Diisi!");
                    return;
                }
                //20150723, liliana, LIBST13020, begin
                if (!CheckCIF(cmpsrCIFBooking.Text1.Trim()))
                {
                    return;
                }
                //20150723, liliana, LIBST13020, end
            }
            else if (_strTabName == "SWCNONRDB")
            {
                if (cmpsrCIFSwc.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No CIF Switching Harus Diisi!");
                    return;
                }

                if (cmpsrNoRefSwc.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No Referensi Switching Harus Diisi!");
                    return;
                }
                //20150723, liliana, LIBST13020, begin
                if (!CheckCIF(cmpsrCIFSwc.Text1.Trim()))
                {
                    return;
                }
                //20150723, liliana, LIBST13020, end
            }
            //20150820, liliana, LIBST13020, begin
            //else if (_strTabName == "SWCNONRDB")
            else if (_strTabName == "SWCRDB")
            //20150820, liliana, LIBST13020, end
            {
                if (cmpsrCIFSwcRDB.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No CIF Switching RDB Harus Diisi!");
                    return;
                }

                if (cmpsrNoRefSwcRDB.Text1.Trim().Length == 0)
                {
                    MessageBox.Show("No Referensi Switching RDB Harus Diisi!");
                    return;
                }
                //20150723, liliana, LIBST13020, begin
                if (!CheckCIF(cmpsrCIFSwcRDB.Text1.Trim()))
                {
                    return;
                }
                //20150723, liliana, LIBST13020, end
            }
            //20150505, liliana, LIBST13020, end
            //20150710, liliana, LIBST13020, begin
            subRefresh();
            //20150710, liliana, LIBST13020, end
            _intType = 2;
            DisableAllForm(false);
            subResetToolBar();
            string strCriteria;
            strCriteria = "";

            if (_strTabName == "SUBS")
            {
                //20150505, liliana, LIBST13020, begin
                DisableFormTrxSubs(false);
                //20150505, liliana, LIBST13020, end

                //20150706, liliana, LIBST13020, begin
                //cmpsrCIFSubs.SearchDesc = "CUSTOMER_ID";
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFSubs.SearchDesc = "REKSA_CIF";
                cmpsrCIFSubs.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFSubs.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFSubs.Text1 + "#" + cmpsrNoRekSubs.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrProductSubs.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductSubs.Criteria = strCriteria;

                buttonAddSubs.Enabled = false;
                buttonEditSubs.Enabled = true;
                //20150617, liliana, LIBST13020, begin
                cmpsrCIFSubs.Enabled = false;
                cmpsrNoRefSubs.Enabled = false;
                //20150617, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                cmbTASubs.Enabled = false;
                //20160829, liliana, LOGEN00196, end

                //20150505, liliana, LIBST13020, begin
                //if (checkFeeEditSubs.Checked)
                //{
                //    _ComboJenisSubs.Enabled = true;
                //    nispMoneyFeeSubs.Enabled = true;
                //    nispPercentageFeeSubs.Enabled = false;
                //}

                //if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSubs.Text1))
                //{
                //    checkPhoneOrderSubs.Enabled = false;
                //    checkPhoneOrderSubs.Checked = false;
                //}
                //else
                //{
                //    checkPhoneOrderSubs.Enabled = true;
                //}
                //20150505, liliana, LIBST13020, end
                //20221017, Andi, HFUNDING-178, begin
                textBoxKodeSalesSubs.Enabled = true;
                richTextBoxKeteranganSubs.Enabled = true;
                //20221017, Andi, HFUNDING-178, end
            }
            else if (_strTabName == "REDEMP")
            {
                //20150505, liliana, LIBST13020, begin
                DisableFormTrxRedemp(false);
                //20150505, liliana, LIBST13020, end
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFRedemp.SearchDesc = "REKSA_CIF";
                cmpsrCIFRedemp.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFRedemp.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFRedemp.Text1 + "#" + cmpsrNoRekRedemp.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrProductRedemp.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductRedemp.Criteria = strCriteria;

                buttonAddRedemp.Enabled = false;
                buttonEditRedemp.Enabled = true;
                //20150617, liliana, LIBST13020, begin
                cmpsrCIFRedemp.Enabled = false;
                cmpsrNoRefRedemp.Enabled = false;
                //20150617, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                cmbTARedemp.Enabled = false;
                //20160829, liliana, LOGEN00196, end

                //20150505, liliana, LIBST13020, begin
                //if (checkFeeEditRedemp.Checked)
                //{
                //    _ComboJenisRedemp.Enabled = true;
                //    nispMoneyFeeRedemp.Enabled = true;
                //    nispPercentageFeeRedemp.Enabled = false;
                //}

                //if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRedemp.Text1))
                //{
                //    checkPhoneOrderRedemp.Enabled = false;
                //    checkPhoneOrderRedemp.Checked = false;
                //}
                //else
                //{
                //    checkPhoneOrderRedemp.Enabled = true;
                //}
                //20150505, liliana, LIBST13020, end
                //20221017, Andi, HFUNDING-178, begin
                textBoxKodeSalesRedemp.Enabled = true;
                richTextBoxKeteranganRedemp.Enabled = true;
                //20221017, Andi, HFUNDING-178, end
            }
            else if (_strTabName == "SUBSRDB")
            {
                //20150505, liliana, LIBST13020, begin
                DisableFormTrxRDB(false);
                //20150505, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, begin
                //cmpsrCIFRDB.SearchDesc = "CUSTOMER_ID";
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFRDB.SearchDesc = "REKSA_CIF";
                cmpsrCIFRDB.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFRDB.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFRDB.Text1 + "#" + cmpsrNoRekRDB.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrProductRDB.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductRDB.Criteria = strCriteria;

                buttonAddRDB.Enabled = false;
                buttonEditRDB.Enabled = true;
                //20150617, liliana, LIBST13020, begin
                cmpsrCIFRDB.Enabled = false;
                cmpsrNoRefRDB.Enabled = false;
                //20150617, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                cmbTARDB.Enabled = false;
                //20160829, liliana, LOGEN00196, end



                //20150505, liliana, LIBST13020, begin
                //if (checkFeeEditRDB.Checked)
                //{
                //    _ComboJenisRDB.Enabled = true;
                //    nispMoneyFeeRDB.Enabled = true;
                //    nispPercentageFeeRDB.Enabled = false;
                //}

                //if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRDB.Text1))
                //{
                //    checkPhoneOrderRDB.Enabled = false;
                //    checkPhoneOrderRDB.Checked = false;
                //}
                //else
                //{
                //    checkPhoneOrderRDB.Enabled = true;
                //}
                //20150505, liliana, LIBST13020, end
                //20221017, Andi, HFUNDING-178, begin
                textBoxKodeSalesSubsRdb.Enabled = true;
                richTextBoxKeteranganSubsRdb.Enabled = true;
                //20221017, Andi, HFUNDING-178, end
            }
            else if (_strTabName == "BOOK")
            {
                //20150709, liliana, LIBST13020, begin
                DisableFormBooking(false);
                //20150709, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, begin
                //cmpsrCIFBooking.SearchDesc = "CUSTOMER_ID";
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFBooking.SearchDesc = "REKSA_CIF";
                cmpsrCIFBooking.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20150706, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFBooking.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFBooking.Text1 + "#" + cmpsrNoRekBooking.Text2.Trim();
                //20220112, sandi, RDN-727, end
                cmpsrProductBooking.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductBooking.Criteria = strCriteria;

                if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFBooking.Text1))
                {
                    checkPhoneOrderBooking.Enabled = false;
                    checkPhoneOrderBooking.Checked = false;
                }
                else
                {
                    checkPhoneOrderBooking.Enabled = true;
                }
                //20221017, Andi, HFUNDING-178, begin
                textBoxKodeSalesBook.Enabled = true;
                richTextBoxKeteranganBook.Enabled = true;
                //20221017, Andi, HFUNDING-178, end
            }
            else if (_strTabName == "SWCNONRDB")
            {
                //20150710, liliana, LIBST13020, begin
                DisableFormSwc(false);
                //20150710, liliana, LIBST13020, end
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFSwc.SearchDesc = "REKSA_CIF";
                cmpsrCIFSwc.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20220111, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFSwc.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFSwc.Text1 + "#" + cmpsrNoRekSwc.Text2.Trim();
                //20220111, sandi, RDN-727, end

                cmpsrProductSwcOut.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductSwcOut.Criteria = strCriteria;

                cmpsrProductSwcIn.SearchDesc = "TRXSWITCHIN";
                cmpsrProductSwcIn.Criteria = cmpsrProductSwcOut.Text1.Trim();

                if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSwc.Text1))
                {
                    checkPhoneOrderSwc.Enabled = false;
                    checkPhoneOrderSwc.Checked = false;
                }
                else
                {
                    checkPhoneOrderSwc.Enabled = true;
                }
                //20221017, Andi, HFUNDING-178, begin
                textBoxKodeSalesSwcNonRdb.Enabled = true;
                richTextBoxKeteranganSwcNonRdb.Enabled = true;
                //20221017, Andi, HFUNDING-178, end

            }
            else if (_strTabName == "SWCRDB")
            {
                //20150710, liliana, LIBST13020, begin
                DisableFormSwcRDB(false);
                //20150710, liliana, LIBST13020, end
                //20150723, liliana, LIBST13020, begin
                //cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF";
                cmpsrCIFSwcRDB.SearchDesc = "REKSA_CIF_ALL";
                //20150723, liliana, LIBST13020, end
                //20220112, sandi, RDN-727, begin
                //strCriteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1;
                strCriteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1 + "#" + cmpsrNoRekSwcRDB.Text2.Trim();
                //20220112, sandi, RDN-727, end

                cmpsrProductSwcRDBOut.SearchDesc = "REKSA_TRXPRODUCT";
                cmpsrProductSwcRDBOut.Criteria = strCriteria;

                cmpsrProductSwcRDBIn.SearchDesc = "TRXSWITCHIN";
                cmpsrProductSwcRDBIn.Criteria = cmpsrProductSwcRDBOut.Text1.Trim();

                if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSwcRDB.Text1))
                {
                    checkPhoneOrderSwcRDB.Enabled = false;
                    checkPhoneOrderSwcRDB.Checked = false;
                }
                else
                {
                    checkPhoneOrderSwcRDB.Enabled = true;
                }
                //20150820, liliana, LIBST13020, begin

                DataSet dsRDBSwitchOut;
                dsRDBSwitchOut = GetDataRDB(cmpsrClientSwcRDBOut.Text1);
                string DoneDebet;
                DoneDebet = "";

                if (dsRDBSwitchOut.Tables[0].Rows.Count > 0)
                {
                    DoneDebet = dsRDBSwitchOut.Tables[0].Rows[0]["IsDoneDebet"].ToString();
                }

                //20150820, liliana, LIBST13020, begin
                //if (nispJangkaWktSwcRDB.Value == 0)
                if ((nispJangkaWktSwcRDB.Value == 0) && (DoneDebet == "1"))
                //20150820, liliana, LIBST13020, end
                {
                    nispRedempSwcRDB.Enabled = true;
                    //20220805, antoniusfilian, RDN-835, begin
                    checkSwcRDBAll.Enabled = true;
                    //20220805, antoniusfilian, RDN-835, end
                    //20150820, liliana, LIBST13020, begin
                    //20160830, liliana, LOGEN00196, begin
                    //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + "SWCNONRDB";
                    cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + "SWCNONRDB"
                                                    + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                    //20160830, liliana, LOGEN00196, end
                    //20150820, liliana, LIBST13020, end
                    //20150820, liliana, LIBST13020, begin
                    IsSwitchingRDBSebagian = true;
                    //20150820, liliana, LIBST13020, end
                }
                else
                {
                    nispRedempSwcRDB.Enabled = false;
                    nispRedempSwcRDB.Value = nispOutstandingUnitSwcRDB.Value;
                    //20220805, antoniusfilian, RDN-835, begin
                    checkSwcRDBAll.Enabled = false;
                    checkSwcRDBAll.Checked = true;
                    //20220805, antoniusfilian, RDN-835, end
                    //20150820, liliana, LIBST13020, begin
                    //20160830, liliana, LOGEN00196, begin
                    //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName;
                    cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName
                                                    + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                    //20160830, liliana, LOGEN00196, end
                    //20150820, liliana, LIBST13020, end
                    //20150820, liliana, LIBST13020, begin
                    IsSwitchingRDBSebagian = false;
                    //20150820, liliana, LIBST13020, end

                }
                //20150820, liliana, LIBST13020, end
                //20221017, Andi, HFUNDING-178, begin
                textBoxKodeSalesSwcRdb.Enabled = true;
                richTextBoxKeteranganSwcRdb.Enabled = true;
                //20221017, Andi, HFUNDING-178, end

            }
        }

        private void CekRiskProfile(string CIFNo, DateTime TanggalTransaksi, out string RiskProfile, out 
                DateTime LastUpdateRiskProfile,
            //20170825, liliana, COPOD17271, begin
                 out DateTime ExpRiskProfile,
            //20170825, liliana, COPOD17271, end
                out TimeSpan diff)
        {
            DataSet Result;
            OleDbParameter[] Param = new OleDbParameter[1];
            RiskProfile = "";
            LastUpdateRiskProfile = DateTime.Today;
            diff = TimeSpan.Zero;
            //20170825, liliana, COPOD17271, begin
            int ExpRiskProfileYear = 1;
            ExpRiskProfile = DateTime.Today;
            //20170825, liliana, COPOD17271, end

            (Param[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = CIFNo;

            //20150615, liliana, LIBST13020, begin
            ClQ.TimeOut = 6000;
            //20150615, liliana, LIBST13020, end
            if (ClQ.ExecProc("dbo.ReksaGetRiskProfile", ref Param, out Result))
            {
                if (Result.Tables[0].Rows.Count >= 0)
                {
                    RiskProfile = Result.Tables[0].Rows[0]["RiskProfile"].ToString();
                    LastUpdateRiskProfile = (DateTime)Result.Tables[0].Rows[0]["LastUpdate"];
                    diff = TanggalTransaksi.Subtract(LastUpdateRiskProfile);
                    //20170825, liliana, COPOD17271, begin
                    ExpRiskProfileYear = (int)Result.Tables[0].Rows[0]["ExpRiskProfileYear"];
                    ExpRiskProfile = LastUpdateRiskProfile.AddYears(ExpRiskProfileYear);
                    //20170825, liliana, COPOD17271, end
                }
            }
        }

        //20210120, julio, RDN-410, begin
        private void CekElecAddress(string CIFNo, out string Warning)
        {
            DataSet Result;
            OleDbParameter[] Param = new OleDbParameter[2];
            Warning = "";
            (Param[0] = new OleDbParameter("@cType", OleDbType.VarChar, 13)).Value = "MAIL";
            (Param[1] = new OleDbParameter("@nCIFNo", OleDbType.VarChar, 13)).Value = CIFNo;
            ClQ.TimeOut = 6000;
            Boolean Check = ClQ.ExecProc("dbo.ReksaGetWarningElec", ref Param, out Result);

            if (Check)
            {
                if (Result.Tables[0].Rows.Count >= 0)
                {
                    Warning = Result.Tables[0].Rows[0][0].ToString();
                }
            }
            else
            {
                MessageBox.Show("Data tidak dapat diakses", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }


        }
        //20210120, julio, RDN-410, end

        //20221019, Andi, HFUNDING-178, begin
        private void ReksaSalesEksekutifTransaksi(string RefID,
            string KodeSales, string Keterangan, DateTime LastUpdateDate,
            int LastUpdateNIK, DateTime CreateDate, int CreateBy)
        {
            //DataSet dsQueryResult;
            OleDbParameter[] dbParam = new OleDbParameter[7];
            try
            {
                //(dbParam[0] = new OleDbParameter("@pnType", OleDbType.TinyInt)).Value = intType;
                (dbParam[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar, 20)).Value = RefID;
                (dbParam[1] = new OleDbParameter("@pcKodeSales", OleDbType.VarChar, 50)).Value = KodeSales;
                (dbParam[2] = new OleDbParameter("@pcKeterangan", OleDbType.VarChar, 200)).Value = Keterangan;
                (dbParam[3] = new OleDbParameter("@pdLastUpdateDate", OleDbType.Date)).Value = LastUpdateDate;
                (dbParam[4] = new OleDbParameter("@pnLastUpdateNIK", OleDbType.Integer)).Value = LastUpdateNIK;
                (dbParam[5] = new OleDbParameter("@pdCreateDate", OleDbType.Date)).Value = CreateDate;
                (dbParam[6] = new OleDbParameter("@pnCreateBy", OleDbType.Integer)).Value = CreateBy;

                this.ClQ.ExecProc("dbo.ReksaDataSimanis", ref dbParam);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }
        //20221019, Andi, HFUNDING-178, end
        private void ReksaMaintainTransaksiNew(int intType, string strTranType, string RefID,
            string CIFNo,
            //20210922, korvi, RDN-674, begin
            string NoRek,
            string NoRekCCY,
            //20210922, korvi, RDN-674, end
            string OfficeId, string NoRekening, string writerSubs, string writerRedemp,

            //20150518, liliana, LIBST13020, begin
            //string writerRDB, string Inputter, int Seller, int Waperd, int Referentor
            string writerRDB, string Inputter, int Seller, int Waperd,
            //20150728, liliana, LIBST13020, begin
            //string NoRekeningUSD, int Referentor
            string NoRekeningUSD, string NoRekeningMC,
            int Referentor
            //20150728, liliana, LIBST13020, end
            //20150518, liliana, LIBST13020, end
            //20221019, Andi, HFUNDING-178, begin
            , string KodeSales, string Keterangan,
            int LastUpdateNIK
            //20221019, Andi, HFUNDING-178, end
        )
        {
            //20230223, Antonius Filian, RDN-903, begin
            #region Remark Existing
            //DataSet dsQueryResult;

            //20150518, liliana, LIBST13020, begin
            //OleDbParameter[] dbParam = new OleDbParameter[18];
            //20150827, liliana, LIBST13020, begin
            //OleDbParameter[] dbParam = new OleDbParameter[30];
            //20210922, korvi, RDN-674, begin
            //OleDbParameter[] dbParam = new OleDbParameter[34];
            //20210922, korvi, RDN-674, end
            //20150827, liliana, LIBST13020, end
            //20150518, liliana, LIBST13020, end

            #endregion Remark Existing
            //20230223, Antonius Filian, RDN-903, end

            try
            {
                //20230223, Antonius Filian, RDN-903, begin
                #region Remark Existing
                //(dbParam[0] = new OleDbParameter("@pnType", OleDbType.TinyInt)).Value = intType;
                //(dbParam[1] = new OleDbParameter("@pnTranType", OleDbType.VarChar, 20)).Value = strTranType;
                //(dbParam[2] = new OleDbParameter("@pcRefID", OleDbType.Char, 20)).Value = RefID;
                //(dbParam[3] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = CIFNo;
                //(dbParam[4] = new OleDbParameter("@pcOfficeId", OleDbType.VarChar, 5)).Value = OfficeId;
                //(dbParam[5] = new OleDbParameter("@pcNoRekening", OleDbType.VarChar, 20)).Value = NoRekening;

                //(dbParam[6] = new OleDbParameter("@pvcXMLTrxSubscription", OleDbType.LongVarChar)).Value = writerSubs;
                //(dbParam[7] = new OleDbParameter("@pvcXMLTrxRedemption", OleDbType.LongVarChar)).Value = writerRedemp;
                //(dbParam[8] = new OleDbParameter("@pvcXMLTrxRDB", OleDbType.LongVarChar)).Value = writerRDB;

                //(dbParam[9] = new OleDbParameter("@pcInputter", OleDbType.VarChar, 40)).Value = Inputter;
                //(dbParam[10] = new OleDbParameter("@pnSeller", OleDbType.Integer)).Value = Seller;
                //(dbParam[11] = new OleDbParameter("@pnWaperd", OleDbType.Integer)).Value = Waperd;
                //(dbParam[12] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                //(dbParam[13] = new OleDbParameter("@pnReferentor", OleDbType.Integer)).Value = Referentor;

                //(dbParam[14] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;

                //(dbParam[15] = new OleDbParameter("@pcWarnMsg", OleDbType.VarChar, 200)).Value = "";
                //(dbParam[16] = new OleDbParameter("@pcWarnMsg2", OleDbType.VarChar, 200)).Value = "";
                //(dbParam[17] = new OleDbParameter("@pcWarnMsg3", OleDbType.VarChar, 200)).Value = "";
                //20150518, liliana, LIBST13020, begin

                //(dbParam[18] = new OleDbParameter("@pbDocFCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState);
                //(dbParam[19] = new OleDbParameter("@pbDocFCDevidentAuthLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState);
                //(dbParam[20] = new OleDbParameter("@pbDocFCJoinAcctStatementLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState);
                //(dbParam[21] = new OleDbParameter("@pbDocFCIDCopy", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState);
                //(dbParam[22] = new OleDbParameter("@pbDocFCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState);

                //(dbParam[23] = new OleDbParameter("@pbDocTCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState);
                //(dbParam[24] = new OleDbParameter("@pbDocTCTermCondition", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState);
                //(dbParam[25] = new OleDbParameter("@pbDocTCProspectus", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState);
                //(dbParam[26] = new OleDbParameter("@pbDocTCFundFactSheet", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState);
                //(dbParam[27] = new OleDbParameter("@pbDocTCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState);

                //(dbParam[28] = new OleDbParameter("@pcDocFCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("FC"); ;
                //(dbParam[29] = new OleDbParameter("@pcDocTCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("TC"); ;

                //20150518, liliana, LIBST13020, end
                //20150827, liliana, LIBST13020, begin
                //(dbParam[30] = new OleDbParameter("@pcWarnMsg4", OleDbType.VarChar, 400)).Value = "";
                //20150827, liliana, LIBST13020, end
                //20210922, korvi, RDN-674, begin
                //(dbParam[31] = new OleDbParameter("@pbIsAOANonSII", OleDbType.Boolean)).Value = 0;
                //(dbParam[32] = new OleDbParameter("@pcNoRek", OleDbType.VarChar, 20)).Value = NoRek;
                //(dbParam[33] = new OleDbParameter("@pcNoRekCcy", OleDbType.VarChar, 4)).Value = NoRekCCY;
                //20210922, korvi, RDN-674, end

                //dbParam[2].Direction = ParameterDirection.InputOutput;
                //dbParam[15].Direction = ParameterDirection.Output;
                //dbParam[16].Direction = ParameterDirection.Output;
                //dbParam[17].Direction = ParameterDirection.Output;
                //20150827, liliana, LIBST13020, begin
                //dbParam[30].Direction = ParameterDirection.Output;
                //20150827, liliana, LIBST13020, end

                //20150615, liliana, LIBST13020, begin
                //ClQ.TimeOut = 6000;
                //20150615, liliana, LIBST13020, end

                //bool blnResult = ClQ.ExecProc("dbo.ReksaMaintainAllTransaksiNew", ref dbParam, out dsQueryResult);

                #endregion Remark Existing

                #region hit API
                DataSet dsUrl = new DataSet();
                string strUrlAPI = "";
                string _strGuid = "";
                bool blnResult = false;

                _strGuid = Guid.NewGuid().ToString();
                if (_cProc.GetAPIParam("TRX_ReksaMaintainAllTransaksiNew", out dsUrl))
                {
                    strUrlAPI = dsUrl.Tables[0].Rows[0]["ParamVal"].ToString();
                }
                _ReksaMaintainAllTransaksiNewRq = new ReksaMaintainAllTransaksiNewRq();
                _ReksaMaintainAllTransaksiNewRq.MessageGUID = _strGuid;
                _ReksaMaintainAllTransaksiNewRq.ParentMessageGUID = null;
                _ReksaMaintainAllTransaksiNewRq.TransactionMessageGUID = _strGuid;
                _ReksaMaintainAllTransaksiNewRq.IsResponseMessage = "false";
                _ReksaMaintainAllTransaksiNewRq.UserNIK = intNIK.ToString();
                _ReksaMaintainAllTransaksiNewRq.ModuleName = strModule;
                _ReksaMaintainAllTransaksiNewRq.MessageDateTime = DateTime.Now.ToString();
                _ReksaMaintainAllTransaksiNewRq.DestinationURL = strUrlAPI;
                _ReksaMaintainAllTransaksiNewRq.IsSuccess = "true";
                _ReksaMaintainAllTransaksiNewRq.ErrorCode = "";
                _ReksaMaintainAllTransaksiNewRq.ErrorDescription = "";
                //req data 
                _ReksaMaintainAllTransaksiNewRq.Data.nType = Int64.Parse(intType.ToString());
                _ReksaMaintainAllTransaksiNewRq.Data.cTranType = strTranType;
                _ReksaMaintainAllTransaksiNewRq.Data.cRefID = RefID;
                _ReksaMaintainAllTransaksiNewRq.Data.cCIFNo = CIFNo;
                _ReksaMaintainAllTransaksiNewRq.Data.cOfficeId = OfficeId;
                _ReksaMaintainAllTransaksiNewRq.Data.cNoRekening = NoRekening;
                _ReksaMaintainAllTransaksiNewRq.Data.vcXMLTrxSubscription = writerSubs;
                _ReksaMaintainAllTransaksiNewRq.Data.vcXMLTrxRedemption = writerRedemp;
                _ReksaMaintainAllTransaksiNewRq.Data.vcXMLTrxRDB = writerRDB;
                _ReksaMaintainAllTransaksiNewRq.Data.cInputter = Inputter;
                _ReksaMaintainAllTransaksiNewRq.Data.nSeller = Seller;
                _ReksaMaintainAllTransaksiNewRq.Data.nWaperd = Waperd;
                _ReksaMaintainAllTransaksiNewRq.Data.nNIK = intNIK;
                _ReksaMaintainAllTransaksiNewRq.Data.nReferentor = Referentor;
                _ReksaMaintainAllTransaksiNewRq.Data.cGuid = strGuid;
                _ReksaMaintainAllTransaksiNewRq.Data.cWarnMsg = "";
                _ReksaMaintainAllTransaksiNewRq.Data.cWarnMsg2 = "";
                _ReksaMaintainAllTransaksiNewRq.Data.cWarnMsg3 = "";
                _ReksaMaintainAllTransaksiNewRq.Data.bDocFCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocFCDevidentAuthLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocFCJoinAcctStatementLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocFCIDCopy = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocFCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocTCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocTCTermCondition = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocTCProspectus = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocTCFundFactSheet = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.bDocTCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState);
                _ReksaMaintainAllTransaksiNewRq.Data.cDocFCOthersList = objFormDocument.GetOthersList("FC"); ;
                _ReksaMaintainAllTransaksiNewRq.Data.cDocTCOthersList = objFormDocument.GetOthersList("TC"); ;
                _ReksaMaintainAllTransaksiNewRq.Data.cWarnMsg4 = "";
                _ReksaMaintainAllTransaksiNewRq.Data.bIsAOANonSII = false;
                _ReksaMaintainAllTransaksiNewRq.Data.cNoRek = NoRek;
                _ReksaMaintainAllTransaksiNewRq.Data.cNoRekCcy = NoRekCCY;

                //end
                ReksaMaintainAllTransaksiNewRs _response = _iServiceAPI.ReksaMaintainAllTransaksiNew(_ReksaMaintainAllTransaksiNewRq);

                if (_response.IsSuccess == true)
                {
                    blnResult = true;
                }
                else
                {
                    blnResult = false;
                    if (_response.ErrorDescription != "" && _response.ErrorDescription != null)
                    {
                        MessageBox.Show(_response.ErrorDescription);
                        return;
                    }
                }
                #endregion hit API

                //20230223, Antonius Filian, RDN-903, end

                //20230223, Antonius Filian, RDN-903, begin
                if (!blnResult && _response.Data.MandatoryField2 != null)
                {
                    MessageBox.Show("Data gagal disimpan!!");
                    frmErrorMessage frmError = new frmErrorMessage();
                    DataTable dt = ToDataTable(_response.Data.MandatoryField2);
                    frmError.SetErrorTable(dt);
                    frmError.ShowDialog();
                }
                //20230223, Antonius Filian, RDN-903, end

                if (blnResult && _response.Data.ReksaMaintainAllTransaksi1 != null)
                {
                    //20230223, Antonius Filian, RDN-903, begin
                    if (_response.ErrorDescription != null && _response.ErrorDescription != "")
                    {
                        MessageBox.Show("Data gagal disimpan!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        //20150827, liliana, LIBST13020, begin
                        //20230223, Antonius Filian, RDN-903, begin
                        if (_response.Data.ReksaMaintainAllTransaksi1.WarnMsg4 != null && _response.Data.ReksaMaintainAllTransaksi1.WarnMsg4 != "")
                        {
                            MessageBox.Show(_response.Data.ReksaMaintainAllTransaksi1.WarnMsg4.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        //20150827, liliana, LIBST13020, begin
                        //20150825, liliana, LIBST13020, begin
                        //if (dbParam[16].Value.ToString() != "")
                        //{
                        //    MessageBox.Show("Profil Risiko produk lebih tinggi dari Profil Risiko Nasabah . PASTIKAN Nasabah sudah menandatangani kolom Profil Risiko pada Subscription/Switching Form", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        //}
                        //20150825, liliana, LIBST13020, end

                        //20230223, Antonius Filian, RDN-903, begin
                        if (_response.Data.ReksaMaintainAllTransaksi1.WarnMsg3 != null && _response.Data.ReksaMaintainAllTransaksi1.WarnMsg3 != "")
                        {
                            MessageBox.Show("Umur nasabah 55 tahun atau lebih, Mohon dipastikan nasabah menandatangani pernyataan pada kolom yang disediakan di Formulir Subscription/Switching", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        if (_response.Data.ReksaMaintainAllTransaksi1.WarnMsg.ToString() != null)
                        {
                            MessageBox.Show("Transaksi Telah Tersimpan, No Referensi : " + _response.Data.ReksaMaintainAllTransaksi1.RefID.ToString() + ". Perlu Otorisasi Supervisor!\n" + _response.Data.ReksaMaintainAllTransaksi1.WarnMsg.ToString(), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }
                        else
                        {
                            MessageBox.Show("Transaksi Telah Tersimpan, No Referensi : " + _response.Data.ReksaMaintainAllTransaksi1.RefID.ToString() + ". Perlu Otorisasi Supervisor!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        //20230223, Antonius Filian, RDN-903, end

                        //20221019, Andi, HFUNDING-178, begin
                        //20230223, Antonius Filian, RDN-903, begin
                        //ReksaSalesEksekutifTransaksi(dbParam[2].Value.ToString(), KodeSales,
                        //    Keterangan, DateTime.Today, LastUpdateNIK, DateTime.Today, LastUpdateNIK);
                        //20230511, Andhika J, RDN-903, begin
                        //ReksaSalesEksekutifTransaksi(_ReksaMaintainNewBookingRq.Data.cRefID.ToString(), KodeSales,
                        //    Keterangan, DateTime.Today, LastUpdateNIK, DateTime.Today, LastUpdateNIK);
                        ReksaSalesEksekutifTransaksi(_response.Data._ReksaMaintainAllTransaksi1.RefID.ToString(), KodeSales,
                           Keterangan,
                           DateTime.Today, intNIK, DateTime.Today, intNIK);
                        //20230511, Andhika J, RDN-903, end
                        //20230223, Antonius Filian, RDN-903, end
                        //20221019, Andi, HFUNDING-178, end

                        //20150724, liliana, LIBST13020, begin
                        //20150728, liliana, LIBST13020, begin
                        ResetForm();
                        DisableAllForm(false);
                        DisableFormTrxSubs(false);
                        DisableFormTrxRedemp(false);
                        DisableFormTrxRDB(false);
                        //20150728, liliana, LIBST13020, end
                        if (strTranType == "SUBS")
                        {
                            //20230223, Antonius Filian, RDN-903, begin
                            cmpsrNoRefSubs.Text1 = _response.Data.ReksaMaintainAllTransaksi1.RefID.ToString();
                            //20230223, Antonius Filian, RDN-903, end
                            cmpsrNoRefSubs.ValidateField();
                        }
                        else if (strTranType == "REDEMP")
                        {
                            //20230223, Antonius Filian, RDN-903, begin
                            cmpsrNoRefRedemp.Text1 = _response.Data.ReksaMaintainAllTransaksi1.RefID.ToString();
                            //20230223, Antonius Filian, RDN-903, end
                            cmpsrNoRefRedemp.ValidateField();
                        }
                        else if (strTranType == "SUBSRDB")
                        {
                            //20230223, Antonius Filian, RDN-903, begin
                            cmpsrNoRefRDB.Text1 = _response.Data.ReksaMaintainAllTransaksi1.RefID.ToString();
                            //20230223, Antonius Filian, RDN-903, end
                            cmpsrNoRefRDB.ValidateField();
                        }
                        //20150724, liliana, LIBST13020, end
                        //20150710, liliana, LIBST13020, begin
                        //subCancel();
                        subRefresh();
                        //20150710, liliana, LIBST13020, end
                    }
                }
            }
            catch (FormatException fex)
            {
                MessageBox.Show(fex.ToString());
                MessageBox.Show("Data yang diinput dalam format yang tidak valid\nAtau ada field mandatory yang tidak diisi\nMohon periksa kembali data yang diinput");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void subSave()
        {
            if (_strTabName == "SUBS")
            {
                //20160509, Elva, CSODD16117, begin
                if (cmpsrKodeKantorSubs.Text1 == "")
                {
                    MessageBox.Show("Kode Kantor harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string strIsAllowed = "";
                if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorSubs.Text1, out strIsAllowed))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormSubs();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string strErrorMessage;
                strIsAllowed = "";
                if (clsValidator.ValidasiUserCBO(ClQ, cmpsrKodeKantorSubs.Text1, strBranch, out strIsAllowed, out strErrorMessage))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateUserCBOOffice], " + strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormSubs();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateUserCBOOffice]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //20160509, Elva, CSODD16117, end
                if (cmpsrCIFSubs.Text1 == "")
                {
                    MessageBox.Show("CIF harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!GlobalFunctionCIF.RetrieveCIFData(intNIK, strBranch, strModule, strGuid, Int64.Parse(cmpsrCIFSubs.Text1)))
                {
                    MessageBox.Show("Gagal validasi CIF ke modul ProCIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((checkPhoneOrderSubs.Checked) && (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSubs.Text1)))
                {
                    MessageBox.Show("Nasabah tidak memiliki fasilitas phone order!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20150326, liliana, LIBST13020, begin
                //if (textSIDSubs.Text == "")
                //{
                //    MessageBox.Show("SID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150326, liliana, LIBST13020, end

                if (textShareHolderIdSubs.Text == "")
                {
                    MessageBox.Show("Shareholder ID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150505, liliana, LIBST13020, begin
                //20150505, liliana, LIBST13020, begin
                //if (textRekeningSubs.Text == "")
                //if (maskedRekeningSubs.Text == "")
                ////20150505, liliana, LIBST13020, end
                //{
                //    MessageBox.Show("Nomor rekening harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (textNamaRekeningSubs.Text == "")
                //{
                //    MessageBox.Show("Nama rekening harus terisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150505, liliana, LIBST13020, end

                if (cmpsrSellerSubs.Text1 == "")
                {
                    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrWaperdSubs.Text1 == "")
                {
                    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150408, liliana, LIBST13020, begin

                if (dataGridViewSubs.Rows.Count == 0)
                {
                    MessageBox.Show("Data transaksi subscription tidak boleh kosong!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150408, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, begin
                if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Formulir Subscription wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState))
                {
                    MessageBox.Show("Copy Bukti Identitas wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Copy Formulir Subscription wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState))
                {
                    MessageBox.Show("Prospektus wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState))
                {
                    MessageBox.Show("Fund Fact Sheet wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                //20150518, liliana, LIBST13020, end

                //cek risk profile
                string RiskProfile;
                TimeSpan diff;
                DateTime LastUpdateRiskProfile;
                //20170825, liliana, COPOD17271, begin
                //CekRiskProfile(cmpsrCIFSubs.Text1, dateTglTransaksiSubs.Value, out RiskProfile, out LastUpdateRiskProfile, out diff);

                DateTime ExpRiskProfile;
                CekRiskProfile(cmpsrCIFSubs.Text1, dateTglTransaksiSubs.Value, out RiskProfile, out LastUpdateRiskProfile,
                    out ExpRiskProfile,
                    out diff);
                //20170825, liliana, COPOD17271, end


                if (RiskProfile.Trim() == "")
                {
                    MessageBox.Show("CIF : " + cmpsrCIFSubs.Text1 + "\nData risk profile harus dilengkapi di Pro CIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20170825, liliana, COPOD17271, begin
                //if (diff.Days >= 365)
                if (ExpRiskProfile < System.DateTime.Today)
                //20170825, liliana, COPOD17271, end
                {
                    if (MessageBox.Show("CIF : " + cmpsrCIFSubs.Text1 +
                        "\nTanggal Last Update Risk Profile : " + LastUpdateRiskProfile.ToString("dd-MMM-yyyy") +
                        //20170825, liliana, COPOD17271, begin
                        //"\nTanggal Last Update risk profile sudah lewat dari satu tahun" +
                        //"\nLast Update Risk Profile telah lebih dari 1 tahun, apakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        "\nTanggal Last Update risk profile sudah expired" +
                        "\nApakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    //20170825, liliana, COPOD17271, end
                    {
                        MessageBox.Show("Lakukan Perubahan Risk Profile di Pro CIF-Menu Inquiry and Maintenance-Data Pribadi", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else
                    {
                        OleDbParameter[] Param2 = new OleDbParameter[2];
                        (Param2[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = cmpsrCIFSubs.Text1;
                        (Param2[1] = new OleDbParameter("@pdNewLastUpdate", OleDbType.Date)).Value = dateTglTransaksiSubs.Value;

                        if (!ClQ.ExecProc("dbo.ReksaManualUpdateRiskProfile", ref Param2))
                        {
                            MessageBox.Show("Gagal simpan last update risk profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }

                //20210120, julio, RDN-410, begin
                //check email
                String EmailWarning;
                CekElecAddress(cmpsrCIFSubs.Text1, out EmailWarning);
                if (EmailWarning.Trim() != "")
                {
                    if (MessageBox.Show(EmailWarning, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        //20210519, joshua, RDN-540, begin
                        //MessageBox.Show("Lengkapi email Nasabah pada ProCIF untuk mengunduh Surat Konfirmasi pada Akses KSEI. Permintaan report tercetak akan ada potensi dikenakan biaya", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show("Lengkapi email Nasabah pada ProCIF dan ProReksa agar terdaftar dan dapat mengunduh laporan di AKSes KSEI", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //20210519, joshua, RDN-540, begin
                        return;
                    }
                }
                //20210120, julio, RDN-410, end

                //write XML
                System.IO.StringWriter writer = new System.IO.StringWriter();
                cTransaksi.dttSubscription.TableName = "Subscription";
                cTransaksi.dttSubscription.WriteXml(writer, System.Data.XmlWriteMode.IgnoreSchema, false);

                //20150410, liliana, LIBST13020, begin
                int _intReferentor;

                if (cmpsrReferentorSubs.Text1.Trim() == "")
                {
                    _intReferentor = 0;
                }
                else
                {
                    _intReferentor = System.Convert.ToInt32(cmpsrReferentorSubs.Text1.Trim());
                }
                //20150410, liliana, LIBST13020, end
                //20220121, sandi, RDN-727, begin
                string rekSubsIDR = maskedRekeningSubs.Text.Replace("-", "").Trim();
                string rekSubsUSD = maskedRekeningSubsUSD.Text.Replace("-", "").Trim();
                string rekSubsMC = maskedRekeningSubsMC.Text.Replace("-", "").Trim();

                if (cmpsrNoRekSubs.Text1 != rekSubsIDR && cmpsrNoRekSubs.Text1 != rekSubsUSD && cmpsrNoRekSubs.Text1 != rekSubsMC)
                {
                    MessageBox.Show("Rekening " + cmpsrNoRekSubs.Text1 + " akan disimpan di Master Nasabah untuk pembagian deviden dan pendebetan RDB (jika ada)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //20220121, sandi, RDN-727, end
                ReksaMaintainTransaksiNew(_intType, _strTabName, cmpsrNoRefSubs.Text1, cmpsrCIFSubs.Text1,
                    //20210922, korvi, RDN-674, begin
                        cmpsrNoRekSubs.Text1,
                        cmpsrNoRekSubs.Text2,
                    //20210922, korvi, RDN-674, end
                    //20150505, liliana, LIBST13020, begin
                    //cmpsrKodeKantorSubs.Text1, textRekeningSubs.Text, writer.ToString(), "", "",
                    //20150630, liliana, LIBST13020, begin
                    //cmpsrKodeKantorSubs.Text1, maskedRekeningSubs.Text.Replace("-", ""), writer.ToString(), "", "",
                       cmpsrKodeKantorSubs.Text1, maskedRekeningSubs.Text.Replace("-", "").Trim(), writer.ToString(), "", "",
                    //20150630, liliana, LIBST13020, end
                    //20150505, liliana, LIBST13020, end
                       txtbInputterSubs.Text, System.Convert.ToInt32(cmpsrSellerSubs.Text1), System.Convert.ToInt32(cmpsrWaperdSubs.Text1),
                    //20150410, liliana, LIBST13020, begin
                    //System.Convert.ToInt32(cmpsrReferentorSubs.Text1));
                    //20150518, liliana, LIBST13020, begin
                    //20150630, liliana, LIBST13020, begin
                    //maskedRekeningSubsUSD.Text.Replace("-", ""),
                    maskedRekeningSubsUSD.Text.Replace("-", "").Trim(),
                    //20150630, liliana, LIBST13020, end
                    //20150728, liliana, LIBST13020, begin
                    maskedRekeningSubsMC.Text.Replace("-", "").Trim(),
                    //20150728, liliana, LIBST13020, end
                    //20150518, liliana, LIBST13020, end
                       _intReferentor
                    //20221019, Andi, HFUNDING-178, begin
                       , textBoxKodeSalesSubs.Text,
                       richTextBoxKeteranganSubs.Text, intNIK
                    //20221019, Andi, HFUNDING-178, end
                    );
                //20150410, liliana, LIBST13020, end
            }
            else if (_strTabName == "REDEMP")
            {
                //20160509, Elva, CSODD16117, begin
                if (cmpsrKodeKantorRedemp.Text1 == "")
                {
                    MessageBox.Show("Kode Kantor harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string strIsAllowed = "";
                if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorRedemp.Text1, out strIsAllowed))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormRedemp();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //20240530, gio, RDN-1164, begin
                //cek risk profile
                string RiskProfile;
                TimeSpan diff;
                DateTime LastUpdateRiskProfile;
                DateTime ExpRiskProfile;
                CekRiskProfile(cmpsrCIFRedemp.Text1, dateTglTransaksiRedemp.Value, out RiskProfile, out LastUpdateRiskProfile,
                    out ExpRiskProfile,
                    out diff);
                if (RiskProfile.Trim() == "")
                {
                    MessageBox.Show("CIF : " + cmpsrCIFRedemp.Text1 + "\nData risk profile harus dilengkapi di Pro CIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (ExpRiskProfile < System.DateTime.Today)
                {
                    if (MessageBox.Show("CIF : " + cmpsrCIFRedemp.Text1 +
                        "\nTanggal Last Update Risk Profile : " + LastUpdateRiskProfile.ToString("dd-MMM-yyyy") + "\nTanggal Last Update risk profile sudah expired" +
                        "\nApakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        MessageBox.Show("Lakukan Perubahan Risk Profile di Pro CIF-Menu Inquiry and Maintenance-Data Pribadi", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else
                    {
                        OleDbParameter[] Param2 = new OleDbParameter[2];
                        (Param2[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = cmpsrCIFRedemp.Text1;
                        (Param2[1] = new OleDbParameter("@pdNewLastUpdate", OleDbType.Date)).Value = dateTglTransaksiRedemp.Value;
                        if (!ClQ.ExecProc("dbo.ReksaManualUpdateRiskProfile", ref Param2))
                        {
                            MessageBox.Show("Gagal simpan last update risk profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                //20210120, julio, RDN-410, begin
                //check email
                String EmailWarning;
                CekElecAddress(cmpsrCIFRedemp.Text1, out EmailWarning);
                if (EmailWarning.Trim() != "")
                {
                    if (MessageBox.Show(EmailWarning, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        //20210519, joshua, RDN-540, begin
                        //MessageBox.Show("Lengkapi email Nasabah pada ProCIF untuk mengunduh Surat Konfirmasi pada Akses KSEI. Permintaan report tercetak akan ada potensi dikenakan biaya", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show("Lengkapi email Nasabah pada ProCIF dan ProReksa agar terdaftar dan dapat mengunduh laporan di AKSes KSEI", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //20210519, joshua, RDN-540, begin
                        return;
                    }
                }
                //20210120, julio, RDN-410, end
                string strErrorMessage;
                strIsAllowed = "";
                if (clsValidator.ValidasiUserCBO(ClQ, cmpsrKodeKantorRedemp.Text1, strBranch, out strIsAllowed, out strErrorMessage))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateUserCBOOffice], " + strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormRedemp();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateUserCBOOffice]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //20160509, Elva, CSODD16117, end
                if (cmpsrCIFRedemp.Text1 == "")
                {
                    MessageBox.Show("CIF harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!GlobalFunctionCIF.RetrieveCIFData(intNIK, strBranch, strModule, strGuid, Int64.Parse(cmpsrCIFRedemp.Text1)))
                {
                    MessageBox.Show("Gagal validasi CIF ke modul ProCIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((checkPhoneOrderRedemp.Checked) && (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRedemp.Text1)))
                {
                    MessageBox.Show("Nasabah tidak memiliki fasilitas phone order!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20150326, liliana, LIBST13020, begin
                //if (textSIDRedemp.Text == "")
                //{
                //    MessageBox.Show("SID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150326, liliana, LIBST13020, end

                if (textShareHolderIdRedemp.Text == "")
                {
                    MessageBox.Show("Shareholder ID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150505, liliana, LIBST13020, begin
                //20150505, liliana, LIBST13020, begin
                //if (textRekeningRedemp.Text == "")
                //if (maskedRekeningRedemp.Text == "")
                ////20150505, liliana, LIBST13020, end
                //{
                //    MessageBox.Show("Nomor rekening harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (textNamaRekeningRedemp.Text == "")
                //{
                //    MessageBox.Show("Nama rekening harus terisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150505, liliana, LIBST13020, end

                if (cmpsrSellerRedemp.Text1 == "")
                {
                    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrWaperdRedemp.Text1 == "")
                {
                    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150408, liliana, LIBST13020, begin

                if (dataGridViewRedemp.Rows.Count == 0)
                {
                    MessageBox.Show("Data transaksi redemption tidak boleh kosong!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150408, liliana, LIBST13020, end

                //write XML
                System.IO.StringWriter writer = new System.IO.StringWriter();
                cTransaksi.dttRedemption.TableName = "Redemption";
                cTransaksi.dttRedemption.WriteXml(writer, System.Data.XmlWriteMode.IgnoreSchema, false);

                //20150410, liliana, LIBST13020, begin
                int _intReferentor;

                if (cmpsrReferentorRedemp.Text1.Trim() == "")
                {
                    _intReferentor = 0;
                }
                else
                {
                    _intReferentor = System.Convert.ToInt32(cmpsrReferentorRedemp.Text1.Trim());
                }
                //20220121, sandi, RDN-727, begin
                string rekRedmIDR = maskedRekeningRedemp.Text.Replace("-", "").Trim();
                string rekRedmUSD = maskedRekeningRedempUSD.Text.Replace("-", "").Trim();
                string rekRedmMC = maskedRekeningRedempMC.Text.Replace("-", "").Trim();

                if (cmpsrNoRekRedemp.Text1 != rekRedmIDR && cmpsrNoRekRedemp.Text1 != rekRedmUSD && cmpsrNoRekRedemp.Text1 != rekRedmMC)
                {
                    MessageBox.Show("Rekening " + cmpsrNoRekRedemp.Text1 + " akan disimpan di Master Nasabah untuk pembagian deviden dan pendebetan RDB (jika ada)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //20220121, sandi, RDN-727, end
                //20150410, liliana, LIBST13020, end
                ReksaMaintainTransaksiNew(_intType, _strTabName, cmpsrNoRefRedemp.Text1, cmpsrCIFRedemp.Text1,
                    //20210922, korvi, RDN-674, begin
                cmpsrNoRekRedemp.Text1,
                cmpsrNoRekRedemp.Text2,
                    //20210922, korvi, RDN-674, end,
                    //20150505, liliana, LIBST13020, begin
                    //cmpsrKodeKantorRedemp.Text1, textRekeningRedemp.Text, "", writer.ToString(), "",
                    //20150630, liliana, LIBST13020, begin
                    // cmpsrKodeKantorRedemp.Text1, maskedRekeningRedemp.Text.Replace("-", ""), "", writer.ToString(), "",
                cmpsrKodeKantorRedemp.Text1, maskedRekeningRedemp.Text.Replace("-", "").Trim(), "", writer.ToString(), "",
                    //20150630, liliana, LIBST13020, end
                    //20150505, liliana, LIBST13020, end
                       txtbInputterRedemp.Text, System.Convert.ToInt32(cmpsrSellerRedemp.Text1),
                    //20150410, liliana, LIBST13020, begin
                    //System.Convert.ToInt32(cmpsrWaperdRedemp.Text1), System.Convert.ToInt32(cmpsrReferentorRedemp.Text1));
                    //20150630, liliana, LIBST13020, begin
                    //System.Convert.ToInt32(cmpsrWaperdRedemp.Text1), maskedRekeningRedempUSD.Text.Replace("-", ""), _intReferentor);
                    //20150728, liliana, LIBST13020, begin
                    //System.Convert.ToInt32(cmpsrWaperdRedemp.Text1), maskedRekeningRedempUSD.Text.Replace("-", "").Trim(), _intReferentor);
                       System.Convert.ToInt32(cmpsrWaperdRedemp.Text1), maskedRekeningRedempUSD.Text.Replace("-", "").Trim()
                       , maskedRekeningRedempMC.Text.Replace("-", "").Trim()
                       , _intReferentor
                    //20221019, Andi, HFUNDING-178, begin
                       , textBoxKodeSalesRedemp.Text,
                       richTextBoxKeteranganRedemp.Text, intNIK
                    //20221019, Andi, HFUNDING-178, end
                       );
                //20150728, liliana, LIBST13020, end
                //20150630, liliana, LIBST13020, end
                //20150410, liliana, LIBST13020, end
            }
            else if (_strTabName == "SUBSRDB")
            {
                //20160509, Elva, CSODD16117, begin
                if (cmpsrKodeKantorRDB.Text1 == "")
                {
                    MessageBox.Show("Kode Kantor harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string strIsAllowed = "";
                if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorRDB.Text1, out strIsAllowed))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormRDB();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string strErrorMessage;
                strIsAllowed = "";
                if (clsValidator.ValidasiUserCBO(ClQ, cmpsrKodeKantorRDB.Text1, strBranch, out strIsAllowed, out strErrorMessage))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateUserCBOOffice], " + strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormRDB();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateUserCBOOffice]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //20160509, Elva, CSODD16117, end
                if (cmpsrCIFRDB.Text1 == "")
                {
                    MessageBox.Show("CIF harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!GlobalFunctionCIF.RetrieveCIFData(intNIK, strBranch, strModule, strGuid, Int64.Parse(cmpsrCIFRDB.Text1)))
                {
                    MessageBox.Show("Gagal validasi CIF ke modul ProCIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((checkPhoneOrderRDB.Checked) && (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRDB.Text1)))
                {
                    MessageBox.Show("Nasabah tidak memiliki fasilitas phone order!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20150326, liliana, LIBST13020, begin
                //if (textSIDRDB.Text == "")
                //{
                //    MessageBox.Show("SID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150326, liliana, LIBST13020, end

                if (textShareHolderIdRDB.Text == "")
                {
                    MessageBox.Show("Shareholder ID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150505, liliana, LIBST13020, begin
                ////20150505, liliana, LIBST13020, begin
                ////if (textRekeningRDB.Text == "")
                //if (maskedRekeningRDB.Text == "")
                ////20150505, liliana, LIBST13020, end
                //{
                //    MessageBox.Show("Nomor rekening harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (textNamaRekeningRDB.Text == "")
                //{
                //    MessageBox.Show("Nama rekening harus terisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150505, liliana, LIBST13020, end

                if (cmpsrSellerRDB.Text1 == "")
                {
                    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrWaperdRDB.Text1 == "")
                {
                    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150408, liliana, LIBST13020, begin

                if (dataGridViewRDB.Rows.Count == 0)
                {
                    MessageBox.Show("Data transaksi Subscription RDB tidak boleh kosong!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150408, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, begin
                if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Formulir Subscription wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState))
                {
                    MessageBox.Show("Copy Bukti Identitas wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Copy Formulir Subscription wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState))
                {
                    MessageBox.Show("Prospektus wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState))
                {
                    MessageBox.Show("Fund Fact Sheet wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                //20150518, liliana, LIBST13020, end

                //cek risk profile
                string RiskProfile;
                TimeSpan diff;
                DateTime LastUpdateRiskProfile;
                //20170825, liliana, COPOD17271, begin
                //CekRiskProfile(cmpsrCIFRDB.Text1, dateTglTransaksiRDB.Value, out RiskProfile, out LastUpdateRiskProfile, out diff);

                DateTime ExpRiskProfile;
                CekRiskProfile(cmpsrCIFRDB.Text1, dateTglTransaksiRDB.Value, out RiskProfile, out LastUpdateRiskProfile,
                    out ExpRiskProfile,
                    out diff);
                //20170825, liliana, COPOD17271, end

                if (RiskProfile.Trim() == "")
                {
                    MessageBox.Show("CIF : " + cmpsrCIFRDB.Text1 + "\nData risk profile harus dilengkapi di Pro CIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20170825, liliana, COPOD17271, begin
                //if (diff.Days >= 365)
                if (ExpRiskProfile < System.DateTime.Today)
                //20170825, liliana, COPOD17271, end
                {
                    if (MessageBox.Show("CIF : " + cmpsrCIFRDB.Text1 +
                        "\nTanggal Last Update Risk Profile : " + LastUpdateRiskProfile.ToString("dd-MMM-yyyy") +
                        //20170825, liliana, COPOD17271, begin
                        //"\nTanggal Last Update risk profile sudah lewat dari satu tahun" +
                        //"\nLast Update Risk Profile telah lebih dari 1 tahun, apakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        "\nTanggal Last Update risk profile sudah expired" +
                        "\nApakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    //20170825, liliana, COPOD17271, end
                    {
                        MessageBox.Show("Lakukan Perubahan Risk Profile di Pro CIF-Menu Inquiry and Maintenance-Data Pribadi", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else
                    {
                        OleDbParameter[] Param2 = new OleDbParameter[2];
                        (Param2[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = cmpsrCIFRDB.Text1;
                        (Param2[1] = new OleDbParameter("@pdNewLastUpdate", OleDbType.Date)).Value = dateTglTransaksiRDB.Value;

                        if (!ClQ.ExecProc("dbo.ReksaManualUpdateRiskProfile", ref Param2))
                        {
                            MessageBox.Show("Gagal simpan last update risk profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                //20210120, julio, RDN-410, begin
                //check email
                String EmailWarning;
                CekElecAddress(cmpsrCIFRDB.Text1, out EmailWarning);
                if (EmailWarning.Trim() != "")
                {
                    if (MessageBox.Show(EmailWarning, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        //20210519, joshua, RDN-540, begin
                        //MessageBox.Show("Lengkapi email Nasabah pada ProCIF untuk mengunduh Surat Konfirmasi pada Akses KSEI. Permintaan report tercetak akan ada potensi dikenakan biaya", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show("Lengkapi email Nasabah pada ProCIF dan ProReksa agar terdaftar dan dapat mengunduh laporan di AKSes KSEI", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //20210519, joshua, RDN-540, begin
                        return;
                    }
                }
                //20210120, julio, RDN-410, end

                //write XML
                System.IO.StringWriter writer = new System.IO.StringWriter();
                //20150413, liliana, LIBST13020, begin
                //cTransaksi.dttRedemption.TableName = "SubsRDB";
                //cTransaksi.dttRedemption.WriteXml(writer, System.Data.XmlWriteMode.IgnoreSchema, false);
                cTransaksi.dttSubsRDB.TableName = "SubsRDB";
                cTransaksi.dttSubsRDB.WriteXml(writer, System.Data.XmlWriteMode.IgnoreSchema, false);
                //20150413, liliana, LIBST13020, end

                //20150410, liliana, LIBST13020, begin
                int _intReferentor;

                if (cmpsrReferentorRDB.Text1.Trim() == "")
                {
                    _intReferentor = 0;
                }
                else
                {
                    _intReferentor = System.Convert.ToInt32(cmpsrReferentorRDB.Text1.Trim());
                }

                //20220121, sandi, RDN-727, begin
                string rekRDBIDR = maskedRekeningRDB.Text.Replace("-", "").Trim();
                string rekRDBUSD = maskedRekeningRDBUSD.Text.Replace("-", "").Trim();
                string rekRDBMC = maskedRekeningRDBMC.Text.Replace("-", "").Trim();

                if (cmpsrNoRekRDB.Text1 != rekRDBIDR && cmpsrNoRekRDB.Text1 != rekRDBUSD && cmpsrNoRekRDB.Text1 != rekRDBMC)
                {
                    MessageBox.Show("Rekening " + cmpsrNoRekRDB.Text1 + " akan disimpan di Master Nasabah untuk pembagian deviden dan pendebetan RDB (jika ada)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //20220121, sandi, RDN-727, end

                //20150410, liliana, LIBST13020, end
                ReksaMaintainTransaksiNew(_intType, _strTabName, cmpsrNoRefRDB.Text1, cmpsrCIFRDB.Text1,
                    //20210922, korvi, RDN-674, begin
                        cmpsrNoRekRDB.Text1,
                        cmpsrNoRekRDB.Text2,
                    //20210922, korvi, RDN-674, end,
                    //20150505, liliana, LIBST13020, begin
                    //cmpsrKodeKantorRDB.Text1, textRekeningRDB.Text, "", "", writer.ToString(),
                    //20150630, liliana, LIBST13020, begin
                    //cmpsrKodeKantorRDB.Text1, maskedRekeningRDB.Text.Replace("-", ""), "", "", writer.ToString(),
                       cmpsrKodeKantorRDB.Text1, maskedRekeningRDB.Text.Replace("-", "").Trim(), "", "", writer.ToString(),
                    //20150630, liliana, LIBST13020, end
                    //20150505, liliana, LIBST13020, end
                       txtbInputterRDB.Text, System.Convert.ToInt32(cmpsrSellerRDB.Text1),
                    //20150410, liliana, LIBST13020, begin
                    //System.Convert.ToInt32(cmpsrWaperdRDB.Text1), System.Convert.ToInt32(cmpsrReferentorRDB.Text1));
                    //20150630, liliana, LIBST13020, begin
                    //System.Convert.ToInt32(cmpsrWaperdRDB.Text1), maskedRekeningRDBUSD.Text.Replace("-", ""), _intReferentor);
                    //20150728, liliana, LIBST13020, begin
                    //System.Convert.ToInt32(cmpsrWaperdRDB.Text1), maskedRekeningRDBUSD.Text.Replace("-", "").Trim(), _intReferentor);
                       System.Convert.ToInt32(cmpsrWaperdRDB.Text1), maskedRekeningRDBUSD.Text.Replace("-", "").Trim(),
                       maskedRekeningRDBMC.Text.Replace("-", "").Trim(),
                       _intReferentor
                    //20221019, Andi, HFUNDING-178, begin
                       , textBoxKodeSalesSubsRdb.Text,
                       richTextBoxKeteranganSubsRdb.Text, intNIK
                    //20221019, Andi, HFUNDING-178, end
                       );
                //20150728, liliana, LIBST13020, end
                //20150630, liliana, LIBST13020, end
                //20150410, liliana, LIBST13020, end                
            }
            else if (_strTabName == "SWCNONRDB")
            {
                //20160509, Elva, CSODD16117, begin
                if (cmpsrKodeKantorSwc.Text1 == "")
                {
                    MessageBox.Show("Kode Kantor harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string strIsAllowed = "";
                if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorSwc.Text1, out strIsAllowed))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormSwc();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string strErrorMessage;
                strIsAllowed = "";
                if (clsValidator.ValidasiUserCBO(ClQ, cmpsrKodeKantorSwc.Text1, strBranch, out strIsAllowed, out strErrorMessage))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateUserCBOOffice], " + strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormSwc();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateUserCBOOffice]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //20160509, Elva, CSODD16117, end
                if (cmpsrCIFSwc.Text1 == "")
                {
                    MessageBox.Show("CIF harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!GlobalFunctionCIF.RetrieveCIFData(intNIK, strBranch, strModule, strGuid, Int64.Parse(cmpsrCIFSwc.Text1)))
                {
                    MessageBox.Show("Gagal validasi CIF ke modul ProCIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((checkPhoneOrderSwc.Checked) && (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSwc.Text1)))
                {
                    MessageBox.Show("Nasabah tidak memiliki fasilitas phone order!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20150326, liliana, LIBST13020, begin
                //if (textSIDSwc.Text == "")
                //{
                //    MessageBox.Show("SID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150326, liliana, LIBST13020, end

                if (textShareHolderIdSwc.Text == "")
                {
                    MessageBox.Show("Shareholder ID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, begin

                if (cmbTASwc.SelectedIndex == -1)
                {
                    MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, end

                //20150505, liliana, LIBST13020, begin
                ////20150505, liliana, LIBST13020, begin
                ////if (textRekeningSwc.Text == "")
                //if (maskedRekeningSwc.Text == "")
                ////20150505, liliana, LIBST13020, end
                //{
                //    MessageBox.Show("Nomor rekening harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (textNamaRekeningSwc.Text == "")
                //{
                //    MessageBox.Show("Nama rekening harus terisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150505, liliana, LIBST13020, end

                //20150622, liliana, LIBST13020, begin
                //if (cmpsrSellerSwc.Text1 == "")
                //{
                //    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (cmpsrWaperdSwc.Text1 == "")
                //{
                //    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150622, liliana, LIBST13020, end

                if (cmpsrProductSwcOut.Text1 == "")
                {
                    MessageBox.Show("Produk Switch Out Switching belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrProductSwcIn.Text1 == "")
                {
                    MessageBox.Show("Produk Switch In Switching belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrClientSwcOut.Text1 == "")
                {
                    MessageBox.Show("Client Code Switch Out Switching belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150813, liliana, LIBST13020, begin
                //if (cmpsrClientSwcIn.Text1 == "")
                if ((cmpsrClientSwcIn.Text1 == "") && (IsSubsNew == false))
                //20150813, liliana, LIBST13020, end
                {
                    MessageBox.Show("Client Code Switch In Switching belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (nispRedempSwc.Value == 0)
                {
                    MessageBox.Show("Jumlah unit Switching harus diisi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150921, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150921, liliana, LIBST13020, end
                    if (nispRedempSwc.Value > nispOutstandingUnitSwc.Value)
                    {
                        MessageBox.Show("Jumlah unit Switching tidak boleh lebih besar dari Outstanding Unit!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150921, liliana, LIBST13020, begin
                }
                //20150921, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, begin
                if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Formulir Subscription wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState))
                {
                    MessageBox.Show("Copy Bukti Identitas wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Copy Formulir Subscription wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState))
                {
                    MessageBox.Show("Prospektus wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState))
                {
                    MessageBox.Show("Fund Fact Sheet wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                //20150518, liliana, LIBST13020, end
                //20150622, liliana, LIBST13020, begin
                if (cmpsrSellerSwc.Text1 == "")
                {
                    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrWaperdSwc.Text1 == "")
                {
                    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150622, liliana, LIBST13020, end

                //cek risk profile
                string RiskProfile;
                TimeSpan diff;
                DateTime LastUpdateRiskProfile;
                //20170825, liliana, COPOD17271, begin
                //CekRiskProfile(cmpsrCIFSwc.Text1, dateTglTransaksiSwc.Value, out RiskProfile, out LastUpdateRiskProfile, out diff);

                DateTime ExpRiskProfile;
                CekRiskProfile(cmpsrCIFSwc.Text1, dateTglTransaksiSwc.Value, out RiskProfile, out LastUpdateRiskProfile,
                    out ExpRiskProfile,
                    out diff);
                //20170825, liliana, COPOD17271, end

                if (RiskProfile.Trim() == "")
                {
                    MessageBox.Show("CIF : " + cmpsrCIFSwc.Text1 + "\nData risk profile harus dilengkapi di Pro CIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20170825, liliana, COPOD17271, begin
                //if (diff.Days >= 365)
                if (ExpRiskProfile < System.DateTime.Today)
                //20170825, liliana, COPOD17271, end
                {
                    if (MessageBox.Show("CIF : " + cmpsrCIFSwc.Text1 +
                        "\nTanggal Last Update Risk Profile : " + LastUpdateRiskProfile.ToString("dd-MMM-yyyy") +
                        //20170825, liliana, COPOD17271, begin
                        //"\nTanggal Last Update risk profile sudah lewat dari satu tahun" +
                        //"\nLast Update Risk Profile telah lebih dari 1 tahun, apakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        "\nTanggal Last Update risk profile sudah expired" +
                        "\nApakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    //20170825, liliana, COPOD17271, end
                    {
                        MessageBox.Show("Lakukan Perubahan Risk Profile di Pro CIF-Menu Inquiry and Maintenance-Data Pribadi", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else
                    {
                        OleDbParameter[] Param2 = new OleDbParameter[2];
                        (Param2[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = cmpsrCIFSwc.Text1;
                        (Param2[1] = new OleDbParameter("@pdNewLastUpdate", OleDbType.Date)).Value = dateTglTransaksiSwc.Value;

                        if (!ClQ.ExecProc("dbo.ReksaManualUpdateRiskProfile", ref Param2))
                        {
                            MessageBox.Show("Gagal simpan last update risk profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                //20210120, julio, RDN-410, begin
                //check email
                String EmailWarning;
                CekElecAddress(cmpsrCIFSwc.Text1, out EmailWarning);
                if (EmailWarning.Trim() != "")
                {
                    if (MessageBox.Show(EmailWarning, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        //20210519, joshua, RDN-540, begin
                        //MessageBox.Show("Lengkapi email Nasabah pada ProCIF untuk mengunduh Surat Konfirmasi pada Akses KSEI. Permintaan report tercetak akan ada potensi dikenakan biaya", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show("Lengkapi email Nasabah pada ProCIF dan ProReksa agar terdaftar dan dapat mengunduh laporan di AKSes KSEI", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //20210519, joshua, RDN-540, begin
                        return;
                    }
                }
                //20210120, julio, RDN-410, end

                //set switching type
                //5: switching sebagian ; 6: switching all 
                int intSwcType, intClientIdSwcIn;
                if (IsSwitchingAll)
                {
                    intSwcType = 6;
                }
                else
                {
                    intSwcType = 5;
                }

                if (IsSubsNew)
                {
                    intClientIdSwcIn = 0;
                }
                else
                {
                    intClientIdSwcIn = int.Parse(cmpsrClientSwcIn[2].ToString());
                }
                //20150408, liliana, LIBST13020, begin
                decimal decUnitBalanceNomSwcOut, decUnitBalanceNomSwcIn;

                decUnitBalanceNomSwcOut = nispOutstandingUnitSwc.Value * _NAVSwcOutNonRDB;
                decUnitBalanceNomSwcIn = OutstandingUnitSwcIn * _NAVSwcInNonRDB;
                //20150408, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                bool bIsTax;
                bIsTax = false;

                if (cmbTASwc.SelectedIndex == 1)
                {
                    bIsTax = true;
                }
                //20160829, liliana, LOGEN00196, end

                //20230206, Antonius Filian, RDN-903, begin
                #region Remark Existing
                //DataSet dsQueryResult;
                //20150828, liliana, LIBST13020, begin
                //OleDbParameter[] dbParam = new OleDbParameter[56];
                //20160829, liliana, LOGEN00196, begin
                //OleDbParameter[] dbParam = new OleDbParameter[58];
                //20210922, lita, RDN-674, begin
                //OleDbParameter[] dbParam = new OleDbParameter[59];
                //OleDbParameter[] dbParam = new OleDbParameter[61];
                //20210922, lita, RDN-674, end
                //20160829, liliana, LOGEN00196, end
                //20150828, liliana, LIBST13020, end

                #endregion Remark Existing
                //20230206, Antonius Filian, RDN-903, end

                try
                {
                    //20230206, Antonius Filian, RDN-903, begin
                    #region Remark Existing
                    //(dbParam[0] = new OleDbParameter("@pnType", OleDbType.TinyInt)).Value = _intType;
                    //(dbParam[1] = new OleDbParameter("@pnTranType", OleDbType.TinyInt)).Value = intSwcType;
                    //20150820, liliana, LIBST13020, begin
                    //(dbParam[2] = new OleDbParameter("@pcTranCode", OleDbType.Char, 8)).Value = "";
                    //(dbParam[2] = new OleDbParameter("@pcTranCode", OleDbType.Char, 8)).Value = textNoTransaksiSwc.Text;
                    //20150820, liliana, LIBST13020, end
                    //(dbParam[3] = new OleDbParameter("@pnTranId", OleDbType.TinyInt)).Value = 1;
                    //(dbParam[4] = new OleDbParameter("@pdTranDate", OleDbType.Date)).Value = dateTglTransaksiSwc.Value;
                    //(dbParam[5] = new OleDbParameter("@pnProdIdSwcOut", OleDbType.Integer)).Value = int.Parse(cmpsrProductSwcOut[2].ToString());
                    //(dbParam[6] = new OleDbParameter("@pnProdIdSwcIn", OleDbType.Integer)).Value = int.Parse(cmpsrProductSwcIn[2].ToString());
                    //(dbParam[7] = new OleDbParameter("@pnClientIdSwcOut", OleDbType.Integer)).Value = int.Parse(cmpsrClientSwcOut[2].ToString());
                    //(dbParam[8] = new OleDbParameter("@pnClientIdSwcIn", OleDbType.Integer)).Value = intClientIdSwcIn;

                    //(dbParam[9] = new OleDbParameter("@pnFundIdSwcOut", OleDbType.Integer)).Value = 0;
                    //(dbParam[10] = new OleDbParameter("@pnFundIdSwcIn", OleDbType.Integer)).Value = 0;

                    //20150505, liliana, LIBST13020, begin
                    //(dbParam[11] = new OleDbParameter("@pcSelectedAccNo", OleDbType.VarChar, 20)).Value = textRekeningSwc.Text;
                    //20150630, liliana, LIBST13020, begin
                    //(dbParam[11] = new OleDbParameter("@pcSelectedAccNo", OleDbType.VarChar, 20)).Value = maskedRekeningSwc.Text.Replace("-", "");
                    //(dbParam[11] = new OleDbParameter("@pcSelectedAccNo", OleDbType.VarChar, 20)).Value = maskedRekeningSwc.Text.Replace("-", "").Trim();
                    //20150630, liliana, LIBST13020, end
                    //20150505, liliana, LIBST13020, end

                    //(dbParam[12] = new OleDbParameter("@pnAgentIdSwcOut", OleDbType.Integer)).Value = 0;
                    //(dbParam[13] = new OleDbParameter("@pnAgentIdSwcIn", OleDbType.Integer)).Value = 0;
                    //(dbParam[14] = new OleDbParameter("@pcTranCCY", OleDbType.Char, 3)).Value = cmpsrProductSwcOut[3].ToString();
                    //(dbParam[15] = new OleDbParameter("@pmTranAmt", OleDbType.Double)).Value = 0;
                    //(dbParam[16] = new OleDbParameter("@pmTranUnit", OleDbType.Double)).Value = (double)nispRedempSwc.Value;
                    //(dbParam[17] = new OleDbParameter("@pmSwitchingFee", OleDbType.Double)).Value = (double)nispMoneyFeeSwc.Value;
                    //20150408, liliana, LIBST13020, begin
                    //(dbParam[18] = new OleDbParameter("@pmNAVSwcOut", OleDbType.Double)).Value = 0;
                    //(dbParam[19] = new OleDbParameter("@pmNAVSwcIn", OleDbType.Double)).Value = 0;
                    //(dbParam[18] = new OleDbParameter("@pmNAVSwcOut", OleDbType.Double)).Value = (double)_NAVSwcOutNonRDB;
                    //(dbParam[19] = new OleDbParameter("@pmNAVSwcIn", OleDbType.Double)).Value = (double)_NAVSwcInNonRDB;
                    //20150408, liliana, LIBST13020, end
                    //(dbParam[20] = new OleDbParameter("@pdNAVValueDate", OleDbType.Date)).Value = dateTglTransaksiSwc.Value.AddDays(-1); //lom tau ambil darimana
                    //(dbParam[21] = new OleDbParameter("@pmUnitBalanceSwcOut", OleDbType.Double)).Value = (double)nispOutstandingUnitSwc.Value;
                    //20150408, liliana, LIBST13020, begin
                    //(dbParam[22] = new OleDbParameter("@pmUnitBalanceNomSwcOut", OleDbType.Double)).Value = 0;
                    //(dbParam[23] = new OleDbParameter("@pmUnitBalanceSwcIn", OleDbType.Double)).Value = 0;
                    //(dbParam[24] = new OleDbParameter("@pmUnitBalanceNomSwcIn", OleDbType.Double)).Value = 0;
                    //(dbParam[22] = new OleDbParameter("@pmUnitBalanceNomSwcOut", OleDbType.Double)).Value = decUnitBalanceNomSwcOut;
                    //20150831, liliana, LIBST13020, begin
                    //(dbParam[23] = new OleDbParameter("@pmUnitBalanceSwcIn", OleDbType.Double)).Value = 0;
                    //(dbParam[23] = new OleDbParameter("@pmUnitBalanceSwcIn", OleDbType.Double)).Value = OutstandingUnitSwcIn;
                    //20150831, liliana, LIBST13020, end
                    //(dbParam[24] = new OleDbParameter("@pmUnitBalanceNomSwcIn", OleDbType.Double)).Value = decUnitBalanceNomSwcIn;
                    //20150408, liliana, LIBST13020, end
                    //(dbParam[25] = new OleDbParameter("@pnUserSuid", OleDbType.Integer)).Value = intNIK;
                    //(dbParam[26] = new OleDbParameter("@pbByUnit", OleDbType.Boolean)).Value = true;
                    //(dbParam[27] = new OleDbParameter("@pnSalesId", OleDbType.Integer)).Value = 0;
                    //(dbParam[28] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
                    //(dbParam[29] = new OleDbParameter("@pcWarnMsg", OleDbType.VarChar, 100)).Value = "";
                    //(dbParam[30] = new OleDbParameter("@pcInputter", OleDbType.VarChar, 40)).Value = txtbInputterSwc.Text;
                    //(dbParam[31] = new OleDbParameter("@pnSeller", OleDbType.Integer)).Value = System.Convert.ToInt32(cmpsrSellerSwc.Text1);
                    //(dbParam[32] = new OleDbParameter("@pnWaperd", OleDbType.Integer)).Value = System.Convert.ToInt32(cmpsrWaperdSwc.Text1);
                    //(dbParam[33] = new OleDbParameter("@pbIsFeeEdit", OleDbType.Boolean)).Value = checkFeeEditSwc.Checked;

                    //20150518, liliana, LIBST13020, begin
                    //(dbParam[34] = new OleDbParameter("@pbDocFCSubscriptionForm", OleDbType.Boolean)).Value = false;
                    //(dbParam[35] = new OleDbParameter("@pbDocFCDevidentAuthLetter", OleDbType.Boolean)).Value = false;
                    //(dbParam[36] = new OleDbParameter("@pbDocFCJoinAcctStatementLetter", OleDbType.Boolean)).Value = false;
                    //(dbParam[37] = new OleDbParameter("@pbDocFCIDCopy", OleDbType.Boolean)).Value = false;
                    //(dbParam[38] = new OleDbParameter("@pbDocFCOthers", OleDbType.Boolean)).Value = false;

                    //(dbParam[39] = new OleDbParameter("@pbDocTCSubscriptionForm", OleDbType.Boolean)).Value = false;
                    //(dbParam[40] = new OleDbParameter("@pbDocTCTermCondition", OleDbType.Boolean)).Value = false;
                    //(dbParam[41] = new OleDbParameter("@pbDocTCProspectus", OleDbType.Boolean)).Value = false;
                    //(dbParam[42] = new OleDbParameter("@pbDocTCFundFactSheet", OleDbType.Boolean)).Value = false;
                    //(dbParam[43] = new OleDbParameter("@pbDocTCOthers", OleDbType.Boolean)).Value = false;

                    //(dbParam[44] = new OleDbParameter("@pcDocFCOthersList", OleDbType.VarChar, 4000)).Value = "";
                    //(dbParam[45] = new OleDbParameter("@pcDocTCOthersList", OleDbType.VarChar, 4000)).Value = "";
                    //(dbParam[34] = new OleDbParameter("@pbDocFCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState);
                    //(dbParam[35] = new OleDbParameter("@pbDocFCDevidentAuthLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState);
                    //(dbParam[36] = new OleDbParameter("@pbDocFCJoinAcctStatementLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState);
                    //(dbParam[37] = new OleDbParameter("@pbDocFCIDCopy", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState);
                    //(dbParam[38] = new OleDbParameter("@pbDocFCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState);

                    //(dbParam[39] = new OleDbParameter("@pbDocTCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState);
                    //(dbParam[40] = new OleDbParameter("@pbDocTCTermCondition", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState);
                    //(dbParam[41] = new OleDbParameter("@pbDocTCProspectus", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState);
                    //(dbParam[42] = new OleDbParameter("@pbDocTCFundFactSheet", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState);
                    //(dbParam[43] = new OleDbParameter("@pbDocTCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState);

                    //(dbParam[44] = new OleDbParameter("@pcDocFCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("FC"); ;
                    //(dbParam[45] = new OleDbParameter("@pcDocTCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("TC"); ;
                    //20150518, liliana, LIBST13020, end

                    //(dbParam[46] = new OleDbParameter("@pcWarnMsg2", OleDbType.VarChar, 100)).Value = "";

                    //(dbParam[47] = new OleDbParameter("@pdPercentageFee", OleDbType.Double)).Value = nispPercentageFeeSwc.Value;
                    //(dbParam[48] = new OleDbParameter("@pbByPhoneOrder", OleDbType.Boolean)).Value = checkPhoneOrderSwc.Checked;
                    //(dbParam[49] = new OleDbParameter("@pcWarnMsg3", OleDbType.VarChar, 100)).Value = "";

                    //(dbParam[50] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 20)).Value = cmpsrCIFSwc.Text1;
                    //(dbParam[51] = new OleDbParameter("@pcOfficeId", OleDbType.VarChar, 5)).Value = cmpsrKodeKantorSwc.Text1;
                    //(dbParam[52] = new OleDbParameter("@pcRefID", OleDbType.VarChar, 20)).Value = cmpsrNoRefSwc.Text1;
                    //(dbParam[53] = new OleDbParameter("@pbIsNew", OleDbType.Boolean)).Value = IsSubsNew;
                    //(dbParam[54] = new OleDbParameter("@pcClientCodeSwitchInNew", OleDbType.VarChar, 20)).Value = cmpsrClientSwcIn.Text1;
                    //20150414, liliana, LIBST13020, begin
                    //(dbParam[55] = new OleDbParameter("@pnReferentor", OleDbType.Integer)).Value = System.Convert.ToInt32(cmpsrReferentorSwc.Text1);
                    //dbParam[55] = new OleDbParameter("@pnReferentor", OleDbType.Integer);
                    //if (cmpsrReferentorSwc.Text1.Trim() == "")
                    //{
                    //    dbParam[55].Value = 0;
                    //}
                    //else
                    //{
                    //    dbParam[55].Value = System.Convert.ToInt32(cmpsrReferentorSwc.Text1);
                    //}
                    //20150414, liliana, LIBST13020, end
                    //20150828, liliana, LIBST13020, begin
                    //(dbParam[56] = new OleDbParameter("@pcWarnMsg4", OleDbType.VarChar, 400)).Value = "";
                    //(dbParam[57] = new OleDbParameter("@pcWarnMsg5", OleDbType.VarChar, 400)).Value = "";
                    //20150828, liliana, LIBST13020, end
                    //20160829, liliana, LOGEN00196, begin
                    //(dbParam[58] = new OleDbParameter("@pbTrxTaxAmnesty", OleDbType.Boolean)).Value = bIsTax;
                    //20160829, liliana, LOGEN00196, end

                    //20150820, liliana, LIBST13020, begin
                    //dbParam[2].Direction = ParameterDirection.Output;
                    //20150820, liliana, LIBST13020, end
                    //dbParam[29].Direction = ParameterDirection.Output;
                    //dbParam[46].Direction = ParameterDirection.Output;
                    //dbParam[49].Direction = ParameterDirection.Output;
                    //dbParam[52].Direction = ParameterDirection.InputOutput;
                    //20150828, liliana, LIBST13020, begin
                    //dbParam[56].Direction = ParameterDirection.Output;
                    //dbParam[57].Direction = ParameterDirection.Output;
                    //20150828, liliana, LIBST13020, end

                    //20210922, Lita, RDN-674, begin
                    //(dbParam[59] = new OleDbParameter("@pcNoRek", OleDbType.VarChar, 20)).Value = cmpsrNoRekSwc.Text1;
                    //(dbParam[60] = new OleDbParameter("@pcNoRekCcy", OleDbType.VarChar, 4)).Value = cmpsrNoRekSwc.Text2;
                    //20210922, Lita, RDN-674, end

                    //bool blnResult = ClQ.ExecProc("dbo.ReksaMaintainSwitching", ref dbParam, out dsQueryResult);

                    #endregion Remark Existing

                    #region hit API
                    DataSet dsUrl = new DataSet();
                    string strUrlAPI = "";
                    string _strGuid = "";
                    bool blnResult = false;

                    _strGuid = Guid.NewGuid().ToString();
                    if (_cProc.GetAPIParam("TRX_ReksaMaintainSwitching", out dsUrl))
                    {
                        strUrlAPI = dsUrl.Tables[0].Rows[0]["ParamVal"].ToString();
                    }
                    _ReksaMaintainSwitchingRq = new ReksaMaintainSwitchingRq();
                    _ReksaMaintainSwitchingRq.MessageGUID = _strGuid;
                    _ReksaMaintainSwitchingRq.ParentMessageGUID = null;
                    _ReksaMaintainSwitchingRq.TransactionMessageGUID = _strGuid;
                    _ReksaMaintainSwitchingRq.IsResponseMessage = "false";
                    _ReksaMaintainSwitchingRq.UserNIK = intNIK.ToString();
                    _ReksaMaintainSwitchingRq.ModuleName = strModule;
                    _ReksaMaintainSwitchingRq.MessageDateTime = DateTime.Now.ToString();
                    _ReksaMaintainSwitchingRq.DestinationURL = strUrlAPI;
                    _ReksaMaintainSwitchingRq.IsSuccess = "true";
                    _ReksaMaintainSwitchingRq.ErrorCode = "";
                    _ReksaMaintainSwitchingRq.ErrorDescription = "";
                    //req data 
                    _ReksaMaintainSwitchingRq.Data.pnType = _intType;
                    _ReksaMaintainSwitchingRq.Data.pnTranType = intSwcType;
                    _ReksaMaintainSwitchingRq.Data.pcTranCode = textNoTransaksiSwc.Text;
                    //_ReksaMaintainSwitchingRq.Data.pnTranId = 1;
                    _ReksaMaintainSwitchingRq.Data.pdTranDate = dateTglTransaksiSwc.Value.ToString();
                    _ReksaMaintainSwitchingRq.Data.pnProdIdSwcOut = int.Parse(cmpsrProductSwcOut[2].ToString());
                    _ReksaMaintainSwitchingRq.Data.pnProdIdSwcIn = int.Parse(cmpsrProductSwcIn[2].ToString());
                    _ReksaMaintainSwitchingRq.Data.pnClientIdSwcOut = int.Parse(cmpsrClientSwcOut[2].ToString());
                    _ReksaMaintainSwitchingRq.Data.pnClientIdSwcIn = intClientIdSwcIn;
                    _ReksaMaintainSwitchingRq.Data.pnFundIdSwcOut = 0;
                    _ReksaMaintainSwitchingRq.Data.pnFundIdSwcIn = 0;
                    _ReksaMaintainSwitchingRq.Data.pcSelectedAccNo = maskedRekeningSwc.Text.Replace("-", "").Trim();
                    _ReksaMaintainSwitchingRq.Data.pnAgentIdSwcOut = 0;
                    _ReksaMaintainSwitchingRq.Data.pnAgentIdSwcIn = 0;
                    _ReksaMaintainSwitchingRq.Data.pcTranCCY = cmpsrProductSwcOut[3].ToString();
                    _ReksaMaintainSwitchingRq.Data.pmTranAmt = 0;
                    _ReksaMaintainSwitchingRq.Data.pmTranUnit = (decimal)nispRedempSwc.Value;
                    _ReksaMaintainSwitchingRq.Data.pmSwitchingFee = (decimal)nispMoneyFeeSwc.Value;
                    _ReksaMaintainSwitchingRq.Data.pmNAVSwcOut = (decimal)_NAVSwcOutNonRDB;
                    _ReksaMaintainSwitchingRq.Data.pmNAVSwcIn = (decimal)_NAVSwcInNonRDB;
                    _ReksaMaintainSwitchingRq.Data.pdNAVValueDate = dateTglTransaksiSwc.Value.AddDays(-1).ToString();
                    _ReksaMaintainSwitchingRq.Data.pmUnitBalanceSwcOut = (decimal)nispOutstandingUnitSwc.Value;
                    _ReksaMaintainSwitchingRq.Data.pmUnitBalanceNomSwcOut = decUnitBalanceNomSwcOut;
                    _ReksaMaintainSwitchingRq.Data.pmUnitBalanceSwcIn = OutstandingUnitSwcIn;
                    _ReksaMaintainSwitchingRq.Data.pmUnitBalanceNomSwcIn = decUnitBalanceNomSwcIn;
                    _ReksaMaintainSwitchingRq.Data.pnUserSuid = intNIK;
                    _ReksaMaintainSwitchingRq.Data.pbByUnit = 1;
                    _ReksaMaintainSwitchingRq.Data.pnSalesId = 0;
                    _ReksaMaintainSwitchingRq.Data.pcGuid = strGuid;
                    //_ReksaMaintainSwitchingRq.Data.pcWarnMsg = "";
                    _ReksaMaintainSwitchingRq.Data.pcInputter = txtbInputterSwc.Text;
                    _ReksaMaintainSwitchingRq.Data.pnSeller = Int32.Parse(cmpsrSellerSwc.Text1.ToString());
                    _ReksaMaintainSwitchingRq.Data.pnWaperd = Int32.Parse(cmpsrWaperdSwc.Text1.ToString());
                    _ReksaMaintainSwitchingRq.Data.pbIsFeeEdit = checkFeeEditSwc.Checked ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocFCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocFCDevidentAuthLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocFCJoinAcctStatementLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocFCIDCopy = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocFCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocTCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocTCTermCondition = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocTCProspectus = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocTCFundFactSheet = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pbDocTCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pcDocFCOthersList = objFormDocument.GetOthersList("FC");
                    _ReksaMaintainSwitchingRq.Data.pcDocTCOthersList = objFormDocument.GetOthersList("TC");
                    //_ReksaMaintainSwitchingRq.Data.pcWarnMsg2 = "";
                    _ReksaMaintainSwitchingRq.Data.pdPercentageFee = nispPercentageFeeSwc.Value;
                    _ReksaMaintainSwitchingRq.Data.pbByPhoneOrder = checkPhoneOrderSwc.Checked ? 1 : 0;
                    //_ReksaMaintainSwitchingRq.Data.pcWarnMsg3 = "";
                    _ReksaMaintainSwitchingRq.Data.pcCIFNo = cmpsrCIFSwc.Text1;
                    _ReksaMaintainSwitchingRq.Data.pcOfficeId = cmpsrKodeKantorSwc.Text1;
                    _ReksaMaintainSwitchingRq.Data.pcRefID = cmpsrNoRefSwc.Text1;
                    _ReksaMaintainSwitchingRq.Data.pbIsNew = IsSubsNew ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pcClientCodeSwitchInNew = cmpsrClientSwcIn.Text1;
                    if (cmpsrReferentorSwc.Text1.Trim() == "")
                    {
                        _ReksaMaintainSwitchingRq.Data.pnReferentor = 0;
                    }
                    else
                    {
                        _ReksaMaintainSwitchingRq.Data.pnReferentor = System.Convert.ToInt32(cmpsrReferentorSwc.Text1);
                    }
                    //_ReksaMaintainSwitchingRq.Data.pcWarnMsg4 = "";
                    //_ReksaMaintainSwitchingRq.Data.pcWarnMsg5 = "";
                    _ReksaMaintainSwitchingRq.Data.pbTrxTaxAmnesty = bIsTax ? 1 : 0;
                    _ReksaMaintainSwitchingRq.Data.pcNoRek = cmpsrNoRekSwc.Text1;
                    _ReksaMaintainSwitchingRq.Data.pcNoRekCcy = cmpsrNoRekSwc.Text2;


                    //end
                    ReksaMaintainSwitchingRs _response = _iServiceAPI.ReksaMaintainSwitching(_ReksaMaintainSwitchingRq);

                    if (_response.IsSuccess == true)
                    {
                        blnResult = true;
                    }
                    else
                    {
                        blnResult = false;
                        if (_response.ErrorDescription != "")
                        {
                            MessageBox.Show(_response.ErrorDescription);
                            return;
                        }
                    }
                    #endregion hit API
                    //20230206, Antonius Filian, RDN-903, end

                    if (blnResult)
                    {
                        //20230206, Antonius Filian, RDN-903, begin
                        if (_response.Data.cErrorMsg != "")
                        {
                            MessageBox.Show("Data gagal disimpan!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            //20150828, liliana, LIBST13020, begin
                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg4.ToString() != "")
                            {
                                if (MessageBox.Show(_response.Data.pcWarnMsg4.ToString(), "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                {
                                    MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }
                            //20230206, Antonius Filian, RDN-903, end

                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg5.ToString() != "")
                            {
                                MessageBox.Show(_response.Data.pcWarnMsg5.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            //20230206, Antonius Filian, RDN-903, end

                            //20150828, liliana, LIBST13020, end
                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg2.ToString() != "")
                            {
                                MessageBox.Show("Profil Risiko produk lebih tinggi dari Profil Risiko Nasabah . PASTIKAN Nasabah sudah menandatangani kolom Profil Risiko pada Subscription/Switching Form", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            //20230206, Antonius Filian, RDN-903, end

                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg3.ToString() != "")
                            {
                                MessageBox.Show("Umur nasabah 55 tahun atau lebih, Mohon dipastikan nasabah menandatangani pernyataan pada kolom yang disediakan di Formulir Subscription/Switching", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            //20230206, Antonius Filian, RDN-903, end

                            //20220121, sandi, RDN-727, begin
                            string rekSwcIDR = maskedRekeningSwc.Text.Replace("-", "").Trim();
                            string rekSwcUSD = maskedRekeningSwcUSD.Text.Replace("-", "").Trim();
                            string rekSwcMC = maskedRekeningSwcMC.Text.Replace("-", "").Trim();

                            if (cmpsrNoRekSwc.Text1 != rekSwcIDR && cmpsrNoRekSwc.Text1 != rekSwcUSD && cmpsrNoRekSwc.Text1 != rekSwcMC)
                            {
                                MessageBox.Show("Rekening " + cmpsrNoRekSwc.Text1 + " akan disimpan di Master Nasabah untuk pembagian deviden dan pendebetan RDB (jika ada)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            //20220121, sandi, RDN-727, end

                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg.ToString() != "")
                            {
                                MessageBox.Show("Transaksi Telah Tersimpan, Perlu Otorisasi Supervisor! No Referensi: " + _response.Data.pcRefID.ToString() + "\n" + _response.Data.pcWarnMsg.ToString(), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            }
                            else
                            {
                                MessageBox.Show("Transaksi Telah Tersimpan, Perlu Otorisasi Supervisor! No Referensi: " + _response.Data.pcRefID.ToString(), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            }
                            //20230206, Antonius Filian, RDN-903, end

                            //20221020, Andi, HFUNDING-178, begin
                            //20230206, Antonius Filian, RDN-903, begin
                            //ReksaSalesEksekutifTransaksi(dbParam[52].Value.ToString(), textBoxKodeSalesSwcNonRdb.Text,
                            //    richTextBoxKeteranganSwcNonRdb.Text,
                            //    DateTime.Today, intNIK, DateTime.Today, intNIK);
                            //20230511, Andhika J, RDN-903, begin
                            //ReksaSalesEksekutifTransaksi(_ReksaMaintainNewBookingRq.Data.cRefID.ToString(), textBoxKodeSalesSwcNonRdb.Text,
                            //    richTextBoxKeteranganSwcNonRdb.Text,
                            //    DateTime.Today, intNIK, DateTime.Today, intNIK);
                            ReksaSalesEksekutifTransaksi(_response.Data._pcRefID.ToString(), textBoxKodeSalesSwcNonRdb.Text,
                                richTextBoxKeteranganSwcNonRdb.Text,
                                DateTime.Today, intNIK, DateTime.Today, intNIK);
                            //20230511, Andhika J, RDN-903, end
                            //20230206, Antonius Filian, RDN-903, end
                            //20221020, Andi, HFUNDING-178, end
                            //20150724, liliana, LIBST13020, begin
                            //20150728, liliana, LIBST13020, end
                            ResetForm();
                            DisableAllForm(false);
                            //20150728, liliana, LIBST13020, end
                            cmpsrNoRefSwc.Text1 = _response.Data.pcRefID.ToString();
                            cmpsrNoRefSwc.ValidateField();
                            //20150724, liliana, LIBST13020, end
                            //20150710, liliana, LIBST13020, begin
                            //subCancel();
                            subRefresh();
                            //20150710, liliana, LIBST13020, end
                        }
                    }
                }
                catch (FormatException fex)
                {
                    MessageBox.Show(fex.ToString());
                    MessageBox.Show("Data yang diinput dalam format yang tidak valid\nAtau ada field mandatory yang tidak diisi\nMohon periksa kembali data yang diinput");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }
            else if (_strTabName == "SWCRDB")
            {
                //20160509, Elva, CSODD16117, begin
                if (cmpsrKodeKantorSwcRDB.Text1 == "")
                {
                    MessageBox.Show("Kode Kantor harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string strIsAllowed = "";
                if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorSwcRDB.Text1, out strIsAllowed))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormSwcRDB();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string strErrorMessage;
                strIsAllowed = "";
                if (clsValidator.ValidasiUserCBO(ClQ, cmpsrKodeKantorSwcRDB.Text1, strBranch, out strIsAllowed, out strErrorMessage))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateUserCBOOffice], " + strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormSwcRDB();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateUserCBOOffice]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //20160509, Elva, CSODD16117, end
                if (cmpsrCIFSwcRDB.Text1 == "")
                {
                    MessageBox.Show("CIF harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!GlobalFunctionCIF.RetrieveCIFData(intNIK, strBranch, strModule, strGuid, Int64.Parse(cmpsrCIFSwcRDB.Text1)))
                {
                    MessageBox.Show("Gagal validasi CIF ke modul ProCIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((checkPhoneOrderSwcRDB.Checked) && (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSwcRDB.Text1)))
                {
                    MessageBox.Show("Nasabah tidak memiliki fasilitas phone order!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20150326, liliana, LIBST13020, begin
                //if (textSIDSwcRDB.Text == "")
                //{
                //    MessageBox.Show("SID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150326, liliana, LIBST13020, end

                if (textShareHolderIdSwcRDB.Text == "")
                {
                    MessageBox.Show("Shareholder ID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, begin

                if (cmbTASwcRDB.SelectedIndex == -1)
                {
                    MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, end
                //20150505, liliana, LIBST13020, begin
                ////20150505, liliana, LIBST13020, begin
                ////if (textRekeningSwcRDB.Text == "")
                //if (maskedRekeningSwcRDB.Text == "")
                ////20150505, liliana, LIBST13020, end
                //{
                //    MessageBox.Show("Nomor rekening harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (textNamaRekeningSwcRDB.Text == "")
                //{
                //    MessageBox.Show("Nama rekening harus terisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150505, liliana, LIBST13020, end

                //20150622, liliana, LIBST13020, begin
                //if (cmpsrSellerSwcRDB.Text1 == "")
                //{
                //    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (cmpsrWaperdSwcRDB.Text1 == "")
                //{
                //    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150622, liliana, LIBST13020, end

                if (cmpsrProductSwcRDBOut.Text1 == "")
                {
                    MessageBox.Show("Produk Switch Out Switching RDB belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrProductSwcRDBIn.Text1 == "")
                {
                    MessageBox.Show("Produk Switch In Switching RDB belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrClientSwcRDBOut.Text1 == "")
                {
                    MessageBox.Show("Client Code Switch Out Switching RDB belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                //20150813, liliana, LIBST13020, begin
                //if (cmpsrClientSwcRDBIn.Text1 == "")
                if ((cmpsrClientSwcRDBIn.Text1 == "") && (IsSubsNew == false))
                //20150813, liliana, LIBST13020, end
                {
                    MessageBox.Show("Client Code Switch In Switching RDB belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (nispRedempSwcRDB.Value == 0)
                {
                    MessageBox.Show("Jumlah unit Switching RDB harus diisi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20150921, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150921, liliana, LIBST13020, end
                    if (nispRedempSwcRDB.Value > nispOutstandingUnitSwcRDB.Value)
                    {
                        MessageBox.Show("Jumlah unit Switching RDB tidak boleh lebih besar dari Outstanding Unit!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150921, liliana, LIBST13020, begin
                }
                //20150921, liliana, LIBST13020, end

                //20150904, liliana, LIBST13020, begin
                if (nispJangkaWktSwcRDB.Value > 0)
                {
                    //20150904, liliana, LIBST13020, end
                    if (cmbFrekPendebetanSwcRDB.Text == "")
                    {
                        MessageBox.Show("Data Frekuensi pendebetan tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    //20150904, liliana, LIBST13020, begin
                    //if (nispJangkaWktSwcRDB.Value == 0)
                    //{
                    //    MessageBox.Show("Jangka Waktu tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //    return;
                    //}
                    //20150904, liliana, LIBST13020, end

                    if (cmbAutoRedempSwcRDB.Text == "")
                    {
                        MessageBox.Show("Auto Redemption tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (cmbAsuransiSwcRDB.Text == "")
                    {
                        MessageBox.Show("Asuransi tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150904, liliana, LIBST13020, begin
                }
                //20150904, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, begin
                if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Formulir Subscription wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState))
                {
                    MessageBox.Show("Copy Bukti Identitas wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Copy Formulir Subscription wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState))
                {
                    MessageBox.Show("Prospektus wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState))
                {
                    MessageBox.Show("Fund Fact Sheet wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                //20150518, liliana, LIBST13020, end
                //20150622, liliana, LIBST13020, begin
                if (cmpsrSellerSwcRDB.Text1 == "")
                {
                    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrWaperdSwcRDB.Text1 == "")
                {
                    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150622, liliana, LIBST13020, end

                //cek risk profile
                string RiskProfile;
                TimeSpan diff;
                DateTime LastUpdateRiskProfile;
                //20170825, liliana, COPOD17271, begin
                //CekRiskProfile(cmpsrCIFSwcRDB.Text1, dateTglTransaksiSwcRDB.Value, out RiskProfile, out LastUpdateRiskProfile, out diff);

                DateTime ExpRiskProfile;
                CekRiskProfile(cmpsrCIFSwcRDB.Text1, dateTglTransaksiSwcRDB.Value, out RiskProfile, out LastUpdateRiskProfile,
                    out ExpRiskProfile,
                    out diff);
                //20170825, liliana, COPOD17271, end

                if (RiskProfile.Trim() == "")
                {
                    MessageBox.Show("CIF : " + cmpsrCIFSwcRDB.Text1 + "\nData risk profile harus dilengkapi di Pro CIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20170825, liliana, COPOD17271, begin
                //if (diff.Days >= 365)
                if (ExpRiskProfile < System.DateTime.Today)
                //20170825, liliana, COPOD17271, end
                {
                    if (MessageBox.Show("CIF : " + cmpsrCIFSwcRDB.Text1 +
                        "\nTanggal Last Update Risk Profile : " + LastUpdateRiskProfile.ToString("dd-MMM-yyyy") +
                        //20170825, liliana, COPOD17271, begin
                        //"\nTanggal Last Update risk profile sudah lewat dari satu tahun" +
                        //"\nLast Update Risk Profile telah lebih dari 1 tahun, apakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        "\nTanggal Last Update risk profile sudah expired" +
                        "\nApakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    //20170825, liliana, COPOD17271, end
                    {
                        MessageBox.Show("Lakukan Perubahan Risk Profile di Pro CIF-Menu Inquiry and Maintenance-Data Pribadi", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else
                    {
                        OleDbParameter[] Param2 = new OleDbParameter[2];
                        (Param2[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = cmpsrCIFSwcRDB.Text1;
                        (Param2[1] = new OleDbParameter("@pdNewLastUpdate", OleDbType.Date)).Value = dateTglTransaksiSwcRDB.Value;

                        if (!ClQ.ExecProc("dbo.ReksaManualUpdateRiskProfile", ref Param2))
                        {
                            MessageBox.Show("Gagal simpan last update risk profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                //20210120, julio, RDN-410, begin
                //check email
                String EmailWarning;
                CekElecAddress(cmpsrCIFSwcRDB.Text1, out EmailWarning);
                if (EmailWarning.Trim() != "")
                {
                    if (MessageBox.Show(EmailWarning, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        //20210519, joshua, RDN-540, begin
                        //MessageBox.Show("Lengkapi email Nasabah pada ProCIF untuk mengunduh Surat Konfirmasi pada Akses KSEI. Permintaan report tercetak akan ada potensi dikenakan biaya", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show("Lengkapi email Nasabah pada ProCIF dan ProReksa agar terdaftar dan dapat mengunduh laporan di AKSes KSEI", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //20210519, joshua, RDN-540, begin
                        return;
                    }
                }
                //20210120, julio, RDN-410, end
                //set switching type 
                int intSwcType, intClientIdSwcIn, intAutoRedemp, intAsuransi;
                intSwcType = 9;

                if (IsSubsNew)
                {
                    intClientIdSwcIn = 0;
                }
                else
                {
                    intClientIdSwcIn = int.Parse(cmpsrClientSwcRDBIn[2].ToString());
                }

                if (cmbAutoRedempSwcRDB.Text == "YA")
                {
                    intAutoRedemp = 1;
                }
                else
                {
                    intAutoRedemp = 0;
                }
                if (cmbAsuransiSwcRDB.Text == "YA")
                {
                    intAsuransi = 1;
                }
                else
                {
                    intAsuransi = 0;
                }
                //20160829, liliana, LOGEN00196, begin
                bool bIsTax;
                bIsTax = false;

                if (cmbTASwcRDB.SelectedIndex == 1)
                {
                    bIsTax = true;
                }
                //20160829, liliana, LOGEN00196, end

                //20230206, Antonius Filian, RDN-903, begin
                #region Remark Existing

                //DataSet dsQueryResult;
                //20150518, liliana, LIBST13020, begin
                //OleDbParameter[] dbParam = new OleDbParameter[36];
                //20150828, liliana, LIBST13020, begin
                //OleDbParameter[] dbParam = new OleDbParameter[48];
                //20160829, liliana, LOGEN00196, begin
                //OleDbParameter[] dbParam = new OleDbParameter[50];
                //20210922, lita, RDN-674, begin
                //OleDbParameter[] dbParam = new OleDbParameter[51];
                //OleDbParameter[] dbParam = new OleDbParameter[53];
                //20210922, lita, RDN-674, end
                //20160829, liliana, LOGEN00196, end
                //20150828, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, end

                #endregion Remark Existing
                //20230206, Antonius Filian, RDN-903, end
                try
                {
                    //20230206, Antonius Filian, RDN-903, begin
                    #region Remark Existing

                    //(dbParam[0] = new OleDbParameter("@pnType", OleDbType.TinyInt)).Value = _intType;
                    //(dbParam[1] = new OleDbParameter("@pnTranType", OleDbType.TinyInt)).Value = intSwcType;
                    //20150820, liliana, LIBST13020, begin
                    //(dbParam[2] = new OleDbParameter("@pcTranCode", OleDbType.Char, 8)).Value = "";
                    //(dbParam[2] = new OleDbParameter("@pcTranCode", OleDbType.Char, 8)).Value = textNoTransaksiSwcRDB.Text;
                    //20150820, liliana, LIBST13020, end
                    //(dbParam[3] = new OleDbParameter("@pnTranId", OleDbType.TinyInt)).Value = 1;
                    //(dbParam[4] = new OleDbParameter("@pdTranDate", OleDbType.Date)).Value = dateTglTransaksiSwcRDB.Value;
                    //(dbParam[5] = new OleDbParameter("@pnProdIdSwcOut", OleDbType.Integer)).Value = int.Parse(cmpsrProductSwcRDBOut[2].ToString());
                    //(dbParam[6] = new OleDbParameter("@pnProdIdSwcIn", OleDbType.Integer)).Value = int.Parse(cmpsrProductSwcRDBIn[2].ToString());
                    //(dbParam[7] = new OleDbParameter("@pnClientIdSwcOut", OleDbType.Integer)).Value = int.Parse(cmpsrClientSwcRDBOut[2].ToString());
                    //(dbParam[8] = new OleDbParameter("@pnClientIdSwcIn", OleDbType.Integer)).Value = intClientIdSwcIn;
                    //20150505, liliana, LIBST13020, begin
                    //(dbParam[9] = new OleDbParameter("@pcSelectedAccNo", OleDbType.VarChar, 20)).Value = textRekeningSwcRDB.Text;
                    //20150630, liliana, LIBST13020, begin
                    //(dbParam[9] = new OleDbParameter("@pcSelectedAccNo", OleDbType.VarChar, 20)).Value = maskedRekeningSwcRDB.Text.Replace("-", "");
                    //(dbParam[9] = new OleDbParameter("@pcSelectedAccNo", OleDbType.VarChar, 20)).Value = maskedRekeningSwcRDB.Text.Replace("-", "").Trim();
                    //20150630, liliana, LIBST13020, end
                    //20150505, liliana, LIBST13020, end
                    //(dbParam[10] = new OleDbParameter("@pmTranUnit", OleDbType.Double)).Value = (double)nispRedempSwcRDB.Value;
                    //(dbParam[11] = new OleDbParameter("@pmSwitchingFee", OleDbType.Double)).Value = (double)nispMoneyFeeSwcRDB.Value;
                    //(dbParam[12] = new OleDbParameter("@pmUnitBalanceSwcOut", OleDbType.Double)).Value = (double)nispOutstandingUnitSwcRDB.Value;
                    //(dbParam[13] = new OleDbParameter("@pnJangkaWaktu", OleDbType.Integer)).Value = (int)nispJangkaWktSwcRDB.Value;
                    //(dbParam[14] = new OleDbParameter("@pdJatuhTempo", OleDbType.Integer)).Value = dtJatuhTempoSwcRDB.Value;
                    //(dbParam[15] = new OleDbParameter("@pnFrekPendebetan", OleDbType.Integer)).Value = System.Convert.ToInt32(cmbFrekPendebetanSwcRDB.Text);
                    //(dbParam[16] = new OleDbParameter("@pnAutoRedemption", OleDbType.TinyInt)).Value = intAutoRedemp;
                    //(dbParam[17] = new OleDbParameter("@pnAsuransi", OleDbType.TinyInt)).Value = intAsuransi;
                    //(dbParam[18] = new OleDbParameter("@pnUserSuid", OleDbType.Integer)).Value = intNIK;
                    //20150414, liliana, LIBST13020, begin
                    //(dbParam[19] = new OleDbParameter("@pnReferentor", OleDbType.Integer)).Value = System.Convert.ToInt32(cmpsrReferentorSwcRDB.Text1);

                    //dbParam[19] = new OleDbParameter("@pnReferentor", OleDbType.Integer);
                    //if (cmpsrReferentorSwcRDB.Text1.Trim() == "")
                    //{
                    //    dbParam[19].Value = 0;
                    //}
                    //else
                    //{
                    //    dbParam[19].Value = System.Convert.ToInt32(cmpsrReferentorSwcRDB.Text1);
                    //}
                    //20150414, liliana, LIBST13020, end

                    //(dbParam[20] = new OleDbParameter("@pbByUnit", OleDbType.Boolean)).Value = true;
                    //(dbParam[21] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
                    //(dbParam[22] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 20)).Value = cmpsrCIFSwcRDB.Text1;
                    //(dbParam[23] = new OleDbParameter("@pcOfficeId", OleDbType.VarChar, 5)).Value = cmpsrKodeKantorSwcRDB.Text1;
                    //(dbParam[24] = new OleDbParameter("@pcRefID", OleDbType.VarChar, 20)).Value = cmpsrNoRefSwcRDB.Text1;
                    //(dbParam[25] = new OleDbParameter("@pbIsNew", OleDbType.Boolean)).Value = IsSubsNew;
                    //(dbParam[26] = new OleDbParameter("@pcClientCodeSwitchInNew", OleDbType.VarChar, 20)).Value = cmpsrClientSwcRDBIn.Text1;
                    //(dbParam[27] = new OleDbParameter("@pcInputter", OleDbType.VarChar, 40)).Value = txtbInputterSwcRDB.Text;
                    //(dbParam[28] = new OleDbParameter("@pnSeller", OleDbType.Integer)).Value = System.Convert.ToInt32(cmpsrSellerSwcRDB.Text1);
                    //(dbParam[29] = new OleDbParameter("@pnWaperd", OleDbType.Integer)).Value = System.Convert.ToInt32(cmpsrWaperdSwcRDB.Text1);
                    //(dbParam[30] = new OleDbParameter("@pbIsFeeEdit", OleDbType.Boolean)).Value = checkFeeEditSwcRDB.Checked;
                    //(dbParam[31] = new OleDbParameter("@pdPercentageFee", OleDbType.Double)).Value = nispPercentageFeeSwcRDB.Value;
                    //(dbParam[32] = new OleDbParameter("@pbByPhoneOrder", OleDbType.Boolean)).Value = checkPhoneOrderSwcRDB.Checked;
                    //(dbParam[33] = new OleDbParameter("@pcWarnMsg", OleDbType.VarChar, 100)).Value = "";
                    //(dbParam[34] = new OleDbParameter("@pcWarnMsg2", OleDbType.VarChar, 100)).Value = "";
                    //(dbParam[35] = new OleDbParameter("@pcWarnMsg3", OleDbType.VarChar, 100)).Value = "";
                    //20150518, liliana, LIBST13020, begin
                    //(dbParam[36] = new OleDbParameter("@pbDocFCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState);
                    //(dbParam[37] = new OleDbParameter("@pbDocFCDevidentAuthLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState);
                    //(dbParam[38] = new OleDbParameter("@pbDocFCJoinAcctStatementLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState);
                    //(dbParam[39] = new OleDbParameter("@pbDocFCIDCopy", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState);
                    //(dbParam[40] = new OleDbParameter("@pbDocFCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState);

                    //(dbParam[41] = new OleDbParameter("@pbDocTCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState);
                    //(dbParam[42] = new OleDbParameter("@pbDocTCTermCondition", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState);
                    //(dbParam[43] = new OleDbParameter("@pbDocTCProspectus", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState);
                    //(dbParam[44] = new OleDbParameter("@pbDocTCFundFactSheet", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState);
                    //(dbParam[45] = new OleDbParameter("@pbDocTCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState);

                    //(dbParam[46] = new OleDbParameter("@pcDocFCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("FC"); ;
                    //(dbParam[47] = new OleDbParameter("@pcDocTCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("TC"); ;
                    //20150518, liliana, LIBST13020, end
                    //20150828, liliana, LIBST13020, begin
                    //(dbParam[48] = new OleDbParameter("@pcWarnMsg4", OleDbType.VarChar, 400)).Value = "";
                    //(dbParam[49] = new OleDbParameter("@pcWarnMsg5", OleDbType.VarChar, 400)).Value = "";
                    //20150828, liliana, LIBST13020, end
                    //20160829, liliana, LOGEN00196, begin
                    //(dbParam[50] = new OleDbParameter("@pbTrxTaxAmnesty", OleDbType.Boolean)).Value = bIsTax;
                    //20160829, liliana, LOGEN00196, end

                    //20150820, liliana, LIBST13020, begin
                    //dbParam[2].Direction = ParameterDirection.Output;
                    //20150820, liliana, LIBST13020, end
                    //dbParam[33].Direction = ParameterDirection.Output;
                    //dbParam[34].Direction = ParameterDirection.Output;
                    //dbParam[35].Direction = ParameterDirection.Output;
                    //dbParam[24].Direction = ParameterDirection.InputOutput;
                    //20150828, liliana, LIBST13020, begin
                    //dbParam[48].Direction = ParameterDirection.Output;
                    //dbParam[49].Direction = ParameterDirection.Output;
                    //20150828, liliana, LIBST13020, end
                    //20210922, Lita, RDN-674, begin
                    //(dbParam[51] = new OleDbParameter("@pcNoRek", OleDbType.VarChar, 20)).Value = cmpsrNoRekSwcRDB.Text1;
                    //(dbParam[52] = new OleDbParameter("@pcNoRekCcy", OleDbType.VarChar, 4)).Value = cmpsrNoRekSwcRDB.Text2;
                    //20210922, Lita, RDN-674, end


                    //bool blnResult = ClQ.ExecProc("dbo.ReksaMaintainSwitchingRDB", ref dbParam, out dsQueryResult);

                    #endregion Remark Existing

                    #region hit API
                    DataSet dsUrl = new DataSet();
                    string strUrlAPI = "";
                    string _strGuid = "";
                    bool blnResult = false;

                    _strGuid = Guid.NewGuid().ToString();
                    if (_cProc.GetAPIParam("TRX_ReksaMaintainSwitchingRDB", out dsUrl))
                    {
                        strUrlAPI = dsUrl.Tables[0].Rows[0]["ParamVal"].ToString();
                    }
                    _ReksaMaintainSwitchingRDBRq = new ReksaMaintainSwitchingRDBRq();
                    _ReksaMaintainSwitchingRDBRq.MessageGUID = _strGuid;
                    _ReksaMaintainSwitchingRDBRq.ParentMessageGUID = null;
                    _ReksaMaintainSwitchingRDBRq.TransactionMessageGUID = _strGuid;
                    _ReksaMaintainSwitchingRDBRq.IsResponseMessage = "false";
                    _ReksaMaintainSwitchingRDBRq.UserNIK = intNIK.ToString();
                    _ReksaMaintainSwitchingRDBRq.ModuleName = strModule;
                    _ReksaMaintainSwitchingRDBRq.MessageDateTime = DateTime.Now.ToString();
                    _ReksaMaintainSwitchingRDBRq.DestinationURL = strUrlAPI;
                    _ReksaMaintainSwitchingRDBRq.IsSuccess = "true";
                    _ReksaMaintainSwitchingRDBRq.ErrorCode = "";
                    _ReksaMaintainSwitchingRDBRq.ErrorDescription = "";
                    //req data 
                    _ReksaMaintainSwitchingRDBRq.Data.pnType = _intType;
                    _ReksaMaintainSwitchingRDBRq.Data.pnTranType = intSwcType;
                    _ReksaMaintainSwitchingRDBRq.Data.pcTranCode = textNoTransaksiSwcRDB.Text;
                    _ReksaMaintainSwitchingRDBRq.Data.pnTranId = 1;
                    _ReksaMaintainSwitchingRDBRq.Data.pdTranDate = dateTglTransaksiSwcRDB.Value.ToString();
                    _ReksaMaintainSwitchingRDBRq.Data.pnProdIdSwcOut = int.Parse(cmpsrProductSwcRDBOut[2].ToString());
                    _ReksaMaintainSwitchingRDBRq.Data.pnProdIdSwcIn = int.Parse(cmpsrProductSwcRDBIn[2].ToString());
                    _ReksaMaintainSwitchingRDBRq.Data.pnClientIdSwcOut = int.Parse(cmpsrClientSwcRDBOut[2].ToString());
                    _ReksaMaintainSwitchingRDBRq.Data.pnClientIdSwcIn = intClientIdSwcIn;
                    _ReksaMaintainSwitchingRDBRq.Data.pcSelectedAccNo = maskedRekeningSwcRDB.Text.Replace("-", "").Trim();
                    _ReksaMaintainSwitchingRDBRq.Data.pmTranUnit = (decimal)nispRedempSwcRDB.Value;
                    _ReksaMaintainSwitchingRDBRq.Data.pmSwitchingFee = (decimal)nispMoneyFeeSwcRDB.Value;
                    _ReksaMaintainSwitchingRDBRq.Data.pmUnitBalanceSwcOut = (decimal)nispOutstandingUnitSwcRDB.Value;
                    _ReksaMaintainSwitchingRDBRq.Data.pnJangkaWaktu = (int)nispJangkaWktSwcRDB.Value;
                    _ReksaMaintainSwitchingRDBRq.Data.pdJatuhTempo = dtJatuhTempoSwcRDB.Value.ToString();
                    _ReksaMaintainSwitchingRDBRq.Data.pnFrekuensiPendebetan = System.Convert.ToInt32(cmbFrekPendebetanSwcRDB.Text);
                    _ReksaMaintainSwitchingRDBRq.Data.pnAutoRedemption = intAutoRedemp;
                    _ReksaMaintainSwitchingRDBRq.Data.pnAsuransi = intAsuransi;
                    _ReksaMaintainSwitchingRDBRq.Data.pnUserSuid = intNIK;
                    if (cmpsrReferentorSwcRDB.Text1.Trim() == "")
                    {
                        _ReksaMaintainSwitchingRDBRq.Data.pnReferentor = 0;
                    }
                    else
                    {
                        _ReksaMaintainSwitchingRDBRq.Data.pnReferentor = System.Convert.ToInt32(cmpsrReferentorSwcRDB.Text1);
                    }
                    _ReksaMaintainSwitchingRDBRq.Data.pbByUnit = 1;
                    _ReksaMaintainSwitchingRDBRq.Data.pcGuid = strGuid;
                    _ReksaMaintainSwitchingRDBRq.Data.pcCIFNo = cmpsrCIFSwcRDB.Text1;
                    _ReksaMaintainSwitchingRDBRq.Data.pcOfficeId = cmpsrKodeKantorSwcRDB.Text1;
                    _ReksaMaintainSwitchingRDBRq.Data.pcRefID = cmpsrNoRefSwcRDB.Text1;
                    _ReksaMaintainSwitchingRDBRq.Data.pbIsNew = IsSubsNew ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pcClientCodeSwitchInNew = cmpsrClientSwcRDBIn.Text1;
                    _ReksaMaintainSwitchingRDBRq.Data.pcInputter = txtbInputterSwcRDB.Text;
                    _ReksaMaintainSwitchingRDBRq.Data.pnSeller = System.Convert.ToInt32(cmpsrSellerSwcRDB.Text1);
                    _ReksaMaintainSwitchingRDBRq.Data.pnWaperd = System.Convert.ToInt32(cmpsrWaperdSwcRDB.Text1);
                    _ReksaMaintainSwitchingRDBRq.Data.pbIsFeeEdit = checkFeeEditSwcRDB.Checked ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pdPercentageFee = (double)nispPercentageFeeSwcRDB.Value;
                    _ReksaMaintainSwitchingRDBRq.Data.pbByPhoneOrder = checkPhoneOrderSwcRDB.Checked ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pcWarnMsg = "";
                    _ReksaMaintainSwitchingRDBRq.Data.pcWarnMsg2 = "";
                    _ReksaMaintainSwitchingRDBRq.Data.pcWarnMsg3 = "";
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocFCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocFCDevidentAuthLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocFCJoinAcctStatementLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocFCIDCopy = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocFCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocTCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocTCTermCondition = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocTCProspectus = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocTCFundFactSheet = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pbDocTCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState) ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pcDocFCOthersList = objFormDocument.GetOthersList("FC");
                    _ReksaMaintainSwitchingRDBRq.Data.pcDocTCOthersList = objFormDocument.GetOthersList("TC");
                    _ReksaMaintainSwitchingRDBRq.Data.pcWarnMsg4 = "";
                    _ReksaMaintainSwitchingRDBRq.Data.pcWarnMsg5 = "";
                    _ReksaMaintainSwitchingRDBRq.Data.pbTrxTaxAmnesty = bIsTax ? 1 : 0;
                    _ReksaMaintainSwitchingRDBRq.Data.pcNoRek = cmpsrNoRekSwcRDB.Text1;
                    _ReksaMaintainSwitchingRDBRq.Data.pcNoRekCcy = cmpsrNoRekSwcRDB.Text2;

                    //end
                    ReksaMaintainSwitchingRDBRs _response = _iServiceAPI.ReksaMaintainSwitchingRDB(_ReksaMaintainSwitchingRDBRq);

                    if (_response.IsSuccess == true)
                    {
                        blnResult = true;
                    }
                    else
                    {
                        blnResult = false;
                        if (_response.ErrorDescription != "")
                        {
                            MessageBox.Show(_response.ErrorDescription);
                            return;
                        }
                    }
                    #endregion hit API

                    //20230206, Antonius Filian, RDN-903, end

                    if (blnResult)
                    {
                        //20230206, Antonius Filian, RDN-903, begin
                        if (_response.Data.cErrorMsg != "")
                        {
                            MessageBox.Show("Data gagal disimpan!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            //20150828, liliana, LIBST13020, begin
                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg4.ToString() != "")
                            //20230206, Antonius Filian, RDN-903, end
                            {
                                //20230206, Antonius Filian, RDN-903, begin
                                if (MessageBox.Show(_response.Data.pcWarnMsg4.ToString(), "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                //20230206, Antonius Filian, RDN-903, end
                                {
                                    MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }

                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg5.ToString() != "")
                            //20230206, Antonius Filian, RDN-903, end
                            {
                                //20230206, Antonius Filian, RDN-903, begin
                                MessageBox.Show(_response.Data.pcWarnMsg5.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                //20230206, Antonius Filian, RDN-903, end
                            }

                            //20150828, liliana, LIBST13020, end
                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg2.ToString() != "")
                            //20230206, Antonius Filian, RDN-903, end
                            {
                                MessageBox.Show("Profil Risiko produk lebih tinggi dari Profil Risiko Nasabah . PASTIKAN Nasabah sudah menandatangani kolom Profil Risiko pada Subscription/Switching Form", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }

                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg3.ToString() != "")
                            //20230206, Antonius Filian, RDN-903, end
                            {
                                MessageBox.Show("Umur nasabah 55 tahun atau lebih, Mohon dipastikan nasabah menandatangani pernyataan pada kolom yang disediakan di Formulir Subscription/Switching", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }

                            //20220121, sandi, RDN-727, begin
                            string rekSwcRDBIDR = maskedRekeningSwcRDB.Text.Replace("-", "").Trim();
                            string rekSwcRDBUSD = maskedRekeningSwcRDBUSD.Text.Replace("-", "").Trim();
                            string rekSwcRDBMC = maskedRekeningSwcRDBMC.Text.Replace("-", "").Trim();

                            if (cmpsrNoRekSwcRDB.Text1 != rekSwcRDBIDR && cmpsrNoRekSwcRDB.Text1 != rekSwcRDBUSD && cmpsrNoRekSwcRDB.Text1 != rekSwcRDBMC)
                            {
                                MessageBox.Show("Rekening " + cmpsrNoRekSwcRDB.Text1 + " akan disimpan di Master Nasabah untuk pembagian deviden dan pendebetan RDB (jika ada)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            //20220121, sandi, RDN-727, end

                            //20230206, Antonius Filian, RDN-903, begin
                            if (_response.Data.pcWarnMsg.ToString() != "")
                            //20230206, Antonius Filian, RDN-903, end
                            {
                                //20230206, Antonius Filian, RDN-903, begin
                                MessageBox.Show("Transaksi Telah Tersimpan, Perlu Otorisasi Supervisor! No Referensi: " + _response.Data.pcRefID.ToString() + "\n" + _response.Data.pcWarnMsg.ToString(), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                //20230206, Antonius Filian, RDN-903, end
                            }
                            else
                            {
                                MessageBox.Show("Transaksi Telah Tersimpan, Perlu Otorisasi Supervisor! No Referensi: " + _response.Data.pcRefID.ToString(), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            }

                            //20221020, Andi, HFUNDING-178, begin
                            //20230206, Antonius Filian, RDN-903, begin
                            //ReksaSalesEksekutifTransaksi(dbParam[24].Value.ToString(), textBoxKodeSalesSwcRdb.Text,
                            //    richTextBoxKeteranganSwcRdb.Text,
                            //    DateTime.Today, intNIK, DateTime.Today, intNIK); 
                            //20230511, Andhika J, RDN-903, begin
                            //ReksaSalesEksekutifTransaksi(_ReksaMaintainNewBookingRq.Data.cRefID.ToString(), textBoxKodeSalesSwcRdb.Text,
                            //     richTextBoxKeteranganSwcRdb.Text,
                            //     DateTime.Today, intNIK, DateTime.Today, intNIK);
                            ReksaSalesEksekutifTransaksi(_response.Data.pcRefID.ToString(), textBoxKodeSalesSwcRdb.Text,
                                 richTextBoxKeteranganSwcRdb.Text,
                                 DateTime.Today, intNIK, DateTime.Today, intNIK);
                            //20230511, Andhika J, RDN-903, end
                            //20230206, Antonius Filian, RDN-903, end
                            //20221020, Andi, HFUNDING-178, end
                            //20150724, liliana, LIBST13020, begin
                            //20150728, liliana, LIBST13020, end
                            ResetForm();
                            DisableAllForm(false);
                            //20150728, liliana, LIBST13020, end
                            //20230206, Antonius Filian, RDN-903, begin
                            cmpsrNoRefSwcRDB.Text1 = _response.Data.pcRefID.ToString();
                            //20230206, Antonius Filian, RDN-903, end
                            cmpsrNoRefSwcRDB.ValidateField();
                            //20150724, liliana, LIBST13020, end
                            //20150710, liliana, LIBST13020, begin
                            //subCancel();
                            subRefresh();
                            //20150710, liliana, LIBST13020, end
                        }
                    }
                }
                catch (FormatException fex)
                {
                    MessageBox.Show(fex.ToString());
                    MessageBox.Show("Data yang diinput dalam format yang tidak valid\nAtau ada field mandatory yang tidak diisi\nMohon periksa kembali data yang diinput");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }
            else if (_strTabName == "BOOK")
            {
                decimal decSisaUnit;
                decimal.TryParse(_sisaunit.Text, out decSisaUnit);

                //20160509, Elva, CSODD16117, begin
                if (cmpsrKodeKantorBooking.Text1 == "")
                {
                    MessageBox.Show("Kode Kantor harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string strIsAllowed = "";
                if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorBooking.Text1, out strIsAllowed))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormBooking();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string strErrorMessage;
                strIsAllowed = "";
                if (clsValidator.ValidasiUserCBO(ClQ, cmpsrKodeKantorBooking.Text1, strBranch, out strIsAllowed, out strErrorMessage))
                {
                    if (strIsAllowed == "0")
                    {
                        MessageBox.Show("Error [ReksaValidateUserCBOOffice], " + strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetFormBooking();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error [ReksaValidateUserCBOOffice]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //20160509, Elva, CSODD16117, end
                if (cmpsrCIFBooking.Text1 == "")
                {
                    MessageBox.Show("CIF harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!GlobalFunctionCIF.RetrieveCIFData(intNIK, strBranch, strModule, strGuid, Int64.Parse(cmpsrCIFBooking.Text1)))
                {
                    MessageBox.Show("Gagal validasi CIF ke modul ProCIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((checkPhoneOrderBooking.Checked) && (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFBooking.Text1)))
                {
                    MessageBox.Show("Nasabah tidak memiliki fasilitas phone order!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20150326, liliana, LIBST13020, begin
                //if (textSIDBooking.Text == "")
                //{
                //    MessageBox.Show("SID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150326, liliana, LIBST13020, end

                if (textShareHolderIdBooking.Text == "")
                {
                    MessageBox.Show("Shareholder ID harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, begin

                if (cmbTABook.SelectedIndex == -1)
                {
                    MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, end
                //20150505, liliana, LIBST13020, begin
                ////20150505, liliana, LIBST13020, begin
                ////if (textRekeningBooking.Text == "")
                //if (maskedRekeningBooking.Text == "")
                ////20150505, liliana, LIBST13020, end
                //{
                //    MessageBox.Show("Nomor rekening harus terdaftar", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                //if (textNamaRekeningBooking.Text == "")
                //{
                //    MessageBox.Show("Nama rekening harus terisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                //20150505, liliana, LIBST13020, end

                if (cmpsrSellerBooking.Text1 == "")
                {
                    MessageBox.Show("NIK Seller harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrWaperdBooking.Text1 == "")
                {
                    MessageBox.Show("NIK Seller tidak terdaftar sbg WAPERD", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmpsrProductBooking.Text1 == "")
                {
                    MessageBox.Show("Produk booking belum dipilih", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (nispMoneyNomBooking.Value == 0)
                {
                    MessageBox.Show("Nominal booking tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150518, liliana, LIBST13020, begin
                if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Formulir Subscription wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState))
                {
                    MessageBox.Show("Copy Bukti Identitas wajib ada", "Penerimaan Dokumen dari Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState))
                {
                    MessageBox.Show("Copy Formulir Subscription wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState))
                {
                    MessageBox.Show("Prospektus wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                else if (!System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState))
                {
                    MessageBox.Show("Fund Fact Sheet wajib ada", "Dokumen yang diterima oleh Nasabah");
                    return;
                }
                //20150518, liliana, LIBST13020, end

                //cek risk profile
                string RiskProfile;
                TimeSpan diff;
                DateTime LastUpdateRiskProfile;
                //20170825, liliana, COPOD17271, begin
                //CekRiskProfile(cmpsrCIFBooking.Text1, dateTglTransaksiBooking.Value, out RiskProfile, out LastUpdateRiskProfile, out diff);

                DateTime ExpRiskProfile;
                CekRiskProfile(cmpsrCIFBooking.Text1, dateTglTransaksiBooking.Value, out RiskProfile, out LastUpdateRiskProfile,
                    out ExpRiskProfile,
                    out diff);
                //20170825, liliana, COPOD17271, end

                if (RiskProfile.Trim() == "")
                {
                    MessageBox.Show("CIF : " + cmpsrCIFBooking.Text1 + "\nData risk profile harus dilengkapi di Pro CIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //20170825, liliana, COPOD17271, begin
                //if (diff.Days >= 365)
                if (ExpRiskProfile < System.DateTime.Today)
                //20170825, liliana, COPOD17271, end
                {
                    if (MessageBox.Show("CIF : " + cmpsrCIFBooking.Text1 +
                        "\nTanggal Last Update Risk Profile : " + LastUpdateRiskProfile.ToString("dd-MMM-yyyy") +
                        //20170825, liliana, COPOD17271, begin
                        //"\nTanggal Last Update risk profile sudah lewat dari satu tahun" +
                        //"\nLast Update Risk Profile telah lebih dari 1 tahun, apakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        "\nTanggal Last Update risk profile sudah expired" +
                        "\nApakah Risk Profile Nasabah berubah? ", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    //20170825, liliana, COPOD17271, end
                    {
                        MessageBox.Show("Lakukan Perubahan Risk Profile di Pro CIF-Menu Inquiry and Maintenance-Data Pribadi", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else
                    {
                        OleDbParameter[] Param2 = new OleDbParameter[2];
                        (Param2[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 13)).Value = cmpsrCIFBooking.Text1;
                        (Param2[1] = new OleDbParameter("@pdNewLastUpdate", OleDbType.Date)).Value = dateTglTransaksiBooking.Value;

                        if (!ClQ.ExecProc("dbo.ReksaManualUpdateRiskProfile", ref Param2))
                        {
                            MessageBox.Show("Gagal simpan last update risk profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                //20210120, julio, RDN-410, begin
                //check email
                String EmailWarning;
                CekElecAddress(cmpsrCIFBooking.Text1, out EmailWarning);
                if (EmailWarning.Trim() != "")
                {
                    if (MessageBox.Show(EmailWarning, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        //20210519, joshua, RDN-540, begin
                        //MessageBox.Show("Lengkapi email Nasabah pada ProCIF untuk mengunduh Surat Konfirmasi pada Akses KSEI. Permintaan report tercetak akan ada potensi dikenakan biaya", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show("Lengkapi email Nasabah pada ProCIF dan ProReksa agar terdaftar dan dapat mengunduh laporan di AKSes KSEI", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //20210519, joshua, RDN-540, begin
                        return;
                    }
                }
                //20210120, julio, RDN-410, end
                decimal moneyNominalFee, moneyPercentageFee;

                int ComboJenis = 1;

                if (_ComboJenisBooking.Text == "By %")
                {
                    moneyNominalFee = nispPercentageFeeBooking.Value;
                    moneyPercentageFee = nispMoneyFeeBooking.Value;
                    ComboJenis = 1;
                }
                else
                {
                    moneyNominalFee = nispMoneyFeeBooking.Value;
                    moneyPercentageFee = nispPercentageFeeBooking.Value;
                    ComboJenis = 0;
                }
                //20160829, liliana, LOGEN00196, begin
                bool bIsTax;
                bIsTax = false;

                if (cmbTABook.SelectedIndex == 1)
                {
                    bIsTax = true;
                }
                //20160829, liliana, LOGEN00196, end

                System.Data.DataSet dsSave;
                //20150518, liliana, LIBST13020, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[24];
                //20150831, liliana, LIBST13020, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[36];
                //20160829, liliana, LOGEN00196, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[37];
                //20210922, lita, RDN-674, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[38];
                //20230206, Antonius Filian, RDN-903, begin
                #region Remark Existing
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[40];
                //20210922, lita, RDN-674, end
                //20160829, liliana, LOGEN00196, end
                //20150831, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, end

                //dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnType", System.Data.OleDb.OleDbType.Integer);
                //dbParam[0].Value = _intType;
                //dbParam[0].Direction = System.Data.ParameterDirection.Input;

                //dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnBookingId", System.Data.OleDb.OleDbType.Integer);

                //if (_intType == 1)
                //{
                //    dbParam[1].Value = 0;
                //}
                //else
                //{
                //    dbParam[1].Value = (int)cmpsrNoRefBooking[2];
                //}

                //dbParam[1].Direction = System.Data.ParameterDirection.Input;

                //dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcRefID", System.Data.OleDb.OleDbType.VarChar, 20);
                //dbParam[2].Value = cmpsrNoRefBooking.Text1.ToString();
                //dbParam[2].Direction = System.Data.ParameterDirection.InputOutput;

                //dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcCIF", System.Data.OleDb.OleDbType.VarChar, 13);
                //dbParam[3].Value = cmpsrCIFBooking.Text1.ToString();
                //dbParam[3].Direction = System.Data.ParameterDirection.Input;

                //dbParam[4] = new System.Data.OleDb.OleDbParameter("@pcCIFName", System.Data.OleDb.OleDbType.VarChar, 100);
                //dbParam[4].Value = cmpsrCIFBooking.Text2.ToString();
                //dbParam[4].Direction = System.Data.ParameterDirection.Input;

                //dbParam[5] = new System.Data.OleDb.OleDbParameter("@pnProdId", System.Data.OleDb.OleDbType.Integer);
                //dbParam[5].Value = (int)cmpsrProductBooking[2];
                //dbParam[5].Direction = System.Data.ParameterDirection.Input;

                //dbParam[6] = new System.Data.OleDb.OleDbParameter("@pcOfficeId", System.Data.OleDb.OleDbType.VarChar, 5);
                //dbParam[6].Value = cmpsrKodeKantorBooking.Text1;
                //dbParam[6].Direction = System.Data.ParameterDirection.Input;

                //dbParam[7] = new System.Data.OleDb.OleDbParameter("@pcCurrency", System.Data.OleDb.OleDbType.VarChar, 3);
                //dbParam[7].Value = cmpsrCurrBooking.Text1;
                //dbParam[7].Direction = System.Data.ParameterDirection.Input;

                //dbParam[8] = new System.Data.OleDb.OleDbParameter("@pmNominal", System.Data.OleDb.OleDbType.Double);
                //dbParam[8].Value = System.Convert.ToDouble(nispMoneyNomBooking.Value);
                //dbParam[8].Direction = System.Data.ParameterDirection.Input;

                //dbParam[9] = new System.Data.OleDb.OleDbParameter("@pcRekening", System.Data.OleDb.OleDbType.VarChar, 50);
                //20150505, liliana, LIBST13020, begin
                //dbParam[9].Value = textRekeningBooking.Text;
                //20150630, liliana, LIBST13020, begin
                //dbParam[9].Value = maskedRekeningBooking.Text.Replace("-", "");
                //dbParam[9].Value = maskedRekeningBooking.Text.Replace("-", "").Trim();
                //20150630, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, end
                //dbParam[9].Direction = System.Data.ParameterDirection.Input;

                //dbParam[10] = new System.Data.OleDb.OleDbParameter("@pcNamaRek", System.Data.OleDb.OleDbType.VarChar, 50);
                //dbParam[10].Value = textNamaRekeningBooking.Text;
                //dbParam[10].Direction = System.Data.ParameterDirection.Input;

                //dbParam[11] = new System.Data.OleDb.OleDbParameter("@pnReferentor", System.Data.OleDb.OleDbType.Integer);

                //if (cmpsrReferentorBooking.Text1.Trim() == "")
                //{
                //    dbParam[11].Value = 0;
                //}
                //else
                //{
                //    dbParam[11].Value = System.Convert.ToInt32(cmpsrReferentorBooking.Text1);
                //}
                //dbParam[11].Direction = System.Data.ParameterDirection.Input;


                //dbParam[12] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
                //dbParam[12].Value = intNIK;
                //dbParam[12].Direction = System.Data.ParameterDirection.Input;

                //dbParam[13] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
                //dbParam[13].Value = strGuid;
                //dbParam[13].Direction = System.Data.ParameterDirection.Input;

                //dbParam[14] = new System.Data.OleDb.OleDbParameter("@pcInputter", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[14].Value = txtbInputterBooking.Text.Trim();
                //dbParam[14].Direction = System.Data.ParameterDirection.Input;

                //dbParam[15] = new System.Data.OleDb.OleDbParameter("@pnSeller", System.Data.OleDb.OleDbType.Integer);
                //dbParam[15].Value = int.Parse(cmpsrSellerBooking.Text1.ToString().Trim());
                //dbParam[15].Direction = System.Data.ParameterDirection.Input;

                //dbParam[16] = new System.Data.OleDb.OleDbParameter("@pnWaperd", System.Data.OleDb.OleDbType.Integer);
                //dbParam[16].Value = int.Parse(cmpsrWaperdBooking.Text1.ToString().Trim());
                //dbParam[16].Direction = System.Data.ParameterDirection.Input;

                //dbParam[17] = new System.Data.OleDb.OleDbParameter("@pbIsPhoneOrder", System.Data.OleDb.OleDbType.Boolean);
                //dbParam[17].Value = checkPhoneOrderBooking.Checked;
                //dbParam[17].Direction = System.Data.ParameterDirection.Input;


                //dbParam[18] = new System.Data.OleDb.OleDbParameter("@pbIsFeeEdit", System.Data.OleDb.OleDbType.Boolean);
                //dbParam[18].Value = checkFeeEditBooking.Checked;
                //dbParam[18].Direction = System.Data.ParameterDirection.Input;

                //dbParam[19] = new System.Data.OleDb.OleDbParameter("@pbJenisPerhitunganFee", System.Data.OleDb.OleDbType.Integer);
                //dbParam[19].Value = ComboJenis;
                //dbParam[19].Direction = System.Data.ParameterDirection.Input;

                //dbParam[20] = new System.Data.OleDb.OleDbParameter("@pdPercentageFee", System.Data.OleDb.OleDbType.Double);
                //dbParam[20].Value = moneyPercentageFee;
                //dbParam[20].Direction = System.Data.ParameterDirection.Input;

                //dbParam[21] = new System.Data.OleDb.OleDbParameter("@pmSubcFee", System.Data.OleDb.OleDbType.Double);
                //dbParam[21].Value = moneyNominalFee;
                //dbParam[21].Direction = System.Data.ParameterDirection.Input;

                //dbParam[22] = new System.Data.OleDb.OleDbParameter("@pcWarnMsg", System.Data.OleDb.OleDbType.VarChar, 100);
                //dbParam[22].Value = "";
                //dbParam[22].Direction = System.Data.ParameterDirection.Output;

                //dbParam[23] = new System.Data.OleDb.OleDbParameter("@pcWarnMsg2", System.Data.OleDb.OleDbType.VarChar, 100);
                //dbParam[23].Value = "";
                //dbParam[23].Direction = System.Data.ParameterDirection.Output;
                //20150518, liliana, LIBST13020, begin
                //(dbParam[24] = new OleDbParameter("@pbDocFCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState);
                //(dbParam[25] = new OleDbParameter("@pbDocFCDevidentAuthLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState);
                //(dbParam[26] = new OleDbParameter("@pbDocFCJoinAcctStatementLetter", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState);
                //(dbParam[27] = new OleDbParameter("@pbDocFCIDCopy", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState);
                //(dbParam[28] = new OleDbParameter("@pbDocFCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState);

                //(dbParam[29] = new OleDbParameter("@pbDocTCSubscriptionForm", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState);
                //(dbParam[30] = new OleDbParameter("@pbDocTCTermCondition", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState);
                //(dbParam[31] = new OleDbParameter("@pbDocTCProspectus", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState);
                //(dbParam[32] = new OleDbParameter("@pbDocTCFundFactSheet", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState);
                //(dbParam[33] = new OleDbParameter("@pbDocTCOthers", OleDbType.Boolean)).Value = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState);

                //(dbParam[34] = new OleDbParameter("@pcDocFCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("FC"); ;
                //(dbParam[35] = new OleDbParameter("@pcDocTCOthersList", OleDbType.VarChar, 4000)).Value = objFormDocument.GetOthersList("TC"); ;
                //20150518, liliana, LIBST13020, end
                //20150831, liliana, LIBST13020, begin

                //dbParam[36] = new System.Data.OleDb.OleDbParameter("@pcWarnMsg3", System.Data.OleDb.OleDbType.VarChar, 500);
                //dbParam[36].Value = "";
                //dbParam[36].Direction = System.Data.ParameterDirection.Output;
                //20150831, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin

                //dbParam[37] = new System.Data.OleDb.OleDbParameter("@pbTrxTaxAmnesty", System.Data.OleDb.OleDbType.Boolean);
                //dbParam[37].Value = bIsTax;
                //dbParam[37].Direction = System.Data.ParameterDirection.Input;
                //20160829, liliana, LOGEN00196, end
                //20210922, Lita, RDN-674, begin
                //(dbParam[38] = new OleDbParameter("@pcNoRek", OleDbType.VarChar, 20)).Value = cmpsrNoRekBooking.Text1;
                //(dbParam[39] = new OleDbParameter("@pcNoRekCcy", OleDbType.VarChar, 4)).Value = cmpsrNoRekBooking.Text2;
                //20210922, Lita, RDN-674, end

                //bool blnResult = ClQ.ExecProc("ReksaMaintainNewBooking", ref dbParam, out dsSave);
                #endregion Remark Existing

                #region hit API
                DataSet dsUrl = new DataSet();
                string strUrlAPI = "";
                string _strGuid = "";
                bool blnResult = false;

                _strGuid = Guid.NewGuid().ToString();
                if (_cProc.GetAPIParam("TRX_ReksaMaintainNewBooking", out dsUrl))
                {
                    strUrlAPI = dsUrl.Tables[0].Rows[0]["ParamVal"].ToString();
                }
                _ReksaMaintainNewBookingRq = new ReksaMaintainNewBookingRq();
                _ReksaMaintainNewBookingRq.MessageGUID = _strGuid;
                _ReksaMaintainNewBookingRq.ParentMessageGUID = null;
                _ReksaMaintainNewBookingRq.TransactionMessageGUID = _strGuid;
                _ReksaMaintainNewBookingRq.IsResponseMessage = "false";
                _ReksaMaintainNewBookingRq.UserNIK = intNIK.ToString();
                _ReksaMaintainNewBookingRq.ModuleName = strModule;
                _ReksaMaintainNewBookingRq.MessageDateTime = DateTime.Now.ToString();
                _ReksaMaintainNewBookingRq.DestinationURL = strUrlAPI;
                _ReksaMaintainNewBookingRq.IsSuccess = "true";
                _ReksaMaintainNewBookingRq.ErrorCode = "";
                _ReksaMaintainNewBookingRq.ErrorDescription = "";
                //req data 
                _ReksaMaintainNewBookingRq.Data.nType = _intType.ToString();
                if (_intType == 1)
                {
                    _ReksaMaintainNewBookingRq.Data.nBookingId = "0";
                }
                else
                {
                    _ReksaMaintainNewBookingRq.Data.nBookingId = ((int)cmpsrNoRefBooking[2]).ToString();
                }
                _ReksaMaintainNewBookingRq.Data.cRefID = cmpsrNoRefBooking.Text1.ToString();
                _ReksaMaintainNewBookingRq.Data.cCIF = cmpsrCIFBooking.Text1.ToString();
                _ReksaMaintainNewBookingRq.Data.cCIFName = cmpsrCIFBooking.Text2.ToString();
                _ReksaMaintainNewBookingRq.Data.nProdId = (int)cmpsrProductBooking[2];
                _ReksaMaintainNewBookingRq.Data.cOfficeId = cmpsrKodeKantorBooking.Text1;
                _ReksaMaintainNewBookingRq.Data.cCurrency = cmpsrCurrBooking.Text1;
                _ReksaMaintainNewBookingRq.Data.mNominal = Decimal.Parse(nispMoneyNomBooking.Value.ToString());
                _ReksaMaintainNewBookingRq.Data.cRekening = textRekeningBooking.Text.Replace("-", "").Trim();
                _ReksaMaintainNewBookingRq.Data.cNamaRek = textNamaRekeningBooking.Text;
                //20240115, gio, RDN-1115, begin
                //_ReksaMaintainNewBookingRq.Data.cBookingDate = dateTglTransaksiBooking.Value.ToString();
                //20240115, gio, RDN-1115, end
                if (cmpsrReferentorBooking.Text1.Trim() == "")
                {
                    _ReksaMaintainNewBookingRq.Data.nReferentor = 0;
                }
                else
                {
                    _ReksaMaintainNewBookingRq.Data.nReferentor = System.Convert.ToInt32(cmpsrReferentorBooking.Text1);
                }
                _ReksaMaintainNewBookingRq.Data.nNIK = intNIK;
                _ReksaMaintainNewBookingRq.Data.cGuid = strGuid;
                _ReksaMaintainNewBookingRq.Data.cInputter = txtbInputterBooking.Text.Trim();
                _ReksaMaintainNewBookingRq.Data.nSeller = int.Parse(cmpsrSellerBooking.Text1.ToString().Trim());
                _ReksaMaintainNewBookingRq.Data.nWaperd = int.Parse(cmpsrWaperdBooking.Text1.ToString().Trim());
                _ReksaMaintainNewBookingRq.Data.bIsPhoneOrder = checkPhoneOrderBooking.Checked;
                _ReksaMaintainNewBookingRq.Data.bIsFeeEdit = checkFeeEditBooking.Checked;
                _ReksaMaintainNewBookingRq.Data.bJenisPerhitunganFee = ComboJenis;
                _ReksaMaintainNewBookingRq.Data.dPercentageFee = moneyPercentageFee;
                _ReksaMaintainNewBookingRq.Data.mSubcFee = Int64.Parse(moneyNominalFee.ToString());
                _ReksaMaintainNewBookingRq.Data.cWarnMsg = "";
                _ReksaMaintainNewBookingRq.Data.cWarnMsg2 = "";
                _ReksaMaintainNewBookingRq.Data.bDocFCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocFCSubscriptionForm.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocFCDevidentAuthLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCDevidentAuthLetter.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocFCJoinAcctStatementLetter = System.Convert.ToBoolean(objFormDocument.chkbDocFCJoinAcctStatementLetter.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocFCIDCopy = System.Convert.ToBoolean(objFormDocument.chkbDocFCIDCopy.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocFCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocFCOthers.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocTCSubscriptionForm = System.Convert.ToBoolean(objFormDocument.chkbDocTCSubscriptionForm.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocTCTermCondition = System.Convert.ToBoolean(objFormDocument.chkbDocTCTermCondition.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocTCProspectus = System.Convert.ToBoolean(objFormDocument.chkbDocTCProspectus.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocTCFundFactSheet = System.Convert.ToBoolean(objFormDocument.chkbDocTCFundFactSheet.CheckState);
                _ReksaMaintainNewBookingRq.Data.bDocTCOthers = System.Convert.ToBoolean(objFormDocument.chkbDocTCOthers.CheckState);
                _ReksaMaintainNewBookingRq.Data.cDocFCOthersList = objFormDocument.GetOthersList("FC");
                _ReksaMaintainNewBookingRq.Data.cDocTCOthersList = objFormDocument.GetOthersList("TC");
                _ReksaMaintainNewBookingRq.Data.cWarnMsg3 = "";
                _ReksaMaintainNewBookingRq.Data.bTrxTaxAmnesty = bIsTax;
                _ReksaMaintainNewBookingRq.Data.cNoRek = cmpsrNoRekBooking.Text1;
                _ReksaMaintainNewBookingRq.Data.cNoRekCcy = cmpsrNoRekBooking.Text2;
                _ReksaMaintainNewBookingRq.Data.cBookingCode = "";

                //end
                ReksaMaintainNewBookingRs _response = _iServiceAPI.ReksaMaintainNewBooking(_ReksaMaintainNewBookingRq);

                if (_response.IsSuccess == true)
                {
                    blnResult = true;
                }
                else
                {
                    blnResult = false;
                    if (_response.ErrorDescription != "")
                    {
                        MessageBox.Show(_response.ErrorDescription);
                        return;
                    }
                }
                #endregion hit API
                //20230206, Antonius Filian, RDN-903, end

                if (blnResult == true)
                {
                    //20230206, Antonius Filian, RDN-903, begin
                    if (_response.Data.ReksaMaintainNewBooking1.ErrMsg != "")
                    {
                        MessageBox.Show("Data gagal disimpan!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    if (!blnResult && _response.Data.MandatoryField2 != null)
                    {
                        MessageBox.Show("Data gagal disimpan!!");
                        frmErrorMessage frmError = new frmErrorMessage();
                        DataTable dt = ToDataTable(_response.Data.MandatoryField2);
                        frmError.SetErrorTable(dt);
                        frmError.ShowDialog();
                    }
                    //if (dsSave.Tables.Count > 0)
                    //{
                    //    MessageBox.Show("Data gagal disimpan!!");
                    //    frmErrorMessage frmError = new frmErrorMessage();
                    //    frmError.SetErrorTable(dsSave.Tables[0]);
                    //    frmError.ShowDialog();
                    //}
                    //20230206, Antonius Filian, RDN-903, end
                    else
                    {
                        //20150831, liliana, LIBST13020, begin
                        //20230206, Antonius Filian, RDN-903, begin                        
                        if (_response.Data.ReksaMaintainNewBooking1.WarnMsg3 != "")
                        {
                            MessageBox.Show(_response.Data.ReksaMaintainNewBooking1.WarnMsg3.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        //20230206, Antonius Filian, RDN-903, end

                        //20150831, liliana, LIBST13020, end
                        if (_response.Data.ReksaMaintainNewBooking1.WarnMsg.ToString() != "")
                        {
                            MessageBox.Show("Profil Risiko produk lebih tinggi dari Profil Risiko Nasabah . PASTIKAN Nasabah sudah menandatangani kolom Profil Risiko pada Subscription/Switching Form", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        if (_response.Data.ReksaMaintainNewBooking1.WarnMsg2.ToString() != "")
                        {
                            MessageBox.Show("Umur nasabah 55 tahun atau lebih, Mohon dipastikan nasabah menandatangani pernyataan pada kolom yang disediakan di Formulir Subscription/Switching", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        //20220121, sandi, RDN-727, begin
                        string rekBookingIDR = maskedRekeningBooking.Text.Replace("-", "").Trim();
                        string rekBookingUSD = maskedRekeningBookingUSD.Text.Replace("-", "").Trim();
                        string rekBookingMC = maskedRekeningBookingMC.Text.Replace("-", "").Trim();

                        if (cmpsrNoRekBooking.Text1 != rekBookingIDR && cmpsrNoRekBooking.Text1 != rekBookingUSD && cmpsrNoRekBooking.Text1 != rekBookingMC)
                        {
                            MessageBox.Show("Rekening " + cmpsrNoRekBooking.Text1 + " akan disimpan di Master Nasabah untuk pembagian deviden dan pendebetan RDB (jika ada)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        //20220121, sandi, RDN-727, end

                        //20230222, Filian, RDN-903, begin
                        //string strBookingCode = dbParam[2].Value.ToString(); 
                        //string strBookingCode = _ReksaMaintainNewBookingRq.Data.cRefID.ToString();
                        string strBookingCode = _response.Data.ReksaMaintainNewBooking1._RefID.ToString();
                        //20230222, Filian, RDN-903, end

                        MessageBox.Show("Simpan Berhasil dengan No Referensi: " + strBookingCode + ", Harap lakukan Otorisasi Supervisor!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        //20221020, Andi, HFUNDING-178, begin
                        //20230222, Filian, RDN-903, begin
                        //ReksaSalesEksekutifTransaksi(dbParam[2].Value.ToString(), textBoxKodeSalesBook.Text,
                        //    richTextBoxKeteranganBook.Text,
                        //    DateTime.Today, intNIK, DateTime.Today, intNIK);
                        //20230511, Andhika J, RDN-903, begin
                        //ReksaSalesEksekutifTransaksi(_ReksaMaintainNewBookingRq.Data.cRefID.ToString(), textBoxKodeSalesBook.Text,
                        //    richTextBoxKeteranganBook.Text,
                        //    DateTime.Today, intNIK, DateTime.Today, intNIK);
                        ReksaSalesEksekutifTransaksi(_response.Data.ReksaMaintainNewBooking1._RefID.ToString(), textBoxKodeSalesBook.Text,
                            richTextBoxKeteranganBook.Text,
                            DateTime.Today, intNIK, DateTime.Today, intNIK);
                        //20230511, Andhika J, RDN-903, end

                        //20230222, Filian, RDN-903, end
                        //20221020, Andi, HFUNDING-178, end
                        //20150728, liliana, LIBST13020, begin
                        ResetForm();
                        DisableAllForm(false);
                        //20150728, liliana, LIBST13020, end
                        //20150724, liliana, LIBST13020, begin
                        cmpsrNoRefBooking.Text1 = strBookingCode;
                        cmpsrNoRefBooking.ValidateField();
                        //20150724, liliana, LIBST13020, end
                        //20150710, liliana, LIBST13020, begin
                        //subCancel();
                        subRefresh();
                        //20150710, liliana, LIBST13020, end
                    }
                }

                dsSave = null;
                //20230222, Filian, RDN-903, begin
                //dbParam = null;
                //20230222, Filian, RDN-903, end
            }
        }

        private void subSaveAsuransi()
        {
            //20150326, liliana, LIBST13020, begin
            bool _isAsuransi, _isAutoRedemption;

            if (_strTabName == "SUBSRDB")
            {

            }
            else if (_strTabName == "SWCRDB")
            {
                if (cmbAutoRedempSwcRDB.Text == "YA")
                {
                    _isAutoRedemption = true;
                }
                else
                {
                    _isAutoRedemption = false;
                }

                if (cmbAsuransiSwcRDB.Text == "YA")
                {
                    _isAsuransi = true;
                }
                else
                {
                    _isAsuransi = false;
                }


                System.Data.OleDb.OleDbParameter[] pr = new System.Data.OleDb.OleDbParameter[5];

                try
                {
                    (pr[0] = new System.Data.OleDb.OleDbParameter("@pcTranCode", System.Data.OleDb.OleDbType.VarChar, 20)).Value = textNoTransaksiSwcRDB.Text;
                    (pr[1] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer)).Value = intNIK;
                    (pr[2] = new System.Data.OleDb.OleDbParameter("@pbAsuransi", System.Data.OleDb.OleDbType.Boolean)).Value = _isAsuransi;
                    (pr[3] = new System.Data.OleDb.OleDbParameter("@pbAutoRedemption", System.Data.OleDb.OleDbType.Boolean)).Value = _isAutoRedemption;
                    (pr[4] = new System.Data.OleDb.OleDbParameter("@pcTranType", System.Data.OleDb.OleDbType.VarChar, 20)).Value = _strTabName;

                    bool blnResult = ClQ.ExecProc("dbo.ReksaFlagAsuransi", ref pr);

                    if (blnResult)
                    {
                        MessageBox.Show("Proses berhasil. Perubahan akan aktif jika sudah diotorisasi.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        subCancel();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            //20150326, liliana, LIBST13020, end
        }
        //20150326, liliana, LIBST13020, begin

        private void subUpdateAsuransi()
        {
            _intType = 2;
            DisableAllForm(false);
            subResetToolBar();

            if (_strTabName == "SUBSRDB")
            {
                cmbAutoRedempRDB.Enabled = true;
                cmbAsuransiRDB.Enabled = true;
            }
            //else if (_strTabName == "SWCRDB")
            //{
            //    cmbAutoRedempSwcRDB.Enabled = true;
            //    cmbAsuransiSwcRDB.Enabled = true;
            //}
        }
        //20150326, liliana, LIBST13020, end

        private void subCancel()
        {
            _intType = 0;
            ResetForm();
            cTransaksi.ClearData();
            DisableAllForm(false);
            //20150505, liliana, LIBST13020, begin
            DisableFormTrxSubs(false);
            DisableFormTrxRedemp(false);
            DisableFormTrxRDB(false);
            //20150505, liliana, LIBST13020, end
            subResetToolBar();
            //20160509, Elva, CSODD16117, begin
            ResetAllKodeKantor();
            //20160509, Elva, CSODD16117, end
        }

        //20221019, Andi, HFUNDING-178, begin
        private void GetDataSalesEksekutif(string RefID, out string KodeSales, out string Keterangan
            )
        {
            DataSet dsRefresh;
            KodeSales = "";
            Keterangan = "";

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcRefID", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = RefID;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            bool blnResult = ClQ.ExecProc("ReksaGetDataSimanis", ref dbParam, out dsRefresh);

            if (blnResult == true)
            {
                if (dsRefresh.Tables[0].Rows.Count > 0)
                {
                    KodeSales = dsRefresh.Tables[0].Rows[0]["KodeSales"].ToString();
                    Keterangan = dsRefresh.Tables[0].Rows[0]["Keterangan"].ToString();
                }
            }

        }
        //20221019, Andi, HFUNDING-178, end

        private void GetDataCIF(string CIFNo, out string ShareholderID, out string NoRekening, out string NamaRekening,
            //20150518, liliana, LIBST13020, begin
            //out string SID)
            out string SID, out string NoRekeningUSD, out string NamaRekeningUSD
            //20150702, liliana, LIBST13020, begin
            //20150728, liliana, LIBST13020, begin
            , out string NoRekeningMC, out string NamaRekeningMC
            //20150728, liliana, LIBST13020, end
            , out string RiskProfile, out DateTime LastUpdateRiskProfile
            //20150702, liliana, LIBST13020, end
            )
        //20150518, liliana, LIBST13020, end
        {
            DataSet dsRefresh;
            ShareholderID = "";
            NoRekening = "";
            NamaRekening = "";
            SID = "";
            //20150518, liliana, LIBST13020, begin
            NoRekeningUSD = "";
            NamaRekeningUSD = "";
            //20150518, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            NoRekeningMC = "";
            NamaRekeningMC = "";
            //20150728, liliana, LIBST13020, end
            //20150702, liliana, LIBST13020, begin
            RiskProfile = "";
            LastUpdateRiskProfile = DateTime.Today;
            //20150702, liliana, LIBST13020, end

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = CIFNo;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
            dbParam[1].Value = intNIK;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
            dbParam[2].Value = strGuid;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            bool blnResult = ClQ.ExecProc("ReksaRefreshNasabah", ref dbParam, out dsRefresh);

            if (blnResult == true)
            {
                if (dsRefresh.Tables[0].Rows.Count > 0)
                {
                    ShareholderID = dsRefresh.Tables[0].Rows[0]["ShareholderID"].ToString();

                    NoRekening = dsRefresh.Tables[0].Rows[0]["NISPAccountId"].ToString();
                    NamaRekening = dsRefresh.Tables[0].Rows[0]["NISPAccountName"].ToString();
                    //20150518, liliana, LIBST13020, begin
                    NoRekeningUSD = dsRefresh.Tables[0].Rows[0]["NISPAccountIdUSD"].ToString();
                    NamaRekeningUSD = dsRefresh.Tables[0].Rows[0]["NISPAccountNameUSD"].ToString();
                    //20150518, liliana, LIBST13020, end
                    //20150702, liliana, LIBST13020, begin
                    //20150728, liliana, LIBST13020, begin
                    NoRekeningMC = dsRefresh.Tables[0].Rows[0]["NISPAccountIdMC"].ToString();
                    NamaRekeningMC = dsRefresh.Tables[0].Rows[0]["NISPAccountNameMC"].ToString();
                    //20150728, liliana, LIBST13020, end
                    RiskProfile = dsRefresh.Tables[1].Rows[0]["RiskProfile"].ToString();
                    LastUpdateRiskProfile = (DateTime)dsRefresh.Tables[1].Rows[0]["LastUpdate"];
                    //20150702, liliana, LIBST13020, end

                    SID = dsRefresh.Tables[0].Rows[0]["CIFSID"].ToString();
                }
            }
        }
        //20150723, liliana, LIBST13020, begin
        //20160829, liliana, LOGEN00196, begin

        private void CheckCIFTaxAmnesty(string CIFNo, out string isTA, out string AccountIdTA, out string AccountIdUSDTA,
               out string AccountIdMCTA, out string AccountNameTA, out string AccountNameUSDTA, out string AccountNameMCTA,
               out string AccountId, out string AccountIdUSD,
               out string AccountIdMC, out string AccountName, out string AccountNameUSD, out string AccountNameMC
            )
        {
            DataSet dsRefresh;
            isTA = "";
            AccountIdTA = "";
            AccountIdUSDTA = "";
            AccountIdMCTA = "";
            AccountNameTA = "";
            AccountNameUSDTA = "";
            AccountNameMCTA = "";

            AccountId = "";
            AccountIdUSD = "";
            AccountIdMC = "";
            AccountName = "";
            AccountNameUSD = "";
            AccountNameMC = "";

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = CIFNo;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
            dbParam[1].Value = intNIK;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
            dbParam[2].Value = strGuid;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            bool blnResult = ClQ.ExecProc("ReksaCheckCIFTaxAmnesty", ref dbParam, out dsRefresh);

            if (blnResult == true)
            {
                if (dsRefresh.Tables[0].Rows.Count > 0)
                {
                    isTA = dsRefresh.Tables[0].Rows[0]["isTA"].ToString();
                    AccountIdTA = dsRefresh.Tables[0].Rows[0]["AccountIdTA"].ToString();
                    AccountIdUSDTA = dsRefresh.Tables[0].Rows[0]["AccountIdUSDTA"].ToString();
                    AccountIdMCTA = dsRefresh.Tables[0].Rows[0]["AccountIdMCTA"].ToString();
                    AccountNameTA = dsRefresh.Tables[0].Rows[0]["AccountNameTA"].ToString();
                    AccountNameUSDTA = dsRefresh.Tables[0].Rows[0]["AccountNameUSDTA"].ToString();
                    AccountNameMCTA = dsRefresh.Tables[0].Rows[0]["AccountNameMCTA"].ToString();

                    AccountId = dsRefresh.Tables[0].Rows[0]["NISPAccountId"].ToString();
                    AccountIdUSD = dsRefresh.Tables[0].Rows[0]["NISPAccountIdUSD"].ToString();
                    AccountIdMC = dsRefresh.Tables[0].Rows[0]["NISPAccountIdMC"].ToString();
                    AccountName = dsRefresh.Tables[0].Rows[0]["NISPAccountName"].ToString();
                    AccountNameUSD = dsRefresh.Tables[0].Rows[0]["NISPAccountNameUSD"].ToString();
                    AccountNameMC = dsRefresh.Tables[0].Rows[0]["NISPAccountNameMC"].ToString();
                }
            }
        }

        //20160829, liliana, LOGEN00196, end
        private bool CheckCIF(string CIF)
        {
            bool blnResult = false;

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = CIF;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            blnResult = ClQ.ExecProc("ReksaCheckCIF", ref dbParam);

            return blnResult;
        }
        //20150723, liliana, LIBST13020, end

        private void cmpsrCIFSubs_onNispText2Changed(object sender, EventArgs e)
        {
            int intUmur = 0;

            if (cmpsrCIFSubs.Text1 != "")
            {

                //20150723, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    if (!CheckCIF(cmpsrCIFSubs.Text1.Trim()))
                    {
                        subCancel();
                        return;
                    }

                    //20170828, liliana, COPOD17271, begin
                    if (!this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIFSubs.Text1.Trim(), strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        subCancel();
                        return;
                    }
                    //20170828, liliana, COPOD17271, end
                }

                //20150723, liliana, LIBST13020, end
                string strShareholderID, strNoRek, strNamaRek, strSID;
                //20150518, liliana, LIBST13020, begin
                //GetDataCIF(cmpsrCIFSubs.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                string strNoRekUSD, strNamaRekUSD;
                //20150702, liliana, LIBST13020, begin
                //20150728, liliana, LIBST13020, begin
                string strNoRekMC, strNamaRekMC;
                //20150728, liliana, LIBST13020, end
                string strRiskProfile;
                DateTime dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                GetDataCIF(cmpsrCIFSubs.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                    , out strNoRekUSD, out strNamaRekUSD
                    //20150702, liliana, LIBST13020, begin
                    //20150728, liliana, LIBST13020, begin
                    , out strNoRekMC, out strNamaRekMC
                    //20150728, liliana, LIBST13020, end
                    , out strRiskProfile, out dtLastUpdateRiskProfile
                    //20150702, liliana, LIBST13020, end
                    );
                //20150518, liliana, LIBST13020, end
                //20150702, liliana, LIBST13020, begin
                txtbRiskProfileSubs.Text = strRiskProfile;
                dtpRiskProfileSubs.Value = dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                textSIDSubs.Text = strSID;
                textShareHolderIdSubs.Text = strShareholderID;
                textRekeningSubs.Text = strNoRek;
                //20150505, liliana, LIBST13020, begin
                maskedRekeningSubs.Text = strNoRek;
                //20150505, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, begin
                maskedRekeningSubsUSD.Text = strNoRekUSD;
                textNamaRekeningSubsUSD.Text = strNamaRekUSD;
                //20150518, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningSubsMC.Text = strNoRekMC;
                textNamaRekeningSubsMC.Text = strNamaRekMC;
                //20150728, liliana, LIBST13020, end
                textNamaRekeningSubs.Text = strNamaRek;

                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSubs.Criteria = cmpsrCIFSubs.Text1.Trim() + "#" + cmpsrProductSubs.Text1.Trim() + "#" + _strTabName;
                cmpsrClientSubs.Criteria = cmpsrCIFSubs.Text1.Trim() + "#" + cmpsrProductSubs.Text1.Trim() + "#" + _strTabName
                                            + "#" + cmbTASubs.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end
                //20150505, liliana, LIBST13020, begin
                cmpsrNoRefSubs.Criteria = _strTabName + "#" + cmpsrCIFSubs.Text1.Trim();
                //20150505, liliana, LIBST13020, end

                intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFSubs.Text1);
                txtUmurSubs.Text = intUmur.ToString();

                //20150827, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150827, liliana, LIBST13020, end
                    if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSubs.Text1))
                    {
                        checkPhoneOrderSubs.Enabled = false;
                        checkPhoneOrderSubs.Checked = false;
                    }
                    else
                    {
                        checkPhoneOrderSubs.Enabled = true;
                    }
                    //20150827, liliana, LIBST13020, begin
                }
                //20150827, liliana, LIBST13020, end
                //20160812, Elva, LOGEN00191, begin
                //20161004, liliana, CSODD16311, begin
                //if (_intType == 1 || _intType == 2)
                //{
                //20161004, liliana, CSODD16311, end
                if (cmpsrCIFSubs.Text1 != "" && cmpsrCIFSubs.Text2 != "" && _isCheckingTASubs == true)
                {
                    string strIsAllowed = "", strErrorMessage = "";
                    if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIFSubs.Text1, out strIsAllowed, out strErrorMessage))
                    {
                        if (strIsAllowed == "0")
                        {
                            //20160901, liliana, LOGEN00196, begin
                            //DialogResult ds = MessageBox.Show(strErrorMessage, "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            //if (ds == DialogResult.No)
                            //    ResetFormSubs();

                            //return;
                            //20161004, liliana, CSODD16311, begin
                            //MessageBox.Show("No CIF teridentifikasi sebagai nasabah Tax Amnesty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASubs.SelectedIndex = 1;
                                cmbTASubs.Enabled = true;
                            }
                            lblTaxAmnestySubs.Visible = true;
                            //20161004, liliana, CSODD16311, end
                            //20160901, liliana, LOGEN00196, end
                        }
                        //20161004, liliana, CSODD16311, begin
                        //20161107, liliana, CSODD16311, begin
                        else if (strIsAllowed == "2")
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASubs.SelectedIndex = 0;
                                cmbTASubs.Enabled = false;
                            }
                            lblTaxAmnestySubs.Visible = true;
                        }
                        //20161107, liliana, CSODD16311, end
                        else
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASubs.SelectedIndex = 0;
                                cmbTASubs.Enabled = false;
                            }
                            lblTaxAmnestySubs.Visible = false;
                        }
                        //20161004, liliana, CSODD16311, end
                    }
                    else
                    {
                        MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                //20210913, Lita, RDN-674, begin
                cmpsrNoRekSubs.Criteria = cmpsrCIFSubs.Text1 + "#" + cmbTASubs.SelectedIndex.ToString();
                cmpsrNoRekSubs.Text1 = "";
                cmpsrNoRekSubs.Validate();
                //20210913, Lita, RDN-674, end



                //20161004, liliana, CSODD16311, begin
                //}
                //20161004, liliana, CSODD16311, end
                //20160812, Elva, LOGEN00191, end
            }
            else
            {
                txtUmurSubs.Text = "";
                textSIDSubs.Text = "";
                textShareHolderIdSubs.Text = "";
                textRekeningSubs.Text = "";
                //20150505, liliana, LIBST13020, begin
                maskedRekeningSubs.Text = "";
                //20150505, liliana, LIBST13020, end
                textNamaRekeningSubs.Text = "";
                //20150518, liliana, LIBST13020, begin
                maskedRekeningSubsUSD.Text = "";
                textNamaRekeningSubsUSD.Text = "";
                //20150518, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningSubsMC.Text = "";
                textNamaRekeningSubsMC.Text = "";
                //20150728, liliana, LIBST13020, end
                //20161004, liliana, CSODD16311, begin
                lblTaxAmnestySubs.Visible = false;
                cmbTASubs.SelectedIndex = -1;
                //20161004, liliana, CSODD16311, end
            }

        }

        private void cmpsrCIFSubs_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCIFSubs.Text2 = "";
        }

        private void cmpsrCIFRedemp_onNispText2Changed(object sender, EventArgs e)
        {
            int intUmur = 0;

            if (cmpsrCIFRedemp.Text1 != "")
            {
                //20150723, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    if (!CheckCIF(cmpsrCIFRedemp.Text1.Trim()))
                    {
                        subCancel();
                        return;
                    }

                    //20170828, liliana, COPOD17271, begin
                    if (!this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIFRedemp.Text1.Trim(), strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        subCancel();
                        return;
                    }
                    //20170828, liliana, COPOD17271, end
                }
                //20150723, liliana, LIBST13020, end
                string strShareholderID, strNoRek, strNamaRek, strSID;
                //20150518, liliana, LIBST13020, begin
                //GetDataCIF(cmpsrCIFRedemp.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                string strNoRekUSD, strNameRekUSD;
                //20150702, liliana, LIBST13020, begin
                //20150728, liliana, LIBST13020, begin
                string strNoRekMC, strNameRekMC;
                //20150728, liliana, LIBST13020, end
                string strRiskProfile;
                DateTime dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                GetDataCIF(cmpsrCIFRedemp.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                    , out strNoRekUSD, out strNameRekUSD
                    //20150702, liliana, LIBST13020, begin
                    //20150728, liliana, LIBST13020, begin
                    , out strNoRekMC, out strNameRekMC
                    //20150728, liliana, LIBST13020, end
                    , out strRiskProfile, out dtLastUpdateRiskProfile
                    //20150702, liliana, LIBST13020, end
                    );
                //20150518, liliana, LIBST13020, end
                //20150702, liliana, LIBST13020, begin
                txtbRiskProfileRedemp.Text = strRiskProfile;
                dtpRiskProfileRedemp.Value = dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                textSIDRedemp.Text = strSID;
                textShareHolderIdRedemp.Text = strShareholderID;
                textRekeningRedemp.Text = strNoRek;
                //20150505, liliana, LIBST13020, begin
                maskedRekeningRedemp.Text = strNoRek;
                //20150505, liliana, LIBST13020, end
                textNamaRekeningRedemp.Text = strNamaRek;
                //20150518, liliana, LIBST13020, begin
                maskedRekeningRedempUSD.Text = strNoRekUSD;
                textNamaRekeningRedempUSD.Text = strNameRekUSD;
                //20150518, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningRedempMC.Text = strNoRekMC;
                textNamaRekeningRedempMC.Text = strNameRekMC;
                //20150728, liliana, LIBST13020, end

                cmpsrProductRedemp.Criteria = _strTabName + "#" + cmpsrCIFRedemp.Text1;
                //20150505, liliana, LIBST13020, begin
                cmpsrNoRefRedemp.Criteria = _strTabName + "#" + cmpsrCIFRedemp.Text1.Trim();
                //20150505, liliana, LIBST13020, end

                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientRedemp.Criteria = cmpsrCIFRedemp.Text1.Trim() + "#" + cmpsrProductRedemp.Text1.Trim() + "#" + _strTabName;
                cmpsrClientRedemp.Criteria = cmpsrCIFRedemp.Text1.Trim() + "#" + cmpsrProductRedemp.Text1.Trim() + "#" + _strTabName
                                            + "#" + cmbTARedemp.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFRedemp.Text1);
                txtUmurRedemp.Text = intUmur.ToString();

                //20150827, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150827, liliana, LIBST13020, end
                    if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRedemp.Text1))
                    {
                        checkPhoneOrderRedemp.Enabled = false;
                        checkPhoneOrderRedemp.Checked = false;
                    }
                    else
                    {
                        checkPhoneOrderRedemp.Enabled = true;
                    }
                    //20150827, liliana, LIBST13020, begin
                }
                //20150827, liliana, LIBST13020, end
                //20160812, Elva, LOGEN00191, begin
                //20161004, liliana, CSODD16311, begin
                //if (_intType == 1 || _intType == 2)
                //{
                //20161004, liliana, CSODD16311, end
                if (cmpsrCIFRedemp.Text1 != "" && cmpsrCIFRedemp.Text2 != "" && _isCheckingTARedemp == true)
                {
                    string strIsAllowed = "", strErrorMessage = "";
                    if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIFRedemp.Text1, out strIsAllowed, out strErrorMessage))
                    {
                        if (strIsAllowed == "0")
                        {
                            //20160901, liliana, LOGEN00196, begin
                            //DialogResult ds = MessageBox.Show(strErrorMessage, "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            //if (ds == DialogResult.No)
                            //    ResetFormRedemp();

                            //return;
                            //20161004, liliana, CSODD16311, begin
                            //MessageBox.Show("No CIF teridentifikasi sebagai nasabah Tax Amnesty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTARedemp.SelectedIndex = 1;
                                cmbTARedemp.Enabled = true;
                            }
                            lblTaxAmnestyRedemp.Visible = true;
                            //20161004, liliana, CSODD16311, end
                            //20160901, liliana, LOGEN00196, end
                        }
                        //20161004, liliana, CSODD16311, begin
                        //20161107, liliana, CSODD16311, begin
                        else if (strIsAllowed == "2")
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTARedemp.SelectedIndex = 1;
                                cmbTARedemp.Enabled = true;
                            }
                            lblTaxAmnestyRedemp.Visible = true;

                        }
                        //20161107, liliana, CSODD16311, end
                        //20161108, liliana, CSODD16311, begin
                        else if (strIsAllowed == "3")
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTARedemp.SelectedIndex = 1;
                                cmbTARedemp.Enabled = true;
                            }
                            lblTaxAmnestyRedemp.Visible = true;

                        }
                        //20161108, liliana, CSODD16311, end
                        else
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTARedemp.SelectedIndex = 0;
                                cmbTARedemp.Enabled = false;
                            }
                            lblTaxAmnestyRedemp.Visible = false;
                        }
                        //20161004, liliana, CSODD16311, end
                    }
                    else
                    {
                        MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                //20210913, Lita, RDN-674, begin
                cmpsrNoRekRedemp.Criteria = cmpsrCIFRedemp.Text1 + "#" + cmbTARedemp.SelectedIndex.ToString();
                cmpsrNoRekRedemp.Text1 = "";
                cmpsrNoRekRedemp.Validate();
                //20210913, Lita, RDN-674, end

                //20161004, liliana, CSODD16311, begin
                //}
                //20161004, liliana, CSODD16311, end
                //20160812, Elva, LOGEN00191, end
            }
            else
            {
                txtUmurRedemp.Text = "";
                textSIDRedemp.Text = "";
                textShareHolderIdRedemp.Text = "";
                textRekeningRedemp.Text = "";
                //20150505, liliana, LIBST13020, begin
                maskedRekeningRedemp.Text = "";
                //20150505, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningRedempUSD.Text = "";
                maskedRekeningRedempMC.Text = "";
                //20150728, liliana, LIBST13020, END
                textNamaRekeningRedemp.Text = "";
                //20161004, liliana, CSODD16311, begin
                lblTaxAmnestyRedemp.Visible = false;
                cmbTARedemp.SelectedIndex = -1;
                //20161004, liliana, CSODD16311, end

            }
        }

        private void cmpsrCIFRedemp_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCIFRedemp.Text2 = "";
        }

        private void cmpsrCIFRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCIFRDB.Text2 = "";
        }

        private void cmpsrCIFRDB_onNispText2Changed(object sender, EventArgs e)
        {
            int intUmur = 0;

            if (cmpsrCIFRDB.Text1 != "")
            {

                //20150723, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    if (!CheckCIF(cmpsrCIFRDB.Text1.Trim()))
                    {
                        subCancel();
                        return;
                    }

                    //20170828, liliana, COPOD17271, begin
                    if (!this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIFRDB.Text1.Trim(), strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        subCancel();
                        return;
                    }
                    //20170828, liliana, COPOD17271, end
                }
                //20150723, liliana, LIBST13020, end
                string strShareholderID, strNoRek, strNamaRek, strSID;
                //20150518, liliana, LIBST13020, begin
                //GetDataCIF(cmpsrCIFRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                string strNoRekUSD, strNameRekUSD;
                //20150702, liliana, LIBST13020, begin
                string strRiskProfile;
                DateTime dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                string strNoRekMC, strNameRekMC;
                //20150728, liliana, LIBST13020, end
                GetDataCIF(cmpsrCIFRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                    , out strNoRekUSD, out strNameRekUSD
                    //20150702, liliana, LIBST13020, begin
                    //20150728, liliana, LIBST13020, begin
                    , out strNoRekMC, out strNameRekMC
                    //20150728, liliana, LIBST13020, end
                    , out strRiskProfile, out dtLastUpdateRiskProfile
                    //20150702, liliana, LIBST13020, end
                    );
                //20150518, liliana, LIBST13020, end
                //20150702, liliana, LIBST13020, begin
                txtbRiskProfileRDB.Text = strRiskProfile;
                dtpRiskProfileRDB.Value = dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                textSIDRDB.Text = strSID;
                textShareHolderIdRDB.Text = strShareholderID;
                textRekeningRDB.Text = strNoRek;
                //20150505, liliana, LIBST13020, begin
                maskedRekeningRDB.Text = strNoRek;
                //20150505, liliana, LIBST13020, end
                textNamaRekeningRDB.Text = strNamaRek;
                //20150518, liliana, LIBST13020, begin
                maskedRekeningRDBUSD.Text = strNoRekUSD;
                textNamaRekeningRDBUSD.Text = strNameRekUSD;
                //20150518, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningRDBMC.Text = strNoRekMC;
                textNamaRekeningRDBMC.Text = strNameRekMC;
                //20150728, liliana, LIBST13020, end

                //20150505, liliana, LIBST13020, begin
                cmpsrNoRefRDB.Criteria = _strTabName + "#" + cmpsrCIFRDB.Text1.Trim();
                //20150505, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientRDB.Criteria = cmpsrCIFRDB.Text1.Trim() + "#" + cmpsrProductRDB.Text1.Trim() + "#" + _strTabName;
                cmpsrClientRDB.Criteria = cmpsrCIFRDB.Text1.Trim() + "#" + cmpsrProductRDB.Text1.Trim() + "#" + _strTabName
                                          + "#" + cmbTARDB.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFRDB.Text1);
                txtUmurRDB.Text = intUmur.ToString();

                //20150827, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150827, liliana, LIBST13020, begin
                    if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRDB.Text1))
                    {
                        checkPhoneOrderRDB.Enabled = false;
                        checkPhoneOrderRDB.Checked = false;
                    }
                    else
                    {
                        checkPhoneOrderRDB.Enabled = true;
                    }
                    //20150827, liliana, LIBST13020, begin
                }
                //20150827, liliana, LIBST13020, end
                //20160812, Elva, LOGEN00191, begin
                //20161004, liliana, CSODD16311, begin
                //if (_intType == 1 || _intType == 2)
                //{
                //20161004, liliana, CSODD16311, end
                if (cmpsrCIFRDB.Text1 != "" && cmpsrCIFRDB.Text2 != "" && _isCheckingTARDB == true)
                {
                    string strIsAllowed = "", strErrorMessage = "";
                    if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIFRDB.Text1, out strIsAllowed, out strErrorMessage))
                    {
                        if (strIsAllowed == "0")
                        {
                            //20160901, liliana, LOGEN00196, begin
                            //DialogResult ds = MessageBox.Show(strErrorMessage, "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            //if (ds == DialogResult.No)
                            //    ResetFormRDB();

                            //return;
                            //20161004, liliana, CSODD16311, begin
                            //MessageBox.Show("No CIF teridentifikasi sebagai nasabah Tax Amnesty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTARDB.SelectedIndex = 1;
                                cmbTARDB.Enabled = true;
                            }
                            lblTaxAmnestyRDB.Visible = true;
                            //20161004, liliana, CSODD16311, end
                            //20160901, liliana, LOGEN00196, end
                        }
                        //20161004, liliana, CSODD16311, begin
                        //20161107, liliana, CSODD16311, begin
                        else if (strIsAllowed == "2")
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTARDB.SelectedIndex = 0;
                                cmbTARDB.Enabled = false;
                            }
                            lblTaxAmnestyRDB.Visible = true;
                        }
                        //20161107, liliana, CSODD16311, end
                        else
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTARDB.SelectedIndex = 0;
                                cmbTARDB.Enabled = false;
                            }
                            lblTaxAmnestyRDB.Visible = false;
                        }
                        //20161004, liliana, CSODD16311, end
                    }
                    else
                    {
                        MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                //20210913, Lita, RDN-674, begin
                cmpsrNoRekRDB.Criteria = cmpsrCIFRDB.Text1 + "#" + cmbTARDB.SelectedIndex.ToString();
                cmpsrNoRekRDB.Text1 = "";
                cmpsrNoRekRDB.Validate();
                //20210913, Lita, RDN-674, end

                //20161004, liliana, CSODD16311, begin
                //}
                //20161004, liliana, CSODD16311, end
                //20160812, Elva, LOGEN00191, end 
                //20221017, Andhika J, RDN-861, begin
                string IsAsuransi = "";
                ReksaValidateInsuranceRDB(cmpsrCIFRDB.Text1.Trim(), IsAsuransi);
                //20221017, Andhika J, RDN-861, end
            }
            else
            {
                txtUmurRDB.Text = "";
                textSIDRDB.Text = "";
                textShareHolderIdRDB.Text = "";
                textRekeningRDB.Text = "";
                //20150505, liliana, LIBST13020, begin
                maskedRekeningRDB.Text = "";
                //20150505, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningRDBUSD.Text = "";
                maskedRekeningRDBMC.Text = "";
                //20150728, liliana, LIBST13020, END
                textNamaRekeningRDB.Text = "";
                //20161004, liliana, CSODD16311, begin
                lblTaxAmnestyRDB.Visible = false;
                cmbTARDB.SelectedIndex = -1;
                //20161004, liliana, CSODD16311, end
            }
        }

        private void cmpsrCIFSwc_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCIFSwc.Text2 = "";
        }

        private void cmpsrCIFSwc_onNispText2Changed(object sender, EventArgs e)
        {
            int intUmur = 0;

            if (cmpsrCIFSwc.Text1 != "")
            {
                //20150723, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    if (!CheckCIF(cmpsrCIFSwc.Text1.Trim()))
                    {
                        subCancel();
                        return;
                    }

                    //20170828, liliana, COPOD17271, begin
                    if (!this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIFSwc.Text1.Trim(), strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        subCancel();
                        return;
                    }
                    //20170828, liliana, COPOD17271, end
                }
                //20150723, liliana, LIBST13020, end
                string strShareholderID, strNoRek, strNamaRek, strSID;
                //20150518, liliana, LIBST13020, begin
                //GetDataCIF(cmpsrCIFSwc.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                string strNoRekUSD, strNamaRekUSD;
                //20150702, liliana, LIBST13020, begin
                //20150728, liliana, LIBST13020, begin
                string strNoRekMC, strNamaRekMC;
                //20150728, liliana, LIBST13020, END
                string strRiskProfile;
                DateTime dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                GetDataCIF(cmpsrCIFSwc.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                    , out strNoRekUSD, out strNamaRekUSD
                    //20150702, liliana, LIBST13020, begin
                    //20150728, liliana, LIBST13020, begin
                    , out strNoRekMC, out strNamaRekMC
                    //20150728, liliana, LIBST13020, END
                    , out strRiskProfile, out dtLastUpdateRiskProfile
                    //20150702, liliana, LIBST13020, end
                    );
                //20150518, liliana, LIBST13020, end
                //20150702, liliana, LIBST13020, begin
                txtbRiskProfileSwc.Text = strRiskProfile;
                dtpRiskProfileSwc.Value = dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                textSIDSwc.Text = strSID;
                textShareHolderIdSwc.Text = strShareholderID;
                textRekeningSwc.Text = strNoRek;
                //20150505, liliana, LIBST13020, begin
                maskedRekeningSwc.Text = strNoRek;
                //20150505, liliana, LIBST13020, end
                textNamaRekeningSwc.Text = strNamaRek;
                //20150518, liliana, LIBST13020, begin
                maskedRekeningSwcUSD.Text = strNoRekUSD;
                textNamaRekeningSwcUSD.Text = strNamaRekUSD;
                //20150518, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningSwcMC.Text = strNoRekMC;
                textNamaRekeningSwcMC.Text = strNamaRekMC;
                //20150728, liliana, LIBST13020, END

                //20150505, liliana, LIBST13020, begin
                cmpsrNoRefSwc.Criteria = cmpsrCIFSwc.Text1.Trim();
                //20150505, liliana, LIBST13020, end
                cmpsrProductSwcOut.Criteria = _strTabName + "#" + cmpsrCIFSwc.Text1;

                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSwcOut.Criteria = cmpsrCIFSwc.Text1 + "#" + cmpsrProductSwcOut.Text1 + "#" + _strTabName;
                cmpsrClientSwcOut.Criteria = cmpsrCIFSwc.Text1 + "#" + cmpsrProductSwcOut.Text1 + "#" + _strTabName
                                            + "#" + cmbTASwc.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFSwc.Text1);
                txtUmurSwc.Text = intUmur.ToString();

                //20150827, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150827, liliana, LIBST13020, end
                    if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSwc.Text1))
                    {
                        checkPhoneOrderSwc.Enabled = false;
                        checkPhoneOrderSwc.Checked = false;
                    }
                    else
                    {
                        checkPhoneOrderSwc.Enabled = true;
                    }
                    //20150827, liliana, LIBST13020, begin
                }
                //20150827, liliana, LIBST13020, end

                //20160812, Elva, LOGEN00191, begin
                //20161004, liliana, CSODD16311, begin
                //if (_intType == 1 || _intType == 2)
                //{
                //20161004, liliana, CSODD16311, end
                if (cmpsrCIFSwc.Text1 != "" && cmpsrCIFSwc.Text2 != "" && _isCheckingTASwcNonRDB == true)
                {
                    string strIsAllowed = "", strErrorMessage = "";
                    if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIFSwc.Text1, out strIsAllowed, out strErrorMessage))
                    {
                        if (strIsAllowed == "0")
                        {
                            //20160901, liliana, LOGEN00196, begin
                            //DialogResult ds = MessageBox.Show(strErrorMessage, "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            //if (ds == DialogResult.No)
                            //    ResetFormSwc();

                            //return;
                            //20161004, liliana, CSODD16311, begin
                            //MessageBox.Show("No CIF teridentifikasi sebagai nasabah Tax Amnesty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASwc.SelectedIndex = 1;
                                cmbTASwc.Enabled = true;
                            }
                            lblTaxAmnestySwc.Visible = true;
                            //20161004, liliana, CSODD16311, end
                            //20160901, liliana, LOGEN00196, end
                        }
                        //20161004, liliana, CSODD16311, begin
                        //20161107, liliana, CSODD16311, begin
                        else if (strIsAllowed == "2")
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASwc.SelectedIndex = 0;
                                cmbTASwc.Enabled = false;
                            }
                            lblTaxAmnestySwc.Visible = true;
                        }
                        //20161107, liliana, CSODD16311, end
                        else
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASwc.SelectedIndex = 0;
                                cmbTASwc.Enabled = false;
                            }
                            lblTaxAmnestySwc.Visible = false;
                        }
                        //20161004, liliana, CSODD16311, end
                    }
                    else
                    {
                        MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                //20210913, korvi, RDN-674, begin
                cmpsrNoRekSwc.Criteria = cmpsrCIFSwc.Text1 + "#" + cmbTASwc.SelectedIndex.ToString();
                cmpsrNoRekSwc.Text1 = "";
                cmpsrNoRekSwc.Validate();
                //20210913, korvi, RDN-674, end

                //20161004, liliana, CSODD16311, begin
                //}
                //20161004, liliana, CSODD16311, end
                //20160812, Elva, LOGEN00191, end

            }
            else
            {
                txtUmurSwc.Text = "";
                textSIDSwc.Text = "";
                textShareHolderIdSwc.Text = "";
                textRekeningSwc.Text = "";
                //20150505, liliana, LIBST13020, begin
                maskedRekeningSwc.Text = "";
                //20150505, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningSwcUSD.Text = "";
                maskedRekeningSwcMC.Text = "";
                //20150728, liliana, LIBST13020, END
                textNamaRekeningSwc.Text = "";
                //20161004, liliana, CSODD16311, begin
                lblTaxAmnestySwc.Visible = false;
                cmbTASwc.SelectedIndex = -1;
                //20161004, liliana, CSODD16311, end
            }
        }

        private void cmpsrCIFSwcRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCIFSwcRDB.Text2 = "";
        }

        private void cmpsrCIFSwcRDB_onNispText2Changed(object sender, EventArgs e)
        {
            int intUmur = 0;

            if (cmpsrCIFSwcRDB.Text1 != "")
            {
                //20150723, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    if (!CheckCIF(cmpsrCIFSwcRDB.Text1.Trim()))
                    {
                        subCancel();
                        return;
                    }

                    //20170828, liliana, COPOD17271, begin
                    if (!this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIFSwcRDB.Text1.Trim(), strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        subCancel();
                        return;
                    }
                    //20170828, liliana, COPOD17271, end
                }
                //20150723, liliana, LIBST13020, end
                string strShareholderID, strNoRek, strNamaRek, strSID;
                //20150518, liliana, LIBST13020, begin
                //GetDataCIF(cmpsrCIFSwcRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                string strNoRekUSD, strNamaRekUSD;
                //20150702, liliana, LIBST13020, begin
                //20150728, liliana, LIBST13020, begin
                string strNoRekMC, strNamaRekMC;
                //20150728, liliana, LIBST13020, end
                string strRiskProfile;
                DateTime dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                GetDataCIF(cmpsrCIFSwcRDB.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                    , out strNoRekUSD, out strNamaRekUSD
                    //20150702, liliana, LIBST13020, begin
                    //20150728, liliana, LIBST13020, begin
                    , out strNoRekMC, out strNamaRekMC
                    //20150728, liliana, LIBST13020, end
                    , out strRiskProfile, out dtLastUpdateRiskProfile
                    //20150702, liliana, LIBST13020, end
                    );
                //20150518, liliana, LIBST13020, end
                //20150702, liliana, LIBST13020, begin
                txtbRiskProfileSwcRDB.Text = strRiskProfile;
                dtpRiskProfileSwcRDB.Value = dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                textSIDSwcRDB.Text = strSID;
                textShareHolderIdSwcRDB.Text = strShareholderID;
                textRekeningSwcRDB.Text = strNoRek;
                //20150505, liliana, LIBST13020, begin
                maskedRekeningSwcRDB.Text = strNoRek;
                //20150505, liliana, LIBST13020, end
                textNamaRekeningSwcRDB.Text = strNamaRek;
                //20150518, liliana, LIBST13020, begin
                maskedRekeningSwcRDBUSD.Text = strNoRekUSD;
                textNamaRekeningSwcRDBUSD.Text = strNamaRekUSD;
                //20150518, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningSwcRDBMC.Text = strNoRekMC;
                textNamaRekeningSwcRDBMC.Text = strNamaRekMC;
                //20150728, liliana, LIBST13020, end

                //20150505, liliana, LIBST13020, begin
                cmpsrNoRefSwcRDB.Criteria = cmpsrCIFSwcRDB.Text1.Trim();
                //20150505, liliana, LIBST13020, end
                cmpsrProductSwcRDBOut.Criteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1;

                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSwcRDBOut.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBOut.Text1 + "#" + _strTabName;
                cmpsrClientSwcRDBOut.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBOut.Text1 + "#" + _strTabName
                                                 + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFSwcRDB.Text1);
                txtUmurSwcRDB.Text = intUmur.ToString();

                //20150827, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150827, liliana, LIBST13020, end
                    if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSwcRDB.Text1))
                    {
                        checkPhoneOrderSwcRDB.Enabled = false;
                        checkPhoneOrderSwcRDB.Checked = false;
                    }
                    else
                    {
                        checkPhoneOrderSwcRDB.Enabled = true;

                    }
                    //20150827, liliana, LIBST13020, begin
                }
                //20150827, liliana, LIBST13020, end

                //20160812, Elva, LOGEN00191, begin
                //20161004, liliana, CSODD16311, begin
                //if (_intType == 1 || _intType == 2)
                //{
                //20161004, liliana, CSODD16311, end
                if (cmpsrCIFSwcRDB.Text1 != "" && cmpsrCIFSwcRDB.Text2 != "" && _isCheckingTASwcRDB == true)
                {
                    string strIsAllowed = "", strErrorMessage = "";
                    if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIFSwcRDB.Text1, out strIsAllowed, out strErrorMessage))
                    {
                        if (strIsAllowed == "0")
                        {
                            //20160901, liliana, LOGEN00196, begin
                            //DialogResult ds = MessageBox.Show(strErrorMessage, "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            //if (ds == DialogResult.No)
                            //    ResetFormSwcRDB();

                            //return;
                            //20161004, liliana, CSODD16311, begin
                            //MessageBox.Show("No CIF teridentifikasi sebagai nasabah Tax Amnesty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASwcRDB.SelectedIndex = 1;
                                cmbTASwcRDB.Enabled = true;
                            }
                            lblTaxAmnestySwcRDB.Visible = true;
                            //20161004, liliana, CSODD16311, end
                            //20160901, liliana, LOGEN00196, end
                        }
                        //20161004, liliana, CSODD16311, begin
                        //20161107, liliana, CSODD16311, begin
                        else if (strIsAllowed == "2")
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASwcRDB.SelectedIndex = 0;
                                cmbTASwcRDB.Enabled = false;
                            }
                            lblTaxAmnestySwcRDB.Visible = true;
                        }
                        //20161107, liliana, CSODD16311, end
                        else
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTASwcRDB.SelectedIndex = 0;
                                cmbTASwcRDB.Enabled = false;
                            }
                            lblTaxAmnestySwcRDB.Visible = false;
                        }
                        //20161004, liliana, CSODD16311, end
                    }
                    else
                    {
                        MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                //20210913, korvi, RDN-674, begin
                cmpsrNoRekSwcRDB.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                cmpsrNoRekSwcRDB.Text1 = "";
                cmpsrNoRekSwcRDB.Validate();
                //20210913, korvi, RDN-674, end

                //20161004, liliana, CSODD16311, begin
                //}
                //20161004, liliana, CSODD16311, end
                //20160812, Elva, LOGEN00191, end
            }
            else
            {
                txtUmurSwcRDB.Text = "";
                textSIDSwcRDB.Text = "";
                textShareHolderIdSwcRDB.Text = "";
                textRekeningSwcRDB.Text = "";
                //20150505, liliana, LIBST13020, begin
                maskedRekeningSwcRDB.Text = "";
                //20150505, liliana, LIBST13020, end
                //20150728, liliana, LIBST13020, begin
                maskedRekeningSwcRDBUSD.Text = "";
                maskedRekeningSwcRDBMC.Text = "";
                //20150728, liliana, LIBST13020, end
                textNamaRekeningSwcRDB.Text = "";
                //20161004, liliana, CSODD16311, begin
                lblTaxAmnestySwcRDB.Visible = false;
                cmbTASwcRDB.SelectedIndex = -1;
                //20161004, liliana, CSODD16311, end
            }
        }

        private void cmpsrCIFBooking_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCIFBooking.Text2 = "";
        }

        private void cmpsrCIFBooking_onNispText2Changed(object sender, EventArgs e)
        {
            int intUmur = 0;

            if (cmpsrCIFBooking.Text1 != "")
            {
                //20150723, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    if (!CheckCIF(cmpsrCIFBooking.Text1.Trim()))
                    {
                        subCancel();
                        return;
                    }

                    //20170828, liliana, COPOD17271, begin
                    if (!this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIFBooking.Text1.Trim(), strBranch, intClassificationId.ToString(),
                     out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        subCancel();
                        return;
                    }
                    //20170828, liliana, COPOD17271, end
                }
                //20150723, liliana, LIBST13020, end
                string strShareholderID, strNoRek, strNamaRek, strSID;
                //20150518, liliana, LIBST13020, begin
                //GetDataCIF(cmpsrCIFBooking.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID);
                string strNoRekUSD, strNamaRekUSD;
                //20150702, liliana, LIBST13020, begin
                //201507028, liliana, LIBST13020, begin
                string strNoRekMC, strNamaRekMC;
                //201507028, liliana, LIBST13020, end
                string strRiskProfile;
                DateTime dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                GetDataCIF(cmpsrCIFBooking.Text1.Trim(), out strShareholderID, out strNoRek, out strNamaRek, out strSID
                    , out strNoRekUSD, out strNamaRekUSD
                    //201507028, liliana, LIBST13020, begin
                    , out strNoRekMC, out strNamaRekMC
                    //201507028, liliana, LIBST13020, END
                    //20150702, liliana, LIBST13020, begin
                    , out strRiskProfile, out dtLastUpdateRiskProfile
                    //20150702, liliana, LIBST13020, end

                    );
                //20150518, liliana, LIBST13020, end
                //20150702, liliana, LIBST13020, begin
                txtbRiskProfileBooking.Text = strRiskProfile;
                dtpRiskProfileBooking.Value = dtLastUpdateRiskProfile;
                //20150702, liliana, LIBST13020, end
                textSIDBooking.Text = strSID;
                textShareHolderIdBooking.Text = strShareholderID;
                textRekeningBooking.Text = strNoRek;
                //20150505, liliana, LIBST13020, begin
                maskedRekeningBooking.Text = strNoRek;
                //20150505, liliana, LIBST13020, end
                textNamaRekeningBooking.Text = strNamaRek;
                //20150518, liliana, LIBST13020, begin
                maskedRekeningBookingUSD.Text = strNoRekUSD;
                textNamaRekeningBookingUSD.Text = strNamaRekUSD;
                //20150518, liliana, LIBST13020, end
                //201507028, liliana, LIBST13020, begin
                maskedRekeningBookingMC.Text = strNoRekMC;
                textNamaRekeningBookingMC.Text = strNamaRekMC;
                //201507028, liliana, LIBST13020, END

                //20150505, liliana, LIBST13020, begin
                cmpsrNoRefBooking.Criteria = cmpsrCIFBooking.Text1.Trim();
                //20150505, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientBooking.Criteria = cmpsrCIFBooking.Text1.Trim() + "#" + cmpsrProductBooking.Text1.Trim() + "#" + _strTabName;
                cmpsrClientBooking.Criteria = cmpsrCIFBooking.Text1.Trim() + "#" + cmpsrProductBooking.Text1.Trim() + "#" + _strTabName
                                                + "#" + cmbTABook.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                intUmur = GlobalFunctionCIF.HitungUmur(cmpsrCIFBooking.Text1);
                txtUmurBooking.Text = intUmur.ToString();

                //20150827, liliana, LIBST13020, begin
                if (_intType == 1)
                {
                    //20150827, liliana, LIBST13020, end
                    if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFBooking.Text1))
                    {
                        checkPhoneOrderBooking.Enabled = false;
                        checkPhoneOrderBooking.Checked = false;
                    }
                    else
                    {
                        checkPhoneOrderBooking.Enabled = true;
                    }
                    //20150827, liliana, LIBST13020, begin
                }
                //20150827, liliana, LIBST13020, end

                //20160812, Elva, LOGEN00191, begin
                //20161004, liliana, CSODD16311, begin
                //if (_intType == 1 || _intType == 2)
                //{
                //20161004, liliana, CSODD16311, end
                if (cmpsrCIFBooking.Text1 != "" && cmpsrCIFBooking.Text2 != "" && _isCheckingTABook == true)
                {
                    string strIsAllowed = "", strErrorMessage = "";
                    if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIFBooking.Text1, out strIsAllowed, out strErrorMessage))
                    {
                        if (strIsAllowed == "0")
                        {
                            //20160901, liliana, LOGEN00196, begin
                            //DialogResult ds = MessageBox.Show(strErrorMessage, "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            //if (ds == DialogResult.No)
                            //    ResetFormBooking();

                            //return;
                            //20161004, liliana, CSODD16311, begin
                            //MessageBox.Show("No CIF teridentifikasi sebagai nasabah Tax Amnesty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTABook.SelectedIndex = 1;
                                cmbTABook.Enabled = true;
                            }
                            lblTaxAmnestyBooking.Visible = true;
                            //20161004, liliana, CSODD16311, end
                            //20160901, liliana, LOGEN00196, end
                        }
                        //20161004, liliana, CSODD16311, begin
                        //20161107, liliana, CSODD16311, begin
                        else if (strIsAllowed == "2")
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTABook.SelectedIndex = 0;
                                cmbTABook.Enabled = false;
                            }
                            lblTaxAmnestyBooking.Visible = true;
                        }
                        //20161107, liliana, CSODD16311, end
                        else
                        {
                            if (_intType == 1 || _intType == 2)
                            {
                                cmbTABook.SelectedIndex = 0;
                                cmbTABook.Enabled = false;
                            }
                            lblTaxAmnestyBooking.Visible = false;
                        }
                        //20161004, liliana, CSODD16311, end
                    }
                    else
                    {
                        MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                //20210913, korvi, RDN-674, begin
                cmpsrNoRekBooking.Criteria = cmpsrCIFBooking.Text1 + "#" + cmbTABook.SelectedIndex.ToString();
                cmpsrNoRekBooking.Text1 = "";
                cmpsrNoRekBooking.Validate();
                //20210913, korvi, RDN-674, end


                //20161004, liliana, CSODD16311, begin
                //}
                //20161004, liliana, CSODD16311, end
                //20160812, Elva, LOGEN00191, end
            }
            else
            {
                txtUmurBooking.Text = "";
                textSIDBooking.Text = "";
                textShareHolderIdBooking.Text = "";
                textRekeningBooking.Text = "";
                //20150505, liliana, LIBST13020, begin
                maskedRekeningBooking.Text = "";
                //20150505, liliana, LIBST13020, end
                //201507028, liliana, LIBST13020, begin
                maskedRekeningBookingUSD.Text = "";
                maskedRekeningBookingMC.Text = "";
                //201507028, liliana, LIBST13020, end
                textNamaRekeningBooking.Text = "";
                //20161004, liliana, CSODD16311, begin
                lblTaxAmnestyBooking.Visible = false;
                cmbTABook.SelectedIndex = -1;
                //20161004, liliana, CSODD16311, end
            }
        }

        private void cmpsrSellerSubs_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrSellerSubs.Text2 = "";
            cmpsrWaperdSubs.Text1 = cmpsrSellerSubs.Text1;
            cmpsrWaperdSubs.ValidateField();
        }

        private void cmpsrSellerSubs_onNispText2Changed(object sender, EventArgs e)
        {

        }

        private void cmpsrSellerSubs_onNispText1Enter(object sender, EventArgs e)
        {
            cmpsrSellerSubs.ValidateField();
            cmpsrWaperdSubs.Text1 = cmpsrSellerSubs.Text1;
            cmpsrWaperdSubs.ValidateField();
        }

        private void cmpsrSellerSubs_Leave(object sender, EventArgs e)
        {
            cmpsrSellerSubs.ValidateField();
            cmpsrWaperdSubs.Text1 = cmpsrSellerSubs.Text1;
            cmpsrWaperdSubs.ValidateField();
        }

        private void cmpsrSellerRedemp_Leave(object sender, EventArgs e)
        {
            cmpsrSellerRedemp.ValidateField();
            cmpsrWaperdRedemp.Text1 = cmpsrSellerRedemp.Text1;
            cmpsrWaperdRedemp.ValidateField();
        }

        private void cmpsrSellerRedemp_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrSellerRedemp.Text2 = "";
            cmpsrWaperdRedemp.Text1 = cmpsrSellerRedemp.Text1;
            cmpsrWaperdRedemp.ValidateField();
        }

        private void cmpsrSellerRedemp_onNispText1Enter(object sender, EventArgs e)
        {
            cmpsrSellerRedemp.ValidateField();
            cmpsrWaperdRedemp.Text1 = cmpsrSellerRedemp.Text1;
            cmpsrWaperdRedemp.ValidateField();
        }

        private void cmpsrSellerRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrSellerRDB.Text2 = "";
            cmpsrWaperdRDB.Text1 = cmpsrSellerRDB.Text1;
            cmpsrWaperdRDB.ValidateField();
        }

        private void cmpsrSellerRDB_onNispText1Enter(object sender, EventArgs e)
        {
            cmpsrSellerRDB.ValidateField();
            cmpsrWaperdRDB.Text1 = cmpsrSellerRDB.Text1;
            cmpsrWaperdRDB.ValidateField();
        }

        private void cmpsrSellerRDB_Leave(object sender, EventArgs e)
        {
            cmpsrSellerRDB.ValidateField();
            cmpsrWaperdRDB.Text1 = cmpsrSellerRDB.Text1;
            cmpsrWaperdRDB.ValidateField();
        }

        private void cmpsrSellerSwc_Leave(object sender, EventArgs e)
        {
            cmpsrSellerSwc.ValidateField();
            cmpsrWaperdSwc.Text1 = cmpsrSellerSwc.Text1;
            cmpsrWaperdSwc.ValidateField();
        }

        private void cmpsrSellerSwc_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrSellerSwc.Text2 = "";
            cmpsrWaperdSwc.Text1 = cmpsrSellerSwc.Text1;
            cmpsrWaperdSwc.ValidateField();
        }

        private void cmpsrSellerSwc_onNispText1Enter(object sender, EventArgs e)
        {
            cmpsrSellerSwc.ValidateField();
            cmpsrWaperdSwc.Text1 = cmpsrSellerSwc.Text1;
            cmpsrWaperdSwc.ValidateField();
        }

        private void cmpsrSellerSwcRDB_Leave(object sender, EventArgs e)
        {
            cmpsrSellerSwcRDB.ValidateField();
            cmpsrWaperdSwcRDB.Text1 = cmpsrSellerSwcRDB.Text1;
            cmpsrWaperdSwcRDB.ValidateField();
        }

        private void cmpsrSellerSwcRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrSellerSwcRDB.Text2 = "";
            cmpsrWaperdSwcRDB.Text1 = cmpsrSellerSwcRDB.Text1;
            cmpsrWaperdSwcRDB.ValidateField();
        }

        private void cmpsrSellerSwcRDB_onNispText1Enter(object sender, EventArgs e)
        {
            cmpsrSellerSwcRDB.ValidateField();
            cmpsrWaperdSwcRDB.Text1 = cmpsrSellerSwcRDB.Text1;
            cmpsrWaperdSwcRDB.ValidateField();
        }

        private void cmpsrSellerBooking_Leave(object sender, EventArgs e)
        {
            cmpsrSellerBooking.ValidateField();
            cmpsrWaperdBooking.Text1 = cmpsrSellerBooking.Text1;
            cmpsrWaperdBooking.ValidateField();
        }

        private void cmpsrSellerBooking_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrSellerBooking.Text2 = "";
            cmpsrWaperdBooking.Text1 = cmpsrSellerBooking.Text1;
            cmpsrWaperdBooking.ValidateField();
        }

        private void cmpsrSellerBooking_onNispText1Enter(object sender, EventArgs e)
        {
            cmpsrSellerBooking.ValidateField();
            cmpsrWaperdBooking.Text1 = cmpsrSellerBooking.Text1;
            cmpsrWaperdBooking.ValidateField();
        }

        private void cmpsrWaperdSubs_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                if (cmpsrWaperdSubs.Text1 != "")
                {
                    DateTime TanggalExpire = new DateTime();
                    TanggalExpire = (DateTime)cmpsrWaperdSubs[2];
                    textExpireWaperdSubs.Text = TanggalExpire.ToString("dd-MM-yyyy");
                }
                else
                {
                    textExpireWaperdSubs.Text = "";
                }
            }
            catch
            {
                textExpireWaperdSubs.Text = "";
            }
        }

        private void cmpsrWaperdRedemp_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                if (cmpsrWaperdRedemp.Text1 != "")
                {
                    DateTime TanggalExpire = new DateTime();
                    TanggalExpire = (DateTime)cmpsrWaperdRedemp[2];
                    textExpireWaperdRedemp.Text = TanggalExpire.ToString("dd-MM-yyyy");
                }
                else
                {
                    textExpireWaperdRedemp.Text = "";
                }
            }
            catch
            {
                textExpireWaperdRedemp.Text = "";
            }
        }

        private void cmpsrWaperdRDB_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                if (cmpsrWaperdRDB.Text1 != "")
                {
                    DateTime TanggalExpire = new DateTime();
                    TanggalExpire = (DateTime)cmpsrWaperdRDB[2];
                    textExpireWaperdRDB.Text = TanggalExpire.ToString("dd-MM-yyyy");
                }
                else
                {
                    textExpireWaperdRDB.Text = "";
                }
            }
            catch
            {
                textExpireWaperdRDB.Text = "";
            }
        }

        private void cmpsrWaperdSwc_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                if (cmpsrWaperdSwc.Text1 != "")
                {
                    DateTime TanggalExpire = new DateTime();
                    TanggalExpire = (DateTime)cmpsrWaperdSwc[2];
                    textExpireWaperdSwc.Text = TanggalExpire.ToString("dd-MM-yyyy");
                }
                else
                {
                    textExpireWaperdSwc.Text = "";
                }
            }
            catch
            {
                textExpireWaperdSwc.Text = "";
            }
        }

        private void cmpsrWaperdSwcRDB_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                if (cmpsrWaperdSwcRDB.Text1 != "")
                {
                    DateTime TanggalExpire = new DateTime();
                    TanggalExpire = (DateTime)cmpsrWaperdSwcRDB[2];
                    textExpireWaperdSwcRDB.Text = TanggalExpire.ToString("dd-MM-yyyy");
                }
                else
                {
                    textExpireWaperdSwcRDB.Text = "";
                }
            }
            catch
            {
                textExpireWaperdSwcRDB.Text = "";
            }
        }

        private void cmpsrWaperdBooking_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                if (cmpsrWaperdBooking.Text1 != "")
                {
                    DateTime TanggalExpire = new DateTime();
                    TanggalExpire = (DateTime)cmpsrWaperdBooking[2];
                    textExpireWaperdBooking.Text = TanggalExpire.ToString("dd-MM-yyyy");
                }
                else
                {
                    textExpireWaperdBooking.Text = "";
                }
            }
            catch
            {
                textExpireWaperdBooking.Text = "";
            }
        }

        private void InquirySisaUnit()
        {
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[5];

            decimal NAV = 0;
            decimal sisaunit, nav, jml;

            try
            {
                (dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcProdCode", System.Data.OleDb.OleDbType.VarChar, 10)).Value = cmpsrProductBooking.Text1.Trim();
                (dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnSisaUnit", System.Data.OleDb.OleDbType.Double)).Value = 0;
                (dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer)).Value = intNIK;
                (dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50)).Value = strGuid;
                (dbParam[4] = new System.Data.OleDb.OleDbParameter("@pdCurrDate", System.Data.OleDb.OleDbType.Date)).Value = DateTime.Today;

                dbParam[1].Direction = ParameterDirection.Output;

                bool OK = ClQ.ExecProc("dbo.ReksaInqUnitDitwrkan", ref dbParam);

                _sisaunit.Text = String.Format("{0:F2}", dbParam[1].Value);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
            }
        }

        private void _inquiry_Click(object sender, EventArgs e)
        {
            InquirySisaUnit();
        }

        private void checkFeeEditSubs_CheckedChanged(object sender, EventArgs e)
        {
            //20150415, liliana, LIBST13020, begin
            //20150828, liliana, LIBST13020, begin
            //cmpsrProductSubs.ValidateField();
            //20150828, liliana, LIBST13020, end
            //20150415, liliana, LIBST13020, end

            

            if (checkFeeEditSubs.Checked)
            {
                //20231121, ahmad.fansyuri, HTR-189, begin
                //if (_intType != 0)
                //{
                //    _ComboJenisSubs.Enabled = true;
                //    nispMoneyFeeSubs.Enabled = true;
                //    nispPercentageFeeSubs.Enabled = false;
                //}

                _ComboJenisSubs.Enabled = false;
                _ComboJenisSubs.SelectedIndex = 1;
                nispMoneyFeeSubs.Enabled = true;
                nispPercentageFeeSubs.Enabled = false;

                //20231121, ahmad.fansyuri, HTR-189, end

            }
            else
            {
                try
                {
                    if (cmpsrProductSubs.Text1 != "")
                    {
                        _ComboJenisSubs.Enabled = false;
                        nispMoneyFeeSubs.Enabled = false;
                        nispPercentageFeeSubs.Enabled = false;

                        _ComboJenisSubs.SelectedIndex = 1;
                        nispMoneyFeeSubs.Value = (decimal)Fee;

                        decimal decNominalFee, decPctFee;
                        int intProdId, intClientid, intTranType;
                        string strPrdId = GetImportantData("PRODUKID", cmpsrProductSubs.Text1);
                        int.TryParse(strPrdId, out intProdId);

                        if (IsSubsNew)
                        {
                            intTranType = 1;
                            intClientid = 0;
                        }
                        else
                        {
                            intTranType = 2;
                            string strClientId = GetImportantData("CLIENTID", cmpsrClientSubs.Text1);
                            int.TryParse(strClientId, out intClientid);
                        }

                        HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
                            checkFeeEditSubs.Checked, nispPercentageFeeSubs.Value, 1, out strFeeCurr, out decNominalFee,
                            out decPctFee, cmpsrCIFSubs.Text1);

                        nispMoneyFeeSubs.Value = decNominalFee;
                        nispPercentageFeeSubs.Value = decPctFee;
                        labelFeeCurrencySubs.Text = "%";
                        _KeteranganFeeSubs.Text = strFeeCurr;
                    }
                }
                catch
                {
                    return;
                }
            }
            //20231121, ahmad.fansyuri, HTR-189, end


            //20231121, ahmad.fansyuri, HTR-189, begin Remarks
            //if (checkFeeEditSubs.Checked)
            //{
            //    if (_intType != 0)
            //    {
            //        _ComboJenisSubs.Enabled = true;
            //        nispMoneyFeeSubs.Enabled = true;
            //        nispPercentageFeeSubs.Enabled = false;
            //    }
            //}
            //else
            //{
            //    //20150415, liliana, LIBST13020, begin
            //    try
            //    {
            //        //20150505, liliana, LIBST13020, begin
            //        if (cmpsrProductSubs.Text1 != "")
            //        {
            //            //20150505, liliana, LIBST13020, end
            //            //20150415, liliana, LIBST13020, end
            //            _ComboJenisSubs.Enabled = false;
            //            nispMoneyFeeSubs.Enabled = false;
            //            nispPercentageFeeSubs.Enabled = false;

            //            //20150424, liliana, LIBST13020, begin
            //            //_ComboJenisSubs.SelectedIndex = 0;
            //            _ComboJenisSubs.SelectedIndex = 1;
            //            //20150424, liliana, LIBST13020, end
            //            nispMoneyFeeSubs.Value = (decimal)Fee;

            //            decimal decNominalFee, decPctFee;
            //            int intProdId, intClientid, intTranType;
            //            //20150827, liliana, LIBST13020, begin
            //            //int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);
            //            string strPrdId = GetImportantData("PRODUKID", cmpsrProductSubs.Text1);
            //            int.TryParse(strPrdId, out intProdId);
            //            //20150827, liliana, LIBST13020, end

            //            if (IsSubsNew)
            //            {
            //                intTranType = 1;
            //                intClientid = 0;
            //            }
            //            else
            //            {
            //                intTranType = 2;
            //                //20150618, liliana, LIBST13020, begin
            //                //20150828, liliana, LIBST13020, begin
            //                //cmpsrClientSubs.ValidateField();
            //                //20150828, liliana, LIBST13020, end
            //                //20150618, liliana, LIBST13020, end
            //                //20150827, liliana, LIBST13020, begin
            //                //int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
            //                string strClientId = GetImportantData("CLIENTID", cmpsrClientSubs.Text1);
            //                int.TryParse(strClientId, out intClientid);
            //                //20150827, liliana, LIBST13020, end
            //            }

            //            HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
            //                checkFeeEditSubs.Checked, nispPercentageFeeSubs.Value, 1, out strFeeCurr, out decNominalFee,
            //                out decPctFee, cmpsrCIFSubs.Text1);

            //            //20150505, liliana, LIBST13020, begin
            //            //nispMoneyFeeSubs.Value = decNominalFee;
            //            //nispPercentageFeeSubs.Value = decPctFee;
            //            //labelFeeCurrencySubs.Text = strFeeCurr;
            //            nispMoneyFeeSubs.Value = decNominalFee;
            //            nispPercentageFeeSubs.Value = decPctFee;
            //            labelFeeCurrencySubs.Text = "%";
            //            _KeteranganFeeSubs.Text = strFeeCurr;
            //        }
            //        //20150505, liliana, LIBST13020, end
            //        //20150415, liliana, LIBST13020, begin
            //    }
            //    catch
            //    {
            //        return;
            //    }
            //    //20150415, liliana, LIBST13020, end

            //}

            //20231121, ahmad.fansyuri, HTR-189, end Remarks

        }

        private void checkFeeEditRedemp_CheckedChanged(object sender, EventArgs e)
        {
            //20150415, liliana, LIBST13020, begin
            //20150828, liliana, LIBST13020, begin
            //cmpsrProductRedemp.ValidateField();
            //cmpsrClientRedemp.ValidateField();
            //20150828, liliana, LIBST13020, end
            //20150415, liliana, LIBST13020, end

            if (checkFeeEditRedemp.Checked)
            {
                _ComboJenisRedemp.Enabled = false;
                _ComboJenisRedemp.SelectedIndex = 1;

                nispMoneyFeeRedemp.Enabled = true;
                nispPercentageFeeRedemp.Enabled = false;
            }
            else
            {
                //20150415, liliana, LIBST13020, begin
                try
                {
                    //20150415, liliana, LIBST13020, end
                    _ComboJenisRedemp.Enabled = false;
                    nispMoneyFeeRedemp.Enabled = false;
                    nispPercentageFeeRedemp.Enabled = false;

                    //20150424, liliana, LIBST13020, begin
                    //_ComboJenisRedemp.SelectedIndex = 0;
                    _ComboJenisRedemp.SelectedIndex = 1;
                    //20150424, liliana, LIBST13020, end
                    nispMoneyFeeRedemp.Value = (decimal)Fee;

                    decimal decNominalFee, decPctFee;
                    int intProdId, intClientid, intTranType;
                    //20150828, liliana, LIBST13020, begin
                    //int.TryParse(cmpsrProductRedemp[2].ToString(), out intProdId);
                    //int.TryParse(cmpsrClientRedemp[2].ToString(), out intClientid);
                    string strPrdId = GetImportantData("PRODUKID", cmpsrProductRedemp.Text1);
                    int.TryParse(strPrdId, out intProdId);

                    string strClientId = GetImportantData("CLIENTID", cmpsrClientRedemp.Text1);
                    int.TryParse(strClientId, out intClientid);
                    //20150828, liliana, LIBST13020, end
                    //20150619, liliana, LIBST13020, begin
                    if ((_ComboJenisRedemp.Text == "By %") && (checkFeeEditRedemp.Checked))
                    {
                        ByPercent = true;
                    }
                    else
                    {
                        ByPercent = false;
                    }
                    //20150619, liliana, LIBST13020, end


                    if (IsRedempAll)
                    {
                        intTranType = 4;
                    }
                    else
                    {
                        intTranType = 3;
                    }

                    HitungFee(intProdId, intClientid, intTranType, 0, nispRedempUnit.Value, false,
                        //20150619, liliana, LIBST13020, begin
                        //checkFeeEditRedemp.Checked, nispPercentageFeeRedemp.Value, 1, out strFeeCurr,
                        checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, 1, out strFeeCurr,
                        //20150619, liliana, LIBST13020, end
                        out decNominalFee, out decPctFee, cmpsrCIFRedemp.Text1);

                    //20150619, liliana, LIBST13020, begin
                    //nispMoneyFeeRedemp.Value = decNominalFee;
                    //nispPercentageFeeRedemp.Value = decPctFee;
                    nispMoneyFeeRedemp.Value = decPctFee;
                    nispPercentageFeeRedemp.Value = decNominalFee;
                    //20150619, liliana, LIBST13020, end
                    //20150505, liliana, LIBST13020, begin
                    //labelFeeCurrencyRedemp.Text = strFeeCurr;
                    labelFeeCurrencyRedemp.Text = "%";
                    _KeteranganFeeRedemp.Text = strFeeCurr;
                    //20150505, liliana, LIBST13020, end
                    //20150415, liliana, LIBST13020, begin
                }
                catch
                {
                    return;
                }
                //20150415, liliana, LIBST13020, end
            }
        }

        private void checkFeeEditRDB_CheckedChanged(object sender, EventArgs e)
        {
            if (checkFeeEditRDB.Checked)
            {
                _ComboJenisRDB.Enabled = false;
                nispMoneyFeeRDB.Enabled = false;
                nispPercentageFeeRDB.Enabled = false;
                checkFeeEditRDB.Checked = false;
            }
        }

        private void checkFeeEditSwc_CheckedChanged(object sender, EventArgs e)
        {

            if (checkFeeEditSwc.Checked)
            {
                nispMoneyFeeSwc.Enabled = false;
                nispPercentageFeeSwc.Enabled = true;
            }
            else
            {
                nispMoneyFeeSwc.Enabled = false;
                nispPercentageFeeSwc.Enabled = false;

                nispMoneyFeeSwc.Value = (decimal)Fee;
                nispPercentageFeeSwc.Value = (decimal)PercentFee;
            }

        }

        private void checkFeeEditSwcRDB_CheckedChanged(object sender, EventArgs e)
        {
            if (checkFeeEditSwcRDB.Checked)
            {
                nispMoneyFeeSwcRDB.Enabled = true;
                nispPercentageFeeSwcRDB.Enabled = true;
            }
            else
            {
                nispMoneyFeeSwcRDB.Enabled = false;
                nispPercentageFeeSwcRDB.Enabled = false;

                nispMoneyFeeSwcRDB.Value = (decimal)Fee;
                nispPercentageFeeSwcRDB.Value = (decimal)PercentFee;
            }
        }

        private void checkFeeEditBooking_CheckedChanged(object sender, EventArgs e)
        {
            if (checkFeeEditBooking.Checked)
            {
                _ComboJenisBooking.Enabled = true;
                nispMoneyFeeBooking.Enabled = true;
                nispPercentageFeeBooking.Enabled = false;
            }
            else
            {
                _ComboJenisBooking.Enabled = false;
                nispMoneyFeeBooking.Enabled = false;
                nispPercentageFeeBooking.Enabled = false;

                _ComboJenisBooking.SelectedIndex = 0;
                nispMoneyFeeBooking.Value = (decimal)Fee;

                string strFeeCurr;
                decimal decNominalFee, decPctFee;

                HitungBookingFee(cmpsrCIFBooking.Text1, nispMoneyNomBooking.Value, cmpsrProductBooking.Text1, ByPercent,
                    checkFeeEditBooking.Checked, nispMoneyFeeBooking.Value, out decPctFee, out strFeeCurr, out decNominalFee);

                if (checkFeeEditBooking.Checked == false) //hitung fee tanpa edit fee
                {
                    nispMoneyFeeBooking.Value = decNominalFee;
                    nispPercentageFeeBooking.Value = decPctFee;
                    labelFeeCurrencyBooking.Text = strFeeCurr;
                    _KeteranganFeeBooking.Text = "%";
                }
                else if (checkFeeEditBooking.Checked == true) //hitung fee dengan edit fee
                {
                    if (ByPercent)
                    {
                        nispPercentageFeeBooking.Value = decNominalFee;
                        labelFeeCurrencyBooking.Text = "%";
                        _KeteranganFeeBooking.Text = strFeeCurr;
                    }
                    else
                    {
                        nispPercentageFeeBooking.Value = decPctFee;
                        labelFeeCurrencyBooking.Text = strFeeCurr;
                        _KeteranganFeeBooking.Text = "%";

                    }
                }

            }
        }

        private void _tabJenisTransaksi_Deselected(object sender, TabControlEventArgs e)
        {
            try
            {
                _strLastTabName = _tabJenisTransaksi.SelectedTab.Name.ToString();

            }
            catch
            {
                _strLastTabName = "";
            }
        }

        private void _tabJenisTransaksi_Selected(object sender, TabControlEventArgs e)
        {
            _strTabName = _tabJenisTransaksi.SelectedTab.Name.ToString();
            cTransaksi = new clsTransaksi(_strTabName, intNIK, strGuid, ClQ);

            cmpsrNoRefSubs.Criteria = _strTabName;
            cmpsrNoRefRedemp.Criteria = _strTabName;
            cmpsrNoRefRDB.Criteria = _strTabName;
            //20150930, liliana, LIBST13020, begin
            if (_strTabName == "SWCRDB")
            {
                this.NISPToolbarButton("4").Enabled = false;
            }
            else
            {
                this.NISPToolbarButton("4").Enabled = true;
            }
            //20150930, liliana, LIBST13020, end
        }

        private void _tabJenisTransaksi_Selecting(object sender, TabControlCancelEventArgs e)
        {
            _strTabName = _tabJenisTransaksi.SelectedTab.Name.ToString();

            if ((_intType == 1) || (_intType == 2))
            {
                if ((_strLastTabName == "SUBS")
                    && ((_strTabName == "REDEMP") || (_strTabName == "SUBSRDB") || (_strTabName == "SWCNONRDB")
                    || (_strTabName == "SWCRDB") || (_strTabName == "BOOK")
                    ))
                {
                    e.Cancel = true;
                }

                if ((_strLastTabName == "REDEMP")
                 && ((_strTabName == "SUBS") || (_strTabName == "SUBSRDB") || (_strTabName == "SWCNONRDB")
                    || (_strTabName == "SWCRDB") || (_strTabName == "BOOK")
                    ))
                {
                    e.Cancel = true;
                }

                if ((_strLastTabName == "SUBSRDB")
                 && ((_strTabName == "SUBS") || (_strTabName == "REDEMP") || (_strTabName == "SWCNONRDB")
                    || (_strTabName == "SWCRDB") || (_strTabName == "BOOK")
                    ))
                {
                    e.Cancel = true;
                }

                if ((_strLastTabName == "SWCNONRDB")
                 && ((_strTabName == "SUBS") || (_strTabName == "REDEMP") || (_strTabName == "SUBSRDB")
                    || (_strTabName == "SWCRDB") || (_strTabName == "BOOK")
                    ))
                {
                    e.Cancel = true;
                }

                if ((_strLastTabName == "SWCRDB")
                 && ((_strTabName == "SUBS") || (_strTabName == "REDEMP") || (_strTabName == "SUBSRDB")
                    || (_strTabName == "SWCNONRDB") || (_strTabName == "BOOK")
                    ))
                {
                    e.Cancel = true;
                }

                if ((_strLastTabName == "BOOK")
                 && ((_strTabName == "SUBS") || (_strTabName == "REDEMP") || (_strTabName == "SUBSRDB")
                    || (_strTabName == "SWCNONRDB") || (_strTabName == "SWCRDB")
                    ))
                {
                    e.Cancel = true;
                }
            }

            _strTabName = _strLastTabName;
        }

        //20150617, liliana, LIBST13020, begin
        //private bool CheckIsSubsNew(string CIFNo, int ProductId, bool IsRDB)
        private bool CheckIsSubsNew(string CIFNo, int ProductId, bool IsRDB, out string ClientCodeSubsAdd
            //20160829, liliana, LOGEN00196, begin
            , int IsTrxTA
            //20160829, liliana, LOGEN00196, end
            )
        //20150617, liliana, LIBST13020, end
        {
            bool IsSubsNew = false;
            //20150617, liliana, LIBST13020, begin
            ClientCodeSubsAdd = "";
            //20150617, liliana, LIBST13020, end

            //20150617, liliana, LIBST13020, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[4];
            //20160829, liliana, LOGEN00196, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[5];
            System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[6];
            //20160829, liliana, LOGEN00196, end
            //20150617, liliana, LIBST13020, end

            param[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
            param[0].Value = CIFNo;
            param[0].Direction = System.Data.ParameterDirection.Input;

            param[1] = new System.Data.OleDb.OleDbParameter("@pnProductId", System.Data.OleDb.OleDbType.Integer);
            param[1].Value = ProductId;
            param[1].Direction = System.Data.ParameterDirection.Input;

            param[2] = new System.Data.OleDb.OleDbParameter("@pbIsRDB", System.Data.OleDb.OleDbType.Boolean);
            param[2].Value = IsRDB;
            param[2].Direction = System.Data.ParameterDirection.Input;

            param[3] = new System.Data.OleDb.OleDbParameter("@pbIsSubsNew", System.Data.OleDb.OleDbType.Boolean);
            param[3].Value = IsSubsNew;
            param[3].Direction = System.Data.ParameterDirection.InputOutput;
            //20150617, liliana, LIBST13020, begin
            param[4] = new System.Data.OleDb.OleDbParameter("@pcClientCode", System.Data.OleDb.OleDbType.VarChar, 20);
            param[4].Value = ClientCodeSubsAdd;
            param[4].Direction = System.Data.ParameterDirection.InputOutput;
            //20150617, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin

            param[5] = new System.Data.OleDb.OleDbParameter("@pbIsTrxTA", System.Data.OleDb.OleDbType.Integer);
            param[5].Value = IsTrxTA;
            param[5].Direction = System.Data.ParameterDirection.Input;
            //20160829, liliana, LOGEN00196, end

            bool blnResult = ClQ.ExecProc("ReksaCheckSubsType", ref param);

            if (blnResult == true)
            {
                IsSubsNew = (bool)param[3].Value;
                //20150617, liliana, LIBST13020, begin
                ClientCodeSubsAdd = param[4].Value.ToString();
                //20150617, liliana, LIBST13020, end
            }

            return IsSubsNew;
        }

        private void cmpsrProductRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductRDB.Text2 = "";
            cmpsrClientRDB.Text1 = "";
            cmpsrClientRDB.Text2 = "";
            nispMoneyNomRDB.Value = 0;
            nispMoneyNomRDB.Text = "";
        }

        private void cmpsrProductRDB_onNispText2Changed(object sender, EventArgs e)
        {
            //20220322, gio, RDN-757, begin
            DataSet _ds;
            //20220322, gio, RDN-757, end
            try
            {
                cmpsrCurrRDB.Text1 = cmpsrProductRDB[3].ToString();
                cmpsrCurrRDB.ValidateField();

                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientRDB.Criteria = cmpsrCIFRDB.Text1.Trim() + "#" + cmpsrProductRDB.Text1.Trim() + "#" + _strTabName;
                cmpsrClientRDB.Criteria = cmpsrCIFRDB.Text1.Trim() + "#" + cmpsrProductRDB.Text1.Trim() + "#" + _strTabName
                                          + "#" + cmbTARDB.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                //20220322, gio, RDN-757, begin
                _ds = new DataSet();

                //reset combobox, diisi di event saat pilih metode debet
                cmbAsuransiRDB.DataSource = null;
                cmbFrekPendebetanRDB.DataSource = null;

                if (PopulateParamComboRDB("FREQDEBETMETHOD", cmpsrProductRDB.Text1, "", out _ds))
                {
                    if (_ds.Tables[0] != null)
                    {
                        if (_ds.Tables[0].Rows.Count > 0)
                        {
                            cmbFrekDebetMethodRDB.DataSource = _ds.Tables[0];
                            cmbFrekDebetMethodRDB.DisplayMember = "ComboText";
                            cmbFrekDebetMethodRDB.ValueMember = "ComboValue";

                            if (_ds.Tables[0].Rows.Count == 1)
                                cmbFrekDebetMethodRDB.Enabled = false;
                            else if (_ds.Tables[0].Rows.Count > 1)
                                cmbFrekDebetMethodRDB.Enabled = true;

                            cmbFrekDebetMethodRDB.SelectedIndex = 0;
                        }

                    }
                }
                //20220322, gio ,RDN-757, end

                IsSubsNew = true;
            }
            catch
            {
                cmpsrCurrRDB.Text1 = "";
            }
            //20221017, Andhika J, RDN-861, begin
            if (_strTabName == "SUBSRDB")
            {
                string IsAsuransi = "";
                ReksaValidateInsuranceRDB(cmpsrCIFRDB.Text1.Trim(), IsAsuransi);
            }
            //20221017, Andhika J, RDN-861, end

        }

        private void cmpsrProductSubs_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductSubs.Text2 = "";
            cmpsrClientSubs.Enabled = false;
            cmpsrClientSubs.Text1 = "";
            cmpsrClientSubs.Text2 = "";

            cmpsrCurrSubs.Text1 = "";
            cmpsrCurrSubs.Text2 = "";

            nispMoneyNomSubs.Value = 0;
            nispMoneyNomSubs.Text = "";
        }

        private void cmpsrProductSubs_onNispText2Changed(object sender, EventArgs e)
        {
            //20160829, liliana, LOGEN00196, begin
            if (cmbTASubs.SelectedIndex == -1)
            {
                MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                cmpsrProductSubs.Text1 = "";
                cmpsrProductSubs.Text2 = "";
                return;
            }

            //20160829, liliana, LOGEN00196, end
            try
            {
                cmpsrCurrSubs.Text1 = cmpsrProductSubs[3].ToString();
                cmpsrCurrSubs.ValidateField();

                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSubs.Criteria = cmpsrCIFSubs.Text1.Trim() + "#" + cmpsrProductSubs.Text1.Trim() + "#" + _strTabName;
                cmpsrClientSubs.Criteria = cmpsrCIFSubs.Text1.Trim() + "#" + cmpsrProductSubs.Text1.Trim() + "#" + _strTabName
                                            + "#" + cmbTASubs.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                //cek subs new / subs add
                if (_intType == 1)
                {
                    int intProductId;
                    int.TryParse(cmpsrProductSubs[2].ToString(), out intProductId);

                    //20150617, liliana, LIBST13020, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSubs.Text1.Trim(), intProductId, false);
                    string ClientCodeSubsAdd;
                    ClientCodeSubsAdd = "";

                    //20160829, liliana, LOGEN00196, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSubs.Text1.Trim(), intProductId, false, out ClientCodeSubsAdd);
                    IsSubsNew = CheckIsSubsNew(cmpsrCIFSubs.Text1.Trim(), intProductId, false, out ClientCodeSubsAdd
                        , cmbTASubs.SelectedIndex
                        );
                    //20160829, liliana, LOGEN00196, end
                    //20150617, liliana, LIBST13020, end

                    if (IsSubsNew)
                    {
                        cmpsrClientSubs.Enabled = false;
                        cmpsrClientSubs.Text1 = "";
                    }
                    else
                    {
                        //20150617, liliana, LIBST13020, begin
                        //cmpsrClientSubs.Enabled = true;
                        cmpsrClientSubs.Enabled = false;
                        cmpsrClientSubs.Text1 = ClientCodeSubsAdd;
                        cmpsrClientSubs.ValidateField();
                        //20150617, liliana, LIBST13020, end
                    }
                }
            }
            catch
            {
                cmpsrCurrSubs.Text1 = "";
            }
        }

        private void cmpsrProductBooking_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductBooking.Text2 = "";
        }

        private void cmpsrProductBooking_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                cmpsrCurrBooking.Text1 = cmpsrProductBooking[3].ToString();
                cmpsrCurrBooking.ValidateField();
            }
            catch
            {
                cmpsrCurrBooking.Text1 = "";
            }

        }

        private void cmpsrClientSubs_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrClientSubs.Text2 = "";
        }

        private void nispMoneyNomSubs_Leave(object sender, EventArgs e)
        {
            try
            {
                //20150505, liliana, LIBST13020, begin
                if (cmpsrProductSubs.Text1 != "")
                {
                    //20150505, liliana, LIBST13020, end
                    decimal decNominalFee, decPctFee;
                    int intProdId, intClientid, intTranType;
                    //20150828, liliana, LIBST13020, begin
                    // int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);
                    string strPrdId = GetImportantData("PRODUKID", cmpsrProductSubs.Text1);
                    int.TryParse(strPrdId, out intProdId);
                    //20150828, liliana, LIBST13020, end

                    //20150505, liliana, LIBST13020, begin
                    if (_ComboJenisSubs.Text == "By %")
                    {
                        ByPercent = true;
                    }
                    else
                    {
                        ByPercent = false;
                    }
                    //20150505, liliana, LIBST13020, end
                    if (IsSubsNew)
                    {
                        intTranType = 1;
                        intClientid = 0;
                    }
                    else
                    {
                        intTranType = 2;
                        //20150618, liliana, LIBST13020, begin
                        //20150828, liliana, LIBST13020, begin
                        //cmpsrClientSubs.ValidateField();
                        ////20150618, liliana, LIBST13020, end
                        //int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
                        string strClientId = GetImportantData("CLIENTID", cmpsrClientSubs.Text1);
                        int.TryParse(strClientId, out intClientid);
                        //20150828, liliana, LIBST13020, end
                    }

                    HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
                        checkFeeEditSubs.Checked, nispPercentageFeeSubs.Value, 1, out strFeeCurr,
                        out decNominalFee, out decPctFee, cmpsrCIFSubs.Text1);

                    //20150505, liliana, LIBST13020, begin
                    //nispMoneyFeeSubs.Value = decNominalFee;
                    //nispPercentageFeeSubs.Value = decPctFee;
                    //labelFeeCurrencySubs.Text = strFeeCurr;
                    nispMoneyFeeSubs.Value = decNominalFee;
                    nispPercentageFeeSubs.Value = decPctFee;
                    labelFeeCurrencySubs.Text = "%";
                    _KeteranganFeeSubs.Text = strFeeCurr;
                }
                //20150505, liliana, LIBST13020, end
            }
            catch
            {
                return;
            }
        }

        private void HitungFee(int ProdId, int ClientId, int TranType, decimal TranAmt, decimal TranUnit,
                bool FullAmount, bool IsFeeEdit, decimal PercentageFeeInput,
                int Jenis, out string FeeCurr, out decimal NominalFee, out decimal PctFee, string strCIFNo)
        {
            DataSet ds;
            double newFee = 0;
            double newPercentFee = 0;
            FeeCurr = "";
            NominalFee = 0;
            PctFee = 0;

            OleDbParameter[] odp = new OleDbParameter[32];

            try
            {
                (odp[0] = new OleDbParameter("@pnProdId", OleDbType.Integer)).Value = ProdId;
                (odp[1] = new OleDbParameter("@pnClientId", OleDbType.Integer)).Value = ClientId;
                (odp[2] = new OleDbParameter("@pnTranType", OleDbType.Integer)).Value = TranType;
                (odp[3] = new OleDbParameter("@pmTranAmt", OleDbType.Double)).Value = TranAmt;
                (odp[4] = new OleDbParameter("@pmUnit", OleDbType.Double)).Value = TranUnit;
                (odp[5] = new OleDbParameter("@pcFeeCCY", OleDbType.Char, 3)).Value = "";
                (odp[6] = new OleDbParameter("@pnFee", OleDbType.Double)).Value = 0;
                (odp[7] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                (odp[8] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
                (odp[9] = new OleDbParameter("@pmNAV", OleDbType.Decimal)).Value = 0;
                (odp[10] = new OleDbParameter("@pbFullAmount", OleDbType.Boolean)).Value = FullAmount;
                (odp[11] = new OleDbParameter("@pbIsByPercent", OleDbType.Boolean)).Value = ByPercent;
                (odp[12] = new OleDbParameter("@pbIsFeeEdit", OleDbType.Boolean)).Value = IsFeeEdit;
                (odp[13] = new OleDbParameter("@pdPercentageFeeInput", OleDbType.Double)).Value = PercentageFeeInput;
                (odp[14] = new OleDbParameter("@pdPercentageFeeOutput", OleDbType.Double)).Value = 0;
                odp[15] = new OleDbParameter("@pbProcess", false);
                (odp[16] = new OleDbParameter("@pmFeeBased", OleDbType.Double)).Value = 0;
                (odp[17] = new OleDbParameter("@pmRedempUnit", OleDbType.Double)).Value = 0;
                (odp[18] = new OleDbParameter("@pmRedempDev", OleDbType.Double)).Value = 0;
                odp[19] = new OleDbParameter("@pbByUnit", false);
                odp[20] = new OleDbParameter("@pbDebug", false);
                (odp[21] = new OleDbParameter("@pmProcessTranId", OleDbType.Integer)).Value = 0;
                odp[22] = new OleDbParameter("@pmErrMsg", OleDbType.VarChar, 100);
                (odp[23] = new OleDbParameter("@pnOutType", OleDbType.TinyInt)).Value = 0;
                odp[24] = new OleDbParameter("@pdValueDate", OleDbType.DBTimeStamp);
                (odp[25] = new OleDbParameter("@pmTaxFeeBased", OleDbType.Double)).Value = 0;
                (odp[26] = new OleDbParameter("@pmFeeBased3", OleDbType.Double)).Value = 0;
                (odp[27] = new OleDbParameter("@pmFeeBased4", OleDbType.Double)).Value = 0;
                (odp[28] = new OleDbParameter("@pmFeeBased5", OleDbType.Double)).Value = 0;
                (odp[29] = new OleDbParameter("@pnPeriod", OleDbType.Integer)).Value = 0;
                (odp[30] = new OleDbParameter("@pnIsRDB", OleDbType.Integer)).Value = 0;
                (odp[31] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 20)).Value = strCIFNo;

                for (int i = 16; i < 31; i++)
                {
                    if (!("19|20|21|23|24").Contains(i.ToString()))
                        odp[i].Direction = ParameterDirection.Output;
                }

                odp[5].Direction = ParameterDirection.Output;
                odp[6].Direction = ParameterDirection.Output;
                odp[14].Direction = ParameterDirection.Output;


                bool blnResult = ClQ.ExecProc("dbo.ReksaCalcFee", ref odp, out ds);

                if (blnResult)
                {
                    FeeCurr = odp[5].Value.ToString();
                    intPeriod = int.Parse(odp[29].Value.ToString());

                    if (Jenis == 1) //hitung fee tanpa edit fee
                    {
                        Fee = Double.Parse(odp[6].Value.ToString());
                        Fee = System.Math.Round(Fee, 2);
                        NominalFee = (decimal)Fee;

                        PercentFee = Double.Parse(odp[14].Value.ToString());
                        PctFee = (decimal)PercentFee;

                        if (ByPercent == true)
                        {
                            NominalFee = (decimal)PercentFee;
                            PctFee = (decimal)Fee;
                        }
                        else if (ByPercent == false)
                        {
                            NominalFee = (decimal)Fee;
                            PctFee = (decimal)PercentFee;
                        }
                    }
                    else if (Jenis == 2) //hitung fee dengan edit fee 
                    {
                        if (ByPercent == true)
                        {
                            newFee = Double.Parse(odp[6].Value.ToString());
                            newFee = System.Math.Round(newFee, 2);
                            PctFee = (decimal)newFee;
                        }
                        else if (ByPercent == false)
                        {
                            newPercentFee = Double.Parse(odp[14].Value.ToString());
                            PctFee = (decimal)newPercentFee;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private void _ComboJenisSubs_SelectedIndexChanged(object sender, EventArgs e)
        {
            nispMoneyFeeSubs.Value = 0;
            nispPercentageFeeSubs.Value = 0;

            if (_ComboJenisSubs.Text == "By %")
            {
                _KeteranganFeeSubs.Text = cmpsrCurrSubs.Text1;
                labelFeeCurrencySubs.Text = "%";
                ByPercent = true;

                //20220802, Lita, RDN-825, begin
                //nispMoneyFeeSubs.DecimalPlace = 3;
                nispMoneyFeeSubs.DecimalPlace = 4;
                //20220802, Lita, RDN-825, end
                //20150622, liliana, LIBST13020, begin
                nispPercentageFeeSubs.DecimalPlace = 2;
                //20150622, liliana, LIBST13020, end
            }
            else
            {
                _KeteranganFeeSubs.Text = "%";
                labelFeeCurrencySubs.Text = cmpsrCurrSubs.Text1;
                ByPercent = false;
                nispMoneyFeeSubs.DecimalPlace = 2;
                //20150622, liliana, LIBST13020, begin
                //20220802, Lita, RDN-825, begin
                //nispPercentageFeeSubs.DecimalPlace = 3;
                nispPercentageFeeSubs.DecimalPlace = 4;
                //20220802, Lita, RDN-825, end
                //20150622, liliana, LIBST13020, end
            }
        }

        //20150827, liliana, LIBST13020, begin 
        //private void GenerateTranCodeAndClientCode(string JenisTrx, bool IsSubsNew, string ProductCode,
        private bool GenerateTranCodeAndClientCode(string JenisTrx, bool IsSubsNew, string ProductCode,
            //20150827, liliana, LIBST13020, end
            //20150505, liliana, LIBST13020, begin
            //string ClientCode, out string TranCode, out string NewClientCode, string CIFNo)
            string ClientCode, out string TranCode, out string NewClientCode, string CIFNo,
            bool IsFeeEdit, decimal PercentageFee, int Period
            //20150610, liliana, LIBST13020, begin
            , bool FullAmount, double Fee, double TranAmt, double TranUnit, bool IsRedempAll, int FrekuensiPendebetan, int JangkaWaktu
            //20150610, liliana, LIBST13020, end
            //20150819, liliana, LIBST13020, begin
            , int intTypeTrx
            //20150819, liliana, LIBST13020, end
            //20150825, liliana, LIBST13020, begin
            , out string strWarnMsg
            , out string strWarnMsg2
            //20150825, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin
            , int TrxTA
            //20160829, liliana, LOGEN00196, end
            //20220520, Lita, RDN-781, begin
            , string sRDBDebetMethod, string sRDBIns, DateTime dRDBDebetDate
            //20220520, Lita, RDN-781, end
            )
        //20150505, liliana, LIBST13020, end
        {
            TranCode = "";
            NewClientCode = "";
            //20150825, liliana, LIBST13020, begin
            strWarnMsg = "";
            strWarnMsg2 = "";
            //20150825, liliana, LIBST13020, end

            //20150505, liliana, LIBST13020, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[7];
            //20150610, liliana, LIBST13020, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[10];
            //20150819, liliana, LIBST13020, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[17];
            //20150825, liliana, LIBST13020, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[18];
            //20160829, liliana, LOGEN00196, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[20];
            //20220520, Lita, RDN-781, begin
            //System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[21];
            System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[24];
            //20220520, Lita, RDN-781, end
            //20160829, liliana, LOGEN00196, end
            //20150825, liliana, LIBST13020, end
            //20150819, liliana, LIBST13020, end
            //20150610, liliana, LIBST13020, end
            //20150505, liliana, LIBST13020, end

            param[0] = new System.Data.OleDb.OleDbParameter("@pcJenisTrx", System.Data.OleDb.OleDbType.VarChar, 20);
            param[0].Value = JenisTrx;
            param[0].Direction = System.Data.ParameterDirection.Input;

            param[1] = new System.Data.OleDb.OleDbParameter("@pbIsSubsNew", System.Data.OleDb.OleDbType.Boolean);
            param[1].Value = IsSubsNew;
            param[1].Direction = System.Data.ParameterDirection.Input;

            param[2] = new System.Data.OleDb.OleDbParameter("@pcProdCode", System.Data.OleDb.OleDbType.VarChar, 10);
            param[2].Value = ProductCode;
            param[2].Direction = System.Data.ParameterDirection.Input;

            param[3] = new System.Data.OleDb.OleDbParameter("@pcClientCode", System.Data.OleDb.OleDbType.VarChar, 20);
            param[3].Value = ClientCode;
            param[3].Direction = System.Data.ParameterDirection.Input;

            param[4] = new System.Data.OleDb.OleDbParameter("@pcTranCode", System.Data.OleDb.OleDbType.VarChar, 20);
            param[4].Value = TranCode;
            param[4].Direction = System.Data.ParameterDirection.InputOutput;

            param[5] = new System.Data.OleDb.OleDbParameter("@pcNewClientCode", System.Data.OleDb.OleDbType.VarChar, 20);
            param[5].Value = NewClientCode;
            param[5].Direction = System.Data.ParameterDirection.InputOutput;

            param[6] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
            param[6].Value = CIFNo;
            param[6].Direction = System.Data.ParameterDirection.Input;
            //20150505, liliana, LIBST13020, begin

            param[7] = new System.Data.OleDb.OleDbParameter("@pbIsFeeEdit", System.Data.OleDb.OleDbType.Boolean);
            param[7].Value = IsFeeEdit;
            param[7].Direction = System.Data.ParameterDirection.Input;

            param[8] = new System.Data.OleDb.OleDbParameter("@pdPercentageFee", System.Data.OleDb.OleDbType.Decimal);
            param[8].Value = PercentageFee;
            param[8].Direction = System.Data.ParameterDirection.Input;

            param[9] = new System.Data.OleDb.OleDbParameter("@pnPeriod", System.Data.OleDb.OleDbType.Integer);
            param[9].Value = Period;
            param[9].Direction = System.Data.ParameterDirection.Input;
            //20150505, liliana, LIBST13020, end
            //20150610, liliana, LIBST13020, begin
            param[10] = new System.Data.OleDb.OleDbParameter("@pbFullAmount", System.Data.OleDb.OleDbType.Boolean);
            param[10].Value = FullAmount;
            param[10].Direction = System.Data.ParameterDirection.Input;

            param[11] = new System.Data.OleDb.OleDbParameter("@pmFee", System.Data.OleDb.OleDbType.Double);
            param[11].Value = Fee;
            param[11].Direction = System.Data.ParameterDirection.Input;

            param[12] = new System.Data.OleDb.OleDbParameter("@pmTranAmt", System.Data.OleDb.OleDbType.Double);
            param[12].Value = TranAmt;
            param[12].Direction = System.Data.ParameterDirection.Input;

            param[13] = new System.Data.OleDb.OleDbParameter("@pmTranUnit", System.Data.OleDb.OleDbType.Double);
            param[13].Value = TranUnit;
            param[13].Direction = System.Data.ParameterDirection.Input;

            param[14] = new System.Data.OleDb.OleDbParameter("@pbIsRedempAll", System.Data.OleDb.OleDbType.Boolean);
            param[14].Value = IsRedempAll;
            param[14].Direction = System.Data.ParameterDirection.Input;

            param[15] = new System.Data.OleDb.OleDbParameter("@pnFrekuensiPendebetan", System.Data.OleDb.OleDbType.Integer);
            param[15].Value = FrekuensiPendebetan;
            param[15].Direction = System.Data.ParameterDirection.Input;

            param[16] = new System.Data.OleDb.OleDbParameter("@pnJangkaWaktu", System.Data.OleDb.OleDbType.Integer);
            param[16].Value = JangkaWaktu;
            param[16].Direction = System.Data.ParameterDirection.Input;
            //20150610, liliana, LIBST13020, end
            //20150819, liliana, LIBST13020, begin

            param[17] = new System.Data.OleDb.OleDbParameter("@pcType", System.Data.OleDb.OleDbType.Integer);
            param[17].Value = intTypeTrx;
            param[17].Direction = System.Data.ParameterDirection.Input;
            //20150819, liliana, LIBST13020, end
            //20150825, liliana, LIBST13020, begin

            (param[18] = new OleDbParameter("@pcWarnMsg", OleDbType.VarChar, 8000)).Value = "";
            (param[19] = new OleDbParameter("@pcWarnMsg2", OleDbType.VarChar, 8000)).Value = "";

            param[18].Direction = ParameterDirection.Output;
            param[19].Direction = ParameterDirection.Output;
            //20150825, liliana, LIBST13020, end
            //20160829, liliana, LOGEN00196, begin

            param[20] = new System.Data.OleDb.OleDbParameter("@piTrxTA", System.Data.OleDb.OleDbType.Integer);
            param[20].Value = TrxTA;
            param[20].Direction = System.Data.ParameterDirection.Input;
            //20160829, liliana, LOGEN00196, end

            //20220520, Lita, RDN-781, begin
            param[21] = new System.Data.OleDb.OleDbParameter("@pcRDBDebetMethod", System.Data.OleDb.OleDbType.VarChar);
            param[21].Value = sRDBDebetMethod;
            param[21].Direction = System.Data.ParameterDirection.Input;

            param[22] = new System.Data.OleDb.OleDbParameter("@pcRDBIns", System.Data.OleDb.OleDbType.VarChar);
            param[22].Value = sRDBIns;
            param[22].Direction = System.Data.ParameterDirection.Input;

            param[23] = new System.Data.OleDb.OleDbParameter("@pdTglDebetRDB", System.Data.OleDb.OleDbType.Date);
            param[23].Value = dRDBDebetDate;
            param[23].Direction = System.Data.ParameterDirection.Input;

            //20220520, liliana, RDN-781, end

            bool blnResult = ClQ.ExecProc("ReksaGenerateTranCodeClientCode", ref param);

            if (blnResult == true)
            {
                TranCode = param[4].Value.ToString();
                NewClientCode = param[5].Value.ToString();
                //20150825, liliana, LIBST13020, begin
                strWarnMsg = param[18].Value.ToString();
                strWarnMsg2 = param[19].Value.ToString();
                //20150825, liliana, LIBST13020, end
            }
            //20150827, liliana, LIBST13020, begin
            return blnResult;
            //20150827, liliana, LIBST13020, end
        }

        private void buttonAddSubs_Click(object sender, EventArgs e)
        {
            //20150505, liliana, LIBST13020, begin
            if (buttonAddSubs.Text == "&Done")
            {
                DisableFormTrxSubs(true);
                ResetFormTrxSubs();
                buttonAddSubs.Text = "&Done";
                buttonEditSubs.Enabled = false;
            }
            else if (buttonAddSubs.Text == "&Add")
            {
                if (dataGridViewSubs.Rows.Count >= 3)
                {
                    MessageBox.Show("Maksimal hanya dapat menambah 3 transaksi !", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20150505, liliana, LIBST13020, end
                if (cmpsrProductSubs.Text1 == "")
                {
                    MessageBox.Show("Kode Produk harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (cmpsrCurrSubs.Text1 == "")
                {
                    MessageBox.Show("Mata Uang Produk harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if ((IsSubsNew == false) && (cmpsrClientSubs.Text1 == ""))
                {
                    MessageBox.Show("Client Code harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (nispMoneyNomSubs.Value == 0)
                {
                    MessageBox.Show("Nominal harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20150505, liliana, LIBST13020, begin
                //20160829, liliana, LOGEN00196, begin
                if (cmbTASubs.SelectedIndex == -1)
                {
                    MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, end

                //20150728, liliana, LIBST13020, begin
                //if ((maskedRekeningSubsUSD.Text == "") && (cmpsrCurrSubs.Text1 == "USD"))
                if ((maskedRekeningSubsUSD.Text == "") && (maskedRekeningSubsMC.Text == "") && (cmpsrCurrSubs.Text1 == "USD"))
                //20150728, liliana, LIBST13020, end
                {
                    //20150728, liliana, LIBST13020, begin
                    //MessageBox.Show("Rekening USD tidak boleh kosong untuk transaksi currency USD!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MessageBox.Show("Rekening USD / Multicurrency tidak boleh kosong untuk transaksi currency USD!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //20150728, liliana, LIBST13020, end
                    return;
                }

                //20150728, liliana, LIBST13020, begin
                //if ((maskedRekeningSubs.Text == "") && (cmpsrCurrSubs.Text1 == "IDR"))
                if ((maskedRekeningSubs.Text == "") && (maskedRekeningSubsMC.Text == "") && (cmpsrCurrSubs.Text1 == "IDR"))
                //20150728, liliana, LIBST13020, end
                {
                    //20150728, liliana, LIBST13020, begin
                    //MessageBox.Show("Rekening IDR tidak boleh kosong untuk transaksi currency IDR!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MessageBox.Show("Rekening IDR / Multicurrency tidak boleh kosong untuk transaksi currency IDR!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //20150728, liliana, LIBST13020, end
                    return;
                }
                //20150505, liliana, LIBST13020, end

                //20160829, liliana, LOGEN00196, begin
                //string strData = String.Format("KodeProduk='{0}'",
                //       cmpsrProductSubs.Text1);
                string strData = String.Format("KodeProduk='{0}' and TrxTaxAmnesty={1}",
                       cmpsrProductSubs.Text1, cmbTASubs.SelectedIndex);
                //20160829, liliana, LOGEN00196, end

                if (cTransaksi.dttSubscription.Select(strData).Length != 0)
                {
                    MessageBox.Show("Subscription ke produk " + cmpsrProductSubs.Text1 + " sudah ada!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20240220, gio, RDN-1108, begin
                if (dateTglTransaksiSubs.Value > DateTime.Today.AddHours(23))
                {
                    MessageBox.Show("Tanggal instruksi tidak lebih dari tanggal hari ini", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (dateTglTransaksiSubs.Value < DateTime.Today)
                {
                    MessageBox.Show("Tanggal instruksi tidak kurang dari tanggal hari ini", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (dateTglTransaksiSubs.Value == DateTime.Today)
                {
                    MessageBox.Show("Waktu instruksi tidak dapat diisi 00.00.00. Mohon untuk menginput waktu instruksi transaksi sesuai informasi Nasabah", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                                
                if (dateTglTransaksiSubs.Value > DateTime.Today.AddHours(13))
                {
                    MessageBox.Show("Waktu instruksi diatas 13.00.00 menggunakan NAV berikutnya", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //20240220, gio, RDN-1108, end

                try
                {
                    string strTranCode, strNewClientCode;
                    //20150505, liliana, LIBST13020, begin
                    decimal _PercentageFee;
                    //20150610, liliana, LIBST13020, begin
                    double _NominalFee;
                    //20150610, liliana, LIBST13020, end
                    //20150728, liliana, LIBST13020, begin
                    int intProductId;
                    int.TryParse(cmpsrProductSubs[2].ToString(), out intProductId);

                    string ClientCodeSubsAdd;
                    ClientCodeSubsAdd = "";
                    //20150825, liliana, LIBST13020, begin
                    string strWarnMsg = "";
                    string strWarnMsg2 = "";
                    //20150825, liliana, LIBST13020, end

                    //20160829, liliana, LOGEN00196, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSubs.Text1.Trim(), intProductId, false, out ClientCodeSubsAdd);
                    IsSubsNew = CheckIsSubsNew(cmpsrCIFSubs.Text1.Trim(), intProductId, false, out ClientCodeSubsAdd
                        , cmbTASubs.SelectedIndex
                        );
                    //20160829, liliana, LOGEN00196, end
                    //20150728, liliana, LIBST13020, end

                    if (_ComboJenisSubs.Text == "By %")
                    {
                        _PercentageFee = nispMoneyFeeSubs.Value;
                        //20150610, liliana, LIBST13020, begin
                        _NominalFee = (double)nispPercentageFeeSubs.Value;
                        //20150610, liliana, LIBST13020, end
                    }
                    else
                    {
                        _PercentageFee = nispPercentageFeeSubs.Value;
                        //20150610, liliana, LIBST13020, begin
                        _NominalFee = (double)nispMoneyFeeSubs.Value;
                        //20150610, liliana, LIBST13020, end
                    }
                    //20150505, liliana, LIBST13020, end

                    GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductSubs.Text1,
                        //20150505, liliana, LIBST13020, begin
                        //cmpsrClientSubs.Text1, out strTranCode, out strNewClientCode, cmpsrCIFSubs.Text1);
                        cmpsrClientSubs.Text1, out strTranCode, out strNewClientCode, cmpsrCIFSubs.Text1,
                        checkFeeEditSubs.Checked, _PercentageFee, 0
                        //20150610, liliana, LIBST13020, begin
                        , checkFullAmtSubs.Checked, _NominalFee, (double)nispMoneyNomSubs.Value, 0, false, 0, 0
                        //20150610, liliana, LIBST13020, end
                        //20150819, liliana, LIBST13020, begin
                        , _intType
                        //20150819, liliana, LIBST13020, end
                        //20150825, liliana, LIBST13020, begin
                        , out strWarnMsg
                        , out strWarnMsg2
                        //20150825, liliana, LIBST13020, end
                        //20160829, liliana, LOGEN00196, begin
                        , cmbTASubs.SelectedIndex
                        //20160829, liliana, LOGEN00196, end
                        //20220520, Lita, RDN-781, begin
                        , "", "", DateTime.Today
                        //20220520, Lita, RDN-781, end
                        );
                    //20150505, liliana, LIBST13020, end
                    //20150825, liliana, LIBST13020, begin
                    if (strWarnMsg != "")
                    {
                        //20170825, liliana, COPOD17271, begin
                        if (MessageBox.Show("Produk yang dipilih diatas ketentuan profile nasabah. Lanjutkan transaksi?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else
                        {
                            //20170825, liliana, COPOD17271, end
                            MessageBox.Show("Profil Risiko produk lebih tinggi dari Profil Risiko Nasabah . PASTIKAN Nasabah sudah menandatangani kolom Profil Risiko pada Subscription/Switching Form", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            //20170825, liliana, COPOD17271, begin
                        }
                        //20170825, liliana, COPOD17271, end
                    }

                    if (strWarnMsg2 != "")
                    {
                        if (MessageBox.Show(strWarnMsg2, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    //20150825, liliana, LIBST13020, end

                    if (strTranCode != "")
                    {
                        DataRow dtrSubscription = cTransaksi.dttSubscription.NewRow();
                        //20150818, liliana, LIBST13020, begin
                        //dtrSubscription["NoTrx"] = strTranCode;
                        dtrSubscription["NoTrx"] = "";
                        //20150818, liliana, LIBST13020, end
                        dtrSubscription["TglTrx"] = dateTglTransaksiSubs.Value;
                        dtrSubscription["KodeProduk"] = cmpsrProductSubs.Text1;
                        //20150617, liliana, LIBST13020, begin
                        dtrSubscription["NamaProduk"] = cmpsrProductSubs.Text2;
                        //20150617, liliana, LIBST13020, end
                        dtrSubscription["ClientCode"] = strNewClientCode;
                        dtrSubscription["CCY"] = cmpsrCurrSubs.Text1;
                        dtrSubscription["Nominal"] = nispMoneyNomSubs.Value;
                        dtrSubscription["PhoneOrder"] = checkPhoneOrderSubs.Checked;
                        dtrSubscription["FullAmount"] = checkFullAmtSubs.Checked;
                        dtrSubscription["EditFee"] = checkFeeEditSubs.Checked;
                        dtrSubscription["JenisFee"] = _ComboJenisSubs.SelectedIndex;
                        dtrSubscription["IsNew"] = IsSubsNew;
                        dtrSubscription["ApaDiUpdate"] = false;
                        //20160829, liliana, LOGEN00196, begin
                        dtrSubscription["TrxTaxAmnesty"] = cmbTASubs.SelectedIndex;
                        //20160829, liliana, LOGEN00196, end

                        //20150430, liliana, LIBST13020, begin
                        if (checkFeeEditSubs.Checked)
                        {
                            dtrSubscription["EditFeeBy"] = _ComboJenisSubs.Text;
                        }
                        else
                        {
                            dtrSubscription["EditFeeBy"] = "";
                        }
                        //20150430, liliana, LIBST13020, end

                        if (IsSubsNew)
                        {
                            dtrSubscription["OutstandingUnit"] = 0;
                        }
                        else
                        {
                            int intClientId = int.Parse(cmpsrClientSubs[2].ToString());
                            decimal decUnitBalance = GetLatestBalance(intClientId);
                            dtrSubscription["OutstandingUnit"] = decUnitBalance;
                        }


                        if (_ComboJenisSubs.Text == "By %")
                        {
                            dtrSubscription["FeeKet"] = labelFeeCurrencySubs.Text;
                            dtrSubscription["FeeCurr"] = _KeteranganFeeSubs.Text;

                            dtrSubscription["NominalFee"] = nispPercentageFeeSubs.Value;
                            dtrSubscription["PctFee"] = nispMoneyFeeSubs.Value;
                        }
                        else
                        {
                            dtrSubscription["FeeKet"] = _KeteranganFeeSubs.Text;
                            dtrSubscription["FeeCurr"] = labelFeeCurrencySubs.Text;

                            dtrSubscription["NominalFee"] = nispMoneyFeeSubs.Value;
                            dtrSubscription["PctFee"] = nispPercentageFeeSubs.Value;
                        }

                        cTransaksi.dttSubscription.Rows.Add(dtrSubscription);
                        cTransaksi.dttSubscription.AcceptChanges();

                        dataGridViewSubs.DataSource = cTransaksi.dttSubscription;

                        for (int i = 0; i < dataGridViewSubs.Columns.Count; i++)
                        {
                            if (dataGridViewSubs.Columns[i].ValueType.ToString() == "System.Decimal")
                            {
                                //20220802, Lita, RDN-825, begin
                                //dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N2";
                                if (dataGridViewSubs.Columns[i].Name == "PctFee")
                                    dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N4";
                                else
                                    dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N2";
                                //20220802, Lita, RDN-825, end
                            }
                        }

                        dataGridViewSubs.AutoResizeColumns();
                        subSetVisibleGrid(_strTabName);
                        ResetFormTrxSubs();
                        //20150505, liliana, LIBST13020, begin
                        DisableFormTrxSubs(true);
                        //20150820, liliana, LIBST13020, begin
                        //buttonEditSubs.Enabled = true;
                        buttonEditSubs.Enabled = false;
                        //20150820, liliana, LIBST13020, end
                        buttonAddSubs.Enabled = true;
                        //20150505, liliana, LIBST13020, end

                        //20240240220, gio, RDN-1108, begin
                        dateTglTransaksiSubs.Enabled = false;
                        //20240240220, gio, RDN-1108, end

                        //20150615, liliana, LIBST13020, begin
                        if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSubs.Text1))
                        {
                            checkPhoneOrderSubs.Enabled = false;
                            checkPhoneOrderSubs.Checked = false;
                        }
                        else
                        {
                            checkPhoneOrderSubs.Enabled = true;
                        }
                        //20150615, liliana, LIBST13020, end
                        //20220119, sandi, RDN-727, begin
                        cmpsrNoRekSubs.Enabled = false;
                        //20220119, sandi, RDN-727, end
                    }
                    else
                    {
                        MessageBox.Show("Gagal generate kode transaksi!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                //20150505, liliana, LIBST13020, begin
            }
            //20150505, liliana, LIBST13020, end
        }

        private void buttonEditSubs_Click(object sender, EventArgs e)
        {
            //20150505, liliana, LIBST13020, begin
            if (buttonEditSubs.Text == "&Edit")
            {
                DisableFormTrxSubs(true);

                cmpsrProductSubs.Enabled = false;
                cmpsrClientSubs.Enabled = false;

                if (checkFeeEditSubs.Checked)
                {
                    _ComboJenisSubs.Enabled = true;
                    nispMoneyFeeSubs.Enabled = true;
                    nispPercentageFeeSubs.Enabled = false;
                }

                if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFSubs.Text1))
                {
                    checkPhoneOrderSubs.Enabled = false;
                    checkPhoneOrderSubs.Checked = false;
                }
                else
                {
                    checkPhoneOrderSubs.Enabled = true;
                }

                buttonEditSubs.Text = "&Done";
                buttonAddSubs.Enabled = false;
            }
            else if (buttonEditSubs.Text == "&Done")
            {
                //20150505, liliana, LIBST13020, end
                if (MessageBox.Show("Apakah akan merubah transaksi Trancode " + textNoTransaksiSubs.Text + "?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
                else
                {
                    //20150629, liliana, LIBST13020, begin
                    if (_StatusTransaksiSubs == "Rejected")
                    {
                        MessageBox.Show("Transaksi dengan Status Rejected tidak dapat diedit!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150629, liliana, LIBST13020, end
                    //20150916, liliana, LIBST13020, begin
                    else if (_StatusTransaksiSubs == "Reversed")
                    {
                        MessageBox.Show("Transaksi dengan Status Reversed tidak dapat diedit!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else if (_StatusTransaksiSubs == "Cancel By PO")
                    {
                        MessageBox.Show("Transaksi dengan Status Cancel By PO tidak dapat diedit!", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150916, liliana, LIBST13020, end
                    //20150415, liliana, LIBST13020, begin
                    if (cmpsrProductSubs.Text1 == "")
                    {
                        MessageBox.Show("Kode Produk harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (cmpsrCurrSubs.Text1 == "")
                    {
                        MessageBox.Show("Mata Uang Produk harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if ((IsSubsNew == false) && (cmpsrClientSubs.Text1 == ""))
                    {
                        MessageBox.Show("Client Code harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (nispMoneyNomSubs.Value == 0)
                    {
                        MessageBox.Show("Nominal harus diisi", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150415, liliana, LIBST13020, end
                    string strData = String.Format("NoTrx='{0}'", textNoTransaksiSubs.Text);

                    //20240220, gio, RDN-1108, begin
                    if (dateTglTransaksiSubs.Value > DateTime.Today.AddHours(23))
                    {
                        MessageBox.Show("Tanggal instruksi tidak lebih dari tanggal hari ini", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (dateTglTransaksiSubs.Value < DateTime.Today)
                    {
                        MessageBox.Show("Tanggal instruksi tidak kurang dari tanggal hari ini", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (dateTglTransaksiSubs.Value == DateTime.Today)
                    {
                        MessageBox.Show("Waktu instruksi tidak dapat diisi 00.00.00. Mohon untuk menginput waktu instruksi transaksi sesuai informasi Nasabah", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (dateTglTransaksiSubs.Value > DateTime.Today.AddHours(13))
                    {
                        MessageBox.Show("Waktu instruksi diatas 13.00.00 menggunakan NAV berikutnya", "Transaksi Subscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    //20240220, gio, RDN-1108, end

                    try
                    {
                        if (cTransaksi.dttSubscription.Select(strData).Length > 0)
                        {
                            //20150819, liliana, LIBST13020, begin
                            string strTranCode, strNewClientCode;
                            decimal _PercentageFee;
                            double _NominalFee;
                            int intProductId;
                            int.TryParse(cmpsrProductSubs[2].ToString(), out intProductId);

                            string ClientCodeSubsAdd;
                            ClientCodeSubsAdd = "";
                            //20150825, liliana, LIBST13020, begin
                            string strWarnMsg = "";
                            string strWarnMsg2 = "";
                            //20150825, liliana, LIBST13020, end

                            if (_ComboJenisSubs.Text == "By %")
                            {
                                _PercentageFee = nispMoneyFeeSubs.Value;
                                _NominalFee = (double)nispPercentageFeeSubs.Value;
                            }
                            else
                            {
                                _PercentageFee = nispPercentageFeeSubs.Value;
                                _NominalFee = (double)nispMoneyFeeSubs.Value;
                            }

                            //20150827, liliana, LIBST13020, begin
                            //GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductSubs.Text1,
                            bool _result = false;

                            _result = GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductSubs.Text1,
                                //20150827, liliana, LIBST13020, end
                                cmpsrClientSubs.Text1, out strTranCode, out strNewClientCode, cmpsrCIFSubs.Text1,
                                checkFeeEditSubs.Checked, _PercentageFee, 0
                                , checkFullAmtSubs.Checked, _NominalFee, (double)nispMoneyNomSubs.Value, 0, false, 0, 0
                                , _intType
                                //20150825, liliana, LIBST13020, begin
                                , out strWarnMsg
                                , out strWarnMsg2
                                //20150825, liliana, LIBST13020, end
                                //20160829, liliana, LOGEN00196, begin
                                , cmbTASubs.SelectedIndex
                                //20160829, liliana, LOGEN00196, end
                                //20220520, Lita, RDN-781, begin
                                , "", "", DateTime.Today
                                //20220520, Lita, RDN-781, end
                                );
                            //20150827, liliana, LIBST13020, begin

                            if (!_result)
                            {
                                return;
                            }
                            //20150827, liliana, LIBST13020, end

                            //20150825, liliana, LIBST13020, begin
                            if (strWarnMsg2 != "")
                            {
                                if (MessageBox.Show(strWarnMsg2, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                {
                                    MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }
                            //20150825, liliana, LIBST13020, end


                            //20150819, liliana, LIBST13020, end
                            DataRow[] dtrSubscription = cTransaksi.dttSubscription.Select(strData);

                            dtrSubscription[0]["KodeProduk"] = cmpsrProductSubs.Text1;
                            //20150617, liliana, LIBST13020, begin
                            dtrSubscription[0]["NamaProduk"] = cmpsrProductSubs.Text2;
                            //20150617, liliana, LIBST13020, end
                            dtrSubscription[0]["ClientCode"] = cmpsrClientSubs.Text1;
                            dtrSubscription[0]["CCY"] = cmpsrCurrSubs.Text1;
                            dtrSubscription[0]["Nominal"] = nispMoneyNomSubs.Value;
                            dtrSubscription[0]["PhoneOrder"] = checkPhoneOrderSubs.Checked;
                            dtrSubscription[0]["FullAmount"] = checkFullAmtSubs.Checked;
                            dtrSubscription[0]["EditFee"] = checkFeeEditSubs.Checked;
                            dtrSubscription[0]["JenisFee"] = _ComboJenisSubs.SelectedIndex;
                            dtrSubscription[0]["IsNew"] = IsSubsNew;
                            dtrSubscription[0]["ApaDiUpdate"] = true;
                            //20160829, liliana, LOGEN00196, begin
                            dtrSubscription[0]["TrxTaxAmnesty"] = cmbTASubs.SelectedIndex;
                            //20160829, liliana, LOGEN00196, end

                            //20210922, korvi, RDN-674, begin
                            dtrSubscription[0]["SelectedAccNo"] = cmpsrNoRekSubs.Text1;
                            //20210922, korvi, RDN-674, end

                            if (IsSubsNew)
                            {
                                dtrSubscription[0]["OutstandingUnit"] = 0;
                            }
                            else
                            {
                                //20150505, liliana, LIBST13020, begin
                                cmpsrClientSubs.ValidateField();
                                //20150505, liliana, LIBST13020, end
                                int intClientId = int.Parse(cmpsrClientSubs[2].ToString());
                                decimal decUnitBalance = GetLatestBalance(intClientId);
                                dtrSubscription[0]["OutstandingUnit"] = decUnitBalance;
                            }

                            if (_ComboJenisSubs.Text == "By %")
                            {
                                dtrSubscription[0]["FeeKet"] = labelFeeCurrencySubs.Text;
                                dtrSubscription[0]["FeeCurr"] = _KeteranganFeeSubs.Text;

                                dtrSubscription[0]["NominalFee"] = nispPercentageFeeSubs.Value;
                                dtrSubscription[0]["PctFee"] = nispMoneyFeeSubs.Value;
                            }
                            else
                            {
                                dtrSubscription[0]["FeeKet"] = _KeteranganFeeSubs.Text;
                                dtrSubscription[0]["FeeCurr"] = labelFeeCurrencySubs.Text;

                                dtrSubscription[0]["NominalFee"] = nispMoneyFeeSubs.Value;
                                dtrSubscription[0]["PctFee"] = nispPercentageFeeSubs.Value;
                            }
                            //20150430, liliana, LIBST13020, begin
                            if (checkFeeEditSubs.Checked)
                            {
                                dtrSubscription[0]["EditFeeBy"] = _ComboJenisSubs.Text;
                            }
                            else
                            {
                                dtrSubscription[0]["EditFeeBy"] = "";
                            }
                            //20150430, liliana, LIBST13020, end
                            cTransaksi.dttSubscription.AcceptChanges();

                            dataGridViewSubs.DataSource = cTransaksi.dttSubscription;

                            for (int i = 0; i < dataGridViewSubs.Columns.Count; i++)
                            {
                                if (dataGridViewSubs.Columns[i].ValueType.ToString() == "System.Decimal")
                                {
                                    //20220802, Lita, RDN-825, begin
                                    //dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N2";
                                    if (dataGridViewSubs.Columns[i].Name == "PctFee")
                                        dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N4";
                                    else
                                        dataGridViewSubs.Columns[i].DefaultCellStyle.Format = "N2";
                                    //20220802, Lita, RDN-825, end
                                }
                            }

                            dataGridViewSubs.AutoResizeColumns();
                            subSetVisibleGrid(_strTabName);
                            ResetFormTrxSubs();
                            //20150505, liliana, LIBST13020, begin
                            //20150810, liliana, LIBSTT13020, begin
                            //DisableFormTrxSubs(false);
                            //buttonEditSubs.Enabled = true;
                            //buttonAddSubs.Enabled = true;
                            if (_intType == 1)
                            {
                                DisableFormTrxSubs(true);
                                buttonEditSubs.Enabled = true;
                                buttonAddSubs.Enabled = true;
                            }
                            else if (_intType == 2)
                            {
                                DisableFormTrxSubs(false);
                                buttonEditSubs.Enabled = true;
                                buttonAddSubs.Enabled = false;
                            }
                            //20150810, liliana, LIBSTT13020, end
                            //20150505, liliana, LIBST13020, end
                        }
                        else
                        {
                            MessageBox.Show("Data tidak ditemukan", "Transaksi Subscription ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Transaksi Subscription");
                        return;
                    }
                }
                //20150505, liliana, LIBST13020, begin
            }
            //20150505, liliana, LIBST13020, end
        }

        private void dataGridViewSubs_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (buttonEditSubs.Text == "&Done")
            {
                try
                {
                    int intComboBox;

                    //20160829, liliana, LOGEN00196, begin
                    bool bIsTA; int iIsTA;
                    iIsTA = -1;
                    bIsTA = (bool)dataGridViewSubs["TrxTaxAmnesty", e.RowIndex].Value;

                    if (bIsTA)
                    {
                        iIsTA = 1;
                    }
                    else
                    {
                        iIsTA = 0;
                    }

                    cmbTASubs.SelectedIndex = iIsTA;
                    //20160829, liliana, LOGEN00196, end
                    textNoTransaksiSubs.Text = dataGridViewSubs["NoTrx", e.RowIndex].Value.ToString();
                    //20150629, liliana, LIBST13020, begin
                    _StatusTransaksiSubs = dataGridViewSubs["StatusTransaksi", e.RowIndex].Value.ToString();
                    //20150629, liliana, LIBST13020, end
                    dateTglTransaksiSubs.Value = (DateTime)dataGridViewSubs["TglTrx", e.RowIndex].Value;

                    cmpsrProductSubs.Text1 = dataGridViewSubs["KodeProduk", e.RowIndex].Value.ToString();
                    cmpsrProductSubs.ValidateField();
                    cmpsrCurrSubs.Text1 = dataGridViewSubs["CCY", e.RowIndex].Value.ToString();

                    nispMoneyNomSubs.Value = (decimal)dataGridViewSubs["Nominal", e.RowIndex].Value;
                    checkPhoneOrderSubs.Checked = (bool)dataGridViewSubs["PhoneOrder", e.RowIndex].Value;
                    checkFullAmtSubs.Checked = (bool)dataGridViewSubs["FullAmount", e.RowIndex].Value;
                    checkFeeEditSubs.Checked = (bool)dataGridViewSubs["EditFee", e.RowIndex].Value;

                    int.TryParse(dataGridViewSubs["JenisFee", e.RowIndex].Value.ToString(), out intComboBox);
                    _ComboJenisSubs.SelectedIndex = intComboBox;

                    if (_ComboJenisSubs.Text == "By %")
                    {
                        nispMoneyFeeSubs.Value = (decimal)dataGridViewSubs["PctFee", e.RowIndex].Value;
                        nispPercentageFeeSubs.Value = (decimal)dataGridViewSubs["NominalFee", e.RowIndex].Value;
                        labelFeeCurrencySubs.Text = dataGridViewSubs["FeeKet", e.RowIndex].Value.ToString();
                        _KeteranganFeeSubs.Text = dataGridViewSubs["FeeCurr", e.RowIndex].Value.ToString();

                    }
                    else
                    {
                        nispMoneyFeeSubs.Value = (decimal)dataGridViewSubs["NominalFee", e.RowIndex].Value;
                        nispPercentageFeeSubs.Value = (decimal)dataGridViewSubs["PctFee", e.RowIndex].Value;
                        labelFeeCurrencySubs.Text = dataGridViewSubs["FeeCurr", e.RowIndex].Value.ToString();
                        _KeteranganFeeSubs.Text = dataGridViewSubs["FeeKet", e.RowIndex].Value.ToString();

                    }

                    cmpsrClientSubs.Text1 = dataGridViewSubs["ClientCode", e.RowIndex].Value.ToString();
                    //20150619, liliana, LIBST13020, begin
                    if (_intType == 2)
                    {
                        cmpsrClientSubs.ValidateField();
                    }
                    //20150619, liliana, LIBST13020, end

                    //20210922, korvi, RDN-674, begin
                    cmpsrNoRekSubs.Criteria = cmpsrCIFSubs.Text1 + "#" + cmbTASubs.SelectedIndex.ToString();
                    cmpsrNoRekSubs.Text1 = dataGridViewSubs["SelectedAccNo", e.RowIndex].Value.ToString();
                    cmpsrNoRekSubs.Text2 = dataGridViewSubs["TranCCY", e.RowIndex].Value.ToString();

                    //20220119, sandi, RDN-727, begin
                    //cmpsrNoRekSubs.ValidateField();
                    //20220119, sandi, RDN-727, end
                    //20210922, korvi, RDN-674, end                    
                }
                catch
                {
                    return;
                }
            }
        }

        private void checkFullAmtSubs_CheckedChanged(object sender, EventArgs e)
        {
            //20150824, liliana, LIBST13020, begin
            ////20150820, liliana, LIBST13020, begin
            //nispMoneyFeeSubs.Value = 0;
            //nispPercentageFeeSubs.Value = 0;
            //nispMoneyFeeSubs.Text = "";
            //nispPercentageFeeSubs.Text = "";
            //labelFeeCurrencySubs.Text = "";
            ////20150820, liliana, LIBST13020, end
            //if ((cmpsrProductSubs.Text1 != "") && (nispMoneyNomSubs.Value != 0))
            //{
            //    if (_ComboJenisSubs.Text == "By %")
            //    {
            //        _KeteranganFeeSubs.Text = cmpsrCurrSubs.Text1;
            //        labelFeeCurrencySubs.Text = "%";
            //        ByPercent = true;
            //    }
            //    else
            //    {
            //        _KeteranganFeeSubs.Text = "%";
            //        labelFeeCurrencySubs.Text = cmpsrCurrSubs.Text1;
            //        ByPercent = false;
            //    }

            //    int jenisfee;
            //    if (checkFeeEditSubs.Checked)
            //    {
            //        jenisfee = 2;
            //    }
            //    else
            //    {
            //        jenisfee = 1;
            //    }

            //    decimal decNominalFee, decPctFee;
            //    int intProdId, intClientid, intTranType;
            //    int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);



            //    if (IsSubsNew)
            //    {
            //        intTranType = 1;
            //        intClientid = 0;
            //    }
            //    else
            //    {
            //        intTranType = 2;
            //        //20150618, liliana, LIBST13020, begin
            //        cmpsrClientSubs.ValidateField();
            //        //20150618, liliana, LIBST13020, end
            //        int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
            //    }

            //    HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
            //        checkFeeEditSubs.Checked, nispPercentageFeeSubs.Value, jenisfee, out strFeeCurr,
            //        out decNominalFee, out decPctFee, cmpsrCIFSubs.Text1);

            //    nispMoneyFeeSubs.Value = decNominalFee;
            //    nispPercentageFeeSubs.Value = decPctFee;
            //    labelFeeCurrencySubs.Text = strFeeCurr;
            //}

            if (checkFeeEditSubs.Checked)
            {
                checkFeeEditSubs.Checked = false;
            }

            try
            {
                if (cmpsrProductSubs.Text1 != "")
                {
                    _ComboJenisSubs.Enabled = false;
                    nispMoneyFeeSubs.Enabled = false;
                    nispPercentageFeeSubs.Enabled = false;

                    _ComboJenisSubs.SelectedIndex = 1;
                    nispMoneyFeeSubs.Value = (decimal)Fee;

                    decimal decNominalFee, decPctFee;
                    int intProdId, intClientid, intTranType;
                    cmpsrProductSubs.ValidateField();
                    int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);

                    if (IsSubsNew)
                    {
                        intTranType = 1;
                        intClientid = 0;
                    }
                    else
                    {
                        intTranType = 2;
                        cmpsrClientSubs.ValidateField();
                        int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
                    }

                    HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
                        checkFeeEditSubs.Checked, nispPercentageFeeSubs.Value, 1, out strFeeCurr, out decNominalFee,
                        out decPctFee, cmpsrCIFSubs.Text1);

                    nispMoneyFeeSubs.Value = decNominalFee;
                    nispPercentageFeeSubs.Value = decPctFee;
                    labelFeeCurrencySubs.Text = "%";
                    _KeteranganFeeSubs.Text = strFeeCurr;
                }
            }
            catch
            {
                return;
            }

            //20150824, liliana, LIBST13020, end
        }

        private void cmpsrCurrSubs_onNispText2Changed(object sender, EventArgs e)
        {
            labelFeeCurrencySubs.Text = cmpsrCurrSubs.Text1;
            _KeteranganFeeSubs.Text = "%";

        }

        private void nispMoneyFeeSubs_onNispMoneyTextChanged(object sender, EventArgs e)
        {

            if (_ComboJenisSubs.Text == "By %")
            {
                _KeteranganFeeSubs.Text = cmpsrCurrSubs.Text1;
                labelFeeCurrencySubs.Text = "%";
                ByPercent = true;
            }
            else
            {
                _KeteranganFeeSubs.Text = "%";
                labelFeeCurrencySubs.Text = cmpsrCurrSubs.Text1;
                ByPercent = false;
            }

            //20150505, liliana, LIBST13020, begin
            //if ((cmpsrProductSubs.Text1 != "") && (nispMoneyNomSubs.Value != 0))
            if ((cmpsrProductSubs.Text1 != "") && (nispMoneyNomSubs.Value != 0) && (checkFeeEditSubs.Checked))
            //20150505, liliana, LIBST13020, end
            {
                //20150415, liliana, LIBST13020, begin
                cmpsrProductSubs.ValidateField();
                //20150415, liliana, LIBST13020, end
                decimal decNominalFee, decPctFee;
                int intProdId, intClientid, intTranType;
                int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);


                if (IsSubsNew)
                {
                    intTranType = 1;
                    intClientid = 0;
                }
                else
                {
                    intTranType = 2;
                    //20150505, liliana, LIBST13020, begin
                    cmpsrClientSubs.ValidateField();
                    //20150505, liliana, LIBST13020, end
                    //20150421, liliana, LIBST13020, begin
                    //int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
                    try
                    {
                        int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
                    }
                    catch
                    {
                        return;
                    }
                    //20150421, liliana, LIBST13020, end
                }

                HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
                    checkFeeEditSubs.Checked, nispMoneyFeeSubs.Value, 2, out strFeeCurr, out decNominalFee,
                    out decPctFee, cmpsrCIFSubs.Text1);

                nispPercentageFeeSubs.Value = decPctFee;

            }

        }

        private void cmpsrProductRedemp_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductRedemp.Text2 = "";
            cmpsrClientRedemp.Text1 = "";
            cmpsrClientRedemp.Text2 = "";

            nispOutstandingUnitRedemp.Value = 0;
            nispOutstandingUnitRedemp.Text = "";

            nispRedempUnit.Value = 0;
            nispRedempUnit.Text = "";
            checkAll.Checked = false;
        }

        private void cmpsrProductRedemp_onNispText2Changed(object sender, EventArgs e)
        {
            //20160829, liliana, LOGEN00196, begin
            //cmpsrClientRedemp.Criteria = cmpsrCIFRedemp.Text1.Trim() + "#" + cmpsrProductRedemp.Text1.Trim() + "#" + _strTabName;
            cmpsrClientRedemp.Criteria = cmpsrCIFRedemp.Text1.Trim() + "#" + cmpsrProductRedemp.Text1.Trim() + "#" + _strTabName
                                        + "#" + cmbTARedemp.SelectedIndex.ToString();
            //20160829, liliana, LOGEN00196, end
        }

        private void cmpsrClientRedemp_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrClientRedemp.Text2 = "";
            nispOutstandingUnitRedemp.Value = 0;

            //20220520, Lita, RDN-781, begin
            nispOutstandingUnitRedemp.Value = 0;
            nispOutstandingUnitRedemp.Text = "";

            nispRedempUnit.Value = 0;
            nispRedempUnit.Text = "";
            checkAll.Checked = false;
            //20220520, Lita, RDN-781, end
        }

        private void nispRedempUnit_Leave(object sender, EventArgs e)
        {
            try
            {
                //20150619, liliana, LIBST13020, begin
                //20150828, liliana, LIBST13020, begin
                //cmpsrProductRedemp.ValidateField();
                //cmpsrClientRedemp.ValidateField();
                //20150828, liliana, LIBST13020, end
                //20150619, liliana, LIBST13020, end
                decimal decNominalFee, decPctFee;
                int intProdId, intClientid, intTranType;
                //20150828, liliana, LIBST13020, begin
                //int.TryParse(cmpsrProductRedemp[2].ToString(), out intProdId);
                //int.TryParse(cmpsrClientRedemp[2].ToString(), out intClientid);
                string strPrdId = GetImportantData("PRODUKID", cmpsrProductRedemp.Text1);
                int.TryParse(strPrdId, out intProdId);

                string strClientId = GetImportantData("CLIENTID", cmpsrClientRedemp.Text1);
                int.TryParse(strClientId, out intClientid);
                //20150828, liliana, LIBST13020, end

                if (nispRedempUnit.Value == nispOutstandingUnitRedemp.Value)
                {
                    IsRedempAll = true;
                    checkAll.Checked = true;
                }
                //20150427, liliana, LIBST13020, begin
                else
                {
                    IsRedempAll = false;
                    checkAll.Checked = false;
                }
                //20150427, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, begin
                //20150619, liliana, LIBST13020, begin
                //if (_ComboJenisRedemp.Text == "By %")
                if ((_ComboJenisRedemp.Text == "By %") && (checkFeeEditRedemp.Checked))
                //20150619, liliana, LIBST13020, end
                {
                    ByPercent = true;
                }
                else
                {
                    ByPercent = false;
                }
                //20150505, liliana, LIBST13020, end


                if (IsRedempAll)
                {
                    intTranType = 4;
                }
                else
                {
                    intTranType = 3;
                }

                //20210406, Lita, RDN-563 RDN-594, format 4 decimal di belakang koma, begin
                nispRedempUnit.Text = String.Format("{0:N4}", nispRedempUnit.Value);
                //20210406, Lita, RDN-563 RDN-594, format 4 decimal di belakang koma, end

                HitungFee(intProdId, intClientid, intTranType, 0, nispRedempUnit.Value, false,
                    //20150619, liliana, LIBST13020, begin
                    //checkFeeEditRedemp.Checked, nispPercentageFeeRedemp.Value, 1, out strFeeCurr,
                    checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, 1, out strFeeCurr,
                    //20150619, liliana, LIBST13020, end
                    out decNominalFee, out decPctFee, cmpsrCIFRedemp.Text1);

                //20150619, liliana, LIBST13020, begin
                //nispMoneyFeeRedemp.Value = decNominalFee;
                //nispPercentageFeeRedemp.Value = decPctFee;
                nispMoneyFeeRedemp.Value = decPctFee;
                nispPercentageFeeRedemp.Value = decNominalFee;
                //20150619, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, begin
                //labelFeeCurrencyRedemp.Text = strFeeCurr;
                labelFeeCurrencyRedemp.Text = "%";
                _KeteranganFeeRedemp.Text = strFeeCurr;
                //20150505, liliana, LIBST13020, end
            }
            catch
            {
                return;
            }
        }

        private void _ComboJenisRedemp_SelectedIndexChanged(object sender, EventArgs e)
        {
            nispMoneyFeeRedemp.Value = 0;
            nispPercentageFeeRedemp.Value = 0;

            if (_ComboJenisRedemp.Text == "By %")
            {
                _KeteranganFeeRedemp.Text = strFeeCurr;
                labelFeeCurrencyRedemp.Text = "%";
                ByPercent = true;
                //20220802, Lita, RDN-825, begin
                //nispMoneyFeeRedemp.DecimalPlace = 3;
                nispMoneyFeeRedemp.DecimalPlace = 4;
                //20220802, Lita, RDN-825, end
                //20150622, liliana, LIBST13020, begin
                nispPercentageFeeRedemp.DecimalPlace = 2;
                //20150622, liliana, LIBST13020, end
            }
            else
            {
                _KeteranganFeeRedemp.Text = "%";
                labelFeeCurrencyRedemp.Text = strFeeCurr;
                ByPercent = false;
                nispMoneyFeeRedemp.DecimalPlace = 2;
                //20150622, liliana, LIBST13020, begin
                //20220802, Lita, RDN-825, begin
                //nispPercentageFeeRedemp.DecimalPlace = 3;
                nispPercentageFeeRedemp.DecimalPlace = 4;
                //20220802, Lita, RDN-825, end
                //20150622, liliana, LIBST13020, end
            }
        }

        private void nispMoneyFeeRedemp_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            if (_ComboJenisRedemp.Text == "By %")
            {
                _KeteranganFeeRedemp.Text = strFeeCurr;
                labelFeeCurrencyRedemp.Text = "%";
                ByPercent = true;
            }
            else
            {
                _KeteranganFeeRedemp.Text = "%";
                labelFeeCurrencyRedemp.Text = strFeeCurr;
                ByPercent = false;
            }

            if ((cmpsrProductRedemp.Text1 != "") && (nispRedempUnit.Value != 0))
            {
                //20150619, liliana, LIBST13020, begin
                //20150828, liliana, LIBST13020, begin
                //cmpsrProductRedemp.ValidateField();
                //cmpsrClientRedemp.ValidateField();
                //20150828, liliana, LIBST13020, end
                //20150619, liliana, LIBST13020, end
                decimal decNominalFee, decPctFee;
                int intProdId, intClientid, intTranType;
                //20150828, liliana, LIBST13020, begin
                //int.TryParse(cmpsrProductRedemp[2].ToString(), out intProdId);
                //int.TryParse(cmpsrClientRedemp[2].ToString(), out intClientid);
                string strPrdId = GetImportantData("PRODUKID", cmpsrProductRedemp.Text1);
                int.TryParse(strPrdId, out intProdId);

                string strClientId = GetImportantData("CLIENTID", cmpsrClientRedemp.Text1);
                int.TryParse(strClientId, out intClientid);
                //20150828, liliana, LIBST13020, end

                //20150619, liliana, LIBST13020, begin
                if ((_ComboJenisRedemp.Text == "By %") && (checkFeeEditRedemp.Checked))
                {
                    ByPercent = true;
                }
                else
                {
                    ByPercent = false;
                }
                //20150619, liliana, LIBST13020, end
                if (IsRedempAll)
                {
                    intTranType = 4;
                }
                else
                {
                    intTranType = 3;
                }

                HitungFee(intProdId, intClientid, intTranType, 0, nispRedempUnit.Value, false,
                    //20150619, liliana, LIBST13020, begin
                    //checkFeeEditRedemp.Checked, nispPercentageFeeRedemp.Value, 2, out strFeeCurr,
                checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, 2, out strFeeCurr,
                    //20150619, liliana, LIBST13020, end
                out decNominalFee, out decPctFee, cmpsrCIFRedemp.Text1);

                nispPercentageFeeRedemp.Value = decPctFee;

            }
        }

        private void buttonAddRedemp_Click(object sender, EventArgs e)
        {
            //20150505, liliana, LIBST13020, begin
            if (buttonAddRedemp.Text == "&Done")
            {
                DisableFormTrxRedemp(true);
                ResetFormTrxRedemp();
                buttonEditRedemp.Enabled = false;
                buttonAddRedemp.Text = "&Done";
            }
            else if (buttonAddRedemp.Text == "&Add")
            {
                if (dataGridViewRedemp.Rows.Count >= 3)
                {
                    MessageBox.Show("Maksimal hanya dapat menambah 3 transaksi !", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20150505, liliana, LIBST13020, end
                if (cmpsrProductRedemp.Text1 == "")
                {
                    MessageBox.Show("Kode Produk harus diisi", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (cmpsrClientRedemp.Text1 == "")
                {
                    MessageBox.Show("Client Code harus diisi", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (nispRedempUnit.Value == 0)
                {
                    MessageBox.Show("Unit harus diisi", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20160829, liliana, LOGEN00196, begin
                if (cmbTARedemp.SelectedIndex == -1)
                {
                    MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, end

                if (nispRedempUnit.Value > nispOutstandingUnitRedemp.Value)
                {
                    MessageBox.Show("Redemption unit tidak boleh lebih besar dari Outstanding Unit!", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20150505, liliana, LIBST13020, begin

                string strData = String.Format("ClientCode='{0}'",
                       cmpsrClientRedemp.Text1);

                if (cTransaksi.dttRedemption.Select(strData).Length != 0)
                {
                    MessageBox.Show("Redemption untuk Client Code " + cmpsrClientRedemp.Text1 + " sudah ada!", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //20150505, liliana, LIBST13020, end

                //20200310, pratama, RDN-45,begin
                string prodCode = cmpsrProductRedemp.Text1;
                if (!clsValidator.ValidasiProdukProteksi(ClQ, prodCode))
                {
                    return;
                }
                //20200310, pratama, RDN-45,end

                //20240220, gio, RDN-1108, begin
                if (dateTglTransaksiRedemp.Value > DateTime.Today.AddHours(23))
                {
                    MessageBox.Show("Tanggal instruksi tidak lebih dari tanggal hari ini", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                if (dateTglTransaksiRedemp.Value < DateTime.Today)
                {
                    MessageBox.Show("Tanggal instruksi tidak kurang dari tanggal hari ini", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (dateTglTransaksiRedemp.Value == DateTime.Today)
                {
                    MessageBox.Show("Waktu instruksi tidak dapat diisi 00.00.00. Mohon untuk menginput waktu instruksi transaksi sesuai informasi Nasabah", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dateTglTransaksiRedemp.Value > DateTime.Today.AddHours(13))
                {
                    MessageBox.Show("Waktu instruksi diatas 13.00.00 menggunakan NAV berikutnya", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //20240220, gio, RDN-1108, end
                try
                {
                    string strTranCode, strNewClientCode;
                    //20150825, liliana, LIBST13020, begin
                    string strWarnMsg = "";
                    string strWarnMsg2 = "";
                    //20150825, liliana, LIBST13020, end

                    GenerateTranCodeAndClientCode(_strTabName, false, cmpsrProductRedemp.Text1,
                        //20150505, liliana, LIBST13020, begin
                        //cmpsrClientRedemp.Text1, out strTranCode, out strNewClientCode, cmpsrCIFRedemp.Text1);
                        cmpsrClientRedemp.Text1, out strTranCode, out strNewClientCode, cmpsrCIFRedemp.Text1,
                        checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, intPeriod
                        //20150610, liliana, LIBST13020, begin
                        , false, (double)nispPercentageFeeRedemp.Value, 0, (double)nispRedempUnit.Value, IsRedempAll, 0, 0
                        //20150610, liliana, LIBST13020, end
                        //20150819, liliana, LIBST13020, begin
                        , _intType
                        //20150819, liliana, LIBST13020, end
                        //20150825, liliana, LIBST13020, begin
                        , out strWarnMsg
                        , out strWarnMsg2
                        //20150825, liliana, LIBST13020, end
                        //20160829, liliana, LOGEN00196, begin
                        , cmbTARedemp.SelectedIndex
                        //20160829, liliana, LOGEN00196, end
                        //20220520, Lita, RDN-781, begin
                        , "", "", DateTime.Today
                        //20220520, Lita, RDN-781, end
                        );
                    //20150505, liliana, LIBST13020, end
                    //20150825, liliana, LIBST13020, begin
                    if (strWarnMsg2 != "")
                    {
                        if (MessageBox.Show(strWarnMsg2, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    //20150825, liliana, LIBST13020, end

                    if (strTranCode != "")
                    {
                        DataRow dtrRedemption = cTransaksi.dttRedemption.NewRow();
                        //20150818, liliana, LIBST13020, begin
                        //dtrRedemption["NoTrx"] = strTranCode;
                        dtrRedemption["NoTrx"] = "";
                        //20150818, liliana, LIBST13020, end
                        dtrRedemption["TglTrx"] = dateTglTransaksiRedemp.Value;
                        dtrRedemption["KodeProduk"] = cmpsrProductRedemp.Text1;
                        //20150617, liliana, LIBST13020, begin
                        dtrRedemption["NamaProduk"] = cmpsrProductRedemp.Text2;
                        //20150630, liliana, LIBST13020, begin
                        //dtrRedemption["EditFeeBy"] = _ComboJenisRedemp.Text;
                        if (checkFeeEditRedemp.Checked)
                        {
                            dtrRedemption["EditFeeBy"] = _ComboJenisRedemp.Text;
                        }
                        else
                        {
                            dtrRedemption["EditFeeBy"] = "";
                        }
                        //20150630, liliana, LIBST13020, end
                        //20150617, liliana, LIBST13020, end
                        dtrRedemption["ClientCode"] = strNewClientCode;
                        dtrRedemption["OutstandingUnit"] = nispOutstandingUnitRedemp.Value;
                        dtrRedemption["RedempUnit"] = nispRedempUnit.Value;
                        dtrRedemption["PhoneOrder"] = checkPhoneOrderRedemp.Checked;
                        dtrRedemption["EditFee"] = checkFeeEditRedemp.Checked;
                        dtrRedemption["JenisFee"] = _ComboJenisRedemp.SelectedIndex;
                        //20150422, liliana, LIBST13020, begin
                        //dtrRedemption["NominalFee"] = nispMoneyFeeRedemp.Value;
                        //dtrRedemption["FeeCurr"] = labelFeeCurrencyRedemp.Text;
                        //dtrRedemption["PctFee"] = nispPercentageFeeRedemp.Value;
                        //dtrRedemption["FeeKet"] = _KeteranganFeeRedemp.Text;
                        dtrRedemption["PctFee"] = nispMoneyFeeRedemp.Value;
                        dtrRedemption["FeeKet"] = labelFeeCurrencyRedemp.Text;
                        dtrRedemption["NominalFee"] = nispPercentageFeeRedemp.Value;
                        dtrRedemption["FeeCurr"] = _KeteranganFeeRedemp.Text;
                        //20150422, liliana, LIBST13020, end
                        dtrRedemption["IsRedempAll"] = IsRedempAll;
                        dtrRedemption["Period"] = intPeriod;
                        dtrRedemption["ApaDiUpdate"] = false;
                        //20160829, liliana, LOGEN00196, begin
                        dtrRedemption["TrxTaxAmnesty"] = cmbTARedemp.SelectedIndex;
                        //20160829, liliana, LOGEN00196, end

                        cTransaksi.dttRedemption.Rows.Add(dtrRedemption);
                        cTransaksi.dttRedemption.AcceptChanges();

                        dataGridViewRedemp.DataSource = cTransaksi.dttRedemption;

                        for (int i = 0; i < dataGridViewRedemp.Columns.Count; i++)
                        {
                            if (dataGridViewRedemp.Columns[i].ValueType.ToString() == "System.Decimal")
                            {
                                //20220520, Lita, RDN-825, begin
                                //dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N2";
                                if (dataGridViewRedemp.Columns[i].Name == "PctFee")
                                    dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N4";
                                else
                                    dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N2";
                                //20220802, Lita, RDN-825, end

                            }
                        }

                        dataGridViewRedemp.AutoResizeColumns();
                        subSetVisibleGrid(_strTabName);
                        ResetFormTrxRedemp();
                        //20150505, liliana, LIBST13020, begin
                        DisableFormTrxRedemp(true);
                        //20150820, liliana, LIBST13020, begin
                        //buttonEditRedemp.Enabled = true;
                        buttonEditRedemp.Enabled = false;
                        //20150820, liliana, LIBST13020, end
                        buttonAddRedemp.Enabled = true;
                        //20150505, liliana, LIBST13020, end
                        //20240240220, gio, RDN-1108, begin
                        dateTglTransaksiRedemp.Enabled = false;
                        //20240240220, gio, RDN-1108, end
                        //20150615, liliana, LIBST13020, begin
                        if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRedemp.Text1))
                        {
                            checkPhoneOrderRedemp.Enabled = false;
                            checkPhoneOrderRedemp.Checked = false;
                        }
                        else
                        {
                            checkPhoneOrderRedemp.Enabled = true;
                        }
                        //20150615, liliana, LIBST13020, end
                        //20220119, sandi, RDN-727, begin
                        cmpsrNoRekRedemp.Enabled = false;
                        //20220119, sandi, RDN-727, end
                    }
                    else
                    {
                        MessageBox.Show("Gagal generate kode transaksi!", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                //20150505, liliana, LIBST13020, begin
            }
            //20150505, liliana, LIBST13020, end
        }

        private void buttonEditRedemp_Click(object sender, EventArgs e)
        {
            //20150505, liliana, LIBST13020, begin
            if (buttonEditRedemp.Text == "&Edit")
            {
                DisableFormTrxRedemp(true);
                //20150825,liliana, LIBST13020, begin
                nispRedempUnit.Enabled = false;
                checkAll.Enabled = false;
                //20150825,liliana, LIBST13020, end

                cmpsrProductRedemp.Enabled = false;
                cmpsrClientRedemp.Enabled = false;

                if (checkFeeEditRedemp.Checked)
                {
                    _ComboJenisRedemp.Enabled = true;
                    nispMoneyFeeRedemp.Enabled = true;
                    nispPercentageFeeRedemp.Enabled = false;
                }

                if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRedemp.Text1))
                {
                    checkPhoneOrderRedemp.Enabled = false;
                    checkPhoneOrderRedemp.Checked = false;
                }
                else
                {
                    checkPhoneOrderRedemp.Enabled = true;
                }

                buttonEditRedemp.Text = "&Done";
                buttonAddRedemp.Enabled = false;
            }
            else if (buttonEditRedemp.Text == "&Done")
            {

                //20150505, liliana, LIBST13020, end
                if (MessageBox.Show("Apakah akan merubah transaksi Trancode " + textNoTransaksiRedemp.Text + "?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
                else
                {
                    //20150629, liliana, LIBST13020, begin
                    if (_StatusTransaksiRedemp == "Rejected")
                    {
                        MessageBox.Show("Transaksi dengan Status Rejected tidak dapat diedit!", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150629, liliana, LIBST13020, end
                    //20150916, liliana, LIBST13020, begin
                    else if (_StatusTransaksiRedemp == "Reversed")
                    {
                        MessageBox.Show("Transaksi dengan Status Reversed tidak dapat diedit!", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else if (_StatusTransaksiRedemp == "Cancel By PO")
                    {
                        MessageBox.Show("Transaksi dengan Status Cancel By PO tidak dapat diedit!", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150916, liliana, LIBST13020, end
                    //20150415, liliana, LIBST13020, begin

                    if (cmpsrProductRedemp.Text1 == "")
                    {
                        MessageBox.Show("Kode Produk harus diisi", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (cmpsrClientRedemp.Text1 == "")
                    {
                        MessageBox.Show("Client Code harus diisi", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (nispRedempUnit.Value == 0)
                    {
                        MessageBox.Show("Unit harus diisi", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    //20150921, liliana, LIBST13020, begin
                    //if (nispRedempUnit.Value > nispOutstandingUnitRedemp.Value)
                    //{
                    //    MessageBox.Show("Redemption unit tidak boleh lebih besar dari Outstanding Unit!", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //    return;
                    //}
                    //20150921, liliana, LIBST13020, end
                    //20150415, liliana, LIBST13020, end
                    string strData = String.Format("NoTrx='{0}'", textNoTransaksiRedemp.Text);

                    //20200310, pratama, RDN-45,begin
                    string prodCode = cmpsrProductRedemp.Text1;
                    if (!clsValidator.ValidasiProdukProteksi(ClQ, prodCode))
                    {
                        return;
                    }
                    //20200310, pratama, RDN-45,end

                    //20240220, gio, RDN-1108, begin
                    if (dateTglTransaksiRedemp.Value > DateTime.Today.AddHours(23))
                    {
                        MessageBox.Show("Tanggal instruksi tidak lebih dari tanggal hari ini", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (dateTglTransaksiRedemp.Value < DateTime.Today)
                    {
                        MessageBox.Show("Tanggal instruksi tidak kurang dari tanggal hari ini", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (dateTglTransaksiRedemp.Value == DateTime.Today)
                    {
                        MessageBox.Show("Waktu instruksi tidak dapat diisi 00.00.00. Mohon untuk menginput waktu instruksi transaksi sesuai informasi Nasabah", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (dateTglTransaksiRedemp.Value > DateTime.Today.AddHours(13))
                    {
                        MessageBox.Show("Waktu instruksi diatas 13.00.00 menggunakan NAV berikutnya", "Transaksi Redemption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    //20240220, gio, RDN-1108, end
                    try
                    {
                        if (cTransaksi.dttRedemption.Select(strData).Length > 0)
                        {
                            //20150819, liliana, LIBST13020, begin
                            string strTranCode, strNewClientCode;
                            //20150825, liliana, LIBST13020, begin
                            string strWarnMsg = "";
                            string strWarnMsg2 = "";
                            //20150825, liliana, LIBST13020, end

                            //20150827, liliana, LIBST13020, begin 
                            //GenerateTranCodeAndClientCode(_strTabName, false, cmpsrProductRedemp.Text1,
                            bool _result = false;

                            _result = GenerateTranCodeAndClientCode(_strTabName, false, cmpsrProductRedemp.Text1,
                                //20150827, liliana, LIBST13020, end
                                cmpsrClientRedemp.Text1, out strTranCode, out strNewClientCode, cmpsrCIFRedemp.Text1,
                                checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, intPeriod
                                , false, (double)nispPercentageFeeRedemp.Value, 0, (double)nispRedempUnit.Value, IsRedempAll, 0, 0
                                , _intType
                                //20150825, liliana, LIBST13020, begin
                                , out strWarnMsg
                                , out strWarnMsg2
                                //20150825, liliana, LIBST13020, end
                                //20160829, liliana, LOGEN00196, begin
                                , cmbTARedemp.SelectedIndex
                                //20160829, liliana, LOGEN00196, end
                                //20220520, Lita, RDN-781, begin
                                , "", "", DateTime.Today
                                //20220520, Lita, RDN-781, end
                                );

                            //20150819, liliana, LIBST13020, end
                            //20150827, liliana, LIBST13020, begin
                            if (!_result)
                            {
                                return;
                            }
                            //20150827, liliana, LIBST13020, end
                            //20150825, liliana, LIBST13020, begin
                            if (strWarnMsg2 != "")
                            {
                                if (MessageBox.Show(strWarnMsg2, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                {
                                    MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }

                            //20150825, liliana, LIBST13020, end
                            DataRow[] dtrRedemption = cTransaksi.dttRedemption.Select(strData);

                            dtrRedemption[0]["KodeProduk"] = cmpsrProductRedemp.Text1;
                            //20150617, liliana, LIBST13020, begin
                            dtrRedemption[0]["NamaProduk"] = cmpsrProductRedemp.Text2;
                            //20150630, liliana, LIBST13020, begin
                            //dtrRedemption[0]["EditFeeBy"] = _ComboJenisRedemp.Text;
                            if (checkFeeEditRedemp.Checked)
                            {
                                dtrRedemption[0]["EditFeeBy"] = _ComboJenisRedemp.Text;
                            }
                            else
                            {
                                dtrRedemption[0]["EditFeeBy"] = "";
                            }
                            //20150630, liliana, LIBST13020, end
                            //20150617, liliana, LIBST13020, end
                            dtrRedemption[0]["ClientCode"] = cmpsrClientRedemp.Text1;
                            dtrRedemption[0]["OutstandingUnit"] = nispOutstandingUnitRedemp.Value;
                            dtrRedemption[0]["RedempUnit"] = nispRedempUnit.Value;
                            dtrRedemption[0]["PhoneOrder"] = checkPhoneOrderRedemp.Checked;

                            dtrRedemption[0]["EditFee"] = checkFeeEditRedemp.Checked;
                            dtrRedemption[0]["JenisFee"] = _ComboJenisRedemp.SelectedIndex;
                            //20150422, liliana, LIBST13020, begin
                            //dtrRedemption[0]["NominalFee"] = nispMoneyFeeRedemp.Value;
                            //dtrRedemption[0]["FeeCurr"] = labelFeeCurrencyRedemp.Text;
                            //dtrRedemption[0]["PctFee"] = nispPercentageFeeRedemp.Value;
                            //dtrRedemption[0]["FeeKet"] = _KeteranganFeeRedemp.Text;
                            dtrRedemption[0]["PctFee"] = nispMoneyFeeRedemp.Value;
                            dtrRedemption[0]["FeeKet"] = labelFeeCurrencyRedemp.Text;
                            dtrRedemption[0]["NominalFee"] = nispPercentageFeeRedemp.Value;
                            dtrRedemption[0]["FeeCurr"] = _KeteranganFeeRedemp.Text;
                            //20150422, liliana, LIBST13020, end
                            dtrRedemption[0]["IsRedempAll"] = IsRedempAll;
                            dtrRedemption[0]["Period"] = intPeriod;
                            dtrRedemption[0]["ApaDiUpdate"] = true;
                            //20160829, liliana, LOGEN00196, begin
                            dtrRedemption[0]["TrxTaxAmnesty"] = cmbTARedemp.SelectedIndex;
                            //20160829, liliana, LOGEN00196, end

                            cTransaksi.dttRedemption.AcceptChanges();

                            dataGridViewRedemp.DataSource = cTransaksi.dttRedemption;

                            for (int i = 0; i < dataGridViewRedemp.Columns.Count; i++)
                            {
                                if (dataGridViewRedemp.Columns[i].ValueType.ToString() == "System.Decimal")
                                {
                                    //20220520, Lita, RDN-825, begin
                                    //dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N2";
                                    if (dataGridViewRedemp.Columns[i].Name == "PctFee")
                                        dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N4";
                                    else
                                        dataGridViewRedemp.Columns[i].DefaultCellStyle.Format = "N2";
                                    //20220802, Lita, RDN-825, end

                                }
                            }

                            dataGridViewRedemp.AutoResizeColumns();
                            subSetVisibleGrid(_strTabName);
                            ResetFormTrxRedemp();
                            //20150505, liliana, LIBST13020, begin
                            //20150810, liliana, LIBSTT13020, begin
                            //DisableFormTrxRedemp(false);
                            //buttonEditRedemp.Enabled = true;
                            //buttonAddRedemp.Enabled = true;
                            if (_intType == 1)
                            {
                                DisableFormTrxRedemp(true);
                                buttonEditRedemp.Enabled = true;
                                buttonAddRedemp.Enabled = true;
                            }
                            else if (_intType == 2)
                            {
                                DisableFormTrxRedemp(false);
                                buttonEditRedemp.Enabled = true;
                                buttonAddRedemp.Enabled = false;
                            }
                            //20150810, liliana, LIBSTT13020, end
                            //20150505, liliana, LIBST13020, end
                        }
                        else
                        {
                            MessageBox.Show("Data tidak ditemukan", "Transaksi Redemption ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Transaksi Redemption");
                        return;
                    }
                }
                //20150505, liliana, LIBST13020, begin
            }

            //20150505, liliana, LIBST13020, end
        }

        private void dataGridViewRedemp_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (buttonEditRedemp.Text == "&Done")
            {
                try
                {
                    int intComboBox;

                    //20160829, liliana, LOGEN00196, begin
                    bool bIsTA; int iIsTA;
                    iIsTA = -1;
                    bIsTA = (bool)dataGridViewRedemp["TrxTaxAmnesty", e.RowIndex].Value;

                    if (bIsTA)
                    {
                        iIsTA = 1;
                    }
                    else
                    {
                        iIsTA = 0;
                    }

                    cmbTARedemp.SelectedIndex = iIsTA;
                    //20160829, liliana, LOGEN00196, end
                    //20150629, liliana, LIBST13020, begin
                    _StatusTransaksiRedemp = dataGridViewRedemp["StatusTransaksi", e.RowIndex].Value.ToString();
                    //20150629, liliana, LIBST13020, end
                    textNoTransaksiRedemp.Text = dataGridViewRedemp["NoTrx", e.RowIndex].Value.ToString();
                    dateTglTransaksiRedemp.Value = (DateTime)dataGridViewRedemp["TglTrx", e.RowIndex].Value;

                    cmpsrProductRedemp.Text1 = dataGridViewRedemp["KodeProduk", e.RowIndex].Value.ToString();
                    cmpsrProductRedemp.ValidateField();

                    nispOutstandingUnitRedemp.Value = (decimal)dataGridViewRedemp["OutstandingUnit", e.RowIndex].Value;
                    nispRedempUnit.Value = (decimal)dataGridViewRedemp["RedempUnit", e.RowIndex].Value;

                    if (nispOutstandingUnitRedemp.Value == nispRedempUnit.Value)
                    {
                        checkAll.Checked = true;
                    }
                    else
                    {
                        checkAll.Checked = false;
                    }

                    checkPhoneOrderRedemp.Checked = (bool)dataGridViewRedemp["PhoneOrder", e.RowIndex].Value;
                    checkFeeEditRedemp.Checked = (bool)dataGridViewRedemp["EditFee", e.RowIndex].Value;

                    int.TryParse(dataGridViewRedemp["JenisFee", e.RowIndex].Value.ToString(), out intComboBox);
                    _ComboJenisRedemp.SelectedIndex = intComboBox;

                    nispMoneyFeeRedemp.Value = (decimal)dataGridViewRedemp["PctFee", e.RowIndex].Value;
                    nispPercentageFeeRedemp.Value = (decimal)dataGridViewRedemp["NominalFee", e.RowIndex].Value;

                    labelFeeCurrencyRedemp.Text = dataGridViewRedemp["FeeKet", e.RowIndex].Value.ToString();
                    _KeteranganFeeRedemp.Text = dataGridViewRedemp["FeeCurr", e.RowIndex].Value.ToString();

                    cmpsrClientRedemp.Text1 = dataGridViewRedemp["ClientCode", e.RowIndex].Value.ToString();
                    cmpsrClientRedemp.ValidateField();
                    //20150820, liliana, LIBST13020, begin
                    nispOutstandingUnitRedemp.Value = (decimal)dataGridViewRedemp["OutstandingUnit", e.RowIndex].Value;
                    //20150820, liliana, LIBST13020, end

                    //20210922, korvi, RDN-674, begin
                    cmpsrNoRekRedemp.Criteria = cmpsrCIFRedemp.Text1 + "#" + cmbTARedemp.SelectedIndex.ToString();
                    cmpsrNoRekRedemp.Text1 = dataGridViewRedemp["SelectedAccNo", e.RowIndex].Value.ToString();
                    cmpsrNoRekRedemp.Text2 = dataGridViewRedemp["TranCCY", e.RowIndex].Value.ToString();
                    //20220119, sandi, RDN-727, begin
                    //cmpsrNoRekRedemp.ValidateField();
                    //20220119, sandi, RDN-727, end
                    //20210922, korvi, RDN-674, end
                }
                catch
                {
                    return;
                }
            }
        }

        private void buttonAddRDB_Click(object sender, EventArgs e)
        {
            //20150505, liliana, LIBST13020, begin
            if (buttonAddRDB.Text == "&Done")
            {
                DisableFormTrxRDB(true);
                ResetFormTrxSubsRDB();
                buttonEditRDB.Enabled = false;
                buttonAddRDB.Text = "&Done";
            }
            else if (buttonAddRDB.Text == "&Add")
            {
                if (dataGridViewRDB.Rows.Count >= 3)
                {
                    MessageBox.Show("Maksimal hanya dapat menambah 3 transaksi !", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20150505, liliana, LIBST13020, end
                if (cmpsrProductRDB.Text1 == "")
                {
                    MessageBox.Show("Kode Produk harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (cmpsrCurrRDB.Text1 == "")
                {
                    MessageBox.Show("Mata Uang Produk harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (nispMoneyNomRDB.Value == 0)
                {
                    MessageBox.Show("Nominal harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (nispJangkaWktRDB.Value == 0)
                {
                    MessageBox.Show("Jangka Waktu harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (cmbFrekPendebetanRDB.Text == "")
                {
                    MessageBox.Show("Frekuensi Pendebetan harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                //20200408, Lita, RDN-88, begin
                if (cmbFrekDebetMethodRDB.Text == "")
                {
                    MessageBox.Show("Metode Frekuensi Pendebetan harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20200408, Lita, RDN-88, begin

                if (cmbAutoRedempRDB.Text == "")
                {
                    MessageBox.Show("Auto Redemption harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (cmbAsuransiRDB.Text == "")
                {
                    MessageBox.Show("Asuransi harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //20160829, liliana, LOGEN00196, begin

                if (cmbTARDB.SelectedIndex == -1)
                {
                    MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //20160829, liliana, LOGEN00196, end

                //20240220, gio, RDN-1108, begin
                if (dateTglTransaksiRDB.Value > DateTime.Today.AddHours(23))
                {
                    MessageBox.Show("Tanggal instruksi tidak lebih dari tanggal hari ini", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (dateTglTransaksiRDB.Value < DateTime.Today)
                {
                    MessageBox.Show("Tanggal instruksi tidak kurang dari tanggal hari ini", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (dateTglTransaksiRDB.Value == DateTime.Today)
                {
                    MessageBox.Show("Waktu instruksi tidak dapat diisi 00.00.00. Mohon untuk menginput waktu instruksi transaksi sesuai informasi Nasabah", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dateTglTransaksiRDB.Value > DateTime.Today.AddHours(13))
                {
                    MessageBox.Show("Waktu instruksi diatas 13.00.00 menggunakan NAV berikutnya", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //20240220, gio, RDN-1108, end
                try
                {
                    string strTranCode, strNewClientCode;
                    //20150610, liliana, LIBST13020, begin
                    int _FrekPendebetan;
                    int.TryParse(cmbFrekPendebetanRDB.Text, out _FrekPendebetan);

                    //20200408, Lita, RDN-88, begin
                    string strFreqDebetMethod = cmbFrekDebetMethodRDB.SelectedValue.ToString();
                    //20200408, Lita, RDN-88, end

                    //20150610, liliana, LIBST13020, end
                    //20150825, liliana, LIBST13020, begin
                    string strWarnMsg = "";
                    string strWarnMsg2 = "";
                    //20150825, liliana, LIBST13020, end

                    GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductRDB.Text1,
                        //20150505, liliana, LIBST13020, begin
                        //cmpsrClientRDB.Text1, out strTranCode, out strNewClientCode, cmpsrCIFRDB.Text1);
                        cmpsrClientRDB.Text1, out strTranCode, out strNewClientCode, cmpsrCIFRDB.Text1,
                        checkFeeEditRDB.Checked, nispPercentageFeeRDB.Value, 0
                        //20150610, liliana, LIBST13020, begin
                        , false, (double)nispMoneyFeeRDB.Value, (double)nispMoneyNomRDB.Value, 0, false, _FrekPendebetan, (int)nispJangkaWktRDB.Value
                        //20150610, liliana, LIBST13020, end
                        //20150819, liliana, LIBST13020, begin
                        , _intType
                        //20150819, liliana, LIBST13020, end
                        //20150825, liliana, LIBST13020, begin
                        , out strWarnMsg
                        , out strWarnMsg2
                        //20150825, liliana, LIBST13020, end
                        //20160829, liliana, LOGEN00196, begin
                        , cmbTARDB.SelectedIndex
                        //20160829, liliana, LOGEN00196, end
                        //20220520, Lita, RDN-781, begin
                        , cmbFrekDebetMethodRDB.SelectedValue.ToString(), cmbAsuransiRDB.Text, dtTglDebetRDB.Value
                        //20220520, Lita, RDN-781, end
                        );
                    //20150505, liliana, LIBST13020, end
                    //20150825, liliana, LIBST13020, begin
                    if (strWarnMsg != "")
                    {
                        //20170825, liliana, COPOD17271, begin
                        if (MessageBox.Show("Produk yang dipilih diatas ketentuan profile nasabah. Lanjutkan transaksi?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else
                        {
                            //20170825, liliana, COPOD17271, end
                            MessageBox.Show("Profil Risiko produk lebih tinggi dari Profil Risiko Nasabah . PASTIKAN Nasabah sudah menandatangani kolom Profil Risiko pada Subscription/Switching Form", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            //20170825, liliana, COPOD17271, begin
                        }
                        //20170825, liliana, COPOD17271, end                    
                    }

                    if (strWarnMsg2 != "")
                    {
                        if (MessageBox.Show(strWarnMsg2, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    //20150825, liliana, LIBST13020, end

                    if (strTranCode != "")
                    {
                        DataRow dtrSubsRDB = cTransaksi.dttSubsRDB.NewRow();
                        //20150819, liliana, LIBST13020, begin
                        //dtrSubsRDB["NoTrx"] = strTranCode;
                        dtrSubsRDB["NoTrx"] = "";
                        //20150819, liliana, LIBST13020, end
                        dtrSubsRDB["TglTrx"] = dateTglTransaksiRDB.Value;
                        dtrSubsRDB["KodeProduk"] = cmpsrProductRDB.Text1;
                        //20150617, liliana, LIBST13020, begin
                        dtrSubsRDB["NamaProduk"] = cmpsrProductRDB.Text2;
                        //20150630, liliana, LIBST13020, begin
                        //dtrSubsRDB["EditFeeBy"] = _ComboJenisRDB.Text;
                        if (checkFeeEditRDB.Checked)
                        {
                            dtrSubsRDB["EditFeeBy"] = _ComboJenisRDB.Text;
                        }
                        else
                        {
                            dtrSubsRDB["EditFeeBy"] = "";

                        }
                        //20150630, liliana, LIBST13020, end
                        //20150617, liliana, LIBST13020, end
                        dtrSubsRDB["ClientCode"] = strNewClientCode;
                        dtrSubsRDB["CCY"] = cmpsrCurrRDB.Text1;
                        dtrSubsRDB["Nominal"] = nispMoneyNomRDB.Value;

                        dtrSubsRDB["JangkaWaktu"] = nispJangkaWktRDB.Value;
                        dtrSubsRDB["JatuhTempo"] = globalJatuhTempoRDB;
                        dtrSubsRDB["FrekPendebetan"] = cmbFrekPendebetanRDB.Text;

                        //20200408, Lita, RDN-88, begin
                        dtrSubsRDB["FrekDebetMethod"] = cmbFrekDebetMethodRDB.Text;
                        dtrSubsRDB["FrekDebetMethodValue"] = cmbFrekDebetMethodRDB.SelectedValue.ToString();
                        dtrSubsRDB["TanggalDebet"] = dtTglDebetRDB.Value;
                        //20200408, Lita, RDN-88, end


                        dtrSubsRDB["AutoRedemption"] = cmbAutoRedempRDB.Text;
                        dtrSubsRDB["Asuransi"] = cmbAsuransiRDB.Text;

                        dtrSubsRDB["PhoneOrder"] = checkPhoneOrderRDB.Checked;
                        dtrSubsRDB["EditFee"] = checkFeeEditRDB.Checked;
                        dtrSubsRDB["JenisFee"] = _ComboJenisRDB.SelectedIndex;
                        dtrSubsRDB["NominalFee"] = nispMoneyFeeRDB.Value;
                        dtrSubsRDB["FeeCurr"] = labelFeeCurrencyRDB.Text;
                        dtrSubsRDB["PctFee"] = nispPercentageFeeRDB.Value;
                        dtrSubsRDB["FeeKet"] = _KeteranganFeeRDB.Text;

                        dtrSubsRDB["ApaDiUpdate"] = false;
                        //20160829, liliana, LOGEN00196, begin
                        dtrSubsRDB["TrxTaxAmnesty"] = cmbTARDB.SelectedIndex;
                        //20160829, liliana, LOGEN00196, end


                        cTransaksi.dttSubsRDB.Rows.Add(dtrSubsRDB);
                        cTransaksi.dttSubsRDB.AcceptChanges();

                        dataGridViewRDB.DataSource = cTransaksi.dttSubsRDB;

                        for (int i = 0; i < dataGridViewRDB.Columns.Count; i++)
                        {
                            if (dataGridViewRDB.Columns[i].ValueType.ToString() == "System.Decimal")
                            {
                                //20220802, Lita, RDN-825, begin
                                //dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N2";
                                if (dataGridViewRDB.Columns[i].Name == "PctFee")
                                    dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N4";
                                else
                                    dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N2";
                                //20220802, Lita, RDN-825, end
                            }
                        }

                        dataGridViewRDB.AutoResizeColumns();
                        subSetVisibleGrid(_strTabName);
                        ResetFormTrxSubsRDB();
                        //20150505, liliana, LIBST13020, begin
                        DisableFormTrxRDB(true);
                        //20150820, liliana, LIBST13020, begin
                        //buttonEditRDB.Enabled = true;
                        buttonEditRDB.Enabled = false;
                        //20150820, liliana, LIBST13020, end
                        buttonAddRDB.Enabled = true;
                        //20150505, liliana, LIBST13020, end
                        //20240240220, gio, RDN-1108, begin
                        dateTglTransaksiRDB.Enabled = false;
                        //20240240220, gio, RDN-1108, end
                        //20150615, liliana, LIBST13020, begin
                        if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRDB.Text1))
                        {
                            checkPhoneOrderRDB.Enabled = false;
                            checkPhoneOrderRDB.Checked = false;
                        }
                        else
                        {
                            checkPhoneOrderRDB.Enabled = true;
                        }
                        //20150615, liliana, LIBST13020, end
                        //20220119, sandi, RDN-727, begin
                        cmpsrNoRekRDB.Enabled = false;
                        //20220119, sandi, RDN-727, end

                    }
                    else
                    {
                        MessageBox.Show("Gagal generate kode transaksi!", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                //20150505, liliana, LIBST13020, begin
            }
            //20150505, liliana, LIBST13020, end
        }

        private void buttonEditRDB_Click(object sender, EventArgs e)
        {
            //20150505, liliana, LIBST13020, begin
            if (buttonEditRDB.Text == "&Edit")
            {
                DisableFormTrxRDB(true);

                if (checkFeeEditRDB.Checked)
                {
                    _ComboJenisRDB.Enabled = true;
                    nispMoneyFeeRDB.Enabled = true;
                    nispPercentageFeeRDB.Enabled = false;
                }

                if (!GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIFRDB.Text1))
                {
                    checkPhoneOrderRDB.Enabled = false;
                    checkPhoneOrderRDB.Checked = false;
                }
                else
                {
                    checkPhoneOrderRDB.Enabled = true;
                }

                cmpsrProductRDB.Enabled = false;
                cmpsrClientRDB.Enabled = false;

                buttonEditRDB.Text = "&Done";
                buttonAddRDB.Enabled = false;
            }
            else if (buttonEditRDB.Text == "&Done")
            {
                //20150505, liliana, LIBST13020, end
                if (MessageBox.Show("Apakah akan merubah transaksi Trancode " + textNoTransaksiRDB.Text + "?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
                else
                {
                    //20150629, liliana, LIBST13020, begin
                    if (_StatusTransaksiRDB == "Rejected")
                    {
                        MessageBox.Show("Transaksi dengan Status Rejected tidak dapat diedit!", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150629, liliana, LIBST13020, end
                    //20150916, liliana, LIBST13020, begin
                    else if (_StatusTransaksiRDB == "Reversed")
                    {
                        MessageBox.Show("Transaksi dengan Status Reversed tidak dapat diedit!", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else if (_StatusTransaksiRDB == "Cancel By PO")
                    {
                        MessageBox.Show("Transaksi dengan Status Cancel By PO tidak dapat diedit!", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150916, liliana, LIBST13020, end
                    //20150505, liliana, LIBST13020, begin
                    if (cmpsrProductRDB.Text1 == "")
                    {
                        MessageBox.Show("Kode Produk harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (cmpsrCurrRDB.Text1 == "")
                    {
                        MessageBox.Show("Mata Uang Produk harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (nispMoneyNomRDB.Value == 0)
                    {
                        MessageBox.Show("Nominal harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (nispJangkaWktRDB.Value == 0)
                    {
                        MessageBox.Show("Jangka Waktu harus diisi", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (cmbFrekPendebetanRDB.Text == "")
                    {
                        MessageBox.Show("Frekuensi Pendebetan harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    //20200408, Lita, RDN-88, begin
                    if (cmbFrekDebetMethodRDB.Text == "")
                    {
                        MessageBox.Show("Metode Frekuensi Pendebetan harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20200408, Lita, RDN-88, end

                    if (cmbAutoRedempRDB.Text == "")
                    {
                        MessageBox.Show("Auto Redemption harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (cmbAsuransiRDB.Text == "")
                    {
                        MessageBox.Show("Asuransi harus dipilih", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    //20150505, liliana, LIBST13020, end
                    string strData = String.Format("NoTrx='{0}'", textNoTransaksiRDB.Text);

                    //20240220, gio, RDN-1108, begin
                    if (dateTglTransaksiRDB.Value > DateTime.Today.AddHours(23))
                    {
                        MessageBox.Show("Tanggal instruksi tidak lebih dari tanggal hari ini", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (dateTglTransaksiRDB.Value < DateTime.Today)
                    {
                        MessageBox.Show("Tanggal instruksi tidak kurang dari tanggal hari ini", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (dateTglTransaksiRDB.Value == DateTime.Today)
                    {
                        MessageBox.Show("Waktu instruksi tidak dapat diisi 00.00.00. Mohon untuk menginput waktu instruksi transaksi sesuai informasi Nasabah", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (dateTglTransaksiRDB.Value > DateTime.Today.AddHours(13))
                    {
                        MessageBox.Show("Waktu instruksi diatas 13.00.00 menggunakan NAV berikutnya", "Transaksi Reksadana Berjangka", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    //20240220, gio, RDN-1108, end
                    try
                    {
                        if (cTransaksi.dttSubsRDB.Select(strData).Length > 0)
                        {
                            //20150819, liliana, LIBST13020, begin
                            string strTranCode, strNewClientCode;
                            int _FrekPendebetan;
                            int.TryParse(cmbFrekPendebetanRDB.Text, out _FrekPendebetan);
                            //20150825, liliana, LIBST13020, begin
                            string strWarnMsg = "";
                            string strWarnMsg2 = "";
                            //20150825, liliana, LIBST13020, end

                            //20150827, liliana, LIBST13020, begin
                            //GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductRDB.Text1,
                            bool _result = false;

                            _result = GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductRDB.Text1,
                                //20150827, liliana, LIBST13020, end
                                cmpsrClientRDB.Text1, out strTranCode, out strNewClientCode, cmpsrCIFRDB.Text1,
                                checkFeeEditRDB.Checked, nispPercentageFeeRDB.Value, 0
                                , false, (double)nispMoneyFeeRDB.Value, (double)nispMoneyNomRDB.Value, 0, false, _FrekPendebetan, (int)nispJangkaWktRDB.Value
                                , _intType
                                //20150825, liliana, LIBST13020, begin
                                , out strWarnMsg
                                , out strWarnMsg2
                                //20150825, liliana, LIBST13020, end
                                //20160829, liliana, LOGEN00196, begin
                                , cmbTARDB.SelectedIndex
                                //20160829, liliana, LOGEN00196, end
                                //20220520, Lita, RDN-781, begin
                                , cmbFrekDebetMethodRDB.SelectedValue.ToString(), cmbAsuransiRDB.Text, dtTglDebetRDB.Value
                                //20220520, Lita, RDN-781, end
                                );

                            //20150819, liliana, LIBST13020, end
                            //20150827, liliana, LIBST13020, begin
                            if (!_result)
                            {
                                return;
                            }
                            //20150827, liliana, LIBST13020, end
                            //20150825, liliana, LIBST13020, begin
                            if (strWarnMsg2 != "")
                            {
                                if (MessageBox.Show(strWarnMsg2, "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                {
                                    MessageBox.Show("Proses transaksi dibatalkan.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }
                            //20150825, liliana, LIBST13020, end
                            DataRow[] dtrSubsRDB = cTransaksi.dttSubsRDB.Select(strData);

                            dtrSubsRDB[0]["KodeProduk"] = cmpsrProductRDB.Text1;
                            //20150617, liliana, LIBST13020, begin
                            dtrSubsRDB[0]["NamaProduk"] = cmpsrProductRDB.Text2;
                            //20150630, liliana, LIBST13020, begin
                            //dtrSubsRDB[0]["EditFeeBy"] = _ComboJenisRDB.Text;
                            if (checkFeeEditRDB.Checked)
                            {
                                dtrSubsRDB[0]["EditFeeBy"] = _ComboJenisRDB.Text;
                            }
                            else
                            {
                                dtrSubsRDB[0]["EditFeeBy"] = "";
                            }
                            //20150630, liliana, LIBST13020, end
                            //20150617, liliana, LIBST13020, end
                            dtrSubsRDB[0]["ClientCode"] = cmpsrClientRDB.Text1;
                            dtrSubsRDB[0]["CCY"] = cmpsrCurrRDB.Text1;
                            dtrSubsRDB[0]["Nominal"] = nispMoneyNomRDB.Value;
                            dtrSubsRDB[0]["PhoneOrder"] = checkPhoneOrderRDB.Checked;
                            dtrSubsRDB[0]["AutoRedemption"] = cmbAutoRedempRDB.Text;
                            dtrSubsRDB[0]["Asuransi"] = cmbAsuransiRDB.Text;
                            dtrSubsRDB[0]["EditFee"] = checkFeeEditRDB.Checked;
                            dtrSubsRDB[0]["JenisFee"] = _ComboJenisRDB.SelectedIndex;
                            dtrSubsRDB[0]["NominalFee"] = nispMoneyFeeRDB.Value;
                            dtrSubsRDB[0]["FeeCurr"] = labelFeeCurrencyRDB.Text;
                            dtrSubsRDB[0]["PctFee"] = nispPercentageFeeRDB.Value;
                            dtrSubsRDB[0]["FeeKet"] = _KeteranganFeeRDB.Text;
                            //20150505, liliana, LIBST13020, begin
                            dtrSubsRDB[0]["JangkaWaktu"] = nispJangkaWktRDB.Value;
                            dtrSubsRDB[0]["JatuhTempo"] = globalJatuhTempoRDB;
                            dtrSubsRDB[0]["FrekPendebetan"] = cmbFrekPendebetanRDB.Text;
                            //20150505, liliana, LIBST13020, end

                            //20200408, Lita, RDN-88, begin
                            dtrSubsRDB[0]["FrekDebetMethod"] = cmbFrekDebetMethodRDB.Text;
                            dtrSubsRDB[0]["FrekDebetMethodValue"] = cmbFrekDebetMethodRDB.SelectedValue.ToString();
                            dtrSubsRDB[0]["TanggalDebet"] = dtTglDebetRDB.Value;
                            //20200408, Lita, RDN-88, begin

                            dtrSubsRDB[0]["ApaDiUpdate"] = true;
                            //20160829, liliana, LOGEN00196, begin
                            dtrSubsRDB[0]["TrxTaxAmnesty"] = cmbTARDB.SelectedIndex;
                            //20160829, liliana, LOGEN00196, end

                            //20210922, korvi, RDN-674, begin
                            dtrSubsRDB[0]["SelectedAccNo"] = cmpsrNoRekRDB.Text1;
                            //20210922, korvi, RDN-674, end

                            cTransaksi.dttSubsRDB.AcceptChanges();

                            dataGridViewRDB.DataSource = cTransaksi.dttSubsRDB;

                            for (int i = 0; i < dataGridViewRDB.Columns.Count; i++)
                            {
                                if (dataGridViewRDB.Columns[i].ValueType.ToString() == "System.Decimal")
                                {
                                    //20220802, Lita, RDN-825, begin
                                    //dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N2";
                                    if (dataGridViewRDB.Columns[i].Name == "PctFee")
                                        dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N4";
                                    else
                                        dataGridViewRDB.Columns[i].DefaultCellStyle.Format = "N2";
                                    //20220802, Lita, RDN-825, end

                                }
                            }

                            dataGridViewRDB.AutoResizeColumns();
                            subSetVisibleGrid(_strTabName);
                            ResetFormTrxSubsRDB();
                            //20150505, liliana, LIBST13020, begin
                            //20150810, liliana, LIBSTT13020, begin
                            //DisableFormTrxRDB(false);
                            //buttonEditRDB.Enabled = true;
                            //buttonAddRDB.Enabled = true;
                            if (_intType == 1)
                            {
                                DisableFormTrxRDB(true);
                                buttonEditRDB.Enabled = true;
                                buttonAddRDB.Enabled = true;
                            }
                            else if (_intType == 2)
                            {
                                DisableFormTrxRDB(false);
                                buttonEditRDB.Enabled = true;
                                buttonAddRDB.Enabled = false;
                            }
                            //20150810, liliana, LIBSTT13020, end
                            //20150505, liliana, LIBST13020, end
                        }
                        else
                        {
                            MessageBox.Show("Data tidak ditemukan", "Transaksi Redemption ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Transaksi Redemption");
                        return;
                    }
                }
                //20150505, liliana, LIBST13020, begin
            }
            //20150505, liliana, LIBST13020, end
        }

        private void dataGridViewRDB_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (buttonEditRDB.Text == "&Done")
            {
                try
                {
                    int intComboBox;

                    //20160829, liliana, LOGEN00196, begin
                    bool bIsTA; int iIsTA;
                    iIsTA = -1;
                    bIsTA = (bool)dataGridViewRDB["TrxTaxAmnesty", e.RowIndex].Value;

                    if (bIsTA)
                    {
                        iIsTA = 1;
                    }
                    else
                    {
                        iIsTA = 0;
                    }

                    cmbTARDB.SelectedIndex = iIsTA;
                    //20160829, liliana, LOGEN00196, end
                    //20150629, liliana, LIBST13020, begin
                    _StatusTransaksiRDB = dataGridViewRDB["StatusTransaksi", e.RowIndex].Value.ToString();
                    //20150629, liliana, LIBST13020, end
                    textNoTransaksiRDB.Text = dataGridViewRDB["NoTrx", e.RowIndex].Value.ToString();
                    dateTglTransaksiRDB.Value = (DateTime)dataGridViewRDB["TglTrx", e.RowIndex].Value;

                    cmpsrProductRDB.Text1 = dataGridViewRDB["KodeProduk", e.RowIndex].Value.ToString();
                    cmpsrProductRDB.ValidateField();

                    cmpsrCurrRDB.Text1 = dataGridViewRDB["CCY", e.RowIndex].Value.ToString();
                    cmpsrCurrRDB.ValidateField();

                    nispMoneyNomRDB.Value = (decimal)dataGridViewRDB["Nominal", e.RowIndex].Value;
                    checkPhoneOrderRDB.Checked = (bool)dataGridViewRDB["PhoneOrder", e.RowIndex].Value;
                    checkFeeEditRDB.Checked = (bool)dataGridViewRDB["EditFee", e.RowIndex].Value;

                    int.TryParse(dataGridViewRDB["JenisFee", e.RowIndex].Value.ToString(), out intComboBox);
                    _ComboJenisRDB.SelectedIndex = intComboBox;

                    nispMoneyFeeRDB.Value = (decimal)dataGridViewRDB["NominalFee", e.RowIndex].Value;
                    nispPercentageFeeRDB.Value = (decimal)dataGridViewRDB["PctFee", e.RowIndex].Value;

                    labelFeeCurrencyRDB.Text = dataGridViewRDB["FeeCurr", e.RowIndex].Value.ToString();
                    _KeteranganFeeRDB.Text = dataGridViewRDB["FeeKet", e.RowIndex].Value.ToString();

                    nispJangkaWktRDB.Value = Convert.ToInt32(dataGridViewRDB["JangkaWaktu", e.RowIndex].Value.ToString());
                    cmbAutoRedempRDB.Text = dataGridViewRDB["AutoRedemption", e.RowIndex].Value.ToString();
                    cmbAsuransiRDB.Text = dataGridViewRDB["Asuransi", e.RowIndex].Value.ToString();
                    DateTime JatuhTempoRDB = (DateTime)dataGridViewRDB["JatuhTempo", e.RowIndex].Value;
                    dtJatuhTempoRDB.Value = Convert.ToInt32(JatuhTempoRDB.ToString("yyyyMMdd"));
                    cmbFrekPendebetanRDB.Text = dataGridViewRDB["FrekPendebetan", e.RowIndex].Value.ToString();
                    //20200408, Lita, RDN-88, begin
                    cmbFrekDebetMethodRDB.Text = dataGridViewRDB["FrekDebetMethod", e.RowIndex].Value.ToString();
                    dtTglDebetRDB.Value = (DateTime)dataGridViewRDB["TanggalDebet", e.RowIndex].Value;
                    //20200408, Lita, RDN-88, end

                    cmpsrClientRDB.Text1 = dataGridViewRDB["ClientCode", e.RowIndex].Value.ToString();
                    //20150619, liliana, LIBST13020, begin
                    if (_intType == 2)
                    {
                        cmpsrClientRDB.ValidateField();
                    }
                    //20150619, liliana, LIBST13020, end

                    //20210922, korvi, RDN-674, begin
                    cmpsrNoRekRDB.Criteria = cmpsrCIFRDB.Text1 + "#" + cmbTARDB.SelectedIndex.ToString();
                    cmpsrNoRekRDB.Text1 = dataGridViewRDB["SelectedAccNo", e.RowIndex].Value.ToString();
                    cmpsrNoRekRDB.Text2 = dataGridViewRDB["TranCCY", e.RowIndex].Value.ToString();

                    //20220119, sandi, RDN-727, begin
                    //cmpsrNoRekRDB.ValidateField();
                    //20220119, sandi, RDN-727, end
                    //20210922, korvi, RDN-674, end
                }
                catch
                {
                    return;
                }
            }
        }

        //20161108, liliana, CSODD16311, begin
        //private bool CheckClientCodeTA(string ClientCode)
        //{
        //    bool isRedempAll = false;
        //    string IsTA, IsExpired;

        //    System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

        //    dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcClientCode", System.Data.OleDb.OleDbType.VarChar, 20);
        //    dbParam[0].Value = ClientCode;

        //    dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcIsTA", System.Data.OleDb.OleDbType.VarChar, 1);
        //    dbParam[1].Value = "";
        //    dbParam[1].Direction = ParameterDirection.InputOutput;

        //    dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcIsExpired", System.Data.OleDb.OleDbType.VarChar, 1);
        //    dbParam[2].Value = "";
        //    dbParam[2].Direction = ParameterDirection.InputOutput;

        //    bool blnResult = ClQ.ExecProc("ReksaCheckingClientCodeTA", ref dbParam);

        //    if (blnResult)
        //    {
        //        IsTA = dbParam[1].Value.ToString();
        //        IsExpired = dbParam[2].Value.ToString();

        //        if ((IsTA == "1") && (IsExpired == "1"))
        //        {
        //            isRedempAll = true;
        //        }
        //    }

        //    return isRedempAll;
        //}

        //20161108, liliana, CSODD16311, end
        private void cmpsrClientRedemp_onNispText2Changed(object sender, EventArgs e)
        {
            //20150826, liliana, LIBST13020, begin
            //20150826, liliana, LIBST13020, begin
            //checkAll.Checked = false;
            //checkAll.Enabled = true;
            //nispRedempUnit.Enabled = true;
            //nispRedempUnit.Value = 0;
            //nispRedempUnit.Text = "";
            //20150826, liliana, LIBST13020, end
            //20150826, liliana, LIBST13020, end
            //tampilin oustanding unit
            decimal OutstandingBalance;
            int intClientCode;
            int.TryParse(cmpsrClientRedemp[2].ToString(), out intClientCode);
            OutstandingBalance = GetLatestBalance(intClientCode);
            nispOutstandingUnitRedemp.Value = OutstandingBalance;
            //20150622, liliana, LIBST13020, begin
            //20150630, liliana, LIBST13020, begin
            //cmpsrProductRedemp.ValidateField();
            //cmpsrClientRedemp.ValidateField();
            //20150630, liliana, LIBST13020, end

            //20150827, liliana, LIBST13020, begin
            //if (cmpsrClientRedemp[6].ToString() == "Y")
            if ((cmpsrClientRedemp[6].ToString() == "Y") && (_intType == 1))
            //20150827, liliana, LIBST13020, end
            {
                //20150826, liliana, LIBST13020, begin
                //checkAll.Checked = true;
                //nispRedempUnit.Value = nispOutstandingUnitRedemp.Value;

                //checkAll.Enabled = false;
                //nispRedempUnit.Enabled = false;
                //IsRedempAll = true;
                //try
                //{
                //    decimal decNominalFee, decPctFee;
                //    int intProdId, intClientid, intTranType;
                //    int.TryParse(cmpsrProductRedemp[2].ToString(), out intProdId);
                //    int.TryParse(cmpsrClientRedemp[2].ToString(), out intClientid);

                //    if ((_ComboJenisRedemp.Text == "By %") && (checkFeeEditRedemp.Checked))
                //    {
                //        ByPercent = true;
                //    }
                //    else
                //    {
                //        ByPercent = false;
                //    }


                //    if (IsRedempAll)
                //    {
                //        intTranType = 4;
                //    }
                //    else
                //    {
                //        intTranType = 3;
                //    }

                //    HitungFee(intProdId, intClientid, intTranType, 0, nispRedempUnit.Value, false,
                //        checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, 1, out strFeeCurr,
                //        out decNominalFee, out decPctFee, cmpsrCIFRedemp.Text1);

                //    nispMoneyFeeRedemp.Value = decPctFee;
                //    nispPercentageFeeRedemp.Value = decNominalFee;
                //    labelFeeCurrencyRedemp.Text = "%";
                //    _KeteranganFeeRedemp.Text = strFeeCurr;
                //}
                //catch
                //{
                //    return;
                //}
                DataSet dsRDBSwitchOut;
                dsRDBSwitchOut = GetDataRDB(cmpsrClientRedemp.Text1);

                if (dsRDBSwitchOut.Tables[0].Rows.Count > 0)
                {
                    decimal decJangkaWaktu;
                    decimal.TryParse(dsRDBSwitchOut.Tables[0].Rows[0]["SisaJangkaWaktu"].ToString(), out decJangkaWaktu);
                    DateTime dtJatuhTempo;
                    DateTime.TryParse(dsRDBSwitchOut.Tables[0].Rows[0]["JatuhTempo"].ToString(), out dtJatuhTempo);
                    string DoneDebet;
                    DoneDebet = dsRDBSwitchOut.Tables[0].Rows[0]["IsDoneDebet"].ToString();

                    //20150827, liliana, LIBST13020, begin
                    //if ((decJangkaWaktu == 0) || (DoneDebet == "1"))
                    if (decJangkaWaktu > 0)
                    //20150827, liliana, LIBST13020, end
                    {
                        checkAll.Checked = true;
                        checkAll.Enabled = false;
                        nispRedempUnit.Enabled = false;
                        IsRedempAll = true;
                        //20150827, liliana, LIBST13020, begin
                        checkFeeEditRedemp.Enabled = false;
                        //20150827, liliana, LIBST13020, end
                    }
                    else
                    {
                        checkAll.Checked = false;
                        checkAll.Enabled = true;
                        nispRedempUnit.Enabled = true;
                        //20150827, liliana, LIBST13020, begin
                        checkFeeEditRedemp.Enabled = false;
                        //20150827, liliana, LIBST13020, end
                    }
                }
                //20150826, liliana, LIBST13020, end

            }
            //20150831, liliana, LIBST13020, begin
            else if ((cmpsrClientRedemp[6].ToString() == "Y") && (_intType == 2))
            {
                //20151008, liliana, LIBST13020, begin
                //checkAll.Checked = false;
                //20151008, liliana, LIBST13020, end
                checkAll.Enabled = false;
                nispRedempUnit.Enabled = false;
                checkFeeEditRedemp.Enabled = false;
            }
            else if ((cmpsrClientRedemp[6].ToString() == "N") && (_intType == 1))
            {
                //20151008, liliana, LIBST13020, begin
                //checkAll.Checked = true;
                //20151008, liliana, LIBST13020, end
                checkAll.Enabled = true;
                nispRedempUnit.Enabled = true;
                checkFeeEditRedemp.Enabled = true;
            }
            else if ((cmpsrClientRedemp[6].ToString() == "N") && (_intType == 2))
            {
                //20151008, liliana, LIBST13020, begin
                //checkAll.Checked = false;
                //20151008, liliana, LIBST13020, end
                checkAll.Enabled = false;
                nispRedempUnit.Enabled = false;
                checkFeeEditRedemp.Enabled = true;
            }
            //20150831, liliana, LIBST13020, end
            //20150622, liliana, LIBST13020, end
            //20170210, liliana, COPOD17019, begin

            //if (cmbTARedemp.SelectedIndex == 1)
            //{
            //    string strIsAllowed, strErrorMessage;
            //    if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIFRedemp.Text1, out strIsAllowed, out strErrorMessage))
            //    {
            //        if (strIsAllowed == "3")
            //        {
            //            checkAll.Checked = true;
            //            checkAll.Enabled = false;
            //            nispRedempUnit.Enabled = false;
            //            IsRedempAll = true;
            //        }
            //    }
            //}

            //20170210, liliana, COPOD17019, end
        }

        private decimal GetLatestBalance(int intClientId)
        {
            decimal decUnitBalance = 0;
            DataSet ds;
            OleDbParameter[] pr = new OleDbParameter[4];

            (pr[0] = new OleDbParameter("@pnClientId", OleDbType.Integer)).Value = intClientId;
            (pr[1] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
            (pr[2] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
            (pr[3] = new OleDbParameter("@pmUnitBalance", OleDbType.Double)).Value = 0;

            pr[3].Direction = ParameterDirection.Output;

            bool blnResult = ClQ.ExecProc("dbo.ReksaGetLatestBalance", ref pr, out ds);

            if (blnResult)
            {
                decUnitBalance = System.Convert.ToDecimal(pr[3].Value);
            }

            return decUnitBalance;
        }

        private void nispJangkaWktRDB_onNispMoneyValueChanged(object sender, EventArgs e)
        {
            if (nispJangkaWktRDB.Value == 0)
                dtJatuhTempoRDB.Value = 0;
            else
            {
                try
                {
                    //20150710, liliana, LIBST13020, begin
                    //if (_intType == 1)
                    if (_intType != 0)
                    //20150710, liliana, LIBST13020, end
                    {
                        //20200421, Lita, RDN-88, begin
                        //DateTime JoinDate = DateTime.Parse(ProReksa2.Global.strCurrentDate.ToString());
                        //20221220, Lita, RDN-885, Begin
                        DateTime JoinDate = DateTime.Parse(dtTglDebetRDB.Value.ToString());
                        //20221220, Lita, RDN-885, end

                        //20200421, Lita, RDN-88, end
                        globalJatuhTempoRDB = JoinDate.AddMonths((int)nispJangkaWktRDB.Value);
                        dtJatuhTempoRDB.Value = Convert.ToInt32(JoinDate.AddMonths((int)nispJangkaWktRDB.Value).ToString("yyyyMMdd"));
                    }
                }
                catch
                {
                    dtJatuhTempoRDB.Value = 0;
                }
            }
        }

        private void _ComboJenisRDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            nispMoneyFeeRDB.Value = 0;
            nispPercentageFeeRDB.Value = 0;

            if (_ComboJenisRDB.Text == "By %")
            {
                _KeteranganFeeRDB.Text = cmpsrCurrRDB.Text1;
                labelFeeCurrencyRDB.Text = "%";
                ByPercent = true;
                //20220802, Lita, RDN-825, begin
                //nispMoneyFeeRDB.DecimalPlace = 3;
                nispPercentageFeeRDB.DecimalPlace = 4;
                //20220802, Lita, RDN-825, end
                //20150622, liliana, LIBST13020, begin
                nispPercentageFeeRDB.DecimalPlace = 2;
                //20150622, liliana, LIBST13020, end
            }
            else
            {
                _KeteranganFeeRDB.Text = "%";
                labelFeeCurrencyRDB.Text = cmpsrCurrRDB.Text1;
                ByPercent = false;
                nispMoneyFeeRDB.DecimalPlace = 2;
                //20150622, liliana, LIBST13020, begin
                //20220802, Lita, RDN-825, begin
                //nispPercentageFeeRDB.DecimalPlace = 3;
                nispPercentageFeeRDB.DecimalPlace = 4;
                //20220802, Lita, RDN-825, end
                //20150622, liliana, LIBST13020, end
            }
        }

        private void nispMoneyFeeRDB_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            if (_ComboJenisRDB.Text == "By %")
            {
                _KeteranganFeeRDB.Text = cmpsrCurrRDB.Text1;
                labelFeeCurrencyRDB.Text = "%";
                ByPercent = true;
            }
            else
            {
                _KeteranganFeeRDB.Text = "%";
                labelFeeCurrencyRDB.Text = cmpsrCurrRDB.Text1;
                ByPercent = false;
            }

            if ((cmpsrProductRDB.Text1 != "") && (nispMoneyNomRDB.Value != 0))
            {
                decimal decNominalFee, decPctFee;
                int intProdId, intClientid, intTranType;
                int.TryParse(cmpsrProductRDB[2].ToString(), out intProdId);

                intTranType = 5;

                HitungFee(intProdId, 0, intTranType, nispMoneyNomRDB.Value, 0, true,
                    checkFeeEditRDB.Checked, nispMoneyFeeRDB.Value, 2, out strFeeCurr,
                    out decNominalFee, out decPctFee, cmpsrCIFRDB.Text1);

                nispPercentageFeeRDB.Value = decPctFee;

            }
        }

        private void nispRedempUnit_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            nispMoneyFeeRedemp.Value = 0;
            nispPercentageFeeRedemp.Value = 0;
            checkFeeEditRedemp.Checked = false;
            //20150424, liliana, LIBST13020, begin
            //_ComboJenisRedemp.SelectedIndex = 0;
            _ComboJenisRedemp.SelectedIndex = 1;

            if (checkFeeEditRedemp.Checked == false)
            {
                _ComboJenisRedemp.Enabled = false;
                nispMoneyFeeRedemp.Enabled = false;
            }
            //20150424, liliana, LIBST13020, end
            _KeteranganFeeRedemp.Text = "";
            labelFeeCurrencyRedemp.Text = "";
            //20150818, liliana, LIBST13020, begin

            try
            {
                if ((nispRedempUnit.Value != 0) && (cmpsrProductRedemp.Text1 != "") &&
                    (cmpsrClientRedemp.Text1 != "") && (nispOutstandingUnitRedemp.Value != 0)
                    )
                {
                    //20150828, liliana, LIBST13020, begin
                    //cmpsrProductRedemp.ValidateField();
                    //20150828, liliana, LIBST13020, end
                    //20150826, liliana, LIBST13020, begin
                    //cmpsrClientRedemp.ValidateField();
                    //20150826, liliana, LIBST13020, end

                    decimal decNominalFee, decPctFee;
                    int intProdId, intClientid, intTranType;
                    //20150828, liliana, LIBST13020, begin
                    //int.TryParse(cmpsrProductRedemp[2].ToString(), out intProdId);
                    //int.TryParse(cmpsrClientRedemp[2].ToString(), out intClientid);
                    string strPrdId = GetImportantData("PRODUKID", cmpsrProductRedemp.Text1);
                    int.TryParse(strPrdId, out intProdId);

                    string strClientId = GetImportantData("CLIENTID", cmpsrClientRedemp.Text1);
                    int.TryParse(strClientId, out intClientid);
                    //20150828, liliana, LIBST13020, end

                    if (nispRedempUnit.Value == nispOutstandingUnitRedemp.Value)
                    {
                        IsRedempAll = true;
                        checkAll.Checked = true;
                    }
                    else
                    {
                        IsRedempAll = false;
                        checkAll.Checked = false;
                    }

                    if ((_ComboJenisRedemp.Text == "By %") && (checkFeeEditRedemp.Checked))
                    {
                        ByPercent = true;
                    }
                    else
                    {
                        ByPercent = false;
                    }


                    if (IsRedempAll)
                    {
                        intTranType = 4;
                    }
                    else
                    {
                        intTranType = 3;
                    }

                    HitungFee(intProdId, intClientid, intTranType, 0, nispRedempUnit.Value, false,
                        checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, 1, out strFeeCurr,
                        out decNominalFee, out decPctFee, cmpsrCIFRedemp.Text1);

                    nispMoneyFeeRedemp.Value = decPctFee;
                    nispPercentageFeeRedemp.Value = decNominalFee;
                    labelFeeCurrencyRedemp.Text = "%";
                    _KeteranganFeeRedemp.Text = strFeeCurr;
                }
            }
            catch
            {
                return;
            }
            //20150818, liliana, LIBST13020, end
        }

        private void nispMoneyNomRDB_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            nispMoneyFeeRDB.Value = 0;
            nispPercentageFeeRDB.Value = 0;
            checkFeeEditRDB.Checked = false;
            _ComboJenisRDB.SelectedIndex = 0;
            //20150505, liliana, LIBST13020, begin
            if (checkFeeEditRDB.Checked == false)
            {
                _ComboJenisRDB.Enabled = false;
                nispMoneyFeeRDB.Enabled = false;
            }
            //20150505, liliana, LIBST13020, end
            _KeteranganFeeRDB.Text = "";
            labelFeeCurrencyRDB.Text = "";
            //20150818, liliana, LIBST13020, begin

            try
            {
                if ((nispMoneyNomRDB.Value != 0) && (cmpsrProductRDB.Text1 != "")
                    )
                {
                    decimal decNominalFee, decPctFee;
                    int intProdId, intClientid, intTranType;
                    cmpsrProductRDB.ValidateField();

                    int.TryParse(cmpsrProductRDB[2].ToString(), out intProdId);

                    intTranType = 5;

                    HitungFee(intProdId, 0, intTranType, nispMoneyNomRDB.Value, 0, true,
                        checkFeeEditRDB.Checked, nispPercentageFeeRDB.Value, 1, out strFeeCurr, out decNominalFee,
                        out decPctFee, cmpsrCIFRDB.Text1);

                    nispMoneyFeeRDB.Value = decNominalFee;
                    nispPercentageFeeRDB.Value = decPctFee;
                    labelFeeCurrencyRDB.Text = strFeeCurr;
                }
            }
            catch
            {
                return;
            }
            //20150818, liliana, LIBST13020, end
        }

        private void nispMoneyNomRDB_Leave(object sender, EventArgs e)
        {
            try
            {
                decimal decNominalFee, decPctFee;
                int intProdId, intClientid, intTranType;
                int.TryParse(cmpsrProductRDB[2].ToString(), out intProdId);

                intTranType = 5;

                HitungFee(intProdId, 0, intTranType, nispMoneyNomRDB.Value, 0, true,
                    checkFeeEditRDB.Checked, nispPercentageFeeRDB.Value, 1, out strFeeCurr, out decNominalFee,
                    out decPctFee, cmpsrCIFRDB.Text1);

                nispMoneyFeeRDB.Value = decNominalFee;
                nispPercentageFeeRDB.Value = decPctFee;
                labelFeeCurrencyRDB.Text = strFeeCurr;
            }
            catch
            {
                return;
            }
        }

        private void cmbAutoRedempRDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            //20150619, liliana, LIBST13020, begin
            //20150505, liliana, LIBST13020, begin
            //if (cmbAutoRedempRDB.Text == "YA")
            //if ((cmbAutoRedempRDB.Text == "YA") && (cmbAutoRedempRDB.Enabled = true))
            ////20150505, liliana, LIBST13020, end
            //{
            //    if (MessageBox.Show("Apakah yakin ingin memilih Auto Redemption?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            //    {
            //        cmbAutoRedempRDB.SelectedIndex = 1;
            //        return;
            //    }
            //}
            //20150619, liliana, LIBST13020, end
        }

        private void cmpsrProductSwcOut_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductSwcOut.Text2 = "";
        }

        private void cmpsrProductSwcOut_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSwcOut.Criteria = cmpsrCIFSwc.Text1 + "#" + cmpsrProductSwcOut.Text1 + "#" + _strTabName;
                cmpsrClientSwcOut.Criteria = cmpsrCIFSwc.Text1 + "#" + cmpsrProductSwcOut.Text1 + "#" + _strTabName
                                            + "#" + cmbTASwc.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                cmpsrProductSwcIn.Criteria = cmpsrProductSwcOut.Text1.Trim();

                cmpsrProductSwcIn.Text1 = "";
                cmpsrProductSwcIn.ValidateField();
                cmpsrClientSwcIn.Text1 = "";
                cmpsrClientSwcIn.ValidateField();

                //20150408, liliana, LIBST13020, begin
                _NAVSwcOutNonRDB = ReksaGetLatestNAV(cmpsrProductSwcOut[2].ToString());
                //20150408, liliana, LIBST13020, end
            }
            catch
            {
                return;
            }
        }

        //20150408, liliana, LIBST13020, begin
        private decimal ReksaGetLatestNAV(string ProdId)
        {

            int intProdId;
            int.TryParse(ProdId, out intProdId);
            decimal NAV = 0;

            DataSet dsQueryResult;
            OleDbParameter[] dbParam = new OleDbParameter[4];

            try
            {
                (dbParam[0] = new OleDbParameter("@pnProdId", OleDbType.Integer)).Value = intProdId;
                (dbParam[1] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                (dbParam[2] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
                (dbParam[3] = new OleDbParameter("@pmNAV", OleDbType.Double)).Value = 0;

                dbParam[3].Direction = ParameterDirection.Output;

                bool blnResult = ClQ.ExecProc("dbo.ReksaGetLatestNAV", ref dbParam, out dsQueryResult);

                if (blnResult)
                {
                    NAV = System.Convert.ToDecimal(dbParam[3].Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return NAV;
        }
        //20150408, liliana, LIBST13020, end

        private void cmpsrProductSwcIn_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductSwcIn.Text2 = "";

            cmpsrClientSwcIn.Text1 = "";
            cmpsrClientSwcIn.Text2 = "";
            cmpsrClientSwcIn.Enabled = false;

        }

        private void cmpsrProductSwcIn_onNispText2Changed(object sender, EventArgs e)
        {
            //20160829, liliana, LOGEN00196, begin
            if (cmbTASwc.SelectedIndex == -1)
            {
                //20220222, gio, RDN736, begin
                //MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //20220222, gio, RDN736, end

                cmpsrProductSwcIn.Text1 = "";
                cmpsrProductSwcIn.Text2 = "";
                return;
            }

            //20160829, liliana, LOGEN00196, end
            if (cmpsrProductSwcIn.Text1 != "")
            {
                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSwcIn.Criteria = cmpsrProductSwcIn[2].ToString() + "#" + cmpsrCIFSwc.Text1.Trim();
                cmpsrClientSwcIn.Criteria = cmpsrProductSwcIn[2].ToString() + "#" + cmpsrCIFSwc.Text1.Trim()
                                            + "#" + cmbTASwc.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                //cek subs new / subs add
                //20150408, liliana, LIBST13020, begin

                _NAVSwcInNonRDB = ReksaGetLatestNAV(cmpsrProductSwcIn[2].ToString());

                if (_intType == 1)
                {
                    //20150408, liliana, LIBST13020, end
                    int intProductId;
                    int.TryParse(cmpsrProductSwcIn[2].ToString(), out intProductId);

                    //20150617, liliana, LIBST13020, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSwc.Text1.Trim(), intProductId, false);
                    string ClientCodeSwcIn;
                    ClientCodeSwcIn = "";

                    //20160829, liliana, LOGEN00196, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSwc.Text1.Trim(), intProductId, false, out ClientCodeSwcIn);
                    IsSubsNew = CheckIsSubsNew(cmpsrCIFSwc.Text1.Trim(), intProductId, false, out ClientCodeSwcIn
                        , cmbTASwc.SelectedIndex
                        );
                    //20160829, liliana, LOGEN00196, end
                    //20150617, liliana, LIBST13020, end

                    if (IsSubsNew)
                    {
                        cmpsrClientSwcIn.Enabled = false;

                        string strTranCode, strNewClientCode;

                        //20150812, liliana, LIBST13020, begin
                        //GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductSwcIn.Text1,
                        //    //20150505, liliana, LIBST13020, begin
                        //    //cmpsrClientSwcIn.Text1, out strTranCode, out strNewClientCode, cmpsrCIFSwc.Text1);
                        //    cmpsrClientSwcIn.Text1, out strTranCode, out strNewClientCode, cmpsrCIFSwc.Text1,
                        //    checkFeeEditSwc.Checked, nispPercentageFeeSwc.Value, 0
                        //    //20150610, liliana, LIBST13020, begin
                        //    , false, (double)nispMoneyFeeSwc.Value, 0, (double)nispRedempSwc.Value, IsSwitchingAll, 0, 0
                        //    //20150610, liliana, LIBST13020, end
                        //    );
                        ////20150505, liliana, LIBST13020, end


                        //cmpsrClientSwcIn.Text1 = strNewClientCode;
                        //cmpsrClientSwcIn.Text2 = cmpsrCIFSwc.Text2;
                        //20150812, liliana, LIBST13020, end

                    }
                    else
                    {
                        //20150617, liliana, LIBST13020, begin
                        //cmpsrClientSwcIn.Enabled = true;
                        cmpsrClientSwcIn.Enabled = false;
                        cmpsrClientSwcIn.Text1 = ClientCodeSwcIn;
                        cmpsrClientSwcIn.ValidateField();
                        //20150617, liliana, LIBST13020, end
                    }
                    //20150408, liliana, LIBST13020, begin
                }
                //20150408, liliana, LIBST13020, end
                //20150915, liliana, LIBST13020, begin

                try
                {
                    string FeeCCY;
                    decimal decNominalFee, decPctFee;

                    HitungSwitchingFee(cmpsrProductSwcOut.Text1, cmpsrProductSwcIn.Text1, true, 0,
                        nispRedempSwc.Value, checkFeeEditSwc.Checked, nispPercentageFeeSwc.Value,
                        //20210309, joshua, RDN-466, begin
                        //cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee);
                        cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwc.Text1);
                    //20210309, joshua, RDN-466, end

                    nispMoneyFeeSwc.Value = decNominalFee;
                    nispPercentageFeeSwc.Value = decPctFee;
                    labelFeeCurrencySwc.Text = FeeCCY;
                }
                catch
                {
                    return;
                }
                //20150915, liliana, LIBST13020, end
            }
        }

        private void cmpsrClientSwcOut_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrClientSwcOut.Text2 = "";
            nispOutstandingUnitSwc.Value = 0;

            //20150408, liliana, LIBST13020, begin
            //if(cmpsrClientSwcOut.Text1 !="")
            //{
            //    //tampilin oustanding unit
            //    decimal OutstandingBalance;
            //    int intClientCode;
            //    int.TryParse(cmpsrClientSwcOut[2].ToString(), out intClientCode);
            //    OutstandingBalance = GetLatestBalance(intClientCode);

            //    nispOutstandingUnitSwc.Value = OutstandingBalance;

            //}
            try
            {
                //tampilin oustanding unit
                decimal OutstandingBalance;
                int intClientCode;
                int.TryParse(cmpsrClientSwcOut[2].ToString(), out intClientCode);
                OutstandingBalance = GetLatestBalance(intClientCode);

                nispOutstandingUnitSwc.Value = OutstandingBalance;

            }
            catch
            {
                return;
            }
            //20150408, liliana, LIBST13020, end
        }

        private void cmpsrClientSwcOut_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrClientSwcIn.Text1 != "")
            {
                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSwcIn.Criteria = cmpsrProductSwcIn[2].ToString() + "#" + cmpsrCIFSwc.Text1.Trim();
                cmpsrClientSwcIn.Criteria = cmpsrProductSwcIn[2].ToString() + "#" + cmpsrCIFSwc.Text1.Trim()
                                            + "#" + cmbTASwc.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end
            }

            try
            {
                //tampilin oustanding unit
                decimal OutstandingBalance;
                int intClientCode;
                int.TryParse(cmpsrClientSwcOut[2].ToString(), out intClientCode);
                OutstandingBalance = GetLatestBalance(intClientCode);

                nispOutstandingUnitSwc.Value = OutstandingBalance;

            }
            catch
            {
                return;
            }
        }

        private void nispRedempSwc_Leave(object sender, EventArgs e)
        {
            try
            {
                string FeeCCY;
                decimal decNominalFee, decPctFee;

                if (nispRedempSwc.Value == nispOutstandingUnitSwc.Value)
                {
                    IsSwitchingAll = true;
                    //20150427, liliana, LIBST13020, begin
                    checkSwcAll.Checked = true;
                    //20150427, liliana, LIBST13020, end
                }
                //20150410, liliana, LIBST13020, begin
                else
                {
                    IsSwitchingAll = false;
                    //20150427, liliana, LIBST13020, begin
                    checkSwcAll.Checked = false;
                    //20150427, liliana, LIBST13020, end
                }
                //20150410, liliana, LIBST13020, end

                //20210406, Lita, RDN-563 RDN-594, format 4 decimal di belakang koma, begin
                nispRedempSwc.Text = String.Format("{0:N4}", nispRedempSwc.Value);
                //20210406, Lita, RDN-563 RDN-594, format 4 decimal di belakang koma, end

                HitungSwitchingFee(cmpsrProductSwcOut.Text1, cmpsrProductSwcIn.Text1, true, 0,
                    nispRedempSwc.Value, checkFeeEditSwc.Checked, nispPercentageFeeSwc.Value,
                    //20210309, joshua, RDN-466, begin
                    //cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee);
                    cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwc.Text1);
                //20210309, joshua, RDN-466, end

                nispMoneyFeeSwc.Value = decNominalFee;
                nispPercentageFeeSwc.Value = decPctFee;
                labelFeeCurrencySwc.Text = FeeCCY;
            }
            catch
            {
                return;
            }
        }

        private void nispRedempSwc_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            nispMoneyFeeSwc.Value = 0;
            nispPercentageFeeSwc.Value = 0;
            checkFeeEditSwc.Checked = false;
            //20150505, liliana, LIBST13020, begin
            if (checkFeeEditSwc.Checked == false)
            {
                nispMoneyFeeSwc.Enabled = false;
            }
            //20150505, liliana, LIBST13020, end
            labelFeeCurrencySwc.Text = "";

            //20150813, liliana, LIBST13020, begin
            if ((nispRedempSwc.Value != 0) && (cmpsrProductSwcOut.Text1 != "") && (cmpsrProductSwcIn.Text1 != "")
                && (cmpsrClientSwcOut.Text1 != "")
                )
            {
                string FeeCCY;
                decimal decNominalFee, decPctFee;
                cmpsrClientSwcOut.ValidateField();

                if (nispRedempSwc.Value == nispOutstandingUnitSwc.Value)
                {
                    IsSwitchingAll = true;
                    checkSwcAll.Checked = true;
                }
                else
                {
                    IsSwitchingAll = false;
                    checkSwcAll.Checked = false;
                }

                HitungSwitchingFee(cmpsrProductSwcOut.Text1, cmpsrProductSwcIn.Text1, true, 0,
                    nispRedempSwc.Value, checkFeeEditSwc.Checked, nispPercentageFeeSwc.Value,
                    //20210309, joshua, RDN-466, begin
                    //cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee);
                    cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwc.Text1);
                //20210309, joshua, RDN-466, end

                nispMoneyFeeSwc.Value = decNominalFee;
                nispPercentageFeeSwc.Value = decPctFee;
                labelFeeCurrencySwc.Text = FeeCCY;
            }
            //20150813, liliana, LIBST13020, end

        }

        private void HitungSwitchingFee(string ProdCodeSwitchOut, string ProdCodeSwitchIn, bool Jenis, decimal Nominal,
            decimal Unit, bool IsEdit, decimal PercentageInput, string IsEmployee,
            //20210309, joshua, RDN-466, begin
            //out string FeeCCY, out decimal PercentageOutput, out decimal NominalFee)
            out string FeeCCY, out decimal PercentageOutput, out decimal NominalFee, string CIFNo)
        //20210309, joshua, RDN-466, end
        {
            DataSet ds;
            //20210309, joshua, RDN-466, begin
            //OleDbParameter[] odp = new OleDbParameter[14];
            OleDbParameter[] odp = new OleDbParameter[15];
            //20210309, joshua, RDN-466, end
            FeeCCY = "";
            PercentageOutput = 0;
            NominalFee = 0;

            try
            {
                (odp[0] = new OleDbParameter("@pcProdSwitchOut", OleDbType.VarChar, 20)).Value = ProdCodeSwitchOut;
                (odp[1] = new OleDbParameter("@pcProdSwitchIn", OleDbType.VarChar, 20)).Value = ProdCodeSwitchIn;
                (odp[2] = new OleDbParameter("@pbJenis", OleDbType.Boolean)).Value = Jenis;
                (odp[3] = new OleDbParameter("@pnNominal", OleDbType.Double)).Value = (double)Nominal;
                (odp[4] = new OleDbParameter("@pmUnit", OleDbType.Double)).Value = (double)Unit;
                (odp[5] = new OleDbParameter("@pcFeeCCY", OleDbType.Char, 3)).Value = FeeCCY;
                (odp[6] = new OleDbParameter("@pnFee", OleDbType.Double)).Value = Fee;
                (odp[7] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                (odp[8] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
                (odp[9] = new OleDbParameter("@pmNAV", OleDbType.Decimal)).Value = 0;
                (odp[10] = new OleDbParameter("@pcIsEdit", OleDbType.Boolean)).Value = IsEdit;
                (odp[11] = new OleDbParameter("@pdPercentageInput", OleDbType.Double)).Value = PercentageInput;
                (odp[12] = new OleDbParameter("@pdPercentageOutput", OleDbType.Double)).Value = PercentageOutput;
                (odp[13] = new OleDbParameter("@bIsEmployee", OleDbType.Char, 1)).Value = IsEmployee;
                //20210309, joshua, RDN-466, begin
                (odp[14] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 20)).Value = CIFNo;
                //20210309, joshua, RDN-466, end

                odp[5].Direction = ParameterDirection.Output;
                odp[6].Direction = ParameterDirection.Output;
                odp[12].Direction = ParameterDirection.Output;

                bool blnResult = ClQ.ExecProc("dbo.ReksaCalcSwitchingFee", ref odp, out ds);

                if (blnResult)
                {
                    FeeCCY = odp[5].Value.ToString();
                    Fee = Double.Parse(odp[6].Value.ToString());
                    NominalFee = (decimal)Fee;

                    if (!IsEdit)
                    {
                        PercentFee = Double.Parse(odp[12].Value.ToString());
                        PercentageOutput = (decimal)PercentFee;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void nispMoneyFeeSwc_onNispMoneyTextChanged(object sender, EventArgs e)
        {

        }

        private void nispPercentageFeeSwc_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            if (nispRedempSwc.Value != 0)
            {
                try
                {
                    string FeeCCY;
                    decimal decNominalFee, decPctFee;

                    HitungSwitchingFee(cmpsrProductSwcOut.Text1, cmpsrProductSwcIn.Text1, true, 0,
                        nispRedempSwc.Value, checkFeeEditSwc.Checked, nispPercentageFeeSwc.Value,
                        //20210309, joshua, RDN-466, begin
                        //cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee);
                        cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwc.Text1);
                    //20210309, joshua, RDN-466, end

                    nispMoneyFeeSwc.Value = decNominalFee;
                    labelFeeCurrencySwc.Text = FeeCCY;
                }
                catch
                {
                    return;
                }
            }
        }

        private void cmpsrProductSwcRDBOut_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductSwcRDBOut.Text2 = "";
        }

        private void cmpsrProductSwcRDBOut_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                //20160829, liliana, LOGEN00196, begin
                //cmpsrClientSwcRDBOut.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBOut.Text1 + "#" + _strTabName;
                cmpsrClientSwcRDBOut.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBOut.Text1 + "#" + _strTabName
                                                 + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                //20160829, liliana, LOGEN00196, end

                cmpsrProductSwcRDBIn.Criteria = cmpsrProductSwcRDBOut.Text1.Trim();

                cmpsrProductSwcRDBIn.Text1 = "";
                cmpsrProductSwcRDBIn.ValidateField();
                cmpsrClientSwcRDBIn.Text1 = "";
                cmpsrClientSwcRDBIn.ValidateField();
            }
            catch
            {
                return;
            }
        }

        private void cmpsrClientSwcRDBOut_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrClientSwcRDBOut.Text2 = "";
            nispOutstandingUnitSwcRDB.Value = 0;

            nispRedempSwcRDB.Value = 0;
            //20220805, antoniusfilian, RDN-835, begin
            checkSwcRDBAll.Checked = false;
            //20220805, antoniusfilian, RDN-835, end
            nispJangkaWktSwcRDB.Value = 0;
            dtJatuhTempoSwcRDB.Value = 0;
            //20150415, liliana, LIBST13020, begin
            dtJatuhTempoSwcRDB.Text = "";
            //20150415, liliana, LIBST13020, end
            cmbFrekPendebetanSwcRDB.SelectedIndex = -1;
            cmbAutoRedempSwcRDB.SelectedIndex = -1;
            cmbAsuransiSwcRDB.SelectedIndex = -1;
        }

        private DataSet GetDataRDB(string ClientCode)
        {
            DataSet dsRDB;
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcClientCode", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = ClientCode;

            bool blnResult = ClQ.ExecProc("ReksaGetListClientRDB", ref dbParam, out dsRDB);

            return dsRDB;
        }

        private void cmpsrClientSwcRDBOut_onNispText2Changed(object sender, EventArgs e)
        {
            try
            {
                //20150826, liliana, LIBST13020, begin
                //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName + "IN";
                //20150826, liliana, LIBST13020, end

                //tampilin oustanding unit
                decimal OutstandingBalance;
                int intClientCode;
                int.TryParse(cmpsrClientSwcRDBOut[2].ToString(), out intClientCode);
                OutstandingBalance = GetLatestBalance(intClientCode);


                nispOutstandingUnitSwcRDB.Value = OutstandingBalance;
                //20150826, liliana, LIBST13020, begin
                //nispRedempSwcRDB.Value = nispOutstandingUnitSwcRDB.Value;
                //20150826, liliana, LIBST13020, end
                DataSet dsRDBSwitchOut;
                dsRDBSwitchOut = GetDataRDB(cmpsrClientSwcRDBOut.Text1);

                if (dsRDBSwitchOut.Tables[0].Rows.Count > 0)
                {
                    decimal decJangkaWaktu;
                    decimal.TryParse(dsRDBSwitchOut.Tables[0].Rows[0]["SisaJangkaWaktu"].ToString(), out decJangkaWaktu);
                    DateTime dtJatuhTempo;
                    DateTime.TryParse(dsRDBSwitchOut.Tables[0].Rows[0]["JatuhTempo"].ToString(), out dtJatuhTempo);
                    //20150820, liliana, LIBST13020, begin
                    string DoneDebet;
                    DoneDebet = dsRDBSwitchOut.Tables[0].Rows[0]["IsDoneDebet"].ToString();
                    //20150820, liliana, LIBST13020, end

                    //20200724, pratama, RDN-39,begin
                    string freqDebetUnit = dsRDBSwitchOut.Tables[0].Rows[0]["FreqDebetUnit"].ToString();
                    freqDebetUnit = freqDebetUnit.Trim().ToLower().Replace("/", "");
                    freqDebetUnit = freqDebetUnit.Remove(freqDebetUnit.Length - 2);//remove an from string
                    label116.Text = freqDebetUnit;
                    label114.Text = freqDebetUnit;
                    //20200724, pratama, RDN-39,end

                    //20210910, korvi, RDN-646, begin
                    if (DoneDebet == "1")
                    {
                        //20220315, gio, RDN-736, begin
                        //cmpsrProductSwcRDBIn.SearchDesc = "TRXSWITCHOUT";
                        cmpsrProductSwcRDBIn.SearchDesc = "TRXSWITCHIN";
                        //cmpsrProductSwcRDBIn.Criteria = cmpsrProductSwcRDBOut.Text1;
                        //20220315, gio, RDN-736, end
                        //20230216, ahmad.fansyuri, RDN-912, BEGIN
                        cmpsrProductSwcRDBIn.Criteria = cmpsrProductSwcRDBOut.Text1 + "#" + cmpsrClientSwcRDBOut.Text1 + "#"
                                                        + DoneDebet;
                        //20230216, ahmad.fansyuri, RDN-912, END
                    }
                    else
                    {
                        //20220405, gio, RDN-769, begin
                        //cmpsrProductSwcRDBIn.SearchDesc = "TRXWITCHINRDB";
                        cmpsrProductSwcRDBIn.SearchDesc = "TRXSWITCHIN";
                        //cmpsrProductSwcRDBIn.Criteria = cmpsrClientSwcRDBOut.Text1;

                        //20220405, gio, RDN-769, end
                        //20230216, ahmad.fansyuri, RDN-912, BEGIN
                        cmpsrProductSwcRDBIn.Criteria = cmpsrProductSwcRDBOut.Text1 + "#" + cmpsrClientSwcRDBOut.Text1 + "#"
                                                        + DoneDebet;
                        //20230216, ahmad.fansyuri, RDN-912, END
                        /*string strCriteria = DoneDebet + "#" + cmpsrClientSwcRDBOut.Text1;
                        cmpsrProductSwcRDBIn.SearchDesc = "TRXWITCHINRDB";
                        cmpsrProductSwcRDBIn.Criteria = cmpsrClientSwcRDBOut.Text1;*/
                    }
                    //20210910, korvi, RDN-646, begin

                    nispJangkaWktSwcRDB.Value = decJangkaWaktu;
                    dtJatuhTempoSwcRDB.Value = Convert.ToInt32(dtJatuhTempo.ToString("yyyyMMdd"));
                    cmbFrekPendebetanSwcRDB.Text = dsRDBSwitchOut.Tables[0].Rows[0]["FrekPendebetan"].ToString();
                    //20150820, liliana, LIBST13020, begin
                    //20150820, liliana, LIBST13020, begin
                    //if (nispJangkaWktSwcRDB.Value == 0)
                    if ((nispJangkaWktSwcRDB.Value == 0) && (DoneDebet == "1"))
                    //20150820, liliana, LIBST13020, end
                    {
                        nispRedempSwcRDB.Enabled = true;
                        //20220805, antoniusfilian, RDN-835, begin
                        checkSwcRDBAll.Enabled = true;
                        checkSwcRDBAll.Checked = false;
                        if (nispOutstandingUnitSwcRDB.Value == 0)
                        {
                            nispRedempSwcRDB.Enabled = false;
                            checkSwcRDBAll.Enabled = false;
                            checkSwcRDBAll.Checked = true;
                        }
                        //20220805, antoniusfilian, RDN-835, end
                        //20150826, liliana, LIBST13020, begin
                        //nispRedempSwcRDB.Value = 0;
                        //20150826, liliana, LIBST13020, end
                        //20150820, liliana, LIBST13020, begin
                        //20160830, liliana, LOGEN00196, begin
                        //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + "SWCNONRDB";
                        cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + "SWCNONRDB"
                                                        + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                        //20160830, liliana, LOGEN00196, end
                        //20150820, liliana, LIBST13020, end
                        //20150820, liliana, LIBST13020, begin
                        IsSwitchingRDBSebagian = false;
                        //20150820, liliana, LIBST13020, end
                    }
                    else
                    {
                        nispRedempSwcRDB.Enabled = false;
                        nispRedempSwcRDB.Value = nispOutstandingUnitSwcRDB.Value;
                        //20150820, liliana, LIBST13020, begin
                        //20150826, liliana, LIBST13020, begin
                        //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName;
                        cmpsrClientSwcRDBIn.Enabled = false;
                        IsSubsNew = true;
                        //20150826, liliana, LIBST13020, end
                        //20150820, liliana, LIBST13020, end
                        //20150820, liliana, LIBST13020, begin
                        IsSwitchingRDBSebagian = true;
                        //20150820, liliana, LIBST13020, end
                        //20220805, antoniusfilian, RDN-835, begin
                        checkSwcRDBAll.Checked = true;
                        checkSwcRDBAll.Enabled = false;
                        //20220805, antoniusfilian, RDN-835, end
                    }
                    //20150820, liliana, LIBST13020, end

                    if (dsRDBSwitchOut.Tables[0].Rows[0]["AutoRedemption"].ToString() == "1")
                    {
                        cmbAutoRedempSwcRDB.SelectedIndex = 0;
                    }
                    else
                    {
                        cmbAutoRedempSwcRDB.SelectedIndex = 1;
                    }

                    if (dsRDBSwitchOut.Tables[0].Rows[0]["Asuransi"].ToString() == "1")
                    {
                        cmbAsuransiSwcRDB.SelectedIndex = 0;
                    }
                    else
                    {
                        cmbAsuransiSwcRDB.SelectedIndex = 1;
                    }
                }

                //20150826, liliana, LIBST13020, begin
                //string FeeCCY;
                //decimal decNominalFee, decPctFee;
                //int ProdSwcOut, ClientSwcOut;
                //int.TryParse(cmpsrProductSwcRDBOut[2].ToString(), out ProdSwcOut);
                //int.TryParse(cmpsrClientSwcRDBOut[2].ToString(), out ClientSwcOut);

                //HitungSwitchingRDBFee(ProdSwcOut, ClientSwcOut, nispRedempSwcRDB.Value, checkFeeEditSwcRDB.Checked,
                //    nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee);

                //nispMoneyFeeSwcRDB.Value = decNominalFee;
                //nispPercentageFeeSwcRDB.Value = decPctFee;
                //labelFeeCurrencySwcRDB.Text = FeeCCY;
                //20150826, liliana, LIBST13020, end

            }
            catch
            {
                return;
            }
        }

        private void cmpsrProductSwcRDBIn_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrProductSwcRDBIn.Text2 = "";

            cmpsrClientSwcRDBIn.Text1 = "";
            cmpsrClientSwcRDBIn.Text2 = "";
            cmpsrClientSwcRDBIn.Enabled = false;
        }

        private void cmpsrProductSwcRDBIn_onNispText2Changed(object sender, EventArgs e)
        {
            //20160829, liliana, LOGEN00196, begin
            //20201112, Lita, RDN-39, begin
            //if (cmbTASwcRDB.SelectedIndex == -1)
            if (cmbTASwcRDB.SelectedIndex == -1 && cmpsrCIFSwcRDB.Text1 != "")
            //20201112, Lita, RDN-39, end
            {
                MessageBox.Show("Harap memilih Source of Fund terlebih dahulu! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                cmpsrProductSwcRDBIn.Text1 = "";
                cmpsrProductSwcRDBIn.Text2 = "";
                return;
            }

            //20160829, liliana, LOGEN00196, end

            if (cmpsrProductSwcRDBIn.Text1 != "")
            {
                //29231027, Andhika J, RDN-1088, begin
                string cErrorMessage = "";
                if (ReksaNotifikasiSwitchingRDB(cmpsrClientSwcRDBOut.Text1, out cErrorMessage))
                {
                    if (cErrorMessage != "")
                    {
                        MessageBox.Show(cErrorMessage, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                //29231027, Andhika J, RDN-1088, end
                //20150820, liliana, LIBST13020, begin
                //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName;
                DataSet dsRDBSwitchOut;
                dsRDBSwitchOut = GetDataRDB(cmpsrClientSwcRDBOut.Text1);
                string DoneDebet;
                DoneDebet = "";

                if (dsRDBSwitchOut.Tables[0].Rows.Count > 0)
                {
                    DoneDebet = dsRDBSwitchOut.Tables[0].Rows[0]["IsDoneDebet"].ToString();
                }

                if ((nispJangkaWktSwcRDB.Value == 0) && (DoneDebet == "1"))
                {
                    nispRedempSwcRDB.Enabled = true;
                    //20220805, antoniusfilian, RDN-835, begin
                    checkSwcRDBAll.Enabled = true;
                    checkSwcRDBAll.Checked = false;
                    //20220805, antoniusfilian, RDN-835, end
                    //20160830, liliana, LOGEN00196, begin
                    //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + "SWCNONRDB";
                    cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + "SWCNONRDB"
                                                    + "#" + cmbTASwcRDB.SelectedIndex.ToString();
                    //20160830, liliana, LOGEN00196, end

                    IsSwitchingRDBSebagian = true;
                }
                else
                {
                    nispRedempSwcRDB.Enabled = false;
                    nispRedempSwcRDB.Value = nispOutstandingUnitSwcRDB.Value;
                    //20220805, antoniusfilian, RDN-835, begin
                    checkSwcRDBAll.Enabled = false;
                    checkSwcRDBAll.Checked = true;
                    //20220805, antoniusfilian, RDN-835, end
                    //20150826, liliana, LIBST13020, begin
                    //cmpsrClientSwcRDBIn.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmpsrProductSwcRDBIn.Text1 + "#" + _strTabName;
                    //IsSwitchingRDBSebagian = true;
                    cmpsrClientSwcRDBIn.Enabled = false;
                    IsSubsNew = true;
                    IsSwitchingRDBSebagian = false;
                    //20150826, liliana, LIBST13020, end
                }
                //20150820, liliana, LIBST13020, end

                //cek subs new / subs add
                //20150408, liliana, LIBST13020, begin
                //20150826, liliana, LIBST13020, begin
                //if (_intType == 1)
                if ((_intType == 1) && (nispJangkaWktSwcRDB.Value == 0) && (DoneDebet == "1"))
                //20150826, liliana, LIBST13020, end
                {
                    //20150408, liliana, LIBST13020, end
                    int intProductId;
                    int.TryParse(cmpsrProductSwcRDBIn[2].ToString(), out intProductId);

                    //20150617, liliana, LIBST13020, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSwcRDB.Text1.Trim(), intProductId, true);
                    string ClientCodeSwcIn;
                    ClientCodeSwcIn = "";

                    //20150826, liliana, LIBST13020, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSwcRDB.Text1.Trim(), intProductId, true, out ClientCodeSwcIn);
                    //20160829, liliana, LOGEN00196, begin
                    //IsSubsNew = CheckIsSubsNew(cmpsrCIFSwcRDB.Text1.Trim(), intProductId, false, out ClientCodeSwcIn);
                    IsSubsNew = CheckIsSubsNew(cmpsrCIFSwcRDB.Text1.Trim(), intProductId, false, out ClientCodeSwcIn
                        , cmbTASwcRDB.SelectedIndex
                        );
                    //20160829, liliana, LOGEN00196, end
                    //20150826, liliana, LIBST13020, end
                    //20150617, liliana, LIBST13020, end

                    if (IsSubsNew)
                    {

                        cmpsrClientSwcRDBIn.Enabled = false;

                        string strTranCode, strNewClientCode;

                        //20150812, liliana, LIBST13020, begin
                        //GenerateTranCodeAndClientCode(_strTabName, IsSubsNew, cmpsrProductSwcRDBIn.Text1,
                        //    //20150505, liliana, LIBST13020, begin
                        //    //cmpsrClientSwcRDBIn.Text1, out strTranCode, out strNewClientCode, cmpsrCIFSwcRDB.Text1);
                        //    cmpsrClientSwcRDBIn.Text1, out strTranCode, out strNewClientCode, cmpsrCIFSwcRDB.Text1,
                        //    checkFeeEditSwcRDB.Checked, nispPercentageFeeSwcRDB.Value, 0
                        //    //20150610, liliana, LIBST13020, begin
                        //    , false, (double)nispMoneyFeeSwcRDB.Value, 0, (double)nispRedempSwcRDB.Value, IsSwitchingAll, 0, 0
                        //    //20150610, liliana, LIBST13020, end
                        //    );
                        ////20150505, liliana, LIBST13020, end


                        //cmpsrClientSwcRDBIn.Text1 = strNewClientCode;
                        //cmpsrClientSwcRDBIn.Text2 = cmpsrCIFSwcRDB.Text2;
                        //20150812, liliana, LIBST13020, end
                    }
                    else
                    {
                        //20150617, liliana, LIBST13020, begin
                        //cmpsrClientSwcRDBIn.Enabled = true;
                        cmpsrClientSwcRDBIn.Enabled = false;
                        cmpsrClientSwcRDBIn.Text1 = ClientCodeSwcIn;
                        cmpsrClientSwcRDBIn.ValidateField();
                        //20150617, liliana, LIBST13020, end
                    }
                    //20150408, liliana, LIBST13020, begin
                }
                //20150408, liliana, LIBST13020, end

                //20220622, sandi, RDN-802, begin
                string FeeCCY;
                decimal decNominalFee, decPctFee;
                int ProdSwcOut, ClientSwcOut, ProdSwcIn;

                string strPrdIdIn = GetImportantData("PRODUKID", cmpsrProductSwcRDBIn.Text1);
                int.TryParse(strPrdIdIn, out ProdSwcIn);

                string strPrdId = GetImportantData("PRODUKID", cmpsrProductSwcRDBOut.Text1);
                int.TryParse(strPrdId, out ProdSwcOut);

                string strClientId = GetImportantData("CLIENTID", cmpsrClientSwcRDBOut.Text1);
                int.TryParse(strClientId, out ClientSwcOut);

                nispRedempSwcRDB.Text = String.Format("{0:N4}", nispRedempSwcRDB.Value);

                if (strClientId != "")
                {
                    HitungSwitchingRDBFee(ProdSwcOut, ClientSwcOut, nispRedempSwcRDB.Value, checkFeeEditSwcRDB.Checked,
                    nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1
                    , ProdSwcIn, cmpsrNoRefSwcRDB.Text1.Trim());

                    nispMoneyFeeSwcRDB.Value = decNominalFee;
                    nispPercentageFeeSwcRDB.Value = decPctFee;
                    labelFeeCurrencySwcRDB.Text = FeeCCY;
                }
                //20220622, sandi, RDN-802, end
            }
        }
        //29231027, Andhika J, RDN-1088, begin
        private bool ReksaNotifikasiSwitchingRDB(string ClientCode, out string ErrMessage)
        {
            bool bReturn = false;
            DataSet dsRDB;
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[2];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcClientCode", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = ClientCode;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@cErrMessage", System.Data.OleDb.OleDbType.VarChar, 200);
            dbParam[1].Value = "";

            dbParam[1].Direction = ParameterDirection.Output;

            if (ClQ.ExecProc("ReksaNotifikasiSwitchingRDB", ref dbParam, out dsRDB))
            {
                ErrMessage = dbParam[1].Value.ToString();
                bReturn = true;
            }
            else
            {
                ErrMessage = "";
                bReturn = false;
            }
            return bReturn;
        }
        //29231027, Andhika J, RDN-1088, end
        private void nispJangkaWktSwcRDB_onNispMoneyValueChanged(object sender, EventArgs e)
        {
            if (nispJangkaWktSwcRDB.Value == 0)
                dtJatuhTempoSwcRDB.Value = 0;
            else
            {
                try
                {
                    DateTime JoinDate = DateTime.Parse(ProReksa2.Global.strCurrentDate.ToString());
                    globalJatuhTempoSwcRDB = JoinDate.AddMonths((int)nispJangkaWktRDB.Value);
                    dtJatuhTempoSwcRDB.Value = Convert.ToInt32(JoinDate.AddMonths((int)nispJangkaWktRDB.Value).ToString("yyyyMMdd"));

                }
                catch
                {
                    dtJatuhTempoSwcRDB.Value = 0;
                }
            }
        }

        private void cmbAutoRedempSwcRDB_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void HitungSwitchingRDBFee(int ProdIdSwitchOut, int ClientIdSwitchOut,
            decimal Unit, bool IsEdit, decimal PercentageInput,
            //20210309, joshua, RDN-466, begin
            //out string FeeCCY, out decimal PercentageOutput, out decimal NominalFee)
            //20220622, sandi, RDN-802, begin
            //out string FeeCCY, out decimal PercentageOutput, out decimal NominalFee, string CIFNo)
            out string FeeCCY, out decimal PercentageOutput, out decimal NominalFee, string CIFNo
            , int ProdIdSwitchIn, string RefID)
        //20220622, sandi, RDN-802, end
        //20210309, joshua, RDN-466, end
        {
            DataSet ds;
            //20210309, joshua, RDN-466, begin
            //OleDbParameter[] odp = new OleDbParameter[10];
            //20220622, sandi, RDN-802, begin
            //OleDbParameter[] odp = new OleDbParameter[11];
            OleDbParameter[] odp = new OleDbParameter[13];
            //20220622, sandi, RDN-802, end
            //20210309, joshua, RDN-466, end
            FeeCCY = "";
            PercentageOutput = 0;
            NominalFee = 0;

            try
            {
                (odp[0] = new OleDbParameter("@pnProdSwitchOut", OleDbType.Integer)).Value = ProdIdSwitchOut;
                (odp[1] = new OleDbParameter("@pnClientSwitchOut", OleDbType.Integer)).Value = ClientIdSwitchOut;
                //20150908, liliana, LIBST13020, begin
                //(odp[2] = new OleDbParameter("@pmUnit", OleDbType.Decimal)).Value = Unit;
                (odp[2] = new OleDbParameter("@pmUnit", OleDbType.Double)).Value = Unit;
                //20150908, liliana, LIBST13020, end
                (odp[3] = new OleDbParameter("@pcFeeCCY", OleDbType.Char, 3)).Value = FeeCCY;
                (odp[4] = new OleDbParameter("@pnFee", OleDbType.Double)).Value = Fee;
                (odp[5] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
                (odp[6] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
                (odp[7] = new OleDbParameter("@pcIsEdit", OleDbType.Boolean)).Value = IsEdit;
                //20150908, liliana, LIBST13020, begin
                //(odp[8] = new OleDbParameter("@pdPercentageInput", OleDbType.Decimal)).Value = PercentageInput;
                //(odp[9] = new OleDbParameter("@pdPercentageOutput", OleDbType.Decimal)).Value = PercentageOutput;
                (odp[8] = new OleDbParameter("@pdPercentageInput", OleDbType.Double)).Value = PercentageInput;
                (odp[9] = new OleDbParameter("@pdPercentageOutput", OleDbType.Double)).Value = PercentageOutput;
                //20150908, liliana, LIBST13020, end
                //20210309, joshua, RDN-466, begin
                (odp[10] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 20)).Value = CIFNo;
                //20210309, joshua, RDN-466, end
                //20220622, sandi, RDN-802, begin
                (odp[11] = new OleDbParameter("@pnProdSwitchIn", OleDbType.Integer)).Value = ProdIdSwitchIn;
                (odp[12] = new OleDbParameter("@pcRefID", OleDbType.VarChar, 20)).Value = RefID;
                //20220622, sandi, RDN-802, end

                odp[3].Direction = ParameterDirection.Output;
                odp[4].Direction = ParameterDirection.Output;
                odp[9].Direction = ParameterDirection.Output;

                bool blnResult = ClQ.ExecProc("dbo.ReksaCalcSwitchingRDBFee", ref odp, out ds);

                if (blnResult)
                {
                    FeeCCY = odp[3].Value.ToString();
                    Fee = Double.Parse(odp[4].Value.ToString());
                    NominalFee = (decimal)Fee;

                    if (!IsEdit)
                    {
                        PercentFee = Double.Parse(odp[9].Value.ToString());
                        PercentageOutput = (decimal)PercentFee;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void nispRedempSwcRDB_Leave(object sender, EventArgs e)
        {
            //20150921, liliana, LIBST13020, begin
            if ((nispRedempSwcRDB.Value != 0) && (cmpsrProductSwcRDBOut.Text1 != "")
                && (cmpsrClientSwcRDBOut.Text1 != "")
                )
            {
                //20150921, liliana, LIBST13020, end
                try
                {
                    string FeeCCY;
                    decimal decNominalFee, decPctFee;
                    //20220805, antoniusfilian, RDN-835, begin
                    if (nispRedempSwcRDB.Value == nispOutstandingUnitSwcRDB.Value)
                    {
                        IsSwitchingRDBSebagian = false;
                        checkSwcRDBAll.Checked = true;
                    }
                    else
                    {
                        IsSwitchingRDBSebagian = true;
                        checkSwcRDBAll.Checked = false;
                    }
                    //20220805, antoniusfilian, RDN-835, end
                    int ProdSwcOut, ClientSwcOut;
                    //20220622, sandi, RDN-802, begin
                    int ProdSwcIn;

                    if (cmpsrProductSwcRDBIn.Text1 == "" && nispRedempSwcRDB.Enabled == true)
                    {
                        MessageBox.Show("Silahkan isi terlebih dahulu produk Switch In!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nispRedempSwcRDB.Text = "0";
                        return;
                    }

                    if (cmpsrProductSwcRDBIn.Text1 == "" && nispRedempSwcRDB.Enabled == false)
                    {
                        return;
                    }

                    string strPrdIdIn = GetImportantData("PRODUKID", cmpsrProductSwcRDBIn.Text1);
                    int.TryParse(strPrdIdIn, out ProdSwcIn);
                    //20220622, sandi, RDN-802, end
                    //20150828, liliana, LIBST13020, begin
                    //int.TryParse(cmpsrProductSwcRDBOut[2].ToString(), out ProdSwcOut);
                    //int.TryParse(cmpsrClientSwcRDBOut[2].ToString(), out ClientSwcOut);
                    string strPrdId = GetImportantData("PRODUKID", cmpsrProductSwcRDBOut.Text1);
                    int.TryParse(strPrdId, out ProdSwcOut);

                    string strClientId = GetImportantData("CLIENTID", cmpsrClientSwcRDBOut.Text1);
                    int.TryParse(strClientId, out ClientSwcOut);
                    //20150828, liliana, LIBST13020, end

                    //20210406, Lita, RDN-563 RDN-594, format 4 decimal di belakang koma, begin
                    nispRedempSwcRDB.Text = String.Format("{0:N4}", nispRedempSwcRDB.Value);
                    //20210406, Lita, RDN-563 RDN-594, format 4 decimal di belakang koma, end

                    HitungSwitchingRDBFee(ProdSwcOut, ClientSwcOut, nispRedempSwcRDB.Value, checkFeeEditSwcRDB.Checked,
                        //20210309, joshua, RDN-466, begin
                        //nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee);
                        //20220622, sandi, RDN-802, begin
                        //nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1);
                        nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1
                        , ProdSwcIn, cmpsrNoRefSwcRDB.Text1.Trim());
                    //20220622, sandi, RDN-802, end
                    //20210309, joshua, RDN-466, end

                    nispMoneyFeeSwcRDB.Value = decNominalFee;
                    nispPercentageFeeSwcRDB.Value = decPctFee;
                    labelFeeCurrencySwcRDB.Text = FeeCCY;
                }
                catch
                {
                    return;
                }
                //20150921, liliana, LIBST13020, begin
            }
            //20150921, liliana, LIBST13020, end
        }

        private void nispRedempSwcRDB_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            nispMoneyFeeSwcRDB.Value = 0;
            nispPercentageFeeSwcRDB.Value = 0;
            checkFeeEditSwcRDB.Checked = false;
            //20150505, liliana, LIBST13020, begin
            if (checkFeeEditSwcRDB.Checked == false)
            {
                nispMoneyFeeSwcRDB.Enabled = false;
            }
            //20150505, liliana, LIBST13020, end
            labelFeeCurrencySwcRDB.Text = "";

            //20150813, liliana, LIBST13020, begin
            if ((nispRedempSwcRDB.Value != 0) && (cmpsrProductSwcRDBOut.Text1 != "")
                && (cmpsrClientSwcRDBOut.Text1 != "")
                )
            {
                string FeeCCY;
                decimal decNominalFee, decPctFee;
                int ProdSwcOut, ClientSwcOut;
                //20150828, liliana, LIBST13020, begin
                //cmpsrProductSwcRDBOut.ValidateField();
                //cmpsrClientSwcRDBOut.ValidateField();
                //20150828, liliana, LIBST13020, end
                //20220622, sandi, RDN-802, begin
                int ProdSwcIn;

                if (cmpsrProductSwcRDBIn.Text1 == "" && nispRedempSwcRDB.Enabled == true)
                {
                    MessageBox.Show("Silahkan isi terlebih dahulu produk Switch In!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nispRedempSwcRDB.Text = "0";
                    return;
                }

                if (cmpsrProductSwcRDBIn.Text1 == "" && nispRedempSwcRDB.Enabled == false)
                {
                    return;
                }

                string strPrdIdIn = GetImportantData("PRODUKID", cmpsrProductSwcRDBIn.Text1);
                int.TryParse(strPrdIdIn, out ProdSwcIn);
                //20220622, sandi, RDN-802, end

                //20150828, liliana, LIBST13020, begin
                //int.TryParse(cmpsrProductSwcRDBOut[2].ToString(), out ProdSwcOut);
                //int.TryParse(cmpsrClientSwcRDBOut[2].ToString(), out ClientSwcOut);
                string strPrdId = GetImportantData("PRODUKID", cmpsrProductSwcRDBOut.Text1);
                int.TryParse(strPrdId, out ProdSwcOut);

                string strClientId = GetImportantData("CLIENTID", cmpsrClientSwcRDBOut.Text1);
                int.TryParse(strClientId, out ClientSwcOut);
                //20150828, liliana, LIBST13020, end

                //20220805, antoniusfilian, RDN-835, begin
                if (nispRedempSwcRDB.Value == nispOutstandingUnitSwcRDB.Value)
                {
                    IsSwitchingRDBSebagian = false;
                    checkSwcRDBAll.Checked = true;
                }
                else
                {
                    IsSwitchingRDBSebagian = true;
                    checkSwcRDBAll.Checked = false;
                }
                //20220805, antoniusfilian, RDN-835, end
                HitungSwitchingRDBFee(ProdSwcOut, ClientSwcOut, nispRedempSwcRDB.Value, checkFeeEditSwcRDB.Checked,
                    //20210309, joshua, RDN-466, begin
                    //nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee);
                    //20220622, sandi, RDN-802, begin
                    //nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1);
                    nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1
                    , ProdSwcIn, cmpsrNoRefSwcRDB.Text1.Trim());
                //20220622, sandi, RDN-802, end
                //20210309, joshua, RDN-466, end

                nispMoneyFeeSwcRDB.Value = decNominalFee;
                nispPercentageFeeSwcRDB.Value = decPctFee;
                labelFeeCurrencySwcRDB.Text = FeeCCY;
            }
            //20150813, liliana, LIBST13020, end
        }

        private void nispMoneyNomBooking_Leave(object sender, EventArgs e)
        {
            try
            {
                string strFeeCurr;
                decimal decNominalFee, decPctFee;

                HitungBookingFee(cmpsrCIFBooking.Text1, nispMoneyNomBooking.Value, cmpsrProductBooking.Text1, ByPercent,
                    checkFeeEditBooking.Checked, nispMoneyFeeBooking.Value, out decPctFee, out strFeeCurr, out decNominalFee);

                if (checkFeeEditBooking.Checked == false) //hitung fee tanpa edit fee
                {
                    nispMoneyFeeBooking.Value = decNominalFee;
                    nispPercentageFeeBooking.Value = decPctFee;
                    labelFeeCurrencyBooking.Text = strFeeCurr;
                    _KeteranganFeeBooking.Text = "%";
                }
                else if (checkFeeEditBooking.Checked == true) //hitung fee dengan edit fee
                {
                    if (ByPercent)
                    {
                        nispPercentageFeeBooking.Value = decNominalFee;
                        labelFeeCurrencyBooking.Text = "%";
                        _KeteranganFeeBooking.Text = strFeeCurr;
                    }
                    else
                    {
                        nispPercentageFeeBooking.Value = decPctFee;
                        labelFeeCurrencyBooking.Text = strFeeCurr;
                        _KeteranganFeeBooking.Text = "%";

                    }
                }

            }
            catch
            {
                return;
            }
        }

        private void HitungBookingFee(string CIFNo, decimal BookingAmount, string ProductCode, bool boolByPercent,
            bool IsFeeEdit, decimal PercentageFeeInput, out decimal PctFee,
            out string FeeCurr, out decimal NominalFee
            )
        {
            DataSet ds;
            FeeCurr = "";
            NominalFee = 0;
            PctFee = 0;

            OleDbParameter[] odp = new OleDbParameter[9];

            try
            {
                (odp[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 20)).Value = CIFNo;
                (odp[1] = new OleDbParameter("@pnAmount", OleDbType.Double)).Value = (double)BookingAmount;
                (odp[2] = new OleDbParameter("@pcProductCode", OleDbType.VarChar, 20)).Value = ProductCode;
                (odp[3] = new OleDbParameter("@pbIsByPercent", OleDbType.Boolean)).Value = boolByPercent;
                (odp[4] = new OleDbParameter("@pbIsFeeEdit", OleDbType.Boolean)).Value = IsFeeEdit;
                (odp[5] = new OleDbParameter("@pdPercentageFeeInput", OleDbType.Double)).Value = (decimal)PercentageFeeInput;
                (odp[6] = new OleDbParameter("@pdPercentageFeeOutput", OleDbType.Double)).Value = 0;
                (odp[7] = new OleDbParameter("@pcFeeCCY", OleDbType.VarChar, 3)).Value = "";
                (odp[8] = new OleDbParameter("@pmFee", OleDbType.Double, 50)).Value = 0;

                odp[6].Direction = ParameterDirection.Output;
                odp[7].Direction = ParameterDirection.Output;
                odp[8].Direction = ParameterDirection.Output;

                bool blnResult = ClQ.ExecProc("dbo.ReksaCalcBookingFee", ref odp, out ds);

                if (blnResult)
                {
                    FeeCurr = odp[7].Value.ToString();
                    Fee = Double.Parse(odp[8].Value.ToString());
                    Fee = System.Math.Round(Fee, 2);
                    NominalFee = (decimal)Fee;

                    PercentFee = Double.Parse(odp[6].Value.ToString());
                    PctFee = (decimal)PercentFee;
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private void checkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (checkAll.Checked)
            {
                nispRedempUnit.Value = nispOutstandingUnitRedemp.Value;
                nispRedempUnit.Enabled = false;
                IsRedempAll = true;
            }
            else
            {
                nispRedempUnit.Value = 0;
                nispRedempUnit.Enabled = true;
                IsRedempAll = false;
            }
            //20150622, liliana, LIBST13020, begin
            //20150826, liliana, LIBST13020, end
            //cmpsrProductRedemp.ValidateField();
            //cmpsrClientRedemp.ValidateField();


            //try
            //{
            //    decimal decNominalFee, decPctFee;
            //    int intProdId, intClientid, intTranType;
            //    int.TryParse(cmpsrProductRedemp[2].ToString(), out intProdId);
            //    int.TryParse(cmpsrClientRedemp[2].ToString(), out intClientid);

            //    if ((_ComboJenisRedemp.Text == "By %") && (checkFeeEditRedemp.Checked))
            //    {
            //        ByPercent = true;
            //    }
            //    else
            //    {
            //        ByPercent = false;
            //    }


            //    if (IsRedempAll)
            //    {
            //        intTranType = 4;
            //    }
            //    else
            //    {
            //        intTranType = 3;
            //    }

            //    HitungFee(intProdId, intClientid, intTranType, 0, nispRedempUnit.Value, false,
            //        checkFeeEditRedemp.Checked, nispMoneyFeeRedemp.Value, 1, out strFeeCurr,
            //        out decNominalFee, out decPctFee, cmpsrCIFRedemp.Text1);

            //    nispMoneyFeeRedemp.Value = decPctFee;
            //    nispPercentageFeeRedemp.Value = decNominalFee;
            //    labelFeeCurrencyRedemp.Text = "%";
            //    _KeteranganFeeRedemp.Text = strFeeCurr;
            //}
            //catch
            //{
            //    return;
            //}
            //20150826, liliana, LIBST13020, end
            //20150622, liliana, LIBST13020, end
        }

        private void nispMoneyNomSubs_onNispMoneyValueChanged(object sender, EventArgs e)
        {
            nispMoneyFeeSubs.Value = 0;
            nispPercentageFeeSubs.Value = 0;
            checkFeeEditSubs.Checked = false;
            //20150424, liliana, LIBST13020, begin
            //_ComboJenisSubs.SelectedIndex = 0;
            _ComboJenisSubs.SelectedIndex = 1;

            if (checkFeeEditSubs.Checked == false)
            {
                _ComboJenisSubs.Enabled = false;
                nispMoneyFeeSubs.Enabled = false;
            }
            //20150424, liliana, LIBST13020, end
            _KeteranganFeeSubs.Text = "";
            labelFeeCurrencySubs.Text = "";
            nispMoneyFeeSubs.Text = "";
            nispPercentageFeeSubs.Text = "";
            //20150818, liliana, LIBST13020, begin

            try
            {
                if ((nispMoneyNomSubs.Value != 0) && (cmpsrProductSubs.Text1 != "")
                    )
                {
                    decimal decNominalFee, decPctFee;
                    int intProdId, intClientid, intTranType;
                    cmpsrProductSubs.ValidateField();

                    int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);


                    if (_ComboJenisSubs.Text == "By %")
                    {
                        ByPercent = true;
                    }
                    else
                    {
                        ByPercent = false;
                    }

                    if (IsSubsNew)
                    {
                        intTranType = 1;
                        intClientid = 0;
                    }
                    else
                    {
                        intTranType = 2;
                        cmpsrClientSubs.ValidateField();
                        int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
                    }

                    HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
                        checkFeeEditSubs.Checked, nispPercentageFeeSubs.Value, 1, out strFeeCurr,
                        out decNominalFee, out decPctFee, cmpsrCIFSubs.Text1);

                    nispMoneyFeeSubs.Value = decNominalFee;
                    nispPercentageFeeSubs.Value = decPctFee;
                    labelFeeCurrencySubs.Text = "%";
                    _KeteranganFeeSubs.Text = strFeeCurr;
                }
            }
            catch
            {
                return;
            }
            //20150818, liliana, LIBST13020, end
        }

        private void _ComboJenisBooking_SelectedIndexChanged(object sender, EventArgs e)
        {
            nispMoneyFeeBooking.Value = 0;
            nispPercentageFeeBooking.Value = 0;

            if (_ComboJenisBooking.Text == "By %")
            {
                _KeteranganFeeBooking.Text = cmpsrCurrBooking.Text1;
                labelFeeCurrencyBooking.Text = "%";
                ByPercent = true;
                //20220802, Lita, RDN-825, begin
                //nispMoneyFeeBooking.DecimalPlace = 3;
                nispMoneyFeeBooking.DecimalPlace = 4;
                //20220802, Lita, RDN-825, end
            }
            else
            {
                _KeteranganFeeBooking.Text = "%";
                labelFeeCurrencyBooking.Text = cmpsrCurrBooking.Text1;
                ByPercent = false;
                nispMoneyFeeBooking.DecimalPlace = 2;
            }
        }

        private void nispMoneyFeeBooking_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            //20150820, liliana, LIBST130020, begin
            //if (_ComboJenisBooking.Text == "By %")
            //{
            //    _KeteranganFeeBooking.Text = cmpsrCurrBooking.Text1;
            //    labelFeeCurrencyBooking.Text = "%";
            //    ByPercent = true;
            //}
            //else
            //{
            //    _KeteranganFeeBooking.Text = "%";
            //    labelFeeCurrencyBooking.Text = cmpsrCurrBooking.Text1;
            //    ByPercent = false;
            //}

            //if ((cmpsrProductBooking.Text1 != "") && (nispMoneyNomBooking.Value != 0))
            //{
            //    string strFeeCurr;
            //    decimal decNominalFee, decPctFee;

            //    HitungBookingFee(cmpsrCIFBooking.Text1, nispMoneyNomBooking.Value, cmpsrProductBooking.Text1, ByPercent,
            //        checkFeeEditBooking.Checked, nispMoneyFeeBooking.Value, out decPctFee, out strFeeCurr, out decNominalFee);

            //    if (checkFeeEditBooking.Checked == false) //hitung fee tanpa edit fee
            //    {
            //        nispMoneyFeeBooking.Value = decNominalFee;
            //        nispPercentageFeeBooking.Value = decPctFee;
            //        labelFeeCurrencyBooking.Text = strFeeCurr;
            //        _KeteranganFeeBooking.Text = "%";
            //    }
            //    else if (checkFeeEditBooking.Checked == true) //hitung fee dengan edit fee
            //    {
            //        if (ByPercent)
            //        {
            //            nispPercentageFeeBooking.Value = decNominalFee;
            //            labelFeeCurrencyBooking.Text = "%";
            //            _KeteranganFeeBooking.Text = strFeeCurr;
            //        }
            //        else
            //        {
            //            nispPercentageFeeBooking.Value = decPctFee;
            //            labelFeeCurrencyBooking.Text = strFeeCurr;
            //            _KeteranganFeeBooking.Text = "%";

            //        }
            //    }

            //}
            //20150820, liliana, LIBST130020, end
        }

        private void cmpsrNoRefSubs_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRefSubs.Text2 = "";
            //20150410, liliana, LIBST13020, begin
            //ResetFormSubs();
            //20150410, liliana, LIBST13020, end
        }

        private void cmpsrNoRefRedemp_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRefRedemp.Text2 = "";
            //20150410, liliana, LIBST13020, begin
            //ResetFormRedemp();
            //20150410, liliana, LIBST13020, end
        }

        private void cmpsrNoRefRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRefRDB.Text2 = "";
            //20150410, liliana, LIBST13020, begin
            //ResetFormRDB();
            //20150410, liliana, LIBST13020, end
        }

        private void cmpsrNoRefSwc_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRefSwc.Text2 = "";
            //20150410, liliana, LIBST13020, begin
            //ResetFormSwc();
            //20150410, liliana, LIBST13020, end
        }

        private void cmpsrNoRefSwcRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRefSwcRDB.Text2 = "";
            //20150410, liliana, LIBST13020, begin
            //ResetFormSwcRDB();
            //20150410, liliana, LIBST13020, end
        }

        private void cmpsrNoRefBooking_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRefBooking.Text2 = "";
            //20150410, liliana, LIBST13020, begin
            //ResetFormBooking();
            //20150410, liliana, LIBST13020, end
        }

        private void nispMoneyFeeSubs_KeyDown(object sender, KeyEventArgs e)
        {
            //try
            //{
            //    if (_ComboJenisSubs.Text == "By %")
            //    {
            //        _KeteranganFeeSubs.Text = cmpsrCurrSubs.Text1;
            //        labelFeeCurrencySubs.Text = "%";
            //        ByPercent = true;
            //    }
            //    else
            //    {
            //        _KeteranganFeeSubs.Text = "%";
            //        labelFeeCurrencySubs.Text = cmpsrCurrSubs.Text1;
            //        ByPercent = false;
            //    }

            //    if ((cmpsrProductSubs.Text1 != "") && (nispMoneyNomSubs.Value != 0))
            //    {
            //        decimal decNominalFee, decPctFee;
            //        int intProdId, intClientid, intTranType;
            //        int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);


            //        if (IsSubsNew)
            //        {
            //            intTranType = 1;
            //            intClientid = 0;
            //        }
            //        else
            //        {
            //            intTranType = 2;
            //            int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
            //        }

            //        HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
            //            checkFeeEditSubs.Checked, nispMoneyFeeSubs.Value, 2, out strFeeCurr, out decNominalFee,
            //            out decPctFee, cmpsrCIFSubs.Text1);

            //        nispPercentageFeeSubs.Value = decPctFee;

            //    }
            //}
            //catch
            //{
            //    return;
            //}
        }

        private void checkFeeEditSubs_Click(object sender, EventArgs e)
        {
            //if (checkFeeEditSubs.Checked)
            //{
            //    _ComboJenisSubs.Enabled = true;
            //    nispMoneyFeeSubs.Enabled = true;
            //    nispPercentageFeeSubs.Enabled = false;
            //}
            //else
            //{
            //    _ComboJenisSubs.Enabled = false;
            //    nispMoneyFeeSubs.Enabled = false;
            //    nispPercentageFeeSubs.Enabled = false;

            //    _ComboJenisSubs.SelectedIndex = 0;
            //    nispMoneyFeeSubs.Value = (decimal)Fee;

            //    decimal decNominalFee, decPctFee;
            //    int intProdId, intClientid, intTranType;
            //    int.TryParse(cmpsrProductSubs[2].ToString(), out intProdId);


            //    if (IsSubsNew)
            //    {
            //        intTranType = 1;
            //        intClientid = 0;
            //    }
            //    else
            //    {
            //        intTranType = 2;
            //        int.TryParse(cmpsrClientSubs[2].ToString(), out intClientid);
            //    }

            //    HitungFee(intProdId, intClientid, intTranType, nispMoneyNomSubs.Value, 0, checkFullAmtSubs.Checked,
            //        checkFeeEditSubs.Checked, nispPercentageFeeSubs.Value, 1, out strFeeCurr, out decNominalFee,
            //        out decPctFee, cmpsrCIFSubs.Text1);

            //    nispMoneyFeeSubs.Value = decNominalFee;
            //    nispPercentageFeeSubs.Value = decPctFee;
            //    labelFeeCurrencySubs.Text = strFeeCurr;

            //}
        }

        //20150410, liliana, LIBST13020, begin
        private void cmpsrClientSwcIn_onNispText2Changed(object sender, EventArgs e)
        {
            if (!IsSubsNew)
            {
                try
                {
                    //tampilin oustanding unit
                    int intClientCode;
                    int.TryParse(cmpsrClientSwcIn[2].ToString(), out intClientCode);
                    OutstandingUnitSwcIn = GetLatestBalance(intClientCode);

                }
                catch
                {
                    return;
                }
            }
            else
            {
                OutstandingUnitSwcIn = 0;
            }
        }

        private void cmpsrClientSwcIn_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrClientSwcIn.Text2 = "";
        }

        //20150413, liliana, LIBST13020, begin
        private void cmpsrNoRefSubs_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrNoRefSubs.Text1 == "" && cmpsrNoRefSubs.Text2 == "")
            {
                ResetFormSubs();
            }
        }

        private void cmpsrNoRefRedemp_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrNoRefRedemp.Text1 == "" && cmpsrNoRefRedemp.Text2 == "")
            {
                ResetFormRedemp();
            }
        }

        private void cmpsrNoRefRDB_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrNoRefRDB.Text1 == "" && cmpsrNoRefRDB.Text2 == "")
            {
                ResetFormRDB();
            }
        }

        private void cmpsrNoRefSwc_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrNoRefSwc.Text1 == "" && cmpsrNoRefSwc.Text2 == "")
            {
                ResetFormSwc();
            }
        }

        private void cmpsrNoRefSwcRDB_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrNoRefSwcRDB.Text1 == "" && cmpsrNoRefSwcRDB.Text2 == "")
            {
                ResetFormSwcRDB();
            }
        }

        private void cmpsrNoRefBooking_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrNoRefBooking.Text1 == "" && cmpsrNoRefBooking.Text2 == "")
            {
                ResetFormBooking();
            }
        }

        //20150427, liliana, LIBST13020, begin
        private void checkSwcAll_CheckedChanged(object sender, EventArgs e)
        {
            if (checkSwcAll.Checked)
            {
                nispRedempSwc.Value = nispOutstandingUnitSwc.Value;
                nispRedempSwc.Enabled = false;
                IsSwitchingAll = true;
            }
            else
            {
                nispRedempSwc.Value = 0;
                nispRedempSwc.Enabled = true;
                IsSwitchingAll = false;
            }
            //20150622, liliana, LIBST13020, begin
            try
            {
                string FeeCCY;
                decimal decNominalFee, decPctFee;

                HitungSwitchingFee(cmpsrProductSwcOut.Text1, cmpsrProductSwcIn.Text1, true, 0,
                    nispRedempSwc.Value, checkFeeEditSwc.Checked, nispPercentageFeeSwc.Value,
                    //20210309, joshua, RDN-466, begin
                    //cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee);
                    cmpsrClientSwcOut[5].ToString(), out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwc.Text1);
                //20210309, joshua, RDN-466, end

                nispMoneyFeeSwc.Value = decNominalFee;
                nispPercentageFeeSwc.Value = decPctFee;
                labelFeeCurrencySwc.Text = FeeCCY;
            }
            catch
            {
                return;
            }
            //20150622, liliana, LIBST13020, end
        }

        //20150505, liliana, LIBST13020, begin
        private void nispMoneyNomBooking_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            nispMoneyFeeBooking.Value = 0;
            labelFeeCurrencyBooking.Text = "";
            nispPercentageFeeBooking.Value = 0;
            _KeteranganFeeBooking.Text = "";
            checkFeeEditBooking.Checked = false;

            if (checkFeeEditBooking.Checked == false)
            {
                _ComboJenisBooking.Enabled = false;
                nispMoneyFeeBooking.Enabled = false;
            }
            //20150813, liliana, LIBST13020, begin

            if ((nispMoneyNomBooking.Value != 0) && (cmpsrCIFBooking.Text1 != "")
                && (cmpsrProductBooking.Text1 != "")
                )
            {
                string strFeeCurr;
                decimal decNominalFee, decPctFee;

                HitungBookingFee(cmpsrCIFBooking.Text1, nispMoneyNomBooking.Value, cmpsrProductBooking.Text1, ByPercent,
                    checkFeeEditBooking.Checked, nispMoneyFeeBooking.Value, out decPctFee, out strFeeCurr, out decNominalFee);

                if (checkFeeEditBooking.Checked == false) //hitung fee tanpa edit fee
                {
                    nispMoneyFeeBooking.Value = decNominalFee;
                    nispPercentageFeeBooking.Value = decPctFee;
                    labelFeeCurrencyBooking.Text = strFeeCurr;
                    _KeteranganFeeBooking.Text = "%";
                }
                else if (checkFeeEditBooking.Checked == true) //hitung fee dengan edit fee
                {
                    if (ByPercent)
                    {
                        nispPercentageFeeBooking.Value = decNominalFee;
                        labelFeeCurrencyBooking.Text = "%";
                        _KeteranganFeeBooking.Text = strFeeCurr;
                    }
                    else
                    {
                        nispPercentageFeeBooking.Value = decPctFee;
                        labelFeeCurrencyBooking.Text = strFeeCurr;
                        _KeteranganFeeBooking.Text = "%";

                    }
                }

            }
            //20150813, liliana, LIBST13020, end
        }

        //20150515, liliana, LIBST13020, begin
        private void btnDocumentSubs_Click(object sender, EventArgs e)
        {
            objFormDocument._menuName = strMenuName;
            objFormDocument.Show();
            objFormDocument.BringToFront();
        }

        private void btnDokumenRedemp_Click(object sender, EventArgs e)
        {
            objFormDocument._menuName = strMenuName;
            objFormDocument.Show();
            objFormDocument.BringToFront();
        }

        private void btnDokumenRDB_Click(object sender, EventArgs e)
        {
            objFormDocument._menuName = strMenuName;
            objFormDocument.Show();
            objFormDocument.BringToFront();
        }

        private void btnDokumenSwc_Click(object sender, EventArgs e)
        {
            objFormDocument._menuName = strMenuName;
            objFormDocument.Show();
            objFormDocument.BringToFront();
        }

        private void btnDokumenSwcRDB_Click(object sender, EventArgs e)
        {
            objFormDocument._menuName = strMenuName;
            objFormDocument.Show();
            objFormDocument.BringToFront();
        }

        private void btnBooking_Click(object sender, EventArgs e)
        {
            objFormDocument._menuName = strMenuName;
            objFormDocument.Show();
            objFormDocument.BringToFront();
        }

        //20150619, liliana, LIBST13020, begin

        private void cmbAutoRedempRDB_Validating(object sender, CancelEventArgs e)
        {
            if ((cmbAutoRedempRDB.Text == "YA") && (cmbAutoRedempRDB.Enabled = true))
            {
                if (MessageBox.Show("Apakah yakin ingin memilih Auto Redemption?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    cmbAutoRedempRDB.SelectedIndex = 1;
                    return;
                }
            }
        }

        //20150710, liliana, LIBST13020, begin
        private void nispPercentageFeeSwcRDB_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            //20150921, liliana, LIBST13020, begin
            //if (nispRedempSwcRDB.Value != 0)
            if ((nispRedempSwcRDB.Value != 0) && (cmpsrProductSwcRDBOut.Text1 != "")
                && (cmpsrClientSwcRDBOut.Text1 != "")
                )
            //20150921, liliana, LIBST13020, end
            {
                try
                {
                    string FeeCCY;
                    decimal decNominalFee, decPctFee;
                    int ProdSwcOut, ClientSwcOut;
                    //20220622, sandi, RDN-802, begin
                    int ProdSwcIn;

                    if (cmpsrProductSwcRDBIn.Text1 == "" && nispRedempSwcRDB.Enabled == true)
                    {
                        MessageBox.Show("Silahkan isi terlebih dahulu produk Switch In!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nispRedempSwcRDB.Text = "0";
                        return;
                    }

                    string strPrdIdIn = GetImportantData("PRODUKID", cmpsrProductSwcRDBIn.Text1);
                    int.TryParse(strPrdIdIn, out ProdSwcIn);
                    //20220622, sandi, RDN-802, end
                    //20150828, liliana, LIBST13020, begin
                    //int.TryParse(cmpsrProductSwcRDBOut[2].ToString(), out ProdSwcOut);
                    //int.TryParse(cmpsrClientSwcRDBOut[2].ToString(), out ClientSwcOut);
                    string strPrdId = GetImportantData("PRODUKID", cmpsrProductSwcRDBOut.Text1);
                    int.TryParse(strPrdId, out ProdSwcOut);

                    string strClientId = GetImportantData("CLIENTID", cmpsrClientSwcRDBOut.Text1);
                    int.TryParse(strClientId, out ClientSwcOut);
                    //20150828, liliana, LIBST13020, end

                    HitungSwitchingRDBFee(ProdSwcOut, ClientSwcOut, nispRedempSwcRDB.Value, checkFeeEditSwcRDB.Checked,
                        //20210309, joshua, RDN-466, begin
                        //nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee);
                        //20220622, sandi, RDN-802, begin
                        //nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1);
                        nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1
                        , ProdSwcIn, cmpsrNoRefSwcRDB.Text1.Trim());
                    //20220622, sandi, RDN-802, end
                    //20210309, joshua, RDN-466, end

                    nispMoneyFeeSwcRDB.Value = decNominalFee;
                    nispPercentageFeeSwcRDB.Value = decPctFee;
                    labelFeeCurrencySwcRDB.Text = FeeCCY;
                }
                catch
                {
                    return;
                }
            }
        }
        //20150827, liliana, LIBST13020, begin
        private string GetImportantData(string CariApa, string InputData)
        {
            string OutputData = "";
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCariApa", System.Data.OleDb.OleDbType.VarChar, 50);
            dbParam[0].Value = CariApa;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcInput", System.Data.OleDb.OleDbType.VarChar, 100);
            dbParam[1].Value = InputData;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@cValue", System.Data.OleDb.OleDbType.VarChar, 500);
            dbParam[2].Direction = ParameterDirection.Output;

            bool blnResult = ClQ.ExecProc("ReksaGetImportantData", ref dbParam);

            if (blnResult)
            {
                OutputData = dbParam[2].Value.ToString();
            }

            return OutputData;
        }

        //20150827, liliana, LIBST13020, end
        //20150710, liliana, LIBST13020, end
        //20150619, liliana, LIBST13020, end
        //20150515, liliana, LIBST13020, end
        //20150505, liliana, LIBST13020, end
        //20150427, liliana, LIBST13020, end
        //20150413, liliana, LIBST13020, end
        //20150410, liliana, LIBST13020, end

        //20160509, Elva, CSODD16117, begin
        private void SetEnableOfficeId(string strKodeKantor)
        {
            string strIsEnable = "", strErrorMessage = "";
            if (clsValidator.ValidasiCBOKodeKantor(ClQ, strKodeKantor, out strIsEnable, out strErrorMessage))
            {
                cmpsrKodeKantorSubs.Text1 = strKodeKantor;
                cmpsrKodeKantorRedemp.Text1 = strKodeKantor;
                cmpsrKodeKantorRDB.Text1 = strKodeKantor;
                cmpsrKodeKantorSwc.Text1 = strKodeKantor;
                cmpsrKodeKantorSwcRDB.Text1 = strKodeKantor;
                cmpsrKodeKantorBooking.Text1 = strKodeKantor;

                if (strIsEnable == "1")
                {
                    cmpsrKodeKantorSubs.Enabled = true;
                    cmpsrKodeKantorSubs.ReadOnly = false;

                    cmpsrKodeKantorRedemp.Enabled = true;
                    cmpsrKodeKantorRedemp.ReadOnly = false;

                    cmpsrKodeKantorRDB.Enabled = true;
                    cmpsrKodeKantorRDB.ReadOnly = false;

                    cmpsrKodeKantorSwc.Enabled = true;
                    cmpsrKodeKantorSwc.ReadOnly = false;

                    cmpsrKodeKantorSwcRDB.Enabled = true;
                    cmpsrKodeKantorSwcRDB.ReadOnly = false;

                    cmpsrKodeKantorBooking.Enabled = true;
                    cmpsrKodeKantorBooking.ReadOnly = false;
                }
                else
                {
                    cmpsrKodeKantorSubs.Enabled = false;
                    cmpsrKodeKantorSubs.ReadOnly = true;

                    cmpsrKodeKantorRedemp.Enabled = false;
                    cmpsrKodeKantorRedemp.ReadOnly = true;

                    cmpsrKodeKantorRDB.Enabled = false;
                    cmpsrKodeKantorRDB.ReadOnly = true;

                    cmpsrKodeKantorSwc.Enabled = false;
                    cmpsrKodeKantorSwc.ReadOnly = true;

                    cmpsrKodeKantorSwcRDB.Enabled = false;
                    cmpsrKodeKantorSwcRDB.ReadOnly = true;

                    cmpsrKodeKantorBooking.Enabled = false;
                    cmpsrKodeKantorBooking.ReadOnly = true;
                }
            }
        }

        private void cmpsrKodeKantorSubs_onNispText2Changed(object sender, EventArgs e)
        {
            string strIsAllowed = "";
            if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorSubs.Text1, out strIsAllowed))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetFormSubs();
                }
            }
            else
                MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmpsrKodeKantorRedemp_onNispText2Changed(object sender, EventArgs e)
        {
            string strIsAllowed = "";
            if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorRedemp.Text1, out strIsAllowed))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetFormRedemp();
                }
            }
            else
                MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmpsrKodeKantorRDB_onNispText2Changed(object sender, EventArgs e)
        {
            string strIsAllowed = "";
            if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorRDB.Text1, out strIsAllowed))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetFormRDB();
                }
            }
            else
                MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmpsrKodeKantorSwc_onNispText2Changed(object sender, EventArgs e)
        {
            string strIsAllowed = "";
            if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorSwc.Text1, out strIsAllowed))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetFormSwc();
                }
            }
            else
                MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmpsrKodeKantorSwcRDB_onNispText2Changed(object sender, EventArgs e)
        {
            string strIsAllowed = "";
            if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorSwcRDB.Text1, out strIsAllowed))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetFormSwcRDB();
                }
            }
            else
                MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmpsrKodeKantorBooking_onNispText2Changed(object sender, EventArgs e)
        {
            string strIsAllowed = "";
            if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantorBooking.Text1, out strIsAllowed))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetFormBooking();
                }
            }
            else
                MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ResetAllKodeKantor()
        {
            cmpsrKodeKantorSubs.Text1 = strBranch;
            cmpsrKodeKantorRedemp.Text1 = strBranch;
            cmpsrKodeKantorRDB.Text1 = strBranch;
            cmpsrKodeKantorSwc.Text1 = strBranch;
            cmpsrKodeKantorSwcRDB.Text1 = strBranch;
            cmpsrKodeKantorBooking.Text1 = strBranch;
        }

        //20160509, Elva, CSODD16117, end
        //20160829, liliana, LOGEN00196, begin
        private void cmbTASubs_SelectedIndexChanged(object sender, EventArgs e)
        {
            string _isTA;
            string AccountIdTA, AccountIdUSDTA, AccountIdMCTA;
            string AccountNameTA, AccountNameUSDTA, AccountNameMCTA;
            string AccountId, AccountIdUSD, AccountIdMC;
            string AccountName, AccountNameUSD, AccountNameMC;

            CheckCIFTaxAmnesty(cmpsrCIFSubs.Text1, out _isTA, out AccountIdTA, out AccountIdUSDTA, out AccountIdMCTA
                , out AccountNameTA, out AccountNameUSDTA, out AccountNameMCTA
                , out AccountId, out AccountIdUSD, out AccountIdMC
                , out AccountName, out AccountNameUSD, out AccountNameMC
                );

            if (cmbTASubs.SelectedIndex == 1)
            {
                if (_isTA == "0")
                {
                    MessageBox.Show("Jenis transaksi Tax Amnesty hanya bisa dilakukan oleh CIF dengan flag Tax Amnesty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbTASubs.SelectedIndex = 0;
                    return;
                }
                else if (_isTA == "1")
                {
                    maskedRekeningSubs.Text = AccountIdTA;
                    textNamaRekeningSubs.Text = AccountNameTA;
                    maskedRekeningSubsUSD.Text = AccountIdUSDTA;
                    textNamaRekeningSubsUSD.Text = AccountNameUSDTA;
                    maskedRekeningSubsMC.Text = AccountIdMCTA;
                    textNamaRekeningSubsMC.Text = AccountNameMCTA;
                }
            }
            else
            {
                maskedRekeningSubs.Text = AccountId;
                textNamaRekeningSubs.Text = AccountName;
                maskedRekeningSubsUSD.Text = AccountIdUSD;
                textNamaRekeningSubsUSD.Text = AccountNameUSD;
                maskedRekeningSubsMC.Text = AccountIdMC;
                textNamaRekeningSubsMC.Text = AccountNameMC;
            }

            cmpsrProductSubs.Text1 = "";

            //20210922, korvi, RDN-674, begin

            cmpsrNoRekSubs.Criteria = cmpsrCIFSubs.Text1 + "#" + cmbTASubs.SelectedIndex.ToString();
            cmpsrNoRekSubs.Text1 = "";
            cmpsrNoRekSubs.ValidateField();

            //20210922, korvi, RDN-674, end
        }

        private void cmbTARedemp_SelectedIndexChanged(object sender, EventArgs e)
        {
            string _isTA;
            string AccountIdTA, AccountIdUSDTA, AccountIdMCTA;
            string AccountNameTA, AccountNameUSDTA, AccountNameMCTA;
            string AccountId, AccountIdUSD, AccountIdMC;
            string AccountName, AccountNameUSD, AccountNameMC;

            CheckCIFTaxAmnesty(cmpsrCIFRedemp.Text1, out _isTA, out AccountIdTA, out AccountIdUSDTA, out AccountIdMCTA
                , out AccountNameTA, out AccountNameUSDTA, out AccountNameMCTA
                , out AccountId, out AccountIdUSD, out AccountIdMC
                , out AccountName, out AccountNameUSD, out AccountNameMC
                );

            if (cmbTARedemp.SelectedIndex == 1)
            {
                if (_isTA == "0")
                {
                    //20161108, liliana, CSODD16311, begin
                    //MessageBox.Show("Jenis transaksi Tax Amnesty hanya bisa dilakukan oleh CIF dengan flag Tax Amnesty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //cmbTARedemp.SelectedIndex = 0;
                    //20161108, liliana, CSODD16311, end
                    return;
                }
                else if (_isTA == "1")
                {
                    maskedRekeningRedemp.Text = AccountIdTA;
                    textNamaRekeningRedemp.Text = AccountNameTA;
                    maskedRekeningRedempUSD.Text = AccountIdUSDTA;
                    textNamaRekeningRedempUSD.Text = AccountNameUSDTA;
                    maskedRekeningRedempMC.Text = AccountIdMCTA;
                    textNamaRekeningRedempMC.Text = AccountNameMCTA;
                }
            }
            else
            {
                maskedRekeningRedemp.Text = AccountId;
                textNamaRekeningRedemp.Text = AccountName;
                maskedRekeningRedempUSD.Text = AccountIdUSD;
                textNamaRekeningRedempUSD.Text = AccountNameUSD;
                maskedRekeningRedempMC.Text = AccountIdMC;
                textNamaRekeningRedempMC.Text = AccountNameMC;
            }

            cmpsrProductRedemp.Text1 = "";

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekRedemp.Criteria = cmpsrCIFRedemp.Text1 + "#" + cmbTARedemp.SelectedIndex.ToString();
            cmpsrNoRekRedemp.Text1 = "";
            cmpsrNoRekRedemp.ValidateField();
            //20210922, korvi, RDN-674, end

        }

        private void cmbTARDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string _isTA;
            string AccountIdTA, AccountIdUSDTA, AccountIdMCTA;
            string AccountNameTA, AccountNameUSDTA, AccountNameMCTA;
            string AccountId, AccountIdUSD, AccountIdMC;
            string AccountName, AccountNameUSD, AccountNameMC;

            CheckCIFTaxAmnesty(cmpsrCIFRDB.Text1, out _isTA, out AccountIdTA, out AccountIdUSDTA, out AccountIdMCTA
                , out AccountNameTA, out AccountNameUSDTA, out AccountNameMCTA
                , out AccountId, out AccountIdUSD, out AccountIdMC
                , out AccountName, out AccountNameUSD, out AccountNameMC
                );

            if (cmbTARDB.SelectedIndex == 1)
            {
                if (_isTA == "0")
                {
                    MessageBox.Show("Jenis transaksi Tax Amnesty hanya bisa dilakukan oleh CIF dengan flag Tax Amnesty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbTARDB.SelectedIndex = 0;
                    return;
                }
                else if (_isTA == "1")
                {
                    maskedRekeningRDB.Text = AccountIdTA;
                    textNamaRekeningRDB.Text = AccountNameTA;
                    maskedRekeningRDBUSD.Text = AccountIdUSDTA;
                    textNamaRekeningRDBUSD.Text = AccountNameUSDTA;
                    maskedRekeningRDBMC.Text = AccountIdMCTA;
                    textNamaRekeningRDBMC.Text = AccountNameMCTA;
                }
            }
            else
            {
                maskedRekeningRDB.Text = AccountId;
                textNamaRekeningRDB.Text = AccountName;
                maskedRekeningRDBUSD.Text = AccountIdUSD;
                textNamaRekeningRDBUSD.Text = AccountNameUSD;
                maskedRekeningRDBMC.Text = AccountIdMC;
                textNamaRekeningRDBMC.Text = AccountNameMC;
            }

            cmpsrProductRDB.Text1 = "";

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekRDB.Criteria = cmpsrCIFRDB.Text1 + "#" + cmbTARDB.SelectedIndex.ToString();
            cmpsrNoRekRDB.Text1 = "";
            cmpsrNoRekRDB.ValidateField();
            //20210922, korvi, RDN-674, end
        }

        private void cmbTASwc_SelectedIndexChanged(object sender, EventArgs e)
        {
            string _isTA;
            string AccountIdTA, AccountIdUSDTA, AccountIdMCTA;
            string AccountNameTA, AccountNameUSDTA, AccountNameMCTA;
            string AccountId, AccountIdUSD, AccountIdMC;
            string AccountName, AccountNameUSD, AccountNameMC;

            CheckCIFTaxAmnesty(cmpsrCIFSwc.Text1, out _isTA, out AccountIdTA, out AccountIdUSDTA, out AccountIdMCTA
                , out AccountNameTA, out AccountNameUSDTA, out AccountNameMCTA
                , out AccountId, out AccountIdUSD, out AccountIdMC
                , out AccountName, out AccountNameUSD, out AccountNameMC
                );

            if (cmbTASwc.SelectedIndex == 1)
            {
                if (_isTA == "0")
                {
                    MessageBox.Show("Jenis transaksi Tax Amnesty hanya bisa dilakukan oleh CIF dengan flag Tax Amnesty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbTASwc.SelectedIndex = 0;
                    return;
                }
                else if (_isTA == "1")
                {
                    maskedRekeningSwc.Text = AccountIdTA;
                    textNamaRekeningSwc.Text = AccountNameTA;
                    maskedRekeningSwcUSD.Text = AccountIdUSDTA;
                    textNamaRekeningSwcUSD.Text = AccountNameUSDTA;
                    maskedRekeningSwcMC.Text = AccountIdMCTA;
                    textNamaRekeningSwcMC.Text = AccountNameMCTA;
                }
            }
            else
            {
                maskedRekeningSwc.Text = AccountId;
                textNamaRekeningSwc.Text = AccountName;
                maskedRekeningSwcUSD.Text = AccountIdUSD;
                textNamaRekeningSwcUSD.Text = AccountNameUSD;
                maskedRekeningSwcMC.Text = AccountIdMC;
                textNamaRekeningSwcMC.Text = AccountNameMC;
            }

            cmpsrProductSwcIn.Text1 = "";
            cmpsrProductSwcOut.Text1 = "";

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSwc.Criteria = cmpsrCIFSwc.Text1 + "#" + cmbTASwc.SelectedIndex.ToString();
            cmpsrNoRekSwc.Text1 = "";
            cmpsrNoRekSwc.ValidateField();
            //20210922, korvi, RDN-674, end
        }

        //20210922, korvi, RDN-674, begin
        private void cmpsrNoRekSubs_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRekSubs.Text2 = "";
        }

        private void cmpsrNoRekRedemp_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRekRedemp.Text2 = "";
        }

        private void cmpsrNoRekSwc_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRekSwc.Text2 = "";
        }

        private void cmpsrNoRekSwcRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRekSwcRDB.Text2 = "";
        }

        private void cmpsrNoRekBooking_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRekBooking.Text2 = "";
        }

        private void cmpsrNoRekRDB_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrNoRekRDB.Text2 = "";
        }
        //20210922, korvi, RDN-674, end

        private void cmbTASwcRDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string _isTA;
            string AccountIdTA, AccountIdUSDTA, AccountIdMCTA;
            string AccountNameTA, AccountNameUSDTA, AccountNameMCTA;
            string AccountId, AccountIdUSD, AccountIdMC;
            string AccountName, AccountNameUSD, AccountNameMC;

            CheckCIFTaxAmnesty(cmpsrCIFSwcRDB.Text1, out _isTA, out AccountIdTA, out AccountIdUSDTA, out AccountIdMCTA
                , out AccountNameTA, out AccountNameUSDTA, out AccountNameMCTA
                , out AccountId, out AccountIdUSD, out AccountIdMC
                , out AccountName, out AccountNameUSD, out AccountNameMC
                );

            if (cmbTASwcRDB.SelectedIndex == 1)
            {
                if (_isTA == "0")
                {
                    MessageBox.Show("Jenis transaksi Tax Amnesty hanya bisa dilakukan oleh CIF dengan flag Tax Amnesty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbTASwcRDB.SelectedIndex = 0;
                    return;
                }
                else if (_isTA == "1")
                {
                    maskedRekeningSwcRDB.Text = AccountIdTA;
                    textNamaRekeningSwcRDB.Text = AccountNameTA;
                    maskedRekeningSwcRDBUSD.Text = AccountIdUSDTA;
                    textNamaRekeningSwcRDBUSD.Text = AccountNameUSDTA;
                    maskedRekeningSwcRDBMC.Text = AccountIdMCTA;
                    textNamaRekeningSwcRDBMC.Text = AccountNameMCTA;
                }
            }
            else
            {
                maskedRekeningSwcRDB.Text = AccountId;
                textNamaRekeningSwcRDB.Text = AccountName;
                maskedRekeningSwcRDBUSD.Text = AccountIdUSD;
                textNamaRekeningSwcRDBUSD.Text = AccountNameUSD;
                maskedRekeningSwcRDBMC.Text = AccountIdMC;
                textNamaRekeningSwcRDBMC.Text = AccountNameMC;
            }

            cmpsrProductSwcRDBIn.Text1 = "";
            cmpsrProductSwcRDBOut.Text1 = "";

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekSwcRDB.Criteria = cmpsrCIFSwcRDB.Text1 + "#" + cmbTASwcRDB.SelectedIndex.ToString();
            cmpsrNoRekSwcRDB.Text1 = "";
            cmpsrNoRekSwcRDB.ValidateField();
            //20210922, korvi, RDN-674, end
        }

        private void cmbTABook_SelectedIndexChanged(object sender, EventArgs e)
        {
            string _isTA;
            string AccountIdTA, AccountIdUSDTA, AccountIdMCTA;
            string AccountNameTA, AccountNameUSDTA, AccountNameMCTA;
            string AccountId, AccountIdUSD, AccountIdMC;
            string AccountName, AccountNameUSD, AccountNameMC;

            CheckCIFTaxAmnesty(cmpsrCIFBooking.Text1, out _isTA, out AccountIdTA, out AccountIdUSDTA, out AccountIdMCTA
                , out AccountNameTA, out AccountNameUSDTA, out AccountNameMCTA
                , out AccountId, out AccountIdUSD, out AccountIdMC
                , out AccountName, out AccountNameUSD, out AccountNameMC
                );

            if (cmbTABook.SelectedIndex == 1)
            {
                if (_isTA == "0")
                {
                    MessageBox.Show("Jenis transaksi Tax Amnesty hanya bisa dilakukan oleh CIF dengan flag Tax Amnesty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbTABook.SelectedIndex = 0;
                    return;
                }
                else if (_isTA == "1")
                {
                    maskedRekeningBooking.Text = AccountIdTA;
                    textNamaRekeningBooking.Text = AccountNameTA;
                    maskedRekeningBookingUSD.Text = AccountIdUSDTA;
                    textNamaRekeningBookingUSD.Text = AccountNameUSDTA;
                    maskedRekeningBookingMC.Text = AccountIdMCTA;
                    textNamaRekeningBookingMC.Text = AccountNameMCTA;
                }
            }
            else
            {
                maskedRekeningBooking.Text = AccountId;
                textNamaRekeningBooking.Text = AccountName;
                maskedRekeningBookingUSD.Text = AccountIdUSD;
                textNamaRekeningBookingUSD.Text = AccountNameUSD;
                maskedRekeningBookingMC.Text = AccountIdMC;
                textNamaRekeningBookingMC.Text = AccountNameMC;
            }
            cmpsrProductBooking.Text1 = "";

            //20210922, korvi, RDN-674, begin
            cmpsrNoRekBooking.Criteria = cmpsrCIFBooking.Text1 + "#" + cmbTABook.SelectedIndex.ToString();
            cmpsrNoRekBooking.Text1 = "";
            cmpsrNoRekBooking.ValidateField();
            //20210922, korvi, RDN-674, end
        }
        //20160829, liliana, LOGEN00196, end
        //20161004, liliana, CSODD16311, begin

        private void OnTimerTick(Object sender, EventArgs eventargs)
        {
            if (lblTaxAmnestySubs.ForeColor == Color.Black)
                lblTaxAmnestySubs.ForeColor = Color.Red;
            else
                lblTaxAmnestySubs.ForeColor = Color.Black;

            if (lblTaxAmnestyRedemp.ForeColor == Color.Black)
                lblTaxAmnestyRedemp.ForeColor = Color.Red;
            else
                lblTaxAmnestyRedemp.ForeColor = Color.Black;

            if (lblTaxAmnestyRDB.ForeColor == Color.Black)
                lblTaxAmnestyRDB.ForeColor = Color.Red;
            else
                lblTaxAmnestyRDB.ForeColor = Color.Black;

            if (lblTaxAmnestySwc.ForeColor == Color.Black)
                lblTaxAmnestySwc.ForeColor = Color.Red;
            else
                lblTaxAmnestySwc.ForeColor = Color.Black;

            if (lblTaxAmnestySwcRDB.ForeColor == Color.Black)
                lblTaxAmnestySwcRDB.ForeColor = Color.Red;
            else
                lblTaxAmnestySwcRDB.ForeColor = Color.Black;

            if (lblTaxAmnestyBooking.ForeColor == Color.Black)
                lblTaxAmnestyBooking.ForeColor = Color.Red;
            else
                lblTaxAmnestyBooking.ForeColor = Color.Black;

        }

        //20161004, liliana, CSODD16311, end


        //20200408, Lita, RDN-88, begin
        private void cmbFrekDebetMethodRDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if ((cmbFrekDebetMethodRDB.Text != "System.Data.DataRowView") || (cmbFrekDebetMethodRDB.Text != ""))
            //{

            if (cmbFrekDebetMethodRDB.Text != "System.Data.DataRowView")
            {

                //20220329, gio, RDN-757, begin
                if (cmbFrekDebetMethodRDB.SelectedValue != null)
                    this._strFreqDebetMethod = cmbFrekDebetMethodRDB.SelectedValue.ToString();
                else
                    this._strFreqDebetMethod = "null";
                //20220325, gio, RDN-757, begin

                string strProdCode;

                strProdCode = cmpsrProductRDB.Text1.ToString();

                //if (_strFreqDebetMethod == "D")
                //    lblFreqDebetRDB.Text = "hari";
                //else if (_strFreqDebetMethod == "W")
                //    lblFreqDebetRDB.Text = "minggu";
                //else if (_strFreqDebetMethod == "M")
                //    lblFreqDebetRDB.Text = "bulan";
                //else
                //    lblFreqDebetRDB.Text = "quartal";

                //lblFreqDebetRDB.Text = cmbFrekPendebetanRDB.Text.ToString();
                ////20220325, gio, RDN-757, end

                //DataTable dt = new DataTable();
                //dt = _dtDataFreqDebetRDB.Clone();

                //DataRow[] drFreqDebetRDB = _dtDataFreqDebetRDB.Select("ComboValue= '" + _strFreqDebetMethod + "'");

                //foreach (DataRow dr in drFreqDebetRDB)
                //{
                //    object[] row = dr.ItemArray;
                //    dt.Rows.Add(row);
                //}

                //cmbFrekPendebetanRDB.DataSource = dt;

                //cmbFrekPendebetanRDB.DisplayMember = "ComboText";
                //cmbFrekPendebetanRDB.ValueMember = "ComboValue";

                //if (cmbFrekPendebetanRDB.Items.Count > 0)
                //    cmbFrekPendebetanRDB.SelectedIndex = 0;

                DataSet _ds = new DataSet();
                if (cmbFrekPendebetanRDB.Text != "System.Data.DataRowView")
                {

                    if (_strFreqDebetMethod != "null")
                        if (PopulateParamComboRDBFrekDebet(strProdCode, _strFreqDebetMethod, out _ds))
                        {
                            if (_ds.Tables[0] != null)
                            {
                                if (_ds.Tables[0].Rows.Count > 0)
                                {
                                    lblFreqDebetRDB.Text = _ds.Tables[0].Rows[0]["FreqDebetMethodDesc"].ToString();
                                    cmbFrekPendebetanRDB.DataSource = _ds.Tables[1];
                                    cmbFrekPendebetanRDB.DisplayMember = "FrekuensiPendebetan";
                                    cmbAsuransiRDB.DataSource = _ds.Tables[2];
                                    cmbAsuransiRDB.DisplayMember = "InsOption";
                                    //cmbAsuransiRDB.SelectedIndex = 0;
                                }
                            }
                        }

                }

                //20220329, gio, RDN-757, end

            }

            //20221017, Andhika J, RDN-861, begin
            string IsAsuransi = "";
            ReksaValidateInsuranceRDB(cmpsrCIFRDB.Text1.Trim(), IsAsuransi);
            //20221017, Andhika J, RDN-861, end
        }

        private void dtTglDebetRDB_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (_intType != 0)
                {

                    //DateTime DebetDate = DateTime.Parse(dtTglDebetRDB.Value.ToShortDateString());
                    //20221220, Lita, RDN-885, Begin
                    //globalJatuhTempoRDB = dtTglDebetRDB.Value.AddMonths((int)nispJangkaWktRDB.Value).AddDays(1);
                    //dtJatuhTempoRDB.Value = Convert.ToInt32(dtTglDebetRDB.Value.AddMonths((int)nispJangkaWktRDB.Value).AddDays(1).ToString("yyyyMMdd"));
                    globalJatuhTempoRDB = dtTglDebetRDB.Value.AddMonths((int)nispJangkaWktRDB.Value);
                    dtJatuhTempoRDB.Value = Convert.ToInt32(dtTglDebetRDB.Value.AddMonths((int)nispJangkaWktRDB.Value).ToString("yyyyMMdd"));
                    //20221220, Lita, RDN-885, end

                }
            }
            catch
            {
                dtJatuhTempoRDB.Value = 0;
            }

        }

        //20200408, Lita, RDN-88, end

        //20220112, sandi, RDN-727, begin
        private void cmpsrNoRekSubs_onNispText2Changed(object sender, EventArgs e)
        {
            //20240906, Lely, RDN-1182, begin
            OleDbParameter[] odp = new OleDbParameter[5];
            (odp[0] = new OleDbParameter("@cCol1", OleDbType.VarChar, 20)).Value = cmpsrNoRekSubs.Text2.Trim();
            (odp[1] = new OleDbParameter("@cCol2", OleDbType.VarChar, 10)).Value = cmpsrCIFSubs[1].ToString();
            (odp[2] = new OleDbParameter("@bValidate", OleDbType.Boolean)).Value = false;
            (odp[3] = new OleDbParameter("@cCriteria", OleDbType.VarChar, 200)).Value = cmpsrCIFSubs.Text1;
            (odp[4] = new OleDbParameter("@cAccNo", OleDbType.VarChar, 19)).Value = cmpsrNoRekSubs.Text1.ToString();

            DataSet ds = new DataSet();
            bool blnResult = ClQ.ExecProc("dbo.ReksaSrcACCTNO_SYR", ref odp, out ds);

            bool isSyariah = false;

            if (blnResult && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                isSyariah = !string.IsNullOrEmpty(ds.Tables[0].Rows[0][4].ToString())
                            && Convert.ToBoolean(ds.Tables[0].Rows[0][4]);

                if (isSyariah &&
                    //20240911, Lely, RDN-1182, begin
                    (_iMode == 1 || _iMode == 2)
                    //20240911, Lely, RDN-1182, end
                    )

                {
                    MessageBox.Show(
                        "Rekening relasi adalah rekening syariah. Rekening ini hanya dapat digunakan untuk produk Reksadana Syariah!",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                        );
                }
            }
            string strCriteria = _strTabName + "#" + cmpsrCIFSubs.Text1 + "#" + cmpsrNoRekSubs.Text2.Trim() + "#" + isSyariah;
            cmpsrProductSubs.SearchDesc = "REKSA_TRXPRODUCT";
            cmpsrProductSubs.Criteria = strCriteria;
            //20240906, Lely, RDN-1182, end
        }

        private void cmpsrNoRekRedemp_onNispText2Changed(object sender, EventArgs e)
        {
            //20240906, Lely, RDN-1182, begin
            OleDbParameter[] odp = new OleDbParameter[5];
            (odp[0] = new OleDbParameter("@cCol1", OleDbType.VarChar, 20)).Value = cmpsrNoRekRedemp.Text2.Trim();
            (odp[1] = new OleDbParameter("@cCol2", OleDbType.VarChar, 10)).Value = cmpsrCIFRedemp[1].ToString();
            (odp[2] = new OleDbParameter("@bValidate", OleDbType.Boolean)).Value = false;
            (odp[3] = new OleDbParameter("@cCriteria", OleDbType.VarChar, 200)).Value = cmpsrCIFRedemp.Text1;
            (odp[4] = new OleDbParameter("@cAccNo", OleDbType.VarChar, 19)).Value = cmpsrNoRekRedemp.Text1.ToString();

            DataSet ds = new DataSet();
            bool blnResult = ClQ.ExecProc("dbo.ReksaSrcACCTNO_SYR", ref odp, out ds);

            bool isSyariah = false;

            if (blnResult && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                isSyariah = !string.IsNullOrEmpty(ds.Tables[0].Rows[0][4].ToString())
                            && Convert.ToBoolean(ds.Tables[0].Rows[0][4]);

                if (isSyariah &&
                    //20240911, Lely, RDN-1182, begin
                    (_iMode == 1 || _iMode == 2)
                    //20240911, Lely, RDN-1182, end
                    )
                {
                    MessageBox.Show(
                        "Rekening relasi adalah rekening syariah. Rekening ini hanya dapat digunakan untuk produk Reksadana Syariah!",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                        );
                }
            }
			//20240906, Lely, RDN-1182, end
            string strCriteria = _strTabName + "#" + cmpsrCIFRedemp.Text1 + "#" + cmpsrNoRekRedemp.Text2.Trim();
            cmpsrProductRedemp.SearchDesc = "REKSA_TRXPRODUCT";
            cmpsrProductRedemp.Criteria = strCriteria;
        }

        private void cmpsrNoRekRDB_onNispText2Changed(object sender, EventArgs e)
        {
            //20240906, Lely, RDN-1182, begin
            OleDbParameter[] odp = new OleDbParameter[5];
            (odp[0] = new OleDbParameter("@cCol1", OleDbType.VarChar, 20)).Value = cmpsrNoRekRDB.Text2.Trim();
            (odp[1] = new OleDbParameter("@cCol2", OleDbType.VarChar, 10)).Value = cmpsrCIFRDB[1].ToString();
            (odp[2] = new OleDbParameter("@bValidate", OleDbType.Boolean)).Value = false;
            (odp[3] = new OleDbParameter("@cCriteria", OleDbType.VarChar, 200)).Value = cmpsrCIFRDB.Text1;
            (odp[4] = new OleDbParameter("@cAccNo", OleDbType.VarChar, 19)).Value = cmpsrNoRekRDB.Text1.ToString();

            DataSet ds = new DataSet();
            bool blnResult = ClQ.ExecProc("dbo.ReksaSrcACCTNO_SYR", ref odp, out ds);

            bool isSyariah = false;

            if (blnResult && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                isSyariah = !string.IsNullOrEmpty(ds.Tables[0].Rows[0][4].ToString())
                            && Convert.ToBoolean(ds.Tables[0].Rows[0][4]);

                if (isSyariah &&
                    //20240911, Lely, RDN-1182, begin
                    (_iMode == 1 || _iMode == 2)
                    //20240911, Lely, RDN-1182, end
                    )
                {
                    MessageBox.Show(
                        "Rekening relasi adalah rekening syariah. Rekening ini hanya dapat digunakan untuk produk Reksadana Syariah!",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                        );
                }
            }
			
            string strCriteria = _strTabName + "#" + cmpsrCIFRDB.Text1 + "#" + cmpsrNoRekRDB.Text2.Trim() + "#" + isSyariah;
            cmpsrProductRDB.SearchDesc = "REKSA_TRXPRODUCT";
            cmpsrProductRDB.Criteria = strCriteria;
            //20240906, Lely, RDN-1182, end
        }

        private void cmpsrNoRekSwc_onNispText2Changed(object sender, EventArgs e)
        {
            //20240906, Lely, RDN-1182, begin
            OleDbParameter[] odp = new OleDbParameter[5];
            (odp[0] = new OleDbParameter("@cCol1", OleDbType.VarChar, 20)).Value = cmpsrNoRekSwc.Text2.Trim();
            (odp[1] = new OleDbParameter("@cCol2", OleDbType.VarChar, 10)).Value = cmpsrCIFSwc[1].ToString();
            (odp[2] = new OleDbParameter("@bValidate", OleDbType.Boolean)).Value = false;
            (odp[3] = new OleDbParameter("@cCriteria", OleDbType.VarChar, 200)).Value = cmpsrCIFSwc.Text1;
            (odp[4] = new OleDbParameter("@cAccNo", OleDbType.VarChar, 19)).Value = cmpsrNoRekSwc.Text1.ToString();

            DataSet ds = new DataSet();
            bool blnResult = ClQ.ExecProc("dbo.ReksaSrcACCTNO_SYR", ref odp, out ds);

            bool isSyariah = false;

            if (blnResult && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                isSyariah = !string.IsNullOrEmpty(ds.Tables[0].Rows[0][4].ToString())
                            && Convert.ToBoolean(ds.Tables[0].Rows[0][4]);

                if (isSyariah &&
                    //20240911, Lely, RDN-1182, begin
                    (_iMode == 1 || _iMode == 2)
                    //20240911, Lely, RDN-1182, end
                    )
                {
                    MessageBox.Show(
                        "Rekening relasi adalah rekening syariah. Rekening ini hanya dapat digunakan untuk produk Reksadana Syariah!",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                        );
                }
            }
			//20240906, Lely, RDN-1182, end
            string strCriteria = _strTabName + "#" + cmpsrCIFSwc.Text1 + "#" + cmpsrNoRekSwc.Text2.Trim();
            cmpsrProductSwcOut.SearchDesc = "REKSA_TRXPRODUCT";
            cmpsrProductSwcOut.Criteria = strCriteria;
        }

        private void cmpsrNoRekSwcRDB_onNispText2Changed(object sender, EventArgs e)
        {
            //20240906, Lely, RDN-1182, begin
            OleDbParameter[] odp = new OleDbParameter[5];
            (odp[0] = new OleDbParameter("@cCol1", OleDbType.VarChar, 20)).Value = cmpsrNoRekSwcRDB.Text2.Trim();
            (odp[1] = new OleDbParameter("@cCol2", OleDbType.VarChar, 10)).Value = cmpsrCIFSwcRDB[1].ToString();
            (odp[2] = new OleDbParameter("@bValidate", OleDbType.Boolean)).Value = false;
            (odp[3] = new OleDbParameter("@cCriteria", OleDbType.VarChar, 200)).Value = cmpsrCIFSwcRDB.Text1;
            (odp[4] = new OleDbParameter("@cAccNo", OleDbType.VarChar, 19)).Value = cmpsrNoRekSwcRDB.Text1.ToString();

            DataSet ds = new DataSet();
            bool blnResult = ClQ.ExecProc("dbo.ReksaSrcACCTNO_SYR", ref odp, out ds);

            bool isSyariah = false;

            if (blnResult && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                isSyariah = !string.IsNullOrEmpty(ds.Tables[0].Rows[0][4].ToString())
                            && Convert.ToBoolean(ds.Tables[0].Rows[0][4]);

                if (isSyariah &&
                    //20240911, Lely, RDN-1182, begin
                    (_iMode == 1 || _iMode == 2)
                    //20240911, Lely, RDN-1182, end
                    )
                {
                    MessageBox.Show(
                        "Rekening relasi adalah rekening syariah. Rekening ini hanya dapat digunakan untuk produk Reksadana Syariah!",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                        );
                }
            }
			//20240906, Lely, RDN-1182, end
            string strCriteria = _strTabName + "#" + cmpsrCIFSwcRDB.Text1 + "#" + cmpsrNoRekSwcRDB.Text2.Trim();
            cmpsrProductSwcRDBOut.SearchDesc = "REKSA_TRXPRODUCT";
            cmpsrProductSwcRDBOut.Criteria = strCriteria;
        }

        private void cmpsrNoRekBooking_onNispText2Changed(object sender, EventArgs e)
        {
            //20240906, Lely, RDN-1182, begin
            OleDbParameter[] odp = new OleDbParameter[5];
            (odp[0] = new OleDbParameter("@cCol1", OleDbType.VarChar, 20)).Value = cmpsrNoRekBooking.Text2.Trim();
            (odp[1] = new OleDbParameter("@cCol2", OleDbType.VarChar, 10)).Value = cmpsrCIFBooking[1].ToString();
            (odp[2] = new OleDbParameter("@bValidate", OleDbType.Boolean)).Value = false;
            (odp[3] = new OleDbParameter("@cCriteria", OleDbType.VarChar, 200)).Value = cmpsrCIFBooking.Text1;
            (odp[4] = new OleDbParameter("@cAccNo", OleDbType.VarChar, 19)).Value = cmpsrNoRekBooking.Text1.ToString();

            DataSet ds = new DataSet();
            bool blnResult = ClQ.ExecProc("dbo.ReksaSrcACCTNO_SYR", ref odp, out ds);

            bool isSyariah = false;

            if (blnResult && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                isSyariah = !string.IsNullOrEmpty(ds.Tables[0].Rows[0][4].ToString())
                            && Convert.ToBoolean(ds.Tables[0].Rows[0][4]);

                if (isSyariah &&
                    //20240911, Lely, RDN-1182, begin
                    (_iMode == 1 || _iMode == 2)
                    //20240911, Lely, RDN-1182, end
                    )
                {
                    MessageBox.Show(
                        "Rekening relasi adalah rekening syariah. Rekening ini hanya dapat digunakan untuk produk Reksadana Syariah!",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                        );
                }
            }
			//20240906, Lely, RDN-1182, end
            string strCriteria = _strTabName + "#" + cmpsrCIFBooking.Text1 + "#" + cmpsrNoRekBooking.Text2.Trim();
            cmpsrProductBooking.SearchDesc = "REKSA_TRXPRODUCT";
            cmpsrProductBooking.Criteria = strCriteria;
        }

        private void cmbFrekPendebetanRDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            ////20220329, gio, RDN-757, begin             
            //DataSet _ds = new DataSet();
            //if (cmbFrekPendebetanRDB.Text != "System.Data.DataRowView")
            //{
            //    int frekPendebetan = Int16.Parse(cmbFrekPendebetanRDB.Text.ToString());
            //    //string frekPendebetan = cmbFrekPendebetanRDB.SelectedValue.ToString();
            //    if(_strFreqDebetMethod!="null")
            //        if (PopulateParamComboRDBFrekDebet(_strFreqDebetMethod, frekPendebetan, "", out _ds))
            //        {
            //            if (_ds.Tables[0] != null)
            //            {
            //                if (_ds.Tables[0].Rows.Count > 0)
            //                {
            //                    lblFreqDebetRDB.Text = _ds.Tables[0].Rows[0]["FreqDebetMethodDesc"].ToString();
            //                }
            //            }
            //        }

            //}
            ////20220329, gio, RDN-757, end
        }
        //20220805, antoniusfilian, RDN-835, begin
        private void checkSwcRDBAll_Click(object sender, EventArgs e)
        {
            if (cmpsrProductSwcRDBIn.Text1 == "" && nispRedempSwcRDB.Enabled == true)
            {
                MessageBox.Show("Silahkan isi terlebih dahulu produk Switch In!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nispRedempSwcRDB.Text = "0";
                checkSwcRDBAll.Checked = !checkSwcRDBAll.Checked;
                return;
            }
            if (checkSwcRDBAll.Checked)
            {
                nispRedempSwcRDB.Value = nispOutstandingUnitSwcRDB.Value;
                nispRedempSwcRDB.Enabled = false;
                IsSwitchingRDBSebagian = false;
            }
            else
            {
                nispRedempSwcRDB.Value = 0;
                nispRedempSwcRDB.Enabled = true;
                IsSwitchingRDBSebagian = true;
            }
            try
            {
                string FeeCCY;
                decimal decNominalFee, decPctFee;
                int ProdSwcOut, ClientSwcOut, ProdSwcIn;

                string strPrdIdIn = GetImportantData("PRODUKID", cmpsrProductSwcRDBIn.Text1);
                int.TryParse(strPrdIdIn, out ProdSwcIn);

                string strPrdId = GetImportantData("PRODUKID", cmpsrProductSwcRDBOut.Text1);
                int.TryParse(strPrdId, out ProdSwcOut);

                string strClientId = GetImportantData("CLIENTID", cmpsrClientSwcRDBOut.Text1);
                int.TryParse(strClientId, out ClientSwcOut);

                nispRedempSwcRDB.Text = String.Format("{0:N4}", nispRedempSwcRDB.Value);

                HitungSwitchingRDBFee(ProdSwcOut, ClientSwcOut, nispRedempSwcRDB.Value, checkFeeEditSwcRDB.Checked,
                    nispPercentageFeeSwcRDB.Value, out FeeCCY, out decPctFee, out decNominalFee, cmpsrCIFSwcRDB.Text1
                    , ProdSwcIn, cmpsrNoRefSwcRDB.Text1.Trim());

                nispMoneyFeeSwcRDB.Value = decNominalFee;
                nispPercentageFeeSwcRDB.Value = decPctFee;
                labelFeeCurrencySwcRDB.Text = FeeCCY;
            }
            catch
            {
                return;
            }
        }
        //20220805, antoniusfilian, RDN-835, end
        //20220112, sandi, RDN-727, end
        //20221017, Andhika J, RDN-861, begin
        private bool ReksaValidateInsuranceRDB(string CIFNo, string iSasuransi)
        {
            bool blnResult = false;

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[2];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = CIFNo;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcIsAsuransi", System.Data.OleDb.OleDbType.VarChar, 1);
            dbParam[1].Value = iSasuransi;
            dbParam[1].Direction = System.Data.ParameterDirection.Output;

            blnResult = ClQ.ExecProc("ReksaValidateInsuranceRDB", ref dbParam);
            if (blnResult)
            {
                iSasuransi = dbParam[1].Value.ToString();
                if (iSasuransi == "N")
                {
                    cmbAsuransiRDB.Text = "TIDAK";
                    //cmbAsuransiRDB.SelectedIndex = 1;
                    cmbAsuransiRDB.Enabled = false;
                }
                else
                {
                    cmbAsuransiRDB.Enabled = true;
                }
            }

            return blnResult;
        }
        //20221017, Andhika J, RDN-861, end

        //20230223, Antonius Filian, RDN-903, begin
        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable();
            PropertyDescriptorCollection propertyDescriptorCollection =
                TypeDescriptor.GetProperties(typeof(T));
            for (int i = 0; i < propertyDescriptorCollection.Count; i++)
            {
                PropertyDescriptor propertyDescriptor = propertyDescriptorCollection[i];
                Type type = propertyDescriptor.PropertyType;

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    type = Nullable.GetUnderlyingType(type);


                dataTable.Columns.Add(propertyDescriptor.Name, type);
            }
            object[] values = new object[propertyDescriptorCollection.Count];
            foreach (T iListItem in items)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = propertyDescriptorCollection[i].GetValue(iListItem);
                }
                dataTable.Rows.Add(values);
            }
            return dataTable;
        }
        //20230223, Antonius Filian, RDN-903, end
    }
}
