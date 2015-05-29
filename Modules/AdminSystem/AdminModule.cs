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
        public AdminModule()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new string[] { "admin" });

            // Administration pages for Table
            Get["/Admin/Tables/{table_name}"] = this.HandleTableRequests;
            
            Get["/Admin"] = this.HandleStaticRequest("admin-dashboard", null);
            Get["/Admin/"] = this.HandleStaticRequest("admin-dashboard", null);
            Get["/Admin/Tables"] = this.HandleStaticRequest("admin-tables", () =>
            {
                return new
                {
                    Table = "DataType",
                    Layout = "_admin",
                };

            });

            Get["/Admin/sitesettings"] = this.HandleStaticRequest("admin-sitesettings", null);
            Post["/Admin/sitesettings/current"] = this.HandleRequest((arg) =>
            {
                var input = arg.body.Value as JObject;
                var settingsFile = Path.Combine(this.RootPath, "App_Data", "sitesettings.json");

                File.Copy( settingsFile, settingsFile + ".bak", true);
                File.WriteAllText(settingsFile, input.ToString());

                MemoryCache.Default["CurrentSite"] = input;

                return input;
            });

            Post["/Admin/api/testemail"] = this.HandleRequest(this.TestSendEmail);


            Get["/tables/DataType"] = this.HandleListDataTypeRequest(()=> this.SiteDatabase);
            Post["/tables/DataType/Scaffold"] = this.HandleScaffoldRequest(()=> this.SiteDatabase);
            Post["/tables/DataType"] = this.HandleRegisterDataTypeRequest(()=> this.SiteDatabase);
            Patch["/tables/DataType/{item_id}"] = this.HandleUpdateDataTypeRequest(()=> this.SiteDatabase);
            Delete["/tables/DataType/{item_id}"] = this.HandleDeleteDataTypeRequest(()=> this.SiteDatabase);
        }

        #region API Requests

        protected Func<dynamic, dynamic> HandleDeleteDataTypeRequest(Func<NancyBlackDatabase> dbGetter)
        {
            return this.HandleRequest((arg) =>
            {

                var id = arg.item_id == null ? 0 : (int?)arg.item_id;
                if (id == 0)
                {
                    throw new InvalidOperationException("Id supplied is not valid");
                }

                dbGetter().DataType.RemoveType(id.Value);

                return 204;

            });
        }

        protected Func<dynamic, dynamic> HandleListDataTypeRequest(Func<NancyBlackDatabase> dbGetter)
        {
            return this.HandleRequest((arg) =>
            {
                return from type in dbGetter().DataType.RegisteredTypes
                       where type.NormalizedName.StartsWith("__") == false
                       select type;
            });
        }

        protected Func<dynamic, dynamic> HandleUpdateDataTypeRequest(Func<NancyBlackDatabase> dbGetter)
        {
            return this.HandleRequest((arg) =>
            {
                var id = arg.item_id == null ? 0 : (int?)arg.item_id;
                if (id == 0)
                {
                    throw new InvalidOperationException("Id supplied is not valid");
                }

                var dataType = (JObject)arg.body;
                return dbGetter().DataType.Register(dataType.ToObject<DataType>());
            });
        }

        protected Func<dynamic, dynamic> HandleScaffoldRequest(Func<NancyBlackDatabase> dbGetter)
        {
            return this.HandleRequest((arg) =>
            {
                var streamReader = new StreamReader(this.Request.Body);
                var json = streamReader.ReadToEnd();

                return dbGetter().DataType.Scaffold(json);
            });
        }

        protected Func<dynamic, dynamic> HandleRegisterDataTypeRequest(Func<NancyBlackDatabase> dbGetter)
        {
            return this.HandleRequest((arg) =>
            {
                var dataType = (JObject)arg.body;
                return dbGetter().DataType.Register(dataType.ToObject<DataType>());
            });
        }


        #endregion


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

        protected dynamic HandleTableRequests(dynamic arg)
        {
            var table_name = (string)arg.table_name;
            var replace = this.Context.Request.Query.regenerate == "true";

            var type = this.SiteDatabase.DataType.FromName(table_name);
            if (type == null)
            {
                return 404;
            }

            this.GenerateAdminView(type.OriginalName, replace);

            if (replace == true)
            {
                // redirect to remove query string and avoid re-generating again
                return this.Response.AsRedirect(this.Context.Request.Path);
            }

            return View["admin-" + arg.table_name, this.GetModel( type )];
        }

        public class ViewModel
        {
            public DataType DataType { get; set; }

            public string Layout { get; set; }
        }

        /// <summary>
        /// Generates the view.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="templatePath">The target template path.</param>
        /// <param name="table_name">The table_name.</param>
        /// <param name="replace">if set to <c>true</c>, view will be replaced.</param>
        /// <exception cref="System.InvalidOperationException">Entity: + table_name +  does not exists, Insert some sample data before running this page.</exception>
        protected void GenerateView(NancyBlackDatabase db, 
                                    string templatePath, 
                                    string table_name, 
                                    string layout,
                                    bool replace = false,            
                                    string fileName = null)
        {
            if (fileName == null)
            {
                fileName = table_name;
            }

            var templateFile = Path.Combine(
                                    templatePath,
                                    fileName + ".cshtml");


            if (File.Exists(templateFile) && replace == false)
            {
                return;
            }

            var type = db.DataType.FromName(table_name);
            if (type == null)
            {
                throw new InvalidOperationException("Entity:" + table_name + " does not exists, Insert some sample data before running this page.");
            }

            var template = File.ReadAllText(Path.Combine(this.RootPath, "Modules", "AdminSystem", "Views", "_backendtemplate.cshtml"));
            var code = Razor.Parse<ViewModel>(template, new ViewModel()
            {
                DataType = type,
                Layout = layout
            }, null);

            Directory.CreateDirectory(templatePath);
            File.WriteAllText(templateFile, code);
        }

        /// <summary>
        /// Generates the admin view for current site
        /// </summary>
        /// <param name="table_name">The table_name.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <exception cref="System.InvalidOperationException">Entity: + table_name +  does not exists, Insert some sample data before running this page.</exception>
        protected void GenerateAdminView(string table_name, bool replace = false)
        {
            var templatePath = Path.Combine( this.RootPath, "Site", "Views");

            this.GenerateView(this.SiteDatabase, templatePath, table_name, "_admin.cshtml", replace, "admin-" + table_name);
        }

    }
}