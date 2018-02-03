using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.NancyBlack.Modules.LogisticsSystem.Types
{
    public class LogisticsCompany : IStaticType
    {
        public string Name { get; set; }

        public string NickName { get; set; }

        public string WebSite { get; set; }

        /// <summary>
        /// for example: https://th.kerryexpress.com/en/track/?track={0}
        /// {0} is tracking code
        /// </summary>
        public string TrackingUrlFormat { get; set; }

        public string GetTrackUrl(string trackingCode)
        {
            return string.Format(this.TrackingUrlFormat, trackingCode);
        }

        #region Static Type Properties

        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        #endregion
    }
}