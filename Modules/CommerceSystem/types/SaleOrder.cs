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
        public const string New = "New";
        public const string WaitingForOrder = "WaitingForOrder";
        public const string Packing = "Packing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancel = "Cancel";
    }

    public sealed class PaymentStatus
    {
        public const string WaitingForPayment = "WaitingForPayment";
        public const string PaymentReceived = "PaymentReceived";
        public const string Deposit = "Deposit";
        public const string DuplicatePayment = "DuplicatePayment";
        public const string Refunded = "Refunded";
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
        public string SaleOrderIdentifier { get; set; }

        /// <summary>
        /// Receipt Identifier
        /// </summary>
        public string ReceiptIdentifier { get; set; }

        /// <summary>
        /// Status of this Sale Order
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Payment Status of this Sale Order
        /// </summary>
        public string PaymentStatus { get; set; }

        /// <summary>
        /// this boolean will be true when payment was made with PaymentReceived Status
        /// </summary>
        public bool IsDuplicatePayment { get; set; }

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
            var oldItemsDetail = this.ItemsDetail;
            this.ItemsDetail = new List<Product>();

            //lookupItemDetail is used for provent duplication
            var lookupItemDetail = new Dictionary<int, Product>();

            foreach (var item in this.Items)
            {
                var product = db.GetById<Product>(item);

                // in case some product no longer exist
                if (product == null)
                {
                    var previousProduct = oldItemsDetail.Where(p => p.Id == item).FirstOrDefault();
                    this.ItemsDetail.Add(previousProduct);
                    continue;
                }

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

        public void AddItem(NancyBlackDatabase db, int itemId)
        {
            var list = this.Items.ToList();
            list.Add(itemId);
            this.Items = list.ToArray();

            var newItem = db.GetById<Product>(itemId);
            var existItem = this.ItemsDetail.Where(p => p.Id == itemId && p.Title == newItem.Title && p.Price == newItem.Price).FirstOrDefault();

            if (existItem == null)
            {
                JObject attr = newItem.Attributes;
                if (attr == null)
                {
                    attr = new JObject();
                    newItem.Attributes = attr;
                }
                attr["Qty"] = 1;
                this.ItemsDetail.Add(newItem);
            }
            else
            {
                JObject attr = existItem.Attributes;
                attr["Qty"] = attr.Value<int>("Qty") + 1;
            }

            this.TotalAmount += newItem.Price;
            db.UpsertRecord<SaleOrder>(this);
        }

        public void DeleteItem(NancyBlackDatabase db, int itemId)
        {
            // still thinking about how to do it
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

        public IEnumerable<object> GetRowVersions(NancyBlackDatabase db)
        {
            var rows = db.Query<RowVersion>()
                .Where(row => row.DataType == "SaleOrder" && row.RowId == this.Id)
                .ToList();

            return rows.Select(row => JsonConvert.DeserializeObject(row.js_Row));
        }

        public IEnumerable<object> GetPaymentLogs(NancyBlackDatabase db)
        {
            var query = db.Query<PaymentLog>()
                .Where(log => log.SaleOrderId == this.Id)
                .OrderBy(log => log.__createdAt);

            if (query.Count() == 0)
            {
                yield break;
            }
            
            foreach (var log in query)
            {
                yield return new
                {
                    PaymentDate = log.PaymentDate,
                    Amount = log.Amount,
                    ApCode = log.FormResponse != null ? (string)log.FormResponse.apCode : null,
                    IsPaymentSuccess = log.IsPaymentSuccess,
                    PaymentMethod = log.PaymentSource
                };
            }
        }
    }
}