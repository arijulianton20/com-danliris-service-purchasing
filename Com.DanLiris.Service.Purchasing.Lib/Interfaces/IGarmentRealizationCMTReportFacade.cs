

using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentReports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentReports.GarmentRealizationCMTReportFacade;

namespace Com.DanLiris.Service.Purchasing.Lib.Interfaces
{
    public interface IGarmentRealizationCMTReportFacade
    {
        Tuple<List<GarmentRealizationCMTReportViewModel>, int> GetReport( DateTime? dateFrom, DateTime? dateTo, long unit, int page, int size, string Order, int offset);
        MemoryStream GenerateExcel(DateTime? dateFrom, DateTime? dateTo, long unit, int offset, string unitname);
    }
}
