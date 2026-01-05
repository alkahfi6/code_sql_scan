using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Windows.Forms;

namespace BankNISP.FrontEnd
{
    class clsCustomer
    {
        public int intNIK;
        public string strGuid;
        public string strMenuName;
        public string strBranch;
        public Int64 intCIF;
        public string strCustomerName1;
        public string strCustomerName2;
        public string strTitleBefore;
        public string strTitleAfterName;
        public string dateBirthIncorporation;
        public string strBirthPlace;
        public string strIDNumber;
        public string strIDTypeCode;
        public string dateIDExpiryDate;
        public string strCustomerTypeCode;
        public string strMotherMaidenName;
        public string strAddressLine1;
        public string strAddressLine2;
        public string strAddressLine3;
        public string strAddressLine4;
        public string strPostalCode;
        public string strKELURAHAN;
        public string strKECAMATAN;
        public string strDATIII;
        public string strPROVINSI;
        public string dateTransactionDate;
        public string dateIDIssuedDate;
        public int intStatusExec;
        public int intCounterEksekusi;
        public int intType;
        public int intStatus;
        public DataSet dsOutDtPribadi = new DataSet();
        public DataSet dsOutAccount = new DataSet();
        public DataSet dsOutCurrentDate = new DataSet();
        //20100413, indra_w, CIFUPLD008, begin -- BUGS
        public DataSet dsOutMaintPribadi = new DataSet();
        //20100413, indra_w, CIFUPLD008, end -- BUGS
        public DateTime TanggalTransaksi;
        public DateTime TanggalSekarang;
        public bool boolForeignAddress;

        public string strIDtypecode;
        public string strIDIssuePlace;
        public string strOriginalcustomerdate;
        public string strSexcode;
        public string strCountryofheritage;
        public string strCountryofcitizenship;
        public string strCountry;
        public string strUIC2userdefined;
        public string strUIC3userdefined;
        public string strUIC7userdefined;
        public string strUIC8userdefined;
        public string strLaporanTxTunai;
        public string strLaporanPajak;
        public int intEmployeeNIK;
        public string strEmployeename;
        public bool boolEmployeeIndicator;

        public string strAccountType;
        public string strShortName;
        public int intMore;
        public Int64 intAccount;
        public string strRebid;
        public string pcIDNumber = "";
        public string pcIDtypecode = "";
        public string pcCustomertypecode = "";
        public string pcCustomername1 = "";
        public string pcCustomername2 = "";
        public string strResult01549 = "";
        public string strResult01516 = "";
        public string strResult01565 = "";
        public string strResult01579 = "";
        public string strResult01700 = "";
        public string strResult01610 = "";

        public NispQuery.ClsQuery clsQuery;
        public string strModule;

