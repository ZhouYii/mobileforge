class_name TosCounterAttackOutcome extends MFSkillOutcome
## Reflects a percentage of received damage back at attacking enemies.
## Persists for duration_turns. Damage reflection happens during enemy turn processing.
## Params: { "reflect_percent": float (default 0.5 = 50%), "duration_turns": int (default 3) }


func activate(context: RefCounted, result: RefCounted) -> void:
	turns_left = int(get_param("duration_turns", 3))
	var pct := float(get_param("reflect_percent", 0.5))
	result.buffs_applied.append({
		"type": "counter_attack",
		"reflect_percent": pct,
		"turns": turns_left,
	})
