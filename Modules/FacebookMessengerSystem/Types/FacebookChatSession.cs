using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

using FM = NantCom.NancyBlack.Modules.FacebookMessengerSystem.FacebookMessengerModule;

namespace NantCom.NancyBlack.Modules.FacebookMessengerSystem.Types
{
    /// <summary>
    /// Partial Implementation of Facebook Chat Session
    /// </summary>
    public partial class FacebookChatSession : IStaticType
    {
        #region standard Properties

        public int Id { get; set; }
        public DateTime __createdAt { get; set; }
        public DateTime __updatedAt { get; set; }

        #endregion

        /// <summary>
        /// Our internal user id
        /// </summary>
        public int NcbUserId { get; set; }

        /// <summary>
        /// PS ID that we are being chat with
        /// </summary>
        public string PageScopedId { get; set; }

        /// <summary>
        /// Last Message recieved from User
        /// </summary>
        public DateTime LastMessageReceived { get; set; }

        /// <summary>
        /// All Previous messages that user have sent
        /// </summary>
        public List<dynamic> Messages { get; set; }

        /// <summary>
        /// Session - related data
        /// </summary>
        public dynamic SessionData { get; set; }

        /// <summary>
        /// Whether we still can send message to this user (24+1 hour rule)
        /// </summary>
        /// <returns></returns>
        public bool IsStillCanSendMessage()
        {
            if (DateTime.Now.Subtract(this.LastMessageReceived).TotalHours <= 24)
            {
                return true;
            }

            // did not count to +1 rule for now

            return false;
        }

        private NancyBlackDatabase _db;
        private dynamic sitesettings;
        private string currentMessageText;
        private delegate bool HandlerMethod( FacebookChatSession session, object message);

        /// <summary>
        /// Handlers
        /// </summary>
        private static List<HandlerMethod> _Handlers;
        private static List<HandlerMethod> _OptInHandlers;

        /// <summary>
        /// Handles the webhook
        /// </summary>
        public void HandleWebhook( NancyBlackDatabase db, dynamic siteSettings,  dynamic message )
        {
            this._db = db;
            this.sitesettings = siteSettings;

            if (this.Messages == null)
            {
                this.Messages = new List<dynamic>();
            }
            this.Messages.Add(message);

            if (message.optin != null)
            {
                string type = (string)message.optin.@ref;
                var isActive = FacebookMessengerModule.IsOptInActive(db,
                                this.NcbUserId, type);

                if (!isActive)
                {
                    db.UpsertRecord(new FacebookMessengerOptIn()
                    {
                        NcbUserId = this.NcbUserId,
                        OptInType = type
                    });
                }
            }

            if (message.message != null)
            {
                this.currentMessageText = message.message.text;
            }
            
            if (_Handlers == null)
            {
                _Handlers = new List<HandlerMethod>();

                var methods = from m in this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static)
                              where
                                m.Name.StartsWith("Handle_") &&
                                m.ReturnType == typeof(bool) &&
                                m.GetParameters().Length == 2
                              select m;

                foreach (var method in methods)
                {
                    HandlerMethod d = (HandlerMethod)Delegate.CreateDelegate(typeof(HandlerMethod), method);
                    _Handlers.Add(d);
                }
            }

            foreach (var handler in _Handlers)
            {
                var handled = handler(this, message);
                if (handled)
                {
                    break;
                }
            }

            db.UpsertRecord(this);
        }

        /// <summary>
        /// Send Message using Send API
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagingType"></param>
        /// <returns></returns>
        private dynamic SendText( string th, string en, string messagingType = "RESPONSE", params QuickReply[] quickreplies)
        {
            var text = "";
            if (this.SessionData.lang == "en")
            {
                text = en + " \n\n#wan";
            }
            else if (this.SessionData.lang == "th")
            {
                text = th + "\n\n#น้องวัน";
            }
            else
            {
                // send both
                text = th + "\n\n" + en +
                       "\n\n#น้องวัน #wan";
            }

            object message;
            if (quickreplies.Length > 0)
            {
                message = new
                {
                    text = text,
                    quick_replies = quickreplies
                };
            }
            else
            {
                message = new
                {
                    text = text,
                };
            }

            return FM.FacebookApiPost(this.sitesettings,
                "/me/messages",
                new
                {
                    recipient = new { id = this.PageScopedId },
                    messaging_type = messagingType,
                    message = message

                }, false);
        }

        /// <summary>
        /// Send Message using Send API
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagingType"></param>
        /// <returns></returns>
        private dynamic SendImage(string image, string messagingType = "RESPONSE", params QuickReply[] quickreplies)
        {
            return FM.FacebookApiPost(this.sitesettings,
                   "/me/messages",
                   new
                   {
                       recipient = new { id = this.PageScopedId },
                       messaging_type = messagingType,
                       message = new
                       {
                           attachment = new
                           {
                               type = "image",
                               payload = new
                               {
                                   url = image,
                                   is_reusable = true
                               }
                           }
                       },
                       quick_replies = quickreplies
                   });
        }

        private dynamic SendSticker(string sticker_id, string messagingType = "RESPONSE", params QuickReply[] quickreplies)
        {
            if (sticker_id == "sorry")
            {
                return this.SendImage("https://scontent.xx.fbcdn.net/v/t39.1997-6/p100x100/13655700_1411567302190359_1061634825_n.png?_nc_cat=1&_nc_ad=z-m&_nc_cid=0&oh=1119ebebc7d879709a792ea25c3704d9&oe=5C2C5010");
            }

            return null;
        }

        private class QuickReply
        {
            /// <summary>
            /// Content Type: text, location, user_email, user_phone_number
            /// </summary>
            public string content_type { get; set; }

            /// <summary>
            /// Title of the button
            /// </summary>
            public string title { get; set; }

            /// <summary>
            /// URL of the image
            /// </summary>
            public string image_url { get; set; }

            /// <summary>
            /// Payload to be send back
            /// </summary>
            public string payload { get; set; }

        }
    }
}