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
        static InventoryAdminModule()
        {
            NancyBlackDatabase.ObjectUpdated += NancyBlackDatabase_ObjectUpdated;
        }

        /// <summary>
        /// Occurred when inventory has finished extracting products from sale order, use this event to
        /// transforms the inventory items such as combining the components into one SKU.
        /// </summary>
        public static event Action<NancyBlackDatabase, SaleOrder, List<InventoryItem>> TransformInventoryRequest = delegate { };

        private static void NancyBlackDatabase_ObjectUpdated(NancyBlackDatabase db, string type, dynamic obj)
        {
            if (type != "SaleOrder")
            {
                return;
            }

            var saleOrder = obj as SaleOrder;

            // only do when status is waiting for order
            if (saleOrder.Status != SaleOrderStatus.WaitingForOrder)
            {
                return;
            }

            // if previous status is already waiting for order - do nothing
            if (db.GetOlderVersions(saleOrder).First().Status == SaleOrderStatus.WaitingForOrder)
            {
                return;
            }

            var now = DateTime.Now;
            var currentSite = AdminModule.ReadSiteSettings();

            // ensures that all logic of sale order has been ran
            saleOrder.UpdateSaleOrder(currentSite, db, false);

            var items = new List<InventoryItem>();
            foreach (var item in saleOrder.ItemsDetail)
            {
                if (item.CurrentPrice < 0) // dont take negative prices (coupon)
                {
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
                }
            }

            // distribute discount into items which has actual sell price
            var discountToDistribute = saleOrder.TotalDiscount / items.Where(item => item.SellingPrice > 0).Count();
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

            InventoryAdminModule.TransformInventoryRequest(db, saleOrder, items);

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

            Get["/admin/tables/inventoryitem/__notfullfilled"] = this.HandleRequest(this.FindNotFullfilled);
            
        }
        
        /// <summary>
        /// List the products that were not fullfilled
        /// </summary>
        /// <returns></returns>
        private IEnumerable<object> FindNotFullfilled(dynamic args)
        {
            var productLookup = this.SiteDatabase.Query<Product>().ToDictionary(p => p.Id);
            
            var notFullfilled = this.SiteDatabase.Query<InventoryItem>()
                                    .Where(ivitm => ivitm.IsFullfilled == false)
                                    .OrderBy(ivitm => ivitm.RequestedDate).ToList();

            return from item in notFullfilled
                   let product = productLookup[item.ProductId]
                   where product.Attributes != null
                   let supplier = JObject.Parse((string)product.Attributes.supplier) as dynamic
                   where supplier != null
                    select new
                    {
                        SupplierId = supplier.id,
                        ProductId = product.Id,
                        InventoryItem = item
                    };
        }
    }
}