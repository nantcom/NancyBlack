using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MembershipSystem
{
    public class NcbRole : IStaticType
    {
        public int Id
        {
            get;
            set;
        }

        public DateTime __createdAt
        {
            get;
            set;
        }

        public DateTime __updatedAt
        {
            get;
            set;
        }

        /// <summary>
        /// Name of this role
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Claims of this Role
        /// </summary>
        public string[] Claims { get; set; }
    }
}