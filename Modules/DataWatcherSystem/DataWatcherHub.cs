using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.DataWatcherSystem
{
    public class DataWatcherHub : Hub
    {

        private IHubContext _Context;

        public DataWatcherHub()
        {
            this._Context = GlobalHost.ConnectionManager
                                        .GetHubContext<DataWatcherHub>();
        }

        public void PrintDocument(dynamic UrlToFileName)
        {
            this._Context.Clients.All.printDocument(UrlToFileName);
        }

        public void GenPDFandUpload(dynamic args)
        {            
            this._Context.Clients.All.genPDFandUpload(args);            
        }


    }
}