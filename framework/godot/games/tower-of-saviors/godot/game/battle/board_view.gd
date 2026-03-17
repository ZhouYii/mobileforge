extends Control
## Visual representation of the puzzle board.
## Handles touch/mouse drag mechanic (PAD-style: pick up gem, swap along path).
## Reads from MFBoardLogic, animates CascadeSteps.
## Styled with TosTheme element colors, unicode icons, and ToS-accurate drag/match animations.

signal turn_completed(cascade_steps: Array)  # Emitted when player finishes dragging and cascade resolves
signal cascade_animating(is_animating: bool)

var _board: RefCounted  # MFBoardLogic
var _gem_nodes: Array = []  # Array of PanelContainer (one per cell)
var _cell_size: float = TosTheme.GEM_CELL_SIZE
var _spacing: float = TosTheme.GEM_SPACING
var _dragging: bool = false
var _drag_start_pos: int = -1  # Flat index
var _drag_path: Array[int] = []
var _drag_indicator: PanelContainer  # Shows where the dragged gem is
var _drag_trail: Array = []  # Trail ring nodes on visited cells
var _move_timer: float = 0.0
var _move_time_limit: float = 5.0  # Seconds allowed for dragging
var _is_animating: bool = false
var _input_enabled: bool = true
var _board_bg: PanelContainer
var _time_bar: ColorRect  # Visual countdown during drag
var _time_bar_bg: ColorRect


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
	_drag_trail.clear()

	if _board == null:
		return

	var config = _board.config
	var board_width = config.cols * (_cell_size + _spacing) - _spacing
	var board_height = config.rows * (_cell_size + _spacing) - _spacing
	custom_minimum_size = Vector2(board_width + 12, board_height + 12)
	size = custom_minimum_size

	# Dark board background panel
	_board_bg = PanelContainer.new()
	var bg_style := StyleBoxFlat.new()
	bg_style.bg_color = Color(0.06, 0.06, 0.08, 0.9)
	bg_style.corner_radius_top_left = 8
	bg_style.corner_radius_top_right = 8
	bg_style.corner_radius_bottom_left = 8
	bg_style.corner_radius_bottom_right = 8
	bg_style.content_margin_left = 6
	bg_style.content_margin_right = 6
	bg_style.content_margin_top = 6
	bg_style.content_margin_bottom = 6
	_board_bg.add_theme_stylebox_override("panel", bg_style)
	_board_bg.size = custom_minimum_size
	_board_bg.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_board_bg)

	for i in range(config.total_cells()):
		var gem_node = _create_gem_node(i)
		_gem_nodes.append(gem_node)
		add_child(gem_node)

	_refresh_all_gems()

	# Create drag indicator (hidden initially)
	_drag_indicator = PanelContainer.new()
	_drag_indicator.custom_minimum_size = Vector2(_cell_size, _cell_size)
	_drag_indicator.size = Vector2(_cell_size, _cell_size)
	_drag_indicator.visible = false
	_drag_indicator.z_index = 10
	# Scale up per ToS FollowMouse
	_drag_indicator.scale = Vector2(TosTheme.GEM_DRAG_SCALE, TosTheme.GEM_DRAG_SCALE)
	add_child(_drag_indicator)

	# Move time countdown bar (below the board)
	var bar_y = board_height + 14
	_time_bar_bg = ColorRect.new()
	_time_bar_bg.size = Vector2(board_width, 6)
	_time_bar_bg.position = Vector2(6, bar_y)
	_time_bar_bg.color = Color(0.15, 0.15, 0.2)
	_time_bar_bg.visible = false
	_time_bar_bg.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_time_bar_bg)

	_time_bar = ColorRect.new()
	_time_bar.size = Vector2(board_width, 6)
	_time_bar.position = Vector2(6, bar_y)
	_time_bar.color = Color(0.2, 0.8, 0.2)
	_time_bar.visible = false
	_time_bar.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_time_bar)


