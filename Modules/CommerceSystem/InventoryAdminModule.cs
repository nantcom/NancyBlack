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
            NancyBlackDatabase.ObjectUpdated += CreateInventoryMovement_WhenPurchaseOrderStatusUpdated;
            NancyBlackDatabase.ObjectUpdated += CreateInventoryMovement_WhenSaleOrderStatusUpdated;
        }

        private static void CreateInventoryMovement_WhenPurchaseOrderStatusUpdated(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (table != "PurchaseOrder")
            {
                return;
            }

            PurchaseOrder purchaseOrder = obj;
            // only Received status can create inventory movement
            if (purchaseOrder.Status != PurchaseOrderStatus.Received)
            {
                return;
            }

            var hasBeenusedForMovement = db.Query<InventoryMovement>()
                .Where(im => im.PurchaseOrderId == purchaseOrder.Id).FirstOrDefault() != null;
            // no record match mean this item never been used 
            // for create movement for prevent duplication
            if (hasBeenusedForMovement)
            {
                return;
            }

            var newInventoryMovements = InventoryMovement.Create(purchaseOrder);
            foreach (var imovement in newInventoryMovements)
            {
                db.UpsertRecord(imovement);
            }
        }

        private static void CreateInventoryMovement_WhenSaleOrderStatusUpdated(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (table != "SaleOrder")
            {
                return;
            }

            SaleOrder saleOrder = obj;
            if (saleOrder.Status != SaleOrderStatus.Packing &&
                saleOrder.Status != SaleOrderStatus.ReadyToShip &&
                saleOrder.Status != SaleOrderStatus.Delivered)
            {
                return;
            }

            var hasBeenusedForMovement = db.Query<InventoryMovement>()
                .Where(im => im.SaleOrderId == saleOrder.Id).FirstOrDefault() != null;
            // no record match mean this item never been used 
            // for create movement for prevent duplication
            if (hasBeenusedForMovement)
            {
                return;
            }

            var newInventoryMovements = InventoryMovement.Create(saleOrder);
            foreach (var imovement in newInventoryMovements)
            {
                db.UpsertRecord(imovement);
            }
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
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var unsummarizedStock = InventorySummary.GetUnsummarizedStocks(this.SiteDatabase);

            // Update stock if last month stock never been update yet
            if (unsummarizedStock != null)
            {
                foreach (var summary in InventorySummary.GetUnsummarizedStocks(this.SiteDatabase))
                {
                    this.SiteDatabase.UpsertRecord(summary);
                }
            }


            var currentStocks = InventorySummary.GetStocks(DateTime.Now, this.SiteDatabase).ToList();

            var dummyPage = new Page();

            var data = new
            {
                CurrentStocks = currentStocks
            };

            return View["/Admin/Inventorymanager", new StandardModel(this, dummyPage, data)];
        }

        private dynamic GetStock(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var param = ((JObject)arg.body.Value);
            var checkingDate = param.Value<DateTime>("checkingDate").ToLocalTime();

            return InventorySummary.GetStocks(checkingDate, this.SiteDatabase).ToList();
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