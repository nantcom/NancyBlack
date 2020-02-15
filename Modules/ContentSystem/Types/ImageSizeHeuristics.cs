using Nancy;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static void LoadHeuristics(NancyBlackDatabase db)
        {
            _Heuristics = new Dictionary<string, ImageSizeHeuristics>();
            var list = db.Query<ImageSizeHeuristics>();

            foreach (var row in list)
            {
                _Heuristics[row.Key] = row;
            }
        }

        /// <summary>
        /// Save Heuristics Data
        /// </summary>
        /// <param name="db"></param>
        public static void SaveHeuristics()
        {
        }

    }
}