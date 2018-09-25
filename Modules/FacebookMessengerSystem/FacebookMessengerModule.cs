using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Nancy;
using NantCom.NancyBlack.Modules.FacebookMessengerSystem.Types;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace NantCom.NancyBlack.Modules.FacebookMessengerSystem
{
    public class FacebookMessengerModule : BaseModule
    {
        private Dictionary<string, Func<dynamic>> _Handlers;

        public FacebookMessengerModule()
        {
            Get["/__fbmp/webhook"] = this.HandleRequest(this.WebhookGet);

            Post["/__fbmp/webhook"] = this.HandleRequest(this.WebhookPost);
        }

        /// <summary>
        /// Gets the Tracing Table
        /// </summary>
        /// <returns></returns>
        private CloudTable GetTracingTable(bool cache = true)
        {
            if (this.CurrentSite.FacebookMessenger.TracingServer == null)
            {
                return null;
            }

            Func<CloudTable> getTable = () =>
            {
                var cred = new StorageCredentials((string)this.CurrentSite.FacebookMessenger.TracingTableCredentials);
                var client = new CloudTableClient(new Uri((string)this.CurrentSite.FacebookMessenger.TracingServer), cred);
                return client.GetTableReference((string)this.CurrentSite.FacebookMessenger.TracingTableName);
            };

            if (cache == false)
            {
                return getTable();
            }

            var key = string.Format("azure{0}-{1}-{2}",
                               (string)this.CurrentSite.FacebookMessenger.TracingTableCredentials,
                               (string)this.CurrentSite.FacebookMessenger.TracingServer,
                               (string)this.CurrentSite.FacebookMessenger.TracingTableName).GetHashCode().ToString();

            var table = MemoryCache.Default[key] as CloudTable;
            if (table == null)
            {
                table = getTable();

                MemoryCache.Default.Add(key, table, DateTimeOffset.Now.AddDays(1));
            }

            return table;
        }

        private static Dictionary<string, object> _SyncLock = new Dictionary<string, object>();

        private dynamic WebhookPost(dynamic args)
        {
            dynamic body = args.body.Value;

#if DEBUG
#else
            var table = this.GetTracingTable();
            if (table != null)
            {
                var now = DateTime.Now;
                var trace = new FacebookWebhookRequest();
                trace.PartitionKey = now.ToString("yyyy-MM-dd");
                trace.RowKey = now.ToString("HH-mm|") + now.Ticks;
                trace.Method = "POST";
                trace.Payload = ((JObject)body).ToString();

                var op = TableOperation.Insert(trace);
                table.Execute(op);
            }
#endif

            if (body["object"] == "page")
            {
                foreach (dynamic entry in body.entry as JArray)
                {
                    if (entry.messaging == null)
                    {
                        continue;
                    }

                    // Gets the body of the webhook event
                    var message = entry.messaging[0];

                    // Get the sender PSID
                    string customerPSID = message.sender.id;
                    bool willHandle = true;

                    if (customerPSID == (string)entry.id) // this is message sent by bot 
                    {
                        willHandle = false;
                        customerPSID = message.recipient.id; // so the cusotmer psid is recipient
                    }

                    object locker = null;

                    lock (_SyncLock)
                    {
                        if (_SyncLock.TryGetValue(customerPSID, out locker) == false)
                        {
                            _SyncLock[customerPSID] = new object();
                        }
                    }

                    lock (_SyncLock[customerPSID])
                    {
                        // try to get the current chat session
                        var existingSession = MemoryCache.Default["FBMP:" + customerPSID] as FacebookChatSession;
                        if (existingSession == null)
                        {
                            existingSession = this.SiteDatabase.Query<FacebookChatSession>().Where(u => u.PageScopedId == customerPSID).FirstOrDefault();

                            if (existingSession == null)
                            {
                                existingSession = new FacebookChatSession();
                                MemoryCache.Default["FBMP:" + customerPSID] = existingSession;

                                existingSession.PageScopedId = customerPSID;

                                // no prior session - see if user have logged in with us before
                                var result = FacebookMessengerModule.FacebookApiGet(this.CurrentSite, string.Format("/{0}/ids_for_apps", customerPSID), true);
                                string appId = result[0].id;

                                var existingUser = this.SiteDatabase.Query<NcbUser>().Where(u => u.FacebookAppScopedId == appId).FirstOrDefault();
                                if (existingUser != null)
                                {
                                    existingSession.NcbUserId = existingUser.Id;
                                }
                            }
                        }

                        if (willHandle)
                        {
                            existingSession.HandleWebhook(this.SiteDatabase, this.CurrentSite, message);
                        }
                        else
                        {
                            if ( existingSession.Messages == null )
                            {
                                existingSession.Messages = new List<dynamic>();
                            }
                            existingSession.Messages.Add(message);

                            this.SiteDatabase.UpsertRecord(existingSession);
                        }
                    }


                }

                return "EVENT_RECEIVED";
            }

            return 404;
        }

        private dynamic WebhookGet(dynamic args)
        {

#if DEBUG
            if (this.CurrentSite.Application == null)
            {
                throw new InvalidOperationException("Application Settings was not initialized.");
            }

            if (this.CurrentSite.FacebookMessenger == null)
            {
                throw new InvalidOperationException("Facebook Messenger Settings was not initialized.");
            }

            if (this.CurrentSite.Application.FacebookAppId == null)
            {
                throw new InvalidOperationException("FacebookAppId Settings was not initialized.");
            }

            if (this.CurrentSite.Application.FacebookAppSecret == null)
            {
                throw new InvalidOperationException("FacebookAppSecret Settings was not initialized.");
            }
#endif

            if (_Handlers == null)
            {
                _Handlers = new Dictionary<string, Func<dynamic>>();
                _Handlers["subscribe"] = () =>
                {
                    if (this.CurrentSite.FacebookMessenger == null)
                    {
                        throw new InvalidOperationException("Facebook Messenger Settings was not initialized.");
                    }

                    if ((string)this.Request.Query["hub.verify_token"] == (string)this.CurrentSite.FacebookMessenger.VerifyToken)
                    {
                        return (string)this.Request.Query["hub.challenge"];
                    }

                    return 403;
                };
            }

            Func<dynamic> handler;
            if (_Handlers.TryGetValue( (string)this.Request.Query["hub.mode"], out handler ))
            {
                return handler();
            }

            return 400;
        }

        /// <summary>
        /// PHP Compatible hash_hmac from : https://stackoverflow.com/questions/12804231/c-sharp-equivalent-to-hash-hmac-in-php
        /// </summary>
        /// <param name="message"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static string HashHmac(string message, string secret)
        {
            Encoding encoding = Encoding.UTF8;
            using (HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(secret)))
            {
                var msg = encoding.GetBytes(message);
                var hash = hmac.ComputeHash(msg);
                return BitConverter.ToString(hash).ToLower().Replace("-", string.Empty);
            }
        }

        static FacebookMessengerModule()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Call Facebook Graph API with given set of parameters
        /// </summary>
        /// <param name="url"></param>
        /// <param name="sendSecretProof"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static dynamic FacebookApiGet( dynamic siteSettings, string url, bool sendSecretProof = false, bool throwOnError = false, params string[] queryStringPair)
        {
            JArray data = new JArray();

            RestClient client = new RestClient("https://graph.facebook.com/v2.11/");
            RestRequest req = new RestRequest(url, Method.GET );

            // add parameters
            if (queryStringPair != null)
            {
                if (queryStringPair.Length % 2 != 0)
                {
                    throw new ArgumentOutOfRangeException("parameterPairs are not pairs");
                }

                for (int i = 0; i < queryStringPair.Length; i+=2)
                {
                    var key = queryStringPair[i];
                    var value = queryStringPair[i + 1];

                    if (key == "access_token")
                    {
                        throw new ArgumentOutOfRangeException("access_token will be added automatically");
                    }

                    if (key == "appsecret_proof")
                    {
                        throw new ArgumentOutOfRangeException("appsecret_proof will be added automatically");
                    }

                    req.Parameters.Add(new Parameter()
                    {
                        Name = key,
                        Value = value,
                        Type = ParameterType.QueryString
                    });
                }
            }
                        
            if (sendSecretProof)
            {
                string hash = MemoryCache.Default["FacebookMessengerModule.appsecret_proof"] as string;
                if (hash == null)
                {
                    hash = FacebookMessengerModule.HashHmac((string)siteSettings.FacebookMessenger.PageAccessToken,
                                                        (string)siteSettings.Application.FacebookAppSecret);

                    MemoryCache.Default["FacebookMessengerModule.appsecret_proof"] = hash;
                }
                req.AddQueryParameter("appsecret_proof", hash );
            }

            req.AddQueryParameter("access_token", (string)siteSettings.FacebookMessenger.PageAccessToken);
            
            LoadMore:

            var response = client.Execute(req);
            if (throwOnError && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Server Returned: " + response.StatusCode);
            }

            dynamic result = JObject.Parse( response.Content );
            if (result.data != null)
            {
                foreach (var item in result.data)
                {
                    data.Add(item);
                }
            }

            if (result.paging != null && result.paging.next != null)
            {
                req.AddQueryParameter("after", (string)result.paging.cursors.after);
                goto LoadMore;
            }

            return JObject.FromObject( new
            {
                data = data
            });
        }


        /// <summary>
        /// Call Facebook Graph API with given set of parameters
        /// </summary>
        /// <param name="url"></param>
        /// <param name="sendSecretProof"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static dynamic FacebookApiPost(dynamic siteSettings, string url, object payload, bool sendSecretProof = false, bool throwOnError = false, params string[] queryStringPair)
        {
            RestClient client = new RestClient("https://graph.facebook.com/v2.11/");
            RestRequest req = new RestRequest(url, Method.POST);

            // add parameters
            if (queryStringPair != null)
            {
                if (queryStringPair.Length % 2 != 0)
                {
                    throw new ArgumentOutOfRangeException("parameterPairs are not pairs");
                }

                for (int i = 0; i < queryStringPair.Length; i += 2)
                {
                    var key = queryStringPair[i];
                    var value = queryStringPair[i + 1];

                    if (key == "access_token")
                    {
                        throw new ArgumentOutOfRangeException("access_token will be added automatically");
                    }

                    if (key == "appsecret_proof")
                    {
                        throw new ArgumentOutOfRangeException("appsecret_proof will be added automatically");
                    }

                    req.Parameters.Add(new Parameter()
                    {
                        Name = key,
                        Value = value,
                        Type = ParameterType.QueryString
                    });
                }
            }

            if (sendSecretProof)
            {
                string hash = MemoryCache.Default["FacebookMessengerModule.appsecret_proof"] as string;
                if (hash == null)
                {
                    hash = FacebookMessengerModule.HashHmac((string)siteSettings.FacebookMessenger.PageAccessToken,
                                                        (string)siteSettings.Application.FacebookAppSecret);

                    MemoryCache.Default["FacebookMessengerModule.appsecret_proof"] = hash;
                }
                req.AddQueryParameter("appsecret_proof", hash);
            }

            req.AddQueryParameter("access_token", (string)siteSettings.FacebookMessenger.PageAccessToken);

            req.RequestFormat = DataFormat.Json;
            req.AddJsonBody(payload);

            var response = client.Execute(req);

            if (throwOnError && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Server Returned: " + response.StatusCode);
            }

            return JObject.Parse(response.Content);
        }
    }
}