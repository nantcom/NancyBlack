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
using System.Collections.Specialized;
using Linq2Rest.Parser;
using System.Linq.Expressions;

using System.Collections.ObjectModel;
using NantCom.NancyBlack.Modules;
using SQLite;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{

    /// <summary>
    /// Represents a Data Type
    /// </summary>
    public class DataType
    {
        private class RawJson : JsonSerializerSettings
        {
            public RawJson()
            {
                this.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None;
            }

            public static readonly RawJson Instance = new RawJson();
        }

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
        [Ignore]
        public string NormalizedName
        {
            get
            {
                return DataTypeFactory.NormalizeTypeName(this.OriginalName);
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
        public List<DataProperty> Properties { get; set; }
        
        /// <summary>
        /// Gets the hash code which can be used to detect whether the properties of this DataType is similar.
        /// </summary>
        /// <value>
        /// The hash code.
        /// </value>
        private int GetPropertiesHashCode()
        {
            return JsonConvert.SerializeObject(this.Properties.OrderBy(p => p.Name), RawJson.Instance).GetHashCode();
        }

        private static string _GeneratorTemplate = @"
using System;

public class @Model.OriginalName
{   
    @foreach( var property in Model.Properties )
    {
        <text>public</text> @property.Type <text> </text> @property.Name <text>{ get; set; }</text>
    }
}
";
        /// <summary>
        /// Combine new fields from other data type
        /// </summary>
        /// <param name="other"></param>
        public virtual void CombineProperties( DataType other )
        {
            var properties = this.Properties.ToList();

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

            foreach (var item in other.Properties)
            {
                addOrReplaceProperties(item.Name, item.Type);
            }

            properties.RemoveAll(p => p.Name == "AttachmentBase64" || p.Name == "AttachmentExtension");

            this.Properties = properties.ToList();
        }

        /// <summary>
        /// Ensures that this data type contains all neccessary properties
        /// </summary>
        public virtual void EnsureHasNeccessaryProperties()
        {
            List<DataProperty> properties;
            if (this.Properties == null)
            {
                properties = new List<DataProperty>();
            }
            else
            {
                properties = this.Properties;
            }

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
            addOrReplaceProperties("__createdAt", "DateTime");
            addOrReplaceProperties("__updatedAt", "DateTime");
            addOrReplaceProperties("__version", "string");

            this.Properties = properties.ToList();
        }
        
        private Assembly _Compiled;

        /// <summary>
        /// Compiles this instance into Assembly
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Compile Error</exception>
        public virtual Assembly GetAssembly()
        {
            if (_Compiled != null)
            {
                return _Compiled;
            }

            this.EnsureHasNeccessaryProperties();

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
        public virtual object GetInstanceOfCompiledType()
        {
            return this.GetAssembly().CreateInstance(this.OriginalName);
        }

        /// <summary>
        /// Gets the compiled Type
        /// </summary>
        /// <returns></returns>
        public virtual Type GetCompiledType()
        {
            return this.GetAssembly().GetType(this.OriginalName);
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

            var sameName = other.NormalizedName == this.NormalizedName;
            var sameProperty = other.GetPropertiesHashCode() == this.GetPropertiesHashCode();

            return sameName && sameProperty;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (this.NormalizedName + this.GetPropertiesHashCode()).GetHashCode();
        }

    }

}