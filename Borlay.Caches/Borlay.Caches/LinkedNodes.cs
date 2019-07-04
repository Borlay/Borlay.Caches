using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Caches
{
    public class LinkedNodes<TKey>
    {
        private Node<TKey> first = null;
        private Node<TKey> last = null;

        public IEnumerable<Node<TKey>> Ascending()
        {
            var current = first;
            while(current != null)
            {
                var result = current;
                current = current.Right;
                yield return result;
            }
        }

        public IEnumerable<Node<TKey>> Descending()
        {
            var current = last;
            while (current != null)
            {
                var result = current;
                current = current.Left;
                yield return result;
            }
        }

        public virtual void AddNode(Node<TKey> node)
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

        public virtual void RemoveNode(Node<TKey> node)
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
        public bool TryRemoveFirst(out TKey key)
        {
            key = default(TKey);
            if (first == null) return false;

            var node = first;
            key = node.Key;
            RemoveNode(node);
            return true;
        }

        /// <summary>
        /// Removes oldest used element
        /// </summary>
        public bool TryRemoveLast(out TKey key)
        {
            key = default(TKey);
            if (last == null) return false;

            var node = last;
            key = node.Key;
            RemoveNode(node);
            return true;
        }
    }
}
