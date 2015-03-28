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
            SmtpClient SmtpServer = new SmtpClient((string)site.SMTPServer);

            mail.From = new MailAddress((string)site.SMTPFromEmail);
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            SmtpServer.Port = site.SMTPServerPort;
            SmtpServer.Credentials = new System.Net.NetworkCredential((string)site.SMTPUsername, (string)site.SMTPPassword);
            SmtpServer.EnableSsl = (bool)site.SMTPUseSSL;

            SmtpServer.Send(mail);
        }
    }
}