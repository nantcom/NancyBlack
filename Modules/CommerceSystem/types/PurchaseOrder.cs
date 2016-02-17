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
    public class PurchaseOrderStatus
    {
        public const string New = "New";
        public const string Generated = "Generated";
        public const string Ordered = "Ordered";
        public const string Received = "Received";
    }

    public class PurchaseOrder : IStaticType
    {
        public void Combine( PurchaseItem item )
        {
            var existing = (from pi in this.Items
                           where pi.Title == item.Title
                           select pi).FirstOrDefault();

            if (existing == null)
            {
                this.Items.Add(item);
            }
            else
            {
                existing.Qty += item.Qty;
            }
        }

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// DateTime which calulate from supplier's order period
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

        /// <summary>
        /// Related Sale orders
        /// </summary>
        public List<int> LinkedSaleorder { get; set; }

        public int SupplierId { get; set; }
        
        public void Generate()
        {
            this.WasGenerated = true;
            this.Status = PurchaseOrderStatus.Generated;
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

        /// <summary>
        /// Supplier who will supply this product
        /// </summary>
        public int SupplierId { get; set; }

        public List<string> Parts { get; set; }
    }
}