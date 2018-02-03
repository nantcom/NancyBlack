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
        /// Date that item was fulfilled
        /// </summary>
        public DateTime FulfilledDate { get; set; }
        
        /// <summary>
        /// Product Id Involved in the movement
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// The related inventory purchase that were used to fulfill this inventory item.
        /// If 0 but status is IsFullfilled = true - the entry was created before InventoryPurchase system.
        /// </summary>
        public int InventoryPurchaseId { get; set; }

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
        /// Price that this item was quoted in the invoice
        /// </summary>
        public Decimal QuotedPrice { get; set; }

        /// <summary>
        /// Actual Price we sell this item for, after discount in the sale order
        /// </summary>
        public Decimal SellingPrice { get; set; }

        /// <summary>
        /// Tax based on price that this item was sold for
        /// </summary>
        public Decimal SellingTax { get; set; }
        
        /// <summary>
        /// Whether this inventory item has been fullfilled by getting item from inventory purchase
        /// </summary>
        public bool IsFullfilled { get; set; }

        /// <summary>
        /// Serial Number of the Item
        /// </summary>
        public string SerialNumber { get; set; }
    }
}