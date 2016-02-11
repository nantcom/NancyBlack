using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public static class PurchaseOrderStatus
    {
        public const string New = "New";
        public const string Confirm = "Confirm";
        public const string Ordered = "Ordered";
        public const string Received = "Received";
    }

    public class PurchaseOrder : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Date for order material
        /// </summary>
        public DateTime OrderDate { get; set; }

        public bool WasGenerated { get; set; }

        public string PurchaseOrderIdentifier { get; set; }

        public string Status { get; set; }

        private List<PurchaseItem> _Items;
        public List<PurchaseItem> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new List<PurchaseItem>();
                }
                return _Items;
            }
            set
            {
                _Items = value;
            }
        }

        public int SupplierId { get; set; }

        public void Add(SaleOrder saleOrder, NancyBlackDatabase db, bool isSaveRequired = true)
        {
            var chasisRequiredProducts = new List<Product>();
            var newPItems = new List<PurchaseItem>();

            // separate item which require chasis and matched supplier's item
            foreach (var soItem in saleOrder.ItemsDetail.OrderBy(item => item.Id))
            {
                var isChasisRequired = ((JObject)soItem.Attributes).Value<string>("chasis") != null;
                if (isChasisRequired)
                {
                    chasisRequiredProducts.Add(soItem);
                    continue;
                }

                var supplierInfoString = ((JObject)soItem.Attributes).Value<string>("supplier");
                if (string.IsNullOrWhiteSpace(supplierInfoString))
                {
                    continue;
                }

                var supplier = JsonConvert.DeserializeObject<JObject>(supplierInfoString);
                var supplierPartName = supplier.Value<string>("part");
                supplierPartName = supplierPartName == null ? soItem.Title : supplierPartName;
                var isSupplierMatched = supplier.Value<int>("id") == this.SupplierId;
                if (!isSupplierMatched)
                {
                    continue;
                }

                newPItems.Add(new PurchaseItem()
                {
                    TrackngIds = soItem.Id.ToString(),
                    Title = supplierPartName,
                    ProductTitle = soItem.Title,
                    Qty = ((JObject)soItem.Attributes).Value<int>("Qty"),
                    Parts = new List<string>()
                });
            }

            var pItemDict = this.Items.ToDictionary<PurchaseItem, string>(item => item.TrackngIds);

            foreach (var pitem in newPItems)
            {
                // find matchedChasisPart
                var matchedChasisPart = chasisRequiredProducts
                    .Where(product => product.Attributes.Value<string>("chasis").Contains(pitem.ProductTitle));

                // add parts to matched PurchaseItem
                foreach (var product in matchedChasisPart)
                {
                    pitem.Parts.Add(product.Title);
                    pitem.TrackngIds = string.Format("{0}-{1}", pitem.TrackngIds, product.Id);
                }

                if (pItemDict.ContainsKey(pitem.TrackngIds))
                {
                    PurchaseItem existPItem = pItemDict[pitem.TrackngIds];
                    existPItem.Qty += pitem.Qty;
                }
                else
                {
                    this.Items.Add(pitem);
                }
            }

            OrderRelation relation = new OrderRelation()
            {
                SaleOrderId = saleOrder.Id,
                PurchaseOrderId = this.Id
            };

            db.UpsertRecord(relation);

            if (isSaveRequired)
            {
                db.UpsertRecord(this);
            }
        }
    }

    public class PurchaseItem
    {
        /// <summary>
        /// use this instead of id witch in case 
        /// there is subpart of item this field will be like: '{mainItemId}-{subItemId1}-{subItemId2}'
        /// </summary>
        public string TrackngIds { get; set; }

        public int Qty { get; set; }

        public string Title { get; set; }

        public string ProductTitle { get; set; }

        public List<string> Parts { get; set; }
    }
}