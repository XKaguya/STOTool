using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using STOTool.Class;
using STOTool.Feature;

namespace STOTool.Generic
{
    public static class Cache
    {
        private static readonly IMemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());
        private static readonly SemaphoreSlim CacheLock = new SemaphoreSlim(1, 1);

        public static readonly string CacheKey = "CachedInfo";
        public static readonly string NewsCacheKey = "NewsCache";
        public static readonly string FastCacheKey = "FastCache";

        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(8);
        private static readonly TimeSpan NewsCacheExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan FastCacheExpiration = TimeSpan.FromMinutes(1);

        private static readonly Dictionary<string, DateTime> CacheSetTimes = new Dictionary<string, DateTime>();

        private static void SetCacheItemWithCallback<T>(string key, T value, TimeSpan expiration, PostEvictionDelegate onExpiration)
        {
            MemoryCache.Remove(key);
            
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal
            };

            cacheEntryOptions.RegisterPostEvictionCallback(onExpiration);

            Logger.Debug($"Setting cache item with key {key}, expiration {expiration}.");
            MemoryCache.Set(key, value, cacheEntryOptions);
            CacheSetTimes[key] = DateTime.Now;
            Logger.Debug($"Cache item with key {key} set with expiration {expiration} at {CacheSetTimes[key]}.");
        }

        private static async void OnExpired(object key, object value, EvictionReason reason, object state)
        {
            if (CacheSetTimes.TryGetValue(key.ToString(), out DateTime setTime))
            {
                var elapsedTime = DateTime.Now - setTime;
                Logger.Debug($"Cache item with key {key} expired after {elapsedTime.TotalSeconds} seconds due to {reason}.");
                CacheSetTimes.Remove(key.ToString());
            }

            try
            {
                switch (key.ToString())
                {
                    case "CachedInfo":
                        Logger.Debug("Executing callback for CachedInfo.");
                        await GetCachedInfoAsync();
                        break;
                    
                    case "NewsCache":
                        Logger.Debug("Executing callback for NewsCache.");
                        await GetCachedNewsAsync();
                        break;

                    case "FastCache":
                        Logger.Debug("Executing callback for FastCache.");
                        await GetFastCachedMaintenanceInfoAsync();
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error in OnExpired callback: {e.Message} {e.StackTrace}");
            }
        }

        public static async Task<CachedInfo> GetCachedInfoAsync()
        {
            await CacheLock.WaitAsync();
            try
            {
                if (MemoryCache.TryGetValue(CacheKey, out CachedInfo cachedInfo))
                {
                    Logger.Debug("CachedInfo found in memory cache.");
                    return cachedInfo;
                }

                Logger.Debug("CachedInfo not found in memory cache. Fetching new data.");
                var newsTask = NewsProcessor.GetNews();
                var eventTask = Calendar.GetRecentEventsAsync();

                await Task.WhenAll(newsTask, eventTask);

                cachedInfo = new CachedInfo
                {
                    NewsInfos = await newsTask,
                    EventInfos = await eventTask
                };

                SetCacheItemWithCallback(CacheKey, cachedInfo, CacheExpiration, OnExpired);

                return cachedInfo;
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public static async Task<MaintenanceInfo> GetFastCachedMaintenanceInfoAsync()
        {
            await CacheLock.WaitAsync();
            try
            {
                if (MemoryCache.TryGetValue(FastCacheKey, out MaintenanceInfo maintenanceInfo))
                {
                    Logger.Debug("FastCache found in memory cache.");
                    return maintenanceInfo;
                }

                Logger.Debug("FastCache not found in memory cache. Fetching new data.");
                maintenanceInfo = await ServerStatus.CheckServerAsync();

                if (Helper.NullCheck(maintenanceInfo))
                {
                    Logger.Debug("MaintenanceInfo is null.");
                    return null;
                }

                SetCacheItemWithCallback(FastCacheKey, maintenanceInfo, FastCacheExpiration, OnExpired);

                return maintenanceInfo;
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public static async Task<CachedNews> GetCachedNewsAsync()
        {
            await CacheLock.WaitAsync();
            try
            {
                if (MemoryCache.TryGetValue(NewsCacheKey, out CachedNews cachedNews))
                {
                    Logger.Debug("NewsCache found in memory cache.");
                    return cachedNews;
                }

                Logger.Debug("NewsCache not found in memory cache or expired. Fetching new data.");
                var newsList = await NewsProcessor.GetNews();

                cachedNews = new CachedNews
                {
                    NewsUrls = newsList.ConvertAll(input => input.NewsLink),
                    ScreenshotData = new Dictionary<string, byte[]>()
                };

                foreach (var link in cachedNews.NewsUrls)
                {
                    cachedNews.ScreenshotData[link] = null;
                }

                var cachedNewsInternal = await Helper.GetAllScreenshot(cachedNews);

                SetCacheItemWithCallback(NewsCacheKey, cachedNewsInternal, NewsCacheExpiration, OnExpired);

                return cachedNews;
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public static void UpdateCache(CachedNews cachedNews)
        {
            Logger.Debug("Updating NewsCache.");
            SetCacheItemWithCallback(NewsCacheKey, cachedNews, NewsCacheExpiration, OnExpired);
        }

        public static void Set<T>(string key, T value)
        {
            SetCacheItemWithCallback(key, value, NewsCacheExpiration, OnExpired);
        }

        public static bool TryGetValue<T>(string key, out T value)
        {
            Logger.Debug($"Trying to get cache item with key {key}.");
            return MemoryCache.TryGetValue(key, out value);
        }

        public static async Task RemoveAll()
        {
            await CacheLock.WaitAsync();
            try
            {
                Logger.Debug("Removing all cache items.");
                await Task.Run(() =>
                {
                    MemoryCache.Remove(FastCacheKey);
                    MemoryCache.Remove(CacheKey);
                    MemoryCache.Remove(NewsCacheKey);
                    CacheSetTimes.Clear();
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error removing cache items: {ex.Message} {ex.StackTrace}");
            }
            finally
            {
                CacheLock.Release();
            }
        }
    }
}