using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MailSenderSystem
{

    public class NcbMailSenderLog : IStaticType
    {
        public int Id
        {
            get;
            set;
        }

        public DateTime __createdAt
        {
            get;
            set;
        }

        public DateTime __updatedAt
        {
            get;
            set;
        }

        public string To { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        /// <summary>
        /// Whether the email was skipped because user already received the mail today
        /// </summary>
        public bool IsSkipped { get; set; }

        /// <summary>
        /// Settings used to send email
        /// </summary>
        public SmtpSettings Settings { get; set; }

        /// <summary>
        /// Exception occured, if any
        /// </summary>
        public Exception Exception { get; set; }
    }

}