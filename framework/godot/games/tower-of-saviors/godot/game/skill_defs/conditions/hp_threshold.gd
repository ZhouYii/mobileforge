class_name TosHpThresholdCondition extends MFSkillCondition
## Returns true if team HP ratio is at or below the threshold.
## Params: { "percent": float (default 0.5) }


func is_valid(ctx: RefCounted) -> bool:
	if ctx.max_hp <= 0:
		return false
	return float(ctx.team_hp) / float(ctx.max_hp) <= float(get_param("percent", 0.5))