func _create_gem_node(index: int) -> PanelContainer:
	var config = _board.config
	var row: int = config.pos_to_row(index)
	var col: int = config.pos_to_col(index)

	var node := PanelContainer.new()
	node.custom_minimum_size = Vector2(_cell_size, _cell_size)
	node.size = Vector2(_cell_size, _cell_size)
	node.position = Vector2(
		col * (_cell_size + _spacing) + 6,  # +6 for bg padding
		row * (_cell_size + _spacing) + 6
	)
	node.mouse_filter = MOUSE_FILTER_IGNORE

	# Rounded gem cell style
	var style := StyleBoxFlat.new()
	style.corner_radius_top_left = TosTheme.BOARD_CORNER_RADIUS
	style.corner_radius_top_right = TosTheme.BOARD_CORNER_RADIUS
	style.corner_radius_bottom_left = TosTheme.BOARD_CORNER_RADIUS
	style.corner_radius_bottom_right = TosTheme.BOARD_CORNER_RADIUS
	style.bg_color = Color.GRAY
	node.add_theme_stylebox_override("panel", style)

	var label := Label.new()
	label.name = "Label"
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.size = Vector2(_cell_size, _cell_size)
	label.add_theme_font_size_override("font_size", 18)
	label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.6))
	label.add_theme_constant_override("shadow_offset_x", 1)
	label.add_theme_constant_override("shadow_offset_y", 1)
	label.mouse_filter = MOUSE_FILTER_IGNORE
	node.add_child(label)

	return node


func _refresh_all_gems() -> void:
	var elements = _board.to_element_array()
	for i in range(elements.size()):
		_update_gem_visual(i, elements[i])


func _update_gem_visual(index: int, element: int) -> void:
	if index >= _gem_nodes.size():
		return
	var node: PanelContainer = _gem_nodes[index]
	var color := TosTheme.element_color(element)

	# Update panel style color
	var style: StyleBoxFlat = node.get_theme_stylebox("panel")
	style.bg_color = color

	var label: Label = node.get_node("Label")
	label.text = TosTheme.element_icon(element)

	# Show status overlays
	var gem = _board.get_gem(index) if _board != null else null
	if gem != null and gem.has_status(MFBoardTypes.GemStatus.FROZEN):
		node.modulate = Color(0.7, 0.85, 1.0, 0.8)
		label.text += " \u2744"  # ❄
	elif gem != null and gem.has_status(MFBoardTypes.GemStatus.LOCKED):
		node.modulate = Color(TosTheme.CARD_ALPHA_LOCKED, TosTheme.CARD_ALPHA_LOCKED, TosTheme.CARD_ALPHA_LOCKED, 0.7)
		label.text += " \U0001f512"  # 🔒
	elif gem != null and gem.has_status(MFBoardTypes.GemStatus.POISONED):
		node.modulate = Color(0.7, 1.0, 0.7, 0.9)
	else:
		node.modulate = Color.WHITE


func _pos_from_local(local_pos: Vector2) -> int:
	# Adjust for board padding
	var adjusted := local_pos - Vector2(6, 6)
	var col := int(adjusted.x / (_cell_size + _spacing))
	var row := int(adjusted.y / (_cell_size + _spacing))
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

	# Setup drag indicator with element style
	var element = gem.element
	var color := TosTheme.element_color(element)
	var ind_style := StyleBoxFlat.new()
	ind_style.bg_color = color
	ind_style.corner_radius_top_left = TosTheme.BOARD_CORNER_RADIUS
	ind_style.corner_radius_top_right = TosTheme.BOARD_CORNER_RADIUS
	ind_style.corner_radius_bottom_left = TosTheme.BOARD_CORNER_RADIUS
	ind_style.corner_radius_bottom_right = TosTheme.BOARD_CORNER_RADIUS
	_drag_indicator.add_theme_stylebox_override("panel", ind_style)
	_drag_indicator.visible = true
	_drag_indicator.modulate.a = TosTheme.GEM_DRAG_ALPHA
	_drag_indicator.position = local_pos - Vector2(_cell_size / 2, _cell_size / 2)

	# Add icon label to drag indicator
	for child in _drag_indicator.get_children():
		child.queue_free()
	var icon_label := Label.new()
	icon_label.text = TosTheme.element_icon(element)
	icon_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	icon_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	icon_label.size = Vector2(_cell_size, _cell_size)
	icon_label.add_theme_font_size_override("font_size", 18)
	icon_label.mouse_filter = MOUSE_FILTER_IGNORE
	_drag_indicator.add_child(icon_label)

	# Dim the source gem
	_gem_nodes[pos].modulate.a = 0.3


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

	# Add trail ring on visited cell
	_add_trail_ring(current_pos)

	# Update visuals
	_refresh_all_gems()

	# Keep source gem dimmed
	if _drag_path.size() > 0:
		var last_in_path := _drag_path[-1]
		_gem_nodes[last_in_path].modulate.a = 0.3


