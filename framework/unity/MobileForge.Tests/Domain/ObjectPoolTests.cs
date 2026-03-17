using System;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class ObjectPoolTests
    {
        private ObjectPool<object> _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new ObjectPool<object>(() => new object());
        }

        [Test]
        public void Acquire_ReturnsNewItem()
        {
            var item = _pool.Acquire();
            Assert.IsNotNull(item);
        }

        [Test]
        public void Release_ThenAcquire_ReturnsSameItem()
        {
            var item1 = _pool.Acquire();
            _pool.Release(item1);
            var item2 = _pool.Acquire();
            Assert.AreSame(item1, item2);
        }

        [Test]
        public void Prewarm_FillsPool()
        {
            _pool.Prewarm(5);
            Assert.AreEqual(5, _pool.PoolSize);
        }

        [Test]
        public void Release_RespectsMaxCapacity()
        {
            var pool = new ObjectPool<object>(() => new object(), maxCapacity: 2);
            var a = pool.Acquire();
            var b = pool.Acquire();
            var c = pool.Acquire();
            pool.Release(a);
            pool.Release(b);
            pool.Release(c); // Should be discarded (over max)
            Assert.AreEqual(2, pool.PoolSize);
        }

        [Test]
        public void Clear_EmptiesPool()
        {
            _pool.Prewarm(3);
            _pool.Clear();
            Assert.AreEqual(0, _pool.PoolSize);
        }

        [Test]
        public void Acquire_CallsOnAcquireCallback()
        {
            bool called = false;
            var pool = new ObjectPool<object>(() => new object(), onAcquire: _ => called = true);
            pool.Acquire();
            Assert.IsTrue(called);
        }

        [Test]
        public void Release_CallsOnReleaseCallback()
        {
            bool called = false;
            var pool = new ObjectPool<object>(() => new object(), onRelease: _ => called = true);
            var item = pool.Acquire();
            pool.Release(item);
            Assert.IsTrue(called);
        }
    }
}
