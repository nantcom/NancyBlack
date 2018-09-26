using Nancy.ViewEngines.Razor;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack
{
    public abstract class NancyBlackRazorThemeBase : NancyRazorViewBase
    {
        public const string THEMEPART_ADDRESS = "Theme_Address";
        public const string THEMEPART_SUBSCRIBEEMAIL = "Theme_SubscribeEmail";
        public const string THEMEPART_SUBSCRIBEEMAILCOMPLETED = "Theme_SubscribeEmailComplete";
        public const string THEMEPART_FOOTER_ABOUT = "Theme_Footer_About";
        public const string THEMEPART_FOOTER_ADDRESS = "Theme_Footer_Address";
        public const string THEMEPART_FOOTER_COPYRIGHT = "Theme_Footer_Copyright";
        
        /// <summary>
        /// Gets the OpenGraph URL
        /// </summary>
        public string GetOpenGraphUrl()
        {
            string url = this.Request.Url;
            if (string.IsNullOrEmpty(this.Request.Url.Query) == false)
            {
                url = url.Replace(this.Request.Url.Query.ToString(), "");
            }

            if (url.StartsWith("https://"))
            {
                url = url.Replace("https://", "http://");
            }

            // try to normalize the url
            if (url.StartsWith("http://www.") == false)
            {
                url = url.Replace("http://", "http://www.");
            }

            return url.ToLowerInvariant();
        }

        /// <summary>
        /// Currently accessing user
        /// </summary>
        protected NcbUser CurrentUser
        {
            get
            {
                return this.RenderContext.Context.CurrentUser as NcbUser;
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

        private string _LastPropertyName;

        /// <summary>
        /// Get Edit Attributes for given property name, the content is saved into site theme (sitesettings.dat)
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public NonEncodedHtmlString MakeThemeEditable(string propertyName)
        {
            _LastPropertyName = propertyName;
            return new NonEncodedHtmlString(string.Format("data-themeeditable=\"true\" data-propertyName=\"{0}\" data-html=\"true\"", propertyName));
        }

        /// <summary>
        /// Get Contents of the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetThemeContent(Func<dynamic, object> defaultContent)
        {
            if (_LastPropertyName == null)
            {
                throw new InvalidOperationException("GetEditAttribute was not used prior to calling this method.");
            }

            return this.GetThemeContent(_LastPropertyName, defaultContent);
        }
        
        /// <summary>
        /// Get Contents of the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public NonEncodedHtmlString GetThemeContent(string propertyName, Func<object, object> defaultContent)
        {
            _LastPropertyName = null;

            string value = null;
            var contentParts = this.ThemeContent.ContentParts as JObject;
            if (contentParts != null)
            {
                value = (string)contentParts[propertyName];
            }

            if (value == null)
            {
                return new NonEncodedHtmlString(defaultContent(null).ToString());
            }

            return value;
        }
        
        /// <summary>
        /// Find the content under given url
        /// </summary>
        /// <param name="url">Base Url </param>
        /// <param name="contentTemplate">Razor Template to render for each item of the output</param>
        public object ListRootContents(Func<dynamic, object> contentTemplate)
        {

            var list = ContentModule.GetRootPages(this.Context.GetSiteDatabase());

            foreach (var item in list)
            {
                var output = contentTemplate(item);
                this.WriteLiteral(output);
            }

            return null;
        }


        /// <summary>
        /// Get Attachments based on item type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> GetAttachments(string type = null)
        {
            var jarray = this.ThemeContent.Attachments as object[];
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
        /// Get attachment url
        /// </summary>
        /// <param name="content"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetAttachmentUrl(string type = null)
        {
            IEnumerable<dynamic> result = this.GetAttachments(type);
            var first = result.FirstOrDefault();
            if (first == null)
            {
                return string.Empty;
            }

            return first.Url;
        }

        /// <summary>
        /// Renders the section, with default content
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultContents"></param>
        /// <returns></returns>
        public NonEncodedHtmlString RenderSectionWithDefault(string name, Func<object, object> defaultContents)
        {
            if (this.IsSectionDefined(name))
            {
                return new NonEncodedHtmlString(RenderSection(name).ToString());
            }

            return new NonEncodedHtmlString(defaultContents(null).ToString());
        }

    }
}