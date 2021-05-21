using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentReports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Interfaces
{
    public interface IAccountingStockReportFacade
    {
        Task<Tuple<List<AccountingStockReportViewModel>, int>> GetStockReportAsync(int offset, string unitcode, string tipebarang, int page, int size, string Order, DateTime? dateFrom, DateTime? dateTo);
        Task<MemoryStream> GenerateExcelAStockReportAsync(string ctg, string categoryname, string unitcode, string unitname, DateTime? datefrom, DateTime? dateto, int offset);
    }
}
