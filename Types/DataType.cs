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

using System.Collections.ObjectModel;
using NantCom.NancyBlack.Modules;

namespace NantCom.NancyBlack.Types
{

    /// <summary>
    /// Represents a Data Type
    /// </summary>
    public class DataType
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonIgnore]
        public string Name
        {
            get
            {
                return this.OriginalName.Trim().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets the original name.
        /// </summary>
        /// <value>
        /// The name of the original.
        /// </value>
        public string OriginalName { get; set; }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public ReadOnlyCollection<DataProperty> Properties { get; set; }

        private int _PropertiesHashCode;

        /// <summary>
        /// Gets the hash code which can be used to detect whether the properties of this DataType is similar.
        /// </summary>
        /// <value>
        /// The hash code.
        /// </value>
        [JsonIgnore]
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

            return other.Name.Equals(this.Name, StringComparison.InvariantCulture) &&
                other.PropertiesHashCode == this.PropertiesHashCode;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (this.Name + this.PropertiesHashCode.ToString()).GetHashCode();
        }

        #region Static Factories & Services

        private static Dictionary<string, DataType> _CachedDataType;

        /// <summary>
        /// Gets the data types, as recorded in database
        /// </summary>
        /// <value>
        /// The data types.
        /// </value>
        private static Dictionary<string, DataType> Types
        {
            get
            {
                if (_CachedDataType == null)
                {
                    var types = DataModule.Current.Database.UseOnceTo().Query<DataType>().ToList();
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
        public static IEnumerable<DataType> RegisteredTypes
        {
            get
            {
                return DataType.Types.Values;
            }
        }

        /// <summary>
        /// Removes the type from database
        /// </summary>
        /// <param name="type">The type.</param>
        public static void RemoveType( int id )
        {
            var type = (from t in DataType.RegisteredTypes
                        where t.Id == id
                        select t).FirstOrDefault();

            if (type == null)
            {
                throw new InvalidOperationException("Specified Id does not represents a valid type");
            }

            DataModule.Current.Database.DropStructureSet(type.GetCompiledType());
            DataModule.Current.Database.UseOnceTo().DeleteById<DataType>( type.Id );

            type.Id = 0;

            _CachedDataType = null;
        }

        /// <summary>
        /// Registers the specified type.
        /// </summary>
        /// <param name="toRegister">To register.</param>
        /// <returns></returns>
        public static DataType Register(DataType toRegister)
        {
            if (toRegister.Id != default(int))
            {
                DataModule.Current.Database.UseOnceTo().Update(toRegister);
            }
            else
            {
                DataModule.Current.Database.UseOnceTo().Insert(toRegister);
            }

            _CachedDataType = null;

            return toRegister;
        }

        /// <summary>
        /// Froms the name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public static DataType FromName( string typeName, bool generateEmpty = false)
        {
            DataType dt;
            if (DataType.Types.TryGetValue(typeName.Trim().ToLowerInvariant(), out dt))
            {
                return dt;
            }

            if (generateEmpty)
            {
                return DataType.FromJson(typeName, "{ Id: 0 }");
            }

            return null;
        }

        /// <summary>
        /// Scaffolds the specified input json into DataType
        /// </summary>
        /// <param name="inputJson">The input json.</param>
        /// <returns></returns>
        public static DataType Scaffold( string inputJson )
        {
            var sourceObject = JsonConvert.DeserializeObject(inputJson) as JObject;

            var clientDataType = new DataType();
            clientDataType.OriginalName = "Scaffoled";

            var properties = (from KeyValuePair<string, JToken> property in sourceObject
                              select new DataProperty(property.Key, property.Value.Type)).ToList();

            var idProperty = (from p in properties
                              where p.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase)
                              select p).FirstOrDefault();

            if (idProperty == null)
            {
                // no Id, create one
                properties.Add(new DataProperty() { Name = "Id", Type = "int" });
            }
            else
            {
                // has Id but may not be named "Id", set it
                idProperty.Name = "Id"; // The field Id is required
            }

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
        public static DataType FromJson(string typeName, string inputJson)
        {
            var clientDataType = DataType.Scaffold(inputJson);
            clientDataType.OriginalName = typeName.Trim().ToLowerInvariant();

            // type with same name must exists only once
            var existingDataType = DataType.FromName(clientDataType.Name);
            if (existingDataType != null)
            {
                // if structure does not change, use it

                if (existingDataType.Equals( clientDataType ))
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
            DataType.Types[clientDataType.Name] = clientDataType;

            if (clientDataType.Id != default(int))
            {
                DataModule.Current.Database.UseOnceTo().Update(clientDataType);
            }
            else
            {
                DataModule.Current.Database.UseOnceTo().Insert(clientDataType);
            }
            
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