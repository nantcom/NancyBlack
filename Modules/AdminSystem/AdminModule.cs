using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Runtime.Caching;
using NantCom.NancyBlack.Modules.AdminSystem.Types;
using NantCom.NancyBlack.Configuration;
using Newtonsoft.Json;

namespace NantCom.NancyBlack.Modules
{
    public class AdminModule : BaseModule
    {

        public AdminModule()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new string[] { "admin" });

            Get["/Admin"] = this.HandleViewRequest("admin-dashboard");
            Get["/Admin/"] = this.HandleViewRequest("admin-dashboard");

            Get["/Admin/sitesettings"] = this.HandleViewRequest("admin-sitesettings");
            Post["/Admin/sitesettings/current"] = this.HandleRequest(this.SaveSiteSettings);

            Post["/Admin/api/testemail"] = this.HandleRequest(this.TestSendEmail);

            Get["/tables/sitesettings"] = this.HandleRequest(this.GetSiteSettings);
            Post["/tables/sitesettings"] = this.HandleRequest(this.SaveSiteSettings);
        }

        /// <summary>
        /// Gets current site settings
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic GetSiteSettings(dynamic arg)
        {
            return new object[] { this.CurrentSite };
        }
        
        private dynamic TestSendEmail(dynamic arg)
        {
            if (this.CurrentUser.HasClaim("admin") == false)
            {
                return 403;
            }

            var target = (string)arg.body.Value.to;
            var settings = arg.body.Value.settings;
            var template = arg.body.Value.template;

            if (template != null)
            {
                MailSenderModule.SendEmail(settings, target, (string)template.Subject,
                    (string)template.Body);
            }
            else
            {
                MailSenderModule.SendEmail(settings, target, "Test Email From NancyBlack",
                    "Email was sent successfully from <a href=\"" + this.Request.Url + "\">NancyBlack</a>");

            }

            return 200;
        }
        
        private dynamic SaveSiteSettings(dynamic arg)
        {
            var input = arg.body.Value as JObject;

            AdminModule.WriteSiteSettings( this.Context, input);

            return input;
        }

        /// <summary>
        /// Writes the site settings
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newSettings"></param>
        public static void WriteSiteSettings( NancyContext context, JObject newSettings)
        {
            var settingsFile = Path.Combine(context.GetRootPath(), "Site", "sitesettings.dat");

            File.Copy(settingsFile, settingsFile + ".bak", true);
            File.WriteAllText(settingsFile, newSettings.ToString());

            context.GetSiteDatabase().UpsertRecord(new SiteSettings()
            {
                js_SettingsJson = newSettings.ToString()
            });

            MemoryCache.Default["CurrentSite"] = newSettings;
        }

        /// <summary>
        /// Read the site settings
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static dynamic ReadSiteSettings()
        {
            if (MemoryCache.Default["CurrentSite"] != null)
            {
                return MemoryCache.Default["CurrentSite"];
            }
            else
            {
                var settingsFile = Path.Combine(BootStrapper.RootPath, "Site", "sitesettings.dat");

                if (File.Exists( settingsFile ) == false)
                {
#if DEBUG
                    System.Diagnostics.Debugger.Break();
                    // NOTE: settings file is moved to Site folder
#endif
                    File.WriteAllText(settingsFile, "{}"); // need the file to monitor change
                }
                
                var json = File.ReadAllText(settingsFile);
                var settingsObject = JObject.Parse(json);

                // ensure that some critical settings exists
                if (settingsObject["lockdown"] == null)
                {
                    settingsObject["lockdown"] = JObject.FromObject(new {
                        enable = false,
                        message = "Site is under maintenance."
                    });
                }

                // automatcially enable rowversion for page and product

                if (settingsObject["watcher"] == null)
                {
                    settingsObject["watcher"] = JObject.FromObject(new
                    {
                        product = new
                        {
                            name = "Product",
                            version = true
                        },
                        page = new
                        {
                            name = "Page",
                            version = true
                        }
                    });
                }

                if (settingsObject["commerce"] == null)
                {
                    settingsObject["commerce"] = JObject.FromObject(new
                    {
                        paysbuy = new { },
                        billing = new { vattype = "" },
                        branding = new { bgcolor = "black", fgcolor = "white", accentcolor = "blue" },
                        shipping = new { fee = 500, insuranceRate = 0.035 },
                        multicurrency = new
                        {
                            openexchangeratekey = "8ecf50d998af4c2f837bfa416698784e",
                            home = "THB",
                            available = new JArray()
                        }
                    });
                }


                var cachePolicy = new CacheItemPolicy();
                cachePolicy.ChangeMonitors.Add(new HostFileChangeMonitor(new List<string>() { settingsFile }));
                MemoryCache.Default.Add("CurrentSite", settingsObject, cachePolicy);

                return settingsObject;
            }
        }
        
    }
}