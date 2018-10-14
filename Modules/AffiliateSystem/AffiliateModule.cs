﻿using Manatee.Trello;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using NantCom.NancyBlack.Modules.CommerceSystem;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using NantCom.NancyBlack.Modules.FacebookMessengerSystem;
using NantCom.NancyBlack.Modules.MailingListSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace NantCom.NancyBlack.Modules.AffiliateSystem
{
    public partial class AffiliateModule : BaseModule, IPipelineHook
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

        private static string TemplatePath;

        static AffiliateModule()
        {
            CommerceModule.PaymentSuccess += CommerceModule_PaymentSuccess;
            NancyBlackDatabase.ObjectCreated += NancyBlackDatabase_ObjectCreated;
        }

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string table, dynamic created)
        {
            if (table == "NcbMailingListSubscription")
            {
                string affiliateCode = created.RefererAffiliateCode;
                var registration = db.Query<AffiliateRegistration>().Where(r => r.AffiliateCode == affiliateCode).FirstOrDefault();

                if (registration == null)
                {
                    return;
                }

                var user = db.GetById<NcbUser>(registration.NcbUserId);
                if (user == null)
                {
                    return;
                }

                int sub = db.QueryAsDynamic("SELECT COUNT(Id) As Count FROM NcbMailingListSubscription WHERE RefererAffiliateCode=?",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode }).First().Count;


                var path = Path.Combine(AffiliateModule.TemplatePath, "Affiliate-NewSubscription.html");
                string emailBody = File.ReadAllText(path);

                var message = "เพียงแค่อีก {{To5}} เรามีโค๊ดส่วนลดแจกให้คุณ <b>2,000 บาท</b> แล้วก็ถ้ามีเพื่อนคุณมาสมัครรับข่าวจากเราอีกแค่ {{To10}} คนละก็ รับไปเลย กระเป๋า SWISSGEAR เวอร์ชั่น LEVEL51 มีแค่ 200 ใบในโลก <b>มูลค่า 2,790 บาท</b> นะจ๊ะ";

                if (sub >= 5)
                {
                    message = "ตอนนี้มีคนมาสมัครครบ 5 คนแล้ว คลิกเข้าไปที่ Dashboard เพื่อขอโค๊ดลด <b>2,000 บาท</b> ของคุณได้เลย และถ้ามีเพื่อนมาอีก {{To10}}  คนละก็ รับไปเลย กระเป๋า SWISSGEAR เวอร์ชั่น LEVEL51 มีแค่ 200 ใบในโลก <b>มูลค่า 2,790 บาท</b> นะจ๊ะ";
                }

                if (sub >= 10)
                {
                    message = "ตอนนี้มีคนมาสมัครครบ 5 คนแล้ว คลิกเข้าไปที่ Dashboard เพื่อขอโค๊ดลด <b>2,000 บาท</b> ของคุณได้เลย และก็รอรับกระเป๋า SWISSGEAR เวอร์ชั่น LEVEL51 มีแค่ 200 ใบในโลก <b>มูลค่า 2,790 บาท</b> อยู่ที่บ้านได้เลย เราจะติดต่อไปนะจ๊ะ";
                }

                emailBody = emailBody.Replace("{{SubscriberTotal}}", sub.ToString());
                emailBody = emailBody.Replace("{{ConvinceMessage}}", message.Replace("{{To5}}", (5 - sub).ToString()).Replace("{{To10}}", (10 - sub).ToString()));
                emailBody = emailBody.Replace("{{Code}}", registration.AffiliateCode);

                MailSenderModule.SendEmail(user.Email, "We have new subscriber thanks to you!", emailBody);
            }
        }

        private static void CommerceModule_PaymentSuccess(SaleOrder so, DatabaseSystem.NancyBlackDatabase db)
        {
            if (so.AffiliateCode == null)
            {
                return;
            }

            if (so.PaymentStatus != PaymentStatus.PaymentReceived)
            {
                return;
            }

            var existing = db.Query<AffiliateTransaction>().Where(t => t.SaleOrderId == so.Id).FirstOrDefault();
            if (existing != null)
            {
                return;
            }

            var registration = db.Query<AffiliateRegistration>()
                               .Where(r => r.AffiliateCode == so.AffiliateCode)
                               .FirstOrDefault();

            if (registration == null) // wrong code?
            {
                return;
            }

            var oldRate = registration.Commission;
            {
                // calculate commission  rate based on number of transaction
                int count = db.QueryAsDynamic("SELECT COUNT(Id) As Count FROM AffiliateTransaction WHERE AffiliateCode=?",
                                new { Count = 0 },
                                new object[] { so.AffiliateCode }).First().Count;

                if (count >= 2)
                {
                    registration.Commission = 0.02M;
                }

                if (count >= 6)
                {
                    registration.Commission = 0.05M;
                }
            }

            {
                // and also number of unique ip
                int ipCount = db.QueryAsDynamic("SELECT COUNT(DISTINCT UserIP) As Count FROM PageView WHERE AffiliateCode=?",
                                new { Count = 0 },
                                new object[] { so.AffiliateCode }).First().Count;

                var ipCount10000 = (ipCount / 10000);
                var rate = (ipCount10000 / 100d) + 0.01;

                if (rate > 0.05)
                {
                    rate = 0.05;
                }

                if (rate > (double)registration.Commission)
                {
                    registration.Commission = (Decimal)rate;
                }
            }

            if (registration.Commission > oldRate)
            {
                db.UpsertRecord(registration);
            }

            // create a transaction
            AffiliateTransaction commission = new AffiliateTransaction();
            commission.AffiliateCode = so.AffiliateCode;
            commission.CommissionAmount = so.TotalAmount * registration.Commission;
            commission.SaleOrderId = so.Id;

            commission.BTCAddress = registration.BTCAddress;
            db.UpsertRecord(commission);
            {
                var user = db.GetById<NcbUser>(registration.NcbUserId);
                if (user == null)
                {
                    return;
                }

                var path = Path.Combine(AffiliateModule.TemplatePath, "Affiliate-NewPaidOrder.html");
                string emailBody = File.ReadAllText(path);

                emailBody = emailBody.Replace("{{CommissionAmount}} ", commission.CommissionAmount.ToString("#,#"));
                emailBody = emailBody.Replace("{{Code}}", registration.AffiliateCode);

                MailSenderModule.SendEmail(user.Email, "We have new Sales thanks to you!", emailBody);
            }

        }

        /// <summary>
        /// Adds rewards card to trello
        /// </summary>
        /// <param name="claim"></param>
        private void AddRewardsCard(dynamic siteSettings, AffiliateRewardsClaim claim, bool createCard = true)
        {
            string title = "SQUAD51: ";
            if (claim.RewardsName == "subscribe2")
            {
                title += string.Format("กระเป๋า (MemberId: {0}, {1} )", CurrentUser.Id, claim.AffiliateCode);
            }

            else if (claim.RewardsName == "buy1")
            {
                title += string.Format("เมาส์ (MemberId: {0}, {1} )", CurrentUser.Id, claim.AffiliateCode);
            }

            if (title == "SQUAD51: ")
            {
                return;
            }

            var auth = new TrelloAuthorization();
            auth.AppKey = siteSettings.trello.AppKey;
            auth.UserToken = siteSettings.trello.UserToken;

            var list = new List("5a58d3dc2c472bd17a4b7901", auth); // จัดส่ง

            // find existsing
            var card = list.Cards.Where(c => c.Name == title).FirstOrDefault();
            if (card == null)
            {
                if (createCard == false)
                {
                    return;
                }

                //if we use ist.Cards.Add(title).Result server will not response and instructions will not be complated
                Task t = Task.Run(async () =>
               {
                   var myCard = await list.Cards.Add(title);
                   card = myCard;
               });

                t.Wait();
            }

            card.DueDate = claim.__createdAt.AddDays(7);

            var profile = SiteDatabase.GetById<NcbUser>(claim.NcbUserId).Profile;

            var cardDescription =
                "Rewards: " + claim.RewardsName + "\r\n\r\n" +
                profile.first_name + " " + profile.last_name + "\r\n" +
                "Tel: " + profile.phone + "\r\n" +
                "Email: " + profile.email + "\r\n\r\n";


            if (profile.address != null)
            {
                cardDescription +=
                    "Address:\r\n" +
                    profile.address.Address1 + "\r\n" +
                    profile.address.Address2 + "\r\n" +
                    profile.address.Subdistrict + "\r\n" +
                    profile.address.District + "\r\n" +
                    profile.address.Province + "\r\n" +
                    profile.address.Country + "\r\n" +
                    profile.address.PostalCode;
            }

            card.Description = cardDescription;
            if (card.Labels.Where((l) => l.Id == "5a5f92592a473c65883fa71a").Count() == 0)
            {
                var board = new Board("59e89f443cfc9dcef8afa098", auth);

                var label = (from l in board.Labels
                             where l.Id == "5a5f92592a473c65883fa71a"
                             select l).FirstOrDefault();

                // recently somehow this code could not get member in board.Labels (got .Count == 0) 
                if (label != null)
                {
                    card.Labels.Add(label);
                }

            }

        }

        /// <summary>
        /// Adds rewards card to trello
        /// </summary>
        /// <param name="claim"></param>
        private void AddPayoutCard(dynamic siteSettings, AffiliateTransaction transaction, bool createCard = true)
        {
            string title = "Payout: ( " + transaction.AffiliateCode + "," + transaction.Id + " )";

            var auth = new TrelloAuthorization();
            auth.AppKey = siteSettings.trello.AppKey;
            auth.UserToken = siteSettings.trello.UserToken;

            var list = new List("5a5f955a370236fdb163a8b5", auth); // โอนเงิน

            // find existsing
            var card = list.Cards.Where(c => c.Name == title).FirstOrDefault();
            if (card == null)
            {
                if (createCard == false)
                {
                    return;
                }

                //if we use ist.Cards.Add(title).Result server will not response and instructions will not be complated
                Task t = Task.Run(async () =>
                {
                    var myCard = await list.Cards.Add(title);
                    card = myCard;
                });

                t.Wait();
            }

            if (transaction.IsPendingApprove)
            {
                card.DueDate = transaction.__updatedAt.AddDays(7);
            }

            var user = SiteDatabase.GetById<NcbUser>(transaction.NcbUserId);
            if (user == null)
            {
                card.Description = "Error: User Not Found. Id: " + transaction.NcbUserId;
                return;
            }

            var profile = SiteDatabase.GetById<NcbUser>(transaction.NcbUserId).Profile;
            var reg = SiteDatabase.GetById<AffiliateRegistration>(transaction.AffiliateRegistrationId);
            var cardDescription =
                "Sale Order: " + transaction.SaleOrderId + "\r\n\r\n" +
                profile.first_name + " " + profile.last_name + "\r\n" +
                "Tel: " + profile.phone + "\r\n" +
                "Email: " + profile.email + "\r\n\r\n" +
                "Amount THB:" + transaction.CommissionAmount + "\r\n" +
                "BTC Address:" + reg.BTCAddress;

            card.Description = cardDescription;

            if (card.Labels.Where((l) => l.Id == "5a5f93bd84565fbcba08f536").Count() == 0) //Ops Label
            {
                var board = new Board("59e89f443cfc9dcef8afa098", auth);
                var label = (from l in board.Labels
                             where l.Id == "5a5f93bd84565fbcba08f536"
                             select l).FirstOrDefault();

                // recently somehow this code could not get member in board.Labels (got .Count == 0) 
                if (label != null)
                {
                    card.Labels.Add(label);
                }
            }

        }

        public const string AFFILIATE_PROGRAM_NAME = "squad51"; 
        
        public AffiliateModule()
        {
            AffiliateModule.TemplatePath = Path.Combine(RootPath, "Site", "Views", "EmailTemplates");

            Post["/__affiliate/apply"] = HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                return AffiliateModule.ApplyAffiliate( this.SiteDatabase, this.CurrentUser.Id);
            });

            Post["/__affiliate/requestpayment"] = HandleRequest((arg) =>
            {
                if (CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                var registration = SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == CurrentUser.Id).FirstOrDefault();

                if (registration == null)
                {
                    return 400;
                }

                if (string.IsNullOrEmpty(registration.BTCAddress))
                {
                    if (arg.body == null)
                    {
                        return 400;
                    }

                    var submittedAddress = arg.body.Value as JObject;
                    var address = submittedAddress.Property("btcaddress").Value.ToString();


                    if (string.IsNullOrEmpty(address) == true)
                    {
                        return 400;
                    }

                    registration.BTCAddress = address;
                    SiteDatabase.UpsertRecord(registration);
                }

                var pendingPayment = SiteDatabase.Query<AffiliateTransaction>()
                                        .Where(t => t.AffiliateCode == registration.AffiliateCode && t.IsCommissionPaid == false && t.IsPendingApprove == false)
                                        .ToList();


                // using alternate rate
                SiteDatabase.Transaction(() =>
                {
                    foreach (var item in pendingPayment)
                    {
                        item.IsPendingApprove = true;
                        item.BTCAddress = registration.BTCAddress;

                        SiteDatabase.UpsertRecord(item);
                        this.AddPayoutCard(CurrentSite, item);
                    }

                });



                return 200;
            });

            Post["/__affiliate/claimrewards"] = HandleRequest((arg) =>
            {
                dynamic param = (arg.body.Value as JObject);
                if (param.Id == null)
                {
                    return 400;
                }

                var registration = AffiliateModule.ApplyAffiliate(this.SiteDatabase, this.CurrentUser.Id);
                
                var result = AffiliateReward.ClaimReward(this.SiteDatabase, (int)param.Id, registration.Id);
                if (result == null)
                {
                    return 403;
                }

                return result;

            });

            Post["/__affiliate/addtosaleorder"] = HandleRequest((arg) =>
            {
                if (CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                var requestBody = (arg.body.Value as JObject);
                var saleOrderId = requestBody.Value<int>("saleOrderId");
                var affiliateRewardsClaimId = requestBody.Value<int>("arcId");

                if (saleOrderId != 0 && affiliateRewardsClaimId != 0)
                {
                    var aRC = SiteDatabase.GetById<AffiliateRewardsClaim>(affiliateRewardsClaimId);

                    if (aRC.IncludedInSaleOrderId != 0 || aRC.DiscountCode != null)
                    {
                        return 403;
                    }

                    aRC.IncludedInSaleOrderId = saleOrderId;
                    SiteDatabase.UpsertRecord(aRC);

                    return new
                    {
                        AffiliateRewardsClaim = aRC
                    };
                }


                return 403;

            });

            Post["/__affiliate/updateprofile"] = HandleRequest((arg) =>
            {
                var requestBody = arg.body.Value;

                if (requestBody.Profile == null && requestBody.Registration == null)
                {
                    return 400;
                }

                // Impersonation in effect
                int userId = this.CurrentUser.Id;
                if (userId != (int)requestBody.UserId)
                {
                    if (userId == 1)
                    {
                        userId = (int)requestBody.UserId;
                    }
                    else
                    {
                        return 400; // user that is not 1 cannot save with impersonation
                    }
                }

                if (requestBody.Profile != null)
                {
                    UserManager.Current.UpdateProfile(this.SiteDatabase, userId, requestBody.Profile);
                }

                if (requestBody.Registration != null)
                {
                    AffiliateRegistration registration = SiteDatabase.Query<AffiliateRegistration>()
                                                           .Where(t => t.NcbUserId == CurrentUser.Id).FirstOrDefault();

                    registration.AffiliateName = requestBody.Registration.AffiliateName;
                    registration.AffiliateMessage = requestBody.Registration.AffiliateMessage;
                    SiteDatabase.UpsertRecord(registration);

                    MemoryCache.Default.Remove("AffiliateReg-" + registration.AffiliateCode);

                    // update all trello cards on the board
                    var claims = SiteDatabase.Query<AffiliateRewardsClaim>().Where(c => c.NcbUserId == registration.NcbUserId);
                    foreach (var claim in claims)
                    {
                        this.AddRewardsCard(CurrentSite, claim, false); // Update card if found
                    }
                }

                MemoryCache.Default.Remove("dashboard-" + CurrentUser.Id);


                return 200;

            });

            Get["/" + AFFILIATE_PROGRAM_NAME + "/dashboard"] = HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return this.Response.AsRedirect("/" + AFFILIATE_PROGRAM_NAME);
                }

                var id = this.CurrentUser.Id;
                AffiliateRegistration registration = null;

                if (this.CurrentUser.HasClaim("admin") && Request.Query.code != null) // admin impersonate anyone
                {
                    var code = (string)Request.Query.code;
                    registration = SiteDatabase.Query<AffiliateRegistration>()
                        .Where(t => t.AffiliateCode == code).FirstOrDefault();

                    if (registration == null)
                    {
                        return 404; // wrong code
                    }
                }
                else if (this.CurrentUser.HasClaim("admin") && Request.Query.so != null)
                {
                    var soId = (int)Request.Query.so;
                    var so = SiteDatabase.GetById<SaleOrder>(soId);

                    if (so.NcbUserId == 0)
                    {
                        return 404; // user cannot view squad51 because they was not registered
                    }

                    registration = SiteDatabase.Query<AffiliateRegistration>()
                        .Where(t => t.NcbUserId == so.NcbUserId).FirstOrDefault();

                    // automatically apply owner of given so
                    if (registration == null &&
                        (so.PaymentStatus == PaymentStatus.PaymentReceived ||
                         so.PaymentStatus == PaymentStatus.Deposit))
                    {
                        registration = AffiliateModule.ApplyAffiliate(this.SiteDatabase, so.NcbUserId);
                    }
                }
                else
                {
                    if (id != 0)
                    {
                        registration = SiteDatabase.Query<AffiliateRegistration>()
                            .Where(t => t.NcbUserId == id).FirstOrDefault();
                    }
                }

                if (registration == null)
                {
                    if (id != 0) // some known user but we still can't get their registration
                    {
                        // no registration - try to see whether this user already a customer
                        var saleOrder = this.SiteDatabase.Query<SaleOrder>()
                                            .Where(so => so.NcbUserId == id &&
                                                         (so.PaymentStatus == PaymentStatus.Deposit ||
                                                          so.PaymentStatus == PaymentStatus.PaymentReceived))
                                            .FirstOrDefault();

                        // already customer - auto register them
                        if (saleOrder != null)
                        {
                            registration = AffiliateModule.ApplyAffiliate(this.SiteDatabase, id);
                            return this.AffiliateDashboard(registration, arg);
                        }
                    }

                    // redirect back to application page
                    return this.Response.AsRedirect("/" + AFFILIATE_PROGRAM_NAME);
                }


                return this.AffiliateDashboard(registration, arg);

            });

            Get["/" + AFFILIATE_PROGRAM_NAME] = HandleRequest((arg) =>
            {
                var id = this.CurrentUser.Id;

                AffiliateRegistration registration = SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == id).FirstOrDefault();

                var content = ContentModule.GetPage(SiteDatabase, "/__affiliate", true);
                return View["affiliate-apply", new StandardModel(this, content, new
                {
                    Registration = registration
                })];

            });

            Get["/__affiliate/profileimage/{id}"] = this.HandleRequest(arg =>
            {
                var response = new Response();
                response.ContentType = "image/jpeg";
                response.Contents = (output) =>
                {
                    WebClient client = new WebClient();
                    var data = client.DownloadData("https://graph.facebook.com/" + (string)arg.id + "/picture?type=large");
                    output.Write(data, 0, data.Length);
                };
                return response;

            });

            Get["/__affiliate/syncrewards"] = HandleRequest((arg) =>
            {
                // sync rewards request to trello
                var claims = SiteDatabase.Query<AffiliateRewardsClaim>().ToList();

                // temp - fill in data
                foreach (var claim in claims)
                {
                    if (claim.NcbUserId != 0)
                    {
                        continue;
                    }
                    var reg = SiteDatabase.Query<AffiliateRegistration>().Where(r => r.AffiliateCode == claim.AffiliateCode).FirstOrDefault();
                    if (reg == null)
                    {
                        continue;
                    }


                    claim.NcbUserId = reg.NcbUserId;
                    claim.AffiliateRegistrationId = reg.Id;

                    SiteDatabase.Connection.Update(claim);
                }

                foreach (var claim in claims)
                {
                    if (claim.IsSent == true)
                    {
                        continue;
                    }

                    this.AddRewardsCard(CurrentSite, claim).Result();
                }

                return "OK";
            });

            Get["/__affiliate/synctransaction"] = HandleRequest((arg) =>
            {
                // sync rewards request to trello
                var trans = SiteDatabase.Query<AffiliateTransaction>().ToList();

                // temp - fill in data
                foreach (var t in trans)
                {
                    if (t.NcbUserId != 0)
                    {
                        continue;
                    }

                    var reg = SiteDatabase.Query<AffiliateRegistration>().Where(r => r.AffiliateCode == t.AffiliateCode).FirstOrDefault();
                    if (reg == null)
                    {
                        continue;
                    }


                    t.NcbUserId = reg.NcbUserId;
                    t.AffiliateRegistrationId = reg.Id;

                    SiteDatabase.Connection.Update(t);
                }

                foreach (var t in trans)
                {
                    this.AddPayoutCard(CurrentSite, t);
                }

                return "OK";
            });

            //Get["/__affiliate/registereveryone"] = HandleRequest((arg) =>
            //{
            //    foreach (var user in this.SiteDatabase.Query<NcbUser>().AsEnumerable())
            //    {
            //        AffiliateModule.ApplyAffiliate(this.SiteDatabase, user.Id);
            //    }

            //    return "OK";
            //});

        }

        public void Hook(IPipelines p)
        {
            p.BeforeRequest.AddItemToEndOfPipeline((ctx) =>
            {
                string code = ctx.Request.Query.source;
                if (code == null && ctx.Request.Cookies.ContainsKey("source"))
                {
                    code = ctx.Request.Cookies["source"];
                }

                if (!string.IsNullOrEmpty(code))
                {
                    AffiliateRegistration reg = MemoryCache.Default["AffiliateReg-" + code] as AffiliateRegistration;
                    if (reg == null)
                    {
                        reg = ctx.GetSiteDatabase().Query<AffiliateRegistration>()
                                    .Where(ar => ar.AffiliateCode == code)
                                    .FirstOrDefault();

                        if (reg != null)
                        {
                            MemoryCache.Default.Add("AffiliateReg-" + code, reg, DateTimeOffset.Now.AddMinutes(15));
                        }

                        ctx.Items["AffiliateReg"] = reg;
                    }

                    if (reg != null)
                    {
                        ctx.Items["AffiliateReg"] = reg;

                        if (ctx.Request.Cookies.ContainsKey("affiliatename") == false)
                        {
                            ctx.Request.Cookies.Add("affiliatename", reg.AffiliateName);
                        }
                        else
                        {
                            ctx.Request.Cookies["affiliatename"] = reg.AffiliateName;
                        }
                    }

                    if (ctx.Request.Cookies.ContainsKey("source") == false)
                    {
                        ctx.Request.Cookies.Add("source", (string)ctx.Request.Query.source.Value);
                    }
                    else
                    {
                        ctx.Request.Cookies["source"] = code;
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

                if (ctx.Request.Cookies.ContainsKey("affiliatename"))
                {
                    ctx.Response.Cookies.Add(
                        new NancyCookie("affiliatename", ctx.Request.Cookies["affiliatename"], DateTime.Now.AddDays(7)));
                }

            });
        }

        private dynamic AffiliateDashboard(AffiliateRegistration registration, dynamic arg)
        {
            var content = ContentModule.GetPage(SiteDatabase, "/__affiliate", true);


            var standardModel = new StandardModel(200);
            standardModel.Content = content;


            if (registration != null)
            {
                var key = "dashboard-" + registration.NcbUserId;
                dynamic dashboardData = MemoryCache.Default[key];

                if (Request.Query.code != null)
                {
                    dashboardData = null; // this is impersonation by user 1 - refresh all data

                    MemoryCache.Default.Remove(key);
                    MemoryCache.Default.Remove("AffiliateReg-" + registration.AffiliateCode);
                }

                if (dashboardData == null)
                {
                    var user = this.SiteDatabase.GetById<NcbUser>(registration.NcbUserId);
                    var saleOrders = this.SiteDatabase.Query<SaleOrder>()
                            .Where(so => so.NcbUserId == registration.NcbUserId)
                            .ToList();

                    var activeSaleOrder = (from so in saleOrders
                                          where so.PaymentStatus == PaymentStatus.Credit ||
                                                so.PaymentStatus == PaymentStatus.Deposit ||
                                                so.PaymentStatus == PaymentStatus.PaymentReceived
                                          select so).FirstOrDefault();

                    if (activeSaleOrder != null)
                    {
                        // figure out delivery date
                        activeSaleOrder.FindShipoutAndDeliveryDate(this.SiteDatabase);
                    }

                    Func<SaleOrder, SaleOrder> reduce = (so) =>
                    {
                        var thinCustomer = new
                        {
                            FirstName = (string)so.Customer.FirstName,
                            LastName = (string)so.Customer.LastName
                        };

                        return new SaleOrder()
                        {
                            __createdAt = so.__createdAt,
                            SaleOrderIdentifier = so.SaleOrderIdentifier,
                            Status = so.Status,
                            PaymentStatus = so.PaymentStatus,
                            Customer = thinCustomer
                        };
                    };

                    var stat = AffiliateReward.GetRewardStats(this.SiteDatabase, registration);
                    Func<AffiliateReward, JObject> addCanClaim = (rew) =>
                    {
                        var canClaim = AffiliateReward.CanClaim(this.SiteDatabase, rew, registration, stat);
                        var toReturn = JObject.FromObject(rew);

                        toReturn.Add("canClaim", canClaim);

                        return toReturn;
                    };

                    dashboardData = new
                    {
                        Registration = registration,
                        Code = registration.AffiliateCode,
                        RelatedOrders = this.SiteDatabase.Query<SaleOrder>()
                                            .Where(so => so.AffiliateCode == registration.AffiliateCode)
                                            .AsEnumerable()
                                            .Select(s => reduce(s)).ToList(),

                        AffiliateTransaction = this.SiteDatabase.Query("SELECT * FROM AffiliateTransaction WHERE AffiliateCode=?",
                        new AffiliateTransaction(),
                        new object[] { registration.AffiliateCode }).ToList(),

                        Profile = user.Profile,

                        SaleOrders = this.SiteDatabase.Query<SaleOrder>()
                            .Where(so => so.NcbUserId == registration.NcbUserId)
                                            .AsEnumerable()
                                            .Select(s => reduce(s)).ToList(),

                        ActiveSaleOrder = activeSaleOrder,

                        /* Stats */

                        SubscribeAll = SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM AffiliateRegistration WHERE RefererAffiliateCode=?",
                            new { Count = 0 },
                            new object[] { registration.AffiliateCode }).First().Count,

                        TotalShareClicks = SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM AffiliateShareClick WHERE AffiliateRegistrationId=?",
                            new { Count = 0 },
                            new object[] { registration.Id }).First().Count,

                        Downline = AffiliateModule.DiscoverDownLine(this.SiteDatabase, registration.AffiliateCode),

                        Rewards = from rew in this.SiteDatabase.Query<AffiliateReward>().AsEnumerable()
                                  select addCanClaim( rew ),

                        RewardsStat = stat,

                        ClaimedRewards = this.SiteDatabase.Query<AffiliateRewardsClaim>()
                                                          .Where( c => c.NcbUserId == registration.NcbUserId)
                                                          .AsEnumerable()
                    };

#if !DEBUG
                    MemoryCache.Default.Add(key, dashboardData, DateTimeOffset.Now.AddMinutes(10));
#endif
                    UpdatePageView(registration);
                }

                standardModel.Data = dashboardData;
            }


            return View["affiliate-dashboard", standardModel];

        }

        public static AffiliateRegistration ApplyAffiliate( NancyBlackDatabase db, int userId, string refererCode = null)
        {
            AffiliateRegistration reg = null;

            // whether user already registered
            var existing = db.Query<AffiliateRegistration>()
                                .Where(r => r.NcbUserId == userId)
                                .FirstOrDefault();

            // dont replace existing code
            if (existing == null)
            {
                reg = new AffiliateRegistration();
                reg.NcbUserId = userId;
                reg.Commission = 0.01M;  // start at 1 percent

                // automatic code
                var bytes = Encoding.ASCII.GetBytes(userId.ToString());
                reg.AffiliateCode = Crc32.ComputeChecksumString(bytes);

                var user = db.GetById<NcbUser>(userId);
                if (user.Profile != null && user.Profile.first_name != null)
                {
                    reg.AffiliateName = user.Profile.first_name;
                }

                if (reg.AffiliateName == null)
                {
                    reg.AffiliateName = "SQUAD51#" + userId;
                }

                reg.RefererAffiliateCode = refererCode;

                db.UpsertRecord(reg);

                return reg;
            }
            else
            {
                return existing;
            }
        }

        /// <summary>
        /// Upate Referer code of given user
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userId"></param>
        /// <param name="refererCode"></param>
        /// <returns></returns>
        public static AffiliateRegistration UpdateReferer(NancyBlackDatabase db, int userId, string refererCode )
        {
            var registration = AffiliateModule.ApplyAffiliate(db, userId, refererCode);

            if (registration.RefererAffiliateCode == null)
            {
                registration.RefererAffiliateCode = refererCode;
                db.UpsertRecord(registration);
            }

            return registration;
        }

        /// <summary>
        /// Query Azure Table Storage and update page view
        /// </summary>
        private void UpdatePageView(AffiliateRegistration reg)
        {
            // Fire and Forget - if multiple threads have spaned
            var database = SiteDatabase;
            var key = reg.AffiliateCode + "-updatepageview";

            reg = database.GetById<AffiliateRegistration>(reg.Id);
            if (reg.LastPageViewUpdate.Date == DateTime.Now.Date)
            {
                return;
            }

            Task.Run(() =>
            {
                lock (BaseModule.GetLockObject(key))
                {
                    var cached = MemoryCache.Default[key] as AffiliateRegistration;
                    if (cached != null)
                    {
                        // someone has recently do the update
                        if (cached.LastPageViewUpdate.Date == DateTime.Now.Date)
                        {
                            return;
                        }
                    }

                    if (reg.LastPageViewUpdate == default(DateTime))
                    {
                        reg.LastPageViewUpdate = reg.__createdAt;
                    }

                    var table = GetPageViewTable();


                    var pageViews = (from pv in table.CreateQuery<PageView>()
                                     where
                                        pv.Timestamp > reg.LastPageViewUpdate &&
                                        pv.AffiliateCode == reg.AffiliateCode
                                     select pv);

                    var userSet = new HashSet<string>();

                    foreach (var item in pageViews)
                    {
                        reg.TotalPageView++;
                        userSet.Add(item.UserUniqueId);

                        if (item.QueryString.Contains("source="))
                        {
                            reg.TotalAffiliateLinkClicks++;
                        }

                        if (item.QueryString.Contains("subscribe=1"))
                        {
                            reg.TotalSubscribeLinkClicks++;
                        }
                    }

                    reg.LastPageViewUpdate = DateTime.Now;
                    reg.TotalUniqueUser += userSet.Count;

                    SiteDatabase.UpsertRecord(reg);

                    // Make the instance for checking available for 1 hour
                    MemoryCache.Default.Add(key, reg, DateTimeOffset.Now.AddHours(1));
                }
            });
        }

        /// <summary>
        /// Discovers down line of current user
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="maxLevel"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> DiscoverDownLine( NancyBlackDatabase db, string topCode, int maxLevel = 2)
        {
            Queue<string> referer = new Queue<string>();
            referer.Enqueue(topCode);

            int currentLevel = 1;
            while (referer.Count > 0)
            {
                var current = referer.Dequeue();

                var downline = db.Query<AffiliateRegistration>()
                                    .Where(a => a.RefererAffiliateCode == current)
                                    .OrderByDescending( a => a.Id );

                foreach (var item in downline)
                {
                    yield return new
                    {
                        level = currentLevel,
                        name = item.AffiliateName,
                        facebookId = db.GetById<NcbUser>( item.NcbUserId ).FacebookAppScopedId,
                        parent = current,
                    };

                    referer.Enqueue(item.AffiliateCode);
                }

                currentLevel++;
                if (currentLevel > maxLevel)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Gets the page view table
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        private CloudTable GetPageViewTable(bool cache = true)
        {
            Func<CloudTable> getTable = () =>
            {
                var cred = new StorageCredentials((string)CurrentSite.analytics.raw.credentials);
                var client = new CloudTableClient(new Uri((string)CurrentSite.analytics.raw.server), cred);
                return client.GetTableReference((string)CurrentSite.analytics.raw.table);
            };


            if (cache == false)
            {
                return getTable();
            }

            var key = string.Format("azure{0}-{1}-{2}",
                               (string)CurrentSite.analytics.raw.credentials,
                               (string)CurrentSite.analytics.raw.server,
                               (string)CurrentSite.analytics.raw.table).GetHashCode().ToString();

            var table = MemoryCache.Default[key] as CloudTable;
            if (table == null)
            {
                table = getTable();

                MemoryCache.Default.Add(key, table, DateTimeOffset.Now.AddDays(1));
            }

            return table;
        }
    }
}