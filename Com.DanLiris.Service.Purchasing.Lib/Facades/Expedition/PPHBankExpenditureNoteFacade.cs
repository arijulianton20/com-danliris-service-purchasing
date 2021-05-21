using Com.DanLiris.Service.Purchasing.Lib.Helpers;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Com.DanLiris.Service.Purchasing.Lib.Models.Expedition;
using Com.Moonlay.Models;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Com.DanLiris.Service.Purchasing.Lib.Enums;
using Com.DanLiris.Service.Purchasing.Lib.Helpers.ReadResponse;
using System.Net.Http;
using System.Text;
using Com.DanLiris.Service.Purchasing.Lib.Utilities.CacheManager.CacheData;
using Com.DanLiris.Service.Purchasing.Lib.Utilities.CacheManager;
using Com.DanLiris.Service.Purchasing.Lib.Services;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.IntegrationViewModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.NewIntegrationViewModel;
using Com.DanLiris.Service.Purchasing.Lib.Models.UnitPaymentOrderModel;
using Com.DanLiris.Service.Purchasing.Lib.Utilities.Currencies;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.Expedition
{
    public class PPHBankExpenditureNoteFacade : IPPHBankExpenditureNoteFacade, IReadByIdable<PPHBankExpenditureNote>
    {
        private const string UserAgent = "Facade";
        private readonly PurchasingDbContext dbContext;
        private readonly DbSet<PPHBankExpenditureNote> dbSet;
        private readonly DbSet<PurchasingDocumentExpedition> dbSetPurchasingDocumentExpedition;
        private readonly IBankDocumentNumberGenerator bankDocumentNumberGenerator;
        private readonly IServiceProvider _serviceProvider;
        //private readonly IdentityService identityService;
        private readonly IMemoryCacheManager _cacheManager;
        private readonly IdentityService identityService;

        public PPHBankExpenditureNoteFacade(PurchasingDbContext dbContext, IBankDocumentNumberGenerator bankDocumentNumberGenerator, IServiceProvider serviceProvider)
        {
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<PPHBankExpenditureNote>();
            this.dbSetPurchasingDocumentExpedition = dbContext.Set<PurchasingDocumentExpedition>();
            this.bankDocumentNumberGenerator = bankDocumentNumberGenerator;
            _serviceProvider = serviceProvider;
            //identityService = (IdentityService)serviceProvider.GetService(typeof(IdentityService));
            _cacheManager = (IMemoryCacheManager)serviceProvider.GetService(typeof(IMemoryCacheManager));
            this.identityService = (IdentityService)serviceProvider.GetService(typeof(IdentityService));

        }

        public List<object> GetUnitPaymentOrder(DateTimeOffset? dateFrom, DateTimeOffset? dateTo, string incomeTaxName, double incomeTaxRate, string currency)
        {
            IQueryable<PurchasingDocumentExpedition> Query = this.dbSetPurchasingDocumentExpedition;

            if (dateFrom == null || dateTo == null)
            {
                Query = Query
               .Where(p => p.IsDeleted == false &&
                   p.IncomeTaxName == incomeTaxName &&
                   p.IncomeTaxRate == incomeTaxRate &&
                   p.Currency == currency &&
                   p.IncomeTaxRate != 0 &&
                   p.IsPaidPPH == false && (p.Position == ExpeditionPosition.CASHIER_DIVISION || p.Position == ExpeditionPosition.FINANCE_DIVISION)
               );
            }
            else
            {
                Query = Query
               .Where(p => p.IsDeleted == false &&
                   p.IncomeTaxName == incomeTaxName &&
                   p.IncomeTaxRate == incomeTaxRate &&
                   p.Currency == currency &&
                   p.IncomeTaxRate != 0 &&
                   p.DueDate.Date >= dateFrom.Value.Date &&
                   p.DueDate.Date <= dateTo.Value.Date &&
                   p.IsPaidPPH == false && (p.Position == ExpeditionPosition.CASHIER_DIVISION || p.Position == ExpeditionPosition.FINANCE_DIVISION)
               );
            }

            Query = Query
                .Select(s => new PurchasingDocumentExpedition
                {
                    Id = s.Id,
                    UnitPaymentOrderNo = s.UnitPaymentOrderNo,
                    UPODate = s.UPODate,
                    DueDate = s.DueDate,
                    InvoiceNo = s.InvoiceNo,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName,
                    CategoryCode = s.CategoryCode,
                    CategoryName = s.CategoryName,
                    DivisionCode = s.DivisionCode,
                    DivisionName = s.DivisionName,
                    IncomeTax = s.IncomeTax,
                    Vat = s.Vat,
                    TotalPaid = s.TotalPaid,
                    Currency = s.Currency,
                    Items = s.Items.Where(d => d.PurchasingDocumentExpeditionId == s.Id).ToList(),
                    LastModifiedUtc = s.LastModifiedUtc
                }).OrderBy(s => s.UnitPaymentOrderNo.Remove(5, 6));

            List<object> list = new List<object>();
            list.AddRange(
               Query.ToList().Select(s => new
               {
                   Id = s.Id,
                   No = s.UnitPaymentOrderNo,
                   UPODate = s.UPODate,
                   DueDate = s.DueDate,
                   InvoiceNo = s.InvoiceNo,
                   SupplierCode = s.SupplierCode,
                   SupplierName = s.SupplierName,
                   CategoryCode = s.CategoryCode,
                   CategoryName = s.CategoryName,
                   DivisionCode = s.DivisionCode,
                   DivisionName = s.DivisionName,
                   IncomeTax = s.IncomeTax,
                   Vat = s.Vat,
                   TotalPaid = s.TotalPaid,
                   Currency = s.Currency,
                   Items = s.Items,
                   LastModifiedUtc = s.LastModifiedUtc
               }).ToList());

            return list;
        }

        public ReadResponse<object> Read(int page = 1, int size = 25, string order = "{}", string keyword = null, string filter = "{}")
        {
            IQueryable<PPHBankExpenditureNote> Query = this.dbSet;

            List<string> searchAttributes = new List<string>()
            {
                "No", "BankAccountName", "Items.UnitPaymentOrderNo"
            };

            Query = QueryHelper<PPHBankExpenditureNote>.ConfigureSearch(Query, searchAttributes, keyword);

            Query = Query
                .Select(s => new PPHBankExpenditureNote
                {
                    Id = s.Id,
                    Date = s.Date,
                    No = s.No,
                    CreatedUtc = s.CreatedUtc,
                    BankAccountName = s.BankAccountName,
                    IncomeTaxName = s.IncomeTaxName,
                    IncomeTaxRate = s.IncomeTaxRate,
                    TotalDPP = s.TotalDPP,
                    TotalIncomeTax = s.TotalIncomeTax,
                    Currency = s.Currency,
                    Items = s.Items.Select(p => new PPHBankExpenditureNoteItem { UnitPaymentOrderNo = p.UnitPaymentOrderNo, PPHBankExpenditureNoteId = p.PPHBankExpenditureNoteId }).Where(p => p.PPHBankExpenditureNoteId == s.Id).ToList(),
                    LastModifiedUtc = s.LastModifiedUtc,
                    IsPosted = s.IsPosted
                });

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(filter);
            Query = QueryHelper<PPHBankExpenditureNote>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(order);
            Query = QueryHelper<PPHBankExpenditureNote>.ConfigureOrder(Query, OrderDictionary);

            Pageable<PPHBankExpenditureNote> pageable = new Pageable<PPHBankExpenditureNote>(Query, page - 1, size);
            List<PPHBankExpenditureNote> Data = pageable.Data.ToList<PPHBankExpenditureNote>();
            int TotalData = pageable.TotalCount;

            List<object> list = new List<object>();
            list.AddRange(
               Data.Select(s => new
               {
                   Id = s.Id,
                   Date = s.Date,
                   No = s.No,
                   CreatedUtc = s.CreatedUtc,
                   BankAccountName = s.BankAccountName,
                   IncomeTaxName = s.IncomeTaxName,
                   IncomeTaxRate = s.IncomeTaxRate,
                   TotalDPP = s.TotalDPP,
                   TotalIncomeTax = s.TotalIncomeTax,
                   Currency = s.Currency,
                   Items = s.Items.Select(p => new { UnitPaymentOrderNo = p.UnitPaymentOrderNo, PPHBankExpenditureNoteId = p.PPHBankExpenditureNoteId }).Where(p => p.PPHBankExpenditureNoteId == s.Id).ToList(),
                   LastModifiedUtc = s.LastModifiedUtc,
                   s.IsPosted
               }).ToList()
            );

            return new ReadResponse<object>(list, TotalData, OrderDictionary);
        }

        public async Task<int> Update(int id, PPHBankExpenditureNote model, string username)
        {
            int Updated = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    EntityExtension.FlagForUpdate(model, username, UserAgent);
                    dbContext.Entry(model).Property(x => x.Date).IsModified = true;
                    dbContext.Entry(model).Property(x => x.TotalDPP).IsModified = true;
                    dbContext.Entry(model).Property(x => x.TotalIncomeTax).IsModified = true;
                    dbContext.Entry(model).Property(x => x.BGNo).IsModified = true;
                    dbContext.Entry(model).Property(x => x.LastModifiedAgent).IsModified = true;
                    dbContext.Entry(model).Property(x => x.LastModifiedBy).IsModified = true;
                    dbContext.Entry(model).Property(x => x.LastModifiedUtc).IsModified = true;

                    foreach (var item in model.Items)
                    {
                        if (item.Id == 0)
                        {
                            EntityExtension.FlagForCreate(item, username, UserAgent);
                            dbContext.PPHBankExpenditureNoteItems.Add(item);

                            PurchasingDocumentExpedition pde = new PurchasingDocumentExpedition
                            {
                                Id = item.PurchasingDocumentExpeditionId,
                                IsPaidPPH = true,
                                BankExpenditureNotePPHNo = model.No,
                                BankExpenditureNotePPHDate = model.Date
                            };

                            EntityExtension.FlagForUpdate(pde, username, UserAgent);
                            //dbContext.Attach(pde);
                            dbContext.Entry(pde).Property(x => x.IsPaidPPH).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHNo).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHDate).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.LastModifiedAgent).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.LastModifiedBy).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.LastModifiedUtc).IsModified = true;
                        }
                    }

                    foreach (var item in dbContext.PPHBankExpenditureNoteItems.AsNoTracking().Where(p => p.PPHBankExpenditureNoteId == model.Id))
                    {
                        PPHBankExpenditureNoteItem itemModel = model.Items.FirstOrDefault(prop => prop.Id.Equals(item.Id));

                        if (itemModel == null)
                        {
                            EntityExtension.FlagForDelete(item, username, UserAgent);
                            this.dbContext.PPHBankExpenditureNoteItems.Update(item);

                            PurchasingDocumentExpedition pde = new PurchasingDocumentExpedition
                            {
                                Id = item.PurchasingDocumentExpeditionId,
                                IsPaidPPH = false,
                                BankExpenditureNotePPHDate = null,
                                BankExpenditureNotePPHNo = null
                            };

                            EntityExtension.FlagForUpdate(pde, username, UserAgent);
                            //dbContext.Attach(pde);
                            dbContext.Entry(pde).Property(x => x.IsPaidPPH).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHNo).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHDate).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.LastModifiedAgent).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.LastModifiedBy).IsModified = true;
                            dbContext.Entry(pde).Property(x => x.LastModifiedUtc).IsModified = true;
                        }
                    }

                    Updated = await dbContext.SaveChangesAsync();

                    //await ReverseJournalTransaction(model.No);
                    //await AutoCreateJournalTransaction(model);

                    //await DeleteDailyBankTransaction(model.No);
                    //await CreateDailyBankTransaction(model);

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

        //private List<IdCOAResult> Units => _cacheManager.Get(MemoryCacheConstant.Units, entry => { return new List<IdCOAResult>(); });
        private List<IdCOAResult> Divisions => _cacheManager.Get(MemoryCacheConstant.Divisions, entry => { return new List<IdCOAResult>(); });
        private List<BankAccountCOAResult> BankAccounts => _cacheManager.Get(MemoryCacheConstant.BankAccounts, entry => { return new List<BankAccountCOAResult>(); });
        private List<IncomeTaxCOAResult> IncomeTaxes => _cacheManager.Get(MemoryCacheConstant.IncomeTaxes, entry => { return new List<IncomeTaxCOAResult>(); });

        private async Task AutoCreateJournalTransaction(PPHBankExpenditureNote model)
        {
            var journalTransactionToPost = new JournalTransaction()
            {
                Date = model.Date,
                Description = "Pengajuan Pembayaran PPh",
                ReferenceNo = model.No,
                Status = "POSTED",
                Items = new List<JournalTransactionItem>()
            };

            int.TryParse(model.BankId, out int bankAccountId);
            var bankAccount = BankAccounts.FirstOrDefault(entity => entity.Id == bankAccountId);
            if (bankAccount == null)
            {
                bankAccount = new BankAccountCOAResult()
                {
                    AccountCOA = "9999.00.00.00"
                };
            }

            int.TryParse(model.IncomeTaxId, out int incomeTaxId);
            var incomeTax = IncomeTaxes.FirstOrDefault(entity => entity.Id == incomeTaxId);
            if (incomeTax == null)
            {
                incomeTax = new IncomeTaxCOAResult()
                {
                    COACodeCredit = "9999.00"
                };
            }

            var journalDebitItems = new List<JournalTransactionItem>();
            var journalCreditItems = new List<JournalTransactionItem>();


            var purchasingDocumentExpeditionIds = model.Items.Select(item => item.PurchasingDocumentExpeditionId).ToList();
            var purchasingDocumentExpeditions = await dbContext.PurchasingDocumentExpeditions.Include(entity => entity.Items).Where(entity => purchasingDocumentExpeditionIds.Contains(entity.Id)).ToListAsync();
            foreach (var item in model.Items)
            {
                var purchasingDocumentExpedition = purchasingDocumentExpeditions.FirstOrDefault(entity => entity.Id == item.PurchasingDocumentExpeditionId);
                var division = Divisions.FirstOrDefault(entity => entity.Code == purchasingDocumentExpedition.DivisionCode);
                if (division == null)
                {
                    division = new IdCOAResult()
                    {
                        COACode = "0"
                    };
                }

                journalDebitItems.Add(new JournalTransactionItem()
                {
                    COA = new COA()
                    {
                        Code = $"{incomeTax.COACodeCredit}.{division.COACode}.00"
                    },
                    Debit = (decimal)purchasingDocumentExpedition.IncomeTax * (decimal)model.CurrencyRate.GetValueOrDefault()
                });
            }


            journalCreditItems.Add(new JournalTransactionItem()
            {
                COA = new COA()
                {
                    Code = bankAccount.AccountCOA
                },
                Credit = journalDebitItems.Sum(element => element.Debit)
            });

            journalDebitItems = journalDebitItems.GroupBy(grouping => grouping.COA.Code).Select(s => new JournalTransactionItem()
            {
                COA = new COA()
                {
                    Code = s.Key
                },
                Debit = s.Sum(sum => Math.Round(sum.Debit.GetValueOrDefault(), 4)),
                Credit = 0,
                //Remark = string.Join("\n", s.Select(grouped => grouped.Remark).ToList())
            }).ToList();
            journalTransactionToPost.Items.AddRange(journalDebitItems);

            journalCreditItems = journalCreditItems.GroupBy(grouping => grouping.COA.Code).Select(s => new JournalTransactionItem()
            {
                COA = new COA()
                {
                    Code = s.Key
                },
                Debit = 0,
                Credit = s.Sum(sum => Math.Round(sum.Credit.GetValueOrDefault(), 4)),
                //Remark = string.Join("\n", s.Select(grouped => grouped.Remark).ToList())
            }).ToList();
            journalTransactionToPost.Items.AddRange(journalCreditItems);

            if (journalTransactionToPost.Items.Any(item => item.COA.Code.Split(".").FirstOrDefault().Equals("9999")))
                journalTransactionToPost.Status = "DRAFT";

            string journalTransactionUri = "journal-transactions";
            var httpClient = (IHttpClientService)_serviceProvider.GetService(typeof(IHttpClientService));
            var response = await httpClient.PostAsync($"{APIEndpoint.Finance}{journalTransactionUri}", new StringContent(JsonConvert.SerializeObject(journalTransactionToPost).ToString(), Encoding.UTF8, General.JsonMediaType));

            response.EnsureSuccessStatusCode();
        }

        //private async Task ReverseJournalTransaction(string referenceNo)
        //{
        //    string journalTransactionUri = $"journal-transactions/reverse-transactions/{referenceNo}";
        //    var httpClient = (IHttpClientService)_serviceProvider.GetService(typeof(IHttpClientService));
        //    var response = await httpClient.PostAsync($"{APIEndpoint.Finance}{journalTransactionUri}", new StringContent(JsonConvert.SerializeObject(new object()).ToString(), Encoding.UTF8, General.JsonMediaType));

        //    response.EnsureSuccessStatusCode();
        //}

        public async Task<PPHBankExpenditureNote> ReadById(int id)
        {
            return await this.dbContext.PPHBankExpenditureNotes
                .AsNoTracking()
                .Include(p => p.Items)
                    .ThenInclude(p => p.PurchasingDocumentExpedition)
                        .ThenInclude(p => p.Items)
                .Where(d => d.Id.Equals(id) && d.IsDeleted.Equals(false))
                .FirstOrDefaultAsync();
        }

        public async Task<int> Create(PPHBankExpenditureNote model, string username)
        {
            int Created = 0;
            var timeOffset = new TimeSpan(identityService.TimezoneOffset, 0, 0);
            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    EntityExtension.FlagForCreate(model, username, UserAgent);
                    model.No = await bankDocumentNumberGenerator.GenerateDocumentNumber("K", model.BankCode, username,model.Date.ToOffset(timeOffset).Date);
                    model.CurrencyRate = 1;
                    if (model.Currency != "IDR")
                    {
                        var garmentCurrency = await GetGarmentCurrency(model.Currency);
                        model.CurrencyRate = garmentCurrency.Rate.GetValueOrDefault();
                    }

                    foreach (var item in model.Items)
                    {
                        EntityExtension.FlagForCreate(item, username, UserAgent);

                        PurchasingDocumentExpedition pde = new PurchasingDocumentExpedition
                        {
                            Id = item.PurchasingDocumentExpeditionId,
                            IsPaidPPH = true,
                            BankExpenditureNotePPHNo = model.No,
                            BankExpenditureNotePPHDate = model.Date
                        };

                        EntityExtension.FlagForUpdate(pde, username, UserAgent);
                        //dbContext.Attach(pde);
                        dbContext.Entry(pde).Property(x => x.IsPaidPPH).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHNo).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHDate).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.LastModifiedAgent).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.LastModifiedBy).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.LastModifiedUtc).IsModified = true;
                    }

                    this.dbSet.Add(model);
                    Created = await dbContext.SaveChangesAsync();
                    //await AutoCreateJournalTransaction(model);
                    //await CreateDailyBankTransaction(model);
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

        private async Task<GarmentCurrency> GetGarmentCurrency(string codeCurrency)
        {
            string date = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
            string queryString = $"code={codeCurrency}&stringDate={date}";

            var http = _serviceProvider.GetService<IHttpClientService>();
            var response = await http.GetAsync(APIEndpoint.Core + $"master/garment-currencies/single-by-code-date?{queryString}");

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonSerializationSetting = new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore };

            var result = JsonConvert.DeserializeObject<APIDefaultResponse<GarmentCurrency>>(responseString, jsonSerializationSetting);

            return result.data;
        }

        public async Task<int> Delete(int id, string username)
        {
            int Count = 0;

            if (this.dbContext.PPHBankExpenditureNotes.Count(p => p.Id == id && p.IsDeleted == false).Equals(0))
            {
                return 0;
            }

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    PPHBankExpenditureNote PPHBankExpenditureNote = dbContext.PPHBankExpenditureNotes.Single(p => p.Id == id);

                    ICollection<PPHBankExpenditureNoteItem> Items = new List<PPHBankExpenditureNoteItem>(this.dbContext.PPHBankExpenditureNoteItems.Where(p => p.PPHBankExpenditureNoteId.Equals(id)));

                    foreach (PPHBankExpenditureNoteItem item in Items)
                    {
                        EntityExtension.FlagForDelete(item, username, UserAgent);
                        this.dbContext.PPHBankExpenditureNoteItems.Update(item);

                        PurchasingDocumentExpedition pde = new PurchasingDocumentExpedition
                        {
                            Id = item.PurchasingDocumentExpeditionId,
                            IsPaidPPH = false,
                            BankExpenditureNotePPHNo = null,
                            BankExpenditureNotePPHDate = null
                        };

                        EntityExtension.FlagForUpdate(pde, username, UserAgent);
                        //dbContext.Attach(pde);
                        dbContext.Entry(pde).Property(x => x.IsPaidPPH).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHNo).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.BankExpenditureNotePPHDate).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.LastModifiedAgent).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.LastModifiedBy).IsModified = true;
                        dbContext.Entry(pde).Property(x => x.LastModifiedUtc).IsModified = true;
                    }

                    EntityExtension.FlagForDelete(PPHBankExpenditureNote, username, UserAgent);
                    this.dbSet.Update(PPHBankExpenditureNote);
                    Count = await this.dbContext.SaveChangesAsync();

                    //await ReverseJournalTransaction(PPHBankExpenditureNote.No);
                    //await DeleteDailyBankTransaction(PPHBankExpenditureNote.No);

                    transaction.Commit();
                }
                catch (DbUpdateConcurrencyException e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }

            return Count;
        }

        private async Task CreateDailyBankTransaction(PPHBankExpenditureNote model)
        {
            var item = model.Items.FirstOrDefault();
            var spb = dbContext.UnitPaymentOrders.FirstOrDefault(entity => entity.UPONo == item.UnitPaymentOrderNo);

            if (spb == null)
                spb = new UnitPaymentOrder() { SupplierId = "1" };

            int.TryParse(model.BankId, out var bankId);
            long.TryParse(spb.SupplierId, out var supplierId);
            var modelToPost = new DailyBankTransactionViewModel()
            {
                Bank = new ViewModels.NewIntegrationViewModel.AccountBankViewModel()
                {
                    Id = bankId,
                    Code = model.BankCode,
                    AccountName = model.BankAccountName,
                    AccountNumber = model.BankAccountNumber,
                    BankCode = model.BankCode,
                    BankName = model.BankName,
                    Currency = new ViewModels.NewIntegrationViewModel.CurrencyViewModel()
                    {
                        Code = model.Currency,
                    }
                },
                Date = model.Date,
                Nominal = model.TotalIncomeTax,
                CurrencyRate= model.CurrencyRate.GetValueOrDefault(),
                ReferenceNo = model.No,
                ReferenceType = "Bayar PPh",
                //Remark = model.Currency != "IDR" ? $"Pembayaran atas {model.BankCurrencyCode} dengan nominal {string.Format("{0:n}", model.GrandTotal)} dan kurs {model.CurrencyCode}" : "",
                SourceType = "Operasional",
                Status = "OUT",
                Supplier = new NewSupplierViewModel()
                {
                    _id = supplierId,
                    code = spb.SupplierCode,
                    name = spb.SupplierName
                },
                IsPosted = true
            };

            if (model.Currency != "IDR")
                modelToPost.NominalValas = model.TotalIncomeTax * model.CurrencyRate;

            string dailyBankTransactionUri = "daily-bank-transactions";
            //var httpClient = new HttpClientService(identityService);
            var httpClient = (IHttpClientService)this._serviceProvider.GetService(typeof(IHttpClientService));
            var response = await httpClient.PostAsync($"{APIEndpoint.Finance}{dailyBankTransactionUri}", new StringContent(JsonConvert.SerializeObject(modelToPost).ToString(), Encoding.UTF8, General.JsonMediaType));
            response.EnsureSuccessStatusCode();
        }

        //private async Task DeleteDailyBankTransaction(string documentNo)
        //{
        //    string dailyBankTransactionUri = "daily-bank-transactions/by-reference-no/";
        //    //var httpClient = new HttpClientService(identityService);
        //    var httpClient = (IHttpClientService)_serviceProvider.GetService(typeof(IHttpClientService));
        //    var response = await httpClient.DeleteAsync($"{APIEndpoint.Finance}{dailyBankTransactionUri}{documentNo}");
        //    response.EnsureSuccessStatusCode();
        //}

        public async Task<int> Posting(List<long> ids)
        {
            var models = dbContext.PPHBankExpenditureNotes.Include(entity => entity.Items).Where(entity => ids.Contains(entity.Id)).ToList();
            var identityService = _serviceProvider.GetService<IdentityService>();

            foreach (var model in models)
            {
                model.IsPosted = true;
                await AutoCreateJournalTransaction(model);
                await CreateDailyBankTransaction(model);
                EntityExtension.FlagForUpdate(model, identityService.Username, UserAgent);
            }

            dbContext.PPHBankExpenditureNotes.UpdateRange(models);
            return dbContext.SaveChanges();
        }
    }
}
