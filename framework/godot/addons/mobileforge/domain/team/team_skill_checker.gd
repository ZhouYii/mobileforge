class_name MFTeamSkillChecker extends RefCounted
## Checks if team composition meets team skill conditions.
## Team skills activate based on which monsters are on the team.


## Check if a team satisfies a set of conditions (from team_skill JSON).
## conditions: Array of {type: String, params: Dictionary}
## team_defs: Array of MonsterDef for each filled slot
static func check_conditions(conditions: Array, team_defs: Array) -> bool:
	for cond in conditions:
		match cond["type"]:
			"all_same_element":
				if not _all_same_element(team_defs):
					return false
			"has_element":
				if not _has_element(team_defs, cond["params"]["element"]):
					return false
			"min_rarity":
				if not _min_rarity(team_defs, cond["params"]["min"]):
					return false
			"unique_elements":
				if not _unique_elements(team_defs, cond["params"]["count"]):
					return false
			"contains_monster":
				if not _contains_monster(team_defs, cond["params"]["monster_id"]):
					return false
			_:
				return false  # Unknown condition type
	return true


static func _all_same_element(defs: Array) -> bool:
	if defs.is_empty():
		return false
	var first_elem: int = defs[0].element
	for def in defs:
		if def.element != first_elem:
			return false
	return true


static func _has_element(defs: Array, element: int) -> bool:
	for def in defs:
		if def.element == element:
			return true
	return false


static func _min_rarity(defs: Array, min_val: int) -> bool:
	for def in defs:
		if def.rarity < min_val:
			return false
	return true


static func _unique_elements(defs: Array, required: int) -> bool:
	var elements: Dictionary = {}
	for def in defs:
		elements[def.element] = true
	return elements.size() >= required


static func _contains_monster(defs: Array, monster_id: int) -> bool:
	for def in defs:
		if def.id == monster_id:
			return true
	return false
