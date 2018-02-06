using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.LogisticsSystem.Types
{
    public interface IShipmentTrackable
    {
        /// <summary>
        /// Date which reward has been ship out
        /// </summary>
        DateTime ShipOutDate { get; set; }

        /// <summary>
        /// id of logistics company which handle shipment
        /// </summary>
        int ShipByLogisticsCompanyId { get; set; }

        string TrackingCode { get; set; }

        string BookingCode { get; set; }
    }
}