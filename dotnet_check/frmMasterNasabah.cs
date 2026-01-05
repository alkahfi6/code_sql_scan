using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//20230206, Andhika J, RDN-903, begin
using ProReksa2.RepositoryAPI.Model;
//20230206, Andhika J, RDN-903, end

namespace BankNISP.FrontEnd
{
    public partial class frmMasterNasabah : BankNISP.Template.StandardForm
    {
        /*    
       ==================================================================================================
       Created By      : Andhika J
       Created Date    : 20230206
       Description     : Migrasi MW lama to API 
       Edited          :
       ==================================================================================================
       Date        Editor              Project ID          Description
       ==================================================================================================
       20230206    Andhika J           RDN-903             Migrasi SP ReksaMaintainNasabah to service API
       20230314    Andhika J           RDN-903             Migrasi SP ReksaGetAccountRelationDetail to service API
       20231710    Ahmad.Fansyuri      RDN-1061             Penambahan Flag Karyawan
       ==================================================================================================
    */
        private enum JnsNasabah : int
        {
            INDIVIDUAL = 1,
            CORPORATE = 4
        }

        private enum DataType
        {
            Booking = 0,
            Account = 1
        }

        internal NispLogin.ClsUser clUserInside;
        internal NispQuery.ClsQuery ClQ;
        private string[] _strDefToolBar;
        private string _strTabName;
        private string _strLastTabName;
        private DataView _dvAkses;
        private int _intProdId;
        private int _intType = 0;
        private int _intJnsNas;
        private string _strStatus;
        private DateTime _dtCurrentDate;
        private int _intValidationNPWP = 0;
        private int _intOpsiNPWP = 0;
        private DataSet _dsUpdate;

        public string strModule;
        public int intNIK;
        public string strGuid;
        public string strMenuName;
        public string strBranch;
        public int intClassificationId;
        private int intId;
        private DataSet dsBranch = new DataSet("Branch");
        private int intSelectedClient;
        //20170825, liliana, COPOD17271, begin
        private clsCoreBankMessaging _clsCoreBankMessaging = null;
        private DataSet dsOut;
        private string ErrMsg;
        //20170825, liliana, COPOD17271, end
        //20180829, Andhika J, BOSIT18231, begin
        private int intMaxDay = 0;
        private int intMaxYear = 0;
        //20180829, Andhika J, BOSIT18231, end
        //20210305, joshua, RDN-466, begin
        private DataSet dsEmail = new DataSet("Email");
        //20210305, joshua, RDN-466, end
        //20230106, sandi, RDN-899, begin
        private int isBlockOnly;
        private int isMature;
        private int isRDB;
        //20230106, sandi, RDN-899, end
        //20230206, Andhika J, RDN-903, begin
        private clsProc _cProc = new clsProc();
        private IServicesAPI _iServiceAPI;
        private ReksaMaintainNasabahRq _ReksaMaintainNasabahRq;
        private ReksaGetAccountRelationDetailRq _ReksaGetAccountRelationDetailRq;
        //20230206, Andhika J, RDN-903, end

        //20231017, ahmad.fansyuri, RDN-1061, begin
        private bool isFlag;
        private string strNIK;
        private string strNama;
        //20231017, ahmad.fansyuri, RDN-1061, end

        public frmMasterNasabah()
        {
            InitializeComponent();
            //20230206, Andhika J, RDN-903, begin
            this._iServiceAPI = clsStaticClass.APIService;
            //20230206, Andhika J, RDN-903, end
        }

        public string[] DefToolBar
        {
            get { return _strDefToolBar; }
            set { _strDefToolBar = value; }
        }

