using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MailSenderSystem
{

    public class SmtpSettings
    {
        public string fromEmail { get; set; }

        public string server { get; set; }

        public int port { get; set; }

        public string username { get; set; }

        public string password { get; set; }

        public bool useSSL { get; set; }
    }

}