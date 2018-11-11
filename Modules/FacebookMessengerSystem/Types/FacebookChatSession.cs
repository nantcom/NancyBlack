using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

using FM = NantCom.NancyBlack.Modules.FacebookMessengerSystem.FacebookWebHook;

namespace NantCom.NancyBlack.Modules.FacebookMessengerSystem.Types
{

    [AttributeUsage(AttributeTargets.Method)]
    public class StateAttribute : Attribute
    {
        public string[] State { get; set; }

        public StateAttribute(params string[] names)
        {
            this.State = names;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RequireChatText : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RequireSessionData : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MatchTextExactAttribute : Attribute
    {
        public string[] Text { get; set; }

        public MatchTextExactAttribute(params string[] matches )
        {
            this.Text = matches;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RequireOptin : Attribute
    {
        /// <summary>
        /// Referrence Type (optin.ref)
        /// </summary>
        public string ReferenceType { get; set; }
        
        public RequireOptin(string referenceType)
        {
            this.ReferenceType = referenceType;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RequireAttachments : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class ProcessEcho : Attribute
    {
    }

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
        /// All Previous messages in this conversation
        /// </summary>
        public List<dynamic> Messages { get; set; }

        /// <summary>
        /// Current State
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// Session - related data
        /// </summary>
        public dynamic SessionData { get; set; }

        /// <summary>
        /// User Profile
        /// </summary>
        public dynamic UserProfile { get; set; }

        /// <summary>
        /// Last User Profile update
        /// </summary>
        public DateTime LastProfileUpdate { get; set; }

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

        private NancyBlackDatabase db;
        private dynamic sitesettings;
        private string currentMessageText;
        private string currentQuickReply;

        private delegate bool HandlerMethod( FacebookChatSession session, object message);

        /// <summary>
        /// Handlers
        /// </summary>
        private static List<MethodInfo> _Handlers;
        private static List<HandlerMethod> _OptInHandlers;

        /// <summary>
        /// Handles the webhook
        /// </summary>
        public void HandleWebhook( NancyBlackDatabase db, dynamic siteSettings,  dynamic messaging )
        {
            this.db = db;
            this.sitesettings = siteSettings;

            if (this.Messages == null)
            {
                this.Messages = new List<dynamic>();
            }
            this.Messages.Add(messaging);

            if (messaging.optin != null)
            {
                string type = (string)messaging.optin.@ref;
                var isActive = FacebookWebHook.IsOptInActive(db,
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

            if (messaging.message != null)
            {
                this.currentMessageText = messaging.message.text;

                if (messaging.message.quick_reply != null)
                {
                    this.currentQuickReply = messaging.message.quick_reply.payload;
                }
            }

            if (_Handlers == null)
            {
                _Handlers = new List<MethodInfo>();

                var methods = from m in this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static)
                              where
                                m.Name.StartsWith("Handle_") &&
                                m.ReturnType == typeof(bool) &&
                                m.GetParameters().Length == 2
                              orderby
                                m.Name
                              select m;

                foreach (var method in methods)
                {
                    _Handlers.Add(method);
                }
            }

            foreach (var handler in _Handlers)
            {
                try
                {
                    if (handler.GetCustomAttribute<ProcessEcho>() != null)
                    {
                        // method want to process echo - and this is not echo
                        if (messaging.message != null && messaging.message.is_echo == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // method dont want echo - and this is echo
                        if (messaging.message != null && messaging.message.is_echo == true)
                        {
                            continue;
                        }
                    }

                    if (handler.GetCustomAttribute<RequireSessionData>() != null)
                    {
                        if (this.SessionData == null)
                        {
                            continue;
                        }
                    }

                    if (handler.GetCustomAttribute<RequireChatText>() != null)
                    {
                        if (string.IsNullOrEmpty(this.currentMessageText))
                        {
                            continue;
                        }
                    }

                    var requireOptin = handler.GetCustomAttribute<RequireOptin>();
                    if (requireOptin != null)
                    {
                        if (messaging.optin == null)
                        {
                            continue;
                        }

                        string input = messaging.optin.@ref;
                        if (input.StartsWith(requireOptin.ReferenceType) == false)
                        {
                            continue;
                        }
                    }

                    if (handler.GetCustomAttribute<RequireAttachments>() != null)
                    {
                        if (messaging.message.attachments == null)
                        {
                            continue;
                        }
                    }

                    var matchA = handler.GetCustomAttribute<MatchTextExactAttribute>();
                    if (matchA != null)
                    {
                        if (string.IsNullOrEmpty(this.currentMessageText))
                        {
                            continue;
                        }

                        bool matched = matchA.Text.Any(s => this.currentMessageText.ToLowerInvariant() == s.ToLowerInvariant());
                        if (matched == false)
                        {
                            continue;
                        }
                    }

                    var stateA = handler.GetCustomAttribute<StateAttribute>();
                    if (stateA != null)
                    {
                        if (string.IsNullOrEmpty(this.CurrentState))
                        {
                            continue;
                        }

                        bool stateMatched = stateA.State.Any(s => this.CurrentState == s || this.CurrentState.StartsWith(s));
                        if (stateMatched == false)
                        {
                            continue;
                        }
                    }

                    HandlerMethod d = (HandlerMethod)Delegate.CreateDelegate(typeof(HandlerMethod), handler);
                    var handled = d(this, messaging);
                    if (handled)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    MailSenderModule.SendEmail("company@nant.co",
                        "FacebookWebHook Handler Error",

                        "<b>Facebook Input:</b><br/>" +
                        messaging.ToString() + "<br/><br/>" +

                        "<b>Message:</b>" + ex.Message + "<br/>" +
                        ex.StackTrace);
                }
            }

            db.UpsertRecord(this);
        }

        /// <summary>
        /// Send Message using send API
        /// </summary>
        /// <param name="th"></param>
        /// <param name="en"></param>
        /// <param name="quickreplies"></param>
        /// <returns></returns>
        private dynamic SendText(string th, string en, params QuickReply[] quickreplies)
        {
            return this.SendText(th, en, "RESPONSE", quickreplies);
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

        /// <summary>
        /// JSTime to DateTime - result in UTC
        /// </summary>
        /// <param name="jstime"></param>
        /// <returns></returns>
        private DateTime JSTimeToDateTime( long jstime )
        {
            var result = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                     .AddMilliseconds(jstime);

            return result;
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