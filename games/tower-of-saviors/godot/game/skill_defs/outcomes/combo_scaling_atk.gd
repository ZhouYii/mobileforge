class_name TosComboScalingAtkOutcome extends MFSkillOutcome
## Registers a MAIN hook that scales ATK with combo count for N turns.
## Each combo above threshold adds bonus_per_combo to multiplier.
## Params: { "bonus_per_combo": float, "duration_turns": int }

var _bonus_per_combo: float
var _hook_ref: Callable


func activate(context: RefCounted, result: RefCounted) -> void:
	_bonus_per_combo = float(get_param("bonus_per_combo", 0.5))
	turns_left = int(get_param("duration_turns", 1))

	if context.combat != null and context.combat.has_method("register_hook"):
		_hook_ref = func(dmg_ctx):
			var extra_combos := maxi(dmg_ctx.combo_count - 1, 0)
			dmg_ctx.damage *= (1.0 + extra_combos * _bonus_per_combo)
		context.combat.register_hook(
			MFCombatTypes.DamageHook.MAIN, _hook_ref, 110, "combo_scaling_atk")

	result.buffs_applied.append({
		"type": "combo_scaling_atk", "bonus_per_combo": _bonus_per_combo, "turns": turns_left
	})


func deactivate(context: RefCounted) -> void:
	if context.combat != null and context.combat.has_method("unregister_hook") and _hook_ref.is_valid():
		context.combat.unregister_hook(MFCombatTypes.DamageHook.MAIN, _hook_ref)
