using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{
    // Tag use for group related content (IContent)
    public class Tag : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// this Content(IContent) seem to be only Product
        /// </summary>
        public int ContentId { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// Url containing category's url
        /// ex: content.url = "/cate1/page1"; Url will be "/cate1"
        /// </summary>
        public string Url { get; set; }
    }
}