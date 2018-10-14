using Nancy.Security;
using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
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
    public class NcbUser : IUserIdentity, IStaticType
    {
        public int Id { get; set; }

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
        /// Guid for looking up by nancy
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
            get;
            set;
        }

        /// <summary>
        /// Password, Hashed
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// User Profile
        /// </summary>
        public dynamic Profile { get; set; }

        /// <summary>
        /// Last time that profile was updated
        /// </summary>
        public DateTime LastProfileUpdate { get; set; }

        /// <summary>
        /// Current Code for verify and register password
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Date/Time which code was generated
        /// </summary>
        public DateTime CodeRequestDate { get; set; }

        /// <summary>
        /// Facebook App Scoped Id
        /// </summary>
        public string FacebookAppScopedId { get; set; }

        /// <summary>
        /// Facebook Messenger Platform PSID - this will help when responding to user in the future
        /// </summary>
        public string FacebookPageScopedId { get; set; }

        /// <summary>
        /// Facebook Access Token
        /// </summary>
        public dynamic FacebookAccessToken { get; set; }

        /// <summary>
        /// Google Oauth token of this user
        /// </summary>
        public dynamic GoogleOAuthToken { get; set; }

        /// <summary>
        /// Google Information about current user
        /// </summary>
        public dynamic GoogleUserInfo { get; set; }

        /// <summary>
        /// Whether this is an anonymous user
        /// </summary>
        [JsonIgnore]
        public bool IsAnonymous
        {
            get
            {
                return this.Id == 0;
            }
        }

        /// <summary>
        /// Current Affiliate Registration Record
        /// </summary>
        public AffiliateRegistration AffiliateRegistration { get; set; }

        /// <summary>
        /// Determine whether user has give claim.
        /// If user has admin claim, this method always return true
        /// </summary>
        /// <param name="claim"></param>
        /// <returns></returns>
        public bool HasClaim( string claim )
        {
            if (this.Claims == null)
            {
                return false;
            }

            // admin has all claim
            if (this.Claims.Contains("admin"))
            {
                return true;
            }
            return this.Claims.Contains(claim);
        }
        
        public NcbUser()
        {
            this.Claims = new string[0];
            this.UserName = "Anonymous";
        }

        /// <summary>
        /// Localhost Admin
        /// </summary>
        public static readonly NcbUser LocalHostAdmin = new NcbUser() { Id = 1, UserName = "LocalHostAdmin", Claims = new string[] { "admin" }};

        /// <summary>
        /// 'Anonymous'
        /// </summary>
        public const string Anonymous = "Anonymous";
    }
}