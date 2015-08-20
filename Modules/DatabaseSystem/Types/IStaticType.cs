using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NantCom.NancyBlack.Modules.DatabaseSystem.Types
{
    public interface IStaticType
    {
        int Id { get; set; }

        DateTime __createdAt { get; set; }

        DateTime __updatedAt { get; set; }

    }

}
