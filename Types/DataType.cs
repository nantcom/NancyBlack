using Microsoft.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorEngine;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using SisoDb;
using System.Collections.Specialized;
using Linq2Rest.Parser;
using System.Linq.Expressions;

namespace NantCom.NancyBlack.Types
{

    /// <summary>
    /// Represents a Data Type
    /// </summary>
    public class DataType
    {
        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public IEnumerable<DataProperty> Properties { get; private set; }

        private int _PropertiesHashCode;

        /// <summary>
        /// Gets the hash code which can be used to detect whether the properties of this DataType is similar.
        /// </summary>
        /// <value>
        /// The hash code.
        /// </value>
        public int PropertiesHashCode
        {
            get
            {
                if (_PropertiesHashCode == 0)
                {
                    _PropertiesHashCode = JsonConvert.SerializeObject(this.Properties).GetHashCode();
                }

                return _PropertiesHashCode;
            }
        }

        private static string _GeneratorTemplate = @"

using System;

public class @Model.Name
{   
    @foreach( var property in Model.Properties )
    {
        <text>public</text> @property.Type <text> </text> @property.Name <text>{ get; set; }</text>
    }
}
";

        private Assembly _Compiled;

        /// <summary>
        /// Compiles this instance into Assembly
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Compile Error</exception>
        public Assembly GetAssembly()
        {
            if (_Compiled != null)
            {
                return _Compiled;
            }

            var code = Razor.Parse(_GeneratorTemplate, this);

            var provider = new CSharpCodeProvider();
            var result = provider.CompileAssemblyFromSource(new CompilerParameters(new string[] { "System.dll" })
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                OutputAssembly = Path.Combine(Path.GetTempPath(), Path.GetTempFileName()),
                
            }, code);

            if (result.Errors.HasErrors)
            {
                throw new InvalidOperationException("Compile Error");
            }

            _Compiled = result.CompiledAssembly;

            return _Compiled;
        }

        /// <summary>
        /// Gets the instance of compiled type
        /// </summary>
        /// <returns></returns>
        public object GetInstanceOfCompiledType()
        {
            return this.GetAssembly().CreateInstance(this.Name);
        }

        /// <summary>
        /// Gets the compiled Type
        /// </summary>
        /// <returns></returns>
        public Type GetCompiledType()
        {
            return this.GetAssembly().GetType(this.Name);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            DataType other = obj as DataType;
            if (other == null)
            {
                return false;
            }

            return other.Name.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase) &&
                other.PropertiesHashCode == this.PropertiesHashCode;
        }

        #region Static Factories

        private static Dictionary<string, DataType> _CachedDataType = new Dictionary<string, DataType>();

        /// <summary>
        /// Froms the name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public static DataType FromName( string typeName )
        {
            DataType dt;
            if (_CachedDataType.TryGetValue( typeName.Trim().ToLowerInvariant(), out dt ))
            {
                return dt;
            }

            return null;
        }

        /// <summary>
        /// Generate DataType from json.
        /// </summary>
        /// <param name="inputJson">The input json.</param>
        /// <returns></returns>
        public static DataType FromJson(string typeName, string inputJson)
        {
            var sourceObject = JsonConvert.DeserializeObject(inputJson) as JObject;

            var clientDataType = new DataType();
            clientDataType.Name = typeName.Trim().ToLowerInvariant();
            
            var properties = (from KeyValuePair<string, JToken> property in sourceObject
                                 select new DataProperty(property.Key, property.Value.Type) ).ToList();

            var idProperty = (from p in properties
                               where p.Name.Equals( "id", StringComparison.InvariantCultureIgnoreCase )
                               select p ).FirstOrDefault();

            if ( idProperty == null)
            {
                // no Id, create one
                properties.Add(new DataProperty() { Name = "Id", Type = "int" });
            }
            else
            {
                // has Id but may not be named "Id", set it
                idProperty.Name = "Id"; // The field Id is required
            }

            clientDataType.Properties = properties;

            // type with same name must exists only once
            var existingDataType = DataType.FromName(clientDataType.Name);
            if (existingDataType != null)
            {
                // if structure does not change, use it

                if (existingDataType.Equals( clientDataType ))
                {
                    return existingDataType;
                }
            }

            // if changed - set to new client's data type
            // we will always update our type to match client's

            _CachedDataType[clientDataType.Name] = clientDataType;
            return clientDataType;
        }

        /// <summary>
        /// Create Instance of DataType froms the json stream.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="inputJsonStream">The input json stream.</param>
        /// <returns></returns>
        public static DataType FromJsonStream(string typeName, Stream inputJsonStream)
        {
            var streamReader = new StreamReader(inputJsonStream);
            var json = streamReader.ReadToEnd();

            if (inputJsonStream.CanSeek)
            {
                inputJsonStream.Position = 0;
            }

            return DataType.FromJson(typeName, json);

        }

        #endregion
    }

}