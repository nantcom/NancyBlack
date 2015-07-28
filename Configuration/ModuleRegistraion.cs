using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    public static class ModuleResource
    {
        private static string _JsOutput;
        private static string _CssOuput;

        private static string[] _Systems;
        private static string[] _Includes;
        private static string[] _AngularModules;

        /// <summary>
        /// HTML Code for JavaScript Resources
        /// </summary>
        public static string JS
        {
            get
            {
                return _JsOutput;
            }
        }

        /// <summary>
        /// CSS Code for Style Sheet
        /// </summary>
        public static string Css
        {
            get
            {
                return _CssOuput;
            }
        }

        /// <summary>
        /// List of Angular Modules to Load
        /// </summary>
        public static string[] AngularModules
        {
            get
            {
                return _AngularModules;
            }
        }

        /// <summary>
        /// List of systems
        /// </summary>
        public static string[] Systems
        {
            get
            {
                return _Systems;
            }
        }

        /// <summary>
        /// List of _include.cshtml files from systems
        /// </summary>
        public static string[] Includes
        {
            get
            {
                return _Includes;
            }
        }

        /// <summary>
        /// Read all module resources
        /// </summary>
        /// <param name="rootPath"></param>
        public static void ReadSystemsAndResources( string rootPath )
        {
            var modules = Path.Combine(rootPath, "Modules");

            var alljs = from js in Directory.GetFiles(modules, "*.js", SearchOption.AllDirectories)
                        where js.Contains("\\Views\\") == false
                         select js.Replace(rootPath, "").Replace('\\', '/');

            var allCss = from css in Directory.GetFiles(modules, "*.min.css", SearchOption.AllDirectories)
                         where css.Contains("\\Views\\") == false
                         select css.Replace(rootPath, "").Replace('\\', '/');

            _Includes = (from inc in Directory.GetFiles(modules, "_include.cshtml", SearchOption.AllDirectories)
                        select inc.Replace(rootPath, "").Replace('\\', '/').Replace(".cshtml", "")).ToArray();

            _Systems = (from module in Directory.GetDirectories(modules)
                        select Path.GetFileNameWithoutExtension(module)).ToArray();
            
            _JsOutput = string.Join("", from js in alljs
                                        select string.Format("<script src=\"/{0}\"></script>", js));

            _CssOuput = string.Join("", from css in allCss
                                        select string.Format("<link href=\"/{0}\" rel=\"stylesheet\" />", css));

            _AngularModules = (from js in alljs
                               select Path.GetFileNameWithoutExtension(js).ToLowerInvariant()).ToArray();

        }
    }
}