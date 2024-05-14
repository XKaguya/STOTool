using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using STOTool.Class;
using STOTool.Feature;

namespace STOTool.Generic
{
    public static class Cache
    {
        private static readonly IMemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());
        private static readonly string CacheKey = "CachedInfo";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);
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

            MemoryCache.Set(CacheKey, cachedInfo, CacheExpiration);

            return cachedInfo;
        }

        public static async Task<MaintenanceInfo> GetFastCachedMaintenanceInfoAsync()
        {
            if (MemoryCache.TryGetValue(CacheKey, out MaintenanceInfo maintenanceInfo))
            {
                return maintenanceInfo;
            }
            
            MaintenanceInfo maintenanceTask = await ServerStatus.CheckServerAsync();

            if (maintenanceInfo == null)
            {
                return null;
            }
            
            MemoryCache.Set(CacheKey, maintenanceInfo, FastCacheExpiration);
            
            return maintenanceInfo;
        }

        public static T Get<T>(string key)
        {
            return MemoryCache.Get<T>(key);
        }

        public static void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            MemoryCache.Set(key, value, absoluteExpirationRelativeToNow);
        }

        public static bool TryGetValue<T>(string key, out T value)
        {
            return MemoryCache.TryGetValue(key, out value);
        }

        public static void Remove(string key)
        {
            MemoryCache.Remove(key);
        }
    }
}