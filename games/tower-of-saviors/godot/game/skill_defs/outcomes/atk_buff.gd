class_name TosAtkBuffOutcome extends MFSkillOutcome
## Registers a MAIN hook that multiplies damage for N turns.
## If element is 0, applies to all elements.
## Params: { "element": int, "multiplier": float, "duration_turns": int }

var _multiplier: float
var _element: int
var _hook_ref: Callable


func activate(context: RefCounted, result: RefCounted) -> void:
	_multiplier = float(get_param("multiplier", 1.5))
	_element = int(get_param("element", 0))
	turns_left = int(get_param("duration_turns", 1))

	if context.combat != null and context.combat.has_method("register_hook"):
		_hook_ref = func(dmg_ctx):
			if _element == 0 or dmg_ctx.attacker_element == _element:
				dmg_ctx.damage *= _multiplier
		context.combat.register_hook(
			MFCombatTypes.DamageHook.MAIN, _hook_ref, 100, "atk_buff")

	result.buffs_applied.append({
		"type": "atk_buff", "element": _element,
		"multiplier": _multiplier, "turns": turns_left
	})


func deactivate(context: RefCounted) -> void:
	if context.combat != null and context.combat.has_method("unregister_hook") and _hook_ref.is_valid():
		context.combat.unregister_hook(MFCombatTypes.DamageHook.MAIN, _hook_ref)
