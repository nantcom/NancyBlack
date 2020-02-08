using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// Simple Price List
    /// </summary>
    public class PriceList : IStaticType
    {
        /// <summary>
        /// Id of this price 
        /// </summary>
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Id of the product
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Id of the Supplier
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Currency of this price
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Current Price of this product
        /// </summary>
        public Decimal PriceExVat { get; set; }

    }
}