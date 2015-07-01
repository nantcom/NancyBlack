using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public interface IStaticType
    {
        int Id { get; set; }

        DateTime __createdAt { get; set; }

        DateTime __updatedAt { get; set; }

        string __version { get; set; }
    }

}
