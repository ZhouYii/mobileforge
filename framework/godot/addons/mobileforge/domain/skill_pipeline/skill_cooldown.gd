class_name MFSkillCooldown extends RefCounted
## Cooldown calculation for active skills.
## ToS formula: current_cd = max(max_cd + 1 - skill_level, min_cd)


static func calculate_cd(max_cd: int, min_cd: int, skill_level: int) -> int:
	return maxi(max_cd + 1 - skill_level, min_cd)


## Tick cooldown by 1 turn. Returns new cooldown value.
static func tick(current_cd: int) -> int:
	return maxi(current_cd - 1, 0)


## Check if skill is ready (cooldown == 0)
static func is_ready(current_cd: int) -> bool:
	return current_cd <= 0
