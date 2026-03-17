class_name MFScenePool extends RefCounted
## Scene-aware object pool for Godot Nodes.
## Handles reparenting, visibility, and queue_free on clear.

var _pool: MFObjectPool
var _factory: Callable


func _init(factory: Callable, max_capacity: int = 0) -> void:
	_factory = factory
	_pool = MFObjectPool.new(
		factory,
		func(node: Node): node.visible = true,
		func(node: Node):
			node.visible = false
			if node.get_parent() != null:
				node.get_parent().remove_child(node),
		max_capacity
	)


func acquire() -> Node:
	return _pool.acquire() as Node


func release(node: Node) -> void:
	_pool.release(node)


func prewarm(count: int) -> void:
	_pool.prewarm(count)


func pool_size() -> int:
	return _pool.pool_size()


func clear() -> void:
	# queue_free all pooled nodes
	for i in range(_pool._pool.size()):
		var node = _pool._pool[i]
		if node is Node and is_instance_valid(node):
			node.queue_free()
	_pool.clear()
