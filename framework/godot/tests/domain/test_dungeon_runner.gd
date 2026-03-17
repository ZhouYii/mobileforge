extends MFTestBase
## Tests for DungeonRunner — the main dungeon orchestrator.
## DungeonRunner wires together BoardLogic, CombatResolver, SkillPipeline, EnemyAI.
##
## Since DungeonRunner does not yet exist as a source file, these tests define
## the expected API contract (test-first development).

const DungeonTypesScript = preload("res://addons/mobileforge/domain/dungeon/dungeon_types.gd")
const EnemyTypesScript = preload("res://addons/mobileforge/domain/enemy/enemy_types.gd")
const EnemyAIScript = preload("res://addons/mobileforge/domain/enemy/enemy_ai.gd")
const BoardConfigScript = preload("res://addons/mobileforge/domain/board/board_config.gd")
const BoardLogicScript = preload("res://addons/mobileforge/domain/board/board_logic.gd")
const ElementChartScript = preload("res://addons/mobileforge/domain/combat/element_chart.gd")
const CombatResolverScript = preload("res://addons/mobileforge/domain/combat/combat_resolver.gd")
const SkillPipelineScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_pipeline.gd")

var _runner: RefCounted  # MFDungeonRunner
var _board: MFBoardLogic
var _combat: MFCombatResolver
var _skill_pipeline: MFSkillPipeline
var _dungeon_def: RefCounted  # DungeonDef
var _events: Array[Dictionary]  # captured events


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _capture_event(event_name: String, data: Dictionary) -> void:
	_events.append({"event": event_name, "data": data})


func _make_dungeon_def() -> RefCounted:
	# 2 waves: wave 0 has 2 weak enemies, wave 1 has 1 stronger enemy
	return MFDungeonTypes.DungeonDef.new({
		"id": 100,
		"name": "Test Dungeon",
		"stamina_cost": 10,
		"waves": [
			{
				"enemies": [
					{"id": 0, "name": "Slime A", "element": 1, "hp": 100, "atk": 10.0, "defense": 0.0, "countdown": 2},
					{"id": 1, "name": "Slime B", "element": 2, "hp": 100, "atk": 15.0, "defense": 0.0, "countdown": 3},
				]
			},
			{
				"enemies": [
					{"id": 2, "name": "Boss", "element": 3, "hp": 500, "atk": 50.0, "defense": 10.0, "countdown": 1},
				]
			},
		],
		"rewards": [{"type": "gold", "id": 0, "count": 100}],
	})


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_events = []
	var config = MFBoardConfig.new()
	_board = MFBoardLogic.new(config)
	_combat = MFCombatResolver.new(MFElementChart.new())
	_skill_pipeline = MFSkillPipeline.new()
	_dungeon_def = _make_dungeon_def()

	# MFDungeonRunner constructor: (board, combat, skill_pipeline, event_callback)
	_runner = MFDungeonRunner.new(_board, _combat, _skill_pipeline,
		Callable(self, "_capture_event"))


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_start_initializes_state() -> void:
	_runner.start_dungeon(_dungeon_def, 5000)  # team_hp = 5000
	var state = _runner.get_dungeon_state()
	assert_true(state.is_active, "state should be active after start")
	assert_eq(state.current_wave_index, 0, "should start at wave 0")
	assert_eq(state.team_hp, 5000, "team_hp should match")


func test_start_loads_enemies() -> void:
	_runner.start_dungeon(_dungeon_def, 5000)
	var state = _runner.get_dungeon_state()
	assert_eq(state.enemies.size(), 2, "wave 0 should have 2 enemies")
	assert_eq(state.enemies[0].name, "Slime A", "first enemy name should match")
	assert_eq(state.enemies[1].name, "Slime B", "second enemy name should match")


func test_execute_player_turn_deals_damage() -> void:
	_runner.start_dungeon(_dungeon_def, 5000)
	# Provide cascade_steps that simulate matched gems
	var cascade_steps = [
		{"matches": [
			{"element": 1, "positions": [0, 1, 2], "gem_count": 3}
		]}
	]
	var result = _runner.execute_player_turn(cascade_steps, 1, 1)
	# At least one enemy should have taken some damage
	var total_damage := 0
	for enemy_id in result.damage_per_enemy:
		total_damage += result.damage_per_enemy[enemy_id]
	assert_gt(total_damage, 0, "cascade with matches should deal damage")


func test_execute_player_turn_kills_enemy() -> void:
	_runner.start_dungeon(_dungeon_def, 5000)
	# Massive cascade to guarantee kill
	var cascade_steps = [
		{"matches": [
			{"element": 1, "positions": [0, 1, 2, 3, 4, 5], "gem_count": 6}
		]},
		{"matches": [
			{"element": 1, "positions": [6, 7, 8], "gem_count": 3}
		]},
		{"matches": [
			{"element": 2, "positions": [9, 10, 11], "gem_count": 3}
		]},
	]
	# Force enemies to low HP so they die
	var state = _runner.get_dungeon_state()
	state.enemies[0].hp = 1
	var result = _runner.execute_player_turn(cascade_steps, 3, 1)
	assert_gt(result.enemies_killed.size(), 0, "at least one enemy should be killed")


func test_execute_player_turn_wave_clear() -> void:
	_runner.start_dungeon(_dungeon_def, 5000)
	# Kill all enemies in wave 0
	var state = _runner.get_dungeon_state()
	for enemy in state.enemies:
		enemy.hp = 1

	var cascade_steps = [
		{"matches": [
			{"element": 1, "positions": [0, 1, 2, 3, 4, 5], "gem_count": 6}
		]},
		{"matches": [
			{"element": 2, "positions": [6, 7, 8], "gem_count": 3}
		]},
	]
	var result = _runner.execute_player_turn(cascade_steps, 2, 1)
	assert_true(result.wave_cleared, "all enemies dead -> wave should be cleared")


func test_execute_enemy_turn_deals_damage() -> void:
	_runner.start_dungeon(_dungeon_def, 5000)
	var state = _runner.get_dungeon_state()
	var initial_hp := state.team_hp

	# Set an enemy countdown to 0 so it attacks
	state.enemies[0].countdown = 0
	var result = _runner.execute_enemy_turn()
	assert_gt(initial_hp, state.team_hp, "team_hp should decrease after enemy attack")
	assert_gt(result.enemy_attacks.size(), 0, "there should be at least one attack record")


func test_battle_won_after_all_waves() -> void:
	_runner.start_dungeon(_dungeon_def, 5000)
	var state = _runner.get_dungeon_state()

	# Clear wave 0: kill all enemies
	for enemy in state.enemies:
		enemy.hp = 0
	_runner.advance_wave()

	# Now on wave 1: kill boss
	state = _runner.get_dungeon_state()
	for enemy in state.enemies:
		enemy.hp = 0
	_runner.advance_wave()

	state = _runner.get_dungeon_state()
	assert_true(state.battle_won if "battle_won" in state else not state.is_active,
		"after clearing all waves, battle should be won")


func test_battle_lost_on_zero_hp() -> void:
	_runner.start_dungeon(_dungeon_def, 100)
	var state = _runner.get_dungeon_state()

	# Set enemy countdown to 0, give it massive ATK
	state.enemies[0].countdown = 0
	state.enemies[0].atk = 99999.0
	_runner.execute_enemy_turn()

	state = _runner.get_dungeon_state()
	assert_eq(state.team_hp, 0, "team_hp should be 0 after massive damage")
	assert_false(state.is_active, "battle should be over when team_hp reaches 0")
