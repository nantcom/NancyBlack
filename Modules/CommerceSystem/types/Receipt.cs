using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class Receipt : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public int SaleOrderId { get; set; }

        public int PaymentLogId { get; set; }

        public string Identifier { get; set; }

        /// <summary>
        /// Whether the receipt is canceled
        /// </summary>
        public bool IsCanceled { get; set; }

        public void SetIdentifier()
        {
            this.Identifier = string.Format("RC{0:yyyy}-{1:000000}", DateTime.Today, this.Id);
        }
    }
}