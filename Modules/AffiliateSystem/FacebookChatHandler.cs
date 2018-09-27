using NantCom.NancyBlack.Modules.AffiliateSystem.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.FacebookMessengerSystem.Types
{
    public partial class FacebookChatSession
    {
        /// <summary>
        /// Handlers which will handle new user
        /// </summary>
        /// <param name="message"></param>
        public static bool Handle_003_AffiliateSubscribe(FacebookChatSession session, dynamic message)
        {
            if (session.SessionData == null)
            {
                return false;
            }

            if (session.NcbUserId == 0)
            {
                return false;
            }

            if (message.optin != null)
            {
                string input = message.optin.@ref;
                if (input.StartsWith("squad51=") == false)
                {
                    return false;
                }

                var type = input.Substring(0, input.IndexOf('='));
                var source = input.Substring(input.IndexOf('=') + 1);

                var existing = session.db.Query<AffiliateRegistration>()
                    .Where(r => r.NcbUserId == session.NcbUserId)
                    .FirstOrDefault();

                if (existing == null)
                {
                    session.SendText("ขอบคุณที่ลงทะเบียนมาคุยกะน้องวันนะ แต่ว่าน้องไม่เจอการลงทะเบียนของพี่เลย :'( เดี๋ยวพี่ลองลงทะเบียนใหม่อีกรอบบนเว็บนะ ลิงค์นี้ -> https://www.level51pc.com?subscribe=1",
                        "Thank you, I will keep you in the loop~ Anyway, I can't find you on our website. :( Can you try register on https://www.level51pc.com?subscribe=1 again?");
                }
                else
                {
                    session.SendText("ขอบคุณที่ลงทะเบียนมาคุยกะน้องวันนะ ถ้ามีอะไรที่น่าสนใจ เดี๋ยวไว้แจ้งไปแน่นอน~",
                        "Thank you, I will keep you in the loop~");

                    if (source != "undefined")
                    {
                        existing.RefererAffiliateCode = source;
                        session.db.UpsertRecord(existing);
                    }
                }

            }
            return false;
        }

    }
}