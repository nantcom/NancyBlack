using Linq2Rest;
using Linq2Rest.Parser;
using NantCom.NancyBlack.Configuration;
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
using Nancy.Bootstrapper;

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

        #region Dynamic Types

        /// <summary>
        /// Queries the specified database
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="odataFilter">The odata filter.</param>
        /// <returns></returns>
        private IEnumerable<object> PerformQuery<T>(NameValueCollection odataFilter) where T : class, new()
        {
            var parser = new ParameterParser<T>();            
            var modelFilter = parser.Parse(odataFilter);

            return modelFilter.Filter(_db.Table<T>());
        }

        /// <summary>
        /// Queries the entity, result is in Json Strings. If data type was not yet registered, the result will be empty list
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public IEnumerable<string> QueryAsJsonString(string entityName, string oDatafilter = null, string oDataSort = null, string skip = null, string take = null)
        {
            return from item in this.Query(entityName, oDatafilter, oDataSort, skip, take)
                   select JObject.FromObject(item).ToString();
        }
        
        /// <summary>
        /// Queries the entity, result is in JObject to support dynamic operations
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public IEnumerable<JObject> QueryAsJObject(string entityName, string oDatafilter = null, string oDataSort = null, string skip = null, string take = null)
        {
            return from item in this.Query(entityName, oDatafilter, oDataSort, skip, take)
                   select JObject.FromObject(item);
        }

        /// <summary>
        /// Queries the entity, result is converted to JObject and casted to dynamic
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public IEnumerable<dynamic> QueryAsDynamic(string entityName, string oDatafilter = null, string oDataSort = null, string skip = null, string take = null)
        {
            return from item in this.Query(entityName, oDatafilter, oDataSort, skip, take)
                   select (dynamic)JObject.FromObject(item);
        }

        /// <summary>
        /// Queries the database for specified entity type. If type does not exists, the query returns without result.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public IEnumerable<object> Query(string entityName, string oDatafilter = null, string oDataSort = null, string skip = null, string take = null)
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

            if (skip != null)
            {
                nv.Add("$skip", skip);
            }

            if (take != null)
            {
                nv.Add("$top", take);
            }

            var method = typeof(NancyBlackDatabase)
                            .GetMethod("PerformQuery", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(type.GetCompiledType());

            var result = (IEnumerable<object>)method.Invoke(this, new object[] { nv });

            if (this.NeedsToPostProcess(type) == true)
            {
                // this type has some property that is complex type
                // needs special treatment
                return from item in result
                       select this.PostProcess(type, item);
            }
            else
            {
                return result;
            }
        }
        
        /// <summary>
        /// Whether we need to post-process the object
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool NeedsToPostProcess( DataType type )
        {
            return type.Properties.Any( p => p.Name.StartsWith( "js_" ));
        }

        /// <summary>
        /// Post-process the object (deserializes js_ fields)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private JObject PostProcess( DataType type, object input )
        {
            JObject jo = JObject.FromObject(input);

            foreach (var prop in jo.Properties().ToList()) // to-list to allow us to add property
            {
                if (prop.Name.StartsWith("js_"))
                {
                    if (prop.Value.Type == JTokenType.Null)
                    {
                        jo[prop.Name.Substring(3)] = JToken.Parse("null");                    
                    }
                    else
                    {
                        jo[prop.Name.Substring(3)] = JToken.Parse((string)prop.Value);
                    
                    }
                    jo.Remove(prop.Name);
                }
            }

            return jo;
        }

        /// <summary>
        /// Gets an object from database given table name and id
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public object GetById(string entityName, int id)
        {
            var type = _dataType.FromName(entityName);
            var obj = _db.Find( id, _db.GetMapping( type.GetCompiledType() ));

            if (obj == null)
            {
                return null;
            }
            return obj;
        }

        /// <summary>
        /// Gets an object from database given table name and id
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public object GetByIdAsJObject(string entityName, int id)
        {
            var type = _dataType.FromName(entityName);
            var obj = _db.Find(id, _db.GetMapping(type.GetCompiledType()));

            if (obj == null)
            {
                return null;
            }
            return JObject.FromObject(obj);
        }

        /// <summary>
        /// Update Record FAST, this will directly update the record to database
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="inputObject"></param>
        /// <returns></returns>
        public dynamic UpsertStaticRecord( string entityName, dynamic inputObject )
        {
            var type = _dataType.FromName(entityName);
            if (type == null)
            {
                throw new ArgumentException("Given entityName does not exists.");
            }

            var actualType = type.GetCompiledType();
            if (inputObject is JObject)
            {
                inputObject = ((JObject)inputObject).ToObject(actualType);
            }

            if (inputObject.Id == 0)
            {
                inputObject.__createdAt = DateTime.Now;
                inputObject.__updatedAt = DateTime.Now;

                _db.Insert((object)inputObject, actualType);
                NancyBlackDatabase.ObjectCreated(this, entityName, inputObject);
            }
            else
            {
                inputObject.__updatedAt = DateTime.Now;

                _db.Update((object)inputObject, actualType);
                NancyBlackDatabase.ObjectUpdated(this, entityName, inputObject);
            }
            
            return inputObject;
        }
        
        /// <summary>
        /// Upserts the specified entity name.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="input">Data to be saved, can be anything including anonymous type. But Anonymous Type must include Id parameter</param>
        public JObject UpsertRecord(string entityName, object inputObject)
        {
            var type = _dataType.FromName(entityName);
            if (type is StaticDataType)
            {
                return JObject.FromObject( this.UpsertStaticRecord(entityName, inputObject) );
            }

            JObject jObject = inputObject as JObject;
            if (jObject == null)
            {
                jObject = JObject.FromObject(inputObject);
            }

            List<JProperty> removed = new List<JProperty>();

            // converts complex properties to Json
            foreach (var prop in jObject.Properties().ToList()) // to-list to allow us to add property
            {
                if (prop.Value.Type == JTokenType.Array || prop.Value.Type == JTokenType.Object )
                {
                    // array or objects are converted to JSON when stored in table
                    jObject["js_" + prop.Name] = prop.Value.ToString(Formatting.None);

                    prop.Remove();
                    removed.Add(prop);
                }
            }

            type = _dataType.FromJObject(entityName, jObject);
            var actualType = type.GetCompiledType();

            jObject["__updatedAt"] = DateTime.Now;

            int id = 0;
            if (jObject.Property("id") != null) // try to get Id
            {
                id = (int)jObject["id"];

                jObject["Id"] = id;
                jObject.Remove("id");
            }

            if (jObject.Property("Id") != null)
            {
                id = (int)jObject["Id"];
            }

            if (id == 0)
            {
                jObject["__createdAt"] = DateTime.Now;

                // needs to convert to object to get Id later
                dynamic toInsert = jObject.ToObject(actualType);
                _db.Insert(toInsert, actualType);
                
                NancyBlackDatabase.ObjectCreated(this, entityName, toInsert);
            }
            else
            {
                _db.Update(jObject.ToObject(actualType), actualType);
                
                NancyBlackDatabase.ObjectUpdated(this, entityName, jObject);
            }

            // remove "js_" properties
            foreach (var prop in jObject.Properties().ToList()) // to-list to allow us to add/remove property
            {
                if (prop.Name.StartsWith("js_"))
                {
                    prop.Remove();
                }
            }

            // add removed complex properties back
            foreach (var prop in removed)
            {
                jObject.Add(prop.Name, prop.Value);
            }

            return jObject;
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

        #endregion

        #region Static Types

        /// <summary>
        /// Gets Static Type From its table by Id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetById<T>( int id ) where T : IStaticType, new()
        {
            return _db.Get<T>(id);
        }

        /// <summary>
        /// Gets Query Interface for given static type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableQuery<T> Query<T>() where T : IStaticType, new()
        {
            return _db.Table<T>();
        }
        
        /// <summary>
        /// Update/Insert the given input object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public T UpsertRecord<T>( T input ) where T : IStaticType, new()
        {
            var actualType = input.GetType();
            var entityName = actualType.Name;

            input.__updatedAt = DateTime.Now;
            
            if (input.Id == 0)
            {
                input.__createdAt = DateTime.Now;

                 _db.Insert( input, actualType );
                NancyBlackDatabase.ObjectCreated(this, entityName, input);
            }
            else
            {
                _db.Update( input, actualType);
                NancyBlackDatabase.ObjectUpdated(this, entityName, input);
            }

            return input;
        }

        /// <summary>
        /// Deletes the record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        public void DeleteRecord<T>( T input ) where T : IStaticType, new()
        {
            var actualType = input.GetType();
            var entityName = actualType.Name;

            var deleting = this.GetById<T>(input.Id); // get the object out before delete
            _db.Delete(deleting);
            
            NancyBlackDatabase.ObjectDeleted(this, entityName, deleting);
        }

        #endregion

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