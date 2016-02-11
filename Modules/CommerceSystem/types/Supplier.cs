using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class OrderDucation
    {
        public const string Daily = "Daily";
        public const string Weekly = "Weekly";
        public const string Monthly = "Monthly";
    }

    public class Supplier : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public NcgAddress Address { get; set; }

        public string Url { get; set; }

        public string WeeklyOrderWhen { get; set; }

        public int MonthlyOrderWhen { get; set; }

        public TimeSpan OrderTime { get; set; }

        public string OrderPeriod { get; set; }
    }
}