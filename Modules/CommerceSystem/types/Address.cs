using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// Address
    /// </summary>
    public class NcgAddress : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public int NcbUserId { get; set; }

        /// <summary>
        /// To
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// To
        /// </summary>
        public string TaxId { get; set; }

        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// State/Province
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// District
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// Sub District
        /// </summary>
        public string SubDistrict { get; set; }

        /// <summary>
        /// Postal Code
        /// </summary>
        public string PostalCode { get; set; }
        
        public override bool Equals(object obj)
        {
            var other = obj as NcgAddress;
            if (other == null)
            {
                return false;
            }

            return  this.NcbUserId == other.NcbUserId &&
                    this.To.Equals(other.To, StringComparison.OrdinalIgnoreCase) &&
                    this.Address.Equals(other.Address, StringComparison.OrdinalIgnoreCase) &&
                    this.Country.Equals(other.Country, StringComparison.OrdinalIgnoreCase) &&
                    this.State.Equals(other.State, StringComparison.OrdinalIgnoreCase) &&
                    this.District.Equals(other.District, StringComparison.OrdinalIgnoreCase) &&
                    this.SubDistrict.Equals(other.SubDistrict, StringComparison.OrdinalIgnoreCase) &&
                    this.PostalCode.Equals(other.PostalCode, StringComparison.OrdinalIgnoreCase);
        }
    }
}