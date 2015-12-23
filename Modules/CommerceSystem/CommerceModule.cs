using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class CommerceModule : BaseDataModule
    {

        public static event Action<SaleOrder, NancyBlackDatabase> PaymentCompleted = delegate { };

        static CommerceModule()
        {
            // Maps Variation to master
            ContentModule.MapPage += (ctx, content) =>
            {
                if (content is Product)
                {
                    var p = content as Product;
                    if (p.IsVariation)
                    {
                        var master = ctx.GetSiteDatabase().GetById<Product>(p.MasterProductId);
                        if (master == null)
                        {
                            return content;
                        }

                        return master;
                    }
                }

                return content;
            };
        }

        private static bool _Triggered = false;

        public CommerceModule()
        {
            if (_Triggered == false)
            {
                // ensure that we have thank you page
                ContentModule.GetPage(this.SiteDatabase, "/__/commerce/thankyou", true);
                _Triggered = true;
            }

            // testing thankyou page by nancy white
            Get["/__/commerce/thankyou"] = this.HandleViewRequest("commerce-thankyoupage", (arg) =>
            {
                if (this.CurrentUser.HasClaim("admin") == false)
                {
                    return new StandardModel(404);
                }
                
                var page = ContentModule.GetPage(this.SiteDatabase, "/__/commerce/thankyou", true);
                return new StandardModel(this, page, JObject.FromObject( new SaleOrder()
                {
                    SaleOrderIdentifier = "SO20990909-999999",
                }));
            });


            Get["/__commerce/cart"] = this.HandleViewRequest("commerce-shoppingcart", (arg) =>
            {
                return new StandardModel(this, "Checkout");
            });

            //Get["/__commerce/saleorder/{id}/notifytransfer"] = this.HandleViewRequest("commerce-notifytransfer", (args) =>
            //{
            //    var saleorder = this.SiteDatabase.Query("saleorder",
            //        string.Format("uuid eq '{0}'", (string)args.id),
            //        "Id desc").FirstOrDefault();

            //    return new StandardModel(this, content: saleorder);
            //});

            // get the product 
            Get["/__commerce/api/productstructure"] = this.HandleRequest(this.BuildProductStructure);

            // List User's address
            Get["/__commerce/api/addresses"] = this.HandleRequest(this.FindUserAddress);

            // Save User's address
            Post["/__commerce/api/address"] = this.HandleRequest(this.UpdateUserAddress);

            // Save User's cart
            Post["/__commerce/api/checkout"] = this.HandleRequest(this.Checkout);

            Get["/__commerce/saleorder/{so_id}/{form}"] = this.HandleViewRequest("commerce-print", (arg) =>
            {
                var id = (string)arg.so_id;
                var so = this.SiteDatabase.Query<SaleOrder>()
                            .Where(row => row.SaleOrderIdentifier == id)
                            .FirstOrDefault();

                var dummyPage = new Page()
                {
                    Title = arg.form + " for " + so.SaleOrderIdentifier,
                    ContentParts = JObject.FromObject(new
                    {
                        Type = (string)arg.form
                    }),

                };

                return new StandardModel(this, dummyPage, so);
            });

            Patch["/tables/SaleOrder/{id:int}"] = this.HandleRequest(this.HandleSalorderSaveRequest);

            Post["/__commerce/api/resolvevariation"] = this.HandleRequest(this.HandleVariationRequest);

            Get["/__commerce/banner"] = this.HandleRequest(this.HandleBannerRequest);
        }

        private dynamic HandleBannerRequest(dynamic arg)
        {
            var bannerList = this.SiteDatabase.Query<Banner>()                            
                            .Take(5)                           
                            .ToList();

            this.SaveDisplayedBanner(bannerList);

            return bannerList;
        }

        private dynamic SaveDisplayedBanner(dynamic arg)
        {
            foreach(var Banner in arg)
            {
                var Impression = new Impression()
                {
                    BannerId = Banner.Id
                };
                this.SiteDatabase.UpsertRecord("Impression", Impression);
            }
            return true;
        }

        private dynamic HandleVariationRequest(dynamic arg)
        {
            var parameters = arg.body.Value as JObject;

            var masterId = (int)parameters["MasterProductId"];
            var products = this.SiteDatabase.Query<Product>()
                            .Where(p => p.MasterProductId == masterId)
                            .ToList();

            foreach (var p in products)
            {
                var attributes = p.Attributes as JObject;
                var match = attributes.Properties().All(attr => (string)attr.Value == (string)parameters[attr.Name]);

                if (match == true)
                {
                    return p;
                }

            }

            return 404;
        }

        private dynamic HandleSalorderSaveRequest(dynamic arg)
        {
            arg.table_name = "SaleOrder";

            // TODO
            // Create Shipping/Billing Address
            // In case of Shipping/Billing Address never been created.
            return this.HandleInsertUpdateRequest(this.SiteDatabase, arg);
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
            if (saleorder.Customer == null)
            {
                saleorder.Customer = new {
                    Email = this.CurrentUser.Email
                };
            }
            else
            {
                saleorder.Customer.Email = this.CurrentUser.Email; // sets email
            }            

            // Update Total
            saleorder.TotalAmount = 0;

            this.SiteDatabase.Transaction(() =>
            {
                // snapshot the products into this sale order
                // so that if there is a change in product info later
                // we still have the one that customer sees
                saleorder.ItemsDetail = new List<Product>();
                foreach (var item in saleorder.Items)
                {
                    var product = this.SiteDatabase.GetById<Product>(item);
                    saleorder.ItemsDetail.Add(product);

                    saleorder.TotalAmount += product.Price;
                }

                // need to insert to get ID
                this.SiteDatabase.UpsertRecord<SaleOrder>(saleorder);

                saleorder.SaleOrderIdentifier = string.Format(CultureInfo.InvariantCulture,
                        "SO{0:yyyyMMdd}-{1:000000}",
                        saleorder.__createdAt,
                        saleorder.Id);

                // save the SO ID again
                this.SiteDatabase.UpsertRecord<SaleOrder>(saleorder);
            });


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

        public static void HandlePayment(NancyBlackDatabase db, PaymentLog log, string saleOrderIdentifier)
        {
            // ensure only one thread is processing this so
            lock (saleOrderIdentifier)
            {
                // find the sale order
                var so = db.Query<SaleOrder>()
                            .Where(row => row.SaleOrderIdentifier == saleOrderIdentifier)
                            .FirstOrDefault();

                // Since before 02 Dec, 2015. Paysbuy had been sending SO with prefix 00.              
                if ( so == null)
                {
                    string soWithoutDoubleZeroPrefix = saleOrderIdentifier.Substring(2);
                    so = db.Query<SaleOrder>()
                            .Where(row => row.SaleOrderIdentifier == soWithoutDoubleZeroPrefix)
                            .FirstOrDefault();
                }

                log.SaleOrderId = so.Id;

                JArray exceptions = new JArray();

                // Wrong Payment Status, Exit now.
                if (so.Status != SaleOrderStatus.WaitingForPayment)
                {
                    so.Status = SaleOrderStatus.DuplicatePayment;
                    exceptions.Add(JObject.FromObject(new
                    {
                        type = "Wrong Status",
                        description = string.Format(
                            "Current status of SO is: {0}", so.Status)
                    }));

                }

                if (log.Amount != so.TotalAmount)
                {
                    so.Status = SaleOrderStatus.PaymentReceivedWithException;
                    exceptions.Add(JObject.FromObject(new
                    {
                        type = "Wrong Amount",
                        description = string.Format(
                            "Expects: {0} amount from SO, payment is {1}", so.TotalAmount, log.Amount)
                    }));
                }

                if (exceptions.Count == 0)
                {
                    so.Status = SaleOrderStatus.PaymentReceived;
                    so.ReceiptIdentifier = string.Format(CultureInfo.InvariantCulture,
                        "RC{0:yyyyMMdd}-{1:000000}", so.__createdAt, so.Id);
                }

                log.Exception = exceptions;
                db.UpsertRecord<PaymentLog>(log);

                db.UpsertRecord<SaleOrder>(so);

                CommerceModule.PaymentCompleted(so, db);
            }

        }

        public static void SetPackingStatus(NancyBlackDatabase db, SaleOrder so)
        {
            // deduct stock of all product
            // first, we group the product id to minimize selects
            db.Transaction(() =>
            {
                var products = from item in so.Items
                               group item by item into g
                               select g;

                foreach (var productIdGroup in products)
                {
                    var product = db.GetById<Product>(productIdGroup.Key);
                    product.Stock = product.Stock - productIdGroup.Count();
                    db.UpsertRecord<Product>(product);
                }
            });
        }
    }
}