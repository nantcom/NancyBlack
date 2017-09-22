using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using NantCom.NancyBlack.Modules.CommerceSystem;
using NantCom.NancyBlack.Modules.CommerceSystem.types;

namespace NantCom.NancyBlack.Modules.AffiliateSystem
{
    public class AffiliateModule : BaseModule, IPipelineHook
    {

        public AffiliateModule()
        {
            Post["/__affiliate/apply"] = this.HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                AffiliateRegistration reg = (arg.body.Value as JObject).ToObject<AffiliateRegistration>();
                var existing = this.SiteDatabase.Query<AffiliateRegistration>()
                                    .Where(r => r.NcbUserId == this.CurrentUser.Id)
                                    .FirstOrDefault();

                // dont replace existing code
                if (existing != null)
                {
                    return 400;
                }

                existing = this.SiteDatabase.Query<AffiliateRegistration>()
                                    .Where(r => r.AffiliateCode == reg.AffiliateCode)
                                    .FirstOrDefault();

                // dont replace existing code
                if (existing != null)
                {
                    return 400;
                }


                reg.NcbUserId = this.CurrentUser.Id;
                reg.Commission = 0.01M;  // start at 1 percent

                this.SiteDatabase.UpsertRecord<AffiliateRegistration>(reg);
                
                return reg;
            });

            Post["/__affiliate/requestpayment"] = this.HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                var registration = this.SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == this.CurrentUser.Id).FirstOrDefault();

                if (registration == null)
                {
                    return 400;
                }

                var pendingPayment =this.SiteDatabase.Query<AffiliateTransaction>()
                                        .Where(t => t.AffiliateCode == registration.AffiliateCode && t.IsCommissionPaid == false && t.IsPendingApprove == false)
                                        .ToList();

                var averageRate = pendingPayment.Average(t => t.BTCRate);
                Decimal currentRate = BitCoinModule.GetQuote(BitCoinModule.Currency.BTC, DateTime.Now, false).data.low;

                // using alternate rate
                this.SiteDatabase.Transaction(() =>
                {
                    foreach (var item in pendingPayment)
                    {
                        if (currentRate / averageRate > 1.05M)
                        {
                            item.AlternateBTCRate = currentRate;
                            item.AlternateBTCAmount = item.BTCAmount * (item.BTCRate / currentRate);
                            item.IsUsingAlternateRate = true;
                        }
                        item.IsPendingApprove = true;

                        this.SiteDatabase.UpsertRecord(item);
                    }

                });

                return 200;
            });

            Get["/affiliate"] = this.HandleViewRequest("affiliate-dashboard", (arg) =>
            {
                var content = ContentModule.GetPage(this.SiteDatabase, "/__affiliate", true);
                
                if (this.CurrentUser.IsAnonymous)
                {
                    return new StandardModel(this, content);
                }

                var registration = this.SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == this.CurrentUser.Id).FirstOrDefault();
                

                var standardModel = new StandardModel(200);
                standardModel.Content = content;


                if (registration != null)
                {
                    standardModel.Data = new
                    {
                        Code = registration.AffiliateCode,
                        RelatedOrders = this.SiteDatabase.Query("SELECT COUNT(Id) As Count, PaymentStatus FROM SaleOrder WHERE AffiliateCode=? GROUP BY PaymentStatus",
                            new { Count = "", PaymentStatus = "" },
                            new object[] { registration.AffiliateCode }),

                        PendingCommission = this.SiteDatabase.Query("SELECT * FROM AffiliateTransaction WHERE AffiliateCode=? AND IsCommissionPaid=0 AND IsPendingApprove=0",
                            new AffiliateTransaction(),
                            new object[] { registration.AffiliateCode }),

                        Traffic = this.SiteDatabase.Query("SELECT COUNT(Id) As Count, Path FROM PageView WHERE Source=? GROUP BY Path",
                            new { Count = "", Path = "" },
                            new object[] { registration.AffiliateCode }),

                        CommissionTotal = this.SiteDatabase.QueryAsDynamic("SELECT SUM(BTCAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=?",
                            new { Amount = "" },
                            new object[] { registration.AffiliateCode }).First().Amount,

                        CommissionPaid = this.SiteDatabase.QueryAsDynamic("SELECT SUM(BTCAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=? AND IsCommissionPaid=1",
                            new { Amount = "" },
                            new object[] { registration.AffiliateCode }).First().Amount,

                        CommissionPendingApproval = this.SiteDatabase.QueryAsDynamic("SELECT SUM(BTCAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=? AND IsPendingApprove=1",
                            new { Amount = "" },
                            new object[] { registration.AffiliateCode }).First().Amount,

                        Products = this.SiteDatabase.Query<Product>().Where(p => p.Url.StartsWith("/products/laptops") || p.Url.StartsWith("/products/pc")).ToList()
                                    .Select( p => new
                                    {
                                        Title = p.Title,
                                        Url = p.Url,
                                        Picture = ( from a in p.Attachments
                                                    where a.AttachmentType == "default-image"
                                                    select a.Url ).FirstOrDefault()
                                    })
                    };
                }


                return standardModel;
            });
        }

        public void Hook(IPipelines p)
        {
            p.BeforeRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (ctx.Request.Cookies.ContainsKey("source") == false)
                {
                    ctx.Request.Cookies.Add( "source", (string)ctx.Request.Query.source.Value );
                }
                
                return null;
            });

            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (ctx.Request.Cookies.ContainsKey("source"))
                {
                    ctx.Response.Cookies.Add(
                        new NancyCookie("source", ctx.Request.Cookies["source"], DateTime.Now.AddDays(1)));
                }
            });
        }
    }
}