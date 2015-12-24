using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class InventoryMovement : IStaticType, IHasAttachment
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
        
        public dynamic[] Attachments { get; set; }

        /// <summary>
        /// Date of this movement (not the date that the record was created)
        /// </summary>
        public DateTime MovementDate { get; set; }

        /// <summary>
        /// Product Id Involved in the movement
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Inbound Amount
        /// </summary>
        public int InboundAmount { get; set; }

        /// <summary>
        /// Outbound Amount
        /// </summary>
        public int CurrentAmount { get; set; }

        /// <summary>
        /// Outbound logs
        /// </summary>
        public List<OutBoundLog> OutBoundLogs { get; set; }
        
        /// <summary>
        /// Total Price buy or sell
        /// </summary>
        public Decimal TotalPrice { get; set; }
        
        /// <summary>
        /// Shipping Fee
        /// </summary>
        public Decimal ShippingFee { get; set; }

        /// <summary>
        /// Handling Fee
        /// </summary>
        public Decimal HandlingFee { get; set; }

        /// <summary>
        /// Tax
        /// </summary>
        public Decimal Tax { get; set; }

        /// <summary>
        /// Price per unit
        /// </summary>
        public Decimal PricePerUnit { get; set; }

        /// <summary>
        /// Serial Number of the item
        /// </summary>
        public string SerialNumber { get; set; }
        
        /// <summary>
        /// Purcahse Order Number
        /// </summary>
        public string PONumber { get; set; }

        /// <summary>
        /// Invoice Number
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Receipt Number
        /// </summary>
        public string ReceiptNumber { get; set; }
    }
}