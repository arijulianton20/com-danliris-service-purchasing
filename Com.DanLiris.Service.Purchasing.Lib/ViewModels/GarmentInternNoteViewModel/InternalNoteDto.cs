using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentInternNoteModel;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentInvoiceModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentInternNoteViewModel
{
    public class InternalNoteDto
    {

        public InternalNoteDto(GarmentInternNote internalNote, List<GarmentInvoice> internalNoteInvoices)
        {
            Id = (int)internalNote.Id;
            DocumentNo = internalNote.INNo;
            Date = internalNote.INDate;
            DueDate = internalNote.Items.FirstOrDefault().Details.FirstOrDefault().PaymentDueDate;
            Supplier = new SupplierDto(internalNote.SupplierId, internalNote.SupplierName, internalNote.SupplierCode);
            DPP = internalNoteInvoices.Sum(internalNoteInvoice => internalNoteInvoice.TotalAmount);

            TotalAmount = internalNoteInvoices.Sum(element =>
            {
                var total = element.TotalAmount;

                if (element.UseVat && element.IsPayVat)
                    total += element.TotalAmount * 0.1;

                if (element.UseIncomeTax && element.IsPayTax)
                    total -= element.TotalAmount * (element.IncomeTaxRate / 100);

                return total;
            });

            VATAmount = internalNoteInvoices.Sum(element =>
            {
                var total = 0.0;

                if (element.UseVat && element.IsPayVat)
                    total += element.TotalAmount * 0.1;

                return total;
            });

            IncomeTaxAmount = internalNoteInvoices.Sum(element =>
            {
                var total = 0.0;

                if (element.UseIncomeTax && element.IsPayTax)
                    total += element.TotalAmount * (element.IncomeTaxRate / 100);

                return total;
            });

            Currency = new CurrencyDto(internalNote.CurrencyId, internalNote.CurrencyCode, internalNote.CurrencyRate);
            Items = internalNoteInvoices.Select(internalNoteInvoice => new InternalNoteInvoiceDto(internalNoteInvoice)).ToList();
        }
        public InternalNoteDto(GarmentInternNote internalNote, List<GarmentInvoiceInternNoteViewModel> internalNoteInvoices)
        {
            Id = (int)internalNote.Id;
            DocumentNo = internalNote.INNo;
            Date = internalNote.INDate;
            DueDate = internalNote.Items.FirstOrDefault().Details.FirstOrDefault().PaymentDueDate;
            Supplier = new SupplierDto(internalNote.SupplierId, internalNote.SupplierName, internalNote.SupplierCode);
            DPP = internalNoteInvoices.Sum(internalNoteInvoice => internalNoteInvoice.GarmentInvoices.TotalAmount);

            TotalAmount = internalNoteInvoices.Sum(element =>
            {
                var total = element.GarmentInvoices.TotalAmount;

                if (element.GarmentInvoices.UseVat && element.GarmentInvoices.IsPayVat)
                    total += element.GarmentInvoices.TotalAmount * 0.1;

                if (element.GarmentInvoices.UseIncomeTax && element.GarmentInvoices.IsPayTax)
                    total -= element.GarmentInvoices.TotalAmount * (element.GarmentInvoices.IncomeTaxRate / 100);

                return total;
            });

            VATAmount = internalNoteInvoices.Sum(element =>
            {
                var total = 0.0;

                if (element.GarmentInvoices.UseVat && element.GarmentInvoices.IsPayVat)
                    total += element.GarmentInvoices.TotalAmount * 0.1;

                return total;
            });

            IncomeTaxAmount = internalNoteInvoices.Sum(element =>
            {
                var total = 0.0;

                if (element.GarmentInvoices.UseIncomeTax && element.GarmentInvoices.IsPayTax)
                    total += element.GarmentInvoices.TotalAmount * (element.GarmentInvoices.IncomeTaxRate / 100);

                return total;
            });

            Currency = new CurrencyDto(internalNote.CurrencyId, internalNote.CurrencyCode, internalNote.CurrencyRate);
            Items = internalNoteInvoices.Select(internalNoteInvoice => new InternalNoteInvoiceDto(internalNoteInvoice)).ToList();
        }

        public int Id { get; set; }
        public string DocumentNo { get; set; }
        public DateTimeOffset Date { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public SupplierDto Supplier { get; set; }
        public double VATAmount { get; set; }
        public double IncomeTaxAmount { get; set; }
        public double DPP { get; set; }
        public double TotalAmount { get; set; }
        public CurrencyDto Currency { get; set; }
        public List<InternalNoteInvoiceDto> Items { get; set; }
    }
}