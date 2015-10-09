using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using System.IO;
using NantCom.NancyBlack.Modules.TrackingSystem.types;

namespace NantCom.NancyBlack.Modules.TrackingSystem
{
    public class EmailTrackingModule : NancyBlack.Modules.BaseModule
    {
        public EmailTrackingModule ()
        {
            Get["/__trackingimage"] = this.HandleRequest(HandleTrackingImage);
        }

        private dynamic HandleTrackingImage(dynamic arg)
        {

            var queryString = this.Request.Query;

            var email = queryString["email"];

            var newTracking = new NcbMailingReceiverLog()
            {
                Email = email,
            };

            this.SiteDatabase.UpsertRecord<NcbMailingReceiverLog>(newTracking);

            var imageAtSitePath = this.RootPath + "/Site/trackingimage.png";

            if ( File.Exists(imageAtSitePath) )
            {                
                return this.Response.AsImage(imageAtSitePath);
            }
            return this.Response.AsImage("Modules/MailingTrackingSystem/images/transparent.png");
        }
    }
}