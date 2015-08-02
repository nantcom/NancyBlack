using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class CommerceModule : BaseModule
    {
        public CommerceModule()
        {
            Get["/__commerce/cart"] = this.HandleRequest((args) =>
            {
                return View["commerce-shoppingcart", this.GetModel()];

            });

            Get["/__commerce/saleorder/{id}/notifytransfer"] = this.HandleRequest((args) =>
            {
                var saleorder = this.SiteDatabase.Query("saleorder",
                    string.Format("uuid eq '{0}'", (string)args.id),
                    "Id desc").FirstOrDefault();

                return View["commerce-notifytransfer", this.GetModel(saleorder)];

            });

        }
    }
}