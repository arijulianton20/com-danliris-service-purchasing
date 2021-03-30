using System;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.DebtAndDispositionSummary
{
    public class DebtAndDispositionSummaryDto
    {
        public string CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public double CurrencyRate { get; set; }
        public string CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public string UnitId { get; set; }
        public string UnitCode { get; set; }
        public string UnitName { get; set; }
        public string DivisionId { get; set; }
        public string DivisionCode { get; set; }
        public string DivisionName { get; set; }
        public bool IsImport { get; set; }
        public bool IsPaid { get; set; }
        public double DebtPrice { get; set; }
        public double DebtQuantity { get; set; }
        public double DispositionPrice { get; set; }
        public double DispositionQuantity { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public double Total { get;  set; }
        public double DispositionTotal { get; set; }
        public double DebtTotal { get; set; }
        public string IncomeTaxBy { get; internal set; }
        public bool UseIncomeTax { get; internal set; }
        public string IncomeTaxRate { get; internal set; }
        public bool UseVat { get; internal set; }
        public int CategoryLayoutIndex { get; internal set; }
        public string AccountingUnitName { get; internal set; }
        public string AccountingUnitId { get; internal set; }

    }
}