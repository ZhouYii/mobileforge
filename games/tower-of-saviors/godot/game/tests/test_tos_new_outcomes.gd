extends MFTestBase
## Tests for new ToS outcomes (single_target_damage, gem_conversion, self_damage, lifesteal, heal_over_time, atk_buff, defense_buff, combo_scaling_atk)

const BoardConfigScript = preload("res://addons/mobileforge/domain/board/board_config.gd")
const BoardLogicScript = preload("res://addons/mobileforge/domain/board/board_logic.gd")


func test_single_target_damage_targets_highest_hp() -> void:
	var ctx = _make_context_with_enemies([
		{"hp": 500, "is_alive": true},
		{"hp": 1500, "is_alive": true},
		{"hp": 800, "is_alive": true},
	])
	var result = _make_result()
	
	_execute_single_target_damage({"multiplier": 5.0, "target": "highest_hp"}, ctx, result)
	
	assert_eq(result.damage_dealt.size(), 1, "should hit exactly one enemy")
	assert_true(result.damage_dealt.has(1), "should hit enemy at index 1 (highest HP)")
	assert_eq(result.damage_dealt[1], 7500, "damage should be atk * multiplier")


func test_single_target_damage_skips_dead() -> void:
	var ctx = _make_context_with_enemies([
		{"hp": 1000, "is_alive": false},
		{"hp": 500, "is_alive": true},
	])
	var result = _make_result()
	
	_execute_single_target_damage({"multiplier": 2.0, "target": "highest_hp"}, ctx, result)
	
	assert_true(result.damage_dealt.has(1), "should target only alive enemy")
	assert_eq(result.damage_dealt[1], 3000, "damage should be atk * multiplier")


func test_gem_conversion_converts_from_to() -> void:
	var ctx = _make_context_with_board([1, 1, 1, 2, 2, 3, 1, 4, 5, 6, 1, 2])
	var result = _make_result()
	
	_execute_gem_conversion({"from_element": 1, "to_element": 6}, ctx, result)
	
	assert_gt(result.board_changes.size(), 0, "should have board changes")
	for change: Dictionary in result.board_changes:
		assert_eq(change.old_element, 1, "old element should be 1")
		assert_eq(change.new_element, 6, "new element should be 6")


func test_self_damage_reduces_hp_by_percent() -> void:
	var ctx = _make_context({"team_hp": 10000, "max_hp": 10000})
	var result = _make_result()
	
	_execute_self_damage({"hp_percent": 0.2}, ctx, result)
	
	assert_eq(ctx.team_hp, 8000, "should reduce HP by 20% of max_hp")


func test_self_damage_floors_at_one() -> void:
	var ctx = _make_context({"team_hp": 100, "max_hp": 10000})
	var result = _make_result()
	
	_execute_self_damage({"hp_percent": 0.5}, ctx, result)
	
	assert_eq(ctx.team_hp, 1, "self damage should not kill the player")


func test_lifesteal_heals_from_previous_damage() -> void:
	var ctx = _make_context({"team_hp": 5000, "max_hp": 10000})
	var result = _make_result()
	result.damage_dealt[0] = 1000
	result.damage_dealt[1] = 2000
	
	_execute_lifesteal({"percent_of_damage": 0.3}, ctx, result)
	
	assert_eq(result.healing, 900, "should heal 30% of 3000 total damage")


func test_lifesteal_no_damage_no_heal() -> void:
	var ctx = _make_context({"team_hp": 5000, "max_hp": 10000})
	var result = _make_result()
	
	_execute_lifesteal({"percent_of_damage": 0.5}, ctx, result)
	
	assert_eq(result.healing, 0, "no damage should result in no healing")


func test_heal_over_time_heals_each_turn() -> void:
	var ctx = _make_context({"team_hp": 5000, "max_hp": 10000})
	ctx.team_stats = [_make_stats(1000, 1500, 300)]
	var result = _make_result()
	
	var outcome = TosHealOverTimeOutcome.new({"recovery_multiplier": 2.0, "duration_turns": 3})
	outcome.activate(ctx, result)
	
	assert_gt(result.healing, 0, "should heal on activate")
	
	var heal_on_activate = result.healing
	result.healing = 0
	
	outcome.on_turn_start(ctx, result)
	assert_eq(result.healing, heal_on_activate, "should heal same amount on turn start")


