class_name MFQuestTracker extends RefCounted
## Tracks quest progress via event matching. Integrates with EventBus for input
## and RewardPipeline for reward granting.

signal quest_activated(quest_id: StringName)
signal quest_completed(quest_id: StringName)
signal quest_progress(quest_id: StringName, objective_index: int, current: int, target: int)
signal quest_claimed(quest_id: StringName)

var _defs: Dictionary = {}  # id -> quest def
var _active: Dictionary = {}  # id -> {objective_progress: Array[int], status: StringName}
var _completed_unclaimed: Dictionary = {}  # id -> true
var _claimed: Dictionary = {}  # id -> true (for non-repeatable)
var _emit_func: Callable  # Optional EventBus emit callback


## Set an optional event emit function for broadcasting quest events.
func set_emit_func(emit_func: Callable) -> void:
	_emit_func = emit_func


## Load quest definitions. Call once at startup.
func load_quest_defs(defs: Array) -> void:
	for def in defs:
		_defs[def.id] = def


## Activate a quest by ID. Returns false if already active, locked by prerequisites, or already claimed (non-repeatable).
func activate_quest(id: StringName) -> bool:
	if not _defs.has(id):
		return false
	if _active.has(id):
		return false
	var def: Dictionary = _defs[id]
	# Check prerequisites
	for prereq in def.get("prerequisites", []):
		if not _claimed.has(prereq):
			return false
	# Check non-repeatable already claimed
	if not def.get("repeatable", false) and _claimed.has(id):
		return false
	var objectives: Array = def.get("objectives", [])
	var progress: Array = []
	for i in range(objectives.size()):
		progress.append(0)
	_active[id] = {"objective_progress": progress, "status": MFQuestTypes.STATUS_ACTIVE}
	quest_activated.emit(id)
	return true


## Process an event. Call this when an EventBus event fires.
## Returns array of quest IDs that were newly completed by this event.
func on_event(event_name: StringName, payload: Dictionary = {}) -> Array:
	var newly_completed: Array = []
	for quest_id in _active.keys():
		var def: Dictionary = _defs[quest_id]
		var state: Dictionary = _active[quest_id]
		if state.status != MFQuestTypes.STATUS_ACTIVE:
			continue
		var objectives: Array = def.get("objectives", [])
		var progress: Array = state.objective_progress
		for i in range(objectives.size()):
			var obj: Dictionary = objectives[i]
			if obj.event_name != event_name:
				continue
			if progress[i] >= obj.target_count:
				continue
			if not _filter_matches(obj.get("filter", {}), payload):
				continue
			progress[i] += 1
			quest_progress.emit(quest_id, i, progress[i], obj.target_count)
		# Check if all objectives complete
		var all_done := true
		for i in range(objectives.size()):
			if progress[i] < objectives[i].target_count:
				all_done = false
				break
		if all_done and state.status == MFQuestTypes.STATUS_ACTIVE:
			state.status = MFQuestTypes.STATUS_COMPLETED
			_completed_unclaimed[quest_id] = true
			newly_completed.append(quest_id)
			quest_completed.emit(quest_id)
	return newly_completed


## Get all currently active quest IDs.
func get_active_quests() -> Array:
	var result: Array = []
	for quest_id in _active:
		if _active[quest_id].status == MFQuestTypes.STATUS_ACTIVE:
			result.append(quest_id)
	return result


## Get all completed-but-unclaimed quest IDs.
func get_completed_unclaimed() -> Array:
	return _completed_unclaimed.keys()


## Get progress for a quest: {objective_progress: Array[int], status: StringName}
func get_quest_state(id: StringName) -> Dictionary:
	if _active.has(id):
		return _active[id].duplicate(true)
	return {}


## Claim a completed quest. Returns the rewards array from the quest def, or empty if not claimable.
func claim_quest(id: StringName) -> Array:
	if not _completed_unclaimed.has(id):
		return []
	_completed_unclaimed.erase(id)
	if _active.has(id):
		_active[id].status = MFQuestTypes.STATUS_CLAIMED
		_active.erase(id)
	_claimed[id] = true
	quest_claimed.emit(id)
	return _defs[id].get("rewards", [])


## Whether there are any completed-but-unclaimed quests (for badge system).
func has_claimable() -> bool:
	return not _completed_unclaimed.is_empty()


## Number of claimable quests.
func claimable_count() -> int:
	return _completed_unclaimed.size()


## Serialize for save system.
func to_save_dict() -> Dictionary:
	return {
		"active": _active.duplicate(true),
		"completed_unclaimed": _completed_unclaimed.keys(),
		"claimed": _claimed.keys(),
	}


## Deserialize from save data.
func from_save_dict(data: Dictionary) -> void:
	_active = data.get("active", {}).duplicate(true)
	_completed_unclaimed.clear()
	for id in data.get("completed_unclaimed", []):
		_completed_unclaimed[id] = true
	_claimed.clear()
	for id in data.get("claimed", []):
		_claimed[id] = true


func _filter_matches(filter: Dictionary, payload: Dictionary) -> bool:
	for key in filter:
		if not payload.has(key):
			return false
		if payload[key] != filter[key]:
			return false
	return true
