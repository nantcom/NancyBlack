using NantCom.NancyBlack.Modules.DatabaseSystem;
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
        
        public object[] Attachments { get; set; }

        /// <summary>
        /// Product Id Involved in the movement
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Change of inventory
        /// </summary>
        public int Change { get; set; }

        /// <summary>
        /// Total Price buy or sell
        /// </summary>
        public Decimal TotalPrice { get; set; }

        /// <summary>
        /// Serial Number of the item
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Purcahse Order Number
        /// </summary>
        public string PONumber { get; set; }

        /// <summary>
        /// Invoice Number
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Receipt Number
        /// </summary>
        public string ReceiptNumber { get; set; }

        static InventoryMovement()
        {
            var interest = new string[] { "inventorymovement" };
            Action<string, NancyBlackDatabase, string, dynamic> genericHandler = (action, db, typeName, affectedRow) =>
            {
                if (interest.Contains(typeName) == false)
                {
                    return;
                }

                var movement = (InventoryMovement)affectedRow;

                lock ("UpdateProduct-" + movement.ProductId)
                {
                    var sumChange = db.Query<InventoryMovement>().Where(m => m.ProductId == movement.ProductId)
                                        .Sum(m => m.Change);

                    var affectedProduct = db.GetById<Product>(movement.ProductId);
                    affectedProduct.Stock = sumChange;
                    db.UpsertRecord<Product>(affectedProduct);
                }
            };

            NancyBlackDatabase.ObjectCreated += (a, b, c) => genericHandler("create", a, b, c);
            NancyBlackDatabase.ObjectUpdated += (a, b, c) => genericHandler("update", a, b, c);
            NancyBlackDatabase.ObjectDeleted += (a, b, c) => genericHandler("deleted", a, b, c);
        }
    }
}