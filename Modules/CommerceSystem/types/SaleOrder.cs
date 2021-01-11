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
        public const string InboundStock = "InboundAsStock";
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
        public const string Credit = "Credit";
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
        public const string ERROR_OTHER = "OTHER";

        /// <summary>
        /// Error message
        /// </summary>
        public string message { get; set; }

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

    public partial class SaleOrder : IStaticType
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
        /// Estimate Inbound Date
        /// </summary>
        public DateTime InboundDateEst { get; set; }

        /// <summary>
        /// The date that we set this order to be inbound
        /// </summary>
        public DateTime? InboundDate { get; set; }

        /// <summary>
        /// Date that it sale order was delivered
        /// </summary>
        public DateTime DeliveryDate { get; set; }

        /// <summary>
        /// Date that it sale order was shipped out
        /// </summary>
        public DateTime ShipOutDate { get; set; }

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
        
        /// <summary>
        /// Inbound Shipping Method
        /// </summary>
        public string InboundShippingMethod { get; set; }

        /// <summary>
        /// Inbound Tracking Number
        /// </summary>
        public string InboundTrackingNumber { get; set; }

        /// <summary>
        /// Outbound Shipping Method
        /// </summary>
        public string OutboundShippingMethod { get; set; }

        /// <summary>
        /// Outbound Tracking Number
        /// </summary>
        public string OutboundTrackingNumber { get; set; }

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

        public int NcbUserId { get; set; }

        /// <summary>
        /// Guid of user who created this sale order
        /// </summary>
        public string UserGuid { get; set; }

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

        private int[] _Items;

        /// <summary>
        /// Product IDs of Items in the Shopping Cart
        /// </summary>
        public int[] Items
        {
            get
            {
                return _Items;
            }
            set
            {
                _Items = value;
            }
        }

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

            if (this.IsPayWithCreditCart && this.TotalAmount >= 3000)
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

            //lookupItemDetail is used for prevent duplication
            var lookupItemDetail = new Dictionary<int, Product>();
            Action<int> addnewProduct = (item) =>
            {
                var product = db.GetById<Product>(item);

                if (product == null)
                {
                    return; // id does not exists
                }

                product.ContentParts = null;
                product.MetaDescription = null;
                product.MetaKeywords = null;
                product.Layout = null;
                product.PromotionReferenceDate = DateTime.Today;

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
                if (product.Price > 0)
                {
                    totalWithoutDiscount += product.Price;
                }
            };

            // generate itemsdetail list from items list if not exists
            if (this.ItemsDetail == null || this.ItemsDetail.Count == 0)
            {
                this.ItemsDetail = new List<Product>();

                foreach (var item in this.Items)
                {
                    addnewProduct(item);
                }
            }
            else
            {
                HashSet<int> processedProductId = new HashSet<int>();

                // otherwise - the items list is being generated from the itemsdetail list
                var newItemsList = new List<int>();
                foreach (var item in this.ItemsDetail)
                {
                    processedProductId.Add(item.Id);

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
                            // suppose to throw because we cannot verify product price due to cannot specify PromotionReferenceDate
                            if (item.PromotionReferenceDate == default(DateTime))
                            {
                                item.PromotionReferenceDate = this.__createdAt;
                            }

                            newItemsList.Add(item.Id);

                            this.TotalAmount += item.CurrentPrice;

                            if (item.CurrentPrice > item.Price) // this is not discount
                            {
                                totalWithoutDiscount += item.CurrentPrice;
                            }
                            else if (item.CurrentPrice < 0 && item.CurrentPrice == item.Price)
                            {
                                // do nothing
                            }
                            else
                            {
                                totalWithoutDiscount += item.Price;
                            }
                        }
                    }
                }

                foreach (var id in this.Items)
                {
                    if (processedProductId.Contains(id) == false)
                    {
                        addnewProduct(id);
                        newItemsList.Add(id);
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
                    if (p.Url.IndexOf(orderList[i]) > 0)
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

                if (this.TotalAmount > totalWithoutDiscount)
                {
                    // discount is more than older amount
                    // this can happen with item that has price = 0
                    discountItem = new Product()
                    {
                        Title = "Discount",
                        Price = 0,
                        Url = "/dummy/dummy",
                        Attributes = attr
                    };
                }
                else
                {
                    discountItem = new Product()
                    {
                        Title = "Discount",
                        Price = this.TotalAmount - (totalWithoutDiscount + totalNegativePrices),
                        Url = "/dummy/dummy",
                        Attributes = attr
                    };
                }

                this.ItemsDetail.Add(discountItem);

                this.TotalDiscount = (discountItem.Price);
                this.TotalWithoutDiscount = totalWithoutDiscount;
            }
            else
            {
                this.TotalDiscount = 0;
                this.TotalWithoutDiscount = this.TotalAmount;
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

        public void AddItem(NancyBlackDatabase db, dynamic currentSite, int itemId, int qty = 1, bool save = true)
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
                newItem.PromotionReferenceDate = DateTime.Now;

                this.ItemsDetail.Add(newItem);
            }
            else
            {
                JObject attr = existItem.Attributes;
                attr["Qty"] = attr.Value<int>("Qty") + qty;
            }

            this.UpdateSaleOrder(currentSite, db, save);
        }

        public void RemoveItem(NancyBlackDatabase db, dynamic currentSite, Product existingItem)
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
                this.Status == SaleOrderStatus.Testing)
            {

                if (this.ItemsDetail.Any(p => p.Attributes == null || p.Attributes.Serial == null))
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
        public PromotionApplyResult ApplyPromotion(dynamic currentSite, NancyBlackDatabase db, string code, bool save = true)
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

            if (codeProduct.Title.Contains("Golden Voucher"))
            {
                goto SkipAllChecks;
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

            if (codeProduct.Attributes.require != null && codeProduct.Attributes.require != "")
            {
                if (codeProduct.Attributes.require.ToString().Contains(","))
                {
                    string list = codeProduct.Attributes.require.ToString();
                    int[] idList = list.Split(',').Select(s => int.Parse(s)).ToArray();

                    if (idList.All(id => this.Items.Contains(id)) == true)
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
                var expire = new DateTime((long)codeProduct.Attributes.until, DateTimeKind.Utc);

                if (DateTime.Now.ToUniversalTime() > expire.AddDays(1)) // give 10 minutes gap
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

            // product that already has discount will always disable promotion
            foreach (var item in this.ItemsDetail)
            {
                if (item.CurrentPrice < item.Price)
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

            SkipAllChecks:
            this.AddItem(db, currentSite, codeProduct.Id, save: save);



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

            return rows.Select(row => row.RowData);
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

        /// <summary>
        /// Figure out Ship out and devliery date if not specified
        /// </summary>
        /// <param name="db"></param>
        public void FindShipoutAndDeliveryDate( NancyBlackDatabase db)
        {
#if DEBUG
            return;
#endif

            // already figured - skip
            if (this.ShipOutDate != default(DateTime) &&
                this.DeliveryDate != default(DateTime))
            {
                return;
            }

            // logic only works with delivered sale order
            if (this.Status != SaleOrderStatus.Delivered)
            {
                return;
            }

            var history = (from rv in db.Query<RowVersion>().Where(r => r.DataType == "SaleOrder" && r.Action == "update" && r.RowId == this.Id).AsEnumerable()
                          orderby rv.Id
                          let a = (rv.RowData.__updatedAt = rv.__createdAt) // make sure updated date is the date of update
                          select rv.RowData).ToList();

            if (history.Count < 2)
            {
                return;
            }

            var last = history[0];
            for (int i = 1; i < history.Count; i++)
            {
                var current = history[i];

                if (this.ShipOutDate == default(DateTime))
                {
                    if ((string)last.Status != SaleOrderStatus.Shipped &&
                        (string)current.Status == SaleOrderStatus.Shipped)
                    {
                        // updated to shipped
                        this.ShipOutDate = current.__updatedAt;
                    }

                }

                if (this.DeliveryDate == default(DateTime))
                {
                    if ((string)last.Status != SaleOrderStatus.Delivered &&
                        (string)current.Status == SaleOrderStatus.Delivered)
                    {
                        // updated to delivered
                        this.DeliveryDate = current.__updatedAt;
                    }
                }
            }

            // also no payment received date, use date created
            if (this.PaymentReceivedDate == default(DateTime))
            {
                this.PaymentReceivedDate = this.__createdAt;
            }

            if (this.ShipOutDate == default(DateTime))
            {
                if (this.DeliveryDate != default(DateTime))
                {
                    this.ShipOutDate = this.DeliveryDate;
                }
                else
                {
                    
                    // make it 2 month after payment date also cannot be figured out
                    this.ShipOutDate = this.PaymentReceivedDate.AddDays(60);
                }
            }

            if (this.DeliveryDate == default(DateTime))
            {
                // cannot figure out - use last updated date
                this.DeliveryDate = this.PaymentReceivedDate.AddDays(60);
            }

            db.Connection.Update(this); // update without messing with rowversion
        }

        /// <summary>
        /// get saleorder from NcbUser.Id (customer)
        /// </summary>
        /// <param name="ncbUserId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static List<SaleOrder> GetFromNcbUserId(int ncbUserId, NancyBlackDatabase db)
        {
            return db.Query<SaleOrder>().Where(so => so.NcbUserId == ncbUserId
                && (so.PaymentStatus == NantCom.NancyBlack.Modules.CommerceSystem.types.PaymentStatus.PaymentReceived ||
                        so.PaymentStatus == NantCom.NancyBlack.Modules.CommerceSystem.types.PaymentStatus.Deposit)).ToList();

        }

        
    }
}
