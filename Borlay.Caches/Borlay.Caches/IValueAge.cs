using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public interface IValueAge<TKey>
    {
        TKey Key { get; }

        DateTime UpdateTime { get; }
    }
}
