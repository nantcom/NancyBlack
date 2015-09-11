using Nancy;
using NantCom.NancyBlack.Modules.ContentSystem;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using NantCom.NancyBlack.Modules.SitemapSystem;
using NantCom.NancyBlack.Modules.SitemapSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NantCom.NancyBlack.Modules
{
    public class ContentModule : BaseModule
    {
        /// <summary>
        /// Allow custom mapping of input url into another url
        /// </summary>
        public static event Func<NancyContext, dynamic, string, string> RewriteUrl = (ctx, arg, url)=> url;

        /// <summary>
        /// Allow custom processing of requested page
        /// </summary>
        public static event Action<NancyContext, IContent> ProcessPage = delegate { };

        /// <summary>
        /// Allow mapping from one page to another
        /// </summary>
        public static event Func<NancyContext, IContent, IContent> MapPage = (ctx, content) => content;

        private static string _RootPath;

        public ContentModule()
        {
            Get["/{path*}"] = this.HandleRequest(this.HandleContentRequest);

            Get["/"] = this.HandleRequest(this.HandleContentRequest);

            _RootPath = this.RootPath;

            SiteMapModule.SiteMapRequested += SiteMapModule_SiteMapRequested;
        }

        private void SiteMapModule_SiteMapRequested(NancyContext ctx, SiteMap sitemap)
        {
            var db = ctx.GetSiteDatabase();
            foreach (var item in db.Query<Page>().AsEnumerable())
            {
                sitemap.RegisterUrl( "http://" + ctx.Request.Url.HostName + item.Url, item.__updatedAt);
            }
        }

        /// <summary>
        /// Generates the layout page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="content">The content.</param>
        protected void GenerateLayoutPage(dynamic site, dynamic content)
        {
            var layout = (string)content.Layout;
            
            string layoutPath = Path.Combine(this.RootPath, "Site", "Views", Path.GetDirectoryName(layout));
            string layoutFilename = Path.Combine(layoutPath, Path.GetFileName(layout) + ".cshtml");

            if (File.Exists(layoutFilename))
            {
                return;
            }

            Directory.CreateDirectory(layoutPath);

            var sourceFile = Path.Combine(this.RootPath, "Content", "Views", "_base" + Path.GetFileName(layout) + "layout.cshtml");
            if (File.Exists(sourceFile) == false)
            {
                sourceFile = Path.Combine(this.RootPath, "Content", "Views", "_basecontentlayout.cshtml");
            }
            File.Copy(sourceFile, layoutFilename);
        }
        
        protected dynamic HandleContentRequest(dynamic arg)
        {
            var url = (string)arg.path;
            if (url == null)
            {
                url = "/";
            }

            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }

            url = url.ToLowerInvariant();

            // invalid admin links
            if (url.StartsWith("/admin", StringComparison.InvariantCultureIgnoreCase))
            {
                return 404;
            }

            // invalid system links
            if (url.StartsWith("/_", StringComparison.InvariantCultureIgnoreCase))
            {
                return 404;
            }

            // invalid table get request
            if (url.StartsWith("/tables", StringComparison.InvariantCultureIgnoreCase))
            {
                return 404;
            }

            IContent requestedContent = null;

            //
            url = ContentModule.RewriteUrl(this.Context, arg, url);

            // see if the url is collection request or content request
            var parts = url.Split('/');
            if (parts.Length > 2 && parts[1].EndsWith("s"))
            {
                // seems to be a collection
                var typeName = parts[1].Substring(0, parts[1].Length - 1);
                var datatype = this.SiteDatabase.DataType.FromName(typeName);

                var result = this.SiteDatabase.Query(typeName, string.Format("Url eq '{0}'", url)).FirstOrDefault();
                if (result != null)
                {
                    // convert it to IContent
                    if (result is IContent)
                    {
                        requestedContent = result as IContent;
                        requestedContent = ContentModule.MapPage(this.Context, requestedContent);
                    }
                    else
                    {
                        requestedContent = JObject.FromObject(result).ToObject<Page>();
                        (requestedContent as Page).SetTableName(typeName);
                    }

                }
            }

            // if it is not table, use content table instead
            if (requestedContent == null)
            {
                requestedContent = ContentModule.GetPage(this.SiteDatabase, url);
            }

            if (requestedContent == null)
            {
                // won't generate path which contains extension
                // as user might be requesting file
                if (string.IsNullOrEmpty(Path.GetExtension(url)) == false)
                {
                    return 404;
                }

                // only admin can generate
                if (this.CurrentUser.HasClaim("admin") == false)
                {
                    return 404;
                }

                requestedContent = ContentModule.CreatePage(this.SiteDatabase, url);
            }
            
            if (string.IsNullOrEmpty((string)requestedContent.RequiredClaims) == false)
            {
                var required = ((string)requestedContent.RequiredClaims).Split(',');
                var user = this.Context.CurrentUser as NcbUser;
                if (required.Any(c => user.HasClaim(c)) == false)
                {
                    // user does not have any required claims
                    if (this.Context.CurrentUser == NcbUser.Anonymous)
                    {
                        return 401;
                    }

                    return 403;
                }
            }

            if (string.IsNullOrEmpty( requestedContent.Layout ))
            {
                requestedContent.Layout = "Content";
            }

            this.GenerateLayoutPage(this.CurrentSite, requestedContent);

            ContentModule.ProcessPage(this.Context, requestedContent);

            return View[(string)requestedContent.Layout, new StandardModel( this, requestedContent )];
        }

        #region All Logic Related to Content

        /// <summary>
        /// Get child content of given url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IEnumerable<Page> GetChildPages(NancyBlackDatabase db, string url)
        {
            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }
            url = url.ToLowerInvariant() + "/";

            return db.Query<Page>()
                    .Where(p => p.Url.StartsWith(url))
                    .OrderBy(p => p.DisplayOrder);
        }

        /// <summary>
        /// Get Root Content
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IEnumerable<Page> GetRootPages(NancyBlackDatabase db)
        {
            return db.Query<Page>()
                    .Where(p => p.Url.StartsWith("/") && p.Url.Substring(1).IndexOf('/') < 0)
                    .OrderBy(p => p.DisplayOrder);
        }


        /// <summary>
        /// Get content of given url and optionally creates the content
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Page GetPage(NancyBlackDatabase db, string url, bool create = false)
        {
            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }
            url = url.ToLowerInvariant();

            var page = db.Query<Page>()
                    .Where(p => p.Url == url)
                    .FirstOrDefault();

            if (page == null && create == true)
            {
                page = ContentModule.CreatePage(db, url);
            }

            return page;
        }

        /// <summary>
        /// Creates a new Page
        /// </summary>
        /// <param name="url"></param>
        /// <param name="layout"></param>
        /// <returns></returns>
        public static dynamic CreatePage(NancyBlackDatabase db, string url, string layout = "", string requiredClaims = "", int displayOrder = 0)
        {
            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }

            // try to find matching view that has same name as url
            var layoutFile = Path.Combine(_RootPath, "Site", "Views", url.Substring(1).Replace('/', '\\') + ".cshtml");
            if (File.Exists(layoutFile))
            {
                layout = url.Substring(1);
            }

            if (layout == "")
            {
                layout = "content";
            }

            // if URL is "/" generate home instead
            if (url == "/")
            {
                layout = "home";
            }

            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }

            var createdContent = db.UpsertRecord<Page>( new Page()
            {
                Id = 0,
                Title = Path.GetFileName(url),
                Url = url.ToLowerInvariant(),
                Layout = layout,
                RequiredClaims = requiredClaims,
                DisplayOrder = displayOrder
            });

            return createdContent;
        }
        
        #endregion
        
    }

}