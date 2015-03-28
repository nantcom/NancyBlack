using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using SisoDb.SqlCe4;
using SisoDb;
using System.Collections.Specialized;
using Linq2Rest.Parser;
using System.Linq.Expressions;
using System.Reflection;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using System.Runtime.Caching;

namespace NantCom.NancyBlack.Modules
{

    public class DataModule : BaseModule
    {
        private string _RootPath;

        public DataModule(IRootPathProvider rootProvider)
            : base(rootProvider)
        {
            _RootPath = rootProvider.GetRootPath();

            // the interface of data mobile is compatible with Azure Mobile Service
            // http://msdn.microsoft.com/en-us/library/azure/jj710104.aspx

            Get["/tables/{table_name}"] = this.HandleRequestForSiteDatabase(this.HandleQueryRequest);

            Post["/tables/{table_name}"] = this.HandleRequestForSiteDatabase(this.HandleInsertUpdateRequest);

            Patch["/tables/{table_name}/{item_id:int}"] = this.HandleRequestForSiteDatabase(this.HandleInsertUpdateRequest);

            Delete["/tables/{table_name}/{item_id:int}"] = this.HandleRequestForSiteDatabase(this.HandleDeleteRecordRequest);


            Get["/system/tables/{table_name}"] = this.HandleRequestForSharedDatabase(this.HandleQueryRequest);

            Post["/system/tables/{table_name}"] = this.HandleRequestForSharedDatabase(this.HandleInsertUpdateRequest);

            Patch["/system/tables/{table_name}/{item_id:int}"] = this.HandleRequestForSharedDatabase(this.HandleInsertUpdateRequest);

            Delete["/system/tables/{table_name}/{item_id:int}"] = this.HandleRequestForSharedDatabase(this.HandleDeleteRecordRequest);

            // Files

            Get["/tables/{table_name}/{item_id:int}/files"] = this.HandleFileListRequest;

            Post["/tables/{table_name}/{item_id:int}/files"] = this.HandleFileUploadRequest;

            Delete["/tables/{table_name}/{item_id:int}/files/{file_name}"] = this.HandleFileDeleteRequest;

            // Special Handling for Site, which must update cache

            Post["/system/tables/site"] = this.HandleUpdateRequestForSiteTable(this.HandleInsertUpdateRequest);

            Patch["/system/tables/site/{item_id:int}"] = this.HandleUpdateRequestForSiteTable(this.HandleInsertUpdateRequest);

            Delete["/system/tables/site/{item_id:int}"] = this.HandleUpdateRequestForSiteTable(this.HandleDeleteRecordRequest);


        }

        protected string GetAttachmentFolder(string tableName, string id)
        {
            var path = Path.Combine(this.GetSiteFolder(), "Attachments", tableName, id);
            Directory.CreateDirectory(path);

            return path;
        }

        /// <summary>
        /// List files from attachments folder of the item
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected dynamic HandleFileListRequest(dynamic args)
        {
            var tableName = (string)args.table_name;
            var id = (string)args.item_id;
            var path = this.GetAttachmentFolder(tableName, id);

            return Directory.GetFiles(path);
        }

        /// <summary>
        /// Attach file to a record
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="id"></param>
        protected string AttachFile(string tableName, string id, string fileName, Stream input, bool replace = true)
        {
            var path = this.GetAttachmentFolder(tableName, id);
            var filePath = Path.Combine(path, fileName);

            if (File.Exists(filePath) && replace == false)
            {
                fileName = Path.GetFileNameWithoutExtension(fileName) +
                    Guid.NewGuid() +
                    Path.GetExtension(fileName);

                filePath = Path.Combine(path, fileName);
            }

            using (var fs = File.Create(filePath))
            {
                input.CopyTo(fs);
                return (
                    Path.Combine(
                        "/Sites",
                        (string)this.CurrentSite.HostName,
                        "Attachments",
                        tableName,
                        id,
                        fileName).Replace('\\', '/'));
            }
        }

        /// <summary>
        /// Attach file to a record
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="id"></param>
        protected string AttachFileFromDataUri(string tableName, string id, string fileName, string dataUri, bool replace = true)
        {
            // cut out data-uri
            if (dataUri.StartsWith("data:") == false)
            {
                throw new InvalidOperationException(" must be data uri");
            }

            var dataStart = dataUri.IndexOf("base64,") + "base64,".Length;
            dataUri = dataUri.Substring(dataStart);

            var stream = new MemoryStream(Convert.FromBase64String(dataUri));
            return this.AttachFile(tableName, id.ToString(), fileName, stream);
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

            List<string> urls = new List<string>();

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
                    urls.Add(
                        Path.Combine(
                            "/Sites",
                            (string)this.CurrentSite.HostName,
                            "Attachments",
                            tableName,
                            id,
                            fileName).Replace('\\', '/'));
                }
            }

