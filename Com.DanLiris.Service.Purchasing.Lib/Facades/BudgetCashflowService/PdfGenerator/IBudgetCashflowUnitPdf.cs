using System;
using System.IO;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.BudgetCashflowService.PdfGenerator
{
    public interface IBudgetCashflowUnitPdf
    {
        MemoryStream Generate(int unitId, DateTimeOffset dueDate);
    }
}
