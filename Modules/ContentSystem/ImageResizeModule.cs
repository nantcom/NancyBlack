using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Plugins.WebP.Imaging.Formats;
using Nancy;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace NantCom.NancyBlack.Modules.ContentSystem
{
    public class ImageResizeModule : BaseModule
    {
        public const int MAX_PER_FILE = 20;

        public enum ImageResizeMode
        {
            Fill,
            FitWidth,
            Fit,
            FitWithBg,
        }

        public ImageResizeModule()
        {
            ImageSizeHeuristics.LoadHeuristics();

            Get["/__resize2/{w}/{h}/{mode}/{path*}"] = (args) =>
            {
                var path = (string)args.path;
                var w = (int)args.w;
                var h = (int)args.h;

                // Local Request wont resize at all
                if (this.Context.Request.Url.HostName.StartsWith("192.168"))
                {
                    if (path.StartsWith("/") == false)
                    {
                        path = "/" + path;
                    }

                    if (path.StartsWith("//"))
                    {
                        path = path.Replace("//", "/");
                    }

                    return this.Response.AsRedirect(path, Nancy.Responses.RedirectResponse.RedirectType.Temporary);
                }

                var mode = ImageResizeMode.Fit;
                if (Enum.TryParse<ImageResizeMode>((string)args.Mode, out mode) == false)
                {
                    mode = ImageResizeMode.Fit;
                }

                return this.ResizeImage(path, w, h, mode);
            };

            Get["/__resize3"] = (arg) =>
            {
                // Local Request wont resize at all
                if (this.Context.Request.Url.HostName.StartsWith("192.168"))
                {
                    return this.Response.AsRedirect((string)this.Request.Query.path, Nancy.Responses.RedirectResponse.RedirectType.Permanent);
                }

                var mode = ImageResizeMode.Fit;

                if (Enum.TryParse<ImageResizeMode>((string)this.Request.Query.Mode, out mode) == false)
                {
                    mode = ImageResizeMode.Fit;
                }

                return this.ResizeImage(
                    (string)this.Request.Query.path,
                    (int)this.Request.Query.w,
                    (int)this.Request.Query.h,
                    mode);
            };

            Get["/__resizeh/{key}"] = (args) =>
            {
                // Local Request wont use resize
                if (this.Context.Request.Url.HostName.StartsWith("192.168"))
                {
                    return 404;
                }

                var mode = ImageResizeMode.Fill;
                var key = (string)args.key;
                string ext = null;

                if (key.Length > 32)
                {
                    ext = key.Substring(33);
                    key = key.Substring(0, 32);
                }

                var heuristics = ImageSizeHeuristics.GetSize(key);

                if (heuristics == null)
                {
                    return 404;
                }

                return this.ResizeImage(
                    (string)heuristics.ImageUrl,
                    (int)heuristics.Width,
                    (int)heuristics.Height,
                    mode,
                    ext);
            };

            Get["/__resizeh-bg/{key}"] = (args) =>
            {
                // Local requests wont use resize
                if (this.Context.Request.Url.HostName.StartsWith("192.168"))
                {
                    return 404;
                }

                var mode = ImageResizeMode.Fit;
                var key = (string)args.key;
                string ext = null;

                if (key.Length > 32)
                {
                    ext = key.Substring(33);
                    key = key.Substring(0, 32);
                }

                var heuristics = ImageSizeHeuristics.GetSize(key);

                if (heuristics == null)
                {
                    return 404;
                }

                var max = Math.Max(heuristics.Width, heuristics.Height);

                return this.ResizeImage(
                    (string)heuristics.ImageUrl,
                    heuristics.Width == max ? max : 0,
                    heuristics.Height == max ? max : 0,
                    mode,
                    ext);
            };

            Post["/__resize/heuristics"] = this.HandleRequest((arg) =>
            {
                var result = arg.body.Value as JArray;
                foreach (dynamic item in result)
                {
                    ImageSizeHeuristics.SetHeuristics((string)item.key, (string)item.imageUrl, (int)item.width, (int)item.height);
                }

                ImageSizeHeuristics.SaveHeuristics();

                return 200;

            });
        }

        private static readonly string[] _Supported = new string[] { "jpg", "jpeg", "png", "gif" };

        /// <summary>
        /// Resize the image
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private dynamic ResizeImage(string path, int w, int h, ImageResizeMode mode, string ext = null)
        {
            path = Path.Combine(this.RootPath, path.StartsWith("/") ? path.Substring(1) : path);
            if (File.Exists(path) == false)
            {
                return 404;
            }

            if ( _Supported.Any( fileExt => path.ToLowerInvariant().EndsWith( $".{fileExt}" ) ) == false )
            {
                return 400;
            }


            var libraryMode = ResizeMode.Crop;
            switch (mode)
            {
                case ImageResizeMode.Fill:
                    libraryMode = ImageProcessor.Imaging.ResizeMode.Crop;
                    break;
                case ImageResizeMode.FitWidth:
                    libraryMode = ImageProcessor.Imaging.ResizeMode.Max;
                    break;
                case ImageResizeMode.Fit:
                    libraryMode = ImageProcessor.Imaging.ResizeMode.Max;
                    break;
                case ImageResizeMode.FitWithBg:
                    libraryMode = ImageProcessor.Imaging.ResizeMode.Pad;
                    break;
                default:
                    break;
            }

            var size = new Size(w, h);

            var response = new Response();
            var resultMS = new MemoryStream();

            using (var ms = new MemoryStream(File.ReadAllBytes(path)))
            using (ImageFactory imageFactory = new ImageFactory(preserveExifData: false))
            {
                var result = imageFactory.Load(ms)
                                         .Resize(new ResizeLayer(size, libraryMode));

                if (ext == "webp" ||
                    this.Request.Headers.Accept.Any(t => t.Item1.Contains("/webp")))
                {
                    result = result.Format(new WebPFormat() { Quality = 80 });
                }

                response.ContentType = result.CurrentImageFormat.MimeType;

                result.Save(resultMS);
            }

            response.Contents = (s) =>
            {
                resultMS.CopyTo(s);
            };

            return response
                    .WithHeader("Cache-Control", "public, max-age=3600");
        }
    }
}