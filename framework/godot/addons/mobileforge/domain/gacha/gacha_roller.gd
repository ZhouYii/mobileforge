class_name MFGachaRoller extends RefCounted
## Pure function gacha roller. NO state, NO deps.
## All randomness comes from the passed-in RNG.


## Roll a single pull from a gacha pool.
## pity_count: how many pulls since last top-rarity result
static func roll(pool: RefCounted, pity_count: int, rng: RandomNumberGenerator) -> RefCounted:
	# Check pity
	if pool.pity_threshold > 0 and pity_count >= pool.pity_threshold - 1:
		return _roll_top_rarity(pool, rng, true)

	# Weighted random from all entries
	var total_weight := 0
	for entry in pool.entries:
		total_weight += entry.weight

	if total_weight <= 0:
		return MFGachaTypes.GachaResult.new()

	var roll_value := rng.randi() % total_weight
	var cumulative := 0
	for entry in pool.entries:
		cumulative += entry.weight
		if roll_value < cumulative:
			return MFGachaTypes.GachaResult.new(
				entry.monster_id,
				entry.rarity,
				false,
				entry.is_featured
			)

	# Fallback (should not reach)
	var last = pool.entries[-1]
	return MFGachaTypes.GachaResult.new(last.monster_id, last.rarity, false, last.is_featured)


## Roll multiple pulls
static func roll_multi(pool: RefCounted, count: int, pity_count: int, rng: RandomNumberGenerator) -> Array:
	var results: Array = []
	var current_pity := pity_count
	for i in range(count):
		var result = roll(pool, current_pity, rng)
		results.append(result)
		if result.is_pity or _is_top_rarity(pool, result.rarity):
			current_pity = 0
		else:
			current_pity += 1
	return results


## Get displayed rates for a pool (for UI)
static func get_displayed_rates(pool: RefCounted) -> Dictionary:
	var total_weight := 0
	var rarity_weights: Dictionary = {}
	for entry in pool.entries:
		total_weight += entry.weight
		if not rarity_weights.has(entry.rarity):
			rarity_weights[entry.rarity] = 0
		rarity_weights[entry.rarity] += entry.weight

	var rates: Dictionary = {}
	for rarity in rarity_weights:
		rates[rarity] = float(rarity_weights[rarity]) / float(total_weight) * 100.0
	return rates


static func _roll_top_rarity(pool: RefCounted, rng: RandomNumberGenerator, is_pity: bool) -> RefCounted:
	var max_rarity := 0
	for entry in pool.entries:
		if entry.rarity > max_rarity:
			max_rarity = entry.rarity

	var top_entries: Array = []
	var top_weight := 0
	for entry in pool.entries:
		if entry.rarity == max_rarity:
			top_entries.append(entry)
			top_weight += entry.weight

	if top_entries.is_empty():
		return MFGachaTypes.GachaResult.new()

	var roll_value := rng.randi() % top_weight
	var cumulative := 0
	for entry in top_entries:
		cumulative += entry.weight
		if roll_value < cumulative:
			return MFGachaTypes.GachaResult.new(entry.monster_id, entry.rarity, is_pity, entry.is_featured)

	var last = top_entries[-1]
	return MFGachaTypes.GachaResult.new(last.monster_id, last.rarity, is_pity, last.is_featured)


static func _is_top_rarity(pool: RefCounted, rarity: int) -> bool:
	var max_rarity := 0
	for entry in pool.entries:
		if entry.rarity > max_rarity:
			max_rarity = entry.rarity
	return rarity == max_rarity
