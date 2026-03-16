extends MFTestBase
## Tests for BoardLogic (domain/board/board_logic.gd)

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
# Tests
# ---------------------------------------------------------------------------

func test_init_board_fills_all_cells() -> void:
	_board.init_board()
	for i in range(_config.total_cells()):
		assert_not_null(_board.get_gem(i), "cell %d should not be null after init" % i)


func test_init_board_no_initial_matches() -> void:
	_board.init_board()
	var matches := MFMatchDetector.find_matches(_board)
	assert_eq(matches.size(), 0, "board should have no matches after init")


func test_swap_gems() -> void:
	_board.init_board()
	var gem_a = _board.get_gem(0)
	var gem_b = _board.get_gem(1)
	var elem_a: int = gem_a.element
	var elem_b: int = gem_b.element
	var result := _board.swap_gems(0, 1)
	assert_true(result, "swap should succeed")
	assert_eq(_board.get_gem(0).element, elem_b, "gem at 0 should now have element of former gem at 1")
	assert_eq(_board.get_gem(1).element, elem_a, "gem at 1 should now have element of former gem at 0")
	assert_eq(_board.get_gem(0).position, 0, "position field should be updated")
	assert_eq(_board.get_gem(1).position, 1, "position field should be updated")


func test_swap_frozen_gem_fails() -> void:
	_board.init_board()
	var gem = _board.get_gem(0)
	gem.add_status(MFBoardTypes.GemStatus.FROZEN)
	var result := _board.swap_gems(0, 1)
	assert_false(result, "swap should fail when gem is frozen")


func test_move_gem_path() -> void:
	# Set up a known board: positions 0..29
	var elements: Array[int] = []
	for i in range(30):
		elements.append((i % 6) + 1)
	_board.from_element_array(elements)

	var start_pos := 0
	var elem_at_0: int = _board.get_gem(0).element
	var elem_at_1: int = _board.get_gem(1).element
	var elem_at_2: int = _board.get_gem(2).element

	# Move gem from 0 along path [1, 2]
	var path: Array[int] = [1, 2]
	_board.move_gem_path(start_pos, path)

	# After moving: gem originally at 0 is now at 2
	assert_eq(_board.get_gem(2).element, elem_at_0, "original gem should be at end of path")
	# Gem originally at 1 should now be at 0 (from first swap)
	assert_eq(_board.get_gem(0).element, elem_at_1, "gem from pos 1 should be at pos 0")
	# Gem originally at 2 should now be at 1 (from second swap)
	assert_eq(_board.get_gem(1).element, elem_at_2, "gem from pos 2 should be at pos 1")


func test_from_element_array() -> void:
	var elements: Array[int] = [
		1, 2, 3, 4, 5, 6,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	]
	_board.from_element_array(elements)
	var result := _board.to_element_array()
	assert_eq(result.size(), elements.size(), "array sizes should match")
	for i in range(elements.size()):
		assert_eq(result[i], elements[i], "element at %d should match" % i)


func test_change_gem_element() -> void:
	_board.init_board()
	_board.change_gem_element(0, MFBoardTypes.Element.HEART)
	assert_eq(_board.get_gem(0).element, MFBoardTypes.Element.HEART, "element should be changed to HEART")


func test_board_config_pos_conversion() -> void:
	# 5 rows, 6 cols. Position 8 = row 1, col 2
	assert_eq(_config.pos_to_row(8), 1, "pos 8 -> row 1")
	assert_eq(_config.pos_to_col(8), 2, "pos 8 -> col 2")
	assert_eq(_config.rc_to_pos(1, 2), 8, "row 1, col 2 -> pos 8")
	# Position 0 = row 0, col 0
	assert_eq(_config.pos_to_row(0), 0, "pos 0 -> row 0")
	assert_eq(_config.pos_to_col(0), 0, "pos 0 -> col 0")
	assert_eq(_config.rc_to_pos(0, 0), 0, "row 0, col 0 -> pos 0")
	# Position 29 (last cell) = row 4, col 5
	assert_eq(_config.pos_to_row(29), 4, "pos 29 -> row 4")
	assert_eq(_config.pos_to_col(29), 5, "pos 29 -> col 5")
	assert_eq(_config.rc_to_pos(4, 5), 29, "row 4, col 5 -> pos 29")


func test_board_config_adjacency() -> void:
	# Horizontal neighbors
	assert_true(_config.are_adjacent(0, 1), "pos 0 and 1 are adjacent (horizontal)")
	# Vertical neighbors
	assert_true(_config.are_adjacent(0, 6), "pos 0 and 6 are adjacent (vertical)")
	# Diagonal = NOT adjacent
	assert_false(_config.are_adjacent(0, 7), "pos 0 and 7 are diagonal, not adjacent")
	# Same position
	assert_false(_config.are_adjacent(0, 0), "same position is not adjacent")
	# Far apart
	assert_false(_config.are_adjacent(0, 29), "pos 0 and 29 are not adjacent")
	# Wrap-around should NOT count (end of row 0 and start of row 1)
	assert_false(_config.are_adjacent(5, 6), "pos 5 and 6 are not adjacent (different rows)")
