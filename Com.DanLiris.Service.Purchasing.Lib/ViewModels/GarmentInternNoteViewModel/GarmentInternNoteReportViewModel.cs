using System;
using System.Collections.Generic;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentInternNoteViewModel
{
    public class GarmentInternNoteReportViewModel
    {
        public string inNo { get; set; }
        public DateTimeOffset iNDate { get; set; }
        public string supplierCode { get; set; }
        public string currencyCode { get; set; }
        public string supplierName { get; set; }
        public string invoiceNo { get; set; }
        public DateTimeOffset invoiceDate { get; set; }
        public string NPN { get; set; }
        public string VatNo { get; set; }
        public string ProductName { get; set; }
        public double priceTotal { get; set; }
        public string doNo { get; set; }
        public DateTimeOffset doDate { get; set; }
        public string billNo { get; set; }
        public string paymentBill { get; set; }
        public double? doCurrencyRate { get; set; }
        public string paymentType { get; set; }
        public string createdBy { get; set; }
    }
}
