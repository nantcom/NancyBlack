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
        private Nancy.Response ProcessLogin(dynamic user)
        {
            user.PasswordHash = null;
            user.Id = 0;

            var response = this.LoginWithoutRedirect(Guid.Parse((string)user.Guid), DateTime.Now.AddMinutes(15));

            user.Guid = null;
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
                dynamic user = this.SiteDatabase.Query("User",
                                string.Format("Email eq '{0}' and PasswordHash eq '{1}'", loginParams.Email, loginParams.Password)).FirstOrDefault();

                if (user == null)
                {
                    return 403;
                }

                return this.ProcessLogin(user);
            };

            Post["/__membership/register"] = p =>
            {
                var registerParams = this.Bind<LoginParams>();

                dynamic user = this.SiteDatabase.Query("User",
                                string.Format("(Email eq '{0}')", registerParams.Email)).FirstOrDefault();

                if (user != null)
                {
                    return 403;
                }

                user = this.SiteDatabase.UpsertRecord("User", new
                {
                    Id = 0,
                    Guid = Guid.NewGuid(),
                    Email = registerParams.Email,
                    PasswordHash = registerParams.Password
                });

                return this.ProcessLogin(user);
            };

            Get["/__membership/enroll"] = _ =>
            {
                if (this.Context.CurrentUser == null ||
                    this.Context.CurrentUser == NancyBlackUser.Anonymous)
                {
                    return this.Response.AsRedirect("/__membership/login?returnUrl=/__membership/enroll");
                }

                return View["membership-enroll"];
            };

            Post["/__membership/enroll"] = _ =>
            {
                if (this.Context.CurrentUser == null ||
                    this.Context.CurrentUser == NancyBlackUser.Anonymous)
                {
                    return this.Response.AsRedirect("/__membership/login?returnUrl=/__membership/enroll");
                }

                var code = (string)this.Request.Form.code;
                var user = this.Context.CurrentUser as NancyBlackUser;


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