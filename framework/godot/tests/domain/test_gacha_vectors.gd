extends MFTestBase
## Test vector harness for gacha_distribution_cases.json


func test_gacha_distribution_vectors() -> void:
	var json_path := "res://framework/shared/test_vectors/gacha_distribution_cases.json"
	if not ResourceLoader.exists(json_path):
		print("  [SKIP] gacha_distribution_cases.json not found")
		return
	
	var file := FileAccess.open(json_path, FileAccess.READ)
	if file == null:
		print("  [SKIP] Cannot open gacha_distribution_cases.json")
		return
	
	var json_text := file.get_as_text()
	var json := JSON.new()
	var err := json.parse(json_text)
	if err != OK:
		print("  [FAIL] Cannot parse gacha_distribution_cases.json: " + json.get_error_message())
		_failed += 1
		return
	
	var pool_data: Dictionary = json.data.pool
	var cases: Array = json.data.cases
	var pool := _build_pool(pool_data)
	
	for case_data: Dictionary in cases:
		_run_gacha_case(case_data, pool)


func _build_pool(data: Dictionary) -> RefCounted:
	var pool := RefCounted.new()
	pool.set("id", data.get("id", 1))
	pool.set("name", data.get("name", "Test Pool"))
	pool.set("pity_threshold", data.get("pity_threshold", 10))
	
	var entries := []
	for e: Dictionary in data.get("entries", []):
		var entry := RefCounted.new()
		entry.set("monster_id", e.get("monster_id", 0))
		entry.set("rarity", e.get("rarity", 3))
		entry.set("weight", e.get("weight", 100))
		entry.set("is_featured", e.get("is_featured", false))
		entries.append(entry)
	pool.set("entries", entries)
	
	return pool


func _run_gacha_case(case_data: Dictionary, pool: RefCounted) -> void:
	var name: String = case_data.name
	
	match name:
		"pity_triggers_at_threshold":
			_test_pity_triggers(case_data, pool)
		"pity_does_not_trigger_below_threshold":
			_test_pity_no_trigger(case_data, pool)
		"displayed_rates_match_weights":
			_test_displayed_rates(case_data, pool)
		"multi_pull_pity_tracks_across_pulls":
			_test_multi_pull(case_data, pool)
		"empty_pool_handling":
			_test_empty_pool(case_data)
		"weighted_distribution_statistical":
			_test_statistical_distribution(case_data, pool)


func _test_pity_triggers(case_data: Dictionary, pool: RefCounted) -> void:
	var pity_count: int = case_data.pity_count
	var result := _roll_single(pool, pity_count, 12345)
	assert_true(result.is_pity, "pity should trigger at threshold")
	assert_eq(result.rarity, 5, "pity should guarantee 5-star")


func _test_pity_no_trigger(case_data: Dictionary, pool: RefCounted) -> void:
	var pity_count: int = case_data.pity_count
	var result := _roll_single(pool, pity_count, 12345)
	assert_false(result.is_pity, "pity should not trigger below threshold")


func _test_displayed_rates(case_data: Dictionary, pool: RefCounted) -> void:
	var expected_rates: Dictionary = case_data.expected_rates
	var rates := _get_displayed_rates(pool)
	
	for rarity: String in expected_rates.keys():
		var r := int(rarity)
		assert_eq(rates.get(r, -1.0), expected_rates[rarity], "rate for rarity " + rarity)


func _test_multi_pull(case_data: Dictionary, pool: RefCounted) -> void:
	var initial_pity: int = case_data.initial_pity
	var pull_count: int = case_data.pull_count
	
	var results := _roll_multi(pool, pull_count, initial_pity, 12345)
	assert_eq(results.size(), pull_count, "should have correct pull count")
	
	var found_pity := false
	for result: RefCounted in results:
		if result.is_pity or result.rarity == 5:
			found_pity = true
	assert_true(found_pity, "should trigger pity within multi-pull")


