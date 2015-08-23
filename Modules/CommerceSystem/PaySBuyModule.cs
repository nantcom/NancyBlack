using NantCom.NancyBlack.Modules.CommerceSystem.types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class PaySBuyModule : BaseModule
    {
        public PaySBuyModule()
        {
            Get["/__commerce/paysbuy/settings"] = this.HandleRequest(this.GetPaySbuySettings);

            Post["/__commerce/paysbuy/postback"] = this.HandleRequest(this.HandlePaySbuyPostback);
        }

        private dynamic GetPaySbuySettings(dynamic arg)
        {
            var settings = this.CurrentSite.paysbuy;

            if (this.Request.Url.HostName == "localhost")
            {
                settings.postUrl = "http://nant.co/__commerce/paysbuy/postback";
            }
            else
            {
                settings.postUrl = "http://" + this.Request.Url.HostName + "/__commerce/paysbuy/postback";
            }

            return settings;
        }

        private dynamic HandlePaySbuyPostback(dynamic arg)
        {
            //foreach (var Key in FormData.Keys)
            //{
            //    var Value = FormData[Key].ToString();
            //    Response += string.Concat(Key.ToString(), ":", Value.ToString(), "|");
            //}
            var log = PaymentLog.FromContext(this.Context);
            this.SiteDatabase.UpsertRecord<PaymentLog>(log);

            return 201;
        }

    }
}