using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.AdminSystem.Types;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class RMASupportModule : BaseModule
    {
        public RMASupportModule()
        {
            Get["/admin/tables/rma"] = this.HandleRequest(this.RMAManagerPage);
            Get["/rma/{saleOrderIdentifier}"] = this.HandleRequest(HandleSupportPage);
            Post["/rma/{saleOrderIdentifier}/save/attachment/message"] = this.HandleRequest(HandleCustomerSaveAttachmentMessage);
            Post["/admin/tables/rma/new"] = this.HandleRequest(this.NewRMA);
            //Post["/support/login"] = this.HandleRequest(HandleSupportLogin);
        }

        private dynamic RMAManagerPage(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var dummyPage = new Page();

            var data = new
            {
            };

            return View["/Admin/rma-manager", new StandardModel(this, dummyPage, data)];
        }

        private dynamic NewRMA(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var rma = new RMA()
            {
                InboundShipment = new RMAShipment() { Location = new JObject() },
                OutboundShipment = new RMAShipment() { Location = new JObject() },
                Status = "",
                Customer = new JObject()
            };

            rma = this.SiteDatabase.UpsertRecord(rma);
            rma.RMAIdentifier = string.Format(CultureInfo.InvariantCulture,
                        "RMA{0:yyyyMMdd}-{1:000000}",
                        rma.__createdAt,
                        rma.Id);
            rma = this.SiteDatabase.UpsertRecord(rma);

            return rma;
        }

        private dynamic HandleSupportPage(dynamic arg)
        {
            var id = (string)arg.saleOrderIdentifier;
            var rma = this.SiteDatabase.Query<RMA>()
                        .Where(row => row.RMAIdentifier == id)
                        .FirstOrDefault();
            

            var dummyPage = new Page();

            var data = new
            {
                RMA = rma
            };

            return View["commerce-rma-support", new StandardModel(this, dummyPage, data)];
        }

        private dynamic HandleCustomerSaveAttachmentMessage(dynamic arg)
        {
            var para = arg.body.Value as JObject;
            var id = (string)arg.saleOrderIdentifier;
            var so = this.SiteDatabase.Query<SaleOrder>()
                        .Where(row => row.SaleOrderIdentifier == id)
                        .FirstOrDefault();

            if (so == null)
            {
                return 404;
            }

            foreach (JObject item in so.Attachments)
            {
                if (item.Value<string>("Url") == para.Value<string>("Url"))
                {
                    item["Caption"] = para.Value<string>("Message");
                    break;
                }
            }

            this.SiteDatabase.UpsertRecord(so);

            return 200;
        }
    }
}