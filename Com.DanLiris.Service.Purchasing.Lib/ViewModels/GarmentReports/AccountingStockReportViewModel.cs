using System;
using System.Collections.Generic;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentReports
{
    public class AccountingStockReportViewModel
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string RO { get; set; }
        public string Buyer { get; set; }
        public string PlanPo { get; set; }
        public string NoArticle { get; set; }
        public decimal BeginningBalanceQty { get; set; }
        public string BeginningBalanceUom { get; set; }
        public double BeginningBalancePrice { get; set; }
        public decimal ReceiptCorrectionQty { get; set; }
        public decimal ReceiptPurchaseQty { get; set; }
        public decimal ReceiptProcessQty { get; set; }
        public decimal ReceiptKon2AQty { get; set; }
        public decimal ReceiptKon2BQty { get; set; }
        public decimal ReceiptKon2CQty { get; set; }
        public decimal ReceiptKon1AQty { get; set; }
        public decimal ReceiptKon1BQty { get; set; }
        public decimal ReceiptCorrectionPrice { get; set; }
        public decimal ReceiptPurchasePrice { get; set; }
        public decimal ReceiptProcessPrice { get; set; }
        public decimal ReceiptKon2APrice { get; set; }
        public decimal ReceiptKon2BPrice { get; set; }
        public decimal ReceiptKon2CPrice { get; set; }
        public decimal ReceiptKon1APrice { get; set; }
        public decimal ReceiptKon1BPrice { get; set; }
        public double ExpendReturQty { get; set; }
        public double ExpendRestQty { get; set; }
        public double ExpendProcessQty { get; set; }
        public double ExpendSampleQty { get; set; }
        public double ExpendKon2AQty { get; set; }
        public double ExpendKon2BQty { get; set; }
        public double ExpendKon2CQty { get; set; }
        public double ExpendKon1AQty { get; set; }
        public double ExpendKon1BQty { get; set; }
        public double ExpendReturPrice { get; set; }
        public double ExpendRestPrice { get; set; }
        public double ExpendProcessPrice { get; set; }
        public double ExpendSamplePrice { get; set; }
        public double ExpendKon2APrice { get; set; }
        public double ExpendKon2BPrice { get; set; }
        public double ExpendKon2CPrice { get; set; }
        public double ExpendKon1APrice { get; set; }
        public double ExpendKon1BPrice { get; set; }
        public decimal EndingBalanceQty { get; set; }
        public double EndingBalancePrice { get; set; }
    }

    public class AccountingStockTempViewModel
    {
        public string ProductCode { get; set; }
        public string RO { get; set; }
        public string Buyer { get; set; }
        public string PlanPo { get; set; }
        public string NoArticle { get; set; }
        public decimal BeginningBalanceQty { get; set; }
        public string BeginningBalanceUom { get; set; }
        public double BeginningBalancePrice { get; set; }
        public decimal ReceiptCorrectionQty { get; set; }
        public decimal ReceiptPurchaseQty { get; set; }
        public decimal ReceiptProcessQty { get; set; }
        public decimal ReceiptKon2AQty { get; set; }
        public decimal ReceiptKon2BQty { get; set; }
        public decimal ReceiptKon2CQty { get; set; }
        public decimal ReceiptKon1AQty { get; set; }
        public decimal ReceiptKon1BQty { get; set; }
        public decimal ReceiptCorrectionPrice { get; set; }
        public decimal ReceiptPurchasePrice { get; set; }
        public decimal ReceiptProcessPrice { get; set; }
        public decimal ReceiptKon2APrice { get; set; }
        public decimal ReceiptKon2BPrice { get; set; }
        public decimal ReceiptKon2CPrice { get; set; }
        public decimal ReceiptKon1APrice { get; set; }
        public decimal ReceiptKon1BPrice { get; set; }
        public double ExpendReturQty { get; set; }
        public double ExpendRestQty { get; set; }
        public double ExpendProcessQty { get; set; }
        public double ExpendSampleQty { get; set; }
        public double ExpendKon2AQty { get; set; }
        public double ExpendKon2BQty { get; set; }
        public double ExpendKon2CQty { get; set; }
        public double ExpendKon1AQty { get; set; }
        public double ExpendKon1BQty { get; set; }
        public double ExpendReturPrice { get; set; }
        public double ExpendRestPrice { get; set; }
        public double ExpendProcessPrice { get; set; }
        public double ExpendSamplePrice { get; set; }
        public double ExpendKon2APrice { get; set; }
        public double ExpendKon2BPrice { get; set; }
        public double ExpendKon2CPrice { get; set; }
        public double ExpendKon1APrice { get; set; }
        public double ExpendKon1BPrice { get; set; }
        public decimal EndingBalanceQty { get; set; }
        public double EndingBalancePrice { get; set; }
    }
}
