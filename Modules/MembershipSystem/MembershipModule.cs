﻿using Nancy;
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
using Newtonsoft.Json.Linq;
using NantCom.NancyBlack.Configuration;
using Nancy.Bootstrapper;
using System.Security.Cryptography;
using System.Text;
using RestSharp;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Google.Apis.Auth;
using NantCom.NancyBlack.Site.Module.Types;

namespace NantCom.NancyBlack.Modules.MembershipSystem
{
    public class MembershipModule : NancyBlack.Modules.BaseModule, IPipelineHook
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

            /// <summary>
            /// Password Reset Code
            /// </summary>
            public string Code { get; set; }
        }

        /// <summary>
        /// Gets MD5 Hash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetHash(string input )
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.ASCII.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < hash.Length; i++)
                {
                    sBuilder.Append(hash[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }

        }

        /// <summary>
        /// Hooks the login process
        /// </summary>
        /// <param name="p"></param>
        public void Hook(IPipelines p)
        {
            p.BeforeRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (ctx.CurrentUser == null)
                {
                    if (ctx.Request.Url.HostName == "localhost" || ctx.Request.Url.HostName.Contains("local.") || ctx.Request.Url.Port == 10096)
                    {
                        ctx.CurrentUser = NcbUser.LocalHostAdmin;
                    }
                    else
                    {
                        ctx.CurrentUser = new NcbUser()
                        {
                            Guid = Guid.Parse(ctx.Items[BuiltInCookies.UserId] as string)
                        };
                    }
                }
                else
                {
                    // ensure that we use same guid as currently logged in user
                    ctx.Items[BuiltInCookies.UserId] = (ctx.CurrentUser as NcbUser).Guid.ToString();
                }

                return null;
            });

            BootStrapper.SetCookies += (ctx) =>
            {

                if (ctx.CurrentUser == null || ctx.CurrentUser.UserName == "Anonymous")
                {
                    ctx.Response.WithCookie("_ncbfbuser", "0", DateTime.MinValue);
                }
            };

            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                // user did not log in
                // we try to gather profile from whatever we have in database
                if (ctx.CurrentUser != null && ctx.CurrentUser.UserName == "Anonymous")
                {
                    var userId = (string)ctx.Items[BuiltInCookies.UserId];
                    var cacheKey = "TempUserCache-" + userId;

                    NcbUser user = MemoryCache.Default[cacheKey] as NcbUser;
                    if (user != null)
                    {
                        // still anonymous because did not logged in but we assign profile
                        var ncbUser = (NcbUser)ctx.CurrentUser;
                        ncbUser.Profile = user.Profile;
                        ncbUser.Email = user.Profile.email;
                    }
                    else
                    {
                        var guid = Guid.Parse(userId);
                        var db = ctx.GetSiteDatabase();
                        user = db.Query<NcbUser>().Where(u => u.Guid == guid).FirstOrDefault();

                        if (user == null)
                        {
                            var lead = db.Query<Level51FacebookLead>().Where(u => u.UserGuid == userId).FirstOrDefault();
                            if (lead != null)
                            {
                                ctx.CurrentUser = new NcbUser()
                                {
                                    UserName = "Anonymous",
                                    Guid = guid,
                                    Email = lead.Email,
                                    Profile = JObject.FromObject(new
                                    {
                                        email = lead.Email,
                                        first_name = lead.FirstName,
                                        last_name = lead.LastName
                                    })
                                };
                            }
                            else
                            {
                                ctx.CurrentUser = new NcbUser()
                                {
                                    UserName = "Anonymous",
                                    Guid = guid,
                                    Email = userId + "@level51pc.com",
                                    Profile = JObject.FromObject(new
                                    {
                                        email = userId + "@level51pc.com",
                                        first_name = userId,
                                        last_name = ""
                                    })
                                };
                            }
                        }
                        else
                        {
                            var ncbUser = (NcbUser)ctx.CurrentUser;
                            ncbUser.Profile = user.Profile;
                            ncbUser.Email = user.Profile.email;
                        }

                        MemoryCache.Default.Add(cacheKey, ctx.CurrentUser, DateTimeOffset.Now.AddMinutes(5));
                    }

                }

            });

        }

        /// <summary>
        /// Performs the login sequence
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private Nancy.Response ProcessLogin(NcbUser user)
        {
            user.PasswordHash = null;            

            DateTime nextDay = DateTime.Now.AddDays(+1);
            var response = this.LoginWithoutRedirect(user.Guid, nextDay);
            response.Contents = (s) =>
            {
                var json = JsonConvert.SerializeObject(user);
                StreamWriter sw = new StreamWriter(s);
                sw.Write(json);
                sw.Flush();
            };

            if (user.UserName.StartsWith("fb_"))
            {
                response = response.WithCookie("_ncbfbuser", "1", nextDay);
            }

            var guid = user.Guid.ToString();
            if (this.Context.GetUserId() != guid)
            {
                this.Context.Items["userid"] = user.Guid.ToString();
            }
            
            return response;
        }


        private static string _FailSafeCode;

        /// <summary>
        /// Current Fail Safe code
        /// </summary>
        public static string FailSafeCode
        {
            get
            {
                return _FailSafeCode;
            }
        }

        public MembershipModule()
        {
            this.GenerateFailSafeKey();

            Get["/__membership/login"] = p =>
            {
                return View["membership-login", new StandardModel(this)];
            };

            Get["/__membership/logindialog"] = p =>
            {
                return View["ncb-membership-logindialog", new StandardModel(this)];
            };

            Get["/__membership/resetpassword"] = p =>
            {
                return View["membership-resetpassword", new StandardModel(this)];
            };

            Get["/__membership/logout"] = p =>
            {
                var response = this.LogoutAndRedirect("/");
                response = response.WithCookie("_ncbfbuser", "0", DateTime.Now.AddDays(-10));

                return response;
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

            Post["/__membership/logingoogle"] = this.HandleRequest(p =>
            {
                var input = p.body.Value;
                if (input == null)
                {
                    return 400;
                }

                if (this.Context.Items[BuiltInCookies.UserId] == null)
                {
                    return 403;
                }

                var payload = GoogleJsonWebSignature.ValidateAsync((string)input.token).Result;
                if (payload == null)
                {
                    return 400;
                }

                string inputEmail = (string)input.me.email;
                if (string.IsNullOrEmpty(inputEmail) == false)
                {
                    var existingUser = this.SiteDatabase.Query<NcbUser>().Where(u => u.Email == inputEmail).FirstOrDefault();
                    if (existingUser != null)
                    {
                        return this.ProcessLogin(existingUser);
                    }
                }

                // this is the guid that nancyblack generated to identify session
                var existingGuid = Guid.Parse(this.Context.Items[BuiltInCookies.UserId] as string);

                var userName = "google_" + input.me.id;
                NcbUser user = UserManager.Current.Register(this.SiteDatabase,
                        userName,
                        input.me.email == null ? userName : (string)input.me.email,
                        this.GetHash(userName),
                        false,
                        true,
                        input.me,
                        existingGuid);

                return this.ProcessLogin(user);
            });

            Post["/__membership/loginfacebook"] = this.HandleRequest( p =>
            {
                var input = p.body.Value;
                if (input == null)
                {
                    return 400;
                }
               
                if (this.Context.Items[BuiltInCookies.UserId] == null)
                {
                    return 403;
                }

                if (this.VerifyFacebookToken((string)input.me.id, (string)input.token) == false)
                {
                    return 403;
                }

                // see if this email already used, if already used - login that user
                string inputEmail = input.me.email as string;
                if (string.IsNullOrEmpty(inputEmail) == false)
                {
                    var existingUser = this.SiteDatabase.Query<NcbUser>().Where(u => u.Email == inputEmail).FirstOrDefault();
                    if (existingUser != null)
                    {
                        return this.ProcessLogin(existingUser);
                    }
                }

                // this is the guid that nancyblack generated to identify session
                var existingGuid = Guid.Parse( this.Context.Items[BuiltInCookies.UserId] as string );

                var userName = "fb_" + input.me.id;
                NcbUser user = UserManager.Current.Register(this.SiteDatabase,
                        userName,
                        input.me.email == null ? userName : (string)input.me.email,
                        this.GetHash( userName ),
                        false,
                        true,
                        input.me,
                        existingGuid);

                var chat = this.SiteDatabase.Query<FacebookMessengerSystem.Types.FacebookChatSession>()
                                .Where(s => s.NcbUserId == user.Id)
                                .FirstOrDefault();

                user.Profile.SendContactEvent = false;

                if (chat != null)
                {
                    if (DateTime.Now.Subtract(chat.LastPixelContactEventSent).TotalDays > 7)
                    {
                        chat.LastPixelContactEventSent = DateTime.Now;
                        this.SiteDatabase.UpsertRecord(chat);

                        user.Profile.SendContactEvent = true;
                    }
                }

                return this.ProcessLogin(user);
            });

            Post["/__membership/register"] = p =>
            {
                var existingGuid = Guid.Parse(this.Context.Items[BuiltInCookies.UserId] as string);
                var registerParams = this.Bind<LoginParams>();
                var user = UserManager.Current.Register(this.SiteDatabase, registerParams.Email, registerParams.Email, registerParams.Password, existingGuid: existingGuid);

                return this.ProcessLogin(user);
            };

            Post["/__membership/reset"] = p =>
            {
                var registerParams = this.Bind<LoginParams>();
                var user = UserManager.Current.Reset(this.SiteDatabase, registerParams.Email, registerParams.Password, registerParams.Code);

                return this.ProcessLogin(user);
            };

            Post["/__membership/resetrequest"] = this.HandleRequest(this.HandlePasswordRequest);

            Get["/__membership/myclaims"] = _ =>
            {
                return View["membership-myclaims", new StandardModel(this)];
            };

            Get["/__membership/enroll"] = _ =>
            {
                if (this.Context.CurrentUser == null ||
                    this.Context.CurrentUser.UserName == NcbUser.Anonymous)
                {
                    return this.Response.AsRedirect("/__membership/login?returnUrl=/__membership/enroll");
                }

                return View["membership-enroll", new StandardModel(this)];
            };

            Post["/__membership/enroll"] = this.HandleRequest(this.HandleEnroll);

            Post["/__membership/api/updateprofile"] = this.HandleRequest(this.UpdateProfile);

            Get["/__membership/impersonate/{id:int}"] = this.HandleRequest((arg) =>
            {
                if (this.Request.Query.failsafetoken != null)
                {
                    if (this.Request.Query.failsafetoken != _FailSafeCode)
                    {
                        return 403;
                    }
                }
                else
                {
                    if (this.CurrentUser.HasClaim("admin") == false)
                    {
                        return 403;
                    }
                }

                int id = arg.id;
                var ncbUser = this.SiteDatabase.GetById<NcbUser>(id);
                var user = UserManager.Current.GetUserFromIdentifier(ncbUser.Guid, this.Context);

                return this.ProcessLogin(user as NcbUser);
            });
            
            Get["/__membership/impersonate/{guid}"] = this.HandleRequest((arg) =>
            {
                if (this.Request.Query.failsafetoken != null)
                {
                    if (this.Request.Query.failsafetoken != _FailSafeCode)
                    {
                        return 403;
                    }
                }
                else
                {
                    if (this.CurrentUser.HasClaim("admin") == false)
                    {
                        return 403;
                    }
                }

                string guid = arg.guid;
                var user = UserManager.Current.GetUserFromIdentifier(Guid.Parse(guid), this.Context);

                return this.ProcessLogin(user as NcbUser);
            });

        }

        /// <summary>
        /// Re-authenticate the user using provided user id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private bool VerifyFacebookToken( string userId, string accessToken )
        {
            var req = new RestRequest(string.Format("/{0}?fields=id", userId), Method.GET);
            req.AddParameter("access_token", accessToken);

            var client = new RestClient("https://graph.facebook.com");
            var result = client.Execute(req);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return false;
            }

            dynamic returned = JObject.Parse(result.Content);
            if ((string)returned.id != userId)
            {
                return false;
            }

            return true;
        }

        private dynamic HandlePasswordRequest(dynamic arg)
        {
            var registerParams = (arg.body.Value as JObject).ToObject<LoginParams>();            

            var code = Guid.NewGuid().ToString();
            var user = this.SiteDatabase.Query<NcbUser>()
                            .Where(u => u.Email == registerParams.Email)
                            .FirstOrDefault();

            user.CodeRequestDate = DateTime.Now;
            user.Code = code;
            this.SiteDatabase.UpsertRecord<NcbUser>(user);

            MailSenderModule.SendEmail(registerParams.Email,
                "Password Reset Request from: " + this.Request.Url.HostName,
                string.Format( @"Please click here to reset your password:<br/>
                      <a href=""http://{0}/__membership/resetpassword?code={1}"">http://{0}/__membership/resetpassword?code={1}</a>" + code
                , this.Request.Url.HostName, code));

            return 200;
        }

        /// <summary>
        /// Generate Fail Safe Key
        /// </summary>
        private void GenerateFailSafeKey()
        {
            if (_FailSafeCode == null)
            {
                _FailSafeCode = Guid.NewGuid().ToString();

                File.WriteAllText(Path.Combine(this.RootPath, "App_Data", "failsafe.key"),
                    _FailSafeCode);
            }
        }

        /// <summary>
        /// Process Enroll Request
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic HandleEnroll(dynamic arg)
        {
            if (this.Context.CurrentUser == null ||
                       this.Context.CurrentUser.UserName == NcbUser.Anonymous)
            {
                return 401;
            }

            var code = (string)this.Request.Form.code;
            var user = this.Context.CurrentUser as NcbUser;

            var ok = UserManager.Current.EnrollUser(user.Guid, this.Context, Guid.Parse(code), code == _FailSafeCode);
            if (ok == false)
            {
                return this.Response.AsRedirect("/__membership/enroll?failed=true");
            }
            else
            {
                this.GenerateFailSafeKey(); // reset the code
                return this.Response.AsRedirect("/__membership/enroll?success=true");
            }
        }

        /// <summary>
        /// Updates user profile 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic UpdateProfile(dynamic arg)
        {
            if (this.CurrentUser.IsAnonymous)
            {
                return 400;
            }

            UserManager.Current.UpdateProfile(this.Context, arg.body.Value);

            return 200;
        }

    }
}