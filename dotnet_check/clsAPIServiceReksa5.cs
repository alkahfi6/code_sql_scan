using System;
using System.Data;
using System.Text;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NISPDataSourceNetCore.webservice.model;
using NISPDataSourceNetCore.webservice;
using NISPDataSourceNetCore.database;
using System.Globalization;
using RestSharp;
using static NISPDataSourceNetCore.database.EPV;
using static NISPDataSourceNetCore.database.SQLSPParameter;
using reksa_rdb_job.Service;
using reksa_rdb_job.Support;
using reksa_rdb_job.Model;
using System.Security.Cryptography;


namespace reksa_rdb_job.Services
{
    public class clsAPIService : IService
    {
        private IConfiguration _configuration;
        private ICommonService _common;
        private string _strConnSOA;
        //private string _strUrlWsPwd;
        private string _strApiUrlCoba;
        private bool _ignoreSSL;
        private EPVEnvironmentType _envType;
        private string _strConnReksa;
        private string _strUrlWsReksa;
        private string _apiGuid;
        private string _localDataDurationDays;
        private string _userNIK;
        private string _apiInquiryAccountURL;
        private string _apiBlockAccountURL;
        private string _apiReleaseAccountURL;
        private string _url_apiWealthTransactionBE;

        public clsAPIService(IConfiguration iconfiguration, GlobalVariabel globalVariabel, ICommonService commonService)
        {
            this._configuration = iconfiguration;
            this._strConnReksa = globalVariabel.ConnectionStringDBReksa;
            this._common = commonService;
            this._strConnSOA = globalVariabel.ConnectionStringDBSOA;
            this._envType = globalVariabel.EnvironmentType;
            this._ignoreSSL = globalVariabel.IgnoreSSL;
            this._strApiUrlCoba = globalVariabel.URLApiCoba;
            //this._strUrlWsPwd = globalVariabel.URLWsPwd;
            this._strUrlWsReksa = globalVariabel.URLWsReksa;
            this._url_apiWealthTransactionBE = globalVariabel.URLApiWealthTransactionBE;
            this._localDataDurationDays = globalVariabel.LocalDataDurationDays;
            this._apiGuid = globalVariabel.ApiGuid;
            this._userNIK = _configuration["userNIK"];
            this._apiInquiryAccountURL = globalVariabel.APIInquiryAccountURL;
            this._apiBlockAccountURL = globalVariabel.APIBlockAccountURL;
            this._apiReleaseAccountURL = globalVariabel.APIReleaseAccountURL;
        }
        public string GetMethodName()
        {
            return new StackTrace(1).GetFrame(0).GetMethod().ReflectedType.Name + "." + new StackTrace(1).GetFrame(0).GetMethod().Name;
        }

