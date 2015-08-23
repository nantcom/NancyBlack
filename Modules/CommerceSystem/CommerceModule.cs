using NantCom.NancyBlack.Modules.CommerceSystem.types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class CommerceModule : BaseModule
    {

        public CommerceModule()
        {
            Get["/__commerce/cart"] = this.HandleViewRequest("commerce-shoppingcart");

            Get["/__commerce/saleorder/{id}/notifytransfer"] = this.HandleViewRequest("commerce-notifytransfer", (args) =>
            {
                var saleorder = this.SiteDatabase.Query("saleorder",
                    string.Format("uuid eq '{0}'", (string)args.id),
                    "Id desc").FirstOrDefault();

                return new StandardModel( this, content: saleorder);
            });

            // get the product 
            Get["/__commerce/api/productstructure"] = this.HandleRequest( this.BuildProductStructure );

            // List User's address
            Get["/__commerce/api/addresses"] = this.HandleRequest(this.FindUserAddress);

            // Save User's address
            Post["/__commerce/api/address"] = this.HandleRequest(this.UpdateUserAddress);
            
            // Save User's cart
            Post["/__commerce/api/checkout"] = this.HandleRequest(this.Checkout);


            // Disable Payment Log table access
            Get["/tables/PaymentLog"] = this.HandleStatusCodeRequest(404);
            Get["/tables/PaymentLog/count"] = this.HandleStatusCodeRequest(404);
            Get["/tables/PaymentLog/{item_id:int}"] = this.HandleStatusCodeRequest(404);
            Post["/tables/PaymentLog"] = this.HandleStatusCodeRequest(404);
            Patch["/tables/PaymentLog/{item_id:int}"] = this.HandleStatusCodeRequest(404);
            Delete["/tables/PaymentLog/{item_id:int}"] = this.HandleStatusCodeRequest(404);

            // Disable Sale Order Table Access
            Get["/tables/SaleOrder"] = this.HandleStatusCodeRequest(404);
            Get["/tables/SaleOrder/count"] = this.HandleStatusCodeRequest(404);
            Get["/tables/SaleOrder/{item_id:int}"] = this.HandleStatusCodeRequest(404);
            Post["/tables/SaleOrder"] = this.HandleStatusCodeRequest(404);
            Patch["/tables/SaleOrder/{item_id:int}"] = this.HandleStatusCodeRequest(404);
            Delete["/tables/SaleOrder/{item_id:int}"] = this.HandleStatusCodeRequest(404);
        }

        private dynamic Checkout(dynamic arg)
        {
            if (this.CurrentUser.IsAnonymous)
            {
                return 400;
            }

            var saleorder = ((JObject)arg.body.Value).ToObject<SaleOrder>();

            saleorder.NcbUserId = this.CurrentUser.Id;
            saleorder.Status = "WaitingForPayment";
            saleorder.Customer = this.CurrentUser.Profile;

            // Update Total
            saleorder.TotalAmount = 0;
            foreach (var item in saleorder.Items)
            {
                var product = this.SiteDatabase.GetById<Product>(item);
                saleorder.TotalAmount += product.Price;
            }

            this.SiteDatabase.UpsertRecord<SaleOrder>(saleorder);

            return saleorder;
        }

        /// <summary>
        /// Update User Address
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private dynamic UpdateUserAddress(dynamic arg)
        {
            if (this.CurrentUser.IsAnonymous)
            {
                return 400;
            }

            var input = (arg.body.Value as JArray);
            if (input == null)
            {
                return 400;
            }

            var existing = this.SiteDatabase.Query<NcgAddress>()
                            .Where(a => a.NcbUserId == this.CurrentUser.Id)
                            .ToList();

            foreach (var item in input)
            {

                var address = item.ToObject<NcgAddress>();
                if (address == null)
                {
                    continue;
                }

                address.NcbUserId = this.CurrentUser.Id;

                var isExisted = existing.Any(a => a.Equals(address));
                if (isExisted == false)
                {
                    address.Id = 0;
                    this.SiteDatabase.UpsertRecord<NcgAddress>(address);

                    existing.Add(address);
                }
            }

            return existing;
        }

        private dynamic FindUserAddress(dynamic arg)
        {
            if (this.CurrentUser.IsAnonymous)
            {
                return 400;
            }

            return this.SiteDatabase.Query<NcgAddress>()
                    .Where(a => a.NcbUserId == this.CurrentUser.Id)
                    .AsEnumerable();
        }

        /// <summary>
        /// Node Class, for product structure view
        /// </summary>
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

        private dynamic BuildProductStructure(dynamic arg)
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
                var parts = item.Split('\\').ToList();
                for (int i = 1; i < parts.Count; i++)
                {
                    var parentPath = string.Join("\\", parts.Take(i));
                    var fullPath = string.Join("\\", parts.Take(i + 1));

                    if (parentPath == "")
                    {
                        parentPath = "Products";
                    }

                    if (tree.ContainsKey(parentPath) == false)
                    {
                        tree[parentPath] = new Node()
                        {
                            id = id++,
                            title = Path.GetFileName(parentPath),
                            fullPath = parentPath.Replace('\\', '/'),
                        };
                    }

                    if (tree.ContainsKey(fullPath) == false)
                    {
                        var node = new Node()
                        {
                            id = id++,
                            title = Path.GetFileName(fullPath),
                            fullPath = fullPath.Replace('\\', '/'),
                        };

                        tree[fullPath] = node;
                        tree[parentPath].nodes.Add(node);
                    }
                }
            }


            return tree.Values.FirstOrDefault();
        }
    }
}