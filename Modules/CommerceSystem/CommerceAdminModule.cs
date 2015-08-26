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
            Get["/admin/product"] = this.HandleViewRequest("/Admin/productmanager", null);
            Get["/admin/inventory"] = this.HandleViewRequest("/Admin/Inventorymanager", null);
            Get["/admin/saleorder"] = this.HandleViewRequest("/Admin/saleordermanager", null);
            Get["/admin/saleorder/{id}"] = this.HandleViewRequest("/Admin/saleorderdetailmanager", null);
            Get["/admin/commerce/forms"] = this.HandleViewRequest("/Admin/commerceadmin-templates", null);
            Get["/admin/commerce/forms"] = this.HandleViewRequest("/Admin/commerceadmin-templates", null);
        }
    }
}