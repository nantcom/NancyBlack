using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class SupportModule : BaseModule
    {
        public SupportModule()
        {
            Get["/support/{saleOrderIdentifier}"] = this.HandleRequest(HandleSupportPage);
            Post["/support/login"] = this.HandleRequest(HandleSupportLogin);
        }

        private dynamic HandleSupportPage(dynamic arg)
        {
            var id = (string)arg.saleOrderIdentifier;
            var so = this.SiteDatabase.Query<SaleOrder>()
                        .Where(row => row.SaleOrderIdentifier == id)
                        .FirstOrDefault();

            //return View["commerce-login-support", new StandardModel(200)];


            var statusList = (typeof(SaleOrderStatus)
                        .GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Where(f => f.FieldType == typeof(string)).Select(f => (string)f.GetValue(null))).ToList();

            var dummyPage = new Page();

            var data = new { StatusList = statusList, SaleOrder = so };

            return View["commerce-support", new StandardModel(this, dummyPage, data)];
        }

        private dynamic HandleSupportLogin(dynamic arg)
        {
            var input = arg.body.Value as JObject;
            var so = this.SiteDatabase.Query<SaleOrder>()
                        .Where(row => row.SaleOrderIdentifier == input.Value<string>("SaleOrderId"))
                        .FirstOrDefault();

            if (((JObject)so.Customer).Value<string>("Email") == input.Value<string>("Email"))
            {

            }
            else
            {
                return 403;
            }

            return 200;
        }
    }
}