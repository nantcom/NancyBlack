using Nancy.ViewEngines.Razor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    public class RazorConfig : IRazorConfiguration
    {
        public IEnumerable<string> GetAssemblyNames()
        {
            yield return "Newtonsoft.Json";
        }

        public IEnumerable<string> GetDefaultNamespaces()
        {
            yield return "Newtonsoft.Json";
            yield return "Newtonsoft.Json.Linq";
            yield return "System.Linq";
            yield return "System.Collections.Generic";
        }

        public bool AutoIncludeModelNamespace
        {
            get { return true; }
        }
    }
}