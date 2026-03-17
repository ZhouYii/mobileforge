## Segmented Save System
## Splits game state into named segments with different save frequencies.
## Essential data (purchases, core progress) saves after every transaction.
## Non-essential data (camera, UI state) saves less often.
## Inspired by Merge Dragons!' GGSaveLoad segmented architecture.
##
## Usage (as Autoload):
##   SegmentedSave.set_segment_value("player", "xp", 1500)
##   SegmentedSave.save_essentials()  # After purchase
##   SegmentedSave.save_all()         # Periodic full save
class_name SegmentedSave
extends Node

const SAVE_DIR: String = "user://saves/"

## Segment names that are always saved (purchases, core progress, settings).
@export var essential_segments: Array[String] = ["player", "purchases", "settings"]

## All segment names.
@export var all_segments: Array[String] = [
	"player", "purchases", "settings", "tutorial",
	"board", "inventory", "metrics", "camera"
]

## Encryption password (empty = no encryption).
@export var encryption_password: String = ""

## In-memory data per segment.
var _segments: Dictionary = {}  # {String: Dictionary}
var _dirty: Dictionary = {}     # {String: bool}

signal segment_saved(segment_name: String)
signal all_saved


func _ready() -> void:
	DirAccess.make_dir_recursive_absolute(SAVE_DIR)
	load_all()


## Set a value in a specific segment.
func set_segment_value(segment: String, key: String, value: Variant) -> void:
	if segment not in _segments:
		_segments[segment] = {}
	_segments[segment][key] = value
	_dirty[segment] = true


## Get a value from a specific segment.
func get_segment_value(segment: String, key: String, default_value: Variant = null) -> Variant:
	if segment not in _segments:
		return default_value
	return _segments[segment].get(key, default_value)


## Save only essential segments (call after purchases, level completions).
func save_essentials() -> void:
	for segment_name in essential_segments:
		if _dirty.get(segment_name, false):
			_save_segment(segment_name)


## Save all dirty segments.
func save_all() -> void:
	for segment_name in all_segments:
		if _dirty.get(segment_name, false):
			_save_segment(segment_name)
	all_saved.emit()


## Load all segments from disk.
func load_all() -> void:
	for segment_name in all_segments:
		_load_segment(segment_name)


## Get all data across all segments (for cloud sync).
func get_all_data() -> Dictionary:
	var result: Dictionary = {}
	for segment_name in _segments:
		result[segment_name] = _segments[segment_name].duplicate(true)
	return result


## Replace all data from cloud sync.
func load_from_data(data: Dictionary) -> void:
	for segment_name in data:
		_segments[segment_name] = data[segment_name]
		_dirty[segment_name] = true
	save_all()


func _save_segment(segment_name: String) -> void:
	if segment_name not in _segments:
		return
	var path := SAVE_DIR + segment_name + ".json"
	var json_string := JSON.stringify(_segments[segment_name], "\t")

	var file: FileAccess
	if encryption_password != "":
		file = FileAccess.open_encrypted_with_pass(path, FileAccess.WRITE, encryption_password)
	else:
		file = FileAccess.open(path, FileAccess.WRITE)

	if file:
		file.store_string(json_string)
		file.close()
		_dirty[segment_name] = false
		segment_saved.emit(segment_name)


func _load_segment(segment_name: String) -> void:
	var path := SAVE_DIR + segment_name + ".json"
	if not FileAccess.file_exists(path):
		_segments[segment_name] = {}
		return

	var file: FileAccess
	if encryption_password != "":
		file = FileAccess.open_encrypted_with_pass(path, FileAccess.READ, encryption_password)
	else:
		file = FileAccess.open(path, FileAccess.READ)

	if file:
		var parsed = JSON.parse_string(file.get_as_text())
		file.close()
		_segments[segment_name] = parsed if parsed is Dictionary else {}
	else:
		_segments[segment_name] = {}


func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_PAUSED:
		save_essentials()
