class_name TosElementsMatchedCondition extends MFSkillCondition
## Returns true if a specific element was matched at least min_count times.
## Params: { "element": int, "min_count": int (default 1) }


func is_valid(ctx: RefCounted) -> bool:
	var elem := int(get_param("element", 0))
	var min_count := int(get_param("min_count", 1))
	return ctx.elements_matched.get(elem, 0) >= min_count
