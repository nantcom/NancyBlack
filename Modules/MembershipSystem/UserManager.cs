using Nancy;
using Nancy.Authentication.Forms;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules.MembershipSystem
{
    /// <summary>
    /// Class which manages user
    /// </summary>
    public class UserManager : IUserMapper
    {
        /// <summary>
        /// Gets current user manager instance
        /// </summary>
        public static UserManager Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Create new instance of user manager
        /// </summary>
        public UserManager()
        {
            UserManager.Current = this;
        }

        private NancyBlackUser GetUserByGuid(Guid guid, NancyContext context)
        {
            var key = "User-" + guid;
            var cached = MemoryCache.Default[key];
            if (cached != null)
            {
                return cached as NancyBlackUser;
            }

            var siteDb = context.Items["SiteDatabase"] as NancyBlackDatabase;
            var userJson = siteDb.QueryAsJsonString("User",
                               string.Format("(Guid eq '{0}')", guid)).FirstOrDefault();

            if (userJson == null)
            {
                return NancyBlackUser.Anonymous;
            }

            var user = JsonConvert.DeserializeObject<NancyBlackUser>(userJson);
            var claims = siteDb.QueryAsJsonString("__Enrollment",
                               string.Format("(UserId eq {0})", user.Id));

            user.Claims = (from item in claims
                           let jo = JObject.Parse(item) as dynamic
                           select (string)jo.Claim).ToList();

            // cache, expires every 15 minutes
            MemoryCache.Default.Add(key, user,
                new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(15) });

            return user;
        }

        /// <summary>
        /// Enroll user with a new claim
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="context"></param>
        /// <param name="claim"></param>
        public void EnrollUser(Guid guid, NancyContext context, string claim, string code)
        {
            var user = this.GetUserByGuid(guid, context);
            if (user == NancyBlackUser.Anonymous)
            {
                throw new InvalidOperationException("User is not found");
            }
            
            var siteDb = context.Items["SiteDatabase"] as NancyBlackDatabase;
            var existing = siteDb.QueryAsJsonString("__Enrollment",
                               string.Format("(code eq '{0}')", code)).FirstOrDefault();

            if (existing != null)
            {
                throw new InvalidOperationException("Code already claimed");
            }

            var result = siteDb.UpsertRecord("__Enrollment", new
            {
                Id = 0,
                UserId = user.Id,
                Claim = claim,
                Code = code,
            });

            MemoryCache.Default.Remove("User-" + guid);
        }

        /// <summary>
        /// Maps guid to user
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Nancy.Security.IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            return this.GetUserByGuid(identifier, context);
        }
    }

}