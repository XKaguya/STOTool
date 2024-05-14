using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using STOTool.Class;

namespace STOTool.Generic
{
    public static class Helper
    {
        private static readonly object LockObject = new ();
        private static HttpClient? _httpClient;
        private static readonly Dictionary<string, byte[]> ImageCache = new ();

        public static HttpClient HttpClient
        {
            get
            {
                lock (LockObject)
                {
                    if (_httpClient == null || _httpClient.BaseAddress == null)
                    {
                        _httpClient = new HttpClient();
                    }
                    
                    return _httpClient;
                }
            }
        }

        public static async Task<byte[]>? DownloadImageAsync(string imageUrl)
        {
            try
            {
                if (ImageCache.TryGetValue(imageUrl, out var imageCache))
                {
                    return imageCache;
                }

                var response = await HttpClient.GetAsync(imageUrl);
                    
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    
                    ImageCache[imageUrl] = imageBytes;

                    return imageBytes;
                }
                else
                {
                    Logger.Error($"Failed to download image from {imageUrl}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception occurred while downloading image from {imageUrl}: {ex.Message}");
                return null;
            }
        }
        
        public static async Task<Image<Rgba32>> LoadImageAsync(Stream stream)
        {
            try
            {
                var imageBytes = new byte[stream.Length];
                await stream.ReadAsync(imageBytes, 0, (int)stream.Length);

                return Image.Load<Rgba32>(imageBytes);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading image: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        public static bool NullCheck(DateTime? date)
        {
            return date == null;
        }

        public static bool NullCheck(TimeSpan? time)
        {
            return time == null;
        }
        
        public static bool NullCheck(EventInfo? eventInfo)
        {
            return eventInfo == null;
        }
        
        public static bool NullCheck(NewsInfo? newsInfo)
        {
            return newsInfo == null;
        }
        
        public static bool NullCheck(MaintenanceInfo? maintenanceInfo)
        {
            return maintenanceInfo == null;
        }
        
        public static bool NullCheck(List<NewsInfo>? newsInfos)
        {
            return newsInfos == null;
        }
        
        public static bool NullCheck(List<EventInfo>? eventInfos)
        {
            return eventInfos == null;
        }
        
        public static bool NullCheck(byte[] bytes)
        {
            return bytes == null! || bytes.Length == 0;
        }
        
        public static bool NullCheck(string str)
        {
            return str == null!;
        }
        
        public static bool NullCheck(CachedInfo cachedInfo)
        {
            return cachedInfo.EventInfos == null || cachedInfo.NewsInfos == null;
        }
    }
}
