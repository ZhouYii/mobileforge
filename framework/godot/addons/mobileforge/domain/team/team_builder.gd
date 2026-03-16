class_name MFTeamBuilder extends RefCounted
## Builds and validates teams.

var _team: RefCounted  # MFTeamTypes.Team
var _monster_manager: RefCounted  # MFMonsterManager reference


func _init(max_slots: int = 6, monster_manager: RefCounted = null) -> void:
	_team = MFTeamTypes.Team.new(max_slots)
	_monster_manager = monster_manager


func get_team() -> RefCounted:
	return _team


func set_slot(index: int, monster: RefCounted) -> bool:  # MonsterInstance or null
	if index < 0 or index >= _team.max_slots:
		return false
	_team.set_slot(index, monster)
	return true


func clear_slot(index: int) -> bool:
	return set_slot(index, null)


func clear_all() -> void:
	for i in range(_team.max_slots):
		_team.set_slot(i, null)


## Validate the team (at least 1 member, no duplicates, within cost limit)
func validate(max_cost: int = 999) -> Array[String]:  # Returns list of error messages
	var errors: Array[String] = []

	if _team.get_filled_count() == 0:
		errors.append("Team must have at least 1 member")
		return errors

	# Check for duplicate instances
	var seen_ids: Dictionary = {}
	for i in range(_team.max_slots):
		var monster = _team.get_slot(i)
		if monster == null:
			continue
		if seen_ids.has(monster.instance_id):
			errors.append("Duplicate monster in slots %d and %d" % [seen_ids[monster.instance_id], i])
		seen_ids[monster.instance_id] = i

	# Check cost
	var stats := get_team_stats()
	if stats.total_cost > max_cost:
		errors.append("Team cost %d exceeds limit %d" % [stats.total_cost, max_cost])

	return errors


## Calculate aggregated team stats
func get_team_stats() -> RefCounted:  # -> TeamStats
	var total_hp := 0
	var total_atk := 0
	var total_rec := 0
	var total_cost := 0
	var count := 0

	for i in range(_team.max_slots):
		var monster = _team.get_slot(i)
		if monster == null:
			continue
		count += 1
		if _monster_manager != null:
			var stats = _monster_manager.get_stats(monster)
			if stats != null:
				total_hp += stats.hp
				total_atk += stats.atk
				total_rec += stats.rec
			var def = _monster_manager.get_def(monster.def_id)
			if def != null:
				total_cost += def.cost

	return MFTeamTypes.TeamStats.new(total_hp, total_atk, total_rec, total_cost, count)
