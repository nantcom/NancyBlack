using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using NantCom.NancyBlack.Modules.CommerceSystem;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using System.Text;
using NantCom.NancyBlack.Modules.MailingListSystem;
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules.AffiliateSystem
{
    public class AffiliateModule : BaseModule, IPipelineHook
    {
        public class Crc32
        {
            static uint[] table;

            private static uint ComputeChecksum(byte[] bytes)
            {
                uint crc = 0xffffffff;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    byte index = (byte)(((crc) & 0xff) ^ bytes[i]);
                    crc = (uint)((crc >> 8) ^ table[index]);
                }
                return ~crc;
            }

            /// <summary>
            /// Calculates CRC32 Checksum
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public static byte[] ComputeChecksumBytes(byte[] bytes)
            {
                return BitConverter.GetBytes(ComputeChecksum(bytes));
            }
            
            /// <summary>
            /// Calculates CRC32 Checksum in string format
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public static string ComputeChecksumString(byte[] input)
            {
                var bytes = Crc32.ComputeChecksumBytes(input);
                var code = string.Join("", bytes.Select(b => string.Format("{0:X}", b)).ToArray());

                return code;
            }

            static Crc32()
            {
                uint poly = 0xedb88320;
                table = new uint[256];
                uint temp = 0;
                for (uint i = 0; i < table.Length; ++i)
                {
                    temp = i;
                    for (int j = 8; j > 0; --j)
                    {
                        if ((temp & 1) == 1)
                        {
                            temp = (uint)((temp >> 1) ^ poly);
                        }
                        else
                        {
                            temp >>= 1;
                        }
                    }
                    table[i] = temp;
                }
            }
        }


        static AffiliateModule()
        {
            CommerceModule.PaymentCompleted += CommerceModule_PaymentCompleted;
        }

        private static void CommerceModule_PaymentCompleted(SaleOrder so, DatabaseSystem.NancyBlackDatabase db)
        {
            if (so.AffiliateCode != null)
            {
                var existing = db.Query<AffiliateRegistration>()
                                   .Where(r => r.AffiliateCode == so.AffiliateCode)
                                   .FirstOrDefault();

                if (existing == null) // wrong code?
                {
                    return;
                }

                // create a transaction
                AffiliateTransaction commission = new AffiliateTransaction();
                commission.AffiliateCode = so.AffiliateCode;
                commission.CommissionAmount = so.TotalAmount * existing.Commission;
                commission.SaleOrderId = so.Id;

                commission.BTCAddress = existing.BTCAddress;
                commission.BTCRate = BitCoinModule.GetQuote(BitCoinModule.Currency.BTC).data.low; // use the low rate from yesterday
                commission.BTCAmount = commission.CommissionAmount / commission.BTCRate;

                db.UpsertRecord(commission);
            }
        }

        public AffiliateModule()
        {
            Post["/__affiliate/apply"] = this.HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                AffiliateRegistration reg = (arg.body.Value as JObject).ToObject<AffiliateRegistration>();
                if (reg.AffiliateCode == null) // auto code
                {
                    var bytes = Encoding.ASCII.GetBytes(this.CurrentUser.Id.ToString());
                    reg.AffiliateCode = Crc32.ComputeChecksumString( bytes );
                }

                if (this.Request.Cookies.ContainsKey("source"))
                {
                    reg.RefererAffiliateCode = this.Request.Cookies["source"];
                }

                // whether user already registered
                var existing = this.SiteDatabase.Query<AffiliateRegistration>()
                                    .Where(r => r.NcbUserId == this.CurrentUser.Id)
                                    .FirstOrDefault();

                // dont replace existing code
                if (existing == null)
                {
                    reg.NcbUserId = this.CurrentUser.Id;
                    reg.Commission = 0.01M;  // start at 1 percent


                    // enroll user into Mailing List Automatically
                    NcbMailingListSubscription sub = new NcbMailingListSubscription();
                    sub.FirstName = this.CurrentUser.Profile.first_name;
                    sub.LastName = this.CurrentUser.Profile.last_name;
                    sub.Email = this.CurrentUser.Profile.email;

                    if (string.IsNullOrEmpty(sub.Email))
                    {
                        var customEmail = (arg.body.Value as JObject).Property("email").Value.ToString();
                        sub.Email = customEmail;
                    }

                    sub.RefererAffiliateCode = reg.RefererAffiliateCode;

                    this.SiteDatabase.UpsertRecord(sub);
                    this.SiteDatabase.UpsertRecord(reg);

                    return reg;
                }
                else
                {
                    return existing;
                }
                
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

                if (string.IsNullOrEmpty( registration.BTCAddress ))
                {
                    if (arg.body == null)
                    {
                        return 400;
                    }

                    var submittedAddress = arg.body.Value as JObject;
                    var address = submittedAddress.Property("btcaddress").Value.ToString();


                    if (string.IsNullOrEmpty(address) == true )
                    {
                        return 400;
                    }

                    registration.BTCAddress = address;
                    this.SiteDatabase.UpsertRecord(registration);
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
                            item.AlternateBTCAmount = item.CommissionAmount / currentRate;
                            item.IsUsingAlternateRate = true;
                        }
                        item.IsPendingApprove = true;
                        item.BTCAddress = registration.BTCAddress;

                        this.SiteDatabase.UpsertRecord(item);
                    }

                });

                return 200;
            });

            Post["/__affiliate/getrewards"] = this.HandleRequest((arg) =>
            {   
                var registration = this.SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == this.CurrentUser.Id).FirstOrDefault();

                if (registration == null)
                {
                    return 400;
                }

                dynamic param = (arg.body.Value as JObject);
                if (param.rewardsName == null)
                {
                    return 400;
                }

                if (param.rewardsName == "subscribe1")
                {
                    var sub = this.SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM NcbMailingListSubscription WHERE RefererAffiliateCode=?",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode }).First().Count;

                    if (sub < 5 )
                    {
                        return new
                        {
                            type = "warning",
                            title = "ขอโทษนะ",
                            text = "ตอนนี้จำนวนคนสมัครรับข่าวมีแค่ " + sub + " คน ยังไม่ครบ 5 คนเลย",
                        };
                    }

                    var claim = this.SiteDatabase.QueryAsDynamic("SELECT DiscountCode FROM AffiliateRewardsClaim WHERE AffiliateCode=? AND RewardsName=?",
                                new { DiscountCode = "" },
                                new object[] { registration.AffiliateCode, "subscribe1" }).FirstOrDefault();

                    if (claim == null)
                    {

                        var bytes = Encoding.ASCII.GetBytes("sub1code" + this.CurrentUser.Id.ToString());
                        var code = Crc32.ComputeChecksumString(bytes);

                        Product p = new Product();
                        p.Url = "/promotions/code/" + code;
                        p.Title = "Affiliate Discount: Subscribe 5 Friends";
                        p.Price = -2000;
                        p.Attributes = new
                        {
                            description = "โค๊ดส่วนลดพิเศษสำหรับคุณ " + this.CurrentUser.Profile.first_name + " จำนวน 2,000 บาท เมื่อสั่งซื้อขั้นต่ำ 32,000 บาท",
                            limit = "32000",
                            onetime = true
                        };
                        
                        claim = new AffiliateRewardsClaim();
                        claim.AffiliateCode = registration.AffiliateCode;
                        claim.DiscountCode = code;

                        this.SiteDatabase.UpsertRecord(p);
                        this.SiteDatabase.UpsertRecord(claim);
                    }
                    
                    return new
                    {
                        type = "success",
                        title = "เยี่ยมกู๊ด",
                        text = "ขอบคุณมากๆ โค๊ดส่วนลดของคุณคือ: <b>" + claim.DiscountCode + "</b> เก็บไว้ดีๆ ละ",
                        html = true
                    };

                }
                
                if (param.rewardsName == "subscribe2")
                {
                    var sub = this.SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM NcbMailingListSubscription WHERE RefererAffiliateCode=?",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode }).First().Count;

                    if (sub < 10)
                    {
                        return new
                        {
                            type = "warning",
                            title = "ขอโทษนะ",
                            text = "ตอนนี้จำนวนคนสมัครรับข่าวมีแค่ " + sub + " คน ยังไม่ครบ 10 คนเลย",
                        };
                    }

                    var claim = this.SiteDatabase.QueryAsDynamic("SELECT DiscountCode FROM AffiliateRewardsClaim WHERE AffiliateCode=? AND RewardsName=?",
                                new { DiscountCode = "" },
                                new object[] { registration.AffiliateCode, "subscribe2" }).FirstOrDefault();

                    if (claim == null)
                    {

                        claim = new AffiliateRewardsClaim();
                        claim.AffiliateCode = registration.AffiliateCode;
                        claim.RewardsName = "subscribe2";
                        
                        this.SiteDatabase.UpsertRecord(claim);

                    }
                    return new
                    {
                        type = "success",
                        title = "เยี่ยมกู๊ด",
                        text = "ขอบคุณมากๆ เราได้รับคำขอรับขอของคุณแล้วนะ กรุณารอเราติดต่อกลับแป๊บนึง",
                        html = true
                    };
                }


                if (param.rewardsName == "buy1")
                {
                    var sub = this.SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM SaleOrder WHERE AffiliateCode=? AND PaymentStatus='PaymentReceived'",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode }).First().Count;

                    if (sub == 0)
                    {
                        return new
                        {
                            type = "warning",
                            title = "ขอโทษนะ",
                            text = "ยังไม่มีคนมาซื้อเลยนี่นา",
                        };
                    }
                    
                    var claim = this.SiteDatabase.QueryAsDynamic("SELECT DiscountCode FROM AffiliateRewardsClaim WHERE AffiliateCode=? AND RewardsName=?",
                                new { DiscountCode = "" },
                                new object[] { registration.AffiliateCode, "buy1" }).FirstOrDefault();

                    if (claim == null)
                    {

                        claim = new AffiliateRewardsClaim();
                        claim.AffiliateCode = registration.AffiliateCode;
                        claim.RewardsName = "buy1";
                        
                        this.SiteDatabase.UpsertRecord(claim);

                    }
                    return new
                    {
                        type = "success",
                        title = "เยี่ยมกู๊ด",
                        text = "ขอบคุณมากๆ เราได้รับคำขอรับขอของคุณแล้วนะ กรุณารอเราติดต่อกลับแป๊บนึง",
                        html = true
                    };
                }


                return 200;

            });

            Get["/squad51"] = this.HandleViewRequest("affiliate-apply", (arg) =>
            {
                var registration = this.SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == this.CurrentUser.Id).FirstOrDefault();

                var content = ContentModule.GetPage(this.SiteDatabase, "/__affiliate", true);
                return new StandardModel(this, content, new
                {
                    Registration = registration
                });
            });

            Get["/squad51/dashboard"] = this.HandleRequest((arg) =>
            {
                var content = ContentModule.GetPage(this.SiteDatabase, "/__affiliate", true);

                AffiliateRegistration registration = null;
                if (this.Request.Query.code == null)
                {
                    if (this.CurrentUser.IsAnonymous)
                    {
                        return Response.AsRedirect("/squad51");
                    }

                    registration = this.SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == this.CurrentUser.Id).FirstOrDefault();
                }
                else
                {
                    var code = (string)this.Request.Query.code;
                    registration = this.SiteDatabase.Query<AffiliateRegistration>()
                        .Where(t => t.AffiliateCode == code).FirstOrDefault();
                }
                

                var standardModel = new StandardModel(200);
                standardModel.Content = content;


                if (registration != null)
                {
                    var key = "dashboard-" + this.CurrentUser.Id;
                    object dashboardData = MemoryCache.Default[key];

                    if (dashboardData == null)
                    {
                        dashboardData = new
                        {
                            Registration = registration,
                            Code = registration.AffiliateCode,
                            RelatedOrders = this.SiteDatabase.Query("SELECT COUNT(Id) As Count, PaymentStatus FROM SaleOrder WHERE AffiliateCode=? GROUP BY PaymentStatus",
                            new { Count = 0, PaymentStatus = "" },
                            new object[] { registration.AffiliateCode }).ToList(),

                            PendingCommission = this.SiteDatabase.Query("SELECT * FROM AffiliateTransaction WHERE AffiliateCode=? AND IsCommissionPaid=0 AND IsPendingApprove=0",
                            new AffiliateTransaction(),
                            new object[] { registration.AffiliateCode }).ToList(),

                            Traffic = this.SiteDatabase.Query("SELECT COUNT(Id) As Count, Path FROM PageView WHERE AffiliateCode=? GROUP BY Path",
                            new { Count = 0, Path = "" },
                            new object[] { registration.AffiliateCode }).ToList(),

                            CommissionTotal = this.SiteDatabase.QueryAsDynamic("SELECT SUM(BTCAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=?",
                            new { Amount = 0M },
                            new object[] { registration.AffiliateCode }).First().Amount,

                            CommissionPaid = this.SiteDatabase.QueryAsDynamic("SELECT SUM(BTCAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=? AND IsCommissionPaid=1",
                            new { Amount = 0M },
                            new object[] { registration.AffiliateCode }).First().Amount,

                            CommissionPendingApproval = this.SiteDatabase.QueryAsDynamic("SELECT SUM(BTCAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=? AND IsPendingApprove=1",
                            new { Amount = 0M },
                            new object[] { registration.AffiliateCode }).First().Amount,

                            Products = this.SiteDatabase.Query<Product>().Where(p => p.Url.StartsWith("/products/laptops") || p.Url.StartsWith("/products/pc")).ToList()
                                    .Select(p => new
                                    {
                                        Title = p.Title,
                                        Url = p.Url,
                                        Picture = (from a in p.Attachments
                                                   where a.AttachmentType == "default-image"
                                                   select a.Url).FirstOrDefault()
                                    }).ToList(),

                            SubscribeClick = this.SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM PageView WHERE AffiliateCode=? AND QueryString=?",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode, "?subscribe=1&source=" + registration.AffiliateCode}).First().Count,

                            SubscribeAll = this.SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM NcbMailingListSubscription WHERE RefererAffiliateCode=?",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode }).First().Count,

                        };

                        MemoryCache.Default.Add(key, dashboardData, DateTimeOffset.Now.AddMinutes(10));
                    }

                    standardModel.Data = dashboardData;
                }


                return View["affiliate-dashboard", standardModel]; ;
            });
        }

        public void Hook(IPipelines p)
        {
            p.BeforeRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (ctx.Request.Query.source != null )
                {
                    if (ctx.Request.Cookies.ContainsKey("source") == false)
                    {
                        ctx.Request.Cookies.Add("source", (string)ctx.Request.Query.source.Value);
                    }
                    else
                    {
                        ctx.Request.Cookies["source"] = ctx.Request.Query.source.Value;
                    }
                }

                
                return null;
            });

            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (ctx.Request.Cookies.ContainsKey("source"))
                {
                    ctx.Response.Cookies.Add(
                        new NancyCookie("source", ctx.Request.Cookies["source"], DateTime.Now.AddDays(7)));
                }
            });
        }
    }
}