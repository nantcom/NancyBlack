using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.LogisticsSystem.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class SaleOrderAdminModule : BaseModule
    {
        public SaleOrderAdminModule()
        {
            Get["/admin/tables/saleorder"] = this.HandleRequest(this.SaleOrderManagerPage);

            Post["/admin/tables/saleorder/new"] = this.HandleRequest(this.NewSaleOrder);

            Get["/admin/tables/saleorder/{id}"] = this.HandleRequest(this.HandleSaleorderDetailPage);

            Post["/admin/saleorder/{id}/add"] = this.HandleRequest(this.AddProductToSaleOrder);

            Post["/admin/saleorder/{id}/updateqty"] = this.HandleRequest(this.UpdateQty);

            Post["/admin/saleorder/{id}/previewtotal"] = this.HandleRequest(this.UpdateQty);

            Get["/admin/commerce/api/sostatus"] = this.HandleRequest(this.GetSaleorderStatusList);

            Get["/admin/commerce/api/paymentstatus"] = this.HandleRequest(this.GetPaymentStatusList);

            Get["/admin/commerce/api/printing/saleorder/current/month/list"] = this.HandleRequest(this.GetSaleorderForPrintingReceiptList);

            Post["/admin/saleorder/__updatedhlstatus"] = this.HandleRequest(this.UpdateDHLStatus);
        }
        
        private dynamic UpdateDHLStatus(dynamic arg)
        {
            return 200;
        }

        private dynamic SaleOrderManagerPage(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var dummyPage = new Page();

            var data = new
            {
            };

            return View["/Admin/saleordermanager", new StandardModel(this, dummyPage, data)];
        }

        private dynamic NewSaleOrder(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var so = new SaleOrder()
            {
                ShippingDetails = new JObject(),
                ItemsDetail = new List<Product>(),
                Items = new int[0],
                Customer = JObject.FromObject( new
                {
                    Email = "tempcustomer@nant.co",
                    FirstName = "NewCustomer",
                    LastName = "NewCustomer",

                    User = new
                    {
                        Id = 0,
                    }
                })
            };

            so.Status = SaleOrderStatus.New;
            so.PaymentStatus = PaymentStatus.WaitingForPayment;

            JObject shippingDetail = so.ShippingDetails;
            shippingDetail.Add("insurance", false);
            so.UpdateSaleOrder( this.CurrentSite, this.SiteDatabase, true);

            return so;
        }

        private dynamic AddProductToSaleOrder(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var so = this.SiteDatabase.GetById<SaleOrder>((int)arg.id);
            dynamic input = arg.Body.Value as JObject;

            so.AddItem(this.SiteDatabase, this.CurrentSite, (int)input.Id, (int)input.Qty);

            InventoryItemModule.ProcessSaleOrderUpdate(this.SiteDatabase, so, true, DateTime.Now);

            return new
            {
                SaleOrder = so,
                InventoryRequests = this.SiteDatabase.Query<InventoryItem>().Where( ivt => ivt.SaleOrderId == so.Id ).ToList()
            };
        }

        private dynamic UpdateQty(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            try
            {
                var so = arg.Body.Value as JObject;
                var existingSo = so.ToObject<SaleOrder>();

                var newItemsList = new List<int>();
                foreach (var item in existingSo.ItemsDetail.ToList()) // create a copy
                {
                    if (item.Attributes["Qty"] == null)
                    {
                        continue;
                    }
                    else
                    {
                        

                        for (int i = 0; i < (int)item.Attributes["Qty"]; i++)
                        {
                            newItemsList.Add(item.Id);
                        }
                    }
                }

                existingSo.Items = newItemsList.ToArray();

                var save = this.Request.Url.Path.EndsWith("updateqty");
                existingSo.UpdateSaleOrder(this.CurrentSite, this.SiteDatabase, save);

                if (save)
                {
                    InventoryItemModule.ProcessSaleOrderUpdate(this.SiteDatabase, existingSo, true, DateTime.Now);
                }

                return new
                {
                    SaleOrder = existingSo,
                    InventoryRequests = this.SiteDatabase.Query<InventoryItem>().Where(ivt => ivt.SaleOrderId == existingSo.Id).ToList()
                };
            }
            catch (Exception)
            {
                return 400;
            }
        }

        private dynamic GetSaleorderForPrintingReceiptList(dynamic arg)
        {
            var currentMonth = DateTime.Now.Date.AddDays(-DateTime.Now.Day + 1);
            var lastMonth = currentMonth.AddMonths(-1);

            var saleOrders = this.SiteDatabase.Query<SaleOrder>()
                .Where(so => so.PaymentReceivedDate >= lastMonth && so.PaymentReceivedDate < currentMonth)
                .ToList();

            var paymentLogs = this.SiteDatabase.Query<PaymentLog>()
                .Where(log => log.IsPaymentSuccess && log.PaymentDate >= lastMonth && log.PaymentDate < currentMonth)
                .ToList();

            var saleOrdersWithAtta = this.SiteDatabase.Query<SaleOrder>()
                .Where(so => so.PaymentReceivedDate >= lastMonth && so.__updatedAt >= lastMonth && so.__updatedAt < currentMonth && so.Attachments != null)
                .ToList()
                .Where(so =>
                {
                    bool isMatch = false;

                    var alreadyCount = saleOrders.Where(item => so.Id == item.Id).FirstOrDefault() != null;

                    if (alreadyCount)
                    {
                        return false;
                    }

                    foreach (JObject item in so.Attachments)
                    {
                        try
                        {
                            if (item.Value<DateTime>("CreateDate") >= lastMonth
                            && item.Value<DateTime>("CreateDate") < currentMonth
                            && (item.Value<string>("Caption").Contains("เงิน") || item.Value<string>("Status").Contains("เงิน")))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    return isMatch;
                })
                .ToList();

            return new { SaleOrders = saleOrders, SaleOrdersWithAtta = saleOrdersWithAtta, PaymentLogs = paymentLogs };
        }

        private dynamic HandleSaleorderDetailPage(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var id = (int)arg.id;
            var so = this.SiteDatabase.GetById<SaleOrder>(id);

            if (so == null)
            {
                return 404;
            }

            var dummyPage = new Page();

            List<AffiliateRewardsClaim> rewardList = null;
            List<AffiliateRewardsClaim> discountCodes = null;
            
            if (so.Customer != null && so.Customer.User != null)
            {
                int userId = so.Customer.User.Id;
                rewardList = AffiliateRewardsClaim.GetRewards(this.SiteDatabase, userId);
                discountCodes = AffiliateRewardsClaim.GetDiscountCodes(this.SiteDatabase, userId);
            }

            var data = new
            {
                SaleOrder = so,
                PaymentLogs = so.GetPaymentLogs(this.SiteDatabase),
                RowVerions = so.GetRowVersions(this.SiteDatabase),
                PaymentMethods = AccountingSystem.AccountingSystemModule.GetCashAccounts(this.SiteDatabase),
                InventoryRequests = this.SiteDatabase.Query<InventoryItem>().Where( i => i.SaleOrderId == so.Id ).ToList(),
                LogisticsCompanies = this.SiteDatabase.Query<LogisticsCompany>().ToList(),
                AffiliateRewardsClaims = rewardList,
                AffiliateDiscountCodes = discountCodes
            };


            return View["/Admin/saleorderdetailmanager", new StandardModel(this, dummyPage, data)];
        }

        private dynamic GetSaleorderStatusList(dynamic arg)
        {
            SaleOrderStatus SOStatus = new SaleOrderStatus();

            var SOStatusList = SOStatus.GetType().GetMembers()
                .Where(w => w.MemberType == System.Reflection.MemberTypes.Field)
                .Select(s => new { title = s.Name })
                .ToList();

            return SOStatusList;
        }

        private dynamic GetPaymentStatusList(dynamic arg)
        {
            PaymentStatus paymentStatus = new PaymentStatus();

            var paymentStatusList = paymentStatus.GetType().GetMembers()
                .Where(w => w.MemberType == System.Reflection.MemberTypes.Field)
                .Select(s => new { title = s.Name })
                .ToList();

            return paymentStatusList;
        }
    }
}