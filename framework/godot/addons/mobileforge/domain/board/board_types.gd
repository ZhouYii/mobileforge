class_name MFBoardTypes extends RefCounted
## Data types for the board module. NO dependencies.

## Element IDs matching ToS: WATER=1, FIRE=2, GRASS=3, LIGHT=4, DARK=5, HEART=6
## Special elements: JAMMER=7 (deals damage to player), BOMB=8 (explodes neighbors),
## POISON=9 (damages player on match, cannot heal)
enum Element {
	NONE = 0,
	WATER = 1,
	FIRE = 2,
	GRASS = 3,
	LIGHT = 4,
	DARK = 5,
	HEART = 6,
	JAMMER = 7,
	BOMB = 8,
	POISON = 9,
}

## Check if an element is a standard matchable element (not special).
static func is_standard_element(elem: int) -> bool:
	return elem >= 1 and elem <= 6

## Check if an element is a hazard placed by enemies.
static func is_hazard_element(elem: int) -> bool:
	return elem >= 7 and elem <= 9

## Gem status types from ToS (14 types)
enum GemStatus {
	NONE = 0,
	FROZEN = 1,         # Cannot be moved
	LOCKED = 2,         # Cannot be matched (but can be moved)
	WEATHERED = 3,      # Breaks if not matched within N turns
	BURNING = 4,        # Deals damage to player each turn
	STICKY = 5,         # Swaps with adjacent when moved
	POISONED = 6,       # Converts to poison element on match
	ENCHANTED = 7,      # Counts as two elements
	PETRIFIED = 8,      # Cannot be moved or matched
	HIDDEN = 9,         # Element hidden until moved
	MARKED = 10,        # Extra damage when matched
	CHAINED = 11,       # Requires multiple matches to free
	TRANSMUTED = 12,    # Changed element
	SHIELDED = 13,      # Absorbs one modification
}


## Represents one gem on the board.
class GemState extends RefCounted:
	var element: int  ## Element enum value
	var statuses: Array[Dictionary] = []  ## Each: {"type": GemStatus, "turns": int, "data": Variant}
	var position: int  ## Flat index

	func _init(p_element: int, p_position: int) -> void:
		element = p_element
		position = p_position
		statuses = []

	func has_status(type: int) -> bool:
		for s in statuses:
			if s["type"] == type:
				return true
		return false

	func add_status(type: int, turns: int = -1, data: Variant = null) -> void:
		# Replace existing status of the same type
		for i in range(statuses.size()):
			if statuses[i]["type"] == type:
				statuses[i] = {"type": type, "turns": turns, "data": data}
				return
		statuses.append({"type": type, "turns": turns, "data": data})

	func remove_status(type: int) -> void:
		for i in range(statuses.size() - 1, -1, -1):
			if statuses[i]["type"] == type:
				statuses.remove_at(i)
				return

	func duplicate() -> GemState:
		var copy := GemState.new(element, position)
		for s in statuses:
			copy.statuses.append(s.duplicate())
		return copy


## A group of matched gems.
class MatchResult extends RefCounted:
	var element: int
	var positions: Array[int] = []  ## Flat positions of matched gems
	var combo_index: int = 0  ## Which combo this is in the cascade
	var gem_count: int:  ## Convenience — positions.size()
		get:
			return positions.size()

	func _init(p_element: int, p_positions: Array[int], p_combo: int = 0) -> void:
		element = p_element
		positions = p_positions
		combo_index = p_combo


## One step of the cascade resolution.
class CascadeStep extends RefCounted:
	var matches: Array = []  ## Array[MatchResult]
	var removed_positions: Array[int] = []
	var drops: Array[Dictionary] = []  ## Each: {"from": int, "to": int}
	var spawned: Array[Dictionary] = []  ## Each: {"position": int, "element": int}
	var hazard_effects: Array[Dictionary] = []  ## Special gem effects: {"type": String, "positions": Array, "damage": int}
	var expired_statuses: Array[Dictionary] = []  ## Statuses that expired: {"position": int, "status": int}
	var step_index: int = 0
