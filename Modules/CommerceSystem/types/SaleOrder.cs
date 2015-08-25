using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public sealed class SaleOrderStatus
    {
        public const string WaitingForPayment = "WaitingForPayment";
        public const string PaymentReceived = "PaymentReceived";
        public const string PaymentReceivedWithException = "PaymentReceivedWithException";
        /// <summary>
        /// Prepares product for packing - stock will now deduct
        /// </summary>
        public const string Packing = "Packing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string DuplicatePayment = "DuplicatePayment";
        public const string Cancel = "Cancel";
    }

    public class SaleOrder : IStaticType
    {
        
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
        
        /// <summary>
        /// Sale Order Identifier
        /// </summary>
        public string SaleOrderIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Receipt Identifier
        /// </summary>
        public string ReceiptIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Status of this Sale Order
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// User Id that made the purchase
        /// </summary>
        public int NcbUserId { get; set; }

        /// <summary>
        /// Customer information
        /// </summary>
        public dynamic Customer { get; set; }

        /// <summary>
        /// Shipping Address
        /// </summary>
        public NcgAddress ShipTo { get; set; }

        /// <summary>
        /// Billing Address
        /// </summary>
        public NcgAddress BillTo { get; set; }

        /// <summary>
        /// Whether billing address is used
        /// </summary>
        public bool UseBillingAddress { get; set; }

        /// <summary>
        /// Product IDs of Items in the Shopping Cart
        /// </summary>
        public int[] Items { get; set; }

        /// <summary>
        /// Total Amount of this sale order
        /// </summary>
        public Decimal TotalAmount { get; set; }

        /// <summary>
        /// Shipping Details related to this sale order
        /// </summary>
        public dynamic ShippingDetails { get; set; }
    }
}