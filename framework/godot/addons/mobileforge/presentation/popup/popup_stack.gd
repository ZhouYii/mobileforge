extends Node
## Manages a stack of popups with priority queue, dimming, and z-ordering.
## Popups are queued -- only one is visible at a time. Higher priority shows first.

var _queue: Array = []  # Array of {id, factory, params, priority, on_dismiss, popup_node, dimmer}
var _container: Node
var _event_bus: Object
var _back_handler: MFBackHandler
var _active_popup: Dictionary = {}  # The currently displayed popup entry

func setup(container: Node, event_bus: Object = null, back_handler: MFBackHandler = null) -> void:
	_container = container
	_event_bus = event_bus
	_back_handler = back_handler
	if _back_handler != null:
		_back_handler.push(&"popup_stack", func() -> bool:
			if _active_popup.is_empty():
				return false
			dismiss()
			return true
		)

## Show a popup. If another popup is active, this one is queued.
## priority: higher number = higher priority (shows first)
func show(popup_id: StringName, factory: Callable, params: Dictionary = {}, priority: int = 0, on_dismiss: Callable = Callable()) -> void:
	var entry := {
		"id": popup_id,
		"factory": factory,
		"params": params,
		"priority": priority,
		"on_dismiss": on_dismiss,
		"popup_node": null,
		"dimmer": null,
	}
	_queue.append(entry)
	# Sort by priority descending
	_queue.sort_custom(func(a, b): return a.priority > b.priority)
	_try_show_next()

## Dismiss the current popup with a result
func dismiss(result: Variant = null) -> void:
	if _active_popup.is_empty():
		return
	_cleanup_active(result)
	_try_show_next()

## Dismiss all popups (clear queue)
func dismiss_all() -> void:
	if not _active_popup.is_empty():
		_cleanup_active(null)
	_queue.clear()

## Get count of queued + active popups
func popup_count() -> int:
	var count := _queue.size()
	if not _active_popup.is_empty():
		count += 1
	return count

## Check if any popup is currently showing
func is_showing() -> bool:
	return not _active_popup.is_empty()

func _try_show_next() -> void:
	if not _active_popup.is_empty() or _queue.is_empty():
		return
	var entry = _queue.pop_front()
	_show_popup(entry)

func _show_popup(entry: Dictionary) -> void:
	# Create dimmer
	var dimmer = MFPopupDimmer.new()
	if _container != null:
		_container.add_child(dimmer)

	# Create popup
	var popup_node: Node = entry.factory.call(entry.params)
	if popup_node == null:
		dimmer.queue_free()
		_try_show_next()
		return

	entry.popup_node = popup_node
	entry.dimmer = dimmer
	_active_popup = entry

	if _container != null:
		_container.add_child(popup_node)

	if popup_node is MFBasePopup:
		popup_node._popup_stack = self
		popup_node.popup_id = entry.id
		popup_node.dismissed.connect(_on_popup_dismissed)
		popup_node.on_show(entry.params)
	# Animated entrance
	if popup_node is Control:
		MFUIAnim.pop_in(popup_node, 0.25)

	_emit(EventNames.POPUP_SHOWN, {"popup_id": entry.id})

func _on_popup_dismissed(result: Variant) -> void:
	dismiss(result)

func _cleanup_active(result: Variant) -> void:
	var entry := _active_popup
	_active_popup = {}

	if entry.popup_node is MFBasePopup:
		entry.popup_node.dismissed.disconnect(_on_popup_dismissed)

	if entry.on_dismiss.is_valid():
		entry.on_dismiss.call(result)

	if entry.popup_node != null and is_instance_valid(entry.popup_node):
		if entry.popup_node is Control:
			MFUIAnim.pop_out(entry.popup_node, 0.2, true)
		else:
			if entry.popup_node.get_parent() != null:
				entry.popup_node.get_parent().remove_child(entry.popup_node)
			entry.popup_node.queue_free()

	if entry.dimmer != null and is_instance_valid(entry.dimmer):
		if entry.dimmer is Control:
			MFUIAnim.fade_out(entry.dimmer, 0.2, true)
		else:
			if entry.dimmer.get_parent() != null:
				entry.dimmer.get_parent().remove_child(entry.dimmer)
			entry.dimmer.queue_free()

	_emit(EventNames.POPUP_DISMISSED, {"popup_id": entry.id, "result": result})

## Show a popup and await its dismissal result.
## Usage: var result = await popup_stack.show_await("confirm", factory, params)
func show_await(popup_id: StringName, factory: Callable, params: Dictionary = {}, priority: int = 0) -> Variant:
	var result_holder := [null]
	var resolved := [false]
	show(popup_id, factory, params, priority, func(result):
		result_holder[0] = result
		resolved[0] = true
	)
	# Wait until the popup is dismissed
	while not resolved[0]:
		await get_tree().process_frame
	return result_holder[0]


func _emit(event: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event, payload)
