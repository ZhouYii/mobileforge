class_name MFTimeScale extends RefCounted
## Global and per-category animation speed control.
## Effective speed = global * category. Default 1.0 for both.

signal speed_changed(effective_scale: float)

var _global: float = 1.0
var _categories: Dictionary = {}  # StringName -> float


## Set the global speed multiplier.
func set_global_speed(scale: float) -> void:
	_global = maxf(scale, 0.0)
	speed_changed.emit(_global)


## Get the global speed multiplier.
func get_global_speed() -> float:
	return _global


## Set speed for a specific category (e.g., "battle", "ui").
func set_category_speed(category: StringName, scale: float) -> void:
	_categories[category] = maxf(scale, 0.0)
	speed_changed.emit(get_effective_speed(category))


## Get the effective speed for a category (global * category).
func get_effective_speed(category: StringName = &"") -> float:
	var cat_scale: float = _categories.get(category, 1.0)
	return _global * cat_scale


## Reset all speeds to 1.0.
func reset() -> void:
	_global = 1.0
	_categories.clear()
	speed_changed.emit(1.0)


## Scale a delta value by effective speed for a category.
func scale_delta(delta: float, category: StringName = &"") -> float:
	return delta * get_effective_speed(category)
