using Nancy.ViewEngines.Razor;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NantCom.NancyBlack.Modules.ContentSystem
{
    public abstract class NancyBlackRazorViewBase : NancyRazorViewBase
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
                return this.Model.Site;
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
        public EncodedHtmlString EditableAttributes(string propertyName, bool html = true, bool global = false)
        {
            var htmlString = string.Format("data-propertyName=\"{0}\" data-html=\"{1}\" data-global=\"{2}\"",
                    propertyName,
                    html,
                    global);

            return htmlString;
        }

    }
}