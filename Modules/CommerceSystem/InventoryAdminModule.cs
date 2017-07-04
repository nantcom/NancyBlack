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
    public class InventoryAdminModule : BaseModule
    {
        /// <summary>
        /// Occurred when inventory has finished extracting products from sale order, use this event to
        /// transforms the inventory items such as combining the components into one SKU.
        /// </summary>
        public static event Action<NancyBlackDatabase, SaleOrder, List<InventoryItem>> TransformInventoryRequest = delegate { };

        /// <summary>
        /// Occured when inbound processing is completed. This will be run in transaction along with inbound processing
        /// and will revert if exception occured
        /// </summary>
        public static event Action<NancyBlackDatabase, InventoryInbound, List<InventoryItem>> InboundCompleted = delegate { };

        static InventoryAdminModule()
        {
            NancyBlackDatabase.ObjectUpdated += (db, table, obj) =>
            {
                if (table == "SaleOrder")
                {
                    InventoryAdminModule.ProcessSaleOrderUpdate(db, obj);
                }
            };
            NancyBlackDatabase.ObjectCreated += (db, table, obj) =>
            {
                if (table == "InventoryInbound")
                {
                    InventoryAdminModule.ProcessInventoryInboundCreation(db, obj);
                }
            };
        }

        private static object LockObject = new object();

        /// <summary>
        /// When inventory inbound is created, find the inventory item that needs to be fullfilled
        /// and fullfil with item from inventory inbound
        /// </summary>
        /// <param name="db"></param>
        /// <param name="obj"></param>
        internal static void ProcessInventoryInboundCreation(NancyBlackDatabase db, InventoryInbound obj)
        {
            // ensures that only one thread will be doing this
            lock (InventoryAdminModule.LockObject)
            {
                db.Transaction(() =>
                {
                    var inboundItems = new List<InventoryItem>();

                    foreach (var item in obj.Items)
                    {
                        InventoryItem ivitm = new InventoryItem();
                        ivitm.InboundDate = obj.InboundDate;
                        ivitm.InventoryInboundId = obj.Id;
                        ivitm.ProductId = item.ProductId;
                        ivitm.BuyingCost = item.Price;
                        ivitm.BuyingTax = item.Tax;

                        db.UpsertRecord(ivitm);

                        inboundItems.Add(ivitm);
                    }

                    InventoryAdminModule.InboundCompleted(db, obj, inboundItems);
                });

            }
        }


        internal static void ProcessSaleOrderUpdate(NancyBlackDatabase db, SaleOrder saleOrder)
        {
            InventoryAdminModule.ProcessSaleOrderUpdate(db, saleOrder, false, DateTime.Now);
        }

        /// <summary>
        /// When Saleorder is set to waiting for order, generate InventoryItem for each item in the sale order
        /// TransformInventoryRequest event is called to finalize the list of items.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="saleOrder"></param>
        internal static void ProcessSaleOrderUpdate(NancyBlackDatabase db, SaleOrder saleOrder, bool replay, DateTime now)
        {
            if (replay == false)
            {
                now = DateTime.Now;

                if (saleOrder.Status == "Cancel")
                {
                    //TODO: Cancel Case

                    return;
                }

                // only do when status is waiting for order
                if (saleOrder.Status != SaleOrderStatus.WaitingForOrder)
                {
                    return;
                }

                // if previous status is already waiting for order - do nothing
                if (db.GetOlderVersions(saleOrder).Any( s => s.Status == SaleOrderStatus.WaitingForOrder))
                {
                    return;
                }
            }

            var currentSite = AdminModule.ReadSiteSettings();

            // NOTE: We can't run it again since it can alter the amount
            // it is possible that admin may change amount in database
            //// ensures that all logic of sale order has been ran
            //saleOrder.UpdateSaleOrder(currentSite, db, false);

            // ensure that no inventory inbound can be run
            var totalDiscount = 0M;

            var items = new List<InventoryItem>();
            foreach (var item in saleOrder.ItemsDetail)
            {
                if (item.CurrentPrice < 0) // dont take negative prices (coupon)
                {
                    totalDiscount += item.CurrentPrice * -1; // record the discount
                    continue;
                }

                // For each items in sale order, create an inventory item
                for (int i = 0; i < (int)item.Attributes.Qty; i++)
                {

                    var ivitm = new InventoryItem()
                    {
                        SaleOrderId = saleOrder.Id,
                        ProductId = item.Id,
                        RequestedDate = now,
                        IsFullfilled = false,
                        SellingPrice = item.CurrentPrice
                    };
                    items.Add(ivitm);

                    if (item.CurrentPrice != item.Price)
                    {
                        totalDiscount += item.Price - item.CurrentPrice;
                    }
                }
            }

            // distribute discount into items which has actual sell price
            var discountToDistribute = totalDiscount / items.Where(item => item.SellingPrice > 0).Count();

            // discount is too great for some item, add it to the most expensive one
            if (items.Where(item => discountToDistribute > item.SellingPrice).Count() > 0)
            {
                var item = items.OrderByDescending(i => i.SellingPrice).First();

                item.SellingPrice -= totalDiscount;

                if (currentSite.commerce.billing.vattype == "addvat")
                {
                    item.SellingTax = item.SellingPrice * (100 + (int)currentSite.commerce.billing.vatpercent) / 100;
                }

                if (currentSite.commerce.billing.vattype == "includevat")
                {
                    var priceWithoutTax = item.SellingPrice * 100 / (100 + (int)currentSite.commerce.billing.vatpercent);
                    item.SellingTax = item.SellingPrice - priceWithoutTax;
                    item.SellingPrice = priceWithoutTax;
                }
            }
            else // distribute it to items
            {
                foreach (var item in items)
                {
                    if (item.SellingPrice > 0)
                    {
                        item.SellingPrice -= discountToDistribute;

                        if (currentSite.commerce.billing.vattype == "addvat")
                        {
                            item.SellingTax = item.SellingPrice * (100 + (int)currentSite.commerce.billing.vatpercent) / 100;
                        }

                        if (currentSite.commerce.billing.vattype == "includevat")
                        {
                            var priceWithoutTax = item.SellingPrice * 100 / (100 + (int)currentSite.commerce.billing.vatpercent);
                            item.SellingTax = item.SellingPrice - priceWithoutTax;
                            item.SellingPrice = priceWithoutTax;
                        }
                    }
                }
            }

            InventoryAdminModule.TransformInventoryRequest(db, saleOrder, items);

            // Remove items with selling price 0 or less than 0
            // that is not a real product
            items = (from item in items
                     where item.SellingPrice > 0
                     select item).ToList();

            db.Transaction(() =>
            {
                // before inserting...
                // if the inventory item for this sale order already fullfilled
                // it will remain in inventory but sale order removed

                // we will always create new inventory item for this sale order
                // and clear out old ones

                foreach (var item in db.Query<InventoryItem>().Where(ivt => ivt.SaleOrderId == saleOrder.Id).ToList())
                {
                    if (item.IsFullfilled)
                    {
                        item.Note = "Sale Order Id was removed because sale order which created this item has status set to WaitingForOrder Again";
                        item.SaleOrderId = 0;
                        item.IsFullfilled = false;
                        db.UpsertRecord(item);
                        continue; // item already fullfilled, we leave it but remove sale order id
                        }

                    db.DeleteRecord(item);
                }

                foreach (var item in items)
                {
                    db.UpsertRecord(item);
                }
            });


        }

        public InventoryAdminModule()
        {
            this.RequiresClaims("admin");

            // insert updatedStock here
            Get["/admin/tables/inventoryitem"] = this.HandleViewRequest("/Admin/commerceadmin-inventory");

            Get["/admin/tables/inventoryitem/__notfullfilled"] = this.HandleRequest(this.GetWaitingForOrder);

            Get["/admin/tables/inventoryitem/__waitingforinbound"] = this.HandleRequest(this.GetWaitingForInbound);

            Get["/admin/tables/inventoryitem/__instock"] = this.HandleRequest((arg) =>
            {
                return this.SiteDatabase.Query("SELECT ProductId, SUM(BuyingCost) as Price, SUM(1) as Qty FROM InventoryItem WHERE IsFullfilled = 0 AND InboundDate > 0 AND SaleOrderId = 0 GROUP BY ProductId", new { ProductId = 0, Price = 0.0, Qty = 0 });
            });

            Get["/admin/tables/inventoryitem/__averageprice"] = this.HandleRequest((arg) =>
            {
                return this.SiteDatabase.Query("SELECT ProductId, AVG(BuyingCost) as Price FROM InventoryItem WHERE BuyingCost > 0 GROUP BY ProductId", new { ProductId = 0, Price = 0.0 });
            });


        }

        /// <summary>
        /// List the products that were not fullfilled
        /// </summary>
        /// <returns></returns>
        private IEnumerable<object> GetWaitingForOrder(dynamic args)
        {
            var productLookup = this.SiteDatabase.Query<Product>().ToLookup(p => p.Id);

            var notFullfilled = this.SiteDatabase.Query<InventoryItem>()
                                    .Where(ivitm => ivitm.IsFullfilled == false && ivitm.InboundDate == DateTime.MinValue)
                                    .OrderBy(ivitm => ivitm.RequestedDate).ToList();
            
            return from item in notFullfilled
                   let product = productLookup[item.ProductId].FirstOrDefault()
                   where product != null
                   select new
                   {
                       SupplierId = product.SupplierId,
                       ProductId = product.Id,
                       InventoryItem = item
                   };
        }

        /// <summary>
        /// List the products that is not yet inbound
        /// </summary>
        /// <returns></returns>
        private IEnumerable<object> GetWaitingForInbound(dynamic args)
        {
            var productLookup = this.SiteDatabase.Query<Product>().ToDictionary(p => p.Id);

            var notFullfilled = this.SiteDatabase.Query<InventoryItem>()
                                    .Where(ivitm => ivitm.InboundDate >= DateTime.Now.Date)
                                    .OrderBy(ivitm => ivitm.RequestedDate).ToList();

            return from item in notFullfilled
                   let product = productLookup[item.ProductId]
                   where product.Attributes != null
                   select new
                   {
                       SupplierId = product.SupplierId,
                       ProductId = product.Id,
                       InventoryItem = item
                   };
        }
    }
}