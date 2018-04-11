using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class PurchaseInvoice 
    {
        public class PurchaseInvoiceItems
        {
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
            /// Product Id Involved in the movement
            /// </summary>
            public int ProductId { get; set; }

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

            /// <summary>
            /// Quantity
            /// </summary>
            public int Qty { get; set; }
        }
        
        public List<PurchaseInvoiceItems> Items { get; set; }

        /// <summary>
        /// Shipping Cost, will get split into buying cost
        /// </summary>
        public decimal Shipping { get; set; }

        /// <summary>
        /// Shipping Invoice Number
        /// </summary>
        public string ShippingInvoiceNumber { get; set; }

        /// <summary>
        /// Additional Costs, will get split into buying cost
        /// </summary>
        public decimal AdditionalCost { get; set; }

        /// <summary>
        /// Total Tax
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Total, includes VAT, shipping and additional costs
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Total value of products in this invoice
        /// </summary>
        public decimal TotalProductValue { get; set; }

        /// <summary>
        /// Whether price include vat
        /// </summary>
        public bool IsPriceIncludeVat { get; set; }

        /// <summary>
        /// Suppler Id
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Supplier Invoice Number
        /// </summary>
        public string SupplierInvoiceNumber { get; set; }

        /// <summary>
        /// Paid by Account
        /// </summary>
        public string PaidByAccount { get; set; }

        /// <summary>
        /// Date/Time of Purchase
        /// </summary>
        public DateTime PurchasedDate { get; set; }

        /// <summary>
        /// Expected Receive Date
        /// </summary>
        public DateTime ProjectedReceiveDate { get; set; }

        /// <summary>
        /// Whether buying with credit
        /// </summary>
        public bool IsConsignment { get; set; }

        /// <summary>
        /// Due Date of payment
        /// </summary>
        public DateTime ConsignmentDueDate { get; set; }

        /// <summary>
        /// Effective Date of tax related to this invoice
        /// </summary>
        public DateTime TaxEffectiveDate { get; set; }

        /// <summary>
        /// Whether this invoice will create GL entries
        /// </summary>
        public bool IsRecordGL { get; set; }
    }
}