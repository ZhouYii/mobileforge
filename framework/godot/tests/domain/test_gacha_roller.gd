extends MFTestBase
## Tests for GachaRoller and PityTracker (domain/gacha/)
## Verifies weighted rolling, pity system, displayed rates, and pity tracker state.

const GachaTypesScript = preload("res://addons/mobileforge/domain/gacha/gacha_types.gd")
const GachaRollerScript = preload("res://addons/mobileforge/domain/gacha/gacha_roller.gd")
const PityTrackerScript = preload("res://addons/mobileforge/domain/gacha/pity_tracker.gd")

var _pool: RefCounted
var _rng: RandomNumberGenerator


# ---------------------------------------------------------------------------
# Test data: 3 entries — rarity 5 (weight 5), rarity 4 (weight 20), rarity 3 (weight 75)
# Pity threshold = 10
# ---------------------------------------------------------------------------

func _make_test_pool() -> RefCounted:
	return MFGachaTypes.GachaPool.new({
		"id": 1,
		"name": "Test Banner",
		"cost_currency": "gems",
		"cost_amount": 5,
		"pity_threshold": 10,
		"featured_ids": [1001],
		"entries": [
			{"monster_id": 1001, "rarity": 5, "weight": 5, "is_featured": true},
			{"monster_id": 2001, "rarity": 4, "weight": 20, "is_featured": false},
			{"monster_id": 3001, "rarity": 3, "weight": 75, "is_featured": false},
		],
	})


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_pool = _make_test_pool()
	_rng = RandomNumberGenerator.new()
	_rng.seed = 12345


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_roll_returns_valid_result() -> void:
	var result = MFGachaRoller.roll(_pool, 0, _rng)
	assert_not_null(result, "roll should return a result")
	assert_true(result.monster_id > 0, "result should have a valid monster_id")
	assert_true(result.rarity >= 3 and result.rarity <= 5,
		"result rarity should be between 3 and 5")


func test_roll_multi_returns_correct_count() -> void:
	var results = MFGachaRoller.roll_multi(_pool, 10, 0, _rng)
	assert_eq(results.size(), 10, "roll_multi(10) should return 10 results")
	for r in results:
		assert_true(r.monster_id > 0, "each result should have a valid monster_id")


func test_pity_triggers_top_rarity() -> void:
	# Set pity_count = threshold - 1 = 9 so that the next roll triggers pity
	var result = MFGachaRoller.roll(_pool, 9, _rng)
	assert_true(result.is_pity, "roll at pity threshold should set is_pity = true")
	assert_eq(result.rarity, 5, "pity roll should return top rarity (5)")
	assert_eq(result.monster_id, 1001, "pity roll should return a top-rarity monster")


func test_pity_resets_after_top_rarity() -> void:
	# Start at pity_count = 8. After the first pull (pity_count becomes 9),
	# pity triggers. After that, pity should reset to 0.
	# We do roll_multi with 12 pulls starting at pity_count = 8.
	var results = MFGachaRoller.roll_multi(_pool, 12, 8, _rng)
	assert_eq(results.size(), 12, "should have 12 results")
	# The first pull is at pity_count=8, second at 9 (triggers pity)
	assert_true(results[1].is_pity or results[1].rarity == 5,
		"second result should be pity or top rarity")
	# After pity resets, subsequent pulls should not all be pity
	var pity_count_after := 0
	for i in range(2, results.size()):
		if not results[i].is_pity:
			pity_count_after += 1
	assert_gt(pity_count_after, 0,
		"after pity reset, not all subsequent pulls should be pity")


func test_weighted_distribution() -> void:
	# Roll 1000 times, verify high-weight entries appear more often
	var rarity_counts := {3: 0, 4: 0, 5: 0}
	for i in range(1000):
		var result = MFGachaRoller.roll(_pool, 0, _rng)
		rarity_counts[result.rarity] += 1

	# Rarity 3 has weight 75 (75%), rarity 4 has 20 (20%), rarity 5 has 5 (5%)
	# With 1000 rolls, we expect rarity 3 to appear most often
	assert_gt(rarity_counts[3], rarity_counts[4],
		"rarity 3 (weight 75) should appear more than rarity 4 (weight 20)")
	assert_gt(rarity_counts[4], rarity_counts[5],
		"rarity 4 (weight 20) should appear more than rarity 5 (weight 5)")
	# Sanity check: rarity 3 should be at least 50% of total
	assert_gt(rarity_counts[3], 500,
		"rarity 3 should account for more than 50% of 1000 rolls")


func test_get_displayed_rates() -> void:
	var rates = MFGachaRoller.get_displayed_rates(_pool)
	# Total weight = 5 + 20 + 75 = 100
	# Rates should sum to ~100%
	var total_rate := 0.0
	for rarity in rates:
		total_rate += rates[rarity]
	assert_true(absf(total_rate - 100.0) < 0.01,
		"displayed rates should sum to ~100%, got %f" % total_rate)
	# Verify individual rates
	assert_true(absf(rates[5] - 5.0) < 0.01, "rarity 5 rate should be ~5%")
	assert_true(absf(rates[4] - 20.0) < 0.01, "rarity 4 rate should be ~20%")
	assert_true(absf(rates[3] - 75.0) < 0.01, "rarity 3 rate should be ~75%")


func test_pity_tracker_increment_and_reset() -> void:
	var tracker = MFPityTracker.new()
	assert_eq(tracker.get_pity(1), 0, "initial pity should be 0")
	tracker.increment(1)
	tracker.increment(1)
	tracker.increment(1)
	assert_eq(tracker.get_pity(1), 3, "pity should be 3 after 3 increments")
	tracker.reset(1)
	assert_eq(tracker.get_pity(1), 0, "pity should be 0 after reset")


func test_pity_tracker_serialization() -> void:
	var tracker = MFPityTracker.new()
	tracker.increment(1)
	tracker.increment(1)
	tracker.increment(2)
	tracker.increment(2)
	tracker.increment(2)

	# Serialize
	var data = tracker.to_dict()
	assert_eq(data[1], 2, "pool 1 should have pity 2 in serialized data")
	assert_eq(data[2], 3, "pool 2 should have pity 3 in serialized data")

	# Deserialize into a new tracker
	var tracker2 = MFPityTracker.new()
	tracker2.from_dict(data)
	assert_eq(tracker2.get_pity(1), 2, "restored pity for pool 1 should be 2")
	assert_eq(tracker2.get_pity(2), 3, "restored pity for pool 2 should be 3")
	assert_eq(tracker2.get_pity(99), 0, "unset pool should still return 0")
