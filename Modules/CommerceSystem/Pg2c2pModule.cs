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
    public class Pg2c2pModule : BaseModule
    {
        public Pg2c2pModule()
        {
            Post["/__commerce/2c2p/parameters"] = this.HandleRequest(this.GetSettings);
            Post["/2c2p/hashdata"] = this.HandleRequest(this.GetHashData);
            Post[PostbackBackEndPath] = this.HandleRequest(this.HandlePaymentPostback);
            Post[PostbackFrontEndPath] = this.HandleRequest(this.HandleRedirectFromPayment);
        }

        private string PostbackBackEndPath = "/__commerce/2c2p/postback";
        private string PostbackFrontEndPath = "/__commerce/2c2p/postback/frontend";

        private dynamic GetSettings(dynamic arg)
        {
            var para = arg.body.Value as JObject;
            if (para == null)
            {
                return 404;
            }

            var soId = para.Value<int>("SaleOrderId");
            var saleOrder = this.SiteDatabase.GetById<SaleOrder>(soId);
            if (saleOrder == null)
            {
                return 404;
            }
            dynamic settings = new JObject(this.CurrentSite.pg2c2p);
            settings.authSercretKey = null;
            var utcNowTicksString = DateTime.UtcNow.Ticks.ToString();
            var orderIdBackPart = utcNowTicksString.Substring(0, 14);
            var orderIdFontPart = saleOrder.Id.ToString("000000");
            settings.orderId = orderIdFontPart + orderIdBackPart;
#if DEBUG
            settings.postBackFrontEndUrl = "http://" + this.Request.Url.HostName + ":10096" + PostbackFrontEndPath;
            settings.postBackBackEndUrl = "http://" + this.Request.Url.HostName + ":10096" + PostbackBackEndPath;
            settings.redirectUrlApi = "https://demo2.2c2p.com/2C2PFrontEnd/RedirectV3/payment";
#else
            settings.postBackFrontEndUrl = "https://" + this.Request.Url.HostName + PostbackFrontEndPath;
            settings.postBackBackEndUrl = "https://" + this.Request.Url.HostName + PostbackBackEndPath;
#endif
            return settings;
        }

        private dynamic GetHashData(dynamic arg)
        {
            var para = arg.body.Value as JObject;
            if (para == null)
            {
                return 404;
            }

            var settings = this.CurrentSite.pg2c2p;
            var parametersString = para.Value<string>("parameters");

            if (string.IsNullOrEmpty(parametersString))
            {
                return 404;
            }

            return Pg2c2pEncryption.GetHmacSha256HexText(parametersString, (string)settings.authSercretKey);
        }

        private PaymentLog InsertPaymentLog()
        {
            JObject postback = JObject.FromObject(this.Request.Form.ToDictionary());
            var response = postback.ToObject<Pg2c2pResponse>();
            var settings = this.CurrentSite.pg2c2p;
            var isIntegrety = response.VerifyHash((string)settings.merchantId, (string)settings.authSercretKey);

            if (!isIntegrety)
                throw new ApplicationException("Authenication Fail");

            if (response.currency != "764")
                throw new ApplicationException("Only support currency Thai Bath");

            var so = this.SiteDatabase.GetById<SaleOrder>(int.Parse(response.user_defined_1));
            if (response.user_defined_2 != so.SaleOrderIdentifier)
                throw new ApplicationException("Mishmatch Information");

            var log = PaymentLog.FromContext(this.Context);
            log.PaymentSource = PaymentMethod.Pg2C2P;
            var pgAmount = decimal.Parse(response.amount);
            log.Amount = pgAmount / 100;
            log.SaleOrderId = so.Id;
            log.SaleOrderIdentifier = so.SaleOrderIdentifier;
            log.IsErrorCode = response.channel_response_code != "00";
            log.ResponseCode = response.channel_response_code;
            log.IsPaymentSuccess = response.channel_response_code == "00";

            DateTime paymentDate = default(DateTime);

            if (!log.IsErrorCode)
            {
                if (DateTime.TryParseExact(response.transaction_datetime,
                    "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out paymentDate) == false)
                {
                    // set paymentDate as Now in case for unable to Parse (this should never happen)
                    paymentDate = DateTime.Now;
                }
            }

            // reponse from 2C2P always be Thailand Time (SE Asia Standard Time) due to configuration
            // convert to Utc DateTime (have to do this in case server use other time zone
            var thaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            paymentDate = paymentDate.AddTicks(thaiTimeZone.BaseUtcOffset.Ticks * -1);
            paymentDate = DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc);

            // find existing payment of same sale order
            var existPaymentLogs = this.SiteDatabase.Query<PaymentLog>().Where(l => l.SaleOrderId == log.SaleOrderId).ToList();
            foreach (var existLog in existPaymentLogs)
            {
                if ((string)existLog.FormResponse.order_id == response.order_id)
                    throw new ApplicationException("Payment has already been saved");
            }

            CommerceModule.HandlePayment(this.SiteDatabase, log, paymentDate);

            return log;
        }

        private dynamic HandleRedirectFromPayment(dynamic arg)
        {
            var paymentLog = this.InsertPaymentLog();
            if (paymentLog.IsPaymentSuccess)
                return this.Response.AsRedirect("/support/" + paymentLog.SaleOrderIdentifier + "?paymentsuccess");
            else
                return this.Response.AsRedirect("/support/" + paymentLog.SaleOrderIdentifier);
        }

        private dynamic HandlePaymentPostback(dynamic arg)
        {
            this.InsertPaymentLog();

            return 200;
        }

    }

    public class Pg2c2pResponse
    {
        public string version { get; set; }
        public string request_timestamp { get; set; }
        public string merchant_id { get; set; }
        public string order_id { get; set; }
        public string invoice_no { get; set; }
        public string currency { get; set; }
        public string amount { get; set; }
        public string transaction_ref { get; set; }
        public string approval_code { get; set; }
        public string eci { get; set; }
        public string transaction_datetime { get; set; }
        public string payment_channel { get; set; }
        public string payment_status { get; set; }
        public string channel_response_code { get; set; }
        public string channel_response_desc { get; set; }
        public string masked_pan { get; set; }
        public string stored_card_unique_id { get; set; }
        public string backend_invoice { get; set; }
        public string paid_channel { get; set; }
        public string paid_agent { get; set; }
        public string recurring_unique_id { get; set; }

        /// <summary>
        /// containing SaleOrder.Id as string
        /// </summary>
        public string user_defined_1 { get; set; }

        /// <summary>
        /// containing SaleOrder.SaleOrderIdentifier
        /// </summary>
        public string user_defined_2 { get; set; }
        public string user_defined_3 { get; set; }
        public string user_defined_4 { get; set; }
        public string user_defined_5 { get; set; }
        public string browser_info { get; set; }
        public string ippPeriod { get; set; }
        public string ippInterestType { get; set; }
        public string ippInterestRate { get; set; }
        public string ippMerchantAbsorbRate { get; set; }
        public string payment_scheme { get; set; }
        public string process_by { get; set; }
        public string sub_merchant_list { get; set; }
        public string card_type { get; set; }
        public string issuer_country { get; set; }
        public string issuer_bank { get; set; }
        public string hash_value { get; set; }

        public bool VerifyHash(string merchantId, string authSecretKey)
        {
            if (this.merchant_id != merchantId)
                return false;
            var message = version + request_timestamp + merchant_id + order_id +
	            invoice_no + currency + amount + transaction_ref + approval_code +
	            eci + transaction_datetime + payment_channel + payment_status +
	            channel_response_code + channel_response_desc + masked_pan +
	            stored_card_unique_id + backend_invoice + paid_channel + paid_agent +
	            recurring_unique_id + user_defined_1 + user_defined_2 + user_defined_3 +
	            user_defined_4 + user_defined_5 + browser_info + ippPeriod +
	            ippInterestType + ippInterestRate + ippMerchantAbsorbRate + payment_scheme +
	            process_by + sub_merchant_list;
            var newCypher = Pg2c2pEncryption.GetHmacSha256HexText(message, authSecretKey);
            return hash_value.ToLower() == newCypher;
        }
    }

    public static class Pg2c2pEncryption
    {
        public static string GetHmacSha256HexText(string input, string key)
        {
            Encoding ascii = Encoding.ASCII;
            HMACSHA256 hmac = new HMACSHA256(ascii.GetBytes(key));

            return Pg2c2pEncryption.ByteArrayToString(hmac.ComputeHash(ascii.GetBytes(input)));
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}