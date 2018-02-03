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
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class InventoryItemModule : BaseModule
    {
        /// <summary>
        /// Occurred when inventory has finished extracting products from sale order, use this event to
        /// transforms the inventory items such as combining the components into one SKU.
        /// </summary>
        public static event Action<NancyBlackDatabase, SaleOrder, List<InventoryItem>> TransformInventoryRequest = delegate { };

        static InventoryItemModule()
        {
            NancyBlackDatabase.ObjectUpdated += (db, table, obj) =>
            {
                if (table == "SaleOrder")
                {
                    InventoryItemModule.ProcessSaleOrderUpdate(db, obj);
                }
            };
        }

        private static object LockObject = new object();

        internal static void ProcessSaleOrderUpdate(NancyBlackDatabase db, SaleOrder saleOrder)
        {
            InventoryItemModule.ProcessSaleOrderUpdate(db, saleOrder, false, DateTime.Now);
        }

        /// <summary>
        /// Automatically Fullfil the sale order once the status has beeen set to Building, Testing or Delivered
        /// And Delete if Cancel.
        /// 
        /// This is temporary solution while waiting for actual PO system to be created and Admin can record inventory inbound.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="so"></param>
        internal static void ProcessFulfillSaleOrder(NancyBlackDatabase db, SaleOrder so )
        {
            var requests = db.Query<InventoryItem>().Where(i => i.SaleOrderId == so.Id).ToList();
            if (requests.Count == 0)
            {
                return;
            }

            if (so.ItemsDetail == null)
            {
                return;
            }

            if (so.Status == SaleOrderStatus.Cancel)
            {
                foreach (var req in requests)
                {
                    db.Connection.Delete(req);
                }

                return;
            }

            var productsById = so.ItemsDetail.ToLookup(p => p.Id);
            var autoFulfill = so.Status == SaleOrderStatus.Delivered ||
                    so.Status == SaleOrderStatus.Shipped ||
                    so.Status == SaleOrderStatus.ReadyToShip;
                        
            if (autoFulfill) // this is old sale order
            {
                // get the average cost
                IEnumerable<dynamic> priceListResult = db.Query("SELECT ProductId, AVG(BuyingPrice) as AvgCost FROM InventoryPurchase GROUP BY ProductId",
                                                        new
                                                        {
                                                            ProductId = 1,
                                                            AvgCost = 0M
                                                        });

                var priceList = priceListResult.ToLookup(i => i.ProductId);

                foreach (var req in requests)
                {
                    if (req.IsFullfilled == false)
                    {
                        req.IsFullfilled = true;
                        req.FulfilledDate = so.__updatedAt;
                    }

                    if (req.BuyingCost == 0) // no price, try to re-run
                    {
                        var price = priceList[req.ProductId].FirstOrDefault();
                        if (price != null)
                        {
                            req.BuyingCost = (Decimal)price.AvgCost;
                        }
                    }

                    req.__updatedAt = so.__updatedAt;
                    db.Connection.Update(req);
                }

                return;
            }


            // we try to match the inventorypurchase into this request
            // ensure that only this thread is running
            lock ("InventoryPurchase")
            {
                // In memory cache of the inventory
                var availableInventory = db.Query<InventoryPurchase>()
                                            .Where(i => i.InventoryItemId == 0)
                                            .ToLookup(i => i.ProductId);
                
                foreach (var req in requests)
                {
                    if (req.IsFullfilled == true)
                    {
                        continue;
                    }
                    
                    var available = availableInventory[req.ProductId].FirstOrDefault( i => i.InventoryItemId == 0 );
                    if (available == null)
                    {
                        // this was fulfilled but we dont have inventory available
                        // probably it was processed before inventory purcahse system
                        continue;
                    }

                    req.BuyingCost = available.BuyingPrice;
                    req.BuyingTax = available.BuyingTax;
                    req.InventoryPurchaseId = available.Id;
                    req.IsFullfilled = true;
                    available.InventoryItemId = req.Id;

                    db.Connection.RunInTransaction(() =>
                    {
                        db.Connection.Update(req);
                        db.Connection.Update(available);
                    });
                }
            }
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

                // fulfill the requests of this sale order
                InventoryItemModule.ProcessFulfillSaleOrder(db, saleOrder);

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
                    // virtual products does not need inventory inbound
                    if (item.Attributes.IsVirtual == 1)
                    {
                        continue;
                    }

                    var ivitm = new InventoryItem()
                    {
                        SaleOrderId = saleOrder.Id,
                        ProductId = item.Id,
                        RequestedDate = now,
                        IsFullfilled = false,
                        QuotedPrice = item.CurrentPrice,
                        SellingPrice = item.CurrentPrice,
                    };
                    items.Add(ivitm);

                    if (item.CurrentPrice != item.Price)
                    {
                        totalDiscount += item.Price - item.CurrentPrice;
                    }
                }
            }

            // distribute discount into items which has actual sell price
            var totalTrueItems = items.Where(item => item.QuotedPrice > 0).Count();

            var discountRemaining = totalDiscount;
            while (discountRemaining > 0)
            {
                foreach (var item in items)
                {
                    if (item.SellingPrice > 0)
                    {
                        // discount by 1% 
                        var discount = item.SellingPrice * 0.01M;

                        if (discountRemaining - discount < 0)
                        {
                            discount = discountRemaining;
                        }
                        discountRemaining -= discount;
                        item.SellingPrice = item.SellingPrice - discount;

                        if (discountRemaining == 0)
                        {
                            break;
                        }
                    }
                }
            }

            foreach (var item in items)
            {
                item.__updatedAt = DateTime.Now;
                item.__createdAt = DateTime.Now;

                if (item.SellingPrice > 0)
                {
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


            InventoryItemModule.TransformInventoryRequest(db, saleOrder, items);

            // Remove items with selling price 0 or less than 0
            // that is not a real product
            items = (from item in items
                     where item.SellingPrice > 0
                     select item).ToList();

            // attempt to merge the old list and new list using product id
            // by seeing if there is any item that was already fulfilled - if 
            // there is any - copy the information into new list
            var existing = db.Query<InventoryItem>().Where(ivt => ivt.SaleOrderId == saleOrder.Id).ToLookup(ivt => ivt.ProductId);
            var newList = items.ToLookup(ivt => ivt.ProductId);

            foreach (var group in existing)
            {
                var existingGroup = existing[group.Key].ToList();
                var newGroup = group.ToList();

                for (int i = 0; i < existingGroup.Count; i++)
                {
                    if ( i >= newGroup.Count)
                    {
                        // old list has more items - keep them if it is already fulfilled
                        if (existingGroup[i].IsFullfilled)
                        {
                            existingGroup[i].Note = "This sale order have less items, this is an orphaned row";
                            db.Connection.Update(existingGroup[i]);
                        }
                        else
                        {
                            // otherwise, just deletes them
                            db.Connection.Delete<InventoryItem>(existingGroup[i].Id);
                        }

                        continue;
                    }

                    if (existingGroup[i].IsFullfilled)
                    {
                        newGroup[i].IsFullfilled = true;
                        newGroup[i].SerialNumber = existingGroup[i].SerialNumber;
                        newGroup[i].FulfilledDate = existingGroup[i].FulfilledDate;
                    }

                    db.Connection.Delete<InventoryItem>(existingGroup[i].Id);
                }
            }

            db.Connection.InsertAll(items);
        }

        public InventoryItemModule()
        {
            this.RequiresClaims("admin");

            // insert updatedStock here
            Get["/admin/tables/inventoryitem"] = this.HandleViewRequest("/Admin/commerceadmin-inventory");
            Get["/admin/tables/inventoryitem/__averageprice"] = this.HandleRequest((arg) =>
            {
                return this.SiteDatabase.Query("SELECT ProductId, AVG(BuyingPrice) as Price FROM InventoryPurchase WHERE BuyingPrice > 0 GROUP BY ProductId", new { ProductId = 0, Price = 0.0 });
            });
            
        }
        
        /// <summary>
        /// List the products that were not fullfilled
        /// </summary>
        /// <returns></returns>
        private IEnumerable<object> GetWaitingForOrderGroupBySupplier(dynamic args)
        {
            var productLookup = this.SiteDatabase.Query<Product>().ToLookup(p => p.Id);

            var notFullfilled = this.SiteDatabase.Query<InventoryItem>()
                                    .Where(ivitm => ivitm.IsFullfilled == false)
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
        
    }
}