class_name MFPreferences extends RefCounted
## Lightweight key-value preferences store. Survives save wipes.
## Uses Godot ConfigFile for persistent storage.

signal preference_changed(key: StringName, value: Variant)

const SECTION := "preferences"

var _config := ConfigFile.new()
var _path: String = ""
var _cache: Dictionary = {}


## Initialize with a file path. Loads existing preferences if file exists.
func setup(path: String) -> void:
	_path = path
	load_prefs()


## Set a preference value.
func set_pref(key: StringName, value: Variant) -> void:
	_cache[key] = value
	_config.set_value(SECTION, key, value)
	preference_changed.emit(key, value)


## Get a preference value, or default if not set.
func get_pref(key: StringName, default: Variant = null) -> Variant:
	return _cache.get(key, default)


## Check if a preference key exists.
func has_pref(key: StringName) -> bool:
	return _cache.has(key)


## Delete a preference key.
func delete_pref(key: StringName) -> void:
	_cache.erase(key)
	if _config.has_section_key(SECTION, key):
		_config.erase_section_key(SECTION, key)


## Save preferences to disk.
func save() -> Error:
	if _path.is_empty():
		return ERR_UNCONFIGURED
	return _config.save(_path)


## Load preferences from disk.
func load_prefs() -> Error:
	if _path.is_empty():
		return ERR_UNCONFIGURED
	var err := _config.load(_path)
	if err != OK:
		return err
	_cache.clear()
	if _config.has_section(SECTION):
		for key in _config.get_section_keys(SECTION):
			_cache[key] = _config.get_value(SECTION, key)
	return OK


## Get all preference keys.
func get_all_keys() -> Array:
	return _cache.keys()
