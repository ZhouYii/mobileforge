class_name StatBlock
extends Resource

## Layered stat modifier system with base stats plus ordered modifiers.
##
## Synthesized from 44/60 analyzed combat-oriented Unity mobile games.
## Primary sources: RAID: Shadow Legends (BattleAbility hierarchy with 219
## stat-modifying subclasses, SharedModel.Battle.Effects), Cookie Run: Kingdom
## (BattleAbilityIncreaseAttackPoint/ByStat composition pattern, 219
## BattleAbility subclasses), AQ Battle Gems (EffectTemplate with operator-based
## stat modification: +, -, *), Archero (buff system with SkillAlone stat VOs),
## Overkill 3 (WeaponShopAttributes with upgrade categories).
##
## Modifier evaluation order:
##   1. Start with base value
##   2. Apply all FLAT_ADD modifiers (sum)
##   3. Apply all PERCENT_ADD modifiers (additive %, then multiply once)
##   4. Apply all PERCENT_MULT modifiers (each multiplied in sequence)
##   5. If any OVERRIDE exists, use the last-applied OVERRIDE instead
##
## This matches the RAID: Shadow Legends pattern where stat bonuses from gear,
## masteries, great hall, and aura stack additively within type, then multiply
## across types.
##
## Usage:
##   var stats := StatBlock.new()
##   stats.set_base("attack", 100.0)
##   var mod_id := stats.add_modifier("attack", StatBlock.ModType.PERCENT_ADD,
##       0.25, "sword_buff")
##   print(stats.get_stat("attack"))  # 125.0
##   stats.remove_modifier(mod_id)


## Emitted when any stat value changes after recalculation.
## [param stat_name] The stat that changed.
## [param old_value] The previous computed value.
## [param new_value] The new computed value.
signal stat_changed(stat_name: StringName, old_value: float, new_value: float)

## Emitted when a modifier is added.
signal modifier_added(modifier_id: int, stat_name: StringName)

## Emitted when a modifier is removed.
signal modifier_removed(modifier_id: int, stat_name: StringName)


## Modifier application types, following the RAID: Shadow Legends / Cookie Run
## stacking model where flat and percent bonuses from different sources combine.
enum ModType {
	FLAT_ADD,      ## Added to base value directly (e.g., +50 attack from gear).
	PERCENT_ADD,   ## Additive percentage (e.g., 25% + 10% = 35% applied once).
	PERCENT_MULT,  ## Multiplicative percentage (e.g., 1.2 * 1.1 = 1.32x). Stacks multiplicatively.
	OVERRIDE,      ## Replaces the final value entirely. Last applied wins.
}


## Internal modifier record.
class StatModifier:
	var id: int
	var stat_name: StringName
	var type: ModType
	var value: float
	var source: StringName  ## Tracks what applied this (buff name, equipment, etc.)
	var priority: int       ## For ordering within same type.


## Base stat values keyed by stat name.
var _bases: Dictionary = {}

## Computed (cached) final values keyed by stat name.
var _computed: Dictionary = {}

## All active modifiers keyed by modifier id.
var _modifiers: Dictionary = {}

## Modifier IDs grouped by stat name for fast lookup.
var _stat_modifier_ids: Dictionary = {}

## Next unique modifier ID.
var _next_id: int = 1

## Whether computed caches are dirty, keyed by stat name.
var _dirty: Dictionary = {}


## Set the base value for a stat. Creates the stat if it doesn't exist.
func set_base(stat_name: StringName, value: float) -> void:
	_bases[stat_name] = value
	_mark_dirty(stat_name)


## Get the base value for a stat before any modifiers.
func get_base(stat_name: StringName) -> float:
	return _bases.get(stat_name, 0.0)


## Get the final computed value for a stat after all modifiers.
func get_stat(stat_name: StringName) -> float:
	if _dirty.get(stat_name, true):
		_recompute(stat_name)
	return _computed.get(stat_name, _bases.get(stat_name, 0.0))


