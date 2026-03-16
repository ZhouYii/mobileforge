class_name MFPityTracker extends RefCounted
## Tracks pity counter per pool. Pure state container.

var _counters: Dictionary = {}  # pool_id -> int


func get_pity(pool_id: int) -> int:
	return _counters.get(pool_id, 0)


func increment(pool_id: int) -> void:
	_counters[pool_id] = get_pity(pool_id) + 1


func reset(pool_id: int) -> void:
	_counters[pool_id] = 0


func to_dict() -> Dictionary:
	return _counters.duplicate()


func from_dict(data: Dictionary) -> void:
	_counters = data.duplicate()
