using Nancy;
using NantCom.NancyBlack.Types;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Nancy.ModelBinding;

namespace NantCom.NancyBlack.Modules
{
    public class AdminModule : BaseModule
    {
        private string _RootPath;

        public AdminModule(IRootPathProvider rootPath) : base(rootPath)
        {
            _RootPath = rootPath.GetRootPath();

            // Administration pages for Table
            Get["/Admin/Tables/{table_name}"] = this.HandleTableRequests;

            #region Static Pages

            Get["/Admin"] = this.HandleStaticRequest("/Admin/dashboard", null);
            Get["/Admin/"] = this.HandleStaticRequest("/Admin/dashboard", null);
            Get["/Admin/Core/Tables"] = this.HandleStaticRequest("/Admin/tables", null);

            #endregion

            #region APIs

            Post["/tables/DataType/Scaffold"] = this.HandleRequest((arg) =>
            {
                var streamReader = new StreamReader(this.Request.Body);
                var json = streamReader.ReadToEnd();

                return this.SiteDatabase.DataType.Scaffold(json);

            });

            Post["/tables/DataType"] = this.HandleRequest((arg) =>
            {
                var dataType = this.Bind<DataType>();
                return this.SiteDatabase.DataType.Register(dataType);

            });

            Patch["/tables/DataType/{item_id}"] = this.HandleRequest((arg) =>
            {
                var id = arg.item_id == null ? 0 : (int?)arg.item_id;
                if (id == 0)
                {
                    throw new InvalidOperationException("Id supplied is not valid");
                }

                var dataType = this.Bind<DataType>();
                return this.SiteDatabase.DataType.Register(dataType);
            });
            
            Get["/tables/DataType"] = this.HandleRequest( (arg) =>
            {
                return this.SiteDatabase.DataType.RegisteredTypes;
            });

            Delete["/tables/DataType/{item_id}"] = this.HandleRequest( (arg)=>{

                var id = arg.item_id == null ? 0 : (int?)arg.item_id;
                if (id == 0)
                {
                    throw new InvalidOperationException("Id supplied is not valid");
                }

                this.SiteDatabase.DataType.RemoveType(id.Value);

                return 204;

            });

            #endregion
        }

        private dynamic HandleTableRequests(dynamic arg)
        {
            var table_name = (string)arg.table_name;
            var replace = this.Context.Request.Query.regenerate == "true";

            var type = this.SiteDatabase.DataType.FromName(table_name);
            if (type == null)
            {
                return 404;
            }

            this.GenerateAdminView(type.OriginalName, replace);

            return View["Admin/" + arg.table_name, this.GetModel( type )];
        }

        /// <summary>
        /// Generates the admin view for current site
        /// </summary>
        /// <param name="table_name">The table_name.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <exception cref="System.InvalidOperationException">Entity: + table_name +  does not exists, Insert some sample data before running this page.</exception>
        private void GenerateAdminView( string table_name, bool replace = false )
        {
            var templatePath = Path.Combine(
                                    _RootPath,
                                    "Sites",
                                    (string)this.CurrentSite.HostName,
                                    "Admin");

            var templateFile = Path.Combine(
                                    templatePath,
                                    table_name + ".cshtml");

            if (File.Exists( templateFile ) && replace == false)
            {
                return;
            }

            var type = this.SiteDatabase.DataType.FromName(table_name);
            if (type == null)
            {
                throw new InvalidOperationException("Entity:" + table_name + " does not exists, Insert some sample data before running this page." );
            }

            var template = File.ReadAllText(Path.Combine(_RootPath, "Content", "Views", "Admin", "_backendtemplate.cshtml"));
            var code = Razor.Parse(template, type, null);

            Directory.CreateDirectory(templatePath);
            File.WriteAllText(templateFile, code);

        }
    }
}