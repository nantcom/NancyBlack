using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// Represents Standard Attachment
    /// </summary>
    public class StandardAttachment
    {
        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string AttachmentType { get; set; }

        /// <summary>
        /// Display Order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Caption
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; }
    }
}