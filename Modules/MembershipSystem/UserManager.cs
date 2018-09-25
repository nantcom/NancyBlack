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
            
            this.RefreshRoleInCache(siteDb);

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
            int _CountRetry = 0;

            try
            {
                
                if (roles.TryGetValue(id, out role))
                {
                    return role;
                }

                _CountRetry++;

                throw new InvalidOperationException("Invalid Role Id:" + id);

            }
            catch (InvalidOperationException e)
            {
                // Retry only one time
                if ( _CountRetry > 1) {
                    throw e;
                }

                this.RefreshRoleInCache(siteDb);
            }

            return new NcbRole();

        }    
            
        /// <summary>
        /// Re-add role from DB to MemCache
        /// </summary>
        private void RefreshRoleInCache(NancyBlackDatabase siteDb)
        {
            MemoryCache.Default.Remove("Membership-RolesByName");            
            var roleByName = siteDb.Query<NcbRole>().ToDictionary(r => r.Name.ToLowerInvariant());
            MemoryCache.Default.Add("Membership-RolesByName", roleByName, DateTimeOffset.Now.AddMinutes(5));

            MemoryCache.Default.Remove("Membership-Roles");
            var roleById = siteDb.Query<NcbRole>().ToDictionary(r => r.Id);
            MemoryCache.Default.Add("Membership-Roles", roleById, DateTimeOffset.Now.AddMinutes(5));
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
                    var _claims = this.GetRoleById(siteDb, item.NcbRoleId).Claims;
                    if(_claims == null )
                    {
                        continue;
                    }

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
            //var cached = MemoryCache.Default[key];
            //if (cached != null)
            //{
            //    return cached as NcbUser;
            //}

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
            //MemoryCache.Default.Add(key, user,
            //    new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(15) });

            return user;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <param name="newProfile"></param>
        public void UpdateProfile(NancyContext context, dynamic newProfile)
        {
            var user = context.CurrentUser as NcbUser;
            if (user == null || user.IsAnonymous || user == NcbUser.LocalHostAdmin)
            {
                throw new InvalidOperationException("Cannot update profile of anonymous user or localhost user");
            }

            var siteDb = context.Items["SiteDatabase"] as NancyBlackDatabase;
            this.UpdateProfile(siteDb, (context.CurrentUser as NcbUser).Id, newProfile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <param name="newProfile"></param>
        public void UpdateProfile(NancyBlackDatabase siteDb, int userId, dynamic newProfile)
        {
            var user = siteDb.GetById<NcbUser>( userId );
            user.Profile = newProfile;

            if (user.Profile != null)
            {
                siteDb.UpsertRecord<NcbUser>(user);

                // refresh the cache after update
                var key = "User-" + user.Guid;
                MemoryCache.Default.Remove(key);
                MemoryCache.Default.Add(key, user,
                    new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(15) });
            }
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

            lock (BaseModule.GetLockObject("RoleEdit-" + site.HostName))
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
        public NcbUser Register( NancyBlackDatabase db, string userName, string email, string passwordHash, bool genCode = false, bool returnExisting = false, dynamic initialProfile = null)
        {
            var existing = db.Query<NcbUser>()
                            .Where(u => u.UserName == userName)
                            .FirstOrDefault();

            if (existing != null )
            {  
                if (returnExisting == true)
                {
                    // Update the profile
                    if (initialProfile != null)
                    {
                        existing.Profile = initialProfile;

                        // this will allow admin to add Email to User and have the profile updated
                        if (existing.Email != null && initialProfile.email == null && existing.Email.StartsWith("fb_") == false)
                        {
                            existing.Profile.email = existing.Email;
                        }

                        // if user has set the email, we extract the email into email field
                        if (initialProfile.email != null && existing.Email.StartsWith("fb_"))
                        {
                            existing.Email = initialProfile.email;
                        }

                        db.UpsertRecord(existing);
                    }

                    return existing;
                }

                throw new InvalidOperationException("Email already in use");
            }

            var user = new NcbUser();
            user.UserName = userName;
            user.Email = email;
            user.PasswordHash = passwordHash;
            user.Guid = Guid.NewGuid();
            user.Profile = initialProfile;

            if (genCode == true)
            {
                user.Code = Guid.NewGuid().ToString();
                user.CodeRequestDate = DateTime.Now;
            }

            // if user is facebook user, keep the id from profile too
            if (user.UserName.StartsWith("fb_") && user.Profile != null)
            {
                user.FacebookAppScopedId = user.Profile.id;
            }

            db.UpsertRecord(user);

            user.PasswordHash = null;

            return user;
        }

        /// <summary>
        /// Registers
        /// </summary>
        /// <param name="db"></param>
        /// <param name="registerParameters"></param>
        /// <returns></returns>
        public NcbUser Reset(NancyBlackDatabase db, string email, string passwordHash, string code)
        {
            var existing = db.Query<NcbUser>()
                            .Where(u => u.Email == email)
                            .FirstOrDefault();

            if (existing == null)
            {
                throw new InvalidOperationException("Not a valid user");
            }

            if (existing.Code != code )
            {
                throw new InvalidOperationException("Invalid Code");
            }

            existing.PasswordHash = passwordHash;
            db.UpsertRecord<NcbUser>(existing);

            return existing;
        }


        public static void GenerateUserCode(NancyBlackDatabase db, NcbUser user)
        {
            user.Code = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
            user.CodeRequestDate = DateTime.Now;

            db.UpsertRecord<NcbUser>(user);            
        }
    
    }

}