        private void subResetToolBar()
        {
            if (intClassificationId == 118)
            {
                this.NISPToolbarButtonSetVisible(true, _strDefToolBar);

                if ((_intType == 0) || (_intType == 3))
                {
                    if ((_strTabName == "MCB") || (_strTabName == "MCA"))
                    {
                        this.NISPToolbarButton("2").Visible = true;
                        this.NISPToolbarButton("3").Visible = false;
                        this.NISPToolbarButton("4").Visible = true;
                        this.NISPToolbarButton("5").Visible = false;
                        this.NISPToolbarButton("6").Visible = false;
                        this.NISPToolbarButton("7").Visible = false;
                    }
                    else
                    {
                        this.NISPToolbarButton("2").Visible = true;
                        this.NISPToolbarButton("3").Visible = false;
                        this.NISPToolbarButton("4").Visible = false;
                        this.NISPToolbarButton("5").Visible = false;
                        this.NISPToolbarButton("6").Visible = false;
                        this.NISPToolbarButton("7").Visible = false;
                    }
                }

                if ((_intType == 1) || (_intType == 2))
                {
                    this.NISPToolbarButton("2").Visible = false;
                    this.NISPToolbarButton("3").Visible = false;
                    this.NISPToolbarButton("4").Visible = false;
                    this.NISPToolbarButton("5").Visible = false;
                    this.NISPToolbarButton("6").Visible = true;
                    this.NISPToolbarButton("7").Visible = true;
                }
            }
            else
            {
                if (_dvAkses != null)
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

                    if ((_intType == 0) || (_intType == 3))
                    {
                        if ((_strTabName == "MCB") || (_strTabName == "MCA"))
                        {
                            //20150706, liliana, LIBST13020, begin
                            //this.NISPToolbarButton("2").Visible = true;
                            //this.NISPToolbarButton("3").Visible = false;
                            //this.NISPToolbarButton("4").Visible = false;
                            //this.NISPToolbarButton("5").Visible = false;
                            //this.NISPToolbarButton("6").Visible = false;
                            //this.NISPToolbarButton("7").Visible = false;
                            this.NISPToolbarButton("2").Enabled = true;
                            this.NISPToolbarButton("3").Enabled = false;
                            this.NISPToolbarButton("4").Enabled = false;
                            this.NISPToolbarButton("5").Enabled = false;
                            this.NISPToolbarButton("6").Enabled = false;
                            this.NISPToolbarButton("7").Enabled = false;
                            //20150706, liliana, LIBST13020, end
                        }
                        else
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
                            this.NISPToolbarButton("5").Enabled = false;
                            this.NISPToolbarButton("6").Enabled = false;
                            this.NISPToolbarButton("7").Enabled = false;
                            //20150706, liliana, LIBST13020, end
                        }
                    }

                    if ((_intType == 1) || (_intType == 2))
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
                        this.NISPToolbarButton("5").Enabled = false;
                        this.NISPToolbarButton("6").Enabled = true;
                        this.NISPToolbarButton("7").Enabled = true;
                        //20150706, liliana, LIBST13020, end
                    }
                }
            }

            //20230106, sandi, RDN-899, begin
            if (ValidasiBlokirUnit(intClassificationId, "Input"))
            {
                if (_strTabName == "MCB")
                {
                    this.NISPToolbarButton("2").Visible = true;
                    this.NISPToolbarButton("3").Visible = true;
                    this.NISPToolbarButton("4").Visible = true;
                    this.NISPToolbarButton("5").Visible = false;
                    this.NISPToolbarButton("6").Visible = false;
                    this.NISPToolbarButton("7").Visible = false;

                    this.NISPToolbarButton("3").Enabled = true;
                    this.NISPToolbarButton("4").Enabled = true;

                    if ((_intType == 1) || (_intType == 2))
                    {
                        this.NISPToolbarButton("2").Visible = false;
                        this.NISPToolbarButton("3").Visible = false;
                        this.NISPToolbarButton("4").Visible = false;
                        this.NISPToolbarButton("5").Visible = false;
                        this.NISPToolbarButton("6").Visible = true;
                        this.NISPToolbarButton("7").Visible = true;
                    }
                }
            }
            else
            {
                if (_strTabName == "MCB")
                {
                    this.NISPToolbarButton("2").Visible = true;
                    this.NISPToolbarButton("3").Visible = false;
                    this.NISPToolbarButton("4").Visible = false;
                    this.NISPToolbarButton("5").Visible = false;
                    this.NISPToolbarButton("6").Visible = false;
                    this.NISPToolbarButton("7").Visible = false;

                    this.NISPToolbarButton("3").Enabled = false;
                    this.NISPToolbarButton("4").Enabled = false;
                }
            }

            if (ValidasiFlagClientCode(intClassificationId, "Input"))
            {
                if (_strTabName == "MCA")
                {
                    this.NISPToolbarButton("2").Visible = true;
                    this.NISPToolbarButton("3").Visible = false;
                    this.NISPToolbarButton("4").Visible = true;
                    this.NISPToolbarButton("5").Visible = false;
                    this.NISPToolbarButton("6").Visible = false;
                    this.NISPToolbarButton("7").Visible = false;

                    this.NISPToolbarButton("4").Enabled = true;

                    if ((_intType == 1) || (_intType == 2))
                    {
                        this.NISPToolbarButton("2").Visible = false;
                        this.NISPToolbarButton("3").Visible = false;
                        this.NISPToolbarButton("4").Visible = false;
                        this.NISPToolbarButton("5").Visible = false;
                        this.NISPToolbarButton("6").Visible = true;
                        this.NISPToolbarButton("7").Visible = true;
                    }
                }
            }
            else
            {
                if (_strTabName == "MCA")
                {
                    this.NISPToolbarButton("2").Visible = true;
                    this.NISPToolbarButton("3").Visible = false;
                    this.NISPToolbarButton("4").Visible = false;
                    this.NISPToolbarButton("5").Visible = false;
                    this.NISPToolbarButton("6").Visible = false;
                    this.NISPToolbarButton("7").Visible = false;

                    this.NISPToolbarButton("4").Enabled = false;
                }
            }

            //20230106, sandi, RDN-899, end

        }

        private void GetComponentSearch()
        {
            cmpsrKodeKantor.SearchDesc = "SIBS_OFFICE";
            //20150706, liliana, LIBST13020, begin
            //cmpsrCIF.SearchDesc = "REKSA_CIF";
            cmpsrCIF.SearchDesc = "REKSA_CIF_ALL";
            //20150706, liliana, LIBST13020, end
            cmpsrNIK.SearchDesc = "REFERENTOR";
            cmpsrCabangSurat.SearchDesc = "SIBS_OFFICE";

            cmpsrSrcClient.SearchDesc = "REKSA_CLIENT_CIF";

        }

        private void frmMasterNasabah_Load(object sender, EventArgs e)
        {
            //20170825, liliana, COPOD17271, begin
            this._clsCoreBankMessaging = new clsCoreBankMessaging(intNIK, strGuid, strModule, GlobalFunctionCIF.QueryCIF);
            //20170825, liliana, COPOD17271, end
            GetComponentSearch();
            _intType = 0;
            intId = 0;
            _dtCurrentDate = System.Convert.ToDateTime(ProReksa2.Global.strCurrentDate.ToString());

            dtpEndDate.Value = _dtCurrentDate;
            dtpJoinDate.Value = _dtCurrentDate;
            dtpStartDate.Value = _dtCurrentDate;
            dtpTglLahir.Value = _dtCurrentDate;
            dtpTglTran.Value = _dtCurrentDate;
            dtpExpiry.Value = _dtCurrentDate;
            dtpTglTran.Value = _dtCurrentDate;
            dtpRiskProfile.Value = _dtCurrentDate;
            dtRDBJatuhTempo.Value = _dtCurrentDate;
            dtpTglNPWPSendiri.Value = _dtCurrentDate;
            dtpTglNPWPKK.Value = _dtCurrentDate;
            dtpTglDokTanpaNPWP.Value = _dtCurrentDate;
            lblStatus.Text = "";
            subDisableAll(_intType);
            _strTabName = tabControlClient.SelectedTab.Name.ToString();

            //20200620, Lita, RDN-88, begin
            dtRDBStartDebetDate.Value = _dtCurrentDate;
            //20200620, Lita, RDN-88, end


            DataSet dsTabAkses;
            OleDbParameter[] dbParam = new OleDbParameter[3];
            (dbParam[0] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
            (dbParam[1] = new OleDbParameter("@pcModule", OleDbType.VarChar, 30)).Value = strModule;
            (dbParam[2] = new OleDbParameter("@pcMenuName", OleDbType.VarChar, 50)).Value = strMenuName;

            bool blnResult = ClQ.ExecProc("UserGetTreeView", ref dbParam, out dsTabAkses);

            if (blnResult == true)
            {
                for (int i = 0; i < tabControlClient.TabPages.Count; i++)
                {
                    bool boolNotExists;
                    string strTabName;

                    strTabName = tabControlClient.TabPages[i].Name;
                    boolNotExists = true;

                    for (int j = 0; j < dsTabAkses.Tables[0].Rows.Count; j++)
                    {
                        if (dsTabAkses.Tables[0].Rows[j]["InterfaceTypeId"].ToString() == strTabName)
                        {
                            boolNotExists = false;
                        }
                    }
                    if (boolNotExists)
                    {
                        tabControlClient.TabPages.RemoveAt(i);
                    }
                }

                _dvAkses = new DataView(dsTabAkses.Tables[1]);
            }
            else
            {
                MessageBox.Show("Error Get Akses Tab");
            }
            //20150323, liliana, LIBST13020, begin
            //subResetToolBar();
            //20150323, liliana, LIBST13020, end

            //npwp
            DataSet dsParamGlobal;
            if (!GlobalFunctionCIF.LoadGlobalParam("KNP", intNIK, out dsParamGlobal))
                MessageBox.Show("Gagal ambil parameter Kepemilikan NPWP", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            cbKepemilikanNPWPKK.DataSource = dsParamGlobal.Tables[0];
            cbKepemilikanNPWPKK.ValueMember = "Kode";
            cbKepemilikanNPWPKK.DisplayMember = "Deskripsi";
            cbKepemilikanNPWPKK.SelectedIndex = -1;
            if (!GlobalFunctionCIF.LoadGlobalParam("ANP", intNIK, out dsParamGlobal))
                MessageBox.Show("Gagal ambil parameter Alasan Tidak Ada NPWP", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            cbAlasanTanpaNPWP.DataSource = dsParamGlobal.Tables[0];
            cbAlasanTanpaNPWP.ValueMember = "Kode";
            cbAlasanTanpaNPWP.DisplayMember = "Deskripsi";
            cbAlasanTanpaNPWP.SelectedIndex = -1;

            //cek kode kantor
            //20160509, Elva, CSODD16117, begin
            if (ValidasiKodeKantor(strBranch))
            {
                //20160509, Elva, CSODD16117, end
                cmpsrKodeKantor.Text1 = strBranch;
                cmpsrKodeKantor.ValidateField();
                //20160509, Elva, CSODD16117, begin
            }
            //ValidasiKodeKantor(cmpsrKodeKantor.Text1);
            //20160509, Elva, CSODD16117, end
            //20161004, liliana, CSODD16311, begin
            tmrTimer.Tick += new EventHandler(OnTimerTick);
            tmrTimer.Interval = 1000; // 1 second interval
            tmrTimer.Start();
            //20161004, liliana, CSODD16311, end
            //20230105, sandi, RDN-899, begin
            cbBlokir.SelectedIndex = 0;
            cbBlokir.Text = "";
            cbBlokir.Enabled = false;
            tbDeskripsiBlokir.Enabled = false;
            nmBlockId.Enabled = false;
            nmNAVYesterday.Value = 0;
            dtpNAVDate.Value = DateTime.Today;
            dtpInputDate.Value = DateTime.Today;
            dtpTglTran.Value = DateTime.Today;
            dtpExpiry.Value = DateTime.Today;
            nmNilaiMarket.Value = 0;
            tbProdCode.Text = "";
            tbProdName.Text = "";
            //20230105, sandi, RDN-899, end
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
                        //20150723, liliana, LIBST13020, begin
                        subCancel();
                        //20150723, liliana, LIBST13020, end
                        tabControlClient.Enabled = false;
                        cmpsrCIF.Enabled = false;
                        MessageBox.Show("Kode Kantor tidak terdaftar, pembuatan master nasabah tidak dapat dilakukan!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //20160509, Elva, CSODD16117, begin
                        //return;
                        return false;
                        //20160509, Elva, CSODD16117, end
                    }
                    else
                    {
                        tabControlClient.Enabled = true;
                        cmpsrCIF.Enabled = true;
                        //20150323, liliana, LIBST13020, begin
                        subResetToolBar();
                        //20150323, liliana, LIBST13020, end
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

        private void frmMasterNasabah_OnNISPToolbarClick(ref ToolStripButton NISPToolbarButton)
        {
            switch (NISPToolbarButton.Name)
            {
                case ("1"): //keluar
                    this.Close();
                    break;
                case ("2"): //refresh
                    subRefresh();
                    break;
                case ("3"): // new
                    subNew();
                    break;
                case ("4"): //update
                    subUpdate();
                    break;
                case ("5"): //delete
                    subDelete();
                    break;
                case ("6"): //save
                    subSave();
                    break;
                case ("7"): //cancel
                    subCancel();
                    break;
            }
        }

        private void subUpdate()
        {
            if (cmpsrCIF.Text1.ToString().Length == 0)
            {
                MessageBox.Show("Pilih Nasabah Terlebih Dahulu");
            }
            else
            {
                //20230106, sandi, RDN-899, begin
                if (_strTabName == "MCB")
                {
                    if (nmBlockId.Value == 0)
                    {
                        MessageBox.Show("Silahkan pilih Client Code dan Blok ID yang akan di-update!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (cbBlokir.SelectedIndex == 1)
                    {
                        MessageBox.Show("Blok ID yang sudah lepas blokir, tidak dapat di-update!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    _intType = 2;
                }
                //20230106, sandi, RDN-899, end
                subRefresh();
                _intType = 2;
                lblStatus.Text = "UPDATING";

                if ((_strTabName == "MCI") || (_strTabName == "MCA") || (_strTabName == "MCN"))
                {

                    cmpsrCIF.Enabled = false;
                    tbNama.Enabled = false;

                    //20231017, ahmad.fansyuri, RDN-1061, begin
                    //cbStatus.Enabled = true;
                    cbStatus.Enabled = false;
                    //20231017, ahmad.fansyuri, RDN-1061, end

                    cmpsrNIK.Enabled = false;
                    //20150910, liliana, LIBST13020, begin
                    //cbProfilResiko.Enabled = true;
                    //cbKetum.Enabled = true;
                    cbProfilResiko.Enabled = false;
                    cbKetum.Enabled = false;
                    //20150910, liliana, LIBST13020, end
                    txtbRiskProfile.Enabled = false;
                    tbRekening.Enabled = true;
                    //20150427, liliana, LIBST13020, begin
                    maskedRekening.Enabled = true;
                    //20150427, liliana, LIBST13020, end
                    tbNamaRekening.Enabled = false;
                    //20150518, liliana, LIBST13020, begin
                    maskedRekeningUSD.Enabled = true;
                    tbNamaRekeningUSD.Enabled = false;
                    //20150518, liliana, LIBST13020, end
                    //20150727, liliana, LIBST13020, begin
                    maskedRekeningMC.Enabled = true;
                    tbNamaRekeningMC.Enabled = false;
                    //20150727, liliana, LIBST13020, end
                    //20160823, Elva, LOGEN00196, begin
                    comboRekIDRTA.Enabled = true;
                    txtNamaRekIDRTA.Enabled = false;
                    comboRekUSDTA.Enabled = true;
                    txtNamaRekUSDTA.Enabled = false;
                    comboRekMultiCurTA.Enabled = true;
                    txtNamaRekMultiCurTA.Enabled = false;
                    //20160823, Elva, LOGEN00196, end
                    cbDikirimKe.Enabled = true;
                    cmpsrCabangSurat.Enabled = true;

                    tbNoNPWPKK.Enabled = false;
                    tbNamaNPWPKK.Enabled = false;
                    cbKepemilikanNPWPKK.Enabled = false;
                    tbKepemilikanLainnya.Enabled = false;
                    dtpTglNPWPKK.Enabled = false;
                    cbAlasanTanpaNPWP.Enabled = false;
                    btnGenerateNoDokTanpaNPWP.Enabled = false;
                    btnGantiOpsiNPWP.Enabled = true;

                    //20150708, liliana, LIBST13020, begin
                    //dgvKonfAddr.Enabled = true;
                    if (dgvKonfAddr.Rows.Count > 0)
                    {
                        for (int i = 0; i < dgvKonfAddr.Columns.Count; i++)
                        {
                            dgvKonfAddr.Columns[i].ReadOnly = false;
                        }
                    }
                    //20150708, liliana, LIBST13020, end
                    //20160111, liliana, LIODD15275, begin
                    cekCheckbox();
                    //20160111, liliana, LIODD15275, end
                    //20161101, liliana, CSODD16311, begin
                    if (lblTaxAmnesty.Visible == false)
                    {
                        comboRekIDRTA.Enabled = false;
                        comboRekMultiCurTA.Enabled = false;
                        comboRekUSDTA.Enabled = false;
                    }
                    else
                    {
                        comboRekIDRTA.Enabled = true;
                        comboRekMultiCurTA.Enabled = true;
                        comboRekUSDTA.Enabled = true;
                    }
                    //20161101, liliana, CSODD16311, end
                }
                else if (_strTabName == "MCB")
                {
                    //20150630, liliana, LIBST13020, begin
                    //20230106, sandi, RDN-899, begin
                    //ReksaRefreshBlokir();
                    //20230106, sandi, RDN-899, end
                    //20150630, liliana, LIBST13020, end
                    nispMoneyBlokir.Enabled = true;
                    dtpExpiry.Enabled = true;
                    //20230105, sandi, RDN-899, begin
                    cbBlokir.Enabled = true;
                    tbDeskripsiBlokir.Enabled = true;
                    dgvBlokir.Enabled = false;
                    dgvLogBlokir.Enabled = false;
                    dtpTglTran.Enabled = true;
                    cmpsrSrcClient.Enabled = false;
                    //20230105, sandi, RDN-899, end
                }
                else
                {
                    _intType = 0;
                }

                subResetToolBar();
            }

        }

        private void subDelete()
        {
            _intType = 3;

            if (MessageBox.Show("Yakin akan melakukan delete data?", "Delete Data", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                           == DialogResult.OK)
            {
                subSave();

            }

            _intType = 0;
            subResetToolBar();
        }

        private void subRefresh()
        {
            if (cmpsrCIF.Text1 == "")
            {
                MessageBox.Show("Nomor CIF Harus Diisi!");
                return;
            }

            //20140324, liliana, LIBST13020, begin
            //20150708, liliana, LIBST13020, begin
            cmpsrCIF.ValidateField();
            //20150708, liliana, LIBST13020, end
            if (cmpsrCIF.Text2 == "")
            {
                //20150723, liliana, LIBST13020, begin
                //MessageBox.Show("Harap memilih Nomor CIF, Nama CIF tidak boleh kosong!");
                MessageBox.Show("No. CIF belum memiliki Master Nasabah!");
                //20150723, liliana, LIBST13020, end
                return;
            }
            //20140324, liliana, LIBST13020, end

            //20170828, liliana, COPOD17271, begin
            if (!this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIF.Text1.Trim(), strBranch, intClassificationId.ToString(),
             out ErrMsg, out dsOut))//dapet akses private banking
            {
                MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //20170828, liliana, COPOD17271, end

            if ((_strTabName == "MCI") || (_strTabName == "MDN"))
            {
                DataSet dsRefresh;

                System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
                dbParam[0].Value = cmpsrCIF.Text1.Trim();
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
                        intId = System.Convert.ToInt32(dsRefresh.Tables[0].Rows[0]["NasabahId"].ToString());

                        cmpsrKodeKantor.Text1 = dsRefresh.Tables[0].Rows[0]["OfficeId"].ToString();
                        cmpsrKodeKantor.ValidateField();
                        textShareHolderId.Text = dsRefresh.Tables[0].Rows[0]["ShareholderID"].ToString();

                        tbNama.Text = dsRefresh.Tables[0].Rows[0]["CIFName"].ToString();

                        if (cmpsrCIF.Text2 == "")
                        {
                            cmpsrCIF.Text2 = tbNama.Text;
                        }

                        tbTmptLahir.Text = dsRefresh.Tables[0].Rows[0]["CIFBirthPlace"].ToString();
                        dtpTglLahir.Value = (DateTime)dsRefresh.Tables[0].Rows[0]["CIFBirthDay"];
                        dtpJoinDate.Value = (DateTime)dsRefresh.Tables[0].Rows[0]["CreateDate"];
                        tbRekening.Text = dsRefresh.Tables[0].Rows[0]["NISPAccountId"].ToString();
                        //20150427, liliana, LIBST13020, begin
                        maskedRekening.Text = dsRefresh.Tables[0].Rows[0]["NISPAccountId"].ToString();
                        //20150427, liliana, LIBST13020, end
                        tbNamaRekening.Text = dsRefresh.Tables[0].Rows[0]["NISPAccountName"].ToString();
                        //20150518, liliana, LIBST13020, begin
                        maskedRekeningUSD.Text = dsRefresh.Tables[0].Rows[0]["NISPAccountIdUSD"].ToString();
                        tbNamaRekeningUSD.Text = dsRefresh.Tables[0].Rows[0]["NISPAccountNameUSD"].ToString();
                        //20150518, liliana, LIBST13020, end
                        //20150727, liliana, LIBST13020, begin
                        maskedRekeningMC.Text = dsRefresh.Tables[0].Rows[0]["NISPAccountIdMC"].ToString();
                        tbNamaRekeningMC.Text = dsRefresh.Tables[0].Rows[0]["NISPAccountNameMC"].ToString();
                        //20150727, liliana, LIBST13020, end
                        //20160823, Elva, LOGEN00196, begin
                        comboRekIDRTA.Text = dsRefresh.Tables[0].Rows[0]["AccountIdTA"].ToString();
                        txtNamaRekIDRTA.Text = dsRefresh.Tables[0].Rows[0]["AccountNameTA"].ToString();
                        comboRekUSDTA.Text = dsRefresh.Tables[0].Rows[0]["AccountIdUSDTA"].ToString();
                        txtNamaRekUSDTA.Text = dsRefresh.Tables[0].Rows[0]["AccountNameUSDTA"].ToString();
                        comboRekMultiCurTA.Text = dsRefresh.Tables[0].Rows[0]["AccountIdMCTA"].ToString();
                        txtNamaRekMultiCurTA.Text = dsRefresh.Tables[0].Rows[0]["AccountNameMCTA"].ToString();
                        //20160823, Elva, LOGEN00196, end

                        tbKTP.Text = dsRefresh.Tables[0].Rows[0]["CIFIDNo"].ToString();
                        tbHP.Text = dsRefresh.Tables[0].Rows[0]["HP"].ToString();
                        textSID.Text = dsRefresh.Tables[0].Rows[0]["CIFSID"].ToString();
                        //20140324, liliana, LIBST13020, begin
                        textSubSegment.Text = dsRefresh.Tables[0].Rows[0]["SubSegment"].ToString();
                        textSegment.Text = dsRefresh.Tables[0].Rows[0]["Segment"].ToString();
                        //20140324, liliana, LIBST13020, end

                        checkPhoneOrder.Checked = GlobalFunctionCIF.CekCIFProductFacility(cmpsrCIF.Text1.ToString());

                        int intcbStatus;
                        int.TryParse(dsRefresh.Tables[0].Rows[0]["IsEmployee"].ToString(), out intcbStatus);

                        //20150706, liliana, LIBST13020, begin
                        //cbStatus.SelectedIndex = intcbStatus;
                        //20150706, liliana, LIBST13020, end
                        _intJnsNas = (int)dsRefresh.Tables[0].Rows[0]["CIFType"];
                        txtJenisNasabah.Text = Enum.GetName(typeof(JnsNasabah), _intJnsNas);

                        //20231121, ahmad.fansyuri, RDN-1086, begin

                        if (dsRefresh.Tables[0].Rows[0]["CIFNik"].ToString() != "")
                        {
                            DataSet dsTest;

                            System.Data.OleDb.OleDbParameter[] dbParameter = new System.Data.OleDb.OleDbParameter[2];

                            dbParameter[0] = new System.Data.OleDb.OleDbParameter("@pcAccountNumber", System.Data.OleDb.OleDbType.VarChar, 20);
                            dbParameter[0].Value = cmpsrCIF.Text1.Trim();
                            dbParameter[0].Direction = System.Data.ParameterDirection.Input;

                            dbParameter[1] = new System.Data.OleDb.OleDbParameter("@pnType", System.Data.OleDb.OleDbType.Integer);
                            dbParameter[1].Value = 2;
                            dbParameter[1].Direction = System.Data.ParameterDirection.Input;

                            bool booleanResult = ClQ.ExecProc("ReksaGetAccountRelationDetail", ref dbParameter, out dsTest);

                            if (booleanResult == true)
                            {
                                cmpsrNIK.ReadOnly = false;
                                cmpsrNIK._Text1.Text = dsTest.Tables[0].Rows[0]["NIK"].ToString();
                                cmpsrNIK._Text2.Text = dsTest.Tables[0].Rows[0]["Nama"].ToString();


                                //20231121, ahmad.fansyuri, RDN-1094, begin
                                if (cmpsrNIK._Text1.Text == "")
                                {
                                    cbStatus.SelectedIndex = 1;
                                }
                                else
                                {
                                    cbStatus.SelectedIndex = 0;
                                }
                                //20231121, ahmad.fansyuri, RDN-1094, end
                            }
                        }
                        else
                        {
                            cmpsrNIK.Text1 = dsRefresh.Tables[0].Rows[0]["CIFNik"].ToString();
                            cmpsrNIK.ValidateField();
                        }

                        //ambil dok
                        SetDocStatus(cmpsrCIF.Text1.ToString(), _intType);


                        //20210220, julio, RDN-410, begin
                        //check email
                        String EmailWarning;
                        CekElecAddress(cmpsrCIF.Text1, out EmailWarning);
                        if (EmailWarning.Trim() != "")
                        {
                            MessageBox.Show(EmailWarning, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }
                        //20210220, julio, RDN-410, end


                        //tambah risk profil
                        txtbRiskProfile.Text = dsRefresh.Tables[1].Rows[0]["RiskProfile"].ToString();
                        dtpRiskProfile.Value = (DateTime)dsRefresh.Tables[1].Rows[0]["LastUpdate"];

                        if (txtbRiskProfile.Text == "")
                        {
                            MessageBox.Show("Data risk profile tidak ada");
                        }

                        if (!GetKonfAddress(cmpsrCIF.Text1.Trim()))
                        {
                            MessageBox.Show("Gagal Ambil Data Alamat Konfirmasi!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        _intOpsiNPWP = 0;
                        if (dsRefresh.Tables[2].Rows.Count > 0)
                        {
                            tbNoNPWPSendiri.Text = dsRefresh.Tables[2].Rows[0]["NoNPWPProCIF"].ToString();
                            tbNamaNPWPSendiri.Text = dsRefresh.Tables[2].Rows[0]["NamaNPWPProCIF"].ToString();
                            dtpTglNPWPSendiri.Value = (DateTime)dsRefresh.Tables[2].Rows[0]["TglNPWP"];
                            tbNoNPWPKK.Text = dsRefresh.Tables[2].Rows[0]["NoNPWPKK"].ToString();
                            tbNamaNPWPKK.Text = dsRefresh.Tables[2].Rows[0]["NamaNPWPKK"].ToString();
                            cbKepemilikanNPWPKK.SelectedValue = int.Parse(dsRefresh.Tables[2].Rows[0]["KepemilikanNPWPKK"].ToString());
                            tbKepemilikanLainnya.Text = dsRefresh.Tables[2].Rows[0]["KepemilikanNPWPKKLainnya"].ToString();
                            dtpTglNPWPKK.Value = (DateTime)dsRefresh.Tables[2].Rows[0]["TglNPWPKK"];
                            cbAlasanTanpaNPWP.SelectedValue = int.Parse(dsRefresh.Tables[2].Rows[0]["AlasanTanpaNPWP"].ToString());
                            tbNoDokTanpaNPWP.Text = dsRefresh.Tables[2].Rows[0]["NoDokTanpaNPWP"].ToString();
                            dtpTglDokTanpaNPWP.Value = (DateTime)dsRefresh.Tables[2].Rows[0]["TglDokTanpaNPWP"];
                            _intOpsiNPWP = int.Parse(dsRefresh.Tables[2].Rows[0]["Opsi"].ToString());
                            _intValidationNPWP = _intOpsiNPWP;
                        }
                        else
                        {
                            tbNoNPWPSendiri.Text = dsRefresh.Tables[0].Rows[0]["CIFNPWP"].ToString();
                            tbNamaNPWPSendiri.Text = dsRefresh.Tables[0].Rows[0]["NamaNPWP"].ToString();
                            dtpTglNPWPSendiri.Value = (DateTime)dsRefresh.Tables[0].Rows[0]["TglNPWP"];
                            tbNoNPWPKK.Text = dsRefresh.Tables[0].Rows[0]["NoNPWPKK"].ToString();
                            tbNamaNPWPKK.Text = dsRefresh.Tables[0].Rows[0]["NamaNPWPKK"].ToString();
                            cbKepemilikanNPWPKK.SelectedValue = int.Parse(dsRefresh.Tables[0].Rows[0]["KepemilikanNPWPKK"].ToString());
                            tbKepemilikanLainnya.Text = dsRefresh.Tables[0].Rows[0]["KepemilikanNPWPKKLainnya"].ToString();
                            dtpTglNPWPKK.Value = (DateTime)dsRefresh.Tables[0].Rows[0]["TglNPWPKK"];
                            cbAlasanTanpaNPWP.SelectedValue = int.Parse(dsRefresh.Tables[0].Rows[0]["AlasanTanpaNPWP"].ToString());
                            tbNoDokTanpaNPWP.Text = dsRefresh.Tables[0].Rows[0]["NoDokTanpaNPWP"].ToString();
                            dtpTglDokTanpaNPWP.Value = (DateTime)dsRefresh.Tables[0].Rows[0]["TglDokTanpaNPWP"];
                            _intOpsiNPWP = int.Parse(dsRefresh.Tables[0].Rows[0]["Opsi"].ToString());
                            _intValidationNPWP = 0;
                        }

                        //20150325, liliana, LIBST13020, begin
                        switch (dsRefresh.Tables[0].Rows[0]["ApprovalStatus"].ToString().Trim())
                        {
                            case "A":
                                {
                                    lblStatus.Text = "Aktif";
                                    break;
                                }
                            case "N":
                                {
                                    lblStatus.Text = "Menunggu Otorisasi";
                                    break;
                                }
                            case "T":
                                {
                                    lblStatus.Text = "Tutup";
                                    break;
                                }
                        }
                        //20150325, liliana, LIBST13020, end

                        _intType = 0;
                        _dsUpdate = dsRefresh.Copy();
                        subResetToolBar();
                    }
                    else
                    {
                        MessageBox.Show("Data tidak ditemukan!");
                        subClearAll();
                        _intType = 0;
                        subResetToolBar();
                    }
                }
                else
                {
                    MessageBox.Show("Error Refresh Data Nasabah");
                }

                dsRefresh = null;
                dbParam = null;

                dgvClientCode.DataSource = null;
                dgvAktifitas.DataSource = null;
                //20150610, liliana, LIBST13020, begin
                //dtRDBJatuhTempo.Value = DateTime.Parse("1900-01-01");
                dtRDBJatuhTempo.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end
                chkRDBAsuransi.Checked = false;
                chkAutoRedemp.Checked = false;
                textFrekPendebetan.Text = "";
                txtRDBJangkaWaktu.Text = "";
                //20150610, liliana, LIBST13020, begin
                //dtpStartDate.Value = DateTime.Parse("1900-01-01");
                //dtpEndDate.Value = DateTime.Parse("1900-01-01");
                dtpStartDate.Value = DateTime.Today;
                dtpEndDate.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end

                //20200620, Lita, RDN-88, begin
                dtRDBStartDebetDate.Value = DateTime.Today;
                lblFreqDebetUnit.Text = "";
                //20200620, Lita, RDN-88, end


                cmpsrSrcClient.Text1 = "";
                cmpsrSrcClient.Text2 = "";
                nispMoneyBlokir.Value = 0;
                nispMoneyTotal.Value = 0;
                nispOutsUnit.Value = 0;
                dgvBlokir.DataSource = null;
                //20150610, liliana, LIBST13020, begin
                //dtpExpiry.Value = DateTime.Parse("1900-01-01");
                //dtpTglTran.Value = DateTime.Parse("1900-01-01");
                dtpExpiry.Value = DateTime.Today;
                dtpTglTran.Value = DateTime.Today;
                //20230106, sandi, RDN-899, begin
                dtpNAVDate.Value = DateTime.Today;
                dtpInputDate.Value = DateTime.Today;
                //20230106, sandi, RDN-899, end
                //20150610, liliana, LIBST13020, end

            }

            //20150615, liliana, LIBST13020, begin
            //if (_strTabName == "MCA")
            if ((_strTabName == "MCI") || (_strTabName == "MCA"))
            //20150615, liliana, LIBST13020, end
            {
                if (cmpsrCIF.Text1 == "")
                {
                    MessageBox.Show("Client Code Harus Diisi!");
                    return;
                }

                DataSet dsListClient;

                System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];

                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
                dbParam[0].Value = cmpsrCIF.Text1.Trim();
                dbParam[0].Direction = System.Data.ParameterDirection.Input;

                bool blnResult = ClQ.ExecProc("ReksaGetListClient", ref dbParam, out dsListClient);

                if (blnResult == true)
                {
                    dgvClientCode.DataSource = dsListClient.Tables[0];
                    dgvClientCode.AutoResizeColumns();
                    dgvClientCode.Columns["ClientId"].Visible = false;
                    //20150904, liliana, LIBST13020, begin
                    for (int i = 0; i < dgvClientCode.Columns.Count; i++)
                    {
                        if (dgvClientCode.Columns[i].ValueType.ToString() == "System.Decimal")
                        {
                            dgvClientCode.Columns[i].DefaultCellStyle.Format = "N2";
                        }
                    }
                    //20150904, liliana, LIBST13020, end
                    subResetToolBar();

                    if (intClassificationId == 118)
                    {
                        for (int i = 1; i < dgvClientCode.Columns.Count; i++)
                        {
                            if (dgvClientCode.Columns[i].HeaderText != "Flag")
                            {
                                dgvClientCode.Columns[i].ReadOnly = true;
                            }
                            else
                            {
                                dgvClientCode.Columns[i].ReadOnly = false;
                            }

                        }
                    }
                    else
                    {
                        for (int i = 1; i < dgvClientCode.Columns.Count; i++)
                        {
                            dgvClientCode.Columns[i].ReadOnly = true;
                        }
                    }
                }

            }

            //20230106, sandi, RDN-899, begin
            if ((_strTabName == "MCB") && (cmpsrSrcClient.Text2 != "") && (_intType == 0))
            //20230106, sandi, RDN-899, end
            {
                ReksaRefreshBlokir();
            }

        }

        public int KonfirmasiCountSelected()
        {
            int Count = 0;

            if (cbDikirimKe.SelectedIndex == 0)
            {
                //20150422, liliana, LIBST13020, begin
                dgvKonfAddr.EndEdit();
                //20150422, liliana, LIBST13020, end
                foreach (DataGridViewRow dgvRow in dgvKonfAddr.Rows)
                {
                    if (System.Convert.ToBoolean(dgvRow.Cells[0].FormattedValue))
                    {
                        Count++;
                    }
                }
            }
            else
            {
                Count = 1;
            }

            return Count;
        }

        public string GetAllData()
        {
            dgvKonfAddr.CommitEdit(DataGridViewDataErrorContexts.Commit);
            string strHeader = "";
            DataTable dtHeader = new DataTable("HeaderData");
            dtHeader.Columns.Add("Type");
            dtHeader.Columns.Add("CIFNo");
            dtHeader.Columns.Add("DataType");
            dtHeader.Columns.Add("Branch");
            dtHeader.Columns.Add("Id");
            dtHeader.Rows.Add(new object[] { DataType.Account.ToString(), cmpsrCIF.Text1, cbDikirimKe.SelectedIndex.ToString(), cmpsrKodeKantor.Text1, intId.ToString() });
            DataSet dsHeader = new DataSet("Header");
            dsHeader.Tables.Add(dtHeader);
            strHeader = dsHeader.GetXml().Trim();

            string strHeaderLength = strHeader.Length.ToString().Trim().PadLeft(5, '0');
            return strHeaderLength + strHeader + GetXMLData();
        }

        //20210211, julio, RDN-410, begin
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
        //20210211, julio, RDN-410, end


        private string GetXMLData()
        {
            //20150610, liliana, LIBST13020, begin
            dgvKonfAddr.EndEdit();
            //20150610, liliana, LIBST13020, end
            dgvKonfAddr.CommitEdit(DataGridViewDataErrorContexts.Commit);

            if ((int)cbDikirimKe.SelectedIndex == 0)
            {
                DataSet dsTemp = new DataSet("Client");
                DataTable dtTemp1 = (DataTable)dgvKonfAddr.DataSource;
                DataTable dtTemp2 = dtTemp1.Copy();
                //20150610, liliana, LIBST13020, begin
                //dtTemp2.DefaultView.RowFilter = "Pilih = 1";
                //dsTemp.Tables.Add(dtTemp2.DefaultView.ToTable());
                dtTemp1.DefaultView.RowFilter = "Pilih = 1";
                dtTemp1.AcceptChanges();
                dsTemp.Tables.Add(dtTemp1.DefaultView.ToTable());
                //20150610, liliana, LIBST13020, end
                return dsTemp.GetXml();
            }
            //20210305, joshua, RDN-466, begin
            //else
            //{
            //    return dsBranch.GetXml();
            //}
            else if ((int)cbDikirimKe.SelectedIndex == 1)
            {
                return dsBranch.GetXml();
            }
            else
            {
                return dsEmail.GetXml();
            }
            //20210305, joshua, RDN-466, end
        }

        private void subSave()
        {
            //20150422, liliana, LIBST13020, begin
            dgvKonfAddr.EndEdit();
            //20150422, liliana, LIBST13020, end
            //20160509, Elva, CSODD16117, begin
            string strErrorMessage = "", strIsAllowed = "";
            if (clsValidator.ValidasiUserCBO(ClQ, cmpsrKodeKantor.Text1, strBranch, out strIsAllowed, out strErrorMessage))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateUserCBOOffice], " + strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Error [ReksaValidateUserCBOOffice]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            //20160509, Elva, CSODD16117, end
            if (cmpsrCIF.Text1 == "")
            {
                MessageBox.Show("No CIF harus diisi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmpsrCIF.Text2 == "")
            {
                MessageBox.Show("Nama CIF harus diisi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if ((_strTabName == "MCI") || (_strTabName == "MCN"))
            {
                if (_intType != 3)
                {
                    //20150424, liliana, LIBST13020, begin
                    if (cmpsrKodeKantor.Text1 == "")
                    {
                        MessageBox.Show("Kode Kantor harus terisi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150424, liliana, LIBST13020, end
                    if (textShareHolderId.Text == "")
                    {
                        MessageBox.Show("Shareholder ID harus diisi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150709, liliana, LIBST13020, begin
                    //20150728, liliana, LIBST13020, begin

                    if ((maskedRekening.Text.Replace("-", "").Trim() != "") && (maskedRekeningMC.Text.Replace("-", "").Trim() != ""))
                    {
                        MessageBox.Show("Harap hanya mengisi salah satu, Rekening IDR atau Rekening Multicurrency saja!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if ((maskedRekeningUSD.Text.Replace("-", "").Trim() != "") && (maskedRekeningMC.Text.Replace("-", "").Trim() != ""))
                    {
                        MessageBox.Show("Harap hanya mengisi salah satu, Rekening USD atau Rekening Multicurrency saja!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20160823, Elva, LOGEN00196, begin
                    if ((comboRekIDRTA.Text.Trim() != "") && (comboRekMultiCurTA.Text.Trim() != ""))
                    {
                        MessageBox.Show("Harap hanya mengisi salah satu, Rekening IDR Tax Amnesty atau Rekening Multicurrency Tax Amnesty saja!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if ((comboRekUSDTA.Text.Trim() != "") && (comboRekMultiCurTA.Text.Trim() != ""))
                    {
                        MessageBox.Show("Harap hanya mengisi salah satu, Rekening USD Tax Amnesty atau Rekening Multicurrency Tax Amnesty saja!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20160823, Elva, LOGEN00196, end
                    //20150728, liliana, LIBST13020, end
                    if (maskedRekening.Text.Replace("-", "").Trim() != "")
                    {
                        //20161101, liliana, CSODD16311, begin
                        //GetAccountRelationDetail(maskedRekening.Text.Replace("-", "").Trim(), 1);
                        if (!GetAccountRelationDetail(maskedRekening.Text.Replace("-", "").Trim(), 1))
                        {
                            return;
                        }
                        //20161101, liliana, CSODD16311, end

                        if (maskedRekening.Text == "")
                        {
                            MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    if (maskedRekeningUSD.Text.Replace("-", "").Trim() != "")
                    {
                        //20161101, liliana, CSODD16311, begin
                        //GetAccountRelationDetail(maskedRekeningUSD.Text.Replace("-", "").Trim(), 3);
                        if (!GetAccountRelationDetail(maskedRekeningUSD.Text.Replace("-", "").Trim(), 3))
                        {
                            return;
                        }
                        //20161101, liliana, CSODD16311, end


                        if (maskedRekeningUSD.Text == "")
                        {
                            MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    //20150709, liliana, LIBST13020, end
                    //20150727, liliana, LIBST13020, begin

                    if (maskedRekeningMC.Text.Replace("-", "").Trim() != "")
                    {
                        //20161101, liliana, CSODD16311, begin
                        //GetAccountRelationDetail(maskedRekeningMC.Text.Replace("-", "").Trim(), 4);
                        if (!GetAccountRelationDetail(maskedRekeningMC.Text.Replace("-", "").Trim(), 4))
                        {
                            return;
                        }
                        //20161101, liliana, CSODD16311, end


                        if (maskedRekeningMC.Text == "")
                        {
                            MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    //20150727, liliana, LIBST13020, end

                    //20150427, liliana, LIBST13020, begin
                    //if (tbRekening.Text == "")
                    //20150622, liliana, LIBST13020, begin
                    //if (maskedRekening.Text == "")
                    //20150623, liliana, LIBST13020, begin
                    //if ((maskedRekening.Text == "") || (maskedRekeningUSD.Text == ""))
                    //20150727, liliana, LIBST13020, begin
                    //if ((maskedRekening.Text.Replace("-", "") == "") || (maskedRekeningUSD.Text.Replace("-", "") == ""))
                    //20160823, Elva, LOGEN00196, begin
                    //if ((maskedRekening.Text.Replace("-", "").Trim() == "") && (maskedRekeningUSD.Text.Replace("-", "").Trim() == "") && (maskedRekeningMC.Text.Replace("-", "").Trim() == ""))
                    if ((maskedRekening.Text.Replace("-", "").Trim() == "") &&
                        (maskedRekeningUSD.Text.Replace("-", "").Trim() == "") &&
                        (maskedRekeningMC.Text.Replace("-", "").Trim() == "") &&
                        (comboRekIDRTA.Text == "") &&
                        (comboRekUSDTA.Text == "") &&
                        (comboRekMultiCurTA.Text == ""))
                    //20160823, Elva, LOGEN00196, end
                    //20150727, liliana, LIBST13020, end
                    //20150623, liliana, LIBST13020, end
                    //20150622, liliana, LIBST13020, end
                    //20150427, liliana, LIBST13020, end
                    {
                        MessageBox.Show("Nomor Rekening harus diisi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20151023, liliana, LIBST13020, begin
                    if ((maskedRekening.Text.Replace("-", "").Trim() != "") && (maskedRekening.Text.Length < 12))
                    {
                        MessageBox.Show("Nomor rekening harus 12 digit!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if ((maskedRekeningUSD.Text.Replace("-", "").Trim() != "") && (maskedRekeningUSD.Text.Length < 12))
                    {
                        MessageBox.Show("Nomor rekening harus 12 digit!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if ((maskedRekeningMC.Text.Replace("-", "").Trim() != "") && (maskedRekeningMC.Text.Length < 12))
                    {
                        MessageBox.Show("Nomor rekening harus 12 digit!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20151023, liliana, LIBST13020, end

                    //20150623, liliana, LIBST13020, begin
                    ////20150622, liliana, LIBST13020, begin
                    ////if (tbNamaRekening.Text == "")
                    //if ((tbNamaRekening.Text == "") && (maskedRekening.Text != ""))
                    ////20150622, liliana, LIBST13020, end
                    //{
                    //    //20150622, liliana, LIBST13020, begin
                    //    //MessageBox.Show("Nomor rekening tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //    MessageBox.Show("Nomor rekening IDR tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //    //20150622, liliana, LIBST13020, end
                    //    return;
                    //}
                    ////20150622, liliana, LIBST13020, begin
                    //if ((tbNamaRekeningUSD.Text == "") && (maskedRekeningUSD.Text != ""))
                    //{
                    //    MessageBox.Show("Nomor rekening USD tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //    return;
                    //}
                    //20150624, liliana, LIBST13020, begin
                    //if ((tbNamaRekening.Text == "") && (maskedRekening.Text.Replace("-","") != ""))
                    if ((tbNamaRekening.Text == "") && (maskedRekening.Text.Replace("-", "").Trim() != ""))
                    //20150624, liliana, LIBST13020, end
                    {
                        MessageBox.Show("Nomor rekening IDR tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    //20150624, liliana, LIBST13020, begin
                    //if ((tbNamaRekeningUSD.Text == "") && (maskedRekeningUSD.Text.Replace("-","") != ""))
                    if ((tbNamaRekeningUSD.Text == "") && (maskedRekeningUSD.Text.Replace("-", "").Trim() != ""))
                    //20150624, liliana, LIBST13020, end
                    {
                        MessageBox.Show("Nomor rekening USD tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150623, liliana, LIBST13020, end
                    //20150622, liliana, LIBST13020, end
                    //20150727, liliana, LIBST13020, begin

                    if ((tbNamaRekeningMC.Text == "") && (maskedRekeningMC.Text.Replace("-", "").Trim() != ""))
                    {
                        MessageBox.Show("Nomor rekening Multicurrency tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150727, liliana, LIBST13020, end
                    //20160823, Elva, LOGEN00196, begin
                    if (comboRekIDRTA.Text != "" && txtNamaRekIDRTA.Text == "")
                    {
                        MessageBox.Show("Nomor rekening IDR Tax Amnesty tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (comboRekUSDTA.Text != "" && txtNamaRekUSDTA.Text == "")
                    {
                        MessageBox.Show("Nomor rekening USD Tax Amnesty tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (comboRekMultiCurTA.Text != "" && txtNamaRekMultiCurTA.Text == "")
                    {
                        MessageBox.Show("Nomor rekening Multicurrency Tax Amnesty tidak terdaftar!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (comboRekIDRTA.Text != "" && txtNamaRekIDRTA.Text.Trim() != cmpsrCIF.Text2.Trim())
                    {
                        if (MessageBox.Show("Apakah rekening IDR Tax Amnesty tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Gagal Simpan Data", "Error");
                            return;
                        }
                    }

                    if (comboRekUSDTA.Text != "" && txtNamaRekUSDTA.Text.Trim() != cmpsrCIF.Text2.Trim())
                    {
                        if (MessageBox.Show("Apakah rekening USD Tax Amnesty tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Gagal Simpan Data", "Error");
                            return;
                        }
                    }

                    if (comboRekMultiCurTA.Text != "" && txtNamaRekMultiCurTA.Text.Trim() != cmpsrCIF.Text2.Trim())
                    {
                        if (MessageBox.Show("Apakah rekening Multicurrency Tax Amnesty tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            MessageBox.Show("Gagal Simpan Data", "Error");
                            return;
                        }
                    }
                    //20160823, Elva, LOGEN00196, end
                    //pengecekan nama rek & nama CIF
                    //20150624, liliana, LIBST13020, begin
                    //if (tbNamaRekening.Text.Trim() != cmpsrCIF.Text2.Trim())
                    if ((maskedRekening.Text.Replace("-", "").Trim() != "") && (tbNamaRekening.Text.Trim() != cmpsrCIF.Text2.Trim()))
                    //20150624, liliana, LIBST13020, end
                    {
                        //20150422, liliana, LIBST13020, begin
                        //if (MessageBox.Show("Nama nasabah di CIF tidak sama dengan nama nasabah di rekening relasi, apakah proses save Master Nasabah akan dilanjutkan?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        //20160823, Elva, LOGEN00196, begin
                        //if (MessageBox.Show("Apakah rekening tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        if (MessageBox.Show("Apakah rekening IDR tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        //20160823, Elva, LOGEN00196, end
                        //20150422, liliana, LIBST13020, end
                        {
                            MessageBox.Show("Gagal Simpan Data", "Error");
                            return;
                        }
                    }

                    if ((maskedRekeningUSD.Text.Replace("-", "").Trim() != "") && (tbNamaRekeningUSD.Text.Trim() != cmpsrCIF.Text2.Trim()))
                    {
                        //20160823, Elva, LOGEN00196, begin
                        //if (MessageBox.Show("Apakah rekening tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        if (MessageBox.Show("Apakah rekening USD tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        //20160823, Elva, LOGEN00196, end
                        {
                            MessageBox.Show("Gagal Simpan Data", "Error");
                            return;
                        }
                    }
                    //20150624, liliana, LIBST13020, end
                    //20150727, liliana, LIBST13020, begin
                    if ((maskedRekeningMC.Text.Replace("-", "").Trim() != "") && (tbNamaRekeningMC.Text.Trim() != cmpsrCIF.Text2.Trim()))
                    {
                        //20160823, Elva, LOGEN00196, begin
                        //if (MessageBox.Show("Apakah rekening tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        if (MessageBox.Show("Apakah rekening Multicurrency tersebut milik pemegang reksadana?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        //20160823, Elva, LOGEN00196, end
                        {
                            MessageBox.Show("Gagal Simpan Data", "Error");
                            return;
                        }
                    }
                    //20150727, liliana, LIBST13020, end

                    if (cbStatus.SelectedIndex == -1)
                    {
                        MessageBox.Show("Status karyawan harus dipilih!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    //20150706, liliana, LIBST13020, begin
                    //else if ((cbStatus.SelectedIndex == 1) && (cmpsrNIK.Text1 == ""))
                    else if ((cbStatus.SelectedIndex == 0) && (cmpsrNIK.Text1 == ""))
                    //20150706, liliana, LIBST13020, end
                    {
                        //20150706, liliana, LIBST13020, begin
                        //MessageBox.Show("NIK karyawan tidak ditemukan, harap mengisi rekening sesuai nomor relasi rekening karyawan!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        MessageBox.Show("NIK karyawan tidak ditemukan!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //20150706, liliana, LIBST13020, end
                        return;
                    }
                    //20150706, liliana, LIBST13020, begin
                    //else if ((cbStatus.SelectedIndex == 0) && (cmpsrNIK.Text1 != ""))
                    else if ((cbStatus.SelectedIndex == 1) && (cmpsrNIK.Text1 != ""))
                    //20150706, liliana, LIBST13020, end
                    {
                        cmpsrNIK.Text1 = "";
                        cmpsrNIK.Text2 = "";
                    }

                    if (cbDikirimKe.SelectedIndex == -1)
                    {
                        MessageBox.Show("'Surat konfirmasi Dikirim Ke' harus dipilih!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    //20150424, liliana, LIBST13020, begin
                    //if ((cbDikirimKe.SelectedIndex == 1) && (cmpsrKodeKantor.Text1 == ""))
                    if ((cbDikirimKe.SelectedIndex == 1) && (cmpsrCabangSurat.Text1 == ""))
                    //20150424, liliana, LIBST13020, end
                    {
                        MessageBox.Show("Kode kantor alamat surat harus diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (KonfirmasiCountSelected() == 0)
                    {
                        MessageBox.Show("Wajib Memilih Alamat Konfirmasi terlebih dahulu!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (KonfirmasiCountSelected() > 1)
                    {
                        MessageBox.Show("Hanya diperbolehkan memilih 1 alamat konfirmasi!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (cbProfilResiko.Checked == false)
                    {
                        MessageBox.Show("Kelengkapan Dokumen Kuesioner Risk Profile belum diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (cbKetum.Checked == false)
                    {
                        MessageBox.Show("Kelengkapan Dokumen Ketentuan Umum Reksadana belum diisi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (txtbRiskProfile.Text == "")
                    {
                        MessageBox.Show("Data risk profile wajib ada. Mohon lengkapi dulu data di Pro CIF", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    //warning risk profile > 1 thn
                    System.TimeSpan diff = _dtCurrentDate.Subtract(dtpRiskProfile.Value);

                    //20180829, Andhika J, BOSIT18231, begin
                    GetRiskProfileParam();
                    if (diff.Days >= intMaxDay || dtExpiredRiskProfile.Value <= _dtCurrentDate)
                    {
                        //20150820, liliana, LIBST13020, begin
                        //if (MessageBox.Show("Apakah data risk profile nasabah berubah?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        if (MessageBox.Show("No CIF : " + cmpsrCIF.Text1 + "\n\nTanggal Last Update Risk Profile : " + dtpRiskProfile.Value + "\n\nTanggal Last Update Risk Profile sudah lewat " + intMaxYear + " tahun \n\n Last Update Risk Profile telah lebih dari " + intMaxYear + " tahun, \n Apakah Risk Profile Nasabah Berubah ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        //20150820, liliana, LIBST13020, end
                        {
                            MessageBox.Show("No CIF : " + cmpsrCIF.Text1 + "\n\nTanggal Last Update Risk Profile : " + dtpRiskProfile.Value + "\n\nTanggal Last Update Risk Profile sudah lewat " + intMaxYear + " tahun \n\n Last Update Risk Profile telah lebih dari " + intMaxYear + " tahun, \n Apakah Risk Profile Nasabah Berubah ?", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            //MessageBox.Show("Mohon data Risk Profile nasabah diperbaharui terlebih dahulu di Pro CIF-Menu Inquiry and Maintenance-Data Pribadi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            MessageBox.Show("Gagal Simpan Data", "Error");
                            return;
                        }
                        else
                        {
                            dtpRiskProfile.Value = System.Convert.ToDateTime(ProReksa2.Global.strCurrentDate.ToString());
                        }
                    }
                    //20180829, Andhika J, BOSIT18231, end
                    //npwp
                    //20150410, liliana, LIBST13020, begin
                    //if (_intType == 1 && _intValidationNPWP == 0)
                    //{
                    //    if (tbNoNPWPKK.Text != "" && tbNamaNPWPKK.Text != "" && cbKepemilikanNPWPKK.SelectedIndex != -1 && dtpTglNPWPKK.Value != DateTime.Now)
                    //    {
                    //        if (dtpTglNPWPKK.Value == DateTime.Parse("1900-01-1"))
                    //        {
                    //            _intValidationNPWP = 0;
                    //            MessageBox.Show("Harap mengisi tanggal NPWP 1 KK!", "Data tidak lengkap", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    //            return;
                    //        }
                    //        _intValidationNPWP = 2;
                    //    }
                    //    else if (cbAlasanTanpaNPWP.SelectedIndex != -1 && tbNoDokTanpaNPWP.Text != "")
                    //        _intValidationNPWP = 3;
                    //    else
                    //    {
                    //        _intValidationNPWP = 0;
                    //        MessageBox.Show("Harap mengisi data NPWP!", "Data tidak lengkap", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    //        return;
                    //    }
                    //}
                    //20150410, liliana, LIBST13020, end

                    if (!GlobalFunctionCIF.RetrieveCIFData(intNIK, strBranch, strModule, strGuid, Int64.Parse(cmpsrCIF.Text1)))
                    {
                        MessageBox.Show("Gagal validasi CIF ke modul ProCIF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                //20150706, liliana, LIBST13020, begin
                int intStatus;
                if (cbStatus.SelectedIndex == 0)
                {
                    intStatus = 1;
                }
                else
                {
                    intStatus = 0;
                }
                //20150706, liliana, LIBST13020, end

                //20180829, Andhika J, BOSIT18231, begin
                //save Expired Date Risk Profile
                SubSaveRiskProfile();
                //20180829, Andhika J, BOSIT18231, end

                //20210120, julio, RDN-410, begin
                //check email
                String EmailWarning;
                CekElecAddress(cmpsrCIF.Text1, out EmailWarning);
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


                //save
                DataSet dsSave;

                //20150518, liliana, LIBST13020, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[30];
                //20150727, liliana, LIBST13020, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[32];
                //20160823, Elva, LOGEN00196, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[34];
                System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[40];
                //20160823, Elva, LOGEN00196, end
                //20150727, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, end
                //20230206, Andhika J, RDN-903, begin
                #region Remark
                //dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnType", System.Data.OleDb.OleDbType.Integer);
                //dbParam[0].Value = _intType;
                //dbParam[0].Direction = System.Data.ParameterDirection.Input;

                //dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
                //dbParam[1].Value = strGuid;
                //dbParam[1].Direction = System.Data.ParameterDirection.Input;

                //dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcCIF", System.Data.OleDb.OleDbType.VarChar, 13);
                //dbParam[2].Value = cmpsrCIF.Text1;
                //dbParam[2].Direction = System.Data.ParameterDirection.Input;

                //dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcCIFName", System.Data.OleDb.OleDbType.VarChar, 100);
                //dbParam[3].Value = cmpsrCIF.Text2;
                //dbParam[3].Direction = System.Data.ParameterDirection.Input;

                //dbParam[4] = new System.Data.OleDb.OleDbParameter("@pcOfficeId", System.Data.OleDb.OleDbType.VarChar, 5);
                //dbParam[4].Value = cmpsrKodeKantor.Text1;
                //dbParam[4].Direction = System.Data.ParameterDirection.Input;

                //dbParam[5] = new System.Data.OleDb.OleDbParameter("@pcCIFType", System.Data.OleDb.OleDbType.TinyInt);
                //dbParam[5].Value = _intJnsNas;
                //dbParam[5].Direction = System.Data.ParameterDirection.Input;

                //dbParam[6] = new System.Data.OleDb.OleDbParameter("@pcShareholderID", System.Data.OleDb.OleDbType.VarChar, 12);
                //dbParam[6].Value = textShareHolderId.Text;
                //dbParam[6].Direction = System.Data.ParameterDirection.Input;

                //dbParam[7] = new System.Data.OleDb.OleDbParameter("@pcCIFBirthPlace", System.Data.OleDb.OleDbType.VarChar, 30);
                //dbParam[7].Value = tbTmptLahir.Text;
                //dbParam[7].Direction = System.Data.ParameterDirection.Input;

                //dbParam[8] = new System.Data.OleDb.OleDbParameter("@pdCIFBirthDay", System.Data.OleDb.OleDbType.Date);
                //dbParam[8].Value = (DateTime)dtpTglLahir.Value;
                //dbParam[8].Direction = System.Data.ParameterDirection.Input;

                //dbParam[9] = new System.Data.OleDb.OleDbParameter("@pdJoinDate", System.Data.OleDb.OleDbType.Date);
                //dbParam[9].Value = (DateTime)dtpJoinDate.Value;
                //dbParam[9].Direction = System.Data.ParameterDirection.Input;

                //dbParam[10] = new System.Data.OleDb.OleDbParameter("@pbIsEmployee", System.Data.OleDb.OleDbType.TinyInt);
                ////20150706, liliana, LIBST13020, begin
                ////dbParam[10].Value = cbStatus.SelectedIndex;
                //dbParam[10].Value = intStatus;
                ////20150706, liliana, LIBST13020, end
                //dbParam[10].Direction = System.Data.ParameterDirection.Input;

                //dbParam[11] = new System.Data.OleDb.OleDbParameter("@pnCIFNIK", System.Data.OleDb.OleDbType.Integer);
                //if (cmpsrNIK.Text1.Trim() == "")
                //{
                //    dbParam[11].Value = 0;
                //}
                //else
                //{
                //    dbParam[11].Value = System.Convert.ToInt32(cmpsrNIK.Text1);
                //}

                //dbParam[12] = new System.Data.OleDb.OleDbParameter("@pcNISPAccId", System.Data.OleDb.OleDbType.VarChar, 14);
                ////20150427, liliana, LIBST13020, begin
                ////dbParam[12].Value = tbRekening.Text;
                //dbParam[12].Value = maskedRekening.Text.Replace("-", "");
                ////20150427, liliana, LIBST13020, end
                //dbParam[12].Direction = System.Data.ParameterDirection.Input;

                //dbParam[13] = new System.Data.OleDb.OleDbParameter("@pcNISPAccName", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[13].Value = tbNamaRekening.Text;
                //dbParam[13].Direction = System.Data.ParameterDirection.Input;

                //dbParam[14] = new System.Data.OleDb.OleDbParameter("@pnNIKInputter", System.Data.OleDb.OleDbType.Integer);
                //dbParam[14].Value = intNIK;
                //dbParam[14].Direction = System.Data.ParameterDirection.Input;

                //dbParam[15] = new System.Data.OleDb.OleDbParameter("@pbRiskProfile", System.Data.OleDb.OleDbType.Boolean);
                //dbParam[15].Value = cbProfilResiko.Checked;
                //dbParam[15].Direction = System.Data.ParameterDirection.Input;

                //dbParam[16] = new System.Data.OleDb.OleDbParameter("@pbTermCondition", System.Data.OleDb.OleDbType.Boolean);
                //dbParam[16].Value = cbKetum.Checked;
                //dbParam[16].Direction = System.Data.ParameterDirection.Input;

                //dbParam[17] = new System.Data.OleDb.OleDbParameter("@pdRiskProfileLastUpdate", System.Data.OleDb.OleDbType.Date);
                //dbParam[17].Value = dtpRiskProfile.Value;
                //dbParam[17].Direction = System.Data.ParameterDirection.Input;

                //dbParam[18] = new System.Data.OleDb.OleDbParameter("@pcAlamatConf", System.Data.OleDb.OleDbType.VarChar, 8000);
                //dbParam[18].Value = GetAllData();
                //dbParam[18].Direction = System.Data.ParameterDirection.Input;

                //dbParam[19] = new System.Data.OleDb.OleDbParameter("@pcNoNPWPKK", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[19].Value = tbNoNPWPKK.Text;
                //dbParam[19].Direction = System.Data.ParameterDirection.Input;

                //dbParam[20] = new System.Data.OleDb.OleDbParameter("@pcNamaNPWPKK", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[20].Value = tbNamaNPWPKK.Text;
                //dbParam[20].Direction = System.Data.ParameterDirection.Input;

                //dbParam[21] = new System.Data.OleDb.OleDbParameter("@pcKepemilikanNPWPKK", System.Data.OleDb.OleDbType.VarChar, 50);
                //dbParam[21].Value = cbKepemilikanNPWPKK.Text;
                //dbParam[21].Direction = System.Data.ParameterDirection.Input;

                //dbParam[22] = new System.Data.OleDb.OleDbParameter("@pcKepemilikanNPWPKKLainnya", System.Data.OleDb.OleDbType.VarChar, 50);
                //dbParam[22].Value = tbKepemilikanLainnya.Text;
                //dbParam[22].Direction = System.Data.ParameterDirection.Input;

                //dbParam[23] = new System.Data.OleDb.OleDbParameter("@pdTglNPWPKK", System.Data.OleDb.OleDbType.Date);
                //dbParam[23].Value = dtpTglNPWPKK.Value;
                //dbParam[23].Direction = System.Data.ParameterDirection.Input;

                //dbParam[24] = new System.Data.OleDb.OleDbParameter("@pcAlasanTanpaNPWP", System.Data.OleDb.OleDbType.VarChar, 50);
                //dbParam[24].Value = cbAlasanTanpaNPWP.Text;
                //dbParam[24].Direction = System.Data.ParameterDirection.Input;

                //dbParam[25] = new System.Data.OleDb.OleDbParameter("@pcNoDokTanpaNPWP", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[25].Value = tbNoDokTanpaNPWP.Text;
                //dbParam[25].Direction = System.Data.ParameterDirection.Input;

                //dbParam[26] = new System.Data.OleDb.OleDbParameter("@pdTglDokTanpaNPWP", System.Data.OleDb.OleDbType.Date);
                //dbParam[26].Value = dtpTglDokTanpaNPWP.Value;
                //dbParam[26].Direction = System.Data.ParameterDirection.Input;

                //dbParam[27] = new System.Data.OleDb.OleDbParameter("@pcNoNPWPProCIF", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[27].Value = tbNoNPWPSendiri.Text;
                //dbParam[27].Direction = System.Data.ParameterDirection.Input;

                //dbParam[28] = new System.Data.OleDb.OleDbParameter("@pcNamaNPWPProCIF", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[28].Value = tbNamaNPWPSendiri.Text;
                //dbParam[28].Direction = System.Data.ParameterDirection.Input;

                //dbParam[29] = new System.Data.OleDb.OleDbParameter("@pnNasabahId", System.Data.OleDb.OleDbType.Integer);
                //if ((_intType == 2) || (_intType == 3))
                //{
                //    dbParam[29].Value = (int)cmpsrCIF[2];
                //}
                //else
                //{
                //    dbParam[29].Value = 0;
                //}
                ////20150518, liliana, LIBST13020, begin

                //dbParam[30] = new System.Data.OleDb.OleDbParameter("@pcNISPAccIdUSD", System.Data.OleDb.OleDbType.VarChar, 14);
                //dbParam[30].Value = maskedRekeningUSD.Text.Replace("-", "");
                //dbParam[30].Direction = System.Data.ParameterDirection.Input;

                //dbParam[31] = new System.Data.OleDb.OleDbParameter("@pcNISPAccNameUSD", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[31].Value = tbNamaRekeningUSD.Text;
                //dbParam[31].Direction = System.Data.ParameterDirection.Input;
                ////20150518, liliana, LIBST13020, end
                ////20150727, liliana, LIBST13020, begin
                //dbParam[32] = new System.Data.OleDb.OleDbParameter("@pcNISPAccIdMC", System.Data.OleDb.OleDbType.VarChar, 14);
                //dbParam[32].Value = maskedRekeningMC.Text.Replace("-", "").Trim();
                //dbParam[32].Direction = System.Data.ParameterDirection.Input;

                //dbParam[33] = new System.Data.OleDb.OleDbParameter("@pcNISPAccNameMC", System.Data.OleDb.OleDbType.VarChar, 40);
                //dbParam[33].Value = tbNamaRekeningMC.Text;
                //dbParam[33].Direction = System.Data.ParameterDirection.Input;
                ////20150727, liliana, LIBST13020, end
                ////20160823, Elva, LOGEN00196, begin
                //dbParam[34] = new System.Data.OleDb.OleDbParameter("@pcAccountIdTA", System.Data.OleDb.OleDbType.VarChar, 19);
                //dbParam[34].Value = comboRekIDRTA.Text.Trim();
                //dbParam[34].Direction = System.Data.ParameterDirection.Input;

                //dbParam[35] = new System.Data.OleDb.OleDbParameter("@pcAccountNameTA", System.Data.OleDb.OleDbType.VarChar, 100);
                //dbParam[35].Value = txtNamaRekIDRTA.Text.Trim();
                //dbParam[35].Direction = System.Data.ParameterDirection.Input;

                //dbParam[36] = new System.Data.OleDb.OleDbParameter("@pcAccountIdUSDTA", System.Data.OleDb.OleDbType.VarChar, 19);
                //dbParam[36].Value = comboRekUSDTA.Text.Trim();
                //dbParam[36].Direction = System.Data.ParameterDirection.Input;

                //dbParam[37] = new System.Data.OleDb.OleDbParameter("@pcAccountNameUSDTA", System.Data.OleDb.OleDbType.VarChar, 100);
                //dbParam[37].Value = txtNamaRekUSDTA.Text.Trim();
                //dbParam[37].Direction = System.Data.ParameterDirection.Input;

                //dbParam[38] = new System.Data.OleDb.OleDbParameter("@pcAccountIdMCTA", System.Data.OleDb.OleDbType.VarChar, 19);
                //dbParam[38].Value = comboRekMultiCurTA.Text.Trim();
                //dbParam[38].Direction = System.Data.ParameterDirection.Input;

                //dbParam[39] = new System.Data.OleDb.OleDbParameter("@pcAccountNameMCTA", System.Data.OleDb.OleDbType.VarChar, 100);
                //dbParam[39].Value = txtNamaRekMultiCurTA.Text.Trim();
                //dbParam[39].Direction = System.Data.ParameterDirection.Input;
                ////20160823, Elva, LOGEN00196, end

                ////20151006, liliana, LIBST13020, begin
                //ClQ.TimeOut = 6000;
                ////20151006, liliana, LIBST13020, end
                //bool blnResult = ClQ.ExecProc("ReksaMaintainNasabah", ref dbParam, out dsSave);
                //if (blnResult == true)
                //{
                //    if (dsSave.Tables.Count > 0)
                //    {
                //        MessageBox.Show("Data gagal disimpan!!");
                //        frmErrorMessage frmError = new frmErrorMessage();
                //        frmError.SetErrorTable(dsSave.Tables[0]);
                //        frmError.ShowDialog();
                //    }
                //    else
                //    {
                //        MessageBox.Show("Simpan Berhasil, Perlu Otorisasi Supervisor!");

                //        _intType = 0;
                //        subDisableAll(_intType);
                //        //20150610, liliana, LIBST13020, begin
                //        //subRefresh();
                //        //20150724, liliana, LIBST13020, begin
                //        //subClearAll();
                //        GetComponentSearch();
                //        //20150724, liliana, LIBST13020, end
                //        //20150610, liliana, LIBST13020, end
                //        //20150707, liliana, LIBST13020, begin
                //        subRefresh();
                //        //20150707, liliana, LIBST13020, end
                //        //20150724, liliana, LIBST13020, begin
                //        //subResetToolBar();
                //        //GetComponentSearch();
                //        //20150724, liliana, LIBST13020, end
                //    }

                //}
                //else
                //{
                //    MessageBox.Show("Error Simpan Data Nasabah");
                //}
                #endregion Remark
                #region hitAPI
                ReksaMaintainNasabahRs _response = new ReksaMaintainNasabahRs();
                DataSet dsUrl = new DataSet();
                string strUrlAPI = "";
                string _strGuid = "";
                int _incmpsrNIK = 0, _inNasabahId = 0;
                bool isDuplicate = false, blnResult = false;

                _strGuid = Guid.NewGuid().ToString();
                if (_cProc.GetAPIParam("TRX_ReksaMaintainNasabah", out dsUrl))
                {
                    strUrlAPI = dsUrl.Tables[0].Rows[0]["ParamVal"].ToString();
                }
                if (cmpsrNIK.Text1.Trim() == "")
                {
                    _incmpsrNIK = 0;
                }
                else
                {
                    _incmpsrNIK = System.Convert.ToInt32(cmpsrNIK.Text1);
                }
                if ((_intType == 2) || (_intType == 3))
                {
                    _inNasabahId = (int)cmpsrCIF[2];
                }
                else
                {
                    _inNasabahId = 0;
                }
                _ReksaMaintainNasabahRq = new ReksaMaintainNasabahRq();
                _ReksaMaintainNasabahRq.MessageGUID = _strGuid;
                _ReksaMaintainNasabahRq.ParentMessageGUID = null;
                _ReksaMaintainNasabahRq.TransactionMessageGUID = _strGuid;
                _ReksaMaintainNasabahRq.IsResponseMessage = "false";
                _ReksaMaintainNasabahRq.UserNIK = intNIK.ToString();
                _ReksaMaintainNasabahRq.ModuleName = strModule;
                _ReksaMaintainNasabahRq.MessageDateTime = DateTime.Now.ToString();
                _ReksaMaintainNasabahRq.DestinationURL = strUrlAPI;
                _ReksaMaintainNasabahRq.IsSuccess = "true";
                _ReksaMaintainNasabahRq.ErrorCode = "";
                _ReksaMaintainNasabahRq.ErrorDescription = "";
                //req data 
                _ReksaMaintainNasabahRq.Data.pnType = _intType;
                _ReksaMaintainNasabahRq.Data.pcGuid = strGuid;
                _ReksaMaintainNasabahRq.Data.pcCIF = cmpsrCIF.Text1;
                _ReksaMaintainNasabahRq.Data.pcCIFName = cmpsrCIF.Text2;
                _ReksaMaintainNasabahRq.Data.pcOfficeId = cmpsrKodeKantor.Text1;
                _ReksaMaintainNasabahRq.Data.pcCIFType = _intJnsNas;
                _ReksaMaintainNasabahRq.Data.pcShareholderID = textShareHolderId.Text;
                _ReksaMaintainNasabahRq.Data.pcCIFBirthPlace = tbTmptLahir.Text;
                _ReksaMaintainNasabahRq.Data.pdCIFBirthDay = (DateTime)dtpTglLahir.Value;
                _ReksaMaintainNasabahRq.Data.pdJoinDate = (DateTime)dtpJoinDate.Value;
                _ReksaMaintainNasabahRq.Data.pbIsEmployee = intStatus;
                _ReksaMaintainNasabahRq.Data.pnCIFNIK = _incmpsrNIK;
                _ReksaMaintainNasabahRq.Data.pcNISPAccId = maskedRekening.Text.Replace("-", "").TrimStart().TrimEnd();
                _ReksaMaintainNasabahRq.Data.pcNISPAccName = tbNamaRekening.Text;
                _ReksaMaintainNasabahRq.Data.pnNIKInputter = intNIK;
                _ReksaMaintainNasabahRq.Data.pbRiskProfile = cbProfilResiko.Checked;
                _ReksaMaintainNasabahRq.Data.pbTermCondition = cbKetum.Checked;
                _ReksaMaintainNasabahRq.Data.pdRiskProfileLastUpdate = dtpRiskProfile.Value;
                _ReksaMaintainNasabahRq.Data.pcAlamatConf = GetAllData();
                _ReksaMaintainNasabahRq.Data.pcNoNPWPKK = tbNoNPWPKK.Text;
                _ReksaMaintainNasabahRq.Data.pcNamaNPWPKK = tbNamaNPWPKK.Text;
                _ReksaMaintainNasabahRq.Data.pcKepemilikanNPWPKK = cbKepemilikanNPWPKK.Text;
                _ReksaMaintainNasabahRq.Data.pcKepemilikanNPWPKKLainnya = tbKepemilikanLainnya.Text;
                _ReksaMaintainNasabahRq.Data.pdTglNPWPKK = dtpTglNPWPKK.Value;
                _ReksaMaintainNasabahRq.Data.pcAlasanTanpaNPWP = cbAlasanTanpaNPWP.Text;
                _ReksaMaintainNasabahRq.Data.pcNoDokTanpaNPWP = tbNoDokTanpaNPWP.Text;
                _ReksaMaintainNasabahRq.Data.pdTglDokTanpaNPWP = dtpTglDokTanpaNPWP.Value;
                _ReksaMaintainNasabahRq.Data.pcNoNPWPProCIF = tbNoNPWPSendiri.Text;
                _ReksaMaintainNasabahRq.Data.pcNamaNPWPProCIF = tbNamaNPWPSendiri.Text;
                _ReksaMaintainNasabahRq.Data.pnNasabahId = _inNasabahId;
                _ReksaMaintainNasabahRq.Data.pcNISPAccIdUSD = maskedRekeningUSD.Text.Replace("-", "").TrimStart().TrimEnd();
                _ReksaMaintainNasabahRq.Data.pcNISPAccNameUSD = tbNamaRekeningUSD.Text;
                _ReksaMaintainNasabahRq.Data.pcNISPAccIdMC = maskedRekeningMC.Text.Replace("-", "").TrimStart().TrimEnd();
                _ReksaMaintainNasabahRq.Data.pcNISPAccNameMC = tbNamaRekeningMC.Text;
                _ReksaMaintainNasabahRq.Data.pcAccountIdTA = comboRekIDRTA.Text.TrimStart().TrimEnd();
                _ReksaMaintainNasabahRq.Data.pcAccountNameTA = txtNamaRekIDRTA.Text.Trim();
                _ReksaMaintainNasabahRq.Data.pcAccountIdUSDTA = comboRekUSDTA.Text.TrimStart().TrimEnd();
                _ReksaMaintainNasabahRq.Data.pcAccountNameUSDTA = txtNamaRekUSDTA.Text.Trim();
                _ReksaMaintainNasabahRq.Data.pcAccountIdMCTA = comboRekMultiCurTA.Text.TrimStart().TrimEnd();
                _ReksaMaintainNasabahRq.Data.pcAccountNameMCTA = txtNamaRekMultiCurTA.Text.Trim();
                //end
                _response = _iServiceAPI.ReksaMaintainNasabah(_ReksaMaintainNasabahRq);
                if (_response.IsSuccess == true)
                {
                    if (_response.Data.MandatoryFieldNasabah1 != null)
                    {
                        blnResult = false;
                        dsSave = new DataSet();
                        MessageBox.Show("Data gagal disimpan!!");
                        frmErrorMessage frmError = new frmErrorMessage();
                        DataTable dt = ToDataTable(_response.Data.MandatoryFieldNasabah1);
                        dsSave.Tables.Add(dt);
                        frmError.SetErrorTable(dsSave.Tables[0]);
                        frmError.ShowDialog();
                    }
                    else
                    {
                        blnResult = true;
                    }
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
                #endregion hitAPI
                if (blnResult == true)
                {
                    MessageBox.Show("Simpan Berhasil, Perlu Otorisasi Supervisor!");
                    _intType = 0;
                    subDisableAll(_intType);
                    GetComponentSearch();
                    subRefresh();
                }
                else
                {
                    MessageBox.Show("Error Simpan Data Nasabah");
                }
                //20230206, Andhika J, RDN-903, end

                dbParam = null;
                dsSave = null;
            }
            else if (_strTabName == "MCB")
            {
                if (cmpsrSrcClient.Text1 == "")
                {
                    MessageBox.Show("Client Code Harus Diisi!");
                    return;
                }

                //20230106, sandi, RDN-899, begin
                //if (nispMoneyBlokir.Value == 0)
                //{
                //    MessageBox.Show("Besar Blokir Harus Diisi");
                if (cbBlokir.SelectedIndex == 1)
                    _intType = 3;

                if (nispMoneyBlokir.Value <= 0)
                {
                    MessageBox.Show("Unit Blokir harus diisi!");
                    //20230106, sandi, RDN-899, end
                    return;
                }

                if ((dtpExpiry.Value <= dtpTglTran.Value) && (_intType != 3))
                {
                    //20230106, sandi, RDN-899, begin
                    //MessageBox.Show("Tanggal Expiry harus > Tanggal Hari ini");
                    MessageBox.Show("Tanggal expired harus lebih besar dari tanggal hari ini!");
                    //20230106, sandi, RDN-899, end
                    return;
                }

                System.Data.DataSet dsSave;

                //20230106, sandi, RDN-899, begin
                //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[9];
                System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[13];
                //20230106, sandi, RDN-899, end

                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnType", System.Data.OleDb.OleDbType.Integer);
                //20150415, liliana, LIBST13020, begin
                //dbParam[0].Value = _intType;
                //20230106, sandi, RDN-889, begin
                //dbParam[0].Value = 1;
                dbParam[0].Value = _intType;
                //20230106, sandi, RDN-889, end
                //20150415, liliana, LIBST13020, end
                dbParam[0].Direction = System.Data.ParameterDirection.Input;

                dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnClientId", System.Data.OleDb.OleDbType.Integer);
                dbParam[1].Value = (int)cmpsrSrcClient[2];
                dbParam[1].Direction = System.Data.ParameterDirection.Input;

                dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnBlockId", System.Data.OleDb.OleDbType.Integer);
                //20230106, sandi, RDN-889, begin
                //if (_intType == 3) 
                //{
                //    dbParam[2].Value = (int)dgvBlokir.CurrentRow.Cells["BlockId"].Value;
                if ((_intType == 2) || (_intType == 3))
                //20230106, sandi, RDN-889, end
                {
                    dbParam[2].Value = (int)nmBlockId.Value;
                }
                else
                {
                    dbParam[2].Value = 0;
                }
                dbParam[2].Direction = System.Data.ParameterDirection.Input;

                dbParam[3] = new System.Data.OleDb.OleDbParameter("@pmBlockAmount", System.Data.OleDb.OleDbType.Double);
                dbParam[3].Value = System.Convert.ToDouble(nispMoneyBlokir.Value);
                dbParam[3].Direction = System.Data.ParameterDirection.Input;

                dbParam[4] = new System.Data.OleDb.OleDbParameter("@pmBlockDesc", System.Data.OleDb.OleDbType.VarChar, 100);
                //20230105, sandi, RDN-899, begin
                //dbParam[4].Value = "";
                dbParam[4].Value = tbDeskripsiBlokir.Text;
                //20230105, sandi, RDN-899, end
                dbParam[4].Direction = System.Data.ParameterDirection.Input;

                dbParam[5] = new System.Data.OleDb.OleDbParameter("@pdExpiryDate", System.Data.OleDb.OleDbType.DBDate);
                dbParam[5].Value = (DateTime)dtpExpiry.Value;
                dbParam[5].Direction = System.Data.ParameterDirection.Input;

                dbParam[6] = new System.Data.OleDb.OleDbParameter("@pbAccepted", System.Data.OleDb.OleDbType.Boolean);
                dbParam[6].Value = false;
                dbParam[6].Direction = System.Data.ParameterDirection.Input;

                dbParam[7] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
                dbParam[7].Value = intNIK;
                dbParam[7].Direction = System.Data.ParameterDirection.Input;

                dbParam[8] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
                dbParam[8].Value = strGuid;
                dbParam[8].Direction = System.Data.ParameterDirection.Input;

                //20230106, sandi, RDN-899, begin
                dbParam[9] = new System.Data.OleDb.OleDbParameter("@pdBlockDate", System.Data.OleDb.OleDbType.DBDate);
                dbParam[9].Value = (DateTime)dtpTglTran.Value;
                dbParam[9].Direction = System.Data.ParameterDirection.Input;

                dbParam[10] = new System.Data.OleDb.OleDbParameter("@pnGroupIdInput", System.Data.OleDb.OleDbType.Integer);
                dbParam[10].Value = intClassificationId;
                dbParam[10].Direction = System.Data.ParameterDirection.Input;

                dbParam[11] = new System.Data.OleDb.OleDbParameter("@pnNAV", System.Data.OleDb.OleDbType.Double);
                dbParam[11].Value = System.Convert.ToDouble(nmNAVYesterday.Value);
                dbParam[11].Direction = System.Data.ParameterDirection.Input;

                dbParam[12] = new System.Data.OleDb.OleDbParameter("@pdNAVDate", System.Data.OleDb.OleDbType.DBDate);
                dbParam[12].Value = (DateTime)dtpNAVDate.Value;
                dbParam[12].Direction = System.Data.ParameterDirection.Input;
                //202301126, sandi, RDN-899, end


                bool blnResult = ClQ.ExecProc("ReksaMaintainBlokir", ref dbParam, out dsSave);
                if (blnResult == true)
                {
                    MessageBox.Show("Maintain Blokir Berhasil. Harap lakukan proses otorisasi supervisor!");
                    _intType = 0;
                    subDisableAll(_intType);
                    subRefresh();
                    subResetToolBar();
                    //20230106, sandi, RDN-899, begin
                    ReksaRefreshBlokir();
                    //20230106, sandi, RDN-899, end
                }
                else
                {
                    MessageBox.Show("Error Simpan Data Blokir!");
                }
                dbParam = null;
                dsSave = null;


            }
            else if (_strTabName == "MCA")
            {
                int intCounter = 0;
                dgvClientCode.EndEdit();

                for (int i = 0; i < dgvClientCode.Rows.Count; i++)
                {
                    if (System.Convert.ToBoolean(dgvClientCode.Rows[i].Cells["Pilih"].Value.ToString()))
                    {
                        intCounter++;
                    }
                }

                if (intCounter <= 0)
                {
                    MessageBox.Show("Harap memilih data yang akan dirubah!");
                    return;
                }


                for (int i = 0; i < dgvClientCode.Rows.Count; i++)
                {
                    if (System.Convert.ToBoolean(dgvClientCode.Rows[i].Cells["Pilih"].Value.ToString()))
                    {
                        string strClientCode = dgvClientCode["ClientCode", i].Value.ToString();
                        int intClientId = System.Convert.ToInt32(dgvClientCode["ClientId", i].Value.ToString());
                        bool boolFlag = System.Convert.ToBoolean(dgvClientCode["Flag", i].Value.ToString());
                        string strFlag = "unflag";

                        if (boolFlag)
                        {
                            strFlag = "flag";
                        }

                        if (MessageBox.Show("Apakah nasabah " + strClientCode + " akan di " + strFlag + " ?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            return;
                        }
                        else
                        {
                            subSaveFlag(intClientId, boolFlag);
                        }
                    }
                }
            }
            //20150724, liliana, LIBST13020, begin
            //lblStatus.Text = "";
            //20150724, liliana, LIBST13020, end
        }

        private void subSaveFlag(int ClientId, bool Flag)
        {
            System.Data.OleDb.OleDbParameter[] pr = new System.Data.OleDb.OleDbParameter[3];

            try
            {
                (pr[0] = new System.Data.OleDb.OleDbParameter("@pnClientId", System.Data.OleDb.OleDbType.Integer)).Value = ClientId;
                (pr[1] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer)).Value = intNIK;
                (pr[2] = new System.Data.OleDb.OleDbParameter("@pbFlag", System.Data.OleDb.OleDbType.Boolean)).Value = Flag;

                bool blnResult = ClQ.ExecProc("dbo.ReksaFlagClientId", ref pr);

                if (blnResult)
                {
                    MessageBox.Show("Proses berhasil. Client code akan diflag jika sudah diotorisasi.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    subClearAll();
                    subDisableAll(_intType);
                    subRefresh();
                    _intType = 0;
                    subResetToolBar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void subDisableAll(int _intType)
        {
            if (_intType == 0)
            {
                cmpsrCIF.Enabled = true;
                cmpsrKodeKantor.Enabled = false;
                txtJenisNasabah.Enabled = false;
                textSegment.Enabled = false;
                textSubSegment.Enabled = false;

                textSID.Enabled = false;
                textShareHolderId.Enabled = false;
                btnShareHolder.Enabled = false;
                tbNama.Enabled = false;
                tbTmptLahir.Enabled = false;
                dtpTglLahir.Enabled = false;
                tbKTP.Enabled = false;
                dtpJoinDate.Enabled = false;
                tbRekening.Enabled = false;
                //20150427, liliana, LIBST13020, begin
                maskedRekening.Enabled = false;
                //20150427, liliana, LIBST13020, end
                //20150505, liliana, LIBST13020, begin
                cbKetum.Enabled = false;
                //20150505, liliana, LIBST13020, end
                tbNamaRekening.Enabled = false;
                //20150518, liliana, LIBST13020, begin
                maskedRekeningUSD.Enabled = false;
                tbNamaRekeningUSD.Enabled = false;
                //20150518, liliana, LIBST13020, end
                //20150727, liliana, LIBST13020, begin
                maskedRekeningMC.Enabled = false;
                tbNamaRekeningMC.Enabled = false;
                //20150727, liliana, LIBST13020, end
                //20160823, Elva, LOGEN00196, begin
                comboRekIDRTA.Enabled = false;
                comboRekMultiCurTA.Enabled = false;
                comboRekUSDTA.Enabled = false;
                txtNamaRekIDRTA.Enabled = false;
                txtNamaRekMultiCurTA.Enabled = false;
                txtNamaRekUSDTA.Enabled = false;
                //20160823, Elva, LOGEN00196, begin

                //20231017, ahmad.fansyuri, RDN-1061, begin
                //cbStatus.Enabled = true;
                cbStatus.Enabled = false;
                //20231017, ahmad.fansyuri,  RDN-1061, end
                cmpsrNIK.Enabled = false;

                cbProfilResiko.Enabled = false;
                dtpRiskProfile.Enabled = false;
                checkPhoneOrder.Enabled = false;

                chkAutoRedemp.Enabled = false;
                chkRDBAsuransi.Enabled = false;

                cbDikirimKe.Enabled = false;
                cmpsrCabangSurat.Enabled = false;

                cmpsrSrcClient.Enabled = true;
                nispMoneyBlokir.Enabled = false;
                nispMoneyTotal.Enabled = false;
                nispOutsUnit.Enabled = false;
                dtpExpiry.Enabled = false;
                dtpTglTran.Enabled = false;

                tbNoNPWPSendiri.Enabled = false;
                tbNamaNPWPSendiri.Enabled = false;
                dtpTglNPWPSendiri.Enabled = false;
                tbNoNPWPKK.Enabled = false;
                tbNamaNPWPKK.Enabled = false;
                cbKepemilikanNPWPKK.Enabled = false;
                tbKepemilikanLainnya.Enabled = false;
                dtpTglNPWPKK.Enabled = false;
                cbAlasanTanpaNPWP.Enabled = false;
                tbNoDokTanpaNPWP.Enabled = false;
                dtpTglDokTanpaNPWP.Enabled = false;
                btnGenerateNoDokTanpaNPWP.Enabled = false;
                btnGantiOpsiNPWP.Enabled = false;

                //20210305, joshua, RDN-466, begin
                tbAlamatEmail.Enabled = false;
                //20210305, joshua, RDN-466, end

                //20150708, liliana, LIBST13020, begin
                //dgvKonfAddr.Enabled = false;
                if (dgvKonfAddr.Rows.Count > 0)
                {
                    for (int i = 0; i < dgvKonfAddr.Columns.Count; i++)
                    {
                        dgvKonfAddr.Columns[i].ReadOnly = true;
                    }
                }
                //20150708, liliana, LIBST13020, end
                //20230106, sandi, RDN-899, begin
                nispMoneyBlokir.Enabled = false;
                cbBlokir.Enabled = false;
                tbDeskripsiBlokir.Enabled = false;
                dgvBlokir.Enabled = true;
                dgvLogBlokir.Enabled = true;
                //20230106, sandi, RDN-899, end

            }
            else if (_intType == 1)
            {
                cmpsrKodeKantor.Enabled = false;
                txtJenisNasabah.Enabled = false;
                textSegment.Enabled = false;
                textSubSegment.Enabled = false;

                textSID.Enabled = false;
                textShareHolderId.Enabled = false;
                btnShareHolder.Enabled = false;
                tbNama.Enabled = false;
                tbTmptLahir.Enabled = false;
                dtpTglLahir.Enabled = false;
                tbKTP.Enabled = false;
                dtpJoinDate.Enabled = false;
                tbRekening.Enabled = true;
                //20150427, liliana, LIBST13020, begin
                maskedRekening.Enabled = true;
                //20150427, liliana, LIBST13020, end
                //20150518, liliana, LIBST13020, begin
                maskedRekeningUSD.Enabled = true;
                tbNamaRekeningUSD.Enabled = false;
                //20150518, liliana, LIBST13020, end
                //20150727, liliana, LIBST13020, begin
                maskedRekeningMC.Enabled = true;
                tbNamaRekeningMC.Enabled = false;
                //20150727, liliana, LIBST13020, end
                tbNamaRekening.Enabled = false;

                //20231017, ahmad.fansyuri, RDN-1061, begin
                //cbStatus.Enabled = true;
                //20231017, ahmad.fansyuri, RDN-1061, end

                cmpsrNIK.Enabled = false;
                //20160823, Elva, LOGEN00196, begin
                comboRekIDRTA.Enabled = true;
                comboRekMultiCurTA.Enabled = true;
                comboRekUSDTA.Enabled = true;
                txtNamaRekIDRTA.Enabled = false;
                txtNamaRekMultiCurTA.Enabled = false;
                txtNamaRekUSDTA.Enabled = false;
                //20160823, Elva, LOGEN00196, begin
                //20150323, liliana, LIBST13020, begin
                //cbProfilResiko.Enabled = false;
                //dtpRiskProfile.Enabled = false;
                cbProfilResiko.Enabled = true;
                //20150423, liliana, LIBST13020, begin
                //dtpRiskProfile.Enabled = true;
                dtpRiskProfile.Enabled = false;
                //20150423, liliana, LIBST13020, end
                //20150323, liliana, LIBST13020, end
                checkPhoneOrder.Enabled = false;

                chkAutoRedemp.Enabled = false;
                chkRDBAsuransi.Enabled = false;

                cbDikirimKe.Enabled = true;
                cmpsrCabangSurat.Enabled = true;

                cmpsrSrcClient.Enabled = true;
                nispMoneyBlokir.Enabled = false;
                nispMoneyTotal.Enabled = false;
                nispOutsUnit.Enabled = false;
                dtpExpiry.Enabled = false;
                dtpTglTran.Enabled = false;

                tbNoNPWPSendiri.Enabled = false;
                tbNamaNPWPSendiri.Enabled = false;
                dtpTglNPWPSendiri.Enabled = false;
                tbNoNPWPKK.Enabled = false;
                tbNamaNPWPKK.Enabled = false;
                cbKepemilikanNPWPKK.Enabled = false;
                tbKepemilikanLainnya.Enabled = false;
                dtpTglNPWPKK.Enabled = false;
                cbAlasanTanpaNPWP.Enabled = false;
                tbNoDokTanpaNPWP.Enabled = false;
                dtpTglDokTanpaNPWP.Enabled = false;
                btnGenerateNoDokTanpaNPWP.Enabled = false;
                btnGantiOpsiNPWP.Enabled = false;

                //20150708, liliana, LIBST13020, begin
                //dgvKonfAddr.Enabled = true;
                if (dgvKonfAddr.Rows.Count > 0)
                {
                    for (int i = 0; i < dgvKonfAddr.Columns.Count; i++)
                    {
                        dgvKonfAddr.Columns[i].ReadOnly = false;
                    }
                }
                //20150708, liliana, LIBST13020, end
            }
            //20230106, sandi, RDN-899, begin
            else if ((_intType == 2) || (_intType == 3))
            {
                if (_strTabName == "MCB")
                {
                    nispMoneyBlokir.Enabled = false;
                    dtpExpiry.Enabled = false;
                    cbBlokir.Enabled = false;
                    tbDeskripsiBlokir.Enabled = false;
                    dgvBlokir.Enabled = true;
                    dgvLogBlokir.Enabled = true;
                }
            }
            //20230106, sandi, RDN-899, end

            panelAlamatCabang.Visible = false;
            panelAlamatNasabah.Visible = false;

        }

        private void subClearAll()
        {
            //20230106, sandi, RDN-899, begin
            if (_strTabName != "MCB")
            {
                //20230106, sandi, RDN-899, end
                tabControlClient.SelectedIndex = 0;
                lblStatus.Text = "";

                cmpsrCIF.Text1 = "";
                cmpsrCIF.Text2 = "";
                txtJenisNasabah.Text = "";
                textSegment.Text = "";
                textSubSegment.Text = "";

                textSID.Text = "";
                textShareHolderId.Text = "";
                tbNama.Text = "";

                tbTmptLahir.Text = "";
                //20150610, liliana, LIBST13020, begin
                //dtpTglLahir.Value = DateTime.Parse("1900-01-01");
                dtpTglLahir.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end
                tbKTP.Text = "";
                //20140324, liliana, LIBST13020, begin
                tbHP.Text = "";
                //20140324, liliana, LIBST13020, end
                //20150610, liliana, LIBST13020, begin
                //dtpJoinDate.Value = DateTime.Parse("1900-01-01");
                dtpJoinDate.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end
                tbRekening.Text = "";
                //20150427, liliana, LIBST13020, begin
                maskedRekening.Text = "";
                //20150427, liliana, LIBST13020, end
                tbNamaRekening.Text = "";
                //20150518, liliana, LIBST13020, begin
                maskedRekeningUSD.Text = "";
                tbNamaRekeningUSD.Text = "";
                //20150518, liliana, LIBST13020, end
                //20150727, liliana, LIBST13020, begin
                maskedRekeningMC.Text = "";
                tbNamaRekeningMC.Text = "";
                //20150727, liliana, LIBST13020, end
                //20160823, Elva, LOGEN00196, begin
                comboRekIDRTA.Text = "";
                comboRekMultiCurTA.Text = "";
                comboRekUSDTA.Text = "";
                txtNamaRekIDRTA.Text = "";
                txtNamaRekMultiCurTA.Text = "";
                txtNamaRekUSDTA.Text = "";
                //20160823, Elva, LOGEN00196, end
                cbStatus.SelectedIndex = -1;
                cmpsrNIK.Text1 = "";
                cmpsrNIK.Text2 = "";

                txtbRiskProfile.Text = "";
                //20150610, liliana, LIBST13020, begin
                //dtpRiskProfile.Value = DateTime.Parse("1900-01-01");
                dtpRiskProfile.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end
                checkPhoneOrder.Checked = false;

                cbProfilResiko.Checked = false;
                cbKetum.Checked = false;

                cbDikirimKe.SelectedIndex = -1;
                dgvKonfAddr.DataSource = null;
                cmpsrCabangSurat.Text1 = "";
                cmpsrCabangSurat.Text2 = "";
                tbAlamatSaatIni1.Text = "";
                tbAlamatSaatIni2.Text = "";
                tbKodePos.Text = "";
                tbKotaNasabahAlmt.Text = "";
                tbProvNasabahAlmt.Text = "";
                //20180802, Lita, LOGEN00649, begin
                txtLastUpdated.Text = "";
                //20180802, Lita, LOGEN00649, end

                dgvClientCode.DataSource = null;
                dgvAktifitas.DataSource = null;
                //20150610, liliana, LIBST13020, begin
                //dtRDBJatuhTempo.Value = DateTime.Parse("1900-01-01");
                dtRDBJatuhTempo.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end
                chkRDBAsuransi.Checked = false;
                chkAutoRedemp.Checked = false;
                textFrekPendebetan.Text = "";
                txtRDBJangkaWaktu.Text = "";
                //20150610, liliana, LIBST13020, begin
                //dtpStartDate.Value = DateTime.Parse("1900-01-01");
                //dtpEndDate.Value = DateTime.Parse("1900-01-01");
                dtpStartDate.Value = DateTime.Today;
                dtpEndDate.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end

                //20200620, Lita, RDN-88, begin
                dtRDBStartDebetDate.Value = DateTime.Today;
                lblFreqDebetUnit.Text = "";
                //20200620, Lita, RDN-88, end

                tbNoNPWPSendiri.Text = "";
                tbNamaNPWPSendiri.Text = "";
                dtpTglNPWPSendiri.Value = DateTime.Now;
                tbNoNPWPKK.Text = "";
                tbNamaNPWPKK.Text = "";
                cbKepemilikanNPWPKK.SelectedIndex = -1;
                tbKepemilikanLainnya.Text = "";
                //20150610, liliana, LIBST13020, begin
                //dtpTglNPWPKK.Value = DateTime.Parse("1900-01-01");
                dtpTglNPWPKK.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end
                cbAlasanTanpaNPWP.SelectedIndex = -1;
                tbNoDokTanpaNPWP.Text = "";
                //20150610, liliana, LIBST13020, begin
                //dtpTglDokTanpaNPWP.Value = DateTime.Parse("1900-01-01");
                dtpTglDokTanpaNPWP.Value = DateTime.Today;
                //20150610, liliana, LIBST13020, end

                //20210305, joshua, RDN-466, begin
                tbAlamatEmail.Text = "";
                //20210305, joshua, RDN-466, end

                subClearBlokir();
                //20161004, liliana, CSODD16311, begin
                lblTaxAmnesty.Visible = false;
                //20161004, liliana, CSODD16311, end
                //20230106, sandi, RDN-899, begin
            }
            else if (_strTabName == "MCB")
            {
                subClearBlokir();
            }
            //20230106, sandi, RDN-899, end
        }

        private void subClearBlokir()
        {
            cmpsrSrcClient.Text1 = "";
            cmpsrSrcClient.Text2 = "";
            nispMoneyBlokir.Value = 0;
            nispMoneyTotal.Value = 0;
            nispOutsUnit.Value = 0;
            dgvBlokir.DataSource = null;
            //20150610, liliana, LIBST13020, begin
            //dtpExpiry.Value = DateTime.Parse("1900-01-01");
            //dtpTglTran.Value = DateTime.Parse("1900-01-01");
            //20230106, sandi, RDN-899, begin
            dtpExpiry.Value = DateTime.Today;
            dtpTglTran.Value = DateTime.Today;
            dtpNAVDate.Value = DateTime.Today;
            dtpInputDate.Value = DateTime.Today;
            //20230106, sandi, RDN-899, end
            //20150610, liliana, LIBST13020, end
            //20230106, sandi, RDN-889, begin
            tbProdCode.Text = "";
            tbProdName.Text = "";
            nmNilaiMarket.Value = 0;
            dgvLogBlokir.DataSource = null;
            cbBlokir.SelectedIndex = 0;
            cbBlokir.Text = "";
            tbDeskripsiBlokir.Text = "";
            nmNAVYesterday.Value = 0;
            nmBlockId.Value = 0;
            nmEffectiveUnit.Value = 0;
            cmpsrSrcClient.Enabled = true;
            //20230106, sandi, RDN-889, end
        }

        private void subNew()
        {
            _intType = 1; //set tipe jadi new

            //20230106, sandi, RDN-899, begin
            if (_strTabName == "MCB")
            {
                if (cmpsrSrcClient.Text2 == "")
                {
                    _intType = 0;
                    MessageBox.Show("Silahkan pilih Client Code terlebih dahulu!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ReksaRefreshBlokir();
                nmBlockId.Value = 0;
                nispMoneyBlokir.Enabled = true;
                nispMoneyBlokir.Value = 0;
                dtpExpiry.Enabled = true;
                cbBlokir.SelectedIndex = 0;
                cbBlokir.Text = "Blokir";
                cbBlokir.Enabled = false;
                tbDeskripsiBlokir.Enabled = true;
                dtpExpiry.Value = DateTime.Today;
                dtpTglTran.Value = DateTime.Today;
                tbDeskripsiBlokir.Text = "";
                dtpInputDate.Value = DateTime.Today;
                dgvLogBlokir.DataSource = null;
                dtpTglTran.Enabled = true;
                cmpsrSrcClient.Enabled = false;
                subResetToolBar();
            }
            else
            {
                //20230106, sandi, RDN-899, end
                subClearAll();
                subDisableAll(_intType);
                subResetToolBar();
                lblStatus.Text = "NEW";
                dtpEndDate.Value = _dtCurrentDate;
                dtpJoinDate.Value = _dtCurrentDate;
                dtpStartDate.Value = _dtCurrentDate;
                dtpTglLahir.Value = _dtCurrentDate;
                dtpTglTran.Value = _dtCurrentDate;
                dtpExpiry.Value = _dtCurrentDate;
                dtpTglTran.Value = _dtCurrentDate;
                dtpRiskProfile.Value = _dtCurrentDate;
                dtRDBJatuhTempo.Value = _dtCurrentDate;
                //20200620, Lita, RDN-88, begin
                dtRDBStartDebetDate.Value = _dtCurrentDate;
                //20200620, Lita, RDN-88, end

                dtpTglNPWPSendiri.Value = _dtCurrentDate;
                dtpTglNPWPKK.Value = _dtCurrentDate;
                dtpTglDokTanpaNPWP.Value = _dtCurrentDate;
                cmpsrCIF.SearchDesc = "CUSTOMER_ID"; //buat yang new
                intId = 0;
                //20160509, Elva, CSODD16117, begin
                if (ValidasiKodeKantor(strBranch))
                {
                    //20160509, Elva, CSODD16117, end
                    //20150723, liliana, LIBST13020, begin
                    cmpsrKodeKantor.Text1 = strBranch;
                    cmpsrKodeKantor.ValidateField();
                    //20160509, Elva, CSODD16117, begin
                    //ValidasiKodeKantor(cmpsrKodeKantor.Text1);
                    //20150723, liliana, LIBST13020, end
                    SetEnableOfficeId(strBranch);
                }
                //20160509, Elva, CSODD16117, end
                //20230106, sandi, RDN-899, begin
            }
            //20230106, sandi, RDN-899, end
        }

        private void GetDataCIF(string CIFNo, int _intType)
        {
            System.Data.DataSet dsDataCIF;

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[5];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 13);
            dbParam[0].Value = CIFNo;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
            dbParam[1].Value = intNIK;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
            dbParam[2].Value = strGuid;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcNPWP", System.Data.OleDb.OleDbType.VarChar, -1);
            dbParam[3].Value = null;
            dbParam[3].Direction = System.Data.ParameterDirection.Input;

            dbParam[4] = new System.Data.OleDb.OleDbParameter("@pcNIK", System.Data.OleDb.OleDbType.VarChar, 10);
            dbParam[4].Value = null;
            dbParam[4].Direction = System.Data.ParameterDirection.Input;

            //20151006, liliana, LIBST13020, begin
            ClQ.TimeOut = 6000;
            //20151006, liliana, LIBST13020, end
            bool blnResult = ClQ.ExecProc("ReksaGetCIFData", ref dbParam, out dsDataCIF);

            if (blnResult == true)
            {
                if (dsDataCIF.Tables[0].Rows.Count > 0)
                {
                    _intJnsNas = (int)dsDataCIF.Tables[0].Rows[0]["JnsNas"];
                    txtJenisNasabah.Text = Enum.GetName(typeof(JnsNasabah), _intJnsNas);

                    textShareHolderId.Text = dsDataCIF.Tables[0].Rows[0]["ShareholderID"].ToString();

                    if ((_intType == 1) && (textShareHolderId.Text == ""))
                    {
                        btnShareHolder.Enabled = true;
                    }

                    tbTmptLahir.Text = dsDataCIF.Tables[0].Rows[0]["TempatLhr"].ToString();
                    dtpTglLahir.Value = (DateTime)dsDataCIF.Tables[0].Rows[0]["TglLhr"];
                    tbKTP.Text = dsDataCIF.Tables[0].Rows[0]["KTP"].ToString();
                    tbHP.Text = dsDataCIF.Tables[0].Rows[0]["HP"].ToString();
                    textSID.Text = dsDataCIF.Tables[0].Rows[0]["CIFSID"].ToString();
                    //20140324, liliana, LIBST13020, begin
                    textSubSegment.Text = dsDataCIF.Tables[0].Rows[0]["SubSegment"].ToString();
                    textSegment.Text = dsDataCIF.Tables[0].Rows[0]["Segment"].ToString();
                    //20140324, liliana, LIBST13020, end

                    dtpJoinDate.Value = _dtCurrentDate;

                    checkPhoneOrder.Checked = GlobalFunctionCIF.CekCIFProductFacility(CIFNo);
                    SetDocStatus(CIFNo, _intType);
                    GetRiskProfile(CIFNo);

                    if (_intType == 1)
                    {
                        tbNama.Enabled = false;
                        tbNama.Text = dsDataCIF.Tables[0].Rows[0]["CIFName"].ToString();
                        //20150904, liliana, LIBST13020, begin
                        if (dsDataCIF.Tables[0].Rows[0]["WarnMsg"].ToString() != "")
                        {
                            MessageBox.Show(dsDataCIF.Tables[0].Rows[0]["WarnMsg"].ToString(), "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        //20150904, liliana, LIBST13020, end
                        if (!GetKonfAddress(CIFNo))
                        {
                            MessageBox.Show("Gagal Ambil Data Alamat Konfirmasi!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    //20140324, liliana, LIBST13020, begin
                    //20140415, liliana, LIBST13020, begin
                    //cmpsrCIF.Text2 = tbNama.Text;
                    //cmpsrCIF.ValidateField();
                    btnGantiOpsiNPWP.Enabled = true;
                    _intValidationNPWP = 1;
                    _intOpsiNPWP = 1;
                    //20140415, liliana, LIBST13020, end
                    //20140324, liliana, LIBST13020, end

                    //npwp
                    tbNoNPWPSendiri.Text = dsDataCIF.Tables[0].Rows[0]["NPWP"].ToString();
                    tbNamaNPWPSendiri.Text = dsDataCIF.Tables[0].Rows[0]["NamaNPWP"].ToString();
                    dtpTglNPWPSendiri.Value = (DateTime)dsDataCIF.Tables[0].Rows[0]["TglNPWP"];
                    tbNoNPWPKK.Text = "";
                    tbNamaNPWPKK.Text = "";
                    cbKepemilikanNPWPKK.SelectedIndex = -1;
                    tbKepemilikanLainnya.Text = "";
                    //20150610, liliana, LIBST13020, begin
                    //dtpTglNPWPKK.Value = DateTime.Parse("1900-01-01");
                    dtpTglNPWPKK.Value = DateTime.Today;
                    //20150610, liliana, LIBST13020, end
                    cbAlasanTanpaNPWP.SelectedIndex = -1;
                    tbNoDokTanpaNPWP.Text = "";
                    //20150610, liliana, LIBST13020, begin
                    //dtpTglDokTanpaNPWP.Value = DateTime.Parse("1900-01-01");
                    dtpTglDokTanpaNPWP.Value = DateTime.Today;
                    //20150610, liliana, LIBST13020, end

                    EnableFieldNPWP(1);
                    ////20150410, liliana, LIBST13020, begin
                    //btnGantiOpsiNPWP.Enabled = false;

                    //_intValidationNPWP = 1;
                    //_intOpsiNPWP = 1;

                    ////20150410, liliana, LIBST13020, begin
                    //if (tbNamaNPWPSendiri.Text.Trim() != tbNama.Text.Trim())
                    //{
                    //    if (tbNoNPWPSendiri.Text.Replace(".", "").Trim() == "")
                    //    {
                    //        _intValidationNPWP = 0;
                    //    }
                    //    else if (dsDataCIF.Tables[1].Rows.Count == 0)
                    //    {
                    //        if (MessageBox.Show("Nama CIF dan Nama NPWP Nasabah berbeda, Apakah NPWP No: " + tbNoNPWPSendiri.Text.Trim() + " a/n " + tbNamaNPWPSendiri.Text.Trim() + " adalah benar milik Nasabah Reksa Dana ini?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    //            _intValidationNPWP = 0;
                    //    }
                    //    else if (tbNamaNPWPSendiri.Text.Trim() == dsDataCIF.Tables[1].Rows[0]["NamaNPWPProCIF"].ToString().Trim())
                    //    {
                    //        _intValidationNPWP = 0;
                    //    }
                    //    else if (tbNamaNPWPSendiri.Text.Trim() != dsDataCIF.Tables[1].Rows[0]["NamaNPWPProCIF"].ToString().Trim())
                    //    {
                    //        if (MessageBox.Show("Nama CIF dan Nama NPWP Nasabah berbeda, Apakah NPWP No: " + tbNoNPWPSendiri.Text.Trim() + " a/n " + tbNamaNPWPSendiri.Text.Trim() + " adalah benar milik Nasabah Reksa Dana ini?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    //            _intValidationNPWP = 0;
                    //    }

                    //    if (_intValidationNPWP == 0)
                    //    {
                    //        if (dsDataCIF.Tables[1].Rows.Count > 0)
                    //        {
                    //            if (dsDataCIF.Tables[1].Rows[0]["Opsi"].ToString() == "1")
                    //            {
                    //                _intOpsiNPWP = 1;
                    //            }
                    //            else if (dsDataCIF.Tables[1].Rows[0]["Opsi"].ToString() == "2")
                    //            {
                    //                tbNoNPWPKK.Text = dsDataCIF.Tables[1].Rows[0]["NoNPWPKK"].ToString();
                    //                tbNamaNPWPKK.Text = dsDataCIF.Tables[1].Rows[0]["NamaNPWPKK"].ToString();
                    //                cbKepemilikanNPWPKK.SelectedValue = int.Parse(dsDataCIF.Tables[1].Rows[0]["KepemilikanNPWPKK"].ToString());
                    //                tbKepemilikanLainnya.Text = dsDataCIF.Tables[1].Rows[0]["KepemilikanNPWPKKLainnya"].ToString();
                    //                dtpTglNPWPKK.Value = DateTime.Parse(dsDataCIF.Tables[1].Rows[0]["TglNPWPKK"].ToString());

                    //                _intOpsiNPWP = 2;
                    //            }
                    //            else if (dsDataCIF.Tables[1].Rows[0]["Opsi"].ToString() == "3")
                    //            {
                    //                cbAlasanTanpaNPWP.SelectedValue = int.Parse(dsDataCIF.Tables[1].Rows[0]["AlasanTanpaNPWP"].ToString());
                    //                tbNoDokTanpaNPWP.Text = dsDataCIF.Tables[1].Rows[0]["NoDokTanpaNPWP"].ToString();
                    //                dtpTglDokTanpaNPWP.Value = DateTime.Parse(dsDataCIF.Tables[1].Rows[0]["TglDokTanpaNPWP"].ToString());

                    //                _intOpsiNPWP = 3;
                    //            }

                    //            EnableFieldNPWP(1);
                    //            btnGantiOpsiNPWP.Enabled = true;
                    //            _intValidationNPWP = _intOpsiNPWP;
                    //        }
                    //        else
                    //        {
                    //            string strOpsiInfo = "";
                    //            if (tbNoNPWPSendiri.Text.Replace(".", "").Trim() == "")
                    //                strOpsiInfo = "Data NPWP pada Pro CIF Nasabah belum ada/masih kosong";
                    //            else if (tbNamaNPWPSendiri.Text.Trim() != cmpsrCIF.Text2.Trim())
                    //                strOpsiInfo = "Data NPWP sudah ada di Pro CIF, tetapi Nama NPWP dan Nama CIF berbeda";


                    //            frmOpsiNPWP frmOpsi = new frmOpsiNPWP();
                    //            frmOpsi.lblOpsiInfo.Text = strOpsiInfo;
                    //            frmOpsi.ShowDialog();
                    //            _intOpsiNPWP = frmOpsi.intOpsi;
                    //            EnableFieldNPWP(_intOpsiNPWP);
                    //            if (_intOpsiNPWP == 1)
                    //            {
                    //                btnGantiOpsiNPWP.Enabled = false;
                    //                MessageBox.Show("Lakukan pembaharuan data NPWP pada ProCIF melalui menu ?CIF / CIF Inquiry and Maintenance / Maintenance Nasabah / ID Tambahan? dengan Jenis Identitas adalah NPWP.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //            }
                    //            else
                    //                btnGantiOpsiNPWP.Enabled = true;
                    //        }
                    //    }
                    //}
                    //20150410, liliana, LIBST13020, end

                    //20231017, ahmad.fansyuri, RDN-1061, begin
                    if (dsDataCIF.Tables[2].Rows[0]["NIK"].ToString() != "")
                    {
                        cbStatus.SelectedIndex = 0;
                        cmpsrNIK.ReadOnly = false;
                        cmpsrNIK._Text1.Text = dsDataCIF.Tables[2].Rows[0]["NIK"].ToString();
                        cmpsrNIK._Text2.Text = dsDataCIF.Tables[2].Rows[0]["Nama"].ToString();
                    }
                    else
                    {
                        cbStatus.SelectedIndex = 1;
                    }
                    //20231017, ahmad.fansyuri, RDN-1061, end

                }
            }
            //20150901, liliana, LIBST13020, begin
            else
            {
                subClearAll();
            }
            //20150901, liliana, LIBST13020, end
        }

        private void EnableFieldNPWP(int intOpsi)
        {
            switch (intOpsi)
            {
                case 1:
                    tbNoNPWPKK.Enabled = false;
                    tbNamaNPWPKK.Enabled = false;
                    cbKepemilikanNPWPKK.Enabled = false;
                    tbKepemilikanLainnya.Enabled = false;
                    dtpTglNPWPKK.Enabled = false;
                    cbAlasanTanpaNPWP.Enabled = false;
                    btnGenerateNoDokTanpaNPWP.Enabled = false;
                    break;
                case 2:
                    tbNoNPWPKK.Enabled = true;
                    tbNamaNPWPKK.Enabled = true;
                    cbKepemilikanNPWPKK.Enabled = true;
                    tbKepemilikanLainnya.Enabled = false;
                    dtpTglNPWPKK.Enabled = true;
                    cbAlasanTanpaNPWP.Enabled = false;
                    btnGenerateNoDokTanpaNPWP.Enabled = false;
                    break;
                case 3:
                    tbNoNPWPKK.Enabled = false;
                    tbNamaNPWPKK.Enabled = false;
                    cbKepemilikanNPWPKK.Enabled = false;
                    tbKepemilikanLainnya.Enabled = false;
                    dtpTglNPWPKK.Enabled = false;
                    cbAlasanTanpaNPWP.Enabled = true;
                    btnGenerateNoDokTanpaNPWP.Enabled = true;
                    dtpTglDokTanpaNPWP.Value = DateTime.Now;
                    break;
            }
        }

        private Boolean GetKonfAddress(string strCIFNo)
        {
            bool blnSuccess = false;

            DataSet dsOut;
            string strMessage = "";
            System.Data.OleDb.OleDbParameter[] param = new System.Data.OleDb.OleDbParameter[7];

            param[0] = new System.Data.OleDb.OleDbParameter("@pnType", System.Data.OleDb.OleDbType.Integer);
            param[0].Value = 2;
            param[0].Direction = System.Data.ParameterDirection.Input;

            param[1] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 13);
            param[1].Value = strCIFNo;
            param[1].Direction = System.Data.ParameterDirection.Input;

            param[2] = new System.Data.OleDb.OleDbParameter("@pcBranch", System.Data.OleDb.OleDbType.Char, 5);
            //20160602, Elva, CSODD16117, begin
            //param[2].Value = strBranch;
            param[2].Value = cmpsrKodeKantor.Text1;
            //20160602, Elva, CSODD16117, end
            param[2].Direction = System.Data.ParameterDirection.Input;

            param[3] = new System.Data.OleDb.OleDbParameter("@pnId", System.Data.OleDb.OleDbType.Integer);
            param[3].Value = intId;
            param[3].Direction = System.Data.ParameterDirection.Input;

            param[4] = new System.Data.OleDb.OleDbParameter("@pcMessage", System.Data.OleDb.OleDbType.VarChar, 100);
            param[4].Direction = System.Data.ParameterDirection.Output;

            param[5] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
            param[5].Value = intNIK;
            param[5].Direction = System.Data.ParameterDirection.Input;

            param[6] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
            param[6].Value = strGuid;
            param[6].Direction = System.Data.ParameterDirection.Input;

            bool blnResult = ClQ.ExecProc("ReksaGetConfAddress", ref param, out dsOut);

            if (blnResult == true)
            {
                if (dsOut.Tables[0].Rows.Count > 0)
                {
                    cbDikirimKe.SelectedIndex = System.Convert.ToInt32(dsOut.Tables[0].Rows[0][0]);
                }
                else
                {
                    cbDikirimKe.SelectedIndex = 0;
                }

                if (cbDikirimKe.SelectedIndex == 0)
                {
                    panelAlamatCabang.Visible = false;
                    panelAlamatNasabah.Visible = true;
                    //20210305, joshua, RDN-466, begin
                    panelAlamatEmail.Visible = false;
                    //20210305, joshua, RDN-466, end
                }
                else if (cbDikirimKe.SelectedIndex == 1)
                {
                    panelAlamatCabang.Visible = true;
                    panelAlamatNasabah.Visible = false;
                    //20210305, joshua, RDN-466, begin
                    panelAlamatEmail.Visible = false;
                    //20210305, joshua, RDN-466, end
                }
                //20210305, joshua, RDN-466, begin
                else if (cbDikirimKe.SelectedIndex == 2)
                {
                    panelAlamatCabang.Visible = false;
                    panelAlamatNasabah.Visible = false;
                    panelAlamatEmail.Visible = true;
                }
                //20210305, joshua, RDN-466, end
                else
                {
                    panelAlamatCabang.Visible = false;
                    panelAlamatNasabah.Visible = false;
                    //20210305, joshua, RDN-466, begin
                    panelAlamatEmail.Visible = false;
                    //20210305, joshua, RDN-466, end
                }

                dgvKonfAddr.DataSource = dsOut.Tables[1];
                dgvKonfAddr.AutoResizeColumns();
                //20150908, liliana, LIBST13020, begin
                if (_intType == 0)
                {
                    if (dgvKonfAddr.Rows.Count > 0)
                    {
                        for (int i = 0; i < dgvKonfAddr.Columns.Count; i++)
                        {
                            dgvKonfAddr.Columns[i].ReadOnly = true;
                        }
                    }
                }
                else
                {
                    if (dgvKonfAddr.Rows.Count > 0)
                    {
                        for (int i = 0; i < dgvKonfAddr.Columns.Count; i++)
                        {
                            dgvKonfAddr.Columns[i].ReadOnly = false;
                        }
                    }
                }
                //20150908, liliana, LIBST13020, end

                dsBranch.Tables.Clear();
                dsBranch.Tables.Add(dsOut.Tables[2].Copy());

                //20150320, liliana, LIBST13020, begin
                if (dsOut.Tables[2].Rows.Count > 0)
                {
                    //20150320, liliana, LIBST13020, end
                    cmpsrCabangSurat.Text1 = dsOut.Tables[2].Rows[0]["Branch"].ToString();
                    cmpsrCabangSurat.Text2 = dsOut.Tables[2].Rows[0]["BranchName"].ToString();

                    tbAlamatSaatIni1.Text = dsOut.Tables[2].Rows[0]["AddressLine1"].ToString();
                    tbAlamatSaatIni2.Text = dsOut.Tables[2].Rows[0]["AddressLine2"].ToString();
                    tbKodePos.Text = dsOut.Tables[2].Rows[0]["PostalCode"].ToString();
                    tbKotaNasabahAlmt.Text = dsOut.Tables[2].Rows[0]["Kota"].ToString();
                    tbProvNasabahAlmt.Text = dsOut.Tables[2].Rows[0]["Province"].ToString();
                    //20180802, Lita, LOGEN00649, begin
                    txtLastUpdated.Text = dsOut.Tables[2].Rows[0]["LastUpdatedDate"].ToString(); ;
                    //20180802, Lita, LOGEN00649, end

                    //20150320, liliana, LIBST13020, begin
                }
                //20150320, liliana, LIBST13020, end
                //20210305, joshua, RDN-466, begin
                dsEmail.Tables.Clear();
                dsEmail.Tables.Add(dsOut.Tables[3].Copy());

                if (dsOut.Tables[3].Rows.Count > 0)
                {
                    tbAlamatEmail.Text = dsOut.Tables[3].Rows[0]["eStatement"].ToString();
                }
                //20210305, joshua, RDN-466, end

                strMessage = param[4].Value.ToString();

                SetDisplay();

                blnSuccess = true;
            }
            return blnSuccess;
        }

        private void SetDisplay()
        {
            foreach (DataGridViewColumn dgvCol in dgvKonfAddr.Columns)
            {
                if (dgvCol.DataPropertyName == "Pilih")
                {
                    dgvCol.Frozen = true;

                }
                else
                {
                    if ((dgvCol.Name == "Kode Pos")
                        ||
                        (dgvCol.Name == "Address Line 1")
                        ||
                        (dgvCol.Name == "Address Line 2")
                        ||
                        (dgvCol.Name == "Address Line 3")
                        ||
                        (dgvCol.Name == "Address Line 4")
                        ||
                        (dgvCol.Name == "Alamat Utama")
                        ||
                        (dgvCol.Name == "Kode Alamat")
                        ||
                        (dgvCol.Name == "Alamat SID")
                        ||
                        (dgvCol.Name == "PeriodThere")
                        ||
                        (dgvCol.Name == "PeriodThereCode")
                        ||
                        (dgvCol.Name == "Jenis Alamat")
                        ||
                        (dgvCol.Name == "StaySince")
                        ||
                        (dgvCol.Name == "Kelurahan")
                        ||
                        (dgvCol.Name == "Kecamatan")
                        ||
                        (dgvCol.Name == "Kota")
                        ||
                        (dgvCol.Name == "Provinsi")
                        )
                    {
                        dgvCol.Visible = false;
                    }
                    dgvCol.ReadOnly = true;
                }
                dgvCol.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            foreach (DataGridViewRow dgvRow in dgvKonfAddr.Rows)
            {
                if (dgvRow.Cells["Alamat Luar Negeri"].Value.ToString().Trim() == "Y")
                {
                    dgvRow.DefaultCellStyle.BackColor = Color.MistyRose;
                    dgvRow.DefaultCellStyle.ApplyStyle(dgvRow.DefaultCellStyle);
                    dgvRow.ReadOnly = true;
                }
                else
                {
                    dgvRow.DefaultCellStyle.BackColor = Color.LightSkyBlue;
                    dgvRow.DefaultCellStyle.ApplyStyle(dgvRow.DefaultCellStyle);
                }

            }
        }

        private void GetRiskProfile(string CIFNo)
        {
            System.Data.DataSet dsRiskResult;

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[1];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 13);
            dbPar[0].Value = CIFNo;
            dbPar[0].Direction = System.Data.ParameterDirection.Input;

            bool blnRes = ClQ.ExecProc("ReksaGetRiskProfile", ref dbPar, out dsRiskResult);
            if (blnRes)
            {
                txtbRiskProfile.Text = dsRiskResult.Tables[0].Rows[0]["RiskProfile"].ToString();
                dtpRiskProfile.Value = (DateTime)dsRiskResult.Tables[0].Rows[0]["LastUpdate"];
            }

            if (txtbRiskProfile.Text == "")
            {
                MessageBox.Show("Data risk profile belum ada");
            }
        }

        private void SetDocStatus(string CIFNo, int _intType)
        {
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];
            DataSet dsResult;

            try
            {
                (dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 13)).Value = CIFNo;

                if (ClQ.ExecProc("dbo.ReksaGetDocStatus", ref dbParam, out dsResult))
                {
                    cbKetum.Checked = (bool)dsResult.Tables[0].Rows[0]["DocTermCondition"];
                    cbProfilResiko.Checked = (bool)dsResult.Tables[0].Rows[0]["DocRiskProfile"];

                    if (_intType != 0)
                    {
                        cekCheckbox();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
            }
        }

        private void cekCheckbox()
        {
            if (cbKetum.Checked)
            {
                cbKetum.Enabled = false;
            }
            else
            {
                cbKetum.Enabled = true;
            }

            if (cbProfilResiko.Checked)
            {
                cbProfilResiko.Enabled = false;
            }
            else
            {
                cbProfilResiko.Enabled = true;
            }
        }

        private void subCancel()
        {
            _intType = 0;
            subClearAll();

            //20230106, sandi, RDN-899, begin
            if (_strTabName != "MCB")
            {
                //20230106, sandi, RDN-899, end
                subDisableAll(_intType);
                subResetToolBar();
                GetComponentSearch();
                //20160509, Elva, CSODD16117, begin
                cmpsrKodeKantor.Text1 = strBranch; //Reset Kode Kantor            
                //20160509, Elva, CSODD16117, begin
                //20230106, sandi, RDN-899, begin
            }
            else if (_strTabName == "MCB")
            {
                subResetToolBar();
                cbBlokir.SelectedIndex = 0;
                cbBlokir.Text = "";
                nmNAVYesterday.Value = 0;
                nispMoneyBlokir.Value = 0;
                nmNilaiMarket.Value = 0;
                tbProdCode.Text = "";
                tbProdName.Text = "";
                dtpNAVDate.Value = DateTime.Today;
                dtpInputDate.Value = DateTime.Today;
                dtpTglTran.Value = DateTime.Today;
                dtpExpiry.Value = DateTime.Today;
                dtpNAVDate.Enabled = false;
                dtpInputDate.Enabled = false;
                dtpTglTran.Enabled = false;
                dtpExpiry.Enabled = false;
                cbBlokir.Enabled = false;
                tbDeskripsiBlokir.Enabled = false;
                nmBlockId.Enabled = false;
                nispMoneyBlokir.Enabled = false;
                dgvBlokir.Enabled = false;
                dgvLogBlokir.Enabled = false;
            }
            //20230106, sandi, RDN-899, end
        }

        private void btnGenerateNoDokTanpaNPWP_Click(object sender, EventArgs e)
        {
            DataSet dsData;
            GlobalFunctionCIF.GetNoNPWPCounter(out dsData);
            tbNoDokTanpaNPWP.Text = dsData.Tables[0].Rows[0]["NoDocNPWP"].ToString();
            btnGenerateNoDokTanpaNPWP.Enabled = false;
        }

        private void cbKepemilikanNPWPKK_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbKepemilikanNPWPKK.Text == "Lainnya")
                tbKepemilikanLainnya.Enabled = true;
            else
                tbKepemilikanLainnya.Enabled = false;
        }

        private void btnGantiOpsiNPWP_Click(object sender, EventArgs e)
        {
            tbNoNPWPKK.Text = "";
            tbNamaNPWPKK.Text = "";
            cbKepemilikanNPWPKK.SelectedIndex = -1;
            tbKepemilikanLainnya.Text = "";
            //20150610, liliana, LIBST13020, begin
            //dtpTglNPWPKK.Value = DateTime.Parse("1900-01-01");
            dtpTglNPWPKK.Value = DateTime.Today;
            //20150610, liliana, LIBST13020, end
            cbAlasanTanpaNPWP.SelectedIndex = -1;
            tbNoDokTanpaNPWP.Text = "";
            //20150610, liliana, LIBST13020, begin
            //dtpTglDokTanpaNPWP.Value = DateTime.Parse("1900-01-01");
            dtpTglDokTanpaNPWP.Value = DateTime.Today;
            //20150610, liliana, LIBST13020, end

            switch (_intOpsiNPWP)
            {
                case 2:
                    _intOpsiNPWP = 3;
                    EnableFieldNPWP(3);
                    break;
                case 1:
                case 3:
                    _intOpsiNPWP = 2;
                    EnableFieldNPWP(2);
                    break;
            }
        }

        private void btnShareHolder_Click(object sender, EventArgs e)
        {
            GenerateSHDID();
        }

        private void GenerateSHDID()
        {
            string _shareholderID = "";
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcShareholderID", System.Data.OleDb.OleDbType.VarChar, 12);
            dbParam[0].Value = _shareholderID;
            dbParam[0].Direction = System.Data.ParameterDirection.InputOutput;

            bool blnResult = ClQ.ExecProc("ReksaGenerateShareholderID", ref dbParam);

            if (blnResult == true)
            {
                _shareholderID = dbParam[0].Value.ToString();
                textShareHolderId.Text = _shareholderID;
            }
            else
            {
                MessageBox.Show("Error Generate Shareholder ID");
            }
        }
        //20161004, liliana, CSODD16311, begin

        private void OnTimerTick(Object sender, EventArgs eventargs)
        {
            if (lblTaxAmnesty.ForeColor == Color.Black)
                lblTaxAmnesty.ForeColor = Color.Red;
            else
                lblTaxAmnesty.ForeColor = Color.Black;
        }
        //20161004, liliana, CSODD16311, end

        private void cmpsrCIF_onNispText2Changed(object sender, EventArgs e)
        {
            //try
            //{
            cmpsrSrcClient.Criteria = cmpsrCIF.Text1.Trim();
            //}
            //catch
            //{
            //    return;
            //}

            if (_intType == 1)
            {
                if (cmpsrCIF.Text1.Trim() != "")
                {
                    //20170828, liliana, COPOD17271, begin
                    if (this._clsCoreBankMessaging.CIFInquiryInqFlagPVBByCIFBranchUserType_13155(cmpsrCIF.Text1.Trim(), strBranch, intClassificationId.ToString(),
                           out ErrMsg, out dsOut))//dapet akses private banking
                    {
                        //20170828, liliana, COPOD17271, end
                        GetDataCIF(cmpsrCIF.Text1.Trim(), _intType);
                        //20160823, Elva, LOGEN00196, begin
                        if (!string.IsNullOrEmpty(cmpsrCIF.Text1))
                        {
                            long lnCIFNo;
                            long.TryParse(cmpsrCIF.Text1, out lnCIFNo);

                            string strIsAllowed = "", strErrorMessage = "";
                            if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIF.Text1, out strIsAllowed, out strErrorMessage))
                            {
                                //20161004, liliana, CSODD16311, begin
                                //if (strIsAllowed == "0")
                                if ((strIsAllowed == "0") || (strIsAllowed == "2"))
                                //20161004, liliana, CSODD16311, end
                                {
                                    comboRekIDRTA.Enabled = true;
                                    comboRekMultiCurTA.Enabled = true;
                                    comboRekUSDTA.Enabled = true;
                                    //20161004, liliana, CSODD16311, begin
                                    lblTaxAmnesty.Visible = true;
                                    //20161004, liliana, CSODD16311, end
                                }
                                else
                                {
                                    comboRekIDRTA.Enabled = false;
                                    comboRekMultiCurTA.Enabled = false;
                                    comboRekUSDTA.Enabled = false;
                                    //20161004, liliana, CSODD16311, begin
                                    lblTaxAmnesty.Visible = false;
                                    //20161004, liliana, CSODD16311, end
                                }
                            }
                            else
                            {
                                MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            GetAccountTaxAmnesty(comboRekIDRTA, lnCIFNo, 1, "IDR");
                            GetAccountTaxAmnesty(comboRekUSDTA, lnCIFNo, 1, "USD");
                            GetAccountTaxAmnesty(comboRekMultiCurTA, lnCIFNo, 1, "MC");
                        }
                        //20160823, Elva, LOGEN00196, end
                        //20170828, liliana, COPOD17271, begin
                    }
                    else
                    {
                        MessageBox.Show(ErrMsg.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    //20170828, liliana, COPOD17271, end
                }
            }
            else
            {
                if (cmpsrCIF.Text1 == "")
                {
                    subClearAll();
                }
                //20160823, Elva, LOGEN00196, begin
                else
                {
                    if (!string.IsNullOrEmpty(cmpsrCIF.Text1))
                    {
                        long lnCIFNo;
                        long.TryParse(cmpsrCIF.Text1, out lnCIFNo);

                        string strIsAllowed = "", strErrorMessage = "";
                        if (clsValidator.ValidasiCIFTaxAmnesty(ClQ, cmpsrCIF.Text1, out strIsAllowed, out strErrorMessage))
                        {
                            //20161004, liliana, CSODD16311, begin
                            //if (strIsAllowed == "0")
                            if ((strIsAllowed == "0") || (strIsAllowed == "2"))
                            //20161004, liliana, CSODD16311, end
                            {
                                comboRekIDRTA.Enabled = false;
                                comboRekMultiCurTA.Enabled = false;
                                comboRekUSDTA.Enabled = false;
                                //20161004, liliana, CSODD16311, begin
                                lblTaxAmnesty.Visible = true;
                                //20161004, liliana, CSODD16311, end
                            }
                            else
                            {
                                //20161004, liliana, CSODD16311, begin
                                //comboRekIDRTA.Enabled = true;
                                //comboRekMultiCurTA.Enabled = true;
                                //comboRekUSDTA.Enabled = true;
                                comboRekIDRTA.Enabled = false;
                                comboRekMultiCurTA.Enabled = false;
                                comboRekUSDTA.Enabled = false;
                                lblTaxAmnesty.Visible = false;
                                //20161004, liliana, CSODD16311, end
                            }
                        }
                        else
                        {
                            MessageBox.Show("Error [ReksaCheckingTaxAmnesty]!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        GetAccountTaxAmnesty(comboRekIDRTA, lnCIFNo, 1, "IDR");
                        GetAccountTaxAmnesty(comboRekUSDTA, lnCIFNo, 1, "USD");
                        GetAccountTaxAmnesty(comboRekMultiCurTA, lnCIFNo, 1, "MC");
                    }
                }
                //20160823, Elva, LOGEN00196, end
            }
        }

        private void cbDikirimKe_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDikirimKe.SelectedIndex == 0)
            {
                panelAlamatCabang.Visible = false;
                panelAlamatNasabah.Visible = true;
                //20210305, joshua, RDN-466, begin
                panelAlamatEmail.Visible = false;
                //20210305, joshua, RDN-466, end
            }
            else if (cbDikirimKe.SelectedIndex == 1)
            {
                panelAlamatCabang.Visible = true;
                panelAlamatNasabah.Visible = false;
                //20210305, joshua, RDN-466, begin
                panelAlamatEmail.Visible = false;
                //20210305, joshua, RDN-466, end
            }
            //20210305, joshua, RDN-466, begin
            else if (cbDikirimKe.SelectedIndex == 2)
            {
                panelAlamatCabang.Visible = false;
                panelAlamatNasabah.Visible = false;
                panelAlamatEmail.Visible = true;
            }
            //20210305, joshua, RDN-466, end
            else
            {
                panelAlamatCabang.Visible = false;
                panelAlamatNasabah.Visible = false;
                //20210305, joshua, RDN-466, begin
                panelAlamatEmail.Visible = false;
                //20210305, joshua, RDN-466, end
            }
        }

        private void tbRekening_Validated(object sender, EventArgs e)
        {
            //20150427, liliana, LIBST13020, begin
            //if (tbRekening.Text != "")
            //{
            //    GetAccountRelationDetail(tbRekening.Text, 1);

            //    if (tbNamaRekening.Text == "")
            //    {
            //        MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        return;
            //    }
            //}
            //20150427, liliana, LIBST13020, end
        }

        //20161101, liliana, CSODD16311, begin
        //private void GetAccountRelationDetail(string AccountNum, int Type)
        private bool GetAccountRelationDetail(string AccountNum, int Type)
        //20161101, liliana, CSODD16311, end
        {
            // Type = 1 --> get account detail
            // Type = 2 --> check if this account is employee
            //20150518, liliana, LIBST13020, begin
            // Type = 3 --> get account detail usd
            //20150518, liliana, LIBST13020, end
            //20150728, liliana, LIBST13020, begin
            // Type = 4 --> get account detail multicurr
            //20150728, liliana, LIBST13020, end
            //20160823, Elva, LOGEN00196, begin
            // Type = 5 --> get account detail idr tax amnesty 
            // Type = 6 --> get account detail usd tax amnesty 
            // Type = 7 --> get account detail multicurrecny tax amnesty 
            //20160823, Elva, LOGEN00196, end
            //20230314, Andhika J, RDN-903, begin
            #region RemarkExisting
            //DataSet _dsOut;
            //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[2];

            //dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcAccountNumber", System.Data.OleDb.OleDbType.VarChar, 20);
            //dbParam[0].Value = AccountNum;

            //dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnType", System.Data.OleDb.OleDbType.Integer);
            //dbParam[1].Value = Type;

            //bool blnResult = ClQ.ExecProc("ReksaGetAccountRelationDetail", ref dbParam, out _dsOut);

            //if (blnResult == true)
            //{
            //    if (Type == 1)
            //    {
            //        if (_dsOut.Tables[0].Rows.Count > 0)
            //        {
            //            //20150914, liliana, LIBST13020, begin
            //            maskedRekening.Text = _dsOut.Tables[0].Rows[0]["NoRek"].ToString();
            //            //20150914, liliana, LIBST13020, end
            //            tbNamaRekening.Text = _dsOut.Tables[0].Rows[0]["Nama"].ToString();
            //        }
            //        else
            //        {
            //            MessageBox.Show("No rekening tidak ditemukan!");
            //        }
            //    }
            //    //20150518, liliana, LIBST13020, begin
            //    else if (Type == 3)
            //    {
            //        if (_dsOut.Tables[0].Rows.Count > 0)
            //        {
            //            //20150914, liliana, LIBST13020, begin
            //            maskedRekeningUSD.Text = _dsOut.Tables[0].Rows[0]["NoRek"].ToString();
            //            //20150914, liliana, LIBST13020, end
            //            tbNamaRekeningUSD.Text = _dsOut.Tables[0].Rows[0]["Nama"].ToString();
            //        }
            //        else
            //        {
            //            MessageBox.Show("No rekening USD tidak ditemukan!");
            //        }
            //    }
            //    //20150518, liliana, LIBST13020, end
            //    //20150728, liliana, LIBST13020, begin
            //    else if (Type == 4)
            //    {
            //        if (_dsOut.Tables[0].Rows.Count > 0)
            //        {
            //            //20150914, liliana, LIBST13020, begin
            //            maskedRekeningMC.Text = _dsOut.Tables[0].Rows[0]["NoRek"].ToString();
            //            //20150914, liliana, LIBST13020, end
            //            tbNamaRekeningMC.Text = _dsOut.Tables[0].Rows[0]["Nama"].ToString();
            //        }
            //        else
            //        {
            //            MessageBox.Show("No rekening Multicurrency tidak ditemukan!");
            //        }
            //    }
            //    //20150728, liliana, LIBST13020, end
            //    else if (Type == 2)
            //    {
            //        cmpsrNIK.Text1 = _dsOut.Tables[0].Rows[0]["NIK"].ToString();
            //        cmpsrNIK.ValidateField();

            //        if (cmpsrNIK.Text1 == "")
            //        {
            //            //20150706, liliana, LIBST13020, begin
            //            //cbStatus.SelectedIndex = 0;
            //            cbStatus.SelectedIndex = 1;
            //            //20150706, liliana, LIBST13020, end
            //            cmpsrNIK.Text1 = "";
            //            cmpsrNIK.Text2 = "";
            //        }
            //        //20150623, liliana, LIBST13020, begin
            //        else
            //        {
            //            //20150706, liliana, LIBST13020, begin
            //            //cbStatus.SelectedIndex = 1;
            //            cbStatus.SelectedIndex = 0;
            //            //20150706, liliana, LIBST13020, end
            //        }
            //        //20150623, liliana, LIBST13020, end
            //    }
            //    //20160823, Elva, LOGEN00196, begin
            //    else if (Type == 5)
            //    {
            //        if (_dsOut.Tables[0].Rows.Count > 0)
            //        {
            //            comboRekIDRTA.Text = _dsOut.Tables[0].Rows[0]["NoRek"].ToString();
            //            txtNamaRekIDRTA.Text = _dsOut.Tables[0].Rows[0]["Nama"].ToString();
            //        }
            //        else
            //            MessageBox.Show("No rekening IDR Tax Amnesty tidak ditemukan!");
            //    }
            //    else if (Type == 6)
            //    {
            //        if (_dsOut.Tables[0].Rows.Count > 0)
            //        {
            //            comboRekUSDTA.Text = _dsOut.Tables[0].Rows[0]["NoRek"].ToString();
            //            txtNamaRekUSDTA.Text = _dsOut.Tables[0].Rows[0]["Nama"].ToString();
            //        }
            //        else
            //            MessageBox.Show("No rekening USD Tax Amnesty tidak ditemukan!");
            //    }
            //    else if (Type == 7)
            //    {
            //        if (_dsOut.Tables[0].Rows.Count > 0)
            //        {
            //            comboRekMultiCurTA.Text = _dsOut.Tables[0].Rows[0]["NoRek"].ToString();
            //            txtNamaRekMultiCurTA.Text = _dsOut.Tables[0].Rows[0]["Nama"].ToString();
            //        }
            //        else
            //        {
            //            MessageBox.Show("No rekening Multicurrency Tax Amnesty tidak ditemukan!");
            //        }
            //    }
            //    //20160823, Elva, LOGEN00196, end
            //}
            //else
            //{
            //    MessageBox.Show("Error mengambil detail rekening!");
            //}
            ////20161101, trilili, begin
            //return blnResult;
            //20161101, trilili, end
            #endregion
            #region HitAPI
            DataSet dsUrl = new DataSet();
            // 20241016, Lely R, RDN-1189, begin
            string date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            // 20241016, Lely R, RDN-1189, end
            string strUrlAPI = "";
            string _strGuid = "";
            bool blnResult = false;
            _strGuid = Guid.NewGuid().ToString();
            if (_cProc.GetAPIParam("TRX_ReksaGetAccountRelationDetail", out dsUrl))
            {
                strUrlAPI = dsUrl.Tables[0].Rows[0]["ParamVal"].ToString();
            }
            _ReksaGetAccountRelationDetailRq = new ReksaGetAccountRelationDetailRq();
            _ReksaGetAccountRelationDetailRq.MessageGUID = _strGuid;
            _ReksaGetAccountRelationDetailRq.ParentMessageGUID = null;
            _ReksaGetAccountRelationDetailRq.TransactionMessageGUID = _strGuid;
            _ReksaGetAccountRelationDetailRq.IsResponseMessage = "false";
            _ReksaGetAccountRelationDetailRq.UserNIK = intNIK.ToString();
            _ReksaGetAccountRelationDetailRq.ModuleName = strModule;
            // 20241016, Lely R, RDN-1189, begin
            _ReksaGetAccountRelationDetailRq.MessageDateTime = date.Replace('/', '-');
            // 20241016, Lely R, RDN-1189, end
            _ReksaGetAccountRelationDetailRq.DestinationURL = strUrlAPI;
            _ReksaGetAccountRelationDetailRq.IsSuccess = true;
            _ReksaGetAccountRelationDetailRq.ErrorCode = "";
            _ReksaGetAccountRelationDetailRq.ErrorDescription = "";
            //req data 
            _ReksaGetAccountRelationDetailRq.Data.pcAccountNumber = AccountNum;
            _ReksaGetAccountRelationDetailRq.Data.pnType = Type;
            //end

            ReksaGetAccountRelationDetailRs _response = _iServiceAPI.ReksaGetAccountRelationDetail(_ReksaGetAccountRelationDetailRq);

            if (_response.IsSuccess == true)
            {
                blnResult = true;
                if (Type == 1)
                {
                    if (_response.Data.pcAccountNumber == "" || _response.Data.pcAccountNumber == null)
                    {
                        MessageBox.Show("No rekening tidak ditemukan!");
                    }
                    else
                    {
                        maskedRekening.Text = _response.Data.pcAccountNumber;
                        tbNamaRekening.Text = _response.Data.cCIFName1;
                    }
                }
                else if (Type == 3)
                {
                    if (_response.Data.pcAccountNumber == "" || _response.Data.pcAccountNumber == null)
                    {
                        MessageBox.Show("No rekening USD tidak ditemukan!");
                    }
                    else
                    {
                        maskedRekeningUSD.Text = _response.Data.pcAccountNumber;
                        tbNamaRekeningUSD.Text = _response.Data.cCIFName1;
                    }
                }
                else if (Type == 4)
                {
                    if (_response.Data.pcAccountNumber == "" || _response.Data.pcAccountNumber == null)
                    {
                        MessageBox.Show("No rekening Multicurrency tidak ditemukan!");
                    }
                    else
                    {
                        maskedRekeningMC.Text = _response.Data.pcAccountNumber;
                        tbNamaRekeningMC.Text = _response.Data.cCIFName1;
                    }
                }
                else if (Type == 2)
                {
                    cmpsrNIK.Text1 = _response.Data.nNIK.ToString();
                    cmpsrNIK.ValidateField();

                    if (cmpsrNIK.Text1 == "")
                    {
                        //20150706, liliana, LIBST13020, begin
                        //cbStatus.SelectedIndex = 0;
                        cbStatus.SelectedIndex = 1;
                        //20150706, liliana, LIBST13020, end
                        cmpsrNIK.Text1 = "";
                        cmpsrNIK.Text2 = "";
                    }
                    //20150623, liliana, LIBST13020, begin
                    else
                    {
                        //20150706, liliana, LIBST13020, begin
                        //cbStatus.SelectedIndex = 1;
                        cbStatus.SelectedIndex = 0;
                        //20150706, liliana, LIBST13020, end
                    }
                    //20150623, liliana, LIBST13020, end
                }
                //20160823, Elva, LOGEN00196, begin
                else if (Type == 5)
                {
                    if (_response.Data.pcAccountNumber == "" || _response.Data.pcAccountNumber == null)
                    {
                        MessageBox.Show("No rekening IDR Tax Amnesty tidak ditemukan!");
                    }
                    else
                    {
                        comboRekIDRTA.Text = _response.Data.pcAccountNumber;
                        txtNamaRekIDRTA.Text = _response.Data.cCIFName1;
                    }
                }
                else if (Type == 6)
                {
                    if (_response.Data.pcAccountNumber == "" || _response.Data.pcAccountNumber == null)
                    {
                        MessageBox.Show("No rekening USD Tax Amnesty tidak ditemukan!");
                    }
                    else
                    {
                        comboRekUSDTA.Text = _response.Data.pcAccountNumber;
                        txtNamaRekUSDTA.Text = _response.Data.cCIFName1;
                    }
                }
                else if (Type == 7)
                {
                    if (_response.Data.pcAccountNumber == "" || _response.Data.pcAccountNumber == null)
                    {
                        MessageBox.Show("No rekening Multicurrency Tax Amnesty tidak ditemukan!");
                    }
                    else
                    {
                        comboRekMultiCurTA.Text = _response.Data.pcAccountNumber;
                        txtNamaRekMultiCurTA.Text = _response.Data.cCIFName1;
                    }
                }
                //20160823, Elva, LOGEN00196, end
            }
            //20161101, trilili, begin
            // 20241010, Lely R, RDN-1189, begin
            else
            {
                blnResult = false;

                if (!string.IsNullOrEmpty(_response.ErrorDescription))
                {
                    MessageBox.Show(_response.ErrorDescription);
                    maskedRekening.Clear();
                }
                else
                {
                    MessageBox.Show("Error mengambil detail rekening!");
                }
            }
            // 20241010, Lely R, RDN-1189, end
            return blnResult;
            #endregion
            //20230314, Andhika J, RDN-903, end
        }

        private void cbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            //20150623, liliana, LIBST13020, begin
            //if (cbStatus.SelectedIndex == 1)
            //{
            //20150623, liliana, LIBST13020, end
            //20150427, liliana, LIBST13020, begin
            //GetAccountRelationDetail(tbRekening.Text, 2);
            //20150619, liliana, LIBST13020, begin
            //GetAccountRelationDetail(maskedRekening.Text, 2);
            //20150708, liliana, LIBST13020, begin
            //20150723, liliana, LIBST13020, begin
            //if (cbStatus.SelectedIndex == 1)
            if (cbStatus.SelectedIndex == 0)
            //20150723, liliana, LIBST13020, end
            {
                //20231121, ahmad.fansyuri, RDN-1094, begin
                ////20150708, liliana, LIBST13020, end
                //GetAccountRelationDetail(cmpsrCIF.Text1, 2);
                //20150708, liliana, LIBST13020, begin
                //20231121, ahmad.fansyuri, RDN-1094, end

            }
            //20150723, liliana, LIBST13020, begin
            else
            {
                cmpsrNIK.Text1 = "";
                cmpsrNIK.Text2 = "";
            }
            //20150723, liliana, LIBST13020, end
            //20150708, liliana, LIBST13020, end
            //20150619, liliana, LIBST13020, end
            //20150427, liliana, LIBST13020, end
            //20150623, liliana, LIBST13020, begin
            //}
            //20150623, liliana, LIBST13020, end
            //20150323, liliana, LIBST13020, begin
            //20150623, liliana, LIBST13020, begin
            //else
            //{
            //    cmpsrNIK.Text1 = "";
            //    cmpsrNIK.Text2 = "";
            //}
            //20150623, liliana, LIBST13020, end
            //20150323, liliana, LIBST13020, end
        }

        private void cmpsrCabangSurat_onNispText2Changed(object sender, EventArgs e)
        {
            if (cmpsrCabangSurat.Text1 != "")
            {
                ReksaGetAlamatCabang(cmpsrCabangSurat.Text1);
            }
        }

        private void cmpsrCabangSurat_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCabangSurat.Text2 = "";
            tbAlamatSaatIni1.Text = "";
            tbAlamatSaatIni2.Text = "";
            tbKodePos.Text = "";
            tbKotaNasabahAlmt.Text = "";
            tbProvNasabahAlmt.Text = "";
            //20180802, Lita, LOGEN00649, begin
            txtLastUpdated.Text = "";
            //20180802, Lita, LOGEN00649, end
        }

        private void ReksaGetAlamatCabang(string KodeCabang)
        {
            DataSet dsOut;
            //20180802, Lita, LOGEN00649, begin
            //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];
            //20180802, Lita, LOGEN00649, end

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcKodeCabang", System.Data.OleDb.OleDbType.VarChar, 5);
            dbParam[0].Value = KodeCabang;

            //20180802, Lita, LOGEN00649, begin
            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcCIFNo", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[1].Value = cmpsrCIF.Text1;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnId", System.Data.OleDb.OleDbType.Integer);
            dbParam[2].Value = intId;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;
            //20180802, Lita, LOGEN00649, end

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcKodeCabang", System.Data.OleDb.OleDbType.VarChar, 5);
            dbParam[0].Value = KodeCabang;

            bool blnResult = ClQ.ExecProc("ReksaGetAlamatCabang", ref dbParam, out dsOut);

            if (blnResult == true)
            {
                tbAlamatSaatIni1.Text = dsOut.Tables[0].Rows[0]["AddressLine1"].ToString();
                tbAlamatSaatIni2.Text = dsOut.Tables[0].Rows[0]["AddressLine2"].ToString();
                tbKodePos.Text = dsOut.Tables[0].Rows[0]["PostalCode"].ToString();
                tbKotaNasabahAlmt.Text = dsOut.Tables[0].Rows[0]["Kota"].ToString();
                tbProvNasabahAlmt.Text = dsOut.Tables[0].Rows[0]["Province"].ToString();
                //20180802, Lita, LOGEN00649, begin
                txtLastUpdated.Text = dsOut.Tables[0].Rows[0]["LastUpdated"].ToString();
                //20180802, Lita, LOGEN00649, end

            }
            else
            {
                MessageBox.Show("Error mengambil alamat cabang");
            }
        }

        private DataSet ReksaGetMandatoryFieldStatus(string CIFNo)
        {
            Int64 intCIFNo;
            Int64.TryParse(CIFNo, out intCIFNo);

            DataSet dsOut;
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[2];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnCIFNo", System.Data.OleDb.OleDbType.BigInt);
            dbParam[0].Value = intCIFNo;

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcErrorMessage", System.Data.OleDb.OleDbType.VarChar, 100);
            dbParam[0].Direction = ParameterDirection.InputOutput;

            bool blnResult = ClQ.ExecProc("ReksaGetMandatoryFieldStatus", ref dbParam, out dsOut);

            return dsOut;
        }

        private void GetDataRDB(string ClientCode)
        {
            //20230105, sandi, RDN-899, begin
            isRDB = 0;
            isMature = 0;
            //20230105, sandi, RDN-899, end
            DataSet dsRDB;
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[1];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcClientCode", System.Data.OleDb.OleDbType.VarChar, 20);
            dbParam[0].Value = ClientCode;

            bool blnResult = ClQ.ExecProc("ReksaGetListClientRDB", ref dbParam, out dsRDB);

            if (blnResult == true)
            {
                if (dsRDB.Tables[0].Rows.Count > 0)
                {
                    txtRDBJangkaWaktu.Text = dsRDB.Tables[0].Rows[0]["JangkaWaktu"].ToString();
                    dtRDBJatuhTempo.Value = (DateTime)dsRDB.Tables[0].Rows[0]["JatuhTempo"];
                    //20150615, liliana, LIBST13020, begin
                    //20150617, liliana, LIBST13020, begin
                    //dtRDBJatuhTempo.CustomFormat = null;
                    //dtRDBJatuhTempo.Format = DateTimePickerFormat.Long;
                    dtRDBJatuhTempo.CustomFormat = "dd MMMM yyyy";
                    dtRDBJatuhTempo.Format = DateTimePickerFormat.Custom;
                    //20150617, liliana, LIBST13020, end
                    //20150615, liliana, LIBST13020, end

                    if (dsRDB.Tables[0].Rows[0]["AutoRedemption"].ToString() == "1")
                    {
                        chkAutoRedemp.Checked = true;
                    }
                    else
                    {
                        chkAutoRedemp.Checked = false;
                    }

                    if (dsRDB.Tables[0].Rows[0]["Asuransi"].ToString() == "1")
                    {
                        chkRDBAsuransi.Checked = true;
                    }
                    else
                    {
                        chkRDBAsuransi.Checked = false;
                    }
                    //20150420, liliana, LIBST13020, begin
                    textFrekPendebetan.Text = dsRDB.Tables[0].Rows[0]["FrekPendebetan"].ToString();
                    //20150420, liliana, LIBST13020, end

                    //20200620, Lita, RDN-88, begin
                    lblFreqDebetUnit.Text = dsRDB.Tables[0].Rows[0]["FreqDebetUnit"].ToString();
                    dtRDBStartDebetDate.Value = (DateTime)dsRDB.Tables[0].Rows[0]["StartDebetDate"];
                    dtRDBStartDebetDate.CustomFormat = "dd MMMM yyyy";
                    dtRDBStartDebetDate.Format = DateTimePickerFormat.Custom;
                    //20200620, Lita, RDN-88, end
                    //20230105, sandi, RDN-899, begin
                    isRDB = 1;
                    isMature = Int32.Parse(dsRDB.Tables[0].Rows[0]["IsMature"].ToString());
                    //20230105, sandi, RDN-899, end
                }
                else
                {
                    txtRDBJangkaWaktu.Text = "";
                    dtRDBJatuhTempo.Value = DateTime.Today;
                    //20150615, liliana, LIBST13020, begin
                    dtRDBJatuhTempo.CustomFormat = " ";
                    dtRDBJatuhTempo.Format = DateTimePickerFormat.Custom;
                    //20150615, liliana, LIBST13020, end
                    chkAutoRedemp.Checked = false;
                    chkRDBAsuransi.Checked = false;
                    //20150420, liliana, LIBST13020, begin
                    textFrekPendebetan.Text = "";
                    //20150420, liliana, LIBST13020, end
                    //20200620, Lita, RDN-88, begin
                    lblFreqDebetUnit.Text = "";
                    dtRDBStartDebetDate.Value = DateTime.Today;
                    dtRDBStartDebetDate.CustomFormat = " ";
                    dtRDBStartDebetDate.Format = DateTimePickerFormat.Custom;
                    //20200620, Lita, RDN-88, end
                    //20230105, sandi, RDN-899, begin
                    isRDB = 0;
                    isMature = 1;
                    //20230105, sandi, RDN-899, end
                }

            }
            else
            {
                MessageBox.Show("Error mengambil detail RDB");
            }
        }

        private void dgvClientCode_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0)
                {
                    string strClientCode = dgvClientCode.Rows[e.RowIndex].Cells["ClientCode"].Value.ToString().Trim();
                    string strClientId = dgvClientCode.Rows[e.RowIndex].Cells["ClientId"].Value.ToString().Trim();
                    int.TryParse(strClientId, out intSelectedClient);
                    GetDataRDB(strClientCode);
                    dgvClientCode.Rows[e.RowIndex].Cells[0].Value = true;

                    //20230105, sandi, RDN-899, begin
                    if ((strClientCode != "") && (cmpsrCIF.Text2 != ""))
                    {
                        if ((isRDB == 1) && (isMature == 0))
                        {
                            subClearBlokir();
                        }

                        if ((isRDB == 0) || ((isRDB == 1) && (isMature == 1)))
                        {
                            cmpsrSrcClient.Text1 = strClientCode;
                            cmpsrSrcClient.ValidateField();
                        }
                    }
                    //20230105, sandi, RDN-899, end

                    for (int i = 0; i < dgvClientCode.RowCount; i++)
                    {
                        if (i != e.RowIndex)
                        {
                            dgvClientCode.Rows[i].Cells[0].Value = false;
                        }
                    }
                }
            }
            catch (Exception er)
            {
                return;
            }
        }

        private void tabControlClient_Selected(object sender, TabControlEventArgs e)
        {
            _strTabName = tabControlClient.SelectedTab.Name.ToString();

            if ((_intType != 1) || (_intType != 2))
            {

                _dvAkses.RowFilter = "InterfaceTypeId = '" + _strTabName + "'";
                subResetToolBar();
            }

            if ((_strTabName == "MCA") || (_strTabName == "MCB"))
            {
                cmpsrCIF.Enabled = false;
            }
            else
            {
                cmpsrCIF.Enabled = true;
            }
        }

        private void tabControlClient_Deselected(object sender, TabControlEventArgs e)
        {
            try
            {
                _strLastTabName = tabControlClient.SelectedTab.Name.ToString();

            }
            catch
            {
                _strLastTabName = "";
            }
        }

        private void tabControlClient_Selecting(object sender, TabControlCancelEventArgs e)
        {
            _strTabName = tabControlClient.SelectedTab.Name.ToString();

            if (((_intType == 1) || (_intType == 2))
                || (cmpsrCIF.Text1.Trim() == ""))
            {
                if (((_strLastTabName == "MCI") || (_strLastTabName == "MCN"))
                    && ((_strTabName == "MCA") || (_strTabName == "MCB")))
                {
                    e.Cancel = true;
                }

                if ((_strLastTabName == "MCB")
                    && ((_strTabName == "MCI") || (_strTabName == "MCN") || (_strTabName == "MCA")))
                {
                    e.Cancel = true;
                }

                if ((_strLastTabName == "MCA")
                 && ((_strTabName == "MCI") || (_strTabName == "MCN") || (_strTabName == "MCB")))
                {
                    e.Cancel = true;
                }
            }

            _strTabName = _strLastTabName;
        }

        private void cmpsrCIF_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrCIF.Text2 = "";
        }

        private void btnPopulate_Click(object sender, EventArgs e)
        {
            if (_strTabName == "MCA")
            {
                ReksaPopulateAktifitas();
            }
        }

        private void ReksaPopulateAktifitas()
        {
            System.Data.DataSet dsRefresh;
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[6];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnClientId", System.Data.OleDb.OleDbType.Integer);
            dbParam[0].Value = intSelectedClient;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pdStartDate", System.Data.OleDb.OleDbType.DBDate);
            dbParam[1].Value = (DateTime)dtpStartDate.Value;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pdEndDate", System.Data.OleDb.OleDbType.DBDate);
            dbParam[2].Value = (DateTime)dtpEndDate.Value;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            dbParam[3] = new System.Data.OleDb.OleDbParameter("@pbIsBalance", System.Data.OleDb.OleDbType.Boolean);
            dbParam[3].Value = false;
            dbParam[3].Direction = System.Data.ParameterDirection.Input;

            dbParam[4] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
            dbParam[4].Value = intNIK;
            dbParam[4].Direction = System.Data.ParameterDirection.Input;

            dbParam[5] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
            dbParam[5].Value = strGuid;
            dbParam[5].Direction = System.Data.ParameterDirection.Input;

            //20151006, liliana, LIBST13020, begin
            ClQ.TimeOut = 6000;
            //20151006, liliana, LIBST13020, end
            bool blnResult = ClQ.ExecProc("ReksaPopulateAktivitas", ref dbParam, out dsRefresh);

            if (blnResult == true)
            {
                dgvAktifitas.DataSource = dsRefresh.Tables[0];
                //20150904, liliana, LIBST13020, begin
                dgvAktifitas.AutoResizeColumns();
                for (int i = 0; i < dgvAktifitas.Columns.Count; i++)
                {
                    if (dgvAktifitas.Columns[i].ValueType.ToString() == "System.Decimal")
                    {
                        dgvAktifitas.Columns[i].DefaultCellStyle.Format = "N2";
                    }
                }

                //20170512, liliana, BOSOD17090, begin
                try
                {
                    //20220225, gio, RDN-740, begin
                    //dgvAktifitas.Columns["NAV"].DefaultCellStyle.Format = "N4";
                    dgvAktifitas.Columns["NAV"].DefaultCellStyle.Format = "N6";
                    //20220225, gio, RDN-740, end
                    dgvAktifitas.Columns["Unit Transaction"].DefaultCellStyle.Format = "N4";
                    dgvAktifitas.Columns["Unit Balance"].DefaultCellStyle.Format = "N4";
                }
                catch
                {
                    return;
                }
                //20170512, liliana, BOSOD17090, end
                //20150904, liliana, LIBST13020, end
            }

            dsRefresh = null;
            dbParam = null;
        }

        private void dgvBlokir_Click(object sender, EventArgs e)
        {
            //if (dgvBlokir.SelectedRows.Count > 0)
            //{
            //    dtpTglTran.Value = (DateTime)dgvBlokir.CurrentRow.Cells["Tanggal Blokir"].Value;
            //    nispMoneyBlokir.Value = (decimal)dgvBlokir.CurrentRow.Cells["Unit Blokir"].Value;
            //    nispOutsUnit.Value = (decimal)dgvBlokir.CurrentRow.Cells["Outstanding Unit"].Value;
            //    dtpExpiry.Value = (DateTime)dgvBlokir.CurrentRow.Cells["Tanggal Expiry Blokir"].Value;
            //}
            //else
            //{
            //    dtpExpiry.Value = _dtCurrentDate;
            //    dtpTglTran.Value = _dtCurrentDate;
            //    nispOutsUnit.Value = 0;
            //    nispMoneyBlokir.Value = 0;

            //}
        }

        private void dgvBlokir_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            this.dgvBlokir_Click(this, e);
        }

        private void cmpsrSrcClient_onNispText1Changed(object sender, EventArgs e)
        {
            cmpsrSrcClient.Text2 = "";
            //subClearBlokir();
        }

        private void cmpsrSrcClient_onNispText2Changed(object sender, EventArgs e)
        {
            //20150617, liliana, LIBST13020, begin
            //20230105, sandi, RDN-899, begin
            //ReksaRefreshBlokir();
            ReksaRefreshBlokir();
            //20230105, sandi, RDN-899, end
            //20150617, liliana, LIBST13020, end
        }

        private void ReksaRefreshBlokir()
        {
            //20150615, liliana, LIBST13020, begin
            //if (cmpsrSrcClient.Text1 != "")
            //20230105, sandi, RDN-899, begin
            //cmpsrSrcClient.ValidateField();
            nispMoneyBlokir.Value = 0;
            nispMoneyTotal.Value = 0;
            nispOutsUnit.Value = 0;
            dgvBlokir.DataSource = null;
            dtpExpiry.Value = DateTime.Today;
            dtpTglTran.Value = DateTime.Today;
            cbBlokir.SelectedIndex = 0;
            cbBlokir.Text = "";
            tbDeskripsiBlokir.Text = "";
            nmNAVYesterday.Value = 0;
            nmBlockId.Value = 0;
            nmEffectiveUnit.Value = 0;
            tbProdCode.Text = "";
            tbProdName.Text = "";
            dtpInputDate.Value = DateTime.Today;
            nmNilaiMarket.Value = 0;
            dgvLogBlokir.DataSource = null;
            dtpNAVDate.Value = DateTime.Today;
            //20230105, sandi, RDN-899, end

            if (cmpsrSrcClient.Text2 != "")
            //20150615, liliana, LIBST13020, end
            {
                System.Data.DataSet dsRefresh;

                System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnClientId", System.Data.OleDb.OleDbType.Integer);
                dbParam[0].Value = (int)cmpsrSrcClient[2];
                dbParam[0].Direction = System.Data.ParameterDirection.Input;

                dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnNIK", System.Data.OleDb.OleDbType.Integer);
                dbParam[1].Value = intNIK;
                dbParam[1].Direction = System.Data.ParameterDirection.Input;

                dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcGuid", System.Data.OleDb.OleDbType.VarChar, 50);
                dbParam[2].Value = strGuid;
                dbParam[2].Direction = System.Data.ParameterDirection.Input;


                bool blnResult = ClQ.ExecProc("ReksaRefreshBlokir", ref dbParam, out dsRefresh);
                if (blnResult == true)
                {
                    dgvBlokir.DataSource = dsRefresh.Tables[0];
                    dgvBlokir.AutoResizeColumns();
                    //20150904, liliana, LIBST13020, begin
                    for (int i = 0; i < dgvBlokir.Columns.Count; i++)
                    {
                        if (dgvBlokir.Columns[i].ValueType.ToString() == "System.Decimal")
                        {
                            //20230106, sandi, RDN-899, begin
                            dgvBlokir.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                            if (dgvBlokir.Columns[i].HeaderText.ToString() == "Unit Blokir")
                            {
                                dgvBlokir.Columns[i].DefaultCellStyle.Format = "N4";
                            }
                            else
                                if (dgvBlokir.Columns[i].HeaderText.ToString() == "NAV")
                                {
                                    dgvBlokir.Columns[i].DefaultCellStyle.Format = "N6";
                                }
                                else
                                {
                                    //20230106, sandi, RDN-899, end
                                    dgvBlokir.Columns[i].DefaultCellStyle.Format = "N2";
                                    //20230106, sandi, RDN-899, begin
                                }
                            //20230106, sandi, RDN-899, end
                        }
                    }
                    //20230105, sandi, RDN-899, begin
                    dgvBlokir.Columns["Tipe Blokir"].Visible = false;
                    //20230105, sandi, RDN-899, end
                    //20150904, liliana, LIBST13020, end
                    nispMoneyTotal.Value = (decimal)dsRefresh.Tables[1].Rows[0]["Total"];
                    nispOutsUnit.Value = (decimal)dsRefresh.Tables[2].Rows[0]["Outstanding Unit"];
                    //20230105, sandi, RDN-899, begin
                    nmEffectiveUnit.Value = (decimal)dsRefresh.Tables[3].Rows[0]["Effective Unit"];
                    nmNAVYesterday.Value = (decimal)dsRefresh.Tables[4].Rows[0]["NAV"];
                    dtpNAVDate.Value = (DateTime)dsRefresh.Tables[4].Rows[0]["NAV Date"];
                    tbProdCode.Text = dsRefresh.Tables[5].Rows[0]["ProdCode"].ToString();
                    tbProdName.Text = dsRefresh.Tables[5].Rows[0]["ProdName"].ToString();

                    if (Convert.ToInt32(dsRefresh.Tables[6].Rows[0]["IsBlock"]) == 0)
                    {
                        cbBlokir.SelectedIndex = 0;
                        cbBlokir.Text = "";
                    }
                    else if (Convert.ToInt32(dsRefresh.Tables[6].Rows[0]["IsBlock"]) == 1)
                    {
                        cbBlokir.SelectedIndex = 0;
                        cbBlokir.Text = "Blokir";
                    }
                    else if (Convert.ToInt32(dsRefresh.Tables[6].Rows[0]["IsBlock"]) == 2)
                    {
                        cbBlokir.SelectedIndex = 1;
                        cbBlokir.Text = "Lepas Blokir";
                    }

                    dgvBlokir.Enabled = true;
                    dgvLogBlokir.Enabled = true;
                    //20230105, sandi, RDN-899, end
                }
                //20230105, sandi, RDN-899, begin
                else
                {
                    subClearBlokir();
                }
                cmpsrCIF.Enabled = false;
                //20230105, sandi, RDN-899, end

                dsRefresh = null;
                dbParam = null;
                subResetToolBar();
            }
        }

        //20150323, liliana, LIBST13020, begin
        private void tbRekening_TextChanged(object sender, EventArgs e)
        {
            //20150427, liliana, LIBST13020, begin
            //tbNamaRekening.Text = "";
            //cbStatus.SelectedIndex = -1;
            //cmpsrNIK.Text1 = "";
            //cmpsrNIK.Text2 = "";
            //20150427, liliana, LIBST13020, end
        }

        //20150427, liliana, LIBST13020, begin
        private void maskedRekening_Validated(object sender, EventArgs e)
        {
            //20150626, liliana, LIBST13020, begin
            //if (maskedRekening.Text != "")
            if (maskedRekening.Text.Replace("-", "").Trim() != "")
            //20150626, liliana, LIBST13020, end
            {
                //20150626, liliana, LIBST13020, begin
                //GetAccountRelationDetail(maskedRekening.Text, 1);
                //20161101, liliana, CSODD16311, begin
                //GetAccountRelationDetail(maskedRekening.Text.Replace("-", "").Trim(), 1);
                if (!GetAccountRelationDetail(maskedRekening.Text.Replace("-", "").Trim(), 1))
                {
                    return;
                }
                //20161101, liliana, CSODD16311, end
                //20150626, liliana, LIBST13020, end

                //20150727, liliana, LIBST13020, begin
                //if (maskedRekening.Text == "")
                if (maskedRekening.Text.Replace("-", "").Trim() == "")
                //20150727, liliana, LIBST13020, end
                {
                    MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
        }

        private void maskedRekening_TextChanged(object sender, EventArgs e)
        {
            tbNamaRekening.Text = "";
            //20150623, liliana, LIBST13020, begin
            //cbStatus.SelectedIndex = -1;
            //cmpsrNIK.Text1 = "";
            //cmpsrNIK.Text2 = "";
            //20150623, liliana, LIBST13020, end
        }

        //20150518, liliana, LIBST13020, begin
        private void maskedRekeningUSD_TextChanged(object sender, EventArgs e)
        {
            tbNamaRekeningUSD.Text = "";
        }

        private void maskedRekeningUSD_Validated(object sender, EventArgs e)
        {
            //20150626, liliana, LIBST13020, begin
            //if (maskedRekeningUSD.Text != "")
            if (maskedRekeningUSD.Text.Replace("-", "").Trim() != "")
            //20150626, liliana, LIBST13020, end
            {
                //20150626, liliana, LIBST13020, begin
                //GetAccountRelationDetail(maskedRekeningUSD.Text, 3);
                //20161101, liliana, CSODD16311, begin
                //GetAccountRelationDetail(maskedRekeningUSD.Text.Replace("-", "").Trim(), 3);
                if (!GetAccountRelationDetail(maskedRekeningUSD.Text.Replace("-", "").Trim(), 3))
                {
                    return;
                }
                //20161101, liliana, CSODD16311, end
                //20150626, liliana, LIBST13020, end

                //20150727, liliana, LIBST13020, begin
                //if (maskedRekeningUSD.Text == "")
                if (maskedRekeningUSD.Text.Replace("-", "").Trim() == "")
                //20150727, liliana, LIBST13020, end
                {
                    MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
        }

        //20150610, liliana, LIBST13020, begin
        private void dgvKonfAddr_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                //20150706, liliana, LIBST13020, begin
                if (_intType != 0)
                {
                    //20150706, liliana, LIBST13020, end
                    if (e.RowIndex >= 0)
                    {
                        if (_intType != 0)
                        {
                            dgvKonfAddr.Rows[e.RowIndex].Cells[0].Value = true;
                        }

                        for (int i = 0; i < dgvKonfAddr.RowCount; i++)
                        {
                            if (i != e.RowIndex)
                            {
                                dgvKonfAddr.Rows[i].Cells[0].Value = false;
                            }
                        }
                    }
                    //20150706, liliana, LIBST13020, begin
                }
                //20150706, liliana, LIBST13020, end
            }
            catch (Exception er)
            {
                return;
            }
        }

        //20150727, liliana, LIBST13020, begin
        private void maskedRekeningMC_Validated(object sender, EventArgs e)
        {
            if (maskedRekeningMC.Text.Replace("-", "").Trim() != "")
            {
                //20161101, liliana, CSODD16311, begin
                //GetAccountRelationDetail(maskedRekeningMC.Text.Replace("-", "").Trim(), 4);
                if (!GetAccountRelationDetail(maskedRekeningMC.Text.Replace("-", "").Trim(), 4))
                {
                    return;
                }
                //20161101, liliana, CSODD16311, end

                if (maskedRekeningMC.Text.Replace("-", "").Trim() == "")
                {
                    MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

            }
        }

        private void maskedRekeningMC_TextChanged(object sender, EventArgs e)
        {
            tbNamaRekeningMC.Text = "";
        }
        //20150727, liliana, LIBST13020, end
        //20150610, liliana, LIBST13020, end
        //20150518, liliana, LIBST13020, end
        //20150427, liliana, LIBST13020, end
        //20150323, liliana, LIBST13020, end
        //20160509, Elva, CSODD16117, begin
        private void SetEnableOfficeId(string strKodeKantor)
        {
            string strIsEnable = "", strErrorMessage = "";
            if (clsValidator.ValidasiCBOKodeKantor(ClQ, strKodeKantor, out strIsEnable, out strErrorMessage))
            {
                cmpsrKodeKantor.Text1 = strKodeKantor;

                if (strIsEnable == "1")
                {
                    cmpsrKodeKantor.Enabled = true;
                    cmpsrKodeKantor.ReadOnly = false;
                }
                else
                {
                    cmpsrKodeKantor.Enabled = false;
                    cmpsrKodeKantor.ReadOnly = true;
                }
            }
        }

        private void cmpsrKodeKantor_onNispText2Changed(object sender, EventArgs e)
        {
            string strIsAllowed = "";
            if (clsValidator.ValidasiInputKodeKantor(ClQ, cmpsrKodeKantor.Text1, out strIsAllowed))
            {
                if (strIsAllowed == "0")
                {
                    MessageBox.Show("Error [ReksaValidateOfficeId], Kode kantor tidak terdaftar ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //tabControlClient.Enabled = false;
                    cmpsrCIF.Enabled = false;
                    subClearAll();
                }
                else
                {
                    tabControlClient.Enabled = true;
                    cmpsrCIF.Enabled = true;
                    subResetToolBar();
                }
            }
            else
                MessageBox.Show("Error [ReksaValidateOfficeId]! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        //20160509, Elva, CSODD16117, end
        //20160823, Elva, LOGEN00196, begin
        private void GetAccountTaxAmnesty(ComboBox cb, long lnCIFNo, int intType, string strCurrency)
        {
            DataSet dsOut = new DataSet();
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnCIFNo", System.Data.OleDb.OleDbType.BigInt);
            dbParam[0].Value = lnCIFNo;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnType", System.Data.OleDb.OleDbType.Integer);
            dbParam[1].Value = intType;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcCCY", System.Data.OleDb.OleDbType.VarChar, 10);
            dbParam[2].Value = strCurrency;

            DataSet dsCombo = new DataSet();

            bool blnResult = ClQ.ExecProc("ReksaGetAccountTA", ref dbParam, out dsCombo);

            if (blnResult)
            {
                if (dsCombo.Tables[0].Rows.Count > 1)
                {
                    cb.DataSource = dsCombo.Tables[0];
                    cb.ValueMember = "AccountRelation";
                    cb.DisplayMember = "AccountRelation";
                }
                else
                    cb.DataSource = null;
            }
            else
            {
                MessageBox.Show("Gagal mengambil account tax amnesty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void comboRekIDRTA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboRekIDRTA.SelectedIndex > 0)
            {
                if (!string.IsNullOrEmpty(comboRekIDRTA.Text))
                {
                    GetAccountRelationDetail(comboRekIDRTA.Text, 5);

                    if (string.IsNullOrEmpty(comboRekIDRTA.Text))
                    {
                        MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            if (comboRekIDRTA.Text == "")
                txtNamaRekIDRTA.Text = "";
        }

        private void comboRekUSDTA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboRekUSDTA.SelectedIndex > 0)
            {
                if (!string.IsNullOrEmpty(comboRekUSDTA.Text))
                {
                    GetAccountRelationDetail(comboRekUSDTA.Text, 6);

                    if (string.IsNullOrEmpty(comboRekUSDTA.Text))
                    {
                        MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            if (comboRekUSDTA.Text == "")
                txtNamaRekUSDTA.Text = "";
        }

        private void comboRekMultiCurTA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboRekMultiCurTA.SelectedIndex > 0)
            {
                if (!string.IsNullOrEmpty(comboRekMultiCurTA.Text))
                {
                    GetAccountRelationDetail(comboRekMultiCurTA.Text, 7);

                    if (string.IsNullOrEmpty(comboRekMultiCurTA.Text))
                    {
                        MessageBox.Show("Nomor rekening salah!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            if (comboRekMultiCurTA.Text == "")
                txtNamaRekMultiCurTA.Text = "";
        }
        //20160823, Elva, LOGEN00196, end
        //20180727, Andhika J, BOSIT18231, begin
        private void dtpRiskProfile_ValueChanged(object sender, EventArgs e)
        {
            DateTime dtExprRiskProfile = new DateTime();
            Int64 pnCIFNo;
            if (cmpsrCIF.Text1 == "")
            {
                pnCIFNo = 0;
            }
            else
            {
                pnCIFNo = Convert.ToInt64(cmpsrCIF.Text1);
            }
            DataSet dsExp = new DataSet();
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@dtRiskProfile", System.Data.OleDb.OleDbType.Date);
            dbParam[0].Value = dtpRiskProfile.Value;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@dtExpRiskProfile", System.Data.OleDb.OleDbType.Date);
            dbParam[1].Value = dtExprRiskProfile;
            dbParam[1].Direction = System.Data.ParameterDirection.Output;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnCIFNo", System.Data.OleDb.OleDbType.BigInt);
            dbParam[2].Value = pnCIFNo;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            ClQ.TimeOut = 6000;
            bool blnResult = ClQ.ExecProc("ReksaCekExpRiskProfile", ref dbParam, out dsExp);

            if (blnResult == true)
            {

                dtExprRiskProfile = (DateTime)(dbParam[1].Value);
                dtExpiredRiskProfile.Text = dtExprRiskProfile.ToString();
                if (dsExp.Tables[0].Rows.Count > 0)
                {
                    txtEmail.Text = dsExp.Tables[0].Rows[0]["Email"].ToString();
                }
                else
                {
                    txtEmail.Text = "";
                }
            }
        }
        private void ValidateRiskProfile()
        {
            DateTime dtExprRiskProfile = new DateTime();
            Int64 pnCIFNo;
            if (cmpsrCIF.Text1 == "")
            {
                pnCIFNo = 0;
            }
            else
            {
                pnCIFNo = Convert.ToInt64(cmpsrCIF.Text1);
            }
            DataSet dsExp = new DataSet();
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@dtRiskProfile", System.Data.OleDb.OleDbType.Date);
            dbParam[0].Value = dtpRiskProfile.Value;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@dtExpRiskProfile", System.Data.OleDb.OleDbType.Date);
            dbParam[1].Value = dtExprRiskProfile;
            dbParam[1].Direction = System.Data.ParameterDirection.Output;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnCIFNo", System.Data.OleDb.OleDbType.BigInt);
            dbParam[2].Value = pnCIFNo;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            ClQ.TimeOut = 6000;
            bool blnResult = ClQ.ExecProc("ReksaCekExpRiskProfile", ref dbParam, out dsExp);

            if (blnResult == true)
            {

                dtExprRiskProfile = (DateTime)(dbParam[1].Value);
                dtExpiredRiskProfile.Text = dtExprRiskProfile.ToString();
                if (dsExp.Tables[0].Rows.Count > 0)
                {
                    txtEmail.Text = dsExp.Tables[0].Rows[0]["Email"].ToString();
                }
                else
                {
                    txtEmail.Text = "";
                }
            }
        }
        private void GetRiskProfileParam()
        {
            DataSet dsParamRiskProfile = new DataSet();
            bool blnResult = ClQ.ExecProc("ReksaCekExpRiskProfileParam", out dsParamRiskProfile);
            if (blnResult == true)
            {
                intMaxDay = Convert.ToInt32(dsParamRiskProfile.Tables[0].Rows[0]["ExpRiskProfileDay"]);
                intMaxYear = Convert.ToInt32(dsParamRiskProfile.Tables[0].Rows[0]["ExpRiskProfileYear"]);
            }

        }
        private void SubSaveRiskProfile()
        {
            DataSet dsSaveRiskProfile = new DataSet();
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@dtRiskProfile", System.Data.OleDb.OleDbType.Date);
            dbParam[0].Value = dtpRiskProfile.Value;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@dtExpRiskProfile", System.Data.OleDb.OleDbType.Date);
            dbParam[1].Value = dtExpiredRiskProfile.Value;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnCIFNo", System.Data.OleDb.OleDbType.BigInt);
            dbParam[2].Value = cmpsrCIF.Text1;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            bool blnResult = ClQ.ExecProc("ReksaSaveExpRiskProfile", ref dbParam, out dsSaveRiskProfile);
        }
        //20180727, Andhika J, BOSIT18231, end

        //20230105, sandi, RDN-899, begin
        private bool ValidasiBlokirUnit(int intGroupF, string strUserType)
        {
            int isAllowed;

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[4];

            try
            {
                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnGroupId", System.Data.OleDb.OleDbType.Integer);
                dbParam[0].Value = intGroupF;
                dbParam[0].Direction = System.Data.ParameterDirection.Input;

                dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcUserType", System.Data.OleDb.OleDbType.VarChar, 10);
                dbParam[1].Value = strUserType;
                dbParam[1].Direction = System.Data.ParameterDirection.Input;

                dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnIsAllowed", System.Data.OleDb.OleDbType.Integer);
                dbParam[2].Value = 0;
                dbParam[2].Direction = System.Data.ParameterDirection.InputOutput;

                dbParam[3] = new System.Data.OleDb.OleDbParameter("@pnBlockOnly", System.Data.OleDb.OleDbType.Integer);
                dbParam[3].Value = 0;
                dbParam[3].Direction = System.Data.ParameterDirection.InputOutput;

                if (ClQ.ExecProc("dbo.ReksaValidateBlockAccess", ref dbParam))
                {
                    int.TryParse(dbParam[2].Value.ToString(), out isAllowed);
                    int.TryParse(dbParam[3].Value.ToString(), out isBlockOnly);

                    if (isAllowed == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
                return false;
            }
        }

        private bool ValidasiFlagClientCode(int intGroupF, string strUserType)
        {
            int isAllowed;

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[3];

            try
            {
                dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnGroupId", System.Data.OleDb.OleDbType.Integer);
                dbParam[0].Value = intGroupF;
                dbParam[0].Direction = System.Data.ParameterDirection.Input;

                dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcUserType", System.Data.OleDb.OleDbType.VarChar, 10);
                dbParam[1].Value = strUserType;
                dbParam[1].Direction = System.Data.ParameterDirection.Input;

                dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnIsAllowed", System.Data.OleDb.OleDbType.Integer);
                dbParam[2].Value = 0;
                dbParam[2].Direction = System.Data.ParameterDirection.InputOutput;

                if (ClQ.ExecProc("dbo.ReksaValidateFlagAccess", ref dbParam))
                {
                    int.TryParse(dbParam[2].Value.ToString(), out isAllowed);

                    if (isAllowed == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
                return false;
            }
        }

        private void dgvBlokir_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                nmBlockId.Value = Int32.Parse(dgvBlokir["BlockId", e.RowIndex].Value.ToString());
                nispMoneyBlokir.Value = decimal.Parse(dgvBlokir["Unit Blokir", e.RowIndex].Value.ToString());
                dtpTglTran.Value = (DateTime)dgvBlokir["Tanggal Blokir", e.RowIndex].Value;
                dtpExpiry.Value = (DateTime)dgvBlokir["Tanggal Expired Blokir", e.RowIndex].Value;
                cbBlokir.Text = dgvBlokir["Tipe Blokir", e.RowIndex].Value.ToString();
                tbDeskripsiBlokir.Text = dgvBlokir["Deskripsi Blokir", e.RowIndex].Value.ToString();
                nmNAVYesterday.Value = decimal.Parse(dgvBlokir["NAV", e.RowIndex].Value.ToString());
                nmNilaiMarket.Value = decimal.Parse(dgvBlokir["Nilai Market", e.RowIndex].Value.ToString());
                dtpNAVDate.Value = (DateTime)dgvBlokir["NAV Date", e.RowIndex].Value;

                //Populate Log Aktivitas
                System.Data.DataSet dsLogBlokir;
                System.Data.OleDb.OleDbParameter[] dbParamLog = new System.Data.OleDb.OleDbParameter[1];

                try
                {
                    dbParamLog[0] = new System.Data.OleDb.OleDbParameter("@pnBlockId", System.Data.OleDb.OleDbType.Integer);
                    dbParamLog[0].Value = nmBlockId.Value;
                    dbParamLog[0].Direction = System.Data.ParameterDirection.Input;

                    bool blnResult = ClQ.ExecProc("ReksaPopulateLogBlokir", ref dbParamLog, out dsLogBlokir);

                    if (blnResult == true)
                    {
                        dgvLogBlokir.DataSource = dsLogBlokir.Tables[0];
                        dgvLogBlokir.AutoResizeColumns();

                        for (int i = 0; i < dgvLogBlokir.Columns.Count; i++)
                        {
                            if (dgvLogBlokir.Columns[i].ValueType.ToString() == "System.Decimal")
                            {
                                dgvLogBlokir.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                                if (dgvLogBlokir.Columns[i].HeaderText.ToString() == "Unit Blokir")
                                {
                                    dgvLogBlokir.Columns[i].DefaultCellStyle.Format = "N4";
                                }
                                else
                                    if (dgvLogBlokir.Columns[i].HeaderText.ToString() == "NAV")
                                    {
                                        dgvLogBlokir.Columns[i].DefaultCellStyle.Format = "N6";
                                    }
                                    else
                                    {
                                        dgvLogBlokir.Columns[i].DefaultCellStyle.Format = "N2";
                                    }
                            }
                        }
                    }

                    dsLogBlokir = null;
                    dbParamLog = null;

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
                    return;
                }
            }
        }

        private void nispMoneyBlokir_onNispMoneyTextChanged(object sender, EventArgs e)
        {
            nmNilaiMarket.Value = nispMoneyBlokir.Value * nmNAVYesterday.Value;
        }
        //20230105, sandi, RDN-899, end
        //20230223, Andhika J, RDN-903, begin
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
        //20230223, Andhika J, RDN-903, end

    }
}