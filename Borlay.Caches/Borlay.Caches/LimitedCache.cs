using System;
using System.Collections.Generic;

namespace Borlay.Caches
{
    public class LimitedCache<TKey, TValue>
    {
        public Dictionary<TKey, Node<TKey, TValue>> dictionary = new Dictionary<TKey, Node<TKey, TValue>>();
        Node<TKey, TValue> first = null;
        Node<TKey, TValue> last = null;


        public int Capacity { get; }

        public int Count => dictionary.Count;

        public LimitedCache(int capacity)
        {
            this.Capacity = capacity;
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
                    return;
                }
                else
                {
                    AddNew(key, value);
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this)
            {
                if (dictionary.TryGetValue(key, out var node))
                {
                    RemoveNode(node);
                    AddNode(node);
                    value = node.Value;
                    return true;
                }

                value = default(TValue);
                return false;
            }
        }

        public TValue ResolveValue(TKey key, Func<TKey, TValue> resolve)
        {
            lock (this)
            {
                if (dictionary.TryGetValue(key, out var node))
                {
                    RemoveNode(node);
                    AddNode(node);
                    return node.Value;
                }
                else
                {
                    var resolvedValue = resolve(key);
                    AddNew(key, resolvedValue);
                    return resolvedValue;
                }
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
                if (TryGetValue(key, out var value))
                    return value;

                throw new KeyNotFoundException($"Key '{key}' not found in the {nameof(LimitedCache<TKey, TValue>)}");
            }
        }
    }
}
