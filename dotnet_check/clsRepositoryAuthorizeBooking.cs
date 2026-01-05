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
    public class clsRepositoryAuthorizeBooking
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
        public clsRepositoryAuthorizeBooking(IConfiguration iconfiguration, GlobalVariabelList globalVariabelList)
        {
            _globalVariabelList = globalVariabelList;
            _ConnectionStringDBReksa = globalVariabelList.ConnectionStringDBReksa;
            this._strUrlWsReksa = globalVariabelList.URLWsReksa2;
            this._strUrlWsReksaDev = globalVariabelList.URLWsReksa2;
        }
        public ApiMessage<GetParamAuthorizeBookingRs> GetParam(ApiMessage<ReksaAuthorizeBookingRq> request)
        {
            ApiMessage<GetParamAuthorizeBookingRs> response = new ApiMessage<GetParamAuthorizeBookingRs>();
            response.Data = new GetParamAuthorizeBookingRs();
            string QGetParam = "", ErrGetParam = "";
            DataSet dsGetParam = new DataSet();
            response.IsSuccess = true;

            try
            {
                QGetParam = @"
            declare    
            @nBookingId					int   
            --20250303, Dimas Hadianto, RDN-1230, begin
            --, @cTellerId              varchar(5)
            , @cTellerId                varchar(10)
            --20250303, Dimas Hadianto, RDN-1230, end
            , @dCurrent					datetime   
            , @cCIFNo					char(13)  
            , @dCurrWorkingDate			datetime     
            , @cTranBranch				char(5)      
            , @cNik						int  
            , @nAuthType				char(1)   
            , @nUserSuid				int      
            , @cErrMsg					varchar(800)
            , @cErrProviderCode			varchar(10)
            , @dEffectiveDate			datetime      
            , @dCutOffDate				datetime      
            , @dPeriodEnd				datetime     
            , @pbAutomatic				bit  
            , @pbDebug					bit    
            , @bAccepted				bit    

            set @nBookingId = " + request.Data.nBookingId + @"
            set @cNik = " + request.Data.cNik + @"
            
            select @dCurrent = current_working_date from dbo.fnGetWorkingDate()      
            set @cTellerId = dbo.fnReksaGetParam('TELLERID')
              
            select @cCIFNo = CIFNo 
            from dbo.ReksaBooking_TM 
            where BookingId = @nBookingId  
                
            select @dCurrWorkingDate = current_working_date   
            from dbo.control_table  
            
            select @cTranBranch = b.office_id_sibs      
            from user_nisp_v a join office_information_all_v b      
             on a.office_id = b.office_id      
            where a.nik = @cNik      
             and isnull(ltrim(cost_centre_sibs),'') = ''      
            
            BEGIN TRY
            if exists(select * from ReksaBooking_TH where BookingId = @nBookingId and AuthType = 4)      
            Begin      
            	select @nAuthType = 2, @nUserSuid = UserSuid      
            	from ReksaBooking_TH      
            	where BookingId = @nBookingId      
            	and AuthType = 4   
                  
            	If @@error!= 0 or @@rowcount = 0      
            	Begin   
            		set @cErrProviderCode = '200'   
            		set @cErrMsg = 'Gagal Ambil Flag Otorisasi(1)!'      
            		raiserror(@cErrMsg, 16, 1)    
            	End      
            End      
            else      
            begin      
            	select @nAuthType = AuthType, @nUserSuid = UserSuid      
            	from ReksaBooking_TM      
            	where BookingId = @nBookingId      
            	     
            	If @@error!= 0 or @@rowcount = 0      
            	Begin      
            		set @cErrProviderCode = '200'   
            		set @cErrMsg = 'Gagal Ambil Flag Otorisasi(2)!'      
            		raiserror(@cErrMsg, 16, 1)      
            	End      
            End      
                
                  
            If @nUserSuid = @cNik      
            Begin  
            	set @cErrProviderCode = '200'     
            	set @cErrMsg = 'Autorizer tidak boleh orang yang sama!'     
            	raiserror(@cErrMsg, 16, 1)    
            End      
                 
            if @pbAutomatic = 0      
            begin      
            	if (select rp.Status from ReksaProduct_TM rp      
            	  join ReksaBooking_TM rb      
            	 on rp.ProdId = rb.ProdId      
            	 where rb.BookingId = @nBookingId) = 1      
            	begin   
            		set @cErrProviderCode = '200'     
            		set @cErrMsg = 'Status produk sudah AKTIF, tidak boleh ada otorisasi booking'     
            		raiserror(@cErrMsg, 16, 1)    
            	end      
            	else      
            	begin 
            		select @dPeriodEnd = PeriodEnd, @dEffectiveDate = dbo.fnReksaGetEffectiveDate(PeriodEnd, EffectiveAfter - 1) 
            		from ReksaProduct_TM rp      
            		join ReksaBooking_TM rb      
            			on rp.ProdId = rb.ProdId      
            		where rb.BookingId = @nBookingId      
                    
            		set @dCutOffDate = convert(datetime, convert(varchar(8), @dEffectiveDate, 112) + ' ' + dbo.fnReksaGetParam('BOOKCUTOFF'))      
                    
            		if @pbDebug = 1      
            		 select @dCutOffDate, getdate()      
                    
            		if (@dCutOffDate <= convert(datetime, convert(varchar(8), @dCurrent, 112) + ' ' + convert(varchar(8), getdate(), 108)) )      
            		begin      
            		 -- sudah lewat cutoff maka tolak      
            		 	set @cErrProviderCode = '200'     
            			set @cErrMsg = 'Gagal otorisasi karena sudah lewat waktu cut off'     
            			raiserror(@cErrMsg, 16, 1)  
            		end      
            		else      
            		begin      
            			-- bole, tp cek tgl akhir penawaran      
            			if (@dCurrent > @dPeriodEnd) and @bAccepted = 0      
            			begin      
            		 		set @cErrProviderCode = '200'     
            				set @cErrMsg = 'Booking > tgl akhir penawaran, tidak boleh reject'     
            				raiserror(@cErrMsg, 16, 1)  
            			end      
            		end      
            	end      
            end   
                  
            if @nAuthType in (1,3) and not exists(select * from ReksaBooking_TM where BookingId = @nBookingId and CheckerSuid is null)     
            begin
            	set @cErrProviderCode = '200'     
            	set @cErrMsg = 'Data Booking Tidak Ada'     
            	raiserror(@cErrMsg, 16, 1)  
            end 
                  
            if @nAuthType = 2 and not exists(select * from ReksaBooking_TH where BookingId = @nBookingId and AuthType = 4)      
            begin
            	set @cErrProviderCode = '200'     
            	set @cErrMsg = 'Data Booking Tidak Ada'     
            	raiserror(@cErrMsg, 16, 1)  
            end
             
            	select isnull(@cErrProviderCode,'') cErrProviderCode, isnull(@cErrMsg,'') cErrMsg, isnull(@nAuthType,0) nAuthType, isnull(@nUserSuid,0) nUserSuid 
            	, @dCurrent dCurrent, @cTellerId cTellerId, @cCIFNo cCIFNo, @dCurrWorkingDate dCurrWorkingDate, isnull(@cTranBranch,'') cTranBranch  
            
            END TRY 
            BEGIN CATCH 
            	select isnull(@cErrProviderCode,'') cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
            END CATCH";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QGetParam, out dsGetParam, out ErrGetParam))
                    throw new Exception(ErrGetParam);

                if (!ErrGetParam.EndsWith(""))
                    throw new Exception(ErrGetParam);

                if ((dsGetParam.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsGetParam.Tables[0].Rows[0]["cErrMsg"].ToString());

                if (dsGetParam.Tables[0].Rows.Count > 0)
                {
                    response.Data.AuthType = Convert.ToInt32(dsGetParam.Tables[0].Rows[0]["nAuthType"]);
                    response.Data.UserSuid = Convert.ToInt32(dsGetParam.Tables[0].Rows[0]["nUserSuid"]);
                    response.Data.Current = Convert.ToDateTime(dsGetParam.Tables[0].Rows[0]["dCurrent"]);
                    response.Data.TellerId = dsGetParam.Tables[0].Rows[0]["cTellerId"].ToString();
                    response.Data.CIFNo = dsGetParam.Tables[0].Rows[0]["cCIFNo"].ToString();
                    response.Data.CurrWorkingDate = Convert.ToDateTime(dsGetParam.Tables[0].Rows[0]["dCurrWorkingDate"]);
                    response.Data.TranBranch = dsGetParam.Tables[0].Rows[0]["cTranBranch"].ToString();
                }
            }

            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Data = null;
            }
            return response;

        }
        public ApiMessage<NewBookingRs> NewBooking(ApiMessage<ReksaAuthorizeBookingRq> request)
        {
            ApiMessage<NewBookingRs> response = new ApiMessage<NewBookingRs>();
            response.Data = new NewBookingRs();
            string QApprove = "", ErrApprove = "";
            DataSet dsApprove = new DataSet();
            response.IsSuccess = true;
            try
            {

                QApprove = @"
            declare 
            @nBookingId					int
            , @mBookingAmt				decimal(25,13)    
            , @cBookingCode				varchar(10)   
            , @nProdId					int       
            , @nSequenceNo				int        
            , @bIsEmployee				bit     
            , @cBookingCCY				char(3)          
            , @cBlockBranch				char(5)       
            , @dTranDate				datetime  
            , @nAmountBeforeFee			decimal(25,13)   
            , @nFee						decimal(25,13)  
            , @cErrProviderCode			varchar(10)
            , @cErrMsg					varchar(800)    
            , @cDebitAccountId			varchar(19)    
            , @nFeeId					int         
            , @nFeePct					decimal(25,13) 
            , @pmFee					decimal(25,13)   
            , @nSubFeeBased				decimal(25,13)  
            , @mFeeBased				decimal(25,13)  
            , @nPercentageTaxFee		decimal(25,13)  
            , @mTaxFeeBased				decimal(25,13)   
            , @nPercentageFee3			decimal(25,13)   
            , @mFeeBased3				decimal(25,13)   
            , @nPercentageFee4			decimal(25,13)   
            , @mFeeBased4				decimal(25,13)   
            , @nPercentageFee5			decimal(25,13)   
            , @mFeeBased5				decimal(25,13)   
            , @mTotalFeeBased			decimal(25,13) 
            , @bIsBlokir				bit  
            --20250303, Dimas Hadianto, RDN-1230, begin
            --, @cTellerId              varchar(5)
            , @cTellerId                varchar(10)
            --20250303, Dimas Hadianto, RDN-1230, end
			, @dCurrWorkingDate			datetime

            set @nBookingId = '" + request.Data.nBookingId + @"'
  
            set @cTellerId = dbo.fnReksaGetParam('TELLERID')
			select @dCurrWorkingDate = current_working_date   
			from dbo.control_table  
  
  
            -- cek apakah duit nasabah cukup atau ngga      
            select @mBookingAmt = BookingAmt
            , @cBookingCode = BookingCode    
             , @nProdId = ProdId      
             , @nSequenceNo = BlockSequence      
             , @bIsEmployee = IsEmployee      
             , @cBlockBranch = isnull(BlockBranch,'')      
             , @cBookingCCY = BookingCCY      
             , @dTranDate = LastUpdate  
             , @nAmountBeforeFee = BookingAmt  
             , @nFee = SubcFee  
            from ReksaBooking_TM      
            where BookingId = @nBookingId      
            
            If @@error != 0 or @@rowcount = 0      
            Begin      
            	set @cErrProviderCode = '200'     
            	set @cErrMsg = 'Data Booking tidak ditemukan!'     
            	raiserror(@cErrMsg, 16, 1)  
            End      
            
            BEGIN TRY 
            
            BEGIN TRAN UPDATEACCOUNT
             
            update tt
            set NISPAccId = case when tt.BookingCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId 
                           when tt.BookingCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != ''  then rm.NISPAccountIdUSD 
                           else rm.NISPAccountIdMC end
            from dbo.ReksaBooking_TM tt
            join dbo.ReksaMasterNasabah_TM rm
              on tt.CIFNo = rm.CIFNo
            where tt.BookingId = @nBookingId 
            and isnull(tt.ExtStatus,0) not in (74)
            and isnull(tt.NISPAccId, '') = ''
            
            
            update tt
            set NISPAccId = case when tt.BookingCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA 
                                       when tt.BookingCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA 
                                       else rm.AccountIdMCTA 
                                       end  
            from dbo.ReksaBooking_TM tt
            join dbo.ReksaMasterNasabah_TM rm
              on tt.CIFNo = rm.CIFNo
            where tt.BookingId = @nBookingId 
            and isnull(tt.ExtStatus,0) in (74)                                      
            and isnull(tt.NISPAccId, '') = ''
            
            COMMIT TRAN UPDATEACCOUNT
            
            select @cDebitAccountId = NISPAccId 
            from dbo.ReksaBooking_TM      
            where BookingId = @nBookingId 
            	 
            if isnull(@cDebitAccountId,'') = ''
            begin
            	set @cErrProviderCode = '200'     
            	set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'     
            	raiserror(@cErrMsg, 16, 1)  
            end   
            
            select @cDebitAccountId =  right(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cBookingCCY, 1) + rtrim(ltrim(@cDebitAccountId)), 12)    
            	  
            -- Amount harus ditambahin Fee      
            select @nFeeId = b.FeeId      
            from ReksaProduct_TM a join ReksaProductParam_TM b      
            on a.ParamId = b.ParamId      
            where a.ProdId = @nProdId      
            
            If exists(select top 1 1 from ReksaSubcPeriod_TM      
            where FeeId = @nFeeId and Nominal = 0)      
            Begin      
            	select top 1  @nFeePct = Fee      
            	from ReksaSubcPeriod_TM      
            	where FeeId = @nFeeId      
            	and IsEmployee = @bIsEmployee      
            End      
            Else      
            Begin      
            	select top 1 @nFeePct = Fee      
            	from ReksaSubcPeriod_TM      
            	where FeeId = @nFeeId      
            	 and IsEmployee = @bIsEmployee      
            	 and Nominal >= isnull(@mBookingAmt,0)      
            	order by Nominal asc      
            End      
            
            If @nFeePct is null      
             set @nFeePct = 0    
            
             
            select @pmFee = dbo.fnReksaSetRounding(@nProdId,3,cast(cast(@nFeePct/100 as decimal(25,13))* @mBookingAmt as decimal(25,13)))    
            
            select @nSubFeeBased = isnull(Percentage,0)  
            from dbo.ReksaListGLFee_TM  
            where TrxType = 'SUBS'  
            and ProdId = @nProdId  
            and Sequence = 1  
            
            
            select @mFeeBased = dbo.fnReksaSetRounding(@nProdId,3, cast(cast(@nSubFeeBased/100.00 as decimal(25,13)) * @pmFee as decimal(25,13)))      
                  
            If @mFeeBased > @pmFee      
             set @mFeeBased = @pmFee  
              
            --pembagian pajak fee based  
            select @nPercentageTaxFee = isnull(Percentage,0)  
            from dbo.ReksaListGLFee_TM  
            where TrxType = 'SUBS'  
             and ProdId = @nProdId  
             and Sequence = 2  
              
            select @mTaxFeeBased = dbo.fnReksaSetRounding(@nProdId,3,cast(cast(@nPercentageTaxFee/100.00 as decimal(25,13)) * @pmFee as decimal(25,13)))  
              
            if @mTaxFeeBased > @pmFee            
              set @mTaxFeeBased = @pmFee       
              
            --pembagian fee based 3 (jika ada)  
            select @nPercentageFee3 = isnull(Percentage,0)  
            from dbo.ReksaListGLFee_TM  
            where TrxType = 'SUBS'  
             and ProdId = @nProdId  
             and Sequence = 3  
              
            select @mFeeBased3 = dbo.fnReksaSetRounding(@nProdId,3,cast(cast(@nPercentageFee3/100.00 as decimal(25,13)) * @pmFee as decimal(25,13)))  
              
            if @mFeeBased3 > @pmFee            
              set @mFeeBased3 = @pmFee   
                
            --pembagian fee based 4 (jika ada)      
            select @nPercentageFee4 = isnull(Percentage,0)  
            from dbo.ReksaListGLFee_TM  
            where TrxType = 'SUBS'  
             and ProdId = @nProdId  
             and Sequence = 4  
              
            select @mFeeBased4 = dbo.fnReksaSetRounding(@nProdId,3,cast(cast(@nPercentageFee4/100.00 as decimal(25,13)) * @pmFee as decimal(25,13)))  
              
            if @mFeeBased4 > @pmFee            
              set @mFeeBased4 = @pmFee   
              
            --pembagian fee based 5 (jika ada)      
            select @nPercentageFee5 = isnull(Percentage,0)  
            from dbo.ReksaListGLFee_TM  
            where TrxType = 'SUBS'  
             and ProdId = @nProdId  
             and Sequence = 5  
              
            select @mFeeBased5 = dbo.fnReksaSetRounding(@nProdId,3,cast(cast(@nPercentageFee5/100.00 as decimal(25,13)) * @pmFee as decimal(25,13)))  
              
            if @mFeeBased5 > @pmFee            
              set @mFeeBased5 = @pmFee   
                
            		--total fee based  
            set @mTotalFeeBased = isnull(@mFeeBased, 0) + isnull(@mTaxFeeBased,0) + isnull(@mFeeBased3,0)+ isnull(@mFeeBased4,0)+ isnull(@mFeeBased5,0)  
            		  
            if @mTotalFeeBased > @pmFee  
             set @mTotalFeeBased = @pmFee  
            
            select @mBookingAmt = @mBookingAmt + @pmFee   
            --cek sudah pernah diblokir untuk rekening yg sama/ belum  
            if not exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where AccountNumber = @cDebitAccountId and StatusBlokir = 1  
              and BlockExpiry >= @dCurrWorkingDate  
             )  
            begin  
            	set @bIsBlokir = 1  
            end  
            else  
            begin  
            	set @bIsBlokir = 0  
            end
            
            
            select isnull(@nSequenceNo,0) nSequenceNo, isnull(@cDebitAccountId,'') cDebitAccountId, isnull(@cBlockBranch,'') cBlockBranch
            	 , isnull(@cTellerId, '') cTellerId, isnull(@mBookingAmt,0) mBookingAmt, isnull(@bIsBlokir,0) IsBlokir, isnull(@nProdId,0) nProdId
                 , isnull(@cBookingCode,'') cBookingCode
                 , isnull(@pmFee,0) pmFee, isnull(@mFeeBased,0) mFeeBased, isnull(@mTaxFeeBased,0) mTaxFeeBased, isnull(@mFeeBased3,0) mFeeBased3
                 , isnull(@mFeeBased4,0) mFeeBased4, isnull(@mFeeBased5,0) mFeeBased5, isnull(@mTotalFeeBased,0) mTotalFeeBased
                 , @dTranDate dTranDate, isnull(@nAmountBeforeFee,0) nAmountBeforeFee, isnull(@nFee,0) nFee
            	 , isnull(@cErrProviderCode,'')  cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
            
            END TRY 
            BEGIN CATCH
            if @@trancount > 0 
                ROLLBACK TRAN UPDATEACCOUNT
             select isnull(@cErrProviderCode,'')  cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
            END CATCH    ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QApprove, out dsApprove, out ErrApprove))
                    throw new Exception(ErrApprove);

                if (!ErrApprove.EndsWith(""))
                    throw new Exception(ErrApprove);

                if ((dsApprove.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsApprove.Tables[0].Rows[0]["cErrMsg"].ToString());

                if (dsApprove.Tables[0].Rows.Count > 0)
                {
                    response.Data.SequenceNo = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nSequenceNo"]);
                    response.Data.dTranDate = Convert.ToDateTime(dsApprove.Tables[0].Rows[0]["dTranDate"]);
                    response.Data.DebitAccountId = dsApprove.Tables[0].Rows[0]["cDebitAccountId"].ToString();
                    response.Data.BlockBranch = dsApprove.Tables[0].Rows[0]["cBlockBranch"].ToString();
                    response.Data.TellerId = dsApprove.Tables[0].Rows[0]["cTellerId"].ToString();
                    response.Data.BookingAmt = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["mBookingAmt"]);
                    response.Data.IsBlokir = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["IsBlokir"]);
                    response.Data.nProdId = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nProdId"]);
                    response.Data.BookingCode = dsApprove.Tables[0].Rows[0]["cBookingCode"].ToString();
                    response.Data.nAmountBeforeFee = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["nAmountBeforeFee"]);
                    response.Data.nFee = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["nFee"]);
                }
                else
                {
                    throw new Exception("Gagal Update Status Booking");
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Data = null;
            }
            return response;

        }
        public ApiMessage<AuthBookingGetMinBalanceRs> GetMinBalanceBlokir(ApiMessage<AuthBookingGetMinBalanceRq> request)
        {
            ApiMessage<AuthBookingGetMinBalanceRs> response = new ApiMessage<AuthBookingGetMinBalanceRs>();
            response.Data = new AuthBookingGetMinBalanceRs();
            string QGetMinBalance = "", ErrMinBalance = "";
            DataSet dsMinBalance = new DataSet();
            response.IsSuccess = true;
            try
            {

                QGetMinBalance = @"
                declare 
                @cDefaultCCY				char(3)   
                , @cDebitAccountId			varchar(19)    
                , @cCurrencyType			char(3)  
                , @cProductCode				varchar(10)  
                , @mMinBalance				decimal(25,13)      
                , @mClosingFee				decimal(25,13)  
                , @mAvailableBalance		money        
                , @bIsBlokir				bit    
                , @mBookingAmt				decimal(25,13)  
                , @dBlockExpiry				datetime      
                , @nProdId					int     
                , @cBlockReason				varchar(40)   
                , @cBookingCode				varchar(10)   
                , @nSaldoMinBlokir			decimal(25,13) 	
                , @cErrProviderCode			varchar(10)
				, @cErrMsg					varchar(200)
                
                set @cDebitAccountId	=  '" + request.Data.cDebitAccountId + @"' 
                set @cCurrencyType		=  '" + request.Data.cCurrencyType + @"' 
                set @cProductCode		=  '" + request.Data.cProductCode + @"' 
                set @mAvailableBalance	=  " + request.Data.mAvailableBalance + @"
                set @bIsBlokir			= " + request.Data.bIsBlokir + @"
                set @mBookingAmt		= " + request.Data.mBookingAmt + @"
                set @nProdId			= " + request.Data.nProdId + @"
                set @cBookingCode		=  '" + request.Data.cBookingCode + @"' 
                
                select @cDefaultCCY = CFAGTY
                from dbo.CFAGRPMC_v
                where convert(bigint, CFAGNO) = convert(bigint, @cDebitAccountId)
                
                if(@cProductCode like '%MC%' and @cCurrencyType = @cDefaultCCY)  
                begin  
                	select @mMinBalance = MINBLM, @mClosingFee = CLSFEE      
                	from SQL_SIBS.dbo.DDPAR2MC_v      
                	where SCCODE = @cProductCode   
                	and DP2CUR = @cCurrencyType     
                end  
                else  
                begin  
                	select @mMinBalance = MINBLM, @mClosingFee = CLSFEE      
                	from DDPAR2_v      
                	where SCCODE = @cProductCode      
                	 and DP2CUR = @cCurrencyType      
                end  
                
                if @mMinBalance is null      
                 set @mMinBalance = 0      
                     
                if @mClosingFee is null      
                 set @mClosingFee = 0      
                     
                if @mAvailableBalance is null      
                 set @mAvailableBalance = 0      
                     
                if @bIsBlokir = 0  
                begin  
                	set @mMinBalance = 0  
                	set @mClosingFee = 0  
                end
                
                BEGIN TRY 
                
                if @mBookingAmt > (@mAvailableBalance - @mMinBalance)   
                Begin
                      set @cErrProviderCode = '200'     
                	  set @cErrMsg = 'Saldo Tidak Cukup untuk transaksi!'     
                	  raiserror(@cErrMsg, 16, 1)  
                End      
                       
                -- Jika saldo cukup, lakukan blokir      
                      
                select @dBlockExpiry = dateadd(mm, 1, PeriodEnd)      
                from ReksaProduct_TM      
                where ProdId = @nProdId      
                
                select @cBlockReason = 'Booking Reksadana ' + @cBookingCode      
                select @nSaldoMinBlokir =  @mMinBalance  
                set @mBookingAmt = @mBookingAmt + @mMinBalance   
                
                
                select isnull(@mMinBalance,0) mMinBalance, isnull(@mClosingFee,0) mClosingFee, @dBlockExpiry dBlockExpiry, isnull(@cBlockReason,'') cBlockReason
                	  , isnull(@nSaldoMinBlokir,0) nSaldoMinBlokir, isnull(@cErrProviderCode,'') cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
                END TRY 
                BEGIN CATCH
                	select isnull(@cErrProviderCode,'') cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
                END CATCH
                
                 ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QGetMinBalance, out dsMinBalance, out ErrMinBalance))
                    throw new Exception(ErrMinBalance);

                if (!ErrMinBalance.EndsWith(""))
                    throw new Exception(ErrMinBalance);

                if ((dsMinBalance.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsMinBalance.Tables[0].Rows[0]["cErrMsg"].ToString());

                if (dsMinBalance.Tables[0].Rows.Count > 0)
                {
                    response.Data.mMinBalance = Convert.ToDecimal(dsMinBalance.Tables[0].Rows[0]["mMinBalance"]);
                    response.Data.mClosingFee = Convert.ToDecimal(dsMinBalance.Tables[0].Rows[0]["mClosingFee"]);
                    response.Data.dBlockExpiry = Convert.ToDateTime(dsMinBalance.Tables[0].Rows[0]["dBlockExpiry"].ToString());
                    response.Data.cBlockReason = dsMinBalance.Tables[0].Rows[0]["cBlockReason"].ToString();
                    response.Data.nSaldoMinBlokir = Convert.ToDecimal(dsMinBalance.Tables[0].Rows[0]["nSaldoMinBlokir"]);
                }
                else
                {
                    throw new Exception("Gagal Update Status Booking");
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Data = null;
            }
            return response;

        }
        public bool UpdateStatusBlokir(ApiMessage<AuthBookingUpdateStatusRq> request)
        {
            bool bResult = false;
            string QUpdateStatus = "", ErrUpdateStatus = "";
            DataSet dsUpdateStatus = new DataSet();

            try
            {
                QUpdateStatus = @"
                declare 
                @nBookingId				int
                , @cNik					int
                , @nSequenceNo			int
                , @cTranBranch			varchar(10)
                , @pmFee				decimal(25,13)
                , @mFeeBased			decimal(25,13)
                , @mTaxFeeBased			decimal(25,13)
                , @mFeeBased3			decimal(25,13) 
                , @mFeeBased4			decimal(25,13) 
                , @mFeeBased5			decimal(25,13) 
                , @mTotalFeeBased		decimal(25,13) 
                , @cDebitAccountId		varchar(20)       
                , @dTranDate			datetime
                , @cCurrencyType		varchar(5)
                , @nAmountBeforeFee		decimal(25,13)
                , @nFee					decimal(25,13)
                , @dBlockExpiry			datetime
                , @cTellerId			varchar(10)
                , @bIsBlokir			bit
                , @cBlockReason			varchar(100)
                , @cCIFNo				char(13)
				, @nSaldoMinBlokir		decimal(25,13)
				, @mBookingAmt			decimal(25,13)
                
                set @nBookingId			= " + request.Data.nBookingId + @"		
                set @cNik				= '" + request.Data.cNik + @"'		
                set @nSequenceNo		= " + request.Data.nSequenceNo + @"
                set @cTranBranch		= '" + request.Data.cTranBranch + @"'
                set @pmFee				= " + request.Data.pmFee + @"
                set @mFeeBased			= " + request.Data.mFeeBased + @"
                set @mTaxFeeBased		= " + request.Data.mTaxFeeBased + @"
                set @mFeeBased3			= " + request.Data.mFeeBased3 + @"
                set @mFeeBased4			= " + request.Data.mFeeBased4 + @"
                set @mFeeBased5			= " + request.Data.mFeeBased5 + @"
                set @mTotalFeeBased		= " + request.Data.mTotalFeeBased + @"
                set @cDebitAccountId	= '" + request.Data.cDebitAccountId + @"'
                set @dTranDate			= '" + request.Data.dTranDate + @"'
                set @cCurrencyType		= '" + request.Data.cCurrencyType + @"'
                set @nAmountBeforeFee	= " + request.Data.nAmountBeforeFee + @"
                set @nFee				= " + request.Data.nFee + @"
                set @dBlockExpiry		= '" + request.Data.dBlockExpiry + @"'
                set @cTellerId			= '" + request.Data.cTellerId + @"'
                set @bIsBlokir			= " + request.Data.bIsBlokir + @"
                set @cBlockReason		= '" + request.Data.cBlockReason + @"'
                set @cCIFNo				= '" + request.Data.cCIFNo + @"'
				set @nSaldoMinBlokir	= " + request.Data.nSaldoMinBlokir + @"
				set @mBookingAmt		= " + request.Data.mBookingAmt + @"
                
                 Update ReksaBooking_TM      
                 set CheckerSuid = @cNik      
                  , BlockSequence = @nSequenceNo      
                  , BlockBranch = @cTranBranch      
                  , SubcFee  = @pmFee      
                  , SubcFeeBased = @mFeeBased   
                  , TaxFeeBased = @mTaxFeeBased  
                  , FeeBased3 = @mFeeBased3  
                  , FeeBased4 = @mFeeBased4  
                  , FeeBased5 = @mFeeBased5  
                  , TotalFeeBased = @mTotalFeeBased        
                 where BookingId = @nBookingId      
                
                 insert dbo.ReksaTranBlokirRelation_TM(TranId, TranType, AccountNumber, TranDate, NAVValueDate, TranCCY,  
                 TranAmt, FeeAmt, SaldoMinBlokir,   
                 BlokirAmount, BlockSequence, BlockBranch, BlockExpiry, TellerId, StatusBlokir, BlockReason  
                 )  
                 select @nBookingId, 0, @cDebitAccountId, @dTranDate, null, @cCurrencyType,  
                 @nAmountBeforeFee, @nFee, @nSaldoMinBlokir,  
                 @mBookingAmt, @nSequenceNo, @cTranBranch, @dBlockExpiry, @cTellerId, @bIsBlokir, @cBlockReason  
                  
                 update dbo.ReksaBooking_TM   
                 set DocRiskProfile = 1  
                 , DocTermCondition = 1  
                 where CIFNo = @cCIFNo  
                
                ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QUpdateStatus, out dsUpdateStatus, out ErrUpdateStatus))
                    throw new Exception(ErrUpdateStatus);

                if (!ErrUpdateStatus.EndsWith(""))
                    throw new Exception(ErrUpdateStatus);

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw new Exception(ex.Message);
            }
            return bResult;

        }
        public bool UpdateStatusReleaseBlokir(int nBookingId, string cDebitAccountId, int nSequenceBlokir)
        {
            bool bResult = false;
            string Query = "", ErrQuery = "";
            DataSet dsQuery = new DataSet();
            try
            {
                Query = @" 
                declare 
                @nBookingId         int
                , @cDebitAccountId  varchar(19)
                , @nSequenceNo      int
            
                set @nBookingId = " + nBookingId + @"
                set @cDebitAccountId = '" + cDebitAccountId + @"'
                set @nSequenceNo = " + nSequenceBlokir + @"
            
                update dbo.ReksaTranBlokirRelation_TM
                set StatusBlokir = 2
                where TranId = @nBookingId
                and AccountNumber = @cDebitAccountId
                and BlockSequence = @nSequenceNo
                
                Update ReksaBooking_TM
                set BlockSequence = 0
                 , BlockBranch = ''
                where BookingId = @nBookingId"

                ;
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, Query, out dsQuery, out ErrQuery))
                    throw new Exception(ErrQuery);
                bResult = true;
            }
            catch (Exception ex)
            {
                bResult = false;
            }

            return bResult;
        }
        public bool UpdateStatusRejectEditBooking(int nBookingId, int nSequence, string cNik, string cTranBranch)
        {
            bool bResult = false;
            string QEditRejectBooking = "", ErrEditRejectBooking = "";
            DataSet dsEditRejectBooking = new DataSet();

            try
            {
                QEditRejectBooking = @"declare 
                 @nBookingId			int
                 , @nSequenceNo			int
                 , @cTranBranch			char(10)
                 , @cNik				char(10)
                 , @cErrMsg				varchar(500)
                
                set @nBookingId			= " + nBookingId + @"
                set @nSequenceNo		= " + nSequence + @"
                set @cTranBranch		= '" + cTranBranch + @"'
                set @cNik				= '" + cNik + @"'
                
                BEGIN TRY 
                
                BEGIN TRAN UPDATEBOOKING
                     
                Update ReksaBooking_TM      
                set BlockSequence = @nSequenceNo      
                 , BlockBranch = @cTranBranch      
                where BookingId = @nBookingId       
                     
                if @@error != 0 or @@rowcount = 0      
                Begin      
                	set @cErrMsg = 'Gagal Blokir Rekening!'   
                    raiserror(@cErrMsg, 16, 1)  
                End
                    
                delete ReksaBooking_TH      
                where BookingId = @nBookingId      
                and AuthType = 4      
                      
                if @@error != 0 or @@rowcount = 0      
                Begin      
                	set @cErrMsg = 'Gagal Delete Request Update!' 
                    raiserror(@cErrMsg, 16, 1)      
                End      
                      
                update ReksaCIFConfirmAddr_TH      
                set Status = 5      
                 , SpvNIK = @cNik      
                where Id = @nBookingId      
                 and Status = 4      
                 and DataType = 0      
                      
                If @@error != 0       
                Begin      
                	set @cErrMsg = 'Gagal Update Alamat Konfirmasi!'   
                    raiserror(@cErrMsg, 16, 1)  
                End     
                
                COMMIT TRAN UPDATEBOOKING
                
                END TRY 
                BEGIN CATCH
                if @@trancount > 0
                	ROLLBACK TRAN UPDATEBOOKING
                	select isnull(@cErrMsg,'') cErrMsg
                END CATCH ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QEditRejectBooking, out dsEditRejectBooking, out ErrEditRejectBooking))
                    throw new Exception(ErrEditRejectBooking);

                if (!ErrEditRejectBooking.EndsWith(""))
                    throw new Exception(ErrEditRejectBooking);

                if ((dsEditRejectBooking.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsEditRejectBooking.Tables[0].Rows[0]["cErrMsg"].ToString());

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw new Exception(ex.Message);
            }
            return bResult;
        }
        public ApiMessage<EditBookingRs> EditBooking(ApiMessage<ReksaAuthorizeBookingRq> request)
        {
            ApiMessage<EditBookingRs> response = new ApiMessage<EditBookingRs>();
            response.Data = new EditBookingRs();
            string QApprove = "", ErrApprove = "";
            DataSet dsApprove = new DataSet();
            response.IsSuccess = true;
            try
            {

                QApprove = @"
            declare 
            @nBookingId					int
            , @mBookingAmt				decimal(25,13)    
            , @cBookingCode				varchar(10)   
            , @nProdId					int       
            , @nSequenceNo				int        
            , @bIsEmployee				bit     
            , @cBookingCCY				char(3)          
            , @cBlockBranch				char(5)       
            , @dTranDate				datetime  
            , @nAmountBeforeFee			decimal(25,13)   
            , @nFee						decimal(25,13)  
            , @cErrProviderCode			varchar(10)
            , @cErrMsg					varchar(800)    
            , @cDebitAccountId			varchar(19)    
            , @bIsBlokir				bit  
            --20250303, Dimas Hadianto, RDN-1230, begin
            --, @cTellerId              varchar(5)
            , @cTellerId                varchar(10)
            --20250303, Dimas Hadianto, RDN-1230, end
            , @mNewAmount               decimal(25,13)      
            , @cNewAccount              varchar(19)   
            , @mOldAmount               decimal(25,13) 
            , @nTranIdTrx2              int 
            , @cTranCodeTrx2            varchar(20)
            , @nBlokirAmountTrx2        decimal(25,13)  
            , @nBlockBranchTrx2         varchar(5)  
            , @nBlockSequenceTrx2       int  
            , @nNewSequenceNo           int  
            , @nTranTypeTrx2            int  
            , @nBlokirIdTrx2            int  
            , @cBlockReasonTrx2         varchar(100)  
            , @nOldDate                 datetime      
            , @nOldSpv                  int      



            set @nBookingId = '" + request.Data.nBookingId + @"'
  
            set @cTellerId = dbo.fnReksaGetParam('TELLERID')
  
            -- cek apakah duit nasabah cukup atau ngga      

            select @mBookingAmt = BookingAmt
              , @cBookingCode = BookingCode      
              , @cDebitAccountId = NISPAccId      
              , @nSequenceNo = BlockSequence      
              , @cBlockBranch = isnull(BlockBranch,'')      
              , @cBookingCCY = BookingCCY      
              , @nOldDate   = LastUpdate      
              , @nOldSpv   = CheckerSuid      
              , @dTranDate = LastUpdate  
              , @nAmountBeforeFee = BookingAmt  
              , @nFee = SubcFee  
             from ReksaBooking_TM      
             where BookingId = @nBookingId 
            
            If @@error != 0 or @@rowcount = 0      
            Begin      
            	set @cErrProviderCode = '200'     
            	set @cErrMsg = 'Data Booking tidak ditemukan!'     
            	raiserror(@cErrMsg, 16, 1)  
            End      
            
            BEGIN TRY 
            
            BEGIN TRAN UPDATEACCOUNT
             
            select @cDebitAccountId =  right(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cBookingCCY, 1) + rtrim(ltrim(@cDebitAccountId)), 12)  

            update tt
	        set NISPAccId = case when tt.BookingCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId 
	                       when tt.BookingCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != ''  then rm.NISPAccountIdUSD 
	                       else rm.NISPAccountIdMC end
	        from dbo.ReksaBooking_TH tt
	        join dbo.ReksaMasterNasabah_TM rm
	          on tt.CIFNo = rm.CIFNo
	        where tt.BookingId = @nBookingId 
	          and tt.AuthType = 4
            and isnull(tt.ExtStatus,0) not in (74)
	        and isnull(tt.NISPAccId, '') = ''
            
            
            update tt
            set NISPAccId = case when tt.BookingCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA 
                                     when tt.BookingCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA 
                                     else rm.AccountIdMCTA 
                                     end  
	        from dbo.ReksaBooking_TH tt
	        join dbo.ReksaMasterNasabah_TM rm
	          on tt.CIFNo = rm.CIFNo
	        where tt.BookingId = @nBookingId 
	          and tt.AuthType = 4
	          and isnull(tt.ExtStatus,0) in (74)                                      
	        and isnull(tt.NISPAccId, '') = ''
            
            COMMIT TRAN UPDATEACCOUNT
            
            select @mNewAmount = BookingAmt      
	          , @cNewAccount = NISPAccId      
	        from ReksaBooking_TH      
	        where BookingId = @nBookingId       
	        and AuthType = 4    
            	 
            If @@error != 0 or @@rowcount = 0      
	        Begin  
	        	set @cErrProviderCode = '200'     
                set @cErrMsg = 'Data Amount Booking Baru tidak ditemukan!'     
                raiserror(@cErrMsg, 16, 1)    
	        End
	        if isnull(@cNewAccount,'') = ''
	        begin
	        	set @cErrProviderCode = '200'     
	        	set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'  
	        	raiserror(@cErrMsg, 16, 1) 
	        end 
            
	        set @mOldAmount = @mBookingAmt      
              
	        If (@mNewAmount != @mOldAmount)      
	         or (@cDebitAccountId != @cNewAccount)      
	        Begin      
	        --cek apakah trx id tsb termasuk trx yg diblokir amount + min amount  
	        if exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where TranId = @nBookingId and TranType in (0) and StatusBlokir = 1  
	          and AccountNumber = @cDebitAccountId and BlockExpiry >= getdate()  
	        )  
	        begin   
	        	--cek apakah pny transaksi lain di no rekening tsb (include switching juga walaupun ada di SP lain utk switching)  
	        	select top 1   
	        	  @nTranIdTrx2 = TranId,  
	        	  @nTranTypeTrx2 = TranType,  
	        	  @nBlokirIdTrx2 = BlokirId,  
	        	  @cBlockReasonTrx2 = BlockReason,  
	        	  @nBlokirAmountTrx2 = BlokirAmount,   
	        	  @nBlockBranchTrx2 = BlockBranch,  
	        	  @nBlockSequenceTrx2 = BlockSequence  
	        	from dbo.ReksaTranBlokirRelation_TM with (nolock)  
	        	where AccountNumber = @cDebitAccountId and StatusBlokir = 0 and BlockExpiry >= getdate() and TranType in (1,2,5,6,8) and TranId != @nBookingId  
	        	order by TranDate --ascending  
	        	if @@rowcount > 0 set @bIsBlokir = 1   
   
	        end
            end
                select @cTellerId cTellerId,isnull(@mBookingAmt,0) mBookingAmt, isnull(@cBookingCode,'') cBookingCode, isnull(@nProdId,0) nProdId      
                , isnull(@nSequenceNo,0) nSequenceNo,isnull(@cDebitAccountId,'') cDebitAccountId, isnull(@bIsEmployee,0) bIsEmployee, isnull(@cBlockBranch,'') cBlockBranch 
                , isnull(@cBookingCCY,'') cBookingCCY, @dTranDate dTranDate, isnull(@nAmountBeforeFee,0) nAmountBeforeFee, isnull(@nFee,0) nFee  
                , isnull(@bIsBlokir,0) IsBlokir, isnull(@mNewAmount,0) mNewAmount, isnull(@cNewAccount,'') cNewAccount, isnull(@mOldAmount,0) mOldAmount  
                , isnull(@nTranIdTrx2,0) nTranIdTrx2, isnull(@nTranTypeTrx2,0) nTranTypeTrx2, isnull(@nBlokirIdTrx2,0) nBlokirIdTrx2 , isnull(@cBlockReasonTrx2,'') cBlockReasonTrx2  
	        	, isnull(@nBlokirAmountTrx2,0) nBlokirAmountTrx2, isnull(@nBlockBranchTrx2,'') nBlockBranchTrx2, isnull(@nBlockSequenceTrx2,0) nBlockSequenceTrx2 
                , @nOldDate nOldDate, isnull(@nOldSpv,'') nOldSpv 
                , isnull(@cErrProviderCode,'')  cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
            
            END TRY 
            BEGIN CATCH
            if @@trancount > 0
                ROLLBACK TRAN UPDATEACCOUNT
             select isnull(@cErrProviderCode,'')  cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
            END CATCH    ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QApprove, out dsApprove, out ErrApprove))
                    throw new Exception(ErrApprove);

                if (!ErrApprove.EndsWith(""))
                    throw new Exception(ErrApprove);

                if ((dsApprove.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsApprove.Tables[0].Rows[0]["cErrMsg"].ToString());

                if (dsApprove.Tables[0].Rows.Count > 0)
                {
                    response.Data.SequenceNo = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nSequenceNo"]);
                    response.Data.DebitAccountId = dsApprove.Tables[0].Rows[0]["cDebitAccountId"].ToString();
                    response.Data.BlockBranch = dsApprove.Tables[0].Rows[0]["cBlockBranch"].ToString();
                    response.Data.TellerId = dsApprove.Tables[0].Rows[0]["cTellerId"].ToString();
                    response.Data.BookingAmt = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["mBookingAmt"]);
                    response.Data.nOldDate = Convert.ToDateTime(dsApprove.Tables[0].Rows[0]["nOldDate"]);
                    response.Data.nOldSpv = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nOldSpv"]);
                    response.Data.IsBlokir = Convert.ToBoolean(dsApprove.Tables[0].Rows[0]["IsBlokir"]);
                    response.Data.nProdId = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nProdId"]);
                    response.Data.BookingCode = dsApprove.Tables[0].Rows[0]["cBookingCode"].ToString();
                    response.Data.nAmountBeforeFee = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["nAmountBeforeFee"]);
                    response.Data.nFee = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["nFee"]);
                    response.Data.mNewAmount = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["mNewAmount"]);
                    response.Data.cNewAccount = dsApprove.Tables[0].Rows[0]["cNewAccount"].ToString();
                    response.Data.mOldAmount = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["mOldAmount"]);
                    response.Data.nTranIdTrx2 = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nTranIdTrx2"]);
                    response.Data.nTranTypeTrx2 = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nTranTypeTrx2"]);
                    response.Data.nBlokirIdTrx2 = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nBlokirIdTrx2"]);
                    response.Data.cBlockReasonTrx2 = dsApprove.Tables[0].Rows[0]["cBlockReasonTrx2"].ToString();
                    response.Data.nBlokirAmountTrx2 = Convert.ToDecimal(dsApprove.Tables[0].Rows[0]["nBlokirAmountTrx2"]);
                    response.Data.nBlockBranchTrx2 = dsApprove.Tables[0].Rows[0]["nBlockBranchTrx2"].ToString();
                    response.Data.nBlockSequenceTrx2 = Convert.ToInt32(dsApprove.Tables[0].Rows[0]["nBlockSequenceTrx2"]);
                }
                else
                {
                    throw new Exception("Gagal Update Status Booking");
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Data = null;
            }
            return response;

        }
        public bool InsertHistoryBooking(ApiMessage<AuthBookingInsertHistoryRq> request)
        {
            bool bResult = false;
            string QHistoryBooking = "", ErrHistoryBooking = "";
            DataSet dsHistoryBooking = new DataSet();

            try
            {
                QHistoryBooking = @"declare 
            @nBookingId				int 
            , @cErrMsg				varchar(200)
            , @cNik					char(10)
            , @nOldDate				datetime      
            , @nOldSpv				int        
            , @nUserSuid			int     
            , @nSequenceNo			int  
            , @cTranBranch			varchar(10)
            , @mNewAmount			decimal(25,13)
            , @mOldAmount			decimal(25,13)
            , @pmFee				decimal(25,13)    
            , @mFeeBased			decimal(25,13) 
            , @mTaxFeeBased			decimal(25,13) 
            , @mFeeBased3			decimal(25,13) 
            , @mFeeBased4			decimal(25,13) 
            , @mFeeBased5			decimal(25,13) 
            , @mTotalFeeBased		decimal(25,13) 
            , @cCIFNo				char(13)  
            
            
            set @nBookingId				= " + request.Data.nBookingId + @"
            set @cNik					= '" + request.Data.cNik + @"'
            set @nOldDate				= '" + request.Data.nOldDate + @"'
            set @nOldSpv				= " + request.Data.nOldSpv + @" 
            set @nUserSuid				= " + request.Data.nUserSuid + @" 
            set @nSequenceNo			= " + request.Data.nSequenceNo + @" 
            set @cTranBranch			= '" + request.Data.cTranBranch + @"'
            set @mNewAmount				= " + request.Data.mNewAmount + @" 
            set @mOldAmount				= " + request.Data.mOldAmount + @" 
            set @pmFee					= " + request.Data.pmFee + @" 
            set @mFeeBased				= " + request.Data.mFeeBased + @" 
            set @mTaxFeeBased			= " + request.Data.mTaxFeeBased + @" 
            set @mFeeBased3				= " + request.Data.mFeeBased3 + @" 
            set @mFeeBased4				= " + request.Data.mFeeBased4 + @" 
            set @mFeeBased5				= " + request.Data.mFeeBased5 + @" 
            set @mTotalFeeBased			= " + request.Data.mTotalFeeBased + @" 
            set @cCIFNo					= '" + request.Data.cCIFNo + @"' 
            
            BEGIN TRY 
            
            BEGIN TRAN INSERTBOOKINGHISTORY
            
            insert ReksaBooking_TH(BookingId, BookingCode, BookingCounter, BookingCCY, BookingAmt, ProdId, CIFNo      
            , IsEmployee, CIFNIK, AgentId, BankId, AccountType, NISPAccId, NonNISPAccId, NonNISPAccName, Referentor      
            , AuthType, Status, HistoryStatus, LastUpdate, UserSuid, CheckerSuid, BlockSequence, BlockBranch  
            , SubcFee, SubcFeeBased           
            , TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased        
            , CIFName, NISPAccName      
            , Channel  
            , DocFCSubscriptionForm,DocFCDevidentAuthLetter,DocFCJoinAcctStatementLetter          
            , DocFCIDCopy,DocFCOthers,DocTCSubscriptionForm,DocTCTermCondition,DocTCProspectus                       
            , DocTCFundFactSheet,DocTCOthers  
            , Inputter, Seller, Waperd, DocRiskProfile, DocTermCondition)      
            select BookingId, BookingCode, BookingCounter, BookingCCY, BookingAmt, ProdId, CIFNo      
            , IsEmployee, CIFNIK, AgentId, BankId, AccountType, NISPAccId, NonNISPAccId, NonNISPAccName, Referentor      
            , AuthType, Status, 'Old', LastUpdate, UserSuid, CheckerSuid, BlockSequence      
            , BlockBranch, SubcFee, SubcFeeBased      
            , TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased  
            , CIFName, NISPAccName      
            , Channel  
            , DocFCSubscriptionForm,DocFCDevidentAuthLetter,DocFCJoinAcctStatementLetter          
            , DocFCIDCopy,DocFCOthers,DocTCSubscriptionForm,DocTCTermCondition,DocTCProspectus                       
            , DocTCFundFactSheet,DocTCOthers
            , Inputter, Seller, Waperd, DocRiskProfile, DocTermCondition      
             from ReksaBooking_TM      
             where BookingId = @nBookingId  
            
            if @@error != 0 or @@rowcount = 0      
            Begin      
            	set @cErrMsg = 'Error Backup Data!'      
            	goto ERROR      
            End  
            
            
            Update a       
            set BookingCCY   = b.BookingCCY      
            , BookingAmt  = b.BookingAmt      
            , ProdId   = b.ProdId      
            , CIFNo    = b.CIFNo      
            , IsEmployee  = b.IsEmployee      
            , CIFNIK   = b.CIFNIK      
            , AgentId   = b.AgentId      
            , BankId   = b.BankId      
            , AccountType  = b.AccountType      
            , NISPAccId   = b.NISPAccId      
            , NonNISPAccId  = b.NonNISPAccId      
            , NonNISPAccName = b.NonNISPAccName      
            , Referentor  = b.Referentor      
            , LastUpdate  = getdate()      
            , UserSuid   = b.UserSuid      
            , CheckerSuid  = @cNik      
            , BlockSequence  = @nSequenceNo      
            , BlockBranch  = @cTranBranch      
            , SubcFee   = case when @mNewAmount != @mOldAmount then @pmFee else a.SubcFee end      
            , SubcFeeBased  = case when @mNewAmount != @mOldAmount then @mFeeBased else a.SubcFeeBased end      
            , TaxFeeBased = case when @mNewAmount != @mOldAmount then @mTaxFeeBased else a.TaxFeeBased end    
            , FeeBased3 = case when @mNewAmount != @mOldAmount then @mFeeBased3 else a.FeeBased3 end    
            , FeeBased4 = case when @mNewAmount != @mOldAmount then @mFeeBased4 else a.FeeBased4 end    
            , FeeBased5 = case when @mNewAmount != @mOldAmount then @mFeeBased5 else a.FeeBased5 end    
            , TotalFeeBased = case when @mNewAmount != @mOldAmount then @mTotalFeeBased else a.TotalFeeBased end    
            , CIFName   = b.CIFName      
            , NISPAccName  = b.NISPAccName      
            , Inputter = b.Inputter      
            , Seller = b.Seller      
            , Waperd = b.Waperd      
            , DocRiskProfile = b.DocRiskProfile      
            , DocTermCondition = b.DocTermCondition         
            , DocFCSubscriptionForm = b.DocFCSubscriptionForm,
            DocFCDevidentAuthLetter = b.DocFCDevidentAuthLetter,
            DocFCJoinAcctStatementLetter = b.DocFCJoinAcctStatementLetter,
            DocFCIDCopy = b.DocFCIDCopy,
            DocFCOthers = b.DocFCOthers,
            DocTCSubscriptionForm = b.DocTCSubscriptionForm,
            DocTCTermCondition = b.DocTCTermCondition,
            DocTCProspectus = b.DocTCProspectus,
            DocTCFundFactSheet = b.DocTCFundFactSheet,
            DocTCOthers = b.DocTCOthers,
            IsFeeEdit = b.IsFeeEdit,
            PercentageFee = b.PercentageFee,
            JenisPerhitunganFee = b.JenisPerhitunganFee
            , Channel = b.Channel  
            from ReksaBooking_TM a join ReksaBooking_TH b      
              on a.BookingId = b.BookingId      
            where a.BookingId = @nBookingId      
             and b.AuthType = 4      
              
            if @@error != 0 or @@rowcount = 0      
            Begin      
            	set @cErrMsg = 'Error Update Data!'      
            	raiserror(@cErrMsg, 16, 1)     
            End   
            
            Update ReksaBooking_TH      
            set AuthType = 2  
             , CheckerSuid = @cNik      
            where BookingId = @nBookingId      
             and AuthType = 4    
              
            if @@error != 0 or @@rowcount = 0      
            Begin      
            	set @cErrMsg = 'Error Update History!'    
            	raiserror(@cErrMsg, 16, 1)     
            End     
            
            delete ReksaCIFConfirmAddr_TM      
            output deleted.*, 7, @nOldDate, @nUserSuid, @nOldSpv into ReksaCIFConfirmAddr_TH      
            where Id = @nBookingId and DataType = 0     
            
            If @@error != 0 or @@rowcount = 0       
            Begin      
              set @cErrMsg = 'Gagal Hapus Alamat Konfirmasi Lama!'      
            	raiserror(@cErrMsg, 16, 1)     
            End   
            
            
            insert ReksaCIFConfirmAddr_TM (Id, DataType, CIFNo, AddressType, Branch, AddressSeq      
               , AddressLine1, AddressLine2, AddressLine3, AddressLine4, ZIPCode, ForeignAddr      
               , Description, AlamatUtama, KodeAlamat, AlamatSID, PeriodThere, PeriodThereCode      
               , JenisAlamat, StaySince, Kelurahan, Kecamatan, Dati2, Provinsi, KodeDati2)      
            select Id, DataType, CIFNo, AddressType, Branch, AddressSeq      
               , AddressLine1, AddressLine2, AddressLine3, AddressLine4, ZIPCode, ForeignAddr      
               , Description, AlamatUtama, KodeAlamat, AlamatSID, PeriodThere, PeriodThereCode      
               , JenisAlamat, StaySince, Kelurahan, Kecamatan, Dati2, Provinsi, KodeDati2      
            from ReksaCIFConfirmAddr_TH      
            where Id = @nBookingId      
               and Status = 4      
               and DataType = 0   
               
            If @@error != 0 or @@rowcount = 0       
            Begin      
            	set @cErrMsg = 'Gagal Insert Alamat Konfirmasi Baru!'        
            	raiserror(@cErrMsg, 16, 1)     
            End 
            
            update ReksaCIFConfirmAddr_TH      
            set Status = 6      
             , SpvNIK = @cNik      
            where Id = @nBookingId      
              and Status = 4      
              and DataType = 0  
              
            If @@error != 0 or @@rowcount = 0       
            Begin      
            	set @cErrMsg = 'Gagal Update Flag History Alamat Konfirmasi!'      
            	goto ERROR      
            End     
            
            update dbo.ReksaBooking_TM   
             set DocRiskProfile = 1  
             , DocTermCondition = 1  
            where CIFNo = @cCIFNo  
            
            If @@error != 0  or @@rowcount = 0 
            Begin  
            	set @cErrMsg = 'Gagal update DocRiskProfile dan DocTermCondition!'      
            	raiserror(@cErrMsg, 16, 1)     
            End      
            
            COMMIT TRAN INSERTBOOKINGHISTORY
            
            
            END TRY 
            BEGIN CATCH
                if @@trancount > 0
               ROLLBACK TRAN INSERTBOOKINGHISTORY
               select isnull(@cErrMsg,'') cErrMsg
            END CATCH  ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QHistoryBooking, out dsHistoryBooking, out ErrHistoryBooking))
                    throw new Exception(ErrHistoryBooking);

                if (!ErrHistoryBooking.EndsWith(""))
                    throw new Exception(ErrHistoryBooking);

                if ((dsHistoryBooking.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsHistoryBooking.Tables[0].Rows[0]["cErrMsg"].ToString());

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw new Exception(ex.Message);
            }
            return bResult;
        }
        public bool UpdateStatusEditBlokir(ApiMessage<AuthBookingEditUpdateStatusRq> request)
        {
            bool bResult = false;
            string QUpdateStatus = "", ErrUpdateStatus = "";
            DataSet dsUpdateStatus = new DataSet();

            try
            {
                QUpdateStatus = @"
                declare 
                @nBookingId				    int
                , @nTranTypeTrx2            int
                , @nNewSequenceNo           int
                , @cTranBranch              varchar(10)
                , @dBlockExpiry             datetime
                , @cTellerId                varchar(10)
                , @nSaldoMinBlokir          decimal(25,13)
                , @nBlokirAmountTrx2        decimal(25,13)
                , @nBlokirIdTrx2			int
                , @nTranIdTrx2				int
                
                @nBookingId				= " + request.Data.nBookingId + @"		
                , @nTranTypeTrx2        = " + request.Data.nTranTypeTrx2 + @"		
                , @nNewSequenceNo       = " + request.Data.nNewSequenceNo + @"
                , @cTranBranch          = '" + request.Data.cTranBranch + @"'
                , @dBlockExpiry         = '" + request.Data.dBlockExpiry + @"'
                , @cTellerId            = '" + request.Data.cTellerId + @"'
                , @nSaldoMinBlokir      = " + request.Data.nSaldoMinBlokir + @"
                , @nBlokirAmountTrx2    = " + request.Data.nBlokirAmountTrx2 + @"
                , @nBlokirIdTrx2		= " + request.Data.nBlokirIdTrx2 + @"
                , @nTranIdTrx2			= " + request.Data.nTranIdTrx2 + @"
               
                
                 update dbo.ReksaTranBlokirRelation_TM  
		         set StatusBlokir = 1,  
		          BlockSequence = @nNewSequenceNo,  
		          BlockBranch = @cTranBranch,   
		          BlockExpiry = @dBlockExpiry,  
		          TellerId = @cTellerId,  
		          SaldoMinBlokir = @nSaldoMinBlokir,  
		          BlokirAmount = @nBlokirAmountTrx2  
		         where BlokirId = @nBlokirIdTrx2  
  
		         if @nTranTypeTrx2 in (1,2,8)  
		         begin  
		         	Update ReksaTransaction_TT  
		         	set BlockSequence = @nNewSequenceNo  
		         	 , BlockBranch = @cTranBranch  
		         	where TranId = @nTranIdTrx2   
		         end      
		         else if @nTranTypeTrx2 in (5,6,9)  
		         begin  
		         	Update ReksaSwitchingTransaction_TM  
		         	set BlockSequence = @nNewSequenceNo  
		         	 , BlockBranch = @cTranBranch  
		         	where TranId = @nTranIdTrx2   
		         end  
		         else if @nTranTypeTrx2 in (0)  
		         begin  
		         	update dbo.ReksaBooking_TM  
		         	set BlockSequence = @nNewSequenceNo  
		         	 , BlockBranch = @cTranBranch  
		         	where BookingId = @nTranIdTrx2   
		         end 

                ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QUpdateStatus, out dsUpdateStatus, out ErrUpdateStatus))
                    throw new Exception(ErrUpdateStatus);

                if (!ErrUpdateStatus.EndsWith(""))
                    throw new Exception(ErrUpdateStatus);

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw new Exception(ex.Message);
            }
            return bResult;

        }
        public ApiMessage<DeleteBookingRs> DeleteBooking(ApiMessage<ReksaAuthorizeBookingRq> request)
        {
            ApiMessage<DeleteBookingRs> response = new ApiMessage<DeleteBookingRs>();
            response.Data = new DeleteBookingRs();
            string QDelete = "", ErrDelete = "";
            DataSet dsDelete = new DataSet();
            response.IsSuccess = true;
            try
            {
                QDelete = @"
            declare 
            @nBookingId					int
            , @mBookingAmt				decimal(25,13)    
            , @cBookingCode				varchar(10)   
            , @nSequenceNo				int        
            , @cBlockBranch				char(5)       
            , @cErrProviderCode			varchar(10)
            , @cErrMsg					varchar(800)    
            , @cDebitAccountId			varchar(19)    
            , @bBlokir				    bit  
            --20250303, Dimas Hadianto, RDN-1230, begin
            --, @cTellerId              varchar(5)
            , @cTellerId                varchar(10)
            --20250303, Dimas Hadianto, RDN-1230, end
            , @nTranIdTrx2              int 
            , @cTranCodeTrx2            varchar(20)
            , @nBlokirAmountTrx2        decimal(25,13)  
            , @nBlockBranchTrx2         varchar(5)  
            , @nBlockSequenceTrx2       int  
            , @nNewSequenceNo           int  
            , @nTranTypeTrx2            int  
            , @nBlokirIdTrx2            int  
            , @cBlockReasonTrx2         varchar(100)  

            
            set @nBookingId = '" + request.Data.nBookingId + @"'
  
            set @cTellerId = dbo.fnReksaGetParam('TELLERID')
            
            BEGIN TRY     
            -- cek apakah duit nasabah cukup atau ngga      
            select @mBookingAmt = BookingAmt, @cBookingCode = BookingCode      
              , @cDebitAccountId = NISPAccId      
              , @nSequenceNo = BlockSequence      
              , @cBlockBranch = BlockBranch      
             from ReksaBooking_TM      
             where BookingId = @nBookingId    
            
            If @@error != 0 or @@rowcount = 0      
            Begin      
            	set @cErrProviderCode = '200'     
            	set @cErrMsg = 'Data Booking tidak ditemukan!'     
            	raiserror(@cErrMsg, 16, 1)  
            End      

            if exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where TranId = @nBookingId and TranType in (0) and StatusBlokir = 1  
               and AccountNumber = @cDebitAccountId and BlockExpiry >= getdate()  
             )  
             begin  
                    --cek apakah pny transaksi lain di no rekening tsb (include switching juga walaupun ada di SP lain utk switching)  
                    select top 1   
                      @nTranIdTrx2 = TranId,  
                      @nTranTypeTrx2 = TranType,  
                      @nBlokirIdTrx2 = BlokirId,  
                      @cBlockReasonTrx2 = BlockReason,  
                      @nBlokirAmountTrx2 = BlokirAmount,   
                      @nBlockBranchTrx2 = BlockBranch,  
                      @nBlockSequenceTrx2 = BlockSequence  
                    from dbo.ReksaTranBlokirRelation_TM with (nolock)  
                    where AccountNumber = @cDebitAccountId and StatusBlokir = 0 and BlockExpiry >= getdate() and TranType in (1,2,5,6,8) and TranId != @nBookingId  
                    order by TranDate --ascending  
                    if @@rowcount > 0 set @bBlokir = 1  
             end  
                SELECT isnull(@mBookingAmt,0) mBookingAmt, isnull(@cBookingCode,'') cBookingCode, isnull(@cDebitAccountId,'') cDebitAccountId  
                , isnull(@nSequenceNo,0) nSequenceNo, isnull(@cBlockBranch,'') cBlockBranch, isnull(@cTellerId,'') cTellerId  
                , isnull(@bBlokir,0) bBlokir
                , isnull(@nTranIdTrx2,0) nTranIdTrx2, isnull(@nTranTypeTrx2,0) nTranTypeTrx2, isnull(@nBlokirIdTrx2,0) nBlokirIdTrx2 , isnull(@cBlockReasonTrx2,'') cBlockReasonTrx2  
	        	, isnull(@nBlokirAmountTrx2,0) nBlokirAmountTrx2, isnull(@nBlockBranchTrx2,'') nBlockBranchTrx2, isnull(@nBlockSequenceTrx2,0) nBlockSequenceTrx2 
                , isnull(@cErrProviderCode,'')  cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
            END TRY 
            BEGIN CATCH 
                 SELECT isnull(@cErrProviderCode,'')  cErrProviderCode, isnull(@cErrMsg,'') cErrMsg
            END CATCH
            
            ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QDelete, out dsDelete, out ErrDelete))
                    throw new Exception(ErrDelete);

                if (!ErrDelete.EndsWith(""))
                    throw new Exception(ErrDelete);

                if ((dsDelete.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsDelete.Tables[0].Rows[0]["cErrMsg"].ToString());

                if (dsDelete.Tables[0].Rows.Count > 0)
                {
                    response.Data.SequenceNo = Convert.ToInt32(dsDelete.Tables[0].Rows[0]["nSequenceNo"]);
                    response.Data.DebitAccountId = dsDelete.Tables[0].Rows[0]["cDebitAccountId"].ToString();
                    response.Data.BlockBranch = dsDelete.Tables[0].Rows[0]["cBlockBranch"].ToString();
                    response.Data.TellerId = dsDelete.Tables[0].Rows[0]["cTellerId"].ToString();
                    response.Data.BookingAmt = Convert.ToDecimal(dsDelete.Tables[0].Rows[0]["mBookingAmt"]);
                    response.Data.IsBlokir = Convert.ToBoolean(dsDelete.Tables[0].Rows[0]["bBlokir"]);
                    response.Data.BookingCode = dsDelete.Tables[0].Rows[0]["cBookingCode"].ToString();
                    response.Data.nTranIdTrx2 = Convert.ToInt32(dsDelete.Tables[0].Rows[0]["nTranIdTrx2"]);
                    response.Data.nTranTypeTrx2 = Convert.ToInt32(dsDelete.Tables[0].Rows[0]["nTranTypeTrx2"]);
                    response.Data.nBlokirIdTrx2 = Convert.ToInt32(dsDelete.Tables[0].Rows[0]["nBlokirIdTrx2"]);
                    response.Data.cBlockReasonTrx2 = dsDelete.Tables[0].Rows[0]["cBlockReasonTrx2"].ToString();
                    response.Data.nBlokirAmountTrx2 = Convert.ToDecimal(dsDelete.Tables[0].Rows[0]["nBlokirAmountTrx2"]);
                    response.Data.nBlockBranchTrx2 = dsDelete.Tables[0].Rows[0]["nBlockBranchTrx2"].ToString();
                    response.Data.nBlockSequenceTrx2 = Convert.ToInt32(dsDelete.Tables[0].Rows[0]["nBlockSequenceTrx2"]);
                }
                else
                {
                    throw new Exception("Gagal Ambil Data Booking AuthType 3");
                }

            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Data = null;
            }
            return response;

        }
        public bool UpdateStatusDeleteBooking(int nBookingId, string cNik)
        {
            bool bResult = false;
            string QEditRejectBooking = "", ErrEditRejectBooking = "";
            DataSet dsEditRejectBooking = new DataSet();

            try
            {
                QEditRejectBooking = @" 
                 declare 
                    @cNik			varchar(10)
                    , @nBookingId	int
                    , @cErrMsg		varchar(200)
                    
                    set @cNik		= '" +cNik+ @"'
                    set @nBookingId	= " + nBookingId + @"
                    
                    BEGIN TRY 
                                    
                    BEGIN TRAN UPDATEBOOKING
                    
                    Update ReksaBooking_TM      
                    set CheckerSuid = @cNik      
                    where BookingId = @nBookingId      
                        
                    if @@error != 0 or @@rowcount = 0      
                    Begin      
                    	set @cErrMsg = 'Gagal Otorisasi!'    
                        raiserror(@cErrMsg, 16, 1) 
                    End       
                          
                    Insert ReksaBooking_TH(BookingId, BookingCode, BookingCounter, BookingCCY, BookingAmt, ProdId      
                     , CIFNo, IsEmployee, AgentId, BankId, AccountType, NISPAccId, NonNISPAccId, NonNISPAccName      
                     , Referentor, AuthType, Status, HistoryStatus, LastUpdate, UserSuid, CheckerSuid      
                     , BlockSequence, BlockBranch, SubcFee, SubcFeeBased  
                     , TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased  
                     , Channel   
                     , CIFName, NISPAccName)      
                    select BookingId, BookingCode, BookingCounter, BookingCCY, BookingAmt, ProdId      
                     , CIFNo, IsEmployee, AgentId, BankId, AccountType, NISPAccId, NonNISPAccId, NonNISPAccName      
                     , Referentor, AuthType, Status, 'Deleted', LastUpdate, UserSuid, CheckerSuid      
                     , BlockSequence, BlockBranch, SubcFee, SubcFeeBased      
                     , TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased  
                     , Channel   
                     , CIFName, NISPAccName      
                    from ReksaBooking_TM      
                    where BookingId = @nBookingId      
                          
                    if @@error != 0 or @@rowcount = 0      
                    Begin      
                    	set @cErrMsg = 'Gagal Move ke History!'    
                        raiserror(@cErrMsg, 16, 1) 
                    End       
                          
                    Delete ReksaBooking_TM      
                    where BookingId = @nBookingId      
                          
                    if @@error != 0 or @@rowcount = 0      
                    Begin      
                    	set @cErrMsg = 'Gagal Delete!'   
                        raiserror(@cErrMsg, 16, 1) 
                    End 
                    
                    COMMIT TRAN UPDATEBOOKING
                                    
                    END TRY 
                    BEGIN CATCH
                        if @@trancount > 0
                    	    ROLLBACK TRAN UPDATEBOOKING
                    	select isnull(@cErrMsg,'') cErrMsg
                    END CATCH "; 
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QEditRejectBooking, out dsEditRejectBooking, out ErrEditRejectBooking))
                    throw new Exception(ErrEditRejectBooking);

                if (!ErrEditRejectBooking.EndsWith(""))
                    throw new Exception(ErrEditRejectBooking);

                if ((dsEditRejectBooking.Tables[0].Rows[0]["cErrMsg"].ToString() != ""))
                    throw new Exception(dsEditRejectBooking.Tables[0].Rows[0]["cErrMsg"].ToString());

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw new Exception(ex.Message);
            }
            return bResult;
        }
        public bool UpdateRekNasabah(int nBookingId, string cDebitAccountId, string cCIFNo)
        {
            bool bResult = false;
            string QUpdateRekNasabah = "", ErrUpdateRekNasabah = "";
            DataSet dsUpdateRekNasabah = new DataSet();

            try
            {
                QUpdateRekNasabah = @" 
                 declare
                 @nBookingId						int
                 , @pcRelationAccountNameNew		varchar(100)
                 , @cMCAllowed					char(1) 
                 , @cDebitAccountId				varchar(20)
                 , @cSelectedAccNo				varchar(20)
                 , @cBookingCCY					char(10)
                 , @cCIFNo						char(13)
                 
                 
                 set @nBookingId			= " + nBookingId + @"
                 set @cDebitAccountId	    = '" + cDebitAccountId + @"'
                 set @cCIFNo				 = '" + cCIFNo + @"'
                 
                 --update rek ke master nasabah
                 	if exists(select top 1 1 from dbo.ReksaBooking_TM with(nolock) where BookingId = @nBookingId) --approved 
                 	begin
                 		select @pcRelationAccountNameNew = ''
                 		, @cMCAllowed = 'N'
                 		select @pcRelationAccountNameNew = CFAAL1 
                 		from CFALTN 
                 		where CFAACT = @cDebitAccountId
                 
                 	if isnull(@pcRelationAccountNameNew, '') = ''
                 	begin
                 		select @pcRelationAccountNameNew = CFAAL1 
                 		from CFALTNNew_v
                 		where CFAACT = @cDebitAccountId
                 	end
                 	
                 	select @cSelectedAccNo = NISPAccId
                 	, @cBookingCCY = BookingCCY
                 	from dbo.ReksaBooking_TM with(nolock)
                 	where BookingId = @nBookingId     
                 
                 	if not exists(select top 1 1 from DDMAST_v where ACCTNO = @cSelectedAccNo)
                 		select @cMCAllowed = D2MULT 
                 		from DDTNEW_v dd 
                 			join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE
                 		where dd.ACCTNO = @cSelectedAccNo
                 	else
                 		select @cMCAllowed = D2MULT 
                 		from DDMAST_v dd 
                 			join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE
                 		where dd.ACCTNO = @cSelectedAccNo
                 
                 	if @cBookingCCY = 'IDR' and @cMCAllowed <> 'Y'
                 		begin   
                 			update dbo.ReksaMasterNasabah_TM
                 			set NISPAccountId = @cSelectedAccNo,
                 				NISPAccountName = @pcRelationAccountNameNew
                 			where CIFNo = @cCIFNo 
                 
                 			if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM with(nolock)
                 			where CIFNo = @cCIFNo and isnull(NISPAccountIdMC,'') != '')
                 			begin
                 				if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
                 				where CIFNo = @cCIFNo and b.ProdCCY <> 'IDR' and a.CIFStatus= 'A')
                 				begin
                 					if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM with(nolock)
                 					where CIFNo = @cCIFNo and isnull(NISPAccountIdUSD,'') = '')
                 					begin
                 						update dbo.ReksaMasterNasabah_TM
                 						set NISPAccountIdUSD = NISPAccountIdMC ,
                 						NISPAccountNameUSD = NISPAccountNameMC
                 						where CIFNo = @cCIFNo 
                 					end
                 				end
                 				update dbo.ReksaMasterNasabah_TM
                 				set NISPAccountIdMC = null,
                 					NISPAccountNameMC = null
                 				where CIFNo = @cCIFNo 
                 			end                     
                 		end
                 		else if @cBookingCCY = 'USD' and @cMCAllowed <> 'Y'
                 		begin
                 			update dbo.ReksaMasterNasabah_TM
                 			set NISPAccountIdUSD = @cSelectedAccNo,
                 				NISPAccountNameUSD = @pcRelationAccountNameNew
                 			where CIFNo = @cCIFNo
                 
                 			if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
                 			where CIFNo = @cCIFNo and b.ProdCCY = 'IDR' and a.CIFStatus= 'A')
                 			begin
                 				if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM
                 				where CIFNo = @cCIFNo and isnull(NISPAccountId,'') = '')
                 				begin
                 					update dbo.ReksaMasterNasabah_TM
                 					set NISPAccountId = NISPAccountIdMC ,
                 					NISPAccountName = NISPAccountNameMC
                 					where CIFNo = @cCIFNo 
                 				end
                 			end
                 
                 			if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
                 			where CIFNo = @cCIFNo and isnull(NISPAccountIdMC,'') != '')
                 			begin
                 				update dbo.ReksaMasterNasabah_TM
                 				set NISPAccountIdMC = null,
                 					NISPAccountNameMC = null
                 				where CIFNo = @cCIFNo 
                 			end         
                 		end
                 		else if  @cMCAllowed = 'Y'
                 		begin
                 			update dbo.ReksaMasterNasabah_TM
                 			set NISPAccountIdMC = @cSelectedAccNo,
                 				NISPAccountNameMC = @pcRelationAccountNameNew
                 			where CIFNo = @cCIFNo 
                 
                 			if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
                 			where CIFNo = @cCIFNo and isnull(NISPAccountId,'') != '')
                 			begin
                 				update dbo.ReksaMasterNasabah_TM
                 				set NISPAccountId = null,
                 					NISPAccountName = null
                 			 where CIFNo = @cCIFNo 
                 			end
                 			if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
                 			where CIFNo = @cCIFNo and isnull(NISPAccountIdUSD,'') != '')
                 			begin
                 				update dbo.ReksaMasterNasabah_TM
                 				set NISPAccountIdUSD = null,
                 					NISPAccountNameUSD = null
                 				where CIFNo = @cCIFNo 
                 			end              
                 		end
                 	end
                  ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QUpdateRekNasabah, out dsUpdateRekNasabah, out ErrUpdateRekNasabah))
                    throw new Exception(ErrUpdateRekNasabah);

                if (!ErrUpdateRekNasabah.EndsWith(""))
                    throw new Exception(ErrUpdateRekNasabah);

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw new Exception(ex.Message);
            }
            return bResult;
        }

        public bool UpdateStatusTrxBooking(int nBookingId, bool bStatus)
        {
            bool bResult = false;
            int nStatus = 0;

            if (bStatus == true)
            {
                nStatus = 1;
            }
            else
            {
                nStatus = 2;
            }
            string QUpdateStatus = "", ErrUpdateStatus = "";
            DataSet dsUpdateStatus = new DataSet();

            try
            {
                QUpdateStatus = @"
                declare 
                @nBookingId				int
                , @nStatus              int
               
                
                set @nBookingId			= " + nBookingId + @"	
                set @nStatus			= " + nStatus + @"	

                if exists (select top 1 1 from ReksaBooking_TM where BookingId = @nBookingId)
                begin
                    Update ReksaBooking_TM      
                    set Status = @nStatus      
                    where BookingId = @nBookingId 
                end
                else
                begin
                    Update ReksaBooking_TH     
                    set Status = @nStatus      
                    where BookingId = @nBookingId 
                end
                ";
                if (!clsCallSPWs.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, QUpdateStatus, out dsUpdateStatus, out ErrUpdateStatus))
                    throw new Exception(ErrUpdateStatus);

                if (!ErrUpdateStatus.EndsWith(""))
                    throw new Exception(ErrUpdateStatus);

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw new Exception(ex.Message);
            }
            return bResult;

        }

    }
}