using Nancy;
using Nancy.ModelBinding;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace NantCom.NancyBlack.Modules.MembershipSystem
{
    public class MembershipAdminModule : NancyBlack.Modules.BaseModule
    {

        public MembershipAdminModule()
        {
            this.RequiresClaims("admin");

            Get["/Admin/Membership"] = p =>
            {
                return View["Admin/membership-admin", new StandardModel(this)];
            };

        }
        
    }


}