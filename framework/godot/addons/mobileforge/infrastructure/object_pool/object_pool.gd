class_name MFObjectPool extends RefCounted
## Generic object pool. Replaces ad-hoc ListItemPool/GridCellPool patterns.
## Supports factory, acquire/release callbacks, prewarm, and max capacity.

var _factory: Callable
var _on_acquire: Callable
var _on_release: Callable
var _pool: Array = []
var _max_capacity: int  ## 0 = unlimited


func _init(factory: Callable, on_acquire: Callable = Callable(), on_release: Callable = Callable(), max_capacity: int = 0) -> void:
	_factory = factory
	_on_acquire = on_acquire
	_on_release = on_release
	_max_capacity = max_capacity


func acquire() -> Variant:
	var item: Variant
	if not _pool.is_empty():
		item = _pool.pop_back()
	else:
		item = _factory.call()
	if _on_acquire.is_valid():
		_on_acquire.call(item)
	return item


func release(item: Variant) -> void:
	if item == null:
		return
	if _max_capacity > 0 and _pool.size() >= _max_capacity:
		return  # Discard — pool is full
	if _on_release.is_valid():
		_on_release.call(item)
	_pool.append(item)


func prewarm(count: int) -> void:
	for i in range(count):
		if _max_capacity > 0 and _pool.size() >= _max_capacity:
			break
		var item = _factory.call()
		if _on_release.is_valid():
			_on_release.call(item)
		_pool.append(item)


func pool_size() -> int:
	return _pool.size()


func clear() -> void:
	_pool.clear()
