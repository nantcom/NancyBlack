using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// use for explain the relation between SaleOrder and PurchaseOrder
    /// </summary>
    public class OrderRelation : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public int SaleOrderId { get; set; }

        public int PurchaseOrderId { get; set; }
    }
}