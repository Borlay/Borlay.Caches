using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public class Node<TKey> : IValueAge<TKey>
    {
        public Node<TKey> Left { get; set; }

        public Node<TKey> Right { get; set; }

        public TKey Key { get; set; }

        public DateTime UpdateTime { get; set; }

        public Node(TKey key)
        {
            this.Key = key;
            this.UpdateTime = DateTime.Now;
        }
    }

    public class ValueNode<TKey, TValue> : Node<TKey> //  INode<TKey>
    {
        //public ValueNode<TKey, TValue> Left { get; set; }

        //public ValueNode<TKey, TValue> Right { get; set; }

        //public TKey Key { get; set; }
        //public DateTime UpdateTime { get; set; }

        public TValue Value { get; set; }

        public ValueNode(TKey key, TValue value)
            : base(key)
        {
            this.Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var node = (ValueNode<TKey, TValue>)obj;
            return this.Key.Equals(node.Key);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }
}
