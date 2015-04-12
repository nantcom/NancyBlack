using Nancy;
using Nancy.Authentication.Forms;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SisoDb;
using SisoDb.SqlCe4;
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

        private dynamic _Content;

        public dynamic Content
        {
            get
            {
                if (_Content == null)
                {
                    return new JObject();
                }
                return _Content;
            }
            set
            {
                if (value == null)
                {
                    _Content = null;
                    return;
                }

                try
                {
                    _Content = JObject.FromObject(value);
                }
                catch (Exception)
                {
                    _Content = JArray.FromObject(value);
                }
            }
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
        protected dynamic CurrentSite
        {
            get
            {
                return BootStrapper.GetSiteSettings();
            }
        }

        /// <summary>
        /// Currently accessing user
        /// </summary>
        protected NancyBlackUser CurrentUser
        {
            get
            {
                if (this.Context.CurrentUser == null)
                {
                    return NancyBlackUser.Anonymous;
                }

                return this.Context.CurrentUser as NancyBlackUser;
            }
        }

        /// <summary>
        /// Gets the shared database for currently requesting site
        /// </summary>
        /// <value>
        /// The shared database.
        /// </value>
        protected NancyBlackDatabase SiteDatabase
        {
            get
            {
                return BootStrapper.GetSiteDatabase();
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
        /// Gets the standard model which require for NancyBlackRazorViewBase
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        protected StandardModel GetModel(dynamic content = null)
        {
            return new StandardModel()
            {
                Database = this.SiteDatabase,
                Content = content
            };

        }

        /// <summary>
        /// Handles the static request.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        protected Func<dynamic, dynamic> HandleStaticRequest(string view, Func<dynamic> modelGetter)
        {
            return (arg) =>
            {
                dynamic model = null;
                if (modelGetter != null)
                {
                    model = modelGetter();
                }

                return View[view, this.GetModel( model )];
            };
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
                dynamic result = null;
                try
                {
                    if (this.Request.Headers.ContentType.Contains( "application/json" ))
                    {
                        try
                        {
                            using (var sr = new StreamReader(this.Request.Body))
                            {
                                using (var jr = new JsonTextReader(sr))
                                {
                                    arg.body = JsonSerializer.Create().Deserialize(jr);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException("Failed to read JSON from body");
                        }
                    }

                    result = action(arg);
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