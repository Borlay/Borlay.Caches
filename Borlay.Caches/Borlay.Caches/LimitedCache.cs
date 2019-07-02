using System;
using System.Collections.Generic;

namespace Borlay.Caches
{
    public class LimitedCache<TKey, TValue> : ICacheResolve<TKey, TValue>
    {
        private Dictionary<TKey, Node<TKey, TValue>> dictionary = new Dictionary<TKey, Node<TKey, TValue>>();
        private Node<TKey, TValue> first = null;
        private Node<TKey, TValue> last = null;

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

        protected virtual void AddNode(Node<TKey, TValue> node)
        {
            if (first == null)
            {
                first = node;
                last = node;
                return;
            }

            last.Right = node;
            node.Left = last;
            last = node;
        }

        protected virtual void RemoveNode(Node<TKey, TValue> node)
        {
            var left = node.Left;
            var right = node.Right;

            node.Left = null;
            node.Right = null;

            if (left != null)
                left.Right = right;

            if (right != null)
                right.Left = left;

            if (node.Equals(last))
            {
                last = left;
            }
            else if (node.Equals(first))
            {
                first = right;
            }

            if (last == null || last == null)
            {
                first = null;
                last = null;
            }
        }

        /// <summary>
        /// Removes oldest used element
        /// </summary>
        protected void RemoveOldest()
        {
            if (first == null) return;

            var node = first;
            dictionary.Remove(node.Key);
            RemoveNode(node);
        }

        public void Clear(int capacity)
        {
            lock(this)
            {
                while (dictionary.Count > capacity && dictionary.Count > 0)
                {
                    RemoveOldest();
                }
            }
        }

        protected virtual void AddNew(TKey key, TValue value)
        {
            var newnode = new Node<TKey, TValue>(key, value);
            AddNode(newnode);
            dictionary[key] = newnode;

            Clear(Capacity);
        }

        public void Set(TKey key, TValue value)
        {
            lock (this)
            {
                if (dictionary.TryGetValue(key, out var node))
                {
                    RemoveNode(node);
                    AddNode(node);
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
                    RemoveNode(node);
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
            //lock (this)
            //{
            //    if (dictionary.TryGetValue(key, out var node))
            //    {
            //        if(DateTime.Now.Subtract(node.UpdateTime) < EntityExpireIn)
            //        {
            //            RemoveNode(node);
            //            AddNode(node);
            //            value = node.Value;
            //            return true;
            //        }
            //        else
            //        {
            //            dictionary.Remove(key);
            //            RemoveNode(node);
            //        }
            //    }

            //    value = default(TValue);
            //    return false;
            //}
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
                        RemoveNode(node);
                        AddNode(node);
                        value = node.Value;
                        return true;
                    }
                    else
                    {
                        if (resolver == null)
                        {
                            dictionary.Remove(key);
                            RemoveNode(node);
                            return false;
                        }
                        var ResolveValuedValue = resolver(key);

                        RemoveNode(node);
                        AddNode(node);
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

        //public TValue ResolveValue(TKey key, Func<TKey, TValue> ResolveValuer)
        //{
        //    lock (this)
        //    {
        //        if (dictionary.TryGetValue(key, out var node))
        //        {
        //            if (DateTime.Now.Subtract(node.UpdateTime) < EntityExpireIn)
        //            {
        //                RemoveNode(node);
        //                AddNode(node);
        //                return node.Value;
        //            }
        //            else
        //            {
        //                var ResolveValuedValue = ResolveValuer(key);

        //                RemoveNode(node);
        //                AddNode(node);
        //                node.Value = ResolveValuedValue;
        //                node.UpdateTime = DateTime.Now;
        //                return ResolveValuedValue;
        //            }
        //        }
        //        else
        //        {
        //            var ResolveValuedValue = ResolveValuer(key);
        //            AddNew(key, ResolveValuedValue);
        //            return ResolveValuedValue;
        //        }
        //    }
        //}

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
