using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly object LockObject = new();
        private static HttpClient? _httpClient;
        private static readonly Dictionary<string, byte[]> ImageCache = new();

        private static IBrowser? BrowserInternal { get; set; }
        private static readonly Lazy<Task<IPlaywright>> LazyPlaywright = new(() => Playwright.CreateAsync());
        private static readonly Lazy<Task<IBrowser>> LazyBrowser = new(async () =>
        {
            try
            {
                var playwright = await LazyPlaywright.Value;
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false
                });
            }
            catch (Exception ex)
            {
                Logger.Critical(ex.Message + ex.StackTrace);
                throw;
            }
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

        public static async Task<byte[]?> DownloadImageAsync(string imageUrl)
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

        private static async Task<IPage> CreateNewPageAsync()
        {
            var browser = await GetBrowserAsync();
            return await browser.NewPageAsync();
        }

        public static async Task<byte[]> GetWebsiteScreenshot(string url)
        {
            byte[] returnNull = Encoding.UTF8.GetBytes("null");

            IPage? page = null;
            try
            {
                page = await CreateNewPageAsync();
                await page.GotoAsync(url);

                await page.EvaluateAsync("document.getElementById('onetrust-button-group') !== null");
                
                await Task.Delay(TimeSpan.FromSeconds(5));

                const string buttonSelector = "#onetrust-accept-btn-handler";
                var element = await page.QuerySelectorAsync(buttonSelector);

                if (element != null)
                {
                    await page.ClickAsync(buttonSelector);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));

                var screenshotData = await page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png, FullPage = true });

                await StoreScreenshotDataIntoCache(url, screenshotData);

                return screenshotData;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                return returnNull;
            }
            finally
            {
                if (page != null && !page.IsClosed)
                {
                    await page.CloseAsync();
                }
            }
        }

        public static async Task<CachedNews?> GetAllScreenshots(CachedNews cachedNews)
        {
            var screenshotData = new Dictionary<string, byte[]>();

            if (NullCheck(cachedNews))
            {
                return null;
            }

            var tasks = cachedNews.NewsUrls!.Select(async link =>
            {
                IPage? page = null;
                try
                {
                    page = await CreateNewPageAsync();
                    await page.GotoAsync(link);
                    
                    await page.EvaluateAsync("document.getElementById('onetrust-button-group') !== null");

                    await Task.Delay(TimeSpan.FromSeconds(5));

                    const string buttonSelector = "#onetrust-accept-btn-handler";
                    var element = await page.QuerySelectorAsync(buttonSelector);

                    if (element != null)
                    {
                        await page.ClickAsync(buttonSelector);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5));

                    var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Type = ScreenshotType.Png,
                        FullPage = true
                    });

                    Logger.Debug($"Finished download of {link}");
                    return new { link, screenshot };
                }
                catch (Exception ex)
                {
                    byte[] returnNull = Encoding.UTF8.GetBytes("null");
                    
                    if (ex.Message.Contains("ERR_CONNECTION_CLOSED ", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Debug($"Cryptic Issue: {ex.Message}");
                        return new { link, screenshot = returnNull };
                    }
                    
                    return new { link, screenshot = returnNull };
                }
                finally
                {
                    if (page != null && !page.IsClosed)
                    {
                        await page.CloseAsync();
                    }
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                screenshotData[result.link] = result.screenshot;
            }

            return new CachedNews
            {
                NewsUrls = cachedNews.NewsUrls,
                ScreenshotData = screenshotData
            };
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

        public static bool NullCheck(List<NewsInfo>? newsInfos)
        {
            if (newsInfos == null)
            {
                return true;
            }

            return newsInfos.Count == 0;
        }
        public static bool NullCheck(List<EventInfo>? eventInfos) 
        {
            return eventInfos == null || eventInfos.Count == 0;
        }

        public static bool NullCheck(byte[]? bytes) => bytes == null || bytes.Length == 0;
        public static bool NullCheck(string str) => string.IsNullOrEmpty(str);
        public static bool NullCheck(CachedNews? cachedNews) => cachedNews == null;
        public static bool NullCheck(NewsNodes? newsNodes)
        {
            if (newsNodes == null) return true;
            return string.IsNullOrEmpty(newsNodes.Node0) || string.IsNullOrEmpty(newsNodes.Node1) || string.IsNullOrEmpty(newsNodes.Node2) || string.IsNullOrEmpty(newsNodes.Hash);
        }

        public static bool NullCheck(CachedInfo? cachedInfo)
        {
            if (cachedInfo == null) return true;
            
            if (cachedInfo.NewsInfos == null || cachedInfo.NewsInfos.Count == 0)
            {
                if (cachedInfo.EventInfos == null || cachedInfo.EventInfos.Count == 0)
                {
                    return true;
                }
            }

            return false;
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
