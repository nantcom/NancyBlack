using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.FacebookMessengerSystem.Types
{
    public class FacebookMessengerOptIn : IStaticType
    {
        #region IStaticType Implementation
        public int Id { get; set; }
        public DateTime __createdAt { get; set; }
        public DateTime __updatedAt { get; set; }
        #endregion

        /// <summary>
        /// User Id
        /// </summary>
        public int NcbUserId { get; set; }

        /// <summary>
        /// Type of the opt-in
        /// </summary>
        public string OptInType { get; set; }

    }
}