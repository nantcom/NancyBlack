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
            
        }
    }
}