extends MFTestBase
## Tests for CascadeResolver (domain/board/cascade_resolver.gd)

const BoardConfigScript = preload("res://addons/mobileforge/domain/board/board_config.gd")
const BoardLogicScript = preload("res://addons/mobileforge/domain/board/board_logic.gd")
const BoardTypesScript = preload("res://addons/mobileforge/domain/board/board_types.gd")
const MatchDetectorScript = preload("res://addons/mobileforge/domain/board/match_detector.gd")
const CascadeResolverScript = preload("res://addons/mobileforge/domain/board/cascade_resolver.gd")

var _config: MFBoardConfig
var _board: MFBoardLogic


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_config = MFBoardConfig.new(5, 6)
	# Use deterministic seed for reproducible tests
	_board = MFBoardLogic.new(_config, 12345)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _set_board(elements: Array) -> void:
	var typed: Array[int] = []
	for e in elements:
		typed.append(e)
	_board.from_element_array(typed)


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_single_match_resolves() -> void:
	# From cascade test vector: single_match_no_cascade
	_set_board([
		1, 1, 1, 2, 3, 4,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	var steps := MFCascadeResolver.resolve(_board)
	assert_gte(steps.size(), 1, "should have at least 1 cascade step")
	# First step should contain the original match
	var first_step = steps[0]
	assert_gte(first_step.matches.size(), 1, "first step should have at least 1 match")
	assert_eq(first_step.step_index, 0, "first step index should be 0")


func test_cascade_step_has_drops() -> void:
	# Set up board where top-row gems match, so gems below must drop
	_set_board([
		1, 1, 1, 2, 3, 4,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	var steps := MFCascadeResolver.resolve(_board)
	assert_gte(steps.size(), 1, "should have at least 1 step")
	# After removing top-row gems [0,1,2], columns 0,1,2 should have drops
	var first_step = steps[0]
	# The removed positions should include [0, 1, 2]
	assert_true(first_step.removed_positions.has(0), "position 0 should be removed")
	assert_true(first_step.removed_positions.has(1), "position 1 should be removed")
	assert_true(first_step.removed_positions.has(2), "position 2 should be removed")


func test_cascade_step_has_spawns() -> void:
	# After removing gems and dropping, empty top cells should get new spawns
	_set_board([
		1, 1, 1, 2, 3, 4,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	var steps := MFCascadeResolver.resolve(_board)
	assert_gte(steps.size(), 1, "should have at least 1 step")
	var first_step = steps[0]
	assert_gt(first_step.spawned.size(), 0, "first step should have spawned gems")
	# Number of spawned gems should equal number of removed gems in the first step
	assert_eq(first_step.spawned.size(), first_step.removed_positions.size(),
		"spawned count should equal removed count")


func test_no_match_returns_empty() -> void:
	# Board with no matches
	_set_board([
		1, 2, 3, 4, 5, 6,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	var steps := MFCascadeResolver.resolve(_board)
	assert_eq(steps.size(), 0, "no matches should produce empty cascade")


func test_all_cells_filled_after_resolve() -> void:
	# After full cascade resolution, every cell should be filled
	_set_board([
		1, 1, 1, 2, 3, 4,
		2, 3, 4, 5, 6, 1,
		3, 4, 5, 6, 1, 2,
		4, 5, 6, 1, 2, 3,
		5, 6, 1, 2, 3, 4,
	])
	var _steps := MFCascadeResolver.resolve(_board)
	for i in range(_config.total_cells()):
		assert_not_null(_board.get_gem(i), "cell %d should not be null after full cascade" % i)
