using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// initial use for record search result by serial of SaleOrder, InventoryItems and RMAItems into format
    /// </summary>
    public class SearchResult
    {
        public DateTime RecordDate { get; set; }

        public string Source { get; set; }

        /// <summary>
        /// display sample of first match data
        /// </summary>
        public string Result { get; set; }
    }
}