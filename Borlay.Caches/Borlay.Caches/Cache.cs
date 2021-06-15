﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Borlay.Caches
{
    public class Cache<TKey, TValue> : ICache<TKey, TValue>, ICacheRefresh<TKey, TValue>
    {
        public event Action<ICacheRefresh<TKey, TValue>> Refresh = (s) => { };

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

        public void SetResolver(Func<TKey[], KeyValuePair<TKey, TValue>[]> resolver)
        {
            lock (this)
            {
                this.resolver = new ActionValueResolver<TKey, TValue>(resolver);
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
                        try
                        {
                            if (resolver == null)
                                return false;

                            if (!resolver.TryResolveValue(key, out var resolvedValue))
                                return false;

                            value = resolvedValue;

                            var dateTime = DateTime.Now;

                            node.Value = resolvedValue;
                            node.UpdateTime = dateTime;
                            usageDictionary.MoveToEnd(node);

                            SetValueAge(key, dateTime);

                            return true;
                        }
                        finally
                        {
                            Refresh(this);
                        }
                    }
                }
                else
                {
                    if (resolver == null)
                        return false;

                    if (!resolver.TryResolveValue(key, out var resolvedValue))
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
                throw new Exception("Cannot ResolveValue value");
        }

        public IEnumerable<IValueAge<TKey>> GetValueAges()
        {
            return valueAgeDictionary.Ascending();
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
        public static Cache<TKey, TValue> Create<TKey, TValue>(int capacity, TimeSpan entityExpiresIn, IValueResolver<TKey, TValue> resolver, int updateTakeCount = 1000)
        {
            var cache = new Cache<TKey, TValue>(capacity, entityExpiresIn, resolver);
            var updater = new CacheUpdater<TKey, TValue>(cache, resolver, entityExpiresIn, updateTakeCount);

            return cache;
        }

        public static Cache<TKey, TValue> Create<TKey, TValue>(int capacity, TimeSpan entityExpiresIn, Func<TKey[], KeyValuePair<TKey, TValue>[]> resolverAction, int updateTakeCount = 1000)
        {
            var resolver = new ActionValueResolver<TKey, TValue>(resolverAction);
            return Create<TKey, TValue>(capacity, entityExpiresIn, resolver, updateTakeCount);
        }

        public static Cache<TKey, TValue> Create<TKey, TValue>(int capacity, Func<TKey, TValue> resolverAction)
        {
            var resolver = new ActionValueResolver<TKey, TValue>((keys) =>
                keys.Select(k => new KeyValuePair<TKey, TValue>(k, resolverAction(k))).ToArray());

            var cache = new Cache<TKey, TValue>(capacity, resolver);
            return cache;
        }
    }
}
