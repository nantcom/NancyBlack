using NantCom.NancyBlack.Modules.AccountingSystem.Types;
using NantCom.NancyBlack.Modules.CommerceSystem;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
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
            InventoryAdminModule.InboundCompleted += ProcessInboundCompleted;
        }

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (table == "InventoryInbound")
            {
                AccountingSystemModule.ProcessInventoryInboundCreation(db, obj);
            }

            if (table == "Receipt")
            {
                AccountingSystemModule.ProcessReceiptCreation(db, obj);
            }
        }

        private static DateTime TaxSystemEpoch = new DateTime(2016, 12, 1);
        
        private static void ProcessReceiptCreation(NancyBlackDatabase db, Receipt obj)
        {
            // When payment receipt is created, create accounting entry
            db.Transaction(() =>
            {
                var saleorder = db.GetById<SaleOrder>(obj.SaleOrderId);
                var paymentlog = db.GetById<PaymentLog>(obj.PaymentLogId);

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
                entry1.TransactionDate = DateTime.Now;
                entry1.TransactionType = "income";
                entry1.DebtorLoanerName = "Customer";
                entry1.IncreaseAccount = paymentlog.PaymentSource;
                entry1.IncreaseAmount = saleorder.TotalAmount;
                entry1.SaleOrderId = saleorder.Id;

                db.UpsertRecord(entry1);

                if (saleorder.TotalTax == 0)
                {
                    return;
                }

                // 2) paid tax is decreased
                // (ภาษีขาย ทำให้ภาษีซื้อลดลง, ภาษีซื้อ บันทึกไว้ตอน InventoryInbound)
                AccountingEntry entry2 = new AccountingEntry();
                entry2.TransactionDate = DateTime.Now;
                entry2.TransactionType = "expense";
                entry2.DebtorLoanerName = "Tax";
                entry2.DecreaseAccount = "Paid Tax";
                entry2.DecreaseAmount = saleorder.TotalTax * -1;
                entry2.SaleOrderId = saleorder.Id;

                db.UpsertRecord(entry2);
            });
        }

        private static void ProcessInventoryInboundCreation(NancyBlackDatabase db, InventoryInbound obj)
        {
            // When inventory inbound is created, record into GL about current asset
            db.Transaction(() =>
            {
                var supplierLookup = db.Query<Supplier>().ToDictionary(s => s.Id);

                // Inbound will create 2 entries
                // 1) inventory increase and account decrease (without tax amount)

                AccountingEntry entry1 = new AccountingEntry();
                entry1.TransactionDate = DateTime.Now;
                entry1.TransactionType = "buy";
                entry1.DebtorLoanerName = supplierLookup[obj.SupplierId].Name;
                entry1.IncreaseAccount = "Inventory";
                entry1.IncreaseAmount = obj.TotalAmountWithoutTax;
                entry1.DecreaseAccount = obj.PaymentAccount;
                entry1.DecreaseAmount = obj.TotalAmountWithoutTax * -1;
                entry1.InventoryInboundId = obj.Id;

                db.UpsertRecord(entry1);

                // 2) paid tax increase and account decrease (tax only amount)
                // (ภาษีซื้อทำให้ภาษีขายที่ต้องจ่ายลดลง)
                if (obj.TotalTax == 0)
                {
                    return;
                }

                AccountingEntry entry2 = new AccountingEntry();
                entry2.TransactionDate = DateTime.Now;
                entry2.TransactionType = "expense";
                entry2.DebtorLoanerName = "Tax";
                entry2.IncreaseAccount = "Paid Tax";
                entry2.IncreaseAmount = obj.TotalTax;
                entry2.DecreaseAccount = obj.PaymentAccount;
                entry2.DecreaseAmount = obj.TotalTax * -1;
                entry2.InventoryInboundId = obj.Id;

                db.UpsertRecord(entry2);
            });
        }
        
        private static void ProcessInboundCompleted(NancyBlackDatabase db, InventoryInbound inbound, List<InventoryItem> items)
        {
            // this already in transaction
            var allFullfilled = from item in items
                                where item.IsFullfilled == true
                                select item;

            // the inventory is withdrawn as expense
            AccountingEntry entry1 = new AccountingEntry();
            entry1.TransactionDate = DateTime.Now;
            entry1.TransactionType = "expense";
            entry1.DebtorLoanerName = "Inventory Used";
            entry1.DecreaseAccount = "Inventory";
            entry1.DecreaseAmount = allFullfilled.Sum( item => item.BuyingCost ) * -1;
            entry1.Notes = "Inventory Used by Sale Order: " + string.Join(",", allFullfilled.Select(item => item.SaleOrderId)) +
                           "From Inbound Id:" + inbound.Id;

            db.UpsertRecord(entry1);

            // if there is net profit/loss - record it
            // but does not remove the amount from account
            var totalAmountBuy = allFullfilled.Sum(i => i.BuyingCost);
            var totalAmountSold = allFullfilled.Sum(i => i.SellingPrice);

            if (totalAmountBuy != totalAmountSold)
            {
                AccountingEntry entry2 = new AccountingEntry();
                entry2.TransactionDate = DateTime.Now;
                entry2.TransactionType = "income";
                entry2.DebtorLoanerName = "n/a";
                entry2.IncreaseAccount = "Gross Profit - " + inbound.PaymentAccount;
                entry2.IncreaseAmount = totalAmountSold - totalAmountBuy;
                entry2.Notes = "From Inbound Id:" + inbound.Id + " the item were used. Profit/Loss is calculated and recorded into Profit(Loss) account for each account";

                db.UpsertRecord(entry2);
            }
        }


        public AccountingSystemModule()
        {
            Get["/admin/tables/accountingentry"] = this.HandleViewRequest("/Admin/accountingsystem-gl", null);

            Get["/admin/tables/accountingentry/__autocompletes"] = this.HandleRequest(this.GenerateAutoComplete);

            Get["/admin/tables/accountingentry/__replaysaleorder"] = this.HandleRequest(this.ReplySaleorder);

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

        /// <summary>
        /// Replay Sale Order
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private dynamic ReplySaleorder( dynamic args )
        {
            return "OK";
        }

        private dynamic GenerateAutoComplete( dynamic args)
        {
            var projects = this.SiteDatabase.Query("SELECT DISTINCT ProjectName AS Name FROM AccountingEntry", new { Name = "" }).Select( item => ((dynamic)item).Name as string ).Where( s => string.IsNullOrEmpty(s) == false ).ToList();
            var debtloan = this.SiteDatabase.Query("SELECT DISTINCT DebtorLoanerName AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var account = this.SiteDatabase.Query("SELECT DISTINCT IncreaseAccount AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var account2 = this.SiteDatabase.Query("SELECT DISTINCT DecreaseAccount AS Name FROM AccountingEntry", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();
            var supplier = this.SiteDatabase.Query("SELECT Name FROM Supplier", new { Name = "" }).Select(item => ((dynamic)item).Name as string).Where(s => string.IsNullOrEmpty(s) == false).ToList();

            return new
            {
                project = projects,
                debtloan = debtloan,
                account = account.Union(account2).ToList(),
                supplier = supplier.Union(debtloan).ToList()
            };
        }
    }
}