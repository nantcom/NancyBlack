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

namespace NantCom.NancyBlack.Modules
{
    public class AdminModule : BaseModule
    {
        public class SiteSettings : IStaticType
        {
            public int Id
            {
                get;
                set;
            }

            public DateTime __createdAt
            {
                get;
                set;
            }

            public DateTime __updatedAt
            {
                get;
                set;
            }

            public string js_SettingsJson { get; set; }
        }

        public AdminModule()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new string[] { "admin" });

            Get["/Admin"] = this.HandleStaticRequest("admin-dashboard", null);
            Get["/Admin/"] = this.HandleStaticRequest("admin-dashboard", null);

            Get["/Admin/sitesettings"] = this.HandleStaticRequest("admin-sitesettings", null);
            Post["/Admin/sitesettings/current"] = this.HandleRequest(this.SaveSiteSettings);

            Post["/Admin/api/testemail"] = this.HandleRequest(this.TestSendEmail);
        }
        

        private dynamic TestSendEmail(dynamic arg)
        {
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
            var settingsFile = Path.Combine(this.RootPath, "App_Data", "sitesettings.json");

            File.Copy(settingsFile, settingsFile + ".bak", true);
            File.WriteAllText(settingsFile, input.ToString());

            this.SiteDatabase.UpsertRecord( new SiteSettings()
            {
                js_SettingsJson = input.ToString()
            });

            MemoryCache.Default["CurrentSite"] = input;

            return input;
        }

    }
}