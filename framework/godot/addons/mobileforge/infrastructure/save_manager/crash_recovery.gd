class_name MFCrashRecovery extends RefCounted
## Checkpoint critical data before risky ops (gacha, IAP).
## Check on next boot to detect incomplete transactions and restore.

var _save_dir: String = "user://saves/"
var _checkpoint_file: String = "crash_checkpoint.json"


func _init(save_dir: String = "user://saves/") -> void:
	_save_dir = save_dir


## Create a checkpoint before a risky operation.
func create_checkpoint(operation: String, data: Dictionary) -> bool:
	var checkpoint := {
		"operation": operation,
		"data": data,
		"timestamp": int(Time.get_unix_time_from_system()),
		"completed": false,
	}
	var path := _save_dir + _checkpoint_file
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(checkpoint, "  "))
	file.close()
	return true


## Mark a checkpoint as completed (operation succeeded).
func complete_checkpoint() -> void:
	var path := _save_dir + _checkpoint_file
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(path)


## Check on boot if there's an incomplete checkpoint.
## Returns the checkpoint data if recovery is needed, or empty dict.
func check_on_boot() -> Dictionary:
	var path := _save_dir + _checkpoint_file
	if not FileAccess.file_exists(path):
		return {}
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return {}
	var text := file.get_as_text()
	file.close()
	var parsed = JSON.parse_string(text)
	if parsed == null or not parsed is Dictionary:
		DirAccess.remove_absolute(path)
		return {}
	var checkpoint: Dictionary = parsed
	if bool(checkpoint.get("completed", false)):
		DirAccess.remove_absolute(path)
		return {}
	return checkpoint
