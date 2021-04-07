using Com.DanLiris.Service.Purchasing.Lib.Helpers;
using Com.DanLiris.Service.Purchasing.Lib.Helpers.ReadResponse;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentUnitExpenditureNoteModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentUnitExpenditureNoteViewModel;
using Microsoft.AspNetCore.JsonPatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Interfaces
{
    public interface IGarmentUnitExpenditureNoteFacade
    {
        ReadResponse<object> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        ReadResponse<object> ReadLoader(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}",ConditionType conditionType= ConditionType.ENUM_INT);
        GarmentUnitExpenditureNoteViewModel ReadById(int id);
		ExpenditureROViewModel GetROAsalById(int id);
		Task<int> Create(GarmentUnitExpenditureNote garmentUnitExpenditureNote);
        Task<int> Update(int id, GarmentUnitExpenditureNote garmentUnitExpenditureNote);
        Task<int> Delete(int id);
        Task<int> PatchOne(long id, JsonPatchDocument<GarmentUnitExpenditureNote> jsonPatch);
        ReadResponse<object> ReadForGPreparing(int Page = 1, int Size = 10, string Order = "{}", string Keyword = null, string Filter = "{}");
        Task<int> UpdateIsPreparing(int id, GarmentUnitExpenditureNote garmentUnitExpenditureNote);
        Task<int> UpdateReturQuantity(int id, double quantity, double quantityBefore);
        GarmentUnitExpenditureNote ReadByUENId(int id);
        Tuple<List<MonitoringOutViewModel>, int> GetReportOut(DateTime? dateFrom, DateTime? dateTo, string type, int page, int size, string Order, int offset);
        MemoryStream GenerateExcelMonOut(DateTime? dateFrom, DateTime? dateTo, string category, int offset);
    }
}
