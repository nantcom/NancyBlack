using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AffiliateSystem.types
{
    public class AffiliateRegistration : IStaticType
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

        #region Static Type Properties

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        #endregion
    }
}