using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public class Node<TKey, TValue>
    {
        public Node<TKey, TValue> Left { get; set; }

        public Node<TKey, TValue> Right { get; set; }

        public TKey Key { get; set; }

        public TValue Value { get; set; }

        public Node(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var node = (Node<TKey, TValue>)obj;
            return this.Key.Equals(node.Key);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }
}
