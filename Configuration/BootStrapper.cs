using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using NantCom.NancyBlack.Modules;
using System.Text.RegularExpressions;

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

            // Site's View Folder has most priority
            // Mobile View Overrides
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("CurrentSite") == false)
                {
                    return string.Empty;
                }

                string u = context.Context.Request.Headers.UserAgent.ToLowerInvariant();
                if (u.Contains( "mobile/" ))
                {
                    return string.Concat("Sites/",
                                            ((dynamic)context.Context.Items["CurrentSite"]).HostName,
                                            "/Views/Mobile/",
                                            viewName);
                }

                return string.Empty; // not mobile browser

            });

            // Desktop View Location
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                if (context.Context.Items.ContainsKey("CurrentSite") == false)
                {
                    return string.Empty;
                }

                return string.Concat("Sites/",
                                        ((dynamic)context.Context.Items["CurrentSite"]).HostName,
                                        "/Views/Desktop/",
                                        viewName);
            });

            // Generic View Location
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


            // and outside
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
            
            var formsAuthConfiguration = new FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/__membership/login",
                UserMapper = container.Resolve<IUserMapper>(),
            };
            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

            SuperAdminModule.Initialize(this.RootPathProvider.GetRootPath(), pipelines);
        }

    }
}