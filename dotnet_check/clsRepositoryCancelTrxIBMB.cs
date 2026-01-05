using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NISPDataSourceNetCore.logger;
using NISPDataSourceNetCore.webservice.model;
using wealth_transaction_be.Services;
using wealth_transaction_be.Models.MigrasiMiddleware;

namespace wealth_transaction_be.Services
{
    public class clsRepositoryCancelTrxIBMB
    {
        private readonly IConfiguration _iconfiguration;
        private readonly IApiLogger _iApiLogger;
        private readonly GlobalVariabelList _globalVariabelList;
        private readonly string _urlGwEDE;
        private readonly string _urlCoreEDE;
        private readonly string _strUrlMBASE;
        private readonly bool _ignoreSSL;
        private string _ConnectionStringDBReksa = "";
        private string _strUrlWsReksa;
        private string _strUrlWsReksaDev;
        public clsRepositoryCancelTrxIBMB(IConfiguration iconfiguration, GlobalVariabelList globalVariabelList)
        {
            _globalVariabelList = globalVariabelList;
            _ConnectionStringDBReksa = globalVariabelList.ConnectionStringDBReksa;
            this._strUrlWsReksa = globalVariabelList.URLWsReksa2;
            this._strUrlWsReksaDev = globalVariabelList.URLWsReksa2;
        }
        public void InquiryIDTrxIBMB(int nId, out string IsExist)
        {
            string cQuery = "", ErrData = "";
            IsExist = "";
            DataSet dsData = new DataSet();
            try
            {
                cQuery = @"
                declare @nId int 

                set @nId = " + nId + @"

                if not exists(  
                select top 1 1 from dbo.ReksaCancelTrxIBMB_TT  
                where CancelId = @nId  )  
                begin  
                	select 'N' as ID 
                end   
                else
                begin
                    select 'Y' as ID
                end ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, cQuery, out dsData, out ErrData))
                    throw new Exception(ErrData);
                if (!ErrData.EndsWith(""))
                    throw new Exception(ErrData);

