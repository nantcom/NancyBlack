using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AffiliateSystem.types
{
    public partial class AffiliateRegistration : IStaticType
    {
        /// <summary>
        /// Id of the user
        /// </summary>
        public int NcbUserId { get; set; }

        /// <summary>
        /// Code to activate affiliation in link
        /// </summary>
        public string AffiliateCode { get; set; }
        
        /// <summary>
        /// Commission Rate
        /// </summary>
        public Decimal Commission { get; set; }

        /// <summary>
        /// Address of BTC Address to be paid to
        /// </summary>
        public string BTCAddress { get; set; }

        /// <summary>
        /// the Code that was refered to this registration
        /// </summary>
        public string RefererAffiliateCode { get; set; }

        /// <summary>
        /// Friendly name of the affiliate, default to Facebook Name
        /// </summary>
        public string AffiliateName { get; set; }

        /// <summary>
        /// Message to show to buyer
        /// </summary>
        public string AffiliateMessage { get; set; }

        /// <summary>
        /// Last Date that page view was updated
        /// </summary>
        public DateTime LastPageViewUpdate { get; set; }

        /// <summary>
        /// Total Page view of this affiliate since Last Page View Update
        /// </summary>
        public int TotalPageView { get; set; }

        /// <summary>
        /// Total Unique Users
        /// </summary>
        public int TotalUniqueUser { get; set; }
        
        /// <summary>
        /// Total Unique Affiliate Clicks
        /// </summary>
        public int TotalAffiliateLinkClicks { get; set; }

        /// <summary>
        /// Total Unique Affiliate Clicks
        /// </summary>
        public int TotalSubscribeLinkClicks { get; set; }

        /// <summary>
        /// Total Sales so far
        /// </summary>
        public int TotalSales { get; set; }

        /// <summary>
        /// Any additional data
        /// </summary>
        public dynamic AdditionalData { get; set; }

        /// <summary>
        /// Bonus XP
        /// </summary>
        public int BonusPoints { get; set; }

        /// <summary>
        /// Update commission rate - calculation must be done in OnUpdateCommissionRate function
        /// </summary>
        public void UpdateCommissionRate()
        {
            this.OnUpdateCommissionRate();
        }

        partial void OnUpdateCommissionRate();

        #region Static Type Properties

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        #endregion
    }
}