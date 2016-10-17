using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public class DynamicQueryModule : BaseModule
    {
        public DynamicQueryModule()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new string[] { "admin" });

            Get["/Admin/dynamicquery"] = this.HandleViewRequest("databasesystem-dynamicquery", (arg)=>
            {
                var dataType = this.Request.Query.t;
                if (dataType != null)
                {
                    return new StandardModel(200)
                    {
                        Data = this.SiteDatabase.DataType.FromName( dataType )
                    };
                }

                return new StandardModel(200);
            });
            
            Get["/Admin/dynamicquery/AllDataType"] = this.HandleListDataTypeRequest(() => this.SiteDatabase);
        }


        protected Func<dynamic, dynamic> HandleListDataTypeRequest(Func<NancyBlackDatabase> dbGetter)
        {
            return this.HandleRequest((arg) =>
            {
                return from type in dbGetter().DataType.RegisteredTypes
                       select type;
            });
        }

    }
}