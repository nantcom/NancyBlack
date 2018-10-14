﻿using HashidsNet;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Web;

namespace NantCom.NancyBlack.Modules.AffiliateSystem.types
{
    public class AffiliateReward : IStaticType
    {
        #region IStaticType Properties
        public int Id { get; set; }
        public DateTime __createdAt { get; set; }
        public DateTime __updatedAt { get; set; }
        #endregion

        /// <summary>
        /// Name of Rewards
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Product Id that will be used as rewards
        /// </summary>
        public int RewardsProductId { get; set; }

        /// <summary>
        /// Required number of unique visitor to claim
        /// </summary>
        public int? UniqueVisitorCount { get; set; }

        /// <summary>
        /// Required number of page views to claim
        /// </summary>
        public int? PageViewsCount { get; set; }

        /// <summary>
        /// Required Down-line count to claim
        /// </summary>
        public int? DownlineCount { get; set; }

        /// <summary>
        /// Required Direct Down-line count to claim
        /// </summary>
        public int? DirectDownlineCount { get; set; }

        /// <summary>
        /// Number of sales to claim
        /// </summary>
        public int? SalesCount { get; set; }

        /// <summary>
        /// Whether it is a one time claim, if not user can claim multiple times.
        /// For Example if set DownlineCount = 10 - user can claim when the number of downline is 10, 20, 30 etc.
        /// </summary>
        public bool IsOneTime { get; set; }

        /// <summary>
        /// Whether this is code type reward - product will be created automatically
        /// </summary>
        public bool IsCodeDiscount { get; set; }

        /// <summary>
        /// Discount Amount
        /// </summary>
        public Decimal CodeDiscountAmount { get; set; }

        /// <summary>
        /// Minimum Purchase
        /// </summary>
        public Decimal MinimumPurchaseAmount { get; set; }

        /// <summary>
        /// Expiry Date (Fixed)
        /// </summary>
        public DateTime? CodeDiscountExpiryDate { get; set; }
        
        /// <summary>
        /// How long the code is valid for
        /// </summary>
        public int? CodeDicountExpiryInDays { get; set; }
        
        /// <summary>
        /// Whether this reward is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Terms and Condition to be displayed
        /// </summary>
        public string Conditions { get; set; }
        
        /// <summary>
        /// Get Statistics that will be use to render reward dashboard
        /// </summary>
        /// <param name="db"></param>
        /// <param name=""></param>
        public static dynamic GetRewardStats( NancyBlackDatabase db, AffiliateRegistration reg)
        {
            var allDownline = AffiliateModule.DiscoverDownLine(db, reg.AffiliateCode).ToList();

            return new
            {
                UniqueVisitorCount = reg.TotalUniqueUser,
                PageViewsCount = reg.TotalPageView,
                DownlineCount = allDownline.Count,
                DirectDownlineCount = allDownline.Where( d => d.level == 1 ).Count(),
                SalesCount = db.Query<SaleOrder>()
                                .Where( so => so.PaymentStatus == PaymentStatus.PaymentReceived )
                                .Count()
            };
        }

        /// <summary>
        /// Whether user can claim the reward
        /// </summary>
        /// <param name="db"></param>
        /// <param name="rewardsId"></param>
        /// <param name="registrationId"></param>
        /// <returns></returns>
        public static bool CanClaim(NancyBlackDatabase db, int rewardsId, int registrationId)
        {
            var reg = db.GetById<AffiliateRegistration>(registrationId);
            var rewards = db.GetById<AffiliateReward>(rewardsId);
            var stat = AffiliateReward.GetRewardStats(db, reg);

            return AffiliateReward.CanClaim(db, rewards, reg, stat);
        }

