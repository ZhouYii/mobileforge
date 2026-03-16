class_name MFCombatResolver extends RefCounted
## 5-hook damage pipeline matching ToS's SkillInstance event system.
## Hooks: PRE_ELEMENT -> POST_ELEMENT -> MAIN -> POST_DEFENSE -> CAN_ZERO
## Skills register callable hooks with priority; resolver fires them in order.


var _element_chart: MFElementChart
var _hooks: Dictionary = {}  ## DamageHook -> Array[{callable: Callable, priority: int, name: String}]


func _init(element_chart: MFElementChart = null) -> void:
	_element_chart = element_chart if element_chart != null else MFElementChart.new()
	for hook_type in MFCombatTypes.DamageHook.values():
		_hooks[hook_type] = []


## Register a hook callable. Lower priority fires first.
func register_hook(hook_type: int, callback: Callable, priority: int = 100, hook_name: String = "") -> void:
	_hooks[hook_type].append({"callable": callback, "priority": priority, "name": hook_name})
	_hooks[hook_type].sort_custom(func(a, b): return a.priority < b.priority)


## Unregister a hook by callable reference.
func unregister_hook(hook_type: int, callback: Callable) -> void:
	var hooks: Array = _hooks[hook_type]
	for i in range(hooks.size() - 1, -1, -1):
		if hooks[i].callable == callback:
			hooks.remove_at(i)
			break


## Clear all hooks of a specific type.
func clear_hooks(hook_type: int) -> void:
	_hooks[hook_type].clear()


## Clear ALL hooks.
func clear_all_hooks() -> void:
	for hook_type in _hooks:
		_hooks[hook_type].clear()


## Resolve player attack damage through the 5-hook pipeline.
func resolve_player_attack(ctx: RefCounted) -> RefCounted:  ## DamageContext -> DamageResult
	var hooks_applied: Array[String] = []

	# 1. Calculate base gem damage
	ctx.base_damage = MFComboCalculator.gem_damage(ctx.attacker_atk, ctx.gems_matched)
	ctx.damage = ctx.base_damage

	# 2. Apply combo multiplier
	var combo_mult := MFComboCalculator.calculate(ctx.combo_count)
	ctx.damage *= combo_mult

	# 3. PRE_ELEMENT hooks
	_fire_hooks(MFCombatTypes.DamageHook.PRE_ELEMENT, ctx, hooks_applied)

	# 4. Element multiplier
	var elem_mult := _element_chart.get_multiplier(ctx.attacker_element, ctx.defender_element)
	ctx.damage *= elem_mult

	# 5. POST_ELEMENT hooks
	_fire_hooks(MFCombatTypes.DamageHook.POST_ELEMENT, ctx, hooks_applied)

	# 6. MAIN hooks (leader skills, team skills typically here)
	_fire_hooks(MFCombatTypes.DamageHook.MAIN, ctx, hooks_applied)

	# 7. Subtract defense
	ctx.damage = maxf(ctx.damage - ctx.defender_defense, 1.0)

	# 8. POST_DEFENSE hooks
	_fire_hooks(MFCombatTypes.DamageHook.POST_DEFENSE, ctx, hooks_applied)

	# 9. CAN_ZERO hooks (some skills can force damage to 0)
	_fire_hooks(MFCombatTypes.DamageHook.CAN_ZERO, ctx, hooks_applied)

	return MFCombatTypes.DamageResult.new(
		int(ctx.damage),
		elem_mult,
		combo_mult,
		hooks_applied
	)


## Resolve enemy attack (simpler -- no combos, no element matching).
func resolve_enemy_attack(enemy_atk: float, team_defense: float = 0.0) -> int:
	return int(maxf(enemy_atk - team_defense, 1.0))


## Get the element multiplier for UI display.
func calculate_element_multiplier(attacker: int, defender: int) -> float:
	return _element_chart.get_multiplier(attacker, defender)


func _fire_hooks(hook_type: int, ctx: RefCounted, hooks_applied: Array[String]) -> void:
	for hook_entry in _hooks[hook_type]:
		hook_entry.callable.call(ctx)
		if hook_entry.name != "":
			hooks_applied.append(hook_entry.name)
