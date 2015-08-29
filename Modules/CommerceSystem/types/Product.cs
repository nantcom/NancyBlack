﻿using NantCom.NancyBlack.Modules.ContentSystem;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// Represents product in the system
    /// </summary>
    public class Product : IStaticType, IContent
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }
        
        public DateTime __updatedAt { get; set; }

        #region IContent Members
        
        public int DisplayOrder { get; set; }

        public string Layout { get; set; }

        public string MetaDescription { get; set; }

        public string MetaKeywords { get; set; }

        public string RequiredClaims { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        #endregion

        public dynamic[] Attachments { get; set; }

        /// <summary>
        /// SKU Number
        /// </summary>
        public string SKUNumber { get; set; }
        
        /// <summary>
        /// Price of this product in home currency
        /// </summary>
        public Decimal Price { get; set; }
        
        /// <summary>
        /// Stock of this product
        /// </summary>
        public int Stock { get; set; }
        
        /// <summary>
        /// Number of items that user can buy at once
        /// </summary>
        public int MaxPerOrder { get; set; }
        
        /// <summary>
        /// Additional information about the object
        /// </summary>
        public dynamic Addendum { get; set; }

        /// <summary>
        /// Attributes of the product (such as size, color...)
        /// </summary>
        public dynamic Attributes { get; set; }

        /// <summary>
        /// Price of this product in Multi Currency
        /// </summary>
        public dynamic PriceMultiCurrency { get; set; }
        
        #region Product Variation

        /// <summary>
        /// Whether this product is a variation of main product
        /// </summary>
        public bool IsVariation { get; set; }

        /// <summary>
        /// Master product Id of this variation
        /// </summary>
        public int MasterProductId { get; set; }

        /// <summary>
        /// Whether this product has variation such as Color, Size. Product that has variation will has its stock determine by combination of all variations
        /// </summary>
        public bool HasVariation { get; set; }

        /// <summary>
        /// Variation Attributes Configuration
        /// </summary>
        public dynamic VariationAttributes { get; set; }
        
        #endregion
    }
    
}