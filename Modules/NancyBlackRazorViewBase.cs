using Nancy.ViewEngines.Razor;
using NantCom.NancyBlack.Modules.DatabaseSystem;
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
        public Nancy.Request Request
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
        /// Gets the currently running site.
        /// </summary>
        /// <value>
        /// The site.
        /// </value>
        public dynamic Site
        {
            get
            {
                if (this.Model == null)
                {
                    return null;
                }
                return this.Model.Site;
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
        /// Output an Editable Area of Content
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="tagClass">The tag class.</param>
        /// <param name="tagId">The tag identifier.</param>
        /// <returns></returns>
        public NonEncodedHtmlString EditAttributes(string propertyName, bool html = true, bool global = false)
        {
            var htmlString = string.Format("data-editable=\"true\" data-propertyName=\"{0}\" data-html=\"{1}\" data-global=\"{2}\"",
                    propertyName,
                    html,
                    global);

            return htmlString;
        }

        /// <summary>
        /// Get Contents of the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetContent(string propertyName)
        {
            var value = (string)this.Content[propertyName];

            return value;
        }

        /// <summary>
        /// Determines whether the specified property name has content.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public bool HasContent( string propertyName )
        {
            if (this.Content is JObject)
            {
                return this.Content[propertyName] != null;
            }

            return false;
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

    }
}