using Nancy;
using Nancy.ModelBinding;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using System.IO;

namespace NantCom.NancyBlack.Modules.MembershipSystem
{
    public class MembershipModule : NancyBlack.Modules.BaseModule
    {
        private class LoginParams
        {
            /// <summary>
            /// Email
            /// </summary>
            public string Email { get; set; }

            /// <summary>
            /// User's Password
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Whether to always log-in user
            /// </summary>
            public bool RememberMe { get; set; }
        }

        /// <summary>
        /// Performs the login sequence
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private Nancy.Response ProcessLogin(NcbUser user)
        {
            user.PasswordHash = null;
            user.Id = 0;

            var response = this.LoginWithoutRedirect(user.Guid, DateTime.Now.AddMinutes(15));
            response.Cookies.Add(new Nancy.Cookies.NancyCookie("UserInfo", JsonConvert.SerializeObject(user)));

            return response;
        }


        private static string _FailSafeCode;

        public MembershipModule()
        {
            // Generate fail-safe key for enroll anyone to be admin
            // to any site
            if (_FailSafeCode == null)
            {
                _FailSafeCode = Guid.NewGuid().ToString();

                File.WriteAllText(Path.Combine(this.RootPath, "App_Data", "failsafe.key"),
                    _FailSafeCode);
            }

            Get["/Admin/Membership/Roles"] = this.HandleStaticRequest("membership-roles", null);

            Get["/__membership/login"] = p =>
            {
                return View["membership-login"];
            };

            Get["/__membership/logout"] = p =>
            {
                return this.LogoutAndRedirect("/");
            };

            Post["/__membership/login"] = p =>
            {
                var loginParams = this.Bind<LoginParams>();

                var user = UserManager.Current.GetUserFromLogin(this.SiteDatabase, loginParams.Email, loginParams.Password);
                if (user == null)
                {
                    return 403;
                }

                return this.ProcessLogin(user);
            };

            Post["/__membership/register"] = p =>
            {
                var registerParams = this.Bind<LoginParams>();
                var user = UserManager.Current.Register(this.SiteDatabase, registerParams.Email, registerParams.Password );

                return this.ProcessLogin(user);
            };

            Get["/__membership/enroll"] = _ =>
            {
                if (this.Context.CurrentUser == null ||
                    this.Context.CurrentUser == NcbUser.Anonymous)
                {
                    return this.Response.AsRedirect("/__membership/login?returnUrl=/__membership/enroll");
                }

                return View["membership-enroll"];
            };

            Post["/__membership/enroll"] = _ =>
            {
                if (this.Context.CurrentUser == null ||
                    this.Context.CurrentUser == NcbUser.Anonymous)
                {
                    return this.Response.AsRedirect("/__membership/login?returnUrl=/__membership/enroll");
                }

                var code = (string)this.Request.Form.code;
                var user = this.Context.CurrentUser as NcbUser;


                if (code == _FailSafeCode)
                {
                    UserManager.Current.EnrollUser(user.Guid, this.Context, code, true);
                }
                else
                {
                    var ok = UserManager.Current.EnrollUser(user.Guid, this.Context, code);
                    if (ok == false)
                    {
                        return this.Response.AsRedirect("/__membership/enroll?failed=true");
                    }
                }

                return this.Response.AsRedirect("/__membership/enroll?success=true");
            };

        }
    }


}