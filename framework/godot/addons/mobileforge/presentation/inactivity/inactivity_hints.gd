extends Node
class_name MFInactivityHints
## Show hints when player is idle, hide on any input.

signal hint_shown(hint_text: String)
signal hint_hidden

@export var idle_threshold: float = 10.0  ## Seconds before showing hint
@export var hint_interval: float = 8.0  ## Seconds between hint changes

var _idle_timer: float = 0.0
var _hint_timer: float = 0.0
var _hints: Array[String] = []
var _is_showing: bool = false
var _current_hint_index: int = 0


func set_hints(hints: Array[String]) -> void:
	_hints = hints


func _input(_event: InputEvent) -> void:
	if _is_showing:
		_hide_hint()
	_idle_timer = 0.0


func _process(delta: float) -> void:
	_idle_timer += delta
	if not _is_showing and _idle_timer >= idle_threshold and not _hints.is_empty():
		_show_hint()
	elif _is_showing:
		_hint_timer += delta
		if _hint_timer >= hint_interval:
			_hint_timer = 0.0
			_show_next_hint()


func _show_hint() -> void:
	_is_showing = true
	_hint_timer = 0.0
	_current_hint_index = 0
	hint_shown.emit(_hints[0])


func _show_next_hint() -> void:
	_current_hint_index = (_current_hint_index + 1) % _hints.size()
	hint_shown.emit(_hints[_current_hint_index])


func _hide_hint() -> void:
	_is_showing = false
	_idle_timer = 0.0
	hint_hidden.emit()
