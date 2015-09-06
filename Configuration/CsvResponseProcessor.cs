using Nancy;
using Nancy.Responses.Negotiation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    /// <summary>
    /// Serializes model as Excel File
    /// </summary>
    public class CsvResponse : Response
    {
        public CsvResponse(object model, NancyContext ctx)
        {
            this.Contents = GetCsvContent(model);
            if (this.Contents == null)
            {
                this.StatusCode = HttpStatusCode.NotAcceptable;
                return;
            }

            this.ContentType = "text/csv";

            // use download file name
            if (ctx.Items.ContainsKey("DownloadFilename"))
            {
                this.Headers.Add("Content-disposition",
                    string.Format("attachment; filename={0}.csv", ctx.Items["DownloadFilename"]));
            }
            else
            {

                this.Headers.Add("Content-disposition",
                    string.Format("attachment; filename={0}-{1:yyyyMMdd}.csv", Path.GetFileName(ctx.Request.Url.Path), DateTime.Now));
            }

            this.StatusCode = HttpStatusCode.OK;
        }

        private static Action<Stream> GetCsvContent(object model)
        {
            var input = model as IEnumerable<object>;
            if (input == null)
            {
                return null;
            }

            var first = input.FirstOrDefault();
            if (input == null)
            {
                return null;
            }

            return (Stream s) =>
            {
                var sw = new StreamWriter(s);
                var props = from prop in input.First().GetType().GetProperties()
                            where prop.CanRead
                            let pe = Expression.Parameter(first.GetType())
                            let selectorExpression = Expression.Property(pe, prop.Name)
                            select Expression.Lambda(selectorExpression, pe).Compile();
                
                foreach (var item in input)
                {
                    foreach (var p in props)
                    {
                        sw.Write(p.DynamicInvoke(item).ToString());
                        sw.Write(",");
                    }
                    sw.Write("\r\n");
                }

                sw.Flush();
                sw.Dispose();
            };
        }
    }


    public class CsvResponseProcessor : IResponseProcessor
    {
        private static readonly IEnumerable<Tuple<string, MediaRange>> extensionMappings =
            new[] { new Tuple<string, MediaRange>("csv", new MediaRange("text/csv")) };

        public CsvResponseProcessor()
        {
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings
        {
            get { return extensionMappings; }
        }

        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            if (requestedMediaRange.Matches("text/csv"))
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
            return new CsvResponse(model, context);
        }
    }
}