using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    public class CustomStatusCodeHandler : IStatusCodeHandler
    {
        private IViewRenderer _renderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomStatusCodeHandler"/> class.
        /// </summary>
        /// <param name="renderer">The renderer (injected by Nancy)</param>
        public CustomStatusCodeHandler(IViewRenderer renderer)
        {
            _renderer = renderer;
        }

        public void Handle(Nancy.HttpStatusCode statusCode, Nancy.NancyContext context)
        {
            if (context.Request.Headers.Accept.Count() == 0)
            {
                return;
            }

            if (context.Request.Headers.Accept.First().Item1.Contains( "/json" ) ||
                context.Request.Headers.Accept.First().Item1.Contains("/xml"))
            {
                return;
            }

            if (context.Request.Method == "GET" ||
                context.Request.Method == "POST" ||
                context.Request.Headers.UserAgent.Contains("Mozilla") ||
                context.Request.Headers.UserAgent.Contains("WebKit") ||
                context.Request.Headers.UserAgent.Contains("Trident") )
            {
                var response = _renderer.RenderView(context, "Codes/" + (int)statusCode);
                response.StatusCode = statusCode;
                response.ContentType = "text/html; charset=utf-8";
                context.Response = response;
            }
            
        }

        public bool HandlesStatusCode(Nancy.HttpStatusCode statusCode, Nancy.NancyContext context)
        {
            return
                (statusCode == HttpStatusCode.Forbidden) ||
                (statusCode == HttpStatusCode.NotFound) ||
                (statusCode == HttpStatusCode.InternalServerError) ||
                (statusCode == HttpStatusCode.Locked);
        }
    }
}