        #region Populate RDB Index Fund
        public bool PopulateRDBIndexFund()
        {
            DataSet dsDataOut = new DataSet();

            bool isSuccess = false;
            string errMsg = "";
            
            String sqlCommand = "";

            sqlCommand = @"
                            declare 
	                            @dTranDate datetime       
	                            , @cTypeCodeIndexFund varchar(10)
	                            , @cErrMsg varchar(100)
	                            , @dCurrentWorkingDate datetime 
	                            , @dEffGetDate datetime

                            update control_table
                            set rdb_index_fund_status = 1

                            select @cTypeCodeIndexFund = ParamValue from dbo.ReksaParam_TR where ParamCode = 'INDEXFUND'      

                            if exists(select top 1 1 from dbo.control_table where isnull(rdb_index_fund_status,0) = 0)          
                            begin        
	                            set @cErrMsg = 'stop cyclic'         
	                            goto RETRY_END        
                            end 

                            select @dTranDate = current_working_date from control_table      
                            select @dCurrentWorkingDate = current_working_date from dbo.fnGetWorkingDate()     

                            --update skip kalau holiday
                            select @dEffGetDate = dbo.fnReksaGetEffectiveDate(getdate(), 0)

                            update sched
                            set sched.StatusId = 99
	                            , LastAttemptDate = getdate()
	                            , ErrorDescription = 'skip schedule pendebetan karena hari libur'
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where rrsc.FreqDebetMethod = 'D'
	                            and datediff(d, sched.ScheduledDate, dbo.fnReksaGetEffectiveDate(sched.ScheduledDate, 0)) > 0
	                            and StatusId = 0 
	                            and datediff(d, sched.ScheduledDate, @dEffGetDate) > 0
	                            and rt.TypeCode = @cTypeCodeIndexFund

                            if (convert(varchar(8),@dCurrentWorkingDate,112) <> convert(varchar(8),@dTranDate,112))    
                            begin                
	                            set @cErrMsg = 'stop cyclic'
	                            goto RETRY_END
                            end 

                            create table #tunggak (TranId int, ClientId int, MaxTunggak int)

                            -- add tunggak untuk monthly
                            insert into #tunggak
                            select distinct sched.TranId, sched.ClientId, frek.MaxTunggak
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            left join ReksaRegulerSubscriptionClient_TM rrsc 
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaRegulerSubscription_TR par 
	                            on rrsc.ProdId = par.ProductId
                            left join ReksaFrekPendebetanParam_TR frek 
	                            on par.MonthlyParamCode = frek.DebetMethodCode
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where dateadd(m, rrsc.FreqDebet, sched.ScheduledDate) <= @dTranDate
	                            and isnull(rrsc.FreqDebetMethod, 'M') = 'M'  
	                            and sched.Type = 0      
	                            and sched.StatusId = 3 -- gagal    
	                            and rt.TypeCode = @cTypeCodeIndexFund
                            group by sched.TranId, sched.ClientId, frek.MaxTunggak
                            having count(*) >= isnull(frek.MaxTunggak,3) 

                            -- add tunggak untuk daily
                            insert into #tunggak
                            select distinct sched.TranId, sched.ClientId, frek.MaxTunggak
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            left join ReksaRegulerSubscriptionClient_TM rrsc 
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaRegulerSubscription_TR par 
	                            on rrsc.ProdId = par.ProductId
                            left join ReksaFrekPendebetanParam_TR frek 
	                            on par.DailyParamCode = frek.DebetMethodCode
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where dateadd(day, rrsc.FreqDebet, sched.ScheduledDate) <= @dTranDate
	                            and rrsc.FreqDebetMethod = 'D'  
	                            and sched.Type = 0      
	                            and sched.StatusId = 3 -- gagal  
	                            and rt.TypeCode = @cTypeCodeIndexFund 
                            group by sched.TranId, sched.ClientId, frek.MaxTunggak
                            having count(*) >= isnull(frek.MaxTunggak,90) 

                            -- add tunggak untuk weekly
                            insert into #tunggak
                            select distinct sched.TranId, sched.ClientId, frek.MaxTunggak
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            left join ReksaRegulerSubscriptionClient_TM rrsc 
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaRegulerSubscription_TR par 
	                            on rrsc.ProdId = par.ProductId
                            left join ReksaFrekPendebetanParam_TR frek 
	                            on par.WeeklyParamCode = frek.DebetMethodCode
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where dateadd(week, rrsc.FreqDebet, sched.ScheduledDate) <= @dTranDate
	                            and rrsc.FreqDebetMethod = 'W'  
	                            and sched.Type = 0      
	                            and sched.StatusId = 3 -- gagal    
	                            and rt.TypeCode = @cTypeCodeIndexFund
                            group by sched.TranId, sched.ClientId, frek.MaxTunggak
                            having count(*) >= isnull(frek.MaxTunggak,12)

                            --hentikan proses auto debet jika sudah menunggak >= 3 x      
                            update rr      
                            set StatusId = 5, --terminated      
	                            LastAttemptDate = getdate()       
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join #tunggak tu      
	                            on tu.TranId = rr.TranId      
		                            and tu.ClientId = rr.ClientId
                            where rr.StatusId in (0) 

                            --stop yang sudah pernah gagal tunggak, dianggap terminated 
                            update rr      
                            set StatusId = 6,      
	                            LastAttemptDate = getdate()       
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join #tunggak tu      
	                            on tu.TranId = rr.TranId   
		                            and tu.ClientId = rr.ClientId
                            where rr.StatusId in (3)   

                            --ambil data-data reguler subscription yg uda pernah trx      
                            select rr.TranId,  rr.ScheduledDate, rr.StatusId, rr.LastAttemptDate, rt.TranType, rt.ClientId, rr.NAVValueDate      
                            into #tempTrx      
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join dbo.ReksaTransaction_TH rt      
	                            on rr.TranId = rt.TranId   
                            join ReksaProduct_TM rp
	                            on rt. ProdId = rp.ProdId
                            join ReksaType_TR rtp
	                            on rp.TypeId = rtp.TypeId  
                            where rr.ScheduledDate <= @dTranDate      
                                and rr.StatusId = 3      
                                and rr.[Type] = 0  
	                            and rtp.TypeCode = @cTypeCodeIndexFund    
                            union all      
                            select rr.TranId,  rr.ScheduledDate, rr.StatusId, rr.LastAttemptDate, rt.TranType, rt.ClientId, rr.NAVValueDate      
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join dbo.ReksaTransaction_TT rt      
	                            on rr.TranId = rt.TranId  
                            join ReksaProduct_TM rp
	                            on rt. ProdId = rp.ProdId
                            join ReksaType_TR rtp
	                            on rp.TypeId = rtp.TypeId
                            where rr.ScheduledDate <= @dTranDate      
                                and rr.StatusId = 3      
                                and rr.[Type] = 0 
	                            and rtp.TypeCode = @cTypeCodeIndexFund

                            --ambil data-data reguler subscription yg uda redempt all      
                            select distinct th.ClientId, th.TranId      
                            into #tempException      
                            from #tempTrx th      
                            join dbo.ReksaTransaction_TH rt      
	                            on th.ClientId = rt.ClientId      
                            where rt.TranType = 4      
                            union all      
                            select distinct th.ClientId, th.TranId      
                            from #tempTrx th      
                            join dbo.ReksaTransaction_TT rt      
	                            on th.ClientId = rt.ClientId      
                            where rt.TranType = 4    
  
                            update rr      
                            set StatusId = 4      
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join #tempException te      
                                on te.TranId = rr.TranId    
		                            and te.ClientId = rr.ClientId
                            where rr.StatusId = 3  

                            --clear data sebelumnya
                            truncate table ReksaRegulerSubscriptionSchedule_TMP

                            --insert data untuk diproses
                            insert into ReksaRegulerSubscriptionSchedule_TMP
                            (
	                            RegulerSubscriptionTranId, TranId, ScheduledDate
	                            , Type, TranAmount, ClientId, StatusId
                            )
                            select sched.RegulerSubscriptionTranId, sched.TranId, sched.ScheduledDate
	                            , sched.Type, sched.TranAmount, sched.ClientId, sched.StatusId            
                            from ReksaRegulerSubscriptionSchedule_TT sched  
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId      
                            where sched.ScheduledDate <= @dTranDate    
	                            and convert(varchar(8),@dCurrentWorkingDate,112) = convert(varchar(8),@dTranDate,112)            
	                            and sched.TranId not in (select TranId from #tempException)        
	                            and ((sched.StatusId = 0 and sched.LastAttemptDate is null)       
	                            or (sched.StatusId = 3 and (convert(varchar,LastAttemptDate,112) < @dCurrentWorkingDate)))   
	                            and rt.TypeCode = @cTypeCodeIndexFund 

                            delete cur
                            from ReksaRegulerSubscriptionSchedule_TMP cur 
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on cur.ClientId = rrsc.ClientId
                            where rrsc.[Status] <> 1 -- yang ga aktif jgn diambil (0 : new input, 1 : approve, 2 : reject)

                            delete cur
                            from ReksaRegulerSubscriptionSchedule_TMP cur 
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on cur.ClientId = rrsc.ClientId
                            where cur.StatusId = 3 and rrsc.FreqDebetMethod = 'D' -- kalau Daily yang gagal debet, tidak diretry lagi
	                            and datediff(d, ScheduledDate, @dTranDate) <> 0 -- yg gagal debet hari ini diretry

                            --stop autoredemp jika autoredemp = 0      
                            delete cr      
                            from ReksaRegulerSubscriptionSchedule_TMP cr      
                            join dbo.ReksaTransaction_TT tt      
	                            on cr.TranId = tt.TranId      
                            where tt.TranType = 8      
	                            and tt.CheckerSuid != 777      
	                            and cr.Type = 1      
	                            and isnull(tt.AutoRedemption, 0) = 0      

                            delete cr      
                            from ReksaRegulerSubscriptionSchedule_TMP cr      
                            join dbo.ReksaTransaction_TH tt      
	                            on cr.TranId = tt.TranId      
                            where tt.TranType = 8      
	                            and tt.CheckerSuid != 777      
	                            and cr.Type = 1      
	                            and isnull(tt.AutoRedemption, 0) = 0      

                            delete cr      
                            from ReksaRegulerSubscriptionSchedule_TMP cr      
                            join dbo.ReksaRegulerSubscriptionClient_TM rsc
	                            on cr.ClientId = rsc.ClientId       
                            where cr.Type = 1      
	                            and isnull(rsc.AutoRedemption, 0) = 0

                            select 'success' as responseMessage

                            RETRY_END:        
	                            select @cErrMsg as responseMessage
                        ";

            try
            {

                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);

                if (!errMsg.EndsWith(""))
                    throw new Exception(errMsg);

                if (
                    (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "stop cyclic") &&
                    (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "success")
                   )
                    throw new Exception(dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());

                isSuccess = true;

            }
            catch (Exception ex)
            {
                isSuccess = false;

                Console.WriteLine("[JOB][FAILED] [POPULATE RDB INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
        #endregion

        #region Populate RDB Non Index Fund
        public bool PopulateRDBNonIndexFund()
        {
            DataSet dsDataOut = new DataSet();
            bool isSuccess = false;
            string errMsg = "";
            String sqlCommand = "";
            sqlCommand = @"
                            declare 
	                            @dTranDate datetime       
	                            , @cTypeCodeIndexFund varchar(10)
	                            , @cErrMsg varchar(100)
	                            , @dCurrentWorkingDate datetime 
	                            , @dEffGetDate datetime
                            update control_table
                            set rdb_process_status = 1
                            select @cTypeCodeIndexFund = ParamValue from dbo.ReksaParam_TR where ParamCode = 'INDEXFUND'      
                            if exists(select top 1 1 from dbo.control_table where isnull(rdb_process_status, 0) = 0)          
                            begin        
	                            set @cErrMsg = 'stop cyclic'         
	                            goto RETRY_END        
                            end 
                            select @dTranDate = current_working_date from control_table      
                            select @dCurrentWorkingDate = current_working_date from dbo.fnGetWorkingDate()     
                            --update skip kalau holiday
                            select @dEffGetDate = dbo.fnReksaGetEffectiveDate(getdate(), 0)
                            update sched
                            set sched.StatusId = 99
	                            , LastAttemptDate = getdate()
	                            , ErrorDescription = 'skip schedule pendebetan karena hari libur'
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where rrsc.FreqDebetMethod = 'D'
	                            and datediff(d, sched.ScheduledDate, dbo.fnReksaGetEffectiveDate(sched.ScheduledDate, 0)) > 0
	                            and StatusId = 0 
	                            and datediff(d, sched.ScheduledDate, @dEffGetDate) > 0
	                            and rt.TypeCode <> @cTypeCodeIndexFund
                            --if (convert(varchar(8),@dCurrentWorkingDate,112) <> convert(varchar(8),@dTranDate,112))    
                            --begin                
	                        --    set @cErrMsg = 'stop cyclic'
	                        --    goto RETRY_END
                            --end 
                            create table #tunggak (TranId int, ClientId int, MaxTunggak int)
                            -- add tunggak untuk monthly
                            insert into #tunggak
                            select distinct sched.TranId, sched.ClientId, frek.MaxTunggak
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            left join ReksaRegulerSubscriptionClient_TM rrsc 
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaRegulerSubscription_TR par 
	                            on rrsc.ProdId = par.ProductId
                            left join ReksaFrekPendebetanParam_TR frek 
	                            on par.MonthlyParamCode = frek.DebetMethodCode
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where dateadd(m, rrsc.FreqDebet, sched.ScheduledDate) <= @dTranDate
	                            and isnull(rrsc.FreqDebetMethod, 'M') = 'M'  
	                            and sched.Type = 0      
	                            and sched.StatusId = 3 -- gagal    
	                            and rt.TypeCode <> @cTypeCodeIndexFund
                            group by sched.TranId, sched.ClientId, frek.MaxTunggak
                            having count(*) >= isnull(frek.MaxTunggak,3) 
                            -- add tunggak untuk daily
                            insert into #tunggak
                            select distinct sched.TranId, sched.ClientId, frek.MaxTunggak
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            left join ReksaRegulerSubscriptionClient_TM rrsc 
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaRegulerSubscription_TR par 
	                            on rrsc.ProdId = par.ProductId
                            left join ReksaFrekPendebetanParam_TR frek 
	                            on par.DailyParamCode = frek.DebetMethodCode
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where dateadd(day, rrsc.FreqDebet, sched.ScheduledDate) <= @dTranDate
	                            and rrsc.FreqDebetMethod = 'D'  
	                            and sched.Type = 0      
	                            and sched.StatusId = 3 -- gagal  
	                            and rt.TypeCode <> @cTypeCodeIndexFund
                            group by sched.TranId, sched.ClientId, frek.MaxTunggak
                            having count(*) >= isnull(frek.MaxTunggak,90) 
                            -- add tunggak untuk weekly
                            insert into #tunggak
                            select distinct sched.TranId, sched.ClientId, frek.MaxTunggak
                            from dbo.ReksaRegulerSubscriptionSchedule_TT sched 
                            left join ReksaRegulerSubscriptionClient_TM rrsc 
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaRegulerSubscription_TR par 
	                            on rrsc.ProdId = par.ProductId
                            left join ReksaFrekPendebetanParam_TR frek 
	                            on par.WeeklyParamCode = frek.DebetMethodCode
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId
                            where dateadd(week, rrsc.FreqDebet, sched.ScheduledDate) <= @dTranDate
	                            and rrsc.FreqDebetMethod = 'W'  
	                            and sched.Type = 0      
	                            and sched.StatusId = 3 -- gagal    
	                            and rt.TypeCode <> @cTypeCodeIndexFund
                            group by sched.TranId, sched.ClientId, frek.MaxTunggak
                            having count(*) >= isnull(frek.MaxTunggak,12)
                            --hentikan proses auto debet jika sudah menunggak >= 3 x      
                            update rr      
                            set StatusId = 5, --terminated      
	                            LastAttemptDate = getdate()       
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join #tunggak tu      
	                            on tu.TranId = rr.TranId      
		                            and tu.ClientId = rr.ClientId
                            where rr.StatusId in (0) 
                            --stop yang sudah pernah gagal tunggak, dianggap terminated 
                            update rr      
                            set StatusId = 6,      
	                            LastAttemptDate = getdate()       
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join #tunggak tu      
	                            on tu.TranId = rr.TranId   
		                            and tu.ClientId = rr.ClientId
                            where rr.StatusId in (3)   
                            --ambil data-data reguler subscription yg uda pernah trx      
                            select rr.TranId,  rr.ScheduledDate, rr.StatusId, rr.LastAttemptDate, rt.TranType, rt.ClientId, rr.NAVValueDate      
                            into #tempTrx      
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join dbo.ReksaTransaction_TH rt      
	                            on rr.TranId = rt.TranId   
                            join ReksaProduct_TM rp
	                            on rt. ProdId = rp.ProdId
                            join ReksaType_TR rtp
	                            on rp.TypeId = rtp.TypeId  
                            where rr.ScheduledDate <= @dTranDate      
                                and rr.StatusId = 3      
                                and rr.[Type] = 0  
	                            and rtp.TypeCode <> @cTypeCodeIndexFund    
                            union all      
                            select rr.TranId,  rr.ScheduledDate, rr.StatusId, rr.LastAttemptDate, rt.TranType, rt.ClientId, rr.NAVValueDate      
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join dbo.ReksaTransaction_TT rt      
	                            on rr.TranId = rt.TranId  
                            join ReksaProduct_TM rp
	                            on rt. ProdId = rp.ProdId
                            join ReksaType_TR rtp
	                            on rp.TypeId = rtp.TypeId
                            where rr.ScheduledDate <= @dTranDate      
                                and rr.StatusId = 3      
                                and rr.[Type] = 0 
	                            and rtp.TypeCode <> @cTypeCodeIndexFund
                            --ambil data-data reguler subscription yg uda redempt all      
                            select distinct th.ClientId, th.TranId      
                            into #tempException      
                            from #tempTrx th      
                            join dbo.ReksaTransaction_TH rt      
	                            on th.ClientId = rt.ClientId      
                            where rt.TranType = 4      
                            union all      
                            select distinct th.ClientId, th.TranId      
                            from #tempTrx th      
                            join dbo.ReksaTransaction_TT rt      
	                            on th.ClientId = rt.ClientId      
                            where rt.TranType = 4    
  
                            update rr      
                            set StatusId = 4      
                            from dbo.ReksaRegulerSubscriptionSchedule_TT rr      
                            join #tempException te      
                                on te.TranId = rr.TranId    
		                            and te.ClientId = rr.ClientId
                            where rr.StatusId = 3  
                            --clear data sebelumnya
                            truncate table ReksaRegulerSubscriptionNonIndexFundSchedule_TMP
                            --insert data untuk diproses
                            insert into ReksaRegulerSubscriptionNonIndexFundSchedule_TMP
                            (
	                            RegulerSubscriptionTranId, TranId, ScheduledDate
	                            , Type, TranAmount, ClientId, StatusId
                            )
                            select sched.RegulerSubscriptionTranId, sched.TranId, sched.ScheduledDate
	                            , sched.Type, sched.TranAmount, sched.ClientId, sched.StatusId            
                            from ReksaRegulerSubscriptionSchedule_TT sched  
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on sched.ClientId = rrsc.ClientId
                            join ReksaProduct_TM rp
	                            on rrsc. ProdId = rp.ProdId
                            join ReksaType_TR rt
	                            on rp.TypeId = rt.TypeId      
                            where sched.ScheduledDate <= @dTranDate    
	                            and convert(varchar(8),@dCurrentWorkingDate,112) = convert(varchar(8),@dTranDate,112)            
	                            and sched.TranId not in (select TranId from #tempException)        
	                            and ((sched.StatusId = 0 and sched.LastAttemptDate is null)       
	                            or (sched.StatusId = 3 and (convert(varchar,LastAttemptDate,112) < @dCurrentWorkingDate)))   
	                            and rt.TypeCode not in (@cTypeCodeIndexFund) 
                                and rrsc.Status = 1
                            delete cur
                            from ReksaRegulerSubscriptionNonIndexFundSchedule_TMP cur 
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on cur.ClientId = rrsc.ClientId
                            where rrsc.[Status] <> 1 -- yang ga aktif jgn diambil (0 : new input, 1 : approve, 2 : reject)
                            delete cur
                            from ReksaRegulerSubscriptionNonIndexFundSchedule_TMP cur 
                            join ReksaRegulerSubscriptionClient_TM rrsc
	                            on cur.ClientId = rrsc.ClientId
                            where cur.StatusId = 3 and rrsc.FreqDebetMethod = 'D' -- kalau Daily yang gagal debet, tidak diretry lagi
	                            and datediff(d, ScheduledDate, @dTranDate) <> 0 -- yg gagal debet hari ini diretry
                            --stop autoredemp jika autoredemp = 0      
                            delete cr      
                            from ReksaRegulerSubscriptionNonIndexFundSchedule_TMP cr      
                            join dbo.ReksaTransaction_TT tt      
	                            on cr.TranId = tt.TranId      
                            where tt.TranType = 8      
	                            and tt.CheckerSuid != 777      
	                            and cr.Type = 1      
	                            and isnull(tt.AutoRedemption, 0) = 0      
                            delete cr      
                            from ReksaRegulerSubscriptionNonIndexFundSchedule_TMP cr      
                            join dbo.ReksaTransaction_TH tt      
	                            on cr.TranId = tt.TranId      
                            where tt.TranType = 8      
	                            and tt.CheckerSuid != 777      
	                            and cr.Type = 1      
	                            and isnull(tt.AutoRedemption, 0) = 0      
                            delete cr      
                            from ReksaRegulerSubscriptionNonIndexFundSchedule_TMP cr      
                            join dbo.ReksaRegulerSubscriptionClient_TM rsc
	                            on cr.ClientId = rsc.ClientId       
                            where cr.Type = 1      
	                            and isnull(rsc.AutoRedemption, 0) = 0
                            select 'success' as responseMessage
                            RETRY_END:        
	                            select @cErrMsg as responseMessage
                        ";
            try
            {
                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);
                if (!errMsg.EndsWith(""))
                    throw new Exception(errMsg);
                if (
                    (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "stop cyclic") &&
                    (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "success")
                   )
                    throw new Exception(dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                Console.WriteLine("[JOB][FAILED] [POPULATE RDB NON INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
        #endregion
        #region Get List RDB Index Fund
        public List<Int64> GetListRDBIndexFund()
        {
            string strErrMsg = "";
            DataSet dsDataOut = new DataSet();
            List<Int64> tmpList = new List<Int64>();

            try
            {
                string sqlCommand = "select RegulerSubscriptionTranId from ReksaRegulerSubscriptionSchedule_TMP with(nolock) where StatusId in (0,3)";

                if (clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsDataOut, out strErrMsg))
                {
                    if (dsDataOut.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow dr in dsDataOut.Tables[0].Rows)
                        {
                            tmpList.Add(Convert.ToInt64(dr["RegulerSubscriptionTranId"]));
                        }
                    }
                }
                else
                {
                    strErrMsg = "Gagal Ambil List RegulerSubscriptionTranId";
                    throw new Exception(strErrMsg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[JOB][FAILED] [GET LIST RDB INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return tmpList;
        }
        #endregion


        #region Get List RDB Non Index Fund
        public List<Int64> GetListRDBNonIndexFund()
        {
            string strErrMsg = "";
            DataSet dsDataOut = new DataSet();
            List<Int64> tmpList = new List<Int64>();
            try
            {
                string sqlCommand = "select RegulerSubscriptionTranId from ReksaRegulerSubscriptionNonIndexFundSchedule_TMP with(nolock) where StatusId in (0,3)";
                if (clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsDataOut, out strErrMsg))
                {
                    if (dsDataOut.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow dr in dsDataOut.Tables[0].Rows)
                        {
                            tmpList.Add(Convert.ToInt64(dr["RegulerSubscriptionTranId"]));
                        }
                    }
                }
                else
                {
                    strErrMsg = "Gagal Ambil List RegulerSubscriptionTranId";
                    throw new Exception(strErrMsg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[JOB][FAILED] [GET LIST RDB NON INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return tmpList;
        }
        #endregion

        #region Insert RDB Index Fund
        public bool InsertTrxRDBIndexFund(int RDBIndexFundId, out DataSet dsInputRDB)
        {
            DataSet dsDataOut = new DataSet();

            dsInputRDB = dsDataOut;

            bool isSuccess = false;
            string errMsg = "";
            
            String sqlCommand = "";

            sqlCommand = @"
                            declare
	                            @nTranId int, @dScheduledDate datetime, @nAmount money, @nTranIdNew int, @dTranDate datetime, @nProdId int       
	                            , @nClientId int, @nFundId int, @nAgentId int, @cTranCCY varchar(5), @nSubcFee money, @nRedempFee money       
	                            , @nNAV money, @dNAVValueDate datetime, @nKurs money, @cTranCode varchar(20), @nTranType int  
	                            , @nTranAmt decimal(25,13), @nTranUnit decimal(25,13), @nSubcFeeBased decimal(25,13), @nRedempFeeBased decimal(25,13)
	                            , @nUnitBalance decimal(25,13), @nUnitBalanceNom decimal(25,13), @nParamId int, @dProcessDate datetime      
	                            , @dSettleDate datetime, @nSettled bit, @dLastUpdate datetime, @nUserSuid int, @nCheckerSuid int, @nWMCheckerSuid int      
	                            , @nWMOtor int, @nReverseSuid int, @nStatus int, @nBillId int, @nByUnit int, @nBlockSequence int, @nFullAmount int      
	                            , @nSalesId int, @cBlockBranch varchar(10), @nRedempUnit  decimal(25,13), @nRedempDev  decimal(25,13)      
	                            , @nRedempDisc  decimal(25,13), @nDiscSuid int, @dDiscDate datetime, @nCancelSuid int, @dCancelDate datetime      
	                            , @dGFChangeDate datetime, @nGFSUid int, @dGoodFund datetime, @nExtStatus int, @nDocFCSubscriptionForm int      
	                            , @nDocFCDevidentAuthLetter int, @nDocFCJoinAcctStatementLetter int, @nDocFCIDCopy int, @nDocFCOthers int      
	                            , @nDocTCSubscriptionForm int, @nDocTCTermCondition int, @nDocTCProspectus int, @nDocTCFundFactSheet int      
	                            , @nDocTCOthers int, @nJangkaWaktu int, @dJatuhTempo datetime, @nAutoRedemption int, @cGiftCode varchar(10)      
	                            , @nBiayaHadiah decimal(25,13), @nRegSubscriptionFlag int, @nAsuransi int, @cWarnMsg varchar(100)  
	                            , @cFrekuensiPendebetan int, @cDocFCOthersList varchar(4000), @cDocTCOthersList varchar(4000)
	                            , @nTranIdRegulerSubscription int, @nOK int, @nError int, @nType int, @nMaxTunggak int, @nUnitRedemp decimal(25,13) 
	                            , @dCurrentWorkingDate datetime, @nUser int, @nProcID int, @cErrMsg varchar(100), @cClientCode char(11), @c3DigitClientCode char(3)                  
	                            , @c5DigitCounter char(5), @nCounter int, @cRefID varchar(20), @cCIFNo varchar(20), @cOfficeId varchar(5), @cNoRekening varchar(20)    
	                            , @cWarnMsg2 varchar(200), @cWarnMsg3 varchar(200), @bByPhoneOrder bit, @bIsFeeEdit bit, @bJenisPerhitunganFee bit, @dPercentageFee decimal(25,13)    
	                            , @nPeriod int, @nReferentor int, @bIsPartialMaturity bit, @bTrxTaxAmnesty bit, @cInitialTranCode varchar(8), @nMaxTunggakDaily	int
	                            , @nMaxTunggakWeekly int, @nStatusId int, @dEffGetDate datetime, @cTypeCodeIndexFund varchar(10)
                                --20250325, Dimas Hadianto, RDN-1230, begin
                                --, @cTellerId varchar(5)
                                , @cTellerId varchar(10)
                                --20250325, Dimas Hadianto, RDN-1230, end
	                            , @dBlockExpireDate datetime, @cProductCodeRek varchar(10), @cMCAllowed varchar(1), @cAccountType varchar(1), @mMinBalance money
								-- 20250102, Lely R, RDN-1208, begin
								, @cBasedCurrency varchar(5)
								-- 20250102, Lely R, RDN-1208, end

                            select @dCurrentWorkingDate = current_working_date from dbo.fnGetWorkingDate()   

                            --Start looping	  

                            select @nTranId = TranId, @dScheduledDate = ScheduledDate, @nType = Type,
                                @nAmount = TranAmount, @nClientId = ClientId, @nStatusId = StatusId
                            from ReksaRegulerSubscriptionSchedule_TMP with(nolock)
                            where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription

                            if (@nTranId = 0)
                            begin 
                                -- ambil dari ReksaRegulerSubscriptionClient_TM
                                select       
                                    @nProdId = ProdId   
                                    , @cInitialTranCode = TranCode  
                                    , @nFundId = FundId      
                                    , @nAgentId = AgentId      
                                    , @cTranCCY = TranCCY      
                                    , @nSubcFee = SubcFee      
                                    , @nRedempFee = 0 
                                    , @dNAVValueDate = @dCurrentWorkingDate         
                                    , @dTranDate = getdate() 
                                    , @nTranAmt = TranAmount          
                                    , @nSubcFee = SubcFee      
                                    , @dNAVValueDate = @dCurrentWorkingDate           
                                    , @nStatus = null      
                                    , @nBillId = null      
                                    , @nByUnit = ByUnit         
                                    , @nBlockSequence = null      
                                    , @nFullAmount = FullAmount           
                                    , @cBlockBranch = null           
                                    , @dGoodFund = @dCurrentWorkingDate     
                                    , @nExtStatus = ExtStatus      
                                    , @nDocFCSubscriptionForm = DocFCSubscriptionForm      
                                    , @nDocFCDevidentAuthLetter = DocFCDevidentAuthLetter      
                                    , @nDocFCJoinAcctStatementLetter = DocFCJoinAcctStatementLetter      
                                    , @nDocFCIDCopy = DocFCIDCopy      
                                    , @nDocFCOthers = DocFCOthers      
                                    , @nDocTCSubscriptionForm = DocTCSubscriptionForm      
                                    , @nDocTCTermCondition = DocTCTermCondition      
                                    , @nDocTCProspectus = DocTCProspectus      
                                    , @nDocTCFundFactSheet = DocTCFundFactSheet      
                                    , @nDocTCOthers = DocTCOthers      
                                    , @nJangkaWaktu = JangkaWaktu      
                                    , @dJatuhTempo= JatuhTempo      
                                    , @nAutoRedemption = AutoRedemption      
                                    , @cGiftCode = ''      
                                    , @nBiayaHadiah = 0
                                    , @nRegSubscriptionFlag = 1      
                                    , @nAsuransi = Asuransi
                                    , @cFrekuensiPendebetan = FreqDebet        
                                    , @bIsFeeEdit = IsFeeEdit    
                                    , @bJenisPerhitunganFee = JenisPerhitunganFee    
                                    , @dPercentageFee = PercentageFee    
                                    , @nReferentor = Referentor    
                                    , @cOfficeId = OfficeId  
                                from ReksaRegulerSubscriptionClient_TM       
                                where ClientId = @nClientId  
                            end
                            else
                            begin
                                select       
                                    @nProdId = ProdId       
                                    , @nClientId = ClientId      
                                    , @nFundId = FundId      
                                    , @nAgentId = AgentId      
                                    , @cTranCCY = TranCCY      
                                    , @nSubcFee = SubcFee      
                                    , @nRedempFee = 0           
                                    , @dNAVValueDate = @dCurrentWorkingDate                
                                    , @nKurs = Kurs                
                                    , @dTranDate = getdate()           
                                    , @nTranAmt = TranAmt      
                                    , @nTranUnit = TranUnit       
                                    , @nSubcFee = SubcFee      
                                    , @nRedempFee = RedempFee      
                                    , @nSubcFeeBased = SubcFeeBased      
                                    , @nRedempFeeBased = RedempFeeBased      
                                    , @nNAV = NAV        
                                    , @nKurs = Kurs      
                                    , @nUnitBalance = UnitBalance      
                                    , @nUnitBalanceNom = UnitBalanceNom      
                                    , @nParamId = ParamId      
                                    , @dProcessDate = ProcessDate      
                                    , @dSettleDate = SettleDate      
                                    , @nSettled = @nSettled      
                                    , @dLastUpdate = LastUpdate      
                                    , @nUserSuid = UserSuid      
                                    , @nCheckerSuid = CheckerSuid      
                                    , @nWMCheckerSuid = WMCheckerSuid      
                                    , @nWMOtor = WMOtor      
                                    , @nReverseSuid = ReverseSuid      
                                    , @nStatus = null      
                                    , @nBillId = null      
                                    , @nByUnit = ByUnit            
                                    , @nBlockSequence = null      
                                    , @nFullAmount = FullAmount      
                                    , @nSalesId = SalesId      
                                    , @cBlockBranch = null      
                                    , @nRedempUnit = RedempUnit      
                                    , @nRedempDev = RedempDev      
                                    , @nRedempDisc = RedempDisc      
                                    , @nDiscSuid = DiscSuid      
                                    , @dDiscDate = DiscDate      
                                    , @nCancelSuid = CancelSuid      
                                    , @dCancelDate = CancelDate      
                                    , @dGFChangeDate = GFChangeDate      
                                    , @nGFSUid = GFSUid          
                                    , @dGoodFund = @dCurrentWorkingDate      
                                    , @nExtStatus = ExtStatus      
                                    , @nDocFCSubscriptionForm = DocFCSubscriptionForm      
                                    , @nDocFCDevidentAuthLetter = DocFCDevidentAuthLetter      
                                    , @nDocFCJoinAcctStatementLetter = DocFCJoinAcctStatementLetter      
                                    , @nDocFCIDCopy = DocFCIDCopy      
                                    , @nDocFCOthers = DocFCOthers      
                                    , @nDocTCSubscriptionForm = DocTCSubscriptionForm      
                                    , @nDocTCTermCondition = DocTCTermCondition      
                                    , @nDocTCProspectus = DocTCProspectus      
                                    , @nDocTCFundFactSheet = DocTCFundFactSheet      
                                    , @nDocTCOthers = DocTCOthers      
                                    , @nJangkaWaktu = JangkaWaktu      
                                    , @dJatuhTempo= JatuhTempo      
                                    , @nAutoRedemption = AutoRedemption      
                                    , @cGiftCode = GiftCode      
                                    , @nBiayaHadiah = BiayaHadiah      
                                    , @nRegSubscriptionFlag = RegSubscriptionFlag      
                                    , @nAsuransi = Asuransi      
                                    , @cFrekuensiPendebetan = FrekPendebetan      
                                    , @bIsFeeEdit = IsFeeEdit    
                                    , @bJenisPerhitunganFee = JenisPerhitunganFee    
                                    , @dPercentageFee = PercentageFee    
                                    , @nReferentor = Referentor    
                                    , @cOfficeId = OfficeId
                                from ReksaTransaction_TT       
                                where TranId = @nTranId      

                                if @@rowcount = 0      
                                    select       
                                        @nProdId = ProdId       
                                        , @nClientId = ClientId      
                                        , @nFundId = FundId      
                                        , @nAgentId = AgentId      
                                        , @cTranCCY = TranCCY      
                                        , @nSubcFee = SubcFee      
                                        , @nRedempFee = 0       
                                        , @dNAVValueDate = @dCurrentWorkingDate      
                                        , @nKurs = Kurs           
                                        , @dTranDate = getdate()      
                                        , @nTranAmt = TranAmt      
                                        , @nTranUnit = TranUnit       
                                        , @nSubcFee = SubcFee      
                                        , @nRedempFee = RedempFee      
                                        , @nSubcFeeBased = SubcFeeBased      
                                        , @nRedempFeeBased = RedempFeeBased      
                                        , @nNAV = NAV      
                                        , @nKurs = Kurs      
                                        , @nUnitBalance = UnitBalance      
                                        , @nUnitBalanceNom = UnitBalanceNom      
                                        , @nParamId = ParamId      
                                        , @dProcessDate = ProcessDate      
                                        , @dSettleDate = SettleDate      
                                        , @nSettled = @nSettled      
                                        , @dLastUpdate = LastUpdate      
                                        , @nUserSuid = UserSuid      
                                        , @nCheckerSuid = CheckerSuid      
                                        , @nWMCheckerSuid = WMCheckerSuid      
                                        , @nWMOtor = WMOtor      
                                        , @nReverseSuid = ReverseSuid      
                                        , @nStatus = null      
                                        , @nBillId = null      
                                        , @nByUnit = ByUnit      
                                        , @nBlockSequence = null      
                                        , @nFullAmount = FullAmount      
                                        , @nSalesId = SalesId      
                                        , @cBlockBranch = null      
                                        , @nRedempUnit = RedempUnit      
                                        , @nRedempDev = RedempDev      
                                        , @nRedempDisc = RedempDisc      
                                        , @nDiscSuid = DiscSuid      
                                        , @dDiscDate = DiscDate      
                                        , @nCancelSuid = CancelSuid      
                                        , @dCancelDate = CancelDate      
                                        , @dGFChangeDate = GFChangeDate      
                                        , @nGFSUid = GFSUid      
                                        , @dGoodFund = @dCurrentWorkingDate      
                                        , @nExtStatus = ExtStatus      
                                        , @nDocFCSubscriptionForm = DocFCSubscriptionForm      
                                        , @nDocFCDevidentAuthLetter = DocFCDevidentAuthLetter      
                                        , @nDocFCJoinAcctStatementLetter = DocFCJoinAcctStatementLetter      
                                        , @nDocFCIDCopy = DocFCIDCopy      
                                        , @nDocFCOthers = DocFCOthers      
                                        , @nDocTCSubscriptionForm = DocTCSubscriptionForm      
                                        , @nDocTCTermCondition = DocTCTermCondition      
                                        , @nDocTCProspectus = DocTCProspectus      
                                        , @nDocTCFundFactSheet = DocTCFundFactSheet      
                                        , @nDocTCOthers = DocTCOthers      
                                        , @nJangkaWaktu = JangkaWaktu      
                                        , @dJatuhTempo= JatuhTempo      
                                        , @nAutoRedemption = AutoRedemption      
                                        , @cGiftCode = GiftCode      
                                        , @nBiayaHadiah = BiayaHadiah      
                                        , @nRegSubscriptionFlag = RegSubscriptionFlag      
                                        , @nAsuransi = Asuransi          
                                        , @cFrekuensiPendebetan = FrekPendebetan      
                                        , @bIsFeeEdit = IsFeeEdit    
                                        , @bJenisPerhitunganFee = JenisPerhitunganFee    
                                        , @dPercentageFee = PercentageFee    
                                        , @nReferentor = Referentor    
                                        , @cOfficeId = OfficeId
                                    from ReksaTransaction_TH       
                                    where TranId = @nTranId      
                                end

                                if @nType = 1      
                                begin      
                                    select @nTranType = 4 --redemption all          
                                    select @nTranUnit = 0    
                                    select @nAmount = 0    

                                    exec dbo.ReksaGetLatestBalance @nClientId, 7, null, @nTranUnit output      

                                    select @nRedempFee = 0.0, @nRedempFeeBased = 0.0       

                                    if exists(select top 1 1 from ReksaRegulerSubscriptionSchedule_TT where TranId = @nTranId and StatusId <> 1 and Type = 0)      
                                    begin      
                                        update ReksaRegulerSubscriptionSchedule_TT      
                                            set StatusId = 3      
                                            , LastAttemptDate = getdate()         
                                            , ErrorDescription = 'Data tidak ditemukan di ReksaRegulerSubscriptionSchedule_TT'                 
                                        where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription         
                                    end

                                    select @nByUnit = 1      
                                    select @nUnitRedemp = @nTranUnit      
                                end      
                                else if @nType = 0      
                                begin   
                                    select @nTranType = 8 --reguler subscription      
                                    select @nUnitRedemp = 0      
                                end  

                                exec @nOK = ReksaGetLatestNAV @nProdId, 7, null, @nNAV output   

                                if @nOK <> 0 or @@error <> 0      
                                begin      
                                    update ReksaRegulerSubscriptionSchedule_TT      
                                    set StatusId = 3      
                                    , LastAttemptDate = getdate()         
                                    , ErrorDescription = 'Gagal exec ReksaGetLatestNAV'      
                                    where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription            
                                end

                                exec @nOK = ReksaGenerateRefID 'SUBSRDB', @cRefID output 

                                if @nOK <> 0 or @@error <> 0 or isnull(@cRefID,'') = ''     
                                begin    
                                    update ReksaRegulerSubscriptionSchedule_TT      
                                    set StatusId = 3      
                                    , LastAttemptDate = getdate()     
                                    , ErrorDescription = 'Gagal generate Ref ID'      
                                    where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription        
                                end  

                                begin try 

                                    select @cClientCode = ClientCode, @cCIFNo = CIFNo     
                                    from dbo.ReksaCIFData_TM                  
                                    where ClientId = @nClientId 

                                    select @cNoRekening = NISPAccountId    
                                    from dbo.ReksaMasterNasabah_TM    
                                    where CIFNo = @cCIFNo

                                    update dbo.ReksaClientCounter_TR                  
                                    set Trans = @nCounter
                                    , @nCounter = isnull(Trans, 0) + 1      
                                    where ProdId = @nProdId           

                                    set @c5DigitCounter = right ( ('00000' + convert(varchar, @nCounter)) , 5)                  
                                    set @c3DigitClientCode = left (@cClientCode, 3)                  
                                    set @cTranCode = @c3DigitClientCode + @c5DigitCounter       
                                    set @bByPhoneOrder = 0    
                                    set @nPeriod = 0    
                                    set @bIsPartialMaturity = 0    

                                    if (isnull(@nExtStatus, 0) = 74)
                                        set @bTrxTaxAmnesty = 1
                                    else
                                        set @bTrxTaxAmnesty = 0

                                    exec @nOK=[API_ReksaMaintainTransaksiNew]    
                                        @pnType = 1    
                                        , @pnTranType = @nTranType    
                                        , @pcTranCode = @cTranCode     
                                        , @pcRefID = @cRefID     
                                        , @pdTranDate = @dTranDate    
                                        , @pcCIFNo = @cCIFNo    
                                        , @pcOfficeId = @cOfficeId    
                                        , @pcNoRekening = @cNoRekening    
                                        , @pnProdId = @nProdId          
                                        , @pnClientId = @nClientId    
                                        , @pcClientCode = @cClientCode    
                                        , @pcTranCCY = @cTranCCY    
                                        , @pmTranAmt = @nAmount    
                                        , @pmTranUnit = @nUnitRedemp    
                                        , @pmUnitBalance = 0    
                                        , @pbFullAmount = @nFullAmount    
                                        , @pbByPhoneOrder = @bByPhoneOrder    
                                        , @pmSubcFee = @nSubcFee    
                                        , @pmRedempFee = @nRedempFee    
                                        , @pbIsFeeEdit = @bIsFeeEdit    
                                        , @pbJenisPerhitunganFee = @bJenisPerhitunganFee    
                                        , @pdPercentageFee = @dPercentageFee    
                                        , @pnPeriod = @nPeriod    
                                        , @pbByUnit = @nByUnit    
                                        , @pnJangkaWaktu = @nJangkaWaktu    
                                        , @pdJatuhTempo = @dJatuhTempo    
                                        , @pnAutoRedemption = @nAutoRedemption    
                                        , @pnAsuransi = @nAsuransi    
                                        , @pnFrekuensiPendebetan = @cFrekuensiPendebetan    
                                        , @pnRegTransactionNextPayment =  1      
                                        , @pcInputter = null    
                                        , @pnSeller = null    
                                        , @pnWaperd = null    
                                        , @pnNIK = 7    
                                        , @pnReferentor = @nReferentor            
                                        , @pcGuid = null    
                                        , @pbIsPartialMaturity = @bIsPartialMaturity    
                                        , @pcWarnMsg = @cWarnMsg output    
                                        , @pcWarnMsg2 =  @cWarnMsg2 output    
                                        , @pcWarnMsg3 =  @cWarnMsg3 output
                                        , @pbTrxTaxAmnesty = @bTrxTaxAmnesty	

                                        select @cTellerId = dbo.fnReksaGetParam('TELLERID')      

                                        select @nTranId = TranId, @cTranCode = TranCode, @nAmount = TranAmt, @cNoRekening = SelectedAccNo, @cOfficeId = OfficeId
                                            , @dBlockExpireDate = dateadd(dd, 3, NAVValueDate), @cTranCCY = TranCCY
                                        from ReksaTransaction_TT
                                        where RefID = @cRefID

                                        set @cNoRekening = right('000000000000' + @cNoRekening, 12)

                                        select @cProductCodeRek = SCCODE, @cAccountType = ACTYPE
                                        from DDTNEW_v
                                        where ACCTNO = @cNoRekening

                                        if isnull(@cProductCodeRek, '') = ''
                                        begin
	                                        select @cProductCodeRek = SCCODE, @cAccountType = ACTYPE
	                                        from DDMAST_v
	                                        where ACCTNO = @cNoRekening
                                        end

                                        if isnull(@cProductCodeRek, '') = ''
                                        begin
	                                        set @cErrMsg = 'TranID ' + convert(varchar(10), isnull(@nTranId,0)) + ' : Rekening tidak ditemukan!'

                                            update ReksaRegulerSubscriptionSchedule_TT      
                                            set StatusId = 3
                                                , LastAttemptDate = getdate()
                                                , ErrorDescription = @cErrMsg        
                                            where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription

                                            update ReksaTransaction_TT
                                            set Status = 2
                                                , CheckerSuid = 77777
                                            where TranId = @nTranId

                                            select @cErrMsg as responseMessage
                                        end
                                        else
                                        begin
                                            select @cMCAllowed = D2MULT, @mMinBalance = MINBLM
                                            from DDPAR2_v
                                            where SCCODE = @cProductCodeRek

                                            if @cMCAllowed = 'Y'            
                                            begin            
                                                set @cNoRekening = ltrim(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cTranCCY, 1) + ltrim(@cNoRekening))  
                                                
                                                select @mMinBalance = MINBLM    
                                                from SQL_SIBS.dbo.DDPAR2MC_v 
                                                where SCCODE = @cProductCodeRek
                                                    and DP2CUR = @cTranCCY   
                                            end

                                            if @mMinBalance is null          
                                                set @mMinBalance = 0 

                                            select @cBlockBranch = b.office_id_sibs        
                                            from user_nisp_v a 
                                            join office_information_all_v b        
	                                            on a.office_id = b.office_id        
                                            where a.nik = 777        
	                                            and isnull(ltrim(cost_centre_sibs),'') = ''

											-- 20250106, Lely R, RDN-1208, begin
                                            -- Get Base Currency
                                            select @cBasedCurrency = isnull(CFDCUR, 'IDR') 
                                            from dbo.CFAGRPMC_v 
                                            where convert (bigint, CFAGNO) = convert (bigint, @cNoRekening)
                                            -- Initialize Total Amount with Subscription Fee
                                            set @nAmount = @nAmount + isnull(@nSubcFee, 0)
                                            -- Check if Base Currency matches Transaction Currency
                                            if(isnull (@cBasedCurrency, 'IDR') = @cTranCCY)
                                            begin
                                                -- Check if Account has an Active Block
                                            -- 20250710, Andhika J, RDN-1208, begin
                                               -- if exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where AccountNumber = @cNoRekening and StatusBlokir = 1  
                                                if not exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where AccountNumber = @cNoRekening and StatusBlokir = 1  
                                            -- 20250710, Andhika J, RDN-1208, end             
                                                 and BlockExpiry >= getdate()                 
                                                 )        
                                                 begin
                                                    -- Add Minimal balance if Block Exists  
	                                    		    set @nAmount = @nAmount + isnull (@mMinBalance,0)
                                                 end  
                                                
    
                                            end
                                            -- 20250106, Lely R, RDN-1208, end
                                            -- 20250710, Andhika J, RDN-1208, begin
                                            --select @nTranId as 'TranId', @cTranCode as 'TranCode', (@nAmount + @mMinBalance + @nSubcFee)   as 'TranAmt', @cNoRekening as 'SelectedAccNo'
                                            select @nTranId as 'TranId', @cTranCode as 'TranCode', @nAmount  as 'TranAmt', @cNoRekening as 'SelectedAccNo'
                                            -- 20250710, Andhika J, RDN-1208, end
	                                            , @cOfficeId as 'OfficeId', convert(varchar(8), @dBlockExpireDate, 112) as 'BlockExpireDate', @cTranCCY as 'TranCCY'
                                                , @cTellerId as 'TellerID', '16' as 'ReasonCode', 'Subc Reksadana ' +  isnull(@cTranCode, '') as 'BlockRemark'
                                                , @cAccountType as 'AccountType', convert(varchar(8), getdate(), 112) as 'TranDate'
                                                , @mMinBalance as 'AccountMinBalance', @cBlockBranch as 'BlockBranch', 'Success' as responseMessage
                                        end
                                end try      
                                begin catch      
                                    update ReksaRegulerSubscriptionSchedule_TT      
                                    set StatusId = 3
                                        , LastAttemptDate = getdate()
                                        , ErrorDescription = error_message()        
                                    where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription   

                                    set @cErrMsg = 'Gagal Insert Data Transaksi RDB Index Fund'       

                                    select @cErrMsg as responseMessage
                                end catch  

                        ";

            try
            {
                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pnTranIdRegulerSubscription", RDBIndexFundId);

                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);

                if (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "Success")
                    throw new Exception(dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());

                dsInputRDB = dsDataOut;

                Console.WriteLine("[JOB][SUCCESS] [INSERT RDB INDEX FUND]" + dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());

                isSuccess = true;

            }
            catch (Exception ex)
            {
                isSuccess = false;

                Console.WriteLine("[JOB][FAILED] [INSERT RDB INDEX FUND]" + ex.Message.ToString());

                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
        #endregion
        #region Insert RDB Non Index Fund
        public bool InsertTrxRDBNonIndexFund(int RDBNonIndexFundId, out DataSet dsInputRDB)
        {
            DataSet dsDataOut = new DataSet();
            dsInputRDB = dsDataOut;
            bool isSuccess = false;
            string errMsg = "";
            String sqlCommand = "";
            sqlCommand = @"
                            declare
	                            @nTranId int, @dScheduledDate datetime, @nAmount money, @nTranIdNew int, @dTranDate datetime, @nProdId int       
	                            , @nClientId int, @nFundId int, @nAgentId int, @cTranCCY varchar(5), @nSubcFee money, @nRedempFee money       
	                            , @nNAV money, @dNAVValueDate datetime, @nKurs money, @cTranCode varchar(20), @nTranType int  
	                            , @nTranAmt decimal(25,13), @nTranUnit decimal(25,13), @nSubcFeeBased decimal(25,13), @nRedempFeeBased decimal(25,13)
	                            , @nUnitBalance decimal(25,13), @nUnitBalanceNom decimal(25,13), @nParamId int, @dProcessDate datetime      
	                            , @dSettleDate datetime, @nSettled bit, @dLastUpdate datetime, @nUserSuid int, @nCheckerSuid int, @nWMCheckerSuid int      
	                            , @nWMOtor int, @nReverseSuid int, @nStatus int, @nBillId int, @nByUnit int, @nBlockSequence int, @nFullAmount int      
	                            , @nSalesId int, @cBlockBranch varchar(10), @nRedempUnit  decimal(25,13), @nRedempDev  decimal(25,13)      
	                            , @nRedempDisc  decimal(25,13), @nDiscSuid int, @dDiscDate datetime, @nCancelSuid int, @dCancelDate datetime      
	                            , @dGFChangeDate datetime, @nGFSUid int, @dGoodFund datetime, @nExtStatus int, @nDocFCSubscriptionForm int      
	                            , @nDocFCDevidentAuthLetter int, @nDocFCJoinAcctStatementLetter int, @nDocFCIDCopy int, @nDocFCOthers int      
	                            , @nDocTCSubscriptionForm int, @nDocTCTermCondition int, @nDocTCProspectus int, @nDocTCFundFactSheet int      
	                            , @nDocTCOthers int, @nJangkaWaktu int, @dJatuhTempo datetime, @nAutoRedemption int, @cGiftCode varchar(10)      
	                            , @nBiayaHadiah decimal(25,13), @nRegSubscriptionFlag int, @nAsuransi int, @cWarnMsg varchar(100)  
	                            , @cFrekuensiPendebetan int, @cDocFCOthersList varchar(4000), @cDocTCOthersList varchar(4000)
	                            , @nTranIdRegulerSubscription int, @nOK int, @nError int, @nType int, @nMaxTunggak int, @nUnitRedemp decimal(25,13) 
	                            , @dCurrentWorkingDate datetime, @nUser int, @nProcID int, @cErrMsg varchar(100), @cClientCode char(11), @c3DigitClientCode char(3)                  
	                            , @c5DigitCounter char(5), @nCounter int, @cRefID varchar(20), @cCIFNo varchar(20), @cOfficeId varchar(5), @cNoRekening varchar(20)    
	                            , @cWarnMsg2 varchar(200), @cWarnMsg3 varchar(200), @bByPhoneOrder bit, @bIsFeeEdit bit, @bJenisPerhitunganFee bit, @dPercentageFee decimal(25,13)    
	                            , @nPeriod int, @nReferentor int, @bIsPartialMaturity bit, @bTrxTaxAmnesty bit, @cInitialTranCode varchar(8), @nMaxTunggakDaily	int
	                            , @nMaxTunggakWeekly int, @nStatusId int, @dEffGetDate datetime, @cTypeCodeIndexFund varchar(10)
                                --20250325, Dimas Hadianto, RDN-1230, begin
                                --, @cTellerId varchar(5)
                                , @cTellerId varchar(10)
                                --20250325, Dimas Hadianto, RDN-1230, end
	                            , @dBlockExpireDate datetime, @cProductCodeRek varchar(10), @cMCAllowed varchar(1), @cAccountType varchar(1), @mMinBalance money
                                , @pnTranIdRegulerSubscription int
								-- 20250102, Lely R, RDN-1208, begin
								, @cBasedCurrency varchar(5)
								-- 20250102, Lely R, RDN-1208, end
                            set @pnTranIdRegulerSubscription = '" + RDBNonIndexFundId + @"'
                            select @dCurrentWorkingDate = current_working_date from dbo.fnGetWorkingDate()   
                            --Start looping	  
                            select @nTranId = TranId, @dScheduledDate = ScheduledDate, @nType = Type,
                                @nAmount = TranAmount, @nClientId = ClientId, @nStatusId = StatusId
                            from ReksaRegulerSubscriptionNonIndexFundSchedule_TMP with(nolock)
                            where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription
                            if (@nTranId = 0)
                            begin 
                                -- ambil dari ReksaRegulerSubscriptionClient_TM
                                select       
                                    @nProdId = ProdId   
                                    , @cInitialTranCode = TranCode  
                                    , @nFundId = FundId      
                                    , @nAgentId = AgentId      
                                    , @cTranCCY = TranCCY      
                                    , @nSubcFee = SubcFee      
                                    , @nRedempFee = 0 
                                    , @dNAVValueDate = @dCurrentWorkingDate         
                                    , @dTranDate = getdate() 
                                    , @nTranAmt = TranAmount          
                                    , @nSubcFee = SubcFee      
                                    , @dNAVValueDate = @dCurrentWorkingDate           
                                    , @nStatus = null      
                                    , @nBillId = null      
                                    , @nByUnit = ByUnit         
                                    , @nBlockSequence = null      
                                    , @nFullAmount = FullAmount           
                                    , @cBlockBranch = null           
                                    , @dGoodFund = @dCurrentWorkingDate     
                                    , @nExtStatus = ExtStatus      
                                    , @nDocFCSubscriptionForm = DocFCSubscriptionForm      
                                    , @nDocFCDevidentAuthLetter = DocFCDevidentAuthLetter      
                                    , @nDocFCJoinAcctStatementLetter = DocFCJoinAcctStatementLetter      
                                    , @nDocFCIDCopy = DocFCIDCopy      
                                    , @nDocFCOthers = DocFCOthers      
                                    , @nDocTCSubscriptionForm = DocTCSubscriptionForm      
                                    , @nDocTCTermCondition = DocTCTermCondition      
                                    , @nDocTCProspectus = DocTCProspectus      
                                    , @nDocTCFundFactSheet = DocTCFundFactSheet      
                                    , @nDocTCOthers = DocTCOthers      
                                    , @nJangkaWaktu = JangkaWaktu      
                                    , @dJatuhTempo= JatuhTempo      
                                    , @nAutoRedemption = AutoRedemption      
                                    , @cGiftCode = ''      
                                    , @nBiayaHadiah = 0
                                    , @nRegSubscriptionFlag = 1      
                                    , @nAsuransi = Asuransi
                                    , @cFrekuensiPendebetan = FreqDebet        
                                    , @bIsFeeEdit = IsFeeEdit    
                                    , @bJenisPerhitunganFee = JenisPerhitunganFee    
                                    , @dPercentageFee = PercentageFee    
                                    , @nReferentor = Referentor    
                                    , @cOfficeId = OfficeId  
                                from ReksaRegulerSubscriptionClient_TM       
                                where ClientId = @nClientId  
                            end
                            else
                            begin
                                select       
                                    @nProdId = ProdId       
                                    , @nClientId = ClientId      
                                    , @nFundId = FundId      
                                    , @nAgentId = AgentId      
                                    , @cTranCCY = TranCCY      
                                    , @nSubcFee = SubcFee      
                                    , @nRedempFee = 0           
                                    , @dNAVValueDate = @dCurrentWorkingDate                
                                    , @nKurs = Kurs                
                                    , @dTranDate = getdate()           
                                    , @nTranAmt = TranAmt      
                                    , @nTranUnit = TranUnit       
                                    , @nSubcFee = SubcFee      
                                    , @nRedempFee = RedempFee      
                                    , @nSubcFeeBased = SubcFeeBased      
                                    , @nRedempFeeBased = RedempFeeBased      
                                    , @nNAV = NAV        
                                    , @nKurs = Kurs      
                                    , @nUnitBalance = UnitBalance      
                                    , @nUnitBalanceNom = UnitBalanceNom      
                                    , @nParamId = ParamId      
                                    , @dProcessDate = ProcessDate      
                                    , @dSettleDate = SettleDate      
                                    , @nSettled = @nSettled      
                                    , @dLastUpdate = LastUpdate      
                                    , @nUserSuid = UserSuid      
                                    , @nCheckerSuid = CheckerSuid      
                                    , @nWMCheckerSuid = WMCheckerSuid      
                                    , @nWMOtor = WMOtor      
                                    , @nReverseSuid = ReverseSuid      
                                    , @nStatus = null      
                                    , @nBillId = null      
                                    , @nByUnit = ByUnit            
                                    , @nBlockSequence = null      
                                    , @nFullAmount = FullAmount      
                                    , @nSalesId = SalesId      
                                    , @cBlockBranch = null      
                                    , @nRedempUnit = RedempUnit      
                                    , @nRedempDev = RedempDev      
                                    , @nRedempDisc = RedempDisc      
                                    , @nDiscSuid = DiscSuid      
                                    , @dDiscDate = DiscDate      
                                    , @nCancelSuid = CancelSuid      
                                    , @dCancelDate = CancelDate      
                                    , @dGFChangeDate = GFChangeDate      
                                    , @nGFSUid = GFSUid          
                                    , @dGoodFund = @dCurrentWorkingDate      
                                    , @nExtStatus = ExtStatus      
                                    , @nDocFCSubscriptionForm = DocFCSubscriptionForm      
                                    , @nDocFCDevidentAuthLetter = DocFCDevidentAuthLetter      
                                    , @nDocFCJoinAcctStatementLetter = DocFCJoinAcctStatementLetter      
                                    , @nDocFCIDCopy = DocFCIDCopy      
                                    , @nDocFCOthers = DocFCOthers      
                                    , @nDocTCSubscriptionForm = DocTCSubscriptionForm      
                                    , @nDocTCTermCondition = DocTCTermCondition      
                                    , @nDocTCProspectus = DocTCProspectus      
                                    , @nDocTCFundFactSheet = DocTCFundFactSheet      
                                    , @nDocTCOthers = DocTCOthers      
                                    , @nJangkaWaktu = JangkaWaktu      
                                    , @dJatuhTempo= JatuhTempo      
                                    , @nAutoRedemption = AutoRedemption      
                                    , @cGiftCode = GiftCode      
                                    , @nBiayaHadiah = BiayaHadiah      
                                    , @nRegSubscriptionFlag = RegSubscriptionFlag      
                                    , @nAsuransi = Asuransi      
                                    , @cFrekuensiPendebetan = FrekPendebetan      
                                    , @bIsFeeEdit = IsFeeEdit    
                                    , @bJenisPerhitunganFee = JenisPerhitunganFee    
                                    , @dPercentageFee = PercentageFee    
                                    , @nReferentor = Referentor    
                                    , @cOfficeId = OfficeId
                                from ReksaTransaction_TT       
                                where TranId = @nTranId      
                                if @@rowcount = 0      
                                    select       
                                        @nProdId = ProdId       
                                        , @nClientId = ClientId      
                                        , @nFundId = FundId      
                                        , @nAgentId = AgentId      
                                        , @cTranCCY = TranCCY      
                                        , @nSubcFee = SubcFee      
                                        , @nRedempFee = 0       
                                        , @dNAVValueDate = @dCurrentWorkingDate      
                                        , @nKurs = Kurs           
                                        , @dTranDate = getdate()      
                                        , @nTranAmt = TranAmt      
                                        , @nTranUnit = TranUnit       
                                        , @nSubcFee = SubcFee      
                                        , @nRedempFee = RedempFee      
                                        , @nSubcFeeBased = SubcFeeBased      
                                        , @nRedempFeeBased = RedempFeeBased      
                                        , @nNAV = NAV      
                                        , @nKurs = Kurs      
                                        , @nUnitBalance = UnitBalance      
                                        , @nUnitBalanceNom = UnitBalanceNom      
                                        , @nParamId = ParamId      
                                        , @dProcessDate = ProcessDate      
                                        , @dSettleDate = SettleDate      
                                        , @nSettled = @nSettled      
                                        , @dLastUpdate = LastUpdate      
                                        , @nUserSuid = UserSuid      
                                        , @nCheckerSuid = CheckerSuid      
                                        , @nWMCheckerSuid = WMCheckerSuid      
                                        , @nWMOtor = WMOtor      
                                        , @nReverseSuid = ReverseSuid      
                                        , @nStatus = null      
                                        , @nBillId = null      
                                        , @nByUnit = ByUnit      
                                        , @nBlockSequence = null      
                                        , @nFullAmount = FullAmount      
                                        , @nSalesId = SalesId      
                                        , @cBlockBranch = null      
                                        , @nRedempUnit = RedempUnit      
                                        , @nRedempDev = RedempDev      
                                        , @nRedempDisc = RedempDisc      
                                        , @nDiscSuid = DiscSuid      
                                        , @dDiscDate = DiscDate      
                                        , @nCancelSuid = CancelSuid      
                                        , @dCancelDate = CancelDate      
                                        , @dGFChangeDate = GFChangeDate      
                                        , @nGFSUid = GFSUid      
                                        , @dGoodFund = @dCurrentWorkingDate      
                                        , @nExtStatus = ExtStatus      
                                        , @nDocFCSubscriptionForm = DocFCSubscriptionForm      
                                        , @nDocFCDevidentAuthLetter = DocFCDevidentAuthLetter      
                                        , @nDocFCJoinAcctStatementLetter = DocFCJoinAcctStatementLetter      
                                        , @nDocFCIDCopy = DocFCIDCopy      
                                        , @nDocFCOthers = DocFCOthers      
                                        , @nDocTCSubscriptionForm = DocTCSubscriptionForm      
                                        , @nDocTCTermCondition = DocTCTermCondition      
                                        , @nDocTCProspectus = DocTCProspectus      
                                        , @nDocTCFundFactSheet = DocTCFundFactSheet      
                                        , @nDocTCOthers = DocTCOthers      
                                        , @nJangkaWaktu = JangkaWaktu      
                                        , @dJatuhTempo= JatuhTempo      
                                        , @nAutoRedemption = AutoRedemption      
                                        , @cGiftCode = GiftCode      
                                        , @nBiayaHadiah = BiayaHadiah      
                                        , @nRegSubscriptionFlag = RegSubscriptionFlag      
                                        , @nAsuransi = Asuransi          
                                        , @cFrekuensiPendebetan = FrekPendebetan      
                                        , @bIsFeeEdit = IsFeeEdit    
                                        , @bJenisPerhitunganFee = JenisPerhitunganFee    
                                        , @dPercentageFee = PercentageFee    
                                        , @nReferentor = Referentor    
                                        , @cOfficeId = OfficeId
                                    from ReksaTransaction_TH       
                                    where TranId = @nTranId      
                                end
                                if @nType = 1      
                                begin      
                                    select @nTranType = 4 --redemption all          
                                    select @nTranUnit = 0    
                                    select @nAmount = 0    
                                    exec dbo.ReksaGetLatestBalance @nClientId, 7, null, @nTranUnit output      
                                    select @nRedempFee = 0.0, @nRedempFeeBased = 0.0       
                                    if exists(select top 1 1 from ReksaRegulerSubscriptionSchedule_TT where TranId = @nTranId and StatusId <> 1 and Type = 0)      
                                    begin      
                                        update ReksaRegulerSubscriptionSchedule_TT      
                                            set StatusId = 3      
                                            , LastAttemptDate = getdate()         
                                            , ErrorDescription = 'Data tidak ditemukan di ReksaRegulerSubscriptionSchedule_TT'                 
                                        where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription         
                                    end
                                    select @nByUnit = 1      
                                    select @nUnitRedemp = @nTranUnit      
                                end      
                                else if @nType = 0      
                                begin   
                                    select @nTranType = 8 --reguler subscription      
                                    select @nUnitRedemp = 0      
                                end  
                                exec @nOK = ReksaGetLatestNAV @nProdId, 7, null, @nNAV output   
                                if @nOK <> 0 or @@error <> 0      
                                begin      
                                    update ReksaRegulerSubscriptionSchedule_TT      
                                    set StatusId = 3      
                                    , LastAttemptDate = getdate()         
                                    , ErrorDescription = 'Gagal exec ReksaGetLatestNAV'      
                                    where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription            
                                end
                                exec @nOK = ReksaGenerateRefID 'SUBSRDB', @cRefID output 
                                if @nOK <> 0 or @@error <> 0 or isnull(@cRefID,'') = ''     
                                begin    
                                    update ReksaRegulerSubscriptionSchedule_TT      
                                    set StatusId = 3      
                                    , LastAttemptDate = getdate()     
                                    , ErrorDescription = 'Gagal generate Ref ID'      
                                    where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription        
                                end  
                                begin try 
                                    select @cClientCode = ClientCode, @cCIFNo = CIFNo     
                                    from dbo.ReksaCIFData_TM                  
                                    where ClientId = @nClientId 
                                    select @cNoRekening = NISPAccountId    
                                    from dbo.ReksaMasterNasabah_TM    
                                    where CIFNo = @cCIFNo
                                    update dbo.ReksaClientCounter_TR                  
                                    set Trans = @nCounter
                                    , @nCounter = isnull(Trans, 0) + 1      
                                    where ProdId = @nProdId           
                                    set @c5DigitCounter = right ( ('00000' + convert(varchar, @nCounter)) , 5)                  
                                    set @c3DigitClientCode = left (@cClientCode, 3)                  
                                    set @cTranCode = @c3DigitClientCode + @c5DigitCounter       
                                    set @bByPhoneOrder = 0    
                                    set @nPeriod = 0    
                                    set @bIsPartialMaturity = 0    
                                    if (isnull(@nExtStatus, 0) = 74)
                                        set @bTrxTaxAmnesty = 1
                                    else
                                        set @bTrxTaxAmnesty = 0
                                    exec @nOK=[API_ReksaMaintainTransaksiNew]    
                                        @pnType = 1    
                                        , @pnTranType = @nTranType    
                                        , @pcTranCode = @cTranCode     
                                        , @pcRefID = @cRefID     
                                        , @pdTranDate = @dTranDate    
                                        , @pcCIFNo = @cCIFNo    
                                        , @pcOfficeId = @cOfficeId    
                                        , @pcNoRekening = @cNoRekening    
                                        , @pnProdId = @nProdId          
                                        , @pnClientId = @nClientId    
                                        , @pcClientCode = @cClientCode    
                                        , @pcTranCCY = @cTranCCY    
                                        , @pmTranAmt = @nAmount    
                                        , @pmTranUnit = @nUnitRedemp    
                                        , @pmUnitBalance = 0    
                                        , @pbFullAmount = @nFullAmount    
                                        , @pbByPhoneOrder = @bByPhoneOrder    
                                        , @pmSubcFee = @nSubcFee    
                                        , @pmRedempFee = @nRedempFee    
                                        , @pbIsFeeEdit = @bIsFeeEdit    
                                        , @pbJenisPerhitunganFee = @bJenisPerhitunganFee    
                                        , @pdPercentageFee = @dPercentageFee    
                                        , @pnPeriod = @nPeriod    
                                        , @pbByUnit = @nByUnit    
                                        , @pnJangkaWaktu = @nJangkaWaktu    
                                        , @pdJatuhTempo = @dJatuhTempo    
                                        , @pnAutoRedemption = @nAutoRedemption    
                                        , @pnAsuransi = @nAsuransi    
                                        , @pnFrekuensiPendebetan = @cFrekuensiPendebetan    
                                        , @pnRegTransactionNextPayment =  1      
                                        , @pcInputter = null    
                                        , @pnSeller = null    
                                        , @pnWaperd = null    
                                        , @pnNIK = 7    
                                        , @pnReferentor = @nReferentor            
                                        , @pcGuid = null    
                                        , @pbIsPartialMaturity = @bIsPartialMaturity    
                                        , @pcWarnMsg = @cWarnMsg output    
                                        , @pcWarnMsg2 =  @cWarnMsg2 output    
                                        , @pcWarnMsg3 =  @cWarnMsg3 output
                                        , @pbTrxTaxAmnesty = @bTrxTaxAmnesty	
                                        select @cTellerId = dbo.fnReksaGetParam('TELLERID')      
                                        select @nTranId = TranId, @cTranCode = TranCode, @nAmount = TranAmt, @cNoRekening = SelectedAccNo, @cOfficeId = OfficeId
                                            , @dBlockExpireDate = dateadd(dd, 3, NAVValueDate), @cTranCCY = TranCCY
                                        from ReksaTransaction_TT
                                        where RefID = @cRefID
                                        set @cNoRekening = right('000000000000' + @cNoRekening, 12)
                                        select @cProductCodeRek = SCCODE, @cAccountType = ACTYPE
                                        from DDTNEW_v
                                        where ACCTNO = @cNoRekening
                                        if isnull(@cProductCodeRek, '') = ''
                                        begin
	                                        select @cProductCodeRek = SCCODE, @cAccountType = ACTYPE
	                                        from DDMAST_v
	                                        where ACCTNO = @cNoRekening
                                        end
                                        if isnull(@cProductCodeRek, '') = ''
                                        begin
	                                        set @cErrMsg = 'TranID ' + convert(varchar(10), isnull(@nTranId,0)) + ' : Rekening tidak ditemukan!'
                                            update ReksaRegulerSubscriptionSchedule_TT      
                                            set StatusId = 3
                                                , LastAttemptDate = getdate()
                                                , ErrorDescription = @cErrMsg        
                                            where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription
                                            update ReksaTransaction_TT
                                            set Status = 2
                                                , CheckerSuid = 77777
                                            where TranId = @nTranId
                                            select @cErrMsg as responseMessage
                                        end
                                        else
                                        begin
                                            select @cMCAllowed = D2MULT, @mMinBalance = MINBLM
                                            from DDPAR2_v
                                            where SCCODE = @cProductCodeRek
                                            if @cMCAllowed = 'Y'            
                                            begin            
                                                set @cNoRekening = ltrim(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cTranCCY, 1) + ltrim(@cNoRekening))  
                                                select @mMinBalance = MINBLM    
                                                from SQL_SIBS.dbo.DDPAR2MC_v 
                                                where SCCODE = @cProductCodeRek
                                                    and DP2CUR = @cTranCCY   
                                            end
                                            if @mMinBalance is null          
                                                set @mMinBalance = 0 
                                            select @cBlockBranch = b.office_id_sibs        
                                            from user_nisp_v a 
                                            join office_information_all_v b        
	                                            on a.office_id = b.office_id        
                                            where a.nik = 777        
	                                            and isnull(ltrim(cost_centre_sibs),'') = ''
											-- 20250106, Lely R, RDN-1208, begin
                                            -- Get Base Currency
                                            select @cBasedCurrency = isnull(CFDCUR, 'IDR') 
                                            from dbo.CFAGRPMC_v 
                                            where convert (bigint, CFAGNO) = convert (bigint, @cNoRekening)
                                            -- Initialize Total Amount with Subscription Fee
                                            set @nAmount = @nAmount + isnull(@nSubcFee, 0)
                                            -- Check if Base Currency matches Transaction Currency
                                            if(isnull (@cBasedCurrency, 'IDR') = @cTranCCY)
                                            begin
                                                -- Check if Account has an Active Block
                                            -- 20250710, Andhika J, RDN-1208, begin
                                                --if exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where AccountNumber = @cNoRekening and StatusBlokir = 1 
                                                if not exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where AccountNumber = @cNoRekening and StatusBlokir = 1 
                                            -- 20250710, Andhika J, RDN-1208, end              
                                                 and BlockExpiry >= getdate()                 
                                                 )        
                                                 begin
                                                    -- Add Minimal balance if Block Exists  
	                                    		    set @nAmount = @nAmount + isnull (@mMinBalance,0)
                                                 end       
                                            end
                                            -- 20250106, Lely R, RDN-1208, end
                                            -- 20250710, Andhika J, RDN-1208, begin
                                            --select @nTranId as 'TranId', @cTranCode as 'TranCode', (@nAmount + @mMinBalance + @nSubcFee) as 'TranAmt', @cNoRekening as 'SelectedAccNo'
                                            select @nTranId as 'TranId', @cTranCode as 'TranCode', @nAmount as 'TranAmt', @cNoRekening as 'SelectedAccNo'
                                            -- 20250710, Andhika J, RDN-1208, end
	                                            , @cOfficeId as 'OfficeId', convert(varchar(8), @dBlockExpireDate, 112) as 'BlockExpireDate', @cTranCCY as 'TranCCY'
                                                , @cTellerId as 'TellerID', '16' as 'ReasonCode', 'Subc Reksadana ' +  isnull(@cTranCode, '') as 'BlockRemark'
                                                , @cAccountType as 'AccountType', convert(varchar(8), getdate(), 112) as 'TranDate'
                                                , @mMinBalance as 'AccountMinBalance', @cBlockBranch as 'BlockBranch', 'Success' as responseMessage
                                        end
                                end try      
                                begin catch      
                                    update ReksaRegulerSubscriptionSchedule_TT      
                                    set StatusId = 3
                                        , LastAttemptDate = getdate()
                                        , ErrorDescription = error_message()        
                                    where RegulerSubscriptionTranId = @pnTranIdRegulerSubscription   
                                    set @cErrMsg = 'Gagal Insert Data Transaksi RDB Index Fund'       
                                    select @cErrMsg as responseMessage
                                end catch  
                        ";
            try
            {
                SqlParameter[] sqlParam = new SqlParameter[1];
                sqlParam[0] = new SqlParameter("@pnTranIdRegulerSubscription", RDBNonIndexFundId);
                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);
                if (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "Success")
                    throw new Exception(dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());
                dsInputRDB = dsDataOut;
                Console.WriteLine("[JOB][SUCCESS] [INSERT RDB NON INDEX FUND]" + dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                Console.WriteLine("[JOB][FAILED] [INSERT RDB NON INDEX FUND]" + ex.Message.ToString());

                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
        #endregion

        #region InquiryAccount
        public ApiMessage<InquiryDetailRes> InquiryAccountLogic(ApiMessage<InquiryDetailReq> paramIn)
        {
            DataSet dsOut = new DataSet();

            String strErrMsg = "";
            ApiMessage<InquiryDetailRes> ApiMsgResponse = new ApiMessage<InquiryDetailRes>();

            try
            {
                string reqUri = this._apiInquiryAccountURL;
                InquiryDetailRes inquiryDetail = GetData<InquiryDetailRes>(reqUri, paramIn);

                InquiryDetailRes res = new InquiryDetailRes();
                try
                {
                    res.AccountType = inquiryDetail.AccountType;
                    res.AccountStatus = inquiryDetail.AccountStatus;
                    res.CurrencyType = inquiryDetail.CurrencyType;
                    res.AvailableBalance = inquiryDetail.AvailableBalance;
                }
                catch (Exception ex)
                {
                    strErrMsg = ex.Message;
                    ApiMsgResponse.ErrorDescription = strErrMsg;
                    ApiMsgResponse.IsSuccess = false;
                    ApiMsgResponse.IsResponseMessage = false;
                    ApiMsgResponse.MessageGUID = paramIn.MessageGUID;
                    ApiMsgResponse.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                    ApiMsgResponse.UserNIK = paramIn.UserNIK;

                    Console.WriteLine("[JOB][FAILED] [INQUIRY ACCOUNT]" + ex.Message.ToString());

                    //20241029, sandi, HTR-272, begin
                    throw new Exception("[JOB][FAILED] [INQUIRY ACCOUNT]" + ex.Message.ToString());
                    //20241029, sandi, HTR-272, end
                }

                ApiMsgResponse.Data = res;
                ApiMsgResponse.ErrorDescription = strErrMsg;
                ApiMsgResponse.IsSuccess = true;
                ApiMsgResponse.IsResponseMessage = true;
                ApiMsgResponse.MessageGUID = paramIn.MessageGUID;
                ApiMsgResponse.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ApiMsgResponse.UserNIK = paramIn.UserNIK;

                return ApiMsgResponse;

            }
            catch (Exception ex)
            {
                ApiMsgResponse.ErrorDescription = ex.Message;
                ApiMsgResponse.IsSuccess = false;
                ApiMsgResponse.IsResponseMessage = false;
                ApiMsgResponse.MessageGUID = paramIn.MessageGUID;
                ApiMsgResponse.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ApiMsgResponse.UserNIK = paramIn.UserNIK;

                Console.WriteLine("[JOB][FAILED] [INQUIRY ACCOUNT]" + ex.Message.ToString());

                return ApiMsgResponse;
            }

        }
        #endregion

        #region BlockAccount
        public ApiMessage<AddBlockAccountRes> BlockAccountLogic(ApiMessage<AddBlockAccountReq> paramIn)
        {
            DataSet dsOut = new DataSet();

            String strErrMsg = "";
            ApiMessage<AddBlockAccountRes> ApiMsgResBlock = new ApiMessage<AddBlockAccountRes>();

            try
            {
                string reqUri = this._apiBlockAccountURL;
                AddBlockAccountRes blockDetail = GetData<AddBlockAccountRes>(reqUri, paramIn);

                AddBlockAccountRes res = new AddBlockAccountRes();
                try
                {
                    res.Sequence = blockDetail.Sequence;
                }
                catch (Exception ex)
                {
                    strErrMsg = ex.Message;
                    ApiMsgResBlock.ErrorDescription = strErrMsg;
                    ApiMsgResBlock.IsSuccess = false;
                    ApiMsgResBlock.IsResponseMessage = false;
                    ApiMsgResBlock.MessageGUID = paramIn.MessageGUID;
                    ApiMsgResBlock.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                    ApiMsgResBlock.UserNIK = paramIn.UserNIK;

                    Console.WriteLine("[JOB][FAILED] [BLOCK ACCOUNT]" + ex.Message.ToString());
                }

                ApiMsgResBlock.Data = res;
                ApiMsgResBlock.ErrorDescription = strErrMsg;
                ApiMsgResBlock.IsSuccess = true;
                ApiMsgResBlock.IsResponseMessage = true;
                ApiMsgResBlock.MessageGUID = paramIn.MessageGUID;
                ApiMsgResBlock.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ApiMsgResBlock.UserNIK = paramIn.UserNIK;

                return ApiMsgResBlock;

            }
            catch (Exception ex)
            {
                ApiMsgResBlock.ErrorDescription = ex.Message;
                ApiMsgResBlock.IsSuccess = false;
                ApiMsgResBlock.IsResponseMessage = false;
                ApiMsgResBlock.MessageGUID = paramIn.MessageGUID;
                ApiMsgResBlock.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ApiMsgResBlock.UserNIK = paramIn.UserNIK;

                Console.WriteLine("[JOB][FAILED] [BLOCK ACCOUNT]" + ex.Message.ToString());

                return ApiMsgResBlock;
            }

        }
        #endregion

        #region Auth RDB Index Fund
        public bool AuthTrxRDBIndexFund(int RDBTranId, int TranId, int Accepted, int AuthNIK, decimal AccountMinBalance, int BlockSequence, decimal AvailableBalance)
        {
            DataSet dsDataOut = new DataSet();

            bool isSuccess = false;
            string errMsg = "";

            String sqlCommand = "";

            sqlCommand = @"
                            declare
	                            @pcTranCode char(8)		--> ex parameter SP
	                            , @nOK int        
	                            , @cErrMsg varchar(200)        
	                            , @dNAVValueDate datetime            
	                            , @nErrNo int        
	                            , @nStatus tinyint   
	                            , @nClientId int        
	                            , @cDebitAccountId varchar(19)        
	                            , @cTranBranch char(5)         
	                            , @cProductCode varchar(10)        
	                            , @cAccountType varchar(3)        
	                            , @cCurrencyType char(3)        
	                            , @cGuid uniqueidentifier            
	                            , @mClosingFee money        
	                            , @mTranAmt money        
	                            , @cTranCode varchar(8)        
	                            , @dBlockExpiry datetime        
	                            , @cCurrWorkingDate datetime        
	                            , @bMessageStatus bit        
	                            , @nTranType tinyint        
	                            , @nUserSuid int        
	                            , @nWMSuid int        
	                            , @bWMOtor bit        
	                            , @cBlockBranch char(5)        
	                            , @cBlockReason varchar(40)        
	                            , @cTranCCY char(3)        
	                            , @cMCAllowed char(1)        
	                            , @mBiayaHadiah money          
	                            --20250325, Dimas Hadianto, RDN-1230, begin
                                --, @cTellerId varchar(5)
                                , @cTellerId varchar(10)
                                --20250325, Dimas Hadianto, RDN-1230, end
	                            , @bRegSubs bit        
	                            , @nRegSubsTranId int        
	                            , @nRegSubsStatus int        
	                            , @mTotalPctFeeBased decimal(25,13)        
	                            , @mDefaultPctTaxFee decimal(25,13)        
	                            , @mSubTotalPctFeeBased decimal(25,13)        
	                            , @dTranDate datetime        
	                            , @nAmountBeforeFee decimal(25,13)        
	                            , @nFee decimal(25,13)        
	                            , @nSaldoMinBlokir decimal(25,13)        
	                            , @bIsBlokir bit        
	                            , @cDefaultCCY char(3)      
	                            , @nType int      
	                            , @mNewAmountWithoutFee decimal(25,13)       
	                            , @mNewFee decimal(25,13)      
	                            , @mNewBlokir decimal(25,13)      
	                            , @bBlokir bit        
	                            , @nTranIdTrx2 int        
	                            , @cTranCodeTrx2 varchar(20)        
	                            , @nBlokirAmountTrx2 decimal(25,13)        
	                            , @nBlockBranchTrx2 varchar(5)        
	                            , @nBlockSequenceTrx2 int        
	                            , @nNewSequenceNo int        
	                            , @nTranTypeTrx2 int        
	                            , @nBlokirIdTrx2 int        
	                            , @cBlockReasonTrx2 varchar(100)        
	                            , @nClientIdReject  int      
	                            , @pcRelationAccountNameNew varchar(100)
	                            , @pcCIFKey varchar(13)
	                            , @cSelectedAccNo varchar(20)
	                            , @cNewRelationAccount varchar(19)
	                            , @cOldRelationAccount varchar(19)
                                , @mMinBalance money
                                , @mAvailableBalance money
                                , @nNAV money 

                            set @mAvailableBalance = '" + AvailableBalance + @"'
                            set @mMinBalance = '" + AccountMinBalance + @"'

                            set @bRegSubs = 0               

                            select @cTellerId = dbo.fnReksaGetParam('TELLERID')        

                            select 
	                            @nUserSuid = UserSuid, @nWMSuid = isnull(WMCheckerSuid,0)        
	                            , @bWMOtor = WMOtor, @nTranType = TranType        
	                            , @dNAVValueDate = NAVValueDate
								, @nNAV = NAV        
	                            , @nType = AuthType      
                                -- 20250120, Lely R, RDN-1211, begin
				                , @cCurrencyType = TranCCY        
                                -- 20250120, Lely R, RDN-1211, end
                            from ReksaTransaction_TT        
                            where TranId = @nTranId  

                            if (@nUserSuid = @cNik) or (@nWMSuid = @cNik)        
                            begin        
	                            set @cErrMsg = 'Autorizer tidak boleh orang yang sama!'        
	                            goto ERROR        
                            end 

                            select @mDefaultPctTaxFee = PercentageTaxFeeDefault        
                            from dbo.control_table  
      
                            set @mSubTotalPctFeeBased = 100        
                            select @mTotalPctFeeBased = @mSubTotalPctFeeBased + @mDefaultPctTaxFee        

                            if not exists(        
	                            select * from dbo.ReksaTransaction_TT        
	                            where TranId = @nTranId        
	                            and CheckerSuid is null)        
                            begin       
	                            if not exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM
		                            where TranCode = @pcTranCode) 
	                            begin
		                            set @cErrMsg = 'Data Transaksi tidak ditemukan !'      
		                            goto ERROR
	                            end       
                            end 
   
                            create table #ReksaSellingFee_TM (        
	                            AgentId int        
	                            , ProdId int        
	                            , CCY char(3)        
	                            , Amount decimal(25,13)        
	                            , TransactionDate datetime        
	                            , ValueDate datetime        
	                            , Settled bit        
	                            , SettleDate datetime        
	                            , UserSuid int        
	                            , TotalAccount int        
	                            , TotalUnit decimal(25,13)        
	                            , TotalNominal decimal(25,13)        
	                            , PercentageSellingFeeBased decimal(25,13)        
	                            , PercentageTaxFeeBased decimal(25,13)        
	                            , PercentageFeeBased3 decimal(25,13)        
	                            , PercentageFeeBased4 decimal(25,13)        
	                            , PercentageFeeBased5 decimal(25,13)        
	                            , SellingFeeBased decimal(25,13)        
	                            , TaxFeeBased decimal(25,13)        
	                            , FeeBased3 decimal(25,13)        
	                            , FeeBased4 decimal(25,13)        
	                            , FeeBased5 decimal(25,13)        
	                            , TotalFeeBased decimal(25,13)         
	                            , SelisihFeeBased decimal(25,13)         
	                            , OfficeId varchar(5)         
                            )        

                            select @cCurrWorkingDate = current_working_date        
                            from control_table        

                            select @cTranBranch = b.office_id_sibs        
                            from user_nisp_v a 
                            join office_information_all_v b        
	                            on a.office_id = b.office_id        
                            where a.nik = @cNik        
	                            and isnull(ltrim(cost_centre_sibs),'') = ''

                            if @bAccepted = 1        
                            begin  
								set @nStatus = 1 

	                            if(@nType = 1)      
	                            begin      
		                            if @nTranType in (3,4)    
		                            begin
			                            update tt      
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId     
									                            when tt.TranCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != '' then rm.NISPAccountIdUSD     
									                            else rm.NISPAccountIdMC     
								                            end        
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId    
				                            and isnull(tt.ExtStatus,0) not in (74) 
				                            and isnull(tt.SelectedAccNo, '') = ''
			
			                            update tt      
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA     
									                            when tt.TranCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA     
									                            else rm.AccountIdMCTA     
								                            end        
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId    
				                            and isnull(tt.ExtStatus,0) in (74) 
				                            and isnull(tt.SelectedAccNo, '') = ''

			                            select @cDebitAccountId = SelectedAccNo      
			                            from dbo.ReksaTransaction_TT      
			                            where TranId = @nTranId           
			
			                            if isnull(@cDebitAccountId,'') = ''      
			                            begin      
				                            set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'        
				                            goto ERROR          
			                            end	
		                            end									
		
		                            if @nTranType in (8) -- blokir khusus subcription saja, kalo ngga perlu otorisasi WM        
		                            Begin       
			                            select @nClientId = ClientId        
				                            , @mTranAmt = TranAmt + case when FullAmount = 1 then SubcFee else 0 end        
				                            , @cTranCode = TranCode        
				                            , @nTranType = TranType        
				                            , @cBlockBranch = BlockBranch        
				                            , @cTranCCY = TranCCY        
				                            , @dTranDate = TranDate     
				                            , @nAmountBeforeFee = TranAmt        
				                            , @nFee = SubcFee        
			                            from ReksaTransaction_TT        
			                            where TranId = @nTranId        
  
			                            if @@error!= 0        
			                            begin         
				                            set @cErrMsg = 'Error ambil data!'   
				                            goto ERROR        
			                            end
			      
			                            select @cCurrWorkingDate = current_working_date        
			                            from control_table 
         
			                            select @cTranBranch = b.office_id_sibs        
			                            from user_nisp_v a 
			                            join office_information_all_v b        
				                            on a.office_id = b.office_id        
			                            where a.nik = @cNik        
				                            and isnull(ltrim(cost_centre_sibs),'') = '' 

			                            update tt      
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId     
									                            when tt.TranCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != '' then rm.NISPAccountIdUSD     
										                            else rm.NISPAccountIdMC     
								                            end     
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId           
				                            and isnull(tt.ExtStatus,0) not in (74)  
				                            and isnull(tt.SelectedAccNo, '') = ''

			                            update tt       
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA     
									                            when tt.TranCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA     
									                            else rm.AccountIdMCTA     
								                            end    
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId     
				                            and isnull(tt.ExtStatus,0) in (74)
				                            and isnull(tt.SelectedAccNo, '') = ''
			                            
			                            select @cDebitAccountId = SelectedAccNo      
			                            from dbo.ReksaTransaction_TT      
			                            where TranId = @nTranId     
										-- 20250710, Andhika J, RDN-1208, begin
                                        set @cDebitAccountId = right('000000000000' + @cDebitAccountId, 12)

                                        if isnull(@cMCAllowed, '') = ''
		                                begin
			                                if not exists(select top 1 1 from DDMAST_v where ACCTNO = @cDebitAccountId)
				                                select @cMCAllowed = D2MULT from DDTNEW_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cDebitAccountId
			                                else
				                                select @cMCAllowed = D2MULT from DDMAST_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cDebitAccountId
		                                end

                                        if(isnull(@cMCAllowed, '') = 'Y')
                                        begin
                                            set @cDebitAccountId = ltrim(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cTranCCY, 1) + ltrim(@cDebitAccountId)) 
                                        end
										-- 20250710, Andhika J, RDN-1208, end
			
			                            if isnull(@cDebitAccountId,'') = ''      
			                            begin      
				                            set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'        
				                            goto ERROR          
			                            end 

			                            --cek sudah pernah diblokir untuk rekening yg sama/ belum       
			                            if not exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where AccountNumber = @cDebitAccountId and StatusBlokir = 1               
				                            and BlockExpiry >= getdate()                
			                            )        
			                            begin        
				                            set @bIsBlokir = 1        
			                            end        
			                            else        
			                            begin        
				                            set @bIsBlokir = 0        
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
			
                                        -- 20250108, Lely R, RDN-1211, begin
			                            --if @mTranAmt > (@mAvailableBalance - @mMinBalance)                  
			                            --begin        
				                            --set @cErrMsg = 'Saldo Tidak Cukup untuk transaksi!'        
				                            --goto ERROR        
			                            --end        

			                            --select @cDefaultCCY = CFAGTY      
			                            --from dbo.CFAGRPMC_v      
			                            --where convert(bigint, CFAGNO) = convert(bigint, @cDebitAccountId) 
                                        -- 20250108, Lely R, RDN-1211, end

                                        set @cGuid = newid()     

			                            if @bIsBlokir = 0        
			                            begin        
				                            set @mMinBalance = 0        
				                            set @mClosingFee = 0        
			                            end

			                            select @nSaldoMinBlokir = @mMinBalance 

			                            set @mTranAmt = @mTranAmt + @mMinBalance 

			                            select @dBlockExpiry = dateadd(dd, 3, @cCurrWorkingDate)

			                            select @cBlockReason = 'Subc Reksadana '+ @cTranCode
                                        -- 20250108, Lely R, RDN-1211, begin
										-- 20250710, Andhika J, RDN-1208, begin
                                        --set @cDebitAccountId = ltrim(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cCurrencyType, 1) + ltrim(@cDebitAccountId))
										-- 20250710, Andhika J, RDN-1208, end
                                        -- 20250108, Lely R, RDN-1211, end

                                        if isnull(@nSequenceNo,0) != 0        
			                            begin             
				                            insert dbo.ReksaTranBlokirRelation_TM(TranId, TranType, AccountNumber, TranDate, NAVValueDate, TranCCY,        
				                            TranAmt, FeeAmt, SaldoMinBlokir,         
				                            BlokirAmount, BlockSequence, BlockBranch, BlockExpiry, TellerId, StatusBlokir, BlockReason        
				                            )        
				                            select @nTranId, @nTranType, @cDebitAccountId, @dTranDate, @dNAVValueDate, @cCurrencyType,        
				                            @nAmountBeforeFee, @nFee, @nSaldoMinBlokir,        
				                            @mTranAmt, @nSequenceNo, @cTranBranch, @dBlockExpiry, @cTellerId, @bIsBlokir, @cBlockReason        
			                            end
			
			                            insert #ReksaSellingFee_TM (AgentId, ProdId, CCY          
				                            , Amount, TransactionDate, ValueDate             
				                            , Settled, SettleDate, UserSuid, TotalAccount, TotalUnit, TotalNominal,    
				                            OfficeId    
			                            )    
			                            select rt.AgentId, rt.ProdId, rp.ProdCCY    
			                            , case 
				                            when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,sum(cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * rt.TranAmt as decimal(25,13))))          
				                            else dbo.fnReksaSetRounding(rt.ProdId,3,sum(cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * (rt.TranAmt - rt.SubcFee) as decimal(25,13))))        
			                            end                      
			                            , rt.TranDate, rt.NAVValueDate          
			                            , 0, NULL, 7,         
			                            count(*), dbo.fnReksaSetRounding(rt.ProdId,2,sum(cast(rt.TranAmt/rt.NAV as decimal(25,13)))),         
			                            case 
				                            when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,sum(rt.TranAmt))        
				                            else dbo.fnReksaSetRounding(rt.ProdId,3,sum(rt.TranAmt - rt.SubcFee))        
			                            end                   
			                            , rt.OfficeId                    
			                            from dbo.ReksaTransaction_TT rt         
			                            join dbo.ReksaProduct_TM rp          
				                            on rt.ProdId = rp.ProdId          
			                            join ReksaProductParam_TM rpp          
				                            on rp.ParamId = rpp.ParamId           
			                            join dbo.ReksaParamFee_TM rpf        
				                            on rpf.ProdId = rt.ProdId        
					                            and rpf.TrxType = 'SELLING'        
			                            where rt.TranId = @nTranId        
				                            and rpp.CloseEndBit = 0          
				                            and isnull(rpf.PctSellingUpfrontDefault,0) > 0          
				                            and rt.TranAmt > 0          
			                            group by rt.OfficeId, rt.AgentId, rt.ProdId, rp.ProdCCY, rt.TranDate, rt.NAVValueDate, rt.FullAmount        

			                            update pa        
			                            set PercentageSellingFeeBased = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl          
				                            on pa.ProdId = rl.ProdId         
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 1  

			                            update pa        
			                            set PercentageTaxFeeBased = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl              
				                            on pa.ProdId = rl.ProdId             
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 2   

			                            update pa        
			                            set PercentageFeeBased3 = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl          
				                            on pa.ProdId = rl.ProdId               
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 3      
        
			                            update pa        
			                            set PercentageFeeBased4 = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl        
				                            on pa.ProdId = rl.ProdId         
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 4   
           
			                            update pa        
			                            set PercentageFeeBased5 = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl               
				                            on pa.ProdId = rl.ProdId                
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 5 

			                            update #ReksaSellingFee_TM        
			                            set SellingFeeBased = cast(cast(PercentageSellingFeeBased/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))        
				                            , TaxFeeBased = cast(cast(PercentageTaxFeeBased/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))         
				                            , FeeBased3 = cast(cast(PercentageFeeBased3/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))          
				                            , FeeBased4 = cast(cast(PercentageFeeBased4/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))        
				                            , FeeBased5 = cast(cast(PercentageFeeBased5/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))  

			                            update #ReksaSellingFee_TM        
			                            set TotalFeeBased = isnull(SellingFeeBased, 0) +  isnull(TaxFeeBased, 0) + isnull(FeeBased3, 0) + isnull(FeeBased4, 0) + isnull(FeeBased5, 0)        
       
			                            update #ReksaSellingFee_TM        
			                            set SelisihFeeBased = isnull(Amount, 0) -  isnull(TotalFeeBased, 0)
        
			                            update #ReksaSellingFee_TM        
			                            set SellingFeeBased = isnull(SellingFeeBased, 0) +  isnull(SelisihFeeBased, 0) 
      
			                            update #ReksaSellingFee_TM        
			                            set TotalFeeBased = isnull(SellingFeeBased, 0) +  isnull(TaxFeeBased, 0) + isnull(FeeBased3, 0) + isnull(FeeBased4, 0) + isnull(FeeBased5, 0) 
			
			                            if not exists(select top 1 1 from dbo.ReksaSellingFee_TM where TransactionDate = @cCurrWorkingDate)          
			                            begin          
				                            insert dbo.ReksaSellingFee_TM (AgentId, ProdId, CCY          
					                            , Amount, TransactionDate, ValueDate          
					                            , Settled, SettleDate, UserSuid, TotalAccount, TotalUnit, TotalNominal        
					                            , SellingFeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased      
					                            , OfficeId     
					                            )        
				                            select AgentId, ProdId, CCY          
					                            , Amount, TransactionDate, ValueDate          
					                            , Settled, SettleDate, UserSuid, TotalAccount, TotalUnit, TotalNominal        
					                            , SellingFeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased       
					                            , OfficeId     
				                            from #ReksaSellingFee_TM        
			                            end 

			                            if not exists(select top 1 1 from dbo.ReksaSellingFeeDetail_TM where TransactionDate = @cCurrWorkingDate)          
			                            begin          
											insert dbo.ReksaSellingFeeDetail_TM (ClientId, AgentId, ProdId, CCY           
												, Amount, TransactionDate, ValueDate, UserSuid             
												, NAV, UnitBalance, SubcUnit)              
											select rt.ClientId, rt.AgentId, rt.ProdId, rp.ProdCCY          
												, case 
													when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * rt.TranAmt as decimal(25,13)))              
													else dbo.fnReksaSetRounding(rt.ProdId,3,cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * (rt.TranAmt - rt.SubcFee) as decimal(25,13)))        
												end                 
												, rt.TranDate, rt.NAVValueDate, 7, rt.NAV         
												, dbo.fnReksaSetRounding(rt.ProdId,2,cast(rt.TranAmt/rt.NAV as decimal(25,13)))         
												, case when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,rt.TranAmt)           
													else dbo.fnReksaSetRounding(rt.ProdId,3,(rt.TranAmt - rt.SubcFee))         
												end                        
											from dbo.ReksaTransaction_TT rt         
											join dbo.ReksaProduct_TM rp          
												on rt.ProdId = rp.ProdId          
											join ReksaProductParam_TM rpp          
												on rp.ParamId = rpp.ParamId           
											join dbo.ReksaParamFee_TM rpf        
												on rpf.ProdId = rt.ProdId        
													and rpf.TrxType = 'SELLING'        
											where rt.TranId = @nTranId        
												and rpp.CloseEndBit = 0          
												and isnull(rpf.PctSellingUpfrontDefault,0) > 0          
												and rt.TranAmt > 0     
										end      
  
			                            If @@error!= 0          
			                            Begin          
				                            set @cErrMsg = 'Gagal Insert Data Selling Fee!'          
				                            goto ERROR          
			                            end            
		                            end          
	                            end

                                if @nTranType = 4 -- redemption all khusus reg subs        
		                            and exists (select top 1 1        
		                            from ReksaTransaction_TT a         
		                            join ReksaRegulerSubscriptionClient_TM b        
		                            on a.ClientId = b.ClientId         
		                            and b.Status = 1        
		                            where a.TranId = @nTranId)              
	                            begin        
		                            set @bRegSubs = 1        
		
		                            select @nClientId = ClientId        
			                            , @mTranAmt = BiayaHadiah        
			                            , @cTranCode = TranCode        
			                            , @nTranType = TranType    
			                            , @cBlockBranch = BlockBranch        
			                            , @cTranCCY = TranCCY        
		                            from ReksaTransaction_TT        
		                            where TranId = @nTranId        
		
		                            if @@error!= 0        
		                            begin         
			                            set @cErrMsg = 'Error ambil data!'        
			                            goto ERROR        
		                            end 

		                            select @cCurrWorkingDate = current_working_date        
		                            from control_table        
		
		                            select @cTranBranch = b.office_id_sibs        
		                            from user_nisp_v a 
		                            join office_information_all_v b        
			                            on a.office_id = b.office_id        
		                            where a.nik = @cNik        
			                            and isnull(ltrim(cost_centre_sibs),'') = ''  
	
		                            update tt      
		                            set SelectedAccNo = case when tt.TranCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId     
								                            when tt.TranCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != '' then rm.NISPAccountIdUSD     
								                            else rm.NISPAccountIdMC     
							                            end            
		                            from dbo.ReksaTransaction_TT tt   
		                            join dbo.ReksaCIFData_TM rc      
			                            on tt.ClientId = rc.ClientId      
		                            join dbo.ReksaMasterNasabah_TM rm      
			                            on rm.CIFNo = rc.CIFNo      
		                            where tt.TranId = @nTranId    
			                            and isnull(tt.ExtStatus,0) not in (74)    
			                            and isnull(tt.SelectedAccNo, '') = ''

		                            update tt       
		                            set SelectedAccNo = case when tt.TranCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA     
								                            when tt.TranCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA     
								                            else rm.AccountIdMCTA     
							                            end          
		                            from dbo.ReksaTransaction_TT tt      
		                            join dbo.ReksaCIFData_TM rc      
			                            on tt.ClientId = rc.ClientId      
		                            join dbo.ReksaMasterNasabah_TM rm      
			                            on rm.CIFNo = rc.CIFNo      
		                            where tt.TranId = @nTranId    
			                            and isnull(tt.ExtStatus,0) in (74) 
			                            and isnull(tt.SelectedAccNo, '') = ''

		                            select @cDebitAccountId = SelectedAccNo      
		                            from dbo.ReksaTransaction_TT      
		                            where TranId = @nTranId  

		                            if isnull(@cDebitAccountId,'') = ''      
		                            begin      
			                            set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'        
			                            goto ERROR          
		                            end

                                    -- blokir rek bila saldo cukup      
		                            set @cGuid = newid() 

                                    --hapus sisa jadwal biar ga otomatis subscribe lagi        
		                            update ReksaRegulerSubscriptionSchedule_TT        
		                            set StatusId = 4 -- sudah redempt all        
		                            from ReksaRegulerSubscriptionSchedule_TT a        
		                            join ReksaTransaction_TH b        
			                            on a.TranId = b.TranId        
		                            where b.ClientId = @nClientId         
			                            and a.StatusId in (0,3)
			
		                            -- belum ada bill jadi coba batalkan transaksi reg subsnya 
		                            update ReksaTransaction_TT
		                            set Status = 2
		                            where ClientId = @nClientId 
			                            and TranType = 8         
			                            and NAVValueDate = @cCurrWorkingDate 
			                            and Status in (0, 1) 
			                            and BillId is null 

                                end        
	                            set @bRegSubs = 0         
                            end  

                            update dbo.ReksaTransaction_TT        
                            set CheckerSuid = @cNik        
	                            , Status = @nStatus        
	                            , BlockSequence = case 
						                            when TranType in (1,2,8) then @nSequenceNo         
						                            when TranType = 4 and @bRegSubs = 1 then @nSequenceNo         
						                            else 0 
					                            end        
					                            , BlockBranch = case 
						                            when TranType in (1,2,8) then @cTranBranch         
						                            when TranType = 4 and @bRegSubs = 1 then @cTranBranch         
						                            else '' 
					                            end              
					                            , LastUpdate = getdate()            
					                            , AuthType = case when @bAccepted = 1 then @nType else 1 end          
                            where TranId = @nTranId 
							
							update ReksaRegulerSubscriptionSchedule_TT      
							set StatusId = @nStatus,      
								LastAttemptDate = getdate()          
								, NAV = @nNAV      
								, NAVValueDate = @dNAVValueDate          
								, ErrorDescription = ''         
							where RegulerSubscriptionTranId = @nTranIdRegulerSubscription    
							    
							update ReksaRegulerSubscriptionSchedule_TMP      
							set StatusId = @nStatus,      
								LastAttemptDate = getdate()          
								, NAV = @nNAV      
								, NAVValueDate = @dNAVValueDate          
								, ErrorDescription = ''         
							where RegulerSubscriptionTranId = @nTranIdRegulerSubscription

                            if @bAccepted = 1
                            begin
	                            if exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM where TranCode = @pcTranCode)    
	                            begin
		                            update dbo.ReksaRegulerSubscriptionClient_TM
		                            set Status = 1
			                            , CheckerSuid = @cNik
		                            where TranCode = @pcTranCode 

		                            update tm 
		                            set tm.LastUser	= tm2.LastUser
			                            , tm.LastUpdate	= tm2.LastUpdate
			                            , tm.RefID	= tm2.RefID
			                            , tm.OfficeId	= tm2.OfficeId
			                            , tm.JoinDate	= tm2.JoinDate
			                            , tm.ProdId	= tm2.ProdId
			                            , tm.TranCode	= tm2.TranCode
			                            , tm.TranCCY	= tm2.TranCCY
			                            , tm.TranAmount	= tm2.TranAmount
			                            , tm.FullAmount	= tm2.FullAmount
			                            , tm.SubcFee	= tm2.SubcFee
			                            , tm.IsFeeEdit	= tm2.IsFeeEdit
			                            , tm.JenisPerhitunganFee	= tm2.JenisPerhitunganFee
			                            , tm.PercentageFee	= tm2.PercentageFee
			                            , tm.ByUnit	= tm2.ByUnit
			                            , tm.JangkaWaktu	= tm2.JangkaWaktu
			                            , tm.JatuhTempo	= tm2.JatuhTempo
			                            , tm.AutoRedemption	= tm2.AutoRedemption
			                            , tm.Asuransi	= tm2.Asuransi
			                            , tm.FreqDebetMethod	= tm2.FreqDebetMethod
			                            , tm.FreqDebet	= tm2.FreqDebet
			                            , tm.StartDebetDate	= tm2.StartDebetDate
			                            , tm.Referentor	= tm2.Referentor
			                            , tm.UserSuid	= tm2.UserSuid
			                            , tm.CheckerSuid	= tm2.CheckerSuid
			                            , tm.Inputter	= tm2.Inputter
			                            , tm.Seller	= tm2.Seller
			                            , tm.Waperd	= tm2.Waperd
			                            , tm.Channel	= tm2.Channel
			                            , tm.UserLoginIBMB	= tm2.UserLoginIBMB
			                            , tm.AuthType	= tm2.AuthType
			                            , tm.ExtStatus	= tm2.ExtStatus
			                            , tm.TranId	= tm2.TranId
			                            , tm.DocFCSubscriptionForm	= tm2.DocFCSubscriptionForm
			                            , tm.DocFCDevidentAuthLetter	= tm2.DocFCDevidentAuthLetter
			                            , tm.DocFCJoinAcctStatementLetter	= tm2.DocFCJoinAcctStatementLetter
			                            , tm.DocFCIDCopy	= tm2.DocFCIDCopy
			                            , tm.DocFCOthers	= tm2.DocFCOthers
			                            , tm.DocTCSubscriptionForm	= tm2.DocTCSubscriptionForm
			                            , tm.DocTCTermCondition	= tm2.DocTCTermCondition
			                            , tm.DocTCProspectus	= tm2.DocTCProspectus
			                            , tm.DocTCFundFactSheet	= tm2.DocTCFundFactSheet
			                            , tm.DocTCOthers	= tm2.DocTCOthers
			                            , tm.FundId	= tm2.FundId
			                            , tm.AgentId	= tm2.AgentId
			                            , tm.IsFutureRDB	= tm2.IsFutureRDB
		                            from dbo.ReksaRegulerSubscriptionClient_TM tm 
		                            join ReksaRegulerSubscriptionClient_TM tm2 
			                            on tm.TranCode = tm2.TranCode
		                            where tm2.TranCode = @pcTranCode and tm2.AuthType = 4
	                            end 

	                            if exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TMP where TranCode = @pcTranCode ) 
	                            begin
		                            update dbo.ReksaRegulerSubscriptionClient_TMP
		                            set Status = 1
		                            where TranCode = @pcTranCode and TranId = @nTranId
	                            end   
	 
	                            if exists (select top 1 1 from dbo.ReksaTransaction_TMP where TranCode = @pcTranCode ) 
	                            begin
		                            update dbo.ReksaTransaction_TMP
		                            set Status = 1
		                            where TranCode = @pcTranCode and TranId = @nTranId
	                            end
	
	                            --update rek ke master nasabah
	                            if exists(select top 1 1 from dbo.ReksaTransaction_TT with(nolock) where TranType IN (1,2,3,4, 8) 
		                            and TranId = @nTranId and isnull(RegSubscriptionFlag,0) <> 2 and Channel not in ('UPL')) -- bukan trx autodebet rdb dan upload PO
	                            begin	
		                            select @pcCIFKey = right('0000000000000' + convert(varchar,cif.CIFNo) ,13) 
		                            from dbo.ReksaTransaction_TT tt with(nolock) 
		                            join dbo.ReksaCIFData_TM cif with(nolock)
			                            on tt.ClientId = cif.ClientId
		                            where tt.TranId = @nTranId 

		                            select @pcRelationAccountNameNew = ''
		                            select @pcRelationAccountNameNew = CFAAL1 from CFALTN where CFAACT = @cDebitAccountId

		                            if isnull(@pcRelationAccountNameNew, '') = ''
		                            begin
			                            select @pcRelationAccountNameNew = CFAAL1 
			                            from CFALTNNew_v
			                            where CFAACT = @cDebitAccountId
		                            end 

		                            select @cSelectedAccNo = SelectedAccNo, @cTranCCY = TranCCY from dbo.ReksaTransaction_TT with(nolock) where TranId = @nTranId    

		                            if isnull(@cMCAllowed, '') = ''
		                            begin
			                            if not exists(select top 1 1 from DDMAST_v where ACCTNO = @cSelectedAccNo)
				                            select @cMCAllowed = D2MULT from DDTNEW_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cSelectedAccNo
			                            else
				                            select @cMCAllowed = D2MULT from DDMAST_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cSelectedAccNo
		                            end

		                            if @cTranCCY = 'IDR' and @cMCAllowed <> 'Y'
		                            begin   
			                            update dbo.ReksaMasterNasabah_TM
			                            set NISPAccountId = @cSelectedAccNo,
				                            NISPAccountName = @pcRelationAccountNameNew
			                            where CIFNo = @pcCIFKey 

			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM with(nolock)
				                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdMC,'') != '')
			                            begin
				                            if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
					                            where CIFNo = @pcCIFKey and b.ProdCCY <> 'IDR' and a.CIFStatus= 'A')
				                            begin
					                            if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM with(nolock)
						                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdUSD,'') = '')
					                            begin
						                            update dbo.ReksaMasterNasabah_TM
						                            set NISPAccountIdUSD = NISPAccountIdMC ,
						                            NISPAccountNameUSD = NISPAccountNameMC
						                            where CIFNo = @pcCIFKey 
					                            end
				                            end

				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountIdMC = null,
					                            NISPAccountNameMC = null
				                            where CIFNo = @pcCIFKey 
			                            end                     
		                            end
		                            else if @cTranCCY = 'USD' and @cMCAllowed <> 'Y'
		                            begin
			                            update dbo.ReksaMasterNasabah_TM
			                            set NISPAccountIdUSD = @cSelectedAccNo,
				                            NISPAccountNameUSD = @pcRelationAccountNameNew
			                            where CIFNo = @pcCIFKey

			                            if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
				                            where CIFNo = @pcCIFKey and b.ProdCCY = 'IDR' and a.CIFStatus= 'A')
			                            begin
				                            if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM
					                            where CIFNo = @pcCIFKey and isnull(NISPAccountId,'') = '')
				                            begin
					                            update dbo.ReksaMasterNasabah_TM
					                            set NISPAccountId = NISPAccountIdMC,
						                            NISPAccountName = NISPAccountNameMC
					                            where CIFNo = @pcCIFKey 
				                            end
			                            end

			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
			                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdMC,'') != '')
			                            begin
				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountIdMC = null,
					                            NISPAccountNameMC = null
				                            where CIFNo = @pcCIFKey 
			                            end         
		                            end
		                            else if  @cMCAllowed = 'Y'
		                            begin
			                            update dbo.ReksaMasterNasabah_TM
			                            set NISPAccountIdMC = @cSelectedAccNo,
				                            NISPAccountNameMC = @pcRelationAccountNameNew
			                            where CIFNo = @pcCIFKey 

			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
				                            where CIFNo = @pcCIFKey and isnull(NISPAccountId,'') != '')
			                            begin
				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountId = null,
					                            NISPAccountName = null
				                            where CIFNo = @pcCIFKey 
			                            end

			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
				                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdUSD,'') != '')
			                            begin
				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountIdUSD = null,
					                            NISPAccountNameUSD = null
				                            where CIFNo = @pcCIFKey 
			                            end 
		                            end
	                            end
                            end
   
                            drop table #ReksaSellingFee_TM  

                            ERROR:        
								if isnull(@cErrMsg, '') <> ''
								begin
									update dbo.ReksaTransaction_TT        
									set CheckerSuid = @cNik        
										, Status = 2         
									where TranId = @nTranId

									update ReksaRegulerSubscriptionSchedule_TMP      
									set StatusId = 3      
										, LastAttemptDate = getdate()   
										, ErrorDescription =  @cErrMsg   
									where RegulerSubscriptionTranId = @nTranIdRegulerSubscription

									update dbo.ReksaRegulerSubscriptionSchedule_TT
		                            set StatusId = 3
										, LastAttemptDate = getdate()
										, ErrorDescription =  @cErrMsg   
		                            where RegulerSubscriptionTranId = @nTranIdRegulerSubscription
								end

								select isnull(@cErrMsg, '') as responseMessage

                        ";
            try
            {
                SqlParameter[] sqlParam = new SqlParameter[5];
                sqlParam[0] = new SqlParameter("@nTranId", TranId);
                sqlParam[1] = new SqlParameter("@bAccepted", Accepted);
                sqlParam[2] = new SqlParameter("@cNik", AuthNIK);
                sqlParam[3] = new SqlParameter("@nSequenceNo", BlockSequence);
                sqlParam[4] = new SqlParameter("@nTranIdRegulerSubscription", RDBTranId);
                

                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);

                if (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "")
                    throw new Exception(dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());

                Console.WriteLine("[JOB][SUCCESS] [AUTH RDB INDEX FUND]" + dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());

                isSuccess = true;

            }
            catch (Exception ex)
            {
                isSuccess = false;

                Console.WriteLine("[JOB][FAILED] [AUTH RDB INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
        #endregion
        #region Auth RDB Non Index Fund
        public bool AuthTrxRDBNonIndexFund(int RDBTranId, int TranId, int Accepted, int AuthNIK, decimal AccountMinBalance, int BlockSequence, decimal AvailableBalance)
        {
            DataSet dsDataOut = new DataSet();
            bool isSuccess = false;
            string errMsg = "";
            String sqlCommand = "";
            sqlCommand = @"
                            declare
	                            @pcTranCode char(8)		--> ex parameter SP
	                            , @nOK int        
	                            , @cErrMsg varchar(200)        
	                            , @dNAVValueDate datetime            
	                            , @nErrNo int        
	                            , @nStatus tinyint   
	                            , @nClientId int        
	                            , @cDebitAccountId varchar(19)        
	                            , @cTranBranch char(5)         
	                            , @cProductCode varchar(10)        
	                            , @cAccountType varchar(3)        
	                            , @cCurrencyType char(3)        
	                            , @cGuid uniqueidentifier            
	                            , @mClosingFee money        
	                            , @mTranAmt money        
	                            , @cTranCode varchar(8)        
	                            , @dBlockExpiry datetime        
	                            , @cCurrWorkingDate datetime        
	                            , @bMessageStatus bit        
	                            , @nTranType tinyint        
	                            , @nUserSuid int        
	                            , @nWMSuid int        
	                            , @bWMOtor bit        
	                            , @cBlockBranch char(5)        
	                            , @cBlockReason varchar(40)        
	                            , @cTranCCY char(3)        
	                            , @cMCAllowed char(1)        
	                            , @mBiayaHadiah money          
	                            --20250325, Dimas Hadianto, RDN-1230, begin
                                --, @cTellerId varchar(5)
                                , @cTellerId varchar(10)
                                --20250325, Dimas Hadianto, RDN-1230, end
	                            , @bRegSubs bit        
	                            , @nRegSubsTranId int        
	                            , @nRegSubsStatus int        
	                            , @mTotalPctFeeBased decimal(25,13)        
	                            , @mDefaultPctTaxFee decimal(25,13)        
	                            , @mSubTotalPctFeeBased decimal(25,13)        
	                            , @dTranDate datetime        
	                            , @nAmountBeforeFee decimal(25,13)        
	                            , @nFee decimal(25,13)        
	                            , @nSaldoMinBlokir decimal(25,13)        
	                            , @bIsBlokir bit        
	                            , @cDefaultCCY char(3)      
	                            , @nType int      
	                            , @mNewAmountWithoutFee decimal(25,13)       
	                            , @mNewFee decimal(25,13)      
	                            , @mNewBlokir decimal(25,13)      
	                            , @bBlokir bit        
	                            , @nTranIdTrx2 int        
	                            , @cTranCodeTrx2 varchar(20)        
	                            , @nBlokirAmountTrx2 decimal(25,13)        
	                            , @nBlockBranchTrx2 varchar(5)        
	                            , @nBlockSequenceTrx2 int        
	                            , @nNewSequenceNo int        
	                            , @nTranTypeTrx2 int        
	                            , @nBlokirIdTrx2 int        
	                            , @cBlockReasonTrx2 varchar(100)        
	                            , @nClientIdReject  int      
	                            , @pcRelationAccountNameNew varchar(100)
	                            , @pcCIFKey varchar(13)
	                            , @cSelectedAccNo varchar(20)
	                            , @cNewRelationAccount varchar(19)
	                            , @cOldRelationAccount varchar(19)
                                , @mMinBalance money
                                , @mAvailableBalance money
                                , @nNAV money 
                            set @mAvailableBalance = '" + AvailableBalance + @"'
                            set @mMinBalance = '" + AccountMinBalance + @"'
                            set @bRegSubs = 0               
                            select @cTellerId = dbo.fnReksaGetParam('TELLERID')        
                            select 
	                            @nUserSuid = UserSuid, @nWMSuid = isnull(WMCheckerSuid,0)        
	                            , @bWMOtor = WMOtor, @nTranType = TranType        
	                            , @dNAVValueDate = NAVValueDate
								, @nNAV = NAV        
	                            , @nType = AuthType      
                                -- 20250120, Lely R, RDN-1211, begin
				                , @cCurrencyType = TranCCY        
                                -- 20250120, Lely R, RDN-1211, end
                            from ReksaTransaction_TT        
                            where TranId = @nTranId  
                            if (@nUserSuid = @cNik) or (@nWMSuid = @cNik)        
                            begin        
	                            set @cErrMsg = 'Autorizer tidak boleh orang yang sama!'        
	                            goto ERROR        
                            end 
                            select @mDefaultPctTaxFee = PercentageTaxFeeDefault        
                            from dbo.control_table  
                            set @mSubTotalPctFeeBased = 100        
                            select @mTotalPctFeeBased = @mSubTotalPctFeeBased + @mDefaultPctTaxFee        
                            if not exists(        
	                            select * from dbo.ReksaTransaction_TT        
	                            where TranId = @nTranId        
	                            and CheckerSuid is null)        
                            begin       
	                            if not exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM
		                            where TranCode = @pcTranCode) 
	                            begin
		                            set @cErrMsg = 'Data Transaksi tidak ditemukan !'      
		                            goto ERROR
	                            end       
                            end 
                            create table #ReksaSellingFee_TM (        
	                            AgentId int        
	                            , ProdId int        
	                            , CCY char(3)        
	                            , Amount decimal(25,13)        
	                            , TransactionDate datetime        
	                            , ValueDate datetime        
	                            , Settled bit        
	                            , SettleDate datetime        
	                            , UserSuid int        
	                            , TotalAccount int        
	                            , TotalUnit decimal(25,13)        
	                            , TotalNominal decimal(25,13)        
	                            , PercentageSellingFeeBased decimal(25,13)        
	                            , PercentageTaxFeeBased decimal(25,13)        
	                            , PercentageFeeBased3 decimal(25,13)        
	                            , PercentageFeeBased4 decimal(25,13)        
	                            , PercentageFeeBased5 decimal(25,13)        
	                            , SellingFeeBased decimal(25,13)        
	                            , TaxFeeBased decimal(25,13)        
	                            , FeeBased3 decimal(25,13)        
	                            , FeeBased4 decimal(25,13)        
	                            , FeeBased5 decimal(25,13)        
	                            , TotalFeeBased decimal(25,13)         
	                            , SelisihFeeBased decimal(25,13)         
	                            , OfficeId varchar(5)         
                            )        
                            select @cCurrWorkingDate = current_working_date        
                            from control_table        
                            select @cTranBranch = b.office_id_sibs        
                            from user_nisp_v a 
                            join office_information_all_v b        
	                            on a.office_id = b.office_id        
                            where a.nik = @cNik        
	                            and isnull(ltrim(cost_centre_sibs),'') = ''
                            if @bAccepted = 1        
                            begin  
								set @nStatus = 1 
	                            if(@nType = 1)      
	                            begin      
		                            if @nTranType in (3,4)    
		                            begin
			                            update tt      
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId     
									                            when tt.TranCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != '' then rm.NISPAccountIdUSD     
									                            else rm.NISPAccountIdMC     
								                            end        
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId    
				                            and isnull(tt.ExtStatus,0) not in (74) 
				                            and isnull(tt.SelectedAccNo, '') = ''
			                            update tt      
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA     
									                            when tt.TranCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA     
									                            else rm.AccountIdMCTA     
								                            end        
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId    
				                            and isnull(tt.ExtStatus,0) in (74) 
				                            and isnull(tt.SelectedAccNo, '') = ''
			                            select @cDebitAccountId = SelectedAccNo      
			                            from dbo.ReksaTransaction_TT      
			                            where TranId = @nTranId           
			                            if isnull(@cDebitAccountId,'') = ''      
			                            begin      
				                            set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'        
				                            goto ERROR          
			                            end	
		                            end									
		                            if @nTranType in (8) -- blokir khusus subcription saja, kalo ngga perlu otorisasi WM        
		                            Begin       
			                            select @nClientId = ClientId        
				                            , @mTranAmt = TranAmt + case when FullAmount = 1 then SubcFee else 0 end        
				                            , @cTranCode = TranCode        
				                            , @nTranType = TranType        
				                            , @cBlockBranch = BlockBranch        
				                            , @cTranCCY = TranCCY        
				                            , @dTranDate = TranDate     
				                            , @nAmountBeforeFee = TranAmt        
				                            , @nFee = SubcFee        
			                            from ReksaTransaction_TT        
			                            where TranId = @nTranId        
			                            if @@error!= 0        
			                            begin         
				                            set @cErrMsg = 'Error ambil data!'   
				                            goto ERROR        
			                            end
			                            select @cCurrWorkingDate = current_working_date        
			                            from control_table 
			                            select @cTranBranch = b.office_id_sibs        
			                            from user_nisp_v a 
			                            join office_information_all_v b        
				                            on a.office_id = b.office_id        
			                            where a.nik = @cNik        
				                            and isnull(ltrim(cost_centre_sibs),'') = '' 
			                            update tt      
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId     
									                            when tt.TranCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != '' then rm.NISPAccountIdUSD     
										                            else rm.NISPAccountIdMC     
								                            end     
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId           
				                            and isnull(tt.ExtStatus,0) not in (74)  
				                            and isnull(tt.SelectedAccNo, '') = ''
			                            update tt       
			                            set SelectedAccNo = case 
									                            when tt.TranCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA     
									                            when tt.TranCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA     
									                            else rm.AccountIdMCTA     
								                            end    
			                            from dbo.ReksaTransaction_TT tt      
			                            join dbo.ReksaCIFData_TM rc      
				                            on tt.ClientId = rc.ClientId      
			                            join dbo.ReksaMasterNasabah_TM rm      
				                            on rm.CIFNo = rc.CIFNo      
			                            where tt.TranId = @nTranId     
				                            and isnull(tt.ExtStatus,0) in (74)
				                            and isnull(tt.SelectedAccNo, '') = ''

			                            select @cDebitAccountId = SelectedAccNo      
			                            from dbo.ReksaTransaction_TT      
			                            where TranId = @nTranId     
                                        -- 20250710, Andhika J, RDN-1208, begin

                                        set @cDebitAccountId = right('000000000000' + @cDebitAccountId, 12)

                                        if isnull(@cMCAllowed, '') = ''
		                                begin
			                                if not exists(select top 1 1 from DDMAST_v where ACCTNO = @cDebitAccountId)
				                                select @cMCAllowed = D2MULT from DDTNEW_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cDebitAccountId
			                                else
				                                select @cMCAllowed = D2MULT from DDMAST_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cDebitAccountId
		                                end

                                        if(isnull(@cMCAllowed, '') = 'Y')
                                        begin
                                            set @cDebitAccountId = ltrim(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cTranCCY, 1) + ltrim(@cDebitAccountId)) 
                                        end
										-- 20250710, Andhika J, RDN-1208, end

			                            if isnull(@cDebitAccountId,'') = ''      
			                            begin      
				                            set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'        
				                            goto ERROR          
			                            end 
			                            --cek sudah pernah diblokir untuk rekening yg sama/ belum       
			                            if not exists(select top 1 1 from dbo.ReksaTranBlokirRelation_TM where AccountNumber = @cDebitAccountId and StatusBlokir = 1               
				                            and BlockExpiry >= getdate()                
			                            )        
			                            begin        
				                            set @bIsBlokir = 1        
			                            end        
			                            else        
			                            begin        
				                            set @bIsBlokir = 0        
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
                                        -- 20250108, Lely R, RDN-1211, begin
			                            --if @mTranAmt > (@mAvailableBalance - @mMinBalance)                  
			                            --begin        
				                            --set @cErrMsg = 'Saldo Tidak Cukup untuk transaksi!'        
				                            --goto ERROR        
			                            --end        
      
			                            --select @cDefaultCCY = CFAGTY      
			                            --from dbo.CFAGRPMC_v      
			                            --where convert(bigint, CFAGNO) = convert(bigint, @cDebitAccountId) 
                                        -- 20250108, Lely R, RDN-1211, end
                                        set @cGuid = newid()     
			                            if @bIsBlokir = 0        
			                            begin        
				                            set @mMinBalance = 0        
				                            set @mClosingFee = 0        
			                            end
			                            select @nSaldoMinBlokir = @mMinBalance 
			                            set @mTranAmt = @mTranAmt + @mMinBalance 
			                            select @dBlockExpiry = dateadd(dd, 3, @cCurrWorkingDate)
			                            select @cBlockReason = 'Subc Reksadana '+ @cTranCode

                                        -- 20250108, Lely R, RDN-1211, begin
										-- 20250710, Andhika J, RDN-1208, begin
                                        --set @cDebitAccountId = ltrim(SQL_SIBS.dbo.fnGetSIBSCurrencyCode(@cCurrencyType, 1) + ltrim(@cDebitAccountId))
										-- 20250710, Andhika J, RDN-1208, end
                                        -- 20250108, Lely R, RDN-1211, end
                                        if isnull(@nSequenceNo,0) != 0        
			                            begin             
				                            insert dbo.ReksaTranBlokirRelation_TM(TranId, TranType, AccountNumber, TranDate, NAVValueDate, TranCCY,        
				                            TranAmt, FeeAmt, SaldoMinBlokir,         
				                            BlokirAmount, BlockSequence, BlockBranch, BlockExpiry, TellerId, StatusBlokir, BlockReason        
				                            )        
				                            select @nTranId, @nTranType, @cDebitAccountId, @dTranDate, @dNAVValueDate, @cCurrencyType,        
				                            @nAmountBeforeFee, @nFee, @nSaldoMinBlokir,        
				                            @mTranAmt, @nSequenceNo, @cTranBranch, @dBlockExpiry, @cTellerId, @bIsBlokir, @cBlockReason        
			                            end
			                            insert #ReksaSellingFee_TM (AgentId, ProdId, CCY          
				                            , Amount, TransactionDate, ValueDate             
				                            , Settled, SettleDate, UserSuid, TotalAccount, TotalUnit, TotalNominal,    
				                            OfficeId    
			                            )    
			                            select rt.AgentId, rt.ProdId, rp.ProdCCY    
			                            , case 
				                            when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,sum(cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * rt.TranAmt as decimal(25,13))))          
				                            else dbo.fnReksaSetRounding(rt.ProdId,3,sum(cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * (rt.TranAmt - rt.SubcFee) as decimal(25,13))))        
			                            end                      
			                            , rt.TranDate, rt.NAVValueDate          
			                            , 0, NULL, 7,         
			                            count(*), dbo.fnReksaSetRounding(rt.ProdId,2,sum(cast(rt.TranAmt/rt.NAV as decimal(25,13)))),         
			                            case 
				                            when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,sum(rt.TranAmt))        
				                            else dbo.fnReksaSetRounding(rt.ProdId,3,sum(rt.TranAmt - rt.SubcFee))        
			                            end                   
			                            , rt.OfficeId                    
			                            from dbo.ReksaTransaction_TT rt         
			                            join dbo.ReksaProduct_TM rp          
				                            on rt.ProdId = rp.ProdId          
			                            join ReksaProductParam_TM rpp          
				                            on rp.ParamId = rpp.ParamId           
			                            join dbo.ReksaParamFee_TM rpf        
				                            on rpf.ProdId = rt.ProdId        
					                            and rpf.TrxType = 'SELLING'        
			                            where rt.TranId = @nTranId        
				                            and rpp.CloseEndBit = 0          
				                            and isnull(rpf.PctSellingUpfrontDefault,0) > 0          
				                            and rt.TranAmt > 0          
			                            group by rt.OfficeId, rt.AgentId, rt.ProdId, rp.ProdCCY, rt.TranDate, rt.NAVValueDate, rt.FullAmount        
			                            update pa        
			                            set PercentageSellingFeeBased = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl          
				                            on pa.ProdId = rl.ProdId         
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 1  
			                            update pa        
			                            set PercentageTaxFeeBased = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl              
				                            on pa.ProdId = rl.ProdId             
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 2   
			                            update pa        
			                            set PercentageFeeBased3 = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl          
				                            on pa.ProdId = rl.ProdId               
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 3      
			                            update pa        
			                            set PercentageFeeBased4 = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl        
				                            on pa.ProdId = rl.ProdId         
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 4   
			                            update pa        
			                            set PercentageFeeBased5 = isnull(rl.Percentage, 0)        
			                            from #ReksaSellingFee_TM pa        
			                            join dbo.ReksaListGLFee_TM rl               
				                            on pa.ProdId = rl.ProdId                
			                            where rl.TrxType = 'SELLING'        
				                            and rl.Sequence = 5 
			                            update #ReksaSellingFee_TM        
			                            set SellingFeeBased = cast(cast(PercentageSellingFeeBased/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))        
				                            , TaxFeeBased = cast(cast(PercentageTaxFeeBased/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))         
				                            , FeeBased3 = cast(cast(PercentageFeeBased3/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))          
				                            , FeeBased4 = cast(cast(PercentageFeeBased4/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))        
				                            , FeeBased5 = cast(cast(PercentageFeeBased5/@mTotalPctFeeBased as decimal(25,13)) * Amount as decimal(25,13))  
			                            update #ReksaSellingFee_TM        
			                            set TotalFeeBased = isnull(SellingFeeBased, 0) +  isnull(TaxFeeBased, 0) + isnull(FeeBased3, 0) + isnull(FeeBased4, 0) + isnull(FeeBased5, 0)        
			                            update #ReksaSellingFee_TM        
			                            set SelisihFeeBased = isnull(Amount, 0) -  isnull(TotalFeeBased, 0)
			                            update #ReksaSellingFee_TM        
			                            set SellingFeeBased = isnull(SellingFeeBased, 0) +  isnull(SelisihFeeBased, 0) 
			                            update #ReksaSellingFee_TM        
			                            set TotalFeeBased = isnull(SellingFeeBased, 0) +  isnull(TaxFeeBased, 0) + isnull(FeeBased3, 0) + isnull(FeeBased4, 0) + isnull(FeeBased5, 0) 
			                            if not exists(select top 1 1 from dbo.ReksaSellingFee_TM where TransactionDate = @cCurrWorkingDate)          
			                            begin          
				                            insert dbo.ReksaSellingFee_TM (AgentId, ProdId, CCY          
					                            , Amount, TransactionDate, ValueDate          
					                            , Settled, SettleDate, UserSuid, TotalAccount, TotalUnit, TotalNominal        
					                            , SellingFeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased      
					                            , OfficeId     
					                            )        
				                            select AgentId, ProdId, CCY          
					                            , Amount, TransactionDate, ValueDate          
					                            , Settled, SettleDate, UserSuid, TotalAccount, TotalUnit, TotalNominal        
					                            , SellingFeeBased, TaxFeeBased, FeeBased3, FeeBased4, FeeBased5, TotalFeeBased       
					                            , OfficeId     
				                            from #ReksaSellingFee_TM        
			                            end 
			                            if not exists(select top 1 1 from dbo.ReksaSellingFeeDetail_TM where TransactionDate = @cCurrWorkingDate)          
			                            begin          
											insert dbo.ReksaSellingFeeDetail_TM (ClientId, AgentId, ProdId, CCY           
												, Amount, TransactionDate, ValueDate, UserSuid             
												, NAV, UnitBalance, SubcUnit)              
											select rt.ClientId, rt.AgentId, rt.ProdId, rp.ProdCCY          
												, case 
													when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * rt.TranAmt as decimal(25,13)))              
													else dbo.fnReksaSetRounding(rt.ProdId,3,cast(cast(isnull(rpf.PctSellingUpfrontDefault,0)/100.00 as decimal(25,13)) * (rt.TranAmt - rt.SubcFee) as decimal(25,13)))        
												end                 
												, rt.TranDate, rt.NAVValueDate, 7, rt.NAV         
												, dbo.fnReksaSetRounding(rt.ProdId,2,cast(rt.TranAmt/rt.NAV as decimal(25,13)))         
												, case when rt.FullAmount = 1 then dbo.fnReksaSetRounding(rt.ProdId,3,rt.TranAmt)           
													else dbo.fnReksaSetRounding(rt.ProdId,3,(rt.TranAmt - rt.SubcFee))         
												end                        
											from dbo.ReksaTransaction_TT rt         
											join dbo.ReksaProduct_TM rp          
												on rt.ProdId = rp.ProdId          
											join ReksaProductParam_TM rpp          
												on rp.ParamId = rpp.ParamId           
											join dbo.ReksaParamFee_TM rpf        
												on rpf.ProdId = rt.ProdId        
													and rpf.TrxType = 'SELLING'        
											where rt.TranId = @nTranId        
												and rpp.CloseEndBit = 0          
												and isnull(rpf.PctSellingUpfrontDefault,0) > 0          
												and rt.TranAmt > 0     
										end      
			                            If @@error!= 0          
			                            Begin          
				                            set @cErrMsg = 'Gagal Insert Data Selling Fee!'          
				                            goto ERROR          
			                            end            
		                            end          
	                            end
                                if @nTranType = 4 -- redemption all khusus reg subs        
		                            and exists (select top 1 1        
		                            from ReksaTransaction_TT a         
		                            join ReksaRegulerSubscriptionClient_TM b        
		                            on a.ClientId = b.ClientId         
		                            and b.Status = 1        
		                            where a.TranId = @nTranId)              
	                            begin        
		                            set @bRegSubs = 1        
		                            select @nClientId = ClientId        
			                            , @mTranAmt = BiayaHadiah        
			                            , @cTranCode = TranCode        
			                            , @nTranType = TranType    
			                            , @cBlockBranch = BlockBranch        
			                            , @cTranCCY = TranCCY        
		                            from ReksaTransaction_TT        
		                            where TranId = @nTranId        
		                            if @@error!= 0        
		                            begin         
			                            set @cErrMsg = 'Error ambil data!'        
			                            goto ERROR        
		                            end 
		                            select @cCurrWorkingDate = current_working_date        
		                            from control_table        
		                            select @cTranBranch = b.office_id_sibs        
		                            from user_nisp_v a 
		                            join office_information_all_v b        
			                            on a.office_id = b.office_id        
		                            where a.nik = @cNik        
			                            and isnull(ltrim(cost_centre_sibs),'') = ''  
		                            update tt      
		                            set SelectedAccNo = case when tt.TranCCY = 'IDR' and isnull(rm.NISPAccountId,'') != '' then rm.NISPAccountId     
								                            when tt.TranCCY = 'USD' and isnull(rm.NISPAccountIdUSD,'') != '' then rm.NISPAccountIdUSD     
								                            else rm.NISPAccountIdMC     
							                            end            
		                            from dbo.ReksaTransaction_TT tt   
		                            join dbo.ReksaCIFData_TM rc      
			                            on tt.ClientId = rc.ClientId      
		                            join dbo.ReksaMasterNasabah_TM rm      
			                            on rm.CIFNo = rc.CIFNo      
		                            where tt.TranId = @nTranId    
			                            and isnull(tt.ExtStatus,0) not in (74)    
			                            and isnull(tt.SelectedAccNo, '') = ''
		                            update tt       
		                            set SelectedAccNo = case when tt.TranCCY = 'IDR' and isnull(rm.AccountIdTA,'') != '' then rm.AccountIdTA     
								                            when tt.TranCCY = 'USD' and isnull(rm.AccountIdUSDTA,'') != '' then rm.AccountIdUSDTA     
								                            else rm.AccountIdMCTA     
							                            end          
		                            from dbo.ReksaTransaction_TT tt      
		                            join dbo.ReksaCIFData_TM rc      
			                            on tt.ClientId = rc.ClientId      
		                            join dbo.ReksaMasterNasabah_TM rm      
			                            on rm.CIFNo = rc.CIFNo      
		                            where tt.TranId = @nTranId    
			                            and isnull(tt.ExtStatus,0) in (74) 
			                            and isnull(tt.SelectedAccNo, '') = ''
		                            select @cDebitAccountId = SelectedAccNo      
		                            from dbo.ReksaTransaction_TT      
		                            where TranId = @nTranId  
		                            if isnull(@cDebitAccountId,'') = ''      
		                            begin      
			                            set @cErrMsg = 'Harap melakukan setting nomor rekening di menu Master Nasabah!'        
			                            goto ERROR          
		                            end
                                    -- blokir rek bila saldo cukup      
		                            set @cGuid = newid() 
                                    --hapus sisa jadwal biar ga otomatis subscribe lagi        
		                            update ReksaRegulerSubscriptionSchedule_TT        
		                            set StatusId = 4 -- sudah redempt all        
		                            from ReksaRegulerSubscriptionSchedule_TT a        
		                            join ReksaTransaction_TH b        
			                            on a.TranId = b.TranId        
		                            where b.ClientId = @nClientId         
			                            and a.StatusId in (0,3)
		                            -- belum ada bill jadi coba batalkan transaksi reg subsnya 
		                            update ReksaTransaction_TT
		                            set Status = 2
		                            where ClientId = @nClientId 
			                            and TranType = 8         
			                            and NAVValueDate = @cCurrWorkingDate 
			                            and Status in (0, 1) 
			                            and BillId is null 
                                end        
	                            set @bRegSubs = 0         
                            end  
                            update dbo.ReksaTransaction_TT        
                            set CheckerSuid = @cNik        
	                            , Status = @nStatus        
	                            , BlockSequence = case 
						                            when TranType in (1,2,8) then @nSequenceNo         
						                            when TranType = 4 and @bRegSubs = 1 then @nSequenceNo         
						                            else 0 
					                            end        
					                            , BlockBranch = case 
						                            when TranType in (1,2,8) then @cTranBranch         
						                            when TranType = 4 and @bRegSubs = 1 then @cTranBranch         
						                            else '' 
					                            end              
					                            , LastUpdate = getdate()            
					                            , AuthType = case when @bAccepted = 1 then @nType else 1 end          
                            where TranId = @nTranId 
							update ReksaRegulerSubscriptionSchedule_TT      
							set StatusId = @nStatus,      
								LastAttemptDate = getdate()          
								, NAV = @nNAV      
								, NAVValueDate = @dNAVValueDate          
								, ErrorDescription = ''         
							where RegulerSubscriptionTranId = @nTranIdRegulerSubscription    
							update ReksaRegulerSubscriptionNonIndexFundSchedule_TMP      
							set StatusId = @nStatus,      
								LastAttemptDate = getdate()          
								, NAV = @nNAV      
								, NAVValueDate = @dNAVValueDate          
								, ErrorDescription = ''         
							where RegulerSubscriptionTranId = @nTranIdRegulerSubscription
                            if @bAccepted = 1
                            begin
	                            if exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TM where TranCode = @pcTranCode)    
	                            begin
		                            update dbo.ReksaRegulerSubscriptionClient_TM
		                            set Status = 1
			                            , CheckerSuid = @cNik
		                            where TranCode = @pcTranCode 
		                            update tm 
		                            set tm.LastUser	= tm2.LastUser
			                            , tm.LastUpdate	= tm2.LastUpdate
			                            , tm.RefID	= tm2.RefID
			                            , tm.OfficeId	= tm2.OfficeId
			                            , tm.JoinDate	= tm2.JoinDate
			                            , tm.ProdId	= tm2.ProdId
			                            , tm.TranCode	= tm2.TranCode
			                            , tm.TranCCY	= tm2.TranCCY
			                            , tm.TranAmount	= tm2.TranAmount
			                            , tm.FullAmount	= tm2.FullAmount
			                            , tm.SubcFee	= tm2.SubcFee
			                            , tm.IsFeeEdit	= tm2.IsFeeEdit
			                            , tm.JenisPerhitunganFee	= tm2.JenisPerhitunganFee
			                            , tm.PercentageFee	= tm2.PercentageFee
			                            , tm.ByUnit	= tm2.ByUnit
			                            , tm.JangkaWaktu	= tm2.JangkaWaktu
			                            , tm.JatuhTempo	= tm2.JatuhTempo
			                            , tm.AutoRedemption	= tm2.AutoRedemption
			                            , tm.Asuransi	= tm2.Asuransi
			                            , tm.FreqDebetMethod	= tm2.FreqDebetMethod
			                            , tm.FreqDebet	= tm2.FreqDebet
			                            , tm.StartDebetDate	= tm2.StartDebetDate
			                            , tm.Referentor	= tm2.Referentor
			                            , tm.UserSuid	= tm2.UserSuid
			                            , tm.CheckerSuid	= tm2.CheckerSuid
			                            , tm.Inputter	= tm2.Inputter
			                            , tm.Seller	= tm2.Seller
			                            , tm.Waperd	= tm2.Waperd
			                            , tm.Channel	= tm2.Channel
			                            , tm.UserLoginIBMB	= tm2.UserLoginIBMB
			                            , tm.AuthType	= tm2.AuthType
			                            , tm.ExtStatus	= tm2.ExtStatus
			                            , tm.TranId	= tm2.TranId
			                            , tm.DocFCSubscriptionForm	= tm2.DocFCSubscriptionForm
			                            , tm.DocFCDevidentAuthLetter	= tm2.DocFCDevidentAuthLetter
			                            , tm.DocFCJoinAcctStatementLetter	= tm2.DocFCJoinAcctStatementLetter
			                            , tm.DocFCIDCopy	= tm2.DocFCIDCopy
			                            , tm.DocFCOthers	= tm2.DocFCOthers
			                            , tm.DocTCSubscriptionForm	= tm2.DocTCSubscriptionForm
			                            , tm.DocTCTermCondition	= tm2.DocTCTermCondition
			                            , tm.DocTCProspectus	= tm2.DocTCProspectus
			                            , tm.DocTCFundFactSheet	= tm2.DocTCFundFactSheet
			                            , tm.DocTCOthers	= tm2.DocTCOthers
			                            , tm.FundId	= tm2.FundId
			                            , tm.AgentId	= tm2.AgentId
			                            , tm.IsFutureRDB	= tm2.IsFutureRDB
		                            from dbo.ReksaRegulerSubscriptionClient_TM tm 
		                            join ReksaRegulerSubscriptionClient_TM tm2 
			                            on tm.TranCode = tm2.TranCode
		                            where tm2.TranCode = @pcTranCode and tm2.AuthType = 4
	                            end 
	                            if exists (select top 1 1 from dbo.ReksaRegulerSubscriptionClient_TMP where TranCode = @pcTranCode ) 
	                            begin
		                            update dbo.ReksaRegulerSubscriptionClient_TMP
		                            set Status = 1
		                            where TranCode = @pcTranCode and TranId = @nTranId
	                            end   
	                            if exists (select top 1 1 from dbo.ReksaTransaction_TMP where TranCode = @pcTranCode ) 
	                            begin
		                            update dbo.ReksaTransaction_TMP
		                            set Status = 1
		                            where TranCode = @pcTranCode and TranId = @nTranId
	                            end
	                            --update rek ke master nasabah
	                            if exists(select top 1 1 from dbo.ReksaTransaction_TT with(nolock) where TranType IN (1,2,3,4, 8) 
		                            and TranId = @nTranId and isnull(RegSubscriptionFlag,0) <> 2 and Channel not in ('UPL')) -- bukan trx autodebet rdb dan upload PO
	                            begin	
		                            select @pcCIFKey = right('0000000000000' + convert(varchar,cif.CIFNo) ,13) 
		                            from dbo.ReksaTransaction_TT tt with(nolock) 
		                            join dbo.ReksaCIFData_TM cif with(nolock)
			                            on tt.ClientId = cif.ClientId
		                            where tt.TranId = @nTranId 
		                            select @pcRelationAccountNameNew = ''
		                            select @pcRelationAccountNameNew = CFAAL1 from CFALTN where CFAACT = @cDebitAccountId
		                            if isnull(@pcRelationAccountNameNew, '') = ''
		                            begin
			                            select @pcRelationAccountNameNew = CFAAL1 
			                            from CFALTNNew_v
			                            where CFAACT = @cDebitAccountId
		                            end 
		                            select @cSelectedAccNo = SelectedAccNo, @cTranCCY = TranCCY from dbo.ReksaTransaction_TT with(nolock) where TranId = @nTranId    
		                            if isnull(@cMCAllowed, '') = ''
		                            begin
			                            if not exists(select top 1 1 from DDMAST_v where ACCTNO = @cSelectedAccNo)
				                            select @cMCAllowed = D2MULT from DDTNEW_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cSelectedAccNo
			                            else
				                            select @cMCAllowed = D2MULT from DDMAST_v dd join DDPAR2_v ddpar on dd.SCCODE = ddpar.SCCODE where dd.ACCTNO = @cSelectedAccNo
		                            end
		                            if @cTranCCY = 'IDR' and @cMCAllowed <> 'Y'
		                            begin   
			                            update dbo.ReksaMasterNasabah_TM
			                            set NISPAccountId = @cSelectedAccNo,
				                            NISPAccountName = @pcRelationAccountNameNew
			                            where CIFNo = @pcCIFKey 
			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM with(nolock)
				                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdMC,'') != '')
			                            begin
				                            if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
					                            where CIFNo = @pcCIFKey and b.ProdCCY <> 'IDR' and a.CIFStatus= 'A')
				                            begin
					                            if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM with(nolock)
						                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdUSD,'') = '')
					                            begin
						                            update dbo.ReksaMasterNasabah_TM
						                            set NISPAccountIdUSD = NISPAccountIdMC ,
						                            NISPAccountNameUSD = NISPAccountNameMC
						                            where CIFNo = @pcCIFKey 
					                            end
				                            end
				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountIdMC = null,
					                            NISPAccountNameMC = null
				                            where CIFNo = @pcCIFKey 
			                            end                     
		                            end
		                            else if @cTranCCY = 'USD' and @cMCAllowed <> 'Y'
		                            begin
			                            update dbo.ReksaMasterNasabah_TM
			                            set NISPAccountIdUSD = @cSelectedAccNo,
				                            NISPAccountNameUSD = @pcRelationAccountNameNew
			                            where CIFNo = @pcCIFKey
			                            if exists (select top 1 1 from dbo.ReksaCIFData_TM a join dbo.ReksaProduct_TM b on a.ProdId = b.ProdId
				                            where CIFNo = @pcCIFKey and b.ProdCCY = 'IDR' and a.CIFStatus= 'A')
			                            begin
				                            if exists (select top 1 1 from dbo.ReksaMasterNasabah_TM
					                            where CIFNo = @pcCIFKey and isnull(NISPAccountId,'') = '')
				                            begin
					                            update dbo.ReksaMasterNasabah_TM
					                            set NISPAccountId = NISPAccountIdMC,
						                            NISPAccountName = NISPAccountNameMC
					                            where CIFNo = @pcCIFKey 
				                            end
			                            end
			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
			                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdMC,'') != '')
			                            begin
				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountIdMC = null,
					                            NISPAccountNameMC = null
				                            where CIFNo = @pcCIFKey 
			                            end         
		                            end
		                            else if  @cMCAllowed = 'Y'
		                            begin
			                            update dbo.ReksaMasterNasabah_TM
			                            set NISPAccountIdMC = @cSelectedAccNo,
				                            NISPAccountNameMC = @pcRelationAccountNameNew
			                            where CIFNo = @pcCIFKey 
			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
				                            where CIFNo = @pcCIFKey and isnull(NISPAccountId,'') != '')
			                            begin
				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountId = null,
					                            NISPAccountName = null
				                            where CIFNo = @pcCIFKey 
			                            end
			                            if exists(select top 1 1 from dbo.ReksaMasterNasabah_TM
				                            where CIFNo = @pcCIFKey and isnull(NISPAccountIdUSD,'') != '')
			                            begin
				                            update dbo.ReksaMasterNasabah_TM
				                            set NISPAccountIdUSD = null,
					                            NISPAccountNameUSD = null
				                            where CIFNo = @pcCIFKey 
			                            end 
		                            end
	                            end
                            end
                            drop table #ReksaSellingFee_TM  
                            ERROR:        
								if isnull(@cErrMsg, '') <> ''
								begin
									update dbo.ReksaTransaction_TT        
									set CheckerSuid = @cNik        
										, Status = 2         
									where TranId = @nTranId
									update ReksaRegulerSubscriptionNonIndexFundSchedule_TMP      
									set StatusId = 3      
										, LastAttemptDate = getdate()   
										, ErrorDescription =  @cErrMsg   
									where RegulerSubscriptionTranId = @nTranIdRegulerSubscription
									update dbo.ReksaRegulerSubscriptionSchedule_TT
		                            set StatusId = 3
										, LastAttemptDate = getdate()
										, ErrorDescription =  @cErrMsg   
		                            where RegulerSubscriptionTranId = @nTranIdRegulerSubscription
								end
								select isnull(@cErrMsg, '') as responseMessage
                        ";
            try
            {
                SqlParameter[] sqlParam = new SqlParameter[5];
                sqlParam[0] = new SqlParameter("@nTranId", TranId);
                sqlParam[1] = new SqlParameter("@bAccepted", Accepted);
                sqlParam[2] = new SqlParameter("@cNik", AuthNIK);
                sqlParam[3] = new SqlParameter("@nSequenceNo", BlockSequence);
                sqlParam[4] = new SqlParameter("@nTranIdRegulerSubscription", RDBTranId);
                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);
                if (dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString() != "")
                    throw new Exception(dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());
                Console.WriteLine("[JOB][SUCCESS] [AUTH RDB NON INDEX FUND]" + dsDataOut.Tables[0].Rows[0]["responseMessage"].ToString());
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                Console.WriteLine("[JOB][FAILED] [AUTH RDB NON INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
            #endregion

        #region get other link API
            private T GetData<T>(string reqUri, object apiMessageRq)
        {
            var restClient = new RestWSClient<ReksaDefaultRes<T>>(_ignoreSSL);
            try
            {
                var apiMessageRs = restClient.invokeRESTServicePost(reqUri, apiMessageRq);
                if (apiMessageRs.IsSuccess)
                {
                    return apiMessageRs.Data;
                }
                else
                {
                    throw new Exception(JsonConvert.SerializeObject(apiMessageRs));
                }
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
        #endregion get other link API

        #region Stop RDB Index Fund
        public bool StopRDBIndexFund()
        {
            DataSet dsDataOut = new DataSet();

            bool isSuccess = false;
            string errMsg = "";
            String sqlCommand = "";

            sqlCommand = @"update control_table set rdb_index_fund_status = 0";

            try
            {
                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);

                if (!errMsg.EndsWith(""))
                    throw new Exception(errMsg);

                isSuccess = true;

            }
            catch (Exception ex)
            {
                isSuccess = false;

                Console.WriteLine("[JOB][FAILED] [STOP RDB INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
        #endregion

        #region Stop RDB Non Index Fund
        public bool StopRDBNonIndexFund()
        {
            DataSet dsDataOut = new DataSet();
            bool isSuccess = false;
            string errMsg = "";
            String sqlCommand = "";
            sqlCommand = @"update control_table set rdb_process_status = 0";
            try
            {
                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);
                if (!errMsg.EndsWith(""))
                    throw new Exception(errMsg);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                Console.WriteLine("[JOB][FAILED] [STOP RDB NON INDEX FUND]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }
            return isSuccess;
        }
        #endregion
        #region Release Account
        public ApiMessage<ReleaseAccountRes> ReleaseAccountLogic(ApiMessage<ReleaseAccountReq> paramIn)
        {
            DataSet dsOut = new DataSet();

            String strErrMsg = "";
            ApiMessage<ReleaseAccountRes> ApiMsgResRelease = new ApiMessage<ReleaseAccountRes>();

            try
            {
                string reqUri = this._apiReleaseAccountURL;
                ReleaseAccountRes releaseDetail = GetData<ReleaseAccountRes>(reqUri, paramIn);

                ReleaseAccountRes res = new ReleaseAccountRes();
                try
                {
                    res.ListData = releaseDetail.ListData;
                }
                catch (Exception ex)
                {
                    strErrMsg = ex.Message;
                    ApiMsgResRelease.ErrorDescription = strErrMsg;
                    ApiMsgResRelease.IsSuccess = false;
                    ApiMsgResRelease.IsResponseMessage = false;
                    ApiMsgResRelease.MessageGUID = paramIn.MessageGUID;
                    ApiMsgResRelease.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                    ApiMsgResRelease.UserNIK = paramIn.UserNIK;
                }

                ApiMsgResRelease.Data = res;
                ApiMsgResRelease.ErrorDescription = strErrMsg;
                ApiMsgResRelease.IsSuccess = true;
                ApiMsgResRelease.IsResponseMessage = true;
                ApiMsgResRelease.MessageGUID = paramIn.MessageGUID;
                ApiMsgResRelease.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ApiMsgResRelease.UserNIK = paramIn.UserNIK;

                return ApiMsgResRelease;

            }
            catch (Exception ex)
            {
                ApiMsgResRelease.ErrorDescription = ex.Message;
                ApiMsgResRelease.IsSuccess = false;
                ApiMsgResRelease.IsResponseMessage = false;
                ApiMsgResRelease.MessageGUID = paramIn.MessageGUID;
                ApiMsgResRelease.TransactionMessageGUID = paramIn.TransactionMessageGUID;
                ApiMsgResRelease.UserNIK = paramIn.UserNIK;

                Console.WriteLine("[JOB][FAILED] [RELEASE ACCOUNT]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + ex.Message.ToString() + " " + GetMethodName(), ex, Guid.NewGuid().ToString());

                return ApiMsgResRelease;
            }

        }
        #endregion

        //20230905, sandi, HTR-160, begin
        #region Update Status Transaksi
        public bool UpdateTransaksi(int RDBTranId, int TranId, int AuthNIK, string ErrorMessage)
        {
            DataSet dsDataOut = new DataSet();

            bool isSuccess = false;
            string errMsg = "";

            String sqlCommand = "";

            sqlCommand = @"
                            update dbo.ReksaTransaction_TT        
						    set CheckerSuid = @cNik       
							    , Status = 2         
						    where TranId = @nTranId

						    update ReksaRegulerSubscriptionSchedule_TMP      
						    set StatusId = 3      
							    , LastAttemptDate = getdate()   
							    , ErrorDescription =  @cErrMsg   
						    where RegulerSubscriptionTranId = @nTranIdRegulerSubscription

                            update ReksaRegulerSubscriptionNonIndexFundSchedule_TMP      
						    set StatusId = 3      
							    , LastAttemptDate = getdate()   
							    , ErrorDescription =  @cErrMsg   
						    where RegulerSubscriptionTranId = @nTranIdRegulerSubscription

						    update dbo.ReksaRegulerSubscriptionSchedule_TT
		                    set StatusId = 3
							    , LastAttemptDate = getdate()
							    , ErrorDescription =  @cErrMsg   
		                    where RegulerSubscriptionTranId = @nTranIdRegulerSubscription

                        ";

            try
            {
                SqlParameter[] sqlParam = new SqlParameter[4];
                sqlParam[0] = new SqlParameter("@nTranId", TranId);
                sqlParam[1] = new SqlParameter("@nTranIdRegulerSubscription", RDBTranId);
                sqlParam[2] = new SqlParameter("@cErrMsg", ErrorMessage);
                sqlParam[3] = new SqlParameter("@cNik", AuthNIK);

                if (!clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sqlCommand, ref sqlParam, out dsDataOut, out errMsg))
                    throw new Exception(errMsg);

                Console.WriteLine("[JOB][SUCCESS] [UPDATE STATUS TRANSAKSI] RDBTranId : " + RDBTranId.ToString() + " - " + ErrorMessage.ToString());

                isSuccess = true;

            }
            catch (Exception ex)
            {
                isSuccess = false;

                Console.WriteLine("[JOB][FAILED] [UPDATE STATUS TRANSAKSI]" + ex.Message.ToString());
                _common.LogError(this, new StackTrace(false), "[ERROR] " + GetMethodName(), ex, Guid.NewGuid().ToString());
            }

            return isSuccess;
        }
        #endregion
        //20230905, sandi, HTR-160, end
        public bool UpdateStatusBlokir (int nTranIdRegulerSubscription, int nTranId, string ErrorMessage, string cNik)
        {
            bool bResult = false;
            DataSet dsData = new DataSet();
            string ErrMsg = "";
            try
            {
                string sql = @"
                        declare 
                        @pnTranId                           int
                        , @nTranIdRegulerSubscription       int
                        , @cErrMsg                          varchar(800)
                        , @cNik                             char(10)
                        set @pnTranId = " + nTranId + @"                        
                        set @nTranIdRegulerSubscription = " + nTranIdRegulerSubscription + @"
                        set @cErrMsg    = '" + ErrorMessage + @"'
                        set @cNik       = '" + cNik + @"'
                        update dbo.ReksaTransaction_TT        
                        set CheckerSuid = @cNik        
                            , Status = 2         
                        where TranId = @nTranId
                        update ReksaRegulerSubscriptionSchedule_TMP      
                        set StatusId = 3      
                            , LastAttemptDate = getdate()   
                            , ErrorDescription =  @cErrMsg   
                        where RegulerSubscriptionTranId = @nTranIdRegulerSubscription
                        update dbo.ReksaRegulerSubscriptionSchedule_TT
                        set StatusId = 3
                            , LastAttemptDate = getdate()
                            , ErrorDescription =  @cErrMsg   
                        where RegulerSubscriptionTranId = @nTranIdRegulerSubscription
                ";
                if (clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sql, out dsData, out ErrMsg))
                {
                    bResult = true;
                }
                else
                {
                    bResult = false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return bResult;
        }
        //20240402, Andhika J, RDN-1150, begin
        public bool CekProsesStatus(string strJobName, int nFlagStatus, out int ProcessStatus) //nFlagStatus = 1(sedang jalan), 0 (tidak jalan)
        {
            bool bResult = false;
            DataSet dsData = new DataSet();
            dsData = new DataSet();
            string ErrMsg = "";
            ProcessStatus = 0;
            try
            {
                string sql = @"
                        declare 
                        @pcJobName                      varchar(20)
                        , @pnProcessStatus              int
                        , @pnStatusAwal                 int
                        , @ErrMessage                   varchar(50)
                       
                        set @pcJobName = '" + strJobName + @"'       
                        set @pnProcessStatus = " + nFlagStatus + @"       

                        if exists(select top 1 1 from ReksaJobAPI_TR where JobCode = @pcJobName and ProcessStatus = 1)
                        begin
                             update ReksaJobAPI_TR 
                             set ProcessStatus = @pnProcessStatus
                             where JobCode = @pcJobName

                             select @pnStatusAwal = ProcessStatus
                             from ReksaJobAPI_TR
                             where JobCode = @pcJobName
            
                        end
                        else
                        begin
                            
                             select @pnStatusAwal = ProcessStatus
                             from ReksaJobAPI_TR
                             where JobCode = @pcJobName

                             update ReksaJobAPI_TR 
                             set ProcessStatus = @pnProcessStatus
                             where JobCode = @pcJobName

                             set @ErrMessage = ''
                        end

						select @pnStatusAwal ProcessStatus
                ";
                if (clsCallWS.CallQueryFromWs(this._strUrlWsReksa, this._ignoreSSL, sql, out dsData, out ErrMsg))
                {
                    if (dsData.Tables[0].Rows.Count > 0)
                    {
                        ProcessStatus = Convert.ToInt32(dsData.Tables[0].Rows[0]["ProcessStatus"]);
                        bResult = true;
                    }
                    else
                    {
                        bResult = false;
                    }
                }
                else
                {
                    bResult = false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return bResult;
        }

        //20240402, Andhika J, RDN-1150, end
    }
}
