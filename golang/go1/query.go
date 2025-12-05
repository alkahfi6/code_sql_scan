package dbobox

const QueryGetPreviousWorkingDate = `select * from OBOXGetPrevDate_v`

const QueryDeleteDuplicateDataConfoMK005 = `DELETE FROM confomk005_a 
WHERE convert(varchar(8), ProcessDate, 112) >= convert(varchar(8), getdate(), 112)`
const QueryDeleteDuplicateDataConfoMK006 = `DELETE FROM confomk006_a 
WHERE convert(varchar(8), ProcessDate, 112) >= convert(varchar(8), getdate(), 112)`

const QueryMUREXGetMK006AConfo = `
select CFN1,CA1
,CA2,CA3,CA4,DEALNO,PS_1
,SECID,ISSUER,DEALDATE,SETTDATE
,IPAYDATE,MDATE,COUPRATE_8,CCY
,QTY,PRICE_8,YIELD_8,COSTAMT
,PURCHINTAMT,BROKFEECCY,VATAMT
,WHTAMT,SETTCCY,SETTAMT,C1_1
,C2_1,ST1,ST2,ST3,ST4,C1_2,C2_2
,BIC,SD,DESCR,BROKFEEAMT
,M_BANK,CONTRACT,X_M_NB
    from [[schema]].mk006a_confo `
const QueryMurexGetMK005AConfo = `
SELECT CFN1,CA1,CA2,CA3,CA4
,DEALNO,PS_1,SECID,DESCR,ISSUER
,DEALDATE,SETTDATE,IPAYDATE,MDATE,COUPRATE_8
,CCY,QTY,PRICE_8,YIELD_8,COSTAMT
,PURCHINTAMT,BROKFEECCY,BROKFEEAMT,VATAMT,WHTAMT
,SETTCCY,SETTAMT,C1_1,C2_1,ST1
,ST2,ST3,ST4,C1_2,C2_2
,BIC,SD,M_BANK,CONTRACT,X_M_NB 
FROM [[schema]].MK005A_CONFO`

const QueryInsertConfoMK006A = `
INSERT INTO SQL_REPLICATE.dbo.confomk006_a (
CFN1,CA1,CA2,CA3,CA4,
DEALNO,PS_1,SECID,ISSUER,DEALDATE,
SETTDATE,IPAYDATE,MDATE,COUPRATE_8,CCY
,QTY,PRICE_8,YIELD_8,COSTAMT,PURCHINTAMT,
BROKFEECCY,VATAMT,WHTAMT,SETTCCY,SETTAMT,
C1_1,C2_1,ST1,ST2,ST3,
ST4,C1_2,C2_2,BIC,SD,
ProcessDate,DESCR,BROKFEEAMT,M_BANK,CONTRACT,
X_M_NB )
values (:CFN1,:CA1,:CA2,:CA3,:CA4,
:DEALNO,:PS_1,:SECID,:ISSUER,:DEALDATE,
:SETTDATE,:IPAYDATE,:MDATE,:COUPRATE_8,:CCY
,:QTY,:PRICE_8,:YIELD_8,:COSTAMT,:PURCHINTAMT,
:BROKFEECCY,:VATAMT,:WHTAMT,:SETTCCY,:SETTAMT,
:C1_1,:C2_1,:ST1,:ST2,:ST3,
:ST4,:C1_2,:C2_2,:BIC,:SD,
:ProcessDate,:DESCR,:BROKFEEAMT,:M_BANK,:CONTRACT,
:X_M_NB )
`

const QueryInsertConfoMK005A = `
INSERT INTO SQL_REPLICATE.dbo.confomk005_a (
CFN1,CA1,CA2,CA3,CA4,
DEALNO,PS_1,SECID,ISSUER,DEALDATE,
SETTDATE,IPAYDATE,MDATE,COUPRATE_8,CCY
,QTY,PRICE_8,YIELD_8,COSTAMT,PURCHINTAMT,
BROKFEECCY,VATAMT,WHTAMT,SETTCCY,SETTAMT,
C1_1,C2_1,ST1,ST2,ST3,
ST4,C1_2,C2_2,BIC,SD,
ProcessDate,DESCR,BROKFEEAMT,M_BANK,CONTRACT,
X_M_NB )
values (:CFN1,:CA1,:CA2,:CA3,:CA4,
:DEALNO,:PS_1,:SECID,:ISSUER,:DEALDATE,
:SETTDATE,:IPAYDATE,:MDATE,:COUPRATE_8,:CCY
,:QTY,:PRICE_8,:YIELD_8,:COSTAMT,:PURCHINTAMT,
:BROKFEECCY,:VATAMT,:WHTAMT,:SETTCCY,:SETTAMT,
:C1_1,:C2_1,:ST1,:ST2,:ST3,
:ST4,:C1_2,:C2_2,:BIC,:SD,
:ProcessDate,:DESCR,:BROKFEEAMT,:M_BANK,:CONTRACT,
:X_M_NB )
`

const QueryDeleteDuplicateDataMK005A = `delete from mk005_a 
where convert(varchar(8),ProcessDate, 112) = convert(varchar(8),getdate(), 112)`
const QueryDeleteDuplicateDataMK006A = `delete from mk006_a 
where convert(varchar(8),ProcessDate, 112) = convert(varchar(8),getdate(), 112)`

