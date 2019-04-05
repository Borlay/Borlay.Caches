using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;

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

            var exist = cache.TryGetValue("k-1", out var value);
            Assert.IsTrue(exist, $"k-1 not exist");

            cache.Set("k-11", "v-11");

            Assert.AreEqual(10, cache.Count);

            exist = cache.TryGetValue("k-0", out value);
            Assert.IsFalse(exist, $"k-0 exist");

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsTrue(exist, $"k-1 not exist");

            exist = cache.TryGetValue("k-2", out value);
            Assert.IsFalse(exist, $"k-2 exist");

            for (int i = 3; i < 12; i++)
            {
                exist = cache.TryGetValue($"k-{i}", out value);
                Assert.IsTrue(exist, $"k-{i} not exist");

                Assert.AreEqual($"v-{i}", value);
            }
        }

        [TestMethod]
        public void ExpireEntityTest()
        {
            var cache = new LimitedCache<string, string>(10, TimeSpan.FromMilliseconds(500));

            cache.Set($"k-0", $"v-0");
            cache.Set($"k-1", $"v-1");

            Assert.AreEqual(2, cache.Count);

            var exist = cache.TryGetValue("k-0", out var value);
            Assert.IsTrue(exist, $"k-0 not exist");

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsTrue(exist, $"k-1 not exist");

            Thread.Sleep(1000);

            exist = cache.TryGetValue("k-0", out value);
            Assert.IsFalse(exist, $"k-0 exist");

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsFalse(exist, $"k-1 exist");
        }

        [TestMethod]
        public void ExpireEntityExistTest()
        {
            var cache = new LimitedCache<string, string>(10, TimeSpan.FromMilliseconds(2000));

            cache.Set($"k-0", $"v-0");
            cache.Set($"k-1", $"v-1");

            Assert.AreEqual(2, cache.Count);

            var exist = cache.TryGetValue("k-0", out var value);
            Assert.IsTrue(exist, $"k-0 not exist");

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsTrue(exist, $"k-1 not exist");

            Thread.Sleep(1100);

            exist = cache.TryGetValue("k-0", out value);
            Assert.IsTrue(exist, $"k-0 exist");

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsTrue(exist, $"k-1 exist");

            cache.Set($"k-1", $"v-1");

            Thread.Sleep(1100);

            exist = cache.TryGetValue("k-0", out value);
            Assert.IsFalse(exist, $"k-0 exist");

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsTrue(exist, $"k-1 exist");

            Thread.Sleep(1100);

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsFalse(exist, $"k-1 exist");

            value = cache.ResolveValue("k-0", (k) => "vr-0");
            Assert.AreEqual("vr-0", value);

            value = cache.ResolveValue("k-1", (k) => "vr-1");
            Assert.AreEqual("vr-1", value);

            exist = cache.TryGetValue("k-1", out value);
            Assert.IsTrue(exist, $"k-1 exist");
            Assert.AreEqual("vr-1", value);
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
