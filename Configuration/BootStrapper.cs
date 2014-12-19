using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
            this.Conventions.ViewLocationConventions.Insert(0, (viewName, model, context) =>
            {
                return string.Concat( "Sites/",
                                        ((dynamic)context.Context.Items["CurrentSite"]).HostName, "/",
                                        viewName );
            });

            this.Conventions.ViewLocationConventions.Insert(1, (viewName, model, context) =>
            {
                return string.Concat( "Content/Views/",
                                        ((dynamic)context.Context.Items["CurrentSite"]).Theme, "/",
                                        viewName );
            });
            
            this.Conventions.ViewLocationConventions.Insert(2, (viewName, model, context) =>
            {
                return string.Concat( "Content/Views/", viewName );
            });

        }
    }
}