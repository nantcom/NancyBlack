using NantCom.NancyBlack.Modules.CommerceSystem.types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class CommerceModule : BaseModule
    {
        public CommerceModule()
        {
            Get["/__commerce/cart"] = this.HandleRequest((args) =>
            {
                return View["commerce-shoppingcart", this.GetModel()];

            });

            Get["/__commerce/saleorder/{id}/notifytransfer"] = this.HandleRequest((args) =>
            {
                var saleorder = this.SiteDatabase.Query("saleorder",
                    string.Format("uuid eq '{0}'", (string)args.id),
                    "Id desc").FirstOrDefault();

                return View["commerce-notifytransfer", this.GetModel(saleorder)];

            });

            Post["/__commerce/paymentlog/paysbuy"] = this.HandleRequest((args) =>
            {

                var FormData = this.Request.Form;

                string Response = string.Empty;

                foreach(var Key in FormData.Keys)
                {
                    var Value = FormData[Key].ToString();
                    Response += string.Concat(Key.ToString(), ":", Value.ToString(), "|");
                }

                PaymentLogPaysbuy PaymentLog = new PaymentLogPaysbuy()
                {
                    Response = Response
                };

                try
                {
                    this.SiteDatabase.UpsertRecord("paymentlogpaysbuy", PaymentLog);                    
                }
                catch (Exception e) {
                    throw e;
                }

                return 201;
                               
            });

        }
    }
}