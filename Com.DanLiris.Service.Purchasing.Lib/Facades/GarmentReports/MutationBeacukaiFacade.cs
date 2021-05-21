using Com.DanLiris.Service.Purchasing.Lib.Helpers;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentDeliveryOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentReports;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.NewIntegrationViewModel;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentReports
{
    public class MutationBeacukaiFacade : IMutationBeacukaiFacade
    {
        private readonly PurchasingDbContext dbContext;
        public readonly IServiceProvider serviceProvider;
        private readonly DbSet<GarmentDeliveryOrder> dbSet;

        public MutationBeacukaiFacade(IServiceProvider serviceProvider, PurchasingDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<GarmentDeliveryOrder>();
        }
        #region BB
        public Tuple<List<MutationBBCentralViewModel>, int> GetReportBBCentral(int page, int size, string Order, DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            //var Query = GetStockQuery(tipebarang, unitcode, dateFrom, dateTo, offset);
            //Query = Query.OrderByDescending(x => x.SupplierName).ThenBy(x => x.Dono);
            List<MutationBBCentralViewModel> Query = GetCentralItemBBReport(dateFrom, dateTo, offset).ToList();
            //Query = Query.OrderBy(x => x.ItemCode).ToList();

            Pageable<MutationBBCentralViewModel> pageable = new Pageable<MutationBBCentralViewModel>(Query, page - 1, size);
            List<MutationBBCentralViewModel> Data = pageable.Data.ToList<MutationBBCentralViewModel>();
            int TotalData = pageable.TotalCount;
            //int TotalData = Data.Count();
            return Tuple.Create(Data, TotalData);
        }

        private List<GarmentCategoryViewModel> GetProductCodes(int page, int size, string order, string filter)
        {
            ////var param = new StringContent(JsonConvert.SerializeObject(codes), Encoding.UTF8, "application/json");
            //IHttpClientService httpClient = (IHttpClientService)this.serviceProvider.GetService(typeof(IHttpClientService));
            ////httpClient.GetAsync($"{APIEndpoint.Core}{currencyUri}/{currencyCode}").Result
            //if (httpClient != null)
            //{
            //    var garmentSupplierUri = APIEndpoint.Core + $"master/garment-categories";
            //    string queryUri = "?page=" + page + "&size=" + size + "&order=" + order + "&filter=" + filter;
            //    string uri = garmentSupplierUri + queryUri;
            //    //var response = httpClient.GetAsync($"{uri}").Result;
            //    var response = httpClient.GetAsync($"{uri}").Result;
            //    var response2  =  response.Content.ReadAsStringAsync();
            //    Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Result);
            //    List<GarmentCategoryViewModel> viewModel = JsonConvert.DeserializeObject<List<GarmentCategoryViewModel>>(result.GetValueOrDefault("data").ToString());
            //    return viewModel;
            //}
            //else
            //{
            //    List<GarmentCategoryViewModel> viewModel = null;
            //    return viewModel;
            //}

            IHttpClientService httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
            var garmentSupplierUri = APIEndpoint.Core + $"master/garment-categories";
            string queryUri = "?page=" + page + "&size=" + size + "&order=" + order + "&filter=" + filter;
            string uri = garmentSupplierUri + queryUri;
            var response = httpClient.GetAsync($"{APIEndpoint.Core}/master/garment-categories?page={page}&size={size}").Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                List<GarmentCategoryViewModel> viewModel = JsonConvert.DeserializeObject<List<GarmentCategoryViewModel>>(result.GetValueOrDefault("data").ToString()); ;
                return viewModel ;
            }
            else
            {
                return null;
            }
        }

        public List<MutationBBCentralViewModel> GetCentralItemBBReport(DateTime? datefrom, DateTime? dateto, int offset)
        {
            DateTime DateFrom = datefrom == null ? new DateTime(1970, 1, 1) : (DateTime)datefrom;
            DateTime DateTo = dateto == null ? DateTime.Now : (DateTime)dateto;

            var pengeluaran = new[] { "PROSES", "SAMPLE", "EXTERNAL" };
            var pemasukan = new[] { "PROSES", "PEMBELIAN" };

            var categories = GetProductCodes(1, int.MaxValue, "{}", "{}");
            var coderequirement = new[] { "BP", "BE" };
            var categories1 = categories.Where(x => x.CodeRequirement == "BB").Select(x => x.Name).ToArray();


            List<MutationBBCentralViewModelTemp> saldoawalreceipt = new List<MutationBBCentralViewModelTemp>();
            List<MutationBBCentralViewModelTemp> saldoawalexpenditure = new List<MutationBBCentralViewModelTemp>();
            List<MutationBBCentralViewModelTemp> saldoawalreceiptcorrection = new List<MutationBBCentralViewModelTemp>();

            #region Balance
            var lastdate = dbContext.BalanceStocks.OrderByDescending(x => x.CreateDate).Select(x => x.CreateDate).FirstOrDefault() == null ? new DateTime(1970, 1, 1) : dbContext.BalanceStocks.OrderByDescending(x => x.CreateDate).Select(x => x.CreateDate).FirstOrDefault();

            var BalanceStock = (from a in (from aa in dbContext.BalanceStocks where aa.CreateDate == lastdate select aa)
                                join b in (from bb in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() where categories1.Contains(bb.ProductName) select bb) on (long)a.EPOItemId equals b.Id
                                join c in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on b.GarmentEPOId equals c.Id
                                select new MutationBBCentralViewModelTemp
                                {
                                    AdjustmentQty = 0,
                                    BeginQty = (double)a.CloseStock,
                                    ExpenditureQty = 0,
                                    ItemCode = b.ProductCode,
                                    ItemName = b.ProductName,
                                    LastQty = 0,
                                    OpnameQty = 0,
                                    ReceiptQty = 0,
                                    SupplierType = c.SupplierImport == false ? "LOKAL" : "IMPORT",
                                    UnitQtyName = b.DealUomUnit

                                });

            var ReceiptBalance = (from a in (from aa in dbContext.GarmentUnitReceiptNotes where aa.CreatedUtc.Date > lastdate
                                             && aa.CreatedUtc.Date < DateFrom.Date && aa.UId == null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType) select aa)
                                  join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                                  join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                  join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                                  //where a.CreatedUtc.Date > lastdate
                                  //&& a.CreatedUtc.Date < DateFrom.Date
                                  //&& b.ProductName == "FABRIC"
                                  //&& a.UId == null
                                  //&& a.IsDeleted == false & b.IsDeleted == false
                                  //&& a.URNType == "PEMBELIAN"
                                  select new MutationBBCentralViewModelTemp
                                  {
                                      AdjustmentQty = 0,
                                      BeginQty = (double)(b.ReceiptQuantity * b.Conversion),
                                      ExpenditureQty = 0,
                                      ItemCode = b.ProductCode,
                                      ItemName = b.ProductName,
                                      LastQty = 0,
                                      OpnameQty = 0,
                                      ReceiptQty = 0,
                                      SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                      UnitQtyName = b.SmallUomUnit
                                  });

            var ReceiptBalanceLocal = (from a in (from aa in dbContext.GarmentUnitReceiptNotes where aa.LastModifiedUtc.Date > lastdate.Value.Date && aa.LastModifiedUtc.Date < DateFrom.Date
                                                  && aa.UId != null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType)
                                                  select aa)
                                       join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                                       join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                       join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                                       //where a.LastModifiedUtc.Date > lastdate.Value.Date
                                       //&& a.LastModifiedUtc.Date < DateFrom.Date
                                       //&& b.ProductName == "FABRIC"
                                       //&& a.UId != null
                                       //&& a.IsDeleted == false & b.IsDeleted == false
                                       //&& a.URNType == "PEMBELIAN"
                                       select new MutationBBCentralViewModelTemp
                                       {
                                           AdjustmentQty = 0,
                                           BeginQty = (double)(b.ReceiptQuantity * b.Conversion),
                                           ExpenditureQty = 0,
                                           ItemCode = b.ProductCode,
                                           ItemName = b.ProductName,
                                           LastQty = 0,
                                           OpnameQty = 0,
                                           ReceiptQty = 0,
                                           SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                           UnitQtyName = b.SmallUomUnit

                                       });
           
            var ExpenditureBalance = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes where aa.CreatedUtc.Date > lastdate && aa.CreatedUtc.Date < DateFrom.Date
                                                 && aa.UId == null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType) select aa)
                                      join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                                      join f in dbContext.GarmentUnitReceiptNoteItems on b.URNItemId equals f.Id
                                      join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on f.EPOItemId equals c.Id
                                      join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id

                                      select new MutationBBCentralViewModelTemp
                                      {
                                          AdjustmentQty = 0,
                                          BeginQty = (double)b.Quantity,
                                          ExpenditureQty = 0,
                                          ItemCode = b.ProductCode,
                                          ItemName = b.ProductName,
                                          LastQty = 0,
                                          OpnameQty = 0,
                                          ReceiptQty = 0,
                                          SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                          UnitQtyName = b.UomUnit

                                      });
            var ExpenditureBalanceLocal = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes
                                                      where aa.LastModifiedUtc.Date > lastdate && aa.LastModifiedUtc.Date < DateFrom.Date
                                                      && aa.UId != null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType)
                                                      select aa)
                                           join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                                           join f in dbContext.GarmentUnitReceiptNoteItems on b.URNItemId equals f.Id
                                           join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                           join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id

                                           
                                           select new MutationBBCentralViewModelTemp
                                           {
                                               AdjustmentQty = 0,
                                               BeginQty = (double)b.Quantity,
                                               ExpenditureQty = 0,
                                               ItemCode = b.ProductCode,
                                               ItemName = b.ProductName,
                                               LastQty = 0,
                                               OpnameQty = 0,
                                               ReceiptQty = 0,
                                               SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                               UnitQtyName = b.UomUnit
                                           });
            
            //var ReceiptCorrectionBalance = (from a in (from aa in dbContext.GarmentReceiptCorrections where aa.CreatedUtc.Date > lastdate && aa.CreatedUtc.Date < DateFrom.Date
            //                                           && aa.UId == null && aa.IsDeleted == false select aa)
            //                                join b in (from bb in dbContext.GarmentReceiptCorrectionItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.CorrectionId
            //                                join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
            //                                join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
            //                                select new MutationBBCentralViewModelTemp
            //                                {
            //                                    AdjustmentQty = 0,
            //                                    BeginQty = (double)b.SmallQuantity,
            //                                    ExpenditureQty = 0,
            //                                    ItemCode = b.ProductCode,
            //                                    ItemName = b.ProductName,
            //                                    LastQty = 0,
            //                                    OpnameQty = 0,
            //                                    ReceiptQty = 0,
            //                                    SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
            //                                    UnitQtyName = b.UomUnit

            //                                });

            
            #endregion
            #region filtered
            var Receipt = (from a in (from aa in dbContext.GarmentUnitReceiptNotes where aa.CreatedUtc.Date >= DateFrom.Date && aa.CreatedUtc.Date <= DateTo.Date 
                                      && aa.UId == null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType)
                                      select aa)
                                  join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                                  join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                  join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                                  
                                  //group new { a, b, c, d } by new { b.ProductCode, b.ProductName, b.SmallUomUnit, d.SupplierImport } into data
                                  select new MutationBBCentralViewModelTemp
                                  {
                                      AdjustmentQty = 0,
                                      BeginQty = 0,
                                      ExpenditureQty = 0,
                                      ItemCode = b.ProductCode,
                                      ItemName = b.ProductName,
                                      LastQty = 0,
                                      OpnameQty = 0,
                                      ReceiptQty = (double)(b.ReceiptQuantity * b.Conversion),
                                      SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                      UnitQtyName = b.SmallUomUnit
                                  });

            var ReceiptLocal = (from a in (from aa in dbContext.GarmentUnitReceiptNotes
                                           where aa.LastModifiedUtc.Date >= DateFrom.Date && aa.LastModifiedUtc.Date <= DateTo.Date
                                            && aa.UId != null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType)
                                           select aa)
                                       join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                                       join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                       join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters()  on c.GarmentEPOId equals d.Id

                                       select new MutationBBCentralViewModelTemp
                                       {
                                           AdjustmentQty = 0,
                                           BeginQty = 0,
                                           ExpenditureQty = 0,
                                           ItemCode = b.ProductCode,
                                           ItemName = b.ProductName,
                                           LastQty = 0,
                                           OpnameQty = 0,
                                           ReceiptQty = (double)(b.ReceiptQuantity * b.Conversion),
                                           SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                           UnitQtyName = b.SmallUomUnit
                                       });
            var Expenditure = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes where aa.CreatedUtc.Date >= DateFrom.Date && aa.CreatedUtc.Date <= DateTo.Date
                                          && aa.UId == null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType) select aa )
                                      join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                                      join f in dbContext.GarmentUnitReceiptNoteItems on b.URNItemId equals f.Id
                                      join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on f.EPOItemId equals c.Id
                                      join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                               select new MutationBBCentralViewModelTemp
                                      {
                                          AdjustmentQty = 0,
                                          BeginQty = 0,
                                          ExpenditureQty = (double)b.Quantity,
                                          ItemCode = b.ProductCode,
                                          ItemName = b.ProductName,
                                          LastQty = 0,
                                          OpnameQty = 0,
                                          ReceiptQty = 0,
                                          SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                          UnitQtyName = b.UomUnit
                                      });
            var ExpenditureLocal = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes
                                               where aa.LastModifiedUtc.Date >= DateFrom.Date && aa.LastModifiedUtc.Date <= DateTo.Date
                                                && aa.UId != null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType)
                                               select aa)
                                    join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                                    join f in dbContext.GarmentUnitReceiptNoteItems on b.URNItemId equals f.Id
                                    join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on f.EPOItemId equals c.Id
                                    join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                                    select new MutationBBCentralViewModelTemp
                                    {
                                        AdjustmentQty = 0,
                                        BeginQty = 0,
                                        ExpenditureQty = b.Quantity,
                                        ItemCode = b.ProductCode,
                                        ItemName = b.ProductName,
                                        LastQty = 0,
                                        OpnameQty = 0,
                                        ReceiptQty = 0,
                                        SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
                                        UnitQtyName = b.UomUnit
                                    });

            //var ReceiptCorrection = (from a in (from aa in dbContext.GarmentReceiptCorrections where aa.CreatedUtc.Date >= DateFrom.Date && aa.CreatedUtc.Date <= DateTo.Date
            //                                    && aa.UId == null && aa.IsDeleted == false select aa)
            //                                join b in (from bb in dbContext.GarmentReceiptCorrectionItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.CorrectionId
            //                                join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
            //                                join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
            //                         //where b.ProductCode == "TC07547"
            //                         select new MutationBBCentralViewModelTemp
            //                                {
            //                                    AdjustmentQty = 0,
            //                                    BeginQty = 0,
            //                                    ExpenditureQty = b.SmallQuantity,
            //                                    ItemCode = b.ProductCode,
            //                                    ItemName = b.ProductName,
            //                                    LastQty = 0,
            //                                    OpnameQty = 0,
            //                                    ReceiptQty = b.SmallQuantity,
            //                                    SupplierType = d.SupplierImport == false ? "LOKAL" : "IMPORT",
            //                                    UnitQtyName = b.UomUnit
            //                                });

            var dataUnion = ReceiptBalance.Union(BalanceStock).Union(ReceiptBalanceLocal).Union(ExpenditureBalance).Union(ExpenditureBalanceLocal).
               // Union(ReceiptCorrectionBalance).
                Union(Receipt).Union(ReceiptLocal)
                .Union(Expenditure).Union(ExpenditureLocal).
                //Union(ReceiptCorrection).
                AsEnumerable();
              
            #endregion

            var mutation = dataUnion.GroupBy(x=>new { x.ItemCode, x.ItemName, x.UnitQtyName, x.SupplierType },(key,group) => new MutationBBCentralViewModel
            {
                AdjustmentQty = Math.Round(Convert.ToDouble(group.Sum(x => x.AdjustmentQty)), 2),
                BeginQty = Math.Round(Convert.ToDouble(group.Sum(x => x.BeginQty)), 2),
                ExpenditureQty = Math.Round(Convert.ToDouble(group.Sum(x => x.ExpenditureQty)), 2),
                ItemCode = key.ItemCode,
                ItemName = key.ItemName,
                LastQty = Math.Round(Convert.ToDouble(group.Sum(x => x.BeginQty)) + Convert.ToDouble(group.Sum(x => x.ReceiptQty)) - Convert.ToDouble(group.Sum(x => x.ExpenditureQty)) + Convert.ToDouble(group.Sum(x => x.AdjustmentQty)) + Convert.ToDouble(group.Sum(x => x.OpnameQty)), 2),
                ReceiptQty = Math.Round(Convert.ToDouble(group.Sum(x => x.ReceiptQty)), 2),
                SupplierType = key.SupplierType,
                UnitQtyName = key.UnitQtyName,
                OpnameQty = 0,
                Diff = 0
            }).OrderBy(x => x.ItemCode).ToList();
            

            var mm = new MutationBBCentralViewModel();

            mm.AdjustmentQty = Math.Round(mutation.Sum(x => x.AdjustmentQty),2);
            mm.BeginQty = Math.Round(mutation.Sum(x => x.BeginQty),2);
            mm.ExpenditureQty = Math.Round(mutation.Sum(x => x.ExpenditureQty), 2);
            mm.ItemCode = "";
            mm.ItemName = "";
            mm.LastQty = Math.Round(mutation.Sum(x => x.LastQty), 2);
            mm.ReceiptQty = Math.Round(mutation.Sum(x => x.ReceiptQty), 2);
            mm.SupplierType = "";
            mm.UnitQtyName = "";
            mm.OpnameQty = 0;
            mm.Diff = 0;
            
            mutation.Add(new MutationBBCentralViewModel {
                AdjustmentQty = mm.AdjustmentQty,
                BeginQty = mm.BeginQty,
                ExpenditureQty = mm.ExpenditureQty,
                ItemCode = mm.ItemCode,
                ItemName = mm.ItemName,
                LastQty = mm.LastQty,
                ReceiptQty = mm.ReceiptQty,
                SupplierType = mm.SupplierType,
                UnitQtyName = mm.UnitQtyName,
                OpnameQty = mm.OpnameQty,
                Diff = mm.Diff
            });

            return mutation;

        }

        public MemoryStream GenerateExcelBBCentral(DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            var Query = GetCentralItemBBReport(dateFrom, dateTo, offset);
            //Query = Query.OrderBy(b => b.ItemCode).ToList();
            DataTable result = new DataTable();
            result.Columns.Add(new DataColumn() { ColumnName = "Kode Barang", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Barang", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tipe", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Satuan", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Saldo Awal", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Pemasukan", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Pengeluaran", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Penyesuaian", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Saldo Akhir", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Stock Opname", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Selisih", DataType = typeof(Double) });
            //if (Query.ToArray().Count() == 0)
            //    result.Rows.Add("", "", "", "", "", "", "", "", "", "", ""); // to allow column name to be generated properly for empty data as template
            //else
            foreach (var item in Query)
            {
                result.Rows.Add((item.ItemCode), item.ItemName, item.SupplierType, item.UnitQtyName, item.BeginQty, item.ReceiptQty, item.ExpenditureQty, item.AdjustmentQty, item.LastQty, item.OpnameQty, item.Diff);
            }

            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Territory") }, true);

        }
        #endregion 
        #region BP
        public Tuple<List<MutationBPCentralViewModel>, int> GetReportBPCentral(int page, int size, string Order, DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            //var Query = GetStockQuery(tipebarang, unitcode, dateFrom, dateTo, offset);
            //Query = Query.OrderByDescending(x => x.SupplierName).ThenBy(x => x.Dono);
            List<MutationBPCentralViewModel> Query = GetCentralItemBPReport(dateFrom, dateTo, offset).ToList();
            //Query = Query.OrderBy(x => x.ItemCode).ToList();

            Pageable<MutationBPCentralViewModel> pageable = new Pageable<MutationBPCentralViewModel>(Query, page - 1, size);
            List<MutationBPCentralViewModel> Data = pageable.Data.ToList<MutationBPCentralViewModel>();
            int TotalData = pageable.TotalCount;
            //int TotalData = Data.Count();
            return Tuple.Create(Data, TotalData);
        }

        public List<MutationBPCentralViewModel> GetCentralItemBPReport(DateTime? datefrom, DateTime? dateto, int offset)
        {
            DateTime DateFrom = datefrom == null ? new DateTime(1970, 1, 1) : (DateTime)datefrom;
            DateTime DateTo = dateto == null ? DateTime.Now : (DateTime)dateto;

            var pengeluaran = new[] { "PROSES", "SAMPLE", "EXTERNAL" };
            var pemasukan = new[] { "PROSES", "PEMBELIAN" };

            var categories = GetProductCodes(1, int.MaxValue, "{}", "{}");
            var coderequirement = new[] { "BP", "BE" };
            var categories1 = categories.Where(x => coderequirement.Contains(x.CodeRequirement)).Select(x => x.Name).ToArray();

            List<MutationBPCentralViewModelTemp> saldoawalreceipt = new List<MutationBPCentralViewModelTemp>();
            List<MutationBPCentralViewModelTemp> saldoawalexpenditure = new List<MutationBPCentralViewModelTemp>();
            List<MutationBPCentralViewModelTemp> saldoawalreceiptcorrection = new List<MutationBPCentralViewModelTemp>();

            #region Balance
            var lastdate = dbContext.BalanceStocks.OrderByDescending(x => x.CreateDate).Select(x => x.CreateDate).FirstOrDefault();

            var BalanceStock = (from a in (from aa in dbContext.BalanceStocks where aa.CreateDate == lastdate select aa)
                                join b in (from bb in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() where categories1.Contains(bb.ProductName) select bb) on (long)a.EPOItemId equals b.Id
                                join c in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on b.GarmentEPOId equals c.Id
                
                                select new MutationBPCentralViewModelTemp
                                {
                                    ItemCode = b.ProductCode,
                                    BeginQty = (double)a.CloseStock,
                                    ExpenditureQty = 0,
                                    ItemName = b.ProductName,
                                    ReceiptQty = 0,
                                    SupplierType = c.SupplierImport,
                                    UnitQtyName = b.DealUomUnit
                                }
                                );

            var ReceiptBalance = (from a in (from aa in dbContext.GarmentUnitReceiptNotes where aa.CreatedUtc.Date > lastdate && aa.CreatedUtc.Date < DateFrom.Date
                                             && aa.UId == null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType) select aa)
                                  join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                                  join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                  join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
  
                                  select new MutationBPCentralViewModelTemp
                                  {
                                      ItemCode = b.ProductCode,
                                      BeginQty = (double)(b.ReceiptQuantity * b.Conversion),
                                      ExpenditureQty = 0,
                                      ItemName = b.ProductName,
                                      ReceiptQty = 0,
                                      SupplierType = d.SupplierImport,
                                      UnitQtyName = b.SmallUomUnit
                                  }
                                  );



            var ReceiptBalanceLocal = (from a in (from aa in dbContext.GarmentUnitReceiptNotes
                                                   where aa.LastModifiedUtc.Date > lastdate && aa.LastModifiedUtc.Date < DateFrom.Date
                                                    && aa.UId != null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType)
                                                  select aa)
                                       join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                                       join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                       join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id

                                       select new MutationBPCentralViewModelTemp
                                       {
                                           ItemCode = b.ProductCode,
                                           BeginQty = (double)(b.ReceiptQuantity * b.Conversion),
                                           ExpenditureQty = 0,
                                           ItemName = b.ProductName,
                                           ReceiptQty = 0,
                                           SupplierType = d.SupplierImport,
                                           UnitQtyName = b.SmallUomUnit
                                       }
                                       );


            var ExpenditureBalance = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes where aa.CreatedUtc.Date > lastdate &&
                                                 aa.CreatedUtc.Date < DateFrom.Date && aa.UId == null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType) select aa)
                                      join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                                      //join b in dbContext.GarmentUnitExpenditureNoteItems on a.Id equals b.UENId
                                      //join g in dbContext.GarmentUnitDeliveryOrderItems on b.UnitDOItemId equals g.Id
                                      join f in (from ff in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(ff.ProductName) select ff) on b.URNItemId equals f.Id
                                      join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on f.EPOItemId equals c.Id
                                      join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
    
                                      select new MutationBPCentralViewModelTemp
                                      {
                                          ItemCode = b.ProductCode,
                                          BeginQty = (double)(b.Quantity),
                                          ExpenditureQty = 0,
                                          ItemName = b.ProductName,
                                          ReceiptQty = 0,
                                          SupplierType = d.SupplierImport,
                                          UnitQtyName = b.UomUnit
                                      }
                                      );

            var ExpenditureBalanceLocal = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes
                                                      where aa.LastModifiedUtc.Date > lastdate &&
                                                       aa.LastModifiedUtc.Date < DateFrom.Date && aa.UId != null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType)
                                                      select aa)
                                           join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                                           //join g in dbContext.GarmentUnitDeliveryOrderItems on b.UnitDOItemId equals g.Id
                                           join f in (from ff in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(ff.ProductName) select ff) on b.URNItemId equals f.Id
                                           join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                           join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id

                                           select new MutationBPCentralViewModelTemp
                                           {
                                               ItemCode = b.ProductCode,
                                               BeginQty = (double)(b.Quantity),
                                               ExpenditureQty = 0,
                                               ItemName = b.ProductName,
                                               ReceiptQty = 0,
                                               SupplierType = d.SupplierImport,
                                               UnitQtyName = b.UomUnit
                                           }
                                           );

            #endregion
            #region filtered
            var Receipt = (from a in (from aa in dbContext.GarmentUnitReceiptNotes
                                      where aa.CreatedUtc.Date >= DateFrom.Date && aa.CreatedUtc.Date <= DateTo.Date
                                        && aa.UId == null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType)
                                      select aa)
                           join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                           join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                           join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id

                           select new MutationBPCentralViewModelTemp
                           {
                               ItemCode = b.ProductCode,
                               BeginQty = 0,
                               ExpenditureQty = 0,
                               ItemName = b.ProductName,
                               ReceiptQty = (double)(b.ReceiptQuantity * b.Conversion),
                               SupplierType = d.SupplierImport,
                               UnitQtyName = b.SmallUomUnit
                           }
                           );
            var ReceiptLocal = (from a in (from aa in dbContext.GarmentUnitReceiptNotes
                                           where aa.LastModifiedUtc.Date >= DateFrom.Date && aa.LastModifiedUtc.Date <= DateTo.Date
                                            && aa.UId != null && aa.IsDeleted == false && pemasukan.Contains(aa.URNType)
                                           select aa)
                                join b in (from bb in dbContext.GarmentUnitReceiptNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.URNId
                                join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
                                join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id

                                select new MutationBPCentralViewModelTemp
                                {
                                    ItemCode = b.ProductCode,
                                    BeginQty = 0,
                                    ExpenditureQty = 0,
                                    ItemName = b.ProductName,
                                    ReceiptQty = (double)(b.ReceiptQuantity * b.Conversion),
                                    SupplierType = d.SupplierImport,
                                    UnitQtyName = b.SmallUomUnit
                                }
                                );

            var Expenditure = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes
                                          where aa.CreatedUtc.Date >= DateFrom.Date &&
                                            aa.CreatedUtc.Date <= DateTo.Date && aa.UId == null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType)
                                          select aa)
                               join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                               //join g in dbContext.GarmentUnitDeliveryOrderItems on b.UnitDOItemId equals g.Id
                               join f in (from ff in dbContext.GarmentUnitReceiptNoteItems select ff) on b.URNItemId equals f.Id
                               join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on f.EPOItemId equals c.Id
                               join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                               //where b.ProductCode == "NLB001"
                               select new MutationBPCentralViewModelTemp
                               {
                                   ItemCode = b.ProductCode,
                                   BeginQty = 0,
                                   ExpenditureQty = (double)(b.Quantity),
                                   ItemName = b.ProductName,
                                   ReceiptQty = 0,
                                   SupplierType = d.SupplierImport,
                                   UnitQtyName = b.UomUnit
                               }
                               );

            var ExpenditureLocal = (from a in (from aa in dbContext.GarmentUnitExpenditureNotes
                                               where aa.LastModifiedUtc.Date >= DateFrom.Date &&
                                                 aa.LastModifiedUtc.Date <= DateTo.Date && aa.UId != null && aa.IsDeleted == false && pengeluaran.Contains(aa.ExpenditureType)
                                               select aa)
                                    join b in (from bb in dbContext.GarmentUnitExpenditureNoteItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.UENId
                                    //join g in dbContext.GarmentUnitDeliveryOrderItems on b.UnitDOItemId equals g.Id
                                    join f in (from ff in dbContext.GarmentUnitReceiptNoteItems select ff) on b.URNItemId equals f.Id
                                    join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on f.EPOItemId equals c.Id
                                    join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                                    //where b.ProductCode == "NLB001"
                                    select new MutationBPCentralViewModelTemp
                                    {
                                        ItemCode = b.ProductCode,
                                        BeginQty = 0,
                                        ExpenditureQty = (double)(b.Quantity),
                                        ItemName = b.ProductName,
                                        ReceiptQty = 0,
                                        SupplierType = d.SupplierImport,
                                        UnitQtyName = b.UomUnit
                                    }
                                    );

            //var ReceiptCorrection = (from a in (from aa in dbContext.GarmentReceiptCorrections where aa.CreatedUtc.Date >= DateFrom.Date
            //                                     && aa.CreatedUtc.Date <= DateTo.Date && aa.UId == null && aa.IsDeleted == false
            //                                    select aa)
            //                         join b in (from bb in dbContext.GarmentReceiptCorrectionItems where categories1.Contains(bb.ProductName) && bb.IsDeleted == false select bb) on a.Id equals b.CorrectionId
            //                         join c in dbContext.GarmentExternalPurchaseOrderItems.IgnoreQueryFilters() on b.EPOItemId equals c.Id
            //                         join d in dbContext.GarmentExternalPurchaseOrders.IgnoreQueryFilters() on c.GarmentEPOId equals d.Id
                             
            //                         select new MutationBPCentralViewModelTemp
            //                         {
            //                             ItemCode = b.ProductCode,
            //                             BeginQty = 0,
            //                             ExpenditureQty = b.SmallQuantity < 0 ? b.SmallQuantity * -1 : 0,
            //                             ItemName = b.ProductName,
            //                             ReceiptQty = b.SmallQuantity > 0 ? b.SmallQuantity : 0,
            //                             SupplierType = d.SupplierImport,
            //                             UnitQtyName = b.UomUnit
            //                         }
            //                         );

            #endregion

            var data = ReceiptBalance.Union(BalanceStock).Union(ReceiptBalanceLocal).Union(ExpenditureBalance).Union(ExpenditureBalanceLocal).
                Union(Receipt).Union(ReceiptLocal).Union(Expenditure).Union(ExpenditureLocal).
                //Union(ReceiptCorrection).
                AsEnumerable();

            var mutationgroup = data.GroupBy(x => new { x.ItemCode, x.ItemName, x.SupplierType, x.UnitQtyName }, (key, group) => new MutationBPCentralViewModelTemp
            {
                //AdjustmentQty = Math.Round(group.Sum(x => x.AdjustmentQty), 2),
                BeginQty = Math.Round(group.Sum(x => x.BeginQty), 2),
                ExpenditureQty = Math.Round(group.Sum(x => x.ExpenditureQty), 2),
                ItemCode = key.ItemCode,
                ItemName = key.ItemName,
                //LastQty = Math.Round(group.Sum(x => x.BeginQty) + group.Sum(x => x.ReceiptQty) - group.Sum(x => x.ExpenditureQty) + group.Sum(x => x.AdjustmentQty) + group.Sum(x => x.OpnameQty), 2),
                ReceiptQty = Math.Round(group.Sum(x => x.ReceiptQty), 2),
                SupplierType = key.SupplierType,
                UnitQtyName = key.UnitQtyName,
                //OpnameQty = Math.Round(group.Sum(x => x.OpnameQty), 2),
               // Diff = Math.Round(group.Sum(x => x.Diff), 2)
            });

            List<MutationBPCentralViewModel> mutations = new List<MutationBPCentralViewModel>();

            foreach(var item in mutationgroup.Where(x=>(x.ItemCode != "APL001") && (x.ItemCode != "EMB001") && (x.ItemCode != "GMT001") && (x.ItemCode != "PRN001") && (x.ItemCode != "SMP001") && (x.ItemCode != "WSH001") ).ToList())
            {
                MutationBPCentralViewModel mutation = new MutationBPCentralViewModel()
                {
                    AdjustmentQty = 0,
                    BeginQty = item.BeginQty,
                    ExpenditureQty = item.ExpenditureQty,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    LastQty = (item.BeginQty + item.ReceiptQty) - (item.ExpenditureQty + 0 + 0),
                    ReceiptQty = item.ReceiptQty,
                    SupplierType = item.SupplierType == true ? "IMPORT" : "LOKAL",
                    UnitQtyName = item.UnitQtyName,
                    OpnameQty = 0,
                    Diff = 0
                };

                mutations.Add(mutation);
            }


            mutations = mutations.OrderBy(x => x.ItemCode).ToList();

            mutations = mutations.Where(x => x.ItemCode != "APL001" || x.ItemCode != "EMB001" || x.ItemCode != "GMT001" || x.ItemCode != "PRN001" || x.ItemCode != "SMP001" || x.ItemCode != "WSH001").ToList();

            var mm = new MutationBPCentralViewModel();

            mm.AdjustmentQty = Math.Round(mutations.Sum(x => x.AdjustmentQty), 2);
            mm.BeginQty = Math.Round(mutations.Sum(x => x.BeginQty), 2);
            mm.ExpenditureQty = Math.Round(mutations.Sum(x => x.ExpenditureQty), 2);
            mm.ItemCode = "";
            mm.ItemName = "";
            mm.LastQty = Math.Round(mutations.Sum(x => x.LastQty), 2);
            mm.ReceiptQty = Math.Round(mutations.Sum(x => x.ReceiptQty), 2);
            mm.SupplierType = "";
            mm.UnitQtyName = "";
            mm.OpnameQty = 0;
            mm.Diff = 0;

            mutations.Add(new MutationBPCentralViewModel
            {
                AdjustmentQty = mm.AdjustmentQty,
                BeginQty = mm.BeginQty,
                ExpenditureQty = mm.ExpenditureQty,
                ItemCode = mm.ItemCode,
                ItemName = mm.ItemName,
                LastQty = mm.LastQty,
                ReceiptQty = mm.ReceiptQty,
                SupplierType = mm.SupplierType,
                UnitQtyName = mm.UnitQtyName,
                OpnameQty = mm.OpnameQty,
                Diff = mm.Diff
            });

            return mutations;



        }

        public MemoryStream GenerateExcelBPCentral(DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            var Query = GetCentralItemBPReport(dateFrom, dateTo, offset);
            //Query = Query.OrderBy(b => b.ItemCode).ToList();
            DataTable result = new DataTable();
            result.Columns.Add(new DataColumn() { ColumnName = "Kode Barang", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Barang", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tipe", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Satuan", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Saldo Awal", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Pemasukan", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Pengeluaran", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Penyesuaian", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Saldo Akhir", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Stock Opname", DataType = typeof(Double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Selisih", DataType = typeof(Double) });
            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", "", "", "", "", "", "", "", ""); // to allow column name to be generated properly for empty data as template
            else
                foreach (var item in Query)
                {
                    result.Rows.Add((item.ItemCode), item.ItemName, item.SupplierType, item.UnitQtyName, item.BeginQty, item.ReceiptQty, item.ExpenditureQty, item.AdjustmentQty, item.LastQty, item.OpnameQty, item.Diff);
                }

            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Territory") }, true);

        }

        #endregion


    }

}
