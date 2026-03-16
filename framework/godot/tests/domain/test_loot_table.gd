extends MFTestBase
## Tests for LootTable (domain/loot/loot_table.gd)
## Verifies guaranteed drops, non-guaranteed rolling, count ranges, and empty tables.

const LootTypesScript = preload("res://addons/mobileforge/domain/loot/loot_types.gd")
const LootTableScript = preload("res://addons/mobileforge/domain/loot/loot_table.gd")

var _rng: RandomNumberGenerator


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_rng = RandomNumberGenerator.new()
	_rng.seed = 54321


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _make_table_with_guaranteed() -> RefCounted:
	return MFLootTypes.LootTableDef.new({
		"id": 1,
		"entries": [
			{"type": "currency", "item_id": 1, "count_min": 10, "count_max": 10,
			 "weight": 0, "guaranteed": true},
			{"type": "item", "item_id": 2, "count_min": 1, "count_max": 1,
			 "weight": 50, "guaranteed": false},
			{"type": "item", "item_id": 3, "count_min": 1, "count_max": 1,
			 "weight": 50, "guaranteed": false},
		],
	})


func _make_non_guaranteed_only_table() -> RefCounted:
	return MFLootTypes.LootTableDef.new({
		"id": 2,
		"entries": [
			{"type": "item", "item_id": 10, "count_min": 1, "count_max": 1,
			 "weight": 50, "guaranteed": false},
			{"type": "item", "item_id": 11, "count_min": 1, "count_max": 1,
			 "weight": 50, "guaranteed": false},
		],
	})


func _make_count_range_table() -> RefCounted:
	return MFLootTypes.LootTableDef.new({
		"id": 3,
		"entries": [
			{"type": "currency", "item_id": 1, "count_min": 1, "count_max": 5,
			 "weight": 0, "guaranteed": true},
		],
	})


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_guaranteed_drops_always_present() -> void:
	var table = _make_table_with_guaranteed()
	# Roll 20 times and check guaranteed drop is always present
	for i in range(20):
		var drops = MFLootTable.roll_drops(table, _rng, 0)
		# With roll_count=0, only guaranteed drops should appear
		var found_guaranteed := false
		for drop in drops:
			if drop.item_id == 1 and drop.type == "currency":
				found_guaranteed = true
				break
		assert_true(found_guaranteed,
			"guaranteed drop (item_id=1) should always be present")


func test_non_guaranteed_may_not_drop() -> void:
	var table = _make_non_guaranteed_only_table()
	# With roll_count=0, no non-guaranteed drops should appear
	var drops = MFLootTable.roll_drops(table, _rng, 0)
	assert_eq(drops.size(), 0,
		"non-guaranteed table with roll_count=0 should return empty")


func test_roll_count_affects_drops() -> void:
	var table = _make_table_with_guaranteed()
	# roll_count=3 should give guaranteed drops + up to 3 non-guaranteed drops
	var drops = MFLootTable.roll_drops(table, _rng, 3)
	# Should have at least 1 guaranteed drop
	assert_gte(drops.size(), 1, "should have at least the guaranteed drop")
	# Should have at most 1 guaranteed + 3 non-guaranteed = 4
	assert_true(drops.size() <= 4,
		"should have at most 4 drops (1 guaranteed + 3 non-guaranteed), got %d" % drops.size())
	# Verify the guaranteed one is present
	var has_guaranteed := false
	for drop in drops:
		if drop.item_id == 1:
			has_guaranteed = true
			break
	assert_true(has_guaranteed, "guaranteed drop should be present with roll_count=3")


func test_count_range() -> void:
	var table = _make_count_range_table()
	# Roll many times, check count is always in [1, 5]
	for i in range(50):
		var drops = MFLootTable.roll_drops(table, _rng, 0)
		assert_eq(drops.size(), 1, "should have exactly 1 guaranteed drop")
		var drop = drops[0]
		assert_gte(drop.count, 1, "drop count should be >= 1")
		assert_true(drop.count <= 5,
			"drop count should be <= 5, got %d" % drop.count)


func test_empty_table_returns_empty() -> void:
	var empty_table = MFLootTypes.LootTableDef.new({"id": 99, "entries": []})
	var drops = MFLootTable.roll_drops(empty_table, _rng, 5)
	assert_eq(drops.size(), 0, "empty table should return no drops")
