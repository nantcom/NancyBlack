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

            Get["/admin/tables/affiliatereward"] = this.HandleRequest((arg) =>
            {
                if (!this.CurrentUser.HasClaim("admin"))
                {
                    return 403;
                }

                return View["/Admin/couponlist", new StandardModel(this, new Page(), null)];
            });

            Get["/admin/commerce/api/exchangerate"] = this.HandleRequest(this.GetExchangeRate);

            Patch["/tables/product/{id:int}"] = this.HandleRequest(this.HandleProductSave);

            Post["/admin/commerce/api/pay"] = this.HandleRequest(this.HandlePayRequest);

            #region Search By Serial

            Get["/admin/search/by/serial"] = this.HandleRequest(this.HandleSearchBySerial);

            Get["/admin/search/serial"] = this.HandleViewRequest("/Admin/searchserialmanager", (arg) => new StandardModel(this, null, null));

            #endregion

            #region Payment Update Reminder

            Get["/admin/tables/pur"] = this.HandleRequest(this.HandleLoadPURPage);

            Post["/admin/pur/statusupdate"] = this.HandleRequest(this.UpdatePURStatus);

            #endregion

            #region List Receipt for Printing

            Get["/admin/commerce/printreceipt"] = this.HandleViewRequest("/Admin/commerceadmin-receiptprint", this.HandleReceiptRequest);

            Get["/admin/commerce/printreceipt/{year}-{month}"] = this.HandleViewRequest("/Admin/commerceadmin-receiptprint", this.HandleReceiptRequestWithSpecificMonth);

            #endregion

            Get["/admin/commerce/facebookexport"] = this.HandleRequest(this.HandleFacebookCustomAudienceExport);

            #region Quick Actions

            Post["/admin/commerce/api/enablesizing"] = this.HandleRequest(this.EnableSizingVariations);

            #endregion
        }

        #region Generate AccountantMonthlyReceipt Code

        private List<AccountantMonthlyReceipt> GetAccountantMonthlyReceipt(List<Receipt> receipts, List<PaymentLog> payments)
        {
            var result = new List<AccountantMonthlyReceipt>();
            var paymentLogs = payments.ToDictionary(l => l.Id);

            foreach (var receipt in receipts)
            {
                var record = new AccountantMonthlyReceipt();
                record.Receipt = receipt;
                record.SaleOrder = this.SiteDatabase.GetById<SaleOrder>(receipt.SaleOrderId);
                record.RelatedPaymentLogs = this.SiteDatabase.Query<PaymentLog>()
                                                .Where(pl => pl.SaleOrderId == record.SaleOrder.Id && pl.IsPaymentSuccess)
                                                .OrderBy(pl => pl.PaymentDate).ToList();
                record.PaymentLog = paymentLogs[receipt.PaymentLogId];

                var index = 0;
                for (int i = 0; i < record.RelatedPaymentLogs.Count; i++)
                {
                    if (record.RelatedPaymentLogs[i].Id == receipt.PaymentLogId)
                    {
                        record.PaymentLog.SuccessfulPaymentIndex = index;
                        break;
                    }

                    index++;
                }

                if (record.SaleOrder.Attachments == null)
                {
                    record.SaleOrder.Attachments = new dynamic[0];
                }

                var total = record.SaleOrder.TotalAmount;
                var sumPayment = record.RelatedPaymentLogs.Sum(pl => pl.Amount);

                if (total == sumPayment)
                {
                    record.Status = "OK";
                }
                else if (total == sumPayment + record.SaleOrder.PaymentFee)
                {
                    record.Status = "Missing Fee";
                }
                else
                {
                    record.Status = string.Format("{0:0,0.0} to Collect.", total - sumPayment);
                }

                result.Add(record);
            }

            return result;
        }

        private List<AccountantMonthlyReceipt> FindReceipt(int year, int month)
        {
            var begin = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc); // first second of last month

            // the first month we use new receipt identifier format is 2019/11
            if (begin >= new DateTime(2019, 11, 1, 0, 0, 0, DateTimeKind.Utc))
            {
                // set receipt identifier
                CommerceModule.SetReceiptIdentifier(this.SiteDatabase, begin);
            }

            var end = begin.AddMonths(1);
            end = end.AddMilliseconds(-1); // one second before start of this month

            var thaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var negativeTicksOffset = thaiTimeZone.BaseUtcOffset.Ticks * -1;
            begin = begin.AddTicks(negativeTicksOffset);
            end = end.AddTicks(negativeTicksOffset);

            var paymentsThisMonth = this.SiteDatabase.Query<PaymentLog>()
                                      .Where(l => l.__createdAt >= begin && l.__createdAt <= end)
                                      .OrderBy(l => l.__createdAt)
                                      .ThenBy(l => l.Id).ToList();

            List<Receipt> receipts = new List<Receipt>();

            foreach (var l in paymentsThisMonth)
            {
                var receipt = this.SiteDatabase.Query<Receipt>().Where(r => r.PaymentLogId == l.Id).FirstOrDefault();
                if (receipt == null)
                {
                    continue;
                }
                receipts.Add(receipt);
            }

            var result = this.GetAccountantMonthlyReceipt(receipts, paymentsThisMonth);

            return result;
        }

        private StandardModel HandleReceiptRequest(dynamic arg)
        {
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);

            var result = this.FindReceipt(lastMonth.Year, lastMonth.Month);
            return new StandardModel(this, null, result);
        }

        private StandardModel HandleReceiptRequestWithSpecificMonth(dynamic arg)
        {
            var result = this.FindReceipt((int)arg.year, (int)arg.month);
            return new StandardModel(this, null, result);
        }

        #endregion

        #region Payment Update Reminder

        /// <summary>
        /// Generate PUR for non-pair SaleOrder before load webpage
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic HandleLoadPURPage(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            // generate
            // list all so first
            // list non-gen from so status and payment status for generation
            // should have less than 100 rec even in first generation
            Func<SaleOrder, bool> soQueryLogic = (rec) =>
            {
                return rec.PaymentStatus != PaymentStatus.Refunded
                && rec.PaymentStatus != PaymentStatus.PaymentReceived
                && rec.Status != SaleOrderStatus.Cancel
                && rec.Attachments != null;
            };

            var saleOrders = this.SiteDatabase.Query<SaleOrder>().Where(soQueryLogic).ToList();

            //remove saleorder which has been used for generate PUR
            var allPurs = this.SiteDatabase.Query<PaymentUpdateReminder>().ToList();
            var generatedSOIds = allPurs.Select(rec => rec.SaleOrderId);
            var nonGenSaleOrders = from rec in saleOrders where !generatedSOIds.Contains(rec.Id) select rec;

            // generate PUR from nonGenSaleOrders and insert to database
            foreach (var nonGenSo in nonGenSaleOrders)
            {
                var pur = new PaymentUpdateReminder();
                pur.SaleOrderId = nonGenSo.Id;
                pur.SetStatus(nonGenSo, PaymentUpdateReminderStatus.Pending);
                pur = this.SiteDatabase.UpsertRecord(pur);
            }

            foreach (var pur in allPurs.Where(rec => rec.Status != PaymentUpdateReminderStatus.AutoAccepted && rec.Status != PaymentUpdateReminderStatus.Accepted && rec.Status != PaymentUpdateReminderStatus.Rejected))
            {
                var so = this.SiteDatabase.GetById<SaleOrder>(pur.SaleOrderId);

                if (so.PaymentStatus == PaymentStatus.PaymentReceived || so.PaymentStatus == PaymentStatus.Refunded)
                {
                    pur.SetStatus(so, PaymentUpdateReminderStatus.AutoAccepted);
                    this.SiteDatabase.UpsertRecord(pur);
                }
                else if (!pur.HasAttachmentBeenUpdated)
                {
                    pur.UpdateHasAttachmentsString(so);

                    if (pur.HasAttachmentBeenUpdated)
                    {
                        this.SiteDatabase.UpsertRecord(pur);
                    }
                }
            }

            var purStatus = new List<Tuple<int, string>>();
            foreach (PaymentUpdateReminderStatus p in Enum.GetValues(typeof(PaymentUpdateReminderStatus)))
            {
                purStatus.Add(new Tuple<int, string>((int)p, p.ToString()));
            }

            return View["/Admin/payment-update-reminder", new StandardModel(this, new Page(), purStatus)];
        }

        private dynamic UpdatePURStatus(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var param = ((JObject)arg.body.Value);

            var updatedStatus = (PaymentUpdateReminderStatus)param.Value<int>("status");
            var purId = param.Value<int>("purId");

            var pur = this.SiteDatabase.GetById<PaymentUpdateReminder>(purId);
            var so = this.SiteDatabase.GetById<SaleOrder>(pur.SaleOrderId);
            pur.SetStatus(so, updatedStatus);
            pur = this.SiteDatabase.UpsertRecord(pur);

            return pur;
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

            DateTime paidWhen = param.Value<DateTime>("paidWhen").ToUniversalTime();
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
        private dynamic HandleFacebookCustomAudienceExport(dynamic arg)
        {
            var response = new Response();
            response.ContentType = "text/csv";
            response.Headers["Content-Disposition"] =
                string.Format("attachment; filename=\"FacebookExport-{0:yyyyMMdd}{1}.csv\"",
                    DateTime.Now,
                    this.Request.Query.status == null ? "" : "-" + this.Request.Query.status.ToString().Replace("!", "Not"));

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
                Func<dynamic, string> keyGetter = (so) =>
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


                var subtraction = notq.ToLookup(so => keyGetter(so));
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