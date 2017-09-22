using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.ContentSystem;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using NantCom.NancyBlack.Modules.SitemapSystem;
using NantCom.NancyBlack.Modules.SitemapSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Nancy.Bootstrapper;
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules
{
    public class ContentModule : BaseModule, IPipelineHook
    {
        /// <summary>
        /// Allow custom mapping of input url into another url
        /// </summary>
        public static event Func<NancyContext, dynamic, string, string> RewriteUrl = (ctx, arg, url) => url;

        /// <summary>
        /// Allow custom processing of requested page
        /// </summary>
        public static event Action<NancyContext, IContent> ProcessPage = delegate { };

        /// <summary>
        /// Allow mapping from one page to another
        /// </summary>
        public static event Func<NancyContext, IContent, IContent> MapPage = (ctx, content) => content;

        private static string _RootPath;
        private static DateTime _LastPageViewUpdated;
        private const int PAGEVIEW_DELAYTIME = 5;

        /// <summary>
        /// Perform hook 
        /// </summary>
        /// <param name="p"></param>
        public void Hook(IPipelines p)
        {
        }
        
        public ContentModule()
        {
            Get["/robots.txt"] = this.HandleRequest((arg) =>
            {
                return "User-agent: *\nDisallow: ";
            });

            Get["/{path*}"] = this.HandleRequest(this.HandleContentRequest);

            Get["/"] = this.HandleRequest(this.HandleContentRequest);

            Get["/__content/updatepageview"] = this.HandleRequest(this.UpdatePageViewSummary);

            Get["/__content/pageviewcount/{table}/{id}"] = this.HandleRequest(this.GetPageViewCount);
            
            _RootPath = this.RootPath;

            SiteMapModule.SiteMapRequested += SiteMapModule_SiteMapRequested;
            NancyBlackDatabase.ObjectCreated += InsertTag_ObjectCreate;
            NancyBlackDatabase.ObjectUpdated += UpdateTag_ObjectUpdate;

        }

        /// <summary>
        /// Get Page View Count
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic GetPageViewCount(dynamic arg)
        {
            var table = (string)arg.table;
            var id = (int)arg.id;

            // invalid data type, also  prevent SQL Injection attack when we replace string
            if ( this.SiteDatabase.DataType.FromName( table ) == null )
            {
                return 400;
            }

            dynamic cached = MemoryCache.Default.Get(table + id);
            if (cached != null)
            {
                return (string)cached;
            }
            
            dynamic result = this.SiteDatabase.Query
                            (string.Format("SELECT COUNT(Id) as Hit FROM PageView WHERE ContentId = {0} AND TableName = '{1}'", id, table),
                            new
                            {
                                Hit = 0
                            }).FirstOrDefault();

            if (result == null)
            {
                return "0";
            }
            
            MemoryCache.Default.Add(table + id, result.Hit.ToString("0,0"), DateTimeOffset.Now.AddMinutes(10));
            return result.Hit.ToString("0,0");
        }

        private dynamic UpdatePageViewSummary(dynamic arg)
        {
            //lastPageViewId have to be saved on setting (but we will re-count everytime for now)
            var lastPageViewId = 0;
            var results = this.SiteDatabase.Query
                (string.Format("SELECT TableName, ContentId, COUNT(Id) as Hit, Request FROM PageView WHERE Id > {0} GROUP BY TableName, ContentId", lastPageViewId),
                new
                {
                    TableName = "test",
                    ContentId = 0,
                    Hit = 0,
                    Request = ""
                }).ToList();
            
            foreach (dynamic pageView in results)
            {
                string tableName = pageView.TableName;
                int contentId = pageView.ContentId;
                var summary = this.SiteDatabase.Query<PageViewSummary>()
                    .Where(rec => rec.TableName == tableName && rec.ContentId == contentId).FirstOrDefault();

                if (summary == null)
                {
                    var request = JObject.Parse(pageView.Request);
                    summary = new PageViewSummary()
                    {
                        ContentId = contentId,
                        TableName = tableName,
                        PageViews = pageView.Hit,
                        Url = Uri.UnescapeDataString( request.Value<string>("Path") )
                    };
                }
                else
                {
                    summary.PageViews += pageView.Hit;
                }

                this.SiteDatabase.UpsertRecord(summary);
            }

            return "OK";
        }

#region Update Tag Table

        private void UpdateTag_ObjectUpdate(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (!(obj is IContent))
            {
                return;
            }

            IContent content = obj;
            var type = content.GetType().Name;
            var existTags = db.Query<Tag>().Where(tag => tag.ContentId == content.Id && tag.Type == type);

            if (content.Tags != null)
            {
                var source = string.Join(",", content.Tags.Split(',').Distinct().OrderBy(s => s)).ToLowerInvariant();
                var inDb = string.Join(",", existTags.ToList().Select(tag => tag.Name).OrderBy(s => s)).ToLowerInvariant();

                // return when there is nothing change in tags
                if (source == inDb)
                {
                    return;
                }
            }

            db.Transaction(() =>
            {
                // delete exist tags for re-add new tags
                foreach (var tag in existTags)
                {
                    db.DeleteRecord(tag);
                }

                // re-insert tag
                this.InsertTag(db, content);
            });
        }

        private string GetCategoryPath(IContent content)
        {
            var index = content.Url.LastIndexOf('/');
            if (index == -1)
            {
                return string.Empty;
            }

            return content.Url.Substring(0, index);
        }

        private void InsertTag(NancyBlackDatabase db, IContent content)
        {
            if (string.IsNullOrEmpty(content.Tags))
            {
                return;
            }

            var tags = content.Tags.Split(',').Distinct();
            foreach (var tag in tags)
            {
                db.UpsertRecord<Tag>(
                    new Tag()
                    {
                        Name = tag,
                        Type = content.GetType().Name,
                        ContentId = content.Id,
                        Url = GetCategoryPath(content)
                    });
            }
        }

        private void InsertTag_ObjectCreate(NancyBlackDatabase db, string table, dynamic obj)
        {
            if (!(obj is IContent))
            {
                return;
            }

            IContent content = obj;
            this.InsertTag(db, content);
        }

#endregion

        private void SiteMapModule_SiteMapRequested(NancyContext ctx, SiteMap sitemap)
        {
            var db = ctx.GetSiteDatabase();
            foreach (var item in db.Query<Page>().AsEnumerable())
            {
                sitemap.RegisterUrl("http://" + ctx.Request.Url.HostName + item.Url, item.__updatedAt);
            }
        }

        /// <summary>
        /// Generates the layout page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="content">The content.</param>
        protected void GenerateLayoutPage(dynamic site, dynamic content)
        {
            var layout = (string)content.Layout;

            string layoutPath = Path.Combine(this.RootPath, "Site", "Views", Path.GetDirectoryName(layout));
            string layoutFilename = Path.Combine(layoutPath, Path.GetFileName(layout) + ".cshtml");

            if (File.Exists(layoutFilename))
            {
                return;
            }

            Directory.CreateDirectory(layoutPath);

            var sourceFile = Path.Combine(this.RootPath, "NancyBlack", "Content", "Views", "_base" + Path.GetFileName(layout) + "layout.cshtml");
            if (File.Exists(sourceFile) == false)
            {
                sourceFile = Path.Combine(this.RootPath, "NancyBlack", "Content", "Views", "_basecontentlayout.cshtml");
            }
            File.Copy(sourceFile, layoutFilename);
        }

        protected dynamic HandleContentRequest(dynamic arg)
        {
            var url = (string)arg.path;
            if (url == null)
            {
                url = "/";
            }

            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }

            url = url.ToLowerInvariant();

            // invalid admin links
            if (url.StartsWith("/admin", StringComparison.InvariantCultureIgnoreCase))
            {
                return 404;
            }

            // invalid system links
            if (url.StartsWith("/_", StringComparison.InvariantCultureIgnoreCase))
            {
                return 404;
            }

            // invalid table get request
            if (url.StartsWith("/tables", StringComparison.InvariantCultureIgnoreCase))
            {
                return 404;
            }

            IContent requestedContent = null;

            // see if the url is collection request or content request
            var parts = url.Split('/');

            url = ContentModule.RewriteUrl(this.Context, arg, url);
            var subSiteName = (string)this.Context.Items["SubSite"];

            // can be /products/product1
            if (parts.Length > 2 && parts[1].EndsWith("s"))
            {
                // seems to be a collection
                var typeName = parts[1].Substring(0, parts[1].Length - 1);
                var datatype = this.SiteDatabase.DataType.FromName(typeName);
                var contentUrl = url;

                // edit url when using subsite example: convert "/products/..." to "/products/subSiteName/..."
                // for products, blogs (end with 's') and etc
                if (!string.IsNullOrEmpty(subSiteName))
                {
                    contentUrl = string.Join("/", "/" + parts[1], subSiteName);
                    contentUrl = contentUrl + "/" + string.Join("/", parts.Skip(2).ToArray());
                }

                var result = this.SiteDatabase.Query(typeName, string.Format("Url eq '{0}'", contentUrl)).FirstOrDefault();
                
                if (result != null)
                {
                    // convert it to IContent
                    if (result is IContent)
                    {
                        requestedContent = result as IContent;
                        requestedContent = ContentModule.MapPage(this.Context, requestedContent);
                    }
                    else
                    {
                        requestedContent = JObject.FromObject(result).ToObject<Page>();
                        (requestedContent as Page).SetTableName(typeName);
                    }
                }
            }

            // change url to subsite url ex: /contact to /micronics.in.th/contact
            if (!string.IsNullOrEmpty(subSiteName))
            {
                url = "/" + subSiteName + url;
            }

            // if it is not table, use content table instead
            if (requestedContent == null)
            {
                requestedContent = ContentModule.GetPage(this.SiteDatabase, url);
            }

            if (requestedContent == null)
            {
                // won't generate path which contains extension
                // as user might be requesting file
                if (string.IsNullOrEmpty(Path.GetExtension(url)) == false)
                {
                    return 404;
                }

                // only admin can generate
                if (this.CurrentUser.HasClaim("admin") == false)
                {
                    return 404;
                }

                requestedContent = ContentModule.CreatePage(this.SiteDatabase, url);
            }

            if (string.IsNullOrEmpty((string)requestedContent.RequiredClaims) == false)
            {
                var required = ((string)requestedContent.RequiredClaims).Split(',');
                var user = this.Context.CurrentUser as NcbUser;
                if (required.Any(c => user.HasClaim(c)) == false)
                {
                    // user does not have any required claims
                    if (this.Context.CurrentUser == NcbUser.Anonymous)
                    {
                        return 401;
                    }

                    return 403;
                }
            }

            this.SiteDatabase.DelayedInsert(new PageView()
            {
                ContentId = requestedContent.Id,
                TableName = requestedContent.TableName,
                AffiliateCode = this.Request.Cookies["source"],
                QueryString = this.Request.Url.Query,
                Path = this.Request.Url.Path,
                UserIP = this.Request.Headers.Host,
                Referer = this.Request.Headers.Referrer,
                UserAgent = this.Request.Headers.UserAgent
            });

            if (string.IsNullOrEmpty(requestedContent.Layout))
            {
                requestedContent.Layout = "Content";
            }

            this.GenerateLayoutPage(this.CurrentSite, requestedContent);

            ContentModule.ProcessPage(this.Context, requestedContent);

            return View[(string)requestedContent.Layout, new StandardModel(this, requestedContent, requestedContent)];
        }

#region All Logic Related to Content

        /// <summary>
        /// Get child content of given url
        /// </summary>
        /// <param name="db">Reference to the database</param>
        /// <param name="url">Base Url</param>
        /// <param name="levels">Level of items to get, 0 means unlimited.</param>
        /// <returns></returns>
        public static IEnumerable<Page> GetChildPages(NancyBlackDatabase db, string url, int levels = 0)
        {
            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }
            url = url.ToLowerInvariant() + "/";

            var query = db.Query<Page>()
                   .Where(p => p.Url.StartsWith(url))
                   .Where(p => p.DisplayOrder >= 0)
                   .OrderBy(p => p.DisplayOrder)
                   .OrderByDescending(p => p.__createdAt);

            if (levels == 0)
            {
                return query;
            }
            else
            {
                var finishQuery = query.AsEnumerable(); // hit the database
                return finishQuery.Where(p =>
                {

                    var urlParts = p.Url.Split('/');
                    return (urlParts.Length - 2) == levels;

                });
            }
        }

        /// <summary>
        /// Get TagName and Tag's child count
        /// </summary>
        /// <param name="db"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IEnumerable<TagGroup> GetTagGroupByName(NancyBlackDatabase db, string categoryUrl, string type)
        {
            if (categoryUrl.StartsWith("/") == false)
            {
                categoryUrl = "/" + categoryUrl;
            }
            categoryUrl = categoryUrl.ToLowerInvariant() + "/";

            return db.Query<Tag>()
                    .Where(tag => tag.Url == categoryUrl && tag.Type == type)
                    .Select(tag => tag.Name)
                    .GroupBy(tagName => tagName)
                    .Select(group => new TagGroup() { Count = group.Count(), Name = group.Key });
        }

        /// <summary>
        /// Get Content which match the same Tag
        /// </summary>
        /// <param name="db"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IEnumerable<object> GetContentByTag(NancyBlackDatabase db, Tag tag, int skip, int take)
        {
            var tags = db.Query<Tag>()
                        .Where(rec => rec.Url == tag.Url && rec.Type == tag.Type && rec.Name == tag.Name);

            foreach (var rec in tags)
            {
                yield return db.GetById(rec.Type, rec.ContentId);
            }
        }

        /// <summary>
        /// Get Root Content
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IEnumerable<Page> GetRootPages(NancyBlackDatabase db)
        {
            return db.Query<Page>()
                    .Where(p => p.Url.StartsWith("/") && p.Url.Substring(1).IndexOf('/') < 0)
                    .OrderBy(p => p.DisplayOrder);
        }

        /// <summary>
        /// Get content of given url and optionally creates the content
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Page GetPage(NancyBlackDatabase db, string url, bool create = false)
        {
            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }
            url = url.ToLowerInvariant();

            var page = db.Query<Page>()
                    .Where(p => p.Url == url)
                    .FirstOrDefault();

            if (page == null && create == true)
            {
                page = ContentModule.CreatePage(db, url);
            }

            return page;
        }

        /// <summary>
        /// Creates a new Page
        /// </summary>
        /// <param name="url"></param>
        /// <param name="layout"></param>
        /// <returns></returns>
        public static dynamic CreatePage(NancyBlackDatabase db, string url, string layout = "", string requiredClaims = "", int displayOrder = 0)
        {
            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }

            // try to find matching view that has same name as url
            var layoutFile = Path.Combine(_RootPath, "Site", "Views", url.Substring(1).Replace('/', '\\') + ".cshtml");
            if (File.Exists(layoutFile))
            {
                layout = url.Substring(1);
            }

            if (layout == "")
            {
                layout = "content";
            }

            // if URL is "/" generate home instead
            if (url == "/")
            {
                layout = "home";
            }

            if (url.StartsWith("/") == false)
            {
                url = "/" + url;
            }

            var createdContent = db.UpsertRecord<Page>(new Page()
            {
                Id = 0,
                Title = Path.GetFileName(url),
                Url = url.ToLowerInvariant(),
                Layout = layout,
                RequiredClaims = requiredClaims,
                DisplayOrder = displayOrder
            });

            return createdContent;
        }

#endregion

    }

}