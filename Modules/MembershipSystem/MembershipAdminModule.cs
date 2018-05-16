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
using NantCom.NancyBlack.Modules.LogisticsSystem.Types;
using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using NantCom.NancyBlack.Modules.CommerceSystem.types;

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

            Get["/Admin/Member/{id}"] = this.HandleRequest(this.HandleMemberDetailPage);
            Get["/Admin/Member/Readjust/SaleOrder"] = this.HandleRequest(this.HandleRecordNcbUserInSaleOrder);

        }

        private dynamic HandleMemberDetailPage(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }
            
            var member = this.SiteDatabase.GetById<NcbUser>((int)arg.id);

            var dummyPage = new Page();

            var data = new
            {
                Member = member,
                LogisticsCompanies = this.SiteDatabase.Query<LogisticsCompany>().ToList(),
                AffiliateRewardsClaims = AffiliateRewardsClaim.GetRewards(this.SiteDatabase, member.Id),
                AffiliateDiscountCodes = AffiliateRewardsClaim.GetDiscountCodes(this.SiteDatabase, member.Id),
                PurchaseHistory = SaleOrder.GetFromNcbUserId(member.Id, this.SiteDatabase)
            };


            return View["Admin/memberprofile-admin", new StandardModel(this, dummyPage, data)];
        }

        private dynamic HandleRecordNcbUserInSaleOrder(dynamic arg)
        {
            if (!this.CurrentUser.HasClaim("admin"))
            {
                return 403;
            }

            var allSO = this.SiteDatabase.Query<SaleOrder>();
            foreach (var so in allSO)
            {
                if (so.Customer == null || so.Customer.User == null || so.Customer.User.Id == 0)
                {
                    continue;
                }

                if (so.Id == 1177)
                    continue;

                so.NcbUserId = so.Customer.User.Id;
                this.SiteDatabase.UpsertRecord(so);
            }

            return 200;
        }

    }


}