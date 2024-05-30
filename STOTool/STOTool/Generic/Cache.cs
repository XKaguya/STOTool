using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Playwright;
using STOTool.Class;
using STOTool.Feature;

namespace STOTool.Generic
{
    public static class Cache
    {
        private static readonly IMemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());
        private static readonly string CacheKey = "CachedInfo";
        private static readonly string NewsCacheKey = "NewsCache";
        private static readonly string FastCacheKey = "FastCashe";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan NewsCacheExpiration = TimeSpan.FromDays(1);
        private static readonly TimeSpan FastCacheExpiration = TimeSpan.FromMinutes(1);
        
        public static async Task<CachedInfo> GetCachedInfoAsync()
        {
            if (MemoryCache.TryGetValue(CacheKey, out CachedInfo cachedInfo))
            {
                return cachedInfo;
            }
            
            Task<List<NewsInfo>> newsTask = NewsProcessor.GetNewsContentsAsync();
            Task<List<EventInfo>> eventTask = Calendar.GetRecentEventsAsync();

            await Task.WhenAll(newsTask, eventTask);

            cachedInfo = new CachedInfo
            {
                NewsInfos = await newsTask,
                EventInfos = await eventTask
            };
            
            MemoryCache.Remove(CacheKey);

            MemoryCache.Set(CacheKey, cachedInfo, CacheExpiration);

            return cachedInfo;
        }

        public static async Task<MaintenanceInfo> GetFastCachedMaintenanceInfoAsync()
        {
            if (MemoryCache.TryGetValue(FastCacheKey, out MaintenanceInfo maintenanceInfo))
            {
                return maintenanceInfo;
            }
            
            MaintenanceInfo maintenanceTask = await ServerStatus.CheckServerAsync();

            if (Helper.NullCheck(maintenanceTask))
            {
                return null!;
            }
            
            MemoryCache.Remove(FastCacheKey);
            
            MemoryCache.Set(FastCacheKey, maintenanceTask, FastCacheExpiration);
            
            return maintenanceTask;
        }
        
        public static async Task<CachedNews> GetCachedNewsAsync()
        {
            if (MemoryCache.TryGetValue(NewsCacheKey, out CachedNews cachedNews))
            {
                return cachedNews;
            }

            Task<List<NewsInfo>> newsTask = NewsProcessor.GetNewsContentsAsync();
            var newsList = await newsTask;

            cachedNews = new CachedNews();
            cachedNews.NewsUrls = newsList.ConvertAll(input => input.NewsLink);
            cachedNews.ScreenshotData = new();

            foreach (var link in cachedNews.NewsUrls)
            {
                cachedNews.ScreenshotData[link] = null;
            }

            MemoryCache.Remove(NewsCacheKey);
            MemoryCache.Set(NewsCacheKey, cachedNews, NewsCacheExpiration);

            return cachedNews;
        }

        public static void UpdateCache(CachedNews cachedNews)
        {
            MemoryCache.Remove(NewsCacheKey); 
            MemoryCache.Set(NewsCacheKey, cachedNews, NewsCacheExpiration);
        }

        public static void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            MemoryCache.Set(key, value, absoluteExpirationRelativeToNow);
        }

        public static bool TryGetValue<T>(string key, out T value)
        {
            return MemoryCache.TryGetValue(key, out value);
        }
        
        public static async Task RemoveAll()
        {
            try
            {
                await Task.Run(() =>
                {
                    MemoryCache.Remove(FastCacheKey);
                    MemoryCache.Remove(CacheKey);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
            }
        }
    }
}