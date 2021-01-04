using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NantCom.NancyBlack.Modules.ContentSystem.Types
{
    public class ImageSizeHeuristics : IStaticType
    {
        public int Id { get; set; }
        public DateTime __createdAt { get; set; }
        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Key to look for heuristics, Client will set this key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Image Url
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Image Width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Image Height
        /// </summary>
        public int Height { get; set; }

        private static Dictionary<string, ImageSizeHeuristics> _Heuristics = new Dictionary<string, ImageSizeHeuristics>();

        /// <summary>
        /// Get Size of the image from Combination of parameters
        /// </summary>
        /// <param name="pageUrl"></param>
        /// <param name="imageUrl"></param>
        /// <param name="screenWidth"></param>
        /// <returns></returns>
        public static ImageSizeHeuristics GetSize(string key)
        {
            if (key == null)
            {
                return null;
            }

            ImageSizeHeuristics result;
            if (_Heuristics.TryGetValue(key, out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Record the image heuristics
        /// </summary>
        /// <param name="pageUrl"></param>
        /// <param name="imageUrl"></param>
        /// <param name="screenWidth"></param>
        public static void SetHeuristics(string key, string imageUrl, int imageWidth, int imageHeight)
        {
            if (key == null || imageWidth <= 0)
            {
                return;
            }
            ImageSizeHeuristics result;
            if (_Heuristics.TryGetValue(key, out result))
            {
                // Keep the bigger size if the page use image in different places
                if (result.Width < imageWidth || result.Height < imageHeight)
                {
                    result.Width = imageWidth;
                    result.Height = imageHeight;
                }
            }
            else
            {
                _Heuristics[key] = new ImageSizeHeuristics()
                {
                    Width = imageWidth,
                    Height = imageHeight,
                    Key = key,
                    ImageUrl = imageUrl
                };
            }
        }

        /// <summary>
        /// Load Heuristics Data
        /// </summary>
        /// <param name="db"></param>
        public static void LoadHeuristics()
        {
            if (_Heuristics != null && _Heuristics.Count != 0)
            {
                return;
            }

            var db = NancyBlackDatabase.GetSiteDatabase(BootStrapper.RootPath);
            var list = db.Query<ImageSizeHeuristics>();

            _Heuristics = new Dictionary<string, ImageSizeHeuristics>();

            foreach (var row in list)
            {
                _Heuristics[row.Key] = row;
            }
        }

        private static long _SaveCounter = 0;
        private static DateTime _LastSaved;

        /// <summary>
        /// Save Heuristics Data
        /// </summary>
        /// <param name="db"></param>
        public static void SaveHeuristics()
        {
            if (DateTime.Now.Subtract(_LastSaved).TotalSeconds < 5)
            {
                return;
            }

            _LastSaved = DateTime.Now;
            var myId = Interlocked.Increment(ref _SaveCounter);

            Task.Run(() =>
            {
                Task.Delay(3000).Wait();
                if (myId < _SaveCounter)
                {
                    return;
                }

                var db = NancyBlackDatabase.GetSiteDatabase(BootStrapper.RootPath);
                db.Transaction(() =>
                {
                    foreach (var item in _Heuristics.Values)
                    {
                        db.UpsertRecord<ImageSizeHeuristics>(item);
                    }
                });
            });
        }

    }
}