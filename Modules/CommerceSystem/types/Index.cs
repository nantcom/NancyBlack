using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class Index : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public string Type { get; set; }

        public long Value { get; set; }
    }
}