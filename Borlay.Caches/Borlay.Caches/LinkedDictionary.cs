using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public class LinkedDictionary<TKey, TNode> where TNode : Node<TKey>
    {
        private Dictionary<TKey, TNode> dictionary = new Dictionary<TKey, TNode>();
        private readonly LinkedNodes<TKey> linked = new LinkedNodes<TKey>();

        public IEnumerable<TKey> Clear(int capacity)
        {
            while (dictionary.Count > capacity && dictionary.Count > 0)
            {
                if (linked.TryRemoveFirst(out var key))
                {
                    dictionary.Remove(key);
                    yield return key;
                }
            }
        }

        public IEnumerable<Node<TKey>> Ascending()
        {
            return linked.Ascending();
        }

        public IEnumerable<Node<TKey>> Descending()
        {
            return linked.Descending();
        }

        public int Count => dictionary.Count;

        public virtual bool TryGetNode(TKey key, out TNode node)
        {
            return dictionary.TryGetValue(key, out node);
        }

        public virtual void Add(TNode newnode)
        {
            linked.AddNode(newnode);
            dictionary[newnode.Key] = newnode;
        }

        public bool Remove(TKey key)
        {
            if (dictionary.TryGetValue(key, out var node))
            {
                linked.RemoveNode(node);
                dictionary.Remove(key);
                return true;
            }
            return false;
        }

        public bool Remove(Node<TKey> node)
        {
            linked.RemoveNode(node);
            return dictionary.Remove(node.Key);
        }
    }
}
