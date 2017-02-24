using NantCom.NancyBlack.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Bootstrapper;

namespace NantCom.NancyBlack.Modules.MultiLanguageSystem
{
    public class LocaleHook : IPipelineHook
    {
        public void Hook(IPipelines p)
        {
            p.BeforeRequest.AddItemToEndOfPipeline((ctx) =>
           {

               // Gets the language from hostname
               var hostnameParts = ctx.Request.Url.HostName.Split('.');
               if (hostnameParts[0].Length == 2)
               {
                   ctx.Items["Language"] = hostnameParts[0];
               }

               // try setting from the settings
               if (ctx.Items.ContainsKey("Language") == false)
               {
                   var settings = ctx.GetSiteSettings();
                   if (settings.multilanguage != null &&
                       settings.multilanguage.defaultLanguage != null)
                   {
                       ctx.Items["Language"] = settings.multilanguage.defaultLanguage;
                   }


               }

               if (ctx.Items.ContainsKey("Language") == false)
               {
                   ctx.Items["Language"] = string.Empty;
               }

               return null;
           });
        }
    }
}