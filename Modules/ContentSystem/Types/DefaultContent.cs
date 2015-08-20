using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{

    /// <summary>
    /// Default Content Classs, contains properties that the engine requires
    /// </summary>
    public class DefaultContent : IContent
    {
        public int Id { get; set; }

        public string Url { get; set; }

        public string Layout { get; set; }

        public string RequiredClaims { get; set; }

        public int DisplayOrder { get; set; }

        public string Title { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }
    }

}