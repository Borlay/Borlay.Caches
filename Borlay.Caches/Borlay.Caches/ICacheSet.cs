using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public interface ICacheSet<TKey, TValue>
    {
        void Set(TKey key, TValue value, bool moveToEnd = true);

        void Remove(TKey key);
    }
}
