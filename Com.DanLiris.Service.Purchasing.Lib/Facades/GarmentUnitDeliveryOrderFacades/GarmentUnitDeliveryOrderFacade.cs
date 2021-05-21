using AutoMapper;
using Com.DanLiris.Service.Purchasing.Lib.Helpers;
using Com.DanLiris.Service.Purchasing.Lib.Helpers.ReadResponse;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentUnitDeliveryOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentUnitReceiptNoteModel;
using Com.DanLiris.Service.Purchasing.Lib.Services;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.GarmentUnitDeliveryOrderViewModel;
using Com.Moonlay.Models;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentUnitDeliveryOrderFacades
{
    public class GarmentUnitDeliveryOrderFacade : IGarmentUnitDeliveryOrderFacade
    {
        private string USER_AGENT = "Facade";

        public readonly IServiceProvider serviceProvider;
        private readonly IdentityService identityService;

        private readonly PurchasingDbContext dbContext;
        private readonly DbSet<GarmentUnitDeliveryOrder> dbSet;
        private readonly DbSet<GarmentDOItems> dbSetGarmentDOItems;
        private readonly IMapper mapper;

        public GarmentUnitDeliveryOrderFacade(PurchasingDbContext dbContext, IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            identityService = (IdentityService)serviceProvider.GetService(typeof(IdentityService));

            this.dbContext = dbContext;
            dbSet = dbContext.Set<GarmentUnitDeliveryOrder>();
            dbSetGarmentDOItems = dbContext.Set<GarmentDOItems>();
            mapper = serviceProvider == null ? null : (IMapper)serviceProvider.GetService(typeof(IMapper));
        }

        public ReadResponse<object> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
            IQueryable<GarmentUnitDeliveryOrder> Query = dbSet;

            if (!string.IsNullOrWhiteSpace(identityService.Username))
            {
                Query = Query.Where(x => x.CreatedBy == identityService.Username);
            }

            Query = Query.Where(m => m.UnitDOType != "RETUR").Select(m => new GarmentUnitDeliveryOrder
            {
                Id = m.Id,
                UnitDONo = m.UnitDONo,
                UnitDODate = m.UnitDODate,
                UnitDOType = m.UnitDOType,
                UnitRequestCode = m.UnitRequestCode,
                UnitRequestName = m.UnitRequestName,
                UnitSenderCode = m.UnitSenderCode,
                UnitSenderName = m.UnitSenderName,
                StorageName = m.StorageName,
                StorageCode = m.StorageCode,
                StorageRequestCode = m.StorageRequestCode,
                StorageRequestName = m.StorageRequestName,
                IsUsed = m.IsUsed,
                RONo = m.RONo,
                Article = m.Article,
                CreatedBy = m.CreatedBy,
                LastModifiedUtc = m.LastModifiedUtc,
                UENFromNo=m.UENFromNo,
                UENFromId=m.UENFromId,
                Items = m.Items.Select(i => new GarmentUnitDeliveryOrderItem
                {
                    Id = i.Id,
                    DesignColor = i.DesignColor,
                    ProductId = i.ProductId,
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName
                }).ToList()
            });

            List<string> searchAttributes = new List<string>()
            {
                "UnitDONo", "RONo", "UnitDOType", "Article","UnitRequestName","StorageName","UnitSenderName","CreatedBy"
            };

            Query = QueryHelper<GarmentUnitDeliveryOrder>.ConfigureSearch(Query, searchAttributes, Keyword);

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            Query = QueryHelper<GarmentUnitDeliveryOrder>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<GarmentUnitDeliveryOrder>.ConfigureOrder(Query, OrderDictionary);

            Pageable<GarmentUnitDeliveryOrder> pageable = new Pageable<GarmentUnitDeliveryOrder>(Query, Page - 1, Size);
            int TotalData = pageable.TotalCount;

            List<object> ListData = new List<object>();
            ListData.AddRange(pageable.Data.Select(s => new
            {
                s.Id,
                s.UnitDONo,
                s.UnitDODate,
                s.UnitDOType,
                s.RONo,
                s.Article,
                s.UnitRequestName,
                s.StorageName,
                s.CreatedBy,
                s.LastModifiedUtc,
                s.UnitSenderName,
                s.Items
            }));

            return new ReadResponse<object>(ListData, TotalData, OrderDictionary);
        }

        public GarmentUnitDeliveryOrder ReadById(int id)
        {
            var model = dbSet.Where(m => m.Id == id)
                .Include(m => m.Items)
                .FirstOrDefault();
            return model;
        }

        public async Task<int> Create(GarmentUnitDeliveryOrder garmentUnitDeliveryOrder)
        {
            int Created = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    garmentUnitDeliveryOrder.Items = garmentUnitDeliveryOrder.Items.Where(x => x.IsSave).ToList();

                    EntityExtension.FlagForCreate(garmentUnitDeliveryOrder, identityService.Username, USER_AGENT);

                    garmentUnitDeliveryOrder.UnitDONo = await GenerateNo(garmentUnitDeliveryOrder);

                    foreach (var garmentUnitDeliveryOrderItem in garmentUnitDeliveryOrder.Items)
                    {
                        EntityExtension.FlagForCreate(garmentUnitDeliveryOrderItem, identityService.Username, USER_AGENT);

                        
                        // GarmentDOItems
                        GarmentDOItems garmentDOItems = dbSetGarmentDOItems.Single(w => w.Id == garmentUnitDeliveryOrderItem.DOItemsId);
                        
                        EntityExtension.FlagForUpdate(garmentDOItems, identityService.Username, USER_AGENT);
                        garmentDOItems.RemainingQuantity = garmentDOItems.RemainingQuantity - (decimal)garmentUnitDeliveryOrderItem.Quantity;

                        garmentUnitDeliveryOrderItem.DOCurrencyRate = garmentDOItems.DOCurrencyRate;
                        if (garmentUnitDeliveryOrderItem.DOCurrencyRate == 0)
                        {
                            throw new Exception("garmentUnitDeliveryOrderItem.DOCurrencyRate tidak boleh 0");
                        }
                        GarmentUnitReceiptNote garmentUnitReceiptNote = dbContext.GarmentUnitReceiptNotes.IgnoreQueryFilters().Single(s => s.Id == garmentUnitDeliveryOrderItem.URNId);
                        garmentUnitReceiptNote.IsUnitDO = true;

                        GarmentUnitReceiptNoteItem garmentUnitReceiptNoteItem = dbContext.GarmentUnitReceiptNoteItems.IgnoreQueryFilters().Single(s => s.Id == garmentUnitDeliveryOrderItem.URNItemId);
                        garmentUnitReceiptNoteItem.OrderQuantity = garmentUnitReceiptNoteItem.OrderQuantity + (decimal)garmentUnitDeliveryOrderItem.Quantity;

                        EntityExtension.FlagForUpdate(garmentUnitReceiptNoteItem, identityService.Username, USER_AGENT);
                        
                    }

                    dbSet.Add(garmentUnitDeliveryOrder);

                    Created = await dbContext.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }

            return Created;
        }

        public async Task<int> Delete(int id)
        {
            int Deleted = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    var garmentUnitDeliveryOrder = dbSet
                        .Include(m => m.Items)
                        .SingleOrDefault(m => m.Id == id);

                    EntityExtension.FlagForDelete(garmentUnitDeliveryOrder, identityService.Username, USER_AGENT);
                    foreach (var garmentUnitDeliveryOrderItem in garmentUnitDeliveryOrder.Items)
                    {
                        EntityExtension.FlagForDelete(garmentUnitDeliveryOrderItem, identityService.Username, USER_AGENT);

                        GarmentUnitReceiptNoteItem garmentUnitReceiptNoteItem = dbContext.GarmentUnitReceiptNoteItems.Single(s => s.Id == garmentUnitDeliveryOrderItem.URNItemId);
                        EntityExtension.FlagForUpdate(garmentUnitReceiptNoteItem, identityService.Username, USER_AGENT);
                        garmentUnitReceiptNoteItem.OrderQuantity = garmentUnitReceiptNoteItem.OrderQuantity - (decimal)garmentUnitDeliveryOrderItem.Quantity;

                        // GarmentDOItems
                        GarmentDOItems garmentDOItems = dbSetGarmentDOItems.FirstOrDefault(w => w.Id == garmentUnitDeliveryOrderItem.DOItemsId);
                        if (garmentDOItems != null)
                        {
                            EntityExtension.FlagForUpdate(garmentDOItems, identityService.Username, USER_AGENT);
                            garmentDOItems.RemainingQuantity = garmentDOItems.RemainingQuantity + (decimal)garmentUnitDeliveryOrderItem.Quantity;
                        }
                    }

                    Deleted = await dbContext.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }

            return Deleted;
        }

        public async Task<int> Update(int id, GarmentUnitDeliveryOrder garmentUnitDeliveryOrder)
        {
            int Updated = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    garmentUnitDeliveryOrder.Items = garmentUnitDeliveryOrder.Items.Where(x => x.IsSave).ToList();

                    var oldGarmentUnitDeliveryOrder = dbSet
                        .Include(d => d.Items)
                        //.AsNoTracking()
                        .Single(m => m.Id == id);
                    //if (oldGarmentUnitDeliveryOrder.UnitDOType == "MARKETING")
                    //{
                    //    oldGarmentUnitDeliveryOrder.UnitDODate = garmentUnitDeliveryOrder.UnitDODate;
                    //}
                    EntityExtension.FlagForUpdate(oldGarmentUnitDeliveryOrder, identityService.Username, USER_AGENT);

                    foreach (var garmentUnitDeliveryOrderItem in garmentUnitDeliveryOrder.Items)
                    {
                        if (garmentUnitDeliveryOrderItem.Id != 0)
                        {
                            var oldGarmentUnitDeliveryOrderItem = oldGarmentUnitDeliveryOrder.Items.FirstOrDefault(i => i.Id == garmentUnitDeliveryOrderItem.Id);

                            EntityExtension.FlagForUpdate(oldGarmentUnitDeliveryOrderItem, identityService.Username, USER_AGENT);

                            GarmentUnitReceiptNoteItem garmentUnitReceiptNoteItem = dbContext.GarmentUnitReceiptNoteItems.Single(s => s.Id == oldGarmentUnitDeliveryOrderItem.URNItemId);
                            EntityExtension.FlagForUpdate(garmentUnitReceiptNoteItem, identityService.Username, USER_AGENT);
                            garmentUnitReceiptNoteItem.OrderQuantity = garmentUnitReceiptNoteItem.OrderQuantity - (decimal)oldGarmentUnitDeliveryOrderItem.Quantity + (decimal)garmentUnitDeliveryOrderItem.Quantity;

                            // GarmentDOItems
                            GarmentDOItems garmentDOItems = dbSetGarmentDOItems.FirstOrDefault(w => w.Id == garmentUnitDeliveryOrderItem.DOItemsId);
                            if (garmentDOItems != null)
                            {
                                EntityExtension.FlagForUpdate(garmentDOItems, identityService.Username, USER_AGENT);
                                garmentDOItems.RemainingQuantity = garmentDOItems.RemainingQuantity + (decimal)oldGarmentUnitDeliveryOrderItem.Quantity - (decimal)garmentUnitDeliveryOrderItem.Quantity;
                            }

                            oldGarmentUnitDeliveryOrderItem.Quantity = garmentUnitDeliveryOrderItem.Quantity;
                            oldGarmentUnitDeliveryOrderItem.DefaultDOQuantity = garmentUnitDeliveryOrderItem.Quantity; // Jumlah DO awal mengikuti Jumlah yang diubah (reset)
                            oldGarmentUnitDeliveryOrderItem.FabricType = garmentUnitDeliveryOrderItem.FabricType;
                        }
                        else
                        {
                            EntityExtension.FlagForCreate(garmentUnitDeliveryOrderItem, identityService.Username, USER_AGENT);
                            GarmentUnitReceiptNote garmentUnitReceiptNote = dbContext.GarmentUnitReceiptNotes.Single(s => s.Id == garmentUnitDeliveryOrderItem.URNId);
                            garmentUnitDeliveryOrderItem.DOCurrencyRate = garmentUnitReceiptNote.DOCurrencyRate;
                            if (garmentUnitDeliveryOrderItem.DOCurrencyRate == 0)
                            {
                                throw new Exception("oldGarmentUnitDeliveryOrderItem.DOCurrencyRate tidak boleh 0");
                            }
                            oldGarmentUnitDeliveryOrder.Items.Add(garmentUnitDeliveryOrderItem);

                            GarmentUnitReceiptNoteItem garmentUnitReceiptNoteItem = dbContext.GarmentUnitReceiptNoteItems.Single(s => s.Id == garmentUnitDeliveryOrderItem.URNItemId);
                            EntityExtension.FlagForUpdate(garmentUnitReceiptNoteItem, identityService.Username, USER_AGENT);
                            garmentUnitReceiptNoteItem.OrderQuantity = garmentUnitReceiptNoteItem.OrderQuantity + (decimal)garmentUnitDeliveryOrderItem.Quantity;

                            // GarmentDOItems
                            GarmentDOItems garmentDOItems = dbSetGarmentDOItems.FirstOrDefault(w => w.Id == garmentUnitDeliveryOrderItem.DOItemsId);
                            if (garmentDOItems != null)
                            {
                                EntityExtension.FlagForUpdate(garmentDOItems, identityService.Username, USER_AGENT);
                                garmentDOItems.RemainingQuantity = garmentDOItems.RemainingQuantity - (decimal)garmentUnitDeliveryOrderItem.Quantity;
                            }
                        }
                    }

                    foreach (var oldGarmentUnitDeliveryOrderItem in oldGarmentUnitDeliveryOrder.Items)
                    {
                        var newGarmentUnitDeliveryOrderItem = garmentUnitDeliveryOrder.Items.FirstOrDefault(i => i.Id == oldGarmentUnitDeliveryOrderItem.Id);
                        if (newGarmentUnitDeliveryOrderItem == null)
                        {
                            EntityExtension.FlagForDelete(oldGarmentUnitDeliveryOrderItem, identityService.Username, USER_AGENT);

                            GarmentUnitReceiptNote garmentUnitReceiptNote = dbContext.GarmentUnitReceiptNotes.Single(s => s.Id == oldGarmentUnitDeliveryOrderItem.URNId);
                            

                            GarmentUnitReceiptNoteItem garmentUnitReceiptNoteItem = dbContext.GarmentUnitReceiptNoteItems.Single(s => s.Id == oldGarmentUnitDeliveryOrderItem.URNItemId);
                            oldGarmentUnitDeliveryOrderItem.DOCurrencyRate = garmentUnitReceiptNoteItem.DOCurrencyRate;

                            EntityExtension.FlagForUpdate(garmentUnitReceiptNoteItem, identityService.Username, USER_AGENT);
                            garmentUnitReceiptNoteItem.OrderQuantity = garmentUnitReceiptNoteItem.OrderQuantity - (decimal)oldGarmentUnitDeliveryOrderItem.Quantity;

                            // GarmentDOItems
                            GarmentDOItems garmentDOItems = dbSetGarmentDOItems.FirstOrDefault(w => w.Id == oldGarmentUnitDeliveryOrderItem.DOItemsId);
                            if (garmentDOItems != null)
                            {
                                EntityExtension.FlagForUpdate(garmentDOItems, identityService.Username, USER_AGENT);
                                garmentDOItems.RemainingQuantity = garmentDOItems.RemainingQuantity + (decimal)oldGarmentUnitDeliveryOrderItem.Quantity;
                            }
                        }
                    }

                   // dbSet.Update(garmentUnitDeliveryOrder);

                    Updated = await dbContext.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }

            return Updated;
        }

        public async Task<string> GenerateNo(GarmentUnitDeliveryOrder model)
        {
            DateTimeOffset dateTimeOffset = model.UnitDODate.ToOffset(new TimeSpan(identityService.TimezoneOffset, 0, 0));
            string Month = dateTimeOffset.ToString("MM");
            string Year = dateTimeOffset.ToString("yy");
            string Day = dateTimeOffset.ToString("dd");

            string pre = model.UnitDOType == "MARKETING" ? "DOM" : "DO";
            string unitCode= model.UnitDOType == "MARKETING" ? model.UnitSenderCode: model.UnitRequestCode;

            string no = string.Concat(pre, unitCode, Year, Month, Day);
            int Padding = 3;

            var lastDataByNo = await dbSet.Where(w => w.UnitDONo.StartsWith(no) && !w.IsDeleted).OrderByDescending(o => o.UnitDONo).FirstOrDefaultAsync();

            if (lastDataByNo == null)
            {
                return no + "1".PadLeft(Padding, '0');
            }
            else
            {
                int.TryParse(lastDataByNo.UnitDONo.Replace(no, ""), out int lastNoNumber);
                return string.Concat(no, (lastNoNumber + 1).ToString().PadLeft(Padding, '0'));
            }
        }

        public ReadResponse<object> ReadForUnitExpenditureNote(int Page = 1, int Size = 10, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
			var username = identityService.Username;

			Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            //string unitDOType = "";
            bool hasUnitDOTypeFilter = FilterDictionary.ContainsKey("UnitDOType");
            IQueryable<GarmentUnitDeliveryOrder> Query = dbSet
                .Where(x => x.UnitDONo.Contains(Keyword ?? ""))
                .Select(m => new GarmentUnitDeliveryOrder
                {
                    Id = m.Id,
                    UnitDONo = m.UnitDONo,
                    UnitDOType = m.UnitDOType,
                    UnitDODate = m.UnitDODate,
                    UnitSenderId = m.UnitSenderId,
                    UnitSenderCode = m.UnitSenderCode,
                    UnitSenderName = m.UnitSenderName,
                    UnitRequestId = m.UnitRequestId,
                    UnitRequestCode = m.UnitRequestCode,
                    UnitRequestName = m.UnitRequestName,
                    StorageId = m.StorageId,
                    StorageCode = m.StorageCode,
                    StorageName = m.StorageName,
                    StorageRequestId = m.StorageRequestId,
                    StorageRequestCode = m.StorageRequestCode,
                    StorageRequestName = m.StorageRequestName,
                    IsUsed = m.IsUsed,
                    LastModifiedUtc = m.LastModifiedUtc,
                    CreatedBy=m.CreatedBy,
                    Items = m.Items.Select(i => new GarmentUnitDeliveryOrderItem
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductCode = i.ProductCode,
                        ProductName = i.ProductName,
                        ProductRemark = i.ProductRemark,
                        PRItemId = i.PRItemId,
                        EPOItemId = i.EPOItemId,
                        DODetailId = i.DODetailId,
                        POItemId = i.POItemId,
                        POSerialNumber = i.POSerialNumber,
                        PricePerDealUnit = i.PricePerDealUnit,
                        Quantity = i.Quantity,
                        DefaultDOQuantity = i.DefaultDOQuantity,
                        RONo = i.RONo,
                        URNItemId = i.URNItemId,
                        UomId = i.UomId,
                        UomUnit = i.UomUnit,
                        FabricType = i.FabricType,
                        DesignColor = i.DesignColor,
                        DOCurrencyRate = i.DOCurrencyRate,
                    }).ToList()
                });

            Query = QueryHelper<GarmentUnitDeliveryOrder>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<GarmentUnitDeliveryOrder>.ConfigureOrder(Query, OrderDictionary);

            Pageable<GarmentUnitDeliveryOrder> pageable = new Pageable<GarmentUnitDeliveryOrder>(Query, Page - 1, Size);
            List<GarmentUnitDeliveryOrder> DataModel = pageable.Data.ToList();
            int Total = pageable.TotalCount;

            List<GarmentUnitDeliveryOrderViewModel> DataViewModel = mapper.Map<List<GarmentUnitDeliveryOrderViewModel>>(DataModel);

            List<dynamic> listData = new List<dynamic>();
            listData.AddRange(
                DataViewModel.Select(s => new
                {
                    s.Id,
                    s.UnitDONo,
                    s.UnitDOType,
                    s.IsUsed,
                    s.Storage,
                    s.UnitDODate,
                    s.StorageRequest,
                    s.UnitRequest,
                    s.UnitSender,
                    s.CreatedBy,
                    s.LastModifiedUtc,
                    Items = s.Items.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        i.ProductCode,
                        i.ProductName,
                        i.ProductRemark,
                        i.Quantity,
                        i.DefaultDOQuantity,
                        i.DODetailId,
                        i.EPOItemId,
                        i.FabricType,
                        i.PricePerDealUnit,
                        i.POSerialNumber,
                        i.POItemId,
                        i.PRItemId,
                        i.UomId,
                        i.UomUnit,
                        i.RONo,
                        i.URNItemId,
                        i.DesignColor,
                        i.DOCurrency,
                        Buyer = new
                        {
                            Id = dbContext.GarmentInternalPurchaseOrders.Where(m => m.Items.Any(k => k.Id == i.POItemId)).Select(m => m.BuyerId).FirstOrDefault(),
                            Code = dbContext.GarmentInternalPurchaseOrders.Where(m => m.Items.Any(k => k.Id == i.POItemId)).Select(m => m.BuyerCode).FirstOrDefault()
                        },
                    }).ToList()
                }).ToList()
            );
            return new ReadResponse<object>(listData, Total, OrderDictionary);
        }
    }
}
