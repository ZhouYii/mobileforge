class_name MFTabBar extends HBoxContainer
## Horizontal tab selection component.
## Active tab gets a bottom border highlight + full color; inactive tabs are dimmed.

signal tab_selected(index: int)

var _tabs: Array = []  # Array of {label: String, color: Color, button: Button}
var _active_index: int = 0
var tab_min_size: Vector2 = Vector2(90, 34)

var _active_color: Color = Color.WHITE
var _inactive_color: Color = Color(0.45, 0.45, 0.45)
var _highlight_height: int = 3


func _init() -> void:
	alignment = BoxContainer.ALIGNMENT_CENTER
	add_theme_constant_override("separation", 4)


func add_tab(label: String, color: Color = Color.WHITE) -> void:
	var btn := Button.new()
	btn.text = label
	btn.custom_minimum_size = tab_min_size
	btn.add_theme_font_size_override("font_size", 12)
	var idx := _tabs.size()
	btn.pressed.connect(func(): set_active(idx))
	add_child(btn)
	_tabs.append({"label": label, "color": color, "button": btn})
	_refresh()


func set_active(index: int) -> void:
	if index < 0 or index >= _tabs.size():
		return
	_active_index = index
	_refresh()
	tab_selected.emit(index)


func get_active() -> int:
	return _active_index


func get_tab_count() -> int:
	return _tabs.size()


func _refresh() -> void:
	for i in range(_tabs.size()):
		var entry: Dictionary = _tabs[i]
		var btn: Button = entry.button
		var color: Color = entry.color
		if i == _active_index:
			btn.modulate = color
			# Create bottom highlight via stylebox
			var style := StyleBoxFlat.new()
			style.bg_color = Color(0.18, 0.18, 0.22)
			style.border_width_bottom = _highlight_height
			style.border_color = color
			style.corner_radius_top_left = 4
			style.corner_radius_top_right = 4
			style.content_margin_left = 8
			style.content_margin_right = 8
			style.content_margin_top = 6
			style.content_margin_bottom = 6
			btn.add_theme_stylebox_override("normal", style)
			btn.add_theme_stylebox_override("hover", style)
			btn.add_theme_stylebox_override("pressed", style)
		else:
			btn.modulate = _inactive_color
			# Remove style overrides for inactive tabs
			btn.remove_theme_stylebox_override("normal")
			btn.remove_theme_stylebox_override("hover")
			btn.remove_theme_stylebox_override("pressed")
