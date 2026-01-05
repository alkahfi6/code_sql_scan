using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NISPDataSourceNetCore.converter;
using NISPDataSourceNetCore.database;
using NISPDataSourceNetCore.webservice;
using NISPDataSourceNetCore.webservice.model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Wealth.ReksaParameter.API.Business_Logic;
using Wealth.ReksaParameter.API.Model;
using static NISPDataSourceNetCore.database.EPV;
using static NISPDataSourceNetCore.database.SQLSPParameter;
//20221122, Lita, RDN-880, add service generate RDB File, begin
using Wealth.ReksaParameter.API.Support;
//20241001, Andhika J, RDN-1192, Hit CMSHUB, begin
using Microsoft.AspNetCore.Mvc;
//20241001, Andhika J, RDN-1192, Hit CMSHUB, end
//20221122, Lita, RDN-880, add service generate RDB File, end

namespace Wealth.ReksaParameter.API.Service
{
    public class clsAPIService : IService
    {
        private IConfiguration _configuration;
        private string _strConnSOA;
        //20220815, Rendy, M32022-7, begin
        //private string _strUrlWsPwd;
        //20220815, Rendy, M32022-7, end
        private bool _ignoreSSL;
        private EPVEnvironmentType _envType;
        private string _strConnReksa;
        private string _strUrlWsReka;
        private string _strUrlWsReka2;
        //20201027, Rendy, RDN-2, begin
        private string _apiGuid;
        private string _localDataDurationDays;
        //20201027, Rendy, RDN-2, end
        //20210315, korvi, RDN-438, begin
        private string _apiCIFInquiryDetailV2URL;

        private ByonLogic _byonLogic;
        //20210315, korvi, RDN-438, end

        //20221122, Lita, RDN-880, add service generate RDB File, begin
        private FileLogic _fileLogic;
        //20221122, Lita, RDN-880, add service generate RDB File, end

        //20221122, Lita, RDN-880, add service generate RDB File, begin
        //public clsAPIService(IConfiguration iconfiguration, GlobalVariabelList globalVariabelList)
        public clsAPIService(IConfiguration iconfiguration, GlobalVariabelList globalVariabelList, GeneratePDF generatePDF)
        //20221122, Lita, RDN-880, add service generate RDB File, end
        {
            this._configuration = iconfiguration;
            this._strConnReksa = globalVariabelList.ConnectionStringDBReksa;
            this._strConnSOA = globalVariabelList.ConnectionStringDBSOA;
            this._envType = globalVariabelList.EnvironmentType;
            this._ignoreSSL = globalVariabelList.IgnoreSSL;
            //20220815, Rendy, M32022-7, begin
            //this._strUrlWsPwd = globalVariabelList.URLWsPwd;
            //20220815, Rendy, M32022-7, end
            this._strUrlWsReka = globalVariabelList.URLWsReksa;
            this._strUrlWsReka2 = globalVariabelList.URLWsReksa2;
            this._localDataDurationDays = globalVariabelList.LocalDataDurationDays;
            this._apiGuid = globalVariabelList.ApiGuid;

            //20210315, korvi, RDN-438, begin
            this._apiCIFInquiryDetailV2URL = globalVariabelList.APICIFInquiryDetailV2URL;
            this._byonLogic = new ByonLogic(iconfiguration, globalVariabelList);
            //20210315, korvi, RDN-438, end

            //20221122, Lita, RDN-880, add service generate RDB File, begin
            this._fileLogic = new FileLogic(iconfiguration, globalVariabelList, generatePDF);
            //20221122, Lita, RDN-880, add service generate RDB File, end
        }

        #region Query

        #region Inquiry without WS
        #region InquiryProduct
        public ApiMessage<List<ProductRes>> InquiryProduct(ApiMessage<ProductReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<ProductRes> listProductRes = new List<ProductRes>();
            ApiMessage<List<ProductRes>> response = new ApiMessage<List<ProductRes>>();
            DataMapper<ProductRes> dataMapper;

            response.copyHeaderForReply(paramIn);
            response.MessageDateTime = DateTime.Now;
            response.MessageGUID = paramIn.MessageGUID;
            response.UserNIK = paramIn.UserNIK;
            response.ModuleName = "RM-Mobile Inquiry Product";

            DataSet dsOut = null;

            string strQuery = "";

            try
            {
                //select data di table database where prodid ada
                strQuery = "SELECT * ";
                strQuery = strQuery + "FROM dbo.ReksaProduct_TM with(nolock) ";
                //sementara tanpa product 101 di uat data product ada yang membuat error
                strQuery = strQuery + "WHERE ProdId != 101 ";
                if (paramIn.Data.ProdId.ToString() != "")
                    strQuery = strQuery + "AND ProdId = " + paramIn.Data.ProdId.ToString();

                databaseConnector.execQuery(TransactionType.AUTOMATIC_COMMIT_TRANSACTION, strQuery, out dsOut);

                if (dsOut.Tables[0].Rows.Count < 1)
                {
                    response.IsSuccess = false;
                    response.ErrorCode = "1001";
                    response.ErrorDescription = "Data is null nor empty";
                    return response;
                }

                dataMapper = new DataMapper<ProductRes>();
                listProductRes = dataMapper.mapDataTableToList(dsOut.Tables[0], typeof(ProductRes));
                dataMapper = null;

                response.Data = listProductRes;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.ErrorDescription = ex.Message;
                response.IsSuccess = false;
            }
            finally
            {
                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }
            }
            return response;
        }
        #endregion InquiryProduct

