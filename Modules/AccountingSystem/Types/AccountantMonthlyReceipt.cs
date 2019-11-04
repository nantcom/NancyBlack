using NantCom.NancyBlack.Modules.CommerceSystem.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AccountingSystem.Types
{
    public class AccountantMonthlyReceipt
    {
        public Receipt Receipt { get; set; }

        public SaleOrder SaleOrder { get; set; }

        public PaymentLog PaymentLog { get; set; }

        public List<PaymentLog> RelatedPaymentLogs { get; set; }

        public string Status { get; set; }
    }
}