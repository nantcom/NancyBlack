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

        public void NewContosoChatMessage(string name, string message)
        {
            Clients.All.addNewMessageToPage(name, message);
            Clients.All.notify();
        }

        public void NotifyClientForPaymentReceipt(dynamic args)
        {            
            this._Context.Clients.All.genPaymentReceipt(args);
            this._Context.Clients.All.notify();
        }


    }
}