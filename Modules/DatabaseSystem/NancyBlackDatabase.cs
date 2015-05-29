using Linq2Rest;
using Linq2Rest.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public class NancyBlackDatabase
    {
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectDeleted = delegate { };
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectUpdated = delegate { };
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectCreated = delegate { };      

        private SQLiteConnection _db;
        private DataTypeFactory _dataType;

        /// <summary>
        /// Gets the data types.
        /// </summary>
        /// <value>
        /// The data types.
        /// </value>
        public DataTypeFactory DataType
        {
            get
            {
                return _dataType;
            }
        }

        public NancyBlackDatabase(SQLiteConnection db)
        {
            _db = db;
            _dataType = DataTypeFactory.GetForDatabase(db);
        }

        /// <summary>
        /// Queries the specified database
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="odataFilter">The odata filter.</param>
        /// <returns></returns>
        private IList<object> PerformQuery<T>(NameValueCollection odataFilter) where T : class, new()
        {
            var parser = new ParameterParser<T>();            
            var modelFilter = parser.Parse(odataFilter);

            var result = modelFilter.Filter(_db.Table<T>());

            if (modelFilter.SkipCount > 0 )
            {
                result = result.Skip(modelFilter.SkipCount);
            }

            if (modelFilter.TakeCount > 0)
            {
                result = result.Take(modelFilter.TakeCount);
            }

            return result.ToList();
        }

        /// <summary>
        /// Queries the entity, result is in Json Strings. If data type was not yet registered, the result will be empty list
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public IEnumerable<string> QueryAsJsonString(string entityName, string oDatafilter = null, string oDataSort = null)
        {
            return from item in this.Query(entityName, oDatafilter, oDataSort)
                   select item.ToString();
        }

        /// <summary>
        /// Queries the database for specified entity type. If type does not exists, the query returns without result.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public IEnumerable<JObject> Query(string entityName, string oDatafilter = null, string oDataSort = null)
        {
            var type = _dataType.FromName(entityName);
            if (type == null)
            {
                return new JObject[0];
            }

            NameValueCollection nv = new NameValueCollection();
            if (oDatafilter != null)
            {
                nv.Add("$filter", oDatafilter);
            }

            if (oDataSort != null)
            {
                nv.Add("$orderby", oDataSort);
            }

            var method = typeof(NancyBlackDatabase)
                            .GetMethod("PerformQuery", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(type.GetCompiledType());

            var result = (IList<object>)method.Invoke(this, new object[] { nv });

            return from item in result
                   select JObject.FromObject(item); // this will help with dynamic
        }

        /// <summary>
        /// Gets an object from database given table name and id
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public JObject GetById(string entityName, int id)
        {
            var type = _dataType.FromName(entityName);
            var obj = _db.Find( id, _db.GetMapping( type.GetCompiledType() ));

            if (obj == null)
            {
                return null;
            }
            return JObject.FromObject( obj );
        }

        /// <summary>
        /// Upserts the specified entity name.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="input">Data to be saved, can be anything including anonymous type. But Anonymous Type must include Id parameter</param>
        public dynamic UpsertRecord(string entityName, dynamic inputObject)
        {
            int? id = inputObject.Id == null ? null : new int?(inputObject.Id);

            // there is a possibility of casing error of Id from Angular
            if (id == null)
            {
                id = inputObject.id == null ? null : new int?(inputObject.id);
            }

            var inputJson = JsonConvert.SerializeObject(inputObject);
            var type = _dataType.FromJson(entityName, inputJson);

            var actualType = type.GetCompiledType();

            return this.UpsertRecord(entityName, actualType, id ?? 0, inputJson);
        }

        /// <summary>
        /// Upserts the record.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public dynamic UpsertRecord(string entityName, int id, string inputJson)
        {
            var type = _dataType.FromJson(entityName, inputJson);
            var actualType = type.GetCompiledType();

            return this.UpsertRecord(entityName, actualType, id, inputJson);
        }

        private dynamic UpsertRecord( string entityName, Type actualType, int id, string inputJson )
        {
            // inputObject is now copied into internal object format
            // ('coerced') which contains additional system properties
            dynamic coercedObject = JsonConvert.DeserializeObject(inputJson, actualType);
            coercedObject.__updatedAt = DateTime.Now;
            coercedObject.__version = DateTime.Now.Ticks.ToString();

            if (coercedObject.__createdAt == DateTime.MinValue)
            {
                coercedObject.__createdAt = DateTime.Now;
            }

            if (id == 0)
            {
                coercedObject.__createdAt = DateTime.Now;                
                _db.Insert((object)coercedObject, actualType);
                NancyBlackDatabase.ObjectCreated(this, entityName, coercedObject);
            }
            else
            {
                _db.Update((object)coercedObject, actualType);
                NancyBlackDatabase.ObjectUpdated(this, entityName, coercedObject);
            }

            return coercedObject;
        }

        /// <summary>
        /// Deletes the specified entity name.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="inputObject">The input object.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Entity:  + entityName +  does not exists
        /// or
        /// Id of inputObject is not set or has default value
        /// </exception>
        public void DeleteRecord(string entityName, dynamic inputObject)
        {
            var type = _dataType.FromName(entityName);
            int? id = inputObject.Id == null ? null : inputObject.Id;

            if (type == null)
            {
                throw new InvalidOperationException("Entity: " + entityName + " does not exists");
            }
            if (id == null || id == 0)
            {
                throw new InvalidOperationException("Id of inputObject is not set or has default value");
            }

            var deleting = this.GetById(entityName, id.Value); // get the object out before delete
            _db.Delete( deleting );

            NancyBlackDatabase.ObjectDeleted(this, entityName, deleting);
        }

        /// <summary>
        /// Gets site database from given Context
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public static NancyBlackDatabase GetSiteDatabase( string rootPath )
        {
            var key = "SiteDatabase";
            lock (key)
            {
                var cached = MemoryCache.Default.Get(key) as NancyBlackDatabase;
                if (cached != null)
                {
                    return cached;
                }

                var path = Path.Combine(rootPath, "App_Data");
                Directory.CreateDirectory(path);

                var fileName = Path.Combine(path, "data.sqlite");

                if (File.Exists(path))
                {
                    // create hourly backup
                    var backupFile = Path.Combine(path, string.Format("hourlybackup-{0:HH}.bak.sqlite", DateTime.Now));
                    File.Copy(fileName, backupFile, true);

                    // create daily backup
                    var dailybackupFile = Path.Combine(path, string.Format("dailybackup-{0:dd-MM-yyyy}.bak.sqlite", DateTime.Now));
                    if (File.Exists(backupFile) == false)
                    {
                        File.Copy(fileName, backupFile);
                    }

                    var backupFiles = Directory.GetFiles(path, "dailybackup-*.bak.sqlite");
                    var now = DateTime.Now;
                    foreach (var file in backupFiles)
                    {
                        if (now.Subtract(File.GetCreationTime(file)).TotalDays > 15)
                        {
                            try
                            {
                                File.Delete(file); // delete backup older than 15 days
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                var db = new SQLiteConnection(fileName, true);
                cached = new NancyBlackDatabase(db);

                // cache in memory for 1 hour
                MemoryCache.Default.Add(key, cached, DateTimeOffset.Now.AddHours(1));


                return cached;
            }
        }


    }

}