using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentInternNoteModel;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentInvoiceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentInternNoteViewModel
{
    public class GarmentInternalNoteDto
    {
        public GarmentInternalNoteDto(GarmentInternNote internalNote, List<GarmentInvoice> internalNoteInvoices)
        {
            InternalNote = new InternalNoteDto(internalNote, internalNoteInvoices);
        }

        public GarmentInternalNoteDto(GarmentInternNote internalNote, List<GarmentInvoiceInternNoteViewModel> internalNoteInvoices)
        {
            InternalNote = new InternalNoteDto(internalNote, internalNoteInvoices);
        }

        public InternalNoteDto InternalNote { get; set; }

        
    }
}
