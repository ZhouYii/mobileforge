extends Control
## Displays combo count with animation and color ramp.
## 32px font with shadow, color ramp: 1-2=white, 3-4=yellow, 5-6=orange, 7+=gold+pulse.
## Pop-in over 0.3s (from ToS combo particle lifetime).

var _label: Label
var _combo_count: int = 0
var _display_timer: float = 0.0


func _init() -> void:
	_label = Label.new()
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_label.add_theme_font_size_override("font_size", 32)
	# Shadow outline for readability
	_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.8))
	_label.add_theme_constant_override("shadow_offset_x", 2)
	_label.add_theme_constant_override("shadow_offset_y", 2)
	add_child(_label)
	visible = false


func show_combo(count: int) -> void:
	_combo_count = count
	_label.text = "%d Combo!" % count
	visible = true
	_display_timer = 2.0

	# Color ramp based on combo count
	var color := TosTheme.combo_color(count)
	_label.add_theme_color_override("font_color", color)

	# Pop-in animation (0.3s from ToS combo particle)
	MFUIAnim.pop_in(self, TosTheme.ANIM_COMBO)

	# Extra pulse for high combos
	if count >= 7:
		# Delayed pulse after pop-in finishes
		var timer = get_tree().create_timer(TosTheme.ANIM_COMBO)
		timer.timeout.connect(func():
			if visible and is_inside_tree():
				MFUIAnim.pulse(self, 1.15, 0.6))


func _process(delta: float) -> void:
	if visible:
		_display_timer -= delta
		if _display_timer <= 0:
			MFUIAnim.fade_out(self, 0.2)
			_display_timer = 999.0  # Prevent re-triggering
