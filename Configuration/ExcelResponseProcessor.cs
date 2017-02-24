using EPPlusEnumerable;
using Nancy;
using Nancy.Responses.Negotiation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    /// <summary>
    /// Serializes model as Excel File
    /// </summary>
    public class ExcelResponse : Response
    {
        public ExcelResponse( object model, NancyContext ctx )
        {
            this.Contents = GetExcelContent(model);
            this.ContentType = "application/vnd.ms-excel";

            // use download file name
            if (ctx.Items.ContainsKey("DownloadFilename"))
            {
                this.Headers.Add("Content-disposition",
                    string.Format("attachment; filename={0}.xlsx", ctx.Items["DownloadFilename"]));
            }
            else
            {

                this.Headers.Add("Content-disposition",
                    string.Format("attachment; filename={0}-{1:yyyyMMdd}.xlsx", Path.GetFileName(ctx.Request.Url.Path), DateTime.Now));
            }

            this.StatusCode = HttpStatusCode.OK;
        }

        private static Action<Stream> GetExcelContent(object model)
        {
            return (Stream s) =>
            {
                byte[] b = null;
                if (typeof(IEnumerable<object>).IsAssignableFrom( model.GetType() ))
                {
                    b = Spreadsheet.Create(model as IEnumerable<object>);
                }

                if (typeof(IEnumerable<IEnumerable<object>>).IsAssignableFrom(model.GetType()))
                {
                    b = Spreadsheet.Create(model as IEnumerable<IEnumerable<object>>);
                }

                if (b == null)
                {
                    return;
                }

                s.Write(b, 0, b.Length);

            };
        }
    }



    public class ExcelResponseProcessor : IResponseProcessor
    {
        private static readonly IEnumerable<Tuple<string, MediaRange>> extensionMappings =
            new[] { new Tuple<string, MediaRange>("xlsx", new MediaRange("application/vnd.ms-excel")) };

        public ExcelResponseProcessor()
        {
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings
        {
            get { return extensionMappings; }
        }

        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            if (requestedMediaRange.Matches("application/vnd.ms-excel"))
            {
                return new ProcessorMatch
                {
                    ModelResult = MatchResult.DontCare,
                    RequestedContentTypeResult = MatchResult.ExactMatch
                };
            }

            return new ProcessorMatch
            {
                ModelResult = MatchResult.DontCare,
                RequestedContentTypeResult = MatchResult.NoMatch
            };
        }

        public Response Process(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            return new ExcelResponse(model, context);
        }
    }
}