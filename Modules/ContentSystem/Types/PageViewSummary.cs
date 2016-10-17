using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{
    public class PageViewSummary : IStaticType
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

        public int PageViews { get; set; }

        public string Url { get; set; }
    }
}