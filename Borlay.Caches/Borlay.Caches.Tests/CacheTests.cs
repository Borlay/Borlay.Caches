using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Borlay.Caches.Tests
{
    [TestClass]
    public class CacheTests
    {
        [TestMethod]
        public void LimitedCacheTest()
        {
            var cache = new LimitedCache<string, string>(10);

            cache.Set($"k-0", $"v-0");
            cache.Set($"k-1", $"v-1");

            for (int i = 2; i < 10; i++)
            {
                cache.Set($"k-{i}", $"v-{i}");
            }

            Assert.AreEqual(10, cache.Count);

            cache.Set("k-10", "v-10");

            var exist = cache.TryGet("k-1", out var value);
            Assert.IsTrue(exist, $"k-1 not exist");

            cache.Set("k-11", "v-11");

            Assert.AreEqual(10, cache.Count);

            exist = cache.TryGet("k-0", out value);
            Assert.IsFalse(exist, $"k-0 exist");

            exist = cache.TryGet("k-1", out value);
            Assert.IsTrue(exist, $"k-1 not exist");

            exist = cache.TryGet("k-2", out value);
            Assert.IsFalse(exist, $"k-2 exist");

            for (int i = 3; i < 12; i++)
            {
                exist = cache.TryGet($"k-{i}", out value);
                Assert.IsTrue(exist, $"k-{i} not exist");

                Assert.AreEqual($"v-{i}", value);
            }
        }

        [TestMethod]
        public void LimitedCacheManyTest()
        {
            var watch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                LimitedCacheTest();
            }

            watch.Stop();

            // 100k 1.3s

        }
    }
}
