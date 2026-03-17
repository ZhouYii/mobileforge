extends Node
class_name MFBackHandler
## Android back button / Escape key handler.
## Maintains a stack of Callables. Top handler gets called first.
## UIRouter and PopupStack auto-register their handlers.

var _handlers: Array = []  # Array of {id: StringName, handler: Callable}


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		handle_back()
		get_viewport().set_input_as_handled()


func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		handle_back()


## Push a back handler onto the stack. Returns the id for later removal.
func push(id: StringName, handler: Callable) -> void:
	_handlers.append({"id": id, "handler": handler})


## Remove a handler by id.
func remove(id: StringName) -> void:
	for i in range(_handlers.size() - 1, -1, -1):
		if _handlers[i].id == id:
			_handlers.remove_at(i)
			return


## Try to handle back press. Returns true if a handler consumed it.
func handle_back() -> bool:
	for i in range(_handlers.size() - 1, -1, -1):
		var entry = _handlers[i]
		if entry.handler.is_valid():
			var consumed: bool = entry.handler.call()
			if consumed:
				return true
	return false


## Clear all handlers.
func clear() -> void:
	_handlers.clear()
