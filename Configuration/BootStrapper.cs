using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NantCom.NancyBlack.Modules.MembershipSystem;
using System.Collections.Generic;

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

            #region View Conventions

            this.Conventions.ViewLocationConventions.Clear();

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

            // NancyBlack's View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {

                return "Content/Views/" + viewName;
            });

            // then try Views in Systems (AdminSystem, ContentSystem etc...)
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
                ctx.Items["SiteDatabase"] = NancyBlackDatabase.GetSiteDatabase(this.RootPathProvider.GetRootPath());
                ctx.Items["CurrentSite"] = BootStrapper.GetSiteSettings();

                if (ctx.CurrentUser == null)
                {
                    ctx.CurrentUser = NancyBlackUser.Anonymous;
                    if (ctx.Request.Url.HostName == "localhost")
                    {
                        ctx.CurrentUser = NancyBlackUser.LocalHostAdmin;
                    }
                }

                return null;
            });

            if (container.CanResolve<IPipelineHook>())
            {
                container.Resolve<IPipelineHook>().Hook(pipelines);
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

        /// <summary>
        /// Gets the site settings
        /// </summary>
        /// <returns></returns>
        public static dynamic GetSiteSettings()
        {
            if (MemoryCache.Default["CurrentSite"] != null)
            {
                return MemoryCache.Default["CurrentSite"];
            }
            else
            {
                var settingsFile = Path.Combine(BootStrapper.RootPath, "App_Data", "sitesettings.json");
                var json = File.ReadAllText(settingsFile);

                var settingsObject = JObject.Parse(json);

                var cachePolicy = new CacheItemPolicy();
                cachePolicy.ChangeMonitors.Add(new HostFileChangeMonitor( new List<string>() { settingsFile } ));
                MemoryCache.Default.Add("CurrentSite", settingsObject, cachePolicy);

                return settingsObject;
            }
        }
        

    }
}