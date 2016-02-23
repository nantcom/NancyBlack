using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class InventoryMovement : IStaticType, IHasAttachment
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
        
        public dynamic[] Attachments { get; set; }

        /// <summary>
        /// Date of this movement (not the date that the record was created)
        /// </summary>
        public DateTime MovementDate { get; set; }

        /// <summary>
        /// Product Id Involved in the movement
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Change of inventory (can be positive and negative)
        /// </summary>
        public int Change { get; set; }
        
        /// <summary>
        /// Purcahse Order's Id
        /// </summary>
        public int PurchaseOrderId { get; set; }

        /// <summary>
        /// Sale Order's Id
        /// </summary>
        public int SaleOrderId { get; set; }

        public string Note { get; set; }

        public static IEnumerable<InventoryMovement> Create(PurchaseOrder purchaseOrder)
        {
            var now = DateTime.Now;
            foreach (var item in purchaseOrder.Items)
            {
                yield return new InventoryMovement()
                {
                    PurchaseOrderId = purchaseOrder.Id,
                    Change = item.Qty,
                    ProductId = item.ProductId,
                    MovementDate = now,
                };
            }
        }

        public static IEnumerable<InventoryMovement> Create(SaleOrder saleOrder)
        {
            var now = DateTime.Now;
            foreach (var item in saleOrder.ItemsDetail)
            {
                yield return new InventoryMovement()
                {
                    SaleOrderId = saleOrder.Id,
                    Change = -(int)item.Attributes.Qty,
                    ProductId = item.Id,
                    MovementDate = now,
                };
            }
        }
    }
}