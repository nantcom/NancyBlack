using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
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

            Get["/admin/saleorder/{id}/add/{productId}"] = this.HandleRequest(this.AddProductToSaleOrder);

            Get["/admin/saleorder/{id}/remove/{productId}"] = this.HandleRequest(this.RemoveProductFromSaleOrder);

            Get["/admin/commerce/api/sostatus"] = this.HandleRequest(this.GetSaleorderStatusList);

            Get["/admin/commerce/api/paymentstatus"] = this.HandleRequest(this.GetPaymentStatusList);

            Get["/admin/commerce/api/printing/saleorder/current/month/list"] = this.HandleRequest(this.GetSaleorderForPrintingReceiptList);
            
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
                Items = new int[0]
            };
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

            so.AddItem(this.SiteDatabase, this.CurrentSite, (int)arg.productId);

            return 200;
        }

        private dynamic RemoveProductFromSaleOrder(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            return 404;
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

            var dummyPage = new Page();

            var data = new
            {
                SaleOrder = so,
                PaymentLogs = so.GetPaymentLogs(this.SiteDatabase),
                RowVerions = so.GetRowVersions(this.SiteDatabase),
                PaymentMethods = StatusList.GetAllStatus<PaymentMethod>()
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