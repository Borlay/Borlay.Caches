using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public class ActionValueResolver<TKey, TValue> : IValueResolver<TKey, TValue>
    {
        private readonly Func<TKey[], KeyValuePair<TKey, TValue>[]> resolver;

        public ActionValueResolver(Func<TKey[], KeyValuePair<TKey, TValue>[]> resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public virtual KeyValuePair<TKey, TValue>[] Resolve(params TKey[] keys)
        {
            return resolver(keys);
        }
    }
}
