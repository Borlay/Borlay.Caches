using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public interface ICacheRefresh<TKey, TValue> : ICacheSet<TKey, TValue>
    {
        TimeSpan? EntityExpiresIn { get; }
        TKey[] GetAscendingWhile(Func<IValueAge<TKey>, bool> predicate, int count);
    }
}
