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
        private Nancy.Response ProcessLogin( dynamic user )
        {
            user.PasswordHash = null;
            user.Id = 0;

            var response = this.LoginWithoutRedirect(Guid.Parse((string)user.Guid), DateTime.Now.AddMinutes(15));

            user.Guid = null;
            response.Cookies.Add(new Nancy.Cookies.NancyCookie("UserInfo", JsonConvert.SerializeObject(user)));

            return response;
        }
        
        public MembershipModule( IRootPathProvider r) : base( r )
        {
            Get["/__membership/login"] = p =>
            {
                return View["membership-login"];
            };

            Post["/__membership/login"] = p =>
            {
                var loginParams = this.Bind<LoginParams>();
                dynamic user = this.SiteDatabase.Query("User",
                                string.Format("(Email eq '{0}') and (PasswordHash eq '{1}')", loginParams.Email, loginParams.Password)).FirstOrDefault();

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
        }
    }


}