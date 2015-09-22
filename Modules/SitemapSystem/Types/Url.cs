using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.SitemapSystem.Types
{
    public class SiteMapUrl
    {
        /// <summary>
        /// Location (URL)
        /// </summary>
        public string loc { get; set; }

        /// <summary>
        /// Last Moodified date
        /// </summary>
        public DateTime lastmod { get; set; }

        /// <summary>
        /// Change frequency
        /// </summary>
        public ChangeFreq changefreq { get; set; }
        
        /// <summary>
        /// Priority of the URL
        /// </summary>
        public double priority { get; set; }

        /// <summary>
        /// Change Frequency of the url
        /// </summary>
        public enum ChangeFreq
        {
            always,
            hourly,
            daily,
            weekly,
            monthly,
            yearly,
            never
        }
    }
}