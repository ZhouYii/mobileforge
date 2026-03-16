class_name MFScreenRegistry extends RefCounted
## Registry of screen factories. Screens are created lazily via factory Callables.
## Each registered screen has an ID (StringName) and a factory that returns a Node.

var _factories: Dictionary = {}  # StringName -> Callable

func register(screen_id: StringName, factory: Callable) -> void:
	_factories[screen_id] = factory

func create(screen_id: StringName, params: Dictionary = {}) -> Node:
	if not _factories.has(screen_id):
		push_error("Screen '%s' not registered" % screen_id)
		return null
	return _factories[screen_id].call(params)

func has_screen(screen_id: StringName) -> bool:
	return _factories.has(screen_id)

func get_registered_ids() -> Array:
	return _factories.keys()
