class_name MFStateSection extends RefCounted
## A typed section of player state (e.g. currencies, inventory, settings).
## Holds a Dictionary of key-value pairs.
## Does NOT emit events directly -- PlayerState does that.

var _name: StringName
var _data: Dictionary
var _watchers: Dictionary = {}  # key -> Array[Callable(old_value, new_value)]


func _init(section_name: StringName, initial_data: Dictionary = {}) -> void:
	_name = section_name
	_data = initial_data.duplicate(true)


func get_name() -> StringName:
	return _name


func get_value(key: StringName, default: Variant = null) -> Variant:
	return _data.get(key, default)


func set_value(key: StringName, value: Variant) -> Variant:
	var old = _data.get(key, null)
	_data[key] = value
	_notify_watchers(key, old, value)
	return old


func has_key(key: StringName) -> bool:
	return _data.has(key)


func erase(key: StringName) -> bool:
	return _data.erase(key)


func keys() -> Array:
	return _data.keys()


func to_dict() -> Dictionary:
	return _data.duplicate(true)


func from_dict(data: Dictionary) -> void:
	_data = data.duplicate(true)


func clear() -> void:
	_data.clear()


## Watch a specific key for changes. Callback receives (old_value, new_value).
func watch(key: StringName, callback: Callable) -> void:
	if not _watchers.has(key):
		_watchers[key] = []
	_watchers[key].append(callback)


## Remove a watcher for a specific key.
func unwatch(key: StringName, callback: Callable) -> void:
	if not _watchers.has(key):
		return
	var arr: Array = _watchers[key]
	var idx := arr.find(callback)
	if idx >= 0:
		arr.remove_at(idx)
	if arr.is_empty():
		_watchers.erase(key)


func _notify_watchers(key: StringName, old_value: Variant, new_value: Variant) -> void:
	if not _watchers.has(key):
		return
	for callback in _watchers[key]:
		if callback.is_valid():
			callback.call(old_value, new_value)
