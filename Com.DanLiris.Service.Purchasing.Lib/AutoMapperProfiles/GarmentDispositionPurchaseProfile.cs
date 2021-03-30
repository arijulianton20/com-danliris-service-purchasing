using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentDispositionPurchaseModel;
using Com.DanLiris.Service.Purchasing.Lib.Utilities;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentDispositionPurchase;
using System;
using System.Collections.Generic;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.AutoMapperProfiles
{
    public class GarmentDispositionPurchaseProfile : BaseAutoMapperProfile
    {
        public GarmentDispositionPurchaseProfile()
        {
            var map = CreateMap<GarmentDispositionPurchase, FormDto>();
            //map.ForAllMembers(opt => opt.Ignore());
            map.ForMember(s => s.Items, opt => opt.MapFrom(d => d.GarmentDispositionPurchaseItems));
            map.ForMember(s => s.PaymentDueDate, opt => opt.MapFrom(d => d.DueDate));
            map.ForMember(s => s.Remark, opt => opt.MapFrom(d => d.Description));
            map.ForMember(s => s.ProformaNo, opt => opt.MapFrom(d => d.InvoiceProformaNo));
            map.ForMember(s => s.DPP, opt => opt.MapFrom(d => d.Dpp));
            map.ForMember(s => s.IncomeTaxValue, opt => opt.MapFrom(d => d.IncomeTax));
            map.ForMember(s => s.VatValue, opt => opt.MapFrom(d => d.VAT));
            map.ForMember(s => s.MiscAmount, opt => opt.MapFrom(d => d.OtherCost));
            map.ForMember(s => s.CurrencyCode, opt => opt.MapFrom(d => d.CurrencyName));

            map.ReverseMap();
            

            var mapItem = CreateMap<GarmentDispositionPurchaseItem, FormItemDto>();
            //mapItem.ForAllMembers(opt => opt.Ignore());
            mapItem.ForMember(s => s.Details, opt => opt.MapFrom(d => d.GarmentDispositionPurchaseDetails));
            mapItem.ForMember(s => s.IsUseVat, opt => opt.MapFrom(d => d.IsVAT));
            mapItem.ForMember(s => s.VatValue, opt => opt.MapFrom(d => d.VATAmount));
            mapItem.ForMember(s => s.IsUseIncomeTax, opt => opt.MapFrom(d => d.IsIncomeTax));
            mapItem.ForMember(s => s.IncomeTaxValue, opt => opt.MapFrom(d => d.IncomeTaxAmount));
            mapItem.ForMember(s => s.CurrencyId, opt => opt.MapFrom(d => d.CurrencyId));
            mapItem.ForMember(s => s.CurrencyRate, opt => opt.MapFrom(d => d.CurrencyRate));
            mapItem.ForMember(s => s.CurrencyCode, opt => opt.MapFrom(d => d.CurrencyCode));


            mapItem.ReverseMap();

            var mapDetail = CreateMap<GarmentDispositionPurchaseDetail, FormDetailDto>();
            //mapDetail.ForAllMembers(opt => opt.Ignore());
            mapDetail.ReverseMap();


            var mapTable = CreateMap<GarmentDispositionPurchase, DispositionPurchaseTableDto>();
            mapTable
                .ForMember(s => s.Category, opt => opt.MapFrom(d => d.Category))
                .ForMember(s => s.CurrencyName, opt => opt.MapFrom(d => d.CurrencyName))
                .ForMember(s => s.CreatedUtc, opt => opt.MapFrom(d => d.CreatedUtc))
                .ForMember(s => s.DispositionNo, opt => opt.MapFrom(d => d.DispositionNo))
                .ForMember(s => s.DueDate, opt => opt.MapFrom(d => d.DueDate))
                .ForMember(s => s.SupplierName, opt => opt.MapFrom(d => d.SupplierName))
                .ForMember(s=> s.AmountDisposition, opt => opt.MapFrom(d=> d.Amount))
                .ReverseMap();

            var mapEdit = CreateMap<GarmentDispositionPurchase, FormEditDto>();
            //map.ForAllMembers(opt => opt.Ignore());
            mapEdit.ForMember(s => s.Items, opt => opt.MapFrom(d => d.GarmentDispositionPurchaseItems));
            mapEdit.ForMember(s => s.PaymentDueDate, opt => opt.MapFrom(d => d.DueDate));
            mapEdit.ForMember(s => s.Remark, opt => opt.MapFrom(d => d.Description));
            mapEdit.ForMember(s => s.ProformaNo, opt => opt.MapFrom(d => d.InvoiceProformaNo));
            mapEdit.ForMember(s => s.DPP, opt => opt.MapFrom(d => d.Dpp));
            mapEdit.ForMember(s => s.IncomeTaxValue, opt => opt.MapFrom(d => d.IncomeTax));
            mapEdit.ForMember(s => s.VatValue, opt => opt.MapFrom(d => d.VAT));
            mapEdit.ForMember(s => s.MiscAmount, opt => opt.MapFrom(d => d.OtherCost));
            mapEdit.ForMember(s => s.CurrencyCode, opt => opt.MapFrom(d => d.CurrencyName));
               
            mapEdit.ReverseMap();
        }
    }
}
