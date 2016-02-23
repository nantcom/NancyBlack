using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    /// <summary>
    /// InventorySummary is the monthly summary of product in stock 
    /// (only one Product per InventorySummary)
    /// summarize every first day of month
    /// </summary>
    public class InventorySummary : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }
        
        public DateTime SummaryDate { get; set; }

        /// <summary>
        /// Product Id Involved in the summary
        /// </summary>
        public int ProductId { get; set; }

        public int LastSummaryId { get; set; }

        /// <summary>
        /// Remaining stock
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// Collection of InventoryMovement's Id (collected when summarize)
        /// </summary>
        public List<int> MovementTransactionsId { get; set; }

        /// <summary>
        /// Summarize stock in checkingDate
        /// </summary>
        public static IEnumerable<InventorySummary> GetStocks(DateTime checkingDate, NancyBlackDatabase db)
        {
            var startOfThisMonthDate = checkingDate.Date.AddDays(-checkingDate.Day + 1);

            var startOfThisMonthStock = db.Query<InventorySummary>()
                .Where(stock => stock.SummaryDate == startOfThisMonthDate)
                .ToDictionary(stock => stock.ProductId);

            var thisMonthMovements = db.Query<InventoryMovement>()
                .Where(iMovement => iMovement.MovementDate <= checkingDate && iMovement.MovementDate >= startOfThisMonthDate);

            // group movement by product's id
            var movementGrouped = from movement in thisMonthMovements
                                  group movement by movement.ProductId into groupByProduct
                                  select groupByProduct;

            return InventorySummary.SummarizeStock(movementGrouped, startOfThisMonthStock, checkingDate);
        }

        /// <summary>
        /// Summarize from last month to day 1 of current month stock
        /// (return only unsummarized stock if there is a summarized 
        /// stock this function will return null)
        /// </summary>
        public static IEnumerable<InventorySummary> GetUnsummarizedStocks(NancyBlackDatabase db)
        {
            var startOfThisMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            var thisMonthStock = db.Query<InventorySummary>()
                .Where(stock => stock.SummaryDate == startOfThisMonth);

            // return null when start of this month last been summarize
            if (thisMonthStock.Count() > 0)
            {
                return null;
            }

            // start of last month stock
            var previousMonthStocks = db.Query<InventorySummary>()
                .Where(stock => stock.SummaryDate == startOfLastMonth)
                .ToDictionary(stock => stock.ProductId);

            var lastMonthMovements = db.Query<InventoryMovement>()
                .Where(iMovement => iMovement.MovementDate >= startOfLastMonth && iMovement.MovementDate < startOfThisMonth);

            // group movement by product's id
            var movementGrouped = from movement in lastMonthMovements
                                  group movement by movement.ProductId into groupByProduct
                                  select groupByProduct;

            return InventorySummary.SummarizeStock(movementGrouped, previousMonthStocks, startOfThisMonth);
        }
        
        /// <summary>
        /// Summarize remaining stock with the movement
        /// </summary>
        /// <param name="movementGrouped">group by product id</param>
        /// <param name="loopupInStock">*** required to remove some item in this dict ***</param>
        /// <param name="summarizeDate">summarize date in InventoryMovement</param>
        /// <returns></returns>
        private static IEnumerable<InventorySummary> SummarizeStock(

            IEnumerable<IGrouping<int, InventoryMovement>> movementGrouped,
            Dictionary<int, InventorySummary> loopupInStock,
            DateTime summarizeDate

            )
        {
            foreach (var group in movementGrouped)
            {
                InventorySummary inventory = new InventorySummary()
                {
                    ProductId = group.Key,
                    SummaryDate = summarizeDate,
                    Stock = 0
                };

                // add stock from remaining last stock
                if (loopupInStock.ContainsKey(group.Key))
                {
                    inventory.Stock = loopupInStock[group.Key].Stock;
                    inventory.LastSummaryId = loopupInStock[group.Key].Id;
                    loopupInStock.Remove(group.Key);
                }

                inventory.Stock += group.Sum(movement => movement.Change);
                inventory.MovementTransactionsId = group.Select(movement => movement.Id).ToList();
                yield return inventory;
            }

            // Create remaining summary from previous summary
            foreach (var remaiiningInventory in loopupInStock.Values)
            {
                InventorySummary inventory = new InventorySummary()
                {
                    ProductId = remaiiningInventory.ProductId,
                    SummaryDate = summarizeDate,
                    Stock = remaiiningInventory.Stock,
                    LastSummaryId = remaiiningInventory.Id
                };

                yield return inventory;
            }
        }
    }
}