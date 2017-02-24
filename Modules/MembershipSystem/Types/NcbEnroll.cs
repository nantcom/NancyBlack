using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MembershipSystem
{
    public class NcbEnroll : IStaticType, IHiddenType
    {
        #region Standard Properties

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

        #endregion

        /// <summary>
        /// Whether this enrollment is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Code to manually enroll this role
        /// </summary>
        public Guid EnrollCode { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public int NcbUserId { get; set; }

        /// <summary>
        /// Role Id
        /// </summary>
        public int NcbRoleId { get; set; }
    }
}