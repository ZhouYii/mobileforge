extends Control
class_name MFBadgeDot
## Red dot notification indicator. Binds to a BadgeManager source.

var _badge_manager: MFBadgeManager
var _source_id: StringName = &""
var _show_count: bool = false
var _label: Label
var _dot: ColorRect


func _ready() -> void:
	# Create visual elements
	_dot = ColorRect.new()
	_dot.color = Color(1.0, 0.2, 0.2)
	_dot.custom_minimum_size = Vector2(16, 16)
	_dot.visible = false
	add_child(_dot)

	_label = Label.new()
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_label.add_theme_font_size_override("font_size", 10)
	_label.add_theme_color_override("font_color", Color.WHITE)
	_dot.add_child(_label)

	custom_minimum_size = Vector2(16, 16)
	mouse_filter = Control.MOUSE_FILTER_IGNORE


## Bind to a badge manager source. Auto-updates when badge changes.
func bind(badge_manager: MFBadgeManager, source_id: StringName) -> void:
	# Unbind previous
	if _badge_manager != null:
		_badge_manager.badge_changed.disconnect(_on_badge_changed)
	_badge_manager = badge_manager
	_source_id = source_id
	if _badge_manager != null:
		_badge_manager.badge_changed.connect(_on_badge_changed)
		_update_display(_badge_manager.get_badge_count(_source_id))


## Set whether to show count number or just a dot.
func set_show_count(show: bool) -> void:
	_show_count = show
	if _badge_manager != null:
		_update_display(_badge_manager.get_badge_count(_source_id))


func _on_badge_changed(source_id: StringName, count: int) -> void:
	if source_id == _source_id:
		_update_display(count)


func _update_display(count: int) -> void:
	_dot.visible = count > 0
	if _show_count and count > 0:
		_label.text = str(count) if count < 100 else "99+"
		_label.visible = true
	else:
		_label.visible = false
