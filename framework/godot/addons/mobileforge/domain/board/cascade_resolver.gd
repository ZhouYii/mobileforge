class_name MFCascadeResolver extends RefCounted
## Resolves the full cascade after gems are moved.
## Loop: detect matches -> remove -> gravity drop -> spawn -> repeat until no matches.
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
		var removed_positions: Array[int] = []
		for p in removed.keys():
			removed_positions.append(p)
		removed_positions.sort()

		# 3. Remove matched gems
		for pos in removed_positions:
			board.set_gem(pos, null)

		# 4. Gravity: drop gems down to fill gaps (column by column, bottom to top)
		var drops: Array[Dictionary] = []
		var config := board.config
		for col in range(config.cols):
			var write_row := config.rows - 1  # Bottom of column
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

		# 5. Spawn new gems in empty top slots
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
					break  # No more empty above a filled cell

		# 6. Record this cascade step
		var step = MFBoardTypes.CascadeStep.new()
		step.matches = matches
		step.removed_positions = removed_positions
		step.drops = drops
		step.spawned = spawned
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
