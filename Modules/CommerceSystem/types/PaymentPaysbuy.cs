using NantCom.NancyBlack.Modules.DatabaseSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class PaymentPaysbuy : IStaticType
    {
        public string Email { get; set; }

        public string Psb { get; set; }

        public string SecureCode { get; set; }

        public string PostbackUrl { get; set; }

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

    }
}