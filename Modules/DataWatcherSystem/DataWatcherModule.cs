using Nancy;
using Nancy.Bootstrapper;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public class DataWatcherModule : BaseModule, IPipelineHook
    {
        static DataWatcherModule()
        {
            NancyBlackDatabase.ObjectCreated += NancyBlackDatabase_ObjectCreated;
            NancyBlackDatabase.ObjectDeleted += NancyBlackDatabase_ObjectDeleted;
            NancyBlackDatabase.ObjectUpdated += NancyBlackDatabase_ObjectUpdated;
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

        private static void NancyBlackDatabase_ObjectUpdated(NancyBlackDatabase arg1, string arg2, dynamic arg3)
        {
        }

        private static void NancyBlackDatabase_ObjectDeleted(NancyBlackDatabase arg1, string arg2, dynamic arg3)
        {
        }
        
        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string entityName, dynamic value)
        {
        }
        
        /// <summary>
        /// Logs the action on database
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="inputObject"></param>
        private static void Log( string entityName, string action, int id, object inputObject, int byUserId)
        {
            //_db.Insert(new RowVersion()
            //{
            //    UserId = byUserId,
            //    Action = action,
            //    js_Row = JsonConvert.SerializeObject(inputObject),
            //    RowId = id,
            //    DataType = dt.NormalizedName,
            //    __createdAt = DateTime.Now,
            //    __updatedAt = DateTime.Now,
            //    __version = DateTime.Now.Ticks.ToString(),
            //});
        }

        public void Hook(IPipelines p)
        {
            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                // process log
                string s = "";
            });
        }
    }
}