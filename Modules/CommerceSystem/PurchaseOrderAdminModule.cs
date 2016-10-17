using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class PurchaseOrderAdminModule : BaseModule
    {
        public PurchaseOrderAdminModule()
        {
            Get["/admin/tables/purchaseorder"] = this.HandleRequest(this.HandlePurchaseOrderManagerPage);
            Get["/admin/purchaseorder/{poId:int}/items"] = this.HandleRequest(this.GetPurchaseOrderItems);
        }

        private dynamic HandlePurchaseOrderManagerPage(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var statusType = (string)this.Request.Query.status;
            if (statusType == null)
            {
                statusType = SaleOrderStatus.WaitingForOrder;
            }

            var dummyPage = new Page();

            var data = new
            {
                PurchaseOrderStatus = StatusList.GetAllStatus<PurchaseOrderStatus>(),
                UnpaidBillsAmount = this.SiteDatabase.Query<PurchaseOrder>()
                .Where(po => po.IsCancel == false && po.HasPaid == false).Sum(po => po.TotalAmount)
            };

            return View["/Admin/purchaseordermanager", new StandardModel(this, dummyPage, data)];
        }

        private dynamic GetPurchaseOrderItems(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var poId = (int)arg.poId;
            return this.SiteDatabase.Query<Item>()
                .Where(item => item.PurchaseOrderId == poId)
                .ToList();
        }
    }
}