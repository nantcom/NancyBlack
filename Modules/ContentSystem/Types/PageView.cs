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
    }
}