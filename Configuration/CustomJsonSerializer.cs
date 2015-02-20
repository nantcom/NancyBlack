using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    public class CustomJsonSerializer : JsonSerializer
    {
        public CustomJsonSerializer()
        {
            this.NullValueHandling = NullValueHandling.Include;
            this.DefaultValueHandling = DefaultValueHandling.Include;
            this.Formatting = Formatting.None;
        }
    }
}