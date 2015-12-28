using NantCom.NancyBlack.Modules.DatabaseSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class InventoryAdminModule : BaseModule
    {
        public InventoryAdminModule()
        {
            Get["/admin/tables/inventorymovement"] = this.HandleViewRequest("/Admin/Inventorymanager", null);

            Get["/admin/inventorymovement/{id}"] = this.HandleViewRequest("/Admin/Inventorydetailmanager", null);

            #region Quick Actions

            Post["/admin/commerce/api/copystock"] = this.HandleRequest(this.CopyStock);

            #endregion
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