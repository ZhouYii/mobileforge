extends Node
## Singleton autoload. Central player state container with sections.
## Emits events via EventBus on any state change.
##
## API:
##   register_section(name: StringName, initial_data: Dictionary = {}) -> MFStateSection
##   get_section(name: StringName) -> MFStateSection
##   has_section(name: StringName) -> bool
##   set_value(section: StringName, key: StringName, value: Variant) -> void
##   get_value(section: StringName, key: StringName, default: Variant = null) -> Variant
##   to_save_dict() -> Dictionary
##   from_save_dict(data: Dictionary) -> void
##   clear_all() -> void
##
## Requires: EventBus autoload (accesses via get_node("/root/EventBus"))

var _sections: Dictionary = {}
var _event_bus: Node = null


func _ready() -> void:
	_event_bus = get_node_or_null("/root/EventBus")
	if _event_bus == null:
		push_warning("PlayerState: EventBus autoload not found. State change events will not be emitted.")


func register_section(section_name: StringName, initial_data: Dictionary = {}) -> MFStateSection:
	if _sections.has(section_name):
		push_warning("PlayerState: section '%s' already registered, returning existing." % section_name)
		return _sections[section_name] as MFStateSection
	var section := MFStateSection.new(section_name, initial_data)
	_sections[section_name] = section
	return section


func get_section(section_name: StringName) -> MFStateSection:
	if not _sections.has(section_name):
		return null
	return _sections[section_name] as MFStateSection


func has_section(section_name: StringName) -> bool:
	return _sections.has(section_name)


func set_value(section_name: StringName, key: StringName, value: Variant) -> void:
	var section := get_section(section_name)
	if section == null:
		push_error("PlayerState: section '%s' not registered." % section_name)
		return
	var old_value = section.set_value(key, value)
	if _event_bus != null:
		var payload := {
			"section": section_name,
			"key": key,
			"old_value": old_value,
			"new_value": value,
		}
		_event_bus.emit_event(EventNames.STATE_CHANGED, payload)


func get_value(section_name: StringName, key: StringName, default: Variant = null) -> Variant:
	var section := get_section(section_name)
	if section == null:
		return default
	return section.get_value(key, default)


func to_save_dict() -> Dictionary:
	var result: Dictionary = {}
	for section_name: StringName in _sections:
		var section: MFStateSection = _sections[section_name]
		result[section_name] = section.to_dict()
	return result


func from_save_dict(data: Dictionary) -> void:
	for section_name: String in data:
		var sname := StringName(section_name)
		var section_data: Dictionary = data[section_name]
		if _sections.has(sname):
			var section: MFStateSection = _sections[sname]
			section.from_dict(section_data)
		else:
			register_section(sname, section_data)
	if _event_bus != null:
		_event_bus.emit_event(EventNames.STATE_LOADED, {})


func clear_all() -> void:
	for section_name: StringName in _sections:
		var section: MFStateSection = _sections[section_name]
		section.clear()
	_sections.clear()
