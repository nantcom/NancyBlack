using Nancy;
using Nancy.Security;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.EditorSystem
{
    public class EditorModule : BaseModule
    {
        public EditorModule()
        {
            this.RequiresAnyClaim("admin", "editor");
            
            Get["/__editor"] = this.HandleRequest((arg) =>
            {
                return View["editor-editframe", new StandardModel(this)];
            });

            Get["/__editor/data/availablelayouts"] = this.HandleRequest((args) =>
            {
                dynamic site = this.Context.Items["CurrentSite"];
                var viewPath = Path.Combine(this.RootPath, "Site", "Views");
                var views = Directory.GetFiles(viewPath, "*.cshtml", SearchOption.AllDirectories);

                var userViews = from view in views
                       let viewName = view.Replace(viewPath + "\\", "").Replace("\\", "/").Replace(".cshtml", "")
                       where 
                            viewName.StartsWith("Admin/") == false &&
                            viewName.StartsWith("_") == false &&
                            viewName.StartsWith("admin-") == false
                        select viewName;

                return userViews.Distinct();

            });

            Post["/__editor/updateorder"] = this.HandleRequest(this.UpdateContentOrder);
            
        }
        

        private dynamic UpdateContentOrder(dynamic arg)
        {
            dynamic command = arg.Body.Value;

            JArray ids = command.ids;
            string table = command.table;

            // received is the list of ids to set display order
            for (int i = 0; i < ids.Count; i++)
            {
                dynamic item = this.SiteDatabase.GetByIdAsJObject(table, (int)ids[i]);
                item.DisplayOrder = i;

                this.SiteDatabase.UpsertRecord(table, item);
            }

            return 200;
        }
    }
}