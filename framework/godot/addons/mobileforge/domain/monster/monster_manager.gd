class_name MFMonsterManager extends RefCounted
## Manages monster instances: create, level up, fuse, evolve.
## Uses GameData for definitions and PlayerState for persistence (passed in, not imported).


var _next_instance_id: int = 1
var _game_data_lookup: Callable  ## func(def_id: int) -> Dictionary (def data)


func _init(game_data_lookup: Callable = Callable()) -> void:
	_game_data_lookup = game_data_lookup


## Create a new monster instance from a definition ID.
func create_instance(def_id: int, level: int = 1) -> RefCounted:  ## -> MonsterInstance
	var instance = MFMonsterTypes.MonsterInstance.new(_next_instance_id, def_id)
	instance.level = level
	_next_instance_id += 1
	return instance


## Get MonsterDef for a given def_id (via the lookup callback).
func get_def(def_id: int) -> RefCounted:  ## -> MonsterDef or null
	if not _game_data_lookup.is_valid():
		return null
	var data = _game_data_lookup.call(def_id)
	if data == null or (data is Dictionary and data.is_empty()):
		return null
	if data is Dictionary:
		return MFMonsterTypes.MonsterDef.new(data)
	return data  # Already a MonsterDef


## Get raw definition Dictionary for a given def_id.
## Convenience wrapper for UI code that needs Dictionary access.
func get_def_data(def_id: int) -> Dictionary:
	if not _game_data_lookup.is_valid():
		return {}
	var data = _game_data_lookup.call(def_id)
	if data == null:
		return {}
	if data is Dictionary:
		return data
	if data.has_method("raw"):
		return data.raw()
	return {}


## Calculate stats for a monster instance.
func get_stats(instance: RefCounted) -> RefCounted:  ## MonsterInstance -> MonsterStats
	var def = get_def(instance.def_id)
	if def == null:
		return MFMonsterTypes.MonsterStats.new(0, 0, 0)
	return MFStatCalculator.calculate(def, instance)


## Add experience to a monster, leveling up as needed.
## Respects limit break — effective max level = def.max_level + (limit_break * 10).
## Returns the number of levels gained.
func add_exp(instance: RefCounted, exp_amount: int) -> int:
	var def = get_def(instance.def_id)
	if def == null:
		return 0
	var effective_max: int = instance.get_effective_max_level(def.max_level)
	var levels_gained := 0
	instance.exp += exp_amount
	while instance.level < effective_max:
		var needed: int = MFStatCalculator.exp_for_level(instance.level, def.exp_curve)
		if instance.exp >= needed:
			instance.exp -= needed
			instance.level += 1
			levels_gained += 1
		else:
			break
	if instance.level >= effective_max:
		instance.exp = 0  # Cap at effective max level
	return levels_gained


## Fuse a fodder monster into a base monster (add exp).
## Returns exp gained.
func fuse(base: RefCounted, fodder: RefCounted) -> int:
	var fodder_def = get_def(fodder.def_id)
	if fodder_def == null:
		return 0
	# Exp from fusion = base_exp based on fodder level and rarity
	var exp_value: int = fodder.level * fodder_def.rarity * 50
	add_exp(base, exp_value)
	return exp_value


## Check if a monster can evolve (must be at base max level, not limit-broken level).
func can_evolve(instance: RefCounted) -> bool:
	var def = get_def(instance.def_id)
	if def == null or def.evolve_to < 0:
		return false
	if instance.level < def.max_level:
		return false
	return true


## Apply a limit break to raise max level by 10. Max 2 limit breaks.
## Returns the new effective max level, or -1 if already at max limit breaks.
const MAX_LIMIT_BREAKS := 2
func apply_limit_break(instance: RefCounted) -> int:
	if instance.limit_break >= MAX_LIMIT_BREAKS:
		return -1
	var def = get_def(instance.def_id)
	if def == null:
		return -1
	# Must be at current effective max level to limit break
	var effective_max: int = instance.get_effective_max_level(def.max_level)
	if instance.level < effective_max:
		return -1
	instance.limit_break += 1
	return int(instance.get_effective_max_level(def.max_level))


## Check if a monster can be limit broken.
func can_limit_break(instance: RefCounted) -> bool:
	if instance.limit_break >= MAX_LIMIT_BREAKS:
		return false
	var def = get_def(instance.def_id)
	if def == null:
		return false
	var effective_max: int = instance.get_effective_max_level(def.max_level)
	return instance.level >= effective_max


