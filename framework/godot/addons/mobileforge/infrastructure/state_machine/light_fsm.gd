class_name MFLightFSM extends RefCounted
## Flat state machine with callable-based states and guarded transitions.
## Complement to HSM — no hierarchy, just states + guards.

signal state_entered(state_id: StringName)
signal state_exited(state_id: StringName)

var _states: Dictionary = {}  # id -> {on_enter, on_exit, on_update}
var _transitions: Array = []  # Array of {from, to, guard}
var _current: StringName = &""


## Add a state with optional lifecycle callbacks.
func add_state(id: StringName, on_enter: Callable = Callable(), on_exit: Callable = Callable(), on_update: Callable = Callable()) -> void:
	_states[id] = {"on_enter": on_enter, "on_exit": on_exit, "on_update": on_update}


## Add a guarded transition. guard: Callable() -> bool. Null guard = always allowed.
func add_transition(from: StringName, to: StringName, guard: Callable = Callable()) -> void:
	_transitions.append({"from": from, "to": to, "guard": guard})


## Attempt to transition to a target state. Returns false if guard rejects or state doesn't exist.
func transition(to: StringName) -> bool:
	if not _states.has(to):
		return false
	# Check guard
	if _current != &"":
		for t in _transitions:
			if t.from == _current and t.to == to:
				if t.guard.is_valid() and not t.guard.call():
					return false
				break
	# Exit current
	if _current != &"" and _states.has(_current):
		var exit_fn: Callable = _states[_current].on_exit
		if exit_fn.is_valid():
			exit_fn.call()
		state_exited.emit(_current)
	# Enter new
	_current = to
	var enter_fn: Callable = _states[to].on_enter
	if enter_fn.is_valid():
		enter_fn.call()
	state_entered.emit(to)
	return true


## Tick the current state's on_update(delta).
func update(delta: float) -> void:
	if _current == &"" or not _states.has(_current):
		return
	var update_fn: Callable = _states[_current].on_update
	if update_fn.is_valid():
		update_fn.call(delta)


## Get the current state ID.
func get_current() -> StringName:
	return _current


## Check if a transition to the target is allowed (guard passes).
func can_transition(to: StringName) -> bool:
	if not _states.has(to):
		return false
	for t in _transitions:
		if t.from == _current and t.to == to:
			if t.guard.is_valid():
				return t.guard.call()
			return true
	return true  # No guard defined = allowed


## Reset to no state (calls on_exit on current).
func reset() -> void:
	if _current != &"" and _states.has(_current):
		var exit_fn: Callable = _states[_current].on_exit
		if exit_fn.is_valid():
			exit_fn.call()
		state_exited.emit(_current)
	_current = &""
