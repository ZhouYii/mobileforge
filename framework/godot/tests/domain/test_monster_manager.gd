extends MFTestBase
## Tests for MonsterManager (domain/monster/monster_manager.gd)

const MonsterTypesScript = preload("res://addons/mobileforge/domain/monster/monster_types.gd")
const StatCalculatorScript = preload("res://addons/mobileforge/domain/monster/stat_calculator.gd")
const MonsterManagerScript = preload("res://addons/mobileforge/domain/monster/monster_manager.gd")

var _manager: MFMonsterManager


# ---------------------------------------------------------------------------
# Test monster definitions
# ---------------------------------------------------------------------------

## Test MonsterDef data keyed by def_id.
var _test_defs: Dictionary = {
	1: {
		"id": 1,
		"name": "Water Dragon",
		"element": 1,
		"rarity": 5,
		"max_level": 99,
		"base_hp": 500.0,
		"base_atk": 200.0,
		"base_rec": 100.0,
		"max_hp": 3000.0,
		"max_atk": 1500.0,
		"max_rec": 400.0,
		"cost": 25,
		"active_skill_id": 101,
		"leader_skill_id": 201,
		"team_skill_ids": [],
		"evolve_to": 2,
		"evolve_materials": [50, 51],
		"exp_curve": "standard",
	},
	2: {
		"id": 2,
		"name": "Water Dragon Evolved",
		"element": 1,
		"rarity": 6,
		"max_level": 99,
		"base_hp": 1000.0,
		"base_atk": 500.0,
		"base_rec": 200.0,
		"max_hp": 5000.0,
		"max_atk": 2500.0,
		"max_rec": 800.0,
		"cost": 50,
		"active_skill_id": 102,
		"leader_skill_id": 202,
		"team_skill_ids": [],
		"evolve_to": -1,
		"evolve_materials": [],
		"exp_curve": "standard",
	},
	3: {
		"id": 3,
		"name": "Fire Slime",
		"element": 2,
		"rarity": 1,
		"max_level": 10,
		"base_hp": 100.0,
		"base_atk": 50.0,
		"base_rec": 20.0,
		"max_hp": 500.0,
		"max_atk": 200.0,
		"max_rec": 80.0,
		"cost": 5,
		"active_skill_id": -1,
		"leader_skill_id": -1,
		"team_skill_ids": [],
		"evolve_to": -1,
		"evolve_materials": [],
		"exp_curve": "fast",
	},
	4: {
		"id": 4,
		"name": "Slow Growth Monster",
		"element": 3,
		"rarity": 3,
		"max_level": 50,
		"base_hp": 300.0,
		"base_atk": 100.0,
		"base_rec": 50.0,
		"max_hp": 2000.0,
		"max_atk": 800.0,
		"max_rec": 300.0,
		"cost": 15,
		"active_skill_id": -1,
		"leader_skill_id": -1,
		"team_skill_ids": [],
		"evolve_to": -1,
		"evolve_materials": [],
		"exp_curve": "slow",
	},
}


## Mock lookup callable that returns test MonsterDef data.
func _mock_lookup(def_id: int) -> Variant:
	if _test_defs.has(def_id):
		return _test_defs[def_id]
	return null


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_manager = MFMonsterManager.new(Callable(self, "_mock_lookup"))


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_create_instance() -> void:
	var inst = _manager.create_instance(1)
	assert_not_null(inst, "instance should not be null")
	assert_eq(inst.def_id, 1, "instance def_id should match")
	assert_eq(inst.level, 1, "new instance should be level 1")
	assert_eq(inst.exp, 0, "new instance should have 0 exp")


func test_get_stats_level_1() -> void:
	var inst = _manager.create_instance(1)
	var stats = _manager.get_stats(inst)
	assert_not_null(stats, "stats should not be null")
	# At level 1, stats should equal base stats
	assert_eq(stats.hp, int(_test_defs[1]["base_hp"]), "hp at level 1 should equal base_hp")
	assert_eq(stats.atk, int(_test_defs[1]["base_atk"]), "atk at level 1 should equal base_atk")
	assert_eq(stats.rec, int(_test_defs[1]["base_rec"]), "rec at level 1 should equal base_rec")


