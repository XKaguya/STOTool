using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using STOTool.Class;
using STOTool.Feature;
using STOTool.Settings;

namespace STOTool.Generic
{
    public static class Cache
    {
        public static readonly IMemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new();

        public static readonly string CacheKey = "CachedInfo";
        public static readonly string NewsCacheKey = "NewsCache";
        public static readonly string FastCacheKey = "FastCache";

        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[0]);
        private static readonly TimeSpan NewsCacheExpiration = TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[1]);
        private static readonly TimeSpan FastCacheExpiration = TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[2]);

        public static readonly ConcurrentDictionary<string, DateTime> CacheSetTimes = new();
        
        private static readonly Dictionary<string, Timer> Timers = new();

        private static SemaphoreSlim GetOrCreateLock(string key)
        {
            return CacheLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }

        public static void StartCacheGuard()
        {
            Timers[CacheKey] = new Timer(CacheGuardCallback, CacheKey, TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[0]) , TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[0]));
            Timers[NewsCacheKey] = new Timer(CacheGuardCallback, NewsCacheKey, TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[1]), TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[1]));
            Timers[FastCacheKey] = new Timer(CacheGuardCallback, FastCacheKey, TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[2]), TimeSpan.FromMinutes(GlobalVariables.CacheLifeTime[2]));  
        }

        public static void CancelCacheGuard()
        {
            foreach (var kvp in Timers)
            {
                if (Timers.ContainsKey(kvp.Key))
                {
                    Timers[kvp.Key].Dispose();
                    Timers.Remove(kvp.Key);
                    Logger.Debug($"Cancelled cache guard for key {kvp.Key}.");
                }
            }
        }
        
        private static void CacheGuardCallback(object state)
        {
            var key = state as string;
            Logger.Debug($"Refreshing cache {key}.");

            try
            {
                if (MemoryCache.TryGetValue(key, out var value))
                {
                    OnExpired(key, value, EvictionReason.Expired, null);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error in CacheGuardCallback: {e.Message} {e.StackTrace}");
            }

            Logger.Debug($"Cache {key} has been force call the callback to ensure the cache callback was called.");
        }

        private static void SetCacheItemWithCallback<T>(string key, T value, TimeSpan expiration, PostEvictionDelegate onExpiration)
        {
            MemoryCache.Remove(key);

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            cacheEntryOptions.RegisterPostEvictionCallback(onExpiration);

            Logger.Debug($"Setting cache item with key {key}, expiration {expiration}.");
            MemoryCache.Set(key, value, cacheEntryOptions);

            CacheSetTimes[key] = DateTime.Now;

            Logger.Debug($"Cache item with key {key} set with expiration {expiration} at {CacheSetTimes[key]}.");
        }

        private static async void OnExpired(object? key, object? value, EvictionReason reason, object? state)
        {
            if (CacheSetTimes.TryRemove(key.ToString(), out DateTime setTime))
            {
                var elapsedTime = DateTime.Now - setTime;
                Logger.Debug($"Cache item with key {key} expired after {elapsedTime.TotalSeconds} seconds due to {reason}.");
            }

            try
            {
                var keyStr = key.ToString();
                if (keyStr == CacheKey)
                {
                    Logger.Debug("Executing callback for CachedInfo.");
                    await GetCachedInfoAsync();
                }
                else if (keyStr == NewsCacheKey)
                {
                    Logger.Debug("Executing callback for NewsCache.");
                    await GetCachedNewsAsync();
                }
                else if (keyStr == FastCacheKey)
                {
                    Logger.Debug("Executing callback for FastCache.");
                    await GetFastCachedMaintenanceInfoAsync();
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error in OnExpired callback: {e.Message} {e.StackTrace}");
            }
        }

        public static async Task<CachedInfo?> GetCachedInfoAsync()
        {
            var lockHandle = GetOrCreateLock(CacheKey);
            if (!await lockHandle.WaitAsync(0))
            {
                Logger.Debug("CacheLock is already acquired by another operation.");
                return null;
            }

            try
            {
                if (MemoryCache.TryGetValue(CacheKey, out CachedInfo? cachedInfo))
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
                lockHandle.Release();
            }
        }

        public static async Task<MaintenanceInfo?> GetFastCachedMaintenanceInfoAsync()
        {
            var lockHandle = GetOrCreateLock(FastCacheKey);
            if (!await lockHandle.WaitAsync(0))
            {
                Logger.Debug("CacheLock is already acquired by another operation.");
                return null;
            }
            
            try
            {
                if (MemoryCache.TryGetValue(FastCacheKey, out MaintenanceInfo? maintenanceInfo))
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
                lockHandle.Release();
            }
        }

        public static async Task<CachedNews?> GetCachedNewsAsync()
        {
            var lockHandle = GetOrCreateLock(NewsCacheKey);
            if (!await lockHandle.WaitAsync(0))
            {
                Logger.Debug("CacheLock is already acquired by another operation.");
                return null;
            }
            
            try
            {
                if (MemoryCache.TryGetValue(NewsCacheKey, out CachedNews? cachedNews))
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

                var cachedNewsInternal = await Helper.GetAllScreenshots(cachedNews);

                SetCacheItemWithCallback(NewsCacheKey, cachedNewsInternal, NewsCacheExpiration, OnExpired);

                return cachedNews;
            }
            finally
            {
                lockHandle.Release();
            }
        }

        public static void Set<T>(string key, T value)
        {
            SetCacheItemWithCallback(key, value, NewsCacheExpiration, OnExpired);
        }

        public static async Task RemoveAll()
        {
            var allLocks = CacheLocks.Values.ToList();
            foreach (var lockHandle in allLocks)
            {
                await lockHandle.WaitAsync();
            }

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
                foreach (var lockHandle in allLocks)
                {
                    lockHandle.Release();
                }
            }
        }
    }
}