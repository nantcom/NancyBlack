using Nancy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
            SwapWidthHeight
        }

        /// <summary>
        /// internal use class which handles image resize calculation
        /// </summary>
        public class ImageFiltrationParameters
        {
            /// <summary>
            /// Target Width of the image, can be null to automatically calculate
            /// </summary>
            public int TargetWidth { get; set; }

            /// <summary>
            /// Target Height of the image, can be null to automatically calculate
            /// </summary>
            public int TargetHeight { get; set; }

            /// <summary>
            /// Resize Mode
            /// </summary>
            public ImageResizeMode Mode { get; set; }

            /// <summary>
            /// Name of the AForge image fileter to be applied to the image
            /// </summary>
            public string[] Filters { get; set; }

            /// <summary>
            /// Whether resize will be processed, after validate against the source image
            /// </summary>
            public bool IsProcessResize { get; private set; }

            /// <summary>
            /// Image offset and size, after validate against source image
            /// </summary>
            public RectangleF ImageOffsetAndSize { get; private set; }

            /// <summary>
            /// file name to process
            /// </summary>
            public string FileName { get; set; }

            /// <summary>
            /// Validates the parameters
            /// </summary>
            public void Validate(Image input)
            {
                // no width/height specified - use source size
                if (this.TargetWidth == 0 && this.TargetHeight == 0)
                {
                    this.TargetWidth = input.Width;
                    this.TargetHeight = input.Height;

                    this.IsProcessResize = false;
                    this.ImageOffsetAndSize = RectangleF.Empty;
                }
                else
                {
                    #region Very Complex Logic from my old code to determine final image size and offset to crop image

                    float newWidth = 0, newHeight = 0; // width to draw
                    float offsetX = 0, offsetY = 0; // in case user want to scale on both size - to keep aspect ratio, the image must be shifted

                    if (this.TargetWidth != 0 && this.TargetHeight == 0) // resize on width
                    {
                        var hPerW = (float)input.Height / input.Width;
                        newWidth = this.TargetWidth;
                        newHeight = (newWidth * hPerW);
                        this.TargetHeight = (int)newHeight;
                    }
                    else if (this.TargetHeight != 0 && this.TargetWidth == 0) // resize on height
                    {
                        var wPerH = (float)input.Width / input.Height;
                        newHeight = this.TargetHeight;
                        newWidth = (newHeight * wPerH);
                        this.TargetWidth = (int)newWidth;
                    }
                    else // resize on both
                    {
                        // no resize at all
                        if (this.TargetWidth == input.Width && this.TargetHeight == input.Height)
                        {
                            this.IsProcessResize = false;
                            this.ImageOffsetAndSize = RectangleF.Empty;
                        }
                        else
                        {

                            var hPerW = (float)input.Height / input.Width;
                            var wPerH = (float)input.Width / input.Height;

                            if (this.Mode == ImageResizeMode.SwapWidthHeight)
                            {
                                // width and height is swapped - change the target size
                                if ((this.TargetWidth > this.TargetHeight && input.Width < input.Height) ||
                                     (this.TargetHeight > this.TargetWidth && input.Height < input.Width))
                                {
                                    var oldHeight = this.TargetHeight;
                                    this.TargetHeight = this.TargetWidth;
                                    this.TargetWidth = oldHeight;

                                    this.Mode = ImageResizeMode.Fill;
                                }
                            }

                            // fix the width
                            newWidth = this.TargetWidth;
                            newHeight = newWidth * hPerW;

                            switch (this.Mode)
                            {
                                case ImageResizeMode.Fill:

                                    // height is too few - expand image in fill mode
                                    while (newHeight <= this.TargetHeight)
                                    {
                                        newWidth += 2; // increase by 1 pixel on each side
                                        newHeight = newWidth * hPerW;
                                    }

                                    offsetX = (this.TargetWidth - newWidth) / 2f;
                                    offsetY = (this.TargetHeight - newHeight) / 2f;

                                    break;
                                case ImageResizeMode.FitWidth:

                                    // width is fit, but height is not fit - center the image
                                    offsetY = (this.TargetHeight - newHeight) / 2f;

                                    break;
                                case ImageResizeMode.Fit:

                                    // width is fit but height is too much
                                    while (newHeight > this.TargetHeight)
                                    {
                                        // reduce width 1 pixel on each size
                                        newWidth -= 2;
                                        newHeight = newWidth * hPerW;
                                    }

                                    offsetX = (this.TargetWidth - newWidth) / 2f;
                                    offsetY = (this.TargetHeight - newHeight) / 2f;

                                    break;
                                default:
                                    break;
                            }
                        }


                    }
                    #endregion

                    this.IsProcessResize = true;
                    this.ImageOffsetAndSize = new RectangleF(offsetX, offsetY, newWidth, newHeight);

                    // now we try to calculate the final size - if it turns out that the desired image size
                    // if the same size as original - nothing needs to be done
                    if (this.ImageOffsetAndSize.Width == input.Width &&
                        this.ImageOffsetAndSize.Height == input.Height)
                    {
                        this.IsProcessResize = false;
                        this.ImageOffsetAndSize = RectangleF.Empty;
                    }
                }

            }

            /// <summary>
            /// Create new image filtration parameters
            /// </summary>
            /// <param name="ctx"></param>
            public ImageFiltrationParameters( NancyContext ctx, string fileName )
            {
                this.TargetWidth = ctx.Request.Query.w == null ? 0 : (int)ctx.Request.Query.w;
                this.TargetHeight = ctx.Request.Query.h == null ? 0 : (int)ctx.Request.Query.h;
                this.Mode = ctx.Request.Query.mode == null ? ImageResizeMode.Fit :
                            (ImageResizeMode)Enum.Parse(typeof(ImageResizeMode), (string)ctx.Request.Query.mode);
                this.Filters = ctx.Request.Query.filters == null ? null : ((string)ctx.Request.Query.filters).Split(',');
                this.IsProcessResize = false;
                this.ImageOffsetAndSize = RectangleF.Empty;
                this.FileName = fileName;
            }

            public override int GetHashCode()
            {
                return JsonConvert.SerializeObject( this ).GetHashCode();
            }
        }

        public class ResizeResult
        {
            public string SourceFile { get; set; }

            public string ContentType { get; set; }

            public Stream Output { get; set; }
        }

        /// <summary>
        /// Get Resized Image
        /// </summary>
        /// <param name="width">Desired with of the image - will be set to actual image width</param>
        /// <param name="height">Desired height of the image - will be set to actual image height</param>
        /// <returns></returns>
        public static ResizeResult ResizeAndFilterImage(string file, ImageFiltrationParameters parameters)
        {
            var result = new ResizeResult();
            result.Output = new MemoryStream();
            result.SourceFile = file;

            // make sure we have no problem with file lock
            var source = File.ReadAllBytes(file);
            using (var ms = new MemoryStream(source))
            using (var b = Bitmap.FromStream(ms))
            {
                bool isPNG = b.RawFormat.Equals(ImageFormat.Png);
                bool isJPEG = b.RawFormat.Equals(ImageFormat.Jpeg);
                
                parameters.Validate(b);

                Bitmap newBitMap = null;

                #region Resize

                if (parameters.IsProcessResize == true)
                {
                    newBitMap = new Bitmap(parameters.TargetWidth, parameters.TargetHeight);
                    var g = Graphics.FromImage(newBitMap);

                    if (isPNG == true)
                    {
                        g.Clear(Color.Transparent);
                    }
                    else
                    {
                        g.Clear(Color.White);
                    }

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    
                    g.DrawImage(b,  parameters.ImageOffsetAndSize.Left,
                                    parameters.ImageOffsetAndSize.Top,
                                    parameters.ImageOffsetAndSize.Width,
                                    parameters.ImageOffsetAndSize.Height);
                    g.Dispose();
                }
                else
                {
                    newBitMap = (Bitmap)b;
                }

                #endregion

                #region Filtration

                if (parameters.Filters != null)
                {
                    foreach (var filter in parameters.Filters)
                    {
                        if (filter.ToLower() == "grayscale")
                        {
                            var filtered = AForge.Imaging.Filters.Grayscale.CommonAlgorithms.Y.Apply(newBitMap);
                            newBitMap.Dispose();
                            newBitMap = filtered;
                            continue;
                        }

                        var name = " AForge.Imaging.Filters." + filter;
                        var filterType = Assembly.GetAssembly(typeof(AForge.Imaging.Filters.BaseFilter)).GetType(name, false, true);
                        if (filterType == null ||
                            filterType.IsAbstract ||
                            filterType.IsInterface ||
                            !filterType.IsPublic)
                        {
                            throw new InvalidOperationException("Filter name " + filter + " is not valid");
                        }

                        try
                        {
                            var instance = Activator.CreateInstance(filterType) as AForge.Imaging.Filters.IFilter;
                            if (instance == null)
                            {
                                throw new InvalidOperationException("Filter name " + filter + " is not valid");
                            }

                            var filtered = instance.Apply(newBitMap);
                            newBitMap.Dispose();
                            newBitMap = filtered;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException("Filter name " + filter + " is not valid", ex);
                        }

                    }
                }

                #endregion

                if (isJPEG == true)
                {
                    var ep = new EncoderParameters();
                    ep.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

                    var ici = (from item in ImageCodecInfo.GetImageEncoders()
                               where item.MimeType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
                               select item).FirstOrDefault();

                    try
                    {
                        newBitMap.Save(result.Output, ici, ep);
                    }
                    catch (Exception)
                    {
                        newBitMap.Save(result.Output, ImageFormat.Jpeg);
                    }

                    result.ContentType = "image/jpeg";
                }
                else
                {
                    result.ContentType = "image/png";
                    newBitMap.Save(result.Output, ImageFormat.Png);
                }

                newBitMap.Dispose();
            }

            result.Output.Position = 0;
            
            return result;
        }

        private static MemoryCache ImageResizeCache = new MemoryCache("Image");

        private static Dictionary<string, int> _FraudCount = new Dictionary<string, int>();

        private static Dictionary<string, string> _FileList = new Dictionary<string, string>();

        static ImageResizeModule()
        {
            BaseDataModule.AttachmentDeleted += BaseDataModule_AttachmentDeleted;
        }

        private static void BaseDataModule_AttachmentDeleted(NancyContext ctx, string tableName, dynamic contentItem, string filePath)
        {
            if (filePath.StartsWith("/"))
            {
                filePath = filePath.Substring(1);
            }

            var resizeDirectory = Path.Combine(ctx.GetRootPath(), filePath.Replace("/", "\\" ) + "-imageresize");

            if (Directory.Exists( resizeDirectory ))
            {
                Directory.Delete(resizeDirectory, true);
            }
        }

        public ImageResizeModule()
        {
            Get["/__resize/{path*}"] = this.ResizeImage;
            
        }

        private dynamic ResizeImage(dynamic arg)
        {
            var file = Path.Combine(this.RootPath, (string)arg.path);
            if (File.Exists(file) == false)
            {
                return 404;
            }

            if (this.Request.Url.Query == string.Empty)
            {
                return 400;
            }

            var parameter = new ImageResizeModule.ImageFiltrationParameters(this.Context, file);
            var parameterKey = parameter.GetHashCode().ToString();
            var key = file + parameterKey;
            string resizeRelativePath;

            if (_FileList.TryGetValue( key, out resizeRelativePath) == true)
            {
                return this.Response.AsRedirect(resizeRelativePath, Nancy.Responses.RedirectResponse.RedirectType.Permanent);
            }

            resizeRelativePath = (string)arg.path + "-imageresize/" + parameterKey + Path.GetExtension( file );

            var rootPath = this.RootPath;
            if (File.Exists("D:\\DATALOSS_WARNING_README.txt")) // running in azure
            {
                rootPath = "D:\\ImageResize";
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
            }

            var resizeDirectory = Path.Combine(rootPath, (string)arg.path + "-imageresize");
            Directory.CreateDirectory(resizeDirectory);

            var resizeFile = Path.Combine(this.RootPath, resizeRelativePath);

            lock (resizeFile)
            {
                if (File.Exists(resizeFile) == true)
                {
                    _FileList[key] = "/" + resizeRelativePath;
                    return this.Response.AsRedirect("/" + resizeRelativePath, Nancy.Responses.RedirectResponse.RedirectType.Permanent);
                }

                if (Directory.GetFiles(resizeDirectory).Length + 1 > MAX_PER_FILE)
                {
                    return 400;
                }
                
                var result = ImageResizeModule.ResizeAndFilterImage(file, parameter);

                using (var resized = File.OpenWrite( resizeFile ))
                {
                    result.Output.CopyTo(resized);
                }

                _FileList[key] = "/" + resizeRelativePath;
                return this.Response.AsRedirect("/" + resizeRelativePath, Nancy.Responses.RedirectResponse.RedirectType.Permanent);
            }

        }

        /// <summary>
        /// Clears all image resize cache
        /// </summary>
        public static void ClearCache()
        {
            ImageResizeModule.ImageResizeCache = new MemoryCache("Image");
            GC.Collect();
        }

    }
}