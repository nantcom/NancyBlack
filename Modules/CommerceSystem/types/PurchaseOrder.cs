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
        public PurchaseOrder() { }

        public PurchaseOrder(Supplier supplier)
        {
            this.Items = new List<PurchaseItem>();
            this.Status = PurchaseOrderStatus.New;
            this.SupplierId = supplier.Id;
            this.WasGenerated = false;

            if (supplier.OrderPeriod == "Daily")
            {
                var canOrderToday = DateTime.Today.AddTicks(supplier.OrderTime.Ticks).AddHours(-3) > DateTime.Now;
                this.ExpectedOrderDate = canOrderToday ? DateTime.Today.AddTicks(supplier.OrderTime.Ticks) : DateTime.Today.AddTicks(supplier.OrderTime.Ticks).AddDays(1);
            }
            else if (supplier.OrderPeriod == "Weekly")
            {
                var expectedDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), supplier.WeeklyOrderWhen, true);

                // get next week day
                // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
                int daysToAdd = ((int)expectedDay - (int)DateTime.Today.DayOfWeek + 7) % 7;

                // example : next monday (depend on expectedDay)
                var nextDayOfWeek = DateTime.Today.AddDays(daysToAdd).AddTicks(supplier.OrderTime.Ticks);
                var canOrderThisWeek = nextDayOfWeek.AddHours(-3) > DateTime.Now;
                this.ExpectedOrderDate = canOrderThisWeek ? nextDayOfWeek : nextDayOfWeek.AddDays(7);
            }
            else if (supplier.OrderPeriod == "Monthly")
            {
                var endOfLastMonth = DateTime.Today.AddDays(-DateTime.Today.Day);
                var expectedOrderDate = endOfLastMonth
                    .AddDays(supplier.MonthlyOrderWhen) // expectedOrderDate for this month
                    .AddTicks(supplier.OrderTime.Ticks); // expectedOrderTime for this month
                var canOrderThisMonth = expectedOrderDate.AddHours(-3) > DateTime.Now;
                this.ExpectedOrderDate = canOrderThisMonth ? expectedOrderDate : expectedOrderDate.AddMonths(1);
            }
        }

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// DateTime for order material (when being generated)
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// DateTime which calulate from supplier's order period
        /// </summary>
        public DateTime ExpectedOrderDate { get; set; }

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
                var supplierInfoString = ((JObject)soItem.Attributes).Value<string>("supplier");
                if (string.IsNullOrWhiteSpace(supplierInfoString))
                {
                    continue;
                }

                var isChasisRequired = ((JObject)soItem.Attributes).Value<string>("chasis") != null;
                var supplier = JsonConvert.DeserializeObject<JObject>(supplierInfoString);
                var isSupplierMatched = supplier.Value<int>("id") == this.SupplierId;
                var isOrderSeparatedFromChasis = supplier.Value<bool>("isOrderSeparatedFromChasis");

                if (!isSupplierMatched)
                {
                    continue;
                }
                if (isChasisRequired && !isOrderSeparatedFromChasis)
                {
                    chasisRequiredProducts.Add(soItem);
                    continue;
                }

                var supplierPartName = supplier.Value<string>("part");
                supplierPartName = supplierPartName == null ? soItem.Title : supplierPartName;

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

        public void Generate()
        {
            this.WasGenerated = true;
            this.OrderDate = DateTime.Now;
            this.PurchaseOrderIdentifier = string.Format("PO{0:yyyyMMdd}-{1:000000}", this.OrderDate, this.Id);
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