using NantCom.NancyBlack.Modules.ContentSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// Represents product in the system
    /// </summary>
    public class Product : IStaticType, IHasAttachment, IContent
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

        public object[] Attachments { get; set; }

        /// <summary>
        /// Price of this product
        /// </summary>
        public Decimal Price { get; set; }
        
        /// <summary>
        /// Stock of this product
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// Actual items in inventory
        /// </summary>
        public int ActualInventory { get; set; }
    }
}