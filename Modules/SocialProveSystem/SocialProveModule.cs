using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules.SocialProveSystem
{
    public class SocialProveEvent : TableEntity
    {
        /// <summary>
        /// User Guid
        /// </summary>
        public string UserGuid { get; set; }

        /// <summary>
        /// Additional data about the event
        /// </summary>
        public string EventData { get; set; }
    }

    public class SPEComparer : IEqualityComparer<SocialProveEvent>
    {
        public bool Equals(SocialProveEvent x, SocialProveEvent y)
        {
            return x.UserGuid.Equals(y.UserGuid);
        }

        public int GetHashCode(SocialProveEvent obj)
        {
            return obj.UserGuid.GetHashCode();
        }
    }

    public class SocialProveStat
    {
        public int Total { get; set; }

        public int Distinct { get; set; }

        public List<string> DataList { get; set; }

        public int TimeInterval { get; set; }
    }

    public class SocialProveModule : BaseModule
    {
        private static SPEComparer _Comparer = new SPEComparer();

        public SocialProveModule()
        {
            // Get number of user in the given key and also record the event
            Get["/__socialprove"] = (arg) =>
            {
                string eventName = this.Request.Query.e;
                string eventData = this.Request.Query.d;
                var table = this.GetSocialProveTable();

                SocialProveEvent spe = new SocialProveEvent();
                spe.PartitionKey = DateTime.Now.ToString("yyyyMMdd") + "|" + Uri.EscapeDataString( eventName );
                spe.RowKey = DateTime.Now.ToString("HH-mm") + "|" + DateTime.Now.Ticks;
                spe.UserGuid = this.CurrentUser.Guid.ToString();
                spe.EventData = eventData;

                table.Execute(TableOperation.InsertOrReplace(spe));

                var cacheKey = "SocialProve-" + eventName;
                var spestat = MemoryCache.Default[cacheKey] as SocialProveStat;
                if (spestat == null)
                {
                    var time = 30;

                    loadmore:

                    // find all event of same day in past time
                    var past60Minutes = DateTime.Now.AddMinutes(time * -1).ToString("HH-mm");
                    var speQueryString = TableQuery.CombineFilters(
                                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, spe.PartitionKey),
                                    TableOperators.And,
                                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, past60Minutes));

                    var speQuery = new TableQuery<SocialProveEvent>();

                    var rawData = table.ExecuteQuery<SocialProveEvent>(speQuery.Where(speQueryString)).ToList();

                    spestat = new SocialProveStat();
                    spestat.DataList = rawData.Select(r => r.EventData).Distinct().ToList();
                    spestat.Total = rawData.Count;
                    spestat.Distinct = rawData.Distinct(_Comparer).Count();
                    spestat.TimeInterval = time;

                    if (spestat.Total < 50 )
                    {
                        time = time + 30;
                        if (time < 60 * 8) // up to 8 hours
                        {
                            goto loadmore;
                        }
                    }

                    MemoryCache.Default.Add(cacheKey, spestat, DateTimeOffset.Now.AddMinutes(1));
                }

                spestat.Total += 1; // increase count on our side

                return spestat;
            };
        }

        /// <summary>
        /// Gets the pageview table
        /// </summary>
        /// <returns></returns>
        private CloudTable GetSocialProveTable(bool cache = true)
        {
            Func<CloudTable> getTable = () =>
            {
                var cred = new StorageCredentials((string)this.CurrentSite.socialprove.credentials);
                var client = new CloudTableClient(new Uri((string)this.CurrentSite.socialprove.server), cred);
                return client.GetTableReference((string)this.CurrentSite.socialprove.table);
            };


            if (cache == false)
            {
                return getTable();
            }

            var key = string.Format("azure{0}-{1}-{2}",
                               (string)this.CurrentSite.socialprove.credentials,
                               (string)this.CurrentSite.socialprove.server,
                               (string)this.CurrentSite.socialprove.table).GetHashCode().ToString();

            var table = MemoryCache.Default[key] as CloudTable;
            if (table == null)
            {
                table = getTable();

                MemoryCache.Default.Add(key, table, DateTimeOffset.Now.AddDays(1));
            }

            return table;
        }
    }
}