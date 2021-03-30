using Com.DanLiris.Service.Purchasing.Lib.Helpers.ReadResponse;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentStockOpnameModel;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentStockOpnameFacades
{
    public interface IGarmentStockOpnameFacade
    {
        ReadResponse<object> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        GarmentStockOpname ReadById(int id);
        Stream Download(DateTimeOffset date, string unit, string storage, string storageName);
        Task<GarmentStockOpname> Upload(Stream stream);
        GarmentStockOpname GetLastDataByUnitStorage(string unit, string storage);
    }
}
