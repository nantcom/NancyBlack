using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Site.Modules.Types
{
    /// <summary>
    /// Content for displaying promotion or blog
    /// </summary>
    public class WhereOnSaleContent : IStaticType, IContent, IHasAttachment
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public int BrandId { get; set; }

        /// <summary>
        /// Category's Id
        /// </summary>
        public int[] CatsId { get; set; }

        public dynamic[] Attachments { get; set; }

        public dynamic ContentParts { get; set; }

        public int DisplayOrder { get; set; }

        public string Layout { get; set; }

        public string MetaDescription { get; set; }

        public string MetaKeywords { get; set; }

        public string RequiredClaims { get; set; }

        public string TableName
        {
            get
            {
                return "WhereOnSaleContent";
            }
        }

        public string Title { get; set; }

        // use for collect part url
        public string Url { get; set; }

        public dynamic SEOTranslations { get; set; }

        public string Tags { get; set; }

        public dynamic Attributes { get; set; }

        public int UpdatedBy { get; set; }

        public int CreatedBy { get; set; }
    }
}