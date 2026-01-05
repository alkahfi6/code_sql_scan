using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace BankNISP.Obligasi01
{
    public class FLDModel
    {
        private ObligasiQuery _cQuery;
        private int _userNIK;
        private string _userBranch;

        public FLDModel(ObligasiQuery cQuery, int userNIK, string userBranch)
        {
            this._cQuery = cQuery;
            this._userNIK = userNIK;
            this._userBranch = userBranch;
        }

        #region Approval
        public bool PopulateParameterFLD(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSPopulateFLDParamter", out dsOut);


        }

        public bool PopulateApprovalParamFLD(string pcJenisParam, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pcJenisParam", pcJenisParam);

                blnResult = this._cQuery.ExecProc("TRSPopulateApprovalParamFLD", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveApprovalParamFLD(string xmlData, int prosesNIK, string jensiParam, string statusApproval)
        {
            bool blnResult = false;

            try
            {

                OleDbParameter[] oParam = new OleDbParameter[4];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnProcessNIK", prosesNIK);
                oParam[2] = new OleDbParameter("@pcJenisParam", jensiParam);
                oParam[3] = new OleDbParameter("@pcStatusApproval", statusApproval);

                blnResult = this._cQuery.ExecProc("TRSApprovalParamFLD", ref oParam);



            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        #endregion


        #region TAParameter
        public bool PopulateTAParameter(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSPopulateTAParameter", out dsOut);

        }

        //20180116, vanny olivia, BOSIT17195, start
        public bool PopulatePortfolio(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("dbo.TRSPopulatePortfolio", out dsOut);
        }

        //20180116, vanny olivia, BOSIT17195, end
        //20180122, vanny olivia, BOSIT17195, begin
        public bool PopulateParamPortfolio(string strParam, string strParam1, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcParam", strParam);
            dbPar[1] = new OleDbParameter("@pcParam1", strParam1);
            return this._cQuery.ExecProc("dbo.TRSPopulateParamPortfolio", ref dbPar, out dsOut);
        }
        //20180122, vanny olivia, BOSIT17195, end
        public bool SaveParamTA(string xmlData, int userNik)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnUserNik", userNik);


                blnResult = this._cQuery.ExecProc("TRSParamSaveTA", ref oParam);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveParamPortfolio(string xmlData, int userNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlData);
            dbPar[1] = new OleDbParameter("@pnNIK", userNIK);
            return this._cQuery.ExecProc("dbo.TRSSaveParamPortfolio", ref dbPar);
        }
        #endregion

        #region GNCParameter
        //TRBST15111, reza, begin
        //public bool PopulateGNCParameter(out DataSet dsOut)
        public bool PopulateGNCParameter(string strProduct, out DataSet dsOut)
        //TRBST15111, reza, end
        {

            dsOut = new DataSet();
            //TRBST15111, reza, begin
            OleDbParameter[] oParam = new OleDbParameter[1];
            oParam[0] = new OleDbParameter("@pcProduct", strProduct);
            //TRBST15111, reza, end

            //TRBST15111, reza, begin
            //return this._cQuery.ExecProc("TRSPopulateGNCParameter", out dsOut);
            return this._cQuery.ExecProc("TRSPopulateGNCParameter", ref oParam, out dsOut);
            //TRBST15111, reza, begin
        }

        //TRBST15111, reza, begin
        //public bool SaveParamGNC(string xmlData, int userNik)
        public bool SaveParamGNC(string strProduct, string xmlData, int userNik)
        //TRBST15111, reza, end
        {
            bool blnResult = false;

            try
            {
                //TRBST15111, reza, begin
                //OleDbParameter[] oParam = new OleDbParameter[2];
                OleDbParameter[] oParam = new OleDbParameter[3];
                //TRBST15111, reza, end
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnUserNik", userNik);
                //TRBST15111, reza, begin
                oParam[2] = new OleDbParameter("@pcProduct", strProduct);
                //TRBST15111, reza, end

                blnResult = this._cQuery.ExecProc("TRSParamSaveGNC", ref oParam);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //TRBST15111, reza, begin
        public bool PopulateDataProduct(out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[3];
            dbParams[0] = new OleDbParameter("@pcParamType", "Product");
            dbParams[1] = new OleDbParameter("@pnStatus", 1);
            dbParams[2] = new OleDbParameter("@pcFilter", "");

            return this._cQuery.ExecProc("dbo.FMCT_PopulateParam", ref dbParams, out dsOut);
        }
        //TRBST15111, reza, end

        public bool CheckCurrency(string cCcy, string cCode, out DataSet dsResult)
        {
            bool blnResult = false;
            dsResult = new DataSet();
            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcCcy", cCcy);
                oParam[1] = new OleDbParameter("@pcCode", cCode);


                blnResult = this._cQuery.ExecProc("TRSCheckCurrency", ref oParam, out dsResult);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        #endregion

        #region BungaFTP
        //20190207, samypasha, BOSOD18243, begin
        //public bool PopulateBungaFTPParameter(out DataSet dsOut)
        //{

        //    dsOut = new DataSet();

        //    return this._cQuery.ExecProc("TRSPopulateBungaFTPParameter", out dsOut);

        //}
        public bool PopulateBungaFTPParameter(string cCounterCcy, out DataSet dsOut)
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pcCounterCurrency", cCounterCcy);

                blnResult = this._cQuery.ExecProc("RPAPopulateParameterBungaFTP", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;

        }
        //20190207, samypasha, BOSOD18243, end

        public bool SaveParamBungaFTP(string xmlData, int userNik)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnUserNik", userNik);

                //20190207, samypasha, BOSOD18243, begin
                //blnResult = this._cQuery.ExecProc("TRSParamSaveBungaFTP", ref oParam);
                blnResult = this._cQuery.ExecProc("RPAParamSaveBungaFTP", ref oParam);
                //20190207, samypasha, BOSOD18243, end


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool PopulateCurrencyParameter(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSPopulateCurrency", out dsOut);

        }

        //20180228, samypasha, BOSIT18017, begin
        public bool PopulateCcyFromCounter(string cCounterCcy, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pcCounterCcy", cCounterCcy);

                blnResult = this._cQuery.ExecProc("FLDPopulateCurrencyParamBungaFTP", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20180228, samypasha, BOSIT18017, end
        #endregion

        #region Cluster
        public bool PopulateClusterParameter(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSPopulateClusterParameter", out dsOut);

        }

        public bool SaveParamCluster(string xmlData, int userNik)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnUserNik", userNik);


                blnResult = this._cQuery.ExecProc("TRSParamSaveCluster", ref oParam);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        #endregion

        #region Tenor
        public bool PopulateTenorParameter(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSPopulateTenorParameter", out dsOut);

        }

        public bool SaveParamTenor(string xmlData, int userNik)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnUserNik", userNik);


                blnResult = this._cQuery.ExecProc("TRSParamSaveTenor", ref oParam);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        #endregion

        #region Param FLD
        public bool PopulateJenisParameterFLD(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSPopulateComboUploadParamFLD", out dsOut);


        }

        //20180118, samypasha, BOSIT18017, begin
        //public bool PopulateUploadCcyPairFLD(string cValueDate, out DataSet dsOut)
        public bool PopulateUploadCcyPairFLD(string cValueDate, string cCounterCcy, out DataSet dsOut)
        //20180118, samypasha, BOSIT18017, end
        {

            dsOut = new DataSet();

            //20180118, samypasha, BOSIT18017, begin
            //OleDbParameter[] oParam = new OleDbParameter[1];
            OleDbParameter[] oParam = new OleDbParameter[2];
            //20180118, samypasha, BOSIT18017, end
            oParam[0] = new OleDbParameter("@pcValueDate", cValueDate);
            //20180118, samypasha, BOSIT18017, begin
            oParam[1] = new OleDbParameter("@pcFLDCounterCcy", cCounterCcy);
            //20180118, samypasha, BOSIT18017, end

            return this._cQuery.ExecProc("TRSPopulateCcyPairFLD", ref oParam, out dsOut);


        }

        public bool PopulateTemplateFLD(string JenisTemplate, out DataSet dsOut)
        {

            dsOut = new DataSet();
            OleDbParameter[] oParam = new OleDbParameter[1];
            oParam[0] = new OleDbParameter("@pcJenisTemplate", JenisTemplate);

            return this._cQuery.ExecProc("TRSPopulateTemplateFLD", ref oParam, out dsOut);

        }

        //20160404, samy, TRBST15137, begin
        //public bool SaveUploadParamFLD(string xmlData, string jenisParam, int userNik)
        public bool SaveUploadParamFLD(string xmlData, string jenisParam, int userNik, string strFLDCounterCcy)
        //20160404, samy, TRBST15137, end
        {
            bool blnResult = false;

            try
            {

                //20160404, samy, TRBST15137, begin
                //OleDbParameter[] oParam = new OleDbParameter[3];
                OleDbParameter[] oParam = new OleDbParameter[4];
                //20160404, samy, TRBST15137, end

                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnUserNik", userNik);
                oParam[2] = new OleDbParameter("@pcJenisParam", jenisParam);
                //20160404, samy, TRBST15137, begin
                oParam[3] = new OleDbParameter("@pcFLDCounterCcy", strFLDCounterCcy);
                //20160404, samy, TRBST15137, end

                blnResult = this._cQuery.ExecProc("TRSUploadParameterFLD", ref oParam);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        #endregion

        #region Master Nasabah
        public bool PopulateMasterNasabah(string cNoCIF, string cStatus, out DataSet dsOut)
        {

            dsOut = new DataSet();
            OleDbParameter[] oParam = new OleDbParameter[2];
            oParam[0] = new OleDbParameter("@pcCIFNo", cNoCIF);
            oParam[1] = new OleDbParameter("@pcStatus", cStatus);

            return this._cQuery.ExecProc("TRSPopulateMasterNasabahFLD", ref oParam, out dsOut);

        }
        #endregion

        #region Transaksi FLD
        public bool PopulateParamTransaksiFLD(string cCIF, string cParam, string cAccountRelasi, string cCcy, string cCcyPair, string cTenor, string cValueDate, string cNIK, out DataSet dsOut)
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[8];
                oParam[0] = new OleDbParameter("@pcCIFNo", cCIF);
                oParam[1] = new OleDbParameter("@pcParam", cParam);
                oParam[2] = new OleDbParameter("@pcAccountRelasi", cAccountRelasi);
                oParam[3] = new OleDbParameter("@pcCcy", cCcy);
                oParam[4] = new OleDbParameter("@pcCcyPair", cCcyPair);
                oParam[5] = new OleDbParameter("@pcTenor", cTenor);
                oParam[6] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[7] = new OleDbParameter("@pcNIK", cNIK);


                blnResult = this._cQuery.ExecProc("TRSPopulateDataTransaksiFLD", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;



        }

        //20170425, samypasha, TRBST15137, begin
        //public bool CalculateTransaksiFLD(decimal nPokokPenempatan, decimal nKursTengah, decimal nBungaFLD, int nDays, string cCcyPair, string cValueDate, string cTenor, out DataSet dsOut)
        //20180118, samypasha, BOSIT18017, begin
        //public bool CalculateTransaksiFLD(decimal nPokokPenempatan, decimal nKursTengah, decimal nBungaFLD, int nDays, string cCcyPair, string cValueDate, string cTenor, decimal nBungaDeposito, out DataSet dsOut)
        public bool CalculateTransaksiFLD(decimal nPokokPenempatan, decimal nKursTengah, decimal nBungaFLD, int nDays, string cCcyPair, string cValueDate, string cTenor, decimal nBungaDeposito, string cFLDCounterCcy, out DataSet dsOut)
        //20180118, samypasha, BOSIT18017, end
        //20170425, samypasha, TRBST15137, end
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                //20170425, samypasha, TRBST15137, begin
                //OleDbParameter[] oParam = new OleDbParameter[7];
                //20180118, samypasha, BOSIT18017, begin
                //OleDbParameter[] oParam = new OleDbParameter[8];
                OleDbParameter[] oParam = new OleDbParameter[9];
                //20180118, samypasha, BOSIT18017, end
                //20170425, samypasha, TRBST15137, end
                oParam[0] = new OleDbParameter("@pnPokokPenempatan", nPokokPenempatan);
                oParam[1] = new OleDbParameter("@pnKursTengah", nKursTengah);
                oParam[2] = new OleDbParameter("@pnBungaFLD", nBungaFLD);
                oParam[3] = new OleDbParameter("@pnDays", nDays);
                oParam[4] = new OleDbParameter("@pcCcyPair", cCcyPair);
                oParam[5] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[6] = new OleDbParameter("@pcTenor", cTenor);
                //20170425, samypasha, TRBST15137, begin
                oParam[7] = new OleDbParameter("@pnBungaDeposito", nBungaDeposito);
                //20170425, samypasha, TRBST15137, end
                //20180118, samypasha, BOSIT18017, begin
                oParam[8] = new OleDbParameter("@pcFLDCounterCcy", cFLDCounterCcy);
                //20180118, samypasha, BOSIT18017, end


                blnResult = this._cQuery.ExecProc("TRSCalculateFLDTransaction", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;



        }

        //20190723, rezakahfi, LOGAM10236, end
        public bool ValidateField(string CcyPair
                                    , string ValueDate
                                    , string Tenor
                                    , int nDaysTenor
                                    , string CounterCcy
                                    , decimal SwapPoint, decimal BungaFLD, decimal BungaDeposito, out DataSet dsOut)
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[8];

                oParam[0] = new OleDbParameter("@pcCcyPair", CcyPair);
                oParam[1] = new OleDbParameter("@pcValueDate", ValueDate);
                oParam[2] = new OleDbParameter("@pcTenor", Tenor);
                oParam[3] = new OleDbParameter("@pnDaysTenor", nDaysTenor);
                oParam[4] = new OleDbParameter("@pcFLDCounterCcy", CounterCcy);
                ////
                oParam[5] = new OleDbParameter("@pnSwapPointInput", SwapPoint);
                oParam[6] = new OleDbParameter("@pnBungaFLDInput", BungaFLD);
                oParam[7] = new OleDbParameter("@pnBungaFTPInput", BungaDeposito);

                blnResult = this._cQuery.ExecProc("dbo.FLD_ValidateField", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20190723, rezakahfi, LOGAM10236, end

        public bool PopulateParamTransaksiFLDTA(string cCIF, string cParam, string cAccountRelasi, string cCcy, string cCcyPair, string cTenor, string cValueDate, string cNIK, string cTanggalTransaksi, out DataSet dsOut)
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[9];
                oParam[0] = new OleDbParameter("@pcCIFNo", cCIF);
                oParam[1] = new OleDbParameter("@pcParam", cParam);
                oParam[2] = new OleDbParameter("@pcAccountRelasi", cAccountRelasi);
                oParam[3] = new OleDbParameter("@pcCcy", cCcy);
                oParam[4] = new OleDbParameter("@pcCcyPair", cCcyPair);
                oParam[5] = new OleDbParameter("@pcTenor", cTenor);
                oParam[6] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[7] = new OleDbParameter("@pcNIK", cNIK);
                oParam[8] = new OleDbParameter("@pcTanggalTransaksi", cTanggalTransaksi);


                blnResult = this._cQuery.ExecProc("TRSPopulateDataTransaksiFLDTA", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //20180110, samypasha, BOSIT18017, begin
        //public bool CekTanggalFLD(string tanggalPenempatan, string currPair, string valueDate, out DataSet dsOut)
        //20230328, yazri, FLD-61, begin
        //public bool CekTanggalFLD(string tanggalPenempatan, string currPair, string valueDate, string cFLDCounterCcy, out DataSet dsOut)
        public bool CekTanggalFLD(string tanggalPenempatan, string currPair, string valueDate, string cFLDCounterCcy, string cTenor, out DataSet dsOut)
        //20230328, yazri, FLD-61, end
        //20180110, samypasha, BOSIT18017, end
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                //20180110, samypasha, BOSIT18017, begin
                //OleDbParameter[] oParam = new OleDbParameter[3];
                //20230328, yazri, FLD-61, begin
                //OleDbParameter[] oParam = new OleDbParameter[4];
                OleDbParameter[] oParam = new OleDbParameter[5];
                //20230328, yazri, FLD-61, end
                //20180110, samypasha, BOSIT18017, end
                oParam[0] = new OleDbParameter("@pcTanggalPenempatan", tanggalPenempatan);
                oParam[1] = new OleDbParameter("@pcCurrPair", currPair);
                oParam[2] = new OleDbParameter("@pcValueDate", valueDate);
                //20180110, samypasha, BOSIT18017, begin
                oParam[3] = new OleDbParameter("@pcFLDCounterCcy", cFLDCounterCcy);
                //20180110, samypasha, BOSIT18017, end
                //20230328, yazri, FLD-61, begin
                oParam[4] = new OleDbParameter("@pcTenor", cTenor);
                //20230328, yazri, FLD-61, end

                blnResult = this._cQuery.ExecProc("TRSCekTanggalPenempatanFLD", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20161117, samy, TRBST15137, begin
        //public bool CalculateTransaksiTAFLD(decimal nPokokPenempatan, decimal nKursTengah, decimal nBungaFLD, int nDays, string cCcyPair, string cValueDate, string cTenor, decimal nSwapPoint, out DataSet dsOut)
        //20180110, samypasha, BOSIT18017, begin
        //public bool CalculateTransaksiTAFLD(decimal nPokokPenempatan, decimal nKursTengah, decimal nBungaFLD, int nDays, string cCcyPair, string cValueDate, string cTenor, decimal nSwapPoint, bool bSpesial, decimal nBungaDeposito, out DataSet dsOut)
        public bool CalculateTransaksiTAFLD(decimal nPokokPenempatan, decimal nKursTengah, decimal nBungaFLD, int nDays, string cCcyPair, string cValueDate, string cTenor, decimal nSwapPoint, bool bSpesial, decimal nBungaDeposito, string cFLDCounterCcy, out DataSet dsOut)
        //20180110, samypasha, BOSIT18017, end
        //20161117, samy, TRBST15137, end
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                //20161117, samy, TRBST15137, begin
                //OleDbParameter[] oParam = new OleDbParameter[8];
                //20180110, samypasha, BOSIT18017, begin
                //OleDbParameter[] oParam = new OleDbParameter[10];
                OleDbParameter[] oParam = new OleDbParameter[11];
                //20180110, samypasha, BOSIT18017, end
                //20161117, samy, TRBST15137, end
                oParam[0] = new OleDbParameter("@pnPokokPenempatan", nPokokPenempatan);
                oParam[1] = new OleDbParameter("@pnKursTengah", nKursTengah);
                oParam[2] = new OleDbParameter("@pnBungaFLD", nBungaFLD);
                oParam[3] = new OleDbParameter("@pnDays", nDays);
                oParam[4] = new OleDbParameter("@pcCcyPair", cCcyPair);
                oParam[5] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[6] = new OleDbParameter("@pcTenor", cTenor);
                oParam[7] = new OleDbParameter("@pnSwap", nSwapPoint);
                //20161117, samy, TRBST15137, begin
                oParam[8] = new OleDbParameter("@pbSpecial", bSpesial);
                oParam[9] = new OleDbParameter("@pnBungaDeposito", nBungaDeposito);
                //20161117, samy, TRBST15137, end
                //20180110, samypasha, BOSIT18017, begin
                oParam[10] = new OleDbParameter("@pcFLDCounterCcy", cFLDCounterCcy);
                //20180110, samypasha, BOSIT18017, end

                blnResult = this._cQuery.ExecProc("TRSCalculateFLDTransactionTA", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SubmitTranFLD(int userNIK, string noCIF, string namaNasabah, string cAccountNo
            , string cAccountNoCcy, string cValueDate, string cCcyPair, decimal nPokokPenempatan
            , string cTenor, int nDays, decimal nKursTengah, decimal nBungaFLD, decimal nBungaFTP
            , string cSumberDana, string cBranch, string cBranchName, string cNIKSeller, string cNamaSeller
            , string cNoSertifikasi, decimal nSwapPoint, decimal nPremiSwap, decimal nSpreadSales
            , decimal nFLDForwardRateTrader, decimal nFLDForwardRateCustomer, decimal nFLDCounterCcy
            , string cFLDCounterCcyCurrency, decimal nCtrCcy, string cCtrCcyCurrency, decimal nBungaCtrDepo
            , decimal nPajakBungaCtrDepo, decimal nHasilPenempatanFLD, decimal nPajakHasilFLD, decimal nHasilFLDNett
            , decimal nBungaFLDNett, decimal nTrxFXForward, string cTrxFXForwardCurrency, decimal nSwapPointProfit
            , decimal nModal, decimal nDanaYangDiterimaNasabah, string cDanaYangDiterimaNasabahCurrency
            , decimal nProfitCabang, string cProfitCabangCurrency, string cTanggalTransaksi, string cTanggalPenempatan
            //20160413, samy, LOGEN00112, begin
            //, string cTanggalJatuhTempo, string cTAName, bool bIsBackdate, decimal nBungaDeposito, string cCIF2, int? nDealId, string cActionCode, out DataSet dsOut)
            , string cTanggalJatuhTempo, string cTAName, bool bIsBackdate, decimal nBungaDeposito, string cCIF2
            , int? nDealId, string cActionCode, int nNIKReffernator
            //20160420, samypasha, TRBST15137, begin
            , bool bPhoneOrder, string cXMLData, bool isSpecial
            //20160420, samypasha, TRBST15137, end
            //20241202, mustafa.noya, ANT-455, begin
            , string cCIFPelaku
            //20241202, mustafa.noya, ANT-455, end
            , out DataSet dsOut)
        //20160413, samy, LOGEN00112, end
        {
            bool blnResult = false;
            dsOut = new DataSet();
            try
            {
                //20160413, samy, LOGEN00112, begin
                //OleDbParameter[] oParam = new OleDbParameter[51];
                //20160420, samypasha, TRBST15137, begin
                //OleDbParameter[] oParam = new OleDbParameter[52];
                //OleDbParameter[] oParam = new OleDbParameter[55];
                //20160420, samypasha, TRBST15137, end
                //20241202, mustafa.noya, ANT-455, begin
                OleDbParameter[] oParam = new OleDbParameter[56];
                //20241202, mustafa.noya, ANT-455, end
                //20160413, samy, LOGEN00112, end

                oParam[0] = new OleDbParameter("@pnUserNik", userNIK);
                oParam[1] = new OleDbParameter("@pcCIF", noCIF);
                oParam[2] = new OleDbParameter("@pcNamaNasabah", namaNasabah);
                oParam[3] = new OleDbParameter("@pcAccountNo", cAccountNo);
                oParam[4] = new OleDbParameter("@pcAccountNoCcy", cAccountNoCcy);
                oParam[5] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[6] = new OleDbParameter("@pcCcyPair", cCcyPair);
                oParam[7] = new OleDbParameter("@pnPokokPenempatan", nPokokPenempatan);
                oParam[8] = new OleDbParameter("@pcTenor", cTenor);
                oParam[9] = new OleDbParameter("@pnDays", nDays);
                oParam[10] = new OleDbParameter("@pnKursTengahONFX", nKursTengah);
                oParam[11] = new OleDbParameter("@pnBungaFLD", nBungaFLD);
                oParam[12] = new OleDbParameter("@pnBungaFTP", nBungaFTP);
                oParam[13] = new OleDbParameter("@pcSumberDana", cSumberDana);
                oParam[14] = new OleDbParameter("@pcBranch", cBranch);
                oParam[15] = new OleDbParameter("@pcBranchName", cBranchName);
                oParam[16] = new OleDbParameter("@pcNIKSeller", cNIKSeller);
                oParam[17] = new OleDbParameter("@pcNamaSeller", cNamaSeller);
                oParam[18] = new OleDbParameter("@pcNoSertifikasi", cNoSertifikasi);
                oParam[19] = new OleDbParameter("@pnSwapPoint", nSwapPoint);
                oParam[20] = new OleDbParameter("@pnPremiSwap", nPremiSwap);
                oParam[21] = new OleDbParameter("@pnSpreadSales", nSpreadSales);
                oParam[22] = new OleDbParameter("@pnFLDForwardRateTrader", nFLDForwardRateTrader);
                oParam[23] = new OleDbParameter("@pnFLDForwardRateCustomer", nFLDForwardRateCustomer);
                oParam[24] = new OleDbParameter("@pnFLDCounterCcy", nFLDCounterCcy);
                oParam[25] = new OleDbParameter("@pcFLDCounterCcyCurrency", cFLDCounterCcyCurrency);
                oParam[26] = new OleDbParameter("@pnCtrCcy", nCtrCcy);
                oParam[27] = new OleDbParameter("@pcCtrCcyCurrency", cCtrCcyCurrency);
                oParam[28] = new OleDbParameter("@pnBungaCtrDepo", nBungaCtrDepo);
                oParam[29] = new OleDbParameter("@pnPajakBungaCtrDepo", nPajakBungaCtrDepo);
                oParam[30] = new OleDbParameter("@pnHasilPenempatanFLD", nHasilPenempatanFLD);
                oParam[31] = new OleDbParameter("@pnPajakHasilFLD", nPajakHasilFLD);
                oParam[32] = new OleDbParameter("@pnHasilFLDNett", nHasilFLDNett);
                oParam[33] = new OleDbParameter("@pnBungaFLDNett", nBungaFLDNett);
                oParam[34] = new OleDbParameter("@pnTrxFXForward", nTrxFXForward);
                oParam[35] = new OleDbParameter("@pcTrxFXForwardCurrency", cTrxFXForwardCurrency);
                oParam[36] = new OleDbParameter("@pnSwapPointProfit", nSwapPointProfit);
                oParam[37] = new OleDbParameter("@pnModal", nModal);
                oParam[38] = new OleDbParameter("@pnDanaYangDiterimaNasabah", nDanaYangDiterimaNasabah);
                oParam[39] = new OleDbParameter("@pcDanaYangDiterimaNasabahCurrency", cDanaYangDiterimaNasabahCurrency);
                oParam[40] = new OleDbParameter("@pnProfitCabang", nProfitCabang);
                oParam[41] = new OleDbParameter("@pcProfitCabangCurrency", cProfitCabangCurrency);
                oParam[42] = new OleDbParameter("@pcTanggalTransaksi", cTanggalTransaksi);
                oParam[43] = new OleDbParameter("@pcTanggalPenempatan", cTanggalPenempatan);
                oParam[44] = new OleDbParameter("@pcTanggalJatuhTempo", cTanggalJatuhTempo);
                oParam[45] = new OleDbParameter("@pcTAName", cTAName);
                oParam[46] = new OleDbParameter("@pcIsBackDate", bIsBackdate);
                oParam[47] = new OleDbParameter("@pnBungaDeposito", nBungaDeposito);
                oParam[48] = new OleDbParameter("@pcCIF2", cCIF2);
                oParam[49] = new OleDbParameter("@pnDealId", nDealId);
                oParam[50] = new OleDbParameter("@pcActionCode", cActionCode);
                //20160413, samy, LOGEN00112, begin
                oParam[51] = new OleDbParameter("@pnNikRefferantor", nNIKReffernator);
                //20160413, samy, LOGEN00112, end
                //20160420, samypasha, TRBST15137, begin
                oParam[52] = new OleDbParameter("@pbPhoneOrder", bPhoneOrder);
                oParam[53] = new OleDbParameter("@pcXmlData", cXMLData);
                oParam[54] = new OleDbParameter("@pbSpecial", isSpecial);
                //20160420, samypasha, TRBST15137, end
                //20241202, mustafa.noya, ANT-ANT-455, begin
                oParam[55] = new OleDbParameter("@pcCIFPelaku", cCIFPelaku);
                //20241202, mustafa.noya, ANT-ANT-455, end

                blnResult = this._cQuery.ExecProc("TRSSubmitTransaksiFLD", ref oParam, out dsOut);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveTranFLD(int userNIK, string noCIF, string namaNasabah, string cAccountNo
            , string cAccountNoCcy, string cValueDate, string cCcyPair, decimal nPokokPenempatan
            , string cTenor, int nDays, decimal nKursTengah, decimal nBungaFLD, decimal nBungaFTP
            , string cSumberDana, string cBranch, string cBranchName, string cNIKSeller, string cNamaSeller
            , string cNoSertifikasi, decimal nSwapPoint, decimal nPremiSwap, decimal nSpreadSales
            , decimal nFLDForwardRateTrader, decimal nFLDForwardRateCustomer, decimal nFLDCounterCcy
            , string cFLDCounterCcyCurrency, decimal nCtrCcy, string cCtrCcyCurrency, decimal nBungaCtrDepo
            , decimal nPajakBungaCtrDepo, decimal nHasilPenempatanFLD, decimal nPajakHasilFLD, decimal nHasilFLDNett
            , decimal nBungaFLDNett, decimal nTrxFXForward, string cTrxFXForwardCurrency, decimal nSwapPointProfit
            , decimal nModal, decimal nDanaYangDiterimaNasabah, string cDanaYangDiterimaNasabahCurrency
            , decimal nProfitCabang, string cProfitCabangCurrency, string cTanggalTransaksi, string cTanggalPenempatan
            //20160413, samy, LOGEN00112, begin
            //,string cTanggalJatuhTempo,string cTAName, bool bIsBackdate, decimal nBungaDeposito, string cCIF2, out DataSet dsOut)
            , string cTanggalJatuhTempo, string cTAName, bool bIsBackdate, decimal nBungaDeposito, string cCIF2, int nNikRefferantor
            //20160420, samypasha, TRBST15137, begin
            , bool bPhoneOrder, string xmlData, bool isSpecial
            //20160420, samypasha, TRBST15137, end
            //20241202, mustafa.noya, ANT-455, begin
            , string cCIFPelaku
            //20241202, mustafa.noya, ANT-455, end

            , out DataSet dsOut)
        //20160413, samy, LOGEN00112, end
        {
            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                //20160413, samy, LOGEN00112, begin
                //OleDbParameter[] oParam = new OleDbParameter[49];
                //20160420, samypasha, TRBST15137, begin
                //OleDbParameter[] oParam = new OleDbParameter[50];
                //OleDbParameter[] oParam = new OleDbParameter[53];
                //20160420, samypasha, TRBST15137, end
                //20241204, mustafa.noya, ANT-455, begin
                OleDbParameter[] oParam = new OleDbParameter[54];
                //20241204, mustafa.noya, ANT-455, end
                //20160413, samy, LOGEN00112, end

                oParam[0] = new OleDbParameter("@pnUserNik", userNIK);
                oParam[1] = new OleDbParameter("@pcCIF", noCIF);
                oParam[2] = new OleDbParameter("@pcNamaNasabah", namaNasabah);
                oParam[3] = new OleDbParameter("@pcAccountNo", cAccountNo);
                oParam[4] = new OleDbParameter("@pcAccountNoCcy", cAccountNoCcy);
                oParam[5] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[6] = new OleDbParameter("@pcCcyPair", cCcyPair);
                oParam[7] = new OleDbParameter("@pnPokokPenempatan", nPokokPenempatan);
                oParam[8] = new OleDbParameter("@pcTenor", cTenor);
                oParam[9] = new OleDbParameter("@pnDays", nDays);
                oParam[10] = new OleDbParameter("@pnKursTengahONFX", nKursTengah);
                oParam[11] = new OleDbParameter("@pnBungaFLD", nBungaFLD);
                oParam[12] = new OleDbParameter("@pnBungaFTP", nBungaFTP);
                oParam[13] = new OleDbParameter("@pcSumberDana", cSumberDana);
                oParam[14] = new OleDbParameter("@pcBranch", cBranch);
                oParam[15] = new OleDbParameter("@pcBranchName", cBranchName);
                oParam[16] = new OleDbParameter("@pcNIKSeller", cNIKSeller);
                oParam[17] = new OleDbParameter("@pcNamaSeller", cNamaSeller);
                oParam[18] = new OleDbParameter("@pcNoSertifikasi", cNoSertifikasi);
                oParam[19] = new OleDbParameter("@pnSwapPoint", nSwapPoint);
                oParam[20] = new OleDbParameter("@pnPremiSwap", nPremiSwap);
                oParam[21] = new OleDbParameter("@pnSpreadSales", nSpreadSales);
                oParam[22] = new OleDbParameter("@pnFLDForwardRateTrader", nFLDForwardRateTrader);
                oParam[23] = new OleDbParameter("@pnFLDForwardRateCustomer", nFLDForwardRateCustomer);
                oParam[24] = new OleDbParameter("@pnFLDCounterCcy", nFLDCounterCcy);
                oParam[25] = new OleDbParameter("@pcFLDCounterCcyCurrency", cFLDCounterCcyCurrency);
                oParam[26] = new OleDbParameter("@pnCtrCcy", nCtrCcy);
                oParam[27] = new OleDbParameter("@pcCtrCcyCurrency", cCtrCcyCurrency);
                oParam[28] = new OleDbParameter("@pnBungaCtrDepo", nBungaCtrDepo);
                oParam[29] = new OleDbParameter("@pnPajakBungaCtrDepo", nPajakBungaCtrDepo);
                oParam[30] = new OleDbParameter("@pnHasilPenempatanFLD", nHasilPenempatanFLD);
                oParam[31] = new OleDbParameter("@pnPajakHasilFLD", nPajakHasilFLD);
                oParam[32] = new OleDbParameter("@pnHasilFLDNett", nHasilFLDNett);
                oParam[33] = new OleDbParameter("@pnBungaFLDNett", nBungaFLDNett);
                oParam[34] = new OleDbParameter("@pnTrxFXForward", nTrxFXForward);
                oParam[35] = new OleDbParameter("@pcTrxFXForwardCurrency", cTrxFXForwardCurrency);
                oParam[36] = new OleDbParameter("@pnSwapPointProfit", nSwapPointProfit);
                oParam[37] = new OleDbParameter("@pnModal", nModal);
                oParam[38] = new OleDbParameter("@pnDanaYangDiterimaNasabah", nDanaYangDiterimaNasabah);
                oParam[39] = new OleDbParameter("@pcDanaYangDiterimaNasabahCurrency", cDanaYangDiterimaNasabahCurrency);
                oParam[40] = new OleDbParameter("@pnProfitCabang", nProfitCabang);
                oParam[41] = new OleDbParameter("@pcProfitCabangCurrency", cProfitCabangCurrency);
                oParam[42] = new OleDbParameter("@pcTanggalTransaksi", cTanggalTransaksi);
                oParam[43] = new OleDbParameter("@pcTanggalPenempatan", cTanggalPenempatan);
                oParam[44] = new OleDbParameter("@pcTanggalJatuhTempo", cTanggalJatuhTempo);
                oParam[45] = new OleDbParameter("@pcTAName", cTAName);
                oParam[46] = new OleDbParameter("@pcIsBackDate", bIsBackdate);
                oParam[47] = new OleDbParameter("@pnBungaDeposito", nBungaDeposito);
                oParam[48] = new OleDbParameter("@pcCIF2", cCIF2);
                //20160413, samy, LOGEN00112, begin
                oParam[49] = new OleDbParameter("@pnNikRefferantor", nNikRefferantor);
                //20160413, samy, LOGEN00112, end
                //20160420, samypasha, TRBST15137, begin
                oParam[50] = new OleDbParameter("@pbPhoneOrder", bPhoneOrder);
                oParam[51] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[52] = new OleDbParameter("@pbSpecial", isSpecial);
                //20160420, samypasha, TRBST15137, end
                //20241202, mustafa.noya, ANT-ANT-455, begin
                oParam[53] = new OleDbParameter("@pcCIFPelaku", cCIFPelaku);
                //20241202, mustafa.noya, ANT-ANT-455, end


                blnResult = this._cQuery.ExecProc("TRSSaveTransaksiFLD", ref oParam, out dsOut);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool UpdateTranFLD(int userNIK, string noCIF, string namaNasabah, string cAccountNo
            , string cAccountNoCcy, string cValueDate, string cCcyPair, decimal nPokokPenempatan
            , string cTenor, int nDays, decimal nKursTengah, decimal nBungaFLD, decimal nBungaFTP
            , string cSumberDana, string cBranch, string cBranchName, string cNIKSeller, string cNamaSeller
            , string cNoSertifikasi, decimal nSwapPoint, decimal nPremiSwap, decimal nSpreadSales
            , decimal nFLDForwardRateTrader, decimal nFLDForwardRateCustomer, decimal nFLDCounterCcy
            , string cFLDCounterCcyCurrency, decimal nCtrCcy, string cCtrCcyCurrency, decimal nBungaCtrDepo
            , decimal nPajakBungaCtrDepo, decimal nHasilPenempatanFLD, decimal nPajakHasilFLD, decimal nHasilFLDNett
            , decimal nBungaFLDNett, decimal nTrxFXForward, string cTrxFXForwardCurrency, decimal nSwapPointProfit
            , decimal nModal, decimal nDanaYangDiterimaNasabah, string cDanaYangDiterimaNasabahCurrency
            , decimal nProfitCabang, string cProfitCabangCurrency, string cTanggalTransaksi, string cTanggalPenempatan
            //20160413, samy, LOGEN00112, begin
            //, string cTanggalJatuhTempo, string cTAName, bool bIsBackdate, decimal nBungaDeposito, string cCIF2,int nDealId, out DataSet dsOut)
            , string cTanggalJatuhTempo, string cTAName, bool bIsBackdate, decimal nBungaDeposito, string cCIF2, int nDealId, int nNIKRefferantor
            //20160420, samypasha, TRBST15137, begin
            , bool bPhoneOrder, string cXMLData, bool isSpecial
            //20160420, samypasha, TRBST15137, end
            //20241202, mustafa.noya, ANT-455, begin
            , string cCIFPelaku
            //20241202, mustafa.noya, ANT-455, end
            , out DataSet dsOut)
        //20160413, samy, LOGEN00112, end
        {
            bool blnResult = false;
            dsOut = new DataSet();

            try
            {

                //20160413, samy, LOGEN00112, begin
                //OleDbParameter[] oParam = new OleDbParameter[50];
                //20160420, samypasha, TRBST15137, begin
                //OleDbParameter[] oParam = new OleDbParameter[51];
                //OleDbParameter[] oParam = new OleDbParameter[54];
                //20160420, samypasha, TRBST15137, end
                //20241202, mustafa.noya, ANT-455, begin
                OleDbParameter[] oParam = new OleDbParameter[55];
                //20241202, mustafa.noya, ANT-455, end
                //20160413, samy, LOGEN00112, end

                oParam[0] = new OleDbParameter("@pnUserNik", userNIK);
                oParam[1] = new OleDbParameter("@pcCIF", noCIF);
                oParam[2] = new OleDbParameter("@pcNamaNasabah", namaNasabah);
                oParam[3] = new OleDbParameter("@pcAccountNo", cAccountNo);
                oParam[4] = new OleDbParameter("@pcAccountNoCcy", cAccountNoCcy);
                oParam[5] = new OleDbParameter("@pcValueDate", cValueDate);
                oParam[6] = new OleDbParameter("@pcCcyPair", cCcyPair);
                oParam[7] = new OleDbParameter("@pnPokokPenempatan", nPokokPenempatan);
                oParam[8] = new OleDbParameter("@pcTenor", cTenor);
                oParam[9] = new OleDbParameter("@pnDays", nDays);
                oParam[10] = new OleDbParameter("@pnKursTengahONFX", nKursTengah);
                oParam[11] = new OleDbParameter("@pnBungaFLD", nBungaFLD);
                oParam[12] = new OleDbParameter("@pnBungaFTP", nBungaFTP);
                oParam[13] = new OleDbParameter("@pcSumberDana", cSumberDana);
                oParam[14] = new OleDbParameter("@pcBranch", cBranch);
                oParam[15] = new OleDbParameter("@pcBranchName", cBranchName);
                oParam[16] = new OleDbParameter("@pcNIKSeller", cNIKSeller);
                oParam[17] = new OleDbParameter("@pcNamaSeller", cNamaSeller);
                oParam[18] = new OleDbParameter("@pcNoSertifikasi", cNoSertifikasi);
                oParam[19] = new OleDbParameter("@pnSwapPoint", nSwapPoint);
                oParam[20] = new OleDbParameter("@pnPremiSwap", nPremiSwap);
                oParam[21] = new OleDbParameter("@pnSpreadSales", nSpreadSales);
                oParam[22] = new OleDbParameter("@pnFLDForwardRateTrader", nFLDForwardRateTrader);
                oParam[23] = new OleDbParameter("@pnFLDForwardRateCustomer", nFLDForwardRateCustomer);
                oParam[24] = new OleDbParameter("@pnFLDCounterCcy", nFLDCounterCcy);
                oParam[25] = new OleDbParameter("@pcFLDCounterCcyCurrency", cFLDCounterCcyCurrency);
                oParam[26] = new OleDbParameter("@pnCtrCcy", nCtrCcy);
                oParam[27] = new OleDbParameter("@pcCtrCcyCurrency", cCtrCcyCurrency);
                oParam[28] = new OleDbParameter("@pnBungaCtrDepo", nBungaCtrDepo);
                oParam[29] = new OleDbParameter("@pnPajakBungaCtrDepo", nPajakBungaCtrDepo);
                oParam[30] = new OleDbParameter("@pnHasilPenempatanFLD", nHasilPenempatanFLD);
                oParam[31] = new OleDbParameter("@pnPajakHasilFLD", nPajakHasilFLD);
                oParam[32] = new OleDbParameter("@pnHasilFLDNett", nHasilFLDNett);
                oParam[33] = new OleDbParameter("@pnBungaFLDNett", nBungaFLDNett);
                oParam[34] = new OleDbParameter("@pnTrxFXForward", nTrxFXForward);
                oParam[35] = new OleDbParameter("@pcTrxFXForwardCurrency", cTrxFXForwardCurrency);
                oParam[36] = new OleDbParameter("@pnSwapPointProfit", nSwapPointProfit);
                oParam[37] = new OleDbParameter("@pnModal", nModal);
                oParam[38] = new OleDbParameter("@pnDanaYangDiterimaNasabah", nDanaYangDiterimaNasabah);
                oParam[39] = new OleDbParameter("@pcDanaYangDiterimaNasabahCurrency", cDanaYangDiterimaNasabahCurrency);
                oParam[40] = new OleDbParameter("@pnProfitCabang", nProfitCabang);
                oParam[41] = new OleDbParameter("@pcProfitCabangCurrency", cProfitCabangCurrency);
                oParam[42] = new OleDbParameter("@pcTanggalTransaksi", cTanggalTransaksi);
                oParam[43] = new OleDbParameter("@pcTanggalPenempatan", cTanggalPenempatan);
                oParam[44] = new OleDbParameter("@pcTanggalJatuhTempo", cTanggalJatuhTempo);
                oParam[45] = new OleDbParameter("@pcTAName", cTAName);
                oParam[46] = new OleDbParameter("@pcIsBackDate", bIsBackdate);
                oParam[47] = new OleDbParameter("@pnBungaDeposito", nBungaDeposito);
                oParam[48] = new OleDbParameter("@pcCIF2", cCIF2);
                oParam[49] = new OleDbParameter("@pnDealId", nDealId);
                //20160413, samy, LOGEN00112, begin
                oParam[50] = new OleDbParameter("@pnNikRefferantor", nNIKRefferantor);
                //20160413, samy, LOGEN00112, end
                //20160420, samypasha, TRBST15137, begin
                oParam[51] = new OleDbParameter("@pbPhoneOrder", bPhoneOrder);
                oParam[52] = new OleDbParameter("@pcXmlData", cXMLData);
                oParam[53] = new OleDbParameter("@pbSpecial", isSpecial);
                //20160420, samypasha, TRBST15137, end
                //20241202, mustafa.noya, ANT-ANT-455, begin
                oParam[54] = new OleDbParameter("@pcCIFPelaku", cCIFPelaku);
                //20241202, mustafa.noya, ANT-ANT-455, end


                blnResult = this._cQuery.ExecProc("TRSUpdateTransaksiFLD", ref oParam, out dsOut);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool PopulateTransaksiDealFLD(int nDealId, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pnDealId", nDealId);

                blnResult = this._cQuery.ExecProc("TRSPopulateFLDDealTran", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20160825, samy, LOGEN00196, begin
        public bool CekRekeningTaxAmnestyFLD(string cRekRelasi, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pcAccountRelasi", cRekRelasi);

                blnResult = this._cQuery.ExecProc("TRSCekRekeningTaxAmnesty", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20160825, samy, LOGEN00196, end
        //20200611, vanny_w, DCR_109, begin
        public bool RPAPopulateRateDepo(string strCurrency, int Days, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcProduct", "FLD");
            dbPar[1] = new OleDbParameter("@pcCurrency", strCurrency);
            //20210823, rezakahfi, FLD-52, begin
            dbPar[2] = new OleDbParameter("@pnTenor", Days);
            //20210823, rezakahfi, FLD-52, end
            return this._cQuery.ExecProc("dbo.RPAPopulateRateDeposito", ref dbPar, out dsOut);
        }
        //20200611, vanny_w, DCR_109, end

        #endregion

        #region BlotterFLD
        public bool PopulateParamBlotterFLD(string cPosJab, out DataSet dsOut)
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pcStatusJab", cPosJab);

                blnResult = this._cQuery.ExecProc("TRSPopulateDataBlotterFLD", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;



        }


        //20160217, samy, TRBST15137, begin
        //public bool SearchBoltterFLD(string cPosJab, string cBranch, string cNIKSeller, string cTglTrans, string cTgljthTempo, string cCurrPair, out DataSet dsOut)
        public bool SearchBoltterFLD(string cPosJab, string cBranch, string cNIKSeller, string cTglTrans, string cTgljthTempo, string cCurrPair, string cTglTransTo, string cTglJthTmpTo, out DataSet dsOut)
        //20160217, samy, TRBST15137, end
        {

            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                //20160217, samy, TRBST15137, begin
                //OleDbParameter[] oParam = new OleDbParameter[6];
                OleDbParameter[] oParam = new OleDbParameter[8];
                //20160217, samy, TRBST15137, end
                oParam[0] = new OleDbParameter("@pcPosJab", cPosJab);
                oParam[1] = new OleDbParameter("@pcBranch", cBranch);
                oParam[2] = new OleDbParameter("@pcNIKSeller", cNIKSeller);
                oParam[3] = new OleDbParameter("@pdTglTrans", cTglTrans);
                oParam[4] = new OleDbParameter("@pdTglJatuhTempo", cTgljthTempo);
                oParam[5] = new OleDbParameter("@pcCurrPair", cCurrPair);
                //20160217, samy, TRBST15137, begin
                oParam[6] = new OleDbParameter("@pdTglTransTo", cTglTransTo);
                oParam[7] = new OleDbParameter("@pdTglJatuhTempoTo", cTglJthTmpTo);
                //20160217, samy, TRBST15137, end


                blnResult = this._cQuery.ExecProc("TRSSearchBlotterFLD", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //20170227, samypasha, TRBST15137, begin
        public bool SearchBoltterFLDPendingDok(string cCabang, out DataSet dsOut)
        {
            bool blnResult = false;
            dsOut = new DataSet();

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pcBranch", cCabang);

                blnResult = this._cQuery.ExecProc("TRSSearchBlotterPendingDokFLD", ref oParam, out dsOut);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20170227, samypasha, TRBST15137, end
        #endregion

        #region Approval FLD
        public bool PopulateApprovalTransaksiFLD(out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                blnResult = this._cQuery.ExecProc("TRSPopulateApprovalFLDTransaction", out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool PopulateUserDummyFLD(out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                blnResult = this._cQuery.ExecProc("TRSPopulateNIKFLD", out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //20231103, yudha.n, FLD-91, begin
        public int PopulateBranchDummyFLD()
        {
            DataSet dsOut = new DataSet();
            bool bResult = false;
            int branchDummy = 0;

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[5];
                dbParams[0] = new OleDbParameter("@pcTypePopulate", "ParamHoldBranch");
                dbParams[1] = new OleDbParameter("@pcProduct", "FLD");
                dbParams[2] = new OleDbParameter("@pcCIF", "");
                dbParams[3] = new OleDbParameter("@pcCIF2", "");
                dbParams[4] = new OleDbParameter("@pcFilter", "");

                bResult = this._cQuery.ExecProc("dbo.TRSPopulateDataNasabahProduct", ref dbParams, out dsOut);

                if (bResult)
                    branchDummy = int.Parse(dsOut.Tables[0].Rows[0][0].ToString());
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }

            return branchDummy;
        }
        //20231103, yudha.n, FLD-91, end

        //TRSPopulateNIKFLD
        //20160609, samy, TRBST15137, begin
        //public bool SaveApprovalTransaksiFLD(int nDealId, int userNIK, int sequence, string status, string acctType, string blockAcctType)
        //20230719, yazri, FLD-80, begin
        //public bool SaveApprovalTransaksiFLD(int nDealId, int userNIK, int sequence, string status, string acctType, string blockAcctType, string rejectReason)
        public bool SaveApprovalTransaksiFLD(int nDealId, int userNIK, int sequence, string status, string acctType, string blockAcctType, string rejectReason, bool isUsingALM)
        //20230719, yazri, FLD-80, end
        //20160609, samy, TRBST15137, end
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[8];
                oParam[0] = new OleDbParameter("@pnDealID", nDealId);
                oParam[1] = new OleDbParameter("@pnProcessNIK", userNIK);
                oParam[2] = new OleDbParameter("@pnSequence", sequence);
                oParam[3] = new OleDbParameter("@pcStatusApproval", status);
                oParam[4] = new OleDbParameter("@pcAccountType", acctType);
                oParam[5] = new OleDbParameter("@pcBlockAccountType", blockAcctType);
                oParam[6] = new OleDbParameter("@pcRejectReason", rejectReason);
                //20230719, yazri, FLD-80, begin
                oParam[7] = new OleDbParameter("@pbIsUsingALM", isUsingALM);
                //20230719, yazri, FLD-80, end

                blnResult = this._cQuery.ExecProc("TRSApprovalTransactionFLD", ref oParam);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool PopulateEmailFLD(int nDealId, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pnDealID", nDealId);

                blnResult = this._cQuery.ExecProc("TRSPopulateEmailFLD", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool GetBlockAmount(string ProductCode, decimal OriginalAmount, string CIFId, string Currency, out DataSet dsCalculatedAmount)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[4];
                oParam[0] = new OleDbParameter("@pcProductCode", ProductCode);
                oParam[1] = new OleDbParameter("@pmOriginalAmount", OriginalAmount);
                oParam[2] = new OleDbParameter("@pnCIFId", CIFId);
                oParam[3] = new OleDbParameter("@pcCurrency", Currency);

                blnResult = this._cQuery.ExecProc("TRSGetBlokirAmountFLD", ref oParam, out dsCalculatedAmount);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;

        }

        public bool SentEmailFLD(string cPurpose, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pcPurpose", cPurpose);

                blnResult = this._cQuery.ExecProc("TRSSentEmailFLD", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //20160727, samypasha, TRBST15137, begin
        public bool TakePickListFLD(string cAction, int nDealId)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pnDealID", nDealId);
                oParam[1] = new OleDbParameter("@pcAction", cAction);

                blnResult = this._cQuery.ExecProc("TRSTakePickListFLD", ref oParam);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool CheckMxWorkingDate(string cDate, string cCcy, out DataSet dsResult)
        {
            bool blnResult = false;
            dsResult = new DataSet();
            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pdBaseDate", cDate);
                oParam[1] = new OleDbParameter("@pcCcy", cCcy);


                blnResult = this._cQuery.ExecProc("CheckMXWorkingDate", ref oParam, out dsResult);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool PopulateRiskProfileNasabah(string strCIF, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcCIF", strCIF);


            return this._cQuery.ExecProc("dbo.TRSPopulateRiskProfileNasabah", ref dbParams, out dsOut);
        }
        //20160727, samypasha, TRBST15137, end

        #endregion

        #region Cancel FLD
        public bool PopulateTransaksiFLD(int nDealId, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[1];
                oParam[0] = new OleDbParameter("@pnDealId", nDealId);

                blnResult = this._cQuery.ExecProc("TRSPopulateFLDTran", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //20230719, yazri, FLD-80, begin
        //public bool CancelTransaksiFLD(int nDealId, int userNIK, string cReason, string cLepas, Guid gGuid1, Guid gGuid2)
        public bool CancelTransaksiFLD(int nDealId, int userNIK, string cReason, string cLepas, Guid gGuid1, Guid gGuid2, Guid gGuid3)
        //20230719, yazri, FLD-80, end
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[7];
                oParam[0] = new OleDbParameter("@pnDealID", nDealId);
                oParam[1] = new OleDbParameter("@pnProcessNIK", userNIK);
                oParam[2] = new OleDbParameter("@pcReason", cReason);
                oParam[3] = new OleDbParameter("@pcLepas", cLepas);
                oParam[4] = new OleDbParameter("@pgGuid1", gGuid1);
                oParam[5] = new OleDbParameter("@pgGuid2", gGuid2);
                //20230719, yazri, FLD-80, begin
                oParam[6] = new OleDbParameter("@pgGuid3", gGuid3);
                //20230719, yazri, FLD-80, end

                blnResult = this._cQuery.ExecProc("TRSCancelTransactionFLD", ref oParam);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        //20230719, yazri, FLD-80, begin
        public bool FLDIsUsingALM(string strDealId, string Param, out DataSet dsOut)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pnDealId", strDealId);
            dbPar[1] = new OleDbParameter("@pcParam", Param);
            return this._cQuery.ExecProc("dbo.TRSFLDIsUsingALM", ref dbPar, out dsOut);
        }
        //20230719, yazri, FLD-80, end

        #endregion

        //TRBST15137, victor, begin
        #region Proses Penempatan

        //20170726, samypasha, BOSIT17195, begin
        //public bool PopulatePenempatan(string JenisJurnal, out DataSet dsOut)
        //{           
        //dsOut = new DataSet();
        //20170726, samypasha, BOSIT17195, begin
        //OleDbParameter[] oParam = new OleDbParameter[1];
        //20170726, samypasha, BOSIT17195, end
        //oParam[0] = new OleDbParameter("@pcJenisJurnal", JenisJurnal);

        //return this._cQuery.ExecProc("dbo.TRSFLDPopulateProsesPenempatan", ref oParam, out dsOut);
        // }

        //public bool ProsesPenempatan(string jenisJurnal, string xmlData, int userNik)
        //{
        //    bool blnResult = false;

        //    try
        //    {
        //        OleDbParameter[] oParam = new OleDbParameter[2];
        //        oParam[0] = new OleDbParameter("@pnUserNik", userNik);
        //        oParam[1] = new OleDbParameter("@pcXmlData", xmlData);

        //        if (jenisJurnal == "FLD Penempatan")
        //        {
        //            blnResult = this._cQuery.ExecProc("dbo.TRSFLDProsesPenempatan", ref oParam);
        //        }
        //        else
        //        {
        //            blnResult = this._cQuery.ExecProc("dbo.TRSFLDProsesPenempatanDeposito", ref oParam);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }

        //    return blnResult;
        //}
        public bool ProsesPenempatan(string cModule, string jenisJurnal, string cProduct, string xmlData, int userNik, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[5];
                oParam[0] = new OleDbParameter("@pcModule", cModule);
                oParam[1] = new OleDbParameter("@pcJenisJurnal", jenisJurnal);
                oParam[2] = new OleDbParameter("@pcProduct", cProduct);
                oParam[3] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[4] = new OleDbParameter("@pnUserNik", userNik);

                blnResult = this._cQuery.ExecProc("dbo.DCRPopulateProsesPenempatanStructuredProduct", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        //20170726, samypasha, BOSIT17195, end

        public bool PopulateJenisProduct(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("dbo.TRSProdukStructuredProduct", out dsOut);

        }

        public bool PopulateJenisJurnal(string cModule, string cProduct, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcParam", cModule);
            dbPar[1] = new OleDbParameter("@pcProduct", cProduct);

            return _cQuery.ExecProc("dbo.TRSFLDPopulateJenisJurnal", ref dbPar, out dsOut);
        }

        #endregion
        //TRBST15137, victor, end

        //20160115, TRBST15137, samy, begin
        #region ParameterFldCounterCcy
        public bool PopulateParameterFLDCounterCcy(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSFLDPopulateFLDCounterCcy", out dsOut);

        }

        public bool SaveDataFLDCounterCcy(string xml)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xml);
            dbPar[1] = new OleDbParameter("@pnNik", this._userNIK);

            return _cQuery.ExecProc("dbo.TRSSaveParamFLDCounterCcy", ref dbPar);
        }

        #endregion

        #region ParameterFLDDeposito
        public bool PopulateParameterFLDDeposito(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSFLDPopulateFLDDepositoParameter", out dsOut);

        }
        //20161220, samy, TRBST16115, begin
        //public bool PopulateDataParameterFLDDeposito(string cParameter, string cTenor, string cCurrency, out DataSet dsResult)
        public bool PopulateDataParameterFLDDepositos(string cParameter, string cTenor, string cCurrency, string cProductType, out DataSet dsResult)
        //20161220, samy, TRBST16115, end
        {
            bool blnResult = false;
            dsResult = new DataSet();
            try
            {
                //20161220, samy, TRBST16115, begin
                //OleDbParameter[] oParam = new OleDbParameter[3];
                OleDbParameter[] oParam = new OleDbParameter[4];
                //20161220, samy, TRBST16115, end
                oParam[0] = new OleDbParameter("@pcParameter", cParameter);
                oParam[1] = new OleDbParameter("@pcTenor", cTenor);
                oParam[2] = new OleDbParameter("@pcCurrency", cCurrency);
                //20161220, samy, TRBST16115, begin
                oParam[3] = new OleDbParameter("@pcProduct", cProductType);
                //20161220, samy, TRBST16115, end

                blnResult = this._cQuery.ExecProc("TRSFLDPopulateDataFLDDeposito", ref oParam, out dsResult);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveDataFLDDeposito(string xml)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xml);
            dbPar[1] = new OleDbParameter("@pnNik", this._userNIK);

            return _cQuery.ExecProc("dbo.TRSSaveParamFLDDeposito", ref dbPar);
        }

        #endregion

        #region ViewUploadParamFLD
        //20170821, samy, TRBST16115, begin
        //public bool PopulateViewParameterUploadFLD(out DataSet dsOut)
        public bool PopulateViewParameterUploadFLD(string cProduct, out DataSet dsOut)
        //20170821, samy, TRBST16115, end
        {

            dsOut = new DataSet();
            //20170821, samy, TRBST16115, begin
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcProduct", cProduct);
            //20170821, samy, TRBST16115, end

            //20170821, samy, TRBST16115, begin
            //return this._cQuery.ExecProc("TRSPopulateComboViewUploadParamFLD", out dsOut);
            return this._cQuery.ExecProc("TRSPopulateComboViewUploadParamFLD", ref dbPar, out dsOut);
            //20170821, samy, TRBST16115, end
        }

        //20180118, samypasha, BOSIT18017, begin
        //public bool SearchViewUploadparameter(string cProduct, string cParameter, out DataSet dsOut)
        //20170821, samy, TRBST16115, begin
        //public bool SearchViewUploadparameter(string cParameter, string cFLDCounterCcy, out DataSet dsOut)
        public bool SearchViewUploadparameter(string cProduct, string cParameter, string cFLDCounterCcy, out DataSet dsOut)
        //20170821, samy, TRBST16115, end
        //20180118, samypasha, BOSIT18017, end
        {
            //20180118, samypasha, BOSIT18017, begin
            //OleDbParameter[] dbPar = new OleDbParameter[1];
            //20170821, samy, TRBST16115, begin
            //OleDbParameter[] dbPar = new OleDbParameter[2];
            OleDbParameter[] dbPar = new OleDbParameter[3];
            //20170821, samy, TRBST16115, end
            //20180118, samypasha, BOSIT18017, end
            dbPar[0] = new OleDbParameter("@pcParameter", cParameter);
            //20180118, samypasha, BOSIT18017, begin
            dbPar[1] = new OleDbParameter("@pcCounterCcy", cFLDCounterCcy);
            //20180118, samypasha, BOSIT18017, end
            //20170821, samy, TRBST16115, begin
            dbPar[2] = new OleDbParameter("@pcProduct", cProduct);
            //20170821, samy, TRBST16115, end

            return _cQuery.ExecProc("TRSFLDPopulateViewUploadParameterFLD", ref dbPar, out dsOut);
        }
        #endregion

        #region MaintainJobTitle
        public bool PopulateJobTitle(out DataSet dsOut)
        {
            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSFLDPopulateJobTitle", out dsOut);
        }

        public bool SaveDataJobTitle(string xml)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xml);
            dbPar[1] = new OleDbParameter("@pnNik", this._userNIK);

            return _cQuery.ExecProc("dbo.TRSSaveParamFLDJobTitle", ref dbPar);
        }
        #endregion

        #region ApprovalJobTitle
        public bool PopulateApprovalJobTitle(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSFLDPopulateApprovalJobTitle", out dsOut);
        }

        public bool SaveApprovalJobTitle(string xmlData, int prosesNIK, string statusApproval)
        {
            bool blnResult = false;

            try
            {

                OleDbParameter[] oParam = new OleDbParameter[3];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnProcessNIK", prosesNIK);
                oParam[2] = new OleDbParameter("@pcStatusApproval", statusApproval);

                blnResult = this._cQuery.ExecProc("TRSFLDProcessApprovalJobTitle", ref oParam);



            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }
        #endregion

        #region Fee Branch

        //20170726, samypasha, TRBST16115, begin
        //public bool PopulateFeeBranch(out DataSet dsOut)
        public bool PopulateFeeBranch(string cProduct, out DataSet dsOut)
        //20170726, samypasha, TRBST16115, end
        {

            dsOut = new DataSet();
            //20170726, samypasha, TRBST16115, begin
            //return this._cQuery.ExecProc("dbo.TRSFLDPopulateFeeBranch", out dsOut);
            bool bReturn = false;
            if (cProduct == "FLD")
            {
                return this._cQuery.ExecProc("dbo.TRSFLDPopulateFeeBranch", out dsOut);
            }
            else if (cProduct == "DCR")
            {
                return this._cQuery.ExecProc("dbo.DCRPopulateFeeBranch", out dsOut);
            }
            else if (cProduct == "DCR EKI")
            {
                return this._cQuery.ExecProc("dbo.DCREKIPopulateFeeBranch", out dsOut);
            }
            else
            {
                return bReturn;
            }
            //20170726, samypasha, TRBST16115, end

        }

        //20170726, samypasha, TRBST16115, begin
        //public bool ProsesFeeBranch(string xmlData, int userNik)
        public bool ProsesFeeBranch(string cProduct, string xmlData, int userNik)
        //20170726, samypasha, TRBST16115, end
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pnUserNik", userNik);
                oParam[1] = new OleDbParameter("@pcXmlData", xmlData);

                //20170726, samypasha, TRBST16115, begin
                //blnResult = this._cQuery.ExecProc("dbo.TRSFLDProsesFeeBranch", ref oParam);
                if (cProduct == "FLD")
                {
                    blnResult = this._cQuery.ExecProc("dbo.TRSFLDProsesFeeBranch", ref oParam);
                }
                else if (cProduct == "DCR" || cProduct == "DCR EKI")
                {
                    blnResult = this._cQuery.ExecProc("dbo.DCRProsesFeeBranch", ref oParam);
                }

                //20170726, samypasha, TRBST16115, end
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        #endregion

        #region Parameter Cut Off Time

        public bool PopulateComboBoxCutOffTime(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSPopulateComboBoxParamCutOffTime", out dsOut);

        }
        public bool PopulateParamCutOffTime(out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                blnResult = this._cQuery.ExecProc("TRSFLDPopulateCutOffTime", out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveDataParamCutOffTime(string cCutOffTimeTOM, string cCutOffTimeSPOT)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcCutOffTimeTOM", cCutOffTimeTOM);
            dbPar[1] = new OleDbParameter("@pcCutOffTimeSPOT", cCutOffTimeSPOT);
            dbPar[2] = new OleDbParameter("@pnUserNIK", this._userNIK);

            return _cQuery.ExecProc("dbo.TRSParamSaveCutOffTime", ref dbPar);
        }
        #endregion

        #region Parameter Cut Off Time Master Nasabah

        public bool PopulateParamCutOffTimeMasterNasabah(out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                blnResult = this._cQuery.ExecProc("TRSFLDPopulateCutOffTimeMasterNasabah", out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveDataParamCutOffTimeMasterNasabah(int nEfektifMasterNasabah, string cCutOffTime)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnEfektifMasternasabah", nEfektifMasterNasabah);
            dbPar[1] = new OleDbParameter("@pcCutOffTime", cCutOffTime);
            dbPar[2] = new OleDbParameter("@pnUserNIK", this._userNIK);

            return _cQuery.ExecProc("dbo.TRSParamSaveCutOffTimeMasterNasabah", ref dbPar);
        }

        public bool UpdateDataRiskProfile(string cCifNo, string cRiskProfile, string cRiskProfileDescription
            , string cLastUpdateDate, string cExpiredDate)
        {
            OleDbParameter[] dbPar = new OleDbParameter[5];
            dbPar[0] = new OleDbParameter("@pcCIFNo", cCifNo);
            dbPar[1] = new OleDbParameter("@pcRiskProfile", cRiskProfile);
            dbPar[2] = new OleDbParameter("@pcRiskProfileDescription", cRiskProfileDescription);
            dbPar[3] = new OleDbParameter("@pdLastUpdateDate", cLastUpdateDate);
            dbPar[4] = new OleDbParameter("@pdExpiredDate", cExpiredDate);

            return _cQuery.ExecProc("dbo.TRSUpdateRiskProfile", ref dbPar);
        }
        #endregion

        #region Proses Pajak

        public bool PopulatePajak(string jenisJurnal, out DataSet dsOut)
        {

            dsOut = new DataSet();

            if (jenisJurnal == "Jurnal Pajak")
            {
                return this._cQuery.ExecProc("dbo.TRSFLDPopulateProsesPajak", out dsOut);
            }
            else
            {
                return this._cQuery.ExecProc("dbo.TRSFLDPopulateProsesFWDReversal", out dsOut);
            }

        }

        public bool ProsesPajak(string jenisJurnal, string xmlData, int userNik)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pnUserNik", userNik);
                oParam[1] = new OleDbParameter("@pcXmlData", xmlData);

                if (jenisJurnal == "Jurnal Pajak")
                {
                    blnResult = this._cQuery.ExecProc("dbo.TRSFLDProsesPajak", ref oParam);
                }
                else
                {
                    blnResult = this._cQuery.ExecProc("dbo.TRSFLDProsesFWDReversal", ref oParam);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        #endregion

        #region ParamSegment
        public bool PopulateParamSegment(out DataSet dsOut)
        {
            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSFLDPopulateSegmentStructuredProduct", out dsOut);
        }

        public bool SaveDataParamSegment(string xml)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xml);
            dbPar[1] = new OleDbParameter("@pnNik", this._userNIK);

            return _cQuery.ExecProc("dbo.TRSSaveParamSegment", ref dbPar);
        }
        #endregion

        #region Proses Maturity

        public bool PopulateMaturity(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("dbo.TRSFLDPopulateMaturityFLD", out dsOut);

        }

        public bool ProsesMaturity(string xmlData, int userNik)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pnUserNik", userNik);
                oParam[1] = new OleDbParameter("@pcXmlData", xmlData);

                blnResult = this._cQuery.ExecProc("dbo.TRSFLDProsesKreditNasabah", ref oParam);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        #endregion

        #region Parameter Bunga Spesial Deposito
        public bool PopulateParameterBungaSpesialFLDDeposito(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("TRSFLDPopulateBungaSpesial", out dsOut);

        }

        public bool PopulateDataParameterFLDDeposito(string cParameter, string cValue, out DataSet dsResult)
        {
            bool blnResult = false;
            dsResult = new DataSet();
            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcParameter", cParameter);
                oParam[1] = new OleDbParameter("@pcValue", cValue);


                blnResult = this._cQuery.ExecProc("TRSFLDPopulateParamBungaSpesial", ref oParam, out dsResult);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool SaveDataFLDParamBungaSpesial(string xml, string xml2, string code)
        {
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcXmlData", xml);
            dbPar[1] = new OleDbParameter("@pcXmlDataDetail", xml2);
            dbPar[2] = new OleDbParameter("@pnNik", this._userNIK);
            dbPar[3] = new OleDbParameter("@pcCode", code);

            return _cQuery.ExecProc("dbo.TRSSaveParamFLDBungaSpesial", ref dbPar);
        }

        public bool DeleteResetDataFLDBungaSpesial(string counterCcy, string code)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcCounterCurrency", counterCcy);
            dbPar[1] = new OleDbParameter("@pnNik", this._userNIK);
            dbPar[2] = new OleDbParameter("@pcCode", code);

            return _cQuery.ExecProc("dbo.TRSDeleteParamFLDBungaSpesial", ref dbPar);
        }

        #endregion
        //20160115, TRBST15137, samy, end

        //20180109, samypasha, BOSIT18017, begin
        #region ParameterFldCounterCcy
        public bool PopulateParameterFLDCounterCcyPair(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("FLDPopulateFLDCounterCcyPair", out dsOut);

        }

        public bool CekParameterFLDCounterCcyPair(int FLDId, string FLDCounterCcy, string FLDCccy, string CcyPair, out DataSet dsOut)
        {

            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pnFLDId", FLDId);
            dbPar[1] = new OleDbParameter("@pcFLDCounterCcy", FLDCounterCcy);
            dbPar[2] = new OleDbParameter("@pcFLDCurrency", FLDCccy);
            dbPar[3] = new OleDbParameter("@pcCcyPair", CcyPair);

            return this._cQuery.ExecProc("FLDCekFLDCounterCcyPair", ref dbPar, out dsOut);

        }

        public bool FLDPopulateFLDCurrency(out DataSet dsOut)
        {

            dsOut = new DataSet();

            return this._cQuery.ExecProc("FLDPopulateFLDCurrency", out dsOut);

        }

        public bool FLDPopulateCounterCcyPair(string FLDCounterCcy, string FLDCccy, out DataSet dsOut)
        {

            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcFLDCounterCcy", FLDCounterCcy);
            dbPar[1] = new OleDbParameter("@pcFLDCurrency", FLDCccy);

            return this._cQuery.ExecProc("FLDPopulateCounterCcyPair", ref dbPar, out dsOut);

        }

        public bool SaveDataFLDCounterCcyPair(string xml)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xml);
            dbPar[1] = new OleDbParameter("@pnNik", this._userNIK);

            return _cQuery.ExecProc("dbo.TRSSaveParamFLDCounterCcyPair", ref dbPar);
        }

        #endregion
        //20180109, samypasha, BOSIT18017, end
        //20190116, samy, BOSOD18243, begin
        #region Source Of Fund
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

                blnResult = this._cQuery.ExecProc("DCRPopulateSumberDanaStructuredProduct", ref oParam, out dsOut);


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

            return this._cQuery.ExecProc("dbo.FMCTPopulateDataMature", ref dbParams, out dsOut);
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

        public bool PopulateUserDummy(out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                blnResult = this._cQuery.ExecProc("TRSPopulateNIKFLD", out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public bool PopulateApprovalSumberDana(int nDealId, string strProduct, out DataSet dsOut)
        {
            dsOut = new DataSet();
            bool bResult = false;

            try
            {
                OleDbParameter[] dbParams = new OleDbParameter[2];
                dbParams[0] = new OleDbParameter("@pnDealId", nDealId);
                dbParams[1] = new OleDbParameter("@pcProduct", strProduct);

                bResult = this._cQuery.ExecProc("dbo.TRSPopulateSourceOfFundFLD", ref dbParams, out dsOut);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return bResult;
            }

            return bResult;

        }
        #endregion
        //20190116, samy, BOSOD18243, end
        //20230925, samy, FLD-71, begin
        public bool CekChangeMaster(string CIFNo, string ChangeType, out bool bOutput)
        {
            bOutput = false;

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcCIF", CIFNo);
            dbPar[1] = new OleDbParameter("@pcChangeType", ChangeType);
            dbPar[2] = new OleDbParameter("@pbOutputFlag", bOutput);
            dbPar[2].Direction = ParameterDirection.Output;

            bool bOK = clsGlobal.QueryCIF.ExecProc("dbo.TRSCheckChangeMasterStructuredProduct", ref dbPar);

            if (bOK)
            {
                bOutput = bool.Parse(dbPar[2].Value.ToString());
            }

            return bOK;
        }
        //20230925, samy, FLD-71, end

    }
}
