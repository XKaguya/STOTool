using System;
using System.Text;
using System.Threading.Tasks;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class GetNewsImage
    {
        private static async Task<byte[]>? GetScreenshot(string url)
        {
            byte[] returnNull = Encoding.UTF8.GetBytes("null");
            
            try
            {
                CachedNews cachedNews = await Cache.GetCachedNewsAsync();

                if (Helper.NullCheck(cachedNews))
                {
                    return returnNull;
                }

                if (cachedNews.ScreenshotData!.TryGetValue(url, out var screenshotData))
                {
                    Logger.Info($"Cache hit: {url}");

                    return screenshotData;
                }
                else
                {
                    Logger.Info($"Cache miss, Theres {cachedNews.ScreenshotData.Count} inside Cache. Awaiting for new cache.");
                    return await Helper.GetWebsiteScreenshot(url);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error in GetScreenshot: {e.Message} {e.StackTrace}");
                return returnNull;
            }
        }

        public static async Task<string> CallScreenshot(int index)
        {
            try
            {
                byte[] returnNull = Encoding.UTF8.GetBytes("null");
                
                string url = await GetNewsLink(index);

                if (url == null)
                {
                    return "null";
                }
                
                byte[] screenshotData = await GetScreenshot(url);

                if (screenshotData == returnNull)
                {
                    return "null";
                }

                string base64String = Convert.ToBase64String(screenshotData);

                return base64String;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        private static async Task<string> GetNewsLink(int index)
        {
            CachedNews cachedNews = await Cache.GetCachedNewsAsync();
            
            if (Helper.NullCheck(cachedNews))
            {
                return null;
            }

            if (index > cachedNews.NewsUrls!.Count)
            {
                Logger.Error("Index out of range.");
                return cachedNews.NewsUrls[0];
            }
            else
            {
                return cachedNews.NewsUrls[index];
            }
        }
    }
}