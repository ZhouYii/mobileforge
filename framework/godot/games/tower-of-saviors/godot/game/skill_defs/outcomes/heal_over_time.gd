class_name TosHealOverTimeOutcome extends MFSkillOutcome
## Heals the team each turn based on recovery_multiplier * team REC.
## Params: { "recovery_multiplier": float, "duration_turns": int }


func activate(context: RefCounted, result: RefCounted) -> void:
	turns_left = int(get_param("duration_turns", 1))
	# Apply first tick immediately
	_apply_heal(context, result)


func on_turn_start(context: RefCounted) -> void:
	var result = MFSkillTypes.SkillResult.new()
	_apply_heal(context, result)
	if result.healing > 0 and context.has("extra"):
		context.extra["pending_healing"] = context.extra.get("pending_healing", 0) + result.healing


func _apply_heal(context: RefCounted, result: RefCounted) -> void:
	var mult := float(get_param("recovery_multiplier", 1.0))
	var team_rec := 0.0
	for stats in context.team_stats:
		if stats != null and stats.has_method("get") if stats is Dictionary else "rec" in stats:
			team_rec += float(stats.rec) if "rec" in stats else 0.0
	if team_rec <= 0.0:
		team_rec = 100.0  # fallback if no REC stat available
	result.healing += int(team_rec * mult)
