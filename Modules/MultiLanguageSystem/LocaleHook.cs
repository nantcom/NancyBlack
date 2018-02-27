using NantCom.NancyBlack.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Bootstrapper;
using MaxMind.GeoIP2;
using System.Reflection;
using System.IO;
using MaxMind.GeoIP2.Responses;
using System.Net;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace NantCom.NancyBlack.Modules.MultiLanguageSystem
{
    public class LocaleHook : IPipelineHook
    {
        private static DatabaseReader _GeoIP;
        
        public void Hook(IPipelines p)
        {

            p.BeforeRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (_GeoIP == null)
                {
                    var path = Path.Combine(ctx.GetRootPath(), "App_Data", "GeoLite2-Country.mmdb");

                    if (File.Exists(path))
                    {
                        var fi = new FileInfo(path);
                        if (DateTime.Now.Subtract( fi.LastWriteTime ).TotalDays > 40)
                        {
                            File.Delete(path);
                        }
                    }

                    if (File.Exists(path) == false)
                    {
                        // Download Geolite 2 First
                        var url = "http://geolite.maxmind.com/download/geoip/database/GeoLite2-Country.tar.gz";
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(url, path + ".tar.gz");
                        }

                        using (Stream inStream = File.OpenRead(path + ".tar.gz"))
                        {
                            using (Stream gzipStream = new GZipInputStream(inStream))
                            {
                                TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                                tarArchive.ExtractContents(Path.Combine(ctx.GetRootPath(), "App_Data"));
                                tarArchive.Close();

                                gzipStream.Close();
                            }
                            inStream.Close();
                        }

                        // the database is stored in folder
                        var geoliteDir = Directory.GetDirectories(Path.Combine(ctx.GetRootPath(), "App_Data")).Where(d => d.Contains("GeoLite2-Country")).FirstOrDefault();
                        File.Copy(Path.Combine(geoliteDir, "GeoLite2-Country.mmdb"), path);

                        File.Delete(path + ".tar.gz");
                        Directory.Delete(geoliteDir, true);
                    }

                    _GeoIP = new DatabaseReader(path);

                }

                CountryResponse country;
                if (_GeoIP.TryCountry(ctx.Request.UserHostAddress, out country))
                {
                    ctx.Items["Country"] = country.Country.Name;
                    ctx.Items["CountryISO"] = country.Country.IsoCode.ToLowerInvariant();
                }
                else
                {
                    ctx.Items["Country"] = "Thailand";
                    ctx.Items["CountryISO"] = "th";
                }

                // Gets the language from hostname
                var hostnameParts = ctx.Request.Url.HostName.Split('.');
                if (hostnameParts[0].Length == 2)
                {
                    ctx.Items["Language"] = hostnameParts[0];
                }

                // try setting from the settings
                if (ctx.Items.ContainsKey("Language") == false)
                {
                    var settings = ctx.GetSiteSettings();
                    if (settings.multilanguage != null &&
                        settings.multilanguage.defaultLanguage != null)
                    {
                        ctx.Items["Language"] = settings.multilanguage.defaultLanguage;
                    }
                }

                if (ctx.Items.ContainsKey("Language") == false)
                {
                    ctx.Items["Language"] = string.Empty;
                }

                return null;
            });
        }
    }
}