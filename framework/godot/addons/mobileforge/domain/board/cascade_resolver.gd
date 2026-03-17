class_name MFCascadeResolver extends RefCounted
## Resolves the full cascade after gems are moved.
## Loop: detect matches -> process hazards -> remove -> gravity drop -> spawn -> tick statuses -> repeat.
## Returns Array[CascadeStep] — complete history for animation.


static func resolve(board: MFBoardLogic) -> Array:
	var steps: Array = []
	var step_index := 0

	while true:
		# 1. Detect matches
		var matches := MFMatchDetector.find_matches(board)
		if matches.is_empty():
			break

		# 2. Collect all matched positions
		var removed: Dictionary = {}  # pos -> true (set)
		for match_result in matches:
			for pos in match_result.positions:
				removed[pos] = true

		# 3. Process hazard gems in matched set
		var hazard_effects: Array[Dictionary] = []
		var bomb_explode_positions: Dictionary = {}  # extra positions to remove from bomb blasts

		for match_result in matches:
			for pos in match_result.positions:
				var gem = board.get_gem(pos)
				if gem == null:
					continue
				match gem.element:
					MFBoardTypes.Element.JAMMER:
						# Jammers deal flat damage when cleared
						hazard_effects.append({"type": "jammer_damage", "positions": [pos], "damage": 500})
					MFBoardTypes.Element.BOMB:
						# Bombs explode and remove all 8 neighbors
						var neighbors := _get_neighbors(pos, board.config)
						hazard_effects.append({"type": "bomb_explode", "positions": [pos] + neighbors, "damage": 0})
						for n in neighbors:
							bomb_explode_positions[n] = true
					MFBoardTypes.Element.POISON:
						# Poison gems deal damage when matched (instead of healing like heart)
						hazard_effects.append({"type": "poison_damage", "positions": [pos], "damage": 1000})

		# Add bomb explosion positions to removal set
		for pos in bomb_explode_positions:
			removed[pos] = true

		var removed_positions: Array[int] = []
		for p in removed.keys():
			removed_positions.append(p)
		removed_positions.sort()

		# 4. Remove matched gems (and bomb-exploded gems)
		for pos in removed_positions:
			board.set_gem(pos, null)

		# 5. Gravity: drop gems down to fill gaps
		var drops: Array[Dictionary] = []
		var config := board.config
		for col in range(config.cols):
			var write_row := config.rows - 1
			for read_row in range(config.rows - 1, -1, -1):
				var pos := config.rc_to_pos(read_row, col)
				var gem = board.get_gem(pos)
				if gem != null:
					var target_pos := config.rc_to_pos(write_row, col)
					if pos != target_pos:
						drops.append({"from": pos, "to": target_pos})
						board.set_gem(target_pos, gem)
						board.set_gem(pos, null)
					write_row -= 1

		# 6. Spawn new gems in empty top slots
		var spawned: Array[Dictionary] = []
		for col in range(config.cols):
			for row in range(config.rows):
				var pos := config.rc_to_pos(row, col)
				if board.get_gem(pos) == null:
					var element: int = config.elements[board._rng.randi() % config.elements.size()]
					var gem = MFBoardTypes.GemState.new(element, pos)
					board.set_gem(pos, gem)
					spawned.append({"position": pos, "element": element})
				else:
					break

		# 7. Tick gem statuses on all remaining gems
		var expired_statuses: Array[Dictionary] = []
		for i in range(config.total_cells()):
			var gem = board.get_gem(i)
			if gem != null and not gem.statuses.is_empty():
				var expired := MFGemModifier.tick_statuses(gem)
				for status_type in expired:
					expired_statuses.append({"position": i, "status": status_type})

		# 8. Record this cascade step
		var step = MFBoardTypes.CascadeStep.new()
		step.matches = matches
		step.removed_positions = removed_positions
		step.drops = drops
		step.spawned = spawned
		step.hazard_effects = hazard_effects
		step.expired_statuses = expired_statuses
		step.step_index = step_index

		# Set combo_index on each match
		var combo_offset := 0
		if not steps.is_empty():
			var prev_step = steps[-1]
			combo_offset = prev_step.matches[-1].combo_index + prev_step.matches.size()
		for i in range(matches.size()):
			matches[i].combo_index = combo_offset + i

		steps.append(step)
		step_index += 1

	return steps


## Get all 8 neighbors of a position (for bomb explosions).
static func _get_neighbors(pos: int, config: MFBoardConfig) -> Array[int]:
	var neighbors: Array[int] = []
	var row := config.pos_to_row(pos)
	var col := config.pos_to_col(pos)
	for dr in range(-1, 2):
		for dc in range(-1, 2):
			if dr == 0 and dc == 0:
				continue
			var nr := row + dr
			var nc := col + dc
			if config.is_valid_rc(nr, nc):
				neighbors.append(config.rc_to_pos(nr, nc))
	return neighbors
