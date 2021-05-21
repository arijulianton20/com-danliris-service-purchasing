using Com.DanLiris.Service.Purchasing.Lib.ViewModels.NewIntegrationViewModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.NewIntegrationViewModel.GarmentExpenditureGood;
using Com.DanLiris.Service.Purchasing.WebApi.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Com.DanLiris.Service.Purchasing.Test.DataUtils.NewIntegrationDataUtils
{
    public class GarmentExpenditureGoodDataUtil
    {
        public GarmentExpenditureGoodViewModel GetNewData()
        {
            long nowTicks = DateTimeOffset.Now.Ticks;

            var data = new GarmentExpenditureGoodViewModel
            {
                Id = $"Id{nowTicks}",
                Active = false,
                CreatedBy = $"CreatedBy{nowTicks}",
                CreatedUtc = new DateTime(1970, 1, 1),
                LastModifiedBy = $"LastModifiedBy{nowTicks}",
                LastModifiedUtc = new DateTime(1970, 1, 1),
                IsDeleted = false,
                RONo = $"RONo{nowTicks}",
                Invoice = $"Invoice{nowTicks}",
                ExpenditureGoodNo = $"ExpenditureGoodNo{nowTicks}",
                Article = $"Article{nowTicks}",
                TotalQuantity = 120,
            };
            return data;
        }

        public Dictionary<string, object> GetResultFormatterOk()
        {
            return GetResultFormatterOk(GetNewData());
        }

        public Dictionary<string, object> GetResultFormatterOk(GarmentExpenditureGoodViewModel garmentExpenditureGoodViewModel)
        {
            var data = garmentExpenditureGoodViewModel;

            Dictionary<string, object> result =
                new ResultFormatter("1.0", General.OK_STATUS_CODE, General.OK_MESSAGE)
                .Ok(data);

            return result;
        }

        public string GetResultFormatterOkString()
        {
            return GetResultFormatterOkString(GetNewData());
        }

        public string GetResultFormatterOkString(GarmentExpenditureGoodViewModel garmentExpenditureGoodViewModel)
        {
            var result = GetResultFormatterOk(garmentExpenditureGoodViewModel);

            return JsonConvert.SerializeObject(result);
        }
        public Dictionary<string, object> GetMultipleResultFormatterOk()
        {
            var data = new List<GarmentExpenditureGoodViewModel> { GetNewData() };

            Dictionary<string, object> result =
                new ResultFormatter("1.0", General.OK_STATUS_CODE, General.OK_MESSAGE)
                .Ok(data);

            return result;
        }

        public string GetMultipleResultFormatterOkString()
        {
            var result = GetMultipleResultFormatterOk();

            return JsonConvert.SerializeObject(result);
        }
    }
}
