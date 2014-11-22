﻿using Microsoft.CSharp;
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

    }

}