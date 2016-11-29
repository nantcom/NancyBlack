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
        /// Amount that increase
        /// </summary>
        public double IncreaseAmount { get; set; }

        /// <summary>
        /// Amount that decrease
        /// </summary>
        public double DecreaseAmount { get; set; }

        /// <summary>
        /// Id of Sale Order that create this income transaction
        /// </summary>
        public int SaleOrderId { get; set; }

        /// <summary>
        /// Id of Purchase Order that create this buy transaction
        /// </summary>
        public int PurchaseOrderId { get; set; }

        /// <summary>
        /// Id of the product that was bought
        /// </summary>
        public int ProductId { get; set; }
    }
}