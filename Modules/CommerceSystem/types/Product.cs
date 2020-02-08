using NantCom.NancyBlack.Modules.ContentSystem;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{

    /// <summary>
    /// Represents product in the system
    /// </summary>
    public class Product :  IStaticType, IContent
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }
        
        public DateTime __updatedAt { get; set; }

        #region IContent Members
        
        public int DisplayOrder { get; set; }

        public string Layout { get; set; }

        public string MetaDescription { get; set; }

        public string MetaKeywords { get; set; }

        public string RequiredClaims { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        /// <summary>
        /// Content Parts of this product, all frontend editing will gets into this property
        /// </summary>
        public dynamic ContentParts { get; set; }

        /// <summary>
        /// Table Name - always 'Product'
        /// </summary>
        public string TableName {  get { return "Product"; } }

        /// <summary>
        /// Translations of Title, Metakeyword, MetaDescriptions
        /// </summary>
        public dynamic SEOTranslations { get; set; }

        public string Tags { get; set; }

        /// <summary>
        /// Attributes of the product (such as size, color...)
        /// </summary>
        public dynamic Attributes { get; set; }

        #endregion

        public dynamic[] Attachments { get; set; }

        /// <summary>
        /// SKU Number
        /// </summary>
        public string SKUNumber { get; set; }

        /// <summary>
        /// Barcode of this product
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// How many characters in the scanned barcode to be used when matching the barcode
        /// </summary>
        public int BarcodeSubstring { get; set; }

        /// <summary>
        /// Id of supplier who supplies this product
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Part Number of Supplier
        /// </summary>
        public string SupplierPartNumber { get; set; }

        /// <summary>
        /// Full Price of this product. Prices will be automatically set to Current Currency's Price by LocaleHook
        /// </summary>
        public Decimal Price { get; set; }

        /// <summary>
        /// Discount price of this product.  Prices will be automatically set to Current Currency's Price by LocaleHook
        /// </summary>
        public Decimal DiscountPrice { get; set; }

        private dynamic _PriceMultiCurrency;
        private dynamic _DiscountPriceMultiCurrency;

        /// <summary>
        /// Price of this product in Multi Currency
        /// </summary>
        public dynamic PriceMultiCurrency
        {
            get
            {
                if (_PriceMultiCurrency == null)
                {
                    _PriceMultiCurrency = new JObject();
                }

                return _PriceMultiCurrency;
            }
            set
            {
                _PriceMultiCurrency = value;
            }
        }

        /// <summary>
        /// Discount Price in Multi-Currency
        /// </summary>
        public dynamic DiscountPriceMultiCurrency
        {
            get
            {
                if (_DiscountPriceMultiCurrency == null)
                {
                    _DiscountPriceMultiCurrency = new JObject();
                }

                return _DiscountPriceMultiCurrency;
            }
            set
            {
                _DiscountPriceMultiCurrency = value;
            }
        }

        /// <summary>
        /// this field relate to PromotionDate. when current time still in promotion period
        /// the current price will be DiscountPrice.
        /// </summary>
        public Decimal CurrentPrice
        {
            get
            {
                if (this.IsPromotionPrice)
                {
                    return this.DiscountPrice;
                }

                return this.Price;
            }
        }

        public double PercentDiscount
        {
            get 
            {
                if (this.Price == 0)
                {
                    return 0;
                }

                return Math.Floor((double)( 1 - (this.DiscountPrice / this.Price) ));
            }
        }

        public DateTime PromotionStartDate { get; set; }

        private DateTime _PromotionEndDate;

        /// <summary>
        /// End of promotion date - will be last second of given date
        /// </summary>
        public DateTime PromotionEndDate
        {
            get
            {
                return _PromotionEndDate.Date.AddDays(1).AddSeconds(-1);
            }
            set
            {
                _PromotionEndDate = value;
            }
        }

        /// <summary>
        /// The Date that will be used for reference when checking whether user will get promotion
        /// </summary>
        public DateTime PromotionReferenceDate { get; set; }

        /// <summary>
        /// Whether user will get promotion, if reference date is set - it will be used. Otherwise reference date is today
        /// </summary>
        public bool IsPromotionPrice
        {
            get
            {
                if (this.PromotionReferenceDate != DateTime.MinValue &&
                    this.PromotionReferenceDate.Year != 2001)
                {
                    return this.PromotionReferenceDate.Date.ToLocalTime() <= this.PromotionEndDate.ToLocalTime() &&
                            this.PromotionReferenceDate.Date.ToLocalTime() >= this.PromotionStartDate.ToLocalTime();
                }

                return DateTime.Today <= this.PromotionEndDate.ToLocalTime() &&
                        DateTime.Today >= this.PromotionStartDate.ToLocalTime();
            }
        }

        /// <summary>
        /// Stock of this product
        /// </summary>
        public int Stock { get; set; }
        
        /// <summary>
        /// Number of items that user can buy at once
        /// </summary>
        public int MaxPerOrder { get; set; }
        
        /// <summary>
        /// Additional information about the object
        /// </summary>
        public dynamic Addendum { get; set; }

        /// <summary>
        /// Gets the attached picture of this product
        /// </summary>
        public string Image
        {
            get
            {
                if (this.Attachments == null)
                {
                    return null;
                }

                if (this.Attachments.Length == 0)
                {
                    return null;
                }

                // Use the type Picture
                foreach (object item in this.Attachments)
                {
                    var a = JObject.FromObject(item).ToObject<StandardAttachment>();
                    if (a.AttachmentType == "Image")
                    {
                        return a.Url;
                    }
                }

                return this.Attachments[0].Url;
                
            }
        }
        
        #region Product Variation

        /// <summary>
        /// Whether this product is a variation of main product
        /// </summary>
        public bool IsVariation { get; set; }

        /// <summary>
        /// Master product Id of this variation
        /// </summary>
        public int MasterProductId { get; set; }

        /// <summary>
        /// Whether this product has variation such as Color, Size. Product that has variation will has its stock determine by combination of all variations
        /// </summary>
        public bool HasVariation { get; set; }

        /// <summary>
        /// Variation Attributes Configuration
        /// </summary>
        public dynamic VariationAttributes { get; set; }

        /// <summary>
        /// User Id who updated this item
        /// </summary>
        public int UpdatedBy { get; set; }

        /// <summary>
        /// User Id who created this item
        /// </summary>
        public int CreatedBy { get; set; }

        #endregion
        
    }
        
}