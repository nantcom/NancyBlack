using Nancy;
using Nancy.ModelBinding;
using NantCom.NancyBlack.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using SisoDb.SqlCe4;
using SisoDb;

namespace NantCom.NancyBlack.Modules
{
    public class DataModule : NancyModule
    {
        private string _RootPath;
        private ISisoDatabase _SisoDatabase;
        
        public DataModule( IRootPathProvider rootProvider )
        {
            _RootPath = rootProvider.GetRootPath();
            _SisoDatabase = ("Data Source=" + Path.Combine(_RootPath, "Data.sdf") + ";Persist Security Info=False")
                                .CreateSqlCe4Db()
                                .CreateIfNotExists();

            Post["/data/{entityName}"] = this.SaveData;
        }
        
        private dynamic SaveData(dynamic arg)
        {
            // get entity name from path, note the {entityName} in Path registration on line 28
            var entityName = (string)arg.entityName;

            // get the json from request body
            var streamReader = new StreamReader(this.Request.Body);
            var json = streamReader.ReadToEnd();

            var dataType = DataType.FromJson(entityName, json);
            var actualType = dataType.GetCompiledType();

            var inputObject = JsonConvert.DeserializeObject(json, actualType);
            dynamic dynamicInputObject = inputObject; // so we can easily check Id

            if (dynamicInputObject.Id == 0)
            {
                _SisoDatabase.UseOnceTo().Insert(actualType, inputObject);
            }
            else
            {
                _SisoDatabase.UseOnceTo().Update(actualType, inputObject);
            }

            return this.Negotiate
                .WithContentType("application/json")
                .WithModel( inputObject );
        }

    }
}