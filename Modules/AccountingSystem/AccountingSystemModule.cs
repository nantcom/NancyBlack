using NantCom.NancyBlack.Modules.AccountingSystem.Types;
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
        }

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (table == "InventoryInbound")
            {
                AccountingSystemModule.ProcessInventoryInboundCreation(db, obj);
            }
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

        public AccountingSystemModule()
        {
            Get["/admin/tables/accountingentry"] = this.HandleViewRequest("/Admin/accountingsystem-gl", null);

            Get["/admin/tables/accountingentry/__autocompletes"] = this.HandleRequest(this.GenerateAutoComplete);

            Get["/admin/tables/accountingentry/__replaysaleorder"] = this.HandleRequest(this.ReplySaleorder);

            // TODO: Merge logic from client side 
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