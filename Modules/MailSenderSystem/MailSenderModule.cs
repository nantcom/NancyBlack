using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public class MailSenderModule
    {
        public static void SendEmail( dynamic site, string to, string subject, string body)
        {
            MailMessage mail = new MailMessage();
            SmtpClient client = new SmtpClient((string)site.SMTPServer);

            mail.From = new MailAddress((string)site.SMTPFromEmail);
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            client.Port = site.SMTPServerPort;
            client.Credentials = new System.Net.NetworkCredential((string)site.SMTPUsername, (string)site.SMTPPassword);
            client.EnableSsl = (bool)site.SMTPUseSSL;
            client.Timeout = 10000;

            client.Send(mail);
        }
    }
}