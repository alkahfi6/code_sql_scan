using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace BankNISP.Obligasi01
{
    public class clsLelangDatabase
    {
        private ObligasiQuery _cQuery;

        public clsLelangDatabase(ObligasiQuery cQuery)
        {
            this._cQuery = cQuery;
        }

        #region "Parameter Lelang"
        public bool ParamLelangPopulate(out DataSet dsOut)
        {
            dsOut = new DataSet();
            return this._cQuery.ExecProc("dbo.TRSParamLelangPopulate", out dsOut);
        }

        public bool ParamLelangValidate(string strXml, string strCurrent, string strProcessType, bool isUpload, out DataSet dsOut)
        {
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcXmlData", strXml);
            dbPar[1] = new OleDbParameter("@pcXmlCurrent", strCurrent);
            dbPar[2] = new OleDbParameter("@pcProcessType", strProcessType);
            dbPar[3] = new OleDbParameter("@pbIsUpload", isUpload);


            return this._cQuery.ExecProc("dbo.TRSParamLelangValidate", ref dbPar, out dsOut);
        }

        public bool ParamLelangSave(string strXml, int userNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", strXml);
            dbPar[1] = new OleDbParameter("@pnNIK", userNIK);

            return this._cQuery.ExecProc("dbo.TRSParamLelangSave", ref dbPar);
        }

        public bool ParamLelangPopulateApproval(string strType, int userNIK, long apprId, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcType", strType);
            dbPar[1] = new OleDbParameter("@pnNIK", userNIK);
            dbPar[2] = new OleDbParameter("@pnApprId", apprId);

            return this._cQuery.ExecProc("dbo.TRSParamLelangPopulateApproval", ref dbPar, out dsOut);

        }

        public bool ParamLelangProcessApproval(long apprId, int userNIK, string apprStatus, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnApprId", apprId);
            dbPar[1] = new OleDbParameter("@pnProcessNIK", userNIK);
            dbPar[2] = new OleDbParameter("@pcApprStatus", apprStatus);

            return this._cQuery.ExecProc("dbo.TRSParamLelangProcessApproval", ref dbPar, out dsOut);
        }


        public bool ParamLelangGetSoftDefault(string strSecurityNo, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcSecurityNo", strSecurityNo);

            return this._cQuery.ExecProc("dbo.TRSParamLelangGetSoftDefault", ref dbPar, out dsOut);
        }
        #endregion

        #region Email
        public bool SendEmail(string strMenu, string strGuid, string strMailSubject, string strMailSender, string strMailTo, string strMailBody, int userNIK, out string strErrMsg)
        {
            string xmlResult = "";
            int nErrorNo = 0;
            strErrMsg = "";

            bool isOK = false;

            AlertService.AlertService service = new BankNISP.Obligasi01.AlertService.AlertService();
            //
            strMailBody = strMailBody.Replace("<br>", System.Environment.NewLine);
            strMailBody = strMailBody.Replace("<", "&lt;");
            strMailBody = strMailBody.Replace(">", "&gt;");
            //
            try
            {
                isOK = service.SendEmailWithoutAttachment(ref strGuid, userNIK.ToString(), strMailSubject, strMailBody, strMailSender, strMailTo, strMailTo, out xmlResult, out nErrorNo, out strErrMsg);
            }
            catch (Exception ex)
            {
                strErrMsg = ex.Message;
            }
            
            string strXmlData = "<Log>"
                    + "<Mail>"
                    + "<UserNIK>" + userNIK.ToString() + "</UserNIK>"
                    + "<GuidTran>" + strGuid + "</GuidTran>"
                    + "<MenuName>" + strMenu + "</MenuName>"
                    + "<MailSubject>" + strMailSubject + "</MailSubject>"
                    + "<MailSender>" + strMailSender + "</MailSender>"
                    + "<MailTo>" + strMailTo + "</MailTo>"
                    + "<MailBody>" + strMailBody + "</MailBody>"
                    + "<SendStatus>" + (isOK ? "S" : "F") + "</SendStatus>"
                    + "<ErrMsg>" + strErrMsg + "</ErrMsg>"
                    + "</Mail>"
                    + "</Log>";

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcXmlData", strXmlData);

            this._cQuery.ExecProc("dbo.TRSMailServiceInsertLog", ref dbPar);

            return isOK;
        }

        #endregion

        #region General
        public bool LelangGetGeneralParam(string strParamType, string strParamFilter, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcParamType", strParamType);
            dbPar[1] = new OleDbParameter("@pcParamFilter", strParamFilter);

            return this._cQuery.ExecProc("dbo.TRSLelangGetParam", ref dbPar, out dsOut);
        }

        public bool LelangGetGeneralParam(string strParamType, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcParamType", strParamType);

            return this._cQuery.ExecProc("dbo.TRSLelangGetParam", ref dbPar, out dsOut);
        }

        #endregion

        #region Blotter Lelang
        //20200506, uzia, BONDRETAIL-337, begin
        public bool ReplaceLelangPush(long idLelang, int userNIK, string userBranch)
        {
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pnIdLelang", idLelang);
            dbPar[1] = new OleDbParameter("@pnNIK", userNIK);
            dbPar[2] = new OleDbParameter("@pcBranch", userBranch);
            dbPar[3] = new OleDbParameter("@puGuid", System.Guid.NewGuid().ToString());

            return this._cQuery.ExecProc("dbo.TRSLelangPushReplace", ref dbPar);

        }
        //20200506, uzia, BONDRETAIL-337, end
        //20210331, rezakahfi, BONDRETAIL-732, begin
        //public bool BlotterLelangPopulate(string strDateFrom, string strDateTo, string strStatusCode, string strSecurityNo, out DataSet dsOut)
        public bool BlotterLelangPopulate(string strDateFrom, string strDateTo, string strStatusCode
                                                , string strSecurityNo
                                                , string Cabang
                                                , string Inputer
                                                , string Seller
                                                , string strDateTrxFrom, string strDateTrxTo
                                                , out DataSet dsOut
            )
        //20210331, rezakahfi, BONDRETAIL-732, begin
        {
            dsOut = new DataSet();

            //20210331, rezakahfi, BONDRETAIL-732, begin
            //OleDbParameter[] dbPar = new OleDbParameter[4];
            OleDbParameter[] dbPar = new OleDbParameter[9];
            //20210331, rezakahfi, BONDRETAIL-732, end
            dbPar[0] = new OleDbParameter("@pcDateFrom", strDateFrom);
            dbPar[1] = new OleDbParameter("@pcDateTo", strDateTo);
            dbPar[2] = new OleDbParameter("@pcStatusCode", strStatusCode);
            dbPar[3] = new OleDbParameter("@pcSecurityNo", strSecurityNo);
            //20210331, rezakahfi, BONDRETAIL-732, begin
            if (Inputer.Trim() == "")
                Inputer = "0";
            if (Seller.Trim() == "")
                Seller = "0";

            //string strTypeTgl = "Trx";
            //if (!bTrxFilter)
            //    strTypeTgl = "Input";

            dbPar[4] = new OleDbParameter("@pcCabang", Cabang);
            dbPar[5] = new OleDbParameter("@pnInputer", Inputer);
            dbPar[6] = new OleDbParameter("@pnSeller", Seller);
            dbPar[7] = new OleDbParameter("@pcDateTrxFrom", strDateTrxFrom);
            dbPar[8] = new OleDbParameter("@pcDateTrxTo", strDateTrxTo);
            //20210331, rezakahfi, BONDRETAIL-732, end

            return this._cQuery.ExecProc("dbo.TRSBlotterLelangPopulate", ref dbPar, out dsOut);
        }



        #endregion

        #region Transaksi Lelang

        public bool ParamLelangPopulate(string strSecNo,out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcSecurityNo", strSecNo);

            return this._cQuery.ExecProc("dbo.TRSParamLelangPopulate",ref dbParams, out dsOut);
        }

        public string getIdLelang()
        {
            DataSet dsOut = new DataSet();

            if (this.PopulateDataNasabah("NumberOfData", "", "", "IdLelang", out dsOut))
            {
                if (dsOut.Tables.Count > 0)
                {
                    if (dsOut.Tables[0].Rows.Count > 0)
                    {
                        return dsOut.Tables[0].Rows[0]["ID"].ToString();
                    }
                    else
                    {
                        MessageBox.Show("Failed Get IdLelang");
                        return "0";
                    }
                }
                else
                {
                    MessageBox.Show("Failed Get IdLelang");
                    return "0";
                }
            }
            else
                return "0";
        }

        public bool PopulateDataNasabah(string strParamType, string strCIF, string strCIF2, string strFilter, out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[5];
                dbParams[0] = new OleDbParameter("@pcTypePopulate", strParamType);
                dbParams[1] = new OleDbParameter("@pcProduct", "BONDS");
                dbParams[2] = new OleDbParameter("@pcCIF", strCIF);
                dbParams[3] = new OleDbParameter("@pcCIF2", strCIF2);
                dbParams[4] = new OleDbParameter("@pcFilter", strFilter);

                bResult = this._cQuery.ExecProc("dbo.TRSPopulateDataNasabahProduct", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return bResult;
            }

            return bResult;
        }

        //20210805, rezakahfi, BONDRETAIL-789, begin
        //public bool PopulateDataTrxLelang(string strStatus, string strParamType, int Nik,string Branch, out DataSet dsOut)
        public bool PopulateDataTrxLelang(string strStatus, string strParamType
                                            , int Nik
                                            , string Branch
                                            , bool bVisible
                                            , out DataSet dsOut
                                        )
        //20210805, rezakahfi, BONDRETAIL-789, end
        {
            dsOut = new DataSet();
            bool bResult = false;

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[5];
                dbParams[0] = new OleDbParameter("@pcStatus", strStatus);
                dbParams[1] = new OleDbParameter("@pcType", strParamType);
                dbParams[2] = new OleDbParameter("@nNIK", Nik);
                dbParams[3] = new OleDbParameter("@cBranch", Branch);
                //20210805, rezakahfi, BONDRETAIL-789, begin
                dbParams[4] = new OleDbParameter("@bVisibleData", bVisible);
                //20210805, rezakahfi, BONDRETAIL-789, end
                
                bResult = this._cQuery.ExecProc("dbo.SecurityPopulateTrxLelang", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return bResult;
            }

            return bResult;
        }

        public bool RelaseBlock(clsCallWebService clsCallWebService, string strAccountNo, string strAccountType, int nSequence, int nNik)
        {
            try
            {
                System.Data.DataSet dsResponse = new System.Data.DataSet();
                string strError = "";
                if (nSequence > 0)
                {
                    clsCallWebService.CallOBL_WSDeleteBlockAccount(strAccountNo, strAccountType, nSequence, nNik.ToString(), out  strError);

                    if (!string.IsNullOrEmpty(strError))
                    {
                        if (MessageBox.Show("Gagal ketika melakukan unblock rekening,\nerror message: " + strError + "\nLanjut update tanpa lakukan release?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                            return false;
                    }
                }

                return true;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public bool saveLelangWithBlock(DataSet dsInput
            //20210726, rezakahfi, BONDRETAIL-788, begin
            , string XMLSumberDana
            //20210726, rezakahfi, BONDRETAIL-788, end
            , string secAccNo
            , decimal dcCheckAmount
            , string currency
            , clsCallWebService clsCallWebService
            , string ProductCode
            , string AccountType
            , string NoRekInvestor
            , string SNAME
            , int intNik
            , string strBranch
            , string DealId
			//20220920, yudha.n, BONDRETAIL-1052, begin
            , out string DealIdOutput
            //20220920, yudha.n, BONDRETAIL-1052, end
            , string Process
            )
        {
            //20220920, yudha.n, BONDRETAIL-1052, begin
            DealIdOutput = DealId;
            //20220920, yudha.n, BONDRETAIL-1052, end
            try
            {
                ObligasiQuery cQuery = new ObligasiQuery();

                System.Data.DataSet dsResultCustomer = new System.Data.DataSet();
                if (!clsDatabase.subtrs_ListTreasuryCustomer_TM_Original(cQuery, new object[] { secAccNo, currency }, out dsResultCustomer))
                    return false;

                if (DealId == "")
	                DealId = getIdLelang();//isi by system;

                //20220920, yudha.n, BONDRETAIL-1052, begin
                DealIdOutput = DealId;
                //20220920, yudha.n, BONDRETAIL-1052, end
                  // block rekening 
                string strLogDesc = "";

                string strAccountNo = NoRekInvestor;
                string strAccountName = SNAME;

                int nSequence = 0;
                string strRecordID = "";
                string strTypeOfEntry = "HG";
                int nLowCheckNo = 0;
                int nHighCheckNo = 0;
                int nStopCharge = 0;
                string strPayeeName = "";

                string strHoldRemarks = "TRSSTRP_Lelang OBL-" + DealId; // harus diisi

                int nCheckRTNumber = 0;
                string strExpirationDate = ""; // harus diisi
                int nCheckDate = 0;
                int nDateMaintenance = 0;
                string strDatePlaced = ""; // harus diisi
                //20210308, rezakahfi, BONDRETAIL-725, begin
                //int nHoldBranch = int.Parse((string)clsGlobal.dsUserProfile.Tables[0].Rows[0]["office_id_sibs"]);
                int nHoldBranch = int.Parse(dsInput.Tables[0].Rows[0]["InsertedBranch"].ToString());
                //20210308, rezakahfi, BONDRETAIL-725, end
                string strWorkStationID = "ProObligasi"; // harus diisi
                int nTimeChangeMade = 0;
                string strReasonCode = "16"; // harus diisi
                int nNik = (int)clsGlobal.dsUserProfile.Tables[0].Rows[0]["nik"];
                string strPrefixCheckNo = "";
                System.Data.DataSet dsResponse = new System.Data.DataSet();
                string strError = "";

                string CIFId = dsResultCustomer.Tables[0].Rows[0]["CIFId"].ToString();

                clsCallWebService.CallOBL_WSAddBlockAccount(strLogDesc, strAccountNo, AccountType, nSequence, strRecordID,
                    strTypeOfEntry, dcCheckAmount, nLowCheckNo, nHighCheckNo
                    , nStopCharge, strPayeeName, strHoldRemarks, nCheckRTNumber
                    , strExpirationDate, nCheckDate, nDateMaintenance, strDatePlaced, nHoldBranch
                    , nNik, strWorkStationID, nTimeChangeMade, strReasonCode
                    , strPrefixCheckNo, out dsResponse, out strError);

                int blockSequence = 0;
                bool resQuery;

                if (!string.IsNullOrEmpty(strError))
                {
                    MessageBox.Show("Gagal ketika melakukan block rekening,\nerror message: " + strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                else
                {
                    blockSequence = int.Parse((string)dsResponse.Tables[0].Rows[0]["Sequence"]);
                    
                    dsInput.AcceptChanges();
                    dsInput.Tables[0].Rows[0]["IdLelang"] = DealId;
                    dsInput.Tables[0].Rows[0]["AccountBlockACTYPE"] = AccountType;
                    dsInput.Tables[0].Rows[0]["AccountBlockSequence"] = blockSequence;
                    dsInput.AcceptChanges();


                    string xmlFormat = dsInput.GetXml().ToString();

                    //INSERT KE DATABASE
                    OleDbParameter[] dbPar = new OleDbParameter[5];
                    dbPar[0] = new OleDbParameter("@pcXmlData", xmlFormat);
                    dbPar[1] = new OleDbParameter("@pnNIK", intNik);
                    dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
                    dbPar[3] = new OleDbParameter("@pcProcess", Process);
                    //20210726, rezakahfi, BONDRETAIL-788, begin
                    dbPar[4] = new OleDbParameter("@pcXMLSumberDana", XMLSumberDana);
                    //20210726, rezakahfi, BONDRETAIL-788, end

                    resQuery = this._cQuery.ExecProc("dbo.SecurityLelangTrxSubmit", ref dbPar);

                    //20230302, tobias, BONDRETAIL-1162, begin
                    //MessageBox.Show("Rekening diblokir:\nNomor Rekening: " + strAccountNo + ",\nNama Rekening: " + strAccountName + ",\nNominal: " + String.Format("{0:n}", dcCheckAmount) + ".", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (resQuery)
                    {
                        MessageBox.Show("Rekening diblokir:\nNomor Rekening: " + strAccountNo + ",\nNama Rekening: " + strAccountName + ",\nNominal: " + String.Format("{0:n}", dcCheckAmount) + ".", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    //20230302, tobias, BONDRETAIL-1162, end
                }

                if (resQuery)
                {
                    MessageBox.Show("Data Success Tersimpan\n Deal Id : " + DealId + "\n ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
				//20230302, tobias, BONDRETAIL-1162, begin
                else
                {   
                    if (blockSequence >  0)
                    {
                        if (!RelaseBlock(clsCallWebService, strAccountNo, AccountType, blockSequence, intNik))
                        {
                            MessageBox.Show("Data Gagal Tersimpan dan blokir sudah terpasang.\nNomor Rekening: " + strAccountNo + ",\nNama Rekening: " + strAccountName + ",\nNominal: " + String.Format("{0:n}", dcCheckAmount) + ".\n silahkan lepas blokir manual melalui Pro Account", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    
                }
                //20230302, tobias, BONDRETAIL-1162, end

                return resQuery;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public bool saveLelangWithoutBlock(DataSet dsInput, string XMLSumberDana
            , string secAccNo
            , string currency
            , int intNik
            , string strBranch
            , string DealId
            //20220920, yudha.n, BONDRETAIL-1052, begin
            , out string DealIdOutput
            //20220920, yudha.n, BONDRETAIL-1052, end
            , string Process
            )
        {
            //20220920, yudha.n, BONDRETAIL-1052, begin
            DealIdOutput = DealId;
            //20220920, yudha.n, BONDRETAIL-1052, end
            try
            {
                ObligasiQuery cQuery = new ObligasiQuery();

                System.Data.DataSet dsResultCustomer = new System.Data.DataSet();
                if (!clsDatabase.subtrs_ListTreasuryCustomer_TM_Original(cQuery, new object[] { secAccNo, currency }, out dsResultCustomer))
                    return false;

                if (DealId == "")
                    DealId = getIdLelang();//isi by system;

                //20220920, yudha.n, BONDRETAIL-1052, begin
                DealIdOutput = DealId;
                //20220920, yudha.n, BONDRETAIL-1052, end
                dsInput.AcceptChanges();
                dsInput.Tables[0].Rows[0]["IdLelang"] = DealId;
                dsInput.AcceptChanges();

                string xmlFormat = dsInput.GetXml().ToString();



                //INSERT KE DATABASE
                OleDbParameter[] dbPar = new OleDbParameter[5];
                dbPar[0] = new OleDbParameter("@pcXmlData", xmlFormat);
                dbPar[1] = new OleDbParameter("@pnNIK", intNik);
                dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
                dbPar[3] = new OleDbParameter("@pcProcess", Process);
                dbPar[4] = new OleDbParameter("@pcXMLSumberDana", XMLSumberDana);

                bool resQuery = this._cQuery.ExecProc("dbo.SecurityLelangTrxSubmit", ref dbPar);

                if (resQuery)
                {
                    if (Process == "Update")
                    {
                        if (dsInput.Tables[0].Rows[0]["Status"].ToString() == "DLT")
                        {
                            MessageBox.Show("Deal Id : " + DealId + " berhasil dihapus", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (dsInput.Tables[0].Rows[0]["Status"].ToString() == "CFMT")
                        {
                            MessageBox.Show("Deal Id : " + DealId + " berhasil di-confirm tidak ikut", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                            MessageBox.Show("Data Success di-Process\n Deal Id : " + DealId + "\n ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Data Success Tersimpan\n Deal Id : " + DealId + "\n ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                return resQuery;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        //20220920, yudha.n, BONDRETAIL-1052, begin
        public bool BlokirRekeningBiayaMaterai(clsBlokirSaldo clsBlokirSaldo, decimal dcMateraiAmountBlock, string AccountTypeMaterai, string NoRekMaterai, string DealId, out int BlockSequenceMaterai, string strExpirationDate)
        {
            ObligasiQuery cQuery = new ObligasiQuery();
            BlockSequenceMaterai = 0;

            try
            {
                string HoldRemarks = "TRSSTRP_Lelang Materai OBL-" + DealId.ToString();
                if (!clsBlokirSaldo.AddBlockSaldoRekening(
                        dcMateraiAmountBlock
                        , AccountTypeMaterai
                        , NoRekMaterai
                        , strExpirationDate
                        , HoldRemarks
                        , out BlockSequenceMaterai
                        )
                    )
                {
                    throw new Exception("Gagal ketika melakukan blokir biaya meterai!");
                }
                else
                {
                    if (!BankNISP.Obligasi01.clsDatabase.subTRSUpdateBlokirMateraiFee(cQuery, long.Parse(DealId), BlockSequenceMaterai, AccountTypeMaterai, "TRXLELANG"))
                    {
                        throw new Exception("Gagal ketika mengupdate informasi blokir meterai ke database!");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool RelaseBlokirRekeningBiayaMaterai(clsBlokirSaldo clsBlokirSaldo, string noRekInvestor, string strAccountType, int blockSequence)
        {
            try
            {
                System.Data.DataSet dsResponse = new System.Data.DataSet();
                string strError = "";
                if (blockSequence > 0)
                {
                    if (!clsBlokirSaldo.RelaseBlockRekening(noRekInvestor, strAccountType, blockSequence))
                    {
                        if (MessageBox.Show("Gagal ketika melakukan unblock rekening,\nerror message: " + strError + "\nLanjut update tanpa lakukan release?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                            return false;
                    }
                }

                return true;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        //20220920, yudha.n, BONDRETAIL-1052, end

        public bool PopulateSourceOfFundByDeal(string DealId, out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;
            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[2];
                dbPar[0] = new OleDbParameter("@pcProduct", "Lelang");
                dbPar[1] = new OleDbParameter("@pnDealId", DealId);

                bResult = this._cQuery.ExecProc("dbo.TRSPopulateSourceOfFundDeal", ref dbPar, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return bResult;
        }
        #endregion

        #region Replace Trx Lelang

        public bool PopulateDetailReplaceLelang(string IdLelang, out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;
            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[1];
                dbPar[0] = new OleDbParameter("@pnIdLelang_Source", IdLelang);
                

                bResult = this._cQuery.ExecProc("dbo.SecurityPopulateDetailReplaceTrxLelang", ref dbPar, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return bResult;
        }

        public bool saveLelangWithBlockReplace(DataSet dsInput
            //20210726, rezakahfi, BONDRETAIL-788, begin
            , string XMLSumberDana
            //20210726, rezakahfi, BONDRETAIL-788, end
            , string IdLelangSource
            , string secAccNo
            , decimal dcCheckAmount
            , string currency
            , clsCallWebService clsCallWebService
            , string ProductCode
            , string AccountType
            , string NoRekInvestor
            , string SNAME
            , int intNik
            , string strBranch
            , string DealId
            , string Process
            //20220920, yudha.n, BONDRETAIL-1052, begin
            , out string DealIdOutput
            //20220920, yudha.n, BONDRETAIL-1052, end
            )
        {
            //20220920, yudha.n, BONDRETAIL-1052, begin
            DealIdOutput = DealId;
            //20220920, yudha.n, BONDRETAIL-1052, end
            try
            {
                ObligasiQuery cQuery = new ObligasiQuery();

                System.Data.DataSet dsResultCustomer = new System.Data.DataSet();
                if (!clsDatabase.subtrs_ListTreasuryCustomer_TM_Original(cQuery, new object[] { secAccNo, currency }, out dsResultCustomer))
                    return false;

                if (DealId == "")
                    DealId = getIdLelang();//isi by system;
                //20210607, rezakahfi, BONDRETAIL-732, begin
                if (DealId.ToString() == "0" || DealId.ToString() == "")
                {
                    MessageBox.Show("DealId is not allowed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                //20210607, rezakahfi, BONDRETAIL-732, end
                //20220920, yudha.n, BONDRETAIL-1052, begin
                DealIdOutput = DealId;
                //20220920, yudha.n, BONDRETAIL-1052, end

                // block rekening 
                string strLogDesc = "";

                string strAccountNo = NoRekInvestor;
                string strAccountName = SNAME;

                int nSequence = 0;
                string strRecordID = "";
                string strTypeOfEntry = "HG";
                int nLowCheckNo = 0;
                int nHighCheckNo = 0;
                int nStopCharge = 0;
                string strPayeeName = "";

                string strHoldRemarks = "TRSSTRP_LelangOBL-" + DealId; // harus diisi

                int nCheckRTNumber = 0;
                string strExpirationDate = ""; // harus diisi
                int nCheckDate = 0;
                int nDateMaintenance = 0;
                string strDatePlaced = ""; // harus diisi
                int nHoldBranch = int.Parse((string)clsGlobal.dsUserProfile.Tables[0].Rows[0]["office_id_sibs"]);
                string strWorkStationID = "ProObligasi"; // harus diisi
                int nTimeChangeMade = 0;
                string strReasonCode = "16"; // harus diisi
                int nNik = (int)clsGlobal.dsUserProfile.Tables[0].Rows[0]["nik"];
                string strPrefixCheckNo = "";
                System.Data.DataSet dsResponse = new System.Data.DataSet();
                string strError = "";

                string CIFId = dsResultCustomer.Tables[0].Rows[0]["CIFId"].ToString();

                clsCallWebService.CallOBL_WSAddBlockAccount(strLogDesc, strAccountNo, AccountType, nSequence, strRecordID,
                    strTypeOfEntry, dcCheckAmount, nLowCheckNo, nHighCheckNo
                    , nStopCharge, strPayeeName, strHoldRemarks, nCheckRTNumber
                    , strExpirationDate, nCheckDate, nDateMaintenance, strDatePlaced, nHoldBranch
                    , nNik, strWorkStationID, nTimeChangeMade, strReasonCode
                    , strPrefixCheckNo, out dsResponse, out strError);

                int blockSequence = 0;
                bool resQuery;

                if (!string.IsNullOrEmpty(strError))
                {
                    MessageBox.Show("Gagal ketika melakukan block rekening,\nerror message: " + strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                else
                {
                    blockSequence = int.Parse((string)dsResponse.Tables[0].Rows[0]["Sequence"]);
                    
                    dsInput.AcceptChanges();
                    dsInput.Tables[0].Rows[0]["IdLelang"] = DealId;
                    dsInput.Tables[0].Rows[0]["AccountBlockACTYPE"] = AccountType;
                    dsInput.Tables[0].Rows[0]["AccountBlockSequence"] = blockSequence;
                    dsInput.AcceptChanges();

                    string xmlFormat = dsInput.GetXml().ToString();

                    //INSERT KE DATABASE
                    OleDbParameter[] dbPar = new OleDbParameter[6];
                    dbPar[0] = new OleDbParameter("@pnIdLelang_Source", IdLelangSource);
                    dbPar[1] = new OleDbParameter("@pcXmlData", xmlFormat);
                    dbPar[2] = new OleDbParameter("@pnNIK", intNik);
                    dbPar[3] = new OleDbParameter("@pcBranch", strBranch);
                    dbPar[4] = new OleDbParameter("@pcProcess", Process);
                    //20210726, rezakahfi, BONDRETAIL-788, begin
                    dbPar[5] = new OleDbParameter("@pcXMLSumberDana", XMLSumberDana);
                    //20210726, rezakahfi, BONDRETAIL-788, end

                    resQuery = this._cQuery.ExecProc("dbo.SecurityLelangTrxSubmitReplaceDetail", ref dbPar);

                    MessageBox.Show("Rekening diblokir:\nNomor Rekening: " + strAccountNo + ",\nNama Rekening: " + strAccountName + ",\nNominal: " + String.Format("{0:n}", dcCheckAmount) + ".", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                return resQuery;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public bool saveLelangWithoutBlockReplace(DataSet dsInput, string XMLSumberDana
            , string IdLelangSource
            , int intNik
            , string strBranch
            , string DealId
            , string Process
            //20220920, yudha.n, BONDRETAIL-1052, begin
            , out string DealIdOutput
            //20220920, yudha.n, BONDRETAIL-1052, end
            )
        {
            //20220920, yudha.n, BONDRETAIL-1052, begin
            DealIdOutput = DealId;
            //20220920, yudha.n, BONDRETAIL-1052, end
            try
            {
                ObligasiQuery cQuery = new ObligasiQuery();

                if (DealId == "")
                    DealId = getIdLelang();//isi by system;

                //20220920, yudha.n, BONDRETAIL-1052, begin
                DealIdOutput = DealId;
                //20220920, yudha.n, BONDRETAIL-1052, end

                dsInput.AcceptChanges();
                dsInput.Tables[0].Rows[0]["IdLelang"] = DealId;
                dsInput.AcceptChanges();

                string xmlFormat = dsInput.GetXml().ToString();

                //INSERT KE DATABASE
                OleDbParameter[] dbPar = new OleDbParameter[6];
                dbPar[0] = new OleDbParameter("@pnIdLelang_Source", IdLelangSource);
                dbPar[1] = new OleDbParameter("@pcXmlData", xmlFormat);
                dbPar[2] = new OleDbParameter("@pnNIK", intNik);
                dbPar[3] = new OleDbParameter("@pcBranch", strBranch);
                dbPar[4] = new OleDbParameter("@pcProcess", Process);
                dbPar[5] = new OleDbParameter("@pcXMLSumberDana", XMLSumberDana);

                bool resQuery = this._cQuery.ExecProc("dbo.SecurityLelangTrxSubmitReplaceDetail", ref dbPar);

                return resQuery;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public bool ProcessLelangToTransaction(string xmlFormat
            , int intNik
            , string strBranch
            , string Guid
            )
        {

            try
            {
                ObligasiQuery cQuery = new ObligasiQuery();

                //INSERT KE DATABASE
                OleDbParameter[] dbPar = new OleDbParameter[4];
                dbPar[0] = new OleDbParameter("@pcXmlData", xmlFormat);
                dbPar[1] = new OleDbParameter("@pnNIK", intNik);
                dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
                dbPar[3] = new OleDbParameter("@uGuid", Guid);

                bool resQuery = this._cQuery.ExecProc("dbo.SecurityProcessLelangToTransaction", ref dbPar);

                return resQuery;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        #endregion

        #region Hasil Lelang

        public bool PopulateResultLelangProduct(out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;

            try
            {
                bResult = this._cQuery.ExecProc("dbo.SecurityPopulateResultLelangProduct", out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return bResult;
        }

        public bool PopulateResultLelangTA(bool AfterResult,out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[1];
                dbPar[0] = new OleDbParameter("@pbAfterResult", AfterResult);

                bResult = this._cQuery.ExecProc("dbo.SecurityPopulateResultLelangTA",ref dbPar, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return bResult;
        }

        public bool PopulateResultLelangTransaction(string strXmlData, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcXmlData", strXmlData);

            return this._cQuery.ExecProc("dbo.SecurityPopulateResultLelangTransaction", ref dbPar, out dsOut);
        }

        public bool UpdateResultProduct(string ResultId
            , int intNik
            , string strBranch
            , string status
           )
        {
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pnResultId", ResultId);
            dbPar[1] = new OleDbParameter("@pnNIK", intNik);
            dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
            dbPar[3] = new OleDbParameter("@pcStatus", status);

            return this._cQuery.ExecProc("dbo.SecurityLelangUpdateResultProduct", ref dbPar);
        }

        public bool UpdateResultBatch(string ResultId
            , int intNik
            , string strBranch
            , string status
            , string Guid
            , string BatchId
            , string DealType
            , string xml
           )
        {
            OleDbParameter[] dbPar = new OleDbParameter[8];
            dbPar[0] = new OleDbParameter("@pnResultId", ResultId);
            dbPar[1] = new OleDbParameter("@pnNIK", intNik);
            dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
            dbPar[3] = new OleDbParameter("@pcStatus", status);
            dbPar[4] = new OleDbParameter("@uGuid", Guid);
            dbPar[5] = new OleDbParameter("@pnBatchId", BatchId);
            dbPar[6] = new OleDbParameter("@pcDealType", DealType);
            dbPar[7] = new OleDbParameter("@pcXML", xml);

            return this._cQuery.ExecProc("dbo.SecurityLelangUpdateResultProduct", ref dbPar);
        }

        public bool UpdateResultTA(string ResultId
            , int intNik
            , string strBranch
            , string status
           )
        {
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pnResultId", ResultId);
            dbPar[1] = new OleDbParameter("@pnNIK", intNik);
            dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
            dbPar[3] = new OleDbParameter("@pcStatus", status);

            return this._cQuery.ExecProc("dbo.SecurityLelangUpdateResultTA", ref dbPar);
        }
        
        public bool UpdateResultBatchTA(string ResultId
            , int intNik
            , string strBranch
            , string status
            , string Guid
            , string DealType
            , string xml
           )
        {
            OleDbParameter[] dbPar = new OleDbParameter[7];
            dbPar[0] = new OleDbParameter("@pnResultId", ResultId);
            dbPar[1] = new OleDbParameter("@pnNIK", intNik);
            dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
            dbPar[3] = new OleDbParameter("@pcStatus", status);
            dbPar[4] = new OleDbParameter("@uGuid", Guid);
            dbPar[5] = new OleDbParameter("@pcDealType", DealType);
            dbPar[6] = new OleDbParameter("@pcXML", xml);

            return this._cQuery.ExecProc("dbo.SecurityLelangUpdateResultTA", ref dbPar);
        }

        public string GetXMLToMurex(string ResultId
            , string BatchType
            , string pcDealType
            , string Guid
            )
        {
            string strResultXML = "";
            DataSet dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcResultId", ResultId);
            dbPar[1] = new OleDbParameter("@pcBatchType", BatchType);
            dbPar[2] = new OleDbParameter("@pcDealType", pcDealType);
            dbPar[3] = new OleDbParameter("@uGuid", Guid);

            if (this._cQuery.ExecProc("dbo.SecurityGetXMLLelang", ref dbPar, out dsOut))
            {
                if (dsOut.Tables.Count > 0)
                    strResultXML = Model.clsXML.GetXMLFLD(dsOut.Tables[0].Copy());
            }

            return strResultXML;
        }


        public bool ProcessResultLelang(string strXmlData
            , int intNik
            , string strBranch
            , string strStatus)
        {

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcXmlData", strXmlData);
            dbPar[1] = new OleDbParameter("@pnNIK", intNik);
            dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
            dbPar[3] = new OleDbParameter("@pcStatus", strStatus);

            return this._cQuery.ExecProc("dbo.SecurityProcessResultLelang", ref dbPar);
        }

        public bool ProcessResultLelangTA(string strXmlData
            , int intNik
            , string strBranch
            , string strStatus)
        {

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcXmlData", strXmlData);
            dbPar[1] = new OleDbParameter("@pnNIK", intNik);
            dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
            dbPar[3] = new OleDbParameter("@pcStatus", strStatus);

            return this._cQuery.ExecProc("dbo.SecurityProcessResultLelangTA", ref dbPar);
        }

        public bool Blockir(string ACCTNO
                            , decimal AmountBlock
                            , string Remark
                            , clsCallWebService clsCallWebService
                            , string nikUser
                            , int nHoldBranch
                            , out int blockSequence, out string blockActType)
        {
            /*
             * Param Global
             * ACCTNO
             * CIFNo
             * AmountBlock
             * this._modData.GetBlockAmount
             * this.Murex.Trim()
             * ValueDate
             */
            string result = "";
            string rejectDesc = "";
            string strAccountType = "";
            string strAccountName = "";
            blockSequence = 0;
            blockActType = "";
            string ProductCode = "";
            decimal OriginalBalance = 0;
            Guid strNewGuid;
            strNewGuid = Guid.NewGuid();

            string strLogDesc = "";
            string strAccountNo = ACCTNO;
            

            clsCallWebService.CallAccountInquiry(strNewGuid.ToString(), nikUser, strAccountNo, out result, out rejectDesc);

            if (string.IsNullOrEmpty(rejectDesc) && !string.IsNullOrEmpty(result))
            {
                DataSet dsDataAcct = new DataSet();
                dsDataAcct.ReadXml(new System.IO.StringReader(result));

                strAccountType = dsDataAcct.Tables[0].Rows[0]["AccountType"].ToString();
                strAccountName = dsDataAcct.Tables[0].Rows[0]["ShortName"].ToString();
                ProductCode = dsDataAcct.Tables[0].Rows[0]["ProductCode"].ToString();
                OriginalBalance = decimal.Parse(dsDataAcct.Tables[0].Rows[0]["ATMEffectiveBalance"].ToString());
            }
            else
            {
                strAccountType = "S";
                MessageBox.Show(strAccountNo + " - " + rejectDesc.Trim());
                return false;
            }

            decimal dcCheckAmount;
            
            dcCheckAmount = AmountBlock;

            int nSequence = 0;
            string strRecordID = "";
            string strTypeOfEntry = "HG";
            int nLowCheckNo = 0;
            int nHighCheckNo = 0;
            int nStopCharge = 0;
            string strPayeeName = "";
            string strHoldRemarks = Remark; // harus diisi
            int nCheckRTNumber = 0;
            string strExpirationDate = ""; // harus diisi
            int nCheckDate = 0;
            int nDateMaintenance = 0;
            string strDatePlaced = ""; // harus diisi
            
            string strWorkStationID = "ProObligasi"; // harus diisi
            int nTimeChangeMade = 0;
            string strReasonCode = "16"; // harus diisi
            int nNik = int.Parse(nikUser);// 7000401;//NIKInputer
            string strPrefixCheckNo = "";

            System.Data.DataSet dsResponse = new System.Data.DataSet();
            string strError = "";
            int notOK = 0;

            //blokir dulu, takutnya belum terkredit duitnya dari onfx
            //if (dcCheckAmount > OriginalBalance)
            //{
            //    MessageBox.Show("Dana Nasabah tidak mencukupi", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return false;
            //}

            //OriginalBalance
            if (notOK == 0)
            {
                clsCallWebService.CallOBL_WSAddBlockAccount(strLogDesc, strAccountNo, strAccountType, nSequence, strRecordID,
                    strTypeOfEntry, dcCheckAmount, nLowCheckNo, nHighCheckNo
                    , nStopCharge, strPayeeName, strHoldRemarks, nCheckRTNumber
                    , strExpirationDate, nCheckDate, nDateMaintenance, strDatePlaced, nHoldBranch
                    , nNik, strWorkStationID, nTimeChangeMade, strReasonCode
                    , strPrefixCheckNo, out dsResponse, out strError);

                if (!string.IsNullOrEmpty(strError))
                {
                    MessageBox.Show("Gagal ketika melakukan block rekening " + strAccountNo + ",\nerror message: " + strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                if (dsResponse.Tables.Count > 0)
                {
                    blockSequence = int.Parse((string)dsResponse.Tables[0].Rows[0]["Sequence"]);
                    blockActType = (string)dsResponse.Tables[0].Rows[0]["AccountType"];
                }
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool UpdateStatusTrxLelang(DataSet dsInput
            , string XMLSumberDana
            , int intNik
            , string strBranch
            , string DealId
            )
        {
            try
            {
               ObligasiQuery cQuery = new ObligasiQuery();

               dsInput.AcceptChanges();
               dsInput.Tables[0].Rows[0]["IdLelang"] = DealId;
               dsInput.AcceptChanges();

               string xmlFormat = dsInput.GetXml().ToString();

               //INSERT KE DATABASE
               OleDbParameter[] dbPar = new OleDbParameter[5];
               dbPar[0] = new OleDbParameter("@pcXmlData", xmlFormat);
               dbPar[1] = new OleDbParameter("@pnNIK", intNik);
               dbPar[2] = new OleDbParameter("@pcBranch", strBranch);
               dbPar[3] = new OleDbParameter("@pcProcess", "Update");
               dbPar[4] = new OleDbParameter("@pcXMLSumberDana", XMLSumberDana);

               bool resQuery = this._cQuery.ExecProc("dbo.SecurityLelangTrxSubmit", ref dbPar);

               return resQuery;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public bool getDataMail(
             string IdLelang
            ,string SecurityNo
            ,string TypeEmail
            ,int intNik
            ,string strBranch
            ,out DataSet dsOut
            )
        {
            dsOut = new DataSet();

            try
            {
                
                //INSERT KE DATABASE
                OleDbParameter[] dbPar = new OleDbParameter[5];
                dbPar[0] = new OleDbParameter("@pnIdLelang", IdLelang);
                dbPar[1] = new OleDbParameter("@pcSecurityNo", SecurityNo);
                dbPar[2] = new OleDbParameter("@pcTypeEmail", TypeEmail);
                dbPar[3] = new OleDbParameter("@pnNIK", intNik);
                dbPar[4] = new OleDbParameter("@pcBranch", strBranch);

                bool resQuery = this._cQuery.ExecProc("dbo.SecurityPopulateResultLelangEmail", ref dbPar, out dsOut);

                return resQuery;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        //
        public bool isAllowStatus(string DealNo, string MenuName)
        {
            bool bResult = false;

            string strXML = "<data><row><DealNo>" + DealNo + "</DealNo></row></data>";
            string strOutput = "";

            if (this.LelangCheckStatus(strXML, MenuName, out strOutput))
            {
                if (strOutput.Trim() != "")
                {
                    MessageBox.Show(strOutput, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    bResult = false;
                }
                else
                {
                    bResult = true;
                }
            }

            return bResult;
        }

        public bool isAllowStatus(DataTable dtProcess, string MenuName)
        {
            bool bResult = false;

            DataSet dsProcess = new DataSet("data");
            dtProcess.AcceptChanges();
            dtProcess.TableName = "row";
            dtProcess.AcceptChanges();

            dsProcess.Tables.Add(dtProcess.Copy());

            string strXML = dsProcess.GetXml().ToString();
            string strOutput = "";

            if (this.LelangCheckStatus(strXML, MenuName, out strOutput))
            {
                if (strOutput.Trim() != "")
                {
                    MessageBox.Show(strOutput, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    bResult = false;
                }
                else
                {
                    bResult = true;
                }
            }

            return bResult;
        }

        public bool LelangCheckStatus(string xmlData, string MenuName, out string strOutput)
        {
            strOutput = "";
            OleDbParameter[] dbParams = new OleDbParameter[4];
            bool bResult = false;

            try
            {
                dbParams[0] = new OleDbParameter("@pcXmlData", xmlData);
                dbParams[1] = new OleDbParameter("@pcMenuName", MenuName);
                dbParams[2] = new OleDbParameter("@pcAction", "Save");
                dbParams[3] = new OleDbParameter("@pcOutputMsg", OleDbType.VarChar, 255);
                dbParams[3].Direction = ParameterDirection.Output;

                if (this._cQuery.ExecProc("dbo.SecurityLelangCheckStatus", ref dbParams))
                {
                    strOutput = dbParams[3].Value.ToString();
                    bResult = true;
                }
                else
                {
                    bResult = false;
                }

                return bResult;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("SecurityLelangCheckStatus : " + ex.Message.ToString());
                return false;
            }
        }
        //

        #endregion

        //20210928, rezakahfi, BONDRETAIL-822, begin
        public System.Data.DataSet findSecAccNo(string strFind, string Currency, long IdLelang)
        {
            ObligasiQuery cQuery = new ObligasiQuery();
            System.Data.DataSet ds = new System.Data.DataSet();
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[4];

            try
            {
                odpParam[0] = new System.Data.OleDb.OleDbParameter();
                odpParam[0].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[0].Value = strFind;

                odpParam[1] = new System.Data.OleDb.OleDbParameter();
                odpParam[1].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[1].Value = Currency;

                odpParam[2] = new System.Data.OleDb.OleDbParameter();
                odpParam[2].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[2].Value = "Lelang";

                odpParam[3] = new System.Data.OleDb.OleDbParameter();
                odpParam[3].OleDbType = System.Data.OleDb.OleDbType.BigInt;
                odpParam[3].Value = IdLelang;

                cQuery.ExecProc("dbo.trs_ListTreasuryCustomer_TM_Original", ref odpParam, out ds);
                return ds;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
                return ds;
            }
        }
        //20210928, rezakahfi, BONDRETAIL-822, end
		//20220920, yudha.n, BONDRETAIL-1052, begin
        public System.Data.DataSet findSecAccNoMaterai(string strFind, string Currency, long IdLelang)
        {
            ObligasiQuery cQuery = new ObligasiQuery();
            System.Data.DataSet ds = new System.Data.DataSet();
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[4];

            try
            {
                odpParam[0] = new System.Data.OleDb.OleDbParameter();
                odpParam[0].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[0].Value = strFind;

                odpParam[1] = new System.Data.OleDb.OleDbParameter();
                odpParam[1].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[1].Value = Currency;

                odpParam[2] = new System.Data.OleDb.OleDbParameter();
                odpParam[2].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[2].Value = "Lelang";

                odpParam[3] = new System.Data.OleDb.OleDbParameter();
                odpParam[3].OleDbType = System.Data.OleDb.OleDbType.BigInt;
                odpParam[3].Value = IdLelang;

                cQuery.ExecProc("dbo.trs_ListTreasuryCustomer_TM_Original", ref odpParam, out ds);
                return ds;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
                return ds;
            }
        }

        public bool CalculateMateraiFee(string ccy, decimal totalProceed, out decimal nominalMaterai, string product)
        {
            DataSet dsOut = new DataSet();
            bool bResult = false;
            nominalMaterai = 0;

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[4];
                dbPar[0] = new OleDbParameter("@pcCCY", ccy);
                dbPar[1] = new OleDbParameter("@pnTotalProceed", totalProceed);
                dbPar[2] = new OleDbParameter("@pnNominalMaterai", nominalMaterai);
                dbPar[2].Direction = System.Data.ParameterDirection.Output;
                dbPar[3] = new OleDbParameter("@pcProduct", product);

                bResult = this._cQuery.ExecProc("dbo.TRSCalculateMateraiFee", ref dbPar, out dsOut);
                return bResult;
            }
            catch (Exception ex)
            {
                MessageBox.Show("CalculateMateraiFee: " + ex.Message.ToString());
                return false;
            }
        }
        //20210928, yudha.n, BONDRETAIL-822, end
    }
}
