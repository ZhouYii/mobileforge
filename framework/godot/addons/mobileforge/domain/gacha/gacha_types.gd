class_name MFGachaTypes extends RefCounted

class GachaPool extends RefCounted:
	var id: int
	var name: String
	var cost_currency: String  # e.g. "gems"
	var cost_amount: int
	var entries: Array  # Array[GachaEntry]
	var pity_threshold: int  # Guaranteed top rarity after this many pulls
	var featured_ids: Array[int]  # Monster IDs with rate-up

	func _init(data: Dictionary = {}) -> void:
		id = int(data.get("id", 0))
		name = str(data.get("name", ""))
		cost_currency = str(data.get("cost_currency", "gems"))
		cost_amount = int(data.get("cost_amount", 5))
		pity_threshold = int(data.get("pity_threshold", 0))
		featured_ids = []
		for fid in data.get("featured_ids", []):
			featured_ids.append(int(fid))
		entries = []
		for e in data.get("entries", []):
			entries.append(GachaEntry.new(e))


class GachaEntry extends RefCounted:
	var monster_id: int
	var rarity: int
	var weight: int  # Relative weight for weighted random
	var is_featured: bool

	func _init(data: Dictionary = {}) -> void:
		monster_id = int(data.get("monster_id", 0))
		rarity = int(data.get("rarity", 1))
		weight = int(data.get("weight", 100))
		is_featured = bool(data.get("is_featured", false))


class GachaResult extends RefCounted:
	var monster_id: int
	var rarity: int
	var is_pity: bool
	var is_featured: bool

	func _init(p_monster_id: int = 0, p_rarity: int = 1, p_is_pity: bool = false, p_is_featured: bool = false) -> void:
		monster_id = p_monster_id
		rarity = p_rarity
		is_pity = p_is_pity
		is_featured = p_is_featured
