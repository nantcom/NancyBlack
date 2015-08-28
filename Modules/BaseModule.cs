using Nancy;
using Nancy.Authentication.Forms;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public class StandardModel
    {
        /// <summary>
        /// Site
        /// </summary>
        public dynamic Site { get; set; }

        public NancyBlackDatabase Database { get; set; }
        
        /// <summary>
        /// Viewing Content, Content should have standard IContent Interface
        /// </summary>
        public dynamic Content
        {
            get;
            set;
        }

        /// <summary>
        /// Model
        /// </summary>
        public dynamic Data { get; set; }

        /// <summary>
        /// HTTP Response Code
        /// </summary>
        public int ResponseCode { get; set; }

        /// <summary>
        /// Create new instance of Standard Model
        /// </summary>
        /// <param name="content"></param>
        /// <param name="data"></param>
        public StandardModel( BaseModule module, dynamic content = null, dynamic data = null, int responseCode = 200 )
        {
            if (content == null)
            {
                content = new JObject();
            }

            this.Content = content;
            this.Data = data;
            this.Site = module.CurrentSite;
            this.Database = module.SiteDatabase;
            this.ResponseCode = responseCode;
        }

        public StandardModel(BaseModule module, string title, string metakeywords = null, string metadescription = null, dynamic data = null, int responseCode = 200)
        {
            this.Content = JObject.FromObject(new
            {
                Title = title,
                MetaKeywords = metakeywords,
                MetaDescription = metadescription
            }); ;
            this.Data = data;
            this.Site = module.CurrentSite;
            this.Database = module.SiteDatabase;
            this.ResponseCode = responseCode;
        }

        public StandardModel( int responseCode )
        {
            this.ResponseCode = responseCode;
        }
    }

    public abstract class BaseModule : NancyModule
    {
        /// <summary>
        /// Gets the root path.
        /// </summary>
        /// <value>
        /// The root path.
        /// </value>
        protected string RootPath
        {
            get
            {
                return BootStrapper.RootPath;
            }
        }

        /// <summary>
        /// Gets the current site information
        /// </summary>
        /// <value>
        /// The site.
        /// </value>
        public dynamic CurrentSite
        {
            get
            {
                return AdminModule.ReadSiteSettings();
            }
        }

        /// <summary>
        /// Currently accessing user
        /// </summary>
        public NcbUser CurrentUser
        {
            get
            {
                if (this.Context.CurrentUser == null)
                {
                    return NcbUser.Anonymous;
                }

                return this.Context.CurrentUser as NcbUser;
            }
        }

        /// <summary>
        /// Gets the shared database for currently requesting site
        /// </summary>
        /// <value>
        /// The shared database.
        /// </value>
        public NancyBlackDatabase SiteDatabase
        {
            get
            {
                return NancyBlackDatabase.GetSiteDatabase( this.RootPath );
            }
        }

        /// <summary>
        /// Gets a value indicating whether this user is in edit mode.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is in edit mode; otherwise, <c>false</c>.
        /// </value>
        public bool IsInEditMode
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModule"/> class.
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        public BaseModule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModule"/> class.
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        public BaseModule(IRootPathProvider rootPath)
        {
        }
        
        /// <summary>
        /// Handle request to show a view based on given request
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        protected Func<dynamic, dynamic> HandleViewRequest(string view, Func<dynamic, StandardModel> modelGetter = null)
        {
            return (arg) =>
            {
                StandardModel model = null;
                if (modelGetter != null)
                {
                    model = modelGetter(arg);
                }

                if (model == null)
                {
                    model = new StandardModel(this);
                }

                if (model.ResponseCode != 200)
                {
                    return model.ResponseCode;
                }
                
                return View[view, model];
            };
        }

        private CustomJsonSerializer _Serializer = new CustomJsonSerializer();

        /// <summary>
        /// Handles the request by simply returning the status code
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        protected Func<dynamic, dynamic> HandleStatusCodeRequest(int statusCode)
        {
            return (arg) =>
            {
                return statusCode;
            };
        }

        private string[] _LockdownBypass = new string[]
        {
            "/Admin",
            "/__membership/",
            "/tables/"
        };

        protected virtual dynamic HandleLockdown( dynamic arg )
        {
            if (this.CurrentSite.lockdown.enable == true)
            {
                if (_LockdownBypass.Any( bypass => this.Request.Url.Path.StartsWith( bypass ) ) == true)
                {
                    // bypass
                    return null;
                }

                if (this.CurrentUser.HasClaim("admin") == false)
                {
                    return View["lockdown", new StandardModel(this, "Please be patient.")];
                }
            }

            return null;
        }

        /// <summary>
        /// Handles the request.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected Func<dynamic, dynamic> HandleRequest(Func<dynamic, dynamic> action)
        {
            return (arg) =>
            {
                dynamic lockdown = this.HandleLockdown(arg);
                if ( lockdown != null)
                {
                    return lockdown;
                }

                dynamic result = null;
                try
                {
                    if (this.Request.Headers.ContentType.Contains( "application/json" ))
                    {
                        bool error = false;
                        try
                        {
                            using (var sr = new StreamReader(this.Request.Body))
                            using (var jr = new JsonTextReader(sr))
                            {
                                arg.body = _Serializer.Deserialize(jr);
                            }
                        }
                        catch (Exception)
                        {
                            error = true;
                        }

                        if (error) // cannot read normally
                        {
                            // try reading without type handling
                            this.Request.Body.Position = 0;
                            try
                            {
                                using (var sr = new StreamReader(this.Request.Body))
                                using (var jr = new JsonTextReader(sr))
                                {
                                    var ser = JsonSerializer.Create(new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None });
                                    arg.body = ser.Deserialize(jr);
                                }
                            }
                            catch (Exception)
                            {
                                throw new InvalidOperationException("JSON is malformed.");
                            }
                        }
                    }

                    result = action(arg);
                }
                catch (UnauthorizedAccessException)
                {
                    return 403;
                }
                catch (InvalidOperationException ex)
                {
                    this.Context.Items["Exception"] = ex;

                    return this.Negotiate
                        .WithStatusCode(400)
                        .WithModel(new
                        {
                            Code = 400,
                            ExceptionType = ex.GetType().Name,
                            Message = ex.Message
                        });
                }
                catch (Exception ex)
                {
                    this.Context.Items["Exception"] = ex;

                    return this.Negotiate
                        .WithStatusCode(500)
                        .WithModel(new
                        {
                            Code = 500,
                            ExceptionType = ex.GetType().Name,
                            Message = ex.Message
                        });
                }

                if (result is Nancy.Responses.Negotiation.Negotiator)
                {
                    var negotiator = result as Nancy.Responses.Negotiation.Negotiator;
                    if (negotiator.NegotiationContext.Headers.ContainsKey("Content-Type"))
                    {
                        if (negotiator.NegotiationContext.Headers["Content-Type"] == "application/json")
                        {
                            negotiator.NegotiationContext.Headers["Cache-Control"] = "no-store";
                            negotiator.NegotiationContext.Headers["Expires"]= "Mon, 26 Jul 1997 05:00:00 GMT";
                            negotiator.NegotiationContext.Headers["Vary"]= "*";
                        }
                        
                    }
                }

                return result;
            };
        }

    }
}