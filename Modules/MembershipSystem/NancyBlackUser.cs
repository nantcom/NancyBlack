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
        /// Whether this is an anonymous user
        /// </summary>
        [JsonIgnore]
        public bool IsAnonymous
        {
            get;
            private set;
        }

        /// <summary>
        /// All properties of user from database
        /// </summary>
        public dynamic Info
        {
            get;
            set;
        }

        /// <summary>
        /// Determine whether user has give claim.
        /// If user has admin claim, this method always return true
        /// </summary>
        /// <param name="claim"></param>
        /// <returns></returns>
        public bool HasClaim( string claim )
        {
            // admin has all claim
            if (this.Claims.Contains("admin"))
            {
                return true;
            }
            return this.Claims.Contains(claim);
        }
        
        /// <summary>
        /// Localhost Admin
        /// </summary>
        public static readonly NancyBlackUser LocalHostAdmin = new NancyBlackUser() { UserName = "LocalHostAdmin", Claims = new string[] { "admin" }, IsAnonymous = false };
        
        /// <summary>
        /// Anonymous User
        /// </summary>
        public static readonly NancyBlackUser Anonymous = new NancyBlackUser() { UserName = "Anonymous", Claims = new string[0], IsAnonymous = true };
    }
}