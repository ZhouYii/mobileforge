class_name TosDefenseBuffOutcome extends MFSkillOutcome
## Registers a POST_DEFENSE hook that reduces incoming damage for N turns.
## Params: { "damage_reduction": float (0.0-1.0), "duration_turns": int }

var _reduction: float
var _hook_ref: Callable


func activate(context: RefCounted, result: RefCounted) -> void:
	_reduction = float(get_param("damage_reduction", 0.5))
	turns_left = int(get_param("duration_turns", 2))

	if context.combat != null and context.combat.has_method("register_hook"):
		_hook_ref = func(dmg_ctx):
			dmg_ctx.damage *= (1.0 - _reduction)
		context.combat.register_hook(
			MFCombatTypes.DamageHook.POST_DEFENSE, _hook_ref, 50, "defense_buff")

	result.buffs_applied.append({
		"type": "defense_buff", "reduction": _reduction, "turns": turns_left
	})


func deactivate(context: RefCounted) -> void:
	if context.combat != null and context.combat.has_method("unregister_hook") and _hook_ref.is_valid():
		context.combat.unregister_hook(MFCombatTypes.DamageHook.POST_DEFENSE, _hook_ref)
