using NantCom.NancyBlack.Modules.CommerceSystem.types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class CommerceModule : BaseModule
    {
        public class Node
        {
            public int id { get; set; }

            public string title { get; set; }

            public string fullPath { get; set; }

            public List<Node> nodes { get; set; }

            public Node()
            {
                this.nodes = new List<Node>();
            }
        }

        public CommerceModule()
        {
            Get["/__commerce/cart"] = this.HandleRequest((args) =>
            {
                return View["commerce-shoppingcart", this.GetModel()];

            });

            Get["/__commerce/saleorder/{id}/notifytransfer"] = this.HandleRequest((args) =>
            {
                var saleorder = this.SiteDatabase.Query("saleorder",
                    string.Format("uuid eq '{0}'", (string)args.id),
                    "Id desc").FirstOrDefault();

                return View["commerce-notifytransfer", this.GetModel(saleorder)];

            });

            Post["/__commerce/paymentlog/paysbuy"] = this.HandleRequest((args) =>
            {

                var FormData = this.Request.Form;

                string Response = string.Empty;

                foreach (var Key in FormData.Keys)
                {
                    var Value = FormData[Key].ToString();
                    Response += string.Concat(Key.ToString(), ":", Value.ToString(), "|");
                }

                PaymentLogPaysbuy PaymentLog = new PaymentLogPaysbuy()
                {
                    Response = Response
                };

                try
                {
                    this.SiteDatabase.UpsertRecord("paymentlogpaysbuy", PaymentLog);
                }
                catch (Exception e)
                {
                    throw e;
                }

                return 201;

            });

            // get the product 
            Get["/__commerce/api/productstructure"] = this.HandleRequest((arg) =>
            {
                var products = this.SiteDatabase.Query<Product>().AsEnumerable();
                Func<string, string> getUrlWithoutLeaf = (s) =>
                {
                    return Path.GetDirectoryName(s);
                };

                var baseUrls = (from p in products
                                let url = getUrlWithoutLeaf(p.Url)
                                where url != null
                                select url).Distinct().ToList();

                Dictionary<string, Node> tree = new Dictionary<string, Node>();
                int id = 0;
                foreach (var item in baseUrls)
                {
                    var parts = item.Split('\\');
                    var fullPath = "";
                    foreach (var part in parts.Skip(1).Take( parts.Length - 2 ))
                    {
                        fullPath = fullPath + "\\" + part;
                        if (tree.ContainsKey(fullPath) == false)
                        {
                            tree[fullPath] = new Node()
                            {
                                id = ++id,
                                title = part,
                                fullPath = fullPath.Replace('\\', '/'),
                            };
                        }
                    }

                    var parentDirectory = Path.GetDirectoryName(item);
                    var leafDirectory = Path.GetFileName(item);
                    
                    tree[parentDirectory].nodes.Add(new Node()
                    {
                        id = ++id,
                        title = leafDirectory,
                        fullPath = item.Replace('\\', '/')
                    });
                }


                return tree.Values.FirstOrDefault();

            });

        }
    }
}