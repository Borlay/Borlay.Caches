using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Caches
{
    public class CacheUpdater<TKey, TValue>
    {
        private readonly ICacheRefresh<TKey, TValue> cache;
        private readonly IValueResolverAsync<TKey, TValue> valueResolver;

        public TimeSpan UpdateExpiredIn { get; set; }
        public TimeSpan RefreshPeriod { get; set; }
        public int UpdateBatchSize { get; set; }

        public volatile bool updating = false;

        public Action<string> Log { get; set; } = null;

        public CacheUpdater(ICacheRefresh<TKey, TValue> cache, IValueResolverAsync<TKey, TValue> valueResolver,
            TimeSpan updateExpiredIn, TimeSpan refreshPeriod, int updateBatchSize)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.valueResolver = valueResolver ?? throw new ArgumentNullException(nameof(valueResolver));

            if (updateBatchSize <= 0)
                throw new ArgumentException($"{nameof(updateBatchSize)} cannot be 0 or less");

            this.UpdateExpiredIn = updateExpiredIn;
            this.RefreshPeriod = refreshPeriod;
            this.UpdateBatchSize = updateBatchSize;
        }

        public virtual async void Run(CancellationToken cancellationToken)
        {
            await Task.Delay(RefreshPeriod);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                var datenow = DateTime.Now.Add(RefreshPeriod).AddMinutes(1);

                var expiredKeys = cache.GetAscendingWhile(e => e.UpdateTime.Add(UpdateExpiredIn) < datenow, UpdateBatchSize);
                Log?.Invoke($"Expired Keys: {expiredKeys.Length}");
                if (expiredKeys.Length == 0)
                {
                    await Task.Delay(RefreshPeriod);
                    continue;
                }

                var values = await valueResolver.ResolveAsync(expiredKeys);
                if(values == null || values.Length == 0)
                {
                    Log?.Invoke($"Nothing updated");

                    await Task.Delay(RefreshPeriod);
                    continue;
                }
                cache.SetMany(values, false);

                Log?.Invoke($"Cache updated. Count: {values.Length}");

                if(values.Length != expiredKeys.Length)
                {
                    var notFound = expiredKeys.Where(k => !values.Any(v => k.Equals(v.Key))).ToArray();
                    Log?.Invoke($"Not found: {notFound.Length}");

                    foreach (var notfoundKey in notFound)
                    {
                        cache.Remove(notfoundKey);
                    }
                }
            }
        }
    }
}
