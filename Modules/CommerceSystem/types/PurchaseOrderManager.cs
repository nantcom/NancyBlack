using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public static class PurchaseOrderManager
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
                                                                                  (item.Url.Contains("/cpu/") || item.Url.Contains("/gpu/"))
                                                                                 select GetSupplierProductTitle(item));

            }
            return GetSupplierProductTitle(product);
        }

        /// <summary>
        /// Generate OrderRelation and Save to DB
        /// </summary>
        /// <param name="purchaseOrder"></param>
        /// <param name="saleOrder"></param>
        /// <param name="db"></param>
        private static void GenerateOrderRelation(PurchaseOrder purchaseOrder, SaleOrder saleOrder, NancyBlackDatabase db)
        {
            var relation = new OrderRelation()
            {
                PurchaseOrderId = purchaseOrder.Id,
                SaleOrderId = saleOrder.Id
            };

            db.UpsertRecord(relation);
        }

        private static void AddItemToPurchaseOrder(SaleOrder saleOrder, IEnumerable<PurchaseItem> purchaseList, NancyBlackDatabase db)
        {
            var relatedPOId = new List<int>();

            foreach (var purchaseItem in purchaseList)
            {
                var supplier = db.GetById<Supplier>(purchaseItem.SupplierId);

                // try to figure out date to order product from this supplier

                var today = saleOrder.PaymentReceivedDate.Date;
                var now = saleOrder.PaymentReceivedDate;

                if (now == default(DateTime))
                {
                    today = saleOrder.__createdAt.Date;
                    now = saleOrder.__createdAt;
                }

                DateTime orderDate = now;
                if (supplier.OrderPeriod == "Daily")
                {
                    var canOrderToday = today.AddTicks(supplier.OrderTime.Ticks) > now;
                    orderDate = canOrderToday ? today.AddTicks(supplier.OrderTime.Ticks) : today.AddTicks(supplier.OrderTime.Ticks).AddDays(1);
                }
                else if (supplier.OrderPeriod == "Weekly")
                {
                    var expectedDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), supplier.WeeklyOrderWhen, true);

                    // if can order this week
                    orderDate = today.AddDays(expectedDay - today.DayOfWeek);

                    if (today.DayOfWeek >= expectedDay)
                    {
                        // order next week
                        orderDate = orderDate.AddDays(7);
                    }

                }
                else if (supplier.OrderPeriod == "Monthly")
                {
                    var thisMonth = new DateTime(today.Year, today.Month, 1);

                    // if order this month
                    orderDate = thisMonth.AddDays(supplier.MonthlyOrderWhen)
                                     .AddTicks(supplier.OrderTime.Ticks);

                    if (today.Day >= supplier.MonthlyOrderWhen)
                    {
                        // order next month
                        orderDate = orderDate.AddMonths(1);
                    }
                }

                // see if the po already exists
                var poToUse = db.Query<PurchaseOrder>().Where(
                                po => po.OrderDate == orderDate &&
                                      po.SupplierId == purchaseItem.SupplierId).FirstOrDefault();

                if (poToUse == null)
                {
                    poToUse = new PurchaseOrder();
                    poToUse.SupplierId = purchaseItem.SupplierId;
                    poToUse.OrderDate = orderDate;
                    poToUse.Items.Add(purchaseItem);
                    poToUse.LinkedSaleorder = new List<int>() { saleOrder.Id };

                    db.UpsertRecord(poToUse);
                }
                else
                {
                    // existing
                    poToUse.Combine(purchaseItem);

                    if (poToUse.LinkedSaleorder.Contains(saleOrder.Id) == false)
                    {
                        poToUse.LinkedSaleorder.Add(saleOrder.Id);
                    }
                }

                db.UpsertRecord(poToUse);

                if (!relatedPOId.Contains(poToUse.Id))
                {
                    relatedPOId.Add(poToUse.Id);
                    GenerateOrderRelation(poToUse, saleOrder, db);
                }
            }
        }

        private static IEnumerable<Product> GetPurchasingProduct(SaleOrder saleOrder, NancyBlackDatabase db)
        {
            return from item in saleOrder.ItemsDetail
                   let itemInDbTest = db.GetById<Product>(item.Id)
                   let itemInDb = itemInDbTest == null ? item : itemInDbTest
                   where itemInDb.Attributes != null
                   let supplierNull = itemInDb.Attributes.supplier == null
                   let deserialize = (item.Attributes.supplier = supplierNull ? JObject.Parse("{}") : JObject.Parse(itemInDb.Attributes.supplier.ToString()))
                   let setSupplier = (item.Attributes.supplierinfo = item.Attributes.supplier.id == null ? JObject.FromObject(new Supplier()) : JObject.FromObject(db.GetById<Supplier>((int)item.Attributes.supplier.id)))
                   select item;
        }

        private static void GenerateOrderedPurchaseOrder(NancyBlackDatabase db)
        {
            var pos = db.Query<PurchaseOrder>().Where(po => po.WasGenerated == false && po.OrderDate < DateTime.Now);
            foreach (var po in pos)
            {
                po.Generate();
                db.UpsertRecord(po);
            }
        }

        private static List<int> GeneratePartsIdList(Product product, List<Product> items)
        {
            if (product.Url.Contains("/laptops/"))
            {
                return (from item in items
                        where
                          (item.Attributes == null || item.Attributes.supplierinfo.Id == 0) &&
                         (item.Url.Contains("/cpu/") || item.Url.Contains("/gpu/"))
                        select item.Id).ToList();

            }
            return null;
        }

        public static IEnumerable<PurchaseOrder> GetPendingPurchaseOrders(NancyBlackDatabase db)
        {
            var pendingSOs = db.Query<SaleOrder>()
                .Where(so => so.PaymentStatus == PaymentStatus.PaymentReceived && so.Status == SaleOrderStatus.WaitingForOrder)
                .ToList();

            var purchaseItems = new List<PurchaseItem>();
            foreach (var saleOrder in pendingSOs)
            {
                purchaseItems.AddRange(saleOrder.ToPurchaseItems(db));
            }

            var purchaseOrders = new ConcurrentBag<PurchaseOrder>();
            db.Query<Supplier>().AsParallel().ForAll((supplier) =>
            {
                var po = new PurchaseOrder()
                {
                    SupplierId = supplier.Id,
                    Items = new List<PurchaseItem>()
                };

                var matchedItems = purchaseItems.Where(pi => pi.SupplierId == supplier.Id);
                foreach (var item in matchedItems)
                {
                    po.Combine(item);
                }

                purchaseOrders.Add(po);
            });

            return purchaseOrders.Where(po => po.Items.Count > 0).OrderBy(po => po.SupplierId);
        }

        public static IEnumerable<PurchaseItem> ToPurchaseItems(this SaleOrder saleOrder, NancyBlackDatabase db)
        {
            var source = GetPurchasingProduct(saleOrder, db);

            List<Product> items = new List<Product>();
            foreach (var item in source)
            {
                items.Add(item);
                if (item.Attributes.Qty != null)
                {
                    for (int i = 1; i < (int)item.Attributes.Qty; i++)
                    {
                        items.Add(item);
                    }
                }
            }

            // create detailed list of product in sale order
            // DOES NOT WORK DUE TO BUG in SaleOrder.Items
            //var items = (from itemId in saleOrder.Items
            //             let itemInDb = db.GetById<Product>(itemId)
            //             let item = itemInDb == null ? saleOrder.ItemsDetail.Where(p => p.Id == itemId).FirstOrDefault() : itemInDb
            //             where item.Attributes != null
            //             let supplierNull = item.Attributes.supplier == null
            //             let deserialize = (item.Attributes.supplier = supplierNull ? JObject.Parse("{}") : JObject.Parse(item.Attributes.supplier.ToString()))
            //             let setSupplier = (item.Attributes.supplierinfo = item.Attributes.supplier.id == null ? JObject.FromObject(new Supplier()) : JObject.FromObject(db.GetById<Supplier>((int)item.Attributes.supplier.id)))
            //             select item).ToList();

            return from item in items
                               where
                                   item.Attributes.supplierinfo.Id != 0 // only product that has supplier
                               group item by item.Id into itemWithSameId // group the product to get count
                               let firstItem = itemWithSameId.First()
                               select new PurchaseItem()
                               {
                                   SupplierId = firstItem.Attributes.supplierinfo.Id,
                                   ProductTitle = firstItem.Title,
                                   Qty = itemWithSameId.Count(),
                                   Title = GenerateSupplierLaptopTitle(firstItem, items),
                                   ProductId = firstItem.Id,
                                   PartsId = GeneratePartsIdList(firstItem, items)
                               };
        }

        /// <summary>
        /// add item from SaleOrder to PurchaseOrder in case SaleOrder never been added yet
        /// </summary>
        /// <param name="saleOrder"></param>
        /// <param name="db"></param>
        public static void AddSalelOrderToPurchaseOrder(SaleOrder saleOrder, NancyBlackDatabase db)
        {
            GenerateOrderedPurchaseOrder(db);

            var relations = db.Query<OrderRelation>().Where(relation => relation.SaleOrderId == saleOrder.Id);
            if (relations.Count() > 0)
            {
                return;
            }

            var purchaseList = saleOrder.ToPurchaseItems(db);

            AddItemToPurchaseOrder(saleOrder, purchaseList, db);
        }
    }
}