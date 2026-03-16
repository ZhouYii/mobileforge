extends Node
## Singleton autoload. Simple pub/sub event system.
## Uses StringName keys for performance.
##
## API:
##   subscribe(event: StringName, callback: Callable) -> void
##   unsubscribe(event: StringName, callback: Callable) -> void
##   emit_event(event: StringName, payload: Dictionary = {}) -> void
##   clear_all() -> void
##   subscriber_count(event: StringName) -> int
##
## Internal: Dictionary of StringName -> Array[Callable]
## On emit, iterate and call each. On unsubscribe, erase from array.
## Thread-safe is NOT required (single-threaded game loop).

var _listeners: Dictionary = {}


func subscribe(event: StringName, callback: Callable) -> void:
	if not _listeners.has(event):
		_listeners[event] = [] as Array[Callable]
	var arr: Array = _listeners[event]
	if callback not in arr:
		arr.append(callback)


func unsubscribe(event: StringName, callback: Callable) -> void:
	if not _listeners.has(event):
		return
	var arr: Array = _listeners[event]
	arr.erase(callback)
	if arr.is_empty():
		_listeners.erase(event)


func emit_event(event: StringName, payload: Dictionary = {}) -> void:
	if not _listeners.has(event):
		return
	# Iterate a copy so callbacks can safely unsubscribe during emission.
	var arr_copy: Array = _listeners[event].duplicate()
	for callback: Callable in arr_copy:
		callback.call(payload)


func clear_all() -> void:
	_listeners.clear()


func subscriber_count(event: StringName) -> int:
	if not _listeners.has(event):
		return 0
	return _listeners[event].size()
