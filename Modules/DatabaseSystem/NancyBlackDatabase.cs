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
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Nancy;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{

    public class NancyBlackDatabase : IDisposable
    {

        /// <summary>
        /// Fires when object is being created. (before insert) Parameters of Action are Database, Entity Name and the object that is being inserted.
        /// </summary>
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectCreating = delegate { };

        /// <summary>
        /// Fires when object was created. Parameters of Action are Database, Entity Name and the object that was inserted.
        /// </summary>
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectCreated = delegate { };

        /// <summary>
        /// Fires when object is being updated. (before update) Parameters of Action are Database, Entity Name and the object being updated.
        /// </summary>
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectUpdating = delegate { };

        /// <summary>
        /// Fires when object was updated. Parameters of Action are Database, Entity Name and the object that was updated.
        /// </summary>
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectUpdated = delegate { };

        /// <summary>
        /// Fires when object is being deleted. Parameters of Action are Database, Entity Name and the object being deleted.
        /// </summary>
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectDeleting = delegate { };

        /// <summary>
        /// Fires when object was deleted. Parameters of Action are Database, Entity Name and the object that was deleted.
        /// </summary>
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectDeleted = delegate { };

        /// <summary>
        /// Dictionary of Post Processors to process an object right after it was read from Database
        /// </summary>
        public static Dictionary<Type, Action<NancyBlackDatabase, object>> ObjectPostProcessors = new Dictionary<Type, Action<NancyBlackDatabase, object>>();

        /// <summary>
        /// Root path
        /// </summary>
        public string RootPath { get; private set; }

        /// <summary>
        /// Directory of the database
        /// </summary>
        public string DatabaseDirectory { get; set; }

        /// <summary>
        /// Full Path of Database file
        /// </summary>
        public string DatabaseFileName { get; set; }

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

        /// <summary>
        /// 
        /// </summary>
        public NancyContext CurrentContext { get; private set; }

        /// <summary>
        /// Create new instance of NancyBlack Database - the instance is designed to be used per request
        /// </summary>
        /// <param name="db"></param>
        /// <param name="ctx"></param>
        public NancyBlackDatabase(SQLiteConnection db, NancyContext ctx)
        {
            this.CurrentContext = ctx;

            _db = db;
            _dataType = DataTypeFactory.GetForDatabase(db);

            _db.InstanceCreated += (object obj) => {

                var type = obj.GetType();

                Action<NancyBlackDatabase, dynamic> processor;
                if (NancyBlackDatabase.ObjectPostProcessors.TryGetValue(type, out processor))
                {
                    processor(this, obj);
                }
            };
        }

        /// <summary>
        /// Raw Connection to SQLite
        /// </summary>
        public SQLiteConnection Connection
        {
            get
            {
                return _db;
            }
        }
        
        #region Dynamic Types

        /// <summary>
        /// Queries the specified database
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="odataFilter">The odata filter.</param>
        /// <returns></returns>
        private TableQuery<T> PerformQuery<T>(NameValueCollection odataFilter) where T : class, new()
        {
            var parser = new ParameterParser<T>();
            var modelFilter = parser.Parse(odataFilter);

            var table = _db.Table<T>();

            if (modelFilter.FilterExpression != null)
            {
                table = table.Where(modelFilter.FilterExpression);
            }

            if (modelFilter.SortDescriptions.Count() > 0)
            {
                foreach (var sort in modelFilter.SortDescriptions)
                {
                    var lambda = sort.KeySelector as LambdaExpression;
                    var returnType = lambda.ReturnType;

                    var orderbyName = "OrderBy";
                    if (sort.Direction == SortDirection.Descending)
                    {
                        orderbyName = "OrderByDescending";
                    }

                    var applyResult = typeof(TableQuery<T>).GetMethods().Single(
                                         method => method.Name == orderbyName)
                                         .MakeGenericMethod(returnType)
                                         .Invoke(table, new object[] { lambda });

                    table = (TableQuery<T>)applyResult;

                }
            }

            if (modelFilter.SkipCount > 0)
            {
                table = table.Skip(modelFilter.SkipCount);
            }

            if (modelFilter.TakeCount > 0)
            {
                table = table.Take(modelFilter.TakeCount);
            }

            return table;
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
            return from item in this.Query(entityName, oDatafilter, oDataSort, skip, take).ToList()
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
            return from item in this.Query(entityName, oDatafilter, oDataSort, skip, take).ToList()
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
            return from item in this.Query(entityName, oDatafilter, oDataSort, skip, take).ToList()
                   select (dynamic)JObject.FromObject(item);
        }

        /// <summary>
        /// Queries the database for specified entity type. If type does not exists, the query returns without result.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public int Count(string entityName, string oDatafilter = null, string oDataSort = null, string skip = null, string take = null)
        {
            var type = _dataType.FromName(entityName);
            if (type == null)
            {
                return 0;
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

            dynamic result = method.Invoke(this, new object[] { nv });
            return result.Count();
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
        private bool NeedsToPostProcess(DataType type)
        {
            return type.Properties.Any(p => p.Name.StartsWith("js_"));
        }

        /// <summary>
        /// Post-process the object (deserializes js_ fields)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private JObject PostProcess(DataType type, object input)
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
            var obj = _db.Find(id, _db.GetMapping(type.GetCompiledType()));

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

            if (this.NeedsToPostProcess(type) == true)
            {
                return this.PostProcess(type, obj);
            }
            else
            {
                return JObject.FromObject( obj );
            }
            
        }

        /// <summary>
        /// Update Record FAST, this will directly update the record to database
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="inputObject"></param>
        /// <returns></returns>
        public dynamic UpsertStaticRecord(string entityName, dynamic inputObject)
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

                NancyBlackDatabase.ObjectCreating(this, entityName, inputObject);

                _db.Insert((object)inputObject, actualType);

                NancyBlackDatabase.ObjectCreated(this, entityName, inputObject);
            }
            else
            {
                inputObject.__updatedAt = DateTime.Now;

                NancyBlackDatabase.ObjectUpdating(this, entityName, inputObject);

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
                return JObject.FromObject(this.UpsertStaticRecord(entityName, inputObject));
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
                if (prop.Value.Type == JTokenType.Array || prop.Value.Type == JTokenType.Object)
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

                NancyBlackDatabase.ObjectCreating(this, entityName, toInsert);

                _db.Insert(toInsert, actualType);
                jObject["Id"] = toInsert.Id;

                NancyBlackDatabase.ObjectCreated(this, entityName, toInsert);
            }
            else
            {
                NancyBlackDatabase.ObjectUpdating(this, entityName, jObject);

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

            NancyBlackDatabase.ObjectDeleting(this, entityName, deleting);

            _db.Delete(deleting);

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
        public T GetById<T>(int id) where T : IStaticType, new()
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
        /// Runs the given action in SQLite transaction
        /// </summary>
        /// <param name="action"></param>
        public void Transaction(Action action)
        {
            _db.RunInTransaction(action);
        }

        /// <summary>
        /// Update/Insert the given input object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public T UpsertRecord<T>(T input) where T : IStaticType, new()
        {
            var actualType = input.GetType();
            var entityName = actualType.Name;
            var type = _dataType.FromType(actualType);
            if (type == null)
            {
                throw new ArgumentException("Given entityName does not exists.");
            }

            input.__updatedAt = DateTime.Now;

            if (input.Id == 0)
            {
                input.__createdAt = DateTime.Now;

                NancyBlackDatabase.ObjectCreating(this, entityName, input);

                _db.Insert(input, actualType);

                NancyBlackDatabase.ObjectCreated(this, entityName, input);
            }
            else
            {
                NancyBlackDatabase.ObjectUpdating(this, entityName, input);

                _db.Update(input, actualType);

                NancyBlackDatabase.ObjectUpdated(this, entityName, input);
            }

            return input;
        }

        /// <summary>
        /// Deletes the record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        public void DeleteRecord<T>(T input) where T : IStaticType, new()
        {
            var actualType = input.GetType();
            var entityName = actualType.Name;

            var deleting = this.GetById<T>(input.Id); // get the object out before delete

            NancyBlackDatabase.ObjectDeleting(this, entityName, deleting);

            _db.Delete(deleting);

            NancyBlackDatabase.ObjectDeleted(this, entityName, deleting);
        }

        /// <summary>
        /// Execute the specified sql command and g
        /// </summary>
        /// <param name="commandText"></param>
        public IEnumerable<object> Query(string commandText, object sampleOutput, object[] parameters = null)
        {
            var cmd = _db.CreateCommand(commandText);
            var resultType = sampleOutput.GetType();

            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    cmd.Bind(item);
                }
            }

            var fullName = Encoding.ASCII.GetBytes(resultType.FullName);
            var md5Hash = MD5.Create().ComputeHash(fullName);
            var name = "anonymoustype" + BitConverter.ToString(md5Hash).Replace("-", "");

            // create data type on the fly for query
            // since anonymous type cannot be created
            var dt = DataType.FromJObject(name, JObject.FromObject(sampleOutput));
            var mapping = new TableMapping(dt.GetCompiledType(), _db);

            // dynamically invoke the method on SQLite database to read data with specified command
            var method = typeof(SQLiteCommand)
                .GetMethod("ExecuteDeferredQuery", new Type[] { typeof(TableMapping) })
                .MakeGenericMethod(dt.GetCompiledType());

            var result = (IEnumerable<object>)method.Invoke(cmd, new object[] { mapping });
            return result;
        }

        /// <summary>
        /// Execute the specified sql command and get resulting object based on sample
        /// </summary>
        /// <param name="commandText"></param>
        public IEnumerable<dynamic> QueryAsDynamic(string commandText, object sampleOutput, object[] parameters = null)
        {
            foreach (var item in this.Query( commandText, sampleOutput, parameters ))
            {
                yield return item;
            }
        }

        #endregion

        /// <summary>
        /// Find older version of given object, the latest version is returned first
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public IEnumerable<T> GetOlderVersions<T>(T input) where T : IStaticType
        {
            var typeName = input.GetType().Name;
            var createdAt = input.__updatedAt;
            var id = input.Id;

            var oldVersions = this.Query<RowVersion>().Where(rv => rv.DataType == typeName && rv.RowId == id && rv.__createdAt < createdAt)
                .OrderByDescending(rv => rv.__createdAt);

            foreach (var item in oldVersions)
            {
                if (item.RowData is JObject)
                {
                    yield return ((JObject)item.RowData).ToObject<T>();
                }
                else if ( item.RowData == null)
                {
                    
                }
                else
                {
                    yield return item.RowData;
                }
            }
        }

        /// <summary>
        /// Background compression of input file to output file. Input file will be copied first in main thread to temp file.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        private static void BZip(string inputFile, string outputFile, int level = 1)
        {
            var temp = string.Empty;
            var tempOutputFile = outputFile;

            if (File.Exists("D:\\DATALOSS_WARNING_README.txt")) // Running on Azure
            {
                // copy temp file to local storage
                temp = Path.Combine("D:\\", Path.GetFileName(outputFile)) + ".tmp";
                tempOutputFile = Path.Combine("D:\\", Path.GetFileName(outputFile));
            }
            else
            {
                temp = outputFile + ".tmp";
            }

            File.Copy(inputFile, temp, true);
            File.WriteAllBytes(outputFile, new byte[0]); // touches the output file to prevent further backup

            // runs backup in background
            Task.Run(() =>
            {
                try
                {
                    // not using parallel because it will slow down the entire server
                    using (var fs = File.Open(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var bz = new Ionic.BZip2.ParallelBZip2OutputStream(File.OpenWrite(tempOutputFile), level))
                    {
                        fs.CopyTo(bz);
                    }

                    if (tempOutputFile != outputFile)
                    {
                        File.Copy(tempOutputFile, outputFile, true);
                        File.Delete(tempOutputFile);
                    }
                }
                catch (Exception)
                {
                    File.Delete(outputFile);
                }
                finally
                {
                    File.Delete(temp);
                }
            });

        }
        
        /// <summary>
        /// Gets site database from given Context
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public static NancyBlackDatabase GetSiteDatabase(string rootPath, NancyContext context = null)
        {
            var path = Path.Combine(rootPath, "Site");
            var fileName = Path.Combine(path, "data.sqlite");
            
            var db = new SQLiteConnection(fileName, true);
            var ndb = new NancyBlackDatabase(db, context);
            ndb.DatabaseFileName = fileName;
            ndb.DatabaseDirectory = path;

            return ndb;
        }

        /// <summary>
        /// Get NancyBlackDatabase for given filename
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static NancyBlackDatabase GetDatabase(string fileName)
        {
            var db = new SQLiteConnection(fileName, true);
            return new NancyBlackDatabase(db, null);
        }
        
        /// <summary>
        /// Dispose the underlying database and this instance
        /// </summary>
        public void Dispose()
        {
            _db.Dispose();
        }
        
        /// <summary>
        /// Last Time that the backup was checked
        /// </summary>
        private static DateTime _LastBackupCheck;

        /// <summary>
        /// Performs the regular backup
        /// </summary>
        public void PerformBackup( dynamic backupOptions = null )
        {
            if (DateTime.Now.Subtract(_LastBackupCheck).TotalMinutes > 30)
            {
                _LastBackupCheck = DateTime.Now;
            }
            else
            {
                // No Need to do the backup now
                return;
            }

#if DEBUG
            Debugger.Break();
#endif
            Action cleanUp = null;

            var compression = 1;
            var mode = "hourly";
            var databaseFile = this.DatabaseFileName;

            string backupPath = Path.Combine(this.DatabaseDirectory, "Backups");

            if (backupOptions != null)            
            {
                if (backupOptions.compression != null)
                {
                    compression = (int)backupOptions.compression;
                }

                if (backupOptions.uncpath != null)
                {
                    var existed = DriveInfo.GetDrives().ToDictionary(d => d.Name);
                    var chosen = '\0';
                    for (char i = 'A'; i < 'Z'; i++)
                    {
                        if (existed.ContainsKey(i + @":\") == false)
                        {
                            chosen = i;
                            break;  
                        }
                    }

                    if (chosen != '\0')
                    {
                        string command = string.Empty;

                        if (backupOptions.username != null && backupOptions.password != null)
                        {
                            command = string.Format("use {0}: {1} /u:{2} {3}",
                                chosen,
                                backupOptions.uncpath,
                                backupOptions.username,
                                backupOptions.password);
                        }
                        else
                        {
                            command = string.Format("use {0}: {1}",
                               chosen,
                               backupOptions.uncpath);
                        }

                        var process = Process.Start("net.exe", command);
                        process.WaitForExit();
                        
                        if (process.ExitCode == 0)
                        {
                            cleanUp = () =>
                            {
                                string cmd = string.Format("use {0}: /delete",
                                    chosen);

                                var p2 = Process.Start("net.exe", cmd);
                                p2.WaitForExit();
                            };
                            
                            backupPath = chosen + ":\\";
                        }
                    }
                }

                if (backupOptions.sitename != null)
                {
                    backupPath = Path.Combine(backupPath, (string)backupOptions.sitename);
                }
            }

            try
            {
                if (mode == "hourly")
                {
                    backupPath = Path.Combine(backupPath, "DatabaseBackup-Hourly");
                    Directory.CreateDirectory(backupPath);

                    // create hourly backup
                    var backupFile = Path.Combine(backupPath, string.Format("hourlybackup-{0:HH}.sqlite.bz2", DateTime.Now));
                    if (File.Exists(backupFile) == false)
                    {
                        NancyBlackDatabase.BZip(databaseFile, backupFile);
                    }
                    else
                    {
                        // check modified date
                        if (File.GetLastWriteTime(backupFile).Date < DateTime.Now.Date)
                        {
                            // it was the yesterday's file, replace it
                            NancyBlackDatabase.BZip(databaseFile, backupFile);
                        }
                    }

                }

                if (mode == "daily")
                {

                    backupPath = Path.Combine(backupPath, "DatabaseBackup-Hourly");
                    Directory.CreateDirectory(backupPath);

                    var dailybackupFile = Path.Combine(backupPath, string.Format("dailybackup-{0:dd-MM-yyyy}.sqlite.bz2", DateTime.Now));
                    if (File.Exists(dailybackupFile) == false)
                    {
                        NancyBlackDatabase.BZip(databaseFile, dailybackupFile, 9); // max compression for daily backup
                    }

                    var backupFiles = Directory.GetFiles(backupPath, "dailybackup-*.sqlite.bz2");
                    var now = DateTime.Now;
                    var maxDay = 30;

                    if (backupOptions.dailyretention != null)
                    {
                        maxDay = (int)backupOptions.dailyretention;
                    }

                    foreach (var file in backupFiles)
                    {
                        if (now.Subtract(File.GetCreationTime(file)).TotalDays > maxDay)
                        {
                            try
                            {
                                File.Delete(file); // delete backup older than 30 days
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
                // retry backup again
                _LastBackupCheck = DateTime.MinValue;
            }
            finally
            {
                if (cleanUp != null)
                {
                    cleanUp();
                }
            }
        }
    }

}