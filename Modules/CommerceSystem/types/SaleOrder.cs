using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using NantCom.NancyBlack.Modules.MultiLanguageSystem;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public sealed class SaleOrderStatus
    {
        public const string New = "New";
        public const string Confirmed = "Confirmed";
        public const string WaitingForOrder = "WaitingForOrder";
        public const string Delay = "Delay";
        public const string OrderProcessing = "OrderProcessing";
        public const string InTransit = "InTransit";
        public const string CustomsClearance = "CustomsClearance";
        public const string Inbound = "Inbound";
        public const string WaitingForParts = "WaitingForParts";
        public const string Building = "Building";
        public const string PartialBuilding = "PartialBuilding";
        public const string Testing = "Testing";
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
        public const string ERROR_CASH_ONLY = "CASH_ONLY";
        public const string ERROR_MIN_AMOUNT = "MIN_AMOUT";
        public const string ERROR_PRODUCT_DISABLE = "ERROR_PRODUCT_DISABLE";
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
        /// Promised Due Date
        /// </summary>
        public DateTime DueDate { get; set; }

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
        /// Affiliate Code that create this sale order
        /// </summary>
        public string AffiliateCode { get; set; }

        /// <summary>
        /// Status of this Sale Order
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Payment Status of this Sale Order
        /// </summary>
        public string PaymentStatus { get; set; }

        /// <summary>
        /// Outbound DHL Tracking Number
        /// </summary>
        public string DHLTrackingNumber { get; set; }

        /// <summary>
        /// Inbound DHL Tracking Number
        /// </summary>
        public string InboundDHLTrackingNumber { get; set; }

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
        public void SetAllFee()
        {
            var currentSite = this.SiteSettings;

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

                if (feeParts.Count > 1)
                {
                    if (feeParts[feeParts.Count - 2] < 5)
                    {
                        feeParts[feeParts.Count - 2] = 0; // if tenth position is less than 5 - make it 0
                    }

                    if (feeParts[feeParts.Count - 2] > 5)
                    {
                        feeParts[feeParts.Count - 2] = 9; // if tenth position is less than 5 - make it 9
                    }

                    this.PaymentFee = int.Parse(string.Join("", feeParts));

                }

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
        /// Updates the sale order
        /// </summary>
        /// <param name="db"></param>
        public void UpdateSaleOrder(dynamic currentSite, NancyBlackDatabase db, bool save = true)
        {
            // if we dont have site settings, use the one provided
            // otherwise use the remembered one
            if (this.SiteSettings == null)
            {
                this.SiteSettings = currentSite;
            }
            else
            {
                currentSite = this.SiteSettings;
            }

            // Update Total
            this.TotalAmount = 0;

            // Total without discount
            decimal totalWithoutDiscount = 0;

            // New Logic 2018 - we will primariry use itemsdetail
            // if it already exists - so that admin can add/remove items
            // freely and customer still sees the old prices

            // generate itemsdetail list from items list if not exists
            if (this.ItemsDetail == null || this.ItemsDetail.Count == 0)
            {
                //lookupItemDetail is used for provent duplication
                var lookupItemDetail = new Dictionary<int, Product>();

                foreach (var item in this.Items)
                {
                    var product = db.GetById<Product>(item);
                    
                    product.ContentParts = null;
                    product.MetaDescription = null;
                    product.MetaKeywords = null;
                    product.Layout = null;
                    product.EnsuresGetPromotionPrice(this);

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
            }
            else
            {
                // otherwise - the items list is being generated from the itemsdetail list
                var newItemsList = new List<int>();
                foreach (var item in this.ItemsDetail)
                {
                    item.EnsuresGetPromotionPrice(this);

                    if (item.Url == "/dummy/dummy")
                    {
                        continue;
                    }

                    if (item.Attributes["Qty"] == null)
                    {
                        continue;
                    }
                    else
                    {
                        for (int i = 0; i < (int)item.Attributes["Qty"]; i++)
                        {
                            newItemsList.Add(item.Id);

                            this.TotalAmount += item.CurrentPrice;
                            totalWithoutDiscount += item.Price;
                        }
                    }
                }



                this.Items = newItemsList.ToArray();
            }

            Func<Product, int> getSortOrder = (p) =>
            {
                var orderList = new string[] {
                    "/laptops/",
                    "/monitor/",
                    "/calibrate/",
                    "/cpu/",
                    "/gpu/",
                    "/ram/",
                    "/m2/",
                    "/hdd/",
                    "/wifi/",
                    "/keyboard/",
                    "/thermal/",
                    "/os/"
                };

                if (p.Url.Contains("/promotion"))
                {
                    return int.MaxValue;
                }

                for (int i = 0; i < orderList.Length; i++)
                {
                    if (p.Url.IndexOf( orderList[i] ) > 0)
                    {
                        return i;
                    }
                }

                return int.MaxValue;
            };

            this.ItemsDetail = this.ItemsDetail.OrderBy(p => getSortOrder(p)).ToList();

            // remove the discount item
            var discountItem = this.ItemsDetail.Where(i => i.Url == "/dummy/dummy").FirstOrDefault();
            if (discountItem != null)
            {
                this.ItemsDetail.Remove(discountItem);
            }
            // insert discount when there are some item with discount price (all in one discount)
            if (this.TotalAmount != totalWithoutDiscount)
            {

                // find all negative prices
                var totalNegativePrices = this.ItemsDetail.Where(i => i.CurrentPrice < 0).Sum(i => i.CurrentPrice);

                var attr = new JObject();
                attr.Add("Qty", 1);

                discountItem = new Product()
                {
                    Title = "Discount",
                    Price = this.TotalAmount - totalWithoutDiscount,
                    Url = "/dummy/dummy",
                    Attributes = attr
                };

                this.ItemsDetail.Add(discountItem);

                this.TotalDiscount = (discountItem.Price + totalNegativePrices ) * -1;
                this.TotalWithoutDiscount = totalWithoutDiscount + (totalNegativePrices *-1);
            }
            else
            {
            }


            this.SetAllFee();

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

        public void AddItem(NancyBlackDatabase db, dynamic currentSite, int itemId, int qty = 1)
        {
            if (this.ItemsDetail == null)
            {
                this.ItemsDetail = new List<Product>();
            }

            var existItem = this.ItemsDetail.Where(p => p.Id == itemId).FirstOrDefault();

            if (existItem == null)
            {
                var newItem = db.GetById<Product>(itemId);
                JObject attr = newItem.Attributes;
                if (attr == null)
                {
                    attr = new JObject();
                    newItem.Attributes = attr;
                }
                attr["Qty"] = qty;

                newItem.ContentParts = null;
                newItem.MetaDescription = null;
                newItem.MetaKeywords = null;
                newItem.Layout = null;

                this.ItemsDetail.Add(newItem);
            }
            else
            {
                JObject attr = existItem.Attributes;
                attr["Qty"] = attr.Value<int>("Qty") + qty;
            }

            this.UpdateSaleOrder(currentSite, db, true);
        }
        
        public void RemoveItem(NancyBlackDatabase db, dynamic currentSite, Product existingItem )
        {
            if (this.ItemsDetail == null)
            {
                this.ItemsDetail = new List<Product>();
            }

            var existItem = this.ItemsDetail.Where(p => p.Id == existingItem.Id && p.__updatedAt == existingItem.__updatedAt).FirstOrDefault();

            if (existItem == null)
            {
                // existing item not found
                throw new InvalidOperationException("Item not found");
            }
            else
            {
                JObject attr = existItem.Attributes;
                var newQty = attr.Value<int>("Qty") - 1;
                existingItem.Attributes["Qty"] = newQty;
            }

            this.UpdateSaleOrder(currentSite, db, true);
        }

        /// <summary>
        /// Pull serial numbers into ItemsDetail Serial Number Attribute
        /// </summary>
        public void EnsuresSerialNumberVisible(NancyBlackDatabase db)
        {
            if (this.Status == SaleOrderStatus.Delivered ||
                this.Status == SaleOrderStatus.Shipped ||
                this.Status == SaleOrderStatus.ReadyToShip ||
                this.Status == SaleOrderStatus.Testing )
            {

                if (this.ItemsDetail.Any( p => p.Attributes == null || p.Attributes.Serial == null ))
                {
                    var ivt = db.Query<InventoryItem>().Where(row => row.SaleOrderId == this.Id).ToLookup(row => row.ProductId);

                    foreach (var item in this.ItemsDetail)
                    {
                        if (item.Attributes == null)
                        {
                            item.Attributes = new JObject();
                        }

                        item.Attributes.Serial =
                            string.Join(",", ivt[item.Id].Select(row => row.SerialNumber));

                    }

                    db.UpsertRecord(this);
                }
            }

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
                if (codeProduct.Attributes.require.ToString().Contains(","))
                {
                    string list = codeProduct.Attributes.require.ToString();
                    int[] idList = list.Split(',').Select(s => int.Parse(s)).ToArray();

                    if (idList.All( id => this.Items.Contains( id )) == true )
                    {
                        goto OK;
                    }
                }
                else
                {
                    if (this.Items.Contains((int)codeProduct.Attributes.require) == true)
                    {
                        goto OK;
                    }
                }

                return new PromotionApplyResult()
                {
                    code = code,
                    success = false,
                    attributes = codeProduct.Attributes,
                    error = PromotionApplyResult.ERROR_REQUIRE_PRODUCT
                };

                OK:
                ;
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

            if (codeProduct.Attributes.cashonly != null)
            {
                if (this.IsPayWithCreditCart == true)
                {
                    return new PromotionApplyResult()
                    {
                        code = code,
                        success = false,
                        attributes = codeProduct.Attributes,
                        error = PromotionApplyResult.ERROR_CASH_ONLY
                    };
                }
            }

            foreach (var item in this.ItemsDetail)
            {
                if (item.Attributes.disablepromo != null)
                {
                    codeProduct.Attributes.description = item.Title;

                    return new PromotionApplyResult()
                    {
                        code = code,
                        success = false,
                        attributes = codeProduct.Attributes,
                        error = PromotionApplyResult.ERROR_PRODUCT_DISABLE
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
