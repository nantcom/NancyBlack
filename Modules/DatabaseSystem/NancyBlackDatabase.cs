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
            var modelFilter = parser.Parse(odataFilter);

            var queryable = _db.UseOnceTo().Query<T>();

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

        /// <summary>
        /// Queries the entity, result is in Json Strings
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
        /// Upserts the specified entity name.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="input">Data to be saved, can be anything including anonymous type. But Anonymous Type must include Id parameter</param>
        public dynamic UpsertRecord(string entityName, dynamic inputObject)
        {
            int? id = inputObject.Id == null ? null : new int?(0);
            var inputJson = JsonConvert.SerializeObject(inputObject);
            var type = _dataType.FromJson(entityName, inputJson);

            var actualType = type.GetCompiledType();

            // inputObject is now copied into internal object format
            // ('coerced') which contains additional system properties
            dynamic coercedObject = JsonConvert.DeserializeObject(inputJson, actualType);
            coercedObject.__updatedAt = DateTime.Now;
            coercedObject.__version = DateTime.Now.Ticks.ToString();

            if (id == null || id == 0)
            {
                coercedObject.__createdAt = DateTime.Now;
                _db.UseOnceTo().Insert(actualType, (object)coercedObject);
            }
            else
            {
                _db.UseOnceTo().Update(actualType, (object)coercedObject);
            }

            return coercedObject;
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

            // inputObject is now copied into internal object format
            // ('coerced') which contains additional system properties
            dynamic coercedObject = JsonConvert.DeserializeObject(inputJson, actualType);
            coercedObject.__updatedAt = DateTime.Now;
            coercedObject.__version = DateTime.Now.Ticks.ToString();

            if (id == 0)
            {
                coercedObject.__createdAt = DateTime.Now;
                _db.UseOnceTo().Insert(actualType, (object)coercedObject);
            }
            else
            {
                _db.UseOnceTo().Update(actualType, (object)coercedObject);
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

            _db.UseOnceTo().DeleteById(type.GetCompiledType(), id);
        }

    }

}