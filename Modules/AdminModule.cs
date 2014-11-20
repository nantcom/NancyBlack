using Nancy;
using NantCom.NancyBlack.Types;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public class AdminModule : NancyModule
    {
        private string _RootPath;

        public AdminModule(IRootPathProvider rootPath)
        {
            _RootPath = rootPath.GetRootPath();

            Get["/Admin/{table_name}"] = this.HandleRequest;
        }

        private dynamic HandleRequest(dynamic arg)
        {
            var table_name = (string)arg.table_name;
            var replace = this.Context.Request.Query.regenerate == "true";

            this.GenerateAdminView(table_name, replace);

            return View[(string)arg.table_name];
        }

        private void GenerateAdminView( string table_name, bool replace = false )
        {
            var templateFile = Path.Combine(_RootPath, "Content", "Site", "Admin", table_name + ".cshtml");

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

            var template = File.ReadAllText(Path.Combine(_RootPath, "Content", "Views", "Admin", "basebackend.cshtml"));
            var code = Razor.Parse(template, type, null);

            File.WriteAllText(templateFile, code);

        }
    }
}