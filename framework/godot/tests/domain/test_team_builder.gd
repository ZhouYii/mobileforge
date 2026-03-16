extends MFTestBase
## Tests for TeamBuilder — party composition validation and stats aggregation.
##
## Since TeamBuilder does not yet exist as a source file, these tests define
## the expected API contract (test-first development).

const MonsterTypesScript = preload("res://addons/mobileforge/domain/monster/monster_types.gd")
const MonsterManagerScript = preload("res://addons/mobileforge/domain/monster/monster_manager.gd")

var _manager: MFMonsterManager
var _builder: RefCounted  # MFTeamBuilder


# ---------------------------------------------------------------------------
# Test monster definitions
# ---------------------------------------------------------------------------

var _test_defs: Dictionary = {
	1: {
		"id": 1, "name": "Water Dragon", "element": 1, "rarity": 5,
		"max_level": 99,
		"base_hp": 500.0, "base_atk": 200.0, "base_rec": 100.0,
		"max_hp": 3000.0, "max_atk": 1500.0, "max_rec": 400.0,
		"cost": 25, "active_skill_id": 101, "leader_skill_id": 201,
		"team_skill_ids": [], "evolve_to": -1, "evolve_materials": [],
		"exp_curve": "standard",
	},
	2: {
		"id": 2, "name": "Water Golem", "element": 1, "rarity": 3,
		"max_level": 50,
		"base_hp": 800.0, "base_atk": 100.0, "base_rec": 50.0,
		"max_hp": 4000.0, "max_atk": 600.0, "max_rec": 200.0,
		"cost": 15, "active_skill_id": -1, "leader_skill_id": -1,
		"team_skill_ids": [], "evolve_to": -1, "evolve_materials": [],
		"exp_curve": "standard",
	},
	3: {
		"id": 3, "name": "Fire Phoenix", "element": 2, "rarity": 5,
		"max_level": 99,
		"base_hp": 400.0, "base_atk": 300.0, "base_rec": 80.0,
		"max_hp": 2500.0, "max_atk": 2000.0, "max_rec": 350.0,
		"cost": 30, "active_skill_id": 102, "leader_skill_id": 202,
		"team_skill_ids": [], "evolve_to": -1, "evolve_materials": [],
		"exp_curve": "standard",
	},
}


func _mock_lookup(def_id: int) -> Variant:
	if _test_defs.has(def_id):
		return _test_defs[def_id]
	return null


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_manager = MFMonsterManager.new(Callable(self, "_mock_lookup"))
	# MFTeamBuilder constructor: (max_slots: int, manager: MFMonsterManager)
	_builder = MFTeamBuilder.new(6, _manager)


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_set_slot() -> void:
	var inst = _manager.create_instance(1)
	var ok = _builder.set_slot(0, inst)
	assert_true(ok, "set_slot(0) should succeed")
	var team = _builder.get_team()
	assert_not_null(team.get_slot(0), "slot 0 should be populated")
	assert_eq(team.get_slot(0).def_id, 1, "slot 0 def_id should match")


func test_set_slot_out_of_range() -> void:
	var inst = _manager.create_instance(1)
	var ok = _builder.set_slot(-1, inst)
	assert_false(ok, "set_slot(-1) should fail")
	var ok2 = _builder.set_slot(6, inst)
	assert_false(ok2, "set_slot(6) should fail for max_slots=6")


func test_clear_slot() -> void:
	var inst = _manager.create_instance(1)
	_builder.set_slot(0, inst)
	_builder.clear_slot(0)
	var team = _builder.get_team()
	assert_null(team.get_slot(0), "slot 0 should be null after clear")


func test_validate_empty_team() -> void:
	var errors = _builder.validate()
	assert_gt(errors.size(), 0, "empty team should produce errors")
	# Check that one of the errors mentions needing at least 1 member
	var found_empty_error := false
	for e in errors:
		if "at least 1" in e.to_lower() or "empty" in e.to_lower():
			found_empty_error = true
			break
	assert_true(found_empty_error,
		"errors should mention needing at least 1 member")


func test_validate_valid_team() -> void:
	var inst = _manager.create_instance(1)
	_builder.set_slot(0, inst)
	var errors = _builder.validate()
	assert_eq(errors.size(), 0, "team with 1 monster should have no errors")


func test_validate_duplicate_monster() -> void:
	var inst = _manager.create_instance(1)
	_builder.set_slot(0, inst)
	_builder.set_slot(1, inst)  # same instance in 2 slots
	var errors = _builder.validate()
	assert_gt(errors.size(), 0, "duplicate monster should produce errors")
	var found_dup_error := false
	for e in errors:
		if "duplicate" in e.to_lower():
			found_dup_error = true
			break
	assert_true(found_dup_error, "errors should mention duplicates")


func test_get_team_stats() -> void:
	# Place 2 monsters and verify aggregated stats
	var inst_a = _manager.create_instance(1)  # Water Dragon: base hp=500, atk=200, rec=100
	var inst_b = _manager.create_instance(2)  # Water Golem: base hp=800, atk=100, rec=50
	_builder.set_slot(0, inst_a)
	_builder.set_slot(1, inst_b)

	var stats = _builder.get_team_stats()
	# At level 1, stats = base stats
	assert_eq(stats.total_hp, 500 + 800, "total_hp should be sum of base HPs")
	assert_eq(stats.total_atk, 200 + 100, "total_atk should be sum of base ATKs")
	assert_eq(stats.total_rec, 100 + 50, "total_rec should be sum of base RECs")


func test_team_skill_checker_all_same_element() -> void:
	# 2 water monsters -> all_same_element should be true
	var inst_a = _manager.create_instance(1)  # Water, element=1
	var inst_b = _manager.create_instance(2)  # Water, element=1
	_builder.set_slot(0, inst_a)
	_builder.set_slot(1, inst_b)

	assert_true(_builder.all_same_element(),
		"2 water monsters should satisfy all_same_element")

	# Add a fire monster -> should break the check
	var inst_c = _manager.create_instance(3)  # Fire, element=2
	_builder.set_slot(2, inst_c)
	assert_false(_builder.all_same_element(),
		"mixed elements should not satisfy all_same_element")
