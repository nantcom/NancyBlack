using NantCom.NancyBlack.Modules.AccountingSystem.Types;
using NantCom.NancyBlack.Modules.CommerceSystem;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AccountingSystem
{
    public class AccountingSystemModule : BaseModule
    {
        static AccountingSystemModule()
        {
            NancyBlackDatabase.ObjectCreated += NancyBlackDatabase_ObjectCreated;
            NancyBlackDatabase.ObjectUpdated += NancyBlackDatabase_ObjectUpdated;
        }

        private static void NancyBlackDatabase_ObjectUpdated(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (table == "SaleOrder")
            {
                AccountingSystemModule.HandleCreditRequest(db, obj);
            }
        }

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (table == "Receipt")
            {
                AccountingSystemModule.ProcessReceiptCreation(db, JObject.FromObject(obj).ToObject<Receipt>());
            }

            if (table == "AccountingEntry")
            {
                AccountingSystemModule.ProcesAutoRecurranceCreation(db, obj);
            }
        }

        private static DateTime TaxSystemEpoch = new DateTime(2016, 12, 1);
        
        internal static void ProcessReceiptCreation(NancyBlackDatabase db, Receipt obj)
        {
            // When payment receipt is created, create accounting entry
            db.Transaction(() =>
            {
                var saleorder = db.GetById<SaleOrder>(obj.SaleOrderId);
                var paymentlog = db.GetById<PaymentLog>(obj.PaymentLogId);

                if (paymentlog.Amount <= 0)
                {
                    return; // perhaps it is an error
                }

                if (saleorder == null || paymentlog == null)
                {
                    // bogus receipt
                    throw new InvalidOperationException("Invalid Receipt was created");
                }

                var currentSite = saleorder.SiteSettings;
                if (currentSite == null)
                {
                    currentSite = AdminModule.ReadSiteSettings();
                }

                // Ensures all sale order logic has been ran
                // if the sale order was created before new system change
                if (saleorder.__createdAt < TaxSystemEpoch)
                {
                    saleorder.UpdateSaleOrder(currentSite, db, false);
                }

                // Receipt will create 4 entries
                // 1) PaymentSource account increases, with amount paid

                // TODO: Mapping from PaymentSource to Account
                AccountingEntry entry1 = new AccountingEntry();
                entry1.TransactionDate = paymentlog.__createdAt;
                entry1.TransactionType = "income";
                entry1.DebtorLoanerName = "Customer";
                entry1.IncreaseAccount = paymentlog.PaymentSource;
                entry1.IncreaseAmount = paymentlog.Amount;
                entry1.SaleOrderId = saleorder.Id;

                db.UpsertRecord(entry1);

                // 2) Sales Tax Calculation
                {

                    AccountingEntry taxExtry = new AccountingEntry();
                    taxExtry.TransactionDate = paymentlog.__createdAt;
                    taxExtry.TransactionType = "taxcredit";
                    taxExtry.DebtorLoanerName = "Tax";
                    taxExtry.DecreaseAccount = "Tax Credit";
                    taxExtry.SaleOrderId = saleorder.Id;

                    if (currentSite.commerce.billing.vattype == "addvat")
                    {
                        var tax = paymentlog.Amount * ((100 + (Decimal)currentSite.commerce.billing.vatpercent) / 100);
                        taxExtry.DecreaseAmount = tax * -1;

                    }

                    if (currentSite.commerce.billing.vattype == "includevat")
                    {
                        var tax = paymentlog.Amount * ((Decimal)currentSite.commerce.billing.vatpercent / 100);
                        taxExtry.DecreaseAmount = tax * -1;
                    }

                    db.UpsertRecord(taxExtry);
                }
                
                // 3) Payment Fee
                if (paymentlog.Fee > 0)
                {
                    AccountingEntry feeEntry = new AccountingEntry();
                    feeEntry.TransactionDate = paymentlog.__createdAt;
                    feeEntry.TransactionType = "buy";
                    feeEntry.DebtorLoanerName = paymentlog.PaymentSource;
                    feeEntry.IncreaseAccount = "Payment Fee - " + paymentlog.PaymentSource;
                    feeEntry.IncreaseAmount = paymentlog.Fee;
                    feeEntry.SaleOrderId = saleorder.Id;

                    db.UpsertRecord(feeEntry);
                }

                // 4) Receivable from the Sale Order
                {
                    // existing receivable of this sale order
                    var existingReceivable = db.Query<AccountingEntry>().Where(e => e.SaleOrderId == saleorder.Id && e.IncreaseAccount == "Receivable").ToList();
                    
                    // see if we have any receivable of this sale order
                    // if we had, we have to deduct it
                    if (existingReceivable.Count > 0)
                    {
                        AccountingEntry deductReceivableEntry = new AccountingEntry();
                        deductReceivableEntry.TransactionDate = paymentlog.__createdAt;
                        deductReceivableEntry.TransactionType = "arpayment";
                        deductReceivableEntry.DebtorLoanerName = "Receivable From Sales";
                        deductReceivableEntry.DecreaseAccount = "Receivable";
                        deductReceivableEntry.DecreaseAmount = paymentlog.Amount;
                        deductReceivableEntry.SaleOrderId = saleorder.Id;

                        db.UpsertRecord(deductReceivableEntry);
                    }
                    else
                    {
                        // this maybe the first payment, see if all amount has been paid

                        // see all payment log of this sale order
                        // we only query payments up to currently processing payment log
                        // so that when we re
                        var payments = db.Query<PaymentLog>().Where(l => l.SaleOrderId == saleorder.Id && l.Id <= paymentlog.Id ).ToList();
                        var remaining = saleorder.TotalAmount - payments.Sum(p => p.Amount);
                        
                        if (remaining > 0)
                        {
                            // this is partial payment - create new receivable

                            AccountingEntry receivableEntry = new AccountingEntry();
                            receivableEntry.TransactionDate = paymentlog.__createdAt;
                            receivableEntry.TransactionType = "newaccount";
                            receivableEntry.DebtorLoanerName = "Receivable From Sales";
                            receivableEntry.IncreaseAccount = "Receivable";
                            receivableEntry.IncreaseAmount = remaining;
                            receivableEntry.SaleOrderId = saleorder.Id;

                            db.UpsertRecord(receivableEntry);
                        }

                        // this is full payment in one go, no need for receivable
                    }
                    
                }

            });
        }

        /// <summary>
        /// Automatically create recurrance
        /// </summary>
        /// <param name="db"></param>
        /// <param name="entry"></param>
        internal static void ProcesAutoRecurranceCreation( NancyBlackDatabase db, AccountingEntry entry )
        {
            if (entry.Addendum == null)
            {
                return;
            }

            if (entry.Addendum.Recurring == true)
            {
                var lastPayment = (DateTime)entry.Addendum.LastPayment;

                var entries = new List<AccountingEntry>();

                var start = entry.DueDate;
                var index = 2;
                while ( start.AddMonths(1) <= lastPayment)
                {
                    start = start.AddMonths(1);

                    var copy = JObject.FromObject(entry).ToObject<AccountingEntry>();
                    copy.TransactionDate = start;
                    copy.DueDate = start;
                    copy.Id = 0;
                    copy.Notes = "Recurring " + index + " of Entry:" + entry.Id + "\r\n" + entry.Notes;
                    copy.Addendum.RecurringMaster = entry.Id;

                    entries.Add(copy);
                    index++;
                }

                db.Connection.InsertAll(entries);
            }
        }

        /// <summary>
        /// Handles the case when sale order is using credit
        /// </summary>
        /// <param name="db"></param>
        /// <param name="so"></param>
        internal static void HandleCreditRequest(NancyBlackDatabase db, SaleOrder saleorder)
        {
            if (saleorder.PaymentStatus != PaymentStatus.Credit)
            {
                return;
            }

            // only create one receivable per sale order
            var existingReceivable = db.Query<AccountingEntry>().Where(e => e.SaleOrderId == saleorder.Id && e.IncreaseAccount == "Receivable").FirstOrDefault();
            if (existingReceivable != null)
            {
                // update amount if changed
                if (existingReceivable.IncreaseAmount != saleorder.TotalAmount)
                {
                    existingReceivable.IncreaseAmount = saleorder.TotalAmount;
                    db.UpsertRecord(existingReceivable);
                }


                return;
            }
            
            AccountingEntry receivableEntry = new AccountingEntry();
            receivableEntry.TransactionDate = DateTime.Now;
            receivableEntry.DueDate = DateTime.Now.Date.AddDays(30);
            receivableEntry.TransactionType = "newaccount";
            receivableEntry.DebtorLoanerName = "Receivable From Sales";
            receivableEntry.IncreaseAccount = "Receivable";
            receivableEntry.IncreaseAmount = saleorder.TotalAmount;
            receivableEntry.SaleOrderId = saleorder.Id;

            db.UpsertRecord(receivableEntry);
        }

        public AccountingSystemModule()
        {
            Get["/admin/tables/accountingentry"] = this.HandleViewRequest("/Admin/accountingsystem-gl", null);

            Get["/admin/accounts"] = this.HandleViewRequest("/Admin/accountingsystem-accounts", (arg) =>
            {
                dynamic model = new System.Dynamic.ExpandoObject();

                /// All Accounts
                {
                    var baseSummary = this.SiteDatabase.QueryAsDynamic(@"
                                            SELECT
                                                SUM(IncreaseAmount) as TotalIncrease,
                                                IncreaseAccount as Account,
                                                MAX(TransactionDate) as LastUpdated,      
                                                0 as DueDate
                                            FROM
                                                AccountingEntry
                                            WHERE IncreaseAccount IS NOT NULL           
                                            GROUP BY IncreaseAccount"
                                            , new { TotalIncrease = 0M, TotalDecrease = 0M, Account = "", LastUpdated = default(DateTime), DueDate = default(DateTime) }).ToList();

                    var decreaseSummary = this.SiteDatabase.QueryAsDynamic(@"
                                            SELECT
                                                SUM(DecreaseAmount) as TotalDecrease,
                                                DecreaseAccount,
                                                MAX(TransactionDate) as LastUpdated,      
                                                MIN(DueDate) as DueDate
                                            FROM
                                                AccountingEntry
                                            WHERE DecreaseAccount IS NOT NULL           
                                            GROUP BY DecreaseAccount"
                                            , new { TotalDecrease = 0M, DecreaseAccount = "", LastUpdated = default(DateTime), DueDate = default(DateTime) }).ToList();

                    var decreaseLookup = decreaseSummary.ToLookup(item => (string)item.DecreaseAccount);

                    foreach (dynamic item in baseSummary)
                    {
                        dynamic decreaseRecord = decreaseLookup[(string)item.Account].FirstOrDefault();
                        if (decreaseRecord == null)
                        {
                            continue;
                        }

                        item.TotalDecrease = decreaseRecord.TotalDecrease;
                        if ((DateTime)decreaseRecord.LastUpdated > (DateTime)item.LastUpdated)
                        {
                            item.LastUpdated = decreaseRecord.LastUpdated;
                        }

                        item.DueDate = decreaseRecord.DueDate;
                    }

                    model.Accounts = baseSummary;

                }

                {
                    var payableExpected = this.SiteDatabase.QueryAsDynamic(@"
                                            SELECT
                                                SUM(IncreaseAmount) as Amount,
                                                'Expected Expense' As DebtorLoanerName,
                                                'Projection' As DocumentNumber,
                                                MAX(TransactionDate) as LatestDate
                                            FROM
                                                AccountingEntry_2017
                                            WHERE
                                                IncreaseAccount = 'Expense'
                                            GROUP BY IncreaseAccount",
                                            new { Amount = 0M, DebtorLoanerName = "", DocumentNumber = "", LastUpdated = default(DateTime), DueDate = default(DateTime) }).FirstOrDefault();


                    var payableSummary = this.SiteDatabase.QueryAsDynamic(@"
                                            SELECT
                                                SUM(DecreaseAmount) as Amount,
                                                DebtorLoanerName,
                                                DocumentNumber,
                                                MAX(TransactionDate) as LastUpdated,      
                                                MIN(DueDate) as DueDate
                                            FROM
                                                AccountingEntry
                                            WHERE
                                                DecreaseAmount < 0
                                                AND DecreaseAccount = 'Payable' 
                                            GROUP BY DebtorLoanerName, DocumentNumber",
                                            new { Amount = 0M, DebtorLoanerName = "", DocumentNumber = "", LastUpdated = default(DateTime), DueDate = default(DateTime) }).ToList();

                    var payablePaidSummary = this.SiteDatabase.QueryAsDynamic(@"
                                            SELECT
                                                SUM(IncreaseAmount) as Amount,
                                                DebtorLoanerName,
                                                DocumentNumber,
                                                MAX(TransactionDate) as LastUpdated,      
                                                MAX(DueDate) as DueDate
                                            FROM
                                                AccountingEntry
                                            WHERE
                                                IncreaseAmount > 0
                                                AND DecreaseAccount = 'Payable' 
                                            GROUP BY DebtorLoanerName, DocumentNumber",
                                           new { Amount = 0M, DebtorLoanerName = "", DocumentNumber = "", LastUpdated = default(DateTime), DueDate = default(DateTime) }).ToLookup(item => (string)item.DebtorLoanerName + "-" + (string)item.DocumentNumber);

                    foreach (var item in payableSummary)
                    {
                        var payback = payablePaidSummary[(string)item.DebtorLoanerName + "-" + (string)item.DocumentNumber];

                        foreach (var row in payback)
                        {
                            item.Amount += row.Amount;
                        }
                    }

                    var final = payableSummary.Where(item => item.Amount < 0).ToList();

                    payableExpected.Amount = (payableExpected.Amount / 12) *  (12 - DateTime.Now.Month) * -1;
                    payableExpected.DueDate = new DateTime(DateTime.Now.Year, 12, 31);

                    final.Add(payableExpected);
                    model.PayableSummary = final;
                }


                {
                    var assetSummary = this.SiteDatabase.QueryAsDynamic(@"
                                                SELECT
                                                    'Stock On Hand' As Name,
                                                    SUM(BuyingPrice) As TotalAmount,
                                                    Max(__updatedAt) As LastUpdated
                                                FROM
                                                    InventoryPurchase
                                                WHERE
                                                    InventoryItemId = 0",

                                                new
                                                {
                                                    Name = "",
                                                    TotalAmount = 0M,
                                                    LastUpdated = default(DateTime)
                                                });
                    
                    var equipmentSummary = this.SiteDatabase.QueryAsDynamic(@"
                                                SELECT
                                                    'Equipment' As Name,
                                                    SUM(IncreaseAmount) As TotalAmount,
                                                    Max(__updatedAt) As LastUpdated
                                                FROM
                                                    AccountingEntry
                                                WHERE
                                                    IncreaseAccount = 'Equipment'",

                                                new
                                                {
                                                    Name = "",
                                                    TotalAmount = 0M,
                                                    LastUpdated = default(DateTime)
                                                });

                    var final = new List<dynamic>();
                    final.AddRange(assetSummary);
                    final.AddRange(equipmentSummary);
                    
                    model.AssetSummary = final;
                }

                {
                    var receivableSummary = this.SiteDatabase.QueryAsDynamic(@"
                                                SELECT  
                                                    SUM(IncreaseAmount) as Amount,
                                                    SUM(DecreaseAmount) as PaidAmount,
                                                    DebtorLoanerName,
                                                    MAX(TransactionDate) as LatestDate,
                                                    SaleOrderId
                                                FROM AccountingEntry
                                                WHERE
                                                    IncreaseAccount == 'Receivable' OR
                                                    DecreaseAccount == 'Receivable'
                                                GROUP BY DebtorLoanerName, SaleOrderId",

                                                new {
                                                    Amount = 0M,
                                                    PaidAmount = 0M,
                                                    DebtorLoanerName = "",
                                                    LatestDate = default(DateTime),
                                                    SaleOrderId = 0 });

                    var final = new List<dynamic>();

                    foreach (var item in receivableSummary)
                    {
                        if (item.Amount == item.PaidAmount)
                        {
                            continue;
                        }

                        final.Add(item);
                    }

                    model.ReceivableSummary = final;
                }


                if (this.CurrentSite.accounting == null)
                {
                    var ja = new JArray();
                    foreach (var item in model.Accounts)
                    {
                        ja.Add(JObject.FromObject(new
                        {
                            Name = item.Account,
                            Type = ""
                        }));
                    }

                    this.CurrentSite.accounting = JObject.FromObject(new { accounts = ja });
                }
                else
                {
                    // Ensures that the settings page contains all accounts used
                    var existingAccounts = (from dynamic acc in this.CurrentSite.accounting.accounts as JArray
                                            select acc).ToDictionary(a => (string)a.Name);

                    foreach (dynamic account in model.Accounts)
                    {
                        if (existingAccounts.ContainsKey((string)account.Account) == false)
                        {
                            var array = this.CurrentSite.accounting.accounts as JArray;
                            array.Add(JObject.FromObject(new
                            {
                                Name = (string)account.Account,
                                Type = ""
                            }));
                        }
                    }
                }


                return new StandardModel(this, null, model);
            });

            Get["/admin/tables/accountingentry/__opendocuments"] = this.HandleRequest((arg) =>
            {
                return this.SiteDatabase.Query("SELECT DISTINCT DocumentNumber as Name FROM AccountingEntry WHERE IsDocumentClosed == false AND DocumentNumber <> Null", new { Name = "" }).Select(item => ((dynamic)item).Name as string).ToList();
            });

            Get["/admin/tables/accountingentry/__autocompletes"] = this.HandleRequest(this.GenerateAutoComplete);

            Get["/admin/tables/accountingentry/__accountsummary"] = this.HandleRequest((arg) =>
            {
                var totalIncrease = this.SiteDatabase.Query(
                                        @"SELECT IncreaseAccount as Account, SUM(IncreaseAmount) as Amount, MAX(TransactionDate) as LatestDate FROM AccountingEntry
                                            WHERE IncreaseAccount IS NOT NULL
                                            GROUP BY IncreaseAccount",
                                        new { Account = "", Amount = 0M, LatestDate = DateTime.Now });

                var totalDecrease = this.SiteDatabase.Query(
                                        @"SELECT DecreaseAccount as Account, SUM(DecreaseAmount) as Amount, MAX(TransactionDate) as LatestDate FROM AccountingEntry
                                                            WHERE DecreaseAccount IS NOT NULL
                                                            GROUP BY DecreaseAccount",
                                        new { Account = "", Amount = 0M, LatestDate = DateTime.Now });

                return new
                {
                    TotalIncrease = totalIncrease,
                    TotalDecrease = totalDecrease
                };

            });

            Get["/admin/tables/accountingentry/__replayreceipt"] = this.HandleRequest((arg) =>
            {
                // replay receipt since 2018 only

                var date = new DateTime(636503616000000000);
                var receipt = this.SiteDatabase.Query<Receipt>().Where(r => r.__createdAt >= date).ToList();

                // Delete all the affected accounting entries
                var query = this.SiteDatabase.Connection.Execute("DELETE FROM AccountingEntry WHERE __createdAt > 636503616000000000 AND SaleOrderId > 0");

                foreach (var item in receipt)
                {
                    AccountingSystemModule.ProcessReceiptCreation(this.SiteDatabase, item);
                }

                return "OK";

            });
        }

        private dynamic GenerateAutoComplete(dynamic args)
        {
            var projects = this.SiteDatabase.Query("SELECT DISTINCT ProjectName AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var debtloan = this.SiteDatabase.Query("SELECT DISTINCT DebtorLoanerName AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var account = this.SiteDatabase.Query("SELECT DISTINCT IncreaseAccount AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var account2 = this.SiteDatabase.Query("SELECT DISTINCT DecreaseAccount AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var supplier = this.SiteDatabase.Query("SELECT Name FROM Supplier", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var documentnumber = this.SiteDatabase.Query("SELECT DISTINCT DocumentNumber as Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();

            return new
            {
                project = projects,
                debtloan = debtloan,
                account = account.Union(account2).ToList(),
                supplier = supplier.Union(debtloan).ToList(),
                documentnumber = documentnumber
            };
        }

        /// <summary>
        /// Get Receivable Account
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static List<string> GetCashAccounts(NancyBlackDatabase db)
        {
            var siteSettings = AdminModule.ReadSiteSettings();
            var accountSettings = (from dynamic item in siteSettings.accounting.accounts as JArray
                                   select new
                                   {
                                       Name = (string)item.Name,
                                       Type = (string)item.Type
                                   }).ToLookup(i => i.Name, i => i.Type);

            return db.QueryAsDynamic("SELECT DISTINCT IncreaseAccount AS Name FROM AccountingEntry", new { Name = "" })
                .AsEnumerable()
                .Select(item => (string)item.Name )
                .Where(s => string.IsNullOrEmpty(s) == false && accountSettings[s].FirstOrDefault() == "Cash")
                .ToList();
        }
    }
}