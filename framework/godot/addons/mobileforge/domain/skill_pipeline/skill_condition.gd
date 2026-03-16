class_name MFSkillCondition extends RefCounted
## Abstract base class for skill conditions.
## Game code extends this for complex conditions (e.g., ComboAbove, HpThreshold).
## Subclass must override is_valid().

var _params: Dictionary


func _init(params: Dictionary = {}) -> void:
	_params = params


## Override in subclass: return true if condition is met
func is_valid(context: RefCounted) -> bool:  # SkillContext -> bool
	return false


## Optional: called when the parent skill activates
func on_activate(context: RefCounted) -> void:
	pass


## Optional: called when the parent skill deactivates
func on_deactivate() -> void:
	pass


## Get a parameter value with default
func get_param(key: String, default_val: Variant = null) -> Variant:
	return _params.get(key, default_val)


class ConditionRegistry extends RefCounted:
	var _factories: Dictionary = {}  # type_name -> Callable that returns SkillCondition

	func register(type_name: String, factory: Callable) -> void:
		_factories[type_name] = factory

	func create(type_name: String, params: Dictionary) -> MFSkillCondition:
		if not _factories.has(type_name):
			push_warning("SkillCondition type '%s' not registered" % type_name)
			return null
		return _factories[type_name].call(params)

	func has_type(type_name: String) -> bool:
		return _factories.has(type_name)
