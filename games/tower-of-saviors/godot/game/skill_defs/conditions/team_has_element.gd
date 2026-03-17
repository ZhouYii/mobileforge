class_name TosTeamHasElementCondition extends MFSkillCondition
## Returns true if any monster on the team has the specified element.
## Params: { "element": int }


func is_valid(ctx: RefCounted) -> bool:
	var elem := int(get_param("element", 0))
	for monster in ctx.team:
		if monster != null and monster.get("element") == elem:
			return true
	return false
