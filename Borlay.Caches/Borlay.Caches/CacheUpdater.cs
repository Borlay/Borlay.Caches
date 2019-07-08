using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Borlay.Caches
{
    public class CacheUpdater<TKey, TValue>
    {
        private readonly ICacheRefresh<TKey, TValue> cache;
        private readonly IValueResolver<TKey, TValue> valueResolver;

        public TimeSpan UpdateExpiredIn { get; set; }
        public int TakeCount { get; set; }

        public volatile bool updating = false;

        public CacheUpdater(ICacheRefresh<TKey, TValue> cache, IValueResolver<TKey, TValue> valueResolver,
            TimeSpan updateExpiredIn, int takeCount)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.valueResolver = valueResolver ?? throw new ArgumentNullException(nameof(valueResolver));

            if (takeCount <= 0)
                throw new ArgumentException($"{nameof(takeCount)} cannot be 0 or less");

            this.UpdateExpiredIn = updateExpiredIn;
            this.TakeCount = takeCount;

            this.cache.Refresh += Cache_Refresh;

            
        }

        public virtual async void Cache_Refresh(ICacheRefresh<TKey, TValue> cache)
        {
            if (updating) return;
            updating = true;

            try
            {
                var datenow = DateTime.Now;
                var keys = cache.GetValueAges()
                    .TakeWhile(e => e.UpdateTime.Add(UpdateExpiredIn) < datenow)
                    .Take(TakeCount*100).Select(e => e.Key).ToArray().AsEnumerable();

                await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        while (true)
                        {
                            var updateKeys = keys.Take(TakeCount).ToArray();
                            keys = keys.Skip(TakeCount);

                            if (updateKeys.Count() <= 0) break;

                            var keyPairs = valueResolver.Resolve(updateKeys);

                            foreach (var keyPair in keyPairs)
                            {
                                cache.Set(keyPair.Key, keyPair.Value, false);
                            }
                        }
                    }
                    finally
                    {
                        updating = false;
                    }
                });
            }
            finally
            {
                updating = false;
            }
        }
    }
}
