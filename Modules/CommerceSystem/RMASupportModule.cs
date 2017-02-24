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
            Get["/rma/{RMAIdentifier}"] = this.HandleRequest(HandleSupportPage);
            Post["/admin/tables/rma/new"] = this.HandleRequest(this.NewRMA);
            Post["/rma/{RMAIdentifier}/add"] = this.HandleRequest(this.HandleAddRMAItem);
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
            var id = (string)arg.RMAIdentifier;
            var rma = this.SiteDatabase.Query<RMA>()
                        .Where(row => row.RMAIdentifier == id)
                        .FirstOrDefault();

            if (!this.CurrentUser.HasClaim("admin"))
            {
                rma.PrivateNote = null;
            }

            var dummyPage = new Page();

            var data = new
            {
                RMA = rma,
                RMAItems = rma.GetRMAItems(this.SiteDatabase)
            };

            return View["commerce-rma-support", new StandardModel(this, dummyPage, data)];
        }

        private dynamic HandleAddRMAItem(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }
            
            var id = (string)arg.RMAIdentifier;
            var rma = this.SiteDatabase.Query<RMA>()
                        .Where(row => row.RMAIdentifier == id)
                        .FirstOrDefault();
            
            var para = arg.body.Value as JObject;
            RMAItem rmaItem = para.ToObject<RMAItem>();

            rma.AddRMAItem(this.SiteDatabase, rmaItem);

            return 200;
        }
    }
}