            return urls;
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
                try
                {
                    File.Delete(path);
                }
                catch (Exception)
                {
                    return 500;
                }
            }

            return 204;
        }

        protected dynamic HandleRequestForSharedDatabase(Func<NancyBlackDatabase, dynamic, dynamic> action)
        {
            return this.HandleRequest((arg) =>
            {
                return action(this.SharedDatabase, arg);
            });
        }

        protected dynamic HandleRequestForSiteDatabase(Func<NancyBlackDatabase, dynamic, dynamic> action)
        {
            return this.HandleRequest((arg) =>
            {
                return action(this.SiteDatabase, arg);
            });
        }

        protected dynamic HandleUpdateRequestForSiteTable(Func<NancyBlackDatabase, dynamic, dynamic> action)
        {
            return this.HandleRequest((arg) =>
            {
                if (arg.item_id != null)
                {
                    dynamic modifiedSite = this.SharedDatabase.Query("Site",
                                            string.Format("Id eq {0}", (string)arg.item_id)).FirstOrDefault();

                    if (modifiedSite != null)
                    {
                        MemoryCache.Default.Remove("Site-" + modifiedSite.HostName);
                        MemoryCache.Default.Remove("Site-" + modifiedSite.Alias);
                        MemoryCache.Default.Remove("SiteDatabse-" + modifiedSite.HostName);
                        MemoryCache.Default.Remove("SiteDatabse-" + modifiedSite.Alias);
                    }
                }

                arg.table_name = "site";

                return action(this.SharedDatabase, arg);
            });
        }

        /// <summary>
        /// Create a function which will Handles the request.
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

            dynamic record = null;
            var fromClient = arg.body.Value as JObject;
            var imageBase64 = fromClient.Value<string>("Image");

            if (imageBase64 != null && imageBase64.StartsWith("data:") == true)
            {
                fromClient["Image"] = string.Empty; // remove image data as it will be overflow when save
            }

            Func<string> saveDataUriImage = () =>
            {
                if (imageBase64 != null)
                {
                    var fileName = "image.jpg";

                    // cut out data-uri
                    if (imageBase64.StartsWith("data:"))
                    {
                        if (imageBase64.IndexOf("image/png") > 0)
                        {
                            fileName = "image.png";
                        }

                        var dataStart = imageBase64.IndexOf("base64,") + "base64,".Length;
                        imageBase64 = imageBase64.Substring(dataStart);

                        var stream = new MemoryStream(Convert.FromBase64String(imageBase64));
                        return this.AttachFile(entityName, id.ToString(), fileName, stream);
                    }
                }

                return null;
            };

            if (id == 0)
            {
                //insert, need to insert first to get id
                record = db.UpsertRecord(entityName, fromClient);
                id = record.Id;

                // then upload and save
                var url = saveDataUriImage();
                if (url != null)
                {
                    record.Image = url;
                    record = db.UpsertRecord(entityName, fromClient);
                }
            }
            else
            {
                // update, can save immediately
                var url = saveDataUriImage();
                if (url != null)
                {
                    fromClient["Image"] = url;
                }

                record = db.UpsertRecord(entityName, fromClient);
            }




            return this.Negotiate
                .WithContentType("application/json")
                .WithModel((object)record);
        }

        protected dynamic HandleQueryRequest(NancyBlackDatabase db, dynamic arg)
        {
            var entityName = (string)arg.table_name;
            IList<string> rowsAsJson = db.QueryAsJsonString(entityName,
                                this.Request.Query["$filter"],
                                this.Request.Query["$orderby"]);

            // Write output row as RAW json
            var output = new Response();
            output.ContentType = "application/json";
            output.Contents = (s) =>
            {
                var sw = new StreamWriter(s);
                sw.Write("[");
                foreach (var row in rowsAsJson.Take(rowsAsJson.Count - 1))
                {
                    sw.WriteLine(row + ",");
                }

                if (rowsAsJson.Count > 0)
                {
                    sw.WriteLine(rowsAsJson.Last());
                }
                sw.Write("]");
                sw.Close();
                sw.Dispose();
            };

            return output;
        }

        protected dynamic HandleDeleteRecordRequest(NancyBlackDatabase db, dynamic arg)
        {
            var entityName = (string)arg.table_name;
            var id = arg.item_id == null ? 0 : (int?)arg.item_id;

            db.DeleteRecord(entityName, new { Id = id });

            return 204;
        }

    }
}