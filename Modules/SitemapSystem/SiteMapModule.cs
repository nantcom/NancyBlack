using Nancy;
using NantCom.NancyBlack.Modules.SitemapSystem.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Xml;

namespace NantCom.NancyBlack.Modules.SitemapSystem
{
    public class SiteMapModule : BaseModule
    {
        /// <summary>
        /// Event for other modules to attach when site map is requested
        /// </summary>
        public static event Action<NancyContext, SiteMap> SiteMapRequested = delegate { };

        private SiteMap GetSiteMap(bool rebuild = false)
        {
            var cacheKey = "Sitemap-Cached";
            Action<SiteMap> cacheSitemap = (sm) =>
            {
                CacheItemPolicy p = new CacheItemPolicy();
                p.AbsoluteExpiration = DateTimeOffset.Now.AddHours(1);
                MemoryCache.Default.Add(cacheKey, sm, p);
            };

            lock ("SiteMap") // ensures only one thread can build sitemap
            {
                // try from memory (1 hour cache)
                {
                    SiteMap sitemap = MemoryCache.Default[cacheKey] as SiteMap;

                    if (sitemap != null)
                    {
                        return sitemap;
                    }
                }
                
                var sitemapPath = Path.Combine(this.RootPath, "Site", "sitemap.json");

                // use cached file if it is less than 1 days old
                if (File.Exists(sitemapPath) &&
                    DateTime.UtcNow.Subtract(File.GetLastWriteTimeUtc(sitemapPath)).TotalDays < 1)
                {
                    using (var sr = File.OpenText(sitemapPath))
                    {
                        JsonTextReader jr = new JsonTextReader(sr);
                        JsonSerializer ser = new JsonSerializer();
                        var sitemap = ser.Deserialize<SiteMap>(jr);

                        cacheSitemap(sitemap);

                        return sitemap;
                    }
                }

                // rebuild and cache it
                {
                    var sitemap = new SiteMap();
                    SiteMapModule.SiteMapRequested(this.Context, sitemap);

                    using (var fs = File.OpenWrite(sitemapPath))
                    using (var sw = new StreamWriter(fs))
                    {
                        JsonTextWriter jw = new JsonTextWriter(sw);
                        JsonSerializer ser = new JsonSerializer();

                        ser.Serialize(jw, sitemap);
                    }

                    cacheSitemap(sitemap);
                    
                    return sitemap;
                }
            }
        }

        public SiteMapModule()
        {
            // The Sitemap
            Get["/__sitemap/{index:int}"] = this.HandleRequest((arg) =>
            {
                var response = new Response();
                response.Contents = (s) =>
                {
                    var sitemap = this.GetSiteMap();
                    sitemap.WriteSitemap(s, (int)arg.index);
                };

                return response;
            });



            // The Index
            Get["/__sitemap"] = this.HandleRequest(this.GetSitemap);

            // The Index
            Get["/__sitemap/{build}"] = this.HandleRequest(this.GetSitemap);
        }

        private dynamic GetSitemap(dynamic arg)
        {
            var response = new Response();
            var hostName = this.Request.Url.HostName;
            response.Contents = (s) =>
            {
                var sitemap = this.GetSiteMap((bool)(arg.build == "build"));
                sitemap.WriteIndex(hostName, s);
            };

            return response;
        }
    }
}