using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class GetNewsImage
    {
        private static async Task<byte[]> GetScreenshot(string url)
        {
            try
            {
                if (Helper._browser == null || Helper._page == null)
                {
                    await Helper.InitBrowser();
                }
                
                CachedNews cachedNews = await Cache.GetCachedNewsAsync();

                if (cachedNews.ScreenshotData[url] != null && cachedNews.ScreenshotData.TryGetValue(url, out var screenshotData))
                {
                    Logger.Info("Cache hit");
                    
                    return screenshotData;
                }
                else
                {
                    Logger.Info("Cache miss");
                    return await Helper.GetWebsiteScreenshot(cachedNews, url);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error in GetScreenshot: {e.Message} {e.StackTrace}");
                throw;
            }
        }

        public static async Task<string> CallScreenshot(int index)
        {
            try
            {
                string url = await GetNewsLink(index);

                Logger.Info(url);
                
                byte[] screenshotData = await GetScreenshot(url);

                Logger.Info(screenshotData.Length.ToString());

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

            if (index > cachedNews.NewsUrls.Count)
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