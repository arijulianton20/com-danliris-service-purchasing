using Com.DanLiris.Service.Purchasing.Lib.Enums;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentExternalPurchaseOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentDispositionPurchase;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentDispositionPurchaseFacades
{
    public interface IGarmentDispositionPurchaseFacade
    {
        Task<int> Post(FormDto model);
        Task<int> Delete(int id);
        Task<int> Update(FormEditDto model);
        Task<FormDto> GetFormById(int id, bool isVerifiedAmountCalculated = false);
        Task<DispositionPurchaseIndexDto> GetAll(string keyword, int page, int size,string filter, string order);
        Task<List<FormDto>> ReadByDispositionNo(string dispositionNo, int page, int size);
        GarmentExternalPurchaseOrderViewModel ReadByEPOWithDisposition(int EPOid, int supplierId, int currencyId);
        Task<int> SetIsPaidTrue(string dispositionNo, string user);
        Tuple<List<FormDto>, int, Dictionary<string, string>> Read(PurchasingGarmentExpeditionPosition position,int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}",int supplierId=0);

    }
}
