using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Site.Module.Types;
using Newtonsoft.Json.Linq;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace NantCom.NancyBlack.LoaderIOSystem
{
    public class LoaderIOModule : BaseModule
    {
        public LoaderIOModule()
        {
            Get["/loaderio-{key}/"] = this.HandleRequest((arg) =>
            {
                if (this.CurrentSite.loaderio == null)
                {
                    return 404;
                }

                if ((string)this.CurrentSite.loaderio.key == (string)arg.key)
                {
                    return "loaderio-" + arg.key;
                }

                return 404;
            });
        }
    }
}