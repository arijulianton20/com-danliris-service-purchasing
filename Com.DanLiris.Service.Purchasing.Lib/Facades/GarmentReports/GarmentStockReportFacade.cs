using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentDeliveryOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentReports;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Globalization;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentReports
{
    public class GarmentStockReportFacade : IGarmentStockReportFacade
    {
        private readonly PurchasingDbContext dbContext;
        public readonly IServiceProvider serviceProvider;
        private readonly DbSet<GarmentDeliveryOrder> dbSet;

        public GarmentStockReportFacade(IServiceProvider serviceProvider, PurchasingDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<GarmentDeliveryOrder>();
        }

        public List<GarmentStockReportViewModel> GetStockQuery(string ctg, string unitcode, DateTime? datefrom, DateTime? dateto, int offset)
        {
            DateTime DateFrom = datefrom == null ? new DateTime(1970, 1, 1) : (DateTime)datefrom;
            DateTime DateTo = dateto == null ? DateTime.Now : (DateTime)dateto;

            var lastdate = dbContext.BalanceStocks.OrderByDescending(x => x.CreateDate).Select(x => x.CreateDate).FirstOrDefault();

            var BalaceStock = (from a in dbContext.BalanceStocks
                               join b in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on (long)a.EPOItemId equals b.Id
                               join c in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on b.GarmentEPOId equals c.Id
                               join d in dbContext.GarmentInternalPurchaseOrders on b.POId equals d.Id
                               where a.CreateDate == lastdate

                               group new { a, b, c, d } by new { b.ProductCode, b.ProductName, b.PO_SerialNumber } into data

                               select new GarmentStockReportViewModel
                               {
                                  BeginningBalanceQty = (decimal)data.Sum(x=>x.a.CloseStock),
                                  BeginningBalanceUom = data.FirstOrDefault().b.SmallUomUnit,
                                  Buyer = data.FirstOrDefault().d.BuyerCode,
                                  EndingBalanceQty = 0,
                                  EndingUom = data.FirstOrDefault().b.SmallUomUnit,
                                  ExpandUom = data.FirstOrDefault().b.SmallUomUnit,
                                  ExpendQty = 0,
                                  NoArticle = data.FirstOrDefault().a.ArticleNo,
                                  PaymentMethod = data.FirstOrDefault().c.PaymentMethod,
                                  PlanPo = data.Key.PO_SerialNumber,
                                  POId = data.FirstOrDefault().b.POId,
                                  ProductCode = data.Key.ProductCode,
                                  ProductName = data.Key.ProductName,
                                  ProductRemark = data.FirstOrDefault().b.Remark,
                                  ReceiptCorrectionQty = 0,
                                  ReceiptQty = 0,
                                  ReceiptUom = data.FirstOrDefault().b.SmallUomUnit,
                                  RO = data.FirstOrDefault().b.RONo,
                                  UNitCode = "",
                                  UnitSenderCode = "",
                                  UnitRequestCode = ""
                               }).ToList();

            List<GarmentStockReportViewModel> penerimaan = new List<GarmentStockReportViewModel>();
            List<GarmentStockReportViewModel> pengeluaran = new List<GarmentStockReportViewModel>();
            List<GarmentStockReportViewModel> penerimaanSA = new List<GarmentStockReportViewModel>();
            List<GarmentStockReportViewModel> pengeluaranSA = new List<GarmentStockReportViewModel>();

            #region SaldoAwal
            var IdSATerima = (from a in dbContext.GarmentUnitReceiptNotes
                              join b in dbContext.GarmentUnitReceiptNoteItems on a.Id equals b.URNId
                              join d in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals d.Id
                              join f in dbContext.GarmentInternalPurchaseOrderItems on b.POItemId equals f.Id
                              join g in dbContext.GarmentInternalPurchaseOrders on f.GPOId equals g.Id
                              join epoItem in dbContext.GarmentExternalPurchaseOrderItems on b.EPOItemId equals epoItem.Id into EP
                              from epoItem in EP.DefaultIfEmpty()
                              join epo in dbContext.GarmentExternalPurchaseOrders on epoItem.GarmentEPOId equals epo.Id into EPO
                              from epo in EPO.DefaultIfEmpty()
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
                                  POID = g.Id,
                                  EPOItemId = epoItem == null ? 0 : epoItem.Id,
                                  EPOId = epo == null ? 0 : epo.Id,
                                  //UENNo = dd == null ? "-" : dd.UENNo,
                                  a.UnitCode
                              }).ToList().Distinct();
            var sapenerimaanunitreceiptnoteids = IdSATerima.Select(x => x.UrnId).ToList();
            var sapenerimaanunitreceiptnotes = dbContext.GarmentUnitReceiptNotes.Where(x => sapenerimaanunitreceiptnoteids.Contains(x.Id)).Select(s => new { s.ReceiptDate, s.URNType, s.UnitCode, s.UENNo, s.Id }).ToList();
            var sapenerimaanunitreceiptnoteItemIds = IdSATerima.Select(x => x.UrnItemId).ToList();
            var sapenerimaanuntreceiptnoteItems = dbContext.GarmentUnitReceiptNoteItems.Where(x => sapenerimaanunitreceiptnoteItemIds.Contains(x.Id)).Select(s => new { s.ProductCode, s.ProductName, s.RONo, s.SmallUomUnit, s.POSerialNumber, s.ReceiptQuantity, s.DOCurrencyRate, s.PricePerDealUnit, s.Id, s.SmallQuantity, s.Conversion, s.ProductRemark, s.POId }).ToList();
            var sapenerimaandeliveryorderdetailIds = IdSATerima.Select(x => x.DoDetailId).ToList();
            var sapenerimaandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => sapenerimaandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var sapenerimaanExternalPurchaseOrderItemIds = IdSATerima.Select(x => x.EPOItemId).ToList();
            var sapenerimaanExternalPurchaseOrderItems = dbContext.GarmentExternalPurchaseOrderItems.Where(x => sapenerimaanExternalPurchaseOrderItemIds.Contains(x.Id)).Select(s => new { s.GarmentEPOId, s.Id, s.PO_SerialNumber }).ToList();
            var sapenerimaanExternalPurchaseOrderIds = IdSATerima.Select(x => x.EPOId).ToList();
            var sapenerimaanExternalPurchaseOrders = dbContext.GarmentExternalPurchaseOrders.Where(x => sapenerimaanExternalPurchaseOrderIds.Contains(x.Id)).Select(s => new { s.Id, s.PaymentMethod }).ToList();
            var sapenerimaanintrenalpurchaseorderIds = IdSATerima.Select(x => x.POID).ToList();
            var sapenerimaanintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => sapenerimaanintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            foreach (var item in IdSATerima)
            {
                var sapenerimaanunitreceiptnote = sapenerimaanunitreceiptnotes.FirstOrDefault(x => x.Id == item.UrnId);
                var sapenerimaanuntreceiptnoteItem = sapenerimaanuntreceiptnoteItems.FirstOrDefault(x => x.Id == item.UrnItemId);
                var sapenerimaandeliveryorderdetail = sapenerimaandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var sapenerimaanExternalPurchaseOrder = sapenerimaanExternalPurchaseOrders.FirstOrDefault(x => x.Id == item.EPOId);
                var sapenerimaanintrenalpurchaseorder = sapenerimaanintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);

                penerimaanSA.Add(new GarmentStockReportViewModel
                {
                    BeginningBalanceQty = 0,
                    BeginningBalanceUom = sapenerimaanuntreceiptnoteItem.SmallUomUnit,
                    Buyer = sapenerimaanintrenalpurchaseorder.BuyerCode,
                    EndingBalanceQty = 0,
                    EndingUom = sapenerimaanuntreceiptnoteItem.SmallUomUnit,
                    ExpandUom = sapenerimaanuntreceiptnoteItem.SmallUomUnit,
                    ExpendQty = 0,
                    NoArticle = sapenerimaanintrenalpurchaseorder.Article,
                    PaymentMethod = sapenerimaanExternalPurchaseOrder == null ? "" : sapenerimaanExternalPurchaseOrder.PaymentMethod,
                    PlanPo = sapenerimaanuntreceiptnoteItem.POSerialNumber,
                    POId = sapenerimaanintrenalpurchaseorder.Id,
                    ProductCode = sapenerimaanuntreceiptnoteItem.ProductCode,
                    ProductName = sapenerimaanuntreceiptnoteItem.ProductName,
                    ProductRemark = sapenerimaanuntreceiptnoteItem.ProductRemark,
                    ReceiptCorrectionQty = 0,
                    ReceiptQty = (decimal)sapenerimaanuntreceiptnoteItem.ReceiptQuantity * sapenerimaanuntreceiptnoteItem.Conversion,
                    ReceiptUom = sapenerimaanuntreceiptnoteItem.SmallUomUnit,
                    RO = sapenerimaanuntreceiptnoteItem.RONo,
                    UNitCode = "",
                    UnitSenderCode = "",
                    UnitRequestCode = ""
                });
            }

            var IdSAPengeluaran = (from a in dbContext.GarmentUnitExpenditureNotes
                                   join b in dbContext.GarmentUnitExpenditureNoteItems on a.Id equals b.UENId
                                   join c in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals c.Id
                                   join f in dbContext.GarmentInternalPurchaseOrderItems on b.POItemId equals f.Id
                                   join g in dbContext.GarmentInternalPurchaseOrders on f.GPOId equals g.Id
                                   join h in dbContext.GarmentUnitReceiptNoteItems on b.URNItemId equals h.Id
                                   join i in dbContext.GarmentExternalPurchaseOrderItems on h.EPOItemId equals i.Id into epoitems
                                   from epoitem in epoitems.DefaultIfEmpty()
                                   join j in dbContext.GarmentExternalPurchaseOrders on epoitem.GarmentEPOId equals j.Id into epos
                                   from epo in epos.DefaultIfEmpty()
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
                                       EPOId = epos == null ? 0 : epo.Id
                                   }).ToList().Distinct();

            var sapengeluaranUnitExpenditureNoteItemIds = IdSAPengeluaran.Select(x => x.UENItemsId).ToList();
            var sapengeluaranUnitExpenditureNoteItems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => sapengeluaranUnitExpenditureNoteItemIds.Contains(x.Id)).Select(s => new { s.Quantity, s.PricePerDealUnit, s.Id, s.ProductCode, s.ProductName, s.RONo, s.POSerialNumber, s.UomUnit, s.DOCurrencyRate, s.ProductRemark, s.URNItemId }).ToList();
            var sapengeluaranUnitExpenditureNoteIds = IdSAPengeluaran.Select(x => x.UENId).ToList();
            var sapengeluaranUnitExpenditureNotes = dbContext.GarmentUnitExpenditureNotes.Where(x => sapengeluaranUnitExpenditureNoteIds.Contains(x.Id)).Select(s => new { s.UnitSenderCode, s.UnitRequestName, s.ExpenditureTo, s.Id, s.UENNo, s.ExpenditureType }).ToList();
            var sapengeluarandeliveryorderdetailIds = IdSAPengeluaran.Select(x => x.DoDetailId).ToList();
            var sapengeluarandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => sapengeluarandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var sapengeluaranintrenalpurchaseorderIds = IdSAPengeluaran.Select(x => x.POID).ToList();
            var sapengeluaranintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => sapengeluaranintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            var sapengeluaranexternalpurchaseorderIds = IdSAPengeluaran.Select(x => x.EPOId).ToList();
            var sapengeluaranexternalpurchaseorders = dbContext.GarmentExternalPurchaseOrders.Where(x => sapengeluaranexternalpurchaseorderIds.Contains(x.Id)).Select(x => new { x.PaymentMethod, x.Id }).ToList();
            foreach (var item in IdSAPengeluaran)
            {
                var sapengeluarandeliveryorderdetail = sapengeluarandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var sapengeluaranintrenalpurchaseorder = sapengeluaranintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);
                var sapengeluaranUnitExpenditureNoteItem = sapengeluaranUnitExpenditureNoteItems.FirstOrDefault(x => x.Id == item.UENItemsId);
                var sapengeluaranUnitExpenditureNote = sapengeluaranUnitExpenditureNotes.FirstOrDefault(x => x.Id == item.UENId);
                var sapengeluaranexternalpurchaseorder = sapengeluaranexternalpurchaseorders.FirstOrDefault(x => x.Id == item.EPOId);

                pengeluaranSA.Add(new GarmentStockReportViewModel
                {
                    BeginningBalanceQty = 0,
                    BeginningBalanceUom = sapengeluaranUnitExpenditureNoteItem.UomUnit,
                    Buyer = sapengeluaranintrenalpurchaseorder.BuyerCode,
                    EndingBalanceQty = 0,
                    EndingUom = sapengeluaranUnitExpenditureNoteItem.UomUnit,
                    ExpandUom = sapengeluaranUnitExpenditureNoteItem.UomUnit,
                    ExpendQty = sapengeluaranUnitExpenditureNoteItem.Quantity,
                    NoArticle = sapengeluaranintrenalpurchaseorder.Article,
                    PaymentMethod = sapengeluaranexternalpurchaseorder == null ? "" : sapengeluaranexternalpurchaseorder.PaymentMethod,
                    PlanPo = sapengeluaranUnitExpenditureNoteItem.POSerialNumber,
                    POId = sapengeluaranintrenalpurchaseorder.Id,
                    ProductCode = sapengeluaranUnitExpenditureNoteItem.ProductCode,
                    ProductName = sapengeluaranUnitExpenditureNoteItem.ProductName,
                    ProductRemark = sapengeluaranUnitExpenditureNoteItem.ProductRemark,
                    ReceiptCorrectionQty = 0,
                    ReceiptQty = 0,
                    ReceiptUom = sapengeluaranUnitExpenditureNoteItem.UomUnit,
                    RO = sapengeluaranUnitExpenditureNoteItem.RONo,
                    UNitCode = "",
                    UnitSenderCode = "",
                    UnitRequestCode = ""
                });
            }
            var SAwal = BalaceStock.Concat(penerimaanSA).Concat(pengeluaranSA).ToList();

            var SaldoAwal = (from a in SAwal
                            group a by new { a.ProductCode, a.PlanPo } into data
                            select new GarmentStockReportViewModel
                            {
                                BeginningBalanceQty = Math.Round(data.Sum(x => x.BeginningBalanceQty) + data.Sum(x => x.ReceiptQty) + data.Sum(x => x.ReceiptCorrectionQty) - (decimal)data.Sum(x => x.ExpendQty),2),
                                BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                                Buyer = data.FirstOrDefault().Buyer,
                                EndingBalanceQty = 0,
                                EndingUom = data.FirstOrDefault().EndingUom,
                                ExpandUom = data.FirstOrDefault().ExpandUom,
                                ExpendQty = 0,
                                NoArticle = data.FirstOrDefault().NoArticle,
                                PaymentMethod = data.FirstOrDefault().PaymentMethod,
                                PlanPo = data.FirstOrDefault().PlanPo,
                                POId = data.FirstOrDefault().POId,
                                ProductCode = data.FirstOrDefault().ProductCode,
                                ProductName = data.FirstOrDefault().ProductName,
                                ProductRemark = data.FirstOrDefault().ProductRemark,
                                ReceiptCorrectionQty = 0,
                                ReceiptQty = 0,
                                ReceiptUom = data.FirstOrDefault().ReceiptUom,
                                RO = data.FirstOrDefault().RO,
                                UNitCode = data.FirstOrDefault().UNitCode,
                                UnitSenderCode = data.FirstOrDefault().UnitSenderCode,
                                UnitRequestCode = data.FirstOrDefault().UnitRequestCode
                            }).ToList();
            #endregion
            #region Now
            var IdTerima = (from a in dbContext.GarmentUnitReceiptNotes
                            join b in dbContext.GarmentUnitReceiptNoteItems on a.Id equals b.URNId
                            join d in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals d.Id
                            join f in dbContext.GarmentInternalPurchaseOrders on b.POId equals f.Id
                            join epoItem in dbContext.GarmentExternalPurchaseOrderItems on b.EPOItemId equals epoItem.Id into EP
                            from epoItem in EP.DefaultIfEmpty()
                            join epo in dbContext.GarmentExternalPurchaseOrders on epoItem.GarmentEPOId equals epo.Id into EPO
                            from epo in EPO.DefaultIfEmpty()
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
                                EPOItemId = b.EPOItemId,
                                EPOId = epo == null ? 0 : epo.Id,
                                //UENNo = dd == null ? "-" : dd.UENNo,
                                a.UnitCode
                            }).ToList().Distinct();

            var penerimaanunitreceiptnoteids = IdTerima.Select(x => x.UrnId).ToList();
            var penerimaanunitreceiptnotes = dbContext.GarmentUnitReceiptNotes.Where(x => penerimaanunitreceiptnoteids.Contains(x.Id)).Select(s => new { s.ReceiptDate, s.URNType, s.UnitCode, s.UENNo, s.Id }).ToList();
            var penerimaanunitreceiptnoteItemIds = IdTerima.Select(x => x.UrnItemId).ToList();
            var penerimaanuntreceiptnoteItems = dbContext.GarmentUnitReceiptNoteItems.Where(x => penerimaanunitreceiptnoteItemIds.Contains(x.Id)).Select(s => new { s.ProductCode, s.ProductName, s.RONo, s.SmallUomUnit, s.POSerialNumber, s.ReceiptQuantity, s.DOCurrencyRate, s.PricePerDealUnit, s.Id, s.SmallQuantity, s.Conversion, s.ProductRemark }).ToList();
            var penerimaandeliveryorderdetailIds = IdTerima.Select(x => x.DoDetailId).ToList();
            var penerimaandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => penerimaandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var penerimaanintrenalpurchaseorderIds = IdTerima.Select(x => x.POID).ToList();
            var penerimaanintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => penerimaanintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            var penerimaanExternalPurchaseOrderIds = IdTerima.Select(x => x.EPOId).ToList();
            var penerimaanExternalPurchaseOrders = dbContext.GarmentExternalPurchaseOrders.Where(x => penerimaanExternalPurchaseOrderIds.Contains(x.Id)).Select(s => new { s.PaymentMethod, s.Id }).ToList();

            foreach (var item in IdTerima) {
                var penerimaanunitreceiptnote = penerimaanunitreceiptnotes.FirstOrDefault(x => x.Id == item.UrnId);
                var penerimaanuntreceiptnoteItem = penerimaanuntreceiptnoteItems.FirstOrDefault(x => x.Id == item.UrnItemId);
                var penerimaandeliveryorderdetail = penerimaandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var penerimaanintrenalpurchaseorder = penerimaanintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);
                var penerimaanExternalPurchaseOrder = penerimaanExternalPurchaseOrders.FirstOrDefault(x => x.Id == item.EPOId);

                penerimaan.Add(new GarmentStockReportViewModel
                {
                    BeginningBalanceQty = 0,
                    BeginningBalanceUom = penerimaanuntreceiptnoteItem.SmallUomUnit,
                    Buyer = penerimaanintrenalpurchaseorder.BuyerCode,
                    EndingBalanceQty = 0,
                    EndingUom = penerimaanuntreceiptnoteItem.SmallUomUnit,
                    ExpandUom = penerimaanuntreceiptnoteItem.SmallUomUnit,
                    ExpendQty = 0,
                    NoArticle = penerimaanintrenalpurchaseorder.Article,
                    PaymentMethod = penerimaanExternalPurchaseOrder == null ? "" : penerimaanExternalPurchaseOrder.PaymentMethod,
                    PlanPo = penerimaanuntreceiptnoteItem.POSerialNumber,
                    POId = penerimaanintrenalpurchaseorder.Id,
                    ProductCode = penerimaanuntreceiptnoteItem.ProductCode,
                    ProductName = penerimaanuntreceiptnoteItem.ProductName,
                    ProductRemark = penerimaanuntreceiptnoteItem.ProductRemark,
                    ReceiptCorrectionQty = 0,
                    ReceiptQty = (decimal)penerimaanuntreceiptnoteItem.ReceiptQuantity * penerimaanuntreceiptnoteItem.Conversion,
                    ReceiptUom = penerimaanuntreceiptnoteItem.SmallUomUnit,
                    RO = penerimaanuntreceiptnoteItem.RONo,
                    UNitCode = "",
                    UnitSenderCode = "",
                    UnitRequestCode = ""
                });
            }
            var IdPengeluaran = (from a in dbContext.GarmentUnitExpenditureNotes
                                 join b in dbContext.GarmentUnitExpenditureNoteItems on a.Id equals b.UENId
                                 join c in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals c.Id
                                 join f in dbContext.GarmentInternalPurchaseOrderItems on b.POItemId equals f.Id
                                 join g in dbContext.GarmentInternalPurchaseOrders on f.GPOId equals g.Id
                                 join h in dbContext.GarmentUnitReceiptNoteItems on b.URNItemId equals h.Id
                                 join i in dbContext.GarmentExternalPurchaseOrderItems on h.EPOItemId equals i.Id into epoitems
                                 from epoitem in epoitems.DefaultIfEmpty()
                                 join j in dbContext.GarmentExternalPurchaseOrders on epoitem.GarmentEPOId equals j.Id into epos
                                 from epo in epos.DefaultIfEmpty()
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
                                     EPOId = epo == null ? 0 : epo.Id
                                 }).ToList().Distinct();
            var pengeluaranUnitExpenditureNoteItemIds = IdPengeluaran.Select(x => x.UENItemsId).ToList();
            var pengeluaranUnitExpenditureNoteItems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => pengeluaranUnitExpenditureNoteItemIds.Contains(x.Id)).Select(s => new { s.Quantity, s.PricePerDealUnit, s.Id, s.ProductCode, s.ProductName, s.RONo, s.POSerialNumber, s.UomUnit, s.DOCurrencyRate, s.URNItemId, s.ProductRemark }).ToList();
            var pengeluaranUnitExpenditureNoteIds = IdPengeluaran.Select(x => x.UENId).ToList();
            var pengeluaranUnitExpenditureNotes = dbContext.GarmentUnitExpenditureNotes.Where(x => pengeluaranUnitExpenditureNoteIds.Contains(x.Id)).Select(s => new { s.UnitSenderCode, s.UnitRequestName, s.ExpenditureTo, s.Id, s.UENNo }).ToList();
            var pengeluarandeliveryorderdetailIds = IdPengeluaran.Select(x => x.DoDetailId).ToList();
            var pengeluarandeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => pengeluarandeliveryorderdetailIds.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id, s.DOQuantity }).ToList();
            var pengeluaranintrenalpurchaseorderIds = IdPengeluaran.Select(x => x.POID).ToList();
            var pengeluaranintrenalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => pengeluaranintrenalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Article, s.Id }).ToList();
            var pengeluaranexternalpurchaseorderIds = IdPengeluaran.Select(x => x.EPOId).ToList();
            var pengeluaranexternalpurchaseorders = dbContext.GarmentExternalPurchaseOrders.Where(x => pengeluaranexternalpurchaseorderIds.Contains(x.Id)).Select(x => new { x.PaymentMethod, x.Id }).ToList();

            foreach (var item in IdPengeluaran) {
                var pengeluarandeliveryorderdetail = pengeluarandeliveryorderdetails.FirstOrDefault(x => x.Id == item.DoDetailId);
                var pengeluaranintrenalpurchaseorder = pengeluaranintrenalpurchaseorders.FirstOrDefault(x => x.Id == item.POID);
                var pengeluaranUnitExpenditureNoteItem = pengeluaranUnitExpenditureNoteItems.FirstOrDefault(x => x.Id == item.UENItemsId);
                var pengeluaranUnitExpenditureNote = pengeluaranUnitExpenditureNotes.FirstOrDefault(x => x.Id == item.UENId);
                var pengeluaranexternalpurchaseorder = pengeluaranexternalpurchaseorders.FirstOrDefault(x => x.Id == item.EPOId);

                pengeluaran.Add(new GarmentStockReportViewModel
                {
                    BeginningBalanceQty = 0,
                    BeginningBalanceUom = pengeluaranUnitExpenditureNoteItem.UomUnit,
                    Buyer = pengeluaranintrenalpurchaseorder.BuyerCode,
                    EndingBalanceQty = 0,
                    EndingUom = pengeluaranUnitExpenditureNoteItem.UomUnit,
                    ExpandUom = pengeluaranUnitExpenditureNoteItem.UomUnit,
                    ExpendQty = pengeluaranUnitExpenditureNoteItem.Quantity,
                    NoArticle = pengeluaranintrenalpurchaseorder.Article,
                    PaymentMethod = pengeluaranexternalpurchaseorder == null ? "" : pengeluaranexternalpurchaseorder.PaymentMethod,
                    PlanPo = pengeluaranUnitExpenditureNoteItem.POSerialNumber,
                    POId = pengeluaranintrenalpurchaseorder.Id,
                    ProductCode = pengeluaranUnitExpenditureNoteItem.ProductCode,
                    ProductName = pengeluaranUnitExpenditureNoteItem.ProductName,
                    ProductRemark = pengeluaranUnitExpenditureNoteItem.ProductRemark,
                    ReceiptCorrectionQty = 0,
                    ReceiptQty = 0,
                    ReceiptUom = pengeluaranUnitExpenditureNoteItem.UomUnit,
                    RO = pengeluaranUnitExpenditureNoteItem.RONo,
                    UNitCode = "",
                    UnitSenderCode = "",
                    UnitRequestCode = ""
                });

            }

            var SAkhir = penerimaan.Concat(pengeluaran).ToList();
            var SaldoAkhir = (from a in SAkhir
                              group a by new { a.PlanPo, a.ProductCode } into data
                              select new GarmentStockReportViewModel
                              {
                                  BeginningBalanceQty = Math.Round(data.Sum(x => x.BeginningBalanceQty), 2),
                                  BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                                  Buyer = data.FirstOrDefault().Buyer,
                                  EndingBalanceQty = Math.Round(data.Sum(x => x.EndingBalanceQty), 2),
                                  EndingUom = data.FirstOrDefault().EndingUom,
                                  ExpandUom = data.FirstOrDefault().ExpandUom,
                                  ExpendQty = Math.Round(data.Sum(x => x.ExpendQty), 2),
                                  NoArticle = data.FirstOrDefault().NoArticle,
                                  PaymentMethod = data.FirstOrDefault().PaymentMethod,
                                  PlanPo = data.FirstOrDefault().PlanPo,
                                  POId = data.FirstOrDefault().POId,
                                  ProductCode = data.FirstOrDefault().ProductCode,
                                  ProductName = data.FirstOrDefault().ProductName,
                                  ProductRemark = data.FirstOrDefault().ProductRemark,
                                  ReceiptCorrectionQty = Math.Round(data.Sum(x => x.ReceiptCorrectionQty), 2),
                                  ReceiptQty = Math.Round(data.Sum(x => x.ReceiptQty), 2),
                                  ReceiptUom = data.FirstOrDefault().ReceiptUom,
                                  RO = data.FirstOrDefault().RO,
                                  UNitCode = data.FirstOrDefault().UNitCode,
                                  UnitSenderCode = data.FirstOrDefault().UnitSenderCode,
                                  UnitRequestCode = data.FirstOrDefault().UnitRequestCode
                              }).ToList();
            #endregion

            var SaldoAkhirs2 = SaldoAwal.Concat(SaldoAkhir).ToList();
            var stock = (from a in SaldoAkhirs2
                         group a by new { a.PlanPo, a.ProductCode } into data
                         select new GarmentStockReportViewModel
                         {
                             BeginningBalanceQty = Math.Round(data.Sum(x => x.BeginningBalanceQty), 2),
                             BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                             Buyer = data.FirstOrDefault().Buyer,
                             EndingBalanceQty = Math.Round(data.Sum(x => x.BeginningBalanceQty) + data.Sum(x => x.ReceiptQty) + data.Sum(x => x.ReceiptCorrectionQty) - (decimal)data.Sum(x => x.ExpendQty), 2),
                             EndingUom = data.FirstOrDefault().EndingUom,
                             ExpandUom = data.FirstOrDefault().ExpandUom,
                             ExpendQty = Math.Round(data.Sum(x => x.ExpendQty), 2),
                             NoArticle = data.FirstOrDefault().NoArticle,
                             PaymentMethod = data.FirstOrDefault().PaymentMethod,
                             PlanPo = data.FirstOrDefault().PlanPo,
                             POId = data.FirstOrDefault().POId,
                             ProductCode = data.FirstOrDefault().ProductCode,
                             ProductName = data.FirstOrDefault().ProductName,
                             ProductRemark = data.FirstOrDefault().ProductRemark,
                             ReceiptCorrectionQty = Math.Round(data.Sum(x => x.ReceiptCorrectionQty), 2),
                             ReceiptQty = Math.Round(data.Sum(x => x.ReceiptQty), 2),
                             ReceiptUom = data.FirstOrDefault().ReceiptUom,
                             RO = data.FirstOrDefault().RO,
                             UNitCode = data.FirstOrDefault().UNitCode,
                             UnitSenderCode = data.FirstOrDefault().UnitSenderCode,
                             UnitRequestCode = data.FirstOrDefault().UnitRequestCode
                         }).ToList();

            //var saldoawalbalanceepoid = SaldoAwal.Select(x => x.EPOID).ToList();

                        //var SAkhir = (from a in dbContext.GarmentUnitReceiptNotes
                        //              join b in dbContext.GarmentUnitReceiptNoteItems on a.Id equals b.URNId
                        //              join h in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals h.Id
                        //              join f in dbContext.GarmentInternalPurchaseOrders on b.POId equals f.Id
                        //              join e in dbContext.GarmentReceiptCorrectionItems on b.Id equals e.URNItemId into RC
                        //              from ty in RC.DefaultIfEmpty()
                        //              join c in dbContext.GarmentUnitExpenditureNoteItems on b.Id equals c.URNItemId into UE
                        //              from ww in UE.DefaultIfEmpty()
                        //              join r in dbContext.GarmentUnitExpenditureNotes on ww.UENId equals r.Id into UEN
                        //              from dd in UEN.DefaultIfEmpty()
                        //              join epoItem in dbContext.GarmentExternalPurchaseOrderItems on b.EPOItemId equals epoItem.Id into EP
                        //              from epoItem in EP.DefaultIfEmpty()
                        //              join epo in dbContext.GarmentExternalPurchaseOrders on epoItem.GarmentEPOId equals epo.Id into EPO
                        //              from epo in EPO.DefaultIfEmpty()
                        //              where h.CodeRequirment == (String.IsNullOrWhiteSpace(ctg) ? h.CodeRequirment : ctg)
                        //              && a.IsDeleted == false && b.IsDeleted == false
                        //              && a.CreatedUtc.AddHours(offset).Date >= DateFrom.Date
                        //              && a.CreatedUtc.AddHours(offset).Date <= DateTo.Date
                        //              && a.UnitCode == (string.IsNullOrWhiteSpace(unitcode) ? a.UnitCode : unitcode)
                        //              && b.ProductCode == "TC12440"
                        //              && b.POSerialNumber == "PM202000737M"
                        //              select new
                        //              {
                        //                  URNId = a.Id,
                        //                  URNItemsId = b.Id,
                        //                  DODEtailId = h.Id,
                        //                  IPOId = f.Id,
                        //                  RCorrId = ty == null ? 0 : ty.Id,
                        //                  UENItemsId = ww == null ? 0 : ww.Id,
                        //                  UENId = dd == null ? 0 : dd.Id,
                        //                  EPOItemId = epoItem == null ? 0 : epoItem.Id,
                        //                  EPOId = epo == null ? 0 : epo.Id,
                        //                  UnitCode = a.UnitCode,
                        //                  UnitSenderCode = dd == null ? "-" : dd.UnitSenderCode

                        //              }).Distinct().ToList();
                        ////SaldoAkhir = SaldoAkhir.Where(x => x.UENNo.Contains((String.IsNullOrWhiteSpace(unitcode) ? x.UnitCode : unitcode)) || x.UnitCode == ((String.IsNullOrWhiteSpace(unitcode) ? x.UnitCode : unitcode))).Select(x => x).ToList();
                        //var SAkhirs = SAkhir.Select(x => x).ToList();
                        //var unitreceiptnoteIds = SAkhirs.Select(x => x.URNId).ToList();
                        //var unitreceiptnotes = dbContext.GarmentUnitReceiptNotes.Where(x => unitreceiptnoteIds.Contains(x.Id)).Select(s => new { s.ReceiptDate, s.URNType, s.UnitCode, s.Id }).ToList();
                        //var unitreceiptnoteItemIds = SAkhirs.Select(x => x.URNItemsId).ToList();
                        //var unitreceiptnoteItems = dbContext.GarmentUnitReceiptNoteItems.Where(x => unitreceiptnoteItemIds.Contains(x.Id)).Select(s => new { s.ProductCode, s.ProductName, s.ProductRemark, s.RONo, s.SmallUomUnit, s.POSerialNumber, s.ReceiptQuantity, s.PricePerDealUnit, s.Id }).ToList();
                        //var deliveryorderdetailid = SAkhirs.Select(x => x.DODEtailId).ToList();
                        //var deliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => deliveryorderdetailid.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id }).ToList();
                        //var purchaseorderIds = SAkhirs.Select(x => x.IPOId).ToList();
                        //var purchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => purchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Id, s.Article }).ToList();
                        //var receiptCorrectionitemIds = SAkhirs.Select(x => x.RCorrId).ToList();
                        //var receiptCorrectionitems = dbContext.GarmentReceiptCorrectionItems.Where(x => receiptCorrectionitemIds.Contains(x.Id)).Select(s => new { s.CorrectionQuantity, s.PricePerDealUnit, s.Id }).ToList();
                        //var unitexpenditureitemIds = SAkhirs.Select(x => x.UENItemsId).ToList();
                        //var unitexpenditureitems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => unitexpenditureitemIds.Contains(x.Id)).Select(s => new { s.PricePerDealUnit, s.Quantity, s.Id }).ToList();
                        //var unitexpenditureIds = SAkhirs.Select(x => x.UENId).ToList();
                        //var unitexpenditures = dbContext.GarmentUnitExpenditureNotes.Where(x => unitexpenditureIds.Contains(x.Id)).Select(s => new { s.UnitRequestCode, s.UnitSenderCode, s.ExpenditureTo, s.Id }).ToList();
                        //var externalpurchaseorderitemIds = SAkhirs.Select(x => x.EPOItemId).ToList();
                        //var externalpurchaseorderitems = dbContext.GarmentExternalPurchaseOrderItems.Where(x => externalpurchaseorderitemIds.Contains(x.Id)).Select(s => new { s.Id }).ToList();
                        //var externalpurchaseorderIds = SAkhirs.Select(x => x.EPOId).ToList();
                        //var externalpurchaseorders = dbContext.GarmentExternalPurchaseOrders.Where(x => externalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.PaymentMethod, s.Id }).ToList();
                        ////var balancestocks = SaldoAwal.Where(x => externalpurchaseorderitemIds.Contains((long)x.EPOItemId)).Select(s => new { s.BeginningBalanceQty, s.BeginningBaancePrice, s.EPOItemId}).ToList();
                        //List<GarmentStockReportViewModel> SaldoAkhir = new List<GarmentStockReportViewModel>();
                        //foreach (var item in SAkhir)
                        //{
                        //    var unitreceiptnote = unitreceiptnotes.Where(x => x.Id.Equals(item.URNId)).FirstOrDefault();
                        //    var unitreceiptnoteItem = unitreceiptnoteItems.FirstOrDefault(x => x.Id.Equals(item.URNItemsId));
                        //    var deliveryorderdetail = deliveryorderdetails.FirstOrDefault(x => x.Id.Equals(item.DODEtailId));
                        //    var purchaseorder = purchaseorders.FirstOrDefault(x => x.Id.Equals(item.IPOId));
                        //    var receiptCorrectionitem = receiptCorrectionitems.FirstOrDefault(x => x.Id.Equals(item.RCorrId));
                        //    var unitexpenditureitem = unitexpenditureitems.FirstOrDefault(x => x.Id.Equals(item.UENItemsId));
                        //    var unitexpenditure = unitexpenditures.FirstOrDefault(x => x.Id.Equals(item.UENId));
                        //    var externalpurchaseorderitem = externalpurchaseorderitems.FirstOrDefault(x => x.Id.Equals(item.EPOItemId));
                        //    var externalpurchaseorder = externalpurchaseorders.FirstOrDefault(x => x.Id.Equals(item.EPOId));
                        //    //var balancestock = balancestocks.FirstOrDefault(x => x.EPOItemId.Equals(item.EPOItemId));

                        //    SaldoAkhir.Add(new GarmentStockReportViewModel
                        //    {
                        //        BeginningBalanceQty = 0,
                        //        BeginningBalanceUom = unitreceiptnoteItem.SmallUomUnit,
                        //        Buyer = purchaseorder.BuyerCode,
                        //        EndingBalanceQty = 0,
                        //        EndingUom = unitreceiptnoteItem.SmallUomUnit,
                        //        ExpandUom = unitreceiptnoteItem.SmallUomUnit,
                        //        ExpendQty = unitexpenditureitem == null ? 0 : unitexpenditureitem.Quantity,
                        //        NoArticle = purchaseorder.Article,
                        //        PaymentMethod = externalpurchaseorder == null ? "-" : externalpurchaseorder.PaymentMethod,
                        //        PlanPo = unitreceiptnoteItem == null ? "-" : unitreceiptnoteItem.POSerialNumber,
                        //        POId = purchaseorder.Id,
                        //        ProductCode = unitreceiptnoteItem.ProductCode,
                        //        ProductName = unitreceiptnoteItem.ProductName,
                        //        ProductRemark = unitreceiptnoteItem.ProductRemark,
                        //        ReceiptCorrectionQty = receiptCorrectionitem == null ? 0 : (decimal)receiptCorrectionitem.CorrectionQuantity,
                        //        ReceiptQty = unitreceiptnoteItem.ReceiptQuantity,
                        //        ReceiptUom = unitreceiptnoteItem.SmallUomUnit,
                        //        RO = unitreceiptnoteItem.RONo,
                        //        UNitCode = unitreceiptnote.UnitCode,
                        //        UnitSenderCode = unitexpenditure == null ? "-" : unitexpenditure.UnitSenderCode,
                        //        UnitRequestCode = unitexpenditure == null ? "-" : unitexpenditure.UnitRequestCode
                        //    });
                        //}

                        //SaldoAkhir = (from query in SaldoAkhir
                        //              group query by new { query.ProductCode, query.ProductName, query.RO, query.PlanPo, query.POId, query.UNitCode, query.UnitSenderCode } into data
                        //              //group query by new { query.POId } into data
                        //              select new GarmentStockReportViewModel
                        //              {
                        //                  BeginningBalanceQty = data.Sum(x => x.BeginningBalanceQty),
                        //                  BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                        //                  Buyer = data.FirstOrDefault().Buyer,
                        //                  EndingBalanceQty = data.FirstOrDefault().EndingBalanceQty,
                        //                  EndingUom = data.FirstOrDefault().EndingUom,
                        //                  ExpandUom = data.FirstOrDefault().EndingUom,
                        //                  ExpendQty = data.Sum(x => x.ExpendQty),
                        //                  NoArticle = data.FirstOrDefault().NoArticle,
                        //                  PaymentMethod = data.FirstOrDefault().PaymentMethod,
                        //                  PlanPo = data.FirstOrDefault().PlanPo,
                        //                  POId = data.FirstOrDefault().POId,
                        //                  ProductCode = data.FirstOrDefault().ProductCode,
                        //                  ProductName = data.FirstOrDefault().ProductName,
                        //                  ProductRemark = data.FirstOrDefault().ProductRemark,
                        //                  ReceiptCorrectionQty = data.Sum(x => x.ReceiptCorrectionQty),
                        //                  //ReceiptQty = data.Sum(x => x.ReceiptQty),
                        //                  ReceiptQty = data.FirstOrDefault().ReceiptQty,
                        //                  ReceiptUom = data.FirstOrDefault().ReceiptUom,
                        //                  RO = data.FirstOrDefault().RO,
                        //                  UNitCode = data.FirstOrDefault().UNitCode,
                        //                  UnitSenderCode = data.FirstOrDefault().UnitSenderCode,
                        //                  UnitRequestCode = data.FirstOrDefault().UnitRequestCode
                        //              }).ToList();
                        //var SaldoAkhirId = SaldoAkhir.Select(x => x.POId).ToList();
                        //var SA = (from a in dbContext.GarmentUnitReceiptNotes
                        //          join b in dbContext.GarmentUnitReceiptNoteItems on a.Id equals b.URNId
                        //          join i in dbContext.GarmentInternalPurchaseOrders on b.POId equals i.Id
                        //          join c in dbContext.GarmentDeliveryOrderDetails on b.DODetailId equals c.Id
                        //          join e in dbContext.GarmentUnitExpenditureNoteItems on b.Id equals e.URNItemId into UENItem
                        //          from ee in UENItem.DefaultIfEmpty()
                        //          join h in dbContext.GarmentReceiptCorrectionItems on b.Id equals h.URNItemId into RC
                        //          from hh in RC.DefaultIfEmpty()
                        //          join f in dbContext.GarmentUnitExpenditureNotes on ee.UENId equals f.Id into UEN
                        //          from ff in UEN.DefaultIfEmpty()
                        //          join g in dbContext.GarmentExternalPurchaseOrders on b.EPOItemId equals g.Id into Exter
                        //          from gg in Exter.DefaultIfEmpty()
                        //          join epoItem in dbContext.GarmentExternalPurchaseOrderItems on b.EPOItemId equals epoItem.Id into EP
                        //          from epoItem in EP.DefaultIfEmpty()
                        //          join epo in dbContext.GarmentExternalPurchaseOrders on epoItem.GarmentEPOId equals epo.Id into EPO
                        //          from epo in EPO.DefaultIfEmpty()
                        //          where c.CodeRequirment == (string.IsNullOrWhiteSpace(ctg) ? c.CodeRequirment : ctg)
                        //          && a.IsDeleted == false && b.IsDeleted == false
                        //          && a.CreatedUtc > lastdate && a.CreatedUtc < DateFrom
                        //          && a.UnitCode == (string.IsNullOrWhiteSpace(unitcode) ? a.UnitCode : unitcode)
                        //          && SaldoAkhirId.Contains(i.Id)
                        //          select new
                        //          {
                        //              //BSId = bb == null ? "" : bb.BalanceStockId,
                        //              URNItemsId = b == null ? 0 : b.Id,
                        //              UENItemId = ee == null ? 0 : ee.Id,
                        //              RCorItemId = hh != null ? hh.Id : 0,
                        //              URNId = a == null ? 0 : a.Id,
                        //              DODEtailId = c == null ? 0 : c.Id,
                        //              IPOId = i == null ? 0 : i.Id,
                        //              RCorrId = hh == null ? 0 : hh.Id,
                        //              //UENItemsId = ee == null ? 0 : ee.Id,
                        //              UENId = ff == null ? 0 : ff.Id,
                        //              UnitCode = a == null ? "-" : a.UnitCode,
                        //              UnitSenderCode = ff == null ? "-" : ff.UnitSenderCode,
                        //              EPOItemId = epoItem == null ? 0 : epoItem.Id,
                        //              EPOId = epo == null ? 0 : epo.Id,
                        //              //SAQty = (bb != null ? bb.CloseStock : 0) + (hh != null ? hh.CorrectionQuantity : 0) + (double)b.ReceiptQuantity - (ee != null ? ee.Quantity : 0),
                        //              //SAPrice = (bb != null ? bb.ClosePrice : 0) + (hh != null ? (decimal)hh.PricePerDealUnit * (decimal)hh.CorrectionQuantity : 0) +  b.PricePerDealUnit * b.ReceiptQuantity - (ee != null ? (decimal)ee.PricePerDealUnit * (decimal)ee.Quantity : 0)
                        //          }).ToList();

                        //var SaldoAwals = SA.Select(x => x).ToList().Distinct();
                        //var saldoawalunitreceiptnoteIds = SaldoAwals.Select(x => x.URNId).ToList();
                        //var saldoawalunitreceiptnotes = dbContext.GarmentUnitReceiptNotes.Where(x => saldoawalunitreceiptnoteIds.Contains(x.Id)).Select(s => new { s.ReceiptDate, s.URNType, s.UnitCode, s.Id }).ToList();
                        //var saldoawalunitreceiptnoteItemIds = SaldoAwals.Select(x => x.URNItemsId).ToList();
                        //var saldoawalunitreceiptnoteItems = dbContext.GarmentUnitReceiptNoteItems.Where(x => saldoawalunitreceiptnoteItemIds.Contains(x.Id)).Select(s => new { s.ProductCode, s.ProductName, s.ProductRemark, s.RONo, s.SmallUomUnit, s.POSerialNumber, s.ReceiptQuantity, s.PricePerDealUnit, s.Id }).ToList();
                        //var saldoawaldeliveryorderdetailid = SaldoAwals.Select(x => x.DODEtailId).ToList();
                        //var saldoawaldeliveryorderdetails = dbContext.GarmentDeliveryOrderDetails.Where(x => saldoawaldeliveryorderdetailid.Contains(x.Id)).Select(s => new { s.CodeRequirment, s.Id }).ToList();
                        //var saldoawalpurchaseorderIds = SaldoAwals.Select(x => x.IPOId).ToList();
                        //var saldoawalpurchaseorders = dbContext.GarmentInternalPurchaseOrders.Where(x => saldoawalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.BuyerCode, s.Id, s.Article }).ToList();
                        //var saldoawalreceiptCorrectionitemIds = SaldoAwals.Select(x => x.RCorrId).ToList();
                        //var saldoawalreceiptCorrectionitems = dbContext.GarmentReceiptCorrectionItems.Where(x => saldoawalreceiptCorrectionitemIds.Contains(x.Id)).Select(s => new { s.CorrectionQuantity, s.PricePerDealUnit, s.Id }).ToList();
                        //var saldoawalunitexpenditureitemIds = SaldoAwals.Select(x => x.UENItemId).ToList();
                        //var saldoawalunitexpenditureitems = dbContext.GarmentUnitExpenditureNoteItems.Where(x => saldoawalunitexpenditureitemIds.Contains(x.Id)).Select(s => new { s.PricePerDealUnit, s.Quantity, s.Id }).ToList();
                        //var saldoawalunitexpenditureIds = SaldoAwals.Select(x => x.UENId).ToList();
                        //var saldoawalunitexpenditures = dbContext.GarmentUnitExpenditureNotes.Where(x => saldoawalunitexpenditureIds.Contains(x.Id)).Select(s => new { s.UnitRequestCode, s.UnitSenderCode, s.ExpenditureTo, s.Id }).ToList();
                        ////var saldoawalbalancestockepoitemids = SaldoAwals.Select(x => x.BSId).ToList();

                        //var saldoawalexternalpurchaseorderitemIds = SaldoAwals.Select(x => x.EPOItemId).ToList();
                        //var saldoawalexternalpurchaseorderitems = dbContext.GarmentExternalPurchaseOrderItems.Where(x => saldoawalexternalpurchaseorderitemIds.Contains(x.Id)).Select(s => new { s.Id }).ToList();
                        //var saldoawalexternalpurchaseorderIds = SaldoAwals.Select(x => x.EPOId).ToList();
                        //var saldoawalexternalpurchaseorders = dbContext.GarmentExternalPurchaseOrders.Where(x => saldoawalexternalpurchaseorderIds.Contains(x.Id)).Select(s => new { s.PaymentMethod, s.Id }).ToList();
                        //var saldoawalbalancestocks = BalaceStock.Where(x => saldoawalexternalpurchaseorderitemIds.Contains((long)x.EPOItemId)).Select(s => new { s.ArticleNo, s.ClosePrice, s.CloseStock, s.EPOID, s.EPOItemId, s.BalanceStockId }).ToList();
                        //List<GarmentStockReportViewModel> SaldoAwal = new List<GarmentStockReportViewModel>();
                        //foreach (var i in SaldoAwals)
                        //{
                        //    var saldoawalunitreceiptnote = saldoawalunitreceiptnotes.AsParallel().FirstOrDefault(x => x.Id == i.URNId);
                        //    var saldoawalunitreceiptnoteItem = saldoawalunitreceiptnoteItems.AsParallel().FirstOrDefault(x => x.Id.Equals(i.URNItemsId));
                        //    var saldoawaldeliveryorderdetail = saldoawaldeliveryorderdetails.AsParallel().FirstOrDefault(x => x.Id.Equals(i.DODEtailId));
                        //    var saldoawalpurchaseorder = saldoawalpurchaseorders.AsParallel().FirstOrDefault(x => x.Id.Equals(i.IPOId));
                        //    var saldoawalreceiptCorrectionitem = saldoawalreceiptCorrectionitems.AsParallel().FirstOrDefault(x => x.Id.Equals(i.RCorrId));
                        //    var saldoawalunitexpenditureitem = saldoawalunitexpenditureitems.AsParallel().FirstOrDefault(x => x.Id.Equals(i.URNItemsId));
                        //    var saldoawalunitexpenditure = saldoawalunitexpenditures.AsParallel().FirstOrDefault(x => x.Id.Equals(i.URNId));
                        //    var saldoawalbalancestock = saldoawalbalancestocks.AsParallel().FirstOrDefault(x => x.EPOItemId.Equals(i.EPOItemId));
                        //    var saldoawalexternalpurchaseorderitem = saldoawalexternalpurchaseorderitems.AsParallel().FirstOrDefault(x => x.Id.Equals(i.EPOItemId));
                        //    var saldoawalexternalpurchaseorder = saldoawalexternalpurchaseorders.AsParallel().FirstOrDefault(x => x.Id.Equals(i.EPOId));

                        //    SaldoAwal.Add(new GarmentStockReportViewModel
                        //    {
                        //        BeginningBalanceQty = saldoawalbalancestock == null ? (saldoawalreceiptCorrectionitem != null ? (decimal)saldoawalreceiptCorrectionitem.CorrectionQuantity : 0) + saldoawalunitreceiptnoteItem.ReceiptQuantity - (saldoawalunitexpenditureitem != null ? (decimal)saldoawalunitexpenditureitem.Quantity : 0) : (decimal)saldoawalbalancestock.CloseStock + (saldoawalreceiptCorrectionitem != null ? (decimal)saldoawalreceiptCorrectionitem.CorrectionQuantity : 0) + saldoawalunitreceiptnoteItem.ReceiptQuantity - (saldoawalunitexpenditureitem != null ? (decimal)saldoawalunitexpenditureitem.Quantity : 0),
                        //        //BeginningBalanceQty = saldoawalbalancestock != null ? (decimal)saldoawalbalancestock.CloseStock : 0) + (saldoawalreceiptCorrectionitem != null ? (decimal)saldoawalreceiptCorrectionitem.CorrectionQuantity : 0) + saldoawalunitreceiptnoteItem.ReceiptQuantity - (saldoawalunitexpenditureitem != null ? (decimal)saldoawalunitexpenditureitem.Quantity : 0,
                        //        BeginningBalanceUom = saldoawalunitreceiptnoteItem.SmallUomUnit,
                        //        Buyer = saldoawalpurchaseorder.BuyerCode,
                        //        EndingBalanceQty = 0,
                        //        EndingUom = saldoawalunitreceiptnoteItem.SmallUomUnit,
                        //        ExpandUom = saldoawalunitreceiptnoteItem.SmallUomUnit,
                        //        ExpendQty = saldoawalunitexpenditureitem == null ? 0 : saldoawalunitexpenditureitem.Quantity,
                        //        NoArticle = saldoawalpurchaseorder.Article,
                        //        PaymentMethod = saldoawalexternalpurchaseorder == null ? "-" : saldoawalexternalpurchaseorder.PaymentMethod,
                        //        PlanPo = saldoawalunitreceiptnoteItem == null ? "-" : saldoawalunitreceiptnoteItem.POSerialNumber,
                        //        POId = saldoawalpurchaseorder == null ? 0 : saldoawalpurchaseorder.Id,
                        //        ProductCode = saldoawalunitreceiptnoteItem.ProductCode,
                        //        ProductName = saldoawalunitreceiptnoteItem.ProductName,
                        //        ProductRemark = saldoawalunitreceiptnoteItem.ProductRemark,
                        //        ReceiptCorrectionQty = saldoawalreceiptCorrectionitem == null ? 0 : (decimal)saldoawalreceiptCorrectionitem.CorrectionQuantity,
                        //        ReceiptQty = saldoawalunitreceiptnoteItem.ReceiptQuantity,
                        //        ReceiptUom = saldoawalunitreceiptnoteItem.SmallUomUnit,
                        //        RO = saldoawalunitreceiptnoteItem.RONo,
                        //        UNitCode = saldoawalunitreceiptnote.UnitCode,
                        //        UnitSenderCode = saldoawalunitexpenditure == null ? "-" : saldoawalunitexpenditure.UnitSenderCode,
                        //        UnitRequestCode = saldoawalunitexpenditure == null ? "-" : saldoawalunitexpenditure.UnitRequestCode
                        //    });
                        //}

                        //SaldoAwal = (from query in SaldoAwal
                        //             group query by new { query.ProductCode, query.ProductName, query.RO, query.PlanPo, query.POId, query.UNitCode, query.UnitSenderCode } into data
                        //             select new GarmentStockReportViewModel
                        //             {
                        //                 BeginningBalanceQty = data.Sum(x => x.BeginningBalanceQty),
                        //                 BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                        //                 Buyer = data.FirstOrDefault().Buyer,
                        //                 EndingBalanceQty = data.FirstOrDefault().EndingBalanceQty,
                        //                 EndingUom = data.FirstOrDefault().EndingUom,
                        //                 ExpandUom = data.FirstOrDefault().EndingUom,
                        //                 ExpendQty = 0,
                        //                 NoArticle = data.FirstOrDefault().NoArticle,
                        //                 PaymentMethod = data.FirstOrDefault().PaymentMethod,
                        //                 PlanPo = data.FirstOrDefault().PlanPo,
                        //                 POId = data.FirstOrDefault().POId,
                        //                 ProductCode = data.FirstOrDefault().ProductCode,
                        //                 ProductName = data.FirstOrDefault().ProductName,
                        //                 ProductRemark = data.FirstOrDefault().ProductRemark,
                        //                 ReceiptCorrectionQty = 0,
                        //                 ReceiptQty = 0,
                        //                 ReceiptUom = data.FirstOrDefault().ReceiptUom,
                        //                 RO = data.FirstOrDefault().RO,
                        //                 UNitCode = data.FirstOrDefault().UNitCode,
                        //                 UnitSenderCode = data.FirstOrDefault().UnitSenderCode,
                        //                 UnitRequestCode = data.FirstOrDefault().UnitRequestCode
                        //             }).ToList();


                        //var stock = (from a in SaldoAkhir
                        //            join b in SaldoAwal on a.POId equals b.POId into stockdata
                        //            from bb in stockdata.DefaultIfEmpty()
                        //            select new GarmentStockReportViewModel
                        //            {
                        //                BeginningBalanceQty = (bb == null ? 0 : bb.BeginningBalanceQty),
                        //                BeginningBalanceUom = a.BeginningBalanceUom,
                        //                Buyer = a.Buyer,
                        //                EndingBalanceQty = (bb == null ? 0 : bb.BeginningBalanceQty) + a.ReceiptQty + a.ReceiptCorrectionQty - (decimal)a.ExpendQty,
                        //                EndingUom = a.EndingUom,
                        //                ExpendQty = a.ExpendQty,
                        //                ExpandUom = a.ExpandUom,
                        //                NoArticle = a.NoArticle,
                        //                PaymentMethod = a.PaymentMethod,
                        //                PlanPo = a.PlanPo,
                        //                POId = a.POId,
                        //                ProductCode = a.ProductCode,
                        //                ProductName = a.ProductName,
                        //                ProductRemark = a.ProductRemark,
                        //                ReceiptCorrectionQty = a.ReceiptCorrectionQty,
                        //                ReceiptQty = a.ReceiptQty,
                        //                ReceiptUom = a.ReceiptUom,
                        //                RO = a.RO,
                        //                UNitCode = a.UNitCode,
                        //                UnitSenderCode = a.UnitSenderCode,
                        //                UnitRequestCode = a.UnitRequestCode
                        //            }).ToList();
                        //var stock = SaldoAwal.Concat(SaldoAkhir).ToList();
                        //stock = (from query in stock
                        //         group query by new { query.POId, query.ProductCode, query.RO } into data
                        //         select new GarmentStockReportViewModel
                        //         {
                        //             BeginningBalanceQty = data.Sum(x => x.BeginningBalanceQty),
                        //             BeginningBalanceUom = data.FirstOrDefault().BeginningBalanceUom,
                        //             Buyer = data.FirstOrDefault().Buyer,
                        //             EndingBalanceQty = data.Sum(x => x.BeginningBalanceQty) + data.Sum(x => x.ReceiptQty) + data.Sum(x => x.ReceiptCorrectionQty) - data.Sum(x => (decimal)x.ExpendQty),
                        //             EndingUom = data.FirstOrDefault().EndingUom,
                        //             ExpandUom = data.FirstOrDefault().EndingUom,
                        //             ExpendQty = data.Sum(x => x.ExpendQty),
                        //             NoArticle = data.FirstOrDefault().NoArticle,
                        //             PaymentMethod = data.FirstOrDefault().PaymentMethod,
                        //             PlanPo = data.FirstOrDefault().PlanPo,
                        //             POId = data.FirstOrDefault().POId,
                        //             ProductCode = data.FirstOrDefault().ProductCode,
                        //             ProductName = data.FirstOrDefault().ProductName,
                        //             ProductRemark = data.FirstOrDefault().ProductRemark,
                        //             ReceiptCorrectionQty = data.Sum(x => x.ReceiptCorrectionQty),
                        //             ReceiptQty = data.Sum(x => x.ReceiptQty),
                        //             ReceiptUom = data.FirstOrDefault().ReceiptUom,
                        //             RO = data.FirstOrDefault().RO,
                        //             UNitCode = data.FirstOrDefault().UNitCode,
                        //             UnitSenderCode = data.FirstOrDefault().UnitSenderCode,
                        //             UnitRequestCode = data.FirstOrDefault().UnitRequestCode
                        //         }).ToList();
            return stock;
            //return SaldoAwal;

        }

        public Tuple<List<GarmentStockReportViewModel>, int> GetStockReport(int offset, string unitcode, string tipebarang, int page, int size, string Order, DateTime? dateFrom, DateTime? dateTo)
        {
            //var Query = GetStockQuery(tipebarang, unitcode, dateFrom, dateTo, offset);
            //Query = Query.OrderByDescending(x => x.SupplierName).ThenBy(x => x.Dono);
            List<GarmentStockReportViewModel> Data = GetStockQuery(tipebarang, unitcode, dateFrom, dateTo, offset).ToList();
            Data = Data.OrderBy(x => x.ProductCode).ThenBy(x => x.PlanPo).ToList();
            //int TotalData = Data.Count();
            return Tuple.Create(Data, Data.Count());
        }

        public MemoryStream GenerateExcelStockReport(string ctg, string categoryname, string unitname, string unitcode, DateTime? datefrom, DateTime? dateto, int offset)
        {
            var data = GetStockQuery(ctg, unitcode, datefrom, dateto, offset);
            var Query = data.OrderBy(x => x.ProductCode).ThenBy(x => x.PlanPo).ToList();
            DataTable result = new DataTable();
            var headers = new string[] { "No","Kode Barang", "No RO", "Plan PO", "Artikel", "Nama Barang","Keterangan Barang", "Buyer","Saldo Awal","Saldo Awal2", "Penerimaan", "Penerimaan1", "Penerimaan2","Pengeluaran","Pengeluaran1", "Saldo Akhir", "Saldo Akhir1", "Asal" }; 
            var subheaders = new string[] { "Jumlah", "Sat", "Jumlah", "Koreksi", "Sat", "Jumlah", "Sat", "Jumlah", "Sat" };
            for (int i = 0; i < 8; i++)
            {
                result.Columns.Add(new DataColumn() { ColumnName = headers[i], DataType = typeof(string) });
            }

            result.Columns.Add(new DataColumn() { ColumnName = headers[8], DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[9], DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[10], DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[11], DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[12], DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[13], DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[14], DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[15], DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[16], DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = headers[17], DataType = typeof(String) });
            var index = 1;
            decimal BeginningQtyTotal = 0;
            decimal ReceiptQtyTotal = 0;
            double ExpendQtyTotal = 0;
            decimal EndingQtyTotal = 0;
            foreach (var item in Query)
            {
                BeginningQtyTotal += item.BeginningBalanceQty;
                ReceiptQtyTotal += item.ReceiptQty;
                ExpendQtyTotal += item.ExpendQty;
                EndingQtyTotal += item.EndingBalanceQty;

                //result.Rows.Add(index++, item.ProductCode, item.RO, item.PlanPo, item.NoArticle, item.ProductName, item.Information, item.Buyer,

                //    item.BeginningBalanceQty, item.BeginningBalanceUom, item.ReceiptQty, item.ReceiptCorrectionQty, item.ReceiptUom,
                //    NumberFormat(item.ExpendQty),
                //    item.ExpandUom, item.EndingBalanceQty, item.EndingUom, item.From);


                result.Rows.Add(index++, item.ProductCode, item.RO, item.PlanPo, item.NoArticle, item.ProductName, item.ProductRemark, item.Buyer,

                    Convert.ToDouble(item.BeginningBalanceQty), item.BeginningBalanceUom, Convert.ToDouble(item.ReceiptQty), Convert.ToDouble(item.ReceiptCorrectionQty), item.ReceiptUom,
                    item.ExpendQty,
                    item.ExpandUom, Convert.ToDouble(item.EndingBalanceQty), item.EndingUom,
                    item.PaymentMethod == "FREE FROM BUYER" || item.PaymentMethod == "CMT" || item.PaymentMethod == "CMT/IMPORT" ? "BY" : "BL");

            }

            ExcelPackage package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Data");

            var col = (char)('A' + result.Columns.Count);
            string tglawal = new DateTimeOffset(datefrom.Value).ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
            string tglakhir = new DateTimeOffset(dateto.Value).ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
            sheet.Cells[$"A1:{col}1"].Value = string.Format("LAPORAN STOCK GUDANG {0}", categoryname);
            sheet.Cells[$"A1:{col}1"].Merge = true;
            sheet.Cells[$"A1:{col}1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            sheet.Cells[$"A1:{col}1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            sheet.Cells[$"A1:{col}1"].Style.Font.Bold = true;
            sheet.Cells[$"A2:{col}2"].Value = string.Format("Periode {0} - {1}", tglawal, tglakhir);
            sheet.Cells[$"A2:{col}2"].Merge = true;
            sheet.Cells[$"A2:{col}2"].Style.Font.Bold = true;
            sheet.Cells[$"A2:{col}2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            sheet.Cells[$"A2:{col}2"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            sheet.Cells[$"A3:{col}3"].Value = string.Format("KONFEKSI : {0}", unitname);
            sheet.Cells[$"A3:{col}3"].Merge = true;
            sheet.Cells[$"A3:{col}3"].Style.Font.Bold = true;
            sheet.Cells[$"A3:{col}3"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            sheet.Cells[$"A3:{col}3"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;


            sheet.Cells["A7"].LoadFromDataTable(result, false, OfficeOpenXml.Table.TableStyles.Light16);
            sheet.Cells["I5"].Value = headers[8];
            sheet.Cells["I5:J5"].Merge = true;

            sheet.Cells["K5"].Value = headers[10];
            sheet.Cells["K5:M5"].Merge = true;
            sheet.Cells["N5"].Value = headers[13];
            sheet.Cells["N5:O5"].Merge = true;
            sheet.Cells["P5"].Value = headers[15];
            sheet.Cells["P5:Q5"].Merge = true;

            foreach (var i in Enumerable.Range(0, 8))
            {
                col = (char)('A' + i);
                sheet.Cells[$"{col}5"].Value = headers[i];
                sheet.Cells[$"{col}5:{col}6"].Merge = true;
            }

            for (var i = 0; i < 9; i++)
            {
                col = (char)('I' + i);
                sheet.Cells[$"{col}6"].Value = subheaders[i];

            }

            foreach (var i in Enumerable.Range(0, 1))
            {
                col = (char)('R' + i);
                sheet.Cells[$"{col}5"].Value = headers[i + 17];
                sheet.Cells[$"{col}5:{col}6"].Merge = true;
            }

            sheet.Cells["A5:R6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells["A5:R6"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet.Cells["A5:R6"].Style.Font.Bold = true;
            var widths = new int[] {10, 15, 15, 20, 20, 15, 20, 15, 10, 10, 10, 10, 10, 10, 10, 10, 10,15 };
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                sheet.Column(i + 1).Width = widths[i];
            }

            var a = Query.Count();
            sheet.Cells[$"A{6 + a}"].Value = "T O T A L  . . . . . . . . . . . . . . .";
            sheet.Cells[$"A{6 + a}:H{6 + a}"].Merge = true;
            sheet.Cells[$"A{6 + a}:H{6 + a}"].Style.Font.Bold = true;
            sheet.Cells[$"A{6 + a}:H{6 + a}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[$"A{6 + a}:H{6 + a}"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet.Cells[$"I{6 + a}"].Value = BeginningQtyTotal;
            sheet.Cells[$"K{6 + a}"].Value = ReceiptQtyTotal;
            sheet.Cells[$"N{6 + a}"].Value = ExpendQtyTotal;
            sheet.Cells[$"P{6 + a}"].Value = EndingQtyTotal;

            MemoryStream stream = new MemoryStream();
            package.SaveAs(stream);
            return stream;


        }

        String NumberFormat(double? numb)
        {

            var number = string.Format("{0:0,0.00}", numb);

            return number;
        }

        private class SaldoAwal {
            
            public long EPOID { get; set; }
            public long EPOItemId { get; set; }
            public double BeginningBalanceQty { get; set; }
            public decimal BeginningBaancePrice { get; set; }

        }
    }
}
