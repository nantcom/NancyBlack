using Nancy;
using Nancy.Bootstrapper;
using Nancy.ModelBinding;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json.Linq;
using RazorEngine;
using SisoDb.SqlCe4;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public class SuperAdminModule : AdminModule
    {
        public SuperAdminModule(IRootPathProvider rootPath)
            : base(rootPath)
        {
            Get["/SuperAdmin/Tables/{table_name}"] = this.HandleSuperAdminTabaleRequests;

            Get["/SuperAdmin"] = this.HandleSysAdminDashboard();
            Get["/SuperAdmin/"] = this.HandleSysAdminDashboard();

            Get["/SuperAdmin/Tables"] = this.HandleStaticRequest("admin-tables", () =>
            {
                return new
                {
                    Table = "DataType",
                    Layout = "_superadmin"
                };

            });

            Get["/system/tables/DataType"] = this.HandleListDataTypeRequest(() => this.SharedDatabase);
            Post["/system/tables/DataType/Scaffold"] = this.HandleScaffoldRequest(() => this.SharedDatabase);
            Post["/system/tables/DataType"] = this.HandleRegisterDataTypeRequest(() => this.SharedDatabase);
            Patch["/system/tables/DataType/{item_id}"] = this.HandleUpdateDataTypeRequest(() => this.SharedDatabase);
            Delete["/system/tables/DataType/{item_id}"] = this.HandleDeleteDataTypeRequest(() => this.SharedDatabase);

            Post["/SuperAdmin/maplocal"] = p =>
            {
                var targetSite = this.Request.Form.targetSite;
                if (targetSite == null)
                {
                    return 400;
                }

                // maps localhost request to a given site
                _LocalhostSite = SuperAdminModule.GetSharedDatabase().Query("Site",
                                       string.Format("HostName eq '{0}'", (string)targetSite)).FirstOrDefault();

                return this.Response.AsRedirect( "/" );
            };

            Get["/SuperAdmin/resetlocal"] = p =>
            {
                _LocalhostSite = null;

                return 423;
            };
        }

        /// <summary>
        /// Gets list of all sites
        /// </summary>
        public static IEnumerable<dynamic> GetAllSites()
        {
            var sites = SuperAdminModule.GetSharedDatabase().Query("Site");
            foreach (var item in sites)
            {
                yield return (dynamic)item;
            }
        }

        private dynamic HandleSysAdminDashboard()
        {
            return this.HandleStaticRequest("superadmin-dashboard.cshtml", () =>
            {
                // get site that expire this year
                return this.SharedDatabase.Query("Site");
            });
        }

        protected dynamic HandleSuperAdminTabaleRequests(dynamic arg)
        {
            var table_name = (string)arg.table_name;
            var replace = this.Context.Request.Query.regenerate == "true";

            var type = this.SharedDatabase.DataType.FromName(table_name);
            if (type == null)
            {
                return 404;
            }

            this.GenerateSuperAdminView(type.OriginalName, replace);

            return View["superadmin-" + arg.table_name, this.GetModel(type)];
        }

        /// <summary>
        /// Generates the admin view for current site
        /// </summary>
        /// <param name="table_name">The table_name.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <exception cref="System.InvalidOperationException">Entity: + table_name +  does not exists, Insert some sample data before running this page.</exception>
        protected void GenerateSuperAdminView(string table_name, bool replace = false)
        {
            var templatePath = Path.Combine(
                                    _RootPath,
                                    "Modules",
                                    "SuperAdminSystem",
                                    "Views");

            this.GenerateView(this.SharedDatabase, templatePath, table_name, "_superadmin.cshtml", replace, "superadmin." + table_name);
        }

        #region Handles configuring site for the current request

        private static NancyBlackDatabase _SharedDatabase;
        private static string _SharedRootPath;
        private static dynamic _LocalhostSite;

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="rootPath"></param>
        public static void Initialize(string rootPath, IPipelines pipelines)
        {
            _SharedRootPath = rootPath;

            NancyBlackDatabase.ObjectUpdated += (sender, entity, obj) =>
            {
                if (sender != _SharedDatabase)
                {
                    return;
                }

                if (entity == "site")
                {
                    MemoryCache.Default.Remove("Site-" + obj.HostName);
                    MemoryCache.Default.Remove("SiteDatabse-" + obj.Alias);

                    if (obj.Alias != null)
                    {
                        var aliases = ((string)obj.Alias).Split(',');
                        foreach (var item in aliases)
                        {
                            MemoryCache.Default.Remove("Site-" + item);
                            MemoryCache.Default.Remove("SiteDatabse-" + item);
                        }
                    }
                }
            };


            pipelines.BeforeRequest.AddItemToStartOfPipeline(SuperAdminModule.InitializeRequest);
        }

        /// <summary>
        /// Create a shared NancyBlack Database Instance
        /// </summary>
        /// <returns></returns>
        private static NancyBlackDatabase GetSharedDatabase()
        {
            if (_SharedDatabase == null)
            {
                var sisodb = ("Data Source=" + Path.Combine(_SharedRootPath, "Sites", "Shared.sdf") + ";Persist Security Info=False")
                        .CreateSqlCe4Db()
                        .CreateIfNotExists();

                _SharedDatabase = new NancyBlackDatabase(sisodb);
            }

            return _SharedDatabase;
        }

        /// <summary>
        /// Gets site database from given Context
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        private static NancyBlackDatabase GetSiteDatabase(NancyContext ctx)
        {
            if (ctx.Items.ContainsKey("SiteDatabase"))
            {
                return ctx.Items["SiteDatabase"] as NancyBlackDatabase;
            }

            dynamic currentSite = ctx.Items["CurrentSite"];

            var key = "SiteDatabase-" + (string)currentSite.HostName;
            var cached = MemoryCache.Default.Get(key) as NancyBlackDatabase;
            if (cached != null)
            {
                ctx.Items["SiteDatabase"] = cached;
                return cached;
            }

            lock (key)
            {
                dynamic site = ctx.Items["CurrentSite"];

                var path = Path.Combine(_SharedRootPath,
                            "Sites",
                            (string)site.HostName);

                Directory.CreateDirectory(path);

                var fileName = Path.Combine(path, "Data.sdf");
                var sisodb = ("Data Source=" + fileName + ";Persist Security Info=False")
                                .CreateSqlCe4Db()
                                .CreateIfNotExists();

                cached = new NancyBlackDatabase(sisodb);

                // cache in memory and in current request
                MemoryCache.Default.Add(key, cached, DateTimeOffset.MaxValue);
                ctx.Items["SiteDatabase"] = cached;
            }

            return cached;
        }

        /// <summary>
        /// Gets the super admin site
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static dynamic GetSuperAdminSite( NancyContext ctx )
        {
            var hostname = ctx.Request.Url.HostName.Replace("www.", "");

            // allowed superadmin domains are in text files
            var allowedDomains = File.ReadAllLines(
                Path.Combine(_SharedRootPath, "Modules", "SuperAdminSystem", "alloweddomains.dat"));

            if (hostname != "localhost")
            {
                if (allowedDomains.Contains(hostname) == false)
                {
                    return 423; // not allowed
                }
            }

            // if request is from allowed domain, create a site
            // and return
            var site = JObject.FromObject( new
            {
                Id = 9999,
                HostName = "SuperAdmin",
                Alias = string.Empty,
                RegisteredDate = DateTime.Now,
                ExpireDate = DateTime.MaxValue,
                RegisteredBy = "System",
                SiteType = "SuperAdmin",
                Theme = "Basic"
            });

            return site;
        }

        private static dynamic GetSite(NancyContext ctx)
        {
            var isSuperAdmin = ctx.Request.Path.StartsWith("/SuperAdmin", StringComparison.InvariantCultureIgnoreCase);
            var isSuperAdminApi = ctx.Request.Path.StartsWith("/system/tables", StringComparison.InvariantCultureIgnoreCase);
            if (isSuperAdmin || isSuperAdminApi)
            {
                return SuperAdminModule.GetSuperAdminSite(ctx);
            }

            // localhost request
            if ( ctx.Request.Url.HostName == "localhost" )
            {
                if (_LocalhostSite == null)
                {
                    return 423;
                }

                return _LocalhostSite;
            }
            
            var sharedDatabase = SuperAdminModule.GetSharedDatabase();

            var hostname = ctx.Request.Url.HostName.Replace("www.", "");
            var key = "Site-" + hostname;

            dynamic site = MemoryCache.Default.Get(key);
            if ((site as string) == "423" )
            {
                return 423; // this was a repeated request to not configured site
            }

            if (site == null)
            {
                // check from hostname
                site = sharedDatabase.Query("Site",
                                       string.Format("HostName eq '{0}'", hostname)).FirstOrDefault();

                // then from alias
                if (site == null)
                {
                    site = sharedDatabase.Query("Site",
                                       string.Format("Alias eq '{0}'", hostname)).FirstOrDefault();

                    // alias not found, return that site was not configured
                    // and also cache the result
                    if (site == null)
                    {
                        MemoryCache.Default.Add(key, "423", new CacheItemPolicy()
                        {
                            SlidingExpiration = TimeSpan.FromMinutes(5)
                        });

                        return 423;
                    }
                }

                if (site.Theme == null)
                {
                    site.Theme = "Basic";
                }

                MemoryCache.Default.Add(key, site, new CacheItemPolicy()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return site;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static Nancy.Response InitializeRequest(NancyContext ctx)
        {
            if (ctx.Request.Url.HostName == "localhost")
            {
                ctx.CurrentUser = NancyBlackUser.LocalHostAdmin;
            }

            var currentSite = SuperAdminModule.GetSite(ctx);
            if (currentSite is int)
            {
                return currentSite;
            }
            
            ctx.Items["CurrentSite"] = currentSite;
            ctx.Items["SiteDatabase"] = SuperAdminModule.GetSiteDatabase(ctx);
            ctx.Items["SharedDatabase"] = SuperAdminModule.GetSharedDatabase();

            return null;
        }

        #endregion
    }
}