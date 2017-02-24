using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    /// <summary>
    /// Represents a Property in Type
    /// </summary>
    public class DataProperty
    {
        /// <summary>
        /// Gets or sets the property type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the name of property.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        public DataProperty()
        {
        }

        public DataProperty( string name, JTokenType type)
        {
            this.Name = name;
            this.Type = DataProperty.GetTypeFromJTokenType(type);
        }


        /// <summary>
        /// Gets the .NET type from JTokenType.
        /// </summary>
        /// <param name="jt">The JTokenType to convert</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException">support for  + jt +  is not implemented.</exception>
        private static string GetTypeFromJTokenType(JTokenType jt)
        {
            switch (jt)
            {
                case JTokenType.TimeSpan:
                case JTokenType.Uri:
                case JTokenType.Boolean:
                case JTokenType.Guid:
                case JTokenType.String:
                    return jt.ToString();
                case JTokenType.Bytes:
                    return "byte[]";
                case JTokenType.Date:
                    return "DateTime";
                case JTokenType.Float:
                    return "double";
                case JTokenType.Integer:
                    return "int";
                case JTokenType.Null:
                    return "String";
                default:
                    throw new NotImplementedException("support for " + jt + " is not implemented.");
            }
        }

    }

}