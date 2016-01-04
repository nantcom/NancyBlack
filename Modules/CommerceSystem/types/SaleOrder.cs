using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public const string WaitingForOrder = "WaitingForOrder";
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

        /// <summary>
        /// Custom Data for this Sale Order
        /// </summary>
        public dynamic CustomData { get; set; }

        /// <summary>
        /// Snapshot of product that is related to this sale order.
        /// (Just in case there is a change)
        /// </summary>
        public List<Product> ItemsDetail { get; set; }

        /// <summary>
        /// Attachments
        /// </summary>
        public dynamic[] Attachments { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public dynamic Notes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        public void UpdateSaleOrder( NancyBlackDatabase db )
        {

            // Update Total
            this.TotalAmount = 0;
            
            // snapshot the products into this sale order
            // so that if there is a change in product info later
            // we still have the one that customer sees
            this.ItemsDetail = new List<Product>();

            //lookupItemDetail is used for provent duplication
            var lookupItemDetail = new Dictionary<int, Product>();

            foreach (var item in this.Items)
            {
                var product = db.GetById<Product>(item);

                // check for duplication
                if (lookupItemDetail.ContainsKey(product.Id))
                {
                    var existProduct = lookupItemDetail[product.Id];
                    JObject attr = existProduct.Attributes;
                    attr["Qty"] = attr.Value<int>("Qty") + 1;
                }
                else
                {
                    JObject attr = product.Attributes;
                    if (attr == null)
                    {
                        attr = new JObject();
                        product.Attributes = attr;
                    }
                    attr["Qty"] = 1;
                    this.ItemsDetail.Add(product);
                    lookupItemDetail.Add(product.Id, product);
                }
                
                this.TotalAmount += product.Price;
            }
            
            db.Transaction(() =>
            {

                // need to insert to get ID
                db.UpsertRecord<SaleOrder>(this);

                this.SaleOrderIdentifier = string.Format(CultureInfo.InvariantCulture,
                        "SO{0:yyyyMMdd}-{1:000000}",
                        this.__createdAt,
                        this.Id);

                // save the SO ID again
                db.UpsertRecord<SaleOrder>(this);
            });


        }
    }
}