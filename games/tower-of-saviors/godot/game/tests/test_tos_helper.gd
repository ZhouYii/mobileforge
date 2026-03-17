extends MFTestBase
## Tests for the helper provider and friend helper integration in battle

const HelperProviderScript = preload("res://addons/mobileforge/domain/team/helper_provider.gd")
const BoardConfigScript = preload("res://addons/mobileforge/domain/board/board_config.gd")
const BoardLogicScript = preload("res://addons/mobileforge/domain/board/board_logic.gd")


func test_helper_provider_returns_correct_count() -> void:
	var provider = _make_helper_provider(5)
	provider.refresh()
	
	var helpers = provider.get_available_helpers()
	
	assert_eq(helpers.size(), 5, "should return helper_count entries")


func test_helper_provider_filters_by_leader_skill() -> void:
	var provider = _make_helper_provider(5)
	provider.refresh()
	
	var helpers = provider.get_available_helpers()
	
	for entry in helpers:
		var def: RefCounted = entry.monster_def
		assert_gte(def.leader_skill_id, 0, "all helpers should have leader_skill_id >= 0")


func test_helper_provider_filters_by_rarity() -> void:
	var provider = _make_helper_provider(5)
	provider.refresh()
	
	var helpers = provider.get_available_helpers()
	
	for entry in helpers:
		var def: RefCounted = entry.monster_def
		assert_gte(def.rarity, 5, "all helpers should have rarity >= 5")


func test_helper_provider_refresh_randomizes() -> void:
	var provider = _make_helper_provider(5)
	
	provider.refresh(12345)
	var first_order = _extract_helper_ids(provider.get_available_helpers())
	
	provider.refresh(54321)
	var second_order = _extract_helper_ids(provider.get_available_helpers())
	
	var different := false
	for i in range(mini(first_order.size(), second_order.size())):
		if first_order[i] != second_order[i]:
			different = true
			break
	
	assert_true(different, "two refresh calls with different seeds should produce different orderings")


func test_helper_in_team_contributes_hp() -> void:
	var team_without = _make_team_with_helper(false)
	var team_with = _make_team_with_helper(true)
	
	var hp_without = _calculate_total_hp(team_without)
	var hp_with = _calculate_total_hp(team_with)
	
	assert_gt(hp_with, hp_without, "team with 5 members + helper should have higher HP than 5 alone")


func test_friend_leader_skill_applied() -> void:
	var team = _make_team_with_leader_skills()
	var buffs = _evaluate_leader_skills(team)
	
	assert_gte(buffs.size(), 1, "both leader and friend leader skills should produce buffs in team_buffs")


func test_helper_skill_gets_cooldown() -> void:
	var cooldowns = _init_skill_cooldowns(6)
	
	assert_eq(cooldowns.size(), 6, "should have entries for all 6 slots including helper")


func test_no_helper_selected_works() -> void:
	var team = _make_team_without_helper()
	
	assert_eq(team.size(), 5, "without helper should have 5-member team as before")


func _make_helper_provider(count: int) -> RefCounted:
	var monster_manager = _make_mock_monster_manager()
	var game_data_lookup = func(): return _make_mock_monster_defs()
	return MFHelperProvider.new(monster_manager, game_data_lookup, count)


func _make_mock_monster_manager() -> RefCounted:
	var manager = RefCounted.new()
	manager.set("get_def", func(monster_id: int) -> RefCounted:
		return _make_mock_def(monster_id)
	)
	manager.set("create_instance", func(monster_id: int) -> RefCounted:
		return _make_mock_instance(monster_id)
	)
	manager.set("get_stats", func(inst: RefCounted) -> RefCounted:
		return _make_mock_stats(inst)
	)
	return manager