        /// <summary>
        /// Whether user can claim the reward
        /// </summary>
        /// <param name="db"></param>
        /// <param name="rewardsId"></param>
        /// <param name="registrationId"></param>
        /// <returns></returns>
        public static bool CanClaim(NancyBlackDatabase db, AffiliateReward rewards, AffiliateRegistration reg, dynamic statIn)
        {
            AffiliateReward stat;
            if (statIn is JObject)
            {
                stat = ((JObject)statIn)
                              .ToObject<AffiliateReward>();
            }
            else
            { 
                // stat looks like a rewards so we can use it
                stat = JObject.FromObject(statIn)
                              .ToObject<AffiliateReward>();
            }

            Func<AffiliateReward, AffiliateReward, Func<AffiliateReward, int?>, bool> compareStat = (rew, st, prop) =>
            {
                var require = prop(rew);
                if (require == null)
                {
                    return false;
                }

                var multiple = prop(st) / prop(rew);

                var claimed = db.Query<AffiliateRewardsClaim>()
                               .Where(c => c.AffiliateRewardsId == rew.Id && c.AffiliateRegistrationId == reg.Id)
                               .Count();

                var remaining = multiple - claimed;

                if (rew.IsOneTime && claimed > 0) // if one time and already claimed, cannot claim anymore
                {
                    return false;
                }

                /*
                 Require: 10 (rew)
                 Current Stat: 35 (st)

                 multiple = 35 / 10 => 3 (integer division)

                 claimed = 1
                 remaining = 2 

                 */
                return remaining > 0;
            };

            var canClaim = compareStat(rewards, stat, item => item.DirectDownlineCount) ||
                           compareStat(rewards, stat, item => item.DownlineCount) ||
                           compareStat(rewards, stat, item => item.PageViewsCount) ||
                           compareStat(rewards, stat, item => item.SalesCount) ||
                           compareStat(rewards, stat, item => item.UniqueVisitorCount);

            return canClaim;
        }

        private static Hashids hashids = new Hashids();

        /// <summary>
        /// Claim the rewards
        /// </summary>
        /// <param name="db"></param>
        /// <param name="rewardsId"></param>
        /// <param name="registrationId"></param>
        /// <returns></returns>
        public static AffiliateRewardsClaim ClaimReward(NancyBlackDatabase db, int rewardsId, int registrationId)
        {
            var canClaim = AffiliateReward.CanClaim(db, rewardsId, registrationId);
            if (canClaim == false)
            {
                return null;
            }

            var reg = db.GetById<AffiliateRegistration>(registrationId);
            var rewards = db.GetById<AffiliateReward>(rewardsId);

            if (rewards.IsCodeDiscount)
            {
                var until = DateTime.MaxValue.Ticks;

                if (rewards.CodeDicountExpiryInDays != null)
                {
                    until = DateTime.Now.AddDays(rewards.CodeDicountExpiryInDays.Value).Ticks;
                }
                if (rewards.CodeDiscountExpiryDate != null)
                {
                    until = rewards.CodeDiscountExpiryDate.Value.Ticks;
                }

                AffiliateRewardsClaim claim = null;
                db.Transaction(() =>
                {
                    Product p = new Product();
                    p.Price = rewards.CodeDiscountAmount;
                    p.Attributes = new
                    {
                        description = "Discount Rewards for " + reg.AffiliateName + " minimum purchase" + rewards.MinimumPurchaseAmount,
                        limit = rewards.MinimumPurchaseAmount,
                        onetime = true,
                        until = until
                    };
                    db.UpsertRecord(p);

                    var code = hashids.Encode(p.Id, reg.Id);
                    p.Url = "/promotions/code/" + code;
                    p.Title = "Affiliate Discount: " + code;
                    db.UpsertRecord(p);

                    claim = new AffiliateRewardsClaim();
                    claim.AffiliateRegistrationId = reg.Id;
                    claim.NcbUserId = reg.NcbUserId;
                    claim.AffiliateCode = reg.AffiliateCode;
                    claim.DiscountCode = code;
                    claim.RewardsName = rewards.Title;
                    claim.AffiliateRewardsId = rewards.Id;
                    claim.ProductId = p.Id;
                    claim.CouponAttributes = p.Attributes;
                    db.UpsertRecord(claim);
                });

                return claim;
            }

            {
                var claim = new AffiliateRewardsClaim();
                claim.AffiliateRegistrationId = reg.Id;
                claim.NcbUserId = reg.NcbUserId;
                claim.AffiliateCode = reg.AffiliateCode;
                claim.RewardsName = rewards.Title;
                claim.AffiliateRewardsId = rewards.Id;
                claim.ProductId = rewards.RewardsProductId;

                db.UpsertRecord(claim);

                return claim;
            }
        }
    }
}