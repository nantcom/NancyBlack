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
using NantCom.NancyBlack.Modules.MailSenderSystem;
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules
{

    public class MailSenderModule : IPipelineHook
    {
        private static ConcurrentQueue<MailMessage> _Outbox;

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
        public static void SendEmail( string to, string subject, string body, bool queue = true, Nancy.NancyContext ctx = null)
        {
            if (_Outbox == null)
            {
                _Outbox = new ConcurrentQueue<MailMessage>();
            }

            if (string.IsNullOrEmpty(to))
            {
                System.Diagnostics.Debugger.Break();
                return;
            }
            
            MailMessage mail = new MailMessage();
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            _Outbox.Enqueue(mail);

            if (queue == false)
            {
                if (ctx == null)
                {
                    throw new InvalidOperationException("Context is required");
                }

                MailSenderModule.ProcessQueue(ctx);
            }
        }

        /// <summary>
        /// Immediately Send out email
        /// </summary>
        public static void ProcessQueue(Nancy.NancyContext ctx)
        {
            if (_Outbox == null)
            {
                return;
            }

            if (ctx.Items.ContainsKey("SiteSettings") == false)
            {
                return;
            }

            lock (BaseModule.GetLockObject("MailSenderModule.ProcessQueue"))
            {
                if (_Outbox == null)
                {
                    return;
                }

                var toSend = _Outbox.ToList();
                _Outbox = null;

                var site = ctx.Items["SiteSettings"] as JObject;

                Task.Run(() =>
                {
                    Func<SmtpSettings, SmtpClient> getClient = (s) =>
                    {
                        SmtpClient client = new SmtpClient(s.server);
                        client.Port = s.port;
                        client.Credentials = new System.Net.NetworkCredential(s.username, s.password);
                        client.EnableSsl = s.useSSL;
                        client.Timeout = 30000;

                        return client;
                    };

                    var db = NancyBlackDatabase.GetSiteDatabase(BootStrapper.RootPath);
                    var settings = site.Property("smtp").Value.ToObject<SmtpSettings>();
                    var count = 0;
                    SmtpClient sender = getClient(settings);

                    foreach (var mail in toSend)
                    {
                        if (count % 100 == 0)
                        {
                            sender.Dispose();
                            count = 0;

                            sender = getClient(settings);
                        }

                        var log = new NcbMailSenderLog();
                        log.Body = mail.Body;
                        log.To = string.Join(",", from m in mail.To select m.Address);
                        log.Subject = mail.Subject;
                        log.MessageHash = (log.Body + log.To + log.Subject).GetHashCode();
                        log.Settings = settings;
                        log.__createdAt = DateTime.Now;
                        log.__updatedAt = DateTime.Now;

                        var today = DateTime.Now.Date;
                        var lastLog = db.Query<NcbMailSenderLog>().Where(l => l.MessageHash == log.MessageHash).FirstOrDefault();
                        if ( lastLog != null)
                        {
                            if (DateTime.Now.Subtract( lastLog.__createdAt ).TotalHours < 12)
                            {
                                log.IsSkipped = true;
                            }
                        }

                        if (log.IsSkipped == false)
                        {
                            try
                            {
                                log.IsAttempted = true;

                                mail.From = new MailAddress(settings.fromEmail);
                                sender.Send(mail);

                                log.IsSent = true;
                            }
                            catch (Exception e)
                            {
                                log.Exception = e;
                            }
                        }

                        db.UpsertRecord(log);
                        count++;
                    }

                    db.Dispose();               
                });

            }
        }

        public void Hook(IPipelines p)
        {
            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                MailSenderModule.ProcessQueue(ctx);
            });
        }
    }
}