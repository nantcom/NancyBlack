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
            if (context.Request.Headers.Accept.First().Item1.Contains( "/json" ) ||
                context.Request.Headers.Accept.First().Item1.Contains("/xml"))
            {
                return;
            }

            try
            {
                var response = _renderer.RenderView(context, "Codes/" + (int)statusCode);
                response.StatusCode = statusCode;
                response.ContentType = "text/html; charset=utf-8";
                context.Response = response;
            }
            catch (Exception)
            {
            }

        }

        public bool HandlesStatusCode(Nancy.HttpStatusCode statusCode, Nancy.NancyContext context)
        {
#if DEBUG
            if (statusCode == HttpStatusCode.InternalServerError && Debugger.IsAttached)
            {
                Debugger.Break();
            }
#endif
            return
                (statusCode == HttpStatusCode.Forbidden) ||
                (statusCode == HttpStatusCode.NotFound) ||
                (statusCode == HttpStatusCode.InternalServerError) ||
                (statusCode == HttpStatusCode.Locked);
        }
    }
}