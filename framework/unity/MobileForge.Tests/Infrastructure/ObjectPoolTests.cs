using System;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class ObjectPoolInfraTests
    {
        private ObjectPool<object> _pool;
        private int _acquireCount;
        private int _releaseCount;

        [SetUp]
        public void SetUp()
        {
            _acquireCount = 0;
            _releaseCount = 0;
            _pool = new ObjectPool<object>(
                () => new object(),
                onAcquire: _ => _acquireCount++,
                onRelease: _ => _releaseCount++,
                maxCapacity: 3
            );
        }

        [Test]
        public void Acquire_ReturnsNewItem()
        {
            var item = _pool.Acquire();
            Assert.IsNotNull(item);
            Assert.AreEqual(1, _acquireCount);
        }

        [Test]
        public void Release_RespectsMaxCapacity()
        {
            var a = _pool.Acquire();
            var b = _pool.Acquire();
            var c = _pool.Acquire();
            var d = _pool.Acquire();

            _pool.Release(a);
            _pool.Release(b);
            _pool.Release(c);
            _pool.Release(d); // Should be discarded (max=3)
            Assert.AreEqual(3, _pool.PoolSize);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            _pool.Prewarm(3);
            Assert.AreEqual(3, _pool.PoolSize);
            _pool.Clear();
            Assert.AreEqual(0, _pool.PoolSize);
        }
    }
}
