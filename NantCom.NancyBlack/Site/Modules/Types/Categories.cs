using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Site.Modules.Types
{
    /// <summary>
    /// Promotion Categories
    /// </summary>
    public class Categories : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public string Title { get; set; }

        public string Part { get; set; }

        public bool IsShow { get; set; }
    }
}