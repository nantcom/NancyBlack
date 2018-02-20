using Microsoft.WindowsAzure.Storage.Table;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{
    public class PageViewSummary : TableEntity, IStaticType
    {
        [IgnoreProperty]
        public int Id { get; set; }

        [IgnoreProperty]
        public DateTime __createdAt { get; set; }

        [IgnoreProperty]
        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Affiliate Code whichs has this page view
        /// </summary>
        public string AffiliateCode { get; set; }

        /// <summary>
        /// Url of the content
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Page View
        /// </summary>
        public long PageViews { get; set; }
        
        /// <summary>
        /// Prepares the row to be inserted to azure
        /// </summary>
        public void PrepareForAzure()
        {
            this.RowKey = this.Path.Replace('/', '-');
            this.PartitionKey = this.Path.Replace('/', '-');
        }
    }
}