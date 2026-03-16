extends Node
## Navigation manager. Maintains a screen stack.
## API: navigate (replace stack), push (add to stack), pop (go back), replace (swap top)
## Emits events via EventBus when screen changes.

var _registry: MFScreenRegistry
var _stack: Array = []  # Array of {id: StringName, node: Node, params: Dictionary}
var _container: Node  # Parent node where screens are added
var _event_bus: Object

func _init() -> void:
	_registry = MFScreenRegistry.new()

func setup(container: Node, event_bus: Object = null) -> void:
	_container = container
	_event_bus = event_bus

## Register a screen factory
func register(screen_id: StringName, factory: Callable) -> void:
	_registry.register(screen_id, factory)

## Navigate to a screen, clearing the entire stack
func navigate(screen_id: StringName, params: Dictionary = {}) -> Node:
	_clear_stack()
	return _push_internal(screen_id, params)

## Push a screen onto the stack (previous screen stays but is hidden)
func push(screen_id: StringName, params: Dictionary = {}) -> Node:
	if not _stack.is_empty():
		var current = _stack[-1]
		current.node.visible = false
		if current.node.has_method("on_pause"):
			current.node.on_pause()
	return _push_internal(screen_id, params)

## Pop the top screen and return to the previous one
func pop() -> void:
	if _stack.size() <= 1:
		return  # Don't pop the last screen
	var top = _stack.pop_back()
	_remove_screen(top)
	if not _stack.is_empty():
		var current = _stack[-1]
		current.node.visible = true
		if current.node.has_method("on_resume"):
			current.node.on_resume()
		_emit_screen_changed(current.id)

## Replace the top screen without affecting the rest of the stack
func replace(screen_id: StringName, params: Dictionary = {}) -> Node:
	if not _stack.is_empty():
		var top = _stack.pop_back()
		_remove_screen(top)
	return _push_internal(screen_id, params)

## Get current screen ID
func current_screen_id() -> StringName:
	if _stack.is_empty():
		return &""
	return _stack[-1].id

## Get stack depth
func stack_depth() -> int:
	return _stack.size()

func _push_internal(screen_id: StringName, params: Dictionary) -> Node:
	var node := _registry.create(screen_id, params)
	if node == null:
		return null
	_stack.append({"id": screen_id, "node": node, "params": params})
	if _container != null:
		_container.add_child(node)
	if node.has_method("on_enter"):
		node.on_enter(params)
	_emit_screen_changed(screen_id)
	return node

func _remove_screen(entry: Dictionary) -> void:
	var node: Node = entry.node
	if node.has_method("on_exit"):
		node.on_exit()
	if node.get_parent() != null:
		node.get_parent().remove_child(node)
	node.queue_free()

func _clear_stack() -> void:
	while not _stack.is_empty():
		var top = _stack.pop_back()
		_remove_screen(top)

func _emit_screen_changed(screen_id: StringName) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(EventNames.SCREEN_CHANGED, {"screen_id": screen_id})
