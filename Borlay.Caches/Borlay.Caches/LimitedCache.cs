using System;
using System.Collections.Generic;

namespace Borlay.Caches
{
    public class LimitedCache<TKey, TValue> : ICacheResolve<TKey, TValue>
    {
        private Dictionary<TKey, ValueNode<TKey, TValue>> dictionary = new Dictionary<TKey, ValueNode<TKey, TValue>>();

        private readonly LinkedNodes<TKey> usageLinked = new LinkedNodes<TKey>();

        private readonly LinkedNodes<TKey> expireLinked = new LinkedNodes<TKey>();

        LinkedDictionary<TKey, ValueNode<TKey, TValue>> linkedDictionary = new LinkedDictionary<TKey, ValueNode<TKey, TValue>>();

        private Node<TKey> expirationNodes = null;

        private Func<TKey, TValue> resolver = null;

        public TimeSpan EntityExpireIn { get; set; }

        public int Capacity { get; }

        public int Count => dictionary.Count;

        public LimitedCache(int capacity)
            : this(capacity, TimeSpan.FromHours(1))
        {

        }

        public LimitedCache(int capacity, TimeSpan entityExpireIn)
        {
            this.Capacity = capacity;
            this.EntityExpireIn = entityExpireIn;
        }

        public void SetResolver(Func<TKey, TValue> resolver)
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
                foreach(var node in linkedDictionary.Ascending())
                {
                    if(linkedDictionary.Count > capacity)
                    {
                        linkedDictionary.Remove(node);
                    }
                }

                while (dictionary.Count > capacity && dictionary.Count > 0)
                {
                    if (usageLinked.TryRemoveFirst(out var key))
                        dictionary.Remove(key);
                }
            }
        }

        protected virtual void AddNew(TKey key, TValue value)
        {
            var newnode = new ValueNode<TKey, TValue>(key, value);
            usageLinked.AddNode(newnode);
            dictionary[key] = newnode;

            var expNode = new Node<TKey>(key);
            expireLinked.AddNode(expNode);

            Clear(Capacity);
        }

        public void Set(TKey key, TValue value)
        {
            lock (this)
            {
                if (dictionary.TryGetValue(key, out var node))
                {
                    usageLinked.RemoveNode(node);
                    usageLinked.AddNode(node);
                    node.Value = value;
                    node.UpdateTime = DateTime.Now;
                    return;
                }
                else
                {
                    AddNew(key, value);
                }
            }
        }

        public void Remove(TKey key)
        {
            lock (this)
            {
                if (dictionary.TryGetValue(key, out var node))
                {
                    usageLinked.RemoveNode(node);
                    dictionary.Remove(key);
                    return;
                }
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

        protected bool TryResolveValue(TKey key, Func<TKey, TValue> resolver, out TValue value)
        {
            value = default(TValue);

            lock (this)
            {
                if (dictionary.TryGetValue(key, out var node))
                {
                    if (DateTime.Now.Subtract(node.UpdateTime) < EntityExpireIn)
                    {
                        usageLinked.RemoveNode(node);
                        usageLinked.AddNode(node);
                        value = node.Value;
                        return true;
                    }
                    else
                    {
                        if (resolver == null)
                        {
                            dictionary.Remove(key);
                            usageLinked.RemoveNode(node);
                            return false;
                        }
                        var ResolveValuedValue = resolver(key);

                        usageLinked.RemoveNode(node);
                        usageLinked.AddNode(node);
                        node.Value = ResolveValuedValue;
                        node.UpdateTime = DateTime.Now;
                        value = ResolveValuedValue;
                        return true;
                    }
                }
                else
                {
                    if (resolver == null) return false;
                    var ResolveValuedValue = resolver(key);

                    AddNew(key, ResolveValuedValue);
                    value = ResolveValuedValue;
                    return true;
                }
            }
        }

        /// <summary>
        /// Tries to get value and if not exist tries to ResolveValue with ResolveValuer.
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

        public TValue ResolveValue(TKey key, Func<TKey, TValue> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            if (TryResolveValue(key, resolver, out var value))
                return value;
            else
                throw new Exception("Cannot ResolveValue value");
        }

        public TValue this[TKey key]
        {
            set
            {
                Set(key, value);
            }
            get
            {
                if (TryGetValue(key, out var value))
                    return value;

                throw new KeyNotFoundException($"Key '{key}' not found in the {nameof(LimitedCache<TKey, TValue>)}");
            }
        }
    }

    public interface ICacheResolve<TKey, TValue>
    {
        void Set(TKey key, TValue value);

        TValue ResolveValue(TKey key);
        bool TryResolveValue(TKey key, out TValue value);

        void Remove(TKey key);
    }
}
