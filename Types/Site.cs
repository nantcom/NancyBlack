using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NantCom.NancyBlack.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class Site
    {
        public string HostName { get; set; }

        public string Alias { get; set; }

        public DateTime RegisteredDate { get; set; }

        public DateTime ExpireDate { get; set; }

        public string RegisteredBy { get; set; }

        public string SiteType { get; set; }
    }
}