        public bool ProCIFParseParmQuickCreate()
        {
            clsWebService _cWS = new clsWebService();
            bool blnResultTibco;

            _cWS.clsWebServiceLoad(intNIK.ToString(), strGuid, strModule);

            string strMoreIndicator01549;
            strMoreIndicator01549 = "N";
            
            //blnResultTibco = _cWS.CallCIFInquirySFByNameBDandCIF01549(strCustomerName1, DateTime.Parse(dateBirthIncorporation).ToString("ddMMyyyy"), strIDNumber, out strResult01549, ref strMoreIndicator01549);
            blnResultTibco = _cWS.CallCIFInquirySFByNameBDandCIF01549(strCustomerName1, dateBirthIncorporation.Remove(2, 1).Remove(4, 1), strIDNumber, out strResult01549, ref strMoreIndicator01549);
            
            if (!blnResultTibco)
            {
                return false;
            }

            string strMoreIndicator01516;
            strMoreIndicator01516 = "N";
            

            blnResultTibco = _cWS.CallCIFInqBlackList01516mbase(strIDNumber, strIDTypeCode, strCustomerName1, out strResult01516, ref strMoreIndicator01516);

            if (!blnResultTibco)
            {
                return false;
            }

            string strMoreIndicator01579;
            strMoreIndicator01579 = "N";
            

            blnResultTibco = _cWS.CallCIFInquiryHighRiskCustomer01579(strIDNumber, strIDTypeCode, "", "", "", out strResult01579, ref strMoreIndicator01579);

            if (!blnResultTibco)
            {
                return false;
            }

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[35];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcTransactionBranch", System.Data.OleDb.OleDbType.Char, 5);
            dbParam[0].Value = strBranch;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcCustomerName1", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[1].Value = strCustomerName1;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcCustomerName2", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[2].Value = strCustomerName2;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcTitleBefore", System.Data.OleDb.OleDbType.VarChar, 15);
            dbParam[3].Value = strTitleBefore;
            dbParam[3].Direction = System.Data.ParameterDirection.Input;

            dbParam[4] = new System.Data.OleDb.OleDbParameter("@pcTitleAfterName", System.Data.OleDb.OleDbType.VarChar, 15);
            dbParam[4].Value = strTitleAfterName;
            dbParam[4].Direction = System.Data.ParameterDirection.Input;

            dbParam[5] = new System.Data.OleDb.OleDbParameter("@pcBirthIncorporation", System.Data.OleDb.OleDbType.Char, 10);
            dbParam[5].Value = dateBirthIncorporation;
            dbParam[5].Direction = System.Data.ParameterDirection.Input;

            dbParam[6] = new System.Data.OleDb.OleDbParameter("@pcBirthPlace", System.Data.OleDb.OleDbType.Char, 30);
            dbParam[6].Value = strBirthPlace;
            dbParam[6].Direction = System.Data.ParameterDirection.Input;

            dbParam[7] = new System.Data.OleDb.OleDbParameter("@pnBranchNumber", System.Data.OleDb.OleDbType.Char, 5);
            dbParam[7].Value = strBranch;
            dbParam[7].Direction = System.Data.ParameterDirection.Input;

            dbParam[8] = new System.Data.OleDb.OleDbParameter("@pcIDNumber", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[8].Value = strIDNumber;
            dbParam[8].Direction = System.Data.ParameterDirection.Input;

            dbParam[9] = new System.Data.OleDb.OleDbParameter("@pcIDTypeCode", System.Data.OleDb.OleDbType.Char, 4);
            dbParam[9].Value = strIDTypeCode;
            dbParam[9].Direction = System.Data.ParameterDirection.Input;

            dbParam[10] = new System.Data.OleDb.OleDbParameter("@pnIDExpiryDate", System.Data.OleDb.OleDbType.Char, 10);
            dbParam[10].Value = dateIDExpiryDate;
            dbParam[10].Direction = System.Data.ParameterDirection.Input;

            dbParam[11] = new System.Data.OleDb.OleDbParameter("@pcCustomerTypeCode", System.Data.OleDb.OleDbType.Char, 1);
            dbParam[11].Value = strCustomerTypeCode;
            dbParam[11].Direction = System.Data.ParameterDirection.Input;

            dbParam[12] = new System.Data.OleDb.OleDbParameter("@pcMotherMaidenName", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[12].Value = strMotherMaidenName;
            dbParam[12].Direction = System.Data.ParameterDirection.Input;

            dbParam[13] = new System.Data.OleDb.OleDbParameter("@pcAddressLine1", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[13].Value = strAddressLine1;
            dbParam[13].Direction = System.Data.ParameterDirection.Input;

            dbParam[14] = new System.Data.OleDb.OleDbParameter("@pcAddressLine2", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[14].Value = strAddressLine2;
            dbParam[14].Direction = System.Data.ParameterDirection.Input;

            dbParam[15] = new System.Data.OleDb.OleDbParameter("@pcAddressLine3", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[15].Value = strAddressLine3;
            dbParam[15].Direction = System.Data.ParameterDirection.Input;

            dbParam[16] = new System.Data.OleDb.OleDbParameter("@pcAddressLine4", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[16].Value = strAddressLine4;
            dbParam[16].Direction = System.Data.ParameterDirection.Input;

            dbParam[17] = new System.Data.OleDb.OleDbParameter("@pnPostalCode", System.Data.OleDb.OleDbType.Integer);
            if (strPostalCode.Trim() == "")
            {
                dbParam[17].Value = 0;
            }
            else
            {
                dbParam[17].Value = strPostalCode;
            }
            dbParam[17].Direction = System.Data.ParameterDirection.Input;

            dbParam[18] = new System.Data.OleDb.OleDbParameter("@pcUserID", System.Data.OleDb.OleDbType.Char, 10);
            dbParam[18].Value = intNIK;
            dbParam[18].Direction = System.Data.ParameterDirection.Input;

            dbParam[19] = new System.Data.OleDb.OleDbParameter("@pcKELURAHAN", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[19].Value = strKELURAHAN;
            dbParam[19].Direction = System.Data.ParameterDirection.Input;

            dbParam[20] = new System.Data.OleDb.OleDbParameter("@pcKECAMATAN", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[20].Value = strKECAMATAN;
            dbParam[20].Direction = System.Data.ParameterDirection.Input;

            dbParam[21] = new System.Data.OleDb.OleDbParameter("@pcDATIII", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[21].Value = strDATIII;
            dbParam[21].Direction = System.Data.ParameterDirection.Input;

            dbParam[22] = new System.Data.OleDb.OleDbParameter("@pcPROVINSI", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[22].Value = strPROVINSI;
            dbParam[22].Direction = System.Data.ParameterDirection.Input;

            dbParam[23] = new System.Data.OleDb.OleDbParameter("@pdIDIssuedDate", System.Data.OleDb.OleDbType.Char,10);
            dbParam[23].Value = dateIDIssuedDate;
            dbParam[23].Direction = System.Data.ParameterDirection.Input;

            dbParam[24] = new System.Data.OleDb.OleDbParameter("@pvGuid", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[24].Value = strGuid;
            dbParam[24].Direction = System.Data.ParameterDirection.Input;

            dbParam[25] = new System.Data.OleDb.OleDbParameter("@pcTellerId", System.Data.OleDb.OleDbType.Char, 7);
            dbParam[25].Value = intNIK.ToString();
            dbParam[25].Direction = System.Data.ParameterDirection.Input;

            dbParam[26] = new System.Data.OleDb.OleDbParameter("@pcCIF", System.Data.OleDb.OleDbType.BigInt);
            dbParam[26].Value = intCIF; 
            dbParam[26].Direction = System.Data.ParameterDirection.Output;

            dbParam[27] = new System.Data.OleDb.OleDbParameter("@pcStatusExec", System.Data.OleDb.OleDbType.Integer);
            dbParam[27].Value = intStatusExec;
            dbParam[27].Direction = System.Data.ParameterDirection.Output;

            dbParam[28] = new System.Data.OleDb.OleDbParameter("@pnCounterEksekusi", System.Data.OleDb.OleDbType.TinyInt);
            dbParam[28].Value = intCounterEksekusi;
            dbParam[28].Direction = System.Data.ParameterDirection.Input;

            dbParam[29] = new System.Data.OleDb.OleDbParameter("@pbForeignAddress", System.Data.OleDb.OleDbType.Boolean);
            dbParam[29].Value = boolForeignAddress;
            dbParam[29].Direction = System.Data.ParameterDirection.Input;

            dbParam[30] = new System.Data.OleDb.OleDbParameter("@intType", System.Data.OleDb.OleDbType.Integer);
            dbParam[30].Value = intType;
            dbParam[30].Direction = System.Data.ParameterDirection.Input;

            dbParam[31] = new System.Data.OleDb.OleDbParameter("@strResult01549", System.Data.OleDb.OleDbType.VarChar, 10000);
            dbParam[31].Value = strResult01549;
            dbParam[31].Direction = System.Data.ParameterDirection.Input;
            
            dbParam[32] = new System.Data.OleDb.OleDbParameter("@strResult01516", System.Data.OleDb.OleDbType.VarChar, 10000);
            dbParam[32].Value = strResult01516;
            dbParam[32].Direction = System.Data.ParameterDirection.Input;

            dbParam[33] = new System.Data.OleDb.OleDbParameter("@strResult01579", System.Data.OleDb.OleDbType.VarChar, 10000);
            dbParam[33].Value = strResult01579;
            dbParam[33].Direction = System.Data.ParameterDirection.Input;

            dbParam[34] = new System.Data.OleDb.OleDbParameter("@strResult01700", System.Data.OleDb.OleDbType.VarChar, 10000);
            dbParam[34].Value = strResult01700;
            dbParam[34].Direction = System.Data.ParameterDirection.Input;
            
            Cursor.Current = Cursors.WaitCursor;

            bool blnResult = clsQuery.ExecProc("ProCIFParseParmQuickCreate", ref dbParam, out dsOutDtPribadi);

            if (blnResult == true)
            {
                intStatusExec = int.Parse(dbParam[27].Value.ToString());
                if (intStatusExec!= 0)
                {
                    return false;
                }
                else
                {
                    if (intType == 1)
                    {
                        intCIF = Int64.Parse(dbParam[26].Value.ToString());
                    }
                    
                    return true;
                }
                
            }
            else
            {
                intStatusExec = 3;
                return false;
            }

            Cursor.Current = Cursors.Default;
        }

        public bool ProCIFParseParmBasicInfDtls()
        {
            if (intType == 1) //masuk untuk kedua kalinya ambil tibco untuk dilempar ke sql
            {
                clsWebService _cWS = new clsWebService();
                bool blnResultTibco;

                _cWS.clsWebServiceLoad(intNIK.ToString(), strGuid, strModule);

                string strMoreIndicator01516;
                strMoreIndicator01516 = "N";
                
                blnResultTibco = _cWS.CallCIFInqBlackList01516mbase(strIDNumber, strIDTypeCode, strCustomerName1, out strResult01516, ref strMoreIndicator01516);

                if (!blnResultTibco)
                {
                    return false;
                }

                string strMoreIndicator01579;
                strMoreIndicator01579 = "N";
                
                blnResultTibco = _cWS.CallCIFInquiryHighRiskCustomer01579(strIDNumber, strIDTypeCode, "", "", "", out strResult01579, ref strMoreIndicator01579);

                if (!blnResultTibco)
                {
                    return false;
                }

                if (intStatus == 2) //tidak ada di lokal (tibco InfDtls
                {
                    blnResultTibco = _cWS.CallCIFInquiryCIFDetail01610(intCIF.ToString(), out strResult01610);
                    
                }
            }

            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[14];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pcTransactionBranch", System.Data.OleDb.OleDbType.Char, 5);
            dbParam[0].Value = strBranch;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcCustomerNumber", System.Data.OleDb.OleDbType.BigInt);
            dbParam[1].Value = intCIF;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pvGuid", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[2].Value = strGuid;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcTellerId", System.Data.OleDb.OleDbType.Char,7);
            dbParam[3].Value = intNIK.ToString();
            dbParam[3].Direction = System.Data.ParameterDirection.Input;

            //20100305, zahri, penambahan parameter CIFUPLD008
            dbParam[4] = new System.Data.OleDb.OleDbParameter("@intType", System.Data.OleDb.OleDbType.Integer);
            dbParam[4].Value = intType;
            dbParam[4].Direction = System.Data.ParameterDirection.Input;

            dbParam[5] = new System.Data.OleDb.OleDbParameter("@intStatus", System.Data.OleDb.OleDbType.Char, 7);
            dbParam[5].Value = intStatus;
            dbParam[5].Direction = System.Data.ParameterDirection.InputOutput;

            dbParam[6] = new System.Data.OleDb.OleDbParameter("@pcIDNumber", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[6].Value = pcIDNumber;
            dbParam[6].Direction = System.Data.ParameterDirection.InputOutput;

            dbParam[7] = new System.Data.OleDb.OleDbParameter("@pcIDtypecode", System.Data.OleDb.OleDbType.Char, 4);
            dbParam[7].Value = pcIDtypecode;
            dbParam[7].Direction = System.Data.ParameterDirection.InputOutput;

            dbParam[8] = new System.Data.OleDb.OleDbParameter("@pcCustomertypecode", System.Data.OleDb.OleDbType.Char, 1);
            dbParam[8].Value = pcCustomertypecode;
            dbParam[8].Direction = System.Data.ParameterDirection.InputOutput;

            dbParam[9] = new System.Data.OleDb.OleDbParameter("@pcCustomername1", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[9].Value = pcCustomername1;
            dbParam[9].Direction = System.Data.ParameterDirection.InputOutput;

            dbParam[10] = new System.Data.OleDb.OleDbParameter("@pcCustomername2", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[10].Value = pcCustomername2;
            dbParam[10].Direction = System.Data.ParameterDirection.InputOutput;

            dbParam[11] = new System.Data.OleDb.OleDbParameter("@strResult01516", System.Data.OleDb.OleDbType.VarChar, 100000);
            dbParam[11].Value = strResult01516;
            dbParam[11].Direction = System.Data.ParameterDirection.Input;

            dbParam[12] = new System.Data.OleDb.OleDbParameter("@strResult01579", System.Data.OleDb.OleDbType.VarChar, 100000);
            dbParam[12].Value = strResult01579;
            dbParam[12].Direction = System.Data.ParameterDirection.Input;

            dbParam[13] = new System.Data.OleDb.OleDbParameter("@strResult01610", System.Data.OleDb.OleDbType.VarChar, 100000);
            dbParam[13].Value = strResult01610;
            dbParam[13].Direction = System.Data.ParameterDirection.Input;

            Cursor.Current = Cursors.WaitCursor;

            bool blnResult = clsQuery.ExecProc("ProCIFParseParmBasicInfDtls", ref dbParam, out dsOutDtPribadi);

            if (blnResult == true)
            {
                intStatus = int.Parse(dbParam[5].Value.ToString());
                pcIDNumber = dbParam[6].Value.ToString();
                pcIDtypecode = dbParam[7].Value.ToString();
                pcCustomertypecode = dbParam[8].Value.ToString();
                pcCustomername1 = dbParam[9].Value.ToString();
                pcCustomername2 = dbParam[10].Value.ToString();
                return true;
            }
            else
            {
                return false;
            }
            Cursor.Current = Cursors.Default;
        }

        public bool ProCIFParseParmBasicInfV2Maint()
        {
            System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[29];

            dbParam[0] = new System.Data.OleDb.OleDbParameter("@pBranchnumber", System.Data.OleDb.OleDbType.Char, 5);
            dbParam[0].Value = strBranch;
            dbParam[0].Direction = System.Data.ParameterDirection.Input;

            dbParam[1] = new System.Data.OleDb.OleDbParameter("@pnCustomernumber", System.Data.OleDb.OleDbType.BigInt);
            dbParam[1].Value = intCIF;
            dbParam[1].Direction = System.Data.ParameterDirection.Input;

            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcCustomertypecode", System.Data.OleDb.OleDbType.Char,1);
            dbParam[2].Value = strCustomerTypeCode;
            dbParam[2].Direction = System.Data.ParameterDirection.Input;

            dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcIDtypecode", System.Data.OleDb.OleDbType.Char, 4);
            dbParam[3].Value = strIDtypecode;
            dbParam[3].Direction = System.Data.ParameterDirection.Input;

            dbParam[4] = new System.Data.OleDb.OleDbParameter("@pcIDnumber", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[4].Value = strIDNumber;
            dbParam[4].Direction = System.Data.ParameterDirection.Input;

            dbParam[5] = new System.Data.OleDb.OleDbParameter("@pnIDExpirydate", System.Data.OleDb.OleDbType.Char,10);
            dbParam[5].Value = dateIDExpiryDate;
            dbParam[5].Direction = System.Data.ParameterDirection.Input;

            dbParam[6] = new System.Data.OleDb.OleDbParameter("@pcIDIssuePlace", System.Data.OleDb.OleDbType.VarChar,30);
            dbParam[6].Value = strIDIssuePlace;
            dbParam[6].Direction = System.Data.ParameterDirection.Input;

            dbParam[7] = new System.Data.OleDb.OleDbParameter("@pnDateofBirth", System.Data.OleDb.OleDbType.Char, 10);
            dbParam[7].Value = dateBirthIncorporation;
            dbParam[7].Direction = System.Data.ParameterDirection.Input;

            dbParam[8] = new System.Data.OleDb.OleDbParameter("@pcBirthPlace", System.Data.OleDb.OleDbType.VarChar,30);
            dbParam[8].Value = strBirthPlace;
            dbParam[8].Direction = System.Data.ParameterDirection.Input;

            dbParam[9] = new System.Data.OleDb.OleDbParameter("@pcSexcode", System.Data.OleDb.OleDbType.Char, 1);
            dbParam[9].Value = strSexcode;
            dbParam[9].Direction = System.Data.ParameterDirection.Input;

            dbParam[10] = new System.Data.OleDb.OleDbParameter("@pcCountryofheritage", System.Data.OleDb.OleDbType.Char, 3);
            dbParam[10].Value = strCountryofheritage;
            dbParam[10].Direction = System.Data.ParameterDirection.Input;

            dbParam[11] = new System.Data.OleDb.OleDbParameter("@pcCountryofcitizenship", System.Data.OleDb.OleDbType.Char, 3);
            dbParam[11].Value = strCountryofcitizenship;
            dbParam[11].Direction = System.Data.ParameterDirection.Input;

            dbParam[12] = new System.Data.OleDb.OleDbParameter("@pcCountry", System.Data.OleDb.OleDbType.Char, 3);
            dbParam[12].Value = strCountry;
            dbParam[12].Direction = System.Data.ParameterDirection.Input;

            dbParam[13] = new System.Data.OleDb.OleDbParameter("@pcUIC#2userdefined", System.Data.OleDb.OleDbType.Char, 1);
            dbParam[13].Value = strUIC2userdefined;
            dbParam[13].Direction = System.Data.ParameterDirection.Input;

            dbParam[14] = new System.Data.OleDb.OleDbParameter("@pcUIC#3userdefined", System.Data.OleDb.OleDbType.Char, 1);
            dbParam[14].Value = strUIC3userdefined;
            dbParam[14].Direction = System.Data.ParameterDirection.Input;

            dbParam[15] = new System.Data.OleDb.OleDbParameter("@pcUIC#7userdefined", System.Data.OleDb.OleDbType.Char, 1);
            dbParam[15].Value = strUIC7userdefined;
            dbParam[15].Direction = System.Data.ParameterDirection.Input;

            dbParam[16] = new System.Data.OleDb.OleDbParameter("@pcMotherMaidenName", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[16].Value = strMotherMaidenName;
            dbParam[16].Direction = System.Data.ParameterDirection.Input;

            dbParam[17] = new System.Data.OleDb.OleDbParameter("@pnIdIssuedDateDDMMYY", System.Data.OleDb.OleDbType.Char, 10);
            dbParam[17].Value = dateIDIssuedDate;
            dbParam[17].Direction = System.Data.ParameterDirection.Input;

            dbParam[18] = new System.Data.OleDb.OleDbParameter("@pnTransactionType", System.Data.OleDb.OleDbType.TinyInt);
            dbParam[18].Value = 1;
            dbParam[18].Direction = System.Data.ParameterDirection.Input;

            dbParam[19] = new System.Data.OleDb.OleDbParameter("@pnvGuid", System.Data.OleDb.OleDbType.VarChar, 40);
            dbParam[19].Value = strGuid;
            dbParam[19].Direction = System.Data.ParameterDirection.Input;

            dbParam[20] = new System.Data.OleDb.OleDbParameter("@pncTellerId", System.Data.OleDb.OleDbType.Char, 7);
            dbParam[20].Value = intNIK;
            dbParam[20].Direction = System.Data.ParameterDirection.Input;

            dbParam[21] = new System.Data.OleDb.OleDbParameter("@pcStatusExec", System.Data.OleDb.OleDbType.TinyInt);
            dbParam[21].Value = intStatusExec;
            dbParam[21].Direction = System.Data.ParameterDirection.Output;

            dbParam[22] = new System.Data.OleDb.OleDbParameter("@pnCounterEksekusi", System.Data.OleDb.OleDbType.TinyInt);
            dbParam[22].Value = intCounterEksekusi;
            dbParam[22].Direction = System.Data.ParameterDirection.Input;

            dbParam[23] = new System.Data.OleDb.OleDbParameter("@pnLaporanTxTunai", System.Data.OleDb.OleDbType.Char,2);
            dbParam[23].Value = strLaporanTxTunai;
            dbParam[23].Direction = System.Data.ParameterDirection.Input;

            dbParam[24] = new System.Data.OleDb.OleDbParameter("@pnLaporanPajak", System.Data.OleDb.OleDbType.Char,2);
            dbParam[24].Value = strLaporanPajak;
            dbParam[24].Direction = System.Data.ParameterDirection.Input;

            dbParam[25] = new System.Data.OleDb.OleDbParameter("@pcEmployeeNIK", System.Data.OleDb.OleDbType.Integer);
            dbParam[25].Value = intEmployeeNIK;
            dbParam[25].Direction = System.Data.ParameterDirection.Input;

            dbParam[26] = new System.Data.OleDb.OleDbParameter("@pcEmployeename", System.Data.OleDb.OleDbType.VarChar,40);
            dbParam[26].Value = strEmployeename;
            dbParam[26].Direction = System.Data.ParameterDirection.Input;

            dbParam[27] = new System.Data.OleDb.OleDbParameter("@pcEmployeeIndicator", System.Data.OleDb.OleDbType.Boolean);
            dbParam[27].Value = boolEmployeeIndicator;
            dbParam[27].Direction = System.Data.ParameterDirection.Input;

            dbParam[28] = new System.Data.OleDb.OleDbParameter("@pcUIC#8userdefined", System.Data.OleDb.OleDbType.Char, 1);
            dbParam[28].Value = strUIC8userdefined;
            dbParam[28].Direction = System.Data.ParameterDirection.Input;

            Cursor.Current = Cursors.WaitCursor;
                
            //20100413, indra_w, CIFUPLD008, begin -- BUGS
            //bool blnResult = clsQuery.ExecProc("ProCIFParseParmBasicInfV2Maint", ref dbParam, out dsOutDtPribadi);
            bool blnResult = clsQuery.ExecProc("ProCIFParseParmBasicInfV2Maint", ref dbParam, out dsOutMaintPribadi);
            //20100413, indra_w, CIFUPLD008, end -- BUGS
            if (blnResult == true)
            {
                intStatusExec = int.Parse(dbParam[21].Value.ToString());
                if (intStatusExec != 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }

            }
            else
            {
                intStatusExec = 3;
                return false;
            }

            Cursor.Current = Cursors.Default;
        }

        public bool ProCIFToAccountInquiry()
        {
         
            clsWebService _cWS = new clsWebService();
            bool blnResult = false;
            bool blnResultTibco;

            _cWS.clsWebServiceLoad(intNIK.ToString(), strGuid, strModule);

            if (intMore != 1)
            {
                strRebid = "N";
                blnResultTibco = _cWS.CallCIFInquiryCIFToAccount01565(intCIF.ToString(), "", "", strShortName, "", out strResult01565, ref strRebid);
            }
            else
            {
                if (strAccountType.Trim().Equals("Giro"))
                {
                    strAccountType = "D";
                }
                else if (strAccountType.Trim().Equals("Savings"))
                {
                    strAccountType = "S";
                }
                else if (strAccountType.Trim().Equals("Loan"))
                {
                    strAccountType = "L";
                }
                else if (strAccountType.Equals("Deposito"))
                {
                    strAccountType = "T";
                }

                strRebid = "N";
                blnResultTibco = _cWS.CallCIFInquiryCIFToAccount01565(intCIF.ToString(), strAccountType.Trim(), intAccount.ToString(), strShortName.Trim(), "", out strResult01565, ref strRebid);
            }

            if (blnResultTibco)
            {
                bool blnResultDS = _cWS.ConvertRsSubDetailToXML(strResult01565, out dsOutAccount);

                if (intMore == 1)
                {
                    dsOutAccount.Tables[0].Rows.RemoveAt(0);
                }

                dsOutAccount.Tables[0].Columns[1].SetOrdinal(0);
                dsOutAccount.Tables[0].Columns[3].SetOrdinal(1);
                dsOutAccount.Tables[0].Columns[3].SetOrdinal(2);
                dsOutAccount.Tables[0].Columns[6].SetOrdinal(3);
                dsOutAccount.Tables[0].Columns[7].SetOrdinal(4);
                dsOutAccount.Tables[0].Columns[17].SetOrdinal(5);
                //dsOutAccount.Tables[0].Columns[13].SetOrdinal(6);
                dsOutAccount.Tables[0].Columns[13].SetOrdinal(7);
                dsOutAccount.Tables[0].Columns[14].SetOrdinal(8);
                dsOutAccount.Tables[0].Columns[11].SetOrdinal(9);

                dsOutAccount.Tables[0].Columns[4].ColumnName = "Currency";
                dsOutAccount.Tables[0].Columns[5].ColumnName = "Keterangan";
                dsOutAccount.Tables[0].Columns[6].ColumnName = "Branch";
                dsOutAccount.Tables[0].Columns[7].ColumnName = "ShortName";
                dsOutAccount.Tables[0].Columns[8].ColumnName = "SukuBunga";
                dsOutAccount.Tables[0].Columns[9].ColumnName = "Saldo";

                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                dsOutAccount.Tables[0].Columns.RemoveAt(10);
                
                if (blnResultDS)
                {
                    if (dsOutAccount.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < dsOutAccount.Tables[0].Rows.Count; i++)
                        {
                            dsOutAccount.Tables[0].Rows[i][0] = Int64.Parse(dsOutAccount.Tables[0].Rows[i][0].ToString());
                            dsOutAccount.Tables[0].Rows[i][1] = Int64.Parse(dsOutAccount.Tables[0].Rows[i][1].ToString());
                            dsOutAccount.Tables[0].Rows[i][8] = double.Parse(dsOutAccount.Tables[0].Rows[i][8].ToString()) * 100000000000;
                            dsOutAccount.Tables[0].Rows[i][9] = (double.Parse(dsOutAccount.Tables[0].Rows[i][9].ToString()) * 100).ToString("###,##0.00");
                            //ubah status
                            if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("1"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Active account";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("2"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Closed account";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("3"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Matured account";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("4"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "New account created today";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("5"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Zero balance account";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("6"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Restricted account";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("7"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Frozen account No Dr/Cr";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("8"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Not used";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][5].ToString().Trim().Equals("9"))
                            {
                                dsOutAccount.Tables[0].Rows[i][5] = "Dormant account";
                            }

                            //ubah jenis 
                            if (dsOutAccount.Tables[0].Rows[i][2].ToString().Trim().Equals("D"))
                            {
                                dsOutAccount.Tables[0].Rows[i][2] = "Giro";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][2].ToString().Trim().Equals("S"))
                            {
                                dsOutAccount.Tables[0].Rows[i][2] = "Savings";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][2].ToString().Trim().Equals("L"))
                            {
                                dsOutAccount.Tables[0].Rows[i][2] = "Loan";
                            }
                            else if (dsOutAccount.Tables[0].Rows[i][2].ToString().Trim().Equals("T"))
                            {
                                dsOutAccount.Tables[0].Rows[i][2] = "Deposito";
                            }
                        }
                    }
                    blnResult = blnResultDS;
                }
            }
            
            //System.Data.OleDb.OleDbParameter[] dbParam = new System.Data.OleDb.OleDbParameter[8];

            //dbParam[0] = new System.Data.OleDb.OleDbParameter("@pnTellerID", System.Data.OleDb.OleDbType.Integer);
            //dbParam[0].Value = intNIK;
            //dbParam[0].Direction = System.Data.ParameterDirection.Input;

            //dbParam[1] = new System.Data.OleDb.OleDbParameter("@pcCabang", System.Data.OleDb.OleDbType.Char,5);
            //dbParam[1].Value = strBranch;
            //dbParam[1].Direction = System.Data.ParameterDirection.Input;

            //dbParam[2] = new System.Data.OleDb.OleDbParameter("@pnCustomerNumber", System.Data.OleDb.OleDbType.BigInt);
            //dbParam[2].Value = intCIF;
            //dbParam[2].Direction = System.Data.ParameterDirection.Input;

            //dbParam[3] = new System.Data.OleDb.OleDbParameter("@pcAccountType", System.Data.OleDb.OleDbType.Char, 1);
            //if (intMore == 1)
            //{
            //    dbParam[3].Value = strAccountType;
            //}
            //dbParam[3].Direction = System.Data.ParameterDirection.InputOutput;

            //dbParam[4] = new System.Data.OleDb.OleDbParameter("@pcAccoutNumber", System.Data.OleDb.OleDbType.BigInt);
            //if (intMore == 1)
            //{
            //    dbParam[4].Value = intAccount;
            //}
            //dbParam[4].Direction = System.Data.ParameterDirection.InputOutput;

            //dbParam[5] = new System.Data.OleDb.OleDbParameter("@pcShortName", System.Data.OleDb.OleDbType.Char, 20);
            //dbParam[5].Value = strShortName;
            //dbParam[5].Direction = System.Data.ParameterDirection.Input;

            //dbParam[6] = new System.Data.OleDb.OleDbParameter("@pcAccoutRelationShip", System.Data.OleDb.OleDbType.Integer);
            //dbParam[6].Direction = System.Data.ParameterDirection.Input;

            //dbParam[7] = new System.Data.OleDb.OleDbParameter("@pcRebid", System.Data.OleDb.OleDbType.Char, 1);
            //if (intMore == 1)
            //{
            //    dbParam[7].Value = strRebid;
            //}
            //else
            //{
            //    dbParam[7].Value = "Y";
            //}
            //dbParam[7].Direction = System.Data.ParameterDirection.InputOutput;

            //Cursor.Current = Cursors.WaitCursor;

            //bool blnResult = clsQuery.ExecProc("ProCIFToAccountInquiry", ref dbParam, out dsOutAccount);

            //if (blnResult == true)
            //{
            //    if (dbParam[4].Value.ToString() != "")
            //    {
            //        intAccount = Int64.Parse(dbParam[4].Value.ToString());
            //    }
            //    if (dbParam[3].Value.ToString() != "")
            //    {
            //        strAccountType = dbParam[3].Value.ToString();
            //    }
            //    if (dbParam[7].Value.ToString() != "")
            //    {
            //        strRebid = dbParam[7].Value.ToString();
            //    }
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
            Cursor.Current = Cursors.Default;
            return blnResult;
        }

        public string PrepareParseQuickCreation()
        {
            string strPajak;
            string strForeigenAddress;
            string strBlackList = "N";
            string strHighRisk = "N";
            string strDupID = "N";

            if (strCustomerTypeCode.Equals("C"))
            {
                strPajak = "0";
            }
            else
            {
                strPajak = "5";
            }

            if (boolForeignAddress)
            {
                strForeigenAddress = "Y";
            }
            else
            {
                strForeigenAddress = "N";
                strAddressLine4 = strKELURAHAN + ", " + strDATIII + ' ' + strPostalCode;
            }

            if (strResult01516.Equals("Data tidak ada di Database.(MBFCF01516)"))
            {
                strBlackList = "";
            }
            else
            {
                strBlackList = "Y";
            }

            if (strResult01579.Equals("Data tidak ada di Database.(MBFCF01579)"))
            {
                strHighRisk = "";
            }
            else
            {
                strHighRisk = "Y";
            }

            if(intCounterEksekusi == 0)
            {
                strDupID = "";
            }
            else
            {
                strDupID = "Y";
            }

            GetCurrentDate();

            StringBuilder strRqDetail = new StringBuilder();
            strRqDetail.AppendLine("<ns1:Customername>" + XmlValidasi(strCustomerName1) + "</ns1:Customername>");
            strRqDetail.AppendLine("<ns1:Customername2>" + XmlValidasi(strCustomerName2) + "</ns1:Customername2>");
            strRqDetail.AppendLine("<ns1:TitleBeforeName>" + XmlValidasi(strTitleBefore) + "</ns1:TitleBeforeName>");
            strRqDetail.AppendLine("<ns1:TitleAfterName>" + XmlValidasi(strTitleAfterName) + "</ns1:TitleAfterName>");
            strRqDetail.AppendLine("<ns1:EUR>" + XmlValidasi(dateBirthIncorporation.Remove(2,1).Remove(4,1)) + "</ns1:EUR>");
            strRqDetail.AppendLine("<ns1:Banknumber>" + XmlValidasi("") + "</ns1:Banknumber>");
            strRqDetail.AppendLine("<ns1:Branchnumber>" + XmlValidasi(strBranch) + "</ns1:Branchnumber>");
            strRqDetail.AppendLine("<ns1:Shortname>" + XmlValidasi("") + "</ns1:Shortname>");
            strRqDetail.AppendLine("<ns1:Residentcode>" + XmlValidasi("") + "</ns1:Residentcode>");
            strRqDetail.AppendLine("<ns1:State>" + XmlValidasi("") + "</ns1:State>");
            strRqDetail.AppendLine("<ns1:County>" + XmlValidasi("") + "</ns1:County>");
            strRqDetail.AppendLine("<ns1:Country>" + XmlValidasi("") + "</ns1:Country>");
            strRqDetail.AppendLine("<ns1:Countryofcitizenship>" + XmlValidasi("") + "</ns1:Countryofcitizenship>");
            strRqDetail.AppendLine("<ns1:Countryofheritage>" + XmlValidasi("") + "</ns1:Countryofheritage>");
            strRqDetail.AppendLine("<ns1:DMY1>" + XmlValidasi(TanggalTransaksi.ToString("ddMMyy")) + "</ns1:DMY1>");
            strRqDetail.AppendLine("<ns1:IDnumber>" + XmlValidasi(strIDNumber) + "</ns1:IDnumber>");
            strRqDetail.AppendLine("<ns1:IDtypecode>" + XmlValidasi(strIDTypeCode) + "</ns1:IDtypecode>");
            //20100504, indra_w, CIFUPLD008, begin
            if (dateIDExpiryDate.Trim() == "")
            {
                strRqDetail.AppendLine("<ns1:DMY2/>");
            }
            else
            {
                strRqDetail.AppendLine("<ns1:DMY2>" + XmlValidasi(dateIDExpiryDate.Remove(2, 1).Remove(4, 3)) + "</ns1:DMY2>");
            }
            //20100504, indra_w, CIFUPLD008, end
            strRqDetail.AppendLine("<ns1:Customertypecode>" + XmlValidasi(strCustomerTypeCode) + "</ns1:Customertypecode>");
            strRqDetail.AppendLine("<ns1:Subclass>" + XmlValidasi("") + "</ns1:Subclass>");
            strRqDetail.AppendLine("<ns1:FedWHcode>" + XmlValidasi(strPajak) + "</ns1:FedWHcode>");
            strRqDetail.AppendLine("<ns1:MotherMaidenName>" + XmlValidasi(strMotherMaidenName) + "</ns1:MotherMaidenName>");
            strRqDetail.AppendLine("<ns1:Addressline1>" + XmlValidasi(strAddressLine1) + "</ns1:Addressline1>");
            strRqDetail.AppendLine("<ns1:Addressline2>" + XmlValidasi(strAddressLine2) + "</ns1:Addressline2>");
            strRqDetail.AppendLine("<ns1:Addressline3>" + XmlValidasi(strAddressLine3) + "</ns1:Addressline3>");
            strRqDetail.AppendLine("<ns1:Addressline4>" + XmlValidasi(strAddressLine4) + "</ns1:Addressline4>");
            strRqDetail.AppendLine("<ns1:Postalcode>" + XmlValidasi(strPostalCode) + "</ns1:Postalcode>");
            strRqDetail.AppendLine("<ns1:Electionicaddressdescription>" + XmlValidasi("") + "</ns1:Electionicaddressdescription>");
            strRqDetail.AppendLine("<ns1:Electionicaddressdescription2>" + XmlValidasi("") + "</ns1:Electionicaddressdescription2>");
            strRqDetail.AppendLine("<ns1:Customernumber>" + XmlValidasi("") + "</ns1:Customernumber>");
            strRqDetail.AppendLine("<ns1:Phoneticsearchkey>" + XmlValidasi("") + "</ns1:Phoneticsearchkey>");
            strRqDetail.AppendLine("<ns1:AddressSeqNo>" + XmlValidasi("") + "</ns1:AddressSeqNo>");
            strRqDetail.AppendLine("<ns1:Addresssequenceno>" + XmlValidasi("") + "</ns1:Addresssequenceno>");
            strRqDetail.AppendLine("<ns1:Addresssequenceno2>" + XmlValidasi("") + "</ns1:Addresssequenceno2>");
            strRqDetail.AppendLine("<ns1:UserID>" + XmlValidasi("") + "</ns1:UserID>");
            strRqDetail.AppendLine("<ns1:Jobname>" + XmlValidasi("") + "</ns1:Jobname>");
            strRqDetail.AppendLine("<ns1:Foreignaddress>" + XmlValidasi(strForeigenAddress) + "</ns1:Foreignaddress>");
            strRqDetail.AppendLine("<ns1:Sexcode>" + XmlValidasi("") + "</ns1:Sexcode>");
            strRqDetail.AppendLine("<ns1:BLACKLISTEDFLAG>" + XmlValidasi(strBlackList) + "</ns1:BLACKLISTEDFLAG>");
            strRqDetail.AppendLine("<ns1:Identifier>" + XmlValidasi("") + "</ns1:Identifier>");
            strRqDetail.AppendLine("<ns1:Retention>" + XmlValidasi("") + "</ns1:Retention>");
            strRqDetail.AppendLine("<ns1:Aliasnameflag>" + XmlValidasi("N") + "</ns1:Aliasnameflag>");
            strRqDetail.AppendLine("<ns1:Shortnamesequence>" + XmlValidasi("") + "</ns1:Shortnamesequence>");
            strRqDetail.AppendLine("<ns1:Inquirycode>" + XmlValidasi("") + "</ns1:Inquirycode>");
            strRqDetail.AppendLine("<ns1:Officercode>" + XmlValidasi("") + "</ns1:Officercode>");
            strRqDetail.AppendLine("<ns1:Insidercode>" + XmlValidasi("") + "</ns1:Insidercode>");
            strRqDetail.AppendLine("<ns1:VIPcustomercode>" + XmlValidasi("") + "</ns1:VIPcustomercode>");
            strRqDetail.AppendLine("<ns1:Deceasedcustomerflag>" + XmlValidasi("") + "</ns1:Deceasedcustomerflag>");
            strRqDetail.AppendLine("<ns1:Holdmailcode>" + XmlValidasi("") + "</ns1:Holdmailcode>");
            strRqDetail.AppendLine("<ns1:Profitanalysis>" + XmlValidasi("") + "</ns1:Profitanalysis>");
            strRqDetail.AppendLine("<ns1:SIC1userdefined>" + XmlValidasi("") + "</ns1:SIC1userdefined>");
            strRqDetail.AppendLine("<ns1:SIC2userdefined>" + XmlValidasi("") + "</ns1:SIC2userdefined>");
            strRqDetail.AppendLine("<ns1:SIC3userdefined>" + XmlValidasi("") + "</ns1:SIC3userdefined>");
            strRqDetail.AppendLine("<ns1:SIC4userdefined>" + XmlValidasi("") + "</ns1:SIC4userdefined>");
            strRqDetail.AppendLine("<ns1:SIC5userdefined>" + XmlValidasi("") + "</ns1:SIC5userdefined>");
            strRqDetail.AppendLine("<ns1:SIC6userdefined>" + XmlValidasi("") + "</ns1:SIC6userdefined>");
            strRqDetail.AppendLine("<ns1:SIC7userdefined>" + XmlValidasi("") + "</ns1:SIC7userdefined>");
            strRqDetail.AppendLine("<ns1:SIC8userdefined>" + XmlValidasi("") + "</ns1:SIC8userdefined>");
            strRqDetail.AppendLine("<ns1:Customernamecontrol>" + XmlValidasi("") + "</ns1:Customernamecontrol>");
            strRqDetail.AppendLine("<ns1:Dateoflastmaintenance>" + XmlValidasi(TanggalTransaksi.ToString("ddMMyy")) + "</ns1:Dateoflastmaintenance>");
            strRqDetail.AppendLine("<ns1:CustomerReviewDateDDMMYY>" + XmlValidasi("000000") + "</ns1:CustomerReviewDateDDMMYY>");
            //BirthincorporationdateYYYYDDD = format tanggal yang diinginkan ddMMyy
            strRqDetail.AppendLine("<ns1:BirthincorporationdateYYYYDDD>" + XmlValidasi(dateBirthIncorporation.Remove(2, 1).Remove(4, 3)) + "</ns1:BirthincorporationdateYYYYDDD>");
            strRqDetail.AppendLine("<ns1:Individualindicator>" + XmlValidasi("") + "</ns1:Individualindicator>");
            strRqDetail.AppendLine("<ns1:SMSAcode>" + XmlValidasi("") + "</ns1:SMSAcode>");
            strRqDetail.AppendLine("<ns1:Businesstype>" + XmlValidasi("") + "</ns1:Businesstype>");
            strRqDetail.AppendLine("<ns1:Creditrating>" + XmlValidasi("") + "</ns1:Creditrating>");
            strRqDetail.AppendLine("<ns1:CIFgroupcode>" + XmlValidasi("") + "</ns1:CIFgroupcode>");
            strRqDetail.AppendLine("<ns1:CIFcombinedcycle>" + XmlValidasi("") + "</ns1:CIFcombinedcycle>");
            strRqDetail.AppendLine("<ns1:TINstatus>" + XmlValidasi("") + "</ns1:TINstatus>");
            strRqDetail.AppendLine("<ns1:StateWHcode>" + XmlValidasi(strPajak) + "</ns1:StateWHcode>");
            strRqDetail.AppendLine("<ns1:FedWHdateMMDDYY>" + XmlValidasi("000000") + "</ns1:FedWHdateMMDDYY>");
            strRqDetail.AppendLine("<ns1:UIC1userdefined>" + XmlValidasi("") + "</ns1:UIC1userdefined>");
            strRqDetail.AppendLine("<ns1:UIC2userdefined>" + XmlValidasi("") + "</ns1:UIC2userdefined>");
            strRqDetail.AppendLine("<ns1:UIC3userdefined>" + XmlValidasi("") + "</ns1:UIC3userdefined>");
            strRqDetail.AppendLine("<ns1:UIC4userdefined>" + XmlValidasi("") + "</ns1:UIC4userdefined>");
            strRqDetail.AppendLine("<ns1:UIC5userdefined>" + XmlValidasi("") + "</ns1:UIC5userdefined>");
            strRqDetail.AppendLine("<ns1:UIC6userdefined>" + XmlValidasi("") + "</ns1:UIC6userdefined>");
            strRqDetail.AppendLine("<ns1:UIC7userdefined>" + XmlValidasi("") + "</ns1:UIC7userdefined>");
            strRqDetail.AppendLine("<ns1:UIC8userdefined>" + XmlValidasi("") + "</ns1:UIC8userdefined>");
            strRqDetail.AppendLine("<ns1:Customerstatus>" + XmlValidasi("") + "</ns1:Customerstatus>");
            strRqDetail.AppendLine("<ns1:IDIssuePlace>" + XmlValidasi("") + "</ns1:IDIssuePlace>");
            strRqDetail.AppendLine("<ns1:IDStatusCode>" + XmlValidasi("") + "</ns1:IDStatusCode>");
            strRqDetail.AppendLine("<ns1:BirthIncorporationdate>" + XmlValidasi(strBirthPlace) + "</ns1:BirthIncorporationdate>");
            strRqDetail.AppendLine("<ns1:InternalIndustrycode>" + XmlValidasi("") + "</ns1:InternalIndustrycode>");
            strRqDetail.AppendLine("<ns1:GeographicalLocationcode>" + XmlValidasi("") + "</ns1:GeographicalLocationcode>");
            strRqDetail.AppendLine("<ns1:DebitACNumber>" + XmlValidasi("") + "</ns1:DebitACNumber>");
            strRqDetail.AppendLine("<ns1:DebitACType>" + XmlValidasi("") + "</ns1:DebitACType>");
            strRqDetail.AppendLine("<ns1:ECRDraweeCode>" + XmlValidasi("") + "</ns1:ECRDraweeCode>");
            strRqDetail.AppendLine("<ns1:DrawerNumber>" + XmlValidasi("") + "</ns1:DrawerNumber>");
            strRqDetail.AppendLine("<ns1:Lastchangedbyuser>" + XmlValidasi(intNIK.ToString()) + "</ns1:Lastchangedbyuser>");
            strRqDetail.AppendLine("<ns1:LastchangedDDMMYY>" + XmlValidasi(TanggalTransaksi.ToString("ddMMyy")) + "</ns1:LastchangedDDMMYY>");
            strRqDetail.AppendLine("<ns1:Lastchangedtime>" + XmlValidasi(TanggalTransaksi.ToString("hhmmss")) + "</ns1:Lastchangedtime>");
            strRqDetail.AppendLine("<ns1:Noofstatementcopies>" + XmlValidasi("") + "</ns1:Noofstatementcopies>");
            strRqDetail.AppendLine("<ns1:UseinMBVALCF4>" + XmlValidasi("") + "</ns1:UseinMBVALCF4>");
            strRqDetail.AppendLine("<ns1:InformationDateDMY>" + XmlValidasi("") + "</ns1:InformationDateDMY>");
            strRqDetail.AppendLine("<ns1:Dummy>" + XmlValidasi("") + "</ns1:Dummy>");
            strRqDetail.AppendLine("<ns1:ReviewDateDMY>" + XmlValidasi("") + "</ns1:ReviewDateDMY>");
            strRqDetail.AppendLine("<ns1:DUPIDFLAG>" + XmlValidasi(strDupID) + "</ns1:DUPIDFLAG>");
            strRqDetail.AppendLine("<ns1:BusinessUnitCode>" + XmlValidasi("") + "</ns1:BusinessUnitCode>");
            strRqDetail.AppendLine("<ns1:CIFGroupMemberCode>" + XmlValidasi("") + "</ns1:CIFGroupMemberCode>");
            strRqDetail.AppendLine("<ns1:HIGHRISKCUSTOMTERFLAG>" + XmlValidasi(strHighRisk) + "</ns1:HIGHRISKCUSTOMTERFLAG>");
            //20100504, indra_w, CIFUPLD008, begin
            //strRqDetail.AppendLine("<ns1:IdIssuedDateDDMMYY>" + XmlValidasi("") + "</ns1:IdIssuedDateDDMMYY>");
            //strRqDetail.AppendLine("<ns1:IdIssuedDateYYYYDDD>" + XmlValidasi("") + "</ns1:IdIssuedDateYYYYDDD>");
            if (dateIDIssuedDate.Trim() == "")
            {
                strRqDetail.AppendLine("<ns1:IdIssuedDateDDMMYY/>");
                strRqDetail.AppendLine("<ns1:IdIssuedDateYYYYDDD/>");
            }
            else
            {
                strRqDetail.AppendLine("<ns1:IdIssuedDateDDMMYY>" + XmlValidasi(dateIDIssuedDate.Remove(2, 1).Remove(4, 3)) + "</ns1:IdIssuedDateDDMMYY>");
                strRqDetail.AppendLine("<ns1:IdIssuedDateYYYYDDD>" + XmlValidasi(DateToJulian(System.Convert.ToDateTime(dateIDIssuedDate.Substring(6, 4) + "/" + dateIDIssuedDate.Substring(3, 2) + "/" + dateIDIssuedDate.Substring(0, 2)))) + "</ns1:IdIssuedDateYYYYDDD>");
            }
            //20100504, indra_w, CIFUPLD008, end
            strRqDetail.AppendLine("<ns1:Electionicaddressdescription3>" + XmlValidasi("") + "</ns1:Electionicaddressdescription3>");
            strRqDetail.AppendLine("<ns1:Electionicaddressdescription4>" + XmlValidasi("") + "</ns1:Electionicaddressdescription4>");
            strRqDetail.AppendLine("<ns1:Contactname>" + XmlValidasi("") + "</ns1:Contactname>");
            strRqDetail.AppendLine("<ns1:KELURAHAN>" + XmlValidasi(strKELURAHAN) + "</ns1:KELURAHAN>");
            strRqDetail.AppendLine("<ns1:KECAMATAN>" + XmlValidasi(strKECAMATAN) + "</ns1:KECAMATAN>");
            strRqDetail.AppendLine("<ns1:DATIII>" + XmlValidasi(strDATIII) + "</ns1:DATIII>");
            strRqDetail.AppendLine("<ns1:PROVINSI>" + XmlValidasi(strPROVINSI) + "</ns1:PROVINSI>");
            strRqDetail.AppendLine("<ns1:CODELAPTXTUNAI>" + XmlValidasi("") + "</ns1:CODELAPTXTUNAI>");
            strRqDetail.AppendLine("<ns1:BIrthincorporationdateYYYYDDD2>" + XmlValidasi(DateToJulian(System.Convert.ToDateTime(dateBirthIncorporation.Substring(6, 4) + "/" + dateBirthIncorporation.Substring(3, 2) + "/" + dateBirthIncorporation.Substring(0, 2)))) + "</ns1:BIrthincorporationdateYYYYDDD2>");
            strRqDetail.AppendLine("<ns1:USERENTRY>" + XmlValidasi("") + "</ns1:USERENTRY>");
            strRqDetail.AppendLine("<ns1:EUR2>" + XmlValidasi("") + "</ns1:EUR2>");
            //20100504, indra_w, CIFUPLD008, begin
            if (dateIDExpiryDate.Trim() == "")
            {
                strRqDetail.AppendLine("<ns1:IDExpirydate/>");
            }
            else
            {
                //strRqDetail.AppendLine("<ns1:IDExpirydate>" + XmlValidasi(DateToJulian(DateTime.Parse(dateIDExpiryDate))) + "</ns1:IDExpirydate>");
                strRqDetail.AppendLine("<ns1:IDExpirydate>" + XmlValidasi(DateToJulian(System.Convert.ToDateTime(dateIDExpiryDate.Substring(6, 4) + "/" + dateIDExpiryDate.Substring(3, 2) + "/" + dateIDExpiryDate.Substring(0, 2)))) + "</ns1:IDExpirydate>");
            }
            //20100504, indra_w, CIFUPLD008, end
            strRqDetail.AppendLine("<ns1:EUR3>" + XmlValidasi("") + "</ns1:EUR3>");
            strRqDetail.AppendLine("<ns1:KODEDATACENTER>" + XmlValidasi("") + "</ns1:KODEDATACENTER>");
            strRqDetail.AppendLine("<ns1:OriginalcustomerdateYYYYDDD>" + XmlValidasi(DateToJulian(TanggalTransaksi)) + "</ns1:OriginalcustomerdateYYYYDDD>");

            return strRqDetail.ToString();
        }

        private String XmlValidasi(String strChar)
        {
            strChar = strChar + "";
            strChar = strChar.Replace("&", "&amp;");
            strChar = strChar.Replace("<", "&lt;");
            strChar = strChar.Replace(">", "&gt;");

            return strChar;
        }

        public void GetCurrentDate()
        {
            bool blnResult = clsQuery.ExecProc("ProCIFP3GetCurrentDate", out dsOutCurrentDate);

            if (blnResult)
            {
                TanggalTransaksi = DateTime.Parse(dsOutCurrentDate.Tables[0].Rows[0][0].ToString());
                TanggalSekarang = DateTime.Parse(dsOutCurrentDate.Tables[0].Rows[0][1].ToString());
            }
            else
            {
                MessageBox.Show("Gagal Ambil Tanggal Transaksi!", "Warning");
            }

        }

        public String DateToJulian(DateTime date)
        {
            //System.Globalization.JulianCalendar myCal = new System.Globalization.JulianCalendar();
            string hariKe;
            string tahun;

            hariKe = "000"+date.DayOfYear.ToString().Trim();
            hariKe = hariKe.Substring(hariKe.Length - 3, 3);
            tahun = date.Year.ToString();

            return tahun + hariKe;

        }

    }
}
