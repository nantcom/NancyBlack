using Nancy;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using RestSharp;
using Newtonsoft.Json.Linq;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class BitCoinModule : BaseModule
    {
        /// <summary>
        /// Performs Bx API Call
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url">Leaf url of the API to call (such as /api/order, /api)</param>
        /// <param name="twofa"></param>
        /// <returns></returns>
        private JObject BxApiCall( Method method, string url, object parameters = null, string twofa = null )
        {
            RestClient c = new RestClient("https://bx.in.th/");
            RestRequest req = new RestRequest(url, method);

            if (method == Method.POST) // all POST method are private methods
            {
                var sha = SHA256.Create();
                string nonce = DateTime.Now.Ticks.ToString();
                string key = this.CurrentSite.commerce.bx.apikey;
                string secret = this.CurrentSite.commerce.bx.apisecret;

                var hash = sha.ComputeHash(System.Text.Encoding.ASCII.GetBytes(key + nonce + secret));
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                req.Parameters.Add(new Parameter() { Name = "key", Value = key, Type = ParameterType.GetOrPost });
                req.Parameters.Add(new Parameter() { Name = "nonce", Value = nonce, Type = ParameterType.GetOrPost });
                req.Parameters.Add(new Parameter() { Name = "signature", Value = signature, Type = ParameterType.GetOrPost });

                if (!string.IsNullOrEmpty( twofa ))
                {
                    req.Parameters.Add(new Parameter() { Name = "twofa", Value = twofa, Type = ParameterType.GetOrPost });
                }
            }

            if (parameters != null)
            {
                foreach (var p in JObject.FromObject(parameters).Properties())
                {
                    req.Parameters.Add(new Parameter() { Name = p.Name, Value = p.Value, Type = ParameterType.GetOrPost });
                }
            }
            
            
            var result = c.Execute(req);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Failed: " + result.Content);
            }

            return JObject.Parse(result.Content);
        }

        public BitCoinModule()
        {
            Get["/__commerce/btcquote"] = this.HandleRequest((arg) =>
            {
                var day = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                var key = "btchistory-" + day;
                var cached = MemoryCache.Default.Get(key);

                if (cached != null)
                {
                    return cached;
                }

                dynamic timeseries = this.BxApiCall(Method.GET, "/api/tradehistory/",
                    new { paring = 1, date = day });

                MemoryCache.Default.Add(key, timeseries, DateTimeOffset.Now.Date.AddDays(1));

                return timeseries;
            });

            Get["/__commerce/{so_id}/btcdepositinfo"] = this.HandleRequest( (arg) => {

                int soId = 0;
                var id = (string)arg.so_id;

                SaleOrder so = null;
                if (int.TryParse(id, out soId))
                {
                    so = this.SiteDatabase.GetById<SaleOrder>(soId);
                }
                else
                {
                    so = this.SiteDatabase.Query<SaleOrder>()
                                .Where(row => row.SaleOrderIdentifier == id)
                                .FirstOrDefault();
                }

                if (so == null)
                {
                    return 404;
                }

                dynamic timeseries = this.BxApiCall(Method.GET, "/api/tradehistory/",
                    new { paring = 1, date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) } );

                string address = this.BxApiCall(Method.POST, "/api/deposit/",
                                    new { currency = "BTC" })
                                    .Property("address").Value.ToString();

                var amount = Math.Round( so.TotalAmount * (1 / (decimal)timeseries.data.low), 4 );
                return new
                {
                    address = address,
                    amount = amount,
                    qrcode = "https://chart.googleapis.com/chart?chs=225x225&chld=L|2&cht=qr&chl=bitcoin:" + address +
                            "?amount=" + amount +
                            "%26label=" + Uri.EscapeDataString((string)this.CurrentSite.commerce.billing.name) +
                            "%26message=" + Uri.EscapeDataString("Payment for " + so.SaleOrderIdentifier)
                };
            });
        }

    }
}