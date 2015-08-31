using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class PaySbuyModule : BaseModule
    {
        public class PaySbuyPostback
        {
            /// <summary>
            /// Identifier of sale order
            /// </summary>
            public string result { get; set; }

            /// <summary>
            /// apCode
            /// </summary>
            public string apCode { get; set; }

            /// <summary>
            /// Amount
            /// </summary>
            public Decimal amt { get; set; }

            /// <summary>
            /// Fee
            /// </summary>
            public Decimal fee { get; set; }

            public string method { get; set; }

            /// <summary>
            /// Create Date
            /// </summary>
            public string create_date { get; set; }

            /// <summary>
            /// Payment Date
            /// </summary>
            public string payment_date { get; set; }
        }

        public PaySbuyModule()
        {
            Get["/__commerce/paysbuy/settings"] = this.HandleRequest(this.GetPaySbuySettings);

            Post["/__commerce/paysbuy/postback"] = this.HandleViewRequest("commerce-thankyoupage", this.HandlePaySbuyPostback);
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

        private StandardModel HandlePaySbuyPostback(dynamic arg)
        {
            JObject postback = JObject.FromObject(this.Request.Form.ToDictionary());
            PaySbuyPostback paysbuyPostback = postback.ToObject<PaySbuyPostback>();

            var log = PaymentLog.FromContext(this.Context);

            log.Amount = paysbuyPostback.amt;
            log.Fee = paysbuyPostback.fee;

            CommerceModule.HandlePayment(this.SiteDatabase, log, paysbuyPostback.result);

            var page = ContentModule.GetPage(this.SiteDatabase, "/__/commerce/thankyou", true);

            return new StandardModel(this, page, JObject.FromObject(new
            {
                SaleOrderIdentification = paysbuyPostback.result,
            }));
        }

    }
}