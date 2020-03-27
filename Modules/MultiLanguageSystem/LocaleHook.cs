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
using System.Text.RegularExpressions;
using Nancy;
using Newtonsoft.Json.Linq;
using Nancy.Responses;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using Nancy.TinyIoc;
using NantCom.NancyBlack.Modules.CommerceSystem;

namespace NantCom.NancyBlack
{

    public sealed partial class ContextItems
    {
        public const string Language = "Language";
        public const string Currency = "Currency";
        public const string ChosenLanguage = "ChosenLanguage";
        public const string ChosenCurrency = "ChosenCurrency";
        public const string Country = "Country";
        public const string CountryISO = "CountryISO";
    }
}

    namespace NantCom.NancyBlack.Modules.MultiLanguageSystem
{
    public class LocaleHook : IPipelineHook, IRequireGlobalInitialize
    {
        private static DatabaseReader _GeoIP;
        
        /// <summary>
        /// Initializes the LocaleHook system
        /// </summary>
        /// <param name="ctx"></param>
        public void GlobalInitialize(NancyContext ctx)
        {
            var settings = ctx.GetSiteSettings();

            if (settings.commerce.multicurrency == null)
            {
                throw new InvalidOperationException("Multi Currency configuration error");
            }
            if (settings.commerce.multicurrency.home == null)
            {
                throw new InvalidOperationException("Multi Currency configuration error, 'commerce.multicurrency.home' not specified");
            }
            if (settings.commerce.multicurrency.mapping == null)
            {
                throw new InvalidOperationException("Multi Currency configuration error, 'commerce.multicurrency.mapping' not specified");
            }
            if (settings.commerce.multicurrency.available == null)
            {
                throw new InvalidOperationException("Multi Currency configuration error, 'commerce.multicurrency.available' not specified");
            }

            if (GlobalVar.Default["MIGRATED-PRICE"] != "MG4")
            {
                // Migrate prices to multi-currency format
                var available = settings.commerce.multicurrency.available as JArray;
                var home = (string)settings.commerce.multicurrency.home;

                var db = ctx.Items["SiteDatabase"] as NancyBlackDatabase;
                db.Connection.RunInTransaction(() =>
                {
                    var products = db.Query<Product>().ToList();

                    foreach (var p in products)
                    {
                        if (p.PriceMultiCurrency == null)
                        {
                            p.PriceMultiCurrency = new JObject();
                        }

                        p.PriceMultiCurrency[home] = p.Price;
                        p.PriceMultiCurrency["BEFORE_MIGRATE"] = p.Price;

                        foreach (string item in available)
                        {
                            if (item != home)
                            {
                                JObject rate = CommerceAdminModule.ExchangeRate;
                                decimal want = (decimal)rate.Property(item).Value;
                                decimal homeRate = (decimal)rate.Property(home).Value;
                                var conversionRate = want / homeRate;

                                p.PriceMultiCurrency[item] = Math.Round(conversionRate * p.Price);
                            }
                        }

                        if (p.DiscountPriceMultiCurrency == null)
                        {
                            p.DiscountPriceMultiCurrency = new JObject();
                        }

                        p.DiscountPriceMultiCurrency[home] = p.DiscountPrice;
                        p.DiscountPriceMultiCurrency["BEFORE_MIGRATE_DISCOUNT"] = p.DiscountPrice;

                        foreach (string item in available)
                        {
                            if (item != home)
                            {
                                JObject rate = CommerceAdminModule.ExchangeRate;
                                decimal want = (decimal)rate.Property(item).Value;
                                decimal homeRate = (decimal)rate.Property(home).Value;
                                var conversionRate = want / homeRate;

                                p.DiscountPriceMultiCurrency[item] = Math.Round(conversionRate * p.DiscountPrice);
                            }
                        }

                        db.Connection.Update(p);
                    }

                    GlobalVar.Default["MIGRATED-PRICE"] = "MG4";
                    GlobalVar.Default.Persist(db);
                });
            }

            var path = Path.Combine(ctx.GetRootPath(), "App_Data", "GeoLite2-Country.mmdb");
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                if (DateTime.Now.Subtract(fi.LastWriteTime).TotalDays > 40)
                {
                    File.Delete(path);
                }
            }

            if (File.Exists(path) == false && ctx.GetSiteSettings().maxmind != null)
            {
                // Download Geolite 2 First
                var url = "https://download.maxmind.com/app/geoip_download?edition_id=GeoLite2-Country&suffix=tar.gz&license_key=" + ctx.GetSiteSettings().maxmind.LicenseKey;
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


                _GeoIP = new DatabaseReader(path);
            }

