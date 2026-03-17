class_name MFHierarchicalStateMachine extends RefCounted
## Hierarchical (nested) finite state machine.
## Each state can contain a child HSM for nested sub-states.

var _states: Dictionary = {}  # state_id -> {on_enter, on_exit, on_update, child_hsm}
var _current_state: StringName = &""
var _parent_hsm: MFHierarchicalStateMachine  ## Null if this is the root


func _init(parent: MFHierarchicalStateMachine = null) -> void:
	_parent_hsm = parent


## Add a state.
func add_state(state_id: StringName, on_enter: Callable = Callable(), on_exit: Callable = Callable(), on_update: Callable = Callable()) -> void:
	_states[state_id] = {
		"on_enter": on_enter,
		"on_exit": on_exit,
		"on_update": on_update,
		"child_hsm": null,
	}


## Add a child HSM to a state (for nested sub-states).
func add_child_hsm(state_id: StringName) -> MFHierarchicalStateMachine:
	if not _states.has(state_id):
		return null
	var child := MFHierarchicalStateMachine.new(self)
	_states[state_id].child_hsm = child
	return child


## Transition to a state.
func transition(state_id: StringName) -> void:
	if not _states.has(state_id):
		push_warning("HSM: Unknown state '%s'" % state_id)
		return
	# Exit current
	if _current_state != &"" and _states.has(_current_state):
		var current = _states[_current_state]
		if current.child_hsm != null:
			current.child_hsm._exit_current()
		if current.on_exit.is_valid():
			current.on_exit.call()
	# Enter new
	_current_state = state_id
	var new_state = _states[state_id]
	if new_state.on_enter.is_valid():
		new_state.on_enter.call()


## Update the current state (and child HSM if any).
func update(delta: float) -> void:
	if _current_state == &"" or not _states.has(_current_state):
		return
	var state = _states[_current_state]
	if state.on_update.is_valid():
		state.on_update.call(delta)
	if state.child_hsm != null:
		state.child_hsm.update(delta)


## Get the current state id.
func get_current_state() -> StringName:
	return _current_state


## Get the full state path (e.g. "combat/attacking/melee").
func get_state_path() -> String:
	var path := str(_current_state)
	if _current_state != &"" and _states.has(_current_state):
		var child_hsm = _states[_current_state].child_hsm
		if child_hsm != null and child_hsm._current_state != &"":
			path += "/" + child_hsm.get_state_path()
	return path


func _exit_current() -> void:
	if _current_state == &"" or not _states.has(_current_state):
		return
	var state = _states[_current_state]
	if state.child_hsm != null:
		state.child_hsm._exit_current()
	if state.on_exit.is_valid():
		state.on_exit.call()
	_current_state = &""
