extends MFTestBase
## Integration tests for the ToS battle flow using framework modules.

const PlayerStateScript = preload("res://addons/mobileforge/infrastructure/player_state/player_state.gd")
const EventBusScript = preload("res://addons/mobileforge/infrastructure/event_bus/event_bus.gd")

var _bus: Node
var _ps: Node
var _economy: MFEconomy
var _board: MFBoardLogic
var _combat: MFCombatResolver
var _skill_pipeline: MFSkillPipeline
var _dungeon_runner: MFDungeonRunner
var _monster_manager: MFMonsterManager


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_bus = EventBusScript.new()
	_ps = PlayerStateScript.new()
	_ps._event_bus = _bus
	_ps.register_section(&"currencies", {"gems": 50, "coins": 10000, "stamina": 100})
	_ps.register_section(&"monsters", {})

	_economy = MFEconomy.new(_ps, _bus)
	_skill_pipeline = MFSkillPipeline.new()
	_monster_manager = MFMonsterManager.new(func(def_id):
		# Minimal monster def lookup for testing
		return {
			"id": def_id,
			"name": "TestMonster_%d" % def_id,
			"element": ((def_id - 1) % 5) + 1,
			"rarity": clampi(def_id % 6 + 1, 1, 5),
			"max_level": 99,
			"base_hp": 500.0,
			"base_atk": 200.0,
			"base_rec": 100.0,
			"max_hp": 3000.0,
			"max_atk": 1200.0,
			"max_rec": 600.0,
			"cost": 10,
			"evolve_to": -1,
			"exp_curve": "standard",
		})

	# Board + combat for battle tests
	var config = MFBoardConfig.new(5, 6)
	_board = MFBoardLogic.new(config, 42)  # Fixed seed for reproducibility
	_board.init_board()
	var chart = MFElementChart.new()
	_combat = MFCombatResolver.new(chart)
	_dungeon_runner = MFDungeonRunner.new(_board, _combat, _skill_pipeline, null)


func after_each() -> void:
	if _ps != null:
		_ps.free()
		_ps = null
	if _bus != null:
		_bus.free()
		_bus = null


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _make_dungeon_def() -> RefCounted:
	return MFDungeonTypes.DungeonDef.new({
		"id": 1,
		"name": "Test Dungeon",
		"stamina_cost": 10,
		"waves": [
			{"enemies": [
				{"name": "Slime", "element": 2, "hp": 500, "atk": 100, "defense": 0, "countdown": 2},
			]},
		],
		"rewards": [{"type": "coins", "count": 100}],
	})


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_full_battle_flow() -> void:
	## Start a dungeon, auto-resolve turns until battle ends.
	var dungeon_def = _make_dungeon_def()
	_dungeon_runner.start(dungeon_def, 10000, 10000)

	assert_true(_dungeon_runner.state.is_active, "dungeon should be active after start")
	assert_eq(_dungeon_runner.state.enemies.size(), 1, "wave 1 should have 1 enemy")

	# Create a team with one water monster (advantage vs fire enemy)
	var team: Array = [_monster_manager.create_instance(1)]
	var team_stats: Array = [MFMonsterTypes.MonsterStats.new(3000, 1000, 300)]

	# Auto-resolve turns until battle ends (max 20 to prevent infinite loop)
	var turns := 0
	while _dungeon_runner.state.is_active and turns < 20:
		# Cascade the current board
		var cascade_steps = MFCascadeResolver.resolve(_board)
		if cascade_steps.is_empty():
			# No matches; re-init the board and try again
			_board.init_board()
			turns += 1
			continue
		var result = _dungeon_runner.execute_player_turn(cascade_steps, team, team_stats)
		if _dungeon_runner.state.is_active and not result.wave_cleared:
			_dungeon_runner.execute_enemy_turn()
		turns += 1

	assert_false(_dungeon_runner.state.is_active,
		"dungeon should end within 20 turns with 1000 ATK vs 500 HP enemy")
	assert_gt(turns, 0, "should have taken at least 1 turn")


func test_skill_registration() -> void:
	## Register all skills via TosSkillRegistration, verify conditions and effects.
	var pipeline = MFSkillPipeline.new()
	TosSkillRegistration.register(pipeline)

	# Conditions
	assert_true(pipeline.condition_registry.has_type("always_true"),
		"always_true condition should be registered")
	assert_true(pipeline.condition_registry.has_type("combo_above"),
		"combo_above condition should be registered")
	assert_true(pipeline.condition_registry.has_type("hp_below"),
		"hp_below condition should be registered")
	assert_true(pipeline.condition_registry.has_type("elements_matched"),
		"elements_matched condition should be registered")
	assert_true(pipeline.condition_registry.has_type("team_has_element"),
		"team_has_element condition should be registered")

	# Effects
	assert_true(pipeline.effect_registry.has_effect("area_damage"),
		"area_damage effect should be registered")
	assert_true(pipeline.effect_registry.has_effect("heal_flat"),
		"heal_flat effect should be registered")
	assert_true(pipeline.effect_registry.has_effect("heal_percent"),
		"heal_percent effect should be registered")
	assert_true(pipeline.effect_registry.has_effect("change_gem_element"),
		"change_gem_element effect should be registered")
	assert_true(pipeline.effect_registry.has_effect("delay_enemies"),
		"delay_enemies effect should be registered")


