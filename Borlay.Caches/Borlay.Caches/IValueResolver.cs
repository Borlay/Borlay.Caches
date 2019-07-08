using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public interface IValueResolver<TKey, TValue>
    {
        KeyValuePair<TKey, TValue>[] Resolve(params TKey[] keys);
    }
}
