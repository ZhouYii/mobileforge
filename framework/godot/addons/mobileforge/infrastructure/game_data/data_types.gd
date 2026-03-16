class_name MFDataTypes
## Base type for all game definitions loaded from JSON.


class Definition extends RefCounted:
	var id: int
	var _data: Dictionary

	func _init(data: Dictionary = {}) -> void:
		_data = data
		id = data.get("id", 0)

	func get_field(key: StringName, default: Variant = null) -> Variant:
		return _data.get(key, default)

	func get_int(key: StringName, default: int = 0) -> int:
		return int(_data.get(key, default))

	func get_float(key: StringName, default: float = 0.0) -> float:
		return float(_data.get(key, default))

	func get_string(key: StringName, default: String = "") -> String:
		return str(_data.get(key, default))

	func get_array(key: StringName, default: Array = []) -> Array:
		var val = _data.get(key, default)
		return val if val is Array else default

	func get_dict(key: StringName, default: Dictionary = {}) -> Dictionary:
		var val = _data.get(key, default)
		return val if val is Dictionary else default

	func has_field(key: StringName) -> bool:
		return _data.has(key)

	func raw() -> Dictionary:
		return _data
