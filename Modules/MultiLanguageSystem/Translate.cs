using HtmlAgilityPack;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using RestSharp;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MultiLanguageSystem
{
    public class TranslateHelper
    {
        private static NancyBlackDatabase _Database;
        private static Dictionary<string, string> _Translations;

        private static void Initialize()
        {
            if (_Database != null || _Translations != null)
            {
                return;
            }

            lock (BaseModule.GetLockObject("TranslateHelper-Initialize"))
            {
                if (_Database != null || _Translations != null)
                {
                    return;
                }

                var database = Path.Combine(BootStrapper.RootPath, "Site", "data.sqlite");
                _Database = new NancyBlackDatabase(new SQLiteConnection(database, true));
                _Translations = new Dictionary<string, string>();

                foreach (var item in _Database.Query<TranslateEntry>().AsEnumerable())
                {
                    _Translations[item.Language + "-" + item.Primary.Trim()] = item.Translated;
                }
            }

        }
        
        /// <summary>
        /// Translate the input to given locale
        /// </summary>
        /// <param name="input"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public string Translate(string input, string language, string defaultTranslation = null, bool useMachineTranslation = true)
        {
            TranslateHelper.Initialize();

            // primary language - is no translation
            if (string.IsNullOrEmpty(language))
            {
                return input;
            }


            var key = language + "-" + input;
            string translated;
            if (_Translations.TryGetValue(key, out translated) == false)
            {
                if (string.IsNullOrEmpty(defaultTranslation))
                {
                    if (useMachineTranslation)
                    {
                        try
                        {

                        }
                        catch (Exception)
                        {
                            return input;
                        }
                    }
                }

                lock (BaseModule.GetLockObject("Translate-" + key))
                {
                    // when other threads unlocked - we have to check again
                    if (_Translations.ContainsKey(key))
                    {
                        return _Translations[key];
                    }

                    _Translations[key] = defaultTranslation;
                    _Database.UpsertRecord<TranslateEntry>(new TranslateEntry()
                    {
                        Primary = input,
                        Language = language,
                        Translated = defaultTranslation
                    });

                    return defaultTranslation;
                }
            }

            return translated;
        }

        /// <summary>
        /// This method breaks input HTML and send request for each tag
        /// </summary>
        /// <param name="htmlInput"></param>
        /// <returns></returns>
        public string HtmlMachineTranslate(string htmlInput, string translatedLanguage, string primaryLanguage = null)
        {
            TranslateHelper.Initialize();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlInput);

            Action<HtmlNode> drill = null;
            drill = (node) =>
            {
                foreach (var child in node.ChildNodes)
                {
                    if (child.NodeType == HtmlNodeType.Text)
                    {
                        var text = child.InnerText;
                        try
                        {
                            var translated = this.MachineTranslate(text, translatedLanguage, primaryLanguage);
                            child.InnerHtml = translated;
                        }
                        catch (Exception)
                        {
                        }
                    }

                    drill(child);
                }
            };

            drill(doc.DocumentNode);

            return doc.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// Performs Machine translation using Microsoft Translator API
        /// </summary>
        /// <param name="input"></param>
        /// <param name="primaryLanguage"></param>
        /// <param name="translatedLanguage"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public string MachineTranslate(string input, string translatedLanguage, string primaryLanguage = null)
        {
            TranslateHelper.Initialize();

            if (input.Length > 10000)
            {
                throw new InvalidOperationException("Input is too long for machine translation");
            }

            if (string.IsNullOrEmpty( input.Trim() ))
            {
                return string.Empty;
            }

            var key = translatedLanguage + "-" + input.Trim();
            string translated;
            if (_Translations.TryGetValue(key, out translated))
            {
                return translated;
            }

            lock (BaseModule.GetLockObject("Translate-" + key))
            {

                // when other threads unlocked - we have to check again
                if (_Translations.ContainsKey(key))
                {
                    return _Translations[key];
                }
                var siteSettings = AdminModule.ReadSiteSettings();
                var translate = siteSettings.translate;

                if (translate == null || translate.key == null)
                {
                    throw new InvalidOperationException("Machine Translation require 'key' in translate object of site settings");
                }

                RestClient client = new RestClient("https://api.microsofttranslator.com");
                RestRequest req = new RestRequest("/v2/Http.svc/Translate");
                req.Method = Method.GET;
                req.AddHeader("Ocp-Apim-Subscription-Key", (string)translate.key);
                req.AddQueryParameter("text", input);
                req.AddQueryParameter("to", translatedLanguage);

                if (string.IsNullOrEmpty(primaryLanguage) == false)
                {
                    req.AddQueryParameter("from", primaryLanguage);
                }

                var result = client.Execute(req);
                var element = System.Xml.Linq.XElement.Parse(result.Content);
                translated = element.Value;

                _Translations[key] = translated;
                _Database.UpsertRecord<TranslateEntry>(new TranslateEntry()
                {
                    Primary = input,
                    Language = translatedLanguage,
                    Translated = translated
                });

                return translated;
            }
        }
    }

    public class TranslateModule : NancyBlack.Modules.BaseModule
    {
        public TranslateModule()
        {
            Post["/__translate/updatetranslations"] = (arg) =>
            {
                // Will do Web based UI later
                return 400;
            };
        }
    }

    public class TranslateEntry : IStaticType
    {
        /// <summary>
        /// Primary string to translate
        /// </summary>
        public string Primary { get; set; }

        /// <summary>
        /// The locale of this translated string
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// The translated string
        /// </summary>
        public string Translated { get; set; }

        /// <summary>
        /// Last usage time of this entry, for tracking unused entry - currently not used
        /// </summary>
        public DateTime LastUsage { get; set; }

        #region IStaticType

        public int Id { get; set; }
        public DateTime __createdAt { get; set; }
        public DateTime __updatedAt { get; set; }

        #endregion
    }
}