using Com.DanLiris.Service.Purchasing.Lib.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentPurchasingExpedition
{
    public interface IGarmentPurchasingExpeditionService
    {
        List<GarmentInternalNoteDto> GetGarmentInternalNotes(string keyword, GarmentInternalNoteFilterDto filter);
        int UpdateInternNotePosition(UpdatePositionFormDto form);
        List<GarmentInternalNoteDetailsDto> GetGarmentInternNotesDetails(string keyword, GarmentInternalNoteFilterDto filter);
        void UpdateInternNotesIsPphPaid(List<GarmentInternNoteUpdateIsPphPaidDto> listModel);
        List<GarmentDispositionNoteDto> GetGarmentDispositionNotes(string keyword, PurchasingGarmentExpeditionPosition position);
        int UpdateDispositionNotePosition(UpdatePositionFormDto form);
    }
}
