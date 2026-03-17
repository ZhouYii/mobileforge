class_name MFOptimisticUpdate extends RefCounted
## Snapshot before mutation, sync to server, restore on failure.
## Wraps state changes in an optimistic transaction.

var _snapshots: Dictionary = {}  # transaction_id -> {sections: Dictionary, timestamp: int}
var _player_state: Object
var _next_id: int = 0


func _init(player_state: Object = null) -> void:
	_player_state = player_state


## Begin an optimistic transaction. Returns transaction_id.
func begin(section_names: Array) -> int:
	var tx_id := _next_id
	_next_id += 1
	var sections: Dictionary = {}
	for name in section_names:
		if _player_state != null and _player_state.has_section(name):
			sections[name] = _player_state.get_section(name).to_dict()
	_snapshots[tx_id] = {
		"sections": sections,
		"timestamp": int(Time.get_unix_time_from_system()),
	}
	return tx_id


## Commit the transaction (discard snapshot).
func commit(tx_id: int) -> void:
	_snapshots.erase(tx_id)


## Rollback the transaction (restore snapshot).
func rollback(tx_id: int) -> bool:
	if not _snapshots.has(tx_id):
		return false
	var snapshot: Dictionary = _snapshots[tx_id]
	for name in snapshot.sections:
		if _player_state != null and _player_state.has_section(name):
			_player_state.get_section(name).from_dict(snapshot.sections[name])
	_snapshots.erase(tx_id)
	return true


## Check if a transaction is pending.
func is_pending(tx_id: int) -> bool:
	return _snapshots.has(tx_id)