## Add a modifier and return its unique ID for later removal.
## [param source] Identifies the origin (e.g., "fire_buff", "helmet_slot").
## [param priority] Order within same type; higher = applied later.
func add_modifier(stat_name: StringName, type: ModType, value: float,
		source: StringName = &"", priority: int = 0) -> int:
	var mod := StatModifier.new()
	mod.id = _next_id
	mod.stat_name = stat_name
	mod.type = type
	mod.value = value
	mod.source = source
	mod.priority = priority
	_next_id += 1

	_modifiers[mod.id] = mod
	if not _stat_modifier_ids.has(stat_name):
		_stat_modifier_ids[stat_name] = []
	_stat_modifier_ids[stat_name].append(mod.id)
	_mark_dirty(stat_name)
	modifier_added.emit(mod.id, stat_name)
	return mod.id


## Remove a modifier by its ID.
func remove_modifier(modifier_id: int) -> void:
	if not _modifiers.has(modifier_id):
		return
	var mod: StatModifier = _modifiers[modifier_id]
	var stat_name := mod.stat_name
	_modifiers.erase(modifier_id)
	if _stat_modifier_ids.has(stat_name):
		_stat_modifier_ids[stat_name].erase(modifier_id)
	_mark_dirty(stat_name)
	modifier_removed.emit(modifier_id, stat_name)


## Remove all modifiers from a specific source (e.g., clear all "fire_buff" mods).
func remove_modifiers_by_source(source: StringName) -> void:
	var to_remove: Array[int] = []
	for id: int in _modifiers:
		if _modifiers[id].source == source:
			to_remove.append(id)
	for id: int in to_remove:
		remove_modifier(id)


## Remove all modifiers for a specific stat.
func clear_stat_modifiers(stat_name: StringName) -> void:
	if not _stat_modifier_ids.has(stat_name):
		return
	var ids: Array = _stat_modifier_ids[stat_name].duplicate()
	for id: int in ids:
		remove_modifier(id)


## Remove all modifiers from all stats.
func clear_all_modifiers() -> void:
	var all_ids: Array = _modifiers.keys().duplicate()
	for id: int in all_ids:
		remove_modifier(id)


## Get all active modifiers for a stat, sorted by priority.
func get_modifiers(stat_name: StringName) -> Array[StatModifier]:
	var result: Array[StatModifier] = []
	if not _stat_modifier_ids.has(stat_name):
		return result
	for id: int in _stat_modifier_ids[stat_name]:
		if _modifiers.has(id):
			result.append(_modifiers[id])
	result.sort_custom(func(a: StatModifier, b: StatModifier) -> bool:
		return a.priority < b.priority)
	return result


## Check if any modifier from the given source is active.
func has_modifier_from_source(source: StringName) -> bool:
	for id: int in _modifiers:
		if _modifiers[id].source == source:
			return true
	return false


## Get a list of all stat names that have base values or modifiers.
func get_stat_names() -> Array[StringName]:
	var names: Dictionary = {}
	for key: StringName in _bases:
		names[key] = true
	for key: StringName in _stat_modifier_ids:
		names[key] = true
	var result: Array[StringName] = []
	for key: StringName in names:
		result.append(key)
	return result


func _mark_dirty(stat_name: StringName) -> void:
	var old_value: float = _computed.get(stat_name, _bases.get(stat_name, 0.0))
	_dirty[stat_name] = true
	# Eagerly recompute and emit signal.
	_recompute(stat_name)
	var new_value: float = _computed.get(stat_name, 0.0)
	if not is_equal_approx(old_value, new_value):
		stat_changed.emit(stat_name, old_value, new_value)


func _recompute(stat_name: StringName) -> void:
	var base: float = _bases.get(stat_name, 0.0)
	var mods := get_modifiers(stat_name)

	var flat_sum: float = 0.0
	var percent_add_sum: float = 0.0
	var percent_mult_product: float = 1.0
	var override_value: float = 0.0
	var has_override: bool = false

	for mod: StatModifier in mods:
		match mod.type:
			ModType.FLAT_ADD:
				flat_sum += mod.value
			ModType.PERCENT_ADD:
				percent_add_sum += mod.value
			ModType.PERCENT_MULT:
				percent_mult_product *= (1.0 + mod.value)
			ModType.OVERRIDE:
				override_value = mod.value
				has_override = true

	if has_override:
		_computed[stat_name] = override_value
	else:
		var result := (base + flat_sum) * (1.0 + percent_add_sum) * percent_mult_product
		_computed[stat_name] = result

	_dirty[stat_name] = false
