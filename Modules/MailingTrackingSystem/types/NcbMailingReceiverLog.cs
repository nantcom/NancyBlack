using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.TrackingSystem.types
{
    public class NcbMailingReceiverLog : IStaticType
    {
        public int Id
        {
            get; set;
        }

        public DateTime __createdAt
        {
            get; set;
        }

        public DateTime __updatedAt
        {
            get; set;
        }

        public string Email
        {
            get; set;
        }

    }
}