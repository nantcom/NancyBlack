using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json.Linq;
using Nancy.Security;
using Nancy.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class InventoryPurchaseAdminModule : BaseModule
    {
        
        public InventoryPurchaseAdminModule()
        {
            this.RequiresClaims("admin");
            
            Get["/admin/tables/inventorypurchase"] = this.HandleViewRequest("/Admin/commerceadmin-inventorypurchase");
            
            Get["/admin/tables/inventorypurchase/__waitingforinbound"] = this.HandleRequest(this.GetWaitingForInbound);

            Get["/admin/tables/inventorypurchase/__waitinginvoices"] = this.HandleRequest(this.GetWaitingInvoices);

            Post["/admin/tables/inventorypurchase/__submitinvoice"] = this.HandleRequest(this.SubmitInvoice);

            Post["/admin/tables/inventorypurchase/__inbound"] = this.HandleRequest(this.SubmitInbound);
        }

        private dynamic SubmitInvoice( dynamic args )
        {
            var invoice = args.body.Value;

            var head = (invoice as JObject).ToObject<InventoryPurchase>();

            var exists = this.SiteDatabase.Query<InventoryPurchase>()
                            .Where(i => i.SupplierId == head.SupplierId &&
                                   i.SupplierInvoiceNumber == head.SupplierInvoiceNumber).Count();

            if (exists > 0)
            {
                throw new InvalidOperationException("Invoice already submitted");
            }

            int tax = this.CurrentSite.commerce.billing.vatpercent;

            var toInsert = new List<InventoryPurchase>();
            var now = DateTime.Now;
            
            foreach (var item in invoice.Items as JArray)
            {
                var purchase = item.ToObject<InventoryPurchase>();

                purchase.SupplierId = head.SupplierId;
                purchase.SupplierInvoiceNumber = head.SupplierInvoiceNumber;
                purchase.PaidByAccount = head.PaidByAccount;

                if (invoice.IsPriceIncludeVat == true)
                {
                    var noTaxAmount = purchase.BuyingPrice * 100 / (100M + tax);
                    var taxAmount = purchase.BuyingPrice - noTaxAmount;
                    purchase.BuyingTax = taxAmount;
                    purchase.BuyingPrice = noTaxAmount;
                }
                else
                {

                    var taxAmount = purchase.BuyingPrice * (tax / 100M);
                    purchase.BuyingTax = taxAmount;
                }

                purchase.__createdAt = now;
                purchase.__updatedAt = now;
                purchase.PurchasedDate = head.PurchasedDate;
                purchase.ProjectedReceiveDate = head.ProjectedReceiveDate;

                // automatically set to inbound if received date is passed
                // or it is today
                if (purchase.ProjectedReceiveDate.Date <= now.Date)
                {
                    purchase.IsInBound = true;
                }

                var qty = item.Value<int>("Qty");
                for (int i = 0; i < qty; i++)
                {
                    var copy = JObject.FromObject(purchase).ToObject<InventoryPurchase>();
                    toInsert.Add(copy);
                }

            }

            this.SiteDatabase.Connection.InsertAll(toInsert);

            return toInsert;
        }
        
        /// <summary>
        /// List the products that is not yet inbound
        /// </summary>
        /// <returns></returns>
        private IEnumerable<object> GetWaitingForInbound(dynamic args)
        {
            return this.SiteDatabase.Query<InventoryPurchase>()
                                    .Where(i => i.ActualReceiveDate != DateTime.MinValue)
                                    .OrderBy(i => i.__createdAt);
        }

        /// <summary>
        /// List the invoice numbers that is not already inbound
        /// </summary>
        /// <returns></returns>
        private IEnumerable<object> GetWaitingInvoices(dynamic args)
        {
            return this.SiteDatabase.Query(
                @"SELECT SupplierInvoiceNumber FROM
                InventoryPurchase
                WHERE IsInbound = 0
                GROUP BY SupplierInvoiceNumber", new { SupplierInvoiceNumber = "" });
        }

        /// <summary>
        /// Submit Inbound
        /// </summary>
        /// <returns></returns>
        private dynamic SubmitInbound(dynamic args)
        {
            return 200;
        }
    }
}