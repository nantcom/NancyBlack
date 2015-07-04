using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    /// <summary>
    /// Factory which handle the creation and registration of DataType for Database
    /// </summary>
    public class DataTypeFactory
    {
        SQLiteConnection _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeFactory"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        public DataTypeFactory(SQLiteConnection db)
        {
            _db = db;
            _db.CreateTable<DataType>();
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
                    var dynamicTypes = _db.Table<DataType>().ToList();
                    var staticTypes = StaticDataType.GetStaticDataTypes();

                    _CachedDataType = dynamicTypes.Concat( staticTypes ).ToDictionary(k => k.NormalizedName);

                    // remaps all table to ensure the database
                    // get updated to latest type that was created on-the-fly
                    foreach (var table in _CachedDataType.Values )
                    {
                        _db.CreateTable(table.GetCompiledType());
                    }
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

            _db.DropTable(type.GetCompiledType());
            _db.Delete(type);

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
            if (toRegister.Id == int.MaxValue)
            {
                throw new InvalidOperationException("Cannot Update StaticType");
            }

            if (toRegister.Id == 0)
            {
                if (this.RegisteredTypes.Where(t => t.NormalizedName == toRegister.NormalizedName).FirstOrDefault() != null)
                {
                    throw new InvalidOperationException("Duplicate Structure Name");
                }

                toRegister.EnsureHasNeccessaryProperties();
                _db.Insert(toRegister);
            }
            else
            {
                toRegister.EnsureHasNeccessaryProperties();
                _db.Update(toRegister);
            }
            
            _CachedDataType = null;

            var finalType = this.RegisteredTypes.Where(t => t.NormalizedName == toRegister.NormalizedName).FirstOrDefault();
            return finalType;
        }
        
        /// <summary>
        /// Get DataType from Name
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
        /// Get DataType from Type
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public DataType FromType(Type t)
        {
            DataType dt;
            if (this.Types.TryGetValue(t.Name.ToLowerInvariant(), out dt))
            {
                return dt;
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

            clientDataType.Properties = (from KeyValuePair<string, JToken> property in sourceObject
                                         select new DataProperty(property.Key, property.Value.Type)).ToList();

            clientDataType.EnsureHasNeccessaryProperties();

            return clientDataType;
        }

        /// <summary>
        /// Scaffolds the specified input JObject
        /// </summary>
        /// <param name="inputJson">The input json.</param>
        /// <returns></returns>
        public DataType Scaffold(JObject sourceObject)
        {
            var clientDataType = new DataType();
            clientDataType.OriginalName = "Scaffoled";

            clientDataType.Properties = (from KeyValuePair<string, JToken> property in sourceObject
                                         select new DataProperty(property.Key, property.Value.Type)).ToList();

            clientDataType.EnsureHasNeccessaryProperties();

            return clientDataType;
        }

        /// <summary>
        /// Finds the matching data type and update
        /// </summary>
        /// <param name="clientDataType"></param>
        /// <returns></returns>
        private DataType FindMatchingDataTypeAndUpdate(DataType clientDataType)
        {
            // type with same name must exists only once
            var existingDataType = this.FromName(clientDataType.NormalizedName);
            if (existingDataType != null)
            {
                // if structure does not change, use it

                if (existingDataType.Equals(clientDataType))
                {
                    return existingDataType;
                }
                else
                {
                    // otherwise - update
                    clientDataType.Id = existingDataType.Id;
                    clientDataType.OriginalName = existingDataType.OriginalName;

                    // when client is sending data, some fields can be omitted by JSON standards
                    // only allow fields to be added automatically but not removed
                    clientDataType.CombineProperties(existingDataType);

                    // since it will be costly operation - try to ensure that client
                    // really have new property before attempting to update
                    if (clientDataType.Equals(existingDataType) == false)
                    {
                        _db.InsertOrReplace(clientDataType); // update our mappings

                        // remaps the table
                        _db.CreateTable(clientDataType.GetCompiledType());

                        // update the cached type
                        this.Types[clientDataType.NormalizedName] = clientDataType;
                    }

                }
            }
            else
            {
                // this is a new data type
                this.Register(clientDataType);
            }

            return clientDataType;
        }

        /// <summary>
        /// Get DataType from give JObject
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public DataType FromJObject(string typeName, JObject input )
        {
            var clientDataType = this.Scaffold(input);
            clientDataType.OriginalName = typeName;

            return this.FindMatchingDataTypeAndUpdate( clientDataType );
        }

        /// <summary>
        /// Generate DataType from json.
        /// </summary>
        /// <param name="inputJson">The input json.</param>
        /// <returns></returns>
        public DataType FromJson(string typeName, string inputJson)
        {
            var clientDataType = this.Scaffold(inputJson);
            clientDataType.OriginalName = typeName;
            
            return this.FindMatchingDataTypeAndUpdate(clientDataType);
        }

        /// <summary>
        /// Gets a DataType Factory for given database.
        /// Database with same connection string will share the instance of DataTypeFactory
        /// </summary>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        public static DataTypeFactory GetForDatabase(SQLiteConnection db)
        {
            var key = "DataTypeFactory-" + db.DatabasePath;

            var cached = MemoryCache.Default.Get(key);
            if (cached != null)
            {
                return (DataTypeFactory)cached;
            }

            var created = new DataTypeFactory(db);
            MemoryCache.Default.Add(key, created, DateTimeOffset.MaxValue );

            return created;
        }

    }
}