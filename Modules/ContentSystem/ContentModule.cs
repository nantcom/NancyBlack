using Nancy;
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
        private string _RootPath;

        public ContentModule(IRootPathProvider rootPath)
            : base(rootPath)
        {
            _RootPath = rootPath.GetRootPath();

            Get["/{path*}"] = this.HandleRequest(this.HandleContentRequest);

            Get["/"] = this.HandleRequest(this.HandleContentRequest);
        }

        /// <summary>
        /// Generates the layout page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="content">The content.</param>
        protected void GenerateLayoutPage(dynamic site, dynamic content)
        {
            var theme = (string)site.Theme;
            var layout = (string)content.Layout;

            string layoutPath = Path.Combine(_RootPath, "Site", "Views", Path.GetDirectoryName(layout));
            string layoutFilename = Path.Combine(layoutPath, Path.GetFileName(layout) + ".cshtml");

            if (File.Exists(layoutFilename))
            {
                return;
            }

            Directory.CreateDirectory(layoutPath);

            var sourceFile = Path.Combine(_RootPath, "Content", "Views", "_base" + Path.GetFileName(layout) + "layout.cshtml");
            if (File.Exists(sourceFile) == false)
            {
                sourceFile = Path.Combine(_RootPath, "Content", "Views", "_basecontentlayout.cshtml");
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

            dynamic requestedContent = this.SiteDatabase.Query("Content",
                                    string.Format("Url eq '{0}'", url)).FirstOrDefault();

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

                // if Site contains layout with the same name as path, use it
                var layout = "Content";
                var layoutFile = Path.Combine(_RootPath, "Site", "Views", url.Replace('/', '\\') + ".cshtml");
                if (File.Exists( layoutFile ))
                {
                    layout = url;
                }

                requestedContent = this.SiteDatabase.UpsertRecord("Content", new
                {
                    Id = 0,
                    Url = url,
                    Layout = layout,
                    RequiredClaims = string.Empty
                });
            }

            if (string.IsNullOrEmpty( (string)requestedContent.RequiredClaims ) == false)
            {
                var required = ((string)requestedContent.RequiredClaims).Split(',');
                var user = this.Context.CurrentUser as NancyBlackUser;
                if ( required.Any( c => user.HasClaim(c) ) == false)
                {
                    // user does not have any required claims
                    if (this.Context.CurrentUser == NancyBlackUser.Anonymous)
                    {
                        return 401;
                    }

                    return 403;
                }
            }

            this.GenerateLayoutPage(this.CurrentSite, requestedContent);

            return View[(string)requestedContent.Layout, this.GetModel(requestedContent)];
        }


    }

}