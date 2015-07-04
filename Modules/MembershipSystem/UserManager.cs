﻿using Nancy;
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
            NancyBlackDatabase.ObjectDeleted += NancyBlackDatabase_ObjectDeleted;
        }

        void NancyBlackDatabase_ObjectDeleted(NancyBlackDatabase db, string entity, dynamic item)
        {
            // remove cached user
            if (entity == "__Enrollment")
            {
                MemoryCache.Default.Remove("User-" + item.UserGuid);
            }
        }

        /// <summary>
        /// Gets user by Guid
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private NcbUser GetUserByGuid(Guid guid, NancyContext context)
        {
            // cache will never collide because the guid should be unique among all sites
            var key = "User-" + guid;
            var cached = MemoryCache.Default[key];
            if (cached != null)
            {
                return cached as NcbUser;
            }

            var siteDb = context.Items["SiteDatabase"] as NancyBlackDatabase;
            var user = siteDb.Query<NcbUser>()
                                .Where(u => u.Guid == guid)
                                .FirstOrDefault();

            if (user == null)
            {
                return NcbUser.Anonymous;
            }

            var roleQuery = siteDb.Query<NcbRole>();

            if (user.Roles != null)
            {
                foreach (var item in user.Roles)
                {
                    roleQuery = roleQuery.Where(r => r.Id == item);
                }
                
                user.Claims = (from item in roleQuery.ToList()
                               from claim in item.Claims
                               select claim).ToList();
            }
            
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
        /// <param name="code"></param>
        /// <param name="isFailSafe">Whether this enroll is fail safe enroll which will automatically create code</param>
        public bool EnrollUser(Guid guid, NancyContext context, string code, bool isFailSafe = false)
        {
            var user = this.GetUserByGuid(guid, context);
            if (user == NcbUser.Anonymous)
            {
                throw new InvalidOperationException("User is not found");
            }

            var siteDb = context.Items["SiteDatabase"] as NancyBlackDatabase;
            dynamic existing = siteDb.Query("__Enrollment",
                               string.Format("(Code eq '{0}')", code)).FirstOrDefault();

            if (existing == null)
            {
                if (isFailSafe == false)
                {
                    return false;
                }

                // create new enrollment record in fail safe mode
                existing = JObject.FromObject( new
                {
                    Id = 0,
                    Claim = "admin",
                    Code = code,
                });
            }
            else
            {
                if (existing.UserGuid != null &&
                    existing.UserGuid != user.Guid)
                { 
                    //someone already claimed this code
                    return false;
                }

                if (existing.UserGuid == user.Guid)
                {
                    // this user already claimed the code, just let him pass
                    return true;
                }
            }

            existing.UserId = user.Id;
            existing.UserGuid = user.Guid;
            existing.UserJSONText = JsonConvert.SerializeObject(user);

            siteDb.UpsertRecord("__Enrollment", existing);
            MemoryCache.Default.Remove("User-" + guid);

            return true;
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

        /// <summary>
        /// Ensures that the current site has the specifed role registered
        /// </summary>
        /// <param name="context"></param>
        /// <param name="role"></param>
        public void EnsureRoleRegistered( NancyContext context, string role )
        {
            dynamic site = context.Items["CurrentSite"];

            lock ("RoleEdit-" + site.HostName)
            {
                if (site.Roles == null)
                {
                    site.Roles = role;
                }
                else
                {
                    var roles = (string)site.Roles;
                    if (roles.Contains("," + role) == false)
                    {
                        site.Roles = roles + "," + role;
                    }
                }

                var siteDb = context.Items["SharedDatabase"] as NancyBlackDatabase;
                siteDb.UpsertRecord("Site", site);
            }

        }

        /// <summary>
        /// Get User from login information
        /// </summary>
        /// <param name="db"></param>
        /// <param name="email"></param>
        /// <param name="passwordHash"></param>
        /// <returns></returns>
        public NcbUser GetUserFromLogin( NancyBlackDatabase db, string email, string passwordHash )
        {
            var user = db.Query<NcbUser>()
                .Where(u => u.Email == email && u.PasswordHash == passwordHash)
                .FirstOrDefault();

            user.PasswordHash = null;
            return user;
        }

        /// <summary>
        /// Registers
        /// </summary>
        /// <param name="db"></param>
        /// <param name="registerParameters"></param>
        /// <returns></returns>
        public NcbUser Register( NancyBlackDatabase db, string email, string passwordHash )
        {
            var existing = db.Query<NcbUser>()
                            .Where(u => u.Email == email)
                            .FirstOrDefault();

            if (existing != null)
            {
                throw new InvalidOperationException("Email already in use");
            }

            var user = new NcbUser();
            user.Email = email;
            user.PasswordHash = passwordHash;
            user.Guid = Guid.NewGuid();

            db.UpsertRecord(user);

            user.PasswordHash = null;

            return user;
        }
    }

}