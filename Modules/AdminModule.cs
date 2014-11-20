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
            var type = DataType.FromName(table_name, generateEmpty: true);

            if (type.Properties.Count() == 1) // Only ID Property
            {
                var sisoOp = DataModule.Current.Database.UseOnceTo();

                // dynamically call the generic method of sisoOp using reflection
                var queryMethod = sisoOp.GetType()
                                    .GetMethod("Query")
                                    .MakeGenericMethod(type.GetCompiledType());

                dynamic queryable = queryMethod.Invoke(sisoOp, new object[0]);
                IList<string> samples = queryable.ToListOfJson();

                // use the last record as sample data to generate structure
                var jsonSample = samples.Last();
                type = DataType.FromJson(table_name, jsonSample);
            }

            var template = File.ReadAllText(Path.Combine(_RootPath, "Content", "Views", "Admin", "basebackend.cshtml"));
            var code = Razor.Parse(template, type, null);

            File.WriteAllText(templateFile, code);

        }
    }
}