        #region InquirySeller
        public ApiMessage<List<SellerRes>> InquirySeller(ApiMessage<SellerReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SellerRes> listDS = new List<SellerRes>();
            ApiMessage<List<SellerRes>> response = new ApiMessage<List<SellerRes>>();
            DataMapper<SellerRes> dataMapper;

            response.copyHeaderForReply(paramIn);
            response.MessageDateTime = DateTime.Now;
            response.MessageGUID = paramIn.MessageGUID;
            response.UserNIK = paramIn.UserNIK;
            response.ModuleName = "RM-Mobile Inquiry Seller";

            DataSet dsOut = null;

            string queryBuilder = "";

            try
            {
                //select data di table database where prodid ada
                queryBuilder = "select top 200 wa.NIK as 'NIKSeller', wa.WaperdNo, wa.Nama,wa.JobTitle, wa.DateExpire " +
                "from SQL_Employee.dbo.employee_id e with (nolock) " +
                "join dbo.ReksaWaperd_TR wa on e.employee_id = wa.NIK ";
                if (paramIn.Data.NikSeller != "")
                {
                    queryBuilder = queryBuilder + "where wa.NIK = '" + paramIn.Data.NikSeller + "' ";
                }
                else
                {
                    queryBuilder = queryBuilder + "order by wa.DateExpire desc";
                }

                databaseConnector.execQuery(TransactionType.AUTOMATIC_COMMIT_TRANSACTION, queryBuilder, out dsOut);
                dataMapper = new DataMapper<SellerRes>();
                listDS = dataMapper.mapDataTableToList(dsOut.Tables[0], typeof(SellerRes));
                dataMapper = null;

                response.Data = listDS;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.ErrorDescription = ex.Message;
                response.IsSuccess = false;
            }
            finally
            {
                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }
            }
            return response;

        }
        #endregion InquirySeller

        #region InquiryNAV
        public ApiMessage<List<NAVRes>> InquiryNAV(ApiMessage<NAVReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<NAVRes> listDS = new List<NAVRes>();
            ApiMessage<List<NAVRes>> response = new ApiMessage<List<NAVRes>>();
            DataMapper<NAVRes> dataMapper;

            response.copyHeaderForReply(paramIn);
            response.MessageDateTime = DateTime.Now;
            response.MessageGUID = paramIn.MessageGUID;
            response.UserNIK = paramIn.UserNIK;
            response.ModuleName = "RM-Mobile Inquiry NAV";

            DataSet dsOut = null;

            string queryBuilder = "";

            try
            {
                //select data di table database where prodid ada
                queryBuilder = "SELECT TOP 1 * FROM ReksaNAVParam_TH WHERE ProdId = " + paramIn.Data.ProdId + " ORDER BY ValueDate DESC";

                databaseConnector.execQuery(TransactionType.AUTOMATIC_COMMIT_TRANSACTION, queryBuilder, out dsOut);
                dataMapper = new DataMapper<NAVRes>();
                listDS = dataMapper.mapDataTableToList(dsOut.Tables[0], typeof(NAVRes));
                dataMapper = null;

                response.Data = listDS;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.ErrorDescription = ex.Message;
                response.IsSuccess = false;
            }
            finally
            {
                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }
            }
            return response;

        }
        #endregion InquiryNAV

        #region InquiryRate
        public ApiMessage<RateRes> InquiryRate(ApiMessage<RateReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            List<SQLSPParameter> lstSQLSPParameter;
            ApiMessage<RateRes> apiMsgResponse = new ApiMessage<RateRes>();
            RateRes apiMsgResponse2 = new RateRes();

            apiMsgResponse.copyHeaderForReply(paramIn);
            apiMsgResponse.MessageDateTime = DateTime.Now;
            apiMsgResponse.MessageGUID = paramIn.MessageGUID;
            apiMsgResponse.UserNIK = paramIn.UserNIK.ToString();
            apiMsgResponse.ModuleName = "Wealth.ReksaParameter";

            string strSPName = "ReksaCalcFee";
            DataSet dsResult = null;

            if (paramIn.Data.ValueDate == DateTime.MinValue || paramIn.Data.ValueDate.ToString() == "")
            {
                paramIn.Data.ValueDate = DateTime.Today;
            }

            try
            {
                lstSQLSPParameter = new List<SQLSPParameter>();
                lstSQLSPParameter.Add(new SQLSPParameter("@pnProdId", paramIn.Data.ProdId));
                lstSQLSPParameter.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientId));
                lstSQLSPParameter.Add(new SQLSPParameter("@pnTranType", paramIn.Data.TranType));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.TranAmount));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmUnit", paramIn.Data.TranUnit));
                lstSQLSPParameter.Add(new SQLSPParameter("@pcFeeCCY", paramIn.Data.FeeCCY = "", ParamDirection.OUTPUT)); //5
                lstSQLSPParameter.Add(new SQLSPParameter("@pnFee", paramIn.Data.Fee = 0, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                lstSQLSPParameter.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID, 50));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmNAV", paramIn.Data.NAV = 0));
                lstSQLSPParameter.Add(new SQLSPParameter("@pbFullAmount", paramIn.Data.FullAmount));
                lstSQLSPParameter.Add(new SQLSPParameter("@pbIsByPercent", paramIn.Data.IsByPercent));
                lstSQLSPParameter.Add(new SQLSPParameter("@pbIsFeeEdit", paramIn.Data.IsFeeEdit));
                lstSQLSPParameter.Add(new SQLSPParameter("@pdPercentageFeeInput", paramIn.Data.PercentageFeeInput));
                lstSQLSPParameter.Add(new SQLSPParameter("@pdPercentageFeeOutput", paramIn.Data.PercentageFeeOutput, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pbProcess", false));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmFeeBased", paramIn.Data.FeeBased, ParamDirection.OUTPUT));//16
                lstSQLSPParameter.Add(new SQLSPParameter("@pmRedempUnit", paramIn.Data.RedempUnit, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmRedempDev", paramIn.Data.RedempDev, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pbByUnit", paramIn.Data.ByUnit = false));
                lstSQLSPParameter.Add(new SQLSPParameter("@pbDebug", paramIn.Data.Debug = false));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmProcessTranId", paramIn.Data.ProcessTranId = 0));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmErrMsg", paramIn.Data.ErrMsg, 100, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pnOutType", paramIn.Data.OutType = 0));
                lstSQLSPParameter.Add(new SQLSPParameter("@pdValueDate", paramIn.Data.ValueDate)); //24
                lstSQLSPParameter.Add(new SQLSPParameter("@pmTaxFeeBased", paramIn.Data.TaxFeeBased = 0, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmFeeBased3", paramIn.Data.FeeBased3 = 0, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmFeeBased4", paramIn.Data.FeeBased4 = 0, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pmFeeBased5", paramIn.Data.FeeBased5 = 0, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pnPeriod", paramIn.Data.Period = 0, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pnIsRDB", paramIn.Data.IsRDB = 0, ParamDirection.OUTPUT));
                lstSQLSPParameter.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo, 20));//31
                //20250619, Andhika J, RDN-1247 , begin
                lstSQLSPParameter.Add(new SQLSPParameter("@bIsNeedApproval", paramIn.Data.bIsNeedApproval = false, ParamDirection.OUTPUT));//32
                lstSQLSPParameter.Add(new SQLSPParameter("@pcMustApproveBy", paramIn.Data.pcMustApproveBy = "", ParamDirection.OUTPUT));//33
                //20250619, Andhika J, RDN-1247 , end

                if (databaseConnector.execSP(TransactionType.HANDLED_IN_STORED_PROCEDURE, strSPName, lstSQLSPParameter, out dsResult))
                {
                    if (lstSQLSPParameter[22].ParameterValue.ToString() == "")
                    {
                        apiMsgResponse2.FeeCCY = lstSQLSPParameter[5].ParameterValue.ToString(); ;
                        apiMsgResponse2.Fee = Convert.ToDecimal(lstSQLSPParameter[6].ParameterValue.ToString());
                        apiMsgResponse2.PercentageFeeOutput = Convert.ToDecimal(lstSQLSPParameter[14].ParameterValue.ToString());
                        apiMsgResponse2.FeeBased = Convert.ToDecimal(lstSQLSPParameter[16].ParameterValue.ToString());
                        apiMsgResponse2.RedempUnit = Convert.ToDecimal(lstSQLSPParameter[17].ParameterValue.ToString());
                        apiMsgResponse2.RedempDev = Convert.ToDecimal(lstSQLSPParameter[18].ParameterValue.ToString());
                        //apiMsgResponse.Data.ErrMsg = lstSQLSPParameter[22].ParameterValue.ToString();
                        apiMsgResponse2.TaxFeeBased = Convert.ToDecimal(lstSQLSPParameter[25].ParameterValue.ToString());
                        apiMsgResponse2.FeeBased3 = Convert.ToDecimal(lstSQLSPParameter[26].ParameterValue.ToString());
                        apiMsgResponse2.FeeBased4 = Convert.ToDecimal(lstSQLSPParameter[27].ParameterValue.ToString());
                        apiMsgResponse2.FeeBased5 = Convert.ToDecimal(lstSQLSPParameter[28].ParameterValue.ToString());
                        apiMsgResponse2.Period = Convert.ToInt16(lstSQLSPParameter[29].ParameterValue.ToString());
                        apiMsgResponse2.IsRDB = Convert.ToInt16(lstSQLSPParameter[30].ParameterValue.ToString());
                        //20250619, Andhika J, RDN-1247 , begin
                        apiMsgResponse2.IsNeedApproval = Convert.ToBoolean(Convert.ToInt32(lstSQLSPParameter[32].ParameterValue));
                        apiMsgResponse2.MustApproveBy = lstSQLSPParameter[33].ParameterValue.ToString();
                        //20250619, Andhika J, RDN-1247 , end

                        apiMsgResponse.Data = apiMsgResponse2;
                        apiMsgResponse.IsSuccess = true;
                    }
                    else
                    {
                        apiMsgResponse.ErrorDescription = lstSQLSPParameter[22].ParameterValue.ToString();
                        apiMsgResponse.IsSuccess = false;
                    }
                }
            }
            catch (Exception ex)
            {

                apiMsgResponse.ErrorDescription = ex.Message;
                apiMsgResponse.IsSuccess = false;
            }
            finally
            {
                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }
            }


            return apiMsgResponse;
        }
        #endregion InquiryRate
        #endregion Inquiry without WS

        #region Inquiry with WS

        #region Inquiry Product 
        public ApiMessage<List<ProductRes>> InquiryProductWithWS(ApiMessage<ProductReq> paramIn)
        {
            ApiMessage<List<ProductRes>> ApiMsgResponse = new ApiMessage<List<ProductRes>>();
            List<ProductRes> listResponse = new List<ProductRes>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            ApiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                string sqlCommand = @"
                    declare
                    @dtValueDateNAV datetime
                    , @dtValueDateKP datetime

                     -- insert data into temp table
                    select * into #temp_Product
                    from ReksaProduct_TM

                    select top 1 @dtValueDateNAV = ValueDate
                    from ReksaNAVParam_TH with(Nolock)
                    order by ValueDate desc

                    select * into #temp_NAV
                    from ReksaNAVParam_TH
                    where ValueDate = @dtValueDateNAV

                    select top 1 @dtValueDateKP = ValueDate
                    from ReksaKinerjaProduct_TM with(Nolock)
                    order by ValueDate desc

                    select * into #temp_KP
                    from ReksaKinerjaProduct_TM
                    where ValueDate = @dtValueDateKP

                    -- update related fields
                    update tp
                    set tp.NAV = tn.NAV
                    from #temp_Product tp
                    join #temp_NAV tn
                    on tp.ProdId = tn.ProdId

                    -- Minimal Subs untuk RDB
                    create table #tmpReksaRegulerSubscriptionParam (  
                        ProdId int,
                        MinSubsRDBEmployee decimal(19, 0),
                        MinSubsRDBNonEmployee decimal(19, 0)
                    )  

                    insert into #tmpReksaRegulerSubscriptionParam (ProdId, MinSubsRDBEmployee)
                    select ProductId, ParamValue
                    from ReksaRegulerSubscriptionParam_TR
                    where ParamId = 'SubscMin'
                    and IsEmployee = 1

                    update #tmpReksaRegulerSubscriptionParam
                    set MinSubsRDBNonEmployee = ParamValue
                    from #tmpReksaRegulerSubscriptionParam a join ReksaRegulerSubscriptionParam_TR b
                    on a.ProdId = b.ProductId
                    where b.ParamId = 'SubscMin'
                    and IsEmployee = 0

                     -- display data
                    select
                    rp.*
                    , @dtValueDateNAV as NAVValueDate
                    , rprp.RiskProfile
                    , rdrp.RiskProfileDesc
                    , rdrp.RiskProfileDescEN
                    , rt.TypeId
                    , rt.TypeCode as 'ProductCategoryCode'
                    , rt.TypeName as 'ProductCategoryName' -- bahasa indonesia
                    , rt.TypeNameEnglish as 'ProductCategoryNameEN'-- english
                    , kp.Sehari
                    , kp.Seminggu
                    , kp.Sebulan
                    , kp.Setahun
                    , isnull(rrsp.MinSubsRDBEmployee, -1) as MinSubsRDBEmployee
                    , isnull(rrsp.MinSubsRDBNonEmployee, -1) as MinSubsRDBNonEmployee
                    from #temp_Product rp
                    left join #temp_KP kp
                    on rp.ProdId = kp.ProdId
                    left join ReksaProductRiskProfile_TM rprp
                    on rprp.ProductCode = rp.ProdCode
                    left join ReksaDescRiskProfile_TR rdrp
                    on rdrp.RiskProfile = rprp.RiskProfile
                    left join ReksaType_TR rt
                    on rp.TypeId = rt.TypeId
                    left join #tmpReksaRegulerSubscriptionParam rrsp
                    on rp.ProdId = rrsp.ProdId
                ";

                if (paramIn.Data.ProdId != "")
                    sqlCommand = sqlCommand + " where rp.ProdId = " + paramIn.Data.ProdId;

                sqlCommand = sqlCommand + " drop table #temp_Product drop table #temp_NAV drop table #temp_KP drop table #tmpReksaRegulerSubscriptionParam";
                
                #endregion
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    listResponse = JsonConvert.DeserializeObject<List<ProductRes>>(JsonConvert.SerializeObject(dsOut.Tables[0]));
                    
                    ApiMsgResponse.Data = listResponse;
                    ApiMsgResponse.IsSuccess = true;
                }

                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return ApiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorDescription = ex.Message;
            }
            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return ApiMsgResponse;
        }
        #endregion Inquiry Product

        #region Inquiry Seller
        public ApiMessage<List<SellerRes>> InquirySellerWithWS(ApiMessage<SellerReq> paramIn)
        {
            ApiMessage<List<SellerRes>> ApiMsgResponse = new ApiMessage<List<SellerRes>>();
            List<SellerRes> listResponse = new List<SellerRes>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            ApiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                //20230614, Lita, RDN-972 add validasi seller, begin 
                //string sqlCommand = @"
                //    SELECT wa.NIK as 'NIKSeller', wa.WaperdNo, wa.Nama,wa.JobTitle, wa.DateExpire FROM SQL_Employee.dbo.employee_id e with (nolock)
                //    join dbo.ReksaWaperd_TR wa on e.employee_id = wa.NIK 
                //";

                //if (paramIn.Data.NikSeller.ToString() != "")
                //    sqlCommand = sqlCommand + "where wa.NIK = " + paramIn.Data.NikSeller;
                //else
                //{
                //    sqlCommand = sqlCommand + "order by wa.DateExpire desc";
                //}

                string sqlCommand = @"declare @cErrMsg varchar(500)
                                , @pnNIK int
                set @pnNIK = " + paramIn.Data.NikSeller + @"
                set @cErrMsg = ''
                
                if isnull(@pnNIK,0) = 0
                    set @cErrMsg = 'NIK Seller tidak boleh kosong'
                
                if not exists (select top 1 1 from dbo.ReksaWaperd_TR where NIK = @pnNIK)
                    set @cErrMsg = 'NIK Seller tidak terdaftar di reksadana'

                if not exists(select top 1 1 from SQL_Employee.dbo.PSEmployee_v where EMPLID = @pnNIK)
                    set @cErrMsg = 'NIK Seller sudah resign / tidak terdaftar sebagai karyawan'
                
                if exists (select top 1 1 from dbo.ReksaWaperd_TR where NIK = @pnNIK and upper(PPJ) <> 'TENAGA PEMASAR' )
                    set @cErrMsg = 'NIK Seller tidak terdaftar sebagai tenaga pemasar'

                if exists (select top 1 1 from dbo.ReksaWaperd_TR where DateExpire < getdate() and isnull(datediff(d, getdate(), DateExpireSK2), -1) < 0 and NIK = @pnNIK)            
                begin            
                    set @cErrMsg = 'WAPERD untuk NIK '+ convert(varchar,@pnNIK) +' sudah expired'         
                end      
                
                --dataset[0]
                select @cErrMsg 'ErrMsg'

                --dataset[1]
                --select wa.NIK as 'NIKSeller', wa.WaperdNo, wa.Nama,wa.JobTitle, wa.DateExpire 
                select wa.NIK as 'NIKSeller', WaperdNo = case 
	            when wa.DateExpire < wa.DateExpireSK2 and isnull(wa.NomorSK2, '') <> '' then wa.NomorSK2 
	            when wa.DateExpire > getdate() then wa.WaperdNo
	            else ''
                end, wa.Nama,wa.JobTitle
                , DateExpire = case 
	                when wa.DateExpire < wa.DateExpireSK2 and isnull(wa.NomorSK2, '') <> '' then wa.DateExpireSK2 
	                when wa.DateExpire > getdate() then wa.DateExpire
	                else ''
                end
                from dbo.ReksaWaperd_TR wa join SQL_Employee.dbo.PSEmployee_v e with (nolock)
                on wa.NIK = e.EMPLID
                where wa.NIK = @pnNIK";
                //20230614, Lita, RDN-972 add validasi seller, end
                #endregion

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    //20230614, Lita, RDN-972 add validasi seller, begin
                    //if (dsOut == null || dsOut.Tables.Count == 0 || dsOut.Tables[0].Rows.Count == 0)
                    //{
                    //    ApiMsgResponse.IsSuccess = false;
                    //    ApiMsgResponse.ErrorCode = "3000";
                    //    ApiMsgResponse.ErrorDescription = "Data not found";
                    //    return ApiMsgResponse;
                    //}
                    //#region mapping sesuai tipe data agar tidak merubah 
                    //listResponse = JsonConvert.DeserializeObject<List<SellerRes>>(JsonConvert.SerializeObject(dsOut.Tables[0]));
                    //#endregion mapping sesuai tipe data agar tidak merubah

                    //ApiMsgResponse.Data = listResponse;
                    //ApiMsgResponse.IsSuccess = true;

                    if (dsOut == null || dsOut.Tables.Count == 0)
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }
                    else if ((dsOut.Tables[0].Rows.Count > 0) && (dsOut.Tables[0].Rows[0]["ErrMsg"].ToString() != ""))
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = dsOut.Tables[0].Rows[0]["ErrMsg"].ToString();
                        return ApiMsgResponse;
                    }
                    else if (dsOut.Tables[1].Rows.Count > 0)
                    {
                        #region mapping sesuai tipe data agar tidak merubah 
                        listResponse = JsonConvert.DeserializeObject<List<SellerRes>>(JsonConvert.SerializeObject(dsOut.Tables[1]));
                        #endregion mapping sesuai tipe data agar tidak merubah

                        ApiMsgResponse.Data = listResponse;
                        ApiMsgResponse.IsSuccess = true;
                    }
                    //20230614, Lita, RDN-972 add validasi seller, end
                }

                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return ApiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorDescription = ex.Message;
            }

            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return ApiMsgResponse;
        }
        #endregion Inquiry Seller

        #region InquiryNAV by Prod Id
        public ApiMessage<List<NAVRes>> InquiryNAVWithWS(ApiMessage<NAVReq> paramIn)
        {
            ApiMessage<List<NAVRes>> ApiMsgResponse = new ApiMessage<List<NAVRes>>();
            List<NAVRes> listResponse = new List<NAVRes>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            ApiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                string sqlCommand = @"
                    SELECT TOP 1 * FROM ReksaNAVParam_TH WHERE ProdId = " + paramIn.Data.ProdId + " ORDER BY ValueDate DESC";
                #endregion

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut == null || dsOut.Tables.Count == 0 || dsOut.Tables[0].Rows.Count == 0)
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    #region mapping sesuai tipe data agar tidak merubah 
                    listResponse = (from DataRow dr in dsOut.Tables[0].Rows
                                    select new NAVRes()
                                    {
                                        ProdId = dr["ProdId"] != DBNull.Value ? Convert.ToInt32(dr["ProdId"]) : 0,
                                        ValueDate = dr["ValueDate"] != DBNull.Value ? Convert.ToDateTime(dr["ValueDate"]) : DateTime.MinValue,
                                        NAV = dr["NAV"] != DBNull.Value ? Convert.ToDecimal(dr["NAV"]) : 0,
                                        Deviden = dr["Deviden"] != DBNull.Value ? Convert.ToDecimal(dr["Deviden"]) : 0,
                                        Kurs = dr["Kurs"] != DBNull.Value ? Convert.ToDecimal(dr["Kurs"]) : 0,
                                        LastUpdate = dr["LastUpdate"] != DBNull.Value ? Convert.ToDateTime(dr["LastUpdate"]) : DateTime.MinValue,
                                        LastUser = dr["LastUser"] != DBNull.Value ? Convert.ToInt32(dr["LastUser"]) : 0,
                                        NAVMFee = dr["NAVMFee"] != DBNull.Value ? Convert.ToDecimal(dr["NAVMFee"]) : 0
                                    }).ToList();
                    #endregion mapping sesuai tipe data agar tidak merubah

                    if (listResponse.Count == 0)
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    ApiMsgResponse.Data = listResponse;
                    ApiMsgResponse.IsSuccess = true;
                }
                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return ApiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorDescription = ex.Message;
            }

            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return ApiMsgResponse;
        }
        #endregion InquiryNAV by Prod Id

        #region InquiryNAV with Date
        public ApiMessage<List<NAVRes>> InquiryNAVByDateWithWS(ApiMessage<NAVReqWithDate> paramIn)
        {
            ApiMessage<List<NAVRes>> ApiMsgResponse = new ApiMessage<List<NAVRes>>();
            List<NAVRes> listResponse = new List<NAVRes>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            ApiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                string sqlCommand = @"
                    SELECT TOP 1 * FROM ReksaNAVParam_TH WHERE ProdId = " + paramIn.Data.ProdId + "AND ValueDate = '" + paramIn.Data.DateNAV + "' ORDER BY ValueDate DESC";
                #endregion

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut == null || dsOut.Tables.Count == 0 || dsOut.Tables[0].Rows.Count == 0)
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    #region mapping sesuai tipe data agar tidak merubah 
                    listResponse = (from DataRow dr in dsOut.Tables[0].Rows
                                    select new NAVRes()
                                    {
                                        ProdId = dr["ProdId"] != DBNull.Value ? Convert.ToInt32(dr["ProdId"]) : 0,
                                        ValueDate = dr["ValueDate"] != DBNull.Value ? Convert.ToDateTime(dr["ValueDate"]) : DateTime.MinValue,
                                        NAV = dr["NAV"] != DBNull.Value ? Convert.ToDecimal(dr["NAV"]) : 0,
                                        Deviden = dr["Deviden"] != DBNull.Value ? Convert.ToDecimal(dr["Deviden"]) : 0,
                                        Kurs = dr["Kurs"] != DBNull.Value ? Convert.ToDecimal(dr["Kurs"]) : 0,
                                        LastUpdate = dr["LastUpdate"] != DBNull.Value ? Convert.ToDateTime(dr["LastUpdate"]) : DateTime.MinValue,
                                        LastUser = dr["LastUser"] != DBNull.Value ? Convert.ToInt32(dr["LastUser"]) : 0,
                                        NAVMFee = dr["NAVMFee"] != DBNull.Value ? Convert.ToDecimal(dr["NAVMFee"]) : 0
                                    }).ToList();
                    #endregion mapping sesuai tipe data agar tidak merubah

                    if (listResponse.Count == 0)
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    ApiMsgResponse.Data = listResponse;
                    ApiMsgResponse.IsSuccess = true;
                }
                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return ApiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorDescription = ex.Message;
            }

            finally
            {
               ApiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return ApiMsgResponse;
        }
        #endregion InquiryNAV with Date

        #region Inquiry Rate with WS
        public ApiMessage<RateRes> InquiryRateWithWS(ApiMessage<RateReq> paramIn)
        {
            ApiMessage<RateRes> msgResponse = new ApiMessage<RateRes>();
            List<RateRes> clsResponse = new List<RateRes>();
            string errMsg = "";
            DataSet dsParamOut = new DataSet();
            DataSet dsDataOut = new DataSet();
            List<SQLSPParameter> sqlPar = new List<SQLSPParameter>();
            string spName = "";
            int nCondition = 0;
			//20230725, Andi, RDN-1017, Switching RDB, begin
            int nIsRDB = 0;
			//20230725, Andi, RDN-1017, Switching RDB, end
			
            try
            {
                if (paramIn.Data.IsSwitching == true)
                {
                    //20230725, Andi, RDN-1017, Switching RDB, begin
                    string strQueryRDB = "";
                    strQueryRDB = @"
                        declare 
                            @pnClientId int,
                            @pnIsRDB int
                
                        set @pnClientId = '"+ paramIn.Data.ClientId + @"' 

                        if exists(select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM where ClientId = @pnClientId)
                            set @pnIsRDB = 1
                        else
                            set @pnIsRDB = 0

                        select @pnIsRDB as IsRDB
                        
                    ";
                    if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, strQueryRDB, out dsDataOut, out errMsg))
                    {
                        if (dsDataOut.Tables[0].Rows.Count > 0)
                        {
                            nIsRDB = Convert.ToInt32(dsDataOut.Tables[0].Rows[0]["IsRDB"].ToString());
                            if (nIsRDB == 1)
                            {
                                spName = "dbo.ReksaCalcSwitchingRDBFee";
                                nCondition = 3;
                                //msgResponse.Data = new RateRes();
                                //msgResponse.Data.IsRDB = Convert.ToInt32(dsDataOut.Tables[0].Rows[0]["IsRDB"].ToString());
                            }
                            else if (nIsRDB == 0)
                            {
                                spName = "dbo.ReksaCalcSwitchingFee";
                                nCondition = 1;
                            }
                        }
                    }
                    //spName = "dbo.ReksaCalcSwitchingFee";
                    //nCondition = 1;
                    //20230725, Andi, RDN-1017, Switching RDB, end

                }
                else
                {
                    spName = "dbo.ReksaCalcFee";
                    nCondition = 2;
                }
                //untuk subs dan redempt
                if (nCondition == 2)
                {
                    if (paramIn.Data.TranType == 1 || paramIn.Data.TranType == 2 || paramIn.Data.TranType == 5)
                    {
                        sqlPar = new List<SQLSPParameter>();
                        sqlPar.Add(new SQLSPParameter("@pnProdId", paramIn.Data.ProdId));
                        sqlPar.Add(new SQLSPParameter("@pnClientId", paramIn.Data.ClientId));
                        sqlPar.Add(new SQLSPParameter("@pnTranType", paramIn.Data.TranType));
                        sqlPar.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.TranAmount));
                        sqlPar.Add(new SQLSPParameter("@pmUnit", paramIn.Data.TranUnit));
                        sqlPar.Add(new SQLSPParameter("@pcFeeCCY", paramIn.Data.FeeCCY = "", ParamDirection.OUTPUT)); //5
                        sqlPar.Add(new SQLSPParameter("@pnFee", paramIn.Data.Fee = 0, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                        sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID));
                        sqlPar.Add(new SQLSPParameter("@pmNAV", paramIn.Data.NAV = 0));
                        sqlPar.Add(new SQLSPParameter("@pbFullAmount", paramIn.Data.FullAmount));
                        sqlPar.Add(new SQLSPParameter("@pbIsByPercent", paramIn.Data.IsByPercent));
                        sqlPar.Add(new SQLSPParameter("@pbIsFeeEdit", paramIn.Data.IsFeeEdit));
                        sqlPar.Add(new SQLSPParameter("@pdPercentageFeeInput", paramIn.Data.PercentageFeeInput));
                        sqlPar.Add(new SQLSPParameter("@pdPercentageFeeOutput", paramIn.Data.PercentageFeeOutput, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pbProcess", false));
                        sqlPar.Add(new SQLSPParameter("@pmFeeBased", paramIn.Data.FeeBased, ParamDirection.OUTPUT));//16
                        sqlPar.Add(new SQLSPParameter("@pmRedempUnit", paramIn.Data.RedempUnit, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pmRedempDev", paramIn.Data.RedempDev, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pbByUnit", paramIn.Data.ByUnit));
                        sqlPar.Add(new SQLSPParameter("@pbDebug", paramIn.Data.Debug = false));
                        sqlPar.Add(new SQLSPParameter("@pmProcessTranId", 0));
                        sqlPar.Add(new SQLSPParameter("@pmErrMsg", "", 200, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pnOutType", paramIn.Data.OutType = 0));
                        sqlPar.Add(new SQLSPParameter("@pdValueDate", paramIn.Data.ValueDate.ToString("yyyy-MM-dd")));
                        sqlPar.Add(new SQLSPParameter("@pmTaxFeeBased", paramIn.Data.TaxFeeBased = 0, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pmFeeBased3", paramIn.Data.FeeBased3 = 0, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pmFeeBased4", paramIn.Data.FeeBased4 = 0, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pmFeeBased5", paramIn.Data.FeeBased5 = 0, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pnPeriod", paramIn.Data.Period = 0, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pnIsRDB", paramIn.Data.IsRDB = 0, ParamDirection.OUTPUT));
                        sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo = paramIn.Data.CIFNo, 20));//31
                        //20250619, Andhika J, RDN-1247 , begin
                        sqlPar.Add(new SQLSPParameter("@bIsNeedApproval", paramIn.Data.bIsNeedApproval = false, ParamDirection.OUTPUT));//32
                        sqlPar.Add(new SQLSPParameter("@pcMustApproveBy", paramIn.Data.pcMustApproveBy = "", ParamDirection.OUTPUT));//33
                        //20250619, Andhika J, RDN-1247 , end


                        if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReka2, this._ignoreSSL, spName, ref sqlPar, out dsDataOut, out errMsg))
                            throw new Exception(errMsg);

                        errMsg = sqlPar[22].ParameterValue.ToString();

                        if (!errMsg.EndsWith(""))
                            throw new Exception(errMsg);

                        msgResponse.Data = new RateRes();
                        msgResponse.Data.FeeCCY = sqlPar[5].ParameterValue.ToString();
                        msgResponse.Data.Fee = Convert.ToDecimal(sqlPar[6].ParameterValue.ToString());
                        msgResponse.Data.PercentageFeeOutput = Convert.ToDecimal(sqlPar[14].ParameterValue.ToString());
                        msgResponse.Data.FeeBased = Convert.ToDecimal(sqlPar[16].ParameterValue.ToString());
                        msgResponse.Data.RedempUnit = Convert.ToDecimal(sqlPar[17].ParameterValue.ToString());
                        msgResponse.Data.RedempDev = Convert.ToDecimal(sqlPar[18].ParameterValue.ToString());
                        msgResponse.Data.TaxFeeBased = Convert.ToDecimal(sqlPar[25].ParameterValue.ToString());
                        msgResponse.Data.FeeBased3 = Convert.ToDecimal(sqlPar[26].ParameterValue.ToString());
                        msgResponse.Data.FeeBased4 = Convert.ToDecimal(sqlPar[27].ParameterValue.ToString());
                        msgResponse.Data.FeeBased5 = Convert.ToDecimal(sqlPar[28].ParameterValue.ToString());
                        msgResponse.Data.Period = Convert.ToInt16(sqlPar[29].ParameterValue.ToString());
                        msgResponse.Data.IsRDB = Convert.ToInt16(sqlPar[30].ParameterValue.ToString());
                        //20250619, Andhika J, RDN-1247 , begin
                        msgResponse.Data.IsNeedApproval = Convert.ToBoolean(Convert.ToInt32(sqlPar[32].ParameterValue));
                        msgResponse.Data.MustApproveBy = sqlPar[33].ParameterValue.ToString();
                        //20250619, Andhika J, RDN-1247 , end
                        msgResponse.IsSuccess = true;
                    }
                    if (paramIn.Data.TranType == 3 || paramIn.Data.TranType == 4)
                    {
                        string strQuery = "";
                        DataSet dsResult = null;
                        List<RateRes> listResponse = new List<RateRes>();

                        strQuery = @"
                            declare @pcFeeCCY varchar(8000)
                            set @pcFeeCCY=NULL
                            declare @pnFee varchar(8000)
                            set @pnFee=NULL
                            declare @pdPercentageFeeOutput varchar(8000)
                            set @pdPercentageFeeOutput=NULL
                            declare @pmFeeBased varchar(8000)
                            set @pmFeeBased=NULL
                            declare @pmRedempUnit varchar(8000)
                            set @pmRedempUnit=NULL
                            declare @pmRedempDev varchar(8000)
                            set @pmRedempDev=NULL
                            declare @pmErrMsg varchar(8000)
                            set @pmErrMsg=NULL
                            declare @pmTaxFeeBased varchar(8000)
                            set @pmTaxFeeBased=NULL
                            declare @pmFeeBased3 varchar(8000)
                            set @pmFeeBased3=NULL
                            declare @pmFeeBased4 varchar(8000)
                            set @pmFeeBased4=NULL
                            declare @pmFeeBased5 varchar(8000)
                            set @pmFeeBased5=NULL
                            declare @pnPeriod varchar(8000)
                            set @pnPeriod=NULL
                            declare @pnIsRDB varchar(8000)
                            set @pnIsRDB=NULL
                            --20250619, Andhika J, RDN-1247 , begin
                            declare @bIsNeedApproval bit
                            set @bIsNeedApproval = 0
                            declare @pcMustApproveBy varchar(100)
                            set @pcMustApproveBy = ''
                            --20250619, Andhika J, RDN-1247 , end
                            exec dbo.ReksaCalcFee @pnProdId, @pnClientId, @pnTranType, @pmTranAmt, @pmUnit, @pcFeeCCY output, @pnFee output,
                            @pnNIK, @pcGuid, @pmNAV, @pbFullAmount, @pbIsByPercent, @pbIsFeeEdit, @pdPercentageFeeInput,
                            @pdPercentageFeeOutput output, @pbProcess, @pmFeeBased output, @pmRedempUnit output, @pmRedempDev output, 
							@pbByUnit, @pbDebug, @pmProcessTranId,    
                            @pmErrMsg output, @pnOutType, null, @pmTaxFeeBased output, @pmFeeBased3 output,
                            @pmFeeBased4 output, @pmFeeBased5 output, @pnPeriod output, @pnIsRDB output, @pcCIFNo
                            --20250619, Andhika J, RDN-1247 , begin
                            , @bIsNeedApproval output 
                            , @pcMustApproveBy output
                            --20250619, Andhika J, RDN-1247 , end
                            
							select @pcFeeCCY as FeeCCY, @pnFee as Fee, @pdPercentageFeeOutput as PercentageFeeOutput, @pmFeeBased as FeeBased, 
							@pmRedempUnit as RedempUnit, @pmRedempDev as RedempDev, @pmErrMsg as ErrMsg, @pmTaxFeeBased as TaxFeeBased,  
							@pmFeeBased3 as FeeBased3, @pmFeeBased4 as FeeBased4, @pmFeeBased5 as FeeBased5, 
                            @pnPeriod as Period, @pnIsRDB as IsRDB
                            --20250619, Andhika J, RDN-1247 , begin
                            , isnull(@bIsNeedApproval,0) bIsNeedApproval
                            , isnull(@pcMustApproveBy,'') pcMustApproveBy
                            --20250619, Andhika J, RDN-1247 , end
                        ";

                        SqlParameter[] sqlParam = new SqlParameter[18];
                        sqlParam[0] = new SqlParameter("@pnProdId", paramIn.Data.ProdId);
                        sqlParam[1] = new SqlParameter("@pnClientId", paramIn.Data.ClientId);
                        sqlParam[2] = new SqlParameter("@pnTranType", paramIn.Data.TranType);
                        sqlParam[3] = new SqlParameter("@pmTranAmt", paramIn.Data.TranAmount);
                        sqlParam[4] = new SqlParameter("@pmUnit", paramIn.Data.TranUnit);
                        //sqlParam[5] = new SqlParameter("@pcFeeCCY", paramIn.Data.FeeCCY = ""); //5
                        //sqlParam[6] = new SqlParameter("@pnFee", paramIn.Data.Fee = 0);
                        sqlParam[5] = new SqlParameter("@pnNIK", paramIn.UserNIK);
                        sqlParam[6] = new SqlParameter("@pcGuid", paramIn.MessageGUID);
                        sqlParam[7] = new SqlParameter("@pmNAV", paramIn.Data.NAV = 0);
                        sqlParam[8] = new SqlParameter("@pbFullAmount", paramIn.Data.FullAmount);
                        sqlParam[9] = new SqlParameter("@pbIsByPercent", paramIn.Data.IsByPercent);
                        sqlParam[10] = new SqlParameter("@pbIsFeeEdit", paramIn.Data.IsFeeEdit);
                        sqlParam[11] = new SqlParameter("@pdPercentageFeeInput", SqlDbType.Decimal);
                        sqlParam[11].SqlValue = paramIn.Data.PercentageFeeInput;
                        //sqlParam[14] = new SqlParameter("@pdPercentageFeeOutput", paramIn.Data.PercentageFeeOutput);
                        sqlParam[12] = new SqlParameter("@pbProcess", paramIn.Data.Process = false);
                        //sqlParam[16] = new SqlParameter("@pmFeeBased", paramIn.Data.FeeBased);//16
                        //sqlParam[17] = new SqlParameter("@pmRedempUnit", paramIn.Data.RedempUnit);
                        //sqlParam[18] = new SqlParameter("@pmRedempDev", paramIn.Data.RedempDev);
                        sqlParam[13] = new SqlParameter("@pbByUnit", paramIn.Data.ByUnit = false);
                        sqlParam[14] = new SqlParameter("@pbDebug", paramIn.Data.Debug = false);
                        sqlParam[15] = new SqlParameter("@pmProcessTranId", paramIn.Data.ProcessTranId = 0);
                        //sqlParam[22] = new SqlParameter("@pmErrMsg", "");
                        sqlParam[16] = new SqlParameter("@pnOutType", paramIn.Data.OutType = 0);
                        //sqlParam[24] = new SqlParameter("@pdValueDate", paramIn.Data.ValueDate.ToString("yyyy-MM-dd"));
                        //sqlParam[25] = new SqlParameter("@pmFeeBased3", paramIn.Data.FeeBased3 = 0);
                        //sqlParam[26] = new SqlParameter("@pmFeeBased4", paramIn.Data.FeeBased4 = 0);
                        //sqlParam[27] = new SqlParameter("@pmFeeBased5", paramIn.Data.FeeBased5 = 0);
                        //sqlParam[28] = new SqlParameter("@pnPeriod", paramIn.Data.Period = 0);
                        //sqlParam[29] = new SqlParameter("@pnIsRDB", paramIn.Data.IsRDB = 0);
                        sqlParam[17] = new SqlParameter("@pcCIFNo", paramIn.Data.CIFNo);//31


                        if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, strQuery, ref sqlParam, out dsResult, out errMsg))
                        {
                            if (errMsg != "")
                                throw new Exception(errMsg);

                            msgResponse.Data = new RateRes();
                            msgResponse.Data.FeeCCY = dsResult.Tables[0].Rows[0]["FeeCCY"].ToString();
                            msgResponse.Data.Fee = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["Fee"].ToString());
                            msgResponse.Data.PercentageFeeOutput = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["PercentageFeeOutput"].ToString());
                            msgResponse.Data.FeeBased = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased"].ToString());
                            msgResponse.Data.RedempUnit = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["RedempUnit"].ToString());
                            msgResponse.Data.RedempDev = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["RedempDev"].ToString());
                            msgResponse.Data.TaxFeeBased = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["TaxFeeBased"].ToString());
                            msgResponse.Data.FeeBased3 = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased3"].ToString());
                            msgResponse.Data.FeeBased4 = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased4"].ToString());
                            msgResponse.Data.FeeBased5 = Convert.ToDecimal(dsResult.Tables[0].Rows[0]["FeeBased5"].ToString());
                            msgResponse.Data.Period = Convert.ToInt16(dsResult.Tables[0].Rows[0]["Period"].ToString());
                            msgResponse.Data.IsRDB = Convert.ToInt16(dsResult.Tables[0].Rows[0]["IsRDB"].ToString());
                            //20250619, Andhika J, RDN-1247 , begin
                            msgResponse.Data.IsNeedApproval = Convert.ToBoolean(Convert.ToInt32(dsResult.Tables[0].Rows[0]["bIsNeedApproval"]));
                            msgResponse.Data.MustApproveBy = dsResult.Tables[0].Rows[0]["pcMustApproveBy"].ToString();
                            //20250619, Andhika J, RDN-1247 , end
                            msgResponse.IsSuccess = true;

                        }
                    }

                    if (msgResponse.Data.Fee == 0 && paramIn.Data.TranType == 5)
                    {
                        string strErrMsg = "";
                        DataSet dsOut = new DataSet();

                        string sqlCommand = @"
                                    select top 1 1
                                    from ReksaRegulerSubscription_TR
                                    where ProductId = " + paramIn.Data.ProdId + "";

                        if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                        {
                            if (strErrMsg != null)
                                throw new Exception(strErrMsg);
                        }
                        else
                        {
                            if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                                throw new Exception("Prod Id bukan Product RDB");
                        }
                    }
                }
                //untuk switching
                else if (nCondition == 1)
                {
                    sqlPar = new List<SQLSPParameter>();
                    sqlPar.Add(new SQLSPParameter("@pcProdSwitchOut", paramIn.Data.ProdSwitchOut));
                    sqlPar.Add(new SQLSPParameter("@pcProdSwitchIn", paramIn.Data.ProdSwitchIn));
                    sqlPar.Add(new SQLSPParameter("@pbJenis", paramIn.Data.Jenis));
                    sqlPar.Add(new SQLSPParameter("@pmTranAmt", paramIn.Data.TranAmount));
                    sqlPar.Add(new SQLSPParameter("@pmUnit", paramIn.Data.TranUnit));
                    sqlPar.Add(new SQLSPParameter("@pcFeeCCY", paramIn.Data.FeeCCY = "", ParamDirection.OUTPUT)); //5
                    sqlPar.Add(new SQLSPParameter("@pnFee", paramIn.Data.Fee = 0, ParamDirection.OUTPUT));//6
                    sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                    sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID));
                    sqlPar.Add(new SQLSPParameter("@pmNAV", paramIn.Data.NAV = 0));
                    sqlPar.Add(new SQLSPParameter("@pcIsEdit", paramIn.Data.IsFeeEdit));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageFeeInput", paramIn.Data.PercentageFeeInput));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageFeeOutput", paramIn.Data.PercentageFeeOutput, ParamDirection.OUTPUT));//12
                    sqlPar.Add(new SQLSPParameter("@bIsEmployee", paramIn.Data.IsEmployee));
                    sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
                    //20250619, Andhika J, RDN-1247 , begin
                    sqlPar.Add(new SQLSPParameter("@bIsNeedApproval", paramIn.Data.bIsNeedApproval = false, ParamDirection.OUTPUT));//15
                    sqlPar.Add(new SQLSPParameter("@pcMustApproveBy", paramIn.Data.pcMustApproveBy = "", ParamDirection.OUTPUT));//16
                    //20250619, Andhika J, RDN-1247 , end
                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReka2, this._ignoreSSL, spName, ref sqlPar, out dsDataOut, out errMsg))
                        throw new Exception(errMsg);

                    //errMsg = sqlPar[22].ParameterValue.ToString();

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new RateRes();
                    msgResponse.Data.FeeCCY = sqlPar[5].ParameterValue.ToString();
                    msgResponse.Data.Fee = Convert.ToDecimal(sqlPar[6].ParameterValue.ToString());
                    msgResponse.Data.PercentageFeeOutput = Convert.ToDecimal(sqlPar[12].ParameterValue.ToString());
                    //20250619, Andhika J, RDN-1247 , begin
                    msgResponse.Data.IsNeedApproval = Convert.ToBoolean(Convert.ToInt32(sqlPar[15].ParameterValue));
                    msgResponse.Data.MustApproveBy = sqlPar[16].ParameterValue.ToString();
                    //20250619, Andhika J, RDN-1247 , end
                    msgResponse.IsSuccess = true;
                }
                //20230725, Andi, RDN-1017, Switching RDB, begin
                //untuk switching rdb
                else if (nCondition == 3)
                {
                    //variable untuk menerima kode produk switching
                    string KodeProdSwitchOut = "";
                    string KodeProdSwitchIn = "";
                    //ambil data dari ReksaProduct_TM, kemudian dimasukkan ke variable baru diatas
                    string strQueryProd = @"
                        declare 
                            @pcProdSwitchOut varchar(20),
                            @pcProdSwitchIn varchar(20),
                            @nProdSwitchOut int,
                            @nProdSwitchIn int

                        set @pcProdSwitchOut = '" + paramIn.Data.ProdSwitchOut + @"'
                        set @pcProdSwitchIn = '"+ paramIn.Data.ProdSwitchIn + @"'

                        --ini untuk prodSwitchOut
                        set @nProdSwitchOut = (select ProdId from ReksaProduct_TM where ProdCode = @pcProdSwitchOut)

                        --ini untuk prodSwitchIn
                        set @nProdSwitchIn = (select ProdId from ReksaProduct_TM where ProdCode = @pcProdSwitchIn)

                        
                        select @nProdSwitchOut as ProdSwitchOut, @nProdSwitchIn as ProdSwitchIn
                        
                    ";
                    if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, strQueryProd, out dsDataOut, out errMsg))
                    {
                        if (dsDataOut.Tables[0].Rows.Count > 0) {
                            KodeProdSwitchOut = dsDataOut.Tables[0].Rows[0]["ProdSwitchOut"].ToString();
                            KodeProdSwitchIn = dsDataOut.Tables[0].Rows[0]["ProdSwitchIn"].ToString();
                        }
                    }

                    sqlPar = new List<SQLSPParameter>();
                    sqlPar.Add(new SQLSPParameter("@pnProdSwitchOut", KodeProdSwitchOut));
                    sqlPar.Add(new SQLSPParameter("@pnClientSwitchOut", paramIn.Data.ClientId));
                    sqlPar.Add(new SQLSPParameter("@pmUnit", paramIn.Data.TranUnit));
                    sqlPar.Add(new SQLSPParameter("@pcFeeCCY", paramIn.Data.FeeCCY = "", ParamDirection.OUTPUT)); //3
                    sqlPar.Add(new SQLSPParameter("@pnFee", paramIn.Data.Fee = 0, ParamDirection.OUTPUT));//4
                    sqlPar.Add(new SQLSPParameter("@pnNIK", paramIn.UserNIK));
                    sqlPar.Add(new SQLSPParameter("@pcGuid", paramIn.MessageGUID));
                    sqlPar.Add(new SQLSPParameter("@pcIsEdit", paramIn.Data.IsFeeEdit));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageInput", paramIn.Data.PercentageFeeInput));
                    sqlPar.Add(new SQLSPParameter("@pdPercentageOutput", paramIn.Data.PercentageFeeOutput, ParamDirection.OUTPUT));//9
                    sqlPar.Add(new SQLSPParameter("@pcCIFNo", paramIn.Data.CIFNo));
                    sqlPar.Add(new SQLSPParameter("@pnProdSwitchIn", KodeProdSwitchIn));
                    sqlPar.Add(new SQLSPParameter("@pcRefID", ""));
                    //sqlPar.Add(new SQLSPParameter("@pnIsRDB", paramIn.Data.IsRDB = 0, ParamDirection.OUTPUT)); //13



                    if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReka2, this._ignoreSSL, spName, ref sqlPar, out dsDataOut, out errMsg))
                        throw new Exception(errMsg);

                    if (!errMsg.EndsWith(""))
                        throw new Exception(errMsg);

                    msgResponse.Data = new RateRes();
                    msgResponse.Data.FeeCCY = sqlPar[3].ParameterValue.ToString();
                    msgResponse.Data.Fee = Convert.ToDecimal(sqlPar[4].ParameterValue.ToString());
                    msgResponse.Data.PercentageFeeOutput = Convert.ToDecimal(sqlPar[9].ParameterValue.ToString());
                    //msgResponse.Data.IsRDB = Convert.ToInt32(sqlPar[13].ParameterValue.ToString());
                    msgResponse.IsSuccess = true;
                }
                //20230725, Andi, RDN-1017, Switching RDB, end
            }
            catch (Exception ex)
            {
                msgResponse.IsSuccess = false;
                msgResponse.ErrorDescription = ex.Message;
            }
            return msgResponse;
        }

        #endregion Inquiry Rate with WS

        #region Inquiry Tiering Notification 
        public ApiMessage<List<TieringNotificationRes>> InquiryTieringNotification(ApiMessage<TieringNotificationReq> paramIn)
        {
            ApiMessage<List<TieringNotificationRes>> ApiMsgResponse = new ApiMessage<List<TieringNotificationRes>>();
            List<TieringNotificationRes> listResponse = new List<TieringNotificationRes>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();

            ApiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                string sqlCommand = @"
                    SELECT * 
                    FROM ReksaTieringNotification_TM
                ";

                if (paramIn.Data.TrxType != null && paramIn.Data.ProdId != null)
                {
                    sqlCommand = sqlCommand + "where TrxType='" + paramIn.Data.TrxType + "' and ProdId=" + paramIn.Data.ProdId;
                }
                else if (paramIn.Data.TrxType != null)
                {
                    sqlCommand = sqlCommand + "where TrxType='" + paramIn.Data.TrxType + "'";
                }
                else if (paramIn.Data.ProdId != null)
                {
                    sqlCommand = sqlCommand + "where ProdId=" + paramIn.Data.ProdId;
                }
                #endregion

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut == null || dsOut.Tables.Count == 0 || dsOut.Tables[0].Rows.Count == 0)
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    #region mapping sesuai tipe data agar tidak merubah 
                    listResponse = (from DataRow dr in dsOut.Tables[0].Rows
                                    select new TieringNotificationRes()
                                    {
                                        TrxType = dr["TrxType"] != DBNull.Value ? dr["TrxType"].ToString() : "",
                                        ProdId = dr["ProdId"] != DBNull.Value ? Convert.ToInt32(dr["ProdId"].ToString()) : 0,
                                        PercentFrom = dr["PercentFrom"] != DBNull.Value ? Convert.ToDecimal(dr["PercentFrom"]) : 0,
                                        PercentTo = dr["PercentTo"] != DBNull.Value ? Convert.ToDecimal(dr["PercentTo"]) : 0,
                                        MustApproveBy = dr["MustApproveBy"] != DBNull.Value ? dr["MustApproveBy"].ToString() : ""
                                    }).OrderBy(o => o.ProdId).ToList();
                    #endregion mapping sesuai tipe data agar tidak merubah

                    ApiMsgResponse.Data = listResponse;
                    ApiMsgResponse.IsSuccess = true;
                }

                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call SOAP service failed";
                    return ApiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorDescription = ex.Message;
            }

            return ApiMsgResponse;
        }
        #endregion Inquiry Tiering Notification 

        #region InquiryMutualFundsFee
        public ApiMessage<InquiryMutualFundsFeeRes> InquiryMutualFundsFee(ApiMessage<InquiryMutualFundsFeeReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<InquiryMutualFundsFeeRes> apiMsgResponse = new ApiMessage<InquiryMutualFundsFeeRes>();
            List<InquiryMutualFundsFeeRes> dataResponse = new List<InquiryMutualFundsFeeRes>();

            apiMsgResponse.copyHeaderForReply(paramIn);

            #region Query
            string sqlCommand = @"
                    declare @dCurrWorkingDate	datetime
                            , @cErrMsg	        varchar(100)
							, @pcProdCode		varchar(10)
							, @pcProdCodeTarget	varchar(10)	

                    declare @temp_feeparam table(
	                  ProdId			int
	                , ProdIdTarget		int
                    , ProdCode			varchar(10)
	                , ProdCodeTarget	varchar(10)
                    , DefaultFee		decimal(25,13)
                    , MinimumFee		decimal(25,13)
                    , MaximumFee		decimal(25,13)
                    , IsEditable		tinyint
                    )

                select @dCurrWorkingDate = current_working_date          
                from control_table 

                select @pcProdCode = ProdCode
                from ReksaProduct_TM
	                where ProdId = @pnProdId

                select @pcProdCodeTarget = ProdCode
                from ReksaProduct_TM
	                where ProdId = @pnProdIdTarget   

                -- kumpulan validasi global
                if @pnProdId is null
                begin
	                set @cErrMsg = 'Id Produk harus diisi'
	                goto ERROR
                end
                else if @pcTipeTrx is null
                begin
	                set @cErrMsg = 'Tipe Transaksi harus diisi'
	                goto ERROR
                end
                else if (@pbFlagRDB is null)
                begin
	                set @cErrMsg = 'Flag RDB Mandatory untuk tipe trx ' + @pcTipeTrx
	                goto ERROR
                end
                else if (isnull(@pbFlagRDB,0) not in (0,1))
                begin
	                set @cErrMsg = 'Flag RDB harus 0 atau 1 untuk tipe trx ' + @pcTipeTrx
	                goto ERROR
                end
                else if (@pbFlagEmployee is null)
                begin
	                set @cErrMsg = 'Flag Employee Mandatory untuk tipe trx ' + @pcTipeTrx
	                goto ERROR
                end
                else if (isnull(@pbFlagEmployee,0) not in (0,1))
                begin
	                set @cErrMsg = 'Flag Employee harus 0 atau 1 untuk tipe trx ' + @pcTipeTrx
	                goto ERROR
                end
                -- kumpulan validasi khusus RDB
                else if	(@pcTipeTrx in ('REDEMP'))
	                and (isnull(@pbFlagRDB,0)=1)
	                and (@bIsBerhenti is null) 
                begin
	                set @cErrMsg = 'Flag Berhenti Mandatory untuk tipe trx ' + @pcTipeTrx + ' RDB'
	                goto ERROR
                end
                else if	(@pcTipeTrx in ('REDEMP'))
	                and (isnull(@pbFlagRDB,0)=1)
	                and (@bIsBerhenti not in (0,1)) 
                begin
	                set @cErrMsg = 'Flag Berhenti harus 0 atau 1 untuk tipe trx ' + @pcTipeTrx + ' RDB'
	                goto ERROR
                end
                else if	(@pcTipeTrx in ('REDEMP','SWC'))
	                and (isnull(@pbFlagRDB,0)=1)
	                and (@bIsAsuransi is null) 
                begin
	                set @cErrMsg = 'Flag Asuransi Mandatory untuk tipe trx ' + @pcTipeTrx + ' RDB'
	                goto ERROR
                end
                else if	(@pcTipeTrx in ('REDEMP','SWC'))
	                and (isnull(@pbFlagRDB,0)=1)
	                and (@bIsAsuransi not in (0,1)) 
                begin
	                set @cErrMsg = 'Flag Asuransi harus 0 atau 1 untuk tipe trx ' + @pcTipeTrx + ' RDB'
	                goto ERROR
                end
                -- kumpulan validasi switching
                else if (@pcTipeTrx='SWC') and (isnull(@pnProdIdTarget, 0) = 0)
                begin
	                set @cErrMsg = 'Id Produk Tujuan Pengalihan Mandatory untuk tipe trx ' + @pcTipeTrx
	                goto ERROR
                end
                else if (@pcTipeTrx='SWC') and not exists(
	                select top 1 1
	                from ReksaProdSwitchingParam_TR
		                where ProdSwitchOut = @pcProdCode
		                and ProdSwitchIn = @pcProdCodeTarget
	                )
                begin
	                set @cErrMsg = 'Fee Pengalihan dari Produk ' + @pcProdCode + ' ke ' + @pcProdCodeTarget + ' tidak terdaftar'
	                goto ERROR
                end

                -- logic subscription
                if(@pcTipeTrx = 'SUBS')
                begin
                -- logic subscription lumpsum
                    if (@pbFlagRDB = 0)
                    begin
                        insert @temp_feeparam
                        select  ProdId
				                , 0
				                , @pcProdCode
				                , ''
				                , case when @pbFlagEmployee = 1 then MaxPctFeeEmployee else MaxPctFeeNonEmployee end as 'DefaultFee'
				                , case when @pbFlagEmployee = 1 then MinPctFeeEmployee else MinPctFeeNonEmployee end as 'MinimumFee'
				                , case when @pbFlagEmployee = 1 then MaxPctFeeEmployee else MaxPctFeeNonEmployee end as 'MaximumFee'
				                , 1 as 'IsEditable'
		                from ReksaParamFee_TM with(nolock) 
                                where TrxType = 'SUBS'
                                and ProdId = @pnProdId
		                goto HYPERJUMP
                    end
	                -- variasi RDB only
	                else if (@pbFlagRDB = 1) 
                    begin
		                insert @temp_feeparam
                        select  ProductId
				                , 0
				                , @pcProdCode
				                , ''
				                , SubsFee as 'DefaultFee'
				                , SubsFee as 'MinimumFee'
				                , SubsFee as 'MaximumFee'
				                , 0 as 'IsEditable'
                        from ReksaRegulerSubscription_TR with(nolock)
                            where ProductId = @pnProdId
		                goto HYPERJUMP
                    end
                end

                -- logic redemption
                if(@pcTipeTrx = 'REDEMP')
                begin
                -- logic redemption lumpsum
                    if (@pbFlagRDB = 0)
                    begin
                        insert @temp_feeparam
                        select top 1
				                  ProdId
				                , 0
				                , @pcProdCode
				                , ''
                                , isnull(rrp.Fee, 0)					as 'DefaultFee'
                                --, isnull(rpf.MinPctFeeNonEmployee, 0)	as 'MinimumFee'
                                , case when @pbFlagEmployee = 1 then rpf.MinPctFeeEmployee else rpf.MinPctFeeNonEmployee end as 'MinimumFee'
                                , isnull(rrp.Fee,0)						as 'MaximumFee'
                                , 1										as 'IsEditable'
                                --, case when Period = @pnPeriod then rpf.MaxPctFeeNonEmployee else isnull(rrp.Fee, 0) end as 'DefaultFee'
                                --, isnull(rpf.MinPctFeeNonEmployee, 0) as 'MinimumFee'
                                --, case when Period = @pnPeriod then rpf.MaxPctFeeNonEmployee else isnull(rrp.Fee, 0) end as 'MaximumFee'
                                --, 1 as 'IsEditable'
                        from ReksaParamFee_TM rpf
                        left join ReksaRedemPeriod_TM rrp
                            on rrp.FeeId = rpf.FeeId
                            where rpf.TrxType = 'REDEMP'
                            and rpf.ProdId = @pnProdId
                            and @pnPeriod <= Period
                            and IsEmployee = @pbFlagEmployee
		                order by Period asc
		                goto HYPERJUMP
                    end
                -- logic redemption rdb
                    else if (@pbFlagRDB = 1)
	                begin
                -- sudah jatuh tempo dan tidak ada tunggakan
		                if (@dCurrWorkingDate >= @dJatuhTempo)
                        and (@bIsBerhenti = 0)
                        begin
			                insert @temp_feeparam
			                select top 1
					                ProductId
					                , 0
					                , @pcProdCode
					                , ''
				                    , 0 as 'DefaultFee'
					                , 0 as 'MinimumFee'
					                , 0 as 'MaximumFee'
					                , 0 as 'IsEditable'
			                from ReksaRegulerSubscription_TR rrs
				                where ProductId = @pnProdId
			                goto HYPERJUMP
		                end
                -- break, ada tunggakan
		                else
		                begin
			                insert @temp_feeparam
			                select top 1
					                ProductId
					                , 0
					                , @pcProdCode
					                , ''
					                , case when @bIsAsuransi = 0 then RedemptFeeNonAsuransi else RedemptFee end as 'DefaultFee'
					                , case when @bIsAsuransi = 0 then RedemptFeeNonAsuransi else RedemptFee end  as 'MinimumFee'
					                , case when @bIsAsuransi = 0 then RedemptFeeNonAsuransi else RedemptFee end  as 'MaximumFee'
					                , 0 as 'IsEditable'
			                from ReksaRegulerSubscription_TR rrs
				                where ProductId = @pnProdId
			                goto HYPERJUMP
		                end
	                end
                end

                -- logic switching
                if(@pcTipeTrx = 'SWC')
                begin
                -- logic switching lumpsum
	                if (@pbFlagRDB = 0)
	                begin
		                insert @temp_feeparam(
			                  ProdId
			                , ProdIdTarget
			                , ProdCode
			                , ProdCodeTarget
			                , DefaultFee
			                , IsEditable
			                )
		                select 
				                  @pnProdId
				                , @pnProdIdTarget
				                , @pcProdCode
				                , @pcProdCodeTarget
				                , case when @pbFlagEmployee = 1 then SwitchingFeeKaryawan
				                  else SwitchingFee end as 'DefaultFee'
				                , 1 as 'IsEditable'
		                from ReksaProdSwitchingParam_TR
			                where ProdSwitchOut = @pcProdCode
				                and ProdSwitchIn = @pcProdCodeTarget
	
		                update tfp
		                set	   MinimumFee = case when @pbFlagEmployee = 1 then MinPctFeeEmployee
				                  else MinPctFeeNonEmployee end --as 'MinimumFee'
			                ,  MaximumFee = case when @pbFlagEmployee = 1 then MaxPctFeeEmployee
				                  else MaxPctFeeNonEmployee end --as 'MaximumFee'
		                from ReksaParamFee_TM rpf
		                join @temp_feeparam tfp
			                --20220726, Rendy, M32022-6, begin
                            --on rpf.ProdId = tfp.ProdId
                            on rpf.ProdId = tfp.ProdIdTarget
                            --20220726, Rendy, M32022-6, end
			                where TrxType = 'SWC'
			                and tfp.ProdId = @pnProdId
		                goto HYPERJUMP
	                end
                -- logic switching RDB
	                else if (@pbFlagRDB = 1)
	                begin
		                insert @temp_feeparam
		                select top 1
				                  @pnProdId
				                , @pnProdIdTarget
				                , @pcProdCode
				                , @pcProdCodeTarget
				                , case when @bIsAsuransi = 1 then SwcFeeAsuransi else SwcFeeNonAsuransi end as 'DefaultFee'
				                , case when @bIsAsuransi = 1 then SwcFeeAsuransi else SwcFeeNonAsuransi end as 'MinimumFee'
				                , case when @bIsAsuransi = 1 then SwcFeeAsuransi else SwcFeeNonAsuransi end as 'MaximumFee'
				                , 0 as 'IsEditable'
		                from ReksaRegulerSubscription_TR rrs
--20230725, Andi, RDN-1017, Switching RDB, begin
			                --where ProductId = @pnProdId
                            where ProductId = @pnProdIdTarget
--20230725, Andi, RDN-1017, Switching RDB, end
	                goto HYPERJUMP
	                end
                end

                return 

                HYPERJUMP:
                set @cErrMsg = 'API Read Success'
                select *, @cErrMsg as 'ResponseMessage'
                from @temp_feeparam

                return

                ERROR:
                insert @temp_feeparam
	                select @pnProdId, @pnProdIdTarget, @pcProdCode, @pcProdCodeTarget, null, null, null, null

                select *, @cErrMsg as 'ResponseMessage'
                from @temp_feeparam

                return
                ";
            #endregion

            string errMsg = "";
            DataSet dsResult = new DataSet();
            
            try
            {
                SqlParameter[] sqlParam = new SqlParameter[9];
                sqlParam[0] = new SqlParameter("@pnProdId", paramIn.Data.ProdId);
                sqlParam[1] = new SqlParameter("@pnProdIdTarget", paramIn.Data.ProdIdTarget);
                sqlParam[2] = new SqlParameter("@pcTipeTrx", paramIn.Data.TipeTrx);
                sqlParam[3] = new SqlParameter("@pnPeriod", paramIn.Data.Period);
                sqlParam[4] = new SqlParameter("@pbFlagRDB", paramIn.Data.FlagRDB);
                sqlParam[5] = new SqlParameter("@pbFlagEmployee", paramIn.Data.FlagEmployee);
                sqlParam[6] = new SqlParameter("@dJatuhTempo", paramIn.Data.JatuhTempo.ToString("yyyyMMdd"));
                sqlParam[7] = new SqlParameter("@bIsBerhenti", paramIn.Data.IsBerhenti);
                sqlParam[8] = new SqlParameter("@bIsAsuransi", paramIn.Data.IsAsuransi);


                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, ref sqlParam, out dsResult, out errMsg))
                    throw new Exception(errMsg);

                if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                    throw new Exception("Data Mutual Funds Fee not found !");

                if (dsResult.Tables[0].Rows[0]["responseMessage"].ToString() != "API Read Success")
                    throw new Exception(dsResult.Tables[0].Rows[0]["responseMessage"].ToString());

                dataResponse = JsonConvert.DeserializeObject<List<InquiryMutualFundsFeeRes>>(
                                        JsonConvert.SerializeObject(dsResult.Tables[0],
                                                Newtonsoft.Json.Formatting.None,
                                                new JsonSerializerSettings
                                                {
                                                    NullValueHandling = NullValueHandling.Ignore
                                                }));

                apiMsgResponse.Data = dataResponse[0];
                apiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                apiMsgResponse.ErrorDescription = ex.Message;
                apiMsgResponse.IsSuccess = false;
            }
            finally
            {
                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }

                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }
        #endregion

        #region Mapping switch out to switch in
        public ApiMessage<List<MappingSwitchOutToSwitchInRes>> MappingSwitchOutToSwitchIn(ApiMessage<MappingSwitchOutToSwitchInReq> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<List<MappingSwitchOutToSwitchInRes>> apiMsgResponse = new ApiMessage<List<MappingSwitchOutToSwitchInRes>>();
            List<MappingSwitchOutToSwitchInRes> listResponse = new List<MappingSwitchOutToSwitchInRes>();

            string errMsg = "";
            string strQuery = "";
            DataSet dsResult = null;

            apiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                strQuery = @"    
                
                declare @cProdCode varchar(100)
                , @dtValueDateNAV datetime
                , @dtValueDateKP datetime
--20230506, Lita, RDN-944, begin
	, @cCIFNo	varchar(20)
	, @dCurrWorkingDate  datetime
	, @dNextWorkingDate  datetime  
    , @nCutOff     int  
    , @bProcessStatus  bit  
    , @dCutOff datetime  
    , @dToday  datetime 
	, @bIsRDB bit
	, @bIsRDBMature bit 
	, @cFreqDebetMethod varchar(1)
	, @bIsInsurance bit
	, @mTranAmount decimal(25,13)
	, @nIsEmployee tinyint			
	, @bIsDoneDebet bit
--20230506, Lita, RDN-944, end

--20230506, Lita, RDN-944, begin
                --select top 1 @dtValueDateNAV = ValueDate
                --from ReksaNAVParam_TH with(Nolock)
                --order by ValueDate desc

                --select * into #temp_Product
                --from ReksaProduct_TM

                --initialize temp table - begin
                create table #tempListClientId (        
                    CIFNo      varchar(20),        
                    ClientId     int, 
	                ClientCode     varchar(20), 
                    ProdId      int        
                    )   
                create index IDX_TEMP_CLIENT ON #tempListClientId(ClientId, ProdId)         
        
                create table #tempListProdSwcIn (        
	                ProdId      int  
	                , MinSwitchRedempt decimal(25,13)
	                , JenisSwitchRedempt varchar(10)      
                )  
                create index IDX_TEMP_PROD ON #tempListProdSwcIn(ProdId)    
   
                create table #tempListClientTrx (  
                    ClientId        int  
                )    
                create index IDX_TEMP_TRX ON #tempListClientTrx(ClientId)    
    
                create table #tempRDBParam (ProdId int, DebetMethodCode varchar(20)) 

                create table #tempTampil (ProdId int, ProdSwitchIn varchar(10), ClientId int, ClientCode varchar(20), MinSwitchRedempt decimal(25,13), JenisSwitchRedempt varchar(10))

                --initialize temp table - end

                select @dtValueDateNAV = max(ValueDate)        
                from dbo.ReksaNAVParam_TH with(Nolock)

                select @dCurrWorkingDate = current_working_date  
                    , @dNextWorkingDate = next_working_date               
                        from dbo.fnGetWorkingDate()      

                select @dToday = current_working_date from dbo.control_table  
                set @dCutOff = dateadd (minute, @nCutOff, @dCurrWorkingDate)              
                set @dCurrWorkingDate = dateadd(day, datediff(day, getdate(), @dCurrWorkingDate), getdate())  

                if (@dCurrWorkingDate < @dCutOff) and (@bProcessStatus = 0)              
                begin               
                    set @dCurrWorkingDate = convert(varchar,@dCurrWorkingDate,112)        
                end              
                else              
                begin                        
                    If @dToday != convert(datetime, convert(char(8),@dCurrWorkingDate,112))
                        set @dCurrWorkingDate = convert(varchar,@dCurrWorkingDate,112)       
                    else              
                        set @dCurrWorkingDate = convert(varchar,@dNextWorkingDate,112)                             
                end   

                select @bIsRDB = 0, @bIsRDBMature = 0, @bIsDoneDebet = 1
                select @cCIFNo = CIFNo from dbo.ReksaCIFData_TM WHERE ClientId = @pnClientIdSwcOut

                if exists(select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM with(nolock) where ClientId =  @pnClientIdSwcOut)
	                select @bIsRDB = 1

                if (@bIsRDB = 1)
                begin
	                select @cFreqDebetMethod = a.FreqDebetMethod, @bIsInsurance = a.Asuransi, @mTranAmount = a.TranAmount, @nIsEmployee = c.IsEmployee
			                , @bIsRDBMature  = case when datediff(d, a.JatuhTempo, getdate()) >= 0 then 1 else 0 end
	                from ReksaRegulerSubscriptionClient_TM a join ReksaCIFData_TM b on a.ClientId = b.ClientId 
	                join ReksaMasterNasabah_TM c on b.CIFNo = c.CIFNo 
	                where a.ClientId = @pnClientIdSwcOut 

	                if (@cFreqDebetMethod = 'D')
	                begin
		                if exists(select top 1 1 from dbo.ReksaRegulerSubscriptionSchedule_TT  
		                where ClientId = @pnClientIdSwcOut  
		                and StatusId in (0)  
		                and Type = 0)  
		                begin  
			                set @bIsDoneDebet = 0
		                end
	                end
	                else
	                begin
		                if exists(select top 1 1 from dbo.ReksaRegulerSubscriptionSchedule_TT  
		                where ClientId = @pnClientIdSwcOut
		                and StatusId in (0,3,5,6) 
		                and Type = 0
		                )  
		                begin  
			                set @bIsDoneDebet = 0
		                end  
	                end
                end
