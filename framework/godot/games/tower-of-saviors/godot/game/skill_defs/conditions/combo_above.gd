class_name TosComboAboveCondition extends MFSkillCondition
## Returns true if combo count is at or above the threshold.
## Params: { "threshold": int (default 1) }


func is_valid(ctx: RefCounted) -> bool:
	return ctx.combo_count >= int(get_param("threshold", 1))
