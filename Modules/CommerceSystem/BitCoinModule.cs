using Nancy;
using System;
using System.Linq;
using System.Collections.Generic;
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
        public enum Currency
        {
            BTC = 1,
            ETH = 21,
            OMG = 26
        }

        /// <summary>
        /// Performs Bx API Call
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url">Leaf url of the API to call (such as /api/order, /api)</param>
        /// <param name="twofa"></param>
        /// <returns></returns>
        private static JObject BxApiCall( dynamic site, Method method, string url, object parameters = null, string twofa = null )
        {
            RestClient c = new RestClient("https://bx.in.th/");
            RestRequest req = new RestRequest(url, method);

            if (method == Method.POST) // all POST method are private methods
            {
                var sha = SHA256.Create();
                string nonce = DateTime.Now.Ticks.ToString();
                string key = site.commerce.bx.apikey;
                string secret = site.commerce.bx.apisecret;

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

        /// <summary>
        /// Get Quote for Crypto Currency
        /// </summary>
        /// <param name="pairingId"></param>
        /// <param name="dt">Date/Time, if not specified will be yesterday's rate</param>
        /// <param name="cache">Whether to use Cache</param>
        /// <returns></returns>
        public static dynamic GetQuote(Currency c, DateTime? dt = null, bool cache = true)
        {
            if (dt == null)
            {
                dt = DateTime.Now.AddDays(-1);
            }

            var day = dt.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var key =  c.ToString() + "history-" + day;
            var cached = MemoryCache.Default.Get(key);

            if (cached != null && cache == true)
            {
                return cached;
            }

            dynamic timeseries = BitCoinModule.BxApiCall(null, Method.GET, "/api/tradehistory/",
                new { pairing = (int)c, date = day });

            MemoryCache.Default.Add(key, timeseries, DateTimeOffset.Now.Date.AddDays(1));

            return timeseries;
        }
        

        public BitCoinModule()
        {
            Get["/__commerce/btcquote"] = this.HandleRequest((arg) =>
            {
                return BitCoinModule.GetQuote(Currency.BTC);
            });

            Get["/__commerce/cryptoquote"] = this.HandleRequest((arg) =>
            {
                return new {
                    OMG = BitCoinModule.GetQuote(Currency.OMG).data.low,
                    BTC = BitCoinModule.GetQuote(Currency.BTC).data.low,
                    ETH = BitCoinModule.GetQuote(Currency.ETH).data.low
                };
            });

            // hard code address for now
            var address = new Dictionary<string, string>();
            address["BTC"] = "3Bw7NgmqKwK6VDNuNa8begVwHm6TA7daa9";
            address["ETH"] = "0x64d91c1CE4528efFA8F2Aed1334b0A44434c6956";
            address["OMG"] = "0x64d91c1CE4528efFA8F2Aed1334b0A44434c6956";

            Get["/__commerce/{so_id}/cryptodeposit/{currency}"] = this.HandleRequest((arg) => {

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

                var currency = (Currency)Enum.Parse(typeof(Currency), (string)arg.currency);
                var timeseries = BitCoinModule.GetQuote(currency);

                //string address = this.BxApiCall(Method.POST, "/api/deposit/",
                //                    new { currency = currency.ToString() })
                //                    .Property("address").Value.ToString();

                //BX charge 20THB + 10THB per 10,000
                var bxcharge = 20M;
                bxcharge = bxcharge + (so.TotalAmount / 10000) * 10;

                var amount = Math.Round((so.TotalAmount - so.PaymentFee + bxcharge) * (1 / (decimal)timeseries.data.low), 4);

                string url = string.Empty;

                if (currency == Currency.BTC)
                {
                    url = "bitcoin:" + address[currency.ToString()] +
                                "?amount=" + amount +
                                "%26label=" + Uri.EscapeDataString((string)this.CurrentSite.commerce.billing.name) +
                                "%26message=" + Uri.EscapeDataString("Payment for " + so.SaleOrderIdentifier);
                }
                else
                {
                    url = address[currency.ToString()];
                }

                return new
                {
                    address = address[currency.ToString()],
                    amount = amount,
                    qrcode = "https://chart.googleapis.com/chart?chs=225x225&chld=L|2&cht=qr&chl=" + url
                };
            });
        }

    }
}