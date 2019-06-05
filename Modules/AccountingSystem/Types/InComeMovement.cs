using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AccountingSystem.Types
{
    public class InComeMovement : IComparable
    {
        public Product Product { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime MovementDate { get; set; }
        public int SaleOrderId { get; set; }

        public bool IsEqualTo(object obj)
        {
            InComeMovement another = (InComeMovement)obj;
            bool isSame = this.Product.Id == another.Product.Id
                && this.Price == another.Price
                && this.MovementDate.DayOfYear == another.MovementDate.DayOfYear
                && this.MovementDate.Year == another.MovementDate.Year;

            return isSame;
        }

        public int CompareTo(object obj)
        {
            InComeMovement another = (InComeMovement)obj;
            bool isSameDay = this.MovementDate.DayOfYear == another.MovementDate.DayOfYear
                && this.MovementDate.Year == another.MovementDate.Year;

            if (isSameDay)
            {
                return Decimal.Compare(this.Price, another.Price) * -1;
            }
            else
            {
                return DateTime.Compare(this.MovementDate, another.MovementDate);
            }

        }
    }
}