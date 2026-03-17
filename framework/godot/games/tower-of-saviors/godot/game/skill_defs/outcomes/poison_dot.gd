class_name TosPoisonDotOutcome extends MFSkillOutcome
## Applies poison to all alive enemies, dealing damage each turn.
## Damage = multiplier * caster ATK per turn for duration_turns.
## Params: { "multiplier": float (default 1.0), "duration_turns": int (default 3) }


func activate(context: RefCounted, result: RefCounted) -> void:
	turns_left = int(get_param("duration_turns", 3))
	var mult := float(get_param("multiplier", 1.0))
	var atk := _get_caster_atk(context)

	# Apply poison status to all alive enemies
	var dmg_per_tick := int(atk * mult)
	for enemy in context.enemies:
		if enemy.is_alive:
			enemy.add_status("poison", turns_left, {"damage_per_turn": dmg_per_tick})


func on_turn_start(context: RefCounted) -> void:
	# Tick poison damage on all poisoned enemies
	for i in range(context.enemies.size()):
		var enemy = context.enemies[i]
		if not enemy.is_alive:
			continue
		for status in enemy.status_effects:
			if status["type"] == "poison" and status.has("data") and status["data"] != null:
				var tick_dmg: int = status["data"].get("damage_per_turn", 0)
				if tick_dmg > 0:
					enemy.take_damage(tick_dmg)


func _get_caster_atk(context: RefCounted) -> float:
	if context.caster_index >= 0 and context.caster_index < context.team_stats.size():
		var stats = context.team_stats[context.caster_index]
		if stats != null:
			return float(stats.atk)
	# Fallback: first team member
	if not context.team_stats.is_empty() and context.team_stats[0] != null:
		return float(context.team_stats[0].atk)
	return 100.0
