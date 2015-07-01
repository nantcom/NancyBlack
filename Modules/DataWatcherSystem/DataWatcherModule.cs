using Nancy;
using Nancy.Bootstrapper;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private class DatabaseEvent
        {
            public string Action { get; set; }

            public NancyBlackDatabase Database { get; set; }

            public string DataTypeName { get; set; }

            public dynamic AffectedRow { get; set; }
        }

        private static ConcurrentBag<DatabaseEvent> _Events = new ConcurrentBag<DatabaseEvent>();

        static DataWatcherModule()
        {
            NancyBlackDatabase.ObjectCreated += NancyBlackDatabase_ObjectCreated;
            NancyBlackDatabase.ObjectDeleted += NancyBlackDatabase_ObjectDeleted;
            NancyBlackDatabase.ObjectUpdated += NancyBlackDatabase_ObjectUpdated;
        }

        private static void NancyBlackDatabase_ObjectUpdated(NancyBlackDatabase arg1, string arg2, dynamic arg3)
        {
            if (arg2 == "rowversion")
            {
                return;
            }

            _Events.Add(new DatabaseEvent()
            {
                Action = "update",
                AffectedRow = (object)arg3,
                Database = arg1,
                DataTypeName = arg2
            });
        }

        private static void NancyBlackDatabase_ObjectDeleted(NancyBlackDatabase arg1, string arg2, dynamic arg3)
        {
            if (arg2 == "rowversion")
            {
                return;
            }

            _Events.Add(new DatabaseEvent()
            {
                Action = "delete",
                AffectedRow = (object)arg3,
                Database = arg1,
                DataTypeName = arg2
            });
        }

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase arg1, string arg2, dynamic arg3)
        {
            if (arg2 == "rowversion")
            {
                return;
            }

            _Events.Add(new DatabaseEvent()
            {
                Action = "created",
                AffectedRow = (object)arg3,
                Database = arg1,
                DataTypeName = arg2
            });
        }
        
        public DataWatcherModule()
        {
            Get["/Admin/DataWatcher"] = this.HandleStaticRequest("admin-datawatcher", null);
        }
        
        private void SendEmail( string entityName, string email)
        {
            try
            {
                MailSenderModule.SendEmail( this.CurrentSite,
                    email,
                    "Object was Created in table: " + entityName,
                    "There was a new object created in table: " + entityName);
            }
            catch (Exception ex)
            {
            }
        }
        
        public void Hook(IPipelines p)
        {
            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                var db = ctx.Items["SiteDatabase"];
                var events = _Events.ToList();
                var user = ctx.CurrentUser as NancyBlackUser;

                _Events = new ConcurrentBag<DatabaseEvent>();

                Task.Run(() =>
                {
                    foreach (var item in events)
                    {
                        item.Database.UpsertStaticRecord("rowversion", new RowVersion()
                        {
                            Action = item.Action,
                            js_Row = JsonConvert.SerializeObject( item.AffectedRow ),
                            UserId = user.Id,
                            __createdAt = DateTime.Now,
                            RowId = (int)item.AffectedRow.Id,
                            DataType = item.DataTypeName
                        });

                        // send email
                    }
                });

            });
        }
    }
}