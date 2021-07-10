using System;
using System.Linq;
using System.Runtime.Caching;

namespace Seq
{
    public static class CorrelationCache
    {
        private static readonly MemoryCache _cache;
        private static readonly CacheItemPolicy _policy;
        static CorrelationCache()
        {
            _cache = new MemoryCache("CorrelationCache");
            _policy = new CacheItemPolicy
                {SlidingExpiration = TimeSpan.FromSeconds(600), Priority = CacheItemPriority.Default};
        }

        public static int Count => _cache.Count();
        public static void Add(string threadId, string correlationId)
        {
            if (!Contains(threadId))
                _cache.Set(threadId, correlationId, _policy);
        }

        public static void Replace(string threadId, string correlationId)
        {
            if (Contains(threadId))
                _cache.Remove(threadId);
            _cache.Set(threadId, correlationId, _policy);
        }

        public static void Remove(string threadId)
        {
            if (Contains(threadId))
                _cache.Remove(threadId);
        }

        public static string Get(string threadId)
        {
            return (string) _cache.Get(threadId);
        }

        public static bool Contains(string threadId)
        {
            return _cache.Get(threadId) != null;
        }

        public static void Clear()
        {
            _cache.Trim(100);
        }
    }
}