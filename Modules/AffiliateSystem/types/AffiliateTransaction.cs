using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AffiliateSystem.types
{
    public class AffiliateTransaction : IStaticType
    {
        #region Static Type Properties

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        #endregion

        /// <summary>
        /// Id of the Sale Order Involved
        /// </summary>
        public int SaleOrderId { get; set; }

        /// <summary>
        /// Affiliate Code that triggers this transaction
        /// </summary>
        public string AffiliateCode { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public int NcbUserId { get; set; }

        /// <summary>
        /// Id of the affiliate
        /// </summary>
        public int AffiliateRegistrationId { get; set; }

        /// <summary>
        /// Current Rate of BTC at the time that sale order has been paid
        /// </summary>
        public Decimal BTCRate { get; set; }

        /// <summary>
        /// BTC Amount based on the rate at the time sale order has been paid
        /// </summary>
        public Decimal BTCAmount { get; set; }
        
        /// <summary>
        /// Commission Amount in Sale Order currency
        /// </summary>
        public Decimal CommissionAmount { get; set; }

        /// <summary>
        /// Whether this commission is Pending Approval
        /// </summary>
        public bool IsPendingApprove { get; set; }

        /// <summary>
        /// Whether the commission has been paid out
        /// </summary>
        public bool IsCommissionPaid { get; set; }

        /// <summary>
        /// Transaction Id of the commission payment
        /// </summary>
        public string BTCTransactionId { get; set; }

        /// <summary>
        /// BTC Address for the payment
        /// </summary>
        public string BTCAddress { get; set; }
        
    }
}