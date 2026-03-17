class_name MFTutorialTracker extends RefCounted
## Domain-side tutorial state machine.
## Tracks which tutorials have been completed, current step, per-feature phases.
## Step definitions are JSON-driven.

var _player_state: Object
var _event_bus: Object
var _section_name: StringName = &"tutorial"
var _tutorials: Dictionary = {}  # tutorial_id -> {steps: Array, current_step: int}
var _active_tutorial: String = ""


func _init(player_state: Object = null, event_bus: Object = null) -> void:
	_player_state = player_state
	_event_bus = event_bus


## Register a tutorial from JSON definition.
## def: {id: String, steps: [{id, action, target, text, ...}]}
func register(tutorial_def: Dictionary) -> void:
	var id: String = tutorial_def.get("id", "")
	_tutorials[id] = {
		"id": id,
		"steps": tutorial_def.get("steps", []),
		"current_step": 0,
	}


## Start a tutorial if not already completed.
func start(tutorial_id: String) -> bool:
	if is_completed(tutorial_id):
		return false
	if not _tutorials.has(tutorial_id):
		return false
	_active_tutorial = tutorial_id
	_tutorials[tutorial_id].current_step = 0
	_emit("tutorial_started", {"tutorial_id": tutorial_id})
	return true


## Advance to the next step. Returns true if there's a next step.
func advance() -> bool:
	if _active_tutorial == "":
		return false
	var tut: Dictionary = _tutorials[_active_tutorial]
	tut.current_step += 1
	if tut.current_step >= tut.steps.size():
		_complete_tutorial(_active_tutorial)
		return false
	_emit("tutorial_step", {
		"tutorial_id": _active_tutorial,
		"step_index": tut.current_step,
		"step": tut.steps[tut.current_step],
	})
	return true


## Get the current step definition.
func get_current_step() -> Dictionary:
	if _active_tutorial == "" or not _tutorials.has(_active_tutorial):
		return {}
	var tut: Dictionary = _tutorials[_active_tutorial]
	if tut.current_step < tut.steps.size():
		return tut.steps[tut.current_step]
	return {}


## Skip the active tutorial.
func skip() -> void:
	if _active_tutorial != "":
		_complete_tutorial(_active_tutorial)


## Check if a tutorial has been completed.
func is_completed(tutorial_id: String) -> bool:
	_ensure_section()
	if _player_state == null:
		return false
	var section = _player_state.get_section(_section_name)
	return bool(section.get_value(StringName("done_" + tutorial_id), false))


## Get active tutorial id (empty if none).
func get_active_id() -> String:
	return _active_tutorial


func _complete_tutorial(tutorial_id: String) -> void:
	_ensure_section()
	if _player_state != null:
		var section = _player_state.get_section(_section_name)
		section.set_value(StringName("done_" + tutorial_id), true)
	_active_tutorial = ""
	_emit("tutorial_completed", {"tutorial_id": tutorial_id})


func _ensure_section() -> void:
	if _player_state != null and not _player_state.has_section(_section_name):
		_player_state.register_section(_section_name, {})


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)
