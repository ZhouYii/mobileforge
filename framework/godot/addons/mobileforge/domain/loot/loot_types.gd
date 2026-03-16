class_name MFLootTypes extends RefCounted


class LootTableDef extends RefCounted:
	var id: int
	var entries: Array  # Array[LootEntry]

	func _init(data: Dictionary = {}) -> void:
		id = int(data.get("id", 0))
		entries = []
		for e in data.get("entries", []):
			entries.append(LootEntry.new(e))


class LootEntry extends RefCounted:
	var type: String  # "monster", "currency", "item"
	var item_id: int
	var count_min: int
	var count_max: int
	var weight: int
	var guaranteed: bool  # Always drops

	func _init(data: Dictionary = {}) -> void:
		type = str(data.get("type", "item"))
		item_id = int(data.get("item_id", 0))
		count_min = int(data.get("count_min", 1))
		count_max = int(data.get("count_max", 1))
		weight = int(data.get("weight", 100))
		guaranteed = bool(data.get("guaranteed", false))


class LootDrop extends RefCounted:
	var type: String
	var item_id: int
	var count: int

	func _init(p_type: String = "", p_id: int = 0, p_count: int = 1) -> void:
		type = p_type
		item_id = p_id
		count = p_count
