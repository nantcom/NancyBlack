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
using NantCom.NancyBlack.Modules.DatabaseSystem;
using System.IO;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Manatee.Trello;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Auth;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using System.Threading.Tasks;

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
                title += "กระเป๋า ( " + claim.AffiliateCode + " )";
            }

            if (claim.RewardsName == "buy1")
            {
                title += "เมาส์ ( " + claim.AffiliateCode + " )";
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

                card = list.Cards.Add(title).Result;
            }

            card.DueDate = claim.__createdAt.AddDays(7);

            var profile = this.SiteDatabase.GetById<NcbUser>(claim.NcbUserId).Profile;

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

                card.Labels.Add(label);
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

                card = list.Cards.Add(title).Result;
            }

            if (transaction.IsPendingApprove)
            {
                card.DueDate = transaction.__updatedAt.AddDays(7);
            }

            var user = this.SiteDatabase.GetById<NcbUser>(transaction.NcbUserId);
            if (user == null)
            {
                card.Description = "Error: User Not Found. Id: " + transaction.NcbUserId;
                return;
            }

            var profile = this.SiteDatabase.GetById<NcbUser>(transaction.NcbUserId).Profile;
            var reg = this.SiteDatabase.GetById<AffiliateRegistration>(transaction.AffiliateRegistrationId);
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

                card.Labels.Add(label);
            }

        }


        public AffiliateModule()
        {
            AffiliateModule.TemplatePath = Path.Combine(this.RootPath, "Site", "Views", "EmailTemplates");
            
            Get["/__affiliate/___fixname"] = this.HandleRequest((arg) =>
            {
                var affiliates = this.SiteDatabase.Query<AffiliateRegistration>().ToList();
                var users = this.SiteDatabase.Query<NcbUser>().ToDictionary(u => u.Id);

                foreach (var affiliate in affiliates)
                {
                    if (users.ContainsKey(affiliate.NcbUserId))
                    {
                        if (string.IsNullOrEmpty(affiliate.AffiliateName))
                        {
                            affiliate.AffiliateName = (string)users[affiliate.NcbUserId].Profile.first_name;
                            this.SiteDatabase.Connection.Update(affiliate);
                        }
                    }

                }


                return "OK";
            });

            Get["/__affiliate/___syncsubscription"] = this.HandleRequest((arg) =>
            {
                var affiliates = this.SiteDatabase.Query<AffiliateRegistration>().ToList();
                var users = this.SiteDatabase.Query<NcbUser>().ToDictionary(u => u.Id);
                var mailinglist = this.SiteDatabase.Query<NcbMailingListSubscription>().ToLookup(sub => sub.Email);

                foreach (var affiliate in affiliates)
                {
                    if (users.ContainsKey(affiliate.NcbUserId) == false)
                        continue;

                    var user = users[affiliate.NcbUserId];
                    if (user.Email.StartsWith("fb_") == true)
                    {
                        continue; // BUG
                    }

                    // see if user has subscribed to mailing list
                    if (mailinglist[user.Email].Count() == 0)
                    {
                        NcbMailingListSubscription sub = new NcbMailingListSubscription();
                        sub.FirstName = user.Profile.first_name;
                        sub.LastName = user.Profile.last_name;
                        sub.Email = user.Profile.email;
                        sub.BirthDay = user.Profile.birthday;
                        sub.RefererAffiliateCode = affiliate.RefererAffiliateCode;

                        this.SiteDatabase.UpsertRecord(sub);
                    }
                }

                return "OK";
            });

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
                    reg.AffiliateCode = Crc32.ComputeChecksumString(bytes);
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

                    var user = this.SiteDatabase.GetById<NcbUser>(this.CurrentUser.Id);
                    reg.AffiliateName = user.Profile.first_name;

                    // enroll user into Mailing List Automatically
                    NcbMailingListSubscription sub = new NcbMailingListSubscription();
                    sub.FirstName = user.Profile.first_name;
                    sub.LastName = user.Profile.last_name;
                    sub.Email = user.Profile.email;
                    sub.BirthDay = user.Profile.birthday;

                    if (string.IsNullOrEmpty(sub.Email))
                    {
                        var customEmail = (arg.body.Value as JObject).Property("email").Value.ToString();
                        sub.Email = customEmail;

                        user.Profile.email = customEmail;
                        user.Email = customEmail;

                        this.SiteDatabase.UpsertRecord(user);
                    }

                    sub.RefererAffiliateCode = reg.RefererAffiliateCode;

                    this.SiteDatabase.UpsertRecord(reg);
                    this.SiteDatabase.UpsertRecord(sub);

                    return reg;
                }
                else
                {
                    var input = arg.body.Value;
                    if (input == null)
                    {
                        return existing;
                    }

                    string customEmail = input.email;
                    if (customEmail != null)
                    {
                        // update user's email
                        var user = this.SiteDatabase.GetById<NcbUser>(this.CurrentUser.Id);
                        user.Email = (string)customEmail;
                        user.Profile.email = (string)customEmail;

                        this.SiteDatabase.UpsertRecord(user);

                        var existingSub = this.SiteDatabase.Query<NcbMailingListSubscription>().Where(sub => sub.Email == (string)customEmail).FirstOrDefault();
                        if (existingSub == null)
                        {
                            NcbMailingListSubscription sub = new NcbMailingListSubscription();
                            sub.FirstName = user.Profile.first_name;
                            sub.LastName = user.Profile.last_name;
                            sub.Email = user.Profile.email;
                            sub.BirthDay = user.Profile.birthday;
                            sub.RefererAffiliateCode = existing.RefererAffiliateCode;

                            this.SiteDatabase.UpsertRecord(sub);
                        }
                    }


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
                    this.SiteDatabase.UpsertRecord(registration);
                }

                var pendingPayment = this.SiteDatabase.Query<AffiliateTransaction>()
                                        .Where(t => t.AffiliateCode == registration.AffiliateCode && t.IsCommissionPaid == false && t.IsPendingApprove == false)
                                        .ToList();
                

                // using alternate rate
                this.SiteDatabase.Transaction(() =>
                {
                    foreach (var item in pendingPayment)
                    {
                        item.IsPendingApprove = true;
                        item.BTCAddress = registration.BTCAddress;

                        this.SiteDatabase.UpsertRecord(item);
                        this.AddPayoutCard( this.CurrentSite, item);
                    }

                });

                

                return 200;
            });

            Post["/__affiliate/getrewards"] = this.HandleRequest((arg) =>
            {
                dynamic param = (arg.body.Value as JObject);
                if (param.rewardsName == null)
                {
                    return 400;
                }

                var registration = this.SiteDatabase.Query<AffiliateRegistration>()
                    .Where(t => t.NcbUserId == this.CurrentUser.Id).FirstOrDefault();

                if (registration == null)
                {
                    return 400;
                }
                
                if (param.rewardsName == "subscribe1")
                {
                    var sub = this.SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM NcbMailingListSubscription WHERE RefererAffiliateCode=?",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode }).First().Count;

                    if (sub < 5)
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
                        p.Title = "Affiliate Discount: " + code;
                        p.Price = -2000;
                        p.Attributes = new
                        {
                            description = "โค๊ดส่วนลดพิเศษสำหรับคุณ " + this.CurrentUser.Profile.first_name + " จำนวน 2,000 บาท เมื่อสั่งซื้อขั้นต่ำ 32,000 บาท",
                            limit = "32000",
                            onetime = true
                        };

                        claim = new AffiliateRewardsClaim();
                        claim.AffiliateRegistrationId = registration.Id;
                        claim.NcbUserId = registration.NcbUserId;
                        claim.AffiliateCode = registration.AffiliateCode;
                        claim.DiscountCode = code;
                        claim.RewardsName = "subscribe1";

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

                    var profile = this.SiteDatabase.GetById<NcbUser>(registration.NcbUserId).Profile;
                    if (profile.phone == null || profile.email == null)
                    {
                        return new
                        {
                            type = "warning",
                            title = "ขอโทษนะ",
                            text = "รบกวนกรอกข้อมูลหมายเลขโทรศัพท์ และอีเมลล์ก่อนจ้า",
                        };
                    }

                    var claim = this.SiteDatabase.QueryAsDynamic("SELECT DiscountCode FROM AffiliateRewardsClaim WHERE AffiliateCode=? AND RewardsName=?",
                                new { DiscountCode = "" },
                                new object[] { registration.AffiliateCode, "subscribe2" }).FirstOrDefault();

                    if (claim == null)
                    {

                        claim = new AffiliateRewardsClaim();
                        claim.AffiliateRegistrationId = registration.Id;
                        claim.NcbUserId = registration.NcbUserId;
                        claim.AffiliateCode = registration.AffiliateCode;
                        claim.RewardsName = "subscribe2";

                        this.SiteDatabase.UpsertRecord(claim);

                        this.AddRewardsCard(this.CurrentSite, claim, true);
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

                    var profile = this.SiteDatabase.GetById<NcbUser>(registration.NcbUserId).Profile;
                    if (profile.phone == null || profile.email == null)
                    {
                        return new
                        {
                            type = "warning",
                            title = "ขอโทษนะ",
                            text = "รบกวนกรอกข้อมูลหมายเลขโทรศัพท์ และอีเมลล์ก่อนจ้า",
                        };
                    }

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
                        claim.AffiliateRegistrationId = registration.Id;
                        claim.NcbUserId = registration.NcbUserId;
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

            Post["/__affiliate/addtosaleorder"] = this.HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                var requestBody = (arg.body.Value as JObject);
                var saleOrderId = requestBody.Value<int>("saleOrderId");
                var affiliateRewardsClaimId = requestBody.Value<int>("arcId");

                if (saleOrderId != 0 && affiliateRewardsClaimId != 0)
                {
                    var aRC = this.SiteDatabase.GetById<AffiliateRewardsClaim>(affiliateRewardsClaimId);

                    if (aRC.IncludedInSaleOrderId != 0 || aRC.DiscountCode != null)
                    {
                        return 403;
                    }

                    aRC.IncludedInSaleOrderId = saleOrderId;
                    this.SiteDatabase.UpsertRecord(aRC);

                    return new
                    {
                        AffiliateRewardsClaim = aRC
                    };
                }


                return 403;

            });

            Post["/__affiliate/updateprofile"] = this.HandleRequest((arg) =>
            {
                var requestBody = arg.body.Value;

                UserManager.Current.UpdateProfile(this.Context, requestBody.Profile);
                
                AffiliateRegistration registration = this.SiteDatabase.Query<AffiliateRegistration>()
                                                        .Where(t => t.NcbUserId == this.CurrentUser.Id).FirstOrDefault();

                registration.AffiliateName = requestBody.Registration.AffiliateName;
                registration.AffiliateMessage = requestBody.Registration.AffiliateMessage;
                this.SiteDatabase.UpsertRecord(registration);

                MemoryCache.Default.Remove("dashboard-" + this.CurrentUser.Id);
                MemoryCache.Default.Remove("AffiliateReg-" + registration.AffiliateCode);

                // update all trello cards on the board
                var claims = this.SiteDatabase.Query<AffiliateRewardsClaim>().Where(c => c.NcbUserId == registration.NcbUserId);
                foreach (var claim in claims)
                {
                    this.AddRewardsCard(this.CurrentSite, claim, false); // Update card if found
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
                    var key = "dashboard-" + registration.NcbUserId;
                    object dashboardData = MemoryCache.Default[key];

                    if (this.Request.Query.code != null)
                    {
                        dashboardData = null; // force refresh

                        MemoryCache.Default.Remove(key);
                        MemoryCache.Default.Remove("AffiliateReg-" + registration.AffiliateCode);
                    }

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
                            
                            CommissionTotal = this.SiteDatabase.QueryAsDynamic("SELECT SUM(CommissionAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=?",
                            new { Amount = 0M },
                            new object[] { registration.AffiliateCode }).First().Amount,

                            CommissionPaid = this.SiteDatabase.QueryAsDynamic("SELECT SUM(CommissionAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=? AND IsCommissionPaid=1",
                            new { Amount = 0M },
                            new object[] { registration.AffiliateCode }).First().Amount,

                            CommissionPendingApproval = this.SiteDatabase.QueryAsDynamic("SELECT SUM(CommissionAmount) As Amount FROM AffiliateTransaction WHERE AffiliateCode=? AND IsPendingApprove=1",
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
                                new object[] { registration.AffiliateCode, "?subscribe=1&source=" + registration.AffiliateCode }).First().Count,

                            SubscribeAll = this.SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count FROM NcbMailingListSubscription WHERE RefererAffiliateCode=?",
                                new { Count = 0 },
                                new object[] { registration.AffiliateCode }).First().Count,

                            Profile = this.SiteDatabase.GetById<NcbUser>(registration.NcbUserId).Profile,
                        };

                        MemoryCache.Default.Add(key, dashboardData, DateTimeOffset.Now.AddMinutes(10));

                        this.UpdatePageView(registration);
                    }

                    standardModel.Data = dashboardData;
                }


                return View["affiliate-dashboard", standardModel]; ;
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

            Get["/__affiliate/syncrewards"] = this.HandleRequest((arg) =>
            {
                // sync rewards request to trello
                var claims = this.SiteDatabase.Query<AffiliateRewardsClaim>().ToList();

                // temp - fill in data
                foreach (var claim in claims)
                {
                    if (claim.NcbUserId != 0)
                    {
                        continue;
                    }
                    var reg = this.SiteDatabase.Query<AffiliateRegistration>().Where(r => r.AffiliateCode == claim.AffiliateCode).FirstOrDefault();
                    if (reg == null)
                    {
                        continue;
                    }


                    claim.NcbUserId = reg.NcbUserId;
                    claim.AffiliateRegistrationId = reg.Id;

                    this.SiteDatabase.Connection.Update(claim);
                }

                foreach (var claim in claims)
                {
                    if (claim.IsSent == true)
                    {
                        continue;
                    }

                    this.AddRewardsCard(this.CurrentSite, claim);
                }

                return "OK";
            });

            Get["/__affiliate/synctransaction"] = this.HandleRequest((arg) =>
            {
                // sync rewards request to trello
                var trans = this.SiteDatabase.Query<AffiliateTransaction>().ToList();

                // temp - fill in data
                foreach (var t in trans)
                {
                    if (t.NcbUserId != 0)
                    {
                        continue;
                    }

                    var reg = this.SiteDatabase.Query<AffiliateRegistration>().Where(r => r.AffiliateCode == t.AffiliateCode).FirstOrDefault();
                    if (reg == null)
                    {
                        continue;
                    }


                    t.NcbUserId = reg.NcbUserId;
                    t.AffiliateRegistrationId = reg.Id;

                    this.SiteDatabase.Connection.Update(t);
                }

                foreach (var t in trans)
                {
                    this.AddPayoutCard(this.CurrentSite, t);
                }

                return "OK";
            });
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

                if (ctx.Request.Cookies.ContainsKey("userid") == false)
                {
                    ctx.Request.Cookies.Add("userid", Guid.NewGuid().ToString());
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

                if (ctx.Request.Cookies.ContainsKey("userid"))
                {
                    ctx.Response.Cookies.Add(
                        new NancyCookie("userid", ctx.Request.Cookies["userid"], DateTime.Now.AddDays(1)));
                }
            });
        }

        /// <summary>
        /// Query Azure Table Storage and update page view
        /// </summary>
        private void UpdatePageView(AffiliateRegistration reg)
        {
            // Fire and Forget - if multiple threads have spaned
            var database = this.SiteDatabase;
            var key = reg.AffiliateCode + "-updatepageview";

            reg = database.GetById<AffiliateRegistration>(reg.Id);
            if (reg.LastPageViewUpdate.Date == DateTime.Now.Date)
            {
                return;
            }

            Task.Run(() =>
            {
                lock (BaseModule.GetLockObject( key ))
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

                    var table = this.GetPageViewTable();


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
                    reg.TotalUniqueUser = userSet.Count;
                    this.SiteDatabase.UpsertRecord(reg);

                    // Make the instance for checking available for 1 hour
                    MemoryCache.Default.Add(key, reg, DateTimeOffset.Now.AddHours(1));
                }
            });
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
                var cred = new StorageCredentials((string)this.CurrentSite.analytics.raw.credentials);
                var client = new CloudTableClient(new Uri((string)this.CurrentSite.analytics.raw.server), cred);
                return client.GetTableReference((string)this.CurrentSite.analytics.raw.table);
            };


            if (cache == false)
            {
                return getTable();
            }

            var key = string.Format("azure{0}-{1}-{2}",
                               (string)this.CurrentSite.analytics.raw.credentials,
                               (string)this.CurrentSite.analytics.raw.server,
                               (string)this.CurrentSite.analytics.raw.table).GetHashCode().ToString();

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