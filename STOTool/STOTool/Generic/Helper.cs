using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
        
        public static IBrowser _browser;
        public static IPage _page;
        
        private static readonly Lazy<Task<IPlaywright>> LazyPlaywright = new Lazy<Task<IPlaywright>>(() => Playwright.CreateAsync());
        private static readonly Lazy<Task<IBrowser>> LazyBrowser = new Lazy<Task<IBrowser>>(async () =>
        {
            var playwright = await LazyPlaywright.Value;
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        });

        public static Task<IPlaywright> GetPlaywrightAsync() => LazyPlaywright.Value;
        public static Task<IBrowser> GetBrowserAsync() => LazyBrowser.Value;
        
        public static async Task<bool> InitBrowser()
        {
            if (_browser == null)
            {
                _browser = await Helper.GetBrowserAsync();
                _page = await _browser.NewPageAsync();

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

        public static async Task<byte[]> GetWebsiteScreenshot(CachedNews cachedNews, string url)
        {
            await _page.GotoAsync(url);

            string button = "#onetrust-accept-btn-handler";

            var element = await _page.QuerySelectorAsync(button);

            if (element != null)
            {
                await _page.ClickAsync(button);
            }
                    
            cachedNews.ScreenshotData[url] = await _page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png, FullPage = true });

            return cachedNews.ScreenshotData[url];
        }
        
        public static async Task<CachedNews> GetAllScreenshot()
        {
            CachedNews cachedNews = await Cache.GetCachedNewsAsync();
            Dictionary<string, byte[]>? screenshotData = new Dictionary<string, byte[]>();

            foreach (var link in cachedNews.NewsUrls)
            {
                if (_browser == null || _page == null)
                {
                    await InitBrowser();
                }
                
                await _page.GotoAsync(link);
                string button = "#onetrust-accept-btn-handler";
                var element = await _page.QuerySelectorAsync(button);
                
                if (element != null)
                {
                    await _page.ClickAsync(button);
                }
                
                screenshotData[link] = await _page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png, FullPage = true });
                Logger.Info($"Finished download of {link}");
            }

            CachedNews cachedNewsAlter = new CachedNews()
            {
                NewsUrls = cachedNews.NewsUrls,
                ScreenshotData = screenshotData
            };

            return cachedNewsAlter;
        }
        
        public static bool NullCheck(DateTime? date)
        {
            return date == null;
        }
        
        public static bool NullCheck(MemoryStream? memoryStream)
        {
            return memoryStream == null;
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
        
        public static bool NullCheck(CachedNews cachedNews)
        {
            return cachedNews == null!;
        }

        public static bool NullCheck(CachedInfo cachedInfo)
        {
            return cachedInfo.EventInfos == null || cachedInfo.NewsInfos == null;
        }

        public static string StringTrim(string str, int length)
        {
#if DEBUG
            Logger.Debug($"{str}, {length}");
#endif
            if (str.Length > length)
            {
                string result = str.Substring(0, length);
                
                return result;
            }
            else
            {
                return str;
            }
        }
    }
}
