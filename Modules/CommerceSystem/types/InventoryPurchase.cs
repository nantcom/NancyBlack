using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class InventoryPurchase : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
        
        public dynamic[] Attachments { get; set; }

        /// <summary>
        /// Id of the Supplier
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Invoice Number of the Supplier
        /// </summary>
        public string SupplierInvoiceNumber { get; set; }

        /// <summary>
        /// Account that were used to pay for this inventory
        /// </summary>
        public string PaidByAccount { get; set; }

        /// <summary>
        /// Date that the purchase was made
        /// </summary>
        public DateTime PurchasedDate { get; set; }
        
        /// <summary>
        /// Date that item should arrive
        /// </summary>
        public DateTime ProjectedReceiveDate { get; set; }

        /// <summary>
        /// Date that item actually arrived
        /// </summary>
        public DateTime ActualReceiveDate { get; set; }

        /// <summary>
        /// Product Id
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Notes
        /// </summary>
        public string Note { get; set; }
        
        /// <summary>
        /// If not 0, this item was used to fulfill the inventory item
        /// </summary>
        public int InventoryItemId { get; set; }

        /// <summary>
        /// Cost that the item was bought for, include shipping and handling but without any Tax
        /// </summary>
        public Decimal BuyingPrice { get; set; }

        /// <summary>
        /// VAT Paid when bought this item
        /// </summary>
        public Decimal BuyingTax { get; set; }
        
        /// <summary>
        /// Whether this purchase was received
        /// </summary>
        public bool IsInBound { get; set; }
    }
}