func _add_trail_ring(cell_index: int) -> void:
	if cell_index >= _gem_nodes.size():
		return
	var gem_node = _gem_nodes[cell_index]
	var ring := ColorRect.new()
	ring.size = Vector2(_cell_size + 4, _cell_size + 4)
	ring.position = gem_node.position - Vector2(2, 2)
	ring.color = Color(1.0, 1.0, 1.0, 0.3)
	ring.mouse_filter = MOUSE_FILTER_IGNORE
	ring.z_index = 5
	add_child(ring)
	_drag_trail.append(ring)
	# Fade trail over 0.4s (ToS gem effect duration)
	MFUIAnim.fade_out(ring, TosTheme.ANIM_GEM_EFFECT, true)


func _end_drag() -> void:
	if not _dragging:
		return

	_dragging = false
	_drag_indicator.visible = false

	# Clear trail rings
	_drag_trail.clear()

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

	var step = steps[index]

	# Flash matched gems with element-colored burst + scale
	for pos in step.removed_positions:
		if pos < _gem_nodes.size():
			var node: PanelContainer = _gem_nodes[pos]
			var element: int = step.matches[0].element if step.matches.size() > 0 else 0
			var flash_color := TosTheme.element_color(element)
			# Scale up 1.2x then fade
			node.pivot_offset = node.size / 2.0
			var scale_tween := node.create_tween()
			scale_tween.tween_property(node, "scale", Vector2(1.2, 1.2), TosTheme.ANIM_GEM_MATCH * 0.45) \
				.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
			# Flash white then element color
			var style: StyleBoxFlat = node.get_theme_stylebox("panel")
			style.bg_color = Color.WHITE
			var color_tween := node.create_tween()
			color_tween.tween_callback(func(): style.bg_color = flash_color).set_delay(TosTheme.ANIM_GEM_MATCH * 0.15)

	# After match duration, shrink and fade
	var match_timer = get_tree().create_timer(TosTheme.ANIM_GEM_MATCH * 0.45)
	match_timer.timeout.connect(func():
		for pos in step.removed_positions:
			if pos < _gem_nodes.size():
				var node: PanelContainer = _gem_nodes[pos]
				var shrink_tween := node.create_tween()
				shrink_tween.set_parallel(true)
				shrink_tween.tween_property(node, "scale", Vector2(0.3, 0.3), TosTheme.ANIM_GEM_MATCH * 0.55)
				shrink_tween.tween_property(node, "modulate:a", 0.0, TosTheme.ANIM_GEM_MATCH * 0.55)

		# After shrink, refresh and continue
		var settle_timer = get_tree().create_timer(TosTheme.ANIM_GEM_MATCH * 0.55 + 0.05)
		settle_timer.timeout.connect(func():
			# Reset scale/alpha for recycled nodes
			for pos in step.removed_positions:
				if pos < _gem_nodes.size():
					_gem_nodes[pos].scale = Vector2.ONE
					_gem_nodes[pos].modulate.a = 1.0
			_refresh_all_gems()
			_animate_cascade(steps, index + 1)))


func _process(delta: float) -> void:
	if _dragging:
		_move_timer += delta
		if _move_timer >= _move_time_limit:
			_end_drag()
			return
		# Update time bar
		if _time_bar != null and _time_bar_bg != null:
			_time_bar_bg.visible = true
			_time_bar.visible = true
			var ratio := 1.0 - (_move_timer / _move_time_limit)
			_time_bar.size.x = _time_bar_bg.size.x * ratio
			# Color: green > yellow > red as time runs out
			if ratio > 0.5:
				_time_bar.color = Color(0.2, 0.8, 0.2)
			elif ratio > 0.25:
				_time_bar.color = Color(0.9, 0.8, 0.1)
			else:
				_time_bar.color = Color(0.9, 0.15, 0.15)
	else:
		if _time_bar != null:
			_time_bar.visible = false
		if _time_bar_bg != null:
			_time_bar_bg.visible = false