            //register the post processor last so it wont interfere with data migration
            NancyBlackDatabase.ObjectPostProcessors[typeof(Product)] = (db, obj) =>
            {
                var p = obj as Product;

                if (db.CurrentContext != null &&
                    db.CurrentContext.Items.ContainsKey("Currency"))
                {
                    var currency = (string)db.CurrentContext.Items["Currency"];
                    var priceLocal = p.PriceMultiCurrency[currency] ?? 0M;
                    var discountPriceLocal = p.DiscountPriceMultiCurrency[currency] ?? 0M;

                    p.Price = priceLocal;
                    p.DiscountPrice = discountPriceLocal;
                }

            };

        }

        /// <summary>
        /// Hook into pipeline for locale processing
        /// </summary>
        /// <param name="p"></param>
        public void Hook(IPipelines p)
        {

            p.BeforeRequest.AddItemToStartOfPipeline((ctx) =>
            {
                // Dont need to process locale
                if (ctx.Request.Headers.Accept.Any(a => a.Item1 == "text/html" || a.Item1 == "text/plain" || a.Item1 == "application/json") == false)
                {
                    return null;
                }

                var settings = ctx.GetSiteSettings();


                if (_GeoIP != null)
                {
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
                    ctx.Items["Language"] = (string)settings.multilanguage.defaultLanguage;
                }

                if (ctx.Items.ContainsKey("Language") == false)
                {
                    ctx.Items["Language"] = string.Empty;
                }
                else
                {
                    if (ctx.Request.Query.chosen != null)
                    {
                        ctx.Items["ChosenLanguage"] = ctx.Items["Language"];

                        if (ctx.Request.Cookies.ContainsKey("ChosenLanguage"))
                        {
                            ctx.Request.Cookies["ChosenLanguage"] = (string)ctx.Items["Language"];
                        }
                    }
                }

                if (ctx.Request.Query.currency != null)
                {
                    ctx.Items["Currency"] = (string)ctx.Request.Query.currency;
                    ctx.Items["ChosenCurrency"] = ctx.Items["Currency"];
                }

                if (ctx.Items.ContainsKey("Currency") == false)
                {
                    ctx.Items["Currency"] = (string)settings.commerce.multicurrency.home;

                    if ((string)ctx.Items["Language"] != string.Empty &&
                        settings.commerce.multicurrency.mapping[ctx.Items["Language"]] != null)
                    {
                        ctx.Items["Currency"] = (string)settings.commerce.multicurrency.mapping[ctx.Items["Language"]];
                    }
                }

                var isCurrencyAvailable = (settings.commerce.multicurrency.available as JArray).ToArray().Where( cur => (string)cur == (string)ctx.Items["Currency"]).Count() > 0;
                if (isCurrencyAvailable == false)
                {
                    ctx.Items["Currency"] = (string)settings.commerce.multicurrency.home;
                    if (ctx.Items.ContainsKey("ChosenCurrency"))
                    {
                        ctx.Items["ChosenCurrency"] = ctx.Items["Currency"];
                    }
                }

                if (ctx.Request.Cookies.ContainsKey("ChosenLanguage") &&
                    ctx.Request.Url.HostName.StartsWith(ctx.Request.Cookies["ChosenLanguage"]) == false)
                {
                    var domainParts = ctx.Request.Url.HostName.Split('.');
                    var newDomain = string.Empty;

                    if (domainParts[0].Length == 2)
                    {
                        // change first part
                        newDomain = ctx.Request.Cookies["ChosenLanguage"] + "." + string.Join(".", domainParts.Skip(1));
                    }
                    else
                    {
                        newDomain = ctx.Request.Cookies["ChosenLanguage"] + "." + ctx.Request.Url.HostName;
                    }


                    return new RedirectResponse(ctx.Request.Url.ToString().Replace( ctx.Request.Url.HostName, newDomain ), RedirectResponse.RedirectType.SeeOther);
                }

                return null;
            });

            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                var domainParts = ctx.Request.Url.HostName.Split('.');
                var cookieDomain = ctx.Request.Url.HostName;

                if (domainParts[0].Length == 2) // sub domain for language
                {
                    cookieDomain = "." + string.Join(".", domainParts.Skip(1));
                }


                if (ctx.Items.ContainsKey("ChosenLanguage"))
                {
                    ctx.Response.WithCookie("ChosenLanguage", (string)ctx.Items["ChosenLanguage"], DateTime.Now.AddYears(1), cookieDomain, null);
                }

                if (ctx.Items.ContainsKey("ChosenCurrency"))
                {
                    ctx.Response.WithCookie("ChosenCurrency", (string)ctx.Items["ChosenCurrency"], DateTime.Now.AddYears(1), cookieDomain, null);
                }


            });
        }
    }
}