using Nancy;
using Nancy.Bootstrapper;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public class DataWatcherModule : BaseModule, IPipelineHook
    {
        public class DatabaseEvent
        {
            public string Action { get; set; }

            public NancyBlackDatabase Database { get; set; }

            public string DataTypeName { get; set; }

            public dynamic AffectedRow { get; set; }
        }

        public class WatcherConfig
        {
            public class WatcherEmailNotifyConfig
            {
                public bool enable { get; set; }

                public string emailSubject { get; set; }

                public string emailTemplate { get; set; }

                public string sendTo { get; set; }
            }

            /// <summary>
            /// Normalized name of the table
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// Versioning enabled
            /// </summary>
            public bool version { get; set; }

            /// <summary>
            /// Email Notify configuration for create events
            /// </summary>
            public WatcherEmailNotifyConfig create { get; set; }

            /// <summary>
            /// Email Notify configuration for update events
            /// </summary>
            public WatcherEmailNotifyConfig update { get; set; }

            /// <summary>
            /// Email Notify configuration for deleted events
            /// </summary>
            public WatcherEmailNotifyConfig deleted { get; set; }

            /// <summary>
            /// Gets Email notify configuration for given action
            /// </summary>
            /// <param name="action"></param>
            /// <returns></returns>
            public WatcherEmailNotifyConfig this[string action]
            {
                get
                {
                    if (action == "create")
                    {
                        return this.create;
                    }

                    if (action == "update")
                    {
                        return this.update;
                    }

                    if (action == "delete")
                    {
                        return this.deleted;
                    }

                    throw new NotImplementedException(action + " was not implemented.");
                }
            }
        }

        private static ConcurrentBag<DatabaseEvent> _Events;

        static DataWatcherModule()
        {
            var ignore = new string[] { "rowversion", "mailsenderlog", "sitesettings" };
            Action<string, NancyBlackDatabase, string, dynamic> genericHandler = (action, arg1, arg2, arg3) =>
            {
                if (ignore.Contains( arg2 ))
                {
                    return;
                }

                if (_Events == null)
                {
                    _Events = new ConcurrentBag<DatabaseEvent>();
                }

                _Events.Add(new DatabaseEvent()
                {
                    Action = action,
                    AffectedRow = (object)arg3,
                    Database = arg1,
                    DataTypeName = arg2
                });
            };

            NancyBlackDatabase.ObjectCreated += (a, b, c) => genericHandler("create", a, b, c);
            NancyBlackDatabase.ObjectUpdated += (a, b, c) => genericHandler("update", a, b, c);
            NancyBlackDatabase.ObjectDeleted += (a, b, c) => genericHandler("deleted", a, b, c);
        }
                
        public DataWatcherModule()
        {
            Get["/Admin/DataWatcher"] = this.HandleStaticRequest("admin-datawatcher", null);
        }
                
        public void Hook(IPipelines p)
        {
            p.AfterRequest.AddItemToStartOfPipeline((ctx) =>
            {
                if (_Events == null)
                {
                    return;
                }

                var siteconfig = ctx.Items["CurrentSite"] as JObject;
                var user = ctx.CurrentUser as NcbUser;

                var events = _Events.ToList();
                _Events = null;
                
                if (siteconfig.Property("watcher") == null)
                {
                    return; // not configured
                }

                var watcher = siteconfig.Property("watcher").Value as JObject;

                Task.Run(() =>
                {
                    foreach (var item in events)
                    {
                        var datatype = watcher.Property( item.DataTypeName.ToLowerInvariant() );
                        if (datatype == null)
                        {
                            continue;
                        }

                        var config = datatype.Value.ToObject<WatcherConfig>();
                        if (config.version == true)
                        {
                            item.Database.UpsertRecord(new RowVersion()
                            {
                                Action = item.Action,
                                js_Row = JsonConvert.SerializeObject(item.AffectedRow),
                                UserId = user.Id,
                                __createdAt = DateTime.Now,
                                RowId = (int)item.AffectedRow.Id,
                                DataType = item.DataTypeName
                            });
                        }

                        var emailConfig = config[item.Action];
                        if (emailConfig.enable)
                        {
                            var subject = Razor.Parse<DatabaseEvent>(emailConfig.emailSubject, item);
                            var body = Razor.Parse<DatabaseEvent>(emailConfig.emailTemplate, item);

                            MailSenderModule.SendEmail(emailConfig.sendTo, subject, body);
                        }
                    }
                });

            });
        }
    }
}