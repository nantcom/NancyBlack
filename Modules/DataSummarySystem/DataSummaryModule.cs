using NantCom.NancyBlack.Modules.DatabaseSystem;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace NantCom.NancyBlack.Modules.DataSummarySystem
{
    public class Series
    {
        public string Key { get; set; }

        public dynamic Value { get; set; }
    }

    /// <summary>
    /// Time period to summary data to
    /// </summary>
    public enum TimePeriod
    {
        all,
        minute,
        hour,
        day,
        week,
        month,
        year
    }

    /// <summary>
    /// Function to process
    /// </summary>
    public enum Function
    {
        min,
        max,
        mean,
        stddev,
        count,
        sum
    }

    public class SummarizeTimeSeriesFactory
    {
        /// <summary>
        /// Create instance of summarizer
        /// </summary>
        /// <param name="period"></param>
        /// <param name="timeProperty"></param>
        /// <param name="valueProperty"></param>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static dynamic Create<TSource>(IEnumerable<TSource> source, TimePeriod period, string timeProperty, string valueProperty, Function fn)
        {
            var pe = Expression.Parameter(typeof(TSource));

            var sourceType = typeof(TSource);
            var valueType = Expression.Property(pe, valueProperty).Type;

            // we know the type of source and value, create new intance of the summarizer class
            var summarizerType = typeof(SummarizeTimeSeries<,>).MakeGenericType(sourceType, valueType);
            dynamic summarizerInstance = Activator.CreateInstance(summarizerType);

            summarizerInstance.Selector = valueProperty;
            summarizerInstance.TimeSelector = timeProperty;
            summarizerInstance.Period = period;
            summarizerInstance.SummaryKind = fn;
            summarizerInstance.Source = source;

            return summarizerInstance;
        }
    }

    public class SummarizeTimeSeries<TSource, TValue> where TValue : IComparable
    {
        public IEnumerable<TSource> Source { get; set; }

        /// <summary>
        /// Property to be selected out for summarization
        /// </summary>
        public string Selector { get; set; }

        /// <summary>
        /// Property to be used as representation of time from the item in collection
        /// </summary>
        public string TimeSelector { get; set; }

        /// <summary>
        /// Period of time to group data into
        /// </summary>
        public TimePeriod Period { get; set; }

        /// <summary>
        /// What kind of summary is needed
        /// </summary>
        public Function SummaryKind { get; set; }

        /// <summary>
        /// This is the function to get desired value from each item in the collection
        /// </summary>
        /// <returns></returns>
        private Func<TSource, TValue> CreateSelectorFunction()
        {
            var pe = Expression.Parameter(typeof(TSource));
            var selectorExpression = Expression.Property(pe, this.Selector);
            
            if ( "Int32,Int64,Single,Double,Decimal".Contains( selectorExpression.Type.Name ) == false )
            {
                if (this.SummaryKind != Function.count)
                {
                    throw new InvalidOperationException("Selector: " + this.Selector + " does not support min/max/mean/stddev/sum function");
                }
            }

            return Expression.Lambda<Func<TSource, TValue>>(selectorExpression, pe).Compile();
        }

        /// <summary>
        /// crete a functio which get date/time from each item in the collection
        /// </summary>
        /// <returns></returns>
        private Func<TSource, DateTime> CreateTimeSelectorFunction()
        {
            var pe = Expression.Parameter(typeof(TSource));
            var timeExpression = Expression.Property(pe, this.TimeSelector);

            if (timeExpression.Type != typeof(DateTime))
            {
                throw new InvalidOperationException("TimeSelector: " + this.TimeSelector + " does not output DateTime type");
            }

            return Expression.Lambda<Func<TSource, DateTime>>(timeExpression, pe).Compile();
        }

        /// <summary>
        /// Creates a function which groups DateTime based on Period type
        /// </summary>
        /// <returns></returns>
        private Func<DateTime, string> CreatePeriodGroupingFunction()
        {
            Func<DateTime, string> periodNormalizer = null;

            switch (this.Period)
            {
                // minute period, time within same minute will be in same group
                case TimePeriod.minute:
                    periodNormalizer = (dt) => (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0)).ToString("yyyyMMdd-HH-mm-00");
                    break;

                // hour period - time withtin same hour will be in same group
                case TimePeriod.hour:
                    periodNormalizer = (dt) => (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0)).ToString("yyyyMMdd-HH-00-00");
                    break;

                // day period - time withint same day will be in same group
                case TimePeriod.day:
                    periodNormalizer = (dt) => (new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0)).ToString("yyyyMMdd");
                    break;
                case TimePeriod.week:
                    var calendar = CultureInfo.InvariantCulture.Calendar;
                    periodNormalizer = (dt) => "Week-" + calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Monday).ToString() + "/" + dt.Year;
                    break;
                case TimePeriod.month:
                    periodNormalizer = (dt) => dt.ToString("MM-yyyy");
                    break;
                case TimePeriod.year:
                    periodNormalizer = (dt) => dt.Year.ToString();
                    break;
            }

            return periodNormalizer;
        }

        /// <summary>
        /// Creates a function which create summary for a given period
        /// </summary>
        /// <returns></returns>
        private Func<IEnumerable<TSource>, dynamic> CreateSummaryFunction()
        {
            var valueSelector = this.CreateSelectorFunction();

            switch (this.SummaryKind)
            {
                case Function.min:
                    return (col) =>
                    {
                        return col.Min((item) =>
                        {
                            dynamic value = valueSelector(item); // cast value 
                            return (double)value;
                        });
                    };

                case Function.max:
                    return (col) =>
                    {
                        return col.Max((item) =>
                        {
                            dynamic value = valueSelector(item); // cast value 
                            return (double)value;
                        });
                    };

                case Function.mean:
                    return (col) =>
                    {
                        return col.Average((item) =>
                       {
                           dynamic value = valueSelector(item); // cast value 
                            return (double)value;
                       });
                    };
                case Function.stddev:
                    throw new NotImplementedException();

                case Function.sum:

                    return (col) =>
                    {
                        return col.Sum((item) =>
                       {
                           dynamic value = valueSelector(item);
                           return (double)value;
                       });
                    };

                case Function.count:

                    // count will have to create multiple results
                    // each period will contain list of values instead of single value
                    // each value is the count of distinct values in the period
                    return (col) =>
                    {
                        var distinctValues = (from item in col
                                              select valueSelector(item)).Distinct();

                        var frequencies = new List<Series>();
                        foreach (var d in distinctValues)
                        {
                            var count = (from item in col
                                         where valueSelector(item).CompareTo(d) == 0
                                         select item).Count();

                            var key = d.ToString();

                            frequencies.Add(new Series()
                            {
                                Key = key,
                                Value = count
                            });
                        }

                        return frequencies;

                    };
            }


            throw new NotImplementedException();

        }

        /// <summary>
        /// Get Summary Series
        /// </summary>
        /// <returns></returns>
        public IEnumerable<dynamic> GetSummarySeries()
        {
            var grouper = this.CreatePeriodGroupingFunction();
            var timeSelector = this.CreateTimeSelectorFunction();

            var groups = from item in this.Source
                         group item by grouper(timeSelector(item)) into g
                         select g;

            var summary = this.CreateSummaryFunction();

            foreach (var g in groups)
            {
                var series = new Series();
                series.Key = g.Key;
                series.Value = summary(g);

                yield return series;
            }

        }
    }

    public class DataSummaryModule : BaseModule
    {

        public DataSummaryModule()
        {
            Get["/tables/{table_name}/summarize"] = this.HandleRequest(this.HandleSummarizeRequest);
        }



        private dynamic HandleSummarizeRequest(dynamic arg)
        {
            var table = (string)arg.table_name;
            var timeperiod = (string)this.Request.Query.period;
            var select = (string)this.Request.Query.select;
            var function = (string)this.Request.Query.fn;
            var timeselect = (string)this.Request.Query.time;

            if (timeselect == null)
            {
                timeselect = "__createdAt";
            }

            var dt = this.SiteDatabase.DataType.FromName(table);
            if (dt == null)
            {
                return 404;
            }

            var type = dt.GetCompiledType();


            var method = typeof(SummarizeTimeSeriesFactory)
                            .GetMethods().Single(m => m.Name == "Create" && m.IsStatic)
                            .MakeGenericMethod(type); // calling this.Summarize<T>

            var getTable = typeof(NancyBlackDatabase)
                            .GetMethods().Single(m => m.Name == "Query" && m.GetParameters().Length == 0)
                            .MakeGenericMethod(type); // calling this.Summarize<T>
            
            dynamic summarizer = method.Invoke(this, new object[] {
                getTable.Invoke( this.SiteDatabase, null ),
                Enum.Parse( typeof(TimePeriod), timeperiod ),
                timeselect,
                select,
                Enum.Parse(typeof(Function), function)
            });

            return summarizer.GetSummarySeries();
        }
    }
}