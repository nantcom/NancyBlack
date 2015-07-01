using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public class StaticDataType : DataType
    {
        private Type _InnerType;

        /// <summary>
        /// Id of this Data Type, always -1
        /// </summary>
        public new int Id
        {
            get
            {
                return -1;
            }
            set
            {

            }
        }
        
        /// <summary>
        /// Not permitted
        /// </summary>
        public new void CombineProperties(DataType other)
        {
            throw new InvalidOperationException("Not Permitted with Static Data Type");
        }

        /// <summary>
        /// Not permitted
        /// </summary>
        public new void EnsureHasNeccessaryProperties()
        {
            throw new InvalidOperationException("Not Permitted with Static Data Type");
        }

        /// <summary>
        /// Gets assembly that defines the type
        /// </summary>
        /// <returns></returns>
        public new Assembly GetAssembly()
        {
            return _InnerType.Assembly;
        }

        /// <summary>
        /// Gets the static type
        /// </summary>
        /// <returns></returns>
        public new Type GetCompiledType()
        {
            return _InnerType;
        }

        /// <summary>
        /// Create new instance of this data type
        /// </summary>
        /// <returns></returns>
        public new object GetInstanceOfCompiledType()
        {
            return Activator.CreateInstance(_InnerType);
        }

        /// <summary>
        /// Create instance of StaticDataType from given type
        /// </summary>
        /// <param name="type"></param>
        public StaticDataType(Type type )
        {
            if (typeof(IStaticType).IsAssignableFrom(type) == false)
            {
                throw new InvalidOperationException("Type must implement IStaticType");
            }

            _InnerType = type;

            this.OriginalName = type.Name;
            this.Properties = (from p in _InnerType.GetProperties()
                               where p.CanRead && p.CanWrite
                               select new DataProperty()
                               {
                                   Name = p.Name,
                                   Type = p.PropertyType.Name
                               }).ToList();
        }

        /// <summary>
        /// Gets static data types defined in the project
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DataType> GetStaticDataTypes()
        {
            var IStaticType = typeof(IStaticType);

            return from t in Assembly.GetExecutingAssembly().GetTypes()
                        where IStaticType.IsAssignableFrom(t) && IStaticType != t
                        select new StaticDataType(t);
        }
    }
}