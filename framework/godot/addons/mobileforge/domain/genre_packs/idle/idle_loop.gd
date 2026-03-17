class_name MFIdleLoop extends MFGameLoop
## Tick-based idle/incremental game loop.
## Handles resource accumulation, offline earnings, and prestige.

var _resources: Dictionary = {}  # resource_id -> float amount
var _generators: Array = []  # Array of {id, resource, rate_per_sec, level, cost_base, cost_growth}
var _is_active: bool = false
var _total_time: float = 0.0
var _prestige_count: int = 0
var _prestige_multiplier: float = 1.0
var _event_bus: Object


func _init(event_bus: Object = null) -> void:
	_event_bus = event_bus


func start(config: Dictionary) -> void:
	_resources = config.get("initial_resources", {}).duplicate()
	_generators = []
	for gen_data in config.get("generators", []):
		_generators.append(gen_data.duplicate())
	_is_active = true
	_total_time = 0.0

	# Apply offline earnings if provided
	var offline_seconds: float = config.get("offline_seconds", 0.0)
	if offline_seconds > 0.0:
		_apply_earnings(offline_seconds)

	_emit("idle_started", {})


func process_input(input: Dictionary) -> MFGameLoopTypes.PhaseResult:
	var action: String = input.get("action", "")
	var result := MFGameLoopTypes.PhaseResult.new(action, true)

	match action:
		"buy_generator":
			var gen_id: String = input.get("generator_id", "")
			result.data = _buy_generator(gen_id)
		"upgrade_generator":
			var gen_id: String = input.get("generator_id", "")
			result.data = _upgrade_generator(gen_id)
		"prestige":
			result.data = _prestige()
		_:
			result.completed = false

	return result


func tick(delta: float) -> MFGameLoopTypes.PhaseResult:
	if not _is_active:
		return MFGameLoopTypes.PhaseResult.new("idle", false)

	_apply_earnings(delta)
	_total_time += delta

	return MFGameLoopTypes.PhaseResult.new("tick", true, {"resources": _resources.duplicate()})


func get_state() -> MFGameLoopTypes.GameLoopState:
	var s := MFGameLoopTypes.GameLoopState.new()
	s.phase = "running" if _is_active else "inactive"
	s.is_active = _is_active
	s.custom = {
		"resources": _resources.duplicate(),
		"generators": _generators.duplicate(),
		"prestige_count": _prestige_count,
		"prestige_multiplier": _prestige_multiplier,
		"total_time": _total_time,
	}
	return s


func is_active() -> bool:
	return _is_active


func _apply_earnings(seconds: float) -> void:
	for gen in _generators:
		var resource: String = gen.get("resource", "")
		var rate: float = float(gen.get("rate_per_sec", 0.0)) * int(gen.get("level", 0))
		if resource != "" and rate > 0.0:
			var earned := rate * seconds * _prestige_multiplier
			_resources[resource] = _resources.get(resource, 0.0) + earned


func _buy_generator(gen_id: String) -> Dictionary:
	for gen in _generators:
		if gen.get("id", "") == gen_id:
			var cost_base: float = float(gen.get("cost_base", 10))
			var cost_growth: float = float(gen.get("cost_growth", 1.15))
			var level: int = int(gen.get("level", 0))
			var cost: float = cost_base * pow(cost_growth, level)
			var currency: String = gen.get("cost_resource", "gold")
			var balance: float = float(_resources.get(currency, 0.0))
			if balance >= cost:
				_resources[currency] = balance - cost
				gen["level"] = level + 1
				return {"success": true, "new_level": level + 1}
			return {"success": false, "error": "not_enough_" + currency}
	return {"success": false, "error": "generator_not_found"}


func _upgrade_generator(gen_id: String) -> Dictionary:
	return _buy_generator(gen_id)  # Same mechanic for now


func _prestige() -> Dictionary:
	_prestige_count += 1
	_prestige_multiplier = 1.0 + _prestige_count * 0.1
	# Reset resources and generator levels
	for key in _resources:
		_resources[key] = 0.0
	for gen in _generators:
		gen["level"] = 0
	_emit("prestige", {"count": _prestige_count, "multiplier": _prestige_multiplier})
	return {"prestige_count": _prestige_count, "multiplier": _prestige_multiplier}


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)
