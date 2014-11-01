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
        public List<DataProperty> Properties { get; set; }

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

        private static Dictionary<string, DataType> _CachedDataType = new Dictionary<string, DataType>();

        /// <summary>
        /// Generate DataType from json.
        /// </summary>
        /// <param name="inputJson">The input json.</param>
        /// <returns></returns>
        public static DataType FromJson(string typeName, string inputJson)
        {
            var sourceObject = JsonConvert.DeserializeObject(inputJson) as JObject;

            var newType = new DataType();
            newType.Name = typeName;
            newType.Properties = (from KeyValuePair<string, JToken> property in sourceObject
                                 select new DataProperty(property.Key, property.Value.Type) ).ToList();

            var idProperty = ( from p in newType.Properties
                               where p.Name.Equals( "id", StringComparison.InvariantCultureIgnoreCase )
                               select p ).FirstOrDefault();

            if ( idProperty == null)
            {
                // no Id, create one
                newType.Properties.Add( new DataProperty() { Name = "Id", Type = "int" });
            }
            else
            {
                idProperty.Name = "Id"; // The field Id is required
            }

            // lookup from cache
            var cacheKey = JsonConvert.SerializeObject(newType);
            DataType cachedType;
            if (_CachedDataType.TryGetValue( cacheKey, out cachedType))
            {
                return cachedType;
            }

            _CachedDataType.Add(cacheKey, newType);

            return newType;
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
    }

}