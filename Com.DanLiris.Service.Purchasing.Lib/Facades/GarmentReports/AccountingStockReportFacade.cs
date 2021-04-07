using Com.DanLiris.Service.Purchasing.Lib.Helpers;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentDeliveryOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentReports;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentReports
{
    public class AccountingStockReportFacade : IAccountingStockReportFacade
    {
        private readonly PurchasingDbContext dbContext;
        public readonly IServiceProvider serviceProvider;
        private readonly DbSet<GarmentDeliveryOrder> dbSet;

        public AccountingStockReportFacade(IServiceProvider serviceProvider, PurchasingDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<GarmentDeliveryOrder>();
        }

        public Tuple<List<AccountingStockReportViewModel>, int> GetStockReport(int offset, string unitcode, string tipebarang, int page, int size, string Order, DateTime? dateFrom, DateTime? dateTo)
        {
            //var Query = GetStockQuery(tipebarang, unitcode, dateFrom, dateTo, offset);
            //Query = Query.OrderByDescending(x => x.SupplierName).ThenBy(x => x.Dono);
            List<AccountingStockReportViewModel> Data = GetStockQuery(tipebarang, unitcode, dateFrom, dateTo, offset).ToList();
            //Data = Data.Where(x => x.ReceiptCorrectionPrice != 0 || x.ReceiptCorrectionQty != 0 || x.ReceiptKon1APrice != 0 || x.ReceiptKon1AQty != 0 || x.ReceiptKon1BPrice != 0 || x.ReceiptKon1BQty != 0 ||
            //x.ReceiptKon2APrice != 0 || x.ReceiptKon2AQty != 0 || x.ReceiptKon2BPrice != 0 || x.ReceiptKon2BQty != 0 || x.ReceiptKon2CPrice != 0 || x.ReceiptKon2CQty != 0 || x.ReceiptProcessPrice != 0
            //|| x.ReceiptProcessQty != 0 || x.ReceiptPurchasePrice != 0 || x.ReceiptPurchaseQty != 0 || x.ExpendKon1APrice != 0 || x.ExpendKon1AQty != 0 || x.ExpendKon1BPrice != 0 || x.ExpendKon1BQty != 0
            //|| x.ExpendKon2APrice != 0 || x.ExpendKon2AQty != 0 || x.ExpendKon2BPrice != 0 || x.ExpendKon2BQty != 0 || x.ExpendKon2CPrice != 0 || x.ExpendKon2CQty != 0 || x.ExpendProcessPrice != 0 || x.ExpendProcessQty != 0
            //|| x.ExpendRestPrice != 0 || x.ExpendRestQty != 0 || x.ExpendReturPrice != 0 || x.ExpendReturQty != 0 || x.ExpendSamplePrice != 0 || x.ExpendSampleQty != 0).ToList();

            Data = Data.OrderBy(x => x.ProductCode).ThenBy(x => x.PlanPo).ToList();
            //int TotalData = Data.Count();
            return Tuple.Create(Data, Data.Count());
        }
        public List<AccountingStockReportViewModel> GetStockQuery(string ctg, string unitcode, DateTime? datefrom, DateTime? dateto, int offset)
        {
            DateTime DateFrom = datefrom == null ? new DateTime(1970, 1, 1) : (DateTime)datefrom;
            DateTime DateTo = dateto == null ? DateTime.Now : (DateTime)dateto;

            var lastdate = dbContext.BalanceStocks.OrderByDescending(x => x.CreateDate).Select(x => x.CreateDate).FirstOrDefault() == null ? new DateTime(1970, 1, 1) : dbContext.BalanceStocks.OrderByDescending(x => x.CreateDate).Select(x => x.CreateDate).FirstOrDefault();

            var BalaceStock = (from a in dbContext.BalanceStocks
                               join b in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on (long)a.EPOItemId equals b.Id
                               join c in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on b.GarmentEPOId equals c.Id
                               join d in dbContext.GarmentInternalPurchaseOrders on b.POId equals d.Id
                               where a.CreateDate == lastdate

                               group new { a,b,c, d } by new { b.ProductCode, b.ProductName, b.PO_SerialNumber } into data
                               select new AccountingStockReportViewModel
                               {
                                   ProductCode = data.Key.ProductCode,
                                   ProductName = data.Key.ProductName,
                                   RO = data.FirstOrDefault().b.RONo,
                                   Buyer = data.FirstOrDefault().d.BuyerCode,
                                   PlanPo = data.FirstOrDefault().b.PO_SerialNumber,
                                   NoArticle = data.FirstOrDefault().a.ArticleNo,
                                   BeginningBalanceQty = (decimal)data.Sum(x=>x.a.CloseStock),
                                   BeginningBalanceUom = data.FirstOrDefault().b.SmallUomUnit,
                                   BeginningBalancePrice = data.Sum(x => x.a.ClosePrice),
                                   ReceiptCorrectionQty = 0,
                                   ReceiptPurchaseQty =  0,
                                   ReceiptProcessQty = 0,
                                   ReceiptKon2AQty = 0,
                                   ReceiptKon2BQty = 0,
                                   ReceiptKon2CQty = 0,
                                   ReceiptKon1AQty = 0,
                                   ReceiptKon1BQty = 0,
                                   ReceiptCorrectionPrice = 0,
                                   ReceiptPurchasePrice = 0,
                                   ReceiptProcessPrice = 0,
                                   ReceiptKon2APrice = 0,
                                   ReceiptKon2BPrice = 0,
                                   ReceiptKon2CPrice = 0,
                                   ReceiptKon1APrice = 0,
                                   ReceiptKon1BPrice = 0,
                                   ExpendReturQty = 0,
                                   ExpendRestQty = 0,
                                   ExpendProcessQty = 0,
                                   ExpendSampleQty = 0,
                                   ExpendKon2AQty = 0,
                                   ExpendKon2BQty = 0,
                                   ExpendKon2CQty = 0,
                                   ExpendKon1AQty = 0,
                                   ExpendKon1BQty = 0,
                                   ExpendReturPrice = 0,
                                   ExpendRestPrice = 0,
                                   ExpendProcessPrice = 0,
                                   ExpendSamplePrice = 0,
                                   ExpendKon2APrice = 0,
                                   ExpendKon2BPrice = 0,
                                   ExpendKon2CPrice = 0,
                                   ExpendKon1APrice = 0,
                                   ExpendKon1BPrice = 0,
                                   EndingBalanceQty = 0,
                                   EndingBalancePrice = 0,
                                   POId = data.FirstOrDefault().b.POId
                                   //BalanceStockId = data.FirstOrDefault().BalanceStockId,
                                   //ArticleNo = data.FirstOrDefault().ArticleNo,
                                   //EPOID = data.FirstOrDefault().EPOID,
                                   //EPOItemId = data.FirstOrDefault().EPOItemId,
                                   //CloseStock = (double)data.Sum(x => x.CloseStock),
                                   //ClosePrice = (decimal)data.Sum(x => x.ClosePrice)
                               }).ToList();

            List<AccountingStockReportViewModel> penerimaan = new List<AccountingStockReportViewModel>();
            List<AccountingStockReportViewModel> pengeluaran = new List<AccountingStockReportViewModel>();
            List<AccountingStockReportViewModel> penerimaanSA = new List<AccountingStockReportViewModel>();
            List<AccountingStockReportViewModel> pengeluaranSA = new List<AccountingStockReportViewModel>();
            #region SaldoAwal
            var IdSATerima = (from a in dbContext.GarmentUnitReceiptNotes
                              join b in dbContext.GarmentUnitReceiptNoteItems on a.Id equals b.URNId
                              join d in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals d.Id
                              join f in dbContext.GarmentInternalPurchaseOrders on b.POId equals f.Id
                              join c in dbContext.GarmentUnitExpenditureNoteItems on b.UENItemId equals c.Id into UE
                              from ww in UE.DefaultIfEmpty()
                              join r in dbContext.GarmentUnitExpenditureNotes on ww.UENId equals r.Id into UEN
                              from dd in UEN.DefaultIfEmpty()
                              where
                              d.CodeRequirment == (String.IsNullOrWhiteSpace(ctg) ? d.CodeRequirment : ctg)
                              && a.IsDeleted == false && b.IsDeleted == false
                              &&
                              a.CreatedUtc.AddHours(offset).Date > lastdate
                              && a.CreatedUtc.AddHours(offset).Date < DateFrom.Date
                              && a.UnitCode == (string.IsNullOrWhiteSpace(unitcode) ? a.UnitCode : unitcode)

                              select new
                              {
                                  UrnId = a.Id,
                                  UrnItemId = b.Id,
                                  DoDetailId = d.Id,
                                  POID = f.Id,
                                  UENItemsId = ww == null ? 0 : ww.Id,
                                  UENId = dd == null ? 0 : dd.Id,
                                  EPOItemId = b.EPOItemId,
                                  //UENNo = dd == null ? "-" : dd.UENNo,
                                  a.UnitCode
                              }).ToList().Distinct();

            var sapenerimaanunitreceiptnoteids = IdSATerima.Select(x => x.UrnId).ToList();
            var sapenerimaanunitreceiptnotes = dbContext.GarmentUnitReceiptNotes.Where(x => sapenerimaanunitreceiptnoteids.Contains(x.Id)).Select(s => new { s.ReceiptDate, s.URNType, s.UnitCode, s.UENNo, s.Id }).ToList();
            var sapenerimaanunitreceiptnoteItemIds = IdSATerima.Select(x => x.UrnItemId).ToList();
            var sapenerimaanuntreceiptnoteItems = dbContext.GarmentUnitReceiptNoteItems.Where(x => sapenerimaanunitreceiptnoteItemIds.Contains(x.Id)).Select(s => new { s.ProductCode, s.ProductName, s.RONo, s.SmallUomUnit, s.POSerialNumber, s.ReceiptQuantity, s.DOCurrencyRate, s.PricePerDealUnit, s.Id, s.SmallQuantity, s.Conversion }).ToList();
            var sapenerimaandeliveryorderdetailIds = IdSATerima.Select(x => x.DoDetailId).ToList();
            var sapenerimaandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => sapenerimaandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var sapenerimaanintrenalpurchaseorderIds = IdSATerima.Select(x => x.POID).ToList();
            var sapenerimaanintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => sapenerimaanintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            var sapenerimaanUnitExpenditureNoteItemIds = IdSATerima.Select(x => x.UENItemsId).ToList();
            var sapenerimaanUnitExpenditureNoteItems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => sapenerimaanUnitExpenditureNoteItemIds.Contains(x.Id)).Select(s => new { s.Quantity, s.PricePerDealUnit, s.Id, s.Conversion }).ToList();
            var sapenerimaanUnitExpenditureNoteIds = IdSATerima.Select(x => x.UENId).ToList();
            var sapenerimaanUnitExpenditureNotes = dbContext.GarmentUnitExpenditureNotes.Where(x => sapenerimaanUnitExpenditureNoteIds.Contains(x.Id)).Select(s => new { s.UnitSenderCode, s.UnitRequestName, s.ExpenditureTo, s.Id, s.UENNo }).ToList();
            var sapenerimaanExternalPurchaseOrderItemIds = IdSATerima.Select(x => x.EPOItemId).ToList();
            //var sapenerimaanbalancestocks = BalaceStock.Where(x => sapenerimaanExternalPurchaseOrderItemIds.Contains((long)x.EPOItemId)).Select(s => new { s.ArticleNo, s.ClosePrice, s.CloseStock, s.EPOID, s.EPOItemId, s.BalanceStockId }).ToList();
            foreach (var item in IdSATerima)
            {
                var sapenerimaanunitreceiptnote = sapenerimaanunitreceiptnotes.FirstOrDefault(x => x.Id == item.UrnId);
                var sapenerimaanuntreceiptnoteItem = sapenerimaanuntreceiptnoteItems.FirstOrDefault(x => x.Id == item.UrnItemId);
                var sapenerimaandeliveryorderdetail = sapenerimaandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var sapenerimaanintrenalpurchaseorder = sapenerimaanintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);
                var sapenerimaanUnitExpenditureNoteItem = sapenerimaanUnitExpenditureNoteItems.FirstOrDefault(x => x.Id == item.UENItemsId);
                var sapenerimaanUnitExpenditureNote = sapenerimaanUnitExpenditureNotes.FirstOrDefault(x => x.Id == item.UENId);
                //var sapenerimaanbalancestock = sapenerimaanbalancestocks.FirstOrDefault(x => x.EPOItemId == item.EPOItemId);

                penerimaanSA.Add(new AccountingStockReportViewModel
                {
                    ProductCode = sapenerimaanuntreceiptnoteItem.ProductCode,
                    ProductName = sapenerimaanuntreceiptnoteItem.ProductName,
                    RO = sapenerimaanuntreceiptnoteItem.RONo,
                    Buyer = sapenerimaanintrenalpurchaseorder.BuyerCode,
                    PlanPo = sapenerimaanuntreceiptnoteItem.POSerialNumber,
                    NoArticle = sapenerimaanintrenalpurchaseorder.Article,
                    BeginningBalanceQty = 0,
                    BeginningBalanceUom = sapenerimaanuntreceiptnoteItem.SmallUomUnit,
                    BeginningBalancePrice = 0,
                    ReceiptCorrectionQty = 0,
                    ReceiptPurchaseQty = sapenerimaanunitreceiptnote.URNType == "PEMBELIAN" ? (decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * (decimal)sapenerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptProcessQty = sapenerimaanunitreceiptnote.URNType == "PROSES" ? sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon2AQty = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C2A") ? (decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon2BQty = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C2B") ? (decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon2CQty = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C2C") ? (decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon1AQty = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C1A") ? (decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon1BQty = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C1B") ? (decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptCorrectionPrice = 0,
                    ReceiptPurchasePrice = sapenerimaanunitreceiptnote.URNType == "PEMBELIAN" ? Math.Round((decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)sapenerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)sapenerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptProcessPrice = sapenerimaanunitreceiptnote.URNType == "PROSES" ? Math.Round((decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)sapenerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)sapenerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon2APrice = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C2A") ? Math.Round((decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)sapenerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)sapenerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon2BPrice = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C2B") ? Math.Round((decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)sapenerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)sapenerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon2CPrice = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C2C") ? Math.Round((decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)sapenerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)sapenerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon1APrice = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C1A") ? Math.Round((decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)sapenerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)sapenerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon1BPrice = sapenerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (sapenerimaanUnitExpenditureNote == null ? false : sapenerimaanUnitExpenditureNote.UnitSenderCode == "C1B") ? Math.Round((decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)sapenerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)sapenerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ExpendReturQty = 0,
                    ExpendRestQty = 0,
                    ExpendProcessQty = 0,
                    ExpendSampleQty = 0,
                    ExpendKon2AQty = 0,
                    ExpendKon2BQty = 0,
                    ExpendKon2CQty = 0,
                    ExpendKon1AQty = 0,
                    ExpendKon1BQty = 0,
                    ExpendReturPrice = 0,
                    ExpendRestPrice = 0,
                    ExpendProcessPrice = 0,
                    ExpendSamplePrice = 0,
                    ExpendKon2APrice = 0,
                    ExpendKon2BPrice = 0,
                    ExpendKon2CPrice = 0,
                    ExpendKon1APrice = 0,
                    ExpendKon1BPrice = 0,
                    EndingBalanceQty = 0,
                    EndingBalancePrice = 0,
                    POId = sapenerimaanintrenalpurchaseorder.Id
                });

            }

            var IdSAPengeluaran = (from a in dbContext.GarmentUnitExpenditureNotes
                                   join b in dbContext.GarmentUnitExpenditureNoteItems on a.Id equals b.UENId
                                   join c in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals c.Id
                                   join f in dbContext.GarmentInternalPurchaseOrderItems on b.POItemId equals f.Id
                                   join g in dbContext.GarmentInternalPurchaseOrders on f.GPOId equals g.Id
                                   where c.CodeRequirment == (string.IsNullOrWhiteSpace(ctg) ? c.CodeRequirment : ctg)
                                       && a.IsDeleted == false && b.IsDeleted == false
                                       && a.CreatedUtc.AddHours(offset).Date > lastdate.Value.Date
                                        && a.CreatedUtc.AddHours(offset).Date < DateFrom.Date
                                       && a.UnitSenderCode == (string.IsNullOrWhiteSpace(unitcode) ? a.UnitSenderCode : unitcode)

                                   select new
                                   {
                                       UENId = a.Id,
                                       UENItemsId = b.Id,
                                       DoDetailId = c.Id,
                                       POID = g.Id,
                                   }).ToList().Distinct();

            var sapengeluaranUnitExpenditureNoteItemIds = IdSAPengeluaran.Select(x => x.UENItemsId).ToList();
            var sapengeluaranUnitExpenditureNoteItems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => sapengeluaranUnitExpenditureNoteItemIds.Contains(x.Id)).Select(s => new { s.Quantity, s.PricePerDealUnit, s.Id, s.ProductCode, s.ProductName, s.RONo, s.POSerialNumber, s.UomUnit, s.DOCurrencyRate }).ToList();
            var sapengeluaranUnitExpenditureNoteIds = IdSAPengeluaran.Select(x => x.UENId).ToList();
            var sapengeluaranUnitExpenditureNotes = dbContext.GarmentUnitExpenditureNotes.Where(x => sapengeluaranUnitExpenditureNoteIds.Contains(x.Id)).Select(s => new { s.UnitSenderCode, s.UnitRequestName, s.ExpenditureTo, s.Id, s.UENNo, s.ExpenditureType }).ToList();
            var sapengeluarandeliveryorderdetailIds = IdSAPengeluaran.Select(x => x.DoDetailId).ToList();
            var sapengeluarandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => sapengeluarandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var sapengeluaranintrenalpurchaseorderIds = IdSAPengeluaran.Select(x => x.POID).ToList();
            var sapengeluaranintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => sapengeluaranintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            foreach (var item in IdSAPengeluaran)
            {
                var sapengeluarandeliveryorderdetail = sapengeluarandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var sapengeluaranintrenalpurchaseorder = sapengeluaranintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);
                var sapengeluaranUnitExpenditureNoteItem = sapengeluaranUnitExpenditureNoteItems.FirstOrDefault(x => x.Id == item.UENItemsId);
                var sapengeluaranUnitExpenditureNote = sapengeluaranUnitExpenditureNotes.FirstOrDefault(x => x.Id == item.UENId);

                pengeluaranSA.Add(new AccountingStockReportViewModel
                {
                    ProductCode = sapengeluaranUnitExpenditureNoteItem.ProductCode,
                    ProductName = sapengeluaranUnitExpenditureNoteItem.ProductName,
                    RO = sapengeluaranUnitExpenditureNoteItem.RONo,
                    Buyer = sapengeluaranintrenalpurchaseorder.BuyerCode,
                    PlanPo = sapengeluaranUnitExpenditureNoteItem.POSerialNumber,
                    NoArticle = sapengeluaranintrenalpurchaseorder.Article,
                    BeginningBalanceQty = 0,
                    BeginningBalanceUom = sapengeluaranUnitExpenditureNoteItem.UomUnit,
                    BeginningBalancePrice = 0,
                    ReceiptCorrectionQty = 0,
                    ReceiptPurchaseQty = 0,
                    ReceiptProcessQty = 0,
                    ReceiptKon2AQty = 0,
                    ReceiptKon2BQty = 0,
                    ReceiptKon2CQty = 0,
                    ReceiptKon1AQty = 0,
                    ReceiptKon1BQty = 0,
                    ReceiptCorrectionPrice = 0,
                    ReceiptPurchasePrice = 0,
                    ReceiptProcessPrice = 0,
                    ReceiptKon2APrice = 0,
                    ReceiptKon2BPrice = 0,
                    ReceiptKon2CPrice = 0,
                    ReceiptKon1APrice = 0,
                    ReceiptKon1BPrice = 0,
                    ExpendReturQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "EXTERNAL" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendRestQty = 0,
                    ExpendProcessQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "PROSES" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendSampleQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "SAMPLE" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon2AQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon2BQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon2CQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon1AQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon1BQty = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon2AQty = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon2BQty = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon2CQty = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon1AQty = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon1BQty = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? sapengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendReturPrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "PEMBELIAN" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendRestPrice = 0,
                    ExpendProcessPrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "PROSES" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendSamplePrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "SAMPLE" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon2APrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon2BPrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon2CPrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon1APrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon1BPrice = sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon2APrice = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon2BPrice = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon2CPrice = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon1APrice = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon1BPrice = (sapengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || sapengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && sapengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? sapengeluaranUnitExpenditureNoteItem.Quantity * sapengeluaranUnitExpenditureNoteItem.PricePerDealUnit * sapengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    EndingBalanceQty = 0,
                    EndingBalancePrice = 0,
                    POId = sapengeluaranintrenalpurchaseorder.Id
                });
            }

            var SAwal = BalaceStock.Concat(penerimaanSA).Concat(pengeluaranSA).ToList();
            //var SaldoAwal = (from a in SAwal
            //                  group a by new { a.PlanPo, a.ProductCode } into data
            //                  select new AccountingStockReportViewModel
            //                  {
            //                      ProductCode = data.Key.ProductCode,
            //                      ProductName = data.FirstOrDefault().ProductName,
            //                      RO = data.FirstOrDefault().RO,
            //                      Buyer = data.FirstOrDefault().Buyer,
            //                      PlanPo = data.FirstOrDefault().PlanPo,
            //                      NoArticle = data.FirstOrDefault().NoArticle,
            //                      BeginningBalanceQty = Math.Round((decimal)data.Sum(x => x.BeginningBalanceQty) + (decimal)data.Sum(x => x.ReceiptCorrectionQty) + (decimal)data.Sum(x => x.ReceiptPurchaseQty) + (decimal)data.Sum(x => x.ReceiptProcessQty) + (decimal)data.Sum(x => x.ReceiptKon2AQty) + (decimal)data.Sum(x => x.ReceiptKon2BQty) + (decimal)data.Sum(x => x.ReceiptKon2CQty) + (decimal)data.Sum(x => x.ReceiptKon1AQty) + (decimal)data.Sum(x => x.ReceiptKon1BQty) - ((decimal)data.Sum(x => x.ExpendReturQty) + (decimal)data.Sum(x => x.ExpendRestQty) + (decimal)data.Sum(x => x.ExpendProcessQty) + (decimal)data.Sum(x => x.ExpendSampleQty) + (decimal)data.Sum(x => x.ExpendKon2AQty) + (decimal)data.Sum(x => x.ExpendKon2BQty) + (decimal)data.Sum(x => x.ExpendKon2CQty) + (decimal)data.Sum(x => x.ExpendKon1AQty) + (decimal)data.Sum(x => x.ExpendKon1BQty)), 2),
            //                      BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
            //                      BeginningBalancePrice = Math.Round((decimal)data.Sum(x => x.BeginningBalancePrice) + (decimal)data.Sum(x => x.ReceiptCorrectionPrice) + (decimal)data.Sum(x => x.ReceiptPurchasePrice) + (decimal)data.Sum(x => x.ReceiptProcessPrice) + (decimal)data.Sum(x => x.ReceiptKon2APrice) + (decimal)data.Sum(x => x.ReceiptKon2BPrice) + (decimal)data.Sum(x => x.ReceiptKon2CPrice) + (decimal)data.Sum(x => x.ReceiptKon1APrice) + (decimal)data.Sum(x => x.ReceiptKon1BPrice) - ((decimal)data.Sum(x => x.ExpendReturPrice) + (decimal)data.Sum(x => x.ExpendRestPrice) + (decimal)data.Sum(x => x.ExpendProcessPrice) + (decimal)data.Sum(x => x.ExpendSamplePrice) + (decimal)data.Sum(x => x.ExpendKon2APrice) + (decimal)data.Sum(x => x.ExpendKon2BPrice) + (decimal)data.Sum(x => x.ExpendKon2CPrice) + (decimal)data.Sum(x => x.ExpendKon1APrice) + (decimal)data.Sum(x => x.ExpendKon1BPrice)), 2),
            //                      ReceiptCorrectionQty = Math.Round((decimal)data.Sum(x => x.ReceiptCorrectionQty), 2),
            //                      ReceiptPurchaseQty = Math.Round((decimal)data.Sum(x => x.ReceiptPurchaseQty), 2),
            //                      ReceiptProcessQty = Math.Round((decimal)data.Sum(x => x.ReceiptProcessQty), 2),
            //                      ReceiptKon2AQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon2AQty), 2),
            //                      ReceiptKon2BQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon2BQty), 2),
            //                      ReceiptKon2CQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon2CQty), 2),
            //                      ReceiptKon1AQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon1AQty), 2),
            //                      ReceiptKon1BQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon1BQty), 2),
            //                      ReceiptCorrectionPrice = Math.Round((decimal)data.Sum(x => x.ReceiptCorrectionPrice), 2),
            //                      ReceiptPurchasePrice = Math.Round((decimal)data.Sum(x => x.ReceiptPurchasePrice), 2),
            //                      ReceiptProcessPrice = Math.Round((decimal)data.Sum(x => x.ReceiptProcessPrice), 2),
            //                      ReceiptKon2APrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon2APrice), 2),
            //                      ReceiptKon2BPrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon2BPrice), 2),
            //                      ReceiptKon2CPrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon2CPrice), 2),
            //                      ReceiptKon1APrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon1APrice), 2),
            //                      ReceiptKon1BPrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon1BPrice), 2),
            //                      ExpendReturQty = Math.Round((double)data.Sum(x => x.ExpendReturQty), 2),
            //                      ExpendRestQty = Math.Round((double)data.Sum(x => x.ExpendRestQty), 2),
            //                      ExpendProcessQty = Math.Round((double)data.Sum(x => x.ExpendProcessQty), 2),
            //                      ExpendSampleQty = Math.Round((double)data.Sum(x => x.ExpendSampleQty), 2),
            //                      ExpendKon2AQty = Math.Round((double)data.Sum(x => x.ExpendKon2AQty), 2),
            //                      ExpendKon2BQty = Math.Round((double)data.Sum(x => x.ExpendKon2BQty), 2),
            //                      ExpendKon2CQty = Math.Round((double)data.Sum(x => x.ExpendKon2CQty), 2),
            //                      ExpendKon1AQty = Math.Round((double)data.Sum(x => x.ExpendKon1AQty), 2),
            //                      ExpendKon1BQty = Math.Round((double)data.Sum(x => x.ExpendKon1BQty), 2),
            //                      ExpendReturPrice = Math.Round((double)data.Sum(x => x.ExpendReturPrice), 2),
            //                      ExpendRestPrice = Math.Round((double)data.Sum(x => x.ExpendRestPrice), 2),
            //                      ExpendProcessPrice = Math.Round((double)data.Sum(x => x.ExpendProcessPrice), 2),
            //                      ExpendSamplePrice = Math.Round((double)data.Sum(x => x.ExpendSamplePrice), 2),
            //                      ExpendKon2APrice = Math.Round((double)data.Sum(x => x.ExpendKon2APrice), 2),
            //                      ExpendKon2BPrice = Math.Round((double)data.Sum(x => x.ExpendKon2BPrice), 2),
            //                      ExpendKon2CPrice = Math.Round((double)data.Sum(x => x.ExpendKon2CPrice), 2),
            //                      ExpendKon1APrice = Math.Round((double)data.Sum(x => x.ExpendKon1APrice), 2),
            //                      ExpendKon1BPrice = Math.Round((double)data.Sum(x => x.ExpendKon1BPrice), 2),
            //                      //EndingBalanceQty = Math.Round((decimal)data.Sum(x => x.BeginningBalanceQty) + (decimal)data.Sum(x => x.ReceiptCorrectionQty) + (decimal)data.Sum(x => x.ReceiptPurchaseQty) + (decimal)data.Sum(x => x.ReceiptProcessQty) + (decimal)data.Sum(x => x.ReceiptKon2AQty) + (decimal)data.Sum(x => x.ReceiptKon2BQty) + (decimal)data.Sum(x => x.ReceiptKon2CQty) + (decimal)data.Sum(x => x.ReceiptKon1AQty) + (decimal)data.Sum(x => x.ReceiptKon1BQty) - ((decimal)data.Sum(x => x.ExpendReturQty) + (decimal)data.Sum(x => x.ExpendRestQty) + (decimal)data.Sum(x => x.ExpendProcessQty) + (decimal)data.Sum(x=>x.ExpendSampleQty) + (decimal)data.Sum(x => x.ExpendKon2AQty) + (decimal)data.Sum(x => x.ExpendKon2BQty) + (decimal)data.Sum(x => x.ExpendKon2CQty) + (decimal)data.Sum(x => x.ExpendKon1AQty) + (decimal)data.Sum(x => x.ExpendKon1BQty)), 2),
            //                      EndingBalanceQty = 0,
            //                      //EndingBalancePrice = Math.Round((decimal)data.Sum(x => x.BeginningBalancePrice) + (decimal)data.Sum(x => x.ReceiptCorrectionPrice) + (decimal)data.Sum(x => x.ReceiptPurchasePrice) + (decimal)data.Sum(x => x.ReceiptProcessPrice) + (decimal)data.Sum(x => x.ReceiptKon2APrice) + (decimal)data.Sum(x => x.ReceiptKon2BPrice) + (decimal)data.Sum(x => x.ReceiptKon2CPrice) + (decimal)data.Sum(x => x.ReceiptKon1APrice) + (decimal)data.Sum(x => x.ReceiptKon1BPrice) - ((decimal)data.Sum(x => x.ExpendReturPrice) + (decimal)data.Sum(x => x.ExpendRestPrice) + (decimal)data.Sum(x => x.ExpendProcessPrice) + (decimal)data.Sum(x => x.ExpendSamplePrice) + (decimal)data.Sum(x => x.ExpendKon2APrice) + (decimal)data.Sum(x => x.ExpendKon2BPrice) + (decimal)data.Sum(x => x.ExpendKon2CPrice) + (decimal)data.Sum(x => x.ExpendKon1APrice) + (decimal)data.Sum(x => x.ExpendKon1BPrice)), 2),
            //                      EndingBalancePrice = 0,
            //                      POId = data.FirstOrDefault().POId
            //                  }).ToList();
            var SaldoAwal = (from a in SAwal
                             group a by new { a.PlanPo, a.ProductCode } into data
                             select new AccountingStockReportViewModel
                             {
                                 ProductCode = data.Key.ProductCode,
                                 ProductName = data.FirstOrDefault().ProductName,
                                 RO = data.FirstOrDefault().RO,
                                 Buyer = data.FirstOrDefault().Buyer,
                                 PlanPo = data.FirstOrDefault().PlanPo,
                                 NoArticle = data.FirstOrDefault().NoArticle,
                                 BeginningBalanceQty = Math.Round((decimal)data.Sum(x => x.BeginningBalanceQty) + (decimal)data.Sum(x => x.ReceiptCorrectionQty) + (decimal)data.Sum(x => x.ReceiptPurchaseQty) + (decimal)data.Sum(x => x.ReceiptProcessQty) + (decimal)data.Sum(x => x.ReceiptKon2AQty) + (decimal)data.Sum(x => x.ReceiptKon2BQty) + (decimal)data.Sum(x => x.ReceiptKon2CQty) + (decimal)data.Sum(x => x.ReceiptKon1AQty) + (decimal)data.Sum(x => x.ReceiptKon1BQty) - ((decimal)data.Sum(x => x.ExpendReturQty) + (decimal)data.Sum(x => x.ExpendRestQty) + (decimal)data.Sum(x => x.ExpendProcessQty) + (decimal)data.Sum(x => x.ExpendSampleQty) + (decimal)data.Sum(x => x.ExpendKon2AQty) + (decimal)data.Sum(x => x.ExpendKon2BQty) + (decimal)data.Sum(x => x.ExpendKon2CQty) + (decimal)data.Sum(x => x.ExpendKon1AQty) + (decimal)data.Sum(x => x.ExpendKon1BQty)), 2),
                                 BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                                 BeginningBalancePrice = Math.Round((decimal)data.Sum(x => x.BeginningBalancePrice) + (decimal)data.Sum(x => x.ReceiptCorrectionPrice) + (decimal)data.Sum(x => x.ReceiptPurchasePrice) + (decimal)data.Sum(x => x.ReceiptProcessPrice) + (decimal)data.Sum(x => x.ReceiptKon2APrice) + (decimal)data.Sum(x => x.ReceiptKon2BPrice) + (decimal)data.Sum(x => x.ReceiptKon2CPrice) + (decimal)data.Sum(x => x.ReceiptKon1APrice) + (decimal)data.Sum(x => x.ReceiptKon1BPrice) - ((decimal)data.Sum(x => x.ExpendReturPrice) + (decimal)data.Sum(x => x.ExpendRestPrice) + (decimal)data.Sum(x => x.ExpendProcessPrice) + (decimal)data.Sum(x => x.ExpendSamplePrice) + (decimal)data.Sum(x => x.ExpendKon2APrice) + (decimal)data.Sum(x => x.ExpendKon2BPrice) + (decimal)data.Sum(x => x.ExpendKon2CPrice) + (decimal)data.Sum(x => x.ExpendKon1APrice) + (decimal)data.Sum(x => x.ExpendKon1BPrice)), 2),
                                 ReceiptCorrectionQty = 0,
                                 ReceiptPurchaseQty = 0,
                                 ReceiptProcessQty = 0,
                                 ReceiptKon2AQty = 0,
                                 ReceiptKon2BQty = 0,
                                 ReceiptKon2CQty = 0,
                                 ReceiptKon1AQty = 0,
                                 ReceiptKon1BQty = 0,
                                 ReceiptCorrectionPrice = 0,
                                 ReceiptPurchasePrice = 0,
                                 ReceiptProcessPrice = 0,
                                 ReceiptKon2APrice = 0,
                                 ReceiptKon2BPrice = 0,
                                 ReceiptKon2CPrice = 0,
                                 ReceiptKon1APrice = 0,
                                 ReceiptKon1BPrice = 0,
                                 ExpendReturQty = 0,
                                 ExpendRestQty = 0,
                                 ExpendProcessQty = 0,
                                 ExpendSampleQty = 0,
                                 ExpendKon2AQty = 0,
                                 ExpendKon2BQty = 0,
                                 ExpendKon2CQty = 0,
                                 ExpendKon1AQty = 0,
                                 ExpendKon1BQty = 0,
                                 ExpendReturPrice = 0,
                                 ExpendRestPrice = 0,
                                 ExpendProcessPrice = 0,
                                 ExpendSamplePrice = 0,
                                 ExpendKon2APrice = 0,
                                 ExpendKon2BPrice = 0,
                                 ExpendKon2CPrice = 0,
                                 ExpendKon1APrice = 0,
                                 ExpendKon1BPrice = 0,
                                 //EndingBalanceQty = Math.Round((decimal)data.Sum(x => x.BeginningBalanceQty) + (decimal)data.Sum(x => x.ReceiptCorrectionQty) + (decimal)data.Sum(x => x.ReceiptPurchaseQty) + (decimal)data.Sum(x => x.ReceiptProcessQty) + (decimal)data.Sum(x => x.ReceiptKon2AQty) + (decimal)data.Sum(x => x.ReceiptKon2BQty) + (decimal)data.Sum(x => x.ReceiptKon2CQty) + (decimal)data.Sum(x => x.ReceiptKon1AQty) + (decimal)data.Sum(x => x.ReceiptKon1BQty) - ((decimal)data.Sum(x => x.ExpendReturQty) + (decimal)data.Sum(x => x.ExpendRestQty) + (decimal)data.Sum(x => x.ExpendProcessQty) + (decimal)data.Sum(x=>x.ExpendSampleQty) + (decimal)data.Sum(x => x.ExpendKon2AQty) + (decimal)data.Sum(x => x.ExpendKon2BQty) + (decimal)data.Sum(x => x.ExpendKon2CQty) + (decimal)data.Sum(x => x.ExpendKon1AQty) + (decimal)data.Sum(x => x.ExpendKon1BQty)), 2),
                                 EndingBalanceQty = 0,
                                 //EndingBalancePrice = Math.Round((decimal)data.Sum(x => x.BeginningBalancePrice) + (decimal)data.Sum(x => x.ReceiptCorrectionPrice) + (decimal)data.Sum(x => x.ReceiptPurchasePrice) + (decimal)data.Sum(x => x.ReceiptProcessPrice) + (decimal)data.Sum(x => x.ReceiptKon2APrice) + (decimal)data.Sum(x => x.ReceiptKon2BPrice) + (decimal)data.Sum(x => x.ReceiptKon2CPrice) + (decimal)data.Sum(x => x.ReceiptKon1APrice) + (decimal)data.Sum(x => x.ReceiptKon1BPrice) - ((decimal)data.Sum(x => x.ExpendReturPrice) + (decimal)data.Sum(x => x.ExpendRestPrice) + (decimal)data.Sum(x => x.ExpendProcessPrice) + (decimal)data.Sum(x => x.ExpendSamplePrice) + (decimal)data.Sum(x => x.ExpendKon2APrice) + (decimal)data.Sum(x => x.ExpendKon2BPrice) + (decimal)data.Sum(x => x.ExpendKon2CPrice) + (decimal)data.Sum(x => x.ExpendKon1APrice) + (decimal)data.Sum(x => x.ExpendKon1BPrice)), 2),
                                 EndingBalancePrice = 0,
                                 POId = data.FirstOrDefault().POId
                             }).ToList();
            #endregion
            var IdTerima = (from a in dbContext.GarmentUnitReceiptNotes
                            join b in dbContext.GarmentUnitReceiptNoteItems on a.Id equals b.URNId
                            join d in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals d.Id
                            join f in dbContext.GarmentInternalPurchaseOrders on b.POId equals f.Id
                            join c in dbContext.GarmentUnitExpenditureNoteItems on b.UENItemId equals c.Id into UE
                            from ww in UE.DefaultIfEmpty()
                            join r in dbContext.GarmentUnitExpenditureNotes on ww.UENId equals r.Id into UEN
                            from dd in UEN.DefaultIfEmpty()
                            where d.CodeRequirment == (String.IsNullOrWhiteSpace(ctg) ? d.CodeRequirment : ctg)
                            && a.IsDeleted == false && b.IsDeleted == false
                            && a.CreatedUtc.AddHours(offset).Date >= DateFrom.Date
                            && a.CreatedUtc.AddHours(offset).Date <= DateTo.Date
                            && a.UnitCode == (string.IsNullOrWhiteSpace(unitcode) ? a.UnitCode : unitcode)

                            select new
                            {
                                UrnId = a.Id,
                                UrnItemId = b.Id,
                                DoDetailId = d.Id,
                                POID = f.Id,
                                UENItemsId = ww == null ? 0 : ww.Id,
                                UENId = dd == null ? 0 : dd.Id,
                                EPOItemId = b.EPOItemId,
                                //UENNo = dd == null ? "-" : dd.UENNo,
                                a.UnitCode
                            }).ToList().Distinct();
            var penerimaanunitreceiptnoteids = IdTerima.Select(x => x.UrnId).ToList();
            var penerimaanunitreceiptnotes = dbContext.GarmentUnitReceiptNotes.Where(x => penerimaanunitreceiptnoteids.Contains(x.Id)).Select(s => new { s.ReceiptDate, s.URNType, s.UnitCode, s.UENNo, s.Id }).ToList();
            var penerimaanunitreceiptnoteItemIds = IdTerima.Select(x => x.UrnItemId).ToList();
            var penerimaanuntreceiptnoteItems = dbContext.GarmentUnitReceiptNoteItems.Where(x => penerimaanunitreceiptnoteItemIds.Contains(x.Id)).Select(s => new { s.ProductCode, s.ProductName, s.RONo, s.SmallUomUnit, s.POSerialNumber, s.ReceiptQuantity, s.DOCurrencyRate, s.PricePerDealUnit, s.Id, s.SmallQuantity, s.Conversion }).ToList();
            var penerimaandeliveryorderdetailIds = IdTerima.Select(x => x.DoDetailId).ToList();
            var penerimaandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => penerimaandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var penerimaanintrenalpurchaseorderIds = IdTerima.Select(x => x.POID).ToList();
            var penerimaanintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => penerimaanintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            var penerimaanUnitExpenditureNoteItemIds = IdTerima.Select(x => x.UENItemsId).ToList();
            var penerimaanUnitExpenditureNoteItems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => penerimaanUnitExpenditureNoteItemIds.Contains(x.Id)).Select(s => new { s.Quantity, s.PricePerDealUnit, s.Id }).ToList();
            var penerimaanUnitExpenditureNoteIds = IdTerima.Select(x => x.UENId).ToList();
            var penerimaanUnitExpenditureNotes = dbContext.GarmentUnitExpenditureNotes.Where(x => penerimaanUnitExpenditureNoteIds.Contains(x.Id)).Select(s => new { s.UnitSenderCode, s.UnitRequestName, s.ExpenditureTo, s.Id, s.UENNo }).ToList();
            var penerimaanExternalPurchaseOrderItemIds = IdTerima.Select(x => x.EPOItemId).ToList();
            //var penerimaanbalancestocks = BalaceStock.Where(x => penerimaanExternalPurchaseOrderItemIds.Contains((long)x.EPOItemId)).Select(s => new { s.ArticleNo, s.ClosePrice, s.CloseStock, s.EPOID, s.EPOItemId, s.BalanceStockId }).ToList();
            foreach (var item in IdTerima) {
                var penerimaanunitreceiptnote = penerimaanunitreceiptnotes.FirstOrDefault(x => x.Id == item.UrnId);
                var penerimaanuntreceiptnoteItem = penerimaanuntreceiptnoteItems.FirstOrDefault(x => x.Id == item.UrnItemId);
                var penerimaandeliveryorderdetail = penerimaandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var penerimaanintrenalpurchaseorder = penerimaanintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);
                var penerimaanUnitExpenditureNoteItem = penerimaanUnitExpenditureNoteItems.FirstOrDefault(x => x.Id == item.UENItemsId);
                var penerimaanUnitExpenditureNote = penerimaanUnitExpenditureNotes.FirstOrDefault(x => x.Id == item.UENId);
                //var penerimaanbalancestock = penerimaanbalancestocks.FirstOrDefault(x => x.EPOItemId == item.EPOItemId);

                penerimaan.Add(new AccountingStockReportViewModel
                {
                    ProductCode = penerimaanuntreceiptnoteItem.ProductCode,
                    ProductName = penerimaanuntreceiptnoteItem.ProductName,
                    RO = penerimaanuntreceiptnoteItem.RONo,
                    Buyer = penerimaanintrenalpurchaseorder.BuyerCode,
                    PlanPo = penerimaanuntreceiptnoteItem.POSerialNumber,
                    NoArticle = penerimaanintrenalpurchaseorder.Article,
                    BeginningBalanceQty = /*penerimaanbalancestock != null ? (decimal)penerimaanbalancestock.CloseStock : */0,
                    BeginningBalanceUom = penerimaanuntreceiptnoteItem.SmallUomUnit,
                    BeginningBalancePrice = /*penerimaanbalancestock != null ? (decimal)penerimaanbalancestock.ClosePrice : */0,
                    ReceiptCorrectionQty = 0,
                    ReceiptPurchaseQty = penerimaanunitreceiptnote.URNType == "PEMBELIAN" ? (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptProcessQty = penerimaanunitreceiptnote.URNType == "PROSES" ? (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon2AQty = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C2A") ? (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon2BQty = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C2B") ? (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon2CQty = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C2C") ? (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon1AQty = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C1A") ? (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptKon1BQty = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C1B") ? (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion : 0,
                    ReceiptCorrectionPrice = 0,
                    ReceiptPurchasePrice = penerimaanunitreceiptnote.URNType == "PEMBELIAN" ? Math.Round((decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)penerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)penerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptProcessPrice = penerimaanunitreceiptnote.URNType == "PROSES" ? Math.Round((decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)penerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)penerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon2APrice = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C2A") ? Math.Round((decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)penerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)penerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon2BPrice = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C2B") ? Math.Round((decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)penerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)penerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon2CPrice = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C2C") ? Math.Round((decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)penerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)penerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon1APrice = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C1A") ? Math.Round((decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)penerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)penerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ReceiptKon1BPrice = penerimaanunitreceiptnote.URNType == "GUDANG LAIN" && (penerimaanUnitExpenditureNote == null ? false : penerimaanUnitExpenditureNote.UnitSenderCode == "C1B") ? Math.Round((decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion, 2) * (decimal)penerimaanuntreceiptnoteItem.DOCurrencyRate * (decimal)penerimaanuntreceiptnoteItem.PricePerDealUnit : 0,
                    ExpendReturQty = 0,
                    ExpendRestQty = 0,
                    ExpendProcessQty = 0,
                    ExpendSampleQty = 0,
                    ExpendKon2AQty = 0,
                    ExpendKon2BQty = 0,
                    ExpendKon2CQty = 0,
                    ExpendKon1AQty = 0,
                    ExpendKon1BQty = 0,
                    ExpendReturPrice = 0,
                    ExpendRestPrice = 0,
                    ExpendProcessPrice = 0,
                    ExpendSamplePrice = 0,
                    ExpendKon2APrice = 0,
                    ExpendKon2BPrice = 0,
                    ExpendKon2CPrice = 0,
                    ExpendKon1APrice = 0,
                    ExpendKon1BPrice = 0,
                    EndingBalanceQty = 0,
                    EndingBalancePrice = 0,
                    POId = penerimaanintrenalpurchaseorder.Id
                });

            }
            var IdPengeluaran = (from a in dbContext.GarmentUnitExpenditureNotes
                                 join b in dbContext.GarmentUnitExpenditureNoteItems on a.Id equals b.UENId
                                 join c in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals c.Id
                                 join f in dbContext.GarmentInternalPurchaseOrderItems on b.POItemId equals f.Id
                                 join g in dbContext.GarmentInternalPurchaseOrders on f.GPOId equals g.Id
                                 where c.CodeRequirment == (String.IsNullOrWhiteSpace(ctg) ? c.CodeRequirment : ctg)
                                     && a.IsDeleted == false && b.IsDeleted == false
                                     && a.CreatedUtc.AddHours(offset).Date >= DateFrom.Date
                                     && a.CreatedUtc.AddHours(offset).Date <= DateTo.Date
                                     && a.UnitSenderCode == (string.IsNullOrWhiteSpace(unitcode) ? a.UnitSenderCode : unitcode)

                                 select new
                                 {
                                     UENId = a.Id,
                                     UENItemsId = b.Id,
                                     DoDetailId = c.Id,
                                     POID = g.Id,
                                 }).ToList().Distinct();
            var pengeluaranUnitExpenditureNoteItemIds = IdPengeluaran.Select(x => x.UENItemsId).ToList();
            var pengeluaranUnitExpenditureNoteItems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => pengeluaranUnitExpenditureNoteItemIds.Contains(x.Id)).Select(s => new { s.Quantity, s.PricePerDealUnit, s.Id, s.ProductCode, s.ProductName, s.RONo, s.POSerialNumber, s.UomUnit, s.DOCurrencyRate }).ToList();
            var pengeluaranUnitExpenditureNoteIds = IdPengeluaran.Select(x => x.UENId).ToList();
            var pengeluaranUnitExpenditureNotes = dbContext.GarmentUnitExpenditureNotes.Where(x => pengeluaranUnitExpenditureNoteIds.Contains(x.Id)).Select(s => new { s.UnitSenderCode, s.UnitRequestName, s.ExpenditureTo, s.Id, s.UENNo, s.ExpenditureType }).ToList();
            var pengeluarandeliveryorderdetailIds = IdPengeluaran.Select(x => x.DoDetailId).ToList();
            var pengeluarandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => pengeluarandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var pengeluaranintrenalpurchaseorderIds = IdPengeluaran.Select(x => x.POID).ToList();
            var pengeluaranintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => pengeluaranintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            foreach (var item in IdPengeluaran) {
                var pengeluarandeliveryorderdetail = pengeluarandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var pengeluaranintrenalpurchaseorder = pengeluaranintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);
                var pengeluaranUnitExpenditureNoteItem = pengeluaranUnitExpenditureNoteItems.FirstOrDefault(x => x.Id == item.UENItemsId);
                var pengeluaranUnitExpenditureNote = pengeluaranUnitExpenditureNotes.FirstOrDefault(x => x.Id == item.UENId);

                pengeluaran.Add(new AccountingStockReportViewModel
                {
                    ProductCode = pengeluaranUnitExpenditureNoteItem.ProductCode,
                    ProductName = pengeluaranUnitExpenditureNoteItem.ProductName,
                    RO = pengeluaranUnitExpenditureNoteItem.RONo,
                    Buyer = pengeluaranintrenalpurchaseorder.BuyerCode,
                    PlanPo = pengeluaranUnitExpenditureNoteItem.POSerialNumber,
                    NoArticle = pengeluaranintrenalpurchaseorder.Article,
                    BeginningBalanceQty = 0,
                    BeginningBalanceUom = pengeluaranUnitExpenditureNoteItem.UomUnit,
                    BeginningBalancePrice = 0,
                    ReceiptCorrectionQty = 0,
                    ReceiptPurchaseQty =  0,
                    ReceiptProcessQty = 0,
                    ReceiptKon2AQty = 0,
                    ReceiptKon2BQty = 0,
                    ReceiptKon2CQty = 0,
                    ReceiptKon1AQty = 0,
                    ReceiptKon1BQty = 0,
                    ReceiptCorrectionPrice = 0,
                    ReceiptPurchasePrice = 0,
                    ReceiptProcessPrice = 0,
                    ReceiptKon2APrice = 0,
                    ReceiptKon2BPrice = 0,
                    ReceiptKon2CPrice = 0,
                    ReceiptKon1APrice = 0,
                    ReceiptKon1BPrice = 0,
                    ExpendReturQty = pengeluaranUnitExpenditureNote.ExpenditureType == "EXTERNAL" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendRestQty = 0,
                    ExpendProcessQty = pengeluaranUnitExpenditureNote.ExpenditureType == "PROSES" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendSampleQty = pengeluaranUnitExpenditureNote.ExpenditureType == "SAMPLE" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon2AQty = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon2BQty = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon2CQty = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon1AQty = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    //ExpendKon1BQty = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon2AQty = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon2BQty = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon2CQty = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon1AQty = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendKon1BQty = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? pengeluaranUnitExpenditureNoteItem.Quantity : 0,
                    ExpendReturPrice = pengeluaranUnitExpenditureNote.ExpenditureType == "PEMBELIAN" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendRestPrice = 0,
                    ExpendProcessPrice = pengeluaranUnitExpenditureNote.ExpenditureType == "PROSES" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendSamplePrice = pengeluaranUnitExpenditureNote.ExpenditureType == "SAMPLE" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon2APrice = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon2BPrice = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon2CPrice = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon1APrice = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    //ExpendKon1BPrice = pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon2APrice = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2A" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon2BPrice = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2B" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon2CPrice = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 2C/EX. K4" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon1APrice = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1A/EX. K3" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    ExpendKon1BPrice = (pengeluaranUnitExpenditureNote.ExpenditureType == "GUDANG LAIN" || pengeluaranUnitExpenditureNote.ExpenditureType == "TRANSFER") && pengeluaranUnitExpenditureNote.UnitRequestName == "CENTRAL 1B" ? pengeluaranUnitExpenditureNoteItem.Quantity * pengeluaranUnitExpenditureNoteItem.PricePerDealUnit * pengeluaranUnitExpenditureNoteItem.DOCurrencyRate : 0,
                    EndingBalanceQty = 0,
                    EndingBalancePrice = 0,
                    POId = pengeluaranintrenalpurchaseorder.Id
                });
            }

            var SAkhir = penerimaan.Concat(pengeluaran).ToList();
            var SaldoAkhir = (from a in SAkhir
                             group a by new { a.PlanPo, a.ProductCode } into data
                             select new AccountingStockReportViewModel
                             {
                                 ProductCode = data.Key.ProductCode,
                                 ProductName = data.FirstOrDefault().ProductName,
                                 RO = data.FirstOrDefault().RO,
                                 Buyer = data.FirstOrDefault().Buyer,
                                 PlanPo = data.FirstOrDefault().PlanPo,
                                 NoArticle = data.FirstOrDefault().NoArticle,
                                 BeginningBalanceQty = Math.Round((decimal)data.Sum(x => x.BeginningBalanceQty), 2),
                                 BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                                 BeginningBalancePrice = Math.Round((decimal)data.Sum(x => x.BeginningBalancePrice), 2),
                                 ReceiptCorrectionQty = Math.Round((decimal)data.Sum(x => x.ReceiptCorrectionQty), 2),
                                 ReceiptPurchaseQty = Math.Round((decimal)data.Sum(x => x.ReceiptPurchaseQty), 2),
                                 ReceiptProcessQty = Math.Round((decimal)data.Sum(x => x.ReceiptProcessQty), 2),
                                 ReceiptKon2AQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon2AQty), 2),
                                 ReceiptKon2BQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon2BQty), 2),
                                 ReceiptKon2CQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon2CQty), 2),
                                 ReceiptKon1AQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon1AQty), 2),
                                 ReceiptKon1BQty = Math.Round((decimal)data.Sum(x => x.ReceiptKon1BQty), 2),
                                 ReceiptCorrectionPrice = Math.Round((decimal)data.Sum(x => x.ReceiptCorrectionPrice), 2),
                                 ReceiptPurchasePrice = Math.Round((decimal)data.Sum(x => x.ReceiptPurchasePrice), 2),
                                 ReceiptProcessPrice = Math.Round((decimal)data.Sum(x => x.ReceiptProcessPrice), 2),
                                 ReceiptKon2APrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon2APrice), 2),
                                 ReceiptKon2BPrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon2BPrice), 2),
                                 ReceiptKon2CPrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon2CPrice), 2),
                                 ReceiptKon1APrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon1APrice), 2),
                                 ReceiptKon1BPrice = Math.Round((decimal)data.Sum(x => x.ReceiptKon1BPrice), 2),
                                 ExpendReturQty = Math.Round((double)data.Sum(x => x.ExpendReturQty), 2),
                                 ExpendRestQty = Math.Round((double)data.Sum(x => x.ExpendRestQty), 2),
                                 ExpendProcessQty = Math.Round((double)data.Sum(x => x.ExpendProcessQty), 2),
                                 ExpendSampleQty = Math.Round((double)data.Sum(x => x.ExpendSampleQty), 2),
                                 ExpendKon2AQty = Math.Round((double)data.Sum(x => x.ExpendKon2AQty), 2),
                                 ExpendKon2BQty = Math.Round((double)data.Sum(x => x.ExpendKon2BQty), 2),
                                 ExpendKon2CQty = Math.Round((double)data.Sum(x => x.ExpendKon2CQty), 2),
                                 ExpendKon1AQty = Math.Round((double)data.Sum(x => x.ExpendKon1AQty), 2),
                                 ExpendKon1BQty = Math.Round((double)data.Sum(x => x.ExpendKon1BQty), 2),
                                 ExpendReturPrice = Math.Round((double)data.Sum(x => x.ExpendReturPrice), 2),
                                 ExpendRestPrice = Math.Round((double)data.Sum(x => x.ExpendRestPrice), 2),
                                 ExpendProcessPrice = Math.Round((double)data.Sum(x => x.ExpendProcessPrice), 2),
                                 ExpendSamplePrice = Math.Round((double)data.Sum(x => x.ExpendSamplePrice), 2),
                                 ExpendKon2APrice = Math.Round((double)data.Sum(x => x.ExpendKon2APrice), 2),
                                 ExpendKon2BPrice = Math.Round((double)data.Sum(x => x.ExpendKon2BPrice), 2),
                                 ExpendKon2CPrice = Math.Round((double)data.Sum(x => x.ExpendKon2CPrice), 2),
                                 ExpendKon1APrice = Math.Round((double)data.Sum(x => x.ExpendKon1APrice), 2),
                                 ExpendKon1BPrice = Math.Round((double)data.Sum(x => x.ExpendKon1BPrice), 2),
                                 //EndingBalanceQty = Math.Round((decimal)data.Sum(x => x.BeginningBalanceQty) + (decimal)data.Sum(x => x.ReceiptCorrectionQty) + (decimal)data.Sum(x => x.ReceiptPurchaseQty) + (decimal)data.Sum(x => x.ReceiptProcessQty) + (decimal)data.Sum(x => x.ReceiptKon2AQty) + (decimal)data.Sum(x => x.ReceiptKon2BQty) + (decimal)data.Sum(x => x.ReceiptKon2CQty) + (decimal)data.Sum(x => x.ReceiptKon1AQty) + (decimal)data.Sum(x => x.ReceiptKon1BQty) - ((decimal)data.Sum(x => x.ExpendReturQty) + (decimal)data.Sum(x => x.ExpendRestQty) + (decimal)data.Sum(x => x.ExpendProcessQty) + (decimal)data.Sum(x => x.ExpendKon2AQty) + (decimal)data.Sum(x => x.ExpendKon2BQty) + (decimal)data.Sum(x => x.ExpendKon2CQty) + (decimal)data.Sum(x => x.ExpendKon1AQty) + (decimal)data.Sum(x => x.ExpendKon1BQty)), 2),
                                 EndingBalanceQty = 0,
                                 //EndingBalancePrice = Math.Round((decimal)data.Sum(x => x.BeginningBalancePrice) + (decimal)data.Sum(x => x.ReceiptCorrectionPrice) + (decimal)data.Sum(x => x.ReceiptPurchasePrice) + (decimal)data.Sum(x => x.ReceiptProcessPrice) + (decimal)data.Sum(x => x.ReceiptKon2APrice) + (decimal)data.Sum(x => x.ReceiptKon2BPrice) + (decimal)data.Sum(x => x.ReceiptKon2CPrice) + (decimal)data.Sum(x => x.ReceiptKon1APrice) + (decimal)data.Sum(x => x.ReceiptKon1BPrice) - ((decimal)data.Sum(x => x.ExpendReturPrice) + (decimal)data.Sum(x => x.ExpendRestPrice) + (decimal)data.Sum(x => x.ExpendProcessPrice) + (decimal)data.Sum(x => x.ExpendSamplePrice) + (decimal)data.Sum(x => x.ExpendKon2APrice) + (decimal)data.Sum(x => x.ExpendKon2BPrice) + (decimal)data.Sum(x => x.ExpendKon2CPrice) + (decimal)data.Sum(x => x.ExpendKon1APrice) + (decimal)data.Sum(x => x.ExpendKon1BPrice)), 2),
                                 EndingBalancePrice = 0,
                                 POId = data.FirstOrDefault().POId


                             }).ToList();

            var SaldoAkhirs2 = SaldoAwal.Concat(SaldoAkhir).ToList();
            var SaldoAkhirs = from a in SaldoAkhirs2
                              group a by new { a.PlanPo, a.ProductCode } into data
                              select new AccountingStockReportViewModel
                              {
                                  ProductCode = data.Key.ProductCode,
                                  ProductName = data.FirstOrDefault().ProductName,
                                  RO = data.FirstOrDefault().RO,
                                  Buyer = data.FirstOrDefault().Buyer,
                                  PlanPo = data.Key.PlanPo,
                                  NoArticle = data.FirstOrDefault().NoArticle,
                                  BeginningBalanceQty = data.Sum(x => x.BeginningBalanceQty),
                                  BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                                  BeginningBalancePrice = data.Sum(x => x.BeginningBalancePrice),
                                  ReceiptCorrectionQty = data.Sum(x => x.ReceiptCorrectionQty),
                                  ReceiptPurchaseQty = data.Sum(x => x.ReceiptPurchaseQty),
                                  ReceiptProcessQty = data.Sum(x => x.ReceiptProcessQty),
                                  ReceiptKon2AQty = data.Sum(x => x.ReceiptKon2AQty),
                                  ReceiptKon2BQty = data.Sum(x => x.ReceiptKon2BQty),
                                  ReceiptKon2CQty = data.Sum(x => x.ReceiptKon2CQty),
                                  ReceiptKon1AQty = data.Sum(x => x.ReceiptKon1AQty),
                                  ReceiptKon1BQty = data.Sum(x => x.ReceiptKon1BQty),
                                  ReceiptCorrectionPrice = data.Sum(x => x.ReceiptCorrectionPrice),
                                  ReceiptPurchasePrice = data.Sum(x => x.ReceiptPurchasePrice),
                                  ReceiptProcessPrice = data.Sum(x => x.ReceiptProcessPrice),
                                  ReceiptKon2APrice = data.Sum(x => x.ReceiptKon2APrice),
                                  ReceiptKon2BPrice = data.Sum(x => x.ReceiptKon2BPrice),
                                  ReceiptKon2CPrice = data.Sum(x => x.ReceiptKon2CPrice),
                                  ReceiptKon1APrice = data.Sum(x => x.ReceiptKon1APrice),
                                  ReceiptKon1BPrice = data.Sum(x => x.ReceiptKon1BPrice),
                                  ExpendReturQty = data.Sum(x => x.ExpendReturQty),
                                  ExpendRestQty = data.Sum(x => x.ExpendRestQty),
                                  ExpendProcessQty = data.Sum(x => x.ExpendProcessQty),
                                  ExpendSampleQty = data.Sum(x => x.ExpendSampleQty),
                                  ExpendKon2AQty = data.Sum(x => x.ExpendKon2AQty),
                                  ExpendKon2BQty = data.Sum(x => x.ExpendKon2BQty),
                                  ExpendKon2CQty = data.Sum(x => x.ExpendKon2CQty),
                                  ExpendKon1AQty = data.Sum(x => x.ExpendKon1AQty),
                                  ExpendKon1BQty = data.Sum(x => x.ExpendKon1BQty),
                                  ExpendReturPrice = data.Sum(x => x.ExpendReturPrice),
                                  ExpendRestPrice = data.Sum(x => x.ExpendRestPrice),
                                  ExpendProcessPrice = data.Sum(x => x.ExpendProcessPrice),
                                  ExpendSamplePrice = data.Sum(x => x.ExpendSamplePrice),
                                  ExpendKon2APrice = data.Sum(x => x.ExpendKon2APrice),
                                  ExpendKon2BPrice = data.Sum(x => x.ExpendKon2BPrice),
                                  ExpendKon2CPrice = data.Sum(x => x.ExpendKon2CPrice),
                                  ExpendKon1APrice = data.Sum(x => x.ExpendKon1APrice),
                                  ExpendKon1BPrice = data.Sum(x => x.ExpendKon1BPrice),
                                  EndingBalanceQty = Math.Round((decimal)data.Sum(x => x.BeginningBalanceQty) + (decimal)data.Sum(x => x.ReceiptCorrectionQty) + (decimal)data.Sum(a => a.ReceiptPurchaseQty) + (decimal)data.Sum(a => a.ReceiptProcessQty) + (decimal)data.Sum(a => a.ReceiptKon2AQty) + (decimal)data.Sum(a => a.ReceiptKon2BQty) + (decimal)data.Sum(a => a.ReceiptKon2CQty) + (decimal)data.Sum(a => a.ReceiptKon1AQty) + (decimal)data.Sum(a => a.ReceiptKon1BQty) - ((decimal)data.Sum(a => a.ExpendReturQty) + (decimal)data.Sum(a => a.ExpendSampleQty) + (decimal)data.Sum(a => a.ExpendRestQty) + (decimal)data.Sum(a => a.ExpendProcessQty) + (decimal)data.Sum(a => a.ExpendKon2AQty) + (decimal)data.Sum(a => a.ExpendKon2BQty) + (decimal)data.Sum(a => a.ExpendKon2CQty) + (decimal)data.Sum(a => a.ExpendKon1AQty) + (decimal)data.Sum(a => a.ExpendKon1BQty)), 2),
                                  //EndingBalanceQty = 0,
                                  EndingBalancePrice = Math.Round((decimal)data.Sum(a => a.BeginningBalancePrice) + (decimal)data.Sum(a => a.ReceiptCorrectionPrice) + (decimal)data.Sum(a => a.ReceiptPurchasePrice) + (decimal)data.Sum(a => a.ReceiptProcessPrice) + (decimal)data.Sum(a => a.ReceiptKon2APrice) + (decimal)data.Sum(a => a.ReceiptKon2BPrice) + (decimal)data.Sum(a => a.ReceiptKon2CPrice) + (decimal)data.Sum(a => a.ReceiptKon1APrice) + (decimal)data.Sum(a => a.ReceiptKon1BPrice) - ((decimal)data.Sum(a => a.ExpendReturPrice) + (decimal)data.Sum(a => a.ExpendRestPrice) + (decimal)data.Sum(a => a.ExpendProcessPrice) + (decimal)data.Sum(a => a.ExpendSamplePrice) + (decimal)data.Sum(a => a.ExpendKon2APrice) + (decimal)data.Sum(a => a.ExpendKon2BPrice) + (decimal)data.Sum(a => a.ExpendKon2CPrice) + (decimal)data.Sum(a => a.ExpendKon1APrice) + (decimal)data.Sum(a => a.ExpendKon1BPrice)), 2),
                                  //EndingBalancePrice = 0,
                                  POId = data.FirstOrDefault().POId
                              };
            //var SaldoAkhirs = (from a in SaldoAkhir
            //              join b in SaldoAwal on new { a.PlanPo, a.ProductCode } equals new { b.PlanPo, b.ProductCode } into data
            //              from bb in data.DefaultIfEmpty()
            //              select new AccountingStockReportViewModel
            //              {
            //                  ProductCode = a.ProductCode,
            //                  ProductName = a.ProductName,
            //                  RO = a.RO,
            //                  Buyer = a.Buyer,
            //                  PlanPo = a.PlanPo,
            //                  NoArticle = a.NoArticle,
            //                  BeginningBalanceQty = bb != null ? bb.EndingBalanceQty : 0,
            //                  BeginningBalanceUom = a.BeginningBalanceUom,
            //                  BeginningBalancePrice = bb != null ? bb.EndingBalancePrice : 0,
            //                  ReceiptCorrectionQty = a.ReceiptCorrectionQty,
            //                  ReceiptPurchaseQty = a.ReceiptPurchaseQty,
            //                  ReceiptProcessQty = a.ReceiptProcessQty,
            //                  ReceiptKon2AQty = a.ReceiptKon2AQty,
            //                  ReceiptKon2BQty = a.ReceiptKon2BQty,
            //                  ReceiptKon2CQty = a.ReceiptKon2CQty,
            //                  ReceiptKon1AQty = a.ReceiptKon1AQty,
            //                  ReceiptKon1BQty = a.ReceiptKon1BQty,
            //                  ReceiptCorrectionPrice = a.ReceiptCorrectionPrice,
            //                  ReceiptPurchasePrice = a.ReceiptPurchasePrice,
            //                  ReceiptProcessPrice = a.ReceiptProcessPrice,
            //                  ReceiptKon2APrice = a.ReceiptKon2APrice,
            //                  ReceiptKon2BPrice = a.ReceiptKon2BPrice,
            //                  ReceiptKon2CPrice = a.ReceiptKon2CPrice,
            //                  ReceiptKon1APrice = a.ReceiptKon1APrice,
            //                  ReceiptKon1BPrice = a.ReceiptKon1BPrice,
            //                  ExpendReturQty = a.ExpendReturQty,
            //                  ExpendRestQty = a.ExpendRestQty,
            //                  ExpendProcessQty = a.ExpendProcessQty,
            //                  ExpendSampleQty = a.ExpendSampleQty,
            //                  ExpendKon2AQty = a.ExpendKon2AQty,
            //                  ExpendKon2BQty = a.ExpendKon2BQty,
            //                  ExpendKon2CQty = a.ExpendKon2CQty,
            //                  ExpendKon1AQty = a.ExpendKon1AQty,
            //                  ExpendKon1BQty = a.ExpendKon1BQty,
            //                  ExpendReturPrice = a.ExpendReturPrice,
            //                  ExpendRestPrice = a.ExpendRestPrice,
            //                  ExpendProcessPrice = a.ExpendProcessPrice,
            //                  ExpendSamplePrice = a.ExpendSamplePrice,
            //                  ExpendKon2APrice = a.ExpendKon2APrice,
            //                  ExpendKon2BPrice = a.ExpendKon2BPrice,
            //                  ExpendKon2CPrice = a.ExpendKon2CPrice,
            //                  ExpendKon1APrice = a.ExpendKon1APrice,
            //                  ExpendKon1BPrice = a.ExpendKon1BPrice,
            //                  EndingBalanceQty = Math.Round((bb != null ? (decimal)bb.EndingBalanceQty : 0) + (decimal)a.ReceiptCorrectionQty + (decimal)a.ReceiptPurchaseQty + (decimal)a.ReceiptProcessQty + (decimal)a.ReceiptKon2AQty + (decimal)a.ReceiptKon2BQty + (decimal)a.ReceiptKon2CQty + (decimal)a.ReceiptKon1AQty + (decimal)a.ReceiptKon1BQty - ((decimal)a.ExpendReturQty + (decimal)a.ExpendSampleQty +(decimal)a.ExpendRestQty + (decimal)a.ExpendProcessQty + (decimal)a.ExpendKon2AQty + (decimal)a.ExpendKon2BQty + (decimal)a.ExpendKon2CQty + (decimal)a.ExpendKon1AQty + (decimal)a.ExpendKon1BQty), 2),
            //                  //EndingBalanceQty = 0,
            //                  EndingBalancePrice = Math.Round((bb != null ? (decimal)bb.EndingBalancePrice : 0) + (decimal)a.ReceiptCorrectionPrice + (decimal)a.ReceiptPurchasePrice + (decimal)a.ReceiptProcessPrice + (decimal)a.ReceiptKon2APrice + (decimal)a.ReceiptKon2BPrice + (decimal)a.ReceiptKon2CPrice + (decimal)a.ReceiptKon1APrice + (decimal)a.ReceiptKon1BPrice - ((decimal)a.ExpendReturPrice + (decimal)a.ExpendRestPrice + (decimal)a.ExpendProcessPrice + (decimal)a.ExpendSamplePrice + (decimal)a.ExpendKon2APrice + (decimal)a.ExpendKon2BPrice + (decimal)a.ExpendKon2CPrice + (decimal)a.ExpendKon1APrice + (decimal)a.ExpendKon1BPrice), 2),
            //                  //EndingBalancePrice = 0,
            //                  POId = a.POId
            //              }).ToList();

            return SaldoAkhirs.ToList();

        }
        public MemoryStream GenerateExcelAStockReport(string ctg, string categoryname, string unitcode, string unitname, DateTime? datefrom, DateTime? dateto, int offset)
        {
            var Query = GetStockQuery(ctg, unitcode, datefrom, dateto, offset);
            Query = Query.OrderBy(x => x.ProductCode).ThenBy(x => x.PlanPo).ToList();
            #region Pemasukan
            double SaldoAwalQtyTotal = Query.Sum(x => Convert.ToDouble(x.BeginningBalanceQty));
            double SaldoAwalPriceTotal = Query.Sum(x => Convert.ToDouble(x.BeginningBalancePrice));
            double KoreksiQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptCorrectionQty));
            double KoreksiPriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptCorrectionPrice));
            double PEMBELIANQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptPurchaseQty));
            double PEMBELIANPriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptPurchasePrice));
            double PROSESQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptProcessQty));
            double PROSESPriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptProcessPrice));
            double Konfeksi2AQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon2AQty));
            double Konfeksi2APriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon2APrice));
            double KONFEKSI2BQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon2BQty));
            double KONFEKSI2BPriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon2BPrice));
            double KONFEKSI2CQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon2CQty));
            double KONFEKSI2CPriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon2CPrice));
            double KONFEKSI1BQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon1BQty));
            double KONFEKSI1BPriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon1BPrice));
            double KONFEKSI1AQtyTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon1AQty));
            double KONFEKSI1APriceTotal = Query.Sum(x => Convert.ToDouble(x.ReceiptKon1APrice));
            #endregion
            #region Pemngeluaran
            double? ReturQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendReturQty));
            double? ReturJumlahTotal = Query.Sum(x => Convert.ToDouble(x.ExpendReturPrice));
            double? SisaQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendRestQty));
            double? SisaPriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendRestPrice));
            double? ExpendPROSESQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendProcessQty));
            double? ExpendPROSESPriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendProcessPrice));
            double? SAMPLEQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendSampleQty));
            double? SAMPLEPriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendSamplePrice));
            double? ExpendKONFEKSI2AQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon2AQty));
            double? ExpendKonfeksi2APriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon2APrice));
            double? ExpendKONFEKSI2BQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon2BQty));
            double? ExpendKONFEKSI2BPriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon2BPrice));
            double? ExpendKONFEKSI2CQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon2CQty));
            double? ExpendKONFEKSI2CPriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon2CPrice));
            double? ExpendKONFEKSI1BQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon1BQty));
            double? ExpendKONFEKSI1BPriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon1BPrice));
            double? ExpendKONFEKSI1AQtyTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon1AQty));
            double? ExpendKONFEKSI1APriceTotal = Query.Sum(x => Convert.ToDouble(x.ExpendKon1APrice));
            #endregion
            double? EndingQty = Query.Sum(x => Convert.ToDouble(x.EndingBalanceQty));
            double? EndingTotal = Query.Sum(x => Convert.ToDouble(x.EndingBalancePrice));
            DataTable result = new DataTable();
            var headers = new string[] { "No", "Kode", "Nama Barang", "RO", "Buyer", "PlanPO", "No Artikel", "Saldo Awal", "Saldo Awal1", "Saldo Awal2", "P E M A S U K A N", "P E M B E L I A N1", "P E M B E L I A N2", "P E M B E L I A N3", "P E M B E L I A N4", "P E M B E L I A N5", "P E M B E L I A N6", "P E M B E L I A N7", "P E M B E L I A N8", "P E M B E L I A N9", "P E M B E L I A N10", "P E M B E L I A N11", "P E M B E L I A N12", "P E M B E L I A N13", "P E M B E L I A N14", "P E M B E L I A N15", "P E N G E L U A R A N", "P E N G E L U A R A N1", "P E N G E L U A R A N2", "P E N G E L U A R A N3", "P E N G E L U A R A N4", "P E N G E L U A R A N5", "P E N G E L U A R A N6", "P E N G E L U A R A N7", "P E N G E L U A R A N8", "P E N G E L U A R A N9", "P E N G E L U A R A N10", "P E N G E L U A R A N11", "P E N G E L U A R A N12", "P E N G E L U A R A N13", "P E N G E L U A R A N14", "P E N G E L U A R A N15", "P E N G E L U A R A N16", "P E N G E L U A R A N17", "Saldo Akhir", "Saldo Akhir 1" };
            var headers2 = new string[] { "Koreksi", "Pembelian", "Proses", "KONFEKSI 2A", "KONFEKSI 2B", "KONFEKSI 2C/EX.K4", "KONFEKSI 1A/EX.K3",  "KONFEKSI 1B", "Retur", "Sisa", "Proses", "Sample", "KONFEKSI 2A", "KONFEKSI 2B", "KONFEKSI 2C/EX.K4", "KONFEKSI 1A/EX. K3", "KONFEKSI 1B" };
            var subheaders = new string[] { "Jumlah", "Sat", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp", "Qty", "Rp" };
            for (int i = 0; i < 7; i++)
            {
                result.Columns.Add(new DataColumn() { ColumnName = headers[i], DataType = typeof(string) });
            }
            result.Columns.Add(new DataColumn() { ColumnName = headers[7], DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[8], DataType = typeof(string) });
            for (int i = 9; i < headers.Length; i++)
            {
                result.Columns.Add(new DataColumn() { ColumnName = headers[i], DataType = typeof(Double) });
            }
            var index = 1;
            foreach (var item in Query)
            {
                var ReceiptPurchaseQty = unitcode == "C2A" ? item.ReceiptPurchaseQty + item.ReceiptKon2AQty : unitcode == "C2B" ? item.ReceiptPurchaseQty + item.ReceiptKon2BQty : unitcode == "C2C" ? item.ReceiptPurchaseQty + item.ReceiptKon2CQty : unitcode == "C1B" ? item.ReceiptPurchaseQty + item.ReceiptKon1BQty : unitcode == "C1A" ? item.ReceiptPurchaseQty + item.ReceiptKon1AQty : item.ReceiptPurchaseQty + item.ReceiptKon2AQty + item.ReceiptKon2BQty + item.ReceiptKon2CQty + item.ReceiptKon1BQty + item.ReceiptKon1AQty;
                var ReceiptPurchasePrice = unitcode == "C2A" ? item.ReceiptPurchasePrice + item.ReceiptKon2APrice : unitcode == "C2B" ? item.ReceiptPurchasePrice + item.ReceiptKon2BPrice : unitcode == "C2C" ? item.ReceiptPurchasePrice + item.ReceiptKon2CPrice : unitcode == "C1B" ? item.ReceiptPurchasePrice + item.ReceiptKon1BPrice : unitcode == "C1A" ? item.ReceiptPurchasePrice + item.ReceiptKon1APrice : item.ReceiptPurchasePrice + item.ReceiptKon2APrice + item.ReceiptKon2BPrice + item.ReceiptKon2CPrice + item.ReceiptKon1BPrice + item.ReceiptKon1APrice;
                var ReceiptKon2AQty = unitcode == "C2A" ? 0 : item.ReceiptKon2AQty;
                var ReceiptKon2APrice = unitcode == "C2A" ? 0 : item.ReceiptKon2APrice;
                var ReceiptKon2BPrice = unitcode == "C2B" ? 0 : item.ReceiptKon2BPrice;
                var ReceiptKon2BQty = unitcode == "C2B" ? 0 : item.ReceiptKon2BQty;
                var ReceiptKon2CPrice = unitcode == "C2C" ? 0 : item.ReceiptKon2CPrice;
                var ReceiptKon2CQty = unitcode == "C2C" ? 0 : item.ReceiptKon2CQty;
                var ReceiptKon1BPrice = unitcode == "C1B" ? 0 : item.ReceiptKon1BPrice;
                var ReceiptKon1BQty = unitcode == "C1B" ? 0 : item.ReceiptKon1BQty;
                var ReceiptKon1AQty = unitcode == "C1A" ? 0 : item.ReceiptKon1AQty;
                var ReceiptKon1APrice = unitcode == "C1A" ? 0 : item.ReceiptKon1APrice;
                var ReceiptCorrection = item.ReceiptCorrectionPrice;

                result.Rows.Add(index++, item.ProductCode, item.ProductName, item.RO, item.Buyer, item.PlanPo, item.NoArticle,
                    Convert.ToDouble(item.BeginningBalanceQty), item.BeginningBalanceUom,
                    Convert.ToDouble(item.BeginningBalancePrice),
                    Convert.ToDouble(item.ReceiptCorrectionQty),
                    Convert.ToDouble(item.ReceiptCorrectionPrice),
                    Convert.ToDouble(ReceiptPurchaseQty),
                    Convert.ToDouble(ReceiptPurchasePrice),
                    Convert.ToDouble(item.ReceiptProcessQty),
                    Convert.ToDouble(item.ReceiptProcessPrice),
                    Convert.ToDouble(ReceiptKon2AQty),
                    Convert.ToDouble(ReceiptKon2APrice),
                    Convert.ToDouble(ReceiptKon2BQty),
                    Convert.ToDouble(ReceiptKon2BPrice),
                    Convert.ToDouble(ReceiptKon2CQty),
                    Convert.ToDouble(ReceiptKon2CPrice),
                    Convert.ToDouble(ReceiptKon1AQty),
                    Convert.ToDouble(ReceiptKon1APrice),
                    Convert.ToDouble(ReceiptKon1BQty),
                    Convert.ToDouble(ReceiptKon1BPrice),
                    Convert.ToDouble(item.ExpendReturQty), 
                    item.ExpendReturPrice, 
                    item.ExpendRestQty, 
                    item.ExpendRestPrice, 
                    item.ExpendProcessQty, 
                    item.ExpendProcessPrice, 
                    item.ExpendSampleQty, 
                    item.ExpendSamplePrice, 
                    item.ExpendKon2AQty, 
                    item.ExpendKon2APrice, 
                    item.ExpendKon2BQty, 
                    item.ExpendKon2BPrice, 
                    item.ExpendKon2CQty, 
                    item.ExpendKon2CPrice, 
                    item.ExpendKon1AQty, 
                    item.ExpendKon1APrice, 
                    item.ExpendKon1BQty, 
                    item.ExpendKon1BPrice, 
                    Convert.ToDouble(item.EndingBalanceQty), 
                    Convert.ToDouble(item.EndingBalancePrice));
            }

            //result.Rows.Add("", "", "", "", "", "", "",
            //        SaldoAwalQtyTotal, "",
            //        SaldoAwalPriceTotal,
            //        KoreksiQtyTotal,
            //        KoreksiPriceTotal,
            //        PEMBELIANQtyTotal,
            //        PEMBELIANPriceTotal,
            //        PROSESQtyTotal,
            //        PROSESPriceTotal,
            //        Konfeksi2AQtyTotal,
            //        Konfeksi2APriceTotal,
            //        KONFEKSI2BQtyTotal,
            //        KONFEKSI2BPriceTotal,
            //        KONFEKSI2CQtyTotal,
            //        KONFEKSI2CPriceTotal,
            //        KONFEKSI1AQtyTotal,
            //        KONFEKSI1APriceTotal,
            //        KONFEKSI1BQtyTotal,
            //        KONFEKSI1BPriceTotal,
            //        ReturQtyTotal, ReturJumlahTotal, SisaQtyTotal, SisaPriceTotal, ExpendPROSESQtyTotal, ExpendPROSESPriceTotal, SAMPLEQtyTotal, SAMPLEPriceTotal, ExpendKONFEKSI2AQtyTotal, ExpendKonfeksi2APriceTotal, ExpendKONFEKSI2BQtyTotal, ExpendKONFEKSI2BPriceTotal, ExpendKONFEKSI2CQtyTotal, ExpendKONFEKSI2CPriceTotal, ExpendKONFEKSI1AQtyTotal, ExpendKONFEKSI1APriceTotal, ExpendKONFEKSI1BQtyTotal, ExpendKONFEKSI1BPriceTotal, Convert.ToDouble(EndingQty), Convert.ToDouble(EndingTotal));


            ExcelPackage package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Data");
            DateTime DateFrom = datefrom == null ? new DateTime(1970, 1, 1) : (DateTime)datefrom;
            DateTime DateTo = dateto == null ? DateTime.Now : (DateTime)dateto;
            var col1 = (char)('A' + result.Columns.Count);
            string tglawal = DateFrom.ToString("dd MMM yyyy", new CultureInfo("id-ID"));
            string tglakhir = DateTo.ToString("dd MMM yyyy", new CultureInfo("id-ID"));
            //CultureInfo Id = new CultureInfo("id-ID");
            sheet.Cells[$"A1:{col1}1"].Value = string.Format("LAPORAN FLOW {0}", categoryname);
            sheet.Cells[$"A1:{col1}1"].Merge = true;
            sheet.Cells[$"A1:{col1}1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            sheet.Cells[$"A1:{col1}1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            sheet.Cells[$"A1:{col1}1"].Style.Font.Bold = true;
            sheet.Cells[$"A2:{col1}2"].Value = string.Format("Periode {0} - {1}", tglawal, tglakhir);
            sheet.Cells[$"A2:{col1}2"].Merge = true;
            sheet.Cells[$"A2:{col1}2"].Style.Font.Bold = true;
            sheet.Cells[$"A2:{col1}2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            sheet.Cells[$"A2:{col1}2"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            sheet.Cells[$"A3:{col1}3"].Value = string.Format("KONFEKSI : {0}", unitname);
            sheet.Cells[$"A3:{col1}3"].Merge = true;
            sheet.Cells[$"A3:{col1}3"].Style.Font.Bold = true;
            sheet.Cells[$"A3:{col1}3"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            sheet.Cells[$"A3:{col1}3"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

            sheet.Cells["A7"].LoadFromDataTable(result, false, OfficeOpenXml.Table.TableStyles.Light16);
            sheet.Cells["H4"].Value = headers[7];
            sheet.Cells["H4:J5"].Merge = true;
            sheet.Cells["H4:J5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["K4"].Value = headers[10];
            sheet.Cells["K4:Z4"].Merge = true;
            sheet.Cells["K4:Z4"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AA4"].Value = headers[26];
            sheet.Cells["AA4:AR4"].Merge = true;
            sheet.Cells["AA4:AR4"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AS4"].Value = headers[44];
            sheet.Cells["AS4:AT5"].Merge = true;
            sheet.Cells["AS4:AT5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["K5"].Value = headers2[0];
            sheet.Cells["K5:L5"].Merge = true;
            sheet.Cells["K5:L5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["M5"].Value = headers2[1];
            sheet.Cells["M5:N5"].Merge = true;
            sheet.Cells["M5:N5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["O5"].Value = headers2[2];
            sheet.Cells["O5:P5"].Merge = true;
            sheet.Cells["O5:P5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["Q5"].Value = headers2[3];
            sheet.Cells["Q5:R5"].Merge = true;
            sheet.Cells["Q5:R5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["S5"].Value = headers2[4];
            sheet.Cells["S5:T5"].Merge = true;
            sheet.Cells["S5:T5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["U5"].Value = headers2[5];
            sheet.Cells["U5:V5"].Merge = true;
            sheet.Cells["U5:V5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["W5"].Value = headers2[6];
            sheet.Cells["W5:X5"].Merge = true;
            sheet.Cells["W5:X5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["Y5"].Value = headers2[7];
            sheet.Cells["Y5:Z5"].Merge = true;
            sheet.Cells["Y5:Z5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AA5"].Value = headers2[8];
            sheet.Cells["AA5:AB5"].Merge = true;
            sheet.Cells["AA5:AB5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AC5"].Value = headers2[9];
            sheet.Cells["AC5:AD5"].Merge = true;
            sheet.Cells["AC5:AD5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AE5"].Value = headers2[10];
            sheet.Cells["AE5:AF5"].Merge = true;
            sheet.Cells["AE5:AF5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AG5"].Value = headers2[11];
            sheet.Cells["AG5:AH5"].Merge = true;
            sheet.Cells["AG5:AH5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AI5"].Value = headers2[12];
            sheet.Cells["AI5:AJ5"].Merge = true;
            sheet.Cells["AI5:AJ5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AK5"].Value = headers2[13];
            sheet.Cells["AK5:AL5"].Merge = true;
            sheet.Cells["AK5:AL5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AM5"].Value = headers2[14];
            sheet.Cells["AM5:AN5"].Merge = true;
            sheet.Cells["AM5:AN5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AO5"].Value = headers2[15];
            sheet.Cells["AO5:AP5"].Merge = true;
            sheet.Cells["AO5:AP5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            sheet.Cells["AQ5"].Value = headers2[16];
            sheet.Cells["AQ5:AR5"].Merge = true;
            sheet.Cells["AQ5:AR5"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);

            foreach (var i in Enumerable.Range(0, 7))
            {
                var col = (char)('A' + i);
                sheet.Cells[$"{col}4"].Value = headers[i];
                sheet.Cells[$"{col}4:{col}6"].Merge = true;
                sheet.Cells[$"{col}4:{col}6"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
            }

            for (var i = 0; i < 19; i++)
            {
                var col = (char)('H' + i);
                sheet.Cells[$"{col}6"].Value = subheaders[i];
                sheet.Cells[$"{col}6"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);

            }

            for (var i = 19; i < 39; i++)
            {
                var col = (char)('A' + i - 19);
                sheet.Cells[$"A{col}6"].Value = subheaders[i];
                sheet.Cells[$"A{col}6"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);

            }
            sheet.Cells["A4:AS6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells["A4:AS6"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet.Cells["A4:AS6"].Style.Font.Bold = true;
            sheet.Cells[$"A{result.Rows.Count + 7}:G{result.Rows.Count + 7}"].Value = "T O T A L  . . . . . . . . . . . . . . .";
            sheet.Cells[$"A{result.Rows.Count + 7}:G{result.Rows.Count + 7}"].Merge = true;
            sheet.Cells[$"A{result.Rows.Count + 7}:G{result.Rows.Count + 7}"].Style.Font.Bold = true;
            sheet.Cells[$"A{7 + result.Rows.Count}:G{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"A{result.Rows.Count + 7}:G{result.Rows.Count + 7}"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            sheet.Cells[$"A{result.Rows.Count + 7}:G{result.Rows.Count + 7}"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

            sheet.Cells[$"H{7 + result.Rows.Count}"].Value = string.Format("{0:0,0.00}", SaldoAwalQtyTotal);
            sheet.Cells[$"H{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"I{7 + result.Rows.Count}"].Value = "";
            sheet.Cells[$"I{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"J{7 + result.Rows.Count}"].Value = SaldoAwalPriceTotal;
            sheet.Cells[$"J{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"K{7 + result.Rows.Count}"].Value = KoreksiQtyTotal;
            sheet.Cells[$"K{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"L{7 + result.Rows.Count}"].Value = KoreksiPriceTotal;
            sheet.Cells[$"L{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"M{7 + result.Rows.Count}"].Value = PEMBELIANQtyTotal;
            sheet.Cells[$"M{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"N{7 + result.Rows.Count}"].Value = PEMBELIANPriceTotal;
            sheet.Cells[$"N{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"O{7 + result.Rows.Count}"].Value = PROSESQtyTotal;
            sheet.Cells[$"O{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"P{7 + result.Rows.Count}"].Value = PROSESPriceTotal;
            sheet.Cells[$"P{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"Q{7 + result.Rows.Count}"].Value = Konfeksi2AQtyTotal;
            sheet.Cells[$"Q{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"R{7 + result.Rows.Count}"].Value = Konfeksi2APriceTotal;
            sheet.Cells[$"R{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"S{7 + result.Rows.Count}"].Value = KONFEKSI2BQtyTotal;
            sheet.Cells[$"S{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"T{7 + result.Rows.Count}"].Value = KONFEKSI2BPriceTotal;
            sheet.Cells[$"T{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"U{7 + result.Rows.Count}"].Value = KONFEKSI2CQtyTotal;
            sheet.Cells[$"U{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"V{7 + result.Rows.Count}"].Value = KONFEKSI2CPriceTotal;
            sheet.Cells[$"V{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"W{7 + result.Rows.Count}"].Value = KONFEKSI1AQtyTotal;
            sheet.Cells[$"W{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"X{7 + result.Rows.Count}"].Value = KONFEKSI1APriceTotal;
            sheet.Cells[$"X{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"Y{7 + result.Rows.Count}"].Value = KONFEKSI1BQtyTotal;
            sheet.Cells[$"Y{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"Z{7 + result.Rows.Count}"].Value = KONFEKSI1BPriceTotal;
            sheet.Cells[$"Z{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AA{7 + result.Rows.Count}"].Value = ReturQtyTotal;
            sheet.Cells[$"AA{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AB{7 + result.Rows.Count}"].Value = ReturJumlahTotal;
            sheet.Cells[$"AB{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AC{7 + result.Rows.Count}"].Value = SisaQtyTotal;
            sheet.Cells[$"AC{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AD{7 + result.Rows.Count}"].Value = SisaPriceTotal;
            sheet.Cells[$"AD{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AE{7 + result.Rows.Count}"].Value = ExpendPROSESQtyTotal;
            sheet.Cells[$"AE{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AF{7 + result.Rows.Count}"].Value = ExpendPROSESPriceTotal;
            sheet.Cells[$"AF{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AG{7 + result.Rows.Count}"].Value = SAMPLEQtyTotal;
            sheet.Cells[$"AG{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AH{7 + result.Rows.Count}"].Value = SAMPLEPriceTotal;
            sheet.Cells[$"AH{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AI{7 + result.Rows.Count}"].Value = ExpendKONFEKSI2AQtyTotal;
            sheet.Cells[$"AI{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AJ{7 + result.Rows.Count}"].Value = ExpendKonfeksi2APriceTotal;
            sheet.Cells[$"AJ{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AK{7 + result.Rows.Count}"].Value = ExpendKONFEKSI2BQtyTotal;
            sheet.Cells[$"AK{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AL{7 + result.Rows.Count}"].Value = ExpendKONFEKSI2BPriceTotal;
            sheet.Cells[$"AL{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AM{7 + result.Rows.Count}"].Value = ExpendKONFEKSI2CQtyTotal;
            sheet.Cells[$"AM{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AN{7 + result.Rows.Count}"].Value = ExpendKONFEKSI2CPriceTotal;
            sheet.Cells[$"AN{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AO{7 + result.Rows.Count}"].Value = ExpendKONFEKSI1AQtyTotal;
            sheet.Cells[$"AO{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AP{7 + result.Rows.Count}"].Value = ExpendKONFEKSI1APriceTotal;
            sheet.Cells[$"AP{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AQ{7 + result.Rows.Count}"].Value = ExpendKONFEKSI1BQtyTotal;
            sheet.Cells[$"AQ{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AR{7 + result.Rows.Count}"].Value = ExpendKONFEKSI1BPriceTotal;
            sheet.Cells[$"AR{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AS{7 + result.Rows.Count}"].Value = EndingQty;
            sheet.Cells[$"AS{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            sheet.Cells[$"AT{7 + result.Rows.Count}"].Value = EndingTotal;
            sheet.Cells[$"AT{7 + result.Rows.Count}"].Style.Border.BorderAround(ExcelBorderStyle.Medium);


            var widths = new int[] { 5, 10, 20, 15, 7, 20, 20, 10, 7, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                sheet.Column(i + 1).Width = widths[i];
            }

            MemoryStream stream = new MemoryStream();
            package.SaveAs(stream);
            return stream;


        }


    }
}
