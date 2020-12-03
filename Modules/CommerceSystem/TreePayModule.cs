using System.Security.Cryptography;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Nancy;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class TreePayModule : BaseModule
    {
        public TreePayModule()
        {
            Post["/__commerce/treepay/settings"] = this.HandleRequest(this.GetTreePaySettings);
            Post["/treepay/hashdata"] = this.HandleRequest(this.GetHashData);
            Post[PostbackPath] = this.HandleRequest(this.HandlePaymentPostback);
        }

        private string PostbackPath = "/__commerce/treepay/postback";


        private dynamic GetTreePaySettings(dynamic arg)
        {
            dynamic settings = new JObject(this.CurrentSite.treepay);
            settings.secure_key = null;
#if DEBUG
            settings.postUrl = "http://" + this.Request.Url.HostName + ":10096" + PostbackPath;
#else
            settings.postUrl = "https://" + this.Request.Url.HostName + PostbackPath;
#endif

            return settings;
        }

        private string GenerateSHA256String(string inputString)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha256.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }

        private string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }

        private dynamic GetHashData(dynamic arg)
        {
            var para = arg.body.Value as JObject;
            if (para == null)
            {
                return 404;
            }

            var settings = this.CurrentSite.treepay;
            var soIdentifier = para.Value<string>("soIdentifier");
            var so = this.SiteDatabase.Query<SaleOrder>().Where(rec => rec.SaleOrderIdentifier == soIdentifier).FirstOrDefault();

            if (so == null)
            {
                return 404;
            }

            return GenerateSHA256String(
                para.Value<string>("pay_type") +
                para.Value<string>("orderNo") +
                para.Value<int>("trade_mony") +
                settings.site_cd +
                settings.secure_key +
                para.Value<string>("user_id")).ToLower();
        }

        private dynamic HandlePaymentPostback(dynamic arg)
        {
            JObject postback = JObject.FromObject(this.Request.Form.ToDictionary());
            
            var soId = postback.Value<string>("order_no");
            var code = postback.Value<string>("res_cd");

            var log = PaymentLog.FromContext(this.Context);

            log.PaymentSource = PaymentMethod.TreePay;
            log.Amount = postback.Value<decimal>("trade_mony");
            if (soId.Length == 20)
            {
                log.SaleOrderIdentifier = soId.Substring(0, 17);
            }
            else
            {
                log.SaleOrderIdentifier = soId;
            }
            log.IsErrorCode = code != "0000";
            log.ResponseCode = code;
            log.IsPaymentSuccess = false;

            var paymentDateString = log.FormResponse.Value<int>("trade_ymd").ToString();
            var paymentTimeString = log.FormResponse.Value<int>("trade_hms").ToString();

            if (paymentTimeString.Length < 6)
            {
                paymentTimeString = "0" + paymentTimeString;
            }

            DateTime paymentDate = default(DateTime);

            if (!log.IsErrorCode)
            {
                if ( DateTime.TryParseExact(paymentDateString + paymentTimeString, 
                    "yyyyMMddHHmmss", CultureInfo.InvariantCulture, 
                    DateTimeStyles.AssumeLocal, out paymentDate) == false )
                {
                    paymentDate = DateTime.Now;
                }
            }

            // reponse from TreePay always be Thailand Time (SE Asia Standard Time)
            // convert to Utc DateTime (have to do this in case server use other time zone
            var thaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            paymentDate = paymentDate.AddTicks(thaiTimeZone.BaseUtcOffset.Ticks * -1);
            paymentDate = DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc);

            // find existing payment of same sale order
            var existing = this.SiteDatabase.Query<PaymentLog>().Where(l => l.SaleOrderIdentifier == log.SaleOrderIdentifier).ToList();

            // check for same auth_no, if it is already exists
            // return
            foreach (var item in existing)
            {
                if ( log.FormResponse.auth_no == item.FormResponse.auth_no )
                {
                    return this.Response.AsRedirect("/support/" + log.SaleOrderIdentifier);
                }
            }

            CommerceModule.HandlePayment(this.SiteDatabase, log, paymentDate.ToUniversalTime());

            return this.Response.AsRedirect("/support/" + log.SaleOrderIdentifier + "?paymentsuccess");
        }

    }
}