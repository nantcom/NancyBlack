using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using SisoDb.SqlCe4;
using Newtonsoft.Json.Linq;

namespace NantCom.NancyBlack.Configuration
{
    public class BootStrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            StaticConfiguration.DisableErrorTraces = false;
            StaticConfiguration.Caching.EnableRuntimeViewDiscovery = true;
            StaticConfiguration.Caching.EnableRuntimeViewUpdates = true;
            
            container.Register<JsonSerializer, CustomJsonSerializer>();
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            ModuleResource.ReadSystemsAndResources(this.RootPathProvider.GetRootPath());

            #region View Conventions

            this.Conventions.ViewLocationConventions.Clear();

            // Views in Systems (AdminSystem, ContentSystem etc...)

            foreach (var system in ModuleResource.Systems)
            {
                this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
                {
                    return string.Concat("Modules/",
                                         viewName);
                });
                this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
                {
                    return string.Concat("Modules/",
                                         system,
                                         "/Views/",
                                         viewName);
                });
            }

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("CurrentSite") == false)
                {
                    return string.Empty;
                }

                return string.Concat("Sites/",
                                        ((dynamic)context.Context.Items["CurrentSite"]).HostName, 
                                        "/Views/",
                                        viewName);
            });

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("CurrentSite") == false )
                {
                    return string.Empty;
                }

                return string.Concat( "Sites/",
                                        ((dynamic)context.Context.Items["CurrentSite"]).HostName, "/",
                                        viewName );
            });

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("CurrentSite") == false)
                {
                    return string.Empty;
                }

                return string.Concat("Content/Themes/",
                                        ((dynamic)context.Context.Items["CurrentSite"]).Theme, "/",
                                        viewName);
            });

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return string.Concat( "Content/Views/",
                                        viewName ); // part of the name
            });

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return viewName; // fully qualify names
            });

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return viewName.Substring(1); // fully qualify names, remove forward slash at first
            });

            #endregion

            pipelines.BeforeRequest.AddItemToStartOfPipeline(this.InitializeSiteForRequest);
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            var formsAuthConfiguration = new FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/login",
                UserMapper = container.Resolve<IUserMapper>(),
            };
            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

        }

        private static NancyBlackDatabase _SharedDatabase;

        /// <summary>
        /// Create a shared NancyBlack Database Instance
        /// </summary>
        /// <returns></returns>
        private NancyBlackDatabase GetSharedDatabase()
        {
            if (_SharedDatabase == null)
            {
                var sisodb = ("Data Source=" + Path.Combine(this.RootPathProvider.GetRootPath(), "Sites", "Shared.sdf") + ";Persist Security Info=False")
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
        private NancyBlackDatabase GetSiteDatabase( NancyContext ctx )
        {
            if (ctx.Items.ContainsKey( "SiteDatabase" ))
            {
                return ctx.Items["SiteDatabase"] as NancyBlackDatabase;
            }

            var key = "SiteDatabse-" + ctx.Request.Url.HostName;
            var cached = MemoryCache.Default.Get(key) as NancyBlackDatabase;
            if (cached != null)
            {
                ctx.Items["SiteDatabase"] = cached;
                return cached;
            }

            lock (key)
            {
                dynamic site = ctx.Items["CurrentSite"];

                var path = Path.Combine(this.RootPathProvider.GetRootPath(),
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
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private Nancy.Response InitializeSiteForRequest(NancyContext ctx)
        {
            var sharedDatabase = this.GetSharedDatabase();

            var key = "Site-" + ctx.Request.Url.HostName;
            dynamic site = MemoryCache.Default.Get(key);
            if (site == null)
            {
                // check for existing by alias first
                site = sharedDatabase.Query("Site",
                                   string.Format("Alias eq '{0}'", ctx.Request.Url.HostName)).FirstOrDefault();

                // then by hostname
                if (site == null)
                {
                    site = sharedDatabase.Query("Site",
                                       string.Format("HostName eq '{0}'", ctx.Request.Url.HostName)).FirstOrDefault();

                    if (site == null)
                    {
                        if (ctx.Request.Path.StartsWith("/SuperAdmin") == false)
                        {
                            return 423;
                        }
                        else
                        {
                            // superadmin request, make it a site
                            site = new Site
                            {
                                HostName = ctx.Request.Url.HostName,
                                Alias = string.Empty,
                                RegisteredDate = DateTime.Now,
                                ExpireDate = DateTime.Now.AddMonths(1),
                                RegisteredBy = "System",
                                SiteType = "SuperAdmin"
                            };

                            sharedDatabase.UpsertRecord("Site", site);

                            // on first run we must convert it to JObject
                            // so that resulting view can use dynamic
                            site = JObject.FromObject(site);
                        }

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

            ctx.Items["CurrentSite"] = site;
            ctx.Items["SiteDatabase"] = this.GetSiteDatabase(ctx);
            ctx.Items["SharedDatabase"] = this.GetSharedDatabase();

            return null;
        }
    }
}