using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{
    /// <summary>
    /// Designates that the type has required properties
    /// for nancy white collection editor
    /// </summary>
    public interface IContent
    {
        /// <summary>
        /// Url to access this item
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// Title of this item
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Layout
        /// </summary>
        string Layout { get; set; }

        /// <summary>
        /// Display order in the collection
        /// </summary>
        int DisplayOrder { get; set; }

        /// <summary>
        /// Required Claims
        /// </summary>
        string RequiredClaims { get; set; }

        /// <summary>
        /// Meta Keywords
        /// </summary>
        string MetaKeywords { get; set; }

        /// <summary>
        /// Meta Description
        /// </summary>
        string MetaDescription { get; set; }

        /// <summary>
        /// Content Parts of this item to support editing in NancyWhite
        /// </summary>
        dynamic ContentParts { get; set; }

        /// <summary>
        /// Name of the table which stores this data
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Attachments
        /// </summary>
        dynamic[] Attachments { get; set; }
    }
}
