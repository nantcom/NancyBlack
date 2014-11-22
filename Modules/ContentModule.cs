using Nancy;
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

        public ContentModule( IRootPathProvider rootPath) : base( rootPath )
        {
            _RootPath = rootPath.GetRootPath();

            Get["/{path*}"] = this.HandleRequest( this.HandleContentRequest );
            Get["/"] = this.HandleRequest( this.HandleContentRequest );
        }

        /// <summary>
        /// Generates the layout page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="content">The content.</param>
        private void GenerateLayoutPage( dynamic site, dynamic content )
        {
            var theme = (string)site.Theme;
            var layout = (string)content.Layout;

            var layoutPath = Path.Combine(_RootPath, "Sites", (string)site.HostName);
            Directory.CreateDirectory(layoutPath);

            var layoutFilename = Path.Combine(layoutPath, layout + ".cshtml");
            if (File.Exists(layoutFilename))
            {
                return;
            }

            var sourceFile = Path.Combine(_RootPath, "Content", "Views", "_basehomelayout.cshtml");
            File.Copy(sourceFile, layoutFilename);
        }

        private dynamic HandleContentRequest(dynamic arg)
        {
            var url = (string)arg.path;
            if (url == "/" || url == null)
            {
                url = "/home";
            }

            if (url.StartsWith("Admin/", StringComparison.InvariantCultureIgnoreCase))
            {
                // reached this page by error
                return 404;
            }


            dynamic requestedContent = this.SiteDatabase.Query("Content", 
                                    string.Format("Url eq '{0}'", url)).FirstOrDefault();

            if (requestedContent == null)
            {
                requestedContent = this.SiteDatabase.UpsertRecord("Content", new
                                    {
                                        Id = 0,
                                        Url = url,
                                        Layout = url == "/home" ? "Home" : "Content"
                                    });
            }

            this.GenerateLayoutPage(this.CurrentSite, requestedContent);

            return View[(string)requestedContent.Layout, new
            {
                Site = this.CurrentSite,
                Database = this.SiteDatabase,
                Content = requestedContent
            }];
        }

        
    }

}