func test_atk_buff_multiplies_for_duration() -> void:
	var ctx = _make_context_with_combat()
	var result = _make_result()
	
	var outcome = TosAtkBuffOutcome.new({"element": 0, "multiplier": 2.0, "duration_turns": 2})
	outcome.activate(ctx, result)
	
	assert_eq(result.buffs_applied.size(), 1, "should have one buff applied")
	assert_eq(result.buffs_applied[0].multiplier, 2.0, "buff should have correct multiplier")
	assert_eq(outcome.turns_left, 2, "should have 2 turns remaining")
	
	outcome.on_turn_end(ctx)
	assert_eq(outcome.turns_left, 1, "should have 1 turn remaining after first tick")
	
	outcome.on_turn_end(ctx)
	assert_eq(outcome.turns_left, 0, "should expire after duration")


func test_defense_buff_reduces_damage() -> void:
	var ctx = _make_context_with_combat()
	var result = _make_result()
	
	var outcome = TosDefenseBuffOutcome.new({"damage_reduction": 0.5, "duration_turns": 3})
	outcome.activate(ctx, result)
	
	assert_eq(result.buffs_applied.size(), 1, "should have one buff applied")
	assert_eq(result.buffs_applied[0].damage_reduction, 0.5, "should have correct reduction")


func test_combo_scaling_atk_scales_with_combos() -> void:
	var ctx = _make_context_with_combat()
	var result = _make_result()
	
	var outcome = TosComboScalingAtkOutcome.new({"bonus_per_combo": 0.25, "duration_turns": 2})
	outcome.activate(ctx, result)
	
	assert_eq(result.buffs_applied.size(), 1, "should have one buff applied")
	assert_eq(result.buffs_applied[0].bonus_per_combo, 0.25, "should have correct bonus")


func _make_context(overrides: Dictionary = {}) -> RefCounted:
	var ctx = RefCounted.new()
	ctx.set("team_hp", overrides.get("team_hp", 10000))
	ctx.set("max_hp", overrides.get("max_hp", 10000))
	ctx.set("team_stats", [])
	ctx.set("enemies", [])
	ctx.set("board", null)
	ctx.set("combat", null)
	return ctx


func _make_context_with_enemies(enemy_data: Array) -> RefCounted:
	var ctx = _make_context()
	ctx.enemies = []
	for data: Dictionary in enemy_data:
		var enemy = RefCounted.new()
		enemy.set("hp", data.get("hp", 1000))
		enemy.set("is_alive", data.get("is_alive", true))
		enemy.set("countdown", 1)
		ctx.enemies.append(enemy)
	ctx.team_stats = [_make_stats(1000, 1500, 300)]
	return ctx


func _make_context_with_board(elements: Array) -> RefCounted:
	var ctx = _make_context()
	var typed: Array[int] = []
	for e in elements:
		typed.append(int(e))
	var config = MFBoardConfig.new(2, 6)
	ctx.board = MFBoardLogic.new(config, 12345)
	ctx.board.from_element_array(typed)
	return ctx


func _make_context_with_combat() -> RefCounted:
	var ctx = _make_context()
	var mock_combat = RefCounted.new()
	mock_combat.set("hooks", {})
	mock_combat.set("register_hook", func(_hook_type, _callback, _priority, _name): void: pass)
	mock_combat.set("unregister_hook", func(_hook_type, _callback): void: pass)
	ctx.combat = mock_combat
	return ctx


func _make_result() -> RefCounted:
	var result = RefCounted.new()
	result.set("healing", 0)
	result.set("damage_dealt", {})
	result.set("board_changes", [])
	result.set("buffs_applied", [])
	return result


func _make_stats(hp: int, atk: int, rec: int) -> RefCounted:
	var stats = RefCounted.new()
	stats.set("hp", hp)
	stats.set("atk", atk)
	stats.set("rec", rec)
	return stats


func _execute_single_target_damage(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	TosSkillRegistration._register_simple_effects(_make_mock_pipeline())
	var pipeline = _make_mock_pipeline()
	TosSkillRegistration._register_simple_effects(pipeline)
	pipeline.effect_registry.execute("single_target_damage", params, ctx, result)


func _execute_gem_conversion(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var pipeline = _make_mock_pipeline()
	TosSkillRegistration._register_simple_effects(pipeline)
	pipeline.effect_registry.execute("gem_conversion", params, ctx, result)


func _execute_self_damage(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var pipeline = _make_mock_pipeline()
	TosSkillRegistration._register_simple_effects(pipeline)
	pipeline.effect_registry.execute("self_damage", params, ctx, result)


func _execute_lifesteal(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var pipeline = _make_mock_pipeline()
	TosSkillRegistration._register_simple_effects(pipeline)
	pipeline.effect_registry.execute("lifesteal", params, ctx, result)


func _make_mock_pipeline() -> MFSkillPipeline:
	return MFSkillPipeline.new()
