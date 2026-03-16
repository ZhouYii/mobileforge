extends Control
## Displays combo count with animation.

var _label: Label
var _combo_count: int = 0
var _display_timer: float = 0.0


func _init() -> void:
	_label = Label.new()
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_label.add_theme_font_size_override("font_size", 24)
	add_child(_label)
	visible = false


func show_combo(count: int) -> void:
	_combo_count = count
	_label.text = "%d Combo!" % count
	visible = true
	_display_timer = 2.0
	# Scale pop effect
	scale = Vector2(1.3, 1.3)


func _process(delta: float) -> void:
	if visible:
		# Shrink back to normal
		scale = scale.lerp(Vector2.ONE, delta * 5.0)
		_display_timer -= delta
		if _display_timer <= 0:
			visible = false
