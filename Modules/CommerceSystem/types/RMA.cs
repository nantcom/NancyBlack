using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class RMA : IStaticType, IHasAttachment
    {
        public int Id { get; set; }

        /// <summary>
        /// RMA number: "RMA{0:yyyyMMdd}" + Id
        /// </summary>
        public string RMAIdentifier { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public int FromSaleOrderId { get; set; }

        public int BillToSaleOrderId { get; set; }

        private List<int> _RMAItemsId;
        public List<int> RMAItemsId
        {
            get
            {
                if (_RMAItemsId == null)
                {
                    _RMAItemsId = new List<int>();
                }
                return _RMAItemsId;
            }
            set
            {
                _RMAItemsId = value;
            }
        }

        public RMAShipment InboundShipment { get; set; }

        public RMAShipment OutboundShipment { get; set; }

        public string Title { get; set; }

        public string Status { get; set; }

        /// <summary>
        /// contain: custimer's name, number, address, ...
        /// </summary>
        public dynamic Customer { get; set; }

        /// <summary>
        /// Issue which noted by customer
        /// </summary>
        public string Issue { get; set; }

        /// <summary>
        /// cause of customer's issue
        /// </summary>
        public string CauseOfIssue { get; set; }

        /// <summary>
        /// the messange that we will not show to custimer
        /// in case: your opinion which strongly against custimer's interview
        /// ex: there is physicaly crack on item but customer refuse to accept
        /// </summary>
        public string PrivateNote { get; set; }

        public dynamic[] Attachments { get; set; }

        public bool IsInWarranty { get; set; }

        public void RemoveRMAItem(NancyBlackDatabase db, RMAItem item)
        {
            db.DeleteRecord<RMAItem>(item);
            db.UpsertRecord<RMA>(this);
        }

        public void AddRMAItem(NancyBlackDatabase db, RMAItem item, bool saveRMA = true)
        {
            item = db.UpsertRecord<RMAItem>(item);
            this.RMAItemsId.Add(item.Id);
            if (saveRMA)
            {
                db.UpsertRecord<RMA>(this);
            }
        }

        public IEnumerable<RMAItem> GetRMAItems(NancyBlackDatabase db)
        {
            if (this.RMAItemsId == null)
            {
                yield break;
            }

            foreach (var itemId in this.RMAItemsId)
            {
                yield return db.GetById<RMAItem>(itemId);
            }
        }
    }

    public class RMAItem : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public string Note { get; set; }

        /// <summary>
        /// product's id which relate to defective or broken item
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Serial number of defective or broken item
        /// </summary>
        public string FromSerial { get; set; }

        /// <summary>
        /// Exchange item's serial number. Item can be exchanged or new one
        /// </summary>
        public string ToSerial { get; set; }
    }

    public class RMAShipment
    {
        public string TrackingCode { get; set; }
        public DateTime ShipmentDate { get; set; }
        public string Method { get; set; }

        /// <summary>
        /// Address for Customer's Pickup/Return
        /// </summary>
        public dynamic Location { get; set; }
    }

    public static class RMAShipmentMethod
    {
        public const string DHL = "DHL";
        public const string Kerry = "Kerry";
        public const string Carrying = "Carrying";
    }
}