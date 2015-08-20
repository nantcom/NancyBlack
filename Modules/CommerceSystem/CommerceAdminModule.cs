using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class CommerceAdminModule : BaseModule
    {
        public CommerceAdminModule()
        {
            Get["/admin/product"] = this.HandleViewRequest("/Admin/product", null);
            Get["/admin/inventory"] = this.HandleViewRequest("/Admin/Inventory", null);
        }
    }
}