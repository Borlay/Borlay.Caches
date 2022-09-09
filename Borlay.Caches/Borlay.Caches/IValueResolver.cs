using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Borlay.Caches
{
    public interface IValueResolver<TKey, TValue>
    {
        bool TryResolve(TKey key, out TValue value);
        
    }

    public interface IValueResolverAsync<TKey, TValue>
    {
        Task<KeyValuePair<TKey, TValue>[]> ResolveAsync(params TKey[] keys);
    }
}
