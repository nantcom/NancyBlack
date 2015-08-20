using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class PaymentLogPaysbuy : IStaticType
    {
        public string Response { get; set; }
        public int Id { get; set; }        

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
    }
}