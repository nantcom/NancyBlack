using Nancy;
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
        protected string _RootPath;
        private static string _FailSafeCode;


        public AdminEnrollModule( IRootPathProvider r ) : base(r)
        {           
            _RootPath = r.GetRootPath();

            // Generate fail-safe key for enroll anyone to be admin
            // to any site
            if (_FailSafeCode == null)
            {
                _FailSafeCode = Guid.NewGuid().ToString();

                File.WriteAllText(Path.Combine(_RootPath, "Modules", "AdminSystem", "failsafe.key"),
                    _FailSafeCode);
            }

            
            Post["/Admin/__enroll"] = _ =>
            {
                var code = (string)this.Request.Form.code;

                if (code == _FailSafeCode)
                {
                    var user = this.Context.CurrentUser as NancyBlackUser;
                    UserManager.Current.EnrollUser(user.Guid, this.Context, "admin", code);
                }

                return this.Response.AsRedirect("/Admin");

            };
        }
    }
}