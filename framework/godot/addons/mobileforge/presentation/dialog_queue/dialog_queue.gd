extends Node
class_name MFDialogQueue
## Fluent builder that chains popups sequentially through PopupStack.
## Usage:
##   dialog_queue.chain("maintenance", factory1, params1)
##       .chain("daily_login", factory2, params2)
##       .chain("announcements", factory3, params3)
##       .run()

var _popup_stack: Node  # MFPopupStack
var _entries: Array = []  # Array of {id, factory, params, priority}
var _running: bool = false


func _init(popup_stack: Node = null) -> void:
	_popup_stack = popup_stack


func setup(popup_stack: Node) -> void:
	_popup_stack = popup_stack


## Add a popup to the chain. Returns self for fluent chaining.
func chain(popup_id: StringName, factory: Callable, params: Dictionary = {}, priority: int = 0) -> MFDialogQueue:
	_entries.append({"id": popup_id, "factory": factory, "params": params, "priority": priority})
	return self


## Run all chained popups sequentially, each shown after the previous is dismissed.
func run() -> void:
	if _popup_stack == null or _entries.is_empty():
		return
	if _running:
		return
	_running = true
	await _run_sequence()
	_running = false


func _run_sequence() -> void:
	for entry in _entries:
		await _popup_stack.show_await(entry.id, entry.factory, entry.params, entry.priority)
	_entries.clear()


## Clear queued popups without running them.
func clear() -> void:
	_entries.clear()


## Whether the queue is currently running.
func is_running() -> bool:
	return _running
