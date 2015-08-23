using Nancy;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public class PaymentLog : IStaticType
    {
        public int Id { get; set; }        

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Id of the sale order
        /// </summary>
        public int SaleOrderId { get; set; }

        /// <summary>
        /// IP Address of server that created this response
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Request Method
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Url that server has requested
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Querystring from server
        /// </summary>
        public dynamic QueryString { get; set; }

        /// <summary>
        /// Form Post from server
        /// </summary>
        public dynamic FormResponse { get; set; }

        /// <summary>
        /// Any Exception from this Payment Request
        /// </summary>
        public dynamic Exception { get; set; }

        /// <summary>
        /// Creates new instance of payment log from context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static PaymentLog FromContext( NancyContext context)
        {
            PaymentLog log = new PaymentLog();
            log.IPAddress = context.Request.UserHostAddress;
            log.Method = context.Request.Method;
            log.Url = context.Request.Url;
            log.FormResponse = context.Request.Form;
            log.QueryString = context.Request.Query;

            return log;
        }
    }
}