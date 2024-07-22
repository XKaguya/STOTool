using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
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
        
        private static IBrowser? BrowserInternal { get; set; }

        public static IBrowser Browser
        {
            get
            {
                if (BrowserInternal == null)
                {
                    InitBrowser();
                }

                return BrowserInternal;
            }
        }

        private static IPage? Page { get; set; }
        
        private static readonly Lazy<Task<IPlaywright>> LazyPlaywright = new (() => Playwright.CreateAsync());
        private static readonly Lazy<Task<IBrowser>> LazyBrowser = new (async () =>
        {
            var playwright = await LazyPlaywright.Value;
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            });
        });
        
        private static Task<IBrowser> GetBrowserAsync() => LazyBrowser.Value;
        private static IBrowser GetBrowser() => LazyBrowser.Value.Result;

        public static async Task<bool> InitBrowserAsync()
        {
            if (BrowserInternal == null)
            {
                BrowserInternal = await GetBrowserAsync();
                return true;
            }

            return false;
        }
        
        private static bool InitBrowser()
        {
            if (BrowserInternal == null)
            {
                BrowserInternal = GetBrowser();
                return true;
            }

            return false;
        }

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

        public static async Task<byte[]> GetWebsiteScreenshot(string url)
        {
            byte[] returnNull = Encoding.UTF8.GetBytes("null");
            
            try
            {
                Page = await Browser.NewPageAsync();

                await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                const string buttonSelector = "#onetrust-accept-btn-handler";
                var element = await Page.QuerySelectorAsync(buttonSelector);

                if (element != null)
                {
                    await Page.ClickAsync(buttonSelector);
                }

                await Task.Delay(TimeSpan.FromSeconds(2));

                var screenshotData = await Page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png, FullPage = true });

                await Page.CloseAsync();

                await StoreScreenshotDataIntoCache(url, screenshotData);
                
                return screenshotData;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                await Page.CloseAsync();
                return returnNull;
            }
        }

        public static async Task<CachedNews?> GetAllScreenshots(CachedNews cachedNews)
        {
            try
            {
                var screenshotData = new Dictionary<string, byte[]>();

                if (NullCheck(cachedNews))
                {
                    return null;
                }

                foreach (var link in cachedNews.NewsUrls!)
                {
                    Page = await Browser.NewPageAsync();

                    await Page.GotoAsync(link, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                    const string buttonSelector = "#onetrust-accept-btn-handler";
                    var element = await Page.QuerySelectorAsync(buttonSelector);

                    if (element != null)
                    {
                        await Page.ClickAsync(buttonSelector);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));

                    screenshotData[link] = await Page.ScreenshotAsync(new PageScreenshotOptions
                    { 
                        Type = ScreenshotType.Png, 
                        FullPage = true 
                    });

                    Logger.Info($"Finished download of {link}");

                    await Page.CloseAsync();
                }

                return new CachedNews
                {
                    NewsUrls = cachedNews.NewsUrls,
                    ScreenshotData = screenshotData
                };
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                await Page.CloseAsync();
                return null;
            }
        }

        private static async Task<bool> StoreScreenshotDataIntoCache(string url, byte[] data)
        {
            CachedNews cachedNews = await Cache.GetCachedNewsAsync();

            if (NullCheck(cachedNews))
            {
                return false;
            }

            cachedNews.ScreenshotData![url] = data;
            
            Cache.Set(Cache.NewsCacheKey, cachedNews);

            return true;
        }
        
        public static bool NullCheck(DateTime? date) => date == null;
        public static bool NullCheck(MemoryStream? memoryStream) => memoryStream == null;
        public static bool NullCheck(TimeSpan? time) => time == null;
        public static bool NullCheck(EventInfo? eventInfo) => eventInfo == null;
        public static bool NullCheck(NewsInfo? newsInfo) => newsInfo == null;
        public static bool NullCheck(MaintenanceInfo? maintenanceInfo) => maintenanceInfo == null;
        public static bool NullCheck(List<NewsInfo>? newsInfos) => newsInfos == null;
        public static bool NullCheck(List<EventInfo>? eventInfos) => eventInfos == null;
        public static bool NullCheck(byte[] bytes) => bytes == null || bytes.Length == 0;
        public static bool NullCheck(string str) => string.IsNullOrEmpty(str);
        public static bool NullCheck(CachedNews cachedNews) => cachedNews == null;
        public static bool NullCheck(NewsNodes newsNodes)
        {
            if (newsNodes == null) return true;
            return string.IsNullOrEmpty(newsNodes.Node0) || string.IsNullOrEmpty(newsNodes.Node1) || string.IsNullOrEmpty(newsNodes.Node2) || string.IsNullOrEmpty(newsNodes.Hash);
        }

        public static bool NullCheck(CachedInfo cachedInfo)
        {
            if (cachedInfo == null) return true;
            return cachedInfo.NewsInfos.Count == 0 && cachedInfo.EventInfos.Count == 0;
        }

        public static string StringTrim(string str, int length)
        {
            const string ellipsis = "...";

            if (str.Length > length)
            {
                return str.Substring(0, length) + ellipsis;
            }
            else
            {
                return str;
            }
        }
    }
}