const QueryMurexGetMK006A = `
select DEALNO,BRANCHTRANSACTION  ,BRANCHCOMPLETION
,USERCOMPLETION ,STATUSDOKUMEN ,TRANSACTIONDATE
,FLAGDETAIL,KODEISIN,NAMASSB
,KATEGORIPENGUKURAN,NOMINAL,TANGGALPEMBELIAN
,JUMLAHPEMBELIAN,NAMAPEMBELI,GOLONGANPEMBELI
,MATAUANG,CIFNO,JUMLAHPENJUALAN
,CONTRACT,X_M_NB FROM [[schema]].MK006_A`

const QueryMurexGetMK005A = `
select DEALNO ,BRANCHTRANSACTION ,BRANCHCOMPLETION
,USERCOMPLETION ,STATUSDOKUMEN ,TRANSACTIONDATE
,FLAGDETAIL ,KODEISIN ,NAMASSB
,JENISSSB ,KATEGORIPENGUKURAN ,NOMINAL
,GOLONGANPIHAKLAWAN  ,GOLONGANPENERBIT ,PERINGKATPENERBITSSB
,LEMBAGAPEMERINGKAT    ,MATAUANG ,SUKUBUNGA
,DURASI   ,TANGGALJATUHTEMPO     ,JENISBUNGA
,CIFNO    ,JUMLAH   ,NAMAPENJUAL
,CONTRACT ,X_M_NB
FROM [[schema]].MK005_A`

const QueryInsertMK006A = `
INSERT INTO SQL_REPLICATE.dbo.mk006_a (
DealNo,BranchTransaction,BranchCompletion
,UserCompletion,StatusDokumen,TransactionDate
,ProcessDate,FlagDetail,KodeISIN
,NamaSSB,KategoriPengukuran,Nominal
,TanggalPembelian,JumlahPembelian,JumlahPenjualan
,NamaPembeli,GolonganPembeli
,MataUang,CIFNo,CONTRACT,X_M_NB)
VALUES (:DealNo,:BranchTransaction,:BranchCompletion
,:UserCompletion,:StatusDokumen,:TransactionDate
,:ProcessDate,:FlagDetail,:KodeISIN
,:NamaSSB,:KategoriPengukuran,:Nominal
,:TanggalPembelian,:JumlahPembelian,:JumlahPenjualan
,:NamaPembeli,:GolonganPembeli
,:MataUang,:CIFNo,:CONTRACT,:X_M_NB)
`

const QueryInsertMK005A = `
INSERT INTO SQL_REPLICATE.dbo.mk005_a (
DealNo,BranchTransaction,BranchCompletion,UserCompletion,StatusDokumen
,TransactionDate,ProcessDate,FlagDetail,KodeISIN,NamaSSB
,JenisSSB,KategoriPengukuran,Nominal,Jumlah,GolonganPihakLawan
,GolonganPenerbit,PeringkatPenerbitSSB,LembagaPemeringkat,MataUang,SukuBunga
,Durasi,TanggalJatuhTempo,JenisBunga,CIFNo,NamaPenjual
,CONTRACT,X_M_NB)
VALUES (:DealNo,:BranchTransaction,:BranchCompletion,:UserCompletion,:StatusDokumen
,:TransactionDate,:ProcessDate,:FlagDetail,:KodeISIN,:NamaSSB
,:JenisSSB,:KategoriPengukuran,:Nominal,:Jumlah,:GolonganPihakLawan
,:GolonganPenerbit,:PeringkatPenerbitSSB,:LembagaPemeringkat,:MataUang,:SukuBunga
,:Durasi,:TanggalJatuhTempo,:JenisBunga,:CIFNo,:NamaPenjual
,:CONTRACT,:X_M_NB)
`

const QueryGetNameSSB = `
declare @dtToday varchar(8)
set @dtToday = convert(varchar(8), getdate(), 112)
select
isnull(CAST(
 STUFF((SELECT DISTINCT ',' + ('''' + CAST(NamaSSB AS VARCHAR(500)) + '''')
 FROM mk006_a
   WHERE convert(varchar(8), ProcessDate, 112) = @dtToday
   GROUP BY NamaSSB
   FOR XML PATH (''))
   , 1, 1, '') AS VARCHAR(8000)), '''''')  AS secid
`

const QueryDeleteAvgPrice = `
declare @dtToday varchar(8)
set @dtToday = convert(varchar(8),  getdate(), 112)
delete mk006_a_actuate
where convert(varchar(8), ProcessDate, 112) = @dtToday
`

const QueryMurexGetAvgPrice = `
SELECT TRIM(SECID) as SECID, ABS(ROUND(SUM(BOOK_VALUE)/SUM(TDPRINTAMT), 17)) AS AVGPRICE
FROM [[schema]].BOND_JOURNAL a
where TRIM(SECID) IN ([[param]])
and TDPRINTAMT > 0
group by SECID
UNION
select TRIM(SECID) as SECID, ABS(ROUND(AVG(PRICE), 17)) AS AVGPRICE
from [[schema]].BOND_DETAIL where trim(SECID) not in (
select distinct trim(SECID) from [[schema]].BOND_JOURNAL where TDPRINTAMT > 0
)
and TRIM(SECID) IN ([[param]])
group by SECID
`
const QueryInsertMK006AActuate = `
INSERT INTO [dbo].[mk006_a_actuate] (secid,avg_price,ProcessDate)
SELECT ?,?,?
`
