using NantCom.NancyBlack.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using Nancy.Bootstrapper;
using System.Collections.Concurrent;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace NantCom.NancyBlack.Modules
{
    public class MailSenderModule : IPipelineHook
    {
        private static ConcurrentBag<MailMessage> _Outbox = new ConcurrentBag<MailMessage>();

        public class SmtpSettings
        {
            public string fromEmail { get; set; }

            public string server { get; set; }

            public int port { get; set; }

            public string username { get; set; }

            public string password { get; set; }

            public bool useSSL { get; set; }
        }

        public class MailSenderLog : IStaticType
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
            /// Settings used to send email
            /// </summary>
            public SmtpSettings Settings { get; set; }

            /// <summary>
            /// Exception occured, if any
            /// </summary>
            public Exception Exception { get; set; }
        }

        /// <summary>
        /// Old method signature for compatibility
        /// </summary>
        /// <param name="site"></param>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public static void SendEmail(dynamic site, string to, string subject, string body)
        {
            MailSenderModule.SendEmail(to, subject, body);
        }

        /// <summary>
        /// Sends the email
        /// </summary>
        /// <param name="site"></param>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public static void SendEmail( string to, string subject, string body)
        {
            if (_Outbox == null)
            {
                _Outbox = new ConcurrentBag<MailMessage>();
            }

            MailMessage mail = new MailMessage();

            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            _Outbox.Add(mail);
        }

        public void Hook(IPipelines p)
        {
            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                if (_Outbox == null)
                {
                    return;
                }

                var toSend = _Outbox.ToList();
                _Outbox = null;

                var site = ctx.Items["SiteSettings"] as JObject;
                var db = ctx.Items["SiteDatabase"] as NancyBlackDatabase;
                var settings = site.Property("smtp").Value.ToObject<SmtpSettings>();

                Task.Run(() =>
                {
                    SmtpClient client = new SmtpClient(settings.server);
                    client.Port = settings.port;
                    client.Credentials = new System.Net.NetworkCredential(settings.username, settings.password);
                    client.EnableSsl = settings.useSSL;
                    client.Timeout = 30000;

                    foreach (var mail in toSend)
                    {
                        mail.From = new MailAddress(settings.fromEmail);

                        var log = new MailSenderLog();
                        log.Body = mail.Body;
                        log.To = string.Join(",", from m in mail.To select m.Address);
                        log.Subject = mail.Subject;
                        log.Settings = settings;

                        try
                        {
                            client.Send(mail);
                        }
                        catch (Exception e)
                        {
                            log.Exception = e;
                        }

                        db.UpsertRecord(log);
                    }

                });

            });
        }
    }
}