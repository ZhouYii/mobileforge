class_name MFMergeLoop extends MFGameLoop
## Grid-based merge game loop.
## Players place items on a grid and merge matching adjacent items to level them up.

var _grid: Array = []  # 2D array [row][col] of {item_id, level} or null
var _rows: int = 0
var _cols: int = 0
var _is_active: bool = false
var _merge_count: int = 0
var _highest_level: int = 0
var _event_bus: Object


func _init(event_bus: Object = null) -> void:
	_event_bus = event_bus


func start(config: Dictionary) -> void:
	_rows = int(config.get("rows", 5))
	_cols = int(config.get("cols", 5))
	_grid.clear()
	for r in range(_rows):
		var row: Array = []
		for c in range(_cols):
			row.append(null)
		_grid.append(row)
	_is_active = true
	_merge_count = 0
	_highest_level = 0
	_emit("merge_started", {"rows": _rows, "cols": _cols})


func process_input(input: Dictionary) -> MFGameLoopTypes.PhaseResult:
	var action: String = input.get("action", "")
	var result := MFGameLoopTypes.PhaseResult.new(action, true)

	match action:
		"place":
			var row: int = int(input.get("row", -1))
			var col: int = int(input.get("col", -1))
			var item_id: String = input.get("item_id", "")
			result.data = _place_item(row, col, item_id)
		"merge":
			var row: int = int(input.get("row", -1))
			var col: int = int(input.get("col", -1))
			var target_row: int = int(input.get("target_row", -1))
			var target_col: int = int(input.get("target_col", -1))
			result.data = _merge_items(row, col, target_row, target_col)
		_:
			result.completed = false

	return result


func tick(_delta: float) -> MFGameLoopTypes.PhaseResult:
	# Merge games are input-driven, tick is a no-op
	return MFGameLoopTypes.PhaseResult.new("idle", false)


func get_state() -> MFGameLoopTypes.GameLoopState:
	var s := MFGameLoopTypes.GameLoopState.new()
	s.phase = "playing" if _is_active else "inactive"
	s.is_active = _is_active
	s.custom = {
		"grid": _grid.duplicate(true),
		"merge_count": _merge_count,
		"highest_level": _highest_level,
	}
	return s


func is_active() -> bool:
	return _is_active


func _place_item(row: int, col: int, item_id: String) -> Dictionary:
	if not _in_bounds(row, col):
		return {"success": false, "error": "out_of_bounds"}
	if _grid[row][col] != null:
		return {"success": false, "error": "cell_occupied"}
	_grid[row][col] = {"item_id": item_id, "level": 1}
	return {"success": true}


func _merge_items(r1: int, c1: int, r2: int, c2: int) -> Dictionary:
	if not _in_bounds(r1, c1) or not _in_bounds(r2, c2):
		return {"success": false, "error": "out_of_bounds"}
	var a = _grid[r1][c1]
	var b = _grid[r2][c2]
	if a == null or b == null:
		return {"success": false, "error": "empty_cell"}
	if a.item_id != b.item_id or a.level != b.level:
		return {"success": false, "error": "not_mergeable"}

	var new_level: int = a.level + 1
	_grid[r2][c2] = {"item_id": a.item_id, "level": new_level}
	_grid[r1][c1] = null
	_merge_count += 1
	if new_level > _highest_level:
		_highest_level = new_level

	_emit("items_merged", {"item_id": a.item_id, "new_level": new_level, "position": [r2, c2]})
	return {"success": true, "new_level": new_level}


func _in_bounds(row: int, col: int) -> bool:
	return row >= 0 and row < _rows and col >= 0 and col < _cols


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)