## Add plus stats to a monster from a fodder monster.
## ToS formula: +1 per fodder of same element, +1 per matching stat type.
## Simplified: each fodder adds +1 to each stat, capped at 99 per stat (297 total).
## Returns Dictionary of stats actually added: {"hp": int, "atk": int, "rec": int}
func add_plus_stats(instance: RefCounted, hp_add: int = 1, atk_add: int = 1, rec_add: int = 1) -> Dictionary:
	var added := {"hp": 0, "atk": 0, "rec": 0}
	var old_hp: int = instance.plus_hp
	var old_atk: int = instance.plus_atk
	var old_rec: int = instance.plus_rec
	instance.plus_hp = mini(instance.plus_hp + hp_add, 99)
	instance.plus_atk = mini(instance.plus_atk + atk_add, 99)
	instance.plus_rec = mini(instance.plus_rec + rec_add, 99)
	added["hp"] = instance.plus_hp - old_hp
	added["atk"] = instance.plus_atk - old_atk
	added["rec"] = instance.plus_rec - old_rec
	return added


## Get the total plus stat points on a monster (max 297).
func get_plus_total(instance: RefCounted) -> int:
	return instance.plus_hp + instance.plus_atk + instance.plus_rec


## Unlock the next awakening slot for a monster.
## Requires a material monster (consumed). Returns the awakening ID unlocked, or -1 on failure.
func awaken(instance: RefCounted, material: RefCounted) -> int:
	var def = get_def(instance.def_id)
	if def == null or def.awakening_slots.is_empty():
		return -1
	# Check if there are slots left to unlock
	var next_slot: int = instance.awakenings.size()
	if next_slot >= def.awakening_slots.size():
		return -1  # All slots already unlocked
	var awakening_id: int = def.awakening_slots[next_slot]
	instance.awakenings.append(awakening_id)
	return awakening_id


## Check if a monster can be awakened (has remaining slots).
func can_awaken(instance: RefCounted) -> bool:
	var def = get_def(instance.def_id)
	if def == null or def.awakening_slots.is_empty():
		return false
	return instance.awakenings.size() < def.awakening_slots.size()


## Get all awakening ability IDs active on a monster.
func get_awakenings(instance: RefCounted) -> Array[int]:
	return instance.awakenings


## Get awakening display name.
static func awakening_name(awakening_id: int) -> String:
	return MFMonsterTypes.AWAKENING_NAMES.get(awakening_id, "Unknown")


## Attempt to level up a monster's active skill.
## Skill levels up if the fodder has the same active_skill_id (guaranteed)
## or same element (chance-based). Returns true if skill leveled up.
## max_skill_level comes from the skill definition's max_level field.
func level_up_skill(instance: RefCounted, fodder: RefCounted, max_skill_level: int = 10, rng: RandomNumberGenerator = null) -> bool:
	if instance.skill_level >= max_skill_level:
		return false

	var base_def = get_def(instance.def_id)
	var fodder_def = get_def(fodder.def_id)
	if base_def == null or fodder_def == null:
		return false

	# Guaranteed if same active skill
	if base_def.active_skill_id >= 0 and base_def.active_skill_id == fodder_def.active_skill_id:
		instance.skill_level = mini(instance.skill_level + 1, max_skill_level)
		return true

	# Chance-based if same element (higher rarity = higher chance)
	if base_def.element == fodder_def.element:
		var chance: float = 0.1 + fodder_def.rarity * 0.05  # 15% for 1★, 35% for 5★
		if rng == null:
			rng = RandomNumberGenerator.new()
			rng.randomize()
		if rng.randf() < chance:
			instance.skill_level = mini(instance.skill_level + 1, max_skill_level)
			return true

	return false


## Inherit an active skill from a donor monster. Consumes the donor.
## The base monster gains the donor's active_skill_id as its inherited_skill_id.
## Returns the inherited skill_id, or -1 on failure.
func inherit_skill(base: RefCounted, donor: RefCounted) -> int:
	var donor_def = get_def(donor.def_id)
	if donor_def == null or donor_def.active_skill_id < 0:
		return -1
	# Can't inherit if base already has an inherited skill (must remove first)
	if base.inherited_skill_id >= 0:
		return -1
	# Can't inherit from same monster type
	if base.def_id == donor.def_id:
		return -1
	base.inherited_skill_id = donor_def.active_skill_id
	return base.inherited_skill_id


## Remove the inherited skill from a monster.
func remove_inherited_skill(instance: RefCounted) -> void:
	instance.inherited_skill_id = -1


## Check if a monster has an inherited skill.
func has_inherited_skill(instance: RefCounted) -> bool:
	return instance.inherited_skill_id >= 0


## Evolve a monster (returns new def_id, resets level to 1).
func evolve(instance: RefCounted) -> int:
	var def = get_def(instance.def_id)
	if def == null or not can_evolve(instance):
		return -1
	var new_def_id: int = def.evolve_to
	instance.def_id = new_def_id
	instance.level = 1
	instance.exp = 0
	return new_def_id
