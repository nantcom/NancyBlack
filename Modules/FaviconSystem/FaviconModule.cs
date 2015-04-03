using Nancy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace NantCom.NancyBlack.Modules.FaviconSystem
{
    public class FaviconModule : BaseModule
    {
        public FaviconModule(IRootPathProvider r) : base(r)
        {
            Get["/__favicon/{w}x{h}.{type}"] = this.CreateIcon;
        }

        private dynamic CreateIcon(dynamic arg)
        {
            var sourceFile = Path.Combine(this.RootPath, "Content", "favicon.png");

            if (File.Exists(sourceFile) == false)
            {
                return 404;
            }

            var w = (int)arg.w;
            var h = (int)arg.h;
            var type = arg.type;

            if (type != "ico" && type != "png")
            {
                return 404;
            }

            var key = string.Format("favicon-{0},{1}.{2}", w, h, type);
            var requestedFile = MemoryCache.Default[key] as byte[];

        WriteAgain:
            if (requestedFile != null)
            {
                var response = new Response();
                response.ContentType = "image/png";
                response.Contents = (s) =>
                {
                    s.Write(requestedFile, 0, requestedFile.Length);
                };
                return response;
            }

            lock (key)
            {
                Image source = Image.FromFile(sourceFile, true);

                Bitmap target = new Bitmap(w, h,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                Graphics g = Graphics.FromImage(target);
                g.DrawImage(source, 0, 0, w, h);

                var output = new MemoryStream();
                if (type == "ico")
                {
                    var icon = Icon.FromHandle(target.GetHicon());
                    icon.Save(output);
                    icon.Dispose();
                }
                else
                {
                    target.Save(output, System.Drawing.Imaging.ImageFormat.Png);
                }

                target.Dispose();
                source.Dispose();

                requestedFile = output.ToArray();
                MemoryCache.Default.Add(key, requestedFile, DateTimeOffset.MaxValue);

                goto WriteAgain;
            }
        }

    }
}