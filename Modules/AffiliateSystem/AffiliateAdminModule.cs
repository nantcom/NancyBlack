using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules;
using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using NantCom.NancyBlack.Modules.CommerceSystem;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using Nancy.Security;

namespace NantCom.NancyBlack.Modules.AffiliateSystem
{
    public class AffiliateAdminModule : BaseModule
    {
        public AffiliateAdminModule()
        {
            this.RequiresClaims("admin");


            Get["/Admin/affiliate/withdraw"] = this.HandleViewRequest("affiliateadmin-withdraw", (arg) =>
           {
               var model = new StandardModel(this);

               model.Data = new
               {
                   Pending = this.SiteDatabase.Query("SELECT AffiliateCode, SUM(BTCAmount) As Amount, SUM(AlternateBTCAmount) as AlternateAmount, IsUsingAlternateRate FROM AffiliateTransaction WHERE IsPendingApprove = 1 GROUP BY AffiliateCode",
                               new { AffiliateCode = "", Amount = 0M, AlternateAmount = 0M, IsUsingAlternateRate = false }),

               };

               return model;
               
           });
        }
    }
}