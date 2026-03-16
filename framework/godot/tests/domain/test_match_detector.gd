extends MFTestBase
## Tests for MatchDetector (domain/board/match_detector.gd)
## Uses board_match_cases.json test vectors for board setups.

const BoardConfigScript = preload("res://addons/mobileforge/domain/board/board_config.gd")
const BoardLogicScript = preload("res://addons/mobileforge/domain/board/board_logic.gd")
const BoardTypesScript = preload("res://addons/mobileforge/domain/board/board_types.gd")
const MatchDetectorScript = preload("res://addons/mobileforge/domain/board/match_detector.gd")

var _config: MFBoardConfig
var _board: MFBoardLogic


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_config = MFBoardConfig.new(5, 6)
	_board = MFBoardLogic.new(_config, 42)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

## Set board from flat array of ints (matching test vector format).
func _set_board(elements: Array) -> void:
	var typed: Array[int] = []
	for e in elements:
		typed.append(e)
	_board.from_element_array(typed)


## Find a match in the results list that contains all the given positions.
func _find_match_with_positions(matches: Array, positions: Array[int]) -> RefCounted:
	for m in matches:
		var match_pos: Array[int] = m.positions
		var all_found := true
		for p in positions:
			if not match_pos.has(p):
				all_found = false
				break
		if all_found and match_pos.size() == positions.size():
			return m
	return null


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_horizontal_3_match() -> void:
	# From test vector: horizontal_3_match
	_set_board([
		1, 1, 1, 2, 3, 4,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 1, "should find exactly 1 match")
	assert_eq(matches[0].element, 1, "match element should be WATER")
	assert_eq(matches[0].gem_count, 3, "match should have 3 gems")
	var expected_positions: Array[int] = [0, 1, 2]
	assert_eq(matches[0].positions, expected_positions, "match positions should be [0, 1, 2]")


func test_vertical_3_match() -> void:
	# From test vector: vertical_3_match
	_set_board([
		2, 3, 4, 5, 6, 1,
		2, 1, 3, 4, 5, 6,
		2, 4, 5, 6, 1, 3,
		3, 5, 6, 1, 2, 4,
		4, 6, 1, 2, 3, 5,
	])
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 1, "should find exactly 1 match")
	assert_eq(matches[0].element, 2, "match element should be FIRE")
	assert_eq(matches[0].gem_count, 3, "match should have 3 gems")
	var expected_positions: Array[int] = [0, 6, 12]
	assert_eq(matches[0].positions, expected_positions, "match positions should be [0, 6, 12]")


func test_no_match() -> void:
	# From test vector: no_match
	_set_board([
		1, 2, 3, 4, 5, 6,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 0, "should find no matches")


func test_l_shape_merge() -> void:
	# From test vector: l_shape_merge
	_set_board([
		1, 1, 1, 2, 3, 4,
		3, 4, 1, 5, 6, 2,
		2, 5, 1, 6, 4, 3,
		4, 6, 3, 2, 5, 1,
		5, 2, 4, 3, 6, 2,
	])
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 1, "L-shape should merge into 1 match")
	assert_eq(matches[0].element, 1, "match element should be WATER")
	assert_eq(matches[0].gem_count, 5, "L-shape should have 5 gems")
	var expected_positions: Array[int] = [0, 1, 2, 8, 14]
	assert_eq(matches[0].positions, expected_positions, "L-shape positions")


func test_multiple_matches() -> void:
	# From test vector: multiple_independent_matches
	_set_board([
		1, 1, 1, 3, 3, 3,
		2, 4, 5, 6, 4, 2,
		3, 5, 6, 4, 5, 1,
		4, 6, 4, 5, 6, 2,
		5, 4, 5, 6, 4, 3,
	])
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 2, "should find 2 independent matches")
	# Find the water match and grass match
	var water_match = _find_match_with_positions(matches, [0, 1, 2] as Array[int])
	var grass_match = _find_match_with_positions(matches, [3, 4, 5] as Array[int])
	assert_not_null(water_match, "should find water match at [0, 1, 2]")
	assert_not_null(grass_match, "should find grass match at [3, 4, 5]")
	assert_eq(water_match.element, 1, "water match element")
	assert_eq(grass_match.element, 3, "grass match element")


func test_locked_gems_not_matched() -> void:
	# Set up a board where 3 same-element gems are in a row, but the middle one is LOCKED
	_set_board([
		1, 1, 1, 2, 3, 4,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	# Lock the middle gem of the would-be match at position 1
	_board.get_gem(1).add_status(MFBoardTypes.GemStatus.LOCKED)
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 0, "locked gem should prevent match")


func test_5_gem_match() -> void:
	# From test vector: horizontal_5_match
	_set_board([
		3, 3, 3, 3, 3, 1,
		1, 2, 4, 5, 6, 2,
		2, 4, 5, 6, 1, 3,
		4, 5, 6, 1, 2, 4,
		5, 6, 1, 2, 3, 5,
	])
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 1, "should find exactly 1 match")
	assert_eq(matches[0].element, 3, "match element should be GRASS")
	assert_eq(matches[0].gem_count, 5, "match should have 5 gems")
	var expected_positions: Array[int] = [0, 1, 2, 3, 4]
	assert_eq(matches[0].positions, expected_positions, "5-gem match positions")