--20230506, Lita, RDN-944, end

                select * into #temp_NAV
                from ReksaNAVParam_TH
                where ValueDate = @dtValueDateNAV
--20230506, Lita, RDN-944, begin
                --order by ValueDate desc
--20230506, Lita, RDN-944, end

--20230506, Lita, RDN-944, begin
                --select top 1 @dtValueDateKP = ValueDate
                --from ReksaKinerjaProduct_TM with(Nolock)
                --order by ValueDate desc

                select @dtValueDateKP = max(ValueDate) from dbo.ReksaKinerjaProduct_TM with(Nolock)
--20230506, Lita, RDN-944, end
                
                select * into #temp_KP
                from ReksaKinerjaProduct_TM
                where ValueDate = @dtValueDateKP

--20230506, Lita, RDN-944, begin
                --update tp
                --set tp.NAV = tn.NAV
                --from #temp_Product tp
                --join #temp_NAV tn
                --on tp.ProdId = tn.ProdId
--20230506, Lita, RDN-944, end

                select @cProdCode = ProdCode
                from ReksaProduct_TM
                where ProdId = @pnProdId

--20230506, Lita, RDN-944, begin
insert #tempListProdSwcIn (ProdId, MinSwitchRedempt, JenisSwitchRedempt)        
select distinct rp.ProdId, rpp.MinSwitchRedempt, rpp.JenisSwitchRedempt
from dbo.ReksaProdSwitchingParam_TR rpp     with (nolock)    
join dbo.ReksaProduct_TM rp       with (nolock)  
on rp.ProdCode = rpp.ProdSwitchIn        
where rpp.ProdSwitchOut = @cProdCode  
and rp.Status = 1

