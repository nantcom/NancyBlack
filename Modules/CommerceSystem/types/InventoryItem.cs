using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class InventoryItem : IStaticType, IHasAttachment
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
        
        public dynamic[] Attachments { get; set; }

        /// <summary>
        /// Date that the inventory was requested
        /// </summary>
        public DateTime RequestedDate { get; set; }

        /// <summary>
        /// Date of this movement (not the date that the record was created)
        /// </summary>
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// Date of item
        /// </summary>
        public DateTime OutboundDate { get; set; }
        
        /// <summary>
        /// Product Id Involved in the movement
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Purcahse Order's Id
        /// </summary>
        public int PurchaseOrderId { get; set; }

        /// <summary>
        /// Sale Order's Id
        /// </summary>
        public int SaleOrderId { get; set; }

        /// <summary>
        /// Notes regarding this movement
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Cost that the item was bought for, include shipping and handling but without any Tax
        /// </summary>
        public Decimal BuyingCost { get; set; }

        /// <summary>
        /// Tax Paid when bought this item
        /// </summary>
        public Decimal BuyingTax { get; set; }

        /// <summary>
        /// Price that this item was sold for, without Tax
        /// </summary>
        public Decimal SellingPrice { get; set; }

        /// <summary>
        /// Tax based on price that this item was sold for
        /// </summary>
        public Decimal SellingTax { get; set; }
        
        /// <summary>
        /// Whether this inventory movement has been fullfilled
        /// </summary>
        public bool IsFullfilled { get; set; }
    }
}