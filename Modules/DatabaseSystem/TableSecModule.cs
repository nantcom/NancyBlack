using Nancy;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    public class TableSecModule : BaseModule
    {
        public const string PERMISSON_QUERY = "query";
        public const string PERMISSON_CREATE = "create";
        public const string PERMISSON_UPDATE = "update";
        public const string PERMISSON_DELETE = "delete";

        public TableSecModule()
        {
            // Admin Page for Table Security
            Get["/Admin/TableSec"] = this.HandleViewRequest("databasesystem-tablesec", null);
        }

        /// <summary>
        /// Determine whether current user has permission to perform action on the given type
        /// </summary>
        /// <param name="context"></param>
        /// <param name="typeName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool HasPermission(NancyContext context, string typeName, string action )
        {
            NcbUser user = context.CurrentUser as NcbUser;
            if (user.HasClaim("admin"))
            {
                return true;
            }

            var sitesettings = context.GetSiteSettings();
            if (sitesettings.tablesec == null)
            {
                return true; // tablesec was not configured, default to allow
            }

            if (sitesettings.tablesec.enable == false)
            {
                return true; // tablesec was turned off
            }

            var normalizedName = DataTypeFactory.NormalizeTypeName(typeName);
            JObject permission = sitesettings.tablesec[normalizedName];
            if (permission == null)
            {
                return false;
            }

            if ( permission[action] == null )
            {
                return false;
            }

            return (bool)permission[action]["enable"] == true;
        }

        /// <summary>
        /// Throws UnauthorizedAccessException if user dont have permission to perform action on data type
        /// </summary>
        /// <param name="context"></param>
        /// <param name="typeName"></param>
        /// <param name="action"></param>
        public static void ThrowIfNoPermission(NancyContext context, string typeName, string action)
        {
            var hasPermission = TableSecModule.HasPermission(context, typeName, action);

            if (hasPermission == false)
            {
                throw new UnauthorizedAccessException();
            }
        }

        /// <summary>
        /// Set Table Security
        /// </summary>
        /// <param name="type"></param>
        /// <param name="create"></param>
        /// <param name="update"></param>
        /// <param name="delete"></param>
        /// <param name="query"></param>
        public static void SetTableSec( NancyContext context, string name, bool create, bool update, bool delete, bool query)
        {
            var setting = new
            {
                name = name,
                create = new
                {
                    enable = create,
                },
                update = new
                {
                    enable = update,
                },
                delete = new
                {
                    enable = delete,
                },
                query = new
                {
                    enable = query
                }
            };

            var settingJObject = JObject.FromObject(setting);

            var sitesettings = context.GetSiteSettings();

            if (sitesettings.tablesec == null)
            {
                sitesettings.tablesec = new JObject();
            }

            sitesettings.tablesec[ DataTypeFactory.NormalizeTypeName( name )] = settingJObject;

            AdminModule.WriteSiteSettings(context, sitesettings);
        }
    }
}