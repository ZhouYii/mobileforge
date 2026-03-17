class_name MFGachaTypes extends RefCounted

class GachaPool extends RefCounted:
	var id: int
	var name: String
	var cost_currency: String  # e.g. "gems"
	var cost_amount: int
	var multi_pull_discount: int  ## 0 = no discount; >0 = free pulls in a 10-pull (e.g. 1 = pay 9 get 10)
	var daily_free: bool  ## If true, one free pull per day
	var one_time: bool  ## If true, can only be pulled once ever (e.g. beginner gacha)
	var guaranteed_top_rarity: bool  ## If true, at least one result is top rarity
	var entries: Array  # Array[GachaEntry]
	var pity_threshold: int  # Guaranteed top rarity after this many pulls
	var featured_ids: Array[int]  # Monster IDs with rate-up
	var step_up: Array  ## Array of step definitions. Empty = normal pool.

	## Step-up format: each step is a Dictionary:
	##   { "cost_mult": float, "pull_count": int, "guaranteed_rarity": int }
	##   cost_mult: multiplier on base cost (0.0 = free, 0.5 = half price, 1.0 = normal)
	##   pull_count: how many pulls this step does (usually 10)
	##   guaranteed_rarity: if >0, one pull in this step is guaranteed this rarity or higher

	func _init(data: Dictionary = {}) -> void:
		id = int(data.get("id", 0))
		name = str(data.get("name", ""))
		cost_currency = str(data.get("cost_currency", "gems"))
		cost_amount = int(data.get("cost_amount", 5))
		multi_pull_discount = int(data.get("multi_pull_discount", 0))
		daily_free = bool(data.get("daily_free", false))
		one_time = bool(data.get("one_time", false))
		guaranteed_top_rarity = bool(data.get("guaranteed_top_rarity", false))
		pity_threshold = int(data.get("pity_threshold", 0))
		featured_ids = []
		for fid in data.get("featured_ids", []):
			featured_ids.append(int(fid))
		entries = []
		for e in data.get("entries", []):
			entries.append(GachaEntry.new(e))
		step_up = []
		for s in data.get("step_up", []):
			step_up.append(s)

	func is_step_up() -> bool:
		return not step_up.is_empty()

	func get_step(step_index: int) -> Dictionary:
		if step_index < 0 or step_index >= step_up.size():
			return {}
		return step_up[step_index]

	func total_steps() -> int:
		return step_up.size()


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
