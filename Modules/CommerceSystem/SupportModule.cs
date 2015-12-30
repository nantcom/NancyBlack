using NantCom.NancyBlack.Modules.CommerceSystem.types;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem
{
    public class SupportModule : BaseModule
    {
        public SupportModule()
        {
            Get["/support/{saleOrderIdentifier}"] = this.HandleViewRequest("commerce-support", (arg) =>
            {
                var id = (string)arg.saleOrderIdentifier;
                var so = this.SiteDatabase.Query<SaleOrder>()
                            .Where(row => row.SaleOrderIdentifier == id)
                            .FirstOrDefault();

                var statusList = (typeof(SaleOrderStatus)
                            .GetFields(BindingFlags.Public | BindingFlags.Static)
                            .Where(f => f.FieldType == typeof(string)).Select(f => (string)f.GetValue(null))).ToList();

                var dummyPage = new Page();

                var data = new { StatusList = statusList, SaleOrder = so };

                return new StandardModel(this, dummyPage, data);
            });
        }
    }
}