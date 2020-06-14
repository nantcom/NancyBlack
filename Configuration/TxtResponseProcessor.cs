using Nancy;
using Nancy.Responses.Negotiation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    public class TxtResponseProcessor : IResponseProcessor
    {
        private static readonly IEnumerable<Tuple<string, MediaRange>> extensionMappings =
            new[] { new Tuple<string, MediaRange>("txt", new MediaRange("text/plain")) };

        public TxtResponseProcessor()
        {
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings
        {
            get { return extensionMappings; }
        }

        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            if (requestedMediaRange.Matches("text/plain"))
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
            var res = new Response();
            res.ContentType = "text/plain";
            res.Contents += (s) =>
            {
                using (StreamWriter sw = new StreamWriter(s))
                {
                    sw.Write(model.ToString());
                    sw.Flush();
                }
            };

            return res;
        }
    }
}