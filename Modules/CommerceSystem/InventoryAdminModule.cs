using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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

            // ensures that all logic of sale order has been ran
            saleOrder.UpdateSaleOrder(null, db, false);

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
                    items.Add(new InventoryItem()
                    {
                        SaleOrderId = saleOrder.Id,
                        ProductId = item.Id,
                        RequestedDate = now,
                        IsFullfilled = false,
                        SellingPrice = item.CurrentPrice
                    });
                }
            }

            // distribute discount into items which has actual sell price
            var discountToDistribute = saleOrder.TotalDiscount / items.Where( item => item.SellingPrice > 0 ).Count();
            foreach (var item in items)
            {
                if (item.SellingPrice > 0)
                {
                    item.SellingPrice -= discountToDistribute;
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

                foreach (var item in db.Query<InventoryItem>().Where( ivt => ivt.SaleOrderId == saleOrder.Id ).ToList())
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
            // insert updatedStock here
            Get["/admin/tables/inventorymovement"] = this.HandleRequest(OpenInventoryManager);

            Get["/admin/inventory/api/stock"] = this.HandleRequest(GetStock);

            #region Quick Actions

            Post["/admin/commerce/api/copystock"] = this.HandleRequest(this.CopyStock);

            #endregion
        }

        private dynamic OpenInventoryManager(dynamic arg)
        {
            return 200;

            //if (!this.CurrentUser.HasClaim("admin"))
            //{
            //    return 403;
            //}

            //var unsummarizedStock = InventorySummary.GetUnsummarizedStocks(this.SiteDatabase);

            //// Update stock if last month stock never been update yet
            //if (unsummarizedStock != null)
            //{
            //    foreach (var summary in InventorySummary.GetUnsummarizedStocks(this.SiteDatabase))
            //    {
            //        this.SiteDatabase.UpsertRecord(summary);
            //    }
            //}


            //var currentStocks = InventorySummary.GetStocks(DateTime.Now, this.SiteDatabase).ToList();

            //var dummyPage = new Page();

            //var data = new
            //{
            //    CurrentStocks = currentStocks
            //};

            //return View["/Admin/Inventorymanager", new StandardModel(this, dummyPage, data)];
        }

        private dynamic GetStock(dynamic arg)
        {
            return 200;

            //if (!this.CurrentUser.HasClaim("admin"))
            //{
            //    return 403;
            //}

            //var param = ((JObject)arg.body.Value);
            //var checkingDate = param.Value<DateTime>("checkingDate").ToLocalTime();

            //return InventorySummary.GetStocks(checkingDate, this.SiteDatabase).ToList();
        }
        
        private dynamic CopyStock(dynamic arg)
        {
            //TableSecModule.ThrowIfNoPermission(this.Context, "Product", TableSecModule.PERMISSON_UPDATE);

            //this.SiteDatabase.Transaction(() =>
            //{
            //    foreach (var item in this.SiteDatabase.Query<Product>()
            //                            .Where(p => p.IsVariation == true)
            //                            .AsEnumerable())
            //    {

            //        var movements = this.SiteDatabase.Query<InventoryMovement>()
            //                            .Where(mv => mv.ProductId == item.Id)
            //                            .ToList();

            //        item.Stock = movements.Sum(mv => mv.InboundAmount);
            //        this.SiteDatabase.UpsertRecord<Product>(item);
            //    }
            //});

            return 200;
        }
    }
}