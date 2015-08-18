using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using Linq2Rest.Parser;
using System.Linq.Expressions;
using System.Reflection;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules
{

    public abstract class BaseDataModule : BaseModule
    {
        /// <summary>
        /// Handles getting item by it's id
        /// </summary>
        /// <param name="db"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected dynamic HandleSingleItemRequest(NancyBlackDatabase db, dynamic args)
        {
            var tableName = (string)args.table_name;
            var id = (int)args.item_id;

            return this.SiteDatabase.GetByIdAsJObject(tableName, id);
        }

        /// <summary>
        /// Handles Insert/Update Request
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected dynamic HandleInsertUpdateRequest(NancyBlackDatabase db, dynamic arg)
        {
            var entityName = (string)arg.table_name;
            int id = arg.item_id == null ? 0 : (int)arg.item_id;

            if (arg.body == null)
            {
                return 400;
            }

            if (this.SiteDatabase.DataType.FromName(entityName) == null)
            {
                if (this.Request.Url.HostName != "localhost")
                {
                    return 403;
                }
            }

            var fromClient = arg.body.Value as JObject;
            dynamic record = db.UpsertRecord(entityName, fromClient);

            return this.Negotiate
                .WithContentType("application/json")
                .WithModel((object)record);
        }

        /// <summary>
        /// Handles Query Request with support for $count and $inlinecount
        /// </summary>
        /// <param name="db"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected dynamic HandleQueryRequest(NancyBlackDatabase db, dynamic arg)
        {
            if (this.Request.Query["$inlinecount"] == "allpages")
            {
                return this.HandleInlineCountRequest(db, arg);
            }

            if (this.Request.Query["$inlinecount"] == "none")
            {
                return this.HandleQueryRequest_WithoutCountSupport(db, arg);
            }

            if (this.Request.Query["$count"] != null)
            {
                return this.HandleCountRequest(db, arg);
            }

            return this.HandleQueryRequest_WithoutCountSupport(db, arg);
        }

        /// <summary>
        /// Handles Query Request (Get /tables/{tableName}?...)
        /// </summary>
        /// <param name="db"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected dynamic HandleQueryRequest_WithoutCountSupport(NancyBlackDatabase db, dynamic arg)
        {
            var entityName = (string)arg.table_name;

            if (this.Request.Query["$select"] == null)
            {
                var rows = db.Query(entityName,
                                    this.Request.Query["$filter"],
                                    this.Request.Query["$orderby"],
                                    this.Request.Query["$skip"],
                                    this.Request.Query["$top"]);

                return rows;
            }
            else
            {
                var toInclude = ((string)this.Request.Query["$select"]).Split(',');
                if (toInclude.Length == 1 && toInclude[0] == "$select")
                {
                    return 400;
                }

                if (toInclude.Length == 0)
                {
                    return 400;
                }

                var rows = db.QueryAsJObject(entityName,
                                    (string)this.Request.Query["$filter"],
                                    (string)this.Request.Query["$orderby"],
                                    (string)this.Request.Query["$skip"],
                                    (string)this.Request.Query["$top"]).ToList();

                foreach (JObject item in rows)
                {
                    foreach (var property in item.Properties().ToList())
                    {
                        if (toInclude.Contains(property.Name) == false)
                        {
                            property.Remove();
                        }
                    }
                }

                return rows;
            }
        }

        /// <summary>
        /// Handles Count Request
        /// </summary>
        /// <param name="db"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected dynamic HandleCountRequest(NancyBlackDatabase db, dynamic arg)
        {
            var entityName = (string)arg.table_name;
            var rows = db.Count(entityName,
                                this.Request.Query["$filter"],
                                this.Request.Query["$orderby"],
                                this.Request.Query["$skip"],
                                this.Request.Query["$top"]);

            return JToken.Parse(rows.ToString());
        }

        protected dynamic HandleCountWithoutSkipAndTakeRequest(NancyBlackDatabase db, dynamic arg)
        {
            // Count without Skip and Take
            var entityName = (string)arg.table_name;
            var rows = db.Count(entityName,
                                this.Request.Query["$filter"],
                                this.Request.Query["$orderby"],
                                null,
                                null);

            return JToken.Parse(rows.ToString());
        }

        /// <summary>
        /// Handles Count Request
        /// </summary>
        /// <param name="db"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected dynamic HandleInlineCountRequest(NancyBlackDatabase db, dynamic arg)
        {
            return new
            {
                Count = this.HandleCountWithoutSkipAndTakeRequest(db, arg),
                Results = this.HandleQueryRequest_WithoutCountSupport( db, arg )
            };
        }

        /// <summary>
        /// Handles the delete request
        /// </summary>
        /// <param name="db"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected dynamic HandleDeleteRecordRequest(NancyBlackDatabase db, dynamic arg)
        {
            var entityName = (string)arg.table_name;
            var id = arg.item_id == null ? 0 : (int?)arg.item_id;

            db.DeleteRecord(entityName, new { Id = id });

            return 204;
        }

        protected void _CheckInterfaceIHasAttachment(string tableName)
        {
            var type = this.SiteDatabase.DataType.FromName(tableName);
            var classType = type.GetCompiledType();

            // Check weather class implement specifed interface or not            
            bool isImplementIStaticType = typeof(IStaticType).IsAssignableFrom(classType);
            if (isImplementIStaticType == true)
            {
                if (typeof(IHasAttachment).IsAssignableFrom(classType) == false)
                {
                    throw new Exception("Class that implemented IStaticType and need the attachment. It also need to be be implemented IHasAttachment");
                }
            }
        }

        #region File Attachment

        protected string GetAttachmentFolder(string tableName, string id)
        {
            var path = Path.Combine(this.RootPath, "Site", "attachments", tableName, id);
            Directory.CreateDirectory(path);

            return path;
        }

        /// <summary>
        /// Handles file upload request, currently not consolidated with Attach File function
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected dynamic HandleFileUploadRequest(dynamic args)
        {
            var tableName = (string)args.table_name;
            var id = (string)args.item_id;
            var path = this.GetAttachmentFolder(tableName, id);

            dynamic contentItem = this.SiteDatabase.GetByIdAsJObject(tableName, int.Parse(id));

            this._CheckInterfaceIHasAttachment(tableName);

            List<dynamic> newFiles = new List<dynamic>();

            foreach (var item in this.Request.Files)
            {
                var fileName = Path.GetFileName(item.Name);
                var filePath = Path.Combine(path, fileName);
                if (File.Exists(filePath))
                {
                    fileName = Path.GetFileNameWithoutExtension(item.Name) +
                        Guid.NewGuid() +
                        Path.GetExtension(item.Name);

                    filePath = Path.Combine(path, fileName);
                }

                using (var fs = File.Create(filePath))
                {
                    item.Value.CopyTo(fs);
                    newFiles.Add(new
                    {
                        DisplayOrder = 0,
                        Caption = string.Empty,
                        Url =
                            Path.Combine(
                                "/Site",
                                "attachments",
                                tableName,
                                id,
                                fileName).Replace('\\', '/')
                    });
                }
            }

            if (contentItem.Attachments == null)
            {
                contentItem.Attachments = JArray.FromObject(newFiles);
            }
            else
            {
                foreach (var item in newFiles)
                {
                    contentItem.Attachments.Add(JObject.FromObject(item));
                }
            }

            this.SiteDatabase.UpsertRecord(tableName, contentItem);

            return contentItem;
        }

        protected dynamic HandleFileDeleteRequest(dynamic args)
        {
            var tableName = (string)args.table_name;
            var id = (string)args.item_id;
            var fileName = (string)args.file_name;

            var directory = this.GetAttachmentFolder(tableName, id);
            var path = Path.Combine(directory, fileName);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            dynamic contentItem = this.SiteDatabase.GetByIdAsJObject(tableName, int.Parse(id));

            if (contentItem.Attachments != null)
            {
                var array = contentItem.Attachments as JArray;
                for (int i = 0; i < array.Count; i++)
                {
                    var url = (string)array[i]["Url"];
                    if (url.EndsWith("/" + fileName))
                    {
                        array.RemoveAt(i);

                        this.SiteDatabase.UpsertRecord(tableName, contentItem);
                        break;
                    }
                }
            }


            return contentItem;
        }

        #endregion

        /// <summary>
        /// Handles the request againts site's database
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        protected dynamic HandleRequestForSiteDatabase(Func<NancyBlackDatabase, dynamic, dynamic> action)
        {
            return this.HandleRequest((arg) =>
            {
                return action(this.SiteDatabase, arg);
            });
        }

    }

    /// <summary>
    /// Class which performs
    /// </summary>
    public sealed class DataModule : BaseDataModule
    {
        public DataModule()
        {

            // the interface of data mobile is compatible with Azure Mobile Service
            // http://msdn.microsoft.com/en-us/library/azure/jj710104.aspx

            Get["/tables/{table_name}"] = this.HandleRequestForSiteDatabase(this.HandleQueryRequest);

            Get["/tables/{table_name}/count"] = this.HandleRequestForSiteDatabase(this.HandleCountRequest);

            Get["/tables/{table_name}/{item_id:int}"] = this.HandleRequestForSiteDatabase(this.HandleSingleItemRequest);

            Post["/tables/{table_name}"] = this.HandleRequestForSiteDatabase(this.HandleInsertUpdateRequest);

            Patch["/tables/{table_name}/{item_id:int}"] = this.HandleRequestForSiteDatabase(this.HandleInsertUpdateRequest);

            Delete["/tables/{table_name}/{item_id:int}"] = this.HandleRequestForSiteDatabase(this.HandleDeleteRecordRequest);

            // Files

            Post["/tables/{table_name}/{item_id:int}/files"] = this.HandleFileUploadRequest;

            Delete["/tables/{table_name}/{item_id:int}/files/{file_name}"] = this.HandleFileDeleteRequest;
        }
    }
}