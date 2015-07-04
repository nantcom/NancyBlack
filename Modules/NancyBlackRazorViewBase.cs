using Nancy.ViewEngines.Razor;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NantCom.NancyBlack
{
    public abstract class NancyBlackRazorViewBase : NancyRazorViewBase<dynamic>
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
        public dynamic Content
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
                return this.RenderContext.Context.Items["CurrentSite"];
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
                if (this.Model == null)
                {
                    return null;
                }
                return this.Model.Database;
            }
        }
        
        /// <summary>
        /// Gets the absolute site path from given path
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public string SitePath( string path )
        {
            if (this.Site == null)
            {
                return path;
            }

            return string.Concat("/Sites/", this.Site.HostName, path);
        }

        /// <summary>
        /// Create Razor Compatible dynamic from anonymous type
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public dynamic GetDynamicModel( object input )
        {
            return JObject.FromObject(input);
        }

        /// <summary>
        /// Serializes input to json
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string GetJson( object input )
        {
            return JsonConvert.SerializeObject(input);
        }
        
        #region Content Editing

        private string _LastPropertyName;

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
        /// Get Contents of the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(string propertyName, Func<object, object> defaultContent)
        {
            _LastPropertyName = null;

            var value = (string)this.Content[propertyName];
            if (value == null)
            {
                return new NonEncodedHtmlString(defaultContent(null).ToString());
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
                throw new InvalidOperationException("GetEditAttribute was not used prior to calling this method.");
            }

            var value = (string)this.Content[_LastPropertyName];
            if (value == null)
            {
                return new NonEncodedHtmlString(defaultContent(null).ToString());
            }

            return value;
        }

        /// <summary>
        /// Get Content with editable area
        /// </summary>
        /// <returns></returns>
        public void GetEditableContent(string propertyName, Func<dynamic, object> defaultContent, string enclosingTag = "div", string classes = "")
        {
            var content = (string)this.Content[propertyName];
            if (content == null)
            {
                content = defaultContent(null).ToString();
            }

            this.WriteLiteral(string.Format("<{0} class=\"{1}\" data-editable=\"true\" data-propertyName=\"{2}\">", enclosingTag, classes, propertyName));
            this.WriteLiteral(content);
            this.WriteLiteral(string.Format("</{0}>", enclosingTag));
        }

        #endregion

        #region Content Hierachy

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
        public object ListChildContents(string url, Func<dynamic, object> contentTemplate)
        {

#if DEBUG
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
#endif
            var list = ContentModule.GetChildContent(this.Database, url);
                
            foreach (var item in list)
            {
                var output = contentTemplate(JObject.FromObject(item));
                this.WriteLiteral(output);
            }

            return null;
        }

        #endregion
    }
}