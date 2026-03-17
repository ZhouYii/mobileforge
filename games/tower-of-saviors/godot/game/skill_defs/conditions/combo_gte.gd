class_name TosComboGteCondition extends MFSkillCondition
## Returns true if combo count >= min_combo.
## Params: { "min_combo": int (default 1) }


func is_valid(ctx: RefCounted) -> bool:
	return ctx.combo_count >= int(get_param("min_combo", 1))
