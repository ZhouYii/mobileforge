extends Node
class_name MFInputGuard
## Multi-lock input guard. Locked when ANY reason string is active.
## Each caller adds/removes a reason; guard is locked when the set is non-empty.

signal locked_changed(is_locked: bool)

var _locks: Dictionary = {}  # StringName -> true
var _timed_timers: Dictionary = {}  # StringName -> Timer


## Add a lock reason. Emits locked_changed(true) on first lock.
func lock(reason: StringName) -> void:
	var was_empty := _locks.is_empty()
	_locks[reason] = true
	if was_empty:
		locked_changed.emit(true)


## Remove a lock reason. Emits locked_changed(false) when last lock removed.
func unlock(reason: StringName) -> void:
	if not _locks.has(reason):
		return
	_locks.erase(reason)
	# Clean up timed timer if it exists
	if _timed_timers.has(reason):
		var timer: Timer = _timed_timers[reason]
		timer.stop()
		timer.queue_free()
		_timed_timers.erase(reason)
	if _locks.is_empty():
		locked_changed.emit(false)


## Whether any lock is active.
func is_locked() -> bool:
	return not _locks.is_empty()


## Get array of active lock reason strings.
func active_locks() -> Array:
	return _locks.keys()


## Lock with automatic unlock after duration seconds.
func lock_timed(reason: StringName, duration: float) -> void:
	lock(reason)
	# Cancel existing timer for this reason if any
	if _timed_timers.has(reason):
		var old_timer: Timer = _timed_timers[reason]
		old_timer.stop()
		old_timer.queue_free()
	var timer := Timer.new()
	timer.wait_time = duration
	timer.one_shot = true
	timer.timeout.connect(func(): unlock(reason))
	add_child(timer)
	_timed_timers[reason] = timer
	timer.start()
