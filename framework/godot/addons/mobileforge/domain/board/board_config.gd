class_name MFBoardConfig extends RefCounted
## Configuration for a board instance. Immutable after creation.

var rows: int = 5
var cols: int = 6
var elements: Array[int] = [1, 2, 3, 4, 5, 6]  ## Which elements can spawn
var min_match: int = 3  ## Minimum match length
var allow_diagonal: bool = false  ## ToS doesn't allow diagonal matches


func _init(p_rows: int = 5, p_cols: int = 6, p_elements: Array[int] = [1, 2, 3, 4, 5, 6], p_min_match: int = 3) -> void:
	rows = p_rows
	cols = p_cols
	elements = p_elements
	min_match = p_min_match


func total_cells() -> int:
	return rows * cols


func pos_to_row(pos: int) -> int:
	return pos / cols


func pos_to_col(pos: int) -> int:
	return pos % cols


func rc_to_pos(row: int, col: int) -> int:
	return row * cols + col


func is_valid_pos(pos: int) -> bool:
	return pos >= 0 and pos < total_cells()


func is_valid_rc(row: int, col: int) -> bool:
	return row >= 0 and row < rows and col >= 0 and col < cols


func are_adjacent(pos_a: int, pos_b: int) -> bool:
	var r1 := pos_to_row(pos_a)
	var c1 := pos_to_col(pos_a)
	var r2 := pos_to_row(pos_b)
	var c2 := pos_to_col(pos_b)
	return (abs(r1 - r2) + abs(c1 - c2)) == 1
