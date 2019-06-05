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
                var toSend = _Outbox.ToList();
                if (toSend.Count == 0)
                {
                    return;
                }

                _Outbox = null;

                var site = ctx.Items["SiteSettings"] as JObject;
                var db = ctx.Items["SiteDatabase"] as NancyBlackDatabase;
                var settings = site.Property("smtp").Value.ToObject<SmtpSettings>();

                SmtpClient client = new SmtpClient(settings.server);
                client.Port = settings.port;
                client.Credentials = new System.Net.NetworkCredential(settings.username, settings.password);
                client.EnableSsl = settings.useSSL;
                client.Timeout = 30000;

                foreach (var mail in toSend)
                {
                    var log = new NcbMailSenderLog();
                    log.Body = mail.Body;
                    log.To = string.Join(",", from m in mail.To select m.Address);
                    log.Subject = mail.Subject;
                    log.Settings = settings;
                    log.__createdAt = DateTime.Now;
                    log.__updatedAt = DateTime.Now;

                    var key = log.To + "-" + log.Subject + log.Body.GetHashCode();
                    if (MemoryCache.Default[key] != null)
                    {
                        continue; // we just send this email to this user recently, skip
                    }

                    MemoryCache.Default.Add(key, 1, DateTimeOffset.Now.AddMinutes(10));

                    try
                    {
                        mail.From = new MailAddress(settings.fromEmail);
                        client.Send(mail);
                    }
                    catch (Exception e)
                    {
                        log.Exception = e;
                    }

                    db.UpsertRecord(log);
                }
            }
        }

        public void Hook(IPipelines p)
        {
            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                Task.Run(() =>
                {
                    MailSenderModule.ProcessQueue(ctx);
                });
            });
        }
    }
}