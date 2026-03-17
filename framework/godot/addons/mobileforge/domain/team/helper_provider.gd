class_name MFHelperProvider extends RefCounted
## Provides a list of available helper monsters for the friend/helper slot (slot 5).
## Default implementation: randomly selects high-rarity monsters from game data.
## Games can subclass for network-backed friend lists.

var _monster_manager: MFMonsterManager
var _game_data_lookup: Callable  ## func() -> Array[Definition] (all monster definitions)
var _helper_count: int
var _rng: RandomNumberGenerator
var _available_helpers: Array = []  ## Array[HelperEntry]


class HelperEntry extends RefCounted:
	## A single helper option presented to the player.
	var monster_id: int
	var monster_def: RefCounted  ## MonsterDef
	var monster_instance: RefCounted  ## MonsterInstance (at max level)
	var monster_stats: RefCounted  ## MonsterStats
	var display_name: String

	func _init(p_id: int, p_def: RefCounted, p_inst: RefCounted, p_stats: RefCounted, p_name: String) -> void:
		monster_id = p_id
		monster_def = p_def
		monster_instance = p_inst
		monster_stats = p_stats
		display_name = p_name


func _init(monster_manager: MFMonsterManager, game_data_lookup: Callable, count: int = 5) -> void:
	_monster_manager = monster_manager
	_game_data_lookup = game_data_lookup
	_helper_count = count
	_rng = RandomNumberGenerator.new()
	_rng.randomize()


## Get the current list of available helpers.
func get_available_helpers() -> Array:  ## -> Array[HelperEntry]
	return _available_helpers


## Re-randomize the helper list. Call on each dungeon entry.
## Pass a seed >= 0 for deterministic results (useful for testing).
func refresh(seed: int = -1) -> void:
	if seed >= 0:
		_rng.seed = seed
	else:
		_rng.randomize()

	_available_helpers.clear()

	# Get all monster definitions from game data
	if not _game_data_lookup.is_valid():
		return
	var all_defs: Array = _game_data_lookup.call()
	if all_defs.is_empty():
		return

	# Filter: rarity >= 5 AND leader_skill_id > 0
	var candidates: Array = []
	for def_entry in all_defs:
		var raw: Dictionary = def_entry.raw() if def_entry.has_method("raw") else def_entry
		var rarity: int = int(raw.get("rarity", 0))
		var raw_ls = raw.get("leader_skill_id", -1)
		var leader_skill_id: int = int(raw_ls) if raw_ls != null else -1
		if rarity >= 5 and leader_skill_id >= 0:
			candidates.append(raw)

	if candidates.is_empty():
		return

	# Shuffle and take first `count` entries
	_shuffle_array(candidates)
	var pick_count := mini(_helper_count, candidates.size())

	for i in range(pick_count):
		var raw: Dictionary = candidates[i]
		var mid: int = int(raw.get("id", 0))
		var def = _monster_manager.get_def(mid)
		if def == null:
			continue

		# Create instance at max level for stats
		var inst = _monster_manager.create_instance(mid)
		inst.level = def.max_level

		var stats = _monster_manager.get_stats(inst)
		var name: String = raw.get("name", "Helper")

		var entry = HelperEntry.new(mid, def, inst, stats, name)
		_available_helpers.append(entry)


## Fisher-Yates shuffle using our RNG.
func _shuffle_array(arr: Array) -> void:
	for i in range(arr.size() - 1, 0, -1):
		var j := _rng.randi_range(0, i)
		var temp = arr[i]
		arr[i] = arr[j]
		arr[j] = temp
