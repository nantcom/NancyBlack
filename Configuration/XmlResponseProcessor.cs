using EPPlusEnumerable;
using Nancy;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml;

namespace NantCom.NancyBlack.Configuration
{
    public class ResolveEverything : DataContractResolver
    {
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            throw new NotImplementedException();
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            XmlDictionary dictionary = new XmlDictionary();
            typeName = dictionary.Add(type.Name);
            typeNamespace = dictionary.Add("http://www.nancyblack.org/" + type.Namespace);

            return true;
        }

    }

    public class RemoveNamespace : XmlTextWriter
    {
        private string _AnyTypeName;

        public RemoveNamespace(Stream stream, string anyTypeName = "")
          : base(stream, null)
        {
            _AnyTypeName = anyTypeName;
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            if (string.IsNullOrEmpty(_AnyTypeName) == false && localName == "anyType")
            {
                localName = _AnyTypeName;
            }
            base.WriteStartElement(null, localName, "");
        }

    }

    /// <summary>
    /// Serializes model as Excel File
    /// </summary>
    public class XmlResponse : Response
    {
        public XmlResponse(object model, NancyContext ctx)
        {
            this.Contents = GetXmlContent(model);
            this.ContentType = "text/xml";

            this.StatusCode = HttpStatusCode.OK;
        }

        private static Action<Stream> GetXmlContent(object model)
        {
            return (Stream s) =>
            {
                if (model is JToken && ((JToken)model).Type != JTokenType.Object)
                {
                    // must wrap in XML Value
                    using (var writer = new XmlTextWriter(s, System.Text.Encoding.UTF8))
                    {
                        writer.WriteStartElement("value");
                        writer.WriteAttributeString("value", model.ToString());
                        writer.WriteEndElement();
                        writer.Flush();
                    }

                    return;
                }
                else if (typeof(IEnumerable<object>).IsAssignableFrom(model.GetType()))
                {
                    List<object> source = (model as IEnumerable<object>).ToList();
                    if (source.Count == 0)
                    {
                        using (var writer = new XmlTextWriter(s, System.Text.Encoding.UTF8))
                        {
                            writer.WriteStartDocument();
                            writer.WriteStartElement("List");
                            writer.WriteEndElement();
                            writer.Flush();
                        }

                        return;
                    }
                    XmlDictionary dict = new XmlDictionary();
                    var rootName = dict.Add("List");

                    var settings = new DataContractSerializerSettings();
                    settings.DataContractResolver = new ResolveEverything();
                    settings.RootName = rootName;

                    var listSerialize = new DataContractSerializer(source.GetType(), settings);
                    using (var writer = new RemoveNamespace(s, source[0].GetType().Name))
                    {
                        listSerialize.WriteObject(writer, source);
                        writer.Flush();
                    }

                    return;
                }
                else
                {
                    var ser = new DataContractSerializer(model.GetType());
                    using (var writer = new RemoveNamespace(s))
                    {
                        ser.WriteObject(writer, model);
                        writer.Flush();
                    }
                }

            };
        }
    }



    public class XmlResponseProcessor : IResponseProcessor
    {
        private static readonly IEnumerable<Tuple<string, MediaRange>> extensionMappings =
            new[] { new Tuple<string, MediaRange>("xml", new MediaRange("text/xml")),
                    new Tuple<string, MediaRange>("xml", new MediaRange("application/xml"))};

        public XmlResponseProcessor()
        {
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings
        {
            get { return extensionMappings; }
        }

        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            if (requestedMediaRange.Matches("text/xml") ||
                requestedMediaRange.Matches("application/xml"))
            {
                return new ProcessorMatch
                {
                    ModelResult = MatchResult.ExactMatch,
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
            return new XmlResponse(model, context);
        }
    }
}