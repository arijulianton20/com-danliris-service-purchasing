using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentInvoiceModel;
using System;
using System.Linq;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentInternNoteViewModel
{
    public class InvoiceDto
    {
        public InvoiceDto(GarmentInvoiceInternNoteViewModel internalNoteInvoice)
        {
            DocumentNo = internalNoteInvoice.GarmentInvoices.InvoiceNo;
            Date = internalNoteInvoice.GarmentInvoices.InvoiceDate;
            ProductNames = string.Join("\n", internalNoteInvoice.GarmentInvoices.Items.SelectMany(item => item.Details).Select(detail => detail.ProductName));
            Category = internalNoteInvoice.Category;
            PaymentMethod = internalNoteInvoice.PaymentMethod;
            Id = (int)internalNoteInvoice.GarmentInvoices.Id;
            DeliveryOrdersNo = internalNoteInvoice.DeliveryOrdersNo;
            BillsNo = internalNoteInvoice.BillsNo;
            PaymentBills = internalNoteInvoice.PaymentBills;

            Amount = internalNoteInvoice.GarmentInvoices.TotalAmount;

            if (internalNoteInvoice.GarmentInvoices.UseVat && internalNoteInvoice.GarmentInvoices.IsPayVat)
                Amount += internalNoteInvoice.GarmentInvoices.TotalAmount * 0.1;

            if (internalNoteInvoice.GarmentInvoices.UseIncomeTax && internalNoteInvoice.GarmentInvoices.IsPayTax)
                Amount -= internalNoteInvoice.GarmentInvoices.TotalAmount * (internalNoteInvoice.GarmentInvoices.IncomeTaxRate / 100);
        }

        public InvoiceDto(GarmentInvoice internalNoteInvoice)
        {
            DocumentNo = internalNoteInvoice.InvoiceNo;
            Date = internalNoteInvoice.InvoiceDate;
            ProductNames = string.Join("\n", internalNoteInvoice.Items.SelectMany(item => item.Details).Select(detail => detail.ProductName));
            Category = new CategoryDto();
            Amount = internalNoteInvoice.TotalAmount;
            Id = (int)internalNoteInvoice.Id;
        }

        public string DocumentNo { get; set; }
        public DateTimeOffset Date { get; set; }
        public string ProductNames { get; set; }
        public CategoryDto Category { get; set; }
        public string PaymentMethod { get; set; }
        public double Amount { get; set; }
        public int Id { get; set; }
        public string DeliveryOrdersNo { get; set; }
        public string BillsNo { get; private set; }
        public string PaymentBills { get; private set; }
    }
}