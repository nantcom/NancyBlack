using Nancy;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public class ContentModule : BaseModule
    {
        private static string _RootPath;

        public ContentModule()
        {
            Get["/{path*}"] = this.HandleRequest(this.HandleContentRequest);

            Get["/"] = this.HandleRequest(this.HandleContentRequest);

            _RootPath = this.RootPath;
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

            // invalid admin links
            if (url.StartsWith("/Admin", StringComparison.InvariantCultureIgnoreCase))
            {
                return 404;
            }

            dynamic requestedContent = ContentModule.GetContent(this.SiteDatabase, url);

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

                requestedContent = ContentModule.CreateContent(this.SiteDatabase, url);
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

            this.GenerateLayoutPage(this.CurrentSite, requestedContent);

            return View[(string)requestedContent.Layout, this.GetModel(requestedContent)];
        }

        #region All Logic Related to Content

        /// <summary>
        /// Default Content Classs, contains properties that the engine requires
        /// </summary>
        private class DefaultContent
        {
            public int Id { get; set; }

            public string Url { get; set; }

            public string Layout { get; set; }

            public string RequiredClaims { get; set; }

            public int DisplayOrder { get; set; }
        }

        /// <summary>
        /// Get child content of given url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetChildContent(NancyBlackDatabase db, string url)
        {
            return db.QueryAsDynamic("Content", string.Format("startswith(Url, '{0}')", url.ToLowerInvariant()), "DisplayOrder");
        }

        /// <summary>
        /// Get child content of given url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static dynamic GetContent(NancyBlackDatabase db, string url)
        {
            return db.Query("Content", string.Format("Url eq '{0}'", url.ToLowerInvariant())).FirstOrDefault();
        }

        /// <summary>
        /// Creates a content
        /// </summary>
        /// <param name="url"></param>
        /// <param name="layout"></param>
        /// <returns></returns>
        public static dynamic CreateContent(NancyBlackDatabase db, string url, string layout = "", string requiredClaims = "", int displayOrder = 0)
        {
            // try to find matching view that has same name as url
            var layoutFile = Path.Combine(_RootPath, "Site", "Views", url.Replace('/', '\\') + ".cshtml");
            if (File.Exists(layoutFile))
            {
                layout = url;
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

            var createdContent = db.UpsertRecord("Content", new DefaultContent()
            {
                Id = 0,
                Url = url,
                Layout = layout,
                RequiredClaims = requiredClaims,
                DisplayOrder = displayOrder
            });

            return createdContent;
        }
        
        #endregion
    }

}