using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Borlay.Caches
{
    public class Locker
    {
        private LockerItem[] locks;

        int _capacity;
        public int Capacity => _capacity;

        public Locker(int capacity)
        {
            this._capacity = capacity;
            locks = new LockerItem[capacity];
            for(int i = 0; i < capacity; i++)
            {
                locks[i] = new LockerItem();
            }
        }

        public IDisposable Enter<T>(T key)
        {
            var code = Math.Abs(key.GetHashCode());
            var index = code % _capacity;
            var obj = locks[index];
            Monitor.Enter(obj);
            return obj;
        }

        public bool IsEntered<T>(T key)
        {
            var code = key.GetHashCode();
            var index = code % _capacity;
            var obj = locks[index];
            return Monitor.IsEntered(obj);
        }
    }

    public class LockerItem : IDisposable
    {
        public void Dispose()
        {
            Monitor.Exit(this);
        }
    }
}
