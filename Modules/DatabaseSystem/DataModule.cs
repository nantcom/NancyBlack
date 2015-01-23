using Nancy;
using Nancy.ModelBinding;
using NantCom.NancyBlack.Types;
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

        public DataModule( IRootPathProvider rootProvider ) : base( rootProvider )
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

        private string GetAttachmentFolder( string tableName, string id )
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
        private dynamic HandleFileListRequest(dynamic args)
        {
            var tableName = (string)args.table_name;
            var id = (string)args.item_id;
            var path = this.GetAttachmentFolder(tableName, id);

            return Directory.GetFiles(path);
        }

        private dynamic HandleFileUploadRequest(dynamic args)
        {
            var tableName = (string)args.table_name;
            var id =  (string)args.item_id;
            var path = this.GetAttachmentFolder(tableName, id);

            List<string> urls = new List<string>();

            foreach (var item in this.Request.Files)
            {
                var fileName = Path.GetFileName(item.Name);
                var filePath = Path.Combine(path, fileName);
                if (File.Exists( filePath ))
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
                            fileName).Replace( '\\', '/' ));
                }
            }

            return urls;
        }

        private dynamic HandleFileDeleteRequest(dynamic args)
        {
            var tableName = (string)args.table_name;
            var id = (string)args.item_id;
            var fileName = (string)args.file_name;

            var directory = this.GetAttachmentFolder(tableName, id);
            var path = Path.Combine(directory, fileName);

            if (File.Exists( path ))
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


        private dynamic HandleRequestForSharedDatabase( Func<NancyBlackDatabase, dynamic, dynamic> action )
        {
            return this.HandleRequest((arg) =>
            {
                return action(this.SharedDatabase, arg);
            });
        }

        private dynamic HandleRequestForSiteDatabase(Func<NancyBlackDatabase, dynamic, dynamic> action)
        {
            return this.HandleRequest((arg) =>
            {
                return action(this.SiteDatabase, arg);
            });
        }

        private dynamic HandleUpdateRequestForSiteTable(Func<NancyBlackDatabase, dynamic, dynamic> action)
        {
            return this.HandleRequest((arg) =>
            {
                if (arg.item_id != null)
                {
                    dynamic modifiedSite =  this.SharedDatabase.Query("Site",
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
        private dynamic HandleInsertUpdateRequest( NancyBlackDatabase db, dynamic arg )
        {
            var entityName = (string)arg.table_name;
            int id = arg.item_id == null ? 0 : (int)arg.item_id;

            // get the json from request body
            var streamReader = new StreamReader(this.Request.Body);
            var json = streamReader.ReadToEnd();
            
            var record = db.UpsertRecord(entityName, id, json);

            // TODO: Move Attachment to other place
            if (json.IndexOf( "AttachmentBase64" ) > 0)
            {
                dynamic inputJsonObject = JsonConvert.DeserializeObject(json);
                if (string.IsNullOrEmpty((string)inputJsonObject.AttachmentExtension))
                {
                    throw new InvalidOperationException("AttachmentExtension is required to use Attachment Feature. (data will not be saved to database)");
                }

                // this request has file attachment
                var attachmentFolder = Path.Combine( _RootPath, 
                                                "Site", 
                                                (string)this.CurrentSite.HostName,
                                                "Attachments",
                                                entityName);

                Directory.CreateDirectory( attachmentFolder );


                File.WriteAllBytes(
                    Path.Combine(attachmentFolder, record.Id.ToString() + "." + (string)inputJsonObject.AttachmentExtension),
                    Convert.FromBase64String((string)inputJsonObject.AttachmentBase64));

                record.AttachmentUrl =
                    "/CustomContent/Attachments/" + entityName + "/" +
                    record.Id + "." + (string)inputJsonObject.AttachmentExtension;

                db.UpsertRecord(entityName, record);
            }

            return this.Negotiate
                .WithContentType("application/json")
                .WithModel((object)record);
        }

        private dynamic HandleQueryRequest(NancyBlackDatabase db, dynamic arg)
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
                foreach (var row in rowsAsJson.Take( rowsAsJson.Count - 1) )
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

        private dynamic HandleDeleteRecordRequest(NancyBlackDatabase db, dynamic arg)
        {
            var entityName = (string)arg.table_name;
            var id = arg.item_id == null ? 0 : (int?)arg.item_id;

            db.DeleteRecord(entityName, new { Id = id });

            return 204;
        }

    }
}