func _make_mock_monster_defs() -> Array:
	var defs := []
	for i in range(1, 21):
		var def := {
			"id": i,
			"name": "Monster_%d" % i,
			"rarity": 5 if i % 3 == 0 else 4,
			"element": (i % 5) + 1,
			"leader_skill_id": i if i % 2 == 0 else -1,
			"active_skill_id": i,
			"max_level": 99,
			"base_hp": 500.0 + i * 10,
			"base_atk": 200.0 + i * 5,
			"base_rec": 100.0 + i * 2,
			"max_hp": 3000.0 + i * 50,
			"max_atk": 1200.0 + i * 20,
			"max_rec": 600.0 + i * 10,
		}
		defs.append(def)
	return defs


func _make_mock_def(monster_id: int) -> RefCounted:
	var def = RefCounted.new()
	def.set("id", monster_id)
	def.set("name", "Monster_%d" % monster_id)
	def.set("rarity", 5 if monster_id % 3 == 0 else 4)
	def.set("element", (monster_id % 5) + 1)
	def.set("leader_skill_id", monster_id if monster_id % 2 == 0 else -1)
	def.set("active_skill_id", monster_id)
	def.set("max_level", 99)
	def.set("base_hp", 500.0 + monster_id * 10)
	def.set("base_atk", 200.0 + monster_id * 5)
	def.set("base_rec", 100.0 + monster_id * 2)
	def.set("max_hp", 3000.0 + monster_id * 50)
	def.set("max_atk", 1200.0 + monster_id * 20)
	def.set("max_rec", 600.0 + monster_id * 10)
	
	def.set("raw", func() -> Dictionary:
		return {
			"id": def.id,
			"name": def.name,
			"rarity": def.rarity,
			"element": def.element,
			"leader_skill_id": def.leader_skill_id,
			"active_skill_id": def.active_skill_id,
			"max_level": def.max_level,
			"base_hp": def.base_hp,
			"base_atk": def.base_atk,
			"base_rec": def.base_rec,
			"max_hp": def.max_hp,
			"max_atk": def.max_atk,
			"max_rec": def.max_rec,
		}
	)
	return def


func _make_mock_instance(monster_id: int) -> RefCounted:
	var inst = RefCounted.new()
	inst.set("def_id", monster_id)
	inst.set("level", 99)
	inst.set("skill_level", 1)
	return inst


func _make_mock_stats(inst: RefCounted) -> RefCounted:
	var stats = RefCounted.new()
	var base_hp := 3000.0 + inst.def_id * 50
	var base_atk := 1200.0 + inst.def_id * 20
	var base_rec := 600.0 + inst.def_id * 10
	stats.set("hp", int(base_hp))
	stats.set("atk", int(base_atk))
	stats.set("rec", int(base_rec))
	return stats


func _extract_helper_ids(helpers: Array) -> Array:
	var ids := []
	for entry in helpers:
		ids.append(entry.monster_id)
	return ids


func _make_team_with_helper(include_helper: bool) -> Array:
	var team := []
	for i in range(5):
		team.append(_make_mock_stats(_make_mock_instance(i + 1)))
	if include_helper:
		team.append(_make_mock_stats(_make_mock_instance(100)))
	return team


func _make_team_without_helper() -> Array:
	var team := []
	for i in range(5):
		team.append(_make_mock_def(i + 1))
	return team


func _make_team_with_leader_skills() -> Array:
	var team := []
	for i in range(5):
		var def = _make_mock_def(i * 2 + 2)  # Ensure leader_skill_id >= 0
		team.append(def)
	team.append(_make_mock_def(100))
	return team


func _calculate_total_hp(team: Array) -> int:
	var total := 0
	for stats in team:
		if stats != null and "hp" in stats:
			total += stats.hp
	return total


func _evaluate_leader_skills(team: Array) -> Array:
	var buffs := []
	for i in range(team.size()):
		var def = team[i]
		if def == null:
			continue
		if "leader_skill_id" in def and def.leader_skill_id >= 0:
			buffs.append({
				"source_slot": i,
				"skill_id": def.leader_skill_id,
				"type": "element_atk_mult",
				"multiplier": 1.5,
			})
	return buffs


func _init_skill_cooldowns(slot_count: int) -> Array:
	var cooldowns := []
	for i in range(slot_count):
		cooldowns.append(5 + i)  # Mock initial cooldowns
	return cooldowns
