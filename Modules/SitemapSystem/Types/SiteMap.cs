﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace NantCom.NancyBlack.Modules.SitemapSystem.Types
{
    public class SiteMap
    {
        private List<SiteMapUrl> _Urls = new List<SiteMapUrl>();

        /// <summary>
        /// Date/Time the sitemap was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        public List<SiteMapUrl> Urls
        {
            get
            {
                return _Urls;
            }
            set
            {
                _Urls = value;
            }
        }

        public SiteMap()
        {
            this.CreatedDate = DateTime.UtcNow;
        }

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
            sitemapUrl.lastmod = lastModified ?? DateTime.Now;

            _Urls.Add(sitemapUrl);
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
                writer.WriteValue(this.CreatedDate);
                writer.WriteEndElement();


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
            foreach (var url in _Urls.Skip(page * pagesize).Take(pagesize))
            {
                writer.WriteStartElement("url");
                
                this.WriteValueElement(writer, "loc", url.loc);

                if (url.lastmod == default(DateTime))
                {
                    url.lastmod = DateTime.Now;
                }

                this.WriteValueElement(writer, "lastmod", url.lastmod.ToUniversalTime());
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

        private void WriteValueElement(XmlWriter writer, string name, DateTime value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }
    }

}