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
        /// Not permitted
        /// </summary>
        public override void CombineProperties(DataType other)
        {
            return;
        }

        /// <summary>
        /// Not permitted
        /// </summary>
        public override void EnsureHasNeccessaryProperties()
        {
            return;
        }

        /// <summary>
        /// Gets assembly that defines the type
        /// </summary>
        /// <returns></returns>
        public override Assembly GetAssembly()
        {
            return _InnerType.Assembly;
        }

        /// <summary>
        /// Gets the static type
        /// </summary>
        /// <returns></returns>
        public override Type GetCompiledType()
        {
            return _InnerType;
        }

        /// <summary>
        /// Create new instance of this data type
        /// </summary>
        /// <returns></returns>
        public override object GetInstanceOfCompiledType()
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

            if (typeof(IStaticType) == type)
            {
                throw new InvalidOperationException("Type must implement IStaticType and Not IStaticType itself");
            }

            _InnerType = type;

            this.Id = int.MaxValue;
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
                        where IStaticType.IsAssignableFrom(t) && IStaticType != t && t.IsClass
                        select new StaticDataType(t);
        }
    }
}