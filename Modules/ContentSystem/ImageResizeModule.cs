using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using ImageProcessor.Plugins.WebP.Imaging.Formats;
using Nancy;
using Nancy.Responses;
using NantCom.NancyBlack.Modules.ContentSystem.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Web;

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
            Get["/__resize2/{w}/{h}/{mode}/{path*}"] = (args) =>
            {
                var path = (string)args.path;
                var w = (int)args.w;
                var h = (int)args.h;

                var mode = ImageResizeMode.Fit;
                if (Enum.TryParse<ImageResizeMode>((string)args.Mode, out mode) == false)
                {
                    mode = ImageResizeMode.Fit;
                }

                return this.ResizeImage(path, w, h, mode);
            };

            Get["/__resize3"] = (arg) =>
            {
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
                var mode = ImageResizeMode.Fill;

                var heuristics = ImageSizeHeuristics.GetSize((string)args.key);

                if (heuristics == null)
                {
                    return 404;
                }

                return this.ResizeImage(
                    (string)heuristics.ImageUrl,
                    (int)heuristics.Width,
                    (int)heuristics.Height,
                    mode);
            };

            Get["/__resizeh-bg/{key}"] = (args) =>
            {
                var mode = ImageResizeMode.Fit;

                var heuristics = ImageSizeHeuristics.GetSize((string)args.key);

                if (heuristics == null)
                {
                    return 404;
                }

                var max = Math.Max(heuristics.Width, heuristics.Height);

                return this.ResizeImage(
                    (string)heuristics.ImageUrl,
                    heuristics.Width == max ? max : 0,
                    heuristics.Height == max ? max : 0,
                    mode);
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

        /// <summary>
        /// Resize the image
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private dynamic ResizeImage(string path, int w, int h, ImageResizeMode mode)
        {
            path = Path.Combine(this.RootPath, path.StartsWith("/") ? path.Substring(1) : path);
            if (File.Exists(path) == false)
            {
                return 404;
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

                if (this.Request.Headers.Accept.Any(t => t.Item1.Contains("/webp")))
                {
                    result = result.Format(new WebPFormat() { Quality = 60 });
                }

                response.ContentType = result.CurrentImageFormat.MimeType;

                result.Save(resultMS);
            }

            response.Contents = (s) =>
            {
                resultMS.CopyTo(s);
            };

            return response;
        }
    }
}