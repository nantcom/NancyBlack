using Nancy;
using Nancy.Security;
using NantCom.NancyBlack.Modules.MembershipSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.AdminSystem
{
    public class AdminEnrollModule : BaseModule
    {
        private static string _FailSafeCode;


        public AdminEnrollModule()
        {    
            // Generate fail-safe key for enroll anyone to be admin
            // to any site
            if (_FailSafeCode == null)
            {
                _FailSafeCode = Guid.NewGuid().ToString();

                File.WriteAllText(Path.Combine(this.RootPath, "Modules", "AdminSystem", "failsafe.key"),
                    _FailSafeCode);
            }

            this.RequiresAuthentication();

            // Fail-Safe mechanism to grant access to admin system in case
            // of password forget
            Post["/Admin/__enroll"] = _ =>
            {
                var code = (string)this.Request.Form.code;

                if (code == _FailSafeCode)
                {
                    var user = this.Context.CurrentUser as NancyBlackUser;
                    UserManager.Current.EnrollUser(user.Guid, this.Context, code, true);
                }

                return this.Response.AsRedirect("/Admin");

            };


        }
    }
}