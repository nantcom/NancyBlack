using NantCom.NancyBlack.Modules.DatabaseSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types.Investment
{
    public abstract class InvestedReport
    {
        public int Quantity { get; set; }

        public int ReturnFromInvestment { get; set; }
    }

    public class InvestedProductReport : InvestedReport
    {
        public InvestedProductReport(string title, List<SaleOrder> paidSaleOrders, int productId, int returnFromInvestmentPerUnit)
        {
            this.Title = title;
            var monthlyReports = InvestedProductMonthlyReport.GetMonthlyReports(paidSaleOrders, productId, returnFromInvestmentPerUnit);
            this.SummarizeReport(monthlyReports);
        }

        public InvestedProductReport(string title, IEnumerable<InvestedProductMonthlyReport> monthlyReports)
        {
            this.Title = title;
            this.SummarizeReport(monthlyReports);
        }

        private void SummarizeReport(IEnumerable<InvestedProductMonthlyReport> monthlyReports)
        {
            this.Quantity = 0;
            this.ReturnFromInvestment = 0;
            this.MonthlyReports = monthlyReports;
            foreach (var report in monthlyReports)
            {
                this.Quantity += report.Quantity;
                this.ReturnFromInvestment += report.ReturnFromInvestment;
            }
        }

        public string Title { get; set; }

        public IEnumerable<InvestedProductMonthlyReport> MonthlyReports { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monthlyReportLists">List of monthly report list</param>
        /// <returns></returns>
        public static InvestedProductReport MergeReports(string title, params IEnumerable<InvestedProductMonthlyReport>[] monthlyReportLists)
        {
            var lookupReport = new ConcurrentDictionary<DateTime, InvestedProductMonthlyReport>();

            monthlyReportLists.AsParallel().ForAll((monthlyReports) =>
            {
                monthlyReports.AsParallel().ForAll((report) =>
                {
                    var newReport = new InvestedProductMonthlyReport()
                    {
                        ReportedDate = report.ReportedDate,
                        Quantity = report.Quantity,
                        ReturnFromInvestment = report.ReturnFromInvestment
                    };

                    lookupReport.AddOrUpdate(newReport.ReportedDate, newReport, (date, existReport) =>
                    {
                        existReport.Quantity += newReport.Quantity;
                        existReport.ReturnFromInvestment += newReport.ReturnFromInvestment;
                        return existReport;
                    });
                });
            });

            return new InvestedProductReport(title, lookupReport.Values.OrderBy(report => report.ReportedDate));
        }
    }

    public class InvestedProductMonthlyReport : InvestedReport
    {
        public DateTime ReportedDate { get; set; }
        
        public static IEnumerable<InvestedProductMonthlyReport> GetMonthlyReports(List<SaleOrder> paidSaleOrders, int productId, int returnFromInvestmentPerUnit)
        {
            var lookupReport = new ConcurrentDictionary<DateTime, InvestedProductMonthlyReport>();

            paidSaleOrders.AsParallel().ForAll((so) =>
            {
                var product = so.ItemsDetail.Where(item => item.Id == productId).FirstOrDefault();
                if (product != null)
                {
                    var month = new DateTime(so.PaymentReceivedDate.Year, so.PaymentReceivedDate.Month, DateTime.DaysInMonth(so.PaymentReceivedDate.Year, so.PaymentReceivedDate.Month));
                    
                    var newReport = new InvestedProductMonthlyReport()
                    {
                        ReportedDate = month,
                        Quantity = (int)product.Attributes.Qty,
                        ReturnFromInvestment = 0
                    };

                    lookupReport.AddOrUpdate(month, newReport, (date, existReport) =>
                    {
                        existReport.Quantity += (int)product.Attributes.Qty;
                        return existReport;
                    });
                }
            });

            lookupReport.Values.AsParallel().ForAll(report => report.ReturnFromInvestment = report.Quantity * returnFromInvestmentPerUnit);

            return lookupReport.Values.OrderBy(report => report.ReportedDate);
        }
    }
}