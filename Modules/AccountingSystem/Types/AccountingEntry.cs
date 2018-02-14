using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;

namespace NantCom.NancyBlack.Modules.AccountingSystem.Types
{
    public class AccountingEntry : IStaticType
    {
        /// <summary>
        /// Id of the entry
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Created
        /// </summary>
        public DateTime __createdAt { get; set; }

        /// <summary>
        /// Updated
        /// </summary>
        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Date/Time of the transaction
        /// </summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Type of transaction
        /// </summary>
        public string TransactionType { get; set; }

        /// <summary>
        /// Account name that is increasing in amount
        /// </summary>
        public string IncreaseAccount { get; set; }

        /// <summary>
        /// Account name that is decreasing in amount
        /// </summary>
        public string DecreaseAccount { get; set; }

        /// <summary>
        /// Sub Account, such as Name of Client, Loaner, Borrower
        /// </summary>
        public string DebtorLoanerName { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Project Name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Related Document Number
        /// </summary>
        public string DocumentNumber { get; set; }

        /// <summary>
        /// Whether the document related to this entry is closed (such as Invoice is completed)
        /// </summary>
        public bool IsDocumentClosed { get; set; }

        /// <summary>
        /// Date/Time that this transaction is due
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Amount that increase
        /// </summary>
        public Decimal IncreaseAmount { get; set; }

        /// <summary>
        /// Amount that decrease
        /// </summary>
        public Decimal DecreaseAmount { get; set; }

        /// <summary>
        /// Id of Sale Order that create this income transaction
        /// </summary>
        public int SaleOrderId { get; set; }

        /// <summary>
        /// Id of Purchase Order that create this buy transaction
        /// </summary>
        public int PurchaseOrderId { get; set; }

        /// <summary>
        /// Id of the Inbound that create this buy transaction
        /// </summary>
        public int InventoryInboundId { get; set; }

        /// <summary>
        /// Additional Properties for this entry
        /// </summary>
        public dynamic Addendum
        {
            get;
            set;
        }
    }
}