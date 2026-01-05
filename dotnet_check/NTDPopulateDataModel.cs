using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace BankNISP.Obligasi01.Model
{
    class NTDPopulateDataModel
    {
        private ObligasiQuery _cQuery ;
        private int _userNIK;
        private string _userBranch;
        //20230801, yudha.n, BONDRETAIL-1394, begin
        private wsOmniObli.clsService clsOmniService;
        //20230801, yudha.n, BONDRETAIL-1394, end

        public NTDPopulateDataModel(ObligasiQuery cQuery, int nik, string branch)
        {
            this._cQuery = cQuery;
            this._userNIK = nik;
            this._userBranch = branch;
            //20230801, yudha.n, BONDRETAIL-1394, begin
            clsOmniService = new BankNISP.Obligasi01.wsOmniObli.clsService();
            //20230801, yudha.n, BONDRETAIL-1394, end
        }

        #region "Populate Data Transaction"
        /* Populate CurrencyPair + Rate, Value, Source of Fund, Parameter Fitur */
        public bool PopulateDataParam(out DataSet dsOut)
        {
            //20230801, yudha.n, BONDRETAIL-1394, begin
            //return this._cQuery.ExecProc("dbo.TRSPopulateDataParamNTD", out dsOut); 
            string xmlDataTDealDocument = "";
            GetTDealDocument(out xmlDataTDealDocument);

            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcXMLTDealDocument", xmlDataTDealDocument);

            return this._cQuery.ExecProc("dbo.TRSPopulateDataParamNTD", ref dbParams, out dsOut);
            //20230801, yudha.n, BONDRETAIL-1394, end
        }

        //20230801, yudha.n, BONDRETAIL-1394, begin
        public bool GetTDealDocument(out string xmlDataTDealDocument)
        {
            bool isSuccess = false;
            DataSet dsOut = new DataSet();
            xmlDataTDealDocument = "";
            string query = "SELECT FId, FExtLink, FLabel FROM dbo.TDealDocument";

            try
            {
                string paramOut = "", message = "";
                isSuccess = clsOmniService.APIExecQuery(clsGlobal.strEncConnStringSMARTFX, query, "", out paramOut, out dsOut, out message);

                if (dsOut.Tables.Count > 0)
                {
                    dsOut.DataSetName = "DocumentElement";
                    DataTable dtToXMLCalculate = dsOut.Tables[0];
                    dtToXMLCalculate.TableName = "TDealDocument";
                    StringBuilder stringBuilderInner = new StringBuilder();
                    dtToXMLCalculate.WriteXml(System.Xml.XmlWriter.Create(stringBuilderInner));
                    xmlDataTDealDocument = stringBuilderInner.ToString();
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message.ToString());
            }
            return isSuccess;
        }
        //20230801, yudha.n, BONDRETAIL-1394, end

        /* Populate Detail Data Nasabah, CIF2, Related Party, Risk Profile, Phone Order */
        public bool PopulateDetailDataNasabah(string strCIF, out DataSet dsOut)
        {
            dsOut = new DataSet() ; 
            OleDbParameter[] dbPar = new OleDbParameter[1];
            //20180305, vanny_w, BOSIT17195, begin
            dbPar[0] = new OleDbParameter("@pcCIF", strCIF);
            //20180305, vanny_w, BOSIT17195, end
            return this._cQuery.ExecProc("dbo.TRSPopulateDetailDataNasabahNTD", ref dbPar, out dsOut); 
        }

        /* Populate Data Account Name, Account Debet, Account Credit Nasabah CIF1 dan CIF2 (Jika ada), Base CCY */
        //20180305, vanny_w, BOSIT17195, begin
        public bool PopulateAccountNasabah(string strCIF, string strCIF2, string strCCYPair, string strCCY, string strBuySell, string bTax, out DataSet dsOut)
        {
            dsOut = new DataSet(); 
            OleDbParameter[] dbPar = new OleDbParameter[6] ; 
            dbPar[0] = new OleDbParameter("@pcCIF", strCIF) ;
            dbPar[1] = new OleDbParameter("@pcCIF2", strCIF2) ;
            dbPar[2] = new OleDbParameter("@pcCCYPair", strCCYPair);
            dbPar[3] = new OleDbParameter("@pcCCY", strCCY);
            dbPar[4] = new OleDbParameter("@pcBuySell", strBuySell);
            dbPar[5] = new OleDbParameter("@pcTax", bTax); 

            return this._cQuery.ExecProc("dbo.TRSPopulateAccountNasabahNTD", ref dbPar, out dsOut) ; 
        }
        //20180305, vanny_w, BOSIT17195, end

        /* Check Parameter Fitur && Setting Parameter */
        //20190213, vanny_w, TRBST16233, begin
        public bool PopulateCheckParamFitur(string strParam, decimal dcRate, decimal dcRefRate, string strValue, string TradeDate, string strCcyPair, out DataSet dsOut)
        //20190213, vanny_w, TRBST16233, end        
        {
            //20190213, vanny_w, TRBST16233, begin
            //OleDbParameter[] dbPar = new OleDbParameter[5];
            OleDbParameter[] dbPar = new OleDbParameter[6];
            //20190213, vanny_w, TRBST16233, end
            //20180305, vanny_w, BOSIT17195, begin
            dbPar[0] = new OleDbParameter("@pcParam", strParam);
            dbPar[1] = new OleDbParameter("@pnRate", dcRate);
            dbPar[2] = new OleDbParameter("@pnRefRate", dcRefRate); 
            dbPar[3] = new OleDbParameter("@pcValue", strValue);
            dbPar[4] = new OleDbParameter("@pcTradeDate", TradeDate);
            //20190213, vanny_w, TRBST16233, begin
            dbPar[5] = new OleDbParameter("@pcCcy", strCcyPair);
            //20190213, vanny_w, TRBST16233, end            
            //20180225, vanny_BOSIT17195, end
            return this._cQuery.ExecProc("dbo.TRSPopulateCheckParamFiturNTD", ref dbPar, out dsOut);
        }

        /* Hitung Pokok Penempatan, Rate, Margin, Pokok (Eq USD) */
        //20180309, vanny_w, BOSIT17195, begin
        //20180410, vanny_w, BOSIT17195, begin

        public bool PopulateCalculateTransaction(string strCCYPair, string strCCY, string strBuySell, 
            decimal dcBaseAmount, decimal dcIORate, decimal dcCustRate, decimal dcSwapPoint, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[7] ; 
            dbPar[0] = new OleDbParameter("@pcCCYPair", strCCYPair);
            dbPar[1] = new OleDbParameter("@pcCCY", strCCY);
            dbPar[2] = new OleDbParameter("@pcBuySell", strBuySell); 
            dbPar[3] = new OleDbParameter("@pnBaseAmount", dcBaseAmount);
            dbPar[4] = new OleDbParameter("@pnIORate", dcIORate);
            dbPar[5] = new OleDbParameter("@pnCustRate", dcCustRate);
            dbPar[6] = new OleDbParameter("@pnSwapPoint", dcSwapPoint); 
        //20180410, vanny_w, BOSIT17195, end
        //20180309, vanny_w, BOSIT17195, end
            return this._cQuery.ExecProc("dbo.TRSPopulateCalculateTransactionNTD", ref dbPar, out dsOut); 
        }

        public bool PopulateParam(string strParam, string strParam1, string strParam2, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[3];
                //20180305, vanny_w, BOSIT17195, begin
                dbPar[0] = new OleDbParameter("@pcParam", strParam);
                dbPar[1] = new OleDbParameter("@pcParam1", strParam1);
                dbPar[2] = new OleDbParameter("@pcParam2", strParam2);
                //20180305, vanny_w, BOSIT17195, end
                blnResult = this._cQuery.ExecProc("dbo.TRSPopulateDataSubmitNTD", ref dbPar, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //20180425, vanny_w, BOSIT1715, begin
        public bool PopulateSourceOfFund(string strCIF, string strBaseCcy, string strTradeDate, string strMatureDate, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcCIF", strCIF);
            dbPar[1] = new OleDbParameter("@pcBaseCcy", strBaseCcy);
            dbPar[2] = new OleDbParameter("@pcTradeDate", strTradeDate);
            dbPar[3] = new OleDbParameter("@pcMatureDate", strMatureDate); 
            return this._cQuery.ExecProc("dbo.TRSPopulateCheckSourceFundNTD", ref dbPar, out dsOut);
        }
        //20180425, vanny_w, BOSIT1715, end

        public bool PopulateCheckUpdateSourceFund(int iDealId, string xmlData, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pnDealId", iDealId); 
            dbPar[1] = new OleDbParameter("@pcXmlData", xmlData);
            return this._cQuery.ExecProc("dbo.TRSPopulateCheckUpdateSourceFund", ref dbPar, out dsOut); 
        }
        //20180504, vanny_w, BOSIT17195, begin
        public bool CheckHolidayDate(string strCcyPair, string strMatureDate, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2]; 
            dbPar[0] = new OleDbParameter("@pdBaseDate", strMatureDate) ; 
            dbPar[1] = new OleDbParameter("@pcCcy", strCcyPair) ;
            return this._cQuery.ExecProc("dbo.DCRCheckMXWorkingDate", ref dbPar, out dsOut); 
        }
        //20180504, vanny_w, BOSIT17195, end

        //20180706, vanny_w, BOSIT17195, begin
        public bool CheckParamThreshold(string strParam, string strCCYPair, string strBuySell, string strWarganegara, string strValue, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[5] ; 
            dbPar[0] = new OleDbParameter("@pcParam", strParam) ;
            dbPar[1] = new OleDbParameter("@pcCCYPair", strCCYPair) ;
            dbPar[2] = new OleDbParameter("@pcPosisi", strBuySell) ;
            dbPar[3] = new OleDbParameter("@pcWarganegara", strWarganegara) ;
            dbPar[4] = new OleDbParameter("@pcType", strValue) ;
            return this._cQuery.ExecProc("dbo.TRSCheckParamThreshold", ref dbPar, out dsOut); 
        }
        //20180706, vanny_w, BOSIT17195, end

        //20181220, vanny_w, TRBST16233, begin
        public bool CheckRiskProfile(string strCIF, out DataSet dsOut)
        {
            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcCIF", strCIF);
            return this._cQuery.ExecProc("dbo.TRSPopulateRiskProfileNasabah", ref dbParams, out dsOut);
        }
        //20181220, vanny_w, TRBST16233, end

        //20190617, rezakahfi, BOSOD19159, begin
        public bool isHavingPhoneOrder(string strCIF)
        {
            bool isHavingPhoneOrder = false;
            bool isPremier = false;

            System.Data.OleDb.OleDbParameter[] dbPar = new System.Data.OleDb.OleDbParameter[4];
            dbPar[0] = new System.Data.OleDb.OleDbParameter("@pcCIF", strCIF);
            dbPar[1] = new System.Data.OleDb.OleDbParameter("@pbPBFlag", System.Data.OleDb.OleDbType.Boolean);
            dbPar[1].Direction = System.Data.ParameterDirection.Output;
            dbPar[2] = new System.Data.OleDb.OleDbParameter("@pbPhoneOrderFlag", System.Data.OleDb.OleDbType.Boolean);
            dbPar[2].Direction = System.Data.ParameterDirection.Output;
            dbPar[3] = new System.Data.OleDb.OleDbParameter("@pcPBFaciltyType", "FXNTD");

            bool bOK = clsGlobal.QueryCIF.ExecProc("dbo.TRSCheckPhoneOrder", ref dbPar);
            if (bOK)
            {
                isPremier = bool.Parse(dbPar[1].Value.ToString());
                isHavingPhoneOrder = bool.Parse(dbPar[2].Value.ToString());
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Failed Get Phone Order Data", "Error");
            }

            return isHavingPhoneOrder;
        }
        //20190617, rezakahfi, BOSOD19159, end
        #endregion

        //20190326, vanny_w, BOSOD18243, begin
        #region PopulateSourceFund
        public DataTable getDataSourceFund(string Source, string cif, string valueDate, string currency, string Filter, string strFilter2)
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
                dbParams[5] = new OleDbParameter("@pcFilter2", strFilter2);

                if (this._cQuery.ExecProc("dbo.TRSPopulateSourceOfFund", ref dbParams, out dsOut))
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

        public bool PopulateDataNasabah(string strParamType, string strProduct, string strCIF, string strCIF2, string strFilter, out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[5];
                dbParams[0] = new OleDbParameter("@pcTypePopulate", strParamType);
                dbParams[1] = new OleDbParameter("@pcProduct", strProduct);
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
        #endregion
        //20190326, vanny_w, BOSOD18243, end

        #region "Populate Data Approval dan Data Cancel"

        public bool PopulateDataTrx(string strParam, int iParam1, string strParam2, out DataSet dsOut)
            {
                OleDbParameter[] dbPar = new OleDbParameter[3];
                //20180305, vanny_w, BOSIT17195, begin
                dbPar[0] = new OleDbParameter("@pcParam", strParam);
                dbPar[1] = new OleDbParameter("@pnParam1", iParam1);      
                dbPar[2] = new OleDbParameter("@pcParam2", strParam2);
                //20180305, vanny_w, BOSIT17195, end
                return this._cQuery.ExecProc("dbo.TRSPopulateDataTrxNTD", ref dbPar, out dsOut); 
            }

            public bool PopulateParamTIBCO(string strParamType, string Status, string strFilter, out DataSet dsOut)
            {
                dsOut = new DataSet();

                OleDbParameter[] dbParams = new OleDbParameter[5];

                dbParams[0] = new OleDbParameter("@pcParamType", strParamType);
                dbParams[1] = new OleDbParameter("@pnStatus", Status);
                dbParams[2] = new OleDbParameter("@pcFilter", strFilter);
                dbParams[3] = new OleDbParameter("@pnNIK", this._userNIK);
                dbParams[4] = new OleDbParameter("@pcBranch", this._userBranch);

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

        #endregion

        #region "Populate Blotter"
            //20180314, vanny_w, BOSIT17195, begin
            public bool PopulateDataBlotter(bool isTA, string Param1, string Param2, string Param3, string Param4, string Param5, 
                string Param6, string Param7, string Param8, out DataSet dsOut)
            {
                OleDbParameter[] dbPar = new OleDbParameter[9];
                //20180305, vanny_w, BOSIT17195, begin
                dbPar[0] = new OleDbParameter("@pbTA", isTA);
                dbPar[1] = new OleDbParameter("@pcParam1", Param1);
                dbPar[2] = new OleDbParameter("@pcParam2", Param2);
                dbPar[3] = new OleDbParameter("@pcParam3", Param3);
                dbPar[4] = new OleDbParameter("@pcParam4", Param4);
                dbPar[5] = new OleDbParameter("@pcParam5", Param5);
                dbPar[6] = new OleDbParameter("@pcParam6", Param6);
                dbPar[7] = new OleDbParameter("@pcParam7", Param7);
                dbPar[8] = new OleDbParameter("@pcParam8", Param8);
                //20180305, vanny_w, BOSIT17195, end
                return this._cQuery.ExecProc("dbo.TRSPopulateDataBlotterNTD", ref dbPar, out dsOut); 
            }
        //20180314, vanny_w, BOSIT17195, end

        #endregion
    }
}
