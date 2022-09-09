using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public interface ICache<TKey, TValue> : ICacheSet<TKey, TValue>
    {
        TValue ResolveValue(TKey key);
        bool TryResolveValue(TKey key, out TValue value);
        TValue ResolveValue(TKey key, IValueResolver<TKey, TValue> resolver);
        void SetResolver(IValueResolver<TKey, TValue> resolver);
    }
}
