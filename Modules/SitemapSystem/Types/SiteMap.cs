using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace NantCom.NancyBlack.Modules.SitemapSystem.Types
{
    public class SiteMap
    {

        private static LinkedList<SiteMapUrl> _Urls = new LinkedList<SiteMapUrl>();

        /// <summary>
        /// Registers the URL into sitemap
        /// </summary>
        /// <param name="url"></param>
        /// <param name="lastModified"></param>
        /// <param name="changeFreq"></param>
        /// <param name="priority"></param>
        public void RegisterUrl(string url, DateTime? lastModified = null, SiteMapUrl.ChangeFreq changeFreq = SiteMapUrl.ChangeFreq.daily, double priority = 0.5)
        {
            SiteMapUrl sitemapUrl = new SiteMapUrl();
            sitemapUrl.loc = url;
            sitemapUrl.changefreq = changeFreq;
            sitemapUrl.priority = priority;

            if (lastModified != null)
            {
                sitemapUrl.lastmod = DateTime.Now;
            }

            _Urls.AddLast(sitemapUrl);
        }

        /// <summary>
        /// Total indexes required for current site map size
        /// </summary>
        public int TotalIndexes
        {
            get
            {
                return (int)Math.Ceiling(_Urls.Count / 50000d);
            }
        }

        /// <summary>
        /// Writes the sitemap index, index is required when sitemap contians more than 50000 records
        /// </summary>
        /// <param name="s"></param>
        public void WriteIndex(string hostName, Stream s)
        {
            var writer = new XmlTextWriter(s, System.Text.Encoding.UTF8);
            writer.WriteStartDocument();

            writer.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

            for (int page = 0; page < this.TotalIndexes; page++)
            {
                writer.WriteStartElement("sitemap");

                writer.WriteStartElement("loc");
                writer.WriteValue("http://" + hostName + "/__sitemap/" + page);
                writer.WriteEndElement();

                writer.WriteStartElement("lastmod");
                writer.WriteValue(DateTime.Now);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.Flush();
            writer.Close();
            writer.Dispose();
        }

        /// <summary>
        /// Writes a sitemap file
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="s"></param>
        /// <param name="page"></param>
        public void WriteSitemap(Stream s, int page = 0)
        {
            var writer = new XmlTextWriter(s, System.Text.Encoding.UTF8);
            writer.WriteStartDocument();

            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            var pagesize = 50000;
            foreach (var url in _Urls.Skip( page * pagesize ).Take(pagesize))
            {
                writer.WriteStartElement("url");

                this.WriteValueElement(writer, "loc", url.loc);
                this.WriteValueElement(writer, "lastmod", url.lastmod);
                this.WriteValueElement(writer, "changefreq", url.changefreq.ToString());
                this.WriteValueElement(writer, "priority", url.priority);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.Flush();
            writer.Close();
            writer.Dispose();
        }

        private void WriteValueElement(XmlWriter writer, string name, object value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

    }

}