using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{
    public class PageView : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Name of the table
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Id of the Content being tracked
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// Information about the request being sent to server
        /// </summary>
        public dynamic Request { get; set; }

        /// <summary>
        /// Source Query string, used for Affiliate tracking
        /// </summary>
        public string AffiliateCode { get; set; }

        /// <summary>
        /// Local Pathname
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Referer
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        /// Query String
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// User IP Address
        /// </summary>
        public string UserIP { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Unique Id to help identify user
        /// </summary>
        public string UserUniqueId { get; set; }
    }
}