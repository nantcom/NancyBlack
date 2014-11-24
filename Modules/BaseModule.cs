using Nancy;
using NantCom.NancyBlack.Modules.DatabaseSystem;
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

        private NancyBlackDatabase _SharedDatabase;

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
                if (_SharedDatabase == null)
                {
                    var sisodb = ("Data Source=" + Path.Combine(this.RootPath, "Sites", "Shared.sdf") + ";Persist Security Info=False")
                            .CreateSqlCe4Db()
                            .CreateIfNotExists();

                    _SharedDatabase = new NancyBlackDatabase(sisodb);
                }

                return _SharedDatabase;
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
                if (this.Context.Items.ContainsKey( "SiteDatabase" ))
                {
                    return (NancyBlackDatabase)this.Context.Items["SiteDatabase"];
                }

                var key = "SiteDatabse-" + this.CurrentSite.HostName;
                var cached = MemoryCache.Default.Get(key);
                if (cached != null)
                {
                    this.Context.Items["SiteDatabase"] = cached;
                    return cached;
                }

                lock (key)
                {
                    var path = Path.Combine(this.RootPath,
                                "Sites",
                                (string)this.CurrentSite.HostName);
                    Directory.CreateDirectory(path);

                    var fileName = Path.Combine(path, "Data.sdf");
                    var sisodb = ("Data Source=" + fileName + ";Persist Security Info=False")
                                    .CreateSqlCe4Db()
                                    .CreateIfNotExists();

                    cached = new NancyBlackDatabase(sisodb);

                    // cache in memory and in current request
                    MemoryCache.Default.Add(key, cached, DateTimeOffset.MaxValue);
                    this.Context.Items["SiteDatabase"] = cached;
                }

                return cached;
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

            this.Before.AddItemToStartOfPipeline( this.InitializeSiteForRequest );
        }

        /// <summary>
        /// Initializes the site for request.
        /// </summary>
        /// <param name="ctx">The context.</param>
        /// <returns>Always null</returns>
        private Nancy.Response InitializeSiteForRequest( NancyContext ctx)
        {
            var key = "Site-" + this.Request.Url.HostName;
            dynamic site = MemoryCache.Default.Get(key);
            if (site == null)
            {
                // check for existing site
                site = this.SharedDatabase.Query("Site",
                                    string.Format("HostName eq '{0}'", this.Request.Url.HostName)).FirstOrDefault();

                if (site == null)
                {
                    // try to find by Alias
                    site = this.SharedDatabase.Query("Site",
                                   string.Format("contains(Alias,'{0};')", this.Request.Url.HostName)).FirstOrDefault();

                    if (site == null) // insert the site if alias not found
                    {
                        site = this.SharedDatabase.UpsertRecord("Site", new
                        {
                            Id = 0,
                            HostName = this.Request.Url.HostName,
                            Alias = this.Request.Url.HostName + ";",
                            Theme = "Basic",
                            Title = "New NancyBlack Site",
                            RegistrationDate = DateTime.Now,
                            ExpiryDate = DateTime.Now.AddYears(1)
                        });
                    }
                }

                MemoryCache.Default.Add(key, site, new CacheItemPolicy()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            this.Context.Items["CurrentSite"] = site;

            return null;
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

                return result;
            };
        }

    }
}