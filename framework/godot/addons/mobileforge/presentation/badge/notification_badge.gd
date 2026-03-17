class_name MFNotificationBadge extends Control
## Red circle badge with count number, attachable to any Control.
## Common pattern for new items, unclaimed rewards, unread notifications.
## set_count(0) hides the badge.

var _bg: ColorRect
var _label: Label
var _badge_color: Color = Color(0.9, 0.15, 0.15)
var _badge_size: float = 20.0


func _init() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE
	custom_minimum_size = Vector2(_badge_size, _badge_size)
	size = custom_minimum_size
	visible = false

	_bg = ColorRect.new()
	_bg.color = _badge_color
	_bg.size = Vector2(_badge_size, _badge_size)
	_bg.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_bg)

	_label = Label.new()
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_label.size = Vector2(_badge_size, _badge_size)
	_label.add_theme_font_size_override("font_size", 11)
	_label.add_theme_color_override("font_color", Color.WHITE)
	_label.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_label)


func set_count(count: int) -> void:
	if count <= 0:
		visible = false
		return
	visible = true
	if count > 99:
		_label.text = "99+"
	else:
		_label.text = str(count)
	# Pulse on change
	if is_inside_tree():
		MFUIAnim.scale_punch(self, 1.3, 0.2)


func set_color(color: Color) -> void:
	_badge_color = color
	if _bg != null:
		_bg.color = color


## Attach this badge to a control's top-right corner.
func attach_to(control: Control, offset: Vector2 = Vector2(-6, -6)) -> void:
	if get_parent() != null:
		get_parent().remove_child(self)
	control.add_child(self)
	position = Vector2(control.size.x + offset.x, offset.y)