insert #tempListClientId (CIFNo, ClientId, ClientCode, ProdId)        
select rc.CIFNo, rc.ClientId, rc.ClientCode, rc.ProdId        
from dbo.ReksaCIFData_TM rc       with (nolock)  
left join dbo.ReksaRegulerSubscriptionClient_TM rg    with (nolock)     
on rc.ClientId = rg.ClientId        
join dbo.ReksaProduct_TM rp   with (nolock)      
on rc.ProdId = rp.ProdId        
join #tempListProdSwcIn si        
on si.ProdId = rp.ProdId        
where rc.CIFNo = @cCIFNo        
and rc.CIFStatus = 'A'        
and rp.CloseEndBit = 0  
and rg.ClientId is null  

insert #tempListClientTrx (ClientId)  
select tt.ClientId   
from dbo.ReksaTransaction_TT tt with (nolock)  
join #tempListClientId tl  
on tt.ClientId = tl.ClientId  
where TranType = 1 and Status = 1  
union   
select tt.ClientId   
from dbo.ReksaTransaction_TH tt with (nolock)  
join #tempListClientId tl  
on tt.ClientId = tl.ClientId      
where TranType = 1 and Status = 1     
union  
select tt.ClientId   
from dbo.ReksaTransaction_TT tt with (nolock)  
join #tempListClientId tl  
on tt.ClientId = tl.ClientId      
where TranType = 8 and Status = 1 and RegSubscriptionFlag = 1      
union  
select tt.ClientId   
from dbo.ReksaTransaction_TH tt with (nolock)  
join #tempListClientId tl  
on tt.ClientId = tl.ClientId      
where TranType = 8 and Status = 1 and RegSubscriptionFlag = 1            

