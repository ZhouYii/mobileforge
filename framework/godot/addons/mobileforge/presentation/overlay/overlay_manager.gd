extends Node
## Manages named overlays (loading, fade, custom).

var _overlays: Dictionary = {}  # id -> Node
var _container: Node

func setup(container: Node) -> void:
	_container = container

func show_overlay(overlay_id: StringName, factory: Callable = Callable()) -> Node:
	if _overlays.has(overlay_id):
		_overlays[overlay_id].visible = true
		return _overlays[overlay_id]
	var node: Node
	if factory.is_valid():
		node = factory.call()
	else:
		node = MFLoadingOverlay.new()
	_overlays[overlay_id] = node
	if _container != null:
		_container.add_child(node)
	return node

func hide_overlay(overlay_id: StringName) -> void:
	if _overlays.has(overlay_id):
		var node: Node = _overlays[overlay_id]
		if node.get_parent() != null:
			node.get_parent().remove_child(node)
		node.queue_free()
		_overlays.erase(overlay_id)

func hide_all() -> void:
	for overlay_id in _overlays.keys():
		hide_overlay(overlay_id)

func is_showing(overlay_id: StringName) -> bool:
	return _overlays.has(overlay_id)
