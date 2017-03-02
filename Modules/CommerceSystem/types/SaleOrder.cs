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
        public const string Confirmed = "Confirmed";
        public const string WaitingForOrder = "WaitingForOrder";
        public const string PreparingOrder = "PreparingOrder";
        public const string Packing = "Packing";
        public const string ReadyToShip = "ReadyToShip";
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
        /// Prefer language for customer
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Currency that this sale order was created
        /// </summary>
        public string Currency
        {
            get;
            set;
        }

        /// <summary>
        /// Conversion Rate of the currency
        /// </summary>
        public decimal CurrencyConversionRate
        {
            get;
            set;
        }

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

        public string DHLTrackingNumber { get; set; }

        public decimal PaymentFee { get; set; }

        public bool IsPayWithCreditCart { get; set; }

        /// <summary>
        /// Shipping fee
        /// </summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Shipping Insurance Fee
        /// </summary>
        public decimal ShippingInsuranceFee { get; set; }

        /// <summary>
        /// this boolean will be true when payment was made after PaymentReceived Status
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
        /// Total Amount of this sale order, include Tax
        /// </summary>
        public Decimal TotalAmount { get; set; }

        /// <summary>
        /// Total Amount of this sale order without discount
        /// </summary>
        public Decimal TotalWithoutDiscount { get; set; }

        /// <summary>
        /// Total amount of discount in this sale order
        /// </summary>
        public Decimal TotalDiscount { get; set; }

        /// <summary>
        /// Total Tax in this Sale Order
        /// </summary>
        public Decimal TotalTax { get; set; }

        /// <summary>
        /// Total Amount without Tax of this Sale Order
        /// </summary>
        public Decimal TotalAmountWithoutTax { get; set; }

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
        /// The Latest Site Settings that were used to perform calculation
        /// </summary>
        public dynamic SiteSettings { get; set; }
        
        /// <summary>
        /// Set Fees from current site settings
        /// </summary>
        /// <param name="currentSite"></param>
        public void SetAllFee(dynamic currentSite)
        {
            if (currentSite == null)
            {
                return;
            }

            if (this.ShippingDetails.method == "shipping")
            {
                this.ShippingFee = currentSite.commerce.shipping.fee;

                if (this.ShippingDetails.insurance == true)
                {
                    this.ShippingInsuranceFee = this.TotalAmount * (Decimal)currentSite.commerce.shipping.insuranceRate;
                }
            }
            else
            {
                this.ShippingFee = 0;
            }

            if (this.IsPayWithCreditCart)
            {
                this.PaymentFee = this.TotalAmount * (Decimal)currentSite.commerce.creditCardRate;
                this.PaymentFee = Math.Floor(this.PaymentFee);

                var feeParts = (from number in ((int)this.PaymentFee).ToString()
                                select int.Parse(number.ToString())).ToList();

                feeParts[feeParts.Count - 1] = 0;

                if (feeParts[feeParts.Count - 2] < 5)
                {
                    feeParts[feeParts.Count - 2] = 0; // if tenth position is less than 5 - make it 0
                }

                if (feeParts[feeParts.Count - 2] > 5)
                {
                    feeParts[feeParts.Count - 2] = 9; // if tenth position is less than 5 - make it 9
                }

                this.PaymentFee = int.Parse(string.Join("", feeParts));

                /*
                 *  $scope.PaymentFee = Math.floor($scope.PaymentFee);

                var feeParts = ($scope.PaymentFee + "").split('');
                for (var i = 0; i < feeParts.length; i++) {
                    feeParts[i] = parseInt(feeParts[i]);
                }

                feeParts[feeParts.length - 1] = 0; // last digit always 0

                if (feeParts[feeParts.length - 2] < 5) {
                    feeParts[feeParts.length - 2] = 0; // if tenth position is less than 5 - make it 0
                }
                
                if (feeParts[feeParts.length - 2] > 5) {
                    feeParts[feeParts.length - 2] = 9; // if tenth position is less than 5 - make it 9
                }
                 */
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        public void UpdateSaleOrder(dynamic currentSite, NancyBlackDatabase db, bool save = true)
        {
            // Update Total
            this.TotalAmount = 0;

            // Total without discount
            decimal totalWithoutDiscount = 0;

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

                product.ContentParts = null;

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

                this.TotalAmount += product.CurrentPrice;
                totalWithoutDiscount += product.Price;
            }

            // insert discount when there are some item with discount price (all in one discount)
            if (this.TotalAmount != totalWithoutDiscount)
            {

                // find all negative prices
                var totalNegativePrices = this.ItemsDetail.Where(i => i.CurrentPrice < 0).Sum(i => i.CurrentPrice);

                var attr = new JObject();
                attr.Add("Qty", 1);
                var discount = new Product()
                {
                    Title = "Discount",
                    Price = this.TotalAmount - totalWithoutDiscount,
                    Url = "/dummy/dummy",
                    Attributes = attr
                };
                this.ItemsDetail.Add(discount);

                this.TotalDiscount = ( discount.Price + totalNegativePrices ) * -1;
                this.TotalWithoutDiscount = totalWithoutDiscount + (totalNegativePrices *-1);
            }


            this.SetAllFee(currentSite);

            this.TotalAmount += this.ShippingFee + this.ShippingInsuranceFee + this.PaymentFee;
            this.TotalAmount = Math.Round(this.TotalAmount, 2, MidpointRounding.AwayFromZero);

            // TAX Calculation
            if (currentSite.commerce.billing.vattype == "addvat")
            {
                this.TotalAmountWithoutTax = this.TotalAmount;
                this.TotalTax = this.TotalAmountWithoutTax * (100 + (int)currentSite.commerce.billing.vatpercent) / 100;
                this.TotalAmount = this.TotalAmountWithoutTax + this.TotalTax;
            }

            if (currentSite.commerce.billing.vattype == "includevat")
            {
                this.TotalAmountWithoutTax = this.TotalAmount * 100 / (100 + (int)currentSite.commerce.billing.vatpercent);
                this.TotalTax = this.TotalAmount - this.TotalAmountWithoutTax;
            }

            if (!string.IsNullOrEmpty(this.Currency))
            {
                JObject rate = CommerceAdminModule.ExchangeRate;
                decimal want = (decimal)rate.Property(this.Currency).Value;
                decimal home = (decimal)rate.Property("THB").Value;
                this.CurrencyConversionRate = want / home;

                Func<decimal, decimal> toWant = (decimal input) => input * this.CurrencyConversionRate * 1.03m;
                foreach (Product current in this.ItemsDetail)
                {
                    current.Price = toWant(current.Price);
                    current.DiscountPrice = toWant(current.DiscountPrice);
                }

                this.ShippingFee = toWant(this.ShippingFee);
                this.ShippingInsuranceFee = toWant(this.ShippingInsuranceFee);
                this.PaymentFee = toWant(this.PaymentFee);
                this.TotalAmount = toWant(this.TotalAmount);
            }

            this.SiteSettings = currentSite;

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

        public void AddItem(NancyBlackDatabase db, dynamic currentSite, int itemId)
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
                newItem.ContentParts = null;
                this.ItemsDetail.Add(newItem);
            }
            else
            {
                JObject attr = existItem.Attributes;
                attr["Qty"] = attr.Value<int>("Qty") + 1;
            }

            if (newItem.IsPromotionPrice)
            {
                var discount = this.ItemsDetail.Where(p => p.Url == "/dummy/dummy").FirstOrDefault();
                if (discount != null)
                {
                    discount.Price += newItem.CurrentPrice - newItem.Price;
                }
            }

            this.TotalAmount = this.TotalAmount - (this.ShippingFee + this.ShippingInsuranceFee + this.PaymentFee);
            this.TotalAmount += newItem.CurrentPrice;
            this.SetAllFee(currentSite);
            this.TotalAmount += this.ShippingFee + this.ShippingInsuranceFee + this.PaymentFee;

            db.UpsertRecord<SaleOrder>(this);
        }

        /// <summary>
        /// Apply promotion code
        /// </summary>
        /// <param name="code"></param>
        public PromotionApplyResult ApplyPromotion(dynamic currentSite, NancyBlackDatabase db, string code)
        {
            // Sale order
            this.UpdateSaleOrder(currentSite, db, false);

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
