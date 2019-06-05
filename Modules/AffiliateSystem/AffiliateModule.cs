using Manatee.Trello;
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

            // create a transaction
            AffiliateTransaction commission = new AffiliateTransaction();
            commission.AffiliateCode = so.AffiliateCode;
            commission.CommissionAmount = so.TotalAmount * (registration.Commission / 100);
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

                var body = arg.body.Value;

                var currentUser = AffiliateModule.ApplyAffiliate(this.SiteDatabase, this.CurrentUser.Id, (string)body.source);
                AffiliateModule.UpdateReferer(this.SiteDatabase, this.CurrentUser.Id, (string)body.source);

                var regresult = JObject.FromObject(currentUser);

                if (body.sharedCoupon != null)
                {
                    var discountCode = (string)body.sharedCoupon.CouponCode;

                    lock (BaseModule.GetLockObject("SharedCouponCheck-" + currentUser.Id))
                    {
                        // original coupon
                            var claim = this.SiteDatabase.Query<AffiliateRewardsClaim>()
                                               .Where(c => c.DiscountCode == discountCode)
                                               .OrderBy( c => c.Id)
                                               .FirstOrDefault();

                            // owner of the coupon
                            var couponOwner = this.SiteDatabase.Query<AffiliateRegistration>()
                                               .Where(r => r.NcbUserId == claim.NcbUserId)
                                               .FirstOrDefault();

                            System.Action performShare = () =>
                            {

                                // prevent dubplicates
                                var existing = this.SiteDatabase.Query<AffiliateRewardsClaim>()
                                                        .Where(c => c.DiscountCode == discountCode && c.AffiliateRegistrationId == currentUser.Id)
                                                        .FirstOrDefault();

                                if (existing != null)
                                {
                                    // already has this coupon
                                    regresult.Add("CouponSaved", true);

                                    return;
                                }

                                var toCopy = this.SiteDatabase.GetById<AffiliateRewardsClaim>((int)body.sharedCoupon.CouponId);

                                toCopy.Id = 0;
                                toCopy.AffiliateRegistrationId = currentUser.Id;
                                toCopy.AffiliateCode = currentUser.AffiliateCode;
                                toCopy.NcbUserId = this.CurrentUser.Id;
                                toCopy.RewardsName = "shared from ID:" + body.sharedCoupon.AffiliateId;
                                toCopy.IsShareEnabled = false;

                                this.SiteDatabase.UpsertRecord(toCopy);

                            };

                            var ownerAncestors = AffiliateModule.DiscoverUpline( couponOwner.AffiliateCode, this.SiteDatabase );

                            // same user, cannot share
                            if (couponOwner.Id == currentUser.Id)
                            {
                                regresult.Add("CouponSaved", false);
                                regresult.Add("Message", "SAME_USER");
                                regresult.Add("Owner", couponOwner.AffiliateName);

                            }
                            else if (couponOwner.RefererAffiliateCode == currentUser.AffiliateCode)
                            {
                                // owner is direct downline of current user
                                // meaning current user trying to get coupon from direct downline
                                // this is not allowed

                                regresult.Add("CouponSaved", false);
                                regresult.Add("Message", "SAME_TREE");
                                regresult.Add("Owner", couponOwner.AffiliateName);
                            }
                            else if (currentUser.RefererAffiliateCode == couponOwner.AffiliateCode)
                            {
                                // owner is upline of current user
                                // can share coupon
                                performShare();
                            }
                            else if (ownerAncestors.Contains( currentUser.AffiliateCode ) )
                            {
                                // current user is an ancestor of coupon owner, 
                                // allow sharing but we keep track
                                MailSenderModule.SendEmail("company@nant.co",
                                    "Tricky Sharing Coupon Behavior Detected",
                                    "Affiliate:" + couponOwner.AffiliateCode + " trying to share coupon to: " + currentUser.AffiliateCode +
                                    "But " + currentUser.AffiliateCode + " is also ancestor of the person who own coupons." +
                                    "Distance between affiliate is: " + ownerAncestors.IndexOf( currentUser.AffiliateCode ) + " Levels",
                                    false,
                                    this.Context);


                                performShare();
                            }
                            else
                            {
                                // there is no relationship between  owner and current user, can share
                                performShare();
                            }

                    }

                }

                if (body.sharedReward != null)
                {
                    lock (BaseModule.GetLockObject("SharedRewardCheck-" + currentUser.Id))
                    {
                        var result = AffiliateReward.ClaimReward(this.SiteDatabase, (int)body.sharedReward.Id, currentUser.Id);
                        if (result != null)
                        {
                            regresult.Add("RewardClaimed", true);
                        }
                    }
                }

                return regresult;
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

                var pendingPayment = SiteDatabase.Query<AffiliateTransaction>()
                                        .Where(t => t.AffiliateCode == registration.AffiliateCode && t.IsCommissionPaid == false && t.IsPendingApprove == false)
                                        .ToList();


                // using alternate rate
                SiteDatabase.Transaction(() =>
                {
                    foreach (var item in pendingPayment)
                    {
                        item.IsPendingApprove = true;
                        SiteDatabase.UpsertRecord(item);
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

            Get["/__affiliate/getsharedcoupon"] = this.HandleRequest((arg) =>
            {
                if (this.Request.Cookies.ContainsKey("coupon"))
                {
                    var id = this.Request.Cookies["coupon"];
                    var coupon = this.SiteDatabase.GetById<AffiliateRewardsClaim>(int.Parse(id));

                    if (coupon != null)
                    {
                        if (coupon.IsShareEnabled == false)
                        {
                            return 404;
                        }
                        
                        var couponProduct = this.SiteDatabase.GetById<Product>(coupon.ProductId);
                        if (couponProduct.Url.Contains("/archive/"))
                        {
                            return new {
                                IsValid = false,
                                Message = "USED"
                            };
                        }

                        var reg = this.SiteDatabase.GetById<AffiliateRegistration>(coupon.AffiliateRegistrationId);

                        if (this.CurrentUser.IsAnonymous == false)
                        {
                            // prevent referee to share coupon back to referer
                            var currentUser = AffiliateModule.ApplyAffiliate(this.SiteDatabase, this.CurrentUser.Id);
                            if (reg.RefererAffiliateCode == currentUser.AffiliateCode)
                            {
                                return new
                                {
                                    IsValid = false,
                                    Message = "SAME_TREE"
                                };
                            }
                        }

                        return new
                        {
                            AffiliateId = reg.Id,
                            AffiliateName = reg.AffiliateName,
                            AffiliateRewardsId = coupon.AffiliateRewardsId,
                            CouponId = coupon.Id,
                            CouponCode = coupon.DiscountCode,
                            CouponAttributes = coupon.CouponAttributes
                        };
                    }
                }

                return 404;
            });
            
            Get["/__affiliate/getreward"] = this.HandleRequest((arg) =>
            {
                if (this.Request.Cookies.ContainsKey("reward"))
                {
                    var id = this.Request.Cookies["reward"];
                    var reward = this.SiteDatabase.GetById<AffiliateReward>(int.Parse(id));

                    if (reward != null)
                    {
                        if (reward.IsDirectClaim == false)
                        {
                            return 404;
                        }

                        if (reward.IsRewardsClaimable == false)
                        {
                            return new
                            {
                                IsValid = false
                            };
                        }

                        return reward;
                    }
                }

                return 404;
            });

            Get["/__affiliate/myrewards"] = this.HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 401;
                }

                return this.SiteDatabase.Query<AffiliateRewardsClaim>()
                           .Where(c => c.NcbUserId == this.CurrentUser.Id)
                           .AsEnumerable();

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
            dynamic affiliateFacts = MemoryCache.Default["affiliatefacts"];
            if (affiliateFacts == null)
            {
                affiliateFacts = new
                {
                    Total = this.SiteDatabase.Query<AffiliateRegistration>().Count(),
                    TotalActive = this.SiteDatabase.Query("SELECT Distinct AffiliateCode FROM AffiliateTransaction", new { Count = 0 }).Count(),
                    PayoutStats = this.SiteDatabase.Query("SELECT Count(Id) as Count, Avg(CommissionAmount) as Avg, Sum(CommissionAmount) as Sum FROM AffiliateTransaction", new { Count = 0, Avg = 0.0M, Sum = 0.0M }).FirstOrDefault(),
                };
                MemoryCache.Default.Add("affiliatefacts", affiliateFacts, DateTimeOffset.Now.AddHours(1));
            }

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

                    Func<AffiliateReward, bool> postProcess = (rew) =>
                    {
                        if (rew.IsRewardsClaimable)
                        {
                            return true;
                        }

                        if (rew.ActiveUntil.HasValue)
                        {
                            if (DateTime.Now.Subtract(rew.ActiveUntil.Value).TotalDays > 7)
                            {
                                return false; // skip rewards older than 1 week
                            }

                            return true; // show that they have missed this rewards
                        }

                        if (rew.TotalQuota > 0) // with quota, see created date
                        {
                            if (DateTime.Now.Subtract(rew.__createdAt).TotalDays > 7)
                            {
                                return false; // dont show old rewards
                            }

                            return true;
                        }

                        return true;
                    };

                    AffiliateRegistration refererReg;
                    refererReg = this.SiteDatabase.Query<AffiliateRegistration>()
                                         .Where(reg => reg.AffiliateCode == registration.RefererAffiliateCode)
                                         .FirstOrDefault();

                    if (refererReg != null)
                    {
                        var refererUser = this.SiteDatabase.GetById<NcbUser>(refererReg.NcbUserId);
                        refererReg.AdditionalData = new
                        {
                            Id = refererUser.Profile.id
                        };
                    }

                    dashboardData = new
                    {
                        Referer = refererReg,

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

                        ShareClicks = SiteDatabase.QueryAsDynamic("SELECT COUNT(Id) As Count, Url FROM AffiliateShareClick WHERE AffiliateRegistrationId=? GROUP By Url",
                            new { Count = 0, Url = "" },
                            new object[] { registration.Id }),

                        Downline = AffiliateModule.DiscoverDownLine(this.SiteDatabase, registration.AffiliateCode),

                        Rewards = this.SiteDatabase.Query<AffiliateReward>().AsEnumerable()
                                      .Where( rew => rew.IsActive == true )
                                      .AsEnumerable()
                                      .Where( rew => postProcess(rew))
                                      .Select(rew => addCanClaim(rew)),

                        RewardsStat = stat,

                        ClaimedRewards = this.SiteDatabase.Query<AffiliateRewardsClaim>()
                                                          .Where( c => c.NcbUserId == registration.NcbUserId)
                                                          .AsEnumerable(),

                        AffiliateFacts = affiliateFacts
                    };

#if !DEBUG
                    MemoryCache.Default.Add(key, dashboardData, DateTimeOffset.Now.AddMinutes(10));
#endif
                    UpdatePageView(registration);
                }

                standardModel.Data = JObject.FromObject( dashboardData );
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

            // can only change referer if does not already have one
            // NOTE: already tried allowing referer to change - this cause
            // problem with cycle and also possible fraud attempt
            // also - if we allow referer to change the number of
            // downline will be limited and also referer can 'steal'
            // downline from other referer.

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
                    reg.TotalSales = SiteDatabase.Query<SaleOrder>()
                                        .Where(so => so.AffiliateCode == reg.AffiliateCode &&
                                               so.PaymentStatus == PaymentStatus.PaymentReceived).Count();

                    reg.UpdateCommissionRate();

                    SiteDatabase.UpsertRecord(reg);

                    // Make the instance for checking available for 1 hour
                    MemoryCache.Default.Add(key, reg, DateTimeOffset.Now.AddHours(1));
                }
            });
        }

        /// <summary>
        /// Find Ancestor of given affiliate code
        /// </summary>
        /// <param name="code"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private static List<string> DiscoverUpline( string code, NancyBlackDatabase db)
        {
            var ancestorList = new List<string>();
            var currentCode = code;

            while (true)
            {
                var parent = db.Query<AffiliateRegistration>()
                        .Where(r => r.AffiliateCode == currentCode)
                        .FirstOrDefault();

                if (parent == null ||
                    string.IsNullOrEmpty(parent.AffiliateCode) ||
                    parent.AffiliateCode == ancestorList.LastOrDefault() || // BUG in previous version of website
                    ancestorList.Contains(parent.AffiliateCode)) // BUG in previous version of website
                {
                    break;
                }

                ancestorList.Add(parent.AffiliateCode);
                currentCode = parent.RefererAffiliateCode;
            }

            return ancestorList;
        }

        /// <summary>
        /// Discovers down line of current user
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="maxLevel"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> DiscoverDownLine( NancyBlackDatabase db, string topCode, int maxLevel = 2)
        {
            var cache = MemoryCache.Default["AffiliateCache"] as ILookup<string, dynamic>;
            if (cache == null)
            {
                cache = db.Query<AffiliateRegistration>()
                    .AsEnumerable().ToLookup(r => r.RefererAffiliateCode, r => (new { AffiliateName = r.AffiliateName, NcbUserId = r.NcbUserId, AffiliateCode = r.AffiliateCode }) as dynamic);

                MemoryCache.Default.Add("AffiliateCache", cache, DateTimeOffset.Now.AddMinutes(10));
            }

            Queue<string> referer = new Queue<string>();
            referer.Enqueue(topCode);

            int currentLevel = 1;
            while (referer.Count > 0)
            {
                var current = referer.Dequeue();
                var downline = cache[current];

                foreach (var item in downline)
                {
                    yield return new
                    {
                        level = currentLevel,
                        name = (string)item.AffiliateName,
                        facebookId = db.GetById<NcbUser>( (int)item.NcbUserId ).FacebookAppScopedId,
                        parent = current,
                    };

                    referer.Enqueue((string)item.AffiliateCode);
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