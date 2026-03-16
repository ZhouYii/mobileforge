class_name MFStateEvents
## Event payload types for PlayerState changes.


class ValueChanged extends RefCounted:
	var section: StringName
	var key: StringName
	var old_value: Variant
	var new_value: Variant

	func _init(p_section: StringName, p_key: StringName, p_old: Variant, p_new: Variant) -> void:
		section = p_section
		key = p_key
		old_value = p_old
		new_value = p_new


class SectionChanged extends RefCounted:
	var section: StringName

	func _init(p_section: StringName) -> void:
		section = p_section
