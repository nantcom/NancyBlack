﻿using Nancy;
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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using Nancy.Bootstrapper;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Net;
using HtmlAgilityPack;

namespace NantCom.NancyBlack.Modules
{
    public class ContentModule : BaseModule, IRequireGlobalInitialize
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

        public ContentModule()
        {
            Get["/robots.txt"] = this.HandleRequest((arg) =>
            {
                if (File.Exists(this.RootPath + "\\Site\\robots.txt"))
                {
                    return File.ReadAllText(this.RootPath + "\\Site\\robots.txt");
                }

                return "User-agent: *\nDisallow: ";
            });

            Get["/ads.txt"] = this.HandleRequest((arg) =>
            {
                if (File.Exists(this.RootPath + "\\Site\\ads.txt"))
                {
                    return File.ReadAllText(this.RootPath + "\\Site\\ads.txt");
                }

                return 404;

            });

            Get["/{path*}"] = this.HandleRequest(this.HandleContentRequestCached);

            Get["/"] = this.HandleRequest(this.HandleContentRequestCached);

            Get["/__content/pageviewcount/{table}/{id}"] = this.HandleRequest(this.GetPageViewCount);


            _RootPath = this.RootPath;

            SiteMapModule.SiteMapRequested += SiteMapModule_SiteMapRequested;
            NancyBlackDatabase.ObjectCreated += InsertTag_ObjectCreate;
            NancyBlackDatabase.ObjectUpdated += UpdateTag_ObjectUpdate;

        }
        
        private static IContent _SiteContent;

        /// <summary>
        /// Gets the Site Content
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static IContent GetSiteContent( NancyContext ctx )
        {
            if (ctx.IsAdminUser())
            {
                return ContentModule.GetPage(ctx.GetSiteDatabase(), "/", true);
            }

            return _SiteContent;
        }

        /// <summary>
        /// Gets the Theme Content
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static IContent GetThemeContent(NancyContext ctx)
        {
            return ContentModule.GetSiteContent(ctx);
        }

        /// <summary>
        /// Initialize things that can share between requests
        /// </summary>
        /// <param name="ctx"></param>
        public void GlobalInitialize(NancyContext ctx)
        {
            ContentModule._SiteContent = ContentModule.GetPage(ctx.GetSiteDatabase(), "/", true);

            ContentModule.ProcessContentPart(ctx, ContentModule._SiteContent);
        }

