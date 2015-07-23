using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.EditorSystem
{
    public class EditorModule : BaseModule
    {
        public EditorModule()
        {
            this.RequiresAuthentication();

            Post["/__editor/enable"] = _ =>
            {
                var redirect = this.Request.Query.returnUrl;
                if (redirect == null)
                {
                    redirect = "/";
                }

                if (this.CurrentUser.HasClaim("editor") == false)
                {
                    return 403;
                }

                return this.Response
                    .AsRedirect((string)redirect)
                    .WithCookie("editmode", "enabled");

            };

            Post["/__editor/disable"] = _ =>
            {
                var redirect = this.Request.Query.returnUrl;
                if (redirect == null)
                {
                    redirect = "/";
                }

                if (this.CurrentUser.HasClaim("editor") == false)
                {
                    return 403;
                }

                return this.Response
                    .AsRedirect((string)redirect)
                    .WithCookie("editmode", "disabled");

            };

            Get["/__editor"] = this.HandleRequest((arg) =>
            {
                return View["editor-editframe"];
            });

            Get["/__editor/data/availablelayouts"] = this.HandleRequest((args) =>
            {
                dynamic site = this.Context.Items["CurrentSite"];
                var viewPath = Path.Combine(this.RootPath, "Site", "Views");
                var views = Directory.GetFiles(viewPath, "*.cshtml", SearchOption.AllDirectories);

                var userViews = from view in views
                       let viewName = view.Replace(viewPath + "\\", "").Replace("\\", "/").Replace(".cshtml", "")
                       where 
                            viewName.StartsWith("Admin/") == false &&
                            viewName.StartsWith("_") == false &&
                            viewName.StartsWith("admin-") == false
                        select viewName;

                return userViews.Distinct();

            });
            
        }
    }
}