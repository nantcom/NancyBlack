using Nancy;
using Nancy.Authentication.Forms;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using SisoDb;
using SisoDb.SqlCe4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
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
            get;
            private set;
        }

        /// <summary>
        /// Gets the currently requesting site.
        /// </summary>
        /// <value>
        /// The site.
        /// </value>
        protected dynamic CurrentSite
        {
            get
            {
                return this.Context.Items["CurrentSite"];
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
        /// Gets the shared database.
        /// </summary>
        /// <value>
        /// The shared database.
        /// </value>
        protected NancyBlackDatabase SharedDatabase
        {
            get
            {
                return (NancyBlackDatabase)this.Context.Items["SharedDatabase"];
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
                return (NancyBlackDatabase)this.Context.Items["SiteDatabase"];
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
        public BaseModule(IRootPathProvider rootPath)
        {
            this.RootPath = rootPath.GetRootPath();
        }

        /// <summary>
        /// Gets the site's folder
        /// </summary>
        /// <returns></returns>
        protected string GetSiteFolder()
        {
            return Path.Combine(this.RootPath, "Sites", (string)this.CurrentSite.HostName);
        }

        /// <summary>
        /// Gets the standard model which require for NancyBlackRazorViewBase
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        protected dynamic GetModel(dynamic content = null)
        {
            if (content != null &&
                ((object)content).GetType().Name.Contains("AnonymousType"))
            {
                // anonymous type will have problem in template
                // convert it to JObject
                var json = JsonConvert.SerializeObject(content);
                content = JsonConvert.DeserializeObject(json);
            }

            return new
            {
                Site = this.CurrentSite,
                Database = this.SiteDatabase,
                SharedDatabase = this.SharedDatabase,
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