﻿using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class InboundItems
    {
        public int ProductId { get; set; }

        /// <summary>
        /// Quantity
        /// </summary>
        public int Qty { get; set; }

        public Decimal Price { get; set; }

        public Decimal Tax { get; set; }
    }

    /// <summary>
    /// Represents an action 
    /// </summary>
    public class InventoryInbound : IStaticType, IHasAttachment
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
        
        /// <summary>
        /// Date-Time of the inbound
        /// </summary>
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// Accounting document related to this inbound
        /// </summary>
        public string AccountingEntryDocumentNumber { get; set; }

        /// <summary>
        /// Items being inbound
        /// </summary>
        public InboundItems[] Items { get; set; }

        /// <summary>
        /// Total Amount of this Inbound Receipt, including Tax
        /// </summary>
        public Decimal TotalAmount { get; set; }

        /// <summary>
        /// Total Amount of this Inbound Receipt, without Tax
        /// </summary>
        public Decimal TotalAmountWithoutTax { get; set; }

        /// <summary>
        /// Total Tax of this Inbound Receipt
        /// </summary>
        public Decimal TotalTax { get; set; }
        
        /// <summary>
        /// Attachments
        /// </summary>
        public dynamic[] Attachments { get; set; }
        
    }
}