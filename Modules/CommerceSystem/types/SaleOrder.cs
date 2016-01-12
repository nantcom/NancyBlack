using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json;
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

    /// <summary>
    /// Promotion Apply Result
    /// </summary>
    public class PromotionApplyResult
    {
        public const string ERROR_NO_CODE = "NO_CODE";
        public const string ERROR_MIN_AMOUNT = "MIN_AMOUT";
        public const string ERROR_REQUIRE_PRODUCT = "REQUIRE_PRODUCT";

        /// <summary>
        /// Promotion Code
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// Amount of discount
        /// </summary>
        public decimal discount { get; set; }

        /// <summary>
        /// Attributes of the promotion
        /// </summary>
        public dynamic attributes { get; set; }

        /// <summary>
        /// Whether promotion can be applied
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// Error Code
        /// </summary>
        public string error { get; set; }
    }

    public class SaleOrder : IStaticType
    {
        
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public DateTime PaymentReceivedDate { get; set; }
        
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
        public void UpdateSaleOrder( NancyBlackDatabase db, bool save = true )
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

            if (save == false)
            {
                return; // Just update the details for calculation
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

        /// <summary>
        /// Apply promotion code
        /// </summary>
        /// <param name="code"></param>
        public PromotionApplyResult ApplyPromotion( NancyBlackDatabase db, string code )
        {
            // Sale order
            this.UpdateSaleOrder(db, false);
            
            string codeUrl = "/promotions/code/" + code;

            var codeProduct = db.Query<Product>()
                                .Where(p => p.Url == codeUrl)
                                .FirstOrDefault();

            if (codeProduct == null)
            {
                return new PromotionApplyResult()
                {
                    code = code,
                    success = false,
                    error = PromotionApplyResult.ERROR_NO_CODE
                };
            }

            if (codeProduct.Attributes.min != null)
            {
                if ((Decimal)codeProduct.Attributes.min > this.TotalAmount)
                {
                    return new PromotionApplyResult()
                    {
                        code = code,
                        success = false,
                        attributes = codeProduct.Attributes,
                        error = PromotionApplyResult.ERROR_MIN_AMOUNT
                    };
                }
            }

            if (codeProduct.Attributes.require != null)
            {
                if (this.Items.Contains((int)codeProduct.Attributes.require) == false)
                {
                    return new PromotionApplyResult()
                    {
                        code = code,
                        success = false,
                        attributes = codeProduct.Attributes,
                        error = PromotionApplyResult.ERROR_REQUIRE_PRODUCT
                    };
                }
            }

            if (codeProduct.Attributes.until != null)
            {
                var expire = new DateTime((long)codeProduct.Attributes.until);
                
                if (DateTime.Now > expire.AddMinutes(15)) // give 10 minutes gap
                {
                    return new PromotionApplyResult()
                    {
                        code = code,
                        success = false,
                        attributes = codeProduct.Attributes,
                        error = PromotionApplyResult.ERROR_NO_CODE
                    };
                }
            }

            this.Items = this.Items.Concat(new int[] { codeProduct.Id }).ToArray();

            return new PromotionApplyResult()
            {
                code = code,
                success = true,
                attributes = codeProduct.Attributes,
                discount = codeProduct.Price
            };
        }
    }
}