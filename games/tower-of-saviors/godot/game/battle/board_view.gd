extends Control
## Visual representation of the puzzle board.
## Handles touch/mouse drag mechanic (PAD-style: pick up gem, swap along path).
## Reads from MFBoardLogic, animates CascadeSteps.

signal turn_completed(cascade_steps: Array)  # Emitted when player finishes dragging and cascade resolves
signal cascade_animating(is_animating: bool)

const ELEMENT_COLORS := {
	0: Color.GRAY,
	1: Color(0.2, 0.5, 1.0),    # Water - blue
	2: Color(1.0, 0.3, 0.2),    # Fire - red
	3: Color(0.3, 0.8, 0.2),    # Grass - green
	4: Color(1.0, 0.9, 0.3),    # Light - yellow
	5: Color(0.6, 0.3, 0.8),    # Dark - purple
	6: Color(1.0, 0.5, 0.7),    # Heart - pink
}

const ELEMENT_NAMES := {
	0: "?", 1: "W", 2: "F", 3: "G", 4: "L", 5: "D", 6: "H"
}

var _board: RefCounted  # MFBoardLogic
var _gem_nodes: Array = []  # Array of ColorRect (one per cell)
var _cell_size: float = 55.0
var _spacing: float = 4.0
var _dragging: bool = false
var _drag_start_pos: int = -1  # Flat index
var _drag_path: Array[int] = []
var _drag_indicator: ColorRect  # Shows where the dragged gem is
var _move_timer: float = 0.0
var _move_time_limit: float = 5.0  # Seconds allowed for dragging
var _is_animating: bool = false
var _input_enabled: bool = true


func setup(board: RefCounted) -> void:
	_board = board
	_build_grid()


func set_input_enabled(enabled: bool) -> void:
	_input_enabled = enabled


func is_animating() -> bool:
	return _is_animating


func _build_grid() -> void:
	# Clear existing
	for child in get_children():
		child.queue_free()
	_gem_nodes.clear()

	if _board == null:
		return

	var config = _board.config
	custom_minimum_size = Vector2(
		config.cols * (_cell_size + _spacing) - _spacing,
		config.rows * (_cell_size + _spacing) - _spacing
	)
	size = custom_minimum_size

	for i in range(config.total_cells()):
		var gem_node = _create_gem_node(i)
		_gem_nodes.append(gem_node)
		add_child(gem_node)

	_refresh_all_gems()

	# Create drag indicator (hidden initially)
	_drag_indicator = ColorRect.new()
	_drag_indicator.size = Vector2(_cell_size, _cell_size)
	_drag_indicator.visible = false
	_drag_indicator.modulate.a = 0.8
	_drag_indicator.z_index = 10
	add_child(_drag_indicator)


func _create_gem_node(index: int) -> Control:
	var config = _board.config
	var row := config.pos_to_row(index)
	var col := config.pos_to_col(index)

	var node = ColorRect.new()
	node.size = Vector2(_cell_size, _cell_size)
	node.position = Vector2(
		col * (_cell_size + _spacing),
		row * (_cell_size + _spacing)
	)

	var label = Label.new()
	label.name = "Label"
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.size = Vector2(_cell_size, _cell_size)
	node.add_child(label)

	return node


func _refresh_all_gems() -> void:
	var elements = _board.to_element_array()
	for i in range(elements.size()):
		_update_gem_visual(i, elements[i])


func _update_gem_visual(index: int, element: int) -> void:
	if index >= _gem_nodes.size():
		return
	var node: ColorRect = _gem_nodes[index]
	node.color = ELEMENT_COLORS.get(element, Color.GRAY)
	var label: Label = node.get_node("Label")
	label.text = ELEMENT_NAMES.get(element, "?")


func _pos_from_local(local_pos: Vector2) -> int:
	var col := int(local_pos.x / (_cell_size + _spacing))
	var row := int(local_pos.y / (_cell_size + _spacing))
	if _board.config.is_valid_rc(row, col):
		return _board.config.rc_to_pos(row, col)
	return -1


## --- Input Handling (PAD-style drag) ---