                if (dsData.Tables[0].Rows.Count > 0)
                {
                    IsExist = dsData.Tables[0].Rows[0]["ID"].ToString();
                }
            }
            catch (Exception ex)
            {
                IsExist = "N";
            }
        }
        public bool UpdateStatusTrx(int nId, int cNik, int StatusOtor)
        {
            bool bReturn = false;
            string cQuery = "", ErrData = "";
            DataSet dsData = new DataSet();
            try
            {
                cQuery = @"
                declare @nId    int 
                , @cNik         int
                , @nStatusOtor  int
            
                set @nId = " + nId + @"
                set @cNik = " + cNik + @"
                set @nStatusOtor = " + StatusOtor + @"

                update dbo.ReksaCancelTrxIBMB_TT
                set StatusOtor = @nStatusOtor,
                    UserAuth = @cNik,
                    DateAuth = getdate()
                where CancelId = @nId ";

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, cQuery, out dsData, out ErrData))
                    throw new Exception(ErrData);
                if (!ErrData.EndsWith(""))
                    throw new Exception(ErrData);

                bReturn = true;
            }
            catch (Exception ex)
            {
                bReturn = false;
                ErrData = ex.Message;
            }
            return bReturn;
        }
        public bool GetDataTrxCancelIBMB(int nId, out DataSet dsDataTrxCancelIBMB)
        {
            bool bReturn = false;
            string cQuery = "", ErrData = "";
            DataSet dsData = new DataSet();
            dsDataTrxCancelIBMB = dsData;
            try
            {
                cQuery = @"
                declare @nId    int 
            
                set @nId = " + nId + @"

                select TranType
	            , TranId
	            , TranCode
	            , NAVValueDate
                from dbo.ReksaCancelTrxIBMB_TT
                where CancelId = @nId
                and StatusOtor = 0 ";

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, cQuery, out dsData, out ErrData))
                    throw new Exception(ErrData);
                if (!ErrData.EndsWith(""))
                    throw new Exception(ErrData);


                dsDataTrxCancelIBMB = dsData;

                bReturn = true;
            }
            catch (Exception ex)
            {
                bReturn = false;
                ErrData = ex.Message;
            }
            return bReturn;
        }
        public bool UpdateStatusBill(int nTranId, int cNik, DateTime dNAVValueDate, int nTranType)
        {
            bool bReturn = false;
            string cQuery = "", ErrData = "";
            DataSet dsData = new DataSet();
            try
            {
                cQuery = @"
                declare 
                @nTranId			int 
                , @nBillId			int
                , @nSettled			int
                , @cNik				int
                , @cErrMsg			varchar(100)
                , @nCurrentDate		datetime
                , @dNAVValueDate	datetime
                , @nTranUnit		decimal(25,13)
                , @nClientId		int
                , @bIsRDB			bit
                , @nRegSubsFlag		int
                , @nTranType		int
                , @cTranCode		char(20)
				, @nTranUnitOut		decimal(25,13)
				, @nTranUnitIn		decimal(25,13)
				, @nClientIdOut		int
				, @nClientIdIn		int
                
                
                set @nTranId			= " + nTranId + @"
                set @cNik				= " + cNik + @"
                set @dNAVValueDate		= '" + dNAVValueDate + @"'
                set @nTranType			= " + nTranType + @"
                
                select @nCurrentDate = current_working_date 
                from control_table
                
                create table #TempBill(
                    BillId      int
                    , TotalBill money
                    , Fee       money
                    , FeeBased  money
                    , TaxFeeBased money
                    , FeeBased3 money
                    , FeeBased4 money
                    , FeeBased5 money
                )
                
                BEGIN TRY 
                if @nTranType in (1,2,3,4,8)
                begin   
                    if exists(select top 1 1 from dbo.ReksaTransaction_TT where TranId = @nTranId)
                    begin 	
                    	select @nBillId = isnull(BillId, 0), @nSettled = isnull(Settled, 0)
                    	from ReksaTransaction_TT
                    	where TranId = @nTranId  
                    end
                    else
                    if exists(select top 1 1 from dbo.ReksaTransaction_TH where TranId = @nTranId)
                    begin 	
                    	select @nBillId = isnull(BillId, 0), @nSettled = isnull(Settled, 0)
                    	from ReksaTransaction_TH
                    	where TranId = @nTranId  
                    end
                    
                    BEGIN TRAN
                    update dbo.ReksaTransaction_TT 
                    set Status = 4
                        ,ExtStatus = 3
                        , CancelSuid= @cNik
                        , CancelDate= getdate() 
                    where TranId= @nTranId  
                    
                    If @@error != 0
                    begin  
                    	set @cErrMsg = 'Error update status'     
                        raiserror(@cErrMsg, 16, 1) 
                    end  
                    
                    update dbo.ReksaTransaction_TH 
                    set Status = 4
                        ,ExtStatus = 3
                        , CancelSuid= @cNik
                        , CancelDate= getdate() 
                    where TranId= @nTranId  
                    
                    If @@error != 0
                    begin  
                    	set @cErrMsg = 'Error update status'     
                        raiserror(@cErrMsg, 16, 1) 
                    end
                    
                    if convert(varchar(8), @nCurrentDate, 112) <> convert(varchar(8), @dNAVValueDate, 112)
                    begin
                    	select @nTranUnit = TranUnit, @nClientId = ClientId from ReksaTransaction_v
                    	where TranId= @nTranId
                    	
                    	if @nTranType in (1,2,8)
                    	begin
                    		update ReksaCIFData_TM
                    		set UnitBalance = round(UnitBalance - @nTranUnit, 4),
                    			UnitNominal = round((UnitBalance - @nTranUnit) * NAV, 2)
                    		where ClientId = @nClientId
                    	end
                    	else
                    	if @nTranType in (3,4)
                    	begin
                    		update ReksaCIFData_TM
                    		set UnitBalance = round(UnitBalance + @nTranUnit, 4),
                    			UnitNominal = round((UnitBalance + @nTranUnit) * NAV, 2)
                    		where ClientId = @nClientId
                    	end  
                    end
                    if @nTranType in (1,8)
                    begin
                    
                    	select @nClientId = ClientId from ReksaTransaction_v where TranId = @nTranId
                    
                        set @bIsRDB = 0
                    
                        if exists (select top 1 1 from ReksaRegulerSubscriptionClient_TM where ClientId = @nClientId)
                        begin
                            set @bIsRDB = 1
                    		select @nRegSubsFlag = RegSubscriptionFlag from ReksaTransaction_v where TranId = @nTranId
                        end
                    
                        if ((@nTranType = 1) or (@bIsRDB = 1 and @nRegSubsFlag = 1)) -- untuk yg SubsNew & RDB yang baru pertama kali 
                        begin
                            update dbo.ReksaCIFData_TM
                            set CIFStatus = 'T'
                                , LastUpdate = getdate()
                                , CIFNIK = @cNik
                            where ClientId = @nClientId

                            --20231229, Lita, RDN-1097, update rdb master, begin
                            update dbo.ReksaRegulerSubscriptionClient_TM  
                            set ReverseSuid = @cNik, Status = 4
                            where TranId = @nTranId

                            update dbo.ReksaRegulerSubscriptionSchedule_TT  
                            set StatusId = 2, ErrorDescription = 'Transaksi dibatalkan' 
                            where ClientId = @nClientId 
                            --20231229, Lita, RDN-1097, update rdb master, end
                        end
                    
                        If @@error != 0
                        begin  
                            set @cErrMsg = 'Error update status CIF Data' 
                    		raiserror(@cErrMsg, 16, 1) 
                        end
                    end
                    If @nTranType in (1,2,8)
                    Begin
                    	insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5)
                    	select isnull(BillId,0)
                    		, sum(case when FullAmount = 1 then case when TranCCY = 'IDR' then round(TranAmt,2)  
                    							else round(TranAmt,2)  
                    							end  
                    					else case when TranCCY = 'IDR' then round(TranAmt, 2) - round(SubcFee, 2)  
                    						else round(TranAmt, 2) - round(SubcFee, 2)  
                    					end  
                    				end)  
                    			, case when TranCCY = 'IDR' then sum(round(SubcFee, 2)) else sum(round(SubcFee, 2)) end  
                    			, case when TranCCY = 'IDR' then sum(round(SubcFeeBased, 2)) else sum(round(SubcFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(TaxFeeBased, 2)) else sum(round(TaxFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased3, 2)) else sum(round(FeeBased3, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased4, 2)) else sum(round(FeeBased4, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased5, 2)) else sum(round(FeeBased5, 2)) end 
                    	from ReksaTransaction_TT with(nolock)
                    	where BillId = @nBillId
                    		and Status = 1
                    		and TranType in (1,2,8)
                    	group by BillId, TranCCY
                    	union all
                    	select isnull(BillId,0)
                    		, sum(case when FullAmount = 1 then case when TranCCY = 'IDR' then round(TranAmt,2)  
                    							else round(TranAmt,2)  
                    							end  
                    					else case when TranCCY = 'IDR' then round(TranAmt, 2) - round(SubcFee, 2)  
                    						else round(TranAmt, 2) - round(SubcFee, 2)  
                    					end  
                    				end)  
                    			, case when TranCCY = 'IDR' then sum(round(SubcFee, 2)) else sum(round(SubcFee, 2)) end  
                    			, case when TranCCY = 'IDR' then sum(round(SubcFeeBased, 2)) else sum(round(SubcFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(TaxFeeBased, 2)) else sum(round(TaxFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased3, 2)) else sum(round(FeeBased3, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased4, 2)) else sum(round(FeeBased4, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased5, 2)) else sum(round(FeeBased5, 2)) end 
                    	from ReksaTransaction_TH with(nolock) 
                    	where BillId = @nBillId
                    		and Status = 1
                    		and TranType in (1,2,8)
                    	group by BillId, TranCCY
                    
                    	if @@error != 0
                    	Begin
                    		set @cErrMsg = 'Error Prepare data Bill (1)'
                    		raiserror(@cErrMsg, 16, 1) 
                    	End
                    End
                    
                    If @nTranType in (3,4)
                    Begin
                    	insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5)
                    	select isnull(BillId,0)
                    		,sum(case   
                    				when TranCCY = 'IDR' then round(TranAmt, 2) - round(RedempFee, 2)  
                    				else round(TranAmt, 2) - round(RedempFee, 2)  
                    				end) 
                    			, case when TranCCY = 'IDR' then sum(round(RedempFee, 2)) else sum(round(RedempFee, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(RedempFeeBased, 2)) else sum(round(RedempFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(TaxFeeBased, 2)) else sum(round(TaxFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased3, 2)) else sum(round(FeeBased3, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased4, 2)) else sum(round(FeeBased4, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased5, 2)) else sum(round(FeeBased5, 2)) end 
                    	from ReksaTransaction_TT with(nolock)
                    	where BillId = @nBillId
                    		and Status = 1
                    		and TranType in (3,4)
                    	group by BillId, TranCCY
                    	union all
                    	select isnull(BillId,0)
                    		,sum(case   
                    				when TranCCY = 'IDR' then round(TranAmt, 2) - round(RedempFee, 2)  
                    				else round(TranAmt, 2) - round(RedempFee, 2)  
                    				end) 
                    			, case when TranCCY = 'IDR' then sum(round(RedempFee, 2)) else sum(round(RedempFee, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(RedempFeeBased, 2)) else sum(round(RedempFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(TaxFeeBased, 2)) else sum(round(TaxFeeBased, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased3, 2)) else sum(round(FeeBased3, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased4, 2)) else sum(round(FeeBased4, 2)) end 
                    			, case when TranCCY = 'IDR' then sum(round(FeeBased5, 2)) else sum(round(FeeBased5, 2)) end 
                    	from ReksaTransaction_TH with(nolock) 
                    	where BillId = @nBillId
                    		and Status = 1
                    		and TranType in (3,4)
                    	group by BillId, TranCCY
                    
                    	if @@error != 0
                    	Begin
                    		set @cErrMsg = 'Error Prepare data Bill (2)'
                    		raiserror(@cErrMsg, 16, 1) 
                    	End
                    End
                    
                    	-- jika bill kosong, isi dengan 0
                    if not exists(select top 1 1 from #TempBill where BillId = @nBillId)
                    Begin
                    	insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5)
                    	select @nBillId, 0, 0, 0
                    	, 0, 0, 0, 0
                    End
                    
                    if isnull(@nSettled, 0) = 0
                    Begin
                    	update a 
                    	set TotalBill   = b.TotalBill
                    		, Fee       = b.Fee
                    		, FeeBased  = b.FeeBased
                    		, TaxFeeBased = b.TaxFeeBased
                    		, FeeBased3 = b.FeeBased3
                    		, FeeBased4 = b.FeeBased4
                    		, FeeBased5 = b.FeeBased5
                    	from ReksaBill_TM a join #TempBill b
                    		on a.BillId = b.BillId
                    
                    	if @@error != 0
                    	Begin
                    		set @cErrMsg = 'Error Update data Bill (1)'
                    		raiserror(@cErrMsg, 16, 1) 
                    	End
                    
                    	update dbo.ReksaTransaction_TT 
                    	set BillId = NULL 
                    	where TranId= @nTranId 
                    
                    	if @@error != 0
                    	Begin
                    		set @cErrMsg = 'Error Update BillId (1)'
                    		raiserror(@cErrMsg, 16, 1) 
                    	End
                    
                    	update dbo.ReksaTransaction_TH 
                    	set BillId = NULL 
                    	where TranId= @nTranId 
                    
                    	if @@error != 0
                    	Begin
                    		set @cErrMsg = 'Error Update BillId (2)'
                    		raiserror(@cErrMsg, 16, 1) 
                    	End
                    end
                end
                else if @nTranType in (5,6)
                begin
                     --20241024, sandi, RDN-1193, begin
                	 --select @nBillId = isnull(BillId, 0), @nSettled = isnull(Settled, 0)
                     select @nBillId = isnull(BillId, 0), @nSettled = isnull(Settled, 0), @cTranCode = isnull(TranCode, '')
		             --20241024, sandi, RDN-1193, end
                     from ReksaSwitchingTransaction_TM
		             where TranId = @nTranId  

                     update dbo.ReksaSwitchingTransaction_TM 
                     set Status = 4
                     where TranId= @nTranId  

                     If @@error != 0
                     begin  
		             	set @cErrMsg = 'Error update status (2)'    
		             	raiserror(@cErrMsg, 16, 1) 
                     end  

		             update dbo.ReksaTransaction_TT 
                     set Status = 4
                         ,ExtStatus = 3
                         , CancelSuid= @cNik
                         , CancelDate= getdate() 
                     where TranCode = @cTranCode
		             	and NAVValueDate = @dNAVValueDate 

                     If @@error != 0
                     begin  
		             	set @cErrMsg = 'Error update status (3)'    
		             	raiserror(@cErrMsg, 16, 1) 
                     end  

		             update dbo.ReksaTransaction_TH 
                     set Status = 4
                         ,ExtStatus = 3
                         , CancelSuid= @cNik
                         , CancelDate= getdate() 
                     where TranCode = @cTranCode
		             and NAVValueDate = @dNAVValueDate  

                     If @@error != 0
                     begin  
		             	set @cErrMsg = 'Error update status (4)' 
		             	raiserror(@cErrMsg, 16, 1) 
                     end

		             if convert(varchar(8), @nCurrentDate, 112) <> convert(varchar(8), @dNAVValueDate, 112)
		             begin
		             	select @nTranUnitOut = TranUnit, @nClientIdOut = ClientId from ReksaTransaction_v
		             	where TranCode = @cTranCode
		             		and NAVValueDate = @dNAVValueDate
		             		and TranType in (3,4)
		             		and isnull(ExtStatus, 0) = 3

		             	select @nTranUnitIn = TranUnit, @nClientIdIn = ClientId from ReksaTransaction_v
		             	where TranCode = @cTranCode
		             		and NAVValueDate = @dNAVValueDate
		             		and TranType in (1,2)
		             		and isnull(ExtStatus, 0) = 3
		             			
		             	update ReksaCIFData_TM
		             	set UnitBalance = round(UnitBalance - @nTranUnitIn, 4),
		             		UnitNominal = round((UnitBalance - @nTranUnitIn) * NAV, 2)
		             	where ClientId = @nClientIdIn
		             	
		             	update ReksaCIFData_TM
		             	set UnitBalance = round(UnitBalance + @nTranUnitOut, 4),
		             		UnitNominal = round((UnitBalance + @nTranUnitOut) * NAV, 2)
		             	where ClientId = @nClientIdOut  
		             end

		             insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5)
		             select isnull(BillId,0),
		             	case when TranCCY = 'IDR' then sum(round(ActualSwitchingFee, 2)) else sum(round(ActualSwitchingFee, 2)) end
		             	, 0  
		             	, case when TranCCY = 'IDR' then sum(round(SwitchingFeeBased, 2)) else sum(round(SwitchingFeeBased, 2)) end
		             	, case when TranCCY = 'IDR' then sum(round(TaxFeeBased, 2)) else sum(round(TaxFeeBased, 2)) end
		             	, case when TranCCY = 'IDR' then sum(round(FeeBased3, 2)) else sum(round(FeeBased3, 2)) end
		             	, case when TranCCY = 'IDR' then sum(round(FeeBased4, 2)) else sum(round(FeeBased4, 2)) end
		             	, case when TranCCY = 'IDR' then sum(round(FeeBased5, 2)) else sum(round(FeeBased5, 2)) end
		             from ReksaSwitchingTransaction_TM 
		             where BillId = @nBillId
		             	and Status = 1
		             group by BillId, TranCCY

		             if @@error != 0
		             Begin
		             	set @cErrMsg = 'Error Prepare data Bill (2)'  
		             	raiserror(@cErrMsg, 16, 1) 
		             End

		             -- jika bill kosong, isi dengan 0
		             if not exists(select top 1 1 from #TempBill where BillId = @nBillId)
		             Begin
		             	insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5)
		             	select @nBillId, 0, 0, 0
		             	, 0, 0, 0, 0
		             End

		             if isnull(@nSettled, 0) = 0
		             Begin
		             	update a 
		             	set TotalBill   = b.TotalBill
		             		, Fee       = b.Fee
		             		, FeeBased  = b.FeeBased
		             		, TaxFeeBased = b.TaxFeeBased
		             		, FeeBased3 = b.FeeBased3
		             		, FeeBased4 = b.FeeBased4
		             		, FeeBased5 = b.FeeBased5
		             	from ReksaBill_TM a join #TempBill b
		             		on a.BillId = b.BillId

		             	if @@error != 0
		             	Begin
		             		set @cErrMsg = 'Error Update data Bill (2)'  
		             		raiserror(@cErrMsg, 16, 1) 
		             	End

		             	update dbo.ReksaSwitchingTransaction_TM 
		             	set BillId = NULL 
		             	where TranId= @nTranId 

		             	if @@error != 0
		             	Begin
		             		set @cErrMsg = 'Error Update BillId (2)'  
		             		raiserror(@cErrMsg, 16, 1) 
		             	End
		             end     
                end
                COMMIT TRAN
                
                select isnull(@cErrMsg,'') cErrMsg
                
                END TRY 
                
                BEGIN CATCH 
                if @@trancount > 0 
                    ROLLBACK TRAN 
                	select isnull(@cErrMsg,'') cErrMsg
                END CATCH ";

                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, cQuery, out dsData, out ErrData))
                    throw new Exception(ErrData);
                if (!ErrData.EndsWith(""))
                    throw new Exception(ErrData);

                bReturn = true;
            }
            catch (Exception ex)
            {
                bReturn = false;
                ErrData = ex.Message;
            }
            return bReturn;
        }

    }
}