using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.FacebookMessengerSystem.Types
{
    public class FacebookWebhookRequest : TableEntity
    {
        /// <summary>
        /// Method of the webhook
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Payload sent to webhook
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// PSID that we are chatting with
        /// </summary>
        public string PSID { get; set; }
    }
}