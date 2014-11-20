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
using System.Collections.Specialized;
using Linq2Rest.Parser;
using System.Linq.Expressions;
using System.Reflection;

namespace NantCom.NancyBlack.Modules
{
    public class DataModule : NancyModule
    {
        private string _RootPath;
        private ISisoDatabase _SisoDatabase;

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public ISisoDatabase Database
        {
            get
            {
                return _SisoDatabase;
            }
        }
        
        public DataModule( IRootPathProvider rootProvider )
        {
            DataModule.Current = this;

            _RootPath = rootProvider.GetRootPath();
            _SisoDatabase = ("Data Source=" + Path.Combine(_RootPath, "Data.sdf") + ";Persist Security Info=False")
                                .CreateSqlCe4Db()
                                .CreateIfNotExists();


            // the interface of data mobile is compatible with Azure Mobile Service
            // http://msdn.microsoft.com/en-us/library/azure/jj710104.aspx

            Get["/tables/{table_name}"] = this.QueryRecords;

            Post["/tables/{table_name}"] = this.HandleInsertUpdateRequest;

            Patch["/tables/{table_name}/{item_id}"] = this.HandleInsertUpdateRequest;

            Delete["/tables/{table_name}/{item_id}"] = this.DeleteRecord;
        }

        /// <summary>
        /// Create a function which will Handles the request.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        private dynamic HandleInsertUpdateRequest( dynamic arg )
        {
            var entityName = (string)arg.table_name;
            var id = arg.item_id == null ? 0 : (int?)arg.item_id;

            // get the json from request body
            var streamReader = new StreamReader(this.Request.Body);
            var json = streamReader.ReadToEnd();

            // get the compiled type by extracting interface from json
            var dataType = DataType.FromJson(entityName, json);
            var actualType = dataType.GetCompiledType();

            var inputObject = JsonConvert.DeserializeObject(json, actualType);

            if (id == null || id == 0)
            {
                if (this.Request.Method == "PATCH")
                {
                    return this.Negotiate
                        .WithStatusCode(400)
                        .WithReasonPhrase("Cannot update, Id was not specified.");
                }

                _SisoDatabase.UseOnceTo().Insert(actualType, inputObject);
            }
            else
            {
                _SisoDatabase.UseOnceTo().Update(actualType, inputObject);
            }

            if (json.IndexOf( "AttachmentBase64" ) > 0)
            {
                dynamic inputJsonObject = JsonConvert.DeserializeObject(json);
                dynamic dynamicInputObject = inputObject;

                // this request has file attachment
                var attachmentFolder = Path.Combine( _RootPath, "Content", "Site", "Attachments", entityName);
                Directory.CreateDirectory( attachmentFolder );

                if (inputJsonObject.AttachmentUrl == null)
                {
                    throw new InvalidOperationException("AttachmentUrl property must exists in input object to use Attachment Feature");
                }

                if (string.IsNullOrEmpty((string)inputJsonObject.AttachmentExtension))
                {
                    throw new InvalidOperationException("AttachmentExtension is required to use Attachment Feature. (data will not be saved to database)");
                }


                File.WriteAllBytes(
                    Path.Combine(attachmentFolder, dynamicInputObject.Id.ToString() + "." + (string)inputJsonObject.AttachmentExtension ),
                    Convert.FromBase64String((string)inputJsonObject.AttachmentBase64));

                dynamicInputObject.AttachmentUrl = 
                    "/Content/Site/Attachments/" + entityName + "/" +
                    dynamicInputObject.Id + "." + (string)inputJsonObject.AttachmentExtension;

                _SisoDatabase.UseOnceTo().Update(actualType, inputObject);
            }

            return this.Negotiate
                .WithContentType("application/json")
                .WithModel(inputObject);
        }

        /// <summary>
        /// Queries the specified database.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="odataFilter">The odata filter.</param>
        /// <returns></returns>
        private IList<string> PerformQuery<T>(NameValueCollection odataFilter) where T : class
        {
            var parser = new ParameterParser<T>();
            var modelFilter = parser.Parse(odataFilter);

            var queryable = _SisoDatabase.UseOnceTo().Query<T>();

            if (odataFilter["$filter"] != null)
            {
                queryable = queryable.Where(modelFilter.FilterExpression);

            }

            var sortExpressions = (from sort in modelFilter.SortDescriptions
                                  where sort != null
                                  select sort.KeySelector as Expression<Func<T, object>>).ToArray();

            if (sortExpressions.Length > 0)
            {
                queryable = queryable.OrderBy(sortExpressions);
            }

            if (modelFilter.SkipCount > 0)
            {
                queryable = queryable.Skip(modelFilter.SkipCount);
            }

            if (modelFilter.TakeCount > 0)
            {
                queryable = queryable.Take(modelFilter.SkipCount);
            }

            return queryable.ToListOfJson();
        }
        
        private dynamic QueryRecords( dynamic arg )
        {
            var entityName = (string)arg.table_name;

            // we have to create empty one to allow query to be run
            // there is no information in SiSoDB about existing Structure?
            var type = DataType.FromName(entityName, generateEmpty: true);

            NameValueCollection nv = new NameValueCollection();

            var queries = this.Request.Query as IDictionary<string, dynamic>;
            foreach (var item in queries)
            {
                nv.Add(item.Key, item.Value.ToString());
            }

            var method = typeof(DataModule)
                            .GetMethod("PerformQuery", BindingFlags.NonPublic | BindingFlags.Instance )
                            .MakeGenericMethod( type.GetCompiledType() );

            var rowsAsJson = (IList<string>)method.Invoke(this, new object[] { nv });

            // Write output row as RAW json
            var output = new Response();
            output.ContentType = "application/json";
            output.Contents = (s) =>
            {
                var sw = new StreamWriter(s);
                sw.Write("[");
                foreach (var row in rowsAsJson.Take( rowsAsJson.Count - 1) )
                {
                    sw.WriteLine(row + ",");
                }

                if (rowsAsJson.Count > 0)
                {
                    sw.WriteLine(rowsAsJson.Last());
                }
                sw.Write("]");
                sw.Close();
                sw.Dispose();
            };

            return output;
        }

        private dynamic DeleteRecord(dynamic arg)
        {
            var entityName = (string)arg.table_name;
            var id = arg.item_id == null ? 0 : (int?)arg.item_id;

            // we have to create empty one to allow query to be run
            // there is no information in SiSoDB about existing Structure?
            var type = DataType.FromName(entityName, generateEmpty: true);

            _SisoDatabase.UseOnceTo().DeleteById(type.GetCompiledType(), id);

            return this.Negotiate
                .WithStatusCode(204);
        }

        /// <summary>
        /// Gets the current instance of DataModule.
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        public static DataModule Current
        {
            get;
            private set;
        }
    }
}