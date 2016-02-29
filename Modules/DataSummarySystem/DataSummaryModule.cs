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
        sum,
        count,
        frequency,
        cumulativecount,
        cumulativesum
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
        public static dynamic Create<TSource>(IEnumerable<TSource> source, TimePeriod period, string timeProperty, string valueProperty, Function fn, DateTime? minDate)
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
            summarizerInstance.MinDate = minDate;

            return summarizerInstance;
        }
    }

    public class SummarizeTimeSeries<TSource, TValue> where TValue : IComparable
    {
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public IEnumerable<TSource> Source { get; set; }

        /// <summary>
        /// Minimum date to get data, if not specified the defaults for each periods are:
        /// Previous 1 Hour for Minute,
        /// Previous 1 Day for Hour,
        /// Previous 7 days for Day,
        /// Previous 30 days for Week
        /// Previous 12 Months for Month,
        /// Previous 3 Years for year
        /// </summary>
        public DateTime? MinDate { get; set; }

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

            Func<DateTime, string> getJavascriptTime = (dt) =>
            {
                return dt.Subtract(UnixEpoch).TotalMilliseconds.ToString();
            };

            switch (this.Period)
            {
                // minute period, time within same minute will be in same group
                case TimePeriod.minute:
                    periodNormalizer = (dt) => getJavascriptTime(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0));
                    break;

                // hour period - time withtin same hour will be in same group
                case TimePeriod.hour:
                    periodNormalizer = (dt) => getJavascriptTime(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0));
                    break;

                // day period - time withint same day will be in same group
                case TimePeriod.day:
                    periodNormalizer = (dt) => getJavascriptTime(new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0));
                    break;
                case TimePeriod.week:
                    var calendar = CultureInfo.InvariantCulture.Calendar;
                    periodNormalizer = (dt) => "Week-" + calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Monday).ToString("00") + "/" + dt.Year;
                    break;
                case TimePeriod.month:
                    periodNormalizer = (dt) => getJavascriptTime(new DateTime(dt.Year, dt.Month, 1, 0, 0, 0));
                    break;
                case TimePeriod.year:
                    periodNormalizer = (dt) => getJavascriptTime(new DateTime(dt.Year, 1, 1, 0, 0, 0));
                    break;
            }

            return periodNormalizer;
        }

        /// <summary>
        /// Create the period step from given date range
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetPeriods(Func<DateTime, string> periodGrouper, DateTime? min = null, bool includesFuture = false)
        {
            Func<DateTime, DateTime> stepper = null;
            switch (this.Period)
            {
                case TimePeriod.minute:
                    stepper = (dt) => dt.AddMinutes(1);
                    break;
                case TimePeriod.hour:
                    stepper = (dt) => dt.AddHours(1);
                    break;
                case TimePeriod.day:
                    stepper = (dt) => dt.AddDays(1);
                    break;
                case TimePeriod.week:
                    stepper = (dt) => dt.AddDays(7);
                    break;
                case TimePeriod.month:
                    stepper = (dt) => dt.AddMonths(1);
                    break;
                case TimePeriod.year:
                    stepper = (dt) => dt.AddYears(1);
                    break;
                default:
                    break;
            }

            DateTime now = DateTime.Now;
            if (min == null)
            {
                switch (this.Period)
                {
                    case TimePeriod.minute:
                        min = now.AddHours(-1);
                        break;
                    case TimePeriod.hour:
                        min = now.AddHours(-24);
                        break;
                    case TimePeriod.day:
                        min = now.Date.AddDays(-30);
                        break;
                    case TimePeriod.week:
                        min = now.Date.AddDays(-30);
                        break;
                    case TimePeriod.month:
                        min = now.Date.AddMonths(-12);
                        break;
                    case TimePeriod.year:
                        min = now.Date.AddYears(-3);
                        break;
                    default:
                        throw new InvalidOperationException("TimePeriod is not supported: " + this.Period);
                }
            }
            
            DateTime current = min.Value;
            string last = periodGrouper(min.Value);
            yield return last;

            while (true)
            {
                current = stepper(current);
                if (current >= now)
                {
                    if (includesFuture == true)
                    {
                        var lastGroup = periodGrouper(current);
                        if (lastGroup != last)
                        {
                            yield return lastGroup;
                        }
                    }
                    break;
                }

                var newGroup = periodGrouper(current);
                if (newGroup != last)
                {
                    yield return newGroup;
                    last = newGroup;
                }
            }
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

                case Function.cumulativesum:
                case Function.sum:

                    return (col) =>
                    {
                        return col.Sum((item) =>
                       {
                           dynamic value = valueSelector(item);
                           return (double)value;
                       });
                    };

                case Function.cumulativecount:
                case Function.count:

                    // number of items in the given period
                    return (col) =>
                    {
                        return col.Count();
                    };

                case Function.frequency:

                    // how many number of distinct occurance of value
                    // in the given period

                    // each period will contain list of distinct values
                    // and occurance of each distinct values in the given period
                    return (col) =>
                    {
                        var distinctValues = col.ToLookup( item => valueSelector( item ) );
                        return from value in distinctValues
                               let count = value.Count()
                               orderby count 
                               select new Series()
                               {
                                   Key = value.Key.ToString(),
                                   Value = count
                               };

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
            var summary = this.CreateSummaryFunction();

            if (this.Period == TimePeriod.all)
            {
                // all means no period, every item is in the same group
                var series = new Series();
                series.Key = "overall";
                series.Value = summary( this.Source );

                yield return series;

                yield break;
            }

            var grouper = this.CreatePeriodGroupingFunction();
            var timeSelector = this.CreateTimeSelectorFunction();

            var source = this.Source.ToList();

            var groups = from item in source
                         group item by grouper(timeSelector(item)) into g
                         select g;
            
            var allseries = this.GetPeriods(grouper, this.MinDate ).ToDictionary(
                                k => k,
                                v => new Series() { Key = v, Value = 0 }
                            );


            if (this.SummaryKind == Function.cumulativecount ||
                this.SummaryKind == Function.cumulativesum)
            {
                var hasvalue = groups.ToDictionary(g => g.Key);
                double currentValue = 0;

                // for cumulative - must assign values into all series
                foreach (var series in allseries.Values.OrderBy(s => s.Key))
                {
                    // get value from the summary function if current series have value
                    if (hasvalue.ContainsKey( series.Key ))
                    {
                        // keep it for assign in series that does not have value 
                        currentValue += summary( hasvalue[series.Key] );
                    }
                    series.Value = currentValue;
                    yield return series;
                }
            }
            else
            {
                foreach (var g in groups)
                {
                    if (allseries.ContainsKey(g.Key))
                    {
                        var series = new Series();
                        series.Key = g.Key;
                        series.Value = summary(g);

                        allseries[g.Key] = series;
                    }
                }

                foreach (var series in allseries.Values.OrderBy(s => s.Key))
                {
                    yield return series;
                }

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

            TableSecModule.ThrowIfNoPermission(this.Context, table, TableSecModule.PERMISSON_QUERY);
            
            var timeperiod = (string)this.Request.Query.period;
            var select = (string)this.Request.Query.select;
            var function = (string)this.Request.Query.fn;
            var timeselect = (string)this.Request.Query.time;
            var filter = (string)this.Request.Query["$filter"];
            var minTimeString = (string)this.Request.Query.mindate;

            DateTime? minTime = null;
            if (minTimeString != null)
            {
                minTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                           .AddMilliseconds(long.Parse(minTimeString))
                           .ToLocalTime();
            }

            if (timeselect == null)
            {
                timeselect = "__createdAt";
            }

            if (timeperiod == null)
            {
                timeperiod = "all";
                timeselect = "not-used";
            }

            var dt = this.SiteDatabase.DataType.FromName(table);
            if (dt == null)
            {
                return 404;
            }

            if (select == null || function == null || timeselect == null )
            {
                return 400;
            }

            var type = dt.GetCompiledType();
            var sourceTable = this.SiteDatabase.Query(table, filter);
            
            if (minTimeString == "0")
            {
                var min = this.SiteDatabase.QueryAsJObject(table, oDataSort: timeselect).FirstOrDefault();
                if (min == null)
                {
                    return 400;
                }

                minTime = min.Value<DateTime>(timeselect);
            }


            var createSummarizer = typeof(SummarizeTimeSeriesFactory)
                            .GetMethods().Single(m => m.Name == "Create" && m.IsStatic)
                            .MakeGenericMethod(type); // calling this.Summarize<T>
            
            dynamic summarizer = createSummarizer.Invoke(this, new object[] {
                sourceTable,
                Enum.Parse( typeof(TimePeriod), timeperiod ),
                timeselect,
                select,
                Enum.Parse(typeof(Function), function),
                minTime
            });

            return summarizer.GetSummarySeries();
        }
    }
}