class_name TosComboLtCondition extends MFSkillCondition
## Returns true if combo count < max_combo.
## Params: { "max_combo": int (default 1) }


func is_valid(ctx: RefCounted) -> bool:
	return ctx.combo_count < int(get_param("max_combo", 1))
