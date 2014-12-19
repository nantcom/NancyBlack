﻿using Nancy;
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
            this.Conventions.ViewLocationConventions.Clear();

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
        }
    }
}