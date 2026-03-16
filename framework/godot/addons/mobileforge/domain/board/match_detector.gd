class_name MFMatchDetector extends RefCounted
## Detects matches on a board. Pure function — takes board state, returns matches.
## Scans rows then columns for runs of 3+ same-element gems.
## Overlapping matches are merged (e.g., T-shape or L-shape = one match).


## Find all matches on the board.
## Returns Array of MatchResult.
static func find_matches(board: MFBoardLogic) -> Array:
	var config := board.config
	var horizontal: Array = _scan_horizontal(board, config)
	var vertical: Array = _scan_vertical(board, config)
	return _merge_matches(horizontal + vertical)


## Scan rows for horizontal runs of min_match+ same element.
static func _scan_horizontal(board: MFBoardLogic, config: MFBoardConfig) -> Array:
	var matches: Array = []
	for row in range(config.rows):
		var run_element := 0
		var run_positions: Array[int] = []
		for col in range(config.cols):
			var pos := config.rc_to_pos(row, col)
			var gem = board.get_gem(pos)
			if gem == null or gem.element == MFBoardTypes.Element.NONE or gem.has_status(MFBoardTypes.GemStatus.LOCKED):
				_flush_run(run_positions, run_element, config.min_match, matches)
				run_positions = []
				run_element = 0
				continue
			if gem.element == run_element:
				run_positions.append(pos)
			else:
				_flush_run(run_positions, run_element, config.min_match, matches)
				run_element = gem.element
				run_positions = [pos]
		_flush_run(run_positions, run_element, config.min_match, matches)
	return matches


## Scan columns for vertical runs.
static func _scan_vertical(board: MFBoardLogic, config: MFBoardConfig) -> Array:
	var matches: Array = []
	for col in range(config.cols):
		var run_element := 0
		var run_positions: Array[int] = []
		for row in range(config.rows):
			var pos := config.rc_to_pos(row, col)
			var gem = board.get_gem(pos)
			if gem == null or gem.element == MFBoardTypes.Element.NONE or gem.has_status(MFBoardTypes.GemStatus.LOCKED):
				_flush_run(run_positions, run_element, config.min_match, matches)
				run_positions = []
				run_element = 0
				continue
			if gem.element == run_element:
				run_positions.append(pos)
			else:
				_flush_run(run_positions, run_element, config.min_match, matches)
				run_element = gem.element
				run_positions = [pos]
		_flush_run(run_positions, run_element, config.min_match, matches)
	return matches


static func _flush_run(positions: Array[int], element: int, min_match: int, out: Array) -> void:
	if positions.size() >= min_match and element != 0:
		out.append(MFBoardTypes.MatchResult.new(element, positions.duplicate()))


## Merge overlapping matches of the same element into single matches.
## Two matches overlap if they share any position AND have the same element.
static func _merge_matches(matches: Array) -> Array:
	if matches.is_empty():
		return []

	# Union-Find approach: group matches that share positions and element
	var merged: Array = []
	var used: Array[bool] = []
	used.resize(matches.size())
	used.fill(false)

	for i in range(matches.size()):
		if used[i]:
			continue
		var group_element: int = matches[i].element
		var group_positions: Dictionary = {}  # Use as set: pos -> true
		for p in matches[i].positions:
			group_positions[p] = true

		# Find all matches that overlap with this group
		var changed := true
		while changed:
			changed = false
			for j in range(matches.size()):
				if used[j] or i == j:
					continue
				if matches[j].element != group_element:
					continue
				# Check if any position overlaps
				var overlaps := false
				for p in matches[j].positions:
					if group_positions.has(p):
						overlaps = true
						break
				if overlaps:
					for p in matches[j].positions:
						group_positions[p] = true
					used[j] = true
					changed = true

		used[i] = true
		var positions_array: Array[int] = []
		for p in group_positions.keys():
			positions_array.append(p)
		positions_array.sort()
		merged.append(MFBoardTypes.MatchResult.new(group_element, positions_array))

	return merged
