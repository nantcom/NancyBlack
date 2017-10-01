using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AffiliateSystem.types
{
    public class AffiliateRewardsClaim : IStaticType
    {
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
        

        #region Static Type Properties

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        #endregion
    }
}