        /// <summary>
        /// Get Page View Count for given item in table
        /// </summary>
        /// <param name="db"></param>
        /// <param name="table"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static long GetPageViewCount( NancyBlackDatabase db, dynamic siteSettings, string table, int id )
        {
            var key = "PageViewCount-" + table + id;
            object cached = MemoryCache.Default.Get(key);
            if (cached != null)
            {
                return (long)cached;
            }

            if (siteSettings.analytics == null)
            {
                dynamic result = db.Query
                                (string.Format("SELECT COUNT(Id) as Hit FROM PageView WHERE ContentId = {0} AND TableName = '{1}'", id, table),
                                new
                                {
                                    Hit = 0
                                }).FirstOrDefault();

                if (result == null)
                {
                    result = 0;
                }

                MemoryCache.Default.Add(key, result, DateTimeOffset.Now.AddMinutes(10));
                return result;
            }
            else
            {
                var content = db.QueryAsDynamic(table, "Id eq " + id).FirstOrDefault();
                if (content == null)
                {
                    MemoryCache.Default.Add(key, 0, DateTimeOffset.Now.AddMinutes(10));
                    return 0; // wrong content
                }

                string url = content.Url;
                if (url == null)
                {
                    MemoryCache.Default.Add(key, 0, DateTimeOffset.Now.AddMinutes(10));
                    return 0; // cannot get Url
                }

                if (url.Contains("/archive/"))
                {
                    url = url.Replace("/archive/", "/");
                }

                url = url.Replace('/', '-');

                long pageViews = 0;
                lock (BaseModule.GetLockObject("PageViewSummary-" + url)) // ensure only one thread is working on calculation
                {
                    // if multiple threads is locked - they will arrive here when lock is released
                    // so check the cache again
                    cached = MemoryCache.Default.Get(key);
                    if (cached != null)
                    {
                        return (int)cached;
                    }

                    CloudTable summaryTable = ContentModule.GetSummaryTable(siteSettings);
                    var queryString = TableQuery.CombineFilters(
                                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, url),
                                    TableOperators.And,
                                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, url));

                    var query = new TableQuery<PageViewSummary>();
                    var result = summaryTable.ExecuteQuery<PageViewSummary>(query.Where(queryString)).FirstOrDefault();

                    if (result == null)
                    {
                        result = new PageViewSummary();
                        result.PageViews = 0;
                        result.Path = url;
                        result.PrepareForAzure();
                        result.Timestamp = DateTimeOffset.MinValue;

                        summaryTable.Execute(TableOperation.InsertOrReplace(result));
                    }

                    // find all pageview since the pageview summary timestamp
                    var pvQueryString = TableQuery.CombineFilters(
                                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, url),
                                    TableOperators.And,
                                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, result.Timestamp));

                    CloudTable rawTable = ContentModule.GetPageViewTable(siteSettings);
                    var pageviewQuery = new TableQuery<PageView>();
                    var count = rawTable.ExecuteQuery<PageView>(pageviewQuery.Where(pvQueryString)).Count();

                    result.PageViews = result.PageViews + count;
                    summaryTable.Execute(TableOperation.InsertOrReplace(result));

                    pageViews = result.PageViews;

                    MemoryCache.Default.Add(key, pageViews, DateTimeOffset.Now.AddMinutes(10));
                    return pageViews;
                }

            }
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
            if (this.SiteDatabase.DataType.FromName(table) == null)
            {
                return 400;
            }

            this.Context.Items["NoCookie"] = true;

            var count = ContentModule.GetPageViewCount(this.SiteDatabase, this.CurrentSite, table, id);
            return count.ToString();
        }


        /// <summary>
        /// Gets the pageview table
        /// </summary>
        /// <returns></returns>
        private static CloudTable GetPageViewTable(dynamic siteSettings, bool cache = true)
        {
            Func<CloudTable> getTable = () =>
            {
                var cred = new StorageCredentials((string)siteSettings.analytics.raw.credentials);
                var client = new CloudTableClient(new Uri((string)siteSettings.analytics.raw.server), cred);
                return client.GetTableReference((string)siteSettings.analytics.raw.table);
            };


            if (cache == false)
            {
                return getTable();
            }

            var key = string.Format("azure{0}-{1}-{2}",
                               (string)siteSettings.analytics.raw.credentials,
                               (string)siteSettings.analytics.raw.server,
                               (string)siteSettings.analytics.raw.table).GetHashCode().ToString();

            var table = MemoryCache.Default[key] as CloudTable;
            if (table == null)
            {
                table = getTable();

                MemoryCache.Default.Add(key, table, DateTimeOffset.Now.AddDays(1));
            }

            return table;
        }

        /// <summary>
        /// Gets the pageview table
        /// </summary>
        /// <returns></returns>
        private CloudTable GetPageViewTable(bool cache = true)
        {
            return ContentModule.GetPageViewTable(this.CurrentSite, cache);
        }

        /// <summary>
        /// Gets the Summary table
        /// </summary>
        /// <returns></returns>
        private static CloudTable GetSummaryTable(dynamic siteSettings, bool cache = true)
        {
            Func<CloudTable> getTable = () =>
            {
                var cred = new StorageCredentials((string)siteSettings.analytics.summary.credentials);
                var client = new CloudTableClient(new Uri((string)siteSettings.analytics.summary.server), cred);
                return client.GetTableReference((string)siteSettings.analytics.summary.table);
            };

            if (cache == false)
            {
                return getTable();
            }

            var key = string.Format("azure{0}-{1}-{2}",
                               (string)siteSettings.analytics.summary.credentials,
                               (string)siteSettings.analytics.summary.server,
                               (string)siteSettings.analytics.summary.table).GetHashCode().ToString();

            var table = MemoryCache.Default[key] as CloudTable;
            if (table == null)
            {
                table = getTable();

                MemoryCache.Default.Add(key, table, DateTimeOffset.Now.AddDays(1));
            }

            return table;
        }

        /// <summary>
        /// Gets the Summary table
        /// </summary>
        /// <returns></returns>
        private CloudTable GetSummaryTable(bool cache = true)
        {
            return ContentModule.GetSummaryTable(this.CurrentSite, cache);
        }

        private void SendPageView(IContent requestedContent)
        {
            string source = "";
            if (this.Request.Cookies.ContainsKey("source"))
            {
                source = this.Request.Cookies["source"];
            }

#if !DEBUG
            if (this.Request.Headers.UserAgent.StartsWith("Pingdom.com") ||
                this.Request.Headers.UserAgent.StartsWith("loader.io"))
            {
                return;
            }

            var pageView = new PageView()
            {
                __createdAt = DateTime.Now,
                __updatedAt = DateTime.MinValue,
                ContentId = requestedContent.Id,
                TableName = requestedContent.TableName,
                AffiliateCode = source,
                QueryString = this.Request.Url.Query,
                Path = this.Request.Url.Path,
                UserIP = this.Request.UserHostAddress,
                Referer = this.Request.Headers.Referrer,
                UserAgent = this.Request.Headers.UserAgent,
                UserUniqueId = this.Context.Items["userid"] as string
            };
            
            // If not set to use azure table
            if (this.CurrentSite.analytics == null)
            {
                this.SiteDatabase.DelayedInsert(pageView);
                return;
            }

            var table = this.GetPageViewTable();

            Task.Run(() =>
            {
                try
                {
                    pageView.PrepareForAuzre();

                    var op = TableOperation.Insert(pageView);
                    table.Execute(op);
                }
                catch (Exception)
                {
                }
            });
#endif
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

        protected dynamic HandleContentRequestCached( dynamic arg )
        {
            var key = "ContentModule-" + this.Request.Url.ToString();

            if (this.Context.Items.ContainsKey("FBBot"))
            {
                this.GlobalInitialize(this.Context);

                var result = this.HandleContentRequest(arg, false);

                if (result is int)
                {
                    return result;
                }

                return View["_base_facebook", new StandardModel(this, result, result)];
            }

            // Admin will always clear the cache when visit the given page
            // and will always see content without processing
            // as admin might be editing the page
            if (this.CurrentUser.HasClaim("admin"))
            {
                this.GlobalInitialize(this.Context);
                MemoryCache.Default.Remove(key);

                var result = this.HandleContentRequest(arg, false);

                if (result is int)
                {
                    return result;
                }

                return View[(string)result.Layout, new StandardModel(this, result, result)];
            }

            IContent requestedContent = MemoryCache.Default[key] as IContent;

            if (requestedContent == null)
            {
                lock (BaseModule.GetLockObject(key))
                {
                    // other thread may procssed this url already
                    requestedContent = MemoryCache.Default[key] as IContent;
                    if (requestedContent != null)
                    {
                        return requestedContent;
                    }

                    var result = this.HandleContentRequest(arg, true);
                    if (result is int)
                    {
                        return result;
                    }

                    requestedContent = result as IContent;
                    MemoryCache.Default.Add(key, requestedContent, DateTimeOffset.Now.AddHours(1));
                }
            }

            this.SendPageView(requestedContent);
            return View[(string)requestedContent.Layout, new StandardModel(this, requestedContent, requestedContent)];
        }

        /// <summary>
        /// Read Content from given URL in arg
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="processContentPart">Whether to process the content</param>
        /// <returns></returns>
        protected dynamic HandleContentRequest(dynamic arg, bool processContentPart)
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
            var subSiteName = (string)this.Context.Items[ContextItems.SubSite];

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
                    if (this.Context.CurrentUser.UserName == NcbUser.Anonymous)
                    {
                        return 401;
                    }

                    return 403;
                }
            }

            string source = null;
            if (this.Request.Cookies.ContainsKey("source") == true)
            {
                source = this.Request.Cookies["source"];

            }

            if (string.IsNullOrEmpty(requestedContent.Layout))
            {
                requestedContent.Layout = "Content";
            }

            this.GenerateLayoutPage(this.CurrentSite, requestedContent);

            ContentModule.ProcessPage(this.Context, requestedContent);

            this.SendPageView(requestedContent);

            if (processContentPart)
            {
                ContentModule.ProcessContentPart(this.Context, requestedContent);
            }

            return requestedContent;
        }

        private static void ProcessContentPart(NancyContext ctx, IContent content)
        {
            if (content.ContentParts == null)
            {
                return;
            }

            var contentParts = content.ContentParts as JObject;
            foreach (var item in contentParts.Properties())
            {
                var processedContentPart = item.Value.ToString();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(item.Value.ToString());
                foreach (var img in doc.QuerySelectorAll("img"))
                {
                    var srcAttribute = img.Attributes["src"];
                    if (srcAttribute == null ||
                        string.IsNullOrEmpty(srcAttribute.Value) ||
                        srcAttribute.Value.StartsWith("/") == false)
                    {
                        continue;
                    }

                    img.SetAttributeValue("ncb-imgdefer", srcAttribute.Value);
                    srcAttribute.Remove();

                }

                item.Value = doc.DocumentNode.OuterHtml;
            }
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