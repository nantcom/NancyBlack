using Nancy;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Nancy.ModelBinding;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using System.Dynamic;

namespace NantCom.NancyBlack.Modules
{
    public class SuperAdminModule : AdminModule
    {
        private string _RootPath;

        /// <summary>
        /// Whether domain is allowed to use SuperAdmin
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public bool IsDomainAllowed( string hostname )
        {
            if (hostname == "localhost")
            {
                return true; // always allow localhost
            }

            // superadmin request, and it is a newly installed nancy
            // read allowed superadmin domain
            var allowedDomains = File.ReadAllText(
                Path.Combine( _RootPath, "Modules", "SuperAdminSystem", "alloweddomains.txt"));


            if (allowedDomains.IndexOf(hostname) < 0)
            {
                return false; // forbidden
            }

            return true;
        }

        public SuperAdminModule(IRootPathProvider rootPath)
            : base(rootPath)
        {
            _RootPath = rootPath.GetRootPath();

            this.Before.AddItemToStartOfPipeline((ctx) =>
            {
                var hostname = ctx.Request.Url.HostName.Replace("www.", "");
                if (this.IsDomainAllowed( hostname) == false)
                {
                    return 403;
                }

                return null;

            });

            Get["/SuperAdmin/Tables/{table_name}"] = this.HandleSuperAdminTabaleRequests;

            Get["/SuperAdmin"] = this.HandleSysAdminDashboard();
            Get["/SuperAdmin/"] = this.HandleSysAdminDashboard();

            Get["/SuperAdmin/Tables"] = this.HandleStaticRequest("/Admin/tables", () =>
            {
                return new
                {
                    Table = "DataType",
                    Layout = "_superadmin"
                };

            });

            Get["/system/tables/DataType"] = this.HandleListDataTypeRequest(() => this.SharedDatabase);
            Post["/system/tables/DataType/Scaffold"] = this.HandleScaffoldRequest(() => this.SharedDatabase);
            Post["/system/tables/DataType"] = this.HandleRegisterDataTypeRequest(() => this.SharedDatabase);
            Patch["/system/tables/DataType/{item_id}"] = this.HandleUpdateDataTypeRequest(() => this.SharedDatabase);
            Delete["/system/tables/DataType/{item_id}"] = this.HandleDeleteDataTypeRequest(() => this.SharedDatabase);

        }

        private dynamic HandleSysAdminDashboard()
        {
            return this.HandleStaticRequest("superadmin-dashboard.cshtml", () =>
            {
                // get site that expire this year
                return this.SharedDatabase.Query("Site", "year(ExpiryDate) eq " + DateTime.Now.Year);
            });
        }

        protected dynamic HandleSuperAdminTabaleRequests(dynamic arg)
        {
            var table_name = (string)arg.table_name;
            var replace = this.Context.Request.Query.regenerate == "true";

            var type = this.SharedDatabase.DataType.FromName(table_name);
            if (type == null)
            {
                return 404;
            }

            this.GenerateSuperAdminView(type.OriginalName, replace);

            return View["superadmin-" + arg.table_name, this.GetModel(type)];
        }

        /// <summary>
        /// Generates the admin view for current site
        /// </summary>
        /// <param name="table_name">The table_name.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <exception cref="System.InvalidOperationException">Entity: + table_name +  does not exists, Insert some sample data before running this page.</exception>
        protected void GenerateSuperAdminView(string table_name, bool replace = false)
        {
            var templatePath = Path.Combine(
                                    _RootPath,
                                    "Modules",
                                    "SuperAdminSystem",
                                    "Views");

            this.GenerateView(this.SharedDatabase, templatePath, table_name, "_superadmin.cshtml", replace, "superadmin." + table_name);
        }

    }
}