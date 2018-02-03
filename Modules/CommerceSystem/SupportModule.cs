using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.AdminSystem.Types;
using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json;
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
            Post["/support/{saleOrderIdentifier}"] = this.HandleRequest(HandleSupportPage);

            Post["/support/{saleOrderIdentifier}/save/attachment/message"] = this.HandleRequest(HandleCustomerSaveAttachmentMessage);
            //Post["/support/login"] = this.HandleRequest(HandleSupportLogin);
            Post["/support/notify/payment"] = this.HandleRequest(HandleNotifyForPayment);
        }

        private dynamic HandleSupportPage(dynamic arg)
        {
            var id = (string)arg.saleOrderIdentifier;
            var so = this.SiteDatabase.Query<SaleOrder>()
                        .Where(row => row.SaleOrderIdentifier == id)
                        .FirstOrDefault();

            if (so == null)
            {
                return 404;
            }

            so.SiteSettings = null;

            Func<dynamic> response = () =>
            {
                // after 30 days, if no payment - we cancel it
                var isExpired = so.PaymentStatus == PaymentStatus.WaitingForPayment
                        && so.Status == SaleOrderStatus.Confirmed
                        && (so.__createdAt.AddDays(30) < DateTime.Now);

                if (isExpired)
                {
                    so.Status = SaleOrderStatus.Cancel;
                    this.SiteDatabase.UpsertRecord(so);
                }

                var statusList = StatusList.GetAllStatus<SaleOrderStatus>();
                var paymentStatusList = StatusList.GetAllStatus<PaymentStatus>();
                var dummyPage = new Page();
                var data = new
                {
                    StatusList = statusList,
                    PaymentStatusList = paymentStatusList,
                    SaleOrder = so,
                    PaymentLogs = so.GetPaymentLogs(this.SiteDatabase)
                };

                return View["commerce-support", new StandardModel(this, dummyPage, data)];
            };
            

            if (this.Request.Form.key != null)
            {
                string key = ((string)this.Request.Form.key).Trim();
                if (so.Customer != null &&
                    (so.Customer.PhoneNumber == key ||
                     so.Customer.Email == key))
                {
                    return response();
                }
            }

            if (this.CurrentUser.HasClaim("Admin") &&
                this.Request.Url.HostName.StartsWith( "local." ) == false)
            {
                return response();
            }

            if (this.CurrentUser.IsAnonymous)
            {
                return View["commerce-support-login", new StandardModel(this, new Page(), new object())];
            }

            // check whether this user has bought this sale order
            if (so.Customer == null || so.Customer.User == null)
            {
                return View["commerce-support-login", new StandardModel(this, new Page(), new object())];
            }

            if (so.Customer.User.Id != this.CurrentUser.Id)
            {
                return View["commerce-support-login", new StandardModel(this, new Page(), new object())];
            }

            return response();
        }

        private dynamic HandleCustomerSaveAttachmentMessage(dynamic arg)
        {
            var para = arg.body.Value as JObject;
            var id = (string)arg.saleOrderIdentifier;
            var so = this.SiteDatabase.Query<SaleOrder>()
                        .Where(row => row.SaleOrderIdentifier == id)
                        .FirstOrDefault();

            if (so == null)
            {
                return 404;
            }

            foreach (JObject item in so.Attachments)
            {
                if (item.Value<string>("Url") == para.Value<string>("Url"))
                {
                    item["Caption"] = para.Value<string>("Message");
                    break;
                }
            }

            this.SiteDatabase.UpsertRecord(so);

            return 200;
        }

        private dynamic HandleNotifyForPayment(dynamic arg)
        {
            var id = ((JObject)arg.Body.Value).Value<string>("SaleOrderIdentifier");
            var so = this.SiteDatabase.Query<SaleOrder>()
                        .Where(row => row.SaleOrderIdentifier == id)
                        .FirstOrDefault();

            if (so == null)
            {
                return 404;
            }

            var siteSettings = this.SiteDatabase.Query<SiteSettings>().LastOrDefault();
            var setting = JsonConvert.DeserializeObject<JObject>(siteSettings.js_SettingsJson);

            MailSenderModule.SendEmail(
                setting.Value<JObject>("smtp").Value<string>("fromEmail"),
                "Please Check Payment for Id : " + so.Id,
                string.Format(@"Site: {2} <br/>Please check customer payment:<br/>
                      <a href=""http://{0}/support/{1}"">http://{0}/support/{1}</a>",
                      this.Request.Url.HostName, so.SaleOrderIdentifier, setting.Value<JObject>("paysbuy").Value<string>("itm")));

            return 200;
        }

        //private dynamic HandleSupportLogin(dynamic arg)
        //{
        //    var input = arg.body.Value as JObject;
        //    var so = this.SiteDatabase.Query<SaleOrder>()
        //                .Where(row => row.SaleOrderIdentifier == input.Value<string>("SaleOrderId"))
        //                .FirstOrDefault();

        //    if (((JObject)so.Customer).Value<string>("Email") == input.Value<string>("Email"))
        //    {

        //    }
        //    else
        //    {
        //        return 403;
        //    }

        //    return 200;
        //}
    }
}