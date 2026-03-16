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


## Calculate stats for a monster instance.
func get_stats(instance: RefCounted) -> RefCounted:  ## MonsterInstance -> MonsterStats
	var def = get_def(instance.def_id)
	if def == null:
		return MFMonsterTypes.MonsterStats.new(0, 0, 0)
	return MFStatCalculator.calculate(def, instance)


## Add experience to a monster, leveling up as needed.
## Returns the number of levels gained.
func add_exp(instance: RefCounted, exp_amount: int) -> int:
	var def = get_def(instance.def_id)
	if def == null:
		return 0
	var levels_gained := 0
	instance.exp += exp_amount
	while instance.level < def.max_level:
		var needed := MFStatCalculator.exp_for_level(instance.level, def.exp_curve)
		if instance.exp >= needed:
			instance.exp -= needed
			instance.level += 1
			levels_gained += 1
		else:
			break
	if instance.level >= def.max_level:
		instance.exp = 0  # Cap at max level
	return levels_gained


## Fuse a fodder monster into a base monster (add exp).
## Returns exp gained.
func fuse(base: RefCounted, fodder: RefCounted) -> int:
	var fodder_def = get_def(fodder.def_id)
	if fodder_def == null:
		return 0
	# Exp from fusion = base_exp based on fodder level and rarity
	var exp_value := fodder.level * fodder_def.rarity * 50
	add_exp(base, exp_value)
	return exp_value


## Check if a monster can evolve.
func can_evolve(instance: RefCounted) -> bool:
	var def = get_def(instance.def_id)
	if def == null or def.evolve_to < 0:
		return false
	if instance.level < def.max_level:
		return false
	return true


## Evolve a monster (returns new def_id, resets level to 1).
func evolve(instance: RefCounted) -> int:
	var def = get_def(instance.def_id)
	if def == null or not can_evolve(instance):
		return -1
	var new_def_id := def.evolve_to
	instance.def_id = new_def_id
	instance.level = 1
	instance.exp = 0
	return new_def_id
