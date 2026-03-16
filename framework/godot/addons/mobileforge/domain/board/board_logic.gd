class_name MFBoardLogic extends RefCounted
## Core board state manager. Owns the grid array.
## Pure logic — no engine dependencies.
##
## The grid is a flat Array[GemState] of size rows*cols.
## Position = row * cols + col (row-major, origin top-left).

var config: MFBoardConfig
var _grid: Array = []  ## Array of GemState (or null for empty)
var _rng: RandomNumberGenerator


func _init(p_config: MFBoardConfig, seed: int = -1) -> void:
	config = p_config
	_rng = RandomNumberGenerator.new()
	if seed >= 0:
		_rng.seed = seed
	_grid.resize(config.total_cells())


## Initialize board with random gems, ensuring no initial matches.
func init_board() -> void:
	for i in range(config.total_cells()):
		_grid[i] = _spawn_gem_no_match(i)


## Get gem at position (null if empty or invalid).
func get_gem(pos: int) -> RefCounted:
	if not config.is_valid_pos(pos):
		return null
	return _grid[pos]


## Set gem at position.
func set_gem(pos: int, gem) -> void:
	if config.is_valid_pos(pos):
		_grid[pos] = gem
		if gem != null:
			gem.position = pos


## Swap two gems by position. Returns true if valid swap.
func swap_gems(pos_a: int, pos_b: int) -> bool:
	if not config.is_valid_pos(pos_a) or not config.is_valid_pos(pos_b):
		return false
	# Check for frozen/petrified gems that can't be moved
	var gem_a = _grid[pos_a]
	var gem_b = _grid[pos_b]
	if gem_a != null and (gem_a.has_status(MFBoardTypes.GemStatus.FROZEN) or gem_a.has_status(MFBoardTypes.GemStatus.PETRIFIED)):
		return false
	if gem_b != null and (gem_b.has_status(MFBoardTypes.GemStatus.FROZEN) or gem_b.has_status(MFBoardTypes.GemStatus.PETRIFIED)):
		return false
	_grid[pos_a] = gem_b
	_grid[pos_b] = gem_a
	if gem_a != null:
		gem_a.position = pos_b
	if gem_b != null:
		gem_b.position = pos_a
	return true


## Move gem along a path (ToS drag mechanic — gem swaps with each cell along the path).
func move_gem_path(start_pos: int, path: Array[int]) -> void:
	var current := start_pos
	for next_pos in path:
		swap_gems(current, next_pos)
		current = next_pos


## Change the element of a gem at a position.
func change_gem_element(pos: int, new_element: int) -> void:
	var gem = _grid[pos]
	if gem != null:
		gem.element = new_element


## Get a snapshot of the entire grid (deep copy).
func snapshot() -> Array:
	var result: Array = []
	for gem in _grid:
		result.append(gem.duplicate() if gem != null else null)
	return result


## Get flat grid as array of element ints (for quick comparison/testing).
func to_element_array() -> Array[int]:
	var result: Array[int] = []
	for gem in _grid:
		result.append(gem.element if gem != null else 0)
	return result


## Set board from an array of element ints (for testing).
func from_element_array(elements: Array[int]) -> void:
	for i in range(mini(elements.size(), config.total_cells())):
		if elements[i] == 0:
			_grid[i] = null
		else:
			var gem = MFBoardTypes.GemState.new(elements[i], i)
			_grid[i] = gem


## Private: spawn a gem that won't create an immediate match.
func _spawn_gem_no_match(pos: int) -> RefCounted:
	var available := config.elements.duplicate()
	var row := config.pos_to_row(pos)
	var col := config.pos_to_col(pos)

	# Check left 2
	if col >= 2:
		var e1 = _grid[pos - 1]
		var e2 = _grid[pos - 2]
		if e1 != null and e2 != null and e1.element == e2.element:
			available.erase(e1.element)

	# Check above 2
	if row >= 2:
		var e1 = _grid[pos - config.cols]
		var e2 = _grid[pos - 2 * config.cols]
		if e1 != null and e2 != null and e1.element == e2.element:
			available.erase(e1.element)

	if available.is_empty():
		available = config.elements.duplicate()

	var element: int = available[_rng.randi() % available.size()]
	return MFBoardTypes.GemState.new(element, pos)
