using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace BankNISP.FrontEnd
{
    class clsTransaksi
    {
        internal NispLogin.ClsUser clUserInside;
        internal NispQuery.ClsQuery ClQ;

        public string strModule;
        public int intNIK;
        public string strGuid;
        public string strMenuName;
        public string strBranch;
        public int intClassificationId;

        private DataSet _dsTrx;

        public DataTable dttSubscription;
        public DataTable dttRedemption;
        public DataTable dttSubsRDB;
        public DataTable dttSwitching;
        public DataTable dttSwitchingRDB;
        public DataTable dttBooking;

        public string OfficeId;
        public string RefID;
        public string CIFNo;
        public string Inputter;
        public string Seller;
        public string Waperd;
        public string Referentor;
        public string Status;
        public string CIFName;
//20210922, korvi, RDN-674, begin
        public string SelectedAccNo;
        public string TranCCY;
//20210922, korvi, RDN-674, end

        public clsTransaksi(string JenisTrx, int intNIK, string strGuid, NispQuery.ClsQuery cQ)
        {
            this.intNIK = intNIK;
            this.strGuid = strGuid;
            this.ClQ = cQ;

            if (JenisTrx == "SUBS")
            {
                dttSubscription = new DataTable();

                //20150617, liliana, LIBST13020, begin
                //dttSubscription.Columns.Add("NoTrx");
                //dttSubscription.Columns.Add("TglTrx");
                //dttSubscription.Columns.Add("KodeProduk");
                //dttSubscription.Columns.Add("ClientCode");
                //dttSubscription.Columns.Add("CCY");
                //dttSubscription.Columns.Add("Nominal", System.Type.GetType("System.Decimal"));
                //dttSubscription.Columns.Add("PhoneOrder", System.Type.GetType("System.Boolean"));
                //dttSubscription.Columns.Add("FullAmount", System.Type.GetType("System.Boolean"));
                //dttSubscription.Columns.Add("EditFee", System.Type.GetType("System.Boolean"));
                //dttSubscription.Columns.Add("JenisFee");
                //dttSubscription.Columns.Add("NominalFee", System.Type.GetType("System.Decimal"));
                //dttSubscription.Columns.Add("PctFee", System.Type.GetType("System.Decimal"));
                //dttSubscription.Columns.Add("FeeCurr");
                //dttSubscription.Columns.Add("FeeKet");
                ////20150430, liliana, LIBST13020, begin
                //dttSubscription.Columns.Add("EditFeeBy");
                ////20150430, liliana, LIBST13020, end
                //dttSubscription.Columns.Add("IsNew", System.Type.GetType("System.Boolean"));
                //dttSubscription.Columns.Add("OutstandingUnit", System.Type.GetType("System.Decimal"));
                ////20150521, liliana, LIBST13020, begin
                //dttSubscription.Columns.Add("ApaDiUpdate", System.Type.GetType("System.Boolean"));
                //dttSubscription.Columns.Add("StatusTransaksi");
                ////20150521, liliana, LIBST13020, end
                dttSubscription.Columns.Add("NoTrx");
                dttSubscription.Columns.Add("StatusTransaksi");
                dttSubscription.Columns.Add("KodeProduk");
                dttSubscription.Columns.Add("NamaProduk");
                dttSubscription.Columns.Add("ClientCode");
                dttSubscription.Columns.Add("Nominal", System.Type.GetType("System.Decimal"));
                dttSubscription.Columns.Add("EditFeeBy");
                dttSubscription.Columns.Add("NominalFee", System.Type.GetType("System.Decimal"));
                dttSubscription.Columns.Add("FullAmount", System.Type.GetType("System.Boolean"));
                dttSubscription.Columns.Add("PhoneOrder", System.Type.GetType("System.Boolean"));

                dttSubscription.Columns.Add("TglTrx");
                dttSubscription.Columns.Add("CCY");
                dttSubscription.Columns.Add("EditFee", System.Type.GetType("System.Boolean"));
                dttSubscription.Columns.Add("JenisFee");
                dttSubscription.Columns.Add("PctFee", System.Type.GetType("System.Decimal"));
                dttSubscription.Columns.Add("FeeCurr");
                dttSubscription.Columns.Add("FeeKet");
                dttSubscription.Columns.Add("IsNew", System.Type.GetType("System.Boolean"));
                dttSubscription.Columns.Add("OutstandingUnit", System.Type.GetType("System.Decimal"));
                dttSubscription.Columns.Add("ApaDiUpdate", System.Type.GetType("System.Boolean"));
                //20150617, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                dttSubscription.Columns.Add("TrxTaxAmnesty", System.Type.GetType("System.Boolean"));
                //20160829, liliana, LOGEN00196, end
               

                dttSubscription.Columns["TglTrx"].DataType = System.Type.GetType("System.DateTime");
                dttSubscription.Columns["TglTrx"].DateTimeMode = System.Data.DataSetDateTime.Unspecified;

            }
            else if (JenisTrx == "REDEMP")
            {
                dttRedemption = new DataTable();

                //20150617, liliana, LIBST13020, begin
                //dttRedemption.Columns.Add("NoTrx");
                //dttRedemption.Columns.Add("TglTrx");
                //dttRedemption.Columns.Add("KodeProduk");
                //dttRedemption.Columns.Add("ClientCode");
                //dttRedemption.Columns.Add("OutstandingUnit", System.Type.GetType("System.Decimal"));
                //dttRedemption.Columns.Add("RedempUnit", System.Type.GetType("System.Decimal"));
                //dttRedemption.Columns.Add("PhoneOrder", System.Type.GetType("System.Boolean"));
                //dttRedemption.Columns.Add("EditFee", System.Type.GetType("System.Boolean"));
                //dttRedemption.Columns.Add("JenisFee");
                //dttRedemption.Columns.Add("NominalFee", System.Type.GetType("System.Decimal"));
                //dttRedemption.Columns.Add("PctFee", System.Type.GetType("System.Decimal"));
                //dttRedemption.Columns.Add("FeeCurr");
                //dttRedemption.Columns.Add("FeeKet");
                //dttRedemption.Columns.Add("IsRedempAll", System.Type.GetType("System.Boolean"));
                //dttRedemption.Columns.Add("Period");
                ////20150521, liliana, LIBST13020, begin
                //dttRedemption.Columns.Add("ApaDiUpdate", System.Type.GetType("System.Boolean"));
                //dttRedemption.Columns.Add("StatusTransaksi");
                ////20150521, liliana, LIBST13020, end
                dttRedemption.Columns.Add("NoTrx");
                dttRedemption.Columns.Add("StatusTransaksi");
                dttRedemption.Columns.Add("KodeProduk");
                dttRedemption.Columns.Add("NamaProduk");
                dttRedemption.Columns.Add("ClientCode");
                dttRedemption.Columns.Add("OutstandingUnit", System.Type.GetType("System.Decimal"));
                dttRedemption.Columns.Add("RedempUnit", System.Type.GetType("System.Decimal"));
                dttRedemption.Columns.Add("IsRedempAll", System.Type.GetType("System.Boolean"));
                dttRedemption.Columns.Add("EditFeeBy");
                dttRedemption.Columns.Add("NominalFee", System.Type.GetType("System.Decimal"));
                dttRedemption.Columns.Add("PhoneOrder", System.Type.GetType("System.Boolean"));
                
                dttRedemption.Columns.Add("TglTrx");
                dttRedemption.Columns.Add("EditFee", System.Type.GetType("System.Boolean"));
                dttRedemption.Columns.Add("JenisFee");
                dttRedemption.Columns.Add("PctFee", System.Type.GetType("System.Decimal"));
                dttRedemption.Columns.Add("FeeCurr");
                dttRedemption.Columns.Add("FeeKet");
                dttRedemption.Columns.Add("Period");
                dttRedemption.Columns.Add("ApaDiUpdate", System.Type.GetType("System.Boolean"));
                //20150617, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                dttRedemption.Columns.Add("TrxTaxAmnesty", System.Type.GetType("System.Boolean"));
                //20160829, liliana, LOGEN00196, end

                dttRedemption.Columns["TglTrx"].DataType = System.Type.GetType("System.DateTime");
                dttRedemption.Columns["TglTrx"].DateTimeMode = System.Data.DataSetDateTime.Unspecified;
            }
            else if (JenisTrx == "SUBSRDB")
            {
                dttSubsRDB = new DataTable();

                //20150617, liliana, LIBST13020, begin
                //dttSubsRDB.Columns.Add("NoTrx");
                //dttSubsRDB.Columns.Add("TglTrx");
                //dttSubsRDB.Columns.Add("KodeProduk");
                //dttSubsRDB.Columns.Add("ClientCode");
                //dttSubsRDB.Columns.Add("CCY");
                //dttSubsRDB.Columns.Add("Nominal", System.Type.GetType("System.Decimal"));

                //dttSubsRDB.Columns.Add("JangkaWaktu");
                //dttSubsRDB.Columns.Add("JatuhTempo");
                //dttSubsRDB.Columns.Add("FrekPendebetan");
                //dttSubsRDB.Columns.Add("AutoRedemption");
                //dttSubsRDB.Columns.Add("Asuransi");

                //dttSubsRDB.Columns.Add("PhoneOrder", System.Type.GetType("System.Boolean"));
                //dttSubsRDB.Columns.Add("EditFee", System.Type.GetType("System.Boolean"));
                //dttSubsRDB.Columns.Add("JenisFee");
                //dttSubsRDB.Columns.Add("NominalFee", System.Type.GetType("System.Decimal"));
                //dttSubsRDB.Columns.Add("PctFee", System.Type.GetType("System.Decimal"));
                //dttSubsRDB.Columns.Add("FeeCurr");
                //dttSubsRDB.Columns.Add("FeeKet");
                ////20150521, liliana, LIBST13020, begin
                //dttSubsRDB.Columns.Add("ApaDiUpdate", System.Type.GetType("System.Boolean"));
                //dttSubsRDB.Columns.Add("StatusTransaksi");
                ////20150521, liliana, LIBST13020, end
                dttSubsRDB.Columns.Add("NoTrx");
                dttSubsRDB.Columns.Add("StatusTransaksi");
                dttSubsRDB.Columns.Add("KodeProduk");
                dttSubsRDB.Columns.Add("NamaProduk");
                dttSubsRDB.Columns.Add("ClientCode");
                dttSubsRDB.Columns.Add("Nominal", System.Type.GetType("System.Decimal"));
                dttSubsRDB.Columns.Add("EditFeeBy");
                dttSubsRDB.Columns.Add("NominalFee", System.Type.GetType("System.Decimal"));
                dttSubsRDB.Columns.Add("JangkaWaktu");
                dttSubsRDB.Columns.Add("JatuhTempo");
                dttSubsRDB.Columns.Add("FrekPendebetan");

                //20200408, Lita, RDN-88, begin
                dttSubsRDB.Columns.Add("FrekDebetMethod");
                dttSubsRDB.Columns.Add("FrekDebetMethodValue");
                dttSubsRDB.Columns.Add("TanggalDebet");

                dttSubsRDB.Columns["TanggalDebet"].DataType = System.Type.GetType("System.DateTime");
                dttSubsRDB.Columns["TanggalDebet"].DateTimeMode = System.Data.DataSetDateTime.Unspecified;
                //20200408, Lita, RDN-88, end

                dttSubsRDB.Columns.Add("AutoRedemption");
                dttSubsRDB.Columns.Add("Asuransi");
                dttSubsRDB.Columns.Add("PhoneOrder", System.Type.GetType("System.Boolean"));

                dttSubsRDB.Columns.Add("TglTrx");
                dttSubsRDB.Columns.Add("CCY");
                dttSubsRDB.Columns.Add("EditFee", System.Type.GetType("System.Boolean"));
                dttSubsRDB.Columns.Add("JenisFee");
                dttSubsRDB.Columns.Add("PctFee", System.Type.GetType("System.Decimal"));
                dttSubsRDB.Columns.Add("FeeCurr");
                dttSubsRDB.Columns.Add("FeeKet");
                dttSubsRDB.Columns.Add("ApaDiUpdate", System.Type.GetType("System.Boolean"));

                //20150617, liliana, LIBST13020, end
                //20160829, liliana, LOGEN00196, begin
                dttSubsRDB.Columns.Add("TrxTaxAmnesty", System.Type.GetType("System.Boolean"));
                //20160829, liliana, LOGEN00196, end

                dttSubsRDB.Columns["TglTrx"].DataType = System.Type.GetType("System.DateTime");
                dttSubsRDB.Columns["TglTrx"].DateTimeMode = System.Data.DataSetDateTime.Unspecified;

                dttSubsRDB.Columns["JatuhTempo"].DataType = System.Type.GetType("System.DateTime");
                dttSubsRDB.Columns["JatuhTempo"].DateTimeMode = System.Data.DataSetDateTime.Unspecified;
            }
        }

        public void ClearData()
        {
            if (dttSubscription != null)
            {
                dttSubscription.Clear();
            }

            if (dttRedemption != null)
            {
                dttRedemption.Clear();
            }

            if (dttSubsRDB != null)
            {
                dttSubsRDB.Clear();
            }

            OfficeId = "";
            RefID = "";
            CIFNo = "";
            Inputter = "";
            Seller = "";
            Waperd= "";
            Referentor= "";
            Status= "";
            CIFName = "";
        }

        public bool GetDataTransaksi(string RefID, int intNIK, string strGuid, string JenisTrx)
        {
            bool blnResult = false;

            DataSet ds = new DataSet();
            OleDbParameter[] odp = new OleDbParameter[4];

            (odp[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar, 20)).Value = RefID;
            (odp[1] = new OleDbParameter("@pnNIK", OleDbType.Integer)).Value = intNIK;
            (odp[2] = new OleDbParameter("@pcGuid", OleDbType.VarChar, 50)).Value = strGuid;
            (odp[3] = new OleDbParameter("@pcTranType", OleDbType.VarChar, 50)).Value = JenisTrx;

            blnResult = ClQ.ExecProc("dbo.ReksaRefreshTransactionNew", ref odp, out ds);

            if (blnResult)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    OfficeId = ds.Tables[0].Rows[0]["OfficeId"].ToString();
                    RefID = ds.Tables[0].Rows[0]["RefID"].ToString();
                    CIFNo = ds.Tables[0].Rows[0]["CIFNo"].ToString();
                    Inputter = ds.Tables[0].Rows[0]["Inputter"].ToString();
                    Seller = ds.Tables[0].Rows[0]["Seller"].ToString();
                    Waperd = ds.Tables[0].Rows[0]["Waperd"].ToString();
                    Referentor = ds.Tables[0].Rows[0]["Referentor"].ToString();
                    Status = ds.Tables[0].Rows[0]["Status"].ToString();
                    CIFName = ds.Tables[0].Rows[0]["CIFName"].ToString();
//20210922, korvi, RDN-674, begin
                    SelectedAccNo = ds.Tables[0].Rows[0]["CIFName"].ToString();
                    TranCCY = ds.Tables[0].Rows[0]["TranCCY"].ToString();
                    SelectedAccNo = ds.Tables[0].Rows[0]["SelectedAccNo"].ToString();
//20210922, korvi, RDN-674, end

                    dttSubscription = ds.Tables[1].Copy();
                    dttRedemption = ds.Tables[2].Copy();
                    dttSubsRDB = ds.Tables[3].Copy();
                }
                else
                {
                    blnResult = false;
                }
            }

            return blnResult;
        }

    }
}
