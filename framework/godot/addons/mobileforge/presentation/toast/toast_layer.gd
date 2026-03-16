extends Node
## Shows stacking toast notifications. Auto-dismiss after duration.

var _container: VBoxContainer
var _parent: Node
var _event_bus: Object

func setup(parent: Node, event_bus: Object = null) -> void:
	_parent = parent
	_event_bus = event_bus
	_container = VBoxContainer.new()
	_container.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_container.offset_top = 40
	_container.offset_bottom = 200
	_container.alignment = BoxContainer.ALIGNMENT_BEGIN
	parent.add_child(_container)

## Show a toast message
func show_toast(text: String, duration: float = 2.0) -> void:
	var toast = MFToastItem.new()
	toast.setup(text, duration)
	_container.add_child(toast)
	# Limit visible toasts
	while _container.get_child_count() > 5:
		var oldest = _container.get_child(0)
		_container.remove_child(oldest)
		oldest.queue_free()
