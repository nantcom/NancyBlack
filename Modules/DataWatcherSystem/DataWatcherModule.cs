using Nancy;
using Nancy.Bootstrapper;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public static class DataWatcherModule
    {
        private class WatchTask
        {
            /// <summary>
            /// Entities to watch
            /// </summary>
            public string Entities { get; set; }
            
            /// <summary>
            /// Site for this task
            /// </summary>
            public dynamic Site { get; set; }
        }


        static DataWatcherModule()
        {
            NancyBlackDatabase.ObjectCreated += NancyBlackDatabase_ObjectCreated;
        }

        private static Dictionary<NancyBlackDatabase, WatchTask> _WatchTasks = new Dictionary<NancyBlackDatabase, WatchTask>();

        static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string entityName, dynamic value)
        {
            WatchTask task;
            if (_WatchTasks.TryGetValue( db, out task ) == true)
            {
                if (task.Entities.IndexOf( entityName + ",", StringComparison.InvariantCultureIgnoreCase ) >= 0)
                {
                    try
                    {
                        MailSenderModule.SendEmail(task.Site,
                            (string)task.Site.DataWatcher_WatchEmail,
                            "Object was Created in table: " + entityName,
                            "There was a new object created in table: " + entityName);

                        db.UpsertRecord("DataWatcherLog", new
                        {
                            Id = 0,
                            Title = "Mail Sent for Entity:" + entityName,
                            Message = "Success."
                        });
                    }
                    catch (Exception ex)
                    {
                        db.UpsertRecord("DataWatcherLog", new
                        {
                            Id = 0,
                            Title = "Mail Failed to Send",
                            Message = "Failed." + ex.Message
                        });
                    }
                }
            }
        }

        public static void Initialize(IPipelines pipelines )
        {

            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (ctx.Items.ContainsKey("CurrentSite") == false)
                {
                    return;
                }

                dynamic site = ctx.Items["CurrentSite"];
                var siteDb = ctx.Items["SiteDatabase"] as NancyBlackDatabase;

                if (site.DataWatcher_Tables != null)
                {
                    if (_WatchTasks.ContainsKey( siteDb ) == false)
                    {
                        // create watch tasks                        
                        _WatchTasks.Add(siteDb, new WatchTask());

                        // register our type
                        siteDb.DataType.Register(siteDb.DataType.FromJson("DataWatcherLog", "{ \"Title\" : \"\", \"Message\" : \"\"}"));
                    }

                    // Make sure we use latest settings
                    _WatchTasks[siteDb].Site = site;
                    _WatchTasks[siteDb].Entities = site.DataWatcher_Tables + ",";
                }

            });

        }
    }
}