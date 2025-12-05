using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text;

namespace BankNISP.Obligasi01
{
    public class clsDatabase
    {
        public static bool subUserGetToolbar(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {

            string strCommand = "UserGetToolbar";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnNIK", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pcModule", OleDbType.VarChar, 30);
            dbParam[2] = new OleDbParameter("@pcMenuName", OleDbType.VarChar, 50);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateBankCustody(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateBankCustody";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pbIsApproval", OleDbType.Boolean);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessBankCustody(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessBankCustody";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateSeller(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSeller";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pbIsApproval", OleDbType.Boolean);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessSeller(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSeller";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateSecurityFee(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSecurityFee";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSCurrentWorkingDate(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSCurrentWorkingDate";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subtrs_ListSecurityInterestType_TR(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "trs_ListSecurityInterestType_TR";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@whereCond", OleDbType.VarChar, 1000);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSGetSecurityCurrency(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSGetSecurityCurrency";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subtrs_ListSecurityMaster_TM(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "trs_ListSecurityMaster_TM";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@whereCond", OleDbType.VarChar, 1000);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20200219, rezakahfi, BONDRETAIL-188, begin
        public static bool subtrs_ListSecurityMasterPending_TM(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "dbo.trs_ListSecurityMasterPending_TM";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@whereCond", OleDbType.VarChar, 1000);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20200219, rezakahfi, BONDRETAIL-188, end

        public static bool subtrs_ListColumnsTable(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "trs_ListColumnsTable";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@sTableName", OleDbType.VarChar, 150);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subtrs_UpdateSecurityMaster_TM(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "trs_UpdateSecurityMaster_TM";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@xmlInput", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subtrs_InsertSecurityMaster_TM(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "trs_InsertSecurityMaster_TM";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@xmlInput", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subtrs_SearchSecId(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "trs_SearchSecId";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@SecId", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subtrs_DeleteSecurityMaster_TM(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "trs_DeleteSecurityMaster_TM";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@SecId", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@nNIK", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateSecurityMasterAuthorization(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSecurityMasterAuthorization";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSGetMenuSettings(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSGetMenuSettings";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcMenuName", OleDbType.VarChar, 255);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSGetBankCustody(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSGetBankCustody";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcBankCode", OleDbType.VarChar, 2);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessSecurityMaster(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSecurityMaster";
            int paramCount = 4;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLDataSecurity", OleDbType.VarChar);
            dbParam[3] = new OleDbParameter("@pcXMLDataSKF", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSecurityMasterAuthorization(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSecurityMasterAuthorization";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSecurityFee(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSecurityFee";
            int paramCount = 7;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLDataSecurityMaster", OleDbType.VarChar);
            dbParam[3] = new OleDbParameter("@pcXMLDataFee_1_1", OleDbType.VarChar);
            dbParam[4] = new OleDbParameter("@pcXMLDataFee_1_2", OleDbType.VarChar);
            dbParam[5] = new OleDbParameter("@pcXMLDataFee_2_1", OleDbType.VarChar);
            dbParam[6] = new OleDbParameter("@pcXMLDataFee_2_2", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateSecurityFeeAuthorization(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSecurityFeeAuthorization";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSProcessSecurityFeeAuthorization(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSecurityFeeAuthorization";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessEditCustomerAuthorization(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessEditCustomerAuthorization";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateDeleteORIOrder(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateDeleteORIOrder";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@pbIsApproval", OleDbType.Boolean);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessDeleteORIOrder(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessDeleteORIOrder";
            int paramCount = 4;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[3] = new OleDbParameter("@pbCommitImmediately", OleDbType.Boolean);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessDeleteORIOrderCommit(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessDeleteORIOrderCommit";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateSettlementBuyBack(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementBuyBack";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateBuyBack(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateBuyBack";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateSettlementMaturity(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementMaturity";
            //20160202, fauzil, TRBST16240, begin
            //int paramCount = 1;
            int paramCount = 2;
            //20160202, fauzil, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@piStatus", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateSettlementCashBack(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementCashBack";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateSettlementOrder(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementOrder";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20160405, samy, TRBST16240, begin
        public static bool subTRSPopulateSettlementOrderByStatus(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementOrderByStatus";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcStatus", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20160405, samy, TRBST16240, end
        public static bool subTRSPopulateSettlementTransaksiBankJual(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementTransaksiBankJual";
            //20160701, fauzil, TRBST16240, begin
            //int paramCount = 1;
            int paramCount = 2;
            //20160701, fauzil, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            //20160701, fauzil, TRBST16240, begin
            dbParam[1] = new OleDbParameter("@pcSecurityNo", OleDbType.Integer);
            //20160701, fauzil, TRBST16240, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessSettlementBuyBack(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementBuyBack";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSettlementTransaksiBankJual(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementTransaksiBankJual";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessBuyBack(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessBuyBack";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSettlementMaturity(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementMaturity";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSettlementCashBack(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementCashBack";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSettlementOrder(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementOrder";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateAllotment(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateAllotment";
            //20170417, agireza, TRBST16240, begin
            //int paramCount = 1;
            //OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            //dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pcDataType", OleDbType.VarChar);
            //20170417, agireza, TRBST16240, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessAllotment(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessAllotment";
            //20170413, agireza, TRBST16240, begin
            //int paramCount = 3;
            int paramCount = 4;
            //20170413, agireza, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pbUpdateNotInFile", OleDbType.Boolean);
            //20170413, agireza, TRBST16240, begin
            dbParam[3] = new OleDbParameter("@pcTypeData", OleDbType.Char);
            //20170413, agireza, TRBST16240, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessInsertORIOrderCommit(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSProcessInsertORIOrderCommit";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcAccountBlockACTYPE", OleDbType.Char, 1);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessCancelORIOrderCommit(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessCancelORIOrderCommit";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessInsertSecurityTransactionCommit(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSProcessInsertSecurityTransactionCommit";
            //20160617, fauzil, TRBST16240, begin
            int paramCount = 5;
            //int paramCount = 7;
            //20160617, fauzil, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pcXMLDataTransactionLink", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pcAccountBlockACTYPE", OleDbType.Char, 1);
            dbParam[4] = new OleDbParameter("@pbClearUnpaidSafeKeepingFee", OleDbType.Boolean);
            //20160617, fauzil, TRBST16240, begin
            //dbParam[5] = new OleDbParameter("@pnBlokirAmount", OleDbType.Decimal);
            //dbParam[6] = new OleDbParameter("@pnNoRekInvestor", OleDbType.VarChar);
            //20160617, fauzil, TRBST16240, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        //20180409, uzia, LOGEN00606, begin
        public static bool subTRSProcessInsertSecurityTransactionCommit(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult, int nNIKTreasury)
        {
            string strCommand = "TRSProcessInsertSecurityTransactionCommit";

            int paramCount = 5;

            OleDbParameter[] dbParam = new OleDbParameter[11];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pcXMLDataTransactionLink", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pcAccountBlockACTYPE", OleDbType.Char, 1);
            dbParam[4] = new OleDbParameter("@pbClearUnpaidSafeKeepingFee", OleDbType.Boolean);

            dbParam[5] = new OleDbParameter("@piSignature", null);
            dbParam[6] = new OleDbParameter("@pbisOnlineAcc", false);
            dbParam[7] = new OleDbParameter("@piSummary", null);
            dbParam[8] = new OleDbParameter("@pbIsSIDNull", false);
            dbParam[9] = new OleDbParameter("@pcXMLDocs", null);
            dbParam[10] = new OleDbParameter("@pnTreasuryNIK", nNIKTreasury);

            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20180409, uzia, LOGEN00606, end
        //20200917, rezakahfi, BONDRETAIL-550, begin
        public static bool subTRSProcessInsertSecurityTransactionCommit(ObligasiQuery obligasiQuery, object[] objParams
                                                                        , out DataSet dsResult, int nNIKTreasury, string xmlSumberDana
                )
        {
            string strCommand = "TRSProcessInsertSecurityTransactionCommit";

            int paramCount = 5;

            OleDbParameter[] dbParam = new OleDbParameter[12];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pcXMLDataTransactionLink", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pcAccountBlockACTYPE", OleDbType.Char, 1);
            dbParam[4] = new OleDbParameter("@pbClearUnpaidSafeKeepingFee", OleDbType.Boolean);

            dbParam[5] = new OleDbParameter("@piSignature", null);
            dbParam[6] = new OleDbParameter("@pbisOnlineAcc", false);
            dbParam[7] = new OleDbParameter("@piSummary", null);
            dbParam[8] = new OleDbParameter("@pbIsSIDNull", false);
            dbParam[9] = new OleDbParameter("@pcXMLDocs", null);
            dbParam[10] = new OleDbParameter("@pnTreasuryNIK", nNIKTreasury);
            dbParam[11] = new OleDbParameter("@pcXMLSumberDana", xmlSumberDana);

            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20200917, rezakahfi, BONDRETAIL-550, end

        public static bool subTRSProcessDeleteSecurityTransactionCommit(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessDeleteSecurityTransactionCommit";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSAmendSecurityTransaction_TT_Commit(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSAmendSecurityTransaction_TT_Commit";
            //20160617, samypasha, TRBST16240, begin
            //int paramCount = 1;
            //20210315, rezakahfi, BONDRETAIL-703, begin
            //int paramCount = 2;
            int paramCount = 3;
            //20210315, rezakahfi, BONDRETAIL-703, end
            //20160617, samypasha, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityTransaction_TT_XML", OleDbType.VarChar);
            //20160617, samypasha, TRBST16240, begin
            dbParam[1] = new OleDbParameter("@pnBlokirAmount", OleDbType.Decimal);
            //20160617, samypasha, TRBST16240, end
            //20210315, rezakahfi, BONDRETAIL-703, begin
            dbParam[2] = new System.Data.OleDb.OleDbParameter("@pcXMLSumberDana", System.Data.OleDb.OleDbType.VarChar);
            //20210315, rezakahfi, BONDRETAIL-703, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subtrs_ListTreasuryCustomer_TM_Original(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "trs_ListTreasuryCustomer_TM_Original";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@SecAccNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@pcCcyCode", OleDbType.VarChar, 4);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        //20160407, samypasha, LOGEN0012, begin
        //public static bool subTRSPopulateEditCustomerAuthorization(ObligasiQuery obligasiQuery, out DataSet dsResult)
        //20171129, samypasha, TRBST16240, begin
        //public static bool subTRSPopulateEditCustomerAuthorization(ObligasiQuery obligasiQuery, string cUserBranch, out DataSet dsResult)
        public static bool subTRSPopulateEditCustomerAuthorization(ObligasiQuery obligasiQuery, string cUserBranch, int nUserNIK, out DataSet dsResult)
        //20171129, samypasha, TRBST16240, end
        //20160407, samypasha, LOGEN0012, end
        {
            string strCommand = "TRSPopulateEditCustomerAuthorization";

            //20160407, samypasha, LOGEN0012, begin
            //return (obligasiQuery.ExecProc(strCommand, out dsResult));
            //20171129, samypasha, TRBST16240, begin
            //System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[1];
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[2];
            //20171129, samypasha, TRBST16240, end

            odpParam[0] = new System.Data.OleDb.OleDbParameter();
            odpParam[0].OleDbType = System.Data.OleDb.OleDbType.VarChar;
            odpParam[0].Value = cUserBranch;

            //20171129, samypasha, TRBST16240, begin
            odpParam[1] = new System.Data.OleDb.OleDbParameter();
            odpParam[1].OleDbType = System.Data.OleDb.OleDbType.Integer;
            odpParam[1].Value = nUserNIK;
            //20171129, samypasha, TRBST16240, end

            return (obligasiQuery.ExecProc(strCommand, ref odpParam, out dsResult));
            //20160407, samypasha, LOGEN0012, end
        }

        public static bool subTRSPopulateTransactionBalance(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateTransactionBalance";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecAccNo", OleDbType.VarChar, 10);
            dbParam[1] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateMaturityUpload(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateMaturityUpload";
            //20230221, tobias, BONDRETAIL-1245, begin
            //int paramCount = 2;
            int paramCount = 4;
            //20230221, tobias, BONDRETAIL-1245, begin
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            //20230221, tobias, BONDRETAIL-1245, begin
            dbParam[2] = new OleDbParameter("@pcGuid", OleDbType.VarChar);
            dbParam[3] = new OleDbParameter("@pcNIK", OleDbType.Integer);
            //20230221, tobias, BONDRETAIL-1245, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessMaturityUpload(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessMaturityUpload";
            //20230221, tobias, BONDRETAIL-1245, begin
            //int paramCount = 3;
            int paramCount = 4;
            //20230221, tobias, BONDRETAIL-1245, begin
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pcXMLFromDatabase", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pcXMLFromCustody", OleDbType.VarChar);
            //20230221, tobias, BONDRETAIL-1245, begin
            dbParam[3] = new OleDbParameter("@pcGuid", OleDbType.VarChar);
            //20230221, tobias, BONDRETAIL-1245, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateMaturityUploadAuthorization(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateMaturityUploadAuthorization";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessMaturityUploadAuthorization(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessMaturityUploadAuthorization";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateUploadRefund(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateUploadRefund";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessUploadRefund(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessUploadRefund";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSUpdateOrderPriority(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSUpdateOrderPriority";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnSecId", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSSplitOrder(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSSplitOrder";
            int paramCount = 5;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnOrderId", OleDbType.BigInt);
            dbParam[1] = new OleDbParameter("@pnOrderNominal", OleDbType.Decimal);
            dbParam[2] = new OleDbParameter("@pnNominalBuyBack", OleDbType.Decimal);
            dbParam[3] = new OleDbParameter("@pnNominalCashBack", OleDbType.Decimal);
            dbParam[4] = new OleDbParameter("@pbUpdateToPriority0", OleDbType.Boolean);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20130222, victor, BAALN12003, begin
        //20140904, samypasha, LOGAM06620, tambah 2 parameter, begin
        //public static void GetBlockAmount(ObligasiQuery obligasiQuery, string ProductCode, decimal OriginalAmount, out decimal CalculatedAmount)
        //{
        //    CalculatedAmount = 0;
        //    string strCommand = "dbo.TRSGetBlokirAmount";
        //    int paramCount = 3;
        //    OleDbParameter[] dbParam = new OleDbParameter[paramCount];
        //    (dbParam[0] = new OleDbParameter("@pcProductCode", OleDbType.VarChar, 10)).Value = ProductCode;
        //    (dbParam[1] = new OleDbParameter("@pmOriginalAmount", OleDbType.Double)).Value = OriginalAmount;
        //    dbParam[2] = new OleDbParameter("@pmCalculatedAmount", OleDbType.Double);
        //    dbParam[2].Direction = ParameterDirection.Output;
        //    if (obligasiQuery.ExecProc(strCommand, ref dbParam))
        //    {
        //        CalculatedAmount = decimal.Parse(dbParam[2].Value.ToString());
        //    }



        //}
        public static void GetBlockAmount(ObligasiQuery obligasiQuery, string ProductCode, decimal OriginalAmount, int CIFId, string xmlParam, out decimal CalculatedAmount)
        {
            CalculatedAmount = 0;
            string strCommand = "dbo.TRSGetBlokirAmount";
            int paramCount = 5;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            (dbParam[0] = new OleDbParameter("@pcProductCode", OleDbType.VarChar, 10)).Value = ProductCode;
            (dbParam[1] = new OleDbParameter("@pmOriginalAmount", OleDbType.Double)).Value = OriginalAmount;
            (dbParam[2] = new OleDbParameter("@pnCIFId", OleDbType.BigInt)).Value = CIFId;
            (dbParam[3] = new OleDbParameter("@pcXML", OleDbType.VarChar)).Value = xmlParam;
            dbParam[4] = new OleDbParameter("@pmCalculatedAmount", OleDbType.Double);
            dbParam[4].Direction = ParameterDirection.Output;
            if (obligasiQuery.ExecProc(strCommand, ref dbParam))
            {
                CalculatedAmount = decimal.Parse(dbParam[4].Value.ToString());
            }



        }
        //20140904, samypasha, LOGAM06620, tambah 2 parameter, end
        //20130222, victor, BAALN12003, end
        //20130218, uzia, BAALN12003, begin
        #region Parameter Accrued Days
        public bool ParamAccruedDaysPopulate(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            return obligasiQuery.ExecProc("dbo.TRSParamAccruedDaysPopulate", out dsResult);
        }

        public bool ParamAccruedDaysSave(ObligasiQuery obligasiQuery, string strXMLInput, int nNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXMLInput", strXMLInput);
            dbPar[1] = new OleDbParameter("@pnNIK", nNIK);
            return obligasiQuery.ExecProc("dbo.TRSParamAccruedDaysSave", ref dbPar);
        }

        public bool ParamAccruedDaysAuthPopulate(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            return obligasiQuery.ExecProc("dbo.TRSParamAccruedDaysAuthPopulate", out dsResult);
        }

        public bool ParamAccruedDaysAuthProcess(ObligasiQuery obligasiQuery, bool bApprove, int nNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pbApproveBit", bApprove);
            dbPar[1] = new OleDbParameter("@pnNIK", nNIK);

            return obligasiQuery.ExecProc("dbo.TRSParamAccruedDaysAuthProcess", ref dbPar);
        }

        public bool ParamAccruedDaysGetDefaultSettlementDate(ObligasiQuery obligasiQuery, string strSecurityNo, out int nDefaultSettlementDate)
        {
            bool bOK = false;
            nDefaultSettlementDate = 0;

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcSecurityNo", strSecurityNo);
            dbPar[1] = new OleDbParameter("@pnSettlementDate", OleDbType.Integer);
            dbPar[1].Direction = ParameterDirection.Output;

            bOK = obligasiQuery.ExecProc("dbo.TRSGetDefaultSettlementDate", ref dbPar);

            if (bOK)
                nDefaultSettlementDate = int.Parse(dbPar[1].Value.ToString());

            return bOK;
        }

        public bool ParamAccruedDaysGetNormalAccruedDays(ObligasiQuery obligasiQuery, string strSecurityNo, int nSettlementDate, out int nAccruedDays)
        {
            bool bOK = false;
            nAccruedDays = 0;

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcSecurityNo", strSecurityNo);
            dbPar[1] = new OleDbParameter("@pnSettlementDate", nSettlementDate);
            dbPar[2] = new OleDbParameter("@pnAccruedDays", OleDbType.Integer);
            dbPar[2].Direction = ParameterDirection.Output;

            bOK = obligasiQuery.ExecProc("dbo.TRSGetNormalAccuredDays", ref dbPar);

            if (bOK)
                nAccruedDays = int.Parse(dbPar[2].Value.ToString());

            return bOK;
        }

        #endregion
        //20130218, uzia, BAALN12003, end
        //20130705, uzia, ODOSS12018, begin        
        #region Toolbar Access
        public bool ToolbarButtonPopulate(ObligasiQuery cQuery, int nNIK, string strModule, string strMenuName, out string[] strVisible)
        {
            DataSet dsToolbar = new DataSet();
            bool bOK = false;
            strVisible = new string[0];

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnNIK", nNIK);
            dbPar[1] = new OleDbParameter("@pcModule", strModule);
            dbPar[2] = new OleDbParameter("@pcMenuName", strMenuName);

            bOK = cQuery.ExecProc("dbo.UserGetToolbar", ref dbPar, out dsToolbar);

            if (bOK)
            {
                strVisible = new string[dsToolbar.Tables[0].Rows.Count];
                for (int i = 0; i < dsToolbar.Tables[0].Rows.Count; i++)
                {
                    strVisible[i] = dsToolbar.Tables[0].Rows[i]["IconId"].ToString();
                }
            }
            return bOK;
        }
        #endregion

        #region Custody Upload
        public bool CustodyUploadGetGeneralParam(ObligasiQuery cQuery, out DataSet dsOut)
        {
            dsOut = new DataSet();

            return cQuery.ExecProc("dbo.TRSCustodyUploadGetGeneralParam", out dsOut);
        }

        public bool CustodyUploadGetFileParam(ObligasiQuery cQuery, int nFileTypeId, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnFileTypeId", nFileTypeId);

            return cQuery.ExecProc("dbo.TRSCustodyUploadGetFileParam", ref dbPar, out dsOut);
        }

        public bool CustodyUploadSaveData(ObligasiQuery cQuery, string strXml, int nFileTypeId, string strPeriodType, string strFileName, int nNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[5];
            dbPar[0] = new OleDbParameter("@pcXmlData", strXml);
            dbPar[1] = new OleDbParameter("@pnFileTypeId", nFileTypeId);
            dbPar[2] = new OleDbParameter("@pcPeriodType", strPeriodType);
            dbPar[3] = new OleDbParameter("@pcFileName", strFileName);
            dbPar[4] = new OleDbParameter("@pnNIK", nNIK);

            return cQuery.ExecProc("dbo.TRSCustodyUploadSaveData", ref dbPar);
        }

        public bool CustodyUploadPopulateAuthMain(ObligasiQuery cQuery, string strPeriodType, out DataSet dsOut)
        {
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcPeriodType", strPeriodType);

            return cQuery.ExecProc("dbo.TRSCustodyUploadPopulatePendingAuthMain", ref dbPar, out dsOut);
        }

        public bool CustodyUploadPopulateAuthDetail(ObligasiQuery cQuery, long nHistId, out DataSet dsOut)
        {
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnHistId", nHistId);

            return cQuery.ExecProc("dbo.TRSCustodyUploadPopulatePendingAuthDetail", ref dbPar, out dsOut);
        }

        public bool CustodyUploadProcessAuth(ObligasiQuery cQuery, long nHistId, bool bApprove, int nNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnHistId", nHistId);
            dbPar[1] = new OleDbParameter("@pbApprove", bApprove);
            dbPar[2] = new OleDbParameter("@pnNIK", nNIK);

            return cQuery.ExecProc("dbo.TRSCustodyUploadProcessAuth", ref dbPar);
        }

        public bool CustodyUploadValidateCutoff(ObligasiQuery cQuery, string strPeriodType, out bool bCanProcess)
        {
            bCanProcess = false;
            bool bOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcPeriodType", strPeriodType);
            dbPar[1] = new OleDbParameter("@pbCanProcess", OleDbType.Boolean);
            dbPar[1].Direction = ParameterDirection.Output;

            bOK = cQuery.ExecProc("dbo.TRSCustodyUploadValidateCutoff", ref dbPar);

            if (bOK)
                bCanProcess = bool.Parse(dbPar[1].Value.ToString());

            return bOK;
        }

        public bool CustodyUploadCheckExisting(ObligasiQuery cQuery, int nFileTypeId, string strPeriodType, out bool bExists)
        {
            bExists = false;
            bool bOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnFileTypeId", nFileTypeId);
            dbPar[1] = new OleDbParameter("@pcPeriodType", strPeriodType);
            dbPar[2] = new OleDbParameter("@pbExists", OleDbType.Boolean);
            dbPar[2].Direction = ParameterDirection.Output;

            bOK = cQuery.ExecProc("dbo.TRSCustodyUploadCheckExistingData", ref dbPar);

            if (bOK)
                bExists = bool.Parse(dbPar[2].Value.ToString());

            return bOK;
        }

        public bool CustodyUploadMonitorFile(ObligasiQuery cQuery, out DataSet dsOut)
        {
            dsOut = new DataSet();

            return cQuery.ExecProc("dbo.TRSCustodyMonitorFile", out dsOut);
        }

        #endregion

        //20130705, uzia, ODOSS12018, end
        //20140403, samy, TRODD14052, begin
        public static bool GetComboItem(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSGeneralGetComboItem";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcParamDesc", OleDbType.VarChar);

            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20140403, samy, TRODD14052, end
        //20170529, agireza, TRBST15176, begin
        public static bool subTRSPopulateSellerTemplate(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSellerTemplate";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subUploadDataSeller(ObligasiQuery obligasiQuery, string xmlData, int nUserNik, out DataSet dsOut)
        {
            bool blnResult = false;

            try
            {
                OleDbParameter[] oParam = new OleDbParameter[2];
                oParam[0] = new OleDbParameter("@pcXmlData", xmlData);
                oParam[1] = new OleDbParameter("@pnUserNik", nUserNik);

                blnResult = obligasiQuery.ExecProc("TRSUploadDataSeller", ref oParam, out dsOut);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return blnResult;
        }

        public static bool subTRSValidateCutOffTimeTrxEarlyRedem(ObligasiQuery cQuery, string SecAccno, out bool bCanProcess, out string messageError)
        {
            bCanProcess = false;
            messageError = "";
            bool bOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pvSecurityNo", SecAccno);
            dbPar[1] = new OleDbParameter("@pbValid", OleDbType.Boolean);
            dbPar[1].Direction = ParameterDirection.Output;
            dbPar[2] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 500);
            dbPar[2].Direction = ParameterDirection.Output;

            bOK = cQuery.ExecProc("dbo.trs_ValidateCutOffTimeTrxEarlyRedem", ref dbPar);

            if (bOK)
            {
                bCanProcess = bool.Parse(dbPar[1].Value.ToString());
                messageError = dbPar[2].Value.ToString();
            }
            return bOK;
        }

        public static bool subTRSPopulateEarlyRedemAuthorization(ObligasiQuery obligasiQuery, string cUserBranch, string strJob, out DataSet dsResult)
        {
            return subTRSPopulateEarlyRedemAuthorization(obligasiQuery, cUserBranch, strJob, null, out dsResult);
        }

        public static bool subTRSPopulateEarlyRedemAuthorization(ObligasiQuery obligasiQuery, string cUserBranch, string strJob, int? nUserNIK, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateEarlyRedemAuthorization";
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[3];

            odpParam[0] = new System.Data.OleDb.OleDbParameter();
            odpParam[0].OleDbType = System.Data.OleDb.OleDbType.VarChar;
            odpParam[0].Value = cUserBranch;
            odpParam[1] = new System.Data.OleDb.OleDbParameter();
            odpParam[1].OleDbType = System.Data.OleDb.OleDbType.VarChar;
            odpParam[1].Value = strJob;
            odpParam[2] = new System.Data.OleDb.OleDbParameter();
            odpParam[2].OleDbType = System.Data.OleDb.OleDbType.Integer;
            odpParam[2].Value = nUserNIK;

            return (obligasiQuery.ExecProc(strCommand, ref odpParam, out dsResult));
        }

        public static bool subTRSProcessEarlyRedemAuthorization(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessEarlyRedemAuthorization";
            int paramCount = 4;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[3] = new OleDbParameter("@pcStrJob", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateSettlementEarlyRedem(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementEarlyRedem";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@pnEarlyStatus", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateSettlementEarlyRedemByStatus(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementEarlyRedemByStatus";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcStatus", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessSettlementEarlyRedem(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementEarlyRedem";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool ValidateNasabahDormantSettBankBeli(ObligasiQuery cQuery, string RekeningRelasi, out string WarningMessage)
        {
            bool bOK = false;
            WarningMessage = "";

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pvRekeningRelasi", RekeningRelasi);
            dbPar[1] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 500);
            dbPar[1].Direction = ParameterDirection.Output;


            bOK = cQuery.ExecProc("dbo.trs_ValidateNasabahDormantSettBankBeli", ref dbPar);

            if (bOK)
                WarningMessage = dbPar[1].Value.ToString();



            return bOK;
        }

        public static bool subTRSPopulateSettlementTransaksiBankBeli(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementTransaksiBankBeli";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@piStatus", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pnType", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessSettlementTransaksiBankBeli(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementTransaksiBankBeli";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSettlementTransaksiBankBeliFee(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementTransaksiBankBeliFee";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSProcessSettlementTransaksiBankBeliPajak(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementTransaksiBankBeliPajak";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateTransactionLink(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateTransactionLink";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20170529, agireza, TRBST15176, end
        //20240102, samypasha, BONDRETAIL-1494, begin
        public static bool subTRSPopulateParameterTrxBankBeli(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateParameterSettlementTrxBankBeli";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcJurnal", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        //20240102, samypasha, BONDRETAIL-1494, end
        //20170417, fauzil, BOSIT17162, begin
        public static bool subTRSPopulatePTOSUpload(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSUpload";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pnUploadActive", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessPTOSUpload(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSProcessPTOSUpload";
            int paramCount = 5;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@@pcXMLDataErr", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pnUploadActive", OleDbType.Integer);
            dbParam[4] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulatePTOSUploadAuthen(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSUploadAuthen";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pnUploadActive", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessPTOSUploadAuthen(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSUploadAuthen";
            int paramCount = 5;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pnUploadActive", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[4] = new OleDbParameter("@pcOperationCode", OleDbType.Char);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulatePTOSUTrx(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSUTrx";
            int paramCount = 4;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pnDealId", OleDbType.VarChar, 20);
            dbParam[2] = new OleDbParameter("@pdTrxFrom", OleDbType.Date);
            dbParam[3] = new OleDbParameter("@pdTrxTo", OleDbType.Date);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessPTOSTrx(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSTrx";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcOperationType", OleDbType.Char);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static System.Data.DataSet columnsPTOSTransaksi_TR()
        {

            ObligasiQuery cQuery = new ObligasiQuery();
            System.Data.DataSet dsResult = new System.Data.DataSet();
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[1];
            try
            {

                odpParam[0] = new System.Data.OleDb.OleDbParameter();
                odpParam[0].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[0].Value = "PTOSTransaksi_Temp";

                cQuery.ExecProc("dbo.trs_ListColumnsTable", ref odpParam, out dsResult);
                return dsResult;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static bool subTRSPopulatePTOSTtrxAuthen(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSTrxAuthen";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnType", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessPTOSTrxAuthen(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSTrxAuthen";
            int paramCount = 4;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pcOperationCode", OleDbType.Char);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }



        public static bool subTRSPTOSListProduk(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPTOSlistProduk";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pvSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessPTOSSecurityMaster(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTSOSecurityMaster";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLDataSecurity", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulatePTOSSecurityMasterAuthen(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSSecurityMasterAuthen";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSProcessPTOSSecurityMasterAuthen(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSSecurityMasterAuthen";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulatePTOSUCustomer(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSCustomer_TM";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pdTrxFrom", OleDbType.Date);
            dbParam[1] = new OleDbParameter("@pdTrxTo", OleDbType.Date);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static System.Data.DataSet columnsPTOSCustomer_TM()
        {

            ObligasiQuery cQuery = new ObligasiQuery();
            System.Data.DataSet dsResult = new System.Data.DataSet();
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[1];
            try
            {

                odpParam[0] = new System.Data.OleDb.OleDbParameter();
                odpParam[0].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[0].Value = "PTOSCustomer_TM";

                cQuery.ExecProc("dbo.trs_ListColumnsTable", ref odpParam, out dsResult);
                return dsResult;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static bool subTRSProcessPTOSCustomer(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSCustomer_TM";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcOperationType", OleDbType.Char);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulatePTOSCustomerAuthen(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSCustomerAuthen";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSProcessPTOSCustomerAuthen(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSCustomerAuthen";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcOperationCode", OleDbType.Char);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulatePTOSOuts(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSOuts";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pdTrxFrom", OleDbType.Date);
            dbParam[2] = new OleDbParameter("@pdTrxTo", OleDbType.Date);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessPTOSOuts(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSOuts";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcOperationType", OleDbType.Char);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static System.Data.DataSet columnsPTOSOutstanding_TR()
        {

            ObligasiQuery cQuery = new ObligasiQuery();
            System.Data.DataSet dsResult = new System.Data.DataSet();
            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[1];
            try
            {

                odpParam[0] = new System.Data.OleDb.OleDbParameter();
                odpParam[0].OleDbType = System.Data.OleDb.OleDbType.VarChar;
                odpParam[0].Value = "PTOSOutstanding_TR";

                cQuery.ExecProc("dbo.trs_ListColumnsTable", ref odpParam, out dsResult);
                return dsResult;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static bool subTRSPopulatePTOSOutAuthen(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulatePTOSOutsAuthen";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnType", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessPTOSOutsAuthen(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessPTOSOutsAuthen";
            int paramCount = 4;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnType", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pcOperationCode", OleDbType.Char);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        //20170417, fauzil, BOSIT17162, begin
        // 20160201, fauzil, TRBST16240, begin
        public static bool CheckIsThereAnyOrderCorrection(ObligasiQuery cQuery, long OrderIdTemp, out bool bCanProcess)
        {
            bCanProcess = false;
            bool bOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pbOrderId", OrderIdTemp);
            dbPar[1] = new OleDbParameter("@pbValid", OleDbType.Boolean);
            dbPar[1].Direction = ParameterDirection.Output;

            bOK = cQuery.ExecProc("dbo.trs_CheckIsThereAnyOrderCorrection", ref dbPar);

            if (bOK)
                bCanProcess = bool.Parse(dbPar[1].Value.ToString());

            return bOK;
        }

        public static bool CheckDiffNIKOrderHapusOriInpAppv(ObligasiQuery cQuery, long OrderIdTemp, int NIK, out bool bCanProcess)
        {
            bCanProcess = false;
            bool bOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pbOrderId", OrderIdTemp);
            dbPar[1] = new OleDbParameter("@piNik", NIK);
            dbPar[2] = new OleDbParameter("@pbValid", OleDbType.Boolean);
            dbPar[2].Direction = ParameterDirection.Output;

            bOK = cQuery.ExecProc("dbo.trs_CheckDiffNIKOrderHapusOriInpAppv", ref dbPar);

            if (bOK)
                bCanProcess = bool.Parse(dbPar[2].Value.ToString());

            return bOK;
        }

        public static bool subTRSPopulateAmendAccrued(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateAmendAccrued";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSPopulateAmendAccruedAuthorization(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateAmendAccruedAuthorization";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSPopulateTransactionBalanceForUpdate(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateTransactionBalanceForUpdate";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            //20160519, fauzil, TRBST16240, begin
            //dbParam[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            dbParam[0] = new OleDbParameter("@pnDealNo", OleDbType.VarChar, 10);
            //20160519, fauzil, TRBST16240, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool RecalculateAccrued(ObligasiQuery cQuery, long DealId, decimal Interest, out decimal TaxOnAccrued, out decimal TotalProceed)
        {
            bool bOK = false;
            TaxOnAccrued = 0;
            TotalProceed = 0;

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@nDealId", DealId);
            dbPar[1] = new OleDbParameter("@nInterest", Interest);
            dbPar[2] = new OleDbParameter("@nTaxOnAccrued", OleDbType.Decimal);
            dbPar[2].Direction = ParameterDirection.Output;
            dbPar[3] = new OleDbParameter("@nTotalProceed", OleDbType.Decimal);
            dbPar[3].Direction = ParameterDirection.Output;
            // 20190521, Steven.ramli, LOGAM10146, begin
            dbPar[3].Precision = 19;
            dbPar[3].Scale = 5;
            // 20190521, Steven.ramli, LOGAM10146, end

            bOK = cQuery.ExecProc("dbo.TRSRetailReCalculateAccrued", ref dbPar);

            if (bOK)
            {
                TaxOnAccrued = decimal.Parse(dbPar[2].Value.ToString());
                TotalProceed = decimal.Parse(dbPar[3].Value.ToString());
            }


            return bOK;
        }

        public static bool subTRSProcessAmmendAccrued(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessAmendAccrued";
            //20160712, fauzil, TRBST16240, begin
            //int paramCount = 3;
            int paramCount = 5;
            //20160712, fauzil, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            //20160712, fauzil, TRBST16240, begin
            dbParam[3] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbParam[4] = new OleDbParameter("@pnTotalBlock", OleDbType.Decimal);
            //20160712, fauzil, TRBST16240, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool GetDataTransaction(ObligasiQuery cQuery, long DealId, out decimal TotalProceed)
        {
            bool bOK = false;
            TotalProceed = 0;

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pnDealId", DealId);
            dbPar[1] = new OleDbParameter("@pnTotalProceed", OleDbType.Decimal);
            dbPar[1].Direction = ParameterDirection.Output;


            bOK = cQuery.ExecProc("dbo.trs_getDataTransaction", ref dbPar);

            if (bOK)
                TotalProceed = (decimal)dbPar[1].Value;

            return bOK;
        }


        public static bool subTRSProcessUpdateTrxBankJual(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessUpdateTrxBankJual";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            dbParam[1] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcAccountBlockACTYPE", OleDbType.Char, 1);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }


        public static bool subTRSPopulateHapusTransaksiSuratBerharga(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateHapusTransaksiSuratBerharga";
            // 20160620, fauzil, TRBST16240, begin
            int paramCount = 6;
            // 20160620, fauzil, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnTrxType", OleDbType.SmallInt);
            dbParam[1] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            dbParam[2] = new OleDbParameter("@pnSecId", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pnSecAccNo", OleDbType.VarChar, 20);
            dbParam[4] = new OleDbParameter("@pnSecAccNo", OleDbType.VarChar, 50);
            dbParam[5] = new OleDbParameter("@pnSecAccNo", OleDbType.VarChar, 50);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessHapusTransaksiSuratBerharga(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessHapusTransaksiSuratBerharga";
            // 20160620, fauzil, TRBST16240, begin
            //int paramCount = 2;
            int paramCount = 4;
            // 20160620, fauzil, TRBST16240, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            // 20160620, fauzil, TRBST16240, begin
            dbParam[2] = new OleDbParameter("@pnReasonCode", OleDbType.Integer);
            dbParam[3] = new OleDbParameter("@pcReasonDesc", OleDbType.VarChar);
            // 20160620, fauzil, TRBST16240, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateHapusTransaksiSuratBerhargaAuthorization(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateHapusTransaksiSuratBerhargaAuthorizatio";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSRejectHapusTransaksiSuratBerharga(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSRejectHapusTransaksiSuratBerharga";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData1", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pcXMLData2", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }


        public static bool subTRSApproveHapusTransaksiSuratBerharga(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSApproveHapusTransaksiSuratBerharga";
            //20201102, yudha.n, BONDRETAIL-1211, begin
            //int paramCount = 5;
            int paramCount = 6;
            //20201102, yudha.n, BONDRETAIL-1211, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pcGuidInternal", OleDbType.Guid);
            dbParam[2] = new OleDbParameter("@pcGuidExternal", OleDbType.Guid);
            dbParam[3] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[4] = new OleDbParameter("@pbIsMurex", OleDbType.Boolean);
            //20201102, yudha.n, BONDRETAIL-1211, begin
            dbParam[5] = new OleDbParameter("@pbIsAbsorbedByBank", OleDbType.Boolean);
            //20201102, yudha.n, BONDRETAIL-1211, end

            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            //20201102, yudha.n, BONDRETAIL-1211, begin
            dbParam[5].Value = objParams[6];
            //20201102, yudha.n, BONDRETAIL-1211, end
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateKuponUpload(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateKuponUpload";
            //20230221, tobias, BONDRETAIL-1245, begin
            //int paramCount = 3;
            int paramCount = 5;
            //20230221, tobias, BONDRETAIL-1245, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pbIsPermata", OleDbType.Boolean);
            //20230221, tobias, BONDRETAIL-1245, begin
            dbParam[3] = new OleDbParameter("@pcGuid", OleDbType.VarChar);
            dbParam[4] = new OleDbParameter("@pcNIK", OleDbType.Integer);
            //20230221, tobias, BONDRETAIL-1245, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessKuponUpload(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessKuponUpload";
            //20230221, tobias, BONDRETAIL-1245, begin
            //int paramCount = 4;
            int paramCount = 5;
            //20230221, tobias, BONDRETAIL-1245, end
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[1] = new OleDbParameter("@pcXMLFromDatabase", OleDbType.VarChar);
            dbParam[2] = new OleDbParameter("@pcXMLFromCustody", OleDbType.VarChar);
            dbParam[3] = new OleDbParameter("@pbIsPermata", OleDbType.Boolean);
            //20230221, tobias, BONDRETAIL-1245, begin
            dbParam[4] = new OleDbParameter("@pcGuid", OleDbType.VarChar);
            //20230221, tobias, BONDRETAIL-1245, end
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateKuponUploadAuthorization(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateKuponUploadAuthorization";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessKuponUploadAuthorization(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessKuponUploadAuthorization";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSValidateMatchingKuponBeforeProcess(ObligasiQuery cQuery, int nik, string xml, bool isPermata, out bool bCanProcess)
        {
            bCanProcess = false;
            bool bOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pnUserNik", nik);
            dbPar[1] = new OleDbParameter("@pcXMLFromDatabase", xml);
            dbPar[2] = new OleDbParameter("@pbIsPermata", isPermata);
            dbPar[3] = new OleDbParameter("@pbValid", OleDbType.Boolean);
            dbPar[3].Direction = ParameterDirection.Output;

            bOK = cQuery.ExecProc("dbo.TRSValidateProcessKuponUpload", ref dbPar);

            if (bOK)
                bCanProcess = bool.Parse(dbPar[3].Value.ToString());

            return bOK;
        }


        public static bool subTRSGenerateDataMatchingKuponForFileExcel(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSGenerateDataMatchingKuponForFileExcel";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcXMLFromDatabase", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool getParamaterTR(ObligasiQuery obligasiQuery, string code, out string value)
        {
            bool bOK = false;
            value = "";

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcCode", code);
            dbPar[1] = new OleDbParameter("@pcValue", OleDbType.VarChar, 20);
            dbPar[1].Direction = ParameterDirection.Output;

            bOK = obligasiQuery.ExecProc("dbo.trs_getParameterTR", ref dbPar);

            if (bOK)
                value = dbPar[1].Value.ToString();

            return bOK;
        }

        public static bool TRSUpdateBlockSequence(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSUpdateBlockSequence";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSValidateCutOffTimeTransaction(ObligasiQuery cQuery, string type, string SecAccno, out bool bCanProcess, out string messageError)
        {
            bCanProcess = false;
            messageError = "";
            bool bOK = false;

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcType", type);
            dbPar[1] = new OleDbParameter("@pvSecurityNo", SecAccno);
            dbPar[2] = new OleDbParameter("@pbValid", OleDbType.Boolean);
            dbPar[2].Direction = ParameterDirection.Output;
            dbPar[3] = new OleDbParameter("@pcErrMsg", OleDbType.VarChar, 500);
            dbPar[3].Direction = ParameterDirection.Output;

            bOK = cQuery.ExecProc("dbo.trs_ValidateCutOffTimeTransaction", ref dbPar);

            if (bOK)
            {
                bCanProcess = bool.Parse(dbPar[2].Value.ToString());
                messageError = dbPar[3].Value.ToString();
            }


            return bOK;
        }
        public static bool subTRSPopulateSetlementTransaksiBankBeli(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSProrateTrxTaxAmesty";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSPopulateSettlementKupon(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSettlementKupon";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar, 20);
            dbParam[1] = new OleDbParameter("@piStatus", OleDbType.Integer);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }

        public static bool subTRSProcessSettlementKupon(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessSettlementKupon";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }

        public static bool subTRSPopulateApprovalReleaseBookbld(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateApproveReleaseBookbld";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSApproveReleaseBookbld(ObligasiQuery obligasiQuery, string strData, string strTrxType, int nUserNik)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcXmlData", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcXmlData", strData);

            dbPar[1] = new OleDbParameter("@pcStatus", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcStatus", strTrxType);

            dbPar[2] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbPar[2] = new OleDbParameter("@pnUserNik", nUserNik);

            string strCommand = "TRSApproveBookbld";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }

        public static bool subTRSPopulateBlokirSwitchingTransaction(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateBlokirSwitchingTransaction";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSUpdateBlokirSwitchingTransaction(ObligasiQuery obligasiQuery, long DealId, int BlockSequence, string ACType)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            dbPar[0] = new OleDbParameter("@pnDealId", DealId);

            dbPar[1] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbPar[1] = new OleDbParameter("@pnAccountBlockSequence", BlockSequence);

            dbPar[2] = new OleDbParameter("@pcAccountACType", OleDbType.VarChar);
            dbPar[2] = new OleDbParameter("@pcAccountACType", ACType);

            string strCommand = "TRSUpdateBlokirSwitchingTransaction";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }

        //20220920, yudha.n, BONDRETAIL-1052, begin
        public static bool subTRSUpdateBlokirMateraiFee(ObligasiQuery obligasiQuery, long DealId, int BlockSequence, string ACType, string productType)
        {
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pnDealId", OleDbType.BigInt);
            dbPar[0] = new OleDbParameter("@pnDealId", DealId);

            dbPar[1] = new OleDbParameter("@pnAccountBlockSequence", OleDbType.Integer);
            dbPar[1] = new OleDbParameter("@pnAccountBlockSequence", BlockSequence);

            dbPar[2] = new OleDbParameter("@pcAccountACType", OleDbType.VarChar);
            dbPar[2] = new OleDbParameter("@pcAccountACType", ACType);

            dbPar[3] = new OleDbParameter("@pcProduct", OleDbType.VarChar);
            dbPar[3] = new OleDbParameter("@pcProduct", productType);

            string strCommand = "TRSUpdateBlokirMateraiFee";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }

        public static bool DeleteTransactionMateraiFee(ObligasiQuery cQuery, string DealId)
        {
            bool isOK = false;

            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[1];
            odpParam[0] = new System.Data.OleDb.OleDbParameter("@pnDealId", int.Parse(DealId));

            isOK = (cQuery.ExecProc("dbo.TRSDeleteProcessInputMateraiFee", ref odpParam));

            return isOK;
        }
        //20220920, yudha.n, BONDRETAIL-1052, end

        public static bool subTRSPopulateAlamatKorespondensi(ObligasiQuery obligasiQuery, object[] objParams, out DataSet dsResult)
        {
            string strCommand = "trs_PopulateAlamatNasabah";
            int paramCount = 1;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcCIFNo", OleDbType.VarChar, 20);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam, out dsResult));
        }
        // 20160224, fauzil, TRBST16240, end
        //20170929, agireza, COPOD17271, begin
        public static bool subTRSPopulateMinimumRiskProfile(ObligasiQuery obligasiQuery, string strSecurityNo, string strRiskProfileCode, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcSecurityNo", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcSecurityNo", strSecurityNo);

            dbPar[1] = new OleDbParameter("@pcRiskProfileCode", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcRiskProfileCode", strRiskProfileCode);

            string strCommand = "TRSPopulateMinimumRiskProfile";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }
        //20170929, agireza, COPOD17271, end

        //20171220, agireza, TRBST16240, begin
        public static bool subTRSPopulateTransaksiSuratBerhargaTemplate(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateTransaksiSuratBerhargaTemplate";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSRetailCalculateFee2(ObligasiQuery obligasiQuery, string xmlData, out DataSet dsData)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcXmlData", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlData);

            string strCommand = "TRSRetailCalculateFee2";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsData));
        }

        public static bool subTRSUploadTransaksiSuratBerharga(ObligasiQuery obligasiQuery, string xmlData, string xmlSumberDana, int nUserNik, out DataSet dsLog)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcXmlData", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlData);

            dbPar[1] = new OleDbParameter("@pcXMLSumberDana", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcXMLSumberDana", xmlSumberDana);

            dbPar[2] = new OleDbParameter("@pnUserNIK", OleDbType.Numeric);
            dbPar[2] = new OleDbParameter("@pnUserNIK", nUserNik);

            string strCommand = "TRSProcessUploadTransaksiSuratBerharga";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsLog));
        }
        //20171220, agireza, TRBST16240, end
        //20180205, uzia, TRBST16240, begin
        public static bool GetSettlementBankBeliBlokirData(ObligasiQuery obligasiQuery, long dealIdBankJual, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnDealIdBankJual", dealIdBankJual);

            return (obligasiQuery.ExecProc("dbo.TRSGetSettBankBeliBlokirData", ref dbPar, out dsOut));
        }
        //20180205, uzia, TRBST16240, end

        // 20180226, AlexF TRODD16222, begin
        public static bool subTRSPopulateParameterMurexBIS4(ObligasiQuery obligasiQuery, string strMurexCode, string strBIS4Code, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcMurexCode", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcMurexCode", strMurexCode);

            dbPar[1] = new OleDbParameter("@pcBIS4Code", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcBIS4Code", strBIS4Code);

            string strCommand = "TRSPopulateMappingMurexBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSSaveParameterMurexBIS4(ObligasiQuery obligasiQuery, string mapingType, string strMurexCode, string strBIS4Code, int nMappingId, string strStatus, int nUserNik)
        {
            OleDbParameter[] dbPar = new OleDbParameter[6];
            dbPar[0] = new OleDbParameter("@pcMappingType", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcMappingType", mapingType);

            dbPar[1] = new OleDbParameter("@pcMurexCode", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcMurexCode", strMurexCode);

            dbPar[2] = new OleDbParameter("@pcBIS4Code", OleDbType.VarChar);
            dbPar[2] = new OleDbParameter("@pcBIS4Code", strBIS4Code);

            dbPar[3] = new OleDbParameter("@pnMappingId", OleDbType.Integer);
            dbPar[3] = new OleDbParameter("@pnMappingId", nMappingId);

            dbPar[4] = new OleDbParameter("@pcStatus", OleDbType.VarChar);
            dbPar[4] = new OleDbParameter("@pcStatus", strStatus);

            dbPar[5] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbPar[5] = new OleDbParameter("@pnUserNik", nUserNik);

            string strCommand = "TRSSaveMappingMurexBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }

        public static bool subTRSPopulateApprovalParameterMurexBIS4(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateApprovalMappingMurexBIS4";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSApproveParameterMurexBIS4(ObligasiQuery obligasiQuery, string strSelectedData, string strStatus, int nUserNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcSelectedData", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcSelectedData", strSelectedData);

            dbPar[1] = new OleDbParameter("@pcStatus", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcStatus", strStatus);

            dbPar[2] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbPar[2] = new OleDbParameter("@pnUserNik", nUserNIK);

            string strCommand = "TRSApproveMappingMurexBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }

        public static bool subTRSPopulateUploadCTPMutex(ObligasiQuery obligasiQuery, string strCTPNo, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];

            dbPar[0] = new OleDbParameter("@pcCTPNo", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcCTPNo", strCTPNo);

            string strCommand = "TRSPopulateUploadCTPMurex";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSPopulateTemplateUploadCTPMurex(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {

            string strCommand = "TRSPopulateTemplateUploadCTPMurex";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSUploadDataCTPMurex(ObligasiQuery obligasiQuery, string xmlData, int nUserNIK, string strStatus, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pxmlData", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pxmlData", xmlData);

            dbPar[1] = new OleDbParameter("@pnUserNIK", OleDbType.Integer);
            dbPar[1] = new OleDbParameter("@pnUserNIK", nUserNIK);

            dbPar[2] = new OleDbParameter("@pcStatus", OleDbType.VarChar);
            dbPar[2] = new OleDbParameter("@pcStatus", strStatus);

            string strCommand = "TRSSaveUploadCTPMurex";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSPopulateParameter(ObligasiQuery obligasiQuery, string ParamCode, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcParamCode", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcParamCode", ParamCode);

            string strCommand = "TRSPopulateParameter";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSPopulateSettlementBIS4(ObligasiQuery obligasiQuery, string strValueDate, string strTrxStatus, string strTrxType, string strTrxSource, string strCTPNo, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[5];
            dbPar[0] = new OleDbParameter("@pnSettlementDate", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pnSettlementDate", strValueDate);

            dbPar[1] = new OleDbParameter("@pcTrxStatus", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcTrxStatus", strTrxStatus);

            dbPar[2] = new OleDbParameter("@pnTrxType", OleDbType.VarChar);
            dbPar[2] = new OleDbParameter("@pnTrxType", strTrxType);

            dbPar[3] = new OleDbParameter("@pnTrxSource", OleDbType.VarChar);
            dbPar[3] = new OleDbParameter("@pnTrxSource", strTrxSource);

            dbPar[4] = new OleDbParameter("@pcCTPNo", OleDbType.VarChar);
            dbPar[4] = new OleDbParameter("@pcCTPNo", strCTPNo);

            string strCommand = "TRSPopulateSettlementBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSPopulateSettlementBIS4Detail(ObligasiQuery obligasiQuery, int nMurexNo, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnMurexNo", OleDbType.Integer);
            dbPar[0] = new OleDbParameter("@pnMurexNo", nMurexNo);

            string strCommand = "TRSPopulateSettlementBIS4Detail";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSPopulateParticipantParamBIS4(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateParticipantParamBIS4";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSPopulateCashAccountsParam(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateCashAccountsParam";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSPopulateSubInstrumentParam(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateSubInstrumentParam";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSProcessSettlementBIS4(ObligasiQuery obligasiQuery, string strXmlData, int nUserNik, out DataSet dsData)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pxData", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pxData", strXmlData);

            dbPar[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbPar[1] = new OleDbParameter("@pnUserNik", nUserNik);

            string strCommand = "TRSProcessSettlementBIS4";

            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsData));
        }

        public static bool subTRSPopulateApprovalSettlementBIS4(ObligasiQuery obligasiQuery, out DataSet dsData)
        {
            string strCommand = "TRSPopulateApprovalSettlementBIS4";
            return (obligasiQuery.ExecProc(strCommand, out dsData));
        }

        public static bool subTRSProcessApprovalSettlementBIS4(ObligasiQuery obligasiQuery, string ListMurexNo, bool IsApprove, int nUserNik)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcListMurexNo", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcListMurexNo", ListMurexNo);

            dbPar[1] = new OleDbParameter("@pbIsApprove", OleDbType.Boolean);
            dbPar[1] = new OleDbParameter("@pbIsApprove", IsApprove);

            dbPar[2] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbPar[2] = new OleDbParameter("@pnUserNik", nUserNik);

            string strCommand = "TRSProcessApprovalSettlementBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }

        public static bool subTRSCreateMTContentForBIS4(ObligasiQuery obligasiQuery, out DataSet dsData)
        {
            string strCommand = "TRSCreateMTContentForBIS4";
            return (obligasiQuery.ExecProc(strCommand, out dsData));
        }

        public static bool subTRSReportSettlementBIS4(ObligasiQuery obligasiQuery, string strValueDate, string strTrxStatus, string strTrxType, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnSettlementDate", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pnSettlementDate", strValueDate);

            dbPar[1] = new OleDbParameter("@pcTrxStatus", OleDbType.VarChar);
            dbPar[1] = new OleDbParameter("@pcTrxStatus", strTrxStatus);

            dbPar[2] = new OleDbParameter("@pnTrxType", OleDbType.VarChar);
            dbPar[2] = new OleDbParameter("@pnTrxType", strTrxType);

            string strCommand = "TRSReportSettlementBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSPopulateAccSubAccBIS4(ObligasiQuery obligasiQuery, string ParticId, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnParticId", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pnParticId", ParticId);

            string strCommand = "TRSPopulateAccSubAccBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }

        public static bool subTRSPopulatePaymentAgentBIS4(ObligasiQuery obligasiQuery, string CashAccount, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnCashAccount ", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pnCashAccount ", CashAccount);

            string strCommand = "TRSPopulatePaymentAgentBIS4";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }
        //20180226, AlexF TRODD16222, end
        //20180524, samypasha, LOGEN00633, begin
        public static bool subTRSPopulateEditHargaModalAuthorization(ObligasiQuery obligasiQuery, out DataSet dsResult)
        {
            string strCommand = "TRSPopulateEditHargaModalAuthorization";
            return (obligasiQuery.ExecProc(strCommand, out dsResult));
        }

        public static bool subTRSProcessApprovalEditHargaModal(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSProcessApprovalEditHargaModal";
            int paramCount = 3;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.Char, 1);
            dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
            dbParam[2] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];
            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }
        //20180524, samypasha, LOGEN00633, end
        //20190305, rezakahfi, BOSIT18140, begin
        #region SERI ESBN
        public static bool subTRSSubmitSERI(ObligasiQuery obligasiQuery, object[] objParams)
        {
            string strCommand = "TRSSubmitListSeriESBN";
            int paramCount = 2;
            OleDbParameter[] dbParam = new OleDbParameter[paramCount];
            dbParam[0] = new OleDbParameter("@pcOperationCode", OleDbType.VarChar);
            dbParam[1] = new OleDbParameter("@pnNik", OleDbType.Integer);

            for (int i = 0; i < paramCount; i++)
                dbParam[i].Value = objParams[i];

            return (obligasiQuery.ExecProc(strCommand, ref dbParam));
        }
        #endregion
        //20190305, rezakahfi, BOSIT18140, end
        //20190430, uzia, BOSIT18140, begin
        #region DocumentLink
        public static bool DocLinkPopulateData(ObligasiQuery cQuery, out DataSet dsOut)
        {
            dsOut = new DataSet();
            return cQuery.ExecProc("dbo.TRSPopulateDocsLink", out dsOut);
        }

        public static bool DocLinkSave(ObligasiQuery cQuery, string xmlData, int nikUser)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlData);
            dbPar[1] = new OleDbParameter("@pnNIK", nikUser);

            return cQuery.ExecProc("dbo.TRSProcessUploadDocsLink", ref dbPar);
        }

        public static bool DocLinkAuthPopulateMaster(ObligasiQuery cQuery, int nikUser, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnNIK", nikUser);

            return cQuery.ExecProc("dbo.TRSPopulateMainAppUploadDocsLink", ref dbPar, out dsOut);
        }

        public static bool DocLinkAuthPopulateDetail(ObligasiQuery cQuery, long appId, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnAppId", appId);

            return cQuery.ExecProc("dbo.TRSPopulateDetailAppUploadDocsLink", ref dbPar, out dsOut);
        }

        public static bool DocLinkAuthProcess(ObligasiQuery cQuery, long appId, int appStatus, int nikUser)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pnAppId", appId);
            dbPar[1] = new OleDbParameter("@pnAuthNIK", nikUser);
            dbPar[2] = new OleDbParameter("@pnAppStatus", appStatus);

            return cQuery.ExecProc("dbo.TRSProcessApprovalDocsLink", ref dbPar);
        }

        #endregion
        //20190430, uzia, BOSIT18140, end
        //20200219, rezakahfi, BONDRETAIL-179, begin
        public static bool GetNextCouponDate(ObligasiQuery cQuery, string NextCouponDate, int nPeriode, bool IsEOM, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pdNextCouponDate", NextCouponDate);
            dbPar[1] = new OleDbParameter("@pnPeriode", nPeriode);
            dbPar[2] = new OleDbParameter("@pbIsEOM", IsEOM);

            return cQuery.ExecProc("dbo.SecurityGetNextCouponDate", ref dbPar, out dsOut);
        }
        //20200219, rezakahfi, BONDRETAIL-179, end
        //20200716, rezakahfi, BONDRETAIL-506, begin
        public static bool TRSValidateUploadTrx(ObligasiQuery obligasiQuery, string xmlData, out DataSet dsData)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pcXmlData", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlData);

            string strCommand = "TRSValidateUploadTransaction";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsData));
        }
        //20200716, rezakahfi, BONDRETAIL-506, end
        //20200728, uzia, BONDRETAIL-525, begin
        #region Untagging Rekening SBN
        public static bool TRSUntagRekSBNPopulate(ObligasiQuery obligasiQuery, string strSecAccNo, string strCIFNo, string strNama, string strAccountNo, out DataSet dsOut)
        {
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcSecAccNo", strSecAccNo);
            dbPar[1] = new OleDbParameter("@pcCIFNo", strCIFNo);
            dbPar[2] = new OleDbParameter("@pcNama", strNama);
            dbPar[3] = new OleDbParameter("@pcNoRek", strAccountNo);

            string strCommand = "TRSUntagRekSBNPopulate";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsOut));
        }

        public static bool TRSUntagRekSBNSave(ObligasiQuery obligasiQuery, string strXml, int nNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXml", strXml);
            dbPar[1] = new OleDbParameter("@pnNik", nNIK);

            string strCommand = "TRSUntagRekSBNSave";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }

        public static bool TRSUntagRekSBNPopulateApproval(ObligasiQuery obligasiQuery, int nNIK, out DataSet dsOut)
        {
            dsOut = new DataSet();
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnNik", nNIK);

            string strCommand = "TRSUntagRekSBNPopulateApproval";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsOut));
        }

        public static bool TRSUntagRekSBNProcessAuth(ObligasiQuery obligasiQuery, string strXml, string strAuthStatus, int nNIK)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcXml", strXml);
            dbPar[1] = new OleDbParameter("@pcAuthStatus", strAuthStatus);
            dbPar[2] = new OleDbParameter("@pnNik", nNIK);

            string strCommand = "TRSUntagRekSBNProcessAuth";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar));
        }
        #endregion
        //20200728, uzia, BONDRETAIL-525, end
        //20200921, rezakahfi, BONDRETAIL-550, begin
        public static bool subTRSPopulateBlokirSwitchingTransaction(ObligasiQuery obligasiQuery, string strDealId, out DataSet dsResult)
        {
            OleDbParameter[] dbPar = new OleDbParameter[1];
            dbPar[0] = new OleDbParameter("@pnDealId", OleDbType.VarChar);
            dbPar[0] = new OleDbParameter("@pnDealId", strDealId);

            string strCommand = "TRSPopulateBlokirSwitchingTransaction";
            return (obligasiQuery.ExecProc(strCommand, ref dbPar, out dsResult));
        }
        //20200921, rezakahfi, BONDRETAIL-550, end
        //20210113, rezakahfi, BONDRETAIL-544, begin
        public static bool DeleteTransactionSekunder(ObligasiQuery cQuery
            , string DealId
            )
        {
            bool isOK = false;

            System.Data.OleDb.OleDbParameter[] odpParam = new System.Data.OleDb.OleDbParameter[1];
            odpParam[0] = new System.Data.OleDb.OleDbParameter("@pnDealId", int.Parse(DealId));

            isOK = (cQuery.ExecProc("dbo.OMDeleteProcessInputSecurityMaster", ref odpParam));

            return isOK;
        }
        //20210113, rezakahfi, BONDRETAIL-544, end
        //20221031, darul.wahid, BONDRETAIL-1105, begin
        public static bool PopulateGagalProsesPerdana(ObligasiQuery cQuery
            , object[] objParams
            , out string strErrMsg
            , out DataSet dsOut
            )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSPopulateGagalBayarOri";
                int paramCount = 3;
                OleDbParameter[] dbParam = new OleDbParameter[paramCount];
                dbParam[0] = new OleDbParameter("@pbIsAllDate", OleDbType.Boolean);
                dbParam[1] = new OleDbParameter("@pdProcessDateFrom", OleDbType.VarChar);
                dbParam[2] = new OleDbParameter("@pdProcessDateTo", OleDbType.VarChar);

                for (int i = 0; i < paramCount; i++)
                    dbParam[i].Value = objParams[i];

                return (cQuery.ExecProc(strCommand, ref dbParam, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }

        public static bool RePushProsesGagalPerdana(ObligasiQuery cQuery
            , object[] objParams
            , out string strErrMsg
            , out DataSet dsOut
            )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSRePushGagalBayarOri";
                int paramCount = 3;
                OleDbParameter[] dbParam = new OleDbParameter[paramCount];
                dbParam[0] = new OleDbParameter("@pcGuidProcess", OleDbType.VarChar);
                dbParam[1] = new OleDbParameter("@pnUserNik", OleDbType.Integer);
                dbParam[2] = new OleDbParameter("@pcUserBranch", OleDbType.VarChar);

                for (int i = 0; i < paramCount; i++)
                    dbParam[i].Value = objParams[i];

                return (cQuery.ExecProc(strCommand, ref dbParam, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }
        //20221031, darul.wahid, BONDRETAIL-1105, end
        //20230117, darul.wahid, FLD-58, begin
        public static bool PopulateParameterUploadFLD(ObligasiQuery cQuery
            , out string strErrMsg
            , out DataSet dsOut
            )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSPopulateParameterUploadFLD";
                return (cQuery.ExecProc(strCommand, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }

        public static bool PopulateDataParameterUploadFLD(ObligasiQuery cQuery
            , object[] objParams
            , out string strErrMsg
            , out DataSet dsOut
            )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSPopulateDataParameterFLD";
                int paramCount = 2;
                OleDbParameter[] dbParam = new OleDbParameter[paramCount];
                dbParam[0] = new OleDbParameter("@pcTypeParam", OleDbType.VarChar);
                dbParam[1] = new OleDbParameter("@pcCurrencyCode", OleDbType.VarChar);

                for (int i = 0; i < paramCount; i++)
                    dbParam[i].Value = objParams[i];

                return (cQuery.ExecProc(strCommand, ref dbParam, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }

        public static bool SaveParameterFLD(ObligasiQuery cQuery
             , object[] objParams
             , out string strErrMsg
             , out DataSet dsOut
             )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSBranchingSaveParamFLD";
                int paramCount = 2;
                OleDbParameter[] dbParam = new OleDbParameter[paramCount];
                dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
                dbParam[1] = new OleDbParameter("@pnInputter", OleDbType.Integer);


                for (int i = 0; i < paramCount; i++)
                    dbParam[i].Value = objParams[i];

                return (cQuery.ExecProc(strCommand, ref dbParam, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }
        public static bool SaveParameterFLDEach(ObligasiQuery cQuery
             , object[] objParams
             , out string strErrMsg
             , out DataSet dsOut
             )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSProcessSaveParameterFLD";
                int paramCount = 3;
                OleDbParameter[] dbParam = new OleDbParameter[paramCount];
                dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
                dbParam[1] = new OleDbParameter("@pcTypeParam", OleDbType.VarChar);
                dbParam[2] = new OleDbParameter("@pnInputter", OleDbType.Integer);


                for (int i = 0; i < paramCount; i++)
                    dbParam[i].Value = objParams[i];

                return (cQuery.ExecProc(strCommand, ref dbParam, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }
        public static bool ValidateParameterFLD(ObligasiQuery cQuery
             , object[] objParams
             , out string strErrMsg
             , out DataSet dsOut
             )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSValidateUploadParameterFLD";
                int paramCount = 2;
                OleDbParameter[] dbParam = new OleDbParameter[paramCount];
                dbParam[0] = new OleDbParameter("@pcXMLData", OleDbType.VarChar);
                dbParam[1] = new OleDbParameter("@pcTypeParam", OleDbType.VarChar);


                for (int i = 0; i < paramCount; i++)
                    dbParam[i].Value = objParams[i];

                return (cQuery.ExecProc(strCommand, ref dbParam, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }
        //20230117, darul.wahid, FLD-58, end
        //20230412, darul.wahid, BONDRETAIL-1265, begin
        public static bool ValidateDocLinkUrl(ObligasiQuery cQuery, string strUrl, out bool isValid, out string strValidationMsg)
        {
            bool isOk = false;
            isValid = false;
            strValidationMsg = "";
            try
            {
                OleDbParameter[] dbPar = new OleDbParameter[3];
                dbPar[0] = new OleDbParameter("@pcUrlInput", strUrl);
                dbPar[1] = new OleDbParameter("@pbIsValid", OleDbType.Boolean);
                dbPar[1].Direction = ParameterDirection.Output;
                dbPar[2] = new OleDbParameter("@pcValidationMsg", OleDbType.VarChar, 250);
                dbPar[2].Direction = ParameterDirection.Output;
                
                isOk = cQuery.ExecProc("dbo.TRSValidateDocsLinkUrl", ref dbPar);
                if (isOk)
                {
                    isValid = bool.Parse(dbPar[1].Value.ToString());
                    strValidationMsg = dbPar[2].Value.ToString();
                }
            }
            catch
            {
                isOk = false;
            }

            return isOk;

        }

        public static bool DocLinkSave(ObligasiQuery cQuery, string xmlData, int nikUser, string strProcessType)
        {
            OleDbParameter[] dbPar = new OleDbParameter[3];
            dbPar[0] = new OleDbParameter("@pcXmlData", xmlData);
            dbPar[1] = new OleDbParameter("@pnNIK", nikUser);
            dbPar[2] = new OleDbParameter("@pcProcessType", strProcessType);

            return cQuery.ExecProc("dbo.TRSProcessUploadDocsLink", ref dbPar);
        }
        //20230412, darul.wahid, BONDRETAIL-1265, end

        //20240514, alfian.andhika, BONDRETAIL-1586, begin
        public static bool GetSecurityTypeParam(ObligasiQuery cQuery
            , object[] objParams
            , out string strErrMsg
            , out DataSet dsOut
        )
        {
            dsOut = new DataSet();
            strErrMsg = "";
            try
            {
                string strCommand = "TRSGetSecurityTypeParam";
                int paramCount = 1;
                OleDbParameter[] dbParam = new OleDbParameter[paramCount];
                dbParam[0] = new OleDbParameter("@pcSecurityType", OleDbType.VarChar);

                for (int i = 0; i < paramCount; i++)
                    dbParam[i].Value = objParams[i];

                return (cQuery.ExecProc(strCommand, ref dbParam, out dsOut));
            }
            catch (Exception e)
            {
                strErrMsg = e.Message;
                return false;
            }
        }
        //20240514, alfian.andhika, BONDRETAIL-1586, end
        //20240625, darul.wahid, PTBC-1703, begin
        public static bool PopulateParameterMergeFile(ObligasiQuery cQuery, string paramType, string paramFilter, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcParamType", paramType);
            dbPar[1] = new OleDbParameter("@pcParamFilter", paramFilter);

            return cQuery.ExecProc("dbo.TRSPopulateParameterMergeFile", ref dbPar, out dsOut);
        }
        public static bool MergeFileUpload(ObligasiQuery cQuery, string fileType, string xmlPermata, string xmlMaybank, string paramValue1, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[4];
            dbPar[0] = new OleDbParameter("@pcFileType", fileType);
            dbPar[1] = new OleDbParameter("@pcXMLData1", xmlPermata);
            dbPar[2] = new OleDbParameter("@pcXMLData2", xmlMaybank);
            dbPar[3] = new OleDbParameter("@pcParamValue1", paramValue1);

            return cQuery.ExecProc("dbo.TRSMergeFileUpload", ref dbPar, out dsOut);
        }
        //20240625, darul.wahid, PTBC-1703, end
        //20240717, darul.wahid, PTBC-1816, end
        public static bool UpdateFileMaturity(ObligasiQuery cQuery, string xmlKupon, string xmlMaturity, out DataSet dsOut)
        {
            dsOut = new DataSet();

            OleDbParameter[] dbPar = new OleDbParameter[2];
            dbPar[0] = new OleDbParameter("@pcXMLData1", xmlKupon);
            dbPar[1] = new OleDbParameter("@pcXMLData2", xmlMaturity);

            return cQuery.ExecProc("dbo.TRSUpdateFileUploadMaturity", ref dbPar, out dsOut);
        }
        //20240717, darul.wahid, PTBC-1816, end
    }
}
