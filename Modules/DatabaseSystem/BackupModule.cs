using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public class BackupModule : BaseModule
    {
        public BackupModule()
        {
            Post["/Admin/DatabaseSystem/__backup"] = this.HandleRequest( (arg)=> {

                if ( this.Request.Headers["key"].FirstOrDefault() != (string)this.CurrentSite.backup.key )
                {
                    return 403;
                }

                var parameters = arg.Body.Value as JObject;

                Task.Run(() =>
                {
                    this.SiteDatabase.PerformBackup(parameters);

                });

                return 200;
            });
        }
    }
}