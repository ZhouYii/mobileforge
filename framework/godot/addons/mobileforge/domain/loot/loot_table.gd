class_name MFLootTable extends RefCounted
## Pure function loot roller.


static func roll_drops(table_def: RefCounted, rng: RandomNumberGenerator, roll_count: int = 1) -> Array:
	var drops: Array = []

	# Always include guaranteed drops
	for entry in table_def.entries:
		if entry.guaranteed:
			var count := rng.randi_range(entry.count_min, entry.count_max)
			drops.append(MFLootTypes.LootDrop.new(entry.type, entry.item_id, count))

	# Roll for non-guaranteed drops
	var non_guaranteed: Array = []
	var total_weight := 0
	for entry in table_def.entries:
		if not entry.guaranteed:
			non_guaranteed.append(entry)
			total_weight += entry.weight

	if total_weight > 0:
		for i in range(roll_count):
			var roll_value := rng.randi() % total_weight
			var cumulative := 0
			for entry in non_guaranteed:
				cumulative += entry.weight
				if roll_value < cumulative:
					var count := rng.randi_range(entry.count_min, entry.count_max)
					drops.append(MFLootTypes.LootDrop.new(entry.type, entry.item_id, count))
					break

	return drops
