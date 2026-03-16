class_name MFGridCellPool extends RefCounted
## Object pool for grid cells, same pattern as list item pool.

var _factory: Callable
var _pool: Array = []

func _init(factory: Callable) -> void:
	_factory = factory

func acquire() -> Node:
	if not _pool.is_empty():
		return _pool.pop_back()
	return _factory.call()

func release(cell: Node) -> void:
	cell.visible = false
	if cell.get_parent() != null:
		cell.get_parent().remove_child(cell)
	_pool.append(cell)

func clear() -> void:
	for cell in _pool:
		cell.queue_free()
	_pool.clear()
