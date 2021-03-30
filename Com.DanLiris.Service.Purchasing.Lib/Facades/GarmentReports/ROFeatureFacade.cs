using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentDeliveryOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentReports;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentReports
{
    public class ROFeatureFacade : IROFeatureFacade
    {
        private readonly PurchasingDbContext dbContext;
        public readonly IServiceProvider serviceProvider;
        private readonly DbSet<GarmentDeliveryOrder> dbSet;

        public ROFeatureFacade(IServiceProvider serviceProvider, PurchasingDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<GarmentDeliveryOrder>();
        }

        public Tuple<List<ROFeatureViewModel>, int> GetROReport(int offset, string RO, int page, int size, string Order)
        {
            //var Query = GetStockQuery(tipebarang, unitcode, dateFrom, dateTo, offset);
            //Query = Query.OrderByDescending(x => x.SupplierName).ThenBy(x => x.Dono);
            List<ROFeatureViewModel> Data = GetRO(RO, offset);
            Data = Data.OrderByDescending(x => x.KodeBarang).ToList();
            //int TotalData = Data.Count();
            return Tuple.Create(Data, Data.Count());
        }

        public List<ROFeatureViewModel> GetRO(string RO, int offset)
        {

            List<ROFeatureViewModel> final = new List<ROFeatureViewModel>();

            var penerimaan = (from a in dbContext.GarmentUnitReceiptNoteItems
                              join b in dbContext.GarmentUnitReceiptNotes on a.URNId equals b.Id
                              join c in dbContext.GarmentInternalPurchaseOrders on a.POId equals c.Id
                              where a.RONo == (string.IsNullOrWhiteSpace(RO) ? a.RONo : RO) && a.IsDeleted == false && b.IsDeleted == false
                              select new ROFeatureTemp
                              {
                                  KodeBarang = a.ProductCode,
                                  NamaBarang = a.ProductName,
                                  NoBukti = b.URNNo,
                                  PO = a.POSerialNumber,
                                  Article = c.Article,
                                  QtyTerima = Math.Round((double)(a.ReceiptQuantity * a.Conversion), 2),
                                  QtyKeluar = 0,
                                  RONo = a.RONo,
                                  UomMasuk = a.SmallUomUnit,
                                  UomKeluar = ""
                              }).ToList();
            var pengeluaran = (from a in dbContext.GarmentUnitExpenditureNoteItems
                               join b in dbContext.GarmentUnitExpenditureNotes on a.UENId equals b.Id
                               join c in dbContext.GarmentInternalPurchaseOrderItems on a.POItemId equals c.Id
                               join d in dbContext.GarmentInternalPurchaseOrders on c.GPOId equals d.Id
                               where a.RONo == (string.IsNullOrWhiteSpace(RO) ? a.RONo : RO) && a.IsDeleted == false && b.IsDeleted == false
                               select new ROFeatureTemp
                               {
                                   KodeBarang = a.ProductCode,
                                   NoBukti = b.UENNo,
                                   NamaBarang = a.ProductName,
                                   Article = d.Article,
                                   PO = a.POSerialNumber,
                                   QtyTerima = 0,
                                   QtyKeluar = a.Quantity,
                                   RONo = a.RONo,
                                   UomMasuk = "",
                                   UomKeluar = a.UomUnit
                               }).ToList();

            var report = penerimaan.Union(pengeluaran).ToList();
            var datas = (from a in report
                        group a by new { a.KodeBarang, a.PO } into groupdata
                        select new ROFeatureTemp
                        {
                            KodeBarang = groupdata.Key.KodeBarang,
                            NoBukti = groupdata.FirstOrDefault().NoBukti,
                            NamaBarang = groupdata.FirstOrDefault().NamaBarang,
                            Article = groupdata.FirstOrDefault().Article,
                            PO = groupdata.Key.PO,
                            QtyTerima = groupdata.Sum(x => x.QtyTerima),
                            QtyKeluar = groupdata.Sum(x => x.QtyKeluar),
                            RONo = groupdata.FirstOrDefault().RONo,
                            UomMasuk = string.Join("", groupdata.FirstOrDefault().UomMasuk),
                            UomKeluar = string.Join("", groupdata.FirstOrDefault().UomKeluar),
                        }).ToList();

            foreach(var data in datas)
            {
                var masuk = from a in dbContext.GarmentUnitReceiptNotes
                            join b in dbContext.GarmentUnitReceiptNoteItems on a.Id equals b.URNId
                            where b.ProductCode == data.KodeBarang && b.POSerialNumber == data.PO && b.RONo == data.RONo
                            select new {
                                a.ReceiptDate,
                                a.URNNo,
                                b.POSerialNumber,
                                b.ProductCode,
                                b.ProductName,
                                Qty = Math.Round(b.ReceiptQuantity * b.Conversion, 2),
                                b.SmallUomUnit,
                                b.RONo
                            };
                var keluar = from a in dbContext.GarmentUnitExpenditureNotes
                             join b in dbContext.GarmentUnitExpenditureNoteItems on a.Id equals b.UENId
                             join c in dbContext.GarmentUnitDeliveryOrderItems on b.UnitDOItemId equals c.Id
                             join d in dbContext.GarmentUnitDeliveryOrders on c.UnitDOId equals d.Id
                             where b.ProductCode == data.KodeBarang && b.POSerialNumber == data.PO && b.RONo == data.RONo
                             select new
                             {
                                 a.ExpenditureDate,
                                 RO = b.RONo,
                                 a.UENNo,
                                 b.POSerialNumber,
                                 b.ProductCode,
                                 b.ProductName,
                                 Qty = b.Quantity,
                                 UomKeluar = b.UomUnit,
                                 c.RONo,
                                 c.Quantity,
                                 c.UomUnit,
                                 d.UnitDONo,
                                 a.ExpenditureType
                             };

                List<RODetailMasukViewModel> masukdata = new List<RODetailMasukViewModel>();
                List<RODetailViewModel> keluardata = new List<RODetailViewModel>();

                foreach (var i in masuk) {
                    masukdata.Add(new RODetailMasukViewModel
                    {
                        ReceiptDate = i.ReceiptDate.DateTime,
                        KodeBarang = i.ProductCode,
                        NamaBarang = i.ProductName,
                        PO = i.POSerialNumber,
                        RONo = i.RONo,
                        NoBukti = i.URNNo,
                        Qty = (double)i.Qty,
                        Uom = i.SmallUomUnit,
                    });
                }

                foreach (var i in keluar) {
                    keluardata.Add(new RODetailViewModel
                    {
                        KodeBarang = i.ProductCode,
                        NamaBarang = i.ProductName,
                        PO = i.POSerialNumber,
                        NoBukti = i.UENNo,
                        Qty = i.Qty,
                        Uom = i.UomKeluar,
                        UomDO = i.UomUnit,
                        JumlahDO = i.Quantity,
                        RO = i.RONo,
                        RONo = i.RO,
                        TanggalKeluar = i.ExpenditureDate.DateTime,
                        Tipe = i.ExpenditureType,
                        UnitDONo = i.UnitDONo
                    });
                }

                final.Add(new ROFeatureViewModel
                {
                    KodeBarang = data.KodeBarang,
                    NamaBarang = data.NamaBarang,
                    Article = data.Article,
                    PO = data.PO,
                    QtyKeluar = data.QtyKeluar,
                    QtyTerima = data.QtyTerima,
                    RONo = data.RONo,
                    UomKeluar = data.UomKeluar,
                    UomMasuk = data.UomMasuk,
                    items = new ROItemViewModel
                    {
                        Masuk = masukdata,
                        Keluar = keluardata
                    }
                });
            }

            return final;
        }
    }
}
