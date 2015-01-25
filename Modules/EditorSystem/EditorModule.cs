using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.EditorSystem
{
    public class EditorModule : BaseModule
    {
        public EditorModule(IRootPathProvider r) : base(r)
        {
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
                    .WithCookie( "editmode", "enabled" );

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
        }
    }
}