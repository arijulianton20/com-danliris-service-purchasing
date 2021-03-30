using Com.DanLiris.Service.Purchasing.Lib.Helpers;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Services.GarmentDebtBalance
{
    public class GarmentDebtBalanceService : IGarmentDebtBalanceService
    {
        private readonly IServiceProvider _serviceProvider;

        public GarmentDebtBalanceService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<int> CreateFromCustoms(CustomsFormDto form)
        {
            var uri = "garment-debt-balances/customs";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PostAsync($"{APIEndpoint.Finance}{uri}", new StringContent(JsonConvert.SerializeObject(form).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }

        public async Task<int> EmptyBankExpenditureNote(int deliveryOrderId)
        {
            var uri = "garment-debt-balances/remove-bank-expenditure-note/";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PutAsync($"{APIEndpoint.Finance}{uri}{deliveryOrderId}", new StringContent(JsonConvert.SerializeObject(new { }).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }

        public async Task<int> EmptyInternalNote(int deliveryOrderId)
        {
            var uri = "garment-debt-balances/remove-internal-note/";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PutAsync($"{APIEndpoint.Finance}{uri}{deliveryOrderId}", new StringContent(JsonConvert.SerializeObject(new { }).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }

        public async Task<int> EmptyInvoice(int deliveryOrderId)
        {
            var uri = "garment-debt-balances/remove-invoice/";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PutAsync($"{APIEndpoint.Finance}{uri}{deliveryOrderId}", new StringContent(JsonConvert.SerializeObject(new { }).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }

        public async Task<int> RemoveCustoms(int deliveryOrderId)
        {
            var uri = "garment-debt-balances/remove-customs/";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PutAsync($"{APIEndpoint.Finance}{uri}{deliveryOrderId}", new StringContent(JsonConvert.SerializeObject(new { }).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }

        public async Task<int> UpdateFromBankExpenditureNote(int deliveryOrderId, BankExpenditureNoteFormDto form)
        {
            var uri = "garment-debt-balances/bank-expenditure-note/";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PutAsync($"{APIEndpoint.Finance}{uri}{deliveryOrderId}", new StringContent(JsonConvert.SerializeObject(form).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }

        public async Task<int> UpdateFromInternalNote(int deliveryOrderId, InternalNoteFormDto form)
        {
            var uri = "garment-debt-balances/internal-note/";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PutAsync($"{APIEndpoint.Finance}{uri}{deliveryOrderId}", new StringContent(JsonConvert.SerializeObject(form).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }

        public async Task<int> UpdateFromInvoice(int deliveryOrderId, InvoiceFormDto form)
        {
            var uri = "garment-debt-balances/invoice/";
            var httpClient = _serviceProvider.GetService<IHttpClientService>();
            var response = await httpClient.PutAsync($"{APIEndpoint.Finance}{uri}{deliveryOrderId}", new StringContent(JsonConvert.SerializeObject(form).ToString(), Encoding.UTF8, General.JsonMediaType));

            return (int)response.StatusCode;
        }
    }
}
