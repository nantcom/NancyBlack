﻿using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Nancy;
using Nancy.Bootstrapper;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using NantCom.NancyBlack.Modules.DataWatcherSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Caching;
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

                public WatcherAutoGeneratePDF autoGeneratePDF { get; set; }

            }            

            public class WatcherAutoGeneratePDF
            {
                public bool enable { get; set; }

                public string printformUrl { get; set; }

                public string uploadFileUrl { get; set; }
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
            /// Auto print latest attachment enabled
            /// </summary>
            public bool autoPrintAttachment { get; set; }

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

                    if (action == "deleted")
                    {
                        return this.deleted;
                    }

                    if (action == "newAttachments")
                    {
                        return new WatcherEmailNotifyConfig() { };
                    }

                    throw new NotImplementedException(action + " was not implemented.");
                }
            }
        }

        private static ConcurrentBag<DatabaseEvent> _Events;

        public static bool readyToSendMail = true;

        static DataWatcherModule()
        {
            var ignore = new string[] { "rowversion", "mailsenderlog", "sitesettings", "pageview", "pageviewsummary", "newAttachments" };

            // this handler will handle database event from NancyBlack Database system
            // and it will add information of all database actions in the request into buffer

            Action<string, NancyBlackDatabase, string, dynamic> genericHandler = (action, arg1, arg2, arg3) =>
            {
                if (action == "newAttachments")
                {
                    return;
                }

                if (ignore.Contains( arg2.ToLowerInvariant() ))
                {
                    return;
                }

                if (_Events == null)
                {
                    _Events = new ConcurrentBag<DatabaseEvent>();
                }

                if (!readyToSendMail)
                {
                    return;
                }

                _Events.Add(new DatabaseEvent()
                {
                    Action = action,
                    AffectedRow = JObject.FromObject(arg3),
                    Database = arg1,
                    DataTypeName = arg2
                });
            };

            NancyBlackDatabase.ObjectCreated += (a, b, c) => genericHandler("create", a, b, c);
            NancyBlackDatabase.ObjectUpdated += (a, b, c) => genericHandler("update", a, b, c);
            NancyBlackDatabase.ObjectDeleted += (a, b, c) => genericHandler("deleted", a, b, c);
            BaseDataModule.NewAttachments += (ctx, entityName, row, newFiles) => {

                JObject ContentObject = JObject.Parse(row.ToString());

                var result = (from Attachment in ContentObject["Attachments"]
                              orderby Attachment["CreateDate"] descending
                              select Attachment["Url"]).FirstOrDefault().ToString();

                genericHandler("newAttachments", ctx.GetSiteDatabase(), entityName, result);

            };
        }

        public DataWatcherModule()
        {
            Get["/Admin/DataWatcher"] = this.HandleViewRequest("admin-datawatcher", null);
        }
                
        public void Hook(IPipelines p)
        {
            // Use Pipeline Hook so that we can get information about the requests before performing action
            // with the data

            p.AfterRequest.AddItemToStartOfPipeline((ctx) =>
            {
                if (_Events == null)
                {
                    return;
                }

                var siteconfig = ctx.Items["CurrentSite"] as JObject;
                var user = ctx.CurrentUser as NcbUser;
                // this will not work when more than one person updating the site at the same time

                var events = _Events.ToList();
                _Events = null;
                
                if (siteconfig.Property("watcher") == null)
                {
                    return; // not configured
                }

                var watcher = siteconfig.Property("watcher").Value as JObject;
                var userIP = ctx.Request.UserHostAddress;

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

                        if (item.Action == "newAttachments")
                        {
                            if (config.autoPrintAttachment == true)
                            {
                                new DataWatcherHub().PrintDocument(item.AffectedRow);
                            }

                            continue;
                        }

                        if (config.version == true)
                        {
                            var version = new RowVersion()
                            {
                                Action = item.Action,
                                RowData = item.AffectedRow,
                                UserId = user.Id,
                                UserHostAddress = userIP,
                                __createdAt = DateTime.Now,
                                RowId = (int)item.AffectedRow.Id,
                                DataType = item.DataTypeName
                            };

                            item.Database.UpsertRecord(version);

                        }

                        var emailConfig = config[item.Action];
                        if (emailConfig != null)
                        {
                            var autoGenPdfConfig = emailConfig.autoGeneratePDF;
                            if (autoGenPdfConfig != null && autoGenPdfConfig.enable == true)
                            {
                                Object dataToClient = new
                                {
                                    config = autoGenPdfConfig,
                                    data = item.AffectedRow,
                                };
                                new DataWatcherHub().GenPDFandUpload(dataToClient);
                            }

                            if (emailConfig.enable)
                            {
                                var subject = Razor.Parse<DatabaseEvent>(emailConfig.emailSubject, item);
                                var body = Razor.Parse<DatabaseEvent>(emailConfig.emailTemplate, item);

                                MailSenderModule.SendEmail(emailConfig.sendTo, subject, body);
                            }
                        }


                    }
                });

            });
        }

    }
}