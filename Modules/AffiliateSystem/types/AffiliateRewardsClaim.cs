using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using NantCom.NancyBlack.NancyBlack.Modules.LogisticsSystem.Types;
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

        #region Static Type Properties

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        #endregion
    }
}