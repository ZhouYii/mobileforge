class_name MFProgressBar extends Control
## Multi-layer animated progress bar.
## 4 layers: background, fill, loss flash, heal flash.
## Inspired by ToS GamePlayBar: smooth value transitions with
## delayed "loss" shrink and "heal" grow effects.

var _bg: ColorRect
var _fill: ColorRect
var _loss_flash: ColorRect
var _heal_flash: ColorRect
var _text_label: Label

var _current_ratio: float = 1.0
var _target_ratio: float = 1.0
var _bar_width: float = 200.0

var animation_duration: float = 0.4
var fill_color: Color = Color(0.2, 0.8, 0.2)
var loss_color: Color = Color(0.9, 0.15, 0.15)
var heal_color: Color = Color(1.0, 1.0, 1.0, 0.7)
var bg_color: Color = Color(0.12, 0.12, 0.15)
var border_color: Color = Color(0.3, 0.3, 0.35)
var corner_radius: int = 4


func _init() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE


func setup(width: float, height: float) -> void:
	_bar_width = width
	custom_minimum_size = Vector2(width, height)
	size = custom_minimum_size
	_build()


func _build() -> void:
	for child in get_children():
		child.queue_free()

	var h := size.y

	# Background with border
	_bg = ColorRect.new()
	_bg.size = size
	_bg.color = bg_color
	_bg.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_bg)

	# Loss flash layer (red, shows behind fill when HP drops)
	_loss_flash = ColorRect.new()
	_loss_flash.size = Vector2(_bar_width, h)
	_loss_flash.color = loss_color
	_loss_flash.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_loss_flash)

	# Heal flash layer (white, briefly extends past fill on heal)
	_heal_flash = ColorRect.new()
	_heal_flash.size = Vector2(0, h)
	_heal_flash.color = heal_color
	_heal_flash.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_heal_flash)

	# Fill layer (main colored bar)
	_fill = ColorRect.new()
	_fill.size = Vector2(_bar_width, h)
	_fill.color = fill_color
	_fill.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(_fill)

	# Optional text overlay
	_text_label = Label.new()
	_text_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_text_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_text_label.size = size
	_text_label.add_theme_font_size_override("font_size", maxi(int(size.y * 0.6), 10))
	_text_label.mouse_filter = MOUSE_FILTER_IGNORE
	_text_label.visible = false
	# Shadow for readability
	_text_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.8))
	_text_label.add_theme_constant_override("shadow_offset_x", 1)
	_text_label.add_theme_constant_override("shadow_offset_y", 1)
	add_child(_text_label)

	_sync_bars()


func set_value(ratio: float, animate: bool = true) -> void:
	ratio = clampf(ratio, 0.0, 1.0)
	var old_ratio := _current_ratio
	_target_ratio = ratio

	if not animate or not is_inside_tree():
		_current_ratio = ratio
		_sync_bars()
		return

	if ratio < old_ratio:
		# Damage: fill shrinks immediately, loss flash shrinks with delay
		_current_ratio = ratio
		_fill.size.x = _bar_width * ratio
		# Loss flash shows the old value, then shrinks
		_loss_flash.size.x = _bar_width * old_ratio
		var tween := create_tween()
		tween.tween_interval(animation_duration * 0.5)
		tween.tween_property(_loss_flash, "size:x", _bar_width * ratio, animation_duration) \
			.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	else:
		# Heal: heal flash extends to new value, then fill catches up
		_heal_flash.size.x = _bar_width * ratio
		var tween := create_tween()
		tween.tween_property(_fill, "size:x", _bar_width * ratio, animation_duration) \
			.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
		tween.tween_callback(func():
			_heal_flash.size.x = _bar_width * ratio
			_current_ratio = ratio)
		# Fade heal flash
		var flash_tween := create_tween()
		_heal_flash.modulate.a = 1.0
		flash_tween.tween_interval(animation_duration * 0.6)
		flash_tween.tween_property(_heal_flash, "modulate:a", 0.0, animation_duration * 0.4)
		flash_tween.tween_callback(func(): _heal_flash.modulate.a = 1.0)


func set_colors(p_fill: Color, p_loss: Color = Color.RED, p_heal: Color = Color.WHITE) -> void:
	fill_color = p_fill
	loss_color = p_loss
	heal_color = p_heal
	if _fill != null:
		_fill.color = fill_color
	if _loss_flash != null:
		_loss_flash.color = loss_color
	if _heal_flash != null:
		_heal_flash.color = heal_color


func set_text(text: String) -> void:
	if _text_label != null:
		_text_label.text = text
		_text_label.visible = not text.is_empty()


func set_text_visible(p_visible: bool) -> void:
	if _text_label != null:
		_text_label.visible = p_visible


func get_ratio() -> float:
	return _current_ratio


func _sync_bars() -> void:
	if _fill == null:
		return
	var w := _bar_width * _current_ratio
	_fill.size.x = w
	_loss_flash.size.x = w
	_heal_flash.size.x = 0
