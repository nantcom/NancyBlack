using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class Item : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public int ProductId { get; set; }

        public string Serial { get; set; }

        public string ProductTitle { get; set; }

        public string SupplierProductTitle { get; set; }

        public List<int> PartsId { get; set; }

        public bool WasOrdered { get; set; }

        public bool WasReceived { get; set; }

        public bool WasWithdrawn { get; set; }

        public int SaleOrderId { get; set; }

        public int PurchaseOrderId { get; set; }

        public string Note { get; set; }
    }

    public static class ItemManager
    {
        private static string GetSupplierProductTitle(Product product)
        {
            var attr = ((JObject)product.Attributes);
            string title;
            if (attr != null && attr.Value<JObject>("supplier") != null && attr.Value<JObject>("supplier").Value<string>("part") != null)
            {
                title = attr.Value<JObject>("supplier").Value<string>("part");
            }
            else
            {
                title = product.Title;
            }

            return title;
        }

        private static string GenerateSupplierLaptopTitle(Product product, List<Product> items)
        {
            if (product.Url.Contains("/laptops/"))
            {
                return GetSupplierProductTitle(product) + " " + string.Join(" ", from item in items
                                where
                                (item.Attributes == null || item.Attributes.supplierinfo.Id == 0) &&
                                (item.Url.Contains("/cpu/") || item.Url.Contains("/gpu/") || item.Url.Contains("/monitor/"))
                                select GetSupplierProductTitle(item));

            }
            return GetSupplierProductTitle(product);
        }

        private static string GenerateProductTitle(Product product, List<Product> items)
        {
            if (product.Url.Contains("/laptops/"))
            {
                return product.Title + " " + string.Join(" ", from item in items
                                where
                                (item.Attributes == null || item.Attributes.supplierinfo.Id == 0) &&
                                (item.Url.Contains("/cpu/") || item.Url.Contains("/gpu/") || item.Url.Contains("/monitor/"))
                                select item.Title);

            }
            return product.Title;
        }

        private static List<int> GeneratePartsIdList(Product product, List<Product> items)
        {
            if (product.Url.Contains("/laptops/"))
            {
                return (from item in items
                        where
                          (item.Attributes == null || item.Attributes.supplierinfo.Id == 0) &&
                         (item.Url.Contains("/cpu/") || item.Url.Contains("/gpu/") || item.Url.Contains("/monitor/"))
                        select item.Id).ToList();

            }
            return null;
        }

        private static Object thisLock = new object();

        public static void SaveItems(SaleOrder saleOrder, NancyBlackDatabase db)
        {
            // check if already exist
            if (db.Query<Item>().Where(item => item.SaleOrderId == saleOrder.Id).FirstOrDefault() != null)
            {
                return;
            }

            // get products from SO with SupplierInfo
            var products = (from itemId in saleOrder.Items
                         let itemInDb = db.GetById<Product>(itemId)
                         let item = itemInDb == null ? saleOrder.ItemsDetail.Where(p => p.Id == itemId).FirstOrDefault() : itemInDb
                         where item.Attributes != null
                         let isSupplierNull = item.Attributes.supplier == null
                         let deserialize = (item.Attributes.supplier = isSupplierNull ? JObject.Parse("{}") : JObject.Parse(item.Attributes.supplier.ToString()))
                         let setSupplier = (item.Attributes.supplierinfo = item.Attributes.supplier.id == null ? JObject.FromObject(new Supplier()) : JObject.FromObject(db.GetById<Supplier>((int)item.Attributes.supplier.id)))
                         select item).ToList();

            // These groups are a list of Item which grouped by SupplierId 
            var groups = from product in products
                        where product.Attributes.supplierinfo.Id != 0 // only product that has supplier
                        let item = new Item()
                        {
                            ProductTitle = GenerateProductTitle(product, products),
                            SupplierProductTitle = GenerateSupplierLaptopTitle(product, products),
                            ProductId = product.Id,
                            PartsId = GeneratePartsIdList(product, products),
                            SaleOrderId = saleOrder.Id
                        }
                        group item by product.Attributes.supplierinfo.Id // group product by supplier
                        into itemWithSameSupplier
                        select itemWithSameSupplier;

            lock (thisLock)
            {
                // add item to po
                foreach (var group in groups)
                {
                    var supplierId = (int)group.Key;
                    var purchaseOrder = db.Query<PurchaseOrder>()
                        .Where(po => po.HasPaid == false && po.SupplierId == supplierId && po.Status == PurchaseOrderStatus.New).FirstOrDefault();

                    // create new po when there is no exist po
                    if (purchaseOrder == null)
                    {
                        purchaseOrder = new PurchaseOrder()
                        {
                            IsCancel = false,
                            HasPaid = false,
                            SupplierId = supplierId
                        };
                        db.UpsertRecord(purchaseOrder);
                        purchaseOrder.Generate();
                        db.UpsertRecord(purchaseOrder);
                    }

                    foreach (var item in group)
                    {
                        item.PurchaseOrderId = purchaseOrder.Id;
                        db.UpsertRecord(item);
                    }
                }
            }
        }

        // Get Items which related to this SO
        public static List<Item> GetItems(this SaleOrder saleOrder, NancyBlackDatabase db)
        {
            return db.Query<Item>().Where(item => item.SaleOrderId == saleOrder.Id).ToList();
        }
    }
}