func test_get_stats_max_level() -> void:
	var inst = _manager.create_instance(1)
	inst.level = 99  # max level for def_id 1
	var stats = _manager.get_stats(inst)
	assert_not_null(stats, "stats should not be null")
	# At max level, stats should equal max stats
	assert_eq(stats.hp, int(_test_defs[1]["max_hp"]), "hp at max level should equal max_hp")
	assert_eq(stats.atk, int(_test_defs[1]["max_atk"]), "atk at max level should equal max_atk")
	assert_eq(stats.rec, int(_test_defs[1]["max_rec"]), "rec at max level should equal max_rec")


func test_add_exp_levels_up() -> void:
	var inst = _manager.create_instance(1)
	assert_eq(inst.level, 1, "should start at level 1")
	# For standard curve: exp_for_level(1) = 100 * 1 * 1.0 = 100
	# Adding 100 exp should level up to 2
	var levels := _manager.add_exp(inst, 100)
	assert_gt(levels, 0, "should gain at least 1 level")
	assert_gt(inst.level, 1, "level should increase above 1")


func test_fuse_adds_exp() -> void:
	var base = _manager.create_instance(1)
	var fodder = _manager.create_instance(3)  # Fire Slime, rarity 1
	var initial_level := base.level

	var exp_gained := _manager.fuse(base, fodder)
	# Exp from fusion = fodder.level * fodder_def.rarity * 50 = 1 * 1 * 50 = 50
	assert_eq(exp_gained, 50, "fuse should give 50 exp for level 1 rarity 1 fodder")
	assert_gte(base.level, initial_level, "base level should not decrease after fuse")


func test_can_evolve() -> void:
	var inst = _manager.create_instance(1)
	inst.level = 99  # max level
	assert_true(_manager.can_evolve(inst), "monster at max level with evolve_to should be able to evolve")


func test_cannot_evolve_low_level() -> void:
	var inst = _manager.create_instance(1)
	# Level 1, below max level 99
	assert_false(_manager.can_evolve(inst), "monster below max level should not be able to evolve")


func test_stat_curves() -> void:
	# Create two monsters at the same intermediate level but with different curves
	# def_id 3 has "fast" curve (0.7), def_id 4 has "slow" curve (1.5)
	var fast_inst = _manager.create_instance(3)
	fast_inst.level = 5

	var slow_inst = _manager.create_instance(4)
	slow_inst.level = 5

	var fast_stats = _manager.get_stats(fast_inst)
	var slow_stats = _manager.get_stats(slow_inst)

	# At an intermediate level, fast curve monster should have relatively higher stats
	# compared to their own base-to-max range than the slow curve monster.
	# Fast curve (exp=0.7): at level 5/10, ratio = (4/9)^0.7 ≈ 0.525
	# Slow curve (exp=1.5): at level 5/50, ratio = (4/49)^1.5 ≈ 0.023
	# Fast stat at level 5: int(100 + (500 - 100) * 0.525) = int(100 + 210) = 310
	# Slow stat at level 5: int(300 + (2000 - 300) * 0.023) = int(300 + 39.1) = 339
	# They have different base/max ranges so direct comparison is tricky,
	# but we can verify each is between base and max (exclusive at intermediate level)
	assert_gt(fast_stats.hp, int(_test_defs[3]["base_hp"]),
		"fast curve monster hp should be above base at level 5")
	assert_gt(int(_test_defs[3]["max_hp"]), fast_stats.hp,
		"fast curve monster hp should be below max at level 5")
	assert_gt(slow_stats.hp, int(_test_defs[4]["base_hp"]),
		"slow curve monster hp should be above base at level 5")
	assert_gt(int(_test_defs[4]["max_hp"]), slow_stats.hp,
		"slow curve monster hp should be below max at level 5")

	# Verify the curves produce different ratios by computing the normalized progress
	var fast_hp_ratio: float = float(fast_stats.hp - int(_test_defs[3]["base_hp"])) / float(int(_test_defs[3]["max_hp"]) - int(_test_defs[3]["base_hp"]))
	var slow_hp_ratio: float = float(slow_stats.hp - int(_test_defs[4]["base_hp"])) / float(int(_test_defs[4]["max_hp"]) - int(_test_defs[4]["base_hp"]))
	# Fast curve at level 5/10 should have a higher ratio than slow curve at level 5/50
	assert_gt(fast_hp_ratio, slow_hp_ratio,
		"fast curve should produce higher stat ratio than slow curve at same level")
