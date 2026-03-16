class_name MFListItemPool extends RefCounted
## Object pool for list items to enable recycling.

var _factory: Callable
var _pool: Array = []

func _init(factory: Callable) -> void:
	_factory = factory

func acquire() -> Node:
	if not _pool.is_empty():
		return _pool.pop_back()
	return _factory.call()

func release(item: Node) -> void:
	item.visible = false
	if item.get_parent() != null:
		item.get_parent().remove_child(item)
	_pool.append(item)

func pool_size() -> int:
	return _pool.size()

func clear() -> void:
	for item in _pool:
		item.queue_free()
	_pool.clear()
