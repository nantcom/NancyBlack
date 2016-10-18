using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{

    /// <summary>
    /// Represents a Page in Nancy Black
    /// </summary>
    public class Page : IStaticType, IContent, IHasAttachment
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// URL of this Page
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Layout to be used
        /// </summary>
        public string Layout { get; set; }

        /// <summary>
        /// Required Claims
        /// </summary>
        public string RequiredClaims { get; set; }

        /// <summary>
        /// Display Order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Page Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Page Keywords
        /// </summary>
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Page Descriptions
        /// </summary>
        public string MetaDescription { get; set; }

        /// <summary>
        /// Content Parts
        /// </summary>
        public dynamic ContentParts { get; set; }

        public dynamic[] Attachments { get; set; }

        public string Tags { get; set; }

        /// <summary>
        /// Attributes of the Page (such as anything but exist one)
        /// </summary>
        public dynamic Attributes { get; set; }

        /// <summary>
        /// Translations of Title, Metakeyword, MetaDescriptions
        /// </summary>
        public dynamic SEOTranslations { get; set; }

        private string _TableName = "Page";

        /// <summary>
        /// Table which stores this data, since the page will be used by Content Module
        /// to coerce data - it can be set
        /// </summary>
        public string TableName
        {
            get
            {
                return _TableName;
            }
        }

        public void SetTableName(string tableName)
        {
            _TableName = tableName;
        }

        /// <summary>
        /// User Id of person who updated this content
        /// </summary>
        public int UpdatedBy { get; set; }

        /// <summary>
        /// User Id of person who created this content
        /// </summary>
        public int CreatedBy { get; set; }
        
    }

}