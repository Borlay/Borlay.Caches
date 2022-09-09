using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Borlay.Caches
{
    public class Cache<TKey, TValue> : ICache<TKey, TValue>, ICacheRefresh<TKey, TValue>
    {
        //public event Action<ICacheRefresh<TKey, TValue>> Refresh = (s) => { };

        private readonly LinkedDictionary<TKey, ValueNode<TKey, TValue>> usageDictionary = new LinkedDictionary<TKey, ValueNode<TKey, TValue>>();
        private readonly LinkedDictionary<TKey, Node<TKey>> valueAgeDictionary = new LinkedDictionary<TKey, Node<TKey>>();

        private IValueResolver<TKey, TValue> resolver;

        public TimeSpan? EntityExpiresIn { get; set; } = null;

        public int Capacity { get; }

        public int Count => usageDictionary.Count;

        public Cache(int capacity)
        {
            this.Capacity = capacity;
            this.EntityExpiresIn = null;
        }

        public Cache(int capacity, TimeSpan entityExpiresIn)
            : this(capacity, entityExpiresIn, null)
        {

        }

        public Cache(int capacity, TimeSpan entityExpiresIn, IValueResolver<TKey, TValue> resolver)
        {
            this.Capacity = capacity;
            this.EntityExpiresIn = entityExpiresIn;
            this.resolver = resolver;
        }

        public Cache(int capacity, IValueResolver<TKey, TValue> resolver)
        {
            this.Capacity = capacity;
            this.resolver = resolver;
        }

        public void SetResolver(IValueResolver<TKey, TValue> resolver)
        {
            lock (this)
            {
                this.resolver = resolver;
            }
        }

        public void Clear(int capacity)
        {
            lock(this)
            {
                foreach(var node in usageDictionary.Ascending())
                {
                    if (usageDictionary.Count > capacity)
                    {
                        valueAgeDictionary.Remove(node.Key);
                        usageDictionary.Remove(node);
                    }
                    else break;
                }
            }
        }

        protected void AddNew(TKey key, TValue value)
        {
            usageDictionary.Add(key, value);
            valueAgeDictionary.Add(key, DateTime.Now);

            Clear(Capacity);
        }

        public void SetMany(KeyValuePair<TKey, TValue>[] values, bool moveToEnd = true)
        {
            lock (this)
            {
                foreach (var kv in values)
                {
                    if (usageDictionary.TryGetNode(kv.Key, out var node))
                    {
                        if (moveToEnd)
                            usageDictionary.MoveToEnd(node);

                        var dateTime = DateTime.Now;

                        node.Value = kv.Value;
                        node.UpdateTime = dateTime;

                        SetValueAge(kv.Key, dateTime);

                        return;
                    }
                    else
                    {
                        AddNew(kv.Key, kv.Value);
                    }
                }
            }
        }

        public void Set(TKey key, TValue value, bool moveToEnd = true)
        {
            lock (this)
            {
                if (usageDictionary.TryGetNode(key, out var node))
                {
                    if (moveToEnd)
                        usageDictionary.MoveToEnd(node);

                    var dateTime = DateTime.Now;

                    node.Value = value;
                    node.UpdateTime = dateTime;

                    SetValueAge(key, dateTime);

                    return;
                }
                else
                {
                    AddNew(key, value);
                }
            }
        }

        protected void SetValueAge(TKey key, DateTime dateTime)
        {
            if (valueAgeDictionary.TryGetNode(key, out var ageNode))
            {
                ageNode.UpdateTime = dateTime;
                valueAgeDictionary.MoveToEnd(ageNode);
            }
            else
                valueAgeDictionary.Add(key, dateTime);
        }

        public void Remove(TKey key)
        {
            lock (this)
            {
                usageDictionary.Remove(key);
                valueAgeDictionary.Remove(key);
            }
        }

        /// <summary>
        /// Tries to get value without resolving it.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return TryResolveValue(key, null, out value);
        }

        protected bool TryResolveValue(TKey key, IValueResolver<TKey, TValue> resolver, out TValue value)
        {
            value = default(TValue);

            lock (this)
            {
                if (usageDictionary.TryGetNode(key, out var node))
                {
                    if (!EntityExpiresIn.HasValue || (node.UpdateTime.Add(EntityExpiresIn.Value) > DateTime.Now))
                    {
                        usageDictionary.MoveToEnd(node);
                        value = node.Value;
                        return true;
                    }
                    else
                    {
                        if (resolver == null)
                            return false;

                        if (!resolver.TryResolve(key, out var resolvedValue))
                            return false;

                        value = resolvedValue;

                        var dateTime = DateTime.Now;

                        node.Value = resolvedValue;
                        node.UpdateTime = dateTime;
                        usageDictionary.MoveToEnd(node);

                        SetValueAge(key, dateTime);

                        return true;
                    }
                }
                else
                {
                    if (resolver == null)
                        return false;

                    if (!resolver.TryResolve(key, out var resolvedValue))
                        return false;

                    AddNew(key, resolvedValue);
                    value = resolvedValue;
                    return true;
                }
            }
        }

        /// <summary>
        /// Tries to get value and if not exist tries to resolve value via IValueResolver.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public bool TryResolveValue(TKey key, out TValue value)
        {
            return TryResolveValue(key, this.resolver, out value);
        }

        public TValue ResolveValue(TKey key)
        {
            return ResolveValue(key, this.resolver);
        }

        public TValue ResolveValue(TKey key, IValueResolver<TKey, TValue> resolver)
        {
            if (TryResolveValue(key, resolver, out var value))
                return value;
            else
                throw new Exception($"Cannot resolve value for '{key}'");
        }

        public TKey[] GetAscendingWhile(Func<IValueAge<TKey>, bool> predicate, int count)
        {
            lock (this)
            {
                return valueAgeDictionary.Ascending().TakeWhile(predicate).Select(e => e.Key).Take(count).ToArray();
            }
        }

        public TValue this[TKey key]
        {
            set
            {
                Set(key, value);
            }
            get
            {
                return ResolveValue(key);
            }
        }
    }

    public static class Cache
    {
        public static CacheUpdater<TKey, TValue> CreateUpdater<TKey, TValue>(this ICacheRefresh<TKey, TValue> cache, TimeSpan updateExpired, TimeSpan refreshPeriod, int takeCount, IValueResolverAsync<TKey, TValue> resolver)
        {
            var updater = new CacheUpdater<TKey, TValue>(cache, resolver, updateExpired, refreshPeriod, takeCount);
            return updater;
        }

        public static CacheUpdater<TKey, TValue> CreateUpdater<TKey, TValue>(this ICacheRefresh<TKey, TValue> cache, TimeSpan refreshPeriod, int takeCount, IValueResolverAsync<TKey, TValue> resolver)
        {
            var updater = new CacheUpdater<TKey, TValue>(cache, resolver, 
                cache.EntityExpiresIn ?? throw new Exception("Entity expiration in Cache is not set"), 
                refreshPeriod, takeCount);

            return updater;
        }

        public static CacheUpdater<TKey, TValue> CreateUpdater<TKey, TValue>(this ICacheRefresh<TKey, TValue> cache, TimeSpan updateExpired, TimeSpan refreshPeriod, int takeCount, 
            Func<TKey[], Task<KeyValuePair<TKey, TValue>[]>> resolverAction)
        {
            var resolver = new ActionValueResolverAsync<TKey, TValue>(resolverAction);
            var updater = new CacheUpdater<TKey, TValue>(cache, resolver, updateExpired, refreshPeriod, takeCount);
            return updater;
        }

        public static CacheUpdater<TKey, TValue> CreateUpdater<TKey, TValue>(this ICacheRefresh<TKey, TValue> cache, TimeSpan refreshPeriod, int takeCount, 
            Func<TKey[], Task<KeyValuePair<TKey, TValue>[]>> resolverAction)
        {
            var resolver = new ActionValueResolverAsync<TKey, TValue>(resolverAction);

            var updater = new CacheUpdater<TKey, TValue>(cache, resolver,
                cache.EntityExpiresIn ?? throw new Exception("Entity expiration in Cache is not set"),
                refreshPeriod, takeCount);

            return updater;
        }

        public static Cache<TKey, TValue> Create<TKey, TValue>(int capacity, TimeSpan entityExpiresIn, Func<TKey, TValue> resolverAction) where TValue : class
        {
            var resolver = new ActionValueResolver<TKey, TValue>(resolverAction);
            return new Cache<TKey, TValue>(capacity, entityExpiresIn, resolver);
        }

        public static Cache<TKey, TValue> Create<TKey, TValue>(int capacity, Func<TKey, TValue> resolverAction) where TValue : class
        {
            var resolver = new ActionValueResolver<TKey, TValue>(resolverAction);
            return new Cache<TKey, TValue>(capacity, resolver);
        }
    }
}
