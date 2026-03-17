extends MFTestBase
## Tests for ToS skill outcomes (area_damage, change_gem_element, delay_enemies)

const AreaDamageScript = preload("res://game/skill_defs/outcomes/area_damage.gd")
const ChangeGemElementScript = preload("res://game/skill_defs/outcomes/change_gem_element.gd")
const DelayEnemiesScript = preload("res://game/skill_defs/outcomes/delay_enemies.gd")
const BoardConfigScript = preload("res://addons/mobileforge/domain/board/board_config.gd")
const BoardLogicScript = preload("res://addons/mobileforge/domain/board/board_logic.gd")


func test_area_damage_hits_all_alive() -> void:
	var ctx = _make_context_with_enemies([true, true, false])
	var result = _make_result()
	
	TosAreaDamageOutcome.execute({"multiplier": 10.0}, ctx, result)
	
	assert_eq(result.damage_dealt.get(0, 0), 15000, "Enemy 0 should take 15000 damage")
	assert_eq(result.damage_dealt.get(1, 0), 15000, "Enemy 1 should take 15000 damage")
	assert_eq(result.damage_dealt.get(2, 0), 0, "Dead enemy 2 should not take damage")


func test_area_damage_skips_dead() -> void:
	var ctx = _make_context_with_enemies([false, false, false])
	var result = _make_result()
	
	TosAreaDamageOutcome.execute({"multiplier": 5.0}, ctx, result)
	
	assert_eq(result.damage_dealt.size(), 0, "No damage should be dealt to dead enemies")


func test_area_damage_default_multiplier() -> void:
	var ctx = _make_context_with_enemies([true])
	var result = _make_result()
	
	TosAreaDamageOutcome.execute({}, ctx, result)
	
	assert_eq(result.damage_dealt.get(0, 0), 1500, "Default multiplier should be 1.0")


func test_change_gem_element_converts() -> void:
	var ctx = _make_context_with_board([1, 1, 1, 2, 3, 4, 2, 3, 4, 5, 6, 1])
	var result = _make_result()
	
	TosChangeGemElementOutcome.execute({"from": 1, "to": 6}, ctx, result)
	
	assert_gt(result.board_changes.size(), 0, "Should have board changes")
	for change in result.board_changes:
		assert_eq(change.old_element, 1, "Old element should be 1")
		assert_eq(change.new_element, 6, "New element should be 6")


func test_change_gem_element_no_board() -> void:
	var ctx = _make_context_no_board()
	var result = _make_result()
	
	TosChangeGemElementOutcome.execute({"from": 1, "to": 6}, ctx, result)
	
	assert_eq(result.board_changes.size(), 0, "Should handle null board gracefully")


func test_change_gem_element_no_matches() -> void:
	var ctx = _make_context_with_board([2, 3, 4, 5, 6, 2, 3, 4, 5, 6, 1, 2])
	var result = _make_result()
	
	TosChangeGemElementOutcome.execute({"from": 1, "to": 6}, ctx, result)
	
	assert_eq(result.board_changes.size(), 1, "Should find 1 gem with element 1")


func test_delay_enemies_adds_turns() -> void:
	var ctx = _make_context_with_enemies([true, true])
	ctx.enemies[0].countdown = 3
	ctx.enemies[1].countdown = 1
	var result = _make_result()
	
	TosDelayEnemiesOutcome.execute({"turns": 2}, ctx, result)
	
	assert_eq(ctx.enemies[0].countdown, 5, "Enemy 0 countdown should be 3 + 2 = 5")
	assert_eq(ctx.enemies[1].countdown, 3, "Enemy 1 countdown should be 1 + 2 = 3")


func test_delay_enemies_default_turns() -> void:
	var ctx = _make_context_with_enemies([true])
	ctx.enemies[0].countdown = 2
	var result = _make_result()
	
	TosDelayEnemiesOutcome.execute({}, ctx, result)
	
	assert_eq(ctx.enemies[0].countdown, 3, "Default delay should be 1 turn")


func test_delay_enemies_skips_dead() -> void:
	var ctx = _make_context_with_enemies([false, true])
	ctx.enemies[0].countdown = 1
	ctx.enemies[1].countdown = 2
	var result = _make_result()
	
	TosDelayEnemiesOutcome.execute({"turns": 3}, ctx, result)
	
	assert_eq(ctx.enemies[0].countdown, 1, "Dead enemy countdown should not change")
	assert_eq(ctx.enemies[1].countdown, 5, "Alive enemy countdown should increase")


func _make_context_with_enemies(alive_states: Array) -> RefCounted:
	var ctx = RefCounted.new()
	ctx.enemies = []
	ctx.team_stats = [_make_stats(1000, 1500, 300)]
	for i in range(alive_states.size()):
		var enemy = _make_enemy(alive_states[i])
		ctx.enemies.append(enemy)
	ctx.board = null
	return ctx


func _make_context_with_board(elements: Array) -> RefCounted:
	var ctx = RefCounted.new()
	ctx.enemies = []
	ctx.team_stats = []
	
	var typed: Array[int] = []
	for e in elements:
		typed.append(e)
	
	var config = MFBoardConfig.new(2, 6)
	ctx.board = MFBoardLogic.new(config, 12345)
	ctx.board.from_element_array(typed)
	return ctx


func _make_context_no_board() -> RefCounted:
	var ctx = RefCounted.new()
	ctx.enemies = []
	ctx.team_stats = []
	ctx.board = null
	return ctx


func _make_result() -> RefCounted:
	var result = RefCounted.new()
	result.damage_dealt = {}
	result.board_changes = []
	result.healing = 0
	return result


func _make_enemy(is_alive: bool) -> RefCounted:
	var enemy = RefCounted.new()
	enemy.is_alive = is_alive
	enemy.countdown = 1
	enemy.hp = 1000 if is_alive else 0
	enemy.atk = 100
	return enemy


func _make_stats(hp: int, atk: int, rec: int) -> RefCounted:
	var stats = RefCounted.new()
	stats.hp = hp
	stats.atk = atk
	stats.rec = rec
	return stats
