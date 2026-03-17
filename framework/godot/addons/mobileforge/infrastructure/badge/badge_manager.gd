class_name MFBadgeManager extends RefCounted
## Notification badge manager. Tracks badge sources and their counts.
## UI components (MFBadgeDot) bind to this to show/hide red dots.

signal badge_changed(source_id: StringName, count: int)

var _sources: Dictionary = {}  # source_id -> {check_func: Callable}
var _counts: Dictionary = {}  # source_id -> int


## Register a badge source. check_func: Callable() -> int (badge count, 0 = no badge).
func register_source(source_id: StringName, check_func: Callable) -> void:
	_sources[source_id] = {"check_func": check_func}
	_counts[source_id] = 0
	refresh(source_id)


## Unregister a badge source.
func unregister_source(source_id: StringName) -> void:
	_sources.erase(source_id)
	_counts.erase(source_id)


## Refresh a single source's badge count.
func refresh(source_id: StringName) -> void:
	if not _sources.has(source_id):
		return
	var check_func: Callable = _sources[source_id].check_func
	var old_count: int = _counts.get(source_id, 0)
	var new_count: int = 0
	if check_func.is_valid():
		var result = check_func.call()
		if result is bool:
			new_count = 1 if result else 0
		elif result is int:
			new_count = maxi(result, 0)
	_counts[source_id] = new_count
	if old_count != new_count:
		badge_changed.emit(source_id, new_count)


## Refresh all sources.
func refresh_all() -> void:
	for source_id in _sources:
		refresh(source_id)


## Get badge count for a source (0 = no badge).
func get_badge_count(source_id: StringName) -> int:
	return _counts.get(source_id, 0)


## Whether a source has any badges.
func has_badge(source_id: StringName) -> bool:
	return _counts.get(source_id, 0) > 0


## Whether any of the given sources have badges (for parent tabs aggregating children).
func has_any_badge(source_ids: Array) -> bool:
	for source_id in source_ids:
		if has_badge(source_id):
			return true
	return false


## Get total badge count across given sources.
func get_total_count(source_ids: Array) -> int:
	var total := 0
	for source_id in source_ids:
		total += get_badge_count(source_id)
	return total
