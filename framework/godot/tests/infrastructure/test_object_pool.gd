extends MFTestBase
class_name MFTestObjectPool

    func test_acquire_new_returns_null_when_pool_empty() -> void
        var item = _pool.acquire()
        assert.IsNull(item)
    end

    func test_acquire_returns_pooled_items() -> void
        var item1 = _pool.acquire()
        var item2 = _pool.acquire()
        var item3 = _pool.acquire()
        assert.IsNotNull(item1)
        assert.IsNotNull(item2)
        assert.IsNotNull(item3)
        assert.AreNotSame(item1, item2)
    end

    func test_acquire_max_capacity_blocks_creation() -> void
        _pool = new ObjectPool(3, _factory)
        var item = _pool.acquire()
        assert.IsNotNull(item)
        _pool.Release(item)
        Assert.AreEqual(3, _pool.count)
    end

    func test_release_returns_pooled_items() -> void
        var item1 = _pool.acquire()
        var item2 = _pool.acquire()
        var item3 = _pool.acquire()
        var item4 = _pool.acquire()
        assert.IsNull(item4)
        _pool.release(item1)
        _pool.release(item2)
        _pool.release(item3)
        _pool.release(item4)
        Assert.AreEqual(0, _releaseCount)
        Assert.AreEqual(0, _pool.count)
    end

    func test_release_max_capacity_blocks_max_capacity() -> void
        _pool = new ObjectPool(3, _factory)
        var item1 = _pool.acquire()
        _pool.Release(item1)
        var item2 = _pool.acquire()
        var item3 = _pool.acquire()
        _pool.Release(item1)
        _pool.Release(item2)
        _pool.Release(item3)
        Assert.AreEqual(5, _releaseCount)
        _pool.release(item4)
        _pool.clear()
        Assert.AreEqual(0, _pool.count)
    end
}