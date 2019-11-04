using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.AccountingSystem.Types;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class CommerceAdminModule : BaseModule
    {
        public CommerceAdminModule()
        {
            Get["/admin/tables/product"] = this.HandleViewRequest("/Admin/productmanager", null);
            Get["/admin/tables/suplier"] = this.HandleViewRequest("/Admin/suppliermanager", null);
            
            Get["/admin/commerce/settings"] = this.HandleViewRequest("/Admin/commerceadmin-settings", null);

            Post["/admin/commerce/api/uploadlogo"] = this.HandleRequest((arg) =>
            {
                var file = this.Request.Files.First();
                var filePath = "Site/billinglogo" + Path.GetExtension(file.Name);
                using (var localFile = File.OpenWrite(Path.Combine(this.RootPath, filePath)))
                {
                    file.Value.CopyTo(localFile);
                }

                return "/" + filePath;
            });

            Get["/admin/commerce/api/exchangerate"] = this.HandleRequest(this.GetExchangeRate);

            Patch["/tables/product/{id:int}"] = this.HandleRequest(this.HandleProductSave);

            Post["/admin/commerce/api/pay"] = this.HandleRequest(this.HandlePayRequest);

            Get["/admin/search/by/serial"] = this.HandleRequest(this.HandleSearchBySerial);

            Get["/admin/search/serial"] = this.HandleViewRequest("/Admin/searchserialmanager", (arg) => new StandardModel(this, null, null));

            Get["/admin/commerce/printreceipt"] = this.HandleViewRequest("/Admin/commerceadmin-receiptprint", this.HandleReceiptRequest);

            Get["/admin/commerce/printreceipt/{year}-{month}"] = this.HandleViewRequest("/Admin/commerceadmin-receiptprint", this.HandleReceiptRequestWithSpecificMonth);

            Get["/admin/commerce/facebookexport"] = this.HandleRequest(this.HandleFacebookCustomAudienceExport);

            #region Quick Actions

            Post["/admin/commerce/api/enablesizing"] = this.HandleRequest(this.EnableSizingVariations);

            #endregion
        }

        #region Generate AccountantMonthlyReceipt Code

        private List<AccountantMonthlyReceipt> GetAccountantMonthlyReceipt(List<Receipt> receipts)
        {
            var result = new List<AccountantMonthlyReceipt>();

            foreach (var receipt in receipts)
            {
                var record = new AccountantMonthlyReceipt();
                record.Receipt = receipt;
                record.SaleOrder = this.SiteDatabase.GetById<SaleOrder>(receipt.Id);
                record.RelatedPaymentLogs = this.SiteDatabase.Query<PaymentLog>().Where(pl => pl.SaleOrderId == record.SaleOrder.Id).ToList();
                record.PaymentLog = this.SiteDatabase.GetById<PaymentLog>(receipt.PaymentLogId);

                if (record.SaleOrder.Attachments == null)
                {
                    record.SaleOrder.Attachments = new dynamic[0];
                }

                var total = record.SaleOrder.TotalAmount;
                var sumPayment = record.RelatedPaymentLogs.Where(pl => pl.IsPaymentSuccess).Sum(pl => pl.Amount);

                if (total == sumPayment)
                {
                    record.Status = "All Payment matched TotalAmount";
                }
                else if (total == sumPayment + record.SaleOrder.PaymentFee)
                {
                    record.Status = "Payment Fee missing syspected!";
                }
                else
                {
                    record.Status = string.Format("There is Amount: {0:0,0.0} missing from payment", total - sumPayment);
                }

                result.Add(record);
            }

            return result;
        }

        private StandardModel HandleReceiptRequest(dynamic arg)
        {
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);
            lastMonth = new DateTime(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0); // first second of last month

            var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
            thisMonth = thisMonth.AddSeconds(-1); // one second before start of this month

            var receipts = this.SiteDatabase.Query<Receipt>()
                            .Where(r => r.__updatedAt >= lastMonth && r.__updatedAt <= thisMonth)
                            .OrderBy(r => r.Id)
                            .ToList();

            var result = this.GetAccountantMonthlyReceipt(receipts);

            return new StandardModel(this, null, result);
        }

        private StandardModel HandleReceiptRequestWithSpecificMonth(dynamic arg)
        {
            var lastMonth = new DateTime((int)arg.year, (int)arg.month, 1, 0, 0, 0); // first second of last month

            var thisMonth = lastMonth.AddMonths(1);
            thisMonth = thisMonth.AddSeconds(-1); // one second before start of this month

            var receipts = this.SiteDatabase.Query<Receipt>()
                            .Where(r => r.__updatedAt >= lastMonth && r.__updatedAt <= thisMonth)
                            .OrderBy(r => r.Id)
                            .ToList();

            var result = this.GetAccountantMonthlyReceipt(receipts);

            return new StandardModel(this, null, result);
        }

        #endregion

        private dynamic HandleSearchBySerial(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var serial = (string)this.Request.Query.key;
            var saleOrders = this.SiteDatabase.Query<SaleOrder>().AsEnumerable();
            var inventoryItems = this.SiteDatabase.Query<InventoryItem>().AsEnumerable();
            var rmaItems = this.SiteDatabase.Query<RMAItem>().AsEnumerable();

            var result = new List<SearchResult>();

            foreach (var saleOrder in saleOrders)
            {
                if (saleOrder.CustomData != null && saleOrder.CustomData.SerialNumbers != null)
                {
                    foreach (var item in saleOrder.CustomData.SerialNumbers)
                    {
                        if (((string)item.Serial).Contains(serial))
                        {
                            var newResult = new SearchResult()
                            {
                                RecordDate = saleOrder.DeliveryDate == null ? saleOrder.__createdAt : saleOrder.DeliveryDate,
                                Result = item.Serial,
                                Source = string.Format("SaleOrder: {0}", saleOrder.Id)
                            };

                            result.Add(newResult);

                            break;
                        }
                    }
                }
            }

            foreach (var invItem in inventoryItems)
            {
                if (!string.IsNullOrEmpty(invItem.SerialNumber) && invItem.SerialNumber.Contains(serial))
                {
                    var newResult = new SearchResult()
                    {
                        RecordDate = invItem.__updatedAt,
                        Result = invItem.SerialNumber,
                        Source = string.Format("SaleOrder: {0}", invItem.SaleOrderId)
                    };

                    result.Add(newResult);
                }
            }

            foreach (var rmaItem in rmaItems)
            {
                if (!string.IsNullOrEmpty(rmaItem.FromSerial) && rmaItem.FromSerial.Contains(serial))
                {
                    var newResult = new SearchResult()
                    {
                        RecordDate = rmaItem.__createdAt,
                        Result = rmaItem.FromSerial,
                        Source = string.Format("RMA: {0}, used for claim", rmaItem.Id)
                    };

                    result.Add(newResult);
                }
                else if (!string.IsNullOrEmpty(rmaItem.ToSerial) && rmaItem.ToSerial.Contains(serial))
                {
                    var newResult = new SearchResult()
                    {
                        RecordDate = rmaItem.__createdAt,
                        Result = rmaItem.ToSerial,
                        Source = string.Format("RMA: {0}, used for exchange", rmaItem.Id)
                    };

                    result.Add(newResult);
                }
            }

            return from item in result orderby item.RecordDate select item;
        }
        
        private dynamic HandlePayRequest(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var param = ((JObject)arg.body.Value);

            DateTime paidWhen = param.Value<DateTime>("paidWhen").ToLocalTime();
            string soIdentifier = param.Value<string>("saleOrderIdentifier");
            string apCode = param.Value<string>("apCode");
            var form = new JObject();
            form.Add("apCode", apCode);

            var paymentLog = new PaymentLog()
            {
                PaymentSource = param.Value<string>("paymentMethod"),
                Amount = param.Value<decimal>("amount"),
                IsErrorCode = false,
                ResponseCode = "00",
                IsPaymentSuccess = true,
                SaleOrderIdentifier = soIdentifier,
                PaymentDate = paidWhen,
                FormResponse = form
            };

            if (paymentLog.PaymentSource == "Receivable")
            {
                return 400;
            }

            CommerceModule.HandlePayment(this.SiteDatabase, paymentLog, paidWhen);

            return paymentLog;

        }

        private dynamic EnableSizingVariations(dynamic arg)
        {
            TableSecModule.ThrowIfNoPermission(this.Context, "Product", TableSecModule.PERMISSON_UPDATE);

            var sizes = (string)arg.body.Value.choices;

            this.SiteDatabase.Transaction(() =>
            {
                foreach (var item in this.SiteDatabase.Query<Product>()
                                        .Where(p => p.IsVariation == false)
                                        .AsEnumerable())
                {
                    item.HasVariation = true;
                    item.VariationAttributes = JArray.FromObject(new[] { new
                    {
                        Name = "Size",
                        Choices = sizes
                    }});

                    this.HandleProductSave(JObject.FromObject(new
                    {
                        body = new
                        {
                            Value = item
                        }
                    }));
                }
            });

            return 200;
        }

        private dynamic HandleProductSave(dynamic arg)
        {
            TableSecModule.ThrowIfNoPermission(this.Context, "Product", TableSecModule.PERMISSON_UPDATE);


            var input = arg.body.Value as JObject;
            var product = input.ToObject<Product>();

            var dupe = this.SiteDatabase.Query<Product>()
                .Where(p => p.Url == product.Url)
                .FirstOrDefault();

            if (dupe != null && dupe.Id != product.Id)
            {
                throw new InvalidOperationException("Duplicate Url");
            }

            if (product.HasVariation == false)
            {
                this.SiteDatabase.UpsertRecord<Product>(product);
            }

            var attributes = product.VariationAttributes as JArray;
            if (attributes == null)
            {
                this.SiteDatabase.UpsertRecord<Product>(product);
                return product;
            }


            // handle generation of product variation
            this.SiteDatabase.Transaction(() =>
            {
                List<string[]> variations = new List<string[]>();
                foreach (JObject prop in attributes)
                {
                    var choices = from item in prop["Choices"].ToString().Split(',')
                                  select item.Trim();

                    variations.Add(choices.ToArray());
                }

                Action<string> createProduct = (url) =>
                {
                    var newUrl = (product.Url + url).ToLowerInvariant();

                    // copy all information from current product to replace the variation product
                    var newProduct = JObject.FromObject(product).ToObject<Product>();
                    var existingProduct = this.SiteDatabase.Query<Product>()
                                        .Where(p => p.Url == newUrl)
                                        .FirstOrDefault();

                    if (existingProduct == null)
                    {
                        newProduct.Id = 0;
                    }
                    else
                    {
                        newProduct.Id = existingProduct.Id;
                    }

                    newProduct.MasterProductId = product.Id;
                    newProduct.IsVariation = true;
                    newProduct.HasVariation = false;
                    newProduct.VariationAttributes = null;
                    newProduct.Url = newUrl;

                    var parts = url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var newProductAttributes = new JObject();

                    for (int i = 0; i < attributes.Count; i++)
                    {
                        newProductAttributes[attributes[i]["Name"].ToString()] = parts[i];
                    }

                    newProduct.Title += " (Variation:" + url + ")";
                    newProduct.Attributes = newProductAttributes;
                    this.SiteDatabase.UpsertRecord<Product>(newProduct);

                };

                Action<string, int> dig = null;
                dig = (baseUrl, index) =>
                {
                    foreach (var choice in variations[index])
                    {
                        var currentUrl = baseUrl + "/" + choice;

                        if (index + 1 == variations.Count)
                        {
                            createProduct(currentUrl);
                        }
                        else
                        {
                            dig(currentUrl, index + 1);
                        }

                    }
                };

                dig("", 0);
                this.SiteDatabase.UpsertRecord<Product>(product);
            });

            return product;
        }

        /// <summary>
        /// Custom Audience Export
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic HandleFacebookCustomAudienceExport( dynamic arg )
        {
            var response = new Response();
            response.ContentType = "text/csv";
            response.Headers["Content-Disposition"] =
                string.Format("attachment; filename=\"FacebookExport-{0:yyyyMMdd}{1}.csv\"",
                    DateTime.Now,
                    this.Request.Query.status == null ? "" : "-" + this.Request.Query.status.ToString().Replace("!", "Not" ));

            response.Headers["Expires"] = "0";
            response.Headers["Cache-Control"] = "must-revalidate, post-check=0, pre-check=0";
            response.Headers["Pragma"] = "public";

            var q = this.SiteDatabase.Query<SaleOrder>();
            TableQuery<SaleOrder> notq = null;

            if (this.Request.Query.status != null)
            {
                string status = this.Request.Query.status;
                if (status.StartsWith("!"))
                {
                    status = status.Substring(1);
                    q = q.Where(so => so.Status != status);
                    notq = this.SiteDatabase.Query<SaleOrder>().Where(so => so.Status == status);
                }
                else
                {
                    q = q.Where(so => so.Status == status);
                }
            }

            var result = q.ToList();

            if (notq != null)
            {
                Func<dynamic,string> keyGetter = (so) =>
                {
                    if (so.Customer == null)
                    {
                        return "";
                    }

                    string key = so.Customer.Email;

                    if (key == null)
                    {
                        return "";
                    }

                    return key.ToLowerInvariant()
                            .Replace(" ", "")
                            .Replace("-", "");
                };


                var subtraction = notq.ToLookup( so => keyGetter(so) );
                var finalResult = from so in result
                                  where
                                    so.Customer != null &&
                                    subtraction[keyGetter(so)].Count() == 0
                                  select so;

                result = finalResult.ToList();
            }

            response.Contents = (s) =>
            {
                var sw = new StreamWriter(s);
                sw.WriteLine("Name,LastName,Phone,Email,FacebookId,Value");

                Func<dynamic, string> normalizer = (input) =>
                {
                    if (input == null)
                    {
                        return "";
                    }
                    string output = input;
                    return output.Replace(",", ";").ToLowerInvariant();
                };

                foreach (var item in result)
                {
                    try
                    {
                        sw.WriteLine(
                            normalizer(item.Customer.FirstName) + "," +
                            normalizer(item.Customer.LastName) + "," +
                            normalizer(item.Customer.PhoneNumber) + "," +
                            normalizer(item.Customer.Email) + "," +
                            (item.Customer.User != null ? item.Customer.User.Profile.id : "") + "," +
                            item.TotalAmount
                        );
                    }
                    catch (Exception)
                    {
                    } 
                }

                sw.Flush();
                sw.Close();
            };

            return response;
        }

        #region Exchange Rate

        private dynamic GetExchangeRate(dynamic arg)
        {
            byte[] cached = CommerceAdminModule.GetExchangeRateData();

            var response = new Response();
            response.Contents = (s) =>
            {
                s.Write(cached, 0, cached.Length);
            };

            return response;

        }

        public static dynamic ExchangeRate
        {
            get
            {
                byte[] exchangeRateData = CommerceAdminModule.GetExchangeRateData();
                return JObject.Parse(Encoding.UTF8.GetString(exchangeRateData)).Property("rates").Value;
            }
        }
        
        private static byte[] GetExchangeRateData()
        {
            byte[] array = MemoryCache.Default["ExchangeRates"] as byte[];
            if (array == null)
            {
                WebClient web = new WebClient();
                array = web.DownloadData("https://openexchangerates.org/api/latest.json?app_id=8ecf50d998af4c2f837bfa416698784e");
                web.Dispose();
                MemoryCache.Default.Add("ExchangeRates", array, DateTimeOffset.Now.AddMinutes(60.0), null);
            }
            return array;
        }

        #endregion
    }
}