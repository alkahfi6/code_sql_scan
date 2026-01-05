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
using wealth_transaction_be.Models;
using wealth_transaction_be.Models.Settlement;
using wealth_transaction_be.Models.MigrasiMiddleware;

namespace wealth_transaction_be.Services
{
    public class clsRepositoryDeleteBooking
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
        public clsRepositoryDeleteBooking(IConfiguration iconfiguration, GlobalVariabelList globalVariabelList)
        {
            _globalVariabelList = globalVariabelList;
            _ConnectionStringDBReksa = globalVariabelList.ConnectionStringDBReksa;
            this._strUrlWsReksa = globalVariabelList.URLWsReksa;
            this._strUrlWsReksaDev = globalVariabelList.URLWsReksa2;
        }
        public void PopulateTransactionData(int TranId, out ApiMessage<ReksaDeleteBookingRs> PopulateTransactionDataRs)
        {
            List<ReksaDeleteBookingRs> listresponse = new List<ReksaDeleteBookingRs>();
            PopulateTransactionDataRs = new ApiMessage<ReksaDeleteBookingRs>();
            DataSet dsData = new DataSet();
            string ErrMsg = "", cErrMsg = "";
            try
            {
                string sql = @"
                    declare 
                    @nTranId               int    
                    , @nSettled				int  
                    , @nBillId				int
                    , @nTranType			tinyint
                    , @nStatus				tinyint
                    , @cErrMsg				varchar(100)
                    
                    set @nTranId = " + TranId + @"
                    BEGIN TRY 
                    if exists (select top 1 1 from  ReksaTransaction_TT where TranId = @nTranId)
                    begin
                    	select @nSettled = Settled, @nBillId  = BillId
                    	, @nTranType = TranType, @nStatus = Status
                    	from ReksaTransaction_TT  
                    	where TranId = @nTranId  
                    
                    	If @nStatus = 4
                    	begin  
                    		set @cErrMsg = 'Sudah dicancel'  
                    		raiserror(@cErrMsg,16,1) 
                    	end  
                    end
                    else 
                    begin
                    	set @cErrMsg = 'Transaksi tidak ditemukan' 
                        raiserror(@cErrMsg,16,1) 
                    end
                    select @nTranId TranId, @nSettled Settled, isnull(@nBillId,0) BillId
                    	, isnull(@nTranType,0) TranType, @nStatus Status, @cErrMsg ErrMsg
                    END TRY
                    BEGIN CATCH
                        select @cErrMsg ErrMsg
                    END CATCH
                    ";
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksaDev, this._ignoreSSL, sql, out dsData, out ErrMsg))
                {
                    if (dsData.Tables[0].Rows.Count > 0)
                    {
                        cErrMsg = dsData.Tables[0].Rows[0]["ErrMsg"].ToString();
                        if (cErrMsg != "")
                        {
                            throw new Exception(cErrMsg);
                        }
                        else
                        {
                            listresponse = JsonConvert.DeserializeObject<List<ReksaDeleteBookingRs>>(
                                  JsonConvert.SerializeObject(dsData.Tables[0],
                                          Newtonsoft.Json.Formatting.None,
                                          new JsonSerializerSettings
                                          {
                                              NullValueHandling = NullValueHandling.Ignore
                                          }));
                            PopulateTransactionDataRs.Data = listresponse[0];
                        }
                    }
                    else
                    {
                        throw new Exception(ErrMsg);
                    }
                }
                else
                {
                    throw new Exception(ErrMsg);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public bool UpdateDataCIF(int pnTranId)
        {
            bool bResult = false;
            DataSet dsData = new DataSet();
            string ErrMsg = "";
            try
            {
                string sql = @"
                        declare 
                        @pnTranId int

                        set @pnTranId = "+ pnTranId +@"
                        update dbo.ReksaCIFData_TM  
                        set CIFStatus = 'T'  
                        from dbo.ReksaCIFData_TM rc  
                        join dbo.ReksaTransaction_TT rt  
                        on rc.ClientId = rt.ClientId  
                        where rt.TranId = @pnTranId  
                        
                        update dbo.ReksaDeleteTrans_TMP
                        set StatusApproval = 1
                        where TranId = @pnTranId and StatusApproval = 0
                        
                ";
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksaDev, this._ignoreSSL, sql, out dsData, out ErrMsg))
                {
                    bResult = true;
                }
                else
                {
                    bResult = false;
                    throw new Exception(ErrMsg);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return bResult;

        }
        public bool UpdateTempBill(int TranId, int TranType, int BillId, int SettlId, int NIK)
        {
            bool bResult = false;
            DataSet dsData = new DataSet();
            string ErrMsg = "";
            try
            {
                string sql = @"
                        declare 
                        @nTranId           int       
                        , @nTranType       int
                        , @nBillId         int
                        , @cErrMsg         varchar(500)
                        , @nSettled        int
                        , @nNik            int

                        set @nTranId = " + TranId+ @"
                        set @nTranType = " + TranType+ @"
                        set @nBillId = " + BillId+ @"
                        set @nSettled = " + SettlId + @"
                        set @nNik = " + NIK+ @"

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
                        If @nTranType in (1,2)
                        Begin
	                    	if exists (select top 1 1 from ReksaTransaction_TT 
	                    		where BillId = @nBillId
                                and Status = 1
                                and TranType in (1,2))
	                    	begin
	                    		insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5
	                    		)
	                    		select isnull(BillId,0)
	                    		, sum(case when FullAmount = 1 then case when TranCCY = 'IDR' then round(TranAmt,0)  
	                    			else round(TranAmt,2)  
	                    			end  
	                    			else case when TranCCY = 'IDR' then round(TranAmt, 0) - round(SubcFee, 0)  
	                    				else round(TranAmt, 2) - round(SubcFee, 2)  
	                    		    end  
	                    		   end)  
	                    		, case when TranCCY = 'IDR' then sum(round(SubcFee, 0)) else sum(round(SubcFee, 2)) end  
	                    		, case when TranCCY = 'IDR' then sum(round(SubcFeeBased, 0)) else sum(round(SubcFeeBased, 2)) end 
	                    		, case when TranCCY = 'IDR' then sum(round(TaxFeeBased, 0)) else sum(round(TaxFeeBased, 2)) end 
	                    		, case when TranCCY = 'IDR' then sum(round(FeeBased3, 0)) else sum(round(FeeBased3, 2)) end 
	                    		, case when TranCCY = 'IDR' then sum(round(FeeBased4, 0)) else sum(round(FeeBased4, 2)) end 
	                    		, case when TranCCY = 'IDR' then sum(round(FeeBased5, 0)) else sum(round(FeeBased5, 2)) end 
	                    		from ReksaTransaction_TT 
	                    		where BillId = @nBillId
	                    		    and Status = 1
	                    		    and TranType in (1,2)
	                    		group by BillId, TranCCY
	                    	end
	                    	else 
	                    	begin
	                    		set @cErrMsg = 'Error Prepare data Bill (1)'
	                    		raiserror(@cErrMsg,16,1)
	                    	end

                        End

                        If @nTranType in (3,4)
                        Begin
	                    	if exists (select top 1 1 from ReksaTransaction_TT 
	                    		where BillId = @nBillId
                                and Status = 1
                                and TranType in (3,4))
	                    	begin
	                    		insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5)
	                    		select isnull(BillId,0)                
	                    			, sum(case when TranCCY = 'IDR' then round(TranAmt, 0) - round(RedempFee, 0)  
	                    		          else round(TranAmt, 2) - round(RedempFee, 2)  
	                    		          end) 
	                    			, case when TranCCY = 'IDR' then sum(round(RedempFee, 0)) else sum(round(RedempFee, 2)) end 
	                    		    , case when TranCCY = 'IDR' then sum(round(RedempFeeBased, 0)) else sum(round(RedempFeeBased, 2)) end 
	                    		    , case when TranCCY = 'IDR' then sum(round(TaxFeeBased, 0)) else sum(round(TaxFeeBased, 2)) end 
	                    		    , case when TranCCY = 'IDR' then sum(round(FeeBased3, 0)) else sum(round(FeeBased3, 2)) end 
	                    		    , case when TranCCY = 'IDR' then sum(round(FeeBased4, 0)) else sum(round(FeeBased4, 2)) end 
	                    		    , case when TranCCY = 'IDR' then sum(round(FeeBased5, 0)) else sum(round(FeeBased5, 2)) end 
	                    			from ReksaTransaction_TT
	                    		where BillId = @nBillId
	                    		    and Status = 1
	                    		    and TranType in (3,4)
	                    		group by BillId, TranCCY
	                    	end
	                    	else 
	                    	begin
	                    		set @cErrMsg = 'Error Prepare data Bill (2)'
	                    		raiserror(@cErrMsg,16,1)
	                    	end
                        End

                        -- jika bill kosong, isi dengan 0
                        if not exists(select top 1 1 from #TempBill where BillId = @nBillId)
                        Begin
                            insert #TempBill(BillId, TotalBill, Fee, FeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5)
	                        select @nBillId, 0, 0, 0, 0, 0, 0, 0
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
	                    		raiserror(@cErrMsg,16,1)
                            End
                        end
                        else
                        Begin
                            update a 
                            set TotalBill   = b.TotalBill
                                , Fee       = b.Fee
                                , FeeBased  = b.FeeBased
                                , TaxFeeBased = b.TaxFeeBased
                                , FeeBased3 = b.FeeBased3
                                , FeeBased4 = b.FeeBased4
                                , FeeBased5 = b.FeeBased5                     
	                    	from ReksaBill_TH a 
	                    		join #TempBill b
                                on a.BillId = b.BillId

                            if @@error != 0
                            Begin
                                set @cErrMsg = 'Error Update data Bill (2)'
	                    		raiserror(@cErrMsg,16,1)
                            End
                        end

                        update ReksaTransaction_TT 
                        set Status = 4
                            ,ExtStatus = 4
                            , CancelSuid = @nNik
                            , CancelDate = getdate() 
                            , BillId = 0
                        where TranId = @nTranId  

                        If @@error != 0
                        begin  
	                    	set @cErrMsg = 'Error update status (1)'
	                    	raiserror(@cErrMsg,16,1)
                        end  
                    drop table #TempBill
                    select @cErrMsg cErrMsg 
                    END TRY 
                    BEGIN CATCH
                    drop table #TempBill
                    select @cErrMsg cErrMsg
                    END CATCH
                ";
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksaDev, this._ignoreSSL, sql, out dsData, out ErrMsg))
                {
                    if (dsData.Tables[0].Rows.Count > 0 )
                    {
                        ErrMsg = dsData.Tables[0].Rows[0]["cErrMsg"].ToString();
                        if (ErrMsg != "")
                        {
                            bResult = false;
                            throw new Exception(ErrMsg);
                        }
                        else
                        {
                            bResult = true;
                        }
                    }
                }
                else
                {
                    bResult = false;
                    throw new Exception(ErrMsg);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return bResult;
        }
        public bool UpdateStatusTransaksi(int TranId, int Nik)
        {
            bool bResult = false;
            DataSet dsData = new DataSet();
            string ErrMsg = "";
            try
            {
                string sql = @"
                        declare 
                        @nTranId   int
                        , @nNik     int

                        set @nTranId = " + TranId + @"
                        set @nNik = " + Nik + @"

                        update ReksaTransaction_TT 
                        set Status = 4
                            ,ExtStatus = 3
                            , CancelSuid=@nNik
                            , CancelDate=getdate() 
                        where TranId = @nTranId  
                        
                        
                ";
                if (clsCallSPWs.CallQueryFromWs(this._strUrlWsReksaDev, this._ignoreSSL, sql, out dsData, out ErrMsg))
                {
                    bResult = true;
                }
                else
                {
                    bResult = false;
                    throw new Exception(ErrMsg);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return bResult;

        }
    }
}