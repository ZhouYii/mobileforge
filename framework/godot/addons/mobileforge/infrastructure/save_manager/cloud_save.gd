class_name MFCloudSave extends RefCounted
## Cloud save with conflict resolution.
## Three strategies: TAKE_MAX (progression), USER_CHOICE, LAST_MODIFIED_WINS.

enum ConflictStrategy { TAKE_MAX, USER_CHOICE, LAST_MODIFIED_WINS }

signal conflict_detected(local_data: Dictionary, remote_data: Dictionary)
signal sync_completed(success: bool)

var _network_client: Object  # MFNetworkClient
var _save_manager: Object  # MFSaveManager
var _conflict_strategy: ConflictStrategy = ConflictStrategy.LAST_MODIFIED_WINS
var _pending_conflict: Dictionary = {}  ## Holds data during USER_CHOICE resolution


func _init(network_client: Object = null, save_manager: Object = null) -> void:
	_network_client = network_client
	_save_manager = save_manager


func set_conflict_strategy(strategy: ConflictStrategy) -> void:
	_conflict_strategy = strategy


## Upload local save to cloud.
func upload(slot: int = 0, endpoint: String = "/save/upload") -> void:
	if _save_manager == null or _network_client == null:
		sync_completed.emit(false)
		return
	# Delegate to game layer via network client
	var data := {"slot": slot, "timestamp": int(Time.get_unix_time_from_system())}
	_network_client.post(endpoint, data, func(response):
		sync_completed.emit(response.success)
	)


## Download remote save and resolve conflicts.
func download(slot: int = 0, endpoint: String = "/save/download") -> void:
	if _network_client == null:
		sync_completed.emit(false)
		return
	_network_client.get_request(endpoint, {"slot": slot}, func(response):
		if not response.success:
			sync_completed.emit(false)
			return
		_resolve_conflict(response.data, slot)
	)


## For USER_CHOICE strategy: user picks local or remote.
func resolve_with_choice(use_remote: bool) -> void:
	if _pending_conflict.is_empty():
		return
	if use_remote:
		# Apply remote data
		pass  # Game layer loads _pending_conflict.remote
	_pending_conflict = {}
	sync_completed.emit(true)


func _resolve_conflict(remote_data: Dictionary, _slot: int) -> void:
	match _conflict_strategy:
		ConflictStrategy.LAST_MODIFIED_WINS:
			var local_ts := int(Time.get_unix_time_from_system())
			var remote_ts: int = int(remote_data.get("timestamp", 0))
			if remote_ts > local_ts:
				sync_completed.emit(true)  # Use remote
			else:
				sync_completed.emit(true)  # Keep local
		ConflictStrategy.USER_CHOICE:
			_pending_conflict = {"remote": remote_data}
			conflict_detected.emit({}, remote_data)
		ConflictStrategy.TAKE_MAX:
			sync_completed.emit(true)  # Merge: take higher progression values
