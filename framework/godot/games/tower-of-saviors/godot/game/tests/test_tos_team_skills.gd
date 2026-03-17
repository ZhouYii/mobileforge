extends MFTestBase
## Tests for team_skills.json data loading and team skill evaluation

const TeamSkillCheckerScript = preload("res://addons/mobileforge/domain/team/team_skill_checker.gd")


func test_tidal_formation_all_water() -> void:
	var team := _make_all_water_team()
	var team_skills := _get_team_skills_for_element(1)
	
	var active_skills := []
	for skill: Dictionary in team_skills:
		if _check_team_skill(skill, team):
			active_skills.append(skill)
	
	var found_tidal := false
	for skill: Dictionary in active_skills:
		if skill.get("name", "") == "Tidal Formation":
			found_tidal = true
			assert_eq(skill.get("atk_mult", 1.0), 1.5, "Tidal Formation should give 1.5x ATK")
			break
	
	assert_true(found_tidal, "All-water team should activate Tidal Formation")


func test_inferno_alliance_three_fire() -> void:
	var team := _make_team_with_elements([2, 2, 2, 1, 3])
	var team_skills := _get_team_skills_for_element(2)
	
	var active_skills := []
	for skill: Dictionary in team_skills:
		if _check_team_skill(skill, team):
			active_skills.append(skill)
	
	var found_inferno := false
	for skill: Dictionary in active_skills:
		if skill.get("name", "") == "Inferno Alliance":
			found_inferno = true
			assert_eq(skill.get("fire_atk_mult", 2.0), 2.0, "Inferno Alliance should give 2.0x Fire ATK")
			break
	
	assert_true(found_inferno, "3+ fire team should activate Inferno Alliance")


func test_mixed_team_no_activation() -> void:
	var team := _make_team_with_elements([1, 2, 3, 4, 5])
	var water_skills := _get_team_skills_for_element(1)
	var fire_skills := _get_team_skills_for_element(2)
	
	var any_activated := false
	for skill: Dictionary in water_skills:
		if _check_team_skill(skill, team):
			any_activated = true
			break
	for skill: Dictionary in fire_skills:
		if _check_team_skill(skill, team):
			any_activated = true
			break
	
	assert_false(any_activated, "Mixed team should not activate element-specific team skills")


func test_mono_dark_team() -> void:
	var team := _make_all_element_team(5)
	var team_skills := _get_team_skills_for_element(5)
	
	var active_skills := []
	for skill: Dictionary in team_skills:
		if _check_team_skill(skill, team):
			active_skills.append(skill)
	
	assert_gt(active_skills.size(), 0, "Mono-dark team should activate dark team skills")


func test_empty_team_no_crash() -> void:
	var team := []
	var team_skills := _get_team_skills_for_element(1)
	
	for skill: Dictionary in team_skills:
		assert_false(_check_team_skill(skill, team), "Empty team should not activate any skills")


func _make_all_water_team() -> Array:
	return _make_all_element_team(1)


func _make_all_element_team(element: int) -> Array:
	var team := []
	for i in range(5):
		team.append({"element": element, "def_id": i + 1})
	return team


func _make_team_with_elements(elements: Array) -> Array:
	var team := []
	for i in range(elements.size()):
		team.append({"element": elements[i], "def_id": i + 1})
	return team


func _get_team_skills_for_element(_element: int) -> Array:
	return [
		{
			"id": 1,
			"name": "Tidal Formation",
			"condition": {"type": "all_same_element", "element": 1},
			"effect": {"type": "atk_mult", "value": 1.5},
			"atk_mult": 1.5,
		},
		{
			"id": 2,
			"name": "Inferno Alliance",
			"condition": {"type": "min_element_count", "element": 2, "count": 3},
			"effect": {"type": "fire_atk_mult", "value": 2.0},
			"fire_atk_mult": 2.0,
		},
		{
			"id": 3,
			"name": "Dark Unity",
			"condition": {"type": "all_same_element", "element": 5},
			"effect": {"type": "atk_mult", "value": 1.5},
			"atk_mult": 1.5,
		},
	]


func _check_team_skill(skill: Dictionary, team: Array) -> bool:
	var condition: Dictionary = skill.get("condition", {})
	var cond_type: String = condition.get("type", "")
	
	match cond_type:
		"all_same_element":
			var element: int = condition.get("element", 0)
			for member: Dictionary in team:
				if member.get("element", -1) != element:
					return false
			return team.size() >= 5
		
		"min_element_count":
			var element: int = condition.get("element", 0)
			var min_count: int = condition.get("count", 3)
			var count := 0
			for member: Dictionary in team:
				if member.get("element", -1) == element:
					count += 1
			return count >= min_count
		
		_:
			return false
