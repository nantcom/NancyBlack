using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            
            // Since before 02 Dec, 2015. Paysbuy had been sending SO with prefix 00.     
            var soId = paysbuyPostback.result.Substring(2);
            var code = paysbuyPostback.result.Substring(0, 2);

            var log = PaymentLog.FromContext(this.Context);

            log.PaymentSource = PaymentMethod.PaySbuy;
            log.Amount = paysbuyPostback.amt;
            log.Fee = paysbuyPostback.fee;
            log.SaleOrderIdentifier = soId;
            log.IsErrorCode = code != "00";
            log.ResponseCode = code;
            log.IsPaymentSuccess = false;

            var paymentDateString = log.FormResponse.Value<string>("payment_date");
            DateTime paymentDate = DateTime.ParseExact(paymentDateString, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            CommerceModule.HandlePayment(this.SiteDatabase, log, DateTime.Now);

            var page = ContentModule.GetPage(this.SiteDatabase, "/__/commerce/thankyou", true);

            var so = this.SiteDatabase.Query<SaleOrder>().Where(s => s.SaleOrderIdentifier == soId).FirstOrDefault();

            return new StandardModel(this, page, JObject.FromObject(new
            {
                SaleOrderIdentification = soId,
                Log = log,
                IsSplitPayment = so.Status == PaymentStatus.Deposit || so.PaymentStatus == PaymentStatus.Deposit
            }));
        }

    }
}