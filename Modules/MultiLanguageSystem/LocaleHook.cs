using NantCom.NancyBlack.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Bootstrapper;
using MaxMind.GeoIP2;
using System.Reflection;
using System.IO;
using MaxMind.GeoIP2.Responses;

namespace NantCom.NancyBlack.Modules.MultiLanguageSystem
{
    public class LocaleHook : IPipelineHook
    {
        private static DatabaseReader _GeoIP;
        
        public void Hook(IPipelines p)
        {

            p.BeforeRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (_GeoIP == null)
                {
                    var path = Path.Combine( ctx.GetRootPath(), "App_Data", "GeoLite2-Country.mmdb");
                    _GeoIP = new DatabaseReader(path);
                }

                CountryResponse country;
                if ( _GeoIP.TryCountry( ctx.Request.UserHostAddress, out country) )
                {
                    ctx.Items["Country"] = country.Country.Name;
                    ctx.Items["CountryISO"] = country.Country.IsoCode.ToLowerInvariant();
                }
                else
                {
                    ctx.Items["Country"] = "Singapore";
                    ctx.Items["CountryISO"] = "sg";
                }

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