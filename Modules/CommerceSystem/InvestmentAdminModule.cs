using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.CommerceSystem.types.Chart;
using NantCom.NancyBlack.Modules.CommerceSystem.types.Investment;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class InvestmentAdminModule : BaseModule
    {
        public InvestmentAdminModule()
        {
            Get["/admin/tables/Investment"] = this.HandleRequest(this.InvestmentPage);
        }

        private dynamic InvestmentPage(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin") && this.CurrentUser.Id != 3)
            {
                return 403;
            }

            var dummyPage = new Page();

            var cash = 488400;
            List<InvestedProductReport> productReports = null;
            AngularChart revenueChart = null;
            AngularChart soldLaptopChart = null;
            var paidSaleOrders = this.SiteDatabase.Query<SaleOrder>()
                .Where(so => so.PaymentStatus == PaymentStatus.PaymentReceived).ToList();

            Task.WhenAll(
                Task.Run(() => productReports = this.GetItReport(paidSaleOrders)),
                Task.Run(() => revenueChart = this.GetSummarizedRevenueChart(paidSaleOrders)),
                Task.Run(() => soldLaptopChart = this.GetLaptopSoldChart(paidSaleOrders, this.SiteDatabase))
            ).Wait();

            var summarizedProductReport = InvestedProductReport.MergeReports(
                "Summarized Report", productReports.Select(report => report.MonthlyReports).ToArray()
                );

            var currentCash = cash + summarizedProductReport.ReturnFromInvestment;

            var data = new
            {
                SummarizedReports = summarizedProductReport,
                ProductReports = productReports,
                Cash = cash,
                CurrentCash = currentCash,
                SummarizedRevenueChart = revenueChart,
                SoldLaptopChart = soldLaptopChart
            };

            return View["/Admin/investmentsummary", new StandardModel(this, dummyPage, data)];
        }

        private List<InvestedProductReport> GetItReport(List<SaleOrder> paidSaleOrders)
        {
            InvestedProductReport keyboardX14Report = null;
            InvestedProductReport keyboardXL15Report = null;
            InvestedProductReport keyboardXL17Report = null;

            Task.WhenAll(
                Task.Run(() => { keyboardX14Report = new InvestedProductReport("Keyboard's X14 Report", paidSaleOrders, 176, 389); }),
                Task.Run(() => { keyboardXL15Report = new InvestedProductReport("Keyboard's XL15 Report", paidSaleOrders, 138, 555); }),
                Task.Run(() => { keyboardXL17Report = new InvestedProductReport("Keyboard's XL17 Report", paidSaleOrders, 173, 555); })
                ).Wait();

            return new List<InvestedProductReport>()
            {
                keyboardX14Report,
                keyboardXL15Report,
                keyboardXL17Report
            };
        }

        private AngularChart GetSummarizedRevenueChart(List<SaleOrder> paidSaleOrders)
        {
            var chart = new AngularChart()
            {
                Title = "Summarized Revenue",
                Labels = new List<string>(),
                Series = new List<string>() { "Monthly Sales", "Total Sales" },
                Colours = new List<string>() { "#4682B4", "#48BF83" },
                Data = new List<dynamic>()
                {
                    new List<int>(), new List<int>()
                }
            };

            paidSaleOrders.AsParallel().ForAll((saleOrder) =>
            {
                if (saleOrder.PaymentReceivedDate.Ticks == 0)
                {
                    saleOrder.PaymentReceivedDate = saleOrder.__createdAt;
                }
            });

            var monthGroup = paidSaleOrders
                .GroupBy(so => so.PaymentReceivedDate.ToString("yyyy/MM", new CultureInfo("th-TH")))
                .OrderBy(group => group.Key);
            int totalRevenue = 0;

            foreach (var group in monthGroup)
            {
                chart.Labels.Add(group.Key);
                var sum = Decimal.ToInt32(group.Sum(g => g.TotalAmount));
                totalRevenue += sum;
                chart.Data[0].Add(sum);
                chart.Data[1].Add(totalRevenue);
            }

            return chart;
        }

        private AngularChart GetLaptopSoldChart(List<SaleOrder> paidSaleOrders, NancyBlackDatabase db)
        {
            var chart = new AngularChart()
            {
                Title = "Sold Laptop",
                Labels = new List<string>(),
                EnumType = AngularChartType.Pie,
                IsLegend = true,
                Data = new List<dynamic>()
            };
            
            var lookupLaptopId = new ConcurrentDictionary<int, int>();

            // count sold laptop
            paidSaleOrders.AsParallel().ForAll((so =>
            {
                // find laptop from item's url
                var laptop = so.ItemsDetail.Where(item => item.Url.StartsWith("/products/laptops")).FirstOrDefault();

                // find laptop in another type (archive-laptops)
                if (laptop == null)
                {
                    laptop = so.ItemsDetail.Where(item => item.Url.StartsWith("/products/archive-laptops")).FirstOrDefault();
                }

                // count sold laptop when found one
                if (laptop != null)
                {
                    var newQty = (int)laptop.Attributes.Qty;
                    lookupLaptopId.AddOrUpdate(laptop.Id, newQty, (laptopId, existQty) => existQty + newQty);
                }
            }));

            var soldLaptopsData = new ConcurrentBag<dynamic>();

            // group.Key = laptop's id and group.Value = laptop sold's count
            lookupLaptopId.AsParallel().ForAll((group =>
            {
                var laptop = db.GetById<Product>(group.Key);
                soldLaptopsData.Add(new
                {
                    Id = group.Key,
                    Title = laptop.Title,
                    Quantity = group.Value,
                    Price = laptop.Price
                });
            }));

            foreach (var laptop in soldLaptopsData.OrderBy(product => product.Price))
            {
                chart.Labels.Add(laptop.Title);
                chart.Data.Add(laptop.Quantity);
            }

            return chart;
        }
    }
}