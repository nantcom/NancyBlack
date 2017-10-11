using NantCom.NancyBlack.Modules.DatabaseSystem;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MailingListSystem
{
    public class MailingListModule : BaseModule
    {
        static MailingListModule()
        {
            // mailchimp api key
            // 295bd1448b335908c90f7429b4ca04c1-us16

            NancyBlackDatabase.ObjectCreated += NancyBlackDatabase_ObjectCreated;

        }

        private static void NancyBlackDatabase_ObjectCreated(NancyBlackDatabase db, string table, dynamic row)
        {
            if (table == "NcbMailingListSubscription")
            {
                MailingListModule.AddToMailChimp(db, "a8d878538c", row);
            }
        }

        /// <summary>
        /// Adds the subscription to mailchimp
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="sub"></param>
        /// <returns></returns>
        private static bool AddToMailChimp( NancyBlackDatabase db, string listId, NcbMailingListSubscription item )
        {
            var key = "295bd1448b335908c90f7429b4ca04c1-us16";
            var server = string.Format("https://{0}.api.mailchimp.com/3.0/", key.Substring(key.IndexOf("-") + 1));

            RestClient c = new RestClient(server);
            c.Authenticator = new HttpBasicAuthenticator("level51", key);

            var all = db.Query<NcbMailingListSubscription>().ToList();

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

        public MailingListModule()
        {
            Get["/__mailinglist/mailchimpexport/{listid}"] = this.HandleRequest((arg) =>
            {
                var all = this.SiteDatabase.Query<NcbMailingListSubscription>().ToList();
                var errors = new List<string>();

                foreach (var item in all)
                {
                    MailingListModule.AddToMailChimp(this.SiteDatabase, arg.listid, item);
                }

                if (errors.Count > 0)
                {
                    return errors;
                }

                return "OK";
            });
        }
    }
}