using Linq2Rest;
using Linq2Rest.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SisoDb;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public class NancyBlackDatabase
    {
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectDeleted = delegate { };
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectUpdated = delegate { };
        public static event Action<NancyBlackDatabase, string, dynamic> ObjectCreated = delegate { };      

        private ISisoDatabase _db;
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

        public NancyBlackDatabase(ISisoDatabase db)
        {
            _db = db;
            _dataType = DataTypeFactory.GetForDatabase(db);
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
            var queryable = _db.UseOnceTo().Query<T>();

            var modelFilter = parser.Parse(odataFilter);

            if (odataFilter["$filter"] != null)
            {
                queryable = queryable.Where(modelFilter.FilterExpression);

            }

            var sortExpressions = (from sort in modelFilter.SortDescriptions
                                   where sort != null
                                   select sort.KeySelector).ToArray();

            foreach (var item in sortExpressions)
            {
                //queryable = queryable.OrderBy(item);
            }

            if (modelFilter.SkipCount > 0 && modelFilter.TakeCount == 0)
            {
                queryable = queryable.Page(1, modelFilter.SkipCount);
            }

            if (modelFilter.SkipCount > 0 && modelFilter.TakeCount > 0)
            {
                queryable = queryable.Page(modelFilter.SkipCount / modelFilter.TakeCount, modelFilter.TakeCount);
            }

            return queryable.ToListOfJson();
        }

        /// <summary>
        /// Queries the entity, result is in Json Strings. If data type was not yet registered, the result will be empty list
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="oDatafilter">The o datafilter.</param>
        /// <param name="oDataSort">The o data sort.</param>
        /// <returns></returns>
        public IList<string> QueryAsJsonString(string entityName, string oDatafilter = null, string oDataSort = null)
        {
            var type = _dataType.FromName(entityName);
            if (type == null)
            {
                return new List<string>();
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

            return (IList<string>)method.Invoke(this, new object[] { nv });
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
            foreach (var row in this.QueryAsJsonString( entityName, oDatafilter, oDataSort ))
            {
                yield return JObject.Parse(row);
            }
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
            var json = _db.UseOnceTo().GetByIdAsJson(type.GetCompiledType(), id);

            if (json == null)
            {
                return null;
            }
            return JObject.Parse( json );
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

            // using reflection to detect date time properties which will cause trouble when inserting data
            foreach (var prop in actualType.GetProperties())
            {
                if (prop.PropertyType == typeof(DateTime) ) 
                {
                    if (prop.GetSetMethod(false) == null)
                    {
                        continue; //Non-public
                    }

                    var value = (DateTime)prop.GetValue((object)coercedObject);
                    if (value == default(DateTime))
                    {
                        // the value was not set, it cannot be saved to database
                        // set to arbitary minimum value
                        prop.SetValue((object)coercedObject, new DateTime( 1900, 1, 1 ));
                    }
                }
            }

            if (coercedObject.__createdAt == DateTime.MinValue)
            {
                coercedObject.__createdAt = DateTime.Now;
            }

            if (id == 0)
            {
                coercedObject.__createdAt = DateTime.Now;
                _db.UseOnceTo().Insert(actualType, (object)coercedObject);
                NancyBlackDatabase.ObjectCreated(this, entityName, coercedObject);
            }
            else
            {
                _db.UseOnceTo().Update(actualType, (object)coercedObject);
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
            _db.UseOnceTo().DeleteById(type.GetCompiledType(), id);

            NancyBlackDatabase.ObjectDeleted(this, entityName, deleting);
        }

    }

}