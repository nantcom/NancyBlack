using HtmlAgilityPack;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Site.Modules.Types;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NantCom.NancyBlack.Site.Modules
{
    public class LoadDataFromOriginWeb : BaseDataModule
    {
        private static string hostUrl = "http://www.whereonsale.com";
        private RestClient client = new RestClient(hostUrl);

        public LoadDataFromOriginWeb()
        {
            // intercept all table requests /get/whereonsale/all
            Get["/get/whereonsale/all"] = this.HandleRequest(this.HandleGetOriginDataPerPage);
        }

        private int existCount;
        
        /// <summary>
        /// get data per record(promotion or blog) to store in our database
        /// </summary>
        /// <param name="part">url part</param>
        /// <returns></returns>
        private void SaveContentFromOriginData(string part)
        {
            var request = new RestRequest(part, Method.GET);
reload_checkpoint:
            IRestResponse response = client.Execute(request);

            var content = new WhereOnSaleContent();

            // load doc
            var doc = new HtmlDocument();
            doc.LoadHtml(response.Content);

            // record title
            var titleNode = doc.QuerySelector(".active > h1 > a");

            // server's crash happen so restart checkpoint
            if (titleNode == null)
            {
                System.Threading.Thread.Sleep(2500);
                goto reload_checkpoint;
            }
            content.Title = HttpUtility.HtmlDecode(titleNode.InnerText);
            content.Url = part;

            // if there is exist record then return from funciton
            var existRecord = this.SiteDatabase.Query<WhereOnSaleContent>().Where(rec => rec.Url == content.Url).FirstOrDefault();
            if (existRecord != null)
            {
                existCount++;
                return;
            }
            
            // save once for get id
            content = this.SiteDatabase.UpsertRecord(content);

            // record cat
            var cats = doc.QuerySelectorAll(".date > a");
            content.CatsId = new int[cats.Count];
            var index = 0;
            foreach (var cat in cats)
            {
                var catName = HttpUtility.HtmlDecode(cat.InnerText);
                var existCat = this.SiteDatabase.Query<Categories>().Where(rec => rec.Title == catName).FirstOrDefault();
                if (existCat == null)
                {
                    existCat = new Categories()
                    {
                        Title = catName,
                        IsShow = false,
                        Part = getPart(cat.GetAttributeValue("href", "null"))
                    };
                    existCat = this.SiteDatabase.UpsertRecord(existCat);
                }
                content.CatsId[index] = existCat.Id;
                index++;
            }

            // set created date
            var createdDate = doc.QuerySelector("span.time");
            content.__createdAt = DateTime.ParseExact(createdDate.InnerText, " — dd MMMM yyyy", CultureInfo.InvariantCulture);

            //filter only for content node
            var contentNodes = doc.QuerySelector(".content").GetChildElements().ToList();
            contentNodes.RemoveRange(contentNodes.Count - 4, 4);
            contentNodes.RemoveRange(0, 4);

            //remove gallery from content node
            if (contentNodes.Count > 0 && contentNodes.LastOrDefault().QuerySelector("img.size-full") != null)
            {
                contentNodes.RemoveRange(contentNodes.Count - 1, 1);
            }

            //set ContentParts
            var strBuilder = new StringBuilder();
            foreach (var node in contentNodes)
            {
                strBuilder.Append(HttpUtility.HtmlDecode(node.OuterHtml));
            }
            content.ContentParts = new JObject();
            content.ContentParts["FullDescription"] = strBuilder.ToString();

            //get images to save in attachments
            var images = doc.QuerySelectorAll(".content > p > a > .alignnone.size-full");
            var files = new List<Nancy.HttpFile>();
            foreach (var image in images)
            {
                try
                {
                    var imageUrl = image.GetAttributeValue("src", null);
                    var webClient = new WebClient();
                    byte[] imageBytes = webClient.DownloadData(imageUrl);
                    if (files.Count == 0)
                    {
                        files.Add(new Nancy.HttpFile("default", Path.GetFileName(imageUrl), new MemoryStream(imageBytes), ""));
                    }
                    else
                    {
                        files.Add(new Nancy.HttpFile("gallery", Path.GetFileName(imageUrl), new MemoryStream(imageBytes), ""));
                    }
                }
                catch (Exception)
                {
                }
            }
            this.saveContentAttachmentImage(content, files);

            this.SiteDatabase.UpsertRecord(content);
        }

        private void saveContentAttachmentImage(WhereOnSaleContent content, List<Nancy.HttpFile> files)
        {
            var newFiles = new List<dynamic>();
            foreach (var image in files)
            {
                var path = this.GetAttachmentFolder(content.TableName, content.Id.ToString());
                var fileName = Path.GetFileName(image.Name);
                var filePath = Path.Combine(path, fileName);
                if (File.Exists(filePath))
                {
                    fileName = Path.GetFileNameWithoutExtension(image.Name) +
                        Guid.NewGuid() +
                        Path.GetExtension(image.Name);

                    filePath = Path.Combine(path, fileName);
                }

                using (var fs = File.Create(filePath))
                {
                    image.Value.CopyTo(fs);
                    newFiles.Add(new
                    {
                        CreateDate = DateTime.Now,
                        AttachmentType = image.ContentType,
                        DisplayOrder = 0,
                        Caption = string.Empty,
                        Url =
                            Path.Combine(
                                "/Site",
                                "attachments",
                                content.TableName,
                                content.Id.ToString(),
                                fileName).Replace('\\', '/')
                    });
                }
            }
            content.Attachments = newFiles.ToArray();
        }

        private string getPart(string input)
        {
            var uri = new Uri(input);
            return HttpUtility.HtmlDecode(uri.AbsolutePath);
        }

        /// <summary>
        /// Handle Request get categories
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private void UpdateCategories()
        {
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            // load doc
            var doc = new HtmlDocument();
            doc.LoadHtml(response.Content);

            //get all categories node
            var nodes = doc.QuerySelectorAll("#categories > ul > li > a, #categories > ul > li > ul > li > a, #categories > ul > li > ul > li > ul > li > a");
            foreach (var node in nodes)
            {
                var cate = new Categories()
                {
                    Title = HttpUtility.HtmlDecode(node.InnerText),
                    Part = getPart(node.GetAttributeValue("href", "null")),
                    IsShow = true
                };

                // if none exist, insert
                var existCat = this.SiteDatabase.Query<Categories>().Where(rec => rec.Title == cate.Title && rec.Part == cate.Part).FirstOrDefault();
                if (existCat == null)
                {
                    this.SiteDatabase.UpsertRecord(cate);
                }
            }
        }

        /// <summary>
        /// Handle Request get origin data per page
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic HandleGetOriginDataPerPage(dynamic arg)
        {
            //this.UpdateCategories();

            var mainCats = this.SiteDatabase.Query<Categories>().Skip(0).Take(9).ToList();
            var tasks = new List<Task>();
            foreach (var cat in mainCats)
            {
                var request = new RestRequest(cat.Part, Method.GET);
                IRestResponse response = client.Execute(request);

                // load doc
                var doc = new HtmlDocument();
                doc.LoadHtml(response.Content);

                // load data in one page (doc) and then go to next one (previous post becoz sort by new to old)
                var previousButton = doc.QuerySelector(".old_entries > a");
                while (true)
                {
                    // get all 'a' node for get detail's link
                    var contents = doc.QuerySelectorAll("#list_categories > .listing > .content > .left > .imgholder > a");
                    foreach (var content in contents)
                    {
                        var part = getPart(content.GetAttributeValue("href", null));
                        tasks.Add(Task.Run(() =>
                        {
                            this.SaveContentFromOriginData(part);
                        }));
                    }

                    Task.WhenAll(tasks);
                    tasks.Clear();

                    previousButton = doc.QuerySelector(".old_entries > a");
                    if (previousButton == null || existCount > 15)
                    {
                        //got all data from this category
                        existCount = 0;
                        break;
                    }

                    var nextPart = getPart(previousButton.GetAttributeValue("href", null));
                    
                    // load next page
                    request = new RestRequest(nextPart, Method.GET);
                    response = client.Execute(request);

                    // load doc
                    doc = new HtmlDocument();
                    doc.LoadHtml(response.Content);
                }
            }


            return 200;
        }
    }
}