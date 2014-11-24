using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SisoDb;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Types
{
    /// <summary>
    /// Factory which handle the creation and registration of DataType for Database
    /// </summary>
    public class DataTypeFactory
    {
        private ISisoDatabase _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeFactory"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        public DataTypeFactory(ISisoDatabase db)
        {
            _db = db;
        }

        private Dictionary<string, DataType> _CachedDataType;

        /// <summary>
        /// Gets the data types, as recorded in database
        /// </summary>
        /// <value>
        /// The data types.
        /// </value>
        private Dictionary<string, DataType> Types
        {
            get
            {
                if (_CachedDataType == null)
                {
                    var types = _db.UseOnceTo().Query<DataType>().ToList();
                    _CachedDataType = types.ToDictionary(t => t.Name);
                }

                return _CachedDataType;
            }
        }

        /// <summary>
        /// Gets all types.
        /// </summary>
        /// <value>
        /// All types.
        /// </value>
        public IEnumerable<DataType> RegisteredTypes
        {
            get
            {
                return this.Types.Values;
            }
        }

        /// <summary>
        /// Removes the type from database
        /// </summary>
        /// <param name="type">The type.</param>
        public void RemoveType(int id)
        {
            var type = (from t in this.RegisteredTypes
                        where t.Id == id
                        select t).FirstOrDefault();

            if (type == null)
            {
                throw new InvalidOperationException("Specified Id does not represents a valid type");
            }

            _db.DropStructureSet(type.GetCompiledType());
            _db.UseOnceTo().DeleteById<DataType>(type.Id);

            type.Id = 0;

            _CachedDataType = null;
        }

        /// <summary>
        /// Registers the specified type.
        /// </summary>
        /// <param name="toRegister">To register.</param>
        /// <returns></returns>
        public DataType Register(DataType toRegister)
        {
            if (toRegister.Id != default(int))
            {
                _db.UseOnceTo().Update(toRegister);
            }
            else
            {
                _db.UseOnceTo().Insert(toRegister);
            }

            _CachedDataType = null;

            return toRegister;
        }

        /// <summary>
        /// Froms the name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public DataType FromName(string typeName, bool generateEmpty = false)
        {
            DataType dt;
            if (this.Types.TryGetValue(typeName.Trim().ToLowerInvariant(), out dt))
            {
                return dt;
            }

            if (generateEmpty)
            {
                return this.FromJson(typeName, "{}");
            }

            return null;
        }

        /// <summary>
        /// Scaffolds the specified input json into DataType
        /// </summary>
        /// <param name="inputJson">The input json.</param>
        /// <returns></returns>
        public DataType Scaffold(string inputJson)
        {
            var sourceObject = JsonConvert.DeserializeObject(inputJson) as JObject;

            var clientDataType = new DataType();
            clientDataType.OriginalName = "Scaffoled";

            var properties = (from KeyValuePair<string, JToken> property in sourceObject
                              select new DataProperty(property.Key, property.Value.Type)).ToList();

            Action<string, string> addOrReplaceProperties = (name, type) =>
            {
                var prop = (from p in properties
                            where p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                            select p).FirstOrDefault();

                if (prop == null)
                {
                    properties.Add(new DataProperty() { Name = name, Type = type });
                }
                else
                {
                    prop.Name = name;
                    prop.Type = type;
                }
            };

            addOrReplaceProperties("Id", "int");
            addOrReplaceProperties("AttachmentUrl", "string");
            addOrReplaceProperties("__createdAt", "DateTime");
            addOrReplaceProperties("__updatedAt", "DateTime");
            addOrReplaceProperties("__version", "string");

            // find AttachmentBase64 field and remove it
            // we will not keep this field in database, but will be kept in 
            // attachment folder
            properties.RemoveAll(p => p.Name == "AttachmentBase64" || p.Name == "AttachmentExtension");

            clientDataType.Properties = new ReadOnlyCollection<DataProperty>(properties);

            return clientDataType;
        }

        /// <summary>
        /// Generate DataType from json.
        /// </summary>
        /// <param name="inputJson">The input json.</param>
        /// <returns></returns>
        public DataType FromJson(string typeName, string inputJson)
        {
            var clientDataType = this.Scaffold(inputJson);
            clientDataType.OriginalName = typeName.Trim().ToLowerInvariant();

            // type with same name must exists only once
            var existingDataType = this.FromName(clientDataType.Name);
            if (existingDataType != null)
            {
                // if structure does not change, use it

                if (existingDataType.Equals(clientDataType))
                {
                    return existingDataType;
                }
                else
                {
                    clientDataType.Id = existingDataType.Id;
                }
            }

            // if changed - set to new client's data type
            // we will always update our type to match client's
            this.Types[clientDataType.Name] = clientDataType;

            if (clientDataType.Id != default(int))
            {
                _db.UseOnceTo().Update(clientDataType);
            }
            else
            {
                _db.UseOnceTo().Insert(clientDataType);
            }

            return clientDataType;
        }

        /// <summary>
        /// Create Instance of DataType froms the json stream.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="inputJsonStream">The input json stream.</param>
        /// <returns></returns>
        public DataType FromJsonStream(string typeName, Stream inputJsonStream)
        {
            var streamReader = new StreamReader(inputJsonStream);
            var json = streamReader.ReadToEnd();

            if (inputJsonStream.CanSeek)
            {
                inputJsonStream.Position = 0;
            }

            return this.FromJson(typeName, json);

        }

        /// <summary>
        /// Gets a DataType Factory for given database.
        /// Database with same connection string will share the instance of DataTypeFactory
        /// </summary>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        public static DataTypeFactory GetForDatabase(ISisoDatabase db)
        {
            var key = "DataTypeFactory-" +
                      db.ConnectionInfo.ClientConnectionString +
                      db.ConnectionInfo.ServerConnectionString;

            var cached = MemoryCache.Default.Get(key);
            if (cached != null)
            {
                return (DataTypeFactory)cached;
            }

            var created = new DataTypeFactory(db);
            MemoryCache.Default.Add(key, created, new CacheItemPolicy()
            {
                SlidingExpiration = TimeSpan.FromMinutes(30)
            });

            return created;
        }

    }
}