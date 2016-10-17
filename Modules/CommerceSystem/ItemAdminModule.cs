using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class ItemAdminModule : BaseModule
    {
        static ItemAdminModule()
        {
            CommerceModule.PaymentCompleted += CommerceModule_PaymentCompleted;
        }

        private static void CommerceModule_PaymentCompleted(SaleOrder so, Modules.DatabaseSystem.NancyBlackDatabase db)
        {
            if (so.PaymentStatus == PaymentStatus.PaymentReceived)
            {
                ItemManager.SaveItems(so, db);
            }
        }

        public ItemAdminModule()
        {
            // insert updatedStock here
            Get["/admin/item"] = this.HandleRequest(OpenInventoryManager);
        }

        private dynamic OpenInventoryManager(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var pendingItems = this.SiteDatabase.Query<Item>().Where(item => item.WasWithdrawn == false).ToList();
            

            var dummyPage = new Page();

            var data = new
            {
                WaitingToOrderItems = pendingItems.Where(item => !item.WasOrdered),
                WaitingForOrderItems = pendingItems.Where(item => item.WasOrdered && !item.WasReceived),
                InStockItems = pendingItems.Where(item => item.WasReceived),
                PendingItemsBySupplierId = (from item in pendingItems group item 
                                    by this.SiteDatabase.GetById<PurchaseOrder>(item.PurchaseOrderId).SupplierId)
                                    .OrderBy(group => group.Key).Select(group => new { SupplierId = group.Key, Items = group.ToList()})
                                    .ToList()
            };

            return View["/Admin/itemmanager", new StandardModel(this, dummyPage, data)];
        }
    }
}