delete tl        
from #tempListClientId tl        
join dbo.ReksaExceptionClient_TR tr    
on tr.ClientId = tl.ClientId        
                  
delete tl        
from #tempListClientId tl        
join dbo.ReksaCIFData_TM rc       
on tl.ClientId = rc.ClientId        
where isnull(rc.Flag, 0) = 1     

insert #tempTampil (ProdId, ProdSwitchIn, MinSwitchRedempt, JenisSwitchRedempt)
select a.ProdId, b.ProdCode,  a.MinSwitchRedempt, a.JenisSwitchRedempt
from #tempListProdSwcIn a join ReksaProduct_TM b on a.ProdId = b.ProdId
where a.ProdId not in (select ProdId from #tempListClientId)     

--20230830, Lita, RDN-1017, begin
--if ((@bIsRDB = 1 and @bIsRDBMature = 1) or @bIsRDB  = 0)
--20241212, Andhika J, RDN-1195, begin
--if ((@bIsRDB = 1 and @bIsDoneDebet = 1) or @bIsRDB  = 0)
--20241212, Andhika J, RDN-1195, end
--20230830, Lita, RDN-1017, end
--20241212, Andhika J, RDN-1195, begin
--begin 
--20241212, Andhika J, RDN-1195, end
--20241112, Andhika J, RDN-1195, begin
	--delete tl        
	--from dbo.ReksaSwitchingTransaction_TM  rst        
	--join #tempListClientId tl   
	--on tl.ClientId = rst.ClientIdSwcOut        
	--where rst.TranType in (5, 6)         
	--and rst.Status in (0, 1)         
	--and rst.NAVValueDate  = @dCurrWorkingDate         
                 
	--delete tl        
	--from dbo.ReksaTransaction_TT  rst        
	--join #tempListClientId tl        
	--on tl.ClientId = rst.ClientId        
	--where rst.TranType in (3, 4)        
	--and rst.Status in (0, 1)         
	--and rst.NAVValueDate  = @dCurrWorkingDate   
  
	--delete tl      
	--from dbo.ReksaTransaction_TT  rst      
	--join #tempListClientId tl      
	--on tl.ClientId = rst.ClientId    
	--where rst.TranType in (1)      
	--and rst.Status in (0, 1)     
	--and rst.BillId is null    
	--and rst.NAVValueDate  = @dCurrWorkingDate   
        
	--delete tl  
	--from dbo.ReksaSwitchingTransaction_TM  rst      
	--join #tempListClientId tl      
	--on tl.ClientId = rst.ClientIdSwcIn  
	--where rst.TranType in (5,6)      
	--and rst.Status in (0, 1)           
	--and rst.BillId is null  
	--and rst.NAVValueDate  = @dCurrWorkingDate   
	--and tl.ClientId not in (select ClientId from #tempListClientTrx)   
--20241112, Andhika J, RDN-1195, end
--20241212, Andhika J, RDN-1195, begin
--end 
--20241212, Andhika J, RDN-1195, end

insert #tempTampil (ProdId, ProdSwitchIn, ClientId, ClientCode, MinSwitchRedempt, JenisSwitchRedempt)
select a.ProdId, b.ProdCode,  ClientId, ClientCode, c.MinSwitchRedempt, c.JenisSwitchRedempt
from #tempListClientId a join ReksaProduct_TM b on a.ProdId = b.ProdId 
join #tempListProdSwcIn c on a.ProdId = c.ProdId

--add RDB Filter
--if (@bIsRDB = 1 and (@bIsDoneDebet = 0 or @bIsRDBMature = 0))
if (@bIsRDB = 1 and @bIsDoneDebet = 0)
begin
	--filter begin
	insert into #tempRDBParam
	select a.ProdId, case 
							when @cFreqDebetMethod = 'D' then DailyParamCode 
							when @cFreqDebetMethod = 'W' then WeeklyParamCode 
							when @cFreqDebetMethod = 'M' then MonthlyParamCode 
						end
	from #tempTampil a join  ReksaRegulerSubscription_TR b on a.ProdId = b.ProductId

	--filter bisa RDB/ga
	delete a 
	from #tempTampil a left join ReksaRegulerSubscription_TR b 
	on a.ProdId = b.ProductId 
	where b.ProductId is null

	--filter bisa D/W/M
	if (@cFreqDebetMethod = 'D')
	begin
		delete a 
		from #tempTampil a join ReksaRegulerSubscription_TR b
		on a.ProdId = b.ProductId
		where @cFreqDebetMethod = 'D' and b.IsDaily = 0 
	end

	if (@cFreqDebetMethod = 'W')
	begin
		delete a 
		from #tempTampil a join ReksaRegulerSubscription_TR b
		on a.ProdId = b.ProductId
		where @cFreqDebetMethod = 'W' and b.IsWeekly = 0 
	end

	if (@cFreqDebetMethod = 'M')
	begin
		delete a 
		from #tempTampil a join ReksaRegulerSubscription_TR b
		on a.ProdId = b.ProductId
		where @cFreqDebetMethod = 'M' and b.IsMonthly = 0 

	end

	-- filter isallowinsurance
	delete a 
	from #tempTampil a join #tempRDBParam b on a.ProdId = b.ProdId
	join ReksaFrekPendebetanParam_TR par on b.DebetMethodCode = par.DebetMethodCode
	where @bIsInsurance = 1 and par.IsAllowInsurance = 0 

	--filter minsubs RDB
	delete a 
	from #tempTampil a join ReksaRegulerSubscriptionParam_TR b 
	on a.ProdId = b.ProductId and b.IsEmployee = @nIsEmployee and b.ParamValue > @mTranAmount 
	where b.ParamId = 'SubscMin'
	--filter end

	update #tempTampil 
	set ClientId = 0, ClientCode = ''
end

--20230506, Lita, RDN-944, end
--20250925, Andhika J, RDN-1264, begin
alter table #tempTampil
add IsSubsNew bit 

update #tempTampil
set IsSubsNew = 1

update tmp
set IsSubsNew = 0
from ReksaCIFData_TM rcd
join #tempTampil tmp
	on rcd.ClientId = tmp.ClientId 
	   and rcd.ProdId = tmp.ProdId
where rcd.CIFStatus != 'T'

update tmp
set IsSubsNew = 0
from ReksaCIFData_TM rcd
join dbo.ReksaRegulerSubscriptionClient_TM rrs
    on rcd.ClientId = rrs.ClientId
join #tempTampil tmp
	on rcd.ClientId = tmp.ClientId and rcd.ProdId = tmp.ProdId
where rcd.CIFStatus != 'T'

update a
set MinSwitchRedempt = case when IsSubsNew = 1 then b.MinSwitchRedempt else MinSwitchAdd end
from #tempTampil a
join ReksaProdSwitchingParam_TR b 
	on a.ProdSwitchIn = b.ProdSwitchIn  
where b.ProdSwitchOut = @cProdCode
--20250925, Andhika J, RDN-1264, end

                select distinct rs.ProdSwitchIn, rp2.ProdName, rp2.ProdId, tn.NAV--rp2.NAV
                , @dtValueDateNAV as NAVValueDate
                , rprp.RiskProfile
                , rdrp.RiskProfileDesc
                , rdrp.RiskProfileDescEN
                , rt.TypeId
                , rt.TypeCode as 'ProductCategoryCode'
                , rt.TypeName as 'ProductCategoryName' -- bahasa indonesia
                , rt.TypeNameEnglish as 'ProductCategoryNameEN' -- english
                , kp.ValueDate as 'kinerjaValueDate'
                , kp.Sehari
                , kp.Seminggu
                , kp.Sebulan
                , kp.Setahun
                , rs.MinSwitchRedempt
                , rs.JenisSwitchRedempt
--20230506, Lita, RDN-944, begin
                --from dbo.ReksaProdSwitchingParam_TR rs with (nolock)
                --join dbo.#temp_Product rp on rs.ProdSwitchOut = rp.ProdCode
                , isnull(rs.ClientId, 0) 'ClientId'
                , isnull(rs.ClientCode, '') 'ClientCode'
                from #tempTampil rs
--20230506, Lita, RDN-944, end
                join dbo.ReksaProduct_TM rp2 on rs.ProdSwitchIn = rp2.ProdCode
--20230506, Lita, RDN-944, begin
                --left join #temp_KP kp on rp.ProdId = kp.ProdId
                left join #temp_KP kp on rp2.ProdId = kp.ProdId
--20230506, Lita, RDN-944, end
                left join ReksaProductRiskProfile_TM rprp ON rprp.ProductCode = rp2.ProdCode
                left join ReksaDescRiskProfile_TR rdrp ON rdrp.RiskProfile = rprp.RiskProfile
                left join ReksaType_TR rt on rp2.TypeId = rt.TypeId
                left join #temp_NAV tn on rp2.ProdId = tn.ProdId
                -- left join ReksaProdSwitchingParam_TR rpsp on rpsp.ProdSwitchOut = rp.ProdCode
--20230506, Lita, RDN-944, begin
                --where rs.ProdSwitchOut = @cProdCode
--20230506, Lita, RDN-944, end

                drop table #temp_NAV
                drop table #temp_KP

--20230506, Lita, RDN-944, begin
                --drop table #temp_Product
                drop table #tempListClientId 
                drop table #tempListClientTrx
                drop table #tempListProdSwcIn
                drop table #tempRDBParam 
                drop table #tempTampil
--20230506, Lita, RDN-944, end

                ";
                #endregion

                //parameter
                //20230506, Lita, RDN - 944, begin
                //SqlParameter[] sqlParam = new SqlParameter[1];
                SqlParameter[] sqlParam = new SqlParameter[2];
                //20230506, Lita, RDN - 944, end
                sqlParam[0] = new SqlParameter("@pnProdId", paramIn.Data.ProdId);
                //20230506, Lita, RDN - 944, begin
                sqlParam[1] = new SqlParameter("@pnClientIdSwcOut", paramIn.Data.ClientIdSwcOut);
                //20230506, Lita, RDN - 944, end

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, strQuery, ref sqlParam, out dsResult, out errMsg))
                {
                    if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                        throw new Exception("Data Product Switch In not found !");

                    //if (dsResult.Tables[1].Columns[0].ToString() != "Column1")
                    //    throw new Exception(dsResult.Tables[1].Columns[0].ToString());

                    listResponse = JsonConvert.DeserializeObject<List<MappingSwitchOutToSwitchInRes>>(JsonConvert.SerializeObject(dsResult.Tables[0]));
                    apiMsgResponse.Data = listResponse;
                    apiMsgResponse.IsSuccess = true;
                }
                //20230506, Lita, RDN - 944, begin
                else
                    throw new Exception(errMsg);
                //20230506, Lita, RDN - 944, end
            }
            catch (Exception ex)
            {
                apiMsgResponse.ErrorDescription = ex.Message;
                apiMsgResponse.IsSuccess = false;
            }
            finally
            {

                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }

                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }
        #endregion Mapping switch out to switch in

        #region Fund Fact Sheet & Prospectus
        public ApiMessage<List<FFSAndProspectusResponse>> FundFactSheetAndProspectus(ApiMessage<FFSAndProspectusRequest> paramIn)
        {
            DatabaseConnectorMsSQL databaseConnector = new DatabaseConnectorMsSQL(this._strConnReksa);
            ApiMessage<List<FFSAndProspectusResponse>> apiMsgResponse = new ApiMessage<List<FFSAndProspectusResponse>>();
            List<FFSAndProspectusResponse> listResponse = new List<FFSAndProspectusResponse>();

            string errMsg = "";
            string strQuery = "";
            DataSet dsResult = null;

            apiMsgResponse.copyHeaderForReply(paramIn);

            try
            {
                #region Query
                strQuery = @"    
                
                select * 
                from ReksaUploadPDFIBMB_TM
                WHERE ProdId = @pnProdId
                
                ";
                #endregion

                //parameter
                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pnProdId", paramIn.Data.ProdId);

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, strQuery, ref sqlParam, out dsResult, out errMsg))
                {
                    if (dsResult == null || dsResult.Tables.Count.Equals(0) || dsResult.Tables[0].Rows.Count.Equals(0))
                        throw new Exception("Data Fund Fact Sheet & Prospectus not found !");

                    listResponse = JsonConvert.DeserializeObject<List<FFSAndProspectusResponse>>(JsonConvert.SerializeObject(dsResult.Tables[0]));
                    apiMsgResponse.Data = listResponse;
                    apiMsgResponse.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                apiMsgResponse.ErrorDescription = ex.Message;
                apiMsgResponse.IsSuccess = false;
            }
            finally
            {

                if (databaseConnector != null)
                {
                    databaseConnector.Dispose();
                    databaseConnector = null;
                }

                apiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return apiMsgResponse;
        }
        #endregion Fund Fact Sheet & Prospectus

        #endregion Inquiry with WS

        #endregion Query

        #region coba ws
        public ApiMessage<object> CobaSelect()
        {
            ApiMessage<object> response = new ApiMessage<object>();
            string strErrMsg = "";
            DataSet dsOut = new DataSet();
            DataSet dsParamOut = new DataSet();
            SqlParameter[] dbPar = new SqlParameter[0];

            try
            {
                string sqlCommand = @"
                    SELECT * FROM dbo.ReksaProduct_TM
                ";

                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                    response.Data = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dsOut));
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorDescription = ex.Message;
            }

            return response;
        }
        #endregion coba ws


        // begin, korvi, RDN-428, 15032021
        #region GetReksaCIFProductList
        public ApiMessage<ReksaCIFProductRes> GetReksaCIFProductList(ApiMessage<ReksaCIFProductReq> paramIn)
        {
            ApiMessage<ReksaCIFProductRes> ApiMsgResponse = new ApiMessage<ReksaCIFProductRes>();
            ApiMsgResponse = _byonLogic.GetReksaCIFProductLogic(paramIn);
            return ApiMsgResponse;
        }

        public ApiMessage<ReksaCIFProductDetailsRes> ReksaCIFProductDetails(ApiMessage<ReksaCIFProductDetailsReq> paramIn)
        {
            ApiMessage<ReksaCIFProductDetailsRes> ApiMsgResponse = new ApiMessage<ReksaCIFProductDetailsRes>();
            ApiMsgResponse = _byonLogic.ReksaCIFProductDetailsLogic(paramIn);
            return ApiMsgResponse;
        }

        public ApiMessage<List<ReksaInquiryPortofolioRes>> ReksaInquiryPortofolio(ApiMessage<ReksaInquiryPortofolioReq> paramIn)
        {
            ApiMessage<List<ReksaInquiryPortofolioRes>> ApiMsgResponse = new ApiMessage<List<ReksaInquiryPortofolioRes>>();
            ApiMsgResponse = _byonLogic.ReksaInquiryPortofolio(paramIn);
            return ApiMsgResponse;
        }

        public ApiMessage<List<ReksaProductSwitchInListRes>> ReksaProductSwitchInList(ApiMessage<ReksaProductSwitchInListReq> paramIn)
        {
            ApiMessage<List<ReksaProductSwitchInListRes>> ApiMsgResponse = new ApiMessage<List<ReksaProductSwitchInListRes>>();
            ApiMsgResponse = _byonLogic.ReksaProductSwitchInList(paramIn);
            return ApiMsgResponse; 
        }

        #endregion GetReksaCIFProductList
        // end, korvi, RDN-428, 15032021
        
        //20210505, Lita, RDN-530, begin
        public ApiMessage<List<ReksaInqTrxCapabilityRes>> ReksaInqTrxCapability(ApiMessage<ReksaInqTrxCapabilityReq> paramIn)
        {
            ApiMessage<List<ReksaInqTrxCapabilityRes>> ApiMsgResponse = new ApiMessage<List<ReksaInqTrxCapabilityRes>>();
            ApiMsgResponse = _byonLogic.ReksaInqTrxCapability(paramIn);
            return ApiMsgResponse;
        }
        //20210505, Lita, RDN-530, end

        //20220722, Andi, RDN-826, begin
        public ApiMessage<List<TopReksaProductRes>> ReksaTopProduct(ApiMessage<TopReksaProductReq> paramIn)
        {
            ApiMessage<List<TopReksaProductRes>> ApiMsgResponse = new ApiMessage<List<TopReksaProductRes>>();
            ApiMsgResponse = _byonLogic.ReksaTopProduct(paramIn);
            return ApiMsgResponse;
        }
        //20220722, Andi, RDN-826, end

        //20220909, Lita, RDN-851, parameter metode debet RDB, begin
        public ApiMessage<List<InquiryRDBParameterRes>> ReksaInqRDBParameter(ApiMessage<InquiryRDBParameterReq> paramIn)
        {
            ApiMessage<List<InquiryRDBParameterRes>> ApiMsgResponse = new ApiMessage<List<InquiryRDBParameterRes>>();
            ApiMsgResponse = _byonLogic.ReksaInqRDBParameter(paramIn);
            return ApiMsgResponse;
        }
        //20220909, Lita, RDN-851, parameter metode debet RDB, end

        //20221018, Andhika J, VELOWEB-1964 , get list parameter product ibmb, begin
        public ApiMessage<List<ReksaProductListRes>> ReksaProductList(ApiMessage<ReksaProductListReq> paramIn)
        {
            ApiMessage<List<ReksaProductListRes>> ApiMsgResponse = new ApiMessage<List<ReksaProductListRes>>();
            ApiMsgResponse = _byonLogic.ReksaProductList(paramIn);
            return ApiMsgResponse;
        }
        //20221018, Andhika J, VELOWEB-1964 , get list parameter product ibmb, end
    
        //20221018, Filian, RDN-865,  Perubahan Asuransi RDB – API One Mobile, begin
        public ApiMessage<ReksaValidateInsuranceRes> ReksaValidateInsurance(ApiMessage<ReksaValidateInsuranceReq> paramIn)
        {
            ApiMessage<ReksaValidateInsuranceRes> ApiMsgResponse = new ApiMessage<ReksaValidateInsuranceRes>();
            ApiMsgResponse = _byonLogic.ReksaValidateInsurance(paramIn);
            return ApiMsgResponse;
        }
        //20221018, Filian, RDN-865,  Perubahan Asuransi RDB – API One Mobile, end

        //20221122, Lita, RDN-865, add service generate RDB File, begin
        public ApiMessage<List<ReksaRDBHTMLRiplayRes>> ReksaRDBGenerateHTMLRiplay(ApiMessage<ReksaRDBHTMLReq> paramIn)
        {
            ApiMessage<List<ReksaRDBHTMLRiplayRes>> ApiMsgResponse = new ApiMessage<List<ReksaRDBHTMLRiplayRes>>();
            ApiMsgResponse = _byonLogic.ReksaRDBGenerateHTMLRiplay(paramIn);
            return ApiMsgResponse;
        }
        //20221122, Lita, RDN-865, add service generate RDB File, end

        //20221125, ahmad.fansyuri, RDN-880, Penambahan service API RMM, begin
        public ApiMessage<ReksaInquiryQuestionRes> ReksaInquiryQuestion(ApiMessage<ReksaInquiryQuestionReq> paramIn)
        {
            ApiMessage<ReksaInquiryQuestionRes> ApiMsgResponse = new ApiMessage<ReksaInquiryQuestionRes>();
            ApiMsgResponse = _byonLogic.ReksaInquiryQuestion(paramIn);
            return ApiMsgResponse;
        }
        //20221125, ahmad.fansyuri, RDN-880, Penambahan service API RMM, end

        //20221129, Filian, RDN-880, Enhancement API for RMM, begin
        public ApiMessage<List<ReksaInquiryRDBInsuranceDataRes>> ReksaInquiryRDBInsuranceData(ApiMessage<ReksaInquiryRDBInsuranceDataReq> paramIn)
        {
            ApiMessage<List<ReksaInquiryRDBInsuranceDataRes>> ApiMsgResponse = new ApiMessage<List<ReksaInquiryRDBInsuranceDataRes>>();
            ApiMsgResponse = _byonLogic.ReksaInquiryRDBInsuranceData(paramIn);
            return ApiMsgResponse;
        }
        //20221129, Filian, RDN-880, Enhancement API for RMM, end
		
		//20221122, Lita, RDN-880, add service generate RDB File, begin
        public ApiMessage<ReksaGeneratePDFRes> ReksaGeneratePDF(ApiMessage<ReksaGeneratePDFReq> paramIn)
        {
            ApiMessage<ReksaGeneratePDFRes> ApiMsgResponse = new ApiMessage<ReksaGeneratePDFRes>();
            ApiMsgResponse = _fileLogic.ReksaGeneratePDF(paramIn);
            return ApiMsgResponse;
        }
        //20221122, Lita, RDN-880, add service generate RDB File, end

        //20230206, Lita, RDN-914, Inquiry Graph NAV, begin
        public ApiMessage<ReksaInquiryNAVPerformanceRes> ReksaInquiryNAVPerformance(ApiMessage<ReksaInquiryNAVPerformanceReq> paramIn)
        {
            ApiMessage<ReksaInquiryNAVPerformanceRes> ApiMsgResponse = new ApiMessage<ReksaInquiryNAVPerformanceRes>();
            ApiMsgResponse = _byonLogic.ReksaInquiryNAVPerformance(paramIn);
            return ApiMsgResponse;
        }
        //20230206, Lita, RDN-914, Inquiry Graph NAV, end

        //20230327, Andhika J, RDN-948, begin
        #region InquiryProductByCode
        public ApiMessage<List<ProductByCodeRes>> InquiryProductByCode(ApiMessage<ProductByCodeReq> paramIn)
        {
            DataSet dsOut = new DataSet();
            String strErrMsg = "";
            ApiMessage<List<ProductByCodeRes>> ApiMsgResponse = new ApiMessage<List<ProductByCodeRes>>();
            List<ProductByCodeRes> listResponse = new List<ProductByCodeRes>();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            dsOut = new DataSet();
            try
            {
                #region Query
                string sqlCommand = @"
                    declare
                    @dtValueDateNAV datetime
                    , @dtValueDateKP datetime

                     -- insert data into temp table
                    select rp.*,rm.ManInvCode,rm.ManInvName, rc.CustodyCode, rc.CustodyName 
                    into #temp_Product
					from dbo.ReksaProduct_TM rp with(nolock)
                    left join dbo.ReksaManInv_TR rm with(nolock)
	                    on rp.ManInvId = rm.ManInvId
                    left join dbo.ReksaCustody_TR rc with(nolock)
	                    on rp.CustodyId = rc.CustodyId
                    where rp.Status = 1

                    select top 1 @dtValueDateNAV = ValueDate
                    from ReksaNAVParam_TH with(Nolock)
                    order by ValueDate desc

                    select * into #temp_NAV
                    from ReksaNAVParam_TH
                    where ValueDate = @dtValueDateNAV

                    select top 1 @dtValueDateKP = ValueDate
                    from ReksaKinerjaProduct_TM with(Nolock)
                    order by ValueDate desc

                    select * into #temp_KP
                    from ReksaKinerjaProduct_TM
                    where ValueDate = @dtValueDateKP

                    -- update related fields
                    update tp
                    set tp.NAV = tn.NAV
                    from #temp_Product tp
                    join #temp_NAV tn
                    on tp.ProdId = tn.ProdId

                    -- Minimal Subs untuk RDB
                    create table #tmpReksaRegulerSubscriptionParam (  
                        ProdId int,
                        MinSubsRDBEmployee decimal(19, 0),
                        MinSubsRDBNonEmployee decimal(19, 0)
                    )  

                    insert into #tmpReksaRegulerSubscriptionParam (ProdId, MinSubsRDBEmployee)
                    select ProductId, ParamValue
                    from ReksaRegulerSubscriptionParam_TR
                    where ParamId = 'SubscMin'
                    and IsEmployee = 1

                    update #tmpReksaRegulerSubscriptionParam
                    set MinSubsRDBNonEmployee = ParamValue
                    from #tmpReksaRegulerSubscriptionParam a join ReksaRegulerSubscriptionParam_TR b
                    on a.ProdId = b.ProductId
                    where b.ParamId = 'SubscMin'
                    and IsEmployee = 0

                     -- display data
                    select
                    rp.*
                    , @dtValueDateNAV as NAVValueDate
                    , rprp.RiskProfile
                    , rdrp.RiskProfileDesc
                    , rdrp.RiskProfileDescEN
                    , rt.TypeId
                    , rt.TypeCode as 'ProductCategoryCode'
                    , rt.TypeName as 'ProductCategoryName' -- bahasa indonesia
                    , rt.TypeNameEnglish as 'ProductCategoryNameEN'-- english
                    , kp.Sehari
                    , kp.Seminggu
                    , kp.Sebulan
                    , kp.Setahun
                    , isnull(rrsp.MinSubsRDBEmployee, -1) as MinSubsRDBEmployee
                    , isnull(rrsp.MinSubsRDBNonEmployee, -1) as MinSubsRDBNonEmployee
                    , rp.ManInvCode as ManInvCode
                    , rp.ManInvName as ManInvName
                    , rp.CustodyCode as CustodyCode
                    , rp.CustodyName as CustodyName
                    from #temp_Product rp
                    left join #temp_KP kp
                    on rp.ProdId = kp.ProdId
                    left join ReksaProductRiskProfile_TM rprp
                    on rprp.ProductCode = rp.ProdCode
                    left join ReksaDescRiskProfile_TR rdrp
                    on rdrp.RiskProfile = rprp.RiskProfile
                    left join ReksaType_TR rt
                    on rp.TypeId = rt.TypeId
                    left join #tmpReksaRegulerSubscriptionParam rrsp
                    on rp.ProdId = rrsp.ProdId
                ";

                if (paramIn.Data.ProdCode != "")
                    sqlCommand = sqlCommand + " where rp.ProdCode like '%" + paramIn.Data.ProdCode + "%'";

                sqlCommand = sqlCommand + " drop table #temp_Product drop table #temp_NAV drop table #temp_KP drop table #tmpReksaRegulerSubscriptionParam";

                #endregion
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {

                    if (dsOut.Tables.Count < 1 || dsOut.Tables[0].Rows.Count.Equals(0))
                    {
                        ApiMsgResponse.IsSuccess = false;
                        ApiMsgResponse.ErrorCode = "3000";
                        ApiMsgResponse.ErrorDescription = "Data not found";
                        return ApiMsgResponse;
                    }

                    listResponse = JsonConvert.DeserializeObject<List<ProductByCodeRes>>(JsonConvert.SerializeObject(dsOut.Tables[0]));

                    ApiMsgResponse.Data = listResponse;
                    ApiMsgResponse.IsSuccess = true;
                }

                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call Query to SQL failed";
                    return ApiMsgResponse;
                }
            }
            catch (Exception ex)
            {
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorDescription = ex.Message;
            }
            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return ApiMsgResponse;
        }
        #endregion InquiryProductByCode
        //20230327, Andhika J, RDN-948, end

        //20230620, Lita, RDN-998, Input Transaction Validation, begin
        #region ReksaInputTrxValidation
        public ApiMessage<List<ReksaInputTrxValidationRes>> ReksaInputTrxValidation(ApiMessage<ReksaInputTrxValidationReq> paramIn)
        {
            DataSet dsOut = new DataSet();
            String strErrMsg = "";
            string strErrCode = "";
            ApiMessage<List<ReksaInputTrxValidationRes>> ApiMsgResponse = new ApiMessage<List<ReksaInputTrxValidationRes>>();
            List<ReksaInputTrxValidationRes> dataResponse = new List<ReksaInputTrxValidationRes>();

            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            dsOut = new DataSet();


            string SpName = "ReksaInputTrxValidation";

            dsOut = new DataSet();
            try
            {
                dbPar = new List<SQLSPParameter>();
                dbPar.Add(new SQLSPParameter("@pcTranType", paramIn.Data.TranType));
                dbPar.Add(new SQLSPParameter("@pcClientCode", paramIn.Data.ClientCode));
                dbPar.Add(new SQLSPParameter("@pbTranAmountByUnit", paramIn.Data.TranAmountByUnit));
                dbPar.Add(new SQLSPParameter("@pdTranAmount", paramIn.Data.TranAmount));
                dbPar.Add(new SQLSPParameter("@pcLangCode", paramIn.Data.LangCode));
                dbPar.Add(new SQLSPParameter("@pcProviderErrCode", strErrCode, ParamDirection.OUTPUT));
                dbPar.Add(new SQLSPParameter("@pcErrMsg", strErrMsg, ParamDirection.OUTPUT));

                //20231023, Lita, RDN-1082, begin
                dbPar.Add(new SQLSPParameter("@pnProdId", paramIn.Data.ProdId));
                dbPar.Add(new SQLSPParameter("@pnCIFNo", paramIn.Data.CIFNo));
                dbPar.Add(new SQLSPParameter("@pbFullAmount", paramIn.Data.FullAmount));
                dbPar.Add(new SQLSPParameter("@pbIsRDB", paramIn.Data.IsRDB));
                dbPar.Add(new SQLSPParameter("@pcChannel", paramIn.Data.Channel == null ? "" : paramIn.Data.Channel));

                //20231023, Lita, RDN-1082, end


                if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReka2, this._ignoreSSL, SpName, ref dbPar, out dsOut, out strErrMsg))
                    throw new Exception(strErrMsg);

                //if (dsOut == null || dsOut.Tables.Count.Equals(0) || dsOut.Tables[0].Rows.Count.Equals(0))
                //    throw new Exception("Data not found !");


                strErrCode = dbPar[5].ParameterValue.ToString();
                strErrMsg = dbPar[6].ParameterValue.ToString();

                if (strErrCode != "")
                    throw new Exception(strErrMsg);

                //ApiMsgResponse.Data = dataResponse;
                #region mapping sesuai tipe data agar tidak merubah 
                dataResponse = JsonConvert.DeserializeObject<List<ReksaInputTrxValidationRes>>(
                   JsonConvert.SerializeObject(dsOut.Tables[0],
                   Newtonsoft.Json.Formatting.None,
                   new JsonSerializerSettings
                   {
                       NullValueHandling = NullValueHandling.Ignore
                   }));

                #endregion mapping sesuai tipe data agar tidak merubah

                ApiMsgResponse.Data = dataResponse;
                ApiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                ApiMsgResponse.ErrorCode = strErrCode;
                ApiMsgResponse.ErrorDescription = ex.Message;
                ApiMsgResponse.IsSuccess = false;
            }
            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return ApiMsgResponse;

        }
        #endregion ReksaInputTrxValidation
        //20230620, Lita, RDN-998, Input Transaction Validation, end

        //20241001, Andhika J, RDN-1192, Hit CMSHUB, begin
        public ActionResult<ApiMessage<bool>> GetDataReksa()
        {
            ActionResult<ApiMessage<bool>> ApiMsgResponse;
            ApiMsgResponse = _byonLogic.GetDataReksa();
            return ApiMsgResponse;
        }
        //20241001, Andhika J, RDN-1192, Hit CMSHUB, end
        //20241209, Dimas Hadianto, RDN-1204, API Simulator Amount yang hit API One SIM, begin
        public ApiMessage<ReksaSimulatorAmountRes> ReksaSimulatorAmount(ApiMessage<ReksaSimulatorAmountReq> paramIn)
        {
            ApiMessage<ReksaSimulatorAmountRes> ApiMsgResponse = new ApiMessage<ReksaSimulatorAmountRes>();
            ApiMsgResponse = _byonLogic.ReksaSimulatorAmount(paramIn);
            return ApiMsgResponse;
        }
        //20241209, Dimas Hadianto, RDN-1204, API Simulator Amount yang hit API One SIM, end

        //20241217, Dimas Hadianto, RDN-1204, API Detail Product Simulation, begin
        public ApiMessage<ReksaDetailProductSimulationRes> ReksaDetailProductSimulation(ApiMessage<ReksaDetailProductSimulationReq> paramIn)
        {
            ApiMessage<ReksaDetailProductSimulationRes> ApiMsgResponse = new ApiMessage<ReksaDetailProductSimulationRes>();
            ApiMsgResponse = _byonLogic.ReksaDetailProductSimulation(paramIn);
            return ApiMsgResponse;
        }
        //20241217, Dimas Hadianto, RDN-1204, API Detail Product Simulation, end

        //20250505, Andhika J, RDN-1236, begin
        #region ReksaParamList
        public ApiMessage<ReksaParamRes> ReksaParamList(ApiMessage<ReksaParamReq> paramIn)
        {
            DataSet dsOut = new DataSet();
            String strErrMsg = "";
            ApiMessage<ReksaParamRes> ApiMsgResponse = new ApiMessage<ReksaParamRes>();

            ReksaParamRes res = new ReksaParamRes();
            List<ReksaListRiskProfile> listRiskProfile = new List<ReksaListRiskProfile>();
            List<ReksaListJenisReksaDana> listJenisReksadana = new List<ReksaListJenisReksaDana>();
            List<ReksaListManajemenInv> listManajemenInv = new List<ReksaListManajemenInv>(); 
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();
            dsOut = new DataSet();
            try
            {
                #region Query
                string sqlCommand = @"
                    declare
                    @cLangCode varchar(2)

                    set @cLangCode = '" + paramIn.Data.LangCode + @"'

                    select RiskProfileCFMAST riskProfileCode, case when @cLangCode = 'ID' then RiskProfileDesc else RiskProfileDescEN end riskProfileDesc from ReksaDescRiskProfile_TR

                    select TypeCode jenisReksadanaCode, case when @cLangCode = 'ID' then TypeName else TypeNameEnglish end jenisReksadanaDesc from ReksaType_TR
                    
                    select distinct
                    --20250805, gio, RDN-1237, begin
                    ManInvId manajemenInvId,
                    --20250805, gio, RDN-1237, end
                    ManInvCode manajemenInvCode, ManInvName manajemenInvDesc from ReksaManInv_TR
                ";
                #endregion
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                {
                    if (dsOut.Tables[0].Rows.Count>0)
                    {
                        listRiskProfile = (from DataRow dr in dsOut.Tables[0].Rows
                                                   select new ReksaListRiskProfile()
                                                   {
                                                       riskProfileCode = dr["riskProfileCode"] != DBNull.Value ? dr["riskProfileCode"].ToString() : "",
                                                       riskProfileDesc = dr["riskProfileDesc"] != DBNull.Value ? dr["riskProfileDesc"].ToString() : ""
                                                   }).ToList();
                    }
                    if (dsOut.Tables[1].Rows.Count > 0)
                    {
                        listJenisReksadana = (from DataRow dr in dsOut.Tables[1].Rows
                                           select new ReksaListJenisReksaDana()
                                           {
                                               jenisReksadanaCode = dr["jenisReksadanaCode"] != DBNull.Value ? dr["jenisReksadanaCode"].ToString() : "",
                                               jenisReksadanaDesc = dr["jenisReksadanaDesc"] != DBNull.Value ? dr["jenisReksadanaDesc"].ToString() : ""
                                           }).ToList();
                    }
                    if (dsOut.Tables[2].Rows.Count > 0)
                    {
                        listManajemenInv = (from DataRow dr in dsOut.Tables[2].Rows
                                              select new ReksaListManajemenInv()
                                              {
                                                  //20250805, gio, RDN-1237, begin
                                                  manajemenInvId = dr["manajemenInvId"] != DBNull.Value ? Convert.ToInt32(dr["manajemenInvId"].ToString()) : 0,
                                                  //20250805, gio, RDN-1237, end
                                                  manajemenInvCode = dr["manajemenInvCode"] != DBNull.Value ? dr["manajemenInvCode"].ToString() : "",
                                                  manajemenInvDesc = dr["manajemenInvDesc"] != DBNull.Value ? dr["manajemenInvDesc"].ToString() : ""
                                              }).ToList();
                    }

                }
                else
                {
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.ErrorCode = "4002";
                    ApiMsgResponse.ErrorDescription = "Call Query to SQL failed";
                    return ApiMsgResponse;
                }
                res.listRiskProfile = listRiskProfile;
                res.listJenisReksadana = listJenisReksadana;
                res.listManajemenInv = listManajemenInv;
                ApiMsgResponse.ErrorDescription = strErrMsg;
                ApiMsgResponse.IsSuccess = true;
                ApiMsgResponse.IsResponseMessage = true;
                ApiMsgResponse.Data = res;
            }
            catch (Exception ex)
            {
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.ErrorDescription = ex.Message;
            }
            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }

            return ApiMsgResponse;
        }
        #endregion ReksaParamList
        //20250505, Andhika J, RDN-1236, end

        //20250507, gio, RDN-1237, begin
        public ApiMessage<ReksaSearchProductRes> ReksaInquirySearchProduct(ApiMessage<ReksaSearchProductReq> paramIn)
        {
            DataSet dsOut = new DataSet();
            String strErrMsg = "";
            ApiMessage<ReksaSearchProductRes> ApiMsgResponse = new ApiMessage<ReksaSearchProductRes>();
            List<ReksaSearchProductListRes> dataResponse = new List<ReksaSearchProductListRes>();
            List<SQLSPParameter> dbPar = new List<SQLSPParameter>();

            //string SpName = "ReksaProductList_Search";
            #region Query Search
            string sqlCommand = @"declare
                 @pcChannel varchar(10)    
                 , @pcProductName varchar(50)  
                 , @pcLangCode varchar(5), 
                 @cErrMsg    varchar(100)    
                  , @nOK        int    
                  , @nErrNo     int    
                  , @cDefaultURL varchar(500)  
                  , @nflagChannel bit  
                --20250428, Andhika J, RDN-1237, begin  
                  , @dLastNAVDate   datetime   
                --20250428, Andhika J, RDN-1237, end  
                --20250507, gio, RDN-1237, begin  
                  ,@counting int = 0  
                  , @totalData int = 0  
                  , @page int = 0  
                --20250507, gio, RDN-1237, end 
  
                set  @pcChannel = '" + paramIn.Data.Channel.ToString() + @"'
                set  @pcProductName = '" + paramIn.Data.Keyword.ToString() + @"'
                set  @pcLangCode = '" + paramIn.Data.LangCode.ToString() + @"'
  
                --20250428, Andhika J, RDN-1237, begin  
                select @dLastNAVDate = max(ValueDate)                              
                from dbo.ReksaNAVParam_TH     
                --20250428, Andhika J, RDN-1237, end  
  
                if (@pcChannel in ('IB','MB','VL'))  
                begin  
                 set @nflagChannel = 1  
                end  
                else  
                begin  
                 set @nflagChannel = 0  
                end  
  
                CREATE TABLE #ReksaProductList_TMP(  
                 ProductID int NULL,  
                 ProductCode varchar(10) NULL,  
                 ProductName varchar(50) NULL,  
                 ProductCategory varchar(100) NULL,  
                 --ProductCategoryEN varchar(100) NULL,  
                 ProductCurrency varchar(3) NULL,  
                 MinimumSubscriptionNew decimal(17, 2) NULL,  
                 MinimumSubscriptionAdd decimal(17, 2) NULL,  
                 SubscriptionFeePercentage decimal(6, 3) NULL,  
                 MinimumRedemption decimal(19, 4) NULL,  
                 RedemptionFeePercentage decimal(6, 3) NULL,  
                 MinimumSwitching decimal(19, 4) NULL,  
                 SwitchingFeePercentage decimal(6, 3) NULL,  
                 MinimumUnitAfterTransaction decimal(19, 4) NULL,  
                 URLProspectus varchar(255) NULL,  
                 URLFundFactsSheet varchar(255) NULL,  
                 RiskProfileProduct varchar(100) NULL,  
                 --RiskProfileProductEN varchar(100) NULL,  
                 MinimumRedemptionNom decimal(19, 4) NULL,  
                 MinimumSwitchingNom decimal(19, 4) NULL,  
                 IsVisibleInfoProduct bit NULL,  
                 bitAllowRDB bit NULL,  
                 bitRDBAllowInsurance bit NULL,  
                 InsuranceProductId int NULL,  
                 InsuranceVendor varchar(50) NULL,  
                 MinRDBSubs decimal(19, 4) NULL,  
                 RDBSubsFeePctNoIns decimal(6, 3) NULL,  
                 RDBSubsFeePctWithIns decimal(6, 3) NULL,  
                 bitRDBAllowRedeem bit NULL,  
                 bitRDBFullRedeem bit NULL,  
                 MinRDBRedeem decimal(19, 4) NULL,  
                 MinRDBRedeemNom decimal(19, 4) NULL,  
                 RDBRedeemFeePctNoIns decimal(6, 3) NULL,  
                 RDBRedeemFeePctWithIns decimal(6, 3) NULL,  
                 bitRDBAllowSwitch bit NULL,  
                 bitRDBFullSwitching bit NULL,  
                 MinRDBSwitching decimal(19, 4) NULL,  
                 MinRDBSwitchingNom decimal(19, 4) NULL,  
                 RDBSwitchFeePctNoIns decimal(6, 3) NULL,  
                 RDBSwitchFeePctWithIns decimal(6, 3) NULL  
                --20250428, Andhika J, RDN-1237, begin  
                 , YearPerformance decimal (9,3) NULL   
                 , NAVDate datetime NULL  
                 , NAV decimal (25,13) NULL  
                 , TotalAUM decimal (25,4)  
                 , TanggalAUM datetime NULL  
                 , AUM varchar (100) NULL    
                --20250428, Andhika J, RDN-1237, end  
                --20250507, gio, RDN-1237, begin  
                 , Pages int null  
                 , ManajemenInvId int null
                --20250507, gio, RDN-1237, end  
				--20251030, gio, RDN-1279, begin
				, IconMI varchar(300) null
				--20251030, gio, RDN-1279, end
                )  
  
  
                  select @cDefaultURL = isnull(DefaultURLProspectusFundFactSheet,'')       
                  from dbo.control_table      
  
                --20250507, gio, RDN-1237, begin  
                select @totalData = count(rp.ProdId) from dbo.ReksaProduct_TM rp with (nolock)     
                 join dbo.ReksaProductIBMB_TM ib with (nolock)   
                 on rp.ProdId = ib.ProdId          
                 left join dbo.ReksaProductRiskProfile_TM rprp with (nolock)     
                 on rprp.ProductCode = rp.ProdCode      
                 left join dbo.ReksaDescRiskProfile_TR drp with (nolock)      
                 on drp.RiskProfile = rprp.RiskProfile      
                 join dbo.ReksaType_TR rt with (nolock)     
                 on rt.TypeId = rp.TypeId      
                 left join dbo.ReksaUploadPDFIBMB_TM up with (nolock)   
                 on up.ProdId = rp.ProdId        
                 and up.TypePDF = 'Prospectus'      
                 left join dbo.ReksaUploadPDFIBMB_TM ff with (nolock)  
                 on ff.ProdId = rp.ProdId       
                 and ff.TypePDF = 'Fund Fact Sheet'     
                 left join ReksaInsuranceProduct_TR rip with (nolock)  
                 on ib.InsuranceProductId = rip.InsuranceProductId  
                left join ReksaInsuranceVendor_TR riv with (nolock)  
                 on rip.InsuranceVendorId = riv.InsuranceVendorId  
                left join dbo.REKSAInquiryUTProductPerformance_TR rupp with (nolock)                        
                 on rupp.ProdCode = rp.ProdCode   
                 and rupp.PeriodType = 'Y'  
                left join dbo.ReksaNAVParam_TH rvp with(nolock)  
                 on rp.ProdId = rvp.ProdId  
                left join dbo.ReksaProductCatalogue_TM rpc with(nolock)  
                 on rp.ProdCode = rpc.ProductCode  
                --20251030, gio, RDN-1279, begin  
                 left join dbo.ReksaUploadPDFIBMB_TM mi with (nolock)  
                 on mi.ProdId = rp.ProdId       
                 and mi.TypePDF = 'IconMI'     
                --20251030, gio, RDN-1279, end
                where rvp.ValueDate = @dLastNAVDate    
                and rp.Status = 1   
                and ib.IsVisibleIBMB = @nflagChannel   

                if (upper(@pcLangCode) = 'EN')
                begin
                while (@counting < @totalData)  
                begin  
                --20250507, gio, RDN-1237, end  
                  insert #ReksaProductList_TMP (ProductID, ProductCode, ProductName,      
                   ProductCategory, ProductCurrency, MinimumSubscriptionNew, MinimumSubscriptionAdd,       
                  SubscriptionFeePercentage, MinimumRedemption, RedemptionFeePercentage, MinimumSwitching,      
                  SwitchingFeePercentage, MinimumUnitAfterTransaction, URLProspectus, URLFundFactsSheet        
                  , RiskProfileProduct        
                  , MinimumRedemptionNom, MinimumSwitchingNom, IsVisibleInfoProduct    
                 , bitAllowRDB, bitRDBAllowInsurance, InsuranceProductId, InsuranceVendor  
                 , MinRDBSubs, RDBSubsFeePctNoIns, RDBSubsFeePctWithIns  
                 , bitRDBAllowRedeem, bitRDBFullRedeem, MinRDBRedeem, MinRDBRedeemNom, RDBRedeemFeePctNoIns, RDBRedeemFeePctWithIns  
                 , bitRDBAllowSwitch, bitRDBFullSwitching, MinRDBSwitching, MinRDBSwitchingNom, RDBSwitchFeePctNoIns, RDBSwitchFeePctWithIns   
                --20250428, Andhika J, RDN-1237, begin  
                 , YearPerformance  
                 , NAVDate  
                 , NAV  
                 , TotalAUM  
                 , TanggalAUM  
                 , AUM 
                --20250428, Andhika J, RDN-1237, end  
                --20250507, gio, RDN-1237, begin  
                 , Pages  
                 , ManajemenInvId
                --20250507, gio, RDN-1237, end  
				--20251030, gio, RDN-1279, begin
				, IconMI
				--20251030, gio, RDN-1279, end
                    )  
                  select distinct rp.ProdId, isnull(rp.ProdCode,''), isnull(rp.ProdName,''), isnull(rt.TypeNameEnglish,''), isnull(rp.ProdCCY,''),  
                  convert(decimal(17,2),isnull(ib.MinSubsNew,0)), convert(decimal(17,2),isnull(ib.MinSubsAdd,0)),      
                  convert(decimal(6,3),isnull(ib.PctFeeSubs,0)),       
                  case when isnull(ib.MinRedemptionByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.MinRedemption,0)) else 0 end,      
                  convert(decimal(6,3),isnull(ib.PctFeeRedemp,0)),       
                  case when isnull(ib.MinSwitchingByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.MinSwitching,0)) else 0 end,      
                  convert(decimal(6,3),isnull(ib.PctFeeSwitching,0)), convert(decimal(19,4), isnull(rp.MinBalance,0)),      
                  isnull(up.FilePath, @cDefaultURL),      
                  isnull(ff.FilePath, @cDefaultURL)     
                  , isnull(drp.RiskProfileDescEN, '')       
                  , case when isnull(ib.MinRedemptionByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.MinRedemption,0)) else 0 end,      
                  case when isnull(ib.MinSwitchingByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.MinSwitching,0)) else 0 end,  
                  isnull(ib.IsVisibleIBMB,0)      
                 , isnull(ib.CanTrxRDB,0)  
                 , case when isnull(ib.InsuranceBit, 0) = 0 then 0 else ib.InsuranceBit end  
                 , case when isnull(ib.InsuranceBit, 0) = 0 then 0 else isnull(ib.InsuranceProductId, '') end  
                 , case when isnull(ib.InsuranceBit, 0) = 0 then '' else isnull(riv.InsuranceVendorCode, '') end  
                 , isnull(ib.RDBMinSubs,0), isnull(RDBPctFeeSubsNoIns, 0), isnull(RDBPctFeeSubsWithIns, 0)  
                 , isnull(ib.CanTrxRDBRedeem,0), isnull(ib.RDBMustFullRedeem,0)  
                 , case when isnull(ib.RDBMinRedeemByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.RDBMinRedeem,0)) else 0 end    
                 , case when isnull(ib.RDBMinRedeemByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.RDBMinRedeem,0)) else 0 end  
                 , isnull(ib.RDBPctFeeRedempNoIns, 0), isnull(ib.RDBPctFeeRedempWithIns, 0)  
                 , isnull(ib.CanTrxRDBSwitch,0), isnull(ib.RDBMustFullSwitch,0)  
                 , MinRDBSwitching = case when isnull(ib.RDBMinSwitchByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.RDBMinSwitch,0)) else 0 end  
                 , MinRDBSwitchingNom =  case when isnull(ib.RDBMinSwitchByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.RDBMinSwitch,0)) else 0 end   
                 , isnull(RDBPctFeeSwitchingNoIns,0), isnull(RDBPctFeeSwitchingWithIns, 0)  
                --20250428, Andhika J, RDN-1237, begin  
                 , isnull(rupp.Percentage,0)  
                 , rvp.ValueDate  
                 , isnull(rvp.NAV,0)  
                 , isnull(rpc.TotalAUM,0)  
                 , rpc.tanggal_aum  
                 , rpc.aum_english  
                --20250428, Andhika J, RDN-1237, end  
                --20250507, gio, RDN-1237, begin  
                 , @page + 1  
                 , isnull(rp.ManInvId, 0)
                --20250507, gio, RDN-1237, end  
                --20251030, gio, RDN-1279, begin  
                  ,isnull(mi.FilePath, @cDefaultURL)     
                --20251030, gio, RDN-1237, end  
                 from dbo.ReksaProduct_TM rp with (nolock)     
                 join dbo.ReksaProductIBMB_TM ib with (nolock)   
                 on rp.ProdId = ib.ProdId          
                 left join dbo.ReksaProductRiskProfile_TM rprp with (nolock)     
                 on rprp.ProductCode = rp.ProdCode      
                 left join dbo.ReksaDescRiskProfile_TR drp with (nolock)      
                 on drp.RiskProfile = rprp.RiskProfile      
                 join dbo.ReksaType_TR rt with (nolock)     
                 on rt.TypeId = rp.TypeId      
                 left join dbo.ReksaUploadPDFIBMB_TM up with (nolock)   
                 on up.ProdId = rp.ProdId        
                 and up.TypePDF = 'Prospectus'      
                 left join dbo.ReksaUploadPDFIBMB_TM ff with (nolock)  
                 on ff.ProdId = rp.ProdId       
                 and ff.TypePDF = 'Fund Fact Sheet'     
                 left join ReksaInsuranceProduct_TR rip with (nolock)  
                 on ib.InsuranceProductId = rip.InsuranceProductId  
                left join ReksaInsuranceVendor_TR riv with (nolock)  
                 on rip.InsuranceVendorId = riv.InsuranceVendorId  
                --20250428, Andhika J, RDN-1237, begin  
                left join dbo.REKSAInquiryUTProductPerformance_TR rupp with (nolock)                        
                 on rupp.ProdCode = rp.ProdCode   
                 and rupp.PeriodType = 'Y'  
                left join dbo.ReksaNAVParam_TH rvp with(nolock)  
                 on rp.ProdId = rvp.ProdId  
                left join dbo.ReksaProductCatalogue_TM rpc with(nolock)  
                 on rp.ProdCode = rpc.ProductCode  
                --20251030, gio, RDN-1279, begin  
                 left join dbo.ReksaUploadPDFIBMB_TM mi with (nolock)  
                 on mi.ProdId = rp.ProdId       
                 and mi.TypePDF = 'IconMI'     
                --20251030, gio, RDN-1279, end
                where rvp.ValueDate = @dLastNAVDate    
                and rp.Status = 1   
                and ib.IsVisibleIBMB = @nflagChannel   
                and UPPER(rp.ProdName) like '%' + UPPER(@pcProductName) +  '%'  
                ORDER BY rp.ProdId  
                OFFSET @counting ROWS FETCH NEXT 12 ROWS ONLY  
  
                set @counting+=12  
                set @page+=1  
                end  
                end
                else if (upper(@pcLangCode) = 'ID') 
                begin

                while (@counting < @totalData)  
                begin  
                --20250507, gio, RDN-1237, end  
                  insert #ReksaProductList_TMP (ProductID, ProductCode, ProductName,      
                  ProductCategory,  ProductCurrency, MinimumSubscriptionNew, MinimumSubscriptionAdd,       
                  SubscriptionFeePercentage, MinimumRedemption, RedemptionFeePercentage, MinimumSwitching,      
                  SwitchingFeePercentage, MinimumUnitAfterTransaction, URLProspectus, URLFundFactsSheet        
                  , RiskProfileProduct   
                  , MinimumRedemptionNom, MinimumSwitchingNom, IsVisibleInfoProduct    
                 , bitAllowRDB, bitRDBAllowInsurance, InsuranceProductId, InsuranceVendor  
                 , MinRDBSubs, RDBSubsFeePctNoIns, RDBSubsFeePctWithIns  
                 , bitRDBAllowRedeem, bitRDBFullRedeem, MinRDBRedeem, MinRDBRedeemNom, RDBRedeemFeePctNoIns, RDBRedeemFeePctWithIns  
                 , bitRDBAllowSwitch, bitRDBFullSwitching, MinRDBSwitching, MinRDBSwitchingNom, RDBSwitchFeePctNoIns, RDBSwitchFeePctWithIns   
                --20250428, Andhika J, RDN-1237, begin  
                 , YearPerformance  
                 , NAVDate  
                 , NAV  
                 , TotalAUM  
                 , TanggalAUM  
                 , AUM 
                --20250428, Andhika J, RDN-1237, end  
                --20250507, gio, RDN-1237, begin  
                 , Pages  
                 , ManajemenInvId
                --20250507, gio, RDN-1237, end 
				--20251030, gio, RDN-1279, begin
				, IconMI
				--20251030, gio, RDN-1279, end 
                    )  
                  select distinct rp.ProdId, isnull(rp.ProdCode,''), isnull(rp.ProdName,''),      
                  isnull(rt.TypeName,''), isnull(rp.ProdCCY,''),  
                  convert(decimal(17,2),isnull(ib.MinSubsNew,0)), convert(decimal(17,2),isnull(ib.MinSubsAdd,0)),      
                  convert(decimal(6,3),isnull(ib.PctFeeSubs,0)),       
                  case when isnull(ib.MinRedemptionByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.MinRedemption,0)) else 0 end,      
                  convert(decimal(6,3),isnull(ib.PctFeeRedemp,0)),       
                  case when isnull(ib.MinSwitchingByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.MinSwitching,0)) else 0 end,      
                  convert(decimal(6,3),isnull(ib.PctFeeSwitching,0)), convert(decimal(19,4), isnull(rp.MinBalance,0)),      
                  isnull(up.FilePath, @cDefaultURL),      
                  isnull(ff.FilePath, @cDefaultURL)      
                  , isnull(drp.RiskProfileDesc, '')     
                  , case when isnull(ib.MinRedemptionByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.MinRedemption,0)) else 0 end,      
                  case when isnull(ib.MinSwitchingByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.MinSwitching,0)) else 0 end,  
                  isnull(ib.IsVisibleIBMB,0)      
                 , isnull(ib.CanTrxRDB,0)  
                 , case when isnull(ib.InsuranceBit, 0) = 0 then 0 else ib.InsuranceBit end  
                 , case when isnull(ib.InsuranceBit, 0) = 0 then 0 else isnull(ib.InsuranceProductId, '') end  
                 , case when isnull(ib.InsuranceBit, 0) = 0 then '' else isnull(riv.InsuranceVendorCode, '') end  
                 , isnull(ib.RDBMinSubs,0), isnull(RDBPctFeeSubsNoIns, 0), isnull(RDBPctFeeSubsWithIns, 0)  
                 , isnull(ib.CanTrxRDBRedeem,0), isnull(ib.RDBMustFullRedeem,0)  
                 , case when isnull(ib.RDBMinRedeemByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.RDBMinRedeem,0)) else 0 end    
                 , case when isnull(ib.RDBMinRedeemByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.RDBMinRedeem,0)) else 0 end  
                 , isnull(ib.RDBPctFeeRedempNoIns, 0), isnull(ib.RDBPctFeeRedempWithIns, 0)  
                 , isnull(ib.CanTrxRDBSwitch,0), isnull(ib.RDBMustFullSwitch,0)  
                 , MinRDBSwitching = case when isnull(ib.RDBMinSwitchByUnit,0) = 1 then convert(decimal(19,4),isnull(ib.RDBMinSwitch,0)) else 0 end  
                 , MinRDBSwitchingNom =  case when isnull(ib.RDBMinSwitchByUnit,0) = 0 then convert(decimal(19,4),isnull(ib.RDBMinSwitch,0)) else 0 end   
                 , isnull(RDBPctFeeSwitchingNoIns,0), isnull(RDBPctFeeSwitchingWithIns, 0)  
                --20250428, Andhika J, RDN-1237, begin  
                 , isnull(rupp.Percentage,0)  
                 , rvp.ValueDate  
                 , isnull(rvp.NAV,0)  
                 , isnull(rpc.TotalAUM,0)  
                 , rpc.tanggal_aum  
                 , rpc.aum_bahasa  
                --20250428, Andhika J, RDN-1237, end  
                --20250507, gio, RDN-1237, begin  
                 , @page + 1  
                 , isnull(rp.ManInvId, 0)
                --20250507, gio, RDN-1237, begin  
                --20251030, gio, RDN-1279, begin  
                  ,isnull(mi.FilePath, @cDefaultURL)     
                --20251030, gio, RDN-1237, end   
                 from dbo.ReksaProduct_TM rp with (nolock)     
                 join dbo.ReksaProductIBMB_TM ib with (nolock)   
                 on rp.ProdId = ib.ProdId          
                 left join dbo.ReksaProductRiskProfile_TM rprp with (nolock)     
                 on rprp.ProductCode = rp.ProdCode      
                 left join dbo.ReksaDescRiskProfile_TR drp with (nolock)      
                 on drp.RiskProfile = rprp.RiskProfile      
                 join dbo.ReksaType_TR rt with (nolock)     
                 on rt.TypeId = rp.TypeId      
                 left join dbo.ReksaUploadPDFIBMB_TM up with (nolock)   
                 on up.ProdId = rp.ProdId        
                 and up.TypePDF = 'Prospectus'      
                 left join dbo.ReksaUploadPDFIBMB_TM ff with (nolock)  
                 on ff.ProdId = rp.ProdId       
                 and ff.TypePDF = 'Fund Fact Sheet'     
                 left join ReksaInsuranceProduct_TR rip with (nolock)  
                 on ib.InsuranceProductId = rip.InsuranceProductId  
                left join ReksaInsuranceVendor_TR riv with (nolock)  
                 on rip.InsuranceVendorId = riv.InsuranceVendorId  
                --20250428, Andhika J, RDN-1237, begin  
                left join dbo.REKSAInquiryUTProductPerformance_TR rupp with (nolock)                        
                 on rupp.ProdCode = rp.ProdCode   
                 and rupp.PeriodType = 'Y'  
                left join dbo.ReksaNAVParam_TH rvp with(nolock)  
                 on rp.ProdId = rvp.ProdId  
                left join dbo.ReksaProductCatalogue_TM rpc with(nolock)  
                 on rp.ProdCode = rpc.ProductCode   
                --20251030, gio, RDN-1279, begin  
                 left join dbo.ReksaUploadPDFIBMB_TM mi with (nolock)  
                 on mi.ProdId = rp.ProdId       
                 and mi.TypePDF = 'IconMI'     
                --20251030, gio, RDN-1279, end
                where rvp.ValueDate = @dLastNAVDate    
                and rp.Status = 1   
                and ib.IsVisibleIBMB = @nflagChannel   
                and UPPER(rp.ProdName) like '%' + UPPER(@pcProductName) +  '%'  
                ORDER BY rp.ProdId  
                OFFSET @counting ROWS FETCH NEXT 12 ROWS ONLY  
  
                set @counting+=12  
                set @page+=1  
                end  
                end
                else
                begin
                raiserror('LangCode tidak sesuai! LangCode harus terisi EN/ID', 16, 1)
                end
  
                 select ProductID, ProductCode, ProductName,      
                  ProductCategory,  ProductCurrency, MinimumSubscriptionNew, MinimumSubscriptionAdd,       
                  SubscriptionFeePercentage, MinimumRedemption, RedemptionFeePercentage, MinimumSwitching,      
                  SwitchingFeePercentage, MinimumUnitAfterTransaction, URLProspectus, URLFundFactsSheet        
                  , RiskProfileProduct        
                  , MinimumRedemptionNom, MinimumSwitchingNom, IsVisibleInfoProduct    
                 , bitAllowRDB, bitRDBAllowInsurance, InsuranceProductId, InsuranceVendor  
                 , MinRDBSubs, RDBSubsFeePctNoIns, RDBSubsFeePctWithIns  
                 , bitRDBAllowRedeem, bitRDBFullRedeem, MinRDBRedeem, MinRDBRedeemNom, RDBRedeemFeePctNoIns, RDBRedeemFeePctWithIns  
                 , bitRDBAllowSwitch, bitRDBFullSwitching, MinRDBSwitching, MinRDBSwitchingNom, RDBSwitchFeePctNoIns, RDBSwitchFeePctWithIns   
                 , YearPerformance  
                 , NAVDate  
                 , NAV  
                 , TotalAUM  
                 , case when TanggalAUM = '1900-01-01' then '' else TanggalAUM end TanggalAUM  
                 , AUM
                 --20250507, gio, RDN-1237, begin  
                 , Pages Page  
                 , ManajemenInvId
                 --20250507, gio, RDN-1237, end   
                --20251030, gio, RDN-1279, begin  
                  ,IconMI    
                --20251030, gio, RDN-1237, end  
                 from #ReksaProductList_TMP  
  
                 drop table #ReksaProductList_TMP  
  
  
            ";
            #endregion

            dsOut = new DataSet();
            try
            {
                //dbPar = new List<SQLSPParameter>();
                //dbPar.Add(new SQLQueryParameter("@pcChannel", paramIn.Data.Channel));
                //dbPar.Add(new SQLQueryParameter("@pcProductName", paramIn.Data.Keyword));
                //dbPar.Add(new SQLQueryParameter("@pcLangCode", paramIn.Data.LangCode));


                //if (!clsCallSPWs.CallSPFromWs(this._strUrlWsReka2, this._ignoreSSL, SpName, ref dbPar, out dsOut, out strErrMsg))

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReka2, this._ignoreSSL, sqlCommand, out dsOut, out strErrMsg))
                        throw new Exception(strErrMsg);

                //if (dsOut == null || dsOut.Tables.Count.Equals(0) || dsOut.Tables[0].Rows.Count.Equals(0))
                //    throw new Exception("Data not found !");


                //ApiMsgResponse.Data = dataResponse;
                #region mapping sesuai tipe data agar tidak merubah 
                dataResponse = JsonConvert.DeserializeObject<List<ReksaSearchProductListRes>>(
                    JsonConvert.SerializeObject(dsOut.Tables[0],
                    Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings
                    { 
                        NullValueHandling = NullValueHandling.Ignore
                    }));

                #endregion mapping sesuai tipe data agar tidak merubah

                ApiMsgResponse.Data = new ReksaSearchProductRes();
                ApiMsgResponse.Data.TotalProductAkumulasi = dataResponse.Count();
                //ApiMsgResponse.Data.ReksaProductList = new List<ReksaProductListRes>();
                ApiMsgResponse.Data.ReksaProductList = dataResponse;
                ApiMsgResponse.IsSuccess = true;
            }
            catch (Exception ex)
            {
                ApiMsgResponse.ErrorDescription = ex.Message;
                ApiMsgResponse.IsSuccess = false;
            }
            finally
            {
                ApiMsgResponse.MessageDateTime = DateTime.Now;
            }
            return ApiMsgResponse;
        }
        //20250507, gio, RDN-1237, end
    }

}