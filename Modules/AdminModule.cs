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

        public AdminModule(IRootPathProvider rootPath)
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

                return DataType.Scaffold(json);

            });

            Post["/tables/DataType"] = this.HandleRequest((arg) =>
            {
                var dataType = this.Bind<DataType>();
                return DataType.Register(dataType);

            });

            Patch["/tables/DataType/{item_id}"] = this.HandleRequest((arg) =>
            {
                var id = arg.item_id == null ? 0 : (int?)arg.item_id;
                if (id == 0)
                {
                    throw new InvalidOperationException("Id supplied is not valid");
                }

                var dataType = this.Bind<DataType>();
                return DataType.Register(dataType);
            });
            
            Get["/tables/DataType"] = this.HandleRequest( (arg) =>
            {
                return DataType.RegisteredTypes;
            });

            Delete["/tables/DataType/{item_id}"] = this.HandleRequest( (arg)=>{

                var id = arg.item_id == null ? 0 : (int?)arg.item_id;
                if (id == 0)
                {
                    throw new InvalidOperationException("Id supplied is not valid");
                }

                DataType.RemoveType(id.Value);

                return 204;

            });

            #endregion
        }

        private dynamic HandleTableRequests(dynamic arg)
        {
            var table_name = (string)arg.table_name;
            var replace = this.Context.Request.Query.regenerate == "true";

            var type = DataType.FromName(table_name);
            if (type == null)
            {
                return 404;
            }

            this.GenerateAdminView(type.OriginalName, replace);

            return View[(string)arg.table_name];
        }

        private void GenerateAdminView( string table_name, bool replace = false )
        {
            var templateFile = Path.Combine(_RootPath, "CustomContent", "Admin", table_name + ".cshtml");

            if (File.Exists( templateFile ) && replace == false)
            {
                return;
            }

            // we have to create empty one to allow query to be run
            // there is no information in SiSoDB about existing Structure?
            var type = DataType.FromName(table_name);
            if (type == null)
            {
                throw new InvalidOperationException("Entity:" + table_name + " does not exists, Insert some sample data before running this page." );
            }

            var template = File.ReadAllText(Path.Combine(_RootPath, "Content", "Views", "Admin", "_backendtemplate.cshtml"));
            var code = Razor.Parse(template, type, null);

            File.WriteAllText(templateFile, code);

        }
    }
}