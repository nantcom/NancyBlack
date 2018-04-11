using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using NantCom.NancyBlack.Modules.LogisticsSystem.Types;
using NantCom.NancyBlack.Modules.MembershipSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AffiliateSystem.types
{
    public class AffiliateRewardsClaim : IStaticType, IShipmentTrackable
    {
        /// <summary>
        /// Affiliate Registration Id
        /// </summary>
        public int AffiliateRegistrationId { get; set; }

        /// <summary>
        /// User Id to send rewards to
        /// </summary>
        public int NcbUserId { get; set; }

        /// <summary>
        /// Code to activate affiliation in link
        /// </summary>
        public string AffiliateCode { get; set; }

        /// <summary>
        /// Rewards that was claimed
        /// </summary>
        public string RewardsName { get; set; }

        /// <summary>
        /// Discount Code that was given
        /// </summary>
        public string DiscountCode { get; set; }

        /// <summary>
        /// Whether rewards were sent
        /// </summary>
        public bool IsSent { get; set; }
        
        public DateTime ShipOutDate { get; set; }
        
        public int ShipByLogisticsCompanyId { get; set; }

        public string TrackingCode { get; set; }

        public string BookingCode { get; set; }

        public string SerialNumber { get; set; }

        public int IncludedInSaleOrderId { get; set; }

        #region Static Type Properties

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userId">NcbUser.Id of owner who claim rewards</param>
        /// <returns></returns>
        public static List<AffiliateRewardsClaim> GetRewards(NancyBlackDatabase data, int userId)
        {
            return data.Query<AffiliateRewardsClaim>().Where(i => i.NcbUserId == userId && i.DiscountCode == null).ToList();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userId">NcbUser.Id of owner who generate discount code</param>
        /// <returns></returns>
        public static List<AffiliateRewardsClaim> GetDiscountCodes(NancyBlackDatabase data, int userId)
        {
            return data.Query<AffiliateRewardsClaim>().Where(i => i.NcbUserId == userId && i.DiscountCode != null).ToList();
        }
    }
}