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
        /// Find the role by Name, roles are cached for 5 minutes
        /// </summary>
        /// <param name="siteDb"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private NcbRole GetRoleByName(NancyBlackDatabase siteDb, string name)
        {
            var roles = MemoryCache.Default["Membership-RolesByName"] as Dictionary<string, NcbRole>;
            if (roles == null)
            {
                roles = siteDb.Query<NcbRole>().ToDictionary(r => r.Name.ToLowerInvariant());
                MemoryCache.Default.Add("Membership-RolesByName", roles, DateTimeOffset.Now.AddMinutes(5));
            }

            name = name.ToLowerInvariant();

            NcbRole role;
            if (roles.TryGetValue(name, out role))
            {
                return role;
            }

            // Make sure admin is available
            if (name == "admin")
            {
                role = new NcbRole()
                {
                    Claims = new string[] { "admin" },
                    Name = "admin"
                };

                siteDb.UpsertRecord( role );
                MemoryCache.Default.Remove("Membership-RolesByName");
                MemoryCache.Default.Remove("Membership-Roles");

                return role;
            }

            throw new InvalidOperationException("Invalid Role Name: " + name );
        }

        /// <summary>
        /// Find the role by ID, roles are cached for 5 minutes
        /// </summary>
        /// <param name="siteDb"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private NcbRole GetRoleById(NancyBlackDatabase siteDb, int id )
        {
            var roles = MemoryCache.Default["Membership-Roles"] as Dictionary<int, NcbRole>;
            if (roles == null)
            {
                roles = siteDb.Query<NcbRole>().ToDictionary(r => r.Id);
                MemoryCache.Default.Add("Membership-Roles", roles, DateTimeOffset.Now.AddMinutes(5));
            }

            NcbRole role;
            if (roles.TryGetValue( id, out role))
            {
                return role;
            }

            throw new InvalidOperationException("Invalid Role Id:" + id);
        }

        /// <summary>
        /// Find User's Claim
        /// </summary>
        /// <param name="siteDb"></param>
        /// <param name="user"></param>
        private void AssignClaims(NancyBlackDatabase siteDb, NcbUser user )
        {
            var enroll = siteDb.Query<NcbEnroll>()
                            .Where(e => e.IsActive && e.NcbUserId == user.Id)
                            .ToList();

            if (enroll.Count > 0)
            {
                var claims = new List<string>();
                foreach (var item in enroll)
                {
                    claims.AddRange(from c in this.GetRoleById(siteDb, item.NcbRoleId).Claims
                                    select c);
                }

                user.Claims = claims;
            }
            else
            {
                user.Claims = new string[0];
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

            user.PasswordHash = null;
            this.AssignClaims(siteDb, user);

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
        public bool EnrollUser(Guid guid, NancyContext context, Guid code, bool isFailSafe = false)
        {
            var user = this.GetUserByGuid(guid, context);
            if (user == NcbUser.Anonymous)
            {
                throw new InvalidOperationException("User is not found");
            }
            
            var siteDb = context.Items["SiteDatabase"] as NancyBlackDatabase;
            
            var existing = siteDb.Query<NcbEnroll>()
                                    .Where(e => e.EnrollCode == code)
                                    .FirstOrDefault();
            
            // code was not found, so it was not used
            if (existing == null)
            {
                if (isFailSafe == true) // only allow in faile safe mode
                {
                    // enroll user as admin
                    siteDb.UpsertRecord<NcbEnroll>(new NcbEnroll()
                    {
                        EnrollCode = code,
                        NcbRoleId = this.GetRoleByName(siteDb, "admin").Id,
                        NcbUserId = user.Id,
                        IsActive = true
                    });

                    MemoryCache.Default.Remove("User-" + guid);
                    return true;
                }

                // code was not found
                return false;
            }
            
            // code was used, and it is this user - nothing to do
            if (existing.NcbUserId == user.Id)
            {
                return true;
            }

            // someone has claimed the code
            if (existing.NcbUserId != 0)
            {
                return false;
            }

            existing.NcbUserId = user.Id;
            existing.IsActive = true;
            siteDb.UpsertRecord<NcbEnroll>(existing);

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

            if (user == null)
            {
                return null;

            }

            user.PasswordHash = null;
            this.AssignClaims(db, user);

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