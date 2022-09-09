using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Borlay.Caches
{
    public static class Extensions
    {
        public static void Add<TKey, TValue>(this LinkedDictionary<TKey, ValueNode<TKey, TValue>> dictionary, TKey key, TValue value)
        {
            dictionary.Add(new ValueNode<TKey, TValue>(key, value));
        }

        public static void Add<TKey, TValue>(this LinkedDictionary<TKey, ValueNode<TKey, TValue>> dictionary, KeyValuePair<TKey, TValue> keyValue)
        {
            dictionary.Add(new ValueNode<TKey, TValue>(keyValue.Key, keyValue.Value));
        }

        public static void Add<TKey>(this LinkedDictionary<TKey, Node<TKey>> dictionary, TKey key, DateTime dateTime)
        {
            dictionary.Add(new Node<TKey>(key) { UpdateTime = dateTime });
        }

        /*public static bool TryResolveValue<TKey, TValue>(this IValueResolver<TKey, TValue> resolver, TKey key, out TValue value)
        {
            value = default(TValue);
            var keyPairs = resolver.Resolve(key);

            if (keyPairs.Count() == 0)
                return false;

            var resolvedValue = keyPairs.Single(k => k.Key.Equals(key)).Value;
            value = resolvedValue;
            return true;
        }*/

        public static void SetResolver<TKey, TValue>(this ICache<TKey, TValue> cache, Func<TKey, TValue> resolver) where TValue : class
        {
            cache.SetResolver(new ActionValueResolver<TKey, TValue>(resolver));
        }

        public static TValue ResolveValue<TKey, TValue>(this ICache<TKey, TValue> cache, TKey key, Func<TKey, TValue> resolverFunc) where TValue: class
        {
            return cache.ResolveValue(key, new ActionValueResolver<TKey, TValue>(k =>
            {
                return resolverFunc(k);
            }));
        }

        public static IEnumerable<T[]> Batches<T>(this IEnumerable<T> keys, int batchSize)
        {
            var queue = new Queue<T>();
            foreach(var key in keys)
            {
                queue.Enqueue(key);
                if(queue.Count == batchSize)
                {
                    var batch = queue.ToArray();
                    queue.Clear();
                    yield return batch;
                }
            }

            if(queue.Count > 0)
                yield return queue.ToArray();
        }
    }
}
