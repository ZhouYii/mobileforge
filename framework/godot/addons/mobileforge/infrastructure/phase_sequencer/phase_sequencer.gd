class_name MFPhaseSequencer extends RefCounted
## Linear ordered phase pipeline with enter/exit/update and auto-advance modes.
## Three advance modes: auto_advance, duration timer, barrier. Or manual advance().

signal phase_entered(phase_id: StringName)
signal phase_exited(phase_id: StringName)
signal sequence_completed

var _phases: Array = []  # Array of PhaseDef dictionaries
var _current_index: int = -1
var _elapsed: float = 0.0
var _running: bool = false
var _barrier_connected: bool = false


## Add a phase definition (use MFPhaseTypes.create_phase()).
func add_phase(phase_def: Dictionary) -> void:
	_phases.append(phase_def)


## Start the sequence from the first phase.
func start() -> void:
	if _phases.is_empty():
		sequence_completed.emit()
		return
	_running = true
	_current_index = -1
	_enter_next()


## Manually advance to the next phase.
func advance() -> void:
	if not _running:
		return
	_exit_current()
	_enter_next()


## Call every frame to tick duration-based and update callbacks.
func update(delta: float) -> void:
	if not _running or _current_index < 0 or _current_index >= _phases.size():
		return
	var phase: Dictionary = _phases[_current_index]
	# Call on_update
	var on_update: Callable = phase.get("on_update", Callable())
	if on_update.is_valid():
		on_update.call(delta)
	# Duration-based advance
	var duration: float = phase.get("duration", 0.0)
	if duration > 0.0:
		_elapsed += delta
		if _elapsed >= duration:
			advance()


## Get the current phase ID, or empty if not running.
func get_current_phase() -> StringName:
	if _current_index < 0 or _current_index >= _phases.size():
		return &""
	return _phases[_current_index].get("id", &"")


## Get the current phase index (0-based), or -1 if not running.
func get_current_index() -> int:
	return _current_index


## Whether the sequence has completed all phases.
func is_complete() -> bool:
	return not _running and _current_index >= _phases.size()


## Whether the sequencer is currently running.
func is_running() -> bool:
	return _running


## Reset to initial state. Exits current phase if running.
func reset() -> void:
	if _running:
		_exit_current()
	_current_index = -1
	_elapsed = 0.0
	_running = false


func _enter_next() -> void:
	_current_index += 1
	if _current_index >= _phases.size():
		_running = false
		sequence_completed.emit()
		return
	_elapsed = 0.0
	var phase: Dictionary = _phases[_current_index]
	var phase_id: StringName = phase.get("id", &"")
	phase_entered.emit(phase_id)
	# Call on_enter
	var on_enter: Callable = phase.get("on_enter", Callable())
	if on_enter.is_valid():
		on_enter.call()
	# Auto-advance mode
	if phase.get("auto_advance", false):
		advance()
		return
	# Barrier mode
	var barrier: MFBarrier = phase.get("barrier", null)
	if barrier != null and not barrier.is_resolved():
		_barrier_connected = true
		barrier.all_resolved.connect(_on_barrier_resolved, CONNECT_ONE_SHOT)


func _exit_current() -> void:
	if _current_index < 0 or _current_index >= _phases.size():
		return
	var phase: Dictionary = _phases[_current_index]
	var phase_id: StringName = phase.get("id", &"")
	# Disconnect barrier if connected
	if _barrier_connected:
		var barrier: MFBarrier = phase.get("barrier", null)
		if barrier != null and barrier.all_resolved.is_connected(_on_barrier_resolved):
			barrier.all_resolved.disconnect(_on_barrier_resolved)
		_barrier_connected = false
	# Call on_exit
	var on_exit: Callable = phase.get("on_exit", Callable())
	if on_exit.is_valid():
		on_exit.call()
	phase_exited.emit(phase_id)


func _on_barrier_resolved() -> void:
	_barrier_connected = false
	advance()
