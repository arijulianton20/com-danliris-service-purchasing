using Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentInvoiceFacades;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Com.DanLiris.Service.Purchasing.Lib.Utilities;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentInvoiceViewModels;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.NewIntegrationViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentInternNoteViewModel
{
    public class GarmentInternNoteViewModel : BaseViewModel, IValidatableObject
    {
        public string UId { get; set; }
        public string inNo { get; set; }
        public DateTimeOffset inDate { get; set; }
        public string remark { get; set; }
        public CurrencyViewModel currency { get; set; }
        public SupplierViewModel supplier { get; set; }
        public List<GarmentInternNoteItemViewModel> items { get; set; }
        public bool isEdit { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            IGarmentInvoice invoiceFacade = validationContext == null ? null : (IGarmentInvoice)validationContext.GetService(typeof(IGarmentInvoice));
            IGarmentDeliveryOrderFacade doFacade = validationContext == null ? null : (IGarmentDeliveryOrderFacade)validationContext.GetService(typeof(IGarmentDeliveryOrderFacade));

            if (currency == null)
            {
                yield return new ValidationResult("currency is required", new List<string> { "currency" });
            }
            if (supplier == null)
            {
                yield return new ValidationResult("Supplier is required", new List<string> { "supplier" });
            }

            int itemErrorCount = 0;
            int detailErrorCount = 0;

            if (this.items == null || items.Count <= 0)
            {
                yield return new ValidationResult("Item is required", new List<string> { "itemscount" });
            }
            else
            {
                string itemError = "[";
                bool? prevUseIncomeTax= null;
                bool? prevUseVat = null;
                string paymentMethod = "";
                long? IncomeTaxId = null;

                foreach (var item in items)
                {
                    itemError += "{";

                    if (item.garmentInvoice == null || item.garmentInvoice.Id == 0)
                    {
                        itemErrorCount++;
                        itemError += "garmentInvoice: 'No Garment Invoice selected', ";
                    }
                    else
                    {
                        var invoice = invoiceFacade.ReadById((int)item.garmentInvoice.Id);
                        if (prevUseIncomeTax != null && prevUseIncomeTax != invoice.UseIncomeTax)
                        {
                            itemErrorCount++;
                            itemError += "useincometax: 'UseIncomeTax harus sama', ";
                        }
                        prevUseIncomeTax = invoice.UseIncomeTax;
                        if (prevUseVat != null && prevUseVat != invoice.UseVat)
                        {
                            itemErrorCount++;
                            itemError += "usevat: 'UseVat harus sama', ";
                        }
                        prevUseVat = invoice.UseVat;
                        if (IncomeTaxId != null && IncomeTaxId != invoice.IncomeTaxId)
                        {
                            itemErrorCount++;
                            itemError += "incometax: 'Income Tax Harus Sama', ";
                        }
                        IncomeTaxId = invoice.IncomeTaxId;
                        if (item.details == null || item.details.Count.Equals(0))
                        {
                            itemErrorCount++;
                            itemError += "detailscount: 'Details is required', ";
                        }
                        else
                        {
                            string detailError = "[";

                            foreach (var detail in item.details)
                            {
                                detailError += "{";
                                var deliveryOrder = doFacade.ReadById((int)detail.deliveryOrder.Id);
                                var invitem = invoice.Items.First(s => s.InvoiceId == item.garmentInvoice.Id);

                                if (invitem != null)
                                {
                                    if (paymentMethod != "" && paymentMethod != invitem.PaymentMethod)
                                    {
                                        detailErrorCount++;
                                        detailError += "paymentMethod: 'TermOfPayment Harus Sama', ";
                                    }
                                    paymentMethod = deliveryOrder.PaymentMethod;
                                }

                                detailError += "}, ";
                            }

                            detailError += "]";

                            if (detailErrorCount > 0)
                            {
                                itemErrorCount++;
                                itemError += $"details: {detailError}, ";
                            }
                        }

                    }


                    itemError += "}, ";
                }

                itemError += "]";

                if (itemErrorCount > 0)
                    yield return new ValidationResult(itemError, new List<string> { "items" });
            }
        }
    }
}
