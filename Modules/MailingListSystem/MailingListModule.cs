using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace NantCom.NancyBlack.Modules.MailingListSystem
{
    public class MailingListModule : BaseModule
    {

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string table, dynamic row)
        {
            if (table == "NcbMailingListSubscription")
            {
                var settings = AdminModule.ReadSiteSettings();
                var listId = settings.commerce.mailchimp.listid;
                var key = settings.commerce.mailchimp.apikey;

                Task.Run(() =>
                {
                    MailingListModule.AddToMailChimp(key, listId, row);
                });
            }
        }

        /// <summary>
        /// Adds the subscription to mailchimp
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="sub"></param>
        /// <returns></returns>
        private static bool AddToMailChimp( string key, string listId, NcbMailingListSubscription item )
        {
            var server = string.Format("https://{0}.api.mailchimp.com/3.0/", key.Substring(key.IndexOf("-") + 1));

            RestClient c = new RestClient(server);
            c.Authenticator = new HttpBasicAuthenticator("level51", key);
            
            RestRequest req = new RestRequest("/lists/" + listId + "/members/", Method.POST);
            req.AddJsonBody(new
            {
                email_address = item.Email,
                status = "subscribed",
                merge_fields = new
                {
                    FNAME = item.FirstName,
                    LNAME = item.LastName
                }
            });

            var result = c.Execute(req);
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return false;
            }

            return true;
        }

        private dynamic SyncMailchimp(dynamic arg)
        {
            var errors = new List<string>();
            string key = AdminModule.ReadSiteSettings().commerce.mailchimp.apikey;
            string memberListId = this.CurrentSite.commerce.mailchimp.listid;
            string customerListId = this.CurrentSite.commerce.mailchimp.customerlistid;

            var all = this.SiteDatabase.Query<NcbMailingListSubscription>().OrderByDescending(u => u.Id).ToList();
            foreach (var item in all)
            {
                MailingListModule.AddToMailChimp(key, memberListId, item);
            }
            
            var allCustomers = this.SiteDatabase.Query<SaleOrder>()
                                .Where( so => so.PaymentStatus == PaymentStatus.PaymentReceived)
                                .OrderByDescending(so => so.Id)
                                .ToList();

            foreach (var item in allCustomers)
            {
                if (item.Customer == null)
                {
                    continue;
                }

                MailingListModule.AddToMailChimp(key, customerListId, new NcbMailingListSubscription()
                {
                    FirstName = item.Customer.FirstName,
                    LastName = item.Customer.LastName,
                    Email = item.Customer.Email
                });
            }

            if (errors.Count > 0)
            {
                return errors;
            }

            return "OK";
        }

        public MailingListModule()
        {
            Get["/__mailinglist/sync"] = this.HandleRequest(this.SyncMailchimp);
        }
    }
}