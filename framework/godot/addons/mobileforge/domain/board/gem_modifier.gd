class_name MFGemModifier extends RefCounted
## Applies and manages gem statuses. Pure functions.
## Handles the 14 status types from ToS + exclusion groups.

## Exclusion groups: some statuses cannot coexist.
const MOVEMENT_BLOCK := [MFBoardTypes.GemStatus.FROZEN, MFBoardTypes.GemStatus.PETRIFIED]
const ELEMENT_MODIFY := [MFBoardTypes.GemStatus.ENCHANTED, MFBoardTypes.GemStatus.TRANSMUTED]


## Apply a status to a gem, respecting exclusion rules.
## Returns true if the status was applied.
static func apply_status(gem: RefCounted, status_type: int, turns: int = -1, data: Variant = null) -> bool:
	if gem == null:
		return false

	# Shielded gems absorb one modification
	if gem.has_status(MFBoardTypes.GemStatus.SHIELDED) and status_type != MFBoardTypes.GemStatus.SHIELDED:
		gem.remove_status(MFBoardTypes.GemStatus.SHIELDED)
		return false

	# Check exclusion groups
	for group in [MOVEMENT_BLOCK, ELEMENT_MODIFY]:
		if status_type in group:
			for existing_type in group:
				if existing_type != status_type:
					gem.remove_status(existing_type)

	gem.add_status(status_type, turns, data)
	return true


## Tick all statuses on a gem (decrement turns). Remove expired ones.
## Returns Array of expired status types.
static func tick_statuses(gem: RefCounted) -> Array[int]:
	if gem == null:
		return []
	var expired: Array[int] = []
	var to_remove: Array[int] = []
	for i in range(gem.statuses.size() - 1, -1, -1):
		var s: Dictionary = gem.statuses[i]
		if s["turns"] > 0:
			s["turns"] -= 1
			if s["turns"] == 0:
				expired.append(s["type"])
				to_remove.append(s["type"])
	for t in to_remove:
		gem.remove_status(t)
	return expired


## Check if a gem can be moved (not frozen or petrified).
static func can_move(gem: RefCounted) -> bool:
	if gem == null:
		return false
	return not gem.has_status(MFBoardTypes.GemStatus.FROZEN) and not gem.has_status(MFBoardTypes.GemStatus.PETRIFIED)


## Check if a gem can be matched (not locked or petrified).
static func can_match(gem: RefCounted) -> bool:
	if gem == null:
		return false
	return not gem.has_status(MFBoardTypes.GemStatus.LOCKED) and not gem.has_status(MFBoardTypes.GemStatus.PETRIFIED)
