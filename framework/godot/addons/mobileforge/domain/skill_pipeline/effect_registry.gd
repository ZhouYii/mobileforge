class_name MFEffectRegistry extends RefCounted
## Simple effects via Callable registry.
## For effects that can be expressed as (params, context, result) -> void in one function.
## More complex effects should use SkillOutcome subclasses.

var _effects: Dictionary = {}  # effect_name -> Callable(params, context, result)


func register(effect_name: String, callback: Callable) -> void:
	_effects[effect_name] = callback


func execute(effect_name: String, params: Dictionary, context: RefCounted, result: RefCounted) -> bool:
	if not _effects.has(effect_name):
		return false
	_effects[effect_name].call(params, context, result)
	return true


func has_effect(effect_name: String) -> bool:
	return _effects.has(effect_name)


func get_registered_names() -> Array[String]:
	var names: Array[String] = []
	for key in _effects.keys():
		names.append(key)
	return names