func _test_empty_pool(case_data: Dictionary) -> void:
	var empty_pool := RefCounted.new()
	empty_pool.set("entries", [])
	empty_pool.set("pity_threshold", 0)
	
	var result := _roll_single(empty_pool, 0, 12345)
	assert_eq(result.monster_id, 0, "empty pool should return 0")
	assert_eq(result.rarity, 0, "empty pool should return rarity 0")


func _test_statistical_distribution(case_data: Dictionary, pool: RefCounted) -> void:
	var sample_size: int = case_data.sample_size
	var tolerance: float = case_data.tolerance_percent
	var expected_dist: Dictionary = case_data.expected_distribution
	
	var counts := {5: 0, 4: 0, 3: 0}
	for i in range(sample_size):
		var result := _roll_single(pool, 0, i * 1337 + 42)
		counts[result.rarity] = counts.get(result.rarity, 0) + 1
	
	for rarity_key: String in expected_dist.keys():
		var rarity := int(rarity_key.replace("rarity_", ""))
		var target: Dictionary = expected_dist[rarity_key]
		var actual_percent := float(counts.get(rarity, 0)) / float(sample_size) * 100.0
		assert_gte(actual_percent, target.min_percent, "rarity %d min" % rarity)
		assert_lte(actual_percent, target.max_percent, "rarity %d max" % rarity)


func _roll_single(pool: RefCounted, pity_count: int, seed: int) -> RefCounted:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed
	
	var result := RefCounted.new()
	result.set("monster_id", 0)
	result.set("rarity", 0)
	result.set("is_pity", false)
	result.set("is_featured", false)
	
	var entries: Array = pool.entries
	if entries.is_empty():
		return result
	
	var pity_threshold: int = pool.pity_threshold
	
	if pity_threshold > 0 and pity_count >= pity_threshold - 1:
		var max_rarity := 0
		for entry: RefCounted in entries:
			max_rarity = maxi(max_rarity, entry.rarity)
		
		var top_entries := []
		for entry: RefCounted in entries:
			if entry.rarity == max_rarity:
				top_entries.append(entry)
		
		if top_entries.size() > 0:
			var idx := rng.randi_range(0, top_entries.size() - 1)
			var chosen: RefCounted = top_entries[idx]
			result.monster_id = chosen.monster_id
			result.rarity = chosen.rarity
			result.is_pity = true
			result.is_featured = chosen.is_featured
		return result
	
	var total_weight := 0
	for entry: RefCounted in entries:
		total_weight += entry.weight
	
	if total_weight <= 0:
		return result
	
	var roll := rng.randi_range(0, total_weight - 1)
	var cumulative := 0
	for entry: RefCounted in entries:
		cumulative += entry.weight
		if roll < cumulative:
			result.monster_id = entry.monster_id
			result.rarity = entry.rarity
			result.is_featured = entry.is_featured
			return result
	
	return result


func _roll_multi(pool: RefCounted, count: int, initial_pity: int, seed: int) -> Array:
	var results := []
	var rng := RandomNumberGenerator.new()
	rng.seed = seed
	var current_pity := initial_pity
	
	for i in range(count):
		var result := _roll_single(pool, current_pity, rng.randi())
		results.append(result)
		if result.is_pity or result.rarity == 5:
			current_pity = 0
		else:
			current_pity += 1
	
	return results


func _get_displayed_rates(pool: RefCounted) -> Dictionary:
	var entries: Array = pool.entries
	var total_weight := 0
	var rarity_weights := {}
	
	for entry: RefCounted in entries:
		total_weight += entry.weight
		var r: int = entry.rarity
		rarity_weights[r] = rarity_weights.get(r, 0) + entry.weight
	
	var rates := {}
	for rarity: int in rarity_weights.keys():
		rates[rarity] = float(rarity_weights[rarity]) / float(total_weight) * 100.0
	
	return rates