func test_gacha_pull_deducts_currency() -> void:
	## Setup economy with 50 gems, pull costs 5 -> balance becomes 45.
	assert_eq(_economy.get_balance("gems"), 50, "should start with 50 gems")

	# Create a minimal gacha pool
	var pool = MFGachaTypes.GachaPool.new({
		"id": 1,
		"name": "Test Pool",
		"cost_currency": "gems",
		"cost_amount": 5,
		"pity_threshold": 0,
		"entries": [
			{"monster_id": 1, "rarity": 3, "weight": 70},
			{"monster_id": 2, "rarity": 4, "weight": 25},
			{"monster_id": 3, "rarity": 5, "weight": 5},
		],
	})

	# Verify pool was created correctly
	assert_eq(pool.cost_amount, 5, "pool cost should be 5 gems")

	# Spend currency for 1 pull
	var can_pay := _economy.can_afford(pool.cost_currency, pool.cost_amount)
	assert_true(can_pay, "should be able to afford 5 gems with 50 balance")
	_economy.spend(pool.cost_currency, pool.cost_amount)

	assert_eq(_economy.get_balance("gems"), 45,
		"balance should be 45 after spending 5 gems on 1 pull")

	# Roll the gacha (verify it returns a valid result)
	var rng = RandomNumberGenerator.new()
	rng.seed = 12345
	var result = MFGachaRoller.roll(pool, 0, rng)
	assert_not_null(result, "gacha roll should return a result")
	assert_gt(result.monster_id, 0, "result should have a valid monster_id")
	assert_gt(result.rarity, 0, "result should have a valid rarity")


func test_board_cascade_produces_matches() -> void:
	## Init board with known seed, verify cascade returns non-empty steps.
	# Use a board with a guaranteed horizontal match: row 0 = [1,1,1, 2,3,4]
	var config = MFBoardConfig.new(5, 6)
	var board = MFBoardLogic.new(config, 99)
	board.init_board()

	# Force a 3-match in row 0
	var elements: Array[int] = board.to_element_array()
	elements[0] = 1  # Water
	elements[1] = 1  # Water
	elements[2] = 1  # Water
	board.from_element_array(elements)

	var cascade_steps = MFCascadeResolver.resolve(board)
	assert_true(not cascade_steps.is_empty(),
		"cascade should find the forced 3-match in row 0")

	# Check the first step has at least one match
	var first_step = cascade_steps[0]
	assert_gt(first_step.matches.size(), 0,
		"first cascade step should contain at least 1 match")

	# Verify the forced match is for element 1 (water)
	var found_water := false
	for match_result in first_step.matches:
		if match_result.element == 1:
			found_water = true
			break
	assert_true(found_water, "cascade should include a water element match")


func test_element_advantage_in_combat() -> void:
	## Water attacker vs Fire enemy -> damage multiplier is 1.5x.
	var chart = MFElementChart.new()
	var combat = MFCombatResolver.new(chart)

	# Verify element multipliers directly
	var water_vs_fire := chart.get_multiplier(1, 2)  # Water(1) vs Fire(2)
	assert_eq(water_vs_fire, 1.5, "water vs fire should be 1.5x")

	var fire_vs_water := chart.get_multiplier(2, 1)  # Fire(2) vs Water(1)
	assert_eq(fire_vs_water, 0.5, "fire vs water should be 0.5x")

	# Test through the combat resolver pipeline
	# Water attacker, ATK=1000, 3 gems matched, 1 combo, vs Fire enemy with 0 defense
	var ctx_adv = MFCombatTypes.DamageContext.new()
	ctx_adv.attacker_element = 1  # Water
	ctx_adv.defender_element = 2  # Fire
	ctx_adv.attacker_atk = 1000.0
	ctx_adv.gems_matched = 3
	ctx_adv.combo_count = 1
	ctx_adv.defender_defense = 0.0

	var result_adv = combat.resolve_player_attack(ctx_adv)

	# Same setup but neutral elements (Water vs Water)
	var ctx_neutral = MFCombatTypes.DamageContext.new()
	ctx_neutral.attacker_element = 1  # Water
	ctx_neutral.defender_element = 1  # Water (neutral)
	ctx_neutral.attacker_atk = 1000.0
	ctx_neutral.gems_matched = 3
	ctx_neutral.combo_count = 1
	ctx_neutral.defender_defense = 0.0

	var result_neutral = combat.resolve_player_attack(ctx_neutral)

	# Advantaged damage should be 1.5x the neutral damage
	assert_eq(result_adv.element_multiplier, 1.5,
		"element multiplier should be 1.5 for water vs fire")
	assert_eq(result_neutral.element_multiplier, 1.0,
		"element multiplier should be 1.0 for neutral matchup")
	assert_eq(result_adv.final_damage, int(result_neutral.final_damage * 1.5),
		"advantaged damage should be 1.5x neutral damage")
