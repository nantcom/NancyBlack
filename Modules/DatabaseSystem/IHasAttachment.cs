using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public interface IHasAttachment
    {
        object[] Attachments { get; set; }
    }
}