using Nancy;
using Nancy.Bootstrapper;
using NantCom.NancyBlack.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules.BundleModuleResourceSystem
{
    public class BundleModule : BaseModule
    {
        public BundleModule()
        {
            Get["/__bundle/js"] = this.ReturnJs;
            Get["/__bundle.js"] = this.ReturnJs;
            Get["/__bundle/css"] = this.ReturnCss;
            Get["/__bundle.css"] = this.ReturnCss;
        }

        private Stream GetBundle( string[] files )
        {
            var cached = new MemoryStream();
            var sw = new StreamWriter(cached);

            foreach (var item in files)
            {
                var css = File.ReadAllText(item);
                sw.Write(css);
                sw.WriteLine("/*end*/");
            }

            sw.Flush();

            return cached;
        }

        private dynamic ReturnBundle( string key, string type, string[] files )
        {
            var cached = MemoryCache.Default[key] as Stream;
            if (cached == null)
            {
                cached = this.GetBundle(files);
                MemoryCache.Default.Add(key, cached, DateTimeOffset.Now.AddMinutes(10));
            }

            var r = new Response();
            r.ContentType = type;
            r.Contents = (s) =>
            {
                cached.Position = 0;
                cached.CopyTo(s);
            };
            return r;
        }

        private dynamic ReturnCss(dynamic arg)
        {
            return this.ReturnBundle("bundle--css", "text/css", ModuleResource.AllCss);
        }

        private dynamic ReturnJs(dynamic arg)
        {
            return this.ReturnBundle("bundle--js", "text/javascript", ModuleResource.AllJS);
        }
    }
}