class_name MFStateSection extends RefCounted
## A typed section of player state (e.g. currencies, inventory, settings).
## Holds a Dictionary of key-value pairs.
## Does NOT emit events directly -- PlayerState does that.

var _name: StringName
var _data: Dictionary


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
