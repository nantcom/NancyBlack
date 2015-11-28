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


        private IContent _ThemeContent;

        /// <summary>
        /// Get the theme content, theme content is stored inside Item with Url "/" of Page Table
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

        public string GetJsonWithoutContentParts( IContent input )
        {
            var copy = JObject.FromObject(input);
            copy.Remove("ContentParts");

            return copy.ToString();
        }
        
        #region Content Editing

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
                // use the localized one if the localized content is requested
                if (this.Context.Items.ContainsKey("Language") == true)
                {
                    value = (string)contentParts[propertyName + "-" + this.Context.Items["Language"]];
                }

                // try to get the non-localized version
                if (value == null)
                {
                    value = (string)contentParts[propertyName];
                }
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
        /// Get Contents of the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(string propertyName)
        {
            _LastPropertyName = null;

            string value = null;
            var contentParts = (this.Content as IContent).ContentParts as JObject;
            if (contentParts != null)
            {
                value = (string)contentParts[propertyName];
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
                // use the localized one if the localized content is requested
                if (this.Context.Items.ContainsKey("Language") == true)
                {
                    value = (string)contentParts[_LastPropertyName + "-" + this.Context.Items["Language"]];
                }

                // try to get the non-localized version
                if (value == null)
                {
                    value = (string)contentParts[_LastPropertyName];
                }
            }

            _LastPropertyName = null;

            if (value == null)
            {
                return new NonEncodedHtmlString(defaultContent(null).ToString());
            }

            return value;
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
        public object ListChildContents(string url, Func<IContent, object> contentTemplate)
        {

#if DEBUG
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
#endif
            var list = ContentModule.GetChildPages(this.Database, url);

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
        /// Get First attachment url of given content
        /// </summary>
        /// <param name="content"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetAttachmentUrl(dynamic content)
        {

            var jarray = content.Attachments as object[];
            if (jarray == null)
            {
                return null;
            }

            var item = jarray[0] as JObject;
            return item["Url"].ToString();
        }

        /// <summary>
        /// Get First attachment url
        /// </summary>
        /// <param name="content"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetAttachmentUrl()
        {
            return this.GetAttachmentUrl(this.Content);
        }

        /// <summary>
        /// Get attachment url
        /// </summary>
        /// <param name="content"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetAttachmentUrl(dynamic content, string type = null, bool fullPath = false)
        {
            IEnumerable<dynamic> result = this.GetAttachments(content, type);
            var first = result.FirstOrDefault();
            if (first == null)
            {
                return string.Empty;
            }

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
        /// Get attachment url
        /// </summary>
        /// <param name="content"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetAttachmentUrl(string type, bool fullPath = false)
        {
            return this.GetAttachmentUrl(this.Content, type, fullPath);
        }

        #endregion
    }
}