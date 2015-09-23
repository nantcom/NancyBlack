using Nancy;
using NantCom.NancyBlack.Modules.SitemapSystem.Types;
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

        public static event Action<NancyContext, SiteMap> SiteMapRequested = delegate { };

        private SiteMap GetSiteMap()
        {
            SiteMap sitemap = MemoryCache.Default["SiteMap"] as SiteMap;
            if (sitemap == null)
            {
                sitemap = new SiteMap();
                SiteMapModule.SiteMapRequested(this.Context, sitemap);

                // Cache for a day
                MemoryCache.Default.Add("SiteMap", sitemap, DateTimeOffset.Now.AddDays(1));
            }

            return sitemap;
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
            Get["/__sitemap"] = this.HandleRequest((arg) =>
            {
                var response = new Response();
                var hostName = this.Request.Url.HostName;
                response.Contents = (s) =>
                {
                    var sitemap = this.GetSiteMap();
                    sitemap.WriteIndex(hostName, s);
                };

                return response;
            });
        }
    }
}