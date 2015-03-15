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

        public ContentModule( IRootPathProvider rootPath) : base( rootPath )
        {
            _RootPath = rootPath.GetRootPath();

            Get["/__role/{roleName}/{path*}"] = this.HandleProtectedContentRequest(this.HandleContentRequest);

            Get["/__role/{roleName}"] = this.HandleProtectedContentRequest(this.HandleContentRequest);
            
            Get["/{path*}"] = this.HandleRequest(this.HandleContentRequest);


            Get["/"] = this.HandleRequest( this.HandleContentRequest );
        }

        /// <summary>
        /// Generates the layout page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="content">The content.</param>
        protected void GenerateLayoutPage( dynamic site, dynamic content )
        {
            var theme = (string)site.Theme;
            var layout = (string)content.Layout;

            string layoutPath = Path.Combine(_RootPath, "Sites", (string)site.HostName, "Views", Path.GetDirectoryName(layout));
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

        protected Func<dynamic, dynamic> HandleProtectedContentRequest(Func<dynamic, dynamic> action)
        {
            return (arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 401;
                }

                var role = (string)arg.roleName;
                var path = (string)arg.path;

                if (this.CurrentUser.HasClaim(role) == false)
                {
                    return 403;
                }

                UserManager.Current.EnsureRoleRegistered(this.Context, role);

                return action(new
                {
                    path = "__role/" + role + "/" + path,
                    roleName = role
                });
            };
        }

        protected dynamic HandleContentRequest(dynamic arg)
        {
            var url = (string)arg.path;
            if (url == "/" || url == null)
            {
                url = "/";
            }

            if (url.StartsWith("Admin/", StringComparison.InvariantCultureIgnoreCase))
            {
                // reached this page by error
                return 404;
            }

            if (this.CurrentSite.SiteType == "SuperAdmin")
            {
                // reached this page by error
                return 404;
            }

            dynamic requestedContent = this.SiteDatabase.Query("Content", 
                                    string.Format("Url eq '{0}'", url)).FirstOrDefault();

            if (requestedContent == null)
            {
                if (string.IsNullOrEmpty( Path.GetExtension( url ) ) == false)
                {
                    return 404;
                }

                if (this.CurrentUser.HasClaim("admin") == false)
                {
                    return 404;
                }

                var layout = url == "/home" ? "Home" : "Content";
                if (arg.roleName != null)
                {
                    layout = arg.roleName + "/" + layout;
                }

                requestedContent = this.SiteDatabase.UpsertRecord("Content", new
                                    {
                                        Id = 0,
                                        Url = url,
                                        Layout = layout
                                    });
            }

            this.GenerateLayoutPage(this.CurrentSite, requestedContent);

            return View[(string)requestedContent.Layout, this.GetModel( requestedContent )];
        }

        
    }

}