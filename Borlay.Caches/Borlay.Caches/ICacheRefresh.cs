using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public interface ICacheRefresh<TKey, TValue> : ICacheSet<TKey, TValue>
    {
        event Action<ICacheRefresh<TKey, TValue>> Refresh;

        IEnumerable<IValueAge<TKey>> GetValueAges();
    }
}
