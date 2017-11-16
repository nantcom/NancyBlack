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
    public class AccountingSystemModule : NancyBlack.Modules.BaseModule
    {
        static AccountingSystemModule()
        {
            NancyBlackDatabase.ObjectCreated += NancyBlackDatabase_ObjectCreated;
        }

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (table == "Receipt")
            {
                AccountingSystemModule.ProcessReceiptCreation(db, obj);
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

                if (saleorder == null || paymentlog == null)
                {
                    // bogus receipt
                    throw new InvalidOperationException("Invalid Receipt was created");
                }

                // Ensures all sale order logic has been ran
                // if the sale order was created before new system change
                if (saleorder.__createdAt < TaxSystemEpoch)
                {
                    saleorder.UpdateSaleOrder(AdminModule.ReadSiteSettings(), db, false);
                }

                // Receipt will create 2 entries
                // 1) PaymentSource account increases, with total amount

                // TODO: Mapping from PaymentSource to Account
                AccountingEntry entry1 = new AccountingEntry();
                entry1.TransactionDate = paymentlog.__createdAt;
                entry1.TransactionType = "income";
                entry1.DebtorLoanerName = "Customer";
                entry1.IncreaseAccount = paymentlog.PaymentSource;
                entry1.IncreaseAmount = saleorder.TotalAmount;
                entry1.SaleOrderId = saleorder.Id;

                db.UpsertRecord(entry1);

                if (saleorder.TotalTax > 0)
                {
                    // 2) paid tax is decreased
                    AccountingEntry entry2 = new AccountingEntry();
                    entry2.TransactionDate = paymentlog.__createdAt;
                    entry2.TransactionType = "taxcredit";
                    entry2.DebtorLoanerName = "Tax";
                    entry2.DecreaseAccount = "Tax Credit";
                    entry2.DecreaseAmount = saleorder.TotalTax * -1;
                    entry2.SaleOrderId = saleorder.Id;

                    db.UpsertRecord(entry2);
                }

            });
        }
        
        public AccountingSystemModule()
        {
            Get["/admin/tables/accountingentry"] = this.HandleViewRequest("/Admin/accountingsystem-gl", null);

            Get["/admin/accounts"] = this.HandleViewRequest("/Admin/accountingsystem-accounts", (arg)=>
            {
                dynamic model = new System.Dynamic.ExpandoObject();

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
                                           new { Amount = 0M, DebtorLoanerName = "", DocumentNumber = "", LastUpdated = default(DateTime), DueDate = default(DateTime) }).ToLookup( item => (string)item.DebtorLoanerName + "-" + (string)item.DocumentNumber);

                    foreach (var item in payableSummary)
                    {
                        var payback = payablePaidSummary[(string)item.DebtorLoanerName + "-" + (string)item.DocumentNumber];

                        foreach (var row in payback)
                        {
                            item.Amount += row.Amount;
                        }
                    }

                    model.PayableSummary = payableSummary.Where( item => item.Amount < 0 ).ToList();
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

                    this.CurrentSite.accounting = JObject.FromObject( new { accounts = ja } );
                }
                

                return new StandardModel(this, null, model);
            });

            Get["/admin/tables/accountingentry/__opendocuments"] = this.HandleRequest((arg)=>
            {
                return this.SiteDatabase.Query("SELECT DISTINCT DocumentNumber as Name FROM AccountingEntry WHERE IsDocumentClosed == false AND DocumentNumber <> Null", new { Name = "" }).Select(item => ((dynamic)item).Name as string).ToList();
            });

            Get["/admin/tables/accountingentry/__autocompletes"] = this.HandleRequest(this.GenerateAutoComplete);
            
            Get["/admin/tables/accountingentry/__accountsummary"] = this.HandleRequest((arg)=>
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
        }
        
        private dynamic GenerateAutoComplete( dynamic args)
        {
            var projects = this.SiteDatabase.Query("SELECT DISTINCT ProjectName AS Name FROM AccountingEntry", new { Name = "" }).Select( item => ((dynamic)item).Name as string ).Where( s => string.IsNullOrEmpty(s) == false ).ToList();
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
        public static List<string> GetReceivableAccounts( NancyBlackDatabase db )
        {
            return db.Query("SELECT DISTINCT IncreaseAccount AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
        }
    }
}