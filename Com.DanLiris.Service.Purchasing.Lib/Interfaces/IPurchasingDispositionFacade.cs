using Com.DanLiris.Service.Purchasing.Lib.Models.PurchasingDispositionModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.PurchasingDispositionViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Interfaces
{
    public interface IPurchasingDispositionFacade
    {
        Tuple<List<PurchasingDisposition>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        Tuple<List<PurchasingDisposition>, int, Dictionary<string, string>> ReadOptimized(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        PurchasingDisposition ReadModelById(int id);
        Task<int> Create(PurchasingDisposition m, string user, int clientTimeZoneOffset = 7);
        int Delete(int id, string user);
        Task<int> Update(int id, PurchasingDisposition purchasingDisposition, string user);
        Task<int> SetIsPaidTrue(string dispositionNo, string user);
        List<PurchasingDisposition> ReadDisposition(string Keyword = null, string Filter = "{}", string epoId = "");
        IQueryable<PurchasingDisposition> ReadByDisposition(string Keyword = null, string Filter = "{}");
        Task<int> UpdatePosition(PurchasingDispositionUpdatePositionPostedViewModel data, string user);
        List<PurchasingDispositionViewModel> GetTotalPaidPrice(List<PurchasingDispositionViewModel> data);

    }
}
