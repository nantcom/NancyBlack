using Nancy.ViewEngines.Razor;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NantCom.NancyBlack
{
    public abstract class NancyBlackRazorViewBase : NancyRazorViewBase<StandardModel>
    {
        /// <summary>
        /// Gets the information about current request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        public new Nancy.Request Request
        {
            get
            {
                return this.RenderContext.Context.Request;
            }
        }

        /// <summary>
        /// Gets the current language requested
        /// </summary>
        public string Language
        {
            get
            {
                if (this.Context.Items.ContainsKey("Language"))
                {
                    return this.Context.Items["Language"].ToString();
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Currently Requesting Currency
        /// </summary>
        public string Currency
        {
            get
            {
                if (base.Context.Items.ContainsKey("Currency"))
                {
                    return base.Context.Items["Currency"].ToString();
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the content being rendered
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public IContent Content
        {
            get
            {
                if (this.Model == null)
                {
                    return null;
                }
                return this.Model.Content;
            }
        }

        private IContent _SEOContent;

        /// <summary>
        /// Gets the localized SEO Content (Title, Keyword, Description)
        /// </summary>
        public IContent SEOContent
        {
            get
            {
                if (this._SEOContent != null)
                {
                    return this._SEOContent;
                }

                if (this.Content == null)
                {
                    return new Page();
                }

                var page = new Page();
                page.Title = this.Content.Title;
                page.MetaKeywords = this.Content.MetaKeywords;
                page.MetaDescription = this.Content.MetaDescription;

                if (this.Content.SEOTranslations != null)
                {
                    var suffix = string.IsNullOrEmpty(this.Language) ? "" : "_" + this.Language;
                    var o = JObject.FromObject(this.Content.SEOTranslations as object);
                    var page2 = new Page();
                    page2.Title = o.Value<string>("Title" + suffix);
                    page2.MetaKeywords = o.Value<string>("MetaKeywords" + suffix);
                    page2.MetaDescription = o.Value<string>("MetaDescription" + suffix);

                    page.Title = string.IsNullOrEmpty(page2.Title) ? page.Title : page2.Title;
                    page.MetaKeywords = string.IsNullOrEmpty(page2.MetaKeywords) ? page.MetaKeywords : page2.MetaKeywords;
                    page.MetaDescription = string.IsNullOrEmpty(page2.MetaDescription) ? page.MetaDescription : page2.MetaDescription;
                }

                if (string.IsNullOrEmpty( page.MetaDescription ) == true)
                {
                    // try getting from content part
                    page.MetaDescription = this.GetContent("ShortText").ToHtmlString();
                }

                this._SEOContent = page;

                return this._SEOContent;
            }
        }

        private IContent _ThemeContent;

        /// <summary>
        /// Get the theme content, theme content is stored inside Item with Url "/" of Page Table
        /// (currently same as Site Content - must be changed when there is a theme support)
        /// </summary>
        public IContent ThemeContent
        {
            get
            {
                if (_ThemeContent == null)
                {
                    _ThemeContent = this.Context.GetSiteDatabase().Query<Page>()
                        .Where(p => p.Url == "/")
                        .FirstOrDefault();
                }

                return _ThemeContent;
            }
        }


        private IContent _SiteContent;

        /// <summary>
        /// Get the Site content, Site content is stored inside Item with Url "/" of Page Table
        /// </summary>
        public IContent SiteContent
        {
            get
            {
                if (_SiteContent == null)
                {
                    _SiteContent = this.Context.GetSiteDatabase().Query<Page>()
                        .Where(p => p.Url == "/")
                        .FirstOrDefault();
                }

                return _SiteContent;
            }
        }

        /// <summary>
        /// Currently accessing user
        /// </summary>
        protected NcbUser CurrentUser
        {
            get
            {
                if (this.RenderContext.Context.CurrentUser == null)
                {
                    return NcbUser.Anonymous;
                }

                return this.RenderContext.Context.CurrentUser as NcbUser;
            }
        }

        /// <summary>
        /// Gets the currently running site.
        /// </summary>
        /// <value>
        /// The site.
        /// </value>
        public dynamic Site
        {
            get
            {
                return this.Context.GetSiteSettings();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is in edit mode.
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
        /// Gets the database access for this request.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public NancyBlackDatabase Database
        {
            get
            {
                return this.Context.GetSiteDatabase();
            }
        }

        /// <summary>
        /// Create Razor Compatible dynamic from anonymous type
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public dynamic GetDynamicModel(object input)
        {
            return JObject.FromObject(input);
        }

        /// <summary>
        /// Serializes input to json
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string GetJson(object input)
        {
            return JsonConvert.SerializeObject(input);
        }

        public string GetJsonWithoutContentParts(object input)
        {
            var copy = JObject.FromObject(input);
            copy.Remove("ContentParts");

            return copy.ToString(Formatting.None);
        }

        #region Content

        private string _LastPropertyName;

        /// <summary>
        /// Define that the element contains list of items from a table
        /// </summary>
        /// <param name="rootUrl">Base URL of the items</param>
        /// <param name="table">Table to get items from</param>
        /// <param name="name">name of the collection (displayed in editor)</param>
        /// <param name="defaultLayout">default layout name for new items</param>
        /// <returns></returns>
        public NonEncodedHtmlString MakeEditableCollection(string rootUrl, string table = null, string name = null, string defaultLayout = null)
        {
            return new NonEncodedHtmlString(string.Format(
                @"ncw-collection=""true"" rooturl=""{0}"" table=""{1}"" name=""{2}"" layout=""{3}"" ",
                rootUrl, table, name, defaultLayout));
        }

        /// <summary>
        /// truncate text to make it within maxLength and put ... at the end of content
        /// </summary>
        /// <param name="source"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public string TruncateWithEllipsis(string source, int maxLength)
        {
            if (source == null)
            {
                return string.Empty;
            }

            if (source.Count() < maxLength)
            {
                return source;
            }

            return source.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Get Edit Attributes for given property name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public NonEncodedHtmlString MakeEditable(string propertyName)
        {
            _LastPropertyName = propertyName;
            return new NonEncodedHtmlString(string.Format("data-editable=\"true\" data-propertyName=\"{0}\" data-html=\"true\"", propertyName));
        }

        /// <summary>
        /// Get Edit Attributes for given property name of given content
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public NonEncodedHtmlString MakeEditableForPage(IContent content, string propertyName)
        {
            _LastPropertyName = propertyName;
            return new NonEncodedHtmlString(string.Format("data-editable=\"true\" data-propertyName=\"{0}\" data-html=\"true\" data-id=\"{1}\" data-table=\"{2}\"", propertyName, content.Id, content.TableName));
        }

        /// <summary>
        /// Get Edit Attributes for given property name of given content
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public NonEncodedHtmlString MakeEditableForItem(dynamic content, string propertyName)
        {
            _LastPropertyName = propertyName;
            return new NonEncodedHtmlString(string.Format("data-editable=\"true\" data-propertyName=\"{0}\" data-html=\"true\" data-id=\"{1}\" data-table=\"{2}\"", propertyName, content.Id, content.TableName));
        }


        /// <summary>
        /// Get Edit Attributes for given property name of current site 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public NonEncodedHtmlString MakeEditableForSite(string propertyName)
        {
            IContent siteContent = this.SiteContent;
            this._LastPropertyName = propertyName;
            return new NonEncodedHtmlString(string.Format("data-editable=\"true\" data-propertyName=\"{0}\" data-html=\"true\" data-id=\"{1}\" data-table=\"{2}\"", propertyName, siteContent.Id, siteContent.TableName));
        }

        /// <summary>
        /// Gets localized content from content parts object and property name
        /// </summary>
        /// <param name="contentParts"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public string GetLocalizedContent(JObject contentParts, string propertyName)
        {
            string value = null;
            if (contentParts != null)
            {
                // use the localized one if the localized content is requested
                if (this.Language != string.Empty)
                {
                    value = (string)contentParts[propertyName + "_" + this.Language];

                    // try to get from english version
                    if (value == null)
                    {
                        value = (string)contentParts[propertyName + "_en"];
                    }

                    // try to get the non-localized version
                    if (value == null)
                    {
                        value = (string)contentParts[propertyName];
                    }
                }
                else
                {
                    value = (string)contentParts[propertyName];
                }

            }

            return value;
        }

        /// <summary>
        /// Get Contents of the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(string propertyName, Func<object, object> defaultContent)
        {
            _LastPropertyName = null;

            string value = null;
            var contentParts = (this.Content as IContent).ContentParts as JObject;

            if (contentParts != null)
            {
                value = this.GetLocalizedContent(contentParts, propertyName);
            }

            if (value == null)
            {
                try
                {
                    return new NonEncodedHtmlString(defaultContent(null).ToString());
                }
                catch (Exception ex)
                {
                    return "Error: " + ex.Message;
                }
            }

            return value;
        }

        /// <summary>
        /// Get Contents of the specified property name in Model.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(string propertyName)
        {
            return this.GetContent(this.Content.ContentParts, propertyName);
        }

        /// <summary>
        /// Get Content of the specified property name in contentParts.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(JObject contentParts, string propertyName)
        {
            _LastPropertyName = null;

            string value = null;
            if (contentParts != null)
            {
                value = this.GetLocalizedContent(contentParts, propertyName);
            }

            if (value == null)
            {
                return new NonEncodedHtmlString(value);
            }

            return value;
        }

        /// <summary>
        /// Get Contents of the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(Func<dynamic, object> defaultContent)
        {
            if (_LastPropertyName == null)
            {
                throw new InvalidOperationException("MakeEditable was not used prior to calling this method.");
            }

            return this.GetContent(_LastPropertyName, defaultContent);
        }


        /// <summary>
        /// Get Contents of the specified property name from specified item
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(IContent content, Func<dynamic, object> defaultContent)
        {
            if (_LastPropertyName == null)
            {
                throw new InvalidOperationException("MakeEditable was not used prior to calling this method.");
            }

            string value = null;
            var contentParts = content.ContentParts as JObject;
            if (contentParts != null)
            {
                value = this.GetLocalizedContent(contentParts, _LastPropertyName);
            }

            _LastPropertyName = null;

            if (value == null)
            {
                return new NonEncodedHtmlString(defaultContent(null).ToString());
            }

            return value;
        }


        /// <summary>
        /// Get Contents of the specified property name from Site Content
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetSiteContent(Func<dynamic, object> defaultContent)
        {
            return this.GetContent(this.SiteContent, defaultContent);
        }

        #endregion

        #region Content Hierachy

        /// <summary>
        /// Querys
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListCollection(string entityName, Func<dynamic, object> contentTemplate)
        {

#if DEBUG
            if (entityName == null)
            {
                throw new ArgumentNullException("collectionName");
            }
#endif
            var list = Database.QueryAsJObject(entityName, oDataSort: "DisplayOrder");

            foreach (var item in list)
            {
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }

        /// <summary>
        /// Querys
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListProductUnderUrl(string entityName, string url, string limit, Func<dynamic, object> contentTemplate)
        {

#if DEBUG
            if (entityName == null)
            {
                throw new ArgumentNullException("collectionName");
            }
#endif
            var list = Database.QueryAsJObject(entityName,
                string.Format("startswith(Url,'{0}') and (IsVariation eq 0)", url),
                "DisplayOrder",
                take: limit);

            foreach (var item in list)
            {
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }


        /// <summary>
        /// List item in collection under given url
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListCollectionUnderUrl(string entityName, string url, Func<dynamic, object> contentTemplate)
        {

#if DEBUG
            if (entityName == null)
            {
                throw new ArgumentNullException("entityName");
            }

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
#endif
            var list = Database.QueryAsJObject(entityName, string.Format("startswith(Url,'{0}')", url), "DisplayOrder");

            foreach (var item in list)
            {
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }

        /// <summary>
        /// List item in collection under this.Model.Content's url which containing tag
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        /// <param name="take">use in query of Tag's table</param>
        /// <param name="skip">use in query of Tag's table</param>
        /// <param name="sortColumn">use in query of Tag's table</param>
        public object ListChildContents(Func<dynamic, object> contentTemplate, string tag, string type, int take = 0, int skip = 0, string sortColumn = null)
        {
            var matchedTags = Database.QueryAsJObject(
                "Tag",
                string.Format("Url eq '{0}' and Name eq '{1}' and Type eq '{2}'", this.Model.Content.Url, tag, type),
                sortColumn,
                skip.ToString(),
                take.ToString()
            );

            foreach (var record in matchedTags)
            {
                var item = this.Database.GetById(type, record.Value<int>("ContentId"));
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }

        /// <summary>
        /// List item in collection under this.Model.Content's url
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListChildContents(Func<dynamic, object> contentTemplate, int take = 0, int skip = 0, string sortColumn = null)
        {
            var list = Database.QueryAsJObject("Page", string.Format("startswith(Url,'{0}') and Url ne '{0}'", this.Model.Content.Url), sortColumn, skip.ToString(), take.ToString());

            foreach (var item in list)
            {
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }

        /// <summary>
        /// Find the Content under current url
        /// </summary>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListChildContents(Func<dynamic, object> contentTemplate)
        {
            return this.ListChildContents(this.Request.Url.Path, contentTemplate);
        }

        /// <summary>
        /// Find the content under given url
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListChildContents(string url, Func<IContent, object> contentTemplate, int levels = 0)
        {

#if DEBUG
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
#endif
            var list = ContentModule.GetChildPages(this.Database, url, levels);

            foreach (var item in list)
            {
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }

        /// <summary>
        /// Find the content under given url
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListRootContents(Func<dynamic, object> contentTemplate)
        {

            var list = ContentModule.GetRootPages(this.Database);

            foreach (var item in list)
            {
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }

        #endregion

        #region Attachments

        /// <summary>
        /// Get Attachments based on item type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> GetAttachments(dynamic content = null, string type = null)
        {
            if (content == null)
            {
                content = this.Content;
            }

            var jarray = content.Attachments as object[];

            if (content.Attachments is JArray)
            {
                jarray = ((JArray)content.Attachments).ToArray<object>();
            }

            if (jarray == null)
            {
                return new dynamic[] { };
            }

            if (type == null)
            {
                return jarray.AsEnumerable<dynamic>();
            }

            return from dynamic item in jarray
                   where item.AttachmentType == type
                   select item;
        }

        /// <summary>
        /// Get attachment of given type for specified content
        /// </summary>
        /// <param name="content">The content to get attachment</param>
        /// <param name="primaryType">The type of attachment to get</param>
        /// <param name="fullPath">Whether full path is required</param>
        /// <param name="secondaryTypes">Fall back attachment types, UserUpload which is the default type is automatically added</param>
        /// <returns></returns>
        public string GetAttachmentUrl(dynamic content, string primaryType = null, bool fullPath = false, params string[] secondaryTypes)
        {
            var types = new List<string>();

            if (primaryType != null)
            {
                types.Add(primaryType);
            }

            if (secondaryTypes != null && secondaryTypes.Length > 0)
            {
                types.AddRange(secondaryTypes);
            }

            types.Add("default");
            types.Add("UserUpload");


            var result = types.Select( type => (this.GetAttachments(content, type) as IEnumerable<dynamic>).ToList() )
                        .FirstOrDefault(list => list.Count > 0 );

            if (result == null)
            {
                types.Clear();
                types.Add("default");
                types.Add("UserUpload");

                result = types.Select(type => (this.GetAttachments(content, type) as IEnumerable<dynamic>).ToList())
                            .FirstOrDefault(list => list.Count > 0);
            }

            if (result == null)
            {
                return string.Empty;
            }

            var first = result[0];
            
            if (fullPath == true)
            {
                var url = this.Request.Url.Scheme;
                url += "://" + this.Request.Url.HostName;

                if (this.Request.Url.Port != 80)
                {
                    url += ":" + this.Request.Url.Port;
                }

                url += first.Url;

                return url;
            }

            return first.Url;
        }

        /// <summary>
        /// Get attachment url of given type for currently viewing content
        /// </summary>
        /// <param name="primaryType">The type of attachment to get</param>
        /// <param name="fullPath">Whether full path is required</param>
        /// <param name="secondaryTypes">Fall back attachment types, UserUpload which is the default type is automatically added</param>
        /// <returns></returns>
        public string GetAttachmentUrl(string primaryType, bool fullPath = false, params string[] secondaryTypes)
        {
            return this.GetAttachmentUrl(this.Content, primaryType, fullPath, secondaryTypes);
        }

        #endregion
    }
}