extends ScrollContainer
## Recycling 2D grid scroll view. Arranges items in a fixed number of columns.

var _data: Array = []
var _cell_factory: Callable
var _bind_callback: Callable  # func(cell: Node, data: Variant, index: int)
var _columns: int = 4
var _cell_size: Vector2 = Vector2(80, 80)
var _spacing: float = 4.0
var _pool: MFGridCellPool
var _content: Control
var _visible_cells: Dictionary = {}  # index -> Node

func setup(cell_factory: Callable, bind_callback: Callable, columns: int = 4, cell_size: Vector2 = Vector2(80, 80)) -> void:
	_cell_factory = cell_factory
	_bind_callback = bind_callback
	_columns = columns
	_cell_size = cell_size
	_pool = MFGridCellPool.new(cell_factory)

	_content = Control.new()
	add_child(_content)

func set_data(data: Array) -> void:
	_recycle_all()
	_data = data
	var rows := ceili(float(data.size()) / _columns)
	_content.custom_minimum_size.y = rows * (_cell_size.y + _spacing)
	_update_visible()

func get_data_count() -> int:
	return _data.size()

func _process(_delta: float) -> void:
	_update_visible()

func _update_visible() -> void:
	if _data.is_empty():
		return

	var scroll_y := get_v_scroll_bar().value if get_v_scroll_bar() != null else 0.0
	var viewport_h := size.y
	var row_height := _cell_size.y + _spacing

	var first_row := maxi(int(scroll_y / row_height) - 1, 0)
	var last_row := mini(int((scroll_y + viewport_h) / row_height) + 1, ceili(float(_data.size()) / _columns) - 1)

	var first_idx := first_row * _columns
	var last_idx := mini((last_row + 1) * _columns - 1, _data.size() - 1)

	# Recycle outside range
	for idx in _visible_cells.keys():
		if idx < first_idx or idx > last_idx:
			_pool.release(_visible_cells[idx])
			_visible_cells.erase(idx)

	# Create in range
	for idx in range(first_idx, last_idx + 1):
		if idx >= _data.size():
			break
		if _visible_cells.has(idx):
			continue
		var cell := _pool.acquire()
		cell.visible = true
		var row := idx / _columns
		var col := idx % _columns
		cell.position = Vector2(col * (_cell_size.x + _spacing), row * row_height)
		cell.custom_minimum_size = _cell_size
		cell.size = _cell_size
		if cell.get_parent() != _content:
			_content.add_child(cell)
		_bind_callback.call(cell, _data[idx], idx)
		_visible_cells[idx] = cell

func _recycle_all() -> void:
	for idx in _visible_cells.keys():
		_pool.release(_visible_cells[idx])
	_visible_cells.clear()
