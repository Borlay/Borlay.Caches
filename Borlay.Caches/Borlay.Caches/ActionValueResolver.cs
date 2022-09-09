using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Borlay.Caches
{
    public class ActionValueResolver<TKey, TValue> : IValueResolver<TKey, TValue> where TValue: class
    {
        private readonly Func<TKey, TValue> resolver;

        public ActionValueResolver(Func<TKey, TValue> resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public bool TryResolve(TKey key, out TValue value)
        {
            value = resolver.Invoke(key);
            return value != null;
        }
    }

    public class ActionValueResolverAsync<TKey, TValue> : IValueResolverAsync<TKey, TValue>
    {
        private readonly Func<TKey[], Task<KeyValuePair<TKey, TValue>[]>> resolver;

        public ActionValueResolverAsync(Func<TKey[], Task<KeyValuePair<TKey, TValue>[]>> resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public Task<KeyValuePair<TKey, TValue>[]> ResolveAsync(params TKey[] keys)
        {
            return resolver.Invoke(keys);
        }
    }
}
