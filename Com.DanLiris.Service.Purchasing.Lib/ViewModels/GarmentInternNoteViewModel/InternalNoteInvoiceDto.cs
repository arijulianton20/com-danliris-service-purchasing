using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentInvoiceModel;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentInternNoteViewModel
{
    public class InternalNoteInvoiceDto
    {
        public InternalNoteInvoiceDto(GarmentInvoice internalNoteInvoice)
        {
            Invoice = new InvoiceDto(internalNoteInvoice);
        }
        public InternalNoteInvoiceDto(GarmentInvoiceInternNoteViewModel internalNoteInvoice)
        {
            Invoice = new InvoiceDto(internalNoteInvoice);
        }
        public InvoiceDto Invoice { get; set; }
    }
}