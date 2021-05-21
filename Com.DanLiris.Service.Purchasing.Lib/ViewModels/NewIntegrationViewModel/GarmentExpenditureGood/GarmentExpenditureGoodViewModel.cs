using Com.DanLiris.Service.Purchasing.Lib.Utilities;
using System;
using System.Collections.Generic;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.NewIntegrationViewModel.GarmentExpenditureGood
{
    public class GarmentExpenditureGoodViewModel //: BaseViewModel
    {
        public string Id { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string CreatedBy { get; set; }
        public DateTime LastModifiedUtc { get; set; }
        public string LastModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public string RONo { get; set; }
        public string Invoice { get; set; }
        public string ExpenditureGoodNo { get; set; }
        public string Article { get; set; }
        public double TotalQuantity { get; set; }     
    }
}
