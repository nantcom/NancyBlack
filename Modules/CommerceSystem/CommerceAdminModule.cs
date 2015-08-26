using System;
using System.Collections.Generic;
using System.IO;
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
            Get["/admin/commerce/settings"] = this.HandleViewRequest("/Admin/commerceadmin-settings", null);
            Get["/admin/saleorder"] = this.HandleViewRequest("/Admin/saleordermanager", null);

            Post["/admin/commerce/api/uploadlogo"] = this.HandleRequest((arg) =>
            {
                var file = this.Request.Files.First();
                var filePath = "Site/billinglogo" + Path.GetExtension(file.Name);
                using (var localFile = File.OpenWrite(Path.Combine(this.RootPath, filePath)))
                {
                    file.Value.CopyTo(localFile);
                }

                return "/" + filePath;
            });
            Get["/admin/saleorder/{id}"] = this.HandleViewRequest("/Admin/saleorderdetailmanager", null);
            Get["/admin/commerce/forms"] = this.HandleViewRequest("/Admin/commerceadmin-templates", null);
            Get["/admin/commerce/forms"] = this.HandleViewRequest("/Admin/commerceadmin-templates", null);
        }
    }
}