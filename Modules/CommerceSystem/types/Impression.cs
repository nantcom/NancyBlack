using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class Impression : IStaticType
    {
        public int BannerId { get; set; }

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
    }
}