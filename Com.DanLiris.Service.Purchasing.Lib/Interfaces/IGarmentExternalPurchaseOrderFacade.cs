using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentExternalPurchaseOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentExternalPurchaseOrderViewModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.NewIntegrationViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Interfaces
{
    public interface IGarmentExternalPurchaseOrderFacade
    {
        Tuple<List<GarmentExternalPurchaseOrder>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        GarmentExternalPurchaseOrder ReadById(int id);
        Task<int> Create(GarmentExternalPurchaseOrder m, string user, int clientTimeZoneOffset = 7);
        Task<int> Update(int id, GarmentExternalPurchaseOrder m, string user, int clientTimeZoneOffset = 7);
        int Delete(int id, string user);
        int EPOPost(List<GarmentExternalPurchaseOrder> ListEPO, string user);
        int EPOUnpost(int id, string user);
        int EPOClose(int id, string user);
        int EPOCancel(int id, string user);
        SupplierViewModel GetSupplier(long supplierId);
        GarmentProductViewModel GetProduct(long productId);
        int EPOApprove(List<GarmentExternalPurchaseOrder> ListEPO, string user);
        List<GarmentExternalPurchaseOrder> ReadBySupplier(string Keyword = null, string Filter = "{}");

        MemoryStream GenerateExcelEPODODuration(string unit, string supplier, string duration, DateTime? dateFrom, DateTime? dateTo, int offset);
        Tuple<List<GarmentExternalPurchaseOrderDeliveryOrderDurationReportViewModel>, int> GetEPODODurationReport(string unit, string supplier, string duration, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset);
		MemoryStream GenerateExcelEPOOverBudget(string epono, string unit, string supplier, string status, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset);
		Tuple<List<GarmentExternalPurchaseOrderOverBudgetMonitoringViewModel>, int> GetEPOOverBudgetReport(string epono, string unit, string supplier, string status, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset);
        List<GarmentExternalPurchaseOrderItem> ReadItemByRO(string Keyword = null, string Filter = "{}");
        List<GarmentExternalPurchaseOrder> ReadItemByEPONo(string EPONo = null, string Filter = "{}");
        Tuple<List<GarmentExternalPurchaseOrder>, int, Dictionary<string, string>> ReadItemByEPONoSimply(string EPONo = null, string Filter = "{}",int supplierId=0, int currencyId=0, int Page=1, int Size=10);


    }
}
