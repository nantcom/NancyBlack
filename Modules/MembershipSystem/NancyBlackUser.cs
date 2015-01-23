using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MembershipSystem
{

    /// <summary>
    /// A very basic Nancy User
    /// </summary>
    public class NancyBlackUser : IUserIdentity
    {
        public int Id { get; set; }

        /// <summary>
        /// Guid
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// User Roles
        /// </summary>
        public IEnumerable<string> Claims { get; set; }

        /// <summary>
        /// Current User Name
        /// </summary>
        public string UserName
        {
            get;
            set;
        }

        /// <summary>
        /// Email
        /// </summary>
        public string Email
        {
            get
            {
                return this.UserName;
            }
            set
            {
                this.UserName = value;
            }
        }

        /// <summary>
        /// Anonymous User
        /// </summary>
        public static readonly NancyBlackUser Anonymous = new NancyBlackUser() { UserName = "Anonymous", Claims = new string[0] };
    }
}