class_name MFSkillOutcome extends RefCounted
## Abstract base class for skill outcomes.
## Game code extends this for complex outcomes (e.g., AreaDamage, ChangeGemElement).

var _params: Dictionary
var turns_left: int = 0  # 0 = instant, >0 = persists for N turns


func _init(params: Dictionary = {}) -> void:
	_params = params
	turns_left = int(params.get("duration", 0))


## Override: execute this outcome
func activate(context: RefCounted, result: RefCounted) -> void:  # SkillContext, SkillResult
	pass


## Override: called when this outcome expires or is manually deactivated
func deactivate(context: RefCounted) -> void:
	pass


## Override: called at start of each turn while active
func on_turn_start(context: RefCounted) -> void:
	pass


## Override: called at end of each turn while active
func on_turn_end(context: RefCounted) -> void:
	pass


func get_param(key: String, default_val: Variant = null) -> Variant:
	return _params.get(key, default_val)


func is_persistent() -> bool:
	return turns_left != 0


class OutcomeRegistry extends RefCounted:
	var _factories: Dictionary = {}

	func register(type_name: String, factory: Callable) -> void:
		_factories[type_name] = factory

	func create(type_name: String, params: Dictionary) -> MFSkillOutcome:
		if not _factories.has(type_name):
			push_warning("SkillOutcome type '%s' not registered" % type_name)
			return null
		return _factories[type_name].call(params)

	func has_type(type_name: String) -> bool:
		return _factories.has(type_name)