func _gui_input(event: InputEvent) -> void:
	if _is_animating or not _input_enabled:
		return

	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				_start_drag(event.position)
			else:
				_end_drag()

	elif event is InputEventMouseMotion and _dragging:
		_continue_drag(event.position)

	# Touch support
	elif event is InputEventScreenTouch:
		if event.pressed:
			_start_drag(event.position - global_position)
		else:
			_end_drag()

	elif event is InputEventScreenDrag and _dragging:
		_continue_drag(event.position - global_position)


func _start_drag(local_pos: Vector2) -> void:
	var pos := _pos_from_local(local_pos)
	if pos < 0:
		return

	var gem = _board.get_gem(pos)
	if gem == null:
		return

	# Check if gem can be moved
	if gem.has_status(MFBoardTypes.GemStatus.FROZEN) or gem.has_status(MFBoardTypes.GemStatus.PETRIFIED):
		return

	_dragging = true
	_drag_start_pos = pos
	_drag_path = []
	_move_timer = 0.0

	# Show drag indicator
	_drag_indicator.color = ELEMENT_COLORS.get(gem.element, Color.GRAY)
	_drag_indicator.visible = true
	_drag_indicator.position = local_pos - Vector2(_cell_size / 2, _cell_size / 2)

	# Dim the source gem
	_gem_nodes[pos].modulate.a = 0.4


func _continue_drag(local_pos: Vector2) -> void:
	if not _dragging:
		return

	# Update drag indicator position
	_drag_indicator.position = local_pos - Vector2(_cell_size / 2, _cell_size / 2)

	var pos := _pos_from_local(local_pos)
	if pos < 0 or pos == _drag_start_pos:
		return

	# Check if this is a new adjacent cell
	var current_pos := _drag_start_pos if _drag_path.is_empty() else _drag_path[-1]
	if pos == current_pos:
		return

	if not _board.config.are_adjacent(current_pos, pos):
		return

	# Avoid revisiting the immediate previous position (no back-and-forth)
	if _drag_path.size() >= 2 and pos == _drag_path[-2]:
		return

	# Perform swap
	_board.swap_gems(current_pos, pos)
	_drag_path.append(pos)

	# Update visuals
	_refresh_all_gems()

	# Keep source gem dimmed, show indicator at new position
	if _drag_path.size() > 0:
		var last_in_path := _drag_path[-1]
		_gem_nodes[last_in_path].modulate.a = 0.4


func _end_drag() -> void:
	if not _dragging:
		return

	_dragging = false
	_drag_indicator.visible = false

	# Restore all gem opacities
	for node in _gem_nodes:
		node.modulate.a = 1.0

	_refresh_all_gems()

	# If we actually moved, resolve cascade
	if not _drag_path.is_empty():
		_resolve_cascade()


func _resolve_cascade() -> void:
	_is_animating = true
	cascade_animating.emit(true)

	var cascade_steps = MFCascadeResolver.resolve(_board)

	# No matches after drag — just refresh
	if cascade_steps.is_empty():
		_is_animating = false
		cascade_animating.emit(false)
		_refresh_all_gems()
		return

	# Animate each step with a brief delay
	_animate_cascade(cascade_steps, 0)


func _animate_cascade(steps: Array, index: int) -> void:
	if index >= steps.size():
		_is_animating = false
		cascade_animating.emit(false)
		_refresh_all_gems()
		turn_completed.emit(steps)
		return

	# Flash matched gems briefly
	var step = steps[index]
	for pos in step.removed_positions:
		if pos < _gem_nodes.size():
			_gem_nodes[pos].color = Color.WHITE

	# After a delay, refresh and show next step
	var timer = get_tree().create_timer(0.3)
	timer.timeout.connect(func():
		_refresh_all_gems()
		var timer2 = get_tree().create_timer(0.15)
		timer2.timeout.connect(func():
			_animate_cascade(steps, index + 1)))


func _process(delta: float) -> void:
	if _dragging:
		_move_timer += delta
		if _move_timer >= _move_time_limit:
			_end_drag()
