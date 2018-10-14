using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.IO;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NantCom.NancyBlack.Modules.MembershipSystem;
using System.Collections.Generic;
using System.Web.Routing;
using NantCom.NancyBlack.Configuration;
using Nancy.Conventions;
using System.Diagnostics;
using System.Reflection;
using Nancy.Cookies;

namespace NantCom.NancyBlack
{
    public static class ContextExt
    {
        /// <summary>
        /// Get Site Settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static dynamic GetSiteSettings( this NancyContext ctx )
        {
            return ctx.Items["CurrentSite"];
        }

        /// <summary>
        /// Get Site Database
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static NancyBlackDatabase GetSiteDatabase(this NancyContext ctx)
        {
            return ctx.Items["SiteDatabase"] as NancyBlackDatabase;
        }

        /// <summary>
        /// Gets root path
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static string GetRootPath(this NancyContext ctx)
        {
            return BootStrapper.RootPath;
        }
    }
}

namespace NantCom.NancyBlack.Configuration
{
    public interface IPipelineHook
    {
        void Hook(IPipelines p);
    }

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
            BootStrapper.RootPath = this.RootPathProvider.GetRootPath();

            // create App_Data
            Directory.CreateDirectory(Path.Combine(BootStrapper.RootPath, "App_Data"));
            
            ModuleResource.ReadSystemsAndResources(BootStrapper.RootPath);

            this.Conventions.ViewLocationConventions.Clear();

            #region Localized View Conventions

            // Site's View Folder has most priority
            // Mobile View Overrides
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("Language") == false)
                {
                    return string.Empty;
                }

                string u = context.Context.Request.Headers.UserAgent.ToLowerInvariant();
                if (u.Contains("mobile/"))
                {
                    return "Site/Views/Mobile/" + viewName + "_" + context.Context.Items["Language"];
                }

                return string.Empty; // not mobile browser

            });

            // Desktop View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("Language") == false)
                {
                    return string.Empty;
                }

                return "Site/Views/Desktop/" + viewName + "_" + context.Context.Items["Language"];
            });

            // Generic View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("Language") == false)
                {
                    return string.Empty;
                }

                return "Site/Views/" + viewName + "_" + context.Context.Items["Language"];
            });

            // Theme view location (views/_theme) can override _theme of the Theme folder
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                var theme = context.Context.GetSiteSettings().Theme;
                if (theme == null)
                {
                    return string.Empty;
                }

                if (context.Context.Items.ContainsKey("Language") == false)
                {
                    return string.Empty;
                }

                return "Themes/" + theme + "/" + viewName + "_" + context.Context.Items["Language"];
            });

            // NancyBlack's View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("Language") == false)
                {
                    return string.Empty;
                }

                return "NancyBlack/Content/Views/" + viewName + "_" + context.Context.Items["Language"];
            });

            // then try Views in Systems (AdminSystem, ContentSystem etc...)
            foreach (var system in ModuleResource.Systems)
            {
                this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
                {
                    if (context.Context.Items.ContainsKey("Language") == false)
                    {
                        return string.Empty;
                    }

                    return string.Concat("NancyBlack/Modules/",
                                         viewName,
                                         "_" ,
                                         context.Context.Items["Language"]);
                });

                this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
                {
                    if (context.Context.Items.ContainsKey("Language") == false)
                    {
                        return string.Empty;
                    }

                    return string.Concat("NancyBlack/Modules/",
                                         system,
                                         "/Views/",
                                         viewName,
                                         "_",
                                         context.Context.Items["Language"]);
                });
            }

            #endregion

            #region Sub Website View Conventions

            // Generic View for SubWebsite Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {

                string subSiteName = (string)context.Context.Items[ContextItems.SubSite];
                if (!string.IsNullOrEmpty(subSiteName))
                {
                    return "Site/SubSites/" + subSiteName + "/Views/" + viewName;
                }

                return string.Empty;
            });

            #endregion

            #region View Conventions

            // Site's View Folder has most priority
            // Mobile View Overrides
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                string u = context.Context.Request.Headers.UserAgent.ToLowerInvariant();
                if (u.Contains("mobile/"))
                {
                    return "Site/Views/Mobile/" + viewName;
                }

                return string.Empty; // not mobile browser

            });

            // Desktop View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return "Site/Views/Desktop/" + viewName;
            });

            // Generic View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return "Site/Views/" + viewName;
            });

            // Theme view location (views/_theme) can override _theme of the Theme folder
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                var theme = context.Context.GetSiteSettings().Theme;
                if (theme == null)
                {
                    return string.Empty;
                }

                return "Themes/" + theme + "/" + viewName;
            });

            // NancyBlack's View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return "NancyBlack/Content/Views/" + viewName;
            });

            // then try Views in Systems (AdminSystem, ContentSystem etc...)
            foreach (var system in ModuleResource.Systems)
            {
                this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
                {
                    return string.Concat("NancyBlack/Modules/",
                                         viewName);
                });
                this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
                {
                    return string.Concat("NancyBlack/Modules/",
                                         system,
                                         "/Views/",
                                         viewName);
                });
            }

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return viewName; // fully qualify names
            });

            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return viewName.Substring(1); // fully qualify names, remove forward slash at first
            });




            #endregion

            var formsAuthConfiguration = new FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/__membership/login",
                UserMapper = container.Resolve<IUserMapper>(),
            };
            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

            pipelines.BeforeRequest.AddItemToStartOfPipeline((ctx) =>
            {
                // Get Subsite Name if in main site will get null
                string folder = Path.Combine(BootStrapper.RootPath, "Site", "SubSites");
                if (Directory.Exists( folder))
                {
                    var subSiteNames = from subDirectories in Directory.GetDirectories(folder) select Path.GetFileName(subDirectories);
                    var matchSubSiteName = (from subSite in subSiteNames where ctx.Request.Url.HostName.Contains(subSite) select subSite).FirstOrDefault();

                    ctx.Items[ContextItems.SubSite] = matchSubSiteName;
                }
                else
                {
                    ctx.Items[ContextItems.SubSite] = null;
                }

                var db = NancyBlackDatabase.GetSiteDatabase(this.RootPathProvider.GetRootPath());
                GlobalVar.Default.Load(db);

                ctx.Items["SiteDatabase"] = db;
                ctx.Items["CurrentSite"] = AdminModule.ReadSiteSettings();
                ctx.Items["SiteSettings"] = AdminModule.ReadSiteSettings();
                ctx.Items["RootPath"] = BootStrapper.RootPath;

                if (ctx.Request.Cookies.ContainsKey("userid") == false)
                {
                    ctx.Request.Cookies.Add("userid", Guid.NewGuid().ToString());
                }

                return null;
            });

            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (ctx.Request.Cookies.ContainsKey("userid"))
                {
                    ctx.Response.Cookies.Add(
                        new NancyCookie("userid", ctx.Request.Cookies["userid"], DateTime.Now.AddDays(1)));
                }

                GlobalVar.Default.Persist(ctx.Items["SiteDatabase"] as NancyBlackDatabase);
            });

            foreach (var item in container.ResolveAll<IPipelineHook>())
            {
                item.Hook(pipelines);
            }
        }

        /// <summary>
        /// Gets the Application's root path
        /// </summary>
        public static string RootPath
        {
            get;
            private set